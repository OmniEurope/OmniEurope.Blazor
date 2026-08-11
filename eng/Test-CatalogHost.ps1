[CmdletBinding()]
param(
    [int]$Port = 5187,
    [int]$BrowserPort = 9222,
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\artifacts\catalog-smoke\OmniEurope.Blazor.Catalog.dll'),
    [ValidateRange(1, 60)]
    [int]$RequestTimeoutSeconds = 10
)

$ErrorActionPreference = 'Stop'
$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
$resolvedAssembly = (Resolve-Path -LiteralPath $AssemblyPath).Path
$contentRoot = Split-Path -Parent $resolvedAssembly
$baseUri = "http://127.0.0.1:$Port"
$stdout = New-TemporaryFile
$stderr = New-TemporaryFile
$process = $null
$browser = $null
$browserProfile = $null

try {
    $existingHost = $false
    try {
        Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/csp-status" -TimeoutSec 1 -ErrorAction Stop | Out-Null
        $existingHost = $true
    }
    catch {
    }
    if ($existingHost) {
        throw ($psText.PortOwned -f $Port)
    }

    $start = @{
        FilePath = 'dotnet'
        ArgumentList = @($resolvedAssembly, '--urls', $baseUri, '--contentRoot', $contentRoot, '--environment', 'Production')
        WorkingDirectory = $contentRoot
        PassThru = $true
        RedirectStandardOutput = $stdout.FullName
        RedirectStandardError = $stderr.FullName
    }
    if ($IsWindows) { $start.WindowStyle = 'Hidden' }
    $process = Start-Process @start

    $homeResponse = $null
    for ($attempt = 1; $attempt -le 50; $attempt++) {
        if ($process.HasExited) {
            throw ($psText.CatalogStopped -f $process.ExitCode)
        }

        try {
            $homeResponse = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/" -TimeoutSec $RequestTimeoutSeconds -ErrorAction Stop
            if ($homeResponse.StatusCode -eq 200) { break }
        }
        catch {
            $homeResponse = $null
        }
        Start-Sleep -Milliseconds 200
    }

    if ($null -eq $homeResponse -or $homeResponse.StatusCode -ne 200) { throw $psText.CatalogNoHttp200 }
    $englishResponse = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/" -Headers @{ 'Accept-Language' = 'en' } -TimeoutSec $RequestTimeoutSeconds
    if ($englishResponse.Content -notmatch '<html lang="en"' -or $englishResponse.Content -notmatch 'Component catalog') {
        throw 'La négociation anglaise du catalogue est invalide.'
    }
    $frenchResponse = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/" -Headers @{ 'Accept-Language' = 'fr' } -TimeoutSec $RequestTimeoutSeconds
    if ($frenchResponse.Content -notmatch '<html lang="fr"' -or $frenchResponse.Content -notmatch 'Catalogue des composants') {
        throw 'La négociation française du catalogue est invalide.'
    }
    $notFoundResponse = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/route-inconnue" -TimeoutSec $RequestTimeoutSeconds
    if ($notFoundResponse.StatusCode -ne 200 -or $notFoundResponse.Content -notmatch 'Page introuvable' -or $notFoundResponse.Content -notmatch 'Retour au catalogue') {
        throw 'La vue accessible de route introuvable est absente.'
    }
    $csp = ($homeResponse.Headers['Content-Security-Policy'] -join '; ')
    if ([string]::IsNullOrWhiteSpace($csp)) { throw $psText.CspHeaderMissing }
    if ($csp -match "unsafe-inline|unsafe-eval") { throw "La CSP contient une directive interdite : $csp" }
    if ($csp -notmatch "connect-src\s+'self'(?:;|$)" -or $csp -match 'wss?:') { throw "La directive connect-src autorise une connexion distante : $csp" }
    foreach ($header in @{
        'X-Content-Type-Options' = 'nosniff'
        'Referrer-Policy' = 'no-referrer'
        'Permissions-Policy' = 'camera=\(\), geolocation=\(\), microphone=\(\)'
    }.GetEnumerator()) {
        $value = ($homeResponse.Headers[$header.Key] -join ', ')
        if ($value -notmatch "^$($header.Value)$") { throw ($psText.HeaderInvalid -f $header.Key, $value) }
    }

    $status = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/csp-status" -TimeoutSec $RequestTimeoutSeconds
    if ($status.StatusCode -ne 200 -or $status.Content -notmatch '"status"\s*:\s*"pass"' -or $status.Content -notmatch '"violations"\s*:\s*0') {
        throw "Le collecteur CSP n'est pas vert : $($status.Content)"
    }

    foreach ($path in @(
        '/_framework/blazor.web.js',
        '/_content/OmniEurope.Blazor/omnieurope.blazor.css',
        '/_content/OmniEurope.Blazor/omniInterop.js'
    )) {
        $asset = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri$path" -TimeoutSec $RequestTimeoutSeconds
        if ($asset.StatusCode -ne 200 -or $asset.RawContentLength -le 0) { throw "Asset catalogue invalide : $path" }
    }

    $browserCommands = @('msedge', 'google-chrome', 'chrome', 'chromium', 'chromium-browser')
    $browserPath = $browserCommands | ForEach-Object { Get-Command $_ -ErrorAction SilentlyContinue } | Select-Object -First 1 -ExpandProperty Source
    if (-not $browserPath -and $IsWindows) {
        $edgePaths = @(
            (Join-Path ${env:ProgramFiles(x86)} 'Microsoft\Edge\Application\msedge.exe'),
            (Join-Path $env:ProgramFiles 'Microsoft\Edge\Application\msedge.exe')
        )
        $browserPath = $edgePaths | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    }
    if (-not $browserPath) { throw $psText.ChromiumMissing }

    $browserProfile = Join-Path ([IO.Path]::GetTempPath()) ("omni-catalog-cdp-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $browserProfile | Out-Null
    $browserArguments = @(
        '--headless=new', '--disable-gpu', '--disable-dev-shm-usage', '--no-sandbox',
        "--remote-debugging-port=$BrowserPort", "--user-data-dir=$browserProfile", 'about:blank'
    )
    $browserStart = @{ FilePath = $browserPath; ArgumentList = $browserArguments; PassThru = $true }
    if ($IsWindows) { $browserStart.WindowStyle = 'Hidden' }
    $browser = Start-Process @browserStart

    & node (Join-Path $PSScriptRoot 'Test-CatalogProbe.mjs') --endpoint "http://127.0.0.1:$BrowserPort" --url $baseUri
    if ($LASTEXITCODE -ne 0) { throw ($psText.CdpFailed -f 'Catalog', $LASTEXITCODE) }

    $status = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/csp-status" -TimeoutSec $RequestTimeoutSeconds
    if ($status.StatusCode -ne 200 -or $status.Content -notmatch '"status"\s*:\s*"pass"' -or $status.Content -notmatch '"violations"\s*:\s*0') {
        throw ($psText.CatalogCspAfter -f $status.Content)
    }

    Write-Host ($psText.CatalogPassed -f $process.Id)
}
catch {
    if (Test-Path -LiteralPath $stdout.FullName) { Get-Content -LiteralPath $stdout.FullName | Write-Host }
    if (Test-Path -LiteralPath $stderr.FullName) { Get-Content -LiteralPath $stderr.FullName | Write-Host }
    throw
}
finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id
        if (-not $process.WaitForExit(5000)) { throw ($psText.CatalogStopFailed -f $process.Id) }
    }
    if ($browser -and -not $browser.HasExited) {
        Stop-Process -Id $browser.Id
        $browser.WaitForExit(5000) | Out-Null
    }
    if ($browserProfile) {
        $resolvedProfile = [IO.Path]::GetFullPath($browserProfile)
        $resolvedTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
        if ($resolvedProfile.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase)) {
            Remove-Item -LiteralPath $resolvedProfile -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    Remove-Item -LiteralPath $stdout.FullName, $stderr.FullName -Force -ErrorAction SilentlyContinue
}

[CmdletBinding()]
param(
    [int]$Port = 5189,
    [int]$BrowserPort = 9223,
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\artifacts\auto-smoke\OmniEurope.Blazor.AutoSmoke.dll')
)

$ErrorActionPreference = 'Stop'
$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
$resolvedAssembly = (Resolve-Path -LiteralPath $AssemblyPath).Path
$workingDirectory = Split-Path -Parent $resolvedAssembly
$baseUri = "http://127.0.0.1:$Port"
$stdout = New-TemporaryFile
$stderr = New-TemporaryFile
$process = $null
$browser = $null
$browserProfile = $null

try {
    $existingHost = $false
    try {
        Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/" -TimeoutSec 1 -ErrorAction Stop | Out-Null
        $existingHost = $true
    }
    catch {
    }
    if ($existingHost) {
        throw ($psText.PortOwned -f $Port)
    }

    $start = @{
        FilePath = 'dotnet'
        ArgumentList = @($resolvedAssembly, '--urls', $baseUri, '--environment', 'Production')
        WorkingDirectory = $workingDirectory
        PassThru = $true
        RedirectStandardOutput = $stdout.FullName
        RedirectStandardError = $stderr.FullName
    }
    if ($IsWindows) { $start.WindowStyle = 'Hidden' }
    $process = Start-Process @start

    $rootResponse = $null
    for ($attempt = 1; $attempt -le 75; $attempt++) {
        if ($process.HasExited) {
            throw ($psText.HostStopped -f 'Interactive Auto', $process.ExitCode)
        }

        try {
            $rootResponse = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/" -TimeoutSec 2 -ErrorAction Stop
            if ($rootResponse.StatusCode -eq 200) { break }
        }
        catch {
        }
        Start-Sleep -Milliseconds 200
    }

    if ($null -eq $rootResponse -or $rootResponse.StatusCode -ne 200) { throw ($psText.HostNoHttp200 -f 'Interactive Auto') }
    if ($rootResponse.Content -notmatch 'OmniEurope\.Blazor Interactive Auto' -or $rootResponse.Content -notmatch 'auto-action') {
        throw $psText.AutoMarkersMissing
    }
    $englishResponse = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/" -Headers @{ 'Accept-Language' = 'en' } -TimeoutSec 2
    if ($englishResponse.Content -notmatch '<html lang="en"' -or $englishResponse.Content -notmatch 'Auto counter: 0') {
        throw 'La négociation anglaise Interactive Auto est invalide.'
    }
    $frenchResponse = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/" -Headers @{ 'Accept-Language' = 'fr' } -TimeoutSec 2
    if ($frenchResponse.Content -notmatch '<html lang="fr"' -or $frenchResponse.Content -notmatch 'Compteur Auto : 0') {
        throw 'La négociation française Interactive Auto est invalide.'
    }

    $csp = ($rootResponse.Headers['Content-Security-Policy'] -join '; ')
    if ([string]::IsNullOrWhiteSpace($csp)) { throw $psText.CspHeaderMissing }
    if ($csp -match "'unsafe-inline'|'unsafe-eval'") { throw "La CSP contient une directive interdite : $csp" }
    if ($csp -notmatch "script-src[^;]*'wasm-unsafe-eval'") { throw "La CSP ne permet pas l'initialisation WebAssembly : $csp" }
    if ($csp -notmatch "connect-src\s+'self'(?:;|$)" -or $csp -match 'wss?:') { throw "La directive connect-src autorise une connexion distante : $csp" }
    foreach ($header in @{
        'X-Content-Type-Options' = 'nosniff'
        'Referrer-Policy' = 'no-referrer'
        'Permissions-Policy' = 'camera=\(\), geolocation=\(\), microphone=\(\)'
    }.GetEnumerator()) {
        $value = ($rootResponse.Headers[$header.Key] -join ', ')
        if ($value -notmatch "^$($header.Value)$") { throw ($psText.HeaderInvalid -f $header.Key, $value) }
    }

    foreach ($path in @(
        '/_framework/blazor.web.js',
        '/_content/OmniEurope.Blazor/omnieurope.blazor.css',
        '/_content/OmniEurope.Blazor/omniInterop.js'
    )) {
        $asset = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri$path" -TimeoutSec 2
        if ($asset.StatusCode -ne 200 -or $asset.RawContentLength -le 0) { throw "Asset Interactive Auto invalide : $path" }
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

    $browserProfile = Join-Path ([IO.Path]::GetTempPath()) ("omni-auto-cdp-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $browserProfile | Out-Null
    $browserArguments = @(
        '--headless=new', '--disable-gpu', '--disable-dev-shm-usage', '--no-sandbox',
        "--remote-debugging-port=$BrowserPort", "--user-data-dir=$browserProfile", $baseUri
    )
    $browserStart = @{ FilePath = $browserPath; ArgumentList = $browserArguments; PassThru = $true }
    if ($IsWindows) { $browserStart.WindowStyle = 'Hidden' }
    $browser = Start-Process @browserStart

    & node (Join-Path $PSScriptRoot 'Test-CdpProbe.mjs') --endpoint "http://127.0.0.1:$BrowserPort" --selector '#auto-action' --output '#auto-action' --expected 'Compteur Auto : 1'
    if ($LASTEXITCODE -ne 0) { throw ($psText.CdpFailed -f 'Interactive Auto', $LASTEXITCODE) }

    Write-Host ($psText.AutoPassed -f $process.Id)
}
catch {
    if (Test-Path -LiteralPath $stdout.FullName) { Get-Content -LiteralPath $stdout.FullName | Write-Host }
    if (Test-Path -LiteralPath $stderr.FullName) { Get-Content -LiteralPath $stderr.FullName | Write-Host }
    throw
}
finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id
        if (-not $process.WaitForExit(5000)) { throw ($psText.AutoStopFailed -f $process.Id) }
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

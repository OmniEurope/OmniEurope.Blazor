[CmdletBinding()]
param(
    [int]$Port = 5189,
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\artifacts\auto-smoke\OmniEurope.Blazor.AutoSmoke.dll'),
    [string]$BrowserLanguage = 'fr'
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

try {
    & (Join-Path $PSScriptRoot 'Assert-ProbePortFree.ps1') -Uri "$baseUri/" -Port $Port

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

    $rootResponse = & (Join-Path $PSScriptRoot 'Wait-ProbeHost.ps1') -Process $process -Uri "$baseUri/" -HostName 'Interactive Auto'
    if ($rootResponse.Content -notmatch 'OmniEurope\.Blazor Interactive Auto' -or $rootResponse.Content -notmatch 'auto-action') {
        throw $psText.AutoMarkersMissing
    }
    $englishResponse = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/" -Headers @{ 'Accept-Language' = 'en' } -TimeoutSec 2
    if ($englishResponse.Content -notmatch '<html lang="en"' -or $englishResponse.Content -notmatch 'Auto counter: 0') {
        throw $psText.AutoNegotiationEnglishInvalid
    }
    $frenchResponse = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/" -Headers @{ 'Accept-Language' = 'fr' } -TimeoutSec 2
    if ($frenchResponse.Content -notmatch '<html lang="fr"' -or $frenchResponse.Content -notmatch 'Compteur Auto : 0') {
        throw $psText.AutoNegotiationFrenchInvalid
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

    $browserPath = & (Join-Path $PSScriptRoot 'Find-ProbeBrowser.ps1')
    $browser = & (Join-Path $PSScriptRoot 'Start-ProbeBrowser.ps1') -BrowserPath $browserPath -Language $BrowserLanguage -StartUrl $baseUri -ProfilePrefix 'omni-auto-cdp-'

    & node (Join-Path $PSScriptRoot 'Test-CdpProbe.mjs') --endpoint "http://127.0.0.1:$($browser.Port)" --selector '#auto-action' --output '#auto-action' --expected 'Compteur Auto : 1'
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
    & (Join-Path $PSScriptRoot 'Stop-ProbeBrowser.ps1') -Browser $browser.Process -BrowserProfile $browser.Profile -RemoveProfile
    Remove-Item -LiteralPath $stdout.FullName, $stderr.FullName -Force -ErrorAction SilentlyContinue
}

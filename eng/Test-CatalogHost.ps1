[CmdletBinding()]
param(
    [int]$Port = 5187,
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

try {
    & (Join-Path $PSScriptRoot 'Assert-ProbePortFree.ps1') -Uri "$baseUri/csp-status" -Port $Port

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
        throw $psText.CatalogNegotiationEnglishInvalid
    }
    $frenchResponse = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/" -Headers @{ 'Accept-Language' = 'fr' } -TimeoutSec $RequestTimeoutSeconds
    if ($frenchResponse.Content -notmatch '<html lang="fr"' -or $frenchResponse.Content -notmatch 'Catalogue des composants') {
        throw $psText.CatalogNegotiationFrenchInvalid
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

    # A browser posts its CSP reports without any antiforgery token: the collector must reach its own checks
    # (media type, size cap) and never answer the antiforgery 400. Neither request below is recorded.
    $oversized = '{"csp-report":{"blocked-uri":"' + ('a' * (16 * 1024)) + '"}}'
    $tooLarge = Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$baseUri/csp-report" -ContentType 'application/csp-report' -Body $oversized -SkipHttpErrorCheck -TimeoutSec $RequestTimeoutSeconds
    if ($tooLarge.StatusCode -ne 413) { throw "Le collecteur CSP ne refuse pas un rapport trop gros sans jeton par 413 : $($tooLarge.StatusCode)" }
    $wrongType = Invoke-WebRequest -UseBasicParsing -Method Post -Uri "$baseUri/csp-report" -ContentType 'text/plain' -Body 'x' -SkipHttpErrorCheck -TimeoutSec $RequestTimeoutSeconds
    if ($wrongType.StatusCode -ne 415) { throw "Le collecteur CSP ne refuse pas un type de contenu inconnu par 415 : $($wrongType.StatusCode)" }

    foreach ($path in @(
        '/_framework/blazor.web.js',
        '/_content/OmniEurope.Blazor/omnieurope.blazor.css',
        '/_content/OmniEurope.Blazor/omniInterop.js'
    )) {
        $asset = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri$path" -TimeoutSec $RequestTimeoutSeconds
        if ($asset.StatusCode -ne 200 -or $asset.RawContentLength -le 0) { throw "Asset catalogue invalide : $path" }
    }

    $browserPath = & (Join-Path $PSScriptRoot 'Find-ProbeBrowser.ps1')
    $browser = & (Join-Path $PSScriptRoot 'Start-ProbeBrowser.ps1') -BrowserPath $browserPath -Language '' -ProfilePrefix 'omni-catalog-cdp-'

    & node (Join-Path $PSScriptRoot 'Test-CatalogProbe.mjs') --endpoint "http://127.0.0.1:$($browser.Port)" --url $baseUri
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
    & (Join-Path $PSScriptRoot 'Stop-ProbeBrowser.ps1') -Browser $browser.Process -BrowserProfile $browser.Profile -RemoveProfile
    Remove-Item -LiteralPath $stdout.FullName, $stderr.FullName -Force -ErrorAction SilentlyContinue
}

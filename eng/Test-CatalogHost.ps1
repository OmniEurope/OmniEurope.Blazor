[CmdletBinding()]
param(
    [int]$Port = 5187,
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\artifacts\catalog-smoke\OmniEurope.Blazor.Catalog.dll')
)

$ErrorActionPreference = 'Stop'
$resolvedAssembly = (Resolve-Path -LiteralPath $AssemblyPath).Path
$contentRoot = Split-Path -Parent $resolvedAssembly
$baseUri = "http://127.0.0.1:$Port"
$stdout = New-TemporaryFile
$stderr = New-TemporaryFile
$process = $null

try {
    $existingHost = $false
    try {
        Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/csp-status" -TimeoutSec 1 -ErrorAction Stop | Out-Null
        $existingHost = $true
    }
    catch {
    }
    if ($existingHost) {
        throw "Le port $Port est déjà occupé par un hôte HTTP ; le test refuse de réutiliser un processus non possédé."
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
            throw "Le catalogue s'est arrêté avant d'être prêt (code $($process.ExitCode))."
        }

        try {
            $homeResponse = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/" -TimeoutSec 2 -ErrorAction Stop
            if ($homeResponse.StatusCode -eq 200) { break }
        }
        catch [System.Net.Http.HttpRequestException] {
        }
        catch [System.Net.WebException] {
        }
        Start-Sleep -Milliseconds 200
    }

    if ($null -eq $homeResponse -or $homeResponse.StatusCode -ne 200) { throw "Le catalogue n'a pas répondu HTTP 200 dans le délai imparti." }
    $csp = ($homeResponse.Headers['Content-Security-Policy'] -join '; ')
    if ([string]::IsNullOrWhiteSpace($csp)) { throw 'En-tête Content-Security-Policy absent.' }
    if ($csp -match "unsafe-inline|unsafe-eval") { throw "La CSP contient une directive interdite : $csp" }

    $status = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/csp-status" -TimeoutSec 2
    if ($status.StatusCode -ne 200 -or $status.Content -notmatch '"status"\s*:\s*"pass"' -or $status.Content -notmatch '"violations"\s*:\s*0') {
        throw "Le collecteur CSP n'est pas vert : $($status.Content)"
    }

    foreach ($path in @(
        '/_framework/blazor.web.js',
        '/_content/OmniEurope.Blazor/omnieurope.blazor.css',
        '/_content/OmniEurope.Blazor/omniInterop.js'
    )) {
        $asset = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri$path" -TimeoutSec 2
        if ($asset.StatusCode -ne 200 -or $asset.RawContentLength -le 0) { throw "Asset catalogue invalide : $path" }
    }

    Write-Host "Catalogue Server validé : HTTP 200, assets, CSP stricte et zéro violation (PID $($process.Id))."
}
catch {
    if (Test-Path -LiteralPath $stdout.FullName) { Get-Content -LiteralPath $stdout.FullName | Write-Host }
    if (Test-Path -LiteralPath $stderr.FullName) { Get-Content -LiteralPath $stderr.FullName | Write-Host }
    throw
}
finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id
        if (-not $process.WaitForExit(5000)) { throw "Le processus catalogue $($process.Id) n'a pas pu être arrêté." }
    }
    Remove-Item -LiteralPath $stdout.FullName, $stderr.FullName -Force -ErrorAction SilentlyContinue
}

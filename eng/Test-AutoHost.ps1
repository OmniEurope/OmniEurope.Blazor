[CmdletBinding()]
param(
    [int]$Port = 5189,
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\artifacts\auto-smoke\OmniEurope.Blazor.AutoSmoke.dll')
)

$ErrorActionPreference = 'Stop'
$resolvedAssembly = (Resolve-Path -LiteralPath $AssemblyPath).Path
$workingDirectory = Split-Path -Parent $resolvedAssembly
$baseUri = "http://127.0.0.1:$Port"
$stdout = New-TemporaryFile
$stderr = New-TemporaryFile
$process = $null

try {
    $existingHost = $false
    try {
        Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/" -TimeoutSec 1 -ErrorAction Stop | Out-Null
        $existingHost = $true
    }
    catch {
    }
    if ($existingHost) {
        throw "Le port $Port est déjà occupé par un hôte HTTP ; le test refuse de réutiliser un processus non possédé."
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
            throw "L'hôte Interactive Auto s'est arrêté avant d'être prêt (code $($process.ExitCode))."
        }

        try {
            $rootResponse = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/" -TimeoutSec 2 -ErrorAction Stop
            if ($rootResponse.StatusCode -eq 200) { break }
        }
        catch {
        }
        Start-Sleep -Milliseconds 200
    }

    if ($null -eq $rootResponse -or $rootResponse.StatusCode -ne 200) { throw "L'hôte Interactive Auto n'a pas répondu HTTP 200 dans le délai imparti." }
    if ($rootResponse.Content -notmatch 'OmniEurope\.Blazor Interactive Auto' -or $rootResponse.Content -notmatch 'auto-action') {
        throw 'Les marqueurs de prérendu Interactive Auto sont absents.'
    }

    $csp = ($rootResponse.Headers['Content-Security-Policy'] -join '; ')
    if ([string]::IsNullOrWhiteSpace($csp)) { throw 'En-tête Content-Security-Policy absent.' }
    if ($csp -match "unsafe-inline|unsafe-eval") { throw "La CSP contient une directive interdite : $csp" }

    foreach ($path in @(
        '/_framework/blazor.web.js',
        '/_content/OmniEurope.Blazor/omnieurope.blazor.css',
        '/_content/OmniEurope.Blazor/omniInterop.js'
    )) {
        $asset = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri$path" -TimeoutSec 2
        if ($asset.StatusCode -ne 200 -or $asset.RawContentLength -le 0) { throw "Asset Interactive Auto invalide : $path" }
    }

    Write-Host "Interactive Auto validé : HTTP 200, prérendu, assets client et CSP stricte (PID $($process.Id))."
}
catch {
    if (Test-Path -LiteralPath $stdout.FullName) { Get-Content -LiteralPath $stdout.FullName | Write-Host }
    if (Test-Path -LiteralPath $stderr.FullName) { Get-Content -LiteralPath $stderr.FullName | Write-Host }
    throw
}
finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id
        if (-not $process.WaitForExit(5000)) { throw "Le processus Interactive Auto $($process.Id) n'a pas pu être arrêté." }
    }
    Remove-Item -LiteralPath $stdout.FullName, $stderr.FullName -Force -ErrorAction SilentlyContinue
}

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PackagePath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$resolvedPackage = (Resolve-Path -LiteralPath $PackagePath).Path
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("omni-package-fixture-" + [guid]::NewGuid().ToString('N'))
$resolvedTempRoot = [IO.Path]::GetFullPath($tempRoot)
$systemTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
if (-not $resolvedTempRoot.StartsWith($systemTemp, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Unsafe package fixture directory.'
}

try {
    $expanded = Join-Path $resolvedTempRoot 'expanded'
    $contaminated = Join-Path $resolvedTempRoot 'contaminated.nupkg'
    [IO.Compression.ZipFile]::ExtractToDirectory($resolvedPackage, $expanded)
    $payload = Join-Path $expanded 'staticwebassets/payload.js'
    [IO.File]::WriteAllText($payload, 'globalThis.forbiddenVendor = "Radzen";', [Text.UTF8Encoding]::new($false))
    [IO.Compression.ZipFile]::CreateFromDirectory($expanded, $contaminated)

    $pwsh = (Get-Process -Id $PID).Path
    $output = & $pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Test-Package.ps1') -PackagePath $contaminated 2>&1
    if ($LASTEXITCODE -eq 0) { throw 'The contaminated package fixture unexpectedly passed.' }
    if (($output -join "`n") -notmatch 'forbidden token') {
        throw "The contaminated package failed for the wrong reason: $($output -join ' | ')"
    }

    Write-Host 'Contaminated package fixture was rejected by payload inspection.'
} finally {
    if (Test-Path -LiteralPath $resolvedTempRoot) {
        Remove-Item -LiteralPath $resolvedTempRoot -Recurse -Force
    }
}

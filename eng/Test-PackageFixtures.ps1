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
    $payload = Join-Path $expanded 'lib/net10.0/payload.pdb'
    [IO.File]::WriteAllBytes($payload, [byte[]](0x42, 0x53, 0x4A, 0x42))
    [IO.Compression.ZipFile]::CreateFromDirectory($expanded, $contaminated)

    $pwsh = (Get-Process -Id $PID).Path
    # The contaminated fixture must be rejected, so this call is expected to fail. Its exit code has
    # to be captured and cleared: a GitHub pwsh step ends with "exit $LASTEXITCODE", so leaving the
    # expected failure behind fails the whole step long after every check has reported success.
    $output = & $pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Test-Package.ps1') -PackagePath $contaminated 2>&1
    $contaminatedExitCode = $LASTEXITCODE
    $global:LASTEXITCODE = 0
    if ($contaminatedExitCode -eq 0) { throw 'The contaminated package fixture unexpectedly passed.' }
    if (($output -join "`n") -notmatch 'unexpected embedded PDB') {
        throw "The contaminated package failed for the wrong reason: $($output -join ' | ')"
    }

    Write-Host 'Contaminated package fixture was rejected by content inspection.'
} finally {
    if (Test-Path -LiteralPath $resolvedTempRoot) {
        Remove-Item -LiteralPath $resolvedTempRoot -Recurse -Force
    }
}

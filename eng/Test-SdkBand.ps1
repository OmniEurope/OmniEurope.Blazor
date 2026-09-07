[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$globalJsonPath = Join-Path $repoRoot 'global.json'
$pinned = [version]((Get-Content -LiteralPath $globalJsonPath -Raw | ConvertFrom-Json).sdk.version)
$actual = [version](dotnet --version)

$pinnedBand = [math]::Floor($pinned.Build / 100)
$actualBand = [math]::Floor($actual.Build / 100)

if ($actual.Major -ne $pinned.Major -or $actual.Minor -ne $pinned.Minor -or $actualBand -ne $pinnedBand) {
    throw "SDK $actual is outside the feature band pinned by global.json ($pinned)."
}

Write-Host "SDK $actual satisfies the feature band pinned by global.json ($pinned)."

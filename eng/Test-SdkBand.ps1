[CmdletBinding()]
param()

# STD-SDKPIN (ADR-002): global.json declares a floor with rollForward latestFeature. The resolved SDK
# must be the same major.minor and at least the floor; any later feature band is accepted, because the
# implicit SDK packs are pinned in Directory.Build.targets and the locks no longer depend on the band.
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$globalJsonPath = Join-Path $repoRoot 'global.json'
$sdk = (Get-Content -LiteralPath $globalJsonPath -Raw | ConvertFrom-Json).sdk
$floor = [version]$sdk.version
$actual = [version](dotnet --version)

if ([string]$sdk.rollForward -ne 'latestFeature') {
    throw "global.json must declare rollForward latestFeature over its SDK floor, found '$($sdk.rollForward)'."
}
if ($actual.Major -ne $floor.Major -or $actual.Minor -ne $floor.Minor -or $actual -lt $floor) {
    throw "SDK $actual is below the floor declared by global.json ($floor) or outside its $($floor.Major).$($floor.Minor) line."
}

Write-Host "SDK $actual satisfies the floor declared by global.json ($floor, latestFeature)."

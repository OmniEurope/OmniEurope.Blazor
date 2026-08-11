[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$messages = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
$registryPath = Join-Path $repoRoot 'docs/third-party-packages.json'
$sbomPath = Join-Path $repoRoot 'docs/sbom.cdx.json'
$noticePath = Join-Path $repoRoot 'NOTICE.md'
foreach ($path in @($registryPath, $sbomPath, $noticePath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing SBOM artifact: $path" }
}

$expected = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$locks = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Filter 'packages.lock.json' |
    Where-Object FullName -NotMatch '[\\/](bin|obj|artifacts|\.git|\.claude)[\\/]')
foreach ($lock in $locks) {
    $document = Get-Content -LiteralPath $lock.FullName -Raw | ConvertFrom-Json
    foreach ($framework in $document.dependencies.PSObject.Properties) {
        foreach ($package in $framework.Value.PSObject.Properties) {
            $version = [string]$package.Value.resolved
            if ($version) { [void]$expected.Add("$($package.Name)|$version") }
        }
    }
}

$registry = Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
if ([int]$registry.schemaVersion -ne 1) { throw 'Unsupported third-party registry schema.' }
$actual = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($package in @($registry.packages)) {
    $key = "$($package.id)|$($package.version)"
    if (-not $actual.Add($key)) { throw "Duplicate package in license registry: $key" }
    if ([string]::IsNullOrWhiteSpace([string]$package.license.kind) -or
        [string]::IsNullOrWhiteSpace([string]$package.license.value)) {
        throw "Missing license classification for $key"
    }
    if (@('expression', 'file', 'url') -notcontains [string]$package.license.kind) {
        throw "Unsupported license classification for $($key): $($package.license.kind)"
    }
    if ($package.license.kind -eq 'file') {
        $localPath = Join-Path $repoRoot ([string]$package.license.localFile)
        if (-not (Test-Path -LiteralPath $localPath -PathType Leaf)) {
            throw "Missing preserved license file for $key"
        }
        $hash = (Get-FileHash -LiteralPath $localPath -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($hash -ne [string]$package.license.localFileSha256) {
            throw "License file hash mismatch for $key"
        }
    }
}
if ($actual.Count -ne $expected.Count) {
    throw "Package registry count mismatch: expected $($expected.Count), found $($actual.Count)."
}
foreach ($key in $expected) {
    if (-not $actual.Contains($key)) { throw "Locked package absent from registry: $key" }
}

$sbom = Get-Content -LiteralPath $sbomPath -Raw | ConvertFrom-Json
if ($sbom.bomFormat -ne 'CycloneDX' -or $sbom.specVersion -ne '1.6') {
    throw 'The SBOM is not CycloneDX 1.6.'
}
if (@($sbom.components).Count -ne $expected.Count) {
    throw "SBOM component count mismatch: expected $($expected.Count), found $(@($sbom.components).Count)."
}
[xml]$packageProject = Get-Content -LiteralPath (Join-Path $repoRoot 'src/OmniEurope.Blazor/OmniEurope.Blazor.csproj') -Raw
$expectedRootName = [string]$packageProject.Project.PropertyGroup.PackageId
$expectedRootVersion = [string]$packageProject.Project.PropertyGroup.Version
if ([string]$sbom.metadata.component.name -cne $expectedRootName -or
    [string]$sbom.metadata.component.version -cne $expectedRootVersion) {
    throw "SBOM root mismatch: expected $expectedRootName $expectedRootVersion."
}
$refs = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$componentKeys = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($component in @($sbom.components)) {
    if (-not $refs.Add([string]$component.'bom-ref')) { throw "Duplicate SBOM reference: $($component.'bom-ref')" }
    if (@($component.licenses).Count -eq 0) { throw "SBOM component has no license: $($component.name)" }
    $key = "$($component.name)|$($component.version)"
    if (-not $componentKeys.Add($key)) { throw "Duplicate SBOM component identity: $key" }
    $expectedPurl = "pkg:nuget/$([Uri]::EscapeDataString([string]$component.name))@$([Uri]::EscapeDataString([string]$component.version))"
    if ([string]$component.'bom-ref' -cne $expectedPurl -or [string]$component.purl -cne $expectedPurl) {
        throw "SBOM purl mismatch for $key"
    }
}
foreach ($package in @($registry.packages)) {
    $key = "$($package.id)|$($package.version)"
    if (-not $componentKeys.Contains($key)) { throw "Registry package absent from SBOM: $key" }
    $component = @($sbom.components | Where-Object { $_.name -ieq $package.id -and $_.version -ieq $package.version })
    if ($component.Count -ne 1) { throw "Expected one SBOM component for $key, found $($component.Count)." }
    $kind = @($component[0].properties | Where-Object name -EQ 'omnieurope:license-kind').value
    if ([string]$kind -cne [string]$package.license.kind) { throw "SBOM license kind mismatch for $key" }
}

$notice = Get-Content -LiteralPath $noticePath -Raw
foreach ($package in @($registry.packages)) {
    if (-not $notice.Contains("``$($package.id)``") -or -not $notice.Contains("``$($package.version)``")) {
        throw "NOTICE does not list $($package.id) $($package.version)."
    }
}
Write-Host ($messages.SbomPassed -f $expected.Count)

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PackagePath,
    [string]$SymbolPackagePath,
    [string]$ExpectedVersion
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Reflection.Metadata

function Read-EntryBytes([System.IO.Compression.ZipArchiveEntry]$Entry) {
    $stream = $Entry.Open()
    $memory = [IO.MemoryStream]::new()
    try {
        $stream.CopyTo($memory)
        return $memory.ToArray()
    } finally {
        $memory.Dispose()
        $stream.Dispose()
    }
}

function Read-EntryText([System.IO.Compression.ZipArchiveEntry]$Entry) {
    $reader = [IO.StreamReader]::new($Entry.Open(), [Text.Encoding]::UTF8, $true)
    try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
}
$resolved = (Resolve-Path -LiteralPath $PackagePath).Path
$archive = [System.IO.Compression.ZipFile]::OpenRead($resolved)
try {
    $entries = @($archive.Entries.FullName)
    $requiredPatterns = @(
        '^OmniEurope\.Blazor\.nuspec$',
        '^README\.md$',
        '^NOTICE\.md$',
        '^compliance/sbom\.cdx\.json$',
        '^compliance/third-party-packages\.json$',
        '^compliance/vendored-assets\.json$',
        '^compliance/licenses/.+$',
        '^lib/net10\.0/OmniEurope\.Blazor\.dll$',
        '^staticwebassets/omnieurope\.blazor\.css$',
        '^staticwebassets/omniInterop\.js$'
    )
    foreach ($pattern in $requiredPatterns) {
        if (-not ($entries -match $pattern)) { throw "Package entry missing: $pattern" }
    }

    # Content files land in every consuming host's wwwroot: the stylesheet ships only as the minified
    # static web asset, and its commented source must never ride along as content.
    $contentEntries = @($entries | Where-Object { $_ -match '^(content|contentFiles)/' })
    if ($contentEntries.Count -gt 0) {
        throw "Package carries content files that would land in every host: $($contentEntries -join ', ')"
    }
    if ((Read-EntryText ($archive.GetEntry('staticwebassets/omnieurope.blazor.css'))).Contains('/*')) {
        throw 'The packaged stylesheet still contains comments: it is not the minified copy.'
    }

    $embeddedRegistry = Read-EntryText ($archive.GetEntry('compliance/third-party-packages.json')) | ConvertFrom-Json
    $expectedLicenseEntries = @($embeddedRegistry.packages |
        Where-Object { $_.license.kind -eq 'file' } |
        ForEach-Object { 'compliance/licenses/' + [IO.Path]::GetFileName([string]$_.license.localFile) } |
        Sort-Object -Unique)
    $embeddedAssets = @()
    $assetsEntry = $archive.GetEntry('compliance/vendored-assets.json')
    if ($assetsEntry) {
        $assetManifest = Read-EntryText $assetsEntry | ConvertFrom-Json
        if ([int]$assetManifest.schemaVersion -ne 1) { throw 'Unsupported vendored asset manifest schema in the package.' }
        $embeddedAssets = @($assetManifest.assets)
    }
    $expectedLicenseEntries = @($expectedLicenseEntries + @($embeddedAssets |
        ForEach-Object { 'compliance/licenses/' + [IO.Path]::GetFileName([string]$_.license.localFile) }) |
        Sort-Object -Unique)
    $actualLicenseEntries = @($entries | Where-Object { $_ -match '^compliance/licenses/[^/]+$' } | Sort-Object -Unique)
    if (Compare-Object $expectedLicenseEntries $actualLicenseEntries) {
        throw 'Packaged license files do not exactly match the embedded third-party registry.'
    }
    foreach ($package in @($embeddedRegistry.packages | Where-Object { $_.license.kind -eq 'file' })) {
        $entryName = 'compliance/licenses/' + [IO.Path]::GetFileName([string]$package.license.localFile)
        $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData((Read-EntryBytes ($archive.GetEntry($entryName))))).ToLowerInvariant()
        if ($hash -cne [string]$package.license.localFileSha256) {
            throw "Packaged license hash mismatch: $entryName"
        }
    }
    foreach ($asset in $embeddedAssets) {
        $entryName = 'compliance/licenses/' + [IO.Path]::GetFileName([string]$asset.license.localFile)
        $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData((Read-EntryBytes ($archive.GetEntry($entryName))))).ToLowerInvariant()
        if ($hash -cne ([string]$asset.license.localFileSha256).ToLowerInvariant()) {
            throw "Packaged license hash mismatch for vendored asset $($asset.id): $entryName"
        }
    }
    if ($entries -match '(?i)\.pdb$') { throw 'Package contains an unexpected embedded PDB.' }

    $nuspecEntry = $archive.GetEntry('OmniEurope.Blazor.nuspec')
    $nuspec = Read-EntryText $nuspecEntry
    $nuspecDocument = [xml]$nuspec
    if ($ExpectedVersion) {
        $normalizedVersion = $ExpectedVersion.TrimStart('v')
        $actualVersion = [string]$nuspecDocument.package.metadata.version
        if ($actualVersion -cne $normalizedVersion) {
            throw "NuGet version mismatch: expected $normalizedVersion, found $actualVersion."
        }
    }
    if ($nuspec -notmatch '<license type="expression">EUPL-1\.2</license>') { throw 'NuGet license expression is missing or incorrect.' }

    $repoRoot = Split-Path -Parent $PSScriptRoot
    [xml]$central = Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Packages.props') -Raw
    [xml]$project = Get-Content -LiteralPath (Join-Path $repoRoot 'src/OmniEurope.Blazor/OmniEurope.Blazor.csproj') -Raw
    $centralVersions = @{}
    foreach ($version in @($central.Project.ItemGroup.PackageVersion)) {
        $centralVersions[[string]$version.Include] = [string]$version.Version
    }
    $expectedDependencies = @{}
    foreach ($reference in @($project.Project.ItemGroup.PackageReference)) {
        $id = [string]$reference.Include
        if (-not $centralVersions.ContainsKey($id)) { throw "Central version missing for package dependency $id." }
        $expectedDependencies[$id] = $centralVersions[$id]
    }
    $actualDependencies = @{}
    foreach ($dependency in @($nuspecDocument.SelectNodes("//*[local-name()='dependency']"))) {
        $id = [string]$dependency.id
        if ($actualDependencies.ContainsKey($id)) { throw "Duplicate nuspec dependency: $id" }
        $actualDependencies[$id] = [string]$dependency.version
    }
    if ($actualDependencies.Count -ne $expectedDependencies.Count) {
        throw "NuGet dependency count mismatch: expected $($expectedDependencies.Count), found $($actualDependencies.Count)."
    }
    foreach ($dependency in $expectedDependencies.GetEnumerator()) {
        if (-not $actualDependencies.ContainsKey($dependency.Key) -or $actualDependencies[$dependency.Key] -cne $dependency.Value) {
            throw "NuGet dependency mismatch for $($dependency.Key): expected $($dependency.Value), found $($actualDependencies[$dependency.Key])."
        }
    }
    if ($nuspec -match 'Microsoft\.AspNetCore\.App|frameworkReference') {
        throw 'NuGet package still declares a server-only framework reference.'
    }
    Write-Host "NuGet content passed: $($entries.Count) entries."
} finally {
    $archive.Dispose()
}

if ($SymbolPackagePath) {
    $resolvedSymbols = (Resolve-Path -LiteralPath $SymbolPackagePath).Path
    $symbols = [System.IO.Compression.ZipFile]::OpenRead($resolvedSymbols)
    try {
        $symbolEntries = @($symbols.Entries.FullName)
        if ($symbolEntries -notcontains 'lib/net10.0/OmniEurope.Blazor.pdb') { throw 'Portable PDB is missing from the symbol package.' }
        Write-Host "NuGet symbols passed: $($symbolEntries.Count) entries."
    } finally {
        $symbols.Dispose()
    }
}

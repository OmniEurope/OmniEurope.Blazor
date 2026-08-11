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

function Assert-NoForbiddenPayloadToken([System.IO.Compression.ZipArchiveEntry]$Entry) {
    $bytes = Read-EntryBytes $Entry
    $utf8 = [Text.Encoding]::UTF8.GetString($bytes)
    $utf16 = [Text.Encoding]::Unicode.GetString($bytes)
    if ($utf8 -match '(?i)radzen' -or $utf16 -match '(?i)radzen') {
        throw "Package payload contains the forbidden token in $($Entry.FullName)."
    }

    if ($Entry.FullName -notmatch '(?i)\.dll$') { return }
    $stream = [IO.MemoryStream]::new($bytes, $false)
    $reader = [Reflection.PortableExecutable.PEReader]::new($stream)
    try {
        if (-not $reader.HasMetadata) { throw "Managed assembly metadata is missing from $($Entry.FullName)." }
        $metadata = [Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($reader)
        $semanticNames = [Collections.Generic.List[string]]::new()
        foreach ($handle in $metadata.AssemblyReferences) {
            $reference = $metadata.GetAssemblyReference($handle)
            $semanticNames.Add($metadata.GetString($reference.Name))
        }
        foreach ($handle in $metadata.TypeReferences) {
            $reference = $metadata.GetTypeReference($handle)
            $semanticNames.Add($metadata.GetString($reference.Namespace))
            $semanticNames.Add($metadata.GetString($reference.Name))
        }
        foreach ($handle in $metadata.TypeDefinitions) {
            $definition = $metadata.GetTypeDefinition($handle)
            $semanticNames.Add($metadata.GetString($definition.Namespace))
            $semanticNames.Add($metadata.GetString($definition.Name))
        }
        foreach ($handle in $metadata.MemberReferences) {
            $semanticNames.Add($metadata.GetString($metadata.GetMemberReference($handle).Name))
        }
        if ($semanticNames -match '(?i)radzen') {
            throw "Managed metadata contains the forbidden token in $($Entry.FullName)."
        }
    } finally {
        $reader.Dispose()
        $stream.Dispose()
    }
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
        '^compliance/licenses/.+$',
        '^lib/net10\.0/OmniEurope\.Blazor\.dll$',
        '^staticwebassets/omnieurope\.blazor\.css$',
        '^staticwebassets/omniInterop\.js$'
    )
    foreach ($pattern in $requiredPatterns) {
        if (-not ($entries -match $pattern)) { throw "Package entry missing: $pattern" }
    }

    $embeddedRegistry = Read-EntryText ($archive.GetEntry('compliance/third-party-packages.json')) | ConvertFrom-Json
    $expectedLicenseEntries = @($embeddedRegistry.packages |
        Where-Object { $_.license.kind -eq 'file' } |
        ForEach-Object { 'compliance/licenses/' + [IO.Path]::GetFileName([string]$_.license.localFile) } |
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
    if ($entries -match '(?i)(Radzen|\.pdb$)') { throw 'Package contains a forbidden Radzen reference or an unexpected embedded PDB.' }
    foreach ($entry in $archive.Entries | Where-Object FullName -Match '(?i)^(lib/.+\.dll|staticwebassets/.+\.(css|js))$') {
        Assert-NoForbiddenPayloadToken $entry
    }

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
        if ($symbolEntries -match '(?i)Radzen') { throw 'Symbol package contains a forbidden Radzen reference.' }
        foreach ($entry in $symbols.Entries | Where-Object FullName -Match '(?i)^lib/.+\.pdb$') {
            Assert-NoForbiddenPayloadToken $entry
        }
        Write-Host "NuGet symbols passed: $($symbolEntries.Count) entries."
    } finally {
        $symbols.Dispose()
    }
}

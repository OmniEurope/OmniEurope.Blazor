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
$repoRoot = Split-Path -Parent $PSScriptRoot
# The package ships one lib folder per target framework of the library project, and only those.
[xml]$project = Get-Content -LiteralPath (Join-Path $repoRoot 'src/OmniEurope.Blazor/OmniEurope.Blazor.csproj') -Raw
$frameworks = @(([string]$project.Project.PropertyGroup.TargetFrameworks).Split(';', [StringSplitOptions]::RemoveEmptyEntries) |
    ForEach-Object { $_.Trim() })
if ($frameworks.Count -lt 2 -or $frameworks -cnotcontains 'net10.0' -or $frameworks -cnotcontains 'net11.0') {
    throw "OmniEurope.Blazor.csproj must target net10.0 and net11.0, found '$($frameworks -join ';')'."
}

# The central version a PackageReference resolves for one target framework: an unconditional row, or the
# '== <tfm>' / '!= <tfm>' pair of Directory.Packages.props (the only conditions Test-DependencyPolicy.ps1 accepts).
function Get-CentralVersion([xml]$Central, [string]$Id, [string]$Framework) {
    $found = @(foreach ($row in @($Central.Project.ItemGroup.PackageVersion | Where-Object { [string]$_.Include -ceq $Id })) {
        $condition = [string]$row.Condition
        if ([string]::IsNullOrWhiteSpace($condition)) { [string]$row.Version }
        elseif ($condition -match "^\s*'\`$\(TargetFramework\)'\s*==\s*'([^']+)'\s*$") { if ($Matches[1] -ceq $Framework) { [string]$row.Version } }
        elseif ($condition -match "^\s*'\`$\(TargetFramework\)'\s*!=\s*'([^']+)'\s*$") { if ($Matches[1] -cne $Framework) { [string]$row.Version } }
        else { throw "Unsupported PackageVersion condition for ${Id}: $condition" }
    })
    if ($found.Count -ne 1) { throw "Central version of $Id for $Framework is ambiguous or missing ($($found.Count) rows)." }
    return $found[0]
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

        '^analyzers/dotnet/cs/OmniEurope\.Blazor\.Analyzers\.dll$',
        '^staticwebassets/omnieurope\.blazor\.css$',
        '^staticwebassets/omniInterop\.js$'
    )
    foreach ($framework in $frameworks) {
        $requiredPatterns += "^lib/$([regex]::Escape($framework))/OmniEurope\.Blazor\.dll$"
        $requiredPatterns += "^lib/$([regex]::Escape($framework))/OmniEurope\.Blazor\.xml$"
    }
    foreach ($pattern in $requiredPatterns) {
        if (-not ($entries -match $pattern)) { throw "Package entry missing: $pattern" }
    }
    $libFolders = @($entries | Where-Object { $_ -match '^lib/([^/]+)/' } | ForEach-Object { ($_ -split '/')[1] } | Sort-Object -Unique)
    if (Compare-Object @($frameworks | Sort-Object) $libFolders) {
        throw "Package lib folders ($($libFolders -join ', ')) differ from the target frameworks ($($frameworks -join ', '))."
    }

    # Every file of the library's wwwroot is a static web asset a host loads by its path (the JS
    # modules, the fonts and their licences): each must ship at the same relative path, so a module
    # added to wwwroot and dropped by packaging fails here instead of in the host's browser.
    $webRoot = Join-Path $repoRoot 'src/OmniEurope.Blazor/wwwroot'
    $expectedAssets = @(Get-ChildItem -LiteralPath $webRoot -Recurse -File |
        ForEach-Object { 'staticwebassets/' + [IO.Path]::GetRelativePath($webRoot, $_.FullName).Replace('\', '/') } |
        Sort-Object -Unique)
    if ($expectedAssets.Count -eq 0) { throw "No file found under $webRoot." }
    $missingAssets = @($expectedAssets | Where-Object { $entries -cnotcontains $_ })
    if ($missingAssets.Count -gt 0) {
        throw "Package is missing static web assets: $($missingAssets -join ', ')"
    }

    # Every culture a resource file of the library is translated into ships as a satellite assembly.
    # The list is read from the resx files present, so a new translation is checked without editing
    # this script.
    $resourceRoot = Join-Path $repoRoot 'src/OmniEurope.Blazor/Resources'
    $cultures = @(Get-ChildItem -LiteralPath $resourceRoot -Filter '*.resx' -File |
        ForEach-Object { if ($_.Name -match '^[^.]+\.(?<culture>[a-z]{2,3}(?:-[A-Za-z0-9]+)*)\.resx$') { $Matches.culture } } |
        Sort-Object -Unique)
    if ($cultures.Count -eq 0) { throw "No translated resource file found under $resourceRoot." }
    $missingSatellites = @($cultures |
        ForEach-Object { $culture = $_; $frameworks | ForEach-Object { "lib/$_/$culture/OmniEurope.Blazor.resources.dll" } } |
        Where-Object { $entries -cnotcontains $_ })
    if ($missingSatellites.Count -gt 0) {
        throw "Package is missing satellite resource assemblies: $($missingSatellites -join ', ')"
    }

    # The analyzer (OE0001) runs inside the consumer's compiler, which provides Roslyn: the package
    # carries that one assembly and nothing else under analyzers/, never a copy of the compiler.
    $analyzerEntries = @($entries | Where-Object { $_ -match '^analyzers/' -and $_ -notmatch '/$' })
    $unexpectedAnalyzers = @($analyzerEntries | Where-Object { $_ -cne 'analyzers/dotnet/cs/OmniEurope.Blazor.Analyzers.dll' })
    if ($unexpectedAnalyzers.Count -gt 0) {
        throw "Package carries unexpected analyzer entries: $($unexpectedAnalyzers -join ', ')"
    }

    # The consumer's compiler refuses an analyzer that references a newer Roslyn (CS9057) and the rule
    # silently stops running: the shipped analyzer must reference the Roslyn of the oldest supported SDK.
    $analyzerEntry = $archive.GetEntry('analyzers/dotnet/cs/OmniEurope.Blazor.Analyzers.dll')
    if ($null -ne $analyzerEntry) {
        $analyzerBytes = [IO.MemoryStream]::new()
        $analyzerStream = $analyzerEntry.Open()
        try { $analyzerStream.CopyTo($analyzerBytes) } finally { $analyzerStream.Dispose() }
        $analyzerBytes.Position = 0
        $peReader = [System.Reflection.PortableExecutable.PEReader]::new($analyzerBytes)
        try {
            $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($peReader)
            $maximumRoslyn = [version]'5.0.0.0'
            $tooNew = @($metadata.AssemblyReferences |
                ForEach-Object { $metadata.GetAssemblyReference($_) } |
                Where-Object { $metadata.GetString($_.Name) -like 'Microsoft.CodeAnalysis*' -and $_.Version -gt $maximumRoslyn } |
                ForEach-Object { "$($metadata.GetString($_.Name)) $($_.Version)" })
            if ($tooNew.Count -gt 0) {
                throw "The shipped analyzer references a Roslyn newer than $maximumRoslyn (SDK 10.0.100), which an older .NET 10 SDK refuses to load: $($tooNew -join ', ')"
            }
        } finally {
            $peReader.Dispose()
        }
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

    [xml]$central = Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Packages.props') -Raw
    # One dependency group per target framework, each at the central version of that framework (a .NET 10
    # consumer must get the 10.0 framework packages, never 11.0).
    $groups = @($nuspecDocument.SelectNodes("//*[local-name()='dependencies']/*[local-name()='group']"))
    $groupFrameworks = @($groups | ForEach-Object { [string]$_.targetFramework } | Sort-Object)
    $expectedGroups = @($frameworks | Sort-Object)
    if ($groups.Count -ne $frameworks.Count -or (Compare-Object $expectedGroups $groupFrameworks)) {
        throw "NuGet dependency groups ($($groupFrameworks -join ', ')) differ from the target frameworks ($($frameworks -join ', '))."
    }
    if (@($nuspecDocument.SelectNodes("//*[local-name()='dependencies']/*[local-name()='dependency']")).Count -gt 0) {
        throw 'NuGet package declares dependencies outside a target framework group.'
    }
    $dependencySummary = @()
    foreach ($framework in $frameworks) {
        $group = @($groups | Where-Object { [string]$_.targetFramework -ceq $framework })[0]
        $expectedDependencies = @{}
        foreach ($reference in @($project.Project.ItemGroup.PackageReference)) {
            $id = [string]$reference.Include
            $expectedDependencies[$id] = Get-CentralVersion $central $id $framework
        }
        $actualDependencies = @{}
        foreach ($dependency in @($group.SelectNodes("*[local-name()='dependency']"))) {
            $id = [string]$dependency.id
            if ($actualDependencies.ContainsKey($id)) { throw "Duplicate nuspec dependency for ${framework}: $id" }
            $actualDependencies[$id] = [string]$dependency.version
        }
        if ($actualDependencies.Count -ne $expectedDependencies.Count) {
            throw "NuGet dependency count mismatch for ${framework}: expected $($expectedDependencies.Count), found $($actualDependencies.Count)."
        }
        foreach ($dependency in $expectedDependencies.GetEnumerator()) {
            if (-not $actualDependencies.ContainsKey($dependency.Key) -or $actualDependencies[$dependency.Key] -cne $dependency.Value) {
                throw "NuGet dependency mismatch for $($dependency.Key) on ${framework}: expected $($dependency.Value), found $($actualDependencies[$dependency.Key])."
            }
        }
        $dependencySummary += "$framework ($(@($expectedDependencies.GetEnumerator() | Sort-Object Key | ForEach-Object { "$($_.Key) $($_.Value)" }) -join ', '))"
    }
    if ($nuspec -match 'Microsoft\.AspNetCore\.App|frameworkReference') {
        throw 'NuGet package still declares a server-only framework reference.'
    }
    Write-Host "NuGet content passed: $($entries.Count) entries, lib $($frameworks -join ' + '), $($expectedAssets.Count) static web assets, $($cultures.Count) satellite cultures per framework ($($cultures -join ', '))."
    Write-Host "NuGet dependencies passed: $($dependencySummary -join '; ')."
} finally {
    $archive.Dispose()
}

if ($SymbolPackagePath) {
    $resolvedSymbols = (Resolve-Path -LiteralPath $SymbolPackagePath).Path
    $symbols = [System.IO.Compression.ZipFile]::OpenRead($resolvedSymbols)
    try {
        $symbolEntries = @($symbols.Entries.FullName)
        foreach ($framework in $frameworks) {
            if ($symbolEntries -notcontains "lib/$framework/OmniEurope.Blazor.pdb") { throw "Portable PDB of $framework is missing from the symbol package." }
        }
        Write-Host "NuGet symbols passed: $($symbolEntries.Count) entries."
    } finally {
        $symbols.Dispose()
    }
}

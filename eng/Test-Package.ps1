[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PackagePath,
    [string]$SymbolPackagePath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$resolved = (Resolve-Path -LiteralPath $PackagePath).Path
$archive = [System.IO.Compression.ZipFile]::OpenRead($resolved)
try {
    $entries = @($archive.Entries.FullName)
    $requiredPatterns = @(
        '^OmniEurope\.Blazor\.nuspec$',
        '^README\.md$',
        '^lib/net10\.0/OmniEurope\.Blazor\.dll$',
        '^staticwebassets/omnieurope\.blazor\.css$',
        '^staticwebassets/omniInterop\.js$'
    )
    foreach ($pattern in $requiredPatterns) {
        if (-not ($entries -match $pattern)) { throw "Package entry missing: $pattern" }
    }
    if ($entries -match '(?i)(Radzen|\.pdb$)') { throw 'Package contains a forbidden Radzen reference or an unexpected embedded PDB.' }

    $nuspecEntry = $archive.GetEntry('OmniEurope.Blazor.nuspec')
    $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
    try { $nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
    if ($nuspec -notmatch '<license type="expression">EUPL-1\.2</license>') { throw 'NuGet license expression is missing or incorrect.' }
    if ($nuspec -notmatch '<dependency id="Microsoft\.AspNetCore\.Components\.Web" version="10\.0\.10"') {
        throw 'NuGet dependency on Microsoft.AspNetCore.Components.Web 10.0.10 is missing.'
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
        Write-Host "NuGet symbols passed: $($symbolEntries.Count) entries."
    } finally {
        $symbols.Dispose()
    }
}

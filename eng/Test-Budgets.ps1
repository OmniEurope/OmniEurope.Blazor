[CmdletBinding()]
param(
    [string]$ShippedCssPath = (Join-Path $PSScriptRoot '..\src\OmniEurope.Blazor\obj\Release\net11.0\omni-stylesheet\omnieurope.blazor.css'),
    # One assembly per target framework of the package (lib/net10.0 and lib/net11.0), each under the same budget.
    [string[]]$AssemblyPath = @(
        (Join-Path $PSScriptRoot '..\src\OmniEurope.Blazor\bin\Release\net10.0\OmniEurope.Blazor.dll'),
        (Join-Path $PSScriptRoot '..\src\OmniEurope.Blazor\bin\Release\net11.0\OmniEurope.Blazor.dll')
    ),
    [string]$PackagePath
)

$ErrorActionPreference = 'Stop'

# The stylesheet carries no size budget: the owner removed it on 2026-09-18. What is still checked is
# that the copy a host downloads is the minified one; given a package, it is read from inside it.
function Read-Bytes([string]$Path, [string]$Name) {
    if (-not (Test-Path -LiteralPath $Path)) { throw "$Name not found at $Path. Build the library in Release first." }
    return [IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Path).Path)
}

$shippedSource = $ShippedCssPath
$packageFrameworks = 0
if ($PackagePath) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $PackagePath).Path)
    try {
        $packageFrameworks = @($archive.Entries | Where-Object { $_.FullName -match '^lib/[^/]+/OmniEurope\.Blazor\.dll$' }).Count
        if ($packageFrameworks -eq 0) { throw "The package contains no lib/<framework>/OmniEurope.Blazor.dll." }
        $entry = $archive.GetEntry('staticwebassets/omnieurope.blazor.css')
        if (-not $entry) { throw "The package does not contain staticwebassets/omnieurope.blazor.css." }
        $stream = $entry.Open()
        $copy = [IO.MemoryStream]::new()
        $stream.CopyTo($copy)
        $stream.Dispose()
        $shippedBytes = $copy.ToArray()
        $shippedSource = "$PackagePath!staticwebassets/omnieurope.blazor.css"
    }
    finally { $archive.Dispose() }
}
else {
    $shippedBytes = Read-Bytes $ShippedCssPath 'The minified stylesheet'
}

if ([Text.Encoding]::UTF8.GetString($shippedBytes).Contains('/*')) {
    throw "The shipped stylesheet still contains comments ($shippedSource): it is not the minified copy."
}

$budgets = @($AssemblyPath | ForEach-Object {
    @{ Name = "Assembly ($(Split-Path -Leaf (Split-Path -Parent $_)))"; Length = (Read-Bytes $_ 'The library assembly').Length; Maximum = 3MB }
})
# The owner raised the package budget to 3 MiB on 2026-10-23, the ceiling of the assembly: 1.9.0 reached
# 2.01 MiB through real growth (assembly, XML documentation, script modules), with nothing parasitic in it.
# Since 2026-11-11 (owner decision, net10.0 + net11.0) the package holds one assembly per target framework,
# so its budget is 3 MiB per framework it ships; each assembly keeps its own 3 MiB budget above.
if ($PackagePath) { $budgets += @{ Name = "NuGet ($packageFrameworks frameworks)"; Length = (Get-Item -LiteralPath $PackagePath).Length; Maximum = 3MB * $packageFrameworks } }

foreach ($budget in $budgets) {
    if ($budget.Length -gt $budget.Maximum) { throw "$($budget.Name) exceeds its budget: $($budget.Length) > $($budget.Maximum) bytes." }
    Write-Host "$($budget.Name): $($budget.Length) / $($budget.Maximum) bytes."
}
Write-Host 'Artifact budgets passed.'

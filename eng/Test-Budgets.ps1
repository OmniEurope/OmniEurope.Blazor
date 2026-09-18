[CmdletBinding()]
param(
    [string]$ShippedCssPath = (Join-Path $PSScriptRoot '..\src\OmniEurope.Blazor\obj\Release\net10.0\omni-stylesheet\omnieurope.blazor.css'),
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\src\OmniEurope.Blazor\bin\Release\net10.0\OmniEurope.Blazor.dll'),
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
if ($PackagePath) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $PackagePath).Path)
    try {
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

$budgets = @(
    @{ Name = 'Assembly'; Length = (Read-Bytes $AssemblyPath 'The library assembly').Length; Maximum = 2MB }
)
if ($PackagePath) { $budgets += @{ Name = 'NuGet'; Length = (Get-Item -LiteralPath $PackagePath).Length; Maximum = 2MB } }

foreach ($budget in $budgets) {
    if ($budget.Length -gt $budget.Maximum) { throw "$($budget.Name) exceeds its budget: $($budget.Length) > $($budget.Maximum) bytes." }
    Write-Host "$($budget.Name): $($budget.Length) / $($budget.Maximum) bytes."
}
Write-Host 'Artifact budgets passed.'

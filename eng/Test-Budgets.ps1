[CmdletBinding()]
param(
    [string]$CssPath = (Join-Path $PSScriptRoot '..\src\OmniEurope.Blazor\wwwroot\omnieurope.blazor.css'),
    [string]$ShippedCssPath = (Join-Path $PSScriptRoot '..\src\OmniEurope.Blazor\obj\Release\net10.0\omni-stylesheet\omnieurope.blazor.css'),
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\src\OmniEurope.Blazor\bin\Release\net10.0\OmniEurope.Blazor.dll'),
    [string]$PackagePath
)

$ErrorActionPreference = 'Stop'

# The source budget keeps comments affordable: they explain the rules and cost a host nothing.
# What a host downloads is budgeted on the minified copy, raw and as brotli, since that is the
# stylesheet the package ships. Given a package, the shipped copy is read from inside it.
function Get-BrotliLength([byte[]]$Bytes) {
    $buffer = [IO.MemoryStream]::new()
    $brotli = [IO.Compression.BrotliStream]::new($buffer, [IO.Compression.CompressionLevel]::Optimal)
    $brotli.Write($Bytes, 0, $Bytes.Length)
    $brotli.Dispose()
    return $buffer.ToArray().Length
}

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

$sourceBytes = Read-Bytes $CssPath 'The stylesheet source'
if ([Text.Encoding]::UTF8.GetString($shippedBytes).Contains('/*')) {
    throw "The shipped stylesheet still contains comments ($shippedSource): it is not the minified copy."
}

$budgets = @(
    @{ Name = 'CSS source'; Length = $sourceBytes.Length; Maximum = 128KB },
    @{ Name = 'CSS shipped'; Length = $shippedBytes.Length; Maximum = 96KB },
    @{ Name = 'CSS shipped (brotli)'; Length = (Get-BrotliLength $shippedBytes); Maximum = 24KB },
    @{ Name = 'Assembly'; Length = (Read-Bytes $AssemblyPath 'The library assembly').Length; Maximum = 1536KB }
)
if ($PackagePath) { $budgets += @{ Name = 'NuGet'; Length = (Get-Item -LiteralPath $PackagePath).Length; Maximum = 2MB } }

foreach ($budget in $budgets) {
    if ($budget.Length -gt $budget.Maximum) { throw "$($budget.Name) exceeds its budget: $($budget.Length) > $($budget.Maximum) bytes." }
    Write-Host "$($budget.Name): $($budget.Length) / $($budget.Maximum) bytes."
}
Write-Host 'Artifact budgets passed.'

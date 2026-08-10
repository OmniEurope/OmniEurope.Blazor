[CmdletBinding()]
param(
    [string]$CssPath = (Join-Path $PSScriptRoot '..\src\OmniEurope.Blazor\wwwroot\omnieurope.blazor.css'),
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\src\OmniEurope.Blazor\bin\Release\net10.0\OmniEurope.Blazor.dll'),
    [string]$PackagePath
)

$ErrorActionPreference = 'Stop'
$budgets = @(
    @{ Name = 'CSS'; Path = $CssPath; Maximum = 96KB },
    @{ Name = 'Assembly'; Path = $AssemblyPath; Maximum = 1536KB }
)
if ($PackagePath) { $budgets += @{ Name = 'NuGet'; Path = $PackagePath; Maximum = 2MB } }

foreach ($budget in $budgets) {
    $file = Get-Item -LiteralPath $budget.Path
    if ($file.Length -gt $budget.Maximum) { throw "$($budget.Name) exceeds its budget: $($file.Length) > $($budget.Maximum) bytes." }
    Write-Host "$($budget.Name): $($file.Length) / $($budget.Maximum) bytes."
}
Write-Host 'Artifact budgets passed.'

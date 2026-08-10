[CmdletBinding()]
param(
    [switch]$Update,
    [string]$SourceRoot = (Join-Path $PSScriptRoot '..\src\OmniEurope.Blazor'),
    [string]$BaselinePath = (Join-Path $PSScriptRoot '..\docs\public-api.txt')
)

$ErrorActionPreference = 'Stop'
$signatures = [System.Collections.Generic.List[string]]::new()
$files = Get-ChildItem -LiteralPath $SourceRoot -Recurse -File | Where-Object { $_.Extension -in '.razor', '.cs' -and $_.FullName -notmatch '(\\|/)(bin|obj)(\\|/)' }

foreach ($file in $files) {
    $content = [string](Get-Content -Raw -LiteralPath $file.FullName)
    $component = if ($file.Extension -eq '.razor') { $file.BaseName } else { $null }
    if ($component) {
        $signatures.Add("component $component")
    }

    $owner = [System.IO.Path]::GetFileNameWithoutExtension($file.BaseName)
    foreach ($match in [regex]::Matches($content, '(?s)\[Parameter(?:\([^\]]*\))?\]\s*(?:\[[^\]]+\]\s*)*public\s+([^\s]+)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\{')) {
        $signatures.Add("parameter $owner.$($match.Groups[2].Value) : $($match.Groups[1].Value)")
    }

    foreach ($match in [regex]::Matches($content, '(?m)^public\s+(?:sealed\s+|abstract\s+|static\s+|partial\s+)*(enum|class|record)\s+([A-Za-z_][A-Za-z0-9_]*)')) {
        $signatures.Add("type $($match.Groups[2].Value) ($($match.Groups[1].Value))")
    }
}

$current = @($signatures | Sort-Object -Unique)
if ($Update) {
    $current | Set-Content -LiteralPath $BaselinePath -Encoding utf8
    Write-Host "Public API baseline updated: $($current.Count) signatures."
    exit 0
}

$expected = @(Get-Content -LiteralPath $BaselinePath)
$difference = Compare-Object -ReferenceObject $expected -DifferenceObject $current
if ($difference) {
    $difference | Format-Table -AutoSize | Out-String | Write-Error
    exit 1
}

Write-Host "Public API baseline passed: $($current.Count) signatures."

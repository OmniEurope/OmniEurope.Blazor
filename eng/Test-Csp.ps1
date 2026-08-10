[CmdletBinding()]
param(
    [string]$SourceRoot = (Join-Path $PSScriptRoot '..\src\OmniEurope.Blazor')
)

$ErrorActionPreference = 'Stop'
$resolvedRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
$sourceFiles = Get-ChildItem -LiteralPath $resolvedRoot -Recurse -File |
    Where-Object { $_.Extension -in '.razor', '.cs', '.js' }

$forbidden = @(
    @{ Name = 'inline style attribute'; Pattern = '(?i)\bstyle\s*=' },
    @{ Name = 'runtime style element'; Pattern = '(?i)<style\b|createElement\s*\(\s*["'']style["'']' },
    @{ Name = 'dynamic JavaScript evaluation'; Pattern = '(?i)\beval\s*\(|\bnew\s+Function\s*\(' }
)

$violations = foreach ($file in $sourceFiles) {
    $content = Get-Content -Raw -LiteralPath $file.FullName
    foreach ($rule in $forbidden) {
        if ($content -match $rule.Pattern) {
            [pscustomobject]@{
                Rule = $rule.Name
                File = $file.FullName.Substring($resolvedRoot.Length + 1)
            }
        }
    }
}

if ($violations) {
    $violations | Format-Table -AutoSize | Out-String | Write-Error
    exit 1
}

Write-Host "CSP source scan passed: $($sourceFiles.Count) files checked."


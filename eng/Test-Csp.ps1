[CmdletBinding()]
param(
    [string[]]$SourceRoots = @(
        (Join-Path $PSScriptRoot '..\src\OmniEurope.Blazor'),
        (Join-Path $PSScriptRoot '..\samples\OmniEurope.Blazor.AutoSmoke'),
        (Join-Path $PSScriptRoot '..\samples\OmniEurope.Blazor.AutoSmoke.Client'),
        (Join-Path $PSScriptRoot '..\samples\OmniEurope.Blazor.Catalog'),
        (Join-Path $PSScriptRoot '..\samples\OmniEurope.Blazor.HybridSmoke'),
        (Join-Path $PSScriptRoot '..\samples\OmniEurope.Blazor.WasmSmoke')
    )
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sourceFiles = $SourceRoots | ForEach-Object { Get-ChildItem -LiteralPath (Resolve-Path -LiteralPath $_).Path -Recurse -File } |
    Where-Object {
        $_.Extension -in '.razor', '.cs', '.js', '.html' -and
        $_.FullName -notmatch '(\\|/)(bin|obj)(\\|/)'
    }

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
                File = [System.IO.Path]::GetRelativePath($repositoryRoot, $file.FullName)
            }
        }
    }
}

if ($violations) {
    $violations | Format-Table -AutoSize | Out-String | Write-Error
    exit 1
}

Write-Host "CSP source scan passed: $($sourceFiles.Count) files checked."

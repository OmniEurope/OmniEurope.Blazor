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
        $_.Extension -in '.razor', '.cs', '.js', '.html', '.css' -and
        $_.FullName -notmatch '(\\|/)(bin|obj)(\\|/)'
    }

$forbidden = @(
    @{ Name = 'inline style attribute'; Pattern = '(?i)\bstyle\s*=' },
    @{ Name = 'runtime style element'; Pattern = '(?i)<style\b|createElement\s*\(\s*["'']style["'']' },
    @{ Name = 'inline HTML event handler'; Pattern = '(?-i)(?<!@)\bon[a-z][a-z0-9_-]*\s*=' },
    @{ Name = 'javascript URI'; Pattern = '(?i)(?:href|src)\s*=\s*["'']?\s*javascript\s*:' },
    @{ Name = 'remote static resource'; Pattern = '(?i)<(?:script|link|img|iframe)\b[^>]*(?:src|href)\s*=\s*["'']\s*https?://' },
    @{ Name = 'remote module import'; Pattern = '(?im)^\s*import(?:\s+[^;]+?\s+from\s+|\s*\()\s*["'']https?://' },
    @{ Name = 'remote CSS import'; Pattern = '(?im)@import\s+(?:url\s*\()?\s*["'']?https?://' },
    @{ Name = 'remote CSS URL'; Pattern = '(?im)url\s*\(\s*["'']?https?://' },
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

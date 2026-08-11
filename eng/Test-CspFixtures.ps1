[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$scanner = Join-Path $PSScriptRoot 'Test-Csp.ps1'
$safeRoot = Join-Path $PSScriptRoot 'fixtures\csp\safe'
$unsafeRoot = Join-Path $PSScriptRoot 'fixtures\csp\unsafe'

& $scanner -SourceRoots $safeRoot
if (-not $?) {
    throw 'The CSP scanner rejected the safe fixture.'
}

& pwsh -NoProfile -File $scanner -SourceRoots $unsafeRoot *> $null
if ($LASTEXITCODE -eq 0) {
    throw 'The CSP scanner accepted the unsafe fixture.'
}

Write-Host 'CSP scanner fixtures passed: safe accepted, unsafe rejected.'

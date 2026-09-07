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

# The unsafe fixture must be rejected, so this call is expected to fail. Its exit code has to be
# captured and cleared: a GitHub pwsh step ends with "exit $LASTEXITCODE", so leaving the expected
# failure behind fails the whole step long after this script has reported success.
& pwsh -NoProfile -File $scanner -SourceRoots $unsafeRoot *> $null
$unsafeExitCode = $LASTEXITCODE
$global:LASTEXITCODE = 0
if ($unsafeExitCode -eq 0) {
    throw 'The CSP scanner accepted the unsafe fixture.'
}

Write-Host 'CSP scanner fixtures passed: safe accepted, unsafe rejected.'

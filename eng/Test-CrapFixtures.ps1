[CmdletBinding()]
param()

# Proves the CRAP gate both ways (PLAN-014): the pass fixture is accepted, generated code and an admitted
# async method included; every other fixture holds exactly one fault the gate must reject.

$ErrorActionPreference = 'Stop'
$gate = Join-Path $PSScriptRoot 'Test-Crap.ps1'
$root = Join-Path $PSScriptRoot 'fixtures\crap'

function Invoke-Gate([string]$name) {
    $fixture = Join-Path $root $name
    & pwsh -NoProfile -File $gate -CoverageRoot $fixture -ExceptionsPath (Join-Path $fixture 'exceptions.json') *> $null
    $code = $LASTEXITCODE
    # An expected failure must not leave its exit code to the CI step that runs this script.
    $global:LASTEXITCODE = 0
    return $code
}

if ((Invoke-Gate 'pass') -ne 0) {
    throw 'The CRAP gate rejected the pass fixture.'
}

foreach ($fault in 'unadmitted', 'stale', 'grown', 'loose', 'unjustified') {
    if ((Invoke-Gate $fault) -eq 0) {
        throw "The CRAP gate accepted the $fault fixture."
    }
}

Write-Host 'CRAP gate fixtures passed: pass accepted; unadmitted, stale, grown, loose and unjustified rejected.'

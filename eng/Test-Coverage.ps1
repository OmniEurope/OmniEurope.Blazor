[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CoverageRoot,
    [int]$MinimumTests = 180,
    [ValidateRange(0, 1)][double]$MinimumLineRate = 0.85,
    [ValidateRange(0, 1)][double]$MinimumBranchRate = 0.65
)

$ErrorActionPreference = 'Stop'
$messages = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
$resolvedRoot = Resolve-Path -LiteralPath $CoverageRoot
$reports = @(Get-ChildItem -LiteralPath $resolvedRoot -Recurse -File -Filter 'coverage.cobertura.xml' |
    Where-Object FullName -NotMatch '[\\/](In|Out)[\\/]')
if ($reports.Count -ne 1) {
    throw "Expected exactly one coverage.cobertura.xml, found $($reports.Count)."
}

[xml]$coverage = Get-Content -LiteralPath $reports[0].FullName -Raw
$root = $coverage.coverage
if ($null -eq $root) {
    throw 'The coverage report has no Cobertura coverage root.'
}

$tests = @(Get-ChildItem -LiteralPath $resolvedRoot -Recurse -File -Filter '*.trx')
if ($tests.Count -ne 1) {
    throw "Expected exactly one TRX report, found $($tests.Count)."
}
[xml]$trx = Get-Content -LiteralPath $tests[0].FullName -Raw
$counters = $trx.TestRun.ResultSummary.Counters
$executed = [int]$counters.executed
$passed = [int]$counters.passed
if ($executed -lt $MinimumTests -or $passed -ne $executed) {
    throw "Expected at least $MinimumTests passing tests, found $passed/$executed."
}

$linesValid = [int]$root.'lines-valid'
$linesCovered = [int]$root.'lines-covered'
$lineRate = [double]::Parse([string]$root.'line-rate', [Globalization.CultureInfo]::InvariantCulture)
$branchRate = [double]::Parse([string]$root.'branch-rate', [Globalization.CultureInfo]::InvariantCulture)
if ($linesValid -le 0 -or $linesCovered -le 0 -or $lineRate -lt $MinimumLineRate -or $lineRate -gt 1) {
    throw "Coverage is not usable: valid=$linesValid, covered=$linesCovered, rate=$lineRate."
}
if ($branchRate -lt $MinimumBranchRate -or $branchRate -gt 1) {
    throw "Branch coverage is below the required floor: rate=$branchRate, minimum=$MinimumBranchRate."
}

$classes = @($root.packages.package.classes.class)
if ($classes.Count -eq 0) {
    throw 'The coverage report contains no classes.'
}
foreach ($class in $classes) {
    if ([string]::IsNullOrWhiteSpace([string]$class.filename)) {
        throw 'A covered class has no source filename.'
    }
}

$ratePercent = [Math]::Round($lineRate * 100, 2)
$branchPercent = [Math]::Round($branchRate * 100, 2)
Write-Host (($messages.CoveragePassed -f $passed, $linesValid, $ratePercent) + " Branches: $branchPercent%.")

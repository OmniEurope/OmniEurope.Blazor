[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CoverageRoot,
    [int]$MinimumTests = 180,
    [ValidateRange(0, 1)][double]$MinimumLineRate = 0.85,
    [ValidateRange(0, 1)][double]$MinimumBranchRate = 0.65,
    # Zero blind spot (PLAN-014): a source file absent from both lists must have every line and every
    # branch run; a file of the baseline may only shrink its gaps, a file of the exceptions keeps the
    # gaps its reason admits.
    [string]$BaselinePath = (Join-Path $PSScriptRoot 'coverage-baseline.json'),
    [string]$ExceptionsPath = (Join-Path $PSScriptRoot 'coverage-exceptions.json'),
    # Rewrites the baseline with the measured gaps, refusing any file whose gaps grew.
    [switch]$UpdateBaseline
)

$ErrorActionPreference = 'Stop'
$messages = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
$resolvedRoot = Resolve-Path -LiteralPath $CoverageRoot
# coverlet.MTP timestamps its report (coverage.cobertura.<yyMMddHHmmssfff>.xml), so the name is matched
# by pattern. Exactly one report is still required: two would mean two runs landed in the same folder.
$reports = @(Get-ChildItem -LiteralPath $resolvedRoot -Recurse -File -Filter 'coverage.cobertura*.xml' |
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

# Gaps per source file: lines never run, and branch outcomes never taken. A file can hold several
# classes (state machines, closures, generic instantiations): a line counts once, with its best reading.
$fileLines = @{}
foreach ($class in $classes) {
    $file = ([string]$class.filename).Replace('\', '/')
    if (-not $fileLines.ContainsKey($file)) { $fileLines[$file] = @{} }
    foreach ($line in @($class.lines.line)) {
        if ($null -eq $line) { continue }
        $number = [int]$line.number
        $hits = [int]$line.hits
        $missedBranches = 0
        if ([string]$line.'condition-coverage' -match '\((?<taken>\d+)/(?<total>\d+)\)') {
            $missedBranches = [int]$Matches.total - [int]$Matches.taken
        }
        $known = $fileLines[$file][$number]
        if ($null -eq $known -or $hits -gt $known.Hits -or $missedBranches -lt $known.Branches) {
            $fileLines[$file][$number] = @{
                Hits     = [Math]::Max($hits, ($known.Hits ?? 0))
                Branches = if ($null -eq $known) { $missedBranches } else { [Math]::Min($missedBranches, $known.Branches) }
            }
        }
    }
}

$gaps = [ordered]@{}
foreach ($file in $fileLines.Keys | Sort-Object) {
    $lines = @($fileLines[$file].Values)
    $missedLines = @($lines | Where-Object { $_.Hits -eq 0 }).Count
    $missedBranches = ($lines | Measure-Object -Property Branches -Sum).Sum
    if ($missedLines -gt 0 -or $missedBranches -gt 0) {
        $gaps[$file] = [ordered]@{ lines = $missedLines; branches = [int]$missedBranches }
    }
}

function Read-GapList([string]$path, [string]$property) {
    $map = @{}
    if (Test-Path -LiteralPath $path) {
        foreach ($entry in @((Get-Content -LiteralPath $path -Raw | ConvertFrom-Json).$property)) {
            if ($null -ne $entry) { $map[[string]$entry.file] = $entry }
        }
    }
    return $map
}

$baseline = Read-GapList $BaselinePath 'files'
$admitted = Read-GapList $ExceptionsPath 'exceptions'
$gapFailures = [Collections.Generic.List[string]]::new()
foreach ($file in $admitted.Keys) {
    if ([string]::IsNullOrWhiteSpace([string]$admitted[$file].reason)) {
        $gapFailures.Add("$file is admitted without a reason.")
    }
}

# Without a baseline file yet, an update seeds it with every gap measured today.
$seeding = $UpdateBaseline -and -not (Test-Path -LiteralPath $BaselinePath)
foreach ($file in $gaps.Keys) {
    $gap = $gaps[$file]
    $allowed = $admitted[$file] ?? $baseline[$file]
    if ($null -eq $allowed) {
        if ($seeding) { continue }
        $gapFailures.Add("$file has $($gap.lines) line(s) never run and $($gap.branches) branch(es) never taken: cover them.")
    }
    elseif ($gap.lines -gt [int]$allowed.lines -or $gap.branches -gt [int]$allowed.branches) {
        $gapFailures.Add("$file has $($gap.lines) line(s) and $($gap.branches) branch(es) uncovered, more than the $($allowed.lines) and $($allowed.branches) it is allowed.")
    }
}

if ($UpdateBaseline) {
    if ($gapFailures.Count -gt 0) {
        throw ("The baseline cannot absorb a growing gap:`n  " + ($gapFailures -join "`n  "))
    }
    $files = @($gaps.Keys | Where-Object { -not $admitted.ContainsKey($_) } | ForEach-Object {
        [ordered]@{ file = $_; lines = $gaps[$_].lines; branches = $gaps[$_].branches }
    })
    $document = [ordered]@{
        '$comment' = 'RCL source files not yet fully covered (PLAN-014): lines never run and branch outcomes never taken. The list may only shrink; a file absent from it must be fully covered. Rewritten by eng/Test-Coverage.ps1 -UpdateBaseline.'
        files      = $files
    }
    $document | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $BaselinePath -Encoding utf8
    Write-Host "Coverage baseline rewritten: $($files.Count) file(s) with gaps."
}
else {
    foreach ($file in $baseline.Keys) {
        $gap = $gaps[$file]
        $lines = $gap.lines ?? 0
        $branches = $gap.branches ?? 0
        if ($lines -lt [int]$baseline[$file].lines -or $branches -lt [int]$baseline[$file].branches) {
            $gapFailures.Add("$file is down to $lines line(s) and $branches branch(es) uncovered: shrink the baseline (eng/Test-Coverage.ps1 -UpdateBaseline).")
        }
    }
    if ($gapFailures.Count -gt 0) {
        throw ("Coverage gaps ($($gapFailures.Count)):`n  " + ($gapFailures -join "`n  "))
    }
}

$ratePercent = [Math]::Round($lineRate * 100, 2)
$branchPercent = [Math]::Round($branchRate * 100, 2)
Write-Host (($messages.CoveragePassed -f $passed, $linesValid, $ratePercent) + " Branches: $branchPercent%.")

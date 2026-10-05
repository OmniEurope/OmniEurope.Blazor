[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CoverageRoot,
    [string]$ExceptionsPath = (Join-Path $PSScriptRoot 'crap-exceptions.json'),
    [double]$Threshold = 30,
    # Writes the method scores to this CSV, for review; the gate itself never reads it.
    [string]$ReportPath
)

# CRAP gate of the RCL (PLAN-014). Score of a method: CC^2 * (1 - line coverage)^3 + CC, with the
# cyclomatic complexity and the line hits of the coverlet Cobertura report. A method above the threshold
# fails unless crap-exceptions.json admits it with a reason and a complexity ceiling. Generated code is
# left out: Razor BuildRenderTree, lambdas and their closure classes. An async or iterator state machine
# (Type/<Name>d__N.MoveNext) is the code of its source method and is scored under that method's name.

$ErrorActionPreference = 'Stop'
$messages = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
$resolvedRoot = Resolve-Path -LiteralPath $CoverageRoot
$reports = @(Get-ChildItem -LiteralPath $resolvedRoot -Recurse -File -Filter 'coverage.cobertura*.xml' |
    Where-Object FullName -NotMatch '[\\/](In|Out)[\\/]')
if ($reports.Count -ne 1) {
    throw "Expected exactly one coverage.cobertura.xml, found $($reports.Count)."
}

[xml]$coverage = Get-Content -LiteralPath $reports[0].FullName -Raw
$invariant = [Globalization.CultureInfo]::InvariantCulture

# The identity of a method, or $null for generated code.
function Get-MethodKey([string]$className, [string]$methodName) {
    if ($methodName -eq 'BuildRenderTree') { return $null }
    $owner = $className
    $name = $methodName
    $nested = $className.IndexOf('/')
    if ($nested -ge 0) {
        $owner = $className.Substring(0, $nested)
        $inner = $className.Substring($nested + 1)
        # Closure classes (<>c, <>c__DisplayClass) hold lambda bodies: generated.
        if ($inner.StartsWith('<>c')) { return $null }
        if ($inner -match '^<(?<source>[^>]+)>d__\d+$') {
            if ($Matches.source -like '*b__*') { return $null }
            $name = $Matches.source
        }
        else {
            $owner = $className
        }
    }
    # Lambdas compiled into the type itself.
    if ($name -match '^<[^>]*>b__') { return $null }
    # Local functions: <Outer>g__Inner|n_m, scored as Outer.Inner.
    if ($name -match '^<(?<outer>[^>]+)>g__(?<inner>[^|]+)\|') { $name = "$($Matches.outer).$($Matches.inner)" }
    return "${owner}::$name"
}

$entries = foreach ($class in @($coverage.coverage.packages.package.classes.class)) {
    foreach ($method in @($class.methods.method)) {
        if ($null -eq $method) { continue }
        $key = Get-MethodKey ([string]$class.name) ([string]$method.name)
        if ($null -eq $key) { continue }
        $lines = @($method.lines.line)
        $valid = $lines.Count
        if ($valid -eq 0) { continue }
        $covered = @($lines | Where-Object { [int]$_.hits -gt 0 }).Count
        $complexity = [double]::Parse([string]$method.complexity, $invariant)
        $uncovered = 1 - ($covered / $valid)
        [pscustomobject]@{
            Key        = $key
            File       = [string]$class.filename
            Line       = [int]$lines[0].number
            Complexity = $complexity
            Coverage   = $covered / $valid
            Score      = ($complexity * $complexity * [Math]::Pow($uncovered, 3)) + $complexity
        }
    }
}
$entries = @($entries)
if ($entries.Count -eq 0) {
    throw 'The coverage report holds no scorable method.'
}

if ($ReportPath) {
    $entries | Sort-Object Score -Descending | Export-Csv -LiteralPath $ReportPath -NoTypeInformation -Encoding utf8
}

$exceptions = @()
if (Test-Path -LiteralPath $ExceptionsPath) {
    $exceptions = @((Get-Content -LiteralPath $ExceptionsPath -Raw | ConvertFrom-Json).exceptions)
}

$failures = [Collections.Generic.List[string]]::new()
$byKey = @{}
foreach ($exception in $exceptions) {
    if ([string]::IsNullOrWhiteSpace([string]$exception.method)) {
        $failures.Add('An exception names no method.')
        continue
    }
    if ($byKey.ContainsKey($exception.method)) {
        $failures.Add("$($exception.method) is admitted twice.")
    }
    $byKey[$exception.method] = $exception
    if ([string]::IsNullOrWhiteSpace([string]$exception.reason)) {
        $failures.Add("$($exception.method) is admitted without a reason.")
    }
}

$used = @{}
foreach ($entry in $entries | Where-Object Score -gt $Threshold | Sort-Object Score -Descending) {
    $exception = $byKey[$entry.Key]
    $where = "$($entry.File):$($entry.Line)"
    if ($null -eq $exception) {
        $failures.Add(('{0} scores {1:N1} (complexity {2}, lines covered {3:P0}) above {4}, {5}: split it or cover it.' -f
            $entry.Key, $entry.Score, $entry.Complexity, $entry.Coverage, $Threshold, $where))
        continue
    }
    $used[$entry.Key] = [Math]::Max([double]($used[$entry.Key] ?? 0), $entry.Complexity)
    if ($entry.Complexity -gt [double]$exception.complexity) {
        $failures.Add("$($entry.Key) grew to complexity $($entry.Complexity), above its admitted ceiling $($exception.complexity), ${where}.")
    }
}

foreach ($exception in $exceptions | Where-Object { $_.method }) {
    if (-not $used.ContainsKey($exception.method)) {
        $failures.Add("$($exception.method) no longer scores above $Threshold (or is gone): remove its exception.")
    }
    elseif ($used[$exception.method] -lt [double]$exception.complexity) {
        $failures.Add("$($exception.method) is down to complexity $($used[$exception.method]): lower its ceiling from $($exception.complexity).")
    }
}

if ($failures.Count -gt 0) {
    throw ("CRAP gate failed ($($failures.Count)):`n  " + ($failures -join "`n  "))
}

Write-Host ($messages.CrapPassed -f $entries.Count, $Threshold, $exceptions.Count)

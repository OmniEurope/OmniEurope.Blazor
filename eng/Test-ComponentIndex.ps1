#requires -Version 7.0
<#
.SYNOPSIS
Checks that the component index (docs/component-families.md and the family guides) is complete and current.

.DESCRIPTION
Reads docs/public-api.txt (the generated public surface) and the family guides, and fails with one line per gap:
  a. a public component cited in no family guide;
  b. a public component parameter cited in no guide that cites the component (as `Name`, or as `Component.Name`);
     the parameters of a base class shared by every component (OmniComponentBase, OmniInputBase) count as cited
     when docs/public-api-conventions.md cites them;
  c. an `OmniXxx` or `OmniXxx.Member` token in backticks that names a type or member absent from the public surface;
  d. a link of docs/component-families.md to a guide that does not exist.
Tolerated stale names live in eng/component-index-exceptions.json, each with a reason. No build is needed.
#>
[CmdletBinding()]
param(
    [string]$ApiPath = (Join-Path $PSScriptRoot '../docs/public-api.txt'),
    [string]$IndexPath = (Join-Path $PSScriptRoot '../docs/component-families.md'),
    [string]$ConventionsPath = (Join-Path $PSScriptRoot '../docs/public-api-conventions.md'),
    [string]$ExceptionsPath = (Join-Path $PSScriptRoot 'component-index-exceptions.json')
)

$ErrorActionPreference = 'Stop'
$messages = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')

function Remove-Generics([string]$text) {
    while ($text -match '<[^<>]*>') { $text = $text -replace '<[^<>]*>', '' }
    return $text
}

# ---- Public surface -------------------------------------------------------------------------------------
$components = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$types = @{}                       # simple type name -> list of base type simple names
$members = @{}                     # simple type name -> set of member names (property, method, field, event)
$parameters = @{}                  # component simple name -> list of parameter names declared on it
$namespaces = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)

foreach ($line in [IO.File]::ReadLines((Resolve-Path -LiteralPath $ApiPath).Path)) {
    if ($line.StartsWith('component ', [StringComparison]::Ordinal)) {
        $qualified = Remove-Generics $line.Substring(10).Trim()
        [void]$components.Add($qualified.Substring($qualified.LastIndexOf('.') + 1))
    }
    elseif ($line -match '^type\s+\w+\s+(?<q>\S+)\s+\[[^\]]*\](?<rest>.*)$') {
        $qualified = Remove-Generics $Matches['q']
        $simple = $qualified.Substring($qualified.LastIndexOf('.') + 1)
        $bases = @()
        # Generic constraints (where T : class) also contain ' : ', so the base list is what follows the last one.
        $rest = $Matches['rest']
        if ($rest.Contains(' : ')) {
            $bases = @($rest.Substring($rest.LastIndexOf(' : ') + 3) -split ',\s+' | ForEach-Object {
                    $baseName = Remove-Generics $_.Trim()
                    $baseName.Substring($baseName.LastIndexOf('.') + 1)
                })
        }
        $types[$simple] = $bases
        $ns = $qualified.Substring(0, $qualified.LastIndexOf('.'))
        while ($ns.Contains('.')) { [void]$namespaces.Add($ns); $ns = $ns.Substring(0, $ns.LastIndexOf('.')) }
        [void]$namespaces.Add($ns)
    }
    elseif ($line -match '^(?<kind>property|method|field|event)\s+[a-z ]*?(?<q>OmniEurope\.\S+?)(?=\(|\s:)') {
        $qualified = Remove-Generics $Matches['q']
        $dot = $qualified.LastIndexOf('.')
        $name = $qualified.Substring($dot + 1)
        $owner = $qualified.Substring(0, $dot)
        $owner = $owner.Substring($owner.LastIndexOf('.') + 1)
        if (-not $members.ContainsKey($owner)) { $members[$owner] = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal) }
        [void]$members[$owner].Add($name)
        if ($Matches['kind'] -eq 'property' -and $line -match '\[Parameter[,\]]') {
            if (-not $parameters.ContainsKey($owner)) { $parameters[$owner] = [System.Collections.Generic.List[string]]::new() }
            $parameters[$owner].Add($name)
        }
    }
}

function Get-MemberSet([string]$type) {
    $set = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $pending = [System.Collections.Generic.Queue[string]]::new()
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $pending.Enqueue($type)
    while ($pending.Count -gt 0) {
        $current = $pending.Dequeue()
        if (-not $seen.Add($current)) { continue }
        if ($members.ContainsKey($current)) { $set.UnionWith($members[$current]) }
        if ($types.ContainsKey($current)) { foreach ($base in $types[$current]) { $pending.Enqueue($base) } }
    }
    return , $set
}

# Members that come from the framework base classes (InputBase, ComponentBase) and are not in the baseline.
$frameworkMembers = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($name in 'Value', 'ValueChanged', 'ValueExpression', 'CssClass', 'EditContext', 'FieldIdentifier', 'DisplayName', 'AdditionalAttributes', 'CurrentValue', 'CurrentValueAsString', 'StateHasChanged', 'InvokeAsync', 'Dispose', 'DisposeAsync', 'ToString') {
    [void]$frameworkMembers.Add($name)
}

# ---- Documents ------------------------------------------------------------------------------------------
$docsRoot = Split-Path -Parent (Resolve-Path -LiteralPath $IndexPath).Path
$failures = [System.Collections.Generic.List[string]]::new()

function Get-DocumentTokens([string]$path) {
    $text = [IO.File]::ReadAllText($path)
    $inline = [System.Collections.Generic.List[string]]::new()
    $fenceLines = $text -split "`r?`n"
    $inFence = $false
    $prose = [System.Collections.Generic.List[string]]::new()
    foreach ($row in $fenceLines) {
        if ($row -match '^\s*```') { $inFence = -not $inFence; continue }
        if (-not $inFence) { $prose.Add($row) }
    }
    foreach ($row in $prose) {
        foreach ($match in [regex]::Matches($row, '(`+)(?<span>[^`]+?)\1')) { $inline.Add($match.Groups['span'].Value) }
    }
    return , $inline
}

$indexText = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $IndexPath).Path)
$guidePaths = [System.Collections.Generic.List[string]]::new()
foreach ($link in [regex]::Matches($indexText, '\]\((?<target>[^)\s]+)\)')) {
    $target = $link.Groups['target'].Value
    if ($target -match '^[a-z]+:' -or $target.StartsWith('#')) { continue }
    $target = ($target -split '#')[0]
    if (-not $target.EndsWith('.md', [StringComparison]::Ordinal)) { continue }
    $resolved = Join-Path $docsRoot $target
    if (-not (Test-Path -LiteralPath $resolved)) {
        $failures.Add(($messages.ComponentIndexGuideMissing -f $target))
        continue
    }
    $full = (Resolve-Path -LiteralPath $resolved).Path
    if (-not $guidePaths.Contains($full)) { $guidePaths.Add($full) }
}

$guides = @{}                      # guide file name -> inline spans
foreach ($path in $guidePaths) { $guides[[IO.Path]::GetFileName($path)] = Get-DocumentTokens $path }
$indexName = [IO.Path]::GetFileName($IndexPath)
$conventionsName = [IO.Path]::GetFileName($ConventionsPath)
$extra = @{}
$extra[$indexName] = Get-DocumentTokens (Resolve-Path -LiteralPath $IndexPath).Path
$extra[$conventionsName] = Get-DocumentTokens (Resolve-Path -LiteralPath $ConventionsPath).Path

$identifierPattern = [regex]'(?<![\w.])[A-Za-z_]\w*'
$pairPattern = [regex]'(?<![\w])(?<owner>[A-Za-z_]\w*)(?:<[^<>`]*>)?\.(?<member>[A-Za-z_]\w*)'

# identifiers cited per document, and Owner.Member pairs cited anywhere
$identifiersByDocument = @{}
$pairs = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($entry in @($guides.GetEnumerator()) + @($extra.GetEnumerator())) {
    $set = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($span in $entry.Value) {
        foreach ($match in $identifierPattern.Matches($span)) { [void]$set.Add($match.Value) }
        foreach ($match in $pairPattern.Matches($span)) { [void]$pairs.Add($match.Groups['owner'].Value + '.' + $match.Groups['member'].Value) }
    }
    $identifiersByDocument[$entry.Key] = $set
}

# ---- a. components cited in a guide ---------------------------------------------------------------------
$citingGuides = @{}                # component -> guide names that cite it
foreach ($component in ($components | Sort-Object)) {
    $citingGuides[$component] = @($guides.Keys | Where-Object { $identifiersByDocument[$_].Contains($component) } | Sort-Object)
    if ($citingGuides[$component].Count -eq 0 -and -not $identifiersByDocument[$conventionsName].Contains($component)) {
        $failures.Add(($messages.ComponentIndexComponentUncited -f $component))
    }
}

# ---- b. parameters cited --------------------------------------------------------------------------------
$parameterCount = 0
foreach ($component in ($parameters.Keys | Where-Object { $components.Contains($_) } | Sort-Object)) {
    $guideNames = @($citingGuides[$component]) + @($conventionsName)
    foreach ($parameter in ($parameters[$component] | Sort-Object -Unique)) {
        $parameterCount++
        if ($pairs.Contains("$component.$parameter")) { continue }
        $cited = $false
        foreach ($guideName in $guideNames) {
            if ($identifiersByDocument[$guideName].Contains($parameter)) { $cited = $true; break }
        }
        if (-not $cited) { $failures.Add(($messages.ComponentIndexParameterUncited -f "$component.$parameter")) }
    }
}

# ---- c. stale names -------------------------------------------------------------------------------------
$exceptions = @()
if (Test-Path -LiteralPath $ExceptionsPath) {
    $document = Get-Content -LiteralPath $ExceptionsPath -Raw | ConvertFrom-Json
    $exceptions = @($document.staleNames | Where-Object { $null -ne $_ })
}
$usedExceptions = [System.Collections.Generic.HashSet[int]]::new()
for ($index = 0; $index -lt $exceptions.Count; $index++) {
    $item = $exceptions[$index]
    if ([string]::IsNullOrWhiteSpace([string]$item.token) -or [string]::IsNullOrWhiteSpace([string]$item.reason)) {
        $failures.Add(($messages.ComponentIndexExceptionInvalid -f $index))
    }
}

$tokenPattern = [regex]'(?<![\w.-])I?Omni\w*(?:<[^<>`]*>)?(?:\.(?<member>[A-Za-z_]\w*))?'
$stale = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$allDocuments = @($guides.GetEnumerator()) + @($extra.GetEnumerator())
foreach ($entry in ($allDocuments | Sort-Object Key)) {
    foreach ($span in $entry.Value) {
        $normalized = $span -replace 'OmniEurope\.Blazor\.Components\.', '' -replace 'OmniEurope\.Blazor\.', ''
        foreach ($match in $tokenPattern.Matches($normalized)) {
            $text = Remove-Generics $match.Value
            $type = $text
            $member = $null
            if ($text.Contains('.')) { $type = $text.Substring(0, $text.IndexOf('.')); $member = $text.Substring($text.IndexOf('.') + 1) }
            $problem = $null
            # The bare prefix `Omni` (as in "the Omni prefix") names no type.
            if ($namespaces.Contains($type) -or $type -eq 'OmniEurope' -or $type -eq 'Omni') { continue }
            if (-not $types.ContainsKey($type)) { $problem = $type }
            elseif ($member) {
                $memberSet = Get-MemberSet $type
                if (-not $memberSet.Contains($member) -and -not $frameworkMembers.Contains($member)) { $problem = "$type.$member" }
            }
            if ($null -eq $problem) { continue }
            $matched = $false
            for ($index = 0; $index -lt $exceptions.Count; $index++) {
                $item = $exceptions[$index]
                $sameFile = @($item.files).Count -eq 0 -or @($item.files) -contains $entry.Key
                if ($sameFile -and ([string]$item.token -ceq $problem -or [string]$item.token -ceq $type)) {
                    [void]$usedExceptions.Add($index)
                    $matched = $true
                }
            }
            if (-not $matched) { [void]$stale.Add(($messages.ComponentIndexStaleName -f $entry.Key, $problem)) }
        }
    }
}
foreach ($line in ($stale | Sort-Object)) { $failures.Add($line) }
for ($index = 0; $index -lt $exceptions.Count; $index++) {
    if (-not $usedExceptions.Contains($index)) { $failures.Add(($messages.ComponentIndexExceptionUnused -f [string]$exceptions[$index].token)) }
}

# ---- Result ---------------------------------------------------------------------------------------------
if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host $failure }
    Write-Host ($messages.ComponentIndexFailed -f $failures.Count)
    exit 1
}
Write-Host ($messages.ComponentIndexPassed -f $components.Count, $parameterCount, $guides.Count)

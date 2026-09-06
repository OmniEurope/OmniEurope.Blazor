[CmdletBinding()]
param(
    [string]$InventoryPath = (Join-Path $PSScriptRoot '..\docs\component-inventory.json'),
    [string]$ComponentRoot = (Join-Path $PSScriptRoot '..\src\OmniEurope.Blazor\Components'),
    [string]$TestRoot = (Join-Path $PSScriptRoot '..\tests'),
    [string]$CatalogMatrixPath = (Join-Path $PSScriptRoot '..\docs\catalog-scenarios.json'),
    [string]$BrowserMatrixPath = (Join-Path $PSScriptRoot '..\docs\browser-scenarios.json'),
    [string]$JsonOutput = (Join-Path $PSScriptRoot '..\docs\component-coverage.json'),
    [string]$MarkdownOutput = (Join-Path $PSScriptRoot '..\docs\component-coverage.md')
)

$ErrorActionPreference = 'Stop'
$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$inventory = Get-Content -Raw -LiteralPath $InventoryPath | ConvertFrom-Json
$catalog = Get-Content -Raw -LiteralPath $CatalogMatrixPath | ConvertFrom-Json
$browser = Get-Content -Raw -LiteralPath $BrowserMatrixPath | ConvertFrom-Json
$componentFiles = @(Get-ChildItem -LiteralPath $ComponentRoot -Filter 'Omni*.razor' -File -Recurse)
$componentGroups = @($componentFiles | Group-Object BaseName | Where-Object Count -gt 1)
if ($componentGroups.Count -gt 0) { throw "Duplicate component targets: $($componentGroups.Name -join ', ')" }
$componentByName = @{}
foreach ($file in $componentFiles) { $componentByName[$file.BaseName] = $file }
$testFiles = @(Get-ChildItem -LiteralPath $TestRoot -Recurse -File | Where-Object Extension -in '.cs', '.razor')
$testSources = @($testFiles | ForEach-Object {
    [pscustomobject]@{ path = $_.FullName; content = [IO.File]::ReadAllText($_.FullName) }
})
$catalogByComponent = @{}
foreach ($item in $catalog.components) { $catalogByComponent[[string]$item.component] = $item }
$browserByComponent = @{}
foreach ($scenario in $browser.scenarios) {
    $scriptPath = Join-Path $repositoryRoot ([string]$scenario.script).Replace('/', '\')
    if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) { throw "Browser evidence script is missing: $($scenario.script)" }
    foreach ($component in $scenario.components) {
        if (-not $browserByComponent.ContainsKey([string]$component)) { $browserByComponent[[string]$component] = [Collections.Generic.List[object]]::new() }
        $browserByComponent[[string]$component].Add([pscustomobject]@{
            id = $scenario.id
            host = $scenario.host
            script = $scenario.script
            assertions = @($scenario.assertions)
        })
    }
}

$targetOverrides = @{
    RadzenBreadCrumb = 'OmniBreadcrumb'
    RadzenBreadCrumbItem = 'OmniBreadcrumbItem'
    RadzenProgressBarCircular = 'OmniProgressBar'
    RadzenTheme = 'OmniThemeScope'
    RadzenComponents = 'OmniComponentsHost'
    RadzenAutoComplete = 'OmniAutocomplete'
}

function Resolve-Target([string]$component) {
    if ($component -like 'RadzenHtmlEditor*') { return 'OmniHtmlEditor' }
    if ($targetOverrides.ContainsKey($component)) { return $targetOverrides[$component] }
    return 'Omni' + $component.Substring('Radzen'.Length)
}

function Resolve-Phase([string]$component) {
    switch -Regex ($component) {
        '^Radzen(HtmlEditor)' { return 12 }
        '^Radzen(Timeline|Scheduler|DayView|WeekView|MonthView)' { return 11 }
        '^Radzen(Chart|.*Series|CategoryAxis|ValueAxis|AxisTitle|Legend|GridLines|Markers|BarOptions|ArcGauge)' { return 10 }
        '^Radzen(DataGrid)' { return 9 }
        '^Radzen(DataList|Pager|Tree)' { return 8 }
        '^Radzen(PanelMenu|BreadCrumb|Tabs|Steps|ProfileMenu)' { return 7 }
        '^Radzen(DropDown|AutoComplete|ListBox|CheckBoxList|RadioButtonList|SelectBar|DatePicker|Slider|ColorPicker|Upload)' { return 6 }
        '^Radzen(TextBox|Password|TextArea|Numeric|CheckBox|Switch|Label|FormField|TemplateForm|.*Validator)' { return 5 }
        '^Radzen(SplitButton|ToggleButton|Components|Dialog|Notification|Tooltip|ContextMenu)' { return 4 }
        default { return 3 }
    }
}

$entries = foreach ($item in $inventory.catalog) {
    $target = Resolve-Target $item.component
    $file = $componentByName[$target]
    $testPattern = "(?<![A-Za-z0-9_])$([regex]::Escape($target))(?![A-Za-z0-9_])"
    $testReferences = [Collections.Generic.List[string]]::new()
    foreach ($reference in $testSources | Where-Object { $_.content -match $testPattern } | ForEach-Object {
        [IO.Path]::GetRelativePath($repositoryRoot, $_.path).Replace('\', '/')
    } | Sort-Object -Unique) {
        $testReferences.Add($reference)
    }
    $catalogEvidence = if ($catalogByComponent.ContainsKey($target)) {
        [pscustomobject]@{ scenario = $catalogByComponent[$target].scenario; evidence = $catalogByComponent[$target].evidence }
    } else { $null }
    $browserEvidence = [Collections.Generic.List[object]]::new()
    if ($browserByComponent.ContainsKey($target)) {
        foreach ($scenario in $browserByComponent[$target]) { $browserEvidence.Add($scenario) }
    }
    [pscustomobject]@{
        source = $item.component
        target = $target
        phase = Resolve-Phase $item.component
        status = if ($file) { 'target-present' } else { 'target-missing' }
        targetFile = if ($file) { [IO.Path]::GetRelativePath($repositoryRoot, $file.FullName).Replace('\', '/') } else { $null }
        targetSha256 = if ($file) { (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant() } else { $null }
        evidence = [ordered]@{
            testReferences = $testReferences
            catalog = $catalogEvidence
            browser = $browserEvidence
        }
        activeProjects = $item.activeProjects
        activeOccurrences = $item.activeOccurrences
        allProjects = $item.allProjects
        occurrences = $item.occurrences
    }
}

$presentCount = @($entries | Where-Object status -eq 'target-present').Count
$withTestReferences = @($entries | Where-Object { $_.evidence.testReferences.Count -gt 0 }).Count
$catalogCount = @($entries | Where-Object { $null -ne $_.evidence.catalog }).Count
$browserCount = @($entries | Where-Object { $_.evidence.browser.Count -gt 0 }).Count
$document = [ordered]@{
    schemaVersion = 2
    generatedFrom = $inventory.generatedAt
    total = @($entries).Count
    targetsPresent = $presentCount
    targetsMissing = @($entries).Count - $presentCount
    withTestReferences = $withTestReferences
    catalogIllustrated = $catalogCount
    browserExercised = $browserCount
    entries = @($entries)
}

$document | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $JsonOutput -Encoding utf8

$lines = [Collections.Generic.List[string]]::new()
$lines.Add('# Registre de couverture des composants')
$lines.Add('')
$lines.Add(($psText.CoverageIntro -f $document.total))
$lines.Add('')
$lines.Add(($psText.CoveragePresent -f $presentCount, $document.total))
$lines.Add(($psText.CoverageMissing -f $document.targetsMissing))
$lines.Add(($psText.CoverageTestReferences -f $withTestReferences, $document.total))
$lines.Add(($psText.CoverageCatalog -f $catalogCount, $document.total))
$lines.Add(($psText.CoverageBrowser -f $browserCount, $document.total))
$lines.Add('')
$lines.Add($psText.CoverageEvidenceHeader)
$lines.Add('| --- | --- | ---: | --- | ---: | --- | ---: |')
foreach ($entry in $entries) {
    $state = if ($entry.status -eq 'target-present') { $psText.CoverageImplemented } else { $psText.CoveragePlanned }
    $catalogState = if ($null -ne $entry.evidence.catalog) { 'oui' } else { 'non' }
    $lines.Add("| ``$($entry.source)`` | ``$($entry.target)`` | $($entry.phase) | $state | $($entry.evidence.testReferences.Count) | $catalogState | $($entry.evidence.browser.Count) |")
}
$lines.Add('')
$lines.Add($psText.CoverageDisclaimer)
$lines | Set-Content -LiteralPath $MarkdownOutput -Encoding utf8

Write-Host "Coverage evidence generated: $presentCount/$($document.total) targets present, $withTestReferences test-referenced, $catalogCount catalog-illustrated, $browserCount browser-exercised."

[CmdletBinding()]
param(
    [string]$InventoryPath = (Join-Path $PSScriptRoot '..\docs\component-inventory.json'),
    [string]$ComponentRoot = (Join-Path $PSScriptRoot '..\src\OmniEurope.Blazor\Components'),
    [string]$JsonOutput = (Join-Path $PSScriptRoot '..\docs\component-coverage.json'),
    [string]$MarkdownOutput = (Join-Path $PSScriptRoot '..\docs\component-coverage.md')
)

$ErrorActionPreference = 'Stop'
$inventory = Get-Content -Raw -LiteralPath $InventoryPath | ConvertFrom-Json
$implementedComponents = Get-ChildItem -LiteralPath $ComponentRoot -Filter 'Omni*.razor' -File |
    ForEach-Object BaseName

$targetOverrides = @{
    RadzenBreadCrumb = 'OmniBreadcrumb'
    RadzenBreadCrumbItem = 'OmniBreadcrumbItem'
    RadzenProgressBarCircular = 'OmniProgressBar'
    RadzenTheme = 'OmniThemeScope'
    RadzenComponents = 'OmniComponentsHost'
    RadzenAutoComplete = 'OmniAutocomplete'
}

function Resolve-Target([string]$component) {
    if ($component -like 'RadzenHtmlEditor*') {
        return 'OmniHtmlEditor'
    }

    if ($targetOverrides.ContainsKey($component)) {
        return $targetOverrides[$component]
    }

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
    [pscustomobject]@{
        source = $item.component
        target = $target
        phase = Resolve-Phase $item.component
        status = if ($implementedComponents -contains $target) { 'implemented' } else { 'planned' }
        activeProjects = $item.activeProjects
        activeOccurrences = $item.activeOccurrences
        allProjects = $item.allProjects
        occurrences = $item.occurrences
    }
}

$implementedCount = @($entries | Where-Object status -eq 'implemented').Count
$document = [ordered]@{
    generatedFrom = $inventory.generatedAt
    total = @($entries).Count
    implemented = $implementedCount
    planned = @($entries).Count - $implementedCount
    entries = @($entries)
}

$document | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $JsonOutput -Encoding utf8

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# Registre de couverture des composants')
$lines.Add('')
$lines.Add("Ce registre relie les $($document.total) balises inventoriées à une capacité OmniEurope et à la phase de construction correspondante. Il ne déclenche aucune migration.")
$lines.Add('')
$lines.Add("- Capacités inventoriées déjà couvertes : **$implementedCount/$($document.total)**")
$lines.Add("- Capacités encore planifiées : **$($document.planned)**")
$lines.Add('')
$lines.Add('| Source inventoriée | Cible OmniEurope | Phase | État | Projets actifs | Occurrences actives |')
$lines.Add('| --- | --- | ---: | --- | ---: | ---: |')

foreach ($entry in $entries) {
    $state = if ($entry.status -eq 'implemented') { 'implémenté' } else { 'planifié' }
    $lines.Add("| ``$($entry.source)`` | ``$($entry.target)`` | $($entry.phase) | $state | $($entry.activeProjects) | $($entry.activeOccurrences) |")
}

$lines.Add('')
$lines.Add('Le statut « implémenté » signifie que le composant Razor cible existe. Les comportements détaillés restent validés par les gates de leur phase dans `PLAN-002`.')
$lines | Set-Content -LiteralPath $MarkdownOutput -Encoding utf8

Write-Host "Coverage registry generated: $implementedCount/$($document.total) inventory items implemented."

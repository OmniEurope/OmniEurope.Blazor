[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RadzenSyntax.ps1')

$fixture = @'
@* <RadzenButton Text="Ignored" /> *@
<RadzenAlert AlertStyle="@(_batchResult.FailureCount == 0 ? AlertStyle.Success : AlertStyle.Warning)"
             Visible="@Status" Title="@L["AnalysisGateIncompleteTitle"]" class="rz-mt-2" role="status" />
<RadzenDataGrid Data="@Rows">
  <Columns>
    <RadzenDataGridColumn Property="Decision" Title="Decision" />
  </Columns>
</RadzenDataGrid>
@code { private int RadzenButtonIconAuditTests => 0; }
'@

$components = @(Get-RadzenRazorComponents -Text $fixture)
if ($components.Count -ne 3) { throw "Expected three component tags, found $($components.Count)." }
$alert = $components | Where-Object component -eq 'RadzenAlert'
if (Compare-Object @('AlertStyle', 'Title', 'Visible') @($alert.parameters)) {
    throw "Alert parameters were parsed incorrectly: $($alert.parameters -join ', ')"
}
if ($components.parameters -contains 'FailureCount' -or $components.parameters -contains 'Status' -or $components.parameters -contains 'AnalysisGateIncompleteTitle' -or $components.parameters -contains 'class' -or $components.parameters -contains 'role' -or $components.component -contains 'RadzenButtonIconAuditTests') {
    throw 'Expression identifiers or test names leaked into the component contract.'
}
$column = $components | Where-Object component -eq 'RadzenDataGridColumn'
if (Compare-Object @('Property', 'Title') @($column.parameters)) {
    throw "Column parameters were parsed incorrectly: $($column.parameters -join ', ')"
}

$references = @(Get-RadzenClassifiedReferences -Text $fixture -Extension '.razor')
if ($references.name -contains 'RadzenButtonIconAuditTests') {
    throw 'A test identifier leaked into classified references.'
}

Write-Host 'Radzen syntax fixtures passed: tags, attributes, comments, expressions, and false identifiers.'

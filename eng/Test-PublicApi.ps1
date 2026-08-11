[CmdletBinding()]
param(
    [switch]$Update,
    [string]$BaselinePath = (Join-Path $PSScriptRoot '..\docs\public-api.txt'),
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$tool = Join-Path $PSScriptRoot "OmniEurope.PublicApiGuard\bin\$Configuration\net10.0\OmniEurope.PublicApiGuard.dll"
if (-not (Test-Path -LiteralPath $tool)) {
    throw "Public API guard binary is missing. Build OmniEurope.Blazor.slnx in $Configuration first."
}

$arguments = @($tool, '--baseline', (Resolve-Path -LiteralPath $BaselinePath).Path)
if ($Update) {
    $arguments += '--update'
}

& dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

[CmdletBinding()]
param(
    [switch]$Update,
    [string]$BaselinePath = (Join-Path $PSScriptRoot '..\docs\public-api.txt'),
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
# The package ships lib/net10.0 and lib/net11.0 with one public surface: the guard is built once per target
# framework, each build loads the library of its own framework, and both must equal the one baseline.
# -Update writes the baseline from net11.0, then checks net10.0 against it, so a surface that differs between
# the two frameworks fails instead of being recorded.
$frameworks = @('net11.0', 'net10.0')
foreach ($framework in $frameworks) {
    $tool = Join-Path $PSScriptRoot "OmniEurope.PublicApiGuard\bin\$Configuration\$framework\OmniEurope.PublicApiGuard.dll"
    if (-not (Test-Path -LiteralPath $tool)) {
        throw "Public API guard binary for $framework is missing. Build OmniEurope.Blazor.slnx in $Configuration first."
    }
}

foreach ($framework in $frameworks) {
    $tool = Join-Path $PSScriptRoot "OmniEurope.PublicApiGuard\bin\$Configuration\$framework\OmniEurope.PublicApiGuard.dll"
    $arguments = @($tool, '--baseline', (Resolve-Path -LiteralPath $BaselinePath).Path)
    if ($Update -and $framework -eq $frameworks[0]) {
        $arguments += '--update'
    }

    Write-Host "Public API of lib/${framework}:"
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

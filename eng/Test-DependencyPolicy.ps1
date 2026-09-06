[CmdletBinding()]
param(
    [switch]$SkipCatalogCheck
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$messages = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
$policy = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'dependency-policy.json') -Raw | ConvertFrom-Json
[xml]$central = Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Packages.props') -Raw
$versions = @{}
foreach ($entry in @($central.Project.ItemGroup.PackageVersion)) {
    $versions[[string]$entry.Include] = [string]$entry.Version
}

foreach ($package in $policy.packages.PSObject.Properties) {
    if (-not $versions.ContainsKey($package.Name)) {
        throw "Policy package $($package.Name) is absent from Directory.Packages.props."
    }
    if ($versions[$package.Name] -ne [string]$package.Value.version) {
        throw "Policy drift for $($package.Name): expected $($package.Value.version), found $($versions[$package.Name])."
    }
}
if ($versions.Count -ne @($policy.packages.PSObject.Properties).Count) {
    $missing = @($versions.Keys | Where-Object { $null -eq $policy.packages.PSObject.Properties[$_] })
    throw "Every central PackageVersion must have a reviewed policy entry. Missing: $($missing -join ', ')."
}

$reviewedAt = [DateTime]::ParseExact([string]$policy.reviewedAt, 'yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture)
if (([DateTime]::UtcNow.Date - $reviewedAt.Date).TotalDays -gt 30) {
    throw "Dependency catalog evidence is older than 30 days: $($policy.reviewedAt)."
}
if (-not $SkipCatalogCheck) {
    foreach ($package in @($policy.packages.PSObject.Properties | Where-Object { $_.Value.status -eq 'latest-stable' })) {
        $id = $package.Name.ToLowerInvariant()
        $catalog = Invoke-RestMethod -Uri "https://api.nuget.org/v3-flatcontainer/$id/index.json" -TimeoutSec 15
        $latest = @($catalog.versions | Where-Object { $_ -notmatch '-' } | Select-Object -Last 1)
        if ($latest.Count -ne 1 -or [string]$latest[0] -cne [string]$package.Value.version) {
            throw "NuGet catalog drift for $($package.Name): reviewed $($package.Value.version), latest stable $latest."
        }
    }
}

$global = Get-Content -LiteralPath (Join-Path $repoRoot 'global.json') -Raw | ConvertFrom-Json
if ([string]$global.sdk.version -ne [string]$policy.toolchain.sdk -or
    [string]$global.sdk.rollForward -ne [string]$policy.toolchain.rollForward -or
    [string]$global.sdk.workloadVersion -ne [string]$policy.toolchain.workloadVersion) {
    throw 'global.json does not match the reviewed toolchain policy.'
}

$testLock = Get-Content -LiteralPath (Join-Path $repoRoot 'tests/OmniEurope.Blazor.Tests/packages.lock.json') -Raw | ConvertFrom-Json
$testFramework = @($testLock.dependencies.PSObject.Properties)[0].Value
foreach ($id in @('bunit', 'coverlet.collector')) {
    $entry = $testFramework.PSObject.Properties[$id]
    if ($null -eq $entry -or [string]$entry.Value.resolved -ne [string]$policy.packages.$id.version) {
        throw "Test lock does not resolve $id at the reviewed version."
    }
}

$hybridLock = Get-Content -LiteralPath (Join-Path $repoRoot 'samples/OmniEurope.Blazor.HybridSmoke/packages.lock.json') -Raw | ConvertFrom-Json
$hybridFramework = @($hybridLock.dependencies.PSObject.Properties | Where-Object Name -Like 'net10.0-windows*')[0].Value
foreach ($id in @('Microsoft.Maui.Controls', 'Microsoft.AspNetCore.Components.WebView.Maui')) {
    $entry = $hybridFramework.PSObject.Properties[$id]
    if ($null -eq $entry -or [string]$entry.Value.resolved -ne [string]$policy.packages.$id.version) {
        throw "Hybrid lock does not resolve $id at the reviewed version."
    }
}

$workflowFiles = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot '.github/workflows') -File |
    Where-Object Extension -In '.yml', '.yaml')
$actionPattern = '^\s*-?\s*uses:\s*([^\s#]+)'
foreach ($fixture in @(
    @{ Line = '      - uses: owner/action@0123456789012345678901234567890123456789'; Valid = $true },
    @{ Line = '        uses: owner/action@0123456789012345678901234567890123456789'; Valid = $true },
    @{ Line = '      - uses: owner/action@main'; Valid = $false },
    @{ Line = '      - uses: owner/action@0123456'; Valid = $false }
)) {
    if ($fixture.Line -notmatch $actionPattern) { throw "Action parser fixture was not recognized: $($fixture.Line)" }
    $isValid = $Matches[1] -match '@[0-9a-f]{40}$'
    if ($isValid -ne $fixture.Valid) { throw "Action parser fixture produced the wrong verdict: $($fixture.Line)" }
}
foreach ($file in $workflowFiles) {
    $lineNumber = 0
    foreach ($line in Get-Content -LiteralPath $file.FullName) {
        $lineNumber++
        if ($line -match $actionPattern) {
            $reference = $Matches[1]
            if ($reference.StartsWith('./')) { continue }
            if ($reference -notmatch '@[0-9a-f]{40}$') {
                throw "Mutable action reference in $($file.Name):$($lineNumber): $reference"
            }
        }
    }
}

Write-Host $messages.DependencyPolicyPassed

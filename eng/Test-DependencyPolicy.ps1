[CmdletBinding()]
param(
    [switch]$SkipCatalogCheck
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$messages = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
$policy = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'dependency-policy.json') -Raw | ConvertFrom-Json
[xml]$central = Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Packages.props') -Raw
# A PackageVersion row is unconditional, or split by target framework in exactly two rows:
# Condition="'$(TargetFramework)' == '<tfm>'" (the version of that framework, policy frameworkVersions.<tfm>)
# and Condition="'$(TargetFramework)' != '<tfm>'" (the version of every other framework, policy version).
# Any other condition is refused, so a row the guard cannot read never escapes it.
$versions = @{}
$frameworkVersions = @{}
$excludedFrameworks = @{}
foreach ($entry in @($central.Project.ItemGroup.PackageVersion)) {
    $id = [string]$entry.Include
    $condition = [string]$entry.Condition
    if ([string]::IsNullOrWhiteSpace($condition)) {
        if ($versions.ContainsKey($id)) { throw "Directory.Packages.props declares $id twice." }
        $versions[$id] = [string]$entry.Version
    }
    elseif ($condition -match "^\s*'\`$\(TargetFramework\)'\s*==\s*'([^']+)'\s*$") {
        if (-not $frameworkVersions.ContainsKey($id)) { $frameworkVersions[$id] = @{} }
        if ($frameworkVersions[$id].ContainsKey($Matches[1])) { throw "Directory.Packages.props declares $id twice for $($Matches[1])." }
        $frameworkVersions[$id][$Matches[1]] = [string]$entry.Version
    }
    elseif ($condition -match "^\s*'\`$\(TargetFramework\)'\s*!=\s*'([^']+)'\s*$") {
        if ($versions.ContainsKey($id)) { throw "Directory.Packages.props declares $id twice." }
        $versions[$id] = [string]$entry.Version
        $excludedFrameworks[$id] = $Matches[1]
    }
    else {
        throw "Unsupported PackageVersion condition for ${id}: $condition"
    }
}
foreach ($id in $frameworkVersions.Keys) {
    $frameworks = @($frameworkVersions[$id].Keys)
    if (-not $versions.ContainsKey($id) -or $frameworks.Count -ne 1 -or $excludedFrameworks[$id] -cne $frameworks[0]) {
        throw "$id must pair its '== $($frameworks -join ', ')' row with one '!=' row on the same target framework."
    }
}
foreach ($id in $excludedFrameworks.Keys) {
    if (-not $frameworkVersions.ContainsKey($id)) { throw "$id excludes $($excludedFrameworks[$id]) without a row for it." }
}

foreach ($package in $policy.packages.PSObject.Properties) {
    if (-not $versions.ContainsKey($package.Name)) {
        throw "Policy package $($package.Name) is absent from Directory.Packages.props."
    }
    if ($versions[$package.Name] -ne [string]$package.Value.version) {
        throw "Policy drift for $($package.Name): expected $($package.Value.version), found $($versions[$package.Name])."
    }
    $declared = if ($frameworkVersions.ContainsKey($package.Name)) { $frameworkVersions[$package.Name] } else { @{} }
    $reviewed = @(if ($package.Value.frameworkVersions) { $package.Value.frameworkVersions.PSObject.Properties })
    if ($declared.Count -ne $reviewed.Count) {
        throw "Policy drift for $($package.Name): $($reviewed.Count) reviewed target framework rows, $($declared.Count) in Directory.Packages.props."
    }
    foreach ($framework in $reviewed) {
        if (-not $declared.ContainsKey($framework.Name) -or $declared[$framework.Name] -ne [string]$framework.Value.version) {
            throw "Policy drift for $($package.Name) on $($framework.Name): expected $($framework.Value.version), found $($declared[$framework.Name])."
        }
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
# STD-SDKPIN: a newer NuGet release is reported, never turned into a build failure. The pins, the
# policy entries and reviewedAt above stay blocking; the upgrade itself is made by hand.
if (-not $SkipCatalogCheck) {
    foreach ($package in @($policy.packages.PSObject.Properties | Where-Object { $_.Value.status -eq 'latest-stable' })) {
        $id = $package.Name.ToLowerInvariant()
        $catalog = Invoke-RestMethod -Uri "https://api.nuget.org/v3-flatcontainer/$id/index.json" -TimeoutSec 15
        $latest = @($catalog.versions | Where-Object { $_ -notmatch '-' } | Select-Object -Last 1)
        if ($latest.Count -ne 1 -or [string]$latest[0] -cne [string]$package.Value.version) {
            $drift = "NuGet catalog drift for $($package.Name): reviewed $($package.Value.version), latest stable $latest."
            if ($env:GITHUB_ACTIONS -eq 'true') { Write-Host "::warning::$drift" } else { Write-Warning $drift }
        }
        # A per-framework row (latest-stable-patch) follows the newest stable patch of its own major.minor line.
        foreach ($framework in @(if ($package.Value.frameworkVersions) { $package.Value.frameworkVersions.PSObject.Properties })) {
            if ([string]$framework.Value.status -ne 'latest-stable-patch') { continue }
            $reviewedVersion = [version][string]$framework.Value.version
            $line = "$($reviewedVersion.Major).$($reviewedVersion.Minor)."
            $latestPatch = @($catalog.versions | Where-Object { $_ -notmatch '-' -and $_.StartsWith($line) } | Select-Object -Last 1)
            if ($latestPatch.Count -ne 1 -or [string]$latestPatch[0] -cne [string]$framework.Value.version) {
                $drift = "NuGet catalog drift for $($package.Name) on $($framework.Name): reviewed $($framework.Value.version), latest stable $line* $latestPatch."
                if ($env:GITHUB_ACTIONS -eq 'true') { Write-Host "::warning::$drift" } else { Write-Warning $drift }
            }
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
$testFrameworks = @($testLock.dependencies.PSObject.Properties)
if ($testFrameworks.Count -eq 0) { throw 'Test lock has no target framework.' }
foreach ($testFramework in $testFrameworks) {
    foreach ($id in @('bunit', 'coverlet.MTP')) {
        $entry = $testFramework.Value.PSObject.Properties[$id]
        if ($null -eq $entry -or [string]$entry.Value.resolved -ne [string]$policy.packages.$id.version) {
            throw "Test lock does not resolve $id at the reviewed version for $($testFramework.Name)."
        }
    }
}

# The package lock: each target framework of the library resolves its framework packages at the reviewed
# version of that framework (frameworkVersions.<tfm>, otherwise version), so lib/net10.0 never ships with 11.0.
$libraryLock = Get-Content -LiteralPath (Join-Path $repoRoot 'src/OmniEurope.Blazor/packages.lock.json') -Raw | ConvertFrom-Json
[xml]$libraryProject = Get-Content -LiteralPath (Join-Path $repoRoot 'src/OmniEurope.Blazor/OmniEurope.Blazor.csproj') -Raw
$libraryFrameworks = @(([string]$libraryProject.Project.PropertyGroup.TargetFrameworks).Split(';', [StringSplitOptions]::RemoveEmptyEntries))
if ($libraryFrameworks.Count -eq 0) { throw 'OmniEurope.Blazor.csproj declares no TargetFrameworks.' }
foreach ($framework in $libraryFrameworks) {
    $resolved = $libraryLock.dependencies.PSObject.Properties[$framework]
    if ($null -eq $resolved) { throw "Library lock has no $framework section." }
    foreach ($reference in @($libraryProject.Project.ItemGroup.PackageReference)) {
        $id = [string]$reference.Include
        $reviewed = $policy.packages.$id
        $expected = if ($reviewed.frameworkVersions -and $reviewed.frameworkVersions.PSObject.Properties[$framework]) {
            [string]$reviewed.frameworkVersions.$framework.version
        } else { [string]$reviewed.version }
        $entry = $resolved.Value.PSObject.Properties[$id]
        if ($null -eq $entry -or [string]$entry.Value.resolved -ne $expected) {
            throw "Library lock resolves $id at $($entry.Value.resolved) for $framework, reviewed $expected."
        }
    }
}

$hybridLock = Get-Content -LiteralPath (Join-Path $repoRoot 'samples/OmniEurope.Blazor.HybridSmoke/packages.lock.json') -Raw | ConvertFrom-Json
$hybridFramework = @($hybridLock.dependencies.PSObject.Properties | Where-Object Name -Like 'net11.0-windows*')[0].Value
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

# Container images: a tag moves under the same name, so a run could test another image than the reviewed one.
$imagePatterns = @('^\s*image:\s*["'']?([^\s"''#]+)', 'Builder\("([^"]+)"\)')
$pinnedImage = '^[^@\s]+:[^@\s]+@sha256:[0-9a-f]{64}$'
foreach ($fixture in @(
    @{ Line = '    image: postgres:18-alpine'; Pinned = $false },
    @{ Line = '    image: "postgres:18-alpine"'; Pinned = $false },
    @{ Line = 'new PostgreSqlBuilder("postgres:18-alpine").Build();'; Pinned = $false },
    @{ Line = "    image: postgres:18-alpine@sha256:$('0' * 64)"; Pinned = $true },
    @{ Line = "new PostgreSqlBuilder(`"postgres:18-alpine@sha256:$('0' * 64)`").Build();"; Pinned = $true }
)) {
    $found = @($imagePatterns | ForEach-Object { if ($fixture.Line -match $_) { $Matches[1] } })
    if ($found.Count -ne 1) { throw "Image parser fixture was not recognized: $($fixture.Line)" }
    if (($found[0] -cmatch $pinnedImage) -ne $fixture.Pinned) { throw "Image parser fixture produced the wrong verdict: $($fixture.Line)" }
}

$reviewedImages = @($policy.images.PSObject.Properties | ForEach-Object { [string]$_.Value.reference })
foreach ($reference in $reviewedImages) {
    if ($reference -cnotmatch $pinnedImage) { throw "Policy image is not pinned by digest: $reference" }
}
$imageFiles = @(
    $workflowFiles
    Get-ChildItem -LiteralPath (Join-Path $repoRoot 'deploy') -Recurse -File -Include '*.yml', '*.yaml'
    Get-ChildItem -LiteralPath (Join-Path $repoRoot 'tests') -Recurse -File -Filter '*.cs' |
        Where-Object FullName -NotMatch '[\\/](bin|obj)[\\/]'
)
$usedImages = [System.Collections.Generic.HashSet[string]]::new()
foreach ($file in $imageFiles) {
    $lineNumber = 0
    foreach ($line in Get-Content -LiteralPath $file.FullName) {
        $lineNumber++
        foreach ($pattern in $imagePatterns) {
            if ($line -notmatch $pattern) { continue }
            $reference = $Matches[1]
            # Builder("...") also matches other builders of the tests: only an image reference has a registry name and a tag.
            if ($reference -notmatch '^[a-z0-9./_-]+(:|@)') { continue }
            if ($reference -cnotin $reviewedImages) {
                throw "Unreviewed or mutable image in $($file.Name):$($lineNumber): $reference"
            }
            [void]$usedImages.Add($reference)
        }
    }
}
foreach ($reference in $reviewedImages) {
    if (-not $usedImages.Contains($reference)) { throw "Policy image is used nowhere: $reference" }
}

Write-Host $messages.DependencyPolicyPassed

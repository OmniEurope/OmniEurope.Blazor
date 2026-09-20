#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build and launch the OmniEurope.Blazor hosts (Server catalog + WebAssembly showcase), run the tests.
.DESCRIPTION
    Reduced web launcher of the _Generic kit (PLAN-001, decision 7): a component library has no
    back end and no database, so only the two browsable hosts are started. This file holds only
    what is specific to the repository: the parameters, the PowerShell 7 trampoline and
    $LaunchConfig. All the mechanics live in scripts\ylaunch-core.ps1, a verbatim copy of the
    kit's versioned core (never edit the copy).
    Contract: _Generic docs/contracts/deployment.md, "Launcher contract". .\ylaunch.ps1 -hl for the workflow.

    Not covered here (still run by hand or by CI, see docs/testing.md and .github/workflows/ci.yml):
    the Release gates of eng/ (Test-SdkBand, Test-CatalogHost, Test-WasmHost, Test-AutoHost,
    Test-HybridHost, Test-ShowcaseHost, Test-Csp, Test-PublicApi, Test-Budgets, Test-Package,
    Test-DependencyPolicy, SBOM). The launcher builds and tests in Debug; CI validates in Release.
.EXAMPLE
    .\ylaunch.ps1 -s          Build + start the catalog and the showcase, no browser
    .\ylaunch.ps1 -t          Build + every unit suite (library and analyzers) + exit
    .\ylaunch.ps1 -tl         Build + the library suite only + exit
    .\ylaunch.ps1 -w          Give this worktree its own ports, then start
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [Alias("s")]   [switch]$Silent,
    [Alias("r")]   [switch]$Reset,
    [Alias("t")]   [switch]$TestUnit,
    [Alias("ta")]  [switch]$TestAll,
    [Alias("tl")]  [switch]$TestLibrary,
    [Alias("tg")]  [switch]$TestAnalyzers,
    [Alias("c")]   [switch]$Coverage,
    [Alias("hr")]  [switch]$HotReload,
    [Alias("w")]   [switch]$Worktree,
    [Alias("h")]   [switch]$Help,
    [Alias("hl")]  [switch]$HelpLong
)

# PowerShell 5.1 parses the whole file before running it, so the core (PS 7 syntax) is only
# dot-sourced after this gate.
if ($PSVersionTable.PSVersion -lt [version]"7.2") {
    $pwshPath = Get-Command pwsh -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
    if (-not $pwshPath -or $PSVersionTable.PSEdition -ne "Desktop") {
        Write-Host "ERROR: PowerShell 7.2+ required. Install from https://aka.ms/powershell" -ForegroundColor Red
        exit 1
    }
    $boundArgs = @()
    foreach ($key in $PSBoundParameters.Keys) {
        $val = $PSBoundParameters[$key]
        if ($val -is [switch]) { if ($val) { $boundArgs += "-$key" } }
        else { $boundArgs += "-$key"; $boundArgs += ($(if ($val -is [array]) { [string]::Join(',', $val) } else { "$val" })) }
    }
    & $pwshPath -NoLogo -NoProfile -File $PSCommandPath @boundArgs
    exit $LASTEXITCODE
}

# ---------- project configuration (the only project-specific part) ----------
# Paths are relative to this file. {KEY} expands to the port of that .ylaunch.local key and
# {URL:Component} to a component's first URL. No Database block: -r has nothing to reset.
$LaunchConfig = @{
    Name     = "OmniEurope.Blazor"
    Solution = "OmniEurope.Blazor.slnx"
    # Folders whose processes belong to this checkout (image path or `dotnet run` project path under
    # them): the next launch stops them, nothing else. The library lives in src\, but the two
    # browsable hosts live in samples\ and site\, so all three are owned; the root itself never is
    # (a worktree nested under .claude\worktrees\ would then be claimed by its parent checkout).
    OwnedFolders = @("src", "samples", "site")
    Web = @{
        Components = @(
            @{ Key = "Catalog"; Project = "samples\OmniEurope.Blazor.Catalog\OmniEurope.Blazor.Catalog.csproj"; Health = "/"
               Urls = @(@{ Key = "CATALOG_PORT"; Default = 5270; Scheme = "http" }) }
            @{ Key = "Showcase"; Project = "site\OmniEurope.Blazor.Showcase\OmniEurope.Blazor.Showcase.csproj"; Health = "/"; Browser = $true
               Urls = @(@{ Key = "SHOWCASE_PORT"; Default = 5280; Scheme = "http" }) }
        )
    }
    Tests = @(
        @{ Key = "Library"; Flag = "TestLibrary"; Alias = "tl"; Kind = "Unit"; Coverage = $true; Project = "tests\OmniEurope.Blazor.Tests\OmniEurope.Blazor.Tests.csproj" }
        @{ Key = "Analyzers"; Flag = "TestAnalyzers"; Alias = "tg"; Kind = "Unit"; Coverage = $true; Project = "eng\OmniEurope.Analyzers.Tests\OmniEurope.Analyzers.Tests.csproj" }
    )
}

$ErrorActionPreference = "Stop"
$corePath = Join-Path $PSScriptRoot "scripts\ylaunch-core.ps1"
try { . $corePath } catch { Write-Host "ERROR: cannot load ${corePath}: $($_.Exception.Message)" -ForegroundColor Red; exit 1 }
$options = @{}
foreach ($key in $PSBoundParameters.Keys) { $options[$key] = $PSBoundParameters[$key] }
Invoke-YLaunch -Config $LaunchConfig -Root $PSScriptRoot -Options $options
exit $script:YLaunchExitCode

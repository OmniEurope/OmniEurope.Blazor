[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string[]]$LockFile
)

# Downloads every package pinned by a lock file into the global NuGet folder, without building or
# even evaluating the project that owns it. The SBOM reads nuspec and licence files straight from
# that folder, so it needs the packages of projects a given CI job never restores: the Hybrid smoke
# host targets Windows and stays out of the solution, so the Linux job that runs the inventory has
# no other way to see its packages.

$ErrorActionPreference = 'Stop'
$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
$repoRoot = Split-Path -Parent $PSScriptRoot
$configFile = Join-Path $repoRoot 'NuGet.Config'

$wanted = [ordered]@{}
foreach ($path in $LockFile) {
    $resolvedPath = (Resolve-Path -LiteralPath $path).Path
    $document = Get-Content -LiteralPath $resolvedPath -Raw | ConvertFrom-Json
    foreach ($framework in $document.dependencies.PSObject.Properties) {
        foreach ($package in $framework.Value.PSObject.Properties) {
            # Project references carry no resolved version and nothing to download.
            $version = [string]$package.Value.resolved
            if ([string]::IsNullOrWhiteSpace($version)) { continue }
            $wanted["$($package.Name)|$version"] = [pscustomobject]@{
                Id = $package.Name
                Version = $version
            }
        }
    }
}

if ($wanted.Count -eq 0) { throw 'The lock files declare no downloadable package.' }

# The project is written outside the repository on purpose: a Directory.Build.props or a central
# package management file would otherwise apply to it and change what gets restored.
$stagingRoot = Join-Path ([IO.Path]::GetTempPath()) ("omnieurope-download-" + [Guid]::NewGuid().ToString('n'))
New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null
try {
    $downloads = foreach ($package in $wanted.Values) {
        # PackageDownload takes an exact bracketed version, which is what a lock file pins.
        '    <PackageDownload Include="{0}" Version="[{1}]" />' -f $package.Id, $package.Version
    }
    $project = @(
        '<Project Sdk="Microsoft.NET.Sdk">'
        '  <PropertyGroup>'
        '    <TargetFramework>net10.0</TargetFramework>'
        '    <EnableDefaultItems>false</EnableDefaultItems>'
        '    <RestorePackagesWithLockFile>false</RestorePackagesWithLockFile>'
        '  </PropertyGroup>'
        '  <ItemGroup>'
        $downloads
        '  </ItemGroup>'
        '</Project>'
    )
    $projectPath = Join-Path $stagingRoot 'LockedPackageDownload.csproj'
    $project | Set-Content -LiteralPath $projectPath -Encoding utf8

    & dotnet restore $projectPath --configfile $configFile
    if ($LASTEXITCODE -ne 0) { throw ($psText.LockedPackagesFailed -f $LASTEXITCODE) }
    Write-Host ($psText.LockedPackagesRestored -f $wanted.Count, $LockFile.Count)
}
finally {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force -ErrorAction SilentlyContinue
}

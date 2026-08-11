[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$docsRoot = Join-Path $repoRoot 'docs'
$licenseRoot = Join-Path $docsRoot 'third-party-licenses'
$messages = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
[xml]$packageProject = Get-Content -LiteralPath (Join-Path $repoRoot 'src/OmniEurope.Blazor/OmniEurope.Blazor.csproj') -Raw
$packageVersion = [string]$packageProject.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($packageVersion)) { throw 'The package project version is missing.' }
$packageRoot = if ($env:NUGET_PACKAGES) {
    $env:NUGET_PACKAGES
} else {
    Join-Path $env:USERPROFILE '.nuget/packages'
}

function Get-RelativePath([string]$Path) {
    return [IO.Path]::GetRelativePath($repoRoot, $Path).Replace('\', '/')
}

function Get-LockedPackages {
    $records = @{}
    $locks = @(Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Filter 'packages.lock.json' |
        Where-Object FullName -NotMatch '[\\/](bin|obj|artifacts|\.git|\.claude)[\\/]')
    foreach ($lock in $locks) {
        $relativeLock = Get-RelativePath $lock.FullName
        $document = Get-Content -LiteralPath $lock.FullName -Raw | ConvertFrom-Json
        foreach ($framework in $document.dependencies.PSObject.Properties) {
            foreach ($package in $framework.Value.PSObject.Properties) {
                $version = [string]$package.Value.resolved
                if ([string]::IsNullOrWhiteSpace($version)) { continue }
                $key = "$($package.Name.ToLowerInvariant())|$($version.ToLowerInvariant())"
                if (-not $records.ContainsKey($key)) {
                    $records[$key] = [ordered]@{
                        id = $package.Name
                        version = $version
                        lockFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
                    }
                }
                [void]$records[$key].lockFiles.Add($relativeLock)
            }
        }
    }
    return @($records.Values | Sort-Object @{ Expression = { $_.id.ToLowerInvariant() } }, @{ Expression = { $_.version } })
}

New-Item -ItemType Directory -Path $licenseRoot -Force | Out-Null
$expectedLicenseFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$packages = @(Get-LockedPackages)
$registry = @()
$components = @()

foreach ($package in $packages) {
    $packageDirectory = Join-Path $packageRoot ($package.id.ToLowerInvariant() + '/' + $package.version.ToLowerInvariant())
    $nuspecPath = Join-Path $packageDirectory ($package.id.ToLowerInvariant() + '.nuspec')
    if (-not (Test-Path -LiteralPath $nuspecPath)) {
        throw "NuGet metadata is missing for $($package.id) $($package.version): $nuspecPath"
    }

    [xml]$nuspec = Get-Content -LiteralPath $nuspecPath -Raw
    $metadata = $nuspec.package.metadata
    $licenseNode = $metadata.license
    $licenseKind = $null
    $licenseValue = $null
    $licenseUrl = $null
    $localLicense = $null
    $localLicenseHash = $null

    if ($null -ne $licenseNode -and -not [string]::IsNullOrWhiteSpace([string]$licenseNode.InnerText)) {
        $licenseKind = [string]$licenseNode.type
        $licenseValue = [string]$licenseNode.InnerText
        if ($licenseKind -eq 'file') {
            $canonicalPackageDirectory = [IO.Path]::GetFullPath($packageDirectory).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
            $sourceLicense = [IO.Path]::GetFullPath((Join-Path $packageDirectory $licenseValue))
            if (-not $sourceLicense.StartsWith($canonicalPackageDirectory, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Declared license file escapes the package directory for $($package.id) $($package.version): $licenseValue"
            }
            if (-not (Test-Path -LiteralPath $sourceLicense -PathType Leaf)) {
                throw "Declared license file is missing for $($package.id) $($package.version): $licenseValue"
            }
            $safeId = $package.id -replace '[^A-Za-z0-9._-]', '_'
            $safeVersion = $package.version -replace '[^A-Za-z0-9._-]', '_'
            $safeName = [IO.Path]::GetFileName($licenseValue) -replace '[^A-Za-z0-9._-]', '_'
            $targetName = "$safeId--$safeVersion--$safeName"
            $targetLicense = Join-Path $licenseRoot $targetName
            Copy-Item -LiteralPath $sourceLicense -Destination $targetLicense -Force
            [void]$expectedLicenseFiles.Add($targetName)
            $localLicense = "docs/third-party-licenses/$targetName"
            $localLicenseHash = (Get-FileHash -LiteralPath $targetLicense -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    } elseif (-not [string]::IsNullOrWhiteSpace([string]$metadata.licenseUrl)) {
        $licenseKind = 'url'
        $licenseValue = [string]$metadata.licenseUrl
        $licenseUrl = $licenseValue
    } else {
        throw "No license declaration exists for $($package.id) $($package.version)."
    }

    $lockFiles = @($package.lockFiles | Sort-Object)
    $record = [ordered]@{
        id = $package.id
        version = $package.version
        license = [ordered]@{
            kind = $licenseKind
            value = $licenseValue
            url = $licenseUrl
            localFile = $localLicense
            localFileSha256 = $localLicenseHash
        }
        lockFiles = $lockFiles
    }
    $registry += $record

    $licenseChoice = if ($licenseKind -eq 'expression') {
        [ordered]@{ expression = $licenseValue }
    } else {
        $license = [ordered]@{ name = $licenseValue }
        if ($licenseUrl) { $license.url = $licenseUrl }
        [ordered]@{ license = $license }
    }
    $properties = @(
        [ordered]@{ name = 'omnieurope:license-kind'; value = $licenseKind },
        [ordered]@{ name = 'omnieurope:lock-files'; value = ($lockFiles -join ';') }
    )
    if ($localLicense) {
        $properties += [ordered]@{ name = 'omnieurope:license-file'; value = $localLicense }
        $properties += [ordered]@{ name = 'omnieurope:license-file-sha256'; value = $localLicenseHash }
    }
    $escapedId = [Uri]::EscapeDataString($package.id)
    $escapedVersion = [Uri]::EscapeDataString($package.version)
    $components += [ordered]@{
        type = 'library'
        name = $package.id
        version = $package.version
        'bom-ref' = "pkg:nuget/$escapedId@$escapedVersion"
        purl = "pkg:nuget/$escapedId@$escapedVersion"
        licenses = @($licenseChoice)
        properties = $properties
    }
}

foreach ($existing in @(Get-ChildItem -LiteralPath $licenseRoot -File)) {
    if (-not $expectedLicenseFiles.Contains($existing.Name)) {
        Remove-Item -LiteralPath $existing.FullName -Force
    }
}

$sbom = [ordered]@{
    bomFormat = 'CycloneDX'
    specVersion = '1.6'
    version = 1
    metadata = [ordered]@{
        timestamp = '2026-08-11T00:00:00Z'
        component = [ordered]@{
            type = 'library'
            name = 'OmniEurope.Blazor'
            version = $packageVersion
        }
        properties = @(
            [ordered]@{ name = 'omnieurope:source'; value = 'packages.lock.json' },
            [ordered]@{ name = 'omnieurope:generator'; value = 'eng/Generate-Sbom.ps1' }
        )
    }
    components = $components
}

$sbom | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $docsRoot 'sbom.cdx.json') -Encoding utf8
[ordered]@{ schemaVersion = 1; generatedFrom = 'packages.lock.json'; packages = $registry } |
    ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $docsRoot 'third-party-packages.json') -Encoding utf8

$notice = [Collections.Generic.List[string]]::new()
$notice.Add($messages.NoticeTitle)
$notice.Add('')
$notice.Add($messages.NoticeIntro)
$notice.Add('')
$notice.Add($messages.NoticeIndependent)
$notice.Add('')
$notice.Add($messages.NoticeLicenseFiles)
$notice.Add('')
$notice.Add($messages.NoticeHeader)
$notice.Add('|---|---:|---|---|---|')
foreach ($item in $registry) {
    $value = ([string]$item.license.value).Replace('|', '\|')
    $local = if ($item.license.localFile) { "``$($item.license.localFile)``" } else { '-' }
    $notice.Add("| ``$($item.id)`` | ``$($item.version)`` | $value | ``$($item.license.kind)`` | $local |")
}
$notice.Add('')
$notice.Add($messages.NoticeUrlDisclaimer)
$notice | Set-Content -LiteralPath (Join-Path $repoRoot 'NOTICE.md') -Encoding utf8

Write-Host ($messages.SbomWritten -f $registry.Count)

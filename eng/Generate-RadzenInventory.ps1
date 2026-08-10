[CmdletBinding()]
param(
    [string]$WorkspaceRoot = 'C:\Dev',
    [string]$MarkdownPath = (Join-Path $PSScriptRoot '..\docs\component-inventory.md'),
    [string]$JsonPath = (Join-Path $PSScriptRoot '..\docs\component-inventory.json')
)

$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path -LiteralPath $WorkspaceRoot).Path.TrimEnd('\')
$excludedSegments = '(\\|/)(bin|obj|node_modules|packages|artifacts|\.git|\.vs)(\\|/)'

function Get-RelativePath {
    param([string]$Path)

    return [System.IO.Path]::GetRelativePath($workspace, $Path).Replace('\', '/')
}

function Get-ProjectStatus {
    param([string]$RelativePath)

    if ($RelativePath.StartsWith('_github/', [StringComparison]::OrdinalIgnoreCase)) { return 'miroir' }
    if ($RelativePath.StartsWith('_Generic/', [StringComparison]::OrdinalIgnoreCase)) { return 'modèle' }
    if ($RelativePath.StartsWith('On hold/', [StringComparison]::OrdinalIgnoreCase)) { return 'archivé' }
    return 'actif'
}

function Get-RadzenVersion {
    param([System.IO.FileInfo]$Project)

    $projectContent = [string](Get-Content -Raw -LiteralPath $Project.FullName)
    $reference = [regex]::Match(
        $projectContent,
        '(?is)<PackageReference\b[^>]*\bInclude\s*=\s*["'']Radzen\.Blazor["''][^>]*(?:/>|>.*?</PackageReference>)')

    if (-not $reference.Success) { return $null }

    $version = [regex]::Match($reference.Value, '(?i)\bVersion\s*=\s*["'']([^"'']+)["'']')
    if ($version.Success) { return $version.Groups[1].Value }

    $nestedVersion = [regex]::Match($reference.Value, '(?is)<Version>([^<]+)</Version>')
    if ($nestedVersion.Success) { return $nestedVersion.Groups[1].Value.Trim() }

    $directory = $Project.Directory
    while ($directory -and $directory.FullName.StartsWith($workspace, [StringComparison]::OrdinalIgnoreCase)) {
        $centralFile = Join-Path $directory.FullName 'Directory.Packages.props'
        if (Test-Path -LiteralPath $centralFile) {
            $centralContent = [string](Get-Content -Raw -LiteralPath $centralFile)
            $centralVersion = [regex]::Match(
                $centralContent,
                '(?is)<PackageVersion\b[^>]*\bInclude\s*=\s*["'']Radzen\.Blazor["''][^>]*\bVersion\s*=\s*["'']([^"'']+)["'']')
            if ($centralVersion.Success) { return $centralVersion.Groups[1].Value }
        }
        $directory = $directory.Parent
    }

    return 'non précisée'
}

$projectFiles = Get-ChildItem -LiteralPath $workspace -Recurse -Filter *.csproj -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch $excludedSegments }

$projectRoots = @($projectFiles | ForEach-Object {
    [pscustomobject]@{
        File = $_
        Root = $_.Directory.FullName.TrimEnd('\')
        RelativePath = Get-RelativePath $_.FullName
    }
} | Sort-Object { $_.Root.Length } -Descending)

$usageByProject = @{}
$fileCountByProject = @{}

$razorFiles = Get-ChildItem -LiteralPath $workspace -Recurse -Filter *.razor -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch $excludedSegments }

foreach ($razorFile in $razorFiles) {
    $owner = $null
    foreach ($candidate in $projectRoots) {
        $prefix = $candidate.Root + '\'
        if ($razorFile.FullName.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
            $owner = $candidate
            break
        }
    }

    if ($null -eq $owner) { continue }

    $content = [string](Get-Content -Raw -LiteralPath $razorFile.FullName)
    $content = [regex]::Replace($content, '(?s)@\*.*?\*@', '')
    $content = [regex]::Replace($content, '(?s)<!--.*?-->', '')
    $matches = [regex]::Matches($content, '<\s*(Radzen[A-Z][A-Za-z0-9]*)\b')
    if ($matches.Count -eq 0) { continue }

    $key = $owner.File.FullName
    if (-not $usageByProject.ContainsKey($key)) {
        $usageByProject[$key] = @{}
        $fileCountByProject[$key] = 0
    }

    $fileCountByProject[$key]++
    foreach ($match in $matches) {
        $component = $match.Groups[1].Value
        if (-not $usageByProject[$key].ContainsKey($component)) {
            $usageByProject[$key][$component] = 0
        }
        $usageByProject[$key][$component]++
    }
}

$projects = foreach ($project in $projectRoots) {
    $version = Get-RadzenVersion $project.File
    $usage = if ($usageByProject.ContainsKey($project.File.FullName)) { $usageByProject[$project.File.FullName] } else { @{} }
    if ($null -eq $version -and $usage.Count -eq 0) { continue }

    $components = @($usage.GetEnumerator() | Sort-Object Name | ForEach-Object {
        [pscustomobject]@{ name = $_.Key; occurrences = $_.Value }
    })

    [pscustomobject]@{
        project = [System.IO.Path]::GetFileNameWithoutExtension($project.File.Name)
        path = $project.RelativePath
        status = Get-ProjectStatus $project.RelativePath
        radzenVersion = if ($null -eq $version) { 'usage transitif' } else { $version }
        distinctComponents = $components.Count
        occurrences = [int](($components | Measure-Object -Property occurrences -Sum).Sum)
        razorFiles = if ($fileCountByProject.ContainsKey($project.File.FullName)) { $fileCountByProject[$project.File.FullName] } else { 0 }
        components = $components
    }
}

$projects = @($projects | Sort-Object @{ Expression = { switch ($_.status) { 'actif' { 0 } 'modèle' { 1 } 'archivé' { 2 } default { 3 } } } }, @{ Expression = 'distinctComponents'; Descending = $true }, path)
$activeProjects = @($projects | Where-Object status -eq 'actif')
$activeUsageProjects = @($activeProjects | Where-Object distinctComponents -gt 0)

$componentNames = @($projects.components.name | Sort-Object -Unique)
$catalog = foreach ($componentName in $componentNames) {
    $consumers = @($projects | Where-Object { $_.components.name -contains $componentName })
    $activeConsumers = @($consumers | Where-Object status -eq 'actif')
    $activeOccurrences = [int](($activeConsumers.components | Where-Object name -eq $componentName | Measure-Object -Property occurrences -Sum).Sum)
    $occurrences = [int](($consumers.components | Where-Object name -eq $componentName | Measure-Object -Property occurrences -Sum).Sum)
    [pscustomobject]@{
        component = $componentName
        activeProjects = $activeConsumers.Count
        allProjects = $consumers.Count
        activeOccurrences = $activeOccurrences
        occurrences = $occurrences
        consumers = @($consumers.path)
    }
}
$catalog = @($catalog | Sort-Object @{ Expression = 'activeProjects'; Descending = $true }, @{ Expression = 'occurrences'; Descending = $true }, component)

$generatedAt = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ssK')
$inventory = [ordered]@{
    generatedAt = $generatedAt
    workspaceRoot = $workspace
    method = 'Balises Radzen* dans les fichiers .razor, commentaires Razor et HTML exclus.'
    projects = $projects
    catalog = $catalog
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# Inventaire des composants Radzen')
$lines.Add('')
$lines.Add("Généré le $generatedAt à partir de ``$workspace``.")
$lines.Add('')
$lines.Add('Méthode : balises `Radzen*` réellement présentes dans les fichiers `.razor`; commentaires Razor et HTML exclus. Les sorties de build, dépendances et dossiers techniques ne sont pas parcourus. Les projets marqués « miroir », « modèle » ou « archivé » restent visibles mais ne déterminent pas la priorité active.')
$lines.Add('')
$activeComponentCount = @($catalog | Where-Object activeProjects -gt 0).Count
$lines.Add("Résumé : **$($activeUsageProjects.Count) projets actifs avec usages Razor**, **$($activeProjects.Count) projets actifs avec usage ou dépendance**, **$activeComponentCount composants distincts actifs** et **$($catalog.Count) composants distincts** sur l'ensemble du parc observé.")
$lines.Add('')
$lines.Add('## Projets')
$lines.Add('')
$lines.Add('| Statut | Projet | Version Radzen | Composants distincts | Occurrences | Fichiers Razor |')
$lines.Add('|---|---|---:|---:|---:|---:|')
foreach ($project in $projects) {
    $lines.Add("| $($project.status) | ``$($project.path)`` | $($project.radzenVersion) | $($project.distinctComponents) | $($project.occurrences) | $($project.razorFiles) |")
}

$lines.Add('')
$lines.Add('## Catalogue global')
$lines.Add('')
$lines.Add('| Composant observé | Projets actifs | Tous projets | Occurrences actives | Toutes occurrences |')
$lines.Add('|---|---:|---:|---:|---:|')
foreach ($entry in $catalog) {
    $lines.Add("| ``$($entry.component)`` | $($entry.activeProjects) | $($entry.allProjects) | $($entry.activeOccurrences) | $($entry.occurrences) |")
}

$lines.Add('')
$lines.Add('## Détail par projet')
foreach ($project in $projects | Where-Object { $_.distinctComponents -gt 0 }) {
    $lines.Add('')
    $lines.Add("### $($project.path)")
    $lines.Add('')
    $detail = $project.components | ForEach-Object { "``$($_.name)`` ($($_.occurrences))" }
    $lines.Add(($detail -join ', '))
}

$lines.Add('')
$lines.Add('## Régénération')
$lines.Add('')
$lines.Add('```powershell')
$lines.Add('.\eng\Generate-RadzenInventory.ps1 -WorkspaceRoot C:\Dev')
$lines.Add('```')
$lines.Add('')
$lines.Add('Le JSON voisin contient les mêmes données sous une forme exploitable par des outils de planification.')

$markdownTarget = [System.IO.Path]::GetFullPath($MarkdownPath)
$jsonTarget = [System.IO.Path]::GetFullPath($JsonPath)
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($markdownTarget)) | Out-Null
[System.IO.File]::WriteAllLines($markdownTarget, $lines, [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText($jsonTarget, ($inventory | ConvertTo-Json -Depth 8), [System.Text.UTF8Encoding]::new($false))

Write-Host "Inventaire écrit : $markdownTarget"
Write-Host "Données JSON écrites : $jsonTarget"
Write-Host "Projets actifs avec usages Razor : $($activeUsageProjects.Count); composants distincts : $($catalog.Count)."

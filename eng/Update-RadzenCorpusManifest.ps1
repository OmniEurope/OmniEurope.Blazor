[CmdletBinding()]
param(
    [string]$WorkspaceRoot = 'C:\Dev',
    [string]$InventoryPath = (Join-Path $PSScriptRoot '..\docs\component-inventory.json'),
    [string]$ManifestPath = (Join-Path $PSScriptRoot '..\docs\radzen-corpus.json')
)

$ErrorActionPreference = 'Stop'
$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
$workspace = (Resolve-Path -LiteralPath $WorkspaceRoot).Path.TrimEnd('\')
$inventory = Get-Content -Raw -LiteralPath $InventoryPath | ConvertFrom-Json
$allowedStatuses = $psText.StatusActive, $psText.StatusModel, $psText.StatusArchived, $psText.StatusMirror
$excluded = '(\\|/)(bin|obj|node_modules|packages|artifacts|\.git|\.vs|\.claude)(\\|/)'
$extensions = '.cs', '.razor', '.css', '.js', '.json', '.resx', '.xml', '.csproj'

function Get-Sha256 {
    param([string]$Path)
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-TextSha256 {
    param([string]$Value)
    $bytes = [Text.Encoding]::UTF8.GetBytes($Value)
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}

function Get-Revision {
    param([string]$Directory)
    $candidate = Get-Item -LiteralPath $Directory
    while ($candidate -and $candidate.FullName.StartsWith($workspace, [StringComparison]::OrdinalIgnoreCase)) {
        if (Test-Path -LiteralPath (Join-Path $candidate.FullName '.git')) {
            $revision = & git -c "safe.directory=$($candidate.FullName)" -C $candidate.FullName rev-parse HEAD 2>$null
            if ($LASTEXITCODE -eq 0 -and $revision) { return [string]$revision }
            return 'unversioned'
        }
        $candidate = $candidate.Parent
    }
    return 'unversioned'
}

$seedProjects = @($inventory.projects | Group-Object path | ForEach-Object { $_.Group[0] })
$duplicates = @($inventory.projects | Group-Object path | Where-Object Count -gt 1)
if ($duplicates.Count -gt 0) { throw "Duplicate project paths in seed inventory: $($duplicates.Name -join ', ')" }

$projectRoots = @($seedProjects | ForEach-Object {
    if ($_.status -notin $allowedStatuses) { throw "Invalid corpus status '$($_.status)' for $($_.path)." }
    $projectPath = Join-Path $workspace ([string]$_.path).Replace('/', '\')
    if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) { throw "Missing corpus project: $projectPath" }
    [pscustomobject]@{
        Seed = $_
        ProjectPath = (Resolve-Path -LiteralPath $projectPath).Path
        Root = (Split-Path -Parent (Resolve-Path -LiteralPath $projectPath).Path).TrimEnd('\')
    }
} | Sort-Object { $_.Root.Length } -Descending)

$ownedFiles = @{}
foreach ($project in $projectRoots) {
    $files = Get-ChildItem -LiteralPath $project.Root -Recurse -File -ErrorAction Stop | Where-Object {
        $_.FullName -notmatch $excluded -and $_.Extension -in $extensions
    }
    foreach ($file in $files) {
        if (-not $ownedFiles.ContainsKey($file.FullName)) { $ownedFiles[$file.FullName] = $project.ProjectPath }
    }
}

$projects = foreach ($project in $projectRoots | Sort-Object { $_.Seed.path }) {
    $sources = @($ownedFiles.GetEnumerator() | Where-Object Value -eq $project.ProjectPath | ForEach-Object {
        $relative = [IO.Path]::GetRelativePath($workspace, $_.Key).Replace('\', '/')
        [pscustomobject]@{ path = $relative; sha256 = Get-Sha256 $_.Key }
    } | Sort-Object path)
    $projectRelative = [IO.Path]::GetRelativePath($workspace, $project.ProjectPath).Replace('\', '/')
    $projectHash = Get-Sha256 $project.ProjectPath
    $fingerprint = Get-TextSha256 (($sources | ForEach-Object { "$($_.path):$($_.sha256)" }) -join "`n")
    [pscustomobject]@{
        path = $projectRelative
        logicalRoot = [IO.Path]::GetDirectoryName($projectRelative).Replace('\', '/')
        status = [string]$project.Seed.status
        revision = Get-Revision $project.Root
        projectSha256 = $projectHash
        inputSha256 = $fingerprint
        sourceFiles = $sources
    }
}

$manifest = [ordered]@{
    schemaVersion = 1
    generatedAt = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ssK')
    workspaceRoot = $workspace
    tool = 'eng/Update-RadzenCorpusManifest.ps1'
    exclusions = @('bin', 'obj', 'node_modules', 'packages', 'artifacts', '.git', '.vs', '.claude')
    projects = @($projects)
}

$target = [IO.Path]::GetFullPath($ManifestPath)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
[IO.File]::WriteAllText($target, ($manifest | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
Write-Host "Corpus manifest written: $target ($($projects.Count) projects, $(@($projects.sourceFiles).Count) files)."

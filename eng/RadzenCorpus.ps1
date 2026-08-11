$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')

function Read-RadzenCorpus {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$ManifestPath,
        [Parameter(Mandatory)] [string]$WorkspaceRoot,
        [bool]$VerifyFiles = $true
    )

    $manifestFile = (Resolve-Path -LiteralPath $ManifestPath).Path
    $workspace = (Resolve-Path -LiteralPath $WorkspaceRoot).Path.TrimEnd('\')
    $manifest = Get-Content -Raw -LiteralPath $manifestFile | ConvertFrom-Json
    if ($manifest.schemaVersion -ne 1) { throw "Unsupported Radzen corpus schema: $($manifest.schemaVersion)" }
    if (-not [IO.Path]::GetFullPath($manifest.workspaceRoot).TrimEnd('\').Equals($workspace, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Corpus workspace mismatch: manifest=$($manifest.workspaceRoot), requested=$workspace"
    }

    $allowedStatuses = $psText.StatusActive, $psText.StatusModel, $psText.StatusArchived, $psText.StatusMirror
    $projectDuplicates = @($manifest.projects | Group-Object path | Where-Object Count -gt 1)
    if ($projectDuplicates.Count -gt 0) { throw "Duplicate corpus projects: $($projectDuplicates.Name -join ', ')" }
    $sourceEntries = [System.Collections.Generic.List[object]]::new()
    $seenSources = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($project in $manifest.projects) {
        if ($project.status -notin $allowedStatuses) { throw "Invalid corpus status '$($project.status)' for $($project.path)." }
        $projectFile = Join-Path $workspace ([string]$project.path).Replace('/', '\')
        if ($VerifyFiles) {
            if (-not (Test-Path -LiteralPath $projectFile -PathType Leaf)) { throw "Missing corpus project: $projectFile" }
            $projectHash = (Get-FileHash -LiteralPath $projectFile -Algorithm SHA256).Hash.ToLowerInvariant()
            if ($projectHash -ne $project.projectSha256) { throw "Corpus project hash mismatch: $($project.path)" }
        }

        if ([string]$project.revision -notmatch '^(unversioned|[0-9a-f]{40,64})$') { throw "Invalid corpus revision: $($project.path)" }
        $fingerprintLines = [System.Collections.Generic.List[string]]::new()
        foreach ($source in $project.sourceFiles) {
            if (-not $seenSources.Add([string]$source.path)) { throw "Duplicate corpus source: $($source.path)" }
            $fullPath = Join-Path $workspace ([string]$source.path).Replace('/', '\')
            if ($VerifyFiles) {
                if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { throw "Missing corpus source: $fullPath" }
                $hash = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToLowerInvariant()
                if ($hash -ne $source.sha256) { throw "Corpus source hash mismatch: $($source.path)" }
            }
            $fingerprintLines.Add("$($source.path):$($source.sha256)")
            $sourceEntries.Add([pscustomobject]@{
                Path = [string]$source.path
                FullPath = $fullPath
                Sha256 = [string]$source.sha256
                Status = [string]$project.status
                Project = [string]$project.path
            })
        }
        $fingerprintBytes = [Text.Encoding]::UTF8.GetBytes(($fingerprintLines -join "`n"))
        $fingerprint = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($fingerprintBytes)).ToLowerInvariant()
        if ($fingerprint -ne $project.inputSha256) { throw "Corpus input fingerprint mismatch: $($project.path)" }
    }

    return [pscustomobject]@{
        Manifest = $manifest
        ManifestPath = $manifestFile
        ManifestSha256 = (Get-FileHash -LiteralPath $manifestFile -Algorithm SHA256).Hash.ToLowerInvariant()
        Workspace = $workspace
        Files = @($sourceEntries)
    }
}

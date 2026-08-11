[CmdletBinding()]
param(
    [string]$WorkspaceRoot = 'C:\Dev',
    [string]$ManifestPath = (Join-Path $PSScriptRoot '..\docs\radzen-corpus.json'),
    [switch]$VerifyFiles
)

$ErrorActionPreference = 'Stop'
$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
. (Join-Path $PSScriptRoot 'RadzenCorpus.ps1')
$corpus = Read-RadzenCorpus -ManifestPath $ManifestPath -WorkspaceRoot $WorkspaceRoot -VerifyFiles:$VerifyFiles.IsPresent
$statuses = @($corpus.Manifest.projects.status | Sort-Object -Unique)
$expectedStatuses = $psText.StatusActive, $psText.StatusArchived, $psText.StatusMirror, $psText.StatusModel
if (Compare-Object $expectedStatuses $statuses) { throw "Corpus statuses mismatch: $($statuses -join ', ')" }
if ($corpus.Files.Count -eq 0) { throw 'The Radzen corpus contains no source files.' }

$mode = if ($VerifyFiles) { 'structure and external file hashes' } else { 'manifest structure and fingerprints' }
Write-Host "Radzen corpus passed ($mode): $($corpus.Manifest.projects.Count) unique projects, $($corpus.Files.Count) unique hashed files, statuses $($statuses -join '/')."

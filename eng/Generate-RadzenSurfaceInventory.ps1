[CmdletBinding()]
param(
    [string]$WorkspaceRoot = 'C:\Dev',
    [string]$ManifestPath = (Join-Path $PSScriptRoot '..\docs\radzen-corpus.json'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\docs')
)

$ErrorActionPreference = 'Stop'
$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
. (Join-Path $PSScriptRoot 'RadzenCorpus.ps1')
. (Join-Path $PSScriptRoot 'RadzenSyntax.ps1')
$corpus = Read-RadzenCorpus -ManifestPath $ManifestPath -WorkspaceRoot $WorkspaceRoot
$manifest = $corpus.Manifest
$output = [IO.Path]::GetFullPath($OutputDirectory)
$selfRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')).TrimEnd('\') + '\'
$extensions = '.cs', '.razor', '.css', '.js', '.json', '.resx', '.xml', '.csproj'
$observations = [Collections.Generic.List[object]]::new()
$contracts = @{}
$templates = [Collections.Generic.List[object]]::new()
$filesWithUsage = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

foreach ($source in $corpus.Files | Where-Object { [IO.Path]::GetExtension($_.Path) -in $extensions }) {
    if ([IO.Path]::GetFullPath($source.FullPath).StartsWith($selfRoot, [StringComparison]::OrdinalIgnoreCase)) { continue }
    $content = [string](Get-Content -Raw -LiteralPath $source.FullPath)
    if ([string]::IsNullOrEmpty($content) -or $content -notmatch '(?i)radzen|\.rz-') { continue }
    $extension = [IO.Path]::GetExtension($source.Path).ToLowerInvariant()

    if ($extension -eq '.razor') {
        $components = @(Get-RadzenRazorComponents -Text $content)
        foreach ($component in $components) {
            $evidence = [ordered]@{
                path = $source.Path
                line = $component.line
                column = $component.column
                offset = $component.offset
                sha256 = $source.Sha256
                project = $source.Project
                status = $source.Status
            }
            $observations.Add([pscustomobject]@{
                kind = 'razor-component'
                name = $component.component
                path = $source.Path
                line = $component.line
                column = $component.column
                offset = $component.offset
                sha256 = $source.Sha256
                project = $source.Project
                status = $source.Status
            })
            [void]$filesWithUsage.Add($source.Path)

            if (-not $contracts.ContainsKey($component.component)) {
                $contracts[$component.component] = @{
                    observations = [Collections.Generic.List[object]]::new()
                    parameters = @{}
                }
            }
            $contracts[$component.component].observations.Add([pscustomobject]@{
                path = $source.Path
                line = $component.line
                column = $component.column
                offset = $component.offset
                sha256 = $source.Sha256
                parameters = @($component.parameters)
            })
            foreach ($parameter in $component.parameters) {
                if (-not $contracts[$component.component].parameters.ContainsKey($parameter)) {
                    $contracts[$component.component].parameters[$parameter] = [Collections.Generic.List[object]]::new()
                }
                $contracts[$component.component].parameters[$parameter].Add([pscustomobject]$evidence)
            }
        }

        if ($components.Count -gt 0) {
            foreach ($template in [regex]::Matches((Remove-RazorCommentsPreservingLines $content), '<\s*(Template|HeaderTemplate|FooterTemplate|ItemTemplate|EditTemplate|ChildContent|EmptyTemplate|LoadingTemplate)\b')) {
                $templates.Add([pscustomobject]@{
                    name = $template.Groups[1].Value
                    path = $source.Path
                    line = Get-SourceLineNumber -Text $content -Index $template.Index
                    column = Get-SourceColumnNumber -Text $content -Index $template.Index
                    offset = $template.Index
                    sha256 = $source.Sha256
                })
            }
        }
    }

    foreach ($reference in Get-RadzenClassifiedReferences -Text $content -Extension $extension) {
        $observations.Add([pscustomobject]@{
            kind = $reference.kind
            name = $reference.name
            path = $source.Path
            line = $reference.line
            column = $reference.column
            offset = $reference.offset
            sha256 = $source.Sha256
            project = $source.Project
            status = $source.Status
        })
        [void]$filesWithUsage.Add($source.Path)
    }
}

$componentSummary = @($observations | Where-Object kind -eq 'razor-component' | Group-Object name | Sort-Object Name | ForEach-Object {
    [pscustomobject]@{ name = $_.Name; occurrences = $_.Count }
})
$referenceSummary = @($observations | Where-Object kind -ne 'razor-component' | Group-Object kind, name | Sort-Object Name | ForEach-Object {
    [pscustomobject]@{ kind = $_.Group[0].kind; name = $_.Group[0].name; occurrences = $_.Count }
})
$contractEntries = @($contracts.GetEnumerator() | Sort-Object Name | ForEach-Object {
    $contract = $_.Value
    [pscustomobject]@{
        component = $_.Key
        occurrences = $contract.observations.Count
        observations = @($contract.observations)
        parameters = @($contract.parameters.GetEnumerator() | Sort-Object Name | ForEach-Object {
            [pscustomobject]@{ name = $_.Key; occurrences = $_.Value.Count; evidence = @($_.Value) }
        })
    }
})
$generatedAt = if ($manifest.generatedAt -is [datetime]) { $manifest.generatedAt.ToString('yyyy-MM-ddTHH:mm:ssK', [Globalization.CultureInfo]::InvariantCulture) } else { [string]$manifest.generatedAt }
$manifestRelative = [IO.Path]::GetRelativePath((Join-Path $PSScriptRoot '..'), $corpus.ManifestPath).Replace('\', '/')

$surface = [ordered]@{
    schemaVersion = 2
    generatedAt = $generatedAt
    provenance = [ordered]@{
        workspaceRoot = $corpus.Workspace
        corpusManifest = $manifestRelative
        corpusManifestSha256 = $corpus.ManifestSha256
        corpusSchemaVersion = $manifest.schemaVersion
    }
    filesWithUsage = $filesWithUsage.Count
    componentSummary = $componentSummary
    referenceSummary = $referenceSummary
    observations = @($observations | Sort-Object path, offset, kind, name)
}
$contractDocument = [ordered]@{
    schemaVersion = 2
    generatedAt = $generatedAt
    provenance = $surface.provenance
    components = $contractEntries
    templates = @($templates | Sort-Object path, line, name)
}

[IO.Directory]::CreateDirectory($output) | Out-Null
$surface | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $output 'radzen-surface-inventory.json') -Encoding utf8
$contractDocument | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $output 'component-contracts.json') -Encoding utf8

$surfaceLines = [Collections.Generic.List[string]]::new()
$surfaceLines.Add($psText.SurfaceTitle)
$surfaceLines.Add('')
$surfaceLines.Add(($psText.InventoryGenerated -f $corpus.ManifestSha256, $generatedAt))
$surfaceLines.Add('')
$surfaceLines.Add($psText.SurfaceMethod)
$surfaceLines.Add('')
$surfaceLines.Add(($psText.SurfaceFiles -f $filesWithUsage.Count))
$surfaceLines.Add(($psText.SurfaceTags -f (($componentSummary | Measure-Object occurrences -Sum).Sum)))
$surfaceLines.Add(($psText.SurfaceDistinctTags -f $componentSummary.Count))
$surfaceLines.Add('')
$surfaceLines.Add($psText.SurfaceComponentsHeading)
$surfaceLines.Add('')
$surfaceLines.Add('| Balise | Occurrences |')
$surfaceLines.Add('|---|---:|')
foreach ($component in $componentSummary) { $surfaceLines.Add("| ``$($component.name)`` | $($component.occurrences) |") }
$surfaceLines.Add('')
$surfaceLines.Add($psText.SurfaceReferencesHeading)
$surfaceLines.Add('')
$surfaceLines.Add($psText.SurfaceReferenceHeader)
$surfaceLines.Add('|---|---|---:|')
foreach ($reference in $referenceSummary) { $surfaceLines.Add("| $($reference.kind) | ``$($reference.name)`` | $($reference.occurrences) |") }
$surfaceLines.Add('')
$surfaceLines.Add($psText.SurfaceProvenance)
$surfaceLines | Set-Content -LiteralPath (Join-Path $output 'radzen-surface-inventory.md') -Encoding utf8

$contractLines = [Collections.Generic.List[string]]::new()
$contractLines.Add($psText.ContractTitle)
$contractLines.Add('')
$contractLines.Add($psText.ContractReliableIntro)
foreach ($entry in $contractEntries) {
    $contractLines.Add('')
    $contractLines.Add("## $($entry.component)")
    $contractLines.Add('')
    if ($entry.parameters.Count -eq 0) {
        $contractLines.Add($psText.ContractNoObservedParameters)
        continue
    }
    $contractLines.Add($psText.ContractEvidenceHeader)
    $contractLines.Add('|---|---:|---|')
    foreach ($parameter in $entry.parameters) {
        $evidence = @($parameter.evidence | ForEach-Object { "``$($_.path):$($_.line)`` (``$($_.sha256.Substring(0, 12))``)" }) -join '<br>'
        $contractLines.Add("| ``$($parameter.name)`` | $($parameter.occurrences) | $evidence |")
    }
}
$contractLines.Add('')
$contractLines.Add($psText.ContractTemplatesHeading)
$contractLines.Add('')
foreach ($template in $contractDocument.templates) {
    $contractLines.Add("- ``$($template.name)`` : ``$($template.path):$($template.line)`` (``$($template.sha256.Substring(0, 12))``)")
}
$contractLines | Set-Content -LiteralPath (Join-Path $output 'component-contracts.md') -Encoding utf8

Write-Host "Classified Radzen inventory generated: $($componentSummary.Count) component tags, $($observations.Count) provenanced observations."

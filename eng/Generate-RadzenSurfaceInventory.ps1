[CmdletBinding()]
param(
    [string]$WorkspaceRoot = 'C:\Dev',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\docs')
)

$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path -LiteralPath $WorkspaceRoot).Path.TrimEnd('\')
$output = [System.IO.Path]::GetFullPath($OutputDirectory)
$selfRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')).TrimEnd('\') + '\'
$excluded = '(\\|/)(bin|obj|node_modules|packages|artifacts|\.git|\.vs)(\\|/)'
$extensions = '.cs', '.razor', '.css', '.js', '.json', '.resx', '.xml', '.csproj'

$fileGlobs = @($extensions | ForEach-Object { '-g'; "*$_" }) + @(
    '-g', '!**/bin/**', '-g', '!**/obj/**', '-g', '!**/node_modules/**',
    '-g', '!**/packages/**', '-g', '!**/artifacts/**', '-g', '!**/.git/**', '-g', '!**/.vs/**'
)
$allFilePaths = @(& rg --files --hidden @fileGlobs $workspace)
$matchingPaths = @(& rg --files-with-matches --hidden --ignore-case @fileGlobs 'radzen' $workspace)
$files = @($matchingPaths | ForEach-Object { Get-Item -LiteralPath $_ } | Where-Object {
    -not $_.FullName.StartsWith($selfRoot, [StringComparison]::OrdinalIgnoreCase)
})

$symbolCounts = @{}
$resourceCounts = @{}
$contracts = @{}
$filesWithUsage = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

foreach ($file in $files) {
    $content = [string](Get-Content -Raw -LiteralPath $file.FullName -ErrorAction SilentlyContinue)
    if ([string]::IsNullOrEmpty($content)) { continue }
    $relative = [System.IO.Path]::GetRelativePath($workspace, $file.FullName).Replace('\', '/')

    $symbols = [regex]::Matches($content, '(?<![A-Za-z0-9_])(Radzen[A-Z][A-Za-z0-9_]*)')
    foreach ($match in $symbols) {
        $name = $match.Groups[1].Value
        if (-not $symbolCounts.ContainsKey($name)) { $symbolCounts[$name] = 0 }
        $symbolCounts[$name]++
        [void]$filesWithUsage.Add($relative)
    }

    foreach ($pattern in @('Radzen\.Blazor', '_content/Radzen\.Blazor', '(?i)radzen[-\.]')) {
        $count = [regex]::Matches($content, $pattern).Count
        if ($count -gt 0) {
            $key = switch -Regex ($pattern) {
                '^Radzen' { 'package-or-namespace' }
                '^_content' { 'static-resource' }
                default { 'css-or-script-token' }
            }
            if (-not $resourceCounts.ContainsKey($key)) { $resourceCounts[$key] = 0 }
            $resourceCounts[$key] += $count
            [void]$filesWithUsage.Add($relative)
        }
    }

    if ($file.Extension -ne '.razor') { continue }
    $clean = [regex]::Replace($content, '(?s)@\*.*?\*@|<!--.*?-->', '')
    foreach ($tag in [regex]::Matches($clean, '(?s)<\s*(Radzen[A-Z][A-Za-z0-9_]*)\b([^<>]*?)>')) {
        $component = $tag.Groups[1].Value
        if (-not $contracts.ContainsKey($component)) {
            $contracts[$component] = @{ occurrences = 0; parameters = @{}; templates = @{} }
        }
        $contracts[$component].occurrences++
        foreach ($attribute in [regex]::Matches($tag.Groups[2].Value, '(?m)(?<![@:A-Za-z0-9_-])([A-Z][A-Za-z0-9_]*)\s*=')) {
            $parameter = $attribute.Groups[1].Value
            if (-not $contracts[$component].parameters.ContainsKey($parameter)) { $contracts[$component].parameters[$parameter] = 0 }
            $contracts[$component].parameters[$parameter]++
        }
    }

    foreach ($template in [regex]::Matches($clean, '<\s*(Template|HeaderTemplate|FooterTemplate|ItemTemplate|EditTemplate|ChildContent|EmptyTemplate|LoadingTemplate)\b')) {
        $name = $template.Groups[1].Value
        if (-not $contracts.ContainsKey('_templates')) { $contracts['_templates'] = @{ occurrences = 0; parameters = @{}; templates = @{} } }
        if (-not $contracts['_templates'].templates.ContainsKey($name)) { $contracts['_templates'].templates[$name] = 0 }
        $contracts['_templates'].templates[$name]++
    }
}

$symbols = @($symbolCounts.GetEnumerator() | Sort-Object Name | ForEach-Object { [pscustomobject]@{ name = $_.Key; occurrences = $_.Value } })
$resources = @($resourceCounts.GetEnumerator() | Sort-Object Name | ForEach-Object { [pscustomobject]@{ kind = $_.Key; occurrences = $_.Value } })
$contractEntries = @($contracts.GetEnumerator() | Where-Object Name -ne '_templates' | Sort-Object Name | ForEach-Object {
    [pscustomobject]@{
        component = $_.Key
        occurrences = $_.Value.occurrences
        parameters = @($_.Value.parameters.GetEnumerator() | Sort-Object Name | ForEach-Object { [pscustomobject]@{ name = $_.Key; occurrences = $_.Value } })
    }
})
$templates = if ($contracts.ContainsKey('_templates')) { @($contracts['_templates'].templates.GetEnumerator() | Sort-Object Name | ForEach-Object { [pscustomobject]@{ name = $_.Key; occurrences = $_.Value } }) } else { @() }
$generatedAt = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ssK')

$surface = [ordered]@{
    generatedAt = $generatedAt
    workspaceRoot = $workspace
    scannedFiles = $allFilePaths.Count
    filesWithUsage = $filesWithUsage.Count
    symbols = $symbols
    resources = $resources
    files = @($filesWithUsage | Sort-Object)
}
$contractDocument = [ordered]@{
    generatedAt = $generatedAt
    components = $contractEntries
    templates = $templates
}

[System.IO.Directory]::CreateDirectory($output) | Out-Null
$surface | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $output 'radzen-surface-inventory.json') -Encoding utf8
$contractDocument | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $output 'component-contracts.json') -Encoding utf8

$surfaceLines = [System.Collections.Generic.List[string]]::new()
$surfaceLines.Add('# Inventaire étendu de la surface Radzen')
$surfaceLines.Add('')
$surfaceLines.Add("Généré le $generatedAt par inspection en lecture seule de $($allFilePaths.Count) fichiers sous ``$workspace``. Le dépôt OmniEurope.Blazor et les sorties techniques sont exclus.")
$surfaceLines.Add('')
$surfaceLines.Add("- Fichiers contenant un symbole, une ressource ou un token Radzen : **$($filesWithUsage.Count)**")
$surfaceLines.Add("- Symboles C#/Razor distincts : **$($symbols.Count)**")
$surfaceLines.Add('')
$surfaceLines.Add('| Symbole | Occurrences |')
$surfaceLines.Add('|---|---:|')
foreach ($symbol in $symbols) { $surfaceLines.Add("| ``$($symbol.name)`` | $($symbol.occurrences) |") }
$surfaceLines.Add('')
$surfaceLines.Add('## Ressources et intégrations')
$surfaceLines.Add('')
$surfaceLines.Add('| Nature | Occurrences |')
$surfaceLines.Add('|---|---:|')
foreach ($resource in $resources) { $surfaceLines.Add("| $($resource.kind) | $($resource.occurrences) |") }
$surfaceLines | Set-Content -LiteralPath (Join-Path $output 'radzen-surface-inventory.md') -Encoding utf8

$contractLines = [System.Collections.Generic.List[string]]::new()
$contractLines.Add('# Contrats observés des composants Radzen')
$contractLines.Add('')
$contractLines.Add('Ce rapport extrait les paramètres nommés et les emplacements de templates réellement présents dans les fichiers Razor, sans lire le code source de Radzen.')
foreach ($entry in $contractEntries) {
    $contractLines.Add('')
    $contractLines.Add("## $($entry.component)")
    $contractLines.Add('')
    $parameterText = if ($entry.parameters.Count -eq 0) { 'Aucun paramètre nommé observé.' } else { ($entry.parameters | ForEach-Object { "``$($_.name)`` ($($_.occurrences))" }) -join ', ' }
    $contractLines.Add($parameterText)
}
$contractLines.Add('')
$contractLines.Add('## Templates observés')
$contractLines.Add('')
$contractLines.Add((($templates | ForEach-Object { "``$($_.name)`` ($($_.occurrences))" }) -join ', '))
$contractLines | Set-Content -LiteralPath (Join-Path $output 'component-contracts.md') -Encoding utf8

Write-Host "Extended Radzen surface inventory generated: $($symbols.Count) symbols across $($filesWithUsage.Count) files."

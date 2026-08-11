function Remove-RazorCommentsPreservingLines {
    param([Parameter(Mandatory)][string]$Text)

    return [regex]::Replace($Text, '(?s)@\*.*?\*@|<!--.*?-->', {
        param($match)
        return -join ($match.Value.ToCharArray() | ForEach-Object { if ($_ -in "`r", "`n") { $_ } else { ' ' } })
    })
}

function Get-SourceLineNumber {
    param(
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][int]$Index
    )

    if ($Index -le 0) { return 1 }
    return 1 + [regex]::Matches($Text.Substring(0, $Index), "`n").Count
}

function Get-SourceColumnNumber {
    param(
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][int]$Index
    )

    if ($Index -le 0) { return 1 }
    $lineStart = $Text.LastIndexOf("`n", [Math]::Min($Index - 1, $Text.Length - 1))
    return $Index - $lineStart
}

function Find-BalancedExpressionEnd {
    param(
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][int]$Start,
        [Parameter(Mandatory)][char]$Open,
        [Parameter(Mandatory)][char]$Close
    )

    $depth = 0
    $quote = [char]0
    $escaped = $false
    for ($index = $Start; $index -lt $Text.Length; $index++) {
        $character = $Text[$index]
        if ($quote -ne [char]0) {
            if ($escaped) { $escaped = $false; continue }
            if ($character -eq '\') { $escaped = $true; continue }
            if ($character -eq $quote) { $quote = [char]0 }
            continue
        }
        if ($character -in '"', "'") { $quote = $character; continue }
        if ($character -eq $Open) { $depth++; continue }
        if ($character -eq $Close) {
            $depth--
            if ($depth -eq 0) { return $index }
        }
    }
    return $Text.Length - 1
}

function Find-RazorExpressionNextIndex {
    param(
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][int]$Start
    )

    $index = $Start + 1
    if ($index -lt $Text.Length -and $Text[$index] -eq '(') {
        return (Find-BalancedExpressionEnd -Text $Text -Start $index -Open '(' -Close ')') + 1
    }
    while ($index -lt $Text.Length) {
        if ($Text[$index] -match '[A-Za-z0-9_?.!]') { $index++; continue }
        if ($Text[$index] -eq '(') {
            $index = (Find-BalancedExpressionEnd -Text $Text -Start $index -Open '(' -Close ')') + 1
            continue
        }
        if ($Text[$index] -eq '[') {
            $index = (Find-BalancedExpressionEnd -Text $Text -Start $index -Open '[' -Close ']') + 1
            continue
        }
        break
    }
    return $index
}

function Get-RadzenRazorComponents {
    param([Parameter(Mandatory)][string]$Text)

    $clean = Remove-RazorCommentsPreservingLines $Text
    $results = [Collections.Generic.List[object]]::new()
    $starts = [regex]::Matches($clean, '<\s*(Radzen[A-Z][A-Za-z0-9]*)\b')
    foreach ($start in $starts) {
        $component = $start.Groups[1].Value
        $index = $start.Index + $start.Length
        $attributes = [Collections.Generic.List[string]]::new()
        while ($index -lt $clean.Length) {
            while ($index -lt $clean.Length -and [char]::IsWhiteSpace($clean[$index])) { $index++ }
            if ($index -ge $clean.Length -or $clean[$index] -eq '>') { break }
            if ($clean[$index] -eq '/' -and $index + 1 -lt $clean.Length -and $clean[$index + 1] -eq '>') { $index++; break }
            if ($clean[$index] -eq '@' -and $index + 1 -lt $clean.Length -and $clean[$index + 1] -eq '(') {
                $index = (Find-BalancedExpressionEnd -Text $clean -Start ($index + 1) -Open '(' -Close ')') + 1
                continue
            }

            $nameStart = $index
            while ($index -lt $clean.Length -and $clean[$index] -match '[A-Za-z0-9_:@.\-]') { $index++ }
            if ($index -eq $nameStart) { $index++; continue }
            $rawName = $clean.Substring($nameStart, $index - $nameStart)
            while ($index -lt $clean.Length -and [char]::IsWhiteSpace($clean[$index])) { $index++ }
            $hasValue = $index -lt $clean.Length -and $clean[$index] -eq '='
            if ($hasValue) {
                $index++
                while ($index -lt $clean.Length -and [char]::IsWhiteSpace($clean[$index])) { $index++ }
                if ($index -lt $clean.Length -and $clean[$index] -in '"', "'") {
                    $outerQuote = $clean[$index]
                    $index++
                    while ($index -lt $clean.Length) {
                        if ($clean[$index] -eq '@' -and $index + 1 -lt $clean.Length) {
                            $index = Find-RazorExpressionNextIndex -Text $clean -Start $index
                            continue
                        }
                        if ($clean[$index] -eq $outerQuote) { $index++; break }
                        $index++
                    }
                } elseif ($index + 1 -lt $clean.Length -and $clean[$index] -eq '@' -and $clean[$index + 1] -eq '(') {
                    $index = (Find-BalancedExpressionEnd -Text $clean -Start ($index + 1) -Open '(' -Close ')') + 1
                } else {
                    while ($index -lt $clean.Length -and -not [char]::IsWhiteSpace($clean[$index]) -and $clean[$index] -ne '>') { $index++ }
                }
            }

            $name = $rawName
            if ($name -cmatch '^@bind-([A-Z][A-Za-z0-9_]*)') { $name = $Matches[1] }
            if ($name -cmatch '^[A-Z][A-Za-z0-9_]*$' -and -not $attributes.Contains($name)) {
                $attributes.Add($name)
            }
        }

        $results.Add([pscustomobject]@{
            component = $component
            line = Get-SourceLineNumber -Text $clean -Index $start.Index
            column = Get-SourceColumnNumber -Text $clean -Index $start.Index
            offset = $start.Index
            parameters = @($attributes)
        })
    }
    return @($results)
}

function Get-RadzenClassifiedReferences {
    param(
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][string]$Extension
    )

    $results = [Collections.Generic.List[object]]::new()
    $patterns = @()
    if ($Extension -in '.cs', '.razor') {
        $patterns += @{ kind = 'namespace-reference'; regex = '(?m)^\s*@?using\s+(Radzen(?:\.[A-Za-z0-9_]+)*)\s*;?' }
        $patterns += @{ kind = 'qualified-reference'; regex = '(?<![A-Za-z0-9_])(Radzen\.[A-Za-z_][A-Za-z0-9_.]*)' }
    }
    if ($Extension -in '.razor', '.css', '.js', '.json', '.xml', '.csproj') {
        $patterns += @{ kind = 'static-resource'; regex = '(_content/Radzen\.Blazor[^\s"''<]*)' }
    }
    if ($Extension -eq '.csproj') {
        $patterns += @{ kind = 'package-reference'; regex = '(?i)<PackageReference\b[^>]*\bInclude\s*=\s*["''](Radzen\.Blazor)["'']' }
    }
    if ($Extension -eq '.css') {
        $patterns += @{ kind = 'css-selector'; regex = '(?<![A-Za-z0-9_-])(\.rz-[A-Za-z0-9_-]+)' }
    }

    foreach ($pattern in $patterns) {
        foreach ($match in [regex]::Matches($Text, $pattern.regex)) {
            $results.Add([pscustomobject]@{
                kind = $pattern.kind
                name = $match.Groups[1].Value
                line = Get-SourceLineNumber -Text $Text -Index $match.Index
                column = Get-SourceColumnNumber -Text $Text -Index $match.Index
                offset = $match.Index
            })
        }
    }
    return @($results)
}

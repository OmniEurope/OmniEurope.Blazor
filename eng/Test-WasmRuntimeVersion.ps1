[CmdletBinding()]
param(
    # A publish output folder (or its wwwroot/_framework); every dotnet.native*.wasm under it is checked.
    [Parameter(Mandatory)]
    [string[]]$PublishPath,
    # The runtime version the WebAssembly hosts must ship; the pin of Directory.Build.targets by default.
    [string]$ExpectedVersion
)

# STD-SDKPIN: Directory.Build.targets pins the Mono browser-wasm runtime pack to OmniImplicitSdkPackVersion,
# over the older runtime an installed wasm-tools workload would bring. This guard reads the runtime version
# a publish really shipped, from the files themselves: the product version (Major.Minor.Patch.0) the native
# runtime embeds in dotnet.native.wasm, the runtime pack path a native relink leaves in it, and what
# dotnet.runtime.js declares. Since .NET 11 the runtime script no longer declares a version string, only the
# commit of the runtime build it comes from (`gitHash`); the native binary embeds that same commit next to its
# product version (`<version> @Commit: <sha>`). The guard therefore requires the commit declared by
# dotnet.runtime.js, dotnet.native.js and the dotnet.js loader to equal the commit of the version-checked
# dotnet.native.wasm, so a runtime script of another build than the pinned native runtime fails. A script that
# still declares a version (the older `var x="10.0.15",y="Release"` form) is checked against the pin instead.
# Any of them other than the pin fails, as does a publish with no runtime at all.
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($ExpectedVersion)) {
    [xml]$targets = Get-Content -LiteralPath (Join-Path $repoRoot 'Directory.Build.targets') -Raw
    $ExpectedVersion = @($targets.Project.PropertyGroup | ForEach-Object { $_.OmniImplicitSdkPackVersion } | Where-Object { $_ })[0]
    if ([string]::IsNullOrWhiteSpace($ExpectedVersion)) {
        throw 'Directory.Build.targets declares no OmniImplicitSdkPackVersion.'
    }
}

$expected = [version]$ExpectedVersion
$line = [regex]::Escape("$($expected.Major).$($expected.Minor)")
$productVersion = [regex]"(?<![\d.])$line\.(\d+)\.0(?![\d.])"
$packPath = [regex]'(?i)microsoft\.netcore\.app\.runtime\.mono\.(?:multithread\.)?browser-wasm[\\/](\d+\.\d+\.\d+)'
$runtimeScript = [regex]'var \w+="(\d+\.\d+\.\d+)",\w+="(?:Release|Debug)"'
$nativeCommit = [regex]'@Commit: ([0-9a-f]{40})(?![0-9a-f])'
$scriptCommit = [regex]'gitHash:"([0-9a-f]{40})"'

$failures = [System.Collections.Generic.List[string]]::new()
$checked = 0
foreach ($root in $PublishPath) {
    $resolved = (Resolve-Path -LiteralPath $root).Path
    $natives = @(Get-ChildItem -LiteralPath $resolved -Recurse -File -Filter 'dotnet.native*.wasm' | Where-Object Extension -eq '.wasm')
    if ($natives.Count -eq 0) {
        $failures.Add("No dotnet.native*.wasm under $resolved.")
        continue
    }

    foreach ($native in $natives) {
        $checked++
        # Latin-1 maps every byte to one character, so the ASCII strings of the binary read as they are.
        $text = [Text.Encoding]::Latin1.GetString([IO.File]::ReadAllBytes($native.FullName))
        $found = @($productVersion.Matches($text) | ForEach-Object { "$($expected.Major).$($expected.Minor).$($_.Groups[1].Value)" })
        $found += @($packPath.Matches($text) | ForEach-Object { $_.Groups[1].Value })
        $found = @($found | Sort-Object -Unique)
        if ($found.Count -eq 0) {
            $failures.Add("$($native.FullName): no runtime version found in the binary.")
        }
        elseif ($found | Where-Object { [version]$_ -ne $expected }) {
            $failures.Add("$($native.FullName): runtime $($found -join ', '), expected $expected.")
        }
        else {
            Write-Host "$($native.FullName): runtime $expected."
        }

        $commits = @($nativeCommit.Matches($text) | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
        if ($commits.Count -gt 1) {
            $failures.Add("$($native.FullName): several runtime commits ($($commits -join ', ')).")
        }

        # The JavaScript files the runtime loads next to the native binary: the runtime script, the native
        # glue and the dotnet.js loader. Each must belong to the pinned runtime; none is skipped.
        $scripts = @(Get-ChildItem -LiteralPath $native.DirectoryName -File | Where-Object { $_.Extension -eq '.js' -and $_.Name -match '^dotnet(\.runtime.*|\.native.*)?\.js$' })
        if (-not ($scripts | Where-Object Name -like 'dotnet.runtime*.js')) {
            $failures.Add("$($native.DirectoryName): no dotnet.runtime*.js next to $($native.Name).")
        }
        foreach ($script in $scripts) {
            $content = Get-Content -LiteralPath $script.FullName -Raw
            $match = $runtimeScript.Match($content)
            if ($match.Success) {
                if ([version]$match.Groups[1].Value -ne $expected) {
                    $failures.Add("$($script.FullName): runtime $($match.Groups[1].Value), expected $expected.")
                }
                continue
            }

            $declared = @($scriptCommit.Matches($content) | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
            if ($declared.Count -eq 0) {
                $failures.Add("$($script.FullName): no runtime version or commit declared.")
            }
            elseif ($commits.Count -eq 0) {
                $failures.Add("$($script.FullName): declares commit $($declared -join ', ') but $($native.Name) embeds no commit to compare it with.")
            }
            elseif ($declared.Count -gt 1 -or $declared[0] -ne $commits[0]) {
                $failures.Add("$($script.FullName): commit $($declared -join ', '), the pinned runtime $expected is $($commits[0]).")
            }
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host $_ }
    throw "$($failures.Count) published WebAssembly runtime file(s) differ from the pinned runtime $expected."
}

Write-Host "$checked published dotnet.native.wasm file(s) ship the pinned runtime $expected."

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
# runtime embeds in dotnet.native.wasm, the runtime pack path a native relink leaves in it, and the version
# dotnet.runtime.js declares. Any of them other than the pin fails, as does a publish with no runtime at all.
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

        foreach ($script in @(Get-ChildItem -LiteralPath $native.DirectoryName -File -Filter 'dotnet.runtime*.js' | Where-Object Extension -eq '.js')) {
            $match = $runtimeScript.Match((Get-Content -LiteralPath $script.FullName -Raw))
            if (-not $match.Success) {
                $failures.Add("$($script.FullName): no runtime version declared.")
            }
            elseif ([version]$match.Groups[1].Value -ne $expected) {
                $failures.Add("$($script.FullName): runtime $($match.Groups[1].Value), expected $expected.")
            }
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host $_ }
    throw "$($failures.Count) published WebAssembly runtime file(s) differ from the pinned runtime $expected."
}

Write-Host "$checked published dotnet.native.wasm file(s) ship the pinned runtime $expected."

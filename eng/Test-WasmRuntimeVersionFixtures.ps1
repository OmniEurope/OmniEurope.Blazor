[CmdletBinding()]
param()

# Self-test of Test-WasmRuntimeVersion.ps1: a publish that ships the expected runtime passes, one that
# ships an older runtime, one with no runtime at all, and one whose script declares another version fail.
# The fixtures are written to a temporary folder, as the few bytes the guard reads in a real publish.
$ErrorActionPreference = 'Stop'
$guard = Join-Path $PSScriptRoot 'Test-WasmRuntimeVersion.ps1'
$root = Join-Path ([IO.Path]::GetTempPath()) "omni-wasm-runtime-fixtures-$([guid]::NewGuid().ToString('N'))"

function New-Publish([string]$Name, [string]$NativeVersion, [string]$ScriptVersion) {
    $framework = Join-Path $root "$Name\wwwroot\_framework"
    New-Item -ItemType Directory -Path $framework -Force | Out-Null
    if ($NativeVersion) {
        $bytes = [byte[]](0, 97, 115, 109) + [Text.Encoding]::ASCII.GetBytes("`0$NativeVersion`0") + [byte[]](0, 1)
        [IO.File]::WriteAllBytes((Join-Path $framework 'dotnet.native.abc123.wasm'), $bytes)
        Set-Content -LiteralPath (Join-Path $framework 'dotnet.runtime.abc123.js') -Value "var e=`"$ScriptVersion`",t=`"Release`";"
    }
    return (Join-Path $root $Name)
}

function Assert-Rejected([string]$Path, [string]$Case) {
    # The call is expected to fail; its exit code is captured and cleared so a pwsh CI step does not
    # end with it (see Test-CspFixtures.ps1).
    & pwsh -NoProfile -File $guard -PublishPath $Path -ExpectedVersion 10.0.14 *> $null
    $exitCode = $LASTEXITCODE
    $global:LASTEXITCODE = 0
    if ($exitCode -eq 0) {
        throw "The WebAssembly runtime guard accepted $Case."
    }
}

try {
    & $guard -PublishPath (New-Publish 'pinned' '10.0.14.0' '10.0.14') -ExpectedVersion 10.0.14
    Assert-Rejected (New-Publish 'older' '10.0.13.0' '10.0.13') 'an older runtime'
    Assert-Rejected (New-Publish 'empty' $null $null) 'a publish with no runtime'
    Assert-Rejected (New-Publish 'script' '10.0.14.0' '10.0.13') 'a runtime script of another version'
    Write-Host 'WebAssembly runtime guard fixtures passed: pinned accepted, older, missing and mismatched rejected.'
}
finally {
    Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}

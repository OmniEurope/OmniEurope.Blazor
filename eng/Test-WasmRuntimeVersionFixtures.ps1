[CmdletBinding()]
param()

# Self-test of Test-WasmRuntimeVersion.ps1: a publish that ships the expected runtime passes, one that
# ships an older runtime, one with no runtime at all, and one whose script declares another version or another
# runtime commit (the .NET 11 form, which declares no version) fail; the older version form stays supported.
# The fixtures are written to a temporary folder, as the few bytes the guard reads in a real publish.
$ErrorActionPreference = 'Stop'
$guard = Join-Path $PSScriptRoot 'Test-WasmRuntimeVersion.ps1'
$root = Join-Path ([IO.Path]::GetTempPath()) "omni-wasm-runtime-fixtures-$([guid]::NewGuid().ToString('N'))"

$commit = '297acf930fb0f5c07fd4e51d78e521b4ad2e5d1f'
$otherCommit = '1111111111111111111111111111111111111111'

# $Script is the dotnet.runtime.js content: 'version' declares a version (older form), 'commit' declares the
# commit of the build it comes from (.NET 11 form), 'none' declares nothing; any other value is the commit.
function New-Publish([string]$Name, [string]$NativeVersion, [string]$Script, [string]$ScriptKind = 'version') {
    $framework = Join-Path $root "$Name\wwwroot\_framework"
    New-Item -ItemType Directory -Path $framework -Force | Out-Null
    if ($NativeVersion) {
        $bytes = [byte[]](0, 97, 115, 109) + [Text.Encoding]::ASCII.GetBytes("`0$NativeVersion`0$NativeVersion.26.53018 @Commit: $commit`0") + [byte[]](0, 1)
        [IO.File]::WriteAllBytes((Join-Path $framework 'dotnet.native.abc123.wasm'), $bytes)
        $content = switch ($ScriptKind) {
            'version' { "var e=`"$Script`",t=`"Release`";" }
            'commit' { "const t={gitHash:`"$Script`",coreAssetsInMemory:ut()};" }
            default { 'const t={};' }
        }
        Set-Content -LiteralPath (Join-Path $framework 'dotnet.runtime.abc123.js') -Value $content
        if ($ScriptKind -eq 'commit') {
            Set-Content -LiteralPath (Join-Path $framework 'dotnet.js') -Value "const l={gitHash:`"$commit`",config:n};"
            Set-Content -LiteralPath (Join-Path $framework 'dotnet.native.abc123.js') -Value "var Module={gitHash:`"$commit`"};"
        }
    }
    return (Join-Path $root $Name)
}

function Assert-Rejected([string]$Path, [string]$Case) {
    # The call is expected to fail; its exit code is captured and cleared so a pwsh CI step does not
    # end with it (see Test-CspFixtures.ps1).
    & pwsh -NoProfile -File $guard -PublishPath $Path -ExpectedVersion 11.0.0 *> $null
    $exitCode = $LASTEXITCODE
    $global:LASTEXITCODE = 0
    if ($exitCode -eq 0) {
        throw "The WebAssembly runtime guard accepted $Case."
    }
}

try {
    & $guard -PublishPath (New-Publish 'pinned' '11.0.0.0' '11.0.0') -ExpectedVersion 11.0.0
    & $guard -PublishPath (New-Publish 'pinned-commit' '11.0.0.0' $commit 'commit') -ExpectedVersion 11.0.0
    Assert-Rejected (New-Publish 'older' '10.0.15.0' '10.0.15') 'an older runtime'
    Assert-Rejected (New-Publish 'empty' $null $null) 'a publish with no runtime'
    Assert-Rejected (New-Publish 'script' '11.0.0.0' '10.0.15') 'a runtime script of another version'
    Assert-Rejected (New-Publish 'commit' '11.0.0.0' $otherCommit 'commit') 'a runtime script of another build commit'
    Assert-Rejected (New-Publish 'none' '11.0.0.0' $null 'none') 'a runtime script declaring neither version nor commit'
    Write-Host 'WebAssembly runtime guard fixtures passed: pinned accepted, older, missing and mismatched rejected.'
}
finally {
    Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}

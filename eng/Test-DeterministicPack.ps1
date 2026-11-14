[CmdletBinding()]
param(
    # The package already packed from the Release build (artifacts/packages in CI).
    [Parameter(Mandatory)][string]$PackagePath,
    [Parameter(Mandatory)][string]$SymbolPackagePath
)

$ErrorActionPreference = 'Stop'

# Packs the same Release build a second time, into a fresh folder, and requires the .nupkg and the .snupkg
# to be byte for byte identical to the first ones: Deterministic plus the fixed DeterministicTimestamp of
# OmniEurope.Blazor.csproj leave nothing that depends on the time of the pack (docs/reproducibility.md).
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src/OmniEurope.Blazor/OmniEurope.Blazor.csproj'
$second = Join-Path ([IO.Path]::GetTempPath()) ("omni-pack-" + [Guid]::NewGuid().ToString('N'))
try {
    # A first pack and a second one in the same second could match by chance on a time-stamped archive;
    # waiting past the two-second resolution of ZIP times rules that out.
    Start-Sleep -Seconds 3
    dotnet pack $project --configuration Release --no-build --no-restore --output $second --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "The second pack failed (exit $LASTEXITCODE)." }

    foreach ($first in @($PackagePath, $SymbolPackagePath)) {
        $name = Split-Path -Leaf $first
        $other = Join-Path $second $name
        if (-not (Test-Path -LiteralPath $other)) { throw "The second pack produced no $name." }
        $firstHash = (Get-FileHash -LiteralPath $first -Algorithm SHA256).Hash
        $otherHash = (Get-FileHash -LiteralPath $other -Algorithm SHA256).Hash
        if ($firstHash -ne $otherHash) {
            throw "$name differs between two packs of the same build: $firstHash then $otherHash."
        }
        Write-Host "$name identique sur deux empaquetages : SHA-256 $firstHash"
    }
}
finally {
    Remove-Item -LiteralPath $second -Recurse -Force -ErrorAction SilentlyContinue
}

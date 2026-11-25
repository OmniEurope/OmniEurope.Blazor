[CmdletBinding()]
param()

# Returns the path of a Chromium browser a CDP probe can drive: the first of Edge, Chrome or Chromium on
# the PATH, then, on Windows, Edge in its usual installation folders. Fails when there is none.
$ErrorActionPreference = 'Stop'
$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
$browserCommands = @('msedge', 'google-chrome', 'chrome', 'chromium', 'chromium-browser')
$browserPath = $browserCommands | ForEach-Object { Get-Command $_ -ErrorAction SilentlyContinue } | Select-Object -First 1 -ExpandProperty Source
if (-not $browserPath -and $IsWindows) {
    $edgePaths = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Microsoft\Edge\Application\msedge.exe'),
        (Join-Path $env:ProgramFiles 'Microsoft\Edge\Application\msedge.exe')
    )
    $browserPath = $edgePaths | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $browserPath) { throw $psText.ChromiumMissing }
$browserPath

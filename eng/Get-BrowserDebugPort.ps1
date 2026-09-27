[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$BrowserProfile,
    [Parameter(Mandatory)]
    [System.Diagnostics.Process]$Browser,
    [ValidateRange(1, 120)]
    [int]$TimeoutSeconds = 30
)

# Chromium started with --remote-debugging-port=0 picks a free port and writes it on the first line of
# <user-data-dir>/DevToolsActivePort. Reading it from the private profile guarantees the probe drives the
# browser this script started, never another one already listening on a well-known port.
$ErrorActionPreference = 'Stop'
$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
$portFile = Join-Path $BrowserProfile 'DevToolsActivePort'
$deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
while ([DateTime]::UtcNow -lt $deadline) {
    if ($Browser.HasExited) { throw ($psText.BrowserExitedBeforePort -f $Browser.ExitCode) }
    if (Test-Path -LiteralPath $portFile) {
        $firstLine = Get-Content -LiteralPath $portFile -TotalCount 1 -ErrorAction SilentlyContinue
        $port = 0
        if ([int]::TryParse("$firstLine", [ref]$port) -and $port -ge 1 -and $port -le 65535) { return $port }
    }
    Start-Sleep -Milliseconds 100
}

throw ($psText.BrowserPortTimeout -f $portFile, $TimeoutSeconds)

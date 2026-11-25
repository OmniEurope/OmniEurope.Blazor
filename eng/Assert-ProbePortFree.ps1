[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Uri,
    [Parameter(Mandatory)]
    [int]$Port
)

# Fails when an HTTP host already answers on the port a probe host is about to start on: the test never
# reuses a process it does not own.
$ErrorActionPreference = 'Stop'
$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
try {
    Invoke-WebRequest -UseBasicParsing -Uri $Uri -TimeoutSec 1 -ErrorAction Stop | Out-Null
    $existingHost = $true
}
catch {
    # Nothing answers on the port: it is free for the host about to start.
    $existingHost = $false
}
if ($existingHost) {
    throw ($psText.PortOwned -f $Port)
}

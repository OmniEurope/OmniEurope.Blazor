[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [System.Diagnostics.Process]$Process,
    [Parameter(Mandatory)]
    [string]$Uri,
    # The host's name in the messages.
    [Parameter(Mandatory)]
    [string]$HostName
)

# Waits up to about 15 s for the host a probe started to answer HTTP 200 on its root, and returns that
# response. Fails as soon as the host stops, or when it never answers 200.
$ErrorActionPreference = 'Stop'
$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
$response = $null
$lastFailure = $null
for ($attempt = 1; $attempt -le 75; $attempt++) {
    if ($Process.HasExited) { throw ($psText.HostStopped -f $HostName, $Process.ExitCode) }
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri $Uri -TimeoutSec 2 -ErrorAction Stop
        if ($response.StatusCode -eq 200) { break }
    }
    catch {
        # The host is still starting; the last refusal is reported if it never answers.
        $lastFailure = $_.Exception.Message
    }
    Start-Sleep -Milliseconds 200
}
if ($null -eq $response -or $response.StatusCode -ne 200) {
    $message = $psText.HostNoHttp200 -f $HostName
    if ($lastFailure) { $message = "$message ($lastFailure)" }
    throw $message
}
$response

[CmdletBinding()]
param(
    [System.Diagnostics.Process]$Browser,
    [string]$BrowserProfile
)

# Stops the browser a probe host started. A Chromium launcher may hand the session to a child process and
# exit at once (Edge 155 does), so stopping the launcher alone would leave the real browser running on the
# private profile. Every process whose command line names that profile is stopped too; the profile is a
# fresh temporary folder, so nothing else carries it.
$ErrorActionPreference = 'Stop'
if ($Browser -and -not $Browser.HasExited) {
    Stop-Process -Id $Browser.Id -ErrorAction SilentlyContinue
    $Browser.WaitForExit(5000) | Out-Null
}

if (-not $BrowserProfile) {
    return
}

$owned = @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
    try { $_.CommandLine -and $_.CommandLine.Contains($BrowserProfile, [StringComparison]::OrdinalIgnoreCase) } catch { $false }
})
foreach ($process in $owned) {
    Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
}
foreach ($process in $owned) {
    try { $process.WaitForExit(5000) | Out-Null } catch { }
}

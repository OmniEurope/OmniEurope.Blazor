[CmdletBinding()]
param(
    [System.Diagnostics.Process]$Browser,
    [string]$BrowserProfile,
    # Also deletes the profile folder once its processes are gone, when it lies in the temporary folder.
    [switch]$RemoveProfile
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

$owned = @()
if ($IsWindows) {
    # One CIM query, filtered by the system: reading CommandLine process by process takes minutes on a
    # machine with hundreds of processes. The profile folder's own name (a GUID, no WQL wildcard or escape
    # character) is enough to tell the browser's processes apart.
    $leaf = Split-Path -Leaf $BrowserProfile
    $ids = @(Get-CimInstance Win32_Process -Filter "CommandLine LIKE '%$leaf%'" -ErrorAction SilentlyContinue |
        Where-Object { $_.ProcessId -ne $PID } | ForEach-Object { $_.ProcessId })
    $owned = @($ids | ForEach-Object { Get-Process -Id $_ -ErrorAction SilentlyContinue })
}
else {
    $owned = @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
        try { $_.CommandLine -and $_.CommandLine.Contains($BrowserProfile, [StringComparison]::OrdinalIgnoreCase) } catch { $false }
    })
}
foreach ($process in $owned) {
    Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
}
foreach ($process in $owned) {
    try { $process.WaitForExit(5000) | Out-Null } catch { }
}
if ($RemoveProfile) {
    $resolvedProfile = [IO.Path]::GetFullPath($BrowserProfile)
    $resolvedTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if ($resolvedProfile.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase)) {
        # The files of a browser that has just exited stay locked for a moment: retried for up to 10 s.
        for ($attempt = 1; $attempt -le 50 -and (Test-Path -LiteralPath $resolvedProfile); $attempt++) {
            Remove-Item -LiteralPath $resolvedProfile -Recurse -Force -ErrorAction SilentlyContinue
            if (Test-Path -LiteralPath $resolvedProfile) { Start-Sleep -Milliseconds 200 }
        }
    }
}

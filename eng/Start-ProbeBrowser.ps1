[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$BrowserPath
)

# Starts one headless Chromium for a showcase probe, on a fresh private profile in the temporary folder
# and a debug port the browser picks itself (read back by eng/Get-BrowserDebugPort.ps1). Returns
# { Process, Profile, Port }; eng/Stop-ProbeBrowser.ps1 -RemoveProfile stops it and deletes the profile.
$ErrorActionPreference = 'Stop'
$browserProfile = Join-Path ([IO.Path]::GetTempPath()) ("omni-showcase-cdp-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $browserProfile | Out-Null
$browserArguments = @(
    '--headless=new', '--disable-gpu', '--disable-dev-shm-usage', '--no-sandbox',
    '--lang=fr', '--accept-lang=fr',
    '--remote-debugging-port=0', "--user-data-dir=$browserProfile", 'about:blank'
)
$browserStart = @{ FilePath = $BrowserPath; ArgumentList = $browserArguments; PassThru = $true }
if ($IsWindows) { $browserStart.WindowStyle = 'Hidden' }
$browser = [pscustomobject]@{ Process = (Start-Process @browserStart); Profile = $browserProfile; Port = 0 }
try {
    $browser.Port = & (Join-Path $PSScriptRoot 'Get-BrowserDebugPort.ps1') -BrowserProfile $browserProfile -Browser $browser.Process
}
catch {
    & (Join-Path $PSScriptRoot 'Stop-ProbeBrowser.ps1') -Browser $browser.Process -BrowserProfile $browserProfile -RemoveProfile
    throw
}
$browser

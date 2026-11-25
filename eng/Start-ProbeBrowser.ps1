[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$BrowserPath,
    # The browser's interface and Accept-Language; empty leaves the browser's own.
    [AllowEmptyString()]
    [string]$Language = 'fr',
    # The page the browser opens on; the probe usually navigates itself.
    [string]$StartUrl = 'about:blank',
    # The start of the profile folder's name, which tells the hosts' leftovers apart.
    [string]$ProfilePrefix = 'omni-showcase-cdp-'
)

# Starts one headless Chromium for a probe, on a fresh private profile in the temporary folder and a
# debug port the browser picks itself (read back by eng/Get-BrowserDebugPort.ps1). Returns
# { Process, Profile, Port }; eng/Stop-ProbeBrowser.ps1 -RemoveProfile stops it and deletes the profile.
$ErrorActionPreference = 'Stop'
$browserProfile = Join-Path ([IO.Path]::GetTempPath()) ($ProfilePrefix + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $browserProfile | Out-Null
$languageArguments = if ($Language) { @("--lang=$Language", "--accept-lang=$Language") } else { @() }
$browserArguments = @(
    '--headless=new', '--disable-gpu', '--disable-dev-shm-usage', '--no-sandbox',
    # Edge signs a fresh profile in with the Windows account and opens its sync dialog as a tab, which
    # hides the probed page: its animation frames stop and a probe waiting on one never ends.
    '--no-first-run', '--no-default-browser-check', '--disable-sync', '--disable-features=msImplicitSignin,msEdgeSyncConsent'
) + $languageArguments + @(
    '--remote-debugging-port=0', "--user-data-dir=$browserProfile", $StartUrl
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

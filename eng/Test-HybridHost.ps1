[CmdletBinding()]
param(
    [string]$ExecutablePath = (Join-Path $PSScriptRoot '..\samples\OmniEurope.Blazor.HybridSmoke\bin\Release\net10.0-windows10.0.19041.0\win-x64\OmniEurope.Blazor.HybridSmoke.exe'),
    [int]$ReadyTimeoutSeconds = 90
)

# The Hybrid host is not driven over the WebView2 debugging port. The CI runner never exposes that
# port whatever the way it is requested, so the host runs a self-test inside its own WebView2 when
# HYBRIDSMOKE_SELFTEST is set: it clicks its button, reads its output, records console errors, and
# publishes the result on standard output. This script launches it, collects that output, and
# asserts the same things the other host probes assert.

$ErrorActionPreference = 'Stop'
$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
if (-not $IsWindows) { throw $psText.HybridWindowsRequired }

$resolvedExecutable = (Resolve-Path -LiteralPath $ExecutablePath).Path
$process = $null
$captured = $null
$subscriptions = @()
$userDataFolder = Join-Path ([IO.Path]::GetTempPath()) ("omnieurope-webview2-" + [Guid]::NewGuid().ToString('n'))
$hostPage = Join-Path $PSScriptRoot '..\samples\OmniEurope.Blazor.HybridSmoke\wwwroot\index.html'
$doneMarker = 'HYBRID-SMOKE selftest-done'

try {
    $hostHtml = Get-Content -Raw -LiteralPath $hostPage
    if ($hostHtml -notmatch 'http-equiv="Content-Security-Policy"') { throw $psText.HybridShellMissingCsp }
    if ($hostHtml -match "'unsafe-inline'|'unsafe-eval'") { throw 'La coque Hybrid contient une directive CSP interdite.' }

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $resolvedExecutable
    $startInfo.WorkingDirectory = Split-Path -Parent $resolvedExecutable
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.Environment['HYBRIDSMOKE_SELFTEST'] = '1'
    # A user data folder of its own keeps one run's WebView2 state away from the next.
    $startInfo.Environment['WEBVIEW2_USER_DATA_FOLDER'] = $userDataFolder

    $captured = [Text.StringBuilder]::new()
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $collect = { if ($null -ne $EventArgs.Data) { [void]$Event.MessageData.AppendLine($EventArgs.Data) } }
    $subscriptions = @(
        Register-ObjectEvent -InputObject $process -EventName OutputDataReceived -Action $collect -MessageData $captured
        Register-ObjectEvent -InputObject $process -EventName ErrorDataReceived -Action $collect -MessageData $captured
    )
    [void]$process.Start()
    $process.BeginOutputReadLine()
    $process.BeginErrorReadLine()

    $reported = $false
    for ($attempt = 1; $attempt -le ($ReadyTimeoutSeconds * 5); $attempt++) {
        if ($captured.ToString().Contains($doneMarker)) { $reported = $true; break }
        if ($process.HasExited) { break }
        Start-Sleep -Milliseconds 200
    }
    # Give the output pump a moment to drain whatever the host wrote right before the marker.
    Start-Sleep -Milliseconds 300
    $output = $captured.ToString()
    if (-not $reported) {
        if ($process.HasExited) { throw ($psText.HostStopped -f 'Hybrid', $process.ExitCode) }
        throw ($psText.HybridSelfTestTimeout -f $ReadyTimeoutSeconds)
    }

    $lines = @($output -split "`r?`n")
    $failed = $lines | Where-Object { $_ -like 'HYBRID-SMOKE selftest-failed *' } | Select-Object -First 1
    if ($failed) { throw ($psText.HybridSelfTestFailed -f $failed.Substring('HYBRID-SMOKE selftest-failed '.Length)) }

    $resultLine = $lines | Where-Object { $_ -like 'HYBRID-SMOKE selftest language=*' } | Select-Object -First 1
    if (-not $resultLine) { throw $psText.HybridSelfTestMissing }
    if ($resultLine -notmatch 'language="(?<language>[^"]*)" title="(?<title>[^"]*)" observed="(?<observed>[^"]*)" errors=(?<errors>\d+)') {
        throw ($psText.HybridSelfTestUnreadable -f $resultLine)
    }
    $language = $Matches['language']
    $title = $Matches['title']
    $observed = $Matches['observed']
    $errorCount = [int]$Matches['errors']

    $expectedLanguage = [Globalization.CultureInfo]::CurrentUICulture.TwoLetterISOLanguageName
    $expectedTitle = if ($expectedLanguage -eq 'fr') { 'Test hybride OmniEurope.Blazor' } else { 'OmniEurope.Blazor hybrid test' }
    if ($observed -ne '1') { throw ($psText.HybridSelfTestMismatch -f $observed, '1') }
    if ($language -ne $expectedLanguage) { throw ($psText.HybridSelfTestLanguage -f $language, $expectedLanguage) }
    if ($title -ne $expectedTitle) { throw ($psText.HybridSelfTestTitle -f $title, $expectedTitle) }
    if ($errorCount -ne 0) {
        $errors = @($lines | Where-Object { $_ -like 'HYBRID-SMOKE selftest-error *' } | ForEach-Object { $_.Substring('HYBRID-SMOKE selftest-error '.Length) })
        throw ($psText.HybridSelfTestErrors -f $errorCount, ($errors -join ' | '))
    }

    Write-Host ($psText.HybridPassed -f $process.Id, $observed, $language, $title)
}
catch {
    if ($captured -and $captured.Length -gt 0) { Write-Host $captured.ToString() }
    throw
}
finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id
        $process.WaitForExit(5000) | Out-Null
    }
    foreach ($subscription in @($subscriptions)) {
        if ($subscription) { Unregister-Event -SubscriptionId $subscription.Id -ErrorAction SilentlyContinue }
    }
    if ($process) { $process.Dispose() }
    Remove-Item -LiteralPath $userDataFolder -Recurse -Force -ErrorAction SilentlyContinue
}

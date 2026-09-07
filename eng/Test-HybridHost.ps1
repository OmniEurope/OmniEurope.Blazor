[CmdletBinding()]
param(
    [int]$Port = 9224,
    [string]$ExecutablePath = (Join-Path $PSScriptRoot '..\samples\OmniEurope.Blazor.HybridSmoke\bin\Release\net10.0-windows10.0.19041.0\win-x64\OmniEurope.Blazor.HybridSmoke.exe'),
    [int]$ReadyTimeoutSeconds = 90
)

$ErrorActionPreference = 'Stop'
$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
if (-not $IsWindows) { throw $psText.HybridWindowsRequired }

$resolvedExecutable = (Resolve-Path -LiteralPath $ExecutablePath).Path
$stdout = New-TemporaryFile
$stderr = New-TemporaryFile
$process = $null
$previousArguments = $env:WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS
$previousUserDataFolder = $env:WEBVIEW2_USER_DATA_FOLDER
$userDataFolder = Join-Path ([IO.Path]::GetTempPath()) ("omnieurope-webview2-" + [Guid]::NewGuid().ToString('n'))
$hostPage = Join-Path $PSScriptRoot '..\samples\OmniEurope.Blazor.HybridSmoke\wwwroot\index.html'

function Get-WebView2RuntimeVersion {
    $client = '{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}'
    $roots = @(
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients',
        'HKLM:\SOFTWARE\Microsoft\EdgeUpdate\Clients',
        'HKCU:\SOFTWARE\Microsoft\EdgeUpdate\Clients'
    )
    foreach ($root in $roots) {
        try {
            $version = (Get-ItemProperty -LiteralPath (Join-Path $root $client) -Name pv -ErrorAction Stop).pv
            if (-not [string]::IsNullOrWhiteSpace($version)) { return $version }
        }
        catch {
        }
    }
    return $null
}

try {
    $hostHtml = Get-Content -Raw -LiteralPath $hostPage
    if ($hostHtml -notmatch 'http-equiv="Content-Security-Policy"') { throw $psText.HybridShellMissingCsp }
    if ($hostHtml -match "'unsafe-inline'|'unsafe-eval'") { throw 'La coque Hybrid contient une directive CSP interdite.' }

    $portOccupied = $false
    try {
        Invoke-RestMethod -Uri "http://127.0.0.1:$Port/json/list" -TimeoutSec 1 -ErrorAction Stop | Out-Null
        $portOccupied = $true
    }
    catch {
    }
    if ($portOccupied) { throw ($psText.HybridPortOwned -f $Port) }

    # Additional browser arguments only apply when WebView2 creates a browser process. An existing
    # process for the same user data folder is reused and the arguments are dropped, which looks
    # exactly like the runner failure: WebView2 alive and rendering, no debugging port. A folder of
    # its own guarantees a fresh browser process that carries the argument.
    $env:WEBVIEW2_USER_DATA_FOLDER = $userDataFolder
    $env:WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS = "--remote-debugging-port=$Port --remote-allow-origins=*"
    $start = @{
        FilePath = $resolvedExecutable
        WorkingDirectory = Split-Path -Parent $resolvedExecutable
        PassThru = $true
        RedirectStandardOutput = $stdout.FullName
        RedirectStandardError = $stderr.FullName
    }
    $process = Start-Process @start

    $target = $null
    $endpointAnswered = $false
    $lastListing = @()
    $lastListError = $null
    for ($attempt = 1; $attempt -le ($ReadyTimeoutSeconds * 5); $attempt++) {
        if ($process.HasExited) { throw ($psText.HostStopped -f 'Hybrid', $process.ExitCode) }
        try {
            $listed = Invoke-RestMethod -Uri "http://127.0.0.1:$Port/json/list" -TimeoutSec 2 -ErrorAction Stop
            $endpointAnswered = $true
            $lastListing = @($listed)
            $target = @($listed | Where-Object { $_.type -eq 'page' -and $_.webSocketDebuggerUrl }) | Select-Object -First 1
            if ($target) { break }
        }
        catch {
            $lastListError = $_.Exception.Message
        }
        Start-Sleep -Milliseconds 200
    }
    if (-not $target) {
        # The wait above is long enough that a slow start is already excluded, so the useful fact
        # is which failure happened: the debugging port never opened, or it opened without a page.
        # Without this the runner only reports a timeout and every diagnosis stays a guess.
        $runtime = Get-WebView2RuntimeVersion
        if (-not $runtime) { $runtime = $psText.HybridDiagRuntimeMissing }
        Write-Host ($psText.HybridDiagRuntime -f $runtime)
        Write-Host ($psText.HybridDiagArguments -f $env:WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS)
        # The browser runs in its own process tree. Whether one exists at all separates a WebView2
        # that was never created from a WebView2 that ignored the debugging port.
        $browsers = @(Get-Process -Name 'msedgewebview2' -ErrorAction SilentlyContinue)
        Write-Host ($psText.HybridDiagBrowsers -f $browsers.Count)

        # The count alone cannot say whether the browser process received the debugging argument.
        # The environment variable printed above is the one this script holds, not the one the
        # child inherited, so the command line is the only place the answer actually exists.
        $commandLines = @(Get-CimInstance Win32_Process -Filter "Name='msedgewebview2.exe'" -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty CommandLine)
        $carrying = @($commandLines | Where-Object { $_ -match [regex]::Escape("--remote-debugging-port=$Port") })
        if ($carrying.Count -gt 0) {
            Write-Host ($psText.HybridDiagArgumentSeen -f $carrying.Count)
        } else {
            Write-Host $psText.HybridDiagArgumentLost
        }
        if ($endpointAnswered) {
            $summary = if ($lastListing.Count -eq 0) {
                $psText.HybridDiagEmpty
            } else {
                (@($lastListing | ForEach-Object { "$($_.type)=$($_.url)" }) -join ' | ')
            }
            Write-Host ($psText.HybridDiagListing -f $lastListing.Count, $summary)
        } else {
            Write-Host ($psText.HybridDiagNoEndpoint -f $lastListError)
        }
        throw ($psText.HybridNoTarget -f $Port, $ReadyTimeoutSeconds)
    }

    $expectedLanguage = [Globalization.CultureInfo]::CurrentUICulture.TwoLetterISOLanguageName
    $expectedTitle = if ($expectedLanguage -eq 'fr') { 'Test hybride OmniEurope.Blazor' } else { 'OmniEurope.Blazor hybrid test' }
    & node (Join-Path $PSScriptRoot 'Test-CdpProbe.mjs') --endpoint "http://127.0.0.1:$Port" --selector '#hybrid-action' --output '#hybrid-count' --expected '1' --assert-language $expectedLanguage --assert-title $expectedTitle
    if ($LASTEXITCODE -ne 0) { throw ($psText.CdpFailed -f 'Hybrid', $LASTEXITCODE) }
    Write-Host ($psText.HybridPassed -f $process.Id)
}
catch {
    if (Test-Path -LiteralPath $stdout.FullName) { Get-Content -LiteralPath $stdout.FullName | Write-Host }
    if (Test-Path -LiteralPath $stderr.FullName) { Get-Content -LiteralPath $stderr.FullName | Write-Host }
    throw
}
finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id
        $process.WaitForExit(5000) | Out-Null
    }
    $env:WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS = $previousArguments
    $env:WEBVIEW2_USER_DATA_FOLDER = $previousUserDataFolder
    Remove-Item -LiteralPath $userDataFolder -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $stdout.FullName, $stderr.FullName -Force -ErrorAction SilentlyContinue
}

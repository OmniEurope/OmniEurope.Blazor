[CmdletBinding()]
param(
    [int]$Port = 5195,
    [int]$BrowserPort = 9229,
    [string]$WebRoot = (Join-Path $PSScriptRoot '..\artifacts\showcase-smoke\wwwroot'),
    # Pickers: the date, time and date and time pickers (PLAN-008 T18 a, T19).
    # Density: the T22 control, every sized element changes height between compact and spacious.
    [ValidateSet('Pickers', 'Density')]
    [string[]]$Probe = @('Pickers', 'Density')
)

# Serves the published showcase (dotnet publish site/OmniEurope.Blazor.Showcase -o artifacts/showcase-smoke)
# with its own `_headers`, strict CSP included, opens a headless Chromium with CDP and runs the
# showcase probes in turn. Every probe runs even when an earlier one fails; the script fails if any
# does. It owns and stops only the server and the browser it started.

$ErrorActionPreference = 'Stop'
$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
$resolvedRoot = (Resolve-Path -LiteralPath $WebRoot).Path
$baseUri = "http://127.0.0.1:$Port"
$stdout = New-TemporaryFile
$stderr = New-TemporaryFile
$server = $null
$browser = $null
$browserProfile = $null
$scripts = @{
    Pickers = 'Test-ShowcasePickerProbe.mjs'
    Density = 'Test-ShowcaseDensityProbe.mjs'
}

try {
    $existingHost = $false
    try {
        Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/" -TimeoutSec 1 -ErrorAction Stop | Out-Null
        $existingHost = $true
    }
    catch {
    }
    if ($existingHost) { throw ($psText.PortOwned -f $Port) }

    $serverStart = @{
        FilePath = 'node'
        ArgumentList = @((Join-Path $PSScriptRoot 'Serve-StaticWithHeaders.mjs'), '--root', $resolvedRoot, '--port', $Port)
        WorkingDirectory = $PSScriptRoot
        PassThru = $true
        RedirectStandardOutput = $stdout.FullName
        RedirectStandardError = $stderr.FullName
    }
    if ($IsWindows) { $serverStart.WindowStyle = 'Hidden' }
    $server = Start-Process @serverStart

    $ready = $false
    for ($attempt = 1; $attempt -le 75; $attempt++) {
        if ($server.HasExited) { throw ($psText.HostStopped -f 'vitrine', $server.ExitCode) }
        try {
            $response = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/" -TimeoutSec 2 -ErrorAction Stop
            if ($response.StatusCode -eq 200) { $ready = $true; break }
        }
        catch {
        }
        Start-Sleep -Milliseconds 200
    }
    if (-not $ready) { throw ($psText.HostNoHttp200 -f 'vitrine') }

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

    $browserProfile = Join-Path ([IO.Path]::GetTempPath()) ("omni-showcase-cdp-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $browserProfile | Out-Null
    $browserArguments = @(
        '--headless=new', '--disable-gpu', '--disable-dev-shm-usage', '--no-sandbox',
        '--lang=fr', '--accept-lang=fr',
        "--remote-debugging-port=$BrowserPort", "--user-data-dir=$browserProfile", 'about:blank'
    )
    $browserStart = @{ FilePath = $browserPath; ArgumentList = $browserArguments; PassThru = $true }
    if ($IsWindows) { $browserStart.WindowStyle = 'Hidden' }
    $browser = Start-Process @browserStart

    $failed = @()
    foreach ($name in $Probe) {
        & node (Join-Path $PSScriptRoot $scripts[$name]) --endpoint "http://127.0.0.1:$BrowserPort" --url "$baseUri/"
        if ($LASTEXITCODE -ne 0) { $failed += $name }
    }
    if ($failed.Count -gt 0) { throw ($psText.CdpFailed -f ('vitrine (' + ($failed -join ', ') + ')'), 1) }

    Write-Host "Vitrine validée : sondes $($Probe -join ', ') (PID $($server.Id))."
}
catch {
    if (Test-Path -LiteralPath $stderr.FullName) { Get-Content -LiteralPath $stderr.FullName | Write-Host }
    throw
}
finally {
    if ($server -and -not $server.HasExited) {
        Stop-Process -Id $server.Id
        $server.WaitForExit(5000) | Out-Null
    }
    if ($browser -and -not $browser.HasExited) {
        Stop-Process -Id $browser.Id
        $browser.WaitForExit(5000) | Out-Null
    }
    if ($browserProfile) {
        $resolvedProfile = [IO.Path]::GetFullPath($browserProfile)
        $resolvedTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
        if ($resolvedProfile.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase)) {
            Remove-Item -LiteralPath $resolvedProfile -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    Remove-Item -LiteralPath $stdout.FullName, $stderr.FullName -Force -ErrorAction SilentlyContinue
}

[CmdletBinding()]
param(
    [int]$Port = 5195,
    [string]$WebRoot = (Join-Path $PSScriptRoot '..\artifacts\showcase-smoke\wwwroot'),
    # Pickers: the date, time and date and time pickers (PLAN-004 T18 a, T19).
    # Density: the T22 control, every sized element changes height between compact and spacious.
    # Contrast: the lot 10 control, contrasts and geometry measured on every theme x palette x mode. Run
    # only when named (-Probe Contrast), owner decision of 2026-10-12: it is most of the pass's time.
    # AutoFit: the fit to content of a grid column, double click, off-screen virtual row and Enter.
    # MindMap: the mind map gestures, trusted drag, wheel zoom, keyboard, context menu, double click.
    # Modules: the grid filter popovers, the fold of the page header and the visual HTML editor.
    # Languages: no text leaves its box and no page scrolls sideways in German, Finnish, Greek and
    # Hungarian, at 1280 and 390 px.
    # Omitted: every probe of $scripts below, in its order, except Contrast.
    [ValidateSet('Pickers', 'Density', 'Contrast', 'AutoFit', 'MindMap', 'Modules', 'Languages', 'Scripts')]
    [string[]]$Probe,
    # PLAN-014: takes the V8 precise coverage of the package scripts during the probes and checks every
    # function of wwwroot/**/*.js against eng/js-coverage-baseline.json and eng/js-coverage-exceptions.json.
    [switch]$JsCoverage,
    # With -JsCoverage: rewrites eng/js-coverage-baseline.json from this pass, refusing any growing gap.
    [switch]$UpdateJsBaseline,
    # With -JsCoverage: keeps the takes in this folder instead of a temporary one, and merges those an
    # earlier pass left there, so a single probe can be rerun against the takes of all the others.
    [string]$JsCoverageDirectory,
    # Probe workers run at the same time, each driving its own headless browser (own temporary profile,
    # own debug port) against the one static server. 0 (default): a sixth of the logical processors,
    # between 1 and 6 (4 on 24 logical processors: 6 workers failed a transition wait once under outside load). 1: the probes one after the other in one browser, their output streamed, no shard.
    [ValidateRange(0, 16)]
    [int]$Parallel = 0
)

# Serves the published showcase (dotnet publish site/OmniEurope.Blazor.Showcase -o artifacts/showcase-smoke)
# with its own `_headers`, strict CSP included, opens a headless Chromium with CDP and runs the
# showcase probes. Every probe runs even when another one fails; the script fails if any does. It owns
# and stops only the server and the browsers it started.
#
# With more than one worker (-Parallel), the probes run as units on a pool of workers, the longest units
# first. Contrast is cut into 2 x workers shards by theme plus one for the shipped look, and Languages into
# 2 x workers shards of language x width pairs, eight at most (eng/ProbeShards.mjs): each shard checks
# its slice at full strength, and eng/Merge-ProbeShards.mjs then proves the shards cover the whole pass
# exactly once and gives the result of the whole probe (for Contrast, the one registry). Each unit runs
# in a fresh browser; its output is buffered and printed whole, in the probes' order.

$ErrorActionPreference = 'Stop'
$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
$resolvedRoot = (Resolve-Path -LiteralPath $WebRoot).Path
$baseUri = "http://127.0.0.1:$Port"
$stdout = New-TemporaryFile
$stderr = New-TemporaryFile
$server = $null
$browsers = [Collections.Concurrent.ConcurrentDictionary[string, object]]::new()
$profiles = [Collections.Concurrent.ConcurrentBag[string]]::new()
$shardDirectory = $null
$scripts = [ordered]@{
    Pickers = 'Test-ShowcasePickerProbe.mjs'
    Density = 'Test-ShowcaseDensityProbe.mjs'
    Contrast = 'Test-ThemeContrastProbe.mjs'
    AutoFit = 'Test-ShowcaseGridAutoFitProbe.mjs'
    MindMap = 'Test-ShowcaseMindMapProbe.mjs'
    Modules = 'Test-ShowcaseModulesProbe.mjs'
    Languages = 'Test-ShowcaseLanguagesProbe.mjs'
    Scripts = 'Test-ShowcaseScriptsProbe.mjs'
}
if (-not $Probe) { $Probe = @($scripts.Keys | Where-Object { $_ -ne 'Contrast' }) }

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

    if ($Parallel -eq 0) { $Parallel = [Math]::Max(1, [Math]::Min(6, [int][Math]::Floor([Environment]::ProcessorCount / 6))) }

    # The units of work: a probe whole, or one shard of it when workers run side by side. Weight is the
    # duration in seconds measured on the reference machine with six workers, used only to start the
    # longest units first. Contrast has 2 x workers theme shards plus the shipped look alone in the last
    # one, which weighs about one theme.
    $weights = @{ Contrast = 1500; Languages = 340; Scripts = 100; Density = 60; Modules = 16; Pickers = 15; AutoFit = 8; MindMap = 8 }
    $shardCounts = @{}
    if ($Parallel -gt 1) {
        $shardDirectory = Join-Path ([IO.Path]::GetTempPath()) ('omni-probe-shards-' + [guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $shardDirectory | Out-Null
        if ($Probe -contains 'Contrast') { $shardCounts.Contrast = 2 * $Parallel + 1 }
        # Languages walks 4 languages x 2 widths: at most one shard per pair, never an empty shard.
        if ($Probe -contains 'Languages') { $shardCounts.Languages = [Math]::Min(8, 2 * $Parallel) }
    }
    $units = [Collections.Generic.List[object]]::new()
    foreach ($name in $Probe) {
        $count = if ($shardCounts.ContainsKey($name)) { $shardCounts[$name] } else { 1 }
        for ($index = 1; $index -le $count; $index++) {
            $unitArguments = @()
            $label = $name
            $weight = $weights[$name]
            if ($shardCounts.ContainsKey($name)) {
                $label = "$name $index/$count"
                $unitArguments = @('--shard', "$index/$count", '--shard-file', (Join-Path $shardDirectory "$name-$index-of-$count.json"))
                $weight = if ($name -eq 'Contrast') { if ($index -eq $count) { 60 } else { $weight / ($count - 1) } } else { $weight / $count }
            }
            $units.Add([pscustomobject]@{
                Order = $units.Count
                Name = $label
                Script = Join-Path $PSScriptRoot $scripts[$name]
                Arguments = $unitArguments
                Weight = $weight
            })
        }
    }
    $workerCount = [Math]::Min($Parallel, $units.Count)

    $coverageDirectory = $null
    if ($JsCoverage) {
        $coverageDirectory = if ($JsCoverageDirectory) { $JsCoverageDirectory } else { Join-Path ([IO.Path]::GetTempPath()) ('omni-js-coverage-' + [guid]::NewGuid().ToString('N')) }
        New-Item -ItemType Directory -Path $coverageDirectory -Force | Out-Null
        $env:OMNI_JS_COVERAGE_DIR = $coverageDirectory
    }

    $failed = [Collections.Generic.List[string]]::new()
    $watch = [Diagnostics.Stopwatch]::StartNew()
    if ($workerCount -le 1) {
        # The original pass: one browser, the probes one after the other, their output streamed.
        $browser = & (Join-Path $PSScriptRoot 'Start-ProbeBrowser.ps1') -BrowserPath $browserPath
        $browsers[$browser.Profile] = $browser
        $profiles.Add($browser.Profile)
        foreach ($unit in $units) {
            & node $unit.Script @($unit.Arguments) --endpoint "http://127.0.0.1:$($browser.Port)" --url "$baseUri/"
            if ($LASTEXITCODE -ne 0) { $failed.Add($unit.Name) }
        }
    }
    else {
        # Every unit gets a fresh browser, as the first probe of the original pass did: a probe may leave
        # a theme, a mode or a language in the browser's storage, and no unit may depend on which unit ran
        # before it on the same worker.
        Write-Host ($psText.ProbePoolStarted -f $units.Count, $workerCount)
        $done = @{}
        $nextToPrint = 0
        $siteUrl = "$baseUri/"
        $engineering = $PSScriptRoot
        $units | Sort-Object -Property Weight -Descending -Stable | ForEach-Object -ThrottleLimit $workerCount -Parallel {
            $unit = $_
            $live = $using:browsers
            $unitWatch = [Diagnostics.Stopwatch]::StartNew()
            $browser = $null
            try {
                $browser = & (Join-Path $using:engineering 'Start-ProbeBrowser.ps1') -BrowserPath $using:browserPath
                $live[$browser.Profile] = $browser
                $created = $using:profiles
                $created.Add($browser.Profile)
                $lines = @(& node $unit.Script @($unit.Arguments) --endpoint "http://127.0.0.1:$($browser.Port)" --url $using:siteUrl 2>&1 | ForEach-Object { "$_" })
                $code = $LASTEXITCODE
            }
            catch {
                $lines = @("$_")
                $code = 1
            }
            finally {
                if ($browser) {
                    & (Join-Path $using:engineering 'Stop-ProbeBrowser.ps1') -Browser $browser.Process -BrowserProfile $browser.Profile -RemoveProfile
                    $removed = $null
                    $live.TryRemove($browser.Profile, [ref]$removed) | Out-Null
                }
            }
            [pscustomobject]@{ Order = $unit.Order; Name = $unit.Name; ExitCode = $code; Seconds = [int]$unitWatch.Elapsed.TotalSeconds; Lines = $lines }
        } | ForEach-Object {
            $result = $_
            $status = if ($result.ExitCode -eq 0) { $psText.ProbeUnitPassed } else { $psText.ProbeUnitFailed -f $result.ExitCode }
            Write-Host ($psText.ProbeUnitDone -f $result.Name, $status, $result.Seconds, [int]$watch.Elapsed.TotalSeconds)
            $done[$result.Order] = $result
            # Each unit's output whole, in the probes' order, as soon as every unit before it is done.
            while ($done.ContainsKey($nextToPrint)) {
                $ready = $done[$nextToPrint]
                $readyStatus = if ($ready.ExitCode -eq 0) { $psText.ProbeUnitPassed } else { $psText.ProbeUnitFailed -f $ready.ExitCode }
                Write-Host ($psText.ProbeUnitHeader -f $ready.Name, $readyStatus, $ready.Seconds)
                $ready.Lines | ForEach-Object { Write-Host $_ }
                $nextToPrint++
            }
        }
        foreach ($order in 0..($units.Count - 1)) {
            if (-not $done.ContainsKey($order)) { $failed.Add($units[$order].Name) }
            elseif ($done[$order].ExitCode -ne 0) { $failed.Add($done[$order].Name) }
        }
        foreach ($name in $Probe | Where-Object { $shardCounts.ContainsKey($_) }) {
            Write-Host ($psText.ProbeMergeHeader -f $name, $shardCounts[$name])
            & node (Join-Path $PSScriptRoot 'Merge-ProbeShards.mjs') --probe $name --directory $shardDirectory --count $shardCounts[$name]
            if ($LASTEXITCODE -ne 0) { $failed.Add($name) }
        }
    }
    Write-Host ($psText.ProbeDuration -f [int]$watch.Elapsed.TotalSeconds, $workerCount)
    if ($JsCoverage) {
        $gateArguments = @((Join-Path $PSScriptRoot 'Test-JsCoverage.mjs'), '--coverage', $coverageDirectory)
        if ($UpdateJsBaseline) { $gateArguments += '--update-baseline' }
        & node @gateArguments
        if ($LASTEXITCODE -ne 0) { $failed.Add('JsCoverage') }
    }
    if ($failed.Count -gt 0) { throw ($psText.CdpFailed -f ('vitrine (' + ($failed -join ', ') + ')'), 1) }

    Write-Host ($psText.ShowcasePassed -f ($Probe -join ', '), $server.Id)
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
    # The browsers still running: the sequential one, or those of units an interruption cut short.
    foreach ($browser in @($browsers.Values)) {
        & (Join-Path $PSScriptRoot 'Stop-ProbeBrowser.ps1') -Browser $browser.Process -BrowserProfile $browser.Profile -RemoveProfile
    }
    # A profile a unit could not delete (its files still locked when the unit ended): stopped and removed
    # once more, and named if it still stays.
    foreach ($browserProfile in @($profiles) | Where-Object { Test-Path -LiteralPath $_ }) {
        & (Join-Path $PSScriptRoot 'Stop-ProbeBrowser.ps1') -BrowserProfile $browserProfile -RemoveProfile
        if (Test-Path -LiteralPath $browserProfile) { Write-Warning ($psText.ProbeProfileLeft -f $browserProfile) }
    }
    if ($shardDirectory) { Remove-Item -LiteralPath $shardDirectory -Recurse -Force -ErrorAction SilentlyContinue }
    if ($coverageDirectory) {
        Remove-Item Env:OMNI_JS_COVERAGE_DIR -ErrorAction SilentlyContinue
        if (-not $JsCoverageDirectory) { Remove-Item -LiteralPath $coverageDirectory -Recurse -Force -ErrorAction SilentlyContinue }
    }
    Remove-Item -LiteralPath $stdout.FullName, $stderr.FullName -Force -ErrorAction SilentlyContinue
}

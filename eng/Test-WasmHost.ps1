[CmdletBinding()]
param(
    [int]$Port = 5190,
    [int]$BrowserPort = 9224,
    [string]$WebRoot = (Join-Path $PSScriptRoot '..\artifacts\wasm-smoke\wwwroot')
)

$ErrorActionPreference = 'Stop'
$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
$resolvedRoot = (Resolve-Path -LiteralPath $WebRoot).Path
$baseUri = "http://127.0.0.1:$Port"
$stdout = New-TemporaryFile
$stderr = New-TemporaryFile
$server = $null
$browser = $null
$browserProfile = $null

try {
    $existingHost = $false
    try {
        Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/" -TimeoutSec 1 -ErrorAction Stop | Out-Null
        $existingHost = $true
    }
    catch {
    }
    if ($existingHost) {
        throw ($psText.PortOwned -f $Port)
    }

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

    $rootResponse = $null
    for ($attempt = 1; $attempt -le 75; $attempt++) {
        if ($server.HasExited) { throw ($psText.HostStopped -f 'WebAssembly', $server.ExitCode) }
        try {
            $rootResponse = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/" -TimeoutSec 2 -ErrorAction Stop
            if ($rootResponse.StatusCode -eq 200) { break }
        }
        catch {
        }
        Start-Sleep -Milliseconds 200
    }

    if ($null -eq $rootResponse -or $rootResponse.StatusCode -ne 200) { throw ($psText.HostNoHttp200 -f 'WebAssembly') }
    if ($rootResponse.Content -notmatch 'blazor\.webassembly\.js' -or $rootResponse.Content -notmatch 'id="app"') {
        throw $psText.WasmMarkersMissing
    }
    $rootCache = ($rootResponse.Headers['Cache-Control'] -join ', ')
    $rootCacheTokens = @($rootCache -split ',' | ForEach-Object { $_.Trim() } | Sort-Object -Unique)
    if ($rootCacheTokens.Count -ne 3 -or @('must-revalidate', 'no-cache', 'no-store').Where({ $_ -notin $rootCacheTokens }).Count -ne 0) {
        throw "Cache-Control du shell WebAssembly invalide : $rootCache"
    }
    $frameworkWasm = Get-ChildItem -LiteralPath (Join-Path $resolvedRoot '_framework') -Filter '*.wasm' -File | Select-Object -First 1
    if ($null -eq $frameworkWasm) { throw 'Aucun asset WebAssembly fingerprinté à vérifier.' }
    $wasmResponse = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri/_framework/$([Uri]::EscapeDataString($frameworkWasm.Name))" -TimeoutSec 3
    $assetCache = ($wasmResponse.Headers['Cache-Control'] -join ', ')
    $assetCacheTokens = @($assetCache -split ',' | ForEach-Object { $_.Trim() } | Sort-Object -Unique)
    if ($assetCacheTokens.Count -ne 3 -or @('immutable', 'max-age=31536000', 'public').Where({ $_ -notin $assetCacheTokens }).Count -ne 0) {
        throw "Cache-Control d'asset WebAssembly invalide : $assetCache"
    }

    $csp = ($rootResponse.Headers['Content-Security-Policy'] -join '; ')
    if ([string]::IsNullOrWhiteSpace($csp)) { throw $psText.CspHeaderMissing }
    if ($csp -match "'unsafe-inline'|'unsafe-eval'") { throw "La CSP contient une directive interdite : $csp" }
    if ($csp -notmatch "script-src[^;]*'wasm-unsafe-eval'") { throw "La CSP ne permet pas l'initialisation WebAssembly : $csp" }
    if ($csp -notmatch "connect-src\s+'self'(?:;|$)" -or $csp -match 'wss?:') { throw "La directive connect-src autorise une connexion distante : $csp" }

    foreach ($path in @('/_framework/blazor.webassembly.js', '/_content/OmniEurope.Blazor/omnieurope.blazor.css', '/app.css')) {
        $asset = Invoke-WebRequest -UseBasicParsing -Uri "$baseUri$path" -TimeoutSec 3
        if ($asset.StatusCode -ne 200 -or $asset.RawContentLength -le 0) { throw "Asset WebAssembly invalide : $path" }
    }

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

    $browserProfile = Join-Path ([IO.Path]::GetTempPath()) ("omni-wasm-cdp-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $browserProfile | Out-Null
    $browserArguments = @(
        '--headless=new', '--disable-gpu', '--disable-dev-shm-usage', '--no-sandbox',
        "--remote-debugging-port=$BrowserPort", "--user-data-dir=$browserProfile", $baseUri
    )
    $browserStart = @{ FilePath = $browserPath; ArgumentList = $browserArguments; PassThru = $true }
    if ($IsWindows) { $browserStart.WindowStyle = 'Hidden' }
    $browser = Start-Process @browserStart

    & node (Join-Path $PSScriptRoot 'Test-CdpProbe.mjs') `
        --endpoint "http://127.0.0.1:$BrowserPort" `
        --selector '#wasm-action' `
        --output '#wasm-action' `
        --expected 'Compteur : 1' `
        --assert-selector '[role="progressbar"]' `
        --assert-attribute 'aria-valuenow' `
        --assert-expected '1' `
        --assert-language 'fr' `
        --assert-title 'Test WebAssembly OmniEurope.Blazor'
    if ($LASTEXITCODE -ne 0) { throw ($psText.CdpFailed -f 'WebAssembly', $LASTEXITCODE) }

    Write-Host ($psText.WasmPassed -f $server.Id)
}
catch {
    if (Test-Path -LiteralPath $stdout.FullName) { Get-Content -LiteralPath $stdout.FullName | Write-Host }
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

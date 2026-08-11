[CmdletBinding()]
param(
    [int]$Port = 9224,
    [string]$ExecutablePath = (Join-Path $PSScriptRoot '..\samples\OmniEurope.Blazor.HybridSmoke\bin\Release\net10.0-windows10.0.19041.0\win-x64\OmniEurope.Blazor.HybridSmoke.exe')
)

$ErrorActionPreference = 'Stop'
$psText = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'PowerShellMessages.psd1')
if (-not $IsWindows) { throw $psText.HybridWindowsRequired }

$resolvedExecutable = (Resolve-Path -LiteralPath $ExecutablePath).Path
$stdout = New-TemporaryFile
$stderr = New-TemporaryFile
$process = $null
$previousArguments = $env:WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS
$hostPage = Join-Path $PSScriptRoot '..\samples\OmniEurope.Blazor.HybridSmoke\wwwroot\index.html'

try {
    $hostHtml = Get-Content -Raw -LiteralPath $hostPage
    if ($hostHtml -notmatch 'http-equiv="Content-Security-Policy"') { throw 'La coque Hybrid ne déclare aucune CSP.' }
    if ($hostHtml -match "'unsafe-inline'|'unsafe-eval'") { throw 'La coque Hybrid contient une directive CSP interdite.' }

    $portOccupied = $false
    try {
        Invoke-RestMethod -Uri "http://127.0.0.1:$Port/json/list" -TimeoutSec 1 -ErrorAction Stop | Out-Null
        $portOccupied = $true
    }
    catch {
    }
    if ($portOccupied) { throw ($psText.HybridPortOwned -f $Port) }

    $env:WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS = "--remote-debugging-port=$Port --remote-allow-origins=*"
    $start = @{
        FilePath = $resolvedExecutable
        WorkingDirectory = Split-Path -Parent $resolvedExecutable
        PassThru = $true
        RedirectStandardOutput = $stdout.FullName
        RedirectStandardError = $stderr.FullName
        WindowStyle = 'Hidden'
    }
    $process = Start-Process @start

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
    Remove-Item -LiteralPath $stdout.FullName, $stderr.FullName -Force -ErrorAction SilentlyContinue
}

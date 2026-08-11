[CmdletBinding()]
param(
    [string]$PublishDirectory = (Join-Path $PSScriptRoot '..\artifacts\wasm-smoke')
)

$ErrorActionPreference = 'Stop'
$headersPath = Join-Path $PublishDirectory 'wwwroot\_headers'
if (-not (Test-Path -LiteralPath $headersPath)) {
    $headersPath = Join-Path $PublishDirectory '_headers'
}
if (-not (Test-Path -LiteralPath $headersPath)) {
    throw "Published WebAssembly header manifest not found under $PublishDirectory."
}

$headers = Get-Content -Raw -LiteralPath $headersPath
$required = @(
    "frame-ancestors 'none'",
    "connect-src 'self'",
    "script-src 'self' 'wasm-unsafe-eval'",
    "X-Content-Type-Options: nosniff",
    "Referrer-Policy: no-referrer",
    'Permissions-Policy:'
    'Cache-Control: no-cache, no-store, must-revalidate'
    'Cache-Control: public, max-age=31536000, immutable'
)
foreach ($value in $required) {
    if (-not $headers.Contains($value, [System.StringComparison]::Ordinal)) {
        throw "Published WebAssembly header manifest is missing: $value"
    }
}
if ($headers -match '(?i)connect-src[^\r\n;]*(?:\bws:|\bwss:)') {
    throw 'Published WebAssembly connect-src must not allow arbitrary WebSocket origins.'
}

Write-Host "WebAssembly deployment headers passed: $headersPath"

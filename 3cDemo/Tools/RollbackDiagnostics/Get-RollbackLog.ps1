param(
    [string]$LogPath = (Join-Path $env:LOCALAPPDATA "Unity\Editor\Editor.log"),
    [int]$Tail = 500,
    [switch]$Follow,
    [switch]$IncludeUnityErrors
)

$markers = @(
    "ROLLBACK_SOAK_RESULT",
    "ROLLBACK_SOAK_FIRST_MISMATCH",
    "ROLLBACK_TIMING_PROBE",
    "\[rollback-synctest\]"
)

if ($IncludeUnityErrors) {
    $markers += @("Exception", "Error", "error CS\d+")
}

if (-not (Test-Path -LiteralPath $LogPath)) {
    throw "Log not found: $LogPath"
}

if ($Tail -lt 1) {
    $Tail = 1
}

$pattern = "(" + ($markers -join "|") + ")"

if ($Follow) {
    Get-Content -LiteralPath $LogPath -Encoding UTF8 -Tail $Tail -Wait | Where-Object { $_ -match $pattern }
    return
}

Get-Content -LiteralPath $LogPath -Encoding UTF8 -Tail $Tail | Where-Object { $_ -match $pattern }

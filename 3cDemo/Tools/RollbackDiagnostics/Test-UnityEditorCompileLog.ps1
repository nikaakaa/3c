param(
    [string]$LogPath = (Join-Path $env:LOCALAPPDATA "Unity\Editor\Editor.log"),
    [int]$Tail = 5000,
    [switch]$ShowMatches
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $LogPath)) {
    Write-Output "UNITY_COMPILE_LOG_CHECK result=FAIL reason=log-not-found path=$LogPath"
    exit 1
}

if ($Tail -lt 1) {
    $Tail = 1
}

$patterns = @(
    "error CS\d+",
    "Compilation failed",
    "Scripts have compiler errors",
    "All compiler errors have to be fixed"
)

$pattern = "(" + ($patterns -join "|") + ")"
$matches = Get-Content -LiteralPath $LogPath -Encoding UTF8 -Tail $Tail | Where-Object { $_ -match $pattern }
$count = @($matches).Count

if ($count -gt 0) {
    Write-Output "UNITY_COMPILE_LOG_CHECK result=FAIL matches=$count tail=$Tail"
    if ($ShowMatches) {
        $matches | Select-Object -Last 20
    }
    exit 1
}

Write-Output "UNITY_COMPILE_LOG_CHECK result=PASS tail=$Tail"
exit 0

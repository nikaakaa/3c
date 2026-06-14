param(
    [string]$LogPath = (Join-Path $env:LOCALAPPDATA "Unity\Editor\Editor.log"),
    [int]$Tail = 5000
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $LogPath)) {
    Write-Output "ROLLBACK_SYNCTEST_ASSERT result=FAIL reason=log-not-found path=$LogPath"
    exit 1
}

if ($Tail -lt 1) {
    $Tail = 1
}

$lines = Get-Content -LiteralPath $LogPath -Encoding UTF8 -Tail $Tail
$synctestLine = $lines | Where-Object { $_ -match "\[rollback-synctest\] (PASS|FAIL)" } | Select-Object -Last 1
$timingLine = $lines | Where-Object { $_ -match "ROLLBACK_TIMING_PROBE" } | Select-Object -Last 1

if ([string]::IsNullOrWhiteSpace($synctestLine)) {
    Write-Output "ROLLBACK_SYNCTEST_ASSERT result=FAIL reason=missing-rollback-synctest tail=$Tail"
    exit 1
}

if ($synctestLine -notmatch "\[rollback-synctest\] PASS") {
    $hasFirstMismatch = @($lines | Where-Object { $_ -match "\[rollback-synctest\] first-mismatch" }).Count -gt 0
    Write-Output "ROLLBACK_SYNCTEST_ASSERT result=FAIL reason=synctest-not-pass hasFirstMismatch=$hasFirstMismatch"
    exit 1
}

if (-not [string]::IsNullOrWhiteSpace($timingLine) -and $timingLine -match "result=FAIL") {
    Write-Output "ROLLBACK_SYNCTEST_ASSERT result=FAIL reason=timing-probe-fail"
    exit 1
}

Write-Output "ROLLBACK_SYNCTEST_ASSERT result=PASS tail=$Tail timingProbePresent=$(-not [string]::IsNullOrWhiteSpace($timingLine))"
exit 0

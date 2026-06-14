param(
    [string]$LogPath = (Join-Path $env:LOCALAPPDATA "Unity\Editor\Editor.log"),
    [int]$Tail = 5000
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $LogPath)) {
    Write-Output "ROLLBACK_SOAK_ASSERT result=FAIL reason=log-not-found path=$LogPath"
    exit 1
}

if ($Tail -lt 1) {
    $Tail = 1
}

$lines = Get-Content -LiteralPath $LogPath -Encoding UTF8 -Tail $Tail
$resultLine = $lines | Where-Object { $_ -match "ROLLBACK_SOAK_RESULT" } | Select-Object -Last 1

if ([string]::IsNullOrWhiteSpace($resultLine)) {
    Write-Output "ROLLBACK_SOAK_ASSERT result=FAIL reason=missing-ROLLBACK_SOAK_RESULT tail=$Tail"
    exit 1
}

$requiredTokens = @(
    "result=PASS",
    "applyReplay=False",
    "sourceRestored=True",
    "visualRestored=True",
    "cameraLocalOnly=True",
    "visualChecked=True"
)

$missing = @()
foreach ($token in $requiredTokens) {
    if (-not $resultLine.Contains($token)) {
        $missing += $token
    }
}

if ($missing.Count -gt 0) {
    $firstMismatch = $lines | Where-Object { $_ -match "ROLLBACK_SOAK_FIRST_MISMATCH" } | Select-Object -Last 1
    $reason = "missing=" + ($missing -join ",")
    if (-not [string]::IsNullOrWhiteSpace($firstMismatch)) {
        Write-Output "ROLLBACK_SOAK_ASSERT result=FAIL reason=$reason hasFirstMismatch=True"
        exit 1
    }

    Write-Output "ROLLBACK_SOAK_ASSERT result=FAIL reason=$reason hasFirstMismatch=False"
    exit 1
}

Write-Output "ROLLBACK_SOAK_ASSERT result=PASS tail=$Tail"
exit 0

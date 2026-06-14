param(
    [string]$LogPath = (Join-Path $env:LOCALAPPDATA "Unity\Editor\Editor.log"),
    [int]$TimeoutSec = 120,
    [int]$PollIntervalSec = 2,
    [int]$Tail = 5000,
    [long]$InitialLength = -1,
    [switch]$SkipPreflight
)

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot

function Invoke-Step([string]$Name, [scriptblock]$Step) {
    & $Step
    if ($LASTEXITCODE -ne 0) {
        Write-Output "ROLLBACK_SYNCTEST_HITL result=FAIL stage=$Name"
        exit 1
    }
}

if ($PollIntervalSec -lt 1) {
    $PollIntervalSec = 1
}

if ($TimeoutSec -lt 1) {
    $TimeoutSec = 1
}

if (-not (Test-Path -LiteralPath $LogPath)) {
    Write-Output "ROLLBACK_SYNCTEST_HITL result=FAIL stage=log reason=log-not-found path=$LogPath"
    exit 1
}

if (-not $SkipPreflight) {
    Invoke-Step "wiring" { powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $scriptRoot "Test-RollbackWiring.ps1") }
    Invoke-Step "compile-log" { powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $scriptRoot "Test-UnityEditorCompileLog.ps1") -LogPath $LogPath -Tail $Tail }
}

Write-Output "ROLLBACK_SYNCTEST_HITL waiting=True timeoutSec=$TimeoutSec action=enter-play-mode-and-press-F6"

$deadline = (Get-Date).AddSeconds($TimeoutSec)
$seen = $false
$currentLogLength = (Get-Item -LiteralPath $LogPath).Length
$startLength = $currentLogLength
if ($InitialLength -ge 0 -and $InitialLength -le $currentLogLength) {
    $startLength = $InitialLength
}

while ((Get-Date) -lt $deadline) {
    $currentLength = (Get-Item -LiteralPath $LogPath).Length
    if ($currentLength -gt $startLength) {
        $lines = Get-Content -LiteralPath $LogPath -Encoding UTF8 -Tail $Tail
        $synctestLine = $lines | Where-Object { $_ -match "\[rollback-synctest\] (PASS|FAIL)" } | Select-Object -Last 1
        if (-not [string]::IsNullOrWhiteSpace($synctestLine)) {
            $seen = $true
            break
        }
    }

    Start-Sleep -Seconds $PollIntervalSec
}

if (-not $seen) {
    Write-Output "ROLLBACK_SYNCTEST_HITL result=FAIL stage=wait reason=missing-rollback-synctest timeoutSec=$TimeoutSec"
    exit 1
}

powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $scriptRoot "Test-RollbackSynctestResult.ps1") -LogPath $LogPath -Tail $Tail
if ($LASTEXITCODE -ne 0) {
    Write-Output "ROLLBACK_SYNCTEST_HITL result=FAIL stage=assert"
    exit 1
}

Write-Output "ROLLBACK_SYNCTEST_HITL result=PASS"
exit 0

param(
    [string]$LogPath = (Join-Path $env:LOCALAPPDATA "Unity\Editor\Editor.log"),
    [int]$TimeoutSec = 120,
    [int]$PollIntervalSec = 2,
    [int]$Tail = 5000,
    [switch]$SkipPreflight,
    [switch]$ConfirmVisualStable
)

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot

function Invoke-Step([string]$Name, [scriptblock]$Step) {
    & $Step
    if ($LASTEXITCODE -ne 0) {
        Write-Output "ROLLBACK_DEMO_HITL result=FAIL stage=$Name"
        exit 1
    }
}

if (-not (Test-Path -LiteralPath $LogPath)) {
    Write-Output "ROLLBACK_DEMO_HITL result=FAIL stage=log reason=log-not-found path=$LogPath"
    exit 1
}

if (-not $SkipPreflight) {
    Invoke-Step "wiring" { powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $scriptRoot "Test-RollbackWiring.ps1") }
    Invoke-Step "compile-log" { powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $scriptRoot "Test-UnityEditorCompileLog.ps1") -LogPath $LogPath -Tail $Tail }
}

$demoInitialLength = (Get-Item -LiteralPath $LogPath).Length
Write-Output "ROLLBACK_DEMO_HITL step=F6 action=enter-play-mode-move-and-press-F6"
Invoke-Step "f6-synctest" {
    powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $scriptRoot "Invoke-RollbackSynctestHitl.ps1") `
        -LogPath $LogPath `
        -TimeoutSec $TimeoutSec `
        -PollIntervalSec $PollIntervalSec `
        -Tail $Tail `
        -InitialLength $demoInitialLength `
        -SkipPreflight
}

Write-Output "ROLLBACK_DEMO_HITL step=F8 action=keep-play-mode-move-and-press-F8"
Invoke-Step "f8-soak" {
    powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $scriptRoot "Invoke-RollbackSoakHitl.ps1") `
        -LogPath $LogPath `
        -TimeoutSec $TimeoutSec `
        -PollIntervalSec $PollIntervalSec `
        -Tail $Tail `
        -InitialLength $demoInitialLength `
        -SkipPreflight
}

if ($ConfirmVisualStable) {
    Write-Output "ROLLBACK_DEMO_HITL result=PASS visualConfirmed=True"
    exit 0
}

Write-Output "ROLLBACK_DEMO_HITL result=PASS visualConfirmed=False action=confirm-hidden-f6-f8-did-not-shift-character-or-camera"
exit 0

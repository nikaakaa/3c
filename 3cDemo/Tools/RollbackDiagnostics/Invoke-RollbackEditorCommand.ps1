param(
    [ValidateSet("synctest", "soak")]
    [string]$Command = "soak",
    [string]$ProjectPath = (Join-Path (Get-Location) "3cDemo\Client\3C_Client"),
    [string]$LogPath = (Join-Path $env:LOCALAPPDATA "Unity\Editor\Editor.log"),
    [int]$TimeoutSec = 180,
    [int]$PollIntervalSec = 2,
    [int]$Tail = 5000,
    [int]$Seed = 12345,
    [int]$TickCount = 600,
    [int]$RollbackFrames = 8,
    [switch]$KeepPlayMode
)

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot

if ($PollIntervalSec -lt 1) {
    $PollIntervalSec = 1
}

if ($TimeoutSec -lt 1) {
    $TimeoutSec = 1
}

if (-not (Test-Path -LiteralPath $ProjectPath)) {
    Write-Output "ROLLBACK_EDITOR_COMMAND_ASSERT result=FAIL reason=project-not-found path=$ProjectPath"
    exit 1
}

if (-not (Test-Path -LiteralPath $LogPath)) {
    Write-Output "ROLLBACK_EDITOR_COMMAND_ASSERT result=FAIL reason=log-not-found path=$LogPath"
    exit 1
}

$commandDir = Join-Path $ProjectPath "Library\RollbackDiagnostics"
New-Item -ItemType Directory -Force -Path $commandDir | Out-Null

$commandPath = Join-Path $commandDir "Command.json"
$resultPath = Join-Path $commandDir "Result.json"
$id = [guid]::NewGuid().ToString("N")

if (Test-Path -LiteralPath $resultPath) {
    Remove-Item -LiteralPath $resultPath -Force
}

$payload = [ordered]@{
    id = $id
    command = $Command
    scene = "Assets/Scenes/Sandbox.unity"
    timeoutSeconds = $TimeoutSec
    seed = $Seed
    tickCount = $TickCount
    rollbackFrames = $RollbackFrames
    stopOnFailure = $true
    exitPlayMode = -not $KeepPlayMode
}

($payload | ConvertTo-Json -Compress) | Set-Content -LiteralPath $commandPath -Encoding UTF8
Write-Output "ROLLBACK_EDITOR_COMMAND_ASSERT waiting=True command=$Command id=$id timeoutSec=$TimeoutSec"

$deadline = (Get-Date).AddSeconds($TimeoutSec)
$result = $null
while ((Get-Date) -lt $deadline) {
    if (Test-Path -LiteralPath $resultPath) {
        try {
            $candidate = Get-Content -LiteralPath $resultPath -Encoding UTF8 -Raw | ConvertFrom-Json
            if ($candidate.id -eq $id) {
                $result = $candidate
                break
            }
        }
        catch {
        }
    }

    Start-Sleep -Seconds $PollIntervalSec
}

if ($null -eq $result) {
    $commandStillPending = Test-Path -LiteralPath $commandPath
    Write-Output "ROLLBACK_EDITOR_COMMAND_ASSERT result=FAIL stage=wait reason=missing-result command=$Command id=$id timeoutSec=$TimeoutSec commandPending=$commandStillPending commandPath=$commandPath"
    exit 1
}

if (-not $result.success) {
    Write-Output "ROLLBACK_EDITOR_COMMAND_ASSERT result=FAIL stage=editor command=$Command id=$id reason=$($result.reason)"
    exit 1
}

if ($Command -eq "synctest") {
    powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $scriptRoot "Test-RollbackSynctestResult.ps1") -LogPath $LogPath -Tail $Tail
    if ($LASTEXITCODE -ne 0) {
        Write-Output "ROLLBACK_EDITOR_COMMAND_ASSERT result=FAIL stage=synctest-assert command=$Command id=$id"
        exit 1
    }
}
elseif ($Command -eq "soak") {
    powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $scriptRoot "Test-RollbackSoakResult.ps1") -LogPath $LogPath -Tail $Tail
    if ($LASTEXITCODE -ne 0) {
        Write-Output "ROLLBACK_EDITOR_COMMAND_ASSERT result=FAIL stage=soak-assert command=$Command id=$id"
        exit 1
    }
}

Write-Output "ROLLBACK_EDITOR_COMMAND_ASSERT result=PASS command=$Command id=$id"
exit 0

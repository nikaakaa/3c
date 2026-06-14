param(
    [int]$TimeoutSec = 30,
    [int]$PollIntervalSec = 1
)

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot

if ($TimeoutSec -lt 5) {
    $TimeoutSec = 5
}

if ($PollIntervalSec -lt 1) {
    $PollIntervalSec = 1
}

function New-TempLogDir {
    $path = Join-Path $env:TEMP ("rollback-hitl-script-check-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $path | Out-Null
    return $path
}

function Invoke-DemoHitlSample([switch]$ConfirmVisualStable, [switch]$MissingF8) {
    $tmp = New-TempLogDir
    $log = Join-Path $tmp "Editor.log"
    $writer = Join-Path $tmp "writer.ps1"

    Set-Content -LiteralPath $log -Encoding UTF8 -Value "old unrelated log"

    $writerLines = @(
        'param([string]$LogPath, [string]$MissingF8)',
        '$skipF8 = $MissingF8 -eq "true"',
        'Start-Sleep -Seconds 2',
        'Add-Content -LiteralPath $LogPath -Encoding UTF8 -Value "[3C-DIAG][Warning][Simulation][Simulation.synctest-pass] frame=10 step=8 message=synctest [rollback-synctest] PASS restore=1 end=9"',
        'if (-not $skipF8) {',
        '    Start-Sleep -Seconds 2',
        '    Add-Content -LiteralPath $LogPath -Encoding UTF8 -Value "[3C-DIAG][Warning][Simulation][Simulation.rollback-soak-result] frame=20 step=0 message=ROLLBACK_SOAK_RESULT result=PASS seed=12345 tickCount=600 rollbackFrames=8 checkedWindows=592 applyReplay=False sourceRestored=True visualRestored=True cameraLocalOnly=True visualChecked=True"',
        '}'
    )
    Set-Content -LiteralPath $writer -Encoding UTF8 -Value $writerLines

    $args = @(
        "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", (Join-Path $scriptRoot "Invoke-RollbackDemoHitl.ps1"),
        "-LogPath", $log,
        "-TimeoutSec", $TimeoutSec,
        "-PollIntervalSec", $PollIntervalSec,
        "-SkipPreflight"
    )

    if ($ConfirmVisualStable) {
        $args += "-ConfirmVisualStable"
    }

    $writerArgs = @(
        "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $writer,
        "-LogPath", $log,
        "-MissingF8", $(if ($MissingF8) { "true" } else { "false" })
    )
    $writerProcess = Start-Process -FilePath "powershell" -ArgumentList $writerArgs -PassThru -WindowStyle Hidden

    $output = & powershell @args 2>&1
    $exitCode = $LASTEXITCODE

    $writerProcess.WaitForExit()
    $writerProcess.Refresh()

    $stdout = [string]::Join("`n", @($output | ForEach-Object { $_.ToString() }))

    Remove-Item -LiteralPath $tmp -Recurse -Force

    return [pscustomobject]@{
        ExitCode = $exitCode
        Stdout = $stdout
        Stderr = ""
    }
}

$passSample = Invoke-DemoHitlSample
if ($passSample.ExitCode -ne 0 -or $passSample.Stdout -notmatch "ROLLBACK_DEMO_HITL result=PASS visualConfirmed=False") {
    Write-Output "ROLLBACK_HITL_SCRIPT_CHECK result=FAIL case=pass-sample exitCode=$($passSample.ExitCode)"
    exit 1
}

$confirmSample = Invoke-DemoHitlSample -ConfirmVisualStable
if ($confirmSample.ExitCode -ne 0 -or $confirmSample.Stdout -notmatch "ROLLBACK_DEMO_HITL result=PASS visualConfirmed=True") {
    Write-Output "ROLLBACK_HITL_SCRIPT_CHECK result=FAIL case=confirm-sample exitCode=$($confirmSample.ExitCode)"
    exit 1
}

$missingF8Sample = Invoke-DemoHitlSample -MissingF8
if ($missingF8Sample.ExitCode -eq 0 -or $missingF8Sample.Stdout -notmatch "ROLLBACK_DEMO_HITL result=FAIL stage=f8-soak") {
    Write-Output "ROLLBACK_HITL_SCRIPT_CHECK result=FAIL case=missing-f8 exitCode=$($missingF8Sample.ExitCode)"
    exit 1
}

Write-Output "ROLLBACK_HITL_SCRIPT_CHECK result=PASS passSample=True confirmSample=True missingF8Fails=True"
exit 0

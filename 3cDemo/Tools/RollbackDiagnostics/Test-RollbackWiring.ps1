param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot "..\..\Client\3C_Client")
)

$ErrorActionPreference = "Stop"

$scenePath = Join-Path $ProjectRoot "Assets\Scenes\Sandbox.unity"
$soakRunnerPath = Join-Path $ProjectRoot "Assets\Scripts\Simulation\Rollback\LocalRollbackSoakDebugRunner.cs"
$synctestRunnerPath = Join-Path $ProjectRoot "Assets\Scripts\Simulation\Rollback\LocalRollbackSynctestDebugRunner.cs"
$latencyRunnerPath = Join-Path $ProjectRoot "Assets\Scripts\Simulation\Rollback\LocalLatencyReconciliationDebugRunner.cs"
$timingProbePath = Join-Path $ProjectRoot "Assets\Scripts\Simulation\Rollback\RollbackTimingProbe.cs"
$synctestLogFormatterPath = Join-Path $ProjectRoot "Assets\Scripts\Simulation\Rollback\LocalRollbackSynctestLogFormatter.cs"
$predictionInputSourcePath = Join-Path $ProjectRoot "Assets\Scripts\Simulation\Rollback\LocomotionPredictionInputFrameSource.cs"
$buttonAdapterPath = Join-Path $ProjectRoot "Assets\Scripts\Input\Runtime\UnityInputSystemRequestBufferAdapter.cs"
$editorCommandRunnerPath = Join-Path $ProjectRoot "Assets\Editor\RollbackDiagnostics\RollbackDiagnosticsCommandRunner.cs"
$editorCommandScriptPath = Join-Path $PSScriptRoot "Invoke-RollbackEditorCommand.ps1"

$requiredSceneTokens = @(
    "guid: f79063608d784da787c3554c8d0eda2d",
    "addedObject: {fileID: 1761501686}",
    "simulationBehaviour: {fileID: 1761501684}",
    "triggerKey: 287",
    "triggerKey: 288",
    "triggerKey: 289",
    "applyReplayResultToScene: 0"
)

$requiredRunnerTokens = @(
    "ROLLBACK_SOAK_RESULT",
    "ROLLBACK_SOAK_FIRST_MISMATCH",
    "sourceRestored=",
    "visualRestored=",
    "cameraLocalOnly=",
    "visualChecked="
)

$requiredSynctestTokens = @(
    "[rollback-synctest]"
)

$requiredLatencyTokens = @(
    "[reconciliation]",
    "outcome=",
    "predictionDiff=",
    "replayDifferences=",
    "applyReplayResultToScene"
)

$requiredTimingProbeTokens = @(
    "ROLLBACK_TIMING_PROBE",
    "cameraState=local-only"
)

function Read-Utf8([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing file: $Path"
    }

    return Get-Content -LiteralPath $Path -Encoding UTF8 -Raw
}

function Assert-Contains([string]$Name, [string]$Text, [string]$Token) {
    if (-not $Text.Contains($Token)) {
        throw "$Name missing token: $Token"
    }
}

function Get-ComponentBlock([string]$Scene, [string]$TriggerLine) {
    $triggerIndex = $Scene.IndexOf($TriggerLine, [StringComparison]::Ordinal)
    if ($triggerIndex -lt 0) {
        throw "Sandbox missing $TriggerLine"
    }

    $blockStart = $Scene.LastIndexOf("--- !u!114", $triggerIndex, [StringComparison]::Ordinal)
    if ($blockStart -lt 0) {
        throw "Cannot locate component block for $TriggerLine"
    }

    $blockEnd = $Scene.IndexOf("--- !u!", $triggerIndex + $TriggerLine.Length, [StringComparison]::Ordinal)
    if ($blockEnd -lt 0) {
        $blockEnd = $Scene.Length
    }

    return $Scene.Substring($blockStart, $blockEnd - $blockStart)
}

try {
    $scene = Read-Utf8 $scenePath
    $soakRunner = Read-Utf8 $soakRunnerPath
    $synctestRunner = Read-Utf8 $synctestRunnerPath
    $latencyRunner = Read-Utf8 $latencyRunnerPath
    $timingProbe = Read-Utf8 $timingProbePath
    $synctestLogFormatter = Read-Utf8 $synctestLogFormatterPath
    $predictionInputSource = Read-Utf8 $predictionInputSourcePath
    $buttonAdapter = Read-Utf8 $buttonAdapterPath
    $editorCommandRunner = Read-Utf8 $editorCommandRunnerPath
    $editorCommandScript = Read-Utf8 $editorCommandScriptPath

    foreach ($token in $requiredSceneTokens) {
        Assert-Contains "Sandbox" $scene $token
    }

    foreach ($triggerLine in @("triggerKey: 287", "triggerKey: 288", "triggerKey: 289")) {
        $block = Get-ComponentBlock $scene $triggerLine
        Assert-Contains "$triggerLine block" $block "presentationInterpolator: {fileID: 108039801}"
        Assert-Contains "$triggerLine block" $block "applyReplayResultToScene: 0"
    }

    foreach ($triggerLine in @("triggerKey: 287", "triggerKey: 289")) {
        $block = Get-ComponentBlock $scene $triggerLine
        Assert-Contains "$triggerLine block" $block "cameraController: {fileID: 5809153074833929713}"
    }

    foreach ($token in $requiredRunnerTokens) {
        Assert-Contains "LocalRollbackSoakDebugRunner" $soakRunner $token
    }

    foreach ($token in $requiredSynctestTokens) {
        Assert-Contains "LocalRollbackSynctestLogFormatter" $synctestLogFormatter $token
    }

    foreach ($token in $requiredLatencyTokens) {
        Assert-Contains "LocalLatencyReconciliationDebugRunner" $latencyRunner $token
    }

    foreach ($token in $requiredTimingProbeTokens) {
        Assert-Contains "RollbackTimingProbe" $timingProbe $token
    }

    Assert-Contains "LocalRollbackSynctestDebugRunner" $synctestRunner "RollbackTimingProbe.Format"

    Assert-Contains "LocomotionPredictionInputFrameSource" $predictionInputSource "IPredictionButtonFrameSource"
    Assert-Contains "LocomotionPredictionInputFrameSource" $predictionInputSource "GetComponentsInParent<MonoBehaviour>(true)"
    Assert-Contains "LocomotionPredictionInputFrameSource" $predictionInputSource "GetComponentsInChildren<MonoBehaviour>(true)"
    Assert-Contains "UnityInputSystemRequestBufferAdapter" $buttonAdapter "IPredictionButtonFrameSource"
    Assert-Contains "UnityInputSystemRequestBufferAdapter" $buttonAdapter "TryReadPredictionButtons"
    Assert-Contains "RollbackDiagnosticsCommandRunner" $editorCommandRunner "ROLLBACK_EDITOR_COMMAND"
    Assert-Contains "RollbackDiagnosticsCommandRunner" $editorCommandRunner "RunSoak"
    Assert-Contains "RollbackDiagnosticsCommandRunner" $editorCommandRunner "RunSynctest"
    Assert-Contains "Invoke-RollbackEditorCommand" $editorCommandScript "Library\RollbackDiagnostics"
    Assert-Contains "Invoke-RollbackEditorCommand" $editorCommandScript "ROLLBACK_EDITOR_COMMAND_ASSERT"

    Write-Output "ROLLBACK_WIRING_CHECK result=PASS scene=Sandbox f6=True f7=True f8=True hidden=True presentation=True camera=True inputButtons=True editorCommand=True logs=True"
    exit 0
}
catch {
    Write-Output "ROLLBACK_WIRING_CHECK result=FAIL reason=$($_.Exception.Message)"
    exit 1
}

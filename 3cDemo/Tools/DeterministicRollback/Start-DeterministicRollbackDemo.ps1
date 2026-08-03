[CmdletBinding()]
param(
    [switch]$StopExisting,
    [switch]$CharacterPipelineTrace,
    [int]$RunSeconds = 0,
    [string]$ProductRoot
)

$ErrorActionPreference = "Stop"
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\.."))
if ([string]::IsNullOrWhiteSpace($ProductRoot)) {
    $ProductRoot = Join-Path $repositoryRoot "3cDemo\Client\3C_Client\Build\Network\DeterministicRollback"
}
$ProductRoot = [System.IO.Path]::GetFullPath($ProductRoot)
$player = Join-Path $ProductRoot "Player\3C_Client.exe"
$server = Join-Path $ProductRoot "Server\ThirdPerson.DeterministicRollback.Server.exe"
$serverManifestPath = Join-Path $ProductRoot "Server\DeterministicRollbackServerManifest.json"
$runId = Get-Date -Format "yyyyMMdd-HHmmss"
$logDirectory = Join-Path $repositoryRoot "3cDemo\Client\3C_Client\Build\Network\RunLogs\DeterministicRollback\$runId"

foreach ($required in @($player, $server, $serverManifestPath)) {
    if (!(Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Deterministic Rollback product file does not exist: $required"
    }
}
$serverManifest = Get-Content -LiteralPath $serverManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$expectedModelIdentity = "Model:$($serverManifest.modelId)@$($serverManifest.modelVersion)/$($serverManifest.modelConfigurationHash)"

. (Join-Path $repositoryRoot "3cDemo\Tools\NetworkTest\Assert-NetworkTestProductBuild.ps1")
$manifest = Assert-NetworkTestProductBuild `
    -Root $ProductRoot `
    -RepositoryRoot $repositoryRoot `
    -ExpectedProductId "thirdperson.network-test.deterministic-rollback" `
    -ExpectedNetworkModelIdentity $expectedModelIdentity `
    -ExpectedRuntimeTopologyIdentity "thirdperson.runtime-topology.deterministic-rollback.relay-two-peers.v1" `
    -ExpectedPlayerRoleId "unity-client-player" `
    -ExpectedAdditionalArtifactRoleIds @("deterministic-relay-server") `
    -ExpectedScenes @("Assets/Scenes/GameplayLab/GameplayLab.unity") `
    -ExpectedBuildOptions "Development, StrictMode" `
    -ExpectedScriptingBackend "IL2CPP"

$relayArtifact = Get-NetworkTestProductArtifact $manifest "deterministic-relay-server"
if ($relayArtifact.kind -ne "ManagedExecutable" -or
    $relayArtifact.productId -ne "thirdperson.server-product.deterministic-rollback-relay" -or
    $relayArtifact.entryPoint -ne "Server/ThirdPerson.DeterministicRollback.Server.exe" -or
    $relayArtifact.manifestPath -ne "Server/DeterministicRollbackServerManifest.json" -or
    $relayArtifact.configurationIdentity -ne $serverManifest.manifestHash -or
    (Get-NetworkTestProductSha256 $serverManifestPath) -ne $relayArtifact.manifestHash) {
    throw "Deterministic Rollback Relay artifact does not match its Server manifest."
}

if ($StopExisting) {
    Get-CimInstance Win32_Process | Where-Object {
        ($_.Name -eq "3C_Client.exe" -and $_.CommandLine -match "--deterministic-rollback-profile=") -or
        ($_.Name -eq "ThirdPerson.DeterministicRollback.Server.exe" -and $_.CommandLine -match "DeterministicRollbackServerManifest.json")
    } | ForEach-Object {
        Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Milliseconds 500
}

$occupied = @(Get-NetUDPEndpoint -ErrorAction SilentlyContinue | Where-Object {
    $_.LocalPort -in @([int]$serverManifest.listenPort, 24101, 24102)
})
if ($occupied.Count -ne 0) {
    $owners = ($occupied | ForEach-Object { "$($_.LocalAddress):$($_.LocalPort) PID=$($_.OwningProcess)" }) -join ", "
    throw "Deterministic Rollback required UDP ports are occupied: $owners"
}

New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
$serverOutput = Join-Path $logDirectory "$runId-relay-stdout.log"
$serverError = Join-Path $logDirectory "$runId-relay-stderr.log"
$serverArguments = @(
    "--manifest", $serverManifestPath,
    "--run-id", $runId,
    "--log-directory", $logDirectory
)
$relay = Start-Process -FilePath $server -ArgumentList $serverArguments -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput $serverOutput -RedirectStandardError $serverError

$readyDeadline = (Get-Date).AddSeconds(8)
do {
    if ($relay.HasExited) {
        throw "Deterministic Rollback Relay exited during startup with code $($relay.ExitCode). Logs: $logDirectory"
    }
    $ready = @(Get-NetUDPEndpoint -ErrorAction SilentlyContinue | Where-Object {
        $_.LocalPort -eq [int]$serverManifest.listenPort -and $_.OwningProcess -eq $relay.Id
    }).Count -eq 1
    if (!$ready) {
        Start-Sleep -Milliseconds 100
    }
} while (!$ready -and (Get-Date) -lt $readyDeadline)
if (!$ready) {
    throw "Deterministic Rollback Relay UDP endpoint did not become ready. Logs: $logDirectory"
}

$common = @("-screen-fullscreen", "0", "-screen-width", "900", "-screen-height", "600")
if ($CharacterPipelineTrace) {
    $common += "--character-pipeline-trace"
}
$peerAArguments = @(
    "--deterministic-rollback-profile=peer-a",
    "-screen-position-x", "0",
    "-screen-position-y", "0",
    "-logFile", (Join-Path $logDirectory "$runId-peer-a.log")
) + $common
$peerBArguments = @(
    "--deterministic-rollback-profile=peer-b",
    "-screen-position-x", "920",
    "-screen-position-y", "0",
    "-logFile", (Join-Path $logDirectory "$runId-peer-b.log")
) + $common

$peerA = Start-Process -FilePath $player -ArgumentList $peerAArguments -WindowStyle Normal -PassThru
Start-Sleep -Milliseconds 500
$peerB = Start-Process -FilePath $player -ArgumentList $peerBArguments -WindowStyle Normal -PassThru
Start-Sleep -Seconds 5

$processes = @($relay, $peerA, $peerB)
$names = @("Dedicated Relay Server", "Peer A", "Peer B")
for ($i = 0; $i -lt $processes.Count; $i++) {
    $processes[$i].Refresh()
    if ($processes[$i].HasExited) {
        throw "$($names[$i]) exited during startup with code $($processes[$i].ExitCode). Logs: $logDirectory"
    }
}

Write-Host "Deterministic Rollback DS demo started."
Write-Host "Dedicated Relay Server PID: $($relay.Id) UDP $($serverManifest.listenPort)"
Write-Host "Peer A PID: $($peerA.Id) UDP 24101"
Write-Host "Peer B PID: $($peerB.Id) UDP 24102"
Write-Host "Logs: $logDirectory"

if ($RunSeconds -gt 0) {
    Start-Sleep -Seconds $RunSeconds
    for ($i = 0; $i -lt $processes.Count; $i++) {
        $processes[$i].Refresh()
        if ($processes[$i].HasExited) {
            throw "$($names[$i]) exited before the requested run duration completed. Logs: $logDirectory"
        }
    }
    Write-Host "Relay Server and both Rollback Peers remained alive for $RunSeconds seconds."
}

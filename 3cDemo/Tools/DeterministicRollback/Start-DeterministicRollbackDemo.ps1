[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunManifest
)

$ErrorActionPreference = "Stop"
$runManifestPath = [System.IO.Path]::GetFullPath($RunManifest)
$run = Get-Content -LiteralPath $runManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$candidateRoot = [System.IO.Path]::GetFullPath($run.candidateRoot)
$runRoot = [System.IO.Path]::GetFullPath($run.runRoot)
if ($run.schemaVersion -ne 1 -or $runRoot -ne [System.IO.Path]::GetFullPath((Split-Path $runManifestPath -Parent)) -or
    $candidateRoot -ne [System.IO.Path]::GetFullPath((Split-Path $run.candidateManifestPath -Parent))) {
    throw "Rollback Run manifest path identity is invalid."
}

function Require-Endpoint([string]$Key) {
    $matches = @($run.endpoints | Where-Object { $_.key -eq $Key })
    if ($matches.Count -ne 1 -or $matches[0].address -ne '127.0.0.1' -or $matches[0].port -le 0) {
        throw "Rollback Run endpoint '$Key' is missing or invalid."
    }
    return $matches[0]
}

function Require-Window([string]$RoleId) {
    $matches = @($run.windows | Where-Object { $_.roleId -eq $RoleId })
    if ($matches.Count -ne 1 -or $matches[0].width -le 0 -or $matches[0].height -le 0) {
        throw "Rollback Run window '$RoleId' is missing or invalid."
    }
    return $matches[0]
}

function Require-Tool([string]$ToolId) {
    $matches = @($run.toolBundles | Where-Object { $_.toolId -eq $ToolId })
    if ($matches.Count -ne 1) { throw "Rollback Run tool '$ToolId' is missing." }
    return $matches[0]
}

function Wait-Udp([System.Diagnostics.Process]$Process, [int]$Port) {
    $deadline = (Get-Date).AddSeconds(8)
    do {
        $Process.Refresh()
        if ($Process.HasExited) { throw "Process $($Process.Id) exited during UDP startup." }
        $ready = @(Get-NetUDPEndpoint -ErrorAction SilentlyContinue | Where-Object {
            $_.LocalPort -eq $Port -and $_.OwningProcess -eq $Process.Id
        }).Count -eq 1
        if (!$ready) { Start-Sleep -Milliseconds 100 }
    } while (!$ready -and (Get-Date) -lt $deadline)
    if (!$ready) { throw "Process $($Process.Id) did not bind UDP $Port." }
}

function Wait-HttpIdentity(
    [System.Diagnostics.Process]$Process,
    [string]$Uri,
    [string]$Token,
    [string]$CandidateId,
    [string]$RunId,
    [string]$SessionId) {
    $deadline = (Get-Date).AddSeconds(8)
    $headers = @{ Authorization = "Bearer $Token" }
    do {
        $Process.Refresh()
        if ($Process.HasExited) { throw "Process $($Process.Id) exited during HTTP startup." }
        try {
            $identity = Invoke-RestMethod -Method Get -Uri $Uri -Headers $headers -TimeoutSec 1
            $ready = $identity.candidateId -eq $CandidateId -and $identity.runId -eq $RunId -and
                $identity.sessionId -eq $SessionId
        } catch {
            $ready = $false
        }
        if (!$ready) { Start-Sleep -Milliseconds 100 }
    } while (!$ready -and (Get-Date) -lt $deadline)
    if (!$ready) { throw "Process $($Process.Id) HTTP identity did not become ready." }
}

$relayEndpoint = Require-Endpoint 'rollback-relay'
$peerAEndpoint = Require-Endpoint 'rollback-peer-a'
$peerBEndpoint = Require-Endpoint 'rollback-peer-b'
$gmEndpoint = Require-Endpoint 'rollback-gm'
$queryEndpoint = Require-Endpoint 'rollback-relay-query'
$peerAWindow = Require-Window 'peer-a'
$peerBWindow = Require-Window 'peer-b'
$gmTool = Require-Tool 'thirdperson.rollback-gm'

$player = Join-Path $candidateRoot 'Player\3C_Client.exe'
$relay = Join-Path $candidateRoot 'Server\ThirdPerson.DeterministicRollback.Server.exe'
$relayCandidate = Join-Path $candidateRoot 'Server\DeterministicRollbackCandidateManifest.json'
$gm = Join-Path $candidateRoot 'Gm\ThirdPerson.Development.Gm.Service.exe'
$gmToolManifest = Join-Path $candidateRoot 'Gm\GmToolManifest.json'
$gmToolPolicy = Join-Path $candidateRoot 'Gm\GmToolPolicy.json'
$configRoot = Join-Path $runRoot 'Config'
$logs = Join-Path $runRoot 'Logs'
$processManifestPath = Join-Path $runRoot 'Processes.json'
New-Item -ItemType Directory -Force -Path $configRoot, $logs | Out-Null
foreach ($path in @($player, $relay, $relayCandidate, $gm, $gmToolManifest, $gmToolPolicy)) {
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) { throw "Rollback Candidate file is missing: $path" }
}

$relayRequestPath = Join-Path $configRoot 'RelayRunRequest.json'
$relayRunManifestPath = Join-Path $configRoot 'DeterministicRollbackServerRunManifest.json'
[ordered]@{
    schemaVersion = 1
    runId = $run.runId
    sessionId = $run.sessionId
    candidateManifestHash = $run.candidateManifestHash
    listenAddress = $relayEndpoint.address
    listenPort = [int]$relayEndpoint.port
} | ConvertTo-Json | Set-Content -LiteralPath $relayRequestPath -Encoding UTF8
& $relay --write-run-manifest $relayCandidate $relayRequestPath $relayRunManifestPath
if ($LASTEXITCODE -ne 0) { throw "Rollback Relay Run Manifest generation failed with exit code $LASTEXITCODE." }

$gmRequestPath = Join-Path $configRoot 'GmRunRequest.json'
[ordered]@{
    schemaVersion = 1
    candidateId = $run.candidateId
    runId = $run.runId
    sessionId = $run.sessionId
    slotId = $run.slotId
    gmAddress = $gmEndpoint.address
    gmPort = [int]$gmEndpoint.port
    relayQueryAddress = $queryEndpoint.address
    relayQueryPort = [int]$queryEndpoint.port
    toolBundleHash = $gmTool.bundleHash
} | ConvertTo-Json | Set-Content -LiteralPath $gmRequestPath -Encoding UTF8
& $gm --write-run-manifests $gmToolManifest $gmToolPolicy $gmRequestPath $configRoot
if ($LASTEXITCODE -ne 0) { throw "Rollback GM Run Manifest generation failed with exit code $LASTEXITCODE." }

$gmServerManifest = Join-Path $configRoot 'GmServerRunManifest.json'
$gmConsoleManifest = Join-Path $configRoot 'GmConsoleRunManifest.json'
$relayQueryManifest = Join-Path $configRoot 'RelayQueryRunManifest.json'
$gmServerConfig = Get-Content -LiteralPath $gmServerManifest -Raw -Encoding UTF8 | ConvertFrom-Json
$gmConsoleConfig = Get-Content -LiteralPath $gmConsoleManifest -Raw -Encoding UTF8 | ConvertFrom-Json
$relayQueryConfig = Get-Content -LiteralPath $relayQueryManifest -Raw -Encoding UTF8 | ConvertFrom-Json

function Write-PeerManifest(
    [string]$Path,
    [string]$ProfileId,
    $LocalEndpoint) {
    [ordered]@{
        schemaVersion = 1
        candidateId = $run.candidateId
        runId = $run.runId
        sessionId = $run.sessionId
        profileId = $ProfileId
        relayAddress = $relayEndpoint.address
        relayPort = [int]$relayEndpoint.port
        localAddress = $LocalEndpoint.address
        localPort = [int]$LocalEndpoint.port
    } | ConvertTo-Json | Set-Content -LiteralPath $Path -Encoding UTF8
}

$peerAManifest = Join-Path $configRoot 'PeerA.json'
$peerBManifest = Join-Path $configRoot 'PeerB.json'
Write-PeerManifest $peerAManifest 'peer-a' $peerAEndpoint
Write-PeerManifest $peerBManifest 'peer-b' $peerBEndpoint

$started = [System.Collections.Generic.List[object]]::new()
try {
    $relayStdout = Join-Path $logs 'relay.stdout.log'
    $relayStderr = Join-Path $logs 'relay.stderr.log'
    $relayProcess = Start-Process -FilePath $relay -ArgumentList @(
        '--manifest', $relayRunManifestPath,
        '--query-manifest', $relayQueryManifest,
        '--run-id', $run.runId,
        '--log-directory', $logs
    ) -WindowStyle Hidden -PassThru -RedirectStandardOutput $relayStdout -RedirectStandardError $relayStderr
    $started.Add([pscustomobject]@{ roleId = 'relay'; process = $relayProcess })
    Wait-Udp $relayProcess ([int]$relayEndpoint.port)
    Wait-HttpIdentity $relayProcess "http://$($queryEndpoint.address):$($queryEndpoint.port)/v1/identity" `
        $relayQueryConfig.http.accessToken $run.candidateId $run.runId $run.sessionId

    $gmProcess = $null
    try {
        $gmProcess = Start-Process -FilePath $gm -ArgumentList @(
            '--manifest', $gmServerManifest,
            '--console-manifest', $gmConsoleManifest,
            '--run-id', $run.runId,
            '--log-directory', $logs
        ) -WindowStyle Normal -PassThru
        Wait-HttpIdentity $gmProcess "http://$($gmEndpoint.address):$($gmEndpoint.port)/v1/service" `
            $gmConsoleConfig.accessToken $run.candidateId $run.runId $run.sessionId
        $started.Add([pscustomobject]@{ roleId = 'gm'; process = $gmProcess })
    } catch {
        if ($null -ne $gmProcess) {
            $gmProcess.Refresh()
            if (!$gmProcess.HasExited) { Stop-Process -Id $gmProcess.Id -Force -ErrorAction SilentlyContinue }
        }
        $_ | Out-String | Set-Content -LiteralPath (Join-Path $logs 'gm-startup-failure.log') -Encoding UTF8
    }

    $common = @('-screen-fullscreen', '0')
    $peerAArguments = @(
        '--deterministic-rollback-profile=peer-a',
        "--deterministic-rollback-run-manifest=$peerAManifest",
        '-screen-position-x', $peerAWindow.x,
        '-screen-position-y', $peerAWindow.y,
        '-screen-width', $peerAWindow.width,
        '-screen-height', $peerAWindow.height,
        '-logFile', (Join-Path $logs 'peer-a.log')
    ) + $common
    $peerAProcess = Start-Process -FilePath $player -ArgumentList $peerAArguments -WindowStyle Normal -PassThru
    $started.Add([pscustomobject]@{ roleId = 'peer-a'; process = $peerAProcess })
    Start-Sleep -Milliseconds 500
    $peerBArguments = @(
        '--deterministic-rollback-profile=peer-b',
        "--deterministic-rollback-run-manifest=$peerBManifest",
        '-screen-position-x', $peerBWindow.x,
        '-screen-position-y', $peerBWindow.y,
        '-screen-width', $peerBWindow.width,
        '-screen-height', $peerBWindow.height,
        '-logFile', (Join-Path $logs 'peer-b.log')
    ) + $common
    $peerBProcess = Start-Process -FilePath $player -ArgumentList $peerBArguments -WindowStyle Normal -PassThru
    $started.Add([pscustomobject]@{ roleId = 'peer-b'; process = $peerBProcess })
    Start-Sleep -Seconds 5
    foreach ($item in $started) {
        $item.process.Refresh()
        if ($item.process.HasExited) { throw "Rollback role '$($item.roleId)' exited during startup." }
    }
    @($started | ForEach-Object {
        [ordered]@{
            roleId = $_.roleId
            processId = $_.process.Id
            processStartTimeUtcTicks = $_.process.StartTime.ToUniversalTime().Ticks
        }
    }) | ConvertTo-Json | Set-Content -LiteralPath $processManifestPath -Encoding UTF8
} catch {
    foreach ($item in $started) {
        $item.process.Refresh()
        if (!$item.process.HasExited) { Stop-Process -Id $item.process.Id -Force -ErrorAction SilentlyContinue }
    }
    throw
}

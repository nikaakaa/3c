[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RunManifest
)

$ErrorActionPreference = "Stop"
$runManifestPath = [System.IO.Path]::GetFullPath($RunManifest)
$run = Get-Content -LiteralPath $runManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$clientBuildRoot = [System.IO.Path]::GetFullPath($run.candidateRoot)
$logs = Join-Path ([System.IO.Path]::GetFullPath($run.runRoot)) "Logs"
if ($run.schemaVersion -ne 1 -or $run.slotId -ne 'default' -or
    $clientBuildRoot -ne [System.IO.Path]::GetFullPath((Split-Path $run.candidateManifestPath -Parent))) {
    throw "Unity Authority Run manifest identity is invalid."
}
$serverBuildRoot = Join-Path $clientBuildRoot "Server"
$manifestPath = Join-Path $clientBuildRoot "NetworkTestProduct.json"
$client = Join-Path $clientBuildRoot "Player\3C_Client.exe"
$clientRuntime = Join-Path $clientBuildRoot "Player\GameAssembly.dll"
$clientContent = Join-Path $clientBuildRoot "Player\3C_Client_Data\globalgamemanagers"
$server = Join-Path $serverBuildRoot "ThirdPerson.UnityAuthority.Server.exe"
$serverConfig = Join-Path $serverBuildRoot "Fantasy.config"
$serverProductManifest = Join-Path $serverBuildRoot "ServerProductBuild.json"
$runId = $run.runId
$authorityOutput = Join-Path $logs "authority-output.log"
$authorityError = Join-Path $logs "authority-error.log"
$clientAOutput = Join-Path $logs "client-a-output.log"
$clientAError = Join-Path $logs "client-a-error.log"
$clientBOutput = Join-Path $logs "client-b-output.log"
$clientBError = Join-Path $logs "client-b-error.log"
$serverLogRoot = Join-Path $logs "server"
$serverOutput = Join-Path $logs "server-output.log"
$serverError = Join-Path $logs "server-error.log"

trap {
    $failure = $_ | Out-String
    [Console]::Error.Write($failure)
    exit 1
}

function Assert-Exists([string]$path, [string]$label) {
    if (!(Test-Path -LiteralPath $path)) {
        throw "$label does not exist: $path"
    }
}

function Assert-PortsAvailable {
    $occupied = @(Get-NetUDPEndpoint -ErrorAction SilentlyContinue | Where-Object {
        $_.LocalPort -eq $controlPort -or $_.LocalPort -eq $authorityDataPort
    })
    if ($occupied.Count -gt 0) {
        $owners = ($occupied | ForEach-Object { "$($_.LocalAddress):$($_.LocalPort) PID=$($_.OwningProcess)" }) -join ", "
        throw "Unity Authority required ports are occupied: $owners"
    }
}

function Get-WindowArguments([string]$RoleId) {
    $matches = @($run.windows | Where-Object { $_.roleId -eq $RoleId })
    if ($matches.Count -ne 1 -or $matches[0].width -le 0 -or $matches[0].height -le 0) {
        throw "Unity Authority Run window '$RoleId' is missing or invalid."
    }
    $window = $matches[0]
    return @(
        "-screen-fullscreen", "0",
        "-screen-position-x", $window.x,
        "-screen-position-y", $window.y,
        "-screen-width", $window.width,
        "-screen-height", $window.height)
}

. (Join-Path $PSScriptRoot "Assert-NetworkTestProductBuild.ps1")
$manifest = Assert-NetworkTestProductBuild `
    -Root $clientBuildRoot `
    -ExpectedProductId "thirdperson.network-test.unity-authority" `
    -ExpectedNetworkModelIdentity "thirdperson.network-model.server-authoritative-hybrid" `
    -ExpectedRuntimeTopologyIdentity "thirdperson.runtime-topology.unity-authority.four-process.v1" `
    -ExpectedPlayerRoleId "unity-player" `
    -ExpectedAdditionalArtifactRoleIds @("unity-authority-gate-server") `
    -ExpectedScenes @(
        "Assets/Scenes/ServerAuthoritative/ServerAuthoritativeNetworkTestBootstrap.unity",
        "Assets/Scenes/ServerAuthoritative/ServerAuthoritativeClient.unity",
        "Assets/Scenes/ServerAuthoritative/ServerAuthoritativeAuthorityWorker.unity") `
    -ExpectedBuildOptions "Development, StrictMode" `
    -ExpectedScriptingBackend "Mono2x"
Assert-Exists $client "Unity Authority Player"
Assert-Exists $clientRuntime "Unity Authority Player runtime"
Assert-Exists $clientContent "Unity Authority Player content"
Assert-Exists $server "Unity Authority Fantasy Server"
Assert-Exists $serverConfig "Unity Authority Fantasy.config"
Assert-Exists $serverProductManifest "Unity Authority server product manifest"
$serverArtifact = Get-NetworkTestProductArtifact $manifest "unity-authority-gate-server"
if ($serverArtifact.configurationIdentity -ne "Debug" -or
    $serverArtifact.productId -ne "thirdperson.server-product.unity-authority" -or
    $serverArtifact.manifestPath -ne "Server/ServerProductBuild.json" -or
    (Get-NetworkTestProductSha256 $serverProductManifest) -ne $serverArtifact.manifestHash) {
    throw "Unity Authority network manifest does not match its server product manifest."
}
. (Join-Path $PSScriptRoot "Assert-ServerProductBuild.ps1")
$null = Assert-ServerProductBuild `
    -Root $serverBuildRoot `
    -ManifestPath $serverProductManifest `
    -ExpectedProductId "thirdperson.server-product.unity-authority" `
    -ExpectedHostProductId "thirdperson.authority-product.unity-worker.v1" `
    -ExpectedHostRouteKind "ExternalAuthorityWorker" `
    -ExpectedLaunchKind "unity-authority-worker" `
    -ExpectedHostManifestSchemaVersion 1 `
    -ExpectedAuthoritySolverId "Unity.CharacterController.WorldSolver" `
    -ExpectedAuthoritySolverVersion "1" `
    -ExpectedAuthoritySolverCapabilities 15 `
    -ExpectedAuthoritySolverFeatures 15 `
    -ExpectedExecutable "ThirdPerson.UnityAuthority.Server.exe" `
    -ExpectedScenes @("Gate") `
    -ExpectedEntityModules @("thirdperson.server.gate.entity", "thirdperson.server.unity-authority.entity") `
    -ExpectedHotfixModules @("thirdperson.server.gate.hotfix", "thirdperson.server.unity-authority.hotfix") `
    -ForbiddenModuleIds @("thirdperson.server.dotrecast-authority.entity", "thirdperson.server.dotrecast-authority.hotfix", "ThirdPerson.Server.DotRecastAuthority.Entity", "ThirdPerson.Server.DotRecastAuthority.Hotfix", "ThirdPersonSimulation.DotRecast", "ThirdPersonSimulation.DotRecastAuthority")
$controlPort = [int](Get-NetworkTestProductField $manifest "controlPort")
$authorityDataPort = [int](Get-NetworkTestProductField $manifest "authorityDataPort")
if ($controlPort -le 0 -or $controlPort -gt 65535 -or
    $authorityDataPort -le 0 -or $authorityDataPort -gt 65535 -or
    $controlPort -eq $authorityDataPort) {
    throw "Unity Authority build manifest contains invalid control/data ports."
}

Assert-PortsAvailable

New-Item -ItemType Directory -Force -Path $logs | Out-Null
New-Item -ItemType Directory -Force -Path $serverLogRoot | Out-Null
$authorityArguments = @(
    "--network-test-scenario=unity-authority-worker",
    "--server-authoritative-role=authority",
    "-logFile", (Join-Path $logs "authority.log")
) + (Get-WindowArguments "authority")
$clientAArguments = @(
    "--network-test-scenario=server-authoritative-client",
    "--server-authoritative-role=client-a",
    "-logFile", (Join-Path $logs "client-a.log")
) + (Get-WindowArguments "client-a")
$clientBArguments = @(
    "--network-test-scenario=server-authoritative-client",
    "--server-authoritative-role=client-b",
    "-logFile", (Join-Path $logs "client-b.log")
) + (Get-WindowArguments "client-b")

$started = [System.Collections.Generic.List[object]]::new()
try {
    $serverLogRootEnvironmentVariable = "THIRDPERSON_SERVER_LOG_ROOT"
    $previousServerLogRoot = [Environment]::GetEnvironmentVariable($serverLogRootEnvironmentVariable, [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable($serverLogRootEnvironmentVariable, $serverLogRoot, [EnvironmentVariableTarget]::Process)
    try {
        $serverProcess = Start-Process -FilePath $server -WorkingDirectory $serverBuildRoot -ArgumentList @("--m", "Develop") -RedirectStandardOutput $serverOutput -RedirectStandardError $serverError -PassThru
    }
    finally {
        [Environment]::SetEnvironmentVariable($serverLogRootEnvironmentVariable, $previousServerLogRoot, [EnvironmentVariableTarget]::Process)
    }
    $started.Add([pscustomobject]@{ RoleId = "fantasy-server"; Label = "Fantasy Server"; Path = $server; Process = $serverProcess })
    Start-Sleep -Seconds 3

    $authorityProcess = Start-Process -FilePath $client -ArgumentList $authorityArguments -RedirectStandardOutput $authorityOutput -RedirectStandardError $authorityError -PassThru
    $started.Add([pscustomobject]@{ RoleId = "authority"; Label = "Unity Authority Worker"; Path = $client; Process = $authorityProcess })
    Start-Sleep -Seconds 2

    $clientAProcess = Start-Process -FilePath $client -ArgumentList $clientAArguments -RedirectStandardOutput $clientAOutput -RedirectStandardError $clientAError -PassThru
    $started.Add([pscustomobject]@{ RoleId = "client-a"; Label = "Client A"; Path = $client; Process = $clientAProcess })
    Start-Sleep -Seconds 1

    $clientBProcess = Start-Process -FilePath $client -ArgumentList $clientBArguments -RedirectStandardOutput $clientBOutput -RedirectStandardError $clientBError -PassThru
    $started.Add([pscustomobject]@{ RoleId = "client-b"; Label = "Client B"; Path = $client; Process = $clientBProcess })
    $readyDeadline = (Get-Date).AddSeconds(30)
    $allEndpointsReady = $false
    do {
        foreach ($record in $started) {
            $trackedProcess = $record.Process
            $trackedProcess.Refresh()
            if ($trackedProcess.HasExited) {
                $trackedProcess.WaitForExit()
                $exitCode = "unavailable"
                try {
                    $exitCode = $trackedProcess.ExitCode
                }
                catch {
                }
                throw "Unity Authority test process exited early: Role=$($record.Label), Path=$($record.Path), PID=$($trackedProcess.Id), ExitCode=$exitCode"
            }
        }
        $udpEndpoints = @(Get-NetUDPEndpoint)
        $authorityUdp = @($udpEndpoints | Where-Object { $_.OwningProcess -eq $authorityProcess.Id })
        $clientAUdp = @($udpEndpoints | Where-Object { $_.OwningProcess -eq $clientAProcess.Id })
        $clientBUdp = @($udpEndpoints | Where-Object { $_.OwningProcess -eq $clientBProcess.Id })
        $authorityDataReady = @($authorityUdp | Where-Object { $_.LocalPort -eq $authorityDataPort }).Count -eq 1
        $allEndpointsReady = $authorityDataReady -and $authorityUdp.Count -ge 2 -and $clientAUdp.Count -ge 2 -and $clientBUdp.Count -ge 2
        if ($allEndpointsReady) {
            break
        }
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $readyDeadline)

    if (!$allEndpointsReady) {
        throw "Unity Authority endpoint handshake did not complete before the 30 second deadline: Authority=$($authorityUdp.Count), AuthorityData=$authorityDataReady, ClientA=$($clientAUdp.Count), ClientB=$($clientBUdp.Count)"
    }

    $result = @(
        "CandidateId: $($manifest.candidateId)",
        "Fantasy Server PID: $($serverProcess.Id)",
        "Authority PID: $($authorityProcess.Id)",
        "Client A PID: $($clientAProcess.Id)",
        "Client B PID: $($clientBProcess.Id)",
        "Fantasy control port: $controlPort",
        "Authority gameplay data port: $authorityDataPort",
        "Log directory: $logs"
    ) -join [Environment]::NewLine
    @($started | ForEach-Object {
        [ordered]@{
            roleId = $_.RoleId
            processId = $_.Process.Id
            processStartTimeUtcTicks = $_.Process.StartTime.ToUniversalTime().Ticks
        }
    }) | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run.runRoot 'Processes.json') -Encoding UTF8
    Write-Output $result
}
catch {
    foreach ($record in $started) {
        $trackedProcess = $record.Process
        if ($null -ne $trackedProcess -and !$trackedProcess.HasExited) {
            Stop-Process -Id $trackedProcess.Id -Force -ErrorAction SilentlyContinue
        }
    }
    throw
}

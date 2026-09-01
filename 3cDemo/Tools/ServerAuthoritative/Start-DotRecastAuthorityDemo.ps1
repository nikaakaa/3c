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
    throw "DotRecast Authority Run manifest identity is invalid."
}
$serverBuildRoot = Join-Path $clientBuildRoot "Server"
$manifestPath = Join-Path $clientBuildRoot "NetworkTestProduct.json"
$client = Join-Path $clientBuildRoot "Player\3C_Client.exe"
$clientRuntime = Join-Path $clientBuildRoot "Player\GameAssembly.dll"
$clientContent = Join-Path $clientBuildRoot "Player\3C_Client_Data\globalgamemanagers"
$server = Join-Path $serverBuildRoot "ThirdPerson.DotRecastAuthority.Server.exe"
$serverConfig = Join-Path $serverBuildRoot "Fantasy.config"
$serverProductManifest = Join-Path $serverBuildRoot "ServerProductBuild.json"
$authorityManifest = Join-Path $serverBuildRoot "Authority\DotRecastAuthorityScene.manifest"
$authorityProgram = Join-Path $serverBuildRoot "Authority\Artifacts\CharacterProgram.csim"
$authorityNavigation = Join-Path $serverBuildRoot "Authority\Artifacts\NavigationSurface.navsurface"
$runId = $run.runId
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
        throw "DotRecast Authority required ports are occupied: $owners"
    }
}

function Get-WindowArguments([string]$RoleId) {
    $matches = @($run.windows | Where-Object { $_.roleId -eq $RoleId })
    if ($matches.Count -ne 1 -or $matches[0].width -le 0 -or $matches[0].height -le 0) {
        throw "DotRecast Authority Run window '$RoleId' is missing or invalid."
    }
    $window = $matches[0]
    return @(
        "-screen-fullscreen", "0",
        "-screen-position-x", $window.x,
        "-screen-position-y", $window.y,
        "-screen-width", $window.width,
        "-screen-height", $window.height)
}

function Get-RunFailureSummary {
    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($path in @(
        $serverOutput,
        $serverError,
        (Join-Path $logs "client-a.log"),
        $clientAOutput,
        $clientAError,
        (Join-Path $logs "client-b.log"),
        $clientBOutput,
        $clientBError)) {
        if (!(Test-Path -LiteralPath $path)) {
            continue
        }
        $tail = @(Get-Content -LiteralPath $path -Encoding UTF8 -Tail 12 | Where-Object {
            ![string]::IsNullOrWhiteSpace($_)
        })
        foreach ($line in $tail) {
            $lines.Add("$([System.IO.Path]::GetFileName($path)): $line")
        }
    }
    return $lines -join [Environment]::NewLine
}

. (Join-Path $PSScriptRoot "Assert-NetworkTestProductBuild.ps1")
$manifest = Assert-NetworkTestProductBuild `
    -Root $clientBuildRoot `
    -ExpectedProductId "thirdperson.network-test.dotrecast-authority" `
    -ExpectedNetworkModelIdentity "thirdperson.network-model.server-authoritative-hybrid" `
    -ExpectedRuntimeTopologyIdentity "thirdperson.runtime-topology.dotrecast-authority.three-process.v1" `
    -ExpectedPlayerRoleId "unity-client-player" `
    -ExpectedAdditionalArtifactRoleIds @("dotrecast-authority-server") `
    -ExpectedScenes @(
        "Assets/Scenes/ServerAuthoritative/ServerAuthoritativeNetworkTestBootstrap.unity",
        "Assets/Scenes/ServerAuthoritative/DotRecastAuthorityClient.unity") `
    -ExpectedBuildOptions "Development, StrictMode" `
    -ExpectedScriptingBackend "Mono2x"
Assert-Exists $client "DotRecast Authority Player"
Assert-Exists $clientRuntime "DotRecast Authority Player runtime"
Assert-Exists $clientContent "DotRecast Authority Player content"
Assert-Exists $server "DotRecast Authority Fantasy Server"
Assert-Exists $serverConfig "DotRecast Authority Fantasy.config"
Assert-Exists $serverProductManifest "DotRecast Authority server product manifest"
Assert-Exists $authorityManifest "DotRecast Authority Scene manifest"
Assert-Exists $authorityProgram "DotRecast Authority Program artifact"
Assert-Exists $authorityNavigation "DotRecast Authority Navigation artifact"
$serverArtifact = Get-NetworkTestProductArtifact $manifest "dotrecast-authority-server"
if ($serverArtifact.configurationIdentity -ne "Debug" -or
    $serverArtifact.productId -ne "thirdperson.server-product.dotrecast-authority" -or
    $serverArtifact.manifestPath -ne "Server/ServerProductBuild.json" -or
    (Get-NetworkTestProductSha256 $serverProductManifest) -ne $serverArtifact.manifestHash -or
    (Get-NetworkTestProductField $serverArtifact "authorityManifestFileHash") -ne
        (Get-NetworkTestProductSha256 $authorityManifest)) {
    throw "DotRecast Authority network manifest does not match its server or authority manifest."
}
. (Join-Path $PSScriptRoot "Assert-ServerProductBuild.ps1")
$null = Assert-ServerProductBuild `
    -Root $serverBuildRoot `
    -ManifestPath $serverProductManifest `
    -ExpectedProductId "thirdperson.server-product.dotrecast-authority" `
    -ExpectedHostProductId "thirdperson.authority-product.dotrecast-scene.v1" `
    -ExpectedHostRouteKind "InProcessAuthorityScene" `
    -ExpectedLaunchKind "dotrecast-authority-scene" `
    -ExpectedHostManifestSchemaVersion 1 `
    -ExpectedAuthoritySolverId "DotRecast.NavigationSurface.WorldSolver" `
    -ExpectedAuthoritySolverVersion "3" `
    -ExpectedAuthoritySolverCapabilities 15 `
    -ExpectedAuthoritySolverFeatures 231 `
    -ExpectedExecutable "ThirdPerson.DotRecastAuthority.Server.exe" `
    -ExpectedScenes @("Gate", "DotRecastAuthority") `
    -ExpectedEntityModules @("thirdperson.server.gate.entity", "thirdperson.server.dotrecast-authority.entity") `
    -ExpectedHotfixModules @("thirdperson.server.gate.hotfix", "thirdperson.server.dotrecast-authority.hotfix") `
    -RequiredPortableDependencies @("ThirdPersonSimulation.DotRecast", "ThirdPersonSimulation.DotRecastAuthority", "ThirdPersonSimulation.ServerAuthoritative", "ThirdPersonSimulation.ServerAuthoritative.Transport") `
    -ExpectedAuthorityArtifacts @("thirdperson.authority.manifest", "thirdperson.authority.program", "thirdperson.authority.navigation") `
    -ForbiddenModuleIds @("thirdperson.server.unity-authority.entity", "thirdperson.server.unity-authority.hotfix", "ThirdPerson.Server.UnityAuthority.Entity", "ThirdPerson.Server.UnityAuthority.Hotfix")
$controlPort = [int](Get-NetworkTestProductField $manifest "controlPort")
$authorityDataPort = [int](Get-NetworkTestProductField $manifest "authorityDataPort")
if ($controlPort -le 0 -or $controlPort -gt 65535 -or
    $authorityDataPort -le 0 -or $authorityDataPort -gt 65535 -or
    $controlPort -eq $authorityDataPort) {
    throw "DotRecast Authority build manifest contains invalid control/data ports."
}

Assert-PortsAvailable

New-Item -ItemType Directory -Force -Path $logs | Out-Null
New-Item -ItemType Directory -Force -Path $serverLogRoot | Out-Null
$serverArguments = @(
    "--m", "Develop"
)
$clientAArguments = @(
    "--network-test-scenario=dotrecast-authority-client",
    "--server-authoritative-role=client-a",
    "--server-authoritative-player-id=corin-player-a",
    "--server-authoritative-actor-id=corin-actor-a",
    "-logFile", (Join-Path $logs "client-a.log")
) + (Get-WindowArguments "client-a")
$clientBArguments = @(
    "--network-test-scenario=dotrecast-authority-client",
    "--server-authoritative-role=client-b",
    "--server-authoritative-player-id=corin-player-b",
    "--server-authoritative-actor-id=corin-actor-b",
    "-logFile", (Join-Path $logs "client-b.log")
) + (Get-WindowArguments "client-b")

$started = [System.Collections.Generic.List[object]]::new()
try {
    $serverRootEnvironmentVariable = "THIRDPERSON_DOTRECAST_AUTHORITY_SERVER_ROOT"
    $serverLogRootEnvironmentVariable = "THIRDPERSON_SERVER_LOG_ROOT"
    $previousServerRoot = [Environment]::GetEnvironmentVariable($serverRootEnvironmentVariable, [EnvironmentVariableTarget]::Process)
    $previousServerLogRoot = [Environment]::GetEnvironmentVariable($serverLogRootEnvironmentVariable, [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable($serverRootEnvironmentVariable, $serverBuildRoot, [EnvironmentVariableTarget]::Process)
    [Environment]::SetEnvironmentVariable($serverLogRootEnvironmentVariable, $serverLogRoot, [EnvironmentVariableTarget]::Process)
    try {
        $serverProcess = Start-Process -FilePath $server -WorkingDirectory $serverBuildRoot -ArgumentList $serverArguments -RedirectStandardOutput $serverOutput -RedirectStandardError $serverError -PassThru
    }
    finally {
        [Environment]::SetEnvironmentVariable($serverRootEnvironmentVariable, $previousServerRoot, [EnvironmentVariableTarget]::Process)
        [Environment]::SetEnvironmentVariable($serverLogRootEnvironmentVariable, $previousServerLogRoot, [EnvironmentVariableTarget]::Process)
    }
    $started.Add([pscustomobject]@{ RoleId = "fantasy-server"; Label = "Fantasy Server"; Path = $server; Process = $serverProcess })
    Start-Sleep -Seconds 3

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
                Start-Sleep -Milliseconds 250
                $exitCode = "unavailable"
                try {
                    $exitCode = $trackedProcess.ExitCode
                }
                catch {
                }
                $failure = Get-RunFailureSummary
                throw "DotRecast Authority test process exited early: Role=$($record.Label), Path=$($record.Path), PID=$($trackedProcess.Id), ExitCode=$exitCode`n$failure"
            }
        }
        $udpEndpoints = @(Get-NetUDPEndpoint)
        $serverUdp = @($udpEndpoints | Where-Object { $_.OwningProcess -eq $serverProcess.Id })
        $clientAUdp = @($udpEndpoints | Where-Object { $_.OwningProcess -eq $clientAProcess.Id })
        $clientBUdp = @($udpEndpoints | Where-Object { $_.OwningProcess -eq $clientBProcess.Id })
        $serverControlReady = @($serverUdp | Where-Object { $_.LocalPort -eq $controlPort }).Count -eq 1
        $serverDataReady = @($serverUdp | Where-Object { $_.LocalPort -eq $authorityDataPort }).Count -eq 1
        $allEndpointsReady = $serverControlReady -and $serverDataReady -and $clientAUdp.Count -ge 2 -and $clientBUdp.Count -ge 2
        if ($allEndpointsReady) {
            break
        }
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $readyDeadline)

    if (!$allEndpointsReady) {
        $failure = Get-RunFailureSummary
        throw "DotRecast Authority endpoint handshake did not complete before the 30 second deadline: ServerControl=$serverControlReady, ServerData=$serverDataReady, ClientA=$($clientAUdp.Count), ClientB=$($clientBUdp.Count)`n$failure"
    }

    $result = @(
        "CandidateId: $($manifest.candidateId)",
        "Fantasy Server PID: $($serverProcess.Id)",
        "Client A PID: $($clientAProcess.Id)",
        "Client B PID: $($clientBProcess.Id)",
        "Fantasy control port: $controlPort",
        "Authority gameplay data port: $authorityDataPort",
        "Authority manifest hash: $($manifest.authorityManifestHash)",
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

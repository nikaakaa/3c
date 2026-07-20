[CmdletBinding()]
param(
    [switch]$StopExisting,
    [string]$ResultPath,
    [string]$ProductRoot
)

$ErrorActionPreference = "Stop"
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\.."))
if ([string]::IsNullOrWhiteSpace($ProductRoot)) {
    $ProductRoot = Join-Path $root "3cDemo\Client\3C_Client\Build\Network\DotRecastAuthority"
}
$clientBuildRoot = [System.IO.Path]::GetFullPath($ProductRoot)
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
$runId = Get-Date -Format "yyyyMMdd-HHmmss"
$logs = Join-Path $root "3cDemo\Client\3C_Client\Build\Network\RunLogs\DotRecastAuthority\$runId"
$clientAOutput = Join-Path $logs "client-a-output.log"
$clientAError = Join-Path $logs "client-a-error.log"
$clientBOutput = Join-Path $logs "client-b-output.log"
$clientBError = Join-Path $logs "client-b-error.log"
$serverLogRoot = Join-Path $logs "server"
$serverOutput = Join-Path $logs "server-output.log"
$serverError = Join-Path $logs "server-error.log"

function Write-LauncherResult([string]$value) {
    if (![string]::IsNullOrWhiteSpace($ResultPath)) {
        [System.IO.File]::WriteAllText(
            [System.IO.Path]::GetFullPath($ResultPath),
            $value,
            [System.Text.UTF8Encoding]::new($false))
    }
}

trap {
    $failure = $_ | Out-String
    Write-LauncherResult $failure
    [Console]::Error.Write($failure)
    exit 1
}

function Assert-Exists([string]$path, [string]$label) {
    if (!(Test-Path -LiteralPath $path)) {
        throw "$label does not exist: $path"
    }
}

function Get-OwnedProcess {
    $dotRecastServerRoot = Join-Path $root "3cDemo\Client\3C_Client\Build\Network\DotRecastAuthority\Server"
    $unityServerRoot = Join-Path $root "3cDemo\Client\3C_Client\Build\Network\UnityAuthority\Server"
    Get-CimInstance Win32_Process | Where-Object {
        ($_.Name -eq "3C_Client.exe" -and $_.CommandLine -match "--network-test-scenario=") -or
        (($_.Name -eq "ThirdPerson.UnityAuthority.Server.exe" -or $_.Name -eq "ThirdPerson.DotRecastAuthority.Server.exe") -and $_.ExecutablePath -and
            ($_.ExecutablePath.StartsWith($dotRecastServerRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
             $_.ExecutablePath.StartsWith($unityServerRoot, [System.StringComparison]::OrdinalIgnoreCase)))
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

. (Join-Path $root "3cDemo\Tools\NetworkTest\Assert-NetworkTestProductBuild.ps1")
$manifest = Assert-NetworkTestProductBuild `
    -Root $clientBuildRoot `
    -RepositoryRoot $root `
    -ExpectedProductId "thirdperson.network-test.dotrecast-authority" `
    -ExpectedNetworkModelIdentity "thirdperson.network-model.server-authoritative-hybrid" `
    -ExpectedRuntimeTopologyIdentity "thirdperson.runtime-topology.dotrecast-authority.three-process.v1" `
    -ExpectedPlayerRoleId "unity-client-player" `
    -ExpectedAdditionalArtifactRoleIds @("dotrecast-authority-server") `
    -ExpectedScenes @(
        "Assets/Scenes/ServerAuthoritative/ServerAuthoritativeNetworkTestBootstrap.unity",
        "Assets/Scenes/ServerAuthoritative/DotRecastAuthorityClient.unity") `
    -ExpectedScriptingBackend "IL2CPP"
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

$existing = @(Get-OwnedProcess)
if ($existing.Count -gt 0 -and !$StopExisting) {
    throw "Existing DotRecast Authority test processes are still running. Close them or use -StopExisting."
}
if ($StopExisting) {
    foreach ($process in $existing) {
        Stop-Process -Id $process.ProcessId -Force
    }
    if ($existing.Count -gt 0) {
        $releaseDeadline = (Get-Date).AddSeconds(5)
        do {
            $occupied = @(Get-NetUDPEndpoint -ErrorAction SilentlyContinue | Where-Object {
                $_.LocalPort -eq $controlPort -or $_.LocalPort -eq $authorityDataPort
            })
            if ($occupied.Count -eq 0) {
                break
            }
            Start-Sleep -Milliseconds 100
        } while ((Get-Date) -lt $releaseDeadline)
    }
}
Assert-PortsAvailable

New-Item -ItemType Directory -Force -Path $logs | Out-Null
New-Item -ItemType Directory -Force -Path $serverLogRoot | Out-Null
$window = @("-screen-fullscreen", "0", "-screen-width", "900", "-screen-height", "600")
$serverArguments = @(
    "--m", "Develop"
)
$clientAArguments = @(
    "--network-test-scenario=dotrecast-authority-client",
    "--server-authoritative-role=client-a",
    "--server-authoritative-player-id=corin-player-a",
    "--server-authoritative-actor-id=corin-actor-a",
    "-logFile", (Join-Path $logs "client-a.log")
) + $window
$clientBArguments = @(
    "--network-test-scenario=dotrecast-authority-client",
    "--server-authoritative-role=client-b",
    "--server-authoritative-player-id=corin-player-b",
    "--server-authoritative-actor-id=corin-actor-b",
    "-logFile", (Join-Path $logs "client-b.log")
) + $window

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
    $started.Add([pscustomobject]@{ Label = "Fantasy Server"; Path = $server; Process = $serverProcess })
    Start-Sleep -Seconds 3

    $clientAProcess = Start-Process -FilePath $client -ArgumentList $clientAArguments -RedirectStandardOutput $clientAOutput -RedirectStandardError $clientAError -PassThru
    $started.Add([pscustomobject]@{ Label = "Client A"; Path = $client; Process = $clientAProcess })
    Start-Sleep -Seconds 1

    $clientBProcess = Start-Process -FilePath $client -ArgumentList $clientBArguments -RedirectStandardOutput $clientBOutput -RedirectStandardError $clientBError -PassThru
    $started.Add([pscustomobject]@{ Label = "Client B"; Path = $client; Process = $clientBProcess })
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
        "BuildId: $($manifest.buildId)",
        "Fantasy Server PID: $($serverProcess.Id)",
        "Client A PID: $($clientAProcess.Id)",
        "Client B PID: $($clientBProcess.Id)",
        "Fantasy control port: $controlPort",
        "Authority gameplay data port: $authorityDataPort",
        "Authority manifest hash: $($manifest.authorityManifestHash)",
        "Log directory: $logs"
    ) -join [Environment]::NewLine
    Write-LauncherResult $result
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

param(
    [string]$Server = "http://127.0.0.1:8080",
    [int]$TimeoutSec = 3
)

$ErrorActionPreference = "Stop"

function Get-UnityProcessSummary {
    $processes = Get-Process Unity -ErrorAction SilentlyContinue
    if (-not $processes) {
        return "unityProcess=False"
    }

    $ids = ($processes | Select-Object -ExpandProperty Id) -join ","
    return "unityProcess=True unityPids=$ids"
}

try {
    $health = Invoke-RestMethod -Uri "$Server/health" -TimeoutSec $TimeoutSec
}
catch {
    $unity = Get-UnityProcessSummary
    Write-Output "UNITY_MCP_CHECK result=FAIL reason=server-unreachable server=$Server $unity"
    exit 1
}

try {
    $instances = Invoke-RestMethod -Uri "$Server/api/instances" -TimeoutSec $TimeoutSec
}
catch {
    $unity = Get-UnityProcessSummary
    Write-Output "UNITY_MCP_CHECK result=FAIL reason=instances-unreachable server=$Server health=$($health.status) $unity"
    exit 1
}

$instanceCount = 0
if ($instances.instances) {
    $instanceCount = @($instances.instances).Count
}

$unitySummary = Get-UnityProcessSummary
if ($health.status -ne "healthy") {
    Write-Output "UNITY_MCP_CHECK result=FAIL reason=server-not-healthy server=$Server health=$($health.status) instances=$instanceCount $unitySummary"
    exit 1
}

if ($instanceCount -lt 1) {
    Write-Output "UNITY_MCP_CHECK result=FAIL reason=no-unity-instance server=$Server health=$($health.status) instances=0 $unitySummary"
    exit 1
}

Write-Output "UNITY_MCP_CHECK result=PASS server=$Server health=$($health.status) instances=$instanceCount $unitySummary"
exit 0

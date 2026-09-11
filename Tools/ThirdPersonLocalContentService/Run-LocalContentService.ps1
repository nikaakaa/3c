param(
    [Parameter(Mandatory = $true)]
    [string]$ContentRoot
)

$ErrorActionPreference = 'Stop'

$toolsRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$dll = Join-Path $toolsRoot 'bin\Release\net8.0\ThirdPerson.LocalContentService.dll'
if (-not (Test-Path $dll)) { throw "build output is missing: $dll" }

$pfx = Join-Path $toolsRoot 'certs\localhost.pfx'
if (-not (Test-Path $pfx)) { throw "certificate is missing, run New-Certs.ps1 first: $pfx" }

dotnet $dll `
    --root $ContentRoot `
    --pfx $pfx `
    --pfx-password thirdperson-local `
    --https-port 8443 `
    --wss-port 9443 `
    --auth-upstream ws://127.0.0.1:21000

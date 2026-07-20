$ErrorActionPreference = "Stop"
$productRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\..\Build\Server\Startup"))
$executable = Join-Path $productRoot "ThirdPerson.Startup.Server.exe"
$manifest = Join-Path $productRoot "ServerProductBuild.json"
if (-not (Test-Path -LiteralPath $executable -PathType Leaf) -or
    -not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
    throw "Published Startup Server executable and manifest are required."
}

& $executable -m Release --pid 1
exit $LASTEXITCODE

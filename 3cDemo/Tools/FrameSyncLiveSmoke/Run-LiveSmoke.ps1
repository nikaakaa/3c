$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '../../..')
$smokeProject = Join-Path $repoRoot '3cDemo/Tools/FrameSyncLiveSmoke/FrameSyncLiveSmoke.csproj'

dotnet build $smokeProject | Out-Host

dotnet run --project $smokeProject -- -m Develop
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

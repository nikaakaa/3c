<#
目的：在同一段真实混合输入上执行历史与候选贡献选择、Stored 捕获和接管，保存可复现的业务结果。
边界：当前独立入口止于 FootMotion，不把它当作 Native Slot、Goal、Physics 或 FBBIK 通过。
#>
$ErrorActionPreference = 'Stop'
$storedRepo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$storedClient = Join-Path $storedRepo '3cDemo/Client/3C_Client'
$storedEvidence = Join-Path $storedRepo 'docs/diagnostics/foot-placement/ik-tests'
$storedPython = 'C:/Users/Lenovo/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $storedPython (Join-Path $PSScriptRoot 'prepare_inputs.py')
if ($LASTEXITCODE -ne 0) { throw '固定输入整理失败' }
& $storedPython (Join-Path $PSScriptRoot 'prepare.py')
if ($LASTEXITCODE -ne 0) { throw '正式源码绑定准备失败' }
$storedProject = Join-Path $storedClient 'Temp/FootStoredPoseFunctions/StoredPoseVerify.csproj'
try {
    & dotnet build $storedProject --disable-build-servers /nr:false /p:UseSharedCompilation=false --verbosity quiet
    $storedBuildExit = $LASTEXITCODE
} finally {
    & dotnet build-server shutdown
}
if ($storedBuildExit -ne 0) { throw '函数入口编译失败' }
$storedAssembly = Join-Path $storedClient 'Temp/FootStoredPoseFunctions/bin/Debug/net8.0/ThirdPersonClient.Editor.dll'
$storedFailed = $false
foreach ($storedCase in @('stored-no-contact-left', 'stored-contact-right')) {
    & dotnet $storedAssembly (Join-Path $storedEvidence "$storedCase-input.json") (Join-Path $storedEvidence "$storedCase-functions.json")
    if ($LASTEXITCODE -ne 0) { $storedFailed = $true }
}
& $storedPython (Join-Path $PSScriptRoot 'render_report.py')
if ($LASTEXITCODE -ne 0) { throw '场景报告生成失败' }
if ($storedFailed) { exit 1 }

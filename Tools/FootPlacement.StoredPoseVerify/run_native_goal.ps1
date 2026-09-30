<#
目的：把保存的 Stored 正式来源函数结果接入 GameplayLabFixed 的实际脚端函数和 PhysicsScene。
边界：冻结18e脚端源码；需要已生成的来源结果。结果写到指定目录，不覆盖历史证据。
#>
param(
    [Parameter(Mandatory=$true)][string]$UnityInstance,
    [Parameter(Mandatory=$true)][string]$ResultDirectory,
    [ValidateSet('sources','rotation')][string]$Mode = 'sources',
    [string]$UnityEditorData = 'C:/Program Files/Unity/Hub/Editor/2022.3.62f2c1/Editor/Data'
)
$ErrorActionPreference = 'Stop'
$env:PYTHONUTF8 = '1'
$storedRepo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$storedClient = Join-Path $storedRepo '3cDemo/Client/3C_Client'
$storedTemp = Join-Path $storedClient 'Temp/FootStoredPoseFunctions'
$storedEvidence = Join-Path $storedRepo 'docs/diagnostics/foot-placement/ik-tests'
$storedCli = Join-Path $env:USERPROFILE '.unity-mcp/Server/.venv/Scripts/unity-mcp.exe'
$storedPython = 'C:/Users/Lenovo/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$storedStateJson = & $storedCli --instance $UnityInstance --format json code execute 'return new { project_path=Application.dataPath, play=EditorApplication.isPlaying, compiling=EditorApplication.isCompiling };'
$storedState = ($storedStateJson | ConvertFrom-Json).result.data.result
if ([IO.Path]::GetFullPath($storedState.project_path) -ne (Join-Path $storedClient 'Assets') -or $storedState.play -or $storedState.compiling) { throw '目标项目必须匹配，且 Editor 非Play、非编译' }
New-Item -ItemType Directory -Path $ResultDirectory -Force | Out-Null
& $storedCli --instance $UnityInstance --format json code execute 'return System.AppDomain.CurrentDomain.GetAssemblies().Where(x=>!x.IsDynamic&&!string.IsNullOrEmpty(x.Location)).Select(x=>x.Location).ToArray();' | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $storedTemp 'editor-assemblies.json')
$storedVariants = if ($Mode -eq 'sources') { @('historical') } else { @('historical','current') }
$storedCases = if ($Mode -eq 'sources') { @('stored-no-contact-left','stored-contact-right') } else { @('live-switch-right','stored-no-contact-left','stored-contact-rotation-right') }
foreach ($storedVariant in $storedVariants) {
    & $storedPython (Join-Path $PSScriptRoot 'prepare_native_goal.py') --mode $Mode --variant $storedVariant
    if ($LASTEXITCODE -ne 0) { throw '原生 Goal 源码准备失败' }
    $storedVariantTemp = $storedTemp
    if ($Mode -eq 'rotation') {
        $storedCommit = if ($storedVariant -eq 'historical') { '552f13083' } else { '15894ee7b' }
        $storedVariantTemp = Join-Path $storedClient "Temp/FootRotationComparison/$storedVariant-$storedCommit"
    }
    try {
        $storedRsp = Join-Path $storedVariantTemp 'native-goal-compile.rsp'
        & (Join-Path $UnityEditorData 'NetCoreRuntime/dotnet.exe') (Join-Path $UnityEditorData 'DotNetSdkRoslyn/csc.dll') /noconfig "@$storedRsp"
        $storedCompileExit = $LASTEXITCODE
    } finally {
        & dotnet build-server shutdown
    }
    if ($storedCompileExit -ne 0) { throw 'Unity 真实绑定的函数入口编译失败' }
    foreach ($storedCase in $storedCases) {
    $storedStem = if ($Mode -eq 'sources') { $storedCase } else { "$storedCase-rotation-$storedVariant" }
    $storedFootVariant = if ($Mode -eq 'sources') { 'sources' } else { "rotation-$storedVariant" }
    $storedArgs = @((Join-Path $storedEvidence "$storedCase-input.json"), (Join-Path $storedEvidence "$storedCase-functions.json"), ([IO.Path]::GetFullPath((Join-Path $ResultDirectory "$storedStem-goal.json"))), $storedFootVariant) | ConvertTo-Json -Compress
    $storedDll = (Join-Path $storedVariantTemp 'ThirdPersonClient.Editor.dll') | ConvertTo-Json -Compress
    $storedCode = @'
var a=System.Reflection.Assembly.Load(System.IO.File.ReadAllBytes(@@DLL@@));
var args=Newtonsoft.Json.Linq.JArray.Parse(@"@@ARGS@@").Select(x=>(object)(string)x).ToArray();
return a.GetType("ThirdPersonCharacter.Pipeline.Editor.CharacterFootCapturedContactTests").GetMethod("RunStoredGoalComparison").Invoke(null,args);
'@
    $storedCode = $storedCode.Replace('@@DLL@@',$storedDll).Replace('@@ARGS@@',$storedArgs.Replace('"','""'))
    $storedCodePath = Join-Path $storedTemp "execute-$storedCase.cs"
    Set-Content -Encoding UTF8 -LiteralPath $storedCodePath -Value $storedCode
    & $storedCli --instance $UnityInstance --format json code execute -f $storedCodePath | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $ResultDirectory "$storedStem-command.json")
    $storedReport = Get-Content -Encoding UTF8 -Raw -LiteralPath (Join-Path $ResultDirectory "$storedStem-goal.json") | ConvertFrom-Json
    if ($storedReport.status -ne 'passed') { throw "场景执行失败：$storedCase，证据已保存" }
    }
    Copy-Item -LiteralPath (Join-Path $storedVariantTemp 'native-goal-source-manifest.json') -Destination (Join-Path $ResultDirectory "$Mode-$storedVariant-source-manifest.json")
}

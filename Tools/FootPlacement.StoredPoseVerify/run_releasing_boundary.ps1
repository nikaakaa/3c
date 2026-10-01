param([Parameter(Mandatory=$true)][string]$UnityInstance)
$ErrorActionPreference='Stop'
$env:PYTHONUTF8='1'
$boundaryRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$boundaryClient=Join-Path $boundaryRepo '3cDemo/Client/3C_Client'
$boundaryTemp=Join-Path $boundaryClient 'Temp/FootReleasingBoundary'
$boundaryEvidence=Join-Path $boundaryRepo 'docs/diagnostics/foot-placement/ik-tests'
$boundaryCli=Join-Path $env:USERPROFILE '.unity-mcp/Server/.venv/Scripts/unity-mcp.exe'
$boundaryPython='C:/Users/Lenovo/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
New-Item -ItemType Directory -Force -Path $boundaryTemp | Out-Null
$boundaryStateJson=& $boundaryCli --instance $UnityInstance --format json code execute 'return new {project_path=Application.dataPath,play=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,updating=EditorApplication.isUpdating,assemblies=System.AppDomain.CurrentDomain.GetAssemblies().Where(x=>!x.IsDynamic&&!string.IsNullOrEmpty(x.Location)).Select(x=>x.Location).ToArray()};'
Set-Content -Encoding UTF8 -LiteralPath (Join-Path $boundaryTemp 'availability.json') -Value $boundaryStateJson
$boundaryState=($boundaryStateJson|ConvertFrom-Json).result.data.result
if($null -eq $boundaryState -or $boundaryState.project_path -ne ($boundaryClient.Replace('\','/')+'/Assets') -or $boundaryState.play -or $boundaryState.compiling -or $boundaryState.updating){
    @{status='not-executed';reason='唯一Editor检查未取得匹配项目且空闲的目标状态';availability=$boundaryStateJson} | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $boundaryEvidence 'releasing-boundary-result.json')
    Write-Output '未执行；停止，不轮询'
    exit 2
}
@{result=@{data=@{result=$boundaryState.assemblies}}} | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $boundaryTemp 'editor-assemblies.json')
& $boundaryPython (Join-Path $PSScriptRoot 'prepare_releasing_boundary.py') --editor-assemblies (Join-Path $boundaryTemp 'editor-assemblies.json')
if($LASTEXITCODE -ne 0){throw '构建输入准备失败，保留候选快照'}
$boundaryEditorData='C:/Program Files/Unity/Hub/Editor/2022.3.62f2c1/Editor/Data'
foreach($boundaryVariant in @('baseline','candidate','foot')){
    $boundaryFolder=Join-Path $boundaryTemp $boundaryVariant
    $boundaryRsp=if($boundaryVariant -eq 'foot'){'native-goal-compile.rsp'}else{'compile.rsp'}
    try{
        & (Join-Path $boundaryEditorData 'NetCoreRuntime/dotnet.exe') (Join-Path $boundaryEditorData 'DotNetSdkRoslyn/csc.dll') /noconfig "@$(Join-Path $boundaryFolder $boundaryRsp)" | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $boundaryFolder 'compile.txt')
        $boundaryCompileExit=$LASTEXITCODE
    }finally{& dotnet build-server shutdown}
    if($boundaryCompileExit -ne 0){throw "独立入口编译失败：$boundaryVariant；不运行旧DLL"}
}
& $boundaryPython (Join-Path $PSScriptRoot 'seal_releasing_boundary_assemblies.py')
if($LASTEXITCODE -ne 0){throw '执行程序集封存失败'}
foreach($boundaryVariant in @('baseline','candidate')){
    $boundaryArgs=@((Join-Path $boundaryRepo 'docs/diagnostics/foot-placement/ik-tests/releasing-business-right-input.json'),(Join-Path $boundaryTemp "$boundaryVariant/native.json"),"fbed4b192+$boundaryVariant-sampling")|ConvertTo-Json -Compress
    $boundaryNativeDll=(Join-Path $boundaryTemp "$boundaryVariant/ThirdPersonClient.Editor.dll")|ConvertTo-Json -Compress
    $boundaryFootDll=(Join-Path $boundaryTemp 'foot/ThirdPersonClient.Editor.dll')|ConvertTo-Json -Compress
    $boundaryGoalArgs=@((Join-Path $boundaryRepo 'docs/diagnostics/foot-placement/ik-tests/releasing-business-right-input.json'),(Join-Path $boundaryTemp "$boundaryVariant/native.json"),(Join-Path $boundaryTemp "$boundaryVariant/goal.json"),'boundary-current')|ConvertTo-Json -Compress
    $boundaryCode=@'
if(Application.dataPath != @@PROJECT@@ || EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating) throw new System.InvalidOperationException("目标项目不匹配或Editor已进入非空闲状态；停止执行");
var n=System.Reflection.Assembly.Load(System.IO.File.ReadAllBytes(@@NATIVE@@));
var native=(Newtonsoft.Json.Linq.JObject)n.GetType("ThirdPersonCharacter.Pipeline.Editor.CapturedReleasingNativeTests").GetMethod("Run").Invoke(null,Newtonsoft.Json.Linq.JArray.Parse(@"@@NARGS@@").ToObject<object[]>());
if((string)native["status"]!="passed")return native;
var f=System.Reflection.Assembly.Load(System.IO.File.ReadAllBytes(@@FOOT@@));
var goal=(Newtonsoft.Json.Linq.JObject)f.GetType("ThirdPersonCharacter.Pipeline.Editor.CharacterFootCapturedContactTests").GetMethod("RunStoredGoalComparison").Invoke(null,Newtonsoft.Json.Linq.JArray.Parse(@"@@GARGS@@").ToObject<object[]>());
return new {nativeStatus=native["status"],goalStatus=goal["status"],allocatedBytes=goal["current"]["allocatedBytes"],nativeMvid=n.ManifestModule.ModuleVersionId,footMvid=f.ManifestModule.ModuleVersionId};
'@
    $boundaryCode=$boundaryCode.Replace('@@PROJECT@@',(($boundaryClient.Replace('\','/')+'/Assets')|ConvertTo-Json -Compress)).Replace('@@NATIVE@@',$boundaryNativeDll).Replace('@@FOOT@@',$boundaryFootDll).Replace('@@NARGS@@',$boundaryArgs.Replace('"','""')).Replace('@@GARGS@@',$boundaryGoalArgs.Replace('"','""'))
    $boundaryCodePath=Join-Path $boundaryTemp "$boundaryVariant/execute.cs"
    Set-Content -Encoding UTF8 -LiteralPath $boundaryCodePath -Value $boundaryCode
    & $boundaryCli --instance $UnityInstance --format json code execute -f $boundaryCodePath | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $boundaryTemp "$boundaryVariant/command.json")
}
& $boundaryPython (Join-Path $PSScriptRoot 'compare_releasing_boundary.py')

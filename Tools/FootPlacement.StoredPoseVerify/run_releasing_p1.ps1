param([Parameter(Mandatory=$true)][string]$UnityInstance)
$ErrorActionPreference='Stop'
$env:PYTHONUTF8='1'
$p1Repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$p1Client=Join-Path $p1Repo '3cDemo/Client/3C_Client'
$p1Temp=Join-Path $p1Client 'Temp/FootReleasingP1'
$p1Evidence=Join-Path $p1Repo 'docs/diagnostics/foot-placement/ik-tests'
$p1Cli=Join-Path $env:USERPROFILE '.unity-mcp/Server/.venv/Scripts/unity-mcp.exe'
$p1Python='C:/Users/Lenovo/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $p1Python (Join-Path $PSScriptRoot 'prepare_releasing_p1.py')
if($LASTEXITCODE -ne 0){throw 'P1构建输入准备失败'}
$p1EditorData='C:/Program Files/Unity/Hub/Editor/2022.3.62f2c1/Editor/Data'
foreach($p1Variant in @('native','baseline','candidate')){
    $p1Folder=Join-Path $p1Temp $p1Variant
    $p1Rsp=if($p1Variant -eq 'native'){'compile.rsp'}else{'native-goal-compile.rsp'}
    try{
        & (Join-Path $p1EditorData 'NetCoreRuntime/dotnet.exe') (Join-Path $p1EditorData 'DotNetSdkRoslyn/csc.dll') /noconfig "@$(Join-Path $p1Folder $p1Rsp)" | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $p1Folder 'compile.txt')
        $p1CompileExit=$LASTEXITCODE
    }finally{& dotnet build-server shutdown | Out-Null}
    if($p1CompileExit -ne 0){Get-Content -Encoding UTF8 -LiteralPath (Join-Path $p1Folder 'compile.txt');throw "P1独立入口编译失败：$p1Variant；停止，不运行旧DLL"}
}
& $p1Python (Join-Path $PSScriptRoot 'seal_releasing_p1_assemblies.py')
if($LASTEXITCODE -ne 0){throw 'P1执行程序集封存失败'}
foreach($p1Variant in @('baseline','candidate')){
    $p1NativeDll=(Join-Path $p1Temp 'native/ThirdPersonClient.Editor.dll')|ConvertTo-Json -Compress
    $p1FootDll=(Join-Path $p1Temp "$p1Variant/ThirdPersonClient.Editor.dll")|ConvertTo-Json -Compress
    $p1NativeArgs=@((Join-Path $p1Evidence 'releasing-business-right-input.json'),(Join-Path $p1Temp 'native/native.json'),'fbed4b192-p1-frozen-native')|ConvertTo-Json -Compress
    $p1GoalArgs=@((Join-Path $p1Evidence 'releasing-business-right-input.json'),(Join-Path $p1Temp 'native/native.json'),(Join-Path $p1Temp "$p1Variant/goal.json"),'p1-current')|ConvertTo-Json -Compress
    $p1NativeCode=if($p1Variant -eq 'baseline'){@'
var n=System.Reflection.Assembly.Load(System.IO.File.ReadAllBytes(@@NATIVE@@));
var native=(Newtonsoft.Json.Linq.JObject)n.GetType("ThirdPersonCharacter.Pipeline.Editor.CapturedReleasingNativeTests").GetMethod("Run").Invoke(null,Newtonsoft.Json.Linq.JArray.Parse(@"@@NARGS@@").ToObject<object[]>());
if((string)native["status"]!="passed")return native;
'@}else{''}
    $p1Code=@'
if(Application.dataPath != @@PROJECT@@ || EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating) return new {status="not-executed",reason="目标项目不匹配或Editor已进入非空闲状态；停止执行",project_path=Application.dataPath,play=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,updating=EditorApplication.isUpdating};
@@NATIVE_CODE@@
var f=System.Reflection.Assembly.Load(System.IO.File.ReadAllBytes(@@FOOT@@));
var goal=(Newtonsoft.Json.Linq.JObject)f.GetType("ThirdPersonCharacter.Pipeline.Editor.CharacterFootCapturedContactTests").GetMethod("RunStoredGoalComparison").Invoke(null,Newtonsoft.Json.Linq.JArray.Parse(@"@@GARGS@@").ToObject<object[]>());
return new {goalStatus=goal["status"],allocatedBytes=goal["current"]["allocatedBytes"],footMvid=f.ManifestModule.ModuleVersionId};
'@
    $p1Code=$p1Code.Replace('@@NATIVE_CODE@@',$p1NativeCode).Replace('@@PROJECT@@',(($p1Client.Replace('\','/')+'/Assets')|ConvertTo-Json -Compress)).Replace('@@NATIVE@@',$p1NativeDll).Replace('@@FOOT@@',$p1FootDll).Replace('@@NARGS@@',$p1NativeArgs.Replace('"','""')).Replace('@@GARGS@@',$p1GoalArgs.Replace('"','""'))
    $p1CodePath=Join-Path $p1Temp "$p1Variant/execute.cs"
    Set-Content -Encoding UTF8 -LiteralPath $p1CodePath -Value $p1Code
    & $p1Cli --instance $UnityInstance --format json code execute -f $p1CodePath | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $p1Temp "$p1Variant/command.json")
    if($LASTEXITCODE -ne 0){throw "P1原作业失败：$p1Variant；保留失败，不轮询"}
    $p1Command=Get-Content -Encoding UTF8 -Raw -LiteralPath (Join-Path $p1Temp "$p1Variant/command.json")|ConvertFrom-Json
    if($p1Command.result.success -ne $true -or $p1Command.result.data.result.goalStatus -ne 'passed'){throw "P1入口未完成：$p1Variant；CLI退出码0不代表函数成功，停止后续调用"}
}

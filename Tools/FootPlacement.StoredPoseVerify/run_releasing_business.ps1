<#
目的：同一2023种子、2024至2056完整释放业务的Native、预测/双脚Goal与真实IK重算。
输入：正式固定输入；历史和来源修正源码冻结；高度候选从已保存源码快照读取。
边界：Unity内函数实验，无正式Runner、Replay或自动输入；IK校准失败仍保留完整候选输出。
#>
param(
    [Parameter(Mandatory=$true)][string]$UnityInstance,
    [Parameter(Mandatory=$true)][string]$ResultDirectory,
    [switch]$HeightCandidate,
    [string]$UnityEditorData='C:/Program Files/Unity/Hub/Editor/2022.3.62f2c1/Editor/Data'
)
$ErrorActionPreference='Stop'
$env:PYTHONUTF8='1'
$releaseRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$releaseClient=Join-Path $releaseRepo '3cDemo/Client/3C_Client'
$releaseOutput=[IO.Path]::GetFullPath($ResultDirectory)
$releaseCli=Join-Path $env:USERPROFILE '.unity-mcp/Server/.venv/Scripts/unity-mcp.exe'
$releasePython='C:/Users/Lenovo/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$releaseTemp=Join-Path $releaseClient ('Temp/FootReleasingBusiness/'+$(if($HeightCandidate){'height'}else{'baseline'}))
$releaseGoalTemp=Join-Path $releaseClient 'Temp/FootRotationComparison/historical-69a36d339'
$releaseInput=Join-Path $releaseRepo 'docs/diagnostics/foot-placement/ik-tests/releasing-business-right-input.json'
$releaseState=(& $releaseCli --instance $UnityInstance --format json code execute 'return new {project=Application.dataPath,play=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,updating=EditorApplication.isUpdating};' | ConvertFrom-Json).result.data.result
if([IO.Path]::GetFullPath($releaseState.project) -ne (Join-Path $releaseClient 'Assets') -or $releaseState.play -or $releaseState.compiling -or $releaseState.updating){throw '目标项目必须匹配且Editor空闲'}
New-Item -ItemType Directory -Force -Path $releaseOutput,$releaseTemp | Out-Null
$releasePrepare=@('--commit','d6d6ab9f9','--out',$releaseTemp)
if($HeightCandidate){$releasePrepare+=@('--foot-height-candidate','--candidate-snapshot',(Join-Path $releaseRepo 'docs/diagnostics/foot-placement/ik-tests/releasing-height-source-snapshot.zip'))}
& $releasePython (Join-Path $PSScriptRoot 'prepare_native_releasing.py') @releasePrepare
if($LASTEXITCODE -ne 0){throw 'Native源码准备失败'}
& $releasePython (Join-Path $PSScriptRoot 'prepare_native_goal.py') --mode releasing --commit 69a36d339
if($LASTEXITCODE -ne 0){throw 'Foot/IK源码准备失败'}
foreach($releaseBuild in @(@($releaseTemp,'compile.rsp'),@($releaseGoalTemp,'native-goal-compile.rsp'))){
    try{
        & (Join-Path $UnityEditorData 'NetCoreRuntime/dotnet.exe') (Join-Path $UnityEditorData 'DotNetSdkRoslyn/csc.dll') /noconfig "@$(Join-Path $releaseBuild[0] $releaseBuild[1])" | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $releaseOutput ($releaseBuild[1]+'.log'))
        $releaseBuildExit=$LASTEXITCODE
    }finally{& dotnet build-server shutdown}
    if($releaseBuildExit -ne 0){throw '编译失败，不运行旧程序集'}
}
function Invoke-ReleasingEntry([string]$Dll,[string]$Type,[string]$Method,[object[]]$Arguments,[string]$Stem){
    $releaseArgJson=ConvertTo-Json -Compress -InputObject $Arguments
    $releaseCode=@'
if(Application.dataPath != @@PROJECT@@ || EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating) throw new System.InvalidOperationException("目标Editor必须匹配且空闲");
var a=System.Reflection.Assembly.Load(System.IO.File.ReadAllBytes(@@DLL@@));
var args=Newtonsoft.Json.Linq.JArray.Parse(@"@@ARGS@@").ToObject<object[]>();
var r=(Newtonsoft.Json.Linq.JObject)a.GetType(@@TYPE@@).GetMethod(@@METHOD@@).Invoke(null,args);
return new {status=r["status"],failure=r["failure"],mvid=a.ManifestModule.ModuleVersionId};
'@
    $releaseCode=$releaseCode.Replace('@@PROJECT@@',((Join-Path $releaseClient 'Assets').Replace('\','/')|ConvertTo-Json -Compress)).Replace('@@DLL@@',($Dll|ConvertTo-Json -Compress)).Replace('@@TYPE@@',($Type|ConvertTo-Json -Compress)).Replace('@@METHOD@@',($Method|ConvertTo-Json -Compress)).Replace('@@ARGS@@',$releaseArgJson.Replace('"','""'))
    $releaseCodePath=Join-Path $releaseTemp ($Stem+'.cs')
    Set-Content -Encoding UTF8 -LiteralPath $releaseCodePath -Value $releaseCode
    & $releaseCli --instance $UnityInstance --format json code execute -f $releaseCodePath | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $releaseOutput ($Stem+'-command.json'))
}
$releaseNativeDll=Join-Path $releaseTemp 'ThirdPersonClient.Editor.dll'
$releaseGoalDll=Join-Path $releaseGoalTemp 'ThirdPersonClient.Editor.dll'
$releaseNativeResult=Join-Path $releaseOutput 'releasing-business-native.json'
$releasePoseResult=Join-Path $releaseOutput 'releasing-business-component-poses.json'
$releaseGoalResult=Join-Path $releaseOutput 'releasing-business-goal.json'
$releaseIkResult=Join-Path $releaseOutput 'releasing-business-ik.json'
Invoke-ReleasingEntry $releaseNativeDll 'ThirdPersonCharacter.Pipeline.Editor.CapturedReleasingNativeTests' 'Run' @($releaseInput,$releaseNativeResult,$(if($HeightCandidate){'saved-height-candidate'}else{'d6d6ab9f9'})) 'native'
if((Get-Content -Encoding UTF8 -Raw -LiteralPath $releaseNativeResult|ConvertFrom-Json).status -ne 'passed'){throw 'Native失败，保留证据'}
Invoke-ReleasingEntry $releaseNativeDll 'ThirdPersonCharacter.Pipeline.Editor.CapturedReleasingNativeTests' 'ExportComponentPoses' @($releaseInput,$releasePoseResult) 'poses'
Invoke-ReleasingEntry $releaseGoalDll 'ThirdPersonCharacter.Pipeline.Editor.CharacterFootCapturedContactTests' 'RunStoredGoalComparison' @($releaseInput,$releaseNativeResult,$releaseGoalResult,'releasing') 'goal'
if((Get-Content -Encoding UTF8 -Raw -LiteralPath $releaseGoalResult|ConvertFrom-Json).status -ne 'passed'){throw 'Foot失败，保留证据'}
Invoke-ReleasingEntry $releaseGoalDll 'ThirdPersonCharacter.Pipeline.Editor.CapturedReleasingFullIkTests' 'Run' @($releaseInput,$releasePoseResult,$releaseGoalResult,$releaseIkResult,$false,$false) 'ik'
Copy-Item -LiteralPath (Join-Path $releaseTemp 'source-manifest.json') -Destination (Join-Path $releaseOutput 'native-source-manifest.json')
Copy-Item -LiteralPath (Join-Path $releaseGoalTemp 'native-goal-source-manifest.json') -Destination (Join-Path $releaseOutput 'foot-source-manifest.json')
if((Get-Content -Encoding UTF8 -Raw -LiteralPath $releaseIkResult|ConvertFrom-Json).status -ne 'passed'){throw '实际IK输出已保存，历史校准失败，整体未通过'}

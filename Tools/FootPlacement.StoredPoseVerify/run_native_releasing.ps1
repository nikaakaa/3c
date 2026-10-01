<#
目的：按固定提交编译并执行 2035～2046 的 Native Slot / 全身 Action 混合业务。
边界：目标 Editor 非Play且非编译；计划权重为录制边界，未执行 Foot、Physics、FBBIK 或正式 Runner。
#>
param(
    [Parameter(Mandatory=$true)][string]$UnityInstance,
    [Parameter(Mandatory=$true)][string]$ResultDirectory,
    [string]$NativeCommit = 'd6d6ab9f9',
    [switch]$WorkingNative,
    [string]$UnityEditorData = 'C:/Program Files/Unity/Hub/Editor/2022.3.62f2c1/Editor/Data'
)
$ErrorActionPreference = 'Stop'
$env:PYTHONUTF8 = '1'
$nativeRepo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$nativeClient = Join-Path $nativeRepo '3cDemo/Client/3C_Client'
$nativeCli = Join-Path $env:USERPROFILE '.unity-mcp/Server/.venv/Scripts/unity-mcp.exe'
$nativePython = 'C:/Users/Lenovo/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$nativeTemp = Join-Path $nativeClient 'Temp/FootReleasingNative'
$nativeStateJson = & $nativeCli --instance $UnityInstance --format json code execute 'return new {project_path=Application.dataPath,play=EditorApplication.isPlaying,compiling=EditorApplication.isCompiling,updating=EditorApplication.isUpdating};'
$nativeState = ($nativeStateJson | ConvertFrom-Json).result.data.result
if ([IO.Path]::GetFullPath($nativeState.project_path) -ne (Join-Path $nativeClient 'Assets') -or $nativeState.play -or $nativeState.compiling -or $nativeState.updating) { throw '目标项目必须匹配且Editor已空闲' }
New-Item -ItemType Directory -Force -Path $ResultDirectory | Out-Null
& $nativeCli --instance $UnityInstance --format json code execute 'return System.AppDomain.CurrentDomain.GetAssemblies().Where(x=>!x.IsDynamic&&!string.IsNullOrEmpty(x.Location)).Select(x=>x.Location).ToArray();' | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $nativeClient 'Temp/FootStoredPoseFunctions/editor-assemblies.json')
$nativePrepareArgs = @('--commit',$NativeCommit)
if ($WorkingNative) { $nativePrepareArgs += '--working-native' }
& $nativePython (Join-Path $PSScriptRoot 'prepare_native_releasing.py') @nativePrepareArgs
if ($LASTEXITCODE -ne 0) { throw '源码准备失败' }
try {
    & (Join-Path $UnityEditorData 'NetCoreRuntime/dotnet.exe') (Join-Path $UnityEditorData 'DotNetSdkRoslyn/csc.dll') /noconfig "@$(Join-Path $nativeTemp 'compile.rsp')" | Tee-Object -FilePath (Join-Path $ResultDirectory 'compile.txt')
    $nativeCompileExit = $LASTEXITCODE
} finally { & dotnet build-server shutdown }
if ($nativeCompileExit -ne 0) { throw '正式函数入口编译失败，保留日志' }
$nativeManifest = Get-Content -Encoding UTF8 -Raw -LiteralPath (Join-Path $nativeTemp 'source-manifest.json') | ConvertFrom-Json
$nativeManifest | Add-Member -NotePropertyName compiledEntrySha256 -NotePropertyValue (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $nativeTemp 'ThirdPersonClient.Editor.dll')).Hash.ToLowerInvariant()
$nativeManifest | ConvertTo-Json -Depth 20 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $ResultDirectory 'releasing-action-native-provenance.json')
$nativeSourceLabel = if ($WorkingNative) { 'working-native-sha; remaining=' + $NativeCommit } else { $NativeCommit }
$nativeArgs = @((Join-Path $nativeRepo 'docs/diagnostics/foot-placement/ik-tests/releasing-action-native-input.json'),[IO.Path]::GetFullPath((Join-Path $ResultDirectory 'releasing-action-native.json')),$nativeSourceLabel) | ConvertTo-Json -Compress
$nativeDll = (Join-Path $nativeTemp 'ThirdPersonClient.Editor.dll') | ConvertTo-Json -Compress
$nativeCode = @'
var a=System.Reflection.Assembly.Load(System.IO.File.ReadAllBytes(@@DLL@@));
var args=Newtonsoft.Json.Linq.JArray.Parse(@"@@ARGS@@").Select(x=>(object)(string)x).ToArray();
return a.GetType("ThirdPersonCharacter.Pipeline.Editor.CapturedReleasingNativeTests").GetMethod("Run").Invoke(null,args);
'@
$nativeCode = $nativeCode.Replace('@@DLL@@',$nativeDll).Replace('@@ARGS@@',$nativeArgs.Replace('"','""'))
$nativeCodePath = Join-Path $nativeTemp 'execute.cs'
Set-Content -Encoding UTF8 -LiteralPath $nativeCodePath -Value $nativeCode
& $nativeCli --instance $UnityInstance --format json code execute -f $nativeCodePath | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $ResultDirectory 'command.json')
$nativeResult = Get-Content -Encoding UTF8 -Raw -LiteralPath (Join-Path $ResultDirectory 'releasing-action-native.json') | ConvertFrom-Json
if ($nativeResult.status -ne 'passed') { throw '场景未通过，失败证据已保存' }

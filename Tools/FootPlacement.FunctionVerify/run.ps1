<#
目的：连续执行同一份采样的历史/当前正式函数，对比目标交接并更新场景 HTML。
输入：expired-contact-input.json 与当前正式 FootMotion 配置；历史代码来自 fixture 中的提交。
边界：插值前目标选择，不执行 Unity 物理、最终 Goal 或骨骼求解。
#>
param([Parameter(Mandatory = $true)][string]$UnityEditorData)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$client = Join-Path $repo '3cDemo/Client/3C_Client'
$evidence = Join-Path $repo 'docs/diagnostics/foot-placement/ik-tests'
$fixturePath = Join-Path $evidence 'expired-contact-input.json'
$fixture = Get-Content -Raw -Encoding UTF8 $fixturePath | ConvertFrom-Json
$runtimeDirectory = '3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/FootPlacement'
$runtimeSource = "$runtimeDirectory/CharacterFootLandingRuntime.cs"
$profile = Join-Path $client 'Assets/Configs/Character/Corin/Pipeline/Presentation/FootPlacement/CorinFootPlacementProfile.asset'
$output = Join-Path $client 'Temp/FootContactFunctions'
[IO.Directory]::CreateDirectory($output) | Out-Null
$historicalPath = Join-Path $output 'CharacterFootLandingRuntime.cs'
$historical = & git -C $repo show "$($fixture.baselineCommit):$runtimeSource"
if ($LASTEXITCODE -ne 0) { throw '读取历史正式源码失败' }
[IO.File]::WriteAllText($historicalPath, ($historical -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
$head = & git -C $repo rev-parse HEAD
$sources = @{}
foreach ($name in @('CharacterFootLandingRuntime', 'CharacterFootStateTargetResolver',
    'CharacterFootSwingMotionBuilder', 'CharacterFootTransitionResolver',
    'CharacterFootTransitionRuntime', 'CharacterFootConstraintMath')) {
    $sources[$name] = (& git -C $repo hash-object "$runtimeDirectory/$name.cs")
}
$report = [ordered]@{
    status = 'running'; scope = 'pre-interpolation-target-selection'
    capture = $fixture.capture; baselineCommit = $fixture.baselineCommit; runtimeCommit = $head
    sourceBlobs = $sources; fixtureSha256 = (Get-FileHash $fixturePath -Algorithm SHA256).Hash.ToLowerInvariant()
    profileSha256 = (Get-FileHash $profile -Algorithm SHA256).Hash.ToLowerInvariant()
    sourceSha256 = $fixture.sourceSha256; command = "pwsh -File Tools/FootPlacement.FunctionVerify/run.ps1 -UnityEditorData `"$UnityEditorData`""
    utc = [DateTime]::UtcNow.ToString('o'); assertions = @()
    boundary = '只运行插值前目标管理、摆腿计算、状态推进与目标选择；输入的世界查询来自原采样。未执行最终 Goal、重新碰撞查询、骨骼 IK。'
}
$report.assemblies = @{}
foreach ($name in @('ThirdPersonCharacter.Animation', 'ThirdPersonSimulation.Core', 'Unity.Collections')) {
    $report.assemblies[$name] = (Get-FileHash (Join-Path $client "Library/ScriptAssemblies/$name.dll") -Algorithm SHA256).Hash.ToLowerInvariant()
}
$exitCode = 0
try {
    $versions = @{}
    foreach ($version in @('old', 'current')) {
        $buildDirectory = Join-Path $output $version
        $landing = if ($version -eq 'old') { $historicalPath } else { Join-Path $repo $runtimeSource }
        & dotnet build (Join-Path $PSScriptRoot 'FootPlacement.FunctionVerify.csproj') --disable-build-servers /nr:false /p:UseSharedCompilation=false "-p:UnityEditorData=$UnityEditorData" "-p:LandingSource=$landing" -o $buildDirectory --verbosity quiet
        $buildExit = $LASTEXITCODE
        & dotnet build-server shutdown
        if ($buildExit -ne 0) { throw "$version 编译失败，退出码 $buildExit" }
        $resultPath = Join-Path $output "$version.json"
        & dotnet (Join-Path $buildDirectory 'ThirdPersonClient.Editor.dll') $fixturePath $profile $resultPath
        $runExit = $LASTEXITCODE
        $versions[$version] = Get-Content -Raw -Encoding UTF8 $resultPath | ConvertFrom-Json
        $report[$version] = $versions[$version]
        if ($runExit -ne 0) { throw "$version 函数实验失败：$($versions[$version].error)" }
    }
    $checks = [Collections.Generic.List[object]]::new()
    function Check-Scenario([bool]$condition, [string]$meaning, [object]$expected, [object]$actual) {
        $checks.Add([ordered]@{ meaning = $meaning; passed = $condition; expected = $expected; actual = $actual })
        if (!$condition) { throw "$meaning；期望 $expected，实际 $actual" }
    }
    $report.assertions = $checks
    $old = $versions.old.rows
    $current = $versions.current.rows
    Check-Scenario ($old.Count -eq 100 -and $current.Count -eq 100) '两个版本均完整执行连续窗口' 100 $current.Count
    $changedNormal = 0
    $maxNormalDifference = 0.0
    $restored = 0
    for ($i = 0; $i -lt $old.Count; $i++) {
        $a = $old[$i]; $b = $current[$i]
        if ($a.frame -ne $b.frame -or $a.prepared -ne $a.recordedPrepared -or $a.targetAvailable -ne $a.recordedTargetAvailable) {
            throw "历史重现不符，首个帧 $($a.frame)：prepared=$($a.prepared)/$($a.recordedPrepared)，target=$($a.targetAvailable)/$($a.recordedTargetAvailable)"
        }
        if (!$b.targetAvailable) { throw "当前版目标仍丢失：采样帧 $($b.frame)" }
        if ($a.frame -lt 34713) {
            $distanceSquared = 0.0
            for ($axis = 0; $axis -lt 3; $axis++) {
                $delta = $a.targetPosition[$axis] - $b.targetPosition[$axis]
                $distanceSquared += $delta * $delta
            }
            $distance = [Math]::Sqrt($distanceSquared)
            $maxNormalDifference = [Math]::Max($maxNormalDifference, $distance)
            if ($distance -gt 0.00001 -or $a.prepared -ne $b.prepared -or $a.targetKind -ne $b.targetKind) { $changedNormal++ }
        }
        if (!$a.targetAvailable -and $b.targetAvailable -and $a.recordedWeight -eq 0) { $restored++ }
        if ($b.contact -eq 0 -and !$b.approach -and ($b.prepared -or !$b.swingAccepted -or $b.targetKind -ne 'SwingGround')) {
            throw "摆腿交接不符：采样帧 $($b.frame)，prepared=$($b.prepared)，kind=$($b.targetKind)"
        }
    }
    Check-Scenario ($changedNormal -eq 0) '释放前 45 帧的目标类型、有效期与位置保持一致（容差 0.01 mm）' 0 $maxNormalDifference
    Check-Scenario ($restored -eq 27) '原采样掉权重的 27 帧：旧版目标缺失、当前版目标有效' 27 $restored
    $approach = @($current | Where-Object { $_.approach -and $_.prepared }).Count
    $contact = @($current | Where-Object { $_.frame -ge 34689 -and $_.frame -le 34712 -and $_.prepared }).Count
    $swing = @($current | Where-Object { $_.contact -eq 0 -and !$_.approach -and !$_.prepared -and $_.targetKind -eq 'SwingGround' }).Count
    Check-Scenario ($approach -eq 20) 'Approach 阶段保留下一落地目标' 20 $approach
    Check-Scenario ($contact -eq 24) '实际接触的 24 帧继续保留当前目标' 24 $contact
    Check-Scenario ($swing -eq 53) 'Contact=0 的 53 帧交给正式摆腿目标' 53 $swing
    $report.metrics = [ordered]@{ frames = 100; normalFrames = 45; approachFrames = $approach; contactFrames = $contact; swingFrames = $swing; oldMissingTargets = @($old | Where-Object { !$_.targetAvailable }).Count; currentMissingTargets = @($current | Where-Object { !$_.targetAvailable }).Count; restoredTargets = $restored; maxNormalPositionDifferenceMeters = $maxNormalDifference }
    $report.status = 'passed'
} catch {
    $report.status = 'failed'
    $report.error = $_.ToString()
    $exitCode = 1
    Write-Host $report.error -ForegroundColor Red
} finally {
    $json = $report | ConvertTo-Json -Depth 40 -Compress
    [IO.File]::WriteAllText((Join-Path $evidence 'expired-contact-functions.json'), $json + "`n", [Text.UTF8Encoding]::new($false))
    $htmlPath = Join-Path $evidence 'expired-contact.html'
    $html = [IO.File]::ReadAllText($htmlPath, [Text.Encoding]::UTF8)
    $marker = '<script id="function-data" type="application/json">'
    $start = $html.IndexOf($marker, [StringComparison]::Ordinal) + $marker.Length
    $end = $html.IndexOf('</script>', $start, [StringComparison]::Ordinal)
    $embedded = $json.Replace('<', '\u003c')
    [IO.File]::WriteAllText($htmlPath + '.pending', $html.Substring(0, $start) + $embedded + $html.Substring($end), [Text.UTF8Encoding]::new($false))
    [IO.File]::Replace($htmlPath + '.pending', $htmlPath, [NullString]::Value)
}
Write-Host "函数实验：$($report.status)；结果：$(Join-Path $evidence 'expired-contact-functions.json')"
exit $exitCode

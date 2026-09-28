# 命令参考

## 获取正式 Host

在被测 worktree 执行：

```powershell
$repository = (git rev-parse --show-toplevel).Trim()
$commonGit = (git rev-parse --path-format=absolute --git-common-dir).Trim()
$configurationPath = Join-Path $commonGit 'development-project.json'
$configuration = Get-Content -Encoding UTF8 $configurationPath | ConvertFrom-Json
$artifactRoot = Join-Path $configuration.artifact_root $configuration.project_id
$hostReferences = @($configuration.tools | Where-Object {
    $manifest = Join-Path $artifactRoot $_.manifest_path
    (Get-Content -Encoding UTF8 $manifest | ConvertFrom-Json).descriptor.tool_id -eq 'thirdperson.development.run-host'
})
if ($hostReferences.Count -ne 1) { throw "Development configuration must select exactly one RunHost." }
$hostManifestPath = Join-Path $artifactRoot $hostReferences[0].manifest_path
$hostManifest = Get-Content -Encoding UTF8 $hostManifestPath | ConvertFrom-Json
$hostExe = Join-Path (Split-Path $hostManifestPath) $hostManifest.descriptor.entry_point
```

Host 从 2.1.0 起提供 change 命令；用 `describe` 检查能力。新命令操作 `ArtifactRoot/ProjectId/Changes/<change_id>.json`，运行仍保存到原 Runs 和 Comparisons，不增加第二套执行器。

## 创建或继续改动

```powershell
& $hostExe changes --configuration $configurationPath
& $hostExe change-create --configuration $configurationPath --repository $repository --title '修复楼梯穿地' --goal '减少穿地，保持落脚连续，Body 行为不退化'
& $hostExe change-show --configuration $configurationPath --change '<change_id>'
& $hostExe change-note --configuration $configurationPath --change '<change_id>' --message '正在检查落脚约束；修改前结果已关联'
```

已有记录应直接继续。新建返回的 change_id 属于改动；run_id 属于一次实际执行，两者不要混用。

## 前后验证

```powershell
& $hostExe change-attach --configuration $configurationPath --change '<change_id>' --phase before --run '<已完成且同条件的旧 RunId>'
& $hostExe change-run --configuration $configurationPath --change '<change_id>' --phase before --workflow character.diagnostic-replay --input '<trace.json绝对路径>' --timeout 900
& $hostExe change-run --configuration $configurationPath --change '<change_id>' --phase after --workflow character.diagnostic-replay --input '<同一 trace.json绝对路径>' --timeout 900
& $hostExe status --configuration $configurationPath --run '<run_id>'
& $hostExe change-compare --configuration $configurationPath --change '<change_id>'
```

复用旧基线与新跑 before 二选一。只需编译时改为 `--workflow compile` 并省略 input。关联的任一当前运行尚在排队或执行时，新的提交会被拒绝；先查询原运行。

高级请求仍使用既有 RunRequest JSON：
```powershell
& $hostExe change-submit --configuration $configurationPath --change '<change_id>' --phase after --request '<request.json>'
```
CLI 从改动记录确定 workspace，未指定 Host 时使用正式配置的选中版本。性能比较仍使用 operation=compare 的正式请求；change-compare 专用于两次 Editor 验证。

## 保存有证据的结论

先读取 change-show，确认引用的是当前结果。review.json 结构：

```json
{
  "summary": "两次固定输入与 Body 轨迹一致；脚部质量尚未完成分析。",
  "verified": ["相同输入和时钟下的 Body 轨迹一致"],
  "unverified": ["脚部穿地与连续性", "相机表现", "性能"],
  "evidence": [
    {
      "run_id": "<本次 comparison_run>",
      "kind": "Comparisons",
      "path": "comparison.json"
    },
    {
      "run_id": "<本次 current_run>",
      "kind": "Runs",
      "path": "editor-result.json"
    }
  ]
}
```

```powershell
& $hostExe change-review --configuration $configurationPath --change '<change_id>' --path '<review.json绝对路径>'
```

kind=Runs 的路径相对该运行目录；其他 kind 必须来自该 Run 的 status.results，路径相对该结果产物目录。文件必须存在于正式 manifest；CLI 验证并填写 sha256。不要把临时文件或另一条任务的报告当成本次证据。

失败时仍可保存有证据的失败说明。若运行期间版本改变，应写清来源，修复正式输入后重新验证；不能把 Completed 描述成通过。

## 多实例

配置 schema 2 显式记录 Unity、Unity MCP 和机器策略；workspace 中的实例 hash 来自规范化 Assets 路径。不得切换全局 active instance。目录只标识执行位置，源码内容、工具版本、录制输入和结果身份以 Run 证据为准。

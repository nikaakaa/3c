# Tasks

## 0. 前置核对
- [x] 0.1 读取 `AGENTS.md`、`openspec/AGENTS.md`、`openspec/project.md`。
- [x] 0.2 运行 `openspec list` 和 `openspec list --specs`。
- [x] 0.3 读取本 change 的 `proposal.md`、`design.md`、`tasks.md` 和 spec delta。
- [x] 0.4 读取相关 active change：`port-ref-timeline-ui-to-unity-2022-compatible-editor`、`migrate-ref-timeline-editor-to-formal-action-config`、`formalize-character-behavior-submission-runtime-chain`。
- [x] 0.5 读取 `action-timeline-framework`、`character-action-catalog`、`dodge-action`、`simulation-tick-system`、`character-frame-pipeline` 当前 specs。
- [x] 0.6 对将要修改的函数、类、方法运行 GitNexus `impact`。
- [x] 0.7 记录 HIGH / CRITICAL impact 的风险、直接调用方和受影响流程。

## 1. 时间模型与迁移边界
- [x] 1.1 盘点当前 `durationFrames/startFrame/endFrame/currentFrame` 使用点。
- [x] 1.2 定义 `ActionTimelineCompileContext.fixedTickSeconds` 或等价 seam，并由调用方从 simulation tick settings 传入。
- [x] 1.3 定义 seconds authoring 字段命名。
- [x] 1.4 定义 compiled tick 字段命名。
- [x] 1.5 定义 legacy frame 字段迁移策略：默认按 60Hz legacy authoring rate 转 seconds，再按 simulation tick settings 编译 ticks。
- [x] 1.6 增加禁止 runtime fallback 到 legacy frame 字段的静态检查或测试。

## 2. 量化模块
- [x] 2.1 实现 seconds -> tick 量化 helper。
- [x] 2.2 覆盖 duration seconds -> duration ticks。
- [x] 2.3 覆盖 clip start / end seconds -> `[startTick,endTick)`。
- [x] 2.4 覆盖 cue seconds -> cue tick。
- [x] 2.5 覆盖非法 tick interval、负秒、end 小于 start 的校验。
- [x] 2.6 编写量化边界 EditMode 测试。

## 3. Authoring 与 Runtime Definition
- [x] 3.1 将 `CommittedActionBranchTimelineAuthoring` 改为 seconds authoring。
- [x] 3.2 将 `ActionTimelineClipAuthoring` 改为 seconds authoring。
- [x] 3.3 保留必要 legacy frame 迁移输入并标记非 runtime 权威。
- [x] 3.4 将 `ActionTimelineDefinition` 改为 compiled tick runtime model。
- [x] 3.5 更新 validator，错误信息区分 authoring seconds 和 compiled tick。
- [x] 3.6 编写 authoring -> runtime definition 编译测试。

## 4. Evaluator 与 Branch
- [x] 4.1 将 `ActionTimelineEvaluator` 输入改为 `localTick/sourceStep`。
- [x] 4.2 将 sustained clip 采样统一为 `[startTick,endTick)`。
- [x] 4.3 将 cue 采样统一为 `cueTick == localTick`。
- [x] 4.4 确认 evaluator 不读取 seconds、Unity time、Animator time 或 editor state。
- [x] 4.5 更新 `CommittedActionBranchEvaluator` 调用。
- [x] 4.6 编写 evaluator 多轨同 tick 输出测试。
- [x] 4.7 编写 cue 单 tick 触发测试。

## 5. Action Lifecycle
- [x] 5.1 为 active action 记录 `actionStartStep` 或批准的等价整数 local tick 状态。
- [x] 5.2 使用 `sourceStep - actionStartStep` 推导 `localTick`。
- [x] 5.3 保留 elapsed seconds 作为派生诊断读数。
- [x] 5.4 更新 capture / restore，使恢复后 local tick 一致。
- [x] 5.5 编写 lifecycle local tick 推进测试。
- [x] 5.6 编写 rollback restore 后 timeline outcome 一致测试。

## 6. Editor Adapter 与 UI 时间显示
- [x] 6.1 将 `CommittedActionTimelineEditorAdapters` 改为读写 seconds 字段。
- [x] 6.2 将 add / move / resize clip 写回 seconds。
- [x] 6.3 将 preview locator 从 seconds 转为 preview local tick。
- [x] 6.4 在 preview summary 显示 local time、local tick 和量化结果。
- [x] 6.5 将 Ref UI 移植任务中的 frame ruler 改为 seconds ruler + tick grid。
- [x] 6.6 编写 editor adapter seconds 写回测试。
- [x] 6.7 编写 preview adapter 与 runtime evaluator 一致测试。

## 7. Dodge 与示例资产迁移
- [x] 7.1 将 Corin Dodge Directional timeline 迁移为 seconds authoring。
- [x] 7.2 将 Corin Dodge Backstep timeline 迁移为 seconds authoring。
- [x] 7.3 确认旧 Directional / Backstep variant 字段只作为诊断或迁移输入。
- [x] 7.4 编写 Dodge timeline compile 测试。
- [x] 7.5 编写缺失 seconds payload 不 fallback 测试。

## 8. 边界验证
- [x] 8.1 静态检查 runtime 不引用 UnityEditor、Ref runner、TimelinePlayer、PlayableGraph。
- [x] 8.2 静态检查 runtime timeline 不读取 editor preview state。
- [x] 8.3 静态检查正式 gameplay 不读取 legacy frame authoring 字段作为 fallback。
- [x] 8.4 确认没有新增第二 motion executor、第二 animation presenter、第二 blackboard writer 或第二角色控制入口。

## 9. 验证
- [x] 9.1 运行 `openspec validate refactor-action-timeline-time-authority --strict --no-interactive`。
- [x] 9.2 通过 Unity MCP 尽量运行相关 EditMode 测试。
- [x] 9.3 Unity MCP 不可用时记录未执行测试名和原因。
- [x] 9.4 每个相关 change 完成后运行 `detect_changes({scope:"all"})` 并记录影响范围。

# Design: Action Timeline Seconds Authoring 与 Tick Sampling

## Context
当前 ActionTimeline 规格和代码都围绕 `durationFrames/startFrame/endFrame/currentFrame` 展开。这个模型能表达确定性 tick，但它把设计者编辑时间、UI 坐标刻度和 runtime 采样 tick 混成同一层。

对 Committed Action Timeline Editor 来说，timeline 更像动作接管后的输出谱面：animation、motion、window 和 cue 应该以设计者可理解的 seconds 进行 authoring；正式 gameplay 则必须在 simulation tick 中以整数 local tick 采样，保证同输入、同 tick 序列下输出一致。

## Goals
- 让 authoring 数据使用 seconds 表达动作输出时间。
- 让 runtime compiled timeline 使用 deterministic local tick 采样。
- 让 evaluator 成为只读纯函数，不读取 Unity 时间、Animator 播放时间、Editor preview 状态或 scene object。
- 让 Editor UI 继续可视化 timeline，但主刻度为 seconds，tick 只作为 grid / preview 辅助。
- 保持输出仍进入 CommittedActionBranch outcome、typed submissions、BodyArbiter 和 OutputApplier。

## Non-Goals
- 不实现新 gameplay tick runner。
- 不引入 Ref runtime 播放器或 Unity Timeline / PlayableGraph。
- 不实现完整格斗 frame data editor。
- 不修改 motion executor 或 animation presenter 权威。

## Decisions

### Decision: Authoring 使用 seconds
`CommittedActionBranchTimelineAuthoring` 或批准的等价 authoring 数据保存：

- `durationSeconds`
- clip `startSeconds`
- clip `endSeconds`
- cue `timeSeconds` 或等价 point time

`durationFrames/startFrame/endFrame` 不再作为正式 authoring 主语。迁移期可以保留 legacy 字段用于一次性迁移或诊断，但 runtime 不得 fallback 到 legacy frame 字段。

### Decision: Runtime 使用 compiled tick
`ActionTimelineDefinition` 或批准的等价 runtime definition 保存整数 tick 结果：

- `durationTicks`
- clip `startTick`
- clip `endTick`
- cue `tick`

runtime sampling 的输入是 `localTick` 和 `sourceStep`。`localTick` 来自 `sourceStep - actionStartStep` 或批准的等价整数 action-local 状态。

ActionTimeline compiler MUST 通过 `ActionTimelineCompileContext.fixedTickSeconds` 或批准的等价 compile context 接收固定 tick 间隔。调用方负责从 simulation tick settings 解析该值并传入 compiler；compiler 本身不得读取 Unity `Time.fixedDeltaTime`、Editor 全局状态或 render frame 状态。

### Decision: 量化规则固定且可测试
固定 tick 间隔来自 simulation tick system 的 fixed delta 语义，并通过 compile context 显式进入 ActionTimeline compiler，不来自 Unity `Time.fixedDeltaTime` 或当前 render frame。

量化规则：

- `durationTicks = Ceil(durationSeconds / fixedTickSeconds)`
- `startTick = Ceil(startSeconds / fixedTickSeconds)`
- `endTick = Ceil(endSeconds / fixedTickSeconds)`
- `cueTick = Ceil(cueSeconds / fixedTickSeconds)`
- clip active 区间为 `[startTick, endTick)`
- cue 只在 `localTick == cueTick` 时触发

选择 `Ceil` 的原因是防止 gameplay 输出早于设计者设定时间发生；end 也使用 `Ceil`，保证 window 不会早于设定结束时间关闭。零长度区间不输出持续 clip，point cue 用 cueTick 表达。

### Decision: legacy frame 迁移先转 seconds
旧 `durationFrames/startFrame/endFrame/currentFrame` 只作为迁移输入或诊断输入。迁移器 MUST 使用显式 `legacyAuthoringFrameRate` 将 legacy frame 转为 seconds，默认 `legacyAuthoringFrameRate` 为 60Hz：

- `legacySeconds = legacyFrame / legacyAuthoringFrameRate`
- `compiledTick = Ceil(legacySeconds / fixedTickSeconds)`

迁移完成后，正式资产以 seconds authoring 字段为准。runtime 不得在 seconds 缺失或非法时 fallback 到 legacy frame 字段，也不得把 legacy frame 直接解释为当前项目 simulation tick。

### Decision: seconds 是 editor 和诊断语言，不是 runtime 权威
Editor 可以显示 seconds、tick grid 和本地预览 tick。诊断可以显示 local time。runtime 仲裁、rollback 对比、synctest 和 evaluator 单元测试必须以 tick 结果为权威。

### Decision: Action lifecycle 保存整数动作时序
Action lifecycle 可以继续暴露 elapsed seconds 作为诊断或 interrupt context 的派生读数，但 timeline sampling 必须使用 action-local tick。restore state 必须能恢复 active action 的 actionStartStep 或 active local tick，使恢复后同一 sourceStep 得到同一 timeline outcome。

### Decision: UI 迁移依赖时间权威
`port-ref-timeline-ui-to-unity-2022-compatible-editor` 中的 field ruler、grid、locator 和 clip move/resize 必须改为 seconds authoring。Ref 的 frame map 可作为视图实现参考，但不得让 frame 成为正式 authoring 字段。

## Risks / Trade-offs
- 风险：现有 Dodge asset 已保存 frame 字段。
  - 处理：实施阶段提供一次性迁移，将 legacy frame 按显式 legacy authoring frame rate 转为 seconds，默认 60Hz，再按 simulation tick settings 编译为 ticks，并在迁移后停止 runtime fallback。
- 风险：`Ceil` 量化让窗口开始略晚于 authoring 秒。
  - 处理：该规则防止提前触发，且必须在 editor preview 中显示量化后的 tick，避免误解。
- 风险：active UI change 已按 frame ruler 拆任务。
  - 处理：本 change 批准后，先完成时间模型迁移，再继续 UI 迁移，并更新对应任务描述。
- 风险：Action interrupt 仍需要 elapsed seconds。
  - 处理：elapsed seconds 从 localTick * fixedTickSeconds 派生，不能反向驱动 timeline。

## Migration Plan
1. 增加 seconds authoring 字段和 tick compiled model。
2. 添加 legacy frame -> seconds 迁移 helper 和测试，默认 legacy authoring frame rate 为 60Hz。
3. 将 `ToDefinition()` / compiler 改为 seconds -> tick。
4. 将 evaluator 改为 localTick 采样。
5. 将 lifecycle 改为 actionStartStep / localTick。
6. 将 editor adapter 和 UI 改为 seconds authoring。
7. 删除或隐藏 legacy frame 字段，停止 runtime fallback。

## Resolved Questions
- 项目固定 tick rate 以 simulation tick settings 为权威；本 change 定义 action timeline compiler 的显式 tick rate 输入 seam。
- 旧 frame asset 迁移默认按 60Hz legacy authoring rate 转为 seconds，再读取项目 simulation tick settings 编译成 ticks。

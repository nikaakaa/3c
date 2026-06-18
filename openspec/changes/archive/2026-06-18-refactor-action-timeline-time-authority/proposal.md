# Change: 重构 Action Timeline 时间权威

## Why
当前 `action-timeline-framework` 将 ActionTimeline 的正式时间单位定义为 frame，并让 editor 只把 seconds 当显示换算。这让 Committed Action Timeline Editor 容易把 UI 像素、authoring frame、runtime tick 和 action state time 混成一个概念，也会让后续 Ref UI 移植继续围绕 `durationFrames/startFrame/endFrame` 扩展，增加返工风险。

Committed Action Timeline 更适合作为动作接管后的输出编排工具：设计者用 seconds 编辑 animation、motion、window 和 cue 的时间；runtime 在固定 simulation tick 中通过 action-local tick 采样，保证 rollback / replay / synctest 可重复。

## What Changes
- **BREAKING**：将 ActionTimeline authoring 语义从 frame authoring 改为 seconds authoring。
- 为 ActionTimeline 增加正式 seconds -> deterministic tick 量化规则。
- ActionTimeline compiler MUST 通过显式 compile context 或批准的等价 seam 接收 `fixedTickSeconds`；该值由调用方从 simulation tick settings 解析，compiler 不直接读取 Unity time 或 editor 全局状态。
- 旧 frame asset 迁移 MUST 先按显式 legacy authoring frame rate 转为 seconds，默认 legacy authoring frame rate 为 60Hz，再按 simulation tick settings 编译为 runtime ticks。
- runtime `ActionTimelineDefinition` 或等价 compiled definition MUST 使用整数 tick 区间采样，不以 seconds、Unity deltaTime、Animator time 或 editor preview time 作为权威。
- Action lifecycle 推进 timeline 时 MUST 使用 action-local tick，该 tick 来自 `sourceStep - actionStartStep` 或批准的等价整数状态，而不是 float state time 除以 tick interval。
- Committed Action Timeline Editor MUST 以 seconds 作为主编辑单位，并在 preview 中显示 local time 与 local tick；tick grid 只能作为视图辅助。
- `port-ref-timeline-ui-to-unity-2022-compatible-editor` 继续负责 Ref UI 迁移，但必须以本 change 定义的 seconds authoring / tick preview 语义为前置边界。

## Non-Goals
- 不实现新的 motion executor、animation presenter、blackboard writer 或角色控制入口。
- 不引入 Ref `TimelinePlayer`、Taco tree、PlayableGraph 或 scene object preview runner。
- 不把 Timeline 升级成通用 Skill Editor 或格斗 Frame Data Editor。
- 不在本 proposal 阶段修改代码或资产。
- 不要求第一版视觉预览播放真实动画、VFX、SFX 或 camera shake；preview 仍以正式 evaluator outcome 为主。

## Impact
- Affected specs:
  - `action-timeline-framework`
  - `character-action-catalog`
  - `dodge-action`
  - `character-behavior-editor-adapters`
- Related active changes:
  - `port-ref-timeline-ui-to-unity-2022-compatible-editor`：UI 迁移任务需要改用 seconds ruler / tick grid 口径。
  - `migrate-ref-timeline-editor-to-formal-action-config`：已完成的正式 action definition 接入需要按新时间模型迁移。
  - `formalize-character-behavior-submission-runtime-chain`：typed submissions 方向保持不变，只调整 timeline 采样输入。
- Affected code after approval:
  - `ActionTimelineDefinition`、`ActionTimelineEvaluator`、`ActionTimelineOutcome`
  - `CommittedActionBranchTimelineAuthoring`、`ActionTimelineTrackAuthoring`、`ActionTimelineClipAuthoring`
  - `CharacterActionDefinitionSO.ToDefinition()` 与 validator / compiler
  - `ActionLifecycleRuntime` 的 timeline local tick 计算与 restore state
  - `CommittedActionTimelineEditorAdapters` 与 `CommittedActionRefPortedTimelineView`
- Test impact:
  - 需要新增 seconds -> tick 量化、采样边界、cue 触发、lifecycle local tick、editor adapter seconds 写回和 Dodge compile 测试。

## Validation
- `openspec validate refactor-action-timeline-time-authority --strict --no-interactive`
- 实施阶段需要定向 EditMode 测试覆盖：
  - seconds -> tick 量化规则。
  - `[startTick, endTick)` clip 采样。
  - cue point 只在目标 local tick 触发。
  - Action lifecycle 使用 action-local tick 而不是 float state time 作为 timeline 采样权威。
  - Editor adapter 将 seconds 字段写回正式 `CharacterActionDefinitionSO` 并编译为 deterministic tick timeline。
  - runtime 静态边界不引用 UnityEditor、Ref runner、TimelinePlayer 或 PlayableGraph。

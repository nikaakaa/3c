# Change: 增加 Committed Action Timeline 场景角色预览绑定

## Why
当前 Timeline Editor 已能用正式 compiler / evaluator 显示 tick、selected node、animation key、motion spec、window 和 cue 的数据预览，但设计者无法把它绑定到场景中的实际角色实例来观察动画姿态。

`Ref/wly970123` 的 TimelinePlayer / PlayableGraph 预览思路可以作为 Editor-only 参考，但不能成为本项目 Action runtime 或第二条 Timeline runner。

## What Changes
- 在 Committed Action Timeline Editor 中增加显式 scene preview target 绑定，目标是场景中的实际角色实例或其 Animator 所在 GameObject。
- 增加 Editor-only preview session：仍先通过正式 `CharacterActionDefinitionSO -> compiler -> CommittedActionBranchEvaluator -> ActionTimelineOutcome` 得到预览真相，再把 outcome 映射到视觉预览。
- 增加 Editor-only animation key resolver：用绑定角色上的正式动作动画表现入口、Animancer TransitionLibrary、Action Animation Profile 或批准等价绑定入口，把 `ActionAnimationKey` 解析为可采样 clip/transition。
- 参考 Ref 的 PlayableGraph 采样方式，为绑定角色的 Animator 提供 scrub / play 姿态预览。
- Motion / Window / Cue 第一版只做 editor 高亮、摘要和可选 ghost/path 诊断，不执行 motion executor、hitbox、VFX、SFX 或 camera 事件。
- 关闭窗口、解绑、停止预览或 domain reload 时必须销毁 preview graph 并恢复角色预览前状态。

## Impact
- Affected specs:
  - `committed-action-timeline-editor`
- Affected code:
  - `Assets/Editor/Character/Action/Timeline/CommittedActionTimelineEditorWindow.cs`
  - `Assets/Editor/Character/Action/Timeline/CommittedActionRefPortedTimelineView.cs`
  - `Assets/Editor/Character/Action/Timeline/CommittedActionTimelineEditorAdapters.cs`
  - 新增 Editor-only preview session / binding / resolver 文件
  - `Assets/Tests/Editor/Character/Action/Timeline/CommittedActionTimelineEditorAdapterTests.cs`

## Dependencies / Conflicts
- `formalize-committed-action-branch-editor-workflow` 已归档并合入 current specs；本变更以前置口径“独立 Timeline Window 定位 TimelineNode”为准。
- 本变更不修改正式 `ActionTimelineDefinition`、`ActionTimelineEvaluator`、`CharacterFramePipeline`、motion executor、Animancer presenter runtime 或 blackboard 写入路径。

## Out of Scope
- 不复制 Ref/Taco `TimelinePlayer`、`Timeline`、`TreeClip`、`TimelineRunningTree` 或 `RunnableNode` 作为正式 runtime。
- 不让 Timeline Editor 在 PlayMode 接管正在运行的角色 gameplay。
- 不播放正式 VFX/SFX/camera cue。
- 不真实执行 motion executor 或改变角色 gameplay transform。
- 不新增 fallback scene 查找、Resources 查找或代码内置动画 key。

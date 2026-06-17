# Change: 建立 Character Graph 合同与 Action Timeline 首个节点

## Why
当前 Action 已有 Catalog、request resolver、lifecycle、motion resolver、body claim、frame submission 和 runtime blackboard，但角色行为编辑视图还没有统一的图合同。继续只做 ActionTimeline 会把未来技能编辑器带窄；直接把完整节点编辑器作为 gameplay runtime 又会让节点图、timeline、黑板和管线之间产生新的运行路径。

本变更先建立 `CharacterGraphDefinition` 的顶层编辑器资产合同，并规定第一版正式 runtime 必须编译为 `CharacterExecutionNodeTree` 节点树执行结构。编辑器可以用图统一组织 Locomotion、Action、UpperBody 和 Cue 分支；运行时节点树允许受控并行分支同帧评估，各分支只产出候选输出、claim 或 outcome，`CharacterFramePipeline` 继续负责仲裁、应用和写入 facts。

## What Changes
- 新增 `CharacterGraphDefinition` / 分支端口 / graph frame result 或等价纯数据合同，表达统一编辑器的大图结构。
- 新增 `CharacterExecutionNodeTree` 或等价运行时节点树合同，约束 runtime node 单父、有序/并行 composite、输入向下、输出向上、状态归节点，不运行任意有环图。
- 明确 Locomotion、Action、UpperBody、Cue 分支第一版先有端口合同；其中只有 Action 分支的 `TimelineNode` 进入第一版实现范围。
- 新增 `ActionBranchDefinition` / `ActionNodeDefinition` / `TimelineNode` 或等价 Action 分支合同。
- 新增 `ActionTimelineDefinition` / `ActionTimelineTrackDefinition` / `ActionTimelineClipDefinition` 或等价纯数据模型，作为 `TimelineNode` 内部时序数据。
- 新增 `ActionTimelineEvaluator` 或等价评估模块，将 active action 的权威 frame、source step 和 timeline definition 转换为本帧 `ActionTimelineOutcome`。
- 明确 Outcome 与 Fact 边界：CharacterExecutionNodeTree 和 ActionTimeline 不直接写 `CharacterRuntimeBlackboard`，只通过候选输出、claim、`CharacterFrameSubmission` 或批准的 frame plan 输入进入管线。
- 将 `ActionLifecycleRuntime` 规划为 Action 分支的运行时推进位置，但不让它成为第二个角色帧 runner。
- 将 Action Catalog 规划为 ActionBranch/Timeline 定义的正式装配入口，缺失配置不得使用隐藏 fallback。
- 为后续节点编辑器定义 authoring 合同：节点图只能校验/编译为 CharacterGraphDefinition / CharacterExecutionNodeTree，不直接驱动 Animator、Transform、Prefab、Particle、黑板或角色帧管线。
- 参考 `Ref/wly970123` 的节点资产、Track/Clip 和 TreeClip 思路，但不接入其 `TreeRunner` / `TimelinePlayer` 作为正式 runtime。

## Non-Goals
- 不实现完整节点编辑器 UI。
- 不实现轻攻击连招、行为树、HitReact、Jump 或多段 Combo。
- 不实现运行时任意有环图、共享 runtime node、隐式合流节点或跨分支直接写状态；受控 parallel/composite node 属于本变更合同。
- 不新增 hitbox/hurtbox 物理判定执行、伤害结算、命中停顿、VFX/SFX/Camera 真实播放。
- 不引入 `TreeRunner`、`TimelinePlayer`、Unity PlayableGraph、Animator Controller 或 MonoBehaviour Update 作为新正式动作运行时。
- 不重定义 `StateTimelinePolicy`、状态请求窗口或 interrupt policy；这些继续归属已有/活跃变更。
- 不新增 fallback 配置、Resources 查找、场景查找或代码默认 timeline。

## Impact
- Affected specs:
  - `character-graph-contracts`
  - `action-timeline-framework`
  - related: `character-action-catalog`
  - related: `character-frame-pipeline`
  - related: `character-runtime-blackboard`
  - related: `action-domain-runtime`
  - related: `character-state-graph-runtime`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Model/*`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Config/*`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Runtime/*`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Solver/*`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/Model/*`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/Solver/*`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/*`
- Related specs / changes:
  - `state-timeline-policy` 负责状态 timeline window / request policy 数据边界，本变更不得复制其职责。
  - `action-domain-runtime` 负责 Action lifecycle、body/channel claim 和 Action 候选输出边界。
  - `character-frame-pipeline` 负责角色帧候选收集、仲裁、输出应用和 facts 写入。


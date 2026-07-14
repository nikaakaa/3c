# Change: 恢复 TreeClip 管线运行时并接入决策黑板

## Why

早期 BTSMTL Timeline 通过 `Timeline.Evaluate(deltaTime)` 依次推进 Track 和 Clip，`TreeClip` 因而能够驱动 `TimelineRunningTree.UpdateTree(deltaTime)`。Timeline 播放权威迁移到 `TimelinePlaybackScheduler` 后，`TimelineNode` 改为只提交播放请求，Scheduler 只显式采样 Animation、Motion、Window、Cue 和 Camera 等轨道，没有迁移 `TreeTrack`。current spec 仍要求 TreeClip 能运行，因此当前实现已经违反 `btsmtl-runnable-timeline-node` 的现行要求。

与此同时，Pipeline Blackboard 已经把 ExposedProperty 收口为正式变量表面，但现有 TreeClip 仍使用 `TreeInstance.InitTree(this)`，Tree 的 `User` 是 TreeClip 而不是 `CharacterGraphContext`，无法通过 `IPipelineBlackboardRuntimeAccess` 读写当前管线黑板。Timeline 中“在某个时间段开放状态切换”的本地决策只能继续被建模为 ActionWindow，导致本地状态门和需要 ActionInstance、网络策略、combat debug 的动作窗口混在一起。

本变更恢复 TreeClip，但不恢复旧 `Timeline.Evaluate()` 自主播放路径。TreeClip 必须进入现有 Scheduler 唯一权威，并显式区分同 Tick 决策与决策后的持续行为。

## What Changes

- 将 TreeTrack/TreeClip 正式迁入 `TimelinePlaybackScheduler` active playback record，恢复 Timeline 驱动 Tree 的 current spec 能力。
- 为 TreeClip 增加显式 `Decision` 与 `Commit` 执行阶段：Decision 在 RootTree/StateMachine 求值前执行，Commit 在状态决策后执行。
- Decision TreeClip 每个 logic tick 以无跨 Tick Running 状态的方式执行一次，只允许纯读取、条件组合和声明式 Pipeline Blackboard 写入；禁止提交 Action、Motion、Cue、Camera、GameplayResult 或场景副作用。
- Commit TreeClip 保留 Enter、持续 Tick、Exit、Destroy 和 Runnable stop 生命周期，但其输出不能反向影响已经完成的同 Tick Transition。
- 将 TimelineRunningTree 的 Clip 绑定与 Graph `User` 解耦：Clip runtime context 提供时间和范围，正式 `CharacterGraphContext` 继续作为 Graph User。
- 让 TreeClip 默认拥有 Timeline 资产内的 inline `TimelineRunningTree`，需要复用时才显式 Extract Shared；删除旧 `TreeAsset + TreeInstance` 双字段和一次性 Tree asset 默认路径。
- 让 Decision TreeClip 通过 ExposedProperty 声明 Frame scope/lifetime 黑板变量，并由 ConditionRuleGraph 的纯 ValueNode 同 Tick读取。
- 将 Corin `DodgeMoveToRun` 从 Action CancelWindow 迁移为 Decision TreeClip 输出的 `CanDodgeMoveCancel` Frame Blackboard gate。
- 删除 Dodge Timeline 中旧 `Cancel/DodgeMoveToRun` clip、Dodge ActionProfile Cancel window policy 和对应 ActionWindow reader，不保留双写或兼容读取。
- 保留 IFrame、Hit、Parry、Armor 和攻击连招 CancelWindow 等需要动作身份、策略解析和同步/debug 身份的 ActionWindow。

## Impact

- 受影响规格：
  - `btsmtl-runnable-timeline-node`
  - `btsmtl-graph-core`
  - `btsmtl-timeline-editor-preview`
  - `character-animation-pipeline`
  - `character-pipeline-blackboard`
  - `character-state-interruption-authoring`
  - `character-action-authoring-closure`
- 受影响实现：
  - TreeTrack、TreeClip、TimelineRunningTree 数据与生命周期
  - TimelinePlaybackScheduler active/loop/cancel runtime
  - Pipeline Blackboard declaration、Frame lifetime 和 Decision 写入
  - Timeline Editor 的 TreeClip inline/shared 下钻与阶段 UI
  - Graph/Timeline validator
  - Corin Dodge Timeline、ActionProfile、Transition 和 OnExit 条件
- 当前 active change `fix-corin-action-lifecycle-and-dodge-interruption` 与本变更存在明确交叉：它新增 `DodgeMoveToRun` ActionWindow，本变更依赖其完整 Action Exit lifecycle 和 DNF 条件能力，但最终会迁移并删除该 Dodge ActionWindow 路径。两者必须串行 apply，不能并行修改同一 Corin 资产。
- current spec 与实现存在现行矛盾：spec 要求 TreeClip 继续运行，实现却没有 Scheduler 入口。本变更修复实现，不删除该 requirement。
- 不恢复 `TimelineNode -> Timeline.Evaluate()`、TimelinePlayer autonomous tick 或第二套 Timeline 播放器。
- 不新增 fallback、兼容字段、一次性 SubTree/TimelineRunningTree asset 或并行黑板。
- 不新增测试，不运行 Unity batchmode。

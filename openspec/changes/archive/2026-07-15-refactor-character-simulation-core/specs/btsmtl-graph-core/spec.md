# btsmtl-graph-core Specification

## MODIFIED Requirements

### Requirement: BaseGraph 承载运行上下文但不承担执行生命周期

系统 MUST 允许 `BaseGraph` 保存非序列化运行上下文，包括 `User`、`DeltaTime` 和类型化上下文读取能力。`BaseGraph` MUST NOT 拥有 `Running`、`State`、`UpdateTree` 或 `ResetTree`。通用 BTSMTL 解释器 MAY 从 resolved authoring graph data 创建隔离运行工作副本，但正式 Character runtime MUST 将同一 authoring 编译为 `CharacterSimulationProgram`，不得通过 `RunnableTree`、`StateMachineGraphRuntime` 或运行时 Graph clone 执行角色 Gameplay。两种用途 MUST 不共享或回写运行状态。

#### Scenario: 非角色通用 RunnableTree tick

- **WHEN** 非 Character 组合显式调用 `RunnableTree.UpdateTree(deltaTime)`
- **THEN** 它 MUST 将 `deltaTime` 写入自己的隔离 `BaseGraph` 运行上下文
- **AND** 节点执行生命周期 MUST 仍由该通用 `RunnableTree` 表达
- **AND** MUST 不读取或修改 CharacterSimulationState

#### Scenario: 非角色通用 StateMachineGraphRuntime tick

- **WHEN** 非 Character 工具显式使用 `StateMachineGraphRuntime.Update(deltaTime)` 解释 `StateMachineGraph`
- **THEN** 它 MUST 将 `deltaTime` 写入隔离运行工作副本的 `BaseGraph.DeltaTime`
- **AND** 它 MUST NOT 要求 `StateMachineGraph` 继承 `RunnableTree`
- **AND** MUST 不成为 Character runtime fallback

#### Scenario: Character 正式运行

- **WHEN** CharacterPipelineDefinition 已生成有效 Program artifact
- **THEN** SimulationSessionRuntime MUST 只执行 Program operation
- **AND** MUST 不创建 BaseGraph 运行工作副本或调用通用解释器

#### Scenario: 子 Graph 继承上下文

- **WHEN** 非 Character 通用解释器初始化下钻 Graph 运行工作副本
- **THEN** 子 Graph MUST 接收父 Graph 的正式 `User`
- **AND** 系统 MUST NOT 使用父节点、父 Graph 或 runner 自身作为 fallback 上下文

### Requirement: TreeClip 私有下钻 Graph 必须默认 inline

Timeline TreeClip 作为拥有下钻 Graph 的 authoring owner 时，编辑器 MUST 自动创建并保存 inline `TimelineRunningTree` graph data。作者需要复用时 MAY 显式 Extract Shared 到 `BaseTreeAsset`。Inline 与 shared MUST 共享同一 resolved authoring graph 合同，并且同一 TreeClip 只能有一个真数据来源。`TimelineRunningTree` 在正式 Character runtime 中 MUST 只作为 Compiler 输入，不能被克隆为 playback runtime。

#### Scenario: 新建 TreeClip

- **WHEN** 作者在 Timeline 中创建 TreeClip
- **THEN** Clip MUST 自动拥有 inline TimelineRunningTree authoring data
- **AND** 作者 MUST 能通过双击或 Open 下钻编辑
- **AND** 创建流程 MUST NOT 弹出或要求分配 BaseTreeAsset

#### Scenario: 抽取 shared Tree

- **WHEN** 作者对 inline TreeClip 执行 Extract Shared
- **THEN** 系统 MUST 创建持有同一 Graph data 的 shared BaseTreeAsset
- **AND** TreeClip MUST 切换到 shared 引用
- **AND** 原 inline 真数据 MUST 被清理

#### Scenario: 多 playback 使用同一 TreeClip

- **WHEN** 多个 Timeline playback 使用同一 inline 或 shared TimelineRunningTree authoring template
- **THEN** Compiler MUST 让它们引用同一不可变 operation/catalog 数据
- **AND** 每个 playback/clip activation MUST 在 CharacterSimulationState 中获得独立 state address
- **AND** 系统 MUST 不创建 TimelineRunningTree runtime clone

### Requirement: Graph 运行时初始化必须收敛到统一非虚入口

明确保留的非 Character 通用解释器 MAY 通过 `BaseGraph` 公开非虚入口完成 root/nested route、runtime identity、节点、边和通用上下文初始化。正式 Character runtime MUST 不调用该入口；Character Graph、StateMachine 与 Timeline TreeClip 必须由 Compiler 解析为 Program operation。`TimelineRunningTree` MUST 不再提供 Character gameplay 专用运行时初始化入口。

#### Scenario: 初始化非 Character 嵌套 Graph

- **WHEN** 明确装配的通用解释器初始化子 Graph
- **THEN** 统一入口 MUST 先建立 parent/route
- **AND** 派生节点引用 MUST 在核心 maps 建立后解析
- **AND** 该工作副本 MUST 与 Character state 隔离

#### Scenario: 编译 Character Timeline TreeClip

- **WHEN** Compiler 解析 TimelineRunningTree authoring data
- **THEN** Compiler MUST 校验 TreeClip owner、clip identity、Blackboard reference 与 operation emitter
- **AND** MUST 不调用 `InitTimelineTree` 或普通 `InitTree`

#### Scenario: 尝试运行时初始化 Character TreeClip

- **WHEN** Character runtime 尝试创建或初始化 TimelineRunningTree 工作副本
- **THEN** 组合或编译校验 MUST 明确失败
- **AND** 系统 MUST 不创建半初始化 Graph 或 fallback context

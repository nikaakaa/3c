## RENAMED Requirements

- FROM: `### Requirement: Graph 运行工作副本来自数据克隆`
- TO: `### Requirement: 正式技能运行必须共享定义并隔离实例状态`

## MODIFIED Requirements

### Requirement: BaseGraph 承载运行上下文但不承担执行生命周期

系统 MUST允许 `BaseGraph` 保存非序列化运行上下文，包括 `User`、`DeltaTime`和类型化上下文读取能力。`BaseGraph` MUST NOT拥有 `Running`、`State`、`UpdateTree`或 `ResetTree`。通用 BTSMTL解释器 MAY从 resolved authoring graph data创建隔离运行工作副本，但正式Character runtime MUST将技能authoring编译为SkillProgram并与C#控制合同组装为CharacterSimulationProgram，并由 Session Pipeline的标准 Program Step Pass执行，不得通过 `RunnableTree`、`StateMachineGraphRuntime`或运行时 Graph clone执行角色 Gameplay。两种用途 MUST不共享或回写运行状态。

#### Scenario: Character 正式运行

- **WHEN** CharacterPipelineDefinition已生成有效 Program artifact且 Session Pipeline进入 Active
- **THEN** Program Evaluate/Finalize Pass MUST在同一事务执行代码控制与compiled技能operation
- **AND** MUST不创建 BaseGraph运行工作副本或调用通用解释器

#### Scenario: 非角色通用 RunnableTree tick

- **WHEN** 非 Character组合显式调用 `RunnableTree.UpdateTree(deltaTime)`
- **THEN** 它 MUST将 `deltaTime`写入自己的隔离 `BaseGraph`运行上下文
- **AND** MUST不读取 CharacterSimulationState或注册 Character Session Pipeline Pass


### Requirement: 正式技能运行必须共享定义并隔离实例状态

正式Character／Skill Runtime MUST共享只读compiled图定义，并为Actor、ActionInstance和调用点建立隔离typed状态，不创建作者Graph数据克隆。只有明确装配的非Character通用解释器用途可以按其既有合同创建工作副本，该副本不得进入角色／技能主链。

#### Scenario: 多个释放引用共享图

- **WHEN** 两个ActionInstance运行同一个共享子图
- **THEN** MUST共享定义并隔离参数、节点和Timeline状态
- **AND** MUST不把作者数据当可变运行状态

#### Scenario: inline技能图运行

- **WHEN** 技能使用owner持有的inline数据
- **THEN** MUST运行其正式编译产物，不能直接更新作者字段

#### Scenario: 多个运行实例引用同一 shared graph

- **WHEN** 两个角色或释放同时使用同一共享技能子图
- **THEN** MUST共享只读Program并建立独立typed调用状态
- **AND** 节点、局部参数和播放进度不得相互污染

#### Scenario: inline graph 运行

- **WHEN** 技能执行自己的inline作者内容
- **THEN** Runtime MUST只读取对应编译定义和实例状态
- **AND** MUST不创建作者Graph工作副本或改写序列化字段


### Requirement: Graph 运行时初始化必须收敛到统一非虚入口

明确保留的非 Character 通用解释器 MAY 通过 `BaseGraph` 公开非虚入口完成 root/nested route、runtime identity、节点、边和通用上下文初始化。正式 Character runtime MUST 不调用该入口；技能Graph、局部StateMachine与Timeline TreeClip必须由Compiler解析为Program operation；角色级控制不调用Graph初始化入口。`TimelineRunningTree` MUST 不再提供 Character gameplay 专用运行时初始化入口。

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

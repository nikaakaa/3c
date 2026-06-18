## MODIFIED Requirements

### Requirement: CharacterBehaviorGraphDefinition 顶层资产合同
系统 MUST 提供 `CharacterBehaviorGraphDefinition` 或等价顶层纯数据资产合同，用于表达角色编辑器中的 source topology。该定义 MAY 包含 schema version、stable id、source node、port、edge、editor position、source reference 和子图引用，但 MUST NOT 保存 Dodge selector、Directional timeline、Backstep timeline、track、clip、motion payload、animation key、window、cue 或其它 Action timeline payload 作为正式运行时数据源。该定义 MUST NOT 自行执行 motion、animation、input consume、blackboard write 或 Unity 场景副作用。

#### Scenario: 顶层资产保存 Source 拓扑
- **WHEN** 工具层读取 CharacterBehaviorGraphDefinition
- **THEN** 定义 MUST 能表达 Locomotion source leaf 或等价 Locomotion source node
- **AND** MUST 能表达 CommittedAction source leaf 或等价 Action source node
- **AND** MUST 能表达 node port、edge 和 editor position
- **AND** MAY 表达 UpperBody、Cue 或后续已审批 source port
- **AND** 定义 MUST NOT 持有 `Transform`、`CharacterController`、`Animator`、Animancer runtime object、`InputAction` 或 `MonoBehaviour`

#### Scenario: 顶层资产不保存 Action Timeline
- **WHEN** 检查 CharacterBehaviorGraphDefinition 的正式 schema
- **THEN** schema MUST NOT 把 Dodge selector、Directional timeline、Backstep timeline、track、clip、motion payload、animation key、window 或 cue 定义为正式字段
- **AND** legacy embedded branch 或 legacy timeline 字段 MUST NOT 被正式 compiler 当作 fallback 消费

#### Scenario: 顶层资产不作为 gameplay runner
- **WHEN** 正式 gameplay 处理 CharacterBehaviorGraphDefinition
- **THEN** 系统 MUST 先校验或编译该定义
- **AND** MUST NOT 直接运行 CharacterBehaviorGraphDefinition 的任意节点边
- **AND** MUST NOT 直接移动角色
- **AND** MUST NOT 直接播放动画
- **AND** MUST NOT 直接写 `CharacterRuntimeBlackboard`

### Requirement: 分支端口合同
CharacterBehaviorExecutionTree MUST 将 Locomotion、Action、UpperBody 和 Cue 表达为明确 source port 或等价合同。端口合同 MUST 先于完整分支 implementation 存在，使编辑器、编译器和角色帧管线能共享同一套候选输出语义。第一版 MAY 只让 Action source 通过正式 ActionDefinition 解析出 CommittedActionBranch / TimelineNode，但 Behavior Graph port 本身不得保存 Action timeline payload。

#### Scenario: Locomotion 端口输出移动候选
- **WHEN** Locomotion 分支端口被 runtime 评估
- **THEN** 它 MUST 输出 `LocomotionCandidate` 或等价纯数据候选
- **AND** 候选 MAY 包含移动意图、基础动画意图、移动 facts 和 source step
- **AND** Locomotion 分支 MUST NOT 直接执行运动或播放动画

#### Scenario: Action 端口输出动作候选
- **WHEN** Action 分支端口被 runtime 评估
- **THEN** 它 MUST 输出 `ActionOutcome`、body/channel claim 或等价纯数据候选
- **AND** 候选 MAY 包含 motion intent、animation intent、hitbox/cancel facts、cue request 和 source step
- **AND** Action 分支 MUST NOT 直接执行运动、播放动画或写黑板
- **AND** Action timeline payload MUST 来自正式 ActionDefinition，而不是 Behavior Graph port

#### Scenario: UpperBody 和 Cue 端口有正式语义
- **WHEN** 第一版未实现 UpperBody 或 Cue runtime
- **THEN** CharacterBehaviorExecutionTree 合同仍 MUST 命名这些端口的输出语义
- **AND** UpperBody MUST 表达上半身候选或 channel claim
- **AND** Cue MUST 表达纯数据表现请求
- **AND** 系统 MUST NOT 通过空字符串、临时 tag 或隐式约定表达这些端口

### Requirement: CommittedActionBranch 作为第一实现分支
第一版 implementation MUST 将 `CommittedActionBranch` 作为 CharacterBehaviorExecutionTree 中的第一个可执行 Action 分支，并将 `TimelineNode` 作为第一种具体 Action 节点。CommittedActionBranch、selector、TimelineNode 和 timeline payload MUST 由正式 ActionDefinition、action catalog 或批准的等价 Action 数据源编译得到；Behavior Graph 只能引用或定位该 Action source，不得复制或保存其 timeline 数据。Locomotion、UpperBody 和 Cue 分支端口第一版 MAY 只定义合同和静态验证，不要求实现完整 runtime。

#### Scenario: Dodge 使用 ActionDefinition 的 CommittedActionBranch TimelineNode
- **GIVEN** Dodge 是第一版 concrete action
- **AND** 正式 `CharacterActionDefinitionSO` 或等价 ActionDefinition 包含 Dodge selector、Directional timeline 和 Backstep timeline
- **WHEN** runtime 构建 Dodge 的 CommittedActionBranch
- **THEN** Dodge MAY 被表达为 selector 加 TimelineNode 的 CommittedActionBranch
- **AND** TimelineNode MUST 产出与现有 Dodge 行为等价的 outcome
- **AND** CommittedActionBranch 抽象模型 MUST NOT 引用 Dodge 专用类型
- **AND** Behavior Graph MUST NOT 提供第二份 Dodge selector 或 timeline payload

#### Scenario: 缺少 ActionDefinition 不产生隐藏 branch
- **GIVEN** Behavior Graph 包含 CommittedAction source leaf
- **AND** runtime composition 缺少正式 ActionDefinition 或 action catalog reference
- **WHEN** 管线处理 CharacterBehaviorExecutionTree
- **THEN** 系统 MUST 报告正式配置错误或空候选诊断
- **AND** MUST NOT 通过 legacy embedded branch、Behavior/Samples、Resources、代码默认 branch、fallback runner、场景查找或临时 MonoBehaviour 继续运行

#### Scenario: 未实现端口不产生 fallback
- **GIVEN** UpperBody 或 Cue 分支端口第一版未实现正式 runtime
- **WHEN** 管线处理 CharacterBehaviorExecutionTree
- **THEN** 系统 MUST 使用明确的空候选或未实现诊断
- **AND** MUST NOT 通过 fallback runner、场景查找、Resources 或临时 MonoBehaviour 继续运行

### Requirement: CharacterBehaviorGraph / ExecutionTree 可测试和可验证
系统 MUST 为 CharacterBehaviorGraphDefinition 和 CharacterBehaviorExecutionTree 合同提供自动测试和静态边界验证，证明顶层资产、运行时节点树、source port、body/channel claim、ActionDefinition 数据归属和管线接入不引入第二角色帧主线或第二 Action timeline 数据源。

#### Scenario: 自动测试覆盖合同
- **WHEN** 运行 CharacterBehaviorGraph / ExecutionTree EditMode 测试
- **THEN** 测试 MUST 覆盖空 CharacterBehaviorGraphDefinition
- **AND** MUST 覆盖 CharacterBehaviorExecutionTree 单父 runtime node 约束
- **AND** MUST 覆盖并行 composite 同帧汇总多个 source 候选
- **AND** MUST 覆盖 Locomotion、Action、UpperBody、Cue 端口默认候选语义
- **AND** MUST 覆盖 CommittedActionBranch TimelineNode 作为第一 concrete Action 分支

#### Scenario: Graph 不拥有 Timeline 的测试
- **WHEN** 运行 CharacterBehaviorGraph / ExecutionTree EditMode 测试
- **THEN** 测试 MUST 覆盖 Graph compiler 不输出 `ActionTimelineDefinition`
- **AND** MUST 覆盖 Graph Editor 保存只影响 source topology 或 editor position
- **AND** MUST 覆盖 Graph Editor 保存不修改 `ActionTimelineTrackAuthoring`、`ActionTimelineClipAuthoring` 或 action timeline payload
- **AND** MUST 覆盖缺少正式 ActionDefinition 时报告配置错误或空候选诊断

#### Scenario: 静态边界验证
- **WHEN** 运行边界测试或静态搜索
- **THEN** 验证 MUST 确认 CharacterBehaviorExecutionTree runtime 不引用 `TreeRunner`
- **AND** MUST 确认 CharacterBehaviorExecutionTree runtime 不引用 `TimelinePlayer`
- **AND** MUST 确认 CharacterBehaviorExecutionTree runtime 不直接调用 motion executor、animation presenter 或 blackboard writer
- **AND** MUST 确认 Behavior Graph 正式 schema 不把 Dodge selector、timeline、track、clip 或 payload 暴露为正式数据源

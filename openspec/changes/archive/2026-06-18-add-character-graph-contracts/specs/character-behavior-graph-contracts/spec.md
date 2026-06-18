## ADDED Requirements

### Requirement: CharacterBehaviorGraphDefinition 顶层资产合同
系统 MUST 提供 `CharacterBehaviorGraphDefinition` 或等价顶层纯数据资产合同，用于表达角色编辑器中的统一大图。该定义 MAY 包含节点、边、端口、分支引用、子图引用和 timeline authoring 信息，但 MUST NOT 自行执行 motion、animation、input consume、blackboard write 或 Unity 场景副作用。

#### Scenario: 顶层资产保存分支端口
- **WHEN** 工具层读取 CharacterBehaviorGraphDefinition
- **THEN** 定义 MUST 能表达 Locomotion 分支端口
- **AND** MUST 能表达 Action 分支端口
- **AND** MAY 表达 UpperBody、Cue 或后续分支端口
- **AND** 定义 MUST NOT 持有 `Transform`、`CharacterController`、`Animator`、Animancer runtime object、`InputAction` 或 `MonoBehaviour`

#### Scenario: 顶层资产不作为 gameplay runner
- **WHEN** 正式 gameplay 处理 CharacterBehaviorGraphDefinition
- **THEN** 系统 MUST 先校验或编译该定义
- **AND** MUST NOT 直接运行 CharacterBehaviorGraphDefinition 的任意节点边
- **AND** MUST NOT 直接移动角色
- **AND** MUST NOT 直接播放动画
- **AND** MUST NOT 直接写 `CharacterRuntimeBlackboard`

### Requirement: CharacterBehaviorExecutionTree 运行时节点树合同
系统 MUST 将正式 gameplay 第一版建模为 `CharacterBehaviorExecutionTree` 或等价节点树运行时合同。该节点树 MUST 由 CharacterBehaviorGraphDefinition 或批准的等价配置编译得到，并 MUST 使用单父 runtime node、受控有序/并行 composite、输入向下传递、输出向上汇总和节点自有 state 的执行语义。

#### Scenario: 编译结果是节点树执行结构
- **GIVEN** 一个合法 CharacterBehaviorGraphDefinition
- **WHEN** compiler 生成正式 runtime model
- **THEN** 结果 MUST 是 CharacterBehaviorExecutionTree 或等价节点树结构
- **AND** 每个 runtime node MUST 至多有一个父节点
- **AND** runtime MUST NOT 生成共享 runtime node、隐式合流节点或循环边

#### Scenario: 输入向下输出向上
- **WHEN** CharacterBehaviorExecutionTree 评估一帧
- **THEN** root MUST 将只读输入传递给子分支或子节点
- **AND** 子分支或子节点 MUST 将候选输出、claim、outcome 和 diagnostics 汇总回父节点
- **AND** 节点 MUST NOT 通过跨分支引用直接写入其它节点 state

#### Scenario: 并行分支同帧评估
- **GIVEN** CharacterBehaviorExecutionTree 包含 ParallelBranch、ParallelComposite 或批准的等价并行节点
- **WHEN** runtime 评估该节点
- **THEN** 它 MAY 在同一角色帧评估 Locomotion、Action、UpperBody 或 Cue 子分支
- **AND** 每个子分支 MUST 只返回纯数据候选输出、claim、outcome 或 diagnostics
- **AND** 最终互斥输出 MUST 仍由 `CharacterFramePipeline` 计划和应用

#### Scenario: 运行时节点树不执行副作用
- **WHEN** runtime 评估 CharacterBehaviorExecutionTree 或其节点
- **THEN** 运行时节点树 MUST 只产出纯数据候选输出、claim 或 outcome
- **AND** MUST NOT 直接移动角色
- **AND** MUST NOT 直接播放动画
- **AND** MUST NOT 直接写 `CharacterRuntimeBlackboard`

### Requirement: 分支端口合同
CharacterBehaviorExecutionTree MUST 将 Locomotion、Action、UpperBody 和 Cue 表达为明确分支端口或等价合同。端口合同 MUST 先于完整分支 implementation 存在，使编辑器、编译器和角色帧管线能共享同一套候选输出语义。第一版 MAY 只实现 Action 分支的 TimelineNode，但其它端口不得是未命名占位。

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

#### Scenario: UpperBody 和 Cue 端口有正式语义
- **WHEN** 第一版未实现 UpperBody 或 Cue runtime
- **THEN** CharacterBehaviorExecutionTree 合同仍 MUST 命名这些端口的输出语义
- **AND** UpperBody MUST 表达上半身候选或 channel claim
- **AND** Cue MUST 表达纯数据表现请求
- **AND** 系统 MUST NOT 通过空字符串、临时 tag 或隐式约定表达这些端口

### Requirement: Body Channel 与行为模块分离
系统 MUST 区分行为模块和身体输出通道。Locomotion、Action、Aim 或 HitReact 是行为模块或分支；FullBody、UpperBody、LowerBody、Additive 等是 body/channel claim 语义。CharacterBehaviorExecutionTree 和管线 MUST NOT 把 FullBody 或 UpperBody 当成 gameplay owner。

#### Scenario: Action claim FullBody
- **GIVEN** Action 分支输出 Dodge 或等价全身动作 outcome
- **WHEN** 该动作需要占用全身输出
- **THEN** Action 分支 MUST 输出 FullBody claim 或等价 body claim
- **AND** FullBody MUST 表示输出通道占用
- **AND** FullBody MUST NOT 成为独立 gameplay 状态机 owner

#### Scenario: Locomotion 不是单纯槽位
- **WHEN** Locomotion 分支输出 Run、TurnBack 或等价基础移动候选
- **THEN** Locomotion MUST 被视为基础移动行为模块
- **AND** 它 MAY claim base movement、lower body 或基础动画通道
- **AND** 它 MUST NOT 被建模为 Action 分支里的普通节点

### Requirement: CharacterBehaviorExecutionTree 接入角色帧管线
CharacterBehaviorExecutionTree 的 runtime 输出 MUST 通过角色帧候选、claim、frame plan input、`CharacterFrameSubmission` 或批准的等价合同进入 `CharacterFramePipeline`。`CharacterFramePipeline` MUST 继续只负责 tick 顺序、候选收集、plan/arbiter、output apply 和 facts 写入，不得解释节点图内部结构。

#### Scenario: 管线只消费候选输出
- **GIVEN** CharacterBehaviorExecutionTree 已经评估出 LocomotionCandidate 和 ActionOutcome
- **WHEN** `CharacterFramePipeline` 构建本帧 plan
- **THEN** pipeline MUST 只消费这些纯数据候选和 claim
- **AND** pipeline MUST NOT 读取 CommittedActionBranch 节点类型
- **AND** pipeline MUST NOT 遍历 CharacterBehaviorGraphDefinition 节点边

#### Scenario: 副作用仍由 output applier 执行
- **GIVEN** FramePlan 选择了某个 Action motion 和 animation outcome
- **WHEN** 角色帧管线应用输出
- **THEN** motion executor 调用 MUST 仍发生在正式 output applier
- **AND** animation presenter 调用 MUST 仍发生在正式 output applier
- **AND** blackboard facts 写入 MUST 仍发生在正式 facts writer

### Requirement: CommittedActionBranch 作为第一实现分支
第一版 implementation MUST 将 `CommittedActionBranch` 作为 CharacterBehaviorExecutionTree 中的第一个可执行分支，并将 `TimelineNode` 作为第一种具体 Action 节点。Locomotion、UpperBody 和 Cue 分支端口第一版 MAY 只定义合同和静态验证，不要求实现完整 runtime。

#### Scenario: Dodge 使用 CommittedActionBranch TimelineNode
- **GIVEN** Dodge 是第一版 concrete action
- **WHEN** runtime 构建 Dodge 的 CommittedActionBranch
- **THEN** Dodge MAY 被表达为只有一个 TimelineNode 的 CommittedActionBranch
- **AND** 该 TimelineNode MUST 产出与现有 Dodge 行为等价的 outcome
- **AND** CommittedActionBranch 抽象模型 MUST NOT 引用 Dodge 专用类型

#### Scenario: 未实现端口不产生 fallback
- **GIVEN** UpperBody 或 Cue 分支端口第一版未实现正式 runtime
- **WHEN** 管线处理 CharacterBehaviorExecutionTree
- **THEN** 系统 MUST 使用明确的空候选或未实现诊断
- **AND** MUST NOT 通过 fallback runner、场景查找、Resources 或临时 MonoBehaviour 继续运行

### Requirement: CharacterBehaviorGraph / ExecutionTree 可测试和可验证
系统 MUST 为 CharacterBehaviorGraphDefinition 和 CharacterBehaviorExecutionTree 合同提供自动测试和静态边界验证，证明顶层资产、运行时节点树、分支端口、body/channel claim 和管线接入不引入第二角色帧主线。

#### Scenario: 自动测试覆盖合同
- **WHEN** 运行 CharacterBehaviorGraph / ExecutionTree EditMode 测试
- **THEN** 测试 MUST 覆盖空 CharacterBehaviorGraphDefinition
- **AND** MUST 覆盖 CharacterBehaviorExecutionTree 单父 runtime node 约束
- **AND** MUST 覆盖并行 composite 同帧汇总多个分支候选
- **AND** MUST 覆盖 Locomotion、Action、UpperBody、Cue 端口默认候选语义
- **AND** MUST 覆盖 CommittedActionBranch TimelineNode 作为第一 concrete 分支

#### Scenario: 静态边界验证
- **WHEN** 运行边界测试或静态搜索
- **THEN** 验证 MUST 确认 CharacterBehaviorExecutionTree runtime 不引用 `TreeRunner`
- **AND** MUST 确认 CharacterBehaviorExecutionTree runtime 不引用 `TimelinePlayer`
- **AND** MUST 确认 CharacterBehaviorExecutionTree runtime 不直接调用 motion executor、animation presenter 或 blackboard writer


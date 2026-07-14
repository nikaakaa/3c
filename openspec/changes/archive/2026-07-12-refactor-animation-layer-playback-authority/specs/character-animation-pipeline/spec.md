## MODIFIED Requirements

### Requirement: 动画混合模型是运行时核心

系统 MUST 使用 Contribution Registry、`CharacterAnimationLayerArbitrator`、持久 `CharacterAnimationLayerRuntime` 与 `CharacterPresentationStage` 组成动画混合模型。Registry 管 producer lifecycle；Arbitrator 消费完整有序 lifecycle records并为每层生成唯一 `AnimationLayerPlan`；LayerRuntime 执行 plan并保存 FinalOutput、HeldOutput 与 ActiveHandoff；PresentationStage 组织单一表现 commit。任意 producer MUST NOT绕过该模型应用动画。

#### Scenario: 多来源写入同一 layer

- **WHEN** Locomotion 与 Action contributions 同时写入 Base
- **THEN** Arbitrator MUST 生成一个 Base DesiredCandidate 与一个 Base LayerPlan
- **AND** LayerRuntime MUST 生成一个 Base FinalOutput
- **AND** 系统 MUST NOT按 StateMachine 分别推进 Base transition

#### Scenario: producer 当帧未提交

- **WHEN** producer 本表现帧没有新 Sample
- **THEN** Registry MUST 依据显式 lifecycle 判断 membership
- **AND** Presenter MUST NOT依据 transient submission list 自行释放 state

### Requirement: CharacterPresentationStage 是 Unity 动画应用边界

`CharacterPresentationStage` MUST 是 lifecycle command queue、Registry、Arbitrator、LayerRuntime、Presenter 与 output job 的聚合边界。Stage MUST 在同一表现帧保留完整 command order，先完成 Registry snapshot，再由 Arbitrator 一次生成全部 LayerPlans，最后让 Runtime 与 Presenter 应用一次最终 layer outputs。Timeline、StateMachine、Registry、Arbitrator 与 LayerRuntime MUST NOT绕过 Stage 直接写 Animator/Animancer。

#### Scenario: target sample 与 Driver 同批

- **WHEN** target owner ready、target Sample 与 Driver 在同一批次到达
- **THEN** Stage MUST 先完成 Registry snapshot
- **AND** Arbitrator MUST 使用完整 ordered records生成 LayerPlan
- **AND** Presenter MUST NOT看到 source release 与 target sample 之间的中间状态

#### Scenario: 多个 logic tick 同批

- **WHEN** 一个 PresentationFrame 前发生多个连续 owner transitions
- **THEN** Stage MUST 将每条 record 的 tick、phase 与 sequence 原样交给 Arbitrator
- **AND** Stage MUST NOT把它们压平成无顺序 Driver 列表
- **AND** 播放层最终 MUST 每层只收到一个 LayerPlan

### Requirement: 状态切换动画混合必须由表现层消费正式切换事实

StateMachine/Tree MUST 使用正式 handoff fact 表达 HandoffRole、strategy definition、source/target logical owner、resolved leaf owner、cause，并由 lifecycle command envelope 保存 tick、phase 与 sequence。Fact MUST NOT携带 visual snapshot 或 layer endpoint。Arbitrator MUST 使用全部 None/Driver facts构造因果链并提交 LayerPlan；LayerRuntime 与 Presenter MUST NOT解释原始 StateMachine facts。

#### Scenario: None role

- **WHEN** 命中 Role=None 的结构 edge
- **THEN** runtime MUST 发布可追踪的 ordered fact
- **AND** 该 fact MAY 桥接连续 owner topology
- **AND** 它 MUST NOT提供视觉 strategy

#### Scenario: Driver role

- **WHEN** 命中 Role=Driver 的 edge
- **THEN** fact MUST 携带 Immediate、ContributionCrossFade 或 Inertialization definition
- **AND** Arbitrator MUST 决定它是 Selected、Coalesced、Retired 或 Conflict

#### Scenario: Inertialization

- **WHEN** 最终 HandoffPlan 使用 Inertialization
- **THEN** source State、Timeline 与 Action MUST 已按逻辑 barrier 停止
- **AND** output job MUST 从当前最终 pose/velocity 接入 plan 的 DesiredCandidate

### Requirement: StateMachine transition 必须提交动画 owner handoff

StateMachine runtime MUST 为 state activation 提供稳定 owner，并维护 nested presentation leaf。presentation leaf MUST 表示该逻辑 owner 最后正式产出动画的 descendant/producer owner，而不是当前 execution stack 最内层或退出回调中的结构 owner。每条 None/Driver logical transition MUST 在 owner release 前提交完整有序 handoff fact；Target State 获得首次正式执行机会后 MUST 提交 AnimationOwnerReady。StateMachine MUST NOT读取 LayerRuntime 或把 fact 直接提交为播放命令。

#### Scenario: 外层进入嵌套 Attack

- **WHEN** None -> Attack Driver 到达
- **AND** 内层 Attack1 ready/sample 随后到达
- **THEN** target leaf MUST 解析为 Attack1 owner
- **AND** Arbitrator MUST 以完整 Desired Base 生成进入 Attack1 的 LayerPlan
- **AND** 外层 Attack 结构 owner MUST NOT成为视觉 endpoint

#### Scenario: Attack1 进入 Attack2

- **WHEN** combo Driver 命中
- **THEN** source/target leaf MUST 是 Attack1/Attack2
- **AND** Arbitrator MUST 生成从当前 Base FinalOutput 接入 Attack2 的 HandoffPlan

#### Scenario: execution stack 先于视觉历史回退

- **WHEN** nested leaf 已停止执行但其动画仍属于 FinalOutput
- **AND** outer owner 为完成退出回调重新进入 execution scope
- **THEN** outer owner 的 resolved source leaf MUST 继续指向该视觉 leaf
- **AND** handoff fact MUST 在 owner membership release 后仍保留该 identity

#### Scenario: target 暂无 contribution

- **WHEN** Driver target owner 尚未 Ready或 RequireOutput layer 尚未形成最终 incoming
- **THEN** Arbitrator MUST 输出 Hold plan并保留待定因果链
- **AND** LayerRuntime MUST NOT自行选择 Empty

### Requirement: 动画 Transition 必须拥有独立可重入生命周期

动画 transition MUST 分为仲裁 lifecycle 与播放 lifecycle。Ordered record 的 Pending、Selected、Coalesced、Retired 与 Conflict MUST 由 Arbitrator 管理；HandoffPlan 的 Capturing、Running、Completed 与 Superseded MUST 由 LayerRuntime 按 LayerId 管理。每个 layer 最多一个 ActiveHandoff，播放生命周期 MUST 使用 presentation delta，MUST NOT使用 logic tick、Timeline logic time 或 `Evaluate(0)` 隐式推进。

#### Scenario: 同一 layer 重入

- **WHEN** ActiveHandoff 完成前新的 HandoffPlan 到达
- **THEN** 旧 handoff MUST Superseded
- **AND** 新 handoff MUST 从当前 FinalOutput capture

#### Scenario: 不同 layer

- **WHEN** 两个不重叠、正式支持的 LayerId 同时获得各自唯一 LayerPlan
- **THEN** LayerRuntime MAY 各自推进一个 session
- **AND** StateMachine runtime identity MUST NOT作为跨 layer key

## ADDED Requirements

### Requirement: Pipeline 必须从 PreviousOutput 与 DesiredCandidate 解析视觉端点

Pipeline MUST 将 LayerRuntime 当前 FinalOutput 作为 outgoing，将完整命令批次后的 DesiredCandidate 作为 incoming。Arbitrator MUST 在二者之间提交一个完整 LayerPlan；逻辑 source/target State 只用于因果连接、authority 与 debug，MUST NOT直接决定 Empty、underlay 或 target clip。

#### Scenario: 结构 Source

- **WHEN** None -> Dodge Driver 的逻辑 source 不产动画
- **THEN** outgoing MUST 仍是当前 Run FinalOutput
- **AND** incoming MUST 是 Dodge DesiredCandidate

#### Scenario: 结构 Target

- **WHEN** Dodge -> None Driver 的逻辑 target 不产动画
- **THEN** outgoing MUST 是 Dodge FinalOutput
- **AND** incoming MUST 是 Registry 仲裁出的 Run/RunEnd/Idle DesiredCandidate

#### Scenario: 连续逻辑链

- **WHEN** 多条有序 transition records 连通当前 FinalOutput 与最终 DesiredCandidate
- **THEN** Arbitrator MUST 先归并为一个 causal component
- **AND** LayerRuntime MUST 只收到一个 HandoffPlan

#### Scenario: 没有有效计划

- **WHEN** 可见 owner 变化缺少 Driver 路径或存在多个同 authority 独立组件
- **THEN** Arbitrator MUST 生成 Invalid plan并保留完整 provenance
- **AND** Pipeline MUST NOT按 sequence、节点位置或默认策略选择

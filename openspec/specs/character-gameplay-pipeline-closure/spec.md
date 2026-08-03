# character-gameplay-pipeline-closure Specification

## Purpose
定义角色 Gameplay 管线闭环：输入、compiled Graph/StateMachine/Timeline/Action/Effect operation、Character/World state、batch WorldSolver、Committer、Presentation 和 Runtime Debug 必须走同一条正式 Program/Session 主线，不恢复旧 SO/config、对象解释器、旧播放器或 demo 临时桥接。
## Requirements
### Requirement: 角色 Gameplay 管线必须形成 ActionInstance 事实闭环

Input Adapter、compiled Graph/StateMachine、portable Action operation、compiled Timeline、CharacterSimulationState、WorldSimulationState与 Committer MUST通过同一 `Ingress -> Schedule -> Evaluate -> ResolveBatch -> Finalize -> Egress -> atomic Commit`主线形成 ActionInstance闭环。系统 MUST不保留第二套 deterministic node或 demo专用业务路径。

#### Scenario: 本地 Attack1 进入 Attack2

- **WHEN** CharacterSimulationInput 在 combo window 内提交第二次 Attack request
- **THEN** compiled Action/StateMachine/Timeline MUST推进同一 ActionInstance 事实链
- **AND** Committer MUST消费正式 presentation commands

### Requirement: Authoring 装配必须从 CharacterPipelineDefinition 汇入 runtime

CharacterPipelineDefinition MUST继续是唯一 authoring 聚合根，但 Runtime Host MUST只加载与 source revision 匹配的 CharacterSimulationProgram 和 CharacterPresentationProjection。Host MUST不直接从 RootTree、Timeline、Action 或 Effect asset 创建 runtime clone。

#### Scenario: 装配 Corin

- **WHEN** Sandbox Host 创建 Corin
- **THEN** MUST从同一 Definition 绑定 Program 与 Projection

### Requirement: Graph 和 Timeline 必须只输出 gameplay facts

Compiled Graph、StateMachine 和 Timeline operation MUST只更新 CharacterSimulationState pending evaluation，并输出 typed gameplay facts、world request 和 EventId presentation commands。它们 MUST不直接写 Transform、调用 Solver、发送 packet、播放动画或裁决命中。

#### Scenario: Timeline 输出 Dodge Motion

- **WHEN** Dodge motion segment 在当前 Tick active
- **THEN** Evaluate MUST产生 portable world request
- **AND** Pipeline Runtime MUST在统一 batch中取得唯一 body result

### Requirement: Motion 闭环必须依赖正式仲裁而不是直接移动

所有 gameplay motion MUST按 contribution resolve、modifier、portable world request、session batch solve、WorldSimulationState body result 与 Finalize 的唯一顺序执行。Pipeline restore/ingress MUST不注入第二条 motion/correction执行路径，Transform MUST不成为第二真值。

#### Scenario: Solver 受到墙面限制

- **WHEN** request 的目标位移被 Unity CharacterController 截断
- **THEN** WorldSimulationState MUST记录 actual body result 而不是原 request

### Requirement: Presentation 闭环必须只消费表现事实

Presentation MUST只消费 Egress OutputDisposition允许并由 Committer提交的 BodyState sample与 EventId presentation command。Presentation MUST不读取 Graph clone、pending evaluation、WorldSolver object或 Character state mutable view，也 MUST不反向产生 Gameplay fact。

#### Scenario: Attack 动画播放

- **WHEN** committed command 选择 Attack producer
- **THEN** Presentation MUST通过 Projection 与现有 Animancer lifecycle 播放
- **AND** MUST不重新决定 Action ownership

### Requirement: GameplayFacts 必须成为 demo 同步和 debug 的唯一事实出口

`SimulationActorTickResult.GameplayFacts`、`PresentationCommands` 与 `CharacterBodySample` MUST成为recording、diagnostics和Model Egress的正式输出边界。Blackboard state、Program internal slot、WorldSolver internal state和Presentation runtime state MUST不被Model Pass直接读取。

#### Scenario: Action Window 输出

- **WHEN** Timeline projection 生成 ActionWindow fact
- **THEN** Tick result MUST保留 ActionInstance、WindowId、Tick 与 EventId
- **AND** 后续 model/debug MUST从该 typed fact 消费

### Requirement: Runtime Debug 必须按 ActionInstance 展示完整链路

Diagnostics MUST通过 Source Map 与 structured Trace 按 ActorId、ActionInstanceId、SimulationTick、operation、world request/result 和 EventId 展示输入、状态决策、Timeline window、motion、Effect 与 committed presentation。Editor MUST不绑定 runtime clone 或 mutable state。

#### Scenario: 查看 Attack2 Tick

- **WHEN** Debug Session 定位 Attack2 ActionInstance
- **THEN** MUST能关联对应 Program operation、world result 和 presentation command

### Requirement: ServerAuthoritative Gameplay必须复用正式Program与Step Pass

Prediction Client与Authority Worker MUST加载同一Corin Float32 Program并复用正式Program Evaluate、World ResolveBatch和Program Finalize Step Pass。Owner/server/remote差异 MUST只存在于Session Source、Ingress/Schedule/Egress Pass和Presentation registration，不得进入Graph、StateMachine、Timeline、Action、GameplayEffect或Motion operation。

#### Scenario: Authority Worker执行Dodge

- **WHEN** Authority Source accepted command包含Actor A Dodge request
- **THEN** Authority Pipeline MUST通过同一compiled Action/Timeline/Motion operation产生WorldRequest
- **AND** MUST不调用model专属Dodge代码

### Requirement: Local与Hybrid必须是显式且互不回退的完整组合

Local gameplay MUST只由Standard Local Pipeline组合运行；Hybrid gameplay MUST由Prediction或Authority Pipeline组合运行。三种Pipeline MAY共享Program Runtime、Execution Backend、标准Step Pass和Solver实现，但 MUST不共享mutable state、Source、History或Endpoint，并 MUST不在失败时互相切换。

#### Scenario: Fantasy连接失败

- **WHEN** Hybrid Prediction Source preparation失败
- **THEN** 当前Session MUST进入Failed
- **AND** MUST不创建Standard Local Pipeline继续Corin gameplay

### Requirement: 网络复制必须只消费正式Finalized Output

Authority Replication Egress MUST只消费finalized Character/World state、typed GameplayFacts、Presentation commands和EventId；MUST不读取Program mutable slot、pending evaluation、Graph authoring、Unity Transform或Animancer state。Prediction command egress MUST只发送canonical input与identity，MUST不发送resolved displacement作为权威真值。

#### Scenario: 复制Action Window

- **WHEN** Authority Timeline生成ActionWindow fact
- **THEN** Replication Egress MUST保留Actor、ActionInstance、Window、Tick和EventId
- **AND** Fantasy Room MUST只路由该事实而不重新解释窗口语义

### Requirement: Remote表现必须属于正式Committer消费链

Remote Body sample、有限Action producer command和reliable EventId facts MUST在Prediction Pipeline最终Commit边界进入remote presentation output，并复用既有Body interpolation、Presentation Fact、Action lifecycle、AnimationSlot和Projection Pose Plan。Fantasy Handler、Room和Model Source MUST不直接调用Animancer、写visual Transform或决定Animation transition。

#### Scenario: Remote Actor切换到Attack2动画

- **WHEN** RemotePresentationEgress提交Authority producer select command
- **THEN** CharacterActionPlaybackRuntime MUST提交Attack2 lifecycle，并由AnimationSlot与Projection Pose Plan播放
- **AND** 网络层 MUST不发送AnimationClip或直接调用Play

### Requirement: Hybrid Diagnostics必须沿统一Source Map与Session Trace关联

Runtime diagnostics MUST能从authoring identity、Program operation、Prediction/Authority Pipeline Pass、SimulationTick、WorldRequest/Result、baseline、correction decision、EventId disposition和Presentation command形成只读关联。Diagnostics MUST不持有runtime clone、packet queue或mutable state。

#### Scenario: 审查Attack纠偏

- **WHEN** Attack2期间发生RestoreReplay
- **THEN** Debug Session MUST关联Authority baseline、Replay steps、Action operation、EventId suppression和最终动画producer

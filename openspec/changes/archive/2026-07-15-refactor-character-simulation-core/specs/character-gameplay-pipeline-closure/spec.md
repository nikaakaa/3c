# character-gameplay-pipeline-closure Specification

## MODIFIED Requirements

### Requirement: 角色 Gameplay 管线必须形成 ActionInstance 事实闭环

Input Adapter、compiled Graph/StateMachine、portable Action operation、compiled Timeline、CharacterSimulationState、WorldSimulationState 与 Committer MUST通过同一 `Program -> Evaluate -> ResolveBatch -> Finalize -> PublishState -> OutputPlan` 主线形成 ActionInstance 闭环。系统 MUST不保留第二套 deterministic node 或 demo 专用业务路径。

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
- **AND** SessionRuntime MUST在统一 batch 中取得唯一 body result

### Requirement: Motion 闭环必须依赖正式仲裁而不是直接移动

所有 gameplay motion MUST按 contribution resolve、modifier、portable world request、session batch solve、WorldSimulationState body result 与 Finalize 的唯一顺序执行。Driver restore/ingress MUST不作为额外 MotionStage correction contribution，Transform MUST不成为第二真值。

#### Scenario: Solver 受到墙面限制

- **WHEN** request 的目标位移被 Unity CharacterController 截断
- **THEN** WorldSimulationState MUST记录 actual body result 而不是原 request

### Requirement: Presentation 闭环必须只消费表现事实

Presentation MUST只消费 Driver OutputPlan 发布并由 Committer 提交的 BodyState sample 与 EventId presentation command。Presentation MUST不读取 Graph clone、pending evaluation、WorldSolver object 或 Character state mutable view，也 MUST不反向产生 Gameplay fact。

#### Scenario: Attack 动画播放

- **WHEN** committed command 选择 Attack producer
- **THEN** Presentation MUST通过 Projection 与现有 Animancer lifecycle 播放
- **AND** MUST不重新决定 Action ownership

### Requirement: SyncFacts 必须成为 demo 同步和 debug 的唯一事实出口

SimulationTickResult typed SyncDomain facts MUST成为 recording、diagnostics 与后续 Model Output Adapter 的唯一 Gameplay 事实出口。Blackboard state、Program internal slot、WorldSolver internal state 和 Presentation state MUST不被 adapter 直接读取。

#### Scenario: Action Window 输出

- **WHEN** Timeline projection 生成 ActionWindow fact
- **THEN** Tick result MUST保留 ActionInstance、WindowId、Tick 与 EventId
- **AND** 后续 model/debug MUST从该 typed fact 消费

### Requirement: Runtime Debug 必须按 ActionInstance 展示完整链路

Diagnostics MUST通过 Source Map 与 structured Trace 按 ActorId、ActionInstanceId、SimulationTick、operation、world request/result 和 EventId 展示输入、状态决策、Timeline window、motion、Effect 与 committed presentation。Editor MUST不绑定 runtime clone 或 mutable state。

#### Scenario: 查看 Attack2 Tick

- **WHEN** Debug Session 定位 Attack2 ActionInstance
- **THEN** MUST能关联对应 Program operation、world result 和 presentation command

## REMOVED Requirements

### Requirement: 第一阶段网络后端只覆盖 None 和 LocalLoopback

**Reason**：Local Driver 不是 Network Model；旧 LocalLoopback 依赖已删除的 Character NetworkStage adapter，不能继续作为核心完成时的可运行后端。

**Migration**：核心完成时单机 Sandbox 只安装 Local Simulation Session。ServerAuthoritative 必须等待后续正式 Driver 完成。

#### Scenario: 核心完成

- **WHEN** Corin Local Session 可运行
- **THEN** Sandbox MUST不装配 None/LocalLoopback 网络模式

### Requirement: 2v2vE demo 第一阶段只实现最小业务压力事实

**Reason**：本 change 只建立单机可移植模拟核心，不交付网络 Demo；继续保留该要求会把未确认网络范围混入核心验收。

**Migration**：由后续各 Network Model/Demo proposal 分别定义业务压力场景。

#### Scenario: 核心验收

- **WHEN** 本 change 完成
- **THEN** MUST只验收 Corin 单机 Gameplay 纵切

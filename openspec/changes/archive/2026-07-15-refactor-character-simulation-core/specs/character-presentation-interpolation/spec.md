# character-presentation-interpolation Specification

## MODIFIED Requirements

### Requirement: 角色表现插值必须基于 logic sample 历史

Presentation MUST从 Driver OutputPlan 发布并由 Committer 提交的 BodyState sample 生成 visual interpolation history。Presentation MUST不直接读取 WorldSimulationState、WorldSolver、runtime clone 或 MotionDebug 作为逻辑真值。

#### Scenario: Local Driver 提交 Body Sample

- **WHEN** Local Driver 发布一个成功 SimulationTickResult 的 BodyState sample
- **THEN** Committer MUST提交唯一 BodyState sample 给 visual history

### Requirement: 动画 visual playback 必须来自表现帧重采样和生命周期注册表

SimulationOutput MUST只提供 producer/EventId/playback intent，CharacterPresentationProjection MUST定位 Unity 资源，现有 AnimationPlaybackLifecycle/Animancer MUST继续在 PresentationFrame 执行 visual sampling、state reuse 和 fade。Kernel MUST不记录 Animancer state。

#### Scenario: Attack Timeline 选中动画 Producer

- **WHEN** Committer 收到 compiled producer command
- **THEN** MUST通过 Projection 定位 binding 并提交给现有 playback lifecycle

### Requirement: Timeline pose time 与 Animancer fade time 必须独立连续推进

CharacterSimulationState MUST保存 Timeline logic time，Presentation MUST使用表现帧重采样所需 visual Timeline time，Animancer MUST以 presentation delta 推进 fade。三者 MUST不共享一个 mutable clock 或把表现时间写回 CharacterSimulationState。

#### Scenario: 两个 Logic Tick 之间渲染

- **WHEN** PresentationFrame 在下一个 SimulationTick 前推进
- **THEN** Animancer fade 和 visual sample MUST连续推进
- **AND** Timeline gameplay state MUST保持不变

### Requirement: 表现插值不得产生同步事实

PresentationFrame MUST保持为 committed/predicted presentation command 的消费阶段。表现插值 MAY产生 visual pose、Animancer playback state 和 diagnostics snapshot，但 MUST不写入 CharacterSimulationState、WorldSimulationState、SimulationIngress、SimulationOutput typed facts 或 Model Output Adapter queue。

#### Scenario: 高帧率表现帧

- **WHEN** 多个 PresentationFrame 发生在两个 SimulationTick 之间
- **THEN** visual root 与 Animancer MAY连续更新
- **AND** MUST不创建额外 gameplay fact、input command 或 world snapshot

### Requirement: Motion visual pose 必须和逻辑 Transform 分离

系统 MUST区分 WorldSimulationState body 与显式 visual root。WorldSolver/SessionRuntime 唯一更新逻辑 body；PresentationFrame MUST只根据 committed/predicted BodyState samples与 interpolation alpha写 visual root，MUST不调用 Solver、不申请 restore、不修改 World state或产生 correction result。

#### Scenario: Local Motion 插值

- **WHEN** previous/current committed body samples有效
- **THEN** PresentationFrame MUST计算并应用 visual pose
- **AND** WorldSimulationState MUST保持不变

#### Scenario: 后续模型执行 Hard Recovery

- **WHEN** Driver 通过正式 restore恢复 World state
- **THEN** Committer MAY按模型 commit policy更新 visual sample history
- **AND** Presentation MUST不自行改写逻辑 body

### Requirement: Visual root 必须是正式配置

Character Host MUST显式持有 visual root/model root 与 Unity WorldSolver actor binding。缺少当前 composition 所需绑定时创建 MUST失败。系统 MUST不自动使用 CharacterController.transform、Animancer transform、子节点搜索、同名对象或 prefab扫描作为 fallback。

#### Scenario: Host 配置 Visual Root

- **WHEN** Host 创建 Local Corin
- **THEN** MUST将显式 visual root传入 Presentation adapter
- **AND** MUST将独立 actor body binding传入 Unity WorldSolver

#### Scenario: 缺少 Visual Root

- **WHEN** 角色需要表现插值但未配置 visual root
- **THEN** Host MUST报告配置错误

## MODIFIED Requirements

### Requirement: 动画层定义来自管线定义

系统 MUST 使用 `CharacterPipelineDefinition` 作为角色动画层唯一正式定义来源。每个 layer MUST 显式保存 identity、order、Animancer layer index、mask、blend mode、apply flag 与 `AnimationLayerOutputPolicy`。OutputPolicy MUST 是 `RequireOutput` 或 `AllowEmpty`；Unspecified MUST 是配置错误。Timeline、State、Presenter 或旧 SO MUST NOT保存另一份 layer 真数据。

#### Scenario: Base layer 要求持续输出

- **WHEN** Corin Base layer 配置为 RequireOutput
- **THEN** 正常激活期间该层 MUST 具有 InitialSeed、Stable、Held、Running 或明确 Invalid 输出
- **AND** 系统 MUST NOT静默把该层解释为 Empty

#### Scenario: Optional layer 允许为空

- **WHEN** 某 layer 显式配置为 AllowEmpty
- **THEN** 正式 LayerPlan MAY 输出 layer weight 0
- **AND** Presenter MUST NOT为该空层创建 fallback clip

#### Scenario: contribution 引用缺失 layer

- **WHEN** contribution 的 LayerId 不存在于 definition
- **THEN** Arbitrator MUST 报告配置错误
- **AND** 该 contribution MUST NOT进入 LayerPlan

### Requirement: 动画层运行时负责仲裁

系统 MUST 将动画层处理分为 `CharacterAnimationLayerArbitrator` 与持久 `CharacterAnimationLayerRuntime`。Arbitrator MUST 从 Registry snapshot、完整有序 lifecycle records 与 Runtime 只读 playback snapshot 计算每层唯一 `AnimationLayerPlan`；它 MUST 负责 contribution priority allocation、DesiredCandidate、transition ledger、因果链归并、独立组件 authority 与 Hold/Invalid 决策。LayerRuntime MUST 只消费 LayerPlan，并按 LayerId 保存 FinalOutput、HeldOutput、唯一 ActiveHandoff 与播放进度。Presenter MUST NOT承担业务仲裁。

#### Scenario: 同一 layer 有多个 override 贡献

- **WHEN** 同一 layer 存在多个 override contributions
- **THEN** Arbitrator MUST 按 priority 从高到低分配覆盖权重
- **AND** 低优先级正式 contribution MUST 填充剩余权重
- **AND** 同优先级总权重超出剩余权重时 MUST 在组内归一

#### Scenario: 每层只提交一个计划

- **WHEN** 一个 PresentationFrame 包含多个 logic tick 的 lifecycle records
- **THEN** Arbitrator MUST 为每个正式 LayerId 输出且只输出一个 LayerPlan
- **AND** LayerRuntime MUST NOT接收原始 Driver 列表自行决定计划

#### Scenario: Action 覆盖 Locomotion

- **WHEN** Action contribution 优先级高于 Locomotion
- **THEN** DesiredCandidate MUST 表达 Action 实际覆盖与 Locomotion underlay
- **AND** Action 覆盖期间的较低 authority Locomotion transition component MUST NOT自动创建可见 handoff

#### Scenario: CompletedHeld 参与候选

- **WHEN** Registry 包含尚未 release membership 的 CompletedHeld contribution
- **THEN** Arbitrator MUST 按普通 layer 规则处理它
- **AND** Arbitrator MUST NOT决定其 producer retirement

### Requirement: Animancer 只是最终播放 adapter

`AnimancerAnimationPresenter` MUST 只消费 `AnimationLayerPlaybackOutput`，并按输出设置 layer weight、state time、state weight、mask 与 additive。它 MUST NOT决定 owner、HandoffRole、OutputPolicy、因果链、LayerPlan、blend completion、Idle fallback、Timeline time 或 contribution lifecycle。

#### Scenario: 应用逐层输出

- **WHEN** LayerRuntime 生成最终 layer outputs
- **THEN** Presenter MUST 按每层 output 设置 Animancer layer weight
- **AND** Presenter MUST 按 state plan 设置 visual clip time 与 layer-local weight

#### Scenario: Held output

- **WHEN** RequireOutput layer 收到 Hold 或 Invalid plan
- **THEN** Presenter MUST 继续应用 LayerRuntime 提供的 held/final plans
- **AND** Presenter MUST NOT因本帧 contribution 缺席自行 Stop

#### Scenario: 正式空 layer

- **WHEN** AllowEmpty layer 的最终 weight 为 0
- **THEN** Presenter MAY 停止 LayerRuntime 已退休的 state
- **AND** Presenter MUST NOT播放隐藏 Idle 或 Controller fallback

### Requirement: 基础姿态必须由正式来源输出

Base pose、Idle、Move 与其它基础动画 MUST 来自正式 Graph、State、Timeline 或 Action contribution。RequireOutput MAY 在正式 Hold/Invalid plan 下保持上一合法 layer output，但 MUST 保留其来源 identity 与错误状态。Pipeline、Arbitrator、LayerRuntime 与 Presenter MUST NOT内置隐藏基础姿态 producer。

#### Scenario: 首次激活缺少基础动画

- **WHEN** RequireOutput Base 尚未 InitialSeed
- **AND** Registry 没有合法 Base contribution
- **THEN** Arbitrator MUST 生成 Invalid plan
- **AND** 系统 MUST NOT选择 bind pose clip、旧 locomotion 或隐藏 Idle

#### Scenario: 已有输出后 incoming 延迟

- **WHEN** Base 已有 FinalOutput
- **AND** target 尚未 Ready或最终 incoming contribution 尚未形成
- **THEN** Arbitrator MUST 生成 Hold plan并保留待定因果链
- **AND** LayerRuntime MUST 保持 HeldOutput

### Requirement: 循环动画必须由连续 visual Timeline time 重采样

循环 Timeline/clip 的 continuous visual time MUST 继续由 TimelinePlaybackScheduler 的 logic time、cycle 与 PresentationFrame interpolation 计算。Arbitrator MUST 使用本帧正式 sample 生成 DesiredCandidate 与 LayerPlan；LayerRuntime MAY 保存 FinalOutput 用于 handoff，但 MUST NOT自行推进 producer clock 或在两个历史 clip time 之间插值。

#### Scenario: 循环回绕

- **WHEN** loop Timeline 从末尾回绕到开头
- **THEN** AnimationTrack MUST 使用连续 visual Timeline time 重采样同一 contribution identity
- **AND** Update plan MUST 使用该正式 clip time 更新同 owner output

#### Scenario: source 已停止

- **WHEN** 循环 source owner 已在逻辑 barrier 内 release
- **AND** layer handoff 仍在 Running
- **THEN** LayerRuntime MAY 冻结当前 visual output 或使用最终 pose
- **AND** Timeline MUST NOT为淡出继续推进

### Requirement: 状态切换混合必须使用 owner handoff 的 Registry 真相

Registry MUST 只提供 producer membership 与合法 contributions。Arbitrator MUST 使用完整有序 lifecycle records、OwnerReady/release、当前 FinalOutput snapshot 与完整 DesiredCandidate 生成 LayerPlan。Outgoing MUST 来自 LayerRuntime 当前 FinalOutput，incoming MUST 来自 Arbitrator 完整 DesiredCandidate；逻辑 source/target MUST 只用于因果连接、authority 与 debug，MUST NOT直接解释为视觉 endpoint。

`AnimationOwnerReady` MUST 作为对应 activation 已获得执行机会的单调事实处理。owner membership release MUST 结束 Registry producer membership，但 MUST NOT删除仍被未决因果链引用的 ready fact。Arbitrator MUST 在所有 Layer 都不再引用对应 record 后清理 released ready facts。

#### Scenario: None 进入 Dodge

- **WHEN** Action None -> Dodge Driver 到达
- **AND** Final Base 是 Run、Desired Base 是 Dodge
- **THEN** Arbitrator MUST 生成一个 Run -> Dodge HandoffPlan
- **AND** source None MUST NOT被当作 outgoing Empty

#### Scenario: Dodge 返回 None

- **WHEN** Dodge -> None Driver 到达
- **AND** Desired Base 是 RunLoop 或 RunEnd
- **THEN** Arbitrator MUST 生成一个 Dodge -> Desired Base HandoffPlan
- **AND** target None MUST NOT被当作 incoming Empty

#### Scenario: target contribution 暂缺

- **WHEN** Driver 已到达、target 尚未 Ready或正式 target contribution 尚未进入 RequireOutput DesiredCandidate
- **THEN** Arbitrator MUST 保留因果链并生成 Hold plan
- **AND** 系统 MUST NOT超时选择 fallback 或 Empty

#### Scenario: target Ready 与 release 同批到达

- **WHEN** Driver、target AnimationOwnerReady 与 target owner release 在同一 Presentation command batch 到达
- **THEN** Arbitrator MUST 先使用 ready fact完成当前 plan commit
- **AND** Registry MUST 仍按 release 结束 producer membership
- **AND** 相关 record 全部完成 disposition 后 MUST 清理 ready fact

### Requirement: 统一动画贡献注册表必须拥有播放实例生命周期

所有 animation producers MUST 使用同一 Registry 表达 playback、contribution、owner membership 与 Active/CompletedHeld/Retired。Handoff 的因果 record、Ready/release retention 与 plan disposition MUST 属于 Arbitrator；Capturing、Running、Superseded 与 Completed 播放状态 MUST 属于 LayerRuntime。Registry MUST NOT保存 transition ledger、blend elapsed、LayerPlan 或 pose history。

#### Scenario: Owner membership 释放

- **WHEN** source owner 在逻辑 barrier 内 release
- **THEN** Registry MUST 拒绝该 owner 的后续 Sample
- **AND** LayerRuntime MAY 使用 FinalOutput 独立完成已提交的视觉收尾

#### Scenario: Once Timeline 完成

- **WHEN** Once playback Complete 但 owner membership 未 release
- **THEN** Registry MUST 保持 contribution 为 CompletedHeld
- **AND** Timeline MUST NOT建立独立保活 mixer

## ADDED Requirements

### Requirement: 动画仲裁必须先归并连续因果链

除 InitialSeed 与同 owner Update 外，Arbitrator MUST 先按 activation owner 与 command order 将 None/Driver transition records 构造成有向因果组件，再对互不连通组件执行 authority 仲裁。一个组件内从当前可见 owner 到最终 Desired owner 的唯一路径 MAY 包含多个 Driver；路径中最后一个 Driver MUST 提供 HandoffPlan strategy，更早 Driver MUST 标记 Coalesced。Sequence MUST 只决定已连通路径内部顺序，MUST NOT决定独立组件胜负。

#### Scenario: 快速 Locomotion 连续切换

- **WHEN** ordered records 为 RunLoop#4 -> RunEnd#5 -> MovingTurn#6 -> RunEnd#7
- **AND** Final Base 是 RunLoop#4、Desired Base 是 RunEnd#7
- **THEN** Arbitrator MUST 将这些 records 归并为一个连续组件
- **AND** MUST 只生成一个 RunLoop#4 -> RunEnd#7 HandoffPlan
- **AND** 路径中更早 Driver MUST 标记 Coalesced而不是 Conflict

#### Scenario: None 桥接

- **WHEN** 连续路径中包含 Role=None 的结构 transition
- **THEN** None record MUST 参与 owner topology 连接
- **AND** None MUST NOT提供 strategy

#### Scenario: 真正独立的多个 Driver

- **WHEN** 两个互不连通组件同时影响同一 layer
- **AND** 两者具有相同最高可见 authority
- **THEN** Arbitrator MUST 生成 Invalid plan并报告全部 component provenance
- **AND** 系统 MUST NOT按最后 command、Parallel 顺序或节点位置选择

#### Scenario: 较低 authority underlay

- **WHEN** 独立 Action component 的可见 authority 高于 Locomotion component
- **THEN** Action component MUST 生成唯一 HandoffPlan
- **AND** Locomotion component MUST 标记 Retired

### Requirement: 每个动画层最多拥有一个 Active Handoff

每个 LayerId MUST 最多拥有一个 ActiveHandoff。Arbitrator 每帧 MUST 只向该 LayerRuntime 提交一个 LayerPlan；新 HandoffPlan 在旧 handoff 完成前到达时，LayerRuntime MUST 从当前 FinalOutput 重新 capture，并将旧 handoff 标记 Superseded。系统 MUST NOT建立 handoff stack。

#### Scenario: CrossFade 中再次切换

- **WHEN** Base CrossFade Running 时新的 HandoffPlan 到达
- **THEN** 新 handoff MUST 从当前加权 FinalOutput 接管
- **AND** 旧 handoff MUST 在 capture 后 Retire

#### Scenario: Inertialization 中再次切换

- **WHEN** Base Inertialization Running 时新的 HandoffPlan 到达
- **THEN** 新 handoff MUST 从当前最终 pose/velocity capture
- **AND** 画面 MUST NOT先跳回旧 target pose

### Requirement: 最终动画输出必须区分 LayerWeight 与 StateWeight

系统 MUST 用 `AnimationLayerPlaybackOutput` 分别表达 layer weight 与 layer-local state weights。Override layer 的 state weights MUST 在非空层内归一；Additive layer MUST 保留 contribution strength 并使用 layer weight 作为 envelope。Presenter MUST 按两级权重应用。

#### Scenario: AllowEmpty layer 淡出

- **WHEN** AllowEmpty layer 的 HandoffPlan 将 DesiredCandidate 变为空
- **THEN** layer weight MUST 按 strategy 过渡到 0
- **AND** outgoing state MUST 在 LayerRuntime retirement 前保持可应用

#### Scenario: Base Stable

- **WHEN** RequireOutput Base 处于合法 Stable
- **THEN** output MUST 表达完整基础 layer coverage
- **AND** state plans MUST 保留正式 owner/contribution identities

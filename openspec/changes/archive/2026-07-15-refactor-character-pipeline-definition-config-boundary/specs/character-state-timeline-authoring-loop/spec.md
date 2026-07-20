## MODIFIED Requirements

### Requirement: Corin 必须由逻辑层提交唯一 Base playback selection

Corin MUST保持单一 Base layer，并在 `CharacterAnimationPresentationProfile` 配置 OutputPolicy=RequireOutput。Locomotion、ActionOverride、Dodge、外层 Action 与 nested combo MUST在逻辑层完成状态、打断和所有权决策，然后为 Base 提交唯一 AnimationPlaybackId。AnimationTrack.Priority、Presentation Driver、Tree route 与 Runtime arbitration MUST不参与该选择。

#### Scenario: Locomotion 正常运行

- **WHEN** ActionOverride 没有活动动作
- **THEN** Base selection MUST来自当前 Locomotion State 的正式 Timeline playback
- **AND** Idle、WalkStart、WalkLoop、RunStart、RunLoop、RunEnd 与 MovingTurn MUST按状态逻辑切换 selection

#### Scenario: Locomotion 进入 Dodge

- **WHEN** Dodge 获得动作所有权
- **THEN** Action 逻辑 MUST为 Base 选择 Dodge playback
- **AND** Animation 模块 MUST不比较 Dodge 与 Locomotion Priority

#### Scenario: Dodge 返回 Locomotion

- **WHEN** Dodge 完成且当前仍有移动输入
- **THEN** 逻辑层 MUST选择当前正式 Run playback
- **AND** 没有移动输入时 MUST选择 RunEnd、Idle 或其它由 Locomotion 状态确定的正式 playback
- **AND** Animation 模块 MUST不从历史 sample 或表现状态猜测返回目标

#### Scenario: Attack1 进入 Attack2

- **WHEN** nested Attack StateMachine 满足连段条件并切换到 Attack2
- **THEN** Action 逻辑 MUST将 Base selection 更新为 Attack2 playback
- **AND** State transition edge MUST只保存逻辑 condition 与 priority

#### Scenario: 无动画 WalkEnd

- **WHEN** WalkEnd 本身没有 animation producer
- **THEN** 本次逻辑提交 MUST省略 Base 更新以保持当前正式 selection，或直接选择目标状态的正式 producer
- **AND** Animation 模块 MUST不为 WalkEnd 创建 fallback Timeline

#### Scenario: 同 tick 多次状态变化

- **WHEN** 一个 logic tick 内 RunLoop、MovingTurn 与 Action ownership 连续变化
- **THEN** Pipeline MUST只提交最终 Base selection
- **AND** playback generation 的 Complete/Release MUST继续保序

### Requirement: Corin animation producer 必须绑定 Animancer 原生 transition

Corin 每个正式 Timeline animation producer MUST拥有稳定 presentation key，并通过 `CharacterAnimationPresentationProfile` 绑定到 Animancer transition key/source。Profile Inspector MUST在显式 Corin Definition context 下，按稳定 identity 列出 Locomotion、Action、Attack1、Attack2 与 Dodge producer 的 Layer 与 binding，但 MUST不复制 producer 之间的逻辑关系；Graph/State edge MUST不保存 transition strategy、duration、curve 或 Driver。

#### Scenario: 配置 Attack1 与 Attack2

- **WHEN** 作者在 Corin Definition context 下的 Profile Inspector 查看 Attack1 和 Attack2
- **THEN** Inspector MUST显示两个 producer 的 stable key 与 Animancer binding
- **AND** source-target fade duration MAY由 Animancer TransitionLibrary modifier 配置
- **AND** Pipeline MUST不创建第二张 pair transition 表

#### Scenario: 配置 Locomotion 与 Dodge

- **WHEN** 作者调整 Dodge 的进入或退出表现
- **THEN** 调整 MUST落在 Animancer 原生 transition/library 数据
- **AND** RootTree、Parallel edge 与 StateMachine edge MUST保持纯逻辑

#### Scenario: 缺失 producer binding

- **WHEN** selected Corin producer 没有合法 Animancer transition binding
- **THEN** runtime MUST报告明确配置错误
- **AND** MUST不使用默认 Idle、当前 clip 或 Immediate fallback

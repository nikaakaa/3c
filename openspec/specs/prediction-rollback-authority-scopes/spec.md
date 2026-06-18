# prediction-rollback-authority-scopes Specification

## Purpose
定义预测/回滚权威域、回滚比较域、状态权威矩阵和单一角色控制器路径边界，为后续本地预测和同步测试提供稳定分层依据。
## Requirements
### Requirement: 预测回滚权威域
系统 MUST 提供预测回滚权威域，用纯数据描述状态、动画播放进度、运动来源和 runtime facts 的权威来源。权威域 MUST 至少区分纯表现、逻辑计时、Profile 驱动和 Animator runtime direct 这几类语义，并且 MUST NOT 依赖 Unity Object、Animancer runtime state、AnimationClip、TransitionAsset 或场景实例引用。

#### Scenario: 纯表现动画不成为 gameplay 权威
- **GIVEN** 某基础移动循环动画只用于视觉播放
- **WHEN** replay 后该动画 normalized time 与原始运行不同
- **THEN** 该差异 MUST 被归类为表现漂移或忽略
- **AND** MUST NOT 作为 strict gameplay mismatch

#### Scenario: Profile 驱动动画成为 strict 权威
- **GIVEN** 某状态的 profile playback window 会驱动真实 root position 或 yaw
- **WHEN** replay 后 playback window 或 normalized time 与原始运行不同
- **THEN** 该差异 MUST 被归类为 strict gameplay mismatch
- **AND** F6/F8 MUST 失败或报告 strict failure

#### Scenario: AnimatorRuntimeDirect 不是强回滚默认路径
- **GIVEN** 某状态声明直接使用 Animator runtime root delta
- **WHEN** 系统执行 strict rollback replay
- **THEN** 该状态 MUST 明确声明其非 strict 或弱预测语义
- **AND** MUST NOT 被隐式当作 deterministic profile motion source

### Requirement: 回滚比较域
系统 MUST 提供回滚比较域，用于把 snapshot comparison 的字段差异划分为 strict gameplay mismatch、predictive gameplay drift、presentation drift 或 ignored。Strict gameplay mismatch MUST 决定 synctest 成败；presentation drift MUST 可诊断但 MUST NOT 导致 strict replay 失败。

#### Scenario: Strict mismatch 决定失败
- **GIVEN** replay 后 position、yaw、状态机 active state 或 profile-driven motion facts 不一致
- **WHEN** comparer 输出结果
- **THEN** 结果 MUST 包含 strict differences
- **AND** synctest result MUST 为失败

#### Scenario: Presentation drift 不决定失败
- **GIVEN** replay 后只有视觉动画 normalized time 或动画名称不一致
- **WHEN** comparer 输出结果
- **THEN** 结果 MUST 包含 presentation differences
- **AND** synctest result MUST 保持成功

#### Scenario: 差异分组可诊断
- **WHEN** F6/F8 输出 replay 结果日志
- **THEN** 日志 MUST 分别输出 strict differences 和 presentationDifferences
- **AND** first mismatch 诊断 MUST 能区分 first strict mismatch 和 first presentation drift

### Requirement: 状态权威矩阵
系统 MUST 提供状态权威矩阵或等价 policy，使 TurnBack、MoveLoop、Dodge、Attack 等状态可以声明动画权威、运动权威和回滚比较域。实现 MUST 通过统一 resolver 或 policy 查询分类，MUST NOT 长期依赖散落在 comparer 内的 alias 硬编码。

#### Scenario: TurnBack 初始分类
- **WHEN** TurnBack 使用 baked motion profile 驱动 translation 或 yaw
- **THEN** TurnBack playback progress 和 profile sampling window MUST 属于 `ProfileDriven`
- **AND** 其比较域 MUST 属于 `StrictGameplay`

#### Scenario: MoveLoop 初始分类
- **WHEN** MoveLoop 的角色位移由输入方向、速度和 motion executor 驱动
- **THEN** MoveLoop 视觉 normalized time MUST 属于 `VisualOnly`
- **AND** 其 animation playback drift SHOULD 属于 `PresentationDrift`

#### Scenario: Action 初始分类
- **WHEN** Action 或 Dodge 的逻辑窗口尚未声明依赖动画播放进度
- **THEN** Action animation normalized time MUST 默认为 `PresentationDrift`
- **AND** Action active/state/completed 等 gameplay facts MUST 仍属于 `StrictGameplay`

### Requirement: 单一角色控制器路径
系统 MUST 在权威域和比较域引入后继续通过现有 FullBody、Locomotion、runtime blackboard 和 motion executor 主线推进。实现 MUST NOT 为 strict、predictive 或 presentation 各自创建独立角色控制器路径，也 MUST NOT 为 F6/F8 创建专用 gameplay 逻辑。

#### Scenario: Scope 不产生第二控制器
- **WHEN** 某状态被标记为 `PresentationDrift`
- **THEN** replay MUST 仍走同一 Character/Locomotion 主线
- **AND** MUST NOT 创建单独的表现层 replay controller

#### Scenario: Strict 状态不绕过主线
- **WHEN** 某状态被标记为 `StrictGameplay`
- **THEN** replay MUST 仍通过正式 state machine、motion source 和 motion executor 产生结果
- **AND** MUST NOT 直接写 Transform 或直接调用底层 sampler 制造收敛

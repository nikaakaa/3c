## MODIFIED Requirements
### Requirement: 动作动画 Profile 数据源
系统 MUST 提供动作动画 Profile 数据源，用于把稳定 action animation key 映射到具体动画表现。动作逻辑、状态生命周期接口和统一状态机输出 MUST 只输出 action animation key，不得写死具体角色 clip、可琳动画名、Animancer transition asset 或 BBB 运行时资源路径。

#### Scenario: Profile 保存动作动画条目
- **WHEN** 设计者配置动作动画 Profile
- **THEN** Profile entry MUST 能保存 action animation key
- **AND** MUST 能保存具体动画引用或等价 Animancer transition 引用
- **AND** MAY 保存 fade 参数、播放参数和调试名

#### Scenario: Profile 不是 FullBody 状态机
- **WHEN** 动作动画 Profile 配置 `Action.Dodge.Directional` 或 `Action.Dodge.Backstep`
- **THEN** Profile MUST 只表达动作语义 key 到动画表现资源的映射
- **AND** Profile MAY 使用直接 clip、Animancer transition 或等价 transition asset
- **AND** Profile MUST NOT 替代 FullBody 主行为域中的状态注册、进入条件、退出条件或运动权威

#### Scenario: Profile 通过动画绑定入口接入
- **WHEN** 系统提供 FullBody Action 动画绑定集或等价动画配置入口
- **THEN** 动作动画 Profile MAY 作为该绑定入口的子配置或引用存在
- **AND** 设计者 SHOULD 能通过 FullBody 主调度入口追踪到 Directional 和 Backstep 的动画表现资源
- **AND** 动作动画 Profile MUST NOT 被要求成为和动作逻辑入口、动画绑定入口无绑定关系的游离配置

#### Scenario: 状态生命周期不写死 clip
- **WHEN** `Enter`、`Tick` 或 `Exit` 生命周期产出动作动画请求
- **THEN** 生命周期输出 MUST 使用 `Action.Dodge.Directional`、`Action.Dodge.Backstep` 或等价稳定 key
- **AND** 生命周期实现 MUST NOT 直接引用具体 `AnimationClip`
- **AND** 生命周期实现 MUST NOT 直接引用具体 Animancer transition asset

#### Scenario: 动作逻辑不写死 clip
- **WHEN** Shift FullBody 动作请求动画表现
- **THEN** 动作逻辑 MUST 输出 `Action.Dodge.Directional` 或 `Action.Dodge.Backstep` key
- **AND** 动作逻辑 MUST NOT 直接引用具体 `AnimationClip`
- **AND** 动作逻辑 MUST NOT 直接引用具体角色动画资源名

#### Scenario: 角色可替换动画套件
- **GIVEN** 同一个 Shift FullBody 动作逻辑和状态生命周期输出
- **WHEN** 设计者替换动作动画 Profile 中的 Directional 或 Backstep 动画引用
- **THEN** 系统 MUST 使用新的动画表现
- **AND** 不需要修改动作逻辑代码或状态机资产

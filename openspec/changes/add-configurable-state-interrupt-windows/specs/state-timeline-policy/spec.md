## ADDED Requirements
### Requirement: 状态 Timeline Policy 数据源
系统 MUST 提供状态级 timeline policy 数据源，用于表达一个统一状态在生命周期内的 motion、input lock、interrupt/cancel、exit、priority 和 resistance 窗口。该数据源 MUST 使用稳定 state id、窗口 id、时间域、窗口起止值和请求过滤表达规则，并 MUST NOT 持有 MonoBehaviour、Transform、Animator、Animancer runtime 对象、AnimationClip、TransitionAsset、CharacterController 或场景实例引用。

#### Scenario: TurnBack 配置状态窗口
- **GIVEN** 状态 id 为 `FullBody/Locomotion/TurnBack`
- **WHEN** 设计者配置 timeline policy
- **THEN** policy MUST 能表达 TurnBack motion window
- **AND** MUST 能表达 TurnBack input lock window
- **AND** MUST 能表达 TurnBack exit window
- **AND** MUST 能表达当前状态 resistance

#### Scenario: Attack 复用同一模型
- **GIVEN** 状态 id 为 `FullBody/Action/Attack01`
- **WHEN** 后续设计者配置攻击取消窗口
- **THEN** policy MUST 能用同一种 window 数据表达 attack cancel window
- **AND** MUST NOT 要求新增 Attack 专用窗口模型

#### Scenario: 数据源保持纯数据边界
- **WHEN** 运行时编译 timeline policy
- **THEN** 编译结果 MUST NOT 包含 Animancer、Animator、AnimationClip、TransitionAsset 或 Transform 引用

### Requirement: 状态窗口语义必须区分退出与打断
系统 MUST 用明确 window kind、window id 或等价字段区分 motion、input lock、natural exit、interrupt 和 cancel 语义。natural exit window 只表达当前状态自身允许收尾流转；interrupt/cancel window MUST 携带 allowed request kind 或等价请求过滤，并继续受 priority、resistance 和 force 规则约束。系统 MUST NOT 将视觉 fade、动画尾巴或自然退出窗口隐式当作任意请求打断许可。

#### Scenario: 自然退出不授权 Dodge
- **GIVEN** 当前状态为 `FullBody/Locomotion/TurnBack`
- **AND** timeline facts 表示 `natural-exit` window active
- **WHEN** 玩家仍有移动输入
- **THEN** 统一状态机 MAY 按配置退出到 `MoveLoop`
- **AND** 该 `natural-exit` window MUST NOT 单独让 Dodge 请求 accepted

#### Scenario: Dodge cancel window 才授权 Dodge 仲裁
- **GIVEN** 当前状态存在 `dodge-cancel` 或等价 cancel window
- **AND** window 携带 allowed request kind `Dodge`
- **WHEN** Dodge 请求进入状态请求仲裁入口
- **THEN** 仲裁入口 MAY 在 priority、resistance 和 force 规则满足时接受该请求

### Requirement: 状态 Timeline Window 时间域
系统 MUST 明确每个 timeline window 的时间域。第一版 MUST 至少支持 normalized state time 和 elapsed seconds；若窗口基于动画播放进度，系统 MUST 通过动画播放进度事实转换为纯数据 normalized time，而不得让 policy 或仲裁器直接读取 Animancer。

#### Scenario: normalized window 命中
- **GIVEN** motion window 使用 normalized time domain，范围为 `0.0` 到 `0.47`
- **WHEN** 当前状态 normalized time 为 `0.3`
- **THEN** window facts MUST 标记该 motion window active

#### Scenario: seconds window 命中
- **GIVEN** interrupt window 使用 seconds time domain，范围为 `0.2` 到 `0.5`
- **WHEN** 当前状态 elapsed seconds 为 `0.3`
- **THEN** window facts MUST 标记该 interrupt window active

#### Scenario: 无播放进度不猜测
- **GIVEN** window 需要动画 normalized time
- **WHEN** 当前动画播放进度事实无效
- **THEN** sampler MUST 不猜测 clip 长度
- **AND** 对应 window MUST 不被标记为 active

### Requirement: 状态 Timeline Policy 不表达视觉混合参数
状态 timeline policy MUST NOT 保存或裁决 clip、clip fallback、fade duration、blend duration、speed、start time、TransitionAsset、TransitionLibrary key 或 Animancer event。动画表现配置 MAY 保存这些表现参数；timeline policy 只输出可被逻辑、仲裁和运动层消费的窗口 facts。

#### Scenario: 修改视觉 fade 不改变窗口事实
- **GIVEN** 同一个 TurnBack timeline policy
- **AND** 动画表现配置的 fade 从 `0.08` 改为 `0.25`
- **WHEN** timeline sampler 在相同状态时间采样
- **THEN** 输出的 active windows MUST 保持一致

#### Scenario: 编译结果不暴露表现字段
- **WHEN** 系统编译 timeline policy
- **THEN** runtime policy MUST NOT 暴露 clip、fade、speed、start time 或 TransitionAsset 字段

### Requirement: 状态 Timeline Policy 校验
系统 MUST 对状态 timeline policy 提供校验，覆盖空 state id、空 window id、非法时间域、非法窗口范围、负优先级、负 resistance、重复窗口和 TurnBack 必需窗口缺失。

#### Scenario: 非法窗口范围报错
- **GIVEN** 一个 normalized window 的 end 小于 start
- **WHEN** 系统校验 timeline policy
- **THEN** 校验结果 MUST 包含错误

#### Scenario: TurnBack 缺 motion window 报错
- **GIVEN** `FullBody/Locomotion/TurnBack` 的 timeline policy 缺少 motion window
- **WHEN** 系统校验配置
- **THEN** 校验结果 MUST 包含错误

#### Scenario: 重复窗口报告 warning
- **GIVEN** 同一个 state id 下存在重复 window id
- **WHEN** 系统校验配置
- **THEN** 校验结果 MUST 包含 warning
- **AND** 重复窗口 MUST NOT 被静默忽略

### Requirement: 状态 Timeline Policy 正式装配
系统 MUST 通过正式配置装配状态 timeline policy。缺失必需 policy 或必需窗口时，系统 MUST 输出可诊断配置错误并阻止相关状态按隐式默认值运行；系统 MUST NOT 通过 Resources、全局单例、代码生成默认值或场景查找创建 fallback 配置。

#### Scenario: 缺失 TurnBack policy 不静默运行
- **GIVEN** 默认角色配置允许 TurnBack
- **AND** TurnBack timeline policy 缺失
- **WHEN** 玩家满足 TurnBack 输入条件
- **THEN** 系统 MUST 输出配置诊断
- **AND** MUST NOT 使用代码中的隐藏默认窗口让 TurnBack 继续运行

#### Scenario: 正式资产可校验
- **WHEN** 设计者运行角色配置校验
- **THEN** 校验结果 MUST 能定位当前角色绑定的 timeline policy 资产
- **AND** MUST 能报告缺失或非法窗口

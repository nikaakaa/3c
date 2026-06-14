## ADDED Requirements
### Requirement: 统一状态机消费状态 Timeline Facts
统一状态机 MUST 消费状态 timeline facts、accepted request facts 和普通输入 facts 来决定 transition。状态机配置 MAY 引用 timeline policy，但 transition evaluator MUST NOT 直接读取 policy SO、Animancer、Animator、AnimationClip、TransitionAsset 或 motion executor。

#### Scenario: TurnBack 入口只消费 accepted fact
- **GIVEN** 输入方向满足 TurnBack 几何条件
- **AND** 状态请求仲裁入口接受 TurnBack 请求
- **WHEN** 统一状态机推进本帧
- **THEN** `MoveLoop -> TurnBack` transition MUST 通过 accepted TurnBack fact 触发
- **AND** transition evaluator MUST NOT 再次计算 priority 或 resistance

#### Scenario: rejected 请求不切状态
- **GIVEN** 输入方向满足 TurnBack 几何条件
- **AND** 状态请求仲裁入口拒绝 TurnBack 请求
- **WHEN** 统一状态机推进本帧
- **THEN** 状态机 MUST NOT 进入 `FullBody/Locomotion/TurnBack`

#### Scenario: evaluator 不读取配置资产
- **WHEN** 检查 transition evaluator 源码
- **THEN** evaluator MUST NOT 引用 timeline policy SO
- **AND** MUST NOT 引用状态请求策略 SO

### Requirement: 状态节点输出使用 Timeline Window
状态节点输出 MUST 能读取当前 state timeline facts 来决定运动锁、baked motion 采样窗口、动画请求和退出条件。输出层 MUST 继续产出纯数据运动/动画命令，不得直接调用运动执行端或动画播放端。

#### Scenario: TurnBack motion window 输出 baked motion
- **GIVEN** 当前状态为 `FullBody/Locomotion/TurnBack`
- **AND** timeline facts 表示 motion window active
- **WHEN** 状态输出生成运动事实
- **THEN** 输出 MUST 请求从配置的 baked motion profile 采样本帧 yaw 和 translation
- **AND** 输出 MUST 抑制普通输入旋转和平面位移

#### Scenario: TurnBack exit window 退出
- **GIVEN** 当前状态为 `FullBody/Locomotion/TurnBack`
- **AND** timeline facts 表示 exit window active
- **WHEN** 玩家仍有移动输入
- **THEN** 状态机 MAY 退出到 `MoveLoop`
- **WHEN** 玩家没有移动输入
- **THEN** 状态机 MAY 退出到 `Idle`

### Requirement: 逻辑 Transition 与视觉 Blend 分离
统一状态机 transition MUST 表达逻辑状态切换条件，而不得表达视觉 crossfade 持续时间。transition 条件满足后，当前逻辑状态 MUST 在该状态机推进中立即更新；视觉 blend MUST 由动画外观 adapter 根据动画表现配置独立执行。需要玩法持续时间的恢复、收尾或打断窗口 MUST 通过显式状态或 timeline window 表达。

#### Scenario: TurnBack 退出不等待视觉 fade 完成
- **GIVEN** 当前状态为 `FullBody/Locomotion/TurnBack`
- **AND** timeline facts 表示 `natural-exit` window active
- **WHEN** `TurnBack -> MoveLoop` 条件满足
- **THEN** 统一状态机当前状态 MUST 切换为 `FullBody/Locomotion/MoveLoop`
- **AND** Animancer 或等价动画外观 MAY 继续执行视觉 crossfade

#### Scenario: 修改 fade 不改变逻辑 transition
- **GIVEN** 同一组 state timeline facts 和 accepted request facts
- **AND** 动画表现配置 fade 从 `0.08` 改为 `0.25`
- **WHEN** transition evaluator 求值
- **THEN** transition 结果 MUST 保持一致

#### Scenario: 玩法恢复段必须显式建模
- **GIVEN** 某个动作退出后需要 `0.2` 秒不可被普通输入打断的恢复段
- **WHEN** 设计者配置该行为
- **THEN** 系统 MUST 使用显式恢复状态、input lock window 或 cancel window 表达该玩法持续时间
- **AND** MUST NOT 依赖视觉 crossfade 时长表达该限制

### Requirement: 状态 Timeline 配置可见
默认统一状态机配置 MUST 让设计者能看到哪些状态绑定了 timeline policy。TurnBack、Dodge 和后续 Attack 的窗口配置 MUST 通过同一种可见入口关联，而不得藏在多个运行时组件的私有字段中。

#### Scenario: TurnBack policy 在配置中可定位
- **WHEN** 设计者检查默认角色状态机配置
- **THEN** `FullBody/Locomotion/TurnBack` MUST 能定位自己的 timeline policy
- **AND** 设计者 MUST 能从该配置找到 motion、input lock 和 exit window

## MODIFIED Requirements
### Requirement: 逻辑状态后的动画转换配置
系统 MUST 在逻辑状态确定后产出稳定动画请求 key 或等价表现请求。Animancer `TransitionAssetBase`、TransitionLibrary key、clip、fade、speed、start time、Animancer event 或等价表现参数 MUST 由独立动画表现配置解析和维护；逻辑状态机配置 MAY 暴露稳定动画请求 key、调试名和正式绑定入口，但 MUST NOT 将视觉 fade 或 clip 参数作为逻辑 transition、退出窗口或打断窗口的权威。

#### Scenario: Dodge 变体配置动画请求
- **WHEN** 设计者配置 `FullBody/Action/Dodge`
- **THEN** `Directional` 变体 MUST 能定位对应稳定动画请求 key 或正式动画表现绑定
- **AND** `Backstep` 变体 MUST 能定位对应稳定动画请求 key 或正式动画表现绑定
- **AND** 具体 clip、fade、speed 和 start time MUST 由动画表现配置维护

#### Scenario: 动画不决定逻辑进入
- **WHEN** 动画外观 adapter 播放某个 Animancer transition
- **THEN** 它 MUST 只消费统一状态机产出的动画请求
- **AND** MUST NOT 决定 `Dodge` 是否允许进入
- **AND** MUST NOT 决定 `Dodge` 是否退出到 `MoveLoop` 或 `Idle`

#### Scenario: 动画事实回传为纯数据
- **WHEN** 状态 transition 需要等待动画可退出
- **THEN** 动画外观 adapter MUST 只回传 normalized time、can exit 或等价纯数据 fact
- **AND** 统一状态机条件 MUST 读取该 fact
- **AND** 统一状态机 MUST NOT 直接读取 Animancer state

#### Scenario: 视觉 fade 不作为退出条件
- **WHEN** 状态 transition 需要等待动作可退出
- **THEN** 退出条件 MUST 读取 timeline window fact、animation can exit fact 或等价纯数据事实
- **AND** MUST NOT 读取动画表现配置中的 fade duration 作为退出时间

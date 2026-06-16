## ADDED Requirements
### Requirement: TurnBack 动画运动策略
系统 MUST 为 `FullBody/Locomotion/TurnBack` 提供独立于普通 Walk/Run 基础移动的动画运动策略。该策略 MUST 允许 TurnBack 在转身窗口内使用 baked motion profile 或等价采样事实驱动根位移和朝向，并允许第一版忽略 TurnBack 动画平移尾巴，转完后交还普通 MoveLoop。该策略 MUST 使用烘焙运动数据入口，使编辑器可以生成 yaw、translation、marker、entry timing 和 exit timing 的纯数据资产。

#### Scenario: TurnBack 播放配置 alias
- **GIVEN** 当前逻辑状态为 `FullBody/Locomotion/TurnBack`
- **WHEN** 系统构建移动动画上下文
- **THEN** 动画外观层 MUST 播放 `Locomotion.Turn.Back` 或配置中等价 alias
- **AND** 该 alias MUST 来自现有动画配置或状态输出绑定

#### Scenario: TurnBack yaw 作为纯数据事实
- **GIVEN** TurnBack 动画包含转身 yaw
- **WHEN** 动画外观层或采样器读取本帧播放窗口
- **THEN** 系统 MUST 产出本帧 yaw 贡献作为纯数据事实
- **AND** 该事实 MUST 不携带 Animancer runtime state
- **AND** 该事实 MUST 不直接写 Transform

#### Scenario: TurnBack 可消费烘焙运动数据
- **GIVEN** TurnBack motion policy 引用了有效 baked motion profile
- **WHEN** 运行时采样当前播放窗口
- **THEN** 系统 MUST 能从 baked profile 读取 yaw、translation 或 marker 事实
- **AND** 采样结果 MUST 仍以纯数据 movement facts 进入运动命令
- **AND** 运行时 sampler MUST NOT 依赖 UnityEditor API

#### Scenario: TurnBack 第一版只消费烘焙转身窗口平移
- **GIVEN** `Locomotion.Turn.Back` 动画包含转身后的继续跑动位移
- **WHEN** TurnBack motion policy 的 translation source 为 baked motion profile 或等价配置
- **THEN** 系统 MUST 只将烘焙转身窗口内的平面位移作为 TurnBack 平面位移贡献
- **AND** MUST NOT 将该跑步尾巴平移作为 TurnBack 平面位移贡献
- **AND** 转完后 MUST 由普通 MoveLoop 位移重新接管

#### Scenario: Presenter 不拥有 TurnBack 逻辑
- **WHEN** Animancer 外观层播放 TurnBack 动画
- **THEN** 外观层 MUST 只负责播放、暴露进度、采样或转发 root motion 事实
- **AND** MUST NOT 决定 TurnBack 是否进入
- **AND** MUST NOT 决定 TurnBack 是否退出
- **AND** MUST NOT 调用 motion executor 或 `CharacterController.Move`

#### Scenario: 不靠手工删除源曲线修复 RootT 基线
- **GIVEN** TurnBack 动画 RootT 存在非零基线或预览偏移
- **WHEN** 运行时 TurnBack motion policy 使用 baked motion profile
- **THEN** 系统 MUST 通过 motion policy 消费生成后的纯数据平移和 yaw
- **AND** MAY 使用工具生成不带平面漂移的视觉 clip
- **AND** MUST NOT 要求用户手工删除源 RootT、RootQ 或 skeleton 根位移曲线作为正确运行前提

### Requirement: TurnBack 动画退出事实
系统 MUST 能基于 TurnBack policy 的进入/退出时间、转完点或等价 marker 产生动画退出事实，使 TurnBack 可以在转身完成后退出，而不是必须等待整段动画自然结束。

#### Scenario: 进入时间由 policy 表达
- **GIVEN** TurnBack policy 配置了 start normalized time、fade 或 lock window
- **WHEN** TurnBack 状态进入
- **THEN** 动画请求 MUST 使用这些进入时间参数或其等价配置
- **AND** 输入锁定窗口 MUST 与 policy 中的时间事实一致

#### Scenario: 转完点产生 can exit
- **GIVEN** 当前状态为 `FullBody/Locomotion/TurnBack`
- **AND** policy 配置了 turn complete normalized time
- **WHEN** 当前 `Locomotion.Turn.Back` 播放进度达到该 normalized time
- **THEN** 动画事实层 MUST 产出 TurnBack 可退出事实

#### Scenario: 未到转完点不能退出
- **GIVEN** 当前状态为 `FullBody/Locomotion/TurnBack`
- **AND** 当前播放进度未达到 turn complete normalized time
- **WHEN** 状态机评估 TurnBack 退出 transition
- **THEN** `LocomotionAnimationCanExit` 或等价条件 MUST 为 false

#### Scenario: 整段播放结束仍兼容
- **GIVEN** TurnBack policy 未配置有效 turn complete normalized time
- **WHEN** `Locomotion.Turn.Back` 播放到自然结束
- **THEN** 系统 MAY 使用现有动画结束事实允许退出
- **AND** MUST 输出诊断说明使用了 fallback 退出方式

### Requirement: TurnBack 编辑器预留边界
系统 MUST 为 TurnBack animation motion policy 保留编辑器 authoring 边界。编辑器 MAY 在后续变更中从 animation clip 提取 root yaw、root translation、turn complete marker、entry timing、exit timing 和校验报告，但运行时 MUST 只依赖生成后的纯数据资产或配置。

#### Scenario: 编辑器生成数据不进入运行时依赖
- **WHEN** 后续编辑器工具生成 TurnBack baked motion profile
- **THEN** 生成结果 MUST 是运行时可读取的纯数据资产或等价配置
- **AND** 运行时代码 MUST NOT 引用 UnityEditor 命名空间

#### Scenario: 编辑器可校验动画窗口
- **WHEN** 设计者使用后续 TurnBack 编辑器检查动画
- **THEN** 编辑器 MAY 报告 RootT 基线、turn complete marker、entry timing、exit timing 和 yaw 累计值
- **AND** 这些报告 MUST NOT 改变运行时状态权威

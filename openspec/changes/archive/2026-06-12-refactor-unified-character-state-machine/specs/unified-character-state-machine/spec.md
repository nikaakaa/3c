## ADDED Requirements
### Requirement: 统一层级逻辑状态机权威
系统 MUST 使用一棵统一、可配置、层级化的角色逻辑状态机作为 FullBody base layer 行为的唯一状态权威。`Idle`、`MoveStart`、`MoveLoop`、`MoveStop`、`Dodge` 及后续 Roll、Jump、Attack 等状态 MUST 归属同一种状态节点模型，而不得由 Locomotion 特化状态机、Dodge 特化 runtime 或外层 FullBody 缝合器分别决定。

#### Scenario: 默认状态树可见
- **WHEN** 设计者打开默认角色逻辑状态机配置
- **THEN** 配置 MUST 能显示 `FullBody/Locomotion/Idle`
- **AND** MUST 能显示 `FullBody/Locomotion/MoveStart`
- **AND** MUST 能显示 `FullBody/Locomotion/MoveLoop`
- **AND** MUST 能显示 `FullBody/Locomotion/MoveStop`
- **AND** MUST 能显示 `FullBody/Action/Dodge`

#### Scenario: 不再存在第二状态权威
- **WHEN** 统一状态机接管 FullBody base layer
- **THEN** Locomotion 四阶段 transition MUST 由统一状态机配置决定
- **AND** Dodge 进入和退出 transition MUST 由统一状态机配置决定
- **AND** 系统 MUST NOT 继续通过 `BasicLocomotionStateMachine`、`LocomotionStateGraphConfigSO`、`DodgeActionRuntime`、`DodgeFullBodyActionModule` 或等价特化 runtime 决定另一套状态流转

#### Scenario: 快照来自统一状态机
- **WHEN** 运行时完成一帧状态推进
- **THEN** 当前状态路径、状态时间、当前变体、当前标签和 pending transition MUST 来自统一状态机快照
- **AND** 该快照 MUST NOT 暴露 Animancer state、CharacterController、InputAction、Cinemachine 或 UnityHFSM 内部 state 对象

### Requirement: 通用 transition 配置
系统 MUST 将角色状态切换表达为统一状态机中的 transition 配置。移动意图、无移动意图、预输入请求、状态时间、动画可退出事实、优先级、抗性和打断窗口 MUST 作为 transition 条件或 transition policy 配置呈现，而不是藏在 Locomotion 图或 Action 仲裁器外部路径中。

#### Scenario: Locomotion transition 使用通用条件
- **WHEN** 系统表达基础移动四阶段切换
- **THEN** `Idle -> MoveStart` MUST 使用 `HasMoveIntent` 或等价通用条件
- **AND** `MoveLoop -> MoveStop` MUST 使用 `NoMoveIntent` 或等价通用条件
- **AND** `MoveStop -> Idle` MUST 使用 `NoMoveIntent + StateCanExit` 或等价通用条件
- **AND** 这些 transition MUST 与 Dodge transition 存在于同一张状态机配置中

#### Scenario: Dodge transition 使用通用条件
- **WHEN** 系统表达 Shift Dodge 进入
- **THEN** `Locomotion/* -> Dodge` MUST 使用 `HasInputRequest(Dodge)` 或等价通用条件
- **AND** priority、resistance、timing window 或等价打断规则 MUST 作为该 transition 的可见配置
- **AND** 系统 MUST NOT 通过状态图外部 Action-only arbiter 隐式选择 Dodge 目标状态

#### Scenario: 条件 evaluator 保持纯数据
- **WHEN** transition 条件被求值
- **THEN** evaluator MUST 只读取统一状态机 context 中的纯数据 facts
- **AND** MUST NOT 读取 Animancer runtime state
- **AND** MUST NOT 读取 `CharacterController`
- **AND** MUST NOT 读取 `InputAction` 或 `Camera.main`

### Requirement: 状态输出配置
系统 MUST 允许逻辑状态节点配置进入、更新和退出时的纯数据输出。输出 MAY 包含运动命令、动画转换请求、输入请求消费、Run latch 写入、状态事实写入和诊断事实，但 MUST 由统一状态机先决定当前状态后再产出。

#### Scenario: Locomotion 状态输出基础移动
- **WHEN** 当前逻辑状态为 `MoveLoop`
- **THEN** 状态输出 MUST 能根据当前移动意图产出基础移动运动命令
- **AND** MUST 能产出 `MoveLoop` 对应的动画转换请求或持续播放请求
- **AND** MUST NOT 通过独立 Locomotion runtime 绕过统一状态机提交 base layer 动画

#### Scenario: Dodge 状态输出动作位移
- **WHEN** 当前逻辑状态为 `Dodge` 且变体为 `Directional`
- **THEN** 状态输出 MUST 能按配置距离和时长产出动作位移命令
- **AND** MUST 能产出立即转向输出
- **AND** MUST 能在完成时产出 Run latch 写入
- **AND** Locomotion 状态 MUST NOT 同时产出第二份平面位移或 base layer 动画输出

#### Scenario: Backstep 不写 Run latch
- **WHEN** 当前逻辑状态为 `Dodge` 且变体为 `Backstep`
- **THEN** 状态输出 MUST 能按配置距离和时长产出后闪位移命令
- **AND** MUST NOT 在完成时强制写入 Run latch

### Requirement: 逻辑状态后的动画转换配置
系统 MUST 在逻辑状态节点或状态变体后配置动画转换。动画转换配置 MUST 能绑定 Animancer `TransitionAssetBase`、TransitionLibrary key、clip fallback、fade、speed、start time 或等价表现参数；运行时 MUST 在逻辑状态确定后把动画请求交给动画外观 adapter。

#### Scenario: Dodge 变体配置动画转换
- **WHEN** 设计者配置 `FullBody/Action/Dodge`
- **THEN** `Directional` 变体 MUST 能直接看到并配置对应动画转换
- **AND** `Backstep` 变体 MUST 能直接看到并配置对应动画转换
- **AND** 这些动画转换 MUST NOT 作为游离 Action Animation Profile 藏在另一个无关入口

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

### Requirement: 删除分裂路径
系统 MUST 在统一状态机实现完成后删除、退役或降级现有分裂路径。任何保留类型 MUST 只能作为纯数据模型、迁移工具或外围 adapter 存在，不得继续拥有状态切换、动作进入、base layer 动画选择或平面位移 owner 的权威。

#### Scenario: Locomotion 特化状态机退役
- **WHEN** 统一状态机实现完成
- **THEN** 运行时代码 MUST NOT 再通过 `BasicLocomotionStateMachine` 推进基础移动阶段
- **AND** MUST NOT 再通过 `LocomotionStateGraphConfigSO` 作为独立基础移动状态图配置
- **AND** 基础移动四阶段 MUST 由统一状态机配置表达

#### Scenario: Dodge 特化 runtime 退役
- **WHEN** 统一状态机实现完成
- **THEN** 运行时代码 MUST NOT 再通过 `DodgeActionRuntime` 或 `DodgeFullBodyActionModule` 决定 Dodge 生命周期
- **AND** Dodge 的进入、更新、完成和退出 MUST 由统一状态机状态、transition 和输出表达

#### Scenario: FullBody 缝合器退役
- **WHEN** 统一状态机实现完成
- **THEN** 运行时代码 MUST NOT 再通过仅包装 Locomotion 和 Action 的 `FullBodyHfsmStateTreeDriver` 或等价缝合器决定 owner
- **AND** FullBody owner MUST 从统一状态机当前状态和输出推导

### Requirement: 输入、运动和动画 adapter 保持外围
系统 MUST 保留输入读取、运动执行、动画播放和相机处理作为统一状态机外围 adapter。adapter MUST 执行状态机输出或提供纯数据 facts，不得反向拥有逻辑状态切换权威。

#### Scenario: 输入缓冲只记录请求
- **WHEN** 玩家按下 Shift
- **THEN** 输入 adapter MUST 只写入 Dodge 请求或等价输入事实
- **AND** 是否消费该请求 MUST 由统一状态机 transition 和输出决定

#### Scenario: 运动执行只消费命令
- **WHEN** 统一状态机产出运动命令
- **THEN** 运动 adapter MUST 执行该命令
- **AND** 运动 adapter MUST NOT 选择当前逻辑状态
- **AND** 统一状态机 runner MUST NOT 直接调用 `CharacterController.Move`

#### Scenario: 动画外观只消费动画请求
- **WHEN** 统一状态机产出动画转换请求
- **THEN** Animancer adapter MUST 播放对应 transition
- **AND** Animancer adapter MUST NOT 选择当前逻辑状态
- **AND** 统一状态机 runner MUST NOT 直接调用 Animancer 播放 API

### Requirement: 可测试和可验证
系统 MUST 为统一层级角色逻辑状态机提供自动测试、静态边界验证和 Play Mode 手动验证。验证 MUST 证明状态机统一了当前移动和 Dodge 行为，并证明旧分裂路径不再参与运行时状态决策。

#### Scenario: 自动测试覆盖当前行为
- **WHEN** 运行统一状态机 EditMode 测试
- **THEN** 测试 MUST 覆盖 Idle、MoveStart、MoveLoop、MoveStop 的状态流转
- **AND** MUST 覆盖有移动输入时进入 Dodge Directional
- **AND** MUST 覆盖无移动输入时进入 Dodge Backstep
- **AND** MUST 覆盖 Directional 完成后 Run latch
- **AND** MUST 覆盖 Backstep 完成后不写 Run latch

#### Scenario: 静态验证旧路径删除
- **WHEN** 检查运行时代码
- **THEN** 静态验证 MUST 确认旧 Locomotion 特化状态机不再被运行时引用
- **AND** MUST 确认旧 Dodge 特化 runtime 不再被运行时引用
- **AND** MUST 确认旧 FullBody 缝合器不再被运行时引用

#### Scenario: 用户手动验证
- **WHEN** 用户在 Play Mode 操作可琳角色
- **THEN** 普通 WASD MUST 按统一状态机路径显示 Idle、MoveStart、MoveLoop、MoveStop
- **AND** 有方向按 Shift MUST 显示 Dodge Directional 并向输入方向冲刺
- **AND** 无方向按 Shift MUST 显示 Dodge Backstep 且不强制 Run
- **AND** 用户 MUST 能在同一状态机配置入口看到 Dodge transition 和 Dodge 动画转换配置

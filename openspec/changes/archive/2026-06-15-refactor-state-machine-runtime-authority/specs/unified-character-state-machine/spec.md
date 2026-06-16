## MODIFIED Requirements
### Requirement: 统一层级逻辑状态机权威
系统 MUST 使用一棵统一、可配置、层级化的角色逻辑状态机作为 FullBody base layer 行为的唯一状态权威。`Idle`、`MoveStart`、`MoveLoop`、`MoveStop`、`Dodge` 及后续 Roll、Jump、Attack 等状态 MUST 归属同一种状态节点模型，而不得由 Locomotion 特化状态机、Dodge 特化 runtime 或外层 FullBody 缝合器分别决定。正式运行时 MUST 只允许 FullBody 主调度入口拥有和推进当前角色的 `CharacterStateMachineRunner`；Locomotion adapter、动作 module、动画 Presenter 和 motion executor MUST NOT 创建第二个运行时 runner 或维护第二份 active state。

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

#### Scenario: 只有 FullBody 入口创建 runner
- **WHEN** 检查当前角色正式运行时代码
- **THEN** `CharacterStateMachineRunner` MUST 只由 FullBody 主调度入口创建和持有
- **AND** `PlayerLocomotionController` MUST NOT 创建或缓存自己的正式运行时 runner
- **AND** Locomotion 相关测试如需推进状态机 MUST 显式传入测试构造的 runner，而不得恢复 Locomotion 自驱 runtime owner

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

#### Scenario: Locomotion 自驱入口退役
- **WHEN** 当前角色通过正式 gameplay 路径运行
- **THEN** `PlayerLocomotionController` MUST NOT 独立读取输入后推进统一状态机 runner
- **AND** `PlayerLocomotionController` MUST 只向 FullBody pipeline 提供 Locomotion facts、运动命令构建和动画桥接能力
- **AND** 任何保留的 Locomotion 直接 tick 入口 MUST 输出迁移诊断或仅用于测试，不得参与正式场景装配

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
- **AND** MUST 确认当前角色正式运行时代码不存在第二个 `CharacterStateMachineRunner` owner

#### Scenario: 用户手动验证
- **WHEN** 用户在 Play Mode 操作可琳角色
- **THEN** 普通 WASD MUST 按统一状态机路径显示 Idle、MoveStart、MoveLoop、MoveStop
- **AND** 有方向按 Shift MUST 显示 Dodge Directional 并向输入方向冲刺
- **AND** 无方向按 Shift MUST 显示 Dodge Backstep 且不强制 Run
- **AND** 用户 MUST 能在同一状态机配置入口看到 Dodge transition 和 Dodge 动画转换配置
- **AND** 诊断日志中当前状态路径 MUST 来自 FullBody 主调度入口持有的唯一 runner

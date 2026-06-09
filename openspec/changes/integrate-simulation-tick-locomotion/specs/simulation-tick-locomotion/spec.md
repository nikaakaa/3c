## ADDED Requirements
### Requirement: Locomotion Tick 接入
系统 MUST 能通过 `UnitySimulationTickDriver` 产生的 simulation tick 调度现有基础 Locomotion 主线，而不是继续只能由 Unity frame `Update` 驱动。

#### Scenario: tick phase 调用 Locomotion
- **WHEN** `UnitySimulationTickDriver` 在 tick N 执行 `SimulationTickPhase.ExecuteMotion`
- **THEN** 系统 MUST 调用当前角色的 `PlayerLocomotionController.Tick`
- **AND** MUST 使用该 tick 的 fixed delta 读取或构造移动输入快照

#### Scenario: 多 tick 多次调用
- **WHEN** 某个 Unity frame 通过 accumulator 产生多个 simulation tick
- **THEN** Locomotion MUST 按每个 simulation tick 各执行一次
- **AND** 每次执行 MUST 使用连续 tick context

### Requirement: 防止 Locomotion 双驱动
系统 MUST 防止同一个 `PlayerLocomotionController` 同时被 Unity frame `Update` 和 simulation tick adapter 驱动。

#### Scenario: adapter 接管时关闭 frame Update
- **WHEN** Locomotion tick adapter 接管某个 `PlayerLocomotionController`
- **THEN** 该 controller 的自动 frame Update 驱动 MUST 被关闭或跳过

#### Scenario: 未接管时保持旧行为
- **WHEN** 没有 tick adapter 接管某个 `PlayerLocomotionController`
- **THEN** 该 controller MUST 默认保持当前 Unity frame Update 行为

#### Scenario: 关闭自动 Update 不读输入
- **WHEN** controller 自动 Update 被关闭
- **THEN** controller 的 Unity frame `Update` MUST NOT 读取 input source
- **AND** MUST NOT 提交 motion executor

### Requirement: Tick Adapter 边界
系统 MUST 使用薄 adapter 将 `SimulationTickContext` 转换为现有 Locomotion 调用，并保持 tick driver 与 Locomotion 具体实现解耦。

#### Scenario: adapter 注册到 runner
- **WHEN** Locomotion tick adapter 启用
- **THEN** adapter MUST 注册到 `SimulationTickPhase.ExecuteMotion`
- **AND** 禁用时 MUST 从该 phase 反注册

#### Scenario: driver 不依赖 Locomotion
- **WHEN** `UnitySimulationTickDriver` 编译或运行
- **THEN** driver MUST NOT 直接引用 `PlayerLocomotionController`
- **AND** MUST NOT 直接引用 `ThirdPersonMovement` 命名空间

#### Scenario: adapter 不绕过主线
- **WHEN** adapter 执行 tick
- **THEN** adapter MUST NOT 直接调用 `BasicLocomotionPipeline`
- **AND** MUST NOT 直接调用 motion executor
- **AND** MUST NOT 直接播放 Animancer

### Requirement: Scene Tick 组装
系统 MUST 在当前演示场景中提供明确的 tick driver 组装点，并将当前角色基础 Locomotion 接入该 driver。

#### Scenario: 场景存在 tick driver
- **WHEN** 打开 `Sandbox` 或当前演示场景
- **THEN** 场景 MUST 包含一个用于客户端 simulation tick 的 `UnitySimulationTickDriver` 或等价组件

#### Scenario: 当前角色接入 tick driver
- **WHEN** 当前演示角色存在 `PlayerLocomotionController`
- **THEN** 该角色 MUST 通过 Locomotion tick adapter 接入场景 tick driver
- **AND** MUST NOT 同时由 frame Update 直接驱动

#### Scenario: 没有第二控制路径
- **WHEN** 场景完成 tick 接入
- **THEN** 场景 MUST NOT 新增绕过 `PlayerLocomotionController`、`BasicLocomotionPipeline` 或 motion executor 的第二套移动控制路径

### Requirement: 当前 Locomotion 行为保持
系统 MUST 在 tick 接入后保持当前基础 WASD/Look 和四阶段 Locomotion 行为不回退。

#### Scenario: WASD 移动保持
- **WHEN** 用户在 Play Mode 按 W/A/S/D
- **THEN** 角色移动方向、移动强度和停止行为 MUST 与接入前语义一致

#### Scenario: Look 行为保持
- **WHEN** 用户在 Play Mode 移动鼠标或摇杆 Look
- **THEN** 相机输入和跟随解析 MUST 不因 tick 接入回退

#### Scenario: 四阶段表现保持
- **WHEN** 用户观察基础移动表现
- **THEN** Idle、MoveStart、MoveLoop、MoveStop 的逻辑阶段和动画表现 MUST 不因 tick 接入回退

### Requirement: 非目标边界保持
系统 MUST 保持本变更只接入客户端基础 Locomotion 到 simulation tick，不得扩展到未审批系统。

#### Scenario: 不修改网络协议
- **WHEN** 实施 tick Locomotion 接入
- **THEN** 实施 MUST NOT 修改 Fantasy proto
- **AND** MUST NOT 新增真实网络发包流程

#### Scenario: 不实现 rollback
- **WHEN** 实施 tick Locomotion 接入
- **THEN** 实施 MUST NOT 新增预测回滚驱动
- **AND** MUST NOT 新增状态快照历史

#### Scenario: 不实现状态图配置
- **WHEN** 实施 tick Locomotion 接入
- **THEN** 实施 MUST NOT 实现 `add-locomotion-state-graph-config` 的状态图配置化内容

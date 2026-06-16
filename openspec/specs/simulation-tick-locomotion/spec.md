# simulation-tick-locomotion Specification

## Purpose
定义基础 Locomotion 接入 simulation tick 的调度方式、输入快照、单驱动约束和回归验证。
## Requirements
### Requirement: Locomotion Tick 接入
系统 MUST 能通过 `UnitySimulationTickDriver` 产生的 simulation tick 调度当前角色正式 FullBody gameplay 主线。基础 Locomotion MUST 作为 FullBody pipeline 内的子职责被调用，而不是由独立 `LocomotionTickAdapter` 推进状态机或提交运动。

#### Scenario: tick phase 调用 FullBody
- **WHEN** `UnitySimulationTickDriver` 在 tick N 执行 FullBody 注册的 simulation phases
- **THEN** 系统 MUST 调用当前角色的 `PlayerFullBodyActionController` 或等价 FullBody 主调度入口
- **AND** FullBody pipeline MUST 在固定 phase 中读取或构造移动输入快照
- **AND** Locomotion facts MUST 在 FullBody pipeline 内生成并输入统一状态机 runner

#### Scenario: 多 tick 多次调用
- **WHEN** 某个 Unity frame 通过 accumulator 产生多个 simulation tick
- **THEN** FullBody gameplay MUST 按每个 simulation tick 各执行一次
- **AND** 每次执行 MUST 使用连续 tick context
- **AND** Locomotion 不得在 FullBody pipeline 外额外执行第二次 gameplay tick

### Requirement: 防止 Locomotion 双驱动
系统 MUST 防止同一个角色同时被 Unity frame `Update`、`LocomotionTickAdapter` 和 `FullBodyActionTickAdapter` 驱动。正式当前角色装配 MUST 只使用 FullBody gameplay driver；Locomotion 直接 tick 入口只能作为迁移诊断或测试工具存在。

#### Scenario: FullBody adapter 接管时关闭 frame Update
- **WHEN** FullBody tick adapter 接管某个 `PlayerFullBodyActionController`
- **THEN** 该 controller 的自动 frame Update 驱动 MUST 被关闭或跳过
- **AND** 被引用的 `PlayerLocomotionController` MUST NOT 通过自己的 frame Update 推进 gameplay

#### Scenario: Locomotion adapter 不作为正式 driver
- **WHEN** 当前角色处于正式 gameplay 装配
- **THEN** 场景 MUST NOT 启用会推进 gameplay 的 `LocomotionTickAdapter`
- **AND** 若检测到旧 Locomotion tick 入口 active，系统 MUST 报告明确装配错误
- **AND** 旧 Locomotion tick 入口 MUST NOT 继续推进状态机 runner 或提交 motion executor

#### Scenario: 关闭自动 Update 不读输入
- **WHEN** controller 自动 Update 被关闭
- **THEN** controller 的 Unity frame `Update` MUST NOT 读取 input source
- **AND** MUST NOT 提交 motion executor

### Requirement: Tick Adapter 边界
系统 MUST 使用薄 adapter 将 `SimulationTickContext` 转换为当前正式 FullBody 调用，并保持 tick driver 与 FullBody/Locomotion 具体实现解耦。

#### Scenario: adapter 注册到 runner
- **WHEN** FullBody tick adapter 启用
- **THEN** adapter MUST 注册到 FullBody pipeline 所需的 simulation phases
- **AND** 禁用时 MUST 从这些 phase 反注册

#### Scenario: driver 不依赖 Locomotion
- **WHEN** `UnitySimulationTickDriver` 编译或运行
- **THEN** driver MUST NOT 直接引用 `PlayerLocomotionController`
- **AND** MUST NOT 直接引用 `ThirdPersonMovement` 命名空间

#### Scenario: adapter 不绕过主线
- **WHEN** adapter 执行 tick
- **THEN** adapter MUST NOT 直接调用 `BasicLocomotionPipeline`
- **AND** MUST NOT 直接调用 motion executor
- **AND** MUST NOT 直接播放 Animancer
- **AND** MUST NOT 创建或推进第二个 `CharacterStateMachineRunner`

### Requirement: Scene Tick 组装
系统 MUST 在当前演示场景中提供明确的 tick driver 组装点，并将当前角色 FullBody gameplay 接入该 driver。Locomotion 作为 FullBody pipeline 子职责参与，而不是独立接入场景 tick driver。

#### Scenario: 场景存在 tick driver
- **WHEN** 打开 `Sandbox` 或当前演示场景
- **THEN** 场景 MUST 包含一个用于客户端 simulation tick 的 `UnitySimulationTickDriver` 或等价组件

#### Scenario: 当前角色接入 FullBody tick driver
- **WHEN** 当前演示角色存在 `PlayerFullBodyActionController`
- **THEN** 该角色 MUST 通过 FullBody tick adapter 接入场景 tick driver
- **AND** MUST NOT 同时由 frame Update 直接驱动
- **AND** MUST NOT 同时由 `LocomotionTickAdapter` 驱动

#### Scenario: 没有第二控制路径
- **WHEN** 场景完成 tick 接入
- **THEN** 场景 MUST NOT 新增绕过 `PlayerFullBodyActionController`、`CharacterFramePipeline` 或 motion executor 的第二套移动控制路径

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

### Requirement: Locomotion 动画运动源 Tick 对齐
系统 MUST 在 simulation tick 内对 `TickSampledMotion` Locomotion 动画运动源进行确定性采样，使 sampled 动画运动贡献与 tick delta、播放窗口和状态 timeline 对齐。

#### Scenario: 每个 tick 独立采样
- **GIVEN** Locomotion 状态声明了 `TickSampledMotion` 动画运动源策略
- **WHEN** `UnitySimulationTickDriver` 产生 tick N
- **THEN** Locomotion pipeline MUST 使用 tick N 的播放进度窗口采样动画运动贡献
- **AND** 该贡献 MUST 只影响 tick N 的 movement facts

#### Scenario: 多 tick 同帧
- **GIVEN** 一个 Unity frame 中 accumulator 产生多个 simulation tick
- **WHEN** Locomotion pipeline 连续处理这些 tick
- **THEN** 每个 tick MUST 使用连续且不重叠的动画播放窗口
- **AND** MUST NOT 多次复用同一份 Unity frame runtime root delta

#### Scenario: 不足一个 tick
- **GIVEN** 当前 Unity frame 不足以产生 simulation tick
- **WHEN** Animator 或表现层仍被 Unity 更新
- **THEN** Locomotion pipeline MUST NOT 因表现层更新而提交新的 simulation movement facts
- **AND** 下一次 tick MUST 按 simulation 播放窗口采样 `TickSampledMotion` 动画运动源

#### Scenario: TurnBack 使用 sampled 权威运动
- **GIVEN** 当前 Locomotion 状态为 TurnBack
- **AND** TurnBack 策略选择 `TickSampledMotion`
- **WHEN** simulation tick 构建本 tick movement facts
- **THEN** pipeline MUST 从 TurnBack motion profile 或等价 tick 对齐数据采样 yaw 和 translation
- **AND** MUST 由统一 motion executor 应用该 yaw 和 translation
- **AND** MUST NOT 从 `OnAnimatorMove` pending buffer 消费 runtime root delta

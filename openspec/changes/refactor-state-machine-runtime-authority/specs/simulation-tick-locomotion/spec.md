## MODIFIED Requirements
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
- **THEN** 场景 MUST NOT 新增绕过 `PlayerFullBodyActionController`、`FullBodyFramePipeline` 或 motion executor 的第二套移动控制路径

## MODIFIED Requirements
### Requirement: Tick Adapter 边界
系统 MUST 使用薄 adapter 将 `SimulationTickContext` 转换为现有 Locomotion 调用，并保持 tick driver 与 Locomotion 具体实现解耦。Locomotion tick adapter MUST 只驱动统一 Locomotion 决策管线主入口，不得直接调用管线中间阶段、motion executor 或动画 presenter。

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
- **AND** MUST NOT 直接构造或消费 TurnBack intent

#### Scenario: adapter 驱动统一决策管线
- **WHEN** adapter 执行 tick
- **THEN** adapter MUST 调用 `PlayerLocomotionController` 的统一 Locomotion 主入口或等价入口
- **AND** 该入口 MUST 负责读取或接收输入快照、构建 Locomotion 决策事实、推进统一状态机、构建运动命令并提交外围 adapter

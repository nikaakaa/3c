## ADDED Requirements
### Requirement: FullBody Host Adapter
系统 MUST 删除 `PlayerFullBodyActionController` 或等价 FullBody MonoBehaviour host adapter。正式角色帧 runtime port MUST 由 `CharacterFrameRuntimeController` 或等价角色级 owner 组合状态机 runtime、Locomotion runtime、FullBody Action runtime、output runtime 和 diagnostics dependencies。生产路径 MUST NOT 使用 `FullBodyRuntimePortAdapter` 包装 `PlayerFullBodyActionController` 暴露 pipeline 所需能力。

#### Scenario: 旧 controller 类型被删除
- **WHEN** 检查生产运行时代码、测试 fixture、prefab 和 scene
- **THEN** `PlayerFullBodyActionController` 类型 MUST 不再作为正式组件、字段、属性、构造参数或端口依赖存在
- **AND** `CharacterFramePipeline`、submitter graph 和 builder MUST NOT 直接或间接依赖该类型

#### Scenario: Runner owner 迁入状态机运行时
- **WHEN** 角色 runtime 初始化或恢复状态
- **THEN** 当前角色唯一 `CharacterStateMachineRunner` MUST 由 `CharacterStateMachineRuntime` 或等价状态机运行时模块拥有
- **AND** Locomotion adapter、FullBody Action runtime、motion executor 和 animation presenter MUST NOT 创建第二个正式 runner
- **AND** runner owner MUST NOT 通过 FullBody controller MonoBehaviour 表达

#### Scenario: Output dependencies 不经过 controller 大面板
- **WHEN** 角色帧 output apply 需要 input buffer、motion executor、animation presenter、Locomotion output、facts writer 或 diagnostics
- **THEN** runtime port MUST 通过明确 dependencies host、output runtime 或窄端口提供这些能力
- **AND** MUST NOT 通过 `PlayerFullBodyActionController` 的公开属性或内部类访问
- **AND** MUST NOT 创建 fallback executor、fallback presenter 或隐藏默认配置

### Requirement: Submitter Graph 依赖窄端口
`CharacterFrameSubmitterGraph` MUST 只依赖 Character、Locomotion、FullBody Action、StateMachine 和 Output 的窄端口。它 MUST NOT 依赖 `PlayerFullBodyActionController`、`FullBodyRuntimePortAdapter` 或单个 FullBody 集成端口来读取所有 runtime 状态。

#### Scenario: Submitter Graph 不包装 FullBody controller
- **WHEN** 构建角色 runtime port 与 submitter graph
- **THEN** Locomotion submitter MUST 通过 Locomotion runtime port 获取 Locomotion 所需数据
- **AND** FullBody Action submitter MUST 通过 action runtime/state facts 窄端口获取 action 所需数据
- **AND** submitter graph MUST NOT 通过 `PlayerFullBodyActionController` 或 `FullBodyRuntimePortAdapter` 访问 runner、Dodge config、Locomotion snapshot、output runtime 或 diagnostics

### Requirement: PlayerFullBodyActionController 删除验证
系统 MUST 提供自动测试验证 `PlayerFullBodyActionController` 已从正式 runtime 边界删除。测试 MUST 覆盖代码引用、prefab/scene 绑定、runtime port 组合和 rollback fixture 迁移。

#### Scenario: 静态边界验证无旧 controller
- **WHEN** 运行 runtime port 静态边界测试
- **THEN** 测试 MUST 确认生产 runtime 代码不定义 `PlayerFullBodyActionController`
- **AND** MUST 确认生产 runtime 代码不引用 `PlayerFullBodyActionController`
- **AND** MUST 确认 Corin prefab/scene 不挂载该组件

#### Scenario: 行为测试仍走角色级端口
- **WHEN** 运行 Character frame runtime controller 定向 EditMode 测试
- **THEN** 测试 MUST 通过角色级 runtime port 推进 Locomotion 和 Dodge
- **AND** MUST 证明状态、motion、animation、facts 和 snapshot 仍来自同一条 `CharacterFramePipeline`

## REMOVED Requirements

### Requirement: Dodge Backstep 恢复退出条件

**Reason**: Dodge recovery 是 Action lifecycle 规则，不应继续写在 Locomotion 状态图配置规格中。

**Migration**: 使用 Action module 的 action facts、timeline 或 resolver 测试覆盖。

## MODIFIED Requirements

### Requirement: Locomotion 条件边界

系统 SHALL 使用受控条件集合解析 Locomotion transition。条件 evaluator MUST 只读取 Locomotion context 中的纯数据移动 facts，不得通过任意运行时代码、任意 ScriptableObject 插件、Action 策略或 FullBody owner 执行转移逻辑。

#### Scenario: 移动意图条件
- **GIVEN** 当前 Locomotion state 为 `Locomotion.Idle`
- **AND** context 存在移动意图
- **WHEN** Locomotion module tick
- **THEN** `HasMoveIntent` 条件成立
- **AND** Locomotion module 可以进入 `Locomotion.MoveStart`

#### Scenario: PhaseCanExit 条件
- **GIVEN** 当前 Locomotion state 为 `Locomotion.MoveStart`
- **AND** 移动意图持续存在
- **WHEN** context 中 `PhaseCanExit` 为 false
- **THEN** Locomotion module MUST 保持 `Locomotion.MoveStart`
- **WHEN** context 中 `PhaseCanExit` 为 true
- **THEN** Locomotion module MUST 进入 `Locomotion.MoveLoop`

#### Scenario: 条件 evaluator 不读取表现层
- **WHEN** transition 条件被求值
- **THEN** evaluator MUST NOT 读取 Animancer runtime state
- **AND** MUST NOT 读取 `CharacterController`
- **AND** MUST NOT 读取 `InputAction`
- **AND** MUST NOT 读取 Camera 或 Cinemachine 对象

### Requirement: 状态机配置校验

系统 SHALL 提供可测试的 Locomotion 配置校验能力，在运行前发现缺失状态、非法 transition、重复状态和缺失必要移动配置。校验 MUST 不依赖 Action 或 FullBody path。

#### Scenario: 缺失初始状态
- **GIVEN** Locomotion 配置的初始状态不在节点列表中
- **WHEN** 运行 validator
- **THEN** validator MUST 返回错误

#### Scenario: 缺失 transition 目标
- **GIVEN** Locomotion 配置包含指向不存在状态的 transition
- **WHEN** 运行 validator
- **THEN** validator MUST 返回错误

#### Scenario: 禁止 FullBody path
- **GIVEN** Locomotion 配置包含 `FullBody/Locomotion/Idle`
- **WHEN** 运行 validator
- **THEN** validator MUST 返回错误或迁移诊断
- **AND** 正式配置 MUST 使用 `Locomotion.Idle`

### Requirement: 单驱动权威

系统 SHALL 保证同一玩家角色在任一运行模式下只有一个 Character frame pipeline 推进 gameplay。Locomotion module MAY 拥有自己的领域状态 authority，但它 MUST 只通过 Locomotion submitter 参与管线，MUST NOT 同时由 Unity frame 路径、独立 Locomotion tick 路径和 Action runtime 路径多重驱动。

#### Scenario: Character runtime tick 统一管线
- **GIVEN** `CharacterFrameRuntimeController` 或等价角色级 runtime owner 启用
- **WHEN** 它处理一帧输入
- **THEN** 它 MUST 推进同一个 Character frame pipeline
- **AND** MUST 根据 CharacterFramePlan 选择基础移动或 Action 输出
- **AND** MUST NOT 通过 FullBody root owner 选择输出

#### Scenario: Locomotion runtime 不拥有第二角色帧
- **GIVEN** Character frame pipeline 需要 Locomotion facts
- **WHEN** 它调用 `LocomotionRuntimeModule`、`ILocomotionFrameRuntimePort` 或等价 Locomotion runtime 入口
- **THEN** Locomotion runtime MUST 只提供 Locomotion facts、state result 或候选输出
- **AND** MUST NOT 自行推进第二 Character frame pipeline
- **AND** MUST NOT 在管线外执行基础移动 motion 或 animation

## ADDED Requirements

### Requirement: Locomotion 状态图归属 Locomotion module

系统 SHALL 将基础移动状态图配置归属为 Locomotion module 的内部状态 authority 或等价纯数据状态模型。该配置 MUST 表达初始状态、启用状态、transition、条件和优先级，并通过 Character frame pipeline 提交移动 facts 与候选输出。

#### Scenario: 默认四阶段图
- **GIVEN** 使用默认 Locomotion 配置
- **WHEN** 系统构建 Locomotion module
- **THEN** 状态配置 MUST 包含 `Locomotion.Idle`
- **AND** MUST 包含 `Locomotion.MoveStart`
- **AND** MUST 包含 `Locomotion.MoveLoop`
- **AND** MUST 包含 `Locomotion.MoveStop`
- **AND** 初始状态 MUST 为 `Locomotion.Idle`

#### Scenario: 与 Action 通过计划协作
- **WHEN** Locomotion 与 Action 同帧都有候选输出
- **THEN** Locomotion 状态图 MUST NOT 要求 `Action.Dodge` 与它位于同一棵 runner
- **AND** Action 与 Locomotion 的互斥输出 MUST 由 Character frame plan 表达

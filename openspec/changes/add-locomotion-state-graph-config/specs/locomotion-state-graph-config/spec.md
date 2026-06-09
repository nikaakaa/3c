## ADDED Requirements

### Requirement: 基础移动状态图配置资产
系统 SHALL 提供一个基础移动状态图配置资产，用于描述当前基础移动状态图的初始状态、启用状态、转移、条件和优先级。

#### Scenario: 默认四阶段图
- **GIVEN** 使用默认基础移动状态图配置
- **WHEN** 系统构建 Locomotion 状态机
- **THEN** 状态机包含 `Idle`、`MoveStart`、`MoveLoop`、`MoveStop`
- **AND** 初始状态为 `Idle`

#### Scenario: 配置转移优先级
- **GIVEN** 同一来源状态存在多条转移
- **WHEN** 系统构建 UnityHFSM 转移
- **THEN** 系统 MUST 使用配置中的显式优先级决定转移解析顺序

### Requirement: 基础移动状态图条件
系统 SHALL 使用受控条件集合解析基础移动状态图转移，不得在第一版通过任意运行时代码或任意 ScriptableObject 插件执行转移逻辑。

#### Scenario: 移动意图条件
- **GIVEN** 当前状态为 `Idle`
- **AND** 输入上下文存在移动意图
- **WHEN** 状态机 tick
- **THEN** `HasMoveIntent` 条件成立
- **AND** 状态机可以进入 `MoveStart`

#### Scenario: 最小时长条件
- **GIVEN** 当前状态为 `MoveStart`
- **AND** 移动意图持续存在
- **AND** 阶段时间小于配置的 `MoveStartMinTime`
- **WHEN** 状态机 tick
- **THEN** `MoveStartMinTimeReached` 条件不成立
- **AND** 状态机保持 `MoveStart`

### Requirement: 配置驱动 UnityHFSM 构建
系统 SHALL 通过项目内 builder 从状态图配置构建 UnityHFSM，不得在 MonoBehaviour 或多个状态类中分散注册基础移动状态转移。

#### Scenario: Builder 构建状态机
- **GIVEN** 状态图配置通过校验
- **WHEN** builder 构建状态机
- **THEN** builder 注册配置中的全部状态
- **AND** builder 注册配置中的全部转移
- **AND** builder 显式设置配置中的初始状态

#### Scenario: 错误配置不得静默运行
- **GIVEN** 状态图配置包含错误
- **WHEN** builder 尝试构建状态机
- **THEN** 系统 MUST 返回错误或阻止构建
- **AND** 错误信息 MUST 指出配置问题

### Requirement: 状态图配置校验
系统 SHALL 提供可测试的状态图配置校验能力，在运行前发现缺失状态、非法转移、重复转移、优先级冲突和不可达状态。

#### Scenario: 缺失初始状态
- **GIVEN** 状态图配置的初始状态不在启用状态列表中
- **WHEN** 运行 validator
- **THEN** validator 返回错误

#### Scenario: 不可达状态
- **GIVEN** 状态图配置包含从初始状态无法到达的启用状态
- **WHEN** 运行 validator
- **THEN** validator 返回错误或警告
- **AND** 诊断信息包含不可达状态 ID

### Requirement: 当前四阶段语义保持
系统 SHALL 在配置化后保持当前基础移动四阶段的用户可见语义。

#### Scenario: 起步到循环
- **GIVEN** 当前状态为 `MoveStart`
- **AND** 移动意图持续存在
- **AND** 阶段时间达到 `MoveStartMinTime`
- **WHEN** 状态机 tick
- **THEN** 状态机进入 `MoveLoop`

#### Scenario: 循环到停止
- **GIVEN** 当前状态为 `MoveLoop`
- **AND** 移动意图消失
- **WHEN** 状态机 tick
- **THEN** 状态机进入 `MoveStop`

#### Scenario: 停止到空闲
- **GIVEN** 当前状态为 `MoveStop`
- **AND** 没有移动意图
- **AND** 阶段时间达到 `MoveStopMinTime`
- **WHEN** 状态机 tick
- **THEN** 状态机进入 `Idle`

### Requirement: 主链边界保持
系统 SHALL 保持基础移动状态图独立于输入实现、动画播放实现、相机实现和运动执行实现。

#### Scenario: 状态图不依赖 Unity 表现对象
- **GIVEN** 状态图 builder 和状态机核心代码
- **WHEN** 检查其依赖
- **THEN** 它们 MUST NOT 引用 Animancer
- **AND** MUST NOT 引用 `CharacterController`
- **AND** MUST NOT 引用 KCC 类型
- **AND** MUST NOT 引用 Camera 或 Cinemachine 类型
- **AND** MUST NOT 引用 Unity Input System 类型

#### Scenario: Controller 仍走端口
- **GIVEN** `PlayerLocomotionController` 使用配置状态图
- **WHEN** fake input source 和 fake motion executor 驱动一帧 tick
- **THEN** controller 通过输入端口读取快照
- **AND** controller 通过运动执行端口提交 `MovementCommand`

### Requirement: 状态图 tick-ready 边界
系统 SHALL 让基础移动状态图只消费调用方传入的 delta/facts，使其可被 Unity frame、测试或后续 simulation tick 调度，但不得让状态图 builder/state machine 拥有 tick 调度职责。

#### Scenario: 固定 delta 事实推进状态机
- **GIVEN** 状态机使用配置状态图
- **AND** 调用方以固定 delta 和移动意图事实推进状态机
- **WHEN** 固定 delta 累积达到 `MoveStartMinTime`
- **THEN** 状态机进入 `MoveLoop`
- **AND** 转移语义与可变帧 delta 调用同一状态机入口时一致

#### Scenario: 状态图不引用 tick driver
- **GIVEN** 状态图 builder 和状态机核心代码
- **WHEN** 检查其依赖
- **THEN** 它们 MUST NOT 引用 `SimulationTickRunner`
- **AND** MUST NOT 引用 `UnitySimulationTickDriver`
- **AND** MUST NOT 引用 tick accumulator 或服务端 tick driver 类型

### Requirement: 输入采样不属于状态图配置
系统 SHALL 让状态图配置和 builder 只读取已整理的移动事实，不得直接采样 Move/Look 输入动作、输入缓冲或设备状态。

#### Scenario: 状态图消费移动意图事实
- **GIVEN** pipeline 上游已经将输入快照整理为 `hasMoveIntent`
- **WHEN** 状态图条件解析 `HasMoveIntent`
- **THEN** 条件解析器只读取 `hasMoveIntent` 事实
- **AND** MUST NOT 读取 Move action、Look action、`InputActionReference` 或输入设备状态

### Requirement: 单驱动权威
系统 SHALL 保证同一玩家 Locomotion 在任一运行模式下只有一个驱动入口推进移动、动画和相机 resolve，不得同时由 Unity frame 路径和 simulation tick 路径双驱动。

#### Scenario: 后续 tick adapter 调度现有主链
- **GIVEN** 后续 simulation tick adapter 被启用
- **WHEN** tick adapter 推进基础移动
- **THEN** adapter MUST 调用现有 `PlayerLocomotionController` 或等价主链入口
- **AND** 同一角色的 Unity frame 自动推进路径 MUST 被关闭或合流到同一入口
- **AND** 系统 MUST NOT 新增绕过 `BasicLocomotionPipeline` 的第二移动控制器

# locomotion-state-graph-config Specification

## Purpose
记录基础移动状态图配置的当前归属。旧的独立 Locomotion 状态图配置已被统一角色逻辑状态机取代，Locomotion transition 现在作为统一状态机 transition 的一部分维护。

## Requirements
### Requirement: Locomotion 子树配置归属统一状态机
系统 SHALL 在统一角色逻辑状态机配置中表达基础移动状态图的初始状态、启用状态、transition、条件和优先级，而不得保留独立 Locomotion 状态图作为第二状态权威。

#### Scenario: 默认四阶段图
- **GIVEN** 使用默认统一状态机配置
- **WHEN** 系统构建角色逻辑状态机
- **THEN** 状态机 MUST 包含 `FullBody/Locomotion/Idle`
- **AND** MUST 包含 `FullBody/Locomotion/MoveStart`
- **AND** MUST 包含 `FullBody/Locomotion/MoveLoop`
- **AND** MUST 包含 `FullBody/Locomotion/MoveStop`
- **AND** 初始状态 MUST 为 `FullBody/Locomotion/Idle`

#### Scenario: 配置转移优先级
- **GIVEN** 同一来源状态存在多条 transition
- **WHEN** 统一状态机 runner 求值 transition
- **THEN** 系统 MUST 使用配置中的显式优先级决定 transition 解析顺序

#### Scenario: 与 Dodge transition 同图可见
- **WHEN** 设计者查看默认统一状态机配置
- **THEN** Locomotion 四阶段 transition MUST 与 `Locomotion/* -> Dodge` transition 位于同一配置入口

### Requirement: Locomotion 条件边界
系统 SHALL 使用受控条件集合解析 Locomotion transition。条件 evaluator MUST 只读取统一状态机 context 中的纯数据 facts，不得在第一版通过任意运行时代码或任意 ScriptableObject 插件执行转移逻辑。

#### Scenario: 移动意图条件
- **GIVEN** 当前状态为 `FullBody/Locomotion/Idle`
- **AND** context 存在移动意图
- **WHEN** 状态机 tick
- **THEN** `HasMoveIntent` 条件成立
- **AND** 状态机可以进入 `FullBody/Locomotion/MoveStart`

#### Scenario: PhaseCanExit 条件
- **GIVEN** 当前状态为 `FullBody/Locomotion/MoveStart`
- **AND** 移动意图持续存在
- **WHEN** context 中 `PhaseCanExit` 为 false
- **THEN** 状态机 MUST 保持 `MoveStart`
- **WHEN** context 中 `PhaseCanExit` 为 true
- **THEN** 状态机 MUST 进入 `MoveLoop`

#### Scenario: 条件 evaluator 不读取表现层
- **WHEN** transition 条件被求值
- **THEN** evaluator MUST NOT 读取 Animancer runtime state
- **AND** MUST NOT 读取 `CharacterController`
- **AND** MUST NOT 读取 `InputAction`
- **AND** MUST NOT 读取 Camera 或 Cinemachine 对象

### Requirement: 状态机配置校验
系统 SHALL 提供可测试的统一状态机配置校验能力，在运行前发现缺失状态、非法 transition、重复状态和缺失 Dodge 变体动画绑定。

#### Scenario: 缺失初始状态
- **GIVEN** 统一状态机配置的初始状态不在节点列表中
- **WHEN** 运行 validator
- **THEN** validator MUST 返回错误

#### Scenario: 缺失 transition 目标
- **GIVEN** 统一状态机配置包含指向不存在状态的 transition
- **WHEN** 运行 validator
- **THEN** validator MUST 返回错误

#### Scenario: 缺失 Dodge 动画绑定
- **GIVEN** `FullBody/Action/Dodge` 变体缺少动画 binding
- **WHEN** 运行 validator
- **THEN** validator MUST 返回错误

### Requirement: 单驱动权威
系统 SHALL 保证同一玩家 FullBody base layer 在任一运行模式下只有一个统一状态机 runner 推进状态，不得同时由 Unity frame 路径、Locomotion 局部图和 Action runtime 路径多重驱动。

#### Scenario: FullBody controller tick 统一 runner
- **GIVEN** `PlayerFullBodyActionController` 启用
- **WHEN** 它处理一帧输入
- **THEN** 它 MUST tick 统一状态机 runner
- **AND** MUST 根据统一状态机输出选择基础移动或 Dodge 输出

#### Scenario: Locomotion controller 不拥有第二状态图
- **GIVEN** `PlayerFullBodyActionController` 接管 FullBody base layer
- **WHEN** 它调用 `PlayerLocomotionController`
- **THEN** `PlayerLocomotionController` MUST 使用传入的统一状态机 runner
- **AND** MUST NOT 推进独立 Locomotion 状态图

#### Scenario: 不新增绕过入口
- **WHEN** 后续接入 simulation tick、网络预测、回放或 AI 输入
- **THEN** 调度层 MUST 合流到同一个统一状态机入口
- **AND** 系统 MUST NOT 新增绕过统一状态机的第二移动控制器

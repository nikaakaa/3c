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

### Requirement: Dodge Backstep 恢复退出条件
系统 SHALL 在统一角色逻辑状态机配置中表达 `Action.Dodge.Backstep` 的无输入回 Idle 退出规则。Backstep 的动作位移时长 MUST 与恢复退出条件分离；无输入回 Idle MUST 等待动作恢复退出事实，而不得只依赖动作位移 duration。

#### Scenario: Backstep 未恢复时保持 Dodge
- **GIVEN** 当前状态为 `FullBody/Action/Dodge`
- **AND** 当前变体为 `Backstep`
- **AND** 本帧没有移动意图
- **AND** Backstep 动作位移 duration 已达到
- **WHEN** 动作恢复退出事实为 false
- **THEN** 统一状态机 MUST 保持在 `FullBody/Action/Dodge`
- **AND** MUST NOT 切换到 `FullBody/Locomotion/Idle`

#### Scenario: Backstep 恢复完成后回 Idle
- **GIVEN** 当前状态为 `FullBody/Action/Dodge`
- **AND** 当前变体为 `Backstep`
- **AND** 本帧没有移动意图
- **WHEN** 动作恢复退出事实为 true
- **THEN** 统一状态机 MUST 切换到 `FullBody/Locomotion/Idle`
- **AND** Backstep MUST NOT 写入 Run latch

#### Scenario: Backstep 恢复段输入移动可提前回移动
- **GIVEN** 当前状态为 `FullBody/Action/Dodge`
- **AND** 当前变体为 `Backstep`
- **AND** Backstep 动作位移 duration 已达到
- **AND** 动作恢复退出事实为 false
- **WHEN** 本帧出现移动意图
- **THEN** 统一状态机 MUST 能切换到 `FullBody/Locomotion/MoveLoop` 或等价移动恢复阶段
- **AND** MUST NOT 等待 Backstep 动画完整播放结束
- **AND** Backstep MUST NOT 写入 Run latch

#### Scenario: Backstep 位移参数不被动画长度污染
- **WHEN** 设计者配置 Backstep 动作位移
- **THEN** Backstep 位移 duration 和 distance MUST 继续表达动作运动窗口
- **AND** MUST NOT 因等待动画恢复而被强制改成动画 clip 总长

#### Scenario: Directional Dodge 行为保持
- **GIVEN** 当前状态为 `FullBody/Action/Dodge`
- **AND** 当前变体为 `Directional`
- **AND** 本帧存在移动意图
- **WHEN** Directional 动作位移 duration 达到
- **THEN** 统一状态机 MUST 仍能切换到 `FullBody/Locomotion/MoveLoop`
- **AND** MUST 保持 Directional 完成后写入 Run latch 的现有行为

### Requirement: 状态机条件不得承载 FullBody Action 请求准入
系统 MUST 保持统一状态机 transition 条件集合的职责边界：条件可以读取移动意图、状态可退出、输入事实是否存在、状态 elapsed time 和状态 tag，但 MUST NOT 在默认 FullBody Action 入口中直接判断动作请求 priority、policy min priority、resistance、force 或 timing window。

#### Scenario: 默认 Dodge 入口只消费 accepted fact
- **GIVEN** 默认统一状态机配置
- **WHEN** 设计者查看 `Locomotion/* -> FullBody/Action/Dodge` transition
- **THEN** transition MUST 包含 `HasInputRequest(Dodge)` 或等价已接受请求事实条件
- **AND** transition MUST NOT 包含动作请求 priority 准入条件
- **AND** transition MUST NOT 读取动作策略集合

#### Scenario: transition evaluator 不读取动作策略
- **WHEN** transition evaluator 求值任意条件
- **THEN** evaluator MUST NOT 调用 `ActionInterruptArbiter`
- **AND** MUST NOT 读取 `ActionInterruptPolicySetSO`
- **AND** MUST NOT 执行 action policy matching

#### Scenario: 状态图 priority 不等于动作请求 priority
- **GIVEN** transition 定义包含 priority 字段
- **WHEN** runner 选择多条已满足 transition 中的一条
- **THEN** 该 priority MUST 只决定状态图 transition 选择顺序
- **AND** MUST NOT 被解释为动作请求 priority

### Requirement: RequestPriorityAtLeast 迁移清理
系统 SHOULD 删除或明确废弃 `RequestPriorityAtLeast` 状态机条件，除非实施阶段发现非动作场景存在已审批的真实依赖。若保留该条件，默认 FullBody Action 入口仍 MUST NOT 使用它。

#### Scenario: 无真实依赖时删除条件
- **GIVEN** 静态搜索确认没有非动作场景依赖 `RequestPriorityAtLeast`
- **WHEN** 实施清理
- **THEN** 系统 SHOULD 删除 `RequestPriorityAtLeast` enum、factory、evaluator 分支和默认测试引用
- **AND** MUST 保持已有资产条件 kind 的序列化含义不被误读

#### Scenario: 发现真实依赖时暂停扩大实现
- **GIVEN** 实施阶段发现非动作场景依赖 `RequestPriorityAtLeast`
- **WHEN** 该依赖不在本 proposal 已审批范围内
- **THEN** 实施 MUST 暂停删除该条件
- **AND** MUST 更新 proposal 或回到用户确认
- **AND** MUST NOT 将该条件用于默认 FullBody Action 入口


# locomotion-state-graph-config Specification

## Purpose
记录基础移动状态图配置的当前归属。Locomotion transition 归 Locomotion 领域状态图维护，并通过 `CharacterFramePipeline` 提交纯数据事实和候选输出。
## Requirements
### Requirement: Locomotion 条件边界

系统 SHALL 使用受控条件集合解析 Locomotion transition。条件 evaluator MUST 只读取 Locomotion context 中的纯数据移动 facts，不得通过任意运行时代码、任意 ScriptableObject 插件、Action 策略或 FullBody-as-owner 执行转移逻辑。

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

系统 SHALL 提供可测试的 Locomotion 配置校验能力，在运行前发现缺失状态、非法 transition、重复状态和缺失必要移动配置。校验 MUST 不依赖 Action 或旧 `/FullBody/...` 层级路径。

#### Scenario: 缺失初始状态
- **GIVEN** Locomotion 配置的初始状态不在节点列表中
- **WHEN** 运行 validator
- **THEN** validator MUST 返回错误

#### Scenario: 缺失 transition 目标
- **GIVEN** Locomotion 配置包含指向不存在状态的 transition
- **WHEN** 运行 validator
- **THEN** validator MUST 返回错误

#### Scenario: 禁止旧 FullBody 层级路径
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

### Requirement: 状态机条件不得承载 Action 请求准入
系统 MUST 保持 Locomotion graph transition 条件集合的职责边界：条件可以读取移动意图、Locomotion state 可退出、Locomotion elapsed time、Locomotion tag 和 Locomotion timeline fact，但 MUST NOT 判断动作请求 priority、policy min priority、resistance、force、timing window 或 BodyClaimPolicy。Action 请求准入 MUST 位于 Action request provider/resolver、ActionInterruptArbiter 或等价 Action lifecycle 边界。

#### Scenario: 默认 Locomotion graph 不包含 Dodge 入口
- **GIVEN** 默认 Corin Locomotion graph
- **WHEN** 设计者查看 transition 列表
- **THEN** graph MUST NOT 包含 `Locomotion.* -> Action.Dodge`
- **AND** graph MUST NOT 包含 `Locomotion.* -> Action.Dodge`
- **AND** graph MUST NOT 通过 `HasInputRequest(Dodge)` 进入 Action state

#### Scenario: transition evaluator 不读取动作策略
- **WHEN** Locomotion transition evaluator 求值任意条件
- **THEN** evaluator MUST NOT 调用 `ActionInterruptArbiter`
- **AND** MUST NOT 读取 `ActionInterruptPolicySetSO`
- **AND** MUST NOT 执行 action policy matching
- **AND** MUST NOT 读取 `BodyClaimPolicySO`

#### Scenario: 状态图 priority 不等于动作请求 priority
- **GIVEN** Locomotion transition 定义包含 priority 字段
- **WHEN** runner 选择多条已满足 transition 中的一条
- **THEN** 该 priority MUST 只决定 Locomotion transition 选择顺序
- **AND** MUST NOT 被解释为动作请求 priority

### Requirement: RequestPriorityAtLeast 迁移清理
系统 SHOULD 删除或明确废弃 `RequestPriorityAtLeast` 状态机条件，除非实施阶段发现非动作场景存在已审批的真实依赖。若保留该条件，默认 Action 入口仍 MUST NOT 使用它。

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
- **AND** MUST NOT 将该条件用于默认 Action 入口

### Requirement: Locomotion transient 抢占退出规则
Locomotion graph MUST 能消费 Action 产生的一次性 Locomotion preemption fact，并用正式 Locomotion transition 结束被抢占的 transient motion source。抢占退出 MUST 根据当前移动输入和 Locomotion runtime facts 选择目标 Locomotion state，不得通过 Action state 节点或 Dodge 专用 transition 表达。

#### Scenario: TurnBack 被抢占且有移动输入时进入 MoveLoop
- **GIVEN** 当前 Locomotion graph active state 为 `Locomotion.TurnBack`
- **AND** context 中存在未消费的 Locomotion preemption fact
- **AND** 本帧存在移动输入
- **WHEN** Locomotion graph 评估 transition
- **THEN** graph MUST 以高于 TurnBack 自然出口的优先级进入 `Locomotion.MoveLoop`
- **AND** gait MUST 由 Locomotion intent、Run latch 或等价 Locomotion facts 决定
- **AND** transition MUST NOT 要求 Shift 仍处于 held 状态

#### Scenario: TurnBack 被抢占且无移动输入时进入 Idle
- **GIVEN** 当前 Locomotion graph active state 为 `Locomotion.TurnBack`
- **AND** context 中存在未消费的 Locomotion preemption fact
- **AND** 本帧没有移动输入
- **WHEN** Locomotion graph 评估 transition
- **THEN** graph MUST 以高于 TurnBack 自然出口的优先级进入 `Locomotion.Idle`
- **AND** 后续 frame MUST NOT 恢复旧 TurnBack motion source

#### Scenario: 抢占事实一次性消费并清理 TurnBack 残留
- **GIVEN** Locomotion graph 已经用 preemption fact 退出 `Locomotion.TurnBack`
- **WHEN** Locomotion runtime 提交该帧结果
- **THEN** preemption fact MUST 被标记为已消费或从下一帧 context 移除
- **AND** pending TurnBack intent MUST 被清除
- **AND** TurnBack motion playback window MUST 被重置

#### Scenario: Locomotion graph 不包含 Action 节点
- **WHEN** 设计者检查 Corin Locomotion graph
- **THEN** 抢占退出 MUST 表达为 `Locomotion.TurnBack -> Locomotion.MoveLoop`
- **AND** 抢占退出 MUST 表达为 `Locomotion.TurnBack -> Locomotion.Idle`
- **AND** graph MUST NOT 新增 `Action.Dodge` 节点
- **AND** graph MUST NOT 新增 `Action.Dodge -> Locomotion.*` transition

### Requirement: Locomotion graph 归属 Movement module
系统 SHALL 将 Corin 默认基础移动状态图作为 Movement module 的 Locomotion 局部 graph implementation。该 graph 只表达基础移动 phase、Locomotion transition、Locomotion 条件和 Locomotion timeline facts；它 MUST NOT 作为全角色混合状态树、FullBody-as-owner 树或 Action lifecycle 权威。

#### Scenario: 默认 Locomotion graph
- **WHEN** 系统加载 Corin 默认 Locomotion graph
- **THEN** graph MUST 包含 `Locomotion.Idle`
- **AND** MUST 包含 `Locomotion.MoveStart`
- **AND** MUST 包含 `Locomotion.MoveLoop`
- **AND** MUST 包含 `Locomotion.MoveStop`
- **AND** MUST 包含 `Locomotion.TurnBack`
- **AND** 初始状态 MUST 为 `Locomotion.Idle`
- **AND** graph MUST NOT 包含 `Action.*` state
- **AND** graph MUST NOT 包含 `Action.*` state

#### Scenario: Action transition 不在 Locomotion graph
- **WHEN** 自动校验 Corin 默认 Locomotion graph transitions
- **THEN** transitions MUST NOT 从 `Action.*` 或 `Action.*` 出发
- **AND** transitions MUST NOT 指向 `Action.*` 或 `Action.*`
- **AND** Dodge 进入、持续和退出 MUST 由 Action lifecycle 或已批准的 Action 局部 implementation 表达

### Requirement: Locomotion Run latch 权威
系统 SHALL 将 Run latch 作为 Movement/Locomotion runtime state 维护。Locomotion graph MAY 读取该事实决定 gait 或后续 transition，但 MUST NOT 通过 `Action.Dodge` 节点、Action transition 或按住 Shift 来表达 Directional Dodge 后续 Run。Run latch MUST 在停止并完成 RunEnd/Idle 收尾后清除，使下一次移动从 Walk 开始。

#### Scenario: Directional 后续 Run 不要求 Shift 持续按住
- **GIVEN** Directional Dodge 完成帧通过 frame output 写入 Run latch
- **AND** 玩家保持移动输入但松开 Shift
- **WHEN** 后续 Locomotion frame 构建 MoveLoop
- **THEN** gait MUST 能解析为 Run
- **AND** 该 Run MUST 来自 Locomotion runtime Run latch
- **AND** Locomotion graph MUST NOT 要求 `Run` input fact 持续为 true 才能维持该 Run

#### Scenario: 停止后清除 Run latch
- **GIVEN** Run latch 当前为 active
- **AND** 玩家松开移动输入
- **WHEN** Locomotion 完成 RunEnd、MoveStop 或等价停止收尾并回到 Idle
- **THEN** Locomotion runtime MUST 清除 Run latch
- **AND** last moving gait MUST 回到 Walk 或等价默认
- **AND** 下一次移动输入 MUST 从 Walk 起步，除非有新的正式 Run 或 Dodge latch 写入

#### Scenario: 无移动完成不设置 Run latch
- **GIVEN** Directional Dodge 完成帧没有移动输入
- **WHEN** Action output 应用完成结果
- **THEN** Locomotion runtime Run latch MUST 保持 false
- **AND** 后续 Locomotion MUST 能进入 Idle

#### Scenario: Backstep 不设置 Run latch
- **GIVEN** Backstep Dodge 完成
- **WHEN** Action output 应用完成结果
- **THEN** Locomotion runtime Run latch MUST 保持 false
- **AND** 后续 Locomotion MUST 能进入 Idle 或 Walk 起步

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

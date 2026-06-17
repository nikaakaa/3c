# locomotion-state-graph-config Delta

## ADDED Requirements
### Requirement: Locomotion graph 归属 Movement module
系统 SHALL 将 Corin 默认基础移动状态图作为 Movement module 的 Locomotion 局部 graph implementation。该 graph 只表达基础移动 phase、Locomotion transition、Locomotion 条件和 Locomotion timeline facts；它 MUST NOT 作为全角色混合状态树、FullBody owner 树或 Action lifecycle 权威。

#### Scenario: 默认 Locomotion graph
- **WHEN** 系统加载 Corin 默认 Locomotion graph
- **THEN** graph MUST 包含 `Locomotion.Idle`
- **AND** MUST 包含 `Locomotion.MoveStart`
- **AND** MUST 包含 `Locomotion.MoveLoop`
- **AND** MUST 包含 `Locomotion.MoveStop`
- **AND** MUST 包含 `Locomotion.TurnBack`
- **AND** 初始状态 MUST 为 `Locomotion.Idle`
- **AND** graph MUST NOT 包含 `Action.*` state
- **AND** graph MUST NOT 包含 `FullBody/Action/*` state

#### Scenario: Action transition 不在 Locomotion graph
- **WHEN** 自动校验 Corin 默认 Locomotion graph transitions
- **THEN** transitions MUST NOT 从 `Action.*` 或 `FullBody/Action/*` 出发
- **AND** transitions MUST NOT 指向 `Action.*` 或 `FullBody/Action/*`
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

## MODIFIED Requirements
### Requirement: Locomotion 条件边界
系统 SHALL 使用受控条件集合解析 Locomotion transition。条件 evaluator MUST 只读取 Movement/Locomotion 所需的纯数据 facts，例如移动意图、Locomotion phase elapsed time、Locomotion timeline exit fact 和转向请求事实。条件 evaluator MUST NOT 读取 Action request policy、BodyClaimPolicy、Action lifecycle state、Animancer runtime、CharacterController、InputAction、Camera 或 Cinemachine 对象。

#### Scenario: 移动意图条件
- **GIVEN** 当前 Locomotion state 为 `Locomotion.Idle`
- **AND** context 存在移动意图
- **WHEN** Locomotion graph tick
- **THEN** `HasMoveIntent` 条件成立
- **AND** graph 可以进入 `Locomotion.MoveStart`

#### Scenario: PhaseCanExit 条件
- **GIVEN** 当前 Locomotion state 为 `Locomotion.MoveStart`
- **AND** 移动意图持续存在
- **WHEN** context 中 `PhaseCanExit` 为 false
- **THEN** Locomotion graph MUST 保持 `Locomotion.MoveStart`
- **WHEN** context 中 `PhaseCanExit` 为 true
- **THEN** Locomotion graph MUST 进入 `Locomotion.MoveLoop`

#### Scenario: 条件 evaluator 不读取其它模块
- **WHEN** Locomotion transition 条件被求值
- **THEN** evaluator MUST NOT 调用 `ActionInterruptArbiter`
- **AND** MUST NOT 读取 `BodyClaimPolicySO`
- **AND** MUST NOT 读取 Action lifecycle active action
- **AND** MUST NOT 读取 Animancer runtime state
- **AND** MUST NOT 读取 `CharacterController`
- **AND** MUST NOT 读取 `InputAction`
- **AND** MUST NOT 读取 Camera 或 Cinemachine 对象

### Requirement: 状态机配置校验
系统 SHALL 提供可测试的 Locomotion graph 配置校验能力，在运行前发现缺失初始状态、缺失 transition 目标、重复状态、非法 Action state、非法 Action transition 和遗留 FullBody owner 节点。Dodge 动画绑定校验 MUST 归属 Action 动画配置或 Action lifecycle 相关校验，不得继续作为 Locomotion graph validator 的职责。

#### Scenario: 缺失初始状态
- **GIVEN** Locomotion graph 的初始状态不在节点列表中
- **WHEN** 运行 validator
- **THEN** validator MUST 返回错误

#### Scenario: 缺失 transition 目标
- **GIVEN** Locomotion graph 包含指向不存在状态的 transition
- **WHEN** 运行 validator
- **THEN** validator MUST 返回错误

#### Scenario: 禁止 Action state
- **GIVEN** Locomotion graph 包含 `Action.Dodge` 或 `FullBody/Action/Dodge`
- **WHEN** 运行 validator
- **THEN** validator MUST 返回错误
- **AND** 错误 MUST 指向 Action state 不属于 Locomotion graph

#### Scenario: 禁止 Action 动画绑定
- **GIVEN** Locomotion graph 包含 `Action.Dodge.Directional` 或 `Action.Dodge.Backstep` 动画绑定
- **WHEN** 运行 validator
- **THEN** validator MUST 返回错误
- **AND** 错误 MUST 指向 Action animation binding 应归属 Action 动画配置

### Requirement: 单驱动权威
系统 SHALL 保证同一玩家正式 gameplay 在任一运行模式下只有一个 Character frame pipeline driver 推进一帧。Locomotion graph 是 Movement module 的局部 implementation，Action lifecycle 是 Action module 的局部 implementation，二者 MUST 通过 CharacterFramePipeline 提交候选和 facts，不得创建独立 gameplay tick 入口或第二角色控制路径。

#### Scenario: Character runtime tick 唯一入口
- **GIVEN** 正式 Corin playable 主线已装配
- **WHEN** 角色推进 tick N
- **THEN** 系统 MUST 从 `CharacterFrameRuntimeController` 或等价正式角色 runtime 入口进入 `CharacterFramePipeline`
- **AND** Locomotion graph MUST 只在 Movement module 内被推进
- **AND** Action lifecycle MUST 只在 Action module 内被推进
- **AND** 二者 MUST NOT 分别从 MonoBehaviour Update 自行推进 gameplay

#### Scenario: Locomotion graph 不直接执行输出
- **WHEN** Locomotion graph tick 完成
- **THEN** Movement module MUST 只提交 Locomotion candidate/facts
- **AND** MUST NOT 直接调用 motion executor
- **AND** MUST NOT 直接播放 animation
- **AND** MUST NOT 直接消费 Action 输入请求

#### Scenario: Action lifecycle 不成为第二 tick 入口
- **WHEN** Action lifecycle tick 完成
- **THEN** Action module MUST 只提交 Action candidate/facts/claim
- **AND** MUST NOT 直接调用 motion executor
- **AND** MUST NOT 直接播放 animation
- **AND** MUST NOT 驱动 Locomotion transition

### Requirement: Dodge Backstep 恢复退出条件
系统 SHALL 将 `Action.Dodge.Backstep` 的持续、恢复、完成和退出归属 Action lifecycle 或已批准的 Action timeline/window 数据。Backstep 的动作位移 duration 只表达动作运动窗口；无移动输入时是否完成 Action lifecycle MUST 由匹配动作动画播放完成事实决定，不得通过 Locomotion graph transition 回 `Locomotion.Idle` 或 `Locomotion.MoveLoop` 表达。

#### Scenario: Backstep 未完成时 Action lifecycle 保持 active
- **GIVEN** 当前 Action lifecycle active action 为 `Action.Dodge`
- **AND** 当前变体为 `Backstep`
- **AND** 本帧没有移动意图
- **AND** Backstep 动作位移 duration 已达到
- **WHEN** 匹配 `Action.Dodge.Backstep` 动作动画尚未播放完成
- **THEN** Action lifecycle MUST 保持 `Action.Dodge` active
- **AND** Locomotion graph MUST NOT 通过 `Action.Dodge -> Locomotion.Idle` transition 退出该动作

#### Scenario: Backstep 有移动输入时可恢复到移动
- **GIVEN** 当前 Action lifecycle active action 为 `Action.Dodge`
- **AND** 当前变体为 `Backstep`
- **AND** Backstep 动作位移 duration 已达到
- **AND** 本帧存在移动意图
- **WHEN** Action lifecycle 处理完成条件
- **THEN** Action lifecycle MAY 释放 claim 让 Locomotion 输出恢复采用
- **AND** Backstep MUST NOT 写入 Run latch
- **AND** Locomotion graph MUST NOT 通过 `Action.Dodge -> Locomotion.MoveLoop` transition 退出该动作

#### Scenario: Backstep 完成后释放 claim
- **GIVEN** 当前 Action lifecycle active action 为 `Action.Dodge`
- **AND** 当前变体为 `Backstep`
- **AND** 本帧没有移动意图
- **WHEN** 匹配 `Action.Dodge.Backstep` 动作动画播放完成
- **THEN** lifecycle MUST 清空 active action 或进入等价 inactive state
- **AND** 后续帧 Body Arbiter MUST 不再收到 Dodge full-body claim
- **AND** Backstep MUST NOT 写入 Run latch

#### Scenario: Directional Dodge 行为保持
- **GIVEN** 当前 Action lifecycle active action 为 `Action.Dodge`
- **AND** 当前变体为 `Directional`
- **AND** 本帧仍存在移动输入
- **WHEN** Directional 动作位移 duration 达到
- **THEN** Action lifecycle MUST 能完成该动作
- **AND** Directional 完成后写入 Run latch 的既有行为 MUST 通过 frame output 写入 Locomotion runtime 的正式 Run latch state
- **AND** Locomotion graph MUST NOT 包含 `Action.Dodge -> Locomotion.MoveLoop` transition

#### Scenario: Directional Dodge 完成时无移动输入
- **GIVEN** 当前 Action lifecycle active action 为 `Action.Dodge`
- **AND** 当前变体为 `Directional`
- **AND** 本帧没有移动输入
- **WHEN** Directional 动作位移 duration 达到
- **THEN** Action lifecycle MUST 等待匹配 `Action.Dodge.Directional` 动作动画播放完成后才能释放 claim
- **AND** frame output MUST NOT 写入 Run latch
- **AND** 后续 Locomotion MUST 能回到 `Locomotion.Idle`

### Requirement: 状态机条件不得承载 FullBody Action 请求准入
系统 MUST 保持 Locomotion graph transition 条件集合的职责边界：条件可以读取移动意图、Locomotion state 可退出、Locomotion elapsed time、Locomotion tag 和 Locomotion timeline fact，但 MUST NOT 判断动作请求 priority、policy min priority、resistance、force、timing window 或 BodyClaimPolicy。Action 请求准入 MUST 位于 Action request provider/resolver、ActionInterruptArbiter 或等价 Action lifecycle 边界。

#### Scenario: 默认 Locomotion graph 不包含 Dodge 入口
- **GIVEN** 默认 Corin Locomotion graph
- **WHEN** 设计者查看 transition 列表
- **THEN** graph MUST NOT 包含 `Locomotion.* -> Action.Dodge`
- **AND** graph MUST NOT 包含 `Locomotion.* -> FullBody/Action/Dodge`
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

## REMOVED Requirements
### Requirement: Locomotion 子树配置归属统一状态机
该要求被 `Locomotion graph 归属 Movement module` 取代。默认 Corin graph 不再是统一角色逻辑状态机的 Locomotion 子树，也不再与 Dodge transition 同图可见。

#### Scenario: 旧统一状态机口径删除
- **WHEN** 本变更归档后查看 `locomotion-state-graph-config`
- **THEN** 规格 MUST NOT 要求默认 graph 包含 `FullBody/Locomotion/*`
- **AND** MUST NOT 要求 Locomotion transition 与 `Locomotion/* -> Dodge` transition 位于同一配置入口

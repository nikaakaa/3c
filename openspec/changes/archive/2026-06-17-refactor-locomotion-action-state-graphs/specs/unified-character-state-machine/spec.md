# unified-character-state-machine Delta

## MODIFIED Requirements
### Requirement: 统一层级逻辑状态机权威
系统 MUST 将原“统一层级角色逻辑状态机”的权威范围收窄为通用 graph runtime 能力。该 runtime MAY 被 Movement module 用作 Locomotion 局部 graph implementation，也 MAY 被未来批准的 Action module 用作 Action 局部 graph implementation；它 MUST NOT 作为默认 FullBody base layer 的全角色混合状态树。默认 Corin 角色状态不得要求同一 graph 同时包含 Locomotion 和 Dodge。

#### Scenario: 默认 Locomotion graph 可见
- **WHEN** 设计者打开默认 Corin Locomotion graph 配置
- **THEN** 配置 MUST 能显示 `Locomotion.Idle`
- **AND** MUST 能显示 `Locomotion.MoveStart`
- **AND** MUST 能显示 `Locomotion.MoveLoop`
- **AND** MUST 能显示 `Locomotion.MoveStop`
- **AND** MUST 能显示 `Locomotion.TurnBack`
- **AND** MUST NOT 显示 `Action.Dodge`
- **AND** MUST NOT 显示 `FullBody/Action/Dodge`

#### Scenario: 不存在全角色混合 graph 权威
- **WHEN** 正式 Corin playable 主线推进一帧
- **THEN** Locomotion phase MUST 来自 Movement module 的 Locomotion graph 或等价局部 implementation
- **AND** Action active state MUST 来自 Action lifecycle 或等价 Action implementation
- **AND** Body owner MUST 来自 CharacterFramePlan/Body Arbiter
- **AND** 系统 MUST NOT 要求一个默认 graph 同时决定 Locomotion phase、Dodge lifecycle 和 Body owner

#### Scenario: graph runtime 保持纯数据
- **WHEN** 运行时完成一次 graph tick
- **THEN** graph snapshot MUST 只表达 active id/path、state time、当前变体、当前标签和 pending transition 等纯数据
- **AND** graph snapshot MUST NOT 暴露 Animancer state、CharacterController、InputAction、Cinemachine 或 UnityHFSM 内部 state 对象

### Requirement: 通用 transition 配置
系统 MUST 将 graph transition 配置视为局部 graph implementation 的拓扑规则。Locomotion graph transition 只表达基础移动 phase 变化；Action 进入、退出、priority、resistance、timing window 和 BodyClaimPolicy MUST 位于 Action request/lifecycle/policy 边界，而不得藏在默认 Locomotion graph transition 中。

#### Scenario: Locomotion transition 使用局部条件
- **WHEN** 系统表达基础移动四阶段切换
- **THEN** `Locomotion.Idle -> Locomotion.MoveStart` MUST 使用 `HasMoveIntent` 或等价 Locomotion 条件
- **AND** `Locomotion.MoveLoop -> Locomotion.MoveStop` MUST 使用 `NoMoveIntent` 或等价 Locomotion 条件
- **AND** `Locomotion.MoveStop -> Locomotion.Idle` MUST 使用 `NoMoveIntent + StateCanExit` 或等价 Locomotion 条件
- **AND** 这些 transition MUST NOT 与 Dodge transition 存在于同一默认 Locomotion graph 中

#### Scenario: Dodge transition 不在默认 Locomotion graph
- **WHEN** 系统表达 Shift Dodge 进入
- **THEN** provider/resolver 和 Action lifecycle MUST 处理 Dodge request 与 active state
- **AND** priority、resistance、timing window 或等价打断规则 MUST 作为 Action policy 配置呈现
- **AND** 系统 MUST NOT 通过默认 Locomotion graph transition 隐式选择 Dodge 目标状态

#### Scenario: 条件 evaluator 保持纯数据
- **WHEN** transition 条件被求值
- **THEN** evaluator MUST 只读取该 graph implementation 批准的纯数据 facts
- **AND** MUST NOT 读取 Animancer runtime state
- **AND** MUST NOT 读取 `CharacterController`
- **AND** MUST NOT 读取 `InputAction` 或 `Camera.main`

### Requirement: 状态输出配置
系统 MUST 保持 graph runtime 与输出执行分离。Locomotion graph MAY 产出 Locomotion phase 或 Locomotion output seed；Action lifecycle MAY 产出 Action motion/animation seed；最终运动命令、动画命令、输入消费、Run latch 写入和诊断事实 MUST 通过 Character frame pipeline 的提交与计划阶段表达，而不得由 graph runner 直接执行副作用。

#### Scenario: Locomotion graph 提交基础移动候选
- **WHEN** 当前 Locomotion state 为 `Locomotion.MoveLoop`
- **THEN** Movement module MUST 能根据当前移动意图产出基础移动候选或 Locomotion facts
- **AND** MUST 能产出 `MoveLoop` 对应的动画语义请求或 seed
- **AND** MUST NOT 直接调用 motion executor
- **AND** MUST NOT 直接调用 Animancer 播放 API

#### Scenario: Dodge 输出来自 Action lifecycle
- **WHEN** 当前 Action lifecycle active action 为 `Action.Dodge` 且变体为 `Directional`
- **THEN** Action module MUST 能按配置距离和时长产出动作位移候选
- **AND** MUST 能产出立即转向请求或 seed
- **AND** MUST 能在完成时产出 Run latch 写入事实或 frame output
- **AND** Locomotion graph MUST NOT 同时持有 Dodge 状态输出配置

#### Scenario: TurnBack 状态输出动画驱动策略
- **WHEN** 当前 Locomotion state 为 `Locomotion.TurnBack`
- **THEN** Locomotion graph 或 Locomotion metadata MUST 能声明 `Locomotion.Turn.Back` 动画请求
- **AND** MUST 能声明 TurnBack motion policy
- **AND** motion policy MUST 能引用 baked motion profile 或等价纯数据资产
- **AND** MUST NOT 直接调用 Animancer、CharacterController 或 motion executor

### Requirement: 逻辑状态后的动画转换配置
系统 MUST 允许局部 graph node、Action lifecycle 或 Action variant 配置动画语义 key、timeline binding key 或等价稳定 ID，用于产出动画请求和匹配动画播放进度事实。具体动画播放配置 MUST 归属到 Animancer TransitionLibrary、`RunLocomotionAnimationConfigSO`、`ActionAnimationProfileSO` 或等价动画配置入口；默认 Locomotion graph MUST NOT 保存 Action 动画 binding。

#### Scenario: Dodge 变体配置动画语义 key
- **WHEN** 设计者配置 `Action.Dodge`
- **THEN** Dodge action config、Action animation binding 或等价 Action 配置 MUST 能解析 `Action.Dodge.Directional`
- **AND** MUST 能解析 `Action.Dodge.Backstep`
- **AND** 默认 Locomotion graph MUST NOT 持有这些 Action animation keys

#### Scenario: Locomotion 状态配置 timeline binding key
- **WHEN** 设计者配置 `Locomotion.TurnBack`
- **THEN** Locomotion graph MAY 保存 `Locomotion.Turn.Back` 或等价 timeline binding key
- **AND** 该 key MUST 只用于 Locomotion 动画请求语义、播放进度事实匹配或 timeline window 采样
- **AND** 具体 Locomotion 动画资源和过渡参数 MUST 由基础移动动画配置或 Animancer TransitionLibrary 解析

#### Scenario: 动画不决定逻辑进入
- **WHEN** 动画外观 adapter 播放某个 Animancer transition
- **THEN** 它 MUST 只消费 Character frame pipeline 产出的动画语义请求
- **AND** MUST NOT 决定 `Action.Dodge` 是否允许进入
- **AND** MUST NOT 决定 `Action.Dodge` 是否退出

### Requirement: 删除分裂路径
系统 MUST 删除或降级旧 Locomotion 特化状态机、FullBody 外层缝合器和默认 graph 中的 Action 叶子路径，使正式角色运行时只保留 CharacterFramePipeline 作为一帧调度权威。Locomotion、Action、输入、运动和动画只能作为 Character frame pipeline 下的 facts、request submission 或 frame output submission 来源参与。

#### Scenario: Locomotion graph 是 Movement 局部实现
- **WHEN** 正式角色通过 gameplay 路径运行
- **THEN** 基础移动 phase MAY 由 Locomotion graph 决定
- **AND** 该 graph MUST 只由 Movement module 推进
- **AND** 该 graph MUST NOT 作为独立 gameplay tick 入口

#### Scenario: Dodge 不由默认状态图决定生命周期
- **WHEN** 正式角色通过 gameplay 路径运行
- **THEN** Dodge 的进入、更新、完成和退出 MUST 由 Action request/lifecycle 边界表达
- **AND** 默认 Locomotion graph MUST NOT 包含 `Action.Dodge`
- **AND** 默认 Locomotion graph MUST NOT 包含 Dodge 进入或退出 transition

#### Scenario: FullBody 缝合器退役
- **WHEN** 正式角色通过 gameplay 路径运行
- **THEN** 运行时代码 MUST NOT 再通过仅包装 Locomotion 和 Action 的 `FullBodyHfsmStateTreeDriver` 或等价缝合器决定 owner
- **AND** FullBody owner 兼容事实 MUST 从 frame plan、runtime facts 或诊断 view 推导
- **AND** 兼容 view 不得反向决定 transition 或输出应用

### Requirement: 状态机通用模型与角色业务模型分层
系统 MUST 将自研 graph runtime 的通用图模型与角色业务模型分层。通用图模型 MUST 只表达 state id、层级关系、path、transition、runtime active state、state time、variant、pending transition 和纯数据 snapshot/restore。Locomotion phase、Action state、Dodge、TurnBack、RunLatch、animation binding、motion spec、timeline policy、BodyClaimPolicy 和 condition domain 等角色业务能力 MUST 位于 Movement module、Action module、character metadata、capability module 或等价业务层模型中。

#### Scenario: Generic graph 不知道角色业务词
- **WHEN** 静态检查 generic graph model 和 runner core
- **THEN** 它 MUST NOT 引用 `Dodge`
- **AND** MUST NOT 引用 `TurnBack`
- **AND** MUST NOT 引用 `BasicMovementGait`
- **AND** MUST NOT 引用 `ActionMovementCommand`
- **AND** MUST NOT 引用 Unity scene object 或 Animancer runtime object

#### Scenario: Character metadata 派生 view
- **WHEN** 运行时需要读取当前 owner、Locomotion phase 或 Action state
- **THEN** 系统 MUST 从 Movement facts、Action lifecycle facts、frame plan、character metadata 或 capability module 派生 `FullBodyStateView` 或等价 view
- **AND** 派生 view MUST NOT 反向决定 transition 或成为第二状态权威

#### Scenario: 默认资产迁移保持行为
- **WHEN** 默认角色状态配置迁移到 Locomotion graph 与 Action lifecycle 分离模型
- **THEN** EditMode tests MUST 覆盖 Idle、MoveStart、MoveLoop、MoveStop、TurnBack 和 Dodge 的关键路径
- **AND** Dodge tests MUST 断言 Action lifecycle active state，而不是默认 graph active state

#### Scenario: Pipeline 保持单一权威
- **WHEN** 分层模型接入正式运行时
- **THEN** 正式运行时 MUST 仍只有 Character frame runtime 入口创建和推进一帧
- **AND** graph runner MUST NOT 执行 motion、animation、input consume 或 diagnostic submit 副作用
- **AND** Locomotion adapter、Action module 和 Presenter MUST NOT 创建第二 gameplay driver

## ADDED Requirements
### Requirement: 默认 Corin graph 不表达 Action lifecycle
默认 Corin graph MUST 只作为 Locomotion graph 使用。Action lifecycle MAY 使用纯数据 lifecycle，也 MAY 在后续获批后使用 Action 局部 graph implementation，但默认 Corin Locomotion graph MUST NOT 表达 Action lifecycle。

#### Scenario: 默认 graph 无 Action.Dodge
- **WHEN** 自动测试读取默认 Corin graph 资产
- **THEN** 资产 MUST NOT 包含 `Action.Dodge`
- **AND** MUST NOT 包含 `FullBody/Action/Dodge`
- **AND** MUST NOT 包含指向 Action state 的 transition

#### Scenario: Action lifecycle 可独立 restore
- **GIVEN** Action lifecycle active action 为 `Action.Dodge`
- **WHEN** 系统 capture/restore 角色状态
- **THEN** active Dodge MUST 由 Action lifecycle restore state 表达
- **AND** MUST NOT 要求 default graph snapshot active state 为 `Action.Dodge`

### Requirement: Run latch 是 Movement runtime 事实
系统 MUST 将 Run latch 视为 Movement/Locomotion runtime 事实，而不是通用 graph runtime 的 active state、Action lifecycle state 或默认 Locomotion graph transition。Action lifecycle MAY 通过 frame output 请求写入 Run latch；最终状态 MUST 由 Locomotion runtime 持有并由停止收尾清除。

#### Scenario: 通用 graph 不持有 Run latch 副作用
- **WHEN** graph runner 推进任意局部 graph
- **THEN** runner MUST NOT 直接写 Run latch
- **AND** runner MUST NOT 直接读取 Shift 或 InputAction 判断 Run latch
- **AND** Run latch 写入 MUST 通过 Character frame output 应用阶段完成

#### Scenario: Action lifecycle 不替代 Run latch 状态
- **GIVEN** Directional Dodge 完成且仍有移动输入
- **WHEN** Action lifecycle 输出动作完成
- **THEN** Action lifecycle MAY 产生写 Run latch 的 frame output
- **AND** Action lifecycle restore state MUST NOT 被用作后续 Run 的唯一状态来源
- **AND** Locomotion runtime Run latch MUST 是后续 Run/Walk gait 判断的正式来源

#### Scenario: 默认 graph 不表达 RunEnd 清 latch
- **GIVEN** Run latch active 且玩家停止移动
- **WHEN** Locomotion 完成 RunEnd、MoveStop 或 Idle 收尾
- **THEN** 清 latch MUST 归属 Locomotion runtime/output completion 或等价 Movement runtime 边界
- **AND** 默认 graph MUST NOT 通过 `Action.Dodge -> Locomotion.*` transition 清除 Run latch

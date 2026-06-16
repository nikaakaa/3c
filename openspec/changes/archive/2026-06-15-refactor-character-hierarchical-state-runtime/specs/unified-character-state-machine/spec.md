## ADDED Requirements
### Requirement: 自研统一分层状态图运行时
系统 MUST 将当前角色 FullBody base layer 的正式状态机定义为项目自研的统一分层状态图运行时。该状态机 MUST 同时满足单一权威和分层路径表达：`FullBody/Locomotion/...` 与 `FullBody/Action/...` MUST 属于同一棵状态树，而不得被描述或实现为两个并列状态机再由外层缝合。UnityHFSM MAY 作为参考资料或未来另行审批的 adapter 方向存在，但当前正式角色主线 MUST NOT 默认迁移到 UnityHFSM。

#### Scenario: 统一和分层同时成立
- **WHEN** 设计者查看默认角色状态机配置
- **THEN** 配置 MUST 表达 `FullBody` 根节点
- **AND** MUST 表达 `FullBody/Locomotion` 子域
- **AND** MUST 表达 `FullBody/Action` 子域
- **AND** Locomotion 和 Action 的叶子状态 MUST 共享同一个 active path、state time、variant 和 snapshot 来源

#### Scenario: UnityHFSM 不是正式主线
- **WHEN** 后续实现角色业务状态机功能
- **THEN** 实现 MUST 继续扩展项目自研状态图运行时
- **AND** MUST NOT 在未审批 proposal 中把 UnityHFSM 接入为正式角色状态机 engine
- **AND** MUST NOT 同时保留 UnityHFSM runtime 和自研 runner 作为双状态权威

#### Scenario: 分层路径不暴露第三方内部对象
- **WHEN** 读取运行时状态快照
- **THEN** 快照 MUST 暴露稳定状态 id、active path、state time、variant、pending transition 或等价纯数据
- **AND** MUST NOT 暴露 UnityHFSM state 对象
- **AND** MUST NOT 暴露 Animancer state、Animator state、CharacterController、InputAction 或 Transform

### Requirement: 状态图运行时职责收窄
系统 MUST 将状态图运行时的核心职责收窄为解释状态图、求值 transition、维护 active state、维护 state time、维护 variant、记录 pending transition 诊断和提供纯数据 snapshot/restore。Timeline facts 采样、状态输出解析、运动命令构建、动画请求构建、输入消费、run latch 写入和诊断提交 MUST 位于明确的外围模块或明确的子职责中，不得继续让 runner 直接成为 FullBody、Locomotion、Action、Animation 和 Motion 的混合实现。

#### Scenario: Runner 只推进状态
- **WHEN** 状态图运行时 tick 一帧
- **THEN** 它 MUST 根据状态图配置和 context facts 选择 transition
- **AND** MUST 更新 active state、state time、variant 和 pending transition 诊断
- **AND** MUST NOT 调用 motion executor
- **AND** MUST NOT 调用 animation presenter
- **AND** MUST NOT 直接消费输入缓冲

#### Scenario: Timeline 采样独立
- **WHEN** Action request gate 或 transition 条件需要 timeline facts
- **THEN** 系统 MUST 通过独立 sampler 或等价纯数据模块提供 `StateTimelineWindowFacts`
- **AND** Action request gate MUST NOT 反向依赖 runner 的实现方法采样 timeline
- **AND** sampler MUST NOT 切换状态

#### Scenario: 状态输出独立
- **WHEN** active state 已经确定
- **THEN** 系统 MUST 通过 state output resolver 或等价模块生成运动、动画、输入消费、run latch 和 TurnBack policy 输出
- **AND** output resolver MUST 只返回纯数据
- **AND** output resolver MUST NOT 执行 `CharacterController.Move`
- **AND** output resolver MUST NOT 播放 Animancer 或 Animator

#### Scenario: Restore 只保存可恢复状态推进事实
- **WHEN** 捕获状态图 restore state
- **THEN** restore state MUST 保存重放所需的 active state、state time、variant、pending transition 和必要的状态 payload
- **AND** MUST NOT 保存 Unity 对象
- **AND** MUST NOT 保存可以从配置或 frame context 重新推导的表现层对象

### Requirement: 经典状态生命周期接口
系统 MUST 在自研统一分层状态机运行时内部提供经典 `Enter / Tick / Exit` 或等价生命周期接口。该接口 MUST 只读取纯数据 context、维护可恢复状态 payload、产出纯数据 frame 输出；接口实现 MUST NOT 直接执行运动、播放动画、消费 Unity 输入对象或写 Unity 场景对象。运行时对外仍 MUST 以单次 `Tick(context)` 产出一个 `CharacterStateMachineFrame` 或等价帧结果。

#### Scenario: Enter 产出进入状态的一次性输出
- **WHEN** transition 选择了新的目标状态
- **THEN** 运行时 MUST 调用目标状态的 `Enter` 或等价生命周期
- **AND** Enter MUST 能初始化 state time、variant、方向 payload、动画语义 key 和输入消费意图
- **AND** Enter MUST NOT 直接调用 Animancer、Animator、CharacterController、InputAction 或 Transform

#### Scenario: Tick 产出当前状态持续输出
- **WHEN** 当前状态在本帧保持 active
- **THEN** 运行时 MUST 调用 active 状态的 `Tick` 或等价生命周期
- **AND** Tick MUST 能产出当前帧运动、动画请求、timeline 相关输出和诊断事实
- **AND** Tick MUST 只产出纯数据，由 FullBody pipeline 执行副作用

#### Scenario: Exit 产出离开状态的一次性输出
- **WHEN** 当前状态要切换到目标状态
- **THEN** 运行时 MUST 在切换 active state 前调用旧状态的 `Exit` 或等价生命周期
- **AND** Exit MUST 能产出 run latch、清理 action payload、离开 TurnBack payload 或等价一次性输出
- **AND** Exit MUST NOT 直接清理动画 presenter 或执行 movement

#### Scenario: 对外仍是单 frame 输出
- **WHEN** 一帧内发生 transition
- **THEN** Exit、Enter 和 Tick 输出 MUST 合并为同一个 `CharacterStateMachineFrame` 或等价帧结果
- **AND** 调用方 MUST NOT 需要按 Enter/Exit/Tick 三条外部管线分别执行副作用

### Requirement: 状态机文档口径一致
系统 MUST 让项目文档、agent 指南和 OpenSpec 对角色状态机采用同一口径：角色正式主线是项目自研统一分层状态机，输入、运动、动画、相机和诊断为外围 adapter。文档 MUST NOT 继续建议后续角色业务状态机优先使用 UnityHFSM，除非新 proposal 明确批准迁移。

#### Scenario: Agent 指南不误导
- **WHEN** agent 阅读项目根文档和状态机指南
- **THEN** 文档 MUST 明确当前角色主线使用自研统一分层状态机
- **AND** MUST 明确 UnityHFSM 不是当前角色主线优先 engine
- **AND** MUST 明确如需改用 UnityHFSM 必须另开 OpenSpec proposal

#### Scenario: 架构文档不使用旧 BBB 主线
- **WHEN** agent 阅读 `openspec/project.md`
- **THEN** 文档 MUST NOT 把 `BBBCharacterController`、`PlayerStateRegistry`、`PlayerBaseState` 或 BBB `StateMachine` 描述为当前项目正式角色主线
- **AND** MUST 描述当前主线的 FullBody pipeline、自研分层状态机、motion executor 和 Animancer presenter 边界

#### Scenario: 文档保留预测回滚约束
- **WHEN** 文档描述状态机与预测回滚关系
- **THEN** 文档 MUST 说明状态机 restore、snapshot 和 replay 只使用纯数据事实
- **AND** MUST 说明不得为了网络或回滚新建第二套状态机路径

## MODIFIED Requirements
### Requirement: 逻辑状态后的动画转换配置
系统 MUST 允许逻辑状态节点或状态变体配置动画语义 key、timeline binding key 或等价稳定 ID，用于产出动画请求和匹配动画播放进度事实。具体动画播放配置 MUST 归属到 Animancer TransitionLibrary、`RunLocomotionAnimationConfigSO`、`ActionAnimationProfileSO` 或等价动画配置入口；逻辑状态机配置 MUST NOT 长期保存具体 `AnimationClip`、`TransitionAsset`、fade、speed、start time 或 Animancer runtime 对象作为状态机权威配置。

#### Scenario: Dodge 变体配置动画语义 key
- **WHEN** 设计者配置 `FullBody/Action/Dodge`
- **THEN** `Directional` 变体 MUST 能配置 `Action.Dodge.Directional` 或等价稳定动画语义 key
- **AND** `Backstep` 变体 MUST 能配置 `Action.Dodge.Backstep` 或等价稳定动画语义 key
- **AND** 具体 clip、transition asset、fade、speed 和 start time MUST 由动作动画 Profile 或等价动画配置解析

#### Scenario: Locomotion 状态配置 timeline binding key
- **WHEN** 设计者配置 `FullBody/Locomotion/TurnBack`
- **THEN** 状态机 MAY 保存 `Locomotion.Turn.Back` 或等价 timeline binding key
- **AND** 该 key MUST 只用于动画请求语义、播放进度事实匹配或 timeline window 采样
- **AND** 具体 Locomotion 动画资源和过渡参数 MUST 由基础移动动画配置或 Animancer TransitionLibrary 解析

#### Scenario: 动画不决定逻辑进入
- **WHEN** 动画外观 adapter 播放某个 Animancer transition
- **THEN** 它 MUST 只消费统一状态机产出的动画语义请求
- **AND** MUST NOT 决定 `Dodge` 是否允许进入
- **AND** MUST NOT 决定 `Dodge` 是否退出到 `MoveLoop` 或 `Idle`

#### Scenario: 动画事实回传为纯数据
- **WHEN** 状态 transition 需要等待动画可退出
- **THEN** 动画外观 adapter MUST 只回传 normalized time、is ended、alias key、action key 或等价纯数据 fact
- **AND** 统一状态机条件 MUST 读取这些 facts
- **AND** 统一状态机 MUST NOT 直接读取 Animancer state

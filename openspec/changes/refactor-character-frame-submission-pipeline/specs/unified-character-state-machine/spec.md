## MODIFIED Requirements
### Requirement: 删除分裂路径
系统 MUST 删除或降级旧 Locomotion 特化状态机、Dodge 特化 runtime 和 FullBody 外层缝合器，使正式角色运行时只保留自研统一分层状态机作为状态权威。Locomotion、Action、输入、运动和动画只能作为 Character frame pipeline 下的 facts、request submission 或 frame output submission 来源参与。

#### Scenario: Locomotion 特化状态机退役
- **WHEN** 统一状态机实现完成
- **THEN** 运行时代码 MUST NOT 再通过 `BasicLocomotionStateMachine` 或等价 Locomotion-only 状态机决定基础移动 phase
- **AND** 基础移动四阶段 MUST 由统一状态机配置表达

#### Scenario: Dodge 特化 runtime 退役
- **WHEN** 统一状态机实现完成
- **THEN** 运行时代码 MUST NOT 再通过 `DodgeActionRuntime` 或 `DodgeFullBodyActionModule` 决定 Dodge 生命周期
- **AND** Dodge 的进入、更新、完成和退出 MUST 由统一状态机状态、transition 和输出表达

#### Scenario: FullBody 缝合器退役
- **WHEN** 统一状态机实现完成
- **THEN** 运行时代码 MUST NOT 再通过仅包装 Locomotion 和 Action 的 `FullBodyHfsmStateTreeDriver` 或等价缝合器决定 owner
- **AND** FullBody owner 兼容事实 MUST 从统一状态机当前状态、输出和 winning submission 推导，且不得反向决定 transition 或输出应用

#### Scenario: Locomotion 自驱入口退役
- **WHEN** 当前角色通过正式 gameplay 路径运行
- **THEN** `PlayerLocomotionController` MUST NOT 独立读取输入后推进统一状态机 runner
- **AND** `PlayerLocomotionController` MUST 只向 Character frame pipeline 或 Locomotion builder 提供 Locomotion facts、运动命令构建输入和动画请求输入
- **AND** 任何保留的 Locomotion 直接 tick 入口 MUST 输出迁移诊断或仅用于测试，不得参与正式场景装配

### Requirement: 输出通道替代互斥 owner 分支
系统 MUST 将状态帧输出表达为 motion、animation、input、latch、timeline、runtime facts 等输出通道或等价纯数据结果。`Locomotion / Action` MAY 作为诊断或兼容事实从模块输出派生，但 MUST NOT 作为决定是否执行 motion、是否播放 animation、是否消费输入的互斥运行时分支权威。最终副作用 MUST 由 Character frame pipeline 的 output composer/applier 决定。

#### Scenario: Action 动画由输出通道驱动
- **WHEN** 当前节点通过模块产出动作动画请求
- **THEN** Character frame output composer/applier MUST 根据 animation output channel 或等价输出播放动作动画
- **AND** MUST NOT 仅通过 `Owner.IsAction` 判断是否播放动作动画

#### Scenario: Locomotion 动画由模块事实驱动
- **WHEN** 当前节点通过 Locomotion phase 模块产出基础移动表现请求
- **THEN** 动画 adapter MUST 使用 phase 与运行时 gait facts 解析具体基础移动动画
- **AND** 状态节点 MUST NOT 直接配置 Walk/Run 作为逻辑子状态

#### Scenario: 兼容 owner 只读派生
- **WHEN** 诊断或旧测试读取当前 owner
- **THEN** owner MAY 从当前节点模块组合派生
- **AND** 派生 owner MUST NOT 反向决定状态图 transition 或输出系统分支

### Requirement: 当前 runner 对模块模型的支撑边界
系统 MUST 在现有自研统一状态图 runner 上实现节点模块模型，而不是新增第二套状态机 runtime。现有 runner MAY 继续负责 active state、state time、variant、transition、pending path 和 restore；模块解析、输出聚合和事实采样 MUST 保持纯数据并位于明确 solver 子职责中。

#### Scenario: 保留单一 runner owner
- **WHEN** 模块化节点配置接入运行时
- **THEN** `PlayerFullBodyActionController` 或等价正式装配入口 MUST 继续是唯一正式 runner owner
- **AND** 系统 MUST NOT 新增 parallel ECS state runner、per-action runner 或独立 Locomotion runner

#### Scenario: Runner 不知道具体模块副作用
- **WHEN** runner 推进一帧状态
- **THEN** runner MUST NOT 直接播放 Animancer
- **AND** MUST NOT 直接执行 movement
- **AND** MUST NOT 直接消费 Unity 输入对象
- **AND** 模块输出 MUST 通过 `CharacterFrameSubmission` 或等价角色级帧输出提交进入 Character frame pipeline，由 output composer/applier 执行副作用

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
- **WHEN** request submission 仲裁或 transition 条件需要 timeline facts
- **THEN** 系统 MUST 通过独立 sampler 或等价纯数据模块提供 `StateTimelineWindowFacts`
- **AND** request provider 或 request arbiter MUST NOT 反向依赖 runner 的实现方法采样 timeline
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
- **AND** Tick MUST 只产出纯数据，由 Character frame pipeline 的 output composer/applier 执行副作用

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
系统 MUST 让项目文档、agent 指南和 OpenSpec 对角色状态机采用同一口径：角色正式主线是 Character frame pipeline 调度下的项目自研统一分层状态机，输入、运动、动画、相机和诊断为外围 adapter 或提交者。文档 MUST NOT 继续建议后续角色业务状态机优先使用 UnityHFSM，除非新 proposal 明确批准迁移。

#### Scenario: Agent 指南不误导
- **WHEN** agent 阅读项目根文档和状态机指南
- **THEN** 文档 MUST 明确当前角色主线使用自研统一分层状态机
- **AND** MUST 明确 UnityHFSM 不是当前角色主线优先 engine
- **AND** MUST 明确如需改用 UnityHFSM 必须另开 OpenSpec proposal

#### Scenario: 架构文档不使用旧 BBB 主线
- **WHEN** agent 阅读 `openspec/project.md`
- **THEN** 文档 MUST NOT 把 `BBBCharacterController`、`PlayerStateRegistry`、`PlayerBaseState` 或 BBB `StateMachine` 描述为当前项目正式角色主线
- **AND** MUST 描述当前主线的 Character frame pipeline、自研分层状态机、motion executor 和 Animancer presenter 边界

#### Scenario: 文档保留预测回滚约束
- **WHEN** 文档描述状态机与预测回滚关系
- **THEN** 文档 MUST 说明状态机 restore、snapshot 和 replay 只使用纯数据事实
- **AND** MUST 说明不得为了网络或回滚新建第二套状态机路径

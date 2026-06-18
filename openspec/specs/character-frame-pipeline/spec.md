# character-frame-pipeline Specification

## Purpose
定义唯一 `CharacterFramePipeline` 的帧阶段、请求提交、行为提交、`CharacterFramePlan` 合成和 output applier 副作用边界。
## Requirements
### Requirement: 唯一 Character Frame Pipeline
系统 MUST 只有一个正式角色帧管线拥有单个角色在一个 simulation tick 或兼容 frame tick 内的 phase 顺序。FullBody、Locomotion、Action、UpperBody、LowerBody 或其它身体域 MUST NOT 拥有独立 phase owner；它们只能在唯一角色帧管线指定的阶段提交请求候选或纯数据帧输出。旧 FullBody action controller MUST NOT 作为兼容入口、转发入口或旧 Tick owner 保留在正式运行时。

#### Scenario: CommittedAction 也通过唯一管线
- **GIVEN** 当前角色存在 CommittedAction 提交源
- **WHEN** 角色推进 tick N
- **THEN** 系统 MUST 通过 `CharacterFramePipeline` 或等价唯一角色帧管线推进
- **AND** CommittedAction MUST 作为 sibling submitter 参与该管线
- **AND** FullBody claim MUST NOT 自行拥有正式最高 phase 顺序

#### Scenario: 后续身体域只提交
- **GIVEN** 后续新增 UpperBody、LowerBody 或其它身体域
- **WHEN** 这些身体域参与 tick N
- **THEN** 它们 MUST 只向角色帧管线提交纯数据结果
- **AND** MUST NOT 自行执行 motion、播放动画、消费输入或写 runtime blackboard

#### Scenario: 旧 FullBody controller 不再作为兼容入口
- **WHEN** 旧 FullBody action controller tick、旧 FullBody tick adapter 或旧 rollback 入口仍被代码、测试、prefab 或 scene 引用
- **THEN** 实施 MUST 删除或迁移该引用
- **AND** 正式推进 MUST 进入 `CharacterFrameRuntimeController -> CharacterFrameRuntimeHost -> CharacterFramePipeline`
- **AND** 系统 MUST NOT 通过保留 controller 转发来延长第二入口寿命

### Requirement: 请求提交和打断仲裁
系统 MUST 在 Locomotion graph 和 Action lifecycle 推进前收集 request submission。外部请求、输入缓冲请求、Dodge、TurnBack、Attack、Jump 或其它动作候选 MUST 通过统一 request submission 进入请求/打断仲裁。request provider MUST 只提交请求候选，不得直接切 Locomotion graph、执行运动、播放动画、消费输入或写 runtime blackboard。accepted Action request MUST 进入 Action lifecycle submission，而不是要求默认 Locomotion graph 进入 Action state。

#### Scenario: 外部请求进入统一仲裁
- **WHEN** 外部系统或输入缓冲提交 Dodge、TurnBack、Attack、Jump 或等价请求候选
- **THEN** 该请求 MUST 被转换为 request submission
- **AND** MUST 进入统一请求/打断仲裁入口
- **AND** MUST NOT 直接变成 graph active state

#### Scenario: accepted Action request 输入 Action lifecycle
- **WHEN** 请求/打断仲裁接受一个 Dodge 或等价 Action 请求
- **THEN** 系统 MUST 生成 accepted resolved action、Action lifecycle seed 或等价纯数据 submission
- **AND** Action lifecycle MUST 通过该 submission active 对应 action
- **AND** accepted request 的输入消费 MUST 仍由后续帧输出和角色级 apply 阶段决定
- **AND** 默认 Locomotion graph MUST NOT 通过该 request 进入 `Action.Dodge`

#### Scenario: accepted Locomotion request 输入 Locomotion graph
- **WHEN** 请求/打断仲裁接受 TurnBack 或等价 Locomotion request
- **THEN** 系统 MAY 生成 Locomotion request fact
- **AND** Locomotion graph MAY 通过该 fact 评估 Locomotion transition
- **AND** 该 fact MUST NOT 表达 Action lifecycle active state

#### Scenario: rejected request 不产生副作用
- **WHEN** 请求/打断仲裁拒绝一个请求
- **THEN** 系统 MUST NOT 消费该请求
- **AND** MUST NOT 切换 graph 或 lifecycle state
- **AND** MUST NOT 执行 motion 或提交 animation

### Requirement: Character Frame Submission 模型
系统 MUST 使用 `CharacterFrameSubmission` 或等价 Character 语义提交模型表达各身体域或 adapter 的状态机后本帧结果。提交内容 MUST 是纯数据，MAY 包含状态帧、运动提案、动画提案、输入消费提案、runtime facts 提案、snapshot/events 提案和 diagnostics trace，但 MUST NOT 直接执行副作用。request submission MUST NOT 与 `CharacterFrameSubmission` 混用。

#### Scenario: 行为提交源提交当前结果
- **WHEN** 当前 Locomotion 或 CommittedAction 提交源完成本帧状态和运动构建
- **THEN** 它 MUST 产出 `CharacterFrameSubmission` 或等价角色级帧提交
- **AND** MUST 提交 `CharacterStateMachineFrame` 或等价状态结果
- **AND** MUST 提交 `BasicLocomotionFrame` 或等价基础移动结果
- **AND** MUST 提交 `ActionMotionResolveResult` 或等价动作运动结果
- **AND** 提交本身 MUST NOT 调用 motion executor 或 animation presenter

#### Scenario: CharacterFrameSubmission 不持有 Unity 场景对象
- **WHEN** 检查 `CharacterFrameSubmission` 或等价角色帧提交模型
- **THEN** 提交模型 MUST NOT 持有 `MonoBehaviour`
- **AND** MUST NOT 持有 `Transform`
- **AND** MUST NOT 持有 `CharacterController`
- **AND** MUST NOT 持有 Animancer runtime object
- **AND** MUST NOT 持有 `InputAction`

#### Scenario: 请求提交不混入帧输出提交
- **WHEN** 检查 `CharacterFrameSubmission` 或等价角色帧提交模型
- **THEN** 它 MUST NOT 表达 request priority、resistance、force 或 timing window 仲裁规则
- **AND** 请求准入 MUST 已经在状态机推进前完成

### Requirement: 输出合成先于输出应用
系统 MUST 在执行任何运动、动画、输入消费、Run latch 写入、runtime facts 写入或 snapshot/events commit 之前，先由角色级 output composer 合成本帧最终输出。第一版 composer MAY 只接收 Locomotion 与 CommittedAction 产生的最小 `CharacterFrameSubmission` 来源，但仍 MUST 是副作用应用前的唯一裁决位置。

#### Scenario: 单一候选来源仍经过 composer
- **GIVEN** 本帧只有一个行为提交源产出候选
- **WHEN** 角色帧管线进入输出合成阶段
- **THEN** composer MUST 从角色级提交中选择最终 movement 输出
- **AND** MUST 从角色级提交中选择最终 animation 输出
- **AND** MUST 从角色级提交中选择最终 input consume 输出
- **AND** MUST 从角色级提交中选择最终 runtime facts 输出
- **AND** MUST 从角色级提交中选择最终 Run latch 输出

#### Scenario: 副作用只在 apply 阶段发生
- **WHEN** 角色帧管线应用 composer 结果
- **THEN** motion executor 调用 MUST 只发生在角色级 output applier 或等价提交阶段
- **AND** animation presenter 调用 MUST 只发生在角色级 output applier 或等价提交阶段
- **AND** input buffer consume MUST 只发生在角色级 output applier 或等价提交阶段
- **AND** Run latch 写入 MUST 只发生在角色级 output applier 或等价提交阶段
- **AND** runtime blackboard 写入 MUST 只发生在角色级 output applier 或等价提交阶段

### Requirement: 旧集成路径兼容迁移行为保持
系统 MUST 在迁移到唯一角色帧管线时保持当前旧集成路径行为输出一致，同时采用新的 Locomotion graph 与 Action lifecycle 分离口径。Idle、MoveStart、MoveLoop、MoveStop、TurnBack、Directional Dodge 和 Backstep Dodge 的输入消费、运动执行、动画提交、runtime facts 和诊断 trace MUST 可测试；Dodge active state MUST 由 Action lifecycle 表达，不再要求默认 graph active path 为 `/FullBody/Action/Dodge`。

#### Scenario: 基础移动行为保持
- **WHEN** 使用相同 WASD 输入、相同配置和相同 tick 序列运行迁移前后路径
- **THEN** Idle、MoveStart、MoveLoop 和 MoveStop 的 Locomotion phase 序列 MUST 等价
- **AND** 基础移动运动命令来源 MUST 等价
- **AND** base layer animation 提交语义 MUST 等价

#### Scenario: Dodge 行为保持
- **WHEN** 使用相同 Dodge 输入、相同配置和相同 tick 序列运行迁移前后路径
- **THEN** Directional Dodge 和 Backstep Dodge 的 accepted/rejected 结果 MUST 等价
- **AND** 动作运动结果 MUST 等价
- **AND** Dodge active 时基础移动输出 MUST 不被重复提交
- **AND** Action lifecycle MUST 表达 active `Action.Dodge`
- **AND** 默认 Locomotion graph MUST NOT active `Action.Dodge`

#### Scenario: Directional 后续 Run 行为保持
- **GIVEN** 玩家有移动输入并按下 Shift 进入 Directional Dodge
- **AND** 动作完成帧仍有移动输入
- **WHEN** 输出应用完成该帧
- **THEN** pipeline MUST 将 Run latch frame output 写入 Locomotion output runtime
- **AND** 后续保持移动输入但松开 Shift 时 MUST 继续 Run

#### Scenario: 无移动或 Backstep 回 Idle
- **GIVEN** 玩家无方向按 Shift 进入 Backstep，或 Directional Dodge 完成帧没有移动输入
- **WHEN** Action lifecycle 等到匹配动作动画播放完成并完成动作
- **THEN** pipeline MUST NOT 写 Run latch
- **AND** Locomotion MUST 能回到 Idle

#### Scenario: TurnBack 行为保持
- **WHEN** 使用相同 RunLoop 反向输入、相同配置和相同 tick 序列运行迁移前后路径
- **THEN** TurnBack 进入、输入抑制、运动源策略和退出结果 MUST 等价
- **AND** TurnBack 运动不得新增第二运动出口

### Requirement: 不引入并行身体域
本变更 MUST 只建立唯一角色帧管线和提交模型，不得实现 UpperBody、LowerBody、Facial、IK、Additive、AvatarMask layer 或并行状态机。后续并行身体域 MUST 另开 OpenSpec 定义身体域职责、动画合成、权威压制和验证方式。

#### Scenario: 不创建 UpperBody 或 LowerBody runtime
- **WHEN** 实施本变更
- **THEN** 系统 MUST NOT 新增正式 UpperBody runtime
- **AND** MUST NOT 新增正式 LowerBody runtime
- **AND** MUST NOT 新增并行状态机调度规则

#### Scenario: 只预留提交接口
- **WHEN** 角色帧管线定义提交模型
- **THEN** 提交模型 MAY 预留来源标识和合成扩展点
- **AND** MUST NOT 在本变更中实现多身体域合成策略

### Requirement: 局部 Pipeline 直接改名
系统 MUST 在唯一角色帧管线中移除旧 FullBody 和 Locomotion 的正式 pipeline 命名。CommittedAction 侧正式提交入口 MUST 是 `CommittedActionFrameSubmitter`、`CharacterBehaviorSubmissionRunner` 下的 committed action leaf 或批准的等价提交构建器；Locomotion 侧正式入口 MUST 是 `LocomotionFrameBuilder` 或等价局部帧构建器。正式路径 MUST NOT 保留 obsolete pipeline 外壳作为 phase owner。

#### Scenario: CommittedAction 不再叫 FullBody Pipeline
- **WHEN** 实施唯一角色帧管线迁移
- **THEN** CommittedAction 侧正式职责 MUST 由 `CommittedActionFrameSubmitter`、`CharacterBehaviorSubmissionRunner` 下的 committed action leaf 或批准的等价提交构建器承担
- **AND** 该构建器 MUST NOT 拥有 phase switch
- **AND** 该构建器 MUST NOT 执行输出副作用

#### Scenario: Locomotion 不再叫 Pipeline
- **WHEN** 实施唯一角色帧管线迁移
- **THEN** Locomotion 侧正式职责 MUST 由 `LocomotionFrameBuilder` 或等价局部帧构建器承担
- **AND** 该构建器 MUST NOT 注册 tick handler
- **AND** 该构建器 MUST NOT 拥有角色级 phase 顺序

#### Scenario: Character Pipeline 不归属 FullBody 目录
- **WHEN** 检查角色级帧管线源码归属
- **THEN** `CharacterFramePipeline`、角色帧模型和 `ICharacterFrameRuntimePort` MUST 位于 `Assets/Scripts/Character/Pipeline/...` 或等价角色级目录
- **AND** `Assets/Scripts/Character/Action/FullBody/...` MUST NOT 保留 `CharacterFramePipeline`、`CharacterFramePipelineTypes` 或 `ICharacterFrameRuntimePort` 的正式文件

### Requirement: 角色级帧仲裁权威
系统 MUST 将正式目标架构定义为 Character 级 frame owner 驱动一帧。Locomotion、Action、body/channel claim、后续 UpperBody 或等价行为域 MUST 作为 sibling submitters 提交请求、事实、占用声明或候选输出；它们 MUST NOT 互相成为目标架构中的上级 owner。

#### Scenario: Character owner 汇集兄弟提交者
- **WHEN** 正式角色运行时处理一帧
- **THEN** Character frame owner MUST 汇集 Locomotion submitter 的移动事实和候选输出
- **AND** MUST 汇集 Action submitter 的 action facts、occupancy claim 和候选输出
- **AND** MUST NOT 要求 Locomotion 作为 FullBody submitter 的长期子 module 才能参与正式主线

#### Scenario: Action claim 参与输出选择
- **GIVEN** Action submitter 提交 full-body 或等价 body/channel claim
- **AND** Locomotion submitter 提交基础移动候选输出
- **WHEN** BodyArbiter 或等价仲裁 module 生成本帧计划
- **THEN** 计划 MAY 选择 Action 的 motion 或 animation candidate
- **AND** MAY 将 Locomotion 的 base layer motion 或 animation candidate 标记为本帧未采用
- **AND** 该选择 MUST 来自角色级仲裁结果
- **AND** MUST NOT 表达为 FullBody 直接拥有或停止 Locomotion runtime

#### Scenario: Pipeline 不保存业务优先级
- **WHEN** `CharacterFramePipeline` 执行本帧
- **THEN** pipeline MUST 消费 `CharacterFramePlan` 或等价纯数据计划
- **AND** pipeline MUST NOT 在自身核心逻辑中硬编码 Action、body/channel claim 或 Locomotion 的具体优先级树
- **AND** 身体占用、互斥和叠加规则 MUST 位于 BodyArbiter 或等价策略 module

### Requirement: CharacterFramePlan 先于新身体层
系统 MUST 在新增正式 UpperBody、HitReact、Aim 或等价身体层 runtime 前，先提供角色级 `CharacterFramePlan` 或等价一帧计划契约。该计划 MUST 能表达 `BaseSlot`、`UpperBodySlot` 或经批准的等价 slot owner，并且 MUST 区分 source、action、claim、slot、channel 与 presentation layer。新身体层 MUST 通过该计划参与 output composer/applier，不能直接绕过角色级管线。

`CharacterFramePlan` 的正式身体结果契约 MUST 使用 slot 口径。正式读取面 MUST 使用 `BaseSlotOwner`、`UpperBodySlotOwner` 和 `UpperBodySlotSuppressed` 或经批准的等价 slot contract。系统 MUST NOT 保留 `BaseLayerOwner`、`UpperBodyOwner` 或等价旧 layer 口径兼容属性。

#### Scenario: 新 UpperBody 需要计划契约
- **WHEN** 要实现 UpperBody Aim 或 UpperBody HitReact
- **THEN** 设计必须先定义它如何向 `CharacterFramePlan` 提交候选
- **AND** 定义它如何与 Locomotion / Action 的 body claim 合成或冲突

#### Scenario: Plan 是纯数据
- **WHEN** `CharacterFramePipeline` 生成一帧计划
- **THEN** 计划只能包含候选、claim、slot owner、权重、优先级、窗口、事件与输出意图等纯数据
- **AND** 不能直接执行动画播放、移动、IK 或黑板写入

#### Scenario: Output applier 仍唯一执行副作用
- **WHEN** 计划被提交到输出层
- **THEN** 只有既有 motion executor、animation presenter、blackboard writer 或经批准的 presenter/applier 可以执行副作用
- **AND** 不得新增第二 motion executor、第二 animation presenter 或第二 blackboard writer

#### Scenario: Plan 表达 slot 而不是表现层
- **WHEN** FullBody claim 赢得本帧仲裁
- **THEN** `CharacterFramePlan` MUST 表达 `BaseSlot` 由 Action-side owner 接管，并表达 `UpperBodySlot` 是否被压制
- **AND** 计划 MUST NOT 把 Animancer layer、timeline track、GraphView node 或 editor view 当作 gameplay slot

### Requirement: Character Frame Pipeline 只消费动作请求解析结果
`CharacterFramePipeline` MUST 只消费 request submission 阶段输出的纯数据结果。动作请求的收集、解析和准入 MUST 在 pipeline 的 request submission 边界内完成；pipeline 主体 MUST NOT 直接读取 Attack、Dodge、Jump 或 HitReact 配置，也 MUST NOT 直接决定这些动作的 target graph state、动画 key 或 motion spec。

#### Scenario: Pipeline 不认识具体动作解析
- **GIVEN** 本帧存在 Attack、Dodge 或 Jump 输入请求
- **WHEN** `CharacterFramePipeline` 执行 GameplayDecision 或等价 request submission phase
- **THEN** 具体动作解析 MUST 已由 provider/resolver 与 action arbiter 完成
- **AND** pipeline MUST 只接收 accepted resolved action、interrupt decision 或等价 pure data submission
- **AND** pipeline MUST NOT 新增具体动作解析分支

#### Scenario: 输出阶段不反推动作请求
- **GIVEN** request submission 已输出 accepted resolved action
- **WHEN** pipeline 进入 BuildMotion、ExecuteMotion、PresentationBridge 或 WriteSnapshotAndEvents
- **THEN** 输出阶段 MUST 只消费 lifecycle frame、motion result、animation request 和 runtime facts
- **AND** 输出阶段 MUST NOT 重新读取输入缓冲来决定 Attack、Dodge 或 Jump

#### Scenario: 没有第二条 action 入口
- **WHEN** 新动作通过通用 request provider/resolver 接入
- **THEN** 它 MUST 继续进入唯一 CharacterFramePipeline
- **AND** MUST NOT 新增第二 pipeline、第二 runner、第二 motion executor 或第二 animation presenter

### Requirement: Character runtime controller 驱动唯一角色帧
系统 MUST 将 `CharacterFrameRuntimeController` 或等价角色级 runtime controller 作为正式 Unity frame update 和 runtime tick 入口。`CharacterFramePipeline` MUST 继续是唯一角色帧管线；FullBody claim、Locomotion、Action 或其它身体域 MUST NOT 作为正式顶层 tick owner 直接推进 gameplay。

#### Scenario: Unity Update 从 Character 入口进入
- **GIVEN** 当前场景未启用 simulation tick driver
- **WHEN** Corin 正式 playable 角色在 frame update 中推进
- **THEN** tick MUST 从 `CharacterFrameRuntimeController` 或等价角色级入口进入
- **AND** MUST 进入同一个 `CharacterFramePipeline`
- **AND** MUST NOT 从 旧 FullBody action controller Update 作为正式主线进入

#### Scenario: Runtime Tick 从 Character 入口进入
- **GIVEN** 当前场景启用 simulation tick driver
- **WHEN** tick driver 推进角色 gameplay phase
- **THEN** phase handler MUST 调用 `CharacterFrameRuntimeController` 或等价角色级入口
- **AND** MUST 复用同一个角色帧 context 和 runtime host
- **AND** MUST NOT 通过 旧 FullBody action tick adapter 作为正式 registration owner

#### Scenario: 旧兼容入口必须删除或迁移
- **WHEN** 旧兼容 API、旧 FullBody action controller tick 或旧 FullBody tick adapter 仍被代码、测试、prefab 或 scene 引用
- **THEN** 实施 MUST 删除该入口或迁移引用到 `CharacterFrameRuntimeController` 或等价角色级入口
- **AND** MUST NOT 通过保留旧 controller 转发来延长第二入口寿命
- **AND** MUST NOT 维护独立 phase 顺序

### Requirement: 角色帧 Behavior Submission
系统 MUST 使用 Character 级 behavior submission runner 或等价组合 module 汇集本帧 sibling submitters。Locomotion submitter、Action submitter 和后续 UpperBody、HitReact、Aim 或其它 submitter MUST 作为兄弟提交者提交请求、事实、占用声明或候选输出。behavior submission runner MUST NOT 把 Locomotion 建模为 FullBody 的长期子职责。

#### Scenario: Locomotion 和 Action 并列提交
- **GIVEN** Locomotion submitter 产生基础移动候选输出
- **AND** Action submitter 产生 full-body 或等价 body/channel claim
- **WHEN** Character frame pipeline 收集本帧提交
- **THEN** 两者 MUST 作为 sibling submissions 进入 behavior submission runner
- **AND** MUST 由角色级 `BodyArbiter` 或等价 module 生成 `CharacterFramePlan`
- **AND** Action submitter MUST NOT 直接拥有 Locomotion runtime

#### Scenario: Future submitter 不塞回 integrated builder
- **WHEN** 后续新增 Attack、Jump、UpperBody、HitReact 或 Aim submitter
- **THEN** 新 submitter MUST 接入 Character 级 behavior submission runner
- **AND** MUST NOT 被塞入 旧 FullBody integrated frame adapter
- **AND** MUST NOT 要求旧 FullBody controller 成为上级 owner

#### Scenario: Graph 不执行副作用
- **WHEN** behavior submission runner 收集和合并本帧请求或候选输出
- **THEN** runner MUST 只产生纯数据 submission、claim、candidate 或 plan input
- **AND** MUST NOT 调用 motion executor
- **AND** MUST NOT 调用 animation presenter
- **AND** MUST NOT 消费 input buffer 或写 runtime blackboard

### Requirement: Plan 合成消费兄弟候选
系统 MUST 让 `CharacterFramePlan` 或等价角色级计划表达 sibling submitters 的最终身体占用、输出选择和未采用原因。最终运动、动画、输入消费、runtime facts 和 diagnostics 的应用 MUST 发生在统一 output applier 阶段。

#### Scenario: Action claim 选择 Action 输出
- **GIVEN** Locomotion submitter 提交基础移动 motion 和 animation candidate
- **AND** Action submitter 提交 full-body 或等价 body/channel claim
- **WHEN** `BodyArbiter` 生成本帧 `CharacterFramePlan`
- **THEN** plan MAY 选择 Action motion candidate
- **AND** plan MAY 选择 Action animation candidate
- **AND** MAY 标记 Locomotion candidate 本帧未采用
- **AND** 该选择 MUST 来自 Character 级计划而不是 FullBody 私有字段

#### Scenario: Output applier 是唯一副作用出口
- **WHEN** `CharacterFramePlan` 选择本帧最终输出
- **THEN** motion executor 调用 MUST 只发生在 Character output applier 或等价角色级输出应用阶段
- **AND** animation presenter 调用 MUST 只发生在 Character output applier 或等价角色级输出应用阶段
- **AND** submitter、graph 和 arbiter MUST NOT 直接执行副作用

### Requirement: 退役单一 FullBody frame submission 权威
系统 MUST 将 `CharacterFrameSubmissionSource.FullBody` 或等价单一 FullBody 来源从正式 output authority 中退役。迁移期可以继续用 legacy adapter 转换旧提交，但最终运动、动画、输入消费、runtime facts 和 diagnostics 的正式选择 MUST 来自 `CharacterFramePlan` 或等价角色级计划。

#### Scenario: Plan 是正式输出选择
- **GIVEN** Locomotion 和 Action 已提交候选输出或 occupancy claim
- **WHEN** output composer 生成本帧结果
- **THEN** composer MUST 以 `CharacterFramePlan` 或等价角色级计划表达最终选择
- **AND** MUST NOT 以 `CharacterFrameSubmissionSource.FullBody` 作为最终输出权威

#### Scenario: Legacy submission 只作为迁移输入
- **GIVEN** 当前实现仍需要 `CharacterFrameSubmission` 承载旧集成结果
- **WHEN** 该 submission 进入 output composer
- **THEN** composer MAY 将它转换为 `CharacterFramePlan`
- **AND** 该路径 MUST 被标记为 legacy 或 integrated adapter
- **AND** 后续新增身体域 MUST NOT 依赖任何单一旧 source 参与正式仲裁

### Requirement: Output composer 不得长期保持 pass-through
系统 MUST 让角色级 output composer 承担 plan 合成或 plan 选择职责。若保留 `Compose(CharacterFrameSubmission)` 或等价 legacy overload，它 MUST 只作为迁移 Adapter，并且 MUST 有自动测试覆盖其删除条件。

#### Scenario: Composer 消费 plan
- **WHEN** 正式角色帧管线进入 BuildMotion 或等价 plan build 阶段
- **THEN** output composer MUST 能消费 `CharacterFramePlan` 或等价角色级计划
- **AND** MUST 保留 body occupancy、motion 选择、animation 选择、input consume 和 runtime facts 的最终选择结果

#### Scenario: Legacy overload 有删除条件
- **WHEN** 代码中仍存在从单个旧 submission 到 output 的 overload
- **THEN** 测试 MUST 标记该 overload 为 legacy adapter
- **AND** MUST 证明正式 plan path 已覆盖 Corin 当前 Locomotion 与 Action 主线
- **AND** 后续迁移完成后该 overload MUST 被删除或移出正式运行时路径

### Requirement: 角色级管线不承担身体域退役策略
`CharacterFramePipeline` MUST 继续只负责 phase 顺序、调用 submitter/composer/applier 和传播结果。旧 FullBody 集成路径退役、Locomotion submitter 拆分、CommittedAction submitter 拆分和 body occupancy 规则 MUST 位于独立 module 或 spec 约束中，不得写成 pipeline 本体的特殊分支。

#### Scenario: Pipeline 不硬编码退役分支
- **WHEN** 检查 `CharacterFramePipeline` 核心逻辑
- **THEN** pipeline MUST NOT 通过具体旧 FullBody builder 类型判断退役路径
- **AND** MUST NOT 通过具体 `CharacterFrameSubmissionSource.FullBody` 判断最终输出
- **AND** MUST NOT 在 phase switch 中写入 Action、body/channel claim 或 Locomotion 的业务优先级

### Requirement: CommittedAction 抢占 Locomotion transient 的帧事实
系统 MUST 在 Character frame pipeline 内提供纯数据 Locomotion preemption fact，用于表达 CommittedAction 已抢占当前 Locomotion transient motion source。该 fact MUST 由 submitter、plan、output 或等价 frame data contract 传递，不得通过 pipeline 核心硬编码具体 Action 或具体 Locomotion 状态完成状态切换。

#### Scenario: FullBody claim 被 CommittedAction 采纳时产出事实
- **GIVEN** Locomotion submitter 已提交 `Locomotion.TurnBack` 候选输出
- **AND** Action submitter 在同一帧开始 `Action.Dodge`
- **AND** `Action.Dodge` 的 full-body claim 被接受
- **WHEN** Character frame pipeline 生成本帧 plan/output
- **THEN** plan/output MUST 继续压制 Locomotion motion output
- **AND** plan/output MUST 携带一次性 Locomotion preemption fact
- **AND** preemption fact MUST 记录 source locomotion state、source action id 和 source step
- **AND** pipeline 本体 MUST NOT 直接切换 Locomotion state

#### Scenario: 非 transient Locomotion 不产生抢占事实
- **GIVEN** Locomotion submitter 当前处于 `Locomotion.Idle`、`Locomotion.MoveLoop` 或等价非 transient motion source
- **AND** Action submitter 开始 full-body action
- **WHEN** Character frame pipeline 生成本帧 plan/output
- **THEN** plan/output MAY 压制 Locomotion motion 或 animation output
- **AND** plan/output MUST NOT 产生 TurnBack preemption fact

#### Scenario: Pipeline 不认识 Dodge 与 TurnBack 细节
- **WHEN** 检查 `CharacterFramePipeline` 核心 phase 顺序代码
- **THEN** pipeline MUST 只调用 submitter、composer、applier 和 runtime port
- **AND** pipeline MUST NOT 通过 `Action.Dodge` 字符串判断是否抢占
- **AND** pipeline MUST NOT 通过 `Locomotion.TurnBack` 字符串执行状态切换

### Requirement: 角色帧主线由 Runtime Core 持有
`CharacterFramePipeline` MUST 仍是唯一角色帧 gameplay 输出主线，但其正式 host ownership MUST 位于 `CharacterRuntimeCore` 或批准的等价纯 C# owner。Unity MonoBehaviour MUST 只能作为 tick adapter 或 dependency composition adapter 调用该 core。

#### Scenario: Unity Update 通过 Core 进入主线
- **GIVEN** 正式角色 prefab 已装配 `CharacterFrameRuntimeController`
- **WHEN** Unity Update 或外部 tick driver 推进一帧
- **THEN** Mono adapter MUST 调用同一个 `CharacterRuntimeCore`
- **AND** core MUST 推进同一个 `CharacterFramePipeline`
- **AND** Locomotion、Action、motion、animation 和 diagnostics 输出 MUST 继续经过同一个 `CharacterFramePlan` 或批准的等价计划

#### Scenario: 不新增第二角色帧循环
- **WHEN** 新增或迁移 Locomotion、Action、rollback replay 或测试 fixture
- **THEN** 新代码 MUST NOT new 独立 `CharacterFramePipeline` 作为生产路径
- **AND** MUST NOT 通过额外 MonoBehaviour Update 直接应用正式 gameplay 输出
- **AND** MUST NOT 绕过 core-owned host 执行 motion 或 animation 副作用

#### Scenario: Phase 顺序保持可测试
- **WHEN** EditMode 测试用 fake dependencies 推进 core tick
- **THEN** request submission、frame submission、plan/composition 和 output application 的顺序 MUST 与现有 `CharacterFramePipeline` 合同一致
- **AND** 测试 MUST 不依赖 scene instance 才能验证顺序

### Requirement: Sibling Submitter 边界
角色帧管线 MUST 将 Locomotion 与 Action 建模为兄弟提交者。Locomotion submitter MUST 只提交 Locomotion motion、animation、facing、camera 或 locomotion facts 候选；Action submitter MUST 只提交 action request、action motion、action animation、occupancy 或 resolved action facts 候选。系统 MUST NOT 通过单个 FullBody 命名 builder 同时构建 Locomotion 与 Action 的正式输出。

#### Scenario: Locomotion 与 Action 独立提交
- **GIVEN** tick N 同时存在 Locomotion 输入和 Dodge 请求
- **WHEN** `CharacterBehaviorSubmissionRunner` 构建提交
- **THEN** Locomotion submitter MUST 提交 Locomotion 候选
- **AND** Action submitter MUST 提交 Dodge 或等价 action 候选
- **AND** 两者 MUST 由 `CharacterFramePipeline` 仲裁
- **AND** 任一 submitter MUST NOT 通过共享旧集成 builder 替另一个 submitter 决定 winning output

### Requirement: Frame Output Source 不表达旧 FullBody 权威
角色帧输出来源 MUST 表达角色级候选、仲裁结果或具体提交域，而不是表达旧 FullBody 集成路径权威。正式路径 MUST NOT 继续使用 `LegacyFullBodyIntegrated` 作为 winning frame output source、diagnostic authority 或测试断言的正式身份。

#### Scenario: Winning frame source 来自角色级仲裁
- **WHEN** `CharacterFramePipeline` 产出 tick N 的 `CharacterFramePlan`
- **THEN** plan MUST 能说明 winning motion、animation 和 facts 来自角色级仲裁后的候选
- **AND** 输出来源 MUST NOT 被标记为 `LegacyFullBodyIntegrated`
- **AND** diagnostics MAY 显示具体 submitter 名称，但 MUST NOT 把旧 FullBody 集成路径标记为正式来源

### Requirement: Motion Warping 结果作为候选输出参与 Plan
Motion Warping result MUST 在角色级输出应用前被转换为 motion candidate 或等价 frame submission 数据，并参与 `CharacterFramePlan` 或批准的等价角色级计划。Character frame output applier MUST 只执行计划选择后的 warped motion，不得在 output apply 阶段临时解析 warp target 或运行 solver。

#### Scenario: Action warped motion 进入提交
- **GIVEN** Action Motion clip 通过 Motion Warping solver 生成 action motion result
- **WHEN** Action submitter 构建本帧 `CharacterFrameSubmission`
- **THEN** submission MUST 携带该 action motion candidate
- **AND** BodyArbiter 或等价 plan builder MUST 能决定该 candidate 是否成为本帧最终 motion
- **AND** output applier MUST 不重新运行 solver

#### Scenario: Action 使用共享 solver result 但保留 Action command
- **GIVEN** MotionWarpSolver 为 Action 攻击吸附或转向修正输出 MotionWarpResult
- **WHEN** Action motion resolve 构建本帧提交
- **THEN** 该 result MUST 被适配为 `ActionMovementCommand` 或批准的等价 Action motion candidate
- **AND** 系统 MUST NOT 要求 `MovementCommand` 与 `ActionMovementCommand` 在本变更中合并

#### Scenario: Locomotion warped motion 进入提交
- **GIVEN** Locomotion 状态通过动画运动源或 Motion Warping solver 生成 movement facts
- **WHEN** Locomotion submitter 构建本帧候选输出
- **THEN** movement facts MUST 进入 Locomotion motion candidate 或等价 frame data
- **AND** 最终是否执行 MUST 服从 `CharacterFramePlan`

#### Scenario: Output apply 不解析 target
- **WHEN** output applier 执行本帧 motion
- **THEN** 它 MUST 只消费已经求解好的 command 或 motion result
- **AND** MUST NOT 解析 warp target binding
- **AND** MUST NOT 查询场景目标
- **AND** MUST NOT 读取 ActionTimeline clip payload 来补算 motion

### Requirement: Motion Warping 不改变角色帧 phase 顺序
引入 Motion Warping MUST 不改变唯一 `CharacterFramePipeline` 的 phase owner 或输出副作用顺序。request submission、state/lifecycle 推进、motion resolve、plan 合成和 output apply 的职责 MUST 保持分离。

#### Scenario: Motion resolve 在 output apply 前完成
- **GIVEN** 本帧存在需要 Motion Warping 的 Action 或 Locomotion motion intent
- **WHEN** 角色帧管线进入 output compose / plan 阶段
- **THEN** warp result MUST 已经作为候选纯数据存在
- **AND** output apply 阶段 MUST 只应用最终计划选择的结果

#### Scenario: 不新增第二帧循环
- **WHEN** 新增 Motion Warping runtime 代码
- **THEN** 系统 MUST NOT 新增 MonoBehaviour Update、独立 tick adapter、第二 `CharacterFramePipeline` 或第二 output applier 来驱动 warped motion
- **AND** 正式推进 MUST 继续从现有角色帧主线进入

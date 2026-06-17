# fullbody-action-framework Delta

## MODIFIED Requirements
### Requirement: FullBody Action Module
系统 MUST 提供 FullBody Action module 端口，使全身动作能通过统一请求、仲裁、lifecycle、body claim、运动候选和动画候选接入 Character frame pipeline。Action module MUST 是角色帧管线下的 Action 领域模块，不得成为独立角色控制路径，不得作为 Locomotion 的父级 owner，也不得依赖默认 Locomotion graph 中的 `Action.*` 节点表达生命周期。

#### Scenario: Module 不是默认状态图叶子
- **WHEN** 系统注册或执行 FullBody Action module
- **THEN** module MUST 作为 Character frame pipeline 下的 Action 领域模块存在
- **AND** MUST NOT 要求默认 Locomotion graph 包含 `Action.Dodge`
- **AND** MUST NOT 要求默认 Locomotion graph 包含 `FullBody/Action/Dodge`
- **AND** MUST NOT 决定 Locomotion phase
- **AND** MUST NOT 形成独立 MonoBehaviour gameplay tick 路径

#### Scenario: Module 使用 Action 仲裁
- **GIVEN** 输入缓冲存在一个 FullBody Action 请求
- **WHEN** module 尝试进入动作
- **THEN** module MUST 通过 `ActionInterruptArbiter` 或等价 Action 仲裁判断是否允许进入
- **AND** accepted 时 MUST 创建或更新 Action lifecycle facts
- **AND** rejected 时 MUST 不消费未过期请求

#### Scenario: Module 输出候选而不直接执行
- **WHEN** module active tick 产生动作位移或动作动画
- **THEN** module MUST 输出纯数据 action motion candidate 或等价命令
- **AND** MUST 输出动作动画 key/command 或等价命令
- **AND** MUST 输出 body claim 或等价占用声明
- **AND** MUST NOT 直接调用 `CharacterController.Move`
- **AND** MUST NOT 直接调用 Animancer 或 Animator 播放 API

#### Scenario: Module 显式退出
- **GIVEN** module 当前 active action 为 `Action.Dodge`
- **WHEN** Action lifecycle 达到自身退出条件
- **THEN** module MUST 显式清空 active action 或进入等价 inactive state
- **AND** body claim MUST 随 lifecycle exit 释放
- **AND** lifecycle MUST NOT 依赖默认状态图 active state 从 `Action.Dodge` 切回 Locomotion

### Requirement: FullBody Action Provider 与 Resolver 分离
系统 MUST 将动作请求候选收集与动作解析拆分为独立接口。request provider MUST 只负责从输入缓冲、外部请求或 runtime facts 生成动作请求候选；request resolver MUST 负责基于动作请求、当前纯数据上下文和正式配置解析出可仲裁的 resolved action。arbiter 主流程 MUST NOT 通过硬编码分支把 Attack、Dodge、Jump 或 HitReact 输入直接映射到默认状态图 target state。

#### Scenario: Resolver 输出 resolved action
- **GIVEN** provider 输出了一个有效动作请求
- **WHEN** request resolver 消费该请求、当前状态上下文和正式配置
- **THEN** resolver MAY 输出 action id、variant、request fact、interrupt request、animation seed、motion seed 和 lifecycle seed
- **AND** resolver 输出 MUST 保持纯数据
- **AND** resolver MUST NOT 输出默认 Locomotion graph target state
- **AND** resolver MUST NOT 读取 Unity scene object、Animancer runtime 或 InputAction

#### Scenario: 新动作不修改 arbiter 主流程
- **WHEN** 后续新增 Attack、Jump 或 HitReact
- **THEN** 新动作 MUST 通过新增 provider 和 resolver 接入
- **AND** `CharacterActionRequestSubmissionArbiter` 或等价主流程 MUST NOT 新增直接面向具体动作的 target-state switch
- **AND** 多动作候选仍 MUST 使用统一 priority、resistance、timing window 和稳定 tie-break 规则

### Requirement: Dodge 通过通用请求解析路径保持行为
现有 Dodge 行为 MUST 迁移到通用 action request provider/resolver 和 Action lifecycle 路径。Dodge provider MUST 只提交 Dodge 请求；Dodge resolver MUST 解析 directional/backstep variant、world direction、priority、animation seed、motion seed、claim policy key 和 lifecycle seed。迁移后 directional dodge、backstep、rejected request 保留和输入消费语义 MUST 与迁移前一致，但 Dodge MUST NOT 作为默认 Locomotion graph 节点存在。

#### Scenario: Directional Dodge 行为保持
- **GIVEN** 输入缓冲中存在 Dodge 输入且当前移动事实支持 directional dodge
- **WHEN** 通用 provider/resolver 路径处理该请求
- **THEN** Dodge resolver MUST 输出 directional dodge resolved action
- **AND** accepted 后 Action lifecycle MUST active `Action.Dodge`
- **AND** motion seed 和 animation seed MUST 与迁移前等价
- **AND** motion seed MAY 标记 Directional 完成时可写 Run latch，但最终写入 MUST 由完成帧移动输入事实决定
- **AND** Directional 完成且 frame output 请求 Run latch 时，输出应用 MUST 通过 Locomotion output runtime 端口写入正式 Run latch state，不得只写 Action facts
- **AND** 默认 Locomotion graph active state MUST NOT 变为 `Action.Dodge`

#### Scenario: Backstep Dodge 行为保持
- **GIVEN** 输入缓冲中存在 Dodge 输入且当前移动事实支持 backstep
- **WHEN** 通用 provider/resolver 路径处理该请求
- **THEN** Dodge resolver MUST 输出 backstep dodge resolved action
- **AND** accepted 后 Action lifecycle MUST active `Action.Dodge`
- **AND** motion seed 和 animation seed MUST 与迁移前等价
- **AND** 无移动输入时，Action lifecycle MUST 等待匹配 `Action.Dodge.Backstep` 动作动画播放完成后才释放 claim
- **AND** motion duration MUST 只表达 Backstep 动作位移窗口，不得单独作为无输入 Backstep 的 lifecycle exit 条件
- **AND** 默认 Locomotion graph active state MUST NOT 变为 `Action.Dodge`

#### Scenario: rejected request 保留
- **GIVEN** 输入缓冲中存在未过期 Dodge 请求
- **AND** Action 仲裁拒绝该请求
- **WHEN** FullBody Action module 完成本帧处理
- **THEN** 请求 MUST NOT 被消费
- **AND** Action lifecycle MUST NOT active `Action.Dodge`
- **AND** Locomotion graph MUST 继续只表达 Locomotion state

### Requirement: FullBody Action 作为兄弟 Submitter
FullBody Action framework MUST 在目标架构中作为 Character frame owner 下的 sibling submitter 存在。它 MUST 提交动作请求、Action lifecycle facts、full-body occupancy claim、action motion candidate 和 action animation candidate。它 MUST NOT 作为正式 Unity tick 入口、Character runtime host owner、Locomotion 上级 owner 或默认 Locomotion graph 的状态叶子。

#### Scenario: Dodge 通过 FullBody Action submitter 提交
- **GIVEN** 输入缓冲中存在有效 Dodge 请求
- **WHEN** Character frame pipeline 收集 FullBody Action submitter 输出
- **THEN** FullBody Action submitter MUST 提交 Dodge action request 或 resolved action candidate
- **AND** MUST 提交 full-body occupancy claim
- **AND** MUST 提交 Action lifecycle facts
- **AND** MUST NOT 直接执行 Dodge movement
- **AND** MUST NOT 直接播放 Dodge animation
- **AND** MUST NOT 要求 Locomotion graph 进入 `Action.Dodge`

#### Scenario: FullBody 不拥有 Locomotion
- **GIVEN** Locomotion submitter 已提交基础移动候选输出
- **AND** FullBody Action submitter 已提交 full-body occupancy claim
- **WHEN** CharacterFramePlan 压制 Locomotion 输出
- **THEN** 压制 MUST 来自角色级计划
- **AND** FullBody Action framework MUST NOT 写 Locomotion runtime 私有状态来表达压制
- **AND** FullBody Action framework MUST NOT 调用 Locomotion output runtime 直接执行压制

#### Scenario: Future Action 不新增入口
- **WHEN** 后续新增 Attack、Jump 或 HitReact
- **THEN** 新动作 MUST 通过 FullBody Action submitter、action provider/resolver 或等价 sibling submitter 接入
- **AND** MAY 使用已批准的 Action 局部 graph implementation
- **AND** MUST NOT 新增 `PlayerAttackController`、`PlayerJumpController` 或等价 MonoBehaviour 作为正式 gameplay tick 入口

### Requirement: FullBody Frame Pipeline
系统 MUST 将当前 FullBody 一帧编排降级为角色帧管线下的提交职责。FullBody MAY 继续复用 Locomotion graph、Action lifecycle、Locomotion facts、Action 仲裁、motion spec resolver 和 Animancer presenter adapter，但正式最高 phase 顺序 MUST 归属唯一 Character frame pipeline。FullBody 提交职责 MUST NOT 自行执行运动、播放动画、消费输入缓冲或写 runtime blackboard。

#### Scenario: 一帧步骤由 Character 管线显式
- **WHEN** 当前角色推进 tick N
- **THEN** 系统 MUST 由唯一 Character frame pipeline 依次处理输入事实、输入请求缓冲、Locomotion facts、Action 请求仲裁、Locomotion graph tick、Action lifecycle tick、运动候选构建、输出合成、输出应用、runtime facts 写入和 snapshot/events commit
- **AND** 每个步骤 MUST 能通过测试观察到输入输出

#### Scenario: FullBody 提交不拥有状态权威
- **WHEN** FullBody 提交职责处理 GameplayDecision
- **THEN** Locomotion phase MUST 由 Movement module 的 Locomotion graph 或等价局部 implementation 决定
- **AND** Action active state MUST 由 Action lifecycle 或等价 Action implementation 决定
- **AND** FullBody 提交职责 MUST NOT 创建独立 Action 状态机
- **AND** FullBody 提交职责 MUST NOT 创建独立 Locomotion 状态机

#### Scenario: FullBody 提交不绕过运动出口
- **WHEN** FullBody 提交职责产生 movement submission
- **THEN** 它 MUST 只输出纯数据运动结果或运动提案
- **AND** MUST NOT 直接调用 `CharacterController.Move`
- **AND** MUST NOT 直接写角色 `Transform.position`
- **AND** MUST NOT 直接调用 motion executor

### Requirement: FullBody 集成提交器降级
当前 `FullBodySubmissionBuilder`、`FullBodyIntegratedFrameAdapter` 或等价 integrated submitter MUST 被定义为迁移期 Adapter，而不是 FullBody Action framework 的长期正式入口。它 MAY 暂时汇集 Locomotion、FullBody Action、状态图和 motion resolve 数据，但新增身体域 MUST NOT 继续扩展该 Module。

#### Scenario: Integrated submitter 不接新身体域
- **WHEN** 后续新增 UpperBody、HitReact、Aim 或等价身体域
- **THEN** 新身体域 MUST 作为 Character-level sibling submitter 接入
- **AND** MUST NOT 被塞进 `FullBodySubmissionBuilder`
- **AND** MUST NOT 读取 FullBody integrated submitter 的私有状态作为上级权威

#### Scenario: FullBody submitter 只提交动作候选
- **WHEN** FullBody Action submitter 处理 Dodge、Attack 或等价全身动作
- **THEN** 它 MUST 提交 action request、occupancy claim、action motion candidate 和 action animation candidate
- **AND** MUST NOT 直接执行 motion
- **AND** MUST NOT 直接播放 animation
- **AND** MUST NOT 直接消费 Locomotion 输出

## MODIFIED Requirements
### Requirement: Body claim policy 独立配置
系统 MUST 将全身动作的 body claim 规则归属独立 `BodyClaimPolicySO` 或等价 BodyClaim policy 配置。Action config MAY 引用 policy key 或 policy asset，但默认 claim 规则 MUST NOT 隐藏在 Locomotion graph、Action lifecycle runtime 常量或状态图层级里。

#### Scenario: Dodge claim 通过 policy 解析
- **GIVEN** Dodge resolved action 需要 full-body occupancy
- **WHEN** FullBody Action submitter 构建本帧 claim
- **THEN** submitter MUST 通过 `BodyClaimPolicySO` 或等价正式配置解析 claim
- **AND** claim MUST 作为纯数据提交给 Body Arbiter
- **AND** Locomotion graph MUST NOT 通过层级 owner 表达 Dodge claim

#### Scenario: policy 缺失时报错
- **GIVEN** Action config 引用的 claim policy 缺失
- **WHEN** 正式 gameplay 路径需要该 policy
- **THEN** 系统 MUST 报告明确配置错误
- **AND** MUST NOT 使用隐藏默认 full-body claim

### Requirement: Action 局部 graph 为可选 implementation
系统 MAY 允许复杂 Action 在 Action module 内部使用局部 graph implementation，但该 graph MUST 只表达该 Action 的内部阶段，不得成为默认角色状态图、Locomotion graph 子节点或新的 gameplay tick 入口。Dodge 默认实现 MUST 不要求局部 action graph。

#### Scenario: Dodge 不要求局部 graph
- **WHEN** 默认 Dodge action 运行
- **THEN** Dodge MAY 只使用 Action lifecycle 数据推进
- **AND** MUST NOT 要求存在 Dodge action graph asset
- **AND** MUST NOT 回退到默认 Locomotion graph 的 `Action.Dodge` 节点

#### Scenario: 复杂 Action 可使用局部 graph
- **WHEN** 后续 Attack 或其它复杂 Action 被批准使用局部 graph
- **THEN** 该 graph MUST 位于 Action module 内部
- **AND** MUST 通过 Action lifecycle 或等价接口提交 action facts、claim、motion candidate 和 animation candidate
- **AND** MUST NOT 直接 tick CharacterFramePipeline
- **AND** MUST NOT 直接驱动 Locomotion graph

### Requirement: Dodge Run latch 输出契约
系统 MUST 将 Shift Dodge 后续奔跑定义为 Action output 与 Locomotion runtime 的协作结果。Shift MAY 同时提供 Dodge request 与 Run input fact，但 Directional Dodge 完成后的持续 Run MUST 由 Action motion completion 产生的 frame output 写入 Locomotion runtime Run latch；Action facts、输入绑定或按住 Shift 均不得成为该持续 Run 的唯一权威。

#### Scenario: Directional 完成且仍移动时写 Run latch
- **GIVEN** 当前 Action lifecycle active action 为 `Action.Dodge`
- **AND** 当前变体为 `Directional`
- **AND** 动作完成帧仍存在移动输入
- **WHEN** Action motion resolver 判定动作完成
- **THEN** Action output MUST 产生写 Run latch 的 frame output
- **AND** 输出应用 MUST 通过 Locomotion output runtime 端口写入正式 Run latch state
- **AND** 后续保持移动输入时 Locomotion MUST 能以 Run 继续，即使 Shift 已松开

#### Scenario: Directional 完成但无移动时不写 Run latch
- **GIVEN** 当前 Action lifecycle active action 为 `Action.Dodge`
- **AND** 当前变体为 `Directional`
- **AND** 动作完成帧没有移动输入
- **WHEN** Action motion resolver 判定动作完成
- **THEN** Action output MUST NOT 写 Run latch
- **AND** Action lifecycle MUST 等待匹配 `Action.Dodge.Directional` 动作动画播放完成后释放 claim
- **AND** 后续 Locomotion MUST 能回到 Idle

#### Scenario: Backstep 完成不写 Run latch
- **GIVEN** 当前 Action lifecycle active action 为 `Action.Dodge`
- **AND** 当前变体为 `Backstep`
- **AND** 本帧没有移动输入
- **WHEN** 匹配 `Action.Dodge.Backstep` 动作动画播放完成
- **THEN** Action output MUST NOT 写 Run latch
- **AND** Action lifecycle MUST 能释放 claim
- **AND** 后续 Locomotion MUST 能回到 Idle

#### Scenario: Action facts 不是 Run latch 权威
- **WHEN** Action output 写入 runtime facts
- **THEN** Action facts MAY 记录动作完成、位移、方向和诊断信息
- **AND** Action facts MUST NOT 作为 Locomotion Run latch 的唯一状态来源
- **AND** Run latch 的正式状态 MUST 由 Locomotion runtime state 持有

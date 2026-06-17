## MODIFIED Requirements

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

### Requirement: 角色帧 Submitter Graph
系统 MUST 使用 Character 级 submitter graph 或等价组合 module 汇集本帧 sibling submitters。Locomotion submitter、Action submitter 和后续 UpperBody、HitReact、Aim 或其它 submitter MUST 作为兄弟节点提交请求、事实、占用声明或候选输出。submitter graph MUST NOT 把 Locomotion 建模为 FullBody 的长期子职责。

#### Scenario: Locomotion 和 Action 并列提交
- **GIVEN** Locomotion submitter 产生基础移动候选输出
- **AND** Action submitter 产生 full-body 或等价 body/channel claim
- **WHEN** Character frame pipeline 收集本帧提交
- **THEN** 两者 MUST 作为 sibling submissions 进入 submitter graph
- **AND** MUST 由角色级 `BodyArbiter` 或等价 module 生成 `CharacterFramePlan`
- **AND** Action submitter MUST NOT 直接拥有 Locomotion runtime

#### Scenario: Future submitter 不塞回 integrated builder
- **WHEN** 后续新增 Attack、Jump、UpperBody、HitReact 或 Aim submitter
- **THEN** 新 submitter MUST 接入 Character 级 submitter graph
- **AND** MUST NOT 被塞入 `FullBodyIntegratedFrameAdapter`
- **AND** MUST NOT 要求旧 FullBody controller 成为上级 owner

#### Scenario: Graph 不执行副作用
- **WHEN** submitter graph 收集和合并本帧请求或候选输出
- **THEN** graph MUST 只产生纯数据 submission、claim、candidate 或 plan input
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
- **AND** 后续新增身体域 MUST NOT 依赖该单一 FullBody source 参与正式仲裁

### Requirement: 角色级管线不承担身体域退役策略
`CharacterFramePipeline` MUST 继续只负责 phase 顺序、调用 submitter/composer/applier 和传播结果。FullBody 集成路径退役、Locomotion submitter 拆分、Action submitter 拆分和 body occupancy 规则 MUST 位于独立 module 或 spec 约束中，不得写成 pipeline 本体的特殊分支。

#### Scenario: Pipeline 不硬编码退役分支
- **WHEN** 检查 `CharacterFramePipeline` 核心逻辑
- **THEN** pipeline MUST NOT 通过具体 `FullBodySubmissionBuilder` 类型判断退役路径
- **AND** MUST NOT 通过具体 `CharacterFrameSubmissionSource.FullBody` 判断最终输出
- **AND** MUST NOT 在 phase switch 中写入 Action、body/channel claim 或 Locomotion 的业务优先级

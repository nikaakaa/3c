## ADDED Requirements
### Requirement: Character runtime controller 驱动唯一角色帧
系统 MUST 将 `CharacterFrameRuntimeController` 或等价角色级 runtime controller 作为正式 Unity frame update 和 runtime tick 入口。`CharacterFramePipeline` MUST 继续是唯一角色帧管线；FullBody、Locomotion、Action 或其它身体域 MUST NOT 作为正式顶层 tick owner 直接推进 gameplay。

#### Scenario: Unity Update 从 Character 入口进入
- **GIVEN** 当前场景未启用 simulation tick driver
- **WHEN** Corin 正式 playable 角色在 frame update 中推进
- **THEN** tick MUST 从 `CharacterFrameRuntimeController` 或等价角色级入口进入
- **AND** MUST 进入同一个 `CharacterFramePipeline`
- **AND** MUST NOT 从 `PlayerFullBodyActionController.Update` 作为正式主线进入

#### Scenario: Runtime Tick 从 Character 入口进入
- **GIVEN** 当前场景启用 simulation tick driver
- **WHEN** tick driver 推进角色 gameplay phase
- **THEN** phase handler MUST 调用 `CharacterFrameRuntimeController` 或等价角色级入口
- **AND** MUST 复用同一个角色帧 context 和 runtime host
- **AND** MUST NOT 通过 `FullBodyActionTickAdapter` 作为正式 registration owner

#### Scenario: 兼容入口不恢复 FullBody 主线
- **WHEN** 旧兼容 API 调用 `PlayerFullBodyActionController.Tick`
- **THEN** 该 API MAY 转发到角色级 runtime controller
- **AND** MUST NOT 自己创建正式 `CharacterFrameRuntimeHost`
- **AND** MUST NOT 维护独立 phase 顺序

### Requirement: 角色帧 Submitter Graph
系统 MUST 使用 Character 级 submitter graph 或等价组合模块汇集本帧 sibling submitters。Locomotion submitter、FullBody Action submitter 和后续 UpperBody、Attack、Jump 或其它 submitter MUST 作为兄弟节点提交请求、事实、占用声明或候选输出。submitter graph MUST NOT 把 Locomotion 建模为 FullBody 的长期子职责。

#### Scenario: Locomotion 和 FullBody Action 并列提交
- **GIVEN** Locomotion submitter 产生基础移动候选输出
- **AND** FullBody Action submitter 产生 full-body occupancy claim
- **WHEN** Character frame pipeline 收集本帧提交
- **THEN** 两者 MUST 作为 sibling submissions 进入 submitter graph
- **AND** MUST 由角色级 `BodyArbiter` 或等价模块生成 `CharacterFramePlan`
- **AND** FullBody Action submitter MUST NOT 直接拥有 Locomotion runtime

#### Scenario: Future submitter 不塞回 integrated builder
- **WHEN** 后续新增 Attack、Jump、UpperBody、HitReact 或 Aim submitter
- **THEN** 新 submitter MUST 接入 Character 级 submitter graph
- **AND** MUST NOT 被塞入 `FullBodyIntegratedFrameAdapter`
- **AND** MUST NOT 要求 `PlayerFullBodyActionController` 成为上级 owner

#### Scenario: Graph 不执行副作用
- **WHEN** submitter graph 收集和合并本帧请求或候选输出
- **THEN** graph MUST 只产生纯数据 submission、claim、candidate 或 plan input
- **AND** MUST NOT 调用 motion executor
- **AND** MUST NOT 调用 animation presenter
- **AND** MUST NOT 消费 input buffer 或写 runtime blackboard

### Requirement: Plan 合成消费兄弟候选
系统 MUST 让 `CharacterFramePlan` 或等价角色级计划表达 sibling submitters 的最终身体占用、输出选择和压制关系。最终运动、动画、输入消费、runtime facts 和 diagnostics 的应用 MUST 发生在统一 output applier 阶段。

#### Scenario: FullBody claim 压制 Locomotion 输出
- **GIVEN** Locomotion submitter 提交基础移动 motion 和 animation candidate
- **AND** FullBody Action submitter 提交 full-body occupancy claim
- **WHEN** `BodyArbiter` 生成本帧 `CharacterFramePlan`
- **THEN** plan MAY 标记 Locomotion motion candidate 被压制
- **AND** plan MAY 标记 Locomotion animation candidate 被压制
- **AND** 该压制 MUST 来自 Character 级计划而不是 FullBody 私有字段

#### Scenario: Output applier 是唯一副作用出口
- **WHEN** `CharacterFramePlan` 选择本帧最终输出
- **THEN** motion executor 调用 MUST 只发生在 Character output applier 或等价角色级输出应用阶段
- **AND** animation presenter 调用 MUST 只发生在 Character output applier 或等价角色级输出应用阶段
- **AND** submitter、graph 和 arbiter MUST NOT 直接执行副作用

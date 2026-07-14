## MODIFIED Requirements

### Requirement: 网络 SyncDomain 必须表达输出同步语义

系统 MUST 使用 SyncDomain 对 Character gameplay facts 进行稳定业务分类，使 recording、debug 和具体 Network Model 可以识别 Motion、Action、GameplayResult、StateEffect 与 Presentation。SyncDomain MUST NOT 定义 packet kind、prediction/correction 算法、snapshot 策略、endpoint 或 transport。Graph 节点路径、SubTree membership 和 Timeline membership MUST NOT 成为同步单位。

#### Scenario: 同一事实进入当前模型

- **WHEN** CharacterPipeline 产生 ActionWindow fact
- **THEN** fact MUST 保持 ActionSyncDomain 和稳定 action/window identity
- **AND** 是否生成 ServerAuthoritative digest packet MUST 由该模型 policy 决定

### Requirement: MotionSyncDomain 必须处理连续运动同步

MotionSyncDomain MUST 表达 input frame identity、resolved motion fact、external pose input 和 correction application result 等连续运动语义。CharacterPipeline MUST 不生成 model packet、ClientCommandFrame、MotionCommand 或 CorrectionAck；具体模型 adapter MUST 选择所需事实并构造自己的命令和 acknowledgement。

#### Scenario: 本地运动完成

- **WHEN** CharacterMotionStage 完成本 tick LocalSolver 结算
- **THEN** MUST 产生 resolved motion fact
- **AND** ServerAuthoritative adapter MAY 将它转换为 MotionCommand
- **AND** 未来其它模型 MAY 读取 canonical input 而不使用 MotionCommand

### Requirement: NetworkSendStage 必须按 SyncDomain 和 policy 打包

CharacterNetworkSendStage 或等价输出 stage MUST 只收集 CharacterInputFrame、resolved motion 和 SyncFacts，并保留 BehaviorId、ActionId、SyncDomain 与稳定 identity。它 MUST 不解析 model policy 或构造 packet。Model-owned adapter MUST 使用当前 model profile 决定过滤、history 和 packet 映射。

#### Scenario: 本地预测角色输出一帧事实

- **WHEN** 本 tick 产生 input、resolved motion、action activation 和 window facts
- **THEN** output stage MUST 原样暴露对应 gameplay facts
- **AND** ServerAuthoritative adapter MUST 在 stage 外构造该模型 outgoing packets

#### Scenario: 没有 Network Model

- **WHEN** CharacterPipeline 以单机方式运行且没有 model session 消费 facts
- **THEN** Pipeline MUST 继续正常执行
- **AND** facts MAY 只供 debug 或 recording 使用

### Requirement: NetworkReceiveStage 必须按 SyncDomain 注入网络结果

CharacterNetworkReceiveStage 或等价输入 stage MUST 只接收 Character/gameplay 语义输入，例如 `ActionLifecycleTransition`、ExternalPoseCorrection、ExternalPoseSample、GameplayResult、StateEffect 和 Cue。Model packet MUST 先由 model-owned adapter 转换；stage MUST NOT 引用 packet、endpoint、history 或 transport。

#### Scenario: 服务端动作确认

- **WHEN** ServerAuthoritative adapter 收到 ActionDecision packet
- **THEN** MUST 先转换为 `ActionLifecycleTransition`
- **AND** input stage MUST 只把该通用生命周期输入交给 Character action runtime

#### Scenario: 运动校正

- **WHEN** model adapter 收到 MotionCorrection
- **THEN** MUST 转换为 ExternalPoseCorrection
- **AND** 最终应用 MUST 仍由 CharacterMotionStage 完成

### Requirement: History 必须按 policy 使用而非强制全局回滚

History 的存储内容、保留范围和恢复方式 MUST 由当前 Network Model 拥有。Character SyncFacts MAY 携带稳定 tick、sequence 和 instance identity，但 CharacterPipeline MUST 不持有 model packet history，也 MUST 不把 model correction history 描述为未来 Rollback history。

#### Scenario: 当前模型记录动作事务

- **WHEN** ServerAuthoritative policy 要求记录 Action activation/window digest
- **THEN** 该 history MUST 保存于 ServerAuthoritative model session
- **AND** ActionRuntime MUST 只保存自己的 gameplay lifecycle 状态

### Requirement: 输入派生变量不得作为独立同步事实

由输入帧、tick、配置和当前状态计算出的 Blackboard variable SHOULD NOT 作为独立 gameplay fact。具体 Network Model MUST 选择 canonical input、resolved motion 或权威结果作为自己的同步合同，不得通过同步全部派生 Blackboard 值绕过正式模型设计。

#### Scenario: MoveAxisMagnitude

- **WHEN** Graph 从 MoveAxis 计算输入幅度
- **THEN** 该值 MAY 留在本地 Blackboard
- **AND** ServerAuthoritative adapter MUST 不把它作为独立 packet 字段，除非正式 model contract 明确要求

### Requirement: Action 和 Presentation 输出必须继续归属同步域

动作窗口、生命周期、玩法结果、状态效果和表现 cue MUST 继续产生稳定 SyncDomain facts。Network Model policy MUST 只决定这些 facts 是否进入模型 packet、history 或远端复制；MUST 不回写或改变 Graph/Timeline 的 gameplay 事实来源。

#### Scenario: 本地 Camera Cue

- **WHEN** Timeline 产生 local camera cue
- **THEN** cue MUST 保持 Presentation fact
- **AND** ServerAuthoritative policy MAY 过滤其 outgoing packet
- **AND** 过滤 MUST 不阻止本地表现

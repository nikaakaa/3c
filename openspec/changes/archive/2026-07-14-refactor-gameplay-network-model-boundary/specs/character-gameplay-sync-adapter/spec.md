## REMOVED Requirements

### Requirement: CharacterPipeline 必须通过 adapter 接入 GameplaySyncRuntime

通用 GameplaySyncRuntime 不再存在；Character 必须通过 model-owned adapter 接入明确模型。

#### Scenario: 删除 generic adapter 入口

- **WHEN** 本 change 完成
- **THEN** CharacterPipeline MUST 不引用 `GameplaySyncRuntime`

### Requirement: Character outgoing adapter 必须按 SyncDomain 映射输出

旧要求把 Character fact 直接映射为通用 packet，必须迁移为 ServerAuthoritative model adapter。

#### Scenario: 删除通用 packet 映射

- **WHEN** 本 change 完成
- **THEN** 系统 MUST 不存在 Character 到通用 GameplaySyncPacket 的映射

### Requirement: Character incoming adapter 必须按 SyncDomain 注入输入

旧要求让 Character receive stage 接收 model payload，必须改为先转换成 Character 语义输入。

#### Scenario: 删除 payload 渗透

- **WHEN** 本 change 完成
- **THEN** CharacterNetworkReceiveStage MUST 不引用 model payload 类型

### Requirement: Character 网络 stage 必须保持 adapter stage 职责

旧名称与职责将按新的 fact/semantic input 边界重新定义。

#### Scenario: 迁移 stage 合同

- **WHEN** 本 change 完成
- **THEN** Character 网络 stage MUST 只交换 gameplay facts 和语义输入

### Requirement: Character adapter tick 必须服从 GameplayTickSystem

tick integration 迁移为 Session model + Character binding ownership。

#### Scenario: 迁移 tick ownership

- **WHEN** Character target 进入 logic tick
- **THEN** model-owned binding MUST 围绕该 tick 注入和收集

### Requirement: Outgoing adapter 必须消费策略解析结果

策略来源从 ActionProfile/GameplayBehaviorProfile 迁移到 ServerAuthoritative model profile。

#### Scenario: 迁移 resolver 来源

- **WHEN** adapter 解析 outgoing fact
- **THEN** MUST 不再读取旧 generic behavior network policy

### Requirement: Adapter packet preview 必须复用正式映射

该能力迁移为 model-owned preview，不再归属 generic CharacterGameplaySyncAdapter。

#### Scenario: 迁移 preview

- **WHEN** 作者查看 ServerAuthoritative packet preview
- **THEN** preview MUST 复用模型 adapter

### Requirement: Adapter 必须保持协议边界而不是作者配置入口

该职责迁移为 model-owned adapter，且 model policy authoring 由专属 profile Inspector 负责。

#### Scenario: 迁移 adapter 配置边界

- **WHEN** model adapter 构造 packet
- **THEN** MUST 不回读 Graph 或 Timeline

## ADDED Requirements

### Requirement: Character 必须通过 Model-owned Adapter 接入网络模型

系统 MUST 使用 `CharacterServerAuthoritativeAdapter` 或等价 model-owned adapter 连接 Character facts/semantic inputs 与 ServerAuthoritative model session。CharacterPipeline MUST 不持有 model session、packet、endpoint、history 或 transport。

#### Scenario: 收集本地输出

- **WHEN** CharacterPipeline 完成本 tick
- **THEN** adapter MUST 从正式 stage 读取 input、resolved motion 和 gameplay facts
- **AND** CharacterPipeline MUST 不直接 enqueue model packet

### Requirement: Outgoing Adapter 必须从事实构造模型命令

Adapter MUST 使用 ServerAuthoritative model policy，将 resolved motion、Action lifecycle、window、result、state 和 cue facts 映射为模型 packet。`MotionCommand` 和 CorrectionAck MUST 在 adapter 内构造，不得成为 CharacterPipeline packet 输出。

#### Scenario: 构造 MotionCommand

- **WHEN** resolved motion fact 通过 Stream policy
- **THEN** adapter MUST 构造模型 MotionCommand
- **AND** fact 本身 MUST 不携带 packet id 或 endpoint

### Requirement: Incoming Adapter 必须先转换为 Character 语义输入

Adapter MUST 将 MotionCorrection、MotionSnapshot、ActionDecision、GameplayResult、StateEffect 和 Cue model payload 转换为 Character/gameplay 语义输入，再推入 Character stage。Adapter MUST NOT 直接调用 ActionRuntime、MotionStage、Graph、Timeline 或 Presentation。

#### Scenario: 动作确认

- **WHEN** adapter 收到模型 ActionDecision Confirm
- **THEN** MUST 转换为 Character 已有的 `ActionLifecycleTransition`
- **AND** MUST 不把 prediction key、authority tick 或 defense-favor metadata 搬入 Character DTO
- **AND** MUST 由正式 Character action stage 消费

### Requirement: Adapter Policy 必须只来自当前模型 Profile

Adapter 和 resolver MUST 只读取绑定角色的 `ServerAuthoritativeCharacterSyncProfile`。ActionProfile、GameplayBehavior identity、Graph、Timeline、Blackboard 和 model endpoint MUST 不作为第二 policy 来源。

#### Scenario: Window policy 缺失

- **WHEN** Attack window fact 没有匹配模型 policy
- **THEN** adapter MUST 报告配置错误并拒绝构造 packet
- **AND** MUST 不使用默认 digest policy

### Requirement: Character Binding 必须精确绑定 Session Actor

Model-owned Character binding MUST 保存唯一 SubjectActorId，并按该 identity drain/submit。Binding MUST 不使用 Performer、Target 或 display name 进行路由。

#### Scenario: 两个 actor 共用 session

- **WHEN** Session 中存在两个 Character bindings
- **THEN** 每个 binding MUST 只消费自己的 actor queue
- **AND** model session MUST 只 Pump 一次当前 tick

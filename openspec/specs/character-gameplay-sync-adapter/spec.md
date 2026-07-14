# character-gameplay-sync-adapter Specification

## Purpose
定义 CharacterPipeline 接入 `GameplaySyncRuntime` 的 adapter 边界：角色管线只通过网络收发 stage 暴露本帧同步输出和 incoming 输入，adapter 负责映射为 GameplaySync packet，角色管线不得直接持有 peer、transport、Fantasy Session 或服务端对象。
## Requirements
### Requirement: Character 必须通过 Model-owned Adapter 接入网络模型

系统 MUST 使用 `CharacterServerAuthoritativeAdapter` 或等价 model-owned adapter 连接 Character facts/semantic inputs 与 ServerAuthoritative model session。CharacterPipeline MUST 不持有 model session、packet、endpoint、history 或 transport。

#### Scenario: 收集本地输出

- **WHEN** CharacterPipeline 完成本 tick
- **THEN** adapter MUST 从正式 stage 读取 input、resolved motion 和 gameplay facts
- **AND** CharacterPipeline MUST 不直接 enqueue model packet

### Requirement: Outgoing Adapter 必须从事实构造模型命令

Adapter MUST 使用 ServerAuthoritative model policy，将 resolved motion、Action lifecycle、window、result、Gameplay Effect lifecycle、Attribute value 和 Gameplay Cue facts 映射为模型 packet。`MotionCommand` 和 CorrectionAck MUST 在 adapter 内构造，不得成为 CharacterPipeline packet 输出。

#### Scenario: 构造 MotionCommand

- **WHEN** resolved motion fact 通过 Stream policy
- **THEN** adapter MUST 构造模型 MotionCommand
- **AND** fact 本身 MUST 不携带 packet id 或 endpoint

### Requirement: Incoming Adapter 必须先转换为 Character 语义输入

Adapter MUST 将 MotionCorrection、MotionSnapshot、ActionDecision、GameplayResult、GameplayEffect lifecycle、Attribute value 和 GameplayCue model payload 转换为 Character/gameplay 语义输入，再推入 Character stage。Adapter MUST NOT 直接调用 ActionRuntime、MotionStage、Graph、Timeline 或 Presentation。

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

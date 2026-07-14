## MODIFIED Requirements

### Requirement: 网络 SyncDomain 必须表达输出同步语义

系统 MUST 使用 SyncDomain 表达一类 pipeline sync fact 的生命周期、仲裁方式和同步方式。SyncDomain 的中文口径是“同步域”。SyncDomain MUST 是 runtime/pipeline contract，MUST NOT 是 Graph port、特殊 node、SubTree 类型、Timeline 类型、profile 表或 transport API。

#### Scenario: Graph 提交不同类型输出

- **WHEN** Graph 在同一 tick 内提交 locomotion motion intent、attack activation、hit result 和 camera cue
- **THEN** 系统 MUST 将需要同步、记录或校验的事实分别归入 MotionSyncDomain、ActionSyncDomain、GameplayResultSyncDomain 和 PresentationSyncDomain
- **AND** Graph 节点路径、SubTree membership 或 Timeline clip membership MUST NOT 成为网络同步单位

#### Scenario: SubTree 组织动作内容

- **WHEN** 作者用 SubTree 或 Group 整理 `Attack.Light.01` 的逻辑、Timeline 和 result 输出
- **THEN** 该 SubTree 或 Group MAY 作为 authoring 组织边界
- **AND** 网络归属 MUST 仍由输出所在 SyncDomain 的稳定 id 表达

### Requirement: NetworkSendStage 必须按 SyncDomain 和 policy 打包

系统 MUST 让 NetworkSendStage 从 `SyncFacts` 读取同步事实，并按 SyncDomain + policy 生成 outgoing packet。NetworkSendStage MUST NOT 同步 Graph 执行路径、SubTree membership、Timeline 结构或节点身份。NetworkSendStage MUST NOT 要求普通单机输出必须进入 SyncFacts。

#### Scenario: 本地预测角色发送一帧输出

- **WHEN** 本地玩家一帧内产生 input command、action activation、lifecycle transition、motion snapshot 和 gameplay result
- **THEN** NetworkSendStage MUST 分别按 MotionSyncDomain、ActionSyncDomain 和 GameplayResultSyncDomain 读取 SyncFacts
- **AND** 每个 packet MUST 使用对应 SyncDomain 的稳定 id

#### Scenario: local-only cue

- **WHEN** PresentationSyncDomain 产出 local-only cue
- **THEN** NetworkSendStage MUST 不发送该 cue
- **AND** 本地表现仍 MAY 正常播放

#### Scenario: 普通单机 tick

- **WHEN** 角色只运行普通 locomotion、普通 Timeline 或 local-only 表现
- **THEN** 系统 MAY 不产生 ActionSyncDomain fact
- **AND** NetworkSendStage MUST NOT 强制创建 ActionInstance 或 ActionContext


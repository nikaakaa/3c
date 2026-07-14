# character-network-sync-domain-contract Specification Delta

## MODIFIED Requirements

### Requirement: MotionSyncDomain 必须处理连续运动同步
系统 MUST 使用 MotionSyncDomain 表达连续运动、locomotion、motion intent、motion result、motion snapshot 和 motion correction。MotionSyncDomain 的稳定同步键 MUST 是 actor/entity identity 加 tick 或 input sequence。MotionSyncDomain MUST NOT 要求 `ActionInstanceId` 才能工作。Graph/BTSMTL MUST NOT 产出 input command；输入网络事实 MUST 由输入帧经 SyncFacts/Network 层打包。

#### Scenario: 本地移动预测
- **WHEN** 本地玩家持续输入移动
- **THEN** InputStage MUST 在 `CharacterInputFrame` 中保存移动 input value
- **AND** Locomotion 或 Motion 模块 MAY 基于该 input value 产出 motion contribution 或 motion intent
- **AND** MotionSyncDomain MUST 使用 input sequence、tick、position、velocity、grounded 和 yaw 数据支持预测或校正
- **AND** 系统 MUST NOT 为每一帧 locomotion 创建 ActionInstance

#### Scenario: 动作产生 root motion
- **WHEN** 攻击 Timeline 产生 root motion contribution
- **THEN** 该 contribution MAY 携带来源 `ActionInstanceId`
- **AND** 最终位移仲裁、Move 和 correction MUST 仍由 MotionSyncDomain/MotionStage 处理

### Requirement: NetworkSendStage 必须按 SyncDomain 和 policy 打包
系统 MUST 让 NetworkSendStage 从 `SyncFacts` 和正式输入帧读取同步事实，并按 SyncDomain + policy 生成 outgoing packet。NetworkSendStage MUST NOT 同步 Graph 执行路径、SubTree membership、Timeline 结构或节点身份。NetworkSendStage MUST NOT 要求普通单机输出必须进入 SyncFacts，也 MUST NOT 要求 Graph 产出 input command。

#### Scenario: 本地预测角色发送一帧输出
- **WHEN** 本地玩家一帧内产生 input value、action request、action activation、lifecycle transition、motion snapshot 和 gameplay result
- **THEN** NetworkSendStage MUST 分别按 MotionSyncDomain、ActionSyncDomain 和 GameplayResultSyncDomain 读取或生成 SyncFacts
- **AND** `ClientCommandFrame` 或等价输入网络事实 MUST 来自 `CharacterInputFrame`
- **AND** 每个 packet MUST 使用对应 SyncDomain 的稳定 id

#### Scenario: local-only cue
- **WHEN** PresentationSyncDomain 产出 local-only cue
- **THEN** NetworkSendStage MUST 不发送该 cue
- **AND** 本地表现仍 MAY 正常播放

#### Scenario: 普通单机 tick
- **WHEN** 角色只运行普通 locomotion、普通 Timeline 或 local-only 表现
- **THEN** 系统 MAY 不产生 ActionSyncDomain fact
- **AND** NetworkSendStage MUST NOT 强制创建 ActionInstance 或 ActionContext

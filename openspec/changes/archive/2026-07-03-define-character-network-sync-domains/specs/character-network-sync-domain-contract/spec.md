## ADDED Requirements

### Requirement: 网络 SyncDomain 必须表达输出同步语义
系统 MUST 使用 SyncDomain 表达一类 pipeline output 的生命周期、仲裁方式和同步方式。SyncDomain 的中文口径是“同步域”。SyncDomain MUST 是 runtime/pipeline contract，MUST NOT 是 Graph port、特殊 node、SubTree 类型、Timeline 类型或 profile 表。

#### Scenario: Graph 提交不同类型输出
- **WHEN** Graph 在同一 tick 内提交 locomotion motion intent、attack activation、hit result 和 camera cue
- **THEN** 系统 MUST 将这些输出分别归入 MotionSyncDomain、ActionSyncDomain、GameplayResultSyncDomain 和 PresentationSyncDomain
- **AND** Graph 节点路径、SubTree membership 或 Timeline clip membership MUST NOT 成为网络同步单位

#### Scenario: SubTree 组织动作内容
- **WHEN** 作者用 SubTree 或 Group 整理 `Attack.Light.01` 的逻辑、Timeline 和 result 输出
- **THEN** 该 SubTree 或 Group MAY 作为 authoring 组织边界
- **AND** 网络归属 MUST 仍由输出所在 SyncDomain 的稳定 id 表达

### Requirement: MotionSyncDomain 必须处理连续运动同步
系统 MUST 使用 MotionSyncDomain 表达连续运动、locomotion、motion intent、motion result、motion snapshot 和 motion correction。MotionSyncDomain 的稳定同步键 MUST 是 actor/entity identity 加 tick 或 input sequence。MotionSyncDomain MUST NOT 要求 `ActionInstanceId` 才能工作。

#### Scenario: 本地移动预测
- **WHEN** 本地玩家持续输入移动
- **THEN** Graph 或 InputStage MUST 产出 input command 或 motion intent
- **AND** MotionSyncDomain MUST 使用 input sequence、tick、position、velocity、grounded 和 yaw 数据支持预测或校正
- **AND** 系统 MUST NOT 为每一帧 locomotion 创建 ActionInstance

#### Scenario: 动作产生 root motion
- **WHEN** 攻击 Timeline 产生 root motion contribution
- **THEN** 该 contribution MAY 携带来源 `ActionInstanceId`
- **AND** 最终位移仲裁、Move 和 correction MUST 仍由 MotionSyncDomain/MotionStage 处理

### Requirement: ActionSyncDomain 必须处理离散动作事务
系统 MUST 使用 ActionSyncDomain 表达有明确 activation、confirm、reject、cancel、correct 或 end 生命周期的离散动作事务。ActionSyncDomain 的稳定同步键 MUST 是 `ActionInstanceId` 或等价 action instance identity。

#### Scenario: 启动轻攻击
- **WHEN** Graph 提交 `ActionActivationRequest(ActionId = Attack.Light.01)`
- **THEN** ActionRuntime MUST 在接受后创建 action instance identity
- **AND** ActionSyncDomain MUST 能按该 identity 聚合 activation、window、action-scoped motion、cue、result 和 end 输出

#### Scenario: 普通 locomotion 不进入 ActionSyncDomain
- **WHEN** Graph 只处理走跑跳等连续运动
- **THEN** 系统 MUST 使用 MotionSyncDomain 输出
- **AND** ActionSyncDomain MUST NOT 强制参与该帧同步

### Requirement: GameplayResultSyncDomain 必须处理权威玩法结果
系统 MUST 使用 GameplayResultSyncDomain 表达命中、伤害、格挡、破防、硬直、受击确认、objective 结果、PvE aggro/threat、revive/respawn 和 score/result event 等权威玩法结果。GameplayResultSyncDomain 的稳定同步键 MUST 是 `GameplayResultId` 或等价 result identity。GameplayResultSyncDomain MAY 关联来源 `ActionInstanceId`，但 MUST NOT 依赖 action 才能表达事件。

#### Scenario: 攻击命中
- **WHEN** 服务端或 hit/result solver 确认某个 hit window 命中目标
- **THEN** GameplayResultSyncDomain MUST 产出 gameplay result，包含 gameplay result id、source actor、target actor、tick 和结果摘要
- **AND** 如果该命中来源于 action window，result MUST 能携带对应 `ActionInstanceId` 和 window id

#### Scenario: 环境伤害
- **WHEN** 角色受到非 action 来源的环境伤害
- **THEN** GameplayResultSyncDomain MUST 能产出 gameplay result
- **AND** 该 result MUST NOT 需要 `ActionInstanceId`

#### Scenario: 目标点归属变化
- **WHEN** 服务端确认目标点从 contested 变成 TeamA captured
- **THEN** GameplayResultSyncDomain MUST 能产出 objective result
- **AND** 该 result MUST NOT 需要 `ActionInstanceId` 或 action window

### Requirement: StateEffectSyncDomain 必须处理状态和效果实例
系统 MUST 使用 StateEffectSyncDomain 表达 buff、debuff、stun、dead、downed、revive、resource/cooldown 或 objective state 等状态实例。StateEffectSyncDomain 的稳定同步键 MUST 是 `StateId`、`EffectInstanceId` 或等价业务 identity。

#### Scenario: 应用眩晕
- **WHEN** gameplay result 应用 `Stun`
- **THEN** StateEffectSyncDomain MUST 产出状态或效果实例变化
- **AND** 该实例 MUST 使用 state/effect identity 维护生命周期

#### Scenario: 动作触发状态
- **WHEN** `Guard.Counter` 成功后给予短暂无敌状态
- **THEN** StateEffectSyncDomain MUST 表达无敌状态实例
- **AND** 该状态 MAY 记录来源 action instance，但自身生命周期 MUST NOT 等同于 action instance

### Requirement: PresentationSyncDomain 必须处理表现事件
系统 MUST 使用 PresentationSyncDomain 表达 VFX、SFX、camera shake、hit stop、post-process cue 和本地 animation cue。PresentationSyncDomain 的稳定同步键 MUST 是 `CueEventId` 或等价表现事件 identity。PresentationSyncDomain 默认 MAY 是 local-only，只有 policy 要求时才复制。

#### Scenario: 本地攻击特效
- **WHEN** Timeline 触发 `slash_vfx`
- **THEN** PresentationSyncDomain MAY 本地播放该 cue
- **AND** 如果 cue 来源于 action，cue event MAY 携带 `ActionInstanceId`

#### Scenario: 远端需要看到表现
- **WHEN** 某个 cue policy 配置为 replicated
- **THEN** NetworkSendStage MUST 能从 PresentationSyncDomain 生成 cue packet
- **AND** Graph 或 Timeline MUST NOT 直接发送该 cue

### Requirement: NetworkSendStage 必须按 SyncDomain 和 policy 打包
系统 MUST 让 NetworkSendStage 从 SyncDomain output 或 `NetworkOutput` 读取同步数据，并按 SyncDomain + policy 生成 outgoing packet。NetworkSendStage MUST NOT 同步 Graph 执行路径、SubTree membership、Timeline 结构或节点身份。

#### Scenario: 本地预测角色发送一帧输出
- **WHEN** 本地玩家一帧内产生 input command、action activation、motion snapshot 和 gameplay result
- **THEN** NetworkSendStage MUST 分别按 MotionSyncDomain、ActionSyncDomain 和 GameplayResultSyncDomain 读取输出
- **AND** 每个 packet MUST 使用对应 SyncDomain 的稳定 id

#### Scenario: local-only cue
- **WHEN** PresentationSyncDomain 产出 local-only cue
- **THEN** NetworkSendStage MUST 不发送该 cue
- **AND** 本地表现仍 MAY 正常播放

### Requirement: NetworkReceiveStage 必须按 SyncDomain 注入网络结果
系统 MUST 让 NetworkReceiveStage 将 incoming snapshot、decision、correction 或 event 注入对应 SyncDomain 的输入缓存、runtime 或 graph context。NetworkReceiveStage MUST NOT 直接 tick Graph、播放 Timeline、修改 Transform 或调用 Presentation 播放器。

#### Scenario: 服务端 action decision
- **WHEN** 收到 action confirm、reject 或 correct decision
- **THEN** NetworkReceiveStage MUST 将 decision 注入 ActionSyncDomain 或 ActionRuntime 的正式入口
- **AND** 它 MUST NOT 直接执行 BTSMTL 节点或 Timeline

#### Scenario: 运动校正
- **WHEN** 收到 motion correction
- **THEN** NetworkReceiveStage MUST 将 correction 注入 MotionSyncDomain 的 correction 输入
- **AND** 最终 Transform 调整 MUST 由 MotionStage 或正式 correction stage 处理

### Requirement: History 必须按 policy 使用而非强制全局回滚
系统 MUST 根据 SyncDomain policy 决定是否记录 history、记录粒度和恢复方式。系统 MUST NOT 要求所有 SyncDomain、所有 actor 或所有输出都进入完整 rollback。

#### Scenario: ClientPredicted action
- **WHEN** ActionProfile 配置为 client predicted
- **THEN** 系统 MUST 记录 activation、prediction key、input sequence、tick 和必要 output digest
- **AND** 服务端 reject 或 correct MAY 触发该 action 相关的重放、修正或表现回退

#### Scenario: LocalOnly 表现
- **WHEN** PresentationSyncDomain cue 配置为 local-only
- **THEN** 系统 MAY 只记录 debug
- **AND** 该 cue MUST NOT 要求进入 rollback history

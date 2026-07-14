## ADDED Requirements

### Requirement: GameplaySyncRuntime 必须作为通用同步运行时
系统 MUST 提供 `GameplaySyncRuntime` 或等价通用同步运行时，用于管理 gameplay outgoing packet、incoming packet、peer、tick、history 和 debug。该运行时 MUST 独立于 CharacterPipeline，MUST NOT 直接 tick Graph、调用 ActionRuntime、调用 MotionStage、播放 Timeline 或修改 Unity Transform。

#### Scenario: Character 与 Objective 共用同步运行时
- **WHEN** 角色动作和目标点占领在同一局内 tick 产生同步输出
- **THEN** 两者 MUST 能进入同一个 GameplaySyncRuntime
- **AND** GameplaySyncRuntime MUST 使用 actor identity、SyncDomain 和 stable id 区分它们
- **AND** 系统 MUST NOT 要求 objective 借 CharacterPipeline 发送网络结果

#### Scenario: 运行时不执行玩法逻辑
- **WHEN** GameplaySyncRuntime 收到 action decision、motion correction 或 gameplay result
- **THEN** 它 MUST 只缓存、路由和记录 packet
- **AND** 它 MUST NOT 直接调用 Graph、ActionRuntime、MotionStage 或 PresentationStage

### Requirement: GameplaySyncPacket 必须使用 SyncDomain 和稳定身份
系统 MUST 使用 GameplaySync packet envelope 表达同步数据的公共身份。Envelope MUST 至少包含 packet id、SyncDomain、policy id 或等价策略引用、owner player id、team id、actor id、controlled actor id、performer actor id、target actor id、stable id、prediction key、input sequence、local logic tick 和 server tick。Graph 节点路径、SubTree membership、Timeline clip membership 和 NodeModule identity MUST NOT 进入 packet envelope。

#### Scenario: Action packet
- **WHEN** Character adapter 将 `ActionActivationRequest` 转换为 outgoing packet
- **THEN** packet MUST 使用 `ActionSyncDomain`
- **AND** stable id MUST 表达 `ActionInstanceId` 或等价 action instance identity
- **AND** packet MUST NOT 保存 Graph 执行路径作为同步身份

#### Scenario: Objective packet
- **WHEN** 目标点归属产生 server authoritative result
- **THEN** packet MUST 使用 `GameplayResultSyncDomain`
- **AND** stable id MUST 表达 `GameplayResultId` 或等价 result identity
- **AND** packet MUST NOT 依赖 `ActionInstanceId`

### Requirement: GameplaySyncPeer 必须是通用 peer 合同
系统 MUST 提供 `IGameplaySyncPeer` 或等价通用 peer 合同。Peer MUST 能接收 outgoing packets、按 local logic tick 推进，并产出 incoming packets。Peer MUST NOT 暴露 CharacterPipeline、ActionRuntime、GraphContext、MotionStage、PresentationStage 或 Fantasy Session 给 gameplay 层。

#### Scenario: Local loopback peer
- **WHEN** 本地 loopback peer 被注册到 GameplaySyncRuntime
- **THEN** 它 MUST 通过同一套 peer 合同消费 outgoing packets 并产出 incoming packets
- **AND** 它 MUST NOT 直接访问 CharacterPipeline 或 ActionRuntime

#### Scenario: Future Fantasy peer
- **WHEN** 未来 Fantasy peer 被注册到 GameplaySyncRuntime
- **THEN** 它 MUST 通过同一套 peer 合同映射 C2S/S2C 消息
- **AND** GameplaySyncRuntime 和 Character adapter MUST 不需要因为 peer 类型改变而更换代码路径

### Requirement: History 必须按 actor、SyncDomain 和 policy 记录
系统 MUST 让 GameplaySyncRuntime 或等价 history 组件按 actor、SyncDomain 和 policy 记录必要历史。系统 MUST NOT 要求所有 actor、所有 SyncDomain 或所有 packet 进入完整 rollback。

#### Scenario: 本地预测角色运动
- **WHEN** LocalPredicted actor 发送 MotionSyncDomain client command frame
- **THEN** history MUST 能按 actor id、input sequence 和 local logic tick 记录命令
- **AND** motion correction MUST 能使用该历史对齐预测修正

#### Scenario: LocalOnly cue
- **WHEN** PresentationSyncDomain cue 的 policy 是 local-only
- **THEN** 系统 MAY 只记录 debug
- **AND** 该 cue MUST NOT 强制进入 rollback history

### Requirement: Runtime Debug 必须按 SyncDomain 展示同步链路
系统 MUST 提供或预留 Runtime Debug 数据，用于展示最近 outgoing packets、peer pending packets、incoming packets、decision、correction 和 gameplay result。Debug MUST 能按 actor id、SyncDomain、stable id、prediction key、input sequence、local logic tick 和 server tick 查询。

#### Scenario: 查看动作预测闭环
- **WHEN** 本地玩家预测启动 `Attack.Light.01`
- **THEN** Debug MUST 能显示 ActionSyncDomain outgoing activation、pending decision 和 incoming confirm/reject
- **AND** Debug MUST 显示 action instance id、prediction key、input sequence、local logic tick 和 server tick

#### Scenario: 查看目标点结果
- **WHEN** 服务端确认 objective captured
- **THEN** Debug MUST 能显示 GameplayResultSyncDomain incoming result
- **AND** Debug MUST 能显示该 result 的 actor/objective identity、team id 和 stable id

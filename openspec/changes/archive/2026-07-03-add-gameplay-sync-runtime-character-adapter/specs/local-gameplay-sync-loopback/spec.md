## ADDED Requirements

### Requirement: LocalGameplaySyncLoopbackPeer 必须复用通用 peer 合同
系统 MUST 提供 `LocalGameplaySyncLoopbackPeer` 或等价本地调试 peer。该 peer MUST 实现 `IGameplaySyncPeer` 或等价通用 peer 合同，MUST 消费 GameplaySync outgoing packets 并按配置延迟产出 GameplaySync incoming packets。Loopback MUST NOT 直接修改 CharacterPipeline、ActionRuntime、GraphContext、MotionStage、PresentationStage 或 Unity Transform。

#### Scenario: 延迟确认动作
- **WHEN** loopback 收到 ActionSyncDomain action activation outgoing packet 且配置为 confirm
- **THEN** 它 MUST 创建延迟投递的 ActionInstanceDecision incoming packet
- **AND** 该 packet MUST 通过 GameplaySyncRuntime 和 CharacterGameplaySyncAdapter 回到 CharacterNetworkReceiveStage

#### Scenario: 不绕过正式入口
- **WHEN** loopback 生成 correction 或 action decision
- **THEN** 它 MUST 只产出 incoming packet
- **AND** 它 MUST NOT 直接调用 ActionRuntime、MotionStage 或 Transform

### Requirement: Loopback 配置必须只作为本地网络调试配置
Loopback MUST 提供本地调试配置，用于控制延迟、动作确认/拒绝、校正偏移、丢包率、快照输出和 defense favor 标记。该配置 MUST NOT 写入 ActionProfile、Graph、Timeline clip 或正式服务端策略数据。

#### Scenario: 模拟动作拒绝
- **WHEN** 开发者配置下一次 action decision 为 reject
- **THEN** loopback MUST 对下一次 ActionSyncDomain activation 生成 rejected decision
- **AND** 该配置 MUST NOT 修改 ActionProfile 或 Graph authoring 数据

#### Scenario: 模拟 defense favor
- **WHEN** 开发者启用 defense favor 标记并触发防守类 action
- **THEN** loopback MAY 生成带 `DefenseFavorApplied` 的 confirmed decision
- **AND** 该标记 MUST 只用于调试防守方占优链路

### Requirement: Loopback 必须覆盖最小混合同步闭环
Loopback 第一阶段 MUST 至少能覆盖 ActionSyncDomain decision、MotionSyncDomain correction 或 snapshot、GameplayResultSyncDomain result 回显或记录，以及 Runtime Debug 记录。Loopback MAY 不实现完整 hit validation、objective solver 或 PvE AI。

#### Scenario: 预测动作闭环
- **WHEN** 本地玩家预测启动攻击
- **THEN** loopback MUST 能按配置返回 action confirm 或 reject
- **AND** Debug MUST 能显示 outgoing activation、pending decision 和 incoming decision

#### Scenario: 运动校正闭环
- **WHEN** 本地玩家产生 client command frame 且 loopback 配置 correction offset
- **THEN** loopback MUST 能生成 MotionSyncDomain correction
- **AND** correction MUST 使用 input sequence 或 tick 对齐本地预测历史

#### Scenario: Gameplay result 记录
- **WHEN** adapter 发送 GameplayResultSyncDomain result 或 digest
- **THEN** loopback MAY 记录或回显该 result 用于调试
- **AND** 第一阶段 MUST NOT 假装已经完成服务端 hit/objective/PvE 裁决

### Requirement: Future Fantasy peer 必须替换 loopback 而不替换 gameplay 语义
未来 `FantasyGameplaySyncPeer` MUST 复用 GameplaySync packet contract。Fantasy peer MUST 只负责 C2S/S2C 协议映射、Session 发送、Handler 接收和 incoming packet 入队，不得引入第二套 action decision、motion correction、gameplay result 或 Graph 同步语义。

#### Scenario: 替换 peer
- **WHEN** 本地 loopback peer 替换为 FantasyGameplaySyncPeer
- **THEN** CharacterGameplaySyncAdapter、CharacterPipeline、Graph、ActionRuntime、Timeline 和 MotionStage MUST 保持同一条代码路径
- **AND** 只有 peer 实现和协议映射发生变化

#### Scenario: 接收服务端 action decision
- **WHEN** Fantasy handler 收到未来 S2C action instance decision
- **THEN** FantasyGameplaySyncPeer MUST 映射为同一 ActionSyncDomain incoming packet
- **AND** 后续 Character adapter 处理 MUST 与 loopback packet 相同

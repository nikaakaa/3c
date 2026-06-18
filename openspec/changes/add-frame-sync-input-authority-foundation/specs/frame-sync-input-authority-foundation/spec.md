## ADDED Requirements

### Requirement: 帧同步输入事实合同
系统 MUST 定义帧同步输入事实合同，使网络输入以 `SimulationTick`、player id、unit id、移动意图、look/aim 意图、run held、button facts、action request facts 和 target intent 表达。该合同 MUST 使用纯数据，不得保存 Unity Object、InputAction、Animancer、Animator、Cinemachine、Transform 或场景实例引用。

#### Scenario: 输入事实 round-trip
- **GIVEN** tick N 的本地 `PredictionInputFrame`
- **WHEN** 它被转换为帧同步输入事实再转换回 replay 输入
- **THEN** tick、move、look、run、Dodge、Attack、Jump 和 Interact facts MUST 保持一致
- **AND** 转换结果 MUST 不包含动作接受结果

#### Scenario: 不同步真实相机
- **WHEN** 构造帧同步输入事实
- **THEN** 系统 MUST NOT 保存真实 camera yaw/pitch、Cinemachine axis、FreeLook state 或 Main Camera transform
- **AND** 如需 replay camera-relative 输入，MUST 使用纯数据 intent 或 `RollbackCameraBasisState` 等价事实

### Requirement: Action Request 只同步输入事实
系统 MUST 将 Dodge、Attack、Jump、Interact 或未来 Action 的网络同步内容限制为 request 输入事实。Action accepted/rejected、active action、state time、cancel window、hit window、body claim 结果和动画播放事实 MUST 由 replay 重新经过 Action domain 推导。

#### Scenario: Dodge request replay
- **GIVEN** 帧同步输入事实中 Dodge pressed 为 true
- **WHEN** replay 推进该 tick
- **THEN** 系统 MUST 将 Dodge pressed 回灌到 `InputRequestBuffer`
- **AND** Action 是否接受 MUST 由 `CharacterFramePipeline` 和 Action domain 重新判定

#### Scenario: held 不重复请求
- **GIVEN** tick N 只有 held 为 true 且 pressed 为 false
- **WHEN** replay 推进 tick N
- **THEN** 系统 MUST NOT 生成新的 pressed request

### Requirement: Confirmed Input Set
系统 MUST 定义 confirmed input set，使服务端或 fake room 能对一个 tick 的多玩家输入集合进行稳定排序、确认和诊断。Confirmed input set MUST NOT 包含角色状态、Transform correction、动画状态或服务端 gameplay 结果。

#### Scenario: 多玩家输入排序稳定
- **GIVEN** 同一 tick 的多个玩家输入以任意顺序到达
- **WHEN** 系统构建 confirmed input set
- **THEN** 输入 MUST 按 player id、unit id 和 local input sequence 稳定排序

#### Scenario: duplicate input 诊断
- **GIVEN** 同一 tick、player、unit 到达两份输入
- **WHEN** 系统构建 confirmed input set
- **THEN** 第一份合法输入 MAY 进入 confirmed set
- **AND** 后续输入 MUST 进入 duplicate diagnostic
- **AND** 系统 MUST NOT 静默覆盖已接受输入

#### Scenario: missing input 诊断
- **GIVEN** 某 tick 期望 player/unit 输入但未收到
- **WHEN** 系统构建 confirmed input set
- **THEN** 系统 MUST 记录 missing diagnostic
- **AND** MUST NOT 将缺失输入伪装为合法空输入

#### Scenario: late input 诊断
- **GIVEN** 输入 tick 早于当前 confirmed tick
- **WHEN** 该输入到达
- **THEN** 系统 MUST 记录 late diagnostic
- **AND** MUST NOT 覆盖已裁剪历史

### Requirement: 帧同步版本握手
系统 MUST 在进入帧同步 gameplay input 前完成协议和配置版本握手。握手 MUST 至少覆盖 protocol version、input schema version、checksum schema version、action catalog hash、locomotion config hash、state machine config hash、motion profile hash 和 input mapping version。

#### Scenario: 版本一致进入同步
- **GIVEN** 客户端和服务端握手字段一致
- **WHEN** 客户端请求进入帧同步
- **THEN** 系统 MAY 允许发送 gameplay input

#### Scenario: 版本不一致拒绝同步
- **GIVEN** 任一握手字段不一致
- **WHEN** 客户端请求进入帧同步
- **THEN** 系统 MUST 拒绝进入同步
- **AND** MUST 输出不一致类别
- **AND** MUST NOT 发送 gameplay input

#### Scenario: 缺失 hash 失败
- **GIVEN** 必需配置 hash 缺失
- **WHEN** 执行握手
- **THEN** 系统 MUST 判定握手失败
- **AND** MUST NOT 使用 fallback 配置继续运行

## MODIFIED Requirements

### Requirement: Graph authoring 必须表达 request 提交而不是结构归属
Graph authoring UI MUST 提供普通 request submit authoring 入口，用于配置 action activation request 的 action profile、source input request、target key、是否消费输入 request 和 instance id blackboard 输出。Graph 内部临时读写 MUST 命名为 blackboard，不得命名为 fact。该 UI MUST NOT 命名或实现为 ActionModule、AbilityNode、ActionSubTree 或静态 node identity。

#### Scenario: 创建格挡反击提交入口
- **WHEN** 作者在 GuardState 的行为 Graph 中创建 action activation request 提交入口
- **THEN** UI MUST 允许选择 `Guard.ParryCounter` ActionProfile
- **AND** UI MUST 允许选择 `Guard` 作为 source input request
- **AND** UI MUST 允许填写 `LastAttacker` 作为 target key

#### Scenario: 编辑网络策略
- **WHEN** 作者选中 Graph 中的 action activation request 提交入口
- **THEN** UI MUST NOT 暴露 HitWindow authority、RootMotion correction 或 Cue playback policy
- **AND** 作者 MUST 到 ActionProfile Inspector 中修改这些策略

### Requirement: Timeline window Inspector 必须只编辑窗口输出
Timeline window Inspector MUST 只编辑 WindowType、WindowId、时间范围和窗口业务参数。Timeline window MUST NOT 保存完整 authority、history、replication、correction 或 cue playback 策略。

#### Scenario: 编辑 HitWindow
- **WHEN** 作者选中 HitWindow clip
- **THEN** Inspector MUST 允许设置 `WindowType = Hit` 和稳定 `WindowId`
- **AND** 是否进入 hit/result history、是否 server authoritative、是否 digest only MUST 从 ActionProfile 解析

#### Scenario: 没有静态 ActionProfile
- **WHEN** Timeline asset 被多个 action 复用
- **THEN** window clip MUST 保持只描述窗口输出
- **AND** 策略预览 MAY 依赖 editor 当前选择的 preview profile 或 runtime debug context，但不得写入 clip 作为正式配置

### Requirement: Runtime Debug 必须展示 request 到 outputs 的完整链路
系统 MUST 提供或预留 Runtime Debug 数据，按 input request、action activation request、ActionInstance、window sample、motion sample、gameplay result、cue event 和 network result 展示链路。

#### Scenario: 本地预测格挡反击
- **WHEN** 本地预测启动 `Guard.ParryCounter`
- **THEN** Debug MUST 显示 source input request、ActionInstanceId、ActionId、PredictionKey、InputSequence、StartTick、Phase 和 State
- **AND** Debug MUST 能关联显示该实例产生的 HitWindow、InvulnerableWindow、RootMotion、ParryFlash 和 GameplayResult

#### Scenario: 服务端拒绝
- **WHEN** 服务端拒绝该 ActionInstance
- **THEN** Debug MUST 显示 rejected instance id、prediction key 和 reason
- **AND** Debug MUST 显示后续 correction 或表现取消状态

### Requirement: UI 闭环必须支持 Timeline 和非 Timeline 动作
作者 MUST 能使用同一套 ActionProfile、Graph request submit UI 和 Runtime Debug 配置 Timeline 动作与非 Timeline 动作。系统 MUST NOT 要求动作必须播放 Timeline。

#### Scenario: Timeline 攻击
- **WHEN** 作者配置轻攻击
- **THEN** Graph request submit UI MUST 提交 `Attack.Light.01`
- **AND** Timeline window Inspector MUST 配置 Hit 和 Cancel 窗口

#### Scenario: 非 Timeline 格挡
- **WHEN** 作者配置持续格挡
- **THEN** Graph request submit UI MUST 能提交 `Guard.Hold`
- **AND** Graph 或后续 stage MUST 能产出 Guard window sample，而不需要 TimelineNode

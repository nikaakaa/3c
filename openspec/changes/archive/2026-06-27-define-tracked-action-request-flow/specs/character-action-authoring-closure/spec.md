## ADDED Requirements

### Requirement: CharacterPipelineDefinition 必须配置 ActionProfile 库
系统 MUST 让 `CharacterPipelineDefinition` 或等价角色管线配置持有正式 ActionProfile 列表。Pipeline 初始化时 MUST 将这些 profile 注册到 `ActionRuntime`。缺失、空 action id 或重复 action id MUST 作为配置错误报告，不得通过字符串全局搜索或 fallback profile 继续运行。

#### Scenario: 角色配置动作库
- **WHEN** 作者打开 `CharacterPipelineDefinition`
- **THEN** Inspector MUST 允许配置该角色可用的 ActionProfile 列表
- **AND** 初始化 pipeline 时 MUST 注册这些 profile

#### Scenario: 重复 action id
- **WHEN** 两个 ActionProfile 使用同一个 action id
- **THEN** 配置校验 MUST 报错
- **AND** 系统 MUST NOT 随机选择其中一个作为 fallback

### Requirement: Graph authoring 必须表达 request 提交而不是结构归属
Graph authoring UI MUST 提供普通 request submit authoring 入口，用于配置 tracked action request 的 action profile、source input request、target key、是否消费输入 request 和 instance id 输出。该 UI MUST NOT 命名或实现为 ActionModule、AbilityNode、ActionSubTree 或静态 node identity。

#### Scenario: 创建格挡反击提交入口
- **WHEN** 作者在 GuardState 的行为 Graph 中创建 tracked action request 提交入口
- **THEN** UI MUST 允许选择 `Combat.ParryCounter` ActionProfile
- **AND** UI MUST 允许选择 `Guard` 作为 source input request
- **AND** UI MUST 允许填写 `LastAttacker` 作为 target key

#### Scenario: 编辑网络策略
- **WHEN** 作者选中 Graph 中的 tracked action request 提交入口
- **THEN** UI MUST NOT 暴露 HitWindow authority、RootMotion correction 或 Cue playback policy
- **AND** 作者 MUST 到 ActionProfile Inspector 中修改这些策略

### Requirement: ActionProfile Inspector 必须是策略主编辑入口
`ActionProfile` Inspector MUST 按 Identity、Network、Tags、Windows、Motion、Cues、Debug 分区展示。ActionProfile MUST NOT 引用 Graph、Timeline 或 Motion runtime 对象。

#### Scenario: 配置攻击动作
- **WHEN** 作者编辑 `Attack.Light.01`
- **THEN** Identity 分区 MUST 配置 action id、display name 和 debug category
- **AND** Network 分区 MUST 配置 prediction、authority、replication 和 correction policy
- **AND** Windows、Motion、Cues 分区 MUST 配置事实类型对应策略

#### Scenario: 动作策略复用
- **WHEN** 多个 Graph 分支都提交同一个 ActionProfile
- **THEN** 它们 MUST 使用同一份 profile 策略
- **AND** Graph 分支 MUST NOT 复制完整网络策略字段

### Requirement: Timeline window Inspector 必须只编辑窗口事实
Timeline window Inspector MUST 只编辑 WindowType、WindowId、时间范围和窗口业务参数。Timeline window MUST NOT 保存完整 authority、history、replication、correction 或 cue playback 策略。

#### Scenario: 编辑 HitWindow
- **WHEN** 作者选中 HitWindow clip
- **THEN** Inspector MUST 允许设置 `WindowType = Hit` 和稳定 `WindowId`
- **AND** 是否进入 combat history、是否 server authoritative、是否 digest only MUST 从 ActionProfile 解析

#### Scenario: 没有静态 ActionProfile
- **WHEN** Timeline asset 被多个 action 复用
- **THEN** window clip MUST 保持只描述窗口事实
- **AND** 策略预览 MAY 依赖 editor 当前选择的 preview profile 或 runtime debug context，但不得写入 clip 作为正式配置

### Requirement: Runtime Debug 必须展示 request 到 facts 的完整链路
系统 MUST 提供或预留 Runtime Debug 数据，按 input request、tracked action request、ActionInstance、window fact、motion fact、combat event、cue fact 和 network result 展示链路。

#### Scenario: 本地预测格挡反击
- **WHEN** 本地预测启动 `Combat.ParryCounter`
- **THEN** Debug MUST 显示 source input request、ActionInstanceId、ActionId、PredictionKey、InputSequence、StartTick、Phase 和 State
- **AND** Debug MUST 能关联显示该实例产生的 HitWindow、InvulnerableWindow、RootMotion、ParryFlash 和 CombatEvent

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
- **THEN** Graph request submit UI MUST 能提交 `Combat.Guard`
- **AND** Graph 或后续 stage MUST 能产出 Guard window fact，而不需要 TimelineNode


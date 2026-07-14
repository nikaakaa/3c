# character-action-network-policy-authoring Specification

## ADDED Requirements

### Requirement: ActionProfile 必须集中配置动作网络策略
系统 MUST 使用 `ActionProfile` 或等价 profile 集中配置动作身份和网络策略。Graph 节点、Timeline clip、Motion fact 和 Cue fact MUST 只声明事实类型和运行时归属，不得分散保存完整网络策略。

#### Scenario: 配置攻击动作
- **WHEN** 作者配置 `attack.light.01`
- **THEN** 作者 MUST 在 `ActionProfile` 中配置 action id、tags、block/cancel tags、prediction policy、authority policy、replication policy 和 correction policy
- **AND** Graph 节点 MUST 只引用该 profile 或 action id

#### Scenario: 修改网络策略
- **WHEN** 作者要把某个动作的 hit window 从本地预测改为服务端权威
- **THEN** 修改 MUST 集中发生在 `ActionProfile` 的 window policy
- **AND** 不需要逐个编辑 Graph 节点或 Timeline clip 的完整网络字段

### Requirement: Timeline window 必须只标事实类型和窗口参数
系统 MUST 让 Timeline window track/clip 只表达窗口事实类型、窗口 id、时间和业务参数。窗口的 authority、history、replication、digest 策略 MUST 通过 `ActionProfile + WindowType` 解析。

#### Scenario: HitWindow
- **WHEN** 作者在 Timeline 中编辑攻击窗口
- **THEN** clip MUST 至少能表达 `WindowType = Hit` 和稳定 `WindowId`
- **AND** 是否进入 combat rewind、是否服务端权威、是否只同步 digest MUST 来自 ActionProfile

#### Scenario: CancelWindow
- **WHEN** 作者在 Timeline 中编辑取消窗口
- **THEN** clip MUST 表达 `WindowType = Cancel` 和窗口参数
- **AND** 本地预测或服务器修正策略 MUST 来自 ActionProfile

### Requirement: Motion 和 Cue 策略必须按事实类型集中解析
系统 MUST 按 action profile、motion source type 和 cue type 解析运动和表现策略。MotionStage 和 PresentationStage MAY 在输出事实上携带 action instance id、input sequence、tick、cue id 或 source type，但 MUST NOT 在每个事实上重复完整 policy 配置。

#### Scenario: RootMotion
- **WHEN** Timeline 或 Graph 产生 root motion contribution
- **THEN** motion fact MUST 能表达 source type 和可选 action instance id
- **AND** client predicted、server correctable 或 smooth correction 策略 MUST 由 profile resolver 给出

#### Scenario: Camera cue
- **WHEN** Timeline 产生 camera shake cue
- **THEN** cue fact MUST 能表达 cue type 或 cue id
- **AND** local only、本地预测或服务端确认策略 MUST 由 profile resolver 给出

### Requirement: Inspector 必须按作者职责分层
系统 MUST 将网络相关 UI 分为 ActionProfile Inspector、Graph Node Inspector、Timeline Window Inspector 和 Runtime Debug Inspector。UI MUST 避免让作者在每个节点或 clip 上重复配置完整网络策略。

#### Scenario: ActionProfile Inspector
- **WHEN** 作者选中 ActionProfile
- **THEN** Inspector MUST 按 Identity、Network、Windows、Motion、Cues、Tags、Debug 分区显示
- **AND** 该 Inspector MUST 是动作网络策略的主编辑入口

#### Scenario: BeginTrackedAction Node Inspector
- **WHEN** 作者选中 `BeginTrackedAction` 节点
- **THEN** Inspector MUST 只配置 ActionProfile/ActionId、TargetKey 和必要输入输出
- **AND** MUST NOT 暴露完整 window/motion/cue 网络策略

#### Scenario: Timeline Window Inspector
- **WHEN** 作者选中 Hit、Cancel、IFrame、Armor 等窗口 clip
- **THEN** Inspector MUST 只配置 WindowType、WindowId、时间和窗口业务参数
- **AND** MUST 提供只读提示说明该窗口最终匹配到哪个 ActionProfile policy

### Requirement: Runtime Debug 必须按 ActionInstance 展示链路
系统 MUST 提供或预留 Runtime Debug 视图按 `ActionInstance` 展示预测、窗口、motion、网络确认和校正链路。调试视图 MUST 帮助面试官看到本地表现和服务端结果如何对应。

#### Scenario: 查看本地预测攻击
- **WHEN** 本地玩家预测启动攻击
- **THEN** Debug MUST 能显示 action instance id、action id、prediction key、input sequence、state 和 phase
- **AND** MUST 能关联显示该实例产生的 window、motion、cue 和网络发送状态

#### Scenario: 查看服务端拒绝
- **WHEN** 服务端拒绝某次预测动作
- **THEN** Debug MUST 能显示被拒绝的 instance id 和拒绝原因
- **AND** MUST 能显示后续 correction 或表现修正状态

### Requirement: 不恢复旧 ActionSO 或节点身份模块
系统 MUST NOT 恢复旧 `ActionSO`、`ActionModule`、`ActionSubTreeNode`、节点 action identity 或 BBB 状态类主线。`ActionProfile` 是动作身份和网络策略中心，不是旧动作执行配置表。

#### Scenario: 新增动作策略
- **WHEN** 新增动作网络策略
- **THEN** 系统 MUST 使用 ActionProfile 和正式 runtime facts
- **AND** MUST NOT 新增旧 SO/config 分裂路径或节点身份模块

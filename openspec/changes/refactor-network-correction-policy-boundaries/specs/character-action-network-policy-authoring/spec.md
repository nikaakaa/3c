## MODIFIED Requirements

### Requirement: ActionProfile 必须集中配置动作网络策略

系统 MUST 使用 `ActionProfile` 或等价 profile 集中配置动作身份、prediction、authority、replication 和动作输出网络策略。Graph 节点、Timeline clip、Motion sample 和 Cue event MUST 只声明输出类型和运行时归属，不得分散保存完整网络策略。ActionProfile MUST NOT 配置 actor motion correction application 或 Action reject 处理方式。

#### Scenario: 配置攻击动作

- **WHEN** 作者配置 `attack.light.01`
- **THEN** 作者 MUST 在 `ActionProfile` 中配置 action id、tags、block/cancel tags、prediction policy、authority policy、replication policy 和各输出 policy
- **AND** Graph action activation 节点 MUST 只引用该 profile，不得提供手填 action id fallback
- **AND** 作者 MUST NOT 在该 profile 选择 SmoothCorrection、ForceCorrection 或 CancelOnReject

#### Scenario: 修改网络策略

- **WHEN** 作者要把某个动作的 hit window 从本地预测改为服务端权威
- **THEN** 修改 MUST 集中发生在 `ActionProfile` 的 window policy
- **AND** 不需要逐个编辑 Graph 节点或 Timeline clip 的完整网络字段

### Requirement: Motion 和 Cue 策略必须按输出类型集中解析

系统 MUST 按 action profile、motion source type 和 cue type 解析运动和表现的网络可见性策略。MotionStage 和 PresentationStage MAY 在动作输出上携带 action instance id、input sequence、tick、cue id 或 source type，但 MUST NOT 在每个输出上重复完整 policy 配置。Action motion policy MUST 只描述动作 motion source 的 prediction 归属；actor motion correction application MUST 由 MotionSyncDomain 与 CharacterMotionStage 处理。

#### Scenario: RootMotion

- **WHEN** Timeline 或 Graph 产生 root motion contribution
- **THEN** motion sample MUST 能表达 source type 和可选 action instance id
- **AND** 本地是否提交预测 motion digest MUST 由 source prediction 加 ActionProfile authority/replication 解析
- **AND** motion sample MUST NOT 携带 Smooth、Force 或 actor correction id

#### Scenario: Camera cue

- **WHEN** Timeline 产生 camera shake cue
- **THEN** cue event MUST 能表达 cue type 或 cue id
- **AND** local only、本地预测或服务端确认策略 MUST 由 profile resolver 给出

### Requirement: ActionProfile Inspector 必须形成网络策略闭环

系统 MUST 让 `ActionProfile` Inspector 成为动作事务网络策略的完整作者入口。Inspector MUST 展示基础策略、每类输出策略、配置错误、effective policy 摘要、expected SyncFacts 和 expected SyncDomain packet 预览。作者 MUST 能从一个 ActionProfile 看出该动作如何预测、确认、复制和调试；Inspector MUST 明确说明 actor motion correction 与 Reject terminal invariant 不在 ActionProfile 配置。

#### Scenario: 查看轻攻击策略闭环

- **WHEN** 作者选中 `Attack.Light.01` ActionProfile
- **THEN** Inspector MUST 显示 action-level prediction、authority 和 replication policy
- **AND** MUST 显示 HitWindow、CancelWindow、RootMotion、CameraCue 和 GameplayResult 的解析结果
- **AND** MUST 显示这些输出预计进入的 SyncDomain
- **AND** MUST NOT 显示 action-level correction policy

#### Scenario: 策略缺失

- **WHEN** ActionProfile 存在 Timeline 或 Graph 会产出的 WindowType 但没有对应策略
- **THEN** Inspector MUST 报告明确配置错误或 warning
- **AND** Runtime MUST NOT 静默使用 fallback 策略

### Requirement: 策略模板必须显式写入正式配置

系统 MAY 提供策略模板、preset 或等价创建入口来减少作者重复配置。模板 MUST 只在创建或应用时把字段显式写入 `ActionProfile` 正式配置；运行时 MUST NOT 依赖模板名称、隐藏默认值或 fallback 配置。模板 MUST NOT 写入 actor motion correction 或 Reject 处理字段。

#### Scenario: 应用本地预测近战模板

- **WHEN** 作者对新 ActionProfile 应用“本地预测近战攻击”模板
- **THEN** 系统 MUST 写入 prediction、authority、replication、window、motion、cue 和 gameplay result 的正式字段
- **AND** 作者后续 MUST 能逐项查看和修改这些字段

#### Scenario: 删除模板资产

- **WHEN** 模板入口被删除或不可用
- **THEN** 已创建的 ActionProfile MUST 仍保留完整正式策略
- **AND** Runtime MUST NOT 读取模板来决定同步行为

### Requirement: Effective Network Policy 必须只读解析动作输出

系统 MUST 提供 `ActionNetworkPolicyResolver` 或等价 Transaction 子解析器，将 `ActionProfile + 输出事实类型` 解析为只读 effective network policy。Resolver MUST 覆盖 activation、lifecycle transition、window、motion、cue 和 gameplay result。它 MUST 作为 `BehaviorNetworkPolicyResolver` 处理 Transaction fact 时的专项策略来源，MUST NOT 成为所有 SyncFacts 的 adapter 总入口，也 MUST NOT 修改 Graph、Timeline clip 或输出事实。Resolver MUST NOT 使用 actor correction application result 推导 Action packet。

#### Scenario: 解析 HitWindow

- **WHEN** Runtime 或 Inspector 使用 `Attack.Light.01 + WindowType.Hit` 请求解析
- **THEN** Resolver MUST 返回该窗口的 authority、history、replication 和 digest 策略
- **AND** 返回结果 MUST 能说明是否进入 combat rewind history 或 ActionSyncDomain packet

#### Scenario: 解析 RootMotion

- **WHEN** Timeline 或 Graph 输出 RootMotion sample 并携带 Action Context
- **THEN** Resolver MUST 根据 `MotionSourceType.RootMotion` 的 prediction policy 和 ActionProfile authority/replication 决定是否产生本地 outgoing digest
- **AND** `ServerConfirmed` source MUST NOT 因 correction 配置而被当成本地预测输出发送
- **AND** Motion sample 自身 MUST NOT 保存完整网络策略

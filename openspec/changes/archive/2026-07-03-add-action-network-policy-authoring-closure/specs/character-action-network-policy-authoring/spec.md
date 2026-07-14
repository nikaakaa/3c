## ADDED Requirements

### Requirement: ActionProfile Inspector 必须形成网络策略闭环

系统 MUST 让 `ActionProfile` Inspector 成为动作网络策略的完整作者入口。Inspector MUST 展示基础策略、每类输出策略、配置错误、effective policy 摘要、expected SyncFacts 和 expected SyncDomain packet 预览。作者 MUST 能从一个 ActionProfile 看出该动作如何预测、确认、复制、纠正和调试。

#### Scenario: 查看轻攻击策略闭环

- **WHEN** 作者选中 `Attack.Light.01` ActionProfile
- **THEN** Inspector MUST 显示 action-level prediction、authority、replication 和 correction policy
- **AND** MUST 显示 HitWindow、CancelWindow、RootMotion、CameraCue 和 GameplayResult 的解析结果
- **AND** MUST 显示这些输出预计进入的 SyncDomain

#### Scenario: 策略缺失

- **WHEN** ActionProfile 存在 Timeline 或 Graph 会产出的 WindowType 但没有对应策略
- **THEN** Inspector MUST 报告明确配置错误或 warning
- **AND** Runtime MUST NOT 静默使用 fallback 策略

### Requirement: 策略模板必须显式写入正式配置

系统 MAY 提供策略模板、preset 或等价创建入口来减少作者重复配置。模板 MUST 只在创建或应用时把字段显式写入 `ActionProfile` 正式配置；运行时 MUST NOT 依赖模板名称、隐藏默认值或 fallback 配置。

#### Scenario: 应用本地预测近战模板

- **WHEN** 作者对新 ActionProfile 应用“本地预测近战攻击”模板
- **THEN** 系统 MUST 写入 prediction、authority、replication、correction、window、motion、cue 和 gameplay result 的正式字段
- **AND** 作者后续 MUST 能逐项查看和修改这些字段

#### Scenario: 删除模板资产

- **WHEN** 模板入口被删除或不可用
- **THEN** 已创建的 ActionProfile MUST 仍保留完整正式策略
- **AND** Runtime MUST NOT 读取模板来决定同步行为

### Requirement: Effective Network Policy 必须只读解析动作输出

系统 MUST 提供 `ActionNetworkPolicyResolver` 或等价服务，将 `ActionProfile + 输出事实类型` 解析为只读 effective network policy。Resolver MUST 覆盖 activation、lifecycle transition、window、motion、cue 和 gameplay result。Resolver MUST NOT 修改 Graph、Timeline clip 或输出事实。

#### Scenario: 解析 HitWindow

- **WHEN** Runtime 或 Inspector 使用 `Attack.Light.01 + WindowType.Hit` 请求解析
- **THEN** Resolver MUST 返回该窗口的 authority、history、replication 和 digest 策略
- **AND** 返回结果 MUST 能说明是否进入 combat rewind history 或 ActionSyncDomain packet

#### Scenario: 解析 RootMotion

- **WHEN** Timeline 或 Graph 输出 RootMotion sample 并携带 Action Context
- **THEN** Resolver MUST 根据 `MotionSourceType.RootMotion` 返回预测、校正和复制策略
- **AND** Motion sample 自身 MUST NOT 保存完整网络策略

### Requirement: GameplayResult 策略必须由 ActionProfile 声明

系统 MUST 让 `ActionProfile` 明确声明 gameplay result 的网络策略。命中、伤害、目标归属、PvE/objective result 等权威结果 MUST 默认按服务器确认语义处理；如果允许客户端提交 proposal，该 proposal 策略 MUST 显式配置，不得从 HitWindow 隐式推导。

#### Scenario: 客户端提交命中 proposal

- **WHEN** 本地预测攻击产生命中候选
- **THEN** ActionProfile MUST 能声明该候选是否作为 GameplayResult proposal 进入 GameplayResultSyncDomain
- **AND** 最终 damage 和 target ownership MUST 等待服务器 confirmed result

#### Scenario: 只同步服务端结果

- **WHEN** 某个动作不允许客户端提交 result proposal
- **THEN** Resolver MUST 标记本地 result 只用于表现或调试
- **AND** Adapter MUST NOT 把该本地 result 映射为权威 GameplayResult packet

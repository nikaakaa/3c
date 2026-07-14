## REMOVED Requirements

### Requirement: ActionProfile 必须集中配置动作网络策略

ActionProfile 必须回归 gameplay 动作定义，网络策略迁移到具体 Network Model profile。

#### Scenario: 删除 ActionProfile 网络字段

- **WHEN** 本 change 完成
- **THEN** ActionProfile MUST 不再保存 prediction、authority 或 replication

### Requirement: Decision TreeClip 与 Blackboard Projection 必须表达窗口作者语义

窗口作者语义继续保留，但网络策略来源从 ActionProfile 迁移到 model profile，因此旧要求被新的模型边界要求取代。

#### Scenario: 迁移窗口 policy owner

- **WHEN** 本 change 完成
- **THEN** TreeClip projection MUST 不再预览 ActionProfile 网络字段

### Requirement: Motion 和 Cue 策略必须按输出类型集中解析

Motion/Cue 网络策略继续集中解析，但 owner 必须是具体 Network Model profile。

#### Scenario: 迁移 Motion/Cue policy

- **WHEN** 本 change 完成
- **THEN** ActionProfile MUST 不再保存 Motion/Cue 网络策略

### Requirement: Inspector 必须按作者职责分层

旧 UI 分层仍把 ActionProfile 当作网络策略入口，必须由 gameplay Action Inspector + model profile Inspector 取代。

#### Scenario: 删除旧 Network 分区

- **WHEN** 作者选中 ActionProfile
- **THEN** Inspector MUST 不再显示 Network policy 分区

### Requirement: Runtime Debug 必须按 ActionInstance 展示链路

旧 Debug 没有显式 Network Model identity，必须迁移为 gameplay lifecycle + model policy/packet 两层展示。

#### Scenario: 迁移 Action debug

- **WHEN** 本 change 完成
- **THEN** 网络 debug MUST 显示 ModelId 与 model policy source

### Requirement: ActionProfile Inspector 必须形成网络策略闭环

ActionProfile Inspector 不再是网络策略作者入口。

#### Scenario: 删除 ActionProfile packet preview

- **WHEN** 本 change 完成
- **THEN** ActionProfile Inspector MUST 不再编辑 expected packet

### Requirement: 策略模板必须显式写入正式配置

旧模板写入 ActionProfile 网络字段，必须删除并迁移到 model profile authoring。

#### Scenario: 删除旧模板

- **WHEN** 本 change 完成
- **THEN** ActionProfile MUST 不再提供 network policy template

### Requirement: Effective Network Policy 必须只读解析动作输出

旧 resolver 直接读取 ActionProfile，必须迁移为 model-owned resolver。

#### Scenario: 删除旧 resolver

- **WHEN** 本 change 完成
- **THEN** `ActionNetworkPolicyResolver` 通用类型 MUST 不再存在

### Requirement: GameplayResult 策略必须由 ActionProfile 声明

GameplayResult 网络策略必须归属具体 Network Model，而不是 gameplay ActionProfile。

#### Scenario: 迁移 result policy

- **WHEN** 本 change 完成
- **THEN** ActionProfile MUST 不再保存 result proposal/history/replication policy

## ADDED Requirements

### Requirement: ActionProfile 必须只保存 Gameplay 动作定义

ActionProfile MUST 唯一保存 ActionId、display、debug、tags、block/cancel、target 和其它 gameplay 动作定义。它 MUST NOT 保存 model id、prediction、authority、replication、history、snapshot、packet、window network policy、motion network policy、cue network policy 或 result network policy。

#### Scenario: 编辑 Attack 动作

- **WHEN** 作者选中 `Attack.Light.01` ActionProfile
- **THEN** Inspector MUST 只显示 gameplay 动作定义
- **AND** 网络策略 MUST 不在该资产中出现

### Requirement: ServerAuthoritative Action Policy 必须集中配置

`ServerAuthoritativeCharacterSyncProfile` MUST 按 ActionId 集中保存该模型的 Transaction 基础策略，以及 window、motion、cue 和 gameplay result 子策略。Graph、Timeline、Blackboard、ActionProfile 和 ActionContext MUST 不复制这些字段。

#### Scenario: 配置 Attack HitWindow

- **WHEN** 作者配置 HitWindow 的 authority、history、replication 和 digest
- **THEN** 修改 MUST 只发生在 ServerAuthoritative Action policy
- **AND** TreeClip MUST 继续只保存 WindowType/WindowId 与时间范围

### Requirement: Model Action Policy 必须通过稳定 ActionId 引用

Model profile MUST 使用稳定 ActionId 引用 CharacterPipelineDefinition 中的 ActionProfile。缺失 ActionProfile、重复 policy、错误 ActionId 或未覆盖模型所需输出 MUST 配置失败，不得按 display name 或目录搜索资产。

#### Scenario: ActionId 拼写错误

- **WHEN** model policy 引用不存在的 ActionId
- **THEN** 配置校验 MUST 报告错误
- **AND** MUST 不绑定同名 asset 或第一个 ActionProfile

### Requirement: Model Inspector 必须形成动作网络策略闭环

ServerAuthoritative model profile Inspector MUST 是当前模型动作网络策略的唯一作者入口。它 MUST 展示引用 ActionProfile、基础 policy、window/motion/cue/result policy、coverage error、effective policy 和 expected model packet preview。

#### Scenario: 查看 Attack 当前模型配置

- **WHEN** 作者在 model profile 中选中 `Attack.Light.01`
- **THEN** UI MUST 显示该 ActionId 的完整 ServerAuthoritative policy
- **AND** packet preview MUST 复用正式 model resolver/adapter 映射

### Requirement: Model Policy Template 必须写入模型资产

系统 MAY 在 ServerAuthoritative model Inspector 提供显式策略模板。模板 MUST 只在作者应用时把完整字段写入 model profile；runtime MUST 不读取模板名称，也 MUST 不使用缺失 policy 的默认模板 fallback。

#### Scenario: 应用本地预测近战模板

- **WHEN** 作者对 model profile 中的新 Action policy 应用模板
- **THEN** 完整 prediction/authority/replication/window/motion/cue/result 字段 MUST 写入该 model profile
- **AND** ActionProfile MUST 不被修改

### Requirement: Model Action Resolver 必须只读解析动作事实

ServerAuthoritative model MUST 提供 model-owned Action policy resolver，将 `ActionId/ActionInstanceId + fact kind + output type` 解析为只读 effective model policy。Resolver MUST 不修改 ActionRuntime、Graph、Timeline、Blackboard 或 facts，也 MUST 不成为其它 Network Model 的公共 resolver。

#### Scenario: 解析 root motion fact

- **WHEN** adapter 处理带 ActionInstanceId 的 resolved motion fact
- **THEN** resolver MUST 通过 ActionInstanceId 找到 ActionId 和对应 model policy
- **AND** MUST 按该模型的 motion policy 决定 packet/history

### Requirement: Model GameplayResult Policy 必须显式声明

ServerAuthoritative Action policy MUST 显式声明 gameplay result proposal、history、replication 和 digest。HitWindow policy MUST 不隐式创建 GameplayResult policy，命中、伤害和目标归属仍由权威 solver 决定。

#### Scenario: Attack 只发送 Window digest

- **WHEN** Attack policy 允许 HitWindow digest 但不允许客户端 GameplayResult proposal
- **THEN** adapter MAY 发送 window digest
- **AND** MUST 不发送 gameplay result proposal

### Requirement: Action Runtime Debug 必须分开展示 Gameplay 与 Model

Action Runtime Debug MUST 展示 ActionInstance gameplay lifecycle；ServerAuthoritative Debug MUST 展示 ModelId、ActionId、model policy、outgoing packet、decision 和 history。两者 MUST 通过稳定 ActionInstanceId 关联，但 MUST 不合并成第二份 ActionRuntime 状态。

#### Scenario: 查看服务端 Reject

- **WHEN** Attack ActionInstance 收到 ServerAuthoritative Reject
- **THEN** model debug MUST 显示 policy、packet 和 decision
- **AND** ActionRuntime debug MUST 显示转换后的 terminal lifecycle

## MODIFIED Requirements

### Requirement: 不恢复旧 ActionSO 或节点身份模块

系统 MUST NOT 恢复旧 `ActionSO`、`ActionModule`、`ActionSubTreeNode`、节点 action identity 或 BBB 状态类主线。`ActionProfile` 是 gameplay 动作身份和约束中心；`ServerAuthoritativeCharacterSyncProfile` 是当前 Network Model 的网络策略中心。两者 MUST 通过稳定 ActionId 引用，MUST NOT 复制 Graph、Timeline、Motion 或动画执行数据。

#### Scenario: 迁移网络策略后运行攻击

- **WHEN** Graph 提交 Attack ActionProfile 的 ActionId
- **THEN** ActionRuntime MUST 使用 ActionProfile 创建 ActionInstance
- **AND** ServerAuthoritative adapter MUST 使用同一 ActionId 查询 model policy
- **AND** 系统 MUST 不恢复旧 ActionSO 执行表

## ADDED Requirements

### Requirement: Pipeline Blackboard declaration 必须作为 Graph Data Catalog 的正式来源

Pipeline Blackboard authoring MUST 将当前 authoring context 可见的 `BaseExposedProperty` declaration 投影到统一 `Graph Data Catalog`。每个条目 MUST 保留 declaration identity、实际 owner、local/inherited 可见性、值类型、scope、lifetime、authority、sync policy、category 和默认值语义。该投影 MUST NOT 复制 declaration，也 MUST NOT 建立 ExposedProperty 与 Pipeline Blackboard 之外的第二套变量配置。

#### Scenario: 显示当前 Graph 本地 declaration

- **WHEN** 作者打开拥有本地 `CanDodgeMoveCancel` declaration 的 Dodge state body
- **THEN** 目录 MUST 将其显示为当前 owner 的 local editable Blackboard 条目

#### Scenario: 显示 RootTree declaration

- **WHEN** inline state body 可见 RootTree 声明的 `RunThreshold`
- **THEN** 目录 MUST 将其显示为 inherited read-only 条目并标明真实 owner

#### Scenario: 同 key 不同 owner

- **WHEN** 两个合法 owner 各自存在显示名相同但 identity 不同的 declaration
- **THEN** 目录 MUST 通过 declaration identity 和 owner 区分条目，MUST NOT 按显示名合并

### Requirement: Blackboard Catalog source 必须按 declaration 所有权限制写操作

Blackboard catalog source MUST 只允许作者编辑或删除当前 owner 持有的本地 declaration。继承 declaration MUST 是只读投影，并 MAY 提供定位原 owner 的命令。新增 declaration MUST 使用当前 owner 的正式 authoring API，并 MUST 遵守既有 scope/lifetime 合法组合。系统 MUST NOT 在当前 Graph 复制继承 declaration、静默改变 owner 或使用 fallback scope。

#### Scenario: 编辑本地默认值

- **WHEN** 作者在目录详情中修改当前 owner 的本地 Config declaration 默认值
- **THEN** 系统 MUST 更新该 owner 的原 declaration

#### Scenario: 删除继承 declaration

- **WHEN** 作者查看从 RootTree 继承的 declaration
- **THEN** 目录 MUST 不提供针对当前 inline graph 的删除命令

#### Scenario: 新建 State variable

- **WHEN** 当前 Graph owner 支持 State scope 且作者通过目录创建 State variable
- **THEN** 系统 MUST 创建属于当前 owner 的合法 declaration

### Requirement: Blackboard Catalog source 必须复用上下文化可见性和节点引用链路

Blackboard catalog source MUST 复用 Pipeline Blackboard 已有的 Graph/Transition context、local/inherited 可见性解析和显式 declaration reference 节点工厂。目录 MUST NOT 重新实现一套 owner 查找、裸 key 匹配或 runtime dictionary 查询。拖拽创建失败时 MUST 保持失败并报告原因，MUST NOT 写入零值、默认值或 object fallback 后继续 authoring。

#### Scenario: Transition 读取阈值

- **WHEN** 作者把可见的 `RunThreshold` 从目录拖入 ConditionRuleGraph
- **THEN** 系统 MUST 创建保存显式 declaration reference 的纯 ValueNode 兼容节点

#### Scenario: declaration 在当前 context 不可见

- **WHEN** 某 declaration 不属于当前 Graph 的 local/inherited 可见集合
- **THEN** 目录 MUST 不展示该条目，也 MUST NOT 通过裸 key 搜索把它加入结果

#### Scenario: 引用目标已失效

- **WHEN** 条目对应 declaration 在拖拽完成前被删除或 owner context 已切换
- **THEN** 节点创建 MUST 失败并报告失效引用，MUST NOT 创建绑定默认值的节点


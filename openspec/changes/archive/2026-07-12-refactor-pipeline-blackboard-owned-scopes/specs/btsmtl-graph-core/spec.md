## ADDED Requirements

### Requirement: BaseGraph declaration 必须保持局部所有权并支持显式外层引用

每个 `BaseGraph` MUST 只序列化自己拥有的 `BaseExposedProperty` declarations。Graph 节点 MAY 通过正式 variable reference 引用 authoring context 中可见的外层 declaration，但该 reference MUST NOT 把 declaration 复制进当前 Graph。Graph 克隆、inline ownership 和 shared asset 解析 MUST 保持 declaration identity 与 owner 关系。

#### Scenario: inline graph 创建局部 declaration

- **WHEN** 作者在 State body inline Graph 中创建 Graph scope declaration
- **THEN** declaration MUST 保存于该 inline Graph 的 exposed property 集合
- **AND** owner StateNode 被删除时该 declaration MUST 随 inline Graph 删除

#### Scenario: inline graph 引用 RootTree declaration

- **WHEN** inline Graph 中的节点引用 RootTree Character declaration
- **THEN** inline Graph MUST 只保存 variable reference
- **AND** inline Graph 的 exposed property 集合 MUST NOT 增加该 Character declaration 副本

#### Scenario: shared graph 运行实例

- **WHEN** 两个 owner 运行同一个 shared Graph
- **THEN** shared Graph declaration identity MUST 保持一致
- **AND** Graph scope runtime value MUST 由各自运行工作副本 identity 隔离

### Requirement: Graph evaluation context 必须携带变量访问所有权

Graph runtime 和下钻 evaluation context MUST 能向统一 blackboard resolver 提供当前 Graph runtime、active State、ActionInstance 和 local logic tick ownership。节点 MUST NOT 自行拼接字符串地址或从 asset path 推断 runtime owner。缺少 declaration 所需 owner 时读取或写入 MUST 失败。

#### Scenario: ConditionRuleGraph 继承 active State

- **WHEN** StateMachine runtime 求值 active State 的 Transition rule
- **THEN** ConditionRuleGraph MUST 继承 owner StateMachineGraph 的 runtime context 与 active `StateMachineExecutionScope`
- **AND** State scope variable reference MUST 解析到当前 activation bucket

#### Scenario: 孤立 Graph 缺少 Action context

- **WHEN** Graph 在没有 `ActionInstanceId` 的上下文中读取 ActionInstance scope declaration
- **THEN** resolver MUST 报告缺失 owner context
- **AND** 系统 MUST NOT 回退到 Character、Graph 或默认值


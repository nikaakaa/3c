## MODIFIED Requirements

### Requirement: Graph 引用保持 BaseTree 资产边界
系统 MUST 保持 Graph 引用模块和下钻 UI 使用 `BaseTree` 资产引用。`BaseGraph` 抽层 MUST NOT 让用户创建无法被现有 Taco 编辑器打开的裸 `BaseGraph` 引用。直接打开 Graph 资产 MUST 继续通过 `OpenTree()` 或等价的 TreeWindowUtility 入口打开；从当前编辑窗口内的节点 Graph 引用下钻时，编辑器 MAY 通过窗口页面栈打开该引用。

#### Scenario: 打开子 Graph
- **WHEN** 节点或模块暴露一个子 Graph 引用
- **THEN** 该引用 MUST 指向 `BaseTree` 或其子类资产
- **AND** 编辑器 MUST 能通过现有 Taco 编辑器窗口打开该引用
- **AND** 当前窗口内的下钻打开 MUST 保留页面栈上下文

#### Scenario: 直接打开 Graph 资产
- **WHEN** 用户通过 Project、Inspector 或 Tree Browser 直接打开一个 `BaseTree` 资产
- **THEN** 编辑器 MUST 继续使用 `OpenTree()` 或等价的 TreeWindowUtility 入口打开该资产
- **AND** 该打开操作 MUST NOT 要求来源节点上下文

#### Scenario: 不保存并行引用字段
- **WHEN** 节点模块需要保存下钻 Graph
- **THEN** 模块 MUST 保存一条正式的 `BaseTree` 引用字段
- **AND** 模块 MUST NOT 同时保存 `BaseTree` 和 `BaseGraph` 两套引用字段

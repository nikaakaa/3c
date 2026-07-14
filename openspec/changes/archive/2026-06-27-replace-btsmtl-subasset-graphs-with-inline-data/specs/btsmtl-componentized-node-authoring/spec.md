# btsmtl-componentized-node-authoring Specification Delta

## MODIFIED Requirements
### Requirement: Graph 嵌套是节点或模块语义
系统 MUST 把 Graph 引用、下钻命令、Graph 作用域、ownership 和循环验证视为节点、边或模块创作语义，而不是 `PropertyPort` 的职责。Graph reference MUST 支持 owner 内部 inline graph data 和显式 shared graph asset，默认私有图必须内联保存，不能保存为 owned embedded subasset。

#### Scenario: 打开子 Graph
- **WHEN** 节点、边或模块暴露 graph reference
- **THEN** BTSMTL MUST 从该 reference 解析 inline graph data 或 shared graph asset
- **AND** BTSMTL MUST NOT 通过属性端口连接推导下钻目标

#### Scenario: 默认私有 Graph 引用
- **WHEN** 节点、边或模块创建默认私有下钻 Graph
- **THEN** 该 Graph MUST 作为 owner 内部普通 C# inline graph data 自动绑定
- **AND** Graph 引用字段 MUST 是实现细节，不得成为默认创作流程中的必填手动拖拽步骤
- **AND** 系统 MUST NOT 创建 owned embedded subasset

#### Scenario: Shared Graph 引用
- **WHEN** 节点、边或模块引用 shared Graph asset
- **THEN** UI MUST 明确显示该引用是 shared
- **AND** 删除节点、边或模块时 MUST NOT 删除 shared Graph asset
- **AND** owner MUST NOT 同时保留 inline graph 真数据

#### Scenario: 循环引用
- **WHEN** Graph A 和 Graph B 通过 inline 或 shared graph reference 形成循环
- **THEN** 嵌套 Graph 校验 MUST 报告该循环非法

## ADDED Requirements
### Requirement: Graph Reference 互斥持有 inline 和 shared
系统 MUST 让每个 graph reference 在 inline graph data 和 shared graph asset 之间保持互斥。互斥规则属于正式数据模型，不是 editor-only 显示状态。

#### Scenario: 使用 inline graph data
- **WHEN** graph reference 没有 shared asset
- **THEN** resolved graph MUST 来自 owner 内部 inline graph data
- **AND** UI MUST 显示该引用为 `Inline`

#### Scenario: 使用 shared graph asset
- **WHEN** graph reference 设置 shared asset
- **THEN** resolved graph MUST 来自 shared asset
- **AND** owner 的 inline graph data MUST 被清理
- **AND** UI MUST 显示该引用为 `Shared Asset`

#### Scenario: 非法双持有
- **WHEN** graph reference 同时存在 inline graph data 和 shared asset
- **THEN** 校验 MUST 报告非法结构
- **AND** 系统 MUST NOT 静默选择其中一个作为 fallback

# btsmtl-graph-core Specification Delta

## ADDED Requirements

### Requirement: TreeWindow 支持 editor-only authoring context
系统 MUST 允许 `BaseTreeWindow` 持有 editor-only authoring context，用于 Tree Inspector 中依赖业务上下文的 authoring 区块展示当前打开入口提供的信息。该 context MUST NOT 序列化到 `BaseGraph`、`BaseTree`、`BaseTreeAsset`、节点、边或 property port 中。下钻 inline graph 或 shared graph 时，窗口 MUST 保持同一个 authoring context。

#### Scenario: 从业务定义打开 RootTree
- **WHEN** editor 通过业务定义打开某个 `BaseTreeAsset`
- **THEN** `BaseTreeWindow` MUST 接收该业务定义提供的 authoring context
- **AND** Graph 数据本身 MUST NOT 保存该 context

#### Scenario: 直接打开孤立 TreeAsset
- **WHEN** 用户直接打开一个普通 `BaseTreeAsset`
- **THEN** `BaseTreeWindow` MAY 没有业务 authoring context
- **AND** Inspector 中依赖业务 context 的区块 MUST 显示缺失上下文状态，而不是写入 fallback 配置

#### Scenario: 下钻 Graph
- **WHEN** 用户从 RootTree 下钻到 inline graph、shared graph 或 transition rule graph
- **THEN** 子页面 MUST 继承当前窗口的 authoring context
- **AND** 子 Graph MUST NOT 单独保存一份 context

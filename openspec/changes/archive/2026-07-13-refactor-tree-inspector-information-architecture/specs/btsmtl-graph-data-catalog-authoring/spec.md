# btsmtl-graph-data-catalog-authoring Specification

## MODIFIED Requirements

### Requirement: Tree Inspector 必须提供唯一 Graph Data Catalog

系统 MUST 在 Tree Inspector 的 `Data` 页中提供唯一 `Graph Data Catalog`，统一列出当前 authoring context 可用于图编辑的正式数据来源。Input 与 Pipeline Blackboard MUST 使用同一目录外壳、搜索入口、分组规则和条目视觉语法。`Inspector` 页 MUST NOT 同时显示、复制或嵌入 Catalog。系统 MUST NOT 同时保留独立 Input 素材区、独立 ExposedProperty 列表或其它并行的正式 Graph 数据目录。

#### Scenario: 在角色 RootTree 查看数据

- **WHEN** 作者打开带有 Character Pipeline authoring context 的 RootTree
- **THEN** Data 页的目录 MUST 同时展示当前 InputProfile 的 Input 条目和当前图可见的 Blackboard declaration
- **AND** Inspector 页 MUST 不重复显示该目录

#### Scenario: 在 inline state body 下钻

- **WHEN** 作者从 RootTree 下钻到 StateNode 的 inline graph
- **THEN** 同一 Data 页目录 MUST 按 inline graph 的当前上下文重新投影数据，而不是打开第二套面板

#### Scenario: 打开 Transition rule

- **WHEN** 作者选择可编辑的 Transition rule graph
- **THEN** 同一 Data 页目录 MUST 显示该 rule graph 可引用的数据与当前可用操作
- **AND** Inspector 页 MUST 继续只显示选中对象或图级 authoring settings

#### Scenario: 切换到 Inspector

- **WHEN** 作者从 Data 页切换到 Inspector 页
- **THEN** Catalog MUST 不可见且不响应拖拽、展开、创建、删除或详情编辑
- **AND** Catalog 的 editor-only 搜索、筛选和折叠状态 MAY 在当前 TreeWindow 内保留

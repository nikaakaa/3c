## RENAMED Requirements

- FROM: `### Requirement: Tree Inspector 必须提供唯一 Graph Data Catalog`
- TO: `### Requirement: Tree Workspace必须提供唯一Graph Data Catalog`

## MODIFIED Requirements

### Requirement: Tree Workspace必须提供唯一Graph Data Catalog

系统 MUST在Tree Workspace左侧Data区域提供唯一`Graph Data Catalog`，统一列出当前authoring context可用于图编辑的正式数据来源。Input与Pipeline Blackboard MUST使用同一目录外壳、搜索入口、分组规则和条目视觉语法。右侧Details MUST只显示当前selection或Graph Authoring Settings，不得复制、嵌入或持有Catalog。系统 MUST不保留独立Input素材区、独立ExposedProperty列表、旧Data/Inspector互斥页签或其它并行正式Graph数据目录。

#### Scenario: 在角色RootTree查看数据

- **WHEN** 作者打开带Character Pipeline authoring context的RootTree
- **THEN** 左侧Data区域 MUST同时展示当前InputProfile的Input条目和当前图可见Blackboard declaration
- **AND** 右侧Details MUST不重复显示该目录

#### Scenario: 在inline state body下钻

- **WHEN** 作者从RootTree下钻到StateNode的inline graph
- **THEN** 同一Data区域 MUST按inline graph当前上下文重新投影数据
- **AND** MUST不打开第二套面板或替换右侧Details owner

#### Scenario: 打开Transition rule

- **WHEN** 作者选择可编辑Transition rule graph
- **THEN** 同一Data区域 MUST显示该rule graph可引用的数据与当前可用操作
- **AND** 右侧Details MUST继续只显示选中对象或图级authoring settings

#### Scenario: 选择Graph节点

- **WHEN** 作者在Graph Canvas选择Node或Edge
- **THEN** Catalog MUST保持可见并保留editor-only搜索、筛选和折叠状态
- **AND** Catalog MUST不响应Details字段修改为第二条catalog mutation
- **AND** Details MUST不复制Catalog条目


# Change: 统一 Graph Data Catalog 编辑体验

## Why

Tree Inspector 目前把角色输入显示为独立的只读 `Input` 素材区，把 Pipeline Blackboard 显示为另一套可编辑面板。两者都承担“把图可用数据拖入当前 Graph”的职责，却使用不同的标题、行高、字段布局、筛选器和操作位置：Input 条目接近紧凑资源行，Blackboard 条目则把所有权塞进名称并使用较大的可展开卡片。作者必须先理解两套 UI，才能判断数据是什么、来自哪里、能否编辑以及能否拖入当前图。

这不是运行时数据模型需要合并，而是 authoring 目录缺少统一的信息架构。若直接把 Input 变成 Blackboard，会复制 `CharacterInputProfile` 定义并破坏输入权威；若只重绘样式，则仍会留下两套搜索、分类和拖拽入口。需要建立一个编辑器专用的 `Graph Data Catalog`，统一展示与交互语法，同时明确保留各数据源的所有权和权限边界。

## What Changes

- 在 Tree Inspector 中以单一 `Graph Data` 目录替换独立 `Input` 素材区和独立 Pipeline Blackboard 列表。
- 引入编辑器专用的目录条目、来源、能力和上下文投影合同；该合同不进入图资产序列化，也不参与运行时寻址。
- 将 `CharacterInputProfile` 投影为外部只读 Input 条目，保留 input value/action request 的稳定身份和原生节点创建链路。
- 将当前图可见的 Pipeline Blackboard declaration 投影为本地或继承条目，保留 owner、scope、lifetime、category、类型、默认值和编辑权限。
- 统一两类条目的紧凑行结构、类型标记、名称、类型、来源/所有权、权限状态、拖拽状态和详情展开方式。
- 统一搜索、来源筛选、上下文筛选和层级分类；未分类 Blackboard declaration 显式进入 `Uncategorized`。
- 将新增 declaration 的 `+` 限定为 Blackboard 命令，并改为按需展开的内联创建条；Input 条目始终不可在目录中编辑或新增。
- 根据当前 Graph/Transition authoring context 计算可用能力；不支持节点创建的上下文不提供对应拖拽命令，也不建立 fallback 创建路径。
- 删除旧 Input panel provider/view/style、旧 ExposedProperty 大卡片视图以及仅服务旧 panel 注入方式的 registry，确保只有一个正式目录入口。
- 不改变 `CharacterInputProfile`、`BaseExposedProperty`、Pipeline Blackboard runtime、输入 runtime 或节点资产的业务权威和序列化结构。

## Impact

- Affected specs:
  - `btsmtl-graph-data-catalog-authoring`（新增）
  - `character-input-node-authoring`（修改并移除独立 Input 素材区要求）
  - `character-pipeline-blackboard`（补充统一目录中的 declaration 来源与权限要求）
- Affected editor areas:
  - BTSMTL `BaseTreeInspectorView`、Inspector UXML/USS 与 ExposedProperty authoring UI
  - Character Pipeline 的 Input authoring 投影与拖拽节点工厂接入
  - Graph/Transition authoring context、目录来源注册和刷新链路
- Runtime impact:
  - 无运行时行为、网络映射、Blackboard address、InputFrame 或 request buffer 变更。
  - 不增加第二份输入配置，不把 Input 转换成 Blackboard declaration。
- Breaking authoring change:
  - Tree Inspector 不再提供独立 `Input` 素材区和旧 ExposedProperty 列表。
  - 编辑器扩展若依赖旧 `ITreeInspectorGraphPanelProvider` 或旧视图模板，必须迁移到统一目录来源合同；不保留兼容入口。

## Dependencies And Conflicts

- 本 change 依赖 `refactor-pipeline-blackboard-owned-scopes` 的最终实现：Graph owner、local/inherited 可见性、scope/lifetime、`CategoryPath` 和 Graph/Transition 上下文必须先成为稳定事实。该 change 当前已完成但尚未归档；实施本 change 前必须先归档它，或以它的最终实现和 delta 为基线显式 rebase，不能并行维护旧黑板面板。
- 现行 `character-input-node-authoring` 要求 Tree Inspector 提供独立 `Input` 素材区，与本 change 直接冲突。本 change 通过 `MODIFIED` 和 `REMOVED` delta 替换该要求，不保留两个正式入口。
- `refactor-pipeline-blackboard-owned-scopes` 要求 Graph tab 与 Transition selection 都可访问上下文化 Blackboard 视图，与统一目录目标一致。本 change 只替换其展示外壳和来源组织，不撤销 owner、scope、lifetime 或可见性语义。
- 现行输入 runtime、Pipeline Blackboard runtime 与 SyncFacts 分层不冲突；统一仅发生在编辑器投影视图，不合并数据权威。


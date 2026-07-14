# Design: 统一 Graph Data Catalog 编辑体验

## Context

当前 Tree Inspector 存在两条相邻但分裂的 authoring 链路：

- Character Pipeline editor 通过 `ITreeInspectorGraphPanelProvider` 注入独立 `Input` 区域，直接读取 `CharacterPipelineDefinition.InputProfile`，并用 `CharacterInputDefinitionView` 提供拖拽创建。
- BTSMTL `BaseTreeInspectorView` 直接持有 ExposedProperty 搜索、scope/context 过滤、创建和 `ExposedPropertyView`，把 Pipeline Blackboard declaration 显示为另一套列表。

两条链路背后的业务权威本来就不同。Input 是角色输入配置的外部只读事实；Blackboard 是当前 RootTree/inline/shared Graph 持有的 declaration。问题出在 UI 将“权威差异”表达成“两套目录”，而不是在同一目录中表达来源、所有权和权限。

## Goals

- 作者只需要在一个位置搜索、识别和拖入当前图可用的数据。
- Input 与 Blackboard 使用一致的视觉语法，但清楚显示来源、所有权和可编辑性。
- 保留 InputProfile 与 Graph-owned declaration 的唯一权威，不复制数据。
- Graph、inline state body 和 Transition rule 使用同一目录合同及上下文过滤语义。
- 删除旧入口和专用注入路径，使后续数据来源可以通过稳定合同扩展。

## Non-Goals

- 不把 Input value/action request 声明迁移成 Blackboard variable。
- 不改变 InputFrame、request buffer、Pipeline Blackboard runtime 或网络同步语义。
- 不新增通用网络 key/value 变量层。
- 不重做 BTSMTL 节点序列化、PropertyPort、PropertyEdge 或节点搜索菜单。
- 不为大量数据预先引入虚拟化、分页或独立数据浏览窗口。

## Decisions

### 1. 统一目录，不统一数据权威

`Graph Data Catalog` 是编辑器专用投影。它从多个正式来源读取条目，但不保存条目副本。Input 条目始终回指 `CharacterInputProfile` 的稳定定义；Blackboard 条目始终回指其 declaration owner 和 declaration identity。

业务取舍：作者获得统一入口，同时输入配置仍由 Profile 集中管理，图变量仍由对应 Graph 管理。代价是目录必须显式处理不同来源的权限，不能假设所有行都可编辑。

### 2. BaseTreeInspector 持有唯一目录外壳，来源通过合同贡献条目

BTSMTL editor core 持有目录 UI、查询状态、分组、行渲染和命令分发。来源实现只负责：

- 根据 `GraphDataCatalogContext` 生成编辑器投影条目；
- 声明条目的来源、种类、所有权、可变性和能力；
- 执行该来源拥有的命令；
- 在源数据变化时使目录失效并刷新。

Core 内建 Blackboard 来源。Character Pipeline editor 在存在正式 `CharacterPipelineDefinition` authoring context 时贡献 Input 来源。Core 不引用 Character 类型，Character 扩展也不注入第二块 UI。

业务取舍：以后增加正式数据来源时可以接入同一目录，而不再复制面板。代价是需要一个明确的 editor contract 和失效机制，但该复杂度被限制在 authoring 层。

### 3. 条目以能力描述交互，不通过来源类型硬编码分支

目录条目至少携带以下投影信息：

- 稳定条目身份；
- source kind 与 entry kind；
- display name、value type、category path；
- external/local/inherited ownership 与 owner label；
- mutable/read-only 状态；
- 当前上下文能力，例如 `DragCreateNode`、`ExpandDetails`、`Edit`、`Delete`、`LocateSource`；
- 类型颜色/图标和用于 tooltip 的完整文本。

目录 UI 只根据能力显示或启用操作。具体节点工厂、编辑和删除仍由来源执行。

业务取舍：Input、Blackboard 以及未来来源可以共享 UI，而不会把 Input 的只读规则散落在视图判断中。代价是能力计算必须随 authoring context 刷新，否则会出现错误操作入口。

### 4. 使用固定的来源层级与共享行语法

目录默认层级为：

- `Input / Values`
- `Input / Requests`
- `Blackboard / <CategoryPath>`
- `Blackboard / Uncategorized`

每行使用稳定紧凑高度：左侧窄类型色条或类型图标，随后是左对齐名称、类型、来源/所有权元数据，尾部显示锁定、定位、展开或菜单命令。所有权不再拼进名称。长文本必须截断并提供 tooltip，不能改变行高或遮挡相邻控件。

Input 行显示外部只读和 Profile 来源；本地 Blackboard 行显示可编辑；继承 Blackboard 行显示 owner 和只读状态。颜色只表达值类型，不承担所有权语义。

业务取舍：作者可快速横向比较不同来源的数据，又不会把“蓝色”误解为“只读”或把 owner 当成变量名。代价是窄 Inspector 中必须控制列宽并把低频字段放进详情。

### 5. 搜索与过滤统一，但来源专属条件不伪装成通用字段

目录提供一个文本搜索入口和紧凑过滤控件：

- Source：`All`、`Input`、`Blackboard`；
- Blackboard ownership/context：`All Visible`、`Current Context`、`Local`、`Inherited` 等由当前已有语义提供的选项；
- Blackboard scope：仅在 Blackboard 来源参与结果时有效。

文本搜索覆盖名称、类型、category、owner 和 source。选择 Blackboard 专属过滤条件时，Input 条目被排除，而不是被赋予虚假的 scope。清空专属过滤后，两类来源重新共同显示。

业务取舍：减少重复搜索和面板切换，同时不污染 Input 数据模型。代价是过滤 UI 需要明确显示当前过滤条件作用于哪个来源。

### 6. 新增命令只创建 Blackboard declaration

目录标题栏保留一个 `+`。点击后在目录内部展开单一创建条，包含名称、scope、类型以及确认/取消；可选字段在创建后通过详情编辑。创建条只调用 Blackboard 来源，且只允许当前 owner 支持的合法 scope/lifetime 组合。

Input 不显示新增、改名或删除命令。需要修改输入配置时，通过 `LocateSource` 定位 `CharacterInputProfile`，Profile Inspector 仍是唯一编辑入口。

业务取舍：高频创建保持快速，低频元数据不挤占常驻空间；同时不会让统一 UI 暗示 Input 可在图中修改。代价是完整 declaration 配置分两步完成。

### 7. 详情在当前目录内按需展开

本地 Blackboard 条目可在行下展开编辑类型允许的默认值、scope、lifetime、authority、sync policy 和 category。继承 Blackboard 与 Input 可展开只读详情，并可提供定位 owner/source 的命令。详情不使用第二个并列详情面板，避免窄 Inspector 中压缩目录宽度。

业务取舍：常态保持紧凑，作者需要时仍能看到完整数据。代价是展开项会增加纵向长度，但当前数据规模适合 ScrollView。

### 8. 节点拖拽沿用正式工厂，能力按目标上下文计算

拖拽 Input 条目继续创建绑定稳定 input id/request id 的正式信息节点。拖拽 Blackboard 条目继续创建显式 declaration reference 的正式节点。目录不生成 object fallback 节点，也不复制配置。

若当前 TreeView/ConditionRuleGraph 不允许某种节点，来源不授予 `DragCreateNode`，并显示不可用原因。目录不得绕过图类型规则或建立临时创建路径。

业务取舍：统一入口不会削弱节点类型安全。代价是同一条目在不同上下文可能具有不同操作能力，UI 必须即时反馈。

### 9. Graph 与 Transition 共享目录实例和查询状态

目录由 Tree Inspector 持有，不属于某一个可替换页面。Graph tab、inline graph 下钻和 Transition selection 切换时，目录更新 `GraphDataCatalogContext` 并重新投影可见条目；搜索与折叠状态在同一 TreeWindow 内保留。来源条目、权限和拖拽目标必须以当前上下文重新计算。

业务取舍：作者切换状态 body 或 Transition 时不必重新学习和定位数据。代价是上下文切换必须有严格的刷新边界，不能沿用上一 Graph 的条目引用。

### 10. 直接删除旧 UI 链路

统一目录接入完成后删除：

- 独立 Input extension container、title、panel provider 和专用 definition row；
- 旧 ExposedProperty 大卡片模板/样式和由 `BaseTreeInspectorView` 直接管理的双份列表状态；
- 仅用于注入独立 Graph panel 且没有其它正式实现的 registry/contract；
- 对应无引用样式、资源和刷新分支。

不保留 hidden legacy panel、兼容 adapter 或双写刷新。

业务取舍：代码和 authoring 入口保持单一，避免后续修复两套 UI。代价是依赖旧 editor extension contract 的代码必须同步迁移，不能渐进共存。

## Alternatives Considered

### 方案 A：只统一颜色、字号和行高，保留两个面板

优点是改动最小，风险集中在 USS/UXML。缺点是搜索、分类、拖拽和上下文仍是两套，作者仍需理解“去哪一块找数据”，也无法解决 owner 被塞进名称的问题。它改善外观但不解决 authoring 信息架构，因此不采用。

### 方案 B：把 InputProfile 项全部转成 Blackboard declaration

优点是运行时和 UI 都只剩一种变量。缺点是 input value/request 不是普通可调变量，转换会复制输入定义、模糊 request 消费语义，并让 Profile 与 Graph 争夺权威。它会制造更深的数据分裂，因此不采用。

### 方案 C：目录列表加独立右侧详情面板

优点是详情结构稳定，适合大型数据浏览器。缺点是 Tree Inspector 宽度有限，右侧详情会挤压名称、类型和 owner，频繁选择也增加操作成本。当前规模使用行内展开更合适；未来数据量显著增长时可重新评估独立窗口，而不是在本 change 预先加入。

### 方案 D：立即使用虚拟化 ListView

优点是大量条目滚动成本较低。缺点是可展开详情、拖拽、分组和动态高度会显著增加实现复杂度，而当前每个角色的 Input 与 Blackboard 规模不足以证明该成本。先使用单一 ScrollView 和稳定行尺寸，来源合同保持未来替换渲染器的可能性。

## Risks And Mitigations

- **窄 Inspector 信息拥挤**：常驻行只保留高频字段，长文本截断并提供 tooltip，完整 metadata 放入详情。
- **上下文切换残留旧条目或旧能力**：目录条目绑定 context generation；切换 Graph/Transition 时先失效旧投影，再从来源重建。
- **拖拽与本地重排手势冲突**：只有声明支持且当前上下文允许的能力才启动对应手势；Blackboard 分类重排若保留，必须使用独立 handle 或明确 drop target。
- **Input source 缺失时产生隐式 fallback**：目录显示明确的 unavailable 状态和缺失上下文原因，不搜索场景、不猜 Profile、不创建空绑定节点。
- **与未归档黑板 change 重叠**：实施前固定依赖基线；统一目录只消费其 owner/scope/context API，不重新实现一套可见性解析。

## Migration

1. 以 `refactor-pipeline-blackboard-owned-scopes` 最终代码为基线，确认 local/inherited、scope、category 和 Transition context 已有单一 API。
2. 建立 editor-only catalog contract 与唯一 Inspector shell。
3. 先接入 Blackboard 来源并迁移现有编辑命令。
4. 再接入 Input 只读来源与原生节点工厂。
5. 切换所有 Graph/Transition authoring context 到统一目录。
6. 删除旧 Input/ExposedProperty UI 和旧 extension registry，不保留兼容路径。

## Open Questions

无阻塞业务决策。实现时仅需依据现有类型系统确定类型图标/颜色映射和合法 scope/lifetime 选项，这些属于既有语义的 UI 投影，不新增运行时合同。


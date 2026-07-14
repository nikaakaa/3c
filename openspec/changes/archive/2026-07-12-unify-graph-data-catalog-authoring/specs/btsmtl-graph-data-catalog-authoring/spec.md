## ADDED Requirements

### Requirement: Tree Inspector 必须提供唯一 Graph Data Catalog

系统 MUST 在 Tree Inspector 中提供唯一 `Graph Data Catalog`，统一列出当前 authoring context 可用于图编辑的正式数据来源。Input 与 Pipeline Blackboard MUST 使用同一目录外壳、搜索入口、分组规则和条目视觉语法。系统 MUST NOT 同时保留独立 Input 素材区、独立 ExposedProperty 列表或其它并行的正式 Graph 数据目录。

#### Scenario: 在角色 RootTree 查看数据

- **WHEN** 作者打开带有 Character Pipeline authoring context 的 RootTree
- **THEN** 目录 MUST 同时展示当前 InputProfile 的 Input 条目和当前图可见的 Blackboard declaration

#### Scenario: 在 inline state body 下钻

- **WHEN** 作者从 RootTree 下钻到 StateNode 的 inline graph
- **THEN** 同一目录 MUST 按 inline graph 的当前上下文重新投影数据，而不是打开第二套面板

#### Scenario: 打开 Transition rule

- **WHEN** 作者选择可编辑的 Transition rule graph
- **THEN** 同一目录 MUST 显示该 rule graph 可引用的数据与当前可用操作

### Requirement: Graph Data Catalog 必须是编辑器专用投影

目录 MUST 从各正式数据来源即时构建 editor-only 条目，MUST NOT 保存 Input 或 Blackboard 的第二份定义。目录条目、过滤状态、折叠状态和能力状态 MUST NOT 进入图资产 runtime 序列化，也 MUST NOT 成为运行时寻址、网络同步或事实提交的数据来源。

#### Scenario: 展示 Input 条目

- **WHEN** 目录展示一个 `MoveAxis` Input 条目
- **THEN** 条目 MUST 回指 InputProfile 的稳定定义，并且 MUST NOT 在 Tree asset 中复制 InputAction 配置

#### Scenario: 展示 Blackboard 条目

- **WHEN** 目录展示一个继承自 RootTree 的 `RunThreshold`
- **THEN** 条目 MUST 回指原 declaration owner 和 identity，并且 MUST NOT 在当前 Graph 创建副本

#### Scenario: 保存图资产

- **WHEN** 作者只改变目录搜索、过滤或折叠状态
- **THEN** 系统 MUST NOT 将图资产标记为包含新的 gameplay/runtime 数据

### Requirement: Catalog source 必须声明条目所有权和能力

每个目录来源 MUST 为条目提供稳定 identity、source kind、entry kind、名称、值类型、category、所有权、owner/source 描述、可变性和当前上下文能力。目录 UI MUST 根据能力提供拖拽创建、详情、编辑、删除或定位操作，MUST NOT 通过显示名或来源类型猜测命令。上下文不支持某操作时，系统 MUST 不提供该操作并 MUST 能说明不可用原因。

#### Scenario: 本地 Blackboard declaration

- **WHEN** 条目属于当前 Graph owner 且 declaration 可编辑
- **THEN** 来源 MUST 授予适用的详情、编辑、删除和节点创建能力

#### Scenario: 继承 Blackboard declaration

- **WHEN** 条目由上层 owner 声明并在当前 Graph 可见
- **THEN** 来源 MUST 将其标记为 inherited read-only，并 MAY 提供定位 owner 能力

#### Scenario: 当前图拒绝某种节点

- **WHEN** 当前 Graph 类型不允许由某条目创建对应节点
- **THEN** 来源 MUST 撤销 `DragCreateNode` 能力，且系统 MUST NOT 绕过图类型规则创建 fallback 节点

### Requirement: Catalog 条目必须使用统一且可区分权限的行语法

Input 与 Blackboard 条目 MUST 使用相同的稳定紧凑行结构。行 MUST 分离显示名称、值类型、来源/所有权和操作状态；MUST NOT 把 `[Local: Owner]`、`[Inherited: Owner]` 等元数据拼接进名称。长文本 MUST 截断并提供完整 tooltip，动态内容 MUST NOT 改变条目头部尺寸或遮挡相邻控件。颜色或类型图标 MUST 表达值类型，锁定/权限控件 MUST 独立表达只读状态。

#### Scenario: 对比 Input 与 Blackboard

- **WHEN** `MoveAxis` Input 与 `MoveAxisScale` Blackboard declaration 同时可见
- **THEN** 两行 MUST 使用一致布局，并分别清楚显示 external read-only 与 local editable 状态

#### Scenario: 显示长 owner 名称

- **WHEN** inherited declaration 的 owner 名称超过可用宽度
- **THEN** 行 MUST 保持稳定尺寸、截断 owner，并通过 tooltip 提供完整文本

#### Scenario: 展开完整 metadata

- **WHEN** 作者展开一个目录条目
- **THEN** 目录 MUST 在该条目下方展示来源允许的可编辑或只读详情，而不创建另一套详情窗口

### Requirement: Catalog 必须统一搜索、来源过滤和层级分组

目录 MUST 提供一个文本搜索入口，并 MUST 支持按 source、当前 Blackboard 可见性上下文和 Blackboard scope 过滤。搜索 MUST 覆盖名称、类型、category、owner 和 source。Blackboard 专属过滤条件 MUST 只作用于 Blackboard 条目；系统 MUST NOT 为 Input 构造虚假 scope 或 owner。目录 MUST 使用固定的 `Input/Values`、`Input/Requests`、`Blackboard/<CategoryPath>` 层级，并将空 Blackboard category 统一显示为 `Blackboard/Uncategorized`。

#### Scenario: 搜索跨来源名称

- **WHEN** 作者在 Source 为 All 时搜索匹配 Input 和 Blackboard 的文本
- **THEN** 目录 MUST 在同一结果视图中返回两个来源的匹配条目

#### Scenario: 过滤当前上下文 Blackboard

- **WHEN** 作者选择只看 Current Context Blackboard
- **THEN** 目录 MUST 只显示匹配的 Blackboard 条目，且 MUST NOT 将 Input 当作具有 Graph scope 的条目

#### Scenario: 未分类 declaration

- **WHEN** 多个 Blackboard declaration 的 `CategoryPath` 为空
- **THEN** 目录 MUST 将它们放入同一个 `Blackboard/Uncategorized` 分组

### Requirement: Catalog 新增入口必须只创建当前 owner 的 Blackboard declaration

目录的全局新增命令 MUST 只调用 Pipeline Blackboard declaration 来源。新增命令 MUST 按需展开内联创建条，并至少收集名称、合法 scope 和值类型。系统 MUST NOT 通过该入口创建或修改 InputProfile 定义，也 MUST NOT 在当前 owner 不支持所选 scope 时创建非法 declaration。

#### Scenario: 新增本地图变量

- **WHEN** 作者在当前 inline graph 点击新增并提交合法名称、scope 和类型
- **THEN** 系统 MUST 通过当前 Graph owner 的正式 declaration API 创建本地 Blackboard declaration

#### Scenario: 尝试在目录新增 Input

- **WHEN** 目录同时显示 Input 来源
- **THEN** 新增入口 MUST NOT 提供 Input 类型或写入 CharacterInputProfile

#### Scenario: 非法 scope

- **WHEN** 当前 owner 不允许作者选择某个 scope
- **THEN** 创建条 MUST 不提供该选项，且 MUST NOT 静默改成 fallback scope

### Requirement: Catalog 必须在 authoring context 切换时重建投影

Graph tab、inline/shared Graph 下钻、返回和 Transition selection 切换时，目录 MUST 使上一 authoring context 的条目与能力失效，并按新 context 从正式来源重建投影。搜索、来源过滤和适用的折叠状态 MAY 在同一 TreeWindow 内保留，但旧 Graph 的对象引用、owner、可见性和命令 MUST NOT 泄漏到新 context。

#### Scenario: 从 Attack1 切换到 Attack2

- **WHEN** 作者从 Attack1 inline state body 下钻到 Attack2 inline state body
- **THEN** 目录 MUST 重新计算本地/继承 declaration 和节点创建能力

#### Scenario: 从 Graph 切换到 Transition

- **WHEN** 作者选择一个 Transition rule graph
- **THEN** 目录 MUST 丢弃原 Graph 的命令目标并绑定 Transition 的正式 authoring context

#### Scenario: 缺少来源上下文

- **WHEN** 某正式来源无法从当前 context 解析
- **THEN** 目录 MUST 显示明确 unavailable 原因，并 MUST NOT 搜索场景、猜测资产或使用上一次 context 的来源


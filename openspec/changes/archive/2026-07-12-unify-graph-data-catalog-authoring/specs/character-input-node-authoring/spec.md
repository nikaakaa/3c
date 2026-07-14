## MODIFIED Requirements

### Requirement: 输入信息节点必须由输入配置创建并绑定

系统 MUST 将 input value 和 action request 配置作为输入信息节点的数据源。编辑器 MUST 在 Tree Inspector 的统一 `Graph Data Catalog` 中提供 `CharacterPipelineDefinition.InputProfile` 的只读 Input 投影，并允许从该投影创建输入信息节点。节点绑定 MUST 使用稳定身份，MUST NOT 依赖显示名匹配。Graph 中 MAY 存在多个引用同一 input value id 或 request id 的读取节点；这些节点 MUST 共享同一 Profile 定义作为数据源，MUST NOT 复制 InputAction 配置。Profile Inspector、Graph 搜索菜单、手动 ObjectField 或独立 Input 素材区 MUST NOT 成为并行的输入信息节点配置入口。

#### Scenario: Profile 新增 MoveAxis

- **WHEN** 作者在当前角色的 `CharacterInputProfile` 中新增 `MoveAxis`
- **THEN** 作者从 Tree Inspector 的 `Graph Data Catalog/Input/Values` 拖拽该输入项时 MUST 能生成对应 `MoveAxis` 输入信息节点

#### Scenario: Profile 删除输入值

- **WHEN** 作者删除某个 input value 定义
- **THEN** 引用该 id 的节点 MUST 报告配置错误，且目录 MUST 不保留该定义的副本

#### Scenario: InputAction 重命名

- **WHEN** `Player/Move` 的显示名发生变化但 action identity 未变化
- **THEN** 对应输入信息节点 MUST 继续绑定同一输入定义

### Requirement: Profile Inspector 不承担 Graph 节点创建入口

系统 MUST 将 `CharacterInputProfile` Inspector 限定为输入配置编辑和配置错误展示。Profile Inspector MUST NOT 暴露直接创建 BTSMTL 输入信息节点的拖拽条目。Graph 中输入信息节点的正式创建入口 MUST 是 Tree Inspector 的统一 `Graph Data Catalog`；目录 MAY 提供定位 Profile 的命令，但 MUST NOT 在目录内编辑 Input 定义。

#### Scenario: 编辑输入配置

- **WHEN** 用户打开 `CharacterInputProfile` Inspector
- **THEN** 用户 MAY 编辑 input value 和 action request 配置

#### Scenario: 创建 Graph 输入节点

- **WHEN** 用户需要在 Graph 中读取 `MoveAxis`
- **THEN** 用户 MUST 从 Tree Inspector 的 `Graph Data Catalog/Input/Values` 拖出该输入项

#### Scenario: 定位输入配置

- **WHEN** 用户从目录中的 Input 条目选择定位来源
- **THEN** 编辑器 MAY 定位该 `CharacterInputProfile`，但 MUST NOT 创建第二个编辑入口

## REMOVED Requirements

### Requirement: Tree Inspector 的 Graph 页必须提供 Input 素材区

**Reason**：独立 Input 素材区被统一 `Graph Data Catalog` 替代；继续保留会形成第二套搜索、分组和创建入口。

**Migration**：原 `CharacterPipelineDefinition.InputProfile` 只读投影迁移到 `Graph Data Catalog/Input` 来源分组，数据权威和稳定身份不变。

#### Scenario: 显示当前角色输入

- **WHEN** TreeWindow 带有 `CharacterPipelineDefinition` authoring context
- **THEN** 系统不再创建独立 Input 素材区，而由统一目录展示该 definition 的 `InputProfile`

#### Scenario: 缺少角色输入上下文

- **WHEN** 用户直接打开孤立 `BaseTreeAsset`
- **THEN** 统一目录 MUST 显示 Input 来源缺少 `CharacterPipelineDefinition` 上下文

### Requirement: Input 素材区条目必须像 ExposedProperty 一样可拖拽生成节点

**Reason**：拖拽能力保留，但其宿主从独立 Input 素材区迁移到统一目录，旧 requirement 的 UI 边界不再成立。

**Migration**：input value/action request 条目由 Input catalog source 提供 `DragCreateNode` 能力，并继续调用原生 typed node 工厂。

#### Scenario: 拖出 MoveAxis

- **WHEN** 用户把 `MoveAxis : Vector2` 从统一目录拖到 Graph
- **THEN** 系统 MUST 创建新的 `CharacterInputVector2InfoNode`

#### Scenario: 拖出 Attack request

- **WHEN** 用户把 `Attack` action request 从统一目录拖到 ConditionRuleGraph 或普通 Graph
- **THEN** 系统 MUST 创建新的 `CharacterActionRequestInfoNode`

#### Scenario: 重复拖拽同一输入

- **WHEN** 当前 Graph 已存在同 id 且同类型的输入信息节点
- **THEN** 再次拖拽该输入 MUST 创建另一个绑定同一 id 且同类型的读取节点


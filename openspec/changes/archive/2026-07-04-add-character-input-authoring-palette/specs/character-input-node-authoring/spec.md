# character-input-node-authoring Specification Delta

## MODIFIED Requirements

### Requirement: 输入信息节点必须由输入配置创建并绑定
系统 MUST 将 input value 和 action request 配置作为输入信息节点的数据源。编辑器 MUST 在 Tree Inspector 的 `Graph` 页内提供从 `CharacterPipelineDefinition.InputProfile` 创建输入信息节点的正式 `Input` 素材区。节点绑定 MUST 使用稳定身份，MUST NOT 依赖显示名匹配。Graph 中 MAY 存在多个引用同一 input value id 或 request id 的读取节点；这些节点 MUST 共享同一 Profile 定义作为数据源，MUST NOT 复制 InputAction 配置。Profile Inspector、Graph 搜索菜单或手动 ObjectField MUST NOT 成为并行的输入信息节点配置入口。

#### Scenario: Profile 新增 MoveAxis
- **WHEN** 作者在当前角色的 `CharacterInputProfile` 中新增 `MoveAxis`
- **THEN** 作者从 Tree Inspector 的 `Graph` 页内 `Input` 素材区拖拽该输入项时 MUST 能生成对应 `MoveAxis` 输入信息节点
- **AND** 该节点 MUST 引用输入定义稳定身份

#### Scenario: Profile 删除输入值
- **WHEN** 作者删除某个 input value 定义
- **THEN** 引用该 id 的节点 MUST 报告配置错误
- **AND** 系统 MUST NOT 自动创建 fallback 输入节点或改读其它输入值

#### Scenario: InputAction 重命名
- **WHEN** `Player/Move` 的显示名发生变化但 action identity 未变化
- **THEN** 对应输入信息节点 MUST 继续绑定同一输入定义
- **AND** Graph 连接 MUST NOT 因显示名变化断开

## ADDED Requirements

### Requirement: Tree Inspector 的 Graph 页必须提供 Input 素材区
系统 MUST 在 BTSMTL Tree Inspector 的 `Graph` 页内提供与 ExposedProperty 同级的 `Input` 素材区。该素材区 MUST 展示当前 `CharacterPipelineDefinition.InputProfile` 的 input value 和 action request 定义。该素材区 MUST 是只读投影，MUST NOT 编辑 `CharacterInputProfile`，MUST NOT 在 Graph 中保存第二份输入定义。

#### Scenario: 显示当前角色输入
- **WHEN** TreeWindow 带有 `CharacterPipelineDefinition` authoring context
- **THEN** `Graph` 页内的 `Input` 素材区 MUST 展示该 definition 的 `InputProfile`
- **AND** input value MUST 显示 id 和 value type
- **AND** action request MUST 显示 request id

#### Scenario: 缺少角色输入上下文
- **WHEN** 用户直接打开孤立 `BaseTreeAsset`
- **THEN** `Input` 素材区 MUST 显示缺少 `CharacterPipelineDefinition` 上下文
- **AND** 素材区 MUST NOT 提供手动选择 profile 的 fallback 字段

### Requirement: Input 素材区条目必须像 ExposedProperty 一样可拖拽生成节点
系统 MUST 让 `Input` 素材区中的 input value 和 action request 条目作为可拖拽 authoring item。用户将条目拖到当前 TreeView 时，系统 MUST 创建新的对应输入信息节点。节点创建 MUST 复用 BTSMTL 原生 node、property port 和 property edge 链路。

#### Scenario: 拖出 MoveAxis
- **WHEN** 用户把 `MoveAxis : Vector2` 从 `Input` 素材区拖到 Graph
- **THEN** 系统 MUST 创建新的 `CharacterInputVector2InfoNode`
- **AND** 节点 MUST 通过 `Vector2PropertyPort` 输出值
- **AND** 节点 MUST NOT 保存 `InputActionReference`

#### Scenario: 拖出 Attack request
- **WHEN** 用户把 `Attack` action request 从 `Input` 素材区拖到 TransitionRuleGraph 或普通 Graph
- **THEN** 系统 MUST 创建新的 `CharacterActionRequestInfoNode`
- **AND** 节点 MUST 通过 `BoolPropertyPort` 输出非消费查询结果

#### Scenario: 重复拖拽同一输入
- **WHEN** 当前 Graph 已存在同 id 且同类型的输入信息节点
- **THEN** 再次拖拽该输入 MUST 创建另一个绑定同一 id 且同类型的读取节点
- **AND** 两个节点 MUST 读取同一个 `CharacterInputProfile` 定义
- **AND** 系统 MUST NOT 创建第二份 InputAction 配置

### Requirement: Profile Inspector 不承担 Graph 节点创建入口
系统 MUST 将 `CharacterInputProfile` Inspector 限定为输入配置编辑和配置错误展示。Profile Inspector MUST NOT 暴露直接创建 BTSMTL 输入信息节点的拖拽条目。Graph 中输入信息节点的正式创建入口 MUST 是 Tree Inspector `Graph` 页内的 `Input` 素材区。

#### Scenario: 编辑输入配置
- **WHEN** 用户打开 `CharacterInputProfile` Inspector
- **THEN** 用户 MAY 编辑 input value 和 action request 配置
- **AND** Inspector MUST NOT 显示用于拖到 Graph 的节点创建 handle

#### Scenario: 创建 Graph 输入节点
- **WHEN** 用户需要在 Graph 中读取 `MoveAxis`
- **THEN** 用户 MUST 从 Tree Inspector `Graph` 页内的 `Input` 素材区拖出该输入项
- **AND** 系统 MUST NOT 要求用户回到 Profile Inspector 创建节点

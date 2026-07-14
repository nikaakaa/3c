# character-input-node-authoring Specification

## Purpose
定义角色输入语义进入 BTSMTL 图的 authoring 链路：`CharacterInputProfile` 中的 input value 和 action request 可以创建正式信息节点，Graph 读取 input value 和 request buffer，不依赖 InputAction 显示名、场景搜索或输入专用 Graph。
## Requirements
### Requirement: 输入配置必须能生成 BTSMTL 输入信息节点
系统 MUST 允许 `CharacterInputProfile` 中的 input value/action request 定义作为 BTSMTL 图内输入来源。编辑器 MAY 提供拖拽创建或同步刷新，但创建结果 MUST 是正式 BTSMTL 信息节点或模块，MUST NOT 创建 Input 专用 Graph、Workbench 路径或 object fallback 节点。

#### Scenario: 拖入 MoveAxis input value
- **WHEN** 用户把 `MoveAxis` input value 定义拖入 BTSMTL 图
- **THEN** 编辑器 MUST 创建 Vector2 input value 信息节点
- **AND** 节点 MUST 通过现有 `Vector2PropertyPort` 输出值
- **AND** UI MUST NOT 将该节点命名为 signal 或 command

#### Scenario: 拖入 Attack action request
- **WHEN** 用户把 `Attack` request 定义拖入 ConditionRuleGraph
- **THEN** 编辑器 MUST 创建 action request 信息节点或 request 查询节点
- **AND** 节点 MUST 通过现有 `BoolPropertyPort` 输出查询结果

### Requirement: Input value 信息节点读取 CharacterInputFrame
系统 MUST 使用 input value 信息节点读取 `CharacterInputFrame` 中的 typed input value。节点 MUST 保存输入定义稳定身份或引用和期望值类型，MUST NOT 直接保存或解析 Unity InputAction 名称作为 gameplay 语义，也 MUST NOT 把 input value 暴露为 continuous command。

#### Scenario: Vector2 input value 信息节点
- **WHEN** `MoveAxis` input value 信息节点被请求输出值
- **THEN** 节点 MUST 从 graph context 当前 `CharacterInputFrame` 读取 `MoveAxis`
- **AND** 读取失败时 MUST 输出 Vector2 默认值并报告缺失来源

#### Scenario: Bool held input value 信息节点
- **WHEN** `SprintHeld` input value 信息节点被请求输出值
- **THEN** 节点 MUST 从 graph context 当前 `CharacterInputFrame` 读取 bool 值
- **AND** 多次读取 MUST NOT 消费或改变该输入

### Requirement: Request 查询节点在规则图中保持纯求值
系统 MUST 在 ConditionRuleGraph 中仅允许 request 查询节点执行非消费查询。ConditionRuleGraph MUST NOT 消费 request、写入 request buffer 或改变输入历史。

#### Scenario: 查询 Attack 预输入
- **WHEN** ConditionRuleGraph 中的 `Has Attack Request` 节点被求值
- **THEN** 节点 MUST 查询 request buffer 中未过期且未消费的 `Attack`
- **AND** 节点 MUST NOT 将该 request 标记为 consumed

#### Scenario: 多条 Transition 查询同一 request
- **WHEN** 同一帧多条 Transition 规则图查询 `Dodge`
- **THEN** 每条规则图 MUST 看到一致的非消费查询结果
- **AND** 最终消费 MUST 留给状态行为或动作管线接受点

### Requirement: Request 消费必须发生在行为或动作接受点
系统 MUST 将 request 消费表达为状态行为、动作管线或后续正式 action accept 点的职责。消费能力 MUST NOT 出现在 ConditionRuleGraph 的纯条件节点范围中。

#### Scenario: 状态接受 Attack
- **WHEN** 状态行为或动作管线决定进入 Attack
- **THEN** 它 MAY 通过正式 request buffer API 消费 `Attack` request
- **AND** 该消费 MUST 写入 pipeline 输出或 debug context，以便后续网络确认和调试

#### Scenario: 规则图拒绝消费节点
- **WHEN** 用户尝试在 ConditionRuleGraph 中创建 request consume 节点
- **THEN** 图类型规则 MUST 拒绝该节点
- **AND** 非法节点 MUST NOT 进入正式节点集合

### Requirement: InputAction raw 节点保留但不是 gameplay 输入主链路
系统 MUST 保留现有 InputAction ValueNode 作为 raw 输入读取、调试和简单条件来源。系统 MUST NOT 将 raw InputAction 节点扩展为 request buffer、预输入或网络 command 的主实现。

#### Scenario: raw 输入调试
- **WHEN** 用户把 InputActionReference 拖入 BTSMTL 图
- **THEN** 系统 MAY 按现有规则创建 InputAction ValueNode
- **AND** 该节点 MUST 继续通过 `IInputActionValueSource` 读取 raw typed value

#### Scenario: gameplay request 使用 action request 信息节点
- **WHEN** 用户需要用 `Attack` 预输入驱动 Transition
- **THEN** 用户 SHOULD 使用 `CharacterInputProfile` 的 `Attack` action request 信息节点
- **AND** 系统 MUST NOT 要求 ConditionRuleGraph 直接依赖 Unity InputAction 名称

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

### Requirement: BTSMTL 输入 authoring 不得暴露 ClientCommand

BTSMTL 输入 authoring MUST 只创建和读取 CharacterInputFrame values、action requests 与 request buffer。它 MUST NOT 创建、读取、保存或显示 ClientCommandFrame、MotionCommand、Rollback input bundle、model packet 或 endpoint。Model command preview MUST 只存在于对应 model profile/Runtime Debug，不得进入 Graph Data Catalog 的输入节点列表。

#### Scenario: 创建 MoveAxis 节点

- **WHEN** 作者从输入配置创建 MoveAxis ValueNode
- **THEN** 节点 MUST 读取 CharacterInputFrame
- **AND** MUST 不提供 ServerAuthoritative MotionCommand 节点

#### Scenario: 查看模型 packet preview

- **WHEN** 作者需要查看 resolved motion 如何映射为 MotionCommand
- **THEN** MUST 在 ServerAuthoritative model Inspector/Debug 查看
- **AND** BTSMTL 输入 authoring MUST 不显示该 packet

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


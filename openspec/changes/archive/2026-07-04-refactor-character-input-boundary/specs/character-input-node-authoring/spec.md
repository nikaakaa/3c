# character-input-node-authoring Specification Delta

## MODIFIED Requirements

### Requirement: 输入配置必须能生成 BTSMTL 输入信息节点
系统 MUST 允许 BTSMTL 图从 `CharacterInputProfile` 或 InputSystem 资产生成对应输入信息节点。输入信息节点 MUST 保存输入定义的稳定身份或引用，并从 `CharacterInputFrame` 或 request buffer 读取运行时值。节点 MUST NOT 保存第二份 InputAction 绑定配置，也 MUST NOT 创建 Input 专用 Graph、Workbench 路径、object fallback 节点或 network command 节点。

#### Scenario: 拖入 MoveAxis input value
- **WHEN** 用户把 `MoveAxis` input value 定义拖入 BTSMTL 图或 TransitionRuleGraph
- **THEN** 编辑器 MUST 创建对应 `MoveAxis` 输入信息节点
- **AND** 节点 MUST 通过现有 `Vector2PropertyPort` 输出值
- **AND** UI MUST NOT 将该节点命名为 signal 或 command

#### Scenario: 拖入 Attack action request
- **WHEN** 用户把 `Attack` action request 定义拖入 TransitionRuleGraph
- **THEN** 编辑器 MUST 创建对应 `Attack` request 信息节点
- **AND** 节点 MUST 通过现有 `BoolPropertyPort` 输出查询结果

### Requirement: Input value 信息节点读取 CharacterInputFrame
系统 MUST 使用 input value 信息节点读取 `CharacterInputFrame` 中的 typed input value。节点 MUST 保存输入定义稳定身份或引用和期望值类型，MUST NOT 直接保存或解析 Unity InputAction 名称作为 gameplay 语义，也 MUST NOT 把 input value 暴露为 continuous command。

#### Scenario: Vector2 input value 节点
- **WHEN** `MoveAxis` input value 节点被请求输出值
- **THEN** 节点 MUST 从 graph context 当前 `CharacterInputFrame` 读取 `MoveAxis`
- **AND** 读取失败时 MUST 输出 Vector2 默认值并报告缺失来源

#### Scenario: Bool held input value 节点
- **WHEN** `SprintHeld` input value 节点被请求输出值
- **THEN** 节点 MUST 从 graph context 当前 `CharacterInputFrame` 读取 bool 值
- **AND** 多次读取 MUST NOT 消费或改变该输入

### Requirement: InputAction raw 节点保留但不是 gameplay 输入主链路
系统 MUST 保留现有 InputAction ValueNode 作为 raw 输入读取、调试和简单条件来源。系统 MUST NOT 将 raw InputAction 节点扩展为 action request buffer、预输入、network command 或 motion 模块主实现。

#### Scenario: raw 输入调试
- **WHEN** 用户把 InputActionReference 拖入 BTSMTL 图
- **THEN** 系统 MAY 按现有规则创建 InputAction ValueNode
- **AND** 该节点 MUST 继续通过 `IInputActionValueSource` 读取 raw typed value

#### Scenario: gameplay request 使用 action request 节点
- **WHEN** 用户需要用 `Attack` 预输入驱动 Transition
- **THEN** 用户 SHOULD 使用 `CharacterInputProfile` 的 `Attack` action request 节点
- **AND** 系统 MUST NOT 要求 TransitionRuleGraph 直接依赖 Unity InputAction 名称

## MODIFIED Requirements

### Requirement: 输入信息节点必须由输入配置创建并绑定
系统 MUST 将 input value 和 action request 配置作为输入信息节点的数据源。编辑器 MUST 提供从 InputProfile/InputSystem 同步输入信息节点的正式路径。同步 MUST 使用稳定身份更新已有节点，MUST NOT 依赖显示名匹配或生成重复节点。

#### Scenario: Profile 新增 MoveAxis
- **WHEN** 作者在 `CharacterInputProfile` 中新增 `MoveAxis`
- **THEN** 作者执行同步或拖拽该输入项时 MUST 能生成对应 `MoveAxis` 输入信息节点
- **AND** 该节点 MUST 引用输入定义稳定身份

#### Scenario: Profile 删除输入值
- **WHEN** 作者删除某个 input value 定义
- **THEN** 引用该 id 的节点 MUST 报告配置错误
- **AND** 系统 MUST NOT 自动创建 fallback 输入节点或改读其它输入值

#### Scenario: InputAction 重命名
- **WHEN** `Player/Move` 的显示名发生变化但 action identity 未变化
- **THEN** 对应输入信息节点 MUST 继续绑定同一输入定义
- **AND** Graph 连接 MUST NOT 因显示名变化断开


### Requirement: BTSMTL 输入 authoring 不得暴露 ClientCommand
系统 MUST 将 `ClientCommand`、`ClientCommandFrame` 和 network command 保留在 SyncFacts/Network 层。BTSMTL 输入 authoring MUST NOT 创建、读取、保存或显示 network command 节点作为 gameplay 输入来源。

#### Scenario: 作者创建移动图节点
- **WHEN** 作者需要让 Locomotion 读取移动输入
- **THEN** 作者 MUST 选择 input value，例如 `MoveAxis`
- **AND** 作者 MUST NOT 选择 `ClientCommand` 或 network command 数据作为节点输入

#### Scenario: 网络调试查看输入
- **WHEN** Runtime Debug 展示本 tick 发送的输入网络事实
- **THEN** 它 MAY 显示 `ClientCommandFrame`
- **AND** 该显示 MUST 与 BTSMTL 节点 authoring 分区隔离

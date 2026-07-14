# character-input-node-authoring Specification

## Purpose
定义角色输入层产物进入 BTSMTL 图的创作链路：用户应能把 `CharacterInputProfile` 中的 semantic signal/request 用作图内值来源或条件来源。该能力复用 BTSMTL `ValueNode`、`PropertyPort`、TransitionRuleGraph 和字段访问器，不新增输入专用 Graph 或并行端口系统。

## ADDED Requirements

### Requirement: 输入层语义定义可进入 BTSMTL 图
系统 MUST 允许 `CharacterInputProfile` 中的 signal/request 定义作为 BTSMTL 图内输入来源。编辑器 MAY 提供拖拽创建，但创建结果 MUST 是正式 BTSMTL 节点或模块，MUST NOT 创建 Input 专用 Graph、Workbench 路径或 object fallback 节点。

#### Scenario: 拖入 Move signal
- **WHEN** 用户把 `Move` signal 定义拖入 BTSMTL 图或 TransitionRuleGraph
- **THEN** 编辑器 MUST 创建 Vector2 semantic input 节点
- **AND** 节点 MUST 通过现有 `Vector2PropertyPort` 输出值

#### Scenario: 拖入 Attack request
- **WHEN** 用户把 `Attack` request 定义拖入 TransitionRuleGraph
- **THEN** 编辑器 MUST 创建 request 查询节点
- **AND** 节点 MUST 通过现有 `BoolPropertyPort` 输出查询结果

### Requirement: Semantic input 节点读取 CharacterInputFrame
系统 MUST 使用 semantic input 节点读取 `CharacterInputFrame` 中的 continuous command。节点 MUST 保存 semantic id 和期望值类型，MUST NOT 直接保存或解析 Unity InputAction 名称作为 gameplay 语义。

#### Scenario: Vector2 signal 节点
- **WHEN** `Move` signal 节点被请求输出值
- **THEN** 节点 MUST 从 graph context 当前 `CharacterInputFrame` 读取 `Move`
- **AND** 读取失败时 MUST 输出 Vector2 默认值并报告缺失来源

#### Scenario: Bool held signal 节点
- **WHEN** `SprintHeld` signal 节点被请求输出值
- **THEN** 节点 MUST 从 graph context 当前 `CharacterInputFrame` 读取 bool 值
- **AND** 多次读取 MUST NOT 消费或改变该输入

### Requirement: Request 查询节点在规则图中保持纯求值
系统 MUST 在 TransitionRuleGraph 中仅允许 request 查询节点执行非消费查询。TransitionRuleGraph MUST NOT 消费 request、写入 request buffer 或改变输入历史。

#### Scenario: 查询 Attack 预输入
- **WHEN** TransitionRuleGraph 中的 `Has Attack Request` 节点被求值
- **THEN** 节点 MUST 查询 request buffer 中未过期且未消费的 `Attack`
- **AND** 节点 MUST NOT 将该 request 标记为 consumed

#### Scenario: 多条 Transition 查询同一 request
- **WHEN** 同一帧多条 Transition 规则图查询 `Dodge`
- **THEN** 每条规则图 MUST 看到一致的非消费查询结果
- **AND** 最终消费 MUST 留给状态行为或动作管线接受点

### Requirement: Request 消费必须发生在行为或动作接受点
系统 MUST 将 request 消费表达为状态行为、动作管线或后续正式 action accept 点的职责。消费能力 MUST NOT 出现在 TransitionRuleGraph 的纯条件节点范围中。

#### Scenario: 状态接受 Attack
- **WHEN** 状态行为或动作管线决定进入 Attack
- **THEN** 它 MAY 通过正式 request buffer API 消费 `Attack` request
- **AND** 该消费 MUST 写入 pipeline 输出或事实，以便后续网络确认和调试

#### Scenario: 规则图拒绝消费节点
- **WHEN** 用户尝试在 TransitionRuleGraph 中创建 request consume 节点
- **THEN** 图类型规则 MUST 拒绝该节点
- **AND** 非法节点 MUST NOT 进入正式节点集合

### Requirement: InputAction raw 节点保留但不是 semantic 主链路
系统 MUST 保留现有 InputAction ValueNode 作为 raw 输入读取、调试和简单条件来源。系统 MUST NOT 将 raw InputAction 节点扩展为 request buffer、预输入或网络 command 的主实现。

#### Scenario: raw 输入调试
- **WHEN** 用户把 InputActionReference 拖入 BTSMTL 图
- **THEN** 系统 MAY 按现有规则创建 InputAction ValueNode
- **AND** 该节点 MUST 继续通过 `IInputActionValueSource` 读取 raw typed value

#### Scenario: gameplay request 使用 semantic 节点
- **WHEN** 用户需要用 `Attack` 预输入驱动 Transition
- **THEN** 用户 SHOULD 使用 `CharacterInputProfile` 的 `Attack` request 节点
- **AND** 系统 MUST NOT 要求 TransitionRuleGraph 直接依赖 Unity InputAction 名称

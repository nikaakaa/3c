# btsmtl-componentized-node-authoring Specification

## Purpose
定义 BTSMTL 节点组合创作主链路：`BaseNode` 通过 `NodeModule` 扩展创作能力，节点字段和模块字段通过同一字段访问器暴露，属性端口继续使用 BTSMTL 原生 `PropertyPort` / `PropertyEdge`，不恢复 Workbench 或并行端口协议。
## Requirements
### Requirement: 节点字段和模块字段使用统一访问器
系统 MUST 通过 `NodeFieldAccessor` 暴露节点创作字段。访问器 MUST 同时覆盖直接声明在 `BaseNode` 上的字段和序列化 `NodeModule` 内部字段，并提供正式序列化路径。

#### Scenario: 节点字段可发现
- **WHEN** 节点直接声明创作字段
- **THEN** 字段访问器 MUST 暴露该字段
- **AND** 字段目标对象 MUST 是节点本身

#### Scenario: 模块字段可发现
- **WHEN** 节点拥有序列化 `NodeModule`
- **THEN** 字段访问器 MUST 暴露该模块的创作字段
- **AND** 字段目标对象 MUST 是模块本身
- **AND** 序列化路径 MUST 指向模块字段而不是假设字段在节点上

### Requirement: NodeModule 承载节点组合能力
系统 MUST 允许 `BaseNode` 通过序列化 `NodeModule` 组合 Graph 引用、Timeline 引用、InputAction 绑定、属性端口和创作元数据。新创作能力 SHOULD 优先通过模块组合表达，而不是新增平行节点继承分支。

#### Scenario: 模块贡献引用和字段
- **WHEN** 节点拥有 Graph、Timeline 或 InputAction 模块
- **THEN** 模块 MUST 能通过字段访问器贡献面板字段、端口或引用命令
- **AND** 节点视图 MUST 通过同一节点界面显示这些能力

### Requirement: PropertyPort 使用稳定身份
系统 MUST 使用稳定 `PortId` 作为属性端口连接身份。显示名、字段名和编辑器文案 MAY 改变，但 MUST NOT 破坏已有属性边。

#### Scenario: 同名模块字段
- **WHEN** 同一节点多个模块声明同名端口字段
- **THEN** 每个端口 MUST 拥有不同 `PortId`

#### Scenario: PropertyEdge 恢复连接
- **WHEN** Graph 重载属性边
- **THEN** `PropertyEdge` MUST 通过起点和终点端口 `PortId` 恢复连接
- **AND** 缺失端口 ID 的属性边 MUST 被视为非法并由清理路径移除

### Requirement: 编辑器 UI 使用字段访问器
系统 MUST 从字段访问器生成节点端口、节点面板和输入默认值字段。编辑器 UI MUST NOT 重新绕过访问器直接扫描节点字段作为另一套创作入口。

#### Scenario: 模块端口显示
- **WHEN** 模块贡献 `PropertyPort`
- **THEN** 节点视图 MUST 在标准 BTSMTL 端口容器中显示该端口
- **AND** 未连接的输入默认值 MUST 能通过正式序列化属性显示和编辑

### Requirement: Graph 嵌套是节点或模块语义
系统 MUST 把 Graph 引用、下钻命令、Graph 作用域、ownership 和循环验证视为节点、边或模块创作语义，而不是 `PropertyPort` 的职责。Graph reference MUST 支持 owner 内部 inline graph data 和显式 shared graph asset，默认私有图必须内联保存，不能保存为 owned embedded subasset。

#### Scenario: 打开子 Graph
- **WHEN** 节点、边或模块暴露 graph reference
- **THEN** BTSMTL MUST 从该 reference 解析 inline graph data 或 shared graph asset
- **AND** BTSMTL MUST NOT 通过属性端口连接推导下钻目标

#### Scenario: 默认私有 Graph 引用
- **WHEN** 节点、边或模块创建默认私有下钻 Graph
- **THEN** 该 Graph MUST 作为 owner 内部普通 C# inline graph data 自动绑定
- **AND** Graph 引用字段 MUST 是实现细节，不得成为默认创作流程中的必填手动拖拽步骤
- **AND** 系统 MUST NOT 创建 owned embedded subasset

#### Scenario: Shared Graph 引用
- **WHEN** 节点、边或模块引用 shared Graph asset
- **THEN** UI MUST 明确显示该引用是 shared
- **AND** 删除节点、边或模块时 MUST NOT 删除 shared Graph asset
- **AND** owner MUST NOT 同时保留 inline graph 真数据

#### Scenario: 循环引用
- **WHEN** Graph A 和 Graph B 通过 inline 或 shared graph reference 形成循环
- **THEN** 嵌套 Graph 校验 MUST 报告该循环非法

### Requirement: 不新增分裂创作路径
系统 MUST 保持 `BaseTree` / `BaseNode` / `NodeModule` / `PropertyPort` / `PropertyEdge` 为唯一 BTSMTL 节点创作主链路。系统 MUST NOT 新增 Workbench Tree、Workbench 端口描述符、并行类型注册表、并行边协议或旧 SO/config fallback。

#### Scenario: 新能力接入
- **WHEN** 新节点能力需要字段、端口或引用
- **THEN** 它 MUST 接入现有节点、模块和端口系统
- **AND** 它 MUST NOT 创建平行 Workbench 或 fallback 配置链路

### Requirement: Graph Reference 互斥持有 inline 和 shared
系统 MUST 让每个 graph reference 在 inline graph data 和 shared graph asset 之间保持互斥。互斥规则属于正式数据模型，不是 editor-only 显示状态。

#### Scenario: 使用 inline graph data
- **WHEN** graph reference 没有 shared asset
- **THEN** resolved graph MUST 来自 owner 内部 inline graph data
- **AND** UI MUST 显示该引用为 `Inline`

#### Scenario: 使用 shared graph asset
- **WHEN** graph reference 设置 shared asset
- **THEN** resolved graph MUST 来自 shared asset
- **AND** owner 的 inline graph data MUST 被清理
- **AND** UI MUST 显示该引用为 `Shared Asset`

#### Scenario: 非法双持有
- **WHEN** graph reference 同时存在 inline graph data 和 shared asset
- **THEN** 校验 MUST 报告非法结构
- **AND** 系统 MUST NOT 静默选择其中一个作为 fallback


# btsmtl-tree-inspector-information-architecture Specification

## Purpose

定义Tree Workspace中左侧Data、右侧Details、运行时观察和内部模块的正式信息架构。

## Requirements

### Requirement: Tree Workspace必须分离Data与Details区域

作者工作区 MUST 将唯一 Data Catalog 与当前选择的 Details 分开显示。Data Catalog 只投影声明、输入和引用的真实 owner；Details 只投影当前领域允许的业务字段和命令。两者 MUST 不复制可写状态。角色动画、Blend、播放生命周期和 Animancer 配置 MUST 由各自正式入口编辑，不得进入技能图 Details 的第二写入口。

#### Scenario: 选择Transition edge

- **WHEN** 作者选择技能 FSM 的 Transition
- **THEN** Details MUST 显示该领域的 priority、条件引用和 interruption
- **AND** Data Catalog MUST 保持当前上下文，不复制这些字段
- **AND** 技能转移 Details MUST 不显示 Pose blend、duration、curve、HandoffRole 或 producer binding 等表现配置

#### Scenario: 打开动画表现配置

- **WHEN** 作者要修改 Pose、Blend 或资源绑定
- **THEN** 工作区 MUST 导航到 Pose/Timeline 正式入口
- **AND** 技能 Details MUST 不写入同一数据的副本

#### Scenario: 打开角色RootTree

- **WHEN** 作者从角色业务定义打开其当前正式技能图
- **THEN** Navigator MUST 显示唯一 Data Catalog，Details MUST 显示图作者设置或明确空状态
- **AND** 两处 MUST 不显示动画播放生命周期字段，不为沿用历史场景标题创建旧 RootTree

### Requirement: Graph Authoring Settings 必须排除运行时生命周期字段

图级 Details MUST 只显示可保存的作者配置。Running、State、runtime handle、compiled index 和等价运行状态 MUST 不通过通用属性扫描或自定义面板成为可写作者字段。Live Debug 只能使用正式 source-mapped diagnostics 的只读投影。

#### Scenario: Authoring模式查看RunnableTree

- **WHEN** 作者在 Authoring 模式打开技能图
- **THEN** Details MUST 不显示运行状态字段
- **AND** 系统 MUST 不把运行状态写回作者资产

#### Scenario: Live Debug观察执行状态

- **WHEN** 作者在 Live Debug 选择有效实例
- **THEN** Details MUST 显示只读诊断和来源身份
- **AND** MUST 不直接修改节点或黑板数据

### Requirement: Data区域筛选必须在窄栏中保持source-aware

Data Catalog MUST 提供文本搜索和 All、Input、Blackboard 分类。Blackboard 的 context、scope、owner 和 inherited/local 状态 MUST 使用声明解析；Input/TargetData、Character State、Attributes/GameplayTags 和 Equipment Provider MUST 保持各自只读或专属写入语义。筛选、折叠和展开只属于 Editor view-state，MUST 不修改声明或运行地址。

#### Scenario: 只查看Input

- **WHEN** 作者选择 Input source
- **THEN** Catalog MUST 显示输入和目标数据
- **AND** MUST 不给它们伪造 Blackboard scope 或写入入口；Input 列表包含 Input Values 与 Action Requests，隐藏不适用的 Blackboard Context/Scope 控件

#### Scenario: 过滤当前图的Blackboard

- **WHEN** 作者选择 Blackboard source 与当前 State scope
- **THEN** Catalog MUST 只显示当前上下文可见的 Skill Local 声明
- **AND** MUST 按稳定 declaration identity 和 owner 区分同名变量

### Requirement: Graph Authoring Editor Shell 运行时模式必须保持窗口级边界

Authoring 与 Live Debug MUST 是整个编辑窗口的模式。Live Debug 下所有作者 mutation MUST 被拒绝；窗口只绑定自己的 runtime target 和 capture position，并从共享 diagnostics/provider 读取状态。导航、Timeline 播放和其它窗口的 binding MUST 不因 selection 或局部面板变化而改变。

#### Scenario: Live Debug同时查看Data与Details

- **WHEN** 作者在 Live Debug 同时查看 Data 和 Details
- **THEN** 两者 MUST 使用同一只读目标和 capture position
- **AND** 任一面板 MUST 不允许写 Graph、Blackboard、Input 或 runtime state
- **AND** Details 选择变化 MUST 不重置 Graph Follow/Pin binding；停止 Capture 后仍使用共享 capture position

#### Scenario: Graph与Timeline同时打开

- **WHEN** 作者同时打开图窗口和 Timeline Editor
- **THEN** 图窗口 MUST 只更新自己的 debug binding
- **AND** Timeline playback binding MUST 保持不变；观察同一 Session 时，两窗口 MUST 从同一正式 provider 和 Capture history position 获取各自结果

#### Scenario: 创建Graph Authoring Editor Shell

- **WHEN** Editor 创建当前正式原生图编辑入口及项目业务面板
- **THEN** 面板 MUST 使用当前 Unity 支持的样式；创建过程 MUST 不产生 stylesheet parser error
- **AND** MUST 不创建旧 TreeWindow 或恢复其视觉树，历史场景标题只用于匹配原有要求

#### Scenario: Play Mode domain reload后恢复当前Graph

- **WHEN** 正式原生图编辑入口经历 Play Mode domain reload
- **THEN** MUST 按已保存 serialized owner、property path 和 GraphAuthoringId 恢复作者对象，并建立新的窗口本地运行绑定
- **AND** 定位缺失或不一致时 MUST 停止恢复，不恢复旧运行实例或按名称、窗口顺序猜测对象

### Requirement: Graph Authoring Editor Shell内部职责必须由独立模块拥有

作者集成 MUST 将 navigation、Data Catalog、Details、mutation、selection 和 runtime overlay 分成独立职责。唯一 mutation owner MUST 负责创建、连接、删除、粘贴和 Undo；Data Catalog 与 Details MUST 不互相保存可写副本；runtime overlay MUST 只读取正式 diagnostics。系统 MUST 不依赖旧 TreeWindow、TreeView 或按名称近似恢复作为业务数据入口。

#### Scenario: 删除StateMachine Transition edge

- **WHEN** 作者删除带规则内容的 Transition
- **THEN** 当前领域 mutation owner MUST 在同一 Undo 边界更新连接、规则引用和 identity
- **AND** Details 或画布 MUST 不再执行第二次写入

#### Scenario: Play Mode domain reload

- **WHEN** Editor reload 后恢复当前作者页面
- **THEN** navigation MUST 使用 serialized owner、property path 和稳定 Graph identity
- **AND** 缺少或不匹配时 MUST 停止恢复，不按名称选择其它图
- **AND** runtime overlay MUST 重建窗口本地绑定，不恢复旧运行实例；Data Catalog 自己保存筛选和折叠状态，Details 选择不改变作者定位或运行绑定

#### Scenario: Data与Details同时可见

- **WHEN** 作者在原生图画布选择节点或连接
- **THEN** Data Catalog MUST 独立保留筛选和折叠状态，Details MUST 根据当前选择显示真实 owner 的字段
- **AND** 选择变化 MUST 不修改作者定位与运行绑定

#### Scenario: Graph与Timeline同时Live Debug

- **WHEN** 原生图编辑入口与 Timeline Editor 观察同一 Session
- **THEN** 各自 overlay MUST 只修改窗口本地 binding，并共享正式 provider 与 Capture history position
- **AND** 图导航和选择 MUST 不改变 Timeline 播放绑定

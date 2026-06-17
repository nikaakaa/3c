## ADDED Requirements

### Requirement: Editor Adapter 读写项目 Definition
系统 MUST 提供 Editor-only adapter，使节点图和 timeline 编辑器读写本项目自己的 behavior tree、action selection 和 timeline definition。Editor adapter MUST NOT 让正式 runtime 依赖 Taco `BaseTree`、`RunnableTree`、`TimelinePlayer`、GraphView node view 或 Editor 类型。

#### Scenario: GraphView 保存项目资产
- **WHEN** 设计者在行为图编辑器中新建节点和连接
- **THEN** 编辑器 MUST 保存到本项目 authoring definition
- **AND** MUST NOT 保存为 Taco runtime tree 作为正式 gameplay 输入

#### Scenario: Runtime 不引用 Editor 类型
- **WHEN** 检查 runtime assembly 或源码
- **THEN** runtime MUST NOT 引用 UnityEditor、GraphView、Taco editor view 或 Ref runtime runner 类型

### Requirement: Ref Editor UI 可移植
Ref/wly970123 的 GraphView、TreeDesigner 和 Timeline Editor UI MAY 被复制或移植到本项目 Editor-only assembly。移植范围 MAY 包含 window、view、manipulator、UXML、USS、图标、节点/端口/连线视图、Timeline track/clip 视图和 inspector 交互。移植后的 UI MUST 通过 adapter、serializer 或 compiler 读写本项目 definition，MUST NOT 直接保存 Taco runtime tree 作为正式 gameplay 输入。

#### Scenario: 移植 Graph UI
- **WHEN** 实现行为图编辑器窗口
- **THEN** 实现 MAY 复用 Ref 的 GraphView window、node view、port view、edge view 和交互资源
- **AND** 保存结果 MUST 写入本项目 authoring definition
- **AND** MUST NOT 让 runtime 消费 Ref `BaseTree` 或 `RunnableTree`

#### Scenario: 移植 Timeline UI
- **WHEN** 实现 action timeline 编辑器窗口
- **THEN** 实现 MAY 复用 Ref 的 timeline window、track view、clip view、frame ruler、UXML、USS 和图标资源
- **AND** 保存结果 MUST 写入本项目 timeline authoring data
- **AND** MUST NOT 让正式 gameplay 通过 Ref `TimelinePlayer` 或 PlayableGraph 执行动作

### Requirement: Timeline Editor 编辑 Runtime Timeline 数据
Timeline editor MUST 编辑本项目 `ActionTimelineDefinition` 或批准的 authoring equivalent，并能生成 runtime 可消费的 track / clip / payload 数据。Timeline editor MUST NOT 直接驱动 Animator、PlayableGraph、Particle、Audio、Cinemachine、Transform 或 scene object。

#### Scenario: 编辑 Clip 生成 Definition
- **WHEN** 设计者在 timeline editor 中创建 AnimationKey、Motion、HitboxWindow、CancelWindow 或 Cue clip
- **THEN** editor MUST 将其保存为本项目 timeline authoring 数据
- **AND** compiler MUST 能生成 runtime `ActionTimelineDefinition` 或等价数据

#### Scenario: Timeline Editor 不播放正式表现
- **WHEN** 设计者编辑 cue clip
- **THEN** editor MAY 显示预览信息
- **AND** MUST NOT 让正式 gameplay 通过 editor timeline 直接播放 VFX、SFX 或 camera shake

### Requirement: Compiler 连接 Authoring 与 Runtime
系统 MUST 提供 compiler，将 editor authoring graph 编译为 `CharacterBehaviorExecutionTree`、Action selection nodes、Action timelines 或批准的等价 runtime model。正式 gameplay MUST 只消费 compiler 输出，MUST NOT 直接运行 editor graph object。

#### Scenario: 编译有效图
- **GIVEN** authoring graph 包含 root、parallel、locomotion leaf 和 committed action leaf
- **WHEN** compiler 运行
- **THEN** 它 MUST 输出 runtime execution tree
- **AND** 输出 MUST 保留稳定 node id 和 child 顺序

#### Scenario: 非法图拒绝编译
- **GIVEN** authoring graph 存在循环、缺失 root、端口不兼容或共享 runtime node
- **WHEN** compiler 运行
- **THEN** compiler MUST 报告明确错误
- **AND** MUST NOT 生成可被正式 runtime 消费的半成品

### Requirement: Ref Importer 只能 Editor-only
Ref/wly970123 的节点树、timeline、track、clip 或 UI 代码 MAY 作为 Editor-only importer / adapter 的移植来源、参考或输入来源。正式 runtime MUST NOT 直接依赖 Ref 的 `TreeRunner.Update`、`RunnableTree`、`RunnableNode`、`TimelinePlayer.FixedUpdate`、PlayableGraph、Animator 驱动或直接 Unity 对象副作用。

#### Scenario: Ref Timeline 转换为项目 Timeline
- **WHEN** importer 读取 Ref timeline 示例资产
- **THEN** 它 MAY 生成本项目 timeline authoring data
- **AND** runtime MUST 只消费转换后的本项目 definition

#### Scenario: Ref Runner 不进入 Runtime
- **WHEN** 检查正式 runtime 源码
- **THEN** 静态验证 MUST 确认没有引用 Ref `TreeRunner`、`RunnableTree`、`TimelinePlayer` 或相关 runtime runner

### Requirement: Editor Asset Versioning
行为图和 timeline authoring asset MUST 提供 schema version、stable id 或批准的等价迁移标记，使 node、port、track、clip 字段后续演进可检测、可迁移、可测试。

#### Scenario: 资产版本缺失时报错
- **GIVEN** editor authoring asset 缺失 schema version 或 stable id
- **WHEN** compiler 或 validator 运行
- **THEN** 系统 MUST 报告明确错误
- **AND** MUST NOT 静默生成 runtime tree

### Requirement: Editor Adapters 可测试和可验证
系统 MUST 提供自动测试和静态边界验证，证明 editor adapters 只存在于 Editor-only 边界，且 compiler 输出能够进入正式 runtime 数据结构。

#### Scenario: 自动测试覆盖编译
- **WHEN** 运行相关 Editor / compiler EditMode 测试
- **THEN** 测试 MUST 覆盖 graph 编译、timeline 编译、非法图报错和 Dodge 示例资产编译

#### Scenario: 静态边界验证
- **WHEN** 检查 runtime 源码和 asmdef
- **THEN** 静态测试 MUST 确认 runtime 不引用 UnityEditor、GraphView、Taco runtime runner、PlayableGraph 或 scene object binding

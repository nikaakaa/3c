# character-behavior-editor-adapters Specification

## Purpose
定义 Character Behavior Editor 与 Committed Action Timeline Editor 的 Editor-only adapter 边界，确保编辑器读写正式配置并且不把 Ref/Taco runtime 带入正式 gameplay。
## Requirements
### Requirement: Editor Adapter 读写项目 Definition
系统 MUST 提供 Editor-only adapter，使 Character Behavior Editor 读写本项目自己的 source topology definition，并使 Committed Action Timeline Editor 读写正式 action definition 中的 selector / timeline authoring 数据。Editor adapter MUST NOT 让正式 runtime 依赖 Taco `BaseTree`、`RunnableTree`、`TimelinePlayer`、GraphView node view 或 Editor 类型。Character Behavior Editor MUST NOT 复制、保存或持有 Dodge selector、Directional timeline、Backstep timeline、track、clip、motion payload、animation key、window 或 cue。

#### Scenario: GraphView 保存项目 Source 资产
- **WHEN** 设计者在行为图编辑器中新建 source 节点和连接
- **THEN** 编辑器 MUST 保存到本项目 behavior source authoring definition
- **AND** 保存内容 MUST 限定为 root、composite、Locomotion leaf、CommittedAction leaf、edge 和 editor position
- **AND** MUST NOT 保存为 Taco runtime tree 作为正式 gameplay 输入
- **AND** MUST NOT 在 graph asset 中创建 Dodge timeline 数据

#### Scenario: Timeline Editor 保存正式 ActionDefinition
- **WHEN** 设计者在 Committed Action Timeline Editor 中编辑 Directional 或 Backstep clip
- **THEN** 编辑器 MUST 写回正式 `CharacterActionDefinitionSO` 或批准的等价 action definition authoring
- **AND** Graph Editor MAY 打开或定位该 action definition
- **AND** Graph Editor MUST NOT 复制该 timeline 数据

#### Scenario: Runtime 不引用 Editor 类型
- **WHEN** 检查 runtime assembly 或源码
- **THEN** runtime MUST NOT 引用 UnityEditor、GraphView、Taco editor view 或 Ref runtime runner 类型

### Requirement: 固定帧管线不进入编辑器
编辑器 MUST NOT 编辑 `CharacterFramePipeline` phase 顺序、output apply 顺序、motion executor、animation presenter、input consume 或 blackboard write 的权威边界。`CharacterFramePipeline` MUST 继续作为代码和架构合同中的固定角色帧管线消费 compiler 输出。

#### Scenario: 不编辑 Pipeline phase
- **WHEN** 设计者打开行为图或 timeline 编辑器
- **THEN** 编辑器 MUST NOT 暴露 RequestSubmission、FrameSubmission、Plan / BodyArbiter、OutputApplier 或 Snapshot / Events phase 的重排入口
- **AND** compiler 输出 MUST 进入固定 `CharacterFramePipeline`

#### Scenario: 旧 Chain 不作为 authoring graph
- **WHEN** 检查 editor authoring asset、compiler 输入和示例资产
- **THEN** 它们 MUST NOT 将旧 `CharacterFrameSubmitterChain` 或 `CharacterFrameSubmitterGraph` 保存为正式 behavior graph
- **AND** 旧 chain MAY 只作为迁移 baseline 或测试对照存在

### Requirement: Locomotion Leaf 只提交候选输出
Locomotion MAY 作为 `CharacterBehaviorGraphDefinition` 中的 leaf / behavior source 被编辑和编译。运行时 Locomotion leaf MUST 只提交 movement facts、state frame、motion candidate、animation candidate、facing / gait / run latch candidate 或 diagnostics。Locomotion leaf MUST NOT 直接执行 movement、播放 animation、写 runtime blackboard 或消费 input apply 结果。

#### Scenario: Locomotion 在图中可见
- **WHEN** authoring graph 包含 Locomotion leaf
- **THEN** compiler MUST 将其编译为 runtime Locomotion behavior leaf 或批准的等价 source
- **AND** 该 leaf MUST 通过 behavior submission 进入统一 frame plan

#### Scenario: Locomotion 不执行副作用
- **WHEN** runtime Locomotion leaf 产出 motion 或 animation candidate
- **THEN** candidate MUST 等待 `CharacterFramePlan` 或等价计划采用
- **AND** 最终 movement MUST 由 output applier 调用正式 motion executor
- **AND** 最终 animation MUST 由 output applier 调用正式 Animancer presenter

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
Timeline editor MUST 编辑本项目正式 action definition 内的 `ActionTimelineDefinition` authoring equivalent，并能通过 action definition compiler 生成 runtime 可消费的 track / clip / payload 数据。Timeline editor MUST 以 `CharacterActionDefinitionSO` 或批准的等价 action definition 为默认编辑入口；MUST NOT 默认加载 `Behavior/Samples` authoring asset，也 MUST NOT 生成 sample-only runtime definition 作为正式 gameplay 输入。Timeline editor MUST NOT 直接驱动 Animator、PlayableGraph、Particle、Audio、Cinemachine、Transform 或 scene object。

#### Scenario: 编辑 Clip 写回 ActionDefinition
- **WHEN** 设计者在 timeline editor 中创建 AnimationKey、Motion、HitboxWindow、CancelWindow 或 Cue clip
- **THEN** editor MUST 将其保存为所选 action definition 的 timeline authoring 数据
- **AND** action definition compiler MUST 能生成 runtime `ActionTimelineDefinition` 或等价数据
- **AND** Behavior Graph compiler MUST NOT 参与该 timeline payload 编译

#### Scenario: Timeline Editor 不播放正式表现
- **WHEN** 设计者编辑 cue clip
- **THEN** editor MAY 显示预览信息
- **AND** MUST NOT 让正式 gameplay 通过 editor timeline 直接播放 VFX、SFX 或 camera shake

#### Scenario: Sample Asset 不作为默认入口
- **WHEN** 设计者打开 Committed Action Timeline Editor
- **THEN** 编辑器 MUST 默认定位正式 Dodge action definition 或用户选择的正式 action definition
- **AND** MUST NOT 默认加载 `Behavior/Samples` 下的 sample authoring asset
- **AND** MUST NOT 创建 sample-only runtime definition 作为正式 gameplay 输入

### Requirement: Compiler 连接 Authoring 与 Runtime
系统 MUST 提供职责拆分的 compiler，将 behavior source authoring graph 编译为 `CharacterBehaviorRuntimeDefinition`、source execution tree 或批准的等价 source runtime model；并将正式 action definition 编译为 Action selection nodes、CommittedActionBranchDefinition、Action timelines 或批准的等价 runtime model。正式 gameplay MUST 只消费 compiler 输出，MUST NOT 直接运行 editor graph object。Behavior compiler MUST NOT 编译 Action selector、ActionTimeline track / clip / payload；Action definition compiler MUST NOT 创建 behavior root、parallel source node 或 Locomotion leaf。

#### Scenario: Behavior Compiler 编译有效 Source 图
- **GIVEN** authoring graph 包含 root、parallel、locomotion leaf 和 committed action leaf
- **WHEN** behavior compiler 运行
- **THEN** 它 MUST 输出 source runtime definition 或 source execution tree
- **AND** 输出 MUST 保留稳定 node id 和 child 顺序
- **AND** 输出 MUST NOT 包含 Dodge selector、TimelineNode、ActionTimeline track、clip 或 payload

#### Scenario: Action Definition Compiler 编译 Action Timeline
- **GIVEN** 正式 Dodge action definition 包含 selector、Directional timeline 和 Backstep timeline
- **WHEN** action definition compiler 或 validator 运行
- **THEN** 它 MUST 输出或验证 Action selection nodes、CommittedActionBranchDefinition 和 ActionTimelineDefinition
- **AND** 它 MUST NOT 创建 behavior root、parallel source node 或 Locomotion leaf

#### Scenario: 非法图拒绝编译
- **GIVEN** authoring graph 存在循环、缺失 root、端口不兼容或共享 runtime node
- **WHEN** behavior compiler 运行
- **THEN** compiler MUST 报告明确错误
- **AND** MUST NOT 生成可被正式 runtime 消费的半成品

#### Scenario: 非法 ActionDefinition 拒绝编译
- **GIVEN** action definition 缺少 Dodge selector、Directional timeline 或 Backstep timeline
- **WHEN** action definition compiler 或 validator 运行
- **THEN** compiler 或 validator MUST 报告明确错误
- **AND** MUST NOT 从 Behavior Graph、Resources、sample asset、legacy embedded branch 或代码默认值补齐

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

### Requirement: 通用技能编辑器声明门槛
本变更 MUST 将交付物描述为 Character Behavior Editor、Committed Action Timeline Editor 或批准的等价角色行为编辑器地基。系统 MUST NOT 在只有 Dodge selector + timeline 示例时，将本阶段宣称为通用 Skill Editor。通用技能编辑器声明 MUST 等到 Block / PerfectBlock、Attack / HitResolve 或等价交互型能力形成第二条金线并补齐对应 runtime 合同后再提出。

#### Scenario: Dodge 示例不代表通用技能编辑器
- **WHEN** 编辑器只覆盖 Dodge selector、Directional / Backstep timeline 和正式 Dodge ActionDefinition 配置
- **THEN** 文档、菜单、窗口标题和测试命名 MUST NOT 将本阶段称为通用 Skill Editor
- **AND** MUST 将其描述为角色行为或 committed action timeline 编辑地基

#### Scenario: 后续交互型能力作为通用性证明
- **WHEN** 后续 proposal 准备把工具升级为通用角色技能编辑器
- **THEN** 该 proposal MUST 至少提供 Block / PerfectBlock、Attack / HitResolve 或等价交互型能力金线
- **AND** MUST 覆盖 incoming hit / contact fact、window fact、hit 或 defense resolve ownership、双方结果或反击请求、cue 和 rollback restore 边界

### Requirement: Editor Adapters 可测试和可验证
系统 MUST 提供自动测试和静态边界验证，证明 editor adapters 只存在于 Editor-only 边界，且 compiler 输出能够进入正式 runtime 数据结构。

#### Scenario: 自动测试覆盖编译
- **WHEN** 运行相关 Editor / compiler EditMode 测试
- **THEN** 测试 MUST 覆盖 graph 边界、timeline 编译、正式 Dodge ActionDefinition 验证和通用 Skill Editor 命名边界

#### Scenario: Timeline Editor 默认编辑正式配置
- **WHEN** 设计者打开 Committed Action Timeline Editor
- **THEN** 编辑器 MUST 默认加载正式 `CorinDodgeActionDefinition.asset` 或用户选择的正式 `CharacterActionDefinitionSO`
- **AND** MUST NOT 默认加载 `Behavior/Samples` 下的 sample authoring asset
- **AND** MUST NOT 生成 sample-only runtime definition 作为正式 gameplay 输入

#### Scenario: 静态边界验证
- **WHEN** 检查 runtime 源码和 asmdef
- **THEN** 静态测试 MUST 确认 runtime 不引用 UnityEditor、GraphView、Taco runtime runner、PlayableGraph 或 scene object binding
- **AND** MUST 确认 editor authoring 不暴露 `CharacterFramePipeline` phase 顺序编辑入口
- **AND** MUST 确认 Locomotion editor leaf 不直接调用 motion executor、animation presenter 或 blackboard writer

### Requirement: Ref Timeline UI 迁移必须兼容 Unity 2022
Committed Action Timeline Editor 的 Ref UI 迁移 MUST 以 Unity 2022.3 可导入、可重载、可测试为前置条件。迁移资源 MAY 参考 `Ref/wly970123` 的 UXML、USS 和图标，但 MUST NOT 直接复制 Ref `.meta`，MUST NOT 保留指向 Ref 项目路径的 `project://database/Assets/Addon/Taco` 样式引用，且 MUST 在绑定代码前确认 Unity Editor 能安全导入资源。

#### Scenario: UXML 和 USS 逐个兼容导入
- **WHEN** 迁移 Ref Timeline UI 资源
- **THEN** 每个 UXML / USS MUST 先转换为 Unity 2022 兼容格式
- **AND** Unity MUST 负责生成本项目 `.meta`
- **AND** 导入失败 MUST 阻止后续代码绑定

#### Scenario: 禁止复制 Ref meta 和项目路径引用
- **WHEN** 运行 editor resource 静态检查
- **THEN** 检查 MUST 发现并拒绝直接复制的 Ref `.meta` 风险
- **AND** MUST 拒绝 `project://database/Assets/Addon/Taco` 样式引用进入本项目迁移资源

### Requirement: Timeline Editor 必须通过 Editor Timeline Model 操作正式 ActionDefinition
Committed Action Timeline Editor MUST 通过 Editor-only timeline model 操作正式 `CharacterActionDefinitionSO` 内的 Committed Action branch authoring。UI MAY 展示 Field、Track、Clip 和 Inspector 组件，但 MUST 通过 Branch Editor 选中的 TimelineNode serialized adapter 写回该 TimelineNode 的 `CommittedActionBranchTimelineAuthoring`、`ActionTimelineTrackAuthoring` 和 `ActionTimelineClipAuthoring`。Timeline Editor MUST NOT 将 `DodgeCommittedActionBranchAuthoring` 或独立 Directional / Backstep 字段作为正式保存目标。

#### Scenario: Model 从正式 ActionDefinition 建立快照
- **GIVEN** 正式 Dodge `CharacterActionDefinitionSO` 包含通用 Committed Action branch authoring、Directional TimelineNode 和 Backstep TimelineNode
- **WHEN** 打开 Committed Action Timeline Editor
- **THEN** editor timeline model MUST 从选中的 TimelineNode 建立 timeline 快照
- **AND** 快照 MUST 包含 timeline duration、track、clip、payload、validation state 和 selection 所需身份

#### Scenario: Model transaction 写回正式 serialized data
- **WHEN** 设计者添加、删除、移动、缩放 track 或 clip
- **THEN** 修改 MUST 通过 model transaction 写回选中 TimelineNode 的 Unity serialized data
- **AND** 保存后 `CharacterActionDefinitionSO.ToDefinition()` MUST 看到同一份修改
- **AND** Behavior Graph compiler MUST NOT 参与 timeline payload 写回

#### Scenario: Selection 不依赖易碎数组 index
- **WHEN** 设计者删除或重排 track / clip
- **THEN** editor selection MUST 通过 stable id 或批准的等价身份保持正确
- **AND** MUST NOT 因数组 index 变化选中错误 payload

### Requirement: Timeline Field Track Clip 交互必须按 Ref 组件落地
迁移后的 Committed Action Timeline Editor MUST 按 Ref Timeline 的组件职责提供 field、track、clip 和 inspector 交互。实现 MAY 重写类名和 adapter，但 MUST 保留本阶段要求的用户可见交互能力。

#### Scenario: Field View 提供时间轴交互
- **WHEN** 设计者编辑 Directional 或 Backstep timeline
- **THEN** Field View MUST 提供 seconds ruler、tick grid、locator click / drag、scroll、zoom、中键 pan、F 定位和 rectangle selector
- **AND** timeline position 到 seconds authoring / local tick preview 的映射 MUST 使用稳定 position map 或批准的等价结构

#### Scenario: Track View 提供轨道编辑
- **WHEN** 设计者编辑 timeline track
- **THEN** Track View MUST 支持 track selection、add、delete、reorder 和 empty track 展示
- **AND** track kind MUST 来自正式 `ActionTimelineTrackKind`
- **AND** 非法 track / clip kind 组合 MUST 被拒绝或报告 validator 错误

#### Scenario: Clip View 提供片段编辑
- **WHEN** 设计者编辑 timeline clip
- **THEN** Clip View MUST 支持 clip selection、多选、add、delete、move、left resize、right resize 和 invalid 视觉状态
- **AND** clip kind MUST 来自正式 `ActionTimelineClipKind`
- **AND** 运行时不支持的 ease-in / ease-out 语义 MUST NOT 作为假编辑能力展示

#### Scenario: Inspector 编辑正式 payload
- **WHEN** 设计者选中 Animation、Motion、Window 或 Cue clip
- **THEN** Inspector MUST 显示并编辑对应正式 payload 字段
- **AND** payload 修改 MUST 写回正式 action definition
- **AND** 缺失必填 payload MUST 被 validator 报告

### Requirement: Timeline UI 迁移必须保持 Gameplay 边界
Timeline UI 迁移 MUST 只发生在 Editor-only 边界。Preview MUST 优先使用正式 evaluator 的数据结果，MUST NOT 引入第二 motion executor、第二 animation presenter、第二 blackboard writer、第二角色控制入口或 Ref gameplay runner。

#### Scenario: Runtime 不引用 Ref Timeline Runner
- **WHEN** 检查正式 runtime 源码和 asmdef
- **THEN** runtime MUST NOT 引用 `TimelinePlayer`
- **AND** MUST NOT 引用 Taco `BaseTree`、`RunnableTree`、`RunnableNode`、`TreeRunner`
- **AND** MUST NOT 使用 Ref `PlayableGraph` 执行动作 timeline

#### Scenario: Preview 使用正式 evaluator 数据
- **WHEN** 设计者拖动 preview locator 到某一帧
- **THEN** preview MUST 调用正式 `CommittedActionBranchEvaluator` 或批准的等价 evaluator
- **AND** MUST 显示 selected node id、animation key、motion spec、active window facts 和 cue requests
- **AND** 缺少 preview binding 时 MUST 显示明确未绑定状态，不得查找 scene object 或使用 fallback 配置

### Requirement: Timeline UI 迁移必须可测试
系统 MUST 提供自动测试和静态检查，证明 Unity 2022 兼容资源、editor timeline model、serialized writeback、preview evaluator 和 runtime 边界均符合本变更要求。

#### Scenario: 自动测试覆盖 UI 数据闭环
- **WHEN** 运行相关 EditMode 测试
- **THEN** 测试 MUST 覆盖 Directional / Backstep timeline 读取
- **AND** MUST 覆盖 track add / delete / reorder
- **AND** MUST 覆盖 clip add / delete / move / resize
- **AND** MUST 覆盖 payload inspector 写回
- **AND** MUST 覆盖 save 后重新读取与 `ToDefinition()` 编译结果

#### Scenario: 静态检查覆盖迁移边界
- **WHEN** 运行 timeline editor 静态边界测试
- **THEN** 测试 MUST 确认 runtime 不引用 UnityEditor、GraphView、TimelinePlayer、PlayableGraph 或 Taco runner
- **AND** MUST 确认迁移资源不包含 Ref 项目路径引用
- **AND** MUST 确认菜单、窗口标题和文档不使用通用技能编辑器命名

### Requirement: Timeline Editor 使用 Seconds Authoring 与 Tick Preview
Committed Action Timeline Editor MUST 以 seconds 作为 timeline authoring 主编辑单位，并将 tick grid、local tick 和量化结果作为预览与诊断信息展示。Editor MAY 复用 Ref 的 frame ruler、field view、track view 和 clip view 结构，但 MUST NOT 让 frame 术语成为正式 authoring 字段或 runtime 采样权威。

#### Scenario: 拖拽写回 seconds
- **WHEN** 设计者在 Committed Action Timeline Editor 中移动或缩放 clip
- **THEN** editor MUST 将结果写回该 clip 的 start seconds / end seconds 或批准的等价 seconds authoring 字段
- **AND** 保存后 action definition compiler MUST 能把这些 seconds 量化为 runtime tick 区间

#### Scenario: 预览显示 local time 和 local tick
- **WHEN** 设计者拖动 preview locator 到某个位置
- **THEN** editor MUST 显示对应 local time seconds
- **AND** MUST 显示按当前 fixed tick interval 量化得到的 local tick
- **AND** preview outcome MUST 调用正式 evaluator 或批准的等价 evaluator

#### Scenario: Tick grid 不成为 authoring 权威
- **WHEN** editor 显示 tick grid 或 frame-like 小刻度
- **THEN** grid MUST 只作为 seconds authoring 的辅助视图
- **AND** editor MUST NOT 将 UI 像素或 render frame 编号直接保存为 runtime timeline 权威数据

#### Scenario: Ref UI 迁移不恢复 frame authoring
- **WHEN** `port-ref-timeline-ui-to-unity-2022-compatible-editor` 迁移 Timeline field、track 或 clip view
- **THEN** 迁移后的 UI MUST 使用本项目 seconds authoring adapter
- **AND** MUST NOT 重新引入 `durationFrames/startFrame/endFrame` 作为正式 authoring 主字段

### Requirement: Behavior Graph 与 Action Branch Editor 边界
Character Behavior Editor MUST 继续只编辑 behavior source topology，例如 root、composite、Locomotion leaf、CommittedAction leaf、edge 和 editor position。Committed Action Branch Editor MUST 负责 action definition 内的 selector、condition、timeline node 和 timeline payload。两个编辑器 MAY 互相提供打开或定位入口，但 MUST NOT 复制、保存或编译对方的数据。

#### Scenario: Behavior Graph 不保存 Action Branch
- **WHEN** 设计者在 Character Behavior Editor 中保存 graph
- **THEN** 保存内容 MUST 限定为 behavior source topology
- **AND** MUST NOT 保存 selector、condition、TimelineNode、ActionTimeline track、clip、motion payload、animation key、window 或 cue
- **AND** Behavior compiler MUST NOT 编译 Committed Action branch payload

#### Scenario: Branch Editor 不编辑 Source Topology
- **WHEN** 设计者在 Committed Action Branch Editor 中编辑 action branch
- **THEN** 保存内容 MUST 写入 `CharacterActionDefinitionSO` 或批准等价 action definition
- **AND** MUST NOT 修改 behavior source graph 的 root、parallel、Locomotion leaf、CommittedAction leaf 或 edge
- **AND** Action definition compiler MUST NOT 创建 behavior source root 或 Locomotion leaf

### Requirement: Ref TreeDesigner 体验映射到项目 Adapter
Character Behavior Editor and Committed Branch mode MAY reuse Ref/wly970123 TreeDesigner interaction patterns, including fixed root presentation, left or in-window inspector, GraphView node shell, node panel, port/edge interaction, SearchWindow grouping and protected node capabilities. These interactions MUST map to this project's editor adapters and formal ScriptableObject authoring data. The editor MUST NOT save or execute Taco `BaseTree`, `RunnableTree`, `RunnableNode`, `RootNode`, `TimelinePlayer` or PlayableGraph runner as formal gameplay data.

#### Scenario: 固定 Root 只映射为项目 Root
- **WHEN** Committed Branch mode uses a Ref-style root node experience
- **THEN** the root MUST map to `CommittedActionBranchAuthoring.rootNodeId`
- **AND** it MUST NOT instantiate or save Ref `RootNode` or `EnterNode`
- **AND** runtime compiler MUST only consume this project's committed action branch definition

#### Scenario: SearchWindow 只创建正式节点类型
- **WHEN** the designer opens the node creation search window in Committed Branch mode
- **THEN** available entries MUST be limited to approved project node types such as Selector, Condition and Timeline
- **AND** root MUST NOT appear as a generic creatable node
- **AND** entries MUST NOT create Taco runtime node instances

#### Scenario: 节点属性面板通过 Adapter 写回
- **WHEN** the designer edits a branch node property through Ref-style node panel or inspector UI
- **THEN** the change MUST be written through this project's serialized adapter
- **AND** saving the action definition MUST persist the change in formal project authoring fields
- **AND** editor UI selection, layout and panel state MUST NOT become runtime authority

### Requirement: Behavior Source 到 Committed Branch 导航
Character Behavior Editor MUST allow a designer to navigate from a `CommittedActionLeaf` in Behavior Source mode to Committed Branch mode in the same `CharacterBehaviorEditorWindow`. The navigation MUST use a deliberate node open gesture such as double click or approved equivalent, MUST keep single click as selection only, and MUST use stable node id rather than array index. The navigation MUST NOT create a second Branch window, duplicate menu entry, embedded Branch graph, embedded Timeline panel, or runtime data path.

#### Scenario: 双击 CommittedActionLeaf 进入 Branch mode
- **GIVEN** Character Behavior Editor is in Behavior Source mode
- **AND** the graph contains a `CommittedActionLeaf`
- **WHEN** the designer double-clicks or performs the approved open gesture on that node
- **THEN** the same editor window MUST switch to Committed Branch mode
- **AND** it MUST populate the branch graph from the selected or default formal `CharacterActionDefinitionSO`
- **AND** it MUST select Branch Root or an approved equivalent branch entry node

#### Scenario: 单击只选择节点
- **GIVEN** Character Behavior Editor is in Behavior Source mode
- **WHEN** the designer single-clicks a `CommittedActionLeaf`
- **THEN** the editor MUST select that source node
- **AND** it MUST NOT switch modes
- **AND** it MUST NOT open Timeline Editor

#### Scenario: 导航不新增窗口
- **WHEN** the designer opens a committed branch from Behavior Source mode
- **THEN** the system MUST reuse `CharacterBehaviorEditorWindow`
- **AND** it MUST NOT open `CommittedActionBranchEditorWindow`
- **AND** it MUST NOT add `Tools/3C/Committed Action Branch Editor`

#### Scenario: 缺少 ActionDefinition 只报诊断
- **GIVEN** a `CommittedActionLeaf` is opened from Behavior Source mode
- **AND** no current or default formal `CharacterActionDefinitionSO` can be resolved
- **WHEN** the editor handles the navigation
- **THEN** it MUST show a clear diagnostic
- **AND** it MUST NOT create a fallback branch, sample action definition, Resources lookup, or hidden runtime default

### Requirement: Behavior 与 Branch 图的编辑器关系可解释
Character Behavior Editor MUST present Behavior Source mode and Committed Branch mode as two editor views over different formal data sources. Behavior Source mode MUST represent source topology, while Committed Branch mode MUST represent a single action definition branch. The editor MAY provide navigation between the two views, but MUST NOT merge their authoring data or compiler responsibilities.

#### Scenario: 两张图使用不同 adapter
- **WHEN** Behavior Source mode populates the graph
- **THEN** it MUST use the behavior authoring graph adapter or approved equivalent
- **AND** it MUST read and write `CharacterBehaviorAuthoringAsset` source topology
- **WHEN** Committed Branch mode populates the graph
- **THEN** it MUST use the committed action branch adapter or approved equivalent
- **AND** it MUST read and write `CharacterActionDefinitionSO` branch authoring

#### Scenario: 导航不改变数据所有权
- **GIVEN** a designer navigates from `CommittedActionLeaf` to Committed Branch mode
- **WHEN** the designer edits selector, condition or TimelineNode data
- **THEN** those edits MUST be saved only in the selected `CharacterActionDefinitionSO`
- **AND** the behavior source authoring asset MUST NOT store a copy of selector, condition, TimelineNode, track, clip or payload data

### Requirement: CommittedActionLeaf 使用 Action Catalog 导航 ActionDefinition
Character Behavior Editor MUST make `CommittedActionLeaf` open a formal Action Catalog navigation flow instead of hardcoding `Action.Dodge`. The navigation flow MUST resolve editable actions from `CharacterConfigSO.ActionCatalog`, an explicitly selected `CharacterActionCatalogSO`, or an approved equivalent formal character action catalog source. The selected entry MUST switch the same `CharacterBehaviorEditorWindow` into Committed Branch mode bound to the selected `CharacterActionDefinitionSO`.

#### Scenario: 单个 ActionDefinition 直接进入 Branch
- **GIVEN** Behavior Source graph contains a `CommittedActionLeaf`
- **AND** the resolved Action Catalog contains exactly one valid `CharacterActionDefinitionSO`
- **WHEN** the designer opens `CommittedActionLeaf`
- **THEN** the editor MUST switch the same window to Committed Branch mode
- **AND** the Branch graph MUST bind that action definition
- **AND** the editor MUST NOT use a hardcoded Dodge asset path, Resources lookup, sample asset or hidden branch fallback

#### Scenario: 多个 ActionDefinition 先选择 Action
- **GIVEN** the resolved Action Catalog contains multiple valid action definitions
- **WHEN** the designer opens `CommittedActionLeaf`
- **THEN** the editor MUST show an in-window action selection entry or approved equivalent picker
- **AND** the selectable entries MUST come from the Action Catalog
- **WHEN** the designer chooses one entry
- **THEN** the editor MUST switch the same window to Committed Branch mode for the chosen `CharacterActionDefinitionSO`
- **AND** it MUST NOT open a second Branch editor window

#### Scenario: Catalog 缺失时只显示诊断
- **GIVEN** Behavior Source graph contains a `CommittedActionLeaf`
- **AND** no formal character config or Action Catalog can be resolved
- **WHEN** the designer opens `CommittedActionLeaf`
- **THEN** the editor MUST show a clear diagnostic
- **AND** it MUST remain outside Committed Branch editing for an unknown action
- **AND** it MUST NOT default to `Action.Dodge`

#### Scenario: Branch 节点不进入主图
- **GIVEN** the Action Catalog contains `Action.Dodge` and another action
- **WHEN** the Behavior Source graph is displayed
- **THEN** the graph MAY show one `CommittedActionLeaf`
- **AND** it MUST NOT show Branch Root, Selector, Condition or TimelineNode nodes inside the Behavior Source graph
- **AND** those branch nodes MUST only appear after an action definition is selected in Committed Branch mode

### Requirement: Action Catalog 导航与 Ref UI Shell 解耦
Action Catalog navigation MUST be implemented as an editor adapter/data flow that can be hosted by the current Character Behavior Editor shell or the Ref source-ported shell. The navigation MUST NOT depend on Ref runtime types, Taco runtime trees, GraphView object identity or Behavior Source serialized action copies.

#### Scenario: Ref shell 更换不改变 catalog 数据源
- **GIVEN** Character Behavior Editor uses a Ref-style source-ported graph shell
- **WHEN** `CommittedActionLeaf` is opened
- **THEN** the action list MUST still come from the formal Action Catalog
- **AND** the selected action MUST still bind a project `CharacterActionDefinitionSO`
- **AND** no Ref `BaseTree`, `RunnableTree`, `RunnableNode` or Taco runtime object may become the action source

#### Scenario: 选择状态不是运行时权威
- **WHEN** a designer selects an action from the in-window catalog navigation UI
- **THEN** the selection MAY update editor session state
- **AND** it MUST NOT be serialized as runtime authority into GraphView nodes, ports, layout, selection state or Ref shell objects

### Requirement: Behavior / Branch Editor 必须源码级替换为 Ref TreeDesigner 组件结构
Character Behavior Editor 与 Committed Branch mode MUST 将当前半移植或自研节点树 shell 替换为 Ref/Taco TreeDesigner / GraphView 的源码级等价组件结构。实现可以使用项目命名和项目 adapter，但 MUST 提供 Ref 等价的 GraphView shell、node view、port view、edge view、SearchWindow、selection、fixed root 展示、node property panel 和受保护节点能力。旧 card/list 伪图、重复 branch editor 窗口或半自研节点编辑路径 MUST NOT 作为正式编辑入口保留。

#### Scenario: GraphView 从项目 adapter 建立节点树
- **WHEN** 打开 Character Behavior Editor 或 Committed Branch mode
- **THEN** GraphView MUST 从项目正式 behavior source authoring 或 committed action branch authoring adapter 建立节点、端口、连线和 layout
- **AND** MUST NOT 从 Ref `BaseTree`、`RunnableTree`、sample asset 或临时 editor object 生成正式 graph

#### Scenario: 固定 Root 与 SearchWindow 对齐 Ref 体验
- **WHEN** graph 包含固定 root
- **THEN** root MUST 作为 protected root node 展示并映射到项目正式 root id
- **AND** root MUST NOT 通过普通 SearchWindow 创建或删除
- **WHEN** 设计者打开 SearchWindow
- **THEN** entries MUST 只创建项目批准的 node kind
- **AND** MUST NOT 创建 Taco runtime node instance

#### Scenario: Node Panel 只写回项目正式数据
- **WHEN** 设计者在 node panel 修改 selector、condition、timeline node 或 behavior source node 属性
- **THEN** 修改 MUST 通过 stable node id 写回项目 adapter
- **AND** GraphView selection、layout、port 和 edge object MUST NOT 成为 runtime 权威

### Requirement: Behavior Graph 与 Timeline Editor 继续分窗且不分裂数据
Character Behavior Editor MUST 继续只编辑 behavior source topology 或 Committed Action branch 节点树；Committed Action Timeline Editor MUST 继续作为独立窗口编辑 selected TimelineNode 的 timeline field / track / clip / payload。两个窗口 MAY 互相打开或定位，但 MUST NOT 复制、保存或编译对方的数据。

#### Scenario: TimelineNode 打开独立 Timeline Editor
- **GIVEN** Committed Branch graph 中选中了 TimelineNode
- **WHEN** 设计者打开 timeline
- **THEN** 系统 MUST 打开或聚焦独立 `CommittedActionTimelineEditorWindow`
- **AND** 该窗口 MUST 读写该 TimelineNode 的 timeline authoring 数据
- **AND** Branch graph 窗口 MUST NOT 内嵌 timeline field、track view、clip view 或 clip inspector

#### Scenario: 保存 Graph 不修改 Timeline 数据
- **WHEN** 设计者在 Character Behavior Editor 中移动节点、创建 edge 或编辑 source node
- **THEN** 保存内容 MUST 限定为对应 graph / branch node authoring 数据
- **AND** MUST NOT 修改其它 TimelineNode 的 track、clip、motion payload、animation key、window 或 cue


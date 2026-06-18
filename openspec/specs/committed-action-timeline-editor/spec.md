# committed-action-timeline-editor Specification

## Purpose
定义 Committed Action Timeline Editor 的 Editor-only 边界、正式 ActionDefinition 数据源、Ref Timeline UI 迁移范围、preview evaluator 和测试要求。
## Requirements
### Requirement: Timeline Editor 以正式 Action Definition 为数据源
Committed Action Timeline Editor MUST 以本项目正式 `CharacterActionDefinitionSO` 作为唯一默认编辑入口。编辑器 MAY 支持用户选择其它正式 action definition，但 MUST NOT 默认加载 `Behavior/Samples` authoring asset，也 MUST NOT 生成 sample-only runtime definition 作为正式 gameplay 输入。

#### Scenario: 默认打开正式 Dodge ActionDefinition
- **WHEN** 设计者打开 `Tools/3C/Committed Action Timeline Editor`
- **THEN** 编辑器 MUST 默认加载 `Assets/Configs/3C/Action/Corin/Actions/Dodge/CorinDodgeActionDefinition.asset`
- **AND** ObjectField MUST 限制为 `CharacterActionDefinitionSO`
- **AND** MUST NOT 默认加载 `CorinDodgeBehaviorAuthoring.asset`

#### Scenario: 保存写回正式 ActionDefinition
- **GIVEN** 设计者移动或修改一个 Dodge timeline clip
- **WHEN** 设计者保存
- **THEN** 修改 MUST 写回被选择的 `CharacterActionDefinitionSO`
- **AND** 保存后 `CharacterActionDefinitionSO.ToDefinition()` MUST 能生成对应 runtime definition

### Requirement: Ref Timeline UI 迁移到 Editor-only Adapter
系统 MUST 将 `Ref/wly970123` Taco timeline 的主要编辑器交互迁移到本项目 Editor-only assembly，并通过 adapter 读写本项目 timeline authoring 数据。迁移后的 UI MUST NOT 直接保存 Taco `Timeline`、`Track`、`Clip` runtime object。

#### Scenario: 迁移 timeline field 和 track/clip view
- **WHEN** 设计者查看 Committed Action Timeline Editor
- **THEN** 编辑器 MUST 提供 track hierarchy、time marker、locator、track view、clip view 和 inspector
- **AND** UI 资源 MAY 来自 Ref UXML / USS / 图标
- **AND** 数据 MUST 映射到本项目 `ActionTimelineTrackAuthoring` 和 `ActionTimelineClipAuthoring`

#### Scenario: Ref runtime 不进入正式 gameplay
- **WHEN** 检查正式 runtime assembly 或 `Assets/Scripts/Character`
- **THEN** runtime MUST NOT 引用 `TimelinePlayer`
- **AND** MUST NOT 引用 Taco `BaseTree`、`RunnableTree`、`RunnableNode`、`TreeRunner`
- **AND** MUST NOT 通过 Ref `PlayableGraph` 执行动作 timeline

### Requirement: Timeline Editor 支持正式 Track 与 Clip 编辑
Timeline Editor MUST 支持对正式 action timeline 的 track 和 clip 进行结构编辑。所有编辑 MUST 通过正式 adapter 写入 Unity serialization，并 MUST 接受正式 validator 校验。

#### Scenario: Track 编辑
- **WHEN** 设计者编辑 Directional 或 Backstep timeline
- **THEN** 编辑器 MUST 支持添加、删除、选择、重排 Animation、Motion、Hitbox、Cancel、Cue track
- **AND** track kind MUST 来自正式 `ActionTimelineTrackKind`
- **AND** 非法 track kind MUST 被拒绝或报告错误

#### Scenario: Clip 编辑
- **WHEN** 设计者编辑一个 track
- **THEN** 编辑器 MUST 支持添加、删除、移动、左右缩放和选择 clip
- **AND** clip kind MUST 来自正式 `ActionTimelineClipKind`
- **AND** AnimationKey、Motion、HitboxWindow、CancelWindow、Cue payload MUST 可编辑
- **AND** 非法 seconds / tick 区间或缺失 payload MUST 被 validator 报告

### Requirement: Dodge Selector 与 Directional / Backstep Timeline 可编辑
系统 MUST 让 Dodge selector 和两个 timeline 作为同一正式 Dodge action definition 的可编辑数据。Directional 与 Backstep 的选择规则 MUST 继续由 `CommittedActionBranchEvaluator` 解释，Timeline Editor MUST NOT 创建第二套 selector 语义。

#### Scenario: Directional timeline 可编辑并可编译
- **GIVEN** 正式 Dodge action definition 包含 Directional timeline
- **WHEN** 设计者修改 Directional 的 Animation、Motion、Window 或 Cue clip
- **THEN** 保存后的 definition MUST 通过 `CommittedActionBranchEvaluator` 选择 `timeline.dodge.directional`
- **AND** evaluator outcome MUST 反映修改后的 timeline payload

#### Scenario: Backstep timeline 可编辑并可编译
- **GIVEN** 正式 Dodge action definition 包含 Backstep timeline
- **WHEN** 设计者修改 Backstep 的 Animation、Motion、Window 或 Cue clip
- **THEN** 保存后的 definition MUST 通过 `CommittedActionBranchEvaluator` 选择 `timeline.dodge.backstep`
- **AND** evaluator outcome MUST 反映修改后的 timeline payload

### Requirement: Timeline Preview 使用正式 Evaluator
Timeline preview MUST 基于本项目正式 `CommittedActionBranchEvaluator` 和 `ActionTimelineEvaluator` 展示当前 local time / local tick 结果。Preview MAY 提供 Editor-only 视觉预览绑定，但 MUST NOT 改变正式 gameplay 的 motion executor、animation presenter、blackboard writer 或角色帧管线。

#### Scenario: 数据预览显示 runtime outcome
- **WHEN** 设计者拖动 preview locator 到某一帧
- **THEN** preview MUST 显示 selected node id
- **AND** MUST 显示当前 local tick animation key、motion spec、active window facts 和 cue requests
- **AND** 显示结果 MUST 与 runtime evaluator 对同一 definition 的输出一致

#### Scenario: 视觉预览不成为 gameplay runner
- **WHEN** 编辑器实现动画、motion 或 cue 视觉预览
- **THEN** 预览代码 MUST 位于 Editor-only assembly
- **AND** runtime MUST NOT 引用该 preview binding
- **AND** 缺失 preview binding 时 MUST 显示明确未绑定状态，不得使用 scene 查找或隐藏 fallback

### Requirement: Timeline Editor 不编辑角色帧权威边界
Timeline Editor MUST NOT 暴露 `CharacterFramePipeline` phase、motion executor、Animancer presenter、blackboard writer、input consume 或 output apply 的重排入口。Timeline 只能编辑 committed action 的 selector/timeline 数据。

#### Scenario: 编辑 timeline 不改变 frame pipeline
- **WHEN** 设计者在 Timeline Editor 中添加 Motion 或 Animation clip
- **THEN** clip MUST 只改变 action timeline authoring data
- **AND** 最终 motion 仍由正式 output applier 调用统一 motion executor
- **AND** 最终 animation 仍由正式 output applier 调用正式 animation presenter

### Requirement: Timeline Editor 可测试
系统 MUST 提供 EditMode 测试和静态边界测试，证明迁移后的 editor 真正读写正式配置、preview 使用正式 evaluator，且 runtime 边界没有引入 Ref runner。

#### Scenario: 自动测试覆盖迁移能力
- **WHEN** 运行 timeline editor adapter EditMode 测试
- **THEN** 测试 MUST 覆盖正式 Dodge asset 读取
- **AND** MUST 覆盖 track add/remove/reorder
- **AND** MUST 覆盖 clip add/move/resize/delete
- **AND** MUST 覆盖 payload 写回
- **AND** MUST 覆盖保存后编译 Directional / Backstep runtime definition
- **AND** MUST 覆盖非法 timeline 报错
- **AND** MUST 覆盖 preview adapter 与 runtime evaluator 一致

#### Scenario: 静态边界测试
- **WHEN** 运行 runtime 边界测试
- **THEN** 测试 MUST 确认 runtime 不引用 UnityEditor、GraphView、TimelinePlayer、PlayableGraph 或 Taco runner
- **AND** 测试 MUST 确认 editor 菜单、窗口标题和文档不把本阶段称为通用 Skill Editor

### Requirement: Timeline Editor 定位 Branch TimelineNode
Committed Action Timeline Editor MUST support opening or focusing an independent timeline window for the TimelineNode selected in Committed Action Branch Editor. The independent window MUST use the selected TimelineNode serialized adapter to read and write timeline authoring track, clip and payload. The Branch graph window MUST only select TimelineNode, show timeline summary and provide an open/focus action. Timeline Editor MUST NOT embed its field/track/clip editor inside the Branch graph window and MUST NOT use Dodge-specific Directional / Backstep fields as formal save targets.

#### Scenario: 独立 Timeline Window 编辑选中节点
- **GIVEN** Branch Editor selected TimelineNode A
- **WHEN** the designer opens Timeline Editor
- **THEN** the system MUST open or focus independent `CommittedActionTimelineEditorWindow`
- **AND** the Timeline Editor MUST read and write TimelineNode A timeline authoring data
- **AND** TimelineNode B timeline authoring data MUST remain unchanged
- **AND** preview outcome MUST use the same action definition compiled runtime branch

#### Scenario: 独立窗口只作为同一数据的编辑入口
- **WHEN** the designer opens `Tools/3C/Committed Action Timeline Editor`
- **THEN** the tool MUST edit a formal `CharacterActionDefinitionSO` timeline node through selected TimelineNode adapter or approved equivalent selection
- **AND** saving MUST write back to the branch authoring TimelineNode
- **AND** it MUST NOT create a timeline definition independent from branch authoring

#### Scenario: Branch Graph 不嵌入 Timeline Field
- **WHEN** checking Committed Branch graph implementation
- **THEN** the Branch graph window MUST NOT contain a Timeline field, track hierarchy, clip view or clip inspector as an embedded timeline editor
- **AND** it MUST only expose node selection, node summary, node property panel and open/focus Timeline Editor action

### Requirement: Timeline Scene Preview Binding
Committed Action Timeline Editor MUST support an explicit Editor-only scene preview target binding for visual preview. The target MAY be a scene GameObject containing an Animator or an approved equivalent scene preview target. The binding MUST be a temporary editor preview binding and MUST NOT be saved into `ActionTimelineDefinition`, `CommittedActionBranchDefinition`, runtime snapshots, rollback data, or formal gameplay configuration.

#### Scenario: Bind scene character target
- **GIVEN** the designer has opened an independent Committed Action Timeline Editor
- **WHEN** the designer assigns a scene character GameObject with an Animator as preview target
- **THEN** the editor MUST report the target as bound
- **AND** visual preview MAY sample that target in EditMode
- **AND** the action definition and runtime timeline definition MUST NOT store the scene object reference

#### Scenario: Missing target remains data preview
- **WHEN** no preview target is assigned
- **THEN** Timeline preview MUST continue to show compiler / evaluator data results
- **AND** visual preview status MUST show an explicit unbound state
- **AND** the editor MUST NOT search the scene hierarchy, Resources, global singletons, or default prefabs as a hidden fallback

#### Scenario: Invalid target reports diagnostic
- **GIVEN** the designer assigns a preview target without an Animator or approved equivalent preview component
- **WHEN** the Timeline Editor refreshes preview binding
- **THEN** the editor MUST report a clear invalid target diagnostic
- **AND** MUST NOT silently bind another object

### Requirement: Timeline Visual Preview Uses Formal Evaluator Outcome
Visual preview MUST consume the same `CommittedActionBranchEvaluator` and `ActionTimelineEvaluator` outcome used by data preview. Visual preview MUST NOT decide selector branch, condition result, local tick, active clip, animation key, motion spec, window fact, or cue request from GraphView state, scene object state, Animator playback time, Unity frame delta, or Ref/Taco timeline state.

#### Scenario: Scrub samples evaluated animation key
- **GIVEN** a preview target is bound
- **AND** current local tick evaluates to an `ActionAnimationKey`
- **WHEN** the designer scrubs the timeline locator
- **THEN** the editor MUST first evaluate the formal action definition for that local tick
- **AND** visual preview MUST sample the animation resolved from the evaluated `ActionAnimationKey`
- **AND** the sampled pose MUST NOT come from an unevaluated editor-only selected clip

#### Scenario: Selector result controls visual timeline
- **GIVEN** a Committed Action branch has multiple TimelineNode paths
- **WHEN** formal evaluator selects TimelineNode A for the preview context
- **THEN** visual preview MUST use TimelineNode A's outcome
- **AND** TimelineNode B MUST NOT drive animation, motion, window, or cue preview for that tick

### Requirement: Timeline Preview Resolves Animation Through Formal Binding
Timeline visual preview MUST resolve `ActionAnimationKey` through the formal action animation binding entry, Action Animation Profile, Animancer TransitionLibrary, or approved equivalent presentation binding associated with the bound preview target. It MUST NOT store concrete `AnimationClip`, Animancer transition asset, or scene object reference in ActionTimeline runtime data.

#### Scenario: Resolve key to preview clip
- **GIVEN** the bound preview target has a formal animation binding where `Action.Dodge.Directional` resolves to a playable clip or transition
- **AND** the evaluated timeline outcome contains `Action.Dodge.Directional`
- **WHEN** visual preview samples the current tick
- **THEN** the preview resolver MUST resolve that key through the bound presentation configuration
- **AND** the preview session MAY sample the resolved clip in Editor-only code

#### Scenario: Missing animation binding is explicit
- **GIVEN** the evaluated timeline outcome contains an animation key
- **AND** the bound preview target cannot resolve that key
- **WHEN** visual preview refreshes
- **THEN** the editor MUST show a clear missing binding diagnostic
- **AND** MUST NOT guess a clip by name, asset search, Resources, scene scan, or Ref sample data

#### Scenario: Resolver does not play runtime presenter
- **WHEN** visual preview resolves an animation key
- **THEN** the resolver MUST NOT call the formal runtime presenter play method
- **AND** MUST NOT mutate action lifecycle, blackboard, motion executor, or CharacterFramePipeline state

### Requirement: Timeline Preview Samples Animator Through Editor-only PlayableGraph
Timeline visual preview MAY use an Editor-only PlayableGraph to sample the bound preview target's Animator. The graph MUST be owned by the Timeline Editor preview session, MUST be destroyed when preview stops, target changes, window closes, or domain reloads, and MUST NOT become the formal ActionTimeline runtime runner.

#### Scenario: Scrub evaluates pose without gameplay tick
- **GIVEN** a preview target and animation binding are valid
- **WHEN** the designer moves the preview locator to local tick N
- **THEN** the preview session MAY set the resolved clip time derived from tick N
- **AND** MAY evaluate the Editor-only graph to update the Animator pose
- **AND** MUST NOT tick Action lifecycle, CharacterFramePipeline, motion executor, hitbox logic, VFX, SFX, or camera systems

#### Scenario: Preview cleanup restores ownership
- **GIVEN** a visual preview graph is active
- **WHEN** the designer clears the target, closes the window, stops preview, or Unity reloads domain
- **THEN** the preview session MUST destroy its graph
- **AND** MUST release Animator ownership and restore required target state or approved equivalent preview-safe state

#### Scenario: Ref PlayableGraph stays editor-only
- **WHEN** checking formal runtime assemblies
- **THEN** runtime MUST NOT reference Ref/Taco `TimelinePlayer`
- **AND** MUST NOT reference the Timeline Editor preview session
- **AND** MUST NOT use `PlayableGraph` as the ActionTimeline gameplay execution path

### Requirement: Timeline Scene Preview Does Not Execute Gameplay Effects
Timeline scene preview MUST keep non-animation clips as editor diagnostics in the first version. Motion clips MAY display motion spec, direction, duration, distance, warp payload, or a preview ghost/path, but MUST NOT call the formal motion executor. Window and Cue clips MAY be highlighted and listed, but MUST NOT trigger hit detection, damage, VFX, SFX, camera events, post-processing, or runtime blackboard writes.

#### Scenario: Motion preview is diagnostic
- **GIVEN** the evaluated outcome contains a Motion clip
- **WHEN** visual preview refreshes
- **THEN** the editor MAY display motion distance, duration, rotate-to-direction, and warp payload diagnostics
- **AND** MUST NOT move the bound character through `CharacterMotionDriver`, `CharacterController.Move`, root motion application, or motion warping solver

#### Scenario: Window and cue preview are diagnostic
- **GIVEN** the evaluated outcome contains active window facts or cue requests
- **WHEN** visual preview refreshes
- **THEN** the editor MAY highlight those clips and list their ids
- **AND** MUST NOT spawn hitboxes, apply damage, play VFX or SFX, trigger camera shake, or write runtime blackboard facts

### Requirement: Timeline Scene Preview Is Tested and Bounded
Timeline scene preview MUST provide EditMode tests and static boundary tests proving that preview binding, key resolution, visual sampling lifecycle, and runtime separation are correct.

#### Scenario: Automatic tests cover preview binding
- **WHEN** Timeline preview binding tests run
- **THEN** they MUST cover unbound target, invalid target, successful Animator binding, missing animation binding, and successful animation key resolution

#### Scenario: Automatic tests cover sampling lifecycle
- **WHEN** Timeline preview session tests run
- **THEN** they MUST cover graph creation, scrub time mapping, graph cleanup, and target change cleanup through testable seams or approved equivalent EditMode coverage

#### Scenario: Static runtime boundary validation
- **WHEN** runtime boundary tests run
- **THEN** they MUST confirm runtime does not reference Timeline Editor preview binding or preview session types
- **AND** MUST confirm ActionTimeline runtime does not store scene target, Animator, AnimationClip, PlayableGraph, or Ref/Taco runtime objects

### Requirement: Timeline Editor 必须源码级替换为 Ref 组件结构
Committed Action Timeline Editor MUST 将当前半移植 / 自研混合的 timeline shell 替换为 Ref/Taco Timeline editor 的源码级等价组件结构。实现可以使用项目命名和项目 adapter，但 MUST 明确提供 Ref 等价的 field view、track view、track handle、clip view、drag manipulator、drag line manipulator、selection、locator、frame position map、move leader、apply move、resize clamp、rectangle selection、pan、zoom、focus 和 context menu 职责。旧的 root pointer mode 推断、局部 frame delta 拼接、card/list timeline 伪编辑面或临时 fallback UI MUST NOT 作为正式编辑路径保留。

#### Scenario: Clip 拖拽与伸缩由独立 manipulator 负责
- **WHEN** 设计者在 timeline 中拖动 clip 主体
- **THEN** move drag manipulator MUST 将开始、移动和结束事件委托给 field view 的 move leader / apply move 流程
- **AND** 该流程 MUST 通过正式 adapter 写回 selected TimelineNode 的 seconds authoring 数据
- **WHEN** 设计者拖动 clip 左右边缘
- **THEN** left resize 和 right resize MUST 使用独立 drag line manipulator 或批准等价结构
- **AND** resize MUST NOT 依赖 root clip pointer mode 猜测

#### Scenario: Field View 持有坐标和 selection 权威
- **WHEN** 设计者 pan、zoom、拖动 locator、框选或多选移动 clip
- **THEN** field view MUST 持有 frame / tick position map、scale、offset、selection、move leader 和 move validation
- **AND** clip view MUST NOT 保存第二套 timeline 权威数据
- **AND** 所有写回 MUST 进入正式 timeline serialized adapter

#### Scenario: 旧半移植交互路径被移除
- **WHEN** 检查 Timeline Editor 实现
- **THEN** 当前被替换的半自研交互 path MUST 删除或不可达
- **AND** MUST NOT 同时存在 Ref-equivalent manipulator path 与旧 root pointer delta path 两套可编辑路径
- **AND** MUST NOT 存在隐藏 fallback 配置来选择旧 timeline editor

### Requirement: Timeline Editor 保持项目数据与运行时边界
源码级移植后的 Timeline Editor MUST 只作为 Editor-only Presentation Layer。UI 内部可以使用 Ref 风格 frame / tick 位置映射，但正式 authoring 字段 MUST 继续使用 seconds，compiler MUST 继续执行 seconds authoring -> fixed tick compile -> runtime tick sampling。正式 runtime MUST NOT 保存或执行 Ref `Timeline`、`Track`、`Clip`、`TimelinePlayer`、PlayableGraph runner 或 Taco asset。

#### Scenario: Ref 数据模型被项目 adapter 替换
- **WHEN** timeline field、track 或 clip view 需要读取或修改数据
- **THEN** 它 MUST 通过项目 timeline editor snapshot、serialized adapter 或批准等价 adapter 访问 `CharacterActionDefinitionSO`
- **AND** MUST NOT 直接持有 Taco `Timeline`、`Track` 或 `Clip` 作为正式保存对象

#### Scenario: Runtime 边界保持干净
- **WHEN** 运行静态边界测试
- **THEN** runtime source MUST NOT 引用 UnityEditor、GraphView、TimelinePlayer、PlayableGraph、Taco `BaseTree`、`RunnableTree`、`RunnableNode` 或 Ref editor view
- **AND** preview MAY 使用 Editor-only visual sampling，但 MUST NOT 成为 gameplay runner


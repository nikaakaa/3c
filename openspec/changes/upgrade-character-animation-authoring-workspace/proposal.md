# Change: 升级角色动画作者工作区

## Why

当前`GraphAuthoringEditorShell`已经统一承载BTSMTL Graph与Character Presentation Pose Graph的窗口、GraphView、搜索、Clipboard、Undo、Inspector和只读diagnostics，但正式布局仍是固定`200px`左栏加右侧画布。Pose Graph虽然已经能够编辑完整表现节点并接收正式runtime snapshot，作者仍难以在一个工作区回答以下问题：

- 当前图有哪些Selection、Parameter、Subgraph和可达Animation producer。
- 选中节点有哪些正式可写配置，哪些数据只属于Timeline、Profile、Rig或generated Projection。
- 当前运行实例正在经过哪些节点、使用什么source、权重、raw/effective time和阶段completion。
- 一个视觉问题来自source采样、Marker Sync、Player、Blend、Inertialization、Foot Placement还是最终输出。

UE Animation Blueprint已经形成稳定的使用心智：左侧查找图和数据，中间编辑Anim Graph，右侧Details调整选中对象，Viewport、Pose Watch和运行时调试解释中间Pose。项目不需要复制UE的Event Graph、Anim State Machine、Montage runtime或Post Process Anim Blueprint，但应借用语义准确的公开概念和编辑器交互，降低作者与招聘展示的学习成本。

current `btsmtl-tree-inspector-information-architecture`和`btsmtl-graph-data-catalog-authoring`仍要求Data与Inspector作为左侧互斥页签。这与目标工作区直接冲突。本change将破坏性替换旧布局，不保留旧页签模式、布局切换开关或第二套Pose Graph窗口。

## What Changes

- 将唯一`GraphAuthoringEditorShell`升级为可组合工作区，提供Toolbar、左侧Navigator/Data、中央Graph Canvas、右侧Details和可折叠Bottom Dock五个通用区域。
- 将BTSMTL Tree、AI Graph和Pose Graph迁移到同一工作区规则：左侧查找正式数据，中间编辑拓扑，右侧编辑选中对象，底部观察诊断；删除旧左栏Data/Inspector互斥路径。
- 保留现有domain adapter和真实serialized owner边界。Shell只装配区域，不读取BTSMTL或动画业务字段，不保存第二份node、edge、selection、catalog或runtime状态。
- 为Pose Graph增加Definition-scoped Navigator，列出Graph/Subgraph、Animation Channel、Parameter与正式可达producer。producer目录只通过显式`CharacterPipelineDefinition`上下文和`CharacterAnimationPresentationAuthoringService`投影，不扫描目录、不反读generated Program/Projection，也不成为第二个写入口。
- 将Pose Graph右侧Details分为`Authoring`、`Live`与`References`：Authoring只通过现有mutation adapter编辑当前Pose节点；Live只读取匹配PoseGraph/Projection revision的正式snapshot；References只显示source map、可达producer和唯一owner跳转。
- 在Pose Graph画布显示语义准确的节点标题、阶段角标、Sync Group水印、当前source、权重、availability和执行高亮。UI只在语义完全相同时采用UE名称；不得把AnimationChannel伪称为Montage Slot、把BTSMTL Timeline改名为Montage，或把主图的world-aware阶段称为UE Post Process Anim Blueprint。
- 增加显式Pose Preview会话。只有作者选择精确Definition/Preview Target并点击播放、暂停、单帧或seek时才执行匹配Projection revision的正式Pose Plan；缺少或过期产物时显示Unavailable/Stale，不自动Build、不创建简化播放器。
- 增加editor-only Pose Watch。作者按稳定PoseNodeId显式订阅一个或多个中间Pose；Preview或正式runtime只从已完成Pose Plan workspace复制有界诊断结果，不重新求值节点、不采样第二次source、不进入authoring资产序列化。
- 增加可折叠Bottom Dock，统一承载Preview、Pose Watch、Diagnostics与只读Sync时间尺。Marker frame、SyncGroup、Role、Animation Clip和registered Curve仍只在Timeline Editor正式修改；Pose Graph只提供精确来源导航。
- 将Compile/Build保持为明确用户命令。selection、Inspector focus、Graph mutation、窗口恢复、AssetDatabase事件和Preview目标切换均不得自动触发Program、Projection、Foot Analysis或Motion Matching Database构建。
- 删除旧固定左侧Inspector布局、旧Data/Inspector互斥页签装配和Pose Graph专用的重复状态展示；不保留兼容UXML、旧窗口入口或临时布局桥接。

## Non-Goals

- 不新增、删除或改变Pose Graph runtime节点、typed port、执行阶段、Marker Sync、Blend Stack、Inertialization或Foot Placement算法。
- 不把BTSMTL Gameplay StateMachine迁入Pose Graph，也不新增UE式Animation State Machine runtime。
- 不把BTSMTL Timeline改造成Montage，不新增Montage、Slot或Post Process Anim Blueprint资产类型。
- 不允许Pose Graph编辑Timeline Marker、Clip、Curve、Profile、Rig、Foot Analysis generated data或Motion Matching Database。
- 不建立第二个GraphView、Preview evaluator、RuntimeDebugSession、Pose writer、Program/Projection builder或Agent authoring入口。
- 不修改Agent可见或可写authoring语义；Agent Document、Snapshot、Patch/MCP仍对Pose Graph保持既有只读边界。
- 不在本change新增自动构建、后台导入、Play Mode自动切换或Unity batchmode流程。

## Dependencies

- `add-character-presentation-pose-graph`必须先安装唯一Graph Authoring Editor Shell、Pose Graph document/adapters、正式节点拓扑、source map和runtime snapshot identity。
- `refactor-animation-selection-pose-graph-boundary`必须先安装`AnimationSelectionInput -> MarkerSync -> SelectedPosePlayer/BlendStack -> Pose -> FootPlacement -> OutputPose`的最终显式链路，并删除隐藏PoseSlot/Stack/FootPlacement路径。
- `refactor-inertial-blending-to-local-pose-node`与`refactor-animation-playback-to-blend-stack`必须先收口各节点的唯一runtime owner，工作区只观察最终正式语义。
- 本change不得把这些active change的过渡字段、旧PoseSlot、旧Blend Library或当前临时runtime结构固化进UI合同。

## Impact

- `GraphAuthoringEditorShell`、共享UXML/USS、Graph domain adapter和窗口view-state。
- BTSMTL Tree/AI窗口的Data Catalog、selection Details和Live Debug装配位置。
- Character Presentation Pose Graph Editor的Navigator、Details、runtime overlay、Preview、Pose Watch和Bottom Dock。
- `CharacterAnimationPresentationAuthoringService`的只读Definition-scoped producer投影与精确导航。
- `AnimationPresentationRuntimeSnapshot`/RuntimeDebugSession的显式debug-interest与有界Pose Watch读取合同；不改变正式动画输出。
- current `btsmtl-tree-inspector-information-architecture`与`btsmtl-graph-data-catalog-authoring`的旧左侧互斥页签要求。
- active `graph-authoring-editor-shell`与`character-presentation-pose-graph`能力的工作区增量。

## Current Spec Comparison

- current `btsmtl-tree-inspector-information-architecture`要求Data与Inspector作为左侧互斥工作页；本change将其替换为左侧Data/Navigator与右侧Details两个独立区域，并保持Authoring/Live Debug仍是窗口级模式。
- current `btsmtl-graph-data-catalog-authoring`要求切到Inspector时Catalog不可见；本change允许Catalog与Details同时可见，但Catalog仍是唯一数据目录，Details不得复制Catalog。
- active `graph-authoring-editor-shell`只规定Shell拥有Inspector宿主与diagnostics overlay，没有规定三栏和Bottom Dock；本change只扩展通用workspace regions，不让Shell理解动画或BTSMTL领域数据。
- active `character-presentation-pose-graph`已经要求Preview、Runtime与Live Debug复用同一Pose Plan；本change增加可操作的Preview/Pose Watch工作区，但不得重新求值图或创建第二条预览链。
- current `character-animation-presentation-authoring`仍含旧PoseSlot与全局Blend Library口径，已由现有active animation changes负责删除。本change依赖其目标口径，不恢复这些过期概念，也不在本change重复迁移runtime数据。


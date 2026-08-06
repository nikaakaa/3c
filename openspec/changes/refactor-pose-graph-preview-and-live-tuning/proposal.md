# Change: 收口Pose Graph可运行作者闭环与实时调参

## Why

BTSMTL逻辑层已经形成可用的小蓝图编辑体验：节点创建、端口连线、StateMachine下钻、selection、Undo、Details与运行高亮都有正式实现。Pose Graph应复用这些图编辑能力，但它不是BTSMTL逻辑图，也不应迫使Tree与AI窗口改造成动画工作区。

当前Pose Graph的问题不是缺少更多页签，而是最基本的作者闭环没有成立：正式入口被Action Animation工作区干扰；窗口把Preview、Live、Diagnostics和全量参数拆成多个表面；角色画面、当前图、选中节点与实际运行值没有形成同一上下文；现有BTSMTL节点和端口视觉资产没有稳定加载；Corin Preview仍可能因Definition、Profile、Projection、Rig或Fixture不匹配而失败。作者无法完成“打开现有图、看到角色、驱动状态机、定位当前节点、修改参数、观察结果、显式发布”这一条连续操作。

现有实现已经完成大部分实时调参底层，包括字段策略、固定Tuning Layout、默认参数块、Editor candidate、表现帧开始处原子交换以及Foot Placement、Full Body IK、Blend和Sequence的部分consumer接入。这些能力必须保留，但不能再以全局参数表或独立Live窗口暴露。它们应只服务当前PoseGraph selection和当前精确目标。

本change因此停止扩张通用BTSMTL Shell，改为先把现有`CharacterPresentationPoseGraphEditorWindow`做到可运行、可定位、可编辑、可实时调参。UE Animation Blueprint只作为动画作者操作顺序与空间关系参考；具体窗口停靠、Pose Watch、资源浏览器和辅助诊断不作为第一完成线。

## What Changes

- 保持BTSMTL逻辑图与AI图现有窗口、Data Catalog、Inspector、StateMachine和Live Debug体验不变。共享范围收敛为Graph document adapter、Canvas、Node/Port/Edge视觉、selection、创建菜单、clipboard、Undo、breadcrumb、StateMachine表面与Details宿主，不再要求所有domain共用Pose Preview布局。
- 保留现有`CharacterPresentationPoseGraphEditorWindow`作为唯一PoseGraph编辑器，并让它组合BTSMTL共享图编辑内核与PoseGraph专属动画工作区。不得新建Action Animation版PoseGraph、第二GraphView、第二selection或第二Mutation链。
- 正式可运行入口固定为`CharacterAnimationPresentationProfile -> Open Pose Graph`。入口必须一次携带精确Definition、Profile、PoseGraph、Projection、Rig、Source Binding与Preview Fixture上下文；不得先解析Action、Timeline、call site或Slot，也不得按名称、场景顺序或上次选择猜测上下文。
- PoseGraph工作区第一闭环只要求以下长期必要功能，区域位置和停靠方式不成为完成条件：
  - 显式Validate/Compile/Character Build、Dirty/Invalid/Stale/Ready状态、目标选择与播放控制；
  - 正式角色画面；
  - Root Graph、root-owned子图与PoseStateMachine导航；
  - 唯一BTSMTL Graph Canvas；
  - 当前Node、State或Transition的Details；
  - Preview-only typed输入与当前精确目标状态。
- 保留现有PoseGraph资产、root-owned flat graph catalog、节点、端口、edge、StateMachine、Transition、Source Slot和layout数据。UI重构不得迁移为新资产、复制拓扑或改变现有编译语义。
- PoseGraph Canvas必须恢复完整BTSMTL编辑体验：节点和typed端口可读，打开页面自动定位内容；支持创建、移动、连接、断开、删除、复制粘贴、Undo/Redo、端口兼容过滤、StateMachine/State/子图下钻和breadcrumb返回。Transition只作为StateMachine edge及其selection Details存在。
- 角色画面必须运行正式链路：Preview typed输入生成正式Presentation Facts和Parameter输入，随后执行当前发布Projection的同一Pose Plan、source backend、PoseStateMachine、AnimationSlot、Foot Placement、Full Body IK与FinalPublication。不得直接播放Clip、创建shadow solver、临时编译Plan或使用场景Host fallback。
- Preview目标使用与当前Definition/Profile/PoseGraph/Projection/Rig精确匹配的editor-only Fixture；Live目标只来自RuntimeDebugSession精确匹配的当前Actor。目标失配时必须清空旧运行值和高亮并显示原因，不得自动改选其它Actor。
- 当前State、Transition和Pose节点执行状态必须叠加在同一作者图上。运行视图不得创建第二张Live Graph，也不得从Animancer权重或作者默认值重建运行事实。
- Details只显示当前selection真正拥有的作者字段、运行输入、当前目标Applied值、引用和错误。`TunableDefault`通过正式typed Mutation保存作者owner并进入Undo，再通过现有完整candidate链应用到当前目标；`RuntimeInput`只读；`Structural`明确显示需要Build。删除全局Tuning Layout参数表和Profile类型专用热调入口。
- `Compile`只调用PoseGraph现有轻量校验与编译入口；`Character Build`只在作者明确点击时发布Program、Projection、Pose Plan、Tuning Layout与默认参数块。打开窗口、选择节点、修改字段、切换目标、播放、asset import或domain reload均不得自动Build。
- 删除错误的Action Animation PoseGraph验收路径、重复Preview/Live/Diagnostics Dock、裸Preview Target ObjectField、窗口私有Graph副本、Unity默认GraphView节点视觉和任何兼容开关。

## First Usable Closure

第一完成线固定为：

1. 从Corin Presentation Profile打开现有PoseGraph，并获得唯一精确上下文。
2. 中央显示现有Root Pose Graph，节点、端口和连线可读且可编辑。
3. Corin Preview Fixture在隔离PreviewScene中运行当前发布的正式Pose Plan，Console没有Projection或Rig上下文异常。
4. Preview typed输入可以驱动Idle、Walk、Run与Turn等现有PoseState切换。
5. 当前State、Transition和执行节点在同一作者图上高亮。
6. 选择现有Foot Placement、Full Body IK、Blend或Inertialization节点后，Details显示作者值、当前目标Applied值与应用语义。
7. 修改支持实时调整的字段后，正式作者值被保存，当前Preview或唯一Live Actor按Next Frame或Next Activation采用新值；Structural字段保持Build Required。
8. Compile错误能够定位到对应Node、Port、State或Transition；Character Build只由明确按钮执行。

第一完成线不要求自由停靠、固定Pose Watch面板、Asset Browser、Trace页签、永久Search Results、Preview Scene Settings或Action Animation联动。这些能力以后若确有作者价值，必须在该闭环稳定后独立规划，不能先占据主工作流。

## Impact

- 继续新增capability：`character-animation-live-tuning`。
- 修改`graph-authoring-editor-shell`、`graph-authoring-domain-framework`、`character-animation-presentation-authoring`、`character-presentation-pose-graph`与`character-animation-pipeline`。
- 影响现有PoseGraph窗口组合、正式入口、BTSMTL视觉资源加载、PoseGraph Canvas交互、Preview Fixture、Runtime target binding、selection Details、运行高亮、Compile/Build状态与Live Tuning UI接线。
- 保留已经实现的Tuning Layout、Parameter Block、candidate compiler、frame-boundary swap和现有runtime consumer；不重写FinalIK、Foot Placement、Blend、PoseStateMachine、Animancer、Projection或Presentation事务算法。
- 不修改BTSMTL逻辑图数据、AI图数据、Data Catalog、Gameplay StateMachine、Action Animation职责、Gameplay Program、Rollback、Network Model或Player写入口。

## 与Current Spec及Active Change对比

- current `graph-authoring-editor-shell`要求Tree、AI与PoseGraph共享同一五区Shell，并禁止Pose专属工作区。该约束与“BTSMTL逻辑编辑体验保持不变、PoseGraph先完成动画闭环”冲突。本change修改为共享唯一图交互内核，但允许现有PoseGraph窗口组合动画专属区域；共享的是Canvas和交互，不是整套窗口布局。
- current `graph-authoring-domain-framework`正确要求从现有BTSMTL作者UI原地抽象并保留BTSMTL行为。本change强化这条边界：PoseGraph必须复用BTSMTL Node/Port/Edge视觉和交互，但不得反向改变BTSMTL Data Catalog、Tree Inspector或逻辑图布局。
- current `character-presentation-pose-graph`已经规定唯一Graph Canvas、正式Mutation、精确revision和同一Pose Plan Preview，但当前UI仍把运行输入、全量调参和diagnostics分散。本change把这些合同收敛到现有PoseGraph窗口、当前selection和当前精确目标。
- current `character-animation-presentation-authoring`已经规定Profile是唯一Presentation配置入口、PoseGraph Navigator需要显式Definition上下文、所有重操作必须明确触发。本change把`Open Pose Graph`升级为唯一可运行入口，并明确Action Animation不得成为前置路径。
- current `character-action-animation-authoring-workspace`只负责有限Action、Timeline、AnimationSlot与运行Trace关系。本change不修改其业务能力，只删除它作为PoseGraph主入口或验收路径的错误接线。
- active `replace-pose-ik-with-finalik-full-body-solver`继续拥有Rig、Foot Goal、Full Body IK solver与Projection迁移。本change只消费其当前正式合同，不修改IK diagnostics或IK Runtime；Corin Preview闭环必须使用与当前发布产物精确一致的上下文。
- active Blend Space、Motion Matching、Linked Pose与Secondary Motion change继续拥有各自数据和算法。本change不为它们增加专用Preview或热调窗口；只有已经正式接入当前PoseGraph的能力进入第一闭环。

## Hard Stop Gates

实施必须在以下边界内停止并报告，不得绕过：

1. 若现有PoseGraph无法通过精确Definition/Profile上下文运行，必须修复正式入口或发布产物，不得搜索场景Host、按名称猜测或回到Action Animation。
2. 若Preview无法执行正式Projection与Pose Plan，必须修复Fixture或runtime adapter，不得直接播放Clip、创建第二PlayableGraph、第二solver或假地面。
3. 若BTSMTL视觉和交互只能通过复制BaseTreeWindow或修改Tree/AI体验获得，必须先提取domain-neutral graph primitive，不得把Pose布局强加给BTSMTL。
4. 若运行时调参需要读取ScriptableObject、直接修改FinalIK组件、重建Rig/source/solver或改变workspace容量，该字段必须保持Structural并要求显式Build。
5. 若Live target无法通过RuntimeDebugSession精确匹配，必须显示Unavailable，不得按名称、场景顺序或旧对象引用选择。
6. 若任何操作会自动触发Program、Projection、Foot Analysis或Motion Matching Database构建，必须停止并改为明确命令。

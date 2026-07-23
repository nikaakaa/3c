# 实施清单

## 实施基线

- 共享工作区入口：`GraphAuthoringEditorShell`读取`BaseTreeWindow.uxml`和`BaseTreeWindow.uss`，通过`GraphAuthoringDomainAdapters`装配document、node catalog、port policy、mutation、Inspector、diagnostics、Navigator、Bottom Dock和Toolbar命令。窗口创建、document重绑、刷新和清理由Shell统一驱动。
- BTSMTL入口：`BaseTreeWindow`持有唯一`BaseTreeView`、`TreeWindowNavigationController`和`TreeWindowRuntimeOverlayController`。`BtsmtlGraphAuthoringNavigatorAdapter`装配唯一Graph Data Catalog，`BtsmtlGraphAuthoringInspectorAdapter`装配selection Details。旧Data/Inspector互斥页签、页签命令和左栏Inspector装配已删除。
- AI入口：`AIControllerTreeWindow`只继承`BaseTreeWindow`并提供`AIControllerTreeInspectorView`；`AIControllerAuthoringContext`仍是AI数据、节点命令和Inspector的唯一上下文。AI没有第二个GraphView或独立Workspace。
- Pose Graph入口：`CharacterPresentationPoseGraphEditorWindow`继续使用唯一document、node catalog、port policy、mutation、Inspector和diagnostics adapter，并只额外提供Pose Navigator与Bottom Dock。
- Pose上下文：窗口显式保存Profile、Rig、Projection和Definition authoring context；runtime target只从`RuntimeDebugSession`的稳定Character runtime identity解析，Preview Target只由用户在当前窗口显式选择。
- Producer发现：`CharacterAnimationPresentationAuthoringService`只从精确Definition composition roots递归发现Timeline/AnimationTrack producer，并返回稳定Timeline、Track、Clip和producer identity。
- Timeline导航：跨资产定位统一调用`RuntimeDebugSourceNavigator.Open`，使用TimelineAuthoringId、TrackAuthoringId和Clip identity，不按显示名或数组index猜测。
- Runtime诊断：`RuntimeDebugSession`继续拥有共享target、current snapshot和Capture history；每个TreeWindow只持有自己的Graph binding、Follow/Pin和interest。
- Pose runtime snapshot：正式snapshot提供Projection/PoseGraph revision、completion、operation trace、source contribution、lifecycle、BlendStack、Inertialization、最终availability和新增的固定容量Pose Watch页。
- Preview执行：`CharacterPipelineHost -> CharacterPipelinePreviewController -> PreviewPlaybackEngine`复用正式Projection、Pose Plan、source sampling、PlayableGraph和snapshot publisher；没有简化Player、临时Pose Graph或Animancer direct Play路径。
- 已安装依赖：最终Pose Graph node set、Selection边界、显式BlendStack和node-local Inertialization均已落地；工作区不读取旧PoseSlot、旧Blend Library、旧snapshot或图外MarkerSync/FootPlacement字段。

## 最终工作区装配

- Shell区域固定为Toolbar、Navigator、唯一Graph Canvas、Details和可选Bottom Dock。区域描述只包含标题、可见性、最小尺寸、默认尺寸和默认折叠状态，不包含BTSMTL、AI或动画领域DTO。
- Navigator、Details和Bottom Dock使用嵌套`TwoPaneSplitView`。Graph Canvas在其它区域折叠后占满剩余空间；窄窗口按Bottom Dock、Navigator、Details顺序确定性折叠。
- `GraphAuthoringWorkspaceLayoutState`只序列化在EditorWindow，保存三块尺寸/折叠、Details页和Bottom页。它不调用Undo、不标记资产dirty，也不修改任何content revision。
- `GraphDataCatalogViewState`只序列化在`BaseTreeWindow`，保存搜索、source、scope、context、Blackboard筛选展开、分组折叠和条目展开。selection刷新和Graph下钻只重建目录投影，不重置这些状态。
- Pose Navigator的搜索以及Pose Watch的identity、颜色、显隐、骨骼过滤、viewport相机/网格/显示选项均只属于Pose Graph EditorWindow。

## Pose Graph作者链路

- Navigator要求显式Definition，按AnimationChannel投影可达Timeline producer，显示Timeline、Track、Clip、source binding和Sync摘要，并提供精确owner导航。缺少Definition时显示Unavailable，不扫描Assets或generated产物。
- Details分为Authoring、Live、References。Authoring只经Pose mutation adapter写当前节点正式字段；Live只读取revision匹配的正式snapshot，并直接投影MarkerSync relation/playback、Player lifecycle/release、BlendStack entry和world-aware stage completion；References只读source map、call-site、producer、Profile、Rig、Mask和Policy owner。
- 画布显示集中式UE对应显示名、阶段角标、Sync Group、正式operation trace、availability、weight、source contribution、call-site和completion。Live Debug下Graph、Navigator和Details统一只读。
- Toolbar的Compile/Build只调用现有正式Build服务；窗口打开、selection、Inspector、Graph mutation、Preview target、domain reload和AssetDatabase refresh均不会自动构建。

## Preview、Pose Watch、Diagnostics与Sync

- Bottom Dock包含Preview、Diagnostics、Sync和Pose Watch四页，页签只改变窗口view-state，不修改runtime binding或Capture history。
- Preview必须显式选择场景`CharacterPipelineHost`并由Play、Pause、Step、Seek、Reset推进。Graph mutation、target切换和document切换会终止旧session；缺少world context时发布正式`WorldContextUnavailable`，不伪造Final Pose。
- Preview viewport使用隐藏Editor Camera直接只读观察选中场景角色，不克隆角色、不改层级、Transform、Animator、Graph或Rig。骨架来自正式RigBinding；VisualRoot轨迹有固定256点窗口容量；Foot support/IK没有正式坐标时明确显示Unavailable。
- Pose Watch identity是PoseGraphId、PoseGraphRevision、PoseNodeId和call-site。runtime与Preview都从同一已完成Pose Plan workspace复制固定容量Pose和contribution；每窗口8个、每target 16个；NoPose、Invalid、NotCompleted和Stale不会泄漏旧Pose。
- 同target多窗口interest由snapshot publisher按owner合并；关闭窗口、target/document切换和取消watch会释放owner interest。Watch不会触发第二次source sample、第二次PlayableGraph Evaluate或修改Player/BlendStack/Inertialization/Final Pose。
- Diagnostics显示唯一validator、Dirty/Stale/Ready、runtime invalid reason并可按source map定位节点。Sync只读显示正式MarkerSync producer identity、时间、cycle、marker pair、fraction、relation和Marker ruler；修改Marker仍只允许进入Timeline Editor。

## 清理与Agent边界

- 已删除旧固定两栏UXML、`left-panel`/`right-panel`查询、Data/Inspector互斥页签、页签切换命令、旧Inspector宽度假设和`ShowDataTab`空桥接。
- Tree、AI和Pose Graph都只装配共享Shell；Timeline仍是独立且唯一的时间轴写入口。
- Agent Snapshot、Document schema、Mutation Compiler、Validator和MCP bridge未增加任何Workspace布局、页签、Pose Watch或Preview字段，schema和operation catalog保持不变。

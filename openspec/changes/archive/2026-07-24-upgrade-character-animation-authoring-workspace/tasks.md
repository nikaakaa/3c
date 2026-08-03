## 1. 依赖与实施基线

- [x] 1.1 记录`GraphAuthoringEditorShell`当前UXML、USS、窗口生命周期和domain adapter装配点。
- [x] 1.2 记录BTSMTL TreeWindow当前Data/Inspector互斥页签、selection、navigation和Live Debug owner。
- [x] 1.3 记录AIController窗口复用BaseTreeWindow与Inspector context的全部入口。
- [x] 1.4 记录Pose Graph窗口当前document、node catalog、port policy、mutation、Inspector和diagnostics adapter。
- [x] 1.5 记录Pose Graph当前Profile context、Rig context、ProjectionRevision和runtime target解析入口。
- [x] 1.6 记录`CharacterAnimationPresentationAuthoringService`当前Definition-scoped producer发现与source navigation能力。
- [x] 1.7 记录Timeline Editor按stable Timeline/Track/Clip identity定位对象的正式入口。
- [x] 1.8 记录RuntimeDebugSession当前window-local binding、interest、Capture history和target切换合同。
- [x] 1.9 记录Pose runtime snapshot当前node trace、source contribution、MarkerSync与completion字段。
- [x] 1.10 记录PreviewPlaybackEngine、Timeline Preview和Motion Matching Query Fixture的正式Pose Plan执行入口。
- [x] 1.11 确认`add-character-presentation-pose-graph`已经安装唯一共享Shell与最终Pose Graph node set。
- [x] 1.12 确认`refactor-animation-selection-pose-graph-boundary`已经删除隐藏PoseSlot、图外MarkerSync和图外FootPlacement路径。
- [x] 1.13 确认BlendStack与Inertialization active change已经收口各自唯一runtime owner。
- [x] 1.14 标记仍属于active change的临时字段、旧PoseSlot、旧Blend Library和旧snapshot字段，禁止工作区引用。
- [x] 1.15 对照current specs列出Data/Inspector互斥页签、Catalog隐藏和旧左栏宽度的全部实现点。

## 2. 共享Workspace区域合同

- [x] 2.1 定义通用Toolbar区域宿主合同。
- [x] 2.2 定义通用Navigator区域宿主合同。
- [x] 2.3 将现有GraphView宿主明确为唯一Graph Canvas区域。
- [x] 2.4 将现有`IGraphAuthoringInspectorAdapter`明确挂载到右侧Details区域。
- [x] 2.5 定义可选Bottom Dock区域宿主合同。
- [x] 2.6 定义domain对区域可见性、标题、最小尺寸和默认折叠状态的只读描述。
- [x] 2.7 定义domain toolbar command descriptor并区分轻量命令与显式重操作。
- [x] 2.8 定义domain navigator adapter的context绑定、清理和refresh生命周期。
- [x] 2.9 定义domain bottom-panel adapter的context绑定、清理和refresh生命周期。
- [x] 2.10 保持document、node catalog、port policy、mutation、Inspector和diagnostics现有职责不变。
- [x] 2.11 禁止Shell contract引用BaseNode、Blackboard、PoseNode、AnimationChannel或Runtime Trace DTO。
- [x] 2.12 禁止任一区域宿主持有第二份node、edge、selection或dirty owner。

## 3. 共享Workspace布局

- [x] 3.1 将`BaseTreeWindow.uxml`替换为Toolbar、Navigator、Graph Canvas、Details和Bottom Dock区域结构。
- [x] 3.2 使用可嵌套SplitView实现Navigator与主内容水平分栏。
- [x] 3.3 使用可嵌套SplitView实现Graph Canvas与Details水平分栏。
- [x] 3.4 使用可嵌套SplitView实现Graph Canvas与Bottom Dock垂直分栏。
- [x] 3.5 为Navigator设置合理默认宽度和最小宽度。
- [x] 3.6 为Details设置合理默认宽度和最小宽度。
- [x] 3.7 为Bottom Dock设置合理默认高度和最小高度。
- [x] 3.8 保持Graph Canvas在其它区域折叠后自动占满剩余空间。
- [x] 3.9 将breadcrumb和Back导航保留在Graph Canvas顶部。
- [x] 3.10 将window mode、显式命令和状态摘要放入Toolbar。
- [x] 3.11 为各区域添加统一边框、标题栏、tab和空状态视觉语法。
- [x] 3.12 删除旧固定`left-panel`和`right-panel`尺寸假设。
- [x] 3.13 删除旧Inspector只能挂到左栏的装配逻辑。
- [x] 3.14 保持当前Unity版本支持的USS selector集合。

## 4. Editor-only布局状态

- [x] 4.1 定义window-local workspace layout state。
- [x] 4.2 保存Navigator宽度和折叠状态。
- [x] 4.3 保存Details宽度、折叠状态和当前Details页签。
- [x] 4.4 保存Bottom Dock高度、折叠状态和当前面板页签。
- [x] 4.5 保存Navigator搜索、分组和当前数据来源筛选。
- [x] 4.6 保存Pose Watch颜色、显隐和选中节点集合。
- [x] 4.7 确保layout state不进入任何Graph、Timeline、Profile或Definition序列化。
- [x] 4.8 确保layout state变化不触发Undo、dirty或content revision。
- [x] 4.9 在domain reload后只恢复layout state和稳定authoring locator。
- [x] 4.10 禁止恢复旧runtime instance、旧Preview session或旧object reference。
- [x] 4.11 为窄窗口定义确定性的Navigator、Details和Bottom Dock折叠顺序。
- [x] 4.12 删除旧Data/Inspector页签view-state字段和迁移代码。

## 5. BTSMTL Tree Workspace迁移

- [x] 5.1 将唯一Graph Data Catalog挂载到左侧Navigator。
- [x] 5.2 将Node/Edge/Graph selection Inspector挂载到右侧Details。
- [x] 5.3 删除Data与Inspector互斥页签控件。
- [x] 5.4 删除切换页签时启停Catalog命令的旧逻辑。
- [x] 5.5 保持Catalog source、search、scope、context和foldout状态独立于selection。
- [x] 5.6 保持Details只投影当前Node/Edge或Graph Authoring Settings。
- [x] 5.7 保持Catalog与Details不共享可写状态。
- [x] 5.8 将Authoring/Live Debug模式控制迁移到Toolbar窗口级入口。
- [x] 5.9 在Live Debug下统一禁用Navigator与Details中的authoring命令。
- [x] 5.10 保持Graph runtime binding只属于当前TreeWindow。
- [x] 5.11 保持Timeline窗口binding不受Tree selection和Navigator操作影响。
- [x] 5.12 保持inline/shared Graph下钻时Catalog按当前context重建。
- [x] 5.13 保持Transition rule selection的Catalog能力和Details owner正确切换。
- [x] 5.14 删除旧左侧Inspector视觉树和重复Graph Settings投影。

## 6. AI Graph Workspace迁移

- [x] 6.1 将AI Data Catalog装配到共享Navigator区域。
- [x] 6.2 将AI Node/Edge/Graph Inspector装配到共享Details区域。
- [x] 6.3 保持AIControllerAuthoringContext是AI数据和命令的唯一上下文。
- [x] 6.4 保持AI窗口复用共享GraphView而不创建第二画布。
- [x] 6.5 保持AI runtime overlay使用共享RuntimeDebugSession和window-local binding。
- [x] 6.6 删除AI窗口对旧left-panel和Data/Inspector页签的依赖。
- [x] 6.7 禁止共享Shell新增AI candidate、observation、memory或intent字段判断。
- [x] 6.8 保持AI mutation、compiler和Agent authoring语义不变。

## 7. Pose Graph Navigator

- [x] 7.1 定义Pose Graph Navigator根模型。
- [x] 7.2 投影当前root graph和breadcrumb page stack。
- [x] 7.3 投影inline与shared PoseSubgraph稳定identity。
- [x] 7.4 投影AnimationSelectionInput及其AnimationChannelId。
- [x] 7.5 投影MotionMatchingSelectionInput及其producer output identity。
- [x] 7.6 投影ProgramParameterInput和Graph参数声明。
- [x] 7.7 投影当前图引用的Rig、Bone Mask、Blend Policy和Inertialization Policy。
- [x] 7.8 要求精确CharacterPipelineDefinition context后再创建producer目录。
- [x] 7.9 复用`CharacterAnimationPresentationAuthoringService`发现可达Timeline与AnimationTrack。
- [x] 7.10 按AnimationChannelId分组stable producer条目。
- [x] 7.11 为producer显示Timeline、Track、Clip、source binding和Sync模式摘要。
- [x] 7.12 为producer提供精确Open Timeline与Select Track操作。
- [x] 7.13 为Profile、Rig、Mask与Policy提供精确owner定位操作。
- [x] 7.14 在shared Pose Graph缺少Definition call-site context时显示Unavailable。
- [x] 7.15 禁止使用上一次窗口Definition作为shared graph fallback。
- [x] 7.16 禁止Navigator扫描Assets目录或读取generated Program/Projection完成bootstrap。
- [x] 7.17 禁止producer条目创建、移动或修改Timeline数据。
- [x] 7.18 确保搜索、分组和展开只修改editor view-state。

## 8. Pose Graph Details

- [x] 8.1 建立`Authoring`、`Live`和`References`三个Details页签。
- [x] 8.2 将现有Pose Inspector内容迁入Authoring页。
- [x] 8.3 按Node Kind只生成当前节点拥有的正式字段。
- [x] 8.4 删除无关节点显示的serialized默认字段。
- [x] 8.5 保持全部Authoring mutation通过唯一Pose mutation adapter。
- [x] 8.6 保持inline与shared subgraph mutation使用精确dirty owner。
- [x] 8.7 为Selection节点显示AnimationChannel、availability和正式binding状态。
- [x] 8.8 为SelectedPosePlayer显示source policy和continuity输出合同。
- [x] 8.9 为BlendStack显示node-local Policy与可达endpoint状态。
- [x] 8.10 为Inertialization显示node-local Policy与Rig revision。
- [x] 8.11 为LayeredBoneBlend显示Mask、Rig identity和输入语义。
- [x] 8.12 为ProgramParameterInput显示ParameterId、type和default。
- [x] 8.13 为ModifyBone显示BoneId、space和operation字段。
- [x] 8.14 为FootPlacement显示Profile、Calibration、weight输入和world-aware阶段说明。
- [x] 8.15 为OutputPose显示唯一输出和阶段completion要求。
- [x] 8.16 将现有runtime snapshot绑定迁入Live页。
- [x] 8.17 在Live页显示availability、weight、source contribution和invalid reason。
- [x] 8.18 在MarkerSync Live页显示source/target、raw/effective time、cycle、marker pair和fraction。
- [x] 8.19 在Player Live页显示current source usage、sample time、play rate和release状态。
- [x] 8.20 在BlendStack Live页显示entry、weight、clock、stored与retirement状态。
- [x] 8.21 在world-aware节点Live页显示stage completion与solver状态。
- [x] 8.22 在References页显示graph source map、call-site与compiled operation identity。
- [x] 8.23 在References页显示reachable producer与精确Timeline owner。
- [x] 8.24 在References页显示Profile、Rig、Mask与Policy owner。
- [x] 8.25 对PoseGraph或Projection revision不匹配统一显示Stale并清空旧值。
- [x] 8.26 禁止Live和References页执行mutation或runtime重新求值。

## 9. Pose Graph画布可视化

- [x] 9.1 为节点标题增加语义准确的显示名层，不修改Node Kind与identity。
- [x] 9.2 为节点增加Selection、Source/Pose、World-Aware和Output阶段角标。
- [x] 9.3 为MarkerSync或相关Selection链显示Sync Group水印。
- [x] 9.4 为Player显示当前source和sample time摘要。
- [x] 9.5 为Blend与BlendStack显示当前输入权重摘要。
- [x] 9.6 为Optional Pose显示NoPose、Pose与Invalid availability。
- [x] 9.7 为OutputPose显示completion identity和最终availability。
- [x] 9.8 使用正式operation trace驱动节点执行高亮。
- [x] 9.9 使用正式source contribution驱动edge weight显示。
- [x] 9.10 对多call-site subgraph显示call-site区分信息。
- [x] 9.11 在Authoring模式隐藏无匹配snapshot的旧Live值。
- [x] 9.12 在Live Debug模式禁止节点、edge和配置mutation。
- [x] 9.13 禁止画布从Graph拓扑计算伪weight、伪source或伪completion。
- [x] 9.14 禁止将AnimationChannel显示为Montage Slot。
- [x] 9.15 禁止显示不存在的Animation State Machine或Post Process Anim Blueprint。

## 10. 显式Pose Preview会话

- [x] 10.1 定义Pose Graph Authoring Preview的显式上下文合同。
- [x] 10.2 要求Preview context携带精确CharacterPipelineDefinition identity。
- [x] 10.3 要求Preview context携带合法Rig/Preview Target identity。
- [x] 10.4 要求Preview context携带匹配PoseGraph和Projection revision。
- [x] 10.5 复用正式Presentation Runtime Factory或既有正式Preview host创建Pose Plan执行实例。
- [x] 10.6 禁止创建简化Player、固定Stack、临时Pose Graph或Animancer direct Play路径。
- [x] 10.7 增加显式Play命令。
- [x] 10.8 增加显式Pause命令。
- [x] 10.9 增加显式Step命令。
- [x] 10.10 增加显式Seek命令。
- [x] 10.11 增加显式Reset命令并复用正式reset语义。
- [x] 10.12 selection变化时只更新Details，不自动推进Preview。
- [x] 10.13 Preview target变化时停止旧session并等待显式Play。
- [x] 10.14 Graph mutation时停止Preview并标记Stale。
- [x] 10.15 Projection revision变化时拒绝继续使用旧Plan。
- [x] 10.16 Preview缺少world context时发布world-aware Unavailable。
- [x] 10.17 禁止跳过FootPlacement后伪造FinalAnimationPoseFrame。
- [x] 10.18 关闭窗口或切换document时按唯一owner顺序释放Preview资源。
- [x] 10.19 禁止窗口恢复、domain reload或AssetDatabase事件自动重建Preview session。

## 11. Preview Viewport

- [x] 11.1 在Bottom Dock建立Pose Preview viewport宿主。
- [x] 11.2 显示明确的Preview Target、PoseGraph revision和Projection revision。
- [x] 11.3 显示播放时间、frame、speed与播放状态。
- [x] 11.4 显示角色骨架开关。
- [x] 11.5 显示Root motion或visual root轨迹开关。
- [x] 11.6 显示Foot Placement current/future support与IK goal开关。
- [x] 11.7 显示world-aware阶段Unavailable或Invalid原因。
- [x] 11.8 保持viewport相机、网格和显示选项为editor-only状态。
- [x] 11.9 禁止viewport直接修改场景runtime target、Graph资产或Rig资产。

## 12. Pose Watch合同与发布

- [x] 12.1 定义window-local Pose Watch identity。
- [x] 12.2 将identity绑定到PoseGraphId、PoseGraphRevision、PoseNodeId与call-site。
- [x] 12.3 定义Pose Watch颜色、显隐和骨骼显示过滤的editor-only模型。
- [x] 12.4 定义RuntimeDebugSession的Pose Watch interest请求。
- [x] 12.5 定义同target多窗口interest合并规则。
- [x] 12.6 定义每窗口、每target和每frame的固定watch容量。
- [x] 12.7 定义每个watch的固定骨骼容量与buffer布局。
- [x] 12.8 从compiled source map解析PoseNodeId到Pose Value workspace index。
- [x] 12.9 只在对应Pose Plan completion成功后复制watch pose。
- [x] 12.10 在NoPose、Invalid或阶段未完成时发布typed availability而非旧Pose。
- [x] 12.11 禁止Pose Watch触发第二次source sampling。
- [x] 12.12 禁止Pose Watch触发第二次PlayableGraph Evaluate。
- [x] 12.13 禁止Pose Watch修改Player、BlendStack或Inertialization状态。
- [x] 12.14 禁止Pose Watch改变FinalAnimationPoseFrame与Foot Placement结果。
- [x] 12.15 为Preview session接入同一watch发布合同。
- [x] 12.16 为Live runtime target接入同一watch发布合同。
- [x] 12.17 在窗口关闭、target切换、document切换和interest取消时释放watch资源。
- [x] 12.18 禁止无界保留watch历史或把watch写入runtime authoring资产。

## 13. Pose Watch面板

- [x] 13.1 在Pose节点context menu增加Toggle Pose Watch命令。
- [x] 13.2 只允许拥有Pose输出的合法节点启用Pose Watch。
- [x] 13.3 在Bottom Dock建立Pose Watch Manager。
- [x] 13.4 显示每个watch的节点、call-site、颜色、availability和completion。
- [x] 13.5 支持显式显示或隐藏单个watch。
- [x] 13.6 支持显式修改watch颜色。
- [x] 13.7 支持从watch条目定位Graph节点。
- [x] 13.8 支持从Graph节点定位watch条目。
- [x] 13.9 在Preview viewport绘制多个颜色区分的只读中间Pose。
- [x] 13.10 在watch超出固定容量时拒绝新增并显示明确容量错误。
- [x] 13.11 在revision mismatch时清空旧watch frame并显示Stale。

## 14. Diagnostics与Sync Bottom Dock

- [x] 14.1 将Pose Graph validation issues投影到Diagnostics面板。
- [x] 14.2 将Compile/Projection Stale状态投影到Diagnostics面板。
- [x] 14.3 将runtime snapshot invalid reason投影到Diagnostics面板。
- [x] 14.4 将MarkerSync relation和playback snapshot投影到Sync面板。
- [x] 14.5 在Sync面板显示source与target producer identity。
- [x] 14.6 在Sync面板显示duration、raw time、effective time与cycle。
- [x] 14.7 在Sync面板显示previous/next MarkerId与segment fraction。
- [x] 14.8 在Sync面板绘制当前与目标producer的只读Marker时间尺。
- [x] 14.9 从formal authoring service解析时间尺对应Timeline/Track stable identity。
- [x] 14.10 提供Open Source Timeline并选择精确Track的导航命令。
- [x] 14.11 禁止Sync面板拖动、创建、删除或重命名Marker。
- [x] 14.12 禁止Sync面板写入Profile、Projection或Pose Graph Marker副本。
- [x] 14.13 点击diagnostic issue时按source map定位Graph node或精确source owner。
- [x] 14.14 保持Diagnostics和Sync页签切换不修改runtime binding或Capture history。

## 15. 显式Compile与状态模型

- [x] 15.1 定义Dirty、Invalid、Stale、Ready和Building状态模型。
- [x] 15.2 从真实Graph dirty与content revision解析Dirty。
- [x] 15.3 从唯一Pose Graph validator解析Invalid。
- [x] 15.4 从PoseGraph、Profile、Rig与Projection revision解析Stale。
- [x] 15.5 从匹配已发布产物解析Ready。
- [x] 15.6 只在显式正式Build事务期间显示Building。
- [x] 15.7 在Toolbar增加显式Compile/Build命令入口。
- [x] 15.8 让命令只调用现有Character Definition正式Build服务。
- [x] 15.9 禁止Shell或Pose Graph Editor复制Build Transaction与SaveAssets逻辑。
- [x] 15.10 禁止asset selection触发Build。
- [x] 15.11 禁止Inspector focus或字段提交后自动Build。
- [x] 15.12 禁止Graph mutation后自动Build。
- [x] 15.13 禁止窗口创建、恢复或domain reload自动Build。
- [x] 15.14 禁止AssetDatabase import/refresh自动Build。
- [x] 15.15 禁止Preview target切换自动Build。
- [x] 15.16 保留轻量validation刷新且不发布generated产物。

## 16. 唯一作者入口与导航

- [x] 16.1 保持Pose节点配置只通过Pose mutation adapter修改。
- [x] 16.2 保持AnimationTrack Clip、Marker和Curve只通过Timeline正式API修改。
- [x] 16.3 保持Presentation Profile只通过Profile Inspector和正式authoring service修改。
- [x] 16.4 保持Rig、Mask、Blend Policy和Inertialization Policy由各自正式owner修改。
- [x] 16.5 保持Foot Analysis generated data只读且只由显式Build生成。
- [x] 16.6 保持Motion Matching Database只由显式Database Build生成。
- [x] 16.7 为所有跨资产条目使用stable identity导航。
- [x] 16.8 禁止按显示名、目录、数组index或当前selection猜测owner。
- [x] 16.9 禁止Pose Graph Workspace直接写跨资产SerializedProperty。
- [x] 16.10 删除任何工作区实现产生的重复Marker、producer、Profile或Rig缓存真相。

## 17. UE术语与帮助信息

- [x] 17.1 建立UI显示名到正式Node Kind的集中映射。
- [x] 17.2 将`LayeredBoneBlend`显示为`Layered Blend Per Bone`并保留正式类型提示。
- [x] 17.3 将MarkerGroup作者概念显示为`Sync Group`。
- [x] 17.4 将ProgramParameterInput显示为`Animation Parameter`并保留ParameterId。
- [x] 17.5 将OutputPose显示为`Output Pose`。
- [x] 17.6 将PoseSubgraph显示为`Pose Subgraph`并说明与Anim Layer的对应边界。
- [x] 17.7 为AnimationChannel显示正式类型和UE概念说明，禁止称为Slot。
- [x] 17.8 为BTSMTL Timeline显示正式类型和UE Montage概念差异，禁止改名。
- [x] 17.9 为world-aware阶段显示正式阶段名，禁止称为独立Post Process Anim Blueprint。
- [x] 17.10 禁止显示项目未安装的Animation State Machine、Blend Space或Montage authoring入口。
- [x] 17.11 保持所有UI显示名变化不修改serialized identity、node kind、port kind或compiler code。

## 18. 旧路径清理

- [x] 18.1 删除旧固定两栏BaseTreeWindow UXML结构。
- [x] 18.2 删除旧左侧Inspector挂载字段和查询名称。
- [x] 18.3 删除旧Data/Inspector互斥页签控件。
- [x] 18.4 删除旧页签切换命令和view-state。
- [x] 18.5 删除旧Inspector宽度常量和局部样式。
- [x] 18.6 删除Pose Graph重复toolbar status与Bottom Dock重复状态源。
- [x] 18.7 删除Pose Graph专用第二selection projection或临时panel缓存。
- [x] 18.8 删除旧window入口、兼容UXML和layout模式开关。
- [x] 18.9 确认Tree、AI和Pose Graph只装配唯一Workspace Shell。
- [x] 18.10 确认Timeline仍保留独立时间轴编辑器且没有被嵌入第二写入口。

## 19. Agent、规格与文档收口

- [x] 19.1 扫描Agent Snapshot、Document schema、Mutation Compiler、Validator和MCP bridge对旧窗口类型与页签的引用。
- [x] 19.2 将纯UI宿主引用迁移到共享Workspace合同。
- [x] 19.3 确认本change没有增加Agent可见或可写authoring字段。
- [x] 19.4 确认Agent schema版本、Document正文和Patch/MCP operation catalog保持不变。
- [x] 19.5 更新`graph-authoring-editor-shell` current spec为五区域工作区合同。
- [x] 19.6 更新`btsmtl-tree-inspector-information-architecture` current spec并删除互斥页签口径。
- [x] 19.7 更新`btsmtl-graph-data-catalog-authoring` current spec为左侧唯一Data区域口径。
- [x] 19.8 更新`character-presentation-pose-graph` current spec的Details、Preview、Pose Watch和术语边界。
- [x] 19.9 更新`character-animation-presentation-authoring` current spec的Definition-scoped Navigator和唯一写入口。
- [x] 19.10 更新`openspec/project.md`的Editor ownership、显式Build和UE术语边界。
- [x] 19.11 删除文档中把PoseSlot称为正式入口、把world-aware阶段称为Post Process Anim Blueprint或把Timeline称为Montage的过期描述。
- [x] 19.12 运行严格OpenSpec校验并修复全部change格式与跨spec矛盾。

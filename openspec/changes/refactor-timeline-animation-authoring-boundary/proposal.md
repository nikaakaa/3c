# Change: 重构 Timeline Animation 作者界面与领域工具边界

## 动画播放边界重新基线

`refactor-animation-selection-pose-graph-boundary`不改变本change的Timeline Core、typed context、Foot Analysis Artifact和按需领域工具边界，只更新Preview的正式输出链：Timeline采样产生`AnimationSelectionFrame`与表现参数，编译Pose Graph决定使用直接Player、可选局部Inertialization或显式Blend Stack，并在同一Pose Plan中执行可用的FootPlacement阶段。

Timeline不得携带transition、PoseSlot、Blend Stack entry、Inertialization residual或IK状态；Preview也不得后台创建图中不存在的Stack或Inertialization。本文后续出现的Animancer最终姿势、固定Stack或图外Foot Placement描述只代表迁移前基线。

## Why

当前 Foot Placement 运行时已经形成唯一表现链：

```text
Body Presentation
  -> Timeline Animation Selection
  -> compiled Pose Graph Plan
  -> explicit Player / composition
  -> Foot Placement Planner / IK Solver
  -> Camera
```

离线脚分析也已经能够从精确 AnimationClip、Sampling Rig 和 Rig Calibration 生成左右脚速度、高度、plant confidence 与下一落地预测。真正的问题是作者边界：现有实现把这些生成数据作为每条 AnimationTrack 的四组常驻 `FOOT ANALYSIS` lane 展开，并通过 Graph Window 传入 `CharacterPipelineDefinition` 上下文，再从完整 `CharacterPresentationProjection` 读取结果。Timeline 中的 `Rebuild` 还会触发整个 Character Simulation Program 与 Projection 的正式编译。

这导致三个本应分离的职责混在一起：

```text
Timeline 作者内容
  = Clip、Marker、Window、可编辑控制曲线

Animation Foot Analysis
  = AnimationClip + Rig/Calibration -> 生成只读特征

Character Projection Publication
  = 收集角色可达producer并发布Player运行时绑定
```

脚分析确实需要角色 Rig 与 Calibration，但不需要 Tree、StateMachine、Timeline call site 或 Simulation Program。Tree 上下文只对 Marker Group 的跨 producer/call site 校验有意义。当前设计把“Projection 是运行时发布真相”错误扩张成了“局部动画分析也必须依赖完整 Projection”，使独立 Timeline 无法使用、Definition 选择和 Rebuild 过重，并把内部 IK 数据伪装成作者参数。

这不是一个Foot Analysis局部UI修复，而是Timeline Animation作者界面的实现边界问题。Timeline Core当前通过无类型`AuthoringContext`承接Graph、Definition、Projection和领域工具，缺少“本地作者内容、跨producer拓扑、生成分析、Live Debug”四类上下文的正式区分。只删除四条lane而不修这个边界，下一项Distance Matching、Motion Analysis或其它领域诊断仍可能重复侵占主轨。

本 change 因此上升到Timeline Editor架构：Timeline Core只拥有时间坐标、Clip/Point Marker/editable Curve的布局、选择、手势和Undo；跨producer Marker校验使用独立typed topology context；领域分析通过显式Editor tool provider进入按需侧面板；Live Debug继续使用独立runtime binding。Foot Analysis作为第一个迁移的领域工具，先以精确 AnimationClip 与 Analysis Source 生成独立、Editor-only、可删除重建的`AnimationFootAnalysisArtifact`，Definition Build再把精确产物发布进Projection。Timeline主时间轴不再显示四条生成lane，也不再需要Tree/Definition才能分析一个AnimationClip。

## What Changes

- 新增typed `TimelineEditorOpenRequest`与`TimelineEditorSessionContext`，分别表达Timeline本地serialized owner、selection/mutation、可选Marker topology context、可选runtime debug binding和显式tool catalog；删除Timeline Editor中的无类型`object AuthoringContext`和任意cast消费。
- 新增显式`TimelineEditorToolProvider`合同。Provider必须声明稳定ToolId、适用的Track/Clip类型、面板标题、所需输入和创建入口；Timeline Core只托管工具区域、selection通知和生命周期，不引用Character、Foot Placement、Projection或具体分析类型。
- Tool provider通过Editor composition root显式注册，不使用反射、`TypeCache`、字符串类名或AssetDatabase扫描。没有provider时Timeline不显示空工具占位。
- 将Timeline上下文拆为四类：本地作者上下文始终存在；Marker topology context只服务跨producer/call site校验；domain tool input由具体面板显式选择；Live Debug只服务运行实例。任何一类不得作为另一类的fallback。
- 新增 Editor-only `AnimationFootAnalysisArtifact` 规范产物。它只保存规范 identity 与经过确定性压缩的左右脚特征，不是 ScriptableObject 作者资产，不进入 `Assets`、Player、Addressables、YooAsset 或网络产物。
- 产物 identity 至少包含 AnimationClip GUID与import dependency、Analysis Source identity/version、Sampling Rig GUID与dependency、Rig Calibration identity/revision、sample rate、threshold/reduction参数和算法版本。
- 新增唯一 `AnimationFootAnalysisArtifactStore`，固定写入 `Library/CharacterFootAnalysis/...`。相同输入必须产生相同 bytes/hash；缺失或过期必须显式报告，不按名称、路径、duration或最近Definition猜测。
- 将 `CharacterFootPlacementAnimationAnalyzer` 拆成“单 AnimationClip 分析”与“Definition 可达clip收集”两个职责。单clip分析只需要 AnimationClip、Analysis Source、Sampling Rig与Calibration，不接受RootTree、SimulationProgram或Projection。
- Timeline Editor删除每条AnimationTrack中的常驻`FOOT ANALYSIS` header、四条metric lane、`Unavailable`占位和Definition Build `Rebuild`按钮。
- Timeline主时间轴只显示作者数据：Animation Clip、Point Marker、TreeClip/Window、MotionWarp区间和显式registered editable Curve Channel。生成分析不得改变Track高度、左侧行数、垂直滚动范围或选中对象。
- Timeline窗口增加按需 `Animation Analysis` 工具面板，不创建第三个独立EditorWindow。作者显式选择当前Animation Clip和Analysis Source，一次选择Left/Right之一及一个metric查看；面板默认关闭，不占据主时间轴行布局。
- 从Character Profile/Graph上下文打开Timeline时，面板 MAY显式接收该Profile精确Analysis Source作为初始选择；独立打开Timeline时作者必须显式选择Analysis Source。两种入口使用同一分析服务，不把选择保存回Timeline资产，也不反向搜索Definition。
- `Rebuild Selected Clip` 只重建当前`AnimationClip + Analysis Source`对应产物，不编译Graph、Semantic IR、Float32/Fixed Program或完整Projection。
- Ready artifact MAY在Analysis面板生成左右脚接触候选。候选必须携带artifact identity/content hash、Timeline/Track/Clip stable identity、脚侧、源动画归一化时间和目标Timeline frame；它只是瞬时只读建议，不是Timeline作者数据或Runtime输入。
- 作者必须显式确认目标AnimationTrack后，Analysis面板才可通过`TimelineEditorSessionContext.Apply`把未过期候选转换为正式`LeftFootContact`/`RightFootContact` Point Marker。Apply只替换这两个MarkerId集合，保留其它业务Marker，并继续进入既有Undo、dirty、validator、compiler与Agent v15链。
- Apply前必须重新解析精确artifact并核对clip dependency、Analysis Source、Sampling Rig、Calibration、采样参数、artifact hash和Timeline映射。任一输入变化必须拒绝Stale候选，不得使用面板缓存、marker旧frame或半周期假设继续写入。
- Definition正式Build先收集全部可达AnimationClip，再按精确identity读取或生成所需artifact，最后把生成数值按stable Timeline/Track/Clip identity嵌入`CharacterPresentationProjection`。Program与Projection的发布仍保持同一原子事务。
- Projection仍是Player Runtime唯一Foot Analysis数据；Runtime不读取Library artifact、不即时采样AnimationClip，也不依赖Editor Analyzer。
- Timeline Analysis状态只描述当前artifact的`Missing/Stale/Ready`；Definition/Profile Inspector继续描述Projection的`Missing/Stale/Ready`。两种状态不得混为一个按钮或revision。
- Marker Sync继续只使用离散Point Marker；Distance Matching继续使用独立命名距离曲线；Foot Analysis只服务Foot Placement/IK生成特征。三者不得互相替代。
- Animation Clip仍只有一个可写`Foot Placement Weight`控制曲线。sole speed、height、plant confidence、landing prediction不得进入typed editable Curve Channel Catalog、Timeline Undo、Blackboard或Agent Patch。
- Agent v15继续只读写Timeline作者数据。Validator只透传artifact/Projection缺失与过期诊断，不采样动画、不写artifact、不增加Foot Analysis mutation。
- 删除`ITimelineFootAnalysisAuthoringContext`、`CharacterPipelineFootAnalysisAuthoringContext`、`TimelineFootAnalysisLaneView`及其Timeline layout/CSS/Inspector消费路径，不保留Definition-context兼容入口。
- 保留现有Rig Calibration、Foot Placement Runtime、Final IK adapter、Projection feature sampler和Corin正式表现装配；迁移后重新生成Corin所需artifact与Projection。

## Capabilities

### Added Capabilities

- `btsmtl-timeline-animation-authoring-surface`：定义Timeline本地作者内容、typed打开请求、领域工具扩展区、上下文分层和主轨布局边界。
- `character-animation-foot-analysis-artifact`：定义Editor-only动画脚分析产物、identity、store、单clip构建与Projection发布边界。

### Modified Capabilities

- `btsmtl-timeline-editor-preview`：删除每Track的Foot Analysis lane与Definition依赖，增加不占主轨的按需Animation Analysis面板。
- `character-animation-presentation-authoring`：让Definition Build消费正式analysis artifact，而不是把单clip分析绑定到完整Projection上下文。
- `btsmtl-compiled-simulation-program`：在原子发布前增加精确artifact收集/校验，同时保持Gameplay Program不受纯表现分析影响。
- `character-foot-placement-presentation`：保留Projection运行时消费，明确artifact只属于Editor生成阶段，Timeline只保留单一Foot Placement Weight作者曲线。
- `agent-character-controller-synthesis`：将分析诊断收敛为只读artifact/Projection状态，不增加生成数据写入口。

## Current Spec Comparison

- 现行Timeline Editor通过`object AuthoringContext`从Graph窗口透传任意领域对象，current specs没有为本地作者、跨producer拓扑、领域分析与Runtime Debug规定typed边界。本change新增正式Timeline Animation Authoring Surface能力并删除无类型Context。
- 现行`btsmtl-timeline-editor-preview`明确要求AnimationTrack提供默认折叠的`FOOT ANALYSIS`分组、Definition context、Projection状态和Definition Build按钮。本change删除这三项要求，改为不占Timeline轨道的按需单clip分析面板。
- 现行`character-animation-presentation-authoring`要求Foot Analysis只能由完整Projection Build生成。本change改为先生成可复用的Editor-only artifact，再由Projection Build按精确identity收集；Projection仍是Runtime唯一发布数据。
- 现行`character-foot-placement-presentation`规定生成特征“只进入Projection且不得拥有第二份资产”。本change允许`Library`中的非作者、非Runtime artifact作为编译中间产物，但禁止ScriptableObject作者副本和Runtime双读路径。
- 现行`btsmtl-compiled-simulation-program`把动画采样与Program/Projection发布绑定在一个重任务中。本change保留发布原子性，但允许单clip artifact预先独立生成和复用；Build必须精确校验artifact输入identity，不能把cache命中当作信任。
- 现行`character-animation-presentation-authoring`禁止独立Foot Analysis窗口。本change不创建新窗口，而是在既有Timeline窗口提供按需工具面板，因此仍保持Graph与Timeline两个窗口。
- 现行Marker Sync、Distance Matching、MotionWarp、Camera和通用Curve Channel合同不由本change重写；本change只删除Foot Analysis对这些作者界面的侵占。

## Dependencies And Sequencing

- 必须先建立typed Timeline open/session/tool合同，再迁移Marker topology与Foot Analysis；不得直接在旧`object AuthoringContext`上再增加一个provider cast。
- 保留`add-timeline-animation-marker-sync`已经安装的Point Marker、Projection marker map和segment映射算法；`refactor-animation-selection-pose-graph-boundary`负责把运行时解析迁入显式MarkerSync节点。Foot Analysis只按Player最终采用的raw/effective sample time采样，不读取MarkerId。
- 保留`add-predictive-foot-placement-presentation-pass`已经安装的唯一Foot Placement Pass、Final IK程序集边界与单一Foot Placement Weight曲线。
- 必须先建立artifact合同、store和单clip analyzer，再迁移Projection Build，最后删除Timeline旧Context/lane。不得先删除Projection生成能力导致Runtime缺数据。
- 如果其它active change同时修改Foot Placement Runtime或Timeline布局，必须按文件所有权串行合并，不建立兼容Context或双UI入口。

## Out Of Scope

- 不改变Foot Placement contact、prediction、Ground Envelope、constraint、pelvis和Final IK算法。
- 不实现Motion Matching、Stride Warping或Distance Matching。
- 不自动提交Marker。分析结果只生成瞬时候选；只有作者显式确认后，才通过Timeline正式mutation把候选转换成AnimationTrack已有的Sync Marker作者数据。
- 不把generated feature曲线写回AnimationClip、Timeline、Blackboard、Profile或独立可编辑FootPhase资产。
- 不为每个Timeline Clip保存Analysis Source；Analysis Source属于角色表现分析配置和Editor面板显式上下文。
- 不让Timeline Preview伪造PhysicsScene、地面查询、Foot Lock或最终IK世界结果。
- 不修改Semantic IR、Float32/Fixed Program ABI、Character State、Snapshot、StateHash或网络协议。
- 不新增测试或人工验证task，不运行Unity batchmode。

## Stop Conditions

- 如果单AnimationClip无法在不读取RootTree/Definition/SimulationProgram的情况下由精确Sampling Rig安全采样，停止说明Analyzer输入缺口，不保留旧Definition-context路径。
- 如果Library artifact无法拥有稳定canonical identity与exact-byte校验，停止说明产物格式取舍，不使用Unity对象引用或内存cache作为发布依据。
- 如果Projection无法按stable Timeline/Track/Clip identity消费共享AnimationClip artifact，停止说明binding缺口，不按clip名称、path或数组index匹配。
- 如果Timeline按需面板必须修改Timeline资产才能记住Analysis Source，停止说明所有权tradeoff，不把角色级Rig配置写进shared Timeline。
- 如果删除旧Foot Analysis lane会破坏Marker、Curve或Clip选择，先统一Timeline selection/layout职责，不保留旧lane作为fallback。

## Success Criteria

- Timeline Editor不再持有或暴露无类型`object AuthoringContext`，所有可选能力通过显式typed合同装配。
- Timeline Core程序集不引用Character Pipeline、Foot Placement、Presentation Projection或具体领域分析实现。
- Marker topology、domain analysis和Live Debug使用三个独立上下文，不互相补全或猜测。
- 独立Timeline窗口能够在显式Analysis Source下分析当前AnimationClip，不需要Graph、Tree、CharacterPipelineDefinition、SimulationProgram或PresentationProjection。
- Timeline主时间轴没有`FOOT ANALYSIS`行、四组生成metric、`Unavailable Definition context`提示或完整Definition `Rebuild`按钮。
- Analysis面板默认关闭，一次只查看一个脚和一个metric，不改变Track高度与主时间轴滚动。
- Analysis面板能从有效骨骼artifact生成左右脚接触候选；候选可视但不自动保存，显式Apply后只更新目标Track的脚接触Marker并保留其它Marker。
- 候选Apply会重新验证artifact与Timeline映射，过期候选被拒绝；WalkLoop与RunLoop不再依赖`0/半周期`人工假设。
- `Rebuild Selected Clip`只更新对应Library artifact，不重编Gameplay Program或完整Projection。
- Definition Build按精确identity收集/生成全部可达artifact，并在同一发布事务中生成匹配Program的Projection。
- Player Runtime只读取Projection，完全不知道Library artifact和Editor Analyzer。
- Marker Sync、Distance Matching、Foot Placement Weight和generated Foot Analysis保持四种独立语义与唯一数据所有权。
- Agent v15无法修改generated feature，Compiler诊断能定位具体Clip、Analysis Source、Sampling Rig、Calibration和artifact状态。
- 旧Timeline Foot Analysis Context、lane、layout和Definition rebuild路径被删除，不存在兼容分支或第二入口。
- `openspec validate refactor-timeline-animation-authoring-boundary --strict --no-interactive`通过。

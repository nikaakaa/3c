# Design: Timeline Animation Authoring Surface 与领域分析工具

## 动画播放边界重新基线

`refactor-animation-selection-pose-graph-boundary`只改变Timeline Preview接入正式表现链的合同：预览时间降低为`AnimationSelectionFrame`，再进入同一编译Pose Plan。图上只使用直接Player就明确硬切，连接局部Inertialization就复用正式history/residual/rebase，连接BlendStack就保留多source历史；Timeline Editor Core、Foot Analysis Artifact和Marker候选工具不得拥有或模拟播放器状态。

Preview若缺少FootPlacement所需Body、PhysicsScene、Rig Calibration或Solver，必须把world-aware阶段标记为Unavailable，并明确停在`ComposedAnimationPoseFrame`；不得伪造平面、静默跳过后仍发布FinalAnimationPoseFrame。

## Context

当前代码把脚分析实现为Projection Build的内部阶段：

```text
CharacterPipelineDefinition
  -> Authoring Discovery遍历RootTree
  -> 收集可达Timeline/AnimationClip
  -> CharacterFootPlacementAnimationAnalyzer
  -> CharacterPresentationProjection
  -> Timeline通过CharacterPipelineAuthoringContext反向读取Projection
```

这条链适合最终发布，但不适合局部动画分析。Timeline只是为了查看一个AnimationClip，却必须从Graph窗口获得Definition context；`Rebuild`会执行完整Character Simulation Build。生成的sole speed、height、plant confidence和landing prediction又被作为四条常驻lane铺在每个AnimationTrack下面，造成“内部诊断数据像作者参数”的错误界面。

根问题不是一条Foot Analysis lane，而是Timeline Editor缺少正式作者表面与上下文分层。正确分层是：

```text
Timeline Animation Authoring Surface
  ├─ Local Authoring: Clip / Marker / Editable Curve / Undo
  ├─ Topology Context: 跨producer与call site只读关系
  ├─ Domain Tools: Animation Analysis等按需工具
  └─ Runtime Debug: 运行实例只读绑定
```

Foot Analysis再沿自己的领域链工作：

```text
AnimationClip + Analysis Source + Sampling Rig + Calibration
  -> Animation Foot Analyzer
  -> Editor-only Analysis Artifact

Character Definition + reachable clip bindings
  -> Artifact Resolver
  -> CharacterPresentationProjection
  -> Player Foot Placement Runtime
```

## Goals

- 让Timeline Core只拥有通用作者表面，不依赖任何Character领域实现。
- 用typed context替换`object AuthoringContext`和运行时cast。
- 为领域工具提供显式、不侵占Track lane的扩展区域。
- 单clip分析不依赖Tree、Definition、Program或Projection。
- Timeline主时间轴只承载作者可直接修改的时间内容。
- 生成特征以规范、可重建、可复用的Editor artifact存在。
- Projection继续是Player Runtime唯一分析数据。
- 分析状态与Projection发布状态分开表达。
- 保留现有Foot Placement算法与Marker作者数据；运行时Marker Sync节点化由`refactor-animation-selection-pose-graph-boundary`统一设计，本change不建立第二套解析路径。

## Non-Goals

- 不把Timeline Editor做成任意插件市场或反射式Inspector框架。
- 不把所有现有Track运行语义抽象成统一数据模型。
- 不改变Foot Placement算法与Final IK求解。
- 不让生成特征变成作者曲线。
- 不自动保存Sync Marker或生成Distance Curve；Foot Analysis只能生成瞬时只读接触候选，作者显式确认后才可通过Timeline正式mutation转换为已有Sync Marker作者数据。
- 不创建第三个动画/表现EditorWindow。
- 不让Library artifact进入Player或版本库。

## Terms

### Timeline Animation Authoring Surface

Timeline窗口中由Core拥有的固定作者区域，只表达Timeline时间上的可编辑内容：

```text
Span Clip
Point Marker
Registered Editable Curve
Track/Clip Selection
Frame Geometry
Undo/Redo
```

### Topology Context

可选只读上下文，用于回答“这个producer被哪些TimelineNode调用”“同组还有哪些producer”“Once/Loop是否冲突”。它可以由Character Graph提供，但不能被本地Clip编辑或领域分析当作必需输入。

### Domain Tool Provider

Editor-only显式扩展，使用Timeline当前selection并自行声明领域输入。Provider只能在Timeline托管的工具区域绘制，不得直接修改Track layout或注册Runtime Track。

### Analysis Source

Editor-only配置资产，唯一保存：

```text
AnalysisSourceId
AnalysisVersion
SamplingRigAssetGuid
RigCalibration
SampleRate
Thresholds
CurveReductionSettings
AlgorithmVersion
```

它描述“使用哪套角色骨架和规则分析动画”，不描述Graph、State、Timeline call site或Runtime IK状态。

### Analysis Artifact

由精确AnimationClip与Analysis Source生成的不可变Editor产物。它不是作者资产，不能手工编辑，也不能被Timeline、Agent或Profile写入。

### Projection Binding

Character Definition Build把artifact中的特征按stable Timeline/Track/Clip identity复制进Projection。相同AnimationClip可被多个binding引用，但每个binding仍保持自己的producer身份和Timeline时间映射。

## Decision 1: Timeline Core使用typed session而不是object AuthoringContext

打开Timeline时构造显式请求：

```text
TimelineEditorOpenRequest
  TimelineData
  SerializedOwner
  SerializedPropertyPath
  OwnershipLabel
  Optional MarkerTopologyContext
  Optional RuntimeDebugBinding
  TimelineEditorToolCatalog
```

窗口内部形成`TimelineEditorSessionContext`，唯一提供selection、mutation transaction、frame geometry和可选typed能力。任何View不得从`object`做`as ITimeline...`探测。

Graph窗口可以提供Marker topology context和Character领域tool catalog，但Timeline独立打开时本地作者能力必须完整可用。

### Tradeoff

- 收益：依赖一眼可见，局部编辑不再被Graph/Definition绑架。
- 代价：所有现有Context consumer需要一次性迁移。保留object兼容会继续产生分裂，因此不保留。

## Decision 2: 领域工具使用显式Provider，不成为Track lane

Provider合同至少包含：

```text
ToolId
DisplayName
IsApplicable(selection)
CreatePanel(session, explicit domain input)
Dispose
```

注册由Timeline Editor composition root显式完成。Core不通过反射发现工具，也不持有Character程序集类型。Provider可以监听selection，但不能：

- 改变Track基础高度。
- 向TimelineData注入派生字段。
- 绕开正式mutation transaction修改作者内容。
- 创建第二个Runtime Timeline解释器。
- 把缺失domain input补成Graph/Definition搜索。

Foot Analysis provider属于Character Editor程序集；Timeline Core只看到通用provider接口。

### Tradeoff

- 收益：后续Distance Analysis、Root Motion检查等工具有固定位置，不会继续长成主轨lane。
- 代价：增加一个很窄的Editor扩展合同。它只解决工具托管，不抽象领域算法。

## Decision 3: Artifact 使用独立规范身份

identity至少由以下内容规范编码并哈希：

```text
FormatVersion
AnimationClip GUID
AnimationClip dependency hash
AnimationClip length/frame rate/import settings revision
Analysis Source GUID
AnalysisSourceId/AnalysisVersion
Sampling Rig GUID/dependency hash
Rig Calibration Id/Revision
SampleRate
Threshold values
Reduction values
AlgorithmVersion
```

输出包含：

```text
Header
LeftFoot FeatureCurveSet
RightFoot FeatureCurveSet
PayloadHash
```

路径由artifact identity决定，例如：

```text
Library/CharacterFootAnalysis/<analysis-source-id>/<clip-guid>/<artifact-hash>.cfa
```

Store必须使用canonical codec写入临时文件并原子替换。读取时重新计算payload hash并验证全部identity；损坏、旧版本或输入不匹配统一视为不可用，不做格式兼容。

### Tradeoff

- 收益：单clip可独立分析；相同输入可跨Timeline/Definition复用；不再为查看曲线重编整棵Tree。
- 代价：增加一个Editor artifact格式与stale规则。这个复杂度集中在生成工具，不污染作者模型和Runtime。

## Decision 4: Analyzer只接受动画分析所需输入

Analyzer核心入口固定为：

```text
Analyze(AnimationClip, CharacterFootPlacementAnalysisSource)
  -> AnimationFootAnalysisArtifactData
```

内部通过Source精确解析Sampling Rig，实例化Preview Scene，使用同一Rig Calibration与Animator/Playable手动采样。核心入口不得接受：

- CharacterPipelineDefinition
- RootTree
- StateMachine
- TimelineData集合
- CharacterSimulationProgram
- CharacterPresentationProjection

Definition Build的clip discovery保留在Compiler orchestration中，只负责形成`stable clip binding -> AnimationClip`列表，然后逐项调用Artifact Resolver。

### Tradeoff

- 收益：分析器输入真实且最小，可以独立使用和缓存。
- 代价：Compiler需要一层明确的binding收集与artifact解析，而不是Analyzer自己遍历所有Timeline。

## Decision 5: Timeline主时间轴不显示生成分析lane

主时间轴只允许以下作者内容改变行布局：

```text
Span Clip
Point Marker
TreeClip/Window
MotionWarp Range
Registered Editable Curve Channel
```

删除：

```text
FOOT ANALYSIS header
Sole Speed lane
Sole Height lane
Plant Confidence lane
Landing Delay/Distance lane
Unavailable Definition Context占位
Definition Rebuild按钮
```

生成分析不能拥有Timeline selection、Undo、dirty owner、mutation adapter或Curve Channel Id。

### Tradeoff

- 收益：作者视图重新聚焦“我能编辑什么”，Timeline高度稳定。
- 代价：需要额外一步打开Analysis面板才能查看内部数据。生成诊断本来就是按需行为，这个代价合理。

## Decision 6: Analysis作为同一Timeline窗口的按需工具面板

Timeline工具栏增加一个`Analysis`开关。关闭时不创建renderer和artifact读取任务；打开时显示独立面板：

```text
Animation Clip       [当前选中Clip]
Analysis Source      [显式对象选择]
Artifact Status      [Missing/Stale/Ready]
Foot                 [Left | Right]
Metric               [Speed | Height | Plant | Landing]
[Rebuild Selected Clip]
单一只读曲线视图
```

面板一次只显示一个脚和一个metric。`Speed`可以在同一metric视图中组合显示XYZ或magnitude，但不得重新扩张成四条Timeline lane。`Landing`只在同一视图切换delay/offset，不成为作者可写曲线。

上下文规则：

1. 从Profile/Graph打开时，窗口可接收该Profile精确Analysis Source作为显式初值。
2. 独立打开时，未选择Source就显示`Analysis Source Required`。
3. 不从AssetDatabase搜索引用该Timeline的Definition。
4. 不把Source写回shared Timeline。
5. Editor session可记住当前窗口选择，但该选择不成为项目作者数据或编译输入。

### Tradeoff

- 收益：不创建第三个窗口，同时消除Tree依赖和主轨膨胀。
- 代价：独立Timeline第一次分析需要选择Source。不同角色可能用同一动画但不同Rig，这个显式步骤比错误猜测安全。

## Decision 7: Local Analysis Build与Projection Publish分离

### Rebuild Selected Clip

```text
Selected AnimationClip
  + Selected Analysis Source
  -> validate Source/Rig/Calibration
  -> sample clip
  -> reduce curves
  -> write exact artifact
  -> refresh Analysis panel
```

它不得调用：

```text
CharacterSimulationProgramBuildService.Build
Authoring Discovery
Semantic IR compile
Float32/Fixed lowering
Program asset publish
Projection asset publish
```

### Definition Build

```text
Authoring Discovery
  -> stable reachable clip bindings
  -> resolve exact artifact for each unique clip/source pair
     -> Ready: validate and reuse
     -> Missing/Stale: generate through same analyzer
  -> bind feature payload by stable Timeline/Track/Clip identity
  -> build Projection
  -> validate Program/Projection identity
  -> atomically publish Program + Projection
```

Artifact生成可以提前发生，也可以在Definition Build中按需发生；两者必须调用同一个Builder和Store。Definition Build不得信任文件名或时间戳，必须验证canonical identity和payload hash。

### Tradeoff

- 收益：局部调试快，正式发布仍完整且原子。
- 代价：存在“artifact Ready但Projection Stale”的合法状态，因此UI必须分别显示两个状态，不能用一个Ready混淆。

## Decision 8: Projection仍是Runtime唯一真相

Player中链路保持：

```text
PresentationCommand
  -> Producer Binding
  -> raw VisualSampleTime
  -> 显式MarkerSync节点（若图中存在）
  -> Player采用的raw/effective sample time
  -> Projection Clip Feature Sampler
  -> Foot Placement Planner
  -> Final IK
```

Runtime程序集不得引用Artifact Store、AssetDatabase、Analysis Source或Editor Analyzer。Library目录删除后，已发布Player行为不受影响。

## Decision 9: Marker、Distance、Weight与Analysis严格分离

| 数据 | 作者形式 | 运行用途 | 是否可编辑 |
|---|---|---|---|
| Sync Marker | Timeline离散点 | Walk/Run等动画采样时间同步 | 是 |
| Distance Curve | 命名连续曲线 | 按目标距离选择动画时间 | 是，仅正式能力启用后 |
| Foot Placement Weight | Clip归一化控制曲线 | 作者控制IK总体介入权重 | 是 |
| Foot Analysis Feature | 生成artifact与Projection数据 | plant/landing预测和诊断 | 否 |

显式MarkerSync节点可以在source采样前把raw VisualSampleTime映射为effective time；Foot Analysis按Player最终采用的时间采样。二者不共享MarkerId、phase或contact状态。Distance Matching未来可消费独立Distance Curve，但不能读取plant confidence代替距离。Foot Placement Weight只控制介入量，不表达左右脚接触。

## Decision 10: Agent只处理作者数据

Agent v15保持：

- 可读写Sync Marker。
- 可读写registered editable Curve Channel，包括Foot Placement Weight。
- 可只读看到Analysis Source identity和Compiler诊断摘要。
- 不导出feature key payload。
- 不构造artifact。
- 不提供Rebuild Foot Analysis Patch operation。
- 不写Projection。

Patch修改AnimationClip控制曲线或Timeline后，正式Definition Build可以使Projection更新；Agent只接收编译报告，不拥有生成阶段。

## Decision 11: Foot Analysis只生成候选，显式Apply才成为Marker

Artifact中的左右脚`PlantConfidence`可以用于推导“未接触到稳定接触”的离散上升沿，但它不能直接成为Runtime Marker真相。Analysis面板为当前精确AnimationClip生成瞬时contact proposal：

```text
Ready artifact
  -> 按artifact sample rate重采样左右PlantConfidence
  -> 检测cyclic contact onset
  -> 映射ClipIn与source cycle到Timeline frame
  -> 只读Left/Right candidate
```

Proposal必须携带artifact identity/content hash、Timeline/Track/Clip stable identity、clip frame mapping、foot side、source normalized time、target frame与plant confidence。Apply时重新读取artifact并重新构建proposal；revision不一致必须报Stale并拒绝。

Apply规则：

1. 仅允许目标Track已经显式配置为`MarkerGroup/Cyclic`。
2. 仅允许一个AnimationClip完整覆盖该Track；多Clip混合不能由单clip骨骼分析推导producer级Marker。
3. 作者必须在确认框中看到目标Timeline与Track identity。
4. mutation必须通过`TimelineEditorSessionContext.Apply`进入Timeline正式Undo、dirty和刷新链。
5. 只替换`LeftFootContact`与`RightFootContact`集合；其它业务Marker原样保留。
6. 复用仍匹配的stable marker identity，新增候选才创建新identity，移除多余脚步Marker。
7. 提交后继续由现有Marker validator、Projection compiler与显式MarkerSync节点消费，不新增analysis runtime reader。

### Tradeoff

- 收益：作者不再手猜半周期，落脚点来自真实角色Rig与动画骨骼，同时Runtime仍只有正式Marker一份真相。
- 代价：必须先得到Ready artifact并执行一次显式Apply；这是防止Rig、import或阈值变化静默改坏Timeline的必要确认。

## Migration

### 保留

- `CharacterFootPlacementRigCalibration`
- `CharacterFootPlacementAnalysisSource`
- Sampling Rig与Analyzer采样算法
- `AnimationFootFeatureCurveSet`
- Projection feature payload与Runtime sampler
- Foot Placement Planner/Runtime/Final IK adapter
- Timeline `Foot Placement Weight`可编辑channel

### 删除

- `ITimelineFootAnalysisAuthoringContext`
- `CharacterPipelineFootAnalysisAuthoringContext`
- `TimelineFootAnalysisLaneView`
- Timeline Editor的`object AuthoringContext`与cast式能力发现
- TimelineTrackView/Handle中的Foot Analysis header、lane、status与Rebuild
- Timeline layout中的Foot Analysis行高与展开状态
- Foot Analysis专用USS样式
- “Open this Timeline from a Character Pipeline graph”路径

### 新增

- `TimelineEditorOpenRequest`
- `TimelineEditorSessionContext`
- `TimelineEditorToolProvider`与显式catalog
- artifact header/data/codec
- artifact identity builder
- artifact store
- single-clip analysis builder
- Definition artifact resolver
- Timeline Animation Analysis panel

迁移不需要兼容旧serialized Timeline字段，因为现有Foot Analysis lane读取Projection且没有作者数据。旧UI代码可直接删除。

## Diagnostics

Artifact诊断必须包含：

```text
AnimationClip GUID/path
Analysis Source identity/path
Sampling Rig identity/path
Calibration identity/revision
Expected artifact hash
Observed artifact hash/status
Analyzer stage
具体失败原因
```

Projection编译诊断必须额外包含stable Timeline/Track/Clip identity。Runtime诊断继续显示Projection identity与当前feature sample，不显示Library路径。

## Performance

- Timeline主界面关闭Analysis时不得读取artifact或创建curve renderer。
- 同一`Clip + Source`在一次Build中只分析一次。
- Store读取使用规范header快速拒绝后再验证payload hash。
- Definition Build可复用Ready artifact，但不得跳过identity验证。
- Runtime热路径不改变，不新增分配或Editor依赖。

## Rejected Alternatives

### 继续只把分析保存在Projection

实现最少，但局部动画分析永远依赖Definition/Tree，Rebuild仍然重。拒绝。

### 把生成曲线写进Timeline Clip

独立Timeline容易查看，但把派生数据变成作者数据，产生stale、Undo和双真相。拒绝。

### 为每条metric保留Timeline只读lane

数据虽然只读，但仍持续侵占主作者界面并混淆Marker/Curve/Analysis。拒绝。

### 从引用关系自动猜Definition或Rig

shared Timeline可能被多个角色和Calibration使用，自动选择会产生错误结果。拒绝。

### 创建独立Foot Analysis EditorWindow

边界清楚，但增加第三个长期窗口，与当前Graph + Timeline双窗口工作方式冲突。使用同Timeline窗口按需面板。

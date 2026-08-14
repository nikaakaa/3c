# Change: 分离动画Sequence作者数据与Action Timeline编排

## Why

当前项目虽然已经要求Timeline与持续Pose Source复用时间尺、Marker和Curve交互，但实际实现仍是两套编辑表面：

- `TimelineEditorView/TimelineFieldView`是完整UI Toolkit主时间轴，直接绑定`TimelineData -> Track -> Clip`。
- 持续Sequence source由独立`CharacterPoseSourceEditorWindow`和IMGUI `AnimationTimeField`编辑；Blend Space Inspector又嵌入一份相同的`AnimationTimeField`。

这导致同一种“原始动画素材时间数据”被不同业务容器分别拥有：持续Run的Marker与Foot Placement Weight在Profile Sequence Binding里，Blend Space sample另存Marker，有限Action的Marker与Foot Placement Weight又在Timeline AnimationTrack/Clip里。编辑器看起来都在编辑AnimationClip，实际Mutation、Undo、选择、预览和数据owner并不统一；Inspector中的内嵌时间轴还压缩主工作面并形成第三种交互。

UE的边界更清楚：AnimSequence拥有单段素材的Marker、Curve和Notify；Montage只引用Sequence并编排片段、Section和动作窗口；状态机只决定播放和Transition。项目不需要照搬UE资产类型，但必须采用同样的数据职责，否则仅复用控件仍会继续产生Marker/Curve副本。

本change先完成第一阶段：建立一等Animation Sequence作者资产和双文档Timeline工作面，清理Pose Source与Blend Space中的重复时间编辑器。Pose StateMachine与Transition编辑页面不在本change实施；现有Compiler继续从两侧Sequence binding推导Source Sync Plan，后续change再规划Transition的人工编辑口径。

## What Changes

- 新增正式`CharacterAnimationSequenceAsset`作为单段原始动画的唯一作者owner，保存稳定Sequence identity、AnimationClip引用、Rig、Loop/Finite语义、默认播放倍率、Marker Sync配置、typed素材Curve、显式Notify和Foot Analysis Source引用。
- Profile-owned Sequence Pose Source Binding不再保存AnimationClip、Marker、Foot Placement Weight或Analysis输入副本，只引用精确Sequence资产并保留Source Slot到Sequence的角色级binding关系。
- Blend Space sample不再直接保存AnimationClip和Marker副本，改为引用精确Sequence资产；sample只保留位置、角色、Stationary时间和Blend Space内部参数。
- 有限Action Timeline的Animation Clip改为`Sequence Segment`：引用Sequence并只保存Start/End、ClipIn、Extrapolation、Weight、Ease和动作编排字段。Timeline AnimationTrack不再拥有素材Marker Sync；Marker、Foot Phase/Weight和Notify必须回到Sequence文档编辑。
- Action Timeline继续唯一拥有ActionWindow、Motion、MotionWarp、Decision、Cue、TreeClip和Action逻辑时间。Sequence Notify是表现素材事件，不得产生Gameplay Fact、Window、Motion、Warp、Cue或Action lifecycle。
- 把现有Timeline Editor重构为一个窗口、两个typed文档模式：
  - `Sequence`模式显示单素材Span、Sync Marker、Notify、typed素材Curve、Analysis和Sequence Preview。
  - `Action Timeline`模式显示Sequence Segment、Section、Action Track、Window、Cue及Timeline Curve；双击Segment进入其Sequence文档。
- 两种模式复用同一UI Toolkit time ruler、frame geometry、zoom/pan、playhead、selection、Marker/Notify/Curve lane、pointer draft、Undo手势和Preview控制基础设施，但分别通过Sequence与Timeline typed document adapter提交到各自正式owner。
- Details/Inspector只显示当前选择的精确数值和属性，不再承载可缩放时间轴、Marker lane或Curve lane。
- 新增Action Timeline Section作者数据，Section只作为Timeline内稳定命名的时间锚点和跳转/导航边界，不复制Sequence Marker、不替代TreeClip Decision，也不直接产生Gameplay事实。
- Sequence Preview只推进表现采样和只读Analysis overlay；Action Timeline Preview继续复用正式Action Playback/AnimationSlot/Pose Plan。两种Preview都不运行Gameplay Simulation Session，不自动Build或重分析。
- 原子迁移现有Profile Sequence Binding、Blend Space sample和Action Timeline AnimationTrack/Clip数据到唯一Sequence资产引用；内容相同且正式资源/Marker/Curve/Analysis输入完全相同的owner可以显式合并为同一Sequence，任何差异都生成不同Sequence，不按名称猜测合并。
- 删除`CharacterPoseSourceEditorWindow`、`AnimationTimeFieldAuthoring`、Blend Space Inspector中的Sample Time Authoring、Timeline AnimationTrack素材Marker owner、Timeline Clip素材Foot Placement Curve以及对应重复菜单、session状态、Inspector内嵌时间轴和旧Document字段；不保留兼容reader、fallback绑定或双写。
- 同步升级Agent Document v3：Sequence进入独立editable分片；Profile、Blend Space和Action Timeline只通过稳定对象引用指向Sequence。Exporter、strict codec、Reconciler、typed Mutation、Validator和context catalog共同切换，不新增Sequence局部MCP工具或第二事务。

## Capabilities

### Added

- `character-animation-sequence-authoring`：定义一等Animation Sequence资产、素材内Marker/Curve/Notify/Analysis所有权、Sequence Preview与对外引用合同。

### Modified

- `btsmtl-timeline-editor-preview`：Timeline Editor升级为Sequence/Action Timeline双文档工作面并共享唯一时间编辑基础设施；Action Marker/素材曲线从Timeline迁出。
- `btsmtl-timeline-animation-authoring-surface`：Timeline Core从只接受TimelineData的窗口提升为typed时间文档宿主，领域工具按Sequence/Action Timeline文档能力装配。
- `character-animation-presentation-authoring`：Profile Sequence Binding与Blend Space sample改为引用Sequence，素材作者数据不再分散在binding或sample中。
- `character-action-animation-authoring-workspace`：Action Workspace把Sequence素材owner与Timeline编排owner分开，并提供精确Open Sequence导航。
- `character-pipeline-definition-authoring`：Presentation Profile只装配Sequence引用，不再拥有Sequence素材正文。
- `character-state-timeline-authoring-loop`：Corin全部动画Marker、Curve与Analysis迁入Sequence，Action Timeline与Pose Binding只保留引用和各自业务字段。
- `character-animation-pipeline`：Action Timeline producer从Sequence Segment解析表现采样；Sequence Notify保持纯表现，不进入Gameplay Timeline。
- `character-animation-foot-analysis-artifact`：Artifact identity从裸AnimationClip使用点统一解析到Sequence及其精确Clip/Rig/Analysis依赖，生成内容仍只读。
- `agent-character-controller-synthesis`：Document把Sequence作为独立正式authoring owner，并让Timeline/Profile/Blend Space只保存稳定Sequence引用。
- `btsmtl-agent-authoring-document-sync`：Document v3增加Sequence分片、严格引用和同一事务Reconciler闭包，删除旧Timeline Track与Profile binding素材字段。
- `btsmtl-agent-authoring-mcp-bridge`：五个生命周期工具透传包含Sequence文件对的同一Character Document事务，不新增Sequence局部工具。

## Current Spec Comparison

- current `btsmtl-timeline-editor-preview`已经要求`Source Time Authoring`跨Timeline与Pose Source复用，但同时要求Timeline AnimationTrack拥有Marker、Timeline Clip拥有Foot Placement Curve，并保留独立Pose Source Editor。该口径只统一交互，没有统一“原始素材”的业务owner；本change用Sequence owner取代两类副本，并把主Timeline窗口升级为双文档模式。
- current `character-animation-presentation-authoring`要求Profile-owned Sequence Binding保存AnimationClip、Marker、Curve与Analysis，并要求`Pose Source Editor`作为唯一写入口。本change修改为Binding只引用Sequence，正式写入口改为主Timeline Editor的Sequence文档；Profile Inspector只负责binding关系。
- current `character-animation-presentation-authoring`要求有限Action Marker由Timeline AnimationTrack拥有。本change与该口径冲突并将其删除：Action Segment引用Sequence，脚步Marker、Foot Phase/Weight和Notify一律由Sequence拥有；Timeline只保留动作编排数据。
- current `character-animation-pipeline`把Action Timeline AnimationTrack直接降低为producer binding与marker binding。本change改为Timeline producer引用Sequence plan，marker binding从Sequence解析；Gameplay logic time与Action lifecycle仍由Timeline拥有。
- current `character-animation-foot-analysis-artifact`以AnimationClip与使用点绑定构造Artifact identity。本change保留Clip dependency，但让作者和编译入口先解析唯一Sequence；Artifact本身仍不写回Sequence、Timeline或Profile。
- current `agent-character-controller-synthesis`中的“Marker Sync只能位于AnimationTrack”和`timeline.json/curves.json`完整读写素材Marker/Curve与新owner冲突。本change必须同步Document schema、Exporter、Reconciler、Mutation、Validator，不能只改人工Editor。
- current `btsmtl-agent-authoring-document-sync`把Sequence Binding素材字段放在`presentation/profile.json`，Timeline素材字段放在`editable/timelines/**`。本change新增Sequence独立分片并让两侧只保存稳定引用；整个Character Document仍是一个hash、一个dry-run/apply和一个Undo事务。
- current `btsmtl-timeline-animation-authoring-surface`要求`TimelineEditorOpenRequest`必须直接持有`TimelineData`，并让Timeline Core的Selection/Mutation认识Track/Clip。该合同无法承载Sequence文档；本change把通用部分改成typed document/canvas ports，Action Timeline adapter继续保留完整TimelineData能力。
- current `character-action-animation-authoring-workspace`明确把Clip、Marker与Clip Curve都交给Action AnimationTrack。本change将其拆为Sequence素材owner与Timeline Segment编排owner；Workspace只聚合和导航，不保存镜像。
- current `character-pipeline-definition-authoring`与`character-state-timeline-authoring-loop`仍要求Profile Binding保存resource、Marker、Curve和Analysis。本change把这些内容原子迁入Sequence，Binding只保留精确引用；Corin旧Locomotion Timeline也直接迁入Sequence而不是Binding。
- active `add-generated-foot-phase-animation-sync`正在为Timeline Track与Profile Sequence Binding分别增加Time Mapping和generated warp。该active change与新Sequence唯一owner直接冲突；实施本change前必须把其authoring、Projection和Agent delta重基线为Sequence Marker owner，不能让Time Mapping继续双写在Track与Binding。
- active `add-character-presentation-blend-space`要求Blend Space sample保存AnimationClip与Marker。该active change必须重基线为sample引用Sequence，内部phase plan从Sequence marker解析；不得保留sample marker副本或Inspector内嵌时间轴。
- current `character-animation-presentation-authoring`与`character-presentation-pose-graph`规定PoseState Transition不保存同步开关，而是从两侧source binding自动推导。本change暂时保留该行为，因此本阶段没有“Transition手动选择同步方式”的编辑能力；这不是Timeline/Sequence change中的fallback，后续需要单独proposal决定Transition是否拥有显式policy。

## Dependencies And Sequencing

1. 先以active `add-generated-foot-phase-animation-sync`当前Time Mapping、Marker occurrence与Foot Analysis合同为唯一数据基线，把这些字段整体迁入Sequence，不另建脚相位类型。
2. 再把active `add-character-presentation-blend-space`的sample authoring重基线为Sequence引用，确保Blend Space内部phase和外部PoseState relation都读取同一Sequence Marker。
3. 建立Sequence资产、Sequence compiler plan和Document分片后，迁移Profile Binding、Blend Space sample与Action Timeline Segment；迁移完成前不得删除旧字段。
4. 全部正式owner切换后，一次删除旧Pose Source/Blend Space内嵌时间编辑器、Timeline素材Marker/Curve路径和旧Document字段，不保留双写过渡期。
5. 最后让主Timeline Editor安装Sequence/Action Timeline文档adapter，并让所有Open Source/Open Segment入口只导航到这一窗口。

## Deliberate Scope

- 不在本change重做Pose StateMachine、State Pose Graph或Transition Rule页面。
- 不在本change决定Transition是否显式保存`None | MarkerGroup | GeneratedFootPhase`；现有Compiler继续从两侧Sequence推导同步。
- 不把Sequence Notify变成Gameplay Event、Action Window、Cue、Motion或State transition条件。
- 不把Unity导入AnimationClip内部原生曲线、AnimationEvent或骨骼曲线直接改写为项目作者数据；Sequence只保存项目正式注册的typed channel和Notify。
- 不让Action Timeline修改引用Sequence的Marker、素材Curve、Notify或Analysis配置；双击只负责导航。
- 不创建Sequence到Timeline的复制按钮、隐式Wrapper Timeline、兼容Profile字段或运行时fallback。
- 不自动运行Foot Analysis、Character Build、Program Build或Projection Build。
- 不运行Unity batchmode，不在proposal阶段修改实现代码或生成产品。

## Breaking Changes

- 新增`CharacterAnimationSequenceAsset`及其稳定identity；Profile Sequence Binding、Blend Space sample和Action Timeline Animation Segment全部改为强类型Sequence引用。
- Timeline AnimationTrack删除素材Marker Sync字段；Timeline Animation Clip删除素材Foot Placement Curve与裸AnimationClip owner字段，改为Sequence Segment引用和segment-local Weight/Ease。
- Profile Sequence Binding删除Clip、Loop、PlayRate、Marker、Time Mapping、Foot Placement Weight与Analysis输入副本；只保留Slot、Sequence引用和角色级binding合同。
- Blend Space sample删除裸AnimationClip与Marker副本；Stationary normalized time仍属于sample，素材时间数据来自Sequence。
- Agent Document v3文件闭包和严格schema变化；旧package字段、旧canonical writer和旧Mutation不提供兼容解析。
- 旧`CharacterPoseSourceEditorWindow`、`AnimationTimeFieldAuthoring`和Blend Space Inspector内嵌时间轴删除；所有正式入口切换到主Timeline Editor的Sequence文档。
- 现有内容必须显式迁移。若同一AnimationClip在不同owner上拥有不同Marker、Curve、Loop、PlayRate、Rig或Analysis配置，必须生成不同Sequence并报告差异，不得按Clip或名称静默合并。

## Success Criteria

- 作者打开Run、TurnBack或Stop时进入Sequence文档，在主时间轴直接编辑左右脚Marker、Time Mapping、Foot Placement Weight、Notify和Analysis候选；数据只写入Sequence资产。
- 作者打开Attack或Dodge时进入Action Timeline文档，只编排Sequence Segment、Section、Window、Cue、Motion与Ease；修改Segment不会改写Sequence素材内容。
- 双击Action Segment可以在同一Timeline Editor窗口打开其精确Sequence，返回后仍保持Action Timeline文档与选择上下文。
- Profile、Blend Space与Action Timeline引用同一Sequence时，共享同一Marker/Curve/Notify真相，不再拥有副本或内嵌时间轴。
- Sequence与Action Timeline共用同一UI Toolkit时间尺、Marker/Curve手势、播放控制和预览基础设施，但Mutation、Undo owner、validator和编译链严格分离。
- Agent checkout、dry-run、apply与人工Editor观察到相同Sequence owner；不存在Timeline Track、Profile Binding或Blend Space sample中的旧素材字段。
- 现有Corin内容完成显式迁移后，旧编辑窗口、旧IMGUI时间控件、旧Document字段和旧运行时解析路径全部删除。

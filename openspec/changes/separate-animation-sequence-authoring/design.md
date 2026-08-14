# Design: 分离动画Sequence作者数据与Action Timeline编排

## Context

当前表面与数据关系如下：

```text
Action Timeline
  TimelineData
    -> AnimationTrack
       -> raw AnimationClip
       -> Marker Sync
       -> Animation Clip Weight/Ease/Foot Placement Curve

Continuous Pose Source
  Profile-owned CharacterSequencePoseSourceBinding
    -> raw AnimationClip
    -> Loop/PlayRate
    -> Marker Sync
    -> Foot Placement Weight Curve
    -> Foot Analysis identity

Blend Space
  Sample
    -> raw AnimationClip
    -> Marker
    -> Inspector-embedded AnimationTimeField
```

三条路径都在描述AnimationClip时间域，却各自保存一份作者语义。当前`AnimationTimeFieldAuthoring`也不是Timeline主时间轴的复用：它是一份约九百行的独立IMGUI交互实现，只有Pose Source Editor和Blend Space Inspector使用；完整的UI Toolkit `TimelineFieldView/TimelineTrackView`仍然直接依赖`TimelineData/Track/Clip`。

目标结构采用UE的职责划分，但保留项目现有名称与运行边界：

```text
CharacterAnimationSequenceAsset
  -> raw AnimationClip
  -> Marker / typed Curve / Notify / Analysis

Action Timeline
  -> Sequence Segment references
  -> Section / Window / Cue / Motion / Decision

Pose Source Binding / Blend Space Sample
  -> Sequence references

Pose StateMachine
  -> 决定何时采样Sequence
  -> 本change仍从两侧Sequence自动推导同步计划
```

## Goals

- 一个原始动画素材只有一个正式Marker、素材Curve、Notify和Analysis owner。
- Sequence与Action Timeline共享同一个成熟的主时间轴交互，不共享可写数据对象。
- Action动作编排不能改写素材内容，Sequence编辑不能产生Gameplay事实。
- 人工Editor、Agent Document、Validator、Compiler、Preview与Runtime看到同一owner关系。
- 迁移完成后删除旧字段、旧窗口、旧IMGUI时间控件和旧编译路径。

## Non-Goals

- 不在本change实现Pose StateMachine工作区或Transition Rule图。
- 不在本change增加Transition显式同步policy。
- 不把Notify接入Gameplay Timeline或BTSMTL事件系统。
- 不建立通用AnimationClip资产数据库、目录扫描或按名称自动绑定。
- 不改变Foot Grounding、IK、Motion Matching或AnimationSlot的业务职责。

## Data Ownership

| 数据 | 唯一owner | 消费者 | 禁止副本 |
| --- | --- | --- | --- |
| raw AnimationClip、Rig、Loop/Finite、默认倍率 | Animation Sequence | Sequence Player、Blend Space、Action Segment | Profile Binding、Blend Space Sample、Timeline Clip |
| Marker Sync、Time Mapping、Point Marker | Animation Sequence | PoseState relation、Blend Space phase、Action Slot relation | Timeline Track、Transition、Profile Binding、sample |
| 素材Foot Placement Weight与其它registered Sequence Curve | Animation Sequence | Presentation Projection与Sequence sampler | Timeline Clip、Profile Binding |
| Sequence Notify | Animation Sequence | Presentation-only notify snapshot/preview | Gameplay Timeline、ActionProfile、StateMachine |
| Segment Start/End、ClipIn、Extrapolation、Weight、Ease | Action Timeline Sequence Segment | Action presentation sampler | Sequence |
| Section | Action Timeline | Timeline导航与有限动作段落编排 | Sequence Marker、TreeClip Decision |
| Action Window、Cue、Motion、Warp、Decision | Action Timeline | Gameplay Program或对应正式consumer | Sequence |
| Source Slot到Sequence关系 | Profile-owned Sequence Binding | Projection Compiler | Pose Graph Player、Timeline |
| Blend sample位置、角色、Stationary time | Blend Space Sample | Blend Space plan | Sequence |

## Decision 1: 新建一等Sequence资产，而不是继续把Sequence等同于Profile Binding

Profile Binding表达“这个角色的Source Slot使用哪份素材”，不是素材本身。把Marker和Curve留在Binding会导致同一Run素材被Pose State、Blend Space和Action引用时出现多份时间数据，也无法从Action Segment稳定导航到唯一素材文档。

一等Sequence资产使Run、TurnBack、Stop等成为可直接打开和复用的作者文档。代价是需要迁移现有Binding和Timeline数据，并增加一种正式资产与Document分片；收益是业务owner清晰，后续状态机Transition、Blend Space和Action都消费同一素材语义。

## Decision 2: 主Timeline Editor承载两种文档，不把Sequence包装成TimelineData

Sequence文档与Action Timeline共用窗口外壳和时间交互，但不能为了复用现有View而给Sequence临时构造`TimelineData`。临时Timeline会制造第二owner、错误Undo和潜在运行时Track。

正式结构分为三层：

```text
AnimationTimeEditorWindow
  -> document header / tabs / breadcrumbs / playback controls / details / tools
  -> AnimationTimeCanvas
       time ruler / geometry / scroll / zoom / playhead
       span / point / curve lane host
       selection / pointer draft / keyboard / clipboard
  -> IAnimationTimeDocumentAdapter
       SequenceDocumentAdapter
       ActionTimelineDocumentAdapter
```

`AnimationTimeCanvas`只处理Editor交互语义，不读取`TimelineData`、`CharacterAnimationSequenceAsset`、`Track`或`Clip`。每个adapter提供稳定document identity、duration/frame rate、lane descriptor、selection projection、typed mutation、Preview binding与Diagnostics。

现有`TimelineEditorWindow/TimelineEditorView/TimelineFieldView`逐步收敛到上述结构，保留主窗口入口和现有Timeline能力；不是新建第二个通用Window。完成后旧`AnimationTimeField`删除。

## Decision 3: Lane使用typed descriptor，不建立统一序列化DTO

Canvas只认识三类交互：

```text
Span  : start/end/clip-in/resize/move
Point : frame/identity/label
Curve : typed channel/full curve/value domain
```

descriptor必须提供稳定owner identity、显示、time mapping、完整读取、合法手势和正式Mutation。它们只是Editor projection：

- Sequence Marker与Notify都是Point，但使用不同typed identity与Mutation。
- Action Section也是Point，但owner为Timeline且不参与Marker Sync。
- Action Window和Sequence Segment是Span，但运行消费者不同。
- Sequence Curve与Timeline Segment Weight/Ease都是Curve，但进入不同compiler/runtime。

不创建`GenericTimeElement`资产、不新增统一Runtime evaluator，也不让adapter用反射或SerializedProperty path发现字段。

## Decision 4: Action Timeline引用Sequence，不允许素材覆盖字段

`Sequence Segment`只保存：

```text
SegmentAuthoringId
Sequence reference
StartFrame
EndFrame
ClipInFrame
Extrapolation
Weight
EaseIn
EaseOut
```

Segment可以裁剪、平移、重叠和混合，但不能覆盖Sequence Marker、Notify、Loop、Rig、Analysis或素材Curve。若业务需要相同AnimationClip的另一套Marker/Curve，作者必须创建另一份明确Sequence；不能在Segment内加override。

这增加Sequence资产数量，但让Action Timeline的职责保持可解释：它组织素材，不修改素材。

## Decision 5: Sequence Notify保持纯表现

Sequence Notify用于标记素材时间上的表现事件，例如脚步声、VFX提示或编辑器标注。首版合同包含稳定Notify identity、typed Notify kind、整数frame和typed payload；只有注册了正式presentation consumer的Notify kind可创建。

Notify不得生成Gameplay Fact、Action Window、Cue、Motion、Warp或State transition。需要影响Gameplay的事件继续放在Action Timeline正式Track中。这样不会因表现帧率、插值、回放或rollback造成Gameplay事件重复。

## Decision 6: Section属于Action Timeline但不复制Decision系统

Section用于命名一个稳定Timeline frame、快速导航和描述动作段落。首版不让Section自行执行分支，也不替代现有Decision/TreeClip：

```text
TimelineSection
  SectionAuthoringId
  Name
  Frame
```

若后续需要Montage式Section跳转，必须另行定义它如何与Gameplay logic time、Decision和rollback交互；本change不预埋不可执行的跳转字段。

## Decision 7: Profile与Blend Space只引用Sequence

Profile Sequence Binding保存精确Source Slot、Sequence对象引用和角色级binding identity。Sequence内的Rig必须与Profile正式Rig兼容；Binding不再重复Rig、Clip、Marker、Curve和Analysis配置。

Blend Space Dynamic sample引用Sequence；Stationary sample仍额外保存fixed normalized time，因为这是sample在Blend Space中的使用方式，不是素材内容。Blend Space phase compiler从每个Sequence解析Marker与Time Mapping，sample不保存Marker副本。

## Decision 8: Preview共享控制基础设施，执行链按文档分开

Sequence Preview：

```text
Sequence authoring
  -> exact Sequence plan
  -> presentation-only source sample
  -> rig/pose preview
  -> read-only Marker/Notify/Analysis overlay
```

Action Timeline Preview：

```text
Timeline authoring
  -> Action playback fixture
  -> Sequence Segment sample
  -> AnimationSlot / Routing / Pose Plan
  -> final pose preview
```

两者共用播放、暂停、seek、速度、游标和Preview target ownership，但session adapter不同。Sequence Preview不创建ActionInstance；Action Preview不执行Gameplay TreeClip、Window、Motion、Warp或Cue副作用。缺少匹配Projection或Rig时显示Unavailable，不临时编译或回退裸Animancer Play。

## Decision 9: Details只做精确属性，不再嵌入时间编辑器

Details可编辑当前Marker frame/label、Notify payload、Curve key数值、Segment范围/ClipIn、Section名称/frame及Track属性。所有需要空间关系的操作都在中央Canvas完成。

Profile Inspector、Blend Space Details、Timeline Clip Inspector不得再嵌入time ruler、Marker lane、Curve lane或独立播放游标。它们只显示引用、摘要和`Open Sequence`/`Open Timeline`导航。

## Decision 10: Agent Document同步成为Sequence正式owner

Document v3新增：

```text
editable/animation-sequences/<stable-segment>/
  sequence.json
  curves.json
```

`sequence.json`保存Sequence identity、正式对象引用、loop/topology、Marker、Notify、Analysis引用；`curves.json`保存registered Sequence Curve完整payload。文件对属于同一Character Document manifest、editable hash、dry-run/apply和Undo事务。

其它分片只保存Sequence引用：

- `presentation/profile.json`：Source Slot -> Sequence。
- Blend Space资产目标：Sample -> Sequence。
- `editable/timelines/**/timeline.json`：Sequence Segment -> Sequence。

Exporter、Package Codec、Reconciler、planning symbol、Mutation handler和Validator必须同时支持现有稳定Sequence和`local:*`新Sequence。不存在`create_sequence` MCP工具；AI仍修改Document文件并走五个生命周期工具。

## Migration

迁移先建立候选内容签名：

```text
raw AnimationClip object identity
Rig identity
Loop/Finite
Default Play Rate
Time Mapping
Marker Group/Topology/Role/ordered Marker
registered material curves
Analysis Source identity
Notify set
```

只有完整签名相同的旧owner才可以合并到同一Sequence。只要任一字段不同，就创建不同Sequence并用业务来源生成可读名称；不得仅按AnimationClip、目录或显示名合并。

迁移顺序：

1. 收集Profile Sequence Binding、Blend Space sample与Action Timeline Animation Clip的精确旧数据。
2. 创建Sequence资产并写入完整素材作者数据。
3. 把Profile、Blend Space和Timeline替换为强类型Sequence引用。
4. 编译/Validator按新引用解析，Document reverse export输出新owner。
5. 删除旧字段、旧mutation、旧editor、旧codec与旧runtime reader。

Action Timeline旧Track级Marker可能覆盖多个不同Animation Clip。该数据无法无损归入单一Sequence时迁移必须失败并列出Track、Clip和Marker coverage；作者需要先把Track拆成明确Sequence语义，迁移器不得把Track Marker复制到每个Clip或选择权重最高Clip。

## Validation

Sequence Validator检查：

- stable identity与资源引用唯一。
- AnimationClip、Rig、duration、frame rate、Loop/Finite与play rate合法。
- Marker mode、Time Mapping、group、topology、role、identity、顺序、边界与directed pair完整。
- registered Curve完整、有限、time/value domain合法。
- Notify kind已注册，identity唯一，frame合法，payload符合typed schema。
- Analysis Source与Sequence Clip/Rig匹配。

Action Timeline Validator检查：

- Segment引用Sequence可解析且Rig/Action binding兼容。
- Start/End、ClipIn、Extrapolation、Weight/Ease合法。
- Section identity/name/frame唯一合法。
- Timeline不再携带素材Marker/Notify/Foot Placement Curve残留。

跨owner Validator检查Profile、Blend Space、Action Timeline与Sequence引用闭包，不按名称或路径猜测目标。

## OpenSpec Reconciliation

- `btsmtl-timeline-editor-preview`中Track-owned Marker、Timeline Clip素材Curve和独立Pose Source Editor要求改为Sequence owner与双文档主窗口。
- `character-animation-presentation-authoring`中Profile Binding-owned Clip/Marker/Curve/Analysis改为Sequence reference；Marker/Curve唯一owner改为Sequence。
- `character-animation-pipeline`中Action Track直接携带marker binding改为Action Segment解析Sequence plan。
- `character-animation-foot-analysis-artifact`保留AnimationClip dependency，但authoring source identity升级为Sequence identity。
- `agent-character-controller-synthesis`与`btsmtl-agent-authoring-document-sync`必须新增Sequence文件对并删除Timeline/Profile旧素材字段。
- active generated foot phase与Blend Space change必须先重基线，否则会把Time Mapping和sample Marker继续写入即将删除的owner。

## Risks

- 现有Timeline主视图对`TimelineData/Track/Clip`耦合较深。实施必须先提取Canvas port，再接Sequence adapter；不得复制`TimelineFieldView`作为Sequence View。
- 旧Action Track级Marker可能无法映射到单一Clip。迁移必须明确失败，不能静默丢数据或复制到多个Sequence。
- Sequence Notify若缺少正式presentation consumer，首版只能拒绝该kind，不能用Unity AnimationEvent或反射调用作为fallback。
- Agent Document增加文件闭包会触发schema breaking change；所有strict parser/writer/reconciler必须一次切换，不能保留旧package reader。

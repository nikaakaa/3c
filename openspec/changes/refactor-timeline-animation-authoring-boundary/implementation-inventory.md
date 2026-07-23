# Timeline Animation作者边界实施清单

## 1. 最终所有权

| 数据 | 唯一所有者 | Timeline可写 | Player读取 |
|---|---|---:|---:|
| Animation Clip与播放区间 | Timeline Animation Clip | 是 | 是 |
| Sync Marker | AnimationTrack | 是 | Projection marker map |
| Foot Placement Weight | Timeline Animation Clip | 是 | Projection clip binding |
| Analysis Source参数 | Analysis Source资产 | 否 | 否 |
| Foot Analysis Artifact | `Library/CharacterFootAnalysis` | 否 | 否 |
| Foot Contact Candidate | Timeline Analysis Editor Session | 否 | 否 |
| Runtime Foot Feature | Presentation Projection | 否 | 是 |

旧FOOT ANALYSIS lane没有独立serialized作者数据，因此本次直接删除，不保留migrator、兼容reader或双写路径。

## 2. Timeline Editor作者边界

新增正式合同：

- `TimelineEditorOpenRequest`
- `TimelineEditorSessionContext`
- `ITimelineEditorSelectionPort`
- `ITimelineEditorMutationPort`
- `ITimelineEditorFrameGeometryPort`
- `ITimelineAnimationMarkerSyncAuthoringContext`
- `ITimelineEditorRuntimeDebugBinding`
- `ITimelineEditorToolProvider`
- `TimelineEditorToolCatalog`

Timeline窗口不再保存或cast无类型`object AuthoringContext`。Graph窗口只在创建OpenRequest之前把自身领域上下文投影为Marker topology、Tool Catalog与Runtime Debug三个精确端口。独立打开Timeline时没有外部上下文，Clip、Marker与editable Curve仍可编辑。

删除文件：

- `TimelineFootAnalysisAuthoringContext.cs`
- `TimelineFootAnalysisLaneView.cs`
- `CharacterPipelineFootAnalysisAuthoringContext.cs`

同步删除Track summary、四组metric行、layout高度、foldout状态、USS和完整Definition Rebuild入口。

## 3. Artifact合同与Store

正式实现：

- `AnimationFootAnalysisArtifact.cs`
- `AnimationFootAnalysisArtifactCodec.cs`
- `AnimationFootAnalysisArtifactStore.cs`
- `AnimationFootAnalysisArtifactBuilder.cs`

Artifact identity包含：

- AnimationClip GUID与dependency hash。
- Analysis Source GUID、dependency hash、SourceId与AnalysisVersion。
- Sampling Rig GUID与dependency hash。
- Calibration GUID、Id与Revision。
- sample rate、plant阈值、landing参数、curve reduction参数与algorithm version。

当前artifact format version为`2`，算法身份为`animation-foot-analysis/v4`。v4先完成全部heel/toe/sole位置与高度采样，再在第二阶段用完整位置序列计算循环中心差分速度；旧v3在采样当前帧时读取尚未写入的未来帧，生成了错误的高速度并漏判Plant，因此必须按algorithm identity判为Stale并重新生成。Codec固定编码左右脚九组feature curve、wrap mode、key、tangent、weight和weighted mode。Reader拒绝未知版本、非法长度、非法枚举、非有限值、无序key、identity hash错误、payload hash错误与尾随字节。Store只写`Library/CharacterFootAnalysis`，使用临时文件、回读校验和原子替换发布；状态严格区分Missing、Stale、Ready与Corrupt。

## 4. 单Clip分析链

```text
AnimationClip + CharacterFootPlacementAnalysisSource
  -> 精确解析Sampling Rig与Calibration
  -> PreviewScene + Animator + Playable手动采样
  -> 统一Calibration地面、左右脚heel/toe、sole速度、高度、plant、landing计算
  -> 确定性curve reduction
  -> canonical artifact
```

`CharacterFootPlacementAnimationAnalyzer`只接受`AnimationClip + Analysis Source`，不遍历Timeline，也不知道Definition、RootTree、Program、Projection或Artifact Store。Analyzer先采齐全部位置和高度，之后才计算循环中心差分速度。Plant判断使用Calibration地面与sole垂直速度，局部水平速度继续作为生成轨迹供Runtime合成。Timeline面板和Definition Build都只调用唯一`AnimationFootAnalysisArtifactBuilder`。

## 5. Definition发布链

正式实现：

- `CharacterProjectionFootAnalysisResolver.cs`
- `CharacterSimulationBuildOrchestrator.cs`
- `CharacterSimulationProgramBuildService.cs`

```text
Character Definition Build
  -> validated Semantic IR建立Presentation Semantic Contract
  -> 收集可达Timeline/Track/Clip stable binding
  -> 按clip与Source identity归并artifact resolve
  -> Ready回读校验 / Missing与Stale生成 / Corrupt拒绝
  -> Projection Compiler从Semantic IR + authoring inventory + analysis生成唯一target-neutral Projection
  -> artifact aggregate content hash进入ProjectionRevision
  -> 请求的Float32/Fixed Target Adapter分别从同一Semantic IR生成Program
  -> ContractHash交叉校验后，全部Target与唯一Projection同一事务原子发布
```

DryRun只检查artifact状态，不触发采样。正式Build可生成Missing或Stale artifact。任一artifact或binding失败都不会发布部分Projection。新增`Tools/3C/Build/Compile All Stale Character Simulation Programs`只是同一`Build -> Orchestrator`的批量入口，没有第二套Compiler。

## 6. Timeline按需工具

`CharacterTimelineAnimationAnalysisTool.cs`通过显式Tool Provider装配到既有Timeline窗口底部工具区，不创建新EditorWindow。工具默认关闭，关闭时不读取artifact；打开后只消费当前选中的Animation Clip与显式Analysis Source。

工具支持：

- Missing、Stale、Ready、Corrupt与精确identity/path显示。
- Left或Right单脚选择。
- Speed、Height、Plant或Landing单metric只读曲线。
- `Rebuild Selected Clip`只重建当前artifact。
- Ready artifact按sample rate重采样左右脚PlantConfidence。上升沿之后必须连续保持Plant，持续样本数由Analysis Source的`MinimumLandingSegmentSeconds`与artifact采样步长精确换算；单帧阈值穿越不再成为contact candidate。
- Candidate携带artifact identity/content hash、Timeline/Track/Clip stable identity、脚侧、归一化时间、目标frame与置信值。
- 只读曲线叠加Left/Right candidate位置；候选本身不进入Timeline selection、Undo或序列化数据。
- Candidate生成前精确核对当前AnimationClip GUID、dependency hash与v4 algorithm identity；Track尚未配置MarkerGroup/Cyclic或存在多Clip时仍显示只读骨骼候选，只禁用Apply并报告映射约束。
- 单AnimationClip完整覆盖MarkerGroup/Cyclic Track时，作者可显式确认并Apply候选。

工具不注册Curve Channel，不反向搜索Definition，也不改变主时间轴行高、滚动范围或marker命中区域。从Character Pipeline Graph打开时，Tool Catalog只注入该Definition Profile显式引用的Source GUID。生成分析与候选展示保持只读；唯一写入口是作者显式确认后的`TimelineEditorSessionContext.Apply`，它重新读取artifact并重建proposal，拒绝任何identity、dependency、采样参数或Timeline映射变化，只替换`LeftFootContact`与`RightFootContact`集合并保留其它Marker。Apply优先复用脚侧与目标frame都匹配的stable marker identity，其次复用同脚侧identity，并删除不再需要的脚步Marker。不存在直接track数组写入、自动保存或候选序列化类型。

## 7. Runtime链

```text
Visible Animation Contribution
  -> Marker Sync后的VisualSampleTime
  -> Presentation Projection stable clip binding
  -> 左右脚feature sample
  -> Foot Placement Planner
  -> Ground Envelope / Constraint / Pelvis / Rotation
  -> Final IK
```

Runtime程序集不引用Artifact Store、Analysis Source或Editor Analyzer。Library artifact可删除而不影响已发布Player。Artifact不进入Program ABI、Snapshot、StateHash或网络包。`Foot Placement Weight`仍是每个Animation Clip唯一可写的IK介入曲线。

## 8. Agent与诊断

Agent Snapshot只输出Analysis Mode、Source GUID、SourceId、SourceVersion与AlgorithmVersion摘要。Agent Patch继续不能读取或修改candidate，也不能触发artifact Rebuild；Sole Speed、Height、Plant和Landing没有Patch operation。候选经作者确认成为正式AnimationSyncMarker后，Agent只通过v15既有Marker操作与Validator观察和修改最终作者数据。Validator通过正式Build DryRun复用artifact和Projection binding诊断；Compile Report区分artifact Missing、Stale、Corrupt与Projection binding错误。

## 9. Corin正式迁移结果

- Analysis Source：`CorinFootPlacementAnalysisSource.asset`
- Sampling Rig：`CorinFootPlacementAnalysisRig.prefab`
- Calibration：`CorinFootPlacementRigCalibration.asset`
- 可达Animation Clip binding：19个。
- 19/19可达Animation Clip已通过唯一Builder生成`animation-foot-analysis/v4` artifact；v3及更早算法产物只能判为Stale，不再作为当前候选或发布输入。
- Projection artifact aggregate content hash：`6aa73e203604bbe96a58e9fd73a7e15887531d666e1245eff980cf1d90bb8930`。
- Projection revision：`31788f722760d3e02a63862df6cbd3018814eeaf6be86adea772a30d6e956eb2`。
- Float32与Fixed SourceRevision：`0a3900b32f99b968a4e82ea9974198aee7e807f71b17569cb1a5ae215533c9a4`。
- Float32与Fixed SemanticHash：`bfe0ebc2cbae4a4b18a013d88920fc14a4bf4997ca5f7264058b2230b451d5a1`。
- Projection semantic contract hash：`4e06592984d8bba455337003779ba1213687c0888a0066f0a2d0cfabc353a62d`。
- Float32 ProgramHash：`c9569e15302ab2bb1e3c25e8c42e162b4342d0e99c42e35d026d8edd29e829c0`。
- Fixed ProgramHash：`aa8068d033b1ec6be4bf80e283277a79429268daa441c4f151b6b6cca0fad0ec`。
- Corin WalkLoop正式Marker：RightFootContact frame `2`，LeftFootContact frame `18`。
- Corin RunLoop正式Marker：RightFootContact frame `1`，LeftFootContact frame `16`。
- 四个Marker通过Agent `agent-character-controller-synthesis.v17`的正式snapshot、dry-run与apply事务写入并保留stable marker identity，没有直接修改Graph YAML。

Standalone、Deterministic Rollback、Unity Authority与DotRecast Runtime Profile都引用Calibration GUID `471a3432ddd640c187b4ebfdf8c94e69`。

## 10. 验证状态

- Analyzer保持Animator启用，并使用v4两阶段采样与速度求导；Walk/Run的左右脚PlantConfidence已产生独立稳定接触段。
- Candidate proposal按`MinimumLandingSegmentSeconds`过滤单帧阈值穿越；RunLoop不再产生额外LeftFootContact候选。
- 已删除`TimelineTrackHandle.uss`中最后残留的旧`footAnalysis*` lane样式，不存在隐藏CSS兼容入口。
- Runtime与Editor程序集均以`--disable-build-servers /nr:false /p:UseSharedCompilation=false`编译通过，完成后已关闭MSBuild与编译器服务器。
- 正式Agent marker事务、19/19 v4 artifact、Presentation Projection、Float32 Program与Fixed wrapper已经统一发布到同一SourceRevision。
- 旧Corin `frame 0/半周期` Marker、v3及更早artifact只算历史配置，不作为候选算法或完成验收真相。
- 未运行Unity batchmode。
- 未新增测试。

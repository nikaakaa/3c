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

当前artifact format version为`2`，算法身份为`animation-foot-analysis/v3`。v3保证Preview Rig的Animator在Playable手动采样期间保持启用；旧v2产物必须按algorithm identity判为Stale并重新生成。Codec固定编码左右脚九组feature curve、wrap mode、key、tangent、weight和weighted mode。Reader拒绝未知版本、非法长度、非法枚举、非有限值、无序key、identity hash错误、payload hash错误与尾随字节。Store只写`Library/CharacterFootAnalysis`，使用临时文件、回读校验和原子替换发布；状态严格区分Missing、Stale、Ready与Corrupt。

## 4. 单Clip分析链

```text
AnimationClip + CharacterFootPlacementAnalysisSource
  -> 精确解析Sampling Rig与Calibration
  -> PreviewScene + Animator + Playable手动采样
  -> 统一Calibration地面、左右脚heel/toe、sole速度、高度、plant、landing计算
  -> 确定性curve reduction
  -> canonical artifact
```

`CharacterFootPlacementAnimationAnalyzer`只接受`AnimationClip + Analysis Source`，不遍历Timeline，也不知道Definition、RootTree、Program、Projection或Artifact Store。Plant判断使用Calibration地面与sole垂直速度，局部水平速度继续作为生成轨迹供Runtime合成。Timeline面板和Definition Build都只调用唯一`AnimationFootAnalysisArtifactBuilder`。

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
- Ready artifact按sample rate重采样左右脚PlantConfidence，生成非接触到稳定接触上升沿的瞬时contact candidate。
- Candidate携带artifact identity/content hash、Timeline/Track/Clip stable identity、脚侧、归一化时间、目标frame与置信值。
- 只读曲线叠加Left/Right candidate位置；候选本身不进入Timeline selection、Undo或序列化数据。
- Candidate生成前精确核对当前AnimationClip GUID、dependency hash与v3 algorithm identity；Track尚未配置MarkerGroup/Cyclic或存在多Clip时仍显示只读骨骼候选，只禁用Apply并报告映射约束。
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

## 9. Corin迁移基线与待发布产物

- Analysis Source：`CorinFootPlacementAnalysisSource.asset`
- Sampling Rig：`CorinFootPlacementAnalysisRig.prefab`
- Calibration：`CorinFootPlacementRigCalibration.asset`
- 可达Animation Clip binding：19个。
- v3算法切换前最后一次发布的v2 artifact为19个。它们与旧Projection只作为迁移基线；v3 identity会把它们判为Stale，不能作为当前候选或验收输入。
- 迁移前Projection artifact aggregate hash：`49180844a582243e65fde7db744eeb83941c54cf554a042d4256760de0fb6503`。
- 迁移前Projection semantic contract hash：`7be2b230cb37eb76d7d40005ed7ae8590641adb157958499659e715d9a36d432`。
- 迁移前Projection revision：`bf0904a2355e1fd189bee0e880079c106b960b999e24e65d346134016a355e14`。
- 迁移前Float32与Fixed SourceRevision：`7e3b3866f0f2416366a7685cbad14db9faa0206991f0dcfc5520c64ceee1222f`。
- 迁移前Float32与Fixed SemanticHash：`d3fa1c4a20be895790d34796f8bf1b23f46ed0e7426ea733c3f2dd64ef128572`。
- 迁移前Float32 ProgramHash：`ebedd28030011ad59aa5d2aa57616686e9137058e191e3bfee1b88f06d468170`。
- 迁移前Fixed ProgramHash：`0e130e749b0de6dc30bf205699522cd12469c22639ab8b666ba91d48d1b2e39a`。
- v3正式artifact aggregate、Projection revision、Program hash和WalkLoop/RunLoop Marker frame必须在整体编译绿后由正式Builder与Agent v15流程生成，再回填本节；不得沿用上述迁移前值。

Standalone、Deterministic Rollback、Unity Authority与DotRecast Runtime Profile都引用Calibration GUID `471a3432ddd640c187b4ebfdf8c94e69`。

## 10. 验证状态

- 前一版Foot Analysis基础链曾完成Runtime/Timeline Editor/Client Editor静态编译与19/19 v2 artifact发布，但这些结果不代表本节新增Marker候选闭环已经验收。
- Analyzer保持Animator启用、v3 algorithm identity、candidate proposal与Analysis面板显式Apply代码已经落盘；任务状态继续保持未完成，直到共享程序集恢复编译、Unity刷新和正式资产迁移完成。
- 已删除`TimelineTrackHandle.uss`中最后残留的旧`footAnalysis*` lane样式，不存在隐藏CSS兼容入口。
- 当前共享Unity编译仍由并行AnimationChannelId/AnimGraph迁移收口；本change不恢复旧AnimationLayerSelection、Transition或Equipment类型，也不复制Simulation Core合同。
- 等整体编译绿后，必须重新生成v3 artifact并确认Walk/Run左右脚PlantConfidence不再同形，再通过Agent v15正式流程迁移Corin WalkLoop/RunLoop Marker并重建Projection。
- 迁移前的Corin `frame 0/半周期` Marker与旧v2 artifact只算历史配置，不作为候选算法或完成验收真相。
- 未运行Unity batchmode。
- 未新增测试。

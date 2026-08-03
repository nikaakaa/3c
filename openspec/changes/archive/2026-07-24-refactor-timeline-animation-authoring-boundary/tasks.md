## 1. 基线与所有权确认

- [x] 1.1 记录现有Foot Analysis从Definition Build到Projection再回读Timeline的完整调用链。
- [x] 1.2 记录现有Timeline Foot Analysis Context、lane、layout、USS和Rebuild入口的全部文件。
- [x] 1.3 记录现有Analyzer真正需要的AnimationClip、Analysis Source、Sampling Rig和Calibration输入。
- [x] 1.4 记录Projection中左右脚feature payload、identity与Runtime sampler的现有结构。
- [x] 1.5 记录CharacterSimulationProgramBuildService当前触发动画分析和发布的调用点。
- [x] 1.6 确认Foot Analysis UI没有独立serialized作者数据，允许直接删除旧lane而无需资产兼容。
- [x] 1.7 确认Marker Sync、Foot Placement Weight、Distance Curve和generated feature的唯一所有权。
- [x] 1.8 将实施清单与current specs冲突更新到implementation-inventory.md。
- [x] 1.9 定义Timeline Editor本地作者表面的固定职责。
- [x] 1.10 定义typed `TimelineEditorOpenRequest`。
- [x] 1.11 定义typed `TimelineEditorSessionContext`。
- [x] 1.12 定义本地selection、mutation transaction与frame geometry端口。
- [x] 1.13 定义独立Marker topology context端口。
- [x] 1.14 定义独立Runtime Debug binding端口。
- [x] 1.15 定义显式`TimelineEditorToolProvider`合同。
- [x] 1.16 定义不使用反射的`TimelineEditorToolCatalog`装配入口。
- [x] 1.17 删除Timeline Editor无类型`object AuthoringContext`合同。
- [x] 1.18 迁移全部AuthoringContext cast消费到精确typed端口。
- [x] 1.19 确认Timeline Core程序集不引用Character、Foot Placement或Projection实现。
- [x] 1.20 确认独立Timeline未提供任何外部context时仍可完整编辑Clip、Marker与Curve。

## 2. Animation Foot Analysis Artifact合同

- [x] 2.1 新增artifact format version与稳定identity类型。
- [x] 2.2 将AnimationClip GUID与import dependency纳入identity。
- [x] 2.3 将Analysis Source GUID、SourceId与AnalysisVersion纳入identity。
- [x] 2.4 将Sampling Rig GUID与dependency hash纳入identity。
- [x] 2.5 将Rig Calibration Id与Revision纳入identity。
- [x] 2.6 将sample rate、threshold、reduction与algorithm version纳入identity。
- [x] 2.7 定义左右脚feature curve set的canonical artifact payload。
- [x] 2.8 定义artifact payload hash与完整identity校验。
- [x] 2.9 定义Missing、Stale、Ready与Corrupt状态，不把Corrupt降级为Missing fallback。
- [x] 2.10 保证artifact合同不引用Tree、Definition、Program、Projection或Runtime Presentation对象。

## 3. Artifact Codec与Store

- [x] 3.1 实现artifact canonical writer。
- [x] 3.2 实现artifact canonical reader。
- [x] 3.3 对全部浮点、曲线key、wrap mode和左右脚顺序定义固定编码。
- [x] 3.4 Reader拒绝未知format version。
- [x] 3.5 Reader拒绝非法长度、非法枚举、NaN、Infinity和无序key。
- [x] 3.6 Reader重新计算并校验payload hash。
- [x] 3.7 建立固定`Library/CharacterFootAnalysis`存储根。
- [x] 3.8 由Analysis Source identity、Clip GUID与artifact hash推导唯一文件路径。
- [x] 3.9 使用临时文件与原子替换发布artifact。
- [x] 3.10 禁止Store写入Assets、Packages、Build、StreamingAssets或YooAsset输出。
- [x] 3.11 实现按精确expected identity检查Missing、Stale、Ready与Corrupt。
- [x] 3.12 删除按文件名、时间戳、clip名称或最近产物猜测的可能路径。

## 4. 单AnimationClip Analyzer

- [x] 4.1 将Analyzer入口收敛为`AnimationClip + Analysis Source`。
- [x] 4.2 从Analysis Source精确解析Sampling Rig GUID。
- [x] 4.3 验证Sampling Rig与Analysis Source引用同一Calibration identity/revision。
- [x] 4.4 保留Preview Scene、Animator和Playable手动采样。
- [x] 4.5 保留左右脚sole pose、速度、高度、plant和landing生成算法。
- [x] 4.6 保留确定性curve reduction。
- [x] 4.7 让相同输入重复分析产生相同canonical payload。
- [x] 4.8 从Analyzer删除Timeline集合遍历。
- [x] 4.9 从Analyzer删除Definition、RootTree、Program和Projection依赖。
- [x] 4.10 让分析错误精确定位Clip、Source、Sampling Rig、Calibration和算法阶段。
- [x] 4.11 建立唯一Artifact Builder，供Timeline面板与Definition Build共同调用。
- [x] 4.12 禁止Editor repaint、selection或foldout隐式触发采样。

## 5. Definition Artifact Resolver与Projection发布

- [x] 5.1 在Compiler orchestration中保留可达Timeline/Track/Clip stable binding收集。
- [x] 5.2 将相同AnimationClip与Analysis Source归并为一次artifact resolve。
- [x] 5.3 对Ready artifact重新校验完整identity与payload hash。
- [x] 5.4 对Missing或Stale artifact调用唯一Artifact Builder。
- [x] 5.5 对Corrupt artifact报告明确错误并拒绝发布。
- [x] 5.6 将artifact feature按stable Timeline/Track/Clip identity绑定到Projection producer clip。
- [x] 5.7 保持同一AnimationClip在多个binding中的独立stable identity。
- [x] 5.8 将artifact content hash纳入ProjectionRevision。
- [x] 5.9 保持纯分析变化不改变Gameplay SourceRevision、SemanticHash或ProgramHash。
- [x] 5.10 保持Program与Projection同一Build Transaction原子发布。
- [x] 5.11 任一clip artifact失败时不发布部分Projection或一半Program reference。
- [x] 5.12 删除Projection Build内部第二套动画采样与cache逻辑。
- [x] 5.13 确认Runtime Projection payload不保存artifact路径、Analysis Source GUID或Sampling Rig依赖。

## 6. Timeline旧Foot Analysis UI删除

- [x] 6.1 删除`ITimelineFootAnalysisAuthoringContext`。
- [x] 6.2 删除`CharacterPipelineFootAnalysisAuthoringContext`。
- [x] 6.3 删除`TimelineFootAnalysisLaneView`。
- [x] 6.4 删除TimelineTrackView中的Foot Analysis summary、header、lane和Rebuild逻辑。
- [x] 6.5 删除TimelineTrackHandle中的四组metric label。
- [x] 6.6 删除TimelineRendering中的Foot Analysis header/lane高度与展开状态。
- [x] 6.7 删除Timeline Curve Editor Session中的Foot Analysis foldout状态。
- [x] 6.8 删除Foot Analysis专用USS class与布局规则。
- [x] 6.9 删除“Open this Timeline from a Character Pipeline graph”提示路径。
- [x] 6.10 删除Timeline调用完整CharacterSimulationProgramBuildService的入口。
- [x] 6.11 确认删除后AnimationTrack高度只由Clip、Marker和editable Curve分组决定。
- [x] 6.12 确认Timeline marker、curve、clip选择与Inspector绑定不再引用Foot Analysis lane。

## 7. Timeline Animation Analysis按需面板

- [x] 7.1 在既有Timeline窗口增加Analysis工具开关，不创建新EditorWindow。
- [x] 7.2 面板默认关闭且关闭时不读取artifact。
- [x] 7.3 面板从当前选中的Animation Clip取得精确AnimationClip引用。
- [x] 7.4 面板提供显式Analysis Source对象选择。
- [x] 7.5 从Profile/Graph打开时只注入精确Profile Source作为初始选择。
- [x] 7.6 独立Timeline未选择Source时显示`Analysis Source Required`。
- [x] 7.7 禁止面板反向搜索引用Timeline的Definition或Profile。
- [x] 7.8 禁止将Analysis Source写回Timeline、Track或Clip。
- [x] 7.9 面板显示当前artifact identity与Missing、Stale、Ready、Corrupt状态。
- [x] 7.10 面板提供Left/Right分段选择。
- [x] 7.11 面板提供Speed、Height、Plant和Landing metric选择。
- [x] 7.12 面板一次只渲染当前脚与当前metric。
- [x] 7.13 面板曲线只读，不注册Curve Channel、mutation、Undo或dirty owner。
- [x] 7.14 `Rebuild Selected Clip`只调用Artifact Builder。
- [x] 7.15 Rebuild成功后只刷新当前面板状态和曲线。
- [x] 7.16 Rebuild失败时保留旧artifact并显示结构化错误。
- [x] 7.17 面板不得改变Timeline Track行高、主时间轴滚动范围和marker命中区域。
- [x] 7.18 面板关闭、Timeline切换和窗口销毁时释放renderer与临时读取状态。

## 8. Profile、Inspector与状态语义

- [x] 8.1 保留CharacterAnimationPresentationProfile唯一Analysis Mode与Source GUID配置。
- [x] 8.2 保留Analysis Source Inspector对Sampling Rig、Calibration和算法参数的唯一编辑权。
- [x] 8.3 Profile Inspector显示Analysis Source配置状态，不执行动画采样。
- [x] 8.4 Definition Inspector显示Projection Missing、Stale、Ready，不显示单clip artifact为Projection Ready。
- [x] 8.5 Timeline Analysis面板只显示当前clip artifact状态，不显示完整Definition发布状态。
- [x] 8.6 明确“Artifact Ready但Projection Stale”的合法状态与提示。
- [x] 8.7 明确“Projection Ready但当前Source选择不同”的状态，不回读不匹配数据。
- [x] 8.8 删除将ProjectionRevision冒充artifact revision的UI文本。

## 9. Runtime与表现边界确认

- [x] 9.1 保持Runtime只从Projection clip binding读取左右脚feature。
- [x] 9.2 保持Player最终采用的raw/effective VisualSampleTime作为feature采样时间；显式MarkerSync迁移由Selection边界change负责。
- [x] 9.3 保持Foot Placement Weight为唯一逐Clip可写IK介入曲线。
- [x] 9.4 保持Foot contact不读取MarkerId、Distance Curve、State或Action。
- [x] 9.5 保持Foot Placement Planner、Ground Envelope、constraint、pelvis和Final IK链不变。
- [x] 9.6 确认Runtime程序集不引用Artifact Store、Analysis Source或Editor Analyzer。
- [x] 9.7 确认Library artifact删除不影响已发布Player运行。
- [x] 9.8 确认artifact不进入Snapshot、StateHash、Network packet或Program ABI。

## 10. Agent与诊断收口

- [x] 10.1 保持Agent v15只输出Analysis Source identity摘要，不输出feature payload。
- [x] 10.2 保持Agent Patch只能修改Foot Placement Weight等registered editable channel。
- [x] 10.3 Agent拒绝Sole Speed、Height、Plant和Landing生成channel mutation。
- [x] 10.4 Agent不新增Rebuild Foot Analysis operation。
- [x] 10.5 Validator复用正式artifact identity和Projection binding诊断。
- [x] 10.6 Compile Report区分artifact Missing、Stale、Corrupt与Projection binding缺失。
- [x] 10.7 更新Agent Snapshot exporter对移除Timeline Foot Analysis Context的影响扫描。
- [x] 10.8 更新btsmtl-agent-authoring skill对generated analysis只读边界的描述。

## 11. Corin产物迁移

- [x] 11.1 确认Corin Analysis Source、Sampling Rig和Calibration正式引用保持唯一。
- [x] 11.2 为Corin全部可达AnimationClip生成精确artifact。
- [x] 11.3 重新生成Corin CharacterPresentationProjection。
- [x] 11.4 确认Projection每个启用Foot Placement的clip binding都有匹配feature。
- [x] 11.5 确认Float32与Fixed Program gameplay payload未因artifact变化而改变语义。
- [x] 11.6 删除旧Timeline Foot Analysis UI产生的Editor session残留状态。
- [x] 11.7 确认Standalone、Rollback、Unity Authority与DotRecast角色继续使用同一Calibration。

## 12. 清理、编译与OpenSpec校验

- [x] 12.1 搜索并删除全部旧Timeline Foot Analysis Context和lane引用。
- [x] 12.2 搜索并确认没有Definition-context fallback或反向AssetDatabase搜索。
- [x] 12.3 搜索并确认generated feature没有进入editable Curve Channel Catalog。
- [x] 12.4 搜索并确认Timeline没有完整Definition Build按钮。
- [x] 12.5 更新openspec/project.md中的Animation Analysis artifact与Timeline边界。
- [x] 12.6 更新受影响current specs并删除旧FOOT ANALYSIS lane要求。
- [x] 12.7 更新implementation-inventory.md为最终文件与链路清单。
- [x] 12.8 使用带`--disable-build-servers /nr:false /p:UseSharedCompilation=false`的命令编译相关Runtime程序集。
- [x] 12.9 使用同样参数编译相关Editor程序集。
- [x] 12.10 编译后立即执行`dotnet build-server shutdown`。
- [x] 12.11 运行`openspec validate refactor-timeline-animation-authoring-boundary --strict --no-interactive`。
- [x] 12.12 核对所有task真实完成后再统一标记为`[x]`。

## 13. 骨骼接触候选与正式Marker闭环

- [x] 13.1 修复Preview Rig采样时误禁用Animator的问题。
- [x] 13.2 定义只读Left/Right contact candidate值对象。
- [x] 13.3 将artifact identity与content hash纳入candidate proposal revision。
- [x] 13.4 将Timeline、Track与Clip stable identity纳入candidate proposal revision。
- [x] 13.5 将AnimationClip dependency、ClipIn、source cycle与目标frame映射纳入proposal revision。
- [x] 13.6 按artifact sample rate重采样PlantConfidence。
- [x] 13.7 检测cyclic非接触到稳定接触上升沿。
- [x] 13.8 生成LeftFootContact与RightFootContact候选并拒绝重复目标frame。
- [x] 13.9 拒绝缺少任一脚接触、非法frame或非有限置信值的proposal。
- [x] 13.10 仅允许单AnimationClip完整覆盖MarkerGroup/Cyclic Track时创建可应用proposal。
- [x] 13.11 在Analysis面板显示candidate revision、脚侧与目标frame。
- [x] 13.12 在只读分析曲线中叠加左右脚candidate位置。
- [x] 13.13 Apply前重新Inspect artifact并重建proposal。
- [x] 13.14 artifact、clip或Timeline映射变化时拒绝Stale proposal。
- [x] 13.15 Apply前明确确认目标Timeline与Track identity。
- [x] 13.16 通过TimelineEditorSessionContext正式mutation提交Marker。
- [x] 13.17 只替换LeftFootContact与RightFootContact并保留其它Marker。
- [x] 13.18 尽量复用匹配的stable marker identity并删除多余脚步Marker。
- [x] 13.19 保持Agent generated analysis只读，不新增candidate Patch operation。
- [x] 13.20 使用Agent v15正式流程迁移Corin WalkLoop与RunLoop Marker。
- [x] 13.21 重新生成Corin artifact与Presentation Projection。
- [x] 13.22 编译受影响Runtime与Editor程序集并关闭build server。
- [x] 13.23 更新implementation inventory与current specs。
- [x] 13.24 运行OpenSpec strict validation并核对任务勾选真实性。

## 14. Preview Selection与Pose Plan重新基线

- [x] 14.1 将Timeline Preview采样输出改为AnimationSelectionFrame与表现参数。
- [x] 14.2 删除Preview输出中的PoseSlotId与transition identity。
- [x] 14.3 让Preview创建与Projection revision匹配的编译Pose Plan实例。
- [x] 14.4 让Preview严格按图执行SelectedPosePlayer、可选局部Inertialization或BlendStack。
- [x] 14.5 禁止Preview后台补建Blend Stack、Inertialization或默认fade。
- [x] 14.6 让Preview按Pose Plan执行source、composition与world-aware阶段。
- [x] 14.7 缺少正式world context时标记FootPlacement阶段Unavailable。
- [x] 14.8 禁止不完整world-aware结果冒充FinalAnimationPoseFrame。
- [x] 14.9 更新Live Debug binding为Selection、PoseNode与plan completion口径。
- [x] 14.10 更新本change相关spec delta并保持Timeline Core不引用播放器实现。

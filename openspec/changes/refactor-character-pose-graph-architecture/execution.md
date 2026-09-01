# PoseGraph串行实施记录

## 固定接入

- 总源码及行为基线固定为`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`。
- 第一阶段IK通过接入提交为`f32e419`，最后运行实现提交为`5b551cb`，证据包为`20260901-070946-569-c14830f966ee465c887849cfc66b1f2a`。
- 第二阶段每个代码闭环同时比较上一通过提交和固定总基线；新Program／Projection identity允许按ABI更新，但输入、Body、source时间、Pose、Foot、Pelvis、Goal、Solved与Physical业务结果必须对账。
- 工作区原有`.gitignore`、`ProjectSettings.asset`、`stabilize` proposal和`project.md`修改不属于本change，不能夹带提交或回退。

## 统一根帧lineage与事务

状态：候选`0b66bf0`已完成Runtime编译和固定Record正式回放；只有既有Input Value未使用字段警告，0错误，build server已关闭。

- 新增`CharacterPoseFrameLineage`，一次保存Actor、根Frame identity、Presentation Frame、Body Tick、Program Id、Pose Program identity、Projection Revision、Rig Id／Revision和actor-local Tuning Generation。
- 旧`AnimationPresentationFrameTransaction`直接改名并替换为`CharacterPoseFrameTransaction`；旧文件和类型不存在。根事务只保存统一Lineage、现有Owner的typed lease、阶段、Outcome和提交时批次，不保存Program、Source、Constraint或Final Pose内部页。
- `CharacterAnimationPresentationRuntime`在Pending Tuning应用后、打开任一Frame页前构造一次Lineage。成功应用Tuning Candidate时只推进该Actor的Generation；没有新增静态或跨Actor状态。
- 现有Action、Sampling、Slot、Motion Matching、Pose和Workspace lease继续走唯一正式路径，并统一与Lineage的Frame／Presentation身份对账。Barrier、Discard、Fault、Writer与Seal顺序没有变化。
- 本步没有新增Module空壳、wrapper、第二事务或第二执行路径。Source／Program／Constraint／Publication的细分Result仍待后续闭环，不能把本步称为全部任务2完成。

验证包为`Diagnostics/FootPlacementRuns/20260901-073338-183-725de431cb724bd69b05022e5f073450`。正式Proof对第一阶段接入包匹配1044输入、aggregate mismatch 0、divergent frame 0；与固定总基线的trace、runtime、起始Body、tick drive、presentation clock、输入／Body hash和1044帧数组也一致。

两组对照均为2086脚行、1215列，1191业务列逐值相同，24列只含运行时间和实例身份变化且23个identity列一一映射无冲突。Source normalized time／cycle／completion、Presentation Delta、Body Tick／Alpha、Transition前后Reason／Source／Target、Blend／Slot、Action、Foot、Pelvis149、Goal30、Knee15、Solver／Physical101与Time19均无业务差异；几何67186行中的22个业务列相同。

facts71、42个Target、20447条detail、规则、资格、计数、Health／Evidence与quality-score保持，总分61.9只作辅助。正式summary、events和frame查询成功；Unity回到Edit／Idle，workflow failure为空，Console无错误。由此确认统一lineage和根事务未改变本Record覆盖的Barrier、时钟、状态、IK与Physical行为。

## Program Prepare与Result收口

状态：`b2966a8`已完成Runtime编译和固定Record正式回放；只有既有Input Value未使用字段警告，0错误，build server已关闭。

- `PosePlanFrameLease`与`PosePlanPreparedEvaluation`直接改为`CharacterPoseProgramFrameLease`和`CharacterPoseProgramPrepared`，没有保留旧别名。
- Program Prepare只接收根事务的open lineage，在生成现有Completion后返回补齐Completion的同一lineage；根事务只接受其它身份完全一致的completed lineage，外层不再单独传Actor和Render Frame给Barrier。
- `ExecuteEvaluateBarrier`返回`CharacterPoseProgramResult`，集中发布lineage、Frame Outcome、Output Availability、Output Invalid Reason、Graph Invalid Reason和Invalid Operation。外层只消费该typed Result判断是否可提交和生成错误信息，不再读取`AnimationFinalPoseNativeReadBinding`内部Slice解释结果。
- 本步没有改变Native Workspace、Operation调度、Constraint、Writer或Seal顺序。Source Frame、Constraint Result、Final Publication Result和per-operation completion仍待各自Owner迁移，因此任务2.2保持未完成。

验证包为`Diagnostics/FootPlacementRuns/20260901-092212-871-cd74efd1c5414a3f889c4bf95c701bed`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-092319-923-f050f55f05b14b6d8cb7e981269e0af0.json`。固定Record完整消费1044帧，并与上一Proof匹配1044帧、aggregate mismatch 0、divergent frame 0；该包同时作为下一小步的工作区内A基线。

## Program、Constraint与Final Publication Result分型

状态：本步候选已完成Runtime编译、Unity脚本刷新和固定Record正式回放；只有既有Input Value未使用字段警告，0错误，build server已关闭。

- Program Result只表达Stage完成后的Output availability、Output/Graph invalid reason和invalid operation，不再用Physical Writer结果反推Program是否完成。
- `CharacterPoseConstraintRuntime.CompleteFrame`在现有同一调用位置发布typed Constraint Result，携带同一lineage、Goal数量、Solver是否产出及FBBIK Result；外层错误报告直接读取该Result，旧`TryGetFullBodyIkFailure`跨Bank反查入口已删除。
- Final Publication Result单独表达Writer outcome、Pose availability、Applied Completion和Output/Graph failure；根`CharacterPoseFrameTransaction`在Evaluate Barrier后绑定Program、Constraint和Publication三个同lineage Result，只有三者都成功才允许Seal。
- 本步只把`staged Pose完成 -> Constraint闭包验证 -> Physical Writer -> Pending完成 -> 根Seal`之间的事实分型，没有改Operation顺序、Foot/Goal/FBBIK数学、Writer骨骼顺序、Barrier或Bank提交时机。具体Final Publication Module与Writer所有权仍留给任务7迁移，不在这里创建wrapper或第二Writer。

改前A包为`Diagnostics/FootPlacementRuns/20260901-092212-871-cd74efd1c5414a3f889c4bf95c701bed`，改后B包为`Diagnostics/FootPlacementRuns/20260901-093635-128-5af8dc0e351c416fbc292ec4ea4eae5b`，输入Record均为`43357ff3cd384e5cba75d2c31175b116`。B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-093743-709-ac0d05ccdb0e40199172d15d15d88ab5.json`，与A匹配1044帧、aggregate mismatch 0、divergent frame 0。

两包均为2086脚行、1215列；1191个业务列逐值相同，24个运行／实例／Surface／Path identity列变化且全部一一映射。两包Ground Path Geometry均为67186行、27列；22个业务列逐值相同，5个identity列变化且全部一一映射。正式Analyzer schema、覆盖、各规则eligible/matched计数、七维分项和84.2浅层参考分一致；`analysis.json`仅Sample/文件hash、detail/index字节与hash及分析耗时不同。由此确认本Record覆盖的Body、source时钟、Foot、Pelvis、Goal、Solver与Physical结果没有因Result分型改变；其它路线和非固定表现调度仍未由本包覆盖。

## Source Demand与Source Frame typed交接候选

状态：Runtime按规定参数编译成功，Unity脚本刷新0错误；Foot Calibration与Projection由对应Owner补齐后，同输入Record已重新完整消费1044帧。因为补采样时同时包含外部Foot／Projection身份更新，本证据用于确认当前完整工作区可作为下一小步A基线，不把它错误归因为Source候选的隔离A/B；任务2.2仍因per-operation completion未建立而保持未完成。

- Program在原`PrepareEvaluation`紧邻调用前生成一次完整completion lineage和`CharacterPoseSourceDemand`，Demand只引用现有只读provider demand及本帧Action／Provider source数量，不取得Workspace写权限。
- 现有唯一source准备路径成功后发布`CharacterPoseSourceFrameResult`，显式区分Pending、Ready、Invalid与Prepared/Awaiting/Invalid outcome；`CharacterPoseProgramPrepared`绑定该Result，根`CharacterPoseFrameTransaction`保存Demand与Source Result并验证与后续Program／Constraint／Publication相同lineage。
- Completion数值的成功帧生成次数、source采样、Playable准备、capture、release、Program workspace、Barrier和Writer顺序没有移动；本步没有建立Source Module空壳、第二source页或fallback。Source物理资源与Owned Pending页仍由任务2.4和任务4迁移。

原请求在0输入帧时失败，正式错误为`Canonical Fixed input replay timed out while starting Gameplay Lab`；根因是并行外部改动已把`CharacterFootPlacementRigCalibration.CurrentSchemaVersion`从4提升到5并新增Current Support Footprint字段，但当时Calibration asset仍是旧内容且Projection未显式重建，导致`CharacterPresentationProjection.IsValid=false`、Actor roster为空。本change没有修改其资产、构建产物或加入兼容绕过。

对应Owner闭合后，补采样包为`Diagnostics/FootPlacementRuns/20260901-110537-059-6498a7fef1cc44319a37d751e921506e`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-110644-970-32ecf37405fa451ebc1a893f21731215.json`。它对上一正式Proof报告Program／Projection七个aggregate identity字段变化，但`DivergentFrameCount=0`、`FirstDivergentRelativeFrame=-1`、`FirstFrameFields=[]`。因此该包只证明外部身份更新后的当前工作区逐帧行为仍与原Record一致，并作为下一小步A；不把叠加外部改动后的结果伪装成Source候选的单改动归因证据。

## Program Prepared实现所有权收口

状态：`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告均来自既有包或既有Input Value未使用字段，build server已关闭。Unity脚本刷新完成且Console 0错误，同输入A/B正式回放通过。

- `CharacterPoseProgramPrepared`只保留`CharacterPoseSourceFrameResult`、同一`CharacterPoseFrameLineage`与typed Source outcome，不再向动画表现根暴露Presentation Delta、`CharacterPoseGraphNativeBinding`、`CharacterPoseGraphStagedExecutor`、Pending／Committed Final Read binding或Committed Final存在性。
- `PosePlanExecutionRuntime`把上述实现数据保存为Program-owned pending prepared状态；`PrepareEvaluation`对同一打开Frame只允许发布一次，`ExecuteEvaluateBarrier`按同一lineage和Completion验证后一次消费并清空。重复Prepare、跨Frame prepared或重复Barrier执行不再能借外层复制的Native struct进入实现。
- Seal、Discard、Reset和Dispose统一清空Program prepared状态；根`CharacterAnimationPresentationRuntime`仍只读取Source Frame与lineage并把typed prepared合同送回同一Program Runtime。Animancer Evaluate、Stage循环、world-aware输入装配、Constraint Complete、Physical Writer、Pending完成和根Seal顺序没有移动。
- 本步只建立Program prepared实现的Owned Pending边界。Source、Constraint与Final Publication各自Owned Pending页、根typed lease以及per-operation completion仍未完成，因此任务2.4和2.2都不提前勾选。

A包为`Diagnostics/FootPlacementRuns/20260901-110537-059-6498a7fef1cc44319a37d751e921506e`，B包为`Diagnostics/FootPlacementRuns/20260901-111208-798-b1a8446ff183468d9eb63f531a00f08d`，输入Record均为`43357ff3cd384e5cba75d2c31175b116`。B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-111310-573-dd19de7b80bc406a8c7ab741cbaac122.json`，与A精确匹配1044帧。

两包均为2086脚行、1215列；1191个业务列逐值相同，24个Run／实例／Surface／Path identity列变化且全部一一映射。Ground Path Geometry均为67186行、27列；22个业务列逐值相同，5个identity列变化且全部一一映射。Analyzer schema、Program／Projection／Pose／Profile identity、覆盖、全部规则计数、七维分项和84.2浅层参考分一致；`analysis.json`只在Sample／文件hash、detail／index大小与hash和分析耗时上变化。由此确认本Record覆盖的Body、source时间、Foot、Pelvis、Goal、Solver与Physical结果没有因Program prepared所有权收口改变。

## Program Prepared原子Pending页候选

状态：`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告均来自既有包或既有Input Value未使用字段，build server已关闭。Unity脚本刷新完成且Console 0错误；同输入B执行和Foot诊断封存完成，但正式Replay Proof在发布前被并行外部脚本编译触发的程序集重载中断，因此本步保留为独立候选，不能作为下一步正式A基线。

- 将Program-owned prepared的lineage、Presentation Delta、Native Frame、Staged Executor、Pending／Committed Final Read与Committed Final存在性合并为单一`CharacterPoseProgramPreparedPage`，`HasValue`只在全部字段写入后提升。
- Pending Page只接受同一typed `CharacterPoseProgramPrepared`，一次`Consume`先冻结只读State再原子Clear；重复Prepare、缺失Page、跨lineage Consume和内部Completion不一致保持fail-closed。Runtime不再分别维护八个可独立更新和清理的prepared字段。
- Begin、Seal、Discard、Reset与Dispose都只操作同一Page；Barrier取得Page State后仍按原顺序执行Animancer Evaluate、Stage、Constraint、Writer与Pending完成。Foot、Pelvis、Goal、FBBIK、Physical Writer和Operation数据均未修改。
- 当前Page是Program Frame Pages的正式组成边界，不建立第二Program、第二Frame或兼容路径。完整`CharacterPoseProgramFramePages`、Operation Completion和其它Module Pending页仍待后续迁移，任务2.2、2.4和5.5均不提前勾选。

A包为`Diagnostics/FootPlacementRuns/20260901-111208-798-b1a8446ff183468d9eb63f531a00f08d`，B1包为`Diagnostics/FootPlacementRuns/20260901-112222-563-5270427812e2458d8ec0fd886437271e`，输入Record均为`43357ff3cd384e5cba75d2c31175b116`。回放状态在封存前已报告1044输入帧执行完成；Editor日志随后记录B1封存1043表现帧、1个既有Pending丢弃帧、2086脚行、67186几何行和完整9份诊断，紧接着出现`Reloading assemblies after forced synchronous recompile`，因此没有生成新的Proof文件。

A/B的1215列Foot CSV中1191个业务列逐值相同，24个Run／实例／Surface／Path identity列变化且全部一一映射；27列Geometry中22个业务列逐值相同，5个identity列变化且全部一一映射。排除Sample／文件hash、detail／index大小与hash和分析耗时后，`analysis.json`完全相同；排除Sample identity与index hash后，`quality-score.json`完全相同且总分均为84.2。由此把运行数据一致与Proof发布器受外部domain reload中断明确分开；并行Foot源文件已在B1封存后变化，必须等其Owner闭合并重建新A，不能继续叠加下一小步。

## Program Prepared合同归位候选

状态：`ThirdPersonClient.Runtime.csproj`按规定参数静态编译成功，0错误；27个警告均来自既有包或既有Input Value未使用字段，build server已关闭。后续`ThirdPersonClient.Editor.csproj`也以规定参数编译成功，0错误，build server已关闭。Foot Owner释放Unity后，已在其它Unity源码写入冻结窗口内重新完成同状态隔离A/B；HEAD合同状态已恢复，Replay Proof、Foot CSV、Ground Path Geometry和全部诊断报告均已对账，候选验证完成。

- 将跨Owner使用的`CharacterPoseProgramPrepared`从`PosePlanExecutionRuntime.cs`移入统一`CharacterPoseFrameContracts.cs`，与Source Demand、Source Frame、Program Result、Constraint Result和Publication Result使用同一合同目录及`ThirdPersonCharacter.Pipeline.Animation`命名空间。
- Runtime文件中的旧定义直接删除；全仓搜索只保留一个正式Prepared合同，不保留别名、转发类型或兼容namespace。现有Program-owned Pending Page继续消费同一typed合同，字段、构造校验和调用顺序均未改变。
- 本步只修正抽象与实现的物理归属，不改变Source采样、Native Workspace、Executor、Constraint、Foot／Pelvis／Goal／FBBIK、Writer或Seal／Discard生命周期。任务2.2仍因per-operation completion未建立而保持未完成。

固定Record仍为`43357ff3cd384e5cba75d2c31175b116`。A通过临时反向应用`4a570788`恢复提交前合同位置后执行，包为`Diagnostics/FootPlacementRuns/20260901-122052-732-2960e8c0f0c04f75980af233629242f3`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-122151-505-6f2fb5d9e32f4949bb8d32148a1293da.json`。A完整封存1043表现帧、2086脚行和67186几何行；相对旧Proof只报告Program／Projection七个aggregate identity字段变化，`DivergentFrameCount=0`、`FirstDivergentRelativeFrame=-1`、`FirstFrameFields=[]`，因此它是当前Foot／Network身份更新后的正式提交前基线。

A完成后已原样恢复HEAD合同位置，全仓仍只有`CharacterPoseFrameContracts.cs`中的一份`CharacterPoseProgramPrepared`定义，Runtime文件只保留其它任务未提交的Performance Marker差异。由于旧A完成后Foot与Network窗口重建过正式产品和诊断基线，本步没有把跨源码状态的旧A硬接到B，而是在二者明确冻结Unity写入后重新建立同状态隔离对：A临时只反向移动`4a570788`的合同定义，包为`Diagnostics/FootPlacementRuns/20260901-125139-634-d1ccc86b44b2482f984ddc88b0b91c00`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-125245-959-c0a9359062294e55b51318c6a0aa4516.json`；B恢复HEAD合同位置后执行，包为`Diagnostics/FootPlacementRuns/20260901-125338-003-e15cbcfa576045d68a62b71b5095bb84`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-125437-102-198c4580f3a84dea9600ef3e848ce9bc.json`。B Proof对A报告`matched=true`、`compared_frame_count=1044`、`aggregate_mismatches=[]`、`divergent_frame_count=0`、`first_divergent_relative_frame=-1`和空`first_frame_mismatches`。

A/B均封存1043表现帧、2086脚行和67186几何行。1215列Foot CSV中1191个业务列逐值相同，24个Run／实例／Surface／Path identity列全部一一映射；27列Geometry中22个业务列逐值相同，5个identity列全部一一映射，没有其它差异。`analysis.json`、`quality-score.json`及八份规则报告在排除Sample／Surface identity、文件hash、detail／index大小与分析耗时后全部相同，七维分项与总分84.2不变。由此确认合同物理归位没有改变Source采样、动画时钟、Foot、Pelvis、Goal、FBBIK、Physical Pose或诊断业务事实；当前B可作为下一项Operation Completion迁移的正式A基线。

## Typed Operation Completion页

状态：提交`f70ad67de`已将旧`NativeArray<ulong> FrameCacheCompletedAt`原子替换为typed Operation Completion entry/page。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误、0警告，build server已关闭；固定Trace回放、Foot诊断与Replay Proof均完成，任务2.2的全部typed合同已建立。

- 新`CharacterPoseOperationCompletion`同时保存Completion identity与`Completed / Skipped / TypedInvalid` Outcome；默认值只表示尚未完成。`CharacterPoseOperationCompletionPage`唯一接受首次合法完成，第二次写同一Operation返回失败且不覆盖第一次结果。
- `AnimationPoseNativeWorkspace`的Committed/Pending页不再分配或暴露裸`ulong`完成数组，而是分配固定Operation Count的typed entry；Binding只暴露typed page。Stage完成与最终Program完成仍保持各自现行合同，本步不提前迁移5.5的完整Program Frame Pages。
- Staged Executor在执行Operation前先拒绝已有completion；重复执行会记录`PoseGraphOperationInvalid`与对应Operation index，使整帧Invalid并阻止正常Final Publication。正常Operation按原执行顺序写`Completed`，非活动Linked Pose分支写`Skipped`，原有typed失败写`TypedInvalid`；Preview同样填充typed completion，不再批量伪写裸identity。
- Diagnostics只从typed completion读取既有Completion identity和完成匹配结果，未获得Outcome写权限，也不把Outcome送回任何Runtime决策；现有Snapshot、Pose Watch、Sampler、Analyzer和评分字段语义保持不变。

A为上一步HEAD合同状态的`Diagnostics/FootPlacementRuns/20260901-125338-003-e15cbcfa576045d68a62b71b5095bb84`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-125437-102-198c4580f3a84dea9600ef3e848ce9bc.json`。B为typed Completion状态的`Diagnostics/FootPlacementRuns/20260901-130336-194-9e188a814d0b4271a5eef0b9baf04778`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-130437-539-13dbb6d69362418ab8f045f5ea139e7f.json`。B Proof对A报告`matched=true`、`compared_frame_count=1044`、空aggregate/frame差异和`divergent_frame_count=0`。

A/B均封存1043表现帧、2086脚行和67186几何行。1215列Foot CSV中1191个业务列逐值相同，24个Run／实例／Surface／Path identity列全部一一映射；27列Geometry中22个业务列逐值相同，5个identity列全部一一映射，没有其它差异。十份诊断报告在排除运行identity、文件hash、detail／index大小与分析耗时后全部相同，七维分项与总分84.2不变。由此确认typed完成页没有改变动画时钟、Operation顺序、Foot、Pelvis、Goal、FBBIK、Physical Pose或诊断业务事实；该B成为下一项Owned Pending页／typed lease迁移的正式A基线。

## Program Owned Pending Lineage Lease候选

状态：提交`95c5e3644`已将Program Frame Lease从实现文件中的裸Frame identity提升为统一合同目录中的完整open lineage。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；唯一警告为既有`PipelineBlackboardValueInfoNode.m_ReportedSourceError`未使用字段，build server已关闭。固定Trace回放、Foot诊断与Replay Proof均完成。本步只完成Program lease，Source、Constraint与Final Publication的Owned Pending页／lease仍待后续，因此任务2.4保持未完成。

- `CharacterPoseProgramFrameLease`现在绑定Actor、Frame、Presentation Frame、Body Tick、Program、Pose Program、Projection、Rig和Tuning Generation，只允许Completion identity尚未分配的open lineage；合同不再定义在`PosePlanExecutionRuntime.cs`实现内部。
- 根Runtime先建立一次open lineage，再把同一个lineage同时交给Program `BeginPendingFrame`与根`CharacterPoseFrameTransaction.Begin`。Program Runtime保存该typed lease，后续Seal／Discard／Mutation必须与活动lease完整lineage相同，不再只比较Frame number。
- Source准备完成后根Transaction仍按现行顺序补入Completion identity；`PoseLease.Matches`只把这一项归零后比较其余完整lineage，因此不会建立第二身份或改变Completion生成时机。Program Pending内部Workspace、Source Backend、Constraint与Final Publisher本步均未搬移。

A为typed Operation Completion状态的`Diagnostics/FootPlacementRuns/20260901-130336-194-9e188a814d0b4271a5eef0b9baf04778`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-130437-539-13dbb6d69362418ab8f045f5ea139e7f.json`。B为Program lineage lease状态的`Diagnostics/FootPlacementRuns/20260901-131316-839-a9253d4b1afe4d07874492e537b81e6e`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-131413-452-22929101ca0648f394536dce3633b66a.json`。B Proof对A报告`matched=true`、`compared_frame_count=1044`、空aggregate/frame差异和`divergent_frame_count=0`。

A/B均封存1043表现帧、2086脚行和67186几何行。1215列Foot CSV中1191个业务列逐值相同，24个运行identity列全部一一映射；27列Geometry中22个业务列逐值相同，5个identity列全部一一映射，没有其它差异。十份诊断报告在排除运行identity、文件hash、detail／index大小与分析耗时后全部相同，七维分项与总分84.2不变。由此确认完整Program lease没有改变Source准备、动画时钟、Operation、Foot、Pelvis、Goal、FBBIK、Physical Pose或诊断业务事实；该B成为Source Owned Pending lease迁移的正式A基线。

## Source Owned Pending页与Lineage Lease候选

状态：提交`4cb9c072a`已由实际`AnimancerPoseSamplingBackend` Source Owner独占Source Pending页及其typed lease。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告均来自既有Unity包、第三方包或既有Input字段，build server已关闭。固定Trace的直接A/B、完整Foot／Geometry／诊断对账和后续内建Replay Proof均已闭合。本步只完成Source lease，Constraint与Final Publication的Owned Pending页／lease仍待后续，因此任务2.4保持未完成。

- 新`CharacterPoseSourceFrameLease`绑定与Program lease相同的完整open lineage。根`CharacterPoseFrameTransaction`单独保存Program Lease与Source Lease，并在Source补入Completion identity后仍按除Completion以外的完整lineage验证Demand、Result、Seal和Discard。
- `AnimancerPoseSamplingBackend`内部唯一`CharacterPoseSourcePendingPage`保存当前lease、唯一Demand和唯一Source Frame Result；Begin、Demand、Result、Validate、Evaluate Barrier、Commit与Discard全部要求同一个typed lease。旧`PosePlanExecutionRuntime.m_PendingSourceDemand`已删除，不保留镜像字段或兼容路径。
- 根Runtime只持有Source Lease和只读Demand／Result，Seal／Discard时把lease交回Source Owner；Program Runtime只生成Demand并消费Source Result，不取得Pending页。Animancer资源准备、Source-local时间、Clip／Blend Space／Action sample、deferred release和Playable调用顺序均未修改。

正式A为Program lineage lease状态的`Diagnostics/FootPlacementRuns/20260901-131316-839-a9253d4b1afe4d07874492e537b81e6e`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-131413-452-22929101ca0648f394536dce3633b66a.json`。第一次候选包`Diagnostics/FootPlacementRuns/20260901-140248-359-3ca202b622c445c997d26d9fae98149b`在Body Tick 367→368期间额外采样两个Presentation Frame，Proof `20260901-140353-434-056e6c2e2069471dab7066432fb675aa.json`只报告`sampling_relative_frame_count: 1043→1045`且`divergent_frame_count=0`；异常前366个表现帧的1191个业务列逐值相同。该包保留为Editor表现调度反例，不作为代码A/B成功证据。

同状态补跑B2为`Diagnostics/FootPlacementRuns/20260901-140725-120-fb6a3adfe8284cddbbaeed32fc97b59a`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-140831-534-f31eba0e0aca44298f216a8860849200.json`。B2直接对正式A的schema、Trace、Runtime、Start Body、Tick／Presentation Clock、1044个frames、Input hash、Body hash和`sampling_relative_frame_count=1043`全部相同。两包均为2086脚行、1215列，其中1191个业务列逐值相同、24个运行identity列一一映射；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射。十份诊断报告排除运行identity、文件hash、detail／index大小与分析耗时后全部相同，七维分项和总分84.2不变。

为让工具自己的链式基线也闭合，最终B3为`Diagnostics/FootPlacementRuns/20260901-141357-584-1f1b11082a7245f9b9c31dd07e123429`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-141501-810-0e4583d4cb2843ec9a8ee50189c671a1.json`；它对B2正式报告`matched=true`、`compared_frame_count=1044`、空aggregate/frame差异和`divergent_frame_count=0`。由此确认Source Pending页所有权收口没有改变动画时钟、Source采样、Operation、Foot、Pelvis、Goal、FBBIK、Physical Pose或诊断业务事实；B3成为Constraint Owned Pending lease迁移的正式A基线。

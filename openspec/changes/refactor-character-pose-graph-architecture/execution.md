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

## Constraint双Bank Lineage Lease候选

状态：提交`0fb3cb432`已让现有`CharacterPoseConstraintRuntime`双Bank绑定完整typed Constraint lease，并由根事务持有其Seal／Discard权限。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告均来自既有Unity包、第三方包或既有Input字段，build server已关闭。固定Trace、Foot、Geometry与诊断对账全部完成。本步不移动Foot／Goal／FBBIK内部页，也不提前拆Final Publication；任务2.4仍只差Final Publication lease。

- 新`CharacterPoseConstraintFrameLease`绑定完整open lineage。Constraint `BeginFrame`一次创建lease并把它写入唯一Pending Bank；旧Bank中的Frame identity、Render Frame、Rig Id和Rig Revision重复字段已删除，所有帧／Rig判断只读lease lineage。
- 根`CharacterPoseFrameTransaction`单独保存Constraint Lease。Program在Begin、Complete、Barrier后Fault、Seal和Discard时必须交回同一个lease；Constraint Result补入Completion identity后仍按除Completion外的完整lineage匹配，不建立第二完成身份。
- 双Bank选择、Foot Placement Bank、Pelvis、Goal Contribution、Goal Set、BendHistory、FBBIK Solver、Physical Writer调用和诊断发布顺序均保持原实现；本步只收紧Bank外部生命周期与身份，不改变任何IK公式、参数、准入或数值顺序。

A为Source lease最终状态的`Diagnostics/FootPlacementRuns/20260901-141357-584-1f1b11082a7245f9b9c31dd07e123429`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-141501-810-0e4583d4cb2843ec9a8ee50189c671a1.json`。B为Constraint lease状态的`Diagnostics/FootPlacementRuns/20260901-142115-026-3d661384834a42669887b2d1a51022b6`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-142225-539-a238d21b26944e57875927593ad9886f.json`；B对A报告`matched=true`、`compared_frame_count=1044`、空aggregate/frame差异和`divergent_frame_count=0`。

A/B均封存1043表现帧、2086脚行和67186几何行。1215列Foot CSV中1191个业务列逐值相同、24个运行identity列一一映射；27列Geometry中22个业务列逐值相同、5个identity列一一映射，没有其它差异。十份诊断报告排除运行identity、文件hash、detail／index大小与分析耗时后全部相同，七维分项和总分84.2不变。由此确认Constraint Bank lineage收口没有改变动画时钟、Foot、Pelvis、Goal、Assembler、Bend、FBBIK、Physical Pose或诊断业务事实；B成为Final Publication lease迁移的正式A基线。

## Final Publication Owned Pending页与Lineage Lease

状态：提交`20dac2d15`已让现有最终姿势发布器独占正式`CharacterFinalPosePublicationPendingPage`并绑定完整typed Publication lease；提交`94a8c001e`修正了第一次Unity运行暴露的大结构自动属性setter问题。修正后`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告均来自既有Unity包、第三方包或既有Input字段，build server已关闭。Unity重新加载后固定Trace、Foot、Geometry、十份诊断报告与Replay Proof全部闭合，Console 0错误。Program、Source、Constraint与Final Publication四个现行Owner现均拥有独立Pending页和typed lease，任务2.4完成。

- 新`CharacterFinalPosePublicationFrameLease`绑定与其它三个Module相同的完整open lineage。根`CharacterPoseFrameTransaction`单独保存Publication Lease，并在Begin、Evaluate Barrier、Seal前验证、成功发布、Barrier前Discard与Barrier后Fault清理中始终交回同一lease。
- `ComposedAnimationPoseFramePublisher`内部唯一`CharacterFinalPosePublicationPendingPage`保存lease、选中的双Buffer页、Publication Result和只读Composed Frame；旧的Pending Page index、Completion identity与Frame三个可独立更新字段已删除。Publication Result不再由外层`PosePlanExecutionRuntime`拼装，而由该Pending页Owner按同一完成lineage产出。
- 成功路径仍保持原顺序：Constraint Complete、Physical Writer、Workspace确认Write Outcome、Final Publication冻结Pending结果、根Seal、deferred source release、Publication页提升。失败路径只清理匹配同一lease的Pending页，不改变Committed。Foot、Pelvis、Goal、FBBIK、Physical Writer公式与调用顺序均未修改。
- 本步只完成现行Final Publication的Owned Pending生命周期，不声称已完成第7章：`AnimationFinalPosePhysicalWriter`仍由Constraint外层持有，唯一Final Pose Native物理页仍在`AnimationPoseNativeWorkspace`。后续第7章会把Writer与物理页整体迁入具体`CharacterFinalPosePublication`，不会保留当前分裂归属或建立第二Writer／第二Final Pose页。

第一次候选回放包为`Diagnostics/FootPlacementRuns/20260901-144934-470-9e0237954789465e840ccfebfea044d9`。它在正式采样前的Pose Branch Reset进入`ComposedAnimationPoseFramePublisher.Invalidate`时抛出`InvalidProgramException: Passing an argument of size '10048'`，Accepted Frames为0；Unity `Editor.log`保留了Sample identity、调用栈和失败位置。原因是初版Pending页把约10KB的`ComposedAnimationPoseFrame`保存为自动属性，`Frame = default`生成了Mono不接受的大按值setter参数；离线Roslyn编译能够通过，但Unity Mono执行失败。`94a8c001e`把该Pending页全部存储改为直接字段，删除大结构setter；失败候选不作为A/B成功证据，也没有修改IK输入或输出。

正式A为Constraint lease状态的`Diagnostics/FootPlacementRuns/20260901-142115-026-3d661384834a42669887b2d1a51022b6`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-142225-539-a238d21b26944e57875927593ad9886f.json`。修正后的B为`Diagnostics/FootPlacementRuns/20260901-145615-867-82e9a3802a6a4b2ba49e3b70c64c75d4`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-145714-058-456b2612adfd4f90ae75c8b1bd02992b.json`；B对A报告`matched=true`、`compared_frame_count=1044`、空aggregate/frame差异、`divergent_frame_count=0`、`first_divergent_relative_frame=-1`和空首帧差异，二者`sampling_relative_frame_count`均为1043。

A/B均封存2086脚行和67186几何行。1215列Foot CSV中1191个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射、0个其它差异；27列Geometry中22个业务列逐值相同、5个identity列一一映射、0个其它差异。十份诊断报告排除运行identity、文件hash、detail／index大小与分析耗时后全部相同，七维分项与总分84.2不变。由此确认Publication Pending所有权和lease收口没有改变动画时钟、Source采样、Operation、Foot、Pelvis、Goal、Assembler、Bend、FBBIK或Physical Pose业务事实；B成为下一项外层重构的正式A基线。

## Pose帧合同校验责任分层

状态：提交`0c7e51c6c`已收敛任务2阶段新增的重复静态身份检查与多层Result完整校验。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；唯一警告为既有`PipelineBlackboardValueInfoNode.m_ReportedSourceError`未使用字段，build server已关闭。固定Trace、Foot、Geometry、十份诊断报告与Replay Proof全部通过，Unity Console 0错误，任务2.7完成。

- Character Build已有的typed拓扑、静态写冲突、Operation／Value／Workspace布局与schema证明保持原Owner；本步没有在Runtime、Module或Executor新增Graph／Program静态扫描，也不提前处理后续Compiler Pass任务。
- Runtime创建继续由`CharacterPresentationRuntimeFactory`与`PosePlanExecutionRuntime`一次验证Projection、Rig、资源绑定和固定容量。`CharacterPoseConstraintRuntime.BeginFrame`不再每帧把Rig字符串转换为`FixedString`后重复核对；Final Publisher也不再每帧重复比较Pose Program、Rig与Rig Revision。它们只接收由当前Runtime装配链生成的typed lease。
- Program、Source、Constraint与Publication四种lease只在构造时验证完整open lineage并冻结合法性；后续`IsValid`不再重新扫描Actor、Program、Projection、Rig与Tuning字符串，`Matches`只比较同一完整lineage并忽略尚未分配／已经分配的Completion项。根事务删除`IsValid + Matches`双重调用，只保留当前lease匹配、阶段和Completion交接检查。
- Program Result、Constraint Result、Publication Result与组合Execution Result在Owner构造结果时冻结一次合同合法性；根`CharacterPoseFrameTransaction`后续只读取冻结结果、同lineage和Published Outcome，不在Owner、组合Result与根Seal三层重新计算同一完整合法性。
- Source readiness、动态容量、release闭包、Goal／FBBIK闭包、Operation completion、Final Pose availability、continuity、Write Outcome和Physical binding仍由拥有当前动态输入的边界检查。全仓仍只有一处`new AnimationFinalPosePhysicalWriter`分配；Writer在Evaluate前验证全部Transform binding并保持原Fault政策。本步没有删除任何算法必要动态检查，也没有改变Writer、Foot、Goal或FBBIK执行顺序。

正式A为Final Publication lease状态的`Diagnostics/FootPlacementRuns/20260901-145615-867-82e9a3802a6a4b2ba49e3b70c64c75d4`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-145714-058-456b2612adfd4f90ae75c8b1bd02992b.json`。B为校验责任分层状态的`Diagnostics/FootPlacementRuns/20260901-153824-053-e3bd86ceb07d4741aa12ca1a856e4fae`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-153939-043-da8cbb1602604e0c83b6544619be66c2.json`；B对A报告`matched=true`、`compared_frame_count=1044`、空aggregate/frame差异、`divergent_frame_count=0`、`first_divergent_relative_frame=-1`和空首帧差异，二者`sampling_relative_frame_count`均为1043。

A/B之间主线另有提交`7534b6bf0`把GM／NetworkTest Editor工具合同迁入仓库级本地UPM包；该提交不包含Pose Runtime、产品Program或诊断语义修改。B Proof中的Program、Projection、Source Revision、Semantic／Contract、World、Trace、Start Body、Tick／Presentation Clock、Input和Body identity均与A相同，Foot与Geometry也没有出现第三类差异，因此该Editor-only目录迁移未污染本次Pose行为结论。

A/B均封存2086脚行和67186几何行。1215列Foot CSV中1191个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射、0个其它差异；27列Geometry中22个业务列逐值相同、5个identity列一一映射、0个其它差异。十份诊断报告排除运行identity、文件hash、detail／index大小与分析耗时后全部相同，七维分项与总分84.2不变。由此确认校验责任分层没有改变动画时钟、Source采样、Operation、Foot、Pelvis、Goal、Assembler、Bend、FBBIK、Physical Pose或原Fault外的正常业务事实；B成为Constraint外层边界迁移的正式A基线。

## Constraint根Bank生命周期外层归属

状态：提交`6045f40f0`已把现有`CharacterPoseConstraintRuntime`与其双Bank生命周期所有权从旧`PosePlanExecutionRuntime`提升到唯一帧协调根`CharacterAnimationPresentationRuntime`。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0警告、0错误，build server已关闭。固定Trace、Foot、Geometry、十份诊断报告、Replay Proof、退出Play后的Dispose和Unity Console均通过，任务3.1完成。

- `CharacterAnimationPresentationRuntime`现在保存唯一`m_PoseConstraints`引用并负责最终Dispose；`PosePlanExecutionRuntime`只借用同一实例执行现有Constraint调用，不再在自身Dispose中销毁Bank。根Runtime构造失败时按`Pose Runtime -> Constraint`顺序清理，正常销毁也先结束Pose Runtime使用，再销毁Constraint与Foot内部资源。
- 全仓仍只有`PosePlanExecutionRuntime`构造链中的一处`new CharacterPoseConstraintRuntime`，没有第二Factory、wrapper、可选实现或兼容路径。构造成功后同一实例的生命周期所有权立即交给帧协调根；Program执行侧和根Owner没有复制Bank、Foot页、Goal页、BendHistory或Solver状态。
- `CharacterPoseConstraintRuntime`内部Bank类型、双页选择、Foot Placement Bank、Pelvis、Goal Contribution、Goal Set、BendHistory、FBBIK Solver与Diagnostics布局均未修改；Foot Module仍只随Constraint Owner销毁一次。Physical Writer暂时仍由Constraint持有，按任务7整体迁入Final Publication，本步不建立中间Writer Owner。
- Program执行侧仍通过现有入口调用同一Constraint实例；typed编译Handle、per-operation Result以及NativeSlice／offset／Operation字段收窄属于任务3.3至3.6，本步不借生命周期迁移提前改调用公式或数据布局，因此任务3.2及后续任务不提前勾选。

正式A为校验责任分层状态的`Diagnostics/FootPlacementRuns/20260901-153824-053-e3bd86ceb07d4741aa12ca1a856e4fae`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-153939-043-da8cbb1602604e0c83b6544619be66c2.json`。B为Constraint外层生命周期Owner状态的`Diagnostics/FootPlacementRuns/20260901-155530-797-513d233becd14a5c8ab066a33eecf972`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-155637-385-7ce395f427fa4b9c8b851cbd466cffa5.json`；B对A报告`matched=true`、`compared_frame_count=1044`、空aggregate/frame差异、`divergent_frame_count=0`、`first_divergent_relative_frame=-1`和空首帧差异，二者`sampling_relative_frame_count`均为1043。退出Play触发新Owner Dispose链后Console仍为0错误，确认没有双Dispose、漏Dispose或悬空Bank访问。

A/B之间性能任务把其独立IPC源码从Named Pipe迁向Loopback TCP，但没有修改Pose Program、Constraint、Foot／IK、产品Program或诊断字段；B Proof中的Runtime identity、Trace、Start Body、Tick／Presentation Clock、Input与Body identity全部与A相同。A/B均封存2086脚行和67186几何行：1215列Foot CSV中1191个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射、0个其它差异；27列Geometry中22个业务列逐值相同、5个identity列一一映射、0个其它差异。十份诊断报告归一化后全部相同，七维分项与总分84.2不变。由此确认Constraint生命周期Owner提升没有改变动画时钟、Source采样、Operation、Foot、Pelvis、Goal、Assembler、Bend、FBBIK、Physical Pose或正常销毁行为；B成为Constraint typed入口收窄的正式A基线。

## Foot Placement typed Handle候选与并行Foot行为混入

状态：提交`39180583d`已把Foot Placement的Operation、Callsite、Contribution Value和Goal Workspace地址固化为`CharacterFootPlacementConstraintHandle`，并让Constraint入口返回`CharacterFootPlacementConstraintOperationResult`。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；唯一警告为既有`PipelineBlackboardValueInfoNode.m_ReportedSourceError`未使用字段，build server已关闭。本步尚未形成可接受的行为A/B，不勾任务3.3，也不把混合候选当成下一步基线。

- Native Program只在编译Foot Operation时构造有效Handle；默认struct通过独立有效位保持无效，避免索引默认值0把非Foot Operation误判为Foot。`PosePlanExecutionRuntime`与World-aware Stage Input不再分别传递Operation Index和Goal Offset。
- `CharacterPoseConstraintRuntime`现在以Handle执行Ready或World Context Unavailable两条现有Foot路径，并由typed Result冻结Producer、Callsite、Goal Offset、Availability、Frame和Completion匹配。Foot内部Evaluate顺序、公式、参数、历史页、Goal编码、Assembler、FBBIK与Physical Writer均未修改。
- 第一次候选为`Diagnostics/FootPlacementRuns/20260901-161932-208-257452ac7fdc4ac4a97f8d62ccc3651e`。工具返回的Proof路径为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-162046-483-e365cd3d1559423a914ad43ef86f3bff.json`；它相对`155530`报告1044个Replay Frame无分歧、Input Sequence与Body Trajectory Hash相同、1043个表现采样帧相同，但Program／Projection的7个aggregate identity变化。外部重启Unity后该Temp Proof已不存在，因此本记录只保留工具已经返回的失败身份与持久run，不能把路径引用当成仍存在的Proof文件。
- 两包均有2086脚行；候选Foot CSV因并行诊断修改从1215列增加为1222列，新增7个`PelvisSameLevel`列。1215个同名列中1110列逐值相同、105列变化；其中24个既有运行／实例／Surface／Path identity列一一映射，另有Program、Projection、Pose Plan与Foot Profile身份随产品重建变化，其余差异集中在Stride、Pelvis、FBBIK和Physical结果。Geometry仍为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射。
- 差异归属已由Foot Owner确认：并行提交`68dde5c0c`在同一工作区加入“同平面双脚阶段限制骨盆世界下降”行为、7个正式诊断字段、Profile变化和产品重建；该提交的父提交正是`39180583d`，且候选采样发生在Foot提交前但源码已在工作区。因此`161932`同时包含PoseGraph Handle与Foot行为，不能证明或否定单独的PoseGraph行为等价，必须从本change的通过证据中排除。

恢复结果：Foot与Performance Owner暂停源码、产品、Git和Unity写入后，正式A为`Diagnostics/FootPlacementRuns/20260901-165648-064-ed90c98bff504e6eb99e47c30e164195`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-165746-842-f6cb9db6199f4956ac36df4dac7e58e1.json`；同状态B为`Diagnostics/FootPlacementRuns/20260901-165834-116-4f8dec5c15b8469c98f6498774091a2b`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-165933-491-4c8b5c15e2db400eb5ad7f53d10c72a1.json`。B对A正式报告`matched=true`、`compared_frame_count=1044`，两包均封存1043表现采样帧、2086脚行和67186几何行。

A/B的1222列Foot CSV中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列全部一一映射且无冲突；27列Geometry中22个业务列逐值相同、5个identity列全部一一映射。十份正式报告在排除Sample／文件hash、detail／index大小与hash和分析耗时，并按已证明的一一identity映射归一化后全部相同；退出Play后的Console为0错误。该A/B只证明`68dde5c0c + 39180583d`当前组合状态稳定，并为后续PoseGraph小步建立同schema、同Program／Projection身份的新A；它不把`161932`改写为PoseGraph单步通过证据，也不替代`155530`作为上一份已通过的PoseGraph基线和总基线对照。

后续Foot任务独立提交`943641f8c`并以`707b0df15`记录双脚落地后的骨盆共同高度实验；Performance任务随后完成Player Build、Smoke、1044 Tick零丢帧Replay和正式Capture，并删除5个已过期、会在Domain Reload误触发PrepareReplay的EditorPrefs Pending状态。两项任务均封口并释放后，Foot正式A为`Diagnostics/FootPlacementRuns/20260901-173426-115-81a3c9d1689c48159cbb5a0ff14d8e8d`，其Proof副本保存在run根。当前最终工作区重新建立的A为`Diagnostics/FootPlacementRuns/20260901-183922-499-1330f06a34634d34b614d6909519f38b`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-184022-060-351f7af3851f4f5b83f489a0aa07d80d.json`；同状态B为`Diagnostics/FootPlacementRuns/20260901-184047-794-d3be6ac77c254b32a23077ef35ca71cb`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-184146-483-295b0755a82644c9bf365d4421b7a12d.json`，B正式匹配A的1044帧。

`173426 -> 183922`与`183922 -> 184047`两段均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射。两段十份报告归一化后均为0差异，退出Play后Console为0错误。由此确认Performance最终工具改动、EditorPrefs陈旧Pending清理和当前重启状态没有继续改变Foot／Pelvis／Goal／FBBIK／Physical业务；`184047`成为PoseBone Contribution typed入口迁移的正式A。`943641f8c`的明确Foot业务变化仍按其独立实验归属，不伪装为PoseGraph无行为变化。

## PoseBone Contribution typed Handle

状态：提交`79a53af1e`已为PoseBone Contribution建立独立编译Handle、描述符Catalog和per-operation Result。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；唯一警告为既有`PipelineBlackboardValueInfoNode.m_ReportedSourceError`未使用字段，build server已关闭。Unity完成Domain Reload后Console为0错误，同输入A/B通过；任务3.3仍需等待Goal Assembler和FBBIK两个Family完成后整体勾选。

- Native Program只在`PoseBoneIKGoals` Operation编译时固化Input Pose Value、Contribution Value、Goal Workspace和Descriptor范围；旧`AnimationPoseGraphNativePoseBoneIkGoalRange`页及Operation上的`PoseBoneIkGoalsIndex`已经从Native执行记录删除。Program继续唯一持有同一份Descriptor NativeArray，并只把typed Catalog交给Constraint Runtime。
- Staged Executor现在只检查Handle和Component Pose是否可用，再在原Stage位置调用一次`ExecutePoseBoneContribution`；它不再读取Descriptor数组、构造Descriptor Slice、解释Goal Offset或检查Constraint Bank容量。Constraint Runtime是唯一Handle解析者，仍调用原`CharacterPoseBoneIkGoalSource.BuildGoals`并写入原Contribution槽位；typed Result冻结Producer、Callsite、Goal范围、Frame和Completion匹配。
- PoseBone Goal公式、描述符顺序、Goal编码、Assembler输入、FBBIK、Physical Writer、Operation Stage和数值顺序均未改。Handle内部不嵌套`NativeSlice`，避免把Native Container嵌入存放Operation的NativeArray；没有新增兼容入口、fallback或第二Descriptor Owner。

正式A为`Diagnostics/FootPlacementRuns/20260901-184047-794-d3be6ac77c254b32a23077ef35ca71cb`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-184146-483-295b0755a82644c9bf365d4421b7a12d.json`。候选B为`Diagnostics/FootPlacementRuns/20260901-185755-638-046be3a0a7474c458df6df3e59202359`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-185856-535-aa40943181f94445a406f230c2117421.json`；工具对A正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同，24个运行／实例／Surface／Path identity列全部一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同，5个identity列全部一一映射且0冲突。十份正式报告在排除Sample／文件hash、detail／index大小与hash和分析耗时，并按已证明的Surface identity映射归一化后全部相同。退出Play后的Console为0错误；editor-state最后一份快照为`is_playing=false`、Idle，但之后遥测标记`stale_status`，因此不把`ready_for_tools`作为本步通过证据。由此确认当前固定Trace覆盖的动画时钟、Foot、Pelvis、PoseBone Goal、Assembler、FBBIK和Physical结果未因PoseBone typed入口迁移改变；`185755`成为Goal Assembler typed入口迁移的正式A。

## Goal Assembler typed Handle

状态：提交`ea2ece645`已为Full Body IK Goal Assembler建立独立编译Handle、Contribution输入Catalog和per-operation Result。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告均来自既有依赖和未使用字段，build server已关闭。Unity Refresh完成Domain Reload并返回ready，Console为0错误；任务3.3还剩FBBIK Family，暂不整体勾选。

- Native Operation中的Goal Set输出Value和Contribution输入range现在由`CharacterFullBodyIkGoalAssemblerConstraintHandle`承载；非Assembler Operation保持无效Handle。Program仍唯一持有Contribution Value索引数组，只把验证过Goal Set容量的typed Catalog交给Constraint Runtime。
- Staged Executor删除了Contribution索引NativeArray字段、range校验和NativeSlice构造，只在原Operation位置传Handle并验证typed Result。Constraint Runtime是唯一Catalog解析者，并继续按原输入顺序调用同一个`CharacterFullBodyIkGoalAssembler.Assemble`；Goal冲突规则、Goal排序、Goal Set写入、Producer身份和Frame／Completion没有改。
- 失败仍沿原`CharacterFullBodyIkResult`返回并使当前Operation失败；没有吞错、默认Goal、兼容入口或第二Assembler路径。Handle不嵌套Native Container，Catalog只借用Program拥有的只读数组。

正式A为`Diagnostics/FootPlacementRuns/20260901-185755-638-046be3a0a7474c458df6df3e59202359`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-185856-535-aa40943181f94445a406f230c2117421.json`。候选B为`Diagnostics/FootPlacementRuns/20260901-191034-951-23c75316aae14fa5849ef2aa6179f84e`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-191131-417-22e056a627a245688aea35a1664481d6.json`；工具对A正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同，24个运行／实例／Surface／Path identity列全部一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同，5个identity列全部一一映射且0冲突。十份正式报告按既定Sample／文件hash、detail／index大小与hash、分析耗时和Surface identity规则归一化后0差异；退出Play后的Console为0错误。由此确认当前固定Trace覆盖的Contribution输入顺序、Goal Set、Foot、Pelvis、PoseBone Goal、FBBIK和Physical结果未因Goal Assembler typed入口迁移改变；`191034`成为FBBIK typed入口迁移的正式A。

## FBBIK typed Handle与Constraint Family合同闭合

状态：提交`2f9642805`已为FBBIK建立独立编译Handle和per-operation Result。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告均来自既有依赖和未使用字段，build server已关闭。Unity Refresh完成Domain Reload并返回ready，Console为0错误；Foot Placement、PoseBone Contribution、Goal Assembler和FBBIK四个Family现在都具备typed编译Handle与per-operation Result，任务3.3完成。

- `CharacterFullBodyIkConstraintHandle`把Solver、输入／输出Pose Value、输入Goal Set Value、Operation和Callsite固定为同一编译合同；Native Operation不再保存独立FBBIK索引，既有Diagnostics只通过Handle派生只读身份。
- Pose workspace仍由Program执行侧拥有：Staged Executor按原顺序验证输入并把Input Pose复制到Output Pose，只把Output Component Pose写View和Handle交给`ExecuteFullBodyIk`。Constraint Runtime继续调用原`SolvePrepared`，并保持Goal Set、BendHistory、Solver Outcome、Effector／Limb诊断采集及失败返回顺序不变。
- typed Result冻结Handle、Solve结果、Frame和Completion；Solver失败仍把同一Output Value标记为`FullBodyIkSolverInvalid`。没有复制Solver、默认Goal、兼容入口、fallback或第二Pose写入路径。

正式A为`Diagnostics/FootPlacementRuns/20260901-191034-951-23c75316aae14fa5849ef2aa6179f84e`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-191131-417-22e056a627a245688aea35a1664481d6.json`。候选B为`Diagnostics/FootPlacementRuns/20260901-192117-923-ad1dd64ee4e24e4ea8d0576f406fb253`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-192220-365-11c3b45ab4f54c568b06188339f5727f.json`；工具对A正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同，24个运行／实例／Surface／Path identity列全部一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同，5个identity列全部一一映射且0冲突。十份正式报告按既定规则归一化后0差异；退出Play后的Console为0错误。由此确认当前固定Trace覆盖的Goal Set消费、Solver、BendHistory、Foot、Pelvis、PoseBone Goal、FBBIK和Physical结果未因FBBIK typed入口迁移改变；`192117`成为后续Constraint Operation completion收口的正式A。该次提交只据此完成任务3.3，任务3.4至3.6留待独立代码对账。

## Constraint Operation调用与Complete闭包对账

四个Family完成typed入口后重新全仓核对调用点：PoseBone、Goal Assembler和FBBIK各只有Staged Executor原Operation分支中的一次调用；Foot同一Operation分支按World Context在`EvaluateFootPlacement`与`RecordUnavailableFootPlacement`之间互斥选择一次，不存在外层预执行、Constraint扫描Program、Diagnostics重放或第二Stage调度。`ExecuteStage`在调用前要求对应`CharacterPoseOperationCompletionPage`槽为空，调用后统一通过`TryCompleteOperation`写入一次`Completed`或`TypedInvalid`；`TryComplete`拒绝非空槽并记录重复Operation，因此任务3.4完成。

`CharacterPoseConstraintRuntime.CompleteFrame`只绑定同一Completion，核对Pending lease／lineage、Goal Set闭包、Solver Outcome和Foot pending frame，再构造唯一`CharacterPoseConstraintResult`。它不读取Operation数组、不维护Stage Schedule、不调用四个Execute入口、不重新运行Goal Assembler或Solver；任务3.5完成。连续三段`184047 -> 185755 -> 191034 -> 192117`固定Trace又证明内部Foot Placement、Pelvis、PoseBone Goal、Contribution、Assembler、Goal Set、FBBIK和BendHistory业务结果保持，因此任务3.2完成。任务3.6仍有调用方可见的Handle内部地址和Constraint诊断页访问，需要下一步实际收窄，不能由本次代码审计代替。

## Constraint Result与Pending Bank读取收窄

状态：提交`02ccb1b4d`已让四个per-operation Result把Handle、Contribution Header、Goal Set和Solve明细改为私有，只向Program执行侧提供身份匹配；Foot额外只公开业务Availability。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告均来自既有依赖和未使用字段，build server已关闭。

- Staged Executor不再读取Foot Handle中的Foot Descriptor／Contribution Value容量，也不再从Result读取Goal offset/count或回读Constraint Pending Goal Contribution。Ready与World Context Unavailable现在由同一次Foot typed Result直接映射到原`None`或`WorldContextUnavailable`结果，Constraint Runtime继续独占Contribution槽和Goal workspace范围校验。
- Executor初始化只通过`MatchesCompiledLayout`核对Program与Constraint容量，不再分别读取Constraint内部两个Bank数组长度；Operation ABI范围继续由Program自身容量验证。旧`GetPendingGoalContribution`入口已删除，没有保留诊断fallback或第二错误来源。

正式A为`Diagnostics/FootPlacementRuns/20260901-192117-923-ad1dd64ee4e24e4ea8d0576f406fb253`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-192220-365-11c3b45ab4f54c568b06188339f5727f.json`。候选B为`Diagnostics/FootPlacementRuns/20260901-193623-780-a483ca530d484c69abd2208ef698c394`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-193724-165-1ddd495782e2425f8be587bdb9c9ba05.json`；工具对A正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同，24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同，5个identity列一一映射且0冲突；十份正式报告归一化后0差异，退出Play后的Console为0错误。由此确认错误来源收窄没有改变World Context缺失政策、Foot、Pelvis、Goal、FBBIK或Physical业务；`193623`成为下一项Constraint外层可见面收窄的正式A。任务3.6仍未完成，因为Committed Goal／Foot／Solver／Physical Diagnostics页仍由外层与Snapshot Publisher直接读取，必须接入唯一Committed Diagnostics链后再删除，不能在本步伪装完成。

## 根执行Result接入Post-Seal诊断入口

状态：提交`b79b40c05`已让根`CharacterAnimationPresentationRuntime`在成功Seal后把同一`CharacterPoseFrameExecutionResult`直接交给Pose诊断入口。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告均来自既有依赖和未使用字段，build server已关闭。

- `BeginCommittedDiagnostics`现在要求Execution Result已Published，并核对Program／Constraint／Publication lineage、Committed Native页、Final Read和Constraint Bank属于同一Completion。旧入口不能再只凭Native Binding自行认定本帧已提交。
- `AnimationPresentationRuntimeSnapshotPublisher`的Final Summary已从typed Program／Publication Result读取Availability、Invalid Reason、Invalid Operation、PoseGraph Completion和Final Applied Completion。Native Final Read暂时只提供尚未迁移的Continuity、Foot Feature和Pose／Contribution明细，没有建立第二Snapshot或Sampler路径。

正式A为`Diagnostics/FootPlacementRuns/20260901-193623-780-a483ca530d484c69abd2208ef698c394`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-193724-165-1ddd495782e2425f8be587bdb9c9ba05.json`。候选B为`Diagnostics/FootPlacementRuns/20260901-200151-618-05e4e2bbaf264900b9e4916ca41089af`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-200300-335-62d17c8ab95346c380b1459c47dc9e6f.json`；工具对A正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后0差异，退出Play后的Console为0错误。由此确认Final Summary来源迁移没有改变Foot、Pelvis、Goal、FBBIK、Physical或正式诊断产物；`200151`成为Constraint Committed Diagnostics View迁移的正式A。任务13.1只完成根Execution Result接线，Source／Constraint／Final Publication的完整Committed诊断投影合同仍未闭合，不提前勾选。

## Constraint Result随Bank原子提交

状态：提交`8732331ce`让`CharacterPoseConstraintRuntime.CompleteFrame`产生的typed Result进入Pending状态，并在`SealFrame`与同一Bank一起提升为Committed Result；Discard同时清除Pending Result。Post-Seal诊断改为匹配根Execution中的Constraint Result，不再读取`CommittedBankIdentity`或`CommittedRenderFrame`。

第一次候选run为`Diagnostics/FootPlacementRuns/20260901-202209-561-eb7ae2d705544428bf4459712ab17f08`，在第1个Replay Tick后于Frame Commit失败，Console明确报告`Pose Constraint result is incomplete at seal`。原因是候选把Begin阶段Completion为0的lease lineage与完成后的Result lineage直接全等比较；这违反既有lease合同。失败run原样保留，不作为A/B通过证据。提交`700b29347`改为调用现有`lease.Matches(completedLineage)`，只修正完成身份匹配，不改变Constraint数据或执行顺序；修正后`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误，build server已关闭。

正式A为`Diagnostics/FootPlacementRuns/20260901-200151-618-05e4e2bbaf264900b9e4916ca41089af`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-200300-335-62d17c8ab95346c380b1459c47dc9e6f.json`。修正后的候选B为`Diagnostics/FootPlacementRuns/20260901-202513-054-4eb0ef66f0504dfc9a4bddfb0063eadf`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-202615-147-3f76faf457604e078d9bb63b24c618da.json`；工具对A正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。十份正式报告排除既定identity、hash、分析耗时和文件大小后0差异；本轮`analysis.performance.reportBytes`增加10，精确来自十个代表事件的Surface实例ID由5位变6位，使`contact-plane-penetration.json`增加10字节，Surface映射后的报告业务内容完全相同。退出Play后的Console为0错误。由此确认Constraint Result原子提交没有改变Foot、Pelvis、Goal、FBBIK、Physical或诊断业务；`202513`成为Constraint详细Committed Diagnostics View迁移的正式A。

## Constraint详细Committed Diagnostics短租约

状态：提交`5876c279a`已建立预分配的`CharacterPoseConstraintCommittedDiagnosticsView`。Constraint Runtime只在当前诊断Interest需要时，把已提交Goal Contribution、Goal Set和FBBIK Solver明细复制进单一Committed Diagnostics页；每次捕获都会更新代际身份，旧View在下一次捕获后失效。`AnimationPresentationRuntimeSnapshotPublisher`不再持有Constraint Runtime，也不再直接调用其Goal／Solver getter，只消费与根`CharacterPoseFrameExecutionResult`同lineage的短租约。运行求解、Goal装配、Bend History、Foot、Physical写入和Final Publication顺序未改；没有第二Snapshot、兼容入口或运行时分配。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0警告、0错误，build server已关闭。Unity Force Refresh与Domain Reload完成，回放前Console为0错误。正式A为`Diagnostics/FootPlacementRuns/20260901-202513-054-4eb0ef66f0504dfc9a4bddfb0063eadf`，候选B为`Diagnostics/FootPlacementRuns/20260901-203902-659-f1c1b305b4c64584b7395173f8fc52d7`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-203958-168-8a501c3144f644039da32345edcb516f.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告按既定Sample identity、文件hash、detail／index大小与hash、分析耗时和Surface identity规则归一化后0差异。退出Play后的Console为0错误。由此确认Goal Contribution、Goal Set与FBBIK详细诊断的读取边界迁移没有改变Foot、Pelvis、Goal、FBBIK、Physical或正式诊断业务；`203902`成为Foot Committed Diagnostics并入同一View的正式A。任务3.6仍未完成，因为Foot诊断页与Physical写入诊断还暴露在Constraint Runtime外层。

## Foot Committed Diagnostics并入Constraint短租约

状态：提交`f803f6acb`已把`CharacterFootLandingPredictionDiagnostics`并入同一`CharacterPoseConstraintCommittedDiagnosticsView`。Constraint Runtime删除`CommittedFootDiagnostics`、`HasCommittedFootDiagnostics`和空Foot页入口，只在当前Interest确实需要Foot诊断且Committed Bank持有完成值时，把该只读Frame引用复制进同一代际页。Pose执行外层与Snapshot Publisher不再单独持有或传递Foot诊断页；Foot、Goal和Solver明细现在由同一Constraint Result lineage与同一短租约提供。Foot求解、Landing Prediction生成、Debug Registry发布、Physical写入和最终采样内容未改。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告来自既有包和既有未使用字段，build server已关闭。Unity Force Refresh后、回放启动前出现两条既有FinalIK序列化深度日志，均指向`RootMotion.FinalIK::FBIKChain.reachSmoothing`，与本次未修改的FinalIK序列化类型一致；该刷新噪声单独保留，不计入运行时Console。清空后执行固定Trace，正式A为`Diagnostics/FootPlacementRuns/20260901-203902-659-f1c1b305b4c64584b7395173f8fc52d7`，候选B为`Diagnostics/FootPlacementRuns/20260901-204547-204-ce91af9890d84365a04fe6e3478924a3`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-204649-938-ab7905d1aaee442c9397ce89a255e9d3.json`，工具对A Proof正式报告`matched:1044`。

诊断封存末尾的MCP轮询一度路由到另一个Unity实例`pik@78ca1587a25567ad`并返回不支持`character.fixed_input_trace`；重新把活动实例固定为`3C_Client@e852139597e42532`后读取到完成状态、Proof和全部产物，未刷新或中断本项目。A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后0差异。回放运行期间与退出Play后的Console均为0错误。由此确认Foot诊断页收口没有改变Foot、Pelvis、Goal、FBBIK、Physical或正式诊断业务；`204547`成为Physical Committed Diagnostics迁移的正式A。任务3.6只剩Physical诊断外层读取尚未收口。

## Physical Writer与Committed Diagnostics归还Final Publication

状态：提交`b58bf055c`已把`AnimationFinalPosePhysicalWriter`的Binding校验、物理骨骼写入、Pending结果、Commit和Committed Diagnostics短租约整体迁入`ComposedAnimationPoseFramePublisher`。Final Publication Pending页要求Published Result与同Completion的Physical Write同时完成，Commit再原子提升Publication Result、Physical Write和最终Pose Frame。Constraint Runtime删除Physical Writer字段、构造依赖、Bank字段、写入入口、Committed getter和Physical Interest判断；它现在只负责Foot／Goal／FBBIK约束闭包。Snapshot Publisher同时消费Constraint与Final Publication两个同lineage短租约，不再从Constraint反推Physical事实。任务3.6和13.4完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；最终增量编译只有1个既有Input Value未使用字段警告，build server已关闭。Unity Force Refresh与进入Play期间共出现一条同类FinalIK序列化深度日志，仍只指向未修改的`RootMotion.FinalIK::FBIKChain.reachSmoothing`；它单独归为Domain Reload序列化问题，没有PoseGraph、Constraint、Final Publication或Physical Writer异常。正式A为`Diagnostics/FootPlacementRuns/20260901-204547-204-ce91af9890d84365a04fe6e3478924a3`，候选B为`Diagnostics/FootPlacementRuns/20260901-205740-295-b4b71ad545274c708ba65c3e3f964c43`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-205842-557-4019941246c8422a806011001837571f.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后0差异。另一个Unity实例`pik`在活动路由被外部切换后记录的`character.fixed_input_trace`不支持错误不属于`3C_Client`；重新固定目标后，`3C_Client`只有上述一条FinalIK序列化日志，清空后Console为0。由此确认Physical Owner迁移没有改变最终骨骼写入、Foot、Pelvis、Goal、FBBIK或正式诊断业务；`205740`成为下一项Committed Results／Diagnostics Projector收口的正式A。任务13.1仍未完成，因为Source与Program的完整Committed诊断投影合同尚未闭合。

## Final Summary与Detail消费已提交Pose Frame

状态：提交`6616494e2`已让Final Publication Committed Diagnostics短租约同时公开同一Commit原子提升的`ComposedAnimationPoseFrame`。Snapshot Publisher的Final Summary改从该Frame读取Continuity与Foot Features，Final Detail改从该Frame读取参数、可用性、已解析Contribution和dense骨骼权重；旧Native Final Read参数和Final Contribution二次转换已删除。第一次编译发现Slot／Operation／Pose Watch三类尚未迁移的Program Native诊断仍使用原`ConvertContribution`与Workspace Player映射，因此只恢复这三类现有依赖，没有把尚未完成的Program迁移伪装成本步完成，也没有建立第二映射。

`ThirdPersonClient.Runtime.csproj`按规定参数修正后编译成功，0错误、1个既有Input Value未使用字段警告，build server已关闭。Unity调用前均重新固定`3C_Client@e852139597e42532`，没有向`pik`发送本轮命令。正式A为`Diagnostics/FootPlacementRuns/20260901-205740-295-b4b71ad545274c708ba65c3e3f964c43`，候选B为`Diagnostics/FootPlacementRuns/20260901-211231-564-91170212f5ff4e81b74b8f185487655a`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-211331-224-0edf692479ec4354a64d6514b6603bca.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后0差异。3C退出Play后只记录同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`序列化深度日志，没有PoseGraph、Final Publication或Physical Writer异常，清空后Console为0。由此确认Final Summary／Detail来源切换没有改变最终Pose、Foot Features、Contribution、Foot、Pelvis、Goal、FBBIK、Physical或正式诊断业务；`211231`成为Program Committed Diagnostics迁移的正式A。任务13.1仍不勾选。

## Source Committed Result与物理身份短租约

状态：提交`f2928d966`已建立`CharacterPoseSourceCommittedResult`和预分配、代际校验的`CharacterPoseSourceCommittedDiagnosticsView`。Physical Source Registry在正式Commit与已准备Release全部完成后，按当前Source Frame lineage深冻结SourceId、Player Node、Owner和Physical generation映射；下一帧开始即使旧View失效。Snapshot Publisher删除`PhysicalPoseSourceRegistry`参数和所有直接Require调用，Slot／Operation／Pose Watch中的Live primitive Contribution只能通过Source View解析。运行时Final Publication仍使用正式Registry，不由诊断View反向驱动，没有第二Registry、默认Source或兼容解析路径。

`ThirdPersonClient.Runtime.csproj`按规定参数修正属性`in`局部值后编译成功，0错误、1个既有Input Value未使用字段警告，build server已关闭。Unity每次调用前均重新固定`3C_Client@e852139597e42532`。正式A为`Diagnostics/FootPlacementRuns/20260901-211231-564-91170212f5ff4e81b74b8f185487655a`，候选B为`Diagnostics/FootPlacementRuns/20260901-212326-424-516539050652496690b232e9e080844e`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-212424-716-58d8d8ef8c584788bd5b42ed5d5377c4.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后0差异。Editor.log证明本包先封存1043表现帧与全部诊断并报告`replay completed`，随后自由运行才在Frame 2781／Body Tick 3283的`ActionSampling`抛出`Predicted opposing landing pair is incomplete`；该阶段位于Source View与PoseGraph之前，不属于本步固定1044 Tick回放结果，也不伪装为FinalIK刷新噪声。日志中的Frame 2 Constraint Seal错误明确紧邻旧失败包`20260901-202209-561-eb7ae2d705544428bf4459712ab17f08`，不是本轮。停止后清空3C Console为0。由此确认当前固定Trace覆盖的Source映射、Contribution、Foot、Pelvis、Goal、FBBIK和Physical业务未因Source View迁移改变；`212326`成为Program Committed Diagnostics迁移的正式A。任务13.1仍不勾选，因为Program完整投影尚未闭合。

## Program Committed Result与interest分组冻结页

状态：提交`c9c3d2d1e`已建立预分配、代际校验的`CharacterPoseProgramCommittedDiagnosticsView`。Program Owner始终冻结typed Program Result、Operation Completion和PoseGraph invalid header；只在Live／Capture时冻结Slot primitive Contribution、dense权重与动态StateMachine骨骼权重，只在Operation Detail时冻结Value header、primitive Contribution与dense权重，只在Pose Watch时冻结Value header、primitive Contribution与完整Value Pose。下一Program Frame开始旧View立即失效。PlayerIndex到NodeId不新增静态副本，Projector只在有诊断interest时扫描现有Program Image Operation映射。

Snapshot Publisher已删除`CharacterPoseGraphNativeBinding`、`CharacterPoseGraphNativeProgram`、`AnimationPoseNativeWorkspace`和`PhysicalPoseSourceRegistry`类型引用；Slot／Operation／LinkedPose Completion／Pose Watch全部消费Program View，并用Source View解析Live primitive identity，用Constraint View读取Foot／Goal／FBBIK，用Final Publication View读取Final Pose与Physical。Pose Watch不再读取Workspace、重采样Source、执行World Query、调用FBBIK或反推Physical，因此任务13.1与13.7完成。Publisher仍读取Stack、Route、StateMachine、Inertialization与LinkedPose Actor Runtime快照，任务13.5和13.6暂不勾选。

`ThirdPersonClient.Runtime.csproj`按规定参数修正一次`in frame.Layout`属性局部值后编译成功，0错误、1个既有Input Value未使用字段警告，build server已关闭。Unity每次调用前均重新固定`3C_Client@e852139597e42532`。正式A为`Diagnostics/FootPlacementRuns/20260901-212326-424-516539050652496690b232e9e080844e`，候选B为`Diagnostics/FootPlacementRuns/20260901-214438-507-584a118b94b54a6b925bd47eb128147f`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-214536-991-ea955085cc19405d8d9de586e73ebe7f.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后0差异。诊断封存完成后立即停止3C，没有再出现上一包自由运行阶段的opposing landing pair错误；Console只记录同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`Domain Reload日志，清空后为0。由此确认Program冻结页与Projector消费切换没有改变Slot、Operation、Value、Pose Watch、Foot、Pelvis、Goal、FBBIK、Final Pose或Physical业务；`214438`成为Actor Runtime诊断投影收口的正式A。

## LinkedPose Committed Diagnostics短租约

状态：提交`64200fb54`已让`CharacterLinkedPoseRuntimeSession`拥有预分配、代际校验的`CharacterLinkedPoseCommittedDiagnosticsView`。LinkedPose仍按原顺序执行Selector、Group写入与Seal；Seal后才把同一Program Result和各Group已提交快照冻结进唯一诊断页。下一次Prepare或Reset会使旧View失效。Snapshot Publisher不再持有LinkedPose Runtime，也不再现场调用`CreateCommittedSnapshot`，只消费与根`CharacterPoseFrameExecutionResult`同lineage的短租约。提交同时修正前序精确暂存遗留在提交树中的构造器和`BeginCommittedDiagnostics`错位行，使提交本身不再依赖工作区残留。Stack、Route、StateMachine、Inertialization和RootWarp仍待迁移，所以任务13.5和13.6保持未完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0警告、0错误，build server已关闭。Unity首次重连刷新出现一次SourceAssetDB文件时间戳不同步，强制全量刷新后消失；回放和退出Play期间只有同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`Domain Reload序列化深度日志，没有PoseGraph、LinkedPose、IK或采样异常，清空后Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-214438-507-584a118b94b54a6b925bd47eb128147f`，候选B为`Diagnostics/FootPlacementRuns/20260901-215843-383-9b8dbac4d3c646b58e10695f2287f1d9`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-215943-008-0835d958c03e4e41b4a2424c17ce3ba2.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告按既定Sample identity、文件hash、detail／index大小与hash、分析耗时和Surface identity规则归一化后10／10相同，七维分项与总分84.2不变。由此确认LinkedPose诊断读取边界迁移没有改变Linked Pose Group、Slot、Operation、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`215843`成为剩余Actor Runtime诊断投影收口的正式A。

## Root Orientation Warp并入Actor Committed Diagnostics页

状态：提交`92ebd3984`已建立预分配、代际校验的`CharacterPoseActorCommittedDiagnosticsView`并先接入Root Orientation Warp。每个Pose帧开始会立即使上一代View失效；只有已完成Program Result且Live／Capture确实需要基础状态时，Projector才从各Warp的Committed页复制只读快照。Snapshot Publisher删除RootWarp Runtime列表参数和现场`CreateDiagnosticsSnapshot`调用，只消费与根Execution Result同lineage的Actor View。RootWarp的Begin、Prepare、Commit、曲线采样、Facing Error与Yaw Offset公式均未改。Stack、Route、StateMachine与Inertialization仍由Snapshot现场读取，所以任务13.5和13.6保持未完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告只来自既有Unity包、第三方包和既有Input字段，build server已关闭。Unity脚本刷新重连时出现一次Snapshot Publisher文件时间戳不同步，强制全量刷新后消失；回放和退出Play期间只有同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`Domain Reload序列化深度日志，没有RootWarp、PoseGraph、IK或采样异常，清空后Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-215843-383-9b8dbac4d3c646b58e10695f2287f1d9`，候选B为`Diagnostics/FootPlacementRuns/20260901-221205-709-14169caf31c54ac9941f5f68c966f9d6`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-221307-811-afa95c8ad09d4e58b73d3d55708eb85f.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后10／10相同，七维分项与总分84.2不变。由此确认Root Orientation Warp诊断读取边界迁移没有改变Warp输出、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`221205`成为Stack／Route诊断投影收口的正式A。

## Blend Stack与Transition Route并入Actor Committed Diagnostics页

状态：提交`03a86742d`已把Blend Stack、Entry、Entry／Stored骨骼权重与Animation Slot Route快照并入同一`CharacterPoseActorCommittedDiagnosticsView`。Projector只在Program Result已完成且Live／Capture需要基础状态时调用现有Stack／Route快照入口，把结果写进构造期预分配的固定容量页；Snapshot Publisher删除Stack与Route Runtime参数、现场`CopyDiagnostics`和`CreateSlotSnapshot`调用，只复制短租约中的已冻结数组。Stack Advance、Source更新、Entry权重、Stored Pose、Route决策、Release权限、Inertialization请求和Commit顺序均未改。StateMachine与Inertialization仍由Snapshot现场读取，所以任务13.5和13.6保持未完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告只来自既有Unity包、第三方包和既有Input字段，build server已关闭。Unity全量刷新后只有既有FinalIK序列化深度日志。回放封存和A/B完成后读取Console时，另一个MCP观察请求在`MCPForUnity.Editor.Resources.Scene.GameObjectComponentsResource`中反射Animator属性，产生两条OnAnimatorIK限制、三条无AnimatorController和一条非Playback状态错误；调用栈全部位于用户目录下的MCPForUnity `GameObjectSerializer`，没有项目Runtime、Pose、Stack、Route或IK调用栈。该外部观察干扰单独记录，清空后Console为0，不计作运行逻辑回归。

正式A为`Diagnostics/FootPlacementRuns/20260901-221205-709-14169caf31c54ac9941f5f68c966f9d6`，候选B为`Diagnostics/FootPlacementRuns/20260901-221941-106-77c66c8fd22b46898deb7876a8598bc4`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-222039-293-2dac8b71bd0947eda9374049ba410aff.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后10／10相同，七维分项与总分84.2不变。由此确认Stack／Route诊断读取边界迁移没有改变动画选择、过渡、Stored Pose、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`221941`成为StateMachine／Inertialization诊断投影收口的正式A。

## Pose StateMachine并入Actor Committed Diagnostics页

状态：提交`cda8bbbcc`已把Pose StateMachine的Committed header并入同一`CharacterPoseActorCommittedDiagnosticsView`。Projector在基础状态Interest下复制现有`CreateSnapshot`结果；StateMachine动态骨骼权重继续只来自已验证的Program Committed Diagnostics页。Snapshot Publisher删除StateMachine Runtime列表参数和现场快照调用，用Actor header与Program权重组合最终诊断页。State选择、Transition、权重生成、Source准备、StateMachine Commit和Linked Pose关系均未改。当前Actor运行对象中只剩Inertialization仍由Snapshot现场读取，所以任务13.5和13.6继续保持未完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告只来自既有Unity包、第三方包和既有Input字段，build server已关闭。Unity全量刷新、回放与退出Play期间只有同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`Domain Reload序列化深度日志，没有StateMachine、PoseGraph、IK或采样异常，清空后Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-221941-106-77c66c8fd22b46898deb7876a8598bc4`，候选B为`Diagnostics/FootPlacementRuns/20260901-222523-572-cc193515ef1c401893575fb46a03b060`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-222620-545-d5077e9ee35649258985d39c37dba17f.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后10／10相同，七维分项与总分84.2不变。由此确认StateMachine诊断读取边界迁移没有改变状态选择、过渡、骨骼权重、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`222523`成为Inertialization诊断投影收口的正式A。

## Inertialization并入Actor Committed Diagnostics页

状态：提交`6c05135c1`已把Inertialization header、规则解析结果、每骨骼Position／Rotation／Scale residual和Envelope并入同一`CharacterPoseActorCommittedDiagnosticsView`。原Snapshot中的读取与转换代码原样迁入Actor Projector，写入构造期预分配数组；Snapshot Publisher只复制短租约，不再接收`PoseInertializationNativeProgram`。至此Snapshot `BeginFrame`只组合Source、Program、LinkedPose、Actor、Constraint与Final Publication六种同lineage committed view，不再持有Stack、Route、StateMachine、RootWarp或Inertialization Runtime。Inertialization事件、规则、历史、Accumulator、Residual计算、Commit和Pose输出均未改。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告只来自既有Unity包、第三方包和既有Input字段，build server已关闭。Unity全量刷新、回放与退出Play期间只有同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`Domain Reload序列化深度日志，没有Inertialization、PoseGraph、IK或采样异常，清空后Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-222523-572-cc193515ef1c401893575fb46a03b060`，候选B为`Diagnostics/FootPlacementRuns/20260901-223152-465-a85a2c8b60e241cba07a6f2177d34128`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-223250-104-0d890701c71549b4b4a0d7108b69a43d.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后10／10相同，七维分项与总分84.2不变。由此确认Inertialization诊断读取边界迁移没有改变过渡连续性、Residual、Envelope、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`223152`成为Source Release／Clip／BlendSpace诊断投影收口的正式A。任务13.5和13.6仍不勾选，因为Snapshot `Publish`尚直接读取Source Runtime。

## Clip与BlendSpace并入Actor Committed Diagnostics页

状态：提交`99e2af1fc`已把Clip Player的Foot Step observation和Blend Space Player／Sample深冻结进现有`CharacterPoseActorCommittedDiagnosticsView`。Clip／BlendSpace的选择、时钟和Player状态继续属于Program Actor State，没有被塞进Physical Source Registry或新建第二Source owner。Clip在Owner Capture时按原SampleTime、Cycle、Duration、loop flag和曲线生成零权重observation，Snapshot再用同页已冻结Final Contribution的dominant source weight构造最终值；Blend Space在下一Begin／Advance覆盖前深拷贝Player与Sample，并继续用已冻结Operation Result补Availability。`AnimationPresentationRuntimeSnapshotPublisher.Publish`删除Clip／BlendSpace Runtime参数，只剩release数组尚待迁移。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告只来自既有Unity包、第三方包和既有Input字段，build server已关闭。Unity全量刷新、回放与退出Play期间只有同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`Domain Reload序列化深度日志，没有Clip、BlendSpace、PoseGraph、IK或采样异常，清空后Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-223152-465-a85a2c8b60e241cba07a6f2177d34128`，候选B为`Diagnostics/FootPlacementRuns/20260901-225118-668-312b83f59ad84af8a3119303602e15ce`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-225216-854-00387762475341d8bea73b427587e7c1.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后10／10相同，七维分项与总分84.2不变。由此确认Clip observation与Blend Space诊断读取时机迁移没有改变source时间、曲线采样、Blend权重、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`225118`成为release completion迁入Source committed view的正式A。任务13.5和13.6只剩Snapshot `Publish`的release数组旁路。

## Release completion归还Source Committed Diagnostics页

状态：提交`23bc1279b`已把release diagnostics interest、固定容量journal、三类release记录、Discard／Reset取消和最终冻结归还`PhysicalPoseSourceRegistry`的`CharacterPoseSourceCommittedDiagnosticsView`。Prepare阶段在真实release发生前冻结是否记录；Stack、Standalone和Action release继续在原调用点、原completion identity记录；全部deferred release执行完成后关闭journal，Capture才把它与当前物理source mapping冻结进同一Source committed页。旧`PosePlanExecutionRuntime.m_ReleasedSources`、count和record flag全部删除；`AnimationPresentationRuntimeSnapshotPublisher.Publish()`变为零参数，只提升`BeginFrame`已经按同lineage组合好的页。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告只来自既有Unity包、第三方包和既有Input字段，build server已关闭。Unity全量刷新、回放与退出Play期间只有同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`Domain Reload序列化深度日志，没有release lifecycle、PoseGraph、IK或采样异常，清空后Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-225118-668-312b83f59ad84af8a3119303602e15ce`，候选B为`Diagnostics/FootPlacementRuns/20260901-230258-692-d855ef3956c74e8290b94323219372a5`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-230359-450-847a166b52ea4caab319436325cad50a.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后10／10相同，七维分项与总分84.2不变。由此确认release journal Owner迁移没有改变source释放时机、slot复用、Action completion、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`230258`成为正式Source Module提取的下一A。Snapshot对象本身已不持有Runtime参数，但Actor capture仍由外层读取多个运行Implementation，任务13.5和13.6继续不勾选，待Program Runtime／Source Module内聚后闭合。

## CharacterPoseSourceModule物理资源唯一Owner

状态：提交`625074c30`已建立真实`CharacterPoseSourceModule`，由它唯一构造、持有和销毁`AnimancerPoseSamplingBackend`、`PhysicalPoseSourceRegistry`与source fan-in Playable，并在同一Module内完成物理source注册、capture binding安装、fan-in连接、release identity验证、物理断连、deferred release封口和Committed Source diagnostics冻结。`PosePlanExecutionRuntime`已删除backend、registry、fan-in、previous output和release identity set所有权；Blend Stack与Final Publication只通过Source Module解析物理identity，不再接收Registry实现对象。全仓搜索确认上述三个物理资源各只有Source Module一处构造，因此任务4.3完成。

本步没有把Clip／Blend Space Player时钟、PoseState、Action lifecycle、Slot或Blend逻辑迁入Source Module。它们继续留在现有逻辑Owner，Source Module只接收已经解析的request、capture binding与release许可。Clip／Blend／Action Adapter装配、Source-owned固定页、完整release journal迁移及旧Runtime数组删除仍属于4.1、4.2、4.4至4.8，均未提前勾选。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；唯一工程警告为既有`PipelineBlackboardValueInfoNode.m_ReportedSourceError`未使用字段，build server已立即关闭。Unity脚本刷新、回放和退出Play期间只出现同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`Domain Reload序列化深度日志，没有Source Module、Playable、release、PoseGraph或IK异常；单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-230258-692-d855ef3956c74e8290b94323219372a5`，候选B为`Diagnostics/FootPlacementRuns/20260901-233212-574-ed0f12eb26024a79906148a33263ab74`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-233313-409-4bbe705712294d1ea4f42adc6a1f2474.json`，工具对A Proof正式报告`matched:1044`、`divergent_frame_count=0`和`first_divergent_relative_frame=-1`，Runtime identity、Trace、Start Body、Tick／Presentation Clock、Input hash、Body trajectory hash与1043个表现采样帧均保持一致。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。十份正式报告只在Sample identity、文件／index hash、detail字节与分析耗时以及已证明的一一Surface identity上变化，归一化后10／10相同；七维分项、总分84.2和weighted evidence 96.5不变。由此确认本Trace覆盖的source采样与释放时机、slot复用、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose和Physical业务没有因物理Source所有权迁移改变；`233212`成为Source Adapter与Owned页迁移的下一A。

## Source-owned Release Preparation页

状态：提交`4f54a06ab`已在`CharacterPoseSourceModule`内新增固定容量、单调generation的release preparation页。每次release validation后，Module把Physical Registry token与Animancer backend token成对写入自己的私有槽位，只向旧协调Runtime签发包含槽位、generation、physical identity、Source和Node identity的typed `ReleasePreparation`。Standalone、Stack和Action三类外层journal不再分别保存两个Implementation token；成功帧后的断连、backend release与Registry apply统一由Source Module按同一handle执行，重复、过期或未消费handle直接拒绝。Discard会随Physical Source页一起清空Pending preparation，下一Frame和deferred release封口都会拒绝残留handle。

本步没有改变谁决定retirement、Stack／Slot何时允许释放、Action completion identity、Route notification或release diagnostics记录时机；这些逻辑仍在原调用位置按原顺序执行。外层三类journal、Action request／acknowledgement与release completion尚未整体迁入Source-owned页，因此任务4.1和4.4继续保持未完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；唯一工程警告为既有Input Value未使用字段，build server已立即关闭。Unity全量刷新、回放和退出Play期间只出现同一条FinalIK Domain Reload序列化深度日志，没有release generation、capacity、stale handle、Source lifecycle、PoseGraph或IK异常；单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-233212-574-ed0f12eb26024a79906148a33263ab74`，候选B为`Diagnostics/FootPlacementRuns/20260901-234825-639-d86985e088c8402d9d0f7c6315a4223d`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-234924-881-6531da2c29b149e9a57d163b87218989.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告按既定Sample identity、文件／index hash、detail字节、分析耗时与Surface identity规则归一化后10／10相同，七维分项、总分84.2和weighted evidence 96.5不变。由此确认release token成对所有权迁移没有改变source释放时机、slot复用、Action completion、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose或Physical业务；`234825`成为完整release journal迁移的下一A。

## Source-owned Frame Demand与Result页

状态：提交`d938f7a96`已把唯一`CharacterPoseSourceFrameLease`、Demand、Result、ready、Seal和Discard状态从`AnimancerPoseSamplingBackend`迁入`CharacterPoseSourceModule.SourceFramePage`。Source Module现在先创建同lineage lease，再让backend只按该lease的Frame identity打开物理mutation；Demand与Source Frame Result只写Source-owned页，Validate和Evaluate Barrier前由Module验证ready，Commit按原顺序先关闭backend、Seal页，再提交Physical Registry；Barrier前Discard仍先丢backend mutation和页，之后按原顺序丢Physical Registry页。

`AnimancerPoseSamplingBackend`已删除`CharacterPoseSourcePendingPage`、Demand／Result字段、lease创建、Bind／Require接口与Seal／Discard业务页调用，只保留SourceVisual、owner slot、clip plan、release permission、deferred resource和Playable capture mutation。全仓搜索确认`new CharacterPoseSourceFrameLease`及Demand／Result Pending字段只剩Source Module一处。Source Binding、Prepared Resource、Usage、Release completion完整页以及只消费Program Demand／Usage的最终窄Interface仍未全部闭合，因此任务4.1和4.5保持未完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；依赖警告和既有Input Value未使用字段不属于本步，build server已立即关闭。Unity刷新、回放和退出Play期间只出现同一条FinalIK Domain Reload序列化深度日志，没有Source lease、Pending page、Barrier、Playable、PoseGraph或IK异常；单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-234825-639-d86985e088c8402d9d0f7c6315a4223d`，候选B为`Diagnostics/FootPlacementRuns/20260901-235713-270-10be4886ce4a48f0ab7eedb009127664`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-235813-753-468dd009d8d240e587fd65dad221fb24.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。十份正式报告只在既定Sample identity、文件／index hash、detail／index字节、分析耗时和一一Surface identity上变化；其中`analysis.index.bytes`及`performance.indexBytes`仅少1字节，排除既定index大小字段后10／10相同，七维分项、总分84.2和weighted evidence 96.5不变。由此确认Source Frame页所有权迁移没有改变Demand消费、Source采样、Barrier时机、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose或Physical业务；`235713`成为Source Binding／Usage页迁移的下一A。

## Direct／Clip／BlendSpace Source Binding页

状态：提交`358647863`已在`CharacterPoseSourceModule`内建立固定容量Direct、Clip和Blend Space Binding页。每个Prepare阶段按同一Completion identity清空并打开页；物理source注册、backend prepare、capture binding安装和fan-in连接完成后，Module把`AnimationPhysicalSourceIdentity + capture SourceIndex`作为typed `SourceBinding`写入对应Player槽位。Program侧准备Player Job时只按当前Completion读取Binding；未激活Player读取默认无效Physical identity与`SourceIndex=-1`，保持原行为。

`PosePlanExecutionRuntime`已删除`m_DirectPhysicalSources`、`m_SequencePhysicalSources`、`m_BlendSpacePhysicalSources`和三组SourceIndex数组，以及对应构造、每帧清空和写入代码。Player的Relevant判断、Clip／BlendSpace时钟、source-local sample、continuity、PrepareCapture与PrepareJob仍由原逻辑Owner执行；Source Module不选择Player、State或跨source权重。旧Runtime仍保存release pool、usage和其它source控制journal，因此任务4.1与4.6均未整体完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告只来自既有Unity／第三方依赖和Input Value未使用字段，build server已立即关闭。Unity刷新、回放和退出Play期间只出现同一条FinalIK Domain Reload序列化深度日志，没有Source Binding、Completion、Player Job、PoseGraph或IK异常；单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-235713-270-10be4886ce4a48f0ab7eedb009127664`，候选B为`Diagnostics/FootPlacementRuns/20260902-000722-129-1675f4cae70142a1a4c177739208b7bd`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-000845-600-5dd258f8bc9c4492b4e44f76922fc0a1.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告按既定Sample identity、文件／index hash与大小、分析耗时和Surface identity规则归一化后10／10相同，七维分项、总分84.2和weighted evidence 96.5不变。由此确认physical identity scratch迁入Source Binding页没有改变Direct／Clip／BlendSpace绑定、Source时间、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose或Physical业务；`000722`成为Source Usage／Adapter迁移的下一A。

## CharacterPoseSourceFrameResult唯一Publisher

状态：提交`a8422da09`已让`CharacterPoseSourceModule`从自己的Pending Demand页读取唯一Demand，构造、校验并绑定`CharacterPoseSourceFrameResult`后把typed Result返回Program侧。`PosePlanExecutionRuntime`不再直接`new CharacterPoseSourceFrameResult`或调用Source页`BindResult`，只保留原`IsReady`失败政策并把成功Result交给后续Program Prepared合同。Action／provider sample字典、availability判断、字段顺序和Source Adapter数学均未改变。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告只来自既有Unity／第三方依赖与Input Value未使用字段，build server已立即关闭。Unity刷新、回放和退出Play期间只出现同一条FinalIK Domain Reload序列化深度日志，没有Source Result、Pending page、PoseGraph或IK异常；单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-000722-129-1675f4cae70142a1a4c177739208b7bd`，候选B为`Diagnostics/FootPlacementRuns/20260902-001521-259-0a58993b2ae64106b5e93e82413de6b7`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-001644-239-6ae2ea944e0e404db45e61069be963dc.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告按既定Sample identity、文件／index hash与大小、分析耗时和Surface identity规则归一化后10／10相同，七维分项、总分84.2和weighted evidence 96.5不变。由此确认Source Frame Result唯一写入Owner迁移没有改变sample readiness、Source时间、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose或Physical业务；`001521`成为新诊断架构文档对账后的下一PoseGraph实现基线。

## Constraint Pending页删除独立Completion身份

状态：候选已删除`CharacterPoseConstraintRuntime.Bank.CompletionIdentity`及`BindCompletion`。Constraint Pending页现在只保存根`CharacterPoseConstraintFrameLease`的开放lineage，并只响应根Seal／Discard；Foot、PoseBone Contribution、Goal Set、Solver Outcome和最终`CharacterPoseConstraintResult`仍各自携带并验证原Completion identity，`CompleteFrame`继续以根完成lineage核对全部typed结果。正常执行顺序、公式、Goal、BendHistory与Fault政策均未改变。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告只来自既有Unity／第三方依赖和Input Value未使用字段，build server已立即关闭。Unity刷新、回放和退出Play只出现同一条FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-001521-259-0a58993b2ae64106b5e93e82413de6b7`，候选B为`Diagnostics/FootPlacementRuns/20260902-004806-171-217a25ba6e794a92b95a49f903d35030`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-004904-251-bce67b695be34b298b17e657414381fb.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。十份正式报告除Sample／文件／index hash、detail字节与分析耗时外归一化后10／10相同；七维分项、总分84.2和weighted evidence 96.5不变。由此确认删除Constraint Pending的第二Completion身份没有改变Foot、Pelvis、Goal、Assembler、Bend、FBBIK、Final Pose、Physical或正式诊断业务；任务3.7完成，`004806`成为下一PoseGraph小步的正式A。

## Source release成对Owner收口

状态：候选已删除`CharacterPoseSourceModule`对外暴露的原始`AnimationPhysicalSourceReleaseToken`准备／应用入口。Blend Stack正常retirement以及Direct、Clip、Blend Space清理现在全部取得Module-owned `ReleasePreparation`；该token在同一固定页中成对保存backend与physical release，并由Source Module唯一执行fan-in断开、backend release和Physical Registry release。Player／Stack仍只提交自己的逻辑release，不接触Playable或Physical资源。

本步没有勾选任务4.4：prepared source创建、slot reuse、retirement permission和全部release completion还需继续从旧Runtime迁入Source Module；这里只关闭了已确认的physical-only旁路。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告只来自既有Unity／第三方依赖和Input Value未使用字段，build server已立即关闭。Unity刷新、回放和退出Play只出现同一条FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-004806-171-217a25ba6e794a92b95a49f903d35030`，候选B为`Diagnostics/FootPlacementRuns/20260902-005827-531-c0d4c41e222548d4b3f49b20e40662ef`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-005923-994-17e8b72516ff4cbaac3c542ab77a5b1d.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。十份正式报告在排除既定运行identity、文件／index hash与大小和分析耗时后逐字段语义对账10／10相同；七维分项、总分84.2和weighted evidence 96.5不变。由此确认成对release Owner收口没有改变source retirement时机、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`005827`成为下一Source生命周期小步的正式A。

## Source Frame原子打开backend与physical页

状态：候选已把Physical Source页的`BeginFrame`归入`CharacterPoseSourceModule.BeginFrame`。Source Module现在原子打开自己的Frame页、backend页与physical registry页；physical页打开失败时由Module先Discard backend，再丢弃自己的Frame页。`PosePlanExecutionRuntime`不再知道Source Frame还需要第二次`BeginPhysicalFrame`，正常帧中的页面打开顺序和后续Source准备顺序保持。

本步仍不勾选任务4.4；完整Discard、retirement permission、slot reuse和release completion还未全部藏入Source Module。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告只来自既有Unity／第三方依赖和Input Value未使用字段，build server已立即关闭。Unity刷新、回放和退出Play只出现同一条FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-005827-531-c0d4c41e222548d4b3f49b20e40662ef`，候选B为`Diagnostics/FootPlacementRuns/20260902-010730-937-b4dbbc29df944f31919080c7110ccd6a`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-010827-022-5beb8a8de1404acd9eca3428001c3237.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。十份正式报告排除既定运行identity、文件／index hash与大小和分析耗时后逐字段语义对账10／10相同；七维分项、总分84.2和weighted evidence 96.5不变。由此确认Source三页原子打开没有改变source准备、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`010730`成为下一Source生命周期小步的正式A。

## Source Binding页由Demand唯一开启

状态：候选已把`BeginBindingFrame`收为`CharacterPoseSourceModule`私有方法，并在`BindDemand`内直接使用唯一`CharacterPoseSourceDemand.Lineage.CompletionIdentity`清空并开启Direct、Clip与Blend Space Binding页。`PosePlanExecutionRuntime.PrepareEvaluation`不再单独解释Source Binding页的Completion；它继续只负责Program Workspace和各逻辑Player自己的Frame页。

结合前两步的成对ReleasePreparation与三页原子Begin，本步完成任务4.4：全文审计确认`AnimancerPoseSamplingBackend`、`PhysicalPoseSourceRegistry`、backend／physical release token、Source slot reuse、精确retirement校验和deferred release completion只在`CharacterPoseSourceModule`内部；外层只保留Program-owned Player／Stack逻辑release token和不透明Module `ReleasePreparation`。任务4.5与4.6仍未完成，Source Demand／Usage装配和旧Runtime控制journal继续后续迁移。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告只来自既有Unity／第三方依赖和Input Value未使用字段，build server已立即关闭。Unity刷新、回放和退出Play只出现同一条FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-010730-937-b4dbbc29df944f31919080c7110ccd6a`，候选B为`Diagnostics/FootPlacementRuns/20260902-011531-535-ebf6e24df7c048589d9fc5d797ab68f4`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-011629-347-466ed65576c148058206b4e506426714.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。正式报告的规则、eligible／matched、七维分项、总分84.2和weighted evidence 96.5保持；只允许既定运行identity、文件／index hash与大小和分析耗时变化。由此确认Binding页Owner迁移没有改变source绑定、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`011531`成为下一Source生命周期小步的正式A。

## 建立Foot IK具体Committed Capture存储Owner

状态：候选把旧`AnimationFootPlacementRuntimeSnapshotPage`原地改名为`CharacterFootIkCommittedCaptureViewPage`。该页仍是原来的唯一预分配Foot／FBBIK／Physical存储，旧`AnimationFootPlacementRuntimeSnapshot`只作为同一页、同一`FinalAnimationPoseFramePageLease`的只读Live外观；没有新增第二页、复制字段或改变租约失效规则。后续小步将在这份唯一页上补齐Foot Motion／Observation和具体`CharacterFootIkCommittedCaptureViewLease`，而不是从旧Snapshot再拼一份DTO。

本步不勾选任务13.5：具体View Lease尚未公开，旧Foot Sampler仍通过Snapshot join消费。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告只来自既有Unity／第三方依赖和Input Value未使用字段，build server已立即关闭。Unity刷新、回放和退出Play只出现同一条FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-011531-535-ebf6e24df7c048589d9fc5d797ab68f4`，候选B为`Diagnostics/FootPlacementRuns/20260902-012614-285-9e69b176ff8e45108b947330ca3927d3`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-012710-474-40cf684b38ff434eaf4f96f818c5f7ba.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。正式报告规则、七维分项、总分84.2和weighted evidence 96.5保持。由此确认具体Foot IK页命名收口没有改变任何Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或诊断业务；`012614`成为具体View内容迁移的正式A。

## Foot Motion与Observation归入具体View页

状态：候选把`LeftFootSteps`、`RightFootSteps`、`HasFootFeatures`与`FootStepObservation`从通用Snapshot根页迁入唯一`CharacterFootIkCommittedCaptureViewPage`。旧`AnimationPresentationRuntimeSnapshot`不再保存这些字段的独立值，其公开属性只通过同一`AnimationFootPlacementRuntimeSnapshot`租约读取具体Foot页；Foot／FBBIK／Physical数据仍在同一页，未增加复制或第二事实源。

本步不勾选任务13.5：具体`CharacterFootIkCommittedCaptureViewLease`和完整lineage仍待发布，旧Sampler也尚未切换。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告只来自既有Unity／第三方依赖和Input Value未使用字段，build server已立即关闭。Unity刷新、回放和退出Play只出现同一条FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-012614-285-9e69b176ff8e45108b947330ca3927d3`，候选B为`Diagnostics/FootPlacementRuns/20260902-013455-708-d230b17fe48240658b668d27e5ec1741`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-013550-868-1924e050c6e946e08584db38174c3a73.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。正式报告规则、七维分项、总分84.2和weighted evidence 96.5保持。由此确认Foot Motion／Observation存储归位没有改变Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或诊断业务；`013455`成为具体View Lease建立的正式A。

## 发布具体CharacterFootIkCommittedCaptureViewLease合同

状态：候选已删除`AnimationFootPlacementRuntimeSnapshot`旧类型名，唯一替换为公开`CharacterFootIkCommittedCaptureViewLease`。具体页在同一Committed Result投影中保存完整`CharacterPoseFrameLineage`，View继续通过同一`FinalAnimationPoseFramePageLease`验证有效期，并公开Foot Motion、FootStep Observation、Foot／Pelvis／Goal、FBBIK与Physical只读事实。通用`AnimationPresentationRuntimeSnapshot`只保留`FootIkCommittedCaptureView`这一具体属性；Trace、Overlay、Gizmo、Visual Validation和旧Sampler都已编译切换到该类型，不保留旧类型别名或wrapper。

本步不勾选任务13.5：Pose Runtime的`PublishDiagnostics`尚未把具体View作为独立Post-Seal结果交给领域Bridge，旧Sampler仍从Debug View取得它并进行二次join。Runtime与Editor工程均按规定参数编译成功、0错误，构建后立即关闭build server；警告只来自既有Unity／第三方依赖与未使用字段。Unity刷新、回放和退出Play只出现同一条FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-013455-708-d230b17fe48240658b668d27e5ec1741`，候选B为`Diagnostics/FootPlacementRuns/20260902-014359-013-28fb4dea2ad6400cac2a3a4c3ded43a7`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-014455-432-8ad76a8566f74f2b814e23afa5297708.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。正式报告规则、七维分项、总分84.2和weighted evidence 96.5保持。由此确认具体View类型与完整lineage合同没有改变Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或诊断业务；`014359`成为独立Post-Seal交付的正式A。

## 由PoseGraph直接交付Post-Seal Foot IK View

状态：`PosePlanExecutionRuntime.PublishDiagnostics`不再只返回是否发布的布尔值，而是在唯一Diagnostics页成功发布后直接返回同页、同`FinalAnimationPoseFramePageLease`的`CharacterFootIkCommittedCaptureViewLease`。`CharacterAnimationPresentationRuntime`把这个Post-Seal结果串行交给现有Foot Trace入口，不再从`m_DebugView.PosePlan`注册结果回读同一View。具体View仍只由Committed Source、Program、Actor、Constraint与Final Publication诊断投影组成，完整核对`CharacterPoseFrameLineage`；它不持有Program Runtime、Workspace、Constraint Module或Physical Transform，也不知道Capability、Sampler、Schema、Generated Program、packet、Host、CSV和Analyzer。通用Snapshot只引用同一具体页租约，没有第二Foot事实页或第二Snapshot。

由此完成任务13.5。任务13.6与13.10仍不勾选：旧Sampler仍在Foot事件后把这份View与旧Snapshot外层信息二次join，尚未由Foot Bridge在租约内调用生成Program并提交typed packet。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功、0错误；27个警告只来自既有Unity／第三方依赖与未使用字段，构建后立即关闭build server。Unity回放与退出Play只出现既有FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-014359-013-28fb4dea2ad6400cac2a3a4c3ded43a7`，候选B为`Diagnostics/FootPlacementRuns/20260902-015216-834-ccd49fe20da6479b91b0913c7c96627e`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-015313-475-06524d6bc7d546b3a2b9e4a2d2981ab2.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。正式报告的Analyzer版本75、coverage、规则、eligible／matched、七维分项、总分84.2和weighted evidence 96.5保持；只允许既定运行identity、文件／index hash与大小和分析耗时变化。由此确认Post-Seal直接交付没有改变Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或诊断业务；`015216`成为Foot Bridge接入前的正式A。

## Source Module原子丢弃完整内部帧

状态：`CharacterPoseSourceModule.DiscardFrame`现在一次接收唯一`CharacterPoseSourceFrameLease`，在Module内部逆序断开本帧pending physical registration，并尽量完成Backend页、Source Frame页、Physical Registry页、Binding与Release Preparation清理，最后统一抛出聚合故障。`PosePlanExecutionRuntime`无论Begin失败还是正式Discard都只请求Source Module丢弃自己的帧，不再读取`HasBackendFrame`、`HasPhysicalFrame`、`PendingRegistrationCount`，也不再取得physical identity后手动断Fan-In端口。旧分裂Discard入口和外层`DiscardPreparedPhysicalSource`已删除。

任务4.6仍不勾选：外层尚有Standalone／Action release journal及Clip、Blend Space、Motion Matching控制集合需要继续归入对应Program或Source Owner。本步Runtime工程按规定参数编译成功、0错误；27个警告只来自既有Unity／第三方依赖与未使用字段，构建后立即关闭build server。Unity刷新恢复连接后编译成功；回放与退出Play只出现既有FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-015216-834-ccd49fe20da6479b91b0913c7c96627e`，候选B为`Diagnostics/FootPlacementRuns/20260902-020333-815-08bc7f3b04b44d2daa27b8a4919ce442`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-020430-968-69ab7492738548ec83425931c993bc4d.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。正式评分版本`foot-quality-seven-dimensions/3`、七维分项、总分84.2和weighted evidence 96.5保持。由此确认Source失败清理归位没有改变成功帧的Source、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或诊断业务；`020333`成为下一Source外层清理小步的正式A。

## 根Frame lineage补齐PoseGraph identity

状态：`CharacterPoseFrameLineage`现在除Actor、Frame、Completion、Presentation Frame、Body Tick、Program、PlanHash、Projection、Rig与Tuning Generation外，显式携带`PoseGraphId`和`PoseGraphRevision`。唯一根Frame在`CharacterAnimationPresentationRuntime.BeginFrameTransaction`开始时从同一Projection写入这两个值；`WithCompletion`、有效性、等价和hash均保留完整身份，因此Source、Program、Constraint、Final Publication及具体Foot IK View不再需要从Runtime Target或通用Snapshot补查PoseGraph identity。

本步不勾选Foot任务2.1：具体`CharacterFootIkCaptureInterest`尚未接入Frame开始冻结。Runtime工程按规定参数构建成功、0错误，27个警告只来自既有Unity／第三方依赖与未使用字段，构建后立即关闭build server。3C Unity force refresh同时验收通用框架`d633e83c1` codec小步和本次lineage改动，未出现Generated Sampling、codec或Character编译错误；回放退出只出现既有FinalIK Domain Reload日志，已清空。

正式A为`Diagnostics/FootPlacementRuns/20260902-020333-815-08bc7f3b04b44d2daa27b8a4919ce442`，候选B为`Diagnostics/FootPlacementRuns/20260902-024336-767-aae40504baa6455e88eeed312795f33f`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-024433-867-aeefd9a688ca4f64bac1a6de2c56cf58.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。`character-foot-quality-score/4`七维分项、总分84.2和weighted evidence 96.5保持。由此确认根lineage身份闭合没有改变任何运行或诊断业务；`024336`成为specific Foot interest接入前的正式A。

## Foot IK具体View脱离万能Snapshot Publisher

状态：新增独立`CharacterFootIkCommittedCaptureViewProjector`，唯一拥有两份预分配`CharacterFootIkCommittedCaptureViewPage`、独立Lease、Pending／Active切换和失效。Projector只接收同lineage的Actor、Constraint与Final Publication Committed diagnostics及根Execution Result，在成功Seal后的`BeginCommittedDiagnostics`内组合Foot Feature、Foot Step Observation、Landing／Motion、Goal、FBBIK与Physical事实；Observation直接读取Final Publication Frame的正式Contribution，不再依赖Snapshot页先复制Final Contribution。

`AnimationPresentationRuntimeSnapshotPublisher.Page`已删除Foot页分配、Lineage写入、Foot／Solver／Physical复制、Foot Step Observation解析和Clear；`CreateSnapshot`只接收独立Projector发布的同一短租约，供尚未迁走的Overlay／Gizmo／Visual Validation读取，不产生第二事实页。若任一Publisher失败，Pending页会丢弃或独立Lease整体失效；Reset、显式Invalidate与Dispose同时失效两边。任务13.6仍不勾选，因为万能Snapshot对非Foot的Operation／Pose／Linked等旧读取还在，旧Foot Sampler的事件等待与外层Snapshot join也尚未删除。

Runtime工程使用显式保留build退出码的规定命令构建成功、0错误；1个warning为既有未使用字段，构建后立即关闭build server。3C Unity编译无Projector错误。正式A为`Diagnostics/FootPlacementRuns/20260902-024336-767-aae40504baa6455e88eeed312795f33f`，候选B为`Diagnostics/FootPlacementRuns/20260902-030456-895-95d26ff9b1cb422693202d73c63036ec`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-030553-686-4f82add68b76439baafa50be2de960f3.json`，工具对A Proof报告`matched:1044`，无failure且Foot Finalizing结束。

A/B均为2086脚行、1222列，其中1198业务列逐值相同、24 identity列一一映射、0冲突；Geometry均为67186行、27列，其中22业务列逐值相同、5 identity列一一映射、0冲突。七维分项、总分84.2与weighted evidence 96.5保持。由此确认Foot View Owner拆分没有改变Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或诊断业务；`030456`成为specific Foot interest接入的正式A。

## 建立specific Foot IK interest登记与根帧冻结合同

状态：新增只包含固定View容量的`CharacterFootIkCaptureInterest`与同步`ICharacterFootIkCommittedCaptureConsumer`合同。`AnimationPresentationRuntimeTarget`通过唯一owner成对登记interest和consumer；`CharacterAnimationPresentationRuntime`拒绝第二owner，并在每个`Present`开始把当前绑定复制为`CharacterFootIkCaptureBinding`，随唯一`CharacterPoseFrameTransaction.Begin`冻结，Reset后保持会话登记、Dispose时释放consumer引用。该合同不引用Capability、Sampler、Schema、Generated Program、packet、Session、Writer或Host类型。

本步不勾选任务13.2：Constraint、Final Publication与独立Foot Projector尚未改为消费该specific interest，当前未登记时只冻结default值，也不触发consumer。Runtime工程用显式保留build退出码的规定命令构建成功、0错误；1个warning为既有未使用字段，构建后立即关闭build server。首次Unity增量导入出现一次SourceAssetDB修改时间不一致，文件稳定后force refresh消失，清空Console后为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-030456-895-95d26ff9b1cb422693202d73c63036ec`，候选B为`Diagnostics/FootPlacementRuns/20260902-031403-657-15e34fd7f366452493b4ac159f321244`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-031459-554-babcde41dc0048fe88fbc7ec03c4ef44.json`，工具报告`matched:1044`，无failure且Foot Finalizing结束。A/B主表1198业务列和Geometry 22业务列逐值相同，identity列一一映射，0冲突；总分84.2与weighted evidence 96.5保持。`031403`成为specific interest向各Owner传播的正式A。

## specific Foot interest只开启Foot所需Committed页

状态：根Frame冻结的`CharacterFootIkCaptureBinding`现在随`BeginPendingFrame`进入Constraint Owner。Constraint Bank分别保存general diagnostics与specific Foot interest；Foot Placement页和FBBIK诊断只在general需要或specific Foot开启时冻结，Foot-only不打开Pose Watch Goal页。Actor Committed Projector在Foot-only时只冻结相关Clip Foot Step Observation，不复制Stack、StateMachine、Inertialization、Blend Space或Warp页面。Post-Seal入口在Foot-only时只取得Actor、Constraint与Final Publication Committed View并发布具体Foot租约，不捕获Source、Program、Linked或万能Runtime Snapshot。

`CharacterAnimationPresentationRuntime`只在general或specific任一存在时进入Post-Seal diagnostics；旧Trace与Debug View仍只响应general interest，specific Foot租约只同步交给帧开始冻结的唯一consumer。consumer返回false由其自身Capability Session处理；若consumer违反`TryCapture`合同抛错，Runtime只调用`CaptureFault`并隔离其二次错误，不改变已Seal表现帧或下一帧事实。由此完成任务13.2；PoseGraph只看到容量1的typed interest和领域consumer，不读取Capability、Sampler、Schema、Generated Program或packet容量。

Runtime工程用显式保留build退出码的规定命令构建成功、0错误，27个warning来自既有Unity／第三方依赖与未使用字段，构建后立即关闭build server。Unity编译无错误，退出仅有既有FinalIK日志并清空。正式A为`Diagnostics/FootPlacementRuns/20260902-031403-657-15e34fd7f366452493b4ac159f321244`，候选B为`Diagnostics/FootPlacementRuns/20260902-032312-398-b58a9d6e91894276b0610ca19a27affa`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-032408-358-0e43d95f5b2e425fb360e7ea276a84c8.json`，工具报告`matched:1044`。A/B主表1198业务列、Geometry 22业务列逐值相同，identity列一一映射、0冲突；总分84.2、weighted evidence 96.5不变。`032312`成为Final Publication世界事实扩展前的正式A。

## Final Publication按specific interest封存Root与Physical世界事实

状态：唯一`AnimationFinalPosePhysicalWriter`现在接收正式`CharacterRootHierarchyBinding`，构造时确认其Animator根就是`PoseRoot`。根Frame冻结的`CharacterFootIkCaptureInterest`随Final Publication Pending页保存；只有specific Foot interest开启时，Physical Writer才在完成最终骨骼写入的同一位置一次读取LogicRoot、VisualRoot、PoseRoot以及左右脚踝的最终世界／局部空间事实，写入`CharacterFootIkPhysicalCapture`。未登记Foot Capability的正式运行只保留既有Physical诊断，不构造这组Foot payload。

`CharacterFootIkCommittedCaptureViewLease`只从Final Publication Committed页公开这些值；具体Foot Projector与Foot插件目录全文搜索均不包含`Transform`、`RootHierarchy`、`PhysicalBones`或Animator骨骼查询，因此后续Bridge和Generated Program不再反向读取场景。Runtime与Editor工程按规定参数构建成功、0错误；Editor的30个warning只来自既有第三方包，构建后立即关闭build server。Unity只刷新改动脚本，回放与退出Play仅出现既有FinalIK序列化深度日志，已清空。

正式A为`Diagnostics/FootPlacementRuns/20260902-032312-398-b58a9d6e91894276b0610ca19a27affa`，候选B为`Diagnostics/FootPlacementRuns/20260902-033516-613-1591907a78214a389ab18cdc9835fe66`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-033613-172-f30d20daa7554b24b89ed641ecc21a3d.json`，工具报告`matched:1044`。A/B均为2086脚行、1222列，其中1198业务列逐值相同、24个运行身份列一一映射且0冲突；Geometry均为67186行、27列，其中22业务列逐值相同、5个身份列一一映射且0冲突；总分84.2与weighted evidence 96.5不变。由此完成Foot采样重构任务2.2与2.4；`033516`成为Foot Generated Field继续迁移的正式A。

## Source Demand／Prepared Resource／Usage／Retirement外层收口

状态：提交`9260c9d81`、`80645b9d3`、`b036c171d`、`d8732cfb7`、`0916a15d2`、`3758a2bf2`、`d7b0a4cfa`、`de938f796`、`44c25704a`、`0971cd5c8`与`6b08ffeb9`连续完成Source外层收口。`CharacterPoseSourceModule`现在唯一拥有固定容量Binding、prepared resource、Usage、Retirement、release completion与acknowledgement页；Program-owned `CharacterPoseSourcePreparationPage`随`CharacterPoseSourceDemand`发布同Completion短租约，每条命令仍按原顺序追加后立即由Source消费，Source Frame Result发布前验证连续index和零遗漏。

Action、Motion Matching provider、Direct、Clip与Blend Space统一为一个`CharacterPoseSourcePreparation`合同。Source Module内部唯一识别具体Source Adapter，并持有从已校验Projection一次构造的不可变`CharacterPoseSourceCatalog`；旧Runtime的三组Binding数组、physical identity／SourceIndex scratch、Clip Catalog scratch、四套每帧Catalog Builder、MM专属Usage数组／count、release completion List、ack位图和Module嵌套Release token类型均已删除。Program继续唯一决定Player relevance、State、Transition、Slot、Blend、source-local时间、Capture和Retirement Permission；Source不读取这些逻辑Owner状态。

3C Unity通过正常Assets Refresh逐步编译上述Source、Program、Motion Matching与合同文件，当前没有这些文件的编译错误。全局编译停在并行Foot Sampling迁移已删除旧`DiagnosticField` ABI、但旧逐字段声明尚未切换的明确边界；因此本阶段不伪造全工程绿灯，也不在PoseGraph增加兼容Attribute。任务4.1、4.2和4.5按实际所有权闭合完成；任务4.6仍保持未完成，因为旧Runtime还持有Program逻辑release pool／List、Player／Stack集合及其控制journal，后续必须迁入Program Actor State／Frame Pages后删除。为避免在采样ABI切换期间产生无效中间证据，本组合只做一次后续统一Replay，不把当前编译阻塞误报为运行行为差异。

## Program-owned Source Retirement与节点运行索引

状态：提交`8a1410879`先把Direct、Clip与Blend Space的Standalone退休数组、owner分发、Source→Player应用顺序和release诊断记录整体迁入`CharacterPoseProgramSourceRetirementState`。提交`6c8b21e00`继续把Pending Pose与Action backend两套pool／List、request与completion identity、source scratch、exact-source集合、resource identity、Retirement Permission准备、deferred apply、discard边界和跨协议route notification全部迁入同一Program Actor State。Source Module仍唯一执行physical/backend退休；Program State只拥有逻辑Player／Stack token、usage许可和等待外部Action backend确认的协议状态。

提交`72779800a`新增唯一`CharacterPoseProgramNodeRuntimeIndex`，收拢Stack、Route、Player index、Source owner与Direct Player查找，并内聚Motion Matching selection投递和Player source usage判断。旧`PosePlanExecutionRuntime`已删除5个Dictionary字段、三类release对象、三套pool／List、两套scratch、HashSet、resource-id数组与request／completion计数；全文只剩构造期把Direct Player临时收集后转为固定数组的局部List。Source Begin／Validate／Commit／Discard仍在唯一根帧生命周期各执行一次，未新增第二Seal或Discard Owner，因此任务4.6完成。3C Unity正常Assets Refresh没有上述Program State、Node Runtime Index或旧Runtime编译错误；全局失败仍只来自并行Foot Sampling旧字段声明尚未消费0.3 ABI，不影响本项所有权结论。

## Source物理采样旁路审计

状态：全仓Runtime搜索确认`PhysicalPoseSourceRegistry`与`AnimancerPoseSamplingBackend`均只有`CharacterPoseSourceModule`一处构造，`InsertOutputPlayable`也只有该Module安装source fan-in的一处调用；Character Runtime不存在直接`.Play(...)`，不存在以Legacy／Default／Fallback命名或语义补洞的source路径。唯一`AnimationScriptPlayable.Create`位于该Backend内部，source物理注册、Playable连接、capture binding安装、断连与销毁仍由同一Module闭合。

Clip、Blend Space、Motion Matching、Action与Blend Stack保留的多个`PrepareCapture`只按各自Program节点语义准备同帧目标缓冲和typed binding；它们不持有Animancer Graph、Physical Registry或capture Playable，也不能自行连接、播放或发布物理source。Source Module消费这些命令后才由唯一Backend创建／更新capture Playable。因此这些入口是Program逻辑Owner到Source物理Owner的正式输入，不是第二capture owner。审计没有发现需删除的旁路，任务4.8完成；本步没有代码或行为变化，不单独运行回放。

## Source-owned Candidate Tuning Snapshot

状态：新增`CharacterPoseSourceTuningState`、不可变`CharacterPoseSourceTuningSnapshot`与只读`CharacterPoseSourceTuningView`，由`CharacterPoseSourceModule`构造和唯一持有。初始Snapshot从已校验Projection的Tuning Layout与Default Block建立，逐项确认Clip Player的默认`play-rate`与编译Descriptor相同；每次在线候选先验证Program、Projection、Pose Plan、Rig与Layout identity，再建立下一连续`TuningGeneration`的完整候选。Program／Constraint现有调参入口成功后Source才一次提升Committed Snapshot，失败时直接丢弃候选，不对Source执行反向Apply。

`AnimationClipPlayerRuntime`已删除可变`m_PlayRate`、`PlayRate`读取和`ApplyTuning`写入口。Program Actor State仍唯一保存连续时间、cycle、movement clock、continuity和relevance；每帧从同generation的Source View读取Clip倍率，继续按原公式推进普通时钟与Committed Movement时钟，并把同一倍率作为capture的`VisualTimeScale`。Source Module在`BeginFrame`验证根lineage的Tuning Generation，Program Image与旧Native Execution存储均未被修改。

当前发布Tuning schema中，Source sample-local可调字段只有Clip的`play-rate`；Blend Space、Motion Matching与Action没有独立sample-local调参字段。本步不为零字段Adapter制造占位配置、默认值或备用路径；以后新增这三类字段时必须扩展同一Source Snapshot。Program／Constraint仍保留旧可变Apply与失败回滚，留待任务8整体迁移，不能把本步误报为全局Tuning原子化完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误，构建后已关闭MSBuild与编译器服务器。3C Unity普通Assets Refresh后新增文件进入正式工程，改动文件没有编译错误；全局Unity编译仍停在并行Foot Sampling尚未从旧`DiagnosticField` ABI迁到0.3 Generated Projection的已知边界。本步不增加兼容Attribute，也不在该外部阻塞期间伪造Replay证据。任务4.7完成，Source阶段4.1至4.8全部闭合。

## Program控制双页归还Frame Pages

状态：新增`CharacterPoseProgramFramePages`，由它一次分配并唯一销毁StateMachine、Animation Slot、Root Orientation Warp、Linked Pose Call和Linked Pose Active Fragment的Committed／Pending两套Native控制页。旧`CharacterPoseGraphNativeProgram`已删除内部`Page`、五组当前页字段、Committed／Pending页引用、Bind／Capture／Allocate／Dispose页逻辑与自己的`m_FrameOpen`，只在现有外层API内把控制读写转交Frame Pages。

Begin仍只切换Pending页并清空本帧Linked Pose选择，Commit仍交换双页，Discard仍恢复Committed页；StateMachine／Slot未初始化存储、Root Warp清零、Linked Call inactive和Fragment清零的构造选项与原实现相同。静态Operation／Stage／Rig常量、运行Tuning Weight、Value Workspace、Operation Completion和Committed Diagnostics尚未迁移，因此任务5.5与5.8不提前勾选，也没有把旧Native Program改名成新Program Image冒充完成。

Unity普通Assets Refresh把新增文件写入正式Runtime工程；补齐现有Presentation control类型的唯一命名空间引用后，`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误，构建服务器已关闭。本步不改变公式、Operation顺序、Source、Constraint、Final Publication或IK数据，不单独运行回放。

## Program Committed Diagnostics页归还Frame Pages

状态：`CharacterPoseProgramFramePages`继续接管预分配Committed Program Diagnostics页、单调发布identity、下一帧Begin失效和Dispose失效。`CharacterPoseGraphNativeProgram`已删除嵌套`CommittedDiagnosticsPage`、页字段与diagnostics identity计数，只保留按现有字段语义从已完成Frame复制数据的投影过程；`CharacterPoseProgramCommittedDiagnosticsView`直接租用Frame Pages持有的同一页，不建立第二Snapshot或复制Owner。

诊断interest分组、Operation Completion、StateMachine骨骼权重、Slot Contribution、Value header／Contribution／dense weight／Pose的冻结条件与遍历顺序未改。任务5.5仍不勾选，因为Program Value、Operation Completion的运行写页和Source Demand输出尚未归入Frame Pages；本步只闭合Committed diagnostics物理页与租约寿命。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误，构建服务器已关闭；不单独运行回放。

## Program Value与Operation Completion Workspace归还Frame Pages

状态：`CharacterPoseProgramFramePages`现在唯一持有并销毁原`AnimationPoseNativeWorkspace`实现，向Program执行链提供Current Value、Player写页、Operation／Stage Completion、Pose Graph失败、Final Read／Write outcome和Committed Final只读绑定。`PosePlanExecutionRuntime`已删除`m_Workspace`字段与独立Dispose，只保存正式`m_ProgramFrames`引用；构造时创建Workspace、取得初始layout并交给旧Native Program后立即清空局部引用，正常帧不再存在Frame Pages之外的Workspace Owner。

旧`CharacterPoseGraphNativeProgram`构造时验证传入Workspace与自身layout完全一致，并把同一Workspace交给Frame Pages；其Dispose只通过Frame Pages完成一次释放。Begin／RequireStagesCompleted／Commit／Discard、Player／Value／Final binding与容量统计仍调用原Workspace实现，数据布局、清零顺序、completion identity和双页交换没有改变。本步没有建立第二Value页或兼容入口。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误，构建服务器已关闭。任务5.5暂不勾选，只剩Program-owned Source Demand／Preparation输出仍由外层字段持有；该页迁入后再按完整Owner闭包判断。

## Program Source Demand输出归还Frame Pages

状态：`CharacterPoseProgramFramePages`新增固定容量`CharacterPoseSourcePreparationPage`和当前typed `CharacterPoseSourceDemand`，统一负责Begin、Bind、逐条Preparation追加、短租约校验与Clear。`PosePlanExecutionRuntime`已删除`m_SourcePreparationPage`字段和构造分配；创建Demand时先由Frame Pages开启唯一Preparation页并保存完整Demand，再把同一只读命令交给Source Module。Prepare Evaluation同时验证调用方Demand、Program-owned Demand与Source收到的Demand具有相同lineage、计数、provider引用和Preparation页identity。

Discard、Reset与Dispose继续在原时机清空Demand，但现在只调用Frame Pages Owner；成功帧仍保留短租约到下一次Demand Begin，生命周期与原实现一致。Source Module的Demand副本只是本Module接收的typed输入，不取得Program Preparation页写权限，也不成为第二Program输出Owner。

至此`CharacterPoseProgramFramePages`已实际持有Pending node control、Source Demand／Preparation输出、Current Value、Operation／Stage Completion、Program失败／Final outcome和Committed diagnostics，任务5.5完成。旧Native Program仍持有静态执行存储及运行Tuning Weight，所以任务5.8不提前完成。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误，构建服务器已关闭；本步不单独回放。

## Frame Pages脱离旧Native Program所有权

状态：`PosePlanExecutionRuntime`在构造期建立唯一`CharacterPoseProgramFramePages`，把新建Value Workspace的所有权一次交给它，再将同一Frame Pages只读引用用于旧Native Program静态layout核对和Staged Executor帧绑定。旧`CharacterPoseGraphNativeProgram`已删除Frame Pages字段、Frame Pages构造、Frame Pages Dispose、Begin／Commit／Discard与StateMachine／Slot／Root Warp控制写入口；根当前Program Owner直接驱动Frame Pages，Dispose也只执行一次。

`CharacterPoseGraphStagedExecutor`现在显式接收Frame Pages并从该Owner取得五组当前控制页，静态Program只继续提供Operation、Stage、Rig、Blend catalog及其它执行常量。StateMachine transition发布、Slot与Root Warp控制、Linked Pose选择都写入同一Frame Pages；Linked Pose静态candidate解析仍由旧Native Program提供，但必须显式传入当前已打开Frame Pages，不再依赖其内部可变引用。Committed Diagnostics投影同样显式接收同一页Owner。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误，构建服务器已关闭。旧Native Program已不持有Frame identity、Pending／Committed控制页或diagnostics页；任务5.8仍不勾选，因为`SetOperationWeight`仍直接修改Native Operation，必须在Program-owned Candidate Tuning Snapshot建立时删除。

## Native Operation Weight迁入Program Tuning Snapshot

状态：新增`CharacterPoseProgramTuningState`、不可变`CharacterPoseProgramTuningSnapshot`与只读`CharacterPoseProgramTuningView`。初始Snapshot逐项保存全部native operation编译权重；Blend Pose、Layered Bone Blend与Additive Pose的可调字段从同identity Tuning Layout／Default Block建立精确映射，其它Operation保持编译默认。候选使用独立Persistent Native权重页，完整验证后才与Source Candidate按同一连续`TuningGeneration`提交；失败直接Dispose候选，不修改Committed页。

`CharacterPoseGraphNativeProgram.SetOperationWeight`与运行时Native Operation回写已删除，Native Operation数组只在构造编译时写一次。`CharacterPoseGraphStagedExecutor`显式接收同generation Program Tuning View，在唯一Stage执行循环读取对应native index权重并构造本次只读Operation；静态Program Image候选、旧Native执行存储和其它Actor不会被在线调参污染。Sequence Preview只执行不可调Clip Player Operation，保持原静态入口。

全文搜索确认旧Native Program不再保存Frame identity、Pending／Committed控制／诊断页、Goal workspace、Tuning Generation、Candidate或运行调参值；Native Operation赋值只剩构造期`CompileOperations`。因此任务5.8完成。任务6.9仍不勾选：PoseState、Blend Stack与Inertialization调参尚未迁入同一Program Candidate Snapshot。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误，构建服务器已关闭；全局Unity仍由并行Foot 0.3迁移编译边界阻塞，本步不伪造Replay。

## 建立Program Actor State所有权边界

状态：新增`CharacterPoseActorState`，先整体接管PoseState／Player continuity集合、Blend Stack、Transition Routing、Root Orientation Warp、Inertialization、Program节点运行索引与逻辑Source Retirement状态。`PosePlanExecutionRuntime`删除这些对象各自的存储字段，只保留唯一`m_ActorState`；现有执行代码经内部只读访问器取得同一对象，没有复制状态、第二套页或兼容路径。

Actor State统一负责Stack、Direct Player、Clip Player、Blend Space Player与Inertialization的销毁，顺序与异常聚合规则保持不变。Source物理资源仍由`CharacterPoseSourceModule`拥有，Constraint Bank、Final Publication、Diagnostics、Program Frame Pages、Program Tuning与静态Program存储均未进入Actor State，因此没有复制其它Module真相。

任务5.4暂不勾选：`ActionPlaybackInput` lifecycle／command cursor尚未从外层执行器迁入Actor State，Slot控制的跨帧状态也仍需继续对账。Unity MCP已显式路由到`3C_Client@e852139597e42532`完成脚本编译；本次两个文件的错误筛选均为0。全局错误只来自并行Foot生成采样迁移缺失`KK.GeneratedDiagnosticSampling` ABI，本步不添加兼容类型，也不把外部编译阻塞误报为PoseGraph回归；不单独运行回放。

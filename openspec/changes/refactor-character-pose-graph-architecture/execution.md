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

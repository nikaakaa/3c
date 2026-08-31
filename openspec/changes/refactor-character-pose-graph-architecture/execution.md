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

状态：实现候选待正式Record回放；Runtime按规定参数编译成功，只有既有Input Value警告，0错误，build server已关闭。

- `PosePlanFrameLease`与`PosePlanPreparedEvaluation`直接改为`CharacterPoseProgramFrameLease`和`CharacterPoseProgramPrepared`，没有保留旧别名。
- Program Prepare只接收根事务的open lineage，在生成现有Completion后返回补齐Completion的同一lineage；根事务只接受其它身份完全一致的completed lineage，外层不再单独传Actor和Render Frame给Barrier。
- `ExecuteEvaluateBarrier`返回`CharacterPoseProgramResult`，集中发布lineage、Frame Outcome、Output Availability、Output Invalid Reason、Graph Invalid Reason和Invalid Operation。外层只消费该typed Result判断是否可提交和生成错误信息，不再读取`AnimationFinalPoseNativeReadBinding`内部Slice解释结果。
- 本步没有改变Native Workspace、Operation调度、Constraint、Writer或Seal顺序。Source Frame、Constraint Result、Final Publication Result和per-operation completion仍待各自Owner迁移，因此任务2.2保持未完成。

## MODIFIED Requirements

### Requirement: Foot Placement必须是唯一Goal事务

唯一`CharacterPoseConstraintRuntime` MUST继续为每个Actor和表现帧建立匹配Frame、Completion、Program、Projection与Rig lineage的Pending Constraint Bank，并唯一拥有Foot Context、Resolved Foot Pair、Primary Support/Pelvis、Goal Contribution、唯一Goal Assembler、唯一Goal Set、FBBIK BendHistory与Solver Result。它 MUST不再拥有Final Pose物理页、Physical Writer或Physical Result；这些真相只属于`CharacterFinalPosePublication`。

`CharacterPoseProgramRuntime` MUST在Program Image中Foot Placement、PoseBone Contribution、Goal Assembler与FBBIK各自Operation的位置，通过对应typed编译Handle恰好调用一次Constraint Runtime入口并写入唯一per-operation completion。Constraint Module MUST不扫描Program、不维护第二份Stage Schedule、不接收NativeSlice、Goal offset/count、Operation index或call-site index，也 MUST不重新执行已经完成的Operation。Constraint `Complete`只能验证完整闭包并发布一个`CharacterPoseConstraintResult`。

Foot Placement MUST继续形成一个深`CharacterFootPlacementModule`，其Implementation从Constraint Bank取得Committed/Pending Context页，外部只接收同帧不可变Frame Input并发布一个`CharacterFootPlacementResult`。调用方 MUST不知道或编排Landing Prediction、Ground Path、左右脚状态、Support、Pelvis与Goal编码顺序。Module Implementation MUST先通过World Query Adapter生成不可变Observation Page，再为左右脚各执行一次`CharacterFootStateMachine`并生成唯一Resolved Foot Pair，最后计算Primary Support、Pelvis与三个typed Goal Contribution；不得发布第二Goal Set、第二Pelvis、第二FBBIK、第二Final Pose页或第二Physical Writer。

#### Scenario: 正常生成并消费Foot Placement结果

- **WHEN** 同一表现帧具有合法Component Pose、Step、Body、World Query、Profile、Program Operation与Pending Constraint Bank
- **THEN** Foot Placement Operation MUST生成同lineage的Resolved Foot Pair、Pelvis Result、三个Goal Contribution和唯一operation completion
- **AND** 后续Assembler与FBBIK MUST在各自Program Operation位置消费同一Bank结果，调用方不得取得或逐个提交Foot Context、Ground Path、Pelvis、Goal workspace或Solver状态

#### Scenario: 重复执行Foot Placement

- **WHEN** 同一Frame与Completion第二次请求执行同一个Foot Placement Operation
- **THEN** Program Runtime MUST使该Operation completion与整帧Invalid并阻止Final Publication
- **AND** Constraint Module MUST不覆盖第一次结果或建立第二Foot Placement事务

#### Scenario: Constraint完成后发布Final Pose

- **WHEN** Foot、Goal Assembler与FBBIK已经形成合法Constraint Result
- **THEN** Program Output MUST只通过typed write handle写入Final Publication唯一Pending Pose物理页
- **AND** Constraint Bank MUST不保存Physical Writer、Physical Result或第二Final Pose副本

### Requirement: Foot Placement诊断必须只显示正式结果

Runtime Result MUST与Diagnostics严格分型。Constraint Module MAY按Frame开始冻结的interest，从Pending Context、Observation、Resolved Result和Constraint阶段Result单向深冻结Phase Progress、Baseline、Envelope、Swing Correction、Residual、Anchor、Contact Progress、Ownership、Support Eligibility、Support、Pelvis、Goal与Solved结果；这些事实只能进入`CharacterPoseConstraintCommittedResult`。Physical Write与最终Physical Bone结果 MUST只由Final Publication冻结进`CharacterFinalPosePublicationCommittedResult`。

Gizmo、CSV、Trace与Pose Watch MUST只由Diagnostics Projector按相同Frame、Completion、Program、Projection、Rig和Actor lineage组合Source、Program、Constraint与Final Publication的Committed Result。Diagnostics MUST不查询世界、修改Context、选择Support、生成Goal、执行FBBIK、读取Physical Transform反推结果或把Constraint与Physical事实写回同一业务Bank。

#### Scenario: 捕获重构后基线事实

- **WHEN** Foot、Pelvis、Goal、FBBIK、Pending Pose与Physical Writer均成功提交
- **THEN** Diagnostics Projector MUST从同一lineage的Constraint与Final Publication Committed Result发布可对账行为Oracle的正式事实
- **AND** Diagnostics页归属变化 MUST不改变Runtime Result、Final Pose或Physical Writer输入

#### Scenario: Writer失败

- **WHEN** Constraint Result已经完成但Final Publication在Physical Writer前或Writer中失败
- **THEN** Diagnostics MUST不发布本帧Pending Constraint或Physical结果
- **AND** Projector MUST只保留上一Committed Snapshot或正式Actor Fault事实

# PoseGraph运行基础原完成记录

用户要求将完成文档分离归档。这里保留分离前60条已勾选记录的编号和原文，其中7条验证或边界记录不再列入tasks.md；tasks.md只保留53项实现或文档任务。此处不重新认定当前代码版本，也不表示其它PoseGraph任务完成。

原始证据见[原change执行记录](../../refactor-character-pose-graph-architecture/execution.md)。

| 原编号 | 原状态 | 原记录 |
|---|---|---|
| 1.1 | 原已完成 | 对照用户指定提交`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`与behavior-baseline.md核对当前动画／IK源码、Profile／Rig／作者数据、generated artifact及已有正式输入／诊断证据；后续相关差异单独报告，不等待Foot／IK全部归档 |
| 1.2 | 原已完成 | 记录当前Foot Motion实际输入、输出、lineage、Curve消费与未完成行为，不按旧spec补实现剩余Foot能力 |
| 1.3 | 原已完成 | 对账当前保留Foot、Support、Pelvis、Goal、FBBIK与Physical结果，列明已知问题、未覆盖输入、已撤除Reach硬夹紧和已撤销SmoothKnee，不把它们改成重构修复目标 |
| 1.4 | 原已完成 | 从current外部合同与当前已存在实现固定Clip、Blend Space、Linked Pose、Motion Matching、Transition Routing、Blend Stack和Inertialization迁移目录，不接入未配置内容 |
| 1.5 | 原已完成 | 盘点`PosePlanExecutionRuntime`、根`AnimationPresentationFrameTransaction`、Native Program、Staged Executor、Workspace、Action lifecycle、Source backend、Constraint、Writer、在线调参、Diagnostics和Compiler全部状态、页、索引、生命周期与调用顺序 |
| 1.6 | 原已完成 | 为每项现有字段标注唯一目标Owner、寿命类别、写入阶段、读取者和删除位置，分别识别静态Program、actor-local Execution View、Actor State、Program Frame Page、Module Pending页与根事务，拒绝无法归属的共享可变字段 |
| 1.7 | 原已完成 | 为全部现行Operation Code建立`新Family / 跨帧状态Owner / Frame页Owner / Execution Domain / Workspace需求 / 删除字段`迁移表，覆盖Parameter、ActionPlaybackInput lifecycle、Motion Matching、Pose History与Tuning读取 |
| 1.8 | 原已完成 | 固定本change不修改PoseState选择、source时间、Action lifecycle、生效中的Tuning值、Blend权重、Transition、Slot、Inertialization、Foot、Goal、FBBIK和Physical Pose结果 |
| 1.9 | 原已完成 | 记录第一阶段IK维护重构通过提交和证据作为串行接入点，保留其Foot请求／最终结果、Interpolation历史、独立Reset修正与诊断列绑定，不恢复旧结构；总基线仍为ad3527e，实际冲突单独报告 |
| 1.10 | 原已完成 | 按behavior-baseline.md逐项核对动画时钟、Transition／Blend／Slot顺序、Foot source选择、IK计算与持久状态、Root Bone写入政策；被下一帧消费的内部Fact不得当诊断冗余删除 |
| 2.1 | 原已完成 | 建立统一`CharacterPoseFrameLineage`，固定Actor、Frame、Completion、Program、Projection、Rig和Tuning Generation identity并删除各Module自行生成的重复完成身份 |
| 2.2 | 原已完成 | 建立Source Demand、Source Frame、Program Prepared、per-operation Completion、Program Result、Constraint Result和Final Publication Result的typed合同及合法Availability/Outcome |
| 2.3 | 原已完成 | 建立由`CharacterAnimationPresentationRuntime`唯一拥有的根`CharacterPoseFrameTransaction`，只保存lineage、阶段、Module lease/result与统一Outcome，不保存任一Module内部Workspace |
| 2.4 | 原已完成 | 为Program、Source、Constraint与Final Publication分别建立Owned Pending页和typed lease，明确唯一写入Owner、只读下游view与根Seal/Discard权限 |
| 2.5 | 原已完成 | 让现有单一运行路径先携带统一lineage、root lease和typed Result，不提前创建空壳Module、wrapper、第二Frame事务或第二执行路径 |
| 2.6 | 原已完成 | 对齐现有Animancer Evaluate Barrier，固定Barrier前验证、Barrier内执行、Writer后no-throw Seal和Fault语义 |
| 2.7 | 原已完成 | 按Build、Runtime创建、根Frame／跨Owner交接和Writer分配检查责任，删除迁移新增的重复静态扫描与多层完整校验，保留必要动态检查和原Fault政策 |
| 4.1 | 原已完成 | 新增深`CharacterPoseSourceModule`及固定容量Source Demand、Source Binding、Prepared Resource、Usage、Release和Completion页 |
| 4.2 | 原已完成 | 迁移Clip、Blend Space、Motion Matching和有限Action sample Adapter装配，保持各source-local时间、Action sample readiness与数学不变 |
| 4.3 | 原已完成 | 迁移Animancer source backend、Physical Pose Source Registry、capture binding和唯一Playable资源所有权 |
| 4.4 | 原已完成 | 迁移prepared source创建、deferred release、slot reuse、retirement permission与release completion闭包 |
| 4.5 | 原已完成 | 让Source Module只消费Program发布的Demand/Usage并只输出Source Frame Result，不读取PoseState、Action winner、Transition、Slot或Blend内部状态 |
| 4.6 | 原已完成 | 从旧Pose runtime删除source数组、physical identity scratch、release pool、Dictionary/List控制逻辑和重复Seal/Discard顺序 |
| 4.7 | 原已完成 | 将Clip、Blend Space、Motion Matching与Action sample-local调参改为Source-owned Candidate Tuning Snapshot，不修改Program Image或actor-local Execution View |
| 4.8 | 原已完成 | 搜索并消除第二Animancer direct Play、第二Physical Source Registry、第二capture owner和图外source fallback |
| 5.1 | 原已完成 | 将`CharacterPoseProgramImage`作为`CharacterPresentationProjection`内部唯一语义Pose程序，保存Program identity、ProjectionRevision、PoseProgramImageHash、Rig、Stage、Operation Header、Family Payload、Value layout、Workspace layout、Source Map和容量；Gameplay ContractHash只由外层Presentation Contract与Projection拥有 |
| 5.2 | 原已完成 | 建立可选`CharacterPoseProgramExecutionView`，每个Program Runtime最多一份，只逐值materialize同Image并验证相同identity/hash，不得编译、重排、补字段或拥有Actor/Frame状态 |
| 5.3 | 原已完成 | 让`CharacterPoseProgramRuntime`唯一Dispose自己的Execution View，删除第二View、旧Native Program语义容器和旧Runtime Compile路径 |
| 5.4 | 原已完成 | 新增`CharacterPoseActorState`，迁移PoseState、Player continuity、ActionPlaybackInput lifecycle/command cursor、Slot、Blend Stack、Routing、Inertialization和其它跨帧节点状态 |
| 5.5 | 原已完成 | 新增`CharacterPoseProgramFramePages`，保存Pending node control、Source Demand输出、当前帧Value、Operation completion和Program diagnostics |
| 5.6 | 原已完成 | 让根`CharacterPoseFrameTransaction`只持有Program/Source/Constraint/Publication typed lease/result，不取得或索引各Module内部页 |
| 5.7 | 原已完成 | 将Dense跨帧状态改为明确Committed/Pending页，将稀疏节点与source生命周期变化保持为固定pending state或journal |
| 5.8 | 原已完成 | 删除`CharacterPoseGraphNativeProgram`中的Frame identity、Pending/Committed控制、Goal workspace、运行时Tuning Weight和其它可变状态 |
| 5.9 | 原已完成 | 删除Actor State对Source物理资源、Constraint Bank、Final Pose和Diagnostics真相的复制 |
| 5.10 | 原已完成 | 对账Reset、Projection replacement、Preview seek、actor-local Execution View、Dispose和Actor Fault，确保静态、执行View、Actor、Module Frame与根事务寿命各自只由唯一Owner清理 |
| 6.1 | 原已完成 | 新增`CharacterPoseProgramRuntime`，唯一持有Program Image只读引用或自己的actor-local Execution View、Actor State、Program Frame Pages和持久Executor Implementation，并只接收根Frame Lease |
| 6.2 | 原已完成 | 将PoseStateMachine、Player、ActionPlaybackInput lifecycle、AnimationSlot、BlendStack、Transition消费、Inertialization和其它逻辑节点执行迁入Program Runtime |
| 6.3 | 原已完成 | 将每帧Executor构造改为持久绑定Program Image/Execution View和Program自有固定页，只切换根Frame Lease与Pending页索引 |
| 6.4 | 原已完成 | 按Stage Schedule执行每个Operation恰好一次并写入唯一Operation Completion页 |
| 6.5 | 原已完成 | 让Program Runtime通过typed Result调用Source Module，并通过typed编译Handle逐Operation调用Constraint Module |
| 6.6 | 原已完成 | 删除外层Runtime对World-aware Operation的扫描和内部输入装配，删除Staged Executor对同一Operation的第二解释或完成检查 |
| 6.7 | 原已完成 | 删除Constraint Module扫描Program、Source Module扫描Operation以及Diagnostics重放Operation的路径 |
| 6.8 | 原已完成 | 将旧`CharacterPoseGraphStagedExecutor`巨型字段和构造整体替换，删除旧类型而不保留wrapper |
| 6.9 | 原已完成 | 将Node Weight、PoseState、Slot、BlendStack、Routing与Inertialization调参改为Program-owned Candidate Tuning Snapshot |
| 6.10 | 原已完成 | 搜索并消除第二Action lifecycle Owner、第二Pose Operation执行Owner、第二Value writer和任何图外隐式Pose stage |
| 7.1 | 原已完成 | 新增具体`CharacterFinalPosePublication` Module并迁移唯一Committed/Pending Final Pose物理页、完整Rig binding和Publication Result |
| 7.2 | 原已完成 | 让Program Image的Output Family只保存稳定`CharacterFinalPosePublicationLayoutHandle`，不保存Actor页引用且不分配第二Final Output buffer |
| 7.3 | 原已完成 | 在Actor Runtime创建时由Final Publication把layout handle绑定到唯一Pending Final Pose页，Program Output Operation通过actor-local binding写入并发布只读`ProgramOutputPoseResult` |
| 7.4 | 原已完成 | 让Compiler只证明唯一Output与Publication requirement，让Runtime Factory和Final Publication构造证明唯一具体Writer与完整binding |
| 7.5 | 原已完成 | 在写任何Physical Bone前统一验证Pose availability、Rig、continuity、Program completion、Constraint completion和Frame lineage |
| 7.6 | 原已完成 | 让唯一Physical Writer一次应用完整Pending Pose，Invalid时保持Committed Pose并遵守现有Fault政策 |
| 7.7 | 原已完成 | 确保Writer成功后不再执行Foot、Goal、FBBIK、Diagnostics或其它可能因业务输入失败的计算 |
| 7.8 | 原已完成 | 从Program Runtime、Source Module、Constraint Module和外层Runtime删除Physical Transform写入与第二Final Pose页所有权 |
| 7.9 | 原已完成 | 不建立Writer Graph节点、Writer抽象接口或第二Implementation，搜索并删除旧final writer旁路 |
| 8.1 | 原已完成 | 建立`CharacterPoseTuningSnapshot`、单调`TuningGeneration`和Program/Source/Constraint分区Candidate合同 |
| 8.2 | 原已完成 | 让根Runtime在打开新Frame前收集三个Module Candidate并完成identity、容量、值域与resetOwnerState预验证 |
| 8.3 | 原已完成 | 让全部Candidate成功后一次提升同一TuningGeneration，任一失败时保持三个Committed Snapshot不变 |
| 8.4 | 原已完成 | 删除先修改运行对象、失败后反向Apply旧Block的回滚路径 |
| 8.5 | 原已完成 | 删除Program Image、actor-local Execution View、静态Projection和跨Actor对象上的可变Tuning字段 |
| 8.6 | 原已完成 | 对账Runtime与Preview的调参字段、生效时机、resetOwnerState与逐Actor隔离，保持现行作者行为 |

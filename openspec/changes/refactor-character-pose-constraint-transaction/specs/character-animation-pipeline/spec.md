## MODIFIED Requirements

### Requirement: CharacterSimulationPresentationRuntime必须执行唯一编译Pose Plan

SimulationCommitter与唯一`CharacterSimulationPresentationRuntime` MUST共同构成Unity animation application seam。Runtime MUST消费Committed Body/Intent、Program parameter和有限Action command，构造Presentation Fact，并按Projection编译的有序Pose Plan执行PoseStateMachine、state-local provider、AnimationSlot、Local/Component Pose转换、Component Pose控制、Foot Placement、Goal Contribution汇聚、唯一FullBodyIK、后续Pose stage与FinalPublication。

唯一`PosePlanExecutionRuntime` MUST构造并持有唯一`CharacterPoseConstraintRuntime`与根Bank；正式Runtime和Preview MUST通过同一Factory取得该所有权关系。Staged Executor只能按编译Pose阶段调用该实例，MUST不为Foot Placement、Goal Assembler、FBBIK或Diagnostics创建第二根事务、第二Committed identity或独立Seal顺序。根Runtime MUST只拥有阶段顺序、lineage、页选择和事务生命周期，不得实现Foot、Pelvis、Goal Assembly或Solver数学。

Foot Placement与PoseBone Goal来源 MUST只发布typed Goal Contribution。唯一Goal Assembler MUST在预分配页中验证Frame、Completion、Rig、Slot与重复贡献并发布一个Goal Set；唯一FullBodyIK MUST只消费该Goal Set与同一Component Pose。Foot Placement、Goal Assembler、FBBIK BendHistory和紧凑Outcome MUST写入同一CharacterPoseConstraint Pending Bank。任一stage失败 MUST阻断后续stage与FinalPublication；跨过Animancer Evaluate Barrier后的失败 MUST使同一Actor Runtime进入Faulted。

#### Scenario: 正常执行Foot Placement与FBBIK

- **WHEN** Foot Placement与PoseBone贡献通过唯一Assembler形成合法Goal Set且FBBIK成功
- **THEN** Runtime MUST只发布同一Completion的一个Goal Set、一次FBBIK结果和唯一OutputPose
- **AND** Foot、Pelvis与BendHistory MUST属于同一Pending Bank

#### Scenario: Goal Slot重复

- **WHEN** 两个Goal Contribution尝试写入同一FBBIK Effector Slot
- **THEN** Goal Assembler MUST在不可逆Writer前使整帧typed invalid
- **AND** Runtime MUST不按顺序覆盖、择优或创建第二Goal Set

### Requirement: 动画调试只能读取正式Snapshot

系统 MUST从同一Pending Bank已经完成并通过容量、Frame、Completion与Rig lineage验证的Runtime Result、source backend、Pose Plan、Goal Assembler、FBBIK与待写Final Pose冻结只读Diagnostics页。运行Result与Diagnostics MUST严格分型；任何运行算法 MUST不读取Diagnostics决定source、Foot Proposal、Ownership、Pelvis、Goal、Bend或最终Pose。

Runtime MUST在Frame开始冻结并预验证Live、Capture、Pose Watch与detail interest及固定容量。没有interest时 MUST跳过大页、逐骨骼和逐接触Diagnostics复制；有interest时 MUST在Physical Writer前从已完成Pending Result no-throw地写入Pending Diagnostics页。根Bank成功切换后只能发布已随Bank提交的Committed Diagnostics，不得继续写Committed页或在回调中补算。Diagnostics interest、Projector和发布回调 MUST不改变正式求解路径、状态容量与结果。

#### Scenario: Diagnostics interest中途变化

- **WHEN** Editor在当前表现帧中途打开Foot Placement或FBBIK detail interest
- **THEN** 本帧运行Result MUST保持不变且完整诊断 MAY从下一成功帧开始
- **AND** Runtime MUST不读取Pending页补齐半帧Snapshot

### Requirement: 动画表现帧必须使用预分配暂存事务

`CharacterAnimationPresentationRuntime` MUST为每个Actor使用唯一`Prepare -> Validate -> Animancer Evaluate Barrier -> Seal`表现帧事务。Pose Constraint阶段 MUST预分配两个根Bank，统一持有Foot Placement运行页、Primary Support/Pelvis页、Goal Contribution/Goal Set页、FBBIK BendHistory/紧凑Outcome页与按interest启用的Diagnostics页。根Bank和大页 MUST是预分配引用对象；运行方法 MUST不按值传递完整Bank、Ground Path固定页、FixedList payload或Diagnostics聚合体。

每帧 MUST只读取Committed Bank并写另一Pending Bank。Foot、Pelvis、Goal与Bend不得各自拥有对外Committed identity或由调用方顺序Seal。进入Writer前 MUST完成全部lineage、容量、Goal重复、FBBIK binding、Solver outcome和Writer binding验证；Writer成功后Seal MUST只执行no-throw的根Committed Bank identity切换与已验证journal发布。Discard MUST不切换根identity。大页归属根Bank MUST不允许把其业务数学搬入根Runtime。

存在Diagnostics interest时，Pending Diagnostics页 MUST在进入Writer前从同一Pending Runtime Result完成Foot、Pelvis、Goal与Bend字段的固定容量冻结与验证；Writer成功Apply时 MUST把实际Write Completion与最终Physical Bone位置写入同一Pending页。根Bank切换后只发布该Committed页。Diagnostics投影不得发生在根Bank切换之后，也不得成为修改Committed状态的延迟步骤。

#### Scenario: FBBIK后续阶段失败

- **WHEN** FBBIK已经更新Pending BendHistory但后续Pose stage或Writer验证失败
- **THEN** Committed Foot、Pelvis、Goal与BendHistory MUST全部保持上一成功帧
- **AND** 下一帧FBBIK MUST从上一Committed BendHistory重建Vendor状态

#### Scenario: Vendor存在未建模跨帧状态

- **WHEN** FBBIK Vendor对象中任一字段会影响下一帧结果但不能从Committed BendHistory、Profile和当前Goal精确重建
- **THEN** BendHistory迁移 MUST阻止实施完成并报告该状态所有权
- **AND** Runtime MUST不使用默认值、近似初始化或视觉相似结果替代8fc行为

#### Scenario: 正常提交Pose Constraint Bank

- **WHEN** Foot Placement、Goal Assembler、FBBIK和Final Writer全部通过同一Completion验证
- **THEN** Seal MUST只发布一个新的Committed Bank identity
- **AND** 任一正式读者 MUST不观察到左右脚、盆骨或BendHistory的部分提交

### Requirement: Animancer Evaluate必须是唯一不可逆提交门槛

唯一正式Animancer Graph Evaluate MUST作为动画表现帧不可逆Barrier。进入Barrier前，Runtime MUST完成Projection/Profile/Rig/World Context、全部托管identity、静态Goal Slot冲突、固定容量、readiness、source、Diagnostics interest/capacity、FinalIK binding和Final Writer binding验证，并且不得声称已经生成依赖同帧Component Pose的Foot Result、Contact Patch、运行时Goal或FBBIK Pending State，也不得提交Foot、Pelvis、BendHistory、Diagnostics或Final Pose。

Animancer Evaluate产生同帧Component Pose后，Barrier内的world-aware Foot Placement、Goal Assembler与FBBIK MUST按编译阶段生成Pending Result、Goal Set、Pending Component Pose与Pending BendHistory，并完成运行时lineage、重复Slot、非有限值与Solver outcome验证。Barrier之后只可按已冻结interest把已完成Pending Result投影到固定Pending Diagnostics页、验证Native/Writer outcome、执行唯一Physical Writer和发布已验证根Bank；不得动态查找、编译、扩容或重新计算Foot Placement业务。Barrier内或之后失败 MUST Discard根Pending Bank并使Actor Runtime进入Faulted；Writer之后若出现Unity引擎异常，系统不得恢复旧Transform后继续运行。

#### Scenario: Barrier前Foot Placement静态准备失败

- **WHEN** Foot Placement的Profile、Rig、World Context、编译容量、静态Goal Slot或binding在进入Barrier前Invalid
- **THEN** Runtime MUST不执行FBBIK或Physical Writer
- **AND** 根Pending Bank MUST被Discard

#### Scenario: Barrier内Foot Placement运行结果失败

- **WHEN** Animancer Evaluate已经产生Component Pose，但Foot Placement Patch、运行时Goal lineage、Goal Assembler或FBBIK outcome在Barrier内Invalid
- **THEN** Runtime MUST阻断后续Pose stage与Physical Writer并Discard根Pending Bank
- **AND** 同一Actor Animation Runtime MUST进入Faulted，不得把该失败降级成可恢复的Barrier前Discard

### Requirement: Final Pose写入必须在整Rig验证后原子选择Committed或Pending结果

`AnimationFinalPosePhysicalWriter` MUST同时读取当前Committed Final Pose与本帧Pending Final Pose，并在写入任何Physical Bone前验证全部Physical Bone Transform binding、PhysicalBoneCount、Pose availability、continuity identity、graph completion、Goal/FBBIK completion和frame completion。全部合法时 MUST一次写入完整Pending Physical Pose；Invalid时 MUST保持Committed Pose并阻止根Bank发布。

Physical Writer成功之后不得再执行可能失败的Foot、Pelvis、Goal、Bend或Diagnostics业务验证。随后根Bank Seal只可进行no-throw identity切换；Writer抛出Unity引擎异常时Actor MUST进入Faulted，不能伪装成可回滚继续运行。

#### Scenario: Writer成功后发布根Bank

- **WHEN** Writer已经完整写入匹配Completion的Pending Pose
- **THEN** Runtime MUST发布同Completion的Foot、Pelvis、Goal与BendHistory根Bank
- **AND** MUST不在发布前后执行新的业务查询或重新选择Goal

# character-animation-pipeline Delta

## MODIFIED Requirements

### Requirement: CharacterSimulationPresentationRuntime必须执行唯一编译Pose Plan

SimulationCommitter与唯一`CharacterSimulationPresentationRuntime` MUST共同构成Unity animation application boundary。Runtime MUST消费committed Body/Intent、Program parameter和有限Action command，构造Presentation Fact，并按Projection编译的ordered staged Pose Plan执行PoseStateMachine、state-local provider demand/readiness、ActionPlaybackInput、AnimationSlot、Local Pose composition、显式`LocalToComponentPose`、Component Pose Goal Source与控制、唯一FullBodyIK、显式`ComponentToLocalPose`、可选SecondaryMotion及FinalPublication。所有Player、Routing、Inertialization、source capture、空间转换、target value、Secondary Motion group和output completion MUST位于同一RenderFrame固定计划和同一Physical Publication batch；每个Actor MUST只执行一次PlayableGraph Evaluate，全局Magica MUST只执行一次manual batch。任一stage失败 MUST阻断后续stage与FinalPublication；若已跨过Animancer Evaluate Barrier，对应Actor Animation Runtime MUST进入Faulted且不得逆序恢复状态或Physical Bone快照。Runtime MUST不创建图外基础动画、Stack、FootPlacement、FullBodyIK、Secondary Motion、隐式Pose空间转换、第二Pose Graph或第二Physical Publication owner。

#### Scenario: FullBodyIK完成后Secondary Motion失败

- **WHEN** FullBodyIK已经发布合法Component Pose且Base Physical Pose已应用，但SecondaryMotion报告team completion失配
- **THEN** Runtime MUST阻断FinalPublication并使对应Actor进入Faulted
- **AND** MUST不发布只有IK而没有正式次级动画completion的部分Final Pose

#### Scenario: 正常Secondary Motion表现帧

- **WHEN** FullBodyIK、ComponentToLocalPose、global Magica batch和post-secondary capture依次完成
- **THEN** Runtime MUST只发布post-secondary capture形成的唯一FinalAnimationPoseFrame
- **AND** MUST不在图外再次执行Magica或从Base Pose提前发布

#### Scenario: Player source槽位复用

- **WHEN** consumer发布retirement permission且backend完成旧source物理释放
- **THEN** Runtime MAY把workspace槽位分配给新source
- **AND** 旧CaptureJob与新CaptureJob MUST不在同一次Evaluate写入同一槽位

### Requirement: 动画表现帧必须使用预分配暂存事务

`CharacterAnimationPresentationRuntime` MUST为每个Actor使用唯一`Prepare -> Validate -> Animancer Evaluate and Base Physical Apply -> Global Secondary Motion Barrier -> Post-secondary Capture -> Seal`表现帧事务。Runtime创建时 MUST按Projection编译容量一次分配Committed/Pending Dense Pose页、Native workspace页、Inertialization页、Base/Final Pose页、Secondary Motion completion页、pending scalar state、mutation journal与source lifecycle command batch。PresentationFrame MUST只读取Committed状态并写Pending页或journal；MUST不通过`CaptureState`、`Clone`、`ToArray`、新建数组、Dictionary或List复制完整旧状态以建立回滚点。成功帧 MUST在global barrier与Final capture完成后通过页索引交换和已验证journal提交新状态；Animancer Evaluate Barrier前失败 MUST只丢弃Pending，不恢复Committed对象图；Barrier期间或之后失败 MUST使受影响Actor Animation Runtime进入Faulted，不逆序恢复状态或Physical Bone快照。

#### Scenario: 普通动画表现帧成功

- **WHEN** 一个Actor使用合法Projection完成包含SecondaryMotion的PresentationFrame且没有诊断interest
- **THEN** Runtime MUST直接生成Pending Base Pose、Pending post-secondary Final Pose与Pending Module状态并在成功后交换Committed页
- **AND** 事务、Pose发布与关闭的diagnostics MUST不产生每帧托管分配
- **AND** Runtime MUST不复制完整BlendStack、Pose workspace、Magica team state或Physical Transform before-image

#### Scenario: Evaluate前验证失败

- **WHEN** Pending帧在进入Animancer Evaluate前发现identity、容量、source ownership、team或capture binding不合法
- **THEN** Runtime MUST丢弃本帧Pending页、journal和prepared resource
- **AND** 已提交Action、PoseState、Slot、Transition、source ownership、Final Pose与Physical Bones MUST保持不变

### Requirement: Animancer Evaluate必须是唯一不可逆提交门槛

唯一正式Animancer Graph Evaluate MUST作为Physical Publication Barrier的不可逆入口。进入Barrier前，Runtime MUST完成全部Actor托管identity、容量、readiness、source、release、Job binding、Secondary Motion Profile/team/collider、Base Pose Applicator与Final Pose Capture binding验证，并且 MUST不消费command acknowledgement、不提交lifecycle、不销毁Committed source、不发布release completion、Diagnostics或Final Pose。Barrier内部 MUST只执行已编译的Actor Evaluate、Base Physical Pose应用、一次global Magica manual batch、post-secondary capture与FinalPublication，不得动态查找、编译、扩容或业务输入验证。Barrier成功后Seal MUST只交换固定页、应用已验证mutation并发布成功结果。

#### Scenario: Barrier前Module准备失败

- **WHEN** Player、Slot、Routing、source backend或Secondary Motion team在Prepare阶段报告不可提交结果
- **THEN** Runtime MUST不调用对应Actor Animancer Evaluate且不得把该team加入global batch
- **AND** MUST不修改Physical Bones或任何已提交外部资源

#### Scenario: Barrier成功完成

- **WHEN** 全部参与Actor Pending状态、Job binding、Physical binding与team binding通过验证，Actor Graph Evaluate和global Magica batch成功
- **THEN** Runtime MUST以各自completion identity Seal全部Actor Module状态
- **AND** command acknowledgement、lifecycle、retirement、release completion、Final Pose和Camera MUST只属于对应成功帧

### Requirement: Final Pose写入必须在整Rig验证后原子选择Committed或Pending结果

`AnimationPhysicalPoseApplicationCoordinator` MUST在写入任何Physical Bone前验证全部Actor Physical Bone Transform binding、PhysicalBoneCount、Base Pose availability、continuity identity、graph completion、Secondary Motion计划和Final capture binding。全部合法时，Base Pose Applicator MUST把pre-secondary完整Pending Pose写入Physical Rig；global Secondary Motion backend MAY只再次写入Projection声明的controlled Physical Bone；完成后Final Pose Capture MUST读取完整PhysicalBoneCount Local Pose并形成Pending Final Pose。没有SecondaryMotion节点时，Final capture MUST读取Base应用结果。任何typed Pending、Unavailable、Invalid、team mismatch或capture failure MUST禁止FinalPublication；已经发生Physical写入时对应Actor MUST进入Faulted。系统 MUST不先发布Base Final Pose、不恢复Physical Transform快照，也 MUST不允许Coordinator之外的角色动画writer修改这些骨骼。

#### Scenario: Pending Secondary Motion Pose全部合法

- **WHEN** Base Pose、全部Magica team和post-secondary完整Rig capture合法
- **THEN** Final Pose Capture MUST把post-secondary结果写入Pending Final Pose页
- **AND** Seal MUST把对应Pending页提升为Committed并发布同一FinalAnimationPoseFrame

#### Scenario: Pending Secondary Motion Pose无效

- **WHEN** SecondaryMotion发布typed Invalid、completion不匹配或任一Physical Bone capture无效
- **THEN** Runtime MUST不发布Base Pose或部分post-secondary Pose
- **AND** 当前Actor Animation Presentation Runtime MUST进入Faulted并拒绝下一帧
- **AND** MUST不分配或恢复Physical Transform快照

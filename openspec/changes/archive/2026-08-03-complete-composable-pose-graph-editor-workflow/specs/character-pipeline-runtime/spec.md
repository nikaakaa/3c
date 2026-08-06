## MODIFIED Requirements

### Requirement: PresentationFrame必须输出完整最终Pose Plan结果

PresentationFrame MUST消费committed Body/Intent、构造typed Presentation Fact，并消费完整有限Action Selection batch与Parameter page；随后按Projection编译的ordered stage table执行PoseState selection、State source demand/capture、Action playback、Marker time resolve、AnimationSlot、Transition Routing、Local Pose composition、显式Local/Component转换、Component Pose骨骼控制、world-aware FootPlacement、后续Pose stage与FinalPublication。只有唯一OutputPose及全部必需stage完成后才可由唯一final writer发布`FinalAnimationPoseFrame`并推进Camera；任一Fact、source、MarkerSync、Player、Slot、转换、Pose operation、world query、Planner或solver失败 MUST阻止部分最终结果发布，不得沿用上一帧或绕过节点。

#### Scenario: Action等待第一Selection sample

- **WHEN** Program已经选择Action但Presentation尚无合法Selection sample
- **THEN** AnimationSlot MUST按compiled pending/availability policy处理
- **AND** Locomotion PoseState MUST继续来自同帧Fact而不是历史BaseLocomotion selection

#### Scenario: FootPlacement后还有Pose节点

- **WHEN** ordered stage table在FootPlacement后包含合法Component Pose控制与ComponentToLocalPose
- **THEN** PresentationFrame MUST继续执行这些stage后再final write
- **AND** MUST不在FootPlacement完成时提前发布FinalAnimationPoseFrame

### Requirement: Pipeline domain debug 必须进入统一 Trace

Input、ingress、Program operation、StateMachine、Timeline、Blackboard、WorldRequest/Result、Action、Effect、commit、Animation、Pose stage、Pose空间、Foot Placement和Camera diagnostics MUST进入统一structured Trace/view model。Inspector MUST不遍历旧stage、Final IK组件、Transform差值或runtime service私有集合形成平行调试链。Foot Placement trace MUST只读取正式节点completion snapshot，不得重新执行地面查询或solver。

#### Scenario: 查看一次 Dodge Tick

- **WHEN** Debug Session 定位 Dodge EventId
- **THEN** MUST关联input、operation、world batch与committed animation command

#### Scenario: 查看楼梯上的右脚replant

- **WHEN** Foot Placement snapshot记录右脚因超出reach从Locked释放
- **THEN** 统一Trace MUST显示同帧Body、上游Pose completion、surface、constraint reason、pelvis offset和solver结果
- **AND** Inspector MUST不直接读取Final IK或Animator mutable状态

### Requirement: PresentationFrame必须原子提交动画播放与Pose节点生命周期

PresentationFrame MUST在同一外层事务中提交Presentation Fact page、PoseStateMachine active/target state、Sequence/Selection source usage、Marker relation/effective sample page、AnimationSlot state、BlendStack状态、Transition Routing capture/release、Inertialization、空间转换、Pose operation completion、world-aware plan、Component Pose solver结果和final publication。Reset、branch replacement或Projection replacement MUST按compiled stage与operation清理或重建全部stateful节点。Animancer Evaluate Barrier前失败 MUST只Discard Pending；stage失败已经跨过Barrier时 MUST阻断后续stage与final publication并使同一Actor Animation Presentation Runtime进入Faulted，不得恢复状态或Physical Bone快照。任何路径不得只提交Action playback、FootPlacement plan或中间Pose而保留旧Output。

#### Scenario: Action Selection与首个Sample同批

- **WHEN** 新Selection与首份合法source sample在同一PresentationFrame到达
- **THEN** Slot MUST原子初始化并参与本帧完整staged Pose Plan
- **AND** FinalAnimationPoseFrame MUST只反映该次完整事务结果

#### Scenario: World context在求解前失效

- **WHEN** FootPlacement query前精确PhysicsScene或world binding失效
- **THEN** transaction MUST阻断后续stage并保持final publication不可用
- **AND** 当前Actor Animation Presentation Runtime MUST进入Faulted
- **AND** MUST不发布上游未放脚Pose作为替代最终结果

## MODIFIED Requirements

### Requirement: Foot Placement 必须是唯一 Presentation Pose Post Process Pass

`CharacterSimulationPresentationRuntime` MUST在每个合法PresentationFrame中按`Body -> Pose Slot Blend Stack/Source Sampling -> Character Presentation Pose Graph -> Foot Placement -> Camera`固定顺序推进角色表现。Foot Placement MUST只在`AnimationPosePlayableGraphRuntime`发布有效lease-protected `FinalAnimationPoseFrame`后执行一次，并由该Runtime显式创建、更新、reset和dispose。Foot Placement MUST不成为Pose Graph节点，也 MUST不依赖Final IK、Animator、MonoBehaviour或其它manager自主更新形成第二个姿势写入路径。

#### Scenario: 一个表现帧更新Corin

- **WHEN** 两个PoseSlotFrame和OutputPose完成本帧最终未IK pose
- **THEN** Foot Placement MUST读取FinalAnimationPoseFrame并只执行一次
- **AND** Camera MUST在Foot Placement完成后执行

#### Scenario: Pose Graph completion无效

- **WHEN** RequireOutput slot缺失或任一Pose operation产生非法值
- **THEN** Foot Placement MUST reset并拒绝该frame
- **AND** MUST不使用上一帧Animancer或OutputPose残留

### Requirement: Foot Placement 必须只消费表现帧正式输入

Foot Placement MUST只读取同帧`CharacterBodyPresentationFrame`、Pose Graph完成后的最终未IK骨骼姿势、FinalAnimationPoseFrame发布的`AnimationFootPoseInput`、与最终source contribution精确匹配的Projection生成每脚特征、显式Foot Placement Profile、Rig Binding、Rig Calibration和当前PhysicsScene查询结果。左右脚输入 MUST使用Pose Graph在最终Bone Mask、slot weight、Stored Pose与Inertial合成之后发布的actual contribution；不得使用root weight、单一slot scalar、Blend Stack未经过空间合成的weight或Animancer state weight替代。

#### Scenario: FullBody Action覆盖脚部

- **WHEN** FullBodyActionSlot以全身Mask完全覆盖LeftFoot
- **THEN** LeftFoot feature MUST来自action slot最终贡献
- **AND** BaseLocomotion LeftFoot feature MUST不重复参与

#### Scenario: UpperBody overlay不覆盖脚部

- **WHEN** 某overlay的LeftFoot与RightFoot dense mask均为零
- **THEN** 两脚pose与feature contribution MUST继续完全来自Base输入
- **AND** overlay slot scalar MUST不降低Foot Placement权重

#### Scenario: Stored Pose仍有贡献

- **WHEN** 某slot Stored Pose在最终LeftFoot合成后仍有非零贡献
- **THEN** Foot Placement MUST消费capture时的合法LeftFoot feature aggregate
- **AND** MUST不要求Stored Pose伪造AnimationPoseSourceId或AnimationClip

### Requirement: Animation Clip Foot Placement曲线必须沿正式表现投影采样

每个Timeline Animation Clip MUST继续唯一保存可写`Foot Placement Weight`曲线。Player Runtime MUST先按live source VisualSampleTime采样Projection feature，由每PoseSlot Blend Stack形成slot内部live/Stored/Inertial feature contribution，再由Pose Graph按最终Bone Mask和composition生成唯一左右脚`AnimationFootPoseInput`。同一作者Weight对constraint position、rotation、clearance与Pelvis各自最终求解链 MUST只应用一次，不得在source、Stack、Pose Graph与IK中重复相乘。

#### Scenario: Marker Sync改变Base source时间

- **WHEN** BaseLocomotion source的VisualSampleTime被Marker Sync映射
- **THEN** 该source Foot Analysis MUST按effective time采样
- **AND** Pose Graph MUST只组合采样后的正式feature contribution

#### Scenario: FullBody Action淡出

- **WHEN** action slot从Stored/Inertial状态淡出到NoPose
- **THEN** final per-foot input MUST连续转回BaseLocomotion feature
- **AND** Foot Placement MUST不重新采样已经退役的action source

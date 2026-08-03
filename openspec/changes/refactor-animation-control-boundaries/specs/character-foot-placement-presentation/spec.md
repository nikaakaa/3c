# character-foot-placement-presentation Specification

## MODIFIED Requirements

### Requirement: Foot Placement 必须只消费表现帧正式输入

Foot Placement MUST只读取同帧`CharacterBodyPresentationFrame`、已完成的`FinalAnimationPoseFrame`前置Pose结果、最终骨骼姿势、显式Profile、rig binding、Calibration和当前PhysicsScene查询结果。Profile构造runtime settings时 MUST从Projection Pose Program一次性绑定唯一`animation.foot-placement-weight`参数。Present MUST核对同帧Completion、Availability、ProgramHash、最终Foot Features和有限归一化Weight。它 MUST不读取PoseState、AnimationSlot或Playback列表重新仲裁source，不再次采样Projection或Clip，也 MUST不读取BTSMTL State、Action、Blackboard、Marker语义、MotionWarp target或logic Transform作为替代真相。

#### Scenario: 读取Slot与PoseState组合后的姿态

- **WHEN** Locomotion PoseState source与Action Slot共同形成当前最终Pose
- **THEN** Foot Placement MUST只消费该Completion对应的最终Foot Features和最终Weight
- **AND** MUST不遍历Pose source或Action playback重新计算混合结果

#### Scenario: Runtime Projection缺少生成特征

- **WHEN** 启用Foot Placement的角色加载不含匹配Calibration与source feature的Projection
- **THEN** Host创建 MUST失败并定位缺失binding identity
- **AND** Runtime MUST不即时分析Clip或退回姿势差分fallback

### Requirement: Animation Clip Foot Placement曲线必须沿正式表现投影采样

有限Action Timeline Animation Clip MUST继续唯一保存自己的可写`Foot Placement Weight`曲线；持续Presentation Pose source MUST在Profile source binding唯一保存source-local`Foot Placement Weight` typed curve。左右脚sole速度、高度、plant confidence与landing feature MUST由Editor-only artifact生成并在Definition Build时嵌入对应Action producer或Pose source Projection binding，不得成为Timeline Track lane、可编辑generated Curve、Blackboard或Document editable字段。Action producer、SequencePlayer、BlendSpacePlayer与Motion Matching source MUST在各自同一effective visual sample time/cycle把唯一作者Weight和generated Foot Features写入正式source pose payload。Pose transition、AnimationSlot、BlendStack与Pose Graph MUST按显式policy形成唯一`FinalAnimationPoseFrame`，Foot Placement只读取最终参数与最终特征。

#### Scenario: 编辑Action Foot Placement Weight

- **WHEN** 作者在Attack Timeline编辑Foot Placement Weight
- **THEN** Timeline MUST只修改该Action Animation Clip作者曲线
- **AND** generated artifact与Pose source binding MUST不被同步修改

#### Scenario: 编辑Run Foot Placement Weight

- **WHEN** 作者从Run SequencePlayer打开Foot Placement Weight
- **THEN** Profile source editor MUST只修改Run Pose source typed curve
- **AND** MUST不创建Run Timeline Clip

#### Scenario: Marker Sync后的最终特征

- **WHEN** Action MarkerSync或PoseState Source Sync改变某source的effective sample time
- **THEN** 该source MUST在同一时间写入Foot Features与Foot Placement Weight
- **AND** Foot Placement MUST读取Pose Graph最终结果且不重新采样source
- **AND** MUST不读取MarkerId作为plant或contact真相

## MODIFIED Requirements

### Requirement: Foot Placement 必须只消费表现帧正式输入

Foot Placement MUST只读取同帧`CharacterBodyPresentationFrame`、Animancer Evaluate后带有效lease的`FinalAnimationPoseFrame`、最终骨骼姿势、显式`CharacterFootPlacementProfile`、显式rig binding、同identity Rig Calibration和当前Unity PhysicsScene查询结果。Profile构造runtime settings时 MUST从`Projection.PoseProgram.Parameters`一次性绑定唯一`animation.foot-placement-weight`的`PoseParameterId`、dense index与`PoseProgramHash`；Present MUST核对同帧Completion、Availability、ProgramHash、最终Foot Features和有限归一化Weight。若上游包含Inertialization，Foot Placement MUST读取该节点经过下游composition形成的实际贡献与最终Foot Features，MUST不读取旧Stack Inertial contribution、Accumulator或Discontinuity来重新选择动画。它 MUST不读取visible playback列表、Layer、producer binding，不再次采样Projection或AnimationClip，也 MUST不读取BTSMTL runtime、State、Action、Blackboard、GameplayTag、Animation Marker语义、MotionWarp target、WorldSolver对象、Network Model私有状态或logic Transform作为替代真相。

#### Scenario: 读取CrossFade后的最终姿态帧

- **WHEN** Outgoing与Current source经Blend Stack和Pose Graph共同形成最终姿势
- **THEN** Foot Placement MUST只消费该次Completion对应的最终Foot Features和最终`animation.foot-placement-weight`
- **AND** MUST不遍历source重新计算一次混合结果

#### Scenario: Runtime Projection缺少生成特征

- **WHEN** 启用Foot Placement的角色加载不含匹配Calibration与clip feature的Projection
- **THEN** Host创建 MUST失败并定位缺失identity
- **AND** Runtime MUST不即时分析AnimationClip或退回最终姿势差分独占路径

#### Scenario: 左脚分支正在惯性衰减

- **WHEN** 上游Inertialization对左脚Bone仍有残差
- **THEN** FootPlacement MUST消费最终left-foot feature与骨骼结果
- **AND** MUST不查询旧source或MM candidate

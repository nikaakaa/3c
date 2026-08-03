## MODIFIED Requirements

### Requirement: Pipeline domain debug 必须进入统一 Trace

Input、ingress、Program operation、StateMachine、Timeline、Blackboard、WorldRequest/Result、Action、Effect、commit、Animation、Pose Constraint、Foot Placement和Camera diagnostics MUST进入统一structured Trace/view model。Animation Presentation debug view MUST在完整帧事务成功提交后，从同一个已完成Pose workspace发布Rig/Projection identity、`PhysicalBoneCount`、`VirtualBoneCount`、`PoseBoneCount`、Virtual Bone local/component pose与Source/Target identity，以及按PoseNodeId记录的TwoBoneIK chain、Effector、Joint Target、Weight、rotation mode、reach状态、残差和typed failure。Trace、Pose Watch与Inspector MUST只复制该有界结果，不得重新派生Virtual Bone、再次执行TwoBoneIK、读取Animator最终Transform反推结果或遍历FinalIK mutable state。

#### Scenario: 查看Virtual Bone与手臂IK

- **WHEN** 已提交帧包含Virtual Bone source capture和TwoBoneIK operation
- **THEN** RuntimeDebugSession MUST显示匹配Projection revision的Bone Kind、Source/Target与IK输入输出
- **AND** 数据 MUST来自该帧完成Pose page和constraint result

#### Scenario: IK运行时退化

- **WHEN** TwoBoneIK因非有限输入、零长度或弯曲平面退化失败
- **THEN** 统一Trace MUST记录精确PoseNodeId与typed failure
- **AND** Inspector MUST不读取上一帧IK结果或重新运行solver补值

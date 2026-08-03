## MODIFIED Requirements

### Requirement: Pose Graph必须显式选择Animation Player

每个Animation Selection MUST只通过Pose Graph中的`SelectedPosePlayer`或`BlendStack`节点降低为Pose Value。`SelectedPosePlayer` MUST只采样当前Selection并发布typed discontinuity，MUST不计算Inertial residual；没有下游Inertialization时执行明确硬切。`BlendStack` MUST保存该节点自己的多source历史并只执行已编译CrossFade或Stored Pose连续化。需要单Pose惯性化时，作者 MUST显式连接`SelectedPosePlayer -> Inertialization`。Compiler与Runtime MUST不在Selection Input、AnimationChannel或OutputPose背后自动插入Player、Stack、Inertialization或fade。

#### Scenario: 稳定动作使用直接Player

- **WHEN** 作者把Action Selection连接到SelectedPosePlayer
- **THEN** Selection变化 MUST直接替换当前source
- **AND** Runtime MUST不创建隐藏Blend Stack

#### Scenario: 状态机输出使用Blend Stack

- **WHEN** 作者把BaseLocomotion Selection连接到BlendStack
- **THEN** Selection变化 MUST由该节点保存旧player并连续过渡
- **AND** 其它未连接该节点的Selection MUST不承担其workspace或transition

#### Scenario: MM使用局部Inertialization

- **WHEN** MotionMatchingSelectionInput连接SelectedPosePlayer再连接Inertialization
- **THEN** MM jump MUST由Player发布Discontinuity
- **AND** residual MUST只由Inertialization节点计算

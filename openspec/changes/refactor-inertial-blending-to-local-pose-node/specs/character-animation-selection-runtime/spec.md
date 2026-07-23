## MODIFIED Requirements

### Requirement: Pose Graph必须显式选择Animation Player

Selection MUST显式连接`SelectedPosePlayer`或`BlendStack`。SelectedPosePlayer MUST输出当前Pose与typed PoseDiscontinuity；它 MUST不计算Inertial residual。BlendStack MUST只执行多source CrossFade/Stored Pose连续化。需要单Pose惯性化时，作者 MUST显式连接`SelectedPosePlayer -> Inertialization`。

#### Scenario: MM使用局部Inertialization

- **WHEN** MotionMatchingSelectionInput连接SelectedPosePlayer再连接Inertialization
- **THEN** MM jump MUST由Player发布Discontinuity
- **AND** residual MUST只由Inertialization节点计算

### Requirement: Blend Stack节点必须独占自身多source时间连续性

BlendStack MUST唯一拥有其active sources、CrossFade clocks、Stored Pose、capacity与source release，但 MUST不拥有Inertial accumulator或residual。Runtime MUST不在Stack内保留`AnimationBlendTechnique.Inertial`。

#### Scenario: Blend Stack收到新Selection

- **WHEN** 节点已有旧source且收到新source identity
- **THEN** 节点 MUST只按CrossFade/Stored Policy处理
- **AND** MUST不创建Inertial residual


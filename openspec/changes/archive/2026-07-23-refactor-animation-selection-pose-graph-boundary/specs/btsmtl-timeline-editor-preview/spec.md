## MODIFIED Requirements

### Requirement: 预览采样必须复用正式动画Selection与Pose Plan

Authoring Preview MUST把当前Timeline/Track/Clip时间降低为含raw visual time的正式Animation Selection与Parameter page，并执行匹配Projection的`CharacterPresentationPosePlan`。Preview MUST只在图中存在MarkerSync时显示和应用effective time，并按图中显式节点决定直接Player硬切、局部Inertialization残差或Blend Stack多source历史，按同一Layered/Additive/ModifyBone拓扑生成Composed Pose；具备正式Body与PhysicsScene上下文时 MAY执行FootPlacement节点，否则 MUST把world-aware阶段标记为Unavailable。Preview MUST不创建隐藏Marker Sync、固定per-slot Stack、隐藏Inertialization、简化PoseGraph、Animancer direct Play或假Foot Physics。

#### Scenario: 当前时间采样

- **WHEN** 作者把Preview游标移动到Attack clip中间
- **THEN** Preview MUST生成对应Attack Selection并送入图中绑定channel的Selection Input
- **AND** 最终路径 MUST与图中Player和Pose节点连接一致

#### Scenario: 预览Walk到Run handoff

- **WHEN** BaseLocomotion图路径包含MarkerSync且Preview从Walk切到Run
- **THEN** Preview MUST显示该PoseNodeId的raw/effective time、leader、marker pair与fraction
- **AND** 删除MarkerSync节点后同一Preview MUST使用raw time且不得后台继续同步

#### Scenario: 非连续seek

- **WHEN** 作者从一个producer非连续seek到另一个producer
- **THEN** 连接SelectedPosePlayer时 MUST硬切
- **AND** 连接Inertialization时 MUST按正式seek/reset policy处理history与residual
- **AND** 连接BlendStack时 MUST按正式node reset/seek policy处理而不创建额外fade

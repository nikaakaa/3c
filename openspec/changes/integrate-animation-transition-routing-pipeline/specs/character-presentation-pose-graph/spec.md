# Character Presentation Pose Graph Specification

## MODIFIED Requirements

### Requirement: Pose Graph工作区必须显式解释完整表现拓扑

Pose Graph正式窗口 MUST在同一工作区提供Definition-scoped Navigator、唯一Graph Canvas、右侧Details和可折叠Bottom Dock。作者 MUST能沿typed edge、typed Inertialization request route和只读source mapping追踪`Animation Selection -> MarkerSync -> SelectedPosePlayer或BlendStack -> Inertialization -> Pose composition -> FootPlacement -> OutputPose`。工作区 MUST不在画布外补建隐藏Player、Stack、Inertialization、request bus、FootPlacement或第二Output路径。

#### Scenario: 查看角色Locomotion链

- **WHEN** 作者从CharacterAnimationPresentationProfile打开正式Pose Graph
- **THEN** Navigator MUST显示BaseLocomotion AnimationChannel及其可达producer
- **AND** Graph Canvas MUST显示Selection、MarkerSync、Player、Inertialization和最终合成路径
- **AND** Details MUST能定位每个节点的正式owner与runtime operation

#### Scenario: 查看Action惯性化请求路由

- **WHEN** 作者选中FullBodyAction BlendStack或Action Inertialization节点
- **THEN** Graph Canvas MUST高亮两者之间的typed request route
- **AND** Details MUST显示可发布请求的精确transition rule及唯一consumer
- **AND** 工作区 MUST不把request route绘制成pose edge

### Requirement: Pose Graph工作区必须准确使用UE对应术语

UI MAY使用`Anim Graph`、`Details`、`Layered Blend Per Bone`、`Inertialization`、`Sync Group`、`Pose Watch`和`Output Pose`等语义一致术语，但 MUST保留正式serialized node kind与identity。transition rule的`Blend Logic` MUST只提供`Standard Blend`与`Inertialization`；零时长Standard Blend MUST显示为Hard Cut结果但 MUST不序列化独立Hard Cut逻辑；BlendStack容量压缩 MUST显示为`Stored Pose` policy但 MUST不把Stored Pose列为Blend Logic。UI MUST不提供未实现的`Custom`占位项，不把AnimationChannel称为Montage Slot、不把BTSMTL Timeline称为Montage、不把Gameplay StateMachine称为Animation State Machine，也 MUST不把Pose Plan的world-aware阶段称为独立Post Process Anim Blueprint。

#### Scenario: 显示BaseLocomotion来源

- **WHEN** Navigator显示`BaseLocomotion`
- **THEN** UI MUST标识其正式类型为Animation Channel
- **AND** MAY提供与UE命名表现入口的概念说明
- **AND** MUST不序列化Slot、Montage或Anim State Machine配置

#### Scenario: 编辑Blend Logic

- **WHEN** 作者编辑一条Player或BlendStack transition rule
- **THEN** Details MUST以UE一致语义显示Standard Blend与Inertialization
- **AND** Duration为零的Standard Blend MUST明确显示其结果为Hard Cut
- **AND** Stored Pose MUST只出现在BlendStack容量与历史策略中
- **AND** Custom MUST不出现在可选值中

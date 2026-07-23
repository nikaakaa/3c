## RENAMED Requirements

- FROM: `### Requirement: Blend Library必须是每Pose Slot转场权威`
- TO: `### Requirement: Blend Policy必须属于显式Blend Stack节点`

## MODIFIED Requirements

### Requirement: Blend Policy必须属于显式Blend Stack节点

`CharacterAnimationPresentationProfile` MUST通过Pose Graph中的显式Blend Stack节点引用`CharacterAnimationBlendPolicy`。Policy MUST保存Stack容量、Stored Pose policy、canonical curve、Blend Profile、authoring default和exact override；Compiler MUST只为引用该Policy的节点物化可达endpoint完整table。Timeline、BTSMTL Graph、Program、SelectedPosePlayer、Equipment Feature与Prefab MUST不保存第二份transition表。

#### Scenario: FullBodyAction使用ActionPolicy

- **WHEN** 作者在Pose Graph把FullBodyAction Selection连接到引用ActionPolicy的BlendStack
- **THEN** Inspector MUST显示该节点可达Action endpoint与完整transition状态
- **AND** 未连接BlendStack的Selection MUST不要求配置ActionPolicy

### Requirement: CharacterAnimationPresentationProfile Inspector必须是唯一Presentation配置入口

Profile Inspector MUST唯一编辑Pose Graph、Blend Policy、Inertialization Policy、Rig Definition、producer source binding、Foot Analysis Mode与Analysis Source GUID。Pose Graph Editor MUST编辑Selection Input、MarkerSync、SelectedPosePlayer、Blend Stack、Inertialization、Blend/Layered/Additive、Parameter、ModifyBone、FootPlacement和Output拓扑；Timeline Editor继续唯一编辑producer-local Clip、SyncGroup、Topology、SyncRole、Point Marker、Window与registered Curve。MarkerSync节点 MUST不复制Track marker数据；Profile、Timeline、Definition或Prefab MUST不保存Pose Graph节点配置副本。

#### Scenario: 从Profile查看BaseLocomotion

- **WHEN** 作者从Profile打开Pose Graph并定位BaseLocomotion AnimationChannel
- **THEN** Editor MUST显示该Selection可选的MarkerSync、连接的SelectedPosePlayer、可选Inertialization或BlendStack，以及后续混合、骨骼处理与Output路径
- **AND** MUST不只显示隐藏Stack摘要

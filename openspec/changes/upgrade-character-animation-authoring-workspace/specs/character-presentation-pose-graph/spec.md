## ADDED Requirements

### Requirement: Pose Graph工作区必须显式解释完整表现拓扑

Pose Graph正式窗口 MUST在同一工作区提供Definition-scoped Navigator、唯一Graph Canvas、右侧Details和可折叠Bottom Dock。作者 MUST能沿typed edge和只读source mapping追踪`Animation Selection -> MarkerSync -> SelectedPosePlayer或BlendStack -> Pose composition -> FootPlacement -> OutputPose`。工作区 MUST不在画布外补建隐藏Player、Stack、Inertialization、FootPlacement或第二Output路径。

#### Scenario: 查看Corin Locomotion链

- **WHEN** 作者从Corin CharacterAnimationPresentationProfile打开正式Pose Graph
- **THEN** Navigator MUST显示BaseLocomotion AnimationChannel及其可达producer
- **AND** Graph Canvas MUST显示Selection、MarkerSync、Player、Inertialization和最终合成路径
- **AND** Details MUST能定位每个节点的正式owner与runtime operation

### Requirement: Pose Graph Details必须分离Authoring、Live与References

Pose Graph右侧Details MUST提供`Authoring`、`Live`和`References`三个互斥内容页。Authoring MUST只通过Pose mutation adapter编辑当前节点正式拥有的字段；Live MUST只读取匹配PoseGraphId、PoseGraphRevision与ProjectionRevision的正式runtime或Preview snapshot；References MUST只读显示source map、call site、reachable producer、Profile、Rig和Policy owner并提供精确导航。任一页面 MUST不修改其它资产或重新求值Pose Plan。

#### Scenario: 选择MarkerSync节点

- **WHEN** 作者选择MarkerSync节点
- **THEN** Authoring MUST只显示该节点正式Pose Graph配置
- **AND** Live MAY显示当前source、raw/effective time、Marker segment与fraction
- **AND** References MUST只读显示相关AnimationTrack并提供Open Source Timeline
- **AND** Details MUST不允许移动、创建或删除Track Marker

#### Scenario: Runtime revision不匹配

- **WHEN** 当前runtime snapshot的PoseGraph或Projection revision与打开文档不一致
- **THEN** Live MUST显示Stale并清空旧node值
- **AND** MUST不从authoring默认值或Animancer state伪造Live结果

### Requirement: Pose Graph画布必须提供source-mapped Live可视化

在匹配正式snapshot时，Graph Canvas MAY按PoseNodeId和call-site显示节点执行高亮、availability、source、weight、阶段角标、Sync Group水印和Output completion。连线权重和节点状态 MUST来自正式Pose operation trace与source contribution，不得由Editor重新混合、重采样或按拓扑猜测。Authoring模式和Live Debug模式 MUST保持窗口级边界，Live Debug下mutation命令 MUST只读。

#### Scenario: Action覆盖Locomotion

- **WHEN** FullBodyAction BlendStack与BaseLocomotion同时对最终Pose有贡献
- **THEN** Graph Canvas MUST按正式trace显示两个分支及其实际权重
- **AND** MUST不把AnimationChannel显示为UE Montage Slot或推断不存在的State Machine

### Requirement: Pose Preview必须显式执行正式Pose Plan

Pose Graph Bottom Dock MAY提供Authoring Preview，但作者 MUST显式选择精确CharacterPipelineDefinition和合法Preview Target，并通过Play、Pause、Step或Seek命令推进。Preview MUST只执行与当前authoring revision匹配的已发布Projection和正式Pose Plan；缺少、Invalid或Stale时 MUST停止并显示原因。selection、Graph mutation、窗口恢复、AssetDatabase事件或Preview target变化 MUST不自动Build或启动Preview。

#### Scenario: Graph修改后继续Preview

- **WHEN** 作者修改Pose Graph使已发布Projection变为Stale
- **THEN** Preview MUST停止消费旧Plan并显示Stale
- **AND** MUST等待作者显式Compile/Build
- **AND** MUST不创建临时Plan、默认Player或旧Projection fallback

#### Scenario: Preview执行FootPlacement但缺少world context

- **WHEN** 正式Pose Plan到达FootPlacement且当前Preview Target不提供合法world context
- **THEN** Preview MUST显示world-aware阶段Unavailable
- **AND** MUST不跳过FootPlacement后伪造FinalAnimationPoseFrame

### Requirement: Pose Watch必须只观察已完成的正式Pose Value

Editor MUST允许作者按稳定PoseNodeId与call-site显式订阅一个或多个Pose Watch。Watch selection、颜色、显隐和面板状态 MUST只属于editor view-state。Preview或Runtime diagnostics MUST从已完成Pose Plan workspace复制固定容量的目标Pose与contribution，不得重新执行节点、第二次采样source、修改Player/Blend/Inertialization history或改变FinalAnimationPoseFrame。

#### Scenario: 同时观察Player与FootPlacement输出

- **WHEN** 作者对Player输出和FootPlacement输出启用Pose Watch
- **THEN** Preview或Live Debug MUST从同一frame completion发布两个只读观察结果
- **AND** 两个Watch MUST不触发额外PlayableGraph Evaluate

#### Scenario: 关闭Pose Graph窗口

- **WHEN** 拥有Pose Watch的窗口关闭或切换runtime target
- **THEN** 窗口 MUST释放自己的debug interest
- **AND** runtime provider MUST不继续无界保留该窗口的Pose历史

### Requirement: Pose Graph工作区必须准确使用UE对应术语

UI MAY使用`Anim Graph`、`Details`、`Layered Blend Per Bone`、`Inertialization`、`Sync Group`、`Pose Watch`和`Output Pose`等语义一致术语，但 MUST保留正式serialized node kind与identity。UI MUST不把AnimationChannel称为Montage Slot、不把BTSMTL Timeline称为Montage、不把Gameplay StateMachine称为Animation State Machine，也 MUST不把Pose Plan的world-aware阶段称为独立Post Process Anim Blueprint。

#### Scenario: 显示BaseLocomotion来源

- **WHEN** Navigator显示`BaseLocomotion`
- **THEN** UI MUST标识其正式类型为Animation Channel
- **AND** MAY提供与UE命名表现入口的概念说明
- **AND** MUST不序列化Slot、Montage或Anim State Machine配置


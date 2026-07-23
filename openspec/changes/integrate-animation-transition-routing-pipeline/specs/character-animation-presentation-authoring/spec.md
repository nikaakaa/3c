# character-animation-presentation-authoring Specification

## MODIFIED Requirements

### Requirement: Pipeline Definition 必须引用唯一 Animation Presentation Profile

`CharacterPipelineDefinition` MUST引用唯一`CharacterAnimationPresentationProfile`，不得内联保存Animation Presentation数据。该Profile MUST唯一引用`CharacterPresentationPoseGraphAsset`、node-local `CharacterAnimationBlendPolicy`、consumer级`CharacterPoseInertializationPolicy`、`CharacterAnimationRigDefinition`，保存稳定producer source bindings，以及显式Foot Placement Analysis Mode与Analysis Source Asset GUID。Pose Graph MUST唯一保存Selection Input、MarkerSync、Player、Inertialization request consumer、Bone Mask composition、Pose Parameter policy、FootPlacement与Output topology；Blend Policy MUST按可达endpoint选择Standard Blend或Inertialization并保存Stack Policy，Inertialization Policy MUST只保存consumer数学、过滤与reset配置。Analysis Source MUST是Editor-only Projection生成输入，只负责生成表现特征，不得保存Graph flow、State、Action、Gameplay contact或运行时IK状态。Profile MUST不持有Analysis Source或Sampling Rig对象强引用。Graph、StateMachine、Timeline、Presenter、Prefab重复数值、旧SO或独立Pipeline表 MUST不保存同一配置的第二份真相。

#### Scenario: Corin启用生成Foot Analysis

- **WHEN** Corin Profile选择GeneratedPerFootFeatures
- **THEN** Profile MUST保存可精确解析到唯一Analysis Source的Asset GUID
- **AND** Source MUST显式引用Sampling Rig与Rig Calibration
- **AND** Definition MUST不内联复制这些字段

#### Scenario: shared Graph被多个角色使用

- **WHEN** 两个CharacterPipelineDefinition引用同一个shared Graph/Timeline
- **THEN** 两个角色 MAY引用不同CharacterAnimationPresentationProfile、Blend/Inertialization Policy和Analysis Source
- **AND** 每个角色 MUST生成与自身Rig、route和Calibration匹配的Projection
- **AND** shared Graph/Timeline MUST不保存角色级分析Rig、Blend Logic或校准

### Requirement: Blend Policy必须属于显式Blend Stack节点

`CharacterAnimationPresentationProfile` MUST通过Pose Graph中的显式BlendStack节点引用唯一`CharacterAnimationBlendPolicy`。Policy MUST保存Stack的`Max Active Blends`、`Store Blended Pose`、`Max Blend In Time To Override Animation`、authoring default和exact source-target/Empty override；每条transition MUST使用`StandardBlend`或`Inertialization` Blend Logic。Standard Blend MUST保存Duration、Mode/Curve与Blend Profile，Duration为0 MUST表达硬切；Inertialization MUST保存请求Duration与Blend Profile并在Compiler阶段绑定唯一下游consumer route。Stored Pose MUST不成为Blend Logic。Compiler MUST只为引用该Policy的节点物化可达endpoint完整table。Timeline、BTSMTL Graph、Program、SelectedPosePlayer、Equipment Feature、Inertialization Policy与Prefab MUST不保存第二份transition表。Animancer source backend MUST只复用和采样source playable，不得调用TransitionLibrary、AnimancerLayer.Play、StartFade或FadeGroup决定转场。

#### Scenario: 播放目标producer并使用Standard Blend

- **WHEN** selected producer收到第一份合法sample且exact rule为StandardBlend
- **THEN** 对应显式BlendStack MUST按Projection中的Duration、Mode/Curve与Blend Profile开始时间混合
- **AND** Animancer source backend MUST只提供所需source pose sample

#### Scenario: 播放目标producer并使用Inertialization

- **WHEN** selected producer收到第一份合法sample且exact rule为Inertialization
- **THEN** 对应显式BlendStack MUST发布compiled request给唯一consumer
- **AND** Inertialization Policy MUST不复制该source-target业务规则

#### Scenario: FullBodyAction淡出到Empty

- **WHEN** FullBodyAction channel提交None且当前action source仍有贡献
- **THEN** FullBodyAction BlendStack MUST使用节点Policy中的Standard Blend source-to-Empty transition连续淡出
- **AND** 系统 MUST不对Empty发出惯性请求或从默认duration补值

### Requirement: CharacterAnimationPresentationProfile Inspector必须是唯一Presentation配置入口

Profile Inspector MUST唯一编辑Pose Graph、Blend Policy、Inertialization consumer Policy、Rig Definition、producer source binding、Foot Analysis Mode与Analysis Source GUID。Pose Graph Editor MUST编辑Selection Input、MarkerSync、SelectedPosePlayer、BlendSpacePlayer、BlendStack、Inertialization、Blend/Layered/Additive、Parameter、ModifyBone、FootPlacement和Output拓扑；BlendStack Details MUST使用`Blend Logic`、`Standard Blend`、`Inertialization`、`Max Active Blends`、`Store Blended Pose`、`Max Blend In Time To Override Animation`、`Duration`、`Mode`、`Custom Blend Curve`与`Blend Profile`等UE对应名称，但 MUST不显示未安装的Custom Blend Logic或独立HardCut选项。Timeline Editor继续唯一编辑producer-local Clip、SyncGroup、Topology、SyncRole、Point Marker、Window与registered Curve。MarkerSync节点 MUST不复制Track marker数据；Profile、Timeline、Definition或Prefab MUST不保存Pose Graph节点配置副本。

#### Scenario: 编辑Action Transition

- **WHEN** 作者在Corin Action BlendStack Details选择一个exact endpoint pair
- **THEN** Inspector MUST允许选择Standard Blend或Inertialization并编辑对应合法字段
- **AND** Stored Pose MUST只显示在Stack Policy区域
- **AND** Inertialization consumer References MUST只读显示该request route

#### Scenario: 从Profile打开Timeline Analysis

- **WHEN** 作者从精确Profile上下文打开Timeline并选择AnimationClip
- **THEN** Analysis provider MAY把该Profile的Source作为显式初始选择
- **AND** Timeline资产 MUST不因打开或分析而变脏

#### Scenario: shared Timeline用于不同角色

- **WHEN** 两个Profile使用同一shared Timeline但不同Analysis Source
- **THEN** 各自 MUST生成不同artifact identity与Projection
- **AND** shared Timeline MUST不保存任一角色的Analysis Source或Blend Logic

### Requirement: 跨资产表现配置必须保持唯一写入口

Pose Graph Navigator、Details与Bottom Dock MAY只读显示AnimationTrack的Clip、SyncGroup、Topology、SyncRole、Marker，Profile/Rig/Blend Policy/Inertialization Policy owner、compiled request route以及generated Foot Analysis状态。修改Clip、Marker和registered Curve MUST精确导航到Timeline Editor；修改Profile、Rig、Blend Logic、Stack Policy、Inertialization consumer Policy和Analysis Source MUST精确导航到各自正式Inspector。Pose Graph Workspace MUST不复制这些字段、直接写SerializedProperty或提供第二mutation命令。

#### Scenario: 从Sync面板调整脚接触Marker

- **WHEN** 作者在Pose Graph Sync面板查看WalkLoop与RunLoop Marker
- **THEN** 面板 MUST保持只读并提供Open Source Timeline
- **AND** Timeline Editor MUST成为移动Marker的唯一正式入口
- **AND** Pose Graph与Profile MUST不保存Marker副本

#### Scenario: 从Inertialization References查看请求来源

- **WHEN** 作者选择Action Inertialization节点并查看References
- **THEN** 工作区 MUST只读显示compiled Action BlendStack route与exact rule owner
- **AND** 修改Blend Logic MUST导航到Action Blend Policy正式入口
- **AND** Inertialization节点 MUST不保存第二份endpoint rule

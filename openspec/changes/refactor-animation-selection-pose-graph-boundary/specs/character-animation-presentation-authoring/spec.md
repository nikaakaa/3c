## MODIFIED Requirements

### Requirement: Blend Policy必须属于显式Blend Stack节点

`CharacterAnimationPresentationProfile` MUST通过Pose Graph中的显式Blend Stack节点引用`CharacterAnimationBlendPolicy`。Policy MUST保存Stack容量、Stored Pose policy、canonical curve、Blend Profile、authoring default和exact override；Compiler MUST只为引用该Policy的节点物化可达endpoint完整table。Timeline、BTSMTL Graph、Program、SelectedPosePlayer、Equipment Feature与Prefab MUST不保存第二份transition表。Animancer source backend MUST只复用和采样source playable，不得调用TransitionLibrary、AnimancerLayer.Play、StartFade或FadeGroup决定转场。

#### Scenario: 播放目标producer

- **WHEN** selected producer收到第一份合法sample
- **THEN** 对应显式BlendStack MUST按Projection中的exact source-target transition开始时间混合
- **AND** Animancer source backend MUST只提供该source pose sample

#### Scenario: FullBodyAction淡出到Empty

- **WHEN** FullBodyAction channel提交None且当前action source仍有贡献
- **THEN** FullBodyAction BlendStack MUST使用节点Policy中的source-to-Empty transition连续淡出
- **AND** 系统 MUST不从TransitionLibrary、Animancer state或默认duration补值

### Requirement: CharacterAnimationPresentationProfile Inspector必须是唯一Presentation配置入口

Profile Inspector MUST唯一编辑Pose Graph、Blend Policy、Inertialization Policy、Rig Definition、producer source binding、Foot Analysis Mode与Analysis Source GUID。Pose Graph Editor MUST编辑Selection Input、MarkerSync、SelectedPosePlayer、Blend Stack、Inertialization、Blend/Layered/Additive、Parameter、ModifyBone、FootPlacement和Output拓扑；Timeline Editor继续唯一编辑producer-local Clip、SyncGroup、Topology、SyncRole、Point Marker、Window与registered Curve。MarkerSync节点 MUST不复制Track marker数据；Profile、Timeline、Definition或Prefab MUST不保存Pose Graph节点配置副本。

#### Scenario: 从Profile打开Timeline Analysis

- **WHEN** 作者从精确Profile上下文打开Timeline并选择AnimationClip
- **THEN** Analysis provider MAY把该Profile的Source作为显式初始选择
- **AND** Timeline资产 MUST不因打开或分析而变脏

#### Scenario: shared Timeline用于不同角色

- **WHEN** 两个Profile使用同一shared Timeline但不同Analysis Source
- **THEN** 各自 MUST生成不同artifact identity与Projection
- **AND** shared Timeline MUST不保存任一角色的Analysis Source

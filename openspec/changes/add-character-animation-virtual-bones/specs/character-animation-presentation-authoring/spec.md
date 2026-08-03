## MODIFIED Requirements

### Requirement: CharacterAnimationPresentationProfile Inspector必须是唯一Presentation配置入口

Profile Inspector MUST唯一编辑Pose Graph、Pose source binding、有限Action producer binding、node-local Blend/Inertialization Policy、Rig Definition、Foot Analysis Mode与Analysis Source GUID。Rig Inspector MUST是Physical Bone与Virtual Bone的唯一写入口：Physical Bone保持parent-first稳定identity，Virtual Bone保存稳定VirtualBoneId、DisplayName以及同一Rig内不同Source/Target Physical BoneId。Pose Graph Details MUST只引用Rig中已有Pose BoneId，不得复制Virtual Bone定义或提供第二写入口。Rig、Mask、Profile或TwoBoneIK authoring变化 MUST只把Projection标记为Dirty、Invalid或Stale；选择资产、修改字段、窗口恢复与Preview target变化 MUST不自动Build。

#### Scenario: 作者新增Virtual Bone

- **WHEN** 作者从Profile导航到Rig Inspector并新增合法Virtual Bone
- **THEN** Rig Inspector MUST保存稳定identity与Source/Target Physical Bone关系
- **AND** 引用旧Rig revision的Mask、Profile与Projection MUST显示Invalid或Stale
- **AND** 系统 MUST不因该编辑自动发布Projection

#### Scenario: Pose Graph配置TwoBoneIK

- **WHEN** 作者在TwoBoneIK Details选择Effector或Joint Target reference
- **THEN** picker MUST只列出当前Rig的合法Physical或Virtual Pose Bone
- **AND** Details MUST不创建Virtual Bone、隐藏Transform target或Rig字段副本

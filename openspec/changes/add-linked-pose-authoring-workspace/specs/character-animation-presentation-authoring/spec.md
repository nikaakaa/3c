# character-animation-presentation-authoring Specification

## MODIFIED Requirements

### Requirement: CharacterAnimationPresentationProfile Inspector必须是唯一Presentation配置入口

`CharacterAnimationPresentationProfile` 的正式Inspector surface MUST作为Definition-scoped Animation Workspace的Profile Details page装配，并成为Profile-owned Pose source binding、Blend Policy、Inertialization Policy、Rig Definition、有限Action producer source binding、Foot Analysis、Linked Pose Group、selector与Implementation关系的唯一人工配置表面。Standalone Unity Custom Inspector MUST只提供轻量摘要、只读诊断与`Open in Animation Workspace`入口，不得形成第二写路径。Timeline Editor继续唯一编辑Action producer-local Clip、Window、Motion、Cue与Timeline marker；持续Locomotion Sequence source的Clip、marker和analysis继续归属Profile binding。所有人工字段 MUST使用类型受限对象选择器和可读业务名，不得要求输入Source Id、Provider Id、Linked Pose identity、GUID、local file id、revision或hash。

#### Scenario: 从Profile资产进入Linked Pose配置

- **WHEN** 作者在Standalone Profile Inspector选择Open in Animation Workspace
- **THEN** 工作区 MUST恢复精确Definition/Profile上下文并在同一Navigator与Details显示Linked Pose Group
- **AND** Standalone Inspector MUST不复制Group、selector或mapping编辑字段

#### Scenario: shared Timeline用于不同角色

- **WHEN** 两个Profile使用同一shared Timeline但不同Analysis Source
- **THEN** 各自 MUST生成不同artifact identity与Projection
- **AND** shared Timeline MUST不保存任一角色的Analysis Source

#### Scenario: 修改Linked Pose Equipment mapping

- **WHEN** 作者在Animation Workspace的Profile/Group Details修改Equipment到Implementation的精确映射
- **THEN** 工作区 MUST通过正式typed Presentation Mutation修改唯一Profile owner并使Projection Stale
- **AND** MUST不自动Build或要求转到selector Asset Inspector

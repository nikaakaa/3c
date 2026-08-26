## ADDED Requirements

### Requirement: Foot Motion数据基础阶段不得改变Runtime动画行为

Definition Build MUST把新增22条Foot Motion Data Curve计入AnimationClip Registered Curve Hash、dependency与Editor质量诊断，但在本change内 MUST不把它们降低为Presentation Projection Runtime payload、Pose Parameter、Foot State输入、Goal、Pelvis或FBBIK配置。

Player Runtime MUST继续使用归档基线的Foot Placement数据与公式，不得读取`Clip Curves`接收器字段、AnimationClip EditorCurve、Library Artifact或未消费Projection字段。后续行为change只有在本change归档后 MAY按独立小步新增正式消费者。

#### Scenario: 新曲线Apply后重建当前产品

- **WHEN** Corin AnimationClip已经Apply合法Foot Motion Curve组并执行当前Definition Build
- **THEN** Projection dependency revision MUST因Registered Curve Hash变化
- **AND** 当前Runtime Foot Goal、状态、Pelvis和FBBIK行为 MUST保持基线逐帧语义

#### Scenario: Player中不存在Editor Artifact

- **WHEN** Player只包含已发布Program与Projection
- **THEN** Player MUST不需要Library Foot Analysis Artifact或`Clip Curves`组件实例
- **AND** 新Foot Motion Curve在没有正式消费者时 MUST不占用Runtime payload

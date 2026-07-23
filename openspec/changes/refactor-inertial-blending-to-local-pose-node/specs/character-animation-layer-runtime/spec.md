## MODIFIED Requirements

### Requirement: 显式动画Player节点必须拥有各自时间连续性

`SelectedPosePlayer` MUST拥有当前source与Discontinuity事实；`BlendStack` MUST拥有多source CrossFade、Stored Pose与release；`Inertialization` MUST拥有单Pose完成history、residual与rebase。三者 MUST不复制状态，Animancer MUST只采样source。

#### Scenario: 删除Inertialization节点

- **WHEN** 图只保留SelectedPosePlayer
- **THEN** source identity变化 MUST显示明确硬切
- **AND** BlendStack或Animancer MUST不补偿该连续性


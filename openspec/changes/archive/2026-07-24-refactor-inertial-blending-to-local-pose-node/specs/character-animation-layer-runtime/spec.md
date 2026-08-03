## MODIFIED Requirements

### Requirement: 显式动画Player节点必须拥有各自时间连续性

`SelectedPosePlayer` MUST只保持当前Selection并输出typed discontinuity；没有下游Inertialization时允许明确硬切。`BlendStack` MUST只对连接到该节点的Selection拥有entry、CrossFade clock、Stored Pose、Per-Bone Blend Profile和source retirement。`Inertialization` MUST独占单Pose residual与rebase。三者 MUST不复制状态。项目 MUST不为每AnimationChannel、旧PoseSlot或Graph branch自动创建隐藏Stack或Inertialization；LayeredBoneBlend、Additive、FootPlacement与OutputPose MUST不重建Player transition。Animancer source backend MUST只创建或复用source playable并把source capture job安装到同一PlayableGraph。

#### Scenario: producer 包含多个 clip

- **WHEN** 同一Timeline producer采样到多个重叠clip
- **THEN** source backend MUST在同一source playable内表达producer内部clip weights
- **AND** 显式BlendStack MUST负责该source与其它source之间的transition

#### Scenario: transition期间再次切换

- **WHEN** 当前BlendStack仍保留A时逻辑选择C
- **THEN** Stack MUST从唯一正式entry/Stored状态push C
- **AND** PlaybackRuntime MUST不建立第二个handoff stack或恢复中间逻辑状态

#### Scenario: slot概览权重为零但骨骼仍有贡献

- **WHEN** Stack完成帧的OutputWeight为零但dense per-bone output仍至少有一个非零权重
- **THEN** Player availability MUST保持Pose
- **AND** Pose Graph MUST按dense per-bone weight执行空间合成
- **AND** MUST不使用OutputWeight裁掉仍然有效的骨骼姿势

#### Scenario: 删除Inertialization节点

- **WHEN** 图只保留SelectedPosePlayer
- **THEN** source identity变化 MUST显示明确硬切
- **AND** BlendStack或Animancer MUST不补偿该连续性

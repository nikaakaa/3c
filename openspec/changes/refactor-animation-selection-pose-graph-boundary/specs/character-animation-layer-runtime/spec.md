## MODIFIED Requirements

### Requirement: 动画通道输入必须是已解析Animation Selection与正式参数页

Program Finalize MUST为每个AnimationChannelId最多提交一个已解析Gameplay winner；Presentation sampler MUST按committed Timeline time与visual interpolation生成含raw visual time和marker binding identity的版本化Animation Selection与typed Parameter page。Selection MUST由Pose Graph显式路径消费；只有连接在Selection Input与Player之间的MarkerSync节点 MAY解析effective time。Lifecycle、Projection、Player与Pose Graph MUST不重新仲裁同channel候选，也 MUST不把transition、Bone Mask或IK状态写回Program。

#### Scenario: Base收到唯一Selection

- **WHEN** 同一逻辑Tick中Locomotion与Action都尝试写BaseLocomotion channel
- **THEN** Program MUST只提交唯一winner Selection
- **AND** Pose Graph MUST只消费该committed结果

#### Scenario: 图中未连接MarkerSync

- **WHEN** AnimationSelectionInput直接连接SelectedPosePlayer或BlendStack
- **THEN** Player MUST使用Selection raw visual time
- **AND** Timeline、Lifecycle与Playback Runtime MUST不执行隐藏marker handoff

### Requirement: 显式动画Player节点必须拥有各自时间连续性

`SelectedPosePlayer` MUST只保持当前Selection并输出typed discontinuity；没有下游Inertialization时允许明确硬切。`BlendStack` MUST只对连接到该节点的Selection拥有entry、CrossFade clock、Stored Pose、Per-Bone Blend Profile和source retirement。`Inertialization` MUST独占单Pose residual与rebase。项目 MUST不为每AnimationChannel、旧PoseSlot或Graph branch自动创建隐藏Stack或Inertialization；LayeredBoneBlend、Additive、FootPlacement与OutputPose MUST不重建Player transition。Animancer source backend MUST只创建或复用source playable并把source capture job安装到同一PlayableGraph。

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

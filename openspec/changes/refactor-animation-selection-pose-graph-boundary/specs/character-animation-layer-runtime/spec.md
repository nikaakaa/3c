## RENAMED Requirements

- FROM: `### Requirement: PoseSlot Blend Stack必须是transition权威`
- TO: `### Requirement: 显式动画Player节点必须拥有各自时间连续性`

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

`SelectedPosePlayer` MUST只保持当前Selection并输出typed discontinuity；没有下游Inertialization时允许明确硬切。`BlendStack` MUST只对连接到该节点的Selection拥有entry、CrossFade clock、Stored Pose、Per-Bone Blend Profile和source retirement。`Inertialization` MUST独占单Pose residual与rebase。项目 MUST不为每AnimationChannel、旧PoseSlot或Graph branch自动创建隐藏Stack或Inertialization；LayeredBoneBlend、Additive、FootPlacement与OutputPose MUST不重建Player transition。

#### Scenario: transition期间再次切换

- **WHEN** 连接到BlendStack的Selection在A到B未完成时切换到C
- **THEN** 该BlendStack节点 MUST连续处理A、B、C历史
- **AND** 其它Player节点状态 MUST不受影响

#### Scenario: Action使用直接Player

- **WHEN** Action Selection连接SelectedPosePlayer
- **THEN** Runtime MUST只采样当前Action source
- **AND** MUST不要求Action配置Stack容量或完整transition table

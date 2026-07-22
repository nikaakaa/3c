## RENAMED Requirements

- FROM: `### Requirement: 逻辑层必须为每个动画层提交唯一播放选择`
- TO: `### Requirement: 逻辑层必须为每个动画通道提交唯一播放选择`
- FROM: `### Requirement: 动画层预览只读取调试 Snapshot`
- TO: `### Requirement: 动画管线预览只读取调试 Snapshot`

## MODIFIED Requirements

### Requirement: CharacterSimulationPresentationRuntime 是 Unity 动画应用边界

SimulationCommitter与唯一`CharacterSimulationPresentationRuntime`协调器 MUST共同构成Unity animation application boundary。协调器 MUST通过Projection校验producer与AnimationChannelId，并将command唯一转发给`CharacterAnimationPlaybackRuntime -> AnimationPlaybackLifecycle -> PoseSlot Blend Stack native job -> source capture -> Pose Graph native job -> final stream writer`。`AnimationPosePlayableGraphRuntime` MUST把这些job安装在同一PlayableGraph并只Evaluate一次。每个PresentationFrame MUST只求值一次最终Animation Pose，再把lease-protected `FinalAnimationPoseFrame`交给唯一Pose Post Process Pass。Program Runtime、Execution Backend、Pipeline Pass、WorldSolver、Session Source与Network adapter MUST不引用Animancer、Blend Stack、Pose Graph job、Animation Jobs或Pose Post Process实现，也 MUST不直接播放、合成或修改动画姿势。

#### Scenario: 同帧提交Locomotion与Attack

- **WHEN** Committer发布BaseLocomotion和FullBodyAction两个channel的最终command
- **THEN** 协调器 MUST分别更新两个Lifecycle与PoseSlot Stack
- **AND** MUST由唯一Pose Graph合成两个slot并写回最终Animator Pose

#### Scenario: 最终动画pose完成

- **WHEN** 全部PoseSlot native job与Pose Graph native job完成本帧求值
- **THEN** 唯一Pose Post Process Pass MAY消费FinalAnimationPoseFrame
- **AND** MUST不建立另一份selection、Stack、Bone Mask或curve resolver

#### Scenario: Rollback替换action command

- **WHEN** rollback EventId journal替换已经显示的FullBodyAction command
- **THEN** 唯一Runtime MUST从当前action slot pose提交新的Stack request
- **AND** BaseLocomotion slot、Pose Graph topology与Gameplay state MUST不被复制或回卷

### Requirement: 动画管线预览只读取调试 Snapshot

系统 MUST从正式AnimationPlaybackLifecycle、每slot Blend Stack、Animancer source backend、PoseSlot native job与Pose Graph native job导出只读`AnimationPlaybackFrameSnapshot`。Snapshot MUST显示AnimationChannelId、PoseSlotId、selection、Pending、entry/Stored/Inertial、source time、PoseNodeId、parameter、final contribution与pose completion，且 MUST不参与Gameplay决策或最终播放。Timeline Preview MUST复用正式Projection、Stack、Rig、Pose Program和同一PlayableGraph运行时，不得只播单clip或建立简化compositor。

#### Scenario: Preview两个channel

- **WHEN** preview session同时采样BaseLocomotion和FullBodyAction producer
- **THEN** snapshot MUST显示两个channel到slot的binding与最终OutputPose
- **AND** Editor MUST只读正式preview runtime结果

#### Scenario: 关闭调试历史

- **WHEN** 项目关闭snapshot历史采集
- **THEN** Runtime MAY不保存历史frame
- **AND** Stack与Pose Graph evaluation MUST不依赖snapshot

### Requirement: 逻辑层必须为每个动画通道提交唯一播放选择

Program Finalize MUST根据State、Action、interruption与Timeline ownership为每个`AnimationChannelId`最多输出一个selected producer/playback command。不同channel MAY在同一Tick各自输出一个command。Committer与Presentation MUST不重新仲裁同一channel的两个候选；Pose Graph只组合channel绑定的PoseSlotFrame，不得成为逻辑winner选择器。

#### Scenario: 同一Locomotion channel冲突

- **WHEN** RunLoop与MovingTurn在同一Tick都声称BaseLocomotion且ownership未产生唯一结果
- **THEN** Finalize MUST报告冲突
- **AND** Presentation MUST不按Pose Graph连线、mask或slot order选winner

#### Scenario: Locomotion与Action并行输出

- **WHEN** BaseLocomotion选择RunLoop且FullBodyAction选择Attack1
- **THEN** Finalize MUST提交两个独立channel command
- **AND** Pose Graph MAY按全身mask显示Attack1而不停止RunLoop逻辑selection

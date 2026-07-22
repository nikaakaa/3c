## RENAMED Requirements

- FROM: `### Requirement: PresentationFrame 必须输出逐层最终动画结果`
- TO: `### Requirement: PresentationFrame 必须输出完整动画合成结果`

## MODIFIED Requirements

### Requirement: Timeline 和动画 tick 权威归属 pipeline

Gameplay Timeline logic time MUST归Program/CharacterSimulationState并按SimulationTick推进；animation visual sampling、每PoseSlot Blend Stack Fade Clock、Animancer source sampling与Pose Graph evaluation MUST归PresentationFrame。Pipeline Runtime与Presentation MUST只通过committed AnimationChannel producer/playback identity连接，MUST不共享mutable clock，Program MUST不读取PoseSlot、Stack或Pose Graph时间。

#### Scenario: 无新Logic Tick的RenderFrame

- **WHEN** PresentationFrame到达但没有新SimulationTick
- **THEN** animation source、slot transition与Pose Graph MAY继续推进
- **AND** Timeline Gameplay state MUST不改变

### Requirement: PresentationFrame 必须输出完整动画合成结果

Presentation diagnostics snapshot MUST按AnimationChannelId和PoseSlotId保存selection、PendingFirstSample、Stack entry、Stored/Inertial、visual sample time，并按PoseNodeId保存availability、参数、骨骼贡献和最终OutputPose completion。Snapshot MUST能追踪`AnimationChannel -> PoseSlot -> Stack -> PoseGraph -> Final Pose`，且只用于diagnostics，不得进入SimulationWorldSnapshot、Program决策或Runtime composition。

#### Scenario: Action等待第一Sample

- **WHEN** FullBodyAction target已提交但首个sample未到
- **THEN** snapshot MUST显示FullBodyAction channel PendingFirstSample与FullBodyActionSlot当前availability
- **AND** MUST同时显示BaseLocomotionSlot和最终OutputPose仍来自哪里

### Requirement: Program Finalize 必须提交逻辑侧唯一动画选择

Program Finalize MUST在State、Action、interruption与Timeline request处理后为每个`AnimationChannelId`最多产生一个selected producer/playback command。不同Animation Channel可以同时产生command。Committer、Projection、Blend Stack与Pose Graph MUST不重新仲裁同一channel候选，Program MUST不读取PoseSlotId、Bone Mask或Pose Graph topology决定winner。

#### Scenario: 同channel所有权冲突

- **WHEN** Program无法为BaseLocomotion channel产生唯一选择
- **THEN** 当前Tick MUST报告明确冲突
- **AND** Presentation MUST不选择默认winner

#### Scenario: 两个channel合法并行

- **WHEN** BaseLocomotion选择Run且FullBodyAction选择Dodge
- **THEN** Program MUST提交两个独立command
- **AND** Pose Graph MUST只在PresentationFrame合成它们

### Requirement: PresentationFrame 必须原子提交动画播放生命周期

PresentationFrame MUST按固定顺序读取Committer queue、先推进既有每slot Blend Stack clock、按channel解析并push selection、更新AnimationPlaybackLifecycle、为全部source安装capture job、安装固定slot job、安装Pose Graph/final writer job、在同一PlayableGraph只Evaluate一次、按exact completion完成Stack、发布lease-protected `FinalAnimationPoseFrame`、执行唯一Pose Post Process、推进Camera、退休source并acknowledge batch。该阶段 MUST不执行Program、TreeClip、Motion、Action、Effect或WorldSolver，也 MUST不产生Gameplay事实、网络输出、第二套Stack算法、第二次Evaluate或第二次VisualRoot写入。

#### Scenario: 两个selection与首样本同批

- **WHEN** BaseLocomotion与FullBodyAction的target selection和合法sample同批到达
- **THEN** 两个Lifecycle与Stack MUST在同一frame plan中原子提交
- **AND** Pose Post Process MUST只观察Pose Graph生成的最终pose

#### Scenario: RequireOutput slot尚未就绪

- **WHEN** BaseLocomotionSlot没有Selected/Retained输出且仍等待首个合法pose request
- **THEN** Pose Graph completion MUST明确Invalid
- **AND** Pose Post Process MUST不对残留骨骼求解

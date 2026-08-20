## MODIFIED Requirements

### Requirement: Timeline 和动画 tick 权威归属 pipeline

Gameplay Timeline logic time MUST归Program与CharacterSimulationState并按SimulationTick推进；每个有限Action Sample command MUST只表达committed playback identity、raw visual time、cycle与time scale锚点，MUST不表达最终骨骼Pose或要求Player只在SimulationTick推进。有限Action projected visual time、PoseStateMachine、Clip/BlendSpace/MM source raw clock、source-local Phase endpoint、AnimationSlot、显式Player、Animancer source sampling与Pose Plan evaluation MUST归PresentationFrame。Pipeline Runtime MUST通过committed Body/Intent构造Presentation Fact，并通过有限Action producer/playback identity连接Timeline与Slot。Program MUST不读取PoseState、ClipPlayer、Phase relation、Slot或Pose Graph时间，Presentation MUST不推进Gameplay Timeline。系统 MUST不提供让同一Timeline在Gameplay-owned与Presentation-owned时钟之间切换的运行模式；有限Action与持续Pose source MUST由其正式authoring owner进入唯一对应链路。

#### Scenario: 无新 Logic Tick 的 RenderFrame

- **WHEN** PresentationFrame 到达但没有新 SimulationTick
- **THEN** Action visual time projector、PoseState source raw clock、Phase endpoint、Slot transition、Player与Pose Graph MAY按presentation delta继续推进
- **AND** Timeline Gameplay state与Action lifecycle MUST不改变

### Requirement: PresentationFrame必须输出完整最终Pose Plan结果

PresentationFrame MUST消费committed Body/Intent、构造typed Presentation Fact，并消费完整有限Action playback batch与Parameter page；随后按Projection编译的ordered stage table执行PoseState selection、State source demand、source-local Phase resolve、source capture、Action playback、AnimationSlot、Transition Routing、Local Pose composition、显式Local/Component转换、Component Pose骨骼控制、world-aware FootPlacement规划与pelvis输出、typed双腿targets、pure pose LegIK、后续Pose stage与FinalPublication。只有唯一OutputPose及全部必需stage完成后才可由唯一final writer发布`FinalAnimationPoseFrame`并推进Camera；任一Fact、source、Phase endpoint、Player、Slot、转换、Pose operation、world query、Planner、targets validation或LegIK solver失败 MUST阻止部分最终结果发布，不得沿用上一帧、只发布pelvis Pose或绕过节点。

#### Scenario: FootPlacement targets与LegIK Pose不匹配

- **WHEN** 同帧targets CompletionIdentity或Rig revision与LegIK Component Pose输入不一致
- **THEN** PresentationFrame MUST阻断LegIK、后续stage和FinalPublication
- **AND** MUST不使用上一次targets或按节点顺序猜测配对

#### Scenario: 完整Foot Placement链成功

- **WHEN** FootPlacement发布合法pelvis Pose与targets且LegIK完成左右腿求解
- **THEN** FinalAnimationPoseFrame MUST包含LegIK输出及全部后续Pose操作
- **AND** Runtime MUST不保留第二Foot Placement或图外Leg IK结果

#### Scenario: Action等待第一Sample

- **WHEN** Program已经选择Action但Presentation尚无合法playback sample
- **THEN** AnimationSlot MUST按compiled pending/availability policy处理
- **AND** Locomotion PoseState MUST继续来自同帧Fact而不是历史BaseLocomotion selection

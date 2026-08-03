## ADDED Requirements

### Requirement: Motion Matching必须是source-neutral Animation Selection provider

当active PoseState声明Motion Matching source时，`CharacterSimulationPresentationRuntime` MUST按state relevance执行trajectory/query/search与selection lifecycle，并把结果降低为State内部正式Selection。有限Action Timeline与MM source MUST进入同一个编译Pose Graph；图上的显式节点决定Player硬切、局部Inertialization或BlendStack CrossFade。MM MUST不建立私有播放器、crossfade、惯性器、PlayableGraph、Pose Graph或Post Process。

#### Scenario: Locomotion PoseState使用MM而Action使用Timeline

- **WHEN** 同帧Locomotion PoseState MM和FullBodyAction Timeline均有合法输出
- **THEN** MM MUST进入State内部Player，Action MUST进入AnimationSlot
- **AND** 唯一Pose Plan MUST按显式Player、Slot与composition节点合成最终pose

#### Scenario: MM query无合法candidate

- **WHEN** active PoseState MM发布typed Invalid
- **THEN** Required State source MUST沿统一动画管线报告Invalid
- **AND** Presentation MUST不调用旧Timeline或隐藏Idle维持输出

### Requirement: PresentationFrame必须在绑定Pose节点求值后更新MM Pose History

PresentationFrame MUST先以旧history完成MM query与source selection，再执行编译Pose Plan，随后只把本帧绑定history source PoseNode的结果追加到MM Pose History，最后完成FootPlacement阶段与Camera。MM MUST不以本帧尚未完成的pose构造循环query。

#### Scenario: 正常表现帧

- **WHEN** 绑定的history source PoseNode完成本帧Pose Value
- **THEN** Runtime MUST在该节点完成后追加MM Pose History
- **AND** 下一帧query MAY消费该sample

#### Scenario: History source节点Invalid

- **WHEN** 本帧绑定history source节点没有合法Pose
- **THEN** Runtime MUST不追加伪造history
- **AND** MM diagnostics MUST记录history gap

### Requirement: Animation Root Motion不得通过MM进入Gameplay应用边界

MM Artifact MAY保存Clip root trajectory用于search，但Unity animation application boundary MUST保持Body/VisualRoot权威分离。Animancer source backend、任何Player节点、Pose Graph与MM Runtime MUST不把selected Clip的deltaPosition或deltaRotation提交给Simulation、WorldSolver、CharacterController或VisualRoot。

#### Scenario: MM采样Root Motion Clip

- **WHEN** selected Clip包含root displacement
- **THEN** source backend MUST只提供root-locked pose sample
- **AND** Character Body world pose MUST继续来自Committed/Selected stream

## ADDED Requirements

### Requirement: Motion Matching必须位于source-neutral Pose Request之前

当已提交AnimationChannel producer绑定Motion Matching Pose Source时，`CharacterSimulationPresentationRuntime` MUST在该Channel的PoseSlot Stack frame plan之前执行trajectory/query/search与selection lifecycle，并把结果降低为正式`ResolvedAnimationPoseRequest`。Timeline与MM request MUST进入同一个Animancer source sampling backend、per-slot Blend Stack、Pose Graph与Foot Placement链。MM MUST不建立私有crossfade、PlayableGraph、Pose Graph或Post Process。

#### Scenario: BaseLocomotion使用MM而Action使用Timeline

- **WHEN** 同帧BaseLocomotion MM和FullBodyAction Timeline均有合法输出
- **THEN** 两者 MUST分别生成source-neutral request并进入各自PoseSlot Stack
- **AND** 唯一Pose Graph MUST合成最终pose

#### Scenario: MM query无合法candidate

- **WHEN** BaseLocomotion MM发布typed Invalid
- **THEN** RequireOutput Slot MUST沿统一动画管线报告Invalid
- **AND** Presentation MUST不调用旧Timeline或隐藏Idle维持输出

### Requirement: PresentationFrame必须在Base Slot求值后更新MM Pose History

PresentationFrame MUST先以旧history完成MM query与source selection，再求值全部PoseSlotFrame和最终Pose Graph，随后只把本帧BaseLocomotionSlot结果追加到MM Pose History，最后执行Foot Placement与Camera。MM MUST不以本帧尚未完成的pose构造循环query。

#### Scenario: 正常表现帧

- **WHEN** BaseLocomotionSlot完成本帧PoseSlotFrame
- **THEN** Runtime MUST在Foot Placement前追加MM Pose History
- **AND** 下一帧query MAY消费该sample

#### Scenario: Base Slot Invalid

- **WHEN** 本帧BaseLocomotionSlot没有合法Pose
- **THEN** Runtime MUST不追加伪造history
- **AND** MM diagnostics MUST记录history gap

### Requirement: Animation Root Motion不得通过MM进入Gameplay应用边界

MM Artifact MAY保存Clip root trajectory用于search，但Unity animation application boundary MUST保持Body/VisualRoot权威分离。Animancer source backend、Blend Stack、Pose Graph与MM Runtime MUST不把selected Clip的deltaPosition或deltaRotation提交给Simulation、WorldSolver、CharacterController或VisualRoot。

#### Scenario: MM采样Root Motion Clip

- **WHEN** selected Clip包含root displacement
- **THEN** source backend MUST只提供root-locked pose sample
- **AND** Character Body world pose MUST继续来自Committed/Selected stream

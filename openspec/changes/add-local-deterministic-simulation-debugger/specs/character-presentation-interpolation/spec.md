# character-presentation-interpolation Specification

## ADDED Requirements

### Requirement: Local Debug Replay 表现必须由 committed stream 重建

Local Fixed debug replay MUST 不保存或恢复 Presentation runtime state。Replay 成功提交后，Presentation MUST 从 committed Body、Intent、Action EventId、playback identity 和 stream reset/replacement 重建可见状态。Presentation MAY 在 Debug Presentation Clock 的 `LivePresentation` 或 `LogicLockedPresentation` 下采样该分支，但 MUST NOT 将 visual pose、骨骼 Pose、Animancer state、PoseState workspace、Slot weight 或 BlendStack runtime 写入 gameplay snapshot、replay artifact 或 hash。

#### Scenario: Replay 改变攻击选择

- **WHEN** Local Fixed debug replay 将 Tick T 的 Attack2 EventId 替换为 Attack1 EventId
- **THEN** Presentation MUST 通过 committed output replacement 重新建立 FullBodyAction selection
- **AND** PoseStateMachine MUST 从新的 Body/Intent fact 求值
- **AND** replay snapshot MUST 不包含旧 Attack2 的骨骼 Pose

#### Scenario: LogicLocked 手动步进观察

- **WHEN** 作者在 LogicLockedPresentation 下执行 StepOne
- **THEN** Presentation MUST 只消费该 Tick 成功提交后的 committed sample
- **AND** 显式Player时间 MAY 按 fixed presentation pulse 推进
- **AND** Presentation MUST 不推进额外 gameplay Tick

## RENAMED Requirements

- FROM: `### Requirement: Equipment动画必须继续通过Timeline producer提交`
- TO: `### Requirement: Equipment有限Action与持续Pose必须使用不同正式入口`

## MODIFIED Requirements

### Requirement: Equipment有限Action与持续Pose必须使用不同正式入口

Feature graph 中的有限动画 MUST 由正式 Timeline AnimationTrack 产生 typed producer command，并经过 Presentation Queue、Animation Playback Lifecycle、ActionPlaybackInput、AnimationSlot 与 Pose Graph Plan。Equipment Host、Action runtime 与 Visual runtime MUST 不直接调用 Animancer、Animator.Play、CrossFade 或修改 Player/Graph weight。

持续装备姿态、移动 Pose 逻辑与 Hand Goals MUST 由 Animation Presentation Profile 中的 Equipment selector 把 committed `EquipmentSlotId + EquipmentId` 映射为通用 Linked Pose selection frame，再由已编译 Implementation 提供；它们 MUST 不伪装成无限 Timeline Action，也 MUST 不由 Equipment Visual 驱动。Linked Pose 核心 MUST 不认识 Equipment 类型。

#### Scenario: Sawblade 攻击播放

- **WHEN** Sawblade Route 进入 Attack1 Timeline
- **THEN** Timeline MUST 提交已绑定的有限 producer command
- **AND** Animation Playback Lifecycle 与 root AnimationSlot MUST 拥有播放、交接和释放状态

#### Scenario: Persistent 持枪姿态

- **WHEN** committed Equipment 状态要求上半身持枪循环
- **THEN** Equipment selector MUST 选择正式 Implementation 并由 Linked Pose Group 输出持续 Pose
- **AND** MUST 不创建无限 Timeline playback 或由 Equipment visual component 驱动 Animator

#### Scenario: 装备同时具有持枪 Pose 与换弹 Action

- **WHEN** Rifle Implementation 正在提供持续 Pose，且 Reload Timeline 提交有限 Action
- **THEN** root AnimationSlot MUST 在同一 Pose Plan 中按正式 Routing 组合 Reload 与持续 Pose
- **AND** Linked Implementation MUST 不创建第二 Slot 或直接消费 Timeline operation

#### Scenario: Equipment 槽为空

- **WHEN** committed Equipment 状态表示 selector 负责的 Slot 为空
- **THEN** selector MUST 映射到显式 Empty Implementation
- **AND** Equipment Presentation MUST 不提供 default weapon pose 或 visual fallback

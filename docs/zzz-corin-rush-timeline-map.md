# ZZZ Corin RushAttack Timeline 对照

本文登记 RushAttack 8 个状态 Timeline 的正式资产、身份、命中 cue 和状态边界。帧号遵循两个口径：

- **SourceFrame**：ZZZ 状态本地 1 基帧，文档、FSM 条件和 dump 对账使用。
- **TimelineFrame**：正式 Timeline clip 0 基帧，等于 `SourceFrame - 1`。
- 每个 Section 从 frame=0 开始，因此 runtime `LocalFrame = TimelineClipStartFrame - SectionFrame + 1 = SourceFrame`。

## 资产与身份合同

| StateId / Section Name | BranchId | Timeline 资产 | TimelineId | SectionId | Source TotalFrames |
|---|---|---|---|---|---:|
| `Attack_Rush` | `Rush` | `Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushTimeline.asset` | `606351d5-2e51-5730-3083-9e61df7814be` | `86b751b3-24d8-fc3c-3f0c-1d6dfa88f07e` | 70 |
| `Attack_Rush_Explode` | `Rush` | `Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushExplodeTimeline.asset` | `7bd96c6f-4480-8426-1abb-438b7c99cec0` | `866672da-8a24-aa7c-d040-bc8f3df58e6a` | 70 |
| `Attack_Rush_End` | `Rush` | `Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEndTimeline.asset` | `8c852140-8ed0-ae2b-b3b3-654743e1e6ee` | `bcd0c997-0211-5653-03de-2e1f13f88e29` | 80 |
| `Attack_Rush_Enhance` | `Rush_Enhance` | `Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceTimeline.asset` | `d089a439-0237-33f1-aa72-ac917567c225` | `017fe590-4234-5099-420e-794651345f58` | 44 |
| `Attack_Rush_Enhance_Loop` | `Rush_Enhance` | `Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceLoopTimeline.asset` | `e874eb9c-8e97-0f49-0971-ee62ea4f029b` | `a4fd678b-681d-0c35-65f8-48af5017c8f9` | 86 |
| `Attack_Rush_Enhance_End` | `Rush_Enhance` | `Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceEndTimeline.asset` | `6c31511d-ed9a-4ae4-0e07-fa9a9e3b19ab` | `43a899a3-042c-644d-130e-673ca2a9d5d3` | 70 |
| `Attack_Rush_Enhance_Explode` | `Rush_Enhance` | `Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceExplodeTimeline.asset` | `a35691a4-9f77-8559-628a-bcfa4adfabfd` | `d33002d9-8bfb-578a-8c7d-c2878aa366c6` | 70 |
| `Attack_Rush_Enhance_Explode_End` | `Rush_Enhance` | `Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEnhanceExplodeEndTimeline.asset` | `dd7314a9-4fcc-26c0-ca7c-1abc4fc6e7ce` | `58bc6ed1-4905-144e-2abc-7a341aabec10` | 118 |

每个 Timeline 包含一条 Presentation `AnimationTrack`，channel 为 `FullBodyAction`，slot 为 `corin.full-body-action`；一条 Logic `ActionCueTrack`；一条 Logic `TreeTrack`。Decision clip 的 graph 全部是 TimelineBody 子资产。

## AttackProperty cue

| StateId | AttackProperty 数量 | SourceFrame 合同 |
|---|---:|---|
| `Attack_Rush` | 32 | frame=8 是 `_01_01`；frame=10,12,14...70 是 `_01_02` |
| `Attack_Rush_Explode` | 1 | frame=1 是 `_02` |
| `Attack_Rush_End` | 0 | 无 |
| `Attack_Rush_Enhance` | 16 | frame=8 是 `Enhance_01_03`；frame=10,12,14...38 是 `Enhance_01_04` |
| `Attack_Rush_Enhance_Loop` | 43 | frame=2,6,10,14,18,22,26 是 `01_01`；frame=4,8,12,16,20,24,28,30 和 frame=32,34...86 是 `01_02` |
| `Attack_Rush_Enhance_End` | 0 | 无 |
| `Attack_Rush_Enhance_Explode` | 1 | frame=1 是 `_02` |
| `Attack_Rush_Enhance_Explode_End` | 0 | 无 |

完整 key 前缀：

- `Corin_Attack_Rush_AttackProperty_01_01`
- `Corin_Attack_Rush_AttackProperty_01_02`
- `Corin_Attack_Rush_AttackProperty_02`
- `Corin_Attack_Rush_Enhance_AttackProperty_01_01`
- `Corin_Attack_Rush_Enhance_AttackProperty_01_02`
- `Corin_Attack_Rush_Enhance_AttackProperty_01_03`
- `Corin_Attack_Rush_Enhance_AttackProperty_01_04`
- `Corin_Attack_Rush_Enhance_AttackProperty_02`

所有 cue 的 `CueType=AttackProperty`；Timeline 只保留原始 `CueId`、播放身份、`StateId`、`LocalFrame` 和 `BranchId`。

## State Boundary Decision

| StateId | SourceFrame | TimelineFrame | 目标 / 语义 | 条件合同 |
|---|---:|---:|---|---|
| `Attack_Rush` | 13 | 12 | `Attack_Rush_Explode` | `HoldAttackA=false` 且 `IsClicking=false` |
| `Attack_Rush` | 23 | 22 | `Attack_Rush_Explode` | `Trigger_SawExplode` |
| `Attack_Rush` | 70 | 69 | `Attack_Rush_Explode` | terminal |
| `Attack_Rush_Explode` | 14 | 13 | `Attack_Normal_04` | `NoHold_Mode4_44` |
| `Attack_Rush_Explode` | 14 | 13 | `Attack_Normal_04` | `PressAttackA_Mode4_44` |
| `Attack_Rush_Explode` | 70 | 69 | `Attack_Rush_End` | terminal |
| `Attack_Rush_End` | 80 | 79 | host route | terminal |
| `Attack_Rush_Enhance` | 35 | 34 | `Attack_Rush_Enhance_Loop` | terminal / exit-time 35/44 |
| `Attack_Rush_Enhance_Loop` | 43 | 42 | `Attack_Rush_Enhance_Explode` | `Trigger_SawExplode` |
| `Attack_Rush_Enhance_Loop` | 81 | 80 | `Attack_Rush_Enhance_Loop` | reenter |
| `Attack_Rush_Enhance_Loop` | 86 | 85 | `Attack_Rush_Enhance_End` | `HoldAttackA=false` 且 `IsClicking=false` |
| `Attack_Rush_Enhance_End` | 40 | 39 | `Attack_Normal_04` | `NoHold_Mode4_40` |
| `Attack_Rush_Enhance_End` | 55 | 54 | `Attack_Normal_04` | `PressAttackA_Mode4_40` |
| `Attack_Rush_Enhance_End` | 55 | 54 | `Attack_Rush_End` | terminal |
| `Attack_Rush_Enhance_Explode` | 55 | 54 | `Attack_Normal_04` | `NoHold_Frame16_Mode4_44` |
| `Attack_Rush_Enhance_Explode` | 55 | 54 | `Attack_Normal_04` | `PressAttackA_Frame16_Mode4_44` |
| `Attack_Rush_Enhance_Explode` | 55 | 54 | `Attack_Rush_Enhance_Explode_End` | terminal |
| `Attack_Rush_Enhance_Explode_End` | 118 | 117 | host route | terminal |

Decision graph 与 clip identity 由 `BtsmtlSkillGraphAssetFactory.StableIdentity` 从以下 seed 生成，并格式化为 dashed GUID：

- `corin.rush.decision.graph:{StateId}:{Target}:{Condition}:{SourceFrame}`
- `corin.rush.decision.clip:{StateId}:{Target}:{Condition}:{SourceFrame}`
- `corin.rush.track.animation:{StateId}`
- `corin.rush.track.cue:{StateId}`
- `corin.rush.track.decision:{StateId}`
- `corin.rush.clip.cue:{StateId}:{CueId}:{SourceFrame}`

## 实现边界

- 本批次只收口 Timeline、Section/BranchId、93 个 AttackProperty cue、状态边界 Decision 和正式内容闭包。
- FSM、Control、Ability admission、Normal04 handoff、Pose producer 接线仍归任务 1 / 任务 3。
- 原 `special_*` 不在 Timeline 内伪造目标；表中 `host route` 只表示宿主 owner 边界。

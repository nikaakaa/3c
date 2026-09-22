# ZZZ Corin RushAttack Timeline 对照

本文登记 RushAttack 三段正式 Timeline 的动画输出和状态窗口。帧号使用 ZZZ 状态本地 1 基 SourceFrame；Timeline clip 时间为 `(SourceFrame - 1) / 60`。

## 资产与身份

| StateId | Timeline 资产 | TimelineId | SectionId | Source TotalFrames |
|---|---|---|---|---:|
| `Attack_Rush` | `Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushTimeline.asset` | `606351d5-2e51-5730-3083-9e61df7814be` | `86b751b3-24d8-fc3c-3f0c-1d6dfa88f07e` | 70 |
| `Attack_Rush_Explode` | `Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushExplodeTimeline.asset` | `e25bc128-d9c4-0448-b708-cf1a0c8b8830` | `866672da-8a24-aa7c-d040-bc8f3df58e6a` | 70 |
| `Attack_Rush_End` | `Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/CorinAttackRushEndTimeline.asset` | `8c852140-8ed0-ae2b-b3b3-654743e1e6ee` | `bcd0c997-0211-5653-03de-2e1f13f88e29` | 80 |

三段 Section 的 `BranchId` 都是 `Rush`。每段各有一条 Presentation `AnimationTrack`，channel 为 `FullBodyAction`，slot 为 `corin.full-body-action`，blend profile 为 `corin.animation-rig.action-blend-profile`。

## 动画资源

| StateId | AnimationClip |
|---|---|
| `Attack_Rush` | `Avatar_Female_Size01_Corin_Ani_Attack_Rush.anim` |
| `Attack_Rush_Explode` | `Avatar_Female_Size01_Corin_Ani_Attack_Rush_Explode.anim` |
| `Attack_Rush_End` | `Avatar_Female_Size01_Corin_Ani_Attack_Rush_End.anim` |

动画轨道只负责表现输出。状态迁移由 Ability FSM 读取 Timeline 投影出的 ActionWindow，不由 Timeline 直接跳转状态。

## ActionWindow

| StateId | SourceFrame | TimelineFrame | WindowType | WindowId | Digest |
|---|---:|---:|---|---|---:|
| `Attack_Rush` | 13 | 12 | `RushRelease` | `RushReleaseOpen` | 8101 |
| `Attack_Rush_Explode` | 14 | 13 | `RushAttackHandoff` | `RushAttackHandoffOpen` | 8102 |

每个窗口由独立 Logic `TreeTrack` 在目标帧运行一次。TreeClip 的 TimelineBody graph 写入一个 Frame scope、Frame lifetime 的布尔声明，并通过 `PipelineBlackboardFactProjectionKind.ActionWindow` 投影给状态机条件。声明 owner 使用实际 graph id，生成后由正式 authoring 诊断校验。

`Attack_Rush_End` 没有窗口 TreeClip；它只播放 80 帧结束动画，完成后由状态机 terminal 条件退出。

## 删除内容

以下五段强化 Rush Timeline、对应 authoring 目录和 Definition catalog 引用已经删除：

- `Attack_Rush_Enhance`
- `Attack_Rush_Enhance_Loop`
- `Attack_Rush_Enhance_End`
- `Attack_Rush_Enhance_Explode`
- `Attack_Rush_Enhance_Explode_End`

旧文档中的 93 个 AttackProperty cue 并不存在于正式 builder 或当前资产中。当前 Timeline 只表达动画与两个 ActionWindow；Effect 依赖登记在 Ability，命中应用需要独立的 GameplayEffect 应用节点，不能由文档假定。

## 稳定身份

Timeline、Section、Track、Clip、graph、declaration、node 和 edge 的身份均由 `BtsmtlSkillGraphAssetFactory.StableIdentity` 从 `corin.rush.*` seed 生成。重复执行 `btsmtl.generate_assets` 必须复用这些身份，并清理不在当前闭包内的旧轨道、Clip、子图和状态。

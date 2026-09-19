# Tasks

## 1. Rush Ability / FSM

- [ ] 1.1 新增独立 `CorinRushAttackGameplayAbilityDefinition` authoring 与 8 个 ZZZ 同名业务状态
- [ ] 1.2 建立 Dodge -> `Attack_Rush` / `Attack_Rush_Enhance` 的正式 activation 入口与 `Badge_S01` 分支
- [ ] 1.3 建立 Rush 内部 8 状态转移、优先级和 Normal04 回归 owner handoff
- [ ] 1.4 将原 `special_*` 中断映射到 Hit / Evade / Aid / Switch 正式 owner；缺失 owner 时显式失败

## 2. Rush Timeline / cue

- [ ] 2.1 建立 8 个状态 Timeline，按 7 个实际 Motion 绑定 AnimationTrack 和 FullBodyAction channel
- [ ] 2.2 为每个 Timeline 建 ZZZ 同名 Section 和 `Rush` / `Rush_Enhance` BranchId
- [ ] 2.3 挂 93 个 AttackProperty Logic ActionCue，保留原始 `CueId` 与状态/分支/本地帧身份
- [ ] 2.4 用 Decision TreeClip 表达 frame=13/23/70、35、43/81/86、40/55/70、55/118 等状态边界
- [ ] 2.5 纳入正式内容闭包与资源引用，重建 Rush Ability 资产

## 3. Pose / 消费接线

- [ ] 3.1 将 8 个 Timeline producer 接入既有 committed FullBodyAction playback / ActionPlaybackInput / Slot 链
- [ ] 3.2 确认 GameplayEffect / Ability 消费 Rush 的 8 类原始 AttackProperty key；Timeline 不携带伤害、碰撞或相机 payload
- [ ] 3.3 同步 `docs/zzz-corin-controller-map.md` 与 Rush cue 对照，记录状态边界、命中帧和 Motion 复用证据

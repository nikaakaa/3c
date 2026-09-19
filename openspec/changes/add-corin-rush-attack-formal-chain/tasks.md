# Tasks

## 1. Rush Ability / FSM

- [ ] 1.1 新增独立 `CorinRushAttackGameplayAbilityDefinition` authoring 与 8 个 ZZZ 同名业务状态
- [ ] 1.2 建立 Dodge -> `Attack_Rush` / `Attack_Rush_Enhance` 的正式 activation 入口与 `Badge_S01` 分支
- [ ] 1.3 建立 Rush 内部 8 状态转移、优先级和 Normal04 回归 owner handoff
- [ ] 1.4 将原 `special_*` 中断映射到 Hit / Evade / Aid / Switch 正式 owner；缺失 owner 时显式失败

## 2. Rush Timeline / cue

- [x] 2.1 建立 8 个状态 Timeline，按 7 个实际 Motion 绑定 AnimationTrack 和 FullBodyAction channel
- [x] 2.2 为每个 Timeline 建 ZZZ 同名 Section 和 `Rush` / `Rush_Enhance` BranchId
- [x] 2.3 挂 93 个 AttackProperty Logic ActionCue，保留原始 `CueId` 与状态/分支/本地帧身份
- [x] 2.4 用 Decision TreeClip 表达 frame=13/23/70、14/70、80、35、43/81/86、40/55、55/118 等状态边界
- [x] 2.5 纳入 8 个 Rush Timeline 资产的正式内容闭包、子图与轨道引用

## 3. Pose / 消费接线

- [ ] 3.1 将 8 个 Timeline producer 接入既有 committed FullBodyAction playback / ActionPlaybackInput / Slot 链。Pose领域确认（2026-09-19）：接线合同已具备、零新增——Corin姿态图已含`corin.full-body-action.slot`节点（AnimationChannelId=FullBodyAction、SelectionAvailability=AllowEmpty，接线为状态机→惯性化→Slot→ControlRig→Output），committed samples经actionSampleProvider按SourceId解析，链路clip无关；Rush 8状态（7 Motion，状态→实际Motion对照见controller-map）只需Timeline producer按2.1绑同channel提交即被Slot混入，唯一运行前提仍是子图Local Pose断点收口。
- [ ] 3.2 确认 GameplayEffect / Ability 消费 Rush 的 8 类原始 AttackProperty key；Timeline 不携带伤害、碰撞或相机 payload
- [ ] 3.3 同步 `docs/zzz-corin-controller-map.md` 与 Rush cue 对照，记录状态边界、命中帧和 Motion 复用证据

# ZZZ Corin Normal Attack Timeline 复核

> 历史记录：保存当时实现、问题与证据；当前合同见[现行文档索引](../../../../openspec/maintenance-audit.md)，当前进展见[文档入口](../../../README.md)。文中的“当前”“待完成”和旧阻塞均属于该记录时点。

只复核 `docs/replication/corin-controller-map.md` 中 Normal Attack 的非 `special_*` 转移。Branch / Rush 不混入现有五段 Attack Timeline。

## 结论

现有五段简化 Timeline 可以继续覆盖 Normal 1 / 2 / 4 的主段和线性 End 恢复；普通 AttackProperty cue 的全局帧仍能发布。但它不能完整表达 Attack 3 Explode 与 Attack 5 End / End_2 的状态分段、分支选择和状态本地时间重映射。缺口如下，不要用循环、拖长 clip 或 Timeline 内分支伪装。

## 复核表

| Normal 状态 | ZZZ 对照 | 现有五段 Timeline | 结论 |
|---|---|---|---|
| Attack1 / Attack1_End | main 无条件到 End；End 没有新的非 special 出边 | main + End 连续排布 | 主起止和主段 cue 可表达；尚无显式 End 状态边界 |
| Attack2 / Attack2_End | main 无条件到 End；End 没有新的非 special 出边 | main + End 连续排布 | 主起止和主段 cue 可表达；尚无显式 End 状态边界 |
| Attack3 / Attack3_Explode / Attack3_End | main 在帧 45 或状态尾进入 Explode；Explode 后可回 Attack4 或到 End | 当前只有 Attack3 main + End 连续段 | 不能表达；见 `GAP-Normal3-ExplodeStateSegment` |
| Attack3 Explode 内 cue | cue 帧分布跨过 ZZZ 帧号 45 | ActionCue 全部写在单一 Timeline 全局轴 | 见 `GAP-Normal3-ExplodeCueRemap` |
| Attack4 / Attack4_End | main 无条件到 End | main + End 连续排布 | 主起止可表达；尚无显式 End 状态边界 |
| Attack5 / Attack5_End / Attack5_End_2 | main 可进 End 或 End_2；End_2 状态实际绑定 `Attack_Normal_05_B` | 当前只有 Attack5 main + End 连续段 | 不能表达；见 `GAP-Normal5-EndBranchSelection` 和 `GAP-Normal5-End2TimelineBinding` |

## 具名缺口

### GAP-Normal3-ExplodeStateSegment

`Attack_Normal_03` 与 `Attack_Normal_03_Explode` 是两个 ZZZ 状态。现有 `CorinAttack3Timeline` 只能表达一条 main 到 End 的连续段，没有独立的 Explode 起止、Explode 到 End 的无条件边界、Explode 到 Attack4 的提前转移投影。不能把 Explode 伪装成普通 End 恢复段。

### GAP-Normal3-ExplodeCueRemap

`CorinAttack3Timeline` 的 AttackProperty cue 位于全局帧 20、21、22、23、38、40、42、44、46、48、50、52、54、56、58、60、62、64、66、68、70、72。控制器对照把 `Attack_Normal_03_Explode` 的进入点放在帧 45。帧 45 前后分属不同状态时，现有 ActionCue 的单一全局帧不能证明等价；必须先定义 Explode 状态本地时间和 cue 重映射，不能直接沿用当前全局帧宣称命中一致。

### GAP-Normal5-EndBranchSelection

`Attack_Normal_05` 可进入 `Attack_Normal_05_End` 或 `Attack_Normal_05_End_2`。条件涉及 `Bool_IsClicking`、`Bool_HoldAttackA`、`Bool_IsAttackLanded_ATK5`、`Trigger_SawExplode`、`Trigger_AttackLanded`。当前 `CorinAttack5Timeline` 没有状态选择边界，不能表达两条 End 路径。Branch / Rush 输入也不得塞进该 Timeline。

### GAP-Normal5-End2TimelineBinding

`Attack_Normal_05_End_2` 不是缺 Clip：控制器状态实际绑定 `Attack_Normal_05_B`，该 Clip 已导出。但现有 `CorinAttack5Timeline` 只绑定 main 与 `Attack_Normal_05_End` 段，没有 `Attack_Normal_05_End_2 -> Attack_Normal_05_B` 的正式 Timeline 段与状态边界。不得用 `Attack_Normal_05_End` 冒充 End_2。
### GAP-NormalEndStateBoundary

Attack1、Attack2、Attack4 的 End 目前只是连续恢复时间，不拥有独立状态边界身份。如果业务只需要线性恢复，可以继续；一旦需要状态名、End 内转移、诊断按状态过滤或状态本地 cue 重映射，必须增加显式状态边界合同，而不是用 clip 作者 id 推断状态。

## Timeline 边界

- AttackProperty payload 仍全量留在 GameplayEffect Profile / Ability 执行域。
- Timeline 只发布稳定 `AttackProperty` ActionCue，payload 只带 `CueId` 和播放身份。
- Branch / Rush 需要各自的 Clip、Timeline、Ability 状态与 Pose binding；不得复用或追加进五段 Normal Timeline。

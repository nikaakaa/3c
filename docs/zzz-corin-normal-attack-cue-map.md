# ZZZ Corin Normal Attack Cue 对照

本文只对齐 Normal Attack 3 / 5 的 AnimatorEvent 与当前 BTSMTL Timeline cue。证据来自 `variants/battle-0.json`、主战斗控制器和 `CorinAttackGameplayAbilityAuthoringCode/Attack.cs`。

## 状态边界

| 源状态 | 转移目标 | 帧条件 | 关键条件 |
|---|---|---:|---|
| `Attack_Normal_03` | `Attack_Normal_03_Explode` | 45 | `FrameCount mode=9 value=45; Bool_IsClicking mode=2 value=0; Bool_HoldAttackA mode=2 value=0` |
| `Attack_Normal_03` | `Attack_Normal_03_Explode` | 45 | `FrameCount mode=9 value=45; Trigger_SawExplode mode=1 value=0` |
| `Attack_Normal_03` | `Attack_Normal_03_Explode` | 80 | `` |
| `Attack_Normal_05` | `Attack_Normal_05_End` | 47 | `FrameCount mode=9 value=47; Bool_IsClicking mode=2 value=0; Bool_HoldAttackA mode=2 value=0; Bool_IsAttackLanded_ATK5 mode=2 value=0` |
| `Attack_Normal_05` | `Attack_Normal_05_End` | 90 | `` |
| `Attack_Normal_05` | `Attack_Normal_05_End_2` | 47 | `FrameCount mode=9 value=47; Bool_IsClicking mode=2 value=0; Bool_HoldAttackA mode=2 value=0; Bool_IsAttackLanded_ATK5 mode=1 value=0` |
| `Attack_Normal_05` | `Attack_Normal_05_End_2` | 47 | `FrameCount mode=9 value=47; Trigger_SawExplode mode=1 value=0` |
| `Attack_Normal_05` | `Attack_Normal_05_End_2` | 47 | `FrameCount mode=9 value=47; Trigger_AttackLanded mode=1 value=0` |

## 当前 / 源事件差异

| 状态 / Pattern | 源事件数 | 当前 AttackProperty cue 数 | 差异 |
|---|---:|---:|---|
| `Corin_Attack_Normal_03` | 20 | 20 | 主段 20 个 cue 帧和 key 完全一致；Explode 状态另缺 1 个 cue |
| `Corin_Attack_Normal_03_Explode` | 1 | 1 | 已在 `Attack_Normal_03_Explode` 段收口 |
| `Corin_Attack_Normal_05` | 15 | 15 | frame=64 多出的 `_01_02` cue 已删除，当前帧和 key 与源一致 |
| `Corin_Attack_Normal_05_End` | 0 | 0 | 无 AttackProperty cue |
| `Corin_Attack_Normal_05_End_2` | 15 | 15 | 已挂在 `CorinAttack5End2Timeline` 的 `Attack_Normal_05_End_2` 段，`BranchId=End_2` |

## 缺失 / 多余 cue 明细

### `Corin_Attack_Normal_03`

| 来源帧 | AnimEventID | 当前帧 | 结论 |
|---:|---|---:|---|
| 40 | `Corin_Attack_Normal_03_AttackProperty_01_01` | 40 | 一致 |
| 42 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 42 | 一致 |
| 44 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 44 | 一致 |
| 46 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 46 | 一致 |
| 48 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 48 | 一致 |
| 50 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 50 | 一致 |
| 52 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 52 | 一致 |
| 54 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 54 | 一致 |
| 56 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 56 | 一致 |
| 58 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 58 | 一致 |
| 60 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 60 | 一致 |
| 62 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 62 | 一致 |
| 64 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 64 | 一致 |
| 66 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 66 | 一致 |
| 68 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 68 | 一致 |
| 70 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 70 | 一致 |
| 72 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 72 | 一致 |
| 74 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 74 | 一致 |
| 76 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 76 | 一致 |
| 78 | `Corin_Attack_Normal_03_AttackProperty_01_02` | 78 | 一致 |

### `Corin_Attack_Normal_03_Explode`

| 来源帧 | AnimEventID | 当前帧 | 结论 |
|---:|---|---:|---|
| 1 | `Corin_Attack_Normal_03_AttackProperty_02` | 75（段内本地 1） | 已收口 |

### `Corin_Attack_Normal_05`

| 来源帧 | AnimEventID | 当前帧 | 结论 |
|---:|---|---:|---|
| 36 | `Corin_Attack_Normal_05_AttackProperty_01_01` | 36 | 一致 |
| 38 | `Corin_Attack_Normal_05_AttackProperty_01_02` | 38 | 一致 |
| 40 | `Corin_Attack_Normal_05_AttackProperty_01_02` | 40 | 一致 |
| 42 | `Corin_Attack_Normal_05_AttackProperty_01_02` | 42 | 一致 |
| 44 | `Corin_Attack_Normal_05_AttackProperty_01_02` | 44 | 一致 |
| 46 | `Corin_Attack_Normal_05_AttackProperty_01_02` | 46 | 一致 |
| 48 | `Corin_Attack_Normal_05_AttackProperty_01_02` | 48 | 一致 |
| 50 | `Corin_Attack_Normal_05_AttackProperty_01_02` | 50 | 一致 |
| 52 | `Corin_Attack_Normal_05_AttackProperty_01_02` | 52 | 一致 |
| 54 | `Corin_Attack_Normal_05_AttackProperty_01_02` | 54 | 一致 |
| 56 | `Corin_Attack_Normal_05_AttackProperty_01_02` | 56 | 一致 |
| 58 | `Corin_Attack_Normal_05_AttackProperty_01_02` | 58 | 一致 |
| 60 | `Corin_Attack_Normal_05_AttackProperty_01_02` | 60 | 一致 |
| 62 | `Corin_Attack_Normal_05_AttackProperty_01_02` | 62 | 一致 |
| 64 | `Corin_Attack_Normal_05_AttackProperty_02` | 64 | 一致 |

### `Corin_Attack_Normal_05_End`

| 来源帧 | AnimEventID | 当前帧 | 结论 |
|---:|---|---:|---|

### `Corin_Attack_Normal_05_End_2`

| 来源帧 | AnimEventID | 当前帧 | 结论 |
|---:|---|---:|---|
| 1 | `Corin_Attack_Normal_05_AttackProperty_03` | 段内本地 1 | 已收口 |
| 10 | `Corin_Attack_Normal_05_AttackProperty_04_01` | 段内本地 10 | 已收口 |
| 12 | `Corin_Attack_Normal_05_AttackProperty_04_02` | 段内本地 12 | 已收口 |
| 14 | `Corin_Attack_Normal_05_AttackProperty_04_02` | 段内本地 14 | 已收口 |
| 16 | `Corin_Attack_Normal_05_AttackProperty_04_02` | 段内本地 16 | 已收口 |
| 18 | `Corin_Attack_Normal_05_AttackProperty_04_02` | 段内本地 18 | 已收口 |
| 20 | `Corin_Attack_Normal_05_AttackProperty_04_02` | 段内本地 20 | 已收口 |
| 22 | `Corin_Attack_Normal_05_AttackProperty_04_02` | 段内本地 22 | 已收口 |
| 24 | `Corin_Attack_Normal_05_AttackProperty_04_02` | 段内本地 24 | 已收口 |
| 26 | `Corin_Attack_Normal_05_AttackProperty_04_02` | 段内本地 26 | 已收口 |
| 28 | `Corin_Attack_Normal_05_AttackProperty_04_02` | 段内本地 28 | 已收口 |
| 30 | `Corin_Attack_Normal_05_AttackProperty_04_02` | 段内本地 30 | 已收口 |
| 32 | `Corin_Attack_Normal_05_AttackProperty_04_02` | 段内本地 32 | 已收口 |
| 34 | `Corin_Attack_Normal_05_AttackProperty_04_02` | 段内本地 34 | 已收口 |
| 36 | `Corin_Attack_Normal_05_AttackProperty_04_02` | 段内本地 36 | 已收口 |
## 收口要求

1. Attack3 Explode 必须拥有独立状态本地时间；其 frame=1 的 `Corin_Attack_Normal_03_AttackProperty_02` 不能塞进主段全局轴。
2. Attack5 的 frame=47 是 End / End_2 分支选择点；现在由 `Attack5EndBoundary` TreeClip 和正式状态转移选择唯一分支。
3. `Corin_Attack_Normal_05_End_2` 的 15 个本地 AttackProperty cue 已挂在 `CorinAttack5End2Timeline` 的 `End_2` 分支。
4. Attack5 frame=64 多出的 `_01_02` cue 已删除；对应 Ability 资产已重建，实际动作回放尚未完成。
5. 已重建对应 Ability/Timeline 资产；无输入 Replay 已删除，不能作为普通攻击运行证据。

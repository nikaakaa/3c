# ZZZ Corin 主战斗控制器抄录对照

本文只对照主战斗控制器 `Avatar_Female_Size01_Corin_Controller__1291803240_00335DAA.json`，不包含 MainCity、NPC、UI。它是 Control/Ability/Timeline/Pose 抄录的唯一状态对照入口；具体资产仍按正式领域 owner 和 C# authoring 链路落盘。

## 分组结论

| 组 | 状态数 | 3C 归属 | 当前结论 |
|---|---:|---|---|
| NormalAttack | 12 | Control + Attack Ability + Timeline | 已收口：12状态进入正式 Attack FSM/Timeline；End/Explode/End_2 状态边界、Attack5EndBoundary 分支和状态本地 cue 已重建，`Attack_Normal_05_End_2` 绑 `Attack_Normal_05_B`。 |
| BranchAttack | 18 | Control + Attack Ability + Timeline + Pose | 已精确对账：18状态全部有真实Clip绑定，Branch_01/03大量共享Branch_02与Branch_Loop/Walk共享Motion；缺的是Timeline/Pose binding与Ability状态，不是Clip。 |
| RushAttack | 8 | Control + Attack Ability + Timeline + Pose | 已精确对账并定链（2026-09-19）：采用与NormalAttack相同的Timeline Action playback链，不扩locomotion状态机；Pose侧Slot链已具备零新增，等待Timeline producer建链。 |
| AidCounterAttack | 20 | Control + Combat/Aid Ability + Timeline | 仅有效果/镜头证据，没有正式 3C Aid/Counter 执行链；AssaultAid 只有 raw Motion。 |
| EvadeHit | 8 | Control + Pose + Hit feedback | Dodge 已有正式 Ability；Hit/HitFly 状态复用 Front/Back Motion，还没有完整 3C 链。 |
| Switch | 11 | Control + Switch Ability | SwitchIn/SwitchOut 还没有正式 Control 请求与 Switch Ability。 |

## Motion 证据统计

| 证据类型 | 状态数 | 含义 |
|---|---:|---|
| exported-anim | 97 | 控制器 Motion 引用已有 `.anim` 导出 |
| raw-only | 2 | 控制器 Motion 引用只有 raw `.dat`，尚未解码成 `.anim` |
| no-motion | 2 | PlaceHolder/InstantOut 等无 Motion 状态 |

## 状态到 Motion 对照

只使用控制器 BlendTree 的实际 `Clip` 引用；状态名和 Motion 名不同时不做近似猜测。`raw-only` 表示现有导出包还不能直接进 Unity。Normal/Branch/Rush 已完成 BlendTree 叶子精确对账；共享 Motion 和非同名绑定逐行标注。

| 状态组 | ZZZ 状态 | Motion 证据 | 证据类型 | 3C 缺口 |
|---|---|---|---|---|
| NormalAttack | `Attack_Normal_01` | `Attack_Normal_01` | exported-anim | 精确同名绑定（BlendTree单叶，已导出） |
| NormalAttack | `Attack_Normal_01_End` | `Attack_Normal_01_End` | exported-anim | 精确同名绑定（已导出） |
| NormalAttack | `Attack_Normal_02` | `Attack_Normal_02` | exported-anim | 精确同名绑定（已导出） |
| NormalAttack | `Attack_Normal_02_End` | `Attack_Normal_02_End` | exported-anim | 精确同名绑定（已导出） |
| NormalAttack | `Attack_Normal_03` | `Attack_Normal_03` | exported-anim | 精确同名绑定（已导出） |
| NormalAttack | `Attack_Normal_03_End` | `Attack_Normal_03_End` | exported-anim | 精确同名绑定（已导出） |
| NormalAttack | `Attack_Normal_03_Explode` | `Attack_Normal_03_Explode` | exported-anim | 精确同名绑定（已导出） |
| NormalAttack | `Attack_Normal_04` | `Attack_Normal_04` | exported-anim | 精确同名绑定（已导出） |
| NormalAttack | `Attack_Normal_04_End` | `Attack_Normal_04_End` | exported-anim | 精确同名绑定（已导出） |
| NormalAttack | `Attack_Normal_05` | `Attack_Normal_05` | exported-anim | 精确同名绑定（已导出） |
| NormalAttack | `Attack_Normal_05_End` | `Attack_Normal_05_End` | exported-anim | 精确同名绑定（已导出） |
| NormalAttack | `Attack_Normal_05_End_2` | `Attack_Normal_05_B` | exported-anim | 非同名：状态实际绑定`Attack_Normal_05_B`（已导出），状态名≠Clip名 |
| BranchAttack | `Attack_Branch_01` | `Attack_Branch_01` | exported-anim | 精确同名绑定（已导出） |
| BranchAttack | `Attack_Branch_01_End` | `Attack_Branch_01_End` | exported-anim | 精确同名绑定（已导出） |
| BranchAttack | `Attack_Branch_01_Explode` | `Attack_Branch_01_Explode` | exported-anim | 精确同名绑定（已导出） |
| BranchAttack | `Attack_Branch_01_NotExplode` | `Attack_Branch_01_Explode` | exported-anim | 共享Motion：与`Attack_Branch_01_Explode`状态绑同一Clip（已导出），NotExplode语义差异在转移条件不在Motion |
| BranchAttack | `Attack_Branch_02` | `Attack_Branch_02` | exported-anim | 精确同名绑定（已导出） |
| BranchAttack | `Attack_Branch_02_End` | `Attack_Branch_02_End` | exported-anim | 精确同名绑定（已导出） |
| BranchAttack | `Attack_Branch_02_Explode` | `Attack_Branch_02_Explode` | exported-anim | 精确同名绑定（已导出） |
| BranchAttack | `Attack_Branch_02_Loop` | `Attack_Branch_Loop` | exported-anim | 共享Motion：Branch_Loop为Branch_02/03_Loop共享（已导出） |
| BranchAttack | `Attack_Branch_02_Walk` | `Attack_Branch_Walk` | exported-anim | 共享Motion：Branch_Walk（已导出），非Branch_02专属 |
| BranchAttack | `Attack_Branch_03` | `Attack_Branch_02` | exported-anim | 共享Motion：Branch_03主体复用Branch_02 Clip（已导出） |
| BranchAttack | `Attack_Branch_03_End` | `Attack_Branch_02_End` | exported-anim | 共享Motion：复用Branch_02_End（已导出） |
| BranchAttack | `Attack_Branch_03_Enhance_Loop` | `Attack_Branch_03_Loop` | exported-anim | 共享Motion：与Branch_03_Loop同Clip（已导出），增强差异在参数/转移 |
| BranchAttack | `Attack_Branch_03_Enhance_Walk` | `Attack_Branch_03_Walk_Loop` | exported-anim | 共享Motion：与Branch_03_Walk同Clip（已导出） |
| BranchAttack | `Attack_Branch_03_Explode` | `Attack_Branch_02_Explode` | exported-anim | 共享Motion：复用Branch_02_Explode（已导出） |
| BranchAttack | `Attack_Branch_03_Loop` | `Attack_Branch_Loop` | exported-anim | 共享Motion：Branch_Loop（已导出） |
| BranchAttack | `Attack_Branch_03_Shake` | `Attack_Branch_03_Shake` | exported-anim | 精确同名绑定（已导出） |
| BranchAttack | `Attack_Branch_03_Walk` | `Attack_Branch_03_Walk_Loop` | exported-anim | 精确绑定（非同名后缀_Loop，已导出） |
| BranchAttack | `Attack_Branch_03_Walk_Shake` | `Attack_Branch_03_Walk_Shake` | exported-anim | 精确同名绑定（已导出） |
| RushAttack | `Attack_Rush` | `Attack_Rush` | exported-anim | 精确同名绑定（已导出） |
| RushAttack | `Attack_Rush_End` | `Attack_Rush_End` | exported-anim | 精确同名绑定（已导出） |
| RushAttack | `Attack_Rush_Enhance` | `Attack_Rush_Enhance_Start` | exported-anim | 非同名：状态实际绑定`Attack_Rush_Enhance_Start`（已导出） |
| RushAttack | `Attack_Rush_Enhance_End` | `Attack_Rush_Explode` | exported-anim | 共享Motion：复用`Attack_Rush_Explode`（已导出） |
| RushAttack | `Attack_Rush_Enhance_Explode` | `Attack_Rush_Enhance_Explode` | exported-anim | 精确同名绑定（已导出） |
| RushAttack | `Attack_Rush_Enhance_Explode_End` | `Attack_Rush_Enhance_End` | exported-anim | 共享Motion：复用Enhance_End状态所绑Clip（已导出） |
| RushAttack | `Attack_Rush_Enhance_Loop` | `Attack_Rush_Enhance_Loop` | exported-anim | 精确同名绑定（已导出） |
| RushAttack | `Attack_Rush_Explode` | `Attack_Rush_Explode` | exported-anim | 精确同名绑定（已导出） |
| AidCounterAttack | `Attack_AssaultAid` | `AssaultAid` | raw-only | 先补 `.anim` 解码，再建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_AssaultAid_End` | `AssaultAid_End` | raw-only | 先补 `.anim` 解码，再建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_BeHitAid` | `Attack_Counter` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_BeHitAid_End` | `Attack_Counter_End` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_BeHitAid_Explode` | `Attack_Counter_Explode` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_Counter` | `Attack_Counter` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_Counter_End` | `Attack_Counter_End` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_Counter_Explode` | `Attack_Counter_Explode` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_Counter_NotExplode` | `Attack_Counter_Explode` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_ParryAid_H` | `Attack_ParryAid_H` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_ParryAid_H_End` | `Attack_ParryAid_H_End` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_ParryAid_H_Start` | `Attack_ParryAid_Start` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_ParryAid_L` | `Attack_ParryAid_L` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_ParryAid_L_End` | `Attack_ParryAid_L_End` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_ParryAid_L_Start` | `Attack_ParryAid_Start` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_ParrySolo_H` | `Attack_ParryAid_H` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_ParrySolo_H_End` | `Attack_ParryAid_H_End` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_ParrySolo_L` | `Attack_ParryAid_L` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_ParrySolo_L_End` | `Attack_ParryAid_L_End` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_ParrySolo_Start` | `Attack_ParryAid_Start` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| EvadeHit | `Evade_Back` | `Evade_Back` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| EvadeHit | `Evade_Front` | `Evade_Front` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| EvadeHit | `HitFly_B` | `HitFly_Back` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| EvadeHit | `HitFly_F` | `HitFly_Front` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| EvadeHit | `Hit_H_B` | `Hit_H_Back` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| EvadeHit | `Hit_H_F` | `Hit_H_Front` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| EvadeHit | `Hit_L_B` | `Hit_L_Back` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| EvadeHit | `Hit_L_F` | `Hit_L_Front` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| Switch | `SwitchIn_Attack` | `SwitchIn_Attack_02` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| Switch | `SwitchIn_Attack_End` | `SwitchIn_Attack_02_End` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| Switch | `SwitchIn_Attack_Ex` | `SwitchIn_Attack_Ex` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| Switch | `SwitchIn_Attack_Ex_End` | `SwitchIn_Attack_Ex_End` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| Switch | `SwitchIn_Attack_Ex_Start` | `SwitchIn_Attack_Ex_Start` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| Switch | `SwitchIn_Attack_Explode` | `SwitchIn_Attack_02_Explode` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| Switch | `SwitchIn_Attack_Explode_End` | `SwitchIn_Attack_02_Explode_End` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| Switch | `SwitchIn_Attack_Landed` | `SwitchIn_Attack_02_Landed` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| Switch | `SwitchIn_Normal` | `SiwtchIn_Normal` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| Switch | `SwitchOut_Attack` | `Evade_Back` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |
| Switch | `SwitchOut_Normal` | `SwitchOut_Normal` | exported-anim | 按 Motion 引用建立 Timeline、Pose binding 与 Ability 状态 |

## 非 `special_*` 转移

下表保留 ZZZ 目标不是 `special_*` 的转移；`special_*` 是宿主层外部状态路由，不直接抄成 3C FSM 边。条件字段保留原始名，后面由 Control/Ability 翻译成 Action Request、Action Window 或事件图变量。

| 源状态 | 目标状态 | 退出比例 / 固定时长 | 条件 |
|---|---|---|---|
| `Attack_Normal_01` | `Attack_Normal_04` | 0.576923 / fixed=true 0.100000 | `FrameCount mode=9 value=30; Bool_HoldAttackA mode=1 value=0; Bool_Badge_S03 mode=1 value=0` |
| `Attack_Normal_01` | `Attack_Normal_04` | 0.576923 / fixed=true 0.100000 | `FrameCount mode=9 value=30; Bool_IsClicking mode=1 value=0; Bool_Badge_S03 mode=1 value=0` |
| `Attack_Normal_01` | `Attack_Normal_02` | 0.634615 / fixed=true 0.050000 | `FrameCount mode=9 value=33; Trigger_PressAttackA mode=1 value=0` |
| `Attack_Normal_01` | `Attack_Normal_01_End` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_Normal_02` | `Attack_Normal_04` | 0.680851 / fixed=true 0.200000 | `FrameCount mode=9 value=30; Bool_IsClicking mode=1 value=0; Bool_Badge_S03 mode=1 value=0` |
| `Attack_Normal_02` | `Attack_Normal_04` | 0.680851 / fixed=true 0.200000 | `FrameCount mode=9 value=30; Bool_HoldAttackA mode=1 value=0; Bool_Badge_S03 mode=1 value=0` |
| `Attack_Normal_02` | `Attack_Normal_03` | 0.531915 / fixed=true 0.100000 | `FrameCount mode=9 value=25; Trigger_PressAttackA mode=1 value=0` |
| `Attack_Normal_02` | `Attack_Normal_02_End` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_Normal_03` | `Attack_Normal_03_Explode` | 0.562500 / fixed=true 0.100000 | `FrameCount mode=9 value=45; Bool_IsClicking mode=2 value=0; Bool_HoldAttackA mode=2 value=0` |
| `Attack_Normal_03` | `Attack_Normal_03_Explode` | 0.562500 / fixed=true 0.100000 | `FrameCount mode=9 value=45; Trigger_SawExplode mode=1 value=0` |
| `Attack_Normal_03` | `Attack_Normal_03_Explode` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_Normal_03_Explode` | `Attack_Normal_04` | 0.083333 / fixed=true 0.100000 | `FrameCount mode=9 value=3; Trigger_PressAttackA mode=1 value=0` |
| `Attack_Normal_03_Explode` | `Attack_Normal_04` | 0.083333 / fixed=true 0.100000 | `FrameCount mode=9 value=3; Bool_HoldAttackA mode=1 value=0` |
| `Attack_Normal_03_Explode` | `Attack_Normal_03_End` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_Normal_04` | `Attack_Normal_05` | 0.681818 / fixed=true 0.100000 | `FrameCount mode=9 value=60; Trigger_PressAttackA mode=1 value=0` |
| `Attack_Normal_04` | `Attack_Normal_04_End` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_Normal_05` | `Attack_Normal_05_End` | 0.522222 / fixed=true 0.250000 | `FrameCount mode=9 value=47; Bool_IsClicking mode=2 value=0; Bool_HoldAttackA mode=2 value=0; Bool_IsAttackLanded_ATK5 mode=2 value=0` |
| `Attack_Normal_05` | `Attack_Normal_05_End_2` | 0.522222 / fixed=true 0.100000 | `FrameCount mode=9 value=47; Bool_IsClicking mode=2 value=0; Bool_HoldAttackA mode=2 value=0; Bool_IsAttackLanded_ATK5 mode=1 value=0` |
| `Attack_Normal_05` | `Attack_Normal_05_End_2` | 0.522222 / fixed=true 0.100000 | `FrameCount mode=9 value=47; Trigger_SawExplode mode=1 value=0` |
| `Attack_Normal_05` | `Attack_Normal_05_End_2` | 0.522222 / fixed=true 0.100000 | `FrameCount mode=9 value=47; Trigger_AttackLanded mode=1 value=0` |
| `Attack_Normal_05` | `Attack_Normal_05_End` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_Branch_01` | `Attack_Branch_01_Explode` | 0.640000 / fixed=true 0.100000 | `FrameCount mode=9 value=64; Bool_HoldAttackB mode=2 value=0; Bool_IsClicking mode=2 value=0` |
| `Attack_Branch_01` | `Attack_Branch_01_Explode` | 0.640000 / fixed=true 0.100000 | `FrameCount mode=9 value=64; Trigger_SawExplode mode=1 value=0` |
| `Attack_Branch_01` | `Attack_Branch_01_Explode` | 0.640000 / fixed=true 0.100000 | `FrameCount mode=9 value=64; Trigger_AttackLanded mode=1 value=0` |
| `Attack_Branch_01` | `Attack_Branch_01_Explode` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_Branch_01_End` | `Attack_Normal_04` | 0.797872 / fixed=true 0.100000 | `Trigger_PressAttackA mode=1 value=0` |
| `Attack_Branch_01_Explode` | `Attack_Normal_04` | 0.675000 / fixed=true 0.100000 | `FrameCount mode=9 value=27; Trigger_PressAttackA mode=1 value=0` |
| `Attack_Branch_01_Explode` | `Attack_Branch_01_End` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_Branch_01_NotExplode` | `Attack_Branch_01_End` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_Branch_02` | `Attack_Branch_02_Loop` | 0.658000 / fixed=true 0.100000 | `Bool_IsPartnerAvatar mode=1 value=0` |
| `Attack_Branch_02` | `Attack_Branch_02_Loop` | 0.658000 / fixed=true 0.100000 | `unconditional` |
| `Attack_Branch_02` | `Attack_Branch_02_Explode` | 0.653061 / fixed=true 0.100000 | `FrameCount mode=9 value=64; Bool_HoldAttackB mode=2 value=0; Bool_IsClicking mode=2 value=0` |
| `Attack_Branch_02_Explode` | `Attack_Branch_02_End` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_Branch_02_Loop` | `Attack_Branch_02_Walk` | 0.500000 / fixed=true 0.200000 | `Bool_IsMoving mode=1 value=0` |
| `Attack_Branch_02_Loop` | `Attack_Branch_02_Explode` | 0.500000 / fixed=true 0.100000 | `Bool_HoldAttackB mode=2 value=0; Bool_IsClicking mode=2 value=0; Bool_IsPartnerAvatar mode=2 value=0` |
| `Attack_Branch_02_Loop` | `Attack_Branch_02_Explode` | 0.750000 / fixed=true 0.100000 | `FrameCount mode=9 value=30; Bool_IsPartnerAvatar mode=1 value=0` |
| `Attack_Branch_02_Loop` | `Attack_Branch_02_Explode` | 0.500000 / fixed=true 0.100000 | `Trigger_SawExplode mode=1 value=0; Bool_IsPartnerAvatar mode=2 value=0` |
| `Attack_Branch_02_Walk` | `Attack_Branch_02_Loop` | 0.468750 / fixed=true 0.200000 | `Bool_IsMoving mode=2 value=0` |
| `Attack_Branch_02_Walk` | `Attack_Branch_02_Explode` | 0.468750 / fixed=true 0.100000 | `Bool_HoldAttackB mode=2 value=0; Bool_IsClicking mode=2 value=0` |
| `Attack_Branch_02_Walk` | `Attack_Branch_02_Explode` | 0.468750 / fixed=true 0.100000 | `Trigger_SawExplode mode=1 value=0` |
| `Attack_Branch_03` | `Attack_Branch_03_Loop` | 0.765306 / fixed=true 0.100000 | `Bool_IsPartnerAvatar mode=1 value=0` |
| `Attack_Branch_03` | `Attack_Branch_03_Loop` | 0.658000 / fixed=true 0.100000 | `unconditional` |
| `Attack_Branch_03_End` | `SwitchOut_Normal` | 0.576923 / fixed=true 0.100000 | `Trigger_PressQTESolo mode=1 value=0; Bool_QTESolo mode=1 value=0` |
| `Attack_Branch_03_End` | `SwitchOut_Normal` | 0.576923 / fixed=true 0.100000 | `Trigger_PressAttackB mode=1 value=0; Bool_QTESolo mode=1 value=0` |
| `Attack_Branch_03_End` | `SwitchOut_Normal` | 0.576923 / fixed=true 0.100000 | `Trigger_PressAttackA mode=1 value=0; Bool_QTESolo mode=1 value=0` |
| `Attack_Branch_03_Enhance_Loop` | `Attack_Branch_03_Explode` | 0.750000 / fixed=true 0.100000 | `FrameCount mode=9 value=30; Bool_IsPartnerAvatar mode=1 value=0` |
| `Attack_Branch_03_Enhance_Loop` | `Attack_Branch_03_Explode` | 0.500000 / fixed=true 0.100000 | `Trigger_SawExplode mode=1 value=0; Bool_IsPartnerAvatar mode=2 value=0` |
| `Attack_Branch_03_Enhance_Loop` | `Attack_Branch_03_Explode` | 0.500000 / fixed=true 0.100000 | `Bool_HoldAttackB mode=2 value=0; Bool_IsClicking mode=2 value=0; Bool_IsPartnerAvatar mode=2 value=0` |
| `Attack_Branch_03_Enhance_Loop` | `Attack_Branch_03_Enhance_Walk` | 0.500000 / fixed=true 0.200000 | `Bool_IsMoving mode=1 value=0` |
| `Attack_Branch_03_Enhance_Walk` | `Attack_Branch_03_Enhance_Loop` | 0.468750 / fixed=true 0.200000 | `Bool_IsMoving mode=2 value=0` |
| `Attack_Branch_03_Enhance_Walk` | `Attack_Branch_03_Explode` | 0.468750 / fixed=true 0.100000 | `Trigger_SawExplode mode=1 value=0` |
| `Attack_Branch_03_Enhance_Walk` | `Attack_Branch_03_Explode` | 0.468750 / fixed=true 0.100000 | `Bool_HoldAttackB mode=2 value=0; Bool_IsClicking mode=2 value=0` |
| `Attack_Branch_03_Explode` | `SwitchOut_Normal` | 0.500000 / fixed=true 0.100000 | `FrameCount mode=9 value=31; Trigger_PressQTESolo mode=1 value=0; Bool_QTESolo mode=1 value=0` |
| `Attack_Branch_03_Explode` | `SwitchOut_Normal` | 0.500000 / fixed=true 0.100000 | `FrameCount mode=9 value=31; Trigger_PressAttackB mode=1 value=0; Bool_QTESolo mode=1 value=0` |
| `Attack_Branch_03_Explode` | `SwitchOut_Normal` | 0.500000 / fixed=true 0.100000 | `FrameCount mode=9 value=31; Trigger_PressAttackA mode=1 value=0; Bool_QTESolo mode=1 value=0` |
| `Attack_Branch_03_Explode` | `Attack_Branch_03_End` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_Branch_03_Loop` | `Attack_Branch_03_Walk` | 0.500000 / fixed=true 0.200000 | `Bool_IsMoving mode=1 value=0` |
| `Attack_Branch_03_Loop` | `Attack_Branch_03_Enhance_Loop` | 0.500000 / fixed=true 0.250000 | `Trigger_Overclocking mode=1 value=0` |
| `Attack_Branch_03_Loop` | `Attack_Branch_03_Explode` | 0.500000 / fixed=true 0.100000 | `Bool_HoldAttackB mode=2 value=0; Bool_IsClicking mode=2 value=0; Bool_IsPartnerAvatar mode=2 value=0` |
| `Attack_Branch_03_Loop` | `Attack_Branch_03_Explode` | 0.750000 / fixed=true 0.100000 | `FrameCount mode=9 value=30; Bool_IsPartnerAvatar mode=1 value=0` |
| `Attack_Branch_03_Shake` | `Attack_Branch_03_Explode` | 0.937500 / fixed=true 0.100000 | `Bool_HoldAttackB mode=2 value=0; Bool_IsClicking mode=2 value=0; Bool_IsPartnerAvatar mode=2 value=0` |
| `Attack_Branch_03_Shake` | `Attack_Branch_03_Explode` | 0.937500 / fixed=true 0.100000 | `Bool_IsPartnerAvatar mode=1 value=0` |
| `Attack_Branch_03_Shake` | `Attack_Branch_03_Explode` | 0.937500 / fixed=true 0.100000 | `Trigger_SawExplode mode=1 value=0; Bool_IsPartnerAvatar mode=2 value=0` |
| `Attack_Branch_03_Walk` | `Attack_Branch_03_Loop` | 0.468750 / fixed=true 0.200000 | `Bool_IsMoving mode=2 value=0` |
| `Attack_Branch_03_Walk` | `Attack_Branch_03_Enhance_Walk` | 0.468750 / fixed=true 0.250000 | `Trigger_Overclocking mode=1 value=0` |
| `Attack_Branch_03_Walk` | `Attack_Branch_03_Explode` | 0.468750 / fixed=true 0.100000 | `Bool_HoldAttackB mode=2 value=0; Bool_IsClicking mode=2 value=0` |
| `Attack_Branch_03_Walk` | `Attack_Branch_03_Explode` | 0.468750 / fixed=true 0.100000 | `Trigger_SawExplode mode=1 value=0` |
| `Attack_Branch_03_Walk_Shake` | `Attack_Branch_03_Explode` | 0.937500 / fixed=true 0.100000 | `Bool_IsPartnerAvatar mode=1 value=0` |
| `Attack_Branch_03_Walk_Shake` | `Attack_Branch_03_Explode` | 0.500000 / fixed=true 0.100000 | `Trigger_SawExplode mode=1 value=0; Bool_IsPartnerAvatar mode=2 value=0` |
| `Attack_Branch_03_Walk_Shake` | `Attack_Branch_03_Explode` | 0.500000 / fixed=true 0.100000 | `Bool_HoldAttackB mode=2 value=0; Bool_IsClicking mode=2 value=0; Bool_IsPartnerAvatar mode=2 value=0` |
| `Attack_Rush` | `Attack_Rush_Explode` | 0.185714 / fixed=true 0.100000 | `FrameCount mode=9 value=13; Bool_HoldAttackA mode=2 value=0; Bool_IsClicking mode=2 value=0` |
| `Attack_Rush` | `Attack_Rush_Explode` | 0.328571 / fixed=true 0.100000 | `FrameCount mode=9 value=23; Trigger_SawExplode mode=1 value=0` |
| `Attack_Rush` | `Attack_Rush_Explode` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_Rush_Enhance` | `Attack_Rush_Enhance_Loop` | 0.785714 / fixed=true 0.100000 | `unconditional` |
| `Attack_Rush_Enhance_End` | `Attack_Normal_04` | 0.571428 / fixed=true 0.100000 | `Bool_Badge_S03 mode=1 value=0; Bool_HoldAttackA mode=1 value=0; FrameCount mode=4 value=40` |
| `Attack_Rush_Enhance_End` | `Attack_Normal_04` | 0.785714 / fixed=true 0.100000 | `FrameCount mode=4 value=40; Trigger_PressAttackA mode=1 value=0` |
| `Attack_Rush_Enhance_End` | `Attack_Rush_End` | 0.785714 / fixed=true 0.000000 | `unconditional` |
| `Attack_Rush_Enhance_Explode` | `Attack_Normal_04` | 0.785714 / fixed=true 0.100000 | `Bool_Badge_S03 mode=1 value=0; Bool_HoldAttackA mode=1 value=0; FrameCount mode=9 value=16; FrameCount mode=4 value=44` |
| `Attack_Rush_Enhance_Explode` | `Attack_Normal_04` | 0.785714 / fixed=true 0.100000 | `FrameCount mode=9 value=16; FrameCount mode=4 value=44; Trigger_PressAttackA mode=1 value=0` |
| `Attack_Rush_Enhance_Explode` | `Attack_Rush_Enhance_Explode_End` | 0.785714 / fixed=true 0.100000 | `unconditional` |
| `Attack_Rush_Enhance_Loop` | `Attack_Rush_Enhance_Explode` | 0.500000 / fixed=true 0.100000 | `Trigger_SawExplode mode=1 value=0` |
| `Attack_Rush_Enhance_Loop` | `Attack_Rush_Enhance_End` | 1.000000 / fixed=true 0.250000 | `Bool_HoldAttackA mode=2 value=0; Bool_IsClicking mode=2 value=0` |
| `Attack_Rush_Enhance_Loop` | `Attack_Rush_Enhance_Loop` | 0.931818 / fixed=true 0.100000 | `unconditional` |
| `Attack_Rush_Explode` | `Attack_Normal_04` | 0.200000 / fixed=true 0.100000 | `Bool_Badge_S03 mode=1 value=0; Bool_HoldAttackA mode=1 value=0; FrameCount mode=9 value=14; FrameCount mode=4 value=44` |
| `Attack_Rush_Explode` | `Attack_Normal_04` | 0.200000 / fixed=true 0.100000 | `FrameCount mode=9 value=14; FrameCount mode=4 value=44; Trigger_PressAttackA mode=1 value=0` |
| `Attack_Rush_Explode` | `Attack_Rush_End` | 1.000000 / fixed=true 0.100000 | `unconditional` |
| `Attack_AssaultAid` | `Attack_AssaultAid_End` | 0.583333 / fixed=true 0.100000 | `FrameCount mode=9 value=70; Bool_HoldAttackA mode=2 value=0; Bool_HoldAttackB mode=2 value=0; Bool_IsClicking mode=2 value=0` |
| `Attack_AssaultAid` | `Attack_AssaultAid_End` | 0.583333 / fixed=true 0.100000 | `FrameCount mode=9 value=70; Trigger_SawExplode mode=1 value=0` |
| `Attack_AssaultAid` | `Attack_AssaultAid_End` | 0.583333 / fixed=true 0.100000 | `FrameCount mode=9 value=70; Trigger_AttackLanded mode=1 value=0` |
| `Attack_AssaultAid` | `Attack_AssaultAid_End` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_AssaultAid_End` | `Attack_Normal_05` | 0.136364 / fixed=true 0.100000 | `FrameCount mode=9 value=24; FrameCount mode=4 value=54; Trigger_PressAttackA mode=1 value=0` |
| `Attack_BeHitAid` | `Attack_BeHitAid_Explode` | 0.431034 / fixed=true 0.100000 | `FrameCount mode=9 value=25; Bool_HoldAttackA mode=2 value=0; Bool_IsClicking mode=2 value=0` |
| `Attack_BeHitAid` | `Attack_BeHitAid_Explode` | 0.431034 / fixed=true 0.100000 | `FrameCount mode=9 value=25; Trigger_SawExplode mode=1 value=0` |
| `Attack_BeHitAid` | `Attack_BeHitAid_Explode` | 0.431034 / fixed=true 0.100000 | `FrameCount mode=9 value=25; Trigger_AttackLanded mode=1 value=0` |
| `Attack_BeHitAid` | `Attack_BeHitAid_Explode` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_BeHitAid_End` | `Attack_Normal_04` | 0.272727 / fixed=true 0.200000 | `Bool_Badge_S03 mode=1 value=0; Bool_HoldAttackA mode=1 value=0; FrameCount mode=9 value=8; FrameCount mode=4 value=48` |
| `Attack_BeHitAid_End` | `Attack_Normal_04` | 0.272727 / fixed=true 0.200000 | `FrameCount mode=9 value=8; FrameCount mode=4 value=48; Trigger_PressAttackA mode=1 value=0` |
| `Attack_BeHitAid_Explode` | `Attack_BeHitAid_End` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_Counter` | `Attack_Counter_Explode` | 0.431034 / fixed=true 0.100000 | `FrameCount mode=9 value=25; Bool_HoldAttackA mode=2 value=0; Bool_IsClicking mode=2 value=0` |
| `Attack_Counter` | `Attack_Counter_Explode` | 0.431034 / fixed=true 0.100000 | `FrameCount mode=9 value=25; Trigger_SawExplode mode=1 value=0` |
| `Attack_Counter` | `Attack_Counter_Explode` | 0.431034 / fixed=true 0.100000 | `FrameCount mode=9 value=25; Trigger_AttackLanded mode=1 value=0` |
| `Attack_Counter` | `Attack_Counter_Explode` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_Counter_End` | `Attack_Normal_04` | 0.045455 / fixed=true 0.200000 | `Bool_Badge_S03 mode=1 value=0; Bool_HoldAttackA mode=1 value=0; FrameCount mode=9 value=8; FrameCount mode=4 value=48` |
| `Attack_Counter_End` | `Attack_Normal_04` | 0.045455 / fixed=true 0.200000 | `FrameCount mode=9 value=8; FrameCount mode=4 value=48; Trigger_PressAttackA mode=1 value=0` |
| `Attack_Counter_Explode` | `Attack_Counter_End` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_Counter_NotExplode` | `Attack_Counter_End` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `Attack_ParryAid_H` | `Attack_ParryAid_H` | 0.200000 / fixed=true 0.000000 | `FrameCount mode=9 value=12; FrameCount mode=4 value=50; Trigger_Parry_H mode=1 value=0` |
| `Attack_ParryAid_H` | `Attack_AssaultAid` | 0.500000 / fixed=true 0.030000 | `FrameCount mode=9 value=30; Trigger_PressAttackB mode=1 value=0; Bool_AssaultAid_Enable mode=1 value=0` |
| `Attack_ParryAid_H` | `Attack_AssaultAid` | 0.500000 / fixed=true 0.030000 | `FrameCount mode=9 value=30; Trigger_PressAttackA mode=1 value=0; Bool_AssaultAid_Enable mode=1 value=0; Bool_EasyAssaultAid mode=1 value=0` |
| `Attack_ParryAid_H` | `Attack_ParryAid_H_End` | 0.687500 / fixed=true 0.000000 | `unconditional` |
| `Attack_ParryAid_H_Start` | `Attack_ParryAid_H` | 0.229358 / fixed=true 0.000000 | `FrameCount mode=4 value=50; Trigger_Parry_H mode=1 value=0` |
| `Attack_ParryAid_L` | `Attack_AssaultAid` | 0.250000 / fixed=true 0.030000 | `FrameCount mode=9 value=10; Trigger_PressAttackB mode=1 value=0; Bool_AssaultAid_Enable mode=1 value=0` |
| `Attack_ParryAid_L` | `Attack_AssaultAid` | 0.250000 / fixed=true 0.030000 | `FrameCount mode=9 value=10; Trigger_PressAttackA mode=1 value=0; Bool_AssaultAid_Enable mode=1 value=0; Bool_EasyAssaultAid mode=1 value=0` |
| `Attack_ParryAid_L` | `Attack_ParryAid_L_End` | 0.687500 / fixed=true 0.000000 | `unconditional` |
| `Attack_ParryAid_L_Start` | `Attack_ParryAid_L` | 0.287356 / fixed=true 0.000000 | `FrameCount mode=4 value=50; Trigger_Parry_L mode=1 value=0` |
| `Attack_ParrySolo_H` | `Attack_ParrySolo_H` | 0.200000 / fixed=true 0.000000 | `FrameCount mode=9 value=12; FrameCount mode=4 value=50; Trigger_Parry_H mode=1 value=0` |
| `Attack_ParrySolo_H` | `Attack_ParrySolo_H_End` | 0.687500 / fixed=true 0.000000 | `unconditional` |
| `Attack_ParrySolo_H` | `Attack_AssaultAid` | 0.500000 / fixed=true 0.030000 | `FrameCount mode=9 value=30; Trigger_PressAttackB mode=1 value=0; Bool_AssaultAid_Enable mode=1 value=0` |
| `Attack_ParrySolo_H` | `Attack_AssaultAid` | 0.500000 / fixed=true 0.030000 | `FrameCount mode=9 value=30; Trigger_PressAttackA mode=1 value=0; Bool_AssaultAid_Enable mode=1 value=0; Bool_EasyAssaultAid mode=1 value=0` |
| `Attack_ParrySolo_H_End` | `Attack_AssaultAid` | 0.500000 / fixed=true 0.030000 | `FrameCount mode=9 value=30; Trigger_PressAttackB mode=1 value=0; Bool_AssaultAid_Enable mode=1 value=0` |
| `Attack_ParrySolo_H_End` | `Attack_AssaultAid` | 0.500000 / fixed=true 0.030000 | `FrameCount mode=9 value=30; Trigger_PressAttackA mode=1 value=0; Bool_AssaultAid_Enable mode=1 value=0; Bool_EasyAssaultAid mode=1 value=0` |
| `Attack_ParrySolo_L` | `Attack_ParrySolo_L_End` | 0.687500 / fixed=true 0.000000 | `unconditional` |
| `Attack_ParrySolo_L` | `Attack_AssaultAid` | 0.250000 / fixed=true 0.030000 | `FrameCount mode=9 value=10; Trigger_PressAttackB mode=1 value=0; Bool_AssaultAid_Enable mode=1 value=0` |
| `Attack_ParrySolo_L` | `Attack_AssaultAid` | 0.250000 / fixed=true 0.030000 | `FrameCount mode=9 value=10; Trigger_PressAttackA mode=1 value=0; Bool_AssaultAid_Enable mode=1 value=0; Bool_EasyAssaultAid mode=1 value=0` |
| `Attack_ParrySolo_Start` | `Attack_ParrySolo_Start` | 0.287356 / fixed=true 0.100000 | `FrameCount mode=9 value=78; Trigger_PressAid mode=1 value=0` |
| `Attack_ParrySolo_Start` | `Attack_ParrySolo_H` | 0.229358 / fixed=true 0.000000 | `FrameCount mode=4 value=72; Trigger_Parry_H mode=1 value=0` |
| `Attack_ParrySolo_Start` | `Attack_ParrySolo_L` | 0.229358 / fixed=true 0.000000 | `FrameCount mode=4 value=72; Trigger_Parry_L mode=1 value=0` |
| `Attack_ParrySolo_Start` | `Attack_ParrySolo_Start` | 0.172414 / fixed=true 0.100000 | `FrameCount mode=9 value=30; FrameCount mode=4 value=60; Bool_HoldAid mode=2 value=0` |
| `Attack_ParrySolo_Start` | `Attack_ParrySolo_Start` | 0.344828 / fixed=true 0.100000 | `FrameCount mode=6 value=60; Bool_HoldAid mode=2 value=0` |
| `Evade_Back` | `Attack_Counter` | 0.400000 / fixed=true 0.100000 | `FrameCount mode=4 value=56; Trigger_PressAttackA mode=1 value=0; Trigger_PerfectEvade mode=1 value=0` |
| `Evade_Back` | `Attack_Rush` | 0.400000 / fixed=true 0.100000 | `FrameCount mode=9 value=5; FrameCount mode=4 value=56; Trigger_PressAttackA mode=1 value=0; Bool_Badge_S01 mode=2 value=0` |
| `Evade_Back` | `Attack_Rush_Enhance` | 0.892857 / fixed=true 0.100000 | `FrameCount mode=9 value=5; FrameCount mode=4 value=56; Trigger_PressAttackA mode=1 value=0; Bool_Badge_S01 mode=1 value=0` |
| `Evade_Back` | `Run_Loop` | 0.400000 / fixed=true 0.200000 | `Bool_IsMoving mode=1 value=0` |
| `Evade_Front` | `Attack_Counter` | 0.171429 / fixed=true 0.100000 | `FrameCount mode=4 value=24; Trigger_PressAttackA mode=1 value=0; Trigger_PerfectEvade mode=1 value=0` |
| `Evade_Front` | `Attack_Rush` | 0.171429 / fixed=true 0.100000 | `FrameCount mode=9 value=5; FrameCount mode=4 value=24; Trigger_PressAttackA mode=1 value=0; Bool_Badge_S01 mode=2 value=0` |
| `Evade_Front` | `Attack_Rush_Enhance` | 0.892857 / fixed=true 0.100000 | `FrameCount mode=9 value=5; FrameCount mode=4 value=24; Trigger_PressAttackA mode=1 value=0; Bool_Badge_S01 mode=1 value=0` |
| `Evade_Front` | `Run_Loop` | 0.162000 / fixed=true 0.050000 | `Bool_IsMoving mode=1 value=0` |
| `HitFly_B` | `Evade_Back` | 0.457317 / fixed=true 0.000000 | `Bool_ForcedDodge mode=1 value=0; Trigger_PressEvade mode=1 value=0` |
| `HitFly_F` | `Evade_Back` | 0.931193 / fixed=true 0.000000 | `Bool_ForcedDodge mode=1 value=0; Trigger_PressEvade mode=1 value=0` |
| `Hit_H_B` | `Evade_Back` | 0.681818 / fixed=true 0.000000 | `Bool_ForcedDodge mode=1 value=0; Trigger_PressEvade mode=1 value=0` |
| `Hit_H_F` | `Evade_Back` | 0.931193 / fixed=true 0.000000 | `Bool_ForcedDodge mode=1 value=0; Trigger_PressEvade mode=1 value=0` |
| `Hit_L_B` | `Evade_Back` | 0.931193 / fixed=true 0.000000 | `Bool_ForcedDodge mode=1 value=0; Trigger_PressEvade mode=1 value=0` |
| `Hit_L_F` | `Evade_Back` | 0.931193 / fixed=true 0.000000 | `Bool_ForcedDodge mode=1 value=0; Trigger_PressEvade mode=1 value=0` |
| `SwitchIn_Attack` | `Attack_Normal_05` | 0.593103 / fixed=true 0.100000 | `FrameCount mode=9 value=86; FrameCount mode=4 value=116; Trigger_PressAttackA mode=1 value=0` |
| `SwitchIn_Attack` | `SwitchIn_Attack_Landed` | 0.413793 / fixed=true 0.030000 | `FrameCount mode=9 value=67; Bool_IsAttackLanded_QTE mode=1 value=0` |
| `SwitchIn_Attack` | `SwitchIn_Attack_End` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `SwitchIn_Attack_Ex` | `SwitchIn_Attack_Ex_End` | 0.864407 / fixed=true 0.200000 | `FrameCount mode=9 value=102; Bool_IsClicking mode=2 value=0; Bool_HoldAttackA mode=2 value=0` |
| `SwitchIn_Attack_Ex` | `SwitchIn_Attack_Ex_End` | 0.864407 / fixed=true 0.200000 | `FrameCount mode=9 value=102; Trigger_SawExplode mode=1 value=0` |
| `SwitchIn_Attack_Ex` | `SwitchIn_Attack_Ex_End` | 0.864407 / fixed=true 0.200000 | `FrameCount mode=9 value=102; Trigger_AttackLanded mode=1 value=0` |
| `SwitchIn_Attack_Ex` | `SwitchIn_Attack_Ex_End` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `SwitchIn_Attack_Ex_End` | `Attack_Normal_04` | 0.045226 / fixed=true 0.100000 | `Bool_Badge_S03 mode=1 value=0; Bool_HoldAttackA mode=1 value=0; FrameCount mode=9 value=9; FrameCount mode=4 value=38` |
| `SwitchIn_Attack_Ex_End` | `Attack_Normal_04` | 0.045226 / fixed=true 0.100000 | `FrameCount mode=9 value=9; FrameCount mode=4 value=38; Trigger_PressAttackA mode=1 value=0` |
| `SwitchIn_Attack_Ex_Start` | `SwitchIn_Attack_Ex` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `SwitchIn_Attack_Explode` | `Attack_Normal_05` | 0.303797 / fixed=true 0.100000 | `FrameCount mode=9 value=24; FrameCount mode=4 value=54; Trigger_PressAttackA mode=1 value=0` |
| `SwitchIn_Attack_Explode` | `SwitchIn_Attack_Explode_End` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `SwitchIn_Attack_Landed` | `SwitchIn_Attack_Explode` | 0.661017 / fixed=true 0.200000 | `FrameCount mode=9 value=78; Bool_IsClicking mode=2 value=0; Bool_HoldAttackA mode=2 value=0` |
| `SwitchIn_Attack_Landed` | `SwitchIn_Attack_Explode` | 0.661017 / fixed=true 0.200000 | `FrameCount mode=9 value=78; Trigger_SawExplode mode=1 value=0` |
| `SwitchIn_Attack_Landed` | `SwitchIn_Attack_Explode` | 0.661017 / fixed=true 0.200000 | `FrameCount mode=9 value=78; Trigger_AttackLanded mode=1 value=0` |
| `SwitchIn_Attack_Landed` | `SwitchIn_Attack_Explode_End` | 1.000000 / fixed=true 0.000000 | `unconditional` |
| `SwitchIn_Normal` | `Run_Loop` | 0.037000 / fixed=true 0.100000 | `Bool_IsMoving mode=1 value=0` |

## 主控收口规则

1. Normal Attack 先收口五段主链的连段窗口与攻击命中；不把 Rush/Branch 伪造成普通连段。
2. Branch/Rush 必须按控制器实际 Motion 引用进入 Pose 资产，并由 Timeline 拥有起止/命中帧后，再生成 Ability 状态；不允许只凭 AttackProperty 创建不可播放状态。
3. `special_*` 不作为图内状态抄录；它对应外部切换、队伍 Aid、受伤收口等宿主路由，必须先落到 3C Control/Aid 请求边界。
4. 所有状态身份保留 ZZZ 原名作为业务对照；3C 资产命名使用正式 `Corin_` 前缀，不使用 `Clone`、`Copy` 或临时序号。
5. 本表更新时必须同步 `docs/replication/corin-copy-sources.md` 的进度；没有对应正式资产落盘不得把状态写成已迁移。

## Branch/Rush 正式 PoseGraph 绑定链路评估（2026-09-19）

依据上文 BlendTree 实际 Motion 引用（不按状态名猜），两个分组共 26 状态去重后需要 19 个独立 Clip：

- BranchAttack 18 状态 → 12 Clip：Branch_01（+_End/_Explode）、Branch_02（+_End/_Explode）、Branch_Loop、Branch_Walk、Branch_03_Loop、Branch_03_Walk_Loop、Branch_03_Shake、Branch_03_Walk_Shake。Branch_03 主体/End/Explode 复用 Branch_02 系列，NotExplode 复用 Branch_01_Explode。
- RushAttack 8 状态 → 7 Clip：Rush、Rush_End、Rush_Explode、Rush_Enhance_Start、Rush_Enhance_Loop、Rush_Enhance_Explode、Rush_Enhance_End（复用 Rush_Explode）。

### RushAttack Timeline 收口（2026-09-19）

Rush 的 8 个状态 Timeline 已落盘到 Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/，继续复用 7 个实际 Motion；每个状态保留独立 TimelineId、StateId、SectionId 与 Rush / Rush_Enhance BranchId。全量 93 个 AttackProperty Logic ActionCue、18 个状态边界 Decision TreeClip 已挂到对应状态本地帧。资产身份、命中帧、边界条件和 runtime SourceFrame = TimelineFrame + 1 合同见 [corin-rush-map.md](corin-rush-map.md)。

### 缺口结论

- Clip 资产：19/19 全部已按 ZZZ 原名导入 3C（`Avatar_Female_Size01_Corin_Ani_*`，含 Inplace/Rootmotion/Weapon 变体），**导入零缺口、零改名需求**——正式绑定直接引用导入资产名，改名反而切断与导出清单的对应。`Attack_Rush_Enhance`（无 _Start 后缀）也已导入但不被这 8 个状态引用，留作证据不冒充绑定。
- Rush Timeline：8/8 已收口，本批不再有 Timeline / cue / 状态边界缺口。Branch 仍无正式 Timeline。
- Rush 剩余缺口在上游接线：Rush Ability / FSM / Control admission 未建，8 个 Timeline producer 也未接进既有 Action playback / Slot 链。

### 可实施顺序

1. **决策点（已定，2026-09-19 总控）**：RushAttack 采用与 NormalAttack 相同的 Timeline Action playback 链，不扩 locomotion Pose 状态机、不建第二套 Attack Pose 路径。Pose 侧已确认承接：`corin.full-body-action.slot` 节点在图内（FullBodyAction channel、AllowEmpty），committed samples 按 SourceId 解析、clip 无关。BranchAttack 是否同链待 Branch 建链时按同口径确认。
2. **Rush Control/Ability 建链**：Rush 8 个 Timeline 起止和 committed sample 已备，剩余是 Dodge 入口、内部转移、Normal04 handoff 和宿主路由；Branch 仍需 Timeline / Ability / Control 建链。
3. **Rush Pose 侧接线**：把 8 个 Rush Timeline producer 接进既有 ActionPlaybackInput / FullBodyAction Slot 链，不新增 Pose 状态机或第二条 Pose 路径。
4. **Branch Pose 侧接线**：材料已备但未建链；若 Branch 后续沿用同一决策，走 Timeline action playback，不把 19 个 Clip 写进 locomotion 源目录。若改走状态机，才需要为对应状态建 Clip Player 绑定和源目录条目。
5. **验证**：Rush Control / Ability / Pose 接线完成后再做 Play 侧 Replay，观察姿态混入、命中时序与 NormalAttack 回归。

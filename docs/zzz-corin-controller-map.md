# ZZZ Corin 主战斗控制器抄录对照

本文只对照主战斗控制器 `Avatar_Female_Size01_Corin_Controller__1291803240_00335DAA.json`，不包含 MainCity、NPC、UI。它是后续 Control/Ability/Timeline/Pose 抄录的唯一状态对照入口；具体资产仍按正式领域 owner 和 C# authoring 链路落盘。

## 分组结论

| 组 | 状态数 | 3C 归属 | 当前结论 |
|---|---:|---|---|
| NormalAttack | 12 | Control + Attack Ability + Timeline | 五段主链已有 3C 简化执行链；End/Explode 状态和保持键分支还未完整对齐。 |
| BranchAttack | 18 | Control + Attack Ability + Timeline + Pose | AttackProperty payload 已就位；战斗 Clip、Timeline 和 Ability 执行状态尚未进入 3C。 |
| RushAttack | 8 | Control + Attack Ability + Timeline + Pose | AttackProperty payload 已就位；战斗 Clip、Timeline 和 Ability 执行状态尚未进入 3C。 |
| AidCounterAttack | 20 | Control + Combat/Aid Ability + Timeline | 仅有效果/镜头证据，没有正式 3C Aid/Counter 执行链。 |
| EvadeHit | 8 | Control + Pose + Hit feedback | Dodge 已有正式 Ability；Hit/HitFly 还没有完整 3C 链。 |
| Switch | 11 | Control + Switch Ability | SwitchIn/SwitchOut 还没有正式 Control 请求与 Switch Ability。 |

## 状态到 Clip 对照

Clip 证据只按精确同名导出记录；没有同名 Clip 就写 `missing`，不用近似名冒充证据。同名状态仍可能引用共享 Motion，需要后续结合 Motion/BlendTree 精确对账。

| 状态组 | ZZZ 状态 | Clip 证据 | 3C 缺口 |
|---|---|---|---|
| NormalAttack | `Attack_Normal_01` | `Attack_Normal_01` | 已由现有五段简化链覆盖 |
| NormalAttack | `Attack_Normal_01_End` | `Attack_Normal_01_End` | 五段简化链已覆盖主命中；细分状态待对齐 |
| NormalAttack | `Attack_Normal_02` | `Attack_Normal_02` | 已由现有五段简化链覆盖 |
| NormalAttack | `Attack_Normal_02_End` | `Attack_Normal_02_End` | 五段简化链已覆盖主命中；细分状态待对齐 |
| NormalAttack | `Attack_Normal_03` | `Attack_Normal_03` | 已由现有五段简化链覆盖 |
| NormalAttack | `Attack_Normal_03_End` | `Attack_Normal_03_End` | 五段简化链已覆盖主命中；细分状态待对齐 |
| NormalAttack | `Attack_Normal_03_Explode` | `Attack_Normal_03_Explode` | 五段简化链已覆盖主命中；细分状态待对齐 |
| NormalAttack | `Attack_Normal_04` | `Attack_Normal_04` | 已由现有五段简化链覆盖 |
| NormalAttack | `Attack_Normal_04_End` | `Attack_Normal_04_End` | 五段简化链已覆盖主命中；细分状态待对齐 |
| NormalAttack | `Attack_Normal_05` | `Attack_Normal_05` | 已由现有五段简化链覆盖 |
| NormalAttack | `Attack_Normal_05_End` | `Attack_Normal_05_End` | 五段简化链已覆盖主命中；细分状态待对齐 |
| NormalAttack | `Attack_Normal_05_End_2` | `missing` | 五段简化链已覆盖主命中；细分状态待对齐 |
| BranchAttack | `Attack_Branch_01` | `Attack_Branch_01` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| BranchAttack | `Attack_Branch_01_End` | `Attack_Branch_01_End` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| BranchAttack | `Attack_Branch_01_Explode` | `Attack_Branch_01_Explode` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| BranchAttack | `Attack_Branch_01_NotExplode` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| BranchAttack | `Attack_Branch_02` | `Attack_Branch_02` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| BranchAttack | `Attack_Branch_02_End` | `Attack_Branch_02_End` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| BranchAttack | `Attack_Branch_02_Explode` | `Attack_Branch_02_Explode` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| BranchAttack | `Attack_Branch_02_Loop` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| BranchAttack | `Attack_Branch_02_Walk` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| BranchAttack | `Attack_Branch_03` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| BranchAttack | `Attack_Branch_03_End` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| BranchAttack | `Attack_Branch_03_Enhance_Loop` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| BranchAttack | `Attack_Branch_03_Enhance_Walk` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| BranchAttack | `Attack_Branch_03_Explode` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| BranchAttack | `Attack_Branch_03_Loop` | `Attack_Branch_03_Loop` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| BranchAttack | `Attack_Branch_03_Shake` | `Attack_Branch_03_Shake` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| BranchAttack | `Attack_Branch_03_Walk` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| BranchAttack | `Attack_Branch_03_Walk_Shake` | `Attack_Branch_03_Walk_Shake` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| RushAttack | `Attack_Rush` | `Attack_Rush` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| RushAttack | `Attack_Rush_End` | `Attack_Rush_End` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| RushAttack | `Attack_Rush_Enhance` | `Attack_Rush_Enhance` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| RushAttack | `Attack_Rush_Enhance_End` | `Attack_Rush_Enhance_End` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| RushAttack | `Attack_Rush_Enhance_Explode` | `Attack_Rush_Enhance_Explode` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| RushAttack | `Attack_Rush_Enhance_Explode_End` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| RushAttack | `Attack_Rush_Enhance_Loop` | `Attack_Rush_Enhance_Loop` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| RushAttack | `Attack_Rush_Explode` | `Attack_Rush_Explode` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_AssaultAid` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| AidCounterAttack | `Attack_AssaultAid_End` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| AidCounterAttack | `Attack_BeHitAid` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| AidCounterAttack | `Attack_BeHitAid_End` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| AidCounterAttack | `Attack_BeHitAid_Explode` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| AidCounterAttack | `Attack_Counter` | `Attack_Counter` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_Counter_End` | `Attack_Counter_End` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_Counter_Explode` | `Attack_Counter_Explode` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_Counter_NotExplode` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| AidCounterAttack | `Attack_ParryAid_H` | `Attack_ParryAid_H` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_ParryAid_H_End` | `Attack_ParryAid_H_End` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_ParryAid_H_Start` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| AidCounterAttack | `Attack_ParryAid_L` | `Attack_ParryAid_L` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_ParryAid_L_End` | `Attack_ParryAid_L_End` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| AidCounterAttack | `Attack_ParryAid_L_Start` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| AidCounterAttack | `Attack_ParrySolo_H` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| AidCounterAttack | `Attack_ParrySolo_H_End` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| AidCounterAttack | `Attack_ParrySolo_L` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| AidCounterAttack | `Attack_ParrySolo_L_End` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| AidCounterAttack | `Attack_ParrySolo_Start` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| EvadeHit | `Evade_Back` | `Evade_Back` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| EvadeHit | `Evade_Front` | `Evade_Front` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| EvadeHit | `HitFly_B` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| EvadeHit | `HitFly_F` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| EvadeHit | `Hit_H_B` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| EvadeHit | `Hit_H_F` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| EvadeHit | `Hit_L_B` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| EvadeHit | `Hit_L_F` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| Switch | `SwitchIn_Attack` | `SwitchIn_Attack` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| Switch | `SwitchIn_Attack_End` | `SwitchIn_Attack_End` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| Switch | `SwitchIn_Attack_Ex` | `SwitchIn_Attack_Ex` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| Switch | `SwitchIn_Attack_Ex_End` | `SwitchIn_Attack_Ex_End` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| Switch | `SwitchIn_Attack_Ex_Start` | `SwitchIn_Attack_Ex_Start` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |
| Switch | `SwitchIn_Attack_Explode` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| Switch | `SwitchIn_Attack_Explode_End` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| Switch | `SwitchIn_Attack_Landed` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| Switch | `SwitchIn_Normal` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| Switch | `SwitchOut_Attack` | `missing` | 缺精确同名 Clip；先查 Motion/BlendTree，不能伪造资产 |
| Switch | `SwitchOut_Normal` | `SwitchOut_Normal` | Clip 可导入；需要 Timeline、Pose binding 与 Ability 状态 |

## 非表现 fallback 转移

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
2. Branch/Rush 必须等战斗 Clip 进入正式 Pose 资产、Timeline 拥有起止/命中帧后，再生成 Ability 状态；不允许只凭 AttackProperty 创建不可播放状态。
3. `special_*` 不作为图内状态抄录；它对应外部切换、队伍 Aid、受伤收口等宿主路由，必须先落到 3C Control/Aid 请求边界。
4. 所有状态身份保留 ZZZ 原名作为业务对照；3C 资产命名使用正式 `Corin_` 前缀，不使用 `Clone`、`Copy` 或临时序号。
5. 本表更新时必须同步 `docs/zzz-corin-copy-sources.md` 的进度；没有对应正式资产落盘不得把状态写成已迁移。

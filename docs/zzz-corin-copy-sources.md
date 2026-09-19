# ZZZ Corin 数据抄录源清单

Replay 闭环已 `matched:1121`。本清单只登记已存在的 ZZZ 证据源、目标领域与优先级；不把未证实行为直接当成 3C 规则。

## 已确认源数据

### 1. Animator Controller / 状态机

- 源目录：`D:/ZZZ_Dump/output/corin_replication/20260903_controller_structured_v6`
- Manifest：`structured-controller-manifest.json`
- 主战斗控制器：`Avatar_Female_Size01_Corin_Controller__1291803240_00335DAA.json`
- 结构：`4 StateMachines / 101 Clips / 81 Parameters`
- 其他控制器：MainCity、NPC、UI 共 5 份，可用于区分战斗与展示状态来源。
- 目标领域：Control 状态、Ability 转移条件、Timeline 起止、Pose 状态选择。
- 抄录优先级：最高；先对齐战斗主控制器，不抄 UI/NPC 表现。
- 当前状态对照：[zzz-corin-controller-map.md](zzz-corin-controller-map.md)

### 2. Animation Clip

- 源目录：`D:/ZZZ_Dump/output/corin_replication/20260903_animations_all_v2`
- Manifest：`export-manifest.json`
- Schema：`corin-animation-export/5`
- 目标 Unity：`2022.3.62f2c1`
- 关键目录：`animations/Avatar_Female_Size01_Corin_Ani_*.anim`
- 覆盖：Attack、Evade、Hit、Locomotion、Gacha/Galgame 等。
- 目标领域：Timeline Clip、PoseGraph Clip Player、RootMotion、攻击时间轴。
- 优先级：先战斗动作，后展示动作。

### 3. Attack AnimEvents / AttackProperty

- 解码源：`D:/ZZZ_Dump/output/corin_replication/20260904_attack_decoded_probe/AttackProperty_Avatar_Female_Size01_Corin.json`
- Manifest：同目录 `manifest.json`
- 根类型：`Dictionary<string, ConfigEntityAnimEvent>`
- 条目数：`30964`
- 目标领域：Ability 打击帧、Timeline Marker、EventGraph、受击/相机反馈触发。
- 优先级：最高；这是把 ZZZ 动作节奏转成 3C Timeline Marker 的主要证据。

### 4. Config / Effect / Controller 配置

- 源目录：`D:/ZZZ_Dump/output/corin_replication/20260904_config_decoded_v6`
- Manifest：`manifest.json`
- 解码文件数：`188`
- 内容：`Config_Corin`、`Eff_Corin_*`、Attack Rush/Trail/QTE 等 JSON。
- 目标领域：Ability 参数、Timeline 表现事件、VFX/Trail 后续正式 domain。
- 优先级：先取 Config/Attack 相关，再处理纯 Effect；Effect 没有正式 domain 前只留证据，不伪造命令。

### 5. Camera TypeTree / 曲线

- 源目录：`D:/ZZZ_Dump/output/corin_replication/20260904_camera_typetree_v1`
- Manifest：`manifest.json`
- 源块：`1223166135`、`1387972831`、`1840230881`、`2158315646`
- 目标领域：Camera Profile、Camera Curve、Timeline Camera Cue。
- 优先级：在攻击 Timeline Marker 对齐后接入。

### 6. PIK / Foot IK / 状态机补充证据

- 源目录：`D:/ZZZ_Dump/PIK分析包`
- 关键文档：
  - `状态机转移表.md`
  - `Foot-Toe与写回链静态闭环.md`
  - `走动IK与SmoothKnee.md`
  - `常量全表.md`
  - `姿态混合尾段.md`
- 目标领域：PoseGraph Foot Placement、 IK 常量、Locomotion 混合边界。
- 优先级：只抄有源码/汇编/运行时证据的常量与顺序，不复刻未命名实现细节。

## 3C 目标入口

- 正式 Corin 配置：`3cDemo/Client/3C_Client/Assets/Configs/Character/Corin/Pipeline`
- Ability 数据：`Pipeline/Abilities`
- Shared Timeline：`Pipeline/Graphs/SharedTimelines`
- RootMotion Timeline：`Pipeline/Motion/RootMotion`
- Pose / Foot / Camera Profile：`Pipeline/Presentation`
- 正式代码生成：`Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/CodeGeneration/Generated`

## 执行顺序

1. 主战斗 Controller 的状态、参数、转移映射成 Control/Ability/Timeline 消费计划。
2. `AttackProperty` AnimEvents 抽成 Timeline Marker 和 Ability 打击帧。
3. 对齐战斗 `.anim` Clip、RootMotion、PoseGraph Clip Player。
4. 接入 Camera TypeTree 曲线到 Camera Profile / Timeline Camera Cue。
5. 最后核对 PIK 常量与 Foot Placement Profile，不引入第二套 Foot 求解。

## 约束

- 不使用临时 JSON bridge 直接喂 runtime。
- 不新增第二套 Timeline、Pose、Camera domain。
- ZZZ 私有结构必须先转成 3C 正式资产/代码入口。
- UI、NPC、MainCity 控制器只作来源区分，不直接进战斗链路。

## 抄录进度

2026-09-19：Corin 五段普通攻击的 ZZZ Controller 战斗事件时间已进入正式 BTSMTL Timeline。`CorinAttackGameplayAbilityDefinition` 中新增 5 条 Logic `AttackProperty` ActionCue 轨道，共 66 个 Cue；CueId 使用原始 `Corin_Attack_Normal_xx_AttackProperty_*` key，帧位保留 ZZZ 战斗事件帧。该批先解决命中/表现触发时序；AttackProperty payload 由后续批次继续补齐。

同 Trace `369327502f7a4add8a21a19a7713d24d` 在数据写入后复跑 1121 帧，Replay 结果仍为 `matched:1121`，无分歧帧。

2026-09-19：首批五段普通攻击的 20 个唯一 AttackProperty key 导入为正式 `GameplayEffectDefinition` 资产，目录为 `Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties`。每个 Effect 携带正式攻击碰撞和攻击属性组件；伤害、破盾、元素积蓄、毁灭值、顿帧、目标阵营、命中效果编号、目标键和碰撞形状进入 Fixed/Float32 GameplayEffect catalog codec。相机、VFX、音频仍留给各自领域消费，不塞进 GameplayEffect。首批 20 个 Effect 先登记到 Corin Character Gameplay Effect Profile 和 CorinAttackGameplayAbilityDefinition，并通过正式 authoring code 引用。

2026-09-19：AttackProperty 正式 Effect 进入 Ability 依赖目录后，同 Trace Replay 逐帧对账仍为 1121 帧全部匹配、0 帧分歧。Aggregate hash 因正式内容版本变化而变化，属于预期；该证据已记入 Replay closure 文档。

2026-09-19：AttackProperty 导入范围扩展到 ZZZ Corin 全量 `108` 个 key；目录保持 `Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties`。`GroundHitEffectId`、`SkyHitEffectId`、`DownHitEffectId` 按原始无符号效果编号进入 `GameplayEffectDefinition`、Fixed/Float32 Portable catalog 和 Runtime binding。Effect Profile 现登记 `109` 个定义：`108` 个 AttackProperty 加既有 `CorinDamageEffect`。Ability 依赖由 `Attack.FixedData.asset`、`Attack.Float32Data.asset` 和正式 Ability asset 承载。

Timeline 侧边界同步收口：ActionCue 只在 Logic commit 后发布 `CueType=AttackProperty` 的稳定领域事件；`CueId` 继续保留原始 `Corin_Attack_*_AttackProperty_*` key。Timeline runtime 不解析命中效果编号、碰撞形状或属性数值；这些 payload 由 GameplayEffect Profile 与 Ability 执行域消费。

2026-09-19：主战斗控制器状态对照已建立，`101` 个状态中 `97` 个已有 `.anim` Motion、`2` 个只有 raw `.dat`、`2` 个无 Motion。Normal/Branch/Rush 已按 BlendTree 实际 Clip 引用对账，非同名与共享 Motion 逐行标注；详见 [zzz-corin-controller-map.md](zzz-corin-controller-map.md)。

2026-09-19：Timeline 复核确认现有五段 Normal Timeline 只能粗表达 Attack 1/2/4 线性 End；Attack3 Explode、Attack5 End/End_2 分支、End2 Timeline 绑定和状态本地 cue 重映射是具名缺口。Branch/Rush 不混入现有五段 Timeline；详见 [zzz-corin-normal-attack-timeline-review.md](zzz-corin-normal-attack-timeline-review.md)。

2026-09-19：Normal Attack 细分状态继续收口。Attack3 `Explode` 使用正式状态分段承载 frame=1 本地 cue；Attack5 在 frame=47 通过 `Attack5EndBoundary` 选择 `End` / `End_2`，`Attack5End2` 绑定 `Attack_Normal_05_B` 并挂 15 个状态本地 AttackProperty cue。同 Trace 1121 帧逐帧 matched、aggregate matched。下一批优先评估 Branch/Rush 的正式 Control + Timeline + Pose 链路；不把 Branch/Rush 伪装成普通五段连段。

2026-09-19：Branch/Rush PoseGraph 绑定评估完成：26 个状态去重后需要 19 个独立 Clip，全部按 ZZZ 原名导入 3C，导入零缺口；Corin Pose 源目录当前 0 条 Attack 条目，真正缺口在 Timeline/Ability/Control 未建链。按防分裂原则，Branch/Rush 走与 NormalAttack 相同的 Timeline Action playback 链，不扩 locomotion Pose 状态机。下一批先做 RushAttack 8 状态，BranchAttack 后置。
2026-09-19：RushAttack 正式链路方案已登记到 `openspec/changes/add-corin-rush-attack-formal-chain`：8 个状态独立 Ability/Timeline，7 个 Motion 按实际引用复用，93 个状态本地 AttackProperty cue 和 source-frame 边界已对账；Pose 继续走 Timeline Action playback，不扩 locomotion 状态机。

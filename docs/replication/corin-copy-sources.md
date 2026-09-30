# ZZZ Corin 数据抄录源清单

无输入 Replay 已删除，真实移动与动作回放闭环尚未完成。本清单只登记已存在的 ZZZ 证据源、目标领域与优先级；不把未证实行为直接当成 3C 规则。

## 已确认源数据

### 1. Animator Controller / 状态机

- 源目录：`D:/ZZZ_Dump/output/corin_replication/20260903_controller_structured_v6`
- Manifest：`structured-controller-manifest.json`
- 主战斗控制器：`Avatar_Female_Size01_Corin_Controller__1291803240_00335DAA.json`
- 结构：`4 StateMachines / 101 Clips / 81 Parameters`
- 其他控制器：MainCity、NPC、UI 共 5 份，可用于区分战斗与展示状态来源。
- 目标领域：Control 状态、Ability 转移条件、Timeline 起止、Pose 状态选择。
- 抄录优先级：最高；先对齐战斗主控制器，不抄 UI/NPC 表现。
- 当前状态对照：[corin-controller-map.md](corin-controller-map.md)
- Rush 正式链（2026-09-19 定）：走与 NormalAttack 相同的 Timeline Action playback 链，不扩 Pose 状态机；Pose 侧 `corin.full-body-action.slot`（FullBodyAction channel、AllowEmpty）已具备、零新增，等待 Rush Timeline producer（7 Motion，状态→Motion 对照见 controller-map）。

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
- Manifest 口径：`247` 个输入，`187` 个解码成功，`60` 个错误；目录中 `188` 个 JSON 是 `187` 个结果加 manifest 自身。
- Effect 边界：`Eff_Corin_*` 的已解内容是 Animator state 时间段配置，不是粒子、Prefab、材质等视觉资产本体。
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


2026-09-19：首批五段普通攻击的 20 个唯一 AttackProperty key 导入为正式 `GameplayEffectDefinition` 资产，目录为 `Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties`。每个 Effect 携带正式攻击碰撞和攻击属性组件；伤害、破盾、元素积蓄、毁灭值、顿帧、目标阵营、命中效果编号、目标键和碰撞形状进入 Fixed/Float32 GameplayEffect catalog codec。相机、VFX、音频仍留给各自领域消费，不塞进 GameplayEffect。首批 20 个 Effect 先登记到 Corin Character Gameplay Effect Profile 和 CorinAttackGameplayAbilityDefinition，并通过正式 authoring code 引用。

2026-09-19：AttackProperty 正式 Effect 已进入 Ability 依赖目录；其实际触发和运行结果仍需包含动作请求的 Replay 证据。

2026-09-19：AttackProperty 导入范围扩展到 ZZZ Corin 全量 `108` 个 key；目录保持 `Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties`。`GroundHitEffectId`、`SkyHitEffectId`、`DownHitEffectId` 按原始无符号效果编号进入 `GameplayEffectDefinition`、Fixed/Float32 Portable catalog 和 Runtime binding。Effect Profile 现登记 `109` 个定义：`108` 个 AttackProperty 加既有 `CorinDamageEffect`。Ability 依赖由 `Attack.FixedData.asset`、`Attack.Float32Data.asset` 和正式 Ability asset 承载。

Timeline 侧边界同步收口：ActionCue 只在 Logic commit 后发布 `CueType=AttackProperty` 的稳定领域事件；`CueId` 继续保留原始 `Corin_Attack_*_AttackProperty_*` key。Timeline runtime 不解析命中效果编号、碰撞形状或属性数值；这些 payload 由 GameplayEffect Profile 与 Ability 执行域消费。

2026-09-19：主战斗控制器状态对照已建立，`101` 个状态中 `97` 个已有 `.anim` Motion、`2` 个只有 raw `.dat`、`2` 个无 Motion。Normal/Branch/Rush 已按 BlendTree 实际 Clip 引用对账，非同名与共享 Motion 逐行标注；详见 [corin-controller-map.md](corin-controller-map.md)。

2026-09-19：Timeline 复核确认现有五段 Normal Timeline 只能粗表达 Attack 1/2/4 线性 End；Attack3 Explode、Attack5 End/End_2 分支、End2 Timeline 绑定和状态本地 cue 重映射是具名缺口。Branch/Rush 不混入现有五段 Timeline；详见 [normal-attack-timeline-review.md](../archive/records/combat/normal-attack-timeline-review.md)。

2026-09-19：Normal Attack 细分状态继续收口。Attack3 `Explode` 使用正式状态分段承载 frame=1 本地 cue；Attack5 在 frame=47 通过 `Attack5EndBoundary` 选择 `End` / `End_2`，`Attack5End2` 绑定 `Attack_Normal_05_B` 并挂 15 个状态本地 AttackProperty cue。下一批优先评估 Branch/Rush 的正式 Control + Timeline + Pose 链路；不把 Branch/Rush 伪装成普通五段连段。

2026-09-19：Branch/Rush PoseGraph 绑定评估完成：26 个状态去重后需要 19 个独立 Clip，全部按 ZZZ 原名导入 3C，导入零缺口；Corin Pose 源目录当前 0 条 Attack 条目，真正缺口在 Timeline/Ability/Control 未建链。按防分裂原则，Branch/Rush 走与 NormalAttack 相同的 Timeline Action playback 链，不扩 locomotion Pose 状态机。下一批先做 RushAttack 8 状态，BranchAttack 后置。
2026-09-19：RushAttack 正式链路方案已登记到 `openspec/changes/add-corin-rush-attack-formal-chain`：8 个状态独立 Ability/Timeline，7 个 Motion 按实际引用复用，93 个状态本地 AttackProperty cue 和 source-frame 边界已对账；Pose 继续走 Timeline Action playback，不扩 locomotion 状态机。

2026-09-19：RushAttack Timeline 8 个状态已正式收口到 Assets/Configs/Character/Corin/Pipeline/Timelines/RushAttack/。7 个实际 Motion 继续按控制器实际引用复用，8 个播放身份和 Rush / Rush_Enhance BranchId 保持独立；93 个 AttackProperty Logic ActionCue 和 18 个状态边界 Decision TreeClip 已挂到状态本地帧。全量资产身份、TimelineId、SectionId、命中帧和边界见 [corin-rush-map.md](corin-rush-map.md)。Rush 现在只剩 Ability / FSM / Control admission 和 Timeline producer 到 Pose Action playback 的接线；命中 payload 仍归 GameplayEffect / Ability，不进 Timeline。

2026-09-19：RushAttack 独立 Ability/FSM 已落盘：新增正式 Admission Profile 与 `CorinRushAttackGameplayAbilityDefinition`，8 个状态绑定 8 条 Timeline producer，8 个 Rush AttackProperty Effect 进入 Ability 依赖；Dodge/Control 激活 owner、Frame 级条件数据源和 `special_*` 宿主路由未接前保持显式不可用。详见 [corin-rush-map.md](corin-rush-map.md)。

2026-09-29：非相机角色业务复刻缺口复核完成。原作 108 份 AttackProperty 已全部导入正式 GameplayEffect 资产，Effect Profile 登记 109 个定义；但当前正式 Ability 依赖只消费其中 29 份：Normal 20、Rush 3、Branch 6。其余 79 份只是已导入数据，不属于任何可运行 Corin 技能。更重要的是，当前 Attack/Rush/Branch 生成作者源码只 resolve 这些 `GameplayEffectDefinition`，没有 `ApplyGameplayEffect` 节点；`PortableAttackCollisionComponent` 和 `PortableAttackPropertyComponent` 也没有碰撞求解、命中结果、跨 Actor 结算或 GameplayFact 生产者。因此“导入 108 份”不能表述为“技能命中已复刻”。

当前正式输入 Profile 只有 `Attack`、`Dodge`、`Branch` 三个请求；`CorinCharacterControlModule` 正式技能只注册 `Attack`、`BranchAttack`、`RushAttack`、`DodgeBack`、`DodgeForward`。对应的正式 Ability/Timeline 只覆盖普通攻击、Rush、Branch 和前后闪避。没有 Parry/Aid/Counter/Switch 的 Control 请求、Admission Profile、Ability、FSM、Timeline producer 或宿主路由。

同日复核 ZZZ Corin 特效解码边界：v6 配置层已解出 `129` 个唯一 `Eff_Corin_*` JSON；原始唯一名是 `130` 个，唯一未解码名是 `Eff_Corin_Cam_SwitchIn_Attack_Ex_Volume`，manifest 中的重复来源都报 `invalid serialization byte array length -920990100 at 0x54`。这 `129` 份的根类型全部是 `MonoEffectPluginDestroy_animatorLayerStateDict`，描述 Animator state 到 `AnimatorStateTimeSegment` 的映射；`AnimatorEventPattern_Avatar_Female_Size01_Corin_Effect.json` 也已解出。但 `D:/ZZZ_Dump/output/corin_replication` 下没有 BaseName 为 `Eff_Corin*` 的 `.prefab`、`.unity`、`.asset`、`.controller` 或 `.anim` 视觉资源。3C 当前 `Assets/Configs/Character/Corin/Pipeline/GameplayEffect` 是攻击配置，不是正式 VFX domain。因此该批只能表述为“特效触发/时间段配置已解出”，不能表述为“特效视觉资产已解码”或“特效已接入 3C”。

同日继续解出视觉资源层级：`Eff_Corin` 视觉树集中在 `1068210133`、`1478113610`、`1539431670`、`2045473264`、`2702541078`、`2968853155`、`350316997`、`988944888` 这 8 个 mhy1 块。解包后得到 `129` 个唯一特效根、`298` 份 `Eff_Corin*.prefab`（同名资源在不同源块中有重复副本）。AssetRipper 项目导出在 `D:/ZZZ_Dump/output/corin_replication/20260929_effect_candidate_ripper_project_v1/ExportedProject/Assets`。粒子模块本体仍未完成结构化解码：AssetRipper 和 UnityPy 都确认 ZZZ 的 `ParticleSystem`/`ParticleSystemRenderer` 与标准 Unity 2019.4 布局不一致；当前已单独保留 `2,942` 个 ParticleSystem/Renderer 原始二进制和清单在 `D:/ZZZ_Dump/output/corin_replication/20260929_corin_effect_assets_decoded_v1/raw_modules`。因此本批可表述为“特效 Prefab 层级与 GameObject/Transform/MonoBehaviour 已解出、粒子模块 raw-only”，不能表述为“粒子参数已完全解码”或“可琳 VFX 已可接入 3C”。

2026-09-30 补充 Shuriken 原生序列化证据：AssetRipper 1.3.14 在本批样本上累计报出 `19,463` 个 `ParticleSystem` 和 `12,037` 个 `ParticleSystemRenderer` 读取错误。样本 `0425_CAB-d6652fef59830a19de7f25294d180258` 的 `ParticleSystem` 已确认不是标准 2019 静态布局：根部带 ZZZ 扩展头，且后半段模块使用 Unity 原生 optimize transfer 的条件分支。`UnityPlayer.dll` 中 `MinMaxCurve::Transfer` 位于 RVA `0x2077D0-0x207A86`：`minMaxState` 按 UInt16 对齐写入，`scalar` 和 `minScalar` 常写。`MinMaxGradient::Transfer` 位于 RVA `0x1FB470-0x1FB6E8`：state 按 UInt16 对齐写入，`minColor`/`maxColor` 按 `Color32` 各 4 字节常写，`maxGradient` 与 `minGradient` 随后写入，不是标准 typetree 中两个 16 字节 ColorRGBA。样本解析已闭合根部、`InitialModule`、`ShapeModule`、`EmissionModule`，以及 `SizeModule` 到 `ExternalForcesModule`、`ClampVelocityModule`、`NoiseModule`、`SizeBySpeedModule` 的边界。

样本 `AnimationCurve` 的 `m_Curve.size=-1` 已证实为 4 字节空本体标记：样本对象借此把 `RotationBySpeedModule` 闭合到 `3280`，`ColorBySpeedModule` 闭合到 `3640`，下一字节正是 `CollisionModule.enabled=0`。但批量验证在另一个对象先于 `SizeModule` 失配，进入 `RotationModule` 后越界；旧记录的 NoiseModule 不是当前工具的第一个失败点。state 与曲线写入还存在未收敛的模块/实例条件，不能升级为全量协议。

同日 `ParticleSystemRenderer` 原生链已按 serialized version 6 修正：不读 `_LodLevel`、`_LodMesh` 和 `_VertexStreamMask`，样本 20 个对象全部闭合到 `296`；此前 `308` 结论是把 `_LodMesh` 错读进标准段。Corin 正式范围已收窄到 8 个 mhy1 块内的 125 个 CAB 和 1,470 个 Renderer，零解析错误；总长 `336/340/344/352` 对应未命名尾部 `40/44/48/56`。Renderer 的 `m_Materials` 全部为空；Mesh 引用分为 2,923 条未命中当前 CAB 的 local、78 条 external（29 条 Unity 内建，49 条 archive）和 1,686 条 invalid fileId。archive 里只有 6 个唯一目标已命中 Mesh，材质/贴图/Mesh 依赖仍未解出。细节和剩余边界见 `docs/diagnostics/effects/effect-particle-native-transfer-20260930.md`。这条证据仍不足以宣称全部粒子参数已解码。

同日继续修正 formal 协议：首个 Corin `ParticleSystem` 通过 formal `SubModule` 扩展和完整 `LightsModule` 双曲线规则闭合到 `8088/8088`，Trail 边界闭到 `6036..6904`。Corin 全量扫描推进到 `740/1472` 后暴露 `ClampVelocityModule.drag` 未闭合；前 740 个里 83 个的拖拽曲线 state 是边界巧合产物，因此全量不能通过。Renderer 的 2019.2 TypeTree 标准段在首个样本闭到 `240`，但另一个 formal 样本在 `m_VertexStreams` 前被 ZZZ 插入字段打断；旧 `296` 依赖汇总和新的 `240` TypeTree 边界都不能当作闭合协议。GameObject/Transform 已单独组装为 `Tools/Rendering/CorinRenderData/corin-prefab-hierarchy.json`，覆盖 129 个根、1,550 个节点，Transform 父子链接零未闭合；组件类型数量与 manifest 对账，但粒子数值和 Renderer 依赖仍受协议缺口限制。

同日完成 AssetRipper 导出的 298 份 `Eff_Corin*.prefab` 引用扫描：129 个 formal 根对应 89 个双副本和 40 个三副本；工程内可解析引用只有本地层级对象和 16 类导出脚本，没有 Mesh、Material、Texture2D 或 Shader 资产。占位 GUID 共 8,913 次，其中 5,571 次是组件槽位、3,342 次是资产引用；已导出 Renderer 的 `m_Materials` 全部为空。formal 层级需要 1,470 个 Renderer，AssetRipper 所有副本最好只导出 918 个，且只有 17 个根能找到完整 Renderer 副本。因此 AssetRipper 工程只能作层级和脚本对照，不能作粒子、Renderer、Mesh、材质或贴图的正式组装来源。证据见 `docs/diagnostics/effects/effect-particle-native-transfer-20260930/corin-ripper-prefab-reference-scan.json`。

同日闭合 ZZZ version 6 的 `ParticleSystem` 和 `ParticleSystemRenderer` formal 线缆：1,472 个粒子全部按 125 个 CAB 读到 `byteSize`；Clamp drag 使用 `float scalar + 84 字节 legacy payload`，不再误读成 MinMaxCurve。1,470 个 Renderer 也全部闭合；真实顺序包含新版 Base 扩展、OrderType、RenderMode/SortMode、Pivot/Flip、四 bool、`m_VertexStreams` TypelessData、五个 Mesh PPtr 和 CustomBounds，尾部固定 `(1,1,1)`。粒子和 Renderer 数值边界已经可正式消费，但 drag payload 内部语义、跨 bundle Mesh、材质与贴图仍未组装；AssetRipper 工程仍不能作为资源源。

| 原作业务组 | 原作关键链路 | 当前已有数据 | 当前运行缺口 |
| --- | --- | --- | --- |
| `ParryAid/Solo` | `PlaceHolder` 或外部流程进入 `Attack_ParryAid_H/L_Start`；`Start` 按 `Trigger_Parry_H/L` 进入 H/L；H/L 后在窗口内按攻击请求进入 `AssaultAid`。状态携带锁敌、无敌和 Aid 标签 | 3 份 Parry AttackProperty，其中 `AttackAid`、`ParryAid`、`ParryAid_H/L/Auto` 标签和攻击碰撞/属性已导入；相关动作 Clip 已导出 | 没有格挡/Aid 请求，没有 ParrySolo 与 ParryAid 的区分入口，没有无敌窗口、锁敌窗口、H/L 判定、状态机、Timeline 和命中链。当前 OpenSpec 的“普通格挡”不是这份 ZZZ 可琳支援格挡复刻 |
| `Attack_Counter` | `Evade_Back` frame 56 或 `Evade_Front` frame 24 需要同时满足 `Trigger_PerfectEvade` 与 `Trigger_PressAttackA`；主段后进入 Explode/NotExplode 分支 | 4 份 Counter AttackProperty 与部分相机资源已导入；前后 Dodge Ability 已存在 | 没有 PerfectEvade 事实生产、Counter 请求、Counter Ability/FSM/Timeline，也没有命中应用与结算 |
| `AssaultAid` | `ParryAid/Solo` 在指定窗口按 `PressAttackA/B` 并满足 `AssaultAid_Enable`/`EasyAssaultAid` 进入；`Attack_AssaultAid` 结束后可接 `Attack_Normal_05` | 15 份 AssaultAid AttackProperty 和部分相机资源已导入；`AssaultAid`、`AssaultAid_End` 仍是 raw Motion | 没有 Aid 使能事实、连携宿主协议、Ability/FSM/Timeline、Motion 解码和命中链 |
| `BeHitAid` | 与 Aid/Counter 同属 `AidCounterAttack`，主段在命中或爆发条件下进入 Explode，结束可接 Normal 04 | 4 份 BeHitAid AttackProperty 已导入；Counter 系列 Clip 可复用 | 没有受援触发协议、Ability/FSM/Timeline、命中和受击响应链 |
| `SwitchIn/Out` | SwitchIn 普通/强化攻击是切人 QTE 业务，含 Landed、Explode、Ex_Start、TimeSlow、SpecialSkill 和 Clicking 触发 | 普通 13 份、强化 18 份 AttackProperty 已导入；11 个 Switch 状态的 Motion 均有导出 | 没有 Switch 请求、多角色切换宿主、Switch Ability/FSM/Timeline、QTE 落地事实、TimeSlow 或切人演出消费者 |
| `Hit/HitFly` | 受击 6 个状态按方向和轻重区分；`ForcedDodge + PressEvade` 可回 `Evade_Back` | Hit 方向 Clip 已导出；Dodge Ability 已存在 | 没有受击方向/等级事实、Hit/HitFly Ability/Timeline、受击表现和 ForcedDodge 消费者 |

同日结论：当前复刻状态应分三层记录。数据和状态对照层，Normal/Branch/Rush/Dodge 已有正式资产，Aid/Counter/Switch 只有原始对照与导入 Effect；技能运行层，正式 Ability 只有五类，且攻击 Effect 尚未真正 Apply；战斗结果层，攻击碰撞、命中结果、受击、格挡、无敌和跨角色结算都未闭合。后续若优先补角色业务，应先定 Aid/Counter/Switch 的宿主入口和事实协议，再建 FSM/Timeline；不要先复制相机或表现资产来伪装业务可用。

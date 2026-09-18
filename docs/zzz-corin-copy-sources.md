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

2026-09-19：Corin 五段普通攻击的 ZZZ Controller 战斗事件时间已进入正式 BTSMTL Timeline。`CorinAttackGameplayAbilityDefinition` 中新增 5 条 Logic `AttackProperty` ActionCue 轨道，共 66 个 Cue；CueId 使用原始 `Corin_Attack_Normal_xx_AttackProperty_*` key，帧位保留 ZZZ 战斗事件帧。该批数据先解决命中/表现触发时序，AttackProperty 完整伤害、碰撞、命中反馈 payload 还没有迁移。

同 Trace `369327502f7a4add8a21a19a7713d24d` 在数据写入后复跑 1121 帧，Replay 结果仍为 `matched:1121`，无分歧帧。

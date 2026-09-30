# 固定输入 Replay 证据状态（2026-09-19）

> 历史记录：保存当时实现、问题与证据；当前合同见[现行文档索引](../../../../openspec/maintenance-audit.md)，当前进展见[文档入口](../../../README.md)。文中的“当前”“待完成”和旧阻塞均属于该记录时点。

## 当前结论

真实移动、攻击、闪避的当前版本回放闭环尚未完成。不能用全零输入对账或编译成功代替玩法验证。

## 已清理的无输入录制

按用户要求，逐帧解码确认移动、视角和动作请求均为空后，删除了 2026-09-07 的 2568 帧录制、2026-09-19 的 1121 帧录制，以及后者对应的 11 份 Replay Proof；Development Center 历史失败运行中的 6 份空输入副本也已删除，失败运行日志保留。相关历史 matched 结论和产物路径不再保留为运行证据。

## 保留的正式录制

| 录制日期 | Trace ID | 帧数 | 非零移动帧 | 非零视角帧 | 动作请求记录 |
|---|---|---:|---:|---:|---|
| 2026-08-27 | `43357ff3cd384e5cba75d2c31175b116` | 1044 | 944 | 590 | 无 |
| 2026-09-04 | `f169da25c67742aaafa0e9860ae4a230` | 1492 | 540 | 644 | Attack 94 条，Dodge 2 条 |
| 2026-09-07 | `dc91fc0cf0e74a5885adc6356d0d750e` | 1604 | 277 | 282 | 无 |

统计使用 `RollbackInputCodec` v3 字段顺序逐帧解码，检查完整消费；动作请求记录可能跨帧保留，不等于独立按键次数。9 月 4 日录制确有操作，但其 ActorId 为 gameplay-lab-player，与当前 fixed-player 场景不一致；实际尝试在准备阶段失败，未形成运行基线。这些录制保留为历史真实输入，当前版本需重新录制。

## 已修复的启动错误

- Corin 有 4 个 Ability grant，但 Fixed / Float32 Ability Data 各只有 3 份，导致角色注册失败和回放启动超时。通过正式 `Republish Corin Ability Data` 入口补齐运行资产。
- 注册 Rush 正式 GameplayTag，删除 Definition 中空的 Behavior Profile 引用。
- Ability 统一语义编译补齐动作标签、准入标签查询、图中标签查询和显式 Effect 依赖的 GameplayEffect 能力声明。
- 属性上下限按 Source 只编码 Constant 或 AttributeId，与 Fixed / Float32 读取合同一致，修复后续 Effect 数量读错位置的问题。

这些代码修复已提交为 `5b6a08184`。普通 Play 曾进入 Recording 并推进，但录制被共用 Editor 的其他任务停止，未留下有效操作录制。角色共享运动被各 Ability 重复求解的问题已在主目录修正：技能提交贡献，角色统一求解，Motion Warp 保留技能归属。Fixed / Float32 同步修改，Unity 编译与重载后 Console 0 error；实际输入和动作行为仍待验证。

## 当前真实操作现场

主线已录到 Trace `92695eab609c4de4abf0fdd9006bdd85`，3870 帧，ActorId 为当前场景的 fixed-player。逐帧完整解码确认：非零移动 631 帧、非零视角 906 帧，Attack 请求 4 条、Dodge 请求 6 条。用户反馈角色仍完全不能操作。

正式 replay_start 已完整回放，Proof Run 为 `a16d74d1beb348f6b0c0d7863cb898be`。3870 帧 Body 坐标恒定，位置变化帧数为 0，Yaw 与水平速度均为 0。此证明只建立了故障基线，不能因 baseline-created 的 matched 字段为 true 而宣称玩法成功。

- 主线提交 `f0a526961` 修复技能服务晚于 Control.Tick 初始化：真实技能请求曾在读取 GameplayEffect 标签时空引用，现调整为服务初始化、事件处理、输入绑定在控制处理之前。Fixed / Float32 同步，编译通过。
- 已定位控制模块每帧新建却始终从 Idle 初始化，没有恢复状态槽中的上一帧状态。当地 UnityHFSM 先运行当前状态逻辑，再处理转移，导致走路状态始终没有输出机会。已让首次初始化恢复已有状态，保留进入 Tick、运动进度和闪避后的跑步意图。修复后使用完全相同的 3870 帧输入完成回放，Proof Run 为 `98808fc7f0194fa18a2fc06691721f1c`，位置变化 614 帧，X 范围 39.4241～48.8703，Z 范围 -3.5421～1.3712。工具报告与旧的不动基线有 1262 帧差异，这是修复产生的预期行为变化，不能删除旧证据或将其解释为已完成全部闭环。

动画表现仍有 Pose Evaluate 警告：Idle Local Pose 的源采样未完成。普通 Play 的完整可操作表现，以及攻击和闪避执行仍待确认。

## 当前改动记录

- 用户要求现在不使用 worktree，后续只在 D:/Unity_Project_1/3C 主目录继续。
- 当前 Center 名称：主目录角色运动与真实输入闭环。
- 当前 Change ID：`dd37173262704b34bb1aa04056de01a7`。

以下独立工作区记录已停止使用，没有迁回其基线提交：

- Center 名称：角色运动只求解一次与真实输入回放。
- Change ID：`e239544738624229a0e45fc2c5b59703`。
- 首次修改前运行：`addb8f65e5104379bbb78fb9d87531fd`，工作区首次 Unity 启动因 Package Manager 解析错误退出，未开始回放。失败运行保留，不作为玩法结果。

## Rush 装配后复验

- Trace：`92695eab609c4de4abf0fdd9006bdd85`，3870 帧。
- Proof：`3cDemo/Client/3C_Client/Temp/CharacterInputReplayProofs/v5/92695eab609c4de4abf0fdd9006bdd85/20260919-123210-843-e4131e670f994817b12ceaed89d1fc3d.json`。
- 帧对账：`DivergentFrameCount=0`，`FirstDivergentRelativeFrame=-1`。
- Aggregate：`mismatch`，差异字段只有 `runtime_content_hash`、`source_revision`、`semantic_hash`；这是 Rush Ability/执行数据加入正式闭包后的预期内容版本变化。
- 前置 Proof：同 Trace `20260919-113223-597-98808fc7f0194fa18a2fc06691721f1c.json` 为 Rush 装配前 `matched:3870` 基线。

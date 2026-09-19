# 固定输入 Replay 闭环证据（2026-09-19）

## 结论

2026-09-19 重新检查录制内容后，更正本页证据范围：Trace `369327502f7a4add8a21a19a7713d24d` 的 1121 帧中，`MoveAxis` 非零帧数为 0，`LookAxis` 非零帧数为 0，动作请求总数为 0。逐帧解码使用 `RollbackInputCodec` v3 的正式字段顺序，并检查每帧完整消费。

历史 `matched:1121` 只证明这段全零控制输入的回放一致，不能证明普通 Play 可操作，也不能证明 NormalAttack、Dodge、Rush、命中或状态分支已经运行。下文保留历史记录，其中动作功能收口的表述不以这份 Replay 为验证依据。

## 正式输入 Trace

- Actor：`fixed-player`
- Trace ID：`369327502f7a4add8a21a19a7713d24d`
- Schema：`character-fixed-input-trace/4`
- 帧数：`1121`
- Trace：`3cDemo/Client/3C_Client/Diagnostics/CharacterInputTraces/20260919-044020-742-369327502f7a4add8a21a19a7713d24d.json`

## Replay Proof

- Schema：`character-fixed-input-replay-proof/6`
- Baseline Proof：`3cDemo/Client/3C_Client/Temp/CharacterInputReplayProofs/v5/369327502f7a4add8a21a19a7713d24d/20260919-044157-552-acf0497997044d9ba8e7ff30ef4a0712.json`
- Compare Proof：`3cDemo/Client/3C_Client/Temp/CharacterInputReplayProofs/v5/369327502f7a4add8a21a19a7713d24d/20260919-044325-807-92ad4babc79942e2af8f8dd8f57050dc.json`
- 对账结果：`matched:1121`
- 分歧帧数：`0`
- Tick Drive：`one-fixed-tick-per-presentation-frame`
- Presentation Clock：`logic-locked`

## 前置状态

- `ThirdPersonClient.Editor.csproj` 构建：`0 error`
- Unity 刷新后 Console error：`0`
- BTSMTL Timeline、PoseGraph、Camera 领域已由对应窗口提交收口与文档同步。
- Pose 运行链路中仍有一个非阻塞 Warning：`corin.locomotion.idle did not produce the current Local Pose`。它不再中断录制或 Replay，但属于后续 Pose 领域收口项。

## 下一阶段

Replay 已 matched，可以进入 ZZZ Corin 正式数据抄录阶段；抄录必须走现有正式 authoring / runtime 链路，不新增临时迁移路径。

## AttackProperty Payload 后复验

- 新 Proof：`3cDemo/Client/3C_Client/Temp/CharacterInputReplayProofs/v5/369327502f7a4add8a21a19a7713d24d/20260919-061501-350-55800fa08aea4ec1b44c2dafb580fc07.json`。
- 帧对账：`compared:1121`，`divergent_frame_count:0`，`first_frame_mismatches:[]`。
- Aggregate 对账为 `mismatch`，差异字段只有 `runtime_content_hash`、`source_revision`、`semantic_hash`；这是新增 20 个 AttackProperty Effect 和正式 provider 合同后的预期内容变化。
- 结论：运行数据没有回归；旧 proof 不能再作为同一内容版本的 aggregate 基线，应作为“内容变更前最后 matched proof”保留。

## 全量 AttackProperty 后复验

- 新 Proof：`3cDemo/Client/3C_Client/Temp/CharacterInputReplayProofs/v5/369327502f7a4add8a21a19a7713d24d/20260919-063251-925-81d9b46f5ec34fa2b2118e060a87ebf9.json`。
- 帧对账：`compared:1121`，`divergent_frame_count:0`，`first_frame_mismatches:[]`。
- Aggregate 对账为 `mismatch`，差异字段只有 `runtime_content_hash`、`source_revision`、`semantic_hash`；这是 AttackProperty 扩展到全量 `108` 个 key、命中效果编号改为 `uint` 并重建 Fixed/Float32 Ability 数据后的预期内容变化。
- 结论：运行数据没有回归；本轮 proof 成为全量 AttackProperty 内容版本的第一份逐帧 matched 证据。

## Attack5 多余 cue 删除后复验

- 新 Proof：`3cDemo/Client/3C_Client/Temp/CharacterInputReplayProofs/v5/369327502f7a4add8a21a19a7713d24d/20260919-071255-632-9d555f9c1fb0410782f9d8317d5279a3.json`。
- 帧对账：`compared:1121`，`divergent_frame_count:0`，`first_frame_mismatches:[]`。
- Aggregate 对账为 `matched`，`aggregate_mismatches:[]`；删除 Attack5 frame=64 多余 `_01_02` cue 并重建 Ability 资产后，运行数据没有回归。

## Attack3 Explode 状态本地 cue 后复验

- 新 Proof：`3cDemo/Client/3C_Client/Temp/CharacterInputReplayProofs/v5/369327502f7a4add8a21a19a7713d24d/20260919-074258-722-ed3e575b8a2c4fe594441c80291f8987.json`。
- 帧对账：`compared:1121`，`divergent_frame_count:0`，`first_frame_mismatches:[]`。
- Aggregate 对账为 `matched`，`aggregate_mismatches:[]`；Attack3 Explode 独立状态本地 cue 收口后，运行数据没有回归。

## BranchId 合同中间态复验

- 新 Proof：`3cDemo/Client/3C_Client/Temp/CharacterInputReplayProofs/v5/369327502f7a4add8a21a19a7713d24d/20260919-083532-884-b4cadf27fd2447f69474ed693135478e.json`。
- 帧对账：`compared:1121`，`divergent_frame_count:0`，`first_frame_mismatches:[]`。
- Aggregate 对账为 `matched`，`aggregate_mismatches:[]`；Timeline Section/ActionCue 增加 `BranchId` 合同的中间态没有引入运行回归。

## Attack5 End / End_2 分支收口后复验

- 新 Proof：`3cDemo/Client/3C_Client/Temp/CharacterInputReplayProofs/v5/369327502f7a4add8a21a19a7713d24d/20260919-085836-293-d0cfb77806a54ee5a8263e90763d42e3.json`。
- 帧对账：`compared:1121`，`divergent_frame_count:0`，`first_frame_mismatches:[]`。
- Aggregate 对账为 `matched`，`aggregate_mismatches:[]`；`Attack5EndBoundary`、`Attack5End` / `Attack5End2` 状态分支、`CorinAttack5End2Timeline` 与 15 个状态本地 cue 重建后，运行数据没有回归。

## Rush Ability 资产后复验

- 新 Proof：`3cDemo/Client/3C_Client/Temp/CharacterInputReplayProofs/v5/369327502f7a4add8a21a19a7713d24d/20260919-100603-789-32d845b6af3d44edbd227f361fc7395c.json`。
- 帧对账：`matched:1121`。
- 本次新增 Corin Rush Admission Profile、`CorinRushAttackGameplayAbilityDefinition`、8 状态 Timeline producer 绑定和 8 个 Rush AttackProperty Ability 依赖；Rush 主动激活链未接入，所以现有固定输入路径不变。
- 结论：新 Rush 资产没有引入 NormalAttack Replay 回归；Unity Console `0 error`，`ThirdPersonClient.Editor.csproj` `0 error`。

## 普通 Play 启动失败复查

2026-09-19 实际操作当前主 Editor，沿失败链确认并修复：

- Corin 已有 4 个 Ability grant，但 Fixed / Float32 Ability Data 各只有 3 份。`FixedCharacterHost.OnEnable` 在加载运行数据时失败，回放随后等待会话启动超时。通过正式 `Republish Corin Ability Data` 入口补齐运行资产。
- Rush Admission Profile 使用未注册的 `Rush` 标签，Definition 还含一个空 Behavior Profile 引用。正式标签目录补齐 Rush，删除空引用。
- Ability 语义编译只根据 Apply / Remove Effect 节点声明 GameplayEffect 能力，漏掉动作标签、准入标签查询、图中标签查询和显式 Effect 依赖。修正共同编译入口，让 Fixed / Float32 按正式能力声明安装服务。
- 属性上下限的写入端同时写入 Constant 和 AttributeId，Fixed / Float32 读取端却按 Source 只读取其一，使后续 Effect 数量错位为 `16777216`。写入端改为按 Source 写对应数据，保留现有严格读取合同。

修复后，同一 Trace 实际推进到 1121 帧并生成新 Proof：`3cDemo/Client/3C_Client/Temp/CharacterInputReplayProofs/v5/369327502f7a4add8a21a19a7713d24d/20260919-104121-731-5ed7963d8b344e3492e1e40f03d2c35e.json`。逐帧分歧为 0，aggregate 与旧 Proof 的差异为 `runtime_content_hash`、`source_revision`、`semantic_hash`，因此整体状态仍为 mismatch，不能称为 matched。

随后普通 Play 进入 Recording，实测录制推进到 926 帧，未拥有 Replay Tick Drive，Console 0 error。该次录制被共用 Editor 的 Timeline 任务执行 Stop 中断，未保存为操作输入证据。移动、攻击和闪避的真实操作录制及回放尚未完成。

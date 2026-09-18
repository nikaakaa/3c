# 固定输入 Replay 闭环证据（2026-09-19）

## 结论

固定输入 Replay 闭环已跑通。第二轮同 Trace Replay 与第一轮 baseline 完成对账，结果为 `matched:1121`，无分歧帧。

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

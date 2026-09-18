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

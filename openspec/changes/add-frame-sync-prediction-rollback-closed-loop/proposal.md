# Change: 帧同步预测回滚闭环

## Why

输入合同和 transport 边界只能保证数据能到达客户端。要形成真正的预测回滚闭环，还必须把 confirmed input 接入现有本地 rollback/replay 主线，并且定义客户端预测缓冲、分歧检测、恢复点选择、追帧重放、checksum 和 correction 的统一语义。

这个 change 是串行实施的第三包。它不是单独做一个 prediction buffer，也不是单独做一个 checksum 工具，而是把“confirmed input 到达后客户端如何回到正确状态”作为一个完整闭环规划。它必须复用当前已经存在的 `PredictionInputHistory`、`PredictionSnapshotHistory`、`ILocalRollbackSynctestSimulation`、`LocalLatencyReconciliationRunner` 语义和 `CharacterFramePipeline` 主线。

本 change 的核心目标是：**真实网络 confirmed input 到达后，客户端能用同一条本地 rollback/replay 主线检测预测分歧、恢复历史快照、追帧重放、分类结果，并输出可诊断 correction。**

## What Changes

- 新增客户端 prediction network buffer 规划。
- 定义 pending outbound、predicted history、confirmed history 和 resolved input stream。
- 定义 confirmed input set 如何替换预测输入。
- 定义 divergence tick、restore tick、replay end tick 和 replay input range。
- 定义 no correction、prediction correction、replay nondeterminism、missing snapshot、missing input 等结果类型。
- 定义 confirmed input reconciliation 复用现有 `ILocalRollbackSynctestSimulation`。
- 定义 correction request 的排队与 simulation tick 消费顺序。
- 定义 strict checksum 的字段来源和 presentation drift 排除规则。
- 明确 checksum 不是状态同步，不替代字段级 comparison。
- 明确 transport callback 不直接修改角色。

## GGPO Reference

本 change 可以参考 `Ref/ggpo` 的 rollback 核心结构，但只吸收设计，不接入 C++ runtime。

主要参考点：

- `sync.h|cpp` 的 `SaveCurrentFrame`、`LoadFrame`、`CheckSimulation`、`AdjustSimulation`、`SetLastConfirmedFrame`、`IncrementFrame`，对应本项目的 snapshot history、restore、replay、confirmed tick 和 simulation tick。
- `input_queue.h|cpp` 的 confirmed input、prediction、first incorrect frame、discard confirmed frames，对应本项目的 pending/predicted/confirmed/resolved input 分层。
- GGPO Developer Guide 中 game state / game inputs 的分离原则，对应本项目“不同步相机、不同步表现层、不保存动作结果”的边界。

区别：

- GGPO 原版以 P2P 和 UDP backend 为主，本项目以 Fantasy room input authority 或 fake room 为 transport。
- GGPO callback 直接要求 save/load game state，本项目必须通过现有 `CharacterSimulationSnapshot` 和 `ILocalRollbackSynctestSimulation`。
- GGPO 的 game state 示例是整体内存 buffer，本项目 snapshot 必须按 Character frame / Locomotion / Action / motion executor / blackboard 分层。
- GGPO 的 input size 是固定 byte buffer，本项目输入要保持可读的 `FrameSyncInputFrame` / action request facts。

## Impact

- Affected specs: frame-sync-prediction-rollback-closed-loop
- Depends on:
  - frame-sync-input-authority-foundation
  - frame-sync-transport-fantasy-adapter
- Related specs:
  - local-latency-reconciliation
  - local-rollback-synctest-foundation
  - character-frame-rollback-replay
  - prediction-rollback-authority-scopes
  - simulation-tick-system
  - presentation-transform-interpolation
- Affected code later:
  - `Assets/Scripts/Simulation/Rollback`
  - future `Assets/Scripts/Simulation/FrameSync`
  - future correction coordinator
  - future checksum projector

## Closed Loop Definition

闭环不是“有一堆类”，而是以下流程可以串起来：

1. 本地输入进入 pending outbound。
2. 本地输入同时进入 predicted history。
3. transport 发送 input。
4. confirmed input set 到达。
5. confirmed history 写入。
6. resolved input stream 生成。
7. predicted 与 confirmed 字段级对比。
8. 找到 first divergence tick。
9. 从 divergence tick 前一帧 snapshot restore。
10. 使用 resolved input 追帧 replay。
11. 每 tick comparison 检查 strict mismatch。
12. 成功时输出 prediction correction。
13. 相同 resolved input 仍分叉时输出 replay nondeterminism。
14. correction request 只排队，由 simulation tick 消费。
15. strict checksum 用于多端一致性探测。

## Prediction Buffer Model

客户端 prediction buffer 分四层：

### Pending Outbound

保存已采集但尚未被 confirmed tick 覆盖的本地输入。

字段包括：

- tick
- player id
- unit id
- local input sequence
- input frame
- sent state
- ack state

它不持有 Fantasy Session，不直接发包。

### Predicted History

保存客户端已经用于本地推进的预测输入。

它可以来自：

- 本地真实输入。
- 对远端缺失输入的预测。
- 重复上一帧策略。

它不保存动作结果。

### Confirmed History

保存服务端或 fake room confirmed input。

confirmed history 一旦覆盖某 tick，就成为该 tick replay 的 resolved 候选。

### Resolved Input Stream

replay 使用的输入流：

- 若 tick 有 confirmed input，使用 confirmed。
- 若 tick 尚无 confirmed input，使用 predicted。
- 若 tick 两者都缺失，返回 missing input 诊断。

## Reconciliation Model

网络 confirmed input reconciliation 应复用本地 latency reconciliation 的核心语义。

不同点是：

- remote input 来源从 `LatencySimulator` 变为 `ConfirmedInputSet` / confirmed history。
- confirmed tick 由服务端或 fake room 推进。
- resolved input stream 可能覆盖多个 player/unit。
- diagnostic 需要区分 network input issue 和 replay nondeterminism。

不变点是：

- restore/advance/capture 通过 `ILocalRollbackSynctestSimulation`。
- replay 走 `CharacterFramePipeline`。
- action request 重新经过 input buffer。
- comparison 使用 scoped comparison。
- 相同输入仍分叉就是 nondeterminism。

## Correction Model

Correction 不是 Transform snap。

Correction 是一个请求，告诉客户端：

- 哪个 tick 发现分歧。
- 从哪个 restore tick 恢复。
- 用哪段 confirmed/resolved input 重放。
- 为什么需要 correction。
- checksum mismatch 详情是什么。

Correction 必须排队，不能在 transport callback 里直接执行。建议 apply order：

1. transport 收到 correction DTO。
2. correction coordinator 入队。
3. simulation tick phase 消费 correction。
4. 查找 restore snapshot。
5. 生成 resolved input stream。
6. restore。
7. replay。
8. compare。
9. 成功则提交逻辑结果。
10. 失败则输出 replay nondeterminism 或 missing snapshot。

## Checksum Model

Strict checksum 是一致性探测，不是同步状态。

它应该从 strict gameplay projection 生成，字段包括：

- tick
- config hash
- unit id
- root position/yaw 的量化值
- locomotion state/facts
- action facts
- motion executor strict state
- runtime blackboard strict facts
- profile-driven motion window

它必须排除：

- presentation drift
- real camera state
- Cinemachine
- Animancer runtime object
- visual-only animation drift
- debug tooling state

checksum mismatch 必须能回到字段级 comparison，不允许只输出 hash 不同。

## Non-Goals

- 不实现服务端状态同步。
- 不实现 server transform correction。
- 不新增网络专用 Character pipeline。
- 不绕过 `ILocalRollbackSynctestSimulation`。
- 不在 transport callback 里写 Transform。
- 不把 presentation drift 当成 strict checksum。
- 不用扩大容差掩盖状态分叉。

## Acceptance Criteria

- pending/predicted/confirmed/resolved 四层边界清晰。
- confirmed input 到达后能找到 divergence tick。
- confirmed input 不变时不回滚。
- confirmed input 不一致时能 restore + replay。
- replay 成功时结果为 prediction correction。
- 相同 resolved input 仍分叉时结果为 replay nondeterminism。
- missing snapshot/input 有明确诊断。
- correction 由 simulation tick 消费。
- checksum 只覆盖 strict gameplay。
- 代码边界后续实现不得创建第二 Character pipeline。

## Implementation Order

1. 定义 prediction network buffer。
2. 定义 resolved input stream。
3. 定义 confirmed input resolver。
4. 定义 network reconciliation runner 或扩展现有 runner 的输入源边界。
5. 定义 divergence tick detection。
6. 定义 restore tick selection。
7. 定义 replay classification。
8. 定义 correction queue。
9. 定义 checksum projection。
10. 添加 focused EditMode tests。
11. 添加静态边界测试。
12. 接入 fake transport。

## Review Notes

明天 review 这个 proposal 时，重点看：

1. 有没有复用现有 rollback/replay 主线。
2. 有没有把 correction 写成直接修改角色。
3. prediction correction 和 replay nondeterminism 是否区分清楚。
4. checksum 是否排除了 presentation drift。
5. 这个闭环是否能先被 fake transport 跑通。

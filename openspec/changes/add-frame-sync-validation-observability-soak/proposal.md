# Change: 帧同步验证、可观测性与长跑

## Why

预测回滚不是写完几个数据结构就算闭环。真正可用的闭环必须能在不接真实 Fantasy 的情况下先跑 fake transport，多客户端输入确认、预测、回滚、checksum 和 correction 都要能被长跑验证。否则明天 review 时只能看代码和猜测，不能看到可复现结果。

本 change 是串行实施的第四包。它把 fake transport synctest、motion determinism audit、debug observability 和 overnight soak 合并成一个正式规划包。它们本质上都在回答同一个问题：**闭环跑起来后，如何证明它稳定，如何定位它为什么不稳定。**

它不是“额外调试工具”，而是预测回滚闭环的验收层。没有这层，Fantasy 接入只会把错误从本地变成网络上更难定位的错误。

## What Changes

- 新增 fake transport multi-client synctest 规划。
- 定义 fake room 支持 latency、reorder、duplicate、missing、late input。
- 定义 overnight soak 的固定 seed、tickCount、rollback window、stopOnFailure 和 summary 输出。
- 定义 motion determinism audit：MoveLoop、TurnBack、Dodge Directional、Dodge Backstep、profile sampling、root motion profile、motion warping、CharacterController collision、moving platform。
- 定义 strict / predictive / presentation-only / unsupported motion scope。
- 定义 frame sync debug observability：tick、confirmed tick、prediction tick、pending count、rollback count、checksum mismatch、correction reason、handshake status。
- 定义低噪声日志标记和 first mismatch 输出格式。
- 定义明天 review 时应看的自动测试、日志和风险表。

## GGPO Reference

本 change 可以参考 `Ref/ggpo` 的 synctest 和 sample app：

- `Ref/ggpo/src/lib/ggpo/backends/synctest.*`：参考本地一致性检查的组织方式。
- `Ref/ggpo/src/apps/vectorwar`：参考最小 demo 如何驱动 input、advance frame、save/load state 和 checksum。
- `Ref/ggpo/src/lib/ggpo/timesync.*`：参考 frame advantage 与 recommended wait，但本项目先把它列为可选调优项，不作为第一阶段闭环硬依赖。

这些参考只用于校正测试和诊断设计，不改变本项目 fake transport / Fantasy adapter / CharacterFramePipeline 主线。

## Impact

- Affected specs: frame-sync-validation-observability-soak
- Depends on:
  - frame-sync-input-authority-foundation
  - frame-sync-transport-fantasy-adapter
  - frame-sync-prediction-rollback-closed-loop
- Related specs:
  - local-rollback-synctest-foundation
  - local-latency-reconciliation
  - prediction-rollback-authority-scopes
  - character-frame-rollback-replay
  - animation-motion-source-pipeline
  - runtime-diagnostic-logging
  - presentation-transform-interpolation
- Affected code later:
  - future fake transport synctest fixtures
  - future frame sync soak runner
  - future checksum/correction diagnostics
  - future motion determinism audit tests

## Formal Planning Boundary

这个 proposal 不拆成 fake transport、motion audit、debug overlay 三个薄 proposal，因为它们是同一个验收系统的不同面：

- fake transport 负责制造网络条件。
- prediction/rollback closed loop 负责响应网络条件。
- motion audit 负责解释 replay 是否应当 strict。
- observability 负责输出结果。
- soak 负责把这些东西跑久。

如果分开做，就会出现 fake transport 跑了但没有可读日志，或者 motion audit 发现风险但 long-run 不报告，或者 checksum mismatch 没法映射到字段差异。

## Fake Transport Synctest Model

fake transport synctest 必须模拟至少两个 client 和一个 fake room。

Fake room 可以做：

- 收集 client input。
- 按 tick 构建 confirmed input set。
- 注入 latency。
- 注入 reorder。
- 注入 duplicate。
- 注入 missing。
- 注入 late input。
- 广播 confirmed input set。
- 注入 correction。
- 收集 checksum。

Fake room 不能做：

- 模拟角色。
- 推进 Character frame。
- 生成权威 Transform。
- 运行 Locomotion / Action。
- 持有 Animator / Animancer / Cinemachine。

## Soak Model

Soak 必须可复现。

建议配置：

- `seed`
- `tickCount`
- `clientCount`
- `rollbackWindow`
- `latencyMin`
- `latencyMax`
- `reorderChance`
- `duplicateChance`
- `missingChance`
- `lateChance`
- `checksumInterval`
- `stopOnFailure`

Soak 成功时输出单条 summary：

- marker
- seed
- tickCount
- clientCount
- checkedWindows
- correctionCount
- rollbackCount
- checksumMismatchCount
- result

失败时输出 first mismatch：

- marker
- seed
- tick
- confirmed tick
- restore tick
- replay end tick
- reason
- first difference field
- prediction input summary
- confirmed input summary
- checksum summary

## Motion Determinism Audit Model

预测回滚闭环真正跑起来后，最大风险不是网络本身，而是同输入重放仍不一致。motion audit 必须提前给每个运动来源定性。

### Strict Gameplay

这些字段必须严格一致：

- root position / yaw
- Locomotion state/facts
- Action active facts
- motion executor state
- profile-driven playback window
- run latch
- action motion resolver result

### Presentation Drift

这些差异可以诊断但不决定 strict failure：

- visual-only animation normalized time
- Animancer blend visual drift
- camera shake
- Cinemachine blend
- screen effect

### Risk / Unsupported

这些必须进入风险表，不能假装已解决：

- CharacterController collision nondeterminism
- moving platform
- runtime Animator delta as sole root source
- physics contacts
- non-deterministic target selection
- unordered collection iteration affecting gameplay

## Observability Model

Frame sync diagnostics 必须低噪声、可 grep、可用于 overnight。

建议固定标记：

- `FRAME_SYNC_HANDSHAKE`
- `FRAME_SYNC_CONFIRMED_INPUT`
- `FRAME_SYNC_CORRECTION`
- `FRAME_SYNC_CHECKSUM`
- `FRAME_SYNC_SOAK_RESULT`
- `FRAME_SYNC_FIRST_MISMATCH`
- `FRAME_SYNC_MOTION_AUDIT`

Debug snapshot 可以包含：

- local tick
- confirmed tick
- prediction tick
- pending outbound count
- predicted history count
- confirmed history count
- correction queue count
- rollback count
- last correction reason
- checksum mismatch count
- handshake status
- transport status

这些诊断数据只能读取 runtime 状态，不能写入 gameplay snapshot。

## Non-Goals

- 不实现正式 UI overlay。
- 不新增正式角色 prefab 上的 debug dependency。
- 不清理已有 log。
- 不把 debug diagnostic 写进 rollback snapshot。
- 不把 fake transport 当服务端业务代码。
- 不把 motion risk 用容差掩盖。

## Acceptance Criteria

- fake transport 可以跑两个或多个 client。
- fake transport 可以注入 latency/reorder/duplicate/missing/late input。
- soak 可以固定 seed 复现。
- 成功输出单条 summary。
- 失败输出 first mismatch。
- motion audit 能列出 strict/presentation/risk 字段。
- debug observability 不进入 gameplay snapshot。
- long-run 不需要手动看屏幕才能判断 pass/fail。

## Implementation Order

1. fake transport synctest。
2. fixed seed input generator。
3. confirmed input multi-client loop。
4. latency/reorder/duplicate/missing/late 注入。
5. checksum mismatch 注入。
6. correction 注入。
7. soak runner。
8. first mismatch formatter。
9. summary formatter。
10. motion determinism audit table。
11. static boundary tests。
12. overnight command documentation。

## Review Notes

明天 review 这个 proposal 时，重点看：

1. 它是否真的能跑完整闭环，而不是只测 DTO。
2. 失败日志是否能定位到 tick、restore tick、reason 和字段。
3. motion 风险是否诚实列出，没有被写成已解决。
4. debug tooling 是否没有污染正式角色 runtime。

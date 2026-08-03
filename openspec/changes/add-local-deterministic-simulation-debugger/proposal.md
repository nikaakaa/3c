# Change: add-local-deterministic-simulation-debugger

## Why

当前先暂停帧率排查，优先补本地调试能力。现在 `GameplayTickSystem` 只按实时 accumulator 推进；`RuntimeDebugSession` 只保存 Trace 和只读 Capture；Local Fixed Source 只声明 Forward 执行，不保存本地历史，也没有正式 restore/replay 控制口。

这导致调试动作、KCC、Timeline、AI 输入和战斗窗口时只能靠实时 Play 跑过去，不能暂停到某一 Tick、单 Tick 推进、变速观察、记录一段输入后重复回放。对求职 Demo 来说，这会直接降低排查效率，也让后续每加一个动作都更依赖肉眼和日志。

## What Changes

- 在 `GameplayTickSystem` 增加正式驱动策略：实时、暂停、单 Tick/多 Tick 手动推进、固定倍率播放。
- 增加 Local Simulation Debug Control Service，由 `SimulationSessionHost` 按精确 Session/target identity 注册控制端口。
- Local Fixed 调试录制显式开启后，记录每 Tick canonical input、step hash、输出摘要，并按固定间隔保存 Fixed gameplay/world/pipeline checkpoint。
- 回放通过现有 Session Source、Schedule、ExecutionPlan restore/replay 和 Backend outer transaction 执行，不直接调用 Character、WorldSolver、Animation 或 Transform。
- Debug UI 只发送 typed command 并显示状态；`RuntimeDebugSession` 继续负责 Trace、Source Map、Live/Capture 只读视图。
- 表现层不进入回放快照；回放后只消费 committed Body/Intent/Action EventId 分支，必要时执行正式 stream reset/replacement。
- 提供本地 replay artifact 的显式导出/导入合同，用于保存一段调试输入、checkpoint、hash 和 Trace 关联身份。

## Out of Scope

- 不继续做 FPS 采样、性能对比或 Unity Profiler 归因。
- 不新增 Unity batchmode、自动构建、自动发布或 PlayMode 测试。
- 不让 Float32 Local 宣称 bit-exact replay；Float32 只支持同一 Tick 控制入口下的暂停、步进和变速观察。
- 不保存骨骼 Pose、Animancer state、PoseState workspace、Slot weight 或 Presentation Frame workspace 到 replay snapshot。
- 不做运行时玩家可见的调试 UI；该能力只面向 Editor/Development。

## Existing Spec Comparison

- `gameplay-tick-system` 当前只定义实时 accumulator、fixed catch-up 和每帧 PresentationFrame。本变更修改该口径，加入正式 Debug drive policy，并明确 pause/manual 下表现时钟如何处理。
- `gameplay-simulation-pipeline` 当前要求 Standard Local Pipeline 不增加 history、restore schedule 或 replay。本变更将该约束收紧为“普通 Local 运行不记录历史”，同时给 Local Fixed 调试录制增加显式、按需、可关闭的正式 history/replay capability。
- `btsmtl-runtime-diagnostics` 当前要求 Diagnostics 只读且 scrub capture 不回滚 runtime。本变更保留该要求，新增独立调试控制服务，避免把 RuntimeDebugSession 变成执行 owner。
- `character-presentation-interpolation` 当前要求 Rollback 不保存动画 Pose。本变更沿用该边界，并把 Local Debug replay 的表现处理也纳入同一 committed stream/reset 合同。
- `close-deterministic-rollback-character-pipeline` 仍负责 Local Fixed 与 Rollback Variant 共享 Fixed Program、KCC 和 Collision 身份。本变更只增加本地调试控制和回放，不改变两个 Variant 的共享资产闭包目标。

## Business Tradeoffs

- 单 Tick 推进放在 TickSystem：调试看到的就是正式玩法 Tick 链路，代价是需要做驱动策略和命令队列，不能用一个临时按钮直接调 Session。
- 回放只对 Local Fixed 做确定性保证：能用于动作、KCC、战斗窗口和 AI 输入的可重复排查，代价是 Float32 Local 不再被包装成“看似可回放”的不可靠能力。
- checkpoint 加 input log：内存和 CPU 比每 Tick 全量 snapshot 低，seek 到中间位置要从最近 checkpoint 重放几帧；只记 Tick 下标又不够，因为黑板、Timeline、Action instance、KCC ground/support、AI state 和 World body 都会影响下一 Tick。
- 不保存骨骼 Pose：回放成本保持在 gameplay/world/pipeline 状态，代价是历史动画视觉不是逐骨骼录屏；表现通过 committed 输出重新构造。

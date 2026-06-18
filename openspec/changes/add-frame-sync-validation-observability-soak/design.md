## Context

前三个实施包分别定义输入权威、transport/Fantasy 和 prediction/rollback closed loop。第四包负责证明它们可以在复杂网络条件下持续运行，并在失败时给出可复现诊断。

## Goals

- 使用 fake transport 先跑完整闭环。
- 提供 overnight soak。
- 提供 first mismatch 诊断。
- 审计 motion determinism。
- 提供低噪声日志和 review 证据。

## Non-Goals

- 不实现真实 Fantasy server。
- 不实现正式 UI overlay。
- 不修复所有 motion 风险。
- 不把 debug tooling 变成 gameplay dependency。

## Decisions

### Decision: Fake transport soak 是 Fantasy 前置验收

原因：

- 真实网络会增加不确定性和调试成本。
- fake transport 可控、可复现、适合 overnight。
- 先证明算法闭环，再接真实 Fantasy。

### Decision: Motion audit 只定性和建测试，不隐藏风险

原因：

- CharacterController 和物理接触可能天然不确定。
- 盲目扩大容差会掩盖分叉。
- 需要知道哪些状态可以承诺 strict rollback。

### Decision: 日志必须低噪声

原因：

- overnight 输出不能刷屏。
- 明天 review 需要快速定位结果。
- 固定 marker 可以让脚本提取结果。

## Failure Categories

- handshake failed
- missing input
- duplicate input
- late input
- prediction correction failed
- replay nondeterminism
- checksum mismatch
- missing snapshot
- unsupported motion source
- transport disconnected

## Risks

- 风险：soak 只测 fake 数据，不接现有 Character frame replay。
  - 缓解：soak 必须走第三包 closed loop。
- 风险：日志输出过多。
  - 缓解：成功单行，失败首个 mismatch 详细。
- 风险：motion audit 变成文档不落测试。
  - 缓解：每个 strict/risk 分类都要有测试或静态检查任务。

## Open Questions

- overnight 默认 tickCount 设多少合适？
- 第一版是否只跑两个 client，还是直接支持 N client？
- Motion audit 风险是否需要单独生成 markdown 报告？

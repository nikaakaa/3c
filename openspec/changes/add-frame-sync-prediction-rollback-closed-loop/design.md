## Context

本地 latency reconciliation 已经证明项目可以在输入延迟条件下做预测、发现分歧、回滚和重放。真实网络接入后，本地 delayed input queue 会被 confirmed input set 替代，但 rollback 算法不应该重写。

## Goals

- 定义客户端 prediction network buffer。
- 定义 confirmed input 到 resolved input 的转换。
- 定义网络 reconciliation 与现有 rollback runner 的关系。
- 定义 correction queue 和 apply order。
- 定义 strict checksum projection。

## Non-Goals

- 不实现真实 Fantasy。
- 不实现 fake transport 长跑。
- 不实现 motion audit。
- 不实现服务端模拟。

## Decisions

### Decision: Reconciliation 复用 `ILocalRollbackSynctestSimulation`

原因：

- 当前 specs 已经要求 rollback/replay 通过该边界。
- 它能防止 network path 创建第二角色主线。
- 现有 debug runner 和 tests 已经围绕它建立。

### Decision: Correction 由 simulation tick 消费

原因：

- transport callback 可能发生在不合适的时机。
- 直接在 callback 改角色会破坏 tick phase 顺序。
- queue + tick consume 更容易测试和诊断。

### Decision: Checksum 是 strict projection

原因：

- snapshot 中存在 presentation differences。
- 视觉漂移不应该触发 gameplay correction。
- strict projection 与当前 authority scope spec 一致。

### Decision: GGPO 作为结构参考，不作为依赖

原因：

- `Ref/ggpo` 已经清楚展示 rollback netcode 的核心循环：input queue、save/load state、confirmed frame、prediction、rollback adjust。
- 当前项目已经有 Unity/C# 的 rollback 地基，直接引入 C++ GGPO 会制造运行时依赖和架构分裂。
- Fantasy 接入目标是 room input authority，不是照搬 GGPO P2P backend。

借鉴方式：

- 用 GGPO `InputQueue` 思路校正 prediction buffer 设计。
- 用 GGPO `Sync` 思路校正 check/adjust/replay 设计。
- 用 GGPO save/load callback 思路审计本项目 snapshot completeness。
- 不复用 GGPO UDP、session、buffer allocation 或 C callback API。

## Result Types

- `NoCorrectionRequired`
- `PredictionCorrection`
- `ReplayNondeterminism`
- `MissingSnapshot`
- `MissingInput`
- `HandshakeBlocked`
- `ChecksumMismatch`
- `Failure`

## Risks

- 风险：现有 local latency runner 输入源写死 `LatencySimulator`。
  - 缓解：规划输入源抽象，但保持旧构造和旧测试语义。
- 风险：checksum 覆盖字段过多导致视觉漂移误报。
  - 缓解：只从 strict gameplay projection 生成。
- 风险：correction apply 破坏现场表现。
  - 缓解：逻辑 correction 与 presentation interpolation 分离。

## Open Questions

- 第一版 resolved input stream 是否只覆盖本地 controllable unit？
- checksum 频率第一版是每 tick、每 N tick，还是只在 debug 模式？
- correction 成功后是否立即提交 visual interpolation，还是只修正逻辑根？

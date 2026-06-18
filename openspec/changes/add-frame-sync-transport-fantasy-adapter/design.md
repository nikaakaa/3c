## Context

项目目标是接入 Fantasy 网络同步，但 rollback core 必须保持纯数据和可测试。Fantasy 是 C# 分布式服务端框架，有自己的 Session、Entity、Scene、Handler、source generator 和 protocol export 规则。直接让 rollback core 引用 Fantasy 会破坏本地 fake transport 测试能力，也会让 Unity 客户端和服务端业务边界混在一起。

## Goals

- 定义 frame sync transport port。
- 让 fake transport 和 Fantasy adapter 使用同一 port。
- 规划 Fantasy Outer protocol 消息。
- 规划 Unity client adapter。
- 规划服务端 room input collector。
- 证明 transport 不推进 gameplay。

## Non-Goals

- 不实现 transport runtime。
- 不实现 Fantasy server。
- 不修改 proto。
- 不生成协议代码。
- 不实现 rollback closed loop。

## Decisions

### Decision: Transport port 属于 frame sync 边界，不属于 Fantasy

原因：

- fake transport 需要同样接入。
- rollback core 不能依赖 Fantasy。
- 后续如果换 transport，不应该改 prediction/reconciliation。

### Decision: Fantasy Handler 只写 room input queue

原因：

- 服务端第一阶段只确认输入。
- Handler 推进 gameplay 会产生第二套角色主线。
- Fantasy Entity 生命周期不应该影响 Unity 客户端 rollback snapshot。

### Decision: confirmed input set 用 server push

原因：

- 每 tick confirmed input 是服务端广播事件。
- 客户端不应该通过 RPC 逐 tick 拉取 confirmed input。
- push 更接近帧同步广播模型。

### Decision: fake transport 是正式测试适配器

原因：

- 本地长跑需要 deterministic fake room。
- 真实 Fantasy 接入前必须证明 prediction/rollback 算法已经闭环。
- fake transport 共享 port 可防止测试和真实网络分裂。

## Message Flow

### Handshake

1. Client sends handshake request.
2. Server compares protocol/config hashes.
3. Server responds accepted or rejected.
4. Client only sends gameplay input after accepted.

### Input Submit

1. Client sends `FrameSyncInputFrame`.
2. Server validates tick/player/unit/session.
3. Server writes room input queue.
4. Server may ack local sequence.

### Confirmed Broadcast

1. Server room tick confirmer builds `ConfirmedInputSet`.
2. Server broadcasts to room clients.
3. Client adapter raises transport event.
4. Prediction buffer consumes event in simulation layer.

### Correction

1. Client or server detects mismatch.
2. Correction DTO is sent.
3. Client queues correction.
4. Simulation tick consumes correction.

## Risks

- 风险：Fantasy adapter 为了方便直接调用 gameplay。
  - 缓解：静态测试禁止 handler 引用 Character runtime。
- 风险：fake transport DTO 与 proto DTO 分裂。
  - 缓解：二者必须映射同一 transport contract。
- 风险：push callback 直接 apply correction。
  - 缓解：callback 只能入队事件。
- 风险：protocol export 被遗漏。
  - 缓解：tasks 明确导出和 build 验证。

## Open Questions

- 真实项目 Fantasy 架构最终使用 Gate 还是 Map 作为 room input authority？
- confirmed input set 是每 tick 一条 push，还是允许批量 range？
- input ack 是否必须第一版实现，还是可由 confirmed tick 隐式覆盖？

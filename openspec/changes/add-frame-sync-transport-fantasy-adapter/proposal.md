# Change: 帧同步 Transport 与 Fantasy Adapter

## Why

输入权威地基定义了网络上应该承载什么输入事实，但这些事实还需要通过一个正式 transport 边界进入客户端和服务端。这个边界必须同时服务 fake transport 和 Fantasy adapter，否则会出现两套消息流：一套用于本地测试，一套用于真实网络。两套消息流很快会导致测试通过但 Fantasy 接入失败，或者 Fantasy 接入绕过本地 rollback 验收。

本 change 是串行实施的第二包。它不是“以后可选接入 Fantasy”的提案，而是正式规划 Fantasy 或等价 transport 如何进入帧同步闭环。它的核心原则是：**transport 只收发帧同步消息，不拥有角色模拟、不推进 gameplay tick、不解释 Action、不读写 snapshot。**

Fantasy 是接入实现之一，不是 rollback core 的依赖。Fake transport 和 Fantasy adapter 必须共用同一个 port。这样第三包 prediction/rollback closed loop 可以先用 fake transport 跑通，再把 transport 实现换成 Fantasy，而不是改 rollback 算法。

## What Changes

- 新增帧同步 transport port 规划。
- 定义 transport 消息类别：handshake、input submit、input ack、confirmed input set、checksum report、correction request、disconnect/reconnect diagnostic。
- 定义 fake transport 与 Fantasy adapter 必须实现同一 port。
- 定义 Unity 客户端 Fantasy Session wrapper 到 transport port 的映射。
- 定义服务端 Fantasy Handler 的职责边界。
- 定义服务端 room input collector 只收集并确认输入，不模拟角色。
- 定义 Fantasy Outer protocol 的消息分组和字段方向。
- 定义 protocol export、`.g.cs` 不手改、Handler source generator 注册和 dotnet build 验证要求。
- 定义 transport callback 不直接推进 `CharacterFramePipeline`，只投递消息到 prediction/reconciliation 层。

## Impact

- Affected specs: frame-sync-transport-fantasy-adapter
- Depends on:
  - frame-sync-input-authority-foundation
- Related specs:
  - simulation-tick-system
  - local-latency-reconciliation
  - local-rollback-synctest-foundation
  - character-frame-rollback-replay
  - runtime-diagnostic-logging
- Affected code later:
  - future `Assets/Scripts/Simulation/FrameSync/Transport`
  - future Unity Fantasy client adapter
  - `3cDemo/Tools/NetworkProtocol`
  - `3cDemo/Tools/ProtocolExportTool`
  - future Fantasy server Hotfix handlers

## Formal Planning Boundary

本 proposal 的粒度是正式 transport 接入规划，不再拆成“transport port 一个 proposal、Fantasy adapter 一个 proposal、fake transport 一个 proposal、handler 一个 proposal”。这些部分必须一起设计，否则会出现 fake 和 Fantasy 不一致。

本 proposal 完成后，系统应该具备一条明确链路：

1. 客户端本地输入事实进入 frame sync transport port。
2. fake transport 或 Fantasy adapter 负责发送。
3. 服务端或 fake room 收集输入。
4. 服务端或 fake room 输出 confirmed input set。
5. 客户端 adapter 将 confirmed input set 投递给 prediction/rollback 层。

这一包不要求 rollback apply 成功，那是第三包的职责；但这一包必须保证消息边界不会阻碍第三包接入。

## Transport Port Model

Transport port 应该是 frame sync core 看到的唯一网络边界。建议概念接口包含：

- `SendInput(FrameSyncInputFrame input)`
- `SendChecksum(FrameSyncChecksum checksum)`
- `SendHandshake(FrameSyncHandshakeRequest request)`
- `OnHandshakeResult`
- `OnInputAck`
- `OnConfirmedInputSet`
- `OnCorrection`
- `OnDisconnected`
- `OnReconnected`
- `OnTransportDiagnostic`

Port 的输入输出必须全部是纯数据 DTO。Port 不允许出现：

- `Session`
- Fantasy `Entity`
- `Scene`
- `GameObject`
- `MonoBehaviour`
- `Transform`
- `CharacterFramePipeline`
- `ILocalRollbackSynctestSimulation`
- `CharacterController`
- Animancer / Cinemachine 类型

Fantasy adapter 可以在 adapter 层持有 Fantasy `Session`，但不能把它暴露给 frame sync core。

## GGPO Boundary

`Ref/ggpo` 的 UDP/P2P backend 可以作为 rollback netcode 网络层参考，但本项目不采用它作为正式 transport。

原因：

- 当前目标是 Fantasy 或等价服务端 room input authority。
- 项目需要 server-confirmed input set，而不是直接照搬 GGPO P2P session。
- fake transport 和 Fantasy adapter 必须共用本项目自己的 transport port。

因此 GGPO 网络层只作为理解参考；正式实现仍走本 proposal 定义的 transport port 和 Fantasy adapter。

## Fantasy Protocol Model

Fantasy 外网协议建议按消息方向规划：

### Client to Server

- `C2G_FrameSyncHandshakeRequest`
- `C2G_FrameSyncInput`
- `C2G_FrameSyncChecksum`
- `C2G_FrameSyncReady`
- `C2G_FrameSyncLeave`

### Server to Client

- `G2C_FrameSyncHandshakeResponse`
- `G2C_FrameSyncInputAck`
- `G2C_FrameSyncConfirmedInputSet`
- `G2C_FrameSyncCorrection`
- `G2C_FrameSyncDiagnostic`

如果项目最终不是 Gate 命名，也可以按实际 Fantasy 架构改成 `C2M/M2C` 或其它前缀，但规则不变：消息名必须表达发起方和目标方。

协议定义必须遵守 Fantasy 规则：

- `.proto` 位于 Outer 协议目录。
- Request/Response 注释中的 response 名必须完全匹配。
- 不手改导出生成的 `.cs` 或 `.g.cs`。
- Handler 用 source generator 自动注册。
- Handler 类使用 `sealed class`。
- async 使用 `FTask`。
- 业务错误用 response error code 或 diagnostic，不用异常表达普通业务失败。

## Server Responsibility

服务端第一阶段只做 input authority。

它可以做：

- session/player 绑定。
- handshake 校验。
- room membership。
- tick input queue。
- duplicate/missing/late/wrong tick diagnostic。
- confirmed input set 构建。
- confirmed input set 广播。
- checksum report 汇总。
- correction request 转发或广播。
- disconnect/reconnect diagnostic。

它不能做：

- 创建服务端角色控制器。
- 推进 `CharacterFramePipeline`。
- 运行 Locomotion / Action runtime。
- 生成权威 Transform。
- 生成权威 Animator state。
- 执行 CharacterController collision。
- 修改客户端 gameplay snapshot。

## Unity Client Responsibility

Unity 客户端 adapter 负责把 Fantasy Session API 映射为 transport port。

它可以做：

- 连接 Fantasy。
- 保存 Session 引用。
- 调用协议导出生成的发送方法。
- 接收服务器 push。
- 将 push DTO 转换为 frame sync transport event。
- 输出 transport diagnostic。

它不能做：

- 在 Session callback 里直接调用 rollback restore/replay。
- 在 push handler 里直接写角色 Transform。
- 把 Fantasy Session 注入 rollback core。
- 因 transport 断线创建 fallback gameplay path。

## Fake Transport Requirement

fake transport 不是临时玩具路径。它是正式测试 transport 实现，必须与 Fantasy adapter 实现同一个 port。

fake transport 必须支持：

- 多 client input submit。
- fake room confirmed input set。
- latency。
- reorder。
- duplicate。
- missing。
- late input。
- checksum report。
- correction injection。

fake transport 不允许模拟角色。它只模拟网络和服务端 input authority。

## Non-Goals

- 不实现 prediction buffer。
- 不实现 rollback apply。
- 不实现 strict checksum 算法。
- 不实现 motion determinism audit。
- 不实现服务端角色控制器。
- 不新增旁路 socket 作为正式配置。
- 不把 fake transport 和 Fantasy adapter 设计成两套 DTO。

## Acceptance Criteria

- transport port 是 fake 和 Fantasy 的唯一共同边界。
- frame sync core 不引用 Fantasy。
- Fantasy handler 不引用角色 runtime。
- Fantasy handler 不推进 gameplay tick。
- fake transport 可在不启动 Fantasy 进程的情况下产出 confirmed input set。
- protocol export 和 dotnet build 被列入验证路径。
- transport diagnostic 可以区分 handshake fail、disconnect、duplicate、late、missing。

## Implementation Order

1. 定义 transport port。
2. 定义 transport DTO。
3. 定义 fake transport 事件语义。
4. 定义 Fantasy proto 消息。
5. 定义 Unity client adapter。
6. 定义 server room input collector。
7. 定义 server broadcaster。
8. 添加静态边界测试。
9. 添加 fake transport 合同测试。
10. 添加 protocol export / dotnet build 验证任务。

## Conflict Check Against Current Specs

### `simulation-tick-system`

Transport 不拥有 tick driver，只传递 tick 标记和 input set。

### `local-latency-reconciliation`

Transport 只替换 remote input 来源，不改变 reconciliation 语义。

### `character-frame-rollback-replay`

Fantasy handler 不触碰 Character frame replay。Replay 仍由客户端 simulation adapter 处理。

### `local-rollback-synctest-foundation`

Fake transport 属于测试 transport，不成为正式角色 prefab 依赖。

## Review Notes

明天 review 这个 proposal 时，重点看：

1. Fantasy 有没有被限制在 adapter 层。
2. fake transport 和 Fantasy 是否真的共用 port。
3. 服务端职责是否仍然只是 input authority。
4. 有没有任何地方暗示服务端要跑角色控制器。

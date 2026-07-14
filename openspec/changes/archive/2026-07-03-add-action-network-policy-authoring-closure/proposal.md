# Change: 补齐 Action 网络策略作者闭环

## Why

当前代码和 spec 已经有 `ActionProfile`、`ActionInstance`、`ActionLifecycleTransition`、`SyncFacts`、`CharacterGameplaySyncAdapter` 等底层概念，但作者侧还没有形成闭环：

- 作者能看到一些 policy enum，但看不出一个动作最终会产生哪些同步事实和 packet。
- Graph、Timeline、非 Timeline 输出之间的职责边界还需要更强约束，避免重新滑回 `ActionModule`、节点身份或 clip 级网络配置。
- `CharacterGameplaySyncAdapter` 已经能做事实到 packet 的映射，但还没有把 `ActionProfile` 的网络策略解析结果作为统一输入。
- Runtime Debug 需要能按 `ActionInstance` 展示“请求、生命周期、窗口、运动、表现、结果、发送/接收”的同一条链路。

这个 change 的目标不是做真实服务端，而是先让本地管线里的动作网络语义能被作者配置、预览和调试。后续接 Fantasy 服务端、预测回滚、combat rewind 时，不需要再改作者心智模型。

## What Changes

1. 将 `ActionProfile` Inspector 收口为动作网络策略主编辑入口。
2. 增加显式策略模板或等价创建入口，用于一次性写入常见策略组合，但运行时不得依赖隐藏默认值。
3. 定义只读的 effective network policy 解析结果，供 Inspector 预览、Runtime Debug 和 adapter 共同使用。
4. 增加 ActionProfile 级同步预览：activation、lifecycle、window、motion、cue、gameplay result 会进入哪些 SyncDomain，哪些只本地表现，哪些需要服务器确认。
5. 约束 Graph 节点只提交 request、持有 Action Context 或产生 lifecycle transition，不承载完整网络策略。
6. 约束 Timeline 和非 Timeline 输出只声明业务事实，网络语义从 ActionProfile 解析。
7. 让 Character outgoing adapter 使用同一份解析结果决定 facts 到 packet 的映射和过滤，不再形成硬编码散落的第二套策略。
8. 补齐 Runtime Debug 数据口径，按 ActionInstance 展示 resolved policy、实际 SyncFacts、outgoing packets、incoming decision/correction。

## Non-Goals

- 不实现真实 Fantasy 服务端。
- 不实现完整 rollback/replay。
- 不实现服务端命中回溯 solver。
- 不把 Graph、SubTree、StateNode 或 Timeline clip 标记为 Ability body。
- 不恢复 `ActionModule`、旧 ActionSO、节点身份模块或 per-clip 网络策略。
- 不处理 `refactor-character-motion-arbitration` 的运动仲裁实现，只定义网络策略如何描述和预览 motion 输出。

## Impact

- 面试展示时可以清楚说明：这个动作为什么能本地预测、哪些结果必须服务端确认、远端玩家会看到什么。
- 作者不需要手敲散落 key，不需要在每个节点和 clip 里重复网络字段。
- 未来接服务端时，服务端只消费事实和策略语义，不需要理解 Unity Graph、Timeline、Animancer 或表现层。
- 本地 loopback、远端 snapshot、owner prediction、ActionInstance transaction 和 combat result 都能用同一套作者心智解释。

## 风险和约束

- 这会暴露现有 `ActionProfile` 策略字段是否足够表达 gameplay result、motion correction 和 cue replication；如果不足，必须扩展正式配置，不做 fallback。
- 如果 adapter 当前仍有硬编码 SyncDomain 选择，本 change 会要求迁移到 resolver 驱动。
- 如果 Timeline 复用多个 ActionProfile，预览必须依赖显式 preview profile 或 runtime context，不得把策略写回 clip。

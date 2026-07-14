# Tasks

## 1. 清理旧规划口径

- [x] 1.1 确认不再新增 `ICharacterNetworkPeer` 作为正式 peer 抽象。
- [x] 1.2 确认旧 `add-local-network-loopback-peer` 口径已被 `add-gameplay-sync-runtime-character-adapter` 替代。
- [x] 1.3 确认实现计划中没有第二套 character-only packet contract。
- [x] 1.4 确认 `CharacterNetworkReceiveStage` 和 `CharacterNetworkSendStage` 只作为 Character adapter stage 保留。

## 2. 定义 GameplaySync 基础身份

- [x] 2.1 定义 `GameplaySyncActorId` 或等价 actor identity。
- [x] 2.2 定义 owner player id 字段。
- [x] 2.3 定义 team id 字段。
- [x] 2.4 定义 performer actor id 字段。
- [x] 2.5 定义 controlled actor id 字段。
- [x] 2.6 定义 target actor id 字段。
- [x] 2.7 定义 local logic tick 与 server tick 字段。
- [x] 2.8 定义 input sequence 字段。
- [x] 2.9 定义 prediction key 字段。
- [x] 2.10 定义 stable id 字段，并注明按 SyncDomain 解释。

## 3. 定义 GameplaySync packet 合同

- [x] 3.1 定义 packet envelope。
- [x] 3.2 packet envelope 包含 `SyncDomain`。
- [x] 3.3 packet envelope 包含 `PolicyId` 或等价策略引用。
- [x] 3.4 定义 `MotionSyncDomain` outgoing packet。
- [x] 3.5 定义 `MotionSyncDomain` incoming packet。
- [x] 3.6 定义 `ActionSyncDomain` outgoing packet。
- [x] 3.7 定义 `ActionSyncDomain` incoming packet。
- [x] 3.8 定义 `GameplayResultSyncDomain` outgoing packet。
- [x] 3.9 定义 `GameplayResultSyncDomain` incoming packet。
- [x] 3.10 定义 `StateEffectSyncDomain` packet 占位。
- [x] 3.11 定义 `PresentationSyncDomain` packet 占位。
- [x] 3.12 确认 packet 不引用 Graph、Timeline、NodeModule 或 Unity editor 类型。

## 4. 定义 GameplaySyncRuntime

- [x] 4.1 定义 outgoing queue。
- [x] 4.2 定义 incoming queue。
- [x] 4.3 定义 peer 注册入口。
- [x] 4.4 定义按 local logic tick pump 的入口。
- [x] 4.5 定义 prediction key 分配器。
- [x] 4.6 定义 stable id 分配或接收边界。
- [x] 4.7 定义按 actor + SyncDomain 的 history buffer。
- [x] 4.8 定义 debug record buffer。
- [x] 4.9 确认 runtime 不 tick Graph、不调用 ActionRuntime、不调用 MotionStage。

## 5. 定义通用 peer 合同

- [x] 5.1 定义 `IGameplaySyncPeer` 或等价接口。
- [x] 5.2 peer 支持接收 outgoing packet。
- [x] 5.3 peer 支持按 tick 推进。
- [x] 5.4 peer 支持取出 incoming packet。
- [x] 5.5 peer 不暴露 CharacterPipeline。
- [x] 5.6 peer 不暴露 ActionRuntime。
- [x] 5.7 peer 不暴露 Fantasy Session 给 gameplay 层。

## 6. 实现 Character outgoing adapter

- [x] 6.1 定义 `CharacterGameplaySyncAdapter`。
- [x] 6.2 将 `ClientCommands` 映射为 MotionSyncDomain packet。
- [x] 6.3 将 `ActionActivationRequests` 映射为 ActionSyncDomain activation packet。
- [x] 6.4 将 `ActionEndRequests` 映射为 ActionSyncDomain end packet。
- [x] 6.5 将 `ActionWindowSamples` 和 `WindowDigests` 映射为 ActionSyncDomain window digest packet。
- [x] 6.6 将 `ActionMotionSamples` 映射为 MotionSyncDomain 或 action-scoped motion digest packet。
- [x] 6.7 将 `ActionCueEvents` 映射为 PresentationSyncDomain cue packet。
- [x] 6.8 将 `ActionCombatEvents` 迁移并映射为 GameplayResultSyncDomain result packet。
- [x] 6.9 删除或改名 `ActionCombatEvent` 正式输出命名，不保留兼容别名。

## 7. 实现 Character incoming adapter

- [x] 7.1 将 Motion correction packet 推入 `CharacterNetworkReceiveStage` correction 队列。
- [x] 7.2 将 Motion snapshot packet 推入正式 snapshot 队列。
- [x] 7.3 将 ActionInstanceDecision packet 推入正式 action decision 队列。
- [x] 7.4 将 GameplayResult packet 推入 gameplay result 队列。
- [x] 7.5 将 StateEffect packet 推入 state/effect 队列。
- [x] 7.6 将 Presentation cue packet 推入 presentation cue 队列。
- [x] 7.7 移除 `ConfirmedEvent(eventId, payload)` 作为 action decision 正式入口。
- [x] 7.8 确认 incoming adapter 不直接调用 ActionRuntime confirm/reject/correct。

## 8. 接入 Character Pipeline tick

- [x] 8.1 定义 driver 在 pipeline tick 前调用 `GameplaySyncRuntime.Pump`。
- [x] 8.2 driver 在 `LogicTick` 前通过 adapter 注入 incoming packets。
- [x] 8.3 保持 `NetworkReceiveStage.Collect` 位于 InputStage 前。
- [x] 8.4 保持 `NetworkSendStage.Collect` 位于 MotionStage 后。
- [x] 8.5 driver 在 `LogicTick` 后收集 outgoing packets。
- [x] 8.6 driver 将 outgoing packets 写入 `GameplaySyncRuntime`。
- [x] 8.7 driver 使用 `CharacterPipelineRunner` 的 tick，不新增第二套 tick。

## 9. 实现 Local GameplaySync loopback

- [x] 9.1 定义 loopback 配置类型。
- [x] 9.2 配置支持延迟 tick。
- [x] 9.3 配置支持 action confirm。
- [x] 9.4 配置支持 action reject。
- [x] 9.5 配置支持 confirm 后 correction。
- [x] 9.6 配置支持 correction offset。
- [x] 9.7 配置支持 packet drop rate。
- [x] 9.8 配置支持 motion snapshot 输出。
- [x] 9.9 配置支持 defense favor 标记。
- [x] 9.10 loopback 使用通用 GameplaySync packet，不引用 CharacterPipeline。
- [x] 9.11 loopback 不直接修改 Graph、ActionRuntime、MotionStage、Presentation 或 Transform。

## 10. 预留 Fantasy adapter 边界

- [x] 10.1 定义 GameplaySync outgoing packet 到未来 C2S 消息的映射表。
- [x] 10.2 定义未来 S2C 消息到 GameplaySync incoming packet 的映射表。
- [x] 10.3 确认 Fantasy adapter 只实现 `IGameplaySyncPeer`。
- [x] 10.4 确认 Fantasy adapter 不引入第二套 action decision。
- [x] 10.5 确认 Fantasy adapter 不同步 Graph、Timeline 或 NodeModule。

## 11. Runtime Debug

- [x] 11.1 Debug 记录最近 outgoing packets。
- [x] 11.2 Debug 记录 peer pending packets。
- [x] 11.3 Debug 记录最近 incoming packets。
- [x] 11.4 Debug 按 actor id 显示 packet。
- [x] 11.5 Debug 按 SyncDomain 显示 packet。
- [x] 11.6 Debug 显示 stable id、prediction key、input sequence、local tick 和 server tick。
- [x] 11.7 Debug 显示 action decision、correction 和 gameplay result。

## 12. 验证

- [x] 12.1 使用 `rg` 确认没有新增 `ICharacterNetworkPeer` 正式抽象。
- [x] 12.2 使用 `rg` 确认 loopback 没有直接调用 ActionRuntime confirm/reject/correct。
- [x] 12.3 使用 `rg` 确认 loopback 没有直接调用 Graph、Timeline、MotionStage、Presentation 或 Transform。
- [x] 12.4 使用 `rg` 确认 `ConfirmedEvent` 不再作为 action decision 正式入口。
- [x] 12.5 使用 `rg` 确认没有新增 `ActionModule`、`AbilityTree` 或 Graph 同步路径。
- [x] 12.6 运行 `openspec validate add-gameplay-sync-runtime-character-adapter --strict --no-interactive`。

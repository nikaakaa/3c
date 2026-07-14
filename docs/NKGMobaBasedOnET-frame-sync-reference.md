# NKGMobaBasedOnET 参考评估

## 结论

`Ref/NKGMobaBasedOnET` 只提供输入历史、确认、校正和调试机制参考，不是 3C 的运行时依赖，也不是当前网络架构模板。

可以借鉴：

- 输入 sequence、历史与确认的组织方式。
- 本地预测误差、服务端确认和校正记录。
- 逻辑 tick 与表现帧分离。
- 有界历史、差量记录和同步诊断。

不能照搬：

- 全局确定性帧同步、完整世界 rollback。
- ET Entity、NPBehave Blackboard、Box2D/寻路和 MOBA 技能类型体系。
- 服务端复制客户端 CharacterPipeline、BTSMTL Graph、Timeline 或 Animancer。
- 把相机、动画状态、VFX、Timeline 播放状态或编辑器状态作为网络真相。

## 当前边界

3C 当前只实现一个完整 Network Model：`ServerAuthoritativeHybrid`。正式链路是：

```text
GameplayTickSystem
-> CharacterPipeline 产生 model-neutral gameplay facts
-> CharacterServerAuthoritativeBinding
-> CharacterServerAuthoritativeAdapter
-> shared ServerAuthoritativeHybridSession
-> ServerAuthoritativeEndpointDefinition
-> disconnected 或 LocalServerAuthoritativeEndpoint
```

`GameplayNetworkSessionHost` 只装配一个模型。多个角色 binding 共享同一个 model session，并使用精确 `SubjectActorId` 路由。Character、Graph、Timeline、Blackboard 和 ActionProfile 不选择模型，也不保存该模型的 packet、history、endpoint 或网络策略。

`ServerAuthoritativePacket`、queue、history、policy resolver 和 endpoint 都属于 `ServerAuthoritativeHybrid`。它们不是 generic GameplaySync 合同。当前没有 `GameplaySyncRuntime`、每角色 peer、backend enum 或第二套网络模型。

## 可吸收机制

### 输入历史与关联身份

本地输入由 `CharacterInputFrame`、`CharacterInputHistory`、`InputSequence` 和 `LocalLogicTick` 组织。`PredictionKey` 是 ActionRuntime 的动作事务关联身份；模型 adapter 可以把它复制进模型 envelope，但 Character 不读取 packet identity、authority tick 或服务端裁决 metadata。

边界：

- Input System 只由 LocalDevice 输入路径读取。
- 网络 endpoint 不读取 InputAction，也不 tick Graph。
- `ServerTick` 只来自模型 snapshot、correction 或 action decision。
- 远端角色后续使用 `ExternalFacts + ExternalPose`，不复制本地输入控制器。

### 预测与校正

Owner 角色继续使用 `LocalDevice + LocalSolver` 立即运行输入、Graph、Timeline、Motion 和动画。模型收到服务端结果后：

- Action decision 由 adapter 转换成 `ActionLifecycleTransition`。
- Pose correction 转换成 Character semantic correction input。
- `CharacterMotionStage` 是位姿校正的唯一应用位置。
- `MotionCorrectionApplicationResult` 再由 adapter 转成模型 acknowledgement。

不做全局 rollback，不回滚整个世界。后续 PvP 命中若实现，只允许服务端权威加局部 pose/hurtbox/action-window rewind。

### 事实、模型包与调试

CharacterNetworkSendStage 只收集 Character 事实：resolved motion、Action/window、GameplayResult、StateEffect 和 Cue。模型 adapter 根据 `ServerAuthoritativeCharacterSyncProfile` 将这些事实映射为当前模型 packet。

```text
Character fact
-> model profile policy
-> ServerAuthoritativePacket
-> model queue/history/endpoint
```

反向链路先把模型 payload 转成 Character 语义输入，再交给 NetworkReceiveStage、ActionRuntime 或 MotionStage。Character 不保存或解释模型 packet。

调试也分两层：Character diagnostics 展示输入、动作生命周期、运动与表现；ServerAuthoritative diagnostics 展示 model id、policy、packet、queue、history 和 endpoint health。两层通过稳定 actor/action/input identity 关联，不复制运行时状态。

## 不采用的路线

- 不新增全局 FrameSync contract。
- 不恢复 generic `GameplaySyncPacket` 或 `IGameplaySyncPeer`。
- 不把 LocalLoopback 与未来 Fantasy 写成 backend enum；它们是同一模型下不同的 EndpointDefinition。
- 不让连接失败回退 LocalLoopback。
- 不把动画 producer、Animancer transition、Timeline asset 或 Graph 节点路径写入网络协议。
- 不让服务端运行 Unity physics、CharacterController 或表现逻辑来重演客户端。

## 后续使用

需要网络参考时，先对齐：

- `openspec/project.md`
- `openspec/changes/refactor-gameplay-network-model-boundary/specs/gameplay-network-model-boundary/spec.md`
- `openspec/changes/refactor-gameplay-network-model-boundary/specs/server-authoritative-hybrid-sync-model/spec.md`
- `openspec/specs/character-network-sync-domain-contract/spec.md`
- `openspec/specs/gameplay-tick-system/spec.md`
- `openspec/changes/add-local-two-client-gameplay-network-closure/proposal.md`

`refactor-gameplay-network-model-boundary` 归档后，应以合并后的 current specs 取代其 change delta 引用。参考项目只能帮助解释机制，不能成为新增第二条 runtime 链路的理由。

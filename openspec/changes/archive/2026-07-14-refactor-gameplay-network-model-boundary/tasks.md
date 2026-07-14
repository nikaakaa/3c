## 1. 固定真实依赖和迁移输入

- [x] 1.1 盘点 `Runtime/Networking/GameplaySync` 中每个类型属于 model、endpoint、transport、debug 或公共 identity 的真实职责。
- [x] 1.2 盘点 Character Action、Behavior、Pipeline、GameplayTick 和 Editor 对 `ThirdPersonGameplay.Sync` 的全部直接引用。
- [x] 1.3 盘点 `GameplayAuthorityMode` 在 tick context、input、motion、graph、host 和 scene serialization 中的全部引用。
- [x] 1.4 盘点 Corin ActionProfile、GameplayBehaviorProfile、CharacterPipelineDefinition 和 Sandbox 中现存网络字段及资产 GUID 引用。
- [x] 1.5 记录 Attack、Dodge、Locomotion、CorrectionAck、StateEffect 当前完整 model policy 值，作为正式迁移输入。
- [x] 1.6 确认 BTSMTL Runtime/Editor 不直接引用 GameplaySync、ServerAuthoritative policy 或 Fantasy 类型。
- [x] 1.7 明确 `add-local-two-client-gameplay-network-closure` 在本 change 完成前保持未实施状态。

## 2. 建立 model-neutral Session 装配边界

- [x] 2.1 新增只表达 model identity、配置校验和 session 创建职责的 `GameplayNetworkModelDefinition` 基类或等价合同。
- [x] 2.2 新增只管理单一 model lifecycle 的 `GameplayNetworkSessionHost`。
- [x] 2.3 让 SessionHost 在启动前要求唯一完整 model definition。
- [x] 2.4 让 SessionHost 在连接、binding 或 tick 开始后拒绝更换 model definition。
- [x] 2.5 让 SessionHost 不引用 MotionCommand、Snapshot、Correction、ActionDecision 或 model policy 类型。
- [x] 2.6 让 SessionHost 不创建 Character、Graph、Timeline、Animation 或 Camera 对象。
- [x] 2.7 让 model session 自己拥有 actor binding registry、queue、history、debug 和 tick coordination。
- [x] 2.8 让 Inspector 只允许配置已存在的 model definition asset，不显示未实现模型枚举。
- [x] 2.9 明确 `None` 不作为 network model；未连接语义由所选模型的 endpoint 配置表达。

## 3. 拆分 Character 输入来源与运动权威

- [x] 3.1 新增 `CharacterInputSource`，只表达 LocalDevice、ExternalFacts 和 None。
- [x] 3.2 新增 `CharacterMotionAuthority`，只表达 LocalSolver、ExternalPose 和 None。
- [x] 3.3 将 CharacterPipelineHost 的单一 authority 字段迁移为 input source 与 motion authority 两个正式字段。
- [x] 3.4 将 CharacterPipeline 构造参数和只读状态迁移为两个正交字段。
- [x] 3.5 让 CharacterInputStage 只按 input source 决定 Input System 或 external facts 来源。
- [x] 3.6 让 CharacterMotionStage/Modifier 只按 motion authority 决定 LocalSolver 或 external pose 路径。
- [x] 3.7 从 GameplayTickContext 和 IGameplayTickTarget 删除 `GameplayAuthorityMode`。
- [x] 3.8 删除 `GameplayAuthorityMode` 类型和 LocalPredicted/RemoteProxy/PresentationOnly 分支。
- [x] 3.9 不保留旧 enum 到新字段的运行时映射、FormerlySerializedAs 或兼容 getter。
- [x] 3.10 更新 Sandbox Owner 为 LocalDevice + LocalSolver 正式组合。

## 4. 将 Character 输出收口为 model-neutral facts

- [x] 4.1 新增 `ResolvedCharacterMotionFact` 或等价事实，保存 input sequence、logic tick、实际 delta/yaw、最终逻辑 pose、grounded 和移动摘要。
- [x] 4.2 让 CharacterMotionStage 在本 tick运动结算后唯一产生 resolved motion fact。
- [x] 4.3 让 CharacterNetworkSendStage 只收集 CharacterInputFrame 引用/摘要、resolved motion fact 和既有 Action/Result/State/Cue facts。
- [x] 4.4 从 Character SyncFacts 删除 `ClientCommand` 和 CorrectionAck packet 语义。
- [x] 4.5 让 CharacterMotionStage 继续唯一输出 `MotionCorrectionApplicationResult`，不构造 model acknowledgement。
- [x] 4.6 让模型 Adapter 将动作裁决收口为既有 `ActionLifecycleTransition`，不新增伪中立 authority DTO。
- [x] 4.7 新增 Character 语义 external pose correction/sample 输入，避免 receive stage 保存 model packet payload。
- [x] 4.8 将 GameplayResult、StateEffect 和 Cue incoming 映射为 Character/gameplay 语义类型。
- [x] 4.9 更新 CharacterNetworkReceiveStage 只缓存语义输入，不引用 `ThirdPersonGameplay.Sync`。
- [x] 4.10 使用 `rg` 确认 CharacterPipeline runtime 不引用 model packet、model history 或 endpoint 类型。

## 5. 分离 Action 与 Behavior 的 Gameplay 身份

- [x] 5.1 从 `ActionProfile` 迁出 prediction、authority 和 replication 字段。
- [x] 5.2 从 `ActionProfile` 迁出 window authority/history/replication/digest 策略。
- [x] 5.3 从 `ActionProfile` 迁出 motion prediction 策略。
- [x] 5.4 从 `ActionProfile` 迁出 cue playback/replication 策略。
- [x] 5.5 从 `ActionProfile` 迁出 gameplay result proposal/history/replication/digest 策略。
- [x] 5.6 保留 ActionId、display、debug category、tags、block/cancel tags 和 target gameplay 语义。
- [x] 5.7 从 `ActionContext` 和 snapshot/debug record 删除 prediction、authority 和 replication 字段。
- [x] 5.8 将 `GameplayBehaviorProfile` 迁移为 gameplay identity-only definition，保留 BehaviorId、BehaviorKind、display、debug category 和 tags。
- [x] 5.9 从 gameplay behavior identity 删除 target SyncDomain、command send、snapshot、remote presentation、history、prediction、authority 和 replication。
- [x] 5.10 保持 ActionProfile 与非事务 Behavior definition 的稳定 ID 唯一校验。

## 6. 建立 ServerAuthoritativeHybrid 模型模块

- [x] 6.1 新增 `ServerAuthoritativeHybridModelDefinition` 并赋予稳定 model id。
- [x] 6.2 新增 `ServerAuthoritativeCharacterSyncProfile`，唯一保存角色在该模型下的同步策略。
- [x] 6.3 在 profile 中建立 BehaviorId 对应 Stream/State/Event model policy。
- [x] 6.4 在 profile 中建立 ActionId 对应 Transaction 基础 model policy。
- [x] 6.5 在 Action model policy 中建立 window、motion、cue 和 gameplay result 子策略。
- [x] 6.6 在 profile 中建立 SyncFact kind 到 BehaviorId/ActionId 的显式绑定。
- [x] 6.7 校验 profile 引用的 BehaviorId/ActionId 必须存在于目标 CharacterPipelineDefinition。
- [x] 6.8 校验每个需要发送或确认的 fact 只有一个 policy owner。
- [x] 6.9 缺失、重复、类型不匹配的 policy 直接配置失败，不建立默认或名称搜索 fallback。
- [x] 6.10 让 model definition 显式引用 endpoint 配置和模型级 queue/debug 容量。

## 7. 迁移 Runtime、Packet、History 和 Endpoint 命名归属

- [x] 7.1 将 `ThirdPersonGameplay.Sync` 目录和 namespace 迁移到 ServerAuthoritativeHybrid 模型模块。
- [x] 7.2 将 `GameplaySyncRuntime` 重命名为模型专属 Session runtime。
- [x] 7.3 将 `GameplaySyncPacket`、Envelope、PacketKind 和 payload 重命名为模型专属合同。
- [x] 7.4 将 `GameplaySyncHistory` 和 Runtime Debug 重命名并归属模型模块。
- [x] 7.5 将 `IGameplaySyncPeer` 重命名为 `IServerAuthoritativeEndpoint` 或等价模型 endpoint 合同。
- [x] 7.6 将 `LocalGameplaySyncLoopbackPeer` 重命名为模型专属 LocalLoopback endpoint。
- [x] 7.7 建立模型专属 EndpointDefinition 创建边界；未配置表示显式 disconnected，LocalLoopback 使用独立 definition，未来 endpoint 不修改模型核心。
- [x] 7.8 删除字符串型 `FantasyGameplaySyncPeerContract` placeholder，不在本 change 新增 Fantasy 实现。
- [x] 7.9 将 incoming queue 改为按唯一 SubjectActorId 分区的有界存储。
- [x] 7.10 删除同时匹配 Actor/Controlled/Performer/Target 的宽松 drain。
- [x] 7.11 让 model Session 的 Pump 对同一 local logic tick 幂等。
- [x] 7.12 让 endpoint 更换、disconnect 和 dispose 精确清理 model queue/history/debug。
- [x] 7.13 让 MotionCommand/MotionSnapshot 仅替换同 actor 同类旧流样本，并让 Action/Result/ACK 等可靠事实在队列溢出时直接失败。

## 8. 迁移 ServerAuthoritative Character Adapter 与策略解析

- [x] 8.1 将 `CharacterGameplaySyncAdapter` 重命名并迁移为 ServerAuthoritative Character adapter。
- [x] 8.2 让 outgoing adapter 从 CharacterInputFrame、resolved motion fact 和 gameplay facts 构造模型 packet。
- [x] 8.3 让 adapter 从 correction application result 构造模型 correction acknowledgement。
- [x] 8.4 让 incoming adapter 把模型 ActionDecision 转换为 Character `ActionLifecycleTransition`，prediction/defense metadata 留在模型内。
- [x] 8.5 让 incoming adapter 把模型 Snapshot/Correction 转换为 Character external pose 输入。
- [x] 8.6 让 incoming adapter 把模型 Result/State/Cue 转换为 gameplay 语义输入。
- [x] 8.7 将 `BehaviorNetworkPolicyResolver` 迁移为模型专属 behavior policy resolver。
- [x] 8.8 将 `ActionNetworkPolicyResolver` 迁移为模型专属 transaction policy resolver。
- [x] 8.9 让所有 resolver 只读取 `ServerAuthoritativeCharacterSyncProfile`，不读取 ActionProfile 网络字段。
- [x] 8.10 让 packet preview 和 debug 复用模型 adapter/resolver 的正式映射。
- [x] 8.11 删除旧 generic resolver、generic packet preview 和 policy source 接口。

## 9. 建立 SessionHost 与 Character Binding 唯一 ownership

- [x] 9.1 新增 ServerAuthoritative model-owned Character binding。
- [x] 9.2 让 binding 只保存 SessionHost、CharacterPipelineHost、SubjectActorId 和 ServerAuthoritative Character Sync Profile。
- [x] 9.3 让 SessionHost 唯一创建 ServerAuthoritative model Session 和 endpoint。
- [x] 9.4 让 binding 在 Character logic tick 前请求 model Session pump 并 drain exact actor 输入。
- [x] 9.5 让 binding 在 Character logic tick 后通过模型 adapter 收集 facts 并 flush endpoint。
- [x] 9.6 让多个 binding 共享 Session runtime，但不得互相消费 actor queue。
- [x] 9.7 删除 `CharacterGameplaySyncDriver` 的 per-character runtime、peer、backend、identity 和 loopback settings ownership。
- [x] 9.8 删除旧 Driver 类型和场景组件，不保留 wrapper 或兼容 MonoBehaviour。
- [x] 9.9 让 SessionHost diagnostics 唯一展示 model id、endpoint、bindings、queue、history 和 policy errors。

## 10. 迁移 Editor、Agent 和 Corin 资产

- [x] 10.1 将 ActionProfile Inspector 收口为 gameplay identity、tags、block/cancel、target 和 debug。
- [x] 10.2 将 GameplayBehavior identity Inspector 删除所有网络模型字段。
- [x] 10.3 新增 ServerAuthoritative Character Sync Profile Inspector。
- [x] 10.4 让 model Inspector 展示 Behavior/Action policy、fact binding、effective packet preview 和配置错误。
- [x] 10.5 更新 CharacterPipelineDefinition Inspector，不再编辑 Behavior network policy 或 SyncFact network binding。
- [x] 10.6 更新 Agent snapshot，只把 ActionProfile/Behavior definition 作为 gameplay identity 输出。
- [x] 10.7 更新 Agent validator 只校验 gameplay identity/window 语义；模型 profile Inspector 只读校验 policy coverage，Agent Patch 不编辑 model policy。
- [x] 10.8 创建 Corin 唯一 ServerAuthoritative Character Sync Profile。
- [x] 10.9 将 Attack 和 Dodge 的完整 Transaction/window/motion/cue/result policy 迁入新 profile。
- [x] 10.10 将 Locomotion、CorrectionAck 和 StateEffect policy/binding 迁入新 profile。
- [x] 10.11 从 Corin ActionProfile 和 Behavior assets 删除旧网络字段并保留 gameplay identity。
- [x] 10.12 从 Corin CharacterPipelineDefinition 删除 model policy/profile/binding ownership。
- [x] 10.13 将 Sandbox 迁移为唯一 GameplayNetworkSessionHost + model definition + Character binding。
- [x] 10.14 删除 Sandbox 旧 per-character backend 和 identity 序列化数据。

## 11. 清理旧路径与重写后继计划

- [x] 11.1 删除旧 `GameplaySyncRuntime`、`GameplaySyncPacket`、`IGameplaySyncPeer` 和 `GameplaySyncBackendMode` 类型名。
- [x] 11.2 删除旧 `CharacterGameplaySyncAdapter`、`CharacterGameplaySyncDriver` 和 generic network policy resolver 类型名。
- [x] 11.3 删除旧 `GameplayAuthorityMode` 与 LocalPredicted/RemoteProxy 枚举引用。
- [x] 11.4 删除 ActionProfile/GameplayBehaviorProfile 中旧网络字段、模板和 Inspector 路径。
- [x] 11.5 使用 `rg` 确认没有 FormerlySerializedAs、fallback profile、名称搜索、旧 wrapper 或双写路径。
- [x] 11.6 更新 `openspec/project.md` 的 Current State、Network Boundary、Code Organization 和 Open Questions。
- [x] 11.7 重写 `add-local-two-client-gameplay-network-closure` 的 proposal，使其只扩展 ServerAuthoritativeHybrid 的 Fantasy endpoint、Room 和双客户端纵切。
- [x] 11.8 重写该后继 change 的 design、tasks 和重叠 spec deltas，删除已经由本 change 完成的 SessionHost、Binding、精确路由、MotionCommand 和 policy ownership 任务。
- [x] 11.9 明确后继 change 不创建 generic GameplaySync runtime，不恢复 per-character peer，不新增第二模型。
- [x] 11.10 补充 `btsmtl-graph-data-catalog-authoring` delta，将 ActionWindow effective policy owner 从 ActionProfile 迁移到当前 Network Model profile。
- [x] 11.11 补充 `btsmtl-runnable-timeline-node` delta，保持 Timeline 只产出动作事实并由模型解析网络策略。
- [x] 11.12 补充 `character-action-activation-flow` delta，保持 Action Context 显式传递并删除 Timeline 对 ActionProfile 网络字段的依赖。
- [x] 11.13 补充 `character-pipeline-blackboard` delta，限制 projection 只保存事实身份并由模型解析 effective policy。
- [x] 11.14 更新项目入口与历史参考文档，删除重复目标快照、过期调试说明和旧流程图。

## 12. 编译与严格校验

- [x] 12.1 使用带 `--disable-build-servers /nr:false /p:UseSharedCompilation=false` 的命令编译 Assembly-CSharp。
- [x] 12.2 Assembly-CSharp 编译后立即执行 `dotnet build-server shutdown`。
- [x] 12.3 使用相同参数编译 Assembly-CSharp-Editor。
- [x] 12.4 Editor 编译后立即执行 `dotnet build-server shutdown`。
- [x] 12.5 使用 `rg` 核对 Character/BTSMTL/common host 对 ServerAuthoritative packet/policy 类型的依赖边界。
- [x] 12.6 使用 `rg` 核对 ServerAuthoritative model 对 Graph、Timeline、Animation 资产结构没有反向读取。
- [x] 12.7 运行 `openspec validate refactor-gameplay-network-model-boundary --strict --no-interactive` 并解决全部问题。

## 1. 建立正式 correction application 结果合同

- [x] 1.1 新增 `MotionCorrectionApplicationResult`，保存 Applied、InputSequence、ServerTick、before/target pose 与实际 delta。
- [x] 1.2 新增只描述本 tick 实际未应用、部分应用或完整应用的 application extent，不把它定义为作者策略。
- [x] 1.3 在 `CharacterPipelineOutput` 中增加当前 logic tick 的 correction application result。
- [x] 1.4 在每帧输出清理中重置 correction application result。
- [x] 1.5 让 `CharacterMotionStage` 在不改变当前数值算法的前提下写入正式 result。
- [x] 1.6 让 `MotionResolveDebugFrame` 从正式 result 生成 correction debug。
- [x] 1.7 让 `CharacterPresentationInterpolator` 从正式 result 读取 application extent，不再读取 `MotionDebug`。
- [x] 1.8 保持完整应用时立即贴合、部分应用时使用普通 logic sample interpolation 的现有表现行为。

## 2. 删除 Action correction 混合模型

- [x] 2.1 从 `ActionPolicyTypes` 删除 `ActionCorrectionPolicy`。
- [x] 2.2 从 `ActionProfile` 删除 action-level correction 字段和访问入口。
- [x] 2.3 从 `ActionMotionPolicy` 删除 correction 字段并收口构造函数。
- [x] 2.4 从 `ActionContext`、Action Context snapshot/handle 删除 correction 数据复制。
- [x] 2.5 从 Action policy templates 删除 correction 写入并保留其余正式字段。
- [x] 2.6 从 `ActionProfileEditor` 删除 correction SerializedProperty 和 UI。
- [x] 2.7 从 lifecycle policy summary 删除 correction 字样，保留 transition type、authority 和 replication 语义。
- [x] 2.8 保持 `Reject` terminal、`Correct` 默认 non-terminal 的 ActionRuntime invariant，不新增可配置 reject 策略。

## 3. 清理 Action motion 中的 actor correction 残留

- [x] 3.1 删除 `ActionMotionSourceType.Correction`，保持现有有效序列化枚举值稳定。
- [x] 3.2 从 `ActionMotionSample` 删除无生产者的 `CorrectionId`。
- [x] 3.3 从 `GameplayActionMotionDigest` 删除无生产者的 `CorrectionId`。
- [x] 3.4 更新 Timeline action motion sample 提交合同。
- [x] 3.5 更新 GameplaySync ActionMotionDigest packet 构造和 adapter 映射。
- [x] 3.6 将 Action motion outgoing 规则收口为 LocalPredicted source 加 action authority/replication。
- [x] 3.7 更新 ActionProfile Inspector 的 motion effective policy 预览与过滤原因。
- [x] 3.8 确认 Action lifecycle transition 用于关联 Correct decision 的 `CorrectionId` 继续保留。

## 4. 拆分 incoming Correction 与 outgoing Acknowledgement

- [x] 4.1 新增运行时 `MotionCorrectionAcknowledgement`，只保存 InputSequence 与 ServerTick。
- [x] 4.2 将 Motion SyncFacts 的 acknowledgement 集合改为新类型。
- [x] 4.3 将 `CharacterNetworkSendStage` 的 acknowledgement 集合改为新类型。
- [x] 4.4 让 MotionStage 只在 correction 确实应用后提交 acknowledgement。
- [x] 4.5 新增 `GameplayMotionCorrectionAcknowledgement` packet payload。
- [x] 4.6 让 `GameplaySyncPacket.MotionCorrectionAckPacket` 使用独立 acknowledgement payload。
- [x] 4.7 更新 `GameplaySyncPacket` 的复制、server tick 和 debug 读取链路。
- [x] 4.8 更新 `CharacterGameplaySyncAdapter` 的 Ack 映射，不再回显 correction position/rotation。
- [x] 4.9 更新 LocalGameplaySyncLoopbackPeer 对 Ack packet 的读取口径。

## 5. 清理 Stream behavior correction authoring

- [x] 5.1 从 `GameplayBehaviorProfile` 删除 correction 字段和访问入口。
- [x] 5.2 从 `GameplayBehaviorProfileEditor` 删除 correction SerializedProperty 和 UI。
- [x] 5.3 将 `ResolveMotionCorrectionAck` 改为按 Motion Stream、authority 和 replication 解析。
- [x] 5.4 更新 Behavior effective policy summary，删除 smooth、force 和 reject 表述。
- [x] 5.5 保持 `SyncFactBehaviorBinding.MotionCorrectionAck` 为唯一显式 Ack behavior 绑定。
- [x] 5.6 确认 GameplayBehaviorProfile 不新增 partial/full、replay 或 visual recovery 代替字段。

## 6. 迁移 Corin 正式资产

- [x] 6.1 从 Corin Attack 与 Dodge ActionProfile 删除 action-level 旧 correction YAML。
- [x] 6.2 从 Corin ActionMotionPolicy 条目删除旧 correction YAML。
- [x] 6.3 从 Corin GameplayBehaviorProfile 资产删除旧 correction YAML。
- [x] 6.4 检查 Corin MotionCorrectionAck behavior binding 仍指向正式 Ack profile。
- [x] 6.5 确认 Corin PipelineDefinition 没有新增 current direct correction 算法配置。

## 7. 统一文档、调试与清理结果

- [x] 7.1 更新 `openspec/project.md` 的 correction 口径，区分动作 decision、逻辑 correction application 与表现采样。
- [x] 7.2 更新相关 runtime debug 标签和 Inspector 摘要，明确 application result 是运行事实而不是策略。
- [x] 7.3 使用 `rg` 确认正式代码、资产和 current docs 不再引用 `ActionCorrectionPolicy`、`m_CorrectionPolicy` 或 `CancelOnReject`。
- [x] 7.4 使用 `rg` 确认 ActionMotionSample/GameplayActionMotionDigest 不再包含 CorrectionId，Action lifecycle 的 CorrectionId 仍保留。
- [x] 7.5 确认 Presentation 正式代码不再读取 MotionDebug 作为运行输入。
- [x] 7.6 确认不存在 CharacterMotionCorrectionDefinition、legacy enum、FormerlySerializedAs、旧 Ack payload、兼容 parser、双写或 fallback 配置。

## 8. 编译与 OpenSpec 校验

- [x] 8.1 使用带 `--disable-build-servers /nr:false /p:UseSharedCompilation=false` 的 dotnet/msbuild 编译受影响 Runtime 程序集。
- [x] 8.2 使用相同参数编译受影响 Editor 程序集。
- [x] 8.3 编译结束后立即执行 `dotnet build-server shutdown`。
- [x] 8.4 运行 `openspec validate refactor-network-correction-policy-boundaries --strict --no-interactive` 并解决全部问题。

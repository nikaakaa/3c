## 1. 固定边界与冲突清理

- [x] 1.1 记录 `GameplayEffectRuntime` 当前全部公开入口和内部职责分布。
- [x] 1.2 记录 Tag、Attribute、ActiveEffect、PredictionJournal 和 ChangeSet 的当前唯一所有者。
- [x] 1.3 记录 CharacterPipeline 当前 GE 构造、Activate、Begin、Commit、Deactivate 和 Dispose 顺序。
- [x] 1.4 记录 `CharacterGameplayEffectAdapter.LastChangeSet` 的全部读写位置。
- [x] 1.5 记录 Character Graph、ActionRuntime、Motion 和 Presentation 当前可访问的 GE 端口。
- [x] 1.6 记录 `GameplayStateEffectFact` 的全部生产、缓存、消费和模型映射位置。
- [x] 1.7 记录 `StateEffectSyncDomainInput/Output` 的全部读写位置。
- [x] 1.8 记录 `ActionCueEvent` 的全部 Graph、Timeline、Action、Presentation、Network 和 Diagnostics 位置。
- [x] 1.9 记录 ServerAuthoritative StateEffect domain、fact kind、packet kind、payload、resolver、profile、history 和 debug 位置。
- [x] 1.10 更新 `refactor-animation-presentation-authoring-boundary` 的过期 delta，不再声明 `ActionCueEvent`。
- [x] 1.11 更新 `refactor-network-correction-policy-boundaries` 的过期 delta，不再声明 StateEffect 固定 binding。

## 2. 拆分 GameplayEffectRuntime 公开门面

- [x] 2.1 固定 `GameplayEffectRuntime` 继续实现的五个窄合同。
- [x] 2.2 新增内部 `GameplayEffectRuntimeState` 或等价单一状态上下文。
- [x] 2.3 将 TagContainer 引用迁入单一状态上下文。
- [x] 2.4 将 AttributeStore 引用迁入单一状态上下文。
- [x] 2.5 将 ActiveGameplayEffectContainer 引用迁入单一状态上下文。
- [x] 2.6 将 GameplayEffectPredictionJournal 引用迁入单一状态上下文。
- [x] 2.7 将 Tick、handle、instance 和 insertion sequence 计数迁入单一状态上下文。
- [x] 2.8 保持 Runtime 为状态上下文和内部协作者的唯一构造入口。
- [x] 2.9 保持 Runtime Dispose 为全部 GE 状态的唯一释放入口。
- [x] 2.10 删除 Runtime 中已经迁移到协作者的重复字段和辅助方法。

## 3. 拆分 Spec 构建

- [x] 3.1 新增内部 `GameplayEffectSpecFactory`。
- [x] 3.2 迁移 Effect Definition lookup。
- [x] 3.3 迁移 Apply request 与 Context 校验。
- [x] 3.4 迁移 SetByCaller declaration 和 required value 校验。
- [x] 3.5 迁移 Source Tag snapshot 捕获。
- [x] 3.6 迁移 Target Tag snapshot 捕获。
- [x] 3.7 迁移 Source Attribute snapshot 捕获。
- [x] 3.8 迁移 Target Attribute snapshot 捕获。
- [x] 3.9 迁移 Constant、SetByCaller、Snapshot 和 Live magnitude 解析。
- [x] 3.10 迁移 duration、period 和首次 period Tick 换算。
- [x] 3.11 迁移 StackKey 构建。
- [x] 3.12 让 SpecFactory 返回明确成功或拒绝结果，不直接修改运行状态。

## 4. 拆分应用事务与 Component 执行

- [x] 4.1 新增内部 `GameplayEffectMutationTransaction`。
- [x] 4.2 新增内部 `GameplayEffectApplicationService`。
- [x] 4.3 迁移 Instant Effect 应用。
- [x] 4.4 迁移新 ActiveEffect 创建。
- [x] 4.5 迁移已有 ActiveEffect 叠层。
- [x] 4.6 迁移 Duration Keep、Refresh 和 Extend。
- [x] 4.7 迁移 Period Keep 和 Reset。
- [x] 4.8 迁移 Overflow Reject、ReplaceOldest 和 AdditionalEffects。
- [x] 4.9 迁移按 Handle、EffectId、SourceActorId 和 TagQuery 移除。
- [x] 4.10 新增内部 `GameplayEffectComponentExecutor`。
- [x] 4.11 迁移 Modifier Component 执行。
- [x] 4.12 迁移 GrantedTag Component 执行。
- [x] 4.13 迁移 Application、Ongoing 和 Removal Requirement 执行。
- [x] 4.14 迁移 Execution mutation 执行。
- [x] 4.15 迁移 AdditionalEffect 为事务内类型化命令队列。
- [x] 4.16 迁移 Cue Binding 为事务内类型化 cue 操作。
- [x] 4.17 确保事务拒绝时不提交 Attribute、Tag、ActiveEffect 或 Cue 部分修改。

## 5. 拆分生命周期与 ChangeSet

- [x] 5.1 新增内部 `GameplayEffectLifecycleScheduler`。
- [x] 5.2 迁移 period 到点执行。
- [x] 5.3 迁移 duration 到期移除。
- [x] 5.4 迁移 ongoing requirement 检查。
- [x] 5.5 迁移 inhibit 时 Modifier 和 Tag 撤销。
- [x] 5.6 迁移 resume 时同一实例恢复。
- [x] 5.7 保持 inhibited 实例时间继续推进。
- [x] 5.8 新增内部 `GameplayEffectChangeRecorder`。
- [x] 5.9 迁移 lifecycle change 记录。
- [x] 5.10 迁移 attribute change 记录。
- [x] 5.11 迁移 tag count change 记录。
- [x] 5.12 迁移 cue change 记录。
- [x] 5.13 保持每 Tick 只有一个可 drain ChangeSet。

## 6. 拆分预测与权威协调

- [x] 6.1 新增内部 `GameplayEffectPredictionReconciler`。
- [x] 6.2 迁移 PredictionKey journal 建立。
- [x] 6.3 迁移预测 ActiveEffect 创建记录。
- [x] 6.4 迁移预测 stack before snapshot 记录。
- [x] 6.5 迁移预测 Attribute base/revision 记录。
- [x] 6.6 迁移预测 Cue identity 记录。
- [x] 6.7 迁移 Confirm identity 和 revision 对齐。
- [x] 6.8 迁移 Reject 精确撤销。
- [x] 6.9 迁移 Correct 的先撤销后应用顺序。
- [x] 6.10 迁移 Attribute revision 冲突报告。
- [x] 6.11 确保 authoritative EffectInstanceId 更新同步修改容器索引。
- [x] 6.12 确保 Runtime 不把 effect-scoped journal 描述为完整世界 rollback。

## 7. 完成 Character Gameplay Effect ports

- [x] 7.1 新增不可变 `CharacterGameplayEffectGraphPorts`。
- [x] 7.2 在 Graph ports 中暴露 `IGameplayTagReader`。
- [x] 7.3 在 Graph ports 中暴露 `IGameplayAttributeReader`。
- [x] 7.4 在 Graph ports 中暴露 `IGameplayEffectCommandSink`。
- [x] 7.5 禁止 Graph ports 暴露 AuthorityInputSink。
- [x] 7.6 禁止 Graph ports 暴露 Adapter、Runtime 和内部容器。
- [x] 7.7 让 `CharacterGameplayEffectAdapter` 委托 Tag Reader。
- [x] 7.8 让 `CharacterGameplayEffectAdapter` 委托 Attribute Reader。
- [x] 7.9 让 `CharacterGameplayEffectAdapter` 委托 Effect Command Sink。
- [x] 7.10 保持 ActionRuntime 使用 scoped TagSourceSink。

## 8. 完成 Character incoming 映射

- [x] 8.1 新增 `CharacterGameplayEffectInputMapper`。
- [x] 8.2 将 incoming GameplayEffectLifecycleFact 映射为 authority input。
- [x] 8.3 将 incoming GameplayAttributeValueFact 映射为 authority input。
- [x] 8.4 将 GameplayResult 中正式 Effect application 映射为 Apply authority input。
- [x] 8.5 保留 PredictionKey、GameplayResultId、source/target actor 和 tick。
- [x] 8.6 拒绝缺失 EffectId、InstanceId 或 revision 的非法权威输入。
- [x] 8.7 修改 Adapter Begin 接收当前 `CharacterPipelineFrame` 或明确语义输入。
- [x] 8.8 删除 Begin 中固定空 authority input 列表。
- [x] 8.9 保持 incoming 协调发生在 Input 和 BTSMTL 之前。

## 9. 完成 Character outgoing 投影

- [x] 9.1 新增 `CharacterGameplayEffectFactProjector`。
- [x] 9.2 将 lifecycle change 投影为 GameplayEffectLifecycleFact。
- [x] 9.3 将 attribute change 投影为 GameplayAttributeValueFact。
- [x] 9.4 保留 EffectId/BehaviorId、instance、revision、stack、context 和 tick。
- [x] 9.5 为 Effect 引起的 Attribute fact 保留 cause EffectId/BehaviorId。
- [x] 9.6 新增 `CharacterGameplayCueProjector`。
- [x] 9.7 将 GE cue change 投影为 GameplayCueFact。
- [x] 9.8 新增 `CharacterGameplayEffectTraceProjector`。
- [x] 9.9 将 lifecycle、attribute、tag 和 cue change 投影为结构化 trace。
- [x] 9.10 修改 Adapter Commit 显式接收 frame output 和 diagnostics。
- [x] 9.11 在 Commit 中只 drain 一次 ChangeSet。
- [x] 9.12 删除 `LastChangeSet` 字段和属性。

## 10. 接入 CharacterPipeline 和 BTSMTL

- [x] 10.1 修改 CharacterPipeline 构造时创建 GE graph ports。
- [x] 10.2 将 GE graph ports 注入 CharacterGraphContext。
- [x] 10.3 修改 Pipeline Begin 调用传入当前 frame 输入。
- [x] 10.4 修改 Pipeline Commit 调用传入当前 frame 输出和 diagnostics。
- [x] 10.5 保持 `NetworkReceive -> ActionLifecycleInput -> GE Begin -> Input -> BTSMTL -> Motion -> GE Commit -> NetworkSend` 顺序。
- [x] 10.6 新增只读 HasTag/TagQuery Graph 节点端口。
- [x] 10.7 新增只读 Attribute Value Graph 节点端口。
- [x] 10.8 新增受控 ApplyEffect Graph 命令节点。
- [x] 10.9 新增受控 RemoveEffect Graph 命令节点。
- [x] 10.10 为命令节点构造稳定 GameplayEffectContext。
- [x] 10.11 确保 Condition、Decision 和 Value 节点不能获得命令能力。
- [x] 10.12 确保 Effect Runtime 不直接提交 Action lifecycle transition。

## 11. 迁移 Character 正式事实

- [x] 11.1 新增 `GameplayEffectLifecycleFact`。
- [x] 11.2 新增 `GameplayAttributeValueFact`。
- [x] 11.3 新增 `GameplayEffectSyncDomainInput`。
- [x] 11.4 新增 `GameplayEffectSyncDomainOutput`。
- [x] 11.5 将 CharacterPipelineFrame 的 StateEffect bucket 改为 GameplayEffect bucket。
- [x] 11.6 将 CharacterNetworkReceiveStage 改为接收 Effect lifecycle fact。
- [x] 11.7 将 CharacterNetworkReceiveStage 改为接收 Attribute value fact。
- [x] 11.8 将 CharacterNetworkSendStage 改为暴露 GameplayEffect output。
- [x] 11.9 删除 `GameplayStateEffectFact`。
- [x] 11.10 删除 `StateEffectSyncDomainInput`。
- [x] 11.11 删除 `StateEffectSyncDomainOutput`。
- [x] 11.12 删除旧 StateId 和 PayloadDigest 解析。

## 12. 统一 Gameplay Cue

- [x] 12.1 新增正式 `GameplayCueFact`。
- [x] 12.2 将 Timeline cue 生产改为 GameplayCueFact。
- [x] 12.3 将 Graph cue 提交改为 GameplayCueFact。
- [x] 12.4 将 ActionRuntime cue diagnostics 改为 GameplayCueFact 或移交正式 projector。
- [x] 12.5 将 CharacterPipelineOutput Presentation cue bucket 改为 GameplayCueFact。
- [x] 12.6 将 CharacterNetworkReceiveStage cue 输入改为 GameplayCueFact。
- [x] 12.7 将 PresentationStage cue 消费改为 GameplayCueFact。
- [x] 12.8 将 Diagnostics cue 消费改为 GameplayCueFact。
- [x] 12.9 删除 `ActionCueEvent`。
- [x] 12.10 删除 SubmitActionCueEventNode 和旧命名，迁移为统一 GameplayCue 提交节点。

## 13. 迁移 ServerAuthoritativeHybrid 模型映射

- [x] 13.1 将 ServerAuthoritative StateEffect domain 改为 GameplayEffect domain。
- [x] 13.2 将 StateEffect fact kind 拆为正式 GameplayEffect lifecycle 和 Attribute value fact kind。
- [x] 13.3 将 StateEffect packet kind 改为正式 GameplayEffect packet kind。
- [x] 13.4 将 `ServerAuthoritativeStateEffect` 改为类型化 GameplayEffect payload。
- [x] 13.5 新增或迁移类型化 Attribute value payload。
- [x] 13.6 将 incoming GameplayEffect packet 映射为 model-neutral lifecycle fact。
- [x] 13.7 将 incoming Attribute packet 映射为 model-neutral attribute fact。
- [x] 13.8 将 outgoing lifecycle fact 按 Effect BehaviorId 逐条解析 policy。
- [x] 13.9 将 outgoing Attribute fact 按 cause BehaviorId 或显式 fact binding 解析 policy。
- [x] 13.10 将 GameplayCueFact 逐条解析 Event policy。
- [x] 13.11 更新 ServerAuthoritativeCharacterSyncProfile 的 Effect coverage 校验。
- [x] 13.12 更新 Behavior resolver 的 Effect domain 映射。
- [x] 13.13 更新 packet factory 和 stable identity 计算。
- [x] 13.14 更新 reliable queue 分类。
- [x] 13.15 更新 history 记录和 prediction correlation。
- [x] 13.16 更新 model debug 的 BehaviorId、policy、packet 和过滤原因。
- [x] 13.17 删除 StateEffect domain、fact kind、packet kind 和 payload。
- [x] 13.18 删除 ActionCue 专用 model payload 和映射。

## 14. 收口 Authoring、Registry 与配置

- [x] 14.1 将 GameplayEffectDefinition 纳入 Character 统一 Behavior registry。
- [x] 14.2 校验 Action、generic Behavior 和 Effect BehaviorId 全局重复。
- [x] 14.3 让 CharacterPipelineDefinition Inspector 展示 Effect Behavior 条目。
- [x] 14.4 让 ServerAuthoritative Profile Inspector 按 Effect BehaviorId 选择 policy。
- [x] 14.5 删除固定 StateEffect behavior binding UI。
- [x] 14.6 删除 Effect Definition 中任何模型策略字段或推断入口。
- [x] 14.7 更新 Corin CharacterGameplayEffectProfile 正式引用。
- [x] 14.8 更新 Corin 使用的 Tag、Attribute 和 Effect Definition 资产。
- [x] 14.9 更新 Corin ServerAuthoritative Profile 的 Effect policy 条目。
- [x] 14.10 缺失 GE Profile、Effect registry 或模型 policy 时明确配置失败。

## 15. 文档、清理与自动校验

- [x] 15.1 更新 `character-network-sync-domain-contract` 的 StateEffect 旧口径。
- [x] 15.2 更新 `character-pipeline-runtime` 的 StateEffect 旧口径。
- [x] 15.3 更新 `character-gameplay-pipeline-closure` 的 ActionCueEvent 旧口径。
- [x] 15.4 更新 `gameplay-behavior-policy-model` 的 State 和网络策略摘要旧口径。
- [x] 15.5 更新 `openspec/project.md` 的 GE、Character bridge 和网络模型口径。
- [x] 15.6 搜索并删除 Runtime 中全部 `GameplayStateEffectFact` 引用。
- [x] 15.7 搜索并删除 Runtime 中全部 `StateEffectSyncDomain` 引用。
- [x] 15.8 搜索并删除 Runtime 中全部 `ActionCueEvent` 引用。
- [x] 15.9 搜索并删除 Runtime 中全部 `ServerAuthoritativeStateEffect` 引用。
- [x] 15.10 搜索并确认 GE 程序集不引用 Character、BTSMTL、Networking、Presentation 或 Diagnostics。
- [x] 15.11 搜索并确认 CharacterPipeline 不访问 GE 内部容器。
- [x] 15.12 搜索并确认不存在 GE MonoBehaviour Update、Coroutine、静态 Manager 或 ServiceLocator。
- [x] 15.13 使用项目要求的禁用 build server 参数编译相关 C# 工程。
- [x] 15.14 编译结束后立即关闭 .NET build server。
- [x] 15.15 执行 `openspec validate refactor-gameplay-effect-runtime-integration --strict --no-interactive`。
- [x] 15.16 执行 `openspec validate --all --strict --no-interactive`。

## 1. 固定实施前边界

- [x] 1.1 确认 `refactor-gameplay-network-model-boundary` 已归档，并以归档后的 current spec 与代码命名为唯一集成基线。
- [x] 1.2 记录完成后的 CharacterPipeline Logic Tick 顺序和共享输入输出类型。
- [x] 1.3 记录完成后的 GameplayEffect semantic input/output 类型和 ServerAuthoritative 映射入口。
- [x] 1.4 记录完成后的 ActionProfile、GameplayBehaviorProfile 和 CharacterPipelineDefinition 字段。
- [x] 1.5 搜索并列出 `ActionRuntime.m_Tags`、`SetTag()`、`HasTag()` 的全部调用位置。
- [x] 1.6 搜索并列出 `ActionProfile.Tags/BlockTags/CancelTags` 的全部作者和运行时读取位置。
- [x] 1.7 搜索并列出 `GameplayBehaviorProfile.Tags` 的全部作者和运行时读取位置。
- [x] 1.8 搜索并列出旧 `GameplayStateEffectFact` 的全部生产、缓存、消费和模型映射位置。
- [x] 1.9 搜索并列出 `ActionCueEvent` 的全部生产、消费、调试和模型映射位置。
- [x] 1.10 确认旧 `KaaKaaFramework` 不在 3C 工程 assembly、asset 或 Addressables 依赖中。

## 2. 建立 Gameplay 程序集、通用合同与 Tag 正式模型

- [ ] 2.1 在正式 `Runtime/Gameplay` 目录创建 `ThirdPersonGameplay.asmdef`。
- [ ] 2.2 限制 `ThirdPersonGameplay` 不引用 Character、BTSMTL、Networking、Presentation 或 Diagnostics 程序集与命名空间。
- [ ] 2.3 将通用 `GameplayBehaviorKind` 迁移到 Gameplay Contracts。
- [ ] 2.4 将通用 `IGameplayBehaviorProfile` 迁移到 Gameplay Contracts。
- [ ] 2.5 更新 ActionProfile、GameplayBehaviorProfile、CharacterPipelineDefinition 和 ServerAuthoritative 模型对新 Behavior 合同的引用。
- [ ] 2.6 删除 `ThirdPersonCharacter.Behavior` 中已经迁移的旧 Behavior enum/interface，不保留转发类型。
- [ ] 2.7 新增不引用 `GameplayLogicTickContext` 的 `GameplayEffectTickContext`。
- [ ] 2.8 新增 `IGameplayTagReader`、scoped `IGameplayTagSourceSink`、`IGameplayAttributeReader`、`IGameplayEffectCommandSink` 和 `IGameplayEffectAuthorityInputSink` 窄合同。
- [ ] 2.9 新增类型化 Apply/Remove request、result 与 `GameplayEffectChangeSet` 合同。
- [ ] 2.10 新增可序列化的 `GameplayTagId` 值类型。
- [ ] 2.11 为 `GameplayTagId` 实现稳定相等、排序和空值校验。
- [ ] 2.12 新增 Gameplay Tag Catalog authoring 类型。
- [ ] 2.13 在 Catalog 中保存 TagId、显示名、父 Tag 和调试分类。
- [ ] 2.14 实现 Catalog 重复 TagId 校验。
- [ ] 2.15 实现 Catalog 缺失父 Tag 校验。
- [ ] 2.16 实现 Catalog 父子环校验。
- [ ] 2.17 实现 Catalog 到运行时 compact index 的构建。
- [ ] 2.18 实现子 Tag 匹配自身与祖先 Tag 的查询。
- [ ] 2.19 新增 All/Any/None `GameplayTagQuery` authoring 类型。
- [ ] 2.20 实现 Tag Query 对 runtime container 的层级匹配。
- [ ] 2.21 新增稳定 `GameplayTagSourceHandle`。
- [ ] 2.22 实现按 source handle 添加一组 Tag。
- [ ] 2.23 实现按 source handle 精确移除全部 Tag。
- [ ] 2.24 实现同一 Tag 多来源计数。
- [ ] 2.25 实现 Tag count 从零到一和从一到零的变更记录。
- [ ] 2.26 拒绝未注册 Catalog Tag 进入运行时 Container。
- [ ] 2.27 为 Character 初始 Tag 建立固定 source handle。

## 3. 建立 Gameplay Attribute 正式模型

- [ ] 3.1 新增可序列化的 `GameplayAttributeId` 值类型。
- [ ] 3.2 为 `GameplayAttributeId` 实现稳定相等、排序和空值校验。
- [ ] 3.3 新增 Gameplay Attribute Definition authoring 类型。
- [ ] 3.4 在 Attribute Definition 中保存显示、分类和边界定义。
- [ ] 3.5 支持常量最小/最大边界。
- [ ] 3.6 支持引用另一 Attribute 的动态边界。
- [ ] 3.7 实现重复 AttributeId 校验。
- [ ] 3.8 实现缺失边界 Attribute 引用校验。
- [ ] 3.9 实现 Attribute 依赖环校验。
- [ ] 3.10 新增 `GameplayAttributeValue` 的 BaseValue、CurrentValue 和 Revision。
- [ ] 3.11 新增 `GameplayModifierOperation` 的 Additive、Multiplicative、Override 和 Clamp。
- [ ] 3.12 新增带来源 EffectHandle、优先级和插入序列的 Modifier handle。
- [ ] 3.13 实现 `Base + Additive` 聚合。
- [ ] 3.14 实现 Multiplicative 聚合。
- [ ] 3.15 实现最高优先级 Override 选择。
- [ ] 3.16 实现 Override 后的最终 Clamp。
- [ ] 3.17 实现相同优先级按稳定插入序列确定结果。
- [ ] 3.18 实现按 Modifier handle 精确移除。
- [ ] 3.19 实现按 ActiveEffectHandle 批量移除 Modifier。
- [ ] 3.20 实现 Attribute 脏标记和按需重算。
- [ ] 3.21 实现依赖 Attribute 变更后的有向脏标记传播。
- [ ] 3.22 实现 BaseValue mutation 并递增 Revision。
- [ ] 3.23 实现 CurrentValue 变化记录并保留 before/after。
- [ ] 3.24 实现 Character 初始 BaseValue 装配。
- [ ] 3.25 拒绝配置中未初始化的 Attribute。
- [ ] 3.26 删除对旧 GenericMath、PropertyHandler 和字符串父属性通知的需求。

## 4. 建立 Gameplay Effect 定义与 Spec

- [ ] 4.1 新增 `GameplayEffectDurationPolicy` 的 Instant、Duration 和 Infinite。
- [ ] 4.2 新增 `GameplayEffectDefinition` ScriptableObject。
- [ ] 4.3 让 Effect Definition 实现 `IGameplayBehaviorProfile`。
- [ ] 4.4 固定 EffectId 与 BehaviorId 使用同一稳定身份。
- [ ] 4.5 固定 Effect Definition 的 BehaviorKind 为 Effect。
- [ ] 4.6 新增 Definition revision 计算输入。
- [ ] 4.7 新增 `GameplayEffectContext`。
- [ ] 4.8 在 Context 中保存 source/target actor identity。
- [ ] 4.9 在 Context 中保存 ActionInstanceId 和 PredictionKey。
- [ ] 4.10 在 Context 中保存 GameplayResultId 和 source tick。
- [ ] 4.11 新增 `GameplayEffectSpec`。
- [ ] 4.12 新增声明式 SetByCaller 参数定义。
- [ ] 4.13 实现 SetByCaller 重复参数校验。
- [ ] 4.14 实现缺失必需 SetByCaller 时拒绝 Spec。
- [ ] 4.15 实现未声明 SetByCaller 时拒绝 Spec。
- [ ] 4.16 新增 Constant magnitude。
- [ ] 4.17 新增 Source Attribute Snapshot magnitude。
- [ ] 4.18 新增 Target Attribute Snapshot magnitude。
- [ ] 4.19 新增 Target Attribute Live magnitude。
- [ ] 4.20 拒绝跨角色 Source Attribute Live magnitude。
- [ ] 4.21 将 authoring 秒数通过正式 Tick 配置转换为整数 Tick。
- [ ] 4.22 拒绝缺失 Tick 配置的 Duration/Period Effect。
- [ ] 4.23 在 Spec 中锁定 DurationTick、PeriodTick 和首次 PeriodTick。
- [ ] 4.24 在 Spec 中锁定需要快照的 Source/Target Tag。
- [ ] 4.25 在 Spec 中锁定需要快照的 Attribute 值。
- [ ] 4.26 确保 Definition 和 Component Definition 不写运行时状态。

## 5. 建立 Effect Component 和 Execution

- [ ] 5.1 新增内联无状态 Effect Component Definition 基类。
- [ ] 5.2 新增 Modifier Component Definition。
- [ ] 5.3 支持 Modifier 选择 Base mutation 或 Active Current modifier。
- [ ] 5.4 新增 Granted Tags Component Definition。
- [ ] 5.5 新增 Source/Target Tag Application Requirement。
- [ ] 5.6 新增 Source/Target Attribute Application Requirement。
- [ ] 5.7 新增 Ongoing Tag Requirement。
- [ ] 5.8 新增 Ongoing Attribute Requirement。
- [ ] 5.9 新增 Removal Requirement。
- [ ] 5.10 新增无状态 Gameplay Effect Execution Definition。
- [ ] 5.11 定义 Execution 的类型化 Attribute mutation 输出。
- [ ] 5.12 定义 Execution 的类型化 Additional Effect 输出。
- [ ] 5.13 新增 Additional Effects Component Definition。
- [ ] 5.14 支持 Applied 触发 Additional Effect。
- [ ] 5.15 支持 Period 触发 Additional Effect。
- [ ] 5.16 支持 Removed 触发 Additional Effect。
- [ ] 5.17 支持 Overflow 触发 Additional Effect。
- [ ] 5.18 实现 Additional Effect 完整引用图校验。
- [ ] 5.19 拒绝 Additional Effect 自环和间接环。
- [ ] 5.20 新增 Effect Cue Binding Component Definition。

## 6. 建立 Active Effect 生命周期

- [ ] 6.1 新增角色作用域内唯一 `GameplayEffectInstanceId`。
- [ ] 6.2 新增 `GameplayEffectHandle`。
- [ ] 6.3 新增 `ActiveGameplayEffect`。
- [ ] 6.4 新增 Active Effect Container。
- [ ] 6.5 实现 Container 稳定插入序列。
- [ ] 6.6 实现遍历期间的内部 mutation buffer。
- [ ] 6.7 实现 Instant Effect 不进入 Container 的执行路径。
- [ ] 6.8 实现 Duration Effect 的 `[StartTick, EndTick)` 生命周期。
- [ ] 6.9 实现 Infinite Effect 生命周期。
- [ ] 6.10 实现 ExecuteOnApplication。
- [ ] 6.11 实现到期 Period 的稳定顺序执行。
- [ ] 6.12 禁止 `NextPeriodTick >= EndTick` 的周期执行。
- [ ] 6.13 实现 Expired 生命周期结果。
- [ ] 6.14 实现按 Handle 精确移除。
- [ ] 6.15 实现按 EffectId 查询移除。
- [ ] 6.16 实现按 SourceActorId 查询移除。
- [ ] 6.17 实现按 Effect Tag Query 查询移除。
- [ ] 6.18 返回批量移除的实际 Handle 列表。
- [ ] 6.19 实现 Ongoing Requirement inhibition。
- [ ] 6.20 Inhibited 时移除 Modifier 和 Granted Tag。
- [ ] 6.21 Inhibited 时停止 Period 和 WhileActive cue。
- [ ] 6.22 Requirement 恢复时恢复 Modifier 和 Granted Tag。
- [ ] 6.23 实现 Removal Requirement 命中后的正式移除。
- [ ] 6.24 实现 Container 清理时不产生新业务 Effect。
- [ ] 6.25 新增不可变 `GameplayEffectRuntimeDefinition`。
- [ ] 6.26 新增 `GameplayEffectRuntime` 并让它唯一组合 Tag、Attribute、Active Effect 和 prediction journal。
- [ ] 6.27 让 GameplayEffectRuntime 只接收 Gameplay Contracts 中的 Tick、Apply、Remove 和 Authority input。
- [ ] 6.28 让 GameplayEffectRuntime 通过窄 reader/command 合同暴露能力，不暴露内部 Container。
- [ ] 6.29 让 GameplayEffectRuntime 为当前 Tick 累积唯一 `GameplayEffectChangeSet`。
- [ ] 6.30 保持状态 mutation 同步生效，ChangeSet 只记录结果而不承担延迟 mutation。
- [ ] 6.31 禁止 GameplayEffectRuntime 直接写 Character frame、SyncFacts、Cue、Trace 或网络对象。
- [ ] 6.32 禁止 GameplayEffectRuntime 使用全局事件总线、ServiceLocator 或消费者回调。

## 7. 建立叠层策略

- [ ] 7.1 新增 Independent stacking。
- [ ] 7.2 新增 AggregateBySource stacking。
- [ ] 7.3 新增 AggregateByTarget stacking。
- [ ] 7.4 实现稳定 StackKey。
- [ ] 7.5 实现 MaxStacks 校验。
- [ ] 7.6 实现 Duration Keep。
- [ ] 7.7 实现 Duration Refresh。
- [ ] 7.8 实现 Duration Extend。
- [ ] 7.9 实现 Period Keep。
- [ ] 7.10 实现 Period Reset。
- [ ] 7.11 实现 Overflow Reject。
- [ ] 7.12 实现 Overflow ReplaceOldest。
- [ ] 7.13 实现 Overflow Additional Effects。
- [ ] 7.14 实现 StackChanged 生命周期结果。
- [ ] 7.15 实现层数变化后的 Modifier magnitude 重算。
- [ ] 7.16 实现层数降为零后的正式移除。

## 8. 建立 Effect 局部预测日志

- [ ] 8.1 新增 Confirmed/Predicted application mode。
- [ ] 8.2 拒绝没有 ActionInstanceId/PredictionKey 的 Predicted Spec。
- [ ] 8.3 新增 Effect-scoped prediction journal。
- [ ] 8.4 记录预测创建的 Active Effect。
- [ ] 8.5 记录预测叠层的 before/after。
- [ ] 8.6 记录预测 Base mutation 的 before/after revision。
- [ ] 8.7 记录预测添加的 Modifier handles。
- [ ] 8.8 记录预测添加的 Tag source handle。
- [ ] 8.9 记录预测 Cue identity。
- [ ] 8.10 实现 Action Confirm 对齐 authoritative instance/revision。
- [ ] 8.11 实现 Action Reject 精确撤销 journal。
- [ ] 8.12 实现 Action Correct 先撤销再应用 authoritative facts。
- [ ] 8.13 防止撤销覆盖其他来源的新 revision。
- [ ] 8.14 清理终态 Action 对应的已确认 journal。

## 9. 接入 CharacterPipeline

- [ ] 9.1 新增 `CharacterGameplayEffectProfile`。
- [ ] 9.2 在 CharacterGameplayEffectProfile 中引用唯一 Tag Catalog。
- [ ] 9.3 在 CharacterGameplayEffectProfile 中保存 Attribute Definitions。
- [ ] 9.4 在 CharacterGameplayEffectProfile 中保存 Initial Attribute Values。
- [ ] 9.5 在 CharacterGameplayEffectProfile 中保存 Initial Tags。
- [ ] 9.6 在 CharacterGameplayEffectProfile 中保存 Effect Registry。
- [ ] 9.7 在 CharacterPipelineDefinition 中增加唯一 CharacterGameplayEffectProfile 引用。
- [ ] 9.8 将 Effect BehaviorId 纳入 CharacterPipelineDefinition 全局唯一校验。
- [ ] 9.9 实现 Effect、Attribute、Tag 的配置闭包校验。
- [ ] 9.10 新增从 Character authoring 资产构建不可变 `GameplayEffectRuntimeDefinition` 的 Builder。
- [ ] 9.11 让 Builder 在缺失引用、重复 identity 或非法闭包时拒绝创建 runtime，不生成空配置 fallback。
- [ ] 9.12 新增薄 `CharacterGameplayEffectAdapter`。
- [ ] 9.13 让 Adapter 唯一持有一个 `GameplayEffectRuntime`。
- [ ] 9.14 新增 `CharacterGameplayEffectInputMapper`。
- [ ] 9.15 让 InputMapper 把 Character Effect/Attribute semantic input 转换为 `GameplayEffectAuthorityInput`。
- [ ] 9.16 新增 `CharacterGameplayEffectFactProjector`。
- [ ] 9.17 让 FactProjector 把 ChangeSet 投影为 Effect/Attribute Character SyncFacts。
- [ ] 9.18 新增 `CharacterGameplayCueProjector`。
- [ ] 9.19 让 CueProjector 把 ChangeSet 投影为正式 GameplayCueFact。
- [ ] 9.20 新增 `CharacterGameplayEffectTraceProjector`。
- [ ] 9.21 让 TraceProjector 在有效 diagnostics interest 下投影 ChangeSet。
- [ ] 9.22 禁止 Adapter、Mapper 和 Projector 实现 Effect、Attribute、Tag、stack、period 或 prediction 业务规则。
- [ ] 9.23 让 CharacterPipeline 唯一构造 CharacterGameplayEffectAdapter。
- [ ] 9.24 在 Pipeline Activate 通过 Adapter 初始化 Attribute 和 base Tag。
- [ ] 9.25 在 Action lifecycle resolve 后调用 Adapter BeginLogicTick。
- [ ] 9.26 在 InputMapper 中处理 Action confirm/reject/correct。
- [ ] 9.27 在 InputMapper 中处理 incoming GameplayEffectLifecycleFact。
- [ ] 9.28 在 InputMapper 中处理 incoming GameplayAttributeValueFact。
- [ ] 9.29 让 Adapter 使用 GameplayEffectTickContext 推进 Runtime 的 Period 和 Expire。
- [ ] 9.30 在 MotionStage 后调用 Adapter CommitFacts 并 drain 唯一 ChangeSet。
- [ ] 9.31 在 Deactivate 通过 Runtime 清理 Active Effect、Modifier、Tag 和 journal。
- [ ] 9.32 在 Dispose 释放 Attribute dependency、Runtime 和 Adapter 资源。
- [ ] 9.33 确认 Adapter 与 GameplayEffectRuntime 都不读取 Unity Time 或启动 Coroutine。

## 10. 迁移 Action 与 Behavior Tag

- [ ] 10.1 为 ActionRuntime 注入只读 Tag 查询接口。
- [ ] 10.2 为 ActionRuntime 注入 ActionInstance Tag source sink。
- [ ] 10.3 将 ActionProfile Tags 迁移为正式 TagId。
- [ ] 10.4 将 ActionProfile BlockTags 迁移为正式 Tag Query。
- [ ] 10.5 将 ActionProfile CancelTags 迁移为正式 Tag Query。
- [ ] 10.6 将 GameplayBehaviorProfile Tags 迁移为正式 TagId。
- [ ] 10.7 在 Action 激活成功后添加 ActionInstance Tag source。
- [ ] 10.8 在 Action 终态移除 ActionInstance Tag source。
- [ ] 10.9 让 activation validation 查询统一 Gameplay Tag Container。
- [ ] 10.10 保持 CancelTags 与当前 ActionProfile Tags 的事务判断。
- [ ] 10.11 删除 `ActionRuntime.m_Tags`。
- [ ] 10.12 删除 `ActionRuntime.SetTag()`。
- [ ] 10.13 删除 Action/Behavior 字符串 Tag 序列化字段。
- [ ] 10.14 更新 ActionProfile Inspector 的 Tag Catalog 选择 UI。
- [ ] 10.15 更新 GameplayBehaviorProfile Inspector 的 Tag Catalog 选择 UI。
- [ ] 10.16 更新 Corin ActionProfile 和 BehaviorProfile Tag 数据。
- [ ] 10.17 删除旧 Tag 字符串数据和兼容读取逻辑。

## 11. 接入 CharacterGraphContext 与 BTSMTL

- [ ] 11.1 新增 `IGameplayEffectGraphPortSource` 或等价单一 Graph context 端口来源。
- [ ] 11.2 让 CharacterGraphContext 只暴露 Gameplay Effect graph port，不实现 GE 业务方法。
- [ ] 11.3 让 graph port 分别提供 Gameplay Tag reader、Attribute reader 和 Effect command sink。
- [ ] 11.4 新增 `HasGameplayTagValueNode`。
- [ ] 11.5 新增 `MatchGameplayTagQueryValueNode`。
- [ ] 11.6 新增 `ReadGameplayAttributeValueNode`。
- [ ] 11.7 新增 `CanApplyGameplayEffectValueNode`。
- [ ] 11.8 新增 `ApplyGameplayEffectNode`。
- [ ] 11.9 让 Apply 节点输出 application result 和 handle。
- [ ] 11.10 新增 `RemoveGameplayEffectNode`。
- [ ] 11.11 让 Remove 节点支持 Handle 和正式 Query。
- [ ] 11.12 允许只读节点进入普通 Graph。
- [ ] 11.13 允许只读节点进入 ConditionRuleGraph。
- [ ] 11.14 允许只读节点进入 Decision TreeClip。
- [ ] 11.15 禁止 Apply/Remove 节点进入 ConditionRuleGraph。
- [ ] 11.16 禁止 Apply/Remove 节点进入 Decision TreeClip。
- [ ] 11.17 禁止 Graph 节点直接解析其他 CharacterPipeline。
- [ ] 11.18 更新 Graph Validator 的节点能力规则。
- [ ] 11.19 更新 Agent Node emitter registry。
- [ ] 11.20 更新 Agent snapshot 的 Tag/Attribute/Effect 只读投影。
- [ ] 11.21 更新 Agent validator 的 Effect 引用闭包校验。
- [ ] 11.22 禁止任何 Graph 节点持有 CharacterGameplayEffectAdapter、GameplayEffectRuntime、Container 或 ActiveEffect instance。

## 12. 扩展 Graph Data Catalog 与 Authoring

- [ ] 12.1 新增 Gameplay Effect Catalog source。
- [ ] 12.2 为 Tag 条目提供稳定 identity 和层级 category。
- [ ] 12.3 为 Attribute 条目提供稳定 identity、值类型和边界详情。
- [ ] 12.4 为 Effect 条目提供稳定 identity、Duration、Period 和 Stack 摘要。
- [ ] 12.5 为 Tag 条目提供 HasTag/MatchQuery 节点创建能力。
- [ ] 12.6 为 Attribute 条目提供 ReadAttribute 节点创建能力。
- [ ] 12.7 为普通可写 Graph 的 Effect 条目提供 ApplyEffect 节点创建能力。
- [ ] 12.8 为只读 Graph 的 Effect 条目只提供 CanApply/详情能力。
- [ ] 12.9 拒绝 Catalog 通过显示名解析 Tag、Attribute 或 Effect。
- [ ] 12.10 在 Character authoring context 缺失时显示明确状态。
- [ ] 12.11 在 CharacterPipelineDefinition Inspector 展示 CharacterGameplayEffectProfile。
- [ ] 12.12 在 Inspector 展示 Tag/Attribute/Effect 配置错误。
- [ ] 12.13 为内联 Effect Components 提供统一编辑入口。
- [ ] 12.14 禁止 Component Definition 保存运行时字段。
- [ ] 12.15 更新 Agent authoring schema 的 Gameplay Effect 只读投影。

## 13. 删除旧 GameplayState 占位并建立 Gameplay Effect 与 Attribute 语义事实

- [ ] 13.1 新增 `GameplayEffectLifecycleOperation`。
- [ ] 13.2 新增类型化 `GameplayEffectLifecycleFact`。
- [ ] 13.3 在 GameplayEffectLifecycleFact 中保存 BehaviorId/EffectId。
- [ ] 13.4 在 GameplayEffectLifecycleFact 中保存 EffectInstanceId 和 LifecycleRevision。
- [ ] 13.5 在 GameplayEffectLifecycleFact 中保存 SourceActorId、ActionInstanceId 和 PredictionKey。
- [ ] 13.6 在 GameplayEffectLifecycleFact 中保存 GameplayResultId。
- [ ] 13.7 在 GameplayEffectLifecycleFact 中保存 StartTick、EndTick 和 StackCount。
- [ ] 13.8 在 GameplayEffectLifecycleFact 中保存声明式 SetByCaller values。
- [ ] 13.9 在 GameplayEffectLifecycleFact 中保存 DefinitionRevision，不保留 PayloadDigest。
- [ ] 13.10 新增类型化 `GameplayAttributeValueFact`。
- [ ] 13.11 在 GameplayAttributeValueFact 中保存 Base、Current、ValueRevision 和 SourceTick。
- [ ] 13.12 在 GameplayAttributeValueFact 中保存 CauseEffectInstanceId。
- [ ] 13.13 将旧 `StateEffectSyncDomainOutput` 改名为 `GameplayEffectSyncDomainOutput`，并拆成 Effect 和 Attribute 两类 typed list。
- [ ] 13.14 将旧 `StateEffectSyncDomainInput` 改名为 `GameplayEffectSyncDomainInput`，并拆成 Effect 和 Attribute 两类 typed list。
- [ ] 13.15 让 CharacterNetworkReceiveStage 只缓存新的语义事实。
- [ ] 13.16 让 CharacterNetworkSendStage 只收集新的语义事实。
- [ ] 13.17 删除旧最小 `GameplayStateEffectFact`。
- [ ] 13.18 删除 PayloadDigest 驱动的隐藏恢复路径。
- [ ] 13.19 实现 incoming fact revision 去重和顺序拒绝。
- [ ] 13.20 缺失 Effect Definition 或 revision 不一致时报告配置错误。
- [ ] 13.21 让 CharacterGameplayEffectFactProjector 成为 GE ChangeSet 到 GameplayEffectLifecycleFact/GameplayAttributeValueFact 的唯一投影入口。
- [ ] 13.22 让 CharacterGameplayEffectInputMapper 成为 incoming GameplayEffectLifecycleFact/GameplayAttributeValueFact 到 AuthorityInput 的唯一翻译入口。
- [ ] 13.23 禁止 GameplayEffectRuntime 和 CharacterGameplayEffectAdapter 直接读写 CharacterNetworkReceiveStage 或 CharacterNetworkSendStage。
- [ ] 13.24 将 `GameplayBehaviorKind.State` 改名为 `GameplayBehaviorKind.Effect`。
- [ ] 13.25 更新 Behavior registry、policy resolver、Inspector 和 diagnostics 对 `GameplayBehaviorKind.Effect` 的唯一映射。
- [ ] 13.26 删除旧 `GameplayBehaviorKind.State` 枚举值与全部 switch 分支，不保留别名。
- [ ] 13.27 删除旧 `StateId` 与 `PayloadDigest` 事实字段和解析路径。
- [ ] 13.28 更新 `character-network-sync-domain-contract` 的 current purpose 与 requirement 口径，只保留 GameplayEffectSyncDomain。
- [ ] 13.29 更新 `gameplay-behavior-policy-model` 与 `character-syncfact-behavior-binding` 的 current purpose、场景和调试口径。
- [ ] 13.30 确认 objective ownership、capture 和 contest 不进入 GameplayEffectSyncDomain。

## 14. 接入 ServerAuthoritativeHybrid 模型

- [ ] 14.1 扩展 ServerAuthoritative Effect packet payload。
- [ ] 14.2 扩展 ServerAuthoritative Attribute value packet payload。
- [ ] 14.3 让 outgoing adapter 映射 GameplayEffectLifecycleFact。
- [ ] 14.4 让 outgoing adapter 映射 GameplayAttributeValueFact。
- [ ] 14.5 让 incoming adapter 转回模型无关 GameplayEffectLifecycleFact。
- [ ] 14.6 让 incoming adapter 转回模型无关 GameplayAttributeValueFact。
- [ ] 14.7 在模型 profile 中按 Effect BehaviorId 解析 Effect policy。
- [ ] 14.8 在模型 profile 中按 GameplayAttributeValueFact kind 解析正式 binding。
- [ ] 14.9 为 predicted GameplayEffectLifecycleFact 记录模型所需 history。
- [ ] 14.10 保持 GE Runtime 不引用模型 packet、policy、history 或 endpoint。
- [ ] 14.11 更新 LocalLoopback 对 Effect confirm/reject/correct 的语义输出。
- [ ] 14.12 将旧 `ServerAuthoritativeStateEffect` 改为正式 `ServerAuthoritativeGameplayEffect` payload，并删除旧类型。
- [ ] 14.13 确认 EffectDefinition 只提供 BehaviorId/BehaviorKind，ServerAuthoritative prediction、authority、replication 和 history policy 只存在于模型 Profile。
- [ ] 14.14 禁止 CharacterGameplayEffectAdapter、FactProjector 和 NetworkSendStage 解析模型 policy。

## 15. 接入 GameplayResult 与跨角色路由

- [ ] 15.1 保持 GameplayResultFact 与 GameplayEffectLifecycleFact 为不同 typed fact。
- [ ] 15.2 在两类 fact 中使用共同 GameplayResultId 关联因果。
- [ ] 15.3 让权威 hit result 产生目标 Apply Damage GameplayEffectLifecycleFact。
- [ ] 15.4 让环境伤害产生无 ActionInstance 的 Apply Damage GameplayEffectLifecycleFact。
- [ ] 15.5 让目标 CharacterPipeline 消费属于自身 actor 的 GameplayEffectLifecycleFact。
- [ ] 15.6 禁止 Timeline 或 ActionWindow 直接修改目标 Attribute。
- [ ] 15.7 禁止 LocalLoopback 通过目标对象引用直接扣血。
- [ ] 15.8 让跨角色 Effect 继续通过精确 actor semantic port 路由。

## 16. 接入 Presentation Cue

- [ ] 16.1 新增通用 `GameplayCueFact`。
- [ ] 16.2 迁移 Action cue 生产者到 GameplayCueFact。
- [ ] 16.3 迁移 Effect lifecycle cue 生产者到 GameplayCueFact。
- [ ] 16.4 映射 Applied/Resumed 到 OnActive。
- [ ] 16.5 映射 Instant/PeriodExecuted 到 Executed。
- [ ] 16.6 映射 active 非 inhibited 到 WhileActive。
- [ ] 16.7 映射 Removed/Rejected 到 Removed。
- [ ] 16.8 映射 Expired 到 Expired。
- [ ] 16.9 让 PresentationSyncDomain 统一收集 GameplayCueFact。
- [ ] 16.10 更新 ServerAuthoritative cue adapter。
- [ ] 16.11 更新 Timeline 和 Graph cue 节点。
- [ ] 16.12 删除旧 `ActionCueEvent` 类型和序列化路径。
- [ ] 16.13 确保 Effect 不直接实例化 VFX、SFX 或 Camera 对象。

## 17. 接入 Runtime Diagnostics

- [ ] 17.1 在 RuntimeTraceChannel 新增 GameplayEffect bit。
- [ ] 17.2 将 GameplayEffect 加入 RuntimeTraceChannel.All。
- [ ] 17.3 新增 Effect applied/rejected/confirmed/corrected 事件类型。
- [ ] 17.4 新增 Effect stack/inhibited/resumed 事件类型。
- [ ] 17.5 新增 Period/removed/expired 事件类型。
- [ ] 17.6 新增 Attribute Base/Current changed 事件类型。
- [ ] 17.7 新增 Tag source count changed 事件类型。
- [ ] 17.8 新增 prediction journal confirm/rollback 事件类型。
- [ ] 17.9 为 GameplayEffect Live State 定义稳定键。
- [ ] 17.10 为 Effect/Attribute/Tag trace 定义受限 payload。
- [ ] 17.11 没有 interest/Capture 时跳过 GameplayEffect payload 构造。
- [ ] 17.12 让 Host Inspector 通过现有 Session 展示 GameplayEffect。
- [ ] 17.13 让 Capture snapshot 包含 GameplayEffect channel。
- [ ] 17.14 保持 diagnostics 只读且不影响 GE 结果。

## 18. 创建首批正式 Gameplay Effect 内容

- [ ] 18.1 创建 Corin Gameplay Tag Catalog。
- [ ] 18.2 定义 Action、State、Cooldown 和 Effect Tag 层级。
- [ ] 18.3 创建 Health Attribute Definition。
- [ ] 18.4 创建 MaxHealth Attribute Definition。
- [ ] 18.5 创建 Stamina Attribute Definition。
- [ ] 18.6 创建 MaxStamina Attribute Definition。
- [ ] 18.7 创建 Poise Attribute Definition。
- [ ] 18.8 创建 MoveSpeed Attribute Definition。
- [ ] 18.9 创建 Damage Instant Effect。
- [ ] 18.10 创建 Heal Instant Effect。
- [ ] 18.11 创建 Attack Stamina Cost Effect。
- [ ] 18.12 创建 Attack Cooldown Duration Effect。
- [ ] 18.13 创建 Invulnerability Duration Effect。
- [ ] 18.14 创建 Stun Duration Effect。
- [ ] 18.15 创建 MoveSpeed Modifier Duration Effect。
- [ ] 18.16 将 Corin CharacterPipelineDefinition 绑定正式 CharacterGameplayEffectProfile。
- [ ] 18.17 将 Corin ActionProfile Block/Cancel/Action Tags 迁移到 Catalog。
- [ ] 18.18 更新 Sandbox 的 ServerAuthoritative Effect behavior policy 引用。

## 19. 清理与编译

- [ ] 19.1 搜索并删除 Runtime 中旧字符串 Tag API。
- [ ] 19.2 搜索并删除旧 `GameplayStateEffectFact` API。
- [ ] 19.3 搜索并删除旧 `ActionCueEvent` API。
- [ ] 19.4 确认 Runtime 不引用旧 KaaKaaFramework assembly 或路径。
- [ ] 19.5 确认 Runtime 不包含 GE MonoBehaviour Update。
- [ ] 19.6 确认 Runtime 不包含 GE Coroutine/WaitForSeconds。
- [ ] 19.7 确认 Runtime 不包含 Effect Addressables 名称加载。
- [ ] 19.8 确认 Runtime 不包含 Effect `params object[]` 回调。
- [ ] 19.9 确认 Blackboard 不保存 Attribute、TagContainer 或 ActiveEffect 真相。
- [ ] 19.10 确认 ActionRuntime 不推进 Effect 生命周期。
- [ ] 19.11 确认 GE 不直接修改其他 CharacterPipeline 或 Transform。
- [ ] 19.12 确认 `ThirdPersonGameplay` 程序集不引用 Character、BTSMTL、Networking、Presentation 或 Diagnostics。
- [ ] 19.13 确认 Character、BTSMTL 节点和网络层只通过正式端口、ChangeSet 或 semantic facts 接入 GE。
- [ ] 19.14 确认不存在第二份 Behavior identity interface、旧 namespace 转发类型或程序集循环依赖。
- [ ] 19.15 确认 Adapter、Mapper 和 Projector 不包含 Effect 类型 switch、属性公式、叠层、周期或预测协调算法。
- [ ] 19.16 更新 `openspec/project.md` 的 Gameplay Effect、Tag、Attribute、Effect、适配层、程序集和网络事实口径。
- [ ] 19.17 使用带 `--disable-build-servers /nr:false /p:UseSharedCompilation=false` 的正式 build 命令编译受影响工程。
- [ ] 19.18 build 结束后立即执行 `dotnet build-server shutdown`。
- [ ] 19.19 根据最终实现逐项同步本 change 的任务状态。
- [ ] 19.20 搜索并确认 Runtime 不再出现 `StateEffectSyncDomain`。
- [ ] 19.21 搜索并确认 Runtime 不再出现 `GameplayBehaviorKind.State`。
- [ ] 19.22 搜索并确认 Runtime 不再使用 `StateId` 或 `PayloadDigest` 表达 Gameplay Effect。

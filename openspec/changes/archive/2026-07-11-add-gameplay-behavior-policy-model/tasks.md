## 1. 现状对齐

- [x] 1.1 读取 `character-network-sync-domain-contract`、`character-action-network-policy-authoring`、`character-motion-semantics`、`character-gameplay-sync-adapter` 当前 spec，确认 Behavior model 不违反 SyncDomain 边界。
- [x] 1.2 读取 `CharacterPipelineDefinition`、`ActionProfile`、`ActionNetworkPolicyResolver`、`CharacterGameplaySyncAdapter`、`MotionContribution`、`ClientCommand` 当前代码，确认现有策略入口和硬编码点。
- [x] 1.3 列出当前已有 behavior-like identity：ActionId、WindowType、Motion source type、StateId、CueType、GameplayResultType、Pipeline Blackboard key。

## 2. 行为模型定义

- [x] 2.1 定义 `BehaviorKind`：Transaction、Stream、State、Event。
- [x] 2.2 定义 behavior identity 合同：BehaviorId、tags、display name、debug category。
- [x] 2.3 定义 behavior network policy 合同：authority、prediction、replication、correction、history、target SyncDomain。
- [x] 2.4 定义 kind 到 runtime/sync 的映射表。
- [x] 2.5 明确 ActionProfile 与 Transaction behavior 的关系，避免 ActionProfile 和 GameplayBehaviorProfile 双身份。

## 3. Authoring 数据入口

- [x] 3.1 在 `CharacterPipelineDefinition` 规划统一 behavior registry 入口。
- [x] 3.2 将现有 ActionProfiles 纳入统一 behavior registry 查询口径。
- [x] 3.3 规划 Stream/State/Event behavior profile 的最小字段。
- [x] 3.4 为 duplicate BehaviorId、缺失 BehaviorId、kind/domain 不匹配制定配置错误。
- [x] 3.5 保持 Graph 节点和 Timeline clip 只引用 BehaviorId、ActionProfile 或输出类型，不保存完整网络策略。

## 4. 策略解析

- [x] 4.1 设计 `BehaviorNetworkPolicyResolver` 或等价服务。
- [x] 4.2 将 Transaction behavior 解析委托或迁移自现有 `ActionNetworkPolicyResolver`。
- [x] 4.3 将 `ClientCommandFrame` 从硬编码策略迁移到 Stream behavior policy。
- [x] 4.4 将 StateEffect、correction ack 等硬编码策略迁移到对应 behavior policy。
- [x] 4.5 让 resolver 输出统一 effective policy：should send、SyncDomain、packet kind、policy id、reason、summary。

## 5. SyncFacts 和 Adapter

- [x] 5.1 规划 SyncFact 携带可选 BehaviorId 的方式。
- [x] 5.2 保持 `CharacterNetworkSendStage` 只收集 `SyncFacts`，不读取 Graph、Timeline 或 Blackboard。
- [x] 5.3 让 `CharacterGameplaySyncAdapter` 消费 behavior resolver 结果，不形成第二套策略。
- [x] 5.4 确保 Stream behavior 不创建 ActionInstance。
- [x] 5.5 确保 Transaction behavior 仍通过 ActionInstance 和 ActionSyncDomain 处理。

## 6. Inspector 和 Debug

- [x] 6.1 规划 `CharacterPipelineDefinition` Inspector 的 Behavior Registry 摘要。
- [x] 6.2 规划 behavior profile Inspector 的 kind-specific policy preview。
- [x] 6.3 规划 Runtime Debug 展示 BehaviorId、BehaviorKind、SyncFact、effective policy 和过滤原因。
- [x] 6.4 规划缺失行为策略、重复 BehaviorId、无效 kind/domain 的 authoring 报错。

## 7. 清理和文档

- [x] 7.1 清理或迁移硬编码 `Character.Motion.ClientCommandFrame` 等隐藏策略。
- [x] 7.2 更新现有 spec 中“ActionProfile 是动作策略中心”的措辞，明确它是 Transaction behavior 的专门入口。
- [x] 7.3 更新网络边界文档，说明 BehaviorId 不是 Graph 路径、不是 Blackboard key、不是 transport packet。
- [x] 7.4 确认没有恢复旧 ActionModule、ActionSO、locomotion 特化 SO/config 或节点级完整网络策略。
- [x] 7.5 将 `IActionNetworkPolicySource` 清理为 transaction-scoped source 命名，避免统一行为源和事务行为源混淆。

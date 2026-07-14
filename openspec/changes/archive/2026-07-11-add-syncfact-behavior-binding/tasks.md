## 1. 现状确认

- [x] 1.1 读取 `CharacterPipelineOutput`、`CharacterNetworkSendStage`、`CharacterGameplaySyncAdapter`，列出当前所有 outgoing fact 类型。
- [x] 1.2 确认哪些 fact 已可从 ActionId 或 ActionInstanceId 解析 Transaction BehaviorId。
- [x] 1.3 确认哪些非事务 fact 仍依赖 `ClientCommandBehavior`、`StateEffectBehavior`、`MotionCorrectionAckBehavior` 固定槽位。
- [x] 1.4 对照 `character-network-sync-domain-contract`、`character-gameplay-sync-adapter`、`gameplay-behavior-policy-model`，确认 BehaviorId 不替代 SyncDomain。

## 2. 数据模型

- [x] 2.1 定义 fact-level behavior binding 合同：`BehaviorId`、fact kind、可选 source type、目标 `BehaviorKind`。
- [x] 2.2 为 `ClientCommand` 增加正式 BehaviorId 来源。
- [x] 2.3 为 `GameplayStateEffectEvent` 增加正式 BehaviorId 来源。
- [x] 2.4 为非 action 来源的 `GameplayResultEvent` 增加正式 BehaviorId 来源。
- [x] 2.5 为 cue/event fact 增加正式 BehaviorId 来源或拆分通用 event fact。
- [x] 2.6 为 correction ack 增加正式 BehaviorId 来源或包装成带 BehaviorId 的 typed fact。

## 3. PipelineDefinition 配置

- [x] 3.1 新增 `SyncFactBehaviorBinding` 或等价通用配置表。
- [x] 3.2 将当前 `ClientCommandBehavior` 迁移到 binding 表。
- [x] 3.3 将当前 `StateEffectBehavior` 迁移到 binding 表或要求 StateEffect fact 显式 BehaviorId。
- [x] 3.4 将当前 `MotionCorrectionAckBehavior` 迁移到 binding 表。
- [x] 3.5 删除固定字段 `m_ClientCommandBehavior`、`m_StateEffectBehavior`、`m_MotionCorrectionAckBehavior`。
- [x] 3.6 在 `CollectConfigurationErrors` 中校验 binding 的 BehaviorId 必须存在于 registry，kind/domain 必须匹配 fact kind。

## 4. Adapter 解析

- [x] 4.1 让 Adapter 对每个 outgoing fact 单独解析 behavior policy，而不是按 fact type 预先解析一次固定槽位。
- [x] 4.2 Transaction fact 继续通过 `ITransactionBehaviorPolicySource` 查 ActionProfile。
- [x] 4.3 非事务 fact 通过 fact-level BehaviorId 查询 `GameplayBehaviorProfile`。
- [x] 4.4 缺失 BehaviorId、缺失 profile、kind/domain 不匹配时记录 Missing policy 并过滤。
- [x] 4.5 保持 Adapter 不读取 Graph、Timeline、Blackboard 或 Inspector-only 配置。

## 5. Authoring 和 Debug

- [x] 5.1 更新 `CharacterPipelineDefinition` Inspector，显示 Behavior registry 和 SyncFact binding 表。
- [x] 5.2 更新 Runtime Network Debug，显示每条 fact 的 BehaviorId、BehaviorKind、fact kind、policy id 和过滤原因。
- [x] 5.3 更新 BehaviorProfile Inspector preview，展示它可匹配哪些 fact kind。
- [x] 5.4 避免 UI 提供完整网络策略字段给 Graph node 或 Timeline clip。

## 6. 清理

- [x] 6.1 删除固定非事务行为槽位相关字段、属性和 inspector 展示。
- [x] 6.2 删除 Adapter 中按固定槽位解析非事务 policy 的代码路径。
- [x] 6.3 确认没有恢复 ActionModule、ActionSO、Ability 或 locomotion 特化 config。
- [x] 6.4 确认没有新增 hidden fallback 或手填字符串 fallback。

## 7. 规范和校验

- [x] 7.1 更新 OpenSpec，说明任意标记的对象是 SyncFact，不是 Graph node。
- [x] 7.2 更新 OpenSpec，说明固定槽位是过渡，完成后必须删除。
- [x] 7.3 运行 `openspec validate add-syncfact-behavior-binding --strict --no-interactive`。

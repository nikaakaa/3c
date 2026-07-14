# Tasks

## 1. 清理旧 Action 节点身份链路

- [x] 1.1 删除 `ActionModule`、`ActionIdentity`、`IActionIdentitySink` 源文件和 `.meta`。
- [x] 1.2 删除 `ActionSubTreeNode` 源文件和 `.meta`。
- [x] 1.3 如果存在 `ActionStateNode`，删除源文件和 `.meta`。
- [x] 1.4 从 `CharacterGraphContext` 移除 `IActionIdentitySink` 实现。
- [x] 1.5 从 `CharacterGraphContext` 移除 active action 缓存、tag 写入和输出同步逻辑。
- [x] 1.6 从 `StrictGameplayOutput` 移除 `ActionId`、`ActionDisplayName`、`ActionPhase`、`ActionTargetKey`、`ActionNetworkIdentity`、`ActionTags`。
- [x] 1.7 清理因删除 action 字段产生的 using 和编译引用。

## 2. 新建独立 Ability 模块

- [x] 2.1 新建 `Assets/GameScripts/Main/Runtime/Character/Ability` 目录。
- [x] 2.2 新增 `AbilityLifecycleState`，表达 requested、active、committed、cancelling、ended、rejected 等状态。
- [x] 2.3 新增 `AbilityPredictionPolicy`，表达 none、local predicted、server authoritative 等首期策略。
- [x] 2.4 新增 `AbilityActivationResult`，表达 success、missing spec、blocked、already active、invalid request 等结果。
- [x] 2.5 新增 `AbilityTargetSnapshot`，表达 target key 和可选目标位置快照。
- [x] 2.6 新增 `AbilityRequest`，表达 ability id、source、input sequence、simulation tick、target snapshot。
- [x] 2.7 新增 `AbilityAsset`，表达 ability id、显示名、tags、activation/block/cancel tags、target key、prediction policy 和 body graph 引用。
- [x] 2.8 新增 `AbilitySpec`，表达运行时授予记录和 enable 状态。
- [x] 2.9 新增 `AbilityActivation`，表达 activation id、spec id、ability id、prediction key、start tick、input sequence、target snapshot 和生命周期。
- [x] 2.10 新增只读 `AbilityContext`，暴露当前 active activation 和 ability metadata。
- [x] 2.11 新增 `IAbilityBody` 抽象，作为后续 BTSMTL/Graph body 接入点。

## 3. 实现 AbilityRuntime 生命周期

- [x] 3.1 新增 `AbilityRuntime` 类型，不依赖 `CharacterPipeline`。
- [x] 3.2 实现 grant ability，生成稳定 `AbilitySpec`。
- [x] 3.3 实现 remove ability，并处理 active spec 被移除时的结束语义。
- [x] 3.4 实现 `CanActivate`，检查 request、spec、enabled、active ability 和 block tags。
- [x] 3.5 实现 `TryActivate`，创建 `AbilityActivation`、prediction key 和 `AbilityContext`。
- [x] 3.6 实现 `Commit`，把 active activation 标记为 committed。
- [x] 3.7 实现 `Cancel`，按 activation id 取消 active ability。
- [x] 3.8 实现 `End`，按 activation id 结束 active ability 并清理 context。
- [x] 3.9 实现 ability tags、block tags、cancel tags 的精确字符串判定，不引入复杂 tag query。

## 4. 保持模块边界

- [x] 4.1 确认 `AbilityRuntime` 不引用 `CharacterPipeline`、`CharacterPipelineFrame`、`CharacterGraphContext`。
- [x] 4.2 确认 `AbilityAsset` 只引用执行体资产，不直接 tick Graph、Timeline 或 Motion。
- [x] 4.3 确认 Graph/BTSMTL 普通节点不再写 active action identity。
- [x] 4.4 确认 Timeline 和 Motion 现有链路不因 ability 模块新增绕行路径。

## 5. 验证

- [x] 5.1 使用 `rg` 确认正式 runtime 中不存在 `ActionModule`、`ActionSubTreeNode`、`ActionStateNode`、`IActionIdentitySink`、`ActionIdentity`。
- [x] 5.2 使用 `rg` 确认正式 runtime 中不存在 `StrictGameplayOutput.Action` 旧字段引用。
- [x] 5.3 使用 `rg` 确认 `AbilityRuntime` 没有引用 `CharacterPipeline`、`CharacterGraphContext`、`CharacterMotionStage`。
- [x] 5.4 运行 `openspec validate replace-action-module-with-ability-runtime --strict --no-interactive`。

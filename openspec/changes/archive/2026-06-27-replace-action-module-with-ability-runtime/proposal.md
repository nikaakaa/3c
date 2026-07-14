# Change: 用独立 Ability Runtime 替代节点 ActionModule 语义

## Why

当前 motion 语义已经收口到 `refactor-action-motion-semantics`，但早期 ActionModule 方案曾把第一阶段动作身份放在 `ActionSubTreeNode + ActionModule` 上。经过重新评估 UE/GAS 的边界后，这个方向会把 SubTree、State 和 Ability 激活事务混在一起：

- `ActionModule` 挂在节点上，会让图结构节点拥有 ability/action 身份。
- `CharacterGraphContext` 和 `StrictGameplayOutput` 被迫保存 active action identity，后续预测、取消、回滚、阻塞关系会继续向图节点扩散。
- UE/GAS 更值得借鉴的是 AbilitySystem/AbilityRuntime 独立拥有激活事务，图或蓝图只作为执行体，而不是普通图节点自己变成 Ability。

本变更要先准备一个独立的、轻量 GAS-inspired Ability 模块，并清理当前 `ActionModule` 语义。Pipeline 接入留到后续 change，通过薄 `CharacterAbilityStage` 适配，不在本变更里实现。

## What Changes

- 新增独立 `Character/Ability` 模块，表达 ability 作者配置、运行时授予、激活请求、激活事务、只读上下文和生命周期。
- 将 UE/GAS 的核心语义轻量映射为：
  - `AbilityRuntime`：角色侧 ability runtime，类似轻量 `AbilitySystemComponent`。
  - `AbilityAsset`：作者配置入口，持有 ability 身份、标签、激活规则和执行体引用。
  - `AbilitySpec`：运行时授予记录。
  - `AbilityRequest`：来自输入、AI、网络或调试的激活请求。
  - `AbilityActivation`：一次激活事务，持有 activation id、prediction key、tick、input sequence 和 target snapshot。
  - `AbilityContext`：Graph/BTSMTL 后续读取的只读 ability 上下文。
- 实现阶段清理 `ActionModule` 链路，不保留兼容 alias：
  - 删除 `ActionModule`、`ActionIdentity`、`IActionIdentitySink`。
  - 删除 `ActionSubTreeNode`、`ActionStateNode` 等节点 ability/action 身份入口。
  - 清理 `CharacterGraphContext.EnterAction/ExitAction` 和 `StrictGameplayOutput.Action*` 字段。
- 明确 Graph/BTSMTL 的职责是 ability body 执行，不拥有 ability 激活事务。
- 明确 Timeline 继续负责时间窗口，MotionStage 继续负责 Move 前运动修正和最终移动。

## Out of Scope

- 不在本变更接入 `CharacterPipeline`，不新增 `CharacterAbilityStage`。
- 不新增 Graph 读取 `AbilityContext` 的节点。
- 不实现完整 GAS：
  - 不实现 `GameplayEffect`、`AttributeSet`、`GameplayCue`。
  - 不实现复杂 `GameplayTagQuery`。
  - 不实现完整网络复制、服务端确认、rollback buffer。
  - 不实现完整 cooldown/cost/资源消耗系统。
  - 不实现 AbilityTask 式异步任务系统。
- 不新增自动化测试，除非后续明确要求。

## Impact

- 正式 runtime 中现有 `ActionModule` 语义会被破坏性删除。
- `refactor-action-motion-semantics` 只保留 motion 语义；Action/Ability 身份边界由本变更接管。
- 新模块不依赖 `CharacterPipeline`，后续 Pipeline change 只需要增加薄适配层。
- Ability 身份将从 Graph 节点迁移到 `AbilityAsset + AbilityActivation`，避免恢复旧 `ActionDefinition`/`ActionSO` 分裂路径。

## Spec Comparison

- 与 `btsmtl-graph-core` 一致：Graph 继续承载结构和执行上下文，不承担跨系统生命周期。
- 与 `btsmtl-sm-node-authoring` 一致：`StateNode` 和 `SubTreeNode` 继续表达状态/行为结构，不承载 ability 激活事务。
- 与 `btsmtl-runnable-timeline-node` 一致：Timeline 仍由 Graph 节点请求播放，但 Timeline 不替代 ability 激活决策。
- 与 `btsmtl-componentized-node-authoring` 存在边界澄清：NodeModule 仍可承载节点组合能力，但 ability 身份不是节点组合能力，不能再用 `ActionModule` 挂到图节点上。
- 与 `refactor-action-motion-semantics` 一致：motion 语义独立于 ability 激活事务，本变更只接管 Action/Ability 身份边界。

# Change: 将 Ability Runtime 收口为 ActionInstance 网络策略链路

## Why

`replace-action-module-with-ability-runtime` 删除了节点 `ActionModule` 污染，但新建的 `Character/Ability` 仍带有 UE/GAS 式 `AbilityAsset -> BodyGraph` 和 `IAbilityBody` 语义。这和本项目目标不完全适配。

当前项目的核心设计是：

```text
Graph/BTSMTL 是统一行为编排层
Timeline 是动作窗口和表现时间轴
MotionStage 是统一运动出口
NetworkStage 是预测、确认、校正和远端插值出口
```

因此不应该再让 Ability 拥有执行图，也不应该把一部分 Tree 标记为 Ability body。接网络真正需要的是：本地预测的动作启动、Timeline 窗口、Motion 结果、Combat 裁决和服务端确认能通过同一个运行时动作实例对应起来。

本变更要把当前 Ability 模块重构为 `ActionProfile + ActionInstance + NetworkPolicy` 链路，服务 `Network-aware Third Person Action Combat Demo` 的动作预测、窗口同步、校正和 combat rewind。

## What Changes

- 将 `Character/Ability` 语义重构为 `Character/Action` 或等价正式动作模块。
- 删除 `AbilityAsset.BodyGraph` 和 `IAbilityBody`，不再表达 Ability 拥有执行图。
- 新增 `ActionProfile` 作为动作身份和策略中心：
  - `ActionId`
  - 显示名和调试分类
  - tags、block tags、cancel tags
  - prediction policy、authority policy、replication policy、correction policy
  - window、motion、cue 的集中网络策略
- 新增 `ActionInstance` 作为一次被接受动作的运行时身份：
  - `ActionInstanceId`
  - `ActionId`
  - `PredictionKey`
  - `InputSequence`
  - `StartTick`
  - `TargetSnapshot`
  - phase/state
- 新增 `ActionRuntime` 或等价 runtime，负责创建、确认、拒绝、取消和结束 `ActionInstance`。
- 明确 Graph 通过正式节点或 context service 提交 `BeginTrackedAction` / `EndTrackedAction`，但 Graph 本身不被标记为 action/ability 类型。
- 明确 Timeline、Motion、Combat、Presentation 产出的网络相关事实必须带运行时归属，例如 `ActionInstanceId`、`InputSequence`、`SimulationTick` 或 `WindowId`。
- 明确同步策略集中在 `ActionProfile`/policy resolver，产出点只标事实类型，不分散配置完整网络策略。

## Out of Scope

- 不实现真实 Fantasy 网络通信。
- 不实现服务端 combat rewind 逻辑。
- 不实现完整预测缓冲、回滚或校正平滑。
- 不实现完整 Action/Combo 内容。
- 不恢复旧 `ActionSO`、旧 `LocomotionSO` 或旧 BBB 状态类路径。
- 不新增特殊 `AbilityTree`、`ActionTree`、`AbilityGraph` 或 `ActionGraph` 类型。
- 不新增自动化测试，除非后续明确要求。

## Impact

- 当前 `Character/Ability` 目录会被重命名或替换为动作实例事务语义。
- `AbilityAsset`、`AbilityRuntime`、`AbilityRequest`、`AbilityActivation`、`AbilityContext` 等命名会被清理为 `ActionProfile`、`ActionRuntime`、`ActionStartRequest`、`ActionInstance`、`ActionContext` 等等价命名。
- `AbilityAsset.BodyGraph` 和 `IAbilityBody` 会被删除，不保留兼容 alias。
- 后续 Graph 节点可以引用 `ActionProfile` 或 `ActionId`，但不会让 Graph/Tree 拥有 Action 身份。
- 后续 Timeline window、MotionContribution、PresentationCue、CombatEvent 会逐步扩展运行时归属字段，以便 NetworkStage 收集和调试。

## Spec Comparison

- 与 `btsmtl-graph-core` 一致：Graph 承载结构和执行上下文，不承担网络同步身份。
- 与 `btsmtl-sm-node-authoring` 一致：StateNode/SubTreeNode 继续表达状态和行为结构，不标记为 networked action 或 ability body。
- 与 `btsmtl-runnable-timeline-node` 一致：Timeline 由 Graph 驱动并产出时间窗口，Timeline 不决定动作能否启动。
- 与 `replace-action-module-with-ability-runtime` 冲突并替代：该 change 中 `AbilityAsset -> BodyGraph` 和 `IAbilityBody` 语义将被本变更删除；其中 activation id、prediction key、target snapshot、block/cancel 事务价值保留并重命名为 ActionInstance 链路。
- 与 `refactor-action-motion-semantics` 的 motion 语义一致：Motion 继续使用 `MotionContribution -> MotionIntent -> MotionModifier -> MotionResult`，动作实例只提供归属和网络策略，不直接移动角色。

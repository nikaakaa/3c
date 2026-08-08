# Change: 删除 Blackboard 网络策略元数据

## Why

当前逻辑层 Inspector 和 Graph Data Catalog 仍显示 `Authority` 与 `Sync Policy`，Agent Document、Semantic IR 和 Target Program catalog 也继续保存这些字段。这会让作者误以为每个 Blackboard 变量能够决定预测、权威、复制、纠正或网络输出，但当前正式运行链并不是这样工作的：

- `Authority` 没有 Target Runtime 消费者，只参与显示、导出和校验。
- `SyncPolicy` 只有 `InputDerived` 被 Float32/Fixed Target 用来筛选 `InputValueId` 绑定，`SyncFact` 只被 ActionWindow projection 校验读取。
- `ConfigVersion`、`ReplicatedCue`、`CorrectionOnly` 没有正式运行时消费者。
- ServerAuthoritative 复制只按 Network Model 的 `GameplayFactKind + ProducerId coverage` 决定，不读取 Blackboard declaration。

这些字段已经不是实现网络能力的配置，只是残留标签。继续保留会把逻辑层和网络层重新粘在一起，也会让 UI、Document 和编译产物表达一套运行时不兑现的语义。

第一张截图中的 `State.OnExit -> SubmitActionLifecycle operation -> ActionRuntime -> ActionState + ActionFact` 不属于本次清理。`Submit Window Cancel`、`Tree Interrupt`、`Tree Abort`、`Natural Complete` 表达动作结束原因，不包含 Packet、Endpoint 或 Network Model 选择，是动作业务闭环的一部分，必须保留。

## What Changes

- 删除 `PipelineBlackboardVariableAuthority`、`PipelineBlackboardVariableSyncPolicy`、对应序列化字段、属性、Mutation 参数、Validator 分支和 UI 字段。
- 将 Blackboard authoring 明确拆为基础声明、可选 `InputBinding` 和可选 `FactProjection` 三个正交合同，不再用一个网络策略枚举选择业务行为。
- `InputBinding` 只保存稳定 `InputValueId`。非空绑定就是唯一输入派生标记；Character/Spawn、值类型和 Input catalog 引用由正式 Validator 校验。
- `FactProjection` 只保存 projection kind 和所需业务载荷。ActionWindow 继续要求 Bool、Frame/Frame、WindowType、WindowId、Digest 与 Action Context provenance，不再要求 `SyncFact`。
- Graph Data Catalog 和 Inspector 删除 `Authority / Sync Policy`，分别展示基础 declaration、可选 Input Binding 和可选 Fact Projection。
- Timeline Decision 校验只比较 declaration identity、类型、scope、lifetime、输入绑定和事实投影，不再比较不存在的网络元数据。
- Semantic IR 不再发出 `Authority` 或 `SyncPolicy` catalog field；Float32/Fixed Target 只按非空且合法的 `InputValueId` 建立 input-to-state binding。
- 删除 `ProgramCatalogFieldId.SyncPolicy`，同步提升 Semantic IR、Float32/Fixed Program format、compiler 和 Target ABI 版本，旧 artifact 直接拒绝。
- Agent Document v3 Blackboard JSON 删除 `authority`、`syncPolicy` 和旧平铺策略形状，使用严格的可选 `inputBinding` 与 `factProjection` payload；旧 package 必须重新 checkout，不提供 reader、升级器或 fallback。
- Exporter、Package Mapper、Reconciler、Mutation、Validator、MCP bridge 和 BTSMTL Agent Authoring 技能同步使用同一 Blackboard schema。
- 通过唯一 Document v3 事务迁移四个正式 RootTree，重写为新序列化形状并清除旧字段，不直接修改 Unity YAML。
- 显式重新发布 Corin、TrainingEnemy 的 Character Semantic IR、Float32/Fixed Target Program，以及两个 AI Controller 的 AIIntentProgram；不得自动触发 Character Build。

## Replacement Semantics

| 旧标签 | 正式归属 |
|---|---|
| `InputDerived` | declaration 的可选 `InputBinding.InputValueId` |
| `SyncFact` | declaration 的可选 `FactProjection=ActionWindow` |
| `ConfigVersion` | 实际配置内容及 `SemanticHash / ProgramHash / LayoutHash` |
| `ClientPredicted` | Network Model Source、Schedule 与 History |
| `ServerAuthoritative` | Network Model Source、Pipeline 与 Solver composition |
| `CorrectionOnly` | model-neutral typed `SimulationIngress` |
| `ReplicatedCue` | Network Model 的 fact kind / producer coverage |
| `PresentationOnly` | Presentation owner 和正式 Presentation 输入合同 |
| `LocalOnly`、`None` | 无需替代；没有外部合同即保持本地 |

## Impact

- Affected specs:
  - `character-pipeline-blackboard`
  - `btsmtl-graph-data-catalog-authoring`
  - `btsmtl-gameplay-semantic-ir`
  - `btsmtl-compiled-simulation-program`
  - `btsmtl-agent-authoring-document-sync`
  - `agent-character-controller-synthesis`
  - `btsmtl-ai-controller-authoring`
  - `character-targeted-motion-warp-demo`
  - `character-action-authoring-closure`
  - `gameplay-network-model-boundary`
- Affected authoring/runtime modules:
  - `TreeDesigner/Scripts/ExposedProperty/ExposedProperty.cs`
  - Blackboard Graph Data Catalog、Timeline Decision validation 和 TreeClip Inspector
  - Character/AI authoring Validator、Exporter、Document models、Package Mapper、Reconciler、Mutation 与 handler
  - Character Semantic Frontend、Semantic IR codec、Program catalog runtime index
  - Float32/Fixed Program lowering、input binding layout 与 artifact codec
  - BTSMTL Agent Authoring skill 和 MCP bridge schema/report
- Affected formal assets:
  - `CorinPlayableRootTree.asset`
  - `TrainingEnemyCharacterRootTree.asset`
  - `CorinTrainingAIController.AIRootTree.asset`
  - `TrainingEnemyAIController.AIRootTree.asset`
- Affected generated products:
  - Corin 与 TrainingEnemy 的 `.csir`、Float32/Fixed Program artifact 和 Unity wrapper
  - CorinTrainingAIController 与 TrainingEnemyAIController 的 AIIntentProgram
- Network packet、Endpoint、Transport、ServerAuthoritative coverage 和 Action lifecycle 数据结构不新增兼容路径。

## Spec Reconciliation

- `character-pipeline-blackboard` 当前仍要求每个变量保存 authority/sync policy，并要求 ActionWindow 使用 `SyncFact`。这是本次需要删除的过期约束。
- `btsmtl-graph-data-catalog-authoring` 当前仍把 `SyncFact` 当作 ActionWindow projection 的准入条件。该 UI 约束必须改为只读取正式 projection payload。
- `agent-character-controller-synthesis` 当前把 Action target 输入写成 `InputDerived InputValueId`。它必须改为显式 Input Binding，不再依赖旧枚举。
- `character-targeted-motion-warp-demo` 当前把正式 ActionTarget 链描述为 InputDerived declaration。它必须改为同一 Character/Spawn declaration 的 Input Binding。
- `character-action-authoring-closure` 当前在 Debug 场景中仍使用 HitWindow SyncFact 术语。它必须改为实际运行合同 `ActionWindowFact`，不能让 SyncFact 作为第二种事实类型残留。
- `character-action-authoring-closure` 已要求 ActionProfile、Node 和 Blackboard 不成为网络配置来源；本变更与该方向一致，不修改动作生命周期。
- `gameplay-network-model-boundary` 已要求 Program 与 Character state 不使用 authority 总控枚举，并禁止 BTSMTL 保存 Network Model 配置。本变更补齐变量级残留字段。
- `server-authoritative-hybrid-sync-model` 已规定可靠输出只由 `GameplayFactKind + ProducerId coverage` 决定。本变更不改变 packet mapping；当前 ActionWindow 没有 mapping 时继续保留本地 fact。
- active changes 中没有其它 change 拥有 Blackboard 网络策略清理。本变更不得被实现为其它 change 的附带兼容层。

## User Verification

实施完成后，用户在 Unity Editor 内按以下方式验收，不运行 batchmode：

1. 分别打开 Corin、TrainingEnemy Character RootTree 和两个 AI RootTree，在 Graph Data Catalog/Details 中确认不再出现 `Authority`、`Sync Policy`、`InputDerived` 或 `SyncFact`；ActionTarget 只显示正式 Input Binding，动作窗口只显示 Fact Projection。
2. 对四个 root 执行 Document v3 checkout、dry-run、apply、validate，确认新 package 不包含 `authority/syncPolicy`，旧 package 会以未知字段或 schema revision 错误拒绝，并且 apply 后重新 checkout 为 `Clean`。
3. 使用正式显式入口重新构建 Corin、TrainingEnemy Character Program，并重新发布两个 AIIntentProgram；确认旧 Semantic IR/Program artifact 在版本门禁处被拒绝，不会被兼容读取。
4. 在 Local Float32 与 Local Fixed Gameplay Lab 中检查移动、Attack target、Dodge、Attack1..5、Combo/Recovery/Hit/IFrame 窗口，确认输入绑定和 ActionWindow fact 与清理前一致。
5. 在 ServerAuthoritative 调试视图中确认 packet 仍由 Model coverage 决定；未映射的 ActionWindow 只显示为本地 GameplayFact，不会因 Blackboard 字段删除而自动发送。
6. 触发自然结束、窗口取消、Tree Interrupt 和 Tree Abort，确认动作仍沿 `SubmitActionLifecycle -> ActionRuntime -> ActionState + ActionFact` 完成，不把这条业务链误删为网络残留。

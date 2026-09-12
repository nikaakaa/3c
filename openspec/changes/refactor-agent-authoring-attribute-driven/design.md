## Context

2026-09-12 交接：本设计与tasks中的v7证据属于已完成阶段。Skill payload、节点定义、字段访问、端口和引用生命周期的统一增量见 [unify-skill-authoring-data-model/design.md](../unify-skill-authoring-data-model/design.md) D1—D8；对应原2.x/3.x/4.1/5.1范围见其D9。当前文件继续拥有既有Agent适配/事务与非Skill领域决策，不复制新模型设计或将旧证据提升为新模型完成结论。

当前 Character Agent Document 已是 v7，外部入口只有 checkout、rebase、dry-run、apply、validate 五个生命周期工具。它可以表达完整 Skill 闭包和 Presentation 目标，但 Agent 内部同时存在正式作者类型和 `AgentPackage...`、手写 Capability、字段/端口校验、owner 推断及 Presentation 模型，造成同一语义有多个来源。

当前正式真相仍是 Unity authoring asset；Document 是 AI 工作副本，Program、Projection、Native Pose Program 和运行状态仍不进入 Document 事务。Skill Graph、Macro、Skill Timeline、Blackboard、Pose Graph 和 Presentation 继续共享一条 Document hash、Mutation、Undo、Rollback、Save 与 reverse export 链。

## Authoring source ledger

| 语义 | 唯一正式来源 | Agent 允许保留的形状 | 需要删除或下沉的重复实现 |
|---|---|---|---|
| BTSMTL Gameplay Graph | `BtsmtlGraphAuthoringCapabilities`、`BaseGraph`/`BaseNode` 作者入口及共享 `GraphAuthoringCapabilityCatalog` | v7 package 的 graph/node/edge/layout DTO | `BtsmtlGraphAuthoringCapabilities` 中直接返回 `AgentPackage...` 的转换和 Agent 专属字段判断 |
| Skill Graph、State、Condition、Macro | `BtsmtlSkillFlowGraph`、`BtsmtlSkillMacroGraph`、`BtsmtlSkillFlowNode`、正式 `BtsmtlSkillCapabilityCatalog`、节点 authoring rule/reference metadata 与正式编辑 Mutation | Skill 闭包的稳定 identity、kind、typed properties、logical ports、references、owner | Agent 侧手写字段/端口/类型/节点规则 |
| Skill Timeline、TreeClip、Clip Curve | `TimelineAsset`/`TimelineData`、正式 Timeline authoring contract、AnimationClip 注册曲线目录 | timeline/section/track/clip/curve package DTO | Agent mapper 中重复的 Timeline 能力推断和非正式 owner 推断 |
| Skill 局部 Blackboard | `BtsmtlSkillBlackboardDeclaration`、`BtsmtlSkillBlackboardReference` 及正式 Blackboard Mutation | graph 内 `blackboardDeclarations` | Agent 侧独立 Blackboard 语义和顶层黑板分片 |
| Pose Graph、PoseStateMachine、Animation Layer、Control Rig | `CharacterPoseNodeDefinitionModule`、`CharacterPoseGraphAuthoringCapabilities`、StateMachine authoring adapter 与 Pose Mutation | presentation graph/node/edge/state/layout package DTO | Presentation codec/reconciler 中重复的 node kind、field、port、role 和 owner 分支 |
| Slot/Group、Mask、Blend Policy、Animation Producer | Character Presentation formal profile、slot/group、mask、blend policy 与 source binding authoring contract | presentation target/reference DTO | Agent 侧 Pose/Animation 专属模型和第二套 Mutation 语义 |
| Document 外壳与事务 | `AgentAuthoringDocumentModels`、唯一 `AgentAuthoringDocumentTransactionService` | v7 manifest/sync/context/editable package | 任何节点级 MCP、领域级 apply、第二 Undo owner 或第二 transaction |

内部实现变化只有在稳定 kind、typed field、logical port、owner 和闭包关系都不变时才不会影响 v7；DTO 只负责包格式，不负责重新定义上表语义。

## Goals / Non-Goals

**Goals:**

- 让正式作者类型、字段、引用方法和正式 Mutation 写入方法成为 Agent 可见语义的唯一来源。
- 保留从零创建或完整修改 Skill 闭包的能力，不缩减为只修改 SkillDefinition 的高层命令。
- 保留完整 v7 JSON 目标、稳定 identity、owner、Graph/Macro/Timeline/TreeClip/Blackboard/Presentation 闭包和严格 hash 流程。
- 让 JSON codec、Capability、Port Shape、Exporter、Parser、Reconciler、Validator、Compiler 和原生 UI 消费同一份 metadata。
- 删除 Agent 侧重复的业务模型和能力定义，同时保持唯一 Mutation 和唯一事务。
- 让内部 C# 类型、文件组织和 Compiler operation 变化在 Agent 可见业务语义不变时不触发 schema 升级。

**Non-Goals:**

- 不新增 MCP 工具、局部节点工具、Pose 专用 apply 或第二套 Mutation/Transaction 链。
- 不把完整 Skill JSON 改成只能修改少量高层字段的命令集合。
- 不把 Agent JSON、Document package 或 metadata 变成 Unity 正式真相。
- 不允许运行时反射、任意 SerializedProperty 写入、C# 类型名或 Compiler operation 进入 Agent 合同。
- 不改变 Skill、Timeline、Pose、Control、Program、Session、网络或 Behavior Designer 的既有业务所有权。

## Decisions

### 1. 正式作者 metadata 是唯一来源

metadata 直接附着在正式作者类型、作者字段、正式引用关系和正式 Mutation 写入方法上，声明 Agent/UI/Document/Compiler共同需要的稳定 kind、typed field、logical port、owner、引用和连接规则。

metadata 可以在 Editor 编译期生成静态查找实现，但生成物不可编辑，不成为第二作者真相。这样仍然有高效查找和严格校验，但不再有一份人工维护的 Agent Capability 定义。

拒绝运行时反射的原因是：运行时反射会把 C# 实现结构误当作作者合同，难以稳定编译、Hash、回放和错误诊断；正式 metadata 才是可审查的作者语义。

### 2. 保留完整 Skill JSON 闭包

Agent JSON 继续表达完整目标：SkillDefinition、Entry Graph、嵌套 Graph/State/Condition、Macro、Skill Timeline、TreeClip、局部 Blackboard 和 typed provider 引用。任意创建的含义是“任意组合已声明 Capability 的合法拓扑”，不是任意写 C# 类型或 Unity 序列化字段。

JSON 仍只保存稳定业务 identity、kind、typed properties、logical ports、references、owner 和闭包关系；不保存第三方类型、SerializedProperty、运行时状态、编译 index、generated payload 或 runtime handle。

### 3. Skill 与 Presentation 维持分域，但共用同一事务

Skill Flow、Skill Timeline、Blackboard、Pose Graph、PoseStateMachine、Slot/Mask、Profile 与 Clip Curve 各自保留正式 owner 和领域 Mutation。Agent 不为 Pose 增加第二入口；同一次 Document apply 仍由唯一 Transaction Module 收集全部 owner，执行一份 Mutation Plan，并统一回滚。

### 4. Agent 内部模块只按阶段和 owner 解耦

保留五个 MCP Adapter 和 Job Scheduler。内部按以下职责收敛：

```text
MCP Adapter
  -> Document Lifecycle
  -> Package Closure / Hash
  -> Formal Metadata Projection
  -> Skill / Presentation / Control Target Mapping
  -> Reconciler
  -> typed Mutation Plan
  -> one Transaction
```

Package、Metadata、Target、Reconciler 和 Transaction 是实现 Module，不是新的 Agent 业务定义。Skill/Presentation Adapter 只负责调用正式作者 Mutation，不重新登记字段或端口。

### 5. 保持 v7，只有可见语义变化才升级

这次重构不因为删除 Agent 内部模型、重命名 C# 类型、拆文件或调整 Compiler 实现而升级 schema。只有 Agent 可见的 kind、typed field、logical port、owner 或闭包语义变化时才升级 Document schema；旧 schema 继续严格拒绝，不保留兼容 reader 或双写。

## Risks / Trade-offs

- [完整 Skill JSON 仍然复杂] → 这是任意创建完整技能的必要成本；通过唯一 metadata 和通用闭包校验集中复杂度，不再复制一份领域模型。
- [metadata 声明错误会同时影响UI、Document和Compiler] → metadata 采用稳定 identity，所有消费方只读同一投影，并由 strict parser、Validator和Compiler共同拒绝未声明字段。
- [正式作者类型增加metadata负担] → metadata 只声明 Agent/UI/Document/Compiler 必须共享的业务语义，不复制运行时字段或编译实现。
- [删除旧模型后迁移面较大] → 按领域和 owner 分步删除，保留 v7 JSON 业务形状，先切换唯一语义来源，再删除旧 Parser/Mapper/Capability 分支；不保留旧/新并行路径。
- [Pose与Skill同时修改的事务仍然复杂] → 继续由唯一 Document Transaction Module 拥有 Undo、Rollback、Save 和 reverse export，领域 Module 不拥有事务生命周期。

## Migration Plan

1. 盘点正式 Skill、Timeline、Blackboard、Pose 作者类型和现有 Agent 重复模型，输出删除/归属清单。
2. 为正式作者类型、字段、引用、端口和 Mutation 写入方法补齐统一 metadata，生成不可编辑的 Editor 静态查找实现。
3. 让 v7 Package Parser、Exporter、Capability、Port Shape、Reconciler、Validator、Compiler 和原生 UI 改用正式 metadata；保持完整 Skill JSON 形状。
4. 把 Skill Flow、Skill Timeline、Blackboard、Pose Graph、PoseStateMachine、Slot/Mask、Profile 和 Clip Curve 的目标映射收敛到正式 authoring Mutation Adapter。
5. 删除 Agent 侧重复 `AgentPackage...` 领域模型、手写节点/端口/字段表、重复 owner 推断和旧 Parser/Mapper 分支；不保留兼容路径。
6. 对精确 Corin Definition 重新 checkout、dry-run、apply、re-checkout 和 validate，核对完整 Skill/Presentation 闭包、identity、owner、hash、Undo 和失败回滚。

# Change: 新增统一 Linked Pose 作者工作区

## Why

`add-linked-pose-interface-runtime` 已经建立 Interface、Implementation、Group、selector、`LinkedPoseCall`、Projection fragment 与运行时选择合同，但当前人工编辑体验没有闭合：Linked Pose 资产 Inspector 是只读的，Profile 只显示现有映射，Navigator 只能打开已有对象，作者不能在正式 UI 中创建 Interface、创建完整 Implementation、维护 Group/selector、修复 required Entry、放置 Call 或预览不同候选。实际可写入口因此退化为 Document JSON/MCP，这不符合现有 `GraphAuthoringEditorShell + typed Presentation Mutation` 的项目方向。

本地 Lyra 与 GASP 资产已经证明 UE 的作者模型不是“运行时替换任意 Graph”，而是四个稳定部分：Animation Layer Interface 定义合同，Anim Blueprint 实现整套 Layer，宿主 Anim Blueprint 持有 Linked Anim Layer 调用，业务数据只选择兼容 AnimInstance class。Lyra 本地项目中 `ALI_ItemAnimLayers` 定义 `FullBody_*`、`LeftHandPose_OverrideState` 等入口，`ABP_ItemAnimLayersBase` 提供基线实现，Rifle、Pistol、Shotgun、Unarmed 资产提供具体实现，`FLyraAnimLayerSelectionSet` 按标签选择一个 Layer class。当前项目的运行时抽象已经对应这四部分，缺的是把它们组织成一条作者能理解、能完成工作的 UI 流程。

本 change 不新增第二套资产模型，也不扩大到任意热更。它把现有 Linked Pose 能力接入唯一 Animation/Pose Graph 工作区，让 Unity 人工编辑与 Document/MCP 成为同一类型化 Mutation、Validator 和资产事务的两个客户端，而不是把 Document 误当成人类作者的唯一修改入口。

## What Changes

- 在现有 `GraphAuthoringEditorShell` 中增加 Definition-scoped Linked Pose 作者区域，不创建独立 Workbench、第二 GraphView 或重型 Custom Inspector。
- Navigator 按 Group 组织 Interface、selector、Implementation、required Entry 与 root Call coverage；单击在同一 Details 中编辑，打开 Entry 时在同一 Graph Canvas 和 breadcrumb 中下钻。
- 为零数据 Profile 提供可执行空状态，按 `Interface -> Group/selector -> Implementation -> root Call` 的依赖顺序引导创建，不再只显示“没有 Linked Pose Groups”。
- 新增 Interface 合同作者命令：创建 Interface、增删改和排序 Entry/typed port、提交 revision，并在修改前展示受影响 Implementation、Call 与 selector。signature hash 继续派生且只读。
- 新增 Implementation 闭包命令：从 Group/Interface 创建或复制 Implementation，并在同一事务中为全部 required Entry 创建 Graph owner、Graph、`GraphInput` 与 `GraphOutput` 边界。系统不自动生成业务节点或隐式 fallback；Empty Implementation 使用单独的显式模板命令。
- 新增 Group 与 selector 作者页：核心 UI 只认识 selector capability，首个具体 presenter 编辑 Equipment Slot、Empty mapping 与精确 Equipment mapping；候选闭包保持派生只读，不在 Linked Pose 核心加入 Equipment switch。
- 完成 `LinkedPoseCall` Details：先从当前 Profile 选择 Group，再从该 Interface 选择 Entry，Interface 与 typed ports 由选择结果派生；不允许输入 identity 字符串。重绑前必须预检现有 edge，不能静默删线或按名称猜测。
- 增加依赖感知的删除与修复命令。被 root Call、selector mapping 或 Group 引用的对象不得直接删除；Details 必须列出可跳转依赖。确认可删除时，资产、Entry Graph 与 Profile 引用进入一个 Undo 事务。
- Bottom Dock 增加 Preview-only Linked Pose selection override、当前 Group/Implementation/generation 与 Call/Entry 诊断。override 只属于窗口会话，不修改资产、不改正式 Runtime selection；Projection Stale 时停止 Preview 并要求作者显式 Build。
- Toolbar 继续只通过现有 `Validate / Compile / Build` 正式入口执行重操作。创建、选择、Inspector focus、Undo、资源导入和 Preview target 变化不得自动 Build、Document Apply 或资产迁移。
- Standalone Profile、Interface、Implementation 与 selector Custom Inspector 收口为轻量只读摘要和 `Open in Animation Workspace` 入口；正式 Linked Pose 写入只在工作区 Details 和 Document Reconciler 中通过同一种 typed Mutation 完成。
- 纠正旧设计口径：Document v3 是 AI 的唯一目录包与 apply 生命周期，不是人工作者的唯一 UI。Unity UI 修改正式资产后，已有 Document 同步状态按现有规则成为 `TreeDirty`，不得自动覆写 Document 或自动 rebase。

## Impact

- Affected specs:
  - `character-linked-pose-authoring-workspace`（新增）
  - `graph-authoring-editor-shell`
  - `graph-authoring-domain-framework`
  - `character-animation-presentation-authoring`
  - `character-presentation-pose-graph`
  - `btsmtl-agent-authoring-document-sync`
- Affected code:
  - `CharacterPresentationPoseGraphEditor` 的 Navigator、Details、breadcrumb、Toolbar 与 Bottom Dock 装配。
  - Linked Pose Interface、Implementation、Group、selector 与 root Call 的 capability presenter 和 command state。
  - `CharacterPresentationMutation`、`CharacterPresentationMutationService` 与多 owner 资产事务。
  - Linked Pose Custom Inspector、Profile Inspector 与 Document 同步状态投影。
- Dependencies:
  - 依赖 `add-linked-pose-interface-runtime` 已有运行时合同、typed assets、Projection、selector 与 `LinkedPoseCall`；本 change 不复制或重写这些能力。
  - 继续沿用当前 `GraphAuthoringEditorShell`、Presentation Mutation、Validator、Character Build、Preview 与 Pose Watch 正式入口。
- Compatibility:
  - 删除只读 Inspector 作为主要 Linked Pose 工作流的旧路径，不保留编辑开关或并行 UI。
  - 不改变 runtime Projection ABI，不新增热更、按需加载、解释器或未知 Graph 注入。
  - 不新增 Anim Blueprint 继承/Layer override 语义；当前作者通过复制 Implementation 并编辑独立 Entry Graph 获得武器变体。

## Non-Goals

- 不迁移 Corin 的具体武器内容和 EquipmentId；本 change 只闭合正式作者工作流。
- 不实现任意 Graph 替换、运行时脚本更新、下载内容、活动 Session 热更或跨 Implementation 状态迁移。
- 不实现 UE Anim Blueprint 类继承、Layer override 或 Property Access 的完整等价物。
- 不新增第二套 Preview evaluator、临时 Projection、自动 Build 或默认 Implementation fallback。
- 不把 selector 固定为武器；首版只提供 Equipment presenter，未来状态、载具或形态 selector 通过同一 capability 扩展。

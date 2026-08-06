## Context

当前 Linked Pose 代码已经覆盖运行时与 AI authoring 的主要数据链，但人工工作区只做了“看见对象”和“打开对象”：

- `CharacterPresentationPoseGraphEditor` 的 Navigator 能列出 Interface、Implementation 与 Entry，却把点击操作转成打开 Unity 对象或 Entry Graph，没有同一页面内的 author command。
- `CharacterLinkedPoseInterfaceAssetEditor`、`CharacterLinkedPoseImplementationAssetEditor` 与 Equipment selector Inspector 明确只读。
- `CharacterAnimationPresentationProfileEditor` 只能显示 Group、selector、mapping 与候选闭包。
- typed Mutation 已覆盖 Implementation、Group、Equipment selector 与 mapping，但没有 Interface 合同 mutation，也没有供人工 UI 组合完整资产闭包的应用服务。
- `LinkedPoseCall` capability 已能投影动态端口，但 Details 还没有 Group/Entry 的上下文选择和依赖预检。

这导致底层能力存在，作者却必须通过 Document JSON/MCP 才能真正配置。问题属于 authoring 架构与 UI 工作流，不是补 Corin 资产可以解决的。

## 本地 UE 参考结论

本设计以本地项目为事实参考，不把 UE 名称直接复制成项目运行时类型：

| 本地 UE 证据 | 观察到的职责 | 本项目对应物 |
| --- | --- | --- |
| `D:\UE_Project\LyraStarterGame\Content\Characters\Heroes\Mannequin\Animations\LinkedLayers\ALI_ItemAnimLayers.uasset` | 定义固定 Layer 入口与输入 Pose | `CharacterLinkedPoseInterfaceAsset` |
| `...\LinkedLayers\ABP_ItemAnimLayersBase.uasset` | 实现完整 Layer 集合 | `CharacterLinkedPoseImplementationAsset` 与 required Entry Graph |
| `...\Locomotion\Rifle\ABP_RifleAnimLayers.uasset`、Pistol、Shotgun、Unarmed | 提供不同武器实现；本地资产存在继承关系 | 多个独立 Implementation；本 change 不增加继承 |
| `...\Animations\ABP_Mannequin_Base.uasset` | 持有 `AnimGraphNode_LinkedAnimLayer` 与 Interface 引用 | root `LinkedPoseCall` |
| `LyraCosmeticAnimationTypes.*` 与 `LyraWeaponInstance.*` | selector 按 RequiredTags 与 DefaultLayer 选择 AnimInstance class | selector binding 产生通用 selection frame |
| `D:\UE_Project\游戏动画示例\Content\Blueprints\SandboxCharacter_Mover_ABP.uasset` | 同时存在 `LinkedAnimLayer`、`LinkedAnimGraph` 与固定宿主 AnimGraph | 固定 root + 受限动态 Entry dispatch |

可直接借用的是职责分离和作者导航关系。不能照搬的是 UClass 热链接、Blueprint 继承与 UE 编译器内部对象模型；当前项目只选择已进入同一 Projection 的 Implementation fragment。

## Goals / Non-Goals

### Goals

- 作者只在一个 Definition-scoped Animation Workspace 中完成 Linked Pose 合同、实现、选择映射、root Call 与 Preview 工作。
- 所有人工操作都降低为正式 typed Presentation Mutation，并复用 Validator、Undo、dirty owner 与显式 Build。
- UI 使用业务名、对象选择器和依赖跳转，不要求作者理解 GUID、hash、revision、GraphId 或 runtime handle。
- 保持 Linked Pose 核心 selector-agnostic；Equipment 只是第一个作者 presenter。
- 允许 authoring 暂时 Invalid，但错误必须原位、可跳转、不可被 fallback 掩盖。

### Non-Goals

- 不把所有 Presentation 资产塞进一个大 SerializedObject Inspector。
- 不让 UI 直接写字段、YAML、generated Projection 或 runtime session。
- 不在选择或修改时自动 Compile/Build。
- 不自动修复 Interface 变更造成的业务连线，也不静默删除依赖。

## Decisions

### 1. 使用现有 GraphAuthoringEditorShell，不建立 Linked Pose 专用窗口

Definition-scoped Pose Graph 工作区继续拥有唯一 Toolbar、Navigator、Graph Canvas、Details、breadcrumb 与 Bottom Dock。Linked Pose 只增加 domain pages 和 presenter：

- 单击 Group、Interface、selector 或 Implementation：保持当前 Canvas 页面，更新同一 Details 和 Navigator selection。
- 双击 Implementation Entry 或执行 `Open Entry Graph`：在同一 Canvas 下钻到该 Entry Graph，breadcrumb 显示 `Profile / Group / Implementation / Entry`。
- 选择 root `LinkedPoseCall`：Details 反向显示所属 Group、Interface、Entry，并提供跳转到 Group 或 Entry 的命令。

业务取舍：独立资产编辑器实现更快，但作者必须在多个 Inspector 和窗口之间拼装关系，也会复制 selection、Undo 与状态显示。复用 Shell 的改动范围更大，却能保持项目已经确定的唯一工作区和唯一 GraphView。

### 2. Navigator 以 Group 为作者主线

Linked Pose Navigator 使用以下业务层级：

```text
Linked Pose
  <Group Display Name> [Ready | Invalid | Stale]
    Contract
      <Interface Display Name>
    Selection
      <Selector Kind / Business Owner>
    Implementations
      <Empty / Unarmed / Rifle / ...>
        <Required Entry>
    Host Calls
      <Required Entry> [Placed | Missing | Duplicate]
```

Interface 可被多个 Group 引用时仍只有一个真实资产；Navigator 只是引用投影。候选闭包、signature、revision 与 compiled handle 不成为树节点名称。

业务取舍：按资产类型平铺更接近 Project 窗口，却不能回答“这个武器组是否完整”。按 Group 聚合能把合同、候选、选择来源和宿主 Call 放在同一工作上下文，代价是共享 Interface 会在多个 Group 下出现引用项。

### 3. 零状态按依赖顺序引导，不提供一键魔法配置

当 Profile 没有 Linked Pose 数据时，Details 显示可执行步骤：

1. 创建或选择 Interface Contract。
2. 从 Interface 创建 Group，并选择 selector capability。
3. 创建 Empty/普通 Implementation。
4. 在 root graph 放置缺失 required Calls 并人工接线。
5. Validate，修复错误后显式 Build。

可以提供 `Create Missing Required Calls` 作为显式批量命令，但它只创建带正确 Group/Entry 和动态端口的 Call 节点，不猜测画布边、插入混合节点或替换现有分支。

业务取舍：一键生成完整武器动画方案看起来省事，却必须猜 root 接线、默认资源和空实现语义。分步工作流多几次操作，但每步都能解释输入、输出和失败原因，不产生隐式 fallback。

### 4. Interface 是高级合同页，但仍有正式人工写入口

Interface Details 提供 Entry 与 typed port 列表、添加、删除、重命名、排序和类型选择。稳定 identity 自动生成且默认隐藏；signature hash 与使用方放在只读 Diagnostics/References。

提交合同修改前，应用服务计算影响闭包并显示：

- 受影响的 Group。
- stale 或不再完整的 Implementation。
- root Call 与当前 edge。
- 需要重新 Build 的 Projection。

作者确认后一次提交 Interface revision 与 authoring mutation。系统允许产生明确 Invalid authoring 以便继续修复，但不得自动改 Implementation 图、按端口名重绑或删 root edge。

业务取舍：彻底只读 Interface 可降低误操作，却会再次迫使工程师离开正式工作流。可编辑合同页提高了能力，但通过高级入口、影响预检和明确 Invalid 状态控制风险。

### 5. 创建 Implementation 必须原子创建 required Entry Graph 闭包

`Create Implementation` 需要作者选择 Group/Interface 和业务显示名。系统生成稳定 Implementation identity，并为 Interface 的每个 required Entry 创建：

- 唯一 Graph owner 与 Graph identity。
- 与 Interface 完全一致的 `GraphInput` / `GraphOutput` 动态端口。
- 初始 layout。
- Implementation Entry binding。

普通 Implementation 不生成内部业务节点、不自动连接输入输出。`Create Empty Implementation` 是明确的领域模板命令，只为项目正式支持的 Empty contract 生成 passthrough 与 Empty Goals 逻辑。复制 Implementation 会复制全部 Entry authoring 与 layout，但生成新的正式 identity，并保持同一 Interface。

删除前必须列出 Group mapping、selector、Call 与其它引用；存在外部引用时拒绝删除并提供跳转。引用清空后，Implementation、Entry bindings、Graph owner 与 Graph assets 在一个 Undo 事务中删除。

### 6. selector 作者 UI 通过 capability 扩展

Linked Pose 工作区只依赖 `selector kind -> presenter + typed mutation lowering + validator` 合同，不读取 Equipment 类型。首版 Equipment presenter 提供：

- 类型受限的 Equipment Slot 选择。
- 必填 Empty Equipment Implementation。
- 从当前 Definition Equipment catalog 选择 Equipment，并映射到同 Interface Implementation。
- 重复、遗漏、跨 Interface 与未知 Equipment 的原位诊断。
- 派生 Candidate Closure 只读显示。

未来新增 Gameplay State、载具或形态 selector 时注册新的 capability/presenter；不修改 Linked Pose 核心页面，不增加中央 `SelectionSourceKind` switch。

### 7. LinkedPoseCall 只选择 Group 和 Entry

root Call Details 的人工字段只有：

- Group：从当前 Profile 的兼容 Group 列表选择。
- Entry：从所选 Group Interface 的 Entry 列表选择。

Interface、端口、signature 与 runtime dispatch identity 全部派生只读。改变 Group/Entry 时先检查现有 edges：所有端口 identity 与类型仍兼容才可原子重绑；否则拒绝 mutation，并逐条列出阻塞 edge。UI 不通过删除 edge 让命令“看起来成功”。Entry Graph context 中继续禁止嵌套 `LinkedPoseCall`。

### 8. Mutation application service 负责多 owner 原子闭包

现有 `CharacterPresentationMutationService` 是节点和 Profile mutation 的基础，但完整创建/删除会涉及 Profile、Interface、Implementation、Entry Graph owner 与 root Pose Graph。新增 domain application service 只负责：

1. 将 UI command 预检并降低为现有或新增的 typed Presentation Mutation。
2. 收集精确 serialized owners。
3. 注册一个 Undo group。
4. 按 owner 依赖顺序应用 mutation。
5. 运行同一 authoring Validator。
6. 任一失败回滚全部 owners。

Document Reconciler 继续生成同一种 mutation，并调用相同 handler/validator；它保留自己的 package hash、dry-run/apply 与目录发布生命周期。Presenter 不得使用 `SerializedObject`、`AssetDatabase.CreateAsset` 或直接数组写入绕开该服务。

### 9. Document 是 AI 协议，不是唯一人工作者表面

旧设计中“Document v3 保持唯一作者修改链”的准确含义改为：

- AI 只能通过 Document v3 五步生命周期修改。
- 人工 UI 只能通过工作区 typed mutation 修改。
- 两者共享 capability、mutation handler、validator、asset identity allocator 与最终 Unity authoring truth。
- Unity UI 修改后，已 checkout 文档按现有 revision/hash 规则成为 `TreeDirty`；系统不自动 export、rebase 或覆盖 AI 编辑。

因此唯一的是语义写链和正式 Unity 资产，不是只有一个输入媒介。

### 10. Preview override 只驱动正式已构建 Preview session

Bottom Dock 在 Preview target 有匹配 Projection revision 时，允许按 Group 从 compiled candidate catalog 选择一个 Implementation。override 进入 Preview session-local selection provider，并显示：

- 当前 Implementation 与 SelectionRevision/generation。
- 各 required Entry 的完成状态。
- root Call 的 contribution、discontinuity 与诊断。

它不修改 selector asset、Equipment committed state 或正式 Runtime session。Authoring 修改使 Projection Stale 后，Preview 立即停止，override 保留为 editor view-state 但不可执行，直到作者显式 Build。

业务取舍：直接预览未构建 authoring 需要第二套临时 compiler/runtime，容易与正式结果分裂。只预览正式 Projection 多一次 Build，但看到的就是最终运行链。

### 11. 重操作与状态必须可见且显式

Toolbar 保持现有 `Validate / Compile / Build / Live Debug`。Linked Pose 页面统一显示：

- `Dirty`：authoring 已改。
- `Invalid`：authoring validator 失败。
- `Stale`：Projection 不匹配。
- `Ready`：当前 Projection 可 Preview/Runtime。
- `Live`：只读匹配 runtime snapshot。

selection、打开页面、对象选择器、Undo/Redo、Inspector focus、AssetDatabase refresh 与 domain reload只刷新轻量状态，不触发 Build、Document Apply、asset migration 或 preview evaluator。

## Risks / Trade-offs

- Interface 可编辑会扩大影响面；通过高级入口、影响闭包和显式 Invalid 状态暴露成本，不用自动修复掩盖破坏。
- 多 owner Undo/rollback 比单资产 mutation 更复杂；这是创建完整 Implementation 闭包必须支付的成本，不能让作者手工拼半套资产。
- 当前没有 UE Anim Blueprint 继承；复制 Implementation 会产生独立资产，重复度更高，但避免在运行时和编译器尚无继承语义时生造覆盖规则。
- 只预览已构建 Projection 会增加一次显式 Build；它保证 Preview 与 Runtime 同源，不产生临时执行路径。
- Group-centric Navigator 会重复显示共享 Interface 引用；它换来每个动态替换点的完整业务上下文。

## Migration Plan

1. 对账并移除“Document 是人工唯一入口”和只读 Linked Pose Inspector 作为正式工作流的旧口径。
2. 补齐 Interface 与复合资产闭包所需 typed Mutation、owner collector、validator 与 command state。
3. 在现有 Graph Shell 接入 Linked Pose selection、Details pages、breadcrumb 与 Group-centric Navigator。
4. 接入 Interface、Group/selector、Implementation 与 Entry authoring 命令。
5. 完成 root Call 的上下文选择、端口重投影和依赖预检。
6. 接入 Preview override、stale gating 与 runtime diagnostics。
7. 将 standalone Inspector 收口为轻量摘要/打开入口，并删除旧只读浏览主路径和重复编辑表面。

迁移必须一次切换到正式工作区写入口，不保留“Profile Inspector 可改一部分、Workspace 改另一部分、Document 才能创建”的三条路径。

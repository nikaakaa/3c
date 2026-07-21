# Change: 禁止 Character Definition Inspector 在热路径执行产物重计算

## Why

`CharacterPipelineDefinitionEditor.OnInspectorGUI` 再次直接调用 `CharacterSimulationProgramBuildService.IsStale`。该方法会计算 ProgramId、完整 SourceRevision、加载 Program、构造 Presentation Contract、重算 ProjectionRevision 并检查目标产物，因此 Unity 每次 Layout/Repaint 都可能遍历整条角色 authoring 链。作者仅仅选中 Definition，就会出现 `Waiting for Unity's code to finish executing`，打断 Timeline、IK、Motion Warping 与角色配置的日常迭代。

当前 `character-pipeline-definition-authoring` 已禁止 selection、Repaint 与 foldout 切换运行完整 source revision，但只规定了“不能做什么”，没有规定精确 `Ready/Stale` 如何得到、缓存失效后显示什么。后续改动为了补回 `Stale` 展示，仍可能把 `IsStale` 塞回 Inspector 热路径。本 change 补全正向状态合同，并让代码中的轻量检查与精确检查拥有不同入口。

## What Changes

- 将发布产物的轻量 Header 检查与完整 stale 检查拆成两个语义明确的 Editor API。
- Definition Inspector 的 selection、`OnEnable`、Layout、Repaint 与 foldout 绘制只允许读取序列化引用、轻量 Header 或 Inspector 已缓存状态。
- 默认状态增加 `Unchecked`，表示产物已发布但尚未显式比较当前 authoring source；不得用未经检查的 `Ready` 掩盖该事实。
- 增加显式 `Refresh Status` 命令，只有该命令可以在 Inspector 中执行完整 `IsStale` 并产生 `Ready` 或 `Stale`。
- Definition 字段修改后状态立即进入 `Needs Compile`；Compile 成功后进入 `Ready`；重新选中或 Domain Reload 后回到由轻量 Header 得到的 `Missing`、`Invalid` 或 `Unchecked`。
- 保持 Build、Play 前发布检查和 Runtime 装载边界继续使用正式精确 stale 语义，不降低产物正确性。

## Scope

### In Scope

- CharacterPipelineDefinition Inspector 的产物状态读取、缓存与按钮行为。
- CharacterSimulationProgramBuildService 的轻量 Header 检查入口。
- `character-pipeline-definition-authoring` 的 Inspector 热路径和状态刷新合同。

### Out of Scope

- 修改 Character Compiler、SourceRevision、ProjectionRevision 或 Target Artifact 的算法。
- 后台线程计算 Unity Asset、持久化全局状态缓存或建立依赖反向索引。
- Timeline、Foot Analysis、IK、Motion Warping 的 authoring 或 runtime 行为。
- Runtime 对 stale、invalid 或 missing 产物的拒绝规则。

## Impact

- Affected specs:
  - `character-pipeline-definition-authoring`
- Affected code:
  - `CharacterPipelineDefinitionEditor`
  - `CharacterSimulationProgramBuildService`
- Breaking UI behavior:
  - 重新选中 Definition 后，已发布产物先显示 `Unchecked`，作者需要显式点击 `Refresh Status` 才得到精确 `Ready/Stale`。

## Current Spec Comparison

- 当前 `character-pipeline-definition-authoring` 已要求 selection、Repaint 和 foldout 切换不得运行 Compiler、完整 source revision、Program decode 或 producer topology projection；现有实现直接调用 `IsStale`，属于明确违规。本 change 不撤销该规则，而是补充可执行的状态机与 API 边界。
- 当前 spec 的默认状态只列出 `Missing`、`Invalid`、`Ready`，没有表达“发布存在但尚未比较当前源码”。本 change 增加 `Unchecked` 与 `Needs Compile`，避免为了自动给出 `Ready/Stale` 而恢复热路径重计算。
- `btsmtl-compiled-simulation-program` 与 `refactor-presentation-projection-target-boundary` 要求精确 stale 检测继续比较正式 identity、revision 与 target expectation。本 change 保留 `IsStale` 作为显式 Refresh、Build 与发布边界的唯一精确实现，只禁止在 GUI 重绘中调用。
- `refactor-timeline-animation-authoring-boundary` 要求 Definition/Profile 能表达 Projection Missing、Stale、Ready。本 change 仍提供这些精确状态，但把计算时机限定为显式 Refresh 或成功 Compile；Timeline 不承担该计算。

## Success Criteria

- 选中或反复重绘 CharacterPipelineDefinition Inspector 不调用 `IsStale`、SourceRevision 计算、Program decode 或 ProjectionRevision 重算。
- 产物 Header 缺失或无效时直接显示 `Missing/Invalid`；Header 有效但未检查源码时显示 `Unchecked`。
- 点击 `Refresh Status` 后显示精确 `Ready/Stale`，且计算只执行一次，不进入后续 Repaint。
- 修改 Definition 后显示 `Needs Compile`；Compile 成功后显示 `Ready`。
- Build、发布与 Runtime 的精确 stale 拒绝能力不变。

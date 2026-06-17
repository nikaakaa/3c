## Context
Runtime 数据必须先稳定，再接编辑器。Ref 项目可复用编辑器交互思路，但不能把 runtime runner 或副作用驱动带入正式 gameplay。编辑器层应读写本项目 definition，compiler 输出本项目 runtime tree。

## Goals
- 提供 Editor-only 行为图编辑 adapter。
- 提供 Editor-only timeline track/clip 编辑 adapter。
- 提供 authoring definition 到 runtime execution tree 的 compiler。
- 明确 Ref/wly970123 的 Editor UI 可以作为主要移植来源，但 runtime runner 和副作用驱动不得进入正式 gameplay。

## Non-Goals
- 不做完整商业级节点编辑器。
- 不接入 Taco runtime。
- 不新增 PlayableGraph、Animator Controller 或 MonoBehaviour runner 作为 gameplay runtime。

## Decisions

### Decision: Editor adapter 读写本项目定义
GraphView / Timeline UI MUST 读写本项目的 authoring definition，不得让 runtime 依赖 Taco `BaseTree`、`RunnableTree`、`TimelinePlayer` 或 editor view classes。

### Decision: Ref Editor UI 可移植
`Ref/wly970123` 的 GraphView / Timeline Editor UI MAY 被复制或移植到本项目 Editor-only assembly，包括 UXML、USS、图标、window/view class、node/port/edge view、timeline track/clip view、manipulator 和 inspector 交互。移植后的代码 MUST 改接本项目 authoring definition、serializer、compiler 和 diagnostics，MUST NOT 直接保存或运行 Taco runtime tree。

### Decision: Compiler 是唯一桥
Editor authoring graph MUST 通过 compiler 生成 runtime execution tree / timeline definitions。正式 gameplay MUST 消费 compiled runtime data，而不是 editor graph object。

### Decision: Ref importer Editor-only
如需复用 Ref 示例资产或格式，MUST 通过 `Assets/Editor/Character/RefImport` 下的 importer 转换为本项目 definition。Importer MUST NOT 进入 runtime asmdef。

### Decision: 资产版本必须明确
Behavior tree 和 timeline authoring asset MUST 有 schema version、stable id 或等价迁移标记，以支持后续 node/port/clip 字段演进。

## Suggested Folder Layout
```text
Assets/Editor/Character/
  Graph/
    Model/
    Views/
    Solver/
  Action/
    Timeline/
      Model/
      Views/
      Solver/
  RefImport/

Assets/Scripts/Character/
  Behavior/
    Config/
    Model/
    Solver/
```

Editor view classes MUST remain under `Assets/Editor`. Runtime config/model/compiler inputs MAY live under `Assets/Scripts` only if they do not reference Editor APIs.

## Compiler Stages
```text
ValidateAuthoringAsset
-> ValidateNodeIds
-> ValidatePorts
-> ValidateTreeShape
-> ValidateTimelineRefs
-> CompileBehaviorTree
-> CompileActionSelectionNodes
-> CompileTimelineDefinitions
-> EmitDiagnostics
```

## Ref Reuse Boundary
允许复制或移植到 Editor-only：
- GraphView / TreeDesigner window、node view、port view、edge view、blackboard / inspector view。
- Timeline editor window、track view、clip view、field view、frame ruler 和 resize / drag handles。
- UXML、USS、图标、菜单、搜索、拖拽、复制粘贴、框选、缩放和平移交互。

允许参考或改写：
- 节点拖拽、端口连线、复制粘贴、搜索菜单。
- Timeline track/clip 布局和 frame ruler。
- Stable GUID map 和 edge 起止端口概念。

禁止迁入 runtime 或正式 gameplay：
- `TreeRunner.Update`
- `RunnableTree` / `RunnableNode`
- `TimelinePlayer.FixedUpdate`
- PlayableGraph 作为正式动作 runtime
- 直接 Animator / Transform / Particle / Audio / Cinemachine 驱动
- Taco runtime node 类型、BaseTree 资产格式或 Ref scene object binding 作为正式 gameplay 输入

## Validation Matrix
```text
Editor:
- Authoring graph can save/load stable ids.
- Timeline clips can save/load frame ranges.

Compiler:
- Valid sample compiles.
- Invalid graph fails with diagnostics.
- Version missing fails.

Runtime boundary:
- No UnityEditor refs in runtime.
- No Ref runner refs in runtime.
- No PlayableGraph runtime path.
```

## Migration Plan
1. 新增 editor authoring asset 数据或使用已批准 definition。
2. 新增 compiler 和校验。
3. 从 `Ref/wly970123` 移植 GraphView UI 壳并改接本项目 definition。
4. 从 `Ref/wly970123` 移植 Timeline UI 壳并改接本项目 timeline definition。
5. 新增 Ref importer 边界。
6. 增加 editor-only 和 runtime boundary 测试。

## Risks / Trade-offs
- Risk: 过早做 UI 拖慢 runtime。
  - Mitigation: 只做最小 adapter 和 compiler，不做完整 UX。
- Risk: Ref runtime 泄漏进正式 runtime。
  - Mitigation: 静态边界测试检查 runtime asmdef / source 引用。

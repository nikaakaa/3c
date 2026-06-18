## Context
Runtime 数据必须先稳定，再接编辑器。Ref 项目可复用编辑器交互思路，但不能把 runtime runner 或副作用驱动带入正式 gameplay。编辑器层应读写本项目 definition，compiler 输出本项目 runtime tree。`CharacterFramePipeline` 仍是固定角色帧管线，不进入节点编辑器；旧 `CharacterFrameSubmitterGraph` / `CharacterFrameSubmitterChain` 已退役，不能作为 authoring graph、迁移基线或 editor 数据源。

## Goals
- 提供 Editor-only 行为图编辑 adapter。
- 提供 Editor-only timeline track/clip 编辑 adapter。
- 提供 authoring definition 到 runtime execution tree 的 compiler。
- 明确 Ref/wly970123 的 Editor UI 可以作为主要移植来源，但 runtime runner 和副作用驱动不得进入正式 gameplay。
- 明确本阶段只交付 Character Behavior / Committed Action Timeline 编辑地基，不把 Dodge 示例包装成通用技能编辑器证明。
- 明确 Locomotion 在 editor graph 中是 behavior leaf / source，运行时只提交 BehaviorSubmission 候选输出。

## Non-Goals
- 不做完整商业级节点编辑器。
- 不接入 Taco runtime。
- 不新增 PlayableGraph、Animator Controller 或 MonoBehaviour runner 作为 gameplay runtime。
- 不提供 `CharacterFramePipeline` phase 顺序编辑器。
- 不把旧 `CharacterFrameSubmitterGraph` / `CharacterFrameSubmitterChain` 可视化为正式 authoring graph。
- 不宣称通用 Skill Editor；在 Block / PerfectBlock、Attack / HitResolve 或等价交互型能力形成第二条金线前，只能称为角色行为和 committed action timeline 编辑器。

## Decisions

### Decision: Editor adapter 读写本项目定义
GraphView / Timeline UI MUST 读写本项目的 authoring definition，不得让 runtime 依赖 Taco `BaseTree`、`RunnableTree`、`TimelinePlayer` 或 editor view classes。

### Decision: 固定管线不进入编辑器
`CharacterFramePipeline` 的 RequestSubmission、FrameSubmission、Plan / BodyArbiter、OutputApplier、Snapshot / Events phase 顺序 MUST 继续由代码和架构合同持有。编辑器 MUST NOT 允许用户通过节点连线改写 pipeline phase 顺序、输出应用顺序、motion executor、animation presenter、input consume 或 blackboard write 的权威边界。

### Decision: 旧 SubmitterGraph/Chain 不作为 authoring graph 或基线
旧 `CharacterFrameSubmitterGraph` / `CharacterFrameSubmitterChain` 已退役。它们 MAY 只作为历史名词出现在删除验证或归档文档中，MUST NOT 被测试用作当前 behavior submission baseline，MUST NOT 被直接可视化为正式 `CharacterBehaviorGraphDefinition`，也 MUST NOT 成为新 editor graph 的数据源。

### Decision: Locomotion 是可编辑行为源但只提交
Locomotion MAY 在 `CharacterBehaviorGraphDefinition` 中作为 leaf / source 出现。编译后的 runtime leaf MUST 委托现有 Locomotion runtime，产出 movement facts、state frame、motion candidate、animation candidate、facing / gait / run latch candidate 和 diagnostics submission。Locomotion leaf MUST NOT 直接调用 motion executor、Animancer presenter、runtime blackboard writer 或 Unity scene object。

### Decision: Ref Editor UI 可移植
`Ref/wly970123` 的 GraphView / Timeline Editor UI MAY 被复制或移植到本项目 Editor-only assembly，包括 UXML、USS、图标、window/view class、node/port/edge view、timeline track/clip view、manipulator 和 inspector 交互。移植后的代码 MUST 改接本项目 authoring definition、serializer、compiler 和 diagnostics，MUST NOT 直接保存或运行 Taco runtime tree。

### Decision: 通用技能编辑器需要第二条交互金线
Dodge 只能证明自发型位移动作和 committed action timeline 能通过本编辑器链路表达。系统 MUST 在完成 Block / PerfectBlock、Attack / HitResolve 或等价交互型能力金线后，才允许把本工具升级描述为通用角色技能编辑器。交互型能力金线至少需要覆盖 incoming hit / contact fact、window fact、hit/defense resolve ownership、双方结果或反击请求、cue 和 rollback restore 边界。

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
- CharacterFramePipeline phase order is not editor-authored.
- Retired CharacterFrameSubmitterGraph/Chain is not converted into an authoring graph or baseline.
- Locomotion editor leaf emits submissions only.

Generality claim:
- Dodge sample does not qualify as generic skill editor proof.
- A second interactive golden line is required before using generic Skill Editor wording.
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

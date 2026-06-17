# Change: 增加 Character Behavior Editor Adapters

## Why
`Ref/wly970123` 中的 TreeDesigner、GraphView 和 Timeline 编辑器 UI 可以作为主要移植来源，包括窗口、节点视图、连线、端口、Timeline track/clip view、UXML、USS、图标和交互控件；但正式 runtime 不能依赖其 `TreeRunner`、`TimelinePlayer`、PlayableGraph 或直接 Unity 副作用。需要在 runtime behavior submission entry、命名边界和 compiler 目标稳定后，建立 Editor-only adapter，让节点编辑器读写本项目自己的 behavior tree / action timeline definition。

## What Changes
- 新增 Editor-only GraphView adapter，用于编辑 `CharacterBehaviorTreeDefinition` 或批准的 authoring definition。
- 新增 Editor-only Timeline adapter，用于编辑 `ActionTimelineDefinition` / track / clip 数据。
- 允许移植 `Ref/wly970123` 的 Editor UI 壳、UXML、USS、图标、view / manipulator 交互和布局代码，但必须通过 adapter 写入本项目 definition。
- 新增 compiler，将 editor authoring graph 编译为 runtime `CharacterBehaviorExecutionTree` 或等价 compiled model。
- 新增 Ref importer / adapter 边界测试，确保正式 runtime 不引用 Taco runtime runner、GraphView、Editor 类型、PlayableGraph 或 Unity scene object。
- 增加一个最小 Dodge selector + timeline editor sample，用于验证编辑器资产能编译到 runtime definition。

## Implementation Slices
1. **Authoring asset slice**：定义 editor asset schema、stable id、node/port/clip id 和 version。
2. **Compiler slice**：先实现无 UI 的 authoring -> runtime 编译和校验。
3. **Graph adapter slice**：接最小 GraphView，能编辑 root、parallel、leaf 和 action selector 引用。
4. **Timeline adapter slice**：接最小 timeline view，能编辑 frame、track、clip 和 payload。
5. **Ref UI port slice**：从 `Ref/wly970123` 移植 Editor-only UI 视图、资源和交互代码，并改接本项目 authoring definition。
6. **Ref adapter slice**：只在 Editor 目录转换 Ref 数据或示例格式，不引入 Ref runtime。
7. **Sample slice**：提供一个 Dodge selector + timeline sample，并用测试编译到 runtime definition。

## Acceptance Criteria
- Editor asset 能编译为 runtime behavior execution tree。
- Timeline editor 生成的 data 能编译为 runtime ActionTimelineDefinition。
- 移植的 Ref UI 必须位于 Editor-only assembly，并通过 adapter/serializer 读写本项目 definition。
- Runtime 源码和 asmdef 不引用 UnityEditor、GraphView、Taco runner、PlayableGraph 或 Ref runtime runner。
- Dodge 示例资产能表达 Directional / Backstep selector + timeline，并通过 compiler 测试。
- 非法图、循环、缺失 root、端口不兼容和版本缺失都能自动报错。

## Stop Conditions
- 如果需要让 runtime 直接依赖 GraphView、Taco BaseTree、TimelinePlayer 或 PlayableGraph，必须停止。
- 如果 editor adapter 要直接驱动正式 Animator、Transform、Particle、Audio 或 Camera runtime，必须停止。
- 如果没有 runtime behavior submission entry 和 compiled runtime definition 作为编译目标，必须停止等待前置变更。

## Non-Goals
- 不接入 Ref/wly970123 runtime runner。
- 不实现完整设计师工作流、资产浏览器、批量迁移器或复杂 UX。
- 不新增表现层 runtime。
- 不让编辑器节点类型进入 runtime asmdef。

## Dependencies
- MUST 在 `add-character-behavior-submission-entry` 后实施。
- MUST 在 `refactor-character-graph-naming-boundaries` 后实施。
- SHOULD 在 `add-committed-action-selection-nodes` 和 `migrate-dodge-to-behavior-timeline` 后实施。

## Impact
- Affected specs:
  - `character-behavior-editor-adapters`
  - related: `character-behavior-submission-tree`
  - related: `committed-action-node-selection`
  - related: `dodge-action`
- Affected code:
  - `Assets/Editor/Character/Graph/*`
  - `Assets/Editor/Character/Action/Timeline/*`
  - `Assets/Editor/Character/RefImport/*`
  - `Assets/Scripts/Character/Graph/Config|Model|Solver/*`
  - `Assets/Tests/Editor/Character/*`

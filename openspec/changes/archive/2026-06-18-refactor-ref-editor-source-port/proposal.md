# Change: 将行为/时间轴编辑器替换为 Ref 源码级移植

## Why
当前 Character Behavior Editor 与 Committed Action Timeline Editor 已经使用部分 Ref/Taco 外观和资源，但关键交互仍混入自研实现，导致 clip 选择、拖拽、伸缩、窗口布局和节点编辑体验反复偏离 Ref 行为。继续修补局部 bug 会长期消耗时间，并形成“Ref 外观 + 自研交互”的分裂路径。

## What Changes
- **BREAKING** 将当前半移植的 timeline / graph editor shell 替换为 Ref/Taco editor 的源码级移植版本，保留项目命名、Editor-only 边界和正式 adapter。
- Timeline Editor MUST 移植 Ref `TimelineFieldView`、`TimelineTrackView`、`TimelineClipView`、track handle、selection、locator、frame position map、`DragManipulator`、`DragLineManipulator`、move leader / apply move、resize、rectangle selection、pan、zoom、focus 和 context menu 的交互结构。
- Character Behavior Editor / Committed Branch mode MUST 移植 Ref TreeDesigner / GraphView 的节点 shell、固定 root、SearchWindow、port / edge、selection、node panel 和受保护节点能力，并通过项目 adapter 写回正式数据。
- 删除或替换当前手写的 card/list、root pointer mode 猜测、局部 delta 计算、临时 half-port helper 和重复编辑入口，不保留 fallback UI 或第二套 editor path。
- Ref runtime 仍然只作为源码来源和设计参考，正式 gameplay / compiler / rollback / frame pipeline MUST NOT 依赖 Taco `TimelinePlayer`、`PlayableGraph` runner、`BaseTree`、`RunnableTree`、`RunnableNode` 或 Ref asset。

## Impact
- Affected specs: `committed-action-timeline-editor`, `character-behavior-editor-adapters`, `committed-action-authoring-toolchain`
- Affected code: `Assets/Editor/Character/Action/Timeline/*`, `Assets/Editor/Character/Graph/*`, `Assets/Editor/Character/Action/Branch/*`, timeline / graph editor tests
- Runtime boundary: 不修改 `CharacterFramePipeline`、Locomotion / Action sibling runtime、motion executor、Animancer presenter、rollback snapshot 或正式 action evaluator 语义
- Tests: 新增/更新 EditMode 自动测试和静态边界测试，覆盖 Ref 源码级移植结构、正式 adapter 写回、selection/drag/resize 边界和 runtime 不引用 Ref runner

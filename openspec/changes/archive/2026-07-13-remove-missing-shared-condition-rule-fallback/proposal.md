# Change: 删除缺失 shared ConditionRuleGraph 的自动 inline 回退

## Why

StateMachine Transition 显式切换到 shared `ConditionRuleGraph` 后，如果该 asset 被删除或不能解析，当前 `StateMachineGraph.CheckInit()` 会调用 `EnsureConditionRuleGraphs()`，把该 edge 自动补成新的 inline 规则图。这样断裂的显式复用引用会被静默改写成另一份真数据，Transition 甚至可能因默认 Rule Result 而继续通过。

这与 BT edge 已有的“无效 shared 图不得创建 inline fallback”合同冲突，也违反项目的显式配置、单一数据来源和缺失即失败原则。

## What Changes

- 为 `BaseEdge` 的 ConditionRuleGraph 引用保存显式 ownership：Inline、Shared 或 Unspecified/invalid；新建合法 edge 直接写入 Inline。
- shared asset 缺失、类型错误或 inline/shared 双持有时，保留其配置错误状态，不清 shared 引用、不生成新 inline 图、不把 edge 当作无条件分支。
- `StateMachineGraph.CheckInit()` 只补齐新建/明确 Inline edge 所需的图数据，不再修复 Shared 或 invalid ownership。
- 编辑器对损坏 shared 引用显示错误，并只允许作者显式替换 shared asset 或执行 Use Inline；打开规则图不得隐式创建 inline。
- runtime 与 validator 对损坏引用 fail closed：该条件 edge 不得通过，且必须报告 edge、owner 和引用错误。
- 对现有资产执行一次显式 editor migration：可唯一识别的有效 inline/shared 引用写入 ownership；缺失或歧义引用报告错误，不创建数据替代物。
- 修改 `btsmtl-sm-node-authoring` 中 shared asset 删除后“自动回到 inline”的过时 requirement，使其与 BT edge 规则一致。

## Impact

- 影响 `BaseEdge`、`StateMachineGraph`、`BaseEdgeView`、NestedGraphValidation、StateMachine runtime 与 BT composite 条件运行时。
- 影响所有 Transition edge 与 Composite child edge 的 inline/shared ConditionRuleGraph 序列化。
- 影响 current `btsmtl-sm-node-authoring` spec；`btsmtl-bt-edge-condition-decorators` 已有的无 fallback 要求保持不变并成为统一验收基线。
- 不新增 runtime fallback、兼容 resolver、并行条件图或临时 shared asset。

## Current Spec Comparison

`btsmtl-bt-edge-condition-decorators` 已规定无效 shared 条件图必须由作者显式替换或切换 inline；而 `btsmtl-sm-node-authoring` 当前要求删除 shared asset 后自动清理并回到 inline。两者对同一 `BaseEdge` 数据模型给出相反结果。本 change 以显式 ownership、编辑器错误和 runtime fail-closed 统一二者。


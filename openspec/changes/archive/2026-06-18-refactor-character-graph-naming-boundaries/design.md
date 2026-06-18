## Context
命名债已经影响架构判断：旧 submitter graph/chain 已退役并由 `CharacterBehaviorSubmissionRunner` 收敛；`CharacterGraphDefinition` 当前只是固定分支合同；`CharacterExecutionNodeTree` 还不是生产入口；`ActionBranch` 也只是 Action module 内部单 timeline 包装。

如果不先收束这些名字，后续节点编辑器会难以判断自己应该编译到哪个 runtime 结构，测试也会继续把 chain、graph、tree 混作一类。

## Goals
- 让 authoring graph、runtime execution tree、behavior submission runner、action branch 的名称对应真实职责。
- 移除“旧 submitter graph/chain 是正式行为图或长期扩展点”的误导。
- 明确所有顶层行为都是 behavior，Action 只是 committed behavior 的具体领域实现。
- 保留迁移期兼容，但禁止旧名称作为新扩展入口。

## Non-Goals
- 不改变运行时手感。
- 不引入新节点语义。
- 不做 editor UI。

## Decisions

### Decision: Graph 只用于 authoring
`CharacterBehaviorGraphDefinition` MUST 表示编辑器资产、节点连线和可视化 authoring 结构。正式 gameplay runtime MUST 消费 `CharacterBehaviorExecutionTree`、compiled model 或 submission model，不直接消费 GraphView 对象。

### Decision: ExecutionTree 表示 runtime
正式 runtime 节点结构 MUST 命名为 `CharacterBehaviorExecutionTree`，表达它是确定性评估树，不是任意有环图。

### Decision: Submitter 组合不是 Graph
Submitter 组合退役由 `refactor-character-submitter-chain-boundary` 负责；本变更 MUST NOT 再引入新的 submitter graph 或 submitter chain 名称。

### Decision: Action 不是顶层行为二分
`CommittedActionBranch` 表示具有请求、生命周期、claim、interrupt 和 timeline 的 committed behavior 分支。它 MUST NOT 被文档或源码命名成“行为树之外的另一半”。

## Rename Procedure
1. 使用 `rg` 枚举 `CharacterGraphDefinition`、`CharacterExecutionNodeTree` 和 `ActionBranch` 使用点。
2. 对 C# symbol 使用安全 rename 工具或知识图谱辅助，不做裸字符串替换。
3. 每个 rename 小批次后运行对应 EditMode 测试或编译检查。
4. 更新 OpenSpec change、spec、docs 和测试类名。
5. 最后运行静态搜索，确认旧名称只剩 archive、历史说明或迁移 adapter。

## Compatibility Policy
- 如果外部测试或 prefab 仍引用旧类型，允许短期保留薄 adapter。
- Adapter MUST 标注迁移用途，并在 tasks 中记录删除条件。
- 新功能 MUST 使用新名称，不得继续基于旧名称扩展。

## Validation Matrix
```text
Naming:
- CharacterBehaviorGraphDefinition is authoring-only.
- CharacterBehaviorExecutionTree names runtime compiled tree.
- CommittedActionBranch replaces ActionBranch as formal action branch name.

Behavior:
- Pipeline phase order unchanged.
- Locomotion and Dodge tests unchanged.

Boundary:
- No new Unity side-effect path.
- No editor/runtime dependency leak.
```

## Migration Plan
1. 识别所有 Graph/Tree/SubmitterGraph/ActionBranch 命名使用点。
2. 分批 rename 或封装兼容 adapter。
3. 更新测试和 OpenSpec 引用。
4. 添加静态边界测试防止旧名称作为新扩展入口复活。

## Risks / Trade-offs
- Risk: 纯 rename 影响面大。
  - Mitigation: 使用知识图谱/IDE rename 或小批量改动，配套 detect changes 和定向测试。
- Risk: 与行为提交树入口重复改动。
  - Mitigation: 本变更只处理命名和职责声明，不新增 runner 语义。

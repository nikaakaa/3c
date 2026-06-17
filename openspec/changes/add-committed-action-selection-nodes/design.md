## Context
ActionTimeline 已经能表达 frame-based animation、motion、window fact 和 cue outcome。但 `CommittedActionNodeKind` 当前只有 Timeline，无法表达“同一个提交型动作根据上下文选择 Directional 或 Backstep timeline”。

本变更将 `CommittedActionBranch` 从单 timeline 包装升级为最小 committed action node tree。它仍然不是通用角色行为树，也不拥有副作用；它只是 committed action module 内部的选择层。

## Goals
- 给 CommittedActionBranch 增加最小 Selector / Condition / Timeline 节点。
- 让 CommittedActionBranchEvaluator 能确定性选择一个 timeline。
- 为 Dodge timeline 正式迁移提供内部节点能力。
- 保持 ActionTimeline evaluator 无状态、无副作用。

## Non-Goals
- 不做完整 BT decorator / abort / service。
- 不做跨 action 的大行为树。
- 不做 editor UI。
- 不迁移 Dodge 配置权威。

## Decisions

### Decision: Selector 顺序确定
Selector MUST 按 child 顺序评估，并选择第一个 condition 通过且可评估的 child。所有顺序 MUST 来自 runtime definition，不能依赖 dictionary 枚举或 Unity object 顺序。

### Decision: Condition 只读
Condition node MUST 只读取 pure input context，不写 blackboard、不改 action lifecycle、不消费 input。Condition 的结果只影响本次 CommittedActionBranch 评估。

### Decision: 未选中节点无输出
未被 selector 选择的 timeline MUST NOT 输出 motion、animation、fact 或 cue。这样可以防止多个 variant 同帧误提交。

### Decision: Timeline 仍是 leaf payload
ActionTimelineDefinition 继续是 TimelineNode 内部数据，Timeline evaluator 仍无持久 state。持久 active action、state time 和 restore 继续归 Action lifecycle 或批准的 Action runtime state。

## Suggested Data Shape
```text
CommittedActionNodeDefinition
- node id
- kind: Selector / Condition / Timeline
- child ids
- condition payload
- timeline payload

ActionConditionDefinition
- condition kind
- expected value / fact id / request field
- source step policy

CommittedActionBranchEvaluationContext
- source step
- current action state
- request fact
- movement intent facts
- runtime blackboard snapshot
- action lifecycle frame info

CommittedActionNodeEvaluationResult
- selected node id
- CommittedActionBranchOutcome
- diagnostics
```

## Evaluation Algorithm
```text
Evaluate(node):
  if node is Timeline:
    return ActionTimelineEvaluator.Evaluate(node.timeline)

  if node is Condition:
    return condition result only; no frame outcome

  if node is Selector:
    for child in stable order:
      if child condition passes or child is directly evaluable:
        result = Evaluate(child)
        if result has output:
          return result
    return empty outcome with diagnostics
```

实现时可以将 condition 作为 selector child 的 guard，也可以将 condition 与 timeline 组合成 guarded child；但 MUST 保证未选中 timeline 无输出。

## Validation Matrix
```text
Compatibility:
- Old root Timeline action still works.

Selection:
- First passing child wins.
- No child passing returns empty/no fallback.
- Unselected cue/fact does not appear.

Boundary:
- Condition reads pure context only.
- Selector does not accept/reject action requests.
```

## Migration Plan
1. 扩展 Action node 数据模型。
2. 增加 condition input / result 纯数据模型。
3. 扩展 CommittedActionBranchEvaluator 支持 selector。
4. 保持现有单 timeline action branch 测试通过。
5. 新增 selector/condition/timeline 组合测试。

## Risks / Trade-offs
- Risk: 选择节点变成第二套请求仲裁。
  - Mitigation: Condition 只在已 accepted action 内选择 timeline，不决定请求是否 accepted。
- Risk: Condition 读写黑板造成副作用。
  - Mitigation: Condition 只读 snapshot，facts 写入仍由 frame plan / output applier 决定。

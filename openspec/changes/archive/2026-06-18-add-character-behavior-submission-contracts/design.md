## Context
之前的大 proposal 把 submission model、runner、wrapper、composer 和默认入口替换放在一起，风险过高。本变更只建立最底层合同，不触碰生产链路。

## Goals
- 明确 behavior submission 是纯数据。
- 明确 RequestPass / OutputPass 的职责。
- 明确状态所有权。
- 用 fake runner 验证同帧收集和排序。
- 为后续 Dodge 金线和正式入口替换提供数据地基。

## Non-Goals
- 不接入 `CharacterFramePipeline`。
- 不包装现有 Locomotion / Action。
- 不让 behavior submission 成为 frame plan 或 arbiter 的替代品。

## Decisions

### Decision: Submission 必须类型化
第一版 MUST 避免单个万能 `BehaviorSubmission` 承载全部语义。可以使用一个聚合 set，但内部 payload MUST 至少区分：

```text
BehaviorRequestSubmission
BehaviorOutputSubmission
BehaviorCueSubmission
BehaviorDiagnosticSubmission
BehaviorStateWriteSubmission
```

### Decision: Consumer 边界必须显式
每类 submission MUST 声明允许 consumer，未被消费或被错误阶段消费都 MUST 产生测试可见的 diagnostic，不得静默吞掉。

```text
BehaviorRequestSubmission -> Request arbiter / Action request context
BehaviorOutputSubmission -> BehaviorSubmissionComposer / FramePlan input
BehaviorCueSubmission -> Cue queue / Presentation adapter future consumer
BehaviorDiagnosticSubmission -> Diagnostics only
BehaviorStateWriteSubmission -> 批准的 state owner 或 frame context writer
```

### Decision: Pass 边界写死
`RequestPass` 只允许提交 request candidate、facts snapshot requirement、diagnostics 或 request-context state write。`OutputPass` 只允许提交 motion candidate、animation candidate、body claim、input consume candidate、window fact candidate、cue request 和 diagnostics。

### Decision: 状态 owner 明确
状态所有权必须固定为：

```text
Behavior node private state -> Behavior runtime
Locomotion state -> Locomotion runtime
Action active action / state time -> ActionLifecycleRuntime
Animation playback state -> Animation presenter
Confirmed gameplay facts -> CharacterRuntimeBlackboard
Rollback restore state -> 各 runtime capture/restore 纯数据
Editor graph state -> Editor-only asset/view
```

### Decision: Fake runner 不代表生产入口
Fake runner 只用于测试合同，不得注册到 `CharacterRuntimeCore`、`CharacterFrameRuntimeHost` 或正式 prefab。

## Suggested File Layout
```text
Assets/Scripts/Character/Behavior/
  Model/
  Solver/
  Diagnostics/

Assets/Tests/Editor/Character/Behavior/
```

## Validation Matrix
```text
Typed payload:
- request/output/cue/diagnostic/state write 可分辨
- source node id / source step / pass 被保留
- consumer / owner 可查询

Pass boundary:
- RequestPass 无 motion/animation apply output
- OutputPass 不接受/reject action request

State ownership:
- 每类状态 owner 有测试或文档断言

Static boundary:
- 无 Unity runtime object
- 无 Editor/GraphView
- 无 blackboard writer / applier 调用
```

## Migration Plan
1. 建立 behavior model 目录。
2. 新增 pass enum 和 source id。
3. 新增 typed submission payload。
4. 新增 submission set。
5. 新增 fake runner 和 fake leaf evaluator。
6. 添加状态所有权测试和静态边界测试。

## Risks / Trade-offs
- Risk: 类型过多导致第一版繁琐。
  - Mitigation: 类型只表达边界，不实现复杂业务。
- Risk: 后续 composer 需要调整字段。
  - Mitigation: 本变更只稳定语义，字段可以在后续 entry proposal 中按实际映射微调。

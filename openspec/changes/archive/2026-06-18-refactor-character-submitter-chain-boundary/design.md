## Context
当前 frame pipeline 的重要隐式顺序是：

```text
GameplayDecision:
1. LocomotionFrameSubmitter 准备 LocomotionDecisionFrame / timeline facts
2. CommittedActionFrameSubmitter 基于 Locomotion facts 和 action catalog 解析 action request

BuildMotion:
1. LocomotionFrameSubmitter 构建 state decision / locomotion frame / state frame，写入 context
2. CommittedActionFrameSubmitter 使用 context 中的 Locomotion/State frame 构建 action output 和最终 frame submission
```

这不是行为树，也不是长期 chain 扩展点，而是正式 behavior submission runner 的上下文依赖。后续替换或扩展 behavior submission 前必须保持这个 pass contract，或显式改写并测试。

## Goals
- 删除旧 submitter graph/chain 正式入口。
- 固定现有 context dependency。
- 保证退役旧迁移层不改变行为。
- 为后续 entry replacement 提供清晰 submission runner 基线。

## Non-Goals
- 不做 behavior tree。
- 不做 timeline 迁移。
- 不做 editor。

## Decisions

### Decision: Graph/Chain 都退役
旧 `CharacterFrameSubmitterGraph` 与 `CharacterFrameSubmitterChain` 都不得作为正式 runtime 类型或扩展入口保留。正式入口 MUST 是 `CharacterBehaviorSubmissionRunner` 或批准的等价 behavior submission runner。

### Decision: Context Dependency 显式测试
Locomotion 先填 context、Committed Action 后消费 context 的顺序 MUST 有测试覆盖。后续 behavior entry 替换必须保持该顺序或显式改写为 pass contract。

### Decision: 本变更不改变语义
本变更只做命名、职责和测试收束，不改变 request resolution、lifecycle、motion resolve 或 output apply 行为。

## Validation Matrix
```text
Naming:
- Submitter submission runner no longer uses Graph or Chain name.

Order:
- Locomotion runs before Action in request stage.
- Locomotion writes state/locomotion frame before Action builds final output.

Behavior:
- Existing Dodge tests pass.
- Existing Locomotion frame tests pass.
```

## Migration Plan
1. 枚举 `CharacterFrameSubmitterGraph` / `CharacterFrameSubmitterChain` 使用点。
2. 删除旧 graph/chain 类型和正式引用。
3. 更新 `CharacterRuntimeCore` 默认 submission runner 创建逻辑。
4. 增加 context dependency 测试。
5. 增加旧 Graph/Chain 名称不作为正式入口的静态测试。

## Risks / Trade-offs
- Risk: 删除旧迁移层影响测试和序列化引用。
  - Mitigation: 分小步删除并运行定向测试。
- Risk: 忽略 Locomotion -> Action context dependency。
  - Mitigation: 本变更必须先补测试，再允许后续 behavior entry 替换。

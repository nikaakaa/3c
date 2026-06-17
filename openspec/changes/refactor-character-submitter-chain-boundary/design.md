## Context
当前 frame pipeline 的重要隐式顺序是：

```text
GameplayDecision:
1. LocomotionFrameSubmitter 准备 LocomotionDecisionFrame / timeline facts
2. FullBodyActionFrameSubmitter 基于 Locomotion facts 和 action catalog 解析 action request

BuildMotion:
1. LocomotionFrameSubmitter 构建 state decision / locomotion frame / state frame，写入 context
2. FullBodyActionFrameSubmitter 使用 context 中的 Locomotion/State frame 构建 action output 和最终 frame submission
```

这不是行为树，而是现有 submitter chain 的上下文依赖。迁移到 behavior submission 前必须把这个事实写死。

## Goals
- 修正 submitter graph 命名。
- 固定现有 context dependency。
- 保证 rename 不改变行为。
- 为后续 entry replacement 提供清晰旧链路基线。

## Non-Goals
- 不做 behavior tree。
- 不做 timeline 迁移。
- 不做 editor。

## Decisions

### Decision: 名称定死为 Chain
若类型仍保留，其正式名称 MUST 是 `CharacterFrameSubmitterChain` 或批准的等价 chain/composite 名称，不能继续叫 Graph。

### Decision: Context Dependency 显式测试
Locomotion 先填 context、Action 后消费 context 的顺序 MUST 有测试覆盖。后续 behavior entry 替换必须保持该顺序或显式改写为 pass contract。

### Decision: 本变更不改变语义
本变更只做命名、职责和测试收束，不改变 request resolution、lifecycle、motion resolve 或 output apply 行为。

## Validation Matrix
```text
Naming:
- Submitter chain no longer uses Graph name.

Order:
- Locomotion runs before Action in request stage.
- Locomotion writes state/locomotion frame before Action builds final output.

Behavior:
- Existing Dodge tests pass.
- Existing Locomotion frame tests pass.
```

## Migration Plan
1. 枚举 `CharacterFrameSubmitterGraph` 使用点。
2. 重命名为 `CharacterFrameSubmitterChain` 或等价名称。
3. 更新 `CharacterRuntimeCore` 默认 host 创建逻辑。
4. 增加 context dependency 测试。
5. 增加旧 Graph 名称不作为正式入口的静态测试。

## Risks / Trade-offs
- Risk: Rename 影响测试和序列化引用。
  - Mitigation: 分小步 rename 并运行定向测试。
- Risk: 忽略 Locomotion -> Action context dependency。
  - Mitigation: 本变更必须先补测试，再允许后续 behavior entry 替换。

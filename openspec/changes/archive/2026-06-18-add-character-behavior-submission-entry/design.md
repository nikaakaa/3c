## Context
前置变更已经完成三件事：submission 合同稳定、submitter chain 的现有顺序依赖被测试固定、Dodge 旧路径可以映射为 behavior submission。现在可以把 production entry 换到 behavior submission runner，但不能改业务语义。

## Goals
- 用 behavior submission entry 替换默认 submitter chain。
- 保持 Locomotion -> Action context dependency。
- 保持 pipeline phase 和 output applier 权威。
- 保持 Dodge golden line 等价。

## Non-Goals
- 不拆 Locomotion 状态机。
- 不迁 Dodge 到 timeline。
- 不做 editor UI。

## Required Pass Flow
```text
RequestPass:
1. Locomotion leaf prepares movement facts / timeline facts / decision context.
2. Committed Action leaf consumes Locomotion context and resolves accepted action request.
3. Request submissions are written into CharacterFrameContext or equivalent pure frame context.

OutputPass:
1. Locomotion leaf builds state frame / locomotion candidate.
2. Committed Action leaf consumes state frame / locomotion candidate context.
3. Action lifecycle tick and CommittedActionBranch/Timeline outcome produce action output submission.
4. Composer maps submissions into CharacterFrameSubmission / plan input.
5. BodyArbiter creates CharacterFramePlan.
6. OutputApplier applies selected output.
```

## Production Definition Source
生产 runner MUST 消费明确的 runtime definition / config。第一版 definition 可以很小，但必须是正式配置：

```text
Root
-> FixedOrderedParallel
   -> LocomotionLeaf
   -> CommittedActionLeaf
```

如果缺失 root、leaf 或顺序，系统 MUST 报配置错误或拒绝创建 runtime host，MUST NOT 生成隐藏 fallback tree。

## Wrapper Responsibilities

### Locomotion Leaf Wrapper
- Owns no new Locomotion state.
- Delegates to existing Locomotion runtime.
- Must output typed submissions.
- Must not apply motion, write blackboard or decide Action request acceptance.

### Committed Action Leaf Wrapper
- Owns no new Action lifecycle state beyond existing `ActionLifecycleRuntime`.
- Delegates request resolution and lifecycle to existing Action runtime.
- Must output typed submissions.
- Must not modify Locomotion private state.

### Submission Composer
- Converts typed behavior submissions to current frame submission / plan input.
- Must use existing BodyArbiter / CharacterFramePlan.
- Must not execute selected output.
- Must report unsupported or unconsumed submission as diagnostics; must not silently drop required output.

## Old Path Retirement
- `CharacterFrameSubmitterGraph` / `CharacterFrameSubmitterChain` MUST NOT remain as migration adapter, production entry, or test baseline.
- `CharacterRuntimeCore` production entry MUST use behavior submission entry / runner.
- Tests MAY mention old names only to assert that they are absent from current runtime boundaries.

## Validation Matrix
```text
Entry:
- RuntimeCore default host uses behavior submission entry.

Order:
- RequestPass Locomotion before Action.
- OutputPass Locomotion before Action.

Output:
- Dodge Directional equals golden line.
- Dodge Backstep equals golden line.
- Locomotion basic output equals previous chain.
- Missing behavior entry definition fails explicitly.

Boundary:
- No second arbiter.
- No direct side effects from wrappers.
- Restore state pure data only.
```

## Risks / Trade-offs
- Risk: Wrapper mirrors old chain too closely.
  - Mitigation: This is acceptable for entry migration, but old chain must stop being default; later proposals can split internals.
- Risk: Context dependency hidden again.
  - Mitigation: Required pass flow and tests must assert ordering.

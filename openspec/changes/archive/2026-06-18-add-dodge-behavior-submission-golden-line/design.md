## Context
Dodge 当前是最好的业务压力源：它有 request、variant、body claim、motion、animation、input consume、Run latch、completion、animation-end 等待和 retry 行为。金线先证明 behavior submission 合同能表达 Dodge，再允许正式入口替换。

## Goals
- 建立旧路径与 behavior submission 的等价对比。
- 发现 BehaviorSubmission 是否遗漏字段或边界。
- 不改变生产默认入口。
- 给后续 entry replacement 和 Dodge timeline migration 提供回归基线。

## Non-Goals
- 不把旧 Dodge variant 迁到 timeline。
- 不加 selector / condition。
- 不做 editor sample。

## Golden Line Scope
必须比较：

```text
Request:
- request accepted / rejected
- source input kind
- input consume candidate
- interrupt priority / resistance 结果

Claim:
- body domain
- body/channel claim

Motion:
- action state
- source state
- variant
- duration
- distance
- rotateToDirection
- locked world direction

Animation:
- animation key
- playback intent identity

Facts / Windows:
- current timeline facts consumed
- action branch outcome window facts if present

Run latch:
- Directional completion with move intent writes run latch candidate
- Backstep does not

Lifecycle:
- start frame
- continued frame
- completed frame
- animation-end waiting

Restore:
- restore state resumes same action timing
```

## Mapping Rules
- Accepted request maps to request submission.
- Action lifecycle frame maps to output submission.
- ActionBranchOutcome maps to output/cue/window fact submissions.
- Input consume remains candidate until final frame output applies it.
- Confirmed runtime facts remain outside behavior submission until frame plan adoption.

## Comparison Rules
- Golden comparison MUST use one shared assertion helper or equivalent schema.
- Exact identity fields such as action id、variant、animation key、claim、source step MUST match exactly.
- Numeric gameplay fields such as duration frame、distance、direction、rotation policy MUST use the same tolerance policy as existing Dodge tests.
- Missing mapped fields MUST fail the test with a field-level diagnostic.

## Failure Semantics
- 如果旧路径 accepted，但 submission 无法表达必要字段，测试失败并提示合同缺口。
- 如果旧路径 rejected，submission MUST NOT include output payload。
- 如果旧路径与 submission 输出不一致，不能通过 fallback 修复，必须定位字段或 owner 错误。

## Validation Matrix
```text
Directional:
- accepted
- motion equals
- animation equals
- run latch candidate equals

Backstep:
- accepted
- motion equals
- animation equals
- no run latch candidate

Rejected:
- no consume
- no output submission

Restore:
- restored frame output equals baseline
```

## Migration Plan
1. 建立 baseline capture helper。
2. 建立 behavior submission mapping helper。
3. 建立 shared comparison helper。
4. 写 Directional golden test。
5. 写 Backstep golden test。
6. 写 rejected / restore / retry golden tests。
7. 增加静态边界测试，确认 helper 不进入正式 runtime entry。

## Risks / Trade-offs
- Risk: 测试 fixture 过度依赖当前实现细节。
  - Mitigation: 只比较公开 frame/lifecycle/submission 输出，不断言私有方法内部步骤。
- Risk: Golden mapping 变成第二生产路径。
  - Mitigation: 将 mapping 限定在测试或明确 adapter，不注册 runtime host。

# Change: 接入 Character Behavior Submission 入口

## Why
只有在 typed submission 合同、submitter chain 边界和 Dodge golden line 都通过后，才可以替换默认 frame submitter 入口。本变更负责把统一 behavior submission entry 接到 `CharacterFramePipeline` 下，但仍保持 pipeline phase、BodyArbiter、FramePlan 和 OutputApplier 的权威不变。

## What Changes
- 新增 production `CharacterBehaviorSubmissionRunner` 或等价最小 runner，第一版只支持固定 root / parallel / leaf。
- 新增 Locomotion behavior leaf wrapper，保持现有 Locomotion 内部逻辑不变。
- 新增 Committed Action behavior leaf wrapper，保持现有 Action request/lifecycle/timeline 逻辑不变。
- 新增 `CharacterBehaviorSubmissionComposer`，将 typed submissions 转为现有 `CharacterFrameSubmission` / `CharacterFramePlan` 输入。
- 将默认 `CharacterRuntimeCore` host 从 submitter chain 替换为 behavior submission entry。
- 增加端到端金线测试：Dodge 输入 -> behavior submissions -> frame plan -> motion/animation/facts/cue candidates -> restore 一致。

## Implementation Slices
1. **Runner slice**：生产 runner 只支持固定 root/parallel/leaf，不做 selector/condition。
2. **Locomotion wrapper slice**：保持 Locomotion 先填 context 的顺序。
3. **Action wrapper slice**：保持 Action 后消费 context 的顺序。
4. **Composer slice**：复用现有 BodyArbiter / CharacterFramePlan。
5. **Default entry slice**：替换 runtime host 默认入口，并保留旧 chain 删除或迁移说明。
6. **E2E slice**：用 Dodge 金线证明行为等价。

## Acceptance Criteria
- 默认角色 runtime 通过 behavior submission entry 进入 frame pipeline。
- Locomotion 与 Action 仍保持 request/output pass 顺序依赖。
- Behavior entry 输出进入现有 BodyArbiter / CharacterFramePlan，不新增第二套仲裁。
- Directional / Backstep Dodge 端到端输出与 golden line 保持一致。
- 旧 submitter chain 不再作为默认正式入口；若保留，必须标注为迁移 adapter 并有删除条件。

## Stop Conditions
- 如果替换默认入口导致 Locomotion context dependency 无法保持，必须停止并回到 chain boundary / contract proposal。
- 如果 composer 需要新增第二套 arbiter，必须停止。
- 如果 Dodge golden line 失败，必须停止，不得继续替换入口。
- 如果需要实现 Action selector / Dodge timeline migration 才能完成本变更，必须停止，保持本变更只包装旧逻辑。

## Non-Goals
- 不新增 Action selector / condition。
- 不迁移 Dodge timeline 权威。
- 不做 editor UI。
- 不实现 UpperBody / Presentation runtime。

## Dependencies
- MUST 在 `add-character-behavior-submission-contracts` 后实施。
- MUST 在 `refactor-character-submitter-chain-boundary` 后实施。
- MUST 在 `add-dodge-behavior-submission-golden-line` 后实施。
- SHOULD 先于 `add-committed-action-selection-nodes` 实施。

## Impact
- Affected specs:
  - `character-behavior-submission-entry`
  - related: `character-behavior-submission-contracts`
  - related: `character-submitter-chain-boundary`
  - related: `character-frame-pipeline`
  - related: `action-domain-runtime`
- Affected code:
  - `Assets/Scripts/Character/Behavior/Runtime/*`
  - `Assets/Scripts/Character/Behavior/Solver/*`
  - `Assets/Scripts/Character/Pipeline/Runtime/CharacterRuntimeCore.cs`
  - `Assets/Scripts/Character/Movement/Runtime/*`
  - `Assets/Scripts/Character/Action/Runtime/*`
  - `Assets/Tests/Editor/Character/*`

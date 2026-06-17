## Context
Dodge 当前有两层表达：旧 variant 字段是生产权威，timeline builder 是等价验证工具。这样会造成具体能力仍绕过新 Action node/timeline 模型，后续技能编辑器无法证明“具体技能不污染抽象框架”。

本变更把 Dodge 作为第一个正式实例迁入 CommittedActionBranch selection + timeline：输入和请求仲裁仍在现有 Action 主线，内部 Directional / Backstep 内容由 node/timeline 数据表达。

## Goals
- 让 `Action.Dodge` 的正式 motion / animation 内容来自 timeline。
- 让 Directional / Backstep 的选择来自 Action selector / condition。
- 退役旧 Dodge variant 作为运行时权威配置。
- 保持现有 Dodge 手感和回归行为。

## Non-Goals
- 不做编辑器 UI。
- 不做完整 Skill 系统。
- 不新增表现层 runtime。
- 不改变 motion executor 或 animation presenter 权威。

## Decisions

### Decision: Planner 只决定意图
`DodgeActionPlanner` 或等价逻辑 SHOULD 只根据输入、移动意图和 facing 产出 dodge request intent、world direction 或 variant selection context。正式 duration、distance、animation key 和 timeline windows MUST 来自 timeline definition。

### Decision: Timeline 是运行时权威
Directional 与 Backstep 的 runtime motion spec、animation key 和 frame windows MUST 由 selected timeline 输出。旧 variant 字段 MAY 在迁移工具中读取，但 MUST NOT 在正式 resolver 中作为 motion/animation 权威继续运行。

### Decision: 无 fallback
如果 Dodge timeline / selector 配置缺失，系统 MUST 报告配置错误或拒绝动作输出，不得从旧 variant、代码默认、Resources 或场景字段补齐。

## Target Runtime Flow
```text
InputBuffer Dodge pressed
-> Action request provider creates Dodge request
-> Action request arbiter accepts / rejects request
-> Dodge resolver builds request context:
   - movement intent exists
   - world direction
   - facing direction
   - source step
-> Action lifecycle starts Action.Dodge
-> CommittedActionBranch selector reads request context
-> Selector chooses DirectionalTimeline or BackstepTimeline
-> ActionTimelineEvaluator outputs motion / animation / facts / cue
-> BehaviorSubmission carries Action outcome
-> CharacterFramePlan decides final output
-> OutputApplier executes selected output
```

## Data Migration Policy
- Existing assets MAY be converted by editor migration code or explicit config update.
- Migration MAY read old `DirectionalDodge` / `BackstepDodge` fields to create timeline clips.
- After migration, runtime MUST read timeline / selector definitions.
- Old fields MUST either be removed, hidden behind migration-only code, or validated as not used by runtime.

## Regression Matrix
```text
Input:
- Shift pressed creates one request.
- Shift held does not repeat.
- Accepted consumes request.
- Rejected preserves request until expiry.

Variant:
- Movement intent selects Directional.
- No movement intent selects Backstep.

Motion:
- Directional distance/duration/rotation unchanged.
- Backstep distance/duration/rotation unchanged.

Lifecycle:
- Completion timing unchanged.
- Animation-end waiting unchanged.
- Re-trigger after completion unchanged.

Locomotion:
- Directional completion writes Run latch when applicable.
- Backstep does not write Run latch.
```

## Migration Plan
1. 为 Dodge 定义 selector + Directional / Backstep timeline 运行时数据。
2. 将 existing Dodge builder 明确定性为迁移 adapter、测试 fixture helper 或删除对象。
3. 修改 resolver，使其输出 action context，而不是直接输出旧 variant motion spec。
4. 让 CommittedActionBranchEvaluator 选择对应 timeline。
5. 更新 Corin Action Catalog / Dodge definition。
6. 退役旧 variant 运行时权威字段或标记为迁移输入。
7. 增加完整 Dodge 回归测试。

## Risks / Trade-offs
- Risk: Dodge 手感变化。
  - Mitigation: 保留同 tick、duration、distance、rotateToDirection、Run latch 和 animation-end 等待测试。
- Risk: 旧资产迁移失败。
  - Mitigation: 提供 editor/config migration 或明确配置错误，不使用 fallback。

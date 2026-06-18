# Change: 将 Dodge 正式迁移到 Behavior Timeline

## Why
当前 Dodge 仍以旧 `DirectionalDodge` / `BackstepDodge` variant 字段作为生产权威，`DodgeActionBranchTimelineBuilder` 只证明可以生成等价 timeline，并未进入正式链路。要让 Dodge 成为框架内第一个 concrete instance，必须在 behavior submission entry 已闭环后，把 Directional / Backstep 的正式 motion、animation 和 frame window 内容迁入 CommittedActionBranch selector + timeline。

## What Changes
- 将 `Action.Dodge` 的正式运行时内容迁移为 CommittedActionBranch selection node + Directional / Backstep timelines。
- 旧 Dodge variant 字段只允许作为迁移输入或兼容读取，不再作为运行时权威配置。
- `DodgeActionPlanner` 或等价逻辑只保留方向、意图和 variant selection 所需的纯数据职责。
- Directional / Backstep 的 motion、animation key、duration frame、Run latch 相关输出和未来 window facts 来自 timeline / node definition。
- Dodge runtime 使用 frame 作为唯一时间权威；工具层 MAY 显示 seconds，但 runtime 不以 seconds 字段作为权威。
- 增加回归测试，证明 Directional、Backstep、Run latch、animation-end 等待和重复触发行为不回退。

## Implementation Slices
1. **Timeline authoring slice**：为 Dodge 定义 Directional / Backstep 两条正式 timeline 数据。
2. **Selector slice**：用 Action selector / condition 表达有方向输入与无方向输入的选择。
3. **Resolver slice**：让 Dodge resolver 输出纯 request context / world direction，不再输出旧 variant 权威 motion spec。
4. **Catalog slice**：将 Corin Dodge action catalog 配置迁到 selector + timeline。
5. **Retirement slice**：旧 variant 字段只作为迁移输入、测试 fixture 或删除对象，不作为正式 runtime authority。
6. **Regression slice**：用现有 Dodge 行为测试证明手感、Run latch、动画结束等待和再次触发不回退。

## Acceptance Criteria
- Directional Dodge motion spec、animation key 和 duration frame 来自 selected timeline。
- Backstep Dodge motion spec、animation key 和 duration frame 来自 selected timeline。
- Duration、window 和 timeline sampling 在 runtime 中以 frame 为权威，seconds 只允许作为编辑器显示或导入转换。
- 缺失 selector 或 timeline 时不使用旧 variant fallback。
- `DodgeActionPlanner` 不再保存正式动作手感参数权威。
- Directional / Backstep 的现有行为回归测试保持通过。
- Dodge 输出仍通过 BehaviorSubmission / CharacterFramePlan / OutputApplier 主线。

## Stop Conditions
- 如果迁移需要代码默认 timeline，必须停止。
- 如果迁移需要直接在 resolver 中执行 motion 或 animation，必须停止。
- 如果旧 variant 字段必须长期作为正式 runtime authority，必须停止并重审 ActionTimeline 目标。
- 如果 timeline 无法表达现有 Run latch 或 animation-end 等待语义，必须拆出专门设计。
- 如果 Dodge runtime 仍需要读取 seconds 权威字段推进 timeline，必须停止并回到 timeline 数据合同。

## Non-Goals
- 不新增完整攻击、连招或技能编辑器 UI。
- 不实现真实 hitbox / damage / VFX / SFX / camera runtime。
- 不改变 `CharacterFramePipeline` 和 behavior submission entry 的副作用边界。
- 不使用代码默认 timeline 或 Resources fallback 补齐缺失配置。

## Dependencies
- MUST 在 `add-dodge-behavior-submission-golden-line` 后实施。
- MUST 在 `add-character-behavior-submission-entry` 后实施。
- MUST 在 `add-committed-action-selection-nodes` 后实施。

## Impact
- Affected specs:
  - `dodge-action`
  - related: `character-action-catalog`
  - related: `action-domain-runtime`
  - related: `committed-action-node-selection`
- Affected code:
  - `Assets/Scripts/Character/Action/Config/CharacterActionDefinitionSO.cs`
  - `Assets/Scripts/Character/Action/Model/CharacterActionDefinition.cs`
  - `Assets/Scripts/Character/Action/Solver/CharacterActionRequestResolution.cs`
  - `Assets/Scripts/Character/Action/Solver/DodgeActionPlanner.cs`
  - `Assets/Scripts/Character/Action/Branch/*`
  - `Assets/Scripts/Character/Action/Timeline/*`
  - `Assets/Configs/3C/Action/*`
  - `Assets/Tests/Editor/Character/Action/*`

# Change: 拆分状态机通用模型与角色业务模型边界

## Why
`CharacterStateMachineTypes` 当前同时承载通用层级状态图结构、角色 FullBody/Locomotion/Action 业务字段、timeline/output/condition 相关模型。随着节点能力模块、条件插件化、timeline facts authority 和 action motion output 拆分推进，如果不把通用模型和角色业务模型分层，状态机 runtime 会继续被 FullBody 业务词污染。

本阶段要规划并实施 model 边界收敛：通用 hierarchical graph model 只表达状态图关系和 transition 基本结构；角色业务能力通过 metadata/module/extension model 进入，不再混在万能状态节点里。

## What Changes
- 将通用状态图模型与角色 FullBody 业务模型拆分。
- 保持自研统一状态机 runtime，不接入第二状态机 engine。
- 明确 generic node/transition/snapshot 与 character capability metadata 的关系。
- 将 Locomotion phase、Action state、timeline binding、output module、condition domain 等角色能力移到角色业务层模型或模块。
- 保持现有配置资产可迁移，不引入 fallback 配置。
- 加静态测试防止 generic runtime/model 引用 action/locomotion concrete 业务类型。

## Non-Goals
- 不实现新的状态机 engine。
- 不迁移到 UnityHFSM。
- 不改变当前默认状态图行为。
- 不实现新业务状态。
- 不改变 transition evaluator pluginization 已存在 proposal 的范围。

## Impact
- Affected specs:
  - `unified-character-state-machine`
  - `fullbody-hfsm-state-tree`
  - `fullbody-hfsm-tree-data`
  - `project-structure`
- Affected code:
  - `Assets/Scripts/Character/StateMachine/Model/CharacterStateMachineTypes.cs`
  - `Assets/Scripts/Character/StateMachine/Model/CharacterStateMachineDefinition.cs`
  - `Assets/Scripts/Character/StateMachine/Solver/Runtime/*`
  - `Assets/Scripts/Character/StateMachine/Solver/Transition/*`
  - `Assets/Scripts/Character/StateMachine/Solver/Output/*`
  - `Assets/Scripts/Character/Action/FullBody/Model/*`
  - `Assets/Scripts/Character/Movement/Model/*`
  - `Assets/Tests/Editor/UnifiedCharacterStateMachineTests.cs`

## Dependencies
- Should land after active `refactor-transition-condition-evaluators`, `refactor-state-timeline-facts-authority`, and `refactor-state-action-motion-output` have stabilized their contracts.
- Should land after `refactor-character-frame-data-contracts`, because frame/result contracts should be stable before model split.
- Must coordinate with config authoring proposals; state machine assets need migration tests.

## Success Criteria
- Generic state graph model files do not reference concrete Action/Locomotion runtime concepts.
- Character FullBody metadata/module model is explicit and testable.
- Existing default state machine asset converts to the new model without behavior changes.
- Runner still has one owner and still produces pure data snapshot/restore.

## Detailed Scope Partition
| Area | This change owns | This change must not own | Completion evidence |
| --- | --- | --- | --- |
| Generic graph model | State id, parent/child hierarchy, path, transition edge, active state, state time and snapshot identity. | Dodge, TurnBack, RunLatch, locomotion gait, action motion command, animation binding. | Static tests forbid character business symbols in generic graph files. |
| Character metadata | Character-specific node capabilities, owner interpretation, locomotion/action metadata and output module references. | Graph hierarchy ownership or runner tick algorithm changes. | FullBodyStateView tests derive same owner/phase/action view as before. |
| Capability modules | Typed containers for timeline binding, output binding, condition domain, action/locomotion metadata. | Unknown future plugin system beyond current needs. | Existing Idle/Move/TurnBack/Dodge assets validate through modules. |
| Snapshot layering | Separate generic active-path/time identity from character view interpretation. | Putting owner/phase/action state directly into generic snapshot as authority. | Snapshot/restore tests compare generic identity and derived FullBody view. |
| Asset migration | Convert or facade existing default state machine assets. | Adding fallback config or accepting partially migrated assets. | Asset validation fails explicitly when required metadata is missing. |
| Runner integration | Keep runner behavior stable while consuming new/facade model. | New state machine engine or second runner. | Tests prove one runner owner and no gameplay side effects in runner. |

## Layering Contract
The intended dependency direction is:

```text
Generic Graph Model
  <- Generic Runtime Snapshot
      <- Character Metadata / Capability Modules
          <- FullBody State View
```

Allowed knowledge:

- Generic graph may know ids, paths, hierarchy, transition edges and timing.
- Generic runtime may know current active node identity and elapsed state time.
- Character metadata may know which graph node carries Locomotion, Action, timeline, output and condition capabilities.
- FullBody view may read generic snapshot plus character metadata to produce convenient owner/phase/action interpretation.

Forbidden knowledge:

- Generic graph must not know Dodge, TurnBack, RunLatch, `BasicMovementGait` or `ActionMovementCommand`.
- Generic runner must not execute motion, animation, input consume or diagnostic submit.
- Character view must not become a second state authority.

## Sequencing With Existing Active Changes
This proposal should be implemented after these contracts stabilize:

1. `refactor-state-timeline-facts-authority`: defines where timeline facts live.
2. `refactor-transition-condition-evaluators`: defines condition key/evaluator registration.
3. `refactor-state-action-motion-output`: defines action motion output data.
4. `refactor-character-frame-data-contracts`: defines frame data contracts consuming runner output.

Doing this earlier risks splitting fields that are still moving.

## User Verification
用户可以通过这些方式确认 change 完成：

- 跑统一状态机、默认状态机资产验证、snapshot/restore 和 FullBody view 相关 EditMode 测试。
- 搜索 generic graph/runtime 文件，确认没有 Dodge、TurnBack、RunLatch、`BasicMovementGait`、`ActionMovementCommand`。
- 搜索 runner core，确认没有 motion、animation、input consume 或 diagnostic submit 副作用。
- 检查默认资产迁移测试，确认 Idle、MoveStart、MoveLoop、MoveStop、TurnBack、Dodge 关键路径等价。

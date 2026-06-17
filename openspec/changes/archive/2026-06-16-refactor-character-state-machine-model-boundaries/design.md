## Context
The current state machine model has grown organically while unifying Locomotion and Action. It now contains concepts from several layers:

- generic hierarchy: node id, parent, path, children
- generic runtime: active state, state time, pending transition, variant
- transition: condition keys, target, priority
- character domain: Locomotion phase, Action state, owner
- output: animation request, motion spec, run latch, input consume
- timeline: binding key/window policy

这些概念都和状态机有关，但不是同一层。通用 runtime 不应该知道 Dodge、TurnBack、RunLatch、BasicMovementGait 等业务词。

## Goals
- 定义 generic graph model。
- 定义 character state capability metadata/model。
- 定义 generic runtime snapshot 和 character view 的关系。
- 让 FullBody/Locomotion/Action 语义从模块或 metadata 派生。
- 保持现有 runner 行为和 restore 语义。
- 提供资产迁移和静态边界测试。

## Non-Goals
- 不改变状态机 tick 算法。
- 不改变 transition condition plugin 设计。
- 不改变 timeline facts authority。
- 不新增并行 layer runtime。
- 不删除旧资产，除非迁移测试覆盖。

## Proposed Shape
建议分层：

```text
Character/StateMachine/Model/
  Graph/
    StateGraphDefinition.cs
    StateGraphNode.cs
    StateGraphTransition.cs
    StateGraphSnapshot.cs
  Runtime/
    CharacterStateMachineSnapshot.cs
    CharacterStateMachineFrame.cs
  Character/
    CharacterStateNodeMetadata.cs
    CharacterStateCapabilityModule.cs
    FullBodyStateView.cs
    CharacterStateOutputModuleData.cs
```

依赖方向：

```text
Graph model <- Runtime model <- Character metadata/view
```

generic graph 不依赖 character metadata；character metadata 可以引用 graph node id。

## Decisions

### Decision: 自研 runner 保留
本 change 只拆 model 边界，不引入 UnityHFSM 或第三方 engine。

理由：当前项目规范明确正式主线是自研统一分层状态机。

### Decision: FullBodyStateView 是派生解释
`FullBodyStateView` 或等价 view 可以继续存在，但应从 character metadata/snapshot 派生，不反向污染 generic graph。

理由：业务层需要读 owner/action/locomotion phase，但 generic graph 不应该内置这些字段。

### Decision: 资产迁移必须可测
默认状态机资产需要自动测试证明迁移后节点、transition、输出和 timeline binding 等价。

理由：这是高风险模型拆分，不能只靠静态编译。

## Migration Plan
1. 读取 active 三个状态机深化 change，确认 contracts 稳定。
2. 新增 generic graph model shadow types。
3. 新增 character metadata/capability model。
4. 写转换器从现有 definition 转到新结构或逐步改构造器。
5. 让 runner 先继续消费兼容 definition facade。
6. 迁移 tests 和 default asset validation。
7. 删除或瘦身旧万能 model 字段。

## Risks / Trade-offs
- Risk: 资产迁移范围很大。
  - Mitigation: 先 facade/adapter，后删除旧字段。
- Risk: generic/character 分层过度抽象。
  - Mitigation: 只拆当前已有业务词，不为未知 layer 设计复杂插件系统。
- Risk: active changes 同时改 runtime frame。
  - Mitigation: 本 change 排在 timeline/condition/action motion 后。

## Open Questions
- generic graph model 是否仍放在 `ThirdPersonCharacterStateMachine` namespace？
- Character metadata 是否按 module list 存储，还是按 typed optional structs 存储？
- 资产迁移是否需要 editor tool，还是 runtime converter 足够？

## Interface Details
### `StateGraphDefinition`
- Interface: generic graph definition consumed by runner core.
- Invariant: contains graph topology and transition edges only.
- Allowed: node ids, parent ids, child ids, root id, transition definitions.
- Forbidden: character owner, locomotion phase, action state, motion command, animation binding.
- Test surface: static forbidden-symbol tests and graph topology tests.

### `StateGraphNode`
- Interface: generic node identity and hierarchy record.
- Invariant: node identity remains stable across asset migration.
- Allowed: id/path/name/parent/children/tags that are generic.
- Forbidden: typed action/locomotion behavior fields.
- Test surface: migrated asset node count/path tests.

### `StateGraphTransition`
- Interface: generic transition edge with target and condition key references.
- Invariant: condition keys are references, not evaluator implementation.
- Allowed: source, target, priority, condition key list, timing policy ids if generic.
- Forbidden: domain evaluator code, action can-exit implementation, locomotion animation exit implementation.
- Test surface: migrated transition equivalence tests.

### `StateGraphSnapshot`
- Interface: generic active state identity and timing snapshot.
- Invariant: snapshot is pure data and restorable without character scene dependencies.
- Allowed: active path/id, state time, pending transition id/key, variant id.
- Forbidden: owner/phase/action interpretation as authoritative fields.
- Test surface: snapshot/restore equivalence tests.

### `CharacterStateNodeMetadata`
- Interface: character-specific metadata keyed by graph node id.
- Invariant: metadata decorates graph nodes without changing graph topology.
- Allowed: owner, locomotion phase, action state, timeline binding, output module data, condition domain.
- Forbidden: runner tick algorithm and output side effects.
- Test surface: metadata validation tests.

### `CharacterStateCapabilityModule`
- Interface: typed or keyed capability payload for character state behavior.
- Invariant: module shape is formal configuration, not fallback behavior.
- Allowed: current known capabilities needed by FullBody/Locomotion/Action.
- Forbidden: speculative future layer fields without use.
- Test surface: default asset validation and missing-required-module failure tests.

### `FullBodyStateView`
- Interface: derived convenience view from generic snapshot plus character metadata.
- Invariant: view is read-only and not authoritative.
- Allowed: owner, locomotion phase, action state, derived active family.
- Forbidden: transition selection or runner mutation.
- Test surface: migration equivalence tests for Idle/Move/TurnBack/Dodge.

## Implementation Phasing
1. Wait until timeline facts, condition evaluator and action motion output contracts are stable.
2. Add static tests that define forbidden references in generic graph/runtime.
3. Introduce generic shadow model types without switching runner immediately.
4. Introduce character metadata/capability model and validators.
5. Add conversion/facade from existing definition to split model.
6. Switch runner lookups to generic graph plus character metadata view.
7. Migrate default asset validation tests and remove old万能 fields only after equivalence passes.

## Compatibility Facade Removal
`CharacterStateMachineDefinition` may temporarily expose legacy `Nodes` / `Transitions` while also exposing `Graph` / `CharacterMetadata`. This facade must shrink when authoring assets serialize graph nodes and character metadata directly. Removal is allowed only after default asset conversion, runner lookup, transition lookup, FullBody view derivation, and rollback tests all consume the split model without reading legacy node output fields.

## Stop Conditions
- Stop if generic graph needs a concrete character gameplay enum.
- Stop if runner must execute motion or animation to preserve behavior.
- Stop if asset migration needs hidden fallback config.
- Stop if FullBody view starts mutating active state.
- Stop if capability modules become a speculative plugin system for unimplemented layers.

## Validation Evidence
- Static forbidden-reference tests for generic graph/runtime.
- Default asset migration equivalence tests.
- Snapshot/restore equivalence tests.
- FullBodyStateView derived interpretation tests.
- `openspec validate refactor-character-state-machine-model-boundaries --strict --no-interactive`.

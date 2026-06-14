## Context
当前默认 Dodge 准入已经通过 `FullBodyActionInterruptGate` 接入 `ActionInterruptArbiter`，状态机默认入口只消费已被仲裁接受的 `CharacterInputRequestFact`。这解决了主要分裂路径。

Dodge 是 FullBody Action 管线的第一个实例。方向/后撤变体、动作位移、转向、结束后回到 Idle 或 MoveLoop、run latch 继承等都属于 Dodge 实例的数据和行为差异，应通过通用 FullBody Action 管线的配置、请求构建、状态机输出和执行边界表达。

本变更处理的是收尾清理：删除状态机中仍可表达“动作请求优先级准入”的遗留条件，统一 Dodge 运行时配置来源，并把当前状态 resistance 作为仲裁上下文的一部分正式接入。

## Goals / Non-Goals
- Goals:
  - 动作请求 priority、resistance、force、timing window 只由动作仲裁入口裁决。
  - 状态机 transition `priority` 只用于多条已满足 transition 的选择顺序。
  - 默认 FullBody Action 入口不得再暴露 `RequestPriorityAtLeast` 或等价条件。
  - Dodge 运行时配置从 `DodgeActionConfigSO` 或等价动作配置入口进入。
  - Dodge 的方向/后撤、动作位移、run latch、返回 Locomotion 等实例行为通过同一条 FullBody Action 管线表达。
  - 当前 Action resistance 从统一状态机快照和动作配置派生，进入 `ActionInterruptContext`。
- Non-Goals:
  - 不新增第二套 FullBody 状态机。
  - 不照搬 BBB 的 `ActionArbiter -> OverrideState -> ChangeState` 路径。
  - 不实现 Attack、HitReact、Death、连招、冷却、消耗或网络回滚。
  - 不删除日志。
  - 不修改动画资源本身。

## Decisions
- Decision: `RequestPriorityAtLeast` 不再作为默认状态机条件能力保留给 FullBody Action 入口。若实施时确认没有非动作场景依赖它，可以删除 enum、factory、evaluator 分支和测试引用。
- Decision: 删除 enum 项时必须保护 Unity 序列化兼容。可以使用显式 enum 数值保留现有 `kind` 含义，或先迁移资产再删除，不能让现有 `kind: 4`、`kind: 6` 等条件被挤位误读。
- Decision: `CharacterStateTransitionDefinition.Priority` 保留。它是状态图解析顺序，不是动作请求准入优先级。
- Decision: `PlayerFullBodyActionController` 通过序列化字段接收 `DodgeActionConfigSO` 或等价配置，运行时解析为纯 `DodgeActionConfig` 后传入 gate。
- Decision: 缺失 Dodge 配置应被校验报告。运行时可以保守 fallback 到 `DodgeActionConfig.Default` 以避免空引用，但默认 prefab 必须绑定正式配置资产。
- Decision: 当前 Action state 以统一状态机 `CharacterStateMachineSnapshot` 为权威。`ActionRuntimeStateTracker` 若被使用，只能作为由 snapshot 同步出的事实缓存，不得独立判断当前状态、自动退出或驱动 transition。
- Decision: 当前 resistance 由小型纯逻辑 resolver 派生：`Locomotion/None -> 0`，`Action.Dodge -> dodgeConfig.Resistance`。未来 Attack、HitReact、Death 接入时在同一 resolver 或动作配置表扩展，不回到状态机 transition。
- Decision: Dodge 可以有动作实例配置和实例行为解析。Dodge 的差异体现在配置数据、请求参数、变体、位移输出和 resistance 上；准入、状态事实和状态机输出仍走 FullBody Action 管线。

## Risks / Trade-offs
- Risk: 删除 `RequestPriorityAtLeast` 时破坏已有序列化 `kind` 数值。
  - Mitigation: 先静态搜索所有资产中的 `kind`，保留显式 enum 值或迁移资产后再删除。
- Risk: 缺失 `DodgeActionConfigSO` 导致运行时行为和测试不一致。
  - Mitigation: 新增默认配置资产并绑定 prefab；配置校验和测试覆盖缺失配置报告。
- Risk: `ActionRuntimeStateTracker` 被误用成第二状态权威。
  - Mitigation: 测试确认 Locomotion controller、状态机 runner、Presenter 不依赖 tracker；FullBody controller 只能从统一状态机快照同步或派生 Action facts。
- Risk: 连续 Dodge 需求需要 `Action.Dodge -> Action.Dodge` 策略。
  - Mitigation: 本变更只接通 resistance 上下文和单一路径；是否允许连续 Dodge 由策略资产和时间窗口决定，不在状态机 transition 中硬编码。
- Risk: 把 Dodge 实例行为误解成新的管线职责，会重新产生分裂路径。
  - Mitigation: 明确 Dodge 只是 FullBody Action 管线实例；保留实例数据和行为，但所有准入、状态事实和输出仍走同一条管线。

## Migration Plan
1. 静态确认默认状态机资产和测试中 `RequestPriorityAtLeast` 的剩余引用。
2. 移除或显式废弃状态机条件 `RequestPriorityAtLeast`，确保 transition `priority` 保持不变。
3. 新增或绑定默认 `DodgeActionConfigSO`。
4. `PlayerFullBodyActionController` 改为解析正式 Dodge 配置。
5. 新增当前 Action resistance resolver 或等价纯逻辑方法。
6. `FullBodyActionInterruptGate` 使用真实 resistance 构建 `ActionInterruptContext`。
7. 增加自动测试和静态边界测试。
8. 给出 Play Mode 手动验证步骤。

## Open Questions
- 如果实施时发现非动作场景仍依赖 `RequestPriorityAtLeast`，应暂停删除并回到 proposal 更新范围；不得把该条件继续用于默认 FullBody Action 入口。

# Change: 重构 Simulation Operation Runtime 模块边界

## Why

当前 Float32 Kernel 通过 `SimulationKernel -> SimulationOperationMachine.Evaluate` 推进角色 Program，但 `SimulationOperationMachine` 实际由 6 个 `partial` 文件共同组成，总计 4310 行。它同时持有 Program、Layout、Input、StateBuilder、Facts、Presentation、Trace、Motion、State execution stack 与 GameplayEffect runtime，并直接实现 Runnable、Composite、StateMachine、Value、Blackboard、Action、Timeline、GameplayEffect 和 Motion 语义。

`partial` 只拆开了文件，没有形成模块：所有实现仍可直接修改同一批可变字段。现在修改 Timeline 可能改变 Motion 汇总，修改 Action 可能直接读取 GE 内部状态，修改 Runnable stop 可能同时影响 Blackboard scope 与 Presentation 输出。该结构也使后续 Fixed Target 无法在不复制 StateMachine/打断控制流、或把全部 4310 行整体泛型化的情况下实现同一 operation-set。

本 change 将执行器收敛为一个 Evaluate 事务入口、一个明确的事务帧、portable Core 中唯一的控制流运行时，以及 Float32 Target 内按业务职责拆分的领域模块。重构必须保持当前 Program/State ABI、`.csir`、`.csim`、ProgramHash、LayoutHash、StateSlot、EventId、事实顺序、表现命令顺序和 Motion 结果语义不变。

## Dependencies

- `refactor-character-simulation-core` MUST 已完成并归档。
- `refactor-character-semantic-frontend-artifact` MUST 已完成并归档。
- 本 change 不依赖 `refactor-gameplay-session-composition-boundary` 的 Host/Composer 迁移；两者不得并行修改同一 `SimulationKernel` 调用点。
- `add-deterministic-rollback-kcc-model` 在实施 Fixed Kernel 前 MUST 依赖本 change 的 portable control runtime，不得复制 Runnable、Composite、StateMachine 与 stop propagation 语义。

## What Changes

- 在 portable Core 建立不可变 `OperationExecutionTopology`，只投影 operation code、handle、control-flow edge、reference、root 和 lifecycle/state semantic slot 绑定；它由 Target Program 的已校验布局一次构建，不保存 scalar/vector 常量，不序列化为第二份 Program，也不改变 ProgramHash。
- 在 portable Core 建立唯一 `OperationControlRuntime<TTarget>`，负责 Runnable enter/update/complete、activation generation、Composite child 选择、StateMachine transition、state execution path、graceful stop、force stop 和 descendant stop barrier。
- 建立受约束的 Target port。portable control runtime 只能请求目标 State cell 读写、Condition 求值、Leaf operation 执行、scope lifecycle hook 和结构 Trace；它不能直接产生 Animation、Camera、Cue、GameplayEffect、Motion 或 Network 输出。
- 将 Float32 根入口改为 `Float32OperationEvaluator`。它只编排现有 Evaluate 顺序并返回唯一 `CharacterOperationEvaluation`，不再实现具体节点、Timeline、Action 或 GE 规则。
- 建立 `Float32EvaluationFrame`，集中一次 Actor/Tick 的 Program、Layout、Input、Ingress、Body、StateBuilder、Facts、Presentation、Trace、Motion contribution、execution budget 和 execution context。它是事务数据，不是第二个业务执行器。
- 将 Float32 领域语义拆为 Value/Input、Blackboard、Action、Timeline、GameplayEffect operation bridge 与 Motion accumulator 模块；模块通过窄读写口协作，不直接取得另一个模块的具体实现。
- 将 Timeline 收敛为时间/clip/TreeClip/Cue/Camera/Animation/MotionContribution 采样模块；Locomotion 与 Timeline 都只提交 contribution，唯一 Motion accumulator 负责 Channel/Priority/Weight/BlendMode 汇总并输出 `CharacterMotionRequest`。
- 将 Trace 的诊断序列与 Gameplay/Presentation `FactSequence` 完全分离；Trace 不写 CharacterSimulationState、不参与 StateHash，也不能改变后续外部 EventId。
- 让 graceful stop 与自然完成、force stop 使用对称的 scope completion，再重置 operation state，避免 GraphInstance Blackboard scope 跨 activation 残留。
- 将 SourceMap operation lookup、Timeline curve decode 与 state-access policy 收敛为 Program 级不可变执行服务；Actor/Tick 事务只持有可重置工作集合和当前请求数据。
- 保留现有 `SimulationGameplayEffectRuntime` 作为 GE 规则实现；本 change 只拆出 Operation 到 GE 的薄桥，不在同一 change 重构 1409 行 GE 内核。
- 删除 `SimulationOperationMachine` 及全部 `partial` 实现、嵌套 DTO 引用、跨 partial 私有字段共享和旧构造调用。最终不得保留兼容 wrapper、双执行器、handler registry fallback 或旧类型别名。
- 更新 Fixed rollback active change 的依赖和措辞，明确“同一业务 operation-set”不等于复用 Float32 Program/State ABI；Fixed Target 必须复用 portable control runtime，并为数值相关 Leaf 提供自己的 Target backend。

## Non-Goals

- 不改变 BTSMTL authoring、Semantic IR schema、Float32 Program canonical codec、State layout 或 Corin 资产。
- 不实现 FixedQ32.32 Program、Fixed State、Deterministic KCC、Rollback Driver 或 Network Model。
- 不拆分 `SimulationGameplayEffectRuntime`、`SimulationGameplayEffectState` 或 Agent/Timeline Editor 大类。
- 不改变单个 SimulationStep 的 `Evaluate all -> ResolveBatch -> Finalize all` 业务语义或 Session 的原子 publish/commit 约束；外层零到多步调度、restore、Egress 与具体 Pass 顺序由 `refactor-gameplay-session-composition-boundary` 的 compiled Pipeline plan 负责。
- 不增加 runtime Graph/Timeline clone、reflection dispatcher、按字符串查找 handler、动态 service locator、fallback 或兼容路径。
- 不新增测试或人工验证任务，不运行 Unity batchmode。

## Current Spec Comparison

- `character-simulation-kernel` 已要求不同 Numeric Target 实现同一 Semantic Operation Set，但当前把“不得复制业务 evaluator”与“Target 必须拥有独立 Program/State/Kernel ABI”写在一起，没有明确哪些语义必须共享源码、哪些数值 Leaf 必须由 Target backend 实现。本 change 修改该 requirement：Runnable、Composite、StateMachine 和 stop propagation 由 portable control runtime 唯一实现；Value、Timeline numeric sampling、GE magnitude 和 Motion blending 由 Target backend 实现，但不得改变 operation 含义和顺序。
- `btsmtl-node-interruption-lifecycle` 已要求 Tree 调度只负责 child、stop barrier 和结构结果，不产生表现或 GE 输出。portable control runtime 直接落实该约束，不修改该 spec。
- `character-motion-simulation-boundary` 已要求 motion operation 产生 contribution 并形成唯一 WorldRequest，但没有明确 Timeline 与 Locomotion 不能各自解析最终 Motion。本 change 补充唯一 Target Motion accumulator 的所有权。
- `character-action-instance-runtime`、`character-pipeline-blackboard` 与 `character-gameplay-effect-integration` 已要求 Action、Blackboard、GE 状态只存在于 CharacterSimulationState。本 change 只移动实现所有权，不新增第二份状态，因此不修改这些 specs。
- `refactor-gameplay-session-composition-boundary` 负责 Session Host、Composer、Pipeline compiler/runtime 与 Actor registration，不负责 operation 内部执行。本 change 不复制这些职责；标准 Program Evaluate Pass 只调用本 change 交付的唯一 `Float32OperationEvaluator`。
- `add-deterministic-rollback-kcc-model` 当前同时写了“建立每个 operation 的 Fixed backend”和“不得新增第二业务 evaluator”。前者正确，后者措辞不够精确。本 change 完成时必须将其修正为：共享 portable control runtime 和 operation-set，Target 只实现自己的 numeric leaf backend，不能复制 authoring 或更改业务语义。

## Impact

- Portable Core：新增 operation topology、control runtime、Target port 与 control cursor。
- Float32 Core：新增 evaluator、evaluation frame、Target adapter、Value、Blackboard、Action、Timeline、GE bridge 和 Motion modules。
- 删除：6 个 `SimulationOperationMachine partial` 实现及其嵌套类型耦合。
- Runtime 行为：Evaluate 顺序、StateSlot、事实、Presentation、Motion、Trace、EventId 与 failure policy 保持不变。
- Diagnostics：Trace 使用独立诊断序列；修复前被 Trace 消耗的 Gameplay/Presentation sequence 不再保留，因为该行为违反只读 diagnostics current spec。
- 下游规划：Fixed rollback change 增加本 change 依赖并修正 Target backend 口径；Session composition 不新增行为依赖。

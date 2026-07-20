## Context

当前 `SimulationOperationMachine` 的公开使用面很小：`SimulationKernel` 构造它并调用 `Evaluate()`。问题集中在实现内部。六个 partial 文件共享同一组字段，因此 Timeline、Action、Blackboard、StateMachine、GE 和 Motion 之间没有真实写权限边界。

当前 Evaluate 顺序是：

```text
BeginBlackboardFrame
-> ApplyIngress
-> AdvanceGameplayEffects
-> ApplyInputRequests
-> PrepareDecisionTimelines
-> TickRunnable(root)
-> ResolveMotion
-> WritePendingMotion
-> SaveGameplayEffects
-> EndBlackboardFrame
-> CharacterOperationEvaluation
```

该顺序已经承载同 Tick Decision Blackboard、Action/GE ingress、RootTree、Timeline commit、Motion request 和输出 EventId 的业务含义，必须作为事务合同保留。

## Goals

- 保留一个深入口：`Evaluate(request) -> CharacterOperationEvaluation`。
- 让控制流、Target 数值语义、业务状态和输出所有权可以分别定位和修改。
- 让未来 Fixed Target 复用相同 Runnable、Composite、StateMachine 和 stop propagation 源码。
- 不把 Float32/Fixed 的 Program、State、Scalar、Timeline、GE 和 Motion ABI强行统一。
- 不改变任何已发布 Program/Projection/State identity 或 Corin 行为配置。

## Decision

采用“portable 控制流 + Target 领域模块”的混合拆分。

### 1. Portable OperationExecutionTopology

`OperationExecutionTopology` 是 Program 的只读运行索引，包含：

- Root operation。
- OperationHandle 与 SimulationOperationCode。
- Child、Transition、Value 等 control-flow edge。
- Operation reference。
- Runnable、cursor、generation、stop context、active state 等 semantic slot 绑定。

它不包含 Float32Scalar、Vector、Yaw、numeric constant payload、catalog payload 或可变 state。Float32 `ProgramExecutionLayout` 在 Program 完整校验后一次创建该 topology；Session 复用同一实例，不能每 Actor/Tick 重建。

Topology 不是新 artifact，不写入 Assets、Library 或 Snapshot，不参与 ProgramHash/LayoutHash，也不能作为 Program 缺失时的 fallback。

### 2. Portable OperationControlRuntime

`OperationControlRuntime<TTarget>` 唯一负责：

- Runnable Dormant/Running/Success/Failure/Stopping。
- activation generation。
- Root、Loop、Sequence、Selector、Parallel。
- StateMachine active state、transition selection 与 state execution path。
- graceful stop、force stop、descendant propagation 与 stop barrier。
- 结构执行预算与结构 Trace 顺序。

它不解释 Float32 value，不读 Action/GE 配置，不采样 Timeline，不生成 Motion，不提交 Presentation。

`TTarget` 使用受约束的值类型 adapter，避免每个 operation 的反射、handler dictionary、delegate allocation、boxing 或 service lookup。Target port 只提供：

- lifecycle/control state cell 的合法读写。
- Condition 求值。
- 非控制流 Leaf operation 执行。
- operation/state scope activation、completion、clear hook。
- 结构 Trace sink。

Target Leaf 需要运行或停止 compiled child 时，只获得窄 `OperationControlCursor`，其能力限定为 Tick、RequestStop、ContinueStop、ForceStop 和 IsActive。Target module 不能取得 control runtime 的内部集合或改写 child cursor。

### 3. Float32OperationEvaluator

`Float32OperationEvaluator` 是 Float32 Kernel 的唯一 operation 入口。它创建一次 `Float32EvaluationFrame`，按固定 Evaluate 顺序调用模块，并返回结果。它不保存跨 Tick 状态；跨 Tick 数据仍只在 `CharacterSimulationState`。

```text
SimulationKernel.Evaluate
-> Float32OperationEvaluator.Evaluate
   -> Float32EvaluationFrame
   -> Float32 ingress/GE/action preparation
   -> OperationControlRuntime<Float32OperationTarget>
   -> Float32 MotionAccumulator
   -> CharacterOperationEvaluation
```

### 4. Float32EvaluationFrame

Frame 只拥有一次 Actor/Tick 的事务数据：

| 数据 | 唯一写入者 |
|---|---|
| Character state slots | 对应语义模块通过受限 state port |
| GameplayFact | Fact sink |
| PresentationCommand | Presentation sink |
| SimulationTraceRecord | Trace sink |
| MotionContribution | Motion contribution sink |
| CharacterMotionRequest | Motion accumulator |
| Execution budget | portable control runtime |
| State execution context | portable control runtime |

Frame 不公开全部字段给所有模块。模块构造时只取得所需 port，不能通过 Frame 反查另一个具体模块。

### 5. Float32 领域模块

| 模块 | 输入 | 输出 | 禁止事项 |
|---|---|---|---|
| ValueRuntime | Program value operation、Input、State/Blackboard/GE query | typed Float32 value 或 Bool condition | 不写 Action/Timeline/Presentation |
| BlackboardRuntime | declaration/layout、scope hook、typed value、provenance | state slot write、projection candidate/fact | 不拥有 Action 或 GE state |
| ActionRuntime | input request、ingress、Action catalog、Tag query | Action state、lifecycle fact、Action context | 不推进 Timeline 或直接改 GE state |
| TimelineRuntime | Timeline catalog、current Tick、Body、Action context、Blackboard port | TreeClip result、Cue/Camera/Animation command、MotionContribution | 不解析最终 MotionRequest |
| GameplayEffectOperationRuntime | GE operation、ingress、当前 GE state | GE command/query、change projection | 不复制 GE stacking/duration/magnitude 规则 |
| MotionAccumulator | Locomotion/Timeline contributions | 唯一 CharacterMotionRequest | 不采样 Timeline、不调用 WorldSolver |

Action 读取 GameplayTag 时使用只读 GE query port；Timeline 读取 Action Context 或写 Blackboard 时使用窄 port。不存在模块之间持有具体实现或双向调用。

### 6. Dispatch

中央 operation code dispatch 保留，因为 operation-set 是封闭且版本化的。控制流 code 由 portable runtime 处理；其余 code 交给 Float32 Target dispatcher，再分派给领域模块。

不使用运行时注册表或反射。未知 operation code 继续明确失败，不能跳过、返回 Success 或搜索 fallback handler。

### 7. Motion ownership

重构后：

```text
Locomotion operation ----\
Timeline MotionCurve -----+-> MotionContributionSink
业务 modifier -----------/        |
                                  v
                         Float32MotionAccumulator
                                  |
                                  v
                         CharacterMotionRequest
```

Timeline module 不再同时拥有采样和最终汇总。Channel、Priority、Weight、BlendMode、ConsumeLowerChannels 的当前排序和计算保持不变，只移动到唯一 accumulator。

### 8. GameplayEffect ownership

`SimulationGameplayEffectRuntime` 和 `SimulationGameplayEffectState` 仍是现有 GE 规则与状态实现。本 change 新增的 Operation bridge 只负责：

- 将 operation/ingress 转成正式 GE 调用。
- 将 GE change 投影为当前 Tick 的 Fact/Presentation/Trace。
- 在 Evaluate 固定位置 Advance/Save。

它不能保存第二份 ActiveEffect、Attribute、Tag、journal 或 prediction state。GE 内核本身的进一步拆分必须另开 change。

### 9. Diagnostics sequence isolation

GameplayFact 与 PresentationCommand 继续使用 CharacterSimulationState 中的正式 `FactSequence`。Simulation Trace 使用只存在于当前 evaluation/finalize diagnostics transaction 的独立单调序列。Trace sequence 不得写 StateSlot、进入 StateHash 或消耗后续外部 EventId 的 local sequence。

Trace EventId 仍由 Program、Actor、activation、Tick、diagnostics local sequence 和 `Trace` channel 稳定生成。增加、删除或按 interest 关闭 Trace 只允许改变 Trace 自身的 identity/order，不得改变 Gameplay、Presentation、Motion 或 Snapshot。

### 10. Symmetric scope completion

operation scope lifecycle 必须保持：

```text
ActivateScopes
-> Running/Stopping
-> CompleteScopes
-> ResetOperationState
```

自然完成、graceful stop 和 force stop 都必须遵守该顺序。State-specific scope 仍由 State exit 规则清理；`CompleteScopes` 负责普通 GraphInstance scope，二者不能互相替代。

### 11. Program services and Actor workspace

`ProgramExecutionLayout` 唯一拥有随 Program 构建的 Float32 immutable services：operation SourcePath index、Timeline curve decode cache 和预验证 state-access policy。它们按 Program 构建一次，不保存 Actor、Tick、StateBuilder 或输出集合。

每个 Actor evaluator 可持有不进入 Gameplay State 的 reusable workspace，用于 Fact/Presentation/Trace staging、Value recursion、Motion contributions 和模块临时集合。每次 Evaluate 必须清空 workspace，并在失败后保持可重用；workspace 不得参与 Snapshot、StateHash 或作为业务状态 fallback。

## Rejected Alternatives

### 只把 partial 方法移动到多个 class

收益是改动较小，但如果所有 class 都取得同一个万能 Context，任何模块仍能改全部状态和输出，只是从“巨型 partial”变成“巨型 service locator”。未来 Fixed 仍会复制控制流，因此拒绝。

### 将全部 4310 行做成 Float32/Fixed 泛型

Timeline、GE、Motion、Value 与 State payload 都会把 Scalar/Vector/Program/State 泛型传播到整个调用图，接口复杂度接近实现复杂度，编译错误与维护成本都很高，因此拒绝。

### 为每个 operation code 建 handler object registry

会增加运行时注册、查找、对象数量与缺失 handler fallback 风险，且 operation-set 本来就是版本化封闭集合，因此拒绝。保留明确 switch。

### 只保留 Target 专属完整 evaluator

Float32 最容易完成，但 Fixed 将重新实现 Sequence、Parallel、StateMachine、interrupt 和 stop barrier，形成真正的第二业务 evaluator，因此拒绝。

## Compatibility And Identity

- `.csir` bytes、SemanticHash、operation-set version 不变。
- Float32 `.csim` canonical bytes、ProgramHash、LayoutHash 不变。
- CharacterSimulationState slot 数量、kind、index、owner、default 不变。
- Input request、Action instance、Timeline segment、stop context 的 canonical bytes 不变。
- Gameplay/Presentation EventId 输入与 Fact/Presentation 顺序保持业务语义；Trace 改用独立 diagnostics sequence，不再消耗 `FactSequence`。
- Motion contribution 排序和数学计算不变。
- Trace code、source operation 和顺序保持不变；Trace identity 只使用 diagnostics sequence，且不得进入 Gameplay EventId/StateHash。

如果实施发现必须改变 Program codec、State layout 或 serialized bytes 才能完成 topology/control runtime，必须停止并修改 proposal；不得偷偷提升 ABI 或重编 Corin 资产掩盖变化。

## Migration

1. 固定当前 operation、slot、output、Evaluate ordering 清单。
2. 建立 topology 与 Target port，但不建立第二 Evaluate 入口。
3. 建立 portable control runtime，并让 Float32 Target adapter 直接消费现有 Program/State。
4. 建立 Frame 与领域模块，逐项移动实现和所有权。
5. 将 `SimulationKernel` 一次切换到 `Float32OperationEvaluator`。
6. 删除旧 `SimulationOperationMachine` partial 文件、旧嵌套类型与旧引用。
7. 静态确认仓库只有一个 Float32 Evaluate path、一个 control runtime 和一个 Motion accumulator。

最终源码中不得保留旧 wrapper、兼容构造器、双 dispatch、feature flag 或临时 adapter。

## Failure Policy

- Topology 与 Program operation/edge/slot 不一致：Session composition 或 Layout 创建失败。
- Target port 收到不支持的 control state kind：当前 Evaluate 失败。
- 未知 operation code 或缺少领域模块实现：当前 Evaluate 失败。
- Leaf 越权请求不存在的 child/stop target：当前 Evaluate 失败。
- Output owner 重复提交同一 EventId：保持现有严格校验并失败。
- 任一失败不得回退旧 `SimulationOperationMachine`。

## Downstream Changes

- `refactor-gameplay-session-composition-boundary` 继续只负责 Session Composer/Host、compiled Pipeline plan 与标准 Step Pass；Program Evaluate Pass 通过 `SimulationKernel` 调用本 change 的唯一 evaluator，不把领域模块提升为 Session Pass。
- `add-deterministic-rollback-kcc-model` 必须复用 portable topology/control runtime，并实现 Fixed Target port 与 numeric leaf modules；不得复用 Float32 State/Program，也不得复制 portable control flow。
- `refactor-server-authoritative-hybrid-runtime` 与 DotRecast backend 使用 Float32 evaluator，不增加 model-specific operation handler。

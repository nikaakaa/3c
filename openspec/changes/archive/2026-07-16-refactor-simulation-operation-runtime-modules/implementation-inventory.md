# Implementation Inventory

## Current Footprint

当前 `SimulationOperationMachine` 为同一个 partial class：

| 文件 | 行数 | 当前主要职责 |
|---|---:|---|
| `SimulationOperationMachine.cs` | 987 | Evaluate、Composite、StateMachine、Value、Input、Fact/Trace、通用查找 |
| `SimulationTimelineOperationRuntime.cs` | 1109 | Timeline、TreeClip、Cue、Camera、Animation、Locomotion、Motion 汇总 |
| `SimulationActionRuntime.cs` | 715 | Action activation/lifecycle/ingress、Tag query、Action codec |
| `SimulationBlackboardRuntime.cs` | 713 | scope/lifetime、读写、provenance、ActionWindow projection |
| `SimulationRunnableLifecycle.cs` | 498 | enter/complete、stop/force stop、State execution path |
| `SimulationGameplayEffectMachine.cs` | 288 | GE advance/save、apply/remove/query、change projection |
| 合计 | 4310 | 同一类共享全部可变状态 |

## Shared Mutable Fields

根类当前持有：

```text
Program
ProgramExecutionLayout
ActorId / Tick / Input / Ingress / PreviousBody
CharacterSimulationStateBuilder
Facts / Presentation / Trace / MotionContribution
Value recursion stack
Execution budget
State execution stack
SimulationGameplayEffectRuntime
```

六个 partial 文件均可跨职责访问这些字段。`SimulationBlackboardRuntime` 还直接引用 `SimulationOperationMachine.ActionInstanceState`，说明文件拆分没有形成类型所有权。

## External Entry

当前外部构造调用只有：

```text
SimulationKernel.Evaluate
-> new SimulationOperationMachine(request)
-> Evaluate()
```

因此重构可以保持 Kernel 的单一入口，不需要兼容 wrapper 或第二运行路径。

## Existing Runtime Cache

`ProgramExecutionLayout` 已在 Session 级按 Program 构建并复用 outgoing edge、incoming value、reference、operation slot 与 root 索引。新 `OperationExecutionTopology` 必须从该正式布局拆出或由其一次创建，不能在每 Tick 重建同样的 List/排序，也不能成为第二份 serialized Program。

## Locked Behaviour

- Evaluate 阶段顺序不变。
- Runnable/Composite/StateMachine 的 result、cursor、generation、stop context 和 barrier 不变。
- Decision Timeline 在 Root operation 前，Commit Timeline 在对应正式位置执行。
- Blackboard Frame cleanup、Action Context 和 projection provenance 不变。
- GE ingress/advance 在 Graph decision 前，Save/Change projection 顺序不变。
- Timeline 与 Locomotion contribution 排序、混合和 pending motion bytes 不变。
- Facts、Presentation、Trace、EventId 和 source operation identity 不变。
- Program、State、Snapshot 与 Corin assets 不迁移。

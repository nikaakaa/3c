## MODIFIED Requirements

### Requirement: Kernel Backend 必须实现同一 Semantic Operation Set

Semantic IR 的 versioned operation set MUST唯一规定 Runnable、StateMachine、Timeline、Blackboard、Action、GameplayEffect 和 Motion 的控制流、状态所有权、事件顺序与输入输出语义。Runnable enter/update/complete、Root/Loop/Sequence/Selector/Parallel、StateMachine transition、state execution path、graceful stop、force stop、descendant stop barrier、Timeline segment/cycle/window/cue 生命周期、GameplayEffect application/stack/period/expire/prediction bookkeeping MUST由 portable Core 中唯一的 operation control runtime 实现。Numeric Target MUST通过受约束 Target port 提供自己的 control state access、Condition 求值、numeric/domain Leaf backend、curve sample、magnitude calculation 与 typed state storage，MAY拥有不同 Program/State/Numeric ABI，但 MUST不复制 portable control flow或业务生命周期、改变 operation 含义或要求不同 authoring node。Target 不支持完整 operation-set 时 MUST在 build/composition 失败。

#### Scenario: Fixed Target 执行 Timeline 和 GameplayEffect

- **WHEN** Fixed Target 对与 Float32 相同的 Timeline、GameplayEffect 和 StateMachine authoring 执行 Program
- **THEN** 两个 Target MUST复用同一 portable Timeline lifecycle、GameplayEffect lifecycle 和 control-flow runtime
- **AND** Fixed Target MUST只提供 fixed time/curve/magnitude、typed state access 和 output leaf
- **AND** MUST不保留 `FixedTimelineControlRuntime` 或 `FixedGameplayEffectLifecycle` 形式的第二业务实现

#### Scenario: Float32 Target 执行 StateMachine

- **WHEN** Float32 Program 执行 nested StateMachine、Parallel 或 LowerPriority interruption
- **THEN** portable control runtime MUST决定 child、transition、stop cause 与 barrier
- **AND** Float32 Target MUST只负责 Condition 与 Leaf operation，不得另行推进第二份 control cursor

#### Scenario: Target 缺少 GameplayEffect 数值能力

- **WHEN** Program 使用 Target 未实现的 magnitude、modifier 或 attribute numeric operation
- **THEN** build/composition MUST明确失败并报告 operation 与 Target identity
- **AND** portable GameplayEffect control MUST不跳过该效果或选择其它 Target fallback

### Requirement: Execution Backend 必须按 Pipeline 事务原子推进零到多个 Step

Execution Backend MUST通过 portable Pipeline Transaction coordinator 先运行 Ingress和唯一 Schedule producer，再按 ExecutionPlan可选 restore并执行零到多个 ordered Step。每个标准 Step MUST按 compiled phase order执行全部 Step Pass，其中 MUST存在按 stable ActorId order执行的唯一 Program Evaluate、一次 World ResolveBatch与唯一 Program Finalize核心锚点，且三个锚点 MUST依次排列。附加 Step Pass MAY依照 descriptor顺序和 Product依赖在核心锚点前后执行，但 MUST在 completed step与 Pipeline projection冻结前完成；portable Core与 Target port MUST不硬编码具体 Network Model的附加 Pass identity。多个 replay step MUST只推进 working state。全部 Step与 Egress成功后 coordinator MUST原子发布最终 Character/World/Pipeline state并 Commit外部输出。任一阶段失败时 MUST不发布部分 working state或副作用。Float32 与 Fixed MAY使用不同 typed transaction port、working state、snapshot codec 和 World request/result ABI，但 MUST不复制阶段顺序、失败回滚、publish 或 commit 规则。

#### Scenario: 第二个 Replay Step Finalize 失败

- **WHEN** Replay 101成功而 Replay 102的 ActorB world result identity不匹配
- **THEN** portable coordinator MUST拒绝整个 outer transaction
- **AND** Replay 101的 state和外部输出 MUST不成为正式结果

#### Scenario: Float32 与 Fixed 运行相同 ExecutionPlan

- **WHEN** 两个 Target 收到语义相同的 restore、replay、current 与 egress plan
- **THEN** 两者 MUST由同一 coordinator 决定阶段和原子提交顺序
- **AND** Target port MUST只处理自己的 typed state、Evaluate、World resolve input/output 和 Finalize

#### Scenario: Rollback History 消费 Finalize 结果

- **WHEN** Fixed Rollback Pipeline 在三个核心 Step锚点之后声明消费 FinalizedStepResult的 History Pass
- **THEN** coordinator MUST在同一 Step内按 compiled order先执行 Program Finalize再执行 History
- **AND** History状态 MUST在 completed step与 Pipeline projection捕获前完成更新
- **AND** Fixed Target port与 portable Core MUST不把 Rollback History当作第四个核心阶段或硬编码其 Pass identity

### Requirement: Program 级执行服务不得每 Tick 重建

operation topology、SourceMap index、Timeline compiled curve/segment lookup、GameplayEffect descriptor/index、state-access policy、immutable roster 与 stable Actor order 等只依赖 Program/Layout/Session composition 的执行数据 MUST分别随 ProgramExecutionServices或 Session execution layout 构建一次并复用，MUST不在每 Actor/Tick/replay step 重建。Session 与 Actor workspace MAY复用临时集合和容量，但每次 outer transaction或 Evaluate MUST按 owner 清空，MUST不保存 Gameplay 状态或跨 Actor 共享可变事务数据。Snapshot、history、published state、egress output 和持久 diagnostics 在越过事务边界前 MUST冻结或复制，不得持有下一 Tick 会重置的 workspace memory。

#### Scenario: 同一 Actor 连续执行两个 Tick

- **WHEN** 两个 Tick 使用同一 ProgramExecutionServices和 Actor workspace
- **THEN** MUST复用相同 immutable execution services 与已分配容量
- **AND** 第二个 Tick MUST不观察到第一个 Tick 的临时 Fact、Trace、Timeline segment、GE scratch 或 Motion contribution

#### Scenario: Snapshot 越过 Tick 边界

- **WHEN** outer transaction 生成需要进入 rollback history 的 Snapshot
- **THEN** Snapshot MUST在 workspace reset 前拥有独立 immutable bytes或等价冻结存储
- **AND** 后续 Tick 的 workspace 写入 MUST不改变该 Snapshot、StateHash 或 restore 结果

#### Scenario: Timeline 只命中一个 Segment

- **WHEN** 当前 sample range 不跨越 Segment 或 cycle 边界
- **THEN** Timeline runtime MUST使用不创建 Segment collection 的单段路径
- **AND** 结果语义 MUST与使用 bounded scratch 的跨段路径一致

## MODIFIED Requirements

### Requirement: Kernel Backend 必须实现同一 Semantic Operation Set

Semantic IR 的 versioned operation set MUST唯一规定 Runnable、StateMachine、Timeline、Blackboard、Action、GameplayEffect 和 Motion 的控制流、状态所有权、事件顺序与输入输出语义。Runnable enter/update/complete、Root/Loop/Sequence/Selector/Parallel、StateMachine transition、state execution path、graceful stop、force stop 与 descendant stop barrier MUST由 portable Core 中唯一的 operation control runtime 实现。Numeric Target MUST通过受约束 Target port 提供自己的 control state access、Condition 求值与 numeric/domain Leaf backend，MAY拥有不同 Program/State/Numeric ABI，但 MUST不复制 portable control flow、改变 operation 含义或要求不同 authoring node。Target 不支持完整 operation-set 时 MUST在 build/composition 失败。

#### Scenario: 后续增加 Fixed Kernel Specialization

- **WHEN** Rollback change 安装 FixedQ32.32 Target
- **THEN** Fixed Kernel MUST复用与 Float32 相同的 portable Runnable、Composite、StateMachine 和 stop propagation runtime
- **AND** Fixed Target MUST为 Value、Timeline numeric sampling、GameplayEffect magnitude 与 Motion blending 提供匹配 Fixed ABI 的 Leaf backend
- **AND** MUST不新增 DeterministicMoveNode、RollbackStateMachineRuntime 或模型专属 Action 业务规则

#### Scenario: Float32 Target 执行 StateMachine

- **WHEN** Float32 Program 执行 nested StateMachine、Parallel 或 LowerPriority interruption
- **THEN** portable control runtime MUST决定 child、transition、stop cause 与 barrier
- **AND** Float32 Target MUST只负责 Condition 与 Leaf operation，不得另行推进第二份 control cursor

## ADDED Requirements

### Requirement: Operation Evaluate 必须只有一个事务入口

每个 Numeric Target 的 Kernel MUST通过唯一 Operation Evaluator 完成一次 Actor/Tick Evaluate。Evaluator MUST按固定顺序协调 ingress、GameplayEffect advance、input request、Decision Timeline、Root control flow、Motion resolution、GameplayEffect save、Blackboard cleanup 和输出收集，并返回唯一 staged state、Motion request、GameplayFact、PresentationCommand 与 Trace。领域模块 MUST不建立第二 Evaluate loop、独立 Tick 或跨 Tick mutable state。

#### Scenario: Float32 Local Tick

- **WHEN** SimulationKernel 对 Corin 执行一个 Float32 Evaluate
- **THEN** MUST只创建一个 Float32 evaluation transaction
- **AND** RootTree、nested StateMachine、Timeline、Action、Blackboard 与 GE MUST在该事务中按正式顺序推进
- **AND** 任一模块失败时 MUST不返回部分 staged state 或部分外部输出

### Requirement: Operation 领域模块必须拥有明确输出权限

Operation runtime MUST将 Value/Input、Blackboard、Action、Timeline、GameplayEffect bridge 与 Motion accumulation 分配给明确模块。模块 MUST只通过窄 state/query/sink port 协作，不得取得万能 mutable context 或另一个模块的具体实现。portable control runtime MUST不产生 Animation、Camera、Cue、GameplayEffect、Motion 或 Network 输出；Timeline 与 Locomotion MUST不直接生成最终 WorldSolver result。

#### Scenario: Timeline 采样攻击动画和位移

- **WHEN** Timeline Leaf 在当前 Tick 采样 Animation producer 与 MotionCurve
- **THEN** Timeline module MUST向 Presentation sink 提交 producer command
- **AND** MUST向 Motion sink 提交 contribution
- **AND** MUST不修改 Runnable child cursor、最终 BodyState 或直接调用 WorldSolver

### Requirement: Operation topology 必须是 Program 的一次性只读运行索引

系统 MAY从已校验 Target Program 建立不含 numeric payload 的 operation execution topology，用于 Root、operation code、control-flow edge、reference 和 semantic slot 查找。Topology MUST按 Program 实例构建一次并由 Session 复用，MUST不在每 Actor/Tick 重建，MUST不序列化为第二份 Program，不参与 ProgramHash/LayoutHash/StateHash/EventId，也 MUST不在 Program 缺失或不匹配时作为 fallback。

#### Scenario: 两个 Actor 使用同一 Corin Program

- **WHEN** 同一 Session 的两个 Actor 绑定同一 Program
- **THEN** 两者 MUST复用同一 immutable operation topology
- **AND** 各自 mutable execution state MUST仍只存在于各自 CharacterSimulationState

#### Scenario: Topology 与 Program 不匹配

- **WHEN** topology 中的 operation、edge、reference 或 slot index 与 Program 不一致
- **THEN** layout/composition MUST在 Evaluate 前失败
- **AND** MUST不重建近似 topology 或回退运行时字符串查找

### Requirement: Operation dispatch 必须保持封闭和无 fallback

Runtime MUST对当前 operation-set version 的每个 operation code 建立唯一 control 或 Target Leaf owner。Dispatch MAY使用明确 switch 或等价的静态封闭映射，但 MUST不使用 reflection、运行时 handler discovery、按字符串 registry 或缺失 handler fallback。未知、重复或未实现 operation code MUST明确失败。

#### Scenario: Target 缺少 Leaf backend

- **WHEN** Program 包含当前 Target 未实现的 versioned Leaf operation
- **THEN** Target build/composition 或 Evaluate MUST明确失败并报告 operation identity
- **AND** MUST不跳过 operation、返回 Success 或搜索另一个 runtime handler

### Requirement: Operation Trace 必须与 Gameplay State 隔离

Operation control、Target leaf 与 Finalize 产生的 Trace MUST使用独立 diagnostics local sequence。Trace MUST不读取或写入 Gameplay/Presentation `FactSequence`，MUST不改变 CharacterStateHash、Snapshot bytes 或后续 GameplayFact/PresentationCommand EventId。关闭、增加或删除 Trace 只允许改变 diagnostics 输出。

#### Scenario: 关闭 operation Trace channel

- **WHEN** 同一 Program、Input 和初始 State 在关闭 Trace 后执行相同 Tick
- **THEN** staged Character state、Motion、GameplayFact 与 PresentationCommand MUST与开启 Trace 时相同
- **AND** 只有 Trace 集合与其 diagnostics identity MAY不同

### Requirement: Operation scope completion 必须覆盖全部停止路径

自然完成、graceful stop 与 force stop MUST在重置 operation local state 前完成该 activation 的 operation-owned scope。GraphInstance scope MUST不跨 activation 保留 owner、generation 或 value；State scope 继续由正式 State exit lifecycle 清理。

#### Scenario: LowerPriority 打断运行中的 Graph scope

- **WHEN** Selector 通过 LowerPriority graceful stop 替换一个持有 GraphInstance Blackboard scope 的运行 child
- **THEN** portable control runtime MUST在 replacement activation 前请求 Target 完成旧 child scope
- **AND** 新 activation MUST不读取旧 generation 的 GraphInstance value

### Requirement: Program 级执行服务不得每 Tick 重建

operation SourceMap index、Timeline compiled curve 与 state-access policy 等只依赖 Program/Layout 的执行数据 MUST随 ProgramExecutionLayout 构建一次并复用。Actor workspace MAY复用临时集合，但每次 Evaluate MUST清空，MUST不保存 Gameplay 状态或跨 Actor 共享可变事务数据。

#### Scenario: 同一 Actor 连续执行两个 Tick

- **WHEN** 两个 Tick 使用同一 ProgramExecutionLayout
- **THEN** MUST复用相同 immutable Program execution services
- **AND** 第二个 Tick MUST不观察到第一个 Tick 的临时 Fact、Trace、Value recursion 或 Motion contribution

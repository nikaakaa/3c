## MODIFIED Requirements

### Requirement: SimulationKernel 必须分离 Evaluate 与 Finalize

SimulationKernel MUST提供无外部副作用的Evaluate与Finalize。Evaluate MUST只接收NumericProfile完全匹配的CharacterSimulationProgram、CharacterSimulationInput、committed CharacterSimulationState、SimulationIngress、SimulationTick和上一Tick body observation，通过Program锁定的代码控制与技能执行服务创建当前Actor/Step唯一State Transaction，并输出持有该未提交transaction的PendingCharacterEvaluation与WorldRequest。Finalize MUST只接收同一target ABI、Program/Layout、Actor和Tick的pending evaluation及精确匹配的WorldSolverResult，继续写入同一transaction并在成功时输出新committed CharacterSimulationState与`SimulationActorTickResult`。Kernel MUST不读取Unity Time、Camera、InputAction、Transport、Network packet或Presentation object。

#### Scenario: Local Session 推进一个角色

- **WHEN** Standard Local Pipeline为当前Actor提交SimulationTick与portable input
- **THEN** Evaluate MUST产生未提交transaction与world request
- **AND** Finalize MUST等待匹配world result后才Commit新状态并产生输出


### Requirement: Character State 必须通过单一 Target Transaction推进

每个Actor的每个SimulationStep MUST以当前committed `CharacterSimulationState`为只读基线创建一个target-specific State Transaction。C#角色控制、技能解释器、Program Evaluate与Program Finalize MUST读写同一个transaction；WorldSolver MUST只消费WorldRequest并不得访问transaction。Transaction MUST在Finalize全部校验和输出构造成功后恰好Commit一次，失败时MUST Abort且不得修改base state。Transaction MUST NOT进入Snapshot、History、Network payload、Pipeline participant state或Presentation。

#### Scenario: Evaluate与Finalize共享写集

- **WHEN** Actor在Evaluate中消费Dodge request并由WorldSolver返回匹配结果
- **THEN** Finalize MUST在同一个未提交transaction中读取已消费request和Action state
- **AND** Finalize成功后MUST只生成一份新的committed Character State

#### Scenario: WorldResult不匹配

- **WHEN** Finalize收到Actor、Tick、RequestId或SolverId不匹配的WorldResult
- **THEN** State Transaction MUST Abort
- **AND** base Character State与Pipeline正式working world MUST保持不变


### Requirement: Operation Evaluate 必须只有一个事务入口

每个Numeric Target的Kernel MUST通过唯一Evaluate入口，按固定顺序协调typed ingress、GE推进、输入、技能Decision candidate、C#显式控制StateMachine与独立动作请求／生命周期、技能Tree／Timeline执行、Motion合成、GE保存、作用域清理与输出收集。控制State／Transition与技能解释 MUST使用同一SimulationTick及Actor／Tick transaction，状态转换和技能激活 MUST不互相隐式触发。领域模块不得建立第二Evaluate loop、独立Tick或状态镜像，代码控制不得重新发射为角色RootTree。

#### Scenario: 角色控制与技能共同推进

- **WHEN** Local或Replay请求当前Actor的一个Tick
- **THEN** MUST只创建一个Target transaction并在其中执行控制代码与技能
- **AND** 世界求解 MUST仍通过唯一batch，任一模块失败不得发布部分结果

#### Scenario: Float32 Local Tick

- **WHEN** Kernel对Corin执行一个Float32 Evaluate
- **THEN** MUST只创建一个Float32 evaluation transaction
- **AND** C#控制State／Transition、技能局部StateMachine／Timeline、Action、变量和GE MUST在同一事务按正式顺序推进
- **AND** 失败不得返回部分staged state或外部输出


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
#### Scenario: 多个实例共享技能定义

- **WHEN** 多个Actor或ActionInstance引用相同Skill Program
- **THEN** 只读操作／曲线／来源索引 MUST按Program复用
- **AND** 各实例 MUST只持有独立执行状态，不重建作者图

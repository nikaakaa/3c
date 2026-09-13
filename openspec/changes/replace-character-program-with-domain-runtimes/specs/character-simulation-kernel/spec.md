## MODIFIED Requirements

### Requirement: SimulationKernel 必须分离 Evaluate 与 Finalize

SimulationKernel MUST提供无外部副作用的Evaluate与Finalize。Evaluate MUST只接收NumericProfile完全匹配的角色领域运行绑定、CharacterSimulationInput、committed CharacterSimulationState、SimulationIngress、SimulationTick和上一Tick body observation，创建当前Actor/Step唯一State Transaction，并输出持有该未提交transaction的PendingCharacterEvaluation与WorldRequest。Finalize MUST只接收同一target ABI、玩法内容／状态格式、Actor和Tick的pending evaluation及精确匹配的WorldSolverResult，继续写入同一transaction并在成功时输出新committed CharacterSimulationState与`SimulationActorTickResult`。Kernel MUST不读取Unity Time、Camera、InputAction、Transport、Network packet或Presentation object。

#### Scenario: Local Session 推进一个角色

- **WHEN** Standard Local Pipeline为当前Actor提交SimulationTick与portable input
- **THEN** Evaluate MUST产生未提交transaction与world request
- **AND** Finalize MUST等待匹配world result后才Commit新状态并产生输出


### Requirement: Character State 必须通过单一 Target Transaction推进

每个Actor的每个SimulationStep MUST以当前committed `CharacterSimulationState`为只读基线创建一个target-specific State Transaction。角色 Evaluate与角色 Finalize MUST读写同一个transaction；WorldSolver MUST只消费WorldRequest并不得访问transaction。Transaction MUST在Finalize全部校验和输出构造成功后恰好Commit一次，失败时MUST Abort且不得修改base state。Transaction MUST NOT进入Snapshot、History、Network payload、Pipeline participant state或Presentation。

#### Scenario: Evaluate与Finalize共享写集

- **WHEN** Actor在Evaluate中消费Dodge request并由WorldSolver返回匹配结果
- **THEN** Finalize MUST在同一个未提交transaction中读取已消费request和Action state
- **AND** Finalize成功后MUST只生成一份新的committed Character State

#### Scenario: WorldResult不匹配

- **WHEN** Finalize收到Actor、Tick、RequestId或SolverId不匹配的WorldResult
- **THEN** State Transaction MUST Abort
- **AND** base Character State与Pipeline正式working world MUST保持不变


### Requirement: Committed Character State 必须使用类型化不可变存储

已提交角色状态 MUST按领域分区保存，Control、Input、Ability、Effect、Equipment 与跨 Tick Motion 各有唯一格式和 owner。运行模块 MUST通过正式 typed 状态合同读写同一个角色 Step 事务，不得使用任意对象反射、opaque bytes 或 mutable dictionary 代替业务状态。Commit MUST复用未修改分区并只提交本次有效写集，不固定复制全角色状态；角色 MUST不依赖 Program State Layout。

#### Scenario: 当前Tick只修改少量状态

- **WHEN** Actor只推进Runnable cursor、Timeline time和FactSequence
- **THEN** Commit MUST复用其它未修改state pages与GameplayEffect aggregate
- **AND** MUST NOT遍历并复制全部全角色状态作为Builder快照


### Requirement: Simulation Session 必须锁定 ProgramCatalog 与 Actor roster

Session Pipeline Runtime MUST在启动前接收完整 GameplayContentCatalog与 ordered Actor roster，并校验每个 ActorId的 GameplayContentId、StateSchemaHash与 World body binding。Session Active后 Catalog与 roster MUST不可变；Ingress/Schedule产品只能为已有 Actor提交 input/ingress，不能隐式 spawn、despawn、替换玩法内容或加入未知 Actor。

#### Scenario: Schedule 提交未知 Actor 输入

- **WHEN** ExecutionPlan包含不在锁定 roster中的 ActorId
- **THEN** 当前 outer Tick MUST在 Step阶段前失败
- **AND** MUST不自动创建默认 Character state、World body或 registration


### Requirement: SimulationWorldSnapshot 必须原子 Capture 与 Restore

Session snapshot MUST聚合GameplayContentCatalogHash、每Actor 领域运行绑定、BackendId/version、PipelineId/Hash、Pipeline state participant identity、State codec identity、Solver/world identity、SimulationTick、stable roster、全部committed CharacterSimulationState canonical bytes、WorldSimulationState与需要回滚的Pipeline state。Capture MUST只编码committed typed state，不得读取active State Transaction。Restore MUST在step loop开始前校验并原子替换完整working world，MUST不只恢复Transform、单Actor、部分Pass、部分领域aggregate或未提交transaction。

#### Scenario: 恢复 Attack2 中的双 Actor Pipeline world

- **WHEN** Schedule Plan请求恢复一个ActorA正在Attack2、ActorB正在移动且包含合法Pipeline participant状态的snapshot
- **THEN** 两个typed Character state、World state与Pipeline state MUST在同一restore transaction中恢复
- **AND** 任一payload、codec identity或PipelineHash失败时当前正式world MUST保持不变


### Requirement: State Hash 必须区分 Character 与 World 有效性

系统 MUST提供CharacterStateHash与SimulationWorldHash。CharacterStateHash MUST覆盖GameplayContentHash、NumericProfile、Target ABI、Character layout、State codec identity与canonical committed Character state bytes；MUST不覆盖active transaction、evaluation workspace或同Step transient motion。WorldHash MUST再覆盖GameplayContentCatalogHash、全部Actor binding、BackendId/semantic version、PipelineHash、Pipeline snapshot participant state、Solver identity/version、world revision、SimulationTick、stable roster与WorldSimulationState。只有Program Runtime、Backend、Pipeline全部Pass、Catalog全部Program与Solver都声明DeterministicReplay时，WorldHash MAY被声明为跨机器确定性判定。

#### Scenario: Unity Solver 产生本地 WorldHash

- **WHEN** Local Session使用Float32 Pass Backend与UnityCharacterControllerWorldSolver
- **THEN** 系统 MAY生成本地capture一致性hash
- **AND** diagnostics MUST标记该WorldHash不具备跨机器deterministic validity


### Requirement: Simulation Session 必须锁定完整 Numeric Target 组合

一个 Session的 GameplayContentCatalog、Program Runtime、Kernel specialization、CharacterSimulationInput、CharacterSimulationState、WorldRequest/Result、GameplayFact、Snapshot codec、Execution Backend、Pipeline Pass与 WorldSolver MUST使用兼容 NumericProfile、Target ABI与 operation-set version。Target-specific Composer MUST在创建 runtime handle前完成 GameplayContentHash、StateSchemaHash、PipelineHash、Backend、roster、initial state、Source port、Solver capability与 codec identity校验；公共 Host MUST不按 Source、Network Model、Actor、Pass、Graph operation、packet或 Tick切换数值 backend。当前 Local组合 MUST只使用 Float32 Program Runtime与 Float32 Pass Backend。

#### Scenario: Float Program 误配 Fixed Backend

- **WHEN** Float32 GameplayContentCatalog、Fixed Execution Backend或错误 ABI Pass被提交给同一 Composer
- **THEN** composition MUST在首 Tick前失败并报告各组成部分 identity
- **AND** MUST不量化 product、转换 state、包装 object adapter或选择默认 Backend/Solver


### Requirement: Execution Backend 必须按 Pipeline 事务原子推进零到多个 Step

Execution Backend MUST通过 portable Pipeline Transaction coordinator 先运行 Ingress和唯一 Schedule producer，再按 ExecutionPlan可选 restore并执行零到多个 ordered Step。每个标准 Step MUST按 compiled phase order执行全部 Step Pass，其中 MUST存在按 stable ActorId order执行的唯一 角色 Evaluate、一次 World ResolveBatch与唯一 角色 Finalize核心锚点，且三个锚点 MUST依次排列。附加 Step Pass MAY依照 descriptor顺序和 Product依赖在核心锚点前后执行，但 MUST在 completed step与 Pipeline projection冻结前完成；portable Core与 Target port MUST不硬编码具体 Network Model的附加 Pass identity。多个 replay step MUST只推进 working state。全部 Step与 Egress成功后 coordinator MUST原子发布最终 Character/World/Pipeline state并 Commit外部输出。任一阶段失败时 MUST不发布部分 working state或副作用。Float32 与 Fixed MAY使用不同 typed transaction port、working state、snapshot codec 和 World request/result ABI，但 MUST不复制阶段顺序、失败回滚、publish 或 commit 规则。

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
- **THEN** coordinator MUST在同一 Step内按 compiled order先执行 角色 Finalize再执行 History
- **AND** History状态 MUST在 completed step与 Pipeline projection捕获前完成更新
- **AND** Fixed Target port与 portable Core MUST不把 Rollback History当作第四个核心阶段或硬编码其 Pass identity


### Requirement: Operation topology 必须是 Program 的一次性只读运行索引

系统 MAY从已校验 Target Program 建立不含 numeric payload 的 operation execution topology，用于 Root、operation code、control-flow edge、reference 和 semantic slot 查找。Topology MUST按 Program 实例构建一次并由 Session 复用，MUST不在每 Actor/Tick 重建，MUST不序列化为第二份 Program，不参与 GameplayContentHash/StateSchemaHash/StateHash/EventId，也 MUST不在 Program 缺失或不匹配时作为 fallback。

#### Scenario: 两个 Actor 使用同一 Corin Program

- **WHEN** 同一 Session 的两个 Actor 绑定同一 Program
- **THEN** 两者 MUST复用同一 immutable operation topology
- **AND** 各自 mutable execution state MUST仍只存在于各自 CharacterSimulationState

#### Scenario: Topology 与 Program 不匹配

- **WHEN** topology 中的 operation、edge、reference 或 slot index 与 Program 不一致
- **THEN** layout/composition MUST在 Evaluate 前失败
- **AND** MUST不重建近似 topology 或回退运行时字符串查找


### Requirement: Program 级执行服务不得每 Tick 重建

operation topology、SourceMap index、Timeline compiled curve/segment lookup、GameplayEffect descriptor/index、state-access policy、immutable roster 与 stable Actor order 等只依赖 玩法内容／状态格式/Session composition 的执行数据 MUST分别随 ProgramExecutionServices或 Session execution layout 构建一次并复用，MUST不在每 Actor/Tick/replay step 重建。Session 与 Actor workspace MAY复用临时集合和容量，但每次 outer transaction或 Evaluate MUST按 owner 清空，MUST不保存 Gameplay 状态或跨 Actor 共享可变事务数据。Snapshot、history、published state、egress output 和持久 diagnostics 在越过事务边界前 MUST冻结或复制，不得持有下一 Tick 会重置的 workspace memory。

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


### Requirement: ProgramExecutionLayout必须预解析Tick热路径静态查询

ProgramExecutionLayout MUST在Program Runtime composition时一次性构建按operation索引的连续Value input span、紧凑Timeline operation集合、Timeline child owner、State所属StateMachine/execution owner、固定语义edge、operation reference和named constant索引。Float32与Fixed Runtime MUST复用各自角色玩法的immutable layout。正常Tick MUST不为这些查询遍历全部Program operation、解析端口字符串、建立端口HashSet、按字符串排序或执行LINQ materialization。Layout MUST只缓存路由，不得缓存依赖mutable state的Value结果。

#### Scenario: 同一Condition跨Tick求值

- **WHEN** 同一Condition在连续Tick读取相同Value graph
- **THEN** Runtime MUST复用同一immutable input span
- **AND** 每次读取 MUST仍按当前transaction state重新求值source operation

#### Scenario: Program扩张但active路径不变

- **WHEN** Program增加不活跃的State、Timeline或Value operation
- **THEN** 当前Tick查找active Timeline、State execution owner和Timeline child owner MUST不重新扫描新增operation
- **AND** 静态关系 MUST只增加composition时layout构建成本

#### Scenario: Layout关系不唯一

- **WHEN** Timeline child有多个owner、State owner无法唯一解析或binding table不canonical
- **THEN** Program Runtime composition MUST失败
- **AND** MUST不在Tick内搜索近似owner或使用SourceMap字符串fallback


### Requirement: Kernel Program Binding必须与共享Program Layout分离

ProgramExecutionLayout与ProgramExecutionServices MUST只持有GameplayContentId、GameplayContentHash、StateSchemaHash、OperationSetVersion和NumericProfile等Program固有身份。具体Kernel backend MUST由Program Runtime创建独立`KernelProgramBinding`，并在Session运行前一次性验证Program、Layout、NumericProfile、Operation Set与backend完整性。同一Program MAY在不同合法Pipeline、Source、Solver或Network Model中复用同一Layout。Evaluate与Finalize MUST只执行O(1) binding identity或引用校验，MUST不重新枚举Program operation。

#### Scenario: 同一Float32 Program用于Local与Authority

- **WHEN** Local Session与Unity Authority Session绑定同一Float32 Program
- **THEN** 两者 MAY复用同一ProgramExecutionLayout
- **AND** 各自 MUST拥有匹配自身Kernel specialization的binding，Layout MUST不被第一个backend改写

#### Scenario: Evaluate收到另一Kernel的Pending

- **WHEN** Finalize收到Actor、Tick、Program、Layout或Kernel binding不匹配的Pending
- **THEN** Kernel MUST明确失败并Abort该transaction
- **AND** MUST不通过backend字符串搜索另一个workspace或重新验证整张Program


### Requirement: Evaluate与Finalize必须通过唯一Actor Output Lease冻结结果

每个Actor/SimulationTick MUST从Evaluate开始持有唯一output workspace lease直到Finalize成功或Abort。Pending evaluation MUST只保存Actor、Tick、玩法内容／状态格式/Kernel binding、lease generation、State Transaction与WorldRequest，不得拥有Facts、Presentation Commands或Trace副本。World ResolveBatch MUST只读取WorldRequest。Finalize MUST在同一workspace追加后置输出，并在正式`SimulationActorTickResult`边界恰好冻结一次。Snapshot、History、Network、Diagnostics与Presentation MUST只消费最终immutable result或在自己的持久边界复制数据。

#### Scenario: Evaluate等待WorldSolver

- **WHEN** Evaluate完成且World ResolveBatch尚未返回
- **THEN** output builders MUST仍由该Actor lease唯一持有
- **AND** 同Actor MUST不能开始下一次Evaluate或让Pending复制builders


## ADDED Requirements

### Requirement: Kernel必须通过领域接口执行角色而非整体操作表

Evaluate／Finalize MUST保留同一 actor／Tick 事务及原业务顺序，通过控制、技能、运动、效果和装备的正式接口运行。只有技能内部使用自己的执行数据，Kernel MUST不解释整角色操作表，不让 Source、Pass 或 Solver 拥有第二份角色状态。

#### Scenario: 控制与技能共同产生运动
- **WHEN** 当前 Tick 的控制与活动技能各提交运动贡献
- **THEN** 同一 Evaluate MUST按原仲裁生成唯一请求，Finalize 等待对应世界结果后一次提交

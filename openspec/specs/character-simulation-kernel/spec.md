# character-simulation-kernel Specification

## Purpose
定义 Numeric Target 专属 SimulationKernel 的 Evaluate/Finalize、Character/World state、Session 四阶段执行、Snapshot 和稳定 EventId 输出合同。
## Requirements
### Requirement: SimulationKernel 必须分离 Evaluate 与 Finalize

SimulationKernel MUST提供无外部副作用的Evaluate与Finalize。Evaluate MUST只接收NumericProfile完全匹配的CharacterSimulationProgram、CharacterSimulationInput、committed CharacterSimulationState、SimulationIngress、SimulationTick和上一Tick body observation，创建当前Actor/Step唯一State Transaction，并输出持有该未提交transaction的PendingCharacterEvaluation与WorldRequest。Finalize MUST只接收同一target ABI、Program/Layout、Actor和Tick的pending evaluation及精确匹配的WorldSolverResult，继续写入同一transaction并在成功时输出新committed CharacterSimulationState与`SimulationActorTickResult`。Kernel MUST不读取Unity Time、Camera、InputAction、Transport、Network packet或Presentation object。

#### Scenario: Local Session 推进一个角色

- **WHEN** Standard Local Pipeline为当前Actor提交SimulationTick与portable input
- **THEN** Evaluate MUST产生未提交transaction与world request
- **AND** Finalize MUST等待匹配world result后才Commit新状态并产生输出

### Requirement: Character State 必须通过单一 Target Transaction推进

每个Actor的每个SimulationStep MUST以当前committed `CharacterSimulationState`为只读基线创建一个target-specific State Transaction。Program Evaluate与Program Finalize MUST读写同一个transaction；WorldSolver MUST只消费WorldRequest并不得访问transaction。Transaction MUST在Finalize全部校验和输出构造成功后恰好Commit一次，失败时MUST Abort且不得修改base state。Transaction MUST NOT进入Snapshot、History、Network payload、Pipeline participant state或Presentation。

#### Scenario: Evaluate与Finalize共享写集

- **WHEN** Actor在Evaluate中消费Dodge request并由WorldSolver返回匹配结果
- **THEN** Finalize MUST在同一个未提交transaction中读取已消费request和Action state
- **AND** Finalize成功后MUST只生成一份新的committed Character State

#### Scenario: WorldResult不匹配

- **WHEN** Finalize收到Actor、Tick、RequestId或SolverId不匹配的WorldResult
- **THEN** State Transaction MUST Abort
- **AND** base Character State与Pipeline正式working world MUST保持不变

### Requirement: Committed Character State 必须使用类型化不可变存储

Committed `CharacterSimulationState` MUST按Program State Layout保存类型化、不可变的state partitions。Runtime领域模块 MUST通过预验证typed address读写transaction，不得以opaque bytes、runtime decode cache、mutable object dictionary或字符串owner查找保存Gameplay状态。State Commit MUST复用未修改partition/page，并只冻结dirty write-set；不得为每个Tick固定复制全部StateSlot两次。

#### Scenario: 当前Tick只修改少量状态

- **WHEN** Actor只推进Runnable cursor、Timeline time和FactSequence
- **THEN** Commit MUST复用其它未修改state pages与GameplayEffect aggregate
- **AND** MUST NOT遍历并复制全部Program StateSlot作为Builder快照

### Requirement: Simulation Session 必须锁定 ProgramCatalog 与 Actor roster

Session Pipeline Runtime MUST在启动前接收完整 SimulationProgramCatalog与 ordered Actor roster，并校验每个 ActorId的 ProgramId、LayoutHash与 World body binding。Session Active后 Catalog与 roster MUST不可变；Ingress/Schedule产品只能为已有 Actor提交 input/ingress，不能隐式 spawn、despawn、换 Program或加入未知 Actor。

#### Scenario: Schedule 提交未知 Actor 输入

- **WHEN** ExecutionPlan包含不在锁定 roster中的 ActorId
- **THEN** 当前 outer Tick MUST在 Step阶段前失败
- **AND** MUST不自动创建默认 Character state、World body或 registration

### Requirement: Character 与 World 状态必须分属不同 owner

CharacterSimulationState MUST只保存单Actor且会影响当前Commit后或未来SimulationTick的类型化Gameplay逻辑状态；同Step的MotionContribution、MotionAccumulator、PendingWorldRequest、输出staging与State Transaction MUST不进入committed Character State。WorldSimulationState MUST保存ordered body state、solver-owned mutable state、world revision与static world identity。影响未来Pipeline执行的Pass状态 MUST进入独立SimulationPipelineStateSnapshot或正式reconstruct合同；Session Source external state与Presentation state MUST不进入Character/World状态容器。

#### Scenario: Player transition继续推进

- **WHEN** 显式Player或BlendStack transition在两个SimulationTick之间推进
- **THEN** CharacterSimulationState、WorldSimulationState与Pipeline Gameplay state MUST不改变

#### Scenario: Evaluate生成当前Step位移请求

- **WHEN** Timeline和Locomotion在Evaluate中形成CharacterMotionRequest
- **THEN** request MUST只进入当前PendingCharacterEvaluation与WorldSolve产品
- **AND** MUST不进入committed Character State或Snapshot

### Requirement: SimulationWorldSnapshot 必须原子 Capture 与 Restore

Session snapshot MUST聚合ProgramCatalogHash、每Actor Program binding、BackendId/version、PipelineId/Hash、Pipeline state participant identity、State codec identity、Solver/world identity、SimulationTick、stable roster、全部committed CharacterSimulationState canonical bytes、WorldSimulationState与需要回滚的Pipeline state。Capture MUST只编码committed typed state，不得读取active State Transaction。Restore MUST在step loop开始前校验并原子替换完整working world，MUST不只恢复Transform、单Actor、部分Pass、部分领域aggregate或未提交transaction。

#### Scenario: 恢复 Attack2 中的双 Actor Pipeline world

- **WHEN** Schedule Plan请求恢复一个ActorA正在Attack2、ActorB正在移动且包含合法Pipeline participant状态的snapshot
- **THEN** 两个typed Character state、World state与Pipeline state MUST在同一restore transaction中恢复
- **AND** 任一payload、codec identity或PipelineHash失败时当前正式world MUST保持不变

### Requirement: State Hash 必须区分 Character 与 World 有效性

系统 MUST提供CharacterStateHash与SimulationWorldHash。CharacterStateHash MUST覆盖ProgramHash、NumericProfile、Target ABI、Character layout、State codec identity与canonical committed Character state bytes；MUST不覆盖active transaction、evaluation workspace或同Step transient motion。WorldHash MUST再覆盖ProgramCatalogHash、全部Actor binding、BackendId/semantic version、PipelineHash、Pipeline snapshot participant state、Solver identity/version、world revision、SimulationTick、stable roster与WorldSimulationState。只有Program Runtime、Backend、Pipeline全部Pass、Catalog全部Program与Solver都声明DeterministicReplay时，WorldHash MAY被声明为跨机器确定性判定。

#### Scenario: Unity Solver 产生本地 WorldHash

- **WHEN** Local Session使用Float32 Pass Backend与UnityCharacterControllerWorldSolver
- **THEN** 系统 MAY生成本地capture一致性hash
- **AND** diagnostics MUST标记该WorldHash不具备跨机器deterministic validity

### Requirement: World Solver 必须批量解决世界约束

ICharacterWorldSolver MUST只接收同一 NumericProfile 的 WorldSimulationState、WorldSolveBatchRequest 和 Tick context，并一次返回同一 target ABI 的 WorldSolveBatchResult 与新 WorldSimulationState。每个 request MUST按 ActorId 与 request identity 精确匹配一个 result。Solver MUST不读取 Graph、Action、Timeline、Network Model、server tick、ack 或 correction packet。

#### Scenario: Unity Solver 处理单 Actor batch

- **WHEN** Local Session 的 batch 只有 Corin 一个 request
- **THEN** Unity adapter MAY在内部调用一次 CharacterController.Move
- **AND** MUST通过同一 batch result 合同返回 portable body result

#### Scenario: Solver 声明 ActorCollision

- **WHEN** Composition要求 `WorldFeature.ActorCollision` 且当前 batch包含多个 Active Actor
- **THEN** Solver MUST在一次 batch resolve 中共同求解全部 Actor pair
- **AND** MUST原子返回全部匹配 result 与唯一新 WorldSimulationState
- **AND** MUST不提交中间 body、让 Character Host 二次修正或让 Presentation 执行碰撞

### Requirement: World Solver 必须声明真实恢复与确定性能力

WorldSolver MUST显式声明 NumericProfile、ABI version、Reconstructible、Snapshotable、DeterministicReplay与实际 world feature。Program/Pass capability union或 Source/Backend requirement未满足时 composition MUST创建失败。系统 MUST不因 Solver返回量化 result就自动声明 DeterministicReplay。

#### Scenario: Rollback Pipeline 尝试使用 Unity Solver

- **WHEN** 后续 Pipeline要求 Snapshotable与 DeterministicReplay
- **AND** Unity Solver只声明 Reconstructible
- **THEN** composition MUST拒绝创建
- **AND** MUST不降级为近似 replay或删除相关 Pass

### Requirement: Simulation Session 必须锁定完整 Numeric Target 组合

一个 Session的 ProgramCatalog、Program Runtime、Kernel specialization、CharacterSimulationInput、CharacterSimulationState、WorldRequest/Result、GameplayFact、Snapshot codec、Execution Backend、Pipeline Pass与 WorldSolver MUST使用兼容 NumericProfile、Target ABI与 operation-set version。Target-specific Composer MUST在创建 runtime handle前完成 ProgramHash、LayoutHash、PipelineHash、Backend、roster、initial state、Source port、Solver capability与 codec identity校验；公共 Host MUST不按 Source、Network Model、Actor、Pass、Graph operation、packet或 Tick切换数值 backend。当前 Local组合 MUST只使用 Float32 Program Runtime与 Float32 Pass Backend。

#### Scenario: Float Program 误配 Fixed Backend

- **WHEN** Float32 ProgramCatalog、Fixed Execution Backend或错误 ABI Pass被提交给同一 Composer
- **THEN** composition MUST在首 Tick前失败并报告各组成部分 identity
- **AND** MUST不量化 product、转换 state、包装 object adapter或选择默认 Backend/Solver

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

### Requirement: CharacterSimulationInput 必须与设备和模型解耦

Kernel MUST只消费当前 NumericProfile的 portable CharacterSimulationInput。Input Adapter、Ingress Pass或具体 Session Source MUST在 Kernel外将 InputAction、Camera-relative方向或 canonical external command转换为稳定 InputId、target scalar/vector value、request、sequence与 source tick。Graph operation MUST不读取 Camera、InputAction、Pipeline Definition或 model packet。

#### Scenario: 相机相对移动

- **WHEN** Unity Input Adapter采样移动轴与 Camera yaw
- **THEN** Adapter MUST在 Ingress产品生成前产生 portable世界方向或 yaw
- **AND** Program operation MUST只读取该 input

### Requirement: SimulationIngress 必须只承载模型无关 Gameplay 事实

SimulationIngress MUST只承载 Core已声明的 typed Action lifecycle、GameplayResult、GameplayEffect lifecycle、Attribute value或其它模型无关 ingress contract，并带 ActorId、source tick、sequence与稳定 fact identity。Session Source/Ingress Pass MUST在进入 Step前移除 packet、authority metadata、endpoint与 transport类型。

#### Scenario: 服务端拒绝预测动作

- **WHEN** 后续 ServerAuthoritative Source收到 Action reject decision
- **THEN** 对应 Ingress Pass MUST将其转换为 typed ActionLifecycle ingress
- **AND** Kernel MUST不读取原始 ActionDecision packet

### Requirement: SimulationActorTickResult 必须通过稳定 EventId 提交副作用

Gameplay facts与 presentation commands MUST使用由 Program operation、ActorId、activation identity、SimulationTick与 local event sequence构成的稳定 EventId。Kernel MUST不播放动画、发送 packet或触发相机/VFX。Egress Pass MUST为外部事件生成带显式ActorId的Publish、Replace、Retire或 Suppress disposition；Execution Backend MUST核对本次EventId与Actor归属，并在 disposition与全部 working state校验后原子发布最终 state，再将 Plan交给 SimulationCommitter。需要跨Tick判断历史EventId的Egress MUST以SnapshotParticipant journal拥有该历史，Unity output adapter MUST不保存无界owner字典。

#### Scenario: Timeline 产生 Cue

- **WHEN** Timeline operation在当前 Step产生 Cue command
- **THEN** Finalize MUST输出带 EventId的 command
- **AND** Local Egress生成 Publish后只有 Committer MAY触发外部 Cue port

### Requirement: Gameplay State 发布与外部输出处置必须分离

Egress OutputDisposition MUST只控制外部 EventId生命周期，不得控制 Gameplay working state是否生效。Execution Backend MUST在全部内部 Step、Egress与 disposition校验成功后一次发布最终 CharacterSimulationState、WorldSimulationState与 Pipeline state；Committer MUST在 state publish后执行外部端口。Committer端口失败时 Session MUST fail-stop并报告精确 EventId，MUST不自动重试、伪造已触发副作用的回滚或继续下一 Tick。

#### Scenario: Egress 引用未知 EventId

- **WHEN** Egress对本次 plan不存在且历史未发布的 EventId生成 Replace或 Retire
- **THEN** Backend MUST在 state publish前拒绝当前 outer transaction
- **AND** Character/World/Pipeline正式 state MUST保持 outer Tick前值

### Requirement: Portable Core 必须由 Unity 与普通 DotNet 共享源集

Semantic IR、Target Compiler、operation-set、Program、Character/World state、Input/Output、Pipeline descriptor/product/execution-plan/snapshot contracts、runtime handle与 WorldSolver合同 MUST来自 canonical portable source set，并可由 Unity asmdef与普通 .NET csproj编译。Float32 Program Runtime、标准 Pass与 Pass Backend MUST由 Unity和普通 .NET Host共享源码；已安装的 Fixed差异只来自 Fixed Program Runtime、Target Pass实现、Backend与 Deterministic KCC Solver。系统 MUST不复制 server Kernel、网络专用 operation runtime、第二 Pipeline compiler或 Authoring业务模型。

#### Scenario: DotNet 项目引用 Core

- **WHEN** 后续普通 .NET Host引用 portable source
- **THEN** MUST编译同一 Program reader、Pipeline descriptor/compiler合同与 Kernel源码
- **AND** MUST不需要 UnityEngine、CharacterPipelineHost或 authoring asset

### Requirement: 确定性 Numeric Target 与 WorldSolver 必须作为独立完整能力实现

Portable Gameplay core MUST提供稳定 Semantic IR operation、Numeric Target extension、snapshot ownership 和 batch solve 形状，但 MUST不在 Float32 Kernel 内实现 Fixed arithmetic、KCC、DotRecast、Unity physics 或 Network Model。完整 deterministic replay MUST由具体 Model 同时装配 Fixed Numeric Target、匹配的 Program/Kernel/State/Snapshot ABI，以及声明 Snapshotable 与 DeterministicReplay 的 WorldSolver。

#### Scenario: Rollback 安装独立 Fixed Target 与 KCC

- **WHEN** DeterministicRollback组合创建 Fixed Program Runtime、Deterministic Backend与 Deterministic KCC
- **THEN** MUST使用与 Program NumericProfile、ABI和 capability匹配的 Fixed WorldSolver
- **AND** Local、ServerAuthoritative与 DotRecast组合 MUST继续使用各自 Float32 Program Runtime与 Solver
- **AND** Fixed与Float32组合 MUST通过不同的Composition与Player产品启动，不得在Active Session中切换

### Requirement: Structured Trace 不得受 Egress OutputDisposition 控制

Compiler、SimulationKernel、Pipeline Runtime/Pass、WorldSolver adapter、Session Source与 SimulationCommitter MUST在各自正式边界向只读 diagnostics sink发布 structured Trace。Trace MUST记录 PipelineHash、PassId、product、成功、失败、restore、replay与 OutputDisposition；Egress MUST不能通过 Publish、Replace、Retire或 Suppress隐藏或改写 Trace。Diagnostics MUST不反向改变 Character/World/Pipeline state或外部输出。

#### Scenario: Rollback replay 抑制重复 Cue

- **WHEN** 后续 Egress Pass对重复 Cue EventId生成 Suppress
- **THEN** Committer MUST不再次触发 Cue port
- **AND** Diagnostics MUST仍记录 replay Step、Pass、EventId与 Suppress disposition

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

### Requirement: Session Source 必须保持外部资源边界

Session Source MUST只拥有 source clock、local input/endpoint、packet/history等外部资源及其显式 ports。它 MUST不执行 Program operation、不调用 WorldSolver、不获得 mutable Character/World/Pipeline working state、不驱动 Presentation，也 MUST不在 Common Host中隐藏注入 Pass。Local与 Network Model Source的差异 MUST通过显式 Source Definition和所选 Pipeline Pass表达。

#### Scenario: Network Model 提供 Correction 数据

- **WHEN** 后续 Model Source收到 authoritative snapshot
- **THEN** 它 MUST通过声明的 Source port供 Ingress/Schedule Pass消费
- **AND** MUST不直接调用 Backend restore或修改 Character Transform

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

### Requirement: ProgramExecutionLayout必须预解析Tick热路径静态查询

ProgramExecutionLayout MUST在Program Runtime composition时一次性构建按operation索引的连续Value input span、紧凑Timeline operation集合、Timeline child owner、State所属StateMachine/execution owner、固定语义edge、operation reference和named constant索引。Float32与Fixed Runtime MUST复用各自Program的immutable layout。正常Tick MUST不为这些查询遍历全部Program operation、解析端口字符串、建立端口HashSet、按字符串排序或执行LINQ materialization。Layout MUST只缓存路由，不得缓存依赖mutable state的Value结果。

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

ProgramExecutionLayout与ProgramExecutionServices MUST只持有ProgramId、ProgramHash、LayoutHash、OperationSetVersion和NumericProfile等Program固有身份。具体Kernel backend MUST由Program Runtime创建独立`KernelProgramBinding`，并在Session运行前一次性验证Program、Layout、NumericProfile、Operation Set与backend完整性。同一Program MAY在不同合法Pipeline、Source、Solver或Network Model中复用同一Layout。Evaluate与Finalize MUST只执行O(1) binding identity或引用校验，MUST不重新枚举Program operation。

#### Scenario: 同一Float32 Program用于Local与Authority

- **WHEN** Local Session与Unity Authority Session绑定同一Float32 Program
- **THEN** 两者 MAY复用同一ProgramExecutionLayout
- **AND** 各自 MUST拥有匹配自身Kernel specialization的binding，Layout MUST不被第一个backend改写

#### Scenario: Evaluate收到另一Kernel的Pending

- **WHEN** Finalize收到Actor、Tick、Program、Layout或Kernel binding不匹配的Pending
- **THEN** Kernel MUST明确失败并Abort该transaction
- **AND** MUST不通过backend字符串搜索另一个workspace或重新验证整张Program

### Requirement: Character State Transaction必须单次复制并移交Dirty Page

Float32与Fixed Character State Transaction MUST使用Actor-owned、layout-indexed transaction workspace复用dirty metadata。一个transaction中每个dirty page MUST最多从base state复制一次；Commit MUST以明确take-ownership把WorkspaceOwned array移交给新的immutable page，并立即删除全部workspace可写引用。未修改page与partition MUST继续共享。Abort或Dispose MUST只释放仍由workspace拥有的数据，MUST不改变base state、前一committed state或已发布page。已发布page array MUST不回到可写池。

#### Scenario: 一个Tick多次写同一page

- **WHEN** 多个operation在同一transaction修改同一typed state page
- **THEN** 第一次写 MUST复制base page一次，后续写 MUST复用同一WorkspaceOwned array
- **AND** Commit MUST不再次clone该array

#### Scenario: Finalize失败

- **WHEN** WorldResult或Finalize validation失败且transaction Abort
- **THEN** base state与全部已提交历史 MUST保持不变
- **AND** 只有未发布workspace data MAY被清理或复用

#### Scenario: Commit发布新状态

- **WHEN** transaction成功Commit
- **THEN** 新state MUST只替换dirty page并复用其它page
- **AND** workspace MUST不再持有任何可修改新state的引用

### Requirement: Evaluate与Finalize必须通过唯一Actor Output Lease冻结结果

每个Actor/SimulationTick MUST从Evaluate开始持有唯一output workspace lease直到Finalize成功或Abort。Pending evaluation MUST只保存Actor、Tick、Program/Layout/Kernel binding、lease generation、State Transaction与WorldRequest，不得拥有Facts、Presentation Commands或Trace副本。World ResolveBatch MUST只读取WorldRequest。Finalize MUST在同一workspace追加后置输出，并在正式`SimulationActorTickResult`边界恰好冻结一次。Snapshot、History、Network、Diagnostics与Presentation MUST只消费最终immutable result或在自己的持久边界复制数据。

#### Scenario: Evaluate等待WorldSolver

- **WHEN** Evaluate完成且World ResolveBatch尚未返回
- **THEN** output builders MUST仍由该Actor lease唯一持有
- **AND** 同Actor MUST不能开始下一次Evaluate或让Pending复制builders

### Requirement: World Body垂直动力必须原子参与Simulation状态

Float32与Fixed `WorldBodyState` MUST独立保存`VerticalVelocity`，且 MUST保持actual `Velocity`为Solver applied displacement速度。Body Motion integration plan MUST只存在Evaluate到WorldSolve/Finalize的同Step transaction内；成功Finalize后只有committed VerticalVelocity进入NextWorldState。WorldState codec、Snapshot、Hash、restore、equality与WorldSolve request/result hash MUST覆盖VerticalVelocity；Abort MUST丢弃pending plan且不修改before state。系统 MUST不从actual Velocity.Y或Grounded推导缺失状态。当前Float32 WorldState/WorldSnapshot/SessionSnapshot codec MUST分别为v3/v3/v2，Fixed MUST分别为v3/v4/v3；旧payload MUST被拒绝。

#### Scenario: WorldSolve后续Actor失败

- **WHEN** 一个Actor已完成Body Motion Prepare但同一outer transaction的后续Actor或Pass失败
- **THEN** 全部pending integration plan MUST被丢弃
- **AND** committed WorldBodyState及VerticalVelocity MUST保持修改前值

#### Scenario: Snapshot恢复空中Actor

- **WHEN** Session原子恢复包含Airborne Actor的World Snapshot
- **THEN** Position、actual Velocity、VerticalVelocity、Grounded与Collision MUST同时恢复
- **AND** 下一次Evaluate MUST只读取恢复后的状态执行Prepare

#### Scenario: Finalize成功

- **WHEN** WorldResult匹配Pending并完成State Commit
- **THEN** pre-world与post-world输出 MUST在同一builder中形成最终集合
- **AND** Result构造 MUST是唯一一次跨workspace冻结

#### Scenario: Outer transaction中途失败

- **WHEN** 一个Actor已Evaluate而后续pass或另一Actor失败
- **THEN** outer Abort MUST释放全部尚未完成的lease并Abort对应transaction
- **AND** 下一次transaction MUST不观察到上一事务的Fact、Presentation或Trace

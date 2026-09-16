# character-simulation-kernel Specification

## Purpose

定义 Numeric Target 专属 Simulation Kernel 的 Evaluate/Finalize、Character/World state、Session roster、Snapshot、WorldSolver 与稳定 EventId 输出合同。Kernel 执行正式 domain runtime 和 Graph Runtime，不读取或发布整角色 Program。

## Requirements

### Requirement: SimulationKernel 必须分离 Evaluate 与 Finalize

SimulationKernel MUST 提供无外部副作用的 Evaluate 与 Finalize。Evaluate MUST 接收匹配 NumericProfile 的正式 Control/Ability domain input、CharacterSimulationInput、committed CharacterSimulationState、SimulationIngress、SimulationTick 和上一 Tick body observation，创建当前 Actor/Step 唯一 State Transaction，并输出持有未提交 transaction 的 PendingCharacterEvaluation 与 WorldRequest。Finalize MUST 接收同一 target、Actor 和 Tick 的 pending evaluation 及精确匹配的 WorldSolverResult，继续写入同一 transaction，并在成功时输出新的 committed CharacterSimulationState 与 `SimulationActorTickResult`。Kernel MUST 不读取 Unity Time、Camera、InputAction、Transport、Network packet 或 Presentation object。

#### Scenario: Local Session推进一个角色

- **WHEN** Standard Local Pipeline 为当前 Actor 提交 SimulationTick 与 portable input
- **THEN** Evaluate MUST 产生未提交 transaction 与 WorldRequest
- **AND** Finalize MUST 等待匹配 WorldSolverResult 后才 Commit 新状态并产生输出

### Requirement: Character State 必须通过单一 Target Transaction 推进

每个 Actor 的每个 SimulationStep MUST 以当前 committed `CharacterSimulationState` 为只读基线创建 target-specific State Transaction。Control、Ability、Effect、Equipment 与其它正式 domain Evaluate/Finalize MUST 读写同一个 transaction；WorldSolver MUST 只消费 WorldRequest，不得访问 transaction。Transaction MUST 在 Finalize 校验和输出构造成功后恰好 Commit 一次，失败时 MUST Abort 且不得修改 base state。Transaction MUST NOT 进入 Snapshot、History、Network payload、Pipeline participant state 或 Presentation。

#### Scenario: Evaluate与Finalize共享写集

- **WHEN** Actor 在 Evaluate 中消费 Dodge request 且 WorldSolver 返回匹配结果
- **THEN** Finalize MUST 在同一个未提交 transaction 中读取已消费 request 和 Action state
- **AND** 成功后 MUST 只生成一份新的 committed Character State

#### Scenario: WorldResult不匹配

- **WHEN** Finalize 收到 Actor、Tick、RequestId 或 SolverId 不匹配的 WorldResult
- **THEN** State Transaction MUST Abort
- **AND** base Character State 与正式 working world MUST 保持不变

### Requirement: Committed Character State 必须使用类型化不可变存储

Committed `CharacterSimulationState` MUST 按各领域正式 state schema 保存类型化、不可变的 state partitions。Runtime domain module MUST 通过预验证 typed address 读写 transaction，不得以 opaque bytes、runtime decode cache、mutable object dictionary 或字符串 owner 查找保存 Gameplay 状态。State Commit MUST 复用未修改 partition/page，并只冻结 dirty write-set；不得每 Tick 固定复制全部领域状态两次。

#### Scenario: 当前Tick只修改少量状态

- **WHEN** Actor 只推进 Action cursor、Timeline time 和 FactSequence
- **THEN** Commit MUST 复用其它未修改 state pages 与 GameplayEffect aggregate
- **AND** MUST 不遍历并复制全部 Graph/domain state 作为快照

### Requirement: Simulation Session 必须锁定领域 binding 与 Actor roster

Session Pipeline Runtime MUST 在启动前接收完整领域 binding 与 ordered Actor roster，并校验每个 ActorId 的 Control、Ability、Timeline、Presentation、World body、Pipeline 和 Source identity。Session Active 后 binding 与 roster MUST 不可变；Ingress/Schedule 只能为已有 Actor 提交 input/ingress，不能隐式 spawn、despawn、替换 domain owner 或加入未知 Actor。

#### Scenario: Schedule提交未知Actor输入

- **WHEN** ExecutionPlan 包含不在锁定 roster 中的 ActorId
- **THEN** 当前 outer Tick MUST 在 Step 阶段前失败
- **AND** MUST 不创建默认 Character state、World body 或 registration

### Requirement: Character 与 World 状态必须分属不同 owner

CharacterSimulationState MUST 只保存单 Actor 且会影响未来 SimulationTick 的类型化 Gameplay logic state；同 Step 的 MotionContribution、MotionAccumulator、PendingWorldRequest、输出 staging 与 State Transaction MUST 不进入 committed Character State。WorldSimulationState MUST 保存 ordered body state、solver-owned state、world revision 与 static world identity。影响未来 Pipeline 执行的 Pass state MUST 进入独立 SimulationPipelineStateSnapshot 或正式 reconstruct 合同；Session Source external state 与 Presentation state MUST 不进入 Character/World state 容器。

#### Scenario: Evaluate生成当前Step位移请求

- **WHEN** Timeline 与 Control 在 Evaluate 中形成 CharacterMotionRequest
- **THEN** request MUST 只进入当前 PendingCharacterEvaluation 与 WorldSolve 产品
- **AND** MUST 不进入 committed Character State 或 Snapshot

### Requirement: SimulationWorldSnapshot 必须原子 Capture 与 Restore

Session Snapshot MUST 聚合 domain binding identity、BackendId/version、PipelineId/Hash、Pipeline participant identity、各领域 State codec identity、Solver/world identity、SimulationTick、stable roster、全部 committed CharacterSimulationState canonical bytes、WorldSimulationState 与需要回滚的 Pipeline state。Capture MUST 只编码 committed typed state，不得读取 active State Transaction。Restore MUST 在 Step loop 开始前校验并原子替换完整 working world，不得只恢复 Transform、单 Actor、部分 Pass、部分 aggregate 或未提交 transaction。

#### Scenario: 恢复双Actor攻击状态

- **WHEN** Schedule 请求恢复 Actor A 正在 Attack2、Actor B 正在移动且包含合法 Pipeline participant state 的 Snapshot
- **THEN** 两个 typed Character state、World state 与 Pipeline state MUST 在同一 restore transaction 中恢复
- **AND** 任一 payload、codec identity 或 PipelineHash 失败时正式 world MUST 保持不变

### Requirement: State Hash 必须覆盖正式 state 而不是瞬时 workspace

系统 MUST 提供 CharacterStateHash 与 SimulationWorldHash。CharacterStateHash MUST 覆盖各领域 binding identity、NumericProfile、Target ABI、Character state schema、State codec identity 与 canonical committed Character state bytes；MUST 不覆盖 active transaction、evaluation workspace 或同 Step transient motion。WorldHash MUST 再覆盖全部 Actor binding、BackendId/version、Pipeline participant state、Solver identity/version、world revision、SimulationTick、stable roster 与 WorldSimulationState。

#### Scenario: Local WorldHash用于一致性捕获

- **WHEN** Local Session 使用 Float32 domain runtime 与 Unity WorldSolver
- **THEN** 系统 MAY 生成本地 capture 一致性 hash
- **AND** diagnostics MUST 标记该 hash 不代表跨机器 deterministic validity

### Requirement: WorldSolver 必须批量解决世界约束

ICharacterWorldSolver MUST 只接收同一 NumericProfile 的 WorldSimulationState、WorldSolveBatchRequest 和 Tick context，并一次返回同一 target ABI 的 WorldSolveBatchResult 与新 WorldSimulationState。每个 request MUST 按 ActorId 与 request identity 精确匹配一个 result。Solver MUST 不读取 Graph、Action、Timeline、Network Model、server tick、ack 或 correction packet。

#### Scenario: UnitySolver处理单Actor batch

- **WHEN** Local Session 的 batch 只有 Corin 一个 request
- **THEN** Unity adapter MAY 在内部调用 CharacterController.Move
- **AND** MUST 通过同一 batch result 合同返回 portable body result

#### Scenario: Solver声明ActorCollision

- **WHEN** Composition 要求 `WorldFeature.ActorCollision` 且 batch 包含多个 Active Actor
- **THEN** Solver MUST 在一次 batch resolve 中共同求解全部 Actor pair
- **AND** MUST 原子返回全部匹配 result 与唯一新 WorldSimulationState
- **AND** MUST 不让 Character Host 二次修正或 Presentation 执行碰撞

### Requirement: WorldSolver 必须声明真实恢复与确定性能力

WorldSolver MUST 显式声明 NumericProfile、ABI version、Reconstructible、Snapshotable、DeterministicReplay 与实际 world feature。Domain/Pass capability union 或 Source/Backend requirement 未满足时 Composition MUST 创建失败。系统 MUST 不因 Solver 返回量化 result 就自动声明 DeterministicReplay。

#### Scenario: Rollback尝试使用UnitySolver

- **WHEN** 后续 Pipeline 要求 Snapshotable 与 DeterministicReplay，但 Unity Solver 只声明 Reconstructible
- **THEN** Composition MUST 拒绝创建
- **AND** MUST 不降级为近似 replay 或删除相关 Pass

### Requirement: Float32 与 Fixed 必须共享 Graph 和 domain 业务语义

Float32 与 Fixed MAY 使用不同数值表示、state codec 和 WorldSolver，但 MUST 共享同一 Graph artifact、Control/Ability/Timeline/Effect/Motion 生命周期、Action selection、facts 与 Session boundary。Target-specific runtime 只负责数值计算和 typed state representation；不得复制第二套 control flow 或业务规则，也不得把 Character、Ability、Timeline 或 Pose 编成 Target-specific 整角色 Program。

#### Scenario: 同一Graph在两个Numeric Target准备

- **WHEN** Float32 与 Fixed 为同一 Graph artifact 准备正式 domain runtime
- **THEN** 两个 Target MUST 保持相同节点、边、Action、Timeline 和 Effect 业务语义
- **AND** 差异 MUST 只存在数值表示、codec、Backend 与 Solver capability

### Requirement: CharacterSimulationInput 与设备和模型解耦

Kernel MUST 只消费当前 NumericProfile 的 portable CharacterSimulationInput。Input Adapter、Ingress Pass 或具体 Session Source MUST 在 Kernel 外将 InputAction、Camera-relative direction 或 canonical external command 转换为稳定 InputId、typed value、request、sequence 与 source tick。Graph operation MUST 不读取 Camera、InputAction、Pipeline Definition 或 model packet。

#### Scenario: 相机相对移动

- **WHEN** Unity Input Adapter 采样移动轴与 Camera yaw
- **THEN** Adapter MUST 在 Ingress 产品生成前产生 portable world direction 或 yaw
- **AND** Graph/domain operation MUST 只读取该 input

### Requirement: SimulationIngress 必须只承载模型无关 Gameplay 事实

SimulationIngress MUST 只承载 Core 已声明的 typed Action lifecycle、GameplayResult、GameplayEffect lifecycle、Attribute value 或其它模型无关 ingress contract，并带 ActorId、source tick、sequence 与稳定 fact identity。Session Source/Ingress Pass MUST 在进入 Step 前移除 packet、authority metadata、endpoint 与 transport 类型。

#### Scenario: 服务端拒绝预测动作

- **WHEN** ServerAuthoritative Source 收到 Action reject decision
- **THEN** Ingress Pass MUST 将其转换为 typed ActionLifecycle ingress
- **AND** Kernel MUST 不读取原始 ActionDecision packet

### Requirement: SimulationActorTickResult 必须通过稳定 EventId 提交副作用

Gameplay facts 与 presentation commands MUST 使用由正式 domain source、ActorId、activation identity、SimulationTick 与 local event sequence 构成的稳定 EventId。Kernel MUST 不播放动画、发送 packet 或触发相机/VFX。Egress Pass MUST 为外部事件生成带显式 ActorId 的 Publish、Replace、Retire 或 Suppress disposition；Backend MUST 核对 EventId 与 Actor 归属，并在 disposition 与全部 working state 校验后原子发布最终 state，再将 plan 交给 SimulationCommitter。

#### Scenario: Timeline产生Cue

- **WHEN** Timeline Runtime 在当前 Step 产生 Cue command
- **THEN** Finalize MUST 输出带 EventId 的 command
- **AND** Local Egress 生成 Publish 后只有 Committer MAY 触发外部 Cue port

### Requirement: Portable Core 与 Unity/普通DotNet Host必须共享正式源集

Graph artifact codec、Graph Runtime、domain state schema、Input/Output、Pipeline descriptor、Snapshot contract、runtime handle 与 WorldSolver contract MUST 来自 canonical portable source set，并可由 Unity asmdef 与普通 .NET csproj 编译。Unity Host 只负责 composition、资源和表现绑定；普通 .NET Host 不得复制 Graph evaluator、Action、Timeline 或 Effect 业务规则。

#### Scenario: DotNet Host使用portable domain runtime

- **WHEN** 普通 .NET Host 引用 canonical portable source
- **THEN** MUST 使用同一 Graph Runtime、domain state、Pipeline transaction 与 Kernel contract
- **AND** MUST 不需要 UnityEngine、ScriptableObject 或 CharacterPipelineHost 执行 Gameplay

### Requirement: Structured Trace 不得受外部输出处置控制

Graph Runtime、SimulationKernel、Pipeline Runtime/Pass、WorldSolver adapter、Session Source 与 SimulationCommitter MUST 在各自正式边界向只读 diagnostics sink 发布 structured Trace。Trace MUST 记录 PipelineHash、PassId、domain owner、成功、失败、restore、replay 与 OutputDisposition；Egress MUST 不能通过 Publish、Replace、Retire 或 Suppress 隐藏或改写 Trace。Diagnostics MUST 不反向改变 Character/World/Pipeline state 或外部输出。

#### Scenario: Replay抑制重复Cue

- **WHEN** Egress Pass 对重复 Cue EventId 生成 Suppress
- **THEN** Committer MUST 不再次触发 Cue port
- **AND** Diagnostics MUST 仍记录 replay step、domain owner、EventId 与 Suppress disposition

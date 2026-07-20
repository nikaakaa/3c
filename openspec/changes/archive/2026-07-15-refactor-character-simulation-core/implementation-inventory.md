# Character Simulation Core 实施清单

> 2026-07-15 数值边界审计：此前直接生成公共 `SimScalar` Program 的实现不再视为最终完成。下列 Authoring 闭包与旧路径盘点仍有效；Numeric、Program、State、Compiler、Kernel、Solver 部分以“Semantic IR -> Float32 Target”的修正设计为实施目标。

## Corin Authoring 闭包

唯一编译根是 `CorinCharacterPipelineDefinition`，它引用：

- `CorinPlayableRootTree`
- `CorinCharacterInputProfile`
- `CorinCharacterGameplayEffectProfile`
- 内联 `CharacterAnimationPresentationDefinition`
- Attack、Dodge Action Profile
- Locomotion Move、Motion Correction Ack Behavior Profile

RootTree 的当前闭包包含 3 个 StateMachineGraph、15 个 StateBehaviorSubTree、11 个 inline TimelineData、8 个 Decision TreeClip、60 个 ConditionRuleGraph、8 个 Bool Blackboard declaration 和 4 个 Float Blackboard declaration。

### 可达执行类型

| 类型组 | 可达类型 | 正式迁移结果 |
|---|---|---|
| Tree | OneRootTree、RootNode、LoopNode、ParallelNode、SelectorNode、SequenceNode、SucceedNode | ControlFlow operation 与 Runnable state slots |
| StateMachine | StateMachineNode、StateMachineGraph、StateMachineEnterNode、StateMachineAnyStateNode、StateMachineExitNode、StateNode、StateBehaviorSubTree、StateOnEnterNode、StateOnExitNode、StateRootCompletedNode | StateMachine table、condition handle 与 execution-path slots |
| Condition | ConditionRuleGraph、ConditionRuleResultNode、CompareNode、AndNode、OrNode、NotNode、ExposedPropertyNode | Typed value/condition operations |
| Input | CharacterActionRequestInfoNode、CharacterInputVector2InfoNode、CharacterInputVector2MagnitudeInfoNode | CharacterSimulationInput 与 request-buffer operations |
| Blackboard | PipelineBlackboardBoolInfoNode、PipelineBlackboardFloatInfoNode、BoolExposedProperty、FloatExposedProperty | Blackboard layout/address 与 typed value operations |
| Action | ActivateActionInstanceNode、ActionContextActiveInfoNode、SubmitActionLifecycleTransitionNode、StateExitCauseInfoNode | Action catalog、instance slots 与 lifecycle operations |
| Motion | LocomotionInputMotionNode、CharacterMoveFacingAngleInfoNode | MotionContribution 与 WorldRequest operations |
| Timeline | TimelineNode、TimelineOwnershipModule、TimelineData、AnimationTrack/Clip、MotionCurveTrack/Clip、TreeTrack/Clip、ActionCueTrack/Clip、TimelineRunningTree、TimelineEnterNode | Timeline table、logic playback slots、TreeClip operations 与 presentation producer manifest |
| Ownership | StateBehaviorGraphReferenceModule、ScopedGraphReferenceModule | 编译期 owner route；运行时不保留 object reference |

PropertyPort、BoolPropertyPort 和普通 PropertyPort 只表达编译期数据流，不生成有状态 runtime object。

## 当前状态与副作用归属

| 旧 owner | 影响未来 Tick 的数据 | 最终 owner | 外部副作用处理 |
|---|---|---|---|
| RunnableNode | State、LifecyclePhase、StopContext、LastStopStatus、ActivationGeneration、ActivationScope、Composite child cursor | CharacterSimulationState Runnable slots | 无 |
| StateMachineGraphRuntime | active/exiting/pending state、transition edge、execution scope、exit context、activation generation、runtime facts | CharacterSimulationState StateMachine slots | 无 |
| TimelinePlaybackScheduler | requests、active/terminal playback、logic time、cycle、TreeClip runtime、motion samples、producer generation | CharacterSimulationState Timeline slots | animation/camera/cue command 交给 Committer |
| TimelineRunningTree clone | Decision/Commit node state、clip lifecycle | CharacterSimulationState TreeClip slots | 无 |
| PipelineBlackboardRuntime | declaration、owner address、value、write provenance、projection candidate | Program Blackboard catalog + CharacterSimulationState Blackboard slots | projected fact 写 SimulationOutput |
| ActionRuntime | profile registry、active ActionInstance、next ids、request/lifecycle/window facts | Program Action catalog + CharacterSimulationState Action slots | Cue/事实写 SimulationOutput |
| GameplayEffectRuntime | Tag、Attribute、ActiveEffect、period、journal、handle/cursor、tick transaction | Program GE catalog + CharacterSimulationState GE slots | Cue/事实写 SimulationOutput |
| CharacterInputStage | request buffer、latched render input、pending request | Adapter 只保留 render latch；request buffer 进入 CharacterSimulationState | 无 |
| CharacterMotionStage | contribution resolve、warp、pending correction、logic pose | Kernel pending evaluation + WorldSimulationState | CharacterController.Move 只在 Unity WorldSolver |
| CharacterPipelineFrame | transient input、network input、animation selection、strict/presentation/sync output | SimulationTickPlan、PendingCharacterEvaluation、SimulationTickResult | Committer 消费 OutputPlan |
| CharacterPresentationStage | interpolation history、animation lifecycle、camera state | Presentation-owned state | Animancer、visual root、camera、cue ports |
| ServerAuthoritative Adapter/Binding | packet mapping、history、correction、ExternalPose | 删除；后续 model-owned Driver 重建 | 当前模型标记不可用 |

## Unity 与外部依赖边界

| 依赖 | 允许位置 | Core 中的替代 |
|---|---|---|
| UnityEngine.Object / ScriptableObject | Authoring、Compiler、Projection asset | stable id、canonical bytes |
| InputAction / Camera-relative direction | Unity Input Adapter | CharacterSimulationInput |
| Transform / CharacterController | Unity WorldSolver binding、Presentation visual root | WorldBodyState、BodySample |
| AnimationClip / Animancer | CharacterPresentationProjection、Presentation adapter | ProducerId、EventId command |
| Unity Time / render delta | GameplayTickSystem、PresentationFrame | SimulationTickPlan 与 presentation delta |
| packet / endpoint / transport / history | 具体 Network Model | typed SimulationIngress、snapshot restore、SimulationOutputPlan |

## 旧路径删除表

| 旧类型或字段 | 处理 |
|---|---|
| CharacterPipeline 单角色 stage runtime | 由 SimulationSessionRuntime 取代 |
| CharacterBTSMTLPhase / BehaviorTreeRuntime Character 装配 | 删除正式入口 |
| StateMachineGraphRuntime Character 装配 | 删除正式入口，保留隔离通用解释器 |
| TimelinePlaybackScheduler gameplay runtime | 删除 |
| TimelineTreeRuntimeSet / Character InitTimelineTree | 删除 |
| CharacterGraphContext runtime service 聚合 | 删除正式角色执行入口 |
| PipelineBlackboardRuntime dictionary | 删除 |
| CharacterMotionStage / LogicPosePort | 删除 |
| UnityCharacterControllerMotionExecutor | 重命名并收口为 UnityCharacterControllerWorldSolver |
| CharacterMotionAuthority | 删除 |
| CharacterNetworkSendStage / ReceiveStage / NetworkInput | 删除 |
| ExternalPoseSample / ExternalPoseCorrection | 删除 |
| MotionStage correction 与 acknowledgement | 删除 |
| CharacterServerAuthoritativeBinding / Adapter | 删除 |
| runtime compile、stale artifact、Transform、默认 Solver fallback | 不建立 |

## Assembly Ownership

| 模块 | Assembly/source set | 允许依赖 |
|---|---|---|
| Identity、Semantic IR、Numeric Target contract、versioned operation semantics | `ThirdPersonSimulation.Core` | System |
| Float32 Program/State/Snapshot/Kernel/Session 与 Driver/Solver ABI | `ThirdPersonSimulation.Float32` portable source set | Core、System |
| Gameplay Effect Semantic catalog/operation/state contract | 独立 portable Gameplay module | Core semantic/target contracts |
| Character authoring Compiler Frontend、emitters、Float32 lowering、Program/Projection assets | Unity Editor/authoring boundary | BTSMTL、Unity、Core、Float32 |
| Float32 Local Driver 与 Unity Input Adapter | Unity runtime assembly | InputSystem、Core、Float32 |
| Float32 UnityCharacterControllerWorldSolver | Unity runtime assembly | UnityEngine、Core、Float32 |
| Projection、Committer、Animation/Camera/Cue ports | Unity runtime assembly | Unity、Animancer、Core、Float32 |
| Network model packet/session/endpoint | model-owned assembly/namespace | Core ports；不得被 Core 引用 |
| Inspector、Agent、Preview composition | Unity Editor assembly | Compiler report、Projection、Diagnostics read model |

## Numeric Target 修正

- Corin 当前作者数据的运动速度上界为 `7.36`，转速上界为 `720 degree/s`，Timeline 最长为 `161 frame`，输入 buffer 为 `0.2 second`，Health 范围为 `0..100`。这些数据只作为 source literal 与 target capability 盘点，不再用于规定所有模型必须共享的定点 scale。
- 已实现的 `SimScalar(Int64 raw, scale=1_000_000)` 属于被否决的公共强制定点 ABI，不能继续作为正式 Program/Input/State/World/GE 合同；重构完成时删除，不把它保留成兼容层或未安装的 Rollback target。
- Compiler Frontend 保存 numeric-neutral canonical source literal。Float32 Target 负责 finite-value、运算和 codec 规则；未来 Fixed Target 自己负责 raw width、scale、rounding、overflow 与量化诊断。
- Session 只装配一个完整 NumericProfile；Program、Kernel、Input、State、WorldSolver 和 Snapshot ABI 必须匹配。不同 target 共享 SemanticHash，不共享 ProgramHash、LayoutHash 或 Snapshot。

## Program artifact 规则

- Semantic IR payload 固定为 versioned manifest、numeric-neutral operation/control-flow/reference/state declaration、source literal、source map 与 producer declaration。
- Float32 Program payload 固定为 target manifest、typed constant/operation/control-flow/reference table、Character state/scope/world request/output layout、source map 与 producer manifest。
- Canonical 编码使用 little-endian integer、UTF-8 length-prefixed string、显式 enum、按稳定 identity 或连续 index 排序的 array；不存在运行时 Dictionary 枚举顺序、Unity serializer、BinaryFormatter 或反射字段顺序。
- `SemanticHash` 覆盖 numeric-neutral IR；`LayoutHash` 覆盖 NumericProfile 与 Character state layout；`ProgramHash` 覆盖 SemanticHash、target manifest 与完整 Program payload。
- `CharacterSimulationProgramAsset` 每个实例只保存一个 target artifact，普通 .NET 文件直接写同一 bytes；runtime 不从 Graph/IR 编译，也不存在 Unity schema 与 server schema 两份格式。
- `SimulationProgramCatalog` 按 ProgramId 排序，拒绝重复 identity、TickRate、NumericProfile、TargetAbiVersion 或 OperationSetVersion 不一致，并合并 required world capability 后一次校验匹配 target 的 Solver。
- Program v8 的 SourceMap 使用 version 1 canonical string table。SourceType、Graph/Node/Edge/Declaration/Timeline/Track/Clip identity 与 display-path segment 只编码一次，entry 只保存 table index；loader 恢复原始只读字符串，不读取旧 Program v7 格式。

### Corin Program 重建体积

| 项目 | Program v7 | Program v8 | 减少 |
|---|---:|---:|---:|
| canonical artifact | 2,411,052 bytes | 1,285,649 bytes | 1,125,403 bytes（46.68%） |
| Unity YAML asset | 4,823,128 bytes | 2,572,323 bytes | 2,250,805 bytes（46.67%） |

重建前后均为 485 operations、804 state slots、2636 source-map entries；SourceMap 全字段内容哈希均为 `0d2468714ced0c6741ad3ff938bc112a4e12deb15adbd8397234a13888fb5748`。

## State 与 Snapshot owner

- `CharacterSimulationState` 只保存 ProgramId/ProgramHash/LayoutHash/NumericProfile、last completed SimulationTick 和 Program layout 声明的 typed slots；slot semantic 覆盖 Runnable、StateMachine、Timeline、Blackboard、Input request、Action、GameplayEffect、motion、RNG、handle allocator 与 fact sequence。
- `WorldSimulationState` 只保存 Solver/WorldRevision、stable ActorId body table 和 Solver 明确声明为 Reconstruct 或 Snapshot 的 payload；Character 与 World 没有共享 mutable reference。
- Character 与 World 使用各自 canonical codec；CharacterStateHash 覆盖 Program/Layout binding 与 state bytes，SimulationWorldHash 再覆盖 Catalog、完整 roster、Solver、WorldRevision、Tick 和 World bytes。
- Snapshot 复制全部 canonical bytes。Restore 先解码并校验所有 Actor 与 World，再构造新的 `SimulationWorldStateSet`，最后由 `SimulationWorldStateStore` 一次替换引用；失败不会部分写回。
- Driver history、模型 history、Animancer、淡出状态、相机和其它 Presentation state 不属于上述类型，因此不会进入 gameplay snapshot/hash。

依赖方向固定为 `Authoring/Unity/Networking/Presentation -> ThirdPersonSimulation.Float32 -> ThirdPersonSimulation.Core`。Core 与 Float32 portable source set 都不引用 BTSMTL、Unity、Editor、Animancer、InputSystem、Cinemachine、TEngine 或具体 Network Model。

## Source 到运行结果迁移矩阵

| Source | Emitter | Operation/data | State slot | World request | Output |
|---|---|---|---|---|---|
| Runnable/Composite | ControlFlowEmitter | ControlFlow table | lifecycle/cursor/stop/generation | 无 | lifecycle trace |
| StateMachine/Edge | StateMachineEmitter | State/transition table | active/pending/exiting/path | 无 | state facts、producer ownership |
| Condition/Value | ConditionEmitter | typed expression table | 只读或声明 slot | 无 | condition trace |
| TimelineNode/Data | TimelineEmitter | playback/track/segment table | time/cycle/request/clip lifecycle | motion segment | producer、window、cue facts |
| Blackboard declaration/reference | BlackboardEmitter | declaration/address/projection catalog | scoped value/provenance | 无 | projected facts |
| Input node | InputEmitter | input lookup/request operation | request buffer | locomotion intent 输入 | input trace |
| Action node/profile | ActionEmitter | action catalog/lifecycle operation | ActionInstance/request/counter | action motion 由 Timeline 产生 | action facts |
| GameplayEffect profile | GameplayEffectEmitter | GE catalog/operations | tag/attribute/effect/journal | modifier 可影响 motion operation | effect/attribute/cue facts |
| Locomotion/MotionCurve | MotionEmitter | contribution/modifier operation | accumulator/pending request | WorldSolveRequest | MotionResult/body sample |
| Animation/Camera/Cue producer | PresentationEmitter | producer manifest | Event sequence | 无 | EventId presentation command |

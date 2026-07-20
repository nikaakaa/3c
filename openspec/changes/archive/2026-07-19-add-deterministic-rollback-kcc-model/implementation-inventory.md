# Deterministic Rollback 实施盘点

## 已完成依赖

以下基座已经归档并进入 current specs：

- `2026-07-15-refactor-character-simulation-core`
- `2026-07-15-refactor-character-semantic-frontend-artifact`
- `2026-07-16-refactor-character-state-transaction-runtime`
- `2026-07-16-refactor-gameplay-session-composition-boundary`
- `2026-07-16-refactor-simulation-operation-runtime-modules`

当前公共入口包括：

- `ValidatedSemanticIrArtifact`
- `OperationExecutionTopology`
- `OperationControlRuntime<TTarget>`
- portable Pipeline descriptor/compiler/plan/state transaction
- `SimulationSessionHost`
- `ISimulationSessionRuntimeHandle`

## Corin Semantic IR 迁移基线

本节记录开始实现 Fixed Target 时读取的历史 artifact，用于解释量化范围与能力覆盖，不是当前工作树的可运行产物身份。当前 Program 身份必须由正式 Build/Reader重新读取，不能复用以下 hash、计数或 operation-set版本。

正式 artifact：

```text
Library/CharacterSimulation/SemanticIr/c7a7c1e3f7e64d81b5a04a90cbeb8d4e.csir
```

身份：

```text
ProgramId: character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e
CompilerVersion: character-simulation-compiler/14
OperationSetVersion: character-gameplay-operations/3
TickRate: 60
SourceRevision: e983e21b46714ab69189ddee165e308dfa67c0dcdccfb2d0d70b0b99aae576e8
SemanticHash: 4887f6ab8a0e171f80f4400ab2f69799336c4c6026762f2049bbbb3377061ed1
```

规模：

```text
Operations: 485
Literals: 789
ControlFlow: 450
References: 252
StateSlots: 793
Scopes: 2
WorldRequests: 1
OutputChannels: 3
CatalogEntries: 91
Producers: 16
SourceMap: 2624
```

Gameplay capability：

```text
Action
GameplayEffect
PipelineBlackboard
RunnableTree
StateMachine
Timeline
TimelineMotionCurve
```

World capability：

```text
BodyMotion
Grounding
Collision
```

该历史基线没有 moving platform、Rigidbody、动态破坏或 Unity Physics operation requirement。Fixed Target必须完整降低启动时匹配的当前 operation set；所有目标数值在编译边界量化，要求 Exact 的值若不能精确表达则直接拒绝。

## 已实现能力：Fixed ActorCollision

`DeterministicKccWorldSolver`现已在同一`ResolveBatch`中先为全部Actor生成量化静态世界candidate，再把其他Active Actor加入Fixed contact pair。Rollback KCC显式声明`WorldFeature.ActorCollision`，Composition也把该feature纳入正式兼容性检查。

正式实现为Fixed `SolidBodyBlock`：按stable ActorId pair order执行垂直区间过滤、连续相对sweep、初始重叠去穿透、闭合法向裁剪、静态世界重新约束、最终间距验证与原子提交。moving-vs-stationary只修正移动方，双方移动时平分去穿透并分别裁剪闭合法向；切向位移保留。该实现不复用DotRecast Float32 ActorContactSolver，也不引入Unity Physics、质量、冲量、攻击专属碰撞或Transform旁路。

KCC identity已提升到`deterministic-kcc/3`，SolverVersion提升到`3`，Configuration schema提升到`deterministic-kcc-configuration/4`，Collision World升级为`deterministic-collision-world/2`。contact shape、query/Motor semantic version、容差、movement/query/contact容量、pair/iteration capacity和policy全部进入configuration或Kcc identity；旧KccId、旧WorldConfigurationHash与旧artifact不能继续使用。

结构化diagnostics现记录requested/applied/remaining displacement、movement/query iteration、canonical blocking primitive/feature/TOI/normal、ground/support/ledge、step结果与拒绝阶段，以及batch pair数、pair iteration、partner ActorId、correction和最终间距结果。代码仍只属于`ThirdPersonSimulation.DeterministicKcc`，程序集只引用Core与Fixed Target。

## 已实现能力：Continuous Fixed KCC Motor

静态查询已从“终点overlap后再二分”替换为唯一Fixed closest-feature query pipeline。Plane使用解析距离；Box使用胶囊轴到AABB的精确最近特征；Triangle使用胶囊轴到face/edge/vertex的精确最近特征并执行one-sided规则。Translational cast使用位移长度作为距离变化上界做保守推进，起点和终点都不重叠时仍能命中路径中的薄障碍；达到固定预算却不能形成保守结果时fail-closed，不返回无碰撞。

Collision artifact v2保存canonical vertex table、indexed triangle、stable vertex/edge/face identity和对称triangle adjacency。Editor baker将MeshCollider与TerrainCollider统一降低为同一种quantized triangle surface，拒绝量化退化和non-manifold edge；Fixed runtime不读取Mesh、TerrainData或Unity Physics。当前正式Demo仍使用五个Box primitive，但资产已经由v2正式codec重建。

唯一`DeterministicKccMotor`负责初始penetration recovery、最早TOI、最多三平面约束、最终overlap validation、稳定ground、坡面、共享边合并法线、事务式step和受previous stable support约束的ground snap。每个Actor在Solver创建期获得独立Motor/query scratch；Actor pair阶段使用一份固定容量workspace，修正后仍回到同一个Motor执行static reconstraint。Tick内不会为Actor、Primitive或contact创建临时集合或自动扩容。

KCC state codec v2只保存会改变下一Tick分支的`Grounded`、support primitive/feature和ground normal。FoundAnyGround、ledge、step phase、query summary、candidate、manifold和diagnostics是瞬态数据，不进入Snapshot或StateHash。当前non-goals仍是moving platform、通用动态刚体、任意旋转capsule、攻击推人、ghost/team过滤和跨Tick warm start。

Rollback hash egress中的`KccHash`只覆盖`WorldSimulationState.SolverStatePayload`的canonical bytes，不再重复填整个`WorldStateHash`。网络模型因此能先区分完整World差异和KCC support state差异，同时仍把Solver payload当作不透明数据，不解析或依赖具体Motor类型。

移动同步不只有Rollback一种。ServerAuthoritative主线继续由Authority完整求解并通过owner prediction/reconciliation和remote interpolation传播结果；当前隔离Demo因为目标就是确定性input rollback，所以Actor contact必须进入Fixed World batch、Snapshot与Hash。详细取舍见`movement-synchronization-research.md`。

## 实际 Operation 分布

Corin 使用 Root、Loop、Parallel、Sequence、Selector、StateMachine、State、State lifecycle、Timeline、Timeline animation/motion/tree clip/cue、Blackboard、Input、Action、Locomotion motion、Condition、Compare、And/Or/Not。GameplayEffect operation即使未出现在某次执行树计数中，Fixed backend仍须通过`CharacterGameplayOperationSet.RequireCompleteBackend`实现启动时匹配的完整operation set，不能按某个角色当前未触发而省略。

当前 state value 分布：

```text
ActionActivationRequest: 2
ActionInstance: 2
ActionInstanceReference: 11
Boolean: 19
GameplayEffectAggregate: 1
Identity: 39
InputRequest: 2
Int32: 521
Scalar: 23
UInt64: 173
```

## 合同边界

唯一语义源保持为 `.csir`。Float32 与 Fixed 仅共享：

- ProgramId、SourceRevision、SemanticHash
- operation-set version
- control-flow、reference、scope、catalog、producer 与 source-map identity
- `OperationExecutionTopology`
- `OperationControlRuntime<TTarget>`
- typed state schema 与 transaction lifecycle 形状
- portable Pipeline/compiler/composition/Host 合同

两者不得共享：

- target Program、ProgramHash、LayoutHash
- numeric value、state partition、state transaction
- Program Runtime、Kernel、Backend
- World state、Snapshot、codec
- mutable operation/domain runtime

## 禁止引用的既有模型路径

Rollback portable 与 Unity adapter 均不得引用以下 ServerAuthoritative implementation：

- `ServerAuthoritativePredictionState`
- `ServerAuthoritativePredictionHistory`
- `ServerAuthoritativePredictionReconciler`
- `ServerAuthoritativePredictionDispositionJournal`
- `ServerAuthoritativeNetworkCheckpoint`
- ServerAuthoritative correction、baseline、observation、datagram 与 Fantasy endpoint 类型

Rollback 只复用 model-neutral `GameplayNetworkModelDefinition`、Source preparation contract、portable Pipeline compiler 与 `SimulationSessionHost`。

## 正式程序集与资产所有权

```text
ThirdPersonSimulation.Core
  公共 identity、Semantic IR、operation-set、control runtime、Pipeline 与 composition 合同

ThirdPersonSimulation.Fixed
  FixedQ32.32 数值、Fixed Program/State/Kernel、Fixed Backend 与 target codec

ThirdPersonSimulation.DeterministicKcc
  量化 Collision World artifact、静态 capsule query 与 Deterministic KCC solver

ThirdPersonSimulation.DeterministicRollback
  canonical input protocol、Source ports、Pipeline Pass、History、Replay、Hash、Recovery、Output Disposition

ThirdPersonSimulation.DeterministicRollback.Unity
  Model/Endpoint/Source/Pipeline/Solver Definition、Peer Scene launch、Unity Input/Presentation 边界

ThirdPersonClient.Editor
  Fixed Target build、artifact publish、Collision World build、Rollback Scene/build authoring
```

Fixed 与 Rollback portable 程序集不得引用 Unity、Fantasy、Animancer、CharacterController、DotRecast 或 ServerAuthoritative。Deterministic KCC 不读取 Unity Scene；Editor 只负责把明确选择的静态几何烘焙为 canonical artifact。

## 数值合同

Fixed Numeric Profile 固定为：

```text
NumericProfileId: fixed-q32.32
TargetAbiVersion: 1
ScalarBits: 64
FractionBits: 32
Rounding: NearestEven
Overflow: Reject
DeterministicReplay: true
CanonicalCodec: fixed-q32.32-le/v1
```

运行时加减乘除、平方根、角度归一化和三角计算只能使用确定整数算法。`double` 仅允许存在于 authoring 到 Fixed 的编译边界和 Unity Presentation 转换边界，不进入 Fixed Program Evaluate、KCC、Snapshot 或 hash。

## Corin Fixed Program 编译证据

- Compiler assembly: `ThirdPersonSimulation.Fixed.Compiler`
- Build tool: `ThirdPersonSimulation.FixedProgramBuildTool`
- Semantic IR：`Library/CharacterSimulation/SemanticIr/<DefinitionGuid>.csir`
- Fixed Program：`Library/CharacterSimulation/Fixed/<DefinitionGuid>.fixed-program`
- Rollback Build先调用正式Character Simulation Build Orchestrator，从当前Definition重新生成Semantic IR、Float32 Program和Presentation Projection。
- Unity Build与portable Build Tool都调用`FixedCharacterSimulationTargetCompiler.CompileArtifact`，该入口唯一拥有Fixed lowering、canonical codec与round-trip ProgramHash/LayoutHash校验。
- Player Build前使用Fixed Program的ProgramId、SourceRevision、SemanticHash与producer identity校验Presentation Projection；旧Fixed Program或旧Projection不会进入Player。

Fixed Runtime assembly 不包含 `.csir` reader/lowerer；只有 Compiler assembly 与 portable build tool 能从 validated Semantic IR 生成 Fixed Program。Fixed StateSlot lowering 会重建 codec identity，禁止沿用 Semantic IR 中的 Float32 codec identity。

## 双 Peer Demo 静态世界运行证据

正式启动入口：

```text
3cDemo/Tools/DeterministicRollback/Start-DeterministicRollbackDemo.ps1
```

启动组合固定为一个 Canonical Host 与 Peer A/B 三个独立 Player进程，UDP端口分别为 `24100/24101/24102`。Peer Scene显式持有一个 `SimulationSessionHost`、两个 stable Actor Host、Fixed Program、Rollback Composition、量化 Collision World、Deterministic KCC、Endpoint和 diagnostics；不包含 `CharacterPipelineHost`、CharacterController binding、ServerAuthoritative组件或运行时模型切换。

2026-07-18正式 Player运行 164秒后，Host推进到 canonical Tick 9502、confirmation Tick 9495；最终 endpoint统计为：

```text
rx=58927; tx=39906; rxDepth=0; rxMax=18/512
txDepth=0; txMax=6/512; pendingReliable=0
inputs=19021; canonical=9502; revisions=0; confirmations=9495; hashes=1898
```

Host、Peer A、Peer B运行日志均未出现 exception、pipeline pass failure、external commit failure、queue/history capacity exhaustion、desync、identity mismatch或 endpoint failure。运行期间分别向两个 Peer提交移动、闪避和攻击输入，两个进程保持 Active并持续消费 owner/remote Actor输出。

该证据采集时Host统计`revisions=0`，因此只覆盖forward与confirmation稳定推进，没有通过真实延迟/丢包触发late-input rollback，也没有覆盖Actor身体接触。Fixed ActorCollision现已改变KccId/WorldConfigurationHash并完成相关程序集静态编译，但上述旧运行记录仍不能作为新接触能力的Player验收证据；必须由新的三进程实机运行验证两个Actor接触、分离、滑动与持续同步。

## 公共边界落地结果

target-typed World Solver合同已经落地：Float32原接口与 Fixed接口分别绑定自己的 request/state/result；Fixed程序集不引用 Float32 world state或 solver。`SimulationSessionHost`继续只消费 numeric-neutral outer runtime handle，不获得 Fixed/Float32分支。Rollback code、asset与Scene静态审计未发现 Unity Physics、CharacterController、DotRecast、Fantasy或 ServerAuthoritative实现引用。

## 后续 Program 重建后的运行故障记录

在当时的Corin Fixed Program扩展到665个operation后，三进程日志暴露了四个同一节奏/确认链上的实现漏洞；665同样是该次运行记录，不是当前Program规模合同：

- Host只在canonical epoch启动时检查4 Tick输入lead，之后按墙钟无限推进；99个输入批次期间生成125个canonical bundle和187次revision，最终可靠窗口堆积。
- Peer没有predicted lead上限；快端可以在慢端停顿时持续增加未确认history。
- Endpoint Source在发送后的第二次Pump后没有再次排空canonical queue，可能把新的confirmed frontier和其最终bundle拆到两次Ingress。
- History Pass在replay中推进confirmed frontier后，Output Committer使用事务结束时frontier判断旧历史，误把事务开始时尚未确认的Tick 3判成已确认。

正式修正保持单一路径：Host每次生成前检查共同显式连续前沿覆盖`NextCanonicalTick + InputDelayTicks`；Schedule以`MaximumRollbackDepthTicks`限制预测领先；Source在第二次Pump后再次Drain；Runtime State记录transaction-start confirmed floor供Output Committer校验。没有扩大history/可靠消息容量，没有吞异常，也没有增加transport或simulation fallback。

`ThirdPersonSimulation.DeterministicRollback.Unity.csproj`及其portable依赖已使用禁用build server参数静态编译通过，0 warning/0 error。新的Player三进程持续运行结果仍需重新构建后由实机日志确认，旧164秒记录不代表本次Program和修正已经完成运行验收。

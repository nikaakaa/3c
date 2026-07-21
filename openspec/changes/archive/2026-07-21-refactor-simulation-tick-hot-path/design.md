# Design: Simulation Tick 热路径的编译索引与数据所有权

## Context

正式运行链路保持为：

```text
Character Authoring
  -> Semantic Frontend
  -> validated .csir
  -> Float32 / Fixed Target Program
  -> Program Runtime + Kernel Binding
  -> Pipeline Schedule
  -> Kernel Evaluate
  -> World ResolveBatch
  -> Kernel Finalize
  -> completed step candidate
  -> Commit / Publish
  -> Presentation
```

当前`ProgramExecutionLayout`已经按Program实例通过`ConditionalWeakTable`共享，且已经缓存control topology、SourceMap、Timeline curve、GameplayEffect program、Tag query和typed state address。问题不是“完全没有cache”，而是cache没有覆盖Value输入路由、Timeline/State owner等稳定关系；同时State、Blackboard、Output和Pipeline state在边界上没有明确的唯一owner。

本设计只移动不变量和收紧数据生命周期，不改变业务operation的执行结果。

## Goals

- 让所有只依赖Program的查询在Program layout构建时完成。
- 让Semantic IR在Target lowering前就能验证Value端口类型、唯一来源和顺序。
- 让共享Program layout保持backend无关，让backend兼容性属于Program Runtime/Kernel binding。
- 保持committed state不可变，同时让dirty page只复制一次。
- 让Blackboard scope失效不依赖字符串owner和全量物理清零。
- 让Evaluate与Finalize共享同一个Actor output workspace，最终结果只冻结一次。
- 让completed step state从构造到publish保持同一canonical实例。
- Float32和Fixed使用同一合同，不复制业务控制流或保留旧实现。

## Non-Goals

- 不缓存依赖mutable state的Value求值结果。
- 不修改operation业务含义、控制流优先级、Timeline采样或Action窗口。
- 不把ProgramExecutionLayout变成Session、Pipeline或Network Model配置。
- 不让Local模式使用会破坏Rollback/Snapshot不可变性的原地状态写入。
- 不通过对象池重新使用仍被committed state持有的page array。
- 不改变最终`SimulationActorTickResult`对外schema或Presentation消费方式。

## Decision 1: Operation Set拥有numeric-neutral Value Port Contract

### Problem

当前Frontend从Unity `PropertyPort`读取字段并为未连接输入创建constant，但Semantic IR只保存literal与Value edge。Target/runtime只能从constant identity中的`/constant/port:`恢复端口。更严重的是，Compiler没有统一合同验证source output与target input是否兼容。

`ProgramStateValueKind`不能直接作为完整Value端口合同：它同时包含committed state专用的InputRequest、ActionInstance和GameplayEffectAggregate，并且当前公共codec identity包含Float32命名。端口合同必须只表达业务Value类型，且独立于Target存储格式。

### Decision

Operation Set新增版本化`OperationValuePortContract`。合同至少表达：

```text
OperationCode
InputPort
  PortId
  Order
  TypeConstraint
OutputPort
  PortId
  TypeConstraint
ConstraintGroup
AllowedConversion
```

新增numeric-neutral `SemanticValueKind`，覆盖当前Value graph真实需要的：

```text
Boolean
Int32
UInt64
Number
Vector2
Vector3
Yaw
Identity
```

固定端口直接声明kind；受约束端口声明规则：

- `Compare`输入接受NumericLike，并要求两个输入在正式conversion规则下可比较，输出Boolean。
- `And`、`Or`、`Not`和`ConditionResult`输入接受现行BooleanLike集合，输出Boolean。
- `BlackboardGet`输出和`BlackboardSet`输入由引用的declaration value kind解析。
- `Constant`输出由literal kind解析。
- `CameraBasisRead`输出由显式output port解析。
- 其它operation必须有固定签名或正式resolver，不得以“unknown/object”跳过校验。

Semantic Emission从authoring port metadata取得实际端口与类型，但只通过Operation Set合同解析为`SemanticValueKind`。Target compiler不再读取Unity port。

### Tradeoff

- 收益：类型错误在artifact发布前失败，Float32与Fixed共享同一Value语义。
- 代价：Operation Set从`/5`升版，全部Emitter与Target backend必须声明完整端口能力。
- 不选择“只保存字符串端口”：它只能消除substring，不能校验类型，也会继续让Unity authoring metadata成为隐含运行合同。
- 不选择“复用CharacterStateValue类型”：它把Target存储和committed state领域泄漏回Semantic IR。

## Decision 2: Value edge保持连线真值，constant使用独立binding

### Decision

linked input继续只由`ProgramControlFlowEdge(kind=Value)`表达，不新增重复linked table。Semantic IR新增：

```text
SemanticConstantInputBinding
  TargetOperation
  TargetPort
  ConstantIndex
  ResolvedValueKind
```

Value edge的source output与target input类型通过Operation Port Contract解析，不需要把同一source/target再复制到另一张IR表。

Semantic IR构造与codec必须验证：

1. source/target operation存在。
2. source output port和target input port存在。
3. Value edge source kind可赋给target约束。
4. constant literal kind可降低到binding resolved kind。
5. 同一target operation/port最多一个source。
6. linked edge与constant binding互斥。
7. constrained-polymorphic group解析结果一致。
8. canonical order固定为target operation、port contract order、port identity。

Target Program保存同语义的target binding，其中constant已降低为Target constant index。Program canonical bytes、ProgramHash和普通Reader覆盖该表。

`ProgramConstant.Identity`只保留稳定source/diagnostics身份，不再携带端口协议。

### Tradeoff

- 收益：不复制linked truth，同时补齐未连接输入常量的结构化关系。
- 代价：Semantic IR、Target Program与Reader都需要新table和版本。
- 不选择“完整统一binding表写回artifact”：那会让每条Value edge在control-flow与binding中双写，产生一致性风险。

## Decision 3: ProgramExecutionLayout生成唯一runtime input span

### Decision

Layout构建时把Value edge与constant binding合并为：

```text
OperationValueInputRange[OperationCount]
  Offset
  Count

CompiledValueInputBinding[]
  TargetPortIndex
  ResolvedValueKind
  SourceKind = Operation | Constant
  SourceOperation
  SourceOutputPortIndex
  ConstantIndex
```

每个operation通过offset/count取得连续span。Float32/Fixed Value runtime按span顺序重新求值source或读取constant，不创建端口集合、不排序、不解析identity。

Layout同时建立：

- 紧凑Timeline operation handle数组。
- Timeline child operation到唯一Timeline owner的直接索引。
- State operation到所属StateMachine及execution path state address的直接索引。
- State `OnEnter`、`Root`、`OnExit`等固定语义edge索引。
- operation到references的kind-indexed span。
- GameplayEffect和其它operation的named constant/index解析结果。
- scope index、Blackboard group和compiled owner address。

这些索引只依赖validated Program，属于共享immutable layout。两个Actor绑定同一Program时复用同一实例。

### Value memoization boundary

Layout只缓存路由，不缓存Value结果。同一Tick内可能先执行BlackboardSet、Action lifecycle、GameplayEffect或Input消费，再读取相同Value operation；后续读取必须看到当前transaction的新状态。

### Tradeoff

- 收益：热路径成本随当前active输入和active Timeline数量增长，不再随整个Program规模做静态查询。
- 代价：Layout构建时间和常驻内存增加，但每个Program只支付一次。
- 风险：输入顺序变化会改变Compare/And/Or行为，因此唯一顺序来自Operation Port Contract，不使用asset遍历顺序或字符串临时排序。

## Decision 4: 共享Program身份与backend绑定身份分离

### Problem

同一Float32 Program会被Local、Preview、Prediction、Unity Authority和DotRecast Authority使用。`ProgramExecutionLayout`按Program共享，因此把backend identity写入Layout会让第一个composition污染后续composition，或迫使同Program创建多份Layout。

### Decision

分成两个对象：

```text
ProgramLayoutIdentity
  ProgramId
  ProgramHash
  LayoutHash
  OperationSetVersion
  NumericProfile

KernelProgramBinding
  Program reference
  ProgramExecutionLayout reference
  ProgramLayoutIdentity
  KernelBackendIdentity
  KernelSpecialization identity
```

`ProgramExecutionLayout`和`ProgramExecutionServices`只持有`ProgramLayoutIdentity`。

`Float32ProgramRuntime`、`FixedProgramRuntime`或等价正式Program Runtime在创建Kernel时，为catalog中的每个Program建立`KernelProgramBinding`。创建时一次性执行：

- NumericProfile匹配。
- OperationSetVersion匹配。
- backend完整实现当前operation set。
- Program每个operation code属于该operation set。
- Program、Layout、Services identity一致。

Actor runtime port直接保存已绑定Program、Layout和Binding。Evaluate/Finalize只做引用/紧凑identity核对，不遍历operation，也不从backend字符串重新查找。

### Tradeoff

- 收益：O(1)热路径校验，并保持Program可被不同Session composition复用。
- 代价：Program Runtime port多一个正式binding对象。
- 不选择“把backend stamp写入Layout”：它违反Program identity与Pipeline/backend identity分离的现行spec。
- 不选择“删除运行时校验”：Pending跨Evaluate/Finalize仍需要fail-fast确认来自同一Kernel binding。

## Decision 5: State Transaction复用metadata并移交dirty page所有权

### Current ownership

```text
base immutable page
  -> first write: CopyValues()
  -> mutable transaction array
  -> Commit: CharacterStatePage(values, false)
  -> second clone
```

### Decision

每个Actor target workspace增加按Program Layout建立的transaction workspace：

```text
Epoch
DirtyPageSlot[]
DirtyPageEpoch[]
DirtyPageIndexes[]
DirtyPartitionIndexes[]
Savepoint metadata
```

每个slot的所有权状态为：

```text
Empty
WorkspaceOwned
Published
Discarded
```

规则：

1. Begin推进epoch、重置dirty counts，不遍历清空全部slot。
2. 第一次写page时从base page复制一次，标记WorkspaceOwned。
3. 后续写同page直接使用同一array。
4. Commit用`takeOwnership`构造immutable page，并在同一动作中把slot标记Published、清除workspace可写引用。
5. 新committed state只替换dirty page，未修改page/partition继续共享。
6. Abort/Dispose只释放WorkspaceOwned数据，不触碰base或Published page。
7. 已Published array绝不进入可写池；只有abort且未发布的scratch可以复用。
8. GameplayEffect savepoint的现有LIFO与restore语义保持不变。

Float32与Fixed各自拥有target page value类型，但使用同一所有权状态和transaction生命周期。

### Tradeoff

- 收益：每个dirty page从两次复制降到一次；Dictionary/Stack元数据不再每Tick新建。
- 代价：状态机更严格，异常路径必须证明每个array只有一个owner。
- 仍会分配最终dirty page storage，因为committed history可能继续引用它；本change不会虚假宣称“所有状态写入零分配”。
- 不选择原地修改committed page：它会破坏Snapshot、Rollback、StateHash和前一状态不可变性。

## Decision 6: Blackboard使用Compiled Owner Address、Generation与typed provenance

### Decision

Program Layout为每个scope分配稳定`CompiledOwnerIndex`。Runtime状态使用：

```text
BlackboardOwnerToken
  ScopeKind
  CompiledOwnerIndex
  Generation

BlackboardWriteStamp
  SourceOperation
  LogicTick
  ActionInstanceId
  TimelineOperation
  ClipOperation
  Cycle
```

Character State增加对应typed value kind与codec。它们不包含人类可读路径；SourceMap formatter只在diagnostics启用或异常时生成字符串。

生命周期规则：

- Character：初始State建立token，generation固定为1。
- Graph Config：初始State建立token，generation固定为1，保持只读。
- Graph Instance：Runnable activation generation。
- State：compiled state execution owner index + 当前activation generation。
- ActionInstance：compiled action scope owner index + ActionInstanceId。
- Frame：compiled frame scope owner index + SimulationTick。

Frame Begin只设置当前generation上下文，不扫描scope。Read时token不匹配则返回declaration default，但不写transaction、不生成provenance。第一次真实Write才写value、token和`BlackboardWriteStamp`。Frame End只flush当前generation的projection candidate并结束上下文，不全量clear。

Graph/State/Action completion继续由各自正式lifecycle结束generation，不能依赖Frame自动清理。

### Projection boundary

ActionWindow projection只接受当前generation的真实Write和typed stamp。默认值读取、旧generation值和没有Action Context的写入不能生成Fact。

### Tradeoff

- 收益：没有Decision写入的Tick不dirty Frame Blackboard page；owner比较不再分配字符串。
- 代价：State layout、LayoutHash、State codec、Snapshot和产品manifest全部变化。
- 不选择只把字符串做intern：仍会保留运行时路径拼接、序列化和字符串等价判断，也不能让Frame默认读取保持无写入。

## Decision 7: Evaluate到Finalize持有唯一Actor Output Lease

### Current ownership

```text
Evaluate workspace lists
  -> Pending copies + ReadOnlyCollection
  -> workspace End/clear
  -> Finalize workspace lists
  -> AddRange Pending
  -> Result copies + ReadOnlyCollection
```

### Decision

Actor workspace的lease覆盖完整：

```text
Evaluate Begin
  -> operation output builders
  -> Pending lease
  -> World ResolveBatch只读取WorldRequest
  -> Finalize复用同一builders
  -> final result freeze
  -> lease release
```

`PendingCharacterEvaluation`保存：

```text
ActorId
Tick
Program/Layout/KernelProgramBinding
LeaseGeneration
StateTransaction
WorldRequest
Diagnostics flag
EntryOperation
```

Pending不拥有Facts、Presentation或Trace副本，也不向Pipeline暴露可变builder。Finalize通过ActorId、Tick、binding和lease generation重新取得唯一workspace，追加Motion Fact与Finalize Trace，Commit state，然后一次性冻结最终Result。

失败规则：

- lease未结束时同Actor不能再次Evaluate。
- Finalize的Actor/Tick/binding/generation不匹配时fail-fast并Abort transaction。
- World Resolve失败、Finalize异常或outer transaction Abort都必须释放lease并清空builders。
- Snapshot、History、Network、diagnostics只能引用最终immutable result或在自己的持久边界复制canonical bytes。

### Tradeoff

- 收益：删除Pending的三组集合复制和Finalize前的反向AddRange。
- 代价：Actor workspace在World Resolve期间保持占用；当前Pipeline本来就要求每Actor每Step只有一个pending evaluation，因此不降低合法并发。
- 不选择让Pending直接成为Result：WorldResult、BodySample和Finalize Fact尚未产生，仍会导致追加到已发布集合或第二次复制。

## Decision 8: Pipeline working state持有canonical state-set引用

### Decision

`Float32PipelineWorkingState`与`FixedPipelineWorkingState`改为只持有当前canonical `SimulationWorldStateSet`引用：

```text
Current
  LastCompletedTick
  Actors
  WorldState
```

规则：

1. 创建working state时直接引用StateStore.Current。
2. BeginSimulationStep把`Current`发布到working-state port。
3. CompleteStep排序/校验actor result后只构造一次next candidate。
4. ApplyCompletedStep只把`Current`替换为completed step candidate引用。
5. 多step schedule中下一step直接读取上一candidate。
6. PublishWorkingState把同一`Current`交给StateStore。
7. Restore preparation构造一个完整restore candidate；working state原子替换该candidate，rollback恢复旧引用。WorldSolver restore仍由正式world participant负责。

Snapshot/StateHash只在execution plan明确要求时构造，不把candidate重新包装为等价state-set。

### Tradeoff

- 收益：删除`ToStateSet`和多次`FreezeActors`，并让candidate identity可审查。
- 代价：Character-only/World-only restore helper需要改成围绕完整candidate协调，不能继续分别重建working halves。
- 不选择把Actors/World直接暴露为mutable：outer transaction rollback依赖candidate引用不可变。

## Decision 9: 版本与发布必须整体断代

本change修改Semantic IR payload、Operation Set、Target Program、State value kind和State codec。必须整体升版：

```text
Frontend CompilerVersion
OperationSetVersion
Semantic IR ArtifactVersion / PayloadVersion
Float32 Target ABI
Fixed Target ABI
Float32/Fixed Program ArtifactVersion
Float32/Fixed ProgramFormatVersion
Float32/Fixed LayoutFormatVersion
Float32 Character State CodecIdentity
Fixed Character State CodecIdentity
Program Runtime / Kernel binding identity中受影响的版本
```

精确数字由实现时从当前`compiler/16`、operation set`/5`、Semantic IR`6/6`、Program artifact`8`、program format`10`、layout format`5`、Float32 ABI`2`、Fixed ABI`1`连续提升，不允许复用旧数字表达新payload。

正式发布顺序：

```text
Authoring
  -> new .csir
  -> new Float32 Program
  -> new Fixed Program
  -> ProgramAsset / Projection binding
  -> product manifests
```

旧artifact、State、Snapshot、History或manifest必须在composition前失败。不得实现old-reader、migrator、fallback artifact search或双版本runtime。

### Tooling

- Semantic IR Inspector新增Value Inputs section。
- Portable Reader的semantic-ir/program命令新增`value-inputs` section和count。
- Reader显示target operation、target port、resolved kind以及source operation/output或constant index。
- SourceMap继续指向原authoring port/declaration，不按runtime layout index制造第二套作者身份。

## Failure Handling

- Operation port合同缺失、端口不存在、类型不匹配、重复source或受约束类型无法解析：Frontend build失败，不发布`.csir`。
- Target不能降低resolved kind或constant：Target build失败，不发布Program/Projection。
- Layout发现Program table不canonical或owner关系不唯一：Program Runtime composition失败，不进入Session。
- Kernel binding不匹配：Program Runtime创建失败，不进入Evaluate。
- dirty page ownership、output lease或candidate identity不匹配：当前outer transaction失败并走正式Abort，不吞异常、不恢复旧路径。

## Migration And Deletion

实现完成后删除：

- `/constant/port:` identity生成与runtime parser。
- Value input runtime `HashSet`、ordered key/value sort和旧input buffer字段。
- Timeline/State owner全Program扫描。
- Kernel Evaluate/Finalize的operation list兼容扫描。
- State Transaction dirty Dictionary和Commit二次page clone。
- Blackboard字符串owner/provenance state、BeginFrame reset与EndFrame clear helper。
- Pending output集合owner与Evaluate结束时提前清空workspace的路径。
- Working state `ToStateSet`、重复`FreezeActors`和等价candidate重建。
- 旧Semantic IR/Program/State reader和旧generated artifact。

## Stop Conditions

- 如果现有authoring port metadata无法为全部可达operation解析稳定类型，必须停止并列出缺失operation/port，不能把Unknown当成功。
- 如果Fixed与Float32现有Value转换语义不同，必须先明确业务上哪一套是Operation Set真值，不能各自保留不同contract。
- 如果任何Snapshot、History、Network或Presentation consumer当前直接持有Pending/workspace collection，必须停止说明迁移tradeoff，不能让lease越过正式Result边界。
- 如果正式Editor build无法安全重建Corin和三个产品artifact，必须停止说明缺口，不能手改generated bytes或保留旧ABI资产。

## Expected Runtime Flow

```text
Program Runtime composition
  -> validate Program once
  -> build/reuse ProgramExecutionLayout
  -> build KernelProgramBinding

Logic Tick
  -> working.Current
  -> Actor workspace lease Begin
  -> State Transaction Begin
  -> Value input span evaluation
  -> active Timeline/State direct index query
  -> typed Blackboard generation read/write
  -> Pending(WorldRequest + lease identity)
  -> World ResolveBatch
  -> Finalize same lease
  -> page ownership transfer
  -> one Result freeze
  -> one completed state candidate
  -> working.Current = candidate
  -> Commit/Publish same candidate
```

# Simulation Tick 热路径实现清单

## 当前正式身份

- Character Semantic Frontend：`character-simulation-compiler/17`
- Operation Set：`character-gameplay-operations/6`
- Semantic IR artifact/payload：`7/7`
- Float32 Numeric ABI：`3`
- Fixed Q32.32 Numeric ABI：`2`
- Float32/Fixed Program artifact/program/layout：`9/11/6`
- Float32 Character State codec：`character-state/float32/v4`
- Fixed Character State codec：`character-state/fixed-q32.32/v3`

Corin 正式产物当前包含 998 个 operation、569 个结构化 Value input、933 条 control-flow edge、433 条 reference、1398 个 state slot、9 个 scope、25 个 producer。Semantic IR、Float32 Program 与 Fixed Program 通过 portable Reader 读取到相同的 569 条 Value input。

## 六条唯一 Owner 链路

### 1. Value 输入路由

```text
OperationValuePortContract
  -> Semantic Value edge / SemanticConstantInputBinding
  -> Float32 / Fixed Program binding
  -> ProgramExecutionLayout contiguous input span
  -> Float32ValueRuntime / FixedValueRuntime
```

- linked input 的唯一真值仍是 `ProgramControlFlowEdge(kind=Value)`。
- 未连接端口的唯一真值是 `SemanticConstantInputBinding` 与 Target `ProgramConstantInputBinding`。
- `CharacterSimulationProgramValueResolver` 统一承担 Target Program 的 linked source kind 解析；Runtime Layout 与 portable Reader 不再各自维护第二套类型判断。
- Tick 内读取只使用 `OperationValueInputRange` 和连续 `CompiledValueInputBinding`，不解析 constant identity，不构造端口集合，不排序字符串。

### 2. Program Layout 与 Kernel Binding

```text
CharacterSimulationProgram
  -> shared ProgramExecutionLayout + ProgramLayoutIdentity
  -> backend KernelProgramBinding
  -> Actor runtime port
  -> O(1) Evaluate / Finalize binding check
```

- `ProgramExecutionLayout` 只保存 Program 固有索引，不包含 Pipeline、Source、Solver、Network Model 或 backend identity。
- Float32/Fixed Program Runtime 在 composition 时各自创建并封存 `KernelProgramBinding`。
- Timeline handle、Timeline child owner、State execution owner、固定语义 edge、reference、named constant 和 SourceMap path 都在 Layout 构建阶段索引。

### 3. Character State Transaction

```text
committed CharacterSimulationState
  -> Actor transaction workspace epoch
  -> first-write page copy
  -> WorkspaceOwned page
  -> take-ownership immutable CharacterStatePage
  -> committed candidate state
```

- dirty partition/page metadata属于 Actor workspace，不按 Tick 新建 Dictionary。
- 同一 transaction 的同一 page 只从 base state 复制一次。
- Commit 使用 `CharacterStatePage(values, true)`移交数组所有权，随后清除 workspace 可写引用。
- Abort 不修改 base state；Published page 不返回可写池。

### 4. Blackboard Owner 与 Provenance

```text
compiled ProgramScopeLayout
  -> BlackboardOwnerToken(scope, owner index, generation)
  -> BlackboardWriteStamp
  -> Character State
  -> current-generation projection
```

- Character/Config generation 在初始 State 建立；Graph、State、Action 来自正式 lifecycle；Frame 使用 SimulationTick。
- generation 不匹配时读取 declaration default，不物理写回 State。
- 第一次真实写入才 materialize value、typed owner 与 typed provenance。
- BeginFrame/EndFrame 只管理 projection scratch，不全量 reset/clear Blackboard scope。

### 5. Evaluate/Finalize Output Lease

```text
Actor workspace Begin
  -> ActorOutputWorkspaceLease
  -> Evaluate append
  -> Pending keeps lease + transaction + WorldRequest
  -> Finalize append
  -> one SimulationActorTickResult freeze
  -> lease release
```

- Pending 不复制 GameplayFact、PresentationCommand 或 Trace。
- World ResolveBatch 只读取独立 `CharacterWorldSolveRequest`。
- World Resolve、Finalize 或 outer transaction 失败都会 Abort transaction 并释放未消费 lease。
- Snapshot、History、Network 与 Diagnostics 只消费最终 immutable result 或自己的 canonical copy。

### 6. Pipeline Canonical Candidate

```text
StateStore.Current
  -> PipelineWorkingState.Current
  -> one completed-step SimulationWorldStateSet candidate
  -> ApplyCompletedStep replaces reference
  -> PublishWorkingState publishes same reference
```

- multi-step schedule 直接把上一个 candidate 作为下一 step 输入。
- restore preparation 构造完整 restore candidate，并由正式 Character/World/Pipeline participant 原子替换。
- Snapshot 与 StateHash 只在 execution plan 明确要求时生成。

## Artifact 与工具

- `CharacterSemanticIrInspectorWindow` 的 Value Inputs 同时展示 linked edge 与 constant binding，并保留 SourceMap 导航。
- `ThirdPersonSimulation.Reader` 支持 `semantic-ir`、`program` 和 `fixed-program`，text/JSON 均展示完整 Value Inputs。
- Target Program 成功发布新 ABI 后，发布事务删除同 Numeric Profile 的旧 ABI `.csim`；当前 Corin 目录只保留 `float32-ieee754-abi3.csim`。
- `.csir`、Float32 `.csim`、Fixed `.fixed-program` 均由正式 compiler/codec 生成和读取；没有旧 payload reader 或 runtime compatibility parser。

## 已删除的旧热路径

- `/constant/port:` constant identity 协议与 runtime parser。
- Tick 内 Value input `HashSet`、字符串排序、Substring 与 incoming edge临时集合。
- Tick 内全 Program Timeline/State owner扫描。
- Float32/Fixed dirty page Dictionary与Commit二次page copy。
- Frame Blackboard物理reset/clear与字符串owner/provenance state。
- Pending output三组中间复制和Finalize前AddRange回填。
- Pipeline working state `ToStateSet`、重复FreezeActors和等价state-set包装。

## 保留复制的正式边界

以下位置需要稳定所有权，复制不是热路径遗留：artifact canonical encoding、composition冻结、最终`SimulationActorTickResult`发布、Snapshot/History、Network payload与按需Diagnostics输出。

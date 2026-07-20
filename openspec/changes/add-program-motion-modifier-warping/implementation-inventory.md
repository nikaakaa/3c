# Motion Modifier 与 MotionWarp 实现清单

## 版本基线

- Frontend：`character-simulation-compiler/18`
- Operation Set：`character-gameplay-operations/7`
- Semantic IR artifact/payload：`8/8`
- Float32 Target ABI：`4`
- Fixed Q32.32 Target ABI：`3`
- Target Program artifact：`10`
- Float32 State codec：`character-state/float32/v5`
- Fixed State codec：`character-state/fixed-q32.32/v4`
- Agent schema：`agent-character-controller-synthesis.v11`

旧 identity reader 已删除，不提供兼容读取或运行时 fallback。

## 当前 Motion 正式链路

Float32 与 Fixed 当前结构相同：

```text
Locomotion / TimelineMotionCurve / GameplayResult
  -> SimulationMotionContribution
  -> Float32MotionAccumulator / FixedMotionAccumulator
  -> ResolvedMotionChannel
  -> Operation Set canonical Motion Modifier span
  -> TimelineMotionWarp（仅合法 Action channel descriptor）
  -> 固定 channel 顺序合成
  -> CharacterMotionRequest
  -> Float32WorldResolveBatchPass / FixedWorldResolveBatchPass
  -> ICharacterWorldSolver.ResolveBatch
  -> actual Body Result
```

关键入口：

- Float32 contribution、channel resolve 与 request：`Runtime/Simulation/Core/Float32/Execution/Float32MotionRuntime.cs`
- Fixed contribution、channel resolve 与 request：`Runtime/Simulation/Core/Fixed/Execution/FixedMotionRuntime.cs`
- Float32 Solver batch：`Runtime/Simulation/Core/Float32/Pipeline/Float32WorldResolveBatchPass.cs`
- Fixed Solver batch：`Runtime/Simulation/Core/Fixed/Pipeline/FixedWorldResolveBatchPass.cs`
- Unity Solver：`Runtime/Simulation/Unity/UnityCharacterControllerWorldSolver.cs`

WorldSolver仍只消费最终`CharacterMotionRequest`，不认识Timeline、Action、MotionWarp或目标快照。

## MotionCurve 正式链路

```text
MotionCurveTrack / MotionCurveClip
  -> CharacterSimulationTimelineEmitterRegistry
  -> TimelineMotionCurve Semantic operation
  -> Float32 / Fixed Target lowering
  -> TimelineControlRuntime
  -> Float32TimelineTarget / FixedTimelineTarget
  -> SimulationMotionContribution
```

Authoring 曲线由 Frontend 的 `BakeCurve` 编为 portable curve constant。Target runtime只采样编译产物，不读取 Unity `AnimationCurve`。

## ActionTargetSnapshot 正式链路

```text
owner-local Pipeline Blackboard declaration
  -> ActivateActionInstanceNode.TargetSnapshotVariable
  -> ProgramExecutionLayout typed state address
  -> Float32ActionRuntime / FixedActionRuntime
  -> immutable ActionActivationRequest
  -> immutable ActionInstance.TargetSnapshot
  -> Character State codec / Snapshot / Hash
```

`CanActivateActionInfoNode`与`ActivateActionInstanceNode`均保存显式target snapshot reference，并通过同一个portable admission request与evaluator判断`SnapshotRequired`。Compiler与Agent共用`ActionTargetAuthoringValidation`核对同一准入/激活链的target declaration、Action Context和Warp profile要求。

## MotionWarp 正式链路

`Runtime/BTSMTL/Timeline/Scripts/Timeline.MotionWarp.cs`现在只保留authoring model与唯一校验服务。正式执行链为：

```text
MotionWarpTrack / MotionWarpClip
  -> CharacterSimulationTimelineEmitterRegistry
  -> TimelineMotionWarp Semantic operation + typed MotionSourceOperation reference
  -> ProgramMotionModifierCompiler descriptor
  -> Float32 / Fixed Program
  -> Resolved Action Motion channel
  -> ProgramMotionModifierRuntime
  -> CharacterMotionRequest
```

旧`TimelineMotionWarpWindow`、`MotionWarpTrack.Sample()`、`TrySampleClip()`、`TargetKey`与Base Clip ease/weight Gameplay采样均已删除。Warp跨Tick状态进入typed Program State，并由State codec、Snapshot、Hash和rollback统一覆盖。

## Action目标要求

旧字符串`TargetPolicy`已经删除，正式字段为：

```text
ActionTargetRequirement.None
ActionTargetRequirement.SnapshotRequired
```

Corin Attack与Dodge都已迁移为`None`。仓库当前没有MotionWarp实例和正式target provider，因此正式Corin Program中的`MotionModifiers=0`；本change不伪造目标或Warp配置。

## 资产盘点

- `MotionWarpTrack`：0 个序列化实例
- `MotionWarpClip`：0 个序列化实例
- 旧 `TargetPolicy`：0 个序列化实例

因此不创建 migrator。新 authoring model 安装后直接删除旧字段和旧 reader。

## Preview 与 Agent 入口

- Timeline Authoring Preview：`TimelinePreviewSession`、`TimelineEditorView`、`TimelinePreviewTarget`
- Gameplay Preview：`CharacterPipelinePreviewController`、`PreviewSession`、`PreviewPlaybackEngine`
- Live Debug：`TimelineEditorWindow`、共享 `RuntimeDebugSession` provider
- Agent Snapshot/Patch：`AgentAuthoringModels`、`AgentGraphSnapshotExporter`、`AgentPatchCommandLowerer`
- Agent mutation：typed command catalog、handler catalog、Timeline正式authoring API
- Agent validation：`AgentGraphValidator`
- MCP bridge：唯一 `manage_btsmtl_agent_authoring`

## 已删除路径

- 旧 MotionWarp DTO、sampler、curve helper和TargetKey字段
- MotionWarp对Base Clip ease/weight的Gameplay采样语义
- ActionProfile字符串TargetPolicy及其Inspector/catalog reader
- 旧 Operation Set、Semantic IR、Float32/Fixed Program与State codec兼容reader
- 旧 ABI artifact读取入口和旧产品identity
- 任何 scene Transform、Presentation、Solver、Network Model专用Warp旁路

不得恢复 BBB `WarpedMotionData`、`MotionProposal`、旧 `CharacterMotionStage` 或第二个 Motion runtime。

## 正式产物基线

当前正式Corin产物由同一authoring source分别生成：

- Semantic IR：Frontend`/18`、Operation Set`/7`、artifact/payload`8/8`
- Float32 Program：ABI`4`、State codec`character-state/float32/v5`
- Fixed Program：ABI`3`、State codec`character-state/fixed-q32.32/v4`

每次Compiler、Operation Set、ABI或source revision变化后，三个产品Build入口都必须重新生成并拒绝旧identity，不提供兼容读取。

## 性能与模块边界收口

正式采样入口为`Tools/3C/Diagnostics/Capture Simulation Performance (10s)`。它使用Gameplay Tick实际差值统计节奏，使用Profiler marker统计Pipeline、Kernel、Operation、WorldSolver与Presentation耗时，并通过Unity正式`GC Allocated In Frame` counter记录全帧分配；不再把Profiler Recorder样本数误当实际Tick或表现帧数。

StandaloneGameplay同场景10秒采样结果：

| 指标 | 收口前 | 收口后 | 最终复测 |
| --- | ---: | ---: | ---: |
| Logic Tick | 59.985 Hz | 60.090 Hz | 60.078 Hz |
| Presentation | 119.871 FPS | 119.681 FPS | 119.856 FPS |
| Session LogicTick平均 | 1.2011 ms | 1.2373 ms | 1.2534 ms |
| ControlTick平均 | 0.3496 ms | 0.3629 ms | 0.3600 ms |
| Animation平均 | 0.1464 ms | 0.1449 ms | 0.1484 ms |
| PendingLease平均 | 未采集 | 未采集 | 0.0025 ms |
| 全帧GC平均 | 107869 B | 104207 B | 101424 B |

最终复测报告为`Library/Performance/Simulation-20260720-081342.json`，并确认修正后的`ThirdPerson.Simulation.Kernel.PendingLease` marker可被正式采集。三次CPU差异处于Editor采样波动范围，不能宣称运行耗时下降，但确认模块拆分与分配清理没有改变60 Hz逻辑、约120 FPS表现或形成节奏回归。Unity 2022当前Mono运行时的`GC.GetAllocatedBytesForCurrentThread`校准为0，不能用于逐phase归因，因此没有保留会输出虚假0 B结果的诊断路径；全帧GC数据明确标为包含Editor、Profiler与MCP开销。

已消除四类可由代码确定的执行瞬时分配：常规State执行改用值类型作用域，ForceStop复用每Actor访问集合，Float32/Fixed内部`CharacterOperationEvaluation`改为仅携带事务引用与Motion值的只读值类型，ExecutionPlan对已排序Actor/roster的成员校验改用二分查找而不再为每个Step创建两个`HashSet`。Float32与Fixed的不可变`ProgramExecutionLayout`继续按Program只构建一次，每Actor可变`EvaluationWorkspace`与GameplayEffect/Motion scratch已迁入各自独立模块；两侧的State访问策略与`ProgramExecutionServices`也已从布局文件迁入各自数值目标的独立执行服务文件。该拆分只收紧职责和Actor可变状态所有权，不改变Program ABI、Tick顺序或唯一执行链，也不据此宣称CPU性能提升。

Timeline动画producer代表clip现在由数值无关的`TimelineAnimationProducerIndex`在Program布局初始化时一次性验证、按identity排序并冻结；`SampleAnimationProducers`与terminal输出不再每Tick创建`SortedDictionary`、扫描全部clip和进行字符串去重。连续两轮优化后复测的TimelineDecision平均为0.0614/0.0636 ms，和优化前0.0649 ms处于同一Editor波动区间，因此只确认删除了确定的容器分配并且没有节奏回归，不宣称CPU耗时显著下降。

Float32与Fixed的`GameplayEffectTarget`也已按核心状态操作、Application Admission端口、Control端口、公共映射和运行时变更记录拆分为同一partial类型的独立模块；两个主Target文件分别由1228/1235行降至506/507行。拆分没有新增Target对象、转发服务或第二条GameplayEffect路径，仍由同一个`GameplayEffectControlRuntime`消费同一状态事务。

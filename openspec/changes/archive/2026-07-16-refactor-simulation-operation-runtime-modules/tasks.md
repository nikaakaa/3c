## 1. 锁定现状与迁移边界

- [x] 1.1 使用 UTF-8 重读本 change 的 proposal、design、tasks 与 spec deltas。
- [x] 1.2 核对 6 个 `SimulationOperationMachine` partial 文件、4310 行统计和当前外部构造调用。
- [x] 1.3 列出全部 `SimulationOperationCode`，为每个 code 标记 portable control 或 Float32 leaf 唯一 owner。
- [x] 1.4 列出 Root、Loop、Sequence、Selector、Parallel、StateMachine、State 的全部方法和互调关系。
- [x] 1.5 列出 Runnable lifecycle、generation、cursor、active state、stop context 和 state execution path 使用的 StateSlot semantic。
- [x] 1.6 列出 Value、Blackboard、Action、Timeline、GE、Motion 使用的 StateSlot semantic 和 mutable collection。
- [x] 1.7 固定当前 Evaluate 调用顺序与每一步允许产生的 State/Fact/Presentation/Trace/Motion 输出。
- [x] 1.8 固定当前 Fact、Presentation、Trace 的 EventId 输入与追加顺序。
- [x] 1.9 固定 Timeline/Locomotion contribution 的 Channel、Priority、Weight、BlendMode 与 ConsumeLowerChannels 排序规则。
- [x] 1.10 核对 `ProgramExecutionLayout` 的构建和 Session 复用位置，禁止新 topology 每 Tick 重建。
- [x] 1.11 核对 `refactor-gameplay-session-composition-boundary` 对 `SimulationKernel` 的预期修改，锁定单一冲突调用点。
- [x] 1.12 若拆分需要改变 Program codec、State layout、canonical bytes 或 EventId，停止实施并修改 proposal。

## 2. 建立 portable Operation topology

- [x] 2.1 在 portable Core 建立 operation execution topology 模块目录和明确 assembly ownership。
- [x] 2.2 定义不包含 Scalar/Vector/Yaw/catalog payload 的 immutable operation descriptor。
- [x] 2.3 定义 root、control-flow edge、reference 与 semantic slot 的只读 topology contract。
- [x] 2.4 将 operation handle/code/integer/flags 中控制流需要的字段投影进 topology。
- [x] 2.5 将 Runnable lifecycle、cursor、generation、stop context、active state 所需 slot binding 投影进 topology。
- [x] 2.6 保持 topology 不包含 mutable Character state、Actor、Tick、Input、Fact 或 Presentation。
- [x] 2.7 让 Float32 `ProgramExecutionLayout` 在 Program 校验后一次构建 topology。
- [x] 2.8 复用现有 outgoing edge、incoming value、reference 和 operation slot 索引，删除重复 List/排序构建。
- [x] 2.9 对 operation count、handle、root、edge endpoint、reference endpoint 和 slot index 做严格一一校验。
- [x] 2.10 让 Session 的每个 Program layout 唯一持有并复用 topology。
- [x] 2.11 禁止 topology 序列化到 Program、Snapshot、Assets 或 Library。
- [x] 2.12 确认 topology 不参与 ProgramHash、LayoutHash、StateHash 或 EventId。

## 3. 建立 portable control Target port

- [x] 3.1 定义 Target control state cell 的最小读取合同。
- [x] 3.2 定义 Target control state cell 的最小写入合同。
- [x] 3.3 定义 Condition 求值合同，只返回结构控制所需 Bool。
- [x] 3.4 定义非控制流 Leaf operation 执行合同，返回统一 OperationResult。
- [x] 3.5 定义 operation activation、completion、state clear 的 scope lifecycle hook。
- [x] 3.6 定义结构 Trace sink，不暴露 Fact/Presentation/Motion sink。
- [x] 3.7 定义窄 Operation control cursor，限定 Tick、RequestStop、ContinueStop、ForceStop 和 IsActive。
- [x] 3.8 禁止 Target port 暴露 Unity、Network Model、packet、Animancer 或 authoring object。
- [x] 3.9 使用受约束值类型 Target adapter，避免 hot path boxing 和反射调用。
- [x] 3.10 禁止每 operation 创建 delegate、handler object 或 service lookup。
- [x] 3.11 对非法 slot kind、未知 handle、缺失 child 和越权 cursor 调用明确失败。
- [x] 3.12 建立 Float32 Target adapter 骨架并只引用 Float32 Program/State/Frame。

## 4. 迁移 Runnable、Composite 与 StateMachine 控制流

- [x] 4.1 将 `SimulationRunnableStatus`、`SimulationOperationResult`、`SimulationStopContext` 等控制类型迁入 portable control module。
- [x] 4.2 迁移 Runnable enter、Running、Success、Failure 与 Stopping 状态推进。
- [x] 4.3 迁移 activation generation 递增与读取。
- [x] 4.4 迁移 operation activation/completion scope hook 调用顺序。
- [x] 4.5 迁移 execution budget 计数与溢出失败。
- [x] 4.6 迁移 Root 与 single-child control flow。
- [x] 4.7 迁移 Loop 的 Running/Success/Failure 规则。
- [x] 4.8 迁移 Sequence cursor、condition、child completion 与 pending stop 规则。
- [x] 4.9 迁移 Selector cursor、self/lower-priority abort 与 replacement 规则。
- [x] 4.10 迁移 Parallel active child、result policy 与全部 descendant stop barrier。
- [x] 4.11 迁移 StateMachine active state、entry state 与 transition selection。
- [x] 4.12 迁移 State enter/update/exit、state execution path 与 exit cause。
- [x] 4.13 迁移 transition graceful stop、continue transition 与 replacement activation。
- [x] 4.14 迁移 RequestStop、ContinueStop、ForceStop 与 descendant propagation。
- [x] 4.15 迁移 operation local state reset、State scope clear 与 StateMachine execution path clear。
- [x] 4.16 保持 LowerPriority、Self、Both、ParentStop、StateTransition、Reset、Shutdown 和 ActionContextEnded cause 不变。
- [x] 4.17 保持 control runtime 不产生 Animation、Camera、Cue、GE、Motion、Fact 或 Network 输出。
- [x] 4.18 保持结构 Trace code、source operation 与追加顺序不变。

## 5. 建立 Float32 Evaluate 事务帧

- [x] 5.1 建立 `Float32EvaluationFrame` 并绑定 Program、Layout、Topology、Actor、Tick、Input、Ingress 与 Body。
- [x] 5.2 将 `CharacterSimulationStateBuilder` 收口进 Frame 的 state access port。
- [x] 5.3 建立 Fact sink 并保持当前 EventId/local sequence 生成规则。
- [x] 5.4 建立 Presentation sink 并保持当前追加顺序。
- [x] 5.5 建立 Trace sink 并保持当前 source mapping。
- [x] 5.6 建立 Motion contribution sink，不允许直接生成 WorldSolverResult。
- [x] 5.7 将 Value recursion stack 收口到 Value module 所有权。
- [x] 5.8 将 state execution context 改由 portable control runtime 所有。
- [x] 5.9 让 Frame 只暴露模块所需的窄 port，不提供万能 public Context。
- [x] 5.10 禁止 Frame 持有跨 Tick mutable state 或第二份 Action/GE/Blackboard 状态。
- [x] 5.11 保持 `CharacterOperationEvaluation` 输出合同不变。
- [x] 5.12 保持 Frame failure 不发布部分 Character state 或外部输出。

## 6. 拆分 Float32 Value、Input 与通用输出

- [x] 6.1 建立 Float32 Value module 并迁移 value operation dispatch。
- [x] 6.2 迁移 Constant、Convert、Compare、Boolean 和 arithmetic value 规则。
- [x] 6.3 迁移 incoming value edge 读取与 output port 选择。
- [x] 6.4 迁移 InputId、InputValueKind、move facing angle 与 discrete request 读取。
- [x] 6.5 迁移 Value recursion detection 并保持循环失败语义。
- [x] 6.6 让 portable control runtime 只通过 Float32 Target adapter 请求 Condition Bool。
- [x] 6.7 迁移通用 source path、constant lookup 与 handle parse/format helper 到明确 owner。
- [x] 6.8 删除 Value module 对 Action、Timeline、Presentation 与 Motion 具体实现的访问。

## 7. 拆分 Blackboard 与 Action 模块

- [x] 7.1 将 Blackboard slot group、Timeline Blackboard context 和 projection candidate 类型移出旧 machine 嵌套关系。
- [x] 7.2 建立 Blackboard module 并迁移 Frame begin/end cleanup。
- [x] 7.3 迁移 Character、Graph、State、ActionInstance 与 Frame scope activation/clear。
- [x] 7.4 迁移 declaration/layout/address/lifetime 校验。
- [x] 7.5 迁移 typed Blackboard read/write 与 provenance 构造。
- [x] 7.6 迁移 Timeline Blackboard context push/pop 和 Action Context 校验。
- [x] 7.7 迁移 ActionWindow projection candidate 收集与统一 flush。
- [x] 7.8 保持同 declaration、多 ActionInstance provenance 的独立事实投影。
- [x] 7.9 建立 Action module 并迁移 activation request 处理。
- [x] 7.10 迁移 Action activation validation、profile/tag query 和 target snapshot。
- [x] 7.11 迁移 Action lifecycle ingress、transition、state slot 和 fact 输出。
- [x] 7.12 将 Action request/instance codec 类型移出旧 machine 嵌套关系并保持 canonical bytes。
- [x] 7.13 让 Action module 只通过只读 GE Tag query port 获取 tag。
- [x] 7.14 让 Blackboard module 只通过 Action Context reader 获取 Action identity。
- [x] 7.15 删除 Blackboard 对 `SimulationOperationMachine.ActionInstanceState` 的引用。
- [x] 7.16 删除 Action 与 Blackboard 对旧 machine 私有字段的访问。

## 8. 拆分 GameplayEffect operation bridge

- [x] 8.1 建立 GameplayEffect operation bridge 并绑定当前 CharacterSimulationState GE slots。
- [x] 8.2 迁移 GE ingress 应用、Advance 与 Save 的 Evaluate 顺序。
- [x] 8.3 迁移 Apply/Remove operation 到正式 `SimulationGameplayEffectRuntime` 调用。
- [x] 8.4 迁移 Attribute/Tag query 到只读 GE query port。
- [x] 8.5 迁移 GE change 到 Fact/Presentation/Trace 的统一 projection。
- [x] 8.6 保持 handle allocator capture/restore 与失败原子性。
- [x] 8.7 保持 GameplayCue producer 查找与 EventId 顺序。
- [x] 8.8 确认 bridge 不复制 ActiveEffect、Attribute、Tag、journal 或 prediction state。
- [x] 8.9 确认本 change 不拆分或重写 `SimulationGameplayEffectRuntime` 规则。

## 9. 拆分 Timeline 与 Motion

- [x] 9.1 将 Timeline segment 与 Motion contribution 类型移出旧 machine 嵌套关系。
- [x] 9.2 建立 Timeline module 并迁移 Decision Timeline preparation。
- [x] 9.3 迁移 Timeline start/time/loop/complete 状态推进。
- [x] 9.4 迁移 Timeline graceful completion、stop 与 force stop。
- [x] 9.5 迁移 TreeClip Decision/Commit lifecycle 和 child control cursor 调用。
- [x] 9.6 迁移 Timeline Action Context 捕获、失效与 ActionContextEnded stop。
- [x] 9.7 迁移 Animation producer select/sample/release/terminal command。
- [x] 9.8 迁移 Cue、Camera continuous 与 Camera cue command。
- [x] 9.9 迁移 Timeline curve、clip weight、ease、loop segment 与 Tick epsilon 采样。
- [x] 9.10 让 Timeline MotionCurve 只提交 MotionContribution。
- [x] 9.11 让 Locomotion operation 只提交 MotionContribution。
- [x] 9.12 建立唯一 Float32 Motion accumulator。
- [x] 9.13 迁移 Channel/Priority/Weight/BlendMode/ConsumeLowerChannels 解析。
- [x] 9.14 保持 contribution 稳定排序、空间转换、displacement 与 yaw 计算不变。
- [x] 9.15 由 Motion accumulator 唯一生成并写入 `CharacterMotionRequest`。
- [x] 9.16 保持 pending motion canonical bytes 不变。
- [x] 9.17 删除 Timeline module 对最终 WorldSolver、Transform 或 Presentation playback 的访问。
- [x] 9.18 删除旧 Timeline 文件中 Locomotion 和最终 Motion 汇总混合职责。

## 10. 建立唯一 Float32OperationEvaluator

- [x] 10.1 建立 `Float32OperationEvaluator` 唯一 Evaluate 入口。
- [x] 10.2 按锁定顺序接通 Blackboard begin、Ingress、GE advance、Input request、Decision Timeline、Root control、Motion、GE save 与 Blackboard end。
- [x] 10.3 建立 Float32 Target dispatcher，将 control code 与 leaf code 明确分流。
- [x] 10.4 对所有 versioned operation code 建立穷尽 owner 映射。
- [x] 10.5 未知或未实现 operation code 明确失败，不跳过或回退 Success。
- [x] 10.6 让 `SimulationKernel.Evaluate` 只调用 `Float32OperationEvaluator`。
- [x] 10.7 保持 `PendingCharacterEvaluation`、WorldRequest 与 Finalize 输入不变。
- [x] 10.8 保持 `ProgramExecutionLayout` 与 topology 只按 Program 构建一次。
- [x] 10.9 禁止每 Tick 构建 handler dictionary、operation registry、edge list 或 reference list。
- [x] 10.10 禁止领域模块直接调用另一个领域模块的 concrete implementation。
- [x] 10.11 确认 Preview、Local、后续 ServerAuthoritative 都通过同一 Kernel/Evaluator。
- [x] 10.12 确认 Network Model、Driver、Solver 与 Presentation 不引用领域模块内部类型。

## 11. 删除旧单体执行器

- [x] 11.1 删除 `SimulationOperationMachine` 类型。
- [x] 11.2 删除 `SimulationOperationMachine.cs` 旧实现文件。
- [x] 11.3 删除 `SimulationRunnableLifecycle.cs` 中的旧 partial 实现。
- [x] 11.4 删除 `SimulationTimelineOperationRuntime.cs` 中的旧 partial 实现。
- [x] 11.5 删除 `SimulationBlackboardRuntime.cs` 中的旧 partial 实现。
- [x] 11.6 删除 `SimulationActionRuntime.cs` 中的旧 partial 实现。
- [x] 11.7 删除 `SimulationGameplayEffectMachine.cs` 中的旧 partial 实现。
- [x] 11.8 删除旧 machine 嵌套 DTO、scope helper 与类型限定引用。
- [x] 11.9 删除旧构造器、wrapper、type alias、feature flag 和兼容 dispatch。
- [x] 11.10 使用 `rg` 确认仓库不存在 `partial class SimulationOperationMachine`。
- [x] 11.11 使用 `rg` 确认仓库不存在 `new SimulationOperationMachine`。
- [x] 11.12 使用 `rg` 确认只有一个 Float32 Evaluate operation 入口。
- [x] 11.13 使用 `rg` 确认只有一个最终 Motion accumulator。
- [x] 11.14 使用 `rg` 确认 portable control runtime 不引用 Float32、Unity、Network 或 Presentation 类型。

## 12. 文档、下游约束与校验

- [x] 12.1 更新 `openspec/project.md` 的 Code Organization 与 Kernel 描述，记录 portable control runtime 和 Float32 领域模块。
- [x] 12.2 更新 `add-deterministic-rollback-kcc-model` proposal 依赖本 change。
- [x] 12.3 更新 `add-deterministic-rollback-kcc-model` design，明确 Fixed 复用 portable control runtime、只实现 Target numeric leaf backend。
- [x] 12.4 更新 `add-deterministic-rollback-kcc-model` tasks，删除复制 control flow evaluator 的含糊任务。
- [x] 12.5 核对 `refactor-gameplay-session-composition-boundary` 不承担 operation module 装配。
- [x] 12.6 核对 current `btsmtl-node-interruption-lifecycle` 与 portable control runtime 无矛盾。
- [x] 12.7 核对 current Action、Blackboard、GE specs 仍保持唯一 CharacterSimulationState 所有权。
- [x] 12.8 更新本 change tasks 勾选，使状态与实际实现一致。
- [x] 12.9 使用带 `--disable-build-servers /nr:false /p:UseSharedCompilation=false` 的命令编译 portable Core 与普通 .NET Reader。
- [x] 12.10 使用带 `--disable-build-servers /nr:false /p:UseSharedCompilation=false` 的命令编译 Float32、Assembly-CSharp 与 Editor 相关工程。
- [x] 12.11 编译结束后立即执行 `dotnet build-server shutdown`。
- [x] 12.12 运行 `openspec validate refactor-simulation-operation-runtime-modules --strict --no-interactive`。

## 13. 审查修复与质量闭环

- [x] 13.1 将 operation 与 Finalize Trace 改为独立 diagnostics sequence，禁止写入 `FactSequence`。
- [x] 13.2 核对 Trace 开关或数量变化不改变 staged State、StateHash 或后续 Gameplay/Presentation EventId。
- [x] 13.3 在 graceful stop 完成后按 `CompleteScopes -> ResetOperationState` 顺序清理 operation scope。
- [x] 13.4 将 portable `EmitStateFact` 改为中性 State lifecycle notification，由 Float32 Target 决定 Fact 投影。
- [x] 13.5 在 ProgramExecutionLayout 缓存 operation SourcePath、Timeline curve 与 state-access policy。
- [x] 13.6 建立 Actor 级 reusable evaluator/module graph 与 evaluation workspace，并确保每 Tick只重绑请求且清空全部临时集合。
- [x] 13.7 使用 `rg` 确认 Trace 不访问 `FactSequence`、graceful stop 不跳过 `CompleteScopes`、SourceMap/curve 不在每 Tick重建。
- [x] 13.8 使用规定参数编译 portable Core/Float32/Reader、Unity Core/Float32、Assembly-CSharp 与 Editor，并关闭 build server。
- [x] 13.9 运行 `openspec validate refactor-simulation-operation-runtime-modules --strict --no-interactive`。

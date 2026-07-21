# Change: 重构 Simulation Tick 热路径的编译索引与数据所有权

## Why

当前 Simulation Core 已经完成 Program、Kernel、WorldSolver、Pipeline、Session 和 Presentation 的正式分层，Float32 与 Fixed 也复用了同一套 numeric-neutral operation control。现在的性能问题不来自“仍在跑旧 Graph runtime”，而来自新通用运行时把多类编译期已知关系和临时数据所有权留到了每个 Tick 处理。

按当前工作树读取 Corin 正式 Float32 Program，真实基线为：

- Frontend `character-simulation-compiler/16`
- Operation Set `character-gameplay-operations/5`（apply 起点；本 change 的正式升版目标为 `/6`）
- 60Hz Logic Tick
- 998 个 operation
- 1572 个 constant
- 933 条 control-flow edge
- 433 条 reference
- 1426 个 state slot
- 14 个 Timeline operation
- 5114 条 source-map 记录
- Float32 ABI 2，Fixed ABI 1

旧提案记录的 875 个 operation、1427 个 state slot、810 条 edge、15 个 Timeline 和 4951 条 source-map 已经过时；旧文档中的具体毫秒和分配数字也没有当前受版本控制的正式 capture 作为证据，不能继续当成验收基线。

代码中仍存在以下确定的 Tick 热路径：

1. `Float32ValueRuntime` 与 `FixedValueRuntime` 每次读取输入都遍历 incoming Value edge，从 constant identity 截取 `/constant/port:`，建立端口 `HashSet`，按字符串排序，再复制为 values。
2. `TimelineControlRuntime` 每 Tick 扫描全部 operation 查找运行中的 Timeline，并在需要时再次扫描全部 Timeline 反查 child owner。
3. `OperationControlRuntime` 为 State execution path 扫描全部 operation 查找所属 StateMachine，并在多处使用运行时 LINQ/端口字符串选择固定语义 edge。
4. Kernel 在 Program Runtime 已经绑定 Program 后，仍在 Evaluate 与 Finalize 重复执行绑定查询；现有共享 `ProgramExecutionLayout` 与 backend-specific Kernel identity 的所有权没有被精确定义。
5. Character State Transaction 首次写 dirty page 时复制 base page，Commit 又通过 `new CharacterStatePage(values, false)` 复制同一页；dirty partition/page Dictionary 和 savepoint容器也由 transaction 每次新建。
6. Blackboard 每 Tick 遍历 scope，构造 actor/frame/graph/action/state字符串 owner；Frame scope在 BeginFrame全量写默认值，在EndFrame再次全量清空，即使本 Tick 没有 Decision TreeClip写入也会制造 dirty page。
7. Evaluate 先把 Facts、Presentation Commands、Trace复制进 `PendingCharacterEvaluation`，随后 Workspace立即 End；Finalize再把 Pending复制回 Workspace，并由 `SimulationActorTickResult`进行第二次正式冻结。
8. `Float32PipelineWorkingState` 与 `FixedPipelineWorkingState` 在创建、BeginSimulationStep、ApplyCompletedStep和PublishWorkingState之间重复冻结同一 Actor roster并构造等价 `SimulationWorldStateSet`。

这些问题都发生在正式唯一链路中，因此可以直接重构，不需要绕过 Session、Program、Pipeline、Kernel或Presentation。但旧提案有两个设计错误必须先修正：

- `ProgramExecutionLayout` 按 Program实例共享，只能持有 Program固有身份和静态索引，不能保存某一个 Local、Prediction、Unity Authority、DotRecast Authority或Rollback backend identity。
- Value输入不能只新增 `TargetPort + ConstantIndex` 表。Compiler还需要一个 numeric-neutral operation value-port合同，才能在 Semantic IR阶段验证 source output、target input、constant与受约束多态端口的实际类型。

## What Changes

- 在公共 Operation Set 中增加版本化、numeric-neutral 的 Value Port Contract，描述每个 operation 的 input/output port、固定类型或受约束类型组、顺序和允许转换；不依赖 Unity reflection、Float32或Fixed runtime类型。
- 保留 `ProgramControlFlowEdge(kind=Value)` 作为 linked input 唯一真值；新增结构化 Semantic Constant Input Binding，记录 target operation、target port、constant index与解析后的 value kind，不再从 constant identity推导端口。
- 在 Semantic IR构造、canonical codec、SemanticHash、Inspector和普通 .NET Reader中正式保存并展示 binding；提升 Frontend、Operation Set与 Semantic IR artifact版本，不读取旧 payload。
- Float32与Fixed Target Program分别降低同一 binding table，并提升 Target ABI、Program artifact/program/layout format、State codec identity及相关 manifest；不保留旧 reader、兼容映射或 runtime parser。
- `ProgramExecutionLayout` 在 composition时把 Value edge与constant binding合并为按 operation索引的连续只读 input span，并预索引 Timeline operations、Timeline child owner、State到所属StateMachine/execution owner、固定语义 edge、operation reference和named constant。
- 将共享 `ProgramLayoutIdentity` 与 backend-specific `KernelProgramBinding` 分开。Program Runtime创建 Kernel binding并一次性完成 operation-set/backend兼容校验；Evaluate/Finalize只核对同一 Program、Layout和Binding引用或紧凑 identity，不再扫描 Program。
- 将 Float32/Fixed Character State Transaction 的 dirty metadata迁入 Actor workspace，使用 layout-indexed slot与epoch复用；每个 dirty page从base state最多复制一次，Commit直接移交该数组给immutable page，并立即丢弃workspace可写引用。
- 将 Blackboard owner与write provenance从字符串改为类型化状态值。Program layout预解析scope owner address；Character/Config在初始State建立，Graph/State/Action由正式lifecycle生成generation，Frame使用SimulationTick作为generation。
- Frame Blackboard读取旧generation时返回declaration default但不写State；当前Tick第一次真实写入才materialize owner token、value和typed provenance。BeginFrame/EndFrame不再全量reset/clear scope。
- 让Actor workspace的output builder从Evaluate保持唯一lease到Finalize。Pending只保存lease身份、Transaction与WorldRequest；Finalize在同一builder追加后置Fact/Trace并恰好一次冻结正式`SimulationActorTickResult`。
- 让Pipeline working state直接持有canonical immutable `SimulationWorldStateSet`；每个completed step只创建一个candidate，Begin、Apply和Publish复用同一实例。
- 清理热路径中只为静态查询或临时投影产生的LINQ、`AsReadOnly`、`ToArray`、字符串端口/owner/source path构造；保留artifact、composition、最终发布、snapshot/history/network和异常诊断边界上必要的稳定复制。
- 保持60Hz Logic Tick、catch-up、WorldSolver、Network Model、PresentationFrame、动画生命周期和业务Graph语义不变。本change不通过降TickRate、扩大容量、吞异常或增加fallback来隐藏问题。

## Capabilities

### Modified Capabilities

- `btsmtl-gameplay-semantic-ir`：增加 numeric-neutral Value Port Contract 与 Semantic Constant Input Binding。
- `btsmtl-compiled-simulation-program`：增加 Target Program结构化 constant binding、版本身份和旧artifact拒绝规则。
- `btsmtl-semantic-ir-inspection`：让Unity Inspector与普通 .NET Reader展示结构化 Value输入。
- `character-simulation-kernel`：增加静态索引、Kernel Program binding、transaction page ownership和Evaluate/Finalize output lease合同。
- `character-pipeline-blackboard`：将runtime owner/provenance改为typed token，并用generation实现Frame逻辑失效。
- `gameplay-simulation-pipeline`：规定completed step candidate在working apply与state store publish之间保持同一canonical实例。

## Dependencies And Sequencing

- 依赖`refactor-gameplay-runtime-and-tooling-modules`当前工作树已经安装的`ProgramExecutionServices`、Actor/Session workspace、portable Timeline control和portable Pipeline coordinator。其6.14、6.15目前已正确保持未完成；本change真实完成相应清理后才能恢复勾选。
- 依赖current DeterministicRollback specs已经安装的Fixed Target、Fixed State、Fixed Kernel和Rollback产品；本change必须同时修改Float32与Fixed实现，不能只优化Float32。
- 依赖current Action specs已经安装的`ActionWindowActive`和`CanActivateAction`。`/5`是本change开始时的输入版本，正式产物升为`/6`；不能按旧`/4`或旧Corin Program设计端口合同。
- 与`refactor-character-presentation-runtime-modules`只在最终`SimulationActorTickResult`消费边界相接。本change只修改result生产和冻结过程，不改变Presentation result schema、Body插值或动画播放语义。
- 本change会修改`ProgramExecutionLayout`、Kernel contracts、Pipeline working state和两套Target runtime，不能与其它同时编辑这些文件的change并行apply。

## Current Spec Comparison

- `btsmtl-gameplay-semantic-ir`当前只要求IR表达operation、控制流和literal，没有定义Value port类型合同，也没有把未连接输入常量与目标端口结构化关联。
- `btsmtl-compiled-simulation-program`已声明Float32与Fixed双Target和稳定ABI；本change继续同步修改两套Target artifact、layout与runtime，不得只更新其中一套。
- `btsmtl-semantic-ir-inspection`当前可以查看operation、literal、control-flow和source map，但看不到“某个target input来自哪条Value edge或哪一个constant”。
- `character-simulation-kernel`已经要求Program级execution services不能每Tick重建，却没有规定Value input span、Timeline/State owner索引、Program固有Layout身份与backend binding的分离，也没有规定dirty page所有权移交和跨Evaluate/Finalize lease。
- `character-pipeline-blackboard`要求Frame开始和结束清理旧值，业务语义正确，但没有区分“generation失效”和“物理遍历写默认值”。本change保持旧值不可读、不可投影的语义，删除全量写入实现。
- `gameplay-simulation-pipeline`已经要求outer transaction原子提交，但没有规定同一个completed step在working apply和state store publish之间复用同一canonical state-set实例。
- `gameplay-tick-system`和Presentation相关spec不需要修改；固定步长、catch-up、render delta与插值语义保持不变。

## Impact

- Frontend与portable artifact：Operation Set、Semantic IR model/codec/store、Emitter、Inspector、Reader。
- Target Program：Float32/Fixed Program model、lowerer/compiler、codec、artifact store、ProgramHash/LayoutHash和Target ABI。
- Runtime：Float32/Fixed ProgramExecutionLayout、Value runtime、Kernel binding、State Transaction、Blackboard、Actor workspace、Pending/Result contracts、Pipeline working state。
- Generated products：Corin Semantic IR、Float32 Program、Fixed Program、ProgramAsset/Projection binding、Unity Authority、DotRecast Authority和Deterministic Rollback产品manifest。
- Breaking changes：旧`.csir`、旧`.csim`、旧`.fixed-program`、旧Snapshot/History、旧State codec、旧ProgramAsset metadata和旧产品manifest全部明确拒绝，不提供兼容reader、migrator、fallback或双写。

## Non-Goals

- 不修改BTSMTL节点、StateMachine、Timeline、Action、GameplayEffect、Motion或Corin业务配置。
- 不实现Value结果memoization；同一Tick中状态写入后的后续读取仍必须看到新值。
- 不把committed Character State改成全局mutable对象，也不允许Snapshot、History或Network payload引用workspace memory。
- 不修改Network Model协议、Prediction/Rollback策略、WorldSolver算法或Presentation插值。
- 不降低Logic Tick、修改catch-up上限、扩大History/Output容量、延长timeout或关闭错误。
- 不新增测试或人工验证task，不运行Unity batchmode。

## Success Criteria

- 正常Value求值不再解析`/constant/port:`、创建输入端口`HashSet`、按字符串排序或构造incoming集合。
- 正常Tick不再为运行中Timeline、Timeline child owner、State execution owner或Kernel兼容性枚举全部Program operation。
- 同一transaction中每个dirty page只从base state复制一次；Commit不再次复制该page，Abort不改变base state或已发布page。
- 没有Frame Blackboard写入的Tick不因Frame scope清理产生dirty page；上一generation的值在下一Tick表现为declaration default，且不能生成旧projection。
- Evaluate到Finalize之间Facts、Presentation Commands和Trace只有一个workspace owner；只在最终`SimulationActorTickResult`边界冻结一次。
- 每个completed step只构造一个canonical `SimulationWorldStateSet` candidate，working apply与state store publish复用该实例。
- Float32与Fixed执行相同的Value binding、owner generation、transaction ownership和output lease语义；不存在Target专用fallback或旧路径。
- 正式Performance Capture marker继续覆盖Kernel Evaluate、Finalize、State Commit、Result Freeze和Pipeline阶段；本change不伪造缺失的性能数字，最终运行时收益由用户按同场景capture验收。
- 所有新artifact与产品manifest使用新identity；旧版本在composition前明确失败。

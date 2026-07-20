## 1. 锁定现状、依赖与删除边界

- [x] 1.1 使用 PowerShell `Get-Content -Encoding UTF8` 重读本 change 的 proposal、design、tasks 与全部 spec delta。
- [x] 1.2 核对 `refactor-simulation-operation-runtime-modules` 已完成且唯一 Float32入口仍是 `SimulationKernel -> Float32OperationEvaluator`。
- [x] 1.3 核对 Standard Local、Preview、ServerAuthoritative 当前都通过同一 Float32 Kernel合同，不存在第二Character evaluator。
- [x] 1.4 盘点 `CharacterSimulationState`、`CharacterStateValue`、`CharacterSimulationStateBuilder`、`BuildPending()` 与全部构造调用。
- [x] 1.5 盘点全部 `ProgramStateValueKind.Bytes` StateSlot声明、默认值、读取、写入和canonical codec。
- [x] 1.6 盘点 Input request bytes的声明、查询、消费、过期和写回路径。
- [x] 1.7 盘点 Action request、instance、lifecycle、context、target snapshot和Timeline retention的重复状态。
- [x] 1.8 盘点 GameplayEffect Tags、Attributes、ActiveEffects、Periods、Journal、cursor及内部Capture/Restore/Save路径。
- [x] 1.9 盘点 MotionAccumulator、PendingWorldRequest、Motion contribution与Finalize清理路径。
- [x] 1.10 盘点 Character State codec、ActorSnapshot、WorldSnapshot、StateHash和Network Baseline对旧bytes ABI的引用。
- [x] 1.11 盘点 Compiler Frontend、CatalogCompiler、ProgramBuilder、Float32 Lowerer对Bytes kind与transient state slot的声明。
- [x] 1.12 盘点 Corin `.csim`、ProgramAsset、Projection、Definition引用和generated artifact identity。
- [x] 1.13 盘点 active ServerAuthoritative、DotRecast与DeterministicRollback change中State/Snapshot/History/Restore任务依赖。
- [x] 1.14 建立旧Builder、旧bytes state codec、旧state semantic、旧ABI和旧artifact删除清单。
- [x] 1.15 确认实施不需要网络兼容payload、旧Program reader、state migrator、双codec或Runtime fallback；若需要则停止并说明业务tradeoff。

## 2. 定义新 Character State ABI 与 typed schema

- [x] 2.1 定义新Float32 Target ABI version与Character State codec version。
- [x] 2.2 定义`TypedStateAddress`的SlotIndex、ValueKind、PartitionIndex、PageIndex和Offset合同。
- [x] 2.3 定义primitive state kind与各自canonical value约束。
- [x] 2.4 定义typed Input request state kind与字段合同。
- [x] 2.5 定义typed Action activation request state kind与字段合同。
- [x] 2.6 定义typed Action instance state kind并吸收phase/state/last transition/reason。
- [x] 2.7 定义最小typed Action instance reference kind供Timeline retention使用。
- [x] 2.8 定义typed Action target snapshot kind供Blackboard declaration使用。
- [x] 2.9 定义typed GameplayEffect aggregate state kind与canonical子集合顺序。
- [x] 2.10 定义Program StateSlot semantic到typed kind的一一合法映射表。
- [x] 2.11 删除Character State对通用`ProgramStateValueKind.Bytes`的合法支持。
- [x] 2.12 删除`MotionAccumulator`与`PendingWorldRequest` committed StateSlot语义。
- [x] 2.13 删除重复`ActionLifecycle` bytes state semantic与`ActionContext`镜像state semantic。
- [x] 2.14 将GameplayEffect五份bytes semantic与独立cursor收敛为唯一aggregate semantic。
- [x] 2.15 保持StateSlot stable index/source map是Program地址，禁止Runtime按type name或反射解析kind。
- [x] 2.16 让kind、semantic、owner、default与typed codec identity共同进入LayoutHash和ProgramHash。

## 3. 建立 Program 级 typed state layout索引

- [x] 3.1 在`ProgramExecutionLayout`建立每个StateSlot的预验证`TypedStateAddress`。
- [x] 3.2 按value kind建立不可变partition descriptor与slot-to-partition映射。
- [x] 3.3 建立Input request id到typed address的Program级索引。
- [x] 3.4 建立ActionId到request、instance和event sequence address的Program级索引。
- [x] 3.5 建立Action ContextId到Action instance address集合的稳定索引。
- [x] 3.6 建立Timeline operation到retained Action reference address的稳定索引。
- [x] 3.7 建立Blackboard declaration到typed Action target snapshot address的稳定索引。
- [x] 3.8 建立唯一GameplayEffect aggregate address。
- [x] 3.9 保留Runnable、StateMachine、Timeline primitive与Blackboard primitive现有policy校验。
- [x] 3.10 在Layout创建时拒绝semantic/kind/owner不匹配、重复singleton和缺失domain state。
- [x] 3.11 禁止typed layout保存Actor、Tick、mutable state、Network Model或Unity object。
- [x] 3.12 确认typed layout按Program构建一次并由全部Actor/Step复用。
- [x] 3.13 删除Input/Action/GE/Timeline Tick内扫描全部`Program.StateSlots`的查找。
- [x] 3.14 删除domain state owner的Tick内字符串拼接与字符串地址查找。

## 4. 建立不可变分页 CharacterSimulationState

- [x] 4.1 定义按typed kind分区的不可变Character State root。
- [x] 4.2 定义primitive partition的固定页大小与slot offset规则。
- [x] 4.3 定义typed domain partition的不可变value/root合同。
- [x] 4.4 让`CharacterSimulationState.CreateInitial`按新Program Layout创建typed默认状态。
- [x] 4.5 让初始状态严格校验ProgramId、ProgramHash、LayoutHash、NumericProfile与ABI。
- [x] 4.6 实现primitive typed address的只读访问。
- [x] 4.7 实现Input typed state的只读访问。
- [x] 4.8 实现Action typed state与reference的只读访问。
- [x] 4.9 实现Blackboard typed target snapshot的只读访问。
- [x] 4.10 实现GameplayEffect typed aggregate的只读访问。
- [x] 4.11 保证Committed State不暴露可变数组、List、Dictionary或领域对象引用。
- [x] 4.12 保证未修改page/aggregate可被后续State安全共享。
- [x] 4.13 保证State读取不分配临时byte[]或执行canonical decode。
- [x] 4.14 保证LastCompletedTick只在正式Transaction Commit时更新。

## 5. 建立 Float32CharacterStateTransaction

- [x] 5.1 定义Transaction生命周期`Created -> Active -> Committed|Aborted -> Disposed`。
- [x] 5.2 让Begin绑定唯一base State、Program、Layout、Actor和SimulationTick。
- [x] 5.3 实现base page只读访问与transaction dirty page write-set。
- [x] 5.4 实现首次写复制page、同page后续写复用。
- [x] 5.5 实现primitive typed read/write/reset并校验address policy。
- [x] 5.6 实现Input typed state read/write/clear。
- [x] 5.7 实现Action typed state read/write/clear。
- [x] 5.8 实现Timeline retained Action reference read/write/clear。
- [x] 5.9 实现Blackboard Action target snapshot typed read/write/reset。
- [x] 5.10 实现GameplayEffect aggregate首次写copy-on-write working root。
- [x] 5.11 实现typed savepoint创建、按栈恢复和释放。
- [x] 5.12 让savepoint同时恢复GE change projection cursor与相关handle allocator状态。
- [x] 5.13 实现Abort丢弃全部dirty page、aggregate、savepoint和mutable引用。
- [x] 5.14 实现Commit冻结dirty state并复用未修改page/aggregate。
- [x] 5.15 禁止Commit两次、Abort后写、Commit后写、跨Actor/Tick复用和越级savepoint。
- [x] 5.16 暴露只读transaction diagnostics summary，不暴露mutable values。
- [x] 5.17 保证Transaction不实现canonical codec、SnapshotParticipant或Network history。

## 6. 将 Evaluate 与 Finalize 接入同一 Transaction

- [x] 6.1 将`Float32EvaluationFrame.Begin`从创建Builder改为开始State Transaction。
- [x] 6.2 将Frame state port改为绑定transaction typed address。
- [x] 6.3 将portable control target的primitive state cell接到transaction。
- [x] 6.4 保持Blackboard begin、Ingress、GE advance、Input request、Timeline decision、Root tick、Motion resolve与Blackboard end顺序不变。
- [x] 6.5 修改`CharacterOperationEvaluation`使其不物化`CharacterSimulationState`。
- [x] 6.6 修改`PendingCharacterEvaluation`持有唯一未提交transaction和现有WorldRequest/output staging。
- [x] 6.7 删除`PendingCharacterEvaluation.StagedState`。
- [x] 6.8 为Pending增加Kernel specialization、Program/Layout、Actor、Tick和single-consume校验。
- [x] 6.9 保证WorldSolver只收到WorldRequest/World state，不访问State Transaction。
- [x] 6.10 将Finalize transient清理、FactSequence和Motion fact写入同一transaction。
- [x] 6.11 让Finalize在全部WorldResult校验和输出构造成功后唯一Commit transaction。
- [x] 6.12 Finalize失败时Abort transaction并保持base State不变。
- [x] 6.13 Evaluate失败时Abort transaction并清空Frame/output workspace。
- [x] 6.14 禁止Pending被Snapshot、History、Network、Trace payload或Pipeline state保存。
- [x] 6.15 保持Pipeline Backend只发布Finalize返回的新immutable State，outer Egress失败仍不发布working world。

## 7. 迁移 Input request typed runtime

- [x] 7.1 将Compiler中的`InputRequestBuffer` state声明从Bytes改为typed request kind。
- [x] 7.2 将Input request默认状态改为typed empty value。
- [x] 7.3 让`Float32InputRuntime.ApplyRequests`通过Program级request index写typed state。
- [x] 7.4 让request查询直接读取typed state，不执行`Bytes.ToArray()`。
- [x] 7.5 让consume直接更新typed consumed字段。
- [x] 7.6 让expire按SimulationTick读取typed expire tick并保持现有业务规则。
- [x] 7.7 保持request id、sequence、source tick、priority和离散去重语义。
- [x] 7.8 删除Input request magic/version/writer/reader与runtime byte[]方法。
- [x] 7.9 删除Input模块对通用Bytes state kind的依赖。
- [x] 7.10 确认Local、Preview和Network supplied `CharacterSimulationInput`使用同一typed request写入链。

## 8. 迁移 Action 与 Timeline retained context

- [x] 8.1 将Action request state声明从Bytes改为typed activation request kind。
- [x] 8.2 将Action instance state声明从Bytes改为typed instance kind。
- [x] 8.3 将phase、state、last transition、transition tick、source tick和reason收敛进Action instance。
- [x] 8.4 删除单独Action lifecycle bytes slot声明与写入。
- [x] 8.5 删除单独Action context UInt64镜像slot声明、写入与一致性校验。
- [x] 8.6 让active Action/context解析使用Program级Action index与typed instance。
- [x] 8.7 让Action request提交、读取、消费和clear使用typed state port。
- [x] 8.8 让Action activation、confirm、correct、complete、cancel、interrupt、reject和abort直接更新typed instance。
- [x] 8.9 保持ActionInstanceId、PredictionKey、InputSequence、TargetSnapshot与EventId业务语义。
- [x] 8.10 将Action target snapshot Blackboard declaration从Bytes改为typed target snapshot kind。
- [x] 8.11 删除Action target snapshot在Tick内的canonical byte codec调用。
- [x] 8.12 将Timeline retention state声明改为typed Action instance reference。
- [x] 8.13 Timeline启动时只捕获最小Action reference，不复制完整Action instance。
- [x] 8.14 Timeline更新时通过Action state port校验reference仍对应active instance。
- [x] 8.15 Timeline离开、stop和reset时清空typed reference。
- [x] 8.16 删除Action request/instance/lifecycle runtime magic/version/writer/reader和Timeline retained bytes路径。
- [x] 8.17 确认Action与Timeline模块不持有彼此具体实现，只通过现有窄port协作。

## 9. 迁移 GameplayEffect typed aggregate与局部事务

- [x] 9.1 定义不可变GameplayEffect committed aggregate root。
- [x] 9.2 将Tag sources按稳定identity顺序保存为typed state。
- [x] 9.3 将Attributes与Modifiers按稳定identity/handle顺序保存为typed state。
- [x] 9.4 将ActiveEffects按稳定handle/instance顺序保存为typed state。
- [x] 9.5 将Period schedule按instance/tick顺序保存为typed state。
- [x] 9.6 将Prediction journal与lifecycle revision按稳定key顺序保存为typed state。
- [x] 9.7 将ChangeCursor纳入同一aggregate。
- [x] 9.8 将Compiler中的五份GE bytes slot和cursor slot替换为唯一typed aggregate slot。
- [x] 9.9 让`SimulationGameplayEffectRuntime`直接消费transaction-owned typed GE view。
- [x] 9.10 删除每次Evaluation构造时的LoadTags/Attributes/ActiveEffects/Periods/Journal。
- [x] 9.11 删除Evaluation结束时的Save与五份runtime encode。
- [x] 9.12 将Effect Apply的原子失败改为typed savepoint restore。
- [x] 9.13 将Effect Remove的原子失败改为typed savepoint restore。
- [x] 9.14 将Period与Additional Effect嵌套失败改为typed savepoint restore。
- [x] 9.15 保持handle allocator capture/restore与GE savepoint同一层级。
- [x] 9.16 保持同Tick后续节点立即读取已修改Tag/Attribute/ActiveEffect。
- [x] 9.17 保持ChangeSet、Fact、Cue、failure reason和EventId输出顺序。
- [x] 9.18 将原五份bytes writer/reader迁入仅供canonical State codec使用的typed GE value codec。
- [x] 9.19 删除`PortableGameplayEffectStateSnapshot`中的bytes镜像和runtime bytes Capture/Restore。
- [x] 9.20 删除GE模块对`CharacterSimulationStateBuilder`和`CharacterStateValue.Bytes`的依赖。

## 10. 将 Motion pending移出 Committed State

- [x] 10.1 删除Compiler对`MotionAccumulator` StateSlot的声明。
- [x] 10.2 删除Compiler对`PendingWorldRequest` StateSlot的声明。
- [x] 10.3 让MotionContribution继续只进入Evaluation workspace。
- [x] 10.4 让唯一Float32 Motion accumulator直接返回`CharacterMotionRequest`。
- [x] 10.5 让Pending product唯一保存当前Step的WorldRequest与expected request identity。
- [x] 10.6 删除Motion request的runtime `Encode`与重复bytes写入。
- [x] 10.7 删除Finalize的`ClearTransientMotion` state写入。
- [x] 10.8 保持World ResolveBatch、applied displacement/yaw和Motion fact语义不变。
- [x] 10.9 确认Motion transient不进入Character State codec、Snapshot、StateHash或Network Baseline。
- [x] 10.10 确认Replay每个内部Step重新由Program Evaluate产生WorldRequest，不从历史transient slot恢复。

## 11. 重写 Character State codec、Snapshot 与 Hash

- [x] 11.1 定义新Character State canonical header与codec version校验。
- [x] 11.2 按Program StateSlot stable index顺序编码primitive typed partitions。
- [x] 11.3 编码typed Input request value并校验有限字段和identity。
- [x] 11.4 编码typed Action request、instance、reference与target snapshot。
- [x] 11.5 编码typed GameplayEffect aggregate及全部稳定子集合。
- [x] 11.6 解码时先校验NumericProfile、Target ABI、ProgramHash、LayoutHash和slot count。
- [x] 11.7 解码时拒绝未知kind、旧Bytes kind、重复entry、非canonical顺序和payload残留。
- [x] 11.8 解码后直接构建immutable typed partitions，不建立Builder或domain bytes cache。
- [x] 11.9 让CharacterStateHash覆盖新codec identity和canonical committed state bytes。
- [x] 11.10 让ActorSnapshot Capture对每个Actor只编码一次并复用bytes计算StateHash。
- [x] 11.11 让WorldSnapshot继续聚合Actor canonical bytes、World state与Pipeline state identity。
- [x] 11.12 让Restore只解码为新的committed typed State并原子替换working world。
- [x] 11.13 禁止Snapshot Capture或Hash访问active transaction、Evaluation Frame或mutable GE view。
- [x] 11.14 删除旧State codec version、旧Bytes value读写与旧hash输入。
- [x] 11.15 保持普通.NET Host可通过同一Float32 source set读取新`.csim`与State/Snapshot bytes。

## 12. 更新 Compiler、artifact 与 Corin正式配置

- [x] 12.1 更新Semantic到Target state declaration lowering以生成新typed kind。
- [x] 12.2 保持Numeric-neutral Semantic IR业务operation不增加Network Model或Float32专用节点。
- [x] 12.3 更新Program StateSlot codec与LayoutHash计算以包含新typed kind。
- [x] 12.4 更新Float32 Program manifest Target ABI与Program codec version。
- [x] 12.5 让旧`.csim`在Program Runtime加载时因ABI/version不匹配明确失败。
- [x] 12.6 更新Program build report显示新State ABI、typed partition和移除的transient slots。
- [x] 12.7 使用正式Build Transaction从Corin同一`.csir`重新生成Float32 `.csim`。
- [x] 12.8 重新生成exact-byte ProgramAsset wrapper与Presentation Projection。
- [x] 12.9 更新Corin CharacterPipelineDefinition对正式generated artifacts的引用。
- [x] 12.10 校验Corin Program/Projection source revision一致且ProgramHash/LayoutHash为新identity。
- [x] 12.11 删除旧generated ProgramAsset、旧store artifact和失效引用，不保留备份asset或fallback。
- [x] 12.12 确认Runtime不从authoring或`.csir`现场重建过期Program。

## 13. 对齐正在实施的网络模型

- [x] 13.1 更新`refactor-server-authoritative-hybrid-runtime`依赖，允许Source/Endpoint/协议/路由继续并行。
- [x] 13.2 标明ServerAuthoritative Prediction History state payload依赖新Character State codec。
- [x] 13.3 标明Authority Baseline capture/decode依赖新ABI与Layout identity。
- [x] 13.4 标明Baseline Merge、Correction Restore/Replay与HardRecovery不得引用State Transaction。
- [x] 13.5 将已出现的旧Builder、Bytes value或旧codec引用直接迁移到new canonical Snapshot合同。
- [x] 13.6 更新`add-dotrecast-authoritative-server-backend`worker loader与snapshot publisher依赖新Float32 ABI。
- [x] 13.7 更新`add-deterministic-rollback-kcc-model`以复用transaction生命周期与typed state schema形状。
- [x] 13.8 明确Fixed Target实现自己的typed partitions、numeric values、codec和transaction specialization。
- [x] 13.9 禁止三个模型共享mutable typed State、transaction、history实现或互相转换Snapshot。
- [x] 13.10 确认Network packet/history只保存canonical bytes、Program/Layout/codec identity与模型自己的metadata。
- [x] 13.11 删除网络侧旧State payload adapter、双codec、旧version switch和fallback decode。
- [x] 13.12 分别严格校验三个active网络change，确认职责没有回流到Character Kernel。

## 14. 删除旧路径并做统一链路审计

- [x] 14.1 删除`CharacterSimulationStateBuilder`类型。
- [x] 14.2 删除`CharacterSimulationState.ToBuilder`。
- [x] 14.3 删除`BuildPending()`与全部调用。
- [x] 14.4 删除`PendingCharacterEvaluation.StagedState`及旧构造参数。
- [x] 14.5 删除Character State中的通用Bytes字段、FromBytes与Bytes getter。
- [x] 14.6 删除Input request runtime byte codec。
- [x] 14.7 删除Action request/instance/lifecycle runtime byte codec。
- [x] 14.8 删除Timeline retained完整Action instance bytes副本。
- [x] 14.9 删除GameplayEffect每TickLoad/Save bytes路径和旧snapshot DTO。
- [x] 14.10 删除Motion pending bytes codec与transient StateSlot。
- [x] 14.11 使用`rg`确认Float32 Execution热路径不存在`.Bytes.ToArray()`。
- [x] 14.12 使用`rg`确认不存在`ProgramStateValueKind.Bytes` Character State声明。
- [x] 14.13 使用`rg`确认不存在`CharacterSimulationStateBuilder`与`BuildPending`。
- [x] 14.14 使用`rg`确认只有一个`CharacterSimulationStateCodec`和一个Float32 State Transaction实现。
- [x] 14.15 使用`rg`确认Network Model不引用transaction、typed mutable domain state或旧codec。
- [x] 14.16 核对最终链路为`Committed State -> Transaction -> Evaluate -> ResolveBatch -> Finalize -> Commit -> Pipeline working world -> atomic publish`。
- [x] 14.17 删除已无执行者的旧Action phase/state/motion-source运行时类型。
- [x] 14.18 删除旧GameplayEffect Apply/Remove/Reconcile合同、Attribute mutable DTO与TagContainer运行时。
- [x] 14.19 删除未消费的Animation selection batch、RootMotion evaluator、GameplayResult motion node与Pipeline factory/store接口。
- [x] 14.20 使用`rg`确认旧领域运行时类型不再进入当前代码链。

## 15. 文档、编译与严格校验

- [x] 15.1 更新`openspec/project.md`的Gameplay Client、Motion、Network Boundary与Code Organization描述。
- [x] 15.2 更新current specs中Builder/opaque Bytes/motion pending/旧GE assembly的过时措辞。
- [x] 15.3 核对`gameplay-simulation-pipeline`仍唯一拥有outer working world与atomic publish。
- [x] 15.4 核对`character-network-sync-domain-contract`仍由具体模型拥有History、Restore和Replay策略。
- [x] 15.5 核对`refactor-simulation-operation-runtime-modules`的portable control runtime与领域模块边界未被破坏。
- [x] 15.6 更新本change tasks勾选，使每项状态与实际实现一致。
- [x] 15.7 使用规定参数编译portable Core工程：`--disable-build-servers /nr:false /p:UseSharedCompilation=false`。
- [x] 15.8 使用规定参数编译Float32与普通.NET Reader工程：`--disable-build-servers /nr:false /p:UseSharedCompilation=false`。
- [x] 15.9 使用规定参数编译`Assembly-CSharp`与Editor相关工程：`--disable-build-servers /nr:false /p:UseSharedCompilation=false`。
- [x] 15.10 编译结束后立即执行`dotnet build-server shutdown`。
- [x] 15.11 运行`openspec validate refactor-character-state-transaction-runtime --strict --no-interactive`。
- [x] 15.12 运行相关active network change的strict validation并修复文档冲突。

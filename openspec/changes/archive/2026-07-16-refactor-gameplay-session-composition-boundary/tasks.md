## 1. 锁定现状、身份与迁移边界

- [x] 1.1 使用 `Get-Content -Encoding UTF8` 重读本 change 的 proposal、design、tasks 与全部 spec deltas。
- [x] 1.2 确认 `refactor-character-simulation-core`、`refactor-character-semantic-frontend-artifact` 与 `refactor-character-pipeline-definition-config-boundary` 已完成并归档。
- [x] 1.3 记录 `refactor-simulation-operation-runtime-modules` 的实施状态，锁定它只修改单 Actor Evaluate 内部，不让 Session Pass 进入 Operation evaluator。
- [x] 1.4 盘点 `CharacterPipelineHost` 创建 ProgramCatalog、Local Driver、Unity Solver、Kernel、Committer、SessionRuntime 与 Tick target 的全部调用点。
- [x] 1.5 盘点 `PreviewSimulationExecution`、Timeline Preview 与其它直接创建 `SimulationSessionRuntime` 的调用点。
- [x] 1.6 盘点 `ISimulationDriver`、`SimulationTickPlan`、restore request、result observation 与 OutputPlan 的全部实现和调用点。
- [x] 1.7 盘点 `SimulationSessionRuntime` 当前 ingress、restore、Evaluate、ResolveBatch、Finalize、state publish、OutputPlan 与 Commit 顺序。
- [x] 1.8 盘点 Session 失败时 Character state、World state、Unity Solver body 与外部输出的恢复路径。
- [x] 1.9 盘点 `GameplayNetworkSessionHost`、`GameplayNetworkModelDefinition`、`SimulationDriverCompositionCapability` 与 ServerAuthoritative 资产引用。
- [x] 1.10 盘点 GameplayTickSystem Input/Logic/Presentation target 的注册、顺序、重复保护与释放路径。
- [x] 1.11 盘点 Corin 场景中 CharacterPipelineHost、GameplayTickBootstrap、body binding、visual root、camera 与 diagnostics 引用。
- [x] 1.12 记录 Corin ProgramId、SourceRevision、SemanticHash、ProgramHash、LayoutHash、NumericProfile、Target ABI 与 canonical bytes hash。
- [x] 1.13 记录 Corin Local Session 的 ActorId、WorldRevision、TickRate、Solver descriptor、ProgramCatalogHash、roster identity 与当前 output disposition。
- [x] 1.14 记录 ProgramAsset canonical bytes 与 Reader 导出的 `.csim` bytes 是否完全一致。
- [x] 1.15 盘点普通 .NET Reader、Server project 与 build tooling 的 Program artifact loader 调用点。
- [x] 1.16 建立旧路径删除清单：每角色 Session、旧 Driver、固定 SessionRuntime、LocalCharacterSimulationSessionTickTarget、capability 位、旧 GameplayNetworkSessionHost、手工 `.csim` 导出和 ProgramAsset 独立 encode。
- [x] 1.17 建立模块所有权清单：portable Pipeline contracts、Float32 Backend、Pass modules、Unity Session Host、Actor registration、Session Source、Solver、artifact store、Inspector 与 Corin assets。
- [x] 1.18 确认 Graph、Timeline、Action、GameplayEffect、Motion、Animation 与 Operation evaluator 业务语义不属于本 change 修改范围。
- [x] 1.19 确认 active Session 动态 roster、Fantasy、DotRecast、Fixed/KCC 与具体 Network Model Pipeline 不进入本 change。
- [x] 1.20 若实施需要在 Runtime 外创建 replay runner、让 Pass 直接修改 Transform、或保留旧 SessionRuntime 并行执行，停止实施并更新 proposal。

## 2. 将 Float32 Program 提升为正式 `.csim` Artifact

- [x] 2.1 定义 Target Program artifact descriptor，覆盖 DefinitionGuid、ProgramId、NumericProfile、Target ABI、SourceRevision、SemanticHash、ProgramHash、LayoutHash 与 canonical bytes identity。
- [x] 2.2 建立 Definition GUID、NumericProfileId 与 ABI version 到 `Library/CharacterSimulation/Programs/.../*.csim` 的唯一安全路径映射。
- [x] 2.3 禁止 Definition 名称、asset path、ProgramId 显示字符串或默认 profile 参与 `.csim` 文件名 fallback。
- [x] 2.4 建立只接受 canonical codec 完整校验结果的 Program artifact store 写入入口。
- [x] 2.5 使用同目录临时文件写入完整 `.csim` bytes 并执行 flush。
- [x] 2.6 在替换前重新读取临时 `.csim` 并校验 magic/version、Target ABI、SourceRevision、SemanticHash、ProgramHash 与 LayoutHash。
- [x] 2.7 通过原子替换发布当前 Target Program artifact，失败时保持旧完整 `.csim`。
- [x] 2.8 建立 Missing、Current、Stale、Invalid 与 UnsupportedVersion 精确状态。
- [x] 2.9 建立按 Definition/Target expectation 严格加载 `.csim` 的 Editor 与普通 .NET 入口。
- [x] 2.10 让 Character Simulation build transaction 在 Projection/Unity Asset publish 前完成 `.csim` staging 与重读校验。
- [x] 2.11 修改 `CharacterSimulationProgramAsset`，使其接收并保存 store 重读后的 exact canonical bytes。
- [x] 2.12 禁止 ProgramAsset 从 Program 对象再次独立 encode canonical payload。
- [x] 2.13 校验 ProgramAsset metadata 与内嵌 `.csim` header/payload 完全一致。
- [x] 2.14 将 `.csim`、ProgramAsset、Projection 与 Definition references 纳入同一可回滚 publish transaction。
- [x] 2.15 transaction 任一步失败时恢复旧 `.csim`、ProgramAsset、Projection 与 Definition references。
- [x] 2.16 删除人工 Reader 验证专用 Program 导出路径和重复 Program artifact writer。
- [x] 2.17 保证 Unity Player 只从 ProgramAsset 内嵌 bytes 加载，不读取 `Library`、`.csir` 或 Editor store。
- [x] 2.18 扩展普通 .NET Reader 输出 `.csim` Target ABI、ProgramHash、LayoutHash 与 capability identity，不复制 codec。

## 3. 建立 Numeric-Neutral Session Composition 合同

- [x] 3.1 在不依赖 Float32/Fixed state 或 Unity 类型的模块中建立 `SimulationSessionCompositionIdentity`。
- [x] 3.2 建立 `SimulationSessionCompositionDescriptor`，覆盖 Session、source clock、TickRate、Program Runtime、Backend、Pipeline、Catalog、roster、Source、Solver、Snapshot、Committer、Model 与 Endpoint identity。
- [x] 3.3 建立 `SimulationSessionLifecycleState`，只允许 Uninitialized、Preparing、Active、Failed 与 Disposed。
- [x] 3.4 建立 `SimulationSessionPreparationStatus`，只允许 Pending、Ready 与 Failed。
- [x] 3.5 建立 `ISimulationSessionPreparation` 的 step、status、failure 与 Dispose 合同。
- [x] 3.6 建立不可变 `SimulationSessionLaunchPlan`，禁止暴露可变 Actor、Program、Pass、Source、Solver 或 port list。
- [x] 3.7 让 LaunchPlan 记录 Program Runtime、Backend、Pipeline plan/hash、Source ports、Solver、Snapshot codec、initial Character/World/Pipeline state 与 output routes。
- [x] 3.8 建立 numeric-neutral `ISimulationSessionRuntimeHandle` 的 descriptor、LogicTick、failure 与 Dispose 合同。
- [x] 3.9 禁止 runtime handle 暴露 Program、Character/World/Pipeline state、Source、Solver、Snapshot 或 Target scalar/vector。
- [x] 3.10 建立 `SimulationSessionCompositionException` 或等价分阶段失败类型，保留 component、Pass 与 product identity。
- [x] 3.11 建立 Runtime/Pass、Source/Endpoint、Solver、Presentation registration 与 preparation 的明确释放顺序。
- [x] 3.12 禁止 common contracts 引用 packet、Network Model policy、Unity Object、Float32 Program 或 Fixed Program。
- [x] 3.13 禁止通过 `object` state、dynamic、反射调用或 Float/Fixed conversion 实现 outer handle。
- [x] 3.14 建立 composition descriptor 的稳定只读 diagnostics snapshot。
- [x] 3.15 建立 Active 后 Definition、Pipeline、roster、Program、Source、Backend 与 Solver 锁定。
- [x] 3.16 建立非法热切换检测，要求销毁并重建 Session。
- [x] 3.17 将 common contracts 放入 Unity 与未来普通 .NET Host 可共享的明确 source set。

## 4. 建立 Portable Pipeline、Pass 与 Product 合同

- [x] 4.1 定义 versioned `SimulationPipelineId`、Revision、SchemaVersion 与 PipelineHash。
- [x] 4.2 定义 Ingress、Schedule、Step 与 Egress 四个固定 phase identity。
- [x] 4.3 定义不可变 portable `SimulationPipelineDescriptor`。
- [x] 4.4 让 descriptor 保存 phase 内稳定 Pass 顺序、PassId、implementation version 与 config hash。
- [x] 4.5 定义 `SimulationPipelinePassDescriptor` 的 NumericProfile、Target ABI、Backend 与 Solver capability requirement。
- [x] 4.6 定义 Pass 的 Forward、Replay、Restore 与 Authoritative 执行支持声明。
- [x] 4.7 定义 Stateless、Reconstructible、SnapshotParticipant 与 ExternalSource state class。
- [x] 4.8 定义 versioned `SimulationPipelineProductId` 与 schema identity。
- [x] 4.9 定义 exclusive producer、append-only producer 与 readonly consumer 权限。
- [x] 4.10 定义基础产品 `CanonicalInputs`、`TypedIngress` 与 `ExecutionPlan`。
- [x] 4.11 定义基础产品 `PendingActorEvaluations`、`WorldSolveBatchRequest` 与 `WorldSolveBatchResult`。
- [x] 4.12 定义基础产品 `FinalizedStepResult`、`PipelineSnapshotContribution`、`OutputDispositionSet` 与 `SourceEgress`。
- [x] 4.13 为 append-only 产品定义 stable ordering、ActorId、Tick、sequence 与 provenance 规则。
- [x] 4.14 定义第三方 product contract 的稳定 ID、schema version、owner、canonical identity 与 diagnostics 要求。
- [x] 4.15 禁止 Pass 使用 `Dictionary<string, object>`、字符串黑板或未声明共享字段传递产品。
- [x] 4.16 定义 phase-specific Ingress Pass runtime 接口与只读/写入端口。
- [x] 4.17 定义唯一 Schedule producer runtime 接口。
- [x] 4.18 定义 Step Pass runtime 接口与 staged transaction context。
- [x] 4.19 定义 Egress Pass runtime 接口与 readonly step result/output builder。
- [x] 4.20 定义 Pass Definition/Factory 合同，使 factory 只绑定声明过的 Source/Target/Solver/diagnostics ports。
- [x] 4.21 禁止 Pass factory 通过静态 current、场景搜索、反射扫描或运行时 service locator 取得依赖。
- [x] 4.22 定义 Pass lifecycle 的 Create、Activate、Execute、Capture/Restore 与 Dispose 顺序。
- [x] 4.23 保证 Pipeline contracts 不引用 BTSMTL Graph、Node、Timeline authoring、Animancer 或 packet 类型。

## 5. 建立 Pipeline Compiler 与 Identity 校验

- [x] 5.1 建立 `SimulationPipelineDefinition` 到 portable descriptor 的唯一转换入口。
- [x] 5.2 让 Pipeline Definition 显式保存 PipelineId、Revision 与四阶段 ordered Pass references。
- [x] 5.3 禁止按显示名、asset path 或类型扫描生成 PassId/PipelineId fallback。
- [x] 5.4 建立已安装 Pass factory catalog，由选定 Backend 显式提供，不使用全局 mutable registry。
- [x] 5.5 校验每个 Pass factory identity 与 descriptor version 精确匹配。
- [x] 5.6 校验 phase 合法且 phase 内顺序稳定。
- [x] 5.7 校验 Schedule phase 恰有一个 ExecutionPlan producer。
- [x] 5.8 校验每个 exclusive product 恰有一个 producer。
- [x] 5.9 校验每个 required product 在 consumer 前已经产生。
- [x] 5.10 校验 append-only product 的 producer order 与 provenance 规则。
- [x] 5.11 校验不存在未消费必需产品、重复 owner、非法跨 phase write 或产品依赖环。
- [x] 5.12 校验 Source port requirement 与当前 Session Source 实际端口匹配。
- [x] 5.13 校验 Pass 的 NumericProfile、Target ABI 与当前 Program Runtime 匹配。
- [x] 5.14 校验 Pass 的 Backend semantic version 与当前 Backend 匹配。
- [x] 5.15 校验 Pass/Program capability union 与 Solver capability 匹配。
- [x] 5.16 校验 Replay/Restore/Deterministic requirement 由 Program Runtime、Backend、Pass、Snapshot codec 与 Solver 全部支持。
- [x] 5.17 校验有状态 Pass 声明合法 state class 与 snapshot/reconstruct owner。
- [x] 5.18 以 canonical descriptor、Pass config、Backend semantic version 和 product schema 计算 PipelineHash。
- [x] 5.19 建立不可变 backend-specific compiled Pipeline plan。
- [x] 5.20 禁止 compiled plan 保存 Unity Editor object、mutable Definition 或 fallback Pass。
- [x] 5.21 对 unknown Pass、unknown product、unsupported version 与 config mismatch 输出精确错误。

## 6. 建立零到多步 ExecutionPlan 与 Pipeline State Snapshot

- [x] 6.1 定义 `SimulationSessionExecutionPlan` 的 outer source tick 与 provenance。
- [x] 6.2 定义 Pending 和 Executable 两种 plan outcome，不用空对象表达失败。
- [x] 6.3 定义可选完整 restore directive 与 snapshot identity。
- [x] 6.4 定义 ordered `SimulationStep`，覆盖 SimulationTick、source identity、Actor input、typed ingress 与 execution provenance。
- [x] 6.5 定义 Forward、Replay、Current 与 Authoritative step kind。
- [x] 6.6 校验 step Tick 顺序、重复 Tick、source clock mapping 与 roster binding。
- [x] 6.7 禁止 plan 为未知 Actor 提供 input/ingress 或隐式修改 roster。
- [x] 6.8 禁止 Schedule Pass 直接取得 mutable Character/World state。
- [x] 6.9 定义 Pipeline state participant 的 canonical capture 合同。
- [x] 6.10 定义 Pipeline state participant 的原子 restore 合同。
- [x] 6.11 定义 Pipeline state participant 的 canonical hash 合同。
- [x] 6.12 建立 `SimulationPipelineStateSnapshot`，按稳定 PassId order 聚合 participant payload。
- [x] 6.13 将 PipelineId、PipelineHash、BackendId/version 与 participant identity 写入 Pipeline snapshot。
- [x] 6.14 将 Pipeline snapshot 与 Character/World snapshot 纳入同一 Session restore transaction。
- [x] 6.15 禁止 transport queue、socket、外部 authoritative history 被序列化为 Character/World/Pipeline Gameplay state。
- [x] 6.16 对影响未来模拟却未声明 snapshot/reconstruct owner 的 Pass 拒绝 composition。
- [x] 6.17 对 participant 缺失、重复、version mismatch 或 payload hash mismatch 拒绝 restore。

## 7. 建立 Float32 Pass Execution Backend

- [x] 7.1 建立 `SimulationExecutionBackendDefinition` 的 portable descriptor 与 capability contract。
- [x] 7.2 建立 `Float32PassExecutionBackendDefinition`，只声明当前真实支持的 Pipeline schema、Float32 ABI 与 transaction capability。
- [x] 7.3 建立强类型 Float32 Backend composition request。
- [x] 7.4 让 request 显式包含 Program Runtime services、compiled Pipeline plan、ordered roster、initial state、Source ports、Solver、Snapshot codec、Committer 与 diagnostics。
- [x] 7.5 建立 Float32 Pass factory catalog，并只注册本 change 安装的标准 Pass。
- [x] 7.6 让 Backend 在创建任何 runtime mutable state 前完成全部 identity/capability 校验。
- [x] 7.7 建立一次 outer LogicTick 的 staged Pipeline transaction。
- [x] 7.8 按 compiled plan 执行全部 Ingress Pass。
- [x] 7.9 调用唯一 Schedule producer并处理 Pending outcome。
- [x] 7.10 在 step loop 前校验并原子应用可选 restore directive。
- [x] 7.11 为每个 SimulationStep 建立独立 working Character/World/Pipeline state view。
- [x] 7.12 按 compiled order 执行全部 Step Pass。
- [x] 7.13 在一个 outer transaction 中将前一 replay step 的 working state作为下一 step 输入。
- [x] 7.14 保证 replay 中间 step 不直接提交 Presentation、Network 或其它外部副作用。
- [x] 7.15 在全部 step 完成后按 compiled order 执行 Egress Pass。
- [x] 7.16 校验 Egress 产生的 EventId disposition 完整且无重复 owner。
- [x] 7.17 在全部 state/product/output 校验成功后原子发布最终 Character/World/Pipeline state。
- [x] 7.18 只在 state publish 后调用唯一 Committer。
- [x] 7.19 在任一 Pass、Solver、Finalize 或 output 校验失败时恢复 outer Tick 前 working state。
- [x] 7.20 在 Solver 已接触 Unity body 后失败时使用正式 Solver restore/reconstruct 合同恢复，不直接写 Transform。
- [x] 7.21 Committer 外部端口失败后保持 fail-stop，不伪造 Gameplay state rollback 或重试副作用。
- [x] 7.22 建立 Float32 numeric-neutral runtime handle adapter。
- [x] 7.23 将 Backend/Pass failure 精确投影到 outer lifecycle 与 diagnostics descriptor。
- [x] 7.24 保证 runtime handle Dispose 只执行一次并按 owner 顺序释放 Pass/Source/Solver。
- [x] 7.25 禁止 Backend 创建私有 Update、协程、Task loop 或 replay tick target。

## 8. 将当前 Local Session 迁为标准 Pipeline Pass

- [x] 8.1 建立 `LocalInputIngressPassDefinition` 与 runtime。
- [x] 8.2 让 Local Input Pass 只从显式 Actor registration 的 local input port读取当前 source tick 输入。
- [x] 8.3 保持相机相对输入在 Input Adapter 边界完成，不让 Pass 读取 Camera 或 InputAction。
- [x] 8.4 建立 `LocalSingleStepSchedulePassDefinition` 与 runtime。
- [x] 8.5 将一个 LocalLogicTick 映射为一个连续 SimulationTick。
- [x] 8.6 让 Local Schedule 不产生 restore、history、replay、endpoint 或 ServerTick。
- [x] 8.7 建立 Float32 `ProgramEvaluatePassDefinition` 与 runtime。
- [x] 8.8 让 Evaluate Pass 按 stable ActorId order调用唯一 SimulationKernel Evaluate。
- [x] 8.9 让 Evaluate Pass 只产生 pending evaluation 与 WorldSolveBatchRequest 产品。
- [x] 8.10 建立 Float32 `WorldResolveBatchPassDefinition` 与 runtime。
- [x] 8.11 让 World Pass 对当前 step全部 Actor request 调用一次唯一 Solver ResolveBatch。
- [x] 8.12 精确校验每个 WorldRequest 与 WorldResult identity。
- [x] 8.13 建立 Float32 `ProgramFinalizePassDefinition` 与 runtime。
- [x] 8.14 让 Finalize Pass 按 stable ActorId order消费 pending evaluation 与 world result。
- [x] 8.15 保持 Character state、Gameplay facts、Presentation commands、Sync facts、Trace 与 EventId 顺序不变。
- [x] 8.16 建立 `LocalImmediateOutputPassDefinition` 与 runtime。
- [x] 8.17 让 Local Output Pass 为全部新外部 EventId 生成 Publish disposition。
- [x] 8.18 保持 Output disposition 不决定 staged Gameplay state 是否生效。
- [x] 8.19 建立唯一 `StandardLocalSimulationPipelineDefinition`。
- [x] 8.20 按 Ingress/Schedule/Step/Egress 明确引用六个标准 Pass。
- [x] 8.21 编译 Local Pipeline 并核对其 PipelineHash 与 descriptor 稳定。
- [x] 8.22 保持当前 Corin 单 Tick业务结果与旧 SessionRuntime 一致。
- [x] 8.23 删除旧 `LocalSimulationDriver` 实现。
- [x] 8.24 删除旧 `ISimulationDriver` 与单 Tick `SimulationTickPlan` 公共入口。
- [x] 8.25 删除固定 `SimulationSessionRuntime`，不得保留 wrapper 或 feature flag。

## 9. 建立 Program Runtime、Composer 与唯一 Session Host

- [x] 9.1 建立 `SimulationProgramRuntimeDefinition` 抽象，明确只拥有 Numeric Program/State/Kernel/Snapshot ABI。
- [x] 9.2 建立 `Float32ProgramRuntimeDefinition` 并绑定正式 ProgramAsset canonical loader。
- [x] 9.3 建立 Target-specific Composer 入口，使其同时接受 Program Runtime 与匹配 Execution Backend。
- [x] 9.4 校验 ProgramCatalog 中全部 Program 使用同一 Float32 NumericProfile、Target ABI、TickRate 与 operation-set。
- [x] 9.5 校验 Kernel specialization 与 ProgramCatalog identity 匹配。
- [x] 9.6 校验 Pipeline Backend、Pass 与 Program Runtime ABI 匹配。
- [x] 9.7 校验 Solver descriptor、NumericProfile、ABI、TickRate 与 Program/Pass capability union 匹配。
- [x] 9.8 校验 Snapshot codec、initial Character/World/Pipeline state 与 roster/solver identity 匹配。
- [x] 9.9 校验 ActorId 唯一、stable order、ProgramId/Hash/LayoutHash 与 body binding。
- [x] 9.10 校验 Committer 和 diagnostics ports 对 roster 中每个需要输出的 Actor 完整。
- [x] 9.11 只在全部校验后创建 Float32 Pass Pipeline runtime handle。
- [x] 9.12 禁止 Character Host、Preview、Demo 或 Network Model直接调用 Backend/runtime 构造器。
- [x] 9.13 建立 `SimulationSessionHost` MonoBehaviour 作为场景 Session 唯一 owner。
- [x] 9.14 建立显式 `SimulationSessionCompositionDefinition` 的五项引用。
- [x] 9.15 禁止 CompositionDefinition 使用 model/source/solver/target/backend/pipeline enum 或默认实现。
- [x] 9.16 建立 Host 的 Uninitialized、Preparing、Active、Failed 与 Disposed 单向状态转换。
- [x] 9.17 让 Host 从 Session Source 创建一次 preparation。
- [x] 9.18 让 Host 在 GameplayTickSystem 正式 target 中推进 preparation，不创建私有 runner。
- [x] 9.19 只在 preparation Ready 后编译并冻结 Pipeline plan 与 LaunchPlan。
- [x] 9.20 让 Target-specific Composer 创建唯一 runtime handle。
- [x] 9.21 创建成功后锁定全部 Definition、roster、Pipeline plan、Source ports 与 runtime identity。
- [x] 9.22 注册每 Session 唯一 Input/Logic tick target。
- [x] 9.23 保证每个 LocalLogicTick 只调用一次 active runtime handle。
- [x] 9.24 在 configuration、preparation、compile 或 runtime 失败时进入 Failed 并停止后续 Tick。
- [x] 9.25 实现严格释放顺序并防止重复 Dispose/Unregister。
- [x] 9.26 禁止失败时创建 Local Source、默认 Pipeline、默认 Solver、空 Session 或旧 Character Session。

## 10. 将 CharacterPipelineHost 拆为 Actor Registration Owner

- [x] 10.1 定义不可变 `CharacterSimulationActorRegistration`。
- [x] 10.2 将显式 ActorId 与 ProgramAsset/Program identity 写入 registration。
- [x] 10.3 将 Projection 与 ProgramHash/SourceRevision 关系写入 registration。
- [x] 10.4 将 Unity World body binding identity 写入 registration。
- [x] 10.5 将可选 local input adapter port 写入 registration，不让远端 Actor伪造设备输入。
- [x] 10.6 将 Presentation runtime/output port 与 visual root identity 写入 registration。
- [x] 10.7 将 diagnostics metadata 与 source map revision 写入 registration。
- [x] 10.8 让 CharacterPipelineHost 在创建 registration 前严格校验 Definition、Program、Projection、ActorId、body 与表现配置。
- [x] 10.9 让 CharacterPipelineHost 显式引用唯一 SimulationSessionHost，不通过场景搜索或静态 current host 注册。
- [x] 10.10 将 registration 生命周期改为 Session Host 接受、锁定和释放，不建立全局 mutable registry。
- [x] 10.11 对重复 registration、未知 Host、重复 ActorId 与 Active 后注册明确失败。
- [x] 10.12 从 CharacterPipelineHost 移除 ProgramCatalog 创建。
- [x] 10.13 从 CharacterPipelineHost 移除 Local Driver/Session Source 创建。
- [x] 10.14 从 CharacterPipelineHost 移除 Unity Solver 创建。
- [x] 10.15 从 CharacterPipelineHost 移除 SimulationKernel 与 Program Runtime创建。
- [x] 10.16 从 CharacterPipelineHost 移除 SessionRuntime/Backend 与 Committer aggregate创建。
- [x] 10.17 从 CharacterPipelineHost 移除 Input/Logic target 自注册。
- [x] 10.18 保留每 Actor Presentation runtime，但将 target 注册/释放所有权交给 Active Session composition。

## 11. 建立 Local/Network Session Source 基座

- [x] 11.1 建立 `SimulationSessionSourceDefinition` 的 identity、preparation、port 与 capability contract。
- [x] 11.2 建立 `LocalSimulationSessionSourceDefinition`，明确它不是 Gameplay Network Model。
- [x] 11.3 让 Local Source 要求每个本地控制 Actor registration 提供合法 local input port。
- [x] 11.4 让 Local preparation 从 registrations 建立 stable ProgramCatalog、roster 与 source clock。
- [x] 11.5 让 Local preparation提供 Local input ports，不建立 endpoint、history、correction、restore 或 replay state。
- [x] 11.6 让 Solver Definition 从 registrations 建立唯一 Unity body binding集合。
- [x] 11.7 让 Local composition 建立初始 Character/World/Pipeline state。
- [x] 11.8 让 Local composition 建立 aggregate Committer 与每 Actor Presentation/output route。
- [x] 11.9 建立 Local composition descriptor，ModelId/EndpointId 为空但不表达 fallback。
- [x] 11.10 将 `GameplayNetworkModelDefinition` 改为正式 Network Model Session Source definition。
- [x] 11.11 定义 ModelId、protocol/endpoint、Source port、Pipeline、Backend、Target ABI 与 World capability requirement。
- [x] 11.12 建立实际 Network Model runtime factory/preparation 创建入口。
- [x] 11.13 让 Model preparation 可在 Ready 前持有 model session/endpoint 并等待 canonical launch roster。
- [x] 11.14 保证 Model Source 不创建 Kernel、执行 Program operation、调用 Solver 或驱动 Presentation。
- [x] 11.15 保证 Model Source 不在 Common Host 中隐藏插入 Pass。
- [x] 11.16 让 Pipeline compiler显式验证所选 Model Source 与模型 Pass 的 port/capability 匹配。
- [x] 11.17 用实际 Source factory、Pass factory 与 requirement validation替换 `SimulationDriverCompositionPart`。
- [x] 11.18 删除 `SimulationDriverCompositionCapability` 位掩码及其 Inspector 展示。
- [x] 11.19 删除旧 `GameplayNetworkSessionHost` MonoBehaviour 与静态 session selection路径。
- [x] 11.20 删除 incomplete model 通过 packet/session/endpoint 存在就被视为可选的路径。
- [x] 11.21 在正式 ServerAuthoritative Source/Pipeline 尚未实现前删除或隐藏其 selectable Definition，不保留空 factory。
- [x] 11.22 建立 Network Model configuration validation 对缺失 Source、Pass、Pipeline、Endpoint、Backend、Target 或 Solver requirement 的精确错误。
- [x] 11.23 保证 Local Source 不经过 GameplayNetworkModelDefinition、Endpoint 或 Model session。

## 12. 迁移 Preview、Presentation 与 Diagnostics

- [x] 12.1 为 Preview 建立隔离 `PreviewSimulationSessionSourceDefinition`。
- [x] 12.2 为 Preview 建立显式 Preview Pipeline Definition。
- [x] 12.3 让 Preview Pipeline 复用 Float32 Program Runtime、Pass Backend 和标准 Step Pass。
- [x] 12.4 保持 Preview input/schedule/output Pass 与 Local gameplay Source 分离。
- [x] 12.5 将 `PreviewSimulationExecution` 的直接 SessionRuntime 创建迁到 Target-specific Composer。
- [x] 12.6 为 Preview 创建隔离 runtime handle，不注册场景 SimulationSessionHost。
- [x] 12.7 保持 Preview Solver body、Committer ports 与 Pipeline state 的隔离所有权。
- [x] 12.8 保证 Timeline Preview 不读取 Active gameplay Session state、Source、World、Pipeline plan 或 roster。
- [x] 12.9 让 Session composition 按 ActorId stable order 注册 Presentation targets。
- [x] 12.10 保证 PresentationFrame 只消费 committed sample/command，不读取 CompositionDefinition、Pipeline product 或 Source state。
- [x] 12.11 将 SessionId、CatalogHash、roster identity、SourceId、SolverId、Program Runtime ABI 与 lifecycle 加入 diagnostics。
- [x] 12.12 将 BackendId/version、PipelineId/Revision/Hash 与 compiled Pass order 加入 diagnostics。
- [x] 12.13 将 preparation Pending/Ready/Failed 与 composition component error加入 structured diagnostics。
- [x] 12.14 将每次 outer LogicTick 的 Ingress、Schedule outcome、restore 与 step count加入 boundary diagnostics。
- [x] 12.15 将每个 Pass 的 phase、PassId、product input/output、成功/失败与耗时加入只读 diagnostics。
- [x] 12.16 将 Pipeline snapshot participant identity 与 restore/hash结果加入 diagnostics。
- [x] 12.17 将 Actor diagnostics target挂到 Session descriptor，不创建第二个 mutable registry。
- [x] 12.18 保持 Program SourceMap、ActionInstance、Timeline、WorldRequest/Result 与 animation diagnostics链不变。
- [x] 12.19 删除 Character Host 旧 Session diagnostics identity 与重复 Tick target metadata。
- [x] 12.20 删除 Preview/Local/Network 专用的重复 composition diagnostics DTO。

## 13. 建立 Pipeline Inspector 并迁移 Corin 正式配置

- [x] 13.1 建立 `SimulationSessionCompositionDefinition` Inspector，显式显示五项引用和只读 compatibility结果。
- [x] 13.2 禁止 Inspector 自动创建或选择默认 Program Runtime、Backend、Pipeline、Source 或 Solver。
- [x] 13.3 建立 `SimulationPipelineDefinition` Inspector 的 Ingress、Schedule、Step 与 Egress 分组。
- [x] 13.4 支持在合法 phase内显式添加、删除、替换与排序 Pass Definition。
- [x] 13.5 显示 PassId、version、state class、consume/produce 与 capability requirement。
- [x] 13.6 显示 Pipeline compile 的 Missing Product、Duplicate Owner、Order、Factory、ABI、Source port 与 Solver错误。
- [x] 13.7 显示 canonical PipelineId、Revision、PipelineHash 与 Backend semantic version。
- [x] 13.8 禁止 Inspector 使用 reflection scan、自动修复 unknown Pass、删除 unsupported Pass 或静默重排。
- [x] 13.9 为 Sandbox 创建唯一 Corin Local `SimulationSessionCompositionDefinition`。
- [x] 13.10 为该 composition显式绑定 Float32 Program Runtime Definition。
- [x] 13.11 为该 composition显式绑定 Float32 Pass Execution Backend Definition。
- [x] 13.12 为该 composition显式绑定 Standard Local Pipeline Definition。
- [x] 13.13 为该 composition显式绑定 Local Session Source Definition。
- [x] 13.14 为该 composition显式绑定 Unity CharacterController WorldSolver Definition。
- [x] 13.15 在 Sandbox创建唯一 SimulationSessionHost 并绑定正式 GameplayTickSystem。
- [x] 13.16 将 Corin CharacterPipelineHost显式绑定到该 Session Host。
- [x] 13.17 迁移 Corin ActorId、WorldRevision、body binding、visual root、camera、input 与 diagnostics引用。
- [x] 13.18 删除 Corin Character Host 上旧每角色 Session/Tick ownership serialized data。
- [x] 13.19 通过正式 Build Transaction重新生成 Corin `.csim`、ProgramAsset 与 Projection。
- [x] 13.20 核对新 `.csim` 与 ProgramAsset内嵌 canonical bytes完全相同。
- [x] 13.21 核对 Corin ProgramHash、LayoutHash、operation/state/source-map 与 Presentation producer identity未因 Pipeline迁移改变。
- [x] 13.22 核对 Corin Local Pipeline descriptor、Pass order 与 PipelineHash稳定。
- [x] 13.23 确认 Corin场景只有一个 Session Logic target、一个 Local Source、一个 compiled Pipeline runtime、一个 Unity Solver与一个 World state owner。

## 14. 更新项目文档与下游 Change

- [x] 14.1 更新 `openspec/project.md`，将固定 SessionRuntime顺序改为 Session transaction + compiled Pipeline plan。
- [x] 14.2 更新 `openspec/project.md`，记录 Program Runtime、Execution Backend、Pipeline、Session Source 与 Solver五项 composition。
- [x] 14.3 更新 `openspec/project.md`，将 CharacterPipelineHost记录为 Actor registration owner，将 SimulationSessionHost记录为唯一运行装配根。
- [x] 14.4 更新 `openspec/project.md`，将 `.csim` 记录为正式 Target artifact，ProgramAsset记录为 exact-byte wrapper。
- [x] 14.5 更新 `refactor-simulation-operation-runtime-modules` 的 Session边界措辞，删除“Session固定实现不可组合”的冲突描述。
- [x] 14.6 更新 `refactor-server-authoritative-hybrid-runtime` dependency，要求本 change全部任务完成、编译与 strict validation通过；不等待归档。
- [x] 14.7 从 ServerAuthoritative tasks删除公共 Host、Pipeline compiler、Float32标准 Step Pass、Actor registration与 `.csim` loader职责。
- [x] 14.8 让 ServerAuthoritative change只实现 Model Source、correction/replay Pass、Prediction Pipeline、Endpoint/Room与 Demo。
- [x] 14.9 更新 `add-dotrecast-authoritative-server-backend` 使用正式 `.csim` loader、ServerAuthoritative Pipeline与公共 composition descriptor。
- [x] 14.10 保证 DotRecast change不创建第二 Pipeline、Network Model或 Session Host。
- [x] 14.11 更新 `add-deterministic-rollback-kcc-model` 使用 Pipeline descriptor、outer runtime handle与唯一 Session Host。
- [x] 14.12 让 Rollback change只实现 Fixed Program Runtime、Deterministic Backend、Rollback Source/Pass/Pipeline、KCC、协议与 Demo。
- [x] 14.13 从 Rollback tasks删除公共 Host、Actor registration与 Pipeline compiler重复职责。
- [x] 14.14 更新三个下游 change 的任务序号、并行边界、Pipeline身份与删除清单。
- [x] 14.15 更新 implementation inventory，记录新旧所有权、Corin identity、标准 Local Pipeline与下游扩展入口。
- [x] 14.16 将 Program Runtime、Session Source 与 WorldSolver 的纯 descriptor 迁入 portable Core source set。
- [x] 14.17 建立 portable Float32 唯一 Composer，并将 Unity Composer 收敛为显式 Definition、port 与 Unity owner 适配器。
- [x] 14.18 建立 Tick 0 Pipeline state 的 activated-default capture 与 explicit snapshot restore 两种正式来源，删除伪空 snapshot 初始化。
- [x] 14.19 让 Output disposition 显式携带 ActorId，删除 Unity output aggregate 的无界 EventId owner ledger。
- [x] 14.20 让 Actor registration 与 Session Host 的激活、失败和释放路径在异常下仍完整清理已取得资源。

## 15. 删除旧路径与静态校验

- [x] 15.1 删除 `LocalCharacterSimulationSessionTickTarget` 文件及 meta。
- [x] 15.2 删除旧 `GameplayNetworkSessionHost` 文件及 meta。
- [x] 15.3 删除 `SimulationDriverCompositionPart` 与 `SimulationDriverCompositionCapability`。
- [x] 15.4 删除旧 `ISimulationDriver`、`LocalSimulationDriver` 与单 Tick plan/restore/output调用链。
- [x] 15.5 删除固定 `SimulationSessionRuntime` 文件、构造器与 Preview/Host引用。
- [x] 15.6 删除 CharacterPipelineHost中的 Local Source、Unity Solver、Kernel、Backend/Runtime与 Logic target构造代码。
- [x] 15.7 删除 ProgramAsset从 Program对象独立 encode的入口。
- [x] 15.8 删除人工 `.csim` 导出、旧 capability Inspector 与 incomplete model selection。
- [x] 15.9 使用 `rg` 确认仓库不存在 `new SimulationSessionRuntime`。
- [x] 15.10 使用 `rg` 确认仓库不存在旧 `ISimulationDriver` 与 `SimulationDriverCompositionPart`。
- [x] 15.11 使用 `rg` 确认 Local input/schedule/output只由标准 Local Pass拥有。
- [x] 15.12 使用 `rg` 确认 `SimulationKernel.Evaluate` 只由标准/正式 Step Pass调用。
- [x] 15.13 使用 `rg` 确认 `ICharacterWorldSolver.ResolveBatch` 只由正式 WorldSolve Pass调用。
- [x] 15.14 使用 `rg` 确认 CharacterPipelineHost不引用 Session Source、Solver、Kernel、Execution Backend或 Pipeline Runtime具体类型。
- [x] 15.15 使用 `rg` 确认 Common Session Host不引用 packet、ServerAuthoritative、Rollback、Float32 State或 Fixed State。
- [x] 15.16 使用 `rg` 确认 Runtime不读取 `.csir`、`Library` Program store或 Editor artifact store。
- [x] 15.17 使用 `rg` 确认 Local模式不创建 Gameplay Network Model、Endpoint、history或 restore Pass。
- [x] 15.18 使用 `rg` 确认 Pass不通过 static current、场景搜索、反射 registry或字符串 service lookup取得运行依赖。
- [x] 15.19 使用普通 .NET Reader读取 Corin正式 `.csim` 并核对 ProgramHash、LayoutHash与 Target ABI。
- [x] 15.20 核对 ProgramAsset内嵌 bytes hash与正式 `.csim` bytes hash完全一致。
- [x] 15.21 核对 Corin PipelineHash不进入 ProgramHash，但进入 Session descriptor与 diagnostics。
- [x] 15.22 使用 `dotnet build --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译 portable Core、Float32与 Reader工程。
- [x] 15.23 portable build后立即执行 `dotnet build-server shutdown`。
- [x] 15.24 使用 `dotnet build --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译 Runtime/Assembly-CSharp工程。
- [x] 15.25 Runtime build后立即执行 `dotnet build-server shutdown`。
- [x] 15.26 使用 `dotnet build --disable-build-servers /nr:false /p:UseSharedCompilation=false /m:1` 编译 Editor/Assembly-CSharp-Editor工程。
- [x] 15.27 Editor build后立即执行 `dotnet build-server shutdown`。
- [x] 15.28 更新本 change proposal/design/spec/tasks与最终实现差异，删除不存在的类型或路径描述。
- [x] 15.29 更新 tasks勾选，使每项 `[x]` 与真实实现一致。
- [x] 15.30 运行 `openspec validate refactor-gameplay-session-composition-boundary --strict --no-interactive` 并修复全部错误。
- [x] 15.31 运行受影响的 `refactor-simulation-operation-runtime-modules`、`refactor-server-authoritative-hybrid-runtime`、`add-dotrecast-authoritative-server-backend` 与 `add-deterministic-rollback-kcc-model` strict validation。

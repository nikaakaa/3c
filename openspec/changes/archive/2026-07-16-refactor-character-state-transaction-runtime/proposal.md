# Change: 重构角色状态事务与类型化运行时

## Why

当前 Float32 角色模拟虽然已经收敛到唯一 `SimulationKernel -> Float32OperationEvaluator` 链路，但 `CharacterSimulationState` 的运行时读写仍沿用“不可变状态数组 + 全量 Builder + opaque bytes”的早期实现：

- `Float32EvaluationFrame.Begin` 每个 Actor/Tick 将全部 StateSlot 复制进 `CharacterSimulationStateBuilder`。
- Evaluate 结束通过 `BuildPending()` 生成一份中间 `CharacterSimulationState`，Finalize 又从该状态创建第二个 Builder。
- Input request、Action request/instance/lifecycle、Timeline retained Action Context、GameplayEffect Tags/Attributes/Active/Periods/Journal 和 Motion pending 都以 `ProgramStateValueKind.Bytes` 保存。
- Input、Action 和 Timeline 在查询时反复 `Bytes.ToArray()` 并反序列化；GameplayEffect 每次 Evaluate 重建运行对象并解码五份状态，结束时再编码脏集合。
- `MotionAccumulator` 与 `PendingWorldRequest` 只服务同一 Tick 的 Evaluate/Finalize，却进入 committed Character State、Snapshot 和 StateHash。

这不仅产生 Tick 热路径复制、分配和编解码，也让状态所有权变得含糊：`byte[]` 同时承担运行态、事务态和网络/快照交换格式。正在实施的 ServerAuthoritative prediction/history，以及后续 Fixed rollback，若直接绑定该模型，会把这些问题固化进多个网络模型。

本 change 将 Character State 收敛为类型化、不可变的 committed state，并让一次 Actor Step 只通过一个 target-specific State Transaction 从 Evaluate 延续到 Finalize。Canonical bytes 只在 Program artifact、Snapshot、Restore、Hash 和 Network Baseline 边界产生，不再作为领域模块的日常运行容器。

## Dependencies

- `refactor-simulation-operation-runtime-modules` MUST 已完成；本 change 复用其唯一 portable control runtime、Float32 evaluator、领域模块和 Program 级执行索引，不恢复旧 `SimulationOperationMachine`。
- `refactor-gameplay-session-composition-boundary` MUST 已归档；本 change 不修改 Source/Pipeline/Host 的职责，只替换 Float32 Kernel 内部 Character State ABI。
- `refactor-server-authoritative-hybrid-runtime` MAY继续并行实施 ModelDefinition、Session Source、Fantasy Endpoint、协议、路由、队列和 Actor registration；其 Prediction History、Baseline Merge、Correction Restore/Replay 与 SnapshotParticipant codec 接入 MUST等待本 change 的正式 State codec 和 ABI。
- `add-dotrecast-authoritative-server-backend` 继续复用 Float32 Program/Kernel；其 worker state loader、Authority Pipeline 与 snapshot publisher MUST消费本 change 的新 ABI。
- `add-deterministic-rollback-kcc-model` MUST复用本 change 定义的 committed state、transaction、typed state kind 和 canonical codec所有权形状，但 Fixed Target必须实现自己的数值状态类型、codec和 transaction specialization，不能引用 Float32状态实现。

## What Changes

- 建立 `Float32CharacterStateTransaction`，以当前 committed `CharacterSimulationState` 为只读基线，拥有一次 Actor Step 的 typed write-set、dirty tracking、savepoint、abort 和唯一 commit。
- 让同一个 State Transaction 从 Program Evaluate 延续到 Program Finalize；`PendingCharacterEvaluation`只持有该未提交事务及 WorldRequest/输出 staging，不再持有 `StagedState`。
- 将 committed Character State 改为按 `ProgramStateValueKind` 分区的不可变 typed storage，并使用按页 copy-on-write；事务成本按实际写入页和领域状态变化增长，不再按全部 StateSlot 数量复制两次。
- 保留稳定 StateSlot index/source map 作为 Program 地址；`ProgramExecutionLayout`一次将 slot 降低为 typed address，并建立 Input、Action、Timeline retention、GameplayEffect 与 primitive state 的预验证索引，Tick 内不得扫描全部 Program StateSlot 或按字符串查找 owner。
- 删除 `ProgramStateValueKind.Bytes` 作为 Character State storage kind。为 Input request、Action activation request、Action instance、Action lifecycle/reference、Action target snapshot、Timeline retained action reference 和 GameplayEffect aggregate 建立明确 typed kind 与 canonical codec。
- 将 Action lifecycle/context 的重复镜像收敛进唯一 Action instance state；Timeline只保存最小 `ActionInstanceReference`，不复制完整 Action instance bytes。
- 将 GameplayEffect Tags、Attributes、ActiveEffects、Periods、Journal 与 change cursor 收敛为一个类型化 GE state aggregate；GE 应用/移除的局部原子性使用 Character State Transaction 的 typed savepoint，不再通过 canonical bytes Capture/Restore。
- 将 `MotionAccumulator`、`PendingWorldRequest` 和 Motion contribution 保持为 evaluation transaction 内的临时数据，移出 committed State Layout、Snapshot 和 StateHash。
- 让 `CharacterSimulationStateCodec`按 Program State Layout稳定编码/解码 typed committed state；Snapshot Capture、StateHash 和 Network Baseline复用同一份 canonical bytes，不读取 mutable transaction或领域模块内部集合。
- 提升 Float32 Target ABI、Program/State codec version与 Layout identity，重新编译并绑定 Corin `.csim`、ProgramAsset和 Projection；旧 Program/State bytes直接拒绝，不保留 reader、迁移器、兼容 enum或运行时重编译。
- 删除 `CharacterSimulationStateBuilder`、`BuildPending()`、运行时 Action/Input/GE bytes codec、pending motion state slots及全部热路径 `Bytes.ToArray()`。
- 更新 active ServerAuthoritative、DotRecast与 DeterministicRollback change文档和任务依赖，明确网络模型只能持有 canonical committed state bytes和codec identity，不能持有 State Transaction或 typed mutable引用。

## Non-Goals

- 不实现 ServerAuthoritative prediction/reconciliation、Fantasy transport、DotRecast Solver、FixedQ32.32、Deterministic KCC 或 rollback history。
- 不改变 BTSMTL authoring、Semantic IR业务 operation、StateMachine/Timeline/Action/GameplayEffect业务规则、Corin动作配置或动画表现链。
- 不把 Character State Transaction提升为 Pipeline Pass、Session Source、Network Model history或 World State transaction。
- 不把 Float32与 Fixed的具体数值状态做成一个万能泛型对象图；二者共享状态语义和所有权形状，但使用各自 Target ABI实现。
- 不以每 Tick一次 decode/encode缓存作为最终方案，不保留 opaque bytes runtime state。
- 不新增测试或人工验证任务，不运行 Unity batchmode。

## Current Spec Comparison

- `character-simulation-kernel` 当前要求 Evaluate输出 pending state、Finalize再生成新状态，但没有规定 pending state不能是第二份 committed state。本 change修改该合同：Evaluate与Finalize共享一个未提交 transaction，只有Finalize成功后才物化新的 committed Character State。
- `btsmtl-compiled-simulation-program` 当前要求 State Layout包含 `motion pending`，与“只保存影响未来 Tick的 committed Gameplay数据”矛盾。本 change删除该矛盾：同 Tick Motion accumulator与 WorldRequest属于 evaluation transaction，不进入 committed layout。
- `character-input-pipeline`、`character-action-instance-runtime` 已要求 request和ActionInstance进入 Character State，但未禁止 opaque bytes。本 change将这些状态改为明确 typed kind，并删除重复 lifecycle/context镜像。
- `gameplay-effect-runtime` 当前仍写有不存在的独立 `ThirdPersonGameplay` assembly，现行代码和 `openspec/project.md` 已收敛为 portable Core contracts + Target source set。本 change同步修正该过时所有权描述，并规定 GE aggregate属于 Target Character State ABI。
- `character-motion-simulation-boundary` 已要求 Evaluate生成 WorldRequest，但没有明确 contribution/pending request不得进入 committed state。本 change补齐这一边界。
- `character-network-sync-domain-contract` 已要求网络模型只使用 canonical Snapshot。本 change补充 Transaction与typed mutable state不得泄漏到 History、Baseline或packet。
- `gameplay-simulation-pipeline` 的四阶段、SnapshotParticipant和outer atomic commit要求与本 change一致；State Transaction只负责单 Actor Evaluate/Finalize内部写集，不替代 Pipeline working world或外部 Commit，因此不修改该 spec。

## Impact

- Portable Core：调整 Program State kind/schema、State semantic约束与 Target transaction扩展形状。
- Float32 Core：重写 Character State storage、transaction、typed domain state、codec和 Kernel pending/finalize合同。
- Compiler：调整 State Layout emission、Float32 lowering、LayoutHash、Target ABI和 generated artifact发布。
- Input/Action/Timeline/GameplayEffect/Motion：迁移到 typed state port或 evaluation-local port，删除 bytes运行路径。
- Snapshot/Hash：继续输出 canonical bytes，但只从 committed state编码；identity随新ABI变化。
- Network changes：Source/Endpoint/协议/路由可以继续并行；Prediction/Restore相关实现必须绑定新codec。
- Corin：authoring不变，generated Program/Projection和 Definition引用需要正式重建。

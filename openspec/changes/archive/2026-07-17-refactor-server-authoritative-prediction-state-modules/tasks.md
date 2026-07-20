## 1. 锁定行为与状态基线

- [x] 1.1 读取并核对Prediction Correction Pipeline与Simulation Pipeline current specs。
- [x] 1.2 确认`refactor-unity-simulation-assembly-ownership`已完成全部任务并通过strict validation，按用户要求暂不归档。
- [x] 1.3 记录Prediction State全部字段、公开属性、Pass-facing方法和Source port调用点。
- [x] 1.4 记录Correction Schedule、History Egress与Output Disposition的checkpoint/capture/restore时序。
- [x] 1.5 记录Correction v3、History v1与Journal当前schema的magic、字段顺序、排序和count上限。
- [x] 1.6 记录三个StateOwner、StateSchemaId、SchemaVersion与SnapshotParticipant顺序。
- [x] 1.7 记录history capacity、journal capacity、confirmed pruning与replay range规则。
- [x] 1.8 记录ack、baseline、hard recovery与restore build的现有mutation顺序。
- [x] 1.9 记录Program/Layout/OperationSet/Solver/Actor/World identity validation清单。
- [x] 1.10 锁定packet、protocol、checkpoint、Pipeline identity和Policy不变的删除清单。

## 2. 建立Confirmation与Request模块

- [x] 2.1 定义内部`ServerAuthoritativePredictionConfirmationState`。
- [x] 2.2 迁移ConfirmedInputSequence与ConfirmedEventHorizon唯一所有权。
- [x] 2.3 迁移LastAuthorityAckTick、LastBaselineTick与LastAuthorityClockEstimate唯一所有权。
- [x] 2.4 迁移pending request集合与sequence去重规则。
- [x] 2.5 迁移ScheduleRequests retain/consume行为。
- [x] 2.6 迁移pending request capacity校验。
- [x] 2.7 建立不可变Confirmation checkpoint。
- [x] 2.8 删除aggregate root中的重复cursor与request集合。

## 3. 建立Prediction History模块

- [x] 3.1 定义内部`ServerAuthoritativePredictionHistory`。
- [x] 3.2 迁移按Tick排序的history record集合。
- [x] 3.3 迁移AddHistory identity与duplicate Tick校验。
- [x] 3.4 迁移TryGet、First、Last与GetReplayAfter查询。
- [x] 3.5 迁移JournalCursor seal行为。
- [x] 3.6 迁移HistoryCapacity淘汰规则和精确错误上下文。
- [x] 3.7 迁移confirmed input sequence pruning。
- [x] 3.8 让History只返回first retained tick而不修改Journal。
- [x] 3.9 建立不可变History checkpoint。
- [x] 3.10 删除aggregate root中的重复history集合与helper。

## 4. 建立Disposition Journal模块

- [x] 4.1 定义内部`ServerAuthoritativePredictionDispositionJournal`。
- [x] 4.2 迁移EventId entry集合、cursor与LastRejectedCount唯一所有权。
- [x] 4.3 迁移Record去重与AuthorityConfirmed终态规则。
- [x] 4.4 迁移WasCommitted查询。
- [x] 4.5 迁移authority horizon confirmation/rejection计算。
- [x] 4.6 迁移缺失horizon EventId补录规则。
- [x] 4.7 迁移journal capacity规则。
- [x] 4.8 迁移基于first retained history tick的prune规则。
- [x] 4.9 建立不可变Journal checkpoint。
- [x] 4.10 删除aggregate root中的重复journal集合与helper。

## 5. 建立Reconciliation模块

- [x] 5.1 定义内部`ServerAuthoritativePredictionReconciler`。
- [x] 5.2 迁移baseline Program/Layout/OperationSet/Solver identity校验。
- [x] 5.3 迁移Actor、WorldRevision和local history identity校验。
- [x] 5.4 迁移Character state hash与body position/yaw误差计算。
- [x] 5.5 迁移NoCorrection、RestoreReplay与HardRecovery decision构造。
- [x] 5.6 迁移MaximumReplayTicks约束。
- [x] 5.7 迁移baseline替换Character/World snapshot逻辑。
- [x] 5.8 迁移Prediction Pipeline state projection重建逻辑。
- [x] 5.9 迁移restore snapshot identity与RestorePort store计划。
- [x] 5.10 确认Reconciler不持有可变History、Journal或Confirmation状态。

## 6. 集中Canonical State Codec

- [x] 6.1 定义内部`ServerAuthoritativePredictionStateCodec`。
- [x] 6.2 迁移Correction v3 exact-byte写入与读取。
- [x] 6.3 迁移History v1 exact-byte写入与读取。
- [x] 6.4 迁移Journal当前schema exact-byte写入与读取。
- [x] 6.5 迁移Pipeline projection nested codec。
- [x] 6.6 保持字段顺序、排序、magic、version和count上限不变。
- [x] 6.7 让codec只返回完整模块快照，读取失败不得修改活动状态。
- [x] 6.8 删除旧单体codec、header/count helper和兼容reader可能性。

## 7. 收敛Prediction Aggregate Root

- [x] 7.1 让`ServerAuthoritativePredictionState`构造并唯一拥有四个内部模块。
- [x] 7.2 保持Source port与现有Pass-facing调用合同不变。
- [x] 7.3 将简单属性和查询降低到唯一模块。
- [x] 7.4 让ApplyAck先准备Journal reconciliation再提交cursor。
- [x] 7.5 让baseline confirmation先完成全部validation再提交History/Journal/Confirmation变化。
- [x] 7.6 让BuildRestore先构造完整snapshot与directive再提交模块变化和Store。
- [x] 7.7 保持NoCorrection baseline推进、confirmed pruning与journal pruning语义不变。
- [x] 7.8 保持HardRecovery history不可用行为不变。
- [x] 7.9 保持三个Pass checkpoint/rollback分别映射对应模块。
- [x] 7.10 禁止Pass直接取得或创建内部子模块。
- [x] 7.11 删除旧checkpoint DTO、重复集合、委托桥接和未引用helper。

## 8. 核对Pipeline与DotRecast依赖

- [x] 8.1 核对Correction Schedule仍是唯一restore/replay/current plan producer。
- [x] 8.2 核对History Egress仍是唯一history capture owner。
- [x] 8.3 核对Output Disposition仍是唯一EventId disposition owner。
- [x] 8.4 核对三个StateOwner、SchemaId、SchemaVersion和Participant顺序未变化。
- [x] 8.5 核对Network checkpoint、baseline、ack与remote presentation bytes未变化。
- [x] 8.6 更新`add-dotrecast-authoritative-server-backend`implementation inventory为模块化Prediction aggregate。
- [x] 8.7 确认DotRecast change不新增专属History、Correction、Journal或codec。
- [x] 8.8 更新`openspec/project.md`的Prediction State内部所有权说明。

## 9. 编译与严格校验

- [x] 9.1 编译`ThirdPersonSimulation.Core`与`ThirdPersonSimulation.Float32`。
- [x] 9.2 编译`ThirdPersonSimulation.ServerAuthoritative`与Transport程序集。
- [x] 9.3 编译Unity Runtime与Editor相关程序集确认调用合同未断裂。
- [x] 9.4 所有dotnet build/msbuild命令带`--disable-build-servers /nr:false /p:UseSharedCompilation=false`。
- [x] 9.5 每轮编译后立即执行`dotnet build-server shutdown`。
- [x] 9.6 运行`openspec validate refactor-server-authoritative-prediction-state-modules --strict --no-interactive`。
- [x] 9.7 运行`openspec validate --all --strict --no-interactive`并解决本change引入的冲突。
- [x] 9.8 核对全部task勾选与真实模块、codec和删除状态一致。

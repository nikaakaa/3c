## MODIFIED Requirements

### Requirement: Runtime producer 必须在正式生命周期边界发布 Trace

SimulationKernel、Pipeline Runtime/Pass、Session Source、WorldSolver adapter和 Committer MUST分别在自己的正式边界发布 Trace。Trace MUST记录 BackendId、PipelineHash、PassId、product、outer source tick、内部 SimulationStep、成功、失败、restore、replay与 OutputDisposition；Egress disposition MUST不能通过 Publish、Replace、Retire或 Suppress隐藏 Trace。Trace MUST不反向驱动 Character/World/Pipeline state、Source policy或 Presentation result。

#### Scenario: 一次 Motion 执行

- **WHEN** Program Evaluate Pass生成 request且 WorldSolve Pass取得 Solver result
- **THEN** Trace MUST区分 operation、Pass、request、solver result、Finalize、published body sample与 OutputDisposition
- **AND** MUST保留当前 PipelineHash和内部 Step provenance

### Requirement: 每个 runtime target 必须拥有按需 Live State 与显式 Capture

每个 Character Session diagnostics target MUST注册 metadata、Program revision、Pipeline/Backend identity、Source Map与默认 `None` 的 diagnostics store。Live State MUST只保存稳定键对应的当前事实；只有作者显式开始 Capture时才创建独立有界 Capture segment store。Capture达到容量后 MUST按完整 outer tick、SimulationStep或 presentation frame segment丢弃最旧数据。target结束时 runtime MUST释放 store；Editor MUST只保留已冻结的 current state或 Capture snapshot，不得继续持有 runtime target、Pass runtime或可写 store。

#### Scenario: Session Pipeline Runtime 结束

- **WHEN** CharacterPipelineHost deactivate或 Session runtime handle dispose
- **THEN** diagnostics store MUST失效全部 interest并发布 target lifecycle终止
- **AND** Editor Session MUST冻结最后一个 source-mapped current state、Pipeline identity和 active Capture
- **AND** Ended view MUST不接收新事件或持有 runtime store


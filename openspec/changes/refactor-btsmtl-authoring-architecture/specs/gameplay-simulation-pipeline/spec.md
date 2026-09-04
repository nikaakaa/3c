## MODIFIED Requirements

### Requirement: Pipeline 必须以四阶段和固定 Commit 边界执行

Pipeline schema MUST只定义 Ingress、Schedule、Step 与 Egress 四个顶层阶段。Ingress MUST只产生 source/input/ingress 产品；Schedule MUST恰有一个 `SimulationSessionExecutionPlan` producer；Step Pass MUST对 plan 中每个内部 SimulationStep 执行；Egress MUST只消费 step result并生成 snapshot/history/hash/source output 与 EventId disposition。最终 state publish 与 external Commit MUST由 Execution Backend 的固定事务边界拥有，不得实现为可替换 Pass。

#### Scenario: Local 外层 Tick

- **WHEN** GameplayTickSystem 向 Local runtime handle提交一个 LocalLogicTick
- **THEN** Pipeline MUST按 Ingress、Schedule、一个 Step sequence和 Egress执行
- **AND** 只有全部阶段校验成功后 Backend MAY原子发布 state并调用 Committer

#### Scenario: Pass 尝试跨阶段越权

- **WHEN** Ingress Pass尝试直接替换 Character state或 Egress Pass尝试重新执行 Program operation
- **THEN** phase contract或 product ownership校验 MUST拒绝该组合
- **AND** MUST不通过万能 Context 暴露越权写入口
#### Scenario: 代码控制和技能进入Step

- **WHEN** Session执行某Actor的正式模拟Step
- **THEN** C#控制与技能解释器 MUST共同位于原Evaluate／WorldResolve／Finalize链
- **AND** MUST不在Host或Network Pass旁边增加控制器Update／技能Tick


### Requirement: Pipeline Compiler 必须在 Active 前完成完整兼容校验

Pipeline Compiler MUST在 Runtime创建前校验 phase/order、Schedule唯一性、product producer/consumer、依赖环、Pass factory/version、Source port、Program Runtime ABI、Execution Backend semantic version、Solver capability、Replay/Restore requirement和 state ownership。编译 MUST产生稳定 PipelineHash和不可变 plan；unknown Pass、unknown product、缺失 factory或 unsupported capability MUST明确失败，不得跳过、替换或降级。

#### Scenario: Rollback Pass 配置到 Unity Local 组合

- **WHEN** Pipeline Pass要求 DeterministicReplay和 Snapshotable Solver，但 composition使用 Float32 Local Program Runtime与 Unity CharacterController Solver
- **THEN** Pipeline compile MUST在首 Tick前失败并列出缺失能力
- **AND** MUST不删除 Rollback Pass或改用 Local单步执行
#### Scenario: 控制或技能状态不支持恢复

- **WHEN** 所选模型要求恢复而控制模块或技能状态合同不完整
- **THEN** Pipeline／Composition MUST在Active前拒绝
- **AND** MUST不按是否来自C#跳过恢复能力检查

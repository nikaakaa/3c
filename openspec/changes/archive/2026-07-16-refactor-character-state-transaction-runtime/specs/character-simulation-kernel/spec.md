## MODIFIED Requirements

### Requirement: Character State 必须通过单一 Target Transaction推进

每个Actor的每个SimulationStep MUST以当前committed `CharacterSimulationState`为只读基线创建一个target-specific State Transaction。Program Evaluate与Program Finalize MUST读写同一个transaction；WorldSolver MUST只消费WorldRequest并不得访问transaction。Transaction MUST在Finalize全部校验和输出构造成功后恰好Commit一次，失败时MUST Abort且不得修改base state。Transaction MUST NOT进入Snapshot、History、Network payload、Pipeline participant state或Presentation。

#### Scenario: Evaluate与Finalize共享写集

- **WHEN** Actor在Evaluate中消费Dodge request并由WorldSolver返回匹配结果
- **THEN** Finalize MUST在同一个未提交transaction中读取已消费request和Action state
- **AND** Finalize成功后MUST只生成一份新的committed Character State

#### Scenario: WorldResult不匹配

- **WHEN** Finalize收到Actor、Tick、RequestId或SolverId不匹配的WorldResult
- **THEN** State Transaction MUST Abort
- **AND** base Character State与Pipeline正式working world MUST保持不变

### Requirement: Committed Character State 必须使用类型化不可变存储

Committed `CharacterSimulationState` MUST按Program State Layout保存类型化、不可变的state partitions。Runtime领域模块 MUST通过预验证typed address读写transaction，不得以opaque bytes、runtime decode cache、mutable object dictionary或字符串owner查找保存Gameplay状态。State Commit MUST复用未修改partition/page，并只冻结dirty write-set；不得为每个Tick固定复制全部StateSlot两次。

#### Scenario: 当前Tick只修改少量状态

- **WHEN** Actor只推进Runnable cursor、Timeline time和FactSequence
- **THEN** Commit MUST复用其它未修改state pages与GameplayEffect aggregate
- **AND** MUST NOT遍历并复制全部Program StateSlot作为Builder快照

### Requirement: SimulationKernel 必须分离 Evaluate 与 Finalize

SimulationKernel MUST提供无外部副作用的Evaluate与Finalize。Evaluate MUST只接收NumericProfile完全匹配的CharacterSimulationProgram、CharacterSimulationInput、committed CharacterSimulationState、SimulationIngress、SimulationTick和上一Tick body observation，创建当前Actor/Step唯一State Transaction，并输出持有该未提交transaction的PendingCharacterEvaluation与WorldRequest。Finalize MUST只接收同一target ABI、Program/Layout、Actor和Tick的pending evaluation及精确匹配的WorldSolverResult，继续写入同一transaction并在成功时输出新committed CharacterSimulationState与`SimulationActorTickResult`。Kernel MUST不读取Unity Time、Camera、InputAction、Transport、Network packet或Presentation object。

#### Scenario: Local Session 推进一个角色

- **WHEN** Standard Local Pipeline为当前Actor提交SimulationTick与portable input
- **THEN** Evaluate MUST产生未提交transaction与world request
- **AND** Finalize MUST等待匹配world result后才Commit新状态并产生输出

### Requirement: Character 与 World 状态必须分属不同 owner

CharacterSimulationState MUST只保存单Actor且会影响当前Commit后或未来SimulationTick的类型化Gameplay逻辑状态；同Step的MotionContribution、MotionAccumulator、PendingWorldRequest、输出staging与State Transaction MUST不进入committed Character State。WorldSimulationState MUST保存ordered body state、solver-owned mutable state、world revision与static world identity。影响未来Pipeline执行的Pass状态 MUST进入独立SimulationPipelineStateSnapshot或正式reconstruct合同；Session Source external state与Presentation state MUST不进入Character/World状态容器。

#### Scenario: 动画淡出继续推进

- **WHEN** Animancer fade在两个SimulationTick之间推进
- **THEN** CharacterSimulationState、WorldSimulationState与Pipeline Gameplay state MUST不改变

#### Scenario: Evaluate生成当前Step位移请求

- **WHEN** Timeline和Locomotion在Evaluate中形成CharacterMotionRequest
- **THEN** request MUST只进入当前PendingCharacterEvaluation与WorldSolve产品
- **AND** MUST不进入committed Character State或Snapshot

### Requirement: SimulationWorldSnapshot 必须原子 Capture 与 Restore

Session snapshot MUST聚合ProgramCatalogHash、每Actor Program binding、BackendId/version、PipelineId/Hash、Pipeline state participant identity、State codec identity、Solver/world identity、SimulationTick、stable roster、全部committed CharacterSimulationState canonical bytes、WorldSimulationState与需要回滚的Pipeline state。Capture MUST只编码committed typed state，不得读取active State Transaction。Restore MUST在step loop开始前校验并原子替换完整working world，MUST不只恢复Transform、单Actor、部分Pass、部分领域aggregate或未提交transaction。

#### Scenario: 恢复 Attack2 中的双 Actor Pipeline world

- **WHEN** Schedule Plan请求恢复一个ActorA正在Attack2、ActorB正在移动且包含合法Pipeline participant状态的snapshot
- **THEN** 两个typed Character state、World state与Pipeline state MUST在同一restore transaction中恢复
- **AND** 任一payload、codec identity或PipelineHash失败时当前正式world MUST保持不变

### Requirement: State Hash 必须区分 Character 与 World 有效性

系统 MUST提供CharacterStateHash与SimulationWorldHash。CharacterStateHash MUST覆盖ProgramHash、NumericProfile、Target ABI、Character layout、State codec identity与canonical committed Character state bytes；MUST不覆盖active transaction、evaluation workspace或同Step transient motion。WorldHash MUST再覆盖ProgramCatalogHash、全部Actor binding、BackendId/semantic version、PipelineHash、Pipeline snapshot participant state、Solver identity/version、world revision、SimulationTick、stable roster与WorldSimulationState。只有Program Runtime、Backend、Pipeline全部Pass、Catalog全部Program与Solver都声明DeterministicReplay时，WorldHash MAY被声明为跨机器确定性判定。

#### Scenario: Unity Solver 产生本地 WorldHash

- **WHEN** Local Session使用Float32 Pass Backend与UnityCharacterControllerWorldSolver
- **THEN** 系统 MAY生成本地capture一致性hash
- **AND** diagnostics MUST标记该WorldHash不具备跨机器deterministic validity

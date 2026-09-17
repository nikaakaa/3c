## MODIFIED Requirements

### Requirement: SimulationKernel 必须分离 Evaluate 与 Finalize

SimulationKernel MUST提供无外部副作用的Evaluate与Finalize。Evaluate MUST只接收NumericProfile完全匹配的角色领域运行绑定、CharacterSimulationInput、committed CharacterSimulationState、SimulationIngress、SimulationTick和上一Tick body observation，创建当前Actor/Step唯一State Transaction，并输出持有该未提交transaction的PendingCharacterEvaluation与WorldRequest。Finalize MUST只接收同一target ABI、玩法内容／状态格式、Actor和Tick的pending evaluation及精确匹配的WorldSolverResult，继续写入同一transaction并在成功时输出新committed CharacterSimulationState与`SimulationActorTickResult`。Kernel MUST不读取Unity Time、Camera、InputAction、Transport、Network packet或Presentation object。

#### Scenario: Local Session 推进一个角色

- **WHEN** Standard Local Pipeline为当前Actor提交SimulationTick与portable input
- **THEN** Evaluate MUST产生未提交transaction与world request
- **AND** Finalize MUST等待匹配world result后才Commit新状态并产生输出

#### Scenario: Local Session推进一个角色

- **WHEN** Standard Local Pipeline为当前Actor提交SimulationTick与portable input
- **THEN** Evaluate MUST产生未提交transaction与WorldRequest
- **AND** Finalize MUST等待匹配WorldSolverResult后才Commit新状态并产生输出



### Requirement: Committed Character State 必须使用类型化不可变存储

已提交角色状态 MUST按领域分区保存，Control、Input、Ability、Effect、Equipment 与跨 Tick Motion 各有唯一格式和 owner。运行模块 MUST通过正式 typed 状态合同读写同一个角色 Step 事务，不得使用任意对象反射、opaque bytes 或 mutable dictionary 代替业务状态。Commit MUST复用未修改分区并只提交本次有效写集，不固定复制全角色状态；角色 MUST不依赖 Program State Layout。

#### Scenario: 当前Tick只修改少量状态

- **WHEN** Actor只推进Runnable cursor、Timeline time和FactSequence
- **THEN** Commit MUST复用其它未修改state pages与GameplayEffect aggregate
- **AND** MUST NOT遍历并复制全部全角色状态作为Builder快照



### Requirement: SimulationWorldSnapshot 必须原子 Capture 与 Restore

Session snapshot MUST聚合GameplayContentCatalogHash、每Actor 领域运行绑定、BackendId/version、PipelineId/Hash、Pipeline state participant identity、State codec identity、Solver/world identity、SimulationTick、stable roster、全部committed CharacterSimulationState canonical bytes、WorldSimulationState与需要回滚的Pipeline state。Capture MUST只编码committed typed state，不得读取active State Transaction。Restore MUST在step loop开始前校验并原子替换完整working world，MUST不只恢复Transform、单Actor、部分Pass、部分领域aggregate或未提交transaction。

#### Scenario: 恢复 Attack2 中的双 Actor Pipeline world

- **WHEN** Schedule Plan请求恢复一个ActorA正在Attack2、ActorB正在移动且包含合法Pipeline participant状态的snapshot
- **THEN** 两个typed Character state、World state与Pipeline state MUST在同一restore transaction中恢复
- **AND** 任一payload、codec identity或PipelineHash失败时当前正式world MUST保持不变

#### Scenario: 恢复双Actor攻击状态

- **WHEN** Schedule请求恢复Actor A正在Attack2、Actor B正在移动且包含合法Pipeline participant state的Snapshot
- **THEN** 两个typed Character state、World state与Pipeline state MUST在同一restore transaction中恢复
- **AND** 任一payload、codec identity或PipelineHash失败时正式world MUST保持不变



## ADDED Requirements

### Requirement: Kernel必须通过领域接口执行角色而非整体操作表

Evaluate／Finalize MUST保留同一 actor／Tick 事务及原业务顺序，通过控制、技能、运动、效果和装备的正式接口运行。只有技能内部使用自己的执行数据，Kernel MUST不解释整角色操作表，不让 Source、Pass 或 Solver 拥有第二份角色状态。

#### Scenario: 控制与技能共同产生运动
- **WHEN** 当前 Tick 的控制与活动技能各提交运动贡献
- **THEN** 同一 Evaluate MUST按原仲裁生成唯一请求，Finalize 等待对应世界结果后一次提交

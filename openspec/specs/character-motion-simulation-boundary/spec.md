# character-motion-simulation-boundary Specification

## Purpose
定义 Character Program 产生 portable motion request、Simulation Session 批量调用唯一 WorldSolver、World state 保存逻辑 body 真值以及 Network Model 装配独立运动后端的边界。
## Requirements
### Requirement: 运动语义、世界约束执行和逻辑位姿必须分层

Compiled motion operations MUST在Evaluate阶段产生当前Numeric Target的contribution；唯一Motion accumulator MUST按Channel、Priority、Weight、BlendMode与ConsumeLowerChannels将Locomotion和Timeline contribution解析为每Actor唯一`ResolvedGameplayMotion`。当前Target唯一Body Motion Integrator MUST在全部Program Motion Modifier之后，根据committed `WorldBodyState`与compiled descriptor生成每Actor唯一`CharacterMotionRequest`和同Step plan。正式Execution Backend的WorldSolve Pass MUST汇总当前Step全部Actor request并调用一次`ICharacterWorldSolver.ResolveBatch`；Solver提供真实applied displacement、稳定Grounded与Collision后 MUST通过Target唯一Body Motion Finalize提交VerticalVelocity，Program Finalize MUST再产生唯一`CharacterBodySample`与Motion GameplayFact。Graph、Timeline、Action、Source、Presentation与concrete Solver MUST不拥有第二份Motion仲裁、重力积分或逻辑Transform真值。

#### Scenario: Timeline MotionCurve 提交位移

- **WHEN** compiled Timeline 在当前 Tick 产生 Action motion contribution
- **THEN** Timeline module MUST只提交带稳定 source、channel、priority、weight、space 与 blend mode 的 contribution
- **AND** 唯一Target Motion accumulator MUST与同Tick Locomotion contribution一起解析出ResolvedGameplayMotion
- **AND** Body Motion Integrator MUST把玩法Y与环境gravity delta合成为一个CharacterMotionRequest
- **AND** request MUST与同 Tick其它 Actor request 一起进入唯一 ResolveBatch
- **AND** Finalize MUST记录Solver actual result与committed VerticalVelocity

#### Scenario: Timeline 与 Locomotion 同 Tick 提交

- **WHEN** Timeline Action channel 与普通 Locomotion channel 在同一 Tick 都有 contribution
- **THEN** MUST由同一个 Motion accumulator 按正式 channel 消费和混合规则处理
- **AND** Timeline、StateMachine 或 Action module MUST不各自生成竞争的 WorldRequest

### Requirement: WorldSolver 合同不得依赖 Unity 或业务作者结构

ICharacterWorldSolver contract shape MUST只使用当前 NumericProfile 的 portable world state、batch request/result、capability 和 Tick identity。UnityCharacterControllerWorldSolver MUST实现 Float32 target ABI并 MAY在 adapter 内使用 Unity API，但 Program、Kernel 和合同 assembly MUST不引用 CharacterController、Transform 或 UnityEngine 数值类型。

#### Scenario: Unity Solver

- **WHEN** Local Session 调用 UnityCharacterControllerWorldSolver
- **THEN** CharacterController.Move MUST只出现在该 adapter concrete implementation 内
- **AND** adapter MUST返回 portable batch result

### Requirement: WorldSimulationState 必须唯一拥有逻辑位姿读写

WorldSimulationState中的 BodyState MUST替代 Logic Pose Port/Transform作为 Core逻辑位姿真值。Unity Host MUST只在 WorldSolver binding与 Presentation adapter边界将 committed BodyState对齐到场景对象，MUST不让 Transform、Character stage、Session Source、其它 Pipeline Pass或 Presentation保存第二份可反写逻辑真值。

#### Scenario: 应用 Solver Result

- **WHEN** WorldSolve Pass返回 Actor的 actual body result且 outer transaction成功
- **THEN** Execution Backend MUST在最终 Commit前更新唯一 WorldSimulationState
- **AND** Presentation MUST只从 committed body sample驱动 visual root

### Requirement: Unity CharacterController 必须只存在于正式 adapter 内

Unity CharacterController引用、Move调用和场景 body binding MUST只存在于 UnityCharacterControllerWorldSolver/Host adapter内。Graph、Timeline、Kernel、Session Source、Pipeline Pass、Execution Backend、Network Model、Committer和 Diagnostics MUST不直接调用它。

#### Scenario: 搜索 CharacterController.Move

- **WHEN** 迁移完成后检查 Character Gameplay主线
- **THEN** concrete Move调用 MUST只存在正式 Unity WorldSolver implementation

### Requirement: 权威服务端必须独立生成并执行 canonical motion

服务端权威运动 MUST从服务端接受的canonical input、Action state与Program配置生成 `CharacterMotionRequest`，并使用选定authoritative WorldSolver得到canonical body。客户端预测的request、body sample或Transform MAY用于prediction comparison与diagnostics，但 MUST NOT成为服务端canonical displacement或body来源。

#### Scenario: Unity 权威服务端

- **WHEN** ServerAuthoritativeHybrid 选择 Unity process backend
- **THEN** 服务端 MUST 独立推进 canonical input/action motion 语义
- **AND** MUST 使用服务端 Unity executor 产生 canonical pose

#### Scenario: DotRecast 权威服务端

- **WHEN** ServerAuthoritativeHybrid 选择InProcess DotRecast authority backend
- **THEN** 服务端 MUST 独立推进 canonical input/action motion 语义
- **AND** MUST使用正式 `DotRecastWorldSolver` 完成navigation surface、ground、slope、step、wall slide与actor contact约束
- **AND** MUST不把单独的navigation query或客户端body当作完整Solver结果

### Requirement: 确定性模拟必须属于独立完整 Network Model

Deterministic KCC、CollisionWorldArtifact、canonical input bundle、Fixed Program/State/Kernel、Fixed `SimulationWorldStateSet/WorldSimulationState/SimulationWorldSnapshot` history、restore/replay、state hash、snapshot recovery 和 side-effect commit MUST共同属于完整 DeterministicRollback Network Model。该模型 MUST从与 Float32 模型相同的 validated Semantic IR artifact 生成独立 Fixed ABI，但 MUST不复用 Float32 CharacterSimulationProgram/SimulationKernel，也 MUST不使用 Unity CharacterController、DotRecast 或 ServerAuthoritative correction 作为 deterministic world execution。

#### Scenario: 完整安装 Rollback Model

- **WHEN** ModelDefinition、Endpoint、History、KCC、Collision World、Replay、Hash、Recovery 和 Committer 全部可用
- **THEN** DeterministicRollback MAY出现在 SessionHost authoring UI

### Requirement: World Solver 必须一次处理当前 Session 的 Actor batch

正式 WorldSolve Pass MUST按 stable ActorId顺序构造 WorldSolveBatchRequest，并在当前 SimulationStep全部 Actor Evaluate完成后调用一次当前唯一 WorldSolver。Solver MAY按自己的正式语义顺序处理角色或共同求解，但 MUST返回与 request set精确一一对应的 result。其它 Pass和 Character Host MUST不直接调用 world mutation。

#### Scenario: 两个 Actor 同 Tick 请求移动

- **WHEN** ActorA与 ActorB在同一 SimulationTick都产生 MotionRequest
- **THEN** 两个 request MUST进入同一个 batch
- **AND** MUST不由两个 Character runtime各自独立调用 world mutation

#### Scenario: Rollback batch 中两个 Active Actor 接触

- **WHEN** Fixed Rollback batch 中两个 Active Actor 的静态世界 candidate 在同一 Tick 发生接触
- **THEN** Deterministic KCC MUST按 stable ActorId pair order执行连续 sweep、初始重叠去穿透和 `SolidBodyBlock`
- **AND** MUST在 Actor contact 后重新约束静态世界并验证最终间距
- **AND** MUST只在全部 Actor 成功后原子提交完整 batch body result
- **AND** MUST不调用 DotRecast ActorContactSolver、Unity Physics 或 Presentation correction

### Requirement: Model Correction 必须由具体 Source 与 Pipeline Pass 拥有

Prediction reconciliation、authoritative restore、ack和 visual recovery policy MUST归需要它们的具体 Network Model Source及显式 Ingress/Schedule/Egress Pass。Kernel、motion operation、Common Host和 WorldSolver MUST不读取 server tick、ack、correction packet或 model policy。Schedule Pass只能通过完整 restore directive和 ordered SimulationStep影响 working state，MUST不向 motion contribution注入公共 correction。

#### Scenario: ServerAuthoritative 发现预测差异

- **WHEN** 后续 ServerAuthoritative Source/Pass发现 authoritative observation与预测状态不同
- **THEN** MUST在 model-owned history与 Schedule plan中决定 restore/replay sequence
- **AND** MUST不恢复 Character内部 correction branch或直接修改 Transform

### Requirement: WorldCapability与WorldFeature必须表达不同层级

WorldCapability MUST表达Program/Pipeline依赖的通用结果合同，WorldFeature MUST表达Solver具体世界机制。BodyMotion、Grounding、Collision、Reconstructible与AirborneVerticalMotion MUST作为通用capability；NavigationSurface、Ground、Slope、Step、WallSlide、DynamicObstacle与ActorCollision MUST作为feature。Composer MUST分别校验两者。AirborneVerticalMotion MUST只由完整消费XYZ request、报告稳定Grounded和方向性Above/Below并进入统一Body Motion Finalize的Solver声明。

#### Scenario: Composition要求NavigationSurface

- **WHEN** Program capability满足但Solver没有NavigationSurface feature
- **THEN** Composition MUST失败
- **AND** MUST不把通用Collision当作NavigationSurface

### Requirement: 同一WorldSolver实现必须可服务不同Session Source

WorldSolver MUST只消费portable WorldState与CharacterMotionRequest并返回portable batch result，MUST不读取Session Source、Network Model、packet、ack、history或Presentation。同一Solver实现 MAY装配到Local、Prediction或Authority Session，但每个Session MUST拥有独立runtime实例与WorldState。

#### Scenario: 两个Session使用DotRecast

- **WHEN** 两个不同Source的Float32 Session选择相同DotRecast Solver Definition
- **THEN** 两者 MUST执行同一Solver语义
- **AND** MUST不共享mutable query或WorldState

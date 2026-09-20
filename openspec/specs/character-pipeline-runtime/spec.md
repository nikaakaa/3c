# character-pipeline-runtime Specification

## Purpose

定义 CharacterPipelineHost 的 Actor 注册边界、SimulationSessionHost 的领域组合、Gameplay/World/Presentation 的唯一运行顺序，以及 ScenePlay 对正式 Actor 的观察入口。Graph 只负责图编译和图运行；角色运行时由正式 domain runtime 组合，不恢复整角色 Program、Projection 或第二套预览执行链。

## Requirements

### Requirement: CharacterPipelineHost 只负责装配和注册

CharacterPipelineHost MUST 只加载并校验 CharacterPipelineDefinition 的正式 Control、Ability、Timeline、Presentation、Input、Body 和 diagnostics binding，建立显式 ActorId、World body binding、可选 local input、Gameplay output port、Presentation output port 和 diagnostics metadata，并向显式 SimulationSessionHost 提供不可变 Actor registration。CharacterPipelineHost MUST 不创建 Session Source、WorldSolver、Execution Backend、Pipeline Runtime、Snapshot codec、Committer aggregate 或 Logic target，也 MUST 不选择 Network Model 或 Pipeline。

#### Scenario: 注册单机 Corin

- **WHEN** Sandbox 中的 Corin CharacterPipelineHost 启用
- **THEN** MUST 向显式 SimulationSessionHost 提交一个 Actor registration
- **AND** Local Source、标准 Pipeline、Float32 domain runtime 与 Unity Solver MUST 只由 Session composition 创建

### Requirement: Actor identity 与领域 binding 必须由 Host 单点锁定

每个 CharacterPipelineHost MUST 显式提供唯一非空 ActorId，并在 Actor registration 中绑定 Control、Ability、Timeline、Presentation、World body 与 Input identity。SimulationSessionHost MUST 在 Active 前验证完整 roster 中 ActorId 唯一且各领域 binding 精确匹配；Graph、Timeline、Solver、Pipeline Pass、Session Source 与 Network Model MUST 不生成替代 Actor identity，Active 后 MUST 不修改 ActorId 或 roster binding。

#### Scenario: 注册未知Actor

- **WHEN** Schedule 或 Ingress 提交不在锁定 roster 中的 ActorId
- **THEN** Session MUST 在进入 Step 前失败
- **AND** MUST 不自动创建默认 Character state、World body 或 registration

### Requirement: Session 必须是唯一 Gameplay Tick target

GameplayTickSystem MUST 只注册 SimulationSessionHost/runtime handle 作为同一 Session 的 Input/Logic target，不得为每个 Character、Pass、Session Source 或 Network Model 注册独立 LogicTick。Character Presentation target MAY 按 Actor 独立注册，但 MUST 只消费当前 Session Committer 发布的 samples/commands，并由 Session composition 统一激活和释放。

#### Scenario: Session包含两个Actor

- **WHEN** Fixed LocalLogicTick 到达
- **THEN** GameplayTickSystem MUST 只推进一次 Session runtime handle
- **AND** 两个 Character Presentation runtime MUST 不各自推进 Gameplay domain 或 Pipeline

### Requirement: Gameplay domain、Graph 与 World 必须沿唯一阶段运行

Session MUST 按固定顺序推进 Ingress、Schedule、Graph Runtime/Control、Ability、Timeline、Effect、Equipment、Motion Resolve、WorldSolver、Finalize、Egress、Committer 和 Presentation，并在同一主线内推进ActionInstance。Graph Runtime只执行Graph artifact中的图语义；Control、Ability、Timeline、Effect、Equipment和Motion各自拥有正式domain state。节点和Timeline MUST只产生state mutation、typed GameplayFact、MotionContribution、WorldRequest或稳定EventId；多个运动贡献必须先经唯一Motion accumulator和Body Motion Integrator，再由WorldSolver生成唯一body result。只有WorldSolver MAY改变WorldSimulationState，只有Presentation adapter MAY写visual root，系统不得建立demo专用或第二套deterministic业务路径。

#### Scenario: Dodge Timeline产生位移

- **WHEN** Action Timeline 在当前 Tick 产生 MotionContribution
- **THEN** contribution MUST 进入统一 Motion accumulator 和 WorldRequest batch
- **AND** WorldSolver MUST 返回唯一 body result
- **AND** Timeline、Graph 和 Unity Transform MUST 不直接移动角色

#### Scenario: 本地Action进入下一段

- **WHEN** 输入满足Action准入并触发后续状态
- **THEN** Control、Ability、Graph与Timeline MUST推进同一ActionInstance事实链
- **AND** Committer MUST只提交经过World Resolve、Finalize与Egress的结果

### Requirement: Timeline Gameplay 时钟与表现时钟必须分责

Gameplay Timeline time MUST 由正式 Timeline Runtime 按 SimulationTick 推进。有限 Action 的 playback identity、sample、cycle 和 time scale MUST 通过正式 presentation command 交给 PresentationFrame；PoseStateMachine、Source、AnimationSlot、显式 Player 和 Native Pose Graph evaluation MUST 归 Presentation。Presentation MUST 不推进 Gameplay Timeline，且同一 Timeline MUST 不在多个 owner 时钟之间切换。

#### Scenario: 没有新Logic Tick的RenderFrame

- **WHEN** PresentationFrame 到达但没有新 SimulationTick
- **THEN** Action visual time、Pose source sampling 与 transition MAY 按 presentation delta 推进
- **AND** Timeline Gameplay state 与 Action lifecycle MUST 不改变

### Requirement: Pipeline 输出必须按 Gameplay、Body、Presentation 与 Trace 分离

`SimulationTickResult` MUST 按 Actor 保存 `SimulationActorTickResult`，并分离 GameplayFacts、CharacterBodySample、PresentationCommands 与 TraceRecords。Egress Pass、Committer与Network Model只能消费正式Finalized输出和EventId disposition；MUST不让Presentation output反向改变Gameplay state，不把packet或Pipeline私有状态写入结果，也不得让Fantasy Handler、Room或Model Source直接修改表现对象或动画转换。

#### Scenario: Attack Tick输出

- **WHEN** Attack 产生 Window、Motion 和 animation command
- **THEN** Finalize MUST 以独立 typed channels 保存并共享同一 Event identity
- **AND** Egress MUST 只决定外部 EventId disposition

### Requirement: CharacterPipelineDefinition 只提供正式 authoring context

CharacterPipelineDefinition MUST 持有 InputProfile、Ability grants、domain profile 与 Presentation binding 的正式引用。Runtime Host MUST分别加载并校验匹配revision的Control、Ability、Timeline、Presentation、World与Input binding，不得从Definition、RootTree、Timeline或Effect资产临时克隆运行状态。Editor MAY 从 Definition 打开 Graph/Ability authoring，并将 Definition、Ability grant 和 InputProfile 作为 editor-only context 传入对应窗口；该 context MUST 不创建 Character RootTree、不拥有运行时状态，也 MUST 不改变 Graph Runtime 语义。

#### Scenario: 从Definition打开Ability Graph

- **WHEN** 作者从 CharacterPipelineDefinition 打开正式 Graph/Ability authoring
- **THEN** Graph window MUST 获得当前 Definition、Ability grant 和 InputProfile
- **AND** 该 context MUST 只用于作者操作与只读目录显示

### Requirement: Blackboard 生命周期必须由正式 domain state 管理

Graph-owned Blackboard declaration、ExposedProperty、Graph Data Catalog 和 scope/lifetime MUST 是唯一黑板数据源。Graph/domain preparation MUST 为 declaration、Graph activation、State execution、ActionInstance 和 Frame owner 准备稳定 typed address；运行时 MUST 通过 CharacterSimulationState 的 typed slots 读写，不得依赖字符串 key、runtime dictionary 或第二 Blackboard service。Frame、State、Action 和 Graph generation 结束后，旧值 MUST 不可读且不得通过遍历全部 slot 物理清零。

#### Scenario: Frame变量跨Tick失效

- **WHEN** 当前 SimulationTick Finalize 完成并进入下一 Tick
- **THEN** 新 Frame generation MUST 使上一 Tick 的 Frame value 按 declaration default 读取
- **AND** 未在新 Tick 写入的 Frame group MUST 不产生 dirty page

### Requirement: PresentationFrame 必须只消费已提交事实

SimulationCommitter MUST 保存未消费的有限 Action producer selection、sample、complete、release 与 EventId lifecycle。PresentationFrame MUST 消费 committed Body/Intent、Presentation Fact、Action playback、AnimationSlot、Native Pose Graph、Source、Constraint 和 Final Publication。持续 Locomotion PoseState、source relevance 与 transition MUST 只存在于 Presentation workspace，不得写入 Gameplay command queue；Presentation 失败 MUST 不回写 Gameplay state。Remote Body sample、Action producer command与reliable EventId只能在正式Commit边界进入表现输出，并复用同一Interpolation、AnimationSlot和Native Pose Graph，不得创建远端专用播放器。

#### Scenario: Action等待首个合法Sample

- **WHEN** Gameplay 已提交 Action selection 但 Presentation 尚无合法 playback sample
- **THEN** AnimationSlot MUST 按正式 Pending/Availability policy 等待或失败
- **AND** Locomotion PoseState MUST 继续来自当前 committed Body/Intent fact

### Requirement: ScenePlay 必须观察正式Session Actor

ScenePlay MUST 通过正式 Session composition 创建或连接 Actor，并只管理 Session lifecycle、输入、Ability request、Timeline UI 和 Runtime Debug binding。Timeline、Pose Graph、Motion Matching 与 Equipment 页面 MUST 不创建独立 evaluator、preview player、事实夹具、独立时钟、临时 PlayableGraph 或第二 Session。

#### Scenario: ScenePlay选择Ability

- **WHEN** 作者在 ScenePlay 选择一个 Ability 并点击 Start
- **THEN** ScenePlay MUST 向正式 Session 提交 Ability request 并显示该 Actor 的正式 playback、Window、Pose 和 completion facts
- **AND** 页面 MUST 不自行执行 Timeline、Pose 或 Action lifecycle

### Requirement: 运行时失败必须显式失败并按Owner清理

Binding、Graph artifact、domain preparation、Pipeline、WorldSolver、Finalize 或 Committer 任一阶段失败时，Session MUST 进入明确 Failed/Faulted 状态，按 owner 释放已取得资源并停止后续 LogicTick。系统 MUST 不回退 Local、其它 Network Model、默认 Pipeline、默认 Solver、Transform 直写、旧 Program Reader 或兼容 wrapper。

#### Scenario: Graph artifact过期

- **WHEN** Graph artifact 的 source revision 或依赖 identity 与当前 Graph 不匹配
- **THEN** Graph/domain preparation MUST 明确失败
- **AND** Session MUST 不自动重新编译、猜测同名资产或切换到旧运行入口

### Requirement: Diagnostics 必须是统一只读目标

每个 Active Simulation Session 与 Actor roster MUST 注册明确 diagnostics target、Graph Source Map、Backend/Pipeline identity、SourceId、Solver identity、默认关闭的 Live/Capture store 和 structured Trace。Trace MUST能按ActorId、ActionInstanceId、SimulationTick、Graph source、World request/result、domain owner与EventId关联输入、决策、Timeline窗口、Motion、Effect和committed Presentation。Editor MUST 只读取当前 target 或 Capture snapshot，不得持有 runtime Graph、mutable Character/World/Pipeline state、Pass runtime 或 Solver object。

#### Scenario: Local Session结束

- **WHEN** Corin Session deactivate 或 runtime handle dispose
- **THEN** diagnostics registry MUST 冻结最后一个 source-mapped current state 并解除 target
- **AND** Editor MUST 不再接收新事件或持有可写 runtime store

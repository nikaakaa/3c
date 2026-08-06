# gameplay-tick-system Delta

## MODIFIED Requirements

### Requirement: GameplayTickSystem 必须每表现帧推进 PresentationFrame

PresentationFrame MUST继续以render/presentation delta推进visual interpolation、Timeline visual sampling、显式Player节点clock、Animancer source sampling、Character Pose Graph Plan、FootPlacement world-aware阶段、Secondary Motion Physical Publication batch、Camera与committed command lifecycle。`GameplayTickSystem.FrameLateUpdate` MUST为同一RenderFrame先按稳定注册顺序调用全部Presentation target的Prepare阶段，再调用构造时提供的唯一`IGameplayPresentationBatchCoordinator`，最后按同一顺序调用全部target的Finalize阶段。全部阶段 MUST共享同一`GameplayPresentationFrameContext`；旧单方法Presentation target接口 MUST删除。GameplayTickSystem MUST不引用Character、Animation或Magica实现类型；正式产品 MUST装配唯一`CharacterPhysicalPublicationBatchCoordinator`，且该实例 MUST在零Secondary Motion team时完成同一批协议而不是切换Null fallback。Rollback replay MUST只产生EventId output replacement，MUST不直接回卷PresentationFrame或用logic tick代替presentation delta。PresentationFrame MUST不调用Kernel Evaluate/Finalize、Gameplay WorldSolver.ResolveBatch或修改Character/World state。

#### Scenario: 高渲染帧率下的表现帧

- **WHEN** 两个SimulationTick之间发生多个PresentationFrame
- **THEN** Body插值、slot淡入淡出、source sampling、Pose Graph输出与Secondary Motion MAY连续推进
- **AND** Session runtime handle MUST不被额外推进

#### Scenario: 同帧多个Presentation target使用Magica

- **WHEN** 多个target在Prepare阶段登记合法Secondary Motion team
- **THEN** GameplayTickSystem MUST在全部Prepare完成后调用一次global Magica manual batch
- **AND** 任一target MUST只在该batch完成后Finalize Final Pose与Camera

#### Scenario: Replay后替换动画选择

- **WHEN** Output Disposition Pass产生FullBodyAction EventId replacement
- **THEN** PresentationFrame MUST从该slot当前视觉结果处理新command
- **AND** MUST继续以presentation delta推进唯一Pose Plan及其中显式Player节点与Secondary Motion状态

### Requirement: GameplayTickSystem 必须通过 target 接口调度业务对象

GameplayTickSystem MUST以 SimulationSessionHost/runtime handle作为每个 Session唯一 Input/Logic target，而不是为同一 Session中的 Character、Pass、Session Source、Endpoint或 Network Model分别注册 LogicTick。Presentation target MUST实现统一Prepare与Finalize批接口，并由GameplayTickSystem之间的唯一Physical Publication Coordinator完成全局表现屏障；target MUST不注册私有LateUpdate runner或自行调用全局Magica Manager。Target在 Preparing状态 MAY推进正式preparation但 MUST不执行 Program；进入 Active后 MUST将每个 source tick只交给runtime handle一次，Pipeline Runtime再按 compiled ExecutionPlan推进 roster、内部 step和 world batch。

#### Scenario: 双 Actor Session 被调度

- **WHEN** 同一 Session roster包含 ActorA与 ActorB
- **THEN** GameplayTickSystem MUST只调用一次 Session logic target
- **AND** 每个内部 Step MUST按 stable ActorId顺序处理两个 Actor

#### Scenario: 两个Presentation target进入同一Physical batch

- **WHEN** ActorA与ActorB分别由不同Presentation target拥有
- **THEN** GameplayTickSystem MUST先完成两者Prepare再执行一次Global Physical Publication Coordinator
- **AND** MUST不按target调用次数推进Magica global time

#### Scenario: Network Model 尚在 Preparing

- **WHEN** Session preparation正在等待 endpoint handshake、launch roster或 Pipeline factory
- **THEN** GameplayTickSystem MAY推进一次 preparation step
- **AND** MUST不创建 SimulationTick、执行 Kernel或注册第二个 Model/Pipeline runner

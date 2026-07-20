## MODIFIED Requirements

### Requirement: Gameplay Tick 系统必须区分本地逻辑 tick、表现帧和服务端 tick

GameplayTickSystem MUST继续区分 fixed LocalLogicTick、PresentationFrame和网络 Source中的 ServerTick。SimulationTick MUST是 Session ExecutionPlan内部 step identity，由唯一 Schedule Pass显式映射 source clock；系统 MUST不假定 LocalLogicTick、ServerTick与 SimulationTick数值相同，PresentationFrame MUST不产生 SimulationTick。一次 LocalLogicTick MAY对应零个、一个或多个内部 SimulationTick。

#### Scenario: Local Simulation

- **WHEN** GameplayTickSystem产生一个 fixed LocalLogicTick
- **THEN** Local Schedule Pass MUST将其映射为当前 Local Session的下一个 SimulationTick
- **AND** Program/Kernel MUST不读取 Unity frame time或 ServerTick

#### Scenario: Prediction Replay

- **WHEN** 一个 LocalLogicTick触发 restore和三个内部 replay/current step
- **THEN** 三个 SimulationTick MUST由同一个 ExecutionPlan明确列出
- **AND** GameplayTickSystem MUST不额外生成 replay LogicTick

### Requirement: GameplayTickSystem 必须通过 target 接口调度业务对象

GameplayTickSystem MUST以 SimulationSessionHost/runtime handle作为每个 Session唯一 Input/Logic target，而不是为同一 Session中的 Character、Pass、Session Source、Endpoint或 Network Model分别注册 LogicTick。Target在 Preparing状态 MAY推进正式 preparation但 MUST不执行 Program；进入 Active后 MUST将每个 source tick只交给 runtime handle一次，Pipeline Runtime再按 compiled ExecutionPlan推进 roster、内部 step和 world batch。

#### Scenario: 双 Actor Session 被调度

- **WHEN** 同一 Session roster包含 ActorA与 ActorB
- **THEN** GameplayTickSystem MUST只调用一次 Session logic target
- **AND** 每个内部 Step MUST按 stable ActorId顺序处理两个 Actor

#### Scenario: Network Model 尚在 Preparing

- **WHEN** Session preparation正在等待 endpoint handshake、launch roster或 Pipeline factory
- **THEN** GameplayTickSystem MAY推进一次 preparation step
- **AND** MUST不创建 SimulationTick、执行 Kernel或注册第二个 Model/Pipeline runner

### Requirement: 服务端 tick 必须只通过网络输入进入角色管线

ServerTick MUST只存在于具体 Network Model Source/packet/history或被转换后的 Pipeline source product、ExecutionPlan provenance中。GameplayTickSystem MUST不自增 ServerTick，Local Source/Schedule Pass MUST不从 LocalLogicTick推导 ServerTick，Kernel MUST不读取 ServerTick作为 Program时间。

#### Scenario: 后续模型收到权威 observation

- **WHEN** Model Endpoint收到携带 ServerTick的消息
- **THEN** Model Source MUST在自己的 ExternalSource state中保存 ServerTick
- **AND** 只把 model-neutral ingress、restore directive或 schedule provenance交给 Pipeline

### Requirement: 模型输入命令必须保留 InputSequence 和 LocalLogicTick

具体 Network Model Source/Ingress Pass MUST从 portable CharacterSimulationInput构造模型输入命令，并保留 InputSequence与来源 LocalLogicTick。模型 Endpoint MAY按 20/30Hz flush多个 command，但 flush频率 MUST NOT改变 LocalLogicTick或 Schedule Pass产生的 SimulationTick语义。

#### Scenario: 本地 60Hz 逻辑与 20Hz 网络发送

- **WHEN** Input Adapter/Ingress Pass以 60Hz生成带 InputSequence的 CharacterSimulationInput
- **AND** 网络 peer以 20Hz flush
- **THEN** 每个 command MUST保留自己的 InputSequence与 LocalLogicTick
- **AND** peer MAY在一包中发送多个 command，MUST不把 flush序号当作 SimulationTick


## Context

动机见 `proposal.md`。本设计采用本轮用户确认的组合：角色总 Program 退役、技能图独立编译、Timeline 内容直接调度、网络 Pass 保留、Pose 原生 FlowCanvas Runtime。此前 `docs/locomotion-skill-compilation-boundary-plan-2026-09-13.md` 中保留角色 Program、扩展统一 artifact 的方案只作历史评估，不作为本 change 的实施依据。

调查基于共享工作区，开始规划时仍有大量并行修改。以下源码定位是职责依据，实施前按当前文件内容接续，不回退其它窗口已正确工作。

| 现有入口 | 已核实的约束 | 本次迁移 |
| --- | --- | --- |
| `Runtime/Character/Control/Rules/CorinCharacterControlModule.cs` | C# UnityHFSM 执行控制逻辑；当前可变状态接在 Program slots 上 | 保留逻辑，状态归控制模块并接入完整角色快照 |
| `Editor/CharacterSimulation/Compilation/Semantic/CharacterSemanticFrontendCompiler.cs` | Definition 根、Gameplay catalog、Control 连接、资源、技能混合处理 | 独立 Ability 闭包与目标数据处理 |
| `Runtime/Simulation/Core/Float32/Execution/Float32OperationEvaluator.cs` 及 Fixed 对应实现 | 先 Control Tick，再 Ability Tick，随后 Motion 和提交 | 同一业务顺序迁入角色执行接口，由原 Pass 调用 |
| `Runtime/Character/Pipeline/Animation/Contracts/Pose/CharacterPoseCanvasGraph.cs`、Node、Connection、NativePorts | 初始化／Bind／输出主动拒绝执行，端口类型仅 Editor 占位 | 迁成真实原生运行图和 typed 节点端口 |
| `Runtime/BTSMTL/EventGraphs/NativeEventGraphRuntime.cs` | 已使用 Graph.Clone、Manual StartGraph、UpdateGraph | 借鉴实例、手动时钟和失败传播，保留事件图唯一变量 owner |
| `Runtime/Character/Pipeline/Animation/PoseGraph/Program`、`Worker` | Image、操作数组、Stage、缓冲计划、跨角色 Worker 和状态均有真实消费者 | 保留算法与资源所有权，删除全图操作模型和编译调度 |
| `Runtime/Simulation/Core/Float32/Network/ServerAuthoritative/ServerAuthoritativePredictionReconciler.cs` | Program／Layout 身份与 state codec 用于 baseline 和恢复 | 迁移玩法内容／状态格式身份，保留纠正算法和事务 |

以上路径相对 `3cDemo/Client/3C_Client/Assets/GameScripts/Main/`。

## Goals / Non-Goals

**Goals:**

- 角色运行不依赖整角色 Program，修改技能、控制、Pose 或网络配置只处理其真实依赖。
- 每个领域只有一个算法 owner、一份作者真相和一个运行实例状态 owner；不以新的全角色 artifact 或操作表维持旧模型。
- 保留现有 Gameplay、网络、动画与 IK 行为，只替换作者图到运行的连接、状态载体和产物边界。
- 原生 Pose Runtime 同时服务正式运行、预览与观察；源资源缺失或求值失败明确报错，不能回退旧 Image。

**Non-Goals:**

- 不把 Skill／Ability 改为 FlowCanvas Runtime；不撤销原生 FSM 作者组织、生命周期或独立 Timeline 产品。
- 不删除网络 Pass、Pipeline Compiler、Backend、WorldSolver、Float32／Fixed、预测纠正或确定性回滚，不扩大 DotRecast Solver 能力。
- 不改变 Foot／Pelvis／Goal／FBBIK 公式、source 时间、混合、惯性化或 Root Motion／MotionWarp 结果；不接入 TrainingEnemy。
- 不删除 ACL／Motion Matching 等资源烘焙，不新增插件、自动 watcher、运行时补构建或生成失败兼容路径。
- 不承诺改图后无损保留播放与 IK 历史，不把未测量的性能收益写为完成条件。

## Decisions

### D1. 角色运行由领域模块组合，技能是唯一 Gameplay 图执行数据

角色配置引用 ControlModule、控制配置、AbilityGrants、BodyMotion、Input、Effect、Equipment 和表现配置。实例工厂只做显式安装和绑定，不生成角色全局 operations、资源 catalog 或布局镜像。

角色执行接口保留 `Evaluate → WorldResolveBatch → Finalize` 关系。Evaluate 依次消费输入、推进 C# Control 与 Ability、处理效果及运动贡献；WorldResolveBatch 仍一次处理同 Tick 全部 actor；Finalize 成功后才发布角色、世界与事实结果。业务调用用窄接口，不能把 Profile、World、network packet 和所有状态塞进万能 Context。

Ability 编译输入是对应 `GameplayAbilityDefinition` 的私有图／FSM／条件／子图引用和 TreeClip 引用的技能图；Timeline 只作为精确内容依赖与调用接口输入，轨道／Clip 不进入技能 IR。共享技能只构建一次；授予、装备选择和实例目标在绑定／运行时提供，不为每个 Character Definition 复制技能。

现有数值中立语义处理只保留技能内容，产出独立可校验的 Ability 数据；Float32／Fixed 仅降低技能数值、状态和运算。目标执行数据不保存 C# Locomotion、BodyMotion、全装备目录或动画表现图。所有技能引用的领域 ID 在绑定时由对应模块解析，并在 Session Active 前报告缺失，不在每 Tick 扫描。

**取舍：**独立技能数据利于图编排和两个数值目标／普通 .NET Host；全部改 C# 会取消作者图能力，全部改 FlowCanvas Runtime 则扩大到 Gameplay 确定性、快照及 Unity 依赖迁移，本次均不选择。原 Character Program 不能仅改名 `CompiledAbility` 后原样保留。

### D2. 角色状态按模块归属，统一捕获不要求统一可执行 Program

Control、Ability instances、Input requests、Effect、Equipment 和跨 Tick MotionWarp 各保存唯一正式状态。角色快照在一个成功提交的 Tick 边界收集这些分区，携带模块身份、状态 schema、实例身份和内容身份；Snapshot／Hash 不读取 Pending、原生图对象、临时运动贡献或 World query scratch。

恢复是先解析并核对完整候选，再原子安装 Character／World／Pipeline 状态。C# 控制模块必须同时恢复其可读字段和 UnityHFSM 内部当前状态；技能必须恢复调用帧、Timeline cursor、请求及动作实例，不只恢复位置或 active skill id。不会跨 Tick 读取的派生索引从同一正式输入重建，不再进入快照。

Control 的模块 ID／语义版本、参数与 state schema 在实例绑定中保留，但不再通过 ControlModule catalog 和全角色 slot 编码。控制 Motion 描述直接来自 C# Contract；作者可调数值来自明确配置。SourceCurve 唯一引用现有 RootMotionCurveAsset，并消费 Timeline 领域拥有的同一源区间／播放时间映射；控制侧与技能侧分别取得正式资源绑定，保留帧／秒／Tick、空间和数值转换次序。portable 运行不回读 Unity 资产。旧 CharacterControlMotionCatalogEmitter 不再承担资源供给；输入适配器的 CameraRelative 判断同步改读控制合同。

**取舍：**分型状态减少全局 slot/schema 联动，但需要各模块提供完整 Capture／Restore；保留统一槽表实现简单的整体 codec，却继续让所有模块依赖一份编译产物，因此不选择。不会以反射序列化任意 C# 对象代替正式状态。

### D3. Pose 使用 FlowCanvas 原生实例和连接，不建立加载期 Compiler

Pose 原生图接入 FlowScript 的初始化、端口绑定和 Manual 更新合同；原生运行节点使用同一作者字段，不复制一份 Runtime DTO。现有仅作者 Graph／Node／Connection 在正式资产迁移中改为实际可绑定的类型，保留可保留的 Graph／Node／Port／Edge identity。类型化 Local Pose、Component Pose、参数、事实与 Goal 端口使用真实运行值或资源句柄，不保留 Editor 占位 class。

每个角色拥有独立图实例和节点状态；原生子图引用按调用实例隔离，动态 Linked Pose 选择仍由已有接口和实例生命周期决定，不转换为私有操作表。共享资产不写入角色播放时间、IK 历史或运行缓存。宿主不直接用 FlowScriptController 自主 Update，而是在已有表现时钟调用 Manual Runtime；不得再增加独立时钟。

FlowCanvas ValueInput 每次读取都会调用 getter，因此 Pose 节点按“角色实例＋图调用实例＋本次求值身份＋阶段”缓存结果。Player 时间、状态转换、Foot Placement 和输出副作用每次正式阶段只执行一次。缓存只存本次结果，不是另一份 Graph、IR 或调度计划。

**取舍：**选择直接原生图执行，减少 IR／Image／自建执行器。保留预编译 Image 可保留全图优化但违背本次减少双模型目标；加载时生成 Image 仍维护 Compiler，不作为折中路线。

### D4. 图执行替换不能撤销动画帧与资源的真实边界

表现宿主继续按事件变量更新、源需求准备、唯一 Animancer Evaluate Barrier、姿态／约束求值、统一 Final Publication 驱动。本次为原生图定义固定的准备与求值入口：准备入口通过原生连接取得当前活跃状态／分支及 source demand；源模块完成资源与采样准备后，求值入口通过相同原生连接取得姿态。每个节点内部按明确阶段调用已有算法，准备结果不冒充可读的最终姿态。宿主只传 typed 输入、阶段与输出，不生成节点遍历表或解释节点类型。

PoseState、Player、Slot、BlendStack、Inertialization 保留各自播放、时间、relevance、capture／release 与跨帧历史。进入／退出状态的源保留、Locomotion Phase、有限动作退出淡出和资源就绪条件不能用通用 FlowCanvas 状态变化替代。PoseState 使用原生图上的业务状态节点调用现有动画状态逻辑，不把 Gameplay FSM 套给动画。

Source 唯一拥有 ACL／Playable 采样与物理资源；Constraint 唯一拥有 Foot／Goal／FBBIK；Final Publication 唯一写整套骨骼。节点只取得 typed 输入／结果，不直接操作 Transform。现有提交／丢弃／故障隔离仍保持：失败帧不提交半帧姿态或半更新历史。图替换和 Dispose 先停止本实例调用，再完成已调度算法和释放资源，不让旧实例晚到结果写新角色。

每个活跃节点初始化时建立复用缓冲或使用领域现有 pool，分支输入只读，写入单独输出；不依赖已删除的全图 Value Lifetime 分配器。第一版正式路线采用原生托管图调度，节点内部可复用已有 Native／Job 算法并在消费结果前完成；不保留旧跨节点、跨角色 Program Worker 批调度，也不新增双模式开关。算法并行和全图编译调度是两个边界。

**取舍：**保留动画帧事务和算子资源管理会留下必要运行代码，但不会重新创建 Compiler。原生调度可能增加端口调用、实例内存和临时缓冲峰值；本次接受取消全图自动优化的结构变化，不声称性能等价。

### D5. EventGraph、技能动作与 Pose 的输入保持单向

EventGraph 唯一声明和更新动画实例变量，成功更新后发布 Contract／Layout／typed Frame；Pose 只读。Pose 条件、BlendSpace 与其他消费者引用同一变量身份，不复制可写 Blackboard 或改用变量名称查找。初始化、变量类型、跨图可见性继续遵守已完成只读 Blackboard 合同。

Ability／Timeline 自动产生带实例和 generation 的有限动作播放、采样、停止请求，直接交 ActionPlayback／Slot；EventGraph 不做必经转发或动作仲裁。Pose 使用 committed Body／Intent，Camera 继续消费表现输入；任何影响 Gameplay 命中／移动／目标判定的结果不得改从原生 Pose 或物理骨骼反推。

**取舍：**避免因为“都用 FlowCanvas”而把两个图合成一个更新 owner。保留各自输入职责只需要同一个宿主的顺序和 typed Frame，不需要第二套变量或时钟。

### D6. 资源和绑定按真实领域保存，不再统一生成 Character Projection 总包

动画 Profile、Rig、原生图和明确资源引用是表现输入；实例创建时建立只读运行绑定，供 Player、Slot、Constraint、Camera 使用。静态资源的加载／骨骼索引解析属于正式初始化，不能在每帧遍历资产。已有 ACL、Motion Matching、Foot 数据与相机资源产品保留自己的构建、身份和加载合同。Camera 任务唯一拥有 Profile／资源到只读 Camera 绑定的准备能力及现有求解／资源规则；本任务只调用它并迁出旧总 Projection 中的挂接，不修改 Camera Builder、payload 或 Timeline.Camera.cs。

`CharacterPresentationProjection` 不再作为必须发布的全角色 Pose 程序容器；移除 Image、ProgramId／SemanticHash 和按整角色产物生成的重复配置。其仍被消费的 Rig／源资源／Slot／有限动作／Camera 数据按实际领域迁回正式配置、资源产品或不可序列化实例绑定，禁止复制成另一个统一编译总包。最终旧 Projection wrapper／codec／字段在无消费者后删除。Action 的源和 Slot 对应关系来自技能播放合同与动画配置，必须验证，但不再要求技能与全部动画资源共享同一个内容 Hash。

**取舍：**实例绑定会增加首次加载工作，但消除作者同时维护图与过期 Projection 的负担；资源压缩和数据库构建仍有真实成本，不因取消图编译而取消。

### D7. 网络 Pipeline／Pass 和两个数值目标全部保留

仍显式安装 Gameplay Runtime、Execution Backend、Pipeline、Session Source、WorldSolver 五个维度；原 Program Runtime Definition 只迁为领域运行模块／Numeric Target 服务装配，不新增另一项重复选择。Pass 顺序、Product 读写、能力校验、History participant、恢复／重放、EventId disposition 和唯一 Session／World owner 保留。

Evaluate／Finalize Pass 调用角色运行接口；WorldResolveBatch 接线不变。Source 与 transport 不直接执行角色或技能。网络握手锁定完整角色玩法内容 identity（模块语义版本、必要配置、授予／技能内容），状态 schema identity、NumericProfile、World／Solver 和 Pipeline／Backend identity。这些 identity 只是兼容检查，不承载可执行 Program 或第二份配置。

Server Authority baseline、Prediction History 与 Fixed Rollback snapshot 迁到 D2 的完整分区状态；保留 state/body 分别比较、remote observed actor、确认／重放与有界 EventId journal。网络重放不推进 Pose 图，模型继续通过既有表现输出规则在提交后更新本地表现。

普通 .NET Authority 继续使用 portable C# 模块、技能数据与配置；不得引入 FlowCanvas、Unity object 或 Pose。Unity Authority 与 DotRecast Authority 仍是各自完整 Host 产品。当前 DotRecast 不具备的空中垂直能力不会通过本次简化获得，保持明确拒绝。Relay 仍只路由输入和一致性产品，不执行角色。

**取舍：**网络兼容身份和快照不能随 Program 名字一起删除；保留 Pass 仍承担其配置／编译成本。取消 Fixed 或某网络模型是另一项业务选择，本提案不做。

### D8. 作者工具、观察与发布只跟随各自内容

Skill 校验／构建只跟随技能闭包；Pose 校验和原生实例替换只跟随图及其绑定；资源处理跟随资源；Pipeline 只在组合或策略变化时准备。改变共同接口必须触发对应绑定检查，不能用缓存掩盖不匹配。

Pose 编辑器不再提供生成 ProgramImage 的 Compile 动作；已有轻量检查继续存在，作者显式刷新运行实例。采用明确的实例重建／历史重置语义，不承诺图拓扑修改后的无损热替换。技能活动实例绑定其启动时的不可变技能版本，新版本只作用于后续实例；Session 玩法 identity 改变需重新准备，网络会话中不偷偷更换版本。

正式运行、预览和 Live Debug 通过同一个原生 Pose Factory 与节点运行结果观察；NodeId、PortId、调用实例和求值身份直接定位原图，不为恢复旧 Debug SourceMap 而编译一份隐藏 Image。技能仍保留自己的来源映射，既有 Scene Play 的观察／暂停／推进能力按各自 owner 迁移，不能用 Preview 专用播放器替代。

C# authoring 继续两个显式调用，使用同一领域字段、端口、Mutation 和共享节点定义，保留完整资产身份与子图引用。此次原生运行节点迁移不是新增作者协议。

### D9. Timeline直接调度正式内容，不编译轨道与Clip

这是本轮明确决策，不留给实现选择：删除 Timeline 轨道／Clip 到 Semantic operation 的发射，正式 Timeline Runtime 直接消费只读轨道、区间、片段类型、参数、稳定 ClipId 和资源引用。技能图上的“调用 Timeline”节点仍可作为技能调用操作存在，但它只绑定 Timeline identity／内容版本／入参；不把 Timeline 内部再次展开成操作图。TreeClip 只引用已独立编译的技能执行入口，Decision／Commit、调用实例和取消传播继续由原生命周期管理。

不复活旧 TimelinePlayer，也不改用 Slate Cutscene Runtime。把现有正式 Timeline 调度、边界遍历、窗口和取消算法从读取 Program operations 改为读取正式时间轴内容，形成唯一 Timeline Runtime；技能调用与非 Skill 调用都经这个入口，只在调用上下文上不同。共享内容只读，每次播放拥有独立 cursor、loop／section、活动片段、目标、generation 与结束状态。

Unity 作者资产仍由 TimelineData 的正式 API 拥有；普通 .NET 所需的 portable Timeline 内容由同一字段／类型合同一对一序列化，Unity 对象转换为稳定资源或技能引用。它只保存作者已有内容，不生成 IR、控制流、状态槽或操作码，不建立另一份可写时间轴。导出不调用 Character Frontend，加载不回读 Unity。Float32／Fixed 在正式实例准备中按同一内容合同做数值和资源绑定，两个目标不得重写时间映射或片段逻辑。

唯一 Timeline 时间 owner 负责作者帧／秒／Logic Tick、ClipIn、源区间、播放速率、Section／loop 和前后采样边界。一次 Tick 跨过多个边界时保持原稳定顺序；取消后关闭该实例窗口／TreeClip 和 Gameplay 输出，纯动画尾部由原 Slot／播放 owner 继续处理。Snapshot 保存各次播放和 TreeClip 调用的跨 Tick 状态，重放复用同一 Runtime，不自行推进表现图。

RootMotionCurveAsset 唯一保存累计 XYZ／Yaw、源时长和求值模式；Clip 只拥有引用、源区间、Timeline位置、混合与 Weight／Ease。时间映射由曲线／Timeline任务唯一提供，控制 Motion 消费相同映射的正式绑定；不在 Control emitter、Warp、预览或导出器复制公式。源区间到达末尾时保持累计终值，后续 delta 为零，Clip 权重／占用仍按自身结束边界；同一源被两次使用仍有两个 Clip／Warp 调用身份。技能发布将实际 Timeline／Motion 内容版本列入依赖或携带其只读数据，C#控制从运动资源准备结果取得绑定，均不依赖角色 Program。

**取舍：**直接调度保留一份 Timeline 内容模型，省掉 Clip→operation／SourceMap／布局发射；仍要维护 Timeline 业务运行和 portable 数据序列化。继续编成技能 operation 可复用旧执行器但继续维护双表示；直接读取 Unity Timeline 资产会破坏普通 .NET 和数值目标边界，两者本次都不选择。

### D10. 领域准备、采用与预览接入必须给出具体结果

以下是待实现的正式合同，不是声称当前已有接口。每个领域使用自己的请求／结果类型，共享稳定状态枚举而不共享任意 object 字典或统一可执行包。

| 领域操作 | 输入 | 准备就绪结果 | 实际采用结果 |
| --- | --- | --- | --- |
| Ability Prepare／Install | AbilityId、请求内容版本、NumericTarget、实际 Timeline／Motion 依赖、provider 合同 | 已校验技能数据、对Timeline准备结果的引用、依赖身份、能力要求 | Ability owner确认实际安装技能版本；活动实例固定启动版本 |
| Timeline Prepare／CreatePlayback | TimelineId／内容版本、NumericTarget、Owner／Call身份、资源绑定、按内容需要的TreeClip服务 | Timeline owner提供独立PreparedContent与依赖结果，不要求Ability外壳 | Timeline owner确认PlaybackInstanceId、ContentRevision及StateSchema；独立调用同样返回实际事实 |
| Pose Prepare／ReplaceInstance | GraphId／图版本、Rig与资源绑定、动画输入合同、actor | 已绑定原生图与合法资源、可创建实例的分型结果 | 实际 GraphRevision、InstanceId、ResetGeneration；替换显式重置历史 |
| Camera Prepare／AdoptBinding | 角色需要的 Camera Profile、资源引用、目标绑定与使用角色 | Camera owner 返回只读相机运行绑定及精确资源版本 | 实际 CameraBindingId、ProfileRevision、资源版本，由原 Camera owner 采用 |
| Motion Prepare／Bind | RootMotionCurveAsset稳定引用、源内容版本、Timeline唯一映射、使用身份、NumericTarget | portable 运动数据与本次使用的映射绑定 | 实际 SourceRevision、MappingIdentity、Target 与绑定身份 |

准备结果必须带本次 RequestId、RequestedSource identity／revision、Status、typed Missing／Invalid reason（来源、字段／资源、原因码）以及仅在 Ready 时存在的 PreparedBinding。Pending 表示 owner 的实际未完成准备；Missing／Invalid／Failed 不得读旧总包伪装 Ready。明确不需要相机的 actor 可以由正式角色职责返回 NotRequired，不能把缺失已配置相机当成不需要。

准备完成和采用完成是两种事实。只有对应 owner 实际安装成功后才能发布 AdoptedResult，包含 Actor／Instance、实际采用的内容／资源版本及该 owner 的采用边界；准备失败不改写当前实例身份。预览只发起公开领域操作、读取这些状态和原 runtime observation，不创建工厂、不计算 fake ProgramEpoch、不调用旧 Character Build，也不因资产已保存就显示“已采用”。

玩法内容／状态 schema 改变时按原 Session规则重新准备，网络锁定会话不能热换；本地合法的新技能版本只影响后续实例。Pose明确重建实例并重置历史。Camera 的采用边界与状态由其模块提供，本任务不以重置 Pose 或重建整个角色代替相机采用。Motion 的新绑定只在对应正式实例创建／内容重新准备时使用，不悄悄改变活动技能的轨迹。

本任务拥有角色领域实例工厂和公共装配调用，只汇集各领域owner已经确认的准备／采用事实。Timeline、Pose、Camera、Motion的实际采用结果必须由各自领域owner发布；核心不得根据保存时间、准备Ready或请求版本合成已采用版本。Ability安装由核心中的Ability owner对自己的真实动作负责。Camera Builder／payload／Timeline.Camera.cs 由 Camera 任务实现；ScenePlay协调器由预览任务接入；运动源／Clip／Warp／映射由曲线迁移任务提供；Slate源码由 Timeline UI任务维护。C#输出／生成的字段和接口变化作为明确依赖交给其 owner，不覆盖已正确的输出实现。

### D11. 当前可消费接口与完成边界

以下是2026-09-13第一批接口快照，保留其已交付事实；2026-09-14新增模块与质量审查见D12。该批确认已有：
- `GameplayAbilitySemanticFrontendCompiler.Compile(GameplayAbilityDefinition)`：返回独立 Ability 的语义结果。
- `GameplayAbilityTargetCompiler.CompileFloat32/CompileFixed(ValidatedSemanticIrArtifact)`：提供两个目标入口。
- `GameplayAbilityFloat32TargetProgramArtifactStore` 与 Fixed 对应类的 `Stage/Write/Load`：按 Ability identity 处理产物。

这些仍复用旧 Semantic／CharacterSimulationProgram 容器，不能当作最终领域 runtime 或预览 API。任务1.1—1.3的已勾选记录保留；D9的 Timeline operation 删除、最终技能数据接口、D10各领域准备／采用结果和角色工厂均由后续未完成任务交付。编译命令曾失败及未运行验证的边界仍按实现记录保留，本规划不改写 implementation.md 的事实。

### D12. 2026-09-14实现审查与收口要求

本轮源码快照：HEAD `50e12e191` 附近的共享工作区。审查只读，不重跑编译或Unity。implementation.md报告的编译成功属于实现窗口记录，不转换为本轮运行证明。

| 审查项 | 已有实际链路 | 完成口径与整改 |
| --- | --- | --- |
| Q1 领域工厂 | Float32／Fixed CharacterDomainRuntimeFactory仍以SimulationEvaluateRequest中的Program／ExecutionLayout创建旧Workspace和OperationEvaluator；CharacterPipelineHost仍Load旧整角色Program | 这是实例创建职责集中，不是最终领域装配。2.1重开；必须从正式角色配置、独立技能绑定和领域状态创建并被Host／Pass实际消费，不能只新增Factory名字 |
| Q2 技能状态归属 | GameplayAbilitySemanticFrontendCompiler无条件声明GameplayEffectAggregate、runtime:rng、handle-allocator和fact-sequence | 将原角色级状态留在每份Ability中会延续完整运行单元模型。1.8接管移出，领域服务唯一拥有；明确的技能局部状态仍保留，不强迫不同技能共享其私有计时或变量 |
| Q3 Provider实际合同 | RequireBinding只比较Kind与ProviderIdentity；BindingEntry不包含真实成员／类型／版本或运行服务句柄 | 1.4只算身份声明入口已完成。1.9必须解析实际提供者合同，不能靠同一资产GUID证明依赖仍存在，不能从旧产物反算“实际”合同 |
| Q4 最终技能数据接口 | GameplayAbilityDataAsset.Load及Fixed对应入口仍返回CharacterSimulationProgram | 1.10完成最终返回／消费类型和布局分离。已有校验、codec／store逻辑可复用，不能只换Asset名称 |
| Q5 后续主链 | Timeline仍走旧发射／Program plan，Pose仍禁止原生运行，网络仍比较Program／Layout身份 | 保持原未完成任务，不把模块拆分类提交等同于这些行为已经改变 |

Q1的完成条件是实际依赖被切断：角色工厂输入不再要求整角色Program，输出不再只是旧Evaluator与Workspace，正式Host使用已绑定技能集合和领域配置创建实例，Pass继续原Evaluate／WorldResolve／Finalize顺序。D10的准备与采用结果仍需由真实owner发布。

Q2不是当前运行已出现重复效果或序号冲突的结论；新技能资源尚未整体接进角色启动。本项指出的是接入前必须清除的状态归属冲突。若业务确有技能私有随机流，必须使用明确的技能／实例作用域与恢复规则，不能复用旧角色级runtime:rng来冒充私有设计。

Q3要求的是被实际引用的成员合同，不是整资产每个无关字段都触发失败。提供者必须从当前已安装模块／正式配置发布成员identity、值类型与合同版本，绑定按技能需求解析typed句柄。同GUID删除MoveAxis、改变某属性类型、或提供者版本不兼容时，准备应返回精确依赖失败；仅更改未消费的作者显示信息不构成合同失配。

已有有效小步继续保留：独立Ability前端、两个Target／store、生命周期模块拆分、Control Tick模块拆分、Pass的CharacterRuntime调用口，以及Control静态Motion直接读取C# Contract。此次重开完成标记不回退代码、不撤销这些成果，也不以改名或保留旧兼容入口代替剩余收口。

### D13. 已有领域任务的规划分工与唯一责任清单

依据：协调文档“2026-09-14：PARALLEL-20260914-DOMAIN-01 审阅”（2801861c1）及用户广播 `parallel-20260914-domain-01-planning-update`。本轮已授权更新规划归属，未授权本窗口启动／重派实现；不把文档更新等同于运行交接完成。复用已有配对，不创建重复任务。

| 领域与已有配对 | 唯一实施清单 | 本主方案保留的集成 |
| --- | --- | --- |
| 核心：规划01a09a5f-7316-79f2-9058-1d2cce37e801／实现01a09a5f-8d64-7c11-a461-7889623b7459 | 本change的tasks.md | 角色Host／Factory、C#控制、Ability最终数据／provider、角色Step／快照／codec、网络／manifest、共享技能编译与总Program／Projection删除 |
| Timeline：规划01a095ac-88a4-7bd3-abbf-197b9058c1ca／实现01a089db-81e3-7a73-ae52-82ef95b744d4 | `../restyle-timeline-editor-slate-style/tasks.md` 的Runtime接收章节，由其规划续写 | 核心消费独立Prepare／CreatePlayback、TreeClip服务与分型状态；不再维护第二份Timeline内部实施清单 |
| Pose：规划01a09594-1751-7512-b8c0-08b04185055b／实现01a081f3-46f4-7c91-8930-73923ff7950b | `../refine-pose-graph-readonly-blackboard/tasks.md` 的Runtime接收章节，由其规划续写 | 核心调用原生图准备／两阶段求值／提交丢弃接口，接角色表现外壳与总包退出；已完成只读输入不重开 |
| Camera／运动源／预览／C#／Foot | 各自已有owner文档 | 核心只接真实接口。曲线源已归档的迁移不重做，读取现行规范与API；Foot／IK按当前已批准任务提供的正式接口接入，不冻结或回退其后续正确算法 |

Timeline接收直接内容Runtime、portable轨道／Clip、播放／取消／TreeClip调用、私有状态与专属发射器退出。Pose接收原生Graph／Node／Port／Connection、准备与求值、实例／缓冲生命周期、节点观察及旧Image专属链退出。主tasks只留下两者的交接指针和核心集成项；原编号的迁出去向记录在spec-audit，不能把删除checkbox当作实现完成。

主方案既有spec增量继续表达总体合同和外部约束，不形成核心实现对领域文件的写入授权。领域内部细化和任务清单只由接收规划维护；涉及共同规范时按真实owner合并，不互改对方任务文件，不增加重复实现清单。

#### 唯一文件写入者

- 核心：CharacterPipelineHost、Float32／Fixed CharacterDomainRuntimeInstance／Factory、角色State／Step／codec、网络checkpoint／manifest、共享Program／artifact、Character Build编排。
- **BtsmtlSkillTimelineCompiler及共享技能编译／调用入口只由核心写入**。Timeline提供直接内容和调用合同；TreeClip技能图编译与执行服务由核心保留，不能双方各改半个方法。
- Timeline：Timeline专属内容／播放、Float32TimelinePlayback／FixedTimelinePlayback、TimelineControlRuntime与专属发射适配。涉及通用Evaluator、Workspace或Program codec的改动交核心一次集成。
- Pose：CharacterPoseCanvasGraph／Node／Connection／NativePorts、Pose内部运行及Image专属Compiler／Worker清理。不能按目录整删仍被Source／Constraint／Foot算法消费的代码。
- CharacterPresentationRuntime、CharacterSimulationPresentationRuntime、角色表现工厂和总Projection挂接由核心唯一写入；Pose／Camera／EventGraph提供接口，不同时改外壳。
- Camera Builder／payload／Prepare／Adopt及Timeline.Camera.cs归Camera；RootMotionCurveAsset、Timeline.MotionCurve.cs／MotionWarp.cs及时间映射与正式配置归运动源原owner；ScenePlay协调器归预览；Slate源码归Timeline UI；C#输出／生成适配归C#任务。

现行实现层临时阻塞协调入口仍是原主实现。编译／Unity刷新沿既有正式入口统一组织，编译期间暂停源码写入，不新增锁服务或验证流程。本轮不发送实现指令、日常状态或回执。

### D14. Timeline独立播放与角色Step的公共合同

本节只固定跨领域输入输出；内部调度、portable内容、状态实现和详细任务归Timeline。无技能外壳的调用不能伪造Ability来准备Timeline。

| Timeline公开操作 | 输入 | owner返回的结果与边界 |
| --- | --- | --- |
| PrepareContent | TimelineId／ContentRevision、NumericTarget、正式资源与唯一源时间映射、调用方能力合同、内容实际需要的TreeClip服务 | Ready／Pending／Missing／Invalid／Failed及精确来源原因；Ready时给不可变PreparedContent。没有TreeClip的内容不强迫提供技能服务 |
| CreatePlayback | PreparedContent、精确OwnerId／CallId／InstanceId、必要目标与输入绑定 | 独立播放实例和由Timeline确认的实际内容／资源版本与私有StateSchema；不要求Character Program |
| Advance | 当前已提交播放状态、调用方提供的Step身份／时间区间／输入和Step-scoped TreeClip执行服务 | 分型PendingPlaybackState、待提交窗口／运动／动作等结果及本次完成状态。不得内部CommitFrame后提前发Gameplay、副作用或改committed cursor |
| ValidatePending／Commit | 对应同一次Advance的候选和调用方正式提交决定 | Timeline先完成自身提交条件检查；在调用方统一提交阶段只安装已验证状态并确认本次结果，不再次运行Clip。外部输出由调用方在整个Step成功后发布 |
| Discard | 同一次Step的未提交候选与失败原因 | 丢弃Timeline Pending和本次新资源，保持此前已提交私有状态；不得删除或重建其它领域状态 |
| RequestStop／Cancel | 精确播放实例、正式终止原因与调用方Step上下文 | 沿同一暂存／提交边界关闭窗口和TreeClip；纯表现尾部继续交原Slot，不在停止请求里提前发布外部结果 |
| Capture／PrepareRestore／ApplyRestore | 已提交实例或携带内容／schema／调用身份的typed Timeline状态 | Timeline唯一拥有cursor、loop／section、活动Clip和调用私有状态；核心只聚合及原子安装，不复制字段语义或直接读写私有状态 |

角色核心拥有整Step的决策、提交／丢弃和Character／World／Pipeline快照；Timeline只拥有自己的分型状态。先取得并验证所有必要领域和World结果，再进行已验证的提交与外部发布；任何前置失败必须丢弃同Step所有Pending。不能出现Timeline先提交、核心随后失败却留下已消费窗口或cursor的情况。

TreeClip通过核心提供的Step-scoped技能调用服务产生同一Step的暂存玩法结果，不绕过核心事务，也不让Timeline创建第二技能执行器。技能调用与非Skill调用使用相同Timeline Runtime和上述生命周期；独立调用方提供自己的正式提交边界和所需服务，不另起播放器或时钟。

### D15. Pose领域与角色表现外壳的公共合同

Pose owner提供PrepareBinding／CreateInstance、PrepareDemand、Evaluate、ValidatePending、Commit／Discard、Stop／Dispose和已完成结果观察。准备输入为原生图／Rig／资源、actor与子图调用身份、同次EventGraph typed输入及有限动作请求；返回真实Ready／失败原因及实例创建结果，不返回Image或全图操作计划。

核心保留表现外壳的调用顺序和统一帧边界：调用Pose准备活跃状态与source demand，沿正式Source完成资源／采样准备及唯一Animancer Barrier，再调用原生姿态／Constraint求值，确认完整结果后由既有Final Publication唯一写骨骼并提交各owner状态。Pose内部只通过自己拥有的节点／状态／缓冲履行两阶段求值，不反向操作角色Host或网络快照。

Barrier前失败按原合同丢弃Pending；Barrier内或之后失败按原Fault边界阻止半帧发布，不能承诺撤销已经发生的物理采样。Stop／替换／Dispose由核心请求、领域owner执行其私有生命周期；实际InstanceId／GraphRevision／ResetGeneration和采用结果由Pose确认，核心只汇集。网络重放不推进Pose，也不保存Pose图对象。

### D16. 以实际入口切换和旧链删除完成迁移

本轮审查代码快照为32f75a37c，实施记录更新至401b52e15。角色状态分区、Provider成员检查、独立Ability数据类型和两个数值目标的执行目录已有进展；当前仍是中间接线，不能描述成角色已脱离Program或编译体系已删除。

#### 当前接线缺陷与完成边界

Float32／Fixed OperationEvaluator仍调用GameplayAbilityExecutionCatalogFactory.FromProgram。该目录把原program.Manifest.Root传给独立Ability数据构造器，后者只接受IsAbility；角色Frontend和Corin现有产物仍为Character根。含技能的Character Program沿此路径会抛异常。这是静态调用链证据，未运行Unity复现。解决归1.10与2.1：正式Host／Factory直接装配独立技能集合，删除FromProgram转换；不能取消根校验、把Character根改标Ability或增加兼容分支。

Ability Load返回独立类型只完成了公开返回边界。Loader内部仍调用旧Program codec，转换Factory复制旧操作、常量、布局等集合，执行目录也仍从旧Program创建。最终必须由技能唯一格式直接加载所需只读数据并供执行器消费；同一技能在多个角色间复用，不逐技能复制全角色内容。Provider现有成员／类型／版本检查应保留，但非空RuntimeHandle字符串还需落实到实际执行绑定，不能仅凭校验函数判定1.9完成。

Pose原生实例、阶段门禁、端口缓存和校验不是完整动画运行。Player／StateMachine／Slot／Blend／惯性化／Constraint算法以及最终输出必须接入；空EvaluateFrame／CommitFrame和其它节点UnsupportedNode只能记录为尚未实现。Timeline仍使用ProgramPlan及轨道／Clip发射链，直接内容运行的目标未改变。网络baseline／恢复仍依赖整角色ProgramHash／LayoutHash，Pass保留不等于状态接口迁移完成。

#### 删除范围与保留职责

| 领域 | 随消费者切换删除 | 保留与收窄 | 实现owner |
| --- | --- | --- | --- |
| 整角色 | Character Frontend／总Builder／BuildService、Program总包、角色全局布局、专属codec／artifact／失效缓存／Build UI及旧转换工厂 | 角色配置装配、领域状态、技能独立编译和格式；共享实现只保留实际技能职责并正确命名 | 核心及原工具owner |
| Timeline | 轨道／Clip Semantic发射、Timeline IR／ProgramPlan、操作码播放适配及专属产物 | 正式轨道内容、portable导出、数值／资源准备、播放取消与恢复；TreeClip继续调用核心技能服务 | Timeline；共享技能入口归核心 |
| Pose | Pose IR／Image、全图Lowering／Schedule／ValueLifetime／Workspace编译、Image专属执行器／发布／缓存 | FlowCanvas原生运行、实例与阶段缓存、原有动画／混合／IK算法、Source／Constraint／Final Publication和必要数值缓冲 | Pose；公共表现外壳归核心 |
| 网络 | 对整角色Program描述、Hash／Layout和旧reader的依赖 | Pipeline编译产生不可变Pass计划、顺序／产品／能力校验、Backend／Source／Solver及网络协议与恢复算法 | 核心 |
| 资源 | 对整角色Build和Projection总包的无关依赖 | ACL、Motion Matching、Foot等实际资源处理及领域绑定 | 各资源owner |

上述表按职责删除，不按Compiler／Program文件名或目录整删。尤其Pose Program目录含实际动画算法，必须迁移算法消费者后删除其旧载体。直接内容导出、资源准备和Pipeline计划校验不属于撤销的全角色／Timeline／Pose可执行图编译。

#### 小步收口规则与业务取舍

每个迁移事项同时交付正式调用路径与该owner可退出的旧路径删除；公共依赖尚未迁移时，明确记录阻挡删除的消费者和owner，该领域事项仍保持未完成。允许小步提交未接完整的代码，不允许提供可选旧运行模式、永久桥接或双reader。8.2／8.4负责剩余公共依赖收口，不表示把所有旧链清理推迟到最后；既有已完成状态分区小步保留，不重新打开。

作者收益是修改技能只处理技能依赖、修改Timeline直接更新内容、修改Pose无需维护另一份Image；代价是同时完成原生节点算法接入、完整恢复和工具消费者迁移。代码净减少应来自重复表示、编译阶段和旧执行器消失，不通过删算法／网络能力或把同样职责改名复制实现。移除全图Worker调度可能改变性能，不能承诺代码减少必然提速。

## Risks / Trade-offs

- [原生端口重复取值] → D3 的调用实例／阶段缓存；同一输入共享两条支路时源时间与求解只推进一次。
- [替换图导致历史或资源悬空] → D4 的停止、完成、释放顺序；显式重建实例并重置历史，不做隐式历史拼接。
- [丢失 Worker／缓冲规划优化] → 保留既有算子内部 Native 算法；接受全图调度策略变化，记录真实耗时／内存边界而非承诺提速。
- [C# 控制模块恢复不完整] → 状态接口覆盖模块内部机器与所有跨 Tick 字段，Snapshot／Hash 只使用同次提交结果。
- [只改 Unity，普通 .NET Host 失去数据] → portable 技能／模块配置和 server manifest 同步迁移；旧部署产物精确拒绝。
- [Pose 改动被误编进技能或网络身份] → 技能、控制资源、表现资源分别计算内容范围；只有实际共同合同变化要求重新绑定。
- [并行任务的正确行为被覆盖] → 下表明确本 change 只接管旧编译／运行边界；算法和作者业务变化仍归原任务，实际冲突报告后由用户裁决。

## Migration Plan

### 完整迁移顺序

1. 固定当前实际作者合同和模块接口，建立技能独立数据、领域状态与内容身份；不修改运动和动画算法。
2. 迁移 Control／Motion／Effect／Equipment 与 Ability 运行接线，两个 Target 和普通 .NET 模块同时接入。
3. 迁移 Network Pass、roster、baseline、snapshot、握手和产品 manifest，保留现有模拟顺序与行为。
4. Timeline与Pose在各自唯一任务清单推进内部Runtime；核心只集成D14／D15的调用、提交／丢弃、分型状态与角色表现外壳，不代写领域实现。
5. 原作者／预览／C#任务消费正式接口；核心迁移角色配置、Host、场景／Variant与公共诊断接线，领域资产由各自owner的正式API处理。
6. 核心收口角色Program和总Projection的剩余共享依赖；Timeline、Pose在各自消费者切换的小步内删除专属发射链和Image链，不统一拖到本步。按共享规范真实owner同步旧要求，不重复实施。

步骤表达依赖顺序，不判定业务优先级。每一步按 owner 范围小步中文提交；模块未接完整可以明确报错，不增加可选择的旧运行模式。旧资产／协议只做一次正式迁移或显式重生成，升级唯一版本后旧 reader 删除。

恢复采用成套代码与对应正式资产回到已确认版本，必须保持网络内容／schema 和角色资源匹配；不在运行代码中保留新旧双 reader。共享工作区不做破坏性回退，不撤销其它任务文件。规划阶段不运行资源生成、Build、Unity 或迁移。

### 现行规范对账

本 change 的 delta 分为两类：完整 MODIFIED 保留原场景；架构载体被撤销的条款用 REMOVED＋新要求明确替换，不能用局部 MODIFIED 丢失未变场景。`spec-audit.md` 逐项记录基线与替换去向。

| 现行规范／文档 | 与本设计的冲突 | 处理 |
| --- | --- | --- |
| `btsmtl-gameplay-semantic-ir`、`btsmtl-compiled-simulation-program` | Definition 唯一根、全部角色状态／目录编进 Program、整体 Program／Projection 发布 | delta 限定技能与既有独立 Timeline 内容；角色装配和状态由新领域规范接管 |
| `character-pose-plan-compilation` | 强制固定 IR／Image Pass 链 | 撤销编译载体条款，保留节点／空间／递归／冲突校验为原生图约束 |
| `character-pose-graph-runtime-architecture` | Image／Execution View／Operation owner 为唯一执行路径 | 原生图实例接管图状态和执行，保留 Source／Constraint／Final Publication 与事务行为 |
| `character-presentation-pose-graph`、动画相关规范 | 固定 Pose Plan、Program producer 总身份和编译绑定 | 原生图与实际动作／资源绑定替代；状态、同步、混合、IK 与最终输出不变 |
| Session／Pipeline／Authority／Rollback | ProgramCatalog、ProgramHash／LayoutHash 为角色必要合同 | 迁为领域模块绑定、玩法内容／schema；Pass 及网络算法保持 |
| `graph-authoring-domain-framework`、Definition／Locomotion 规范 | 所有作者图禁止原生运行；仍有 BTSMTL Locomotion 文案 | Skill 作者图继续编译；Pose／EventGraph 原生运行；角色控制只由 C# 拥有 |
| `openspec/project.md` | 明确禁止 Pose 原生 Runtime，要求整个 Program／Projection 链 | 实施同步时修改 Architecture／Current State／Pending Work 的现行方向，不伪造当前已完成事实 |

### 并行工作与冲突边界

| 活跃 change | 保留的职责 | 本 change 接管或替代 |
| --- | --- | --- |
| `integrate-pose-flowcanvas-editor-preview` | 作者图分层、原生资源选择、Slot／Layer／Control Rig、正式 Mutation 和 UI | 其“仍编译 Image／Native Program、禁止原生 Pose”的执行假设被替代；不重做已正确作者组织 |
| `refactor-character-pose-graph-architecture` | 已正确的算法、Source／Constraint／Final Publication、状态与事务所有权 | ProgramImage／操作索引／全图 Worker 专属工作被原生节点迁移替代；不能把已完成算法回退 |
| `refine-pose-graph-readonly-blackboard`、`add-flowcanvas-event-graph` | 唯一变量声明／Set／typed Frame 和 Pose 只读消费 | 只把 Pose 编译句柄绑定改为原生节点绑定，不接管变量写入业务 |
| `integrate-native-fsm-skill-authoring`、`add-skill-transfer-connections` | GameplayAbilityDefinition、原生 FSM 作者组织、生命周期、条件／连线语义 | 技能输出从角色总 Program 迁为独立技能数据；不复活旧 ActionExit 或其它已清理作者结构 |
| `rebuild-btsmtl-preview-with-scene-play`、`finish-skill-runtime-observation` | 正式会话观察、暂停／推进、身份与来源、用户交互 | 由本任务提供真实领域操作与采用结果，预览任务自己接 ScenePlay协调器；不保留 Character Build／ProgramEpoch，也不构造假全局版本 |
| `rebuild-character-camera-from-zzz` | Camera Builder／payload／Timeline.Camera.cs、Profile／资源准备与求解 | 本任务仅调用其分型准备接口、迁移角色装配与旧 Projection 挂接 |
| 现行运动源规范／C# authoring原owner | 已完成RootMotionCurveAsset、MotionCurveClip／MotionWarp与时间映射迁移；历史change已归档 | 本任务消费当前正式绑定，不重做已完成迁移；共享字段修改仍交原owner |
| `restyle-timeline-editor-slate-style` | Slate 源码、Timeline内容 UI／编辑／保存 | 本任务不接管 Slate 编辑代码；播放仍由正式 Timeline Runtime 拥有 |
| ACL／Foot 相关任务 | 资源构建、算法与已有行为改动 | 只迁移被取消总包的绑定，不扩大到未完成算法或资产 |

本轮不写上述 change 的 tasks／execution，不发日常窗口消息。已有代码如与本设计所需合同直接冲突，先记录具体文件、双方 owner 和业务取舍，再按用户约定提出一次实际冲突；不能把旧规范文字本身当成要求用户重复批准本次规划。

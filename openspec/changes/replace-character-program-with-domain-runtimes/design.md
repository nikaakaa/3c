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

`CharacterPresentationProjection` 不再作为必须发布的全角色 Pose 程序容器；移除 Image、ProgramId／SemanticHash 和按整角色产物生成的重复配置。其仍被消费的 Rig／源资源／Slot／有限动作／Camera 数据按实际领域迁回正式配置、资源产品或不可序列化实例绑定，禁止复制成另一个统一编译总包。按D17先删除旧Projection wrapper／codec及专属字段，再处理保留消费者的领域接线；混合数据只提取有效资源职责，不整删算法和资源。Action 的源和 Slot 对应关系来自技能播放合同与动画配置，必须验证，但不再要求技能与全部动画资源共享同一个内容 Hash。

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

本节保留32f75a37c／401b52e15时的历史审查。后续c101cdb54已撤回错误目录接线，a31c27b79及其它删除提交已退役部分旧链；当前进展以D18和tasks顶部更新为准，不再把本节的旧代码状态当作仍在发生的错误。下面删除范围和最终业务约束继续有效。

#### 历史接线缺陷与最终完成边界

Float32／Fixed OperationEvaluator仍调用GameplayAbilityExecutionCatalogFactory.FromProgram。该目录把原program.Manifest.Root传给独立Ability数据构造器，后者只接受IsAbility；角色Frontend和Corin现有产物仍为Character根。含技能的Character Program沿此路径会抛异常。这是静态调用链证据，未运行Unity复现。解决归1.10与2.1：正式Host／Factory直接装配独立技能集合，删除FromProgram转换；不能取消根校验、把Character根改标Ability或增加兼容分支。

Ability Load返回独立类型只完成了公开返回边界。Loader内部仍调用旧Program codec，转换Factory复制旧操作、常量、布局等集合，执行目录也仍从旧Program创建。最终必须由技能唯一格式直接加载所需只读数据并供执行器消费；同一技能在多个角色间复用，不逐技能复制全角色内容。Provider现有成员／类型／版本检查应保留，但非空RuntimeHandle字符串还需落实到实际执行绑定，不能仅凭校验函数判定1.9完成。

Pose原生实例、阶段门禁、端口缓存和校验不是完整动画运行。Player／StateMachine／Slot／Blend／惯性化／Constraint算法以及最终输出必须接入；空EvaluateFrame／CommitFrame和其它节点UnsupportedNode只能记录为尚未实现。Timeline仍使用ProgramPlan及轨道／Clip发射链，直接内容运行的目标未改变。网络baseline／恢复仍依赖整角色ProgramHash／LayoutHash，Pass保留不等于状态接口迁移完成。

#### 删除范围与保留职责

| 领域 | 已取消职责，按D17先删除 | 保留与收窄 | 实现owner |
| --- | --- | --- | --- |
| 整角色 | Character Frontend／总Builder／BuildService、Program总包、角色全局布局、专属codec／artifact／失效缓存／Build UI及旧转换工厂 | 角色配置装配、领域状态、技能独立编译和格式；共享实现只保留实际技能职责并正确命名 | 核心及原工具owner |
| Timeline | 轨道／Clip Semantic发射、Timeline IR／ProgramPlan、操作码播放适配及专属产物 | 正式轨道内容、portable导出、数值／资源准备、播放取消与恢复；TreeClip继续调用核心技能服务 | Timeline；共享技能入口归核心 |
| Pose | Pose IR／Image、全图Lowering／Schedule／ValueLifetime／Workspace编译、Image专属执行器／发布／缓存 | FlowCanvas原生运行、实例与阶段缓存、原有动画／混合／IK算法、Source／Constraint／Final Publication和必要数值缓冲 | Pose；公共表现外壳归核心 |
| 网络 | 对整角色Program描述、Hash／Layout和旧reader的依赖 | Pipeline编译产生不可变Pass计划、顺序／产品／能力校验、Backend／Source／Solver及网络协议与恢复算法 | 核心 |
| 资源 | 对整角色Build和Projection总包的无关依赖 | ACL、Motion Matching、Foot等实际资源处理及领域绑定 | 各资源owner |

上述表按职责删除，不按Compiler／Program文件名或目录整删。尤其Pose Program目录含实际动画算法，删除混合文件时只做必要算法提取或解除旧载体类型依赖，不要求消费者全部迁完才删旧载体。直接内容导出、资源准备和Pipeline计划校验不属于撤销的全角色／Timeline／Pose可执行图编译。

#### 小步收口规则与业务取舍

最终迁移仍要求旧链删除和保留业务接通，但执行顺序由D17改为删除优先。原8.2／8.4已前置为0.1／0.2，不再只承担末尾公共残留收口；旧消费者仍存在时记录其待修接线，不以此阻挡取消职责的删除。既有已完成状态分区小步保留，不重新打开。

作者收益是修改技能只处理技能依赖、修改Timeline直接更新内容、修改Pose无需维护另一份Image；代价是同时完成原生节点算法接入、完整恢复和工具消费者迁移。代码净减少应来自重复表示、编译阶段和旧执行器消失，不通过删算法／网络能力或把同样职责改名复制实现。移除全图Worker调度可能改变性能，不能承诺代码减少必然提速。

### D17. 先大删除已取消职责，再接通保留业务

用户2026-09-14明确要求“先大删除再做”。本决策替代此前“等消费者全部切换再删”及“保持旧体系可编译后逐层迁移”的顺序；D16的最终业务和删除范围不变。本轮更新规划，不在规划窗口删除代码。

1. 各owner先成批删除已取消的职责：核心整角色Program／Projection及旧转换链；Timeline轨道／Clip发射、IR／ProgramPlan和操作码执行适配；Pose IR／Image、全图编译调度和专属执行器／缓存／发布。纯粹服务于这些旧载体的消费者一并删除，不为每个旧层建立同名的新版本。
2. 混合文件中的有效技能编译、动画采样／混合／状态机／IK、Source／Constraint／Final Publication、播放取消与恢复算法保留；仅做使代码不丢失的必要提取与类型解耦，后续再接正式Runtime。保留网络Pipeline／Pass／Backend／Source／Solver、协议与状态恢复算法，资源资产和已正确作者数据不因旧类型引用一起误删。
3. 用删除后的引用、类型和调用错误定位剩余消费者：已取消业务的消费者直接删除；仍需保留的消费者接到独立技能、Timeline直接内容、Pose原生图、领域状态和资源接口。错误属于其它owner时记录精确文件与所需接口，由原owner处理；不越界改写其正确工作。
4. 允许删除批次暂时无法编译或角色／预览不可运行，明确记录受影响范围与后续条目。这是已获授权的迁移状态，不要求每个删除提交先全项目编译通过，不为消错加空类、默认结果、兼容reader、Program到新数据转换或旧运行开关。不得通过改工程索引排除保留业务来伪造编译通过。
5. 删除进展与业务恢复分别记录。0.1／0.2完成只表示对应旧职责确实退出；1.x／2.x／3.x／4.7等仍待实际接通，不能宣称整体完成。先删除不意味着删完停工，实施继续按保留职责补齐，且仍遵守Unity编译／Play期间的写入和构建约束。

业务取舍：用户接受迁移期间暂时失去可运行状态，换取减少废弃体系内的搬运和重复转换。保留范围是实际玩法、动画、网络和资源能力，不是旧类结构。这里的“大删除”表示按取消职责成批移除，仍由D13既有owner小步中文提交，不做全仓目录清空或覆盖并行改动。

### D18. 删除后按真实业务接线更新执行状态

本节保留2026-09-14检查至dd38dde00时的接线状态；之后Timeline Advance、Pose多类节点／Final与领域状态codec已有新增，最新状态和职责纠正见D19。本次不撤回D17删除优先，不重新勾选已完成历史，也不把暂时编译失败本身判为错误。

| 领域 | 已有代码事实 | 本轮明确的接续结果 |
| --- | --- | --- |
| 核心技能／角色 | 错误Program→Ability目录接线已撤回；Ability数据自持拓扑，独立GameplayAbilityExecutionLayout及两个Target工厂已建立；Host仍要求旧Program／Projection | 直接加载独立技能、建立实例状态、进入角色Tick／取消／恢复，并让Host从领域绑定启动；新增布局不是执行完成 |
| Timeline | 664aea92c／da1f8634e接通正式内容／binding准备和带generation的准备实例；当前Playback主要持有准备结果 | 先补全数值目标、资源／成员、TreeClip实际服务检查，再落实直接Advance、活动Clip、取消、Commit／Discard与私有恢复；Ready／Created不能冒充Running／Committed |
| Pose | 原生Clip／BlendSpace／Selected Player handler调用现有Source与AnimationSelectedPosePlayerJob，子图及阶段接口已有代码 | 保留这些成果，接通StateMachine／Slot／BlendStack／Inertialization、Constraint及Final Publication；核心接入Host和唯一Barrier，不能继续按“只有参数外壳”报告 |
| 网络 | 旧PassBackendCompositionRequest／SessionCompositionPreparation等Program装配载体已删；Pipeline编译器、Pass／Backend及回滚相关实现仍存在 | 保留原顺序、Product／能力校验、原子提交、预测／恢复算法，替换领域输入和状态接口；不以旧类被删推断网络全部误删，也不能把装配缺口扩展为另一套网络系统 |

Timeline完整Ready必须来自当前内容实际需要的全部准备合同；绑定声明检查通过只是其中一步。未完成的数值／资源／技能服务解析须保持准确状态，不通过空服务或默认值掩盖。一个只处于Prepared且可Dispose的对象不能表示时间、窗口和副作用已经可推进。

Pose后续每类节点说明真实输入、复用的已有算法、输出及其下一消费者。Source采样和Player Job接入是实质进展，但必须沿活跃分支、同一帧身份和唯一Barrier连接到Constraint／Final。接口完善只有在解决这条链的具体缺口时才算对应事项的进展，不新增通用注册、缓存或状态层来代替业务接入。

核心Host、会话装配、Pass与网络状态由核心维护；Pose／Timeline的内部实现仍使用唯一领域清单。记录分别标明已删除、已有代码、已接正式消费者及运行证据；旧缺陷已经撤回则标历史，不要求实现窗口再次修同一问题。本轮未运行编译或Unity，不宣称端到端可用。

### D19. 角色状态、技能服务与直接播放的职责收口

检查至dd0708d46及当前工作区。代码已有独立技能安装／服务、CharacterRuntime port、Float32／Fixed状态codec、Timeline游标候选和多类原生Pose节点；下面明确尚未正确落实的领域边界。只按实际职责整改，不因Program命名再重做合理的技能执行数据、拓扑或布局。

**角色与技能状态。** 当前两个CharacterRuntimeState及事务以单个GameplayAbilityExecutionInstallation创建，角色GameplayContentHash直接取其ContentHash，codec也依赖这一个安装来解析多个角色分区。最终角色必须聚合控制、请求、效果、装备、运动及多个技能的同次已提交状态；技能局部值和调用帧按技能／调用实例隔离，角色身份使用实际角色绑定、全部授予内容与模块合同，不能挑一个技能代表角色或复制多份全角色aggregate。沿现有角色绑定、安装集合和分区codec收口，不恢复总Program或另一套统一操作表。核心2.6／2.7负责。

**角色服务与技能需求。** 当前Float32 InstallationSet给每个技能传同一GameplayEffect binding，但Installation对未声明Effect能力且收到非空binding的技能抛异常。角色具有某服务与技能声明使用某服务是两个事实。在角色调用场景中，共享状态由角色对应领域唯一持有，技能安装只解析自身需要的能力和成员；一般技能执行的服务由领域owner通过实际调用方提供，不限定必须存在角色，见D20。角色可同时安装使用效果的攻击与不使用效果的其它技能；真正缺少所需成员／版本时仍失败。不能给所有技能强开效果能力或创建假服务。核心1.9及两个数值目标的安装／执行消费负责，保留既有Provider成员校验。

**Timeline的完整区间调度。** 当前Prepare已显式处理数值目标和资源解析，Playback已有Start、Advance候选、Commit／Discard与停止状态；Advance目前主要按nextFrame选活动Clip。完整调度必须遍历上次提交位置到候选位置之间的全部边界，包括尾段／完整循环／头段；例如10帧推进到30帧必须处理20—21帧Clip的进入、相应阶段和退出，不能因最终落点不在Clip内而漏掉。Stop必须沿同一候选／接受边界关闭窗口与TreeClip，不能提前改committed状态。Timeline 12.3—12.6负责内部规则和codec，核心只通过1.5／3.8消费实际领域结果，不代写轨道调度。

**Pose服务与装配。** Clip／BlendSpace／Selected Player、Blend／Layered／Additive等已有算法代码，Constraint适配和CharacterFinalPoseNativePublication已有真实服务入口；不能继续报告它们全未实现。但当前ICharacterPoseNativeStateMachineSource只发现接口／引用，未发现具体实现，handler类存在不等于状态转换行为已经提供。Pose规划按每类节点的具体服务、构造注入与下游消费者判断进展；Source／Constraint／Final接口由Pose接续，核心4.7把它们接入Host、唯一Barrier和最终发布。既有Pose审查R1—R7已由该owner跟进，本轮不重复派发旧问题或覆盖其修正。

网络继续保留Pipeline计划、Pass／Backend、能力校验和恢复算法；新的PassBackendCompositionRequest已以CharacterRuntime及领域初始状态为输入，同名类不等于旧Program职责复活。修正的是角色快照／服务身份和正式消费者。Host仍加载旧Program／Projection是公共剩余接线，不能靠准备实例、handler注册或新codec存在宣称完成。

### D20. Skill从编译到执行均独立于角色装配

本节补正原规划遗漏，不是新增产品方向。此前只明确Ability构建根、独立数据和角色状态迁出，没有明确禁止技能执行入口依赖整个角色；这不足以保证Skill独立。依赖方向必须是角色或其它正式调用方使用技能，技能只依赖自己的执行合同和实际声明的领域能力。

- 定义／编译／加载只要求技能定义、可达技能图、数值目标、明确引用与服务需求，不要求CharacterPipelineDefinition、角色Host、Pose或完整角色配置。需要特定角色事实的技能节点可声明typed事实服务，但不能使所有技能都要求ControlModule／角色Body。
- 执行入口接收技能执行数据、技能局部状态、实际调用身份／输入／目标／时间及已解析所需服务；不强制接收CharacterRuntimeState、整套CharacterSimulationInput／WorldBodyState或完整角色上下文。身份与事实按节点实际需求提供，不构造假Actor让无角色需求的技能运行。
- 技能拥有局部图执行、调用帧、局部变量、完成／取消状态；只通过接口产生领域请求或候选结果，不创建CharacterRuntimeStateTransaction、不组装Control／Effect／Equipment运行模块，不自行推进或提交角色／World。
- 服务及业务状态由原领域拥有，实际调用方提供并决定自己的外层提交／丢弃／恢复。角色调用时把技能局部状态和领域候选纳入角色Step；TreeClip等调用方复用同一技能执行入口。不能新增一个包含全部角色数据的万能Context冒充独立，也不新建通用宿主框架或第二技能执行器。
- 编译能力声明必须来自实际可达节点。GameplayAbilitySemanticFrontendCompiler无条件RequireGameplayCapability("GameplayEffect")必须退出；不使用效果的技能不要求效果服务，使用效果而缺少服务的技能仍明确失败。Action等其它无条件声明按同样规则区分技能内部必需机制与可选领域服务，不能仅为保留旧执行器强开能力。

当前源码证据：Float32AbilityExecutionFrame构造器接收CharacterSimulationInput／WorldBodyState／Float32CharacterRuntimeState，并创建Float32CharacterRuntimeStateTransaction；Float32AbilityControlRuntime接收Control／Equipment绑定并组装Input／Effect／Equipment实现；Frontend无条件要求GameplayEffect。这些是核心1.11的实际整改点，Fixed消费链按同一边界处理。允许暂时接线失败，不因删除旧依赖而删掉技能节点原有业务。

业务收益是同一技能可被不同合法调用方使用，不为执行技能先造角色或整套角色模块；代价仅是把原有模块装配与事务控制放回真实调用方。已有技能数据／拓扑／布局保留合理职责，Program名称本身不是架构缺陷，不借此再做一轮无关重命名重构。

### D21. 先收口领域职责的文档与执行划分（已审阅，具体接线见D22）

提报编号：DOMAIN-BOUNDARIES-20260914-02。用户要求先把领域做好，并把规划交工作协调窗口审阅。本节把D19／D20已明确的边界组织成可执行的领域分工，不新增通用领域框架、不要求按表新建九个任务／程序集，也不据此重派已有实现。原方案独立Skill、直接Timeline、原生Pose和保留网络行为不重新选择。

#### 业务领域与状态归属

| 领域 | 输入 | 规则与唯一状态 | 输出／外部依赖 | 既有实施归属 |
| --- | --- | --- | --- | --- |
| 输入 | 正式输入样本、请求、序号和调用方Tick | 值投影、缓冲、过期和共享请求消费；同一请求不能在每个技能各存一份Consumed | typed值读取、请求查询／消费结果；不拥有技能执行帧 | 核心内输入模块；输入作者合同仍归原作者owner |
| 角色控制 | 输入、已提交Body／角色事实、可用动作与装备路由 | C#走跑转身、控制机器、输入到意图的选择；发起技能启动／取消请求 | 运动意图、技能请求；不解释技能图或Timeline内部 | 核心内Control模块；原控制算法保持 |
| 技能 | 独立技能数据、局部状态、调用输入／目标／时间和已声明服务 | 技能图、局部变量／调用帧、技能自身准入与生命周期合同；不创建角色事务 | 技能结果、领域请求和候选；只调用所需接口，不组装角色模块 | 核心Skill实现与独立编译；同一入口供角色与TreeClip调用 |
| Timeline | 正式只读轨道／Clip／Section、调用时间与资源／TreeClip服务 | 区间遍历、进入／采样／退出、循环、播放／取消和私有状态 | 窗口、运动、效果／Cue、相机、动画、TreeClip的阶段调用及候选；不拥有被调用业务算法 | 既有Timeline任务第12组 |
| 运动 | Control／技能／Timeline的正式运动请求、源曲线、目标、Body事实 | 来源仲裁、位移／旋转、Root Motion／MotionWarp计算与连续历史 | WorldSolve请求及运动结果；世界碰撞／约束求解仍归Solver | 核心运动运行；RootMotionCurveAsset／源区间／映射归原运动源owner |
| 效果 | 施加／移除请求、目标、效果定义和Tick | 属性、标签、效果实例、持续／叠加／移除状态 | 查询与效果／Cue候选；共享状态属于效果owner，不由技能复制 | 核心Effect模块；既有业务算法保持 |
| 装备 | 装备配置、装卸／路由请求和已提交上下文 | Slot、装备实例／局部状态、授予和动作路由 | 可用技能／装备上下文与外观选择；不执行技能图、Pose或运动 | 核心Equipment模块；作者资产由原owner接入 |
| Pose／动画表现 | 已提交玩法事实、EventGraph typed Frame、有限播放请求和资源 | 原生图实例、Player／State／Slot／Blend／惯性化、节点历史与约束交接 | 唯一最终姿态／属性发布；Source、Constraint、Final保持明确模块边界 | 既有Pose任务第3组；Foot／IK算法归其现有任务 |
| 相机 | 输入、角色只读事实、相机配置及Timeline请求 | 模式、目标、混合、镜头求解与自己的历史 | 相机结果与实际采用状态，不改角色控制／技能状态 | 既有Camera任务 |

Source采样、动画混合、Constraint、Final Writer是Pose内部及原领域服务的分工，不各自升成新领域。ACL／MM／Foot数据加工继续按实际资源owner处理，不归技能编译、不因本表重建加工系统。

#### 两层装配与动作协调

角色装配只连接实际需要的领域模块、聚合完整角色状态并发起调用；Session／Pipeline／Backend拥有外层时钟、执行顺序、WorldSolve和最终提交边界，网络保存／恢复相同已提交领域结果。两层都不解释技能节点或Clip，不用包含全部模块数据的万能Context代替接口。

角色控制“选择动作”不表示把现有准入／消耗／替换／结束规则搬进Control重写。当前Float32ActionRuntime同时连接ActionAdmission、技能请求／生命周期、输入消费和装备上下文；它需要按方法分清调用方动作协调与技能自身规则，而不是整类移动。角色或其它调用方的动作协调处理该调用上下文内的并发、替换与启动／取消；技能定义自己的准入和结束合同，通过正式效果／输入等服务取得结果。窗口由当前正式动作／战斗业务owner保存，Timeline只按时发起开关；尚未单独定位的窗口实现由核心做符号级归属，不泛化归给Effect或另造新战斗系统。

一次攻击的职责链为：输入保存唯一请求 → 控制／调用方选定动作 → 正式准入与技能调用 → 技能请求Timeline → Timeline遍历完整区间并调用运动／窗口等领域 → Solver与调用方完成本Step → Backend发布已接受结果 → Pose／Camera消费已提交事实。每个候选由其领域拥有；外层失败由调用方决定丢弃，技能结果不能直接Abort整个角色事务。

#### 文档组织与实施批次

主proposal说明范围与收益，D21保存领域表／共享接口与协调事项，主tasks只保留核心工作，spec-audit保存代码证据和未完成边界。Timeline仍唯一使用restyle-timeline-editor-slate-style/tasks.md第12组及其direct-runtime规范；Pose仍唯一使用refine-pose-graph-readonly-blackboard/tasks.md第3组。各自implementation／execution记录实际改动，不在主方案复制领域checkbox；docs/coordination-progress.md仍仅由协调窗口维护。本轮不改其它领域文档或代码。

建议按下面的业务切面推进，每一批都包含状态、真实实现和调用消费者，不设置“先写完所有接口”的独立阶段：

1. **技能与调用方边界**：1.9／1.11接需求服务，移出技能内角色模块装配和具体角色事务。技能Pending结果允许没有运动／WorldSolve请求，不得以取消技能结果为由中止整个角色事务。保留技能本地状态与既有图执行算法。
2. **共享状态边界**：2.6／2.7固定角色多技能聚合；输入消费记录归输入owner，技能只拥有自己局部等待／调用状态。角色内容与模块schema共同校验，Timeline私有状态通过3.8组合。1f26e07c0已增加多Ability分区和角色Hash，按当前剩余字段收尾，不重做已正确聚合。
3. **领域到领域的实际调用**：Timeline接完整区间事件、取消和候选输出；Pose接具体节点服务、Source／Constraint／Final；核心只接D14／D15公开合同。动作、运动、效果、装备服务保持各自业务，缺事实服务必须失败，不让默认BodyFacts产生原点／零速度假数据。
4. **公共入口与产品**：核心2.1／4.7／6.4把现有领域实现接到Host／Session，3.x接网络恢复与提交；原作者／预览任务使用同一入口。必要旧链按D17继续删除，保留业务未接通时明确记录不可用，不增加兼容模式。

各领域在自己依赖满足时可以继续，不设全员等待。此顺序是同一重构的职责收口，不能以“领域优先”为由无限推迟真实入口，也不以整体暂时不可编译为理由取消已批准的删除。

#### 交协调窗口审阅的具体事项

- 确认九个业务职责和两层装配是否与已有任务冲突；它们不是新增任务数量。Input／Control／Skill／Effect／Equipment／运动运行当前仍属核心，不自动假定已有独立实现窗口接管。
- 核心仍同时承担多个领域和网络公共接线；若需要进一步分担，请只在已有任务中指定明确文件／符号／合同与唯一清单，保留现有正确修改，不按超时接管。主方案本轮不自行分派或新增窗口。
- 确认ActionRuntime混合职责由核心按方法分清；运动源／Camera／Foot与Pose只走既定领域接口，不把算法迁移变成多方共写。
- 确认共享输入消费、无运动的技能结果、缺失事实明确失败属于原职责约束补全，不新建通用调度器／事件总线／事务框架；协调记录旧“待分派”快照不应覆盖后来已授权并正在执行的事实。

业务取舍：保留大执行器继续添加适配能暂时少改调用处，但会延续共享状态重复和角色依赖；按上表让实际owner承担职责会暴露接线缺口，但减少后续同步和取消／恢复成本。选择领域收口不要求更多文件或接口，优先复用现有正式端口与算法，只删除／提取发生越界的职责。

### D22. 角色Step、多技能调用与领域服务的具体接线

决定编号：DOMAIN-BOUNDARIES-20260914-03。用户明确要求协调窗口直接更新各自规划文档并告知相关实现。沿用核心、Timeline、Pose现有实现及唯一tasks，不向规划窗口层层转发，不新增任务或通用框架。本文定义核心接线，Timeline细节在其timeline-direct-runtime.md第9节，Pose细节在其design.md第12节；不是再要求从协调记录拼接执行方案。

#### 角色工作状态与技能局部状态

每角色每Step准备一份领域工作状态；输入、效果、装备按其原规则推进，多个技能在各自调用实例内执行并访问同一份领域工作状态。CharacterRuntimeState保留已有多Ability集合、角色Hash、领域状态及正确codec；CharacterRuntimeStateTransaction以角色Step为根，不再以单个Installation决定完整角色事务。多个技能不能分别从旧角色快照生成完整结果，再由后提交者覆盖前面的修改。

技能局部状态接口只允许访问局部槽位、调用帧与本次候选。共享InputRequests/Consumed归输入owner，效果、装备、MotionWarp和序号归实际领域服务；整角色Abort及完整Savepoint/Restore不经技能接口暴露。复用已有保存点机制并明确作用范围，不在技能内部复制角色快照或另建事务框架。Float32/Fixed、状态codec与网络恢复同步调整，保留已正确多技能聚合和角色Hash，不能重复撤回。

InputRuntime在角色层接收/筛选/过期请求一次，技能只记录自己的等待进度。请求使用既有身份、来源/序号、优先级及确定性选择语义；多个候选可以查询，正式准入接受时才按原规则消费同一份请求。失败候选不提前消费，取消已运行技能也不自动撤销过去已提交的输入消费。

#### 技能调用与动作协调

技能输入是独立数据、调用身份/目标/时间、局部状态和实际声明所需的typed服务；输出是运行/完成/失败/取消状态、局部候选、实际领域请求和观察。角色与TreeClip调用同一入口，不强制构造角色上下文。AbilityControlRuntime保留图执行、条件、局部变量、调用栈与停止流程；AbilityExecutionServiceSet内的效果整帧推进、装备Begin/End、输入接收/过期处理交回角色领域调度，不能逐技能重复执行。

保留已有Input/Tag/Effect/Equipment/Motion等正式端口，按需要收窄并绑定。不使用效果的技能不要求效果服务；实际读取Body事实时必须有有效数据，不能用默认原点/零速度/false冒充。保留已有按技能能力选择Effect binding的修正，不重新派发旧缺陷。

PendingAbilityEvaluation只表达技能候选，不强制CharacterWorldSolveRequest；无运动请求的技能可以完成。Motion汇总真实请求后由调用方接Solver。技能Discard仅丢弃该调用候选，不能通过接口背后的角色事务Abort中止整个Step。正常取消沿技能生命周期处理，关闭精确实例/generation拥有的窗口和子调用；效果是否随技能结束由效果既有持续/绑定规则决定。

ActionRuntime按方法解除单个AbilityExecutionFrame依赖，保留ActionAdmissionControl、ActionSkillActivationFlow、ActionSkillCommitFlow和AbilityExecution实际规则：Control选择动作；调用方动作协调管理上下文内的并发/替换/启动/取消；技能保留自身准入/结束合同；Equipment判断装备上下文；Input保存消费事实。窗口保存者沿已有动作/战斗符号定位，不笼统归给Timeline或Effect，也不整类搬进Control重写。

领域调用保留两类业务形式：准入、扣费、标签查询、输入消费需要立即返回本帧候选结果，通过实际领域服务处理；运动仲裁、相机和表现输出可汇总正式请求再处理。前者必须有明确工作状态与撤销范围，后者不能把排队当成功；都进入原Step接受边界。全部改成延迟请求会破坏技能条件分支，全部直接发布则破坏失败丢弃；不为统一形式新增事件总线。

#### 角色结果、外层时钟与真实Host

PendingEvaluationBatch按Actor组织时必须保存角色候选，汇集多个技能和领域结果；PendingAbilityEvaluation只保留内部调用作用域，不能一份技能结果代表整角色。角色候选交既有WorldSolve/Finalize/Backend接受，保留原确定性顺序、能力校验、网络Pass及原子提交。实际安装前完成候选检查，安装阶段不重放技能/Clip业务或提前发布部分结果。

CharacterRuntime必须提供实际领域推进与多技能调用，roster/配置集合不等同运行完成。NumericProfile、TickRate由正式Session/数值配置决定，技能数据校验匹配；不能从第一个技能反推角色时钟，也不能让没有活动技能的角色无法推进控制/输入/领域状态。OperationSetVersion保留技能执行兼容含义，不泛化为角色总业务版本。

Host/Registration/Session退出Program/Projection必要条件，安装真实领域绑定和服务。核心只汇集owner确认的采用事实，调用Timeline的实际推进/停止/恢复及Pose的真实源采样至Final结果，不在核心复制它们的状态和算法。角色/技能内容、状态schema、World/Pipeline身份分别保留，网络恢复组合相同已提交结果。预览/作者使用原正式入口，不新增备用播放器或假成功。

#### 实施归属、依赖与规范对账

核心唯一修改Ability执行帧/服务/Pending、CharacterRuntimeState/codec、InputRuntime、ActionRuntime、共享技能Timeline编译/TreeClip执行、Host/Factory与Pipeline/网络；Timeline第12组和Pose第3组仍是唯一域内清单。核心任务落点为1.9—1.11、2.1/2.6—2.8、3.1/3.2/3.8、4.7和6.4，不复制领域checkbox。各业务切面包含状态、真实算法与消费者，不设先写完全部接口的阶段，不替用户另定优先级。小步中文提交，仅实际接口阻塞在实现之间协商，不回执/日报/转发。

本节细化现有character-domain-runtime增量中的技能独立、共享输入、统一Tick提交恢复与唯一实现归属，不恢复旧Program/Image路径。现行spec中旧总载体要求继续由本change已有delta替代，不新增第二份规范。最新并行代码若已修正某项，仅续接剩余消费者，不以历史审查回退正确实现。维持单一角色工作状态能保留同帧多技能修改，代价是共享服务与角色接线要一起完成；将每个技能作为完整角色事务会延续覆盖风险，不作为过渡方案。

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

1. 按D16职责表保留实际算法和数据，由各owner先执行D17删除批次：核心0.1／0.2及共享Timeline发射退出；Timeline／Pose在各自唯一清单先删专属旧链。无需先完成新Runtime或保持全项目可编译。
2. 分类处理删除暴露的消费者：废弃的直接删除；仍有业务用途的连接正式领域接口；跨owner文件交原任务处理，不恢复旧类型或转换链。
3. 核心接通独立技能格式／执行、Control／Motion／Effect／Equipment、角色Host／Factory与完整Step／状态；两个Target和普通.NET产品保留。Timeline／Pose任务接通直接内容和原生图算法，核心仅集成D14／D15公共合同。
4. 接通Network Pass、roster、baseline、snapshot、握手及产品manifest的领域状态／内容接口，保留网络顺序、能力校验和恢复行为。
5. 原作者／预览／C#任务接回正式领域接口；核心完成角色配置、Corin场景／Variant与公共观察，领域资产由其owner的正式API处理。
6. 清理剩余废弃引用并同步共享规范，完成保留业务和正式产品接线后才认定整体迁移完成。

删除优先来自用户明确选择；同批不同owner可独立推进，不要求全项目删完才允许已完成删除的领域补接线。每一步按owner范围小步中文提交，中间失败按D17记录；旧资产／协议只做一次正式迁移或显式重生成，旧reader不作为过渡运行方式保留。

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

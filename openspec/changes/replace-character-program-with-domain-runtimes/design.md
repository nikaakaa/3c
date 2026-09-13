## Context

动机见 `proposal.md`。本设计采用本轮用户确认的组合：角色总 Program 退役、技能数据独立、网络 Pass 保留、Pose 原生 FlowCanvas Runtime。此前 `docs/locomotion-skill-compilation-boundary-plan-2026-09-13.md` 中保留角色 Program、扩展统一 artifact 的方案只作历史评估，不作为本 change 的实施依据。

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

Ability 输入是对应 `GameplayAbilityDefinition` 的私有图／FSM／条件／子图引用／Timeline 与真实技能参数引用。共享技能只构建一次；授予、装备选择和实例目标在绑定／运行时提供，不为每个 Character Definition 复制技能。

现有数值中立语义处理只保留技能内容，产出独立可校验的 Ability 数据；Float32／Fixed 仅降低技能数值、状态和运算。目标执行数据不保存 C# Locomotion、BodyMotion、全装备目录或动画表现图。所有技能引用的领域 ID 在绑定时由对应模块解析，并在 Session Active 前报告缺失，不在每 Tick 扫描。

**取舍：**独立技能数据利于图编排和两个数值目标／普通 .NET Host；全部改 C# 会取消作者图能力，全部改 FlowCanvas Runtime 则扩大到 Gameplay 确定性、快照及 Unity 依赖迁移，本次均不选择。原 Character Program 不能仅改名 `CompiledAbility` 后原样保留。

### D2. 角色状态按模块归属，统一捕获不要求统一可执行 Program

Control、Ability instances、Input requests、Effect、Equipment 和跨 Tick MotionWarp 各保存唯一正式状态。角色快照在一个成功提交的 Tick 边界收集这些分区，携带模块身份、状态 schema、实例身份和内容身份；Snapshot／Hash 不读取 Pending、原生图对象、临时运动贡献或 World query scratch。

恢复是先解析并核对完整候选，再原子安装 Character／World／Pipeline 状态。C# 控制模块必须同时恢复其可读字段和 UnityHFSM 内部当前状态；技能必须恢复调用帧、Timeline cursor、请求及动作实例，不只恢复位置或 active skill id。不会跨 Tick 读取的派生索引从同一正式输入重建，不再进入快照。

Control 的模块 ID／语义版本、参数与 state schema 在实例绑定中保留，但不再通过 ControlModule catalog 和全角色 slot 编码。控制 Motion 描述直接来自 C# Contract；作者可调数值来自明确配置。SourceCurve 仍是独立运动资源，保留现有帧／秒／Tick 映射、坐标空间和数值转换次序；输入适配器的 CameraRelative 判断同步改读该唯一合同。

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

动画 Profile、Rig、原生图和明确资源引用是表现输入；实例创建时建立只读运行绑定，供 Player、Slot、Constraint、Camera 使用。静态资源的加载／骨骼索引解析属于正式初始化，不能在每帧遍历资产。已有 ACL、Motion Matching、Foot 数据与相机资源产品保留自己的构建、身份和加载合同。

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
4. 接入原生 Pose 图／连接／节点和两阶段表现求值，迁移 source／constraint／publication 绑定及实例状态。
5. 迁移作者工具、C# 输出、预览和诊断；通过正式 owner API 更新明确的 Corin 资产范围，独立资源产品按真实变化处理。
6. 删除角色 Program、Pose Image、旧 Projection 总包和专属 compiler／codec／UI／缓存；同步规范与活跃设计中的旧运行要求。

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
| `rebuild-btsmtl-preview-with-scene-play`、`finish-skill-runtime-observation` | 正式会话观察、暂停／推进、身份与来源、用户交互 | 将角色 ProgramEpoch 与 Pose Image 来源接到领域版本和原生图实例；不创建新预览会话 |
| ACL／Camera／Foot 相关任务 | 资源构建、算法与已有行为改动 | 只迁移被取消总包的绑定，不扩大到未完成算法或资产 |

本轮不写上述 change 的 tasks／execution，不发日常窗口消息。已有代码如与本设计所需合同直接冲突，先记录具体文件、双方 owner 和业务取舍，再按用户约定提出一次实际冲突；不能把旧规范文字本身当成要求用户重复批准本次规划。

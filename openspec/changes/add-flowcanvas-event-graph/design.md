## Context

r3（2026-09-13）按用户“先说现有实现哪些混杂、改一下文档”的要求修订。目标是整理现有动画输入与作者逻辑，保持当前表现，不为事件图增加一个未提出的业务用途。此前建议的“根据速度调整步频/播放倍率”撤回；通用Get/Set能力存在，不等于必须给Corin编造一个使用场景。

r1 的原生 FlowCanvas runtime、动画宿主和变量交接方向保持；r2 的公共 C# authoring 双工具、直接 API 与协议退役方向保持。r3 不重做这些已有基础，而是修正输入分类、空图强制装配和内容迁移口径。

本窗口唯一维护本目录规划。当前请求只修改文档，r3 不自动下发实现；已有实现仍以最后明确派发的 r2 为历史基线。普通运行记录不作为新的业务需求或方案确认。

### 当前源码事实与职责表

以下路径相对 Assets/GameScripts/Main；Assets 根位于 3cDemo/Client/3C_Client。本表是当前源码核对，不是新增玩法设计，也不由任务勾选或产物发布结果替代。

| 数据/工作 | 当前生产位置 | 当前消费位置 | r3处理 |
|---|---|---|---|
| Body/Intent对齐、采样时间、Reset代际 | CharacterPresentationFactProjector.Project / SampleIntent | 正式Fact、动画运行与各Pose消费者 | 保留正式事实层，不迁入作者图，不让每张图另做时间对齐 |
| 水平/垂直速度、方向、加速度、朝向误差 | 同一Projector从Body/Intent计算 | FactFrame、RootOrientationWarp、条件/MM等 | 先按现有公共只读事实保留，图需要时直接读；不复制成无消费者的动画变量 |
| MotionPhase | Projector.ResolveMotionPhase：落地、hasMotion、速度阈值和垂直速度分类 | CharacterPoseStateMachineRuntime.SelectPredictiveTarget及正式Fact消费者 | 属于需要辨明的动画分类判断；本轮不迁移、不改阈值。若改成作者变量，会改变公共Fact合同及多处消费，先单独交用户决定 |
| MovementMode | 正式Intent的MovementModeId进入FactFrame | Corin当前21处规则读取presentation.movement-mode | 保持现有Fact身份和状态规则，不改成基于速度的新判断，也不转抄成EventGraph变量 |
| 动画实例变量 | 原生EventGraph声明/更新及唯一Contract/Layout/Frame已存在 | Pose有typed读取代码，Corin当前没有对应变量/Get | 保留能力；只给已有明确作者逻辑建立变量及原消费者，不为消除空图新增效果 |
| Pose参数声明 | CharacterAnimationInputContract.BuildParameters先转写EventGraph变量，再合并各图Parameters | Pose编译与参数消费 | 取消对同一实例变量的第二作者声明和旧合并入口；允许从唯一合同派生只读编译索引 |
| 曲线、BlendShape、Foot权重 | 正式Clip/Pose采样及混合；BuildParameters仍拼入同一参数表 | 原Pose/Foot/最终属性Writer | 保留原运行数学，清理作者/输入层来源混用，不把曲线改为EventGraph Set |
| 节点配置和子图公开输入 | 原Payload、资源及调用接口 | 原节点/调用点 | 留在各自正式位置，不并成共享变量；Blackboard只读投影实际可访问输入 |
| 变量消费绑定 | CharacterAnimationPoseInputFrame.FromPublishedVariables当前每帧扫描Program参数和转换规则 | 同次Pose运行 | 依赖/索引属于已有编译或实例绑定阶段；每帧只交接值，不重复发现消费者 |

当前 CorinAnimationEventGraph.asset 只有 Start/Update、零变量、零连接；其 recipe 也只创建这两个节点。Corin Pose 图当前有9处空参数列表、零变量读取标记，主要状态规则仍读取MovementMode Fact。空图不会改变动作表现。

CharacterAnimationInputContract.Create 与 CharacterSimulationPresentationRuntime 构造当前都强制要求EventGraph，所以即使没有变量需求也必须提供一张占位图。r3明确修改这一装配约束。

Host当前把8项Fact加delta逐项登记并在TryRead再映射一次；MotionPhase、MovementMode等正式Fact类型不在该图输入列表中。Pose Blackboard当前只投影EventGraph变量，并按Action/Foot固定ID排除，不完整表达实际Fact/子图输入。这里是接口/作者视图尚未收口，不能被空Blackboard或成功Build掩盖。

当前代码搜索已无 CharacterPresentationProgramParameterFrame、EventGraphAuthoringDocument、ApplyAuthoringDocument及旧事件图Mapper链。旧协议和motor桥删除属于已有清理结果，不再写成尚待删除的阻塞，也不回退已正确成果。

## Goals / Non-Goals

**Goals:**

- 作者在有真实事件逻辑需求时使用原生节点；当前先完成现有动画输入职责整理，不强制所有角色创建变量或占位事件图。
- 输入是正式角色表现事实、本次 delta 和图内状态；输出是同一次完整更新后的 typed 只读变量帧。
- 将现有事实、动画判断、作者变量、曲线和配置按真实消费者分责；保持现有公式、阈值、时序与动作表现，删除重复来源表述。
- 原生 UI 和生成代码使用同一正式配置 API 与既有局部 Undo/保存；公共工具显式导出/生成，Build 独立，不做 UI-only 能力或临时变量表。

**Non-Goals:**

- 本次不实际接关卡、相机或其它 Gameplay 宿主，不增加全局事件总线。
- 不新增播放倍率、步频调节、速度平滑、状态阈值或其它未提出的效果；不提供动画播放/停止、技能请求、伤害、角色移动、世界查询、Foot/IK或骨骼写入节点。
- 不重写 Pose/Skill runtime，不增加 EventGraph IR、节点 lowering、虚拟机或 fallback。
- 不接管 Pose 只读输入、曲线分类、Body Foot 入口、资源作者改革和旧重复 Pose 子图清理。
- 不增加中央 Validator、整包同步事务、结构 DTO 中转、自动源码同步或事件图专用 MCP。
- 不新增测试或验证任务；用户自行做端到端验证。源码Build、MCP可用性和反复回放不作为本次职责整理的新增任务。

## Decisions

### D0. 先整理已有职责，不添加新业务来填满图

本次已确定的清理是输入来源和装配规则；尚未确定的不是“再找一种动画效果”，而是哪些既有动画判断适合交给作者。公共事实计算不因为能画成节点就必须迁入EventGraph，Pose中的状态机、source选择和姿势混合也不整体搬出。

一个计算只有同时具备“当前已有生产逻辑、真实消费者、明确动画作者职责”才进入迁移清单。保留原表达式、阈值、delta、初值、Reset及消费者，迁出后删除原重复计算。未发现这样的计算时允许本轮没有Corin业务事件图，不复制Fact或接常量节点来凑结果。

MotionPhase保持当前正式Fact语义。将它变成作者状态涉及条件/MM/预测转换，不属于已经确认的例行搬迁；本轮只记录候选与影响，不要求为此停止其它输入清理。

**装配规则：**
- 没有绑定EventGraph且没有动画变量或事件执行需求：合法的同一动画链，不创建事件宿主、不虚构变量帧；Fact、曲线与Pose照常由原机制处理。
- 有变量需求但缺图、缺变量或类型不匹配：明确失败，不提供旧motor值或默认补值。
- 显式绑定了事件图：按图原生逻辑执行，不能因零输出就自动略过。空图可以是尚在编辑的内容，但不代表业务接入。
- 当前Corin仅为满足强制装配而产生的占位图/绑定/专用recipe，在确认没有实际依赖后按明确内容范围移除；保留通用创建、EnsureRoot、导出/生成和运行能力，不扫描删除其它作者图。

业务取舍是按需求装配会增加明确的“没有变量输入”合同表达，但可避免无业务用途的资产和每帧空执行。统一强制空图能减少一种装配情况，却让作者为了运行角色维护无意义内容。本次选择前者；它不是运行失败时启用的fallback，也不增加另一Pose执行链。

### D0.1 每种输入只有一份正式来源

- 事实引用原Fact schema和FactFrame，不把相同值再声明为EventGraph变量。
- 动画变量引用同一原生声明及唯一Contract/Layout/Frame，不再允许每张Pose图重声明同一实例变量并参与冲突合并。
- 子图公开参数属于调用接口，节点常量与资源属于原Payload/资源owner。
- 曲线绑定指定输入Pose，保留采样、混合、惯性与最终属性写入；不再靠Action/Foot固定ID或BlendShape名称前缀判断其来源。

这里整理的是作者语义和输入合同。Compiler仍可从这些正式来源生成只读索引、类型页与固定绑定；这种执行布局不是第二份可写变量真相。引用/类型/consumer需求应在现有编译或实例绑定阶段确定，运行帧不重新扫描全部参数及转换规则。

Fact输入节点由现有EventGraphFloatInputNode等正式类型、AddAuthoringNode、ConfigureHostInput和ConnectAuthoringPorts建立。节点输入引用已有Fact identity，Value口在原生逻辑真正消费时读取当前宿主Fact；仅把FactFrame放进HostContext不代表已接入业务图。

宿主准入从正式Fact声明投影受支持类型，并使用既有typed读取能力，避免另建字段清单与业务分支。当前不支持的Enum/Identity/UInt64若没有实际EventGraph消费者，不为填满菜单额外扩类型；有真实需求时必须完整支持或明确报告，不能静默转成字符串/Float。Pose已有合法Fact读取不依赖EventGraph是否开放该类型。

### D1. 原生执行事件图，编译执行PoseGraph

选择原生 FlowScript 运行。每实例克隆作者图，以 Manual 方式由正式宿主推进；不挂第二个自动 FlowScriptController，也不改开原 Skill/Pose 作者图的运行禁令。

业务收益是作者直接使用原生节点与执行顺序，不需要每增加一个计算节点再做一次项目指令翻译。另一条有价值的路线是将原生作者节点编译进动画 Program，可共享现有状态事务和密集布局，但要逐个维护节点 lowering，覆盖范围会受它约束。本提案选择直接复用，并明确承担 D5 的原生状态推进规则；不同时保留两种执行模式。

运行产物继续由原正式Build发布，接口/类型检查属于该已有产品生命周期，本轮不新增Build或验证任务。Build不把事件连线翻译为另一份事件程序；Pose自己的Compiler保持原链。

### D2. 通用基础只接宿主，动画提供业务数据

拟定三块实现：

| 模块 | 输入与输出 | 唯一职责 |
|---|---|---|
| HostEventGraph + NativeEventGraphRuntime | 原生图、宿主合同、事件调用 → 原生执行结果 | 复用 FlowScript、实例化、绑定、触发、故障和销毁 |
| EventGraphHostContract | 宿主事件/输入/节点权限声明 → Capability 和接入约束 | 通用机制不引用 Actor、Pose、Motor 或 CharacterPresentationFactFrame |
| CharacterAnimationEventGraphHost | 正式 FactFrame、delta、Reset → 动画变量输出 | 动画时机、事实接入及对外只读交接 |

当前通用接入已经位于 Runtime/BTSMTL/EventGraphs，角色接入归 Character/Pipeline/Animation/EventGraphs；本次收口沿用现有位置，不因协议退役搬迁已有效的运行模块。现有 Character 程序集已引用 FlowCanvas/NodeCanvas；不拷贝插件，也不把动画宿主塞入 Skill 图目录。

直接把新事件图增加成 BtsmtlSkillFlowGraph 的一个 role 会少建入口，但会把 Skill 编译、变量 scope 和动画运行绑在一起。独立宿主适配多一个明确边界，却允许以后关卡提供事件而不理解动画；这是通用性的业务价值。第二宿主本轮不实施，也不宣称已经跑通。

### D3. 原生Blackboard是唯一变量声明和状态

当前 HostEventGraph 的原生 Blackboard 是正式对象上的变量声明，保存变量 ID、作者名称、类型与初值；不增加可写的 AnimationVariableDeclaration 镜像。显式导出把这些声明表达为可重建 C#，显式生成按代码重建声明，二者不是同时可写的同步数据源。运行时每实例只有一份 Blackboard 变量状态，原生节点历史由同一实例的原生节点持有。

公开合同由原生声明派生，拟定为：

| 合同 | 必须包含 |
|---|---|
| EventGraphVariableReference | 图稳定身份 + 原生 Variable.ID；显示名不参与运行寻址 |
| CharacterAnimationVariableContract | 图/合同 revision，变量引用、精确类型、初值和读权限；从原生声明只读投影 |
| CharacterAnimationVariableLayout | 发布前生成的唯一密集列及类型，不保存运行值；所有消费者引用同一 layout identity |
| CharacterAnimationVariableFrame | Actor/实例、表现采样身份、Simulation sample tick、Reset generation、图/合同/layout 版本、typed 只读值 |

保留既有 Pose 交接的 Float、Bool、Int32 精确类型，Int32 不以 float 中转。Vector2/Vector3 在事件图内保持原生类型，需要时按正式消费接口提取分量。对象、集合、任意枚举和直接 Pose 向量口不因本次整理自动扩展；这些是可用基础能力，不是必须给Corin新增变量的需求。

Pose 可见变量及跨图权限由独立只读 change 按同一合同投影。事件图负责 Get/Set，PoseGraph 只 Get；变量重命名保留 ID。曲线、BlendShape 与脚权重继续由 Pose 采样/混合链拥有，不能按当前 Usage.Control 标签直接迁入事件图。

备选是单建动画变量资产供两张图读写，能让变量脱离事件图复用，但会重新建设 FlowCanvas 已有的声明与编辑适配。本提案直接沿用原生声明；运行输出快照是只读发布结果，不是第二份可写状态。

### D4. 作者与节点范围沿原生能力接入

使用原生 GraphEditor、Blackboard、节点/连线 Inspector、breadcrumb、Macro 与 Undo。动画图从正式动画作者入口打开，不新建常驻 Details 面板。已有只读 Blackboard 可拾取/拖拽的改动必须保留。

第一版准入原生 StartEvent、UpdateEvent、Get/Set、逻辑/比较、合规数值方法、SwitchBool、Split Instant、Sequence 和同步 Macro。原生数值方法包装器直接调用原有方法，例如限速趋近、插值或 clamp；不复制算法。宿主目录登记成员身份、端口和副作用权限，供原生 UI、直接 C# API 和领域局部规则共用；代码输出薄适配只读同一描述及正式对象。Player 所需反射成员和泛型保留进入正式构建，不新增中央 Validator。

每图一个初始化入口和一个每帧入口，多个处理分支由原生 Split Instant 明确排序。保留 SwitchBool 的 Then 与 Sequence 的 Flip Flop 含义。Get 值按原生消费时机读取；需要“写入前的值”时作者显式保存，不能假定值线先统一执行一次。

动画宿主只提供一次同步更新。UpdateInterval 固定为 0；拒绝 Wait、Timed Split、协程与使调用跨帧悬挂的断点。Set 的 perSecond、DeltaTimed 等直接读全局时间的模式不可用；作者显式将宿主 delta 连接到原生算术节点。该限制只属于本动画宿主，不全局删除 FlowCanvas 的等待/延时能力，以免以后关卡流程无法沿用。

放开全部节点能最快获得原生功能，但会把副作用、跨帧状态和任意 Unity 时间带入动画。宿主准入需要一次元数据工作，却不重写节点行为，同时明确哪些能力真的可用于这个业务。

### D5. 更新图按表现事件推进，不伪装成Pose状态事务

实际顺序：

1. 现有FactProjector完成本次Body/Intent时间对齐及当前仍归它拥有的正式事实计算。
2. 按D0装配了事件图时，首次有效输入先绑定数据、原生Start，再执行同次Update；以后只执行一次UpdateGraph(animationDeltaSeconds)。
3. 配置了事件图时，原生调用完整成功后冻结其输出；失败不发布，不启动本次Pose。
4. 未配置事件图且正式输入合同不要求它时，跳过宿主创建和事件调用，不构造假的变量发布结果；仍由同一Animation.BeginPresentation及Pose链消费正式Fact和其它输入。
5. Pose按编译/实例绑定的消费合同读取实际所需输入，继续Action、PoseAdvance、Source、Evaluate和Seal。

更新成功后，如果Pose source尚未准备好，该图实际拥有的变量和原生节点状态仍然保留，下一次表现事件继续推进；这里没有新增任何计算内容。Pose的Pending、Committed和Physical Writer保持原规则，输入“更新完成”不能被标成姿势“显示完成”。

另一种业务要求是计数等也随 Pose 成功才前进。这需要完整的图状态事务或编译路线，不能只备份 Blackboard：原生 Sequence、事件计时、Macro 等另有状态。本提案明确选择原生推进，不引入反射快照、每帧克隆或失败后重放。

Actor/Body discontinuity、Reset、Replacement 结束旧图实例及其节点状态，下一次有效输入重新初始化；普通 PoseState 切换不重建事件图。Pause/无正 delta 不推进。源输入错误、图错误或 Pose Actor 已 Faulted 时停止该实例，直到正式 Reset/Replacement。

### D6. 失败与输出寿命由正式边界保证

只读输入在声明/连接和实际宿主写入边界拒绝 Set；不是只禁用 UI。节点异常或非法数值使该次调用失败；已部分 Set 的原生实例不再被消费，不声称恢复了全部内部状态。

原生 Ports.cs 的 Editor 捕获分支会调用 Node.Error，外层 catch 不一定收到异常。因此在 Node/Graph 正式错误边界提供按图实例的执行失败通知，统一交给当前调用结果。不能扫描 Console、依赖 Logger 是否开启、复制第二份 Ports 或修改全局节点执行行为来补洞。

成功输出使用预分配 typed 缓冲与明确只读租约，在所有 Pose/Worker 消费结束前不得重写。绑定时解析原生变量与密集列，每帧不按名称搜索，不把可变 Variable 或 Unity 对象传入 Job。观察区分原生执行、已发布输入和已提交 Pose，只在正式场景暂停/单步时控制时钟。

### D7. 与Pose及C# authoring任务的唯一分工

下表源码路径相对 Assets/GameScripts/Main。各方只修改自己拥有的文件或已明确划分的字段；接口实现和删除必须依赖实际调用迁移，不能用临时桥让各任务单独报完成。

| 事项或文件 | 本任务唯一职责 | 其它任务职责 |
|---|---|---|
| 原EventGraphAuthoringDocument及ApplyDocument | 当前已无旧链代码引用；保持删除结果，不重建模型 | 不再将已清理协议列为新的等待前置 |
| Runtime/BTSMTL/EventGraphs/HostEventGraph.cs | 保留已形成的原生运行/身份/创建/连接与直接API，已删除的ApplyAuthoringDocument不恢复 | 其它任务消费 API，不同时编辑本文件 |
| Runtime/BTSMTL/EventGraphs/HostEventGraphEditorMutation.cs | 保留直接变量/节点/连接/配置、局部Undo与真实规则，已删除的ApplyDocument不重做 | 不将该文件整体移入公共输出器或另建事件图事务 |
| Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseGraphAuthoringAdapter.cs | 提供替代 API 和事件图类型合同，不接管该文件 | Pose 任务将 ApplyEventGraphMutation 调用改为直接 API |
| Editor/CharacterPipeline/Authoring/PresentationDocument/AgentAuthoringEventGraphDocumentMapper.cs 及公共 Agent 协议 | 不接管公共文件，不搬迁其中完整结构模型 | C# authoring 任务在调用方迁出后删除 Mapper、协议 DTO 和五工具等 |
| 事件图完整对象读取及正式 API 输出薄适配 | 读取原生变量/节点/配置/连接/Macro/布局，向共同代码输出器提供薄扩展 | C# authoring 拥有通用遍历/语句输出、生成上下文、两个 MCP、文件写入和保存编排 |
| 动画根事件图引用、Contract/Layout/Frame、事件更新 | 本任务唯一拥有；生成后根引用通过正式 API 明确恢复 | Pose 只读取同一声明/布局，不自建更新器或变量表 |
| Pose Get、作用范围、条件、BlendSpace、Compiler/Program消费和曲线 | 提供唯一输出与身份合同，不重做消费 | Pose 任务唯一负责全部消费适配和曲线/Body清理 |
| 原固定motor桥 | 当前已无旧类型代码引用；保持清理结果 | 不恢复FromFact/FromDirect作为缺变量补值 |

r2曾存在的Agent Mapper → EventGraphAuthoringDocument → ApplyDocument整图重建链已在当前代码清理。历史记录保留其来由，r3不重新列为当前缺口。

r3新增接口清理分工：本任务维护EventGraph可选装配的生产侧、Fact宿主适配与占位recipe/根引用；Pose任务维护CharacterAnimationInputContract、Blackboard来源展示、参数/曲线分类、静态消费绑定及运行/Preview的无变量输入表达。FactProjector仍由原Presentation/Pose事实所属模块维护，本任务不擅自接管它或改变MotionPhase。两侧接口需共同表达D0规则，不各自增加一个空Frame或默认提供者。

直接 API 的参数应是该操作所需的对象、稳定 ID 和 typed 配置；不能仍收取一份包含整图变量/节点/边的“新配置模型”，再清空整图执行它。有效的 AddNode、CreatePortConnection、变量绑定、ConfigureIdentity、局部 Apply/Undo、dirty 和失败恢复继续服务人工编辑与 C# 调用，不为去 Agent 而删除这些正确能力。

### D8. 公共C#输出/生成与事件图薄适配

公共行为严格采用 authoring r2：

| 入口 | 输入和输出 | 事件图侧责任 |
|---|---|---|
| btsmtl.export_code | 明确当前资产、Definition/owner 与代码输出路径 → 完整 C#、根入口、外部依赖和诊断 | 完整读取所拥有的事件图闭包，向共同输出器表达正式创建/配置/连接调用；不读旧源码，不修改输入资产 |
| btsmtl.generate_assets | 明确源码、对应已编译 recipe_type、Definition/owner、生成范围和输出路径 → 根对象及实际保存结果 | 提供同一正式 API，重建范围内图/变量/节点/边并恢复指定 Profile/根挂接；不提供任意 eval 或另一生成服务 |

工具、recipe 基础合同、通用值/语句输出和范围保存归 C# authoring 任务。事件图适配覆盖稳定图 ID、原生 Variable.ID/类型/初值、节点真实类型与配置、宿主输入、Get/Set 目标、赋值模式、动态端口与顺序、Macro 接口/共享关系、连接稳定身份、布局以及正式外部引用。语义读取直接来自当前对象及已有字段/方法合同，不先建 EventGraphAuthoringDocument，也不经 JSON 转换。

输出按对象身份去重，先创建生成范围内对象，再配置字段/端口、绑定内部引用，最后连线与恢复明确根引用。输出过程中“对象→本次局部变量”的字典和依赖集合只活在当前调用，不成为持久化结构模型。内部引用使用本次创建对象；真正范围外资源作为明确输入。不能把旧生成子资产 GUID 当内部依赖，否则删除生成资产后无法重建。

C# 明确保存逻辑身份，重建后 Pose Get 继续按同一图/Variable.ID 解析；物理 Unity 实例、GUID/local file ID 不要求永久不变。Profile 根及本次明确的外部挂接必须显式恢复，不全局扫描猜消费者。一个共享内部对象只创建一次，范围外共享资源不复制或删除。

完整输出不能省略当前正式内容。薄适配不支持某节点、成员、Macro 字段、布局或引用时，向共同输出器返回精确对象/字段与原因，使完整导出失败；不能输出占位、默认值或半份代码替换旧文件。尚未实现的事件图适配不能被公共输出器声称为已支持。

人工修改/保存图不导出代码；只有显式 export_code 才把当前结构写入源码。显式 generate_assets 按指定已编译代码重建明确范围，不自动合并未导出的人工调整，不读取旧源码做同步，也不追加“创建后再删除”的操作日志。正常 C# 编译只让生成入口可执行，不是把事件图转换成项目事件程序。两个作者工具不自动 Build/Play。

Graph/宿主校验变量类型、节点、端口及只读输入，Pose 校验消费、布局和拓扑，正式 Compiler 校验运行产品。输出器只检查输出完整性，不复制业务校验，不建新的中央 Validator、整包 hash/session/reconcile/sync 或反向导出事务。保留人工编辑现有 Undo 与局部保存；generate_assets 的明确范围保存由公共作者服务调用现有能力完成。

Build 继续独立校验事件图/宿主/唯一变量合同与消费者依赖、AOT/泛型和运行版本，图语义变更沿既有 Stale/Replacement 规则处理。Document 包版本升级从本任务完全撤销；不再以等待 v8/v9 为由保留旧协议。

业务取舍：完整图导出得到的是当前展开结构，可删除重建且不依赖旧资产；不承诺保留原手写算法、变量名和排版。沿用共同输出器需要事件图补齐真实读取/配置能力，但避免维护另一套遍历、C#打印器或重复规则。

## Risks / Trade-offs

- 把所有可计算Fact搬进EventGraph会改变公共事实合同，并影响既有Pose/MM/预测逻辑；本轮保留正式事实，只记录已存在的作者逻辑候选。
- 只改Blackboard显示而保留混合声明表，作者仍无法理解实际来源；输入合同、只读视图和消费绑定按同一分类修改，编译器内部布局可继续服务执行。
- 删除EventGraph强制依赖时若只放开null检查，会留下运行/Preview仍强制要求变量帧的问题；D0要求生产、编译合同和消费一起表达“未请求变量”，不能补一个空成功结果。
- 用新步频、播放倍率或平滑示例证明图非空，会改变用户原有表现；本次禁止这种范围扩张。真实迁移清单为空是允许的结果。
- 占位图也可能被其它内容引用；只按明确Profile/recipe及对象引用清理，保留有效公共生成能力与范围外资产。
- 原生Get/Set、Reset/失败和同次输出规则继续采用r2，不重新选择运行路线，不恢复旧协议或motor桥。

## Migration Plan

1. 以本设计职责表为当前事实基线，补齐涉及改动的生产者、消费者、类型、时序和文件owner。已经删除的旧链不重做，不把其它任务完成计数当成本轮职责已经统一。
2. 先收口来源合同：正式Fact、原生变量、子图输入、曲线、节点配置各自拥有明确身份；Pose消费侧清理重复声明合并和按名称分类，本任务提供同一原生变量/Fact读取接口。
3. 把消费需求解析与索引绑定留在原Compiler或实例绑定阶段；每帧交接只读值，取消运行帧重复发现全部参数与规则消费者的工作。
4. 同一动画运行和完整Preview按D0表达有/无EventGraph需求。缺必需变量继续失败，无需求时不创建占位宿主或伪造变量输出，不增加第二动画执行链。
5. 仅在职责表确认存在应迁移的已有动画作者计算时，才修改事件图内容及原消费者；保持原公式/阈值/时钟/Reset。会改变公共MotionPhase或既有动作表现的选择先交用户裁定，不编造新Fixture。
6. 对当前无业务内容的Corin占位Graph/Profile引用/专用recipe按明确引用范围清理；删除或解绑的实际工作属于后续实施，本轮不动资产。保留通用Host/API/EnsureRoot/公共输出器。
7. 原生UI和C#输出/生成适配当前正式分类和可选根引用，内部引用使用本次对象，按原共享合同保存。源码导出/生成不自动Build。
8. 同步本任务实际完成范围；用户自行端到端验证。tasks只列实现、迁移与文档工作，不列测试、验证、反复Build或回放任务。

### 完成定义

输入职责可从当前正式数据追踪到实际消费者，作者看到的来源与运行使用一致；同一动画变量不在Pose图重新声明，曲线和配置不混成共享变量。没有需求时不强制空图，有需求但缺绑定时明确失败。已决定迁移的计算保持原行为且删除重复生产，尚未决定迁移的公共事实保持原合同。

保留原生事件、typed帧、实例隔离、错误/Reset与C#完整输出/重建能力。这些是系统行为合同，具体端到端验证由用户完成，不成为额外验证任务。Profile引用、空图生成和成功发布Program/Projection都不能被写作“现有动画输入职责已整理完成”。

## Current Spec Comparison

| 当前规范/并行计划 | 交叉或不一致 | 本r3处理 |
|---|---|---|
| 本change原动画事件条款 | 泛化成每个动画实例都必须经过事件宿主，示例易被误当Corin内容要求 | 改成D0按需求装配；删除强制新平滑用例，保留有图时的运行保证 |
| character-presentation-pose-graph现行参数及Fact条款 | 仍有统一参数页和Transition Fact合同；不能因新增EventGraph全面改写事实/曲线 | Pose任务维护其输入分类、Get/条件/静态绑定delta；本任务不重复接管同名条款 |
| refine-pose-graph-readonly-blackboard的完成状态 | 文档要求多来源输入，而当前Blackboard主要只投影EventGraph变量；BuildParameters仍合并旧声明 | 如实列为尚需收口的源码问题，不修改对方完成标记，也不拿任务勾选证明结构已干净 |
| 当前CharacterAnimationInputContract与Runtime | 两处强制要求图，Corin仅有空图 | 本r3明确修改为无需求可不装配、有需求缺失则失败，后续由两侧按D7落实 |
| 当前FactProjector及MotionPhase消费者 | 时间对齐、运动事实和阶段判断在同一类；阶段判断已有正式消费者 | 记录不同职责，不全量搬移；MotionPhase保持当前来源，行为取舍由用户另定 |
| 公共C# authoring / graph-authoring-domain-framework | 两工具、直接API、原生EventGraph与编译Pose边界继续有效 | 保留r2，现行同名共享Requirement归并仍须保留原生EventGraph边界 |
| 旧Document/motor桥、Action/Foot重复变量 | 代码与Corin内容已清理 | 保持删除，不再以它们缺失为理由恢复变量或重复执行迁移 |

本次只改本任务规划，现行主spec、其它任务文档、业务代码和资产没有因此被修改。可选装配与Pose来源统一仍需对应owner修改其正式入口，不能以本提案更新声称代码已完成。

## Open Questions

- MotionPhase当前保持正式Fact；是否将其中已有判断开放给动画作者，需在说明现有全部消费者和行为影响后单独由用户决定。本轮不以该候选阻塞其它来源/装配清理，也不先行迁移。
- Corin占位图的精确删除对象由后续实施依据实际引用确认；已绑定且存在真实事件操作的其它图不按“变量为空”删除。
- 若最终没有需要迁入图的现有作者计算，本轮就不增加Corin业务变量。这不影响通用事件图能力保留，也不构成寻找新效果的任务。

## Workflow Binding

r2曾按用户明确要求下发实现。当前用户要求“改一下文档”，本次只完成r3职责整理与范围修订，不发送执行消息或触发代码工作；已有实现绑定保留，最后明确派发版本仍为r2。

- planning_revision: r3
- revision_date: 2026-09-13
- revision_source: 用户要求整理现有动画输入职责并修改文档
- coordination_proposal: 2026-09-13-eventgraph-authoring-r2
- authoring_baseline: remove-agent-authoring-use-native-csharp/design.md r2
- action: PLAN
- authorization_source: 用户明确要求“改一下文档吧”
- planning_status: revised_for_review
- implementation_dispatched_for_r3: false
- planning_document_owner: 01a095f2-ed45-7502-93f7-e9c9df0b7279
- last_dispatched_implementation_revision: r2@fb937f4415ff6593e1c9affbea37e465c77d617f
- implementation_dispatched_for_r2: true
- implementation_dispatch_status: r2_sent_r3_not_dispatched
- planning_document_paths: 本目录 proposal.md、design.md、tasks.md 和 specs 下四份当前规范增量
- implementation_document_path: D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/execution.md

实现仍只修改获授权代码和自己的 execution.md，不修改规划文件。普通工具、编译、验证、提交和完成结果留在实现窗口；只有实际合同矛盾按既有文档实施协议处理。接口交付通过正式代码与实现记录完成，不是跨窗口发送进度。

```text
IMPLEMENTATION_LINK
planner_thread_id: 01a095f2-ed45-7502-93f7-e9c9df0b7279
implementation_thread_id: 01a09627-3d34-7e61-822f-76aafa68765e
task_title: FlowCanvas事件图
shared_directory: D:/Unity_Project_1/3C
planning_document_paths:
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/proposal.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/design.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/tasks.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/specs/flowcanvas-event-graph/spec.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/specs/character-animation-event-graph/spec.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/specs/graph-authoring-domain-framework/spec.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/specs/character-animation-pipeline/spec.md
implementation_document_path: D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/execution.md
model: gpt-5.6-luna
reasoning_effort: max
```

```text
COORDINATOR_REFERENCE
protocol_version: workflow-coordinator/2
coordinator_thread_id: 01a0962e-319b-7c31-997d-01cd01230536
project_id: local-9736456dbc652f9fbe79876a1464c9fb
```

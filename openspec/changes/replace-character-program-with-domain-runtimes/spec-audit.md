# 规范对账与迁移覆盖

本文件属于本 change 的规划附件。它记录本轮读取的现行要求与增量操作，不能代替正式 spec，也不证明实现已完成。主规范和其它窗口文件本轮不改。

## 2026-09-14实现质量与删除范围复审

本节补充D12历史审查，当前代码快照32f75a37c、实施记录401b52e15；D12中的旧事实只代表当时版本。此次只更新规划，不修改implementation.md、实现代码或其它领域任务。

下表代码路径均相对`3cDemo/Client/3C_Client/Assets/GameScripts/Main/`。

| 编号 | 当前证据 | 判断与处理 |
| --- | --- | --- |
| Q6 | `Runtime/Simulation/Core/Float32/Execution/Float32OperationEvaluator.cs:422`调用Ability目录FromProgram；Float32／Fixed的`Program/*GameplayAbilityExecutionDataFactory.cs:25`传原Manifest.Root，`*GameplayAbilityExecutionData.cs:39`拒绝非Ability；`Editor/CharacterSimulation/Compilation/Semantic/CharacterSemanticFrontendCompiler.cs:94`仍建Character根，Corin生成Program资产m_RootKind仍为1 | 含技能的Character根沿当前目录路径会抛异常；静态可确认，未运行复现。1.10／2.1直接装配独立技能，删除转换，不放宽根检查或伪造根 |
| Q7 | 两个AbilityDataAsset的Load已返回独立类型；Float32／Fixed Loader仍调用旧CharacterSimulationProgramCodec，Factory复制旧操作／常量／布局，执行目录仍从旧Program建立 | 公开返回边界有进展，独立格式／执行消费者仍未完成；1.10保持未完成，不按新增类型或编译通过勾选 |
| Q8 | `Runtime/Character/Pipeline/Runtime/CharacterPipelineDefinition.AbilityProviders.cs`已枚举实际Provider成员，`GameplayAbilityProgramContracts.cs`检查成员／类型／修订；Provider RuntimeHandle当前在合同检查中用于非空判断 | Q3的仅GUID检查已改进；1.9仍需解析结果进入实际执行服务，不能把声明字符串等同于完成运行绑定 |
| Q9 | `Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeGraphRuntime.cs:563`只实现参数与History等有限输出，其它节点报UnsupportedNode；EvaluateFrame／CommitFrame为空 | 原生实例、阶段、端口缓存是中间成果；4.7不能提前完成，内部算法接线和Image链删除仍归Pose清单 |
| Q10 | `Runtime/BTSMTL/Timeline/Runtime/Float32TimelinePlayback.cs`仍有ProgramPlan／CommitFrame；`Editor/CharacterSimulation/Compilation/Skills/BtsmtlSkillTimelineCompiler.cs:28`仍调用SemanticEmitter；Authority Reconciler仍比较ProgramHash／LayoutHash并按Program解码 | Timeline直接内容与网络领域状态接入仍未完成；分别由Timeline清单、核心1.5／1.6／3.x接续，不把Pass保留等同迁移结束 |

目录规模只作为删除负担线索：当次`rg --files -g '*.cs'`配合UTF-8读取和`Measure-Object -Line`统计，Timeline Semantic相关12个文件共2,139非空行；PoseGraph/Program目录28个文件共13,530非空行；整角色Frontend／Builder／BuildService相关4个文件共1,445非空行。这不是净删除承诺：Pose目录含必须保留的算法，共享编译代码含必要技能职责，须按实际消费者切分。不得为达到行数删业务。

新增`character-domain-runtime`的“领域迁移必须同时退出对应旧编译和运行链”要求及两个场景，D16和既有任务同步说明完成条件；未新增测试或手动验收任务，保留所有已完成checkbox。8.2／8.4只是公共残留收口，各领域能退出的旧链随消费者切换删除。Timeline／Pose不复制第二份领域任务清单。

本次对照current specs与project.md后，旧整角色Program、Pose Image唯一运行要求仍与既定方案冲突，继续由既有delta及8.6替换，不能当成已经发布的新现状。`gameplay-simulation-pipeline`的Pass计划编译／能力检查与本轮删除要求不冲突，明确保留；`btsmtl-compiled-simulation-program`增量的技能编译和`native-flowcanvas-pose-runtime`增量的原生图合同保持一致。旧Requirement标题只用于MODIFIED匹配，不代表保留旧类型命名。

本次没有新增代码、编译、Unity运行或资源生成；实现记录报告两个数值核心局部编译通过，不能证明Q6运行路径或完整角色／Timeline／Pose／网络已经可用。

## 对账结论

- 新增 `character-domain-runtime` 与 `native-flowcanvas-pose-runtime`；现行 19 个能力在原路径提供增量。
- MODIFIED 全文从原 Requirement 复制，保留全部原 Scenario，仅调整已变更的角色／技能／Pose 载体与输入输出。
- REMOVED 明确取消整角色编译目录或 Pose 编译阶段；原状态恢复、节点合法性、来源、资源与算法行为由同文件新增要求和两个新能力接管。编译期操作数组、Image seal 与全图 Workspace 优化本身不迁为运行义务。
- 原 Requirement 标题保留是为了精确匹配现行规范，并不要求运行 API 保留旧 Program 命名；设计和任务要求清理旧类型、字段、reader 与作者入口，不建立兼容别名。
- 当前文档仍写“整角色 Program／Projection 必须共同发布”“Pose 只能执行 ProgramImage”，与本次已确认方向存在真实冲突。本 change 的增量用于替换这些要求，不能把当前主规范描述成已经实现本方案。
- `openspec/project.md` 的 Gameplay Client Direction、Presentation Direction、BTSMTL Authoring And Runtime Direction、Diagnostics Direction、Network Boundary 及 Pending Work 中相应现行方向须随实施同步。历史 archive 不改写。
- `character-pose-graph-runtime-architecture` 的动画／IK 基线要求不撤销；变化只限图执行载体与外层存储、调用、绑定。不得把源时间、混合、Constraint公式或Reset修正混入迁移。

## 并行任务对账

完整分工见 design.md“并行工作与冲突边界”。Pose 编辑器的作者分层／原生资源与 Mutation、事件图变量生产、已完成只读输入、技能 FSM／生命周期、Timeline 与算法任务均保留原 owner。本 change 仅替换与角色总 Program／Pose Image 耦合的接口；出现双方正在修改同一公共合同的实际冲突，记录具体路径后由用户裁决。

## 2026-09-13 Timeline直接运行与公共接入补充

本节对应用户关于 Timeline 不再编译的明确规划结论，以及 USER_BROADCAST `camera-preview-timeline-domain-runtime-plan-2026-09-13`。本轮只改本任务已有规划文档，不改代码／资产／implementation.md，不向实现窗口下发新指令，也不回复协调窗口。

### 已确定合同

- Timeline直接调度正式轨道／Clip内容，不生成Timeline IR或Clip operation；技能只保留调用和内容依赖，TreeClip图仍独立编译。普通.NET的一对一内容导出、资源绑定和目标数值准备继续保留，不能换名恢复Compiler。
- 技能、Pose、Camera、Motion各自提供分型准备状态、缺失原因、实际采用版本／实例。预览只消费这些事实；不能维持旧Character Build／ProgramEpoch或计算假全局版本。
- Camera准备／求解／资源规则归Camera任务，本任务只负责角色装配调用和旧Projection挂接迁出。
- 运动源统一使用RootMotionCurveAsset与Timeline唯一时间映射。技能侧以直接Timeline内容消费实际依赖，C# Control／Motion消费正式portable绑定，撤销旧CharacterControlMotionCatalogEmitter依赖。
- 活动技能固定启动时的技能／Timeline／Motion内容版本；Pose显式重建并重置历史；玩法内容或state schema变化按原Session规则重新准备。

### 本轮规范替换

| 位置 | 旧要求或遗漏 | 新口径 |
| --- | --- | --- |
| btsmtl-gameplay-semantic-ir 的 MotionWarp 四项要求 | Clip必须变成TimelineMotionWarp／MotionCurve operation并在IR中绑定 | 直接Timeline字段与稳定Clip引用，内容校验／绑定时闭合，原数值算法和能力拒绝保留；全部原Scenario名称保留 |
| btsmtl-compiled-simulation-program 的Emitter／Modifier／Warp状态 | Timeline内部操作、descriptor与每Clip状态槽 | 只有技能图Emitter；Timeline／Motion Runtime拥有直接内容、实例状态和实际格式版本 |
| character-domain-runtime | 没有可消费的领域准备／采用接口要求 | 新增Timeline直接内容、RootMotionCurveAsset映射、四领域准备／采用、预览事实和生命周期要求 |
| character-animation-pipeline 的Timeline回绕 | Compiled Timeline operation承担loop边界 | 同一正式Timeline Runtime直接遍历内容边界，原回绕场景和输出顺序保留 |
| btsmtl-timeline-editor-preview 的Curve消费与接入 | Motion进入IR且预览没有真实采用结果 | 直接Timeline／Motion内容；Camera由其领域准备，预览消费实际版本与原因 |
| design D10／D11 | 只说“工厂／绑定”但未说明输入输出与现状 | 明确四类请求／准备／采用结果，列出已存在的Compile／Target／Store方法和尚未交付边界 |
| tasks | 1.1—1.3可能被误认为全部角色运行可用 | 保留勾选事实，新增直接Timeline与公共接口增量；领域工厂和采用结果仍是未完成项 |

### 与其它任务的对账

`unify-timeline-motion-curve-source` 当前设计仍写 Timeline semantic／ControlMotion catalog → Numeric Program，这一接入方向由D9明确替换；其正式源、Clip、Warp和时间映射算法仍归曲线任务，不由本任务覆盖。character-root-motion-curves／character-motion-semantics等运动领域文档中的旧operation措辞由曲线owner按该合同同步；不得把保留字段和算法解释为仍保留旧发射入口。Camera文档中的总Projection挂接由本任务迁出，但Camera Builder／payload／Timeline.Camera.cs归Camera。ScenePlay协调器和窗口会话迁移归预览，本任务提供接口，不能把旧Preview独立状态实现当成公共工厂已完成。Slate源码归Timeline UI，C#输出／生成接口变更明确依赖原owner。

### 当前实现事实

实现记录中1.1—1.3只交付Ability前端、两个Target入口和store，仍借用旧容器。不存在本规划已经交付四领域工厂、原生Pose／直接Timeline完整运行或预览接入的结论。新规划不会把旧小步标记为失败或回退它们；增量在新增未完成任务中接续，运行结果尚无本轮证明。

## 2026-09-14实现质量审查

本节由规划窗口按用户要求记录；源码审查快照为HEAD `50e12e191` 附近。本轮未修改代码、资产或implementation.md，未重跑编译／Unity；实现窗口的历史编译记录仍只代表其报告边界。

| 编号 | 源码证据 | 审查结论 | 任务口径 |
| --- | --- | --- | --- |
| Q1 | Float32／Fixed CharacterDomainRuntimeFactory仍从request.Program／ExecutionLayout创建旧Workspace／Evaluator；Host仍Load整角色Program | 已集中创建职责，但尚未形成最终领域装配 | 2.1重开；保留已有代码和Pass调用口 |
| Q2 | GameplayAbilitySemanticFrontendCompiler声明GameplayEffectAggregate、runtime:rng、handle-allocator、fact-sequence | 技能仍携带角色级状态，接入多技能前必须迁出；不声称当前已发生运行重复 | 1.2保留已实现的前端小步，新增1.8明确状态整改、1.10完成独立数据接口 |
| Q3 | GameplayAbilityProviderContract.RequireBinding只比较Kind／ProviderIdentity | 资产身份相同不证明成员存在、类型或版本兼容 | 1.4描述收窄为已实现身份检查，新增1.9承担真实合同解析 |
| Q4 | GameplayAbilityDataAsset.Load和Fixed对应方法返回CharacterSimulationProgram | 独立Asset wrapper不等于最终技能execution data | 1.10移除返回／消费链对旧容器的依赖 |
| Q5 | Pose初始化仍拒绝FlowCanvas Runtime；Timeline仍用旧发射／Program plan；网络仍锁Program／Layout | 这些主链尚未落地，不以拆分类或小步提交数量推定完成 | 原相关未完成任务保持 |

同时跟随current spec替换Curve Key条款中已退役的Position key场景，完整保留新“源运动曲线不进入Timeline lane”场景及其它未变场景，不恢复源曲线进入Timeline可写lane。

此次修改的是完成口径和明确整改边界，不降低原目标，也不撤销已正确的前端、数值目标、存储、生命周期和静态Motion迁移。implementation.md中的2.1已完成属于实现方此前记录，本轮不改历史日志；tasks.md和design D12记录本次规划审查结论。

本轮在character-domain-runtime增加工厂脱离Program、技能不复制角色级状态、Provider真实合同三项要求与场景。未新增测试代码或验证任务，后续实现仍按原owner边界推进；不向其它窗口发送日常回执或新增执行指令。

## 2026-09-14并行规划归属更新

依据：`docs/coordination-progress.md` 的 `PARALLEL-20260914-DOMAIN-01` 审阅（2801861c1）和用户广播 `parallel-20260914-domain-01-planning-update`。本次只更新本任务已有规划、责任指针与公共合同，不改其它任务文档，不启动实现或发送执行消息。接收方续写Runtime章节不代表其旧的Slate／只读输入工作未完成。

### 唯一清单与旧编号去向

| 原主清单范围 | 现在的唯一责任 | 主清单保留方式 |
| --- | --- | --- |
| 1.5—1.7的Timeline直接内容、portable轨道／Clip与域内调度 | `restyle-timeline-editor-slate-style/tasks.md` Runtime接收章节 | 1.5只接Prepare／Create结果；1.6只改共享技能调用入口；原1.7从本清单移出 |
| 3.8的Timeline私有播放状态 | Timeline接收清单 | 3.8只把领域typed状态和Commit／Discard接入角色／网络事务 |
| 4.1—4.6、5.1—5.8、7.3、8.3 | `refine-pose-graph-readonly-blackboard/tasks.md` Runtime接收章节 | 原checkbox从本清单移出；核心仅4.7—4.8的表现外壳调用与事实汇集 |
| 6.1、7.2、7.6、7.7、8.1—8.2中混合的领域职责 | 对应既有Pose／Timeline／预览／C#／资源owner | 文字收窄为核心公共接入，不重复节点、播放器、导出或资产迁移清单 |

迁出的17个checkbox在本次读取时均未完成，移出不计为完成。主清单现有已完成事项原样保留；未把领域接收章节的编写过程当成主实现阻塞或授权其代做。领域详细步骤和勾选只保留在上述对应文档，由各接收规划维护。

### 协调审阅项处理

| 审阅项 | 本轮修改 |
| --- | --- |
| R1 接收范围 | D13明确已有Timeline／Pose配对、唯一清单、核心保留边界；主tasks移除重复领域实现项 |
| R2 共享技能Timeline入口 | BtsmtlSkillTimelineCompiler只由核心写入，Timeline不复制TreeClip图编译或共改方法 |
| R3 非Skill独立准备 | D10新增Timeline行，D14固定独立PrepareContent／CreatePlayback；不通过伪造Ability准备 |
| R4 提交／丢弃与状态 | Advance只返回Pending；Timeline检查并安装自身状态，核心决定整Step提交／丢弃和快照组合；私有字段不复制，TreeClip图执行帧仍归核心技能服务 |
| R5 Pose与角色外壳 | D15划定Pose两阶段／私有状态和核心表现外壳；CharacterPresentationRuntime等共享外壳由核心唯一写入 |
| R6 当前实现边界 | D12保留其历史审查语境；Q2相关1.8已完成，不作为仍待删的旧位置重复派工。最终工厂／独立数据／Provider按当前tasks推进 |
| R7 采用事实发布者 | 各领域owner确认和发布实际版本／实例；核心只装配与汇集，不能从请求或Ready推断已采用 |
| R8 历史路径与正确算法 | 已归档运动源迁移不重做，读取现行规范和API；Pose接当前Foot／IK正式接口，不以保留旧Constraint冻结另一任务已批准算法 |

现行规范中的整个Program、Timeline operation和PoseImage运行要求仍按本change已有delta替代；此次分工不改变已批准运行方向。新增的规范只约束跨领域准备、原子状态、私有状态所有权和实际采用事实，不把一个领域的实现细节变成另一份核心任务清单。

## 增量清单

| 能力 | 操作 | 原Requirement | 保留原场景数 | 迁移说明 |
| --- | --- | --- | --- | --- |
| gameplay-simulation-session-composition | MODIFIED | SimulationSessionHost 必须是 Unity Session Composition 的唯一 owner | 2 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| gameplay-simulation-session-composition | MODIFIED | Session Composition 必须显式选择五个组成部分 | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| gameplay-simulation-session-composition | MODIFIED | Program Runtime 与 Execution Backend 必须是独立选择维度 | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| gameplay-simulation-session-composition | MODIFIED | Session Source 必须通过 Preparation 产生完整 Launch Plan | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| gameplay-simulation-session-composition | MODIFIED | Session Host 必须使用 Numeric-Neutral Runtime Handle | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| gameplay-simulation-session-composition | MODIFIED | Target-specific Composer 必须唯一创建完整 Runtime | 3 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| gameplay-simulation-session-composition | MODIFIED | Actor Registration 必须在 Active 前形成不可变 roster | 2 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| gameplay-simulation-session-composition | MODIFIED | Session Host 必须按正式 Tick 生命周期推进 Preparation 与 Runtime | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| gameplay-simulation-session-composition | MODIFIED | Session Composition 必须锁定完整身份与真实 capability | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| gameplay-simulation-session-composition | MODIFIED | Composition必须校验Body Motion与Solver垂直能力 | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| gameplay-simulation-session-composition | MODIFIED | Local Launch Plan必须锁定Control Source与Observation能力 | 2 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| gameplay-simulation-pipeline | MODIFIED | Pipeline 必须以四阶段和固定 Commit 边界执行 | 2 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| gameplay-simulation-pipeline | MODIFIED | Pipeline Compiler 必须在 Active 前完成完整兼容校验 | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| gameplay-simulation-pipeline | MODIFIED | Program、Pipeline 与 Backend 身份必须相互独立并共同锁定 | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| gameplay-simulation-pipeline | MODIFIED | 普通扩展必须使用 Pass，完整执行技术替换必须使用 Backend | 2 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| gameplay-simulation-pipeline | MODIFIED | Standard Local Pipeline 必须保持唯一正式单机执行链 | 2 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| server-authoritative-hybrid-sync-model | MODIFIED | ServerAuthoritativeHybrid 必须是完整独立的 Network Model | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| server-authoritative-hybrid-sync-model | MODIFIED | 模型策略必须集中在模型 Definition 与 Pass 配置 | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| server-authoritative-hybrid-sync-model | MODIFIED | ServerAuthoritative 权威运动必须拥有独立模拟后端 | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| server-authoritative-hybrid-sync-model | MODIFIED | ServerAuthoritative 模型缺少正式 Source 或 Pipeline 时必须不可用 | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| server-authoritative-hybrid-sync-model | MODIFIED | ServerAuthoritative 握手必须锁定 Program 与 Pipeline 兼容 Pair | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| server-authoritative-hybrid-sync-model | MODIFIED | ServerAuthoritative 模型必须显式区分Gate Room与Authority Host | 2 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| server-authoritative-prediction-correction-pipeline | MODIFIED | Prediction History必须是正式SnapshotParticipant | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| server-authoritative-prediction-correction-pipeline | MODIFIED | Authority Baseline必须覆盖完整Owner Gameplay恢复状态 | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| server-authoritative-prediction-correction-pipeline | MODIFIED | Authority Pipeline必须独立执行Canonical Gameplay | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| server-authoritative-prediction-correction-pipeline | MODIFIED | Prediction跨模块转换必须原子提交 | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| server-authoritative-prediction-correction-pipeline | MODIFIED | Remote Actor必须保持非Program观察体边界 | 2 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| server-authoritative-host-portability | MODIFIED | Authority Source Runtime必须Host-Neutral | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| server-authoritative-host-portability | MODIFIED | Authority Host必须通过唯一Launch Request调用Portable Composer | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| server-authoritative-host-portability | MODIFIED | 具体Authority Host Profile必须由Host Product拥有 | 3 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| deterministic-rollback-network-model | MODIFIED | Rollback Model 必须严格校验 Deterministic Capability | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| deterministic-rollback-network-model | MODIFIED | Gameplay 输入必须沿单一 Raw-to-Canonical 生命周期传播 | 6 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| deterministic-rollback-network-model | MODIFIED | Rollback History 必须保存完整 Fixed SimulationWorldSnapshot | 2 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| deterministic-rollback-network-model | MODIFIED | Late Input 必须触发原子 Restore 与 Replay | 2 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| deterministic-rollback-network-model | MODIFIED | State Hash 必须支持分层 Desync 定位 | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| deterministic-rollback-network-model | MODIFIED | DeterministicRollback Relay Server必须保持Relay-only DS职责 | 1 | 迁移角色内容与状态身份；全部原场景保留，网络策略／Pass／Solver不变 |
| btsmtl-gameplay-semantic-ir | MODIFIED | Character Authoring 必须先编译为 Numeric-Neutral Semantic IR | 2 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-gameplay-semantic-ir | MODIFIED | Semantic IR 不得成为第二个 Runtime Interpreter | 2 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-gameplay-semantic-ir | MODIFIED | Semantic Identity 与 Target Artifact 必须可追溯 | 2 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-gameplay-semantic-ir | MODIFIED | Semantic IR Artifact 必须原子生成并可由普通 DotNet 读取 | 2 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-gameplay-semantic-ir | REMOVED | Semantic IR必须表达Character composition roots | 明确撤销载体 | 明确取消全角色编译载体；行为由新增领域运行合同接管 |
| btsmtl-gameplay-semantic-ir | REMOVED | Semantic IR必须使用numeric-neutral Equipment schema | 明确撤销载体 | 明确取消全角色编译载体；行为由新增领域运行合同接管 |
| btsmtl-compiled-simulation-program | MODIFIED | Projection Foot Analysis必须拥有独立规范身份 | 1 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-compiled-simulation-program | MODIFIED | Projection不得保存原始动画采样快照 | 1 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-compiled-simulation-program | MODIFIED | Character authoring 必须按显式 Numeric Target 生成 Simulation Program | 2 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-compiled-simulation-program | MODIFIED | Program 必须是不可变 portable 数据 | 1 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-compiled-simulation-program | MODIFIED | Authoring type 必须通过唯一 Emitter 生成 Operation | 1 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-compiled-simulation-program | REMOVED | Program 必须声明完整 Character State Layout | 明确撤销载体 | 明确取消全角色编译载体；行为由新增领域运行合同接管 |
| btsmtl-compiled-simulation-program | MODIFIED | Program 必须声明唯一 Numeric Target ABI | 1 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-compiled-simulation-program | MODIFIED | Program bytes 与 ProgramHash 必须稳定 | 1 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-compiled-simulation-program | MODIFIED | Program Artifact 必须与 Source Revision 严格对齐 | 1 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-compiled-simulation-program | REMOVED | Presentation Projection 必须与 Gameplay Numeric Target 分离 | 明确撤销载体 | 明确取消全角色编译载体；行为由新增领域运行合同接管 |
| btsmtl-compiled-simulation-program | REMOVED | Session ProgramCatalog 必须不可变且支持每 Actor 显式绑定 | 明确撤销载体 | 明确取消全角色编译载体；行为由新增领域运行合同接管 |
| btsmtl-compiled-simulation-program | REMOVED | Program 与 Projection 必须在同一 Build Transaction 中发布 | 明确撤销载体 | 明确取消全角色编译载体；行为由新增领域运行合同接管 |
| btsmtl-compiled-simulation-program | MODIFIED | Compiler Diagnostics 与 Editor 作者入口必须复用正式 Frontend 和 Target 阶段 | 1 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-compiled-simulation-program | MODIFIED | Target Program 必须作为正式独立 Artifact 原子发布 | 2 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-compiled-simulation-program | MODIFIED | Program Identity 与 Session Pipeline Identity 必须分离 | 1 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-compiled-simulation-program | MODIFIED | Target Program必须以结构化Binding保存Constant Value输入 | 4 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-compiled-simulation-program | MODIFIED | Program 必须声明 Motion Modifier descriptor 与固定顺序 | 1 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-compiled-simulation-program | MODIFIED | MotionWarp 跨 Tick 数据必须进入 Character State Layout | 1 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-compiled-simulation-program | REMOVED | Program 必须声明Body Motion descriptor与能力身份 | 明确撤销载体 | 明确取消全角色编译载体；行为由新增领域运行合同接管 |
| btsmtl-compiled-simulation-program | MODIFIED | MotionWarp 版本变化必须拒绝旧 Artifact | 1 | 保留全部原场景，处理范围迁为独立技能／领域资源 |
| btsmtl-compiled-simulation-program | REMOVED | Compiled Program必须包含不可变Equipment catalog和layout | 明确撤销载体 | 明确取消全角色编译载体；行为由新增领域运行合同接管 |
| btsmtl-compiled-simulation-program | REMOVED | Program identity必须覆盖Equipment authoring真相 | 明确撤销载体 | 明确取消全角色编译载体；行为由新增领域运行合同接管 |
| btsmtl-compiled-simulation-program | REMOVED | Program Execution Layout必须预构建Equipment索引 | 明确撤销载体 | 明确取消全角色编译载体；行为由新增领域运行合同接管 |
| character-pipeline-definition-authoring | MODIFIED | CharacterPipelineDefinition 必须是配置装配根 | 3 | 原场景完整保留；按领域改写作者入口与运行owner |
| character-pipeline-definition-authoring | MODIFIED | Definition Inspector 必须分离作者配置与生成产物 | 6 | 原场景完整保留；按领域改写作者入口与运行owner |
| character-pipeline-definition-authoring | MODIFIED | Animation Presentation Profile 必须是唯一表现配置资产 | 3 | 原场景完整保留；按领域改写作者入口与运行owner |
| character-pipeline-definition-authoring | MODIFIED | Body Motion Profile 必须是唯一垂直动力作者配置 | 1 | 原场景完整保留；按领域改写作者入口与运行owner |
| character-pipeline-definition-authoring | MODIFIED | Character Definition 必须通过两个配置引用安装Equipment能力 | 2 | 原场景完整保留；按领域改写作者入口与运行owner |
| character-state-timeline-authoring-loop | MODIFIED | Corin Skill Graph必须表达角色Gameplay流程层 | 1 | 原场景完整保留；按领域改写作者入口与运行owner |
| character-state-timeline-authoring-loop | MODIFIED | Corin Locomotion StateMachine必须只控制Gameplay运动 | 2 | 原场景完整保留；按领域改写作者入口与运行owner |
| character-state-timeline-authoring-loop | MODIFIED | Corin一次性状态行为必须默认使用inline Graph | 1 | 原场景完整保留；按领域改写作者入口与运行owner |
| graph-authoring-domain-framework | MODIFIED | Authoring节点与Runtime执行描述必须分离 | 1 | 原场景完整保留；按领域改写作者入口与运行owner |
| character-pose-plan-compilation | REMOVED | 每种Pose节点必须只有一个Node Definition真相 | 明确撤销载体 | 固定Compiler阶段/数据结构被明确撤销；业务校验迁入原生图新增要求 |
| character-pose-plan-compilation | REMOVED | Node Definition与全局Topology规则必须分离 | 明确撤销载体 | 固定Compiler阶段/数据结构被明确撤销；业务校验迁入原生图新增要求 |
| character-pose-plan-compilation | REMOVED | Pose Compiler必须使用固定不可逆Pass链 | 明确撤销载体 | 固定Compiler阶段/数据结构被明确撤销；业务校验迁入原生图新增要求 |
| character-pose-plan-compilation | REMOVED | Graph Closure Pass必须唯一展开全部可达图 | 明确撤销载体 | 固定Compiler阶段/数据结构被明确撤销；业务校验迁入原生图新增要求 |
| character-pose-plan-compilation | REMOVED | Typed Lowering Pass必须只通过Node Definition生成IR | 明确撤销载体 | 固定Compiler阶段/数据结构被明确撤销；业务校验迁入原生图新增要求 |
| character-pose-plan-compilation | REMOVED | Topology Pass必须统一证明全局执行闭包 | 明确撤销载体 | 固定Compiler阶段/数据结构被明确撤销；业务校验迁入原生图新增要求 |
| character-pose-plan-compilation | REMOVED | Value与Workspace必须由独立Pass按类型和寿命规划 | 明确撤销载体 | 固定Compiler阶段/数据结构被明确撤销；业务校验迁入原生图新增要求 |
| character-pose-plan-compilation | REMOVED | Operation必须使用公共Header与分段Family Payload | 明确撤销载体 | 固定Compiler阶段/数据结构被明确撤销；业务校验迁入原生图新增要求 |
| character-pose-plan-compilation | REMOVED | 全部现行Operation必须进入唯一Family迁移表 | 明确撤销载体 | 固定Compiler阶段/数据结构被明确撤销；业务校验迁入原生图新增要求 |
| character-pose-plan-compilation | REMOVED | Stage Schedule必须由typed依赖和Execution Domain唯一生成 | 明确撤销载体 | 固定Compiler阶段/数据结构被明确撤销；业务校验迁入原生图新增要求 |
| character-pose-plan-compilation | REMOVED | Program Image必须在Seal后不可变且自描述完整 | 明确撤销载体 | 固定Compiler阶段/数据结构被明确撤销；业务校验迁入原生图新增要求 |
| character-pose-plan-compilation | REMOVED | Compiler Diagnostic必须保留稳定source lineage | 明确撤销载体 | 固定Compiler阶段/数据结构被明确撤销；业务校验迁入原生图新增要求 |
| character-pose-plan-compilation | REMOVED | 新Program ABI必须破坏性替换旧Projection | 明确撤销载体 | 固定Compiler阶段/数据结构被明确撤销；业务校验迁入原生图新增要求 |
| character-pose-graph-runtime-architecture | MODIFIED | 动画表现根必须只编排typed Pose Frame结果 | 2 | 保留全部原场景与算法owner；图执行和产物改为原生实例 |
| character-pose-graph-runtime-architecture | MODIFIED | Pose运行必须形成四个唯一业务Owner | 2 | 保留全部原场景与算法owner；图执行和产物改为原生实例 |
| character-pose-graph-runtime-architecture | MODIFIED | Program Image、Execution View、Actor State、Owned Frame页与根Transaction必须完全分型 | 2 | 保留全部原场景与算法owner；图执行和产物改为原生实例 |
| character-pose-graph-runtime-architecture | MODIFIED | Frame数据必须通过唯一写入页和typed只读View单向流动 | 2 | 保留全部原场景与算法owner；图执行和产物改为原生实例 |
| character-pose-graph-runtime-architecture | MODIFIED | 校验必须按输入边界唯一分责 | 1 | 保留全部原场景与算法owner；图执行和产物改为原生实例 |
| character-pose-graph-runtime-architecture | MODIFIED | Pose Program Runtime必须是唯一Operation执行Owner | 2 | 保留全部原场景与算法owner；图执行和产物改为原生实例 |
| character-pose-graph-runtime-architecture | MODIFIED | Source Module必须独占物理source与release生命周期 | 2 | 保留全部原场景与算法owner；图执行和产物改为原生实例 |
| character-pose-graph-runtime-architecture | MODIFIED | Constraint Module Interface不得泄露Program布局 | 2 | 保留全部原场景与算法owner；图执行和产物改为原生实例 |
| character-pose-graph-runtime-architecture | MODIFIED | Final Pose Publication必须原子拥有最终结果与Physical写入 | 2 | 保留全部原场景与算法owner；图执行和产物改为原生实例 |
| character-pose-graph-runtime-architecture | MODIFIED | 所有Module必须服从唯一表现帧事务和Barrier | 2 | 保留全部原场景与算法owner；图执行和产物改为原生实例 |
| character-pose-graph-runtime-architecture | MODIFIED | 在线调参必须使用actor-local原子Snapshot | 2 | 保留全部原场景与算法owner；图执行和产物改为原生实例 |
| character-pose-graph-runtime-architecture | MODIFIED | Diagnostics必须只投影Committed typed Result | 3 | 保留全部原场景与算法owner；图执行和产物改为原生实例 |
| character-pose-graph-runtime-architecture | MODIFIED | Preview与正式Runtime必须复用同一Module Factory和Program Image | 1 | 保留全部原场景与算法owner；图执行和产物改为原生实例 |
| character-pose-graph-runtime-architecture | MODIFIED | Reset、Replacement与Dispose必须按Owner清理状态 | 1 | 保留全部原场景与算法owner；图执行和产物改为原生实例 |
| character-presentation-pose-graph | MODIFIED | Pose Graph运行输入必须通过显式Character Binding装配 | 3 | 保留全部原场景；运行绑定和原生图接管编译载体，动画算法不变 |
| character-presentation-pose-graph | MODIFIED | Pose Graph必须唯一表达完整表现拓扑 | 1 | 保留全部原场景；运行绑定和原生图接管编译载体，动画算法不变 |
| character-presentation-pose-graph | MODIFIED | Pose端口必须显式区分空间并允许typed控制目标 | 1 | 保留全部原场景；运行绑定和原生图接管编译载体，动画算法不变 |
| character-presentation-pose-graph | MODIFIED | Pose Plan必须按拓扑编译为有序执行阶段 | 3 | 保留全部原场景；运行绑定和原生图接管编译载体，动画算法不变 |
| character-presentation-pose-graph | MODIFIED | PoseStateMachine必须是纯表现状态机 | 2 | 保留全部原场景；运行绑定和原生图接管编译载体，动画算法不变 |
| character-presentation-pose-graph | MODIFIED | State inline graph必须存入root-owned flat graph catalog | 2 | 保留全部原场景；运行绑定和原生图接管编译载体，动画算法不变 |
| character-presentation-pose-graph | MODIFIED | State-local source必须由Profile binding和provider解析 | 2 | 保留全部原场景；运行绑定和原生图接管编译载体，动画算法不变 |
| character-presentation-pose-graph | MODIFIED | Pose State transition必须显式编译Routing并从source binding推导同步 | 1 | 保留全部原场景；运行绑定和原生图接管编译载体，动画算法不变 |
| character-presentation-pose-graph | MODIFIED | Pose节点必须显式处理可用性和局部连续性 | 2 | 保留全部原场景；运行绑定和原生图接管编译载体，动画算法不变 |
| character-presentation-pose-graph | MODIFIED | Pose参数必须通过typed页面和显式解析传播 | 1 | 保留全部原场景；运行绑定和原生图接管编译载体，动画算法不变 |
| character-presentation-pose-graph | MODIFIED | Rig、Mask和Pose运输必须使用Rig v4 | 3 | 保留全部原场景；运行绑定和原生图接管编译载体，动画算法不变 |
| character-presentation-pose-graph | MODIFIED | Pose Graph工作区必须准确映射Authoring、Live与References | 2 | 保留全部原场景；运行绑定和原生图接管编译载体，动画算法不变 |
| character-presentation-pose-graph | MODIFIED | Pose Watch必须只观察已完成Pose与typed目标Value | 3 | 保留全部原场景；运行绑定和原生图接管编译载体，动画算法不变 |
| character-presentation-pose-graph | MODIFIED | Preview、Runtime与Live Debug必须复用同一固定Pose Plan | 2 | 保留全部原场景；运行绑定和原生图接管编译载体，动画算法不变 |
| character-presentation-pose-graph | MODIFIED | Pose authoring必须通过正式Capability和Presentation Mutation | 2 | 保留全部原场景；运行绑定和原生图接管编译载体，动画算法不变 |
| character-presentation-pose-graph | MODIFIED | Pose Graph作者资产必须与Character Presentation Binding分离 | 1 | 保留全部原场景；运行绑定和原生图接管编译载体，动画算法不变 |
| character-presentation-pose-graph | MODIFIED | Pose StateMachine layout必须是独立纯作者数据 | 3 | 保留全部原场景；运行绑定和原生图接管编译载体，动画算法不变 |
| character-animation-pipeline | MODIFIED | CharacterSimulationPresentationRuntime必须执行唯一编译Pose Plan | 3 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-pipeline | MODIFIED | 不得恢复Timeline或Preview分裂路径 | 1 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-pipeline | MODIFIED | 有限Action readiness必须来自第一份合法Sample | 1 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-pipeline | MODIFIED | 动画表现帧必须使用预分配暂存事务 | 5 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-pipeline | MODIFIED | Dense状态与稀疏生命周期必须使用不同暂存策略 | 4 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-pipeline | MODIFIED | Animancer Evaluate必须是唯一不可逆提交门槛 | 5 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-pipeline | MODIFIED | Final Pose写入必须在整Rig验证后原子选择Committed或Pending结果 | 3 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-pipeline | MODIFIED | Physical Source资源生命周期必须延迟提交 | 2 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-layer-runtime | MODIFIED | 持续Pose与有限Action控制边界必须分离 | 3 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-layer-runtime | MODIFIED | 基础Pose必须由正式state-local source输出 | 1 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-layer-runtime | MODIFIED | 动画帧必须按固定职责顺序执行 | 1 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-layer-runtime | MODIFIED | PoseState source必须按provider demand和state relevance管理 | 1 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-layer-runtime | MODIFIED | 每类连续性必须只有一个明确owner | 2 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-layer-runtime | MODIFIED | Finite与Cyclic source时间必须保持明确拓扑 | 2 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-layer-runtime | MODIFIED | Source backend必须只负责采样和物理资源释放 | 1 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-layer-runtime | MODIFIED | Float32与Fixed必须共享同一Presentation Projection | 1 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-layer-runtime | MODIFIED | Runtime、Preview和Live Debug必须使用同一事实源 | 1 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-layer-runtime | MODIFIED | Locomotion Phase映射必须编入source-local计划 | 1 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-animation-layer-runtime | MODIFIED | Locomotion Phase relation必须服从Transition generation与Player continuation | 2 | 原场景完整保留；替换Pose载体与来源身份，保留播放／IK／帧事务 |
| character-simulation-kernel | MODIFIED | SimulationKernel 必须分离 Evaluate 与 Finalize | 1 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| character-simulation-kernel | MODIFIED | Character State 必须通过单一 Target Transaction推进 | 2 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| character-simulation-kernel | MODIFIED | Committed Character State 必须使用类型化不可变存储 | 1 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| character-simulation-kernel | MODIFIED | Simulation Session 必须锁定 ProgramCatalog 与 Actor roster | 1 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| character-simulation-kernel | MODIFIED | SimulationWorldSnapshot 必须原子 Capture 与 Restore | 1 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| character-simulation-kernel | MODIFIED | State Hash 必须区分 Character 与 World 有效性 | 1 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| character-simulation-kernel | MODIFIED | Simulation Session 必须锁定完整 Numeric Target 组合 | 1 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| character-simulation-kernel | MODIFIED | Execution Backend 必须按 Pipeline 事务原子推进零到多个 Step | 3 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| character-simulation-kernel | MODIFIED | Operation topology 必须是 Program 的一次性只读运行索引 | 2 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| character-simulation-kernel | MODIFIED | Program 级执行服务不得每 Tick 重建 | 3 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| character-simulation-kernel | MODIFIED | ProgramExecutionLayout必须预解析Tick热路径静态查询 | 3 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| character-simulation-kernel | MODIFIED | Kernel Program Binding必须与共享Program Layout分离 | 2 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| character-simulation-kernel | MODIFIED | Evaluate与Finalize必须通过唯一Actor Output Lease冻结结果 | 1 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| btsmtl-semantic-ir-inspection | MODIFIED | Unity Editor 必须提供只读 Semantic IR Inspector | 2 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| btsmtl-semantic-ir-inspection | MODIFIED | 普通 DotNet Reader 必须显式读取 Semantic IR 与 Program Artifact | 2 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| btsmtl-semantic-ir-inspection | MODIFIED | Semantic IR与Target Program检查工具必须展示结构化Value输入 | 3 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| btsmtl-timeline-editor-preview | MODIFIED | Timeline 编辑器预览目标来自正式管线预览目标 | 2 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| btsmtl-timeline-editor-preview | MODIFIED | Timeline preview session 必须隔离动画生命周期状态 | 4 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| btsmtl-timeline-editor-preview | MODIFIED | Timeline Preview 必须按正式阶段展示 TreeClip | 3 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| btsmtl-timeline-editor-preview | MODIFIED | Timeline、Track 和 Clip 必须拥有稳定 authoring identity | 3 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| btsmtl-timeline-editor-preview | MODIFIED | 预览采样必须复用正式动画Selection与Pose Plan | 4 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| btsmtl-timeline-editor-preview | MODIFIED | Curve Key编辑必须无损且原子 | 4 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |
| btsmtl-timeline-editor-preview | MODIFIED | Curve Editor必须保持领域运行链唯一 | 2 | 原场景完整保留；领域运行／技能检查／原生Pose接管旧载体 |

## 尚未得到运行证明的边界

本轮没有编译、Play、资源生成或回放。原生 Pose 的 CPU／内存、共享分支重复求值、动画过渡与 IK 历史、跨节点 Job 取消后的性能，以及网络完整恢复结果均未得到本 change 的运行证明。它们不作为待用户补答的设计选择：设计已固定原生图、保留算法与原网络行为；实施中不得为通过结果比较修改算法或回退旧程序。

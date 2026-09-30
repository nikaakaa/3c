# 角色底层重构总方案与长任务排查入口

## Context

更新：2026-09-29。状态：完整实施及规格同步已收口；29 项实施任务的当前证据见文末“实施收口与完成核对”。下方方案和“本轮排查结果”保留排查当时事实，不再代表未实施状态。运行验收与性能测量的限制单独记录。动机与能力增量见 [proposal.md](proposal.md)。

当前架构以 [project.md](../../../project.md) 与现行 specs 为准；本文件记录增量方案、证据和偏差。不得把 archive 或旧评估中的目录、行数和状态当作当前事实。路径默认相对仓库根，源码入口的共同前缀为 `3cDemo/Client/3C_Client/Assets/GameScripts/Main/`。

### 排查开始时的基线快照（历史记录）

| 编号 | 范围 | 2026-09-29 核对到的状态 | 后续性质 |
| --- | --- | --- | --- |
| R1 | Pose 整帧事务及失败处理 | `4f6a4cd6e` 已收拢 FrameCoordinator；后续其他提交也修改了 Pose，必须以当前代码复核 | 已实施基线对账与剩余缺口审计，不重做一套事务 |
| R2 | Timeline Host 职责 | 内容、表现图、快照转换、诊断已分别提交；`b7537a19b` 又接通 Timeline 到已编译技能图的正式 binding | 核对唯一状态、实际调用与文档，不回退新 binding |
| R3 | Session 检查点与历史恢复 | Host 修改、History 新文件及 meta 尚在工作区，未提交 | 收尾候选，不视为已验证交付 |
| R4 | Foot 动画采样与分析 | Analyzer 修改、Sampler 新文件及 meta 尚在工作区，未提交 | 收尾候选；此前编译报错修正后未由本任务重新确认最终结果 |
| R5 | Pose 作者工作区 | 已只读排查，未实施职责迁移 | 明确问题及初步方案，仍需完整调用链确认 |
| R6 | TreeDesigner 残留 | 公共作者合同迁出已归档；旧 Skill／Timeline 分支已清理；Character 仍有旧类型编译依赖 | 完整引用与序列化审计后确定删除集 |
| R7 | 多类型大文件 | TimelineRuntimePreparation、FootSwingMotionBuilder 仍混放多个已有职责 | 文件组织；不能冒充算法或性能优化 |
| R8 | BlendStack 及模块保留 | 已看到混合、打断历史和退休状态共同提交的理由 | 输出保留结论，发现具体独立职责才追加方案 |
| R9 | 闭环、性能与 0 GC 证据 | 其他窗口已有一次有效 Player Capture；没有本轮全部改动的运行或分配证明 | 证据和专项交接；不复制性能工具、不自动运行 replay |
| R10 | 规格、计划、完成口径 | 一般重构说明曾把小步完成写成整体完成；现行 spec 存在局部旧结构表述 | 对账并消除冲突；不改写历史归档 |

已提交参考：`507c1a3ec` 清旧 Skill 分支；`f19c3bb50` 内容模块；`685981d8d` 表现图；`c9cc2932c` 快照／诊断；`e1b9802fc` 旧 Timeline 链。提交只说明曾做过什么，长任务必须重新核对当前 HEAD 和工作区，不能据此判断行为通过。

## Goals / Non-Goals

### 目标

- 每个业务输入、输出、可变状态、校验、提交和释放点都有唯一明确 owner；调用方不必理解被调模块全部内部阶段。
- 对本清单所有条目形成有证据的“已完成／实施／保留／转交专项／等待业务决策”结论，避免只做前几项便称全部完成。
- 删除已经被正式链替代的代码、字段、状态、生成入口和依赖；保存需要的业务能力、资产身份、数值顺序与运行时预分配策略。
- 已依据用户后续完整实施指令执行全部 tasks；排查与方案只是输入，不能替代实施完成。

### 排除范围

- 不恢复 Pose IR、整角色 Program／Projection、独立 TreeRunner、Timeline IR、第二 Preview Session 或备用执行路径。
- 不改变 Foot／IK／Blend 数值算法、角色配置、动画时钟与输入回放语义；发现业务问题列出同级方案及成本，不借重构擅自改行为。
- 不因为文件大就拆接口、增加 Manager、分多个 partial 或搬成薄转发；不为假设变化点新增抽象。
- 用户已明确创建完整实施 goal；本次使用目标 Editor 做正式编译检查，不运行 replay、Player 或采集，不新增／修改测试，不创建 worktree。
- 不将整个性能、相机复刻、网络、AI、Foot 稳定化项目吸收入本 change；它们仅在实际交叉处进入证据与依赖清单。

## Decisions

### R1：Pose 帧的整体结果由一个协调点决定

入口：`Runtime/Character/Pipeline/Presentation/CharacterPresentationDomainRuntime.cs`，`Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeFrameCoordinator.cs`、`CharacterPoseNativeRoleRuntime.cs`、`CharacterPoseNativeGraphRuntime.cs`、`CharacterPoseNativeDomainContracts.cs`。

输入是同一角色表现帧的 committed 事实、动作命令、参数与准备好的资源。Presentation 交给 DomainSession 的完整帧入口，FrameCoordinator 决定 Prepare、Evaluate、Validate、Commit／Discard 的顺序及最终结果，再交还外层处理表现。Source 管采样来源和租约；Constraint 管 Foot／IK 的 Pending／Committed 历史；FinalPublication 管结果页和骨骼写入；Graph 管图及子图执行状态。

长任务沿成功、Pending、开帧失败、求值失败、发布失败、释放失败逐一记录：哪些状态已经改变、谁关闭租约、谁能确认动作、谁能推进时钟、哪个步骤不可逆。检查已修复的“发布失败仍推进约束／来源”是否在当前链全部消除；不能只检索一个 if，也不能把图的局部阶段状态当作重复整帧状态删除。

方案取舍：集中整体成败可减少调用方知识；把所有 Source／Constraint／Publication 合成一个大运行器会丢失资源和状态归属，因此保留模块分工。Barrier 前丢弃 Pending；Animancer 求值开始后发生失败保留故障上下文并拒绝后续帧，不宣称物理回滚。新发现的故障语义变化必须单列决定。

### R2：Timeline 内容、播放、表现执行与观察各有归属

入口：`Runtime/Character/Pipeline/Animation/Lifecycle/CharacterTimelineHost.cs`、`CharacterTimelineContentStore.cs`、`CharacterTimelinePresentationGraphRuntime.cs`、`CharacterTimelineSnapshotCodec.cs`、`CharacterTimelinePlaybackDiagnostics.cs`、`CharacterTimelineGraphBindings.cs`，以及 `Runtime/BTSMTL/Timeline/Runtime/`。

作者内容经 ContentStore 准备和采用，Host 唯一登记播放与终态，Timeline 正式实例依照调用方 Step 形成候选；精确图身份经正式 binding 进入已编译 Skill。表现模块消费 Action 时钟及已提交事实，输出 Camera／TreeClip 退出请求；诊断只观察，快照转换不持有第二份播放状态。

核对活动播放冻结内容、generation、开始／跨边界／回绕／停止、恢复、表现 Marker 与退出的输入输出和提交顺序。最新 binding 修复属于基线，不能为了减少依赖恢复旧图参数或空服务。原 Host 文件里尚有多个服务／合同类型，先确认是否只是文件归属问题。

方案取舍：保持已有拆分，并删掉确认无意义的转发；不新增通用 Timeline 控制中心，不让表现模块自主推进逻辑时间。纯文件归位按 R7 执行，不以本条为理由重写播放算法。

### R3：SessionHistory 拥有历史，Host 拥有会话

入口：`Runtime/Character/Pipeline/Unity/SimulationSessionHost.cs` 与工作区 `SimulationSessionHistory.cs`。

Host 接收 Unity 生命周期与用户暂停／恢复操作，持有唯一 runtime、不可变 roster、状态及销毁顺序；History 接收已绑定 runtime、roster 和已完成 Tick，持有检查点集合、裁剪缓冲、历史分支与恢复事务。恢复结果返回 Host，不另建会话或模拟时钟。

排查 Capture、精确恢复、恢复到目标 Tick、输入区间重放、恢复失败回滚和新生命周期。比较提取前后的适用条件、错误优先级、检查点兼容性、暂停入口及释放时序；兼容性由接收恢复请求的责任点确认一次，不在内部恢复助手中重复验证。记录现有检查点捕获分配与每帧热路径的区别，不能为避免初始化分配增加复杂状态。

方案取舍：普通 C# 模块承载完整历史事务，比继续嵌在 MonoBehaviour 更容易审查；不能复制 Active／Paused／roster 状态来实现所谓独立 History runtime。现有回滚属于实际业务协议，不能以“清防御代码”为由删掉失败原子性。

### R4：Foot 采样模块只提供规范样本

入口：`Editor/CharacterSimulation/Analysis/FootPlacement/CharacterFootPlacementAnimationAnalyzer.cs` 与工作区 `CharacterFootPlacementAnimationSampler.cs`；相关正式构建入口和 Analysis Source 为追踪起点。

输入是 Target／Motion Reference Clip、Rig、Sampling Rig、Calibration 和采样配置；Sampler 管预览场景、实例、PlayableGraph、NativeArray、Clip clone 与释放，输出规范骨骼／Root 样本数组。Analyzer 基于同一批样本计算特征、曲线、几何结果及 Motion Data，交给已有 artifact／作者采用链。

核对采样时间、末端／loop 行为、参考动画与目标动画的先后、骨骼空间、有限值检查、所有权与成功／异常释放。采样完再分析，避免为拆层复制大数组。已搬出的算法必须与原实现逐段对账；共享数据结构不能藏第二份曲线或缓存。

方案取舍：分开 Unity 环境和数值计算有真实生命周期收益；每个公式各拆一个接口只增加理解成本。这里不调整 Foot 算法、采样率、Calibration 或素材配对规则。发现旧 spec 的 Projection 表述时按 R10 处理，不能恢复旧发布链。

### R5：Pose Workspace 回到作者交互职责

入口：`Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseGraphWorkspace.cs` 及 Navigation／Tuning partial、`CharacterStateMachineAuthoringAdapters.cs`、`CharacterPoseTuningAuthoringService.cs`；`Editor/CharacterSimulation/Compilation/Presentation/CharacterPoseGraphProjectionValidator.cs`；LinkedPose presenter 为直接消费者。

| 输入与操作 | 当前问题 | 拟定归属与输出 |
| --- | --- | --- |
| 创建 State／Alias | 窗口构造领域状态、默认子图和布局 | 现有 StateMachine mutation 接收 document、位置／选择，完成同一 Undo 事务；窗口刷新页面 |
| Validate | 窗口和 Topology 重复能力检查；为了定位又跑状态机校验 | 正式报告携带 graph／node／port／state／transition 目标；UI 只显示和定位 |
| 保存作者资产 | Workspace 遍历保存，Tuning 模块又保存引用；无 Profile 模式被直接解引用 | 单一保存操作明确处理纯图与带 Profile 模式，并保存属于本次操作的已有引用 owner |
| Navigator／归属查询 | 数据投影、命名、规则 owner 查询和页面操作混在窗口 | 只读 catalog 接收 asset／profile；窗口保留打开页面、选择与导航历史 |
| Compile／状态提示 | 按钮只做 Validate 却报告编译成功，提示依赖旧 Projection／Build | 保留真实 Validate／保存；实际采用状态来自正式 Session，不伪造编译或运行生效 |
| tuning 指纹 | 写入口无消费者，状态恒定，仍参与选择发布分支 | 复查调用、序列化、meta GUID 后删除整条机制及调用 |

完整追踪 UI、C# authoring、Undo、Reload、保存及有／无 Profile 两种入口。作者合法性与依赖 Rig 的校验是不同输入合同，不能删外层重复调用后让纯图模式失去检查，也不能要求纯图保存凭空提供 Profile。不得添加缓存标志掩盖重复校验。

方案取舍：复用现有 mutation 与 validator 比新建统一作者 Service 更能保持单一语义。Navigation 的页面状态属于 UI，应保留；Live／只读行为与工作台当前合同保持，不由此次拆分重设计产品。

### R6：旧依赖清理必须覆盖生成、发现与序列化

入口：`Runtime/Character/ThirdPersonClient.Runtime.asmdef`、`Editor/ThirdPersonClient.Editor.asmdef`、`Runtime/Character/Pipeline/Graph/`、`Pipeline/Input/CharacterInputValueNodes.cs`、`Pipeline/Motion/CharacterMotionNodes.cs`、`Runtime/Character/Action/ActionTargetSnapshot.cs`、`Runtime/Character/Control/Authoring/FlowGraphs/`，以及 TreeDesigner 的 `ExposedPropertyUtility`、`PropertyPortUtility` 和正式 `BtsmtlSkillAuthoringCodeAdapter`。

当前已确认的区分：

- 部分 `using TreeDesigner` 是旧 import；`SemanticValueKind` 已在 `ThirdPersonSimulation`，黑板 scope／lifetime／policy 等已在 `BTSMTL.Authoring.Blackboard`。代码导出仍添加旧 import 的入口必须一起清理。
- Action／Equipment／Input／Motion 等旧节点仍因继承和端口产生编译依赖；尚未找到正式 Skill／Timeline runtime 调用这些旧节点的证据。单次名称检索不能证明无消费者。
- `BaseExposedProperty`／PropertyPort 派生类型可被反射发现；删除旧包装时应保留真实业务值，例如 ActionTargetSnapshot。
- `IActionContextAuthoring`、`ILocomotionInputMotionAuthoring`、`ICharacterInputValueAuthoring` 等合同仍可被正式 Flow 节点使用，不能随旧节点文件整组删除。

长任务为每组候选记录：定义与直接引用、正式入口是否可达、作者能力目录、反射／TypeCache／注册方式、生成代码模板、序列化类型名、脚本 GUID、资产／场景／Prefab 和程序集依赖。分类为无用 import、无业务消费者的旧簇、可原样迁出的公共定义、仍正式使用的实现。

确认无用的整个旧簇同时清理；有效合同迁到已有正确 owner 并更新直接消费者；无法保持行为的迁移列方案待决。最终才判断 TreeDesigner 包／asmdef 是否可删，不预设包删除为完成指标。保留编译 Skill、短路、数据依赖求值、动态端口和子图绑定等正式支持能力；不能把“编译了”当作任意原生节点语义等价的证明。

### R7：多类型文件按已有职责归位

`Runtime/BTSMTL/Timeline/Runtime/TimelineRuntimePreparation.cs` 包含 Preparation、Playback、Evaluator、缓冲及合同；`Runtime/Character/Pipeline/Presentation/FootPlacement/CharacterFootSwingMotionBuilder.cs` 包含输入结果、诊断与实际 Builder；Timeline Host 文件也含多个非 Host 类型。

长任务先按类型列出消费者和现有依赖，然后提出文件归属清单。保持类型名、namespace、可见性、布局、序列化身份和程序集；检查 Unity 主类型与 meta 关系后决定移动方式。只移动本来独立的完整职责，不把一个事务切成多个必须互相改内部状态的类。

收益是定位合同、算法和诊断更直接；成本是 diff 与资产身份管理。若只能减少文件行数而不能改善维护位置，保留原状并说明理由。

### R8：有共同状态的模块允许继续较大

入口：`Runtime/Character/Pipeline/Animation/BlendStack/AnimationBlendStackRuntime.cs`，已有 SourcePoseWorkspace、SlotPoseWorkspace，以及 Pose Graph／子图状态。

当前 `PushCrossFade`、`PrepareCrossFadePlan`、`RetireCompletedHistory` 与 Begin／Commit／Discard 共同维护权重、打断历史、来源引用和退休；初步结论是保持同一混合事务 owner。工作区已经分离，不为行数另建历史 Manager 或独立退休循环。

长任务仍应检查是否存在纯诊断、无引用入口或真正独立资源职责，但新增拆分必须说明可独立的输入输出、状态和修改原因。若删掉所谓新模块只会把同样时序要求散给多个调用者，说明拆分没有收益。保留结论与实施结论同样有效，不能为了任务数量强制改代码。

### R9：闭环和性能证据单列，不以源码结论替代实测

入口：`Tools/ThirdPersonPerformanceCapture/README.md`、当前 Capture manifest 与报告、性能相关 active changes。现有报告 `3cDemo/Client/3C_Client/Library/Performance/Reports/overall-performance-20260929.md` 对应 Player `fe981685443dab7200db321d`，不能代表此后所有提交或未提交改动。

该次正式采集已发布，报告为 2536 LogicTick／4304 表现帧、平均约 99.80 KiB 托管分配／帧、丢弃 9 Tick；预算未通过。游戏、回放证据和诊断分配尚未拆清，约 70.25% 原生 Exclusive 样本未解析到函数。一次普通回放与一次完整诊断的 FPS 差异不能当作精确探针开销。

后续方案必须覆盖：同输入闭环适用范围，Editor／IL2CPP 差异，探针 Disabled／MarkerOnly／Span 校准，多轮同条件比较，丢帧与丢 Tick，分配栈与原生符号，业务时间与诊断整理时间的区间划分。Span 是包含子调用、等待和抢占的经过时间；阶段父子项不能相加当作互斥成本。Disabled 仍有 Recorder／Profiler／WPR，不是零诊断开销。

本 change 只登记缺口和交接，性能实现归原专项。后续若仅授权排查，读取已有证据即可；若用户另行授权实际采集，使用唯一正式 workflow，遵守其 Smoke／Replay Gate，不绕过 Gate，也不因先前“不运行 replay”自行启动它。没有授权时报告缺证据，不伪造优化或 0 GC 结论。

闭环关注技能启动／退出、Timeline 片段和 Marker、状态切换／打断、Pose 子图、失败收尾、角色释放及实际支持的恢复路径；这些是结果评估维度，不是已确认 bug，也不写成 tasks 中的测试或用户验收任务。

### R10：规划、现行规格与专项分别拥有自己的事实

| 现行合同或活跃方案 | 本轮对照 | 处理方式 |
| --- | --- | --- |
| `character-animation-pipeline`：唯一原生 Pose 链 | 仍要求外层逐阶段消费，与已收拢 FrameCoordinator 的代码结构不同 | 本 change 提供完整 MODIFIED delta；保留原场景、模块 owner、算法约束并补整帧失败场景 |
| `graph-authoring-domain-framework`：Mutation、Navigator、原生 Pose | 现有原则保留；缺少可定位报告、纯图保存及真实状态提示的明确行为 | 本 change 增补要求；不引入第二套作者语义 |
| `gameplay-simulation-session-composition`：唯一 Host／roster／释放 | History 提取不改变唯一会话 owner | 保持主合同，排查恢复流程是否有尚未覆盖的外部行为变化 |
| `btsmtl-timeline-direct-runtime`：直接内容、Step、精确 TreeClip、快照 | 仍为正式行为；旧 BaseGraph 条款是“真实消费者存在时保留”，不是要求永久留旧接口 | 核对已删除入口和新 binding；没有行为变化不造 delta |
| `unity-simulation-assembly-ownership`：单向依赖、唯一序列化身份、热路径零分配 | 迁移必须遵守 | 不因清 TreeDesigner 泛化例外或增加空壳程序集 |
| `character-animation-foot-analysis-artifact` | 仍出现 Definition Build、CharacterPresentationProjection、Player 只消费 Projection，和 project 的领域资源准备口径有冲突 | 长任务沿实际 Artifact／Curve／resource binding 链定位受影响 requirement，提出精确替换 delta；此次不猜测或恢复旧 Projection |
| `eliminate-runtime-managed-allocations` | 已有大范围分配改造计划 | 分配归因与修复继续该 owner；本 change 保持热路径存储与数值顺序 |
| `add-compile-time-performance-instrumentation`、`gameplay-performance-capture-workflow` | 工具已超出旧 proposal 的部分历史状态 | 用当前实现和精确 Capture 核对；工具合同修订回到所属方案，不重复创建控制面 |
| 作者工作台、Pose 编辑预览、只读黑板三个 active changes | 涉及相同 Workspace／Session 状态 | 本轮整理内部责任；采用版本、Live 只读、Preview 体验不自行重定义 |
| Timeline 时钟与开放 TreeClip、Foot 稳定化、相机复刻 | 各有自己的业务语义 | 接受其已提交正确改动为基线；涉及同文件时按职责分清 diff，不夹带提交 |

现行规格同步、归档与“实现完成”是不同状态。提案完整不代表代码完成；代码提交不代表闭环通过；单次采集不代表性能归因完成。历史 archive 不重写。

## 长任务排查与交付协议

### 开始时

读取本 change、现行 project／受影响 specs 和各直接交叉方案；记录当前 HEAD、dirty 文件及相关提交，重新确认两组未提交代码是否仍存在和是否被其他窗口改变。逐项检查 R1—R10，不凭本文件的状态快照跳过。

默认只读代码和现有产物，不运行 Editor、构建、采集或 replay，不创建 goal／worktree，不接管其他窗口任务。需要扩大业务范围或改变已确认行为时，仅暂停该项，继续完成不依赖它的排查。

### 每项必须给出的证据和方案

1. 输入提供者、正式入口、关键调用链、输出消费者以及相关源码位置。
2. 状态、校验、提交、失败、恢复与释放各由谁拥有；区分配置准备与运行事实。
3. 具体问题与影响，区分已确认缺陷、静态风险、命名残留和仅有行数印象。
4. 建议的完整切片：保留／移动／删除对象，所有直接消费者、资产、反射、生成代码和程序集影响；不只给新类名。
5. 不改现状的成本、建议方案的收益和迁移成本；存在真正同级选择时列明业务差异，不替用户决定新业务优先级。
6. 现行 spec 与并行 change 是否冲突、需要增加／修改／删除哪个 requirement；涉及行为改变时先修订方案。
7. 可获得的编译／运行／分配证据、缺失证据与结论上限；不开测试任务，不把无样本写为零开销。
8. 结论：已完成、建议实施、明确保留、转交既有专项或需要业务决策；清楚写出剩余工作。

### 最终方案输出

回写本 design 的状态表和各项证据，给出完整调用与状态归属图、删除／迁移清单、按技术依赖排列的小步提交方案、精确 spec delta 和风险。步骤之间有依赖才排先后；独立业务收益列为同级，不臆造收益排名。新增发现必须能追溯本清单业务链，范围外只登记转交。

当前 tasks 仅是已知实施切片的草案。排查可能使某项改为保留或转交；应修订任务及理由，而不是把“决定不做”勾成代码已完成。长任务以交付这份完整方案为本阶段终点，不因为有 tasks 就自动执行代码。

建议后续长任务输入：

> 以 refactor-character-runtime-and-authoring-boundaries 为统一入口，先完整排查 R1—R10，核对现行源码、工作区和相关 specs，按 design 的交付协议给出并回写完整重构方案。区分已完成、未提交、建议实施、明确保留和需要决策。先不实施代码、不运行 replay、不新增测试、不创建 worktree；不要只检查几个大类就宣称全部完成。

## Risks / Trade-offs

- 工作区被多个任务修改 → 每个切片重新读 diff 和直接消费者，使用显式文件清单；不整体提交别人的改动。
- 重构意外改变失败或恢复顺序 → 先写状态交接表，对照正式输入输出与既有错误协议；不能以新增兜底或缓存状态掩盖责任不清。
- 删除类型破坏反射、生成代码或序列化 → 按 R6 的多种引用方式核对；有真实消费者则完整迁移或保留，不用兼容别名。
- 搬文件形成漂亮但浅的包装 → 用完整输入输出和状态所有权判定收益，保留 R8 的共同事务。
- 报告对应旧构建或混有工具成本 → 保留精确身份和区间，明确结论范围，不从源码拆分推断性能收益。
- 规划无限扩张 → R1—R10 全覆盖，但修复其他专项只做明确交接；不能把用户的“完整”解释为重写整个项目。

## Migration Plan

先完成只读方案阶段。用户另行启动实施后，优先收口已经存在的完整候选切片以减少悬空工作，再按 R5／R6／R7 的实际依赖实施；R1／R2 只处理对账发现的真实缺口，R8 允许以有证据的保留结论结束。性能与算法专项独立执行，不能悄悄变成本轮的前置重写。

每个提交包含该职责的生产者、消费者、资产／配置和旧入口清理；保持可解释的中文提交。基本 diff／编译检查按改动进行并记录结果，不新增测试任务。需要撤回时以精确提交为单位协调恢复，不丢弃其他窗口文件，不通过留下新旧两条运行路径便于回退。

收尾时同步通过审议的规格增量和状态台账。用户明确要求归档或表示已经验收后，按正式流程归档，不再追加手动验证门槛。

## 本轮排查结果（2026-09-29）

### 基线、方法与结论范围

排查从 `9f7a58b3c` 开始，期间其他窗口提交了 `d4d05840e`（回放逐帧哈希及输入数值复用）。按当前工作区分别检查 HEAD 与未提交 diff；没有修改业务代码、资产或测试，没有运行 Unity、构建、replay、Player 或性能采集。本文行号来自本次读取，未来移动代码后以符号和调用关系定位。

Timeline 的 HoldLastFrame 改动涉及 Host、SnapshotCodec、TimelineRuntimePreparation、TimelineData 及数值执行后端，属于其他窗口在途行为变化。该链及其他 Camera／EventGraph 修改不得混入本轮机械迁移。SessionHistory 与 AnimationSampler 是本任务此前遗留的未提交候选，仍须修正缺口并单独交付。

| 范围 | 排查后结论 | 实施或保留决定 |
| --- | --- | --- |
| R1 | Pose 内部失败后提交的旧缺陷已修复；外围业务提交与释放还有静态缺口 | 保留 Pose 协调器，补现有 Presentation owner 的故障／收尾责任 |
| R2 | ContentStore／Host／Playback／表现／快照／诊断已有真实分工 | 保留架构与新 GraphBindings；只整理文件和处理 R1 的交叉问题 |
| R3 | History 提取成立；失败释放借用引用、错误优先级仍需收口 | 修正两项后提交完整提取；不新增角色表现恢复能力 |
| R4 | 算法主体与 HEAD 一致，采样公式与数组数量保持 | 收口已有提取，保持正式 artifact／曲线／领域资源链 |
| R5 | 六项旧发现成立，另确认创建 Undo 与生成保存集合不一致 | 五个责任切片完成 UI 与 C# 的同链迁移 |
| R6 | 清理不止 import，含旧节点簇、编译器旧类型判支、反射和旧菜单 | 删除范围见依赖证据台账；有效 Flow 合同和值保留 |
| R7 | 三个文件确有多职责类型混放，另有四个失效场景组件 | 文件归位与失效组件清理分开提交 |
| R8 | BlendStack 状态必须共同提交，继续拆核心收益不足 | 保留单一 Stack owner，修正规格中错误的编译节点称呼 |
| R9 | 一次 Capture 身份及五类文件校验通过，没有基线比较 | 性能归因／校准交现有专项，不宣称此次重构已零 GC |
| R10 | 除原两份合同外，Native Pose／BlendStack／Foot spec 也有偏差 | 逐 requirement 列出同步文本与保留约束，避免全局词替换 |

### 调用与状态归属图

```text
已提交 Body／Action 事实 → PresentationDomainRuntime
  ├─ EventGraph → 本帧动画变量
  ├─ TimelineHost → Action 时钟下的表现候选
  └─ Pose DomainSession.RunFrame → FrameCoordinator
       ├─ RoleRuntime 驱动 Source 租约、准备与退休
       ├─ GraphRuntime／子图持有节点局部候选
       ├─ Constraint 持有 Foot／IK 候选与历史
       └─ 唯一 Animancer Barrier／FinalPublication
            → Pose 成功提交 → 命令与 Action 时钟确认
  → Timeline／Bridge／采样时钟／Camera 业务收尾
  → 观察发布（不拥有业务状态）

Unity／TickSystem → SessionHost（唯一会话生命周期与 roster）
  → backend.LogicTick → History（检查点目录与历史分支）
  → DebugControl 恢复请求 → Host 准入 → History 恢复事务
  → 同一 backend 与 Presentation 能力合同 → TickSystem 继续驱动

作者 UI／C# → 同一领域 mutation → 图／状态／布局 owner
  → 同一创建 Undo 与实际写入 owner 集合 → 正式保存／回退
  → UI 从正式数据重新绑定，诊断只读

Clip／Motion Reference／Rig／Calibration
  → ArtifactBuilder → Analyzer → Sampler → 规范样本数组
  → Analyzer 算法 → Library Artifact
  → 显式作者 Apply → 原生 AnimationClip 注册曲线
  → Pose 领域资源编译 → NativeDomainResourceSet
  → SourceCatalog／DomainServiceFactory → 原生 Pose 节点
```

### R1／R2：整帧责任与失败表

路径仍以 Main 为共同前缀：

- `Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeRoleRuntime.cs:317` 已在物理写入前完成 Constraint 检查；`:349` 检查提交结果，非 Committed 不再提升 Constraint／Source。旧“失败仍提交”不能列为当前缺陷。
- `CharacterPoseNativeFrameCoordinator.cs:28` 拥有完整 RunFrame；`:82` 确认命令与 Action 时钟；`:104` 在 Barrier 后失败时锁定故障。`CharacterPoseNativeDomainContracts.cs:231` 只提供整帧入口。
- `Runtime/Character/Pipeline/Presentation/CharacterPresentationDomainRuntime.cs:510` 在 Pose 返回成功后还提交 Timeline、桥、采样时钟与 Camera；`:518` catch 只报告再抛出。这些操作不在 Coordinator 的 fault latch 内。
- `Runtime/Character/Pipeline/Animation/Lifecycle/CharacterTimelineHost.cs:1528` 在该收尾阶段调用 `PresentationFrameProduced`，`:1554` 释放时钟、`:1558` 发结束事件并更新表现播放状态。事件回调和后置业务提交提供了实际失败入口，不是凭空假设。
- `CharacterPoseNativeDomainContracts.cs:308` 顺序 Dispose Session 与 Services；前者抛出时后者未执行。Presentation `:716` 也有顺序释放链。Role 的逐 owner 清理与异常汇总已存在，可以沿明确资源所有权收口，而非添加通用吞错层。

| 阶段 | 可以改变的状态 | 失败处理目标 |
| --- | --- | --- |
| Barrier 前 | 各 owner 的 Pending、租约、候选资源 | 丢弃所有已开启候选；保留首个错误并记录清理错误 |
| Barrier 开始至 Pose 提交 | 骨骼求值／写入可能已发生 | 进入 Actor 表现 Fault，不确认失败帧成功，不声称物理回滚 |
| Pose 已提交后的业务收尾 | Pose／命令已提交，其他 owner 可能部分安装 | 同一 Actor 表现故障入口拒绝后续帧，保留已提交事实及失败阶段 |
| 纯观察输出 | 已完成业务状态只读 | 使用既有诊断错误通道，不把观察失败标成业务帧已回滚 |
| Stop／Dispose | 各自拥有的租约和资源 | 每个已取得 owner 均尝试释放；一个释放错误不能遮蔽原故障或跳过剩余 owner |

推荐在已有 Presentation owner 收拢外围故障和清理，保留 Pose 可独立复用的局部事务；对外保持唯一 Actor 表现不可用结果，不能让两个故障标志各自决定恢复。另一可行方案是扩展已有帧协议纳入 typed 外围参与者，统一成败但改动更多。两者均不能把 Timeline／Camera 实现塞入 GraphRuntime，也不能把 Barrier 后异常包装成可重试成功。实施前将具体事件消费者区分业务与观察，不统一 catch 后忽略。

Timeline 状态边界已核对：ContentStore `:42/124/154/213` 持有内容与采用计划；Host `:498/622/639/650` 持有播放登记和 Action 关联；Playback `TimelineRuntimePreparation.cs:202/357/495/528` 持有游标和候选；表现图 `CharacterTimelinePresentationGraphRuntime.cs:44/90` 精确按 provenance、graph、revision、hook 绑定程序；SnapshotCodec 无跨帧状态。ContentStore 准备、发布与采用阶段都可能遇到内容失效，不能仅因条件相似删除所有检查。`CharacterTimelineGraphBindings.cs:18/67` 必须保留。

### R3：History 候选的确定修正

`SimulationSessionHost.cs:34/38` 保持唯一 roster／runtime；`SimulationSessionHistory.cs:10–21` 持有检查点、裁剪缓冲和历史分支，仅借用同一 runtime。`Host:454` 绑定借用引用，正常释放 `:659` Detach，但失败释放 `:586` Dispose 后只清 Host 的引用。推荐由实际释放 runtime 的边界同时解除 History 借用，不给 History 新增 Dispose 责任或 Failed／Paused 状态。

恢复入口应保持既有失败先后：精确恢复先确认 runtime capability，再查询检查点与兼容性；输入区间恢复依次确认区间、replay capability、检查点与兼容性；非精确历史恢复保持 replay、checkpoint runtime、presentation 能力检查顺序。候选 History `:84/97/161/178` 改变了这些顺序及部分错误文案，应在一次请求准入中恢复顺序，内部助手接收已确认的 typed 对象，不再重复校验。

保留候选 `:157` 精确命中后直接进入内部恢复的做法，消除旧公开入口的重复检查。保留 `:97–140` 和 `:178–219` 的真实模拟／表现回滚；`FixedPassPipelineRuntimeHandle.cs:137–231` 自己处理重放启动失败，因此上层收到 false 直接返回不是缺失回滚。返回成功只表示重放输入已准备，后续由正式 TickSystem 推进，不等于目标 Tick 已执行完。

`History:294` 的 envelope 兼容性与 `FixedPipelineTransaction.cs:160`／`FixedSimulationSessionSnapshotCodec.cs:97` 的 payload 解码、hash 和 solver 一致性不同，不能将后者批量视为重复防御检查。下一生命周期 `Host:665` 才清空历史与更换 branch，正常释放后保留历史供诊断的行为保持。

重要能力限制：`CharacterPresentationDomainRuntime.cs:177–178/419/427` 明确不支持表现检查点 capture／restore；`FixedCharacterRegistration.cs:142–160` 暴露同一限制。当前角色恢复会被正式 capability 拒绝，这在 HEAD 已存在，不能借 History 重构返回空成功或跳过表现恢复。周期 Capture 仍会创建 snapshot／payload，也不能把提取后的 Tick 铵称为零 GC。

### R4：采样、算法与发布的证据

入口 `AnimationFootAnalysisArtifactBuilder.cs:30–50` 先取得精确 identity，再调用 Analyzer，最后写 Store。Analyzer `:62/92` 使用 Sampler；`:103/114` 为 Calibration 复用同一采样实现；`:125` 消费完整样本；`:241–242` 把 Motion 样本交给原 MotionDataBuilder。

将 HEAD Analyzer 中 `ValidateFlatReconstruction` 到旧 `RequireFinite` 前的算法区段，与工作区相应算法区段归一化换行后逐字比较，结果相同。Sampler `:286` 保持 `Max(2, RoundToInt(duration * sampleRate))`、`intervals+1`、`i*step`、`sequence=i+1`。目标样本仍是左右各七个数组与两个 Root 数组，共十六个；删除的 finite 检查仅针对代码刚赋入的 zero／identity 常量。Motion `:331–389` 保留参考 Clip、loop clone、finally 销毁和原来的 TargetRootLocalSolePositions Clone，没有为模块传递新增整页复制。

Sampler 构造失败通过 Dispose 回收；Analyzer 的 using 在返回或失败时释放采样环境；源对象销毁顺序与 HEAD 相同。这是静态等价证据，不是实际 Unity 采样输出、异常释放或最终编译证明。收口时只暴露 Analyze 所需的采样及 Calibration 接口，BeginClip、单帧 Sample、空间转换和骨骼属性等内部细节不作为新公共作者 API。

正式资源链的精确入口：

- `Editor/CharacterPipeline/Authoring/Animation/CharacterFootMotionCurveAuthoringService.cs:117–147` 校验候选身份，进入同一 Undo 并替换原生 Clip 曲线；`:421` 显式构建 Artifact。
- `Editor/CharacterSimulation/Compilation/Presentation/CharacterPoseNativeDomainResourceSetCompiler.cs:43/105` 显式编译并写领域资源集；`:169–215` 从正式曲线和 Ready Artifact 生成 SourcePlan；`:219–228` 只 Inspect，缺失时拒绝，不即时运行 Analyzer。
- `Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeDomainResourceSet.cs:138/173` 创建来源 catalog 和 Foot 资源；`CharacterPoseNativeDomainServiceFactory.cs:71/93/97/205` 消费这些资源，不读 Library Artifact。

因此原 spec 中整角色 Projection 的名词需要调整，但 `CharacterLocomotionPhaseAuthoringService.cs:39–62/92–109` 当前直接证明的是单 Clip 相位锚点和覆盖检查，不能拿它冒充所有可达状态关系的完整质量门槛。跨关系质量合同是否完整接通是独立证据缺口；保留该业务要求，不能以规格清理为名删掉它。

### R5：窗口、领域修改与持久化的完整切片

重复校验的位置仍是 Workspace `:1327` 与 Topology `CharacterPoseGraphProjectionValidator.cs:159`，状态机 RequireValid `:753` 与 Workspace FindFirstIssue `:1417`。报告应直接携带 graph／node／port／state／transition 目标，删除窗口 `TryFindStateMachineValidationIssue`。纯图入口 `Workspace:199–224` 无 Rig，不能沿用 Topology `:135–155` 的提前返回而跳过作者检查。安静状态提示 `:1578` 仅做 capability 检查，显示范围也应明确。

State／Alias 创建 `Workspace:1130/1170` 移入现有 mutation adapter；窗口继续负责菜单可用性、刷新、Focus、Reload 和 Undo 后重绑。只读目录收拢 `ResolveRole:536`、`ResolveGraphDisplayName:552`、规则 owner 查询及 `NavigatorDataSource.GetItems:1745`，Open 和 Navigation partial 留 UI。

图创建存在额外的 Undo 所有权差异：UI → StateMachine adapter → `CharacterPoseGraphMutation.cs:1045` 只记录已有 owner；`CharacterPoseGraphAuthoringAdapter.cs:590` 的 GraphCatalog mutation → `SetGraph/AddGraph` → `CharacterPresentationPoseGraphAsset.AttachGraph:236` 只 AddObjectToAsset。C# `BtsmtlPoseAuthoringCode.cs:138–139/155–156` 调同一创建 mutation 前却自己 RegisterCreatedObjectUndo。推荐在实际 GraphCatalog 创建 owner 注册一次，删除 C# 同一条根 Graph／子 Graph 路径的重复注册；保留 `EnsureRoot:114–121` 根资产创建事务和 SourceSlot／ResourceSlot 的各自注册。UI Undo 后悬空子资产属于静态风险，未运行复现。

保存不能只修改窗口：`BtsmtlPoseAuthoringCode.cs:178–186` 修改独立 Profile，生成上下文 `BtsmtlUnityAuthoringGenerationContext.cs:28–32/36/113–114` 却只快照和保存 output／definition；MCP 使用该集合回退。因此被修改 Profile 可能未保存或未回退。方案是在生成正式事务写入前登记实际写 owner，使快照、dirty、保存和失败回退使用同一集合；只读 ResolveExternalAsset 不意味着该资产必需写入，不能盲目快照所有依赖。禁止调用 Workspace.Save 绕过生成事务。该项与纯图／带 Profile 的保存整理同属 R5，但可独立提交。

确认删除：Tuning partial 及 meta（GUID `9c4e5f7b1a2d4c6e8f0b3d5a7c9e1f2b` 仅自身）、三个指纹状态字段、相关八个方法、Workspace reset／tuningOnly 分支；两个假 Compile 按钮与 `CompilePoseProjection`；旧 Projection／Build 文案；仅定义的 `m_ObservedPorts`、`DefinitionContextValue`、`RefreshRuntimeDetails`；`&& true` 和未使用 root 局部检查。`CharacterLinkedPoseAuthoringWorkspacePresenter.cs:181` 的状态调用者一并改名。保留 `IsLinkedPoseReadOnly` 的真实消费者，其产品语义属于并行工作台方案；不要因常量值就一并删除。

### R6：完整依赖证据台账与整包退役方案

在 `d4d05840e` 及当时工作区快照，已有足够静态证据提出整包退役方案。结论依赖完整切片，不是只删除一个目录；尚未执行删除、Unity 导入或编译。

正式 Skill 发现链 `BtsmtlSkillNodeCatalog.cs:19` 只发现 BtsmtlSkillFlowNode、Macro 和明确的 Flow wrapper，`BtsmtlSkillFlowGraph.cs:46` 使用该 capability 目录。`BtsmtlSkillGraphOccurrence.cs:261/279` 的 Nodes 是 FlowNode 列表。`Editor/CharacterSimulation/Compilation/Skills/GameplayAbilitySemanticFrontendCompiler.cs:139` 却将节点转为 object，再判断三个旧 Equipment 类型并声明 Equipment capability；旧类型不属于 FlowNode，这个分支不可达，应连同 requiresEquipment 局部状态一起删除，不借机新增 Equipment Flow 能力。

七个可整文件及 meta 删除的包外簇，共三十九个类型：

| 文件（位于 Runtime/Character/Pipeline） | 类型集合 |
| --- | --- |
| Graph/ActionRuntimeNodes.cs | ActivateActionInstanceNode、SubmitActionLifecycleTransitionNode、ActionWindowActiveInfoNode、CanActivateActionInfoNode |
| Graph/EquipmentRuntimeNodes.cs | EquipmentChangeFailurePropertyPort、ReadEquipmentIdentityNode、ReadEquipmentParameterNode、EquipmentChangeOperationNode、RequestEquipmentChangeNode、BeginEquipmentChangeNode、EquipmentPendingChangeOperationNode、CommitEquipmentChangeNode、CancelEquipmentChangeNode、EquipmentNodeValueTypes |
| Graph/GameplayEffectRuntimeNodes.cs | HasGameplayTagNode、MatchGameplayTagQueryNode、ReadGameplayAttributeNode、ApplyGameplayEffectNode、RemoveGameplayEffectNode |
| Graph/SimulationRuntimeNodes.cs | SimulationOperationNode、SimulationValueNode |
| Graph/UInt64PropertyPort.cs | UInt64PropertyPort |
| Input/CharacterInputValueNodes.cs | CharacterInputValueInfoNode、CharacterInputBoolInfoNode、CharacterInputFloatInfoNode、CharacterInputVector2InfoNode、CharacterInputVector2MagnitudeInfoNode、PipelineBlackboardValueInfoNode、PipelineBlackboardBoolInfoNode、PipelineBlackboardIntInfoNode、PipelineBlackboardFloatInfoNode、PipelineBlackboardStringInfoNode、PipelineBlackboardVector2InfoNode、PipelineBlackboardVector3InfoNode、CharacterActionRequestInfoNode、StateExitCauseInfoNode、ActionContextActiveInfoNode |
| Motion/CharacterMotionNodes.cs | LocomotionInputMotionNode、CharacterMoveFacingAngleInfoNode |

类型级删除：`Runtime/Character/Action/ActionTargetSnapshot.cs:30` 的 ActionTargetSnapshotExposedProperty；`Motion/ILocomotionInputMotionAuthoring.cs:51` 的 ISubmitActionLifecycleAuthoring，后者仅被旧节点实现。保留这两个文件及 meta 的其余正式类型。

必须保留：ActionTargetSnapshot；ILocomotionInputMotionAuthoring、ICharacterInputValueAuthoring、ICharacterActionRequestAuthoring、IActionContextAuthoring、IActionWindowAuthoring、ICanActivateActionAuthoring、IGameplayTagAuthoring、IGameplayTagQueryAuthoring、IGameplayAttributeAuthoring、IGameplayEffectApplicationAuthoring、IGameplayEffectRemovalAuthoring、ICharacterBlackboardAuthoring。消费者包括 BtsmtlSkillLocomotionFlowNode `:62`、BtsmtlSkillValueFlowNodes `:9/89`、BtsmtlSkillActionFlowNodes `:34/48/92`、BtsmtlSkillBlackboardFlowNodes `:28` 以及正式 Inspector／CapabilityCatalog。`Pipeline/Graph/CameraRuntimeNodes.cs:71` 已是正式 Flow 节点文件，不属于删除簇。

生成侧同步删除 `BtsmtlSkillAuthoringCodeAdapter.cs:78` 的 AddUsing("TreeDesigner")。清理无用 import 的集合是 CharacterPipelineDefinition、BtsmtlSkillBlackboardDeclaration／EditorAdapter、CharacterAnimationPresentationProfileInspector、BtsmtlSkillAuthoringCode、BtsmtlSkillAuthoringCodeAdapter、CorinRushTimelineAuthoringBuilder，以及 Generated 中 CorinBranchTimelineAuthoring 和 Attack／BranchAttack／RushAttack／DodgeForward／DodgeBack 的对应主代码。SemanticValueKind 和黑板公共定义已经有正确 owner，不再迁移一次，不重建作者资产。

包内 TypeCache 发现入口 ExposedPropertyUtility `:26`、PropertyPortUtility `:27` 的消费者仅位于旧包；连注册器与包装一起删除。整包仍包含三个 Unity 作者菜单：OneRootTree `:64`、SubTree `:25`、StateBehaviorSubTree `:211`，它们创建 SerializeReference BaseTreeAsset，必须明确作为退役内容一并取消。正式 Skill／Timeline 作者能力不依赖它们，不保留兼容菜单。

共享 `Runtime/BTSMTL/Editor/Resources/VisualTree/EnumMenu.uxml:2` 的 TreeDesigner 路径文字实际通过 GUID 指向现存 BTSMTL `StyleSheet/EnumMenu.uss`；修正旧资源路径，保留 GUID 与共享样式。不得删除整个 BTSMTL／Authoring／Diagnostics／FlowCanvas 或误删 NodeCanvas／Opsive 的同名 ActionNode、SubTree。

| 检查方式 | 结果与限制 |
| --- | --- |
| 旧包 115 个脚本、219 个声明名与包外源码对应 | 真正依赖集中在上述删除簇和前端旧 Equipment 判支；已区分同名第三方类型和枚举项 |
| 全部 144 个包内 meta GUID 对 31,009 个包外文本文件 | 无引用；另对 122 个候选脚本 GUID 和四十个旧类型名独立核对 |
| 文本资产／场景／Prefab／controller／配置 | 覆盖 218 个 unity、299 个 prefab、301 个 controller；Assets／Packages 共 884 个 asset；未找到旧 managed-reference，命中仅为两个 asmdef 和共享 UXML 路径 |
| 61 个二进制 asset | 用本机已有 UnityPy 只读解析全部成功：176 对象、124 external 引用、0 解析错误；仅 LightProbes／LightingDataAsset／TerrainData／Texture2D／NavMeshData／Mesh，无 MonoBehaviour、MonoScript 或 managed-reference 对象，type tree 和 external GUID 无旧依赖 |
| 二进制分布 | 第三方 Demo 18、Opsive Samples 10、Animancer Samples 33；Configs／GameScripts 下为零；无需启动 Unity 或安装新工具 |
| 45 个 DLL／dll.bytes | 无 TreeDesigner 字节字符串；只作补充，不当作完整程序集运行证明 |
| asmdef／asmref／link.xml／rsp | 包外仅 Character Runtime `:26` 和 Editor `:48` 两个 asmdef 依赖，没有发现其他显式保留引用 |

实施建议两步：先清 import、生成入口和共享样式路径；再在一个完整提交中删除七文件、两处孤立类型、前端旧判支、TreeDesigner 整包与目录 meta，去掉两个 asmdef 引用。不能提交“旧类仍引用已删程序集”的中间状态。保留短路、动态端口、子图绑定和编译执行 Skill 现有实现；本轮不是对所有历史 Tree 节点作等价支持承诺。实施前若工作区增加了本次证据外的真实引用，应列出具体消费者重新定界，不用 fallback 类型绕过。

### R7／R8：文件归属、场景残留和保留理由

TimelineRuntimePreparation 当前顶层类型多于初评，文件行数不能代表一个类。按完整职责归位：

| 文件职责 | 现有类型分组 | 主要消费者 |
| --- | --- | --- |
| 原 Preparation 文件 | PrepareRequest／Status／Result／Preparation | TimelineRuntimeService |
| DependencyContracts | NumericTarget／DependencyHandle／Resolver／PreparedDependencies | Preparation、Service、Character dependency resolver |
| Playback 与 AdvanceContracts | 完整 Playback；Handle／AdvanceRequest／Result／ClipBoundary 分开归合同 | Service、Host、Simulation adapter |
| EvaluationContracts | TreeClip／Marker 请求、事件、Clip／Scene 采样、MotionWarp、Trace、EvaluationResult | 领域 sink、bridge、diagnostics |
| EvaluationStorage 与 SampleBuffer | 原存储；SampleBuffer／SampleView | Playback、Evaluator、MotionSampling |
| PresentationFrame | PresentationBuffer／Operations／Frame | 表现 driver、Character Host |
| StepCoordinator | Decision／Context／Consumer／Coordinator | Service |
| Evaluator／PresentationEvaluator／EvaluationSegments | 原算法与片段计算 | Playback、Composition、MotionSampling |

保留 Playback 内部状态机，不泛化 SampleBuffer。原 Preparation 与 meta 保留给原类型，其他文件保持 namespace／可见性／程序集。Host 文件中 DomainBindingResolver、DependencyResolver、TreeClipService、MarkerService、观察合同、PendingAdvance／PendingStop、表现调用合同各归相应已有职责；DependencyResolver 与新 GraphBindings 合并为完整同名模块，不复制绑定实现。

FootSwing 文件当前有 33 个顶层类型，无 Unity 对象基类或序列化属性：`:9–386` 输入／结果和约束合同；`:387–677` ResolvedFoot 诊断；`:678–834` Swing 合同；`:835–2174` Swing／连续性／输出阶段诊断；`:2175` 才开始实际 Builder。方案保留 Builder 原文件及 meta，另归 FootPlacementContracts、FootSwingMotionContracts 与 FootPlacementDiagnostics，保留全部字段布局、namespace 和数值代码；不逐 struct 建文件。

真正的资产残留：普通 C# `CharacterTimelineHost : IDisposable` 的脚本 GUID `39951ece6cf593042bd3d137276bfb21` 仍作为 MonoBehaviour 的 m_Script 出现在 `Assets/Scenes/Authoring/BtsmtlPreview.unity:656` 与 `Assets/Scenes/GameplayLab/GameplayLab.unity:191/209/1277`。需删四个已失效组件对象及所属 GameObject 的对应 Prefab m_AddedComponents fileID，不删 GameObject、不加回 MonoBehaviour、不覆盖整个场景。这是静态格式和类型不匹配证据，未开 Editor 复现 Missing Script。

BlendStack 保留核心所有权：`AnimationBlendStackRuntime.cs:580/633` 开帧／提交同时驱动来源、槽位、entry、release 与权重页；`:1240` PushCrossFade 同时压缩历史、保留 Stored Pose、暂存来源退休；`:1557` 根据计划权重判定历史退休。拆独立历史或退休 Manager 会要求每次中断跨多个 owner 同步。无状态诊断格式化可单独整理但目前收益不足，不列为必做任务；SourcePoseWorkspace 和 SlotPoseWorkspace 已分离且有真实资源职责。

### R9：已有采集的核验与专项交接

对精确 Capture manifest 中声明的 summary、runtime-result、comparison、build-inputs、cpu-hotspots 五类文件进行了大小与 SHA-256 对照，全部匹配。manifest 为 Completed／Span，build `fe981685443dab7200db321d`；summary 为 42.407318 秒、101.491917 FPS、9 dropped ticks、budget_evaluated=true／budget_passed=false、53997／76864 未解析原生 Exclusive 样本。comparison 为 NotRequested，未选择 baseline，不能给优化差值。

只校验上述文件，不冒充重新解析了 ETL、Profiler 二进制或逐次 Span，也不推断未采路径。当前代码已有后续提交和未提交改动，该 Capture 不能覆盖当前候选。现有 tools README 的采集、重复比较和模式校准入口保留；GC 栈归因、符号改善、多轮同条件基线和模式校准回到性能专项。R3 周期 checkpoint 分配也进入该专项证据清单，不在 History 提取里悄悄换存储语义。

### R10：规格同步的精确范围

本 change 已有 delta 继续修改两份能力：animation-pipeline 的整帧入口和外围业务故障；authoring-domain-framework 的报告定位、纯图保存、真实状态及 UI／C# 实际写 owner。以下额外同步属于后续正式规格整理，当前以精确编辑清单保留在 design，不直接改现行 spec 或重写 archive：

| 目标 requirement | 精确修订 | 必须保留的语义 |
| --- | --- | --- |
| `native-flowcanvas-pose-runtime` 的“Pose必须提供供角色外壳调用的阶段结果” | 改为外壳调用完整帧入口、消费最终 typed 结果；准备／创建／停止／观察仍独立提供，内部阶段结果由 Pose 协调点消费 | 同一时钟、唯一 Barrier、实际版本／ResetGeneration、Barrier 前 Discard／后 Fault |
| `character-animation-pipeline` 的“Dense状态与稀疏生命周期必须使用不同暂存策略” | Program pending state 改为原生节点小型 Pending 状态；保留既有场景标题身份，正文继续明确原生图执行 | 固定双页、固定容量 journal、Frame 外调参，不复制完整 Registry |
| 同能力的 Barrier／异常 requirement | 明确 Pose 返回后外围业务提交仍属于同一 Actor 的不可逆故障范围；纯观察失败不伪装业务回滚 | 已物理写入不能回滚、未来帧拒绝、首故障与 cleanup 信息 |
| `character-animation-blend-stack` 的“每个显式Blend Stack节点必须拥有唯一有序状态” | “编译后的每个”改“原生图装配后的每个显式” | 独立节点状态、时钟、压缩与来源退休；source-local AnimationPhaseRelationPlan 仍是有效准备数据，不删除 |
| Foot artifact 的 Purpose 及“Definition Build必须精确消费Artifact并发布Projection” | 发布主体改显式动画领域资源准备，输出改与正式 source binding 匹配的领域资源集 | 精确 identity／hash、Ready 才发布、不即时分析、每 source usage 独立、质量门槛仍应覆盖原规定范围 |
| Foot artifact 的“Player Runtime必须只消费Projection” | Player 只消费已发布 Pose 领域资源和正式 Clip／source binding；不依赖整角色 Program | 禁止 Library／Analysis Source／Sampling Rig／AssetDatabase 和即时分析 fallback |
| Foot artifact 的 Artifact Store／Phase候选／可达关系质量／Phase Descriptor 条款 | 将其发布者与过期对象改为实际领域资源准备链；Apply 只写原生 Curve，后续显式重建资源 | 输入失效拒绝、质量失败拒绝、Descriptor Editor-only、不发布 samples、公式门槛不变 |

Foot 跨状态关系的完整质量门槛目前没有从上述单 Clip 编译链取得完整实现证据；必须在对应能力中补齐或提出显式行为决策，不能通过删除该 requirement 假装对齐。当前重构可以继续 Sampler／文件整理，不以这个独立语义缺口为前置。HoldLastFrame 的模式与端点规则由当前实施者同步其专项，不由本次改写。

### 实施顺序与完成判据

顺序按技术依赖和已有工作区切片组织，不宣称业务收益排名：

1. Session 借用引用／错误顺序与 History 完整提取；Foot Sampler 静态等价提取可独立提交。
2. Pose 作者报告与模式 → UI 定位与状态；GraphCatalog 创建 Undo → State／Alias 移交；作者实际写 owner 与保存集合为独立切片。
3. 清理死 tuning、假 Compile 和额外无引用成员；提取只读 catalog，保持导航不变。
4. TreeDesigner import／生成与前端旧类型判支 → 无消费者旧节点簇／包装 → 反射与旧菜单 → 程序集闭包；完整证据台账决定最终包删除。
5. Timeline／Foot 多类型文件归位；四个失效场景组件独立清理，避开 HoldLastFrame 等在途行为 diff。
6. R1 跨域故障与释放边界按完整状态表单独提交，不能夹在机械搬文件里。R8 以保留结论结束。
7. 同步对应规格增量、剩余缺口与专项交接。实施任务只有代码和直接消费者已交付才勾选；未运行闭环／采样继续明确标注。

本阶段的完成是 R1—R10 每项都有证据、决定和可执行切片。它不代表代码已实施、所有角色支持恢复、整条 Tick 链已零 GC，或该次历史 Capture 覆盖了当前工作区。


## 实施收口与完成核对（2026-09-29）

本节为当前结果，取代前文初始状态表与排查段落中的“未实施／待收口”状态；历史证据不重写为运行证明。当前 29 项实施范围已经完成，未运行 replay、未新增或修改测试、未构建或采集 Player，未自动归档。

### 各职责的交付与保留

| 范围 | 最终输入、处理与输出 | 提交或结论 |
| --- | --- | --- |
| R1 | Presentation 输入同帧事实和动作；Pose 完整帧协调点处理图、Source、Constraint、FinalPublication；成功后外壳提交 Timeline、桥、采样时钟和 Camera。外围失败把实际阶段写入同一故障 owner，下一帧入口拒绝；各 owner 清理失败不覆盖首故障 | `418303f85`、`67c12591b`；既有整帧入口基线保留 |
| R2 | ContentStore 管内容；Host 管播放；DependencyResolver 精确绑定已编译图；TreeClip／Marker 服务、表现执行和观察各归既有模块。GraphBindings 合入同名 DependencyResolver，不建立第二播放状态 | 既有提交保留，文件归位见 `12ff37a89` |
| R3 | Host 提供唯一 runtime／roster／完成 Tick，History 管检查点、裁剪、分支和恢复事务；失败优先级、错误文案和实际回滚沿原协议，在真正释放 runtime 前解除借用 | `f023914fb` |
| R4 | Analyzer 请求规范样本；Sampler 独占临时场景、实例、PlayableGraph、NativeArray 和临时 Clip；数值分析使用原数组及时刻，输出交回既有 Artifact／原生曲线链 | `535f29692`；算法段归一化换行后相同 |
| R5 | UI 只提供创建意图、选择与导航；mutation 负责状态／图／布局和 Undo；统一报告直接携带定位身份；纯图与 Profile 模式共用拓扑检查，后者额外校验 Rig／外部输入；持久化和 C# 写 owner 事务覆盖实际修改资源 | `635dc1746`、`2d44c3ba3`、`92d445eec` |
| R6 | 完整删除旧 TreeDesigner 包、七个旧节点文件、两处孤立类型、旧菜单、反射发现、不可达 Equipment 判支、生成 import 与 asmdef 引用；保留真实 Flow 合同、Snapshot 值类型及 Camera Flow 节点 | `b9e0d0ae4`，297 文件变化，15,309 行删除、7 行增加 |
| R7 | 37 个 Timeline 类型、33 个 Foot 类型、11 个 Host 同文件类型按现有职责归位；Foot 输入／结果合入已有 CharacterFootPlacementContracts。四个失效 TimelineHost 组件及对应 Prefab 新增组件引用删除 | `12ff37a89`、`ee00df922`；80 个完整类型体逐字比较未变，另一个 resolver 仅合并原 partial 内容 |
| R8 | 保留 BlendStack 对 entry、混合时钟、Stored Pose、权重与来源退休的共同提交责任；SourcePoseWorkspace、SlotPoseWorkspace 继续承担真实资源职责 | 不拆出独立历史／退休 Manager，不改数值算法或状态布局 |
| R9 | 保留既有 Span Capture 证据与工具，明确它不覆盖本轮全部改动；本次没有性能差值、Player 结果或全链零分配证明 | 性能与分配归因继续由原专项处理 |
| R10 | 五份能力 delta 已同步，project.md 与入口文档更新，现行 spec 与增量严格校验通过；旧行数／未提交状态仅作为历史证据 | 本节、tasks、proposal、五份现行 spec |

技能仍然编译：正式 FlowCanvas 技能作者图经过 Semantic IR／正式后端进入既有执行链。本次删除的是无正式消费者的 TreeDesigner 作者和旧编译判支，不是取消 Skill 编译。Pose 保持原生 FlowCanvas 图执行，不恢复 Pose IR、ProgramImage 或加载期编译计划。

### 故障与清理状态核对

| 触发位置 | 已提交事实 | 处理及后续行为 |
| --- | --- | --- |
| Pose Barrier 前失败 | 未物理提交 Pose | 关闭取得的图、Source、Constraint 与外围 Pending；保留原失败，清理失败追加到同一异常 |
| Pose Barrier 内／后失败 | 可能已有不可逆求值或写入 | 原 FrameCoordinator 记录 Actor／Frame／BodyTick／Completion／阶段，拒绝后续帧，不宣称回滚骨骼 |
| Pose 成功后 Timeline／桥／时钟／Camera 失败 | Pose 和内部动作确认可能已经提交 | Presentation 保留返回的 committed publication，记录具体外围阶段并写入同一个故障 owner，所有清理步骤仍逐一尝试 |
| 纯观察输出失败 | 业务结果不因观察失败撤回 | Foot、Pose／Camera capture、Timeline 诊断经正式诊断错误通道报告；诊断错误订阅者自身抛错时记录原错误与报告错误，不升级为业务回滚 |
| 正常停止／Dispose 遇到一个 owner 失败 | 已取得的其它 owner 仍需释放 | 先解除桥接器事件订阅；Session、Services、各服务资源、时钟租约和表现外围资源逐一尝试释放；首故障与后续清理错误均保留 |

正常帧没有为这套故障协议创建委托、集合或异常对象；聚合异常与 ExceptionDispatchInfo 仅进入失败路径。此为源码分配检查，不等于对整个角色运行时的零 GC 测量。原 History 周期 checkpoint 创建快照的分配仍属性能专项，未借提取修改存储语义。

### 完成依据与限制

- 1.1—1.3：Host 的历史字段和事务入口已迁出；四类恢复入口进入 History；正常与失败释放共用真实 runtime 释放边界。
- 2.1—2.3：Analyzer 正式入口用 using 管理同一 Sampler；采样数组、时间、loop、空间与数值算法对账未变；没有第二采样实现。
- 3.1—3.9：Graph 创建 Undo 在 mutation owner 登记一次；State／Alias 数据构造离开窗口；报告携带错误位置，UI 不重查；纯图也覆盖连线、环、必需输入和输出数；保存覆盖有／无 Profile；C# 实际写 owner 共享快照／保存／回退；只读目录独立；死 tuning、假 Compile 和未使用成员已删除。最终审计另删除前置已完成的能力／子图签名重复校验、无输出的旧可达性遍历及恒不可达的局部门控。
- 4.1—4.4：删除前再次核对 151 个候选 GUID 对 26,286 个包外文本文件，无引用；包外脚本类型命中仅为列明删除簇和不可达 Equipment 判支。二进制资产沿用本节之前记录的只读解析证据，不冒充重新解析了全部二进制。删除后 Assets／Packages 的源码、asmdef／asmref、UXML、link.xml／rsp 未发现 TreeDesigner 残留；Unity Runtime／Editor 编译通过。
- 5.1—5.4：80 个完整类型体对提交前源码逐字比较相同；DependencyResolver 保留原定义与 GraphBindings 方法体及 meta 身份。两场景实际是 Prefab m_AddedComponents，已删四个对象和四条精确引用，保留其它对象与组件。
- 6.1—6.3：沿 Presentation → DomainSession → FrameCoordinator 的故障入口及完整释放调用链核对；没有新增第二故障状态或恢复分支。编译通过不代表已经注入各阶段异常运行验证。
- 7.1—7.3：Native Pose 整帧、Dense Pending、作者 UI、BlendStack 装配和 Foot 领域资源边界已同步；保留现行全部场景身份与质量门槛，专项归属如下。
- 正式 Unity 实例：`e852139597e42532`，projectRoot 为 `D:/Unity_Project_1/3C/3cDemo/Client/3C_Client`，Unity 2022.3.62f2c1。代码编译后再次确认 Edit／idle／非编译、Console error 0；Csc 的相关 Runtime／Editor 输出为 exit 0。本任务仅恢复已停止的本机 HTTP 服务，未发起 Editor 重启、切换全局实例或运行 batchmode。
- `openspec validate --specs --strict --no-interactive`：101 passed／0 failed；本 change 的 strict 校验通过；相关 diff 检查通过。
- 未执行 replay、端到端场景验收、异常注入、Player 构建、性能采集或探针校准。本任务的完成依据是已授权结构实施、静态对账和 Unity 编译；不宣称这些未执行行为已通过。

### 专项归属与不被此次完成覆盖的事项

| 事项 | 当前归属与限制 |
| --- | --- |
| HoldLastFrame、时钟模式、端点规则 | `add-timeline-clock-domain-config`，核对时其 tasks 为 56／56；保留其他窗口实施，不在本次重新设计或冒领验证 |
| 性能归因、GC、符号、多轮基线、Disabled／MarkerOnly／Span 校准 | `eliminate-runtime-managed-allocations` 与 `add-compile-time-performance-instrumentation`；既有 Capture 不是本轮性能结论 |
| Foot 跨状态可达关系质量门槛 | 现行 Foot Artifact spec 保留全部要求；本轮只确认单 Clip 相位锚点／覆盖链，尚无完整跨关系实现证据。具体补齐应单列 Foot 质量方案，不能将 `stabilize-character-foot-path-and-landing` 或此次文件整理当作这项已交付证明 |
| 表现检查点捕获／恢复 | 当前 Presentation 明确拒绝未装配能力；本次 History 提取没有新增表现恢复功能。需要另行明确该业务能力和恢复语义 |
| Pose Live／只读及作者工作台产品行为 | `design-btsmtl-authoring-runtime-workbench` 及既有预览／黑板专项；保留真实消费者，不在本次重设产品语义 |

撤回方式是按上述独立提交逆向恢复源码与 meta；本轮没有迁移磁盘业务数据格式或发布外部产物。其它窗口的代码、资源、构建产物和独立提交均保留，工作区整体不一定为空。完成的是本 change 的约定范围，不是“整个项目再无任何重构或业务缺口”。

# 角色底层重构总方案与长任务排查入口

## Context

更新：2026-09-29。状态：规划草案，覆盖当前已讨论的全部重构面；不是全仓审计完成证明，也不是实施授权。动机与能力增量见 [proposal.md](proposal.md)。

当前架构以 [project.md](../../project.md) 与现行 specs 为准；本文件记录增量方案、证据和偏差。不得把 archive 或旧评估中的目录、行数和状态当作当前事实。路径默认相对仓库根，源码入口的共同前缀为 `3cDemo/Client/3C_Client/Assets/GameScripts/Main/`。

### 已有基线与未完成状态

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
- 长任务的第一份交付是完整排查结果及方案；实际实施以用户后续指令为准。

### 排除范围

- 不恢复 Pose IR、整角色 Program／Projection、独立 TreeRunner、Timeline IR、第二 Preview Session 或备用执行路径。
- 不改变 Foot／IK／Blend 数值算法、角色配置、动画时钟与输入回放语义；发现业务问题列出同级方案及成本，不借重构擅自改行为。
- 不因为文件大就拆接口、增加 Manager、分多个 partial 或搬成薄转发；不为假设变化点新增抽象。
- 不自动创建 goal、worktree，不新增或修改测试；不因本规划启动 Unity、Player、构建、replay 或采集。
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

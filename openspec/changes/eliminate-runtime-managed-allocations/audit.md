# 全项目托管分配与相关 CPU 开销审计

本文件是当前审计入口，最近更新为 2026-09-28。区分托管分配、持续 CPU 工作和首次使用成本，不把减少分配直接等同于减少耗时。最初完成只读审计，随后用户授权按小步修改运行代码；下文 AP 条目记录当前实施范围。本次续查仅做静态检查，没有启动 Editor/Player/服务器、编译、编写测试或采集性能；没有新的 bytes/frame、GC 次数或耗时数据。

2026-09-20 至 2026-09-23 的实施长记录已局部归档到 [历史实施记录](audit-implementation-history-20260920-23.md)。原检查结果、失败记录和未完成边界完整保留；本次不是归档整个 OpenSpec change，不改变 tasks.md 勾选。历史记录中标明未完成的父项仍然未完成。

## 范围与可复查依据

扫描 `rg --files --hidden` 在 3cDemo 与 Tools 返回的 C# 源码，排除 Library、Temp、obj、bin、.git 并遵循仓库忽略规则。忽略的第三方源码、Packages 缓存、仅 DLL/native 插件和未加载产品不冒充已逐行审计。目录名 Runtime 内仍可能有 Editor 条件代码。统计中的候选不是缺陷数量。

共 5451 个源码文件、2180 个候选文件、89 个归属分组。完整分组计数、候选行和当时文件 SHA256 在 [source-inventory.json](source-inventory.json)。源码基线 HEAD 为 `cdb06993e86e421951edb84ae4ac4cd15fd4c75f`，同时包含当时未提交工作区内容，以各文件 hash 区分。

## 已核对的代表性路径

本节 G01–G21 仅保留初始审计快照，不是当前待修列表。后续小步见历史实施记录；本轮复核状态见下文，尤其 G07/G08 的原分配入口已收口，不能继续按未修复统计。其它领域本轮没有逐项复核，不以历史文字替代当前代码结论。

| 编号 | 领域 | 代码位置 | 触发边界 | 判断 |
| --- | --- | --- | --- | --- |
| G01 | 模拟装配 | [FixedCharacterEvaluationRuntime.cs:41](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/Core/Fixed/Execution/FixedCharacterEvaluationRuntime.cs#L41) | 已核对每角色每次 Evaluate | 新建集合和工厂；对每个已安装技能创建 invocation/workspace。Float32 同类入口存在。 |
| G02 | 状态事务 | [FixedCharacterRuntimeStateTransaction.cs:60](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/Core/Fixed/State/FixedCharacterRuntimeStateTransaction.cs#L60) | 每次建立角色事务 | 字典及 state.Clone；另有能力状态字典复制。不能删复制而破坏提交隔离。 |
| G03 | 结果所有权 | [FixedAbilityInvocationRuntime.cs:243](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/Core/Fixed/Execution/FixedAbilityInvocationRuntime.cs#L243) | 每次 Complete | 结果对象和事实/表现/Trace 三个 List 副本。 |
| G04 | 递归求值 | [FixedValueRuntime.cs:352](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/Core/Fixed/Execution/FixedValueRuntime.cs#L352) | 达到本 workspace 新深度 | 创建输入缓冲；List/HashSet 可扩容。G01 每次重建 workspace，因此不是仅启动一次。 |
| G05 | 调用上下文 | [FixedAbilityExecutionFrame.cs:185](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/Core/Fixed/Execution/FixedAbilityExecutionFrame.cs#L185) | 每次 PushActionTraceContext | class scope 分配，创建入口不以诊断开关为前提。 |
| G06 | 诊断取值 | [FixedValueRuntime.cs:266](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/Core/Fixed/Execution/FixedValueRuntime.cs#L266) | 诊断启用且节点符合筛选 | 格式化值及字符串；主链关闭诊断后 G01–G05 仍存在。 |
| G07 | Pose 混合 | [CharacterPoseNativeBlendStackHandler.cs:74](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeBlendStackHandler.cs#L74) | 每次 PrepareFrame | 新建请求 List；有选中源后再建 HashSet。纯方法事实明确，具体场景调用次数待采样。 |
| G08 | Pose 资源目录 | [CharacterPoseNativeDomainResourceCatalogs.cs:51](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeDomainResourceCatalogs.cs#L51) | 每次 getter，调用频率待核实 | Values.ToArray 返回副本；若仅准备阶段调用不能计为每帧热点。 |
| G09 | Timeline 快照 | [TimelineRuntimeService.cs:363](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/BTSMTL/Timeline/Runtime/TimelineRuntimeService.cs#L363) | 每次快照构造 | 活动 Clip、退出决策集合分别复制并包装只读集合；必须保留回滚私有状态。 |
| G10 | 回滚输出 | [RollbackOutputCommitter.cs:92](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/DeterministicRollback/Pipeline/RollbackOutputCommitter.cs#L92) | 每次 Commit | 复制输出字典并创建操作列表，重放批次影响工作量。 |
| G11 | 网络编码 | [RollbackInputCodec.cs:18](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/DeterministicRollback/Protocol/RollbackInputCodec.cs#L18) | 每次 WriteInput/WriteBundle | writer 和 ToArray；ReadInput/ReadBundle 还回写编码比较。协议合法性不能为减分配而取消。 |
| G12 | 世界求解 | [DotRecastWorldSolver.cs:252](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/DotRecast/DotRecastWorldSolver.cs#L252) | 每次 ResolveBatch，限此 Solver | 接触候选集合；不同 Solver 单独核对，不能把 DotRecast 结果套给 KCC。 |
| G13 | 远端表现 | [ServerAuthoritativeRemotePresentationHost.cs:447](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Networking/GameplayNetwork/ServerAuthoritative/ServerAuthoritativeRemotePresentationHost.cs#L447) | 执行到相应到期处理时 | 到期列表分配；消息批次另有样本集合构建。网络模型可达性需按 Variant 实测。 |
| G14 | 服务端事件转发 | [ServerAuthoritativeAuthoritySceneHandlers.cs:69](../../../3cDemo/Server/Products/DotRecastAuthority/Hotfix/ServerAuthoritative/ServerAuthoritativeAuthoritySceneHandlers.cs#L69) | 每个合法可靠事件批 | 列表、消息对象和 payload.Clone；必须与 Fantasy 消息/异步发送释放时机一起迁移。 |
| G15 | 资源状态观察 | [ProductResourceRuntime.cs:76](../../../3cDemo/Client/3C_Client/Assets/GameScripts/HotFix/GameLogic/ProductResource/ProductResourceRuntime.cs#L76) | 每次属性访问，UI刷新频率待核实 | 历史 ToArray；资源加载 async 分配另归生命周期，不能全归每帧。 |
| G16 | 启动状态观察 | [ProductRuntimeContracts.cs:178](../../../3cDemo/Client/3C_Client/Assets/GameScripts/HotFix/GameLogic/ProductStartup/ProductRuntimeContracts.cs#L178) | 每次属性访问 | 历史 ToArray；产品启动未完成不等于当前 GameplayLab 一定调用。 |
| G17 | 输入采样 | [UnityFixedCharacterInputAdapter.cs:239](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/Unity/Fixed/UnityFixedCharacterInputAdapter.cs#L239) | 每次执行对应采样方法 | 集合虽为字段复用，仍构造输入对象；CaptureState writer/ToArray 按快照调用计。 |
| G18 | 相机正向样本 | [CharacterCameraFramePlanner.cs:13](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Camera/Solver/CharacterCameraFramePlanner.cs#L13) | 实例字段初始化 | 已复用集合，不是无条件每帧 new；容量与外部返回值仍需测。 |
| G19 | Foot 查询正向样本 | [CharacterFootPlacementWorldQueryBackend.cs:96](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/FootPlacement/CharacterFootPlacementWorldQueryBackend.cs#L96) | 构造/准备阶段 | 按配置容量持有查询数组；new 结果结构不能直接等同托管分配，IK/vendor 边界待测。 |
| G20 | 渲染正向样本 | [RadialBlurRendererFeature.cs:84](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Rendering/Runtime/RadialBlurRendererFeature.cs#L84) | RebuildResources | AddRenderPasses 复用 pass；资源重建和渲染循环分开，RT/Native 分配另记。 |
| G21 | 采样落盘 | [ThirdPersonPerformanceCaptureAgent.cs:670](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Performance/Unity/ThirdPersonPerformanceCaptureAgent.cs#L670) | 导出阶段 | StringBuilder 与 Recorder.ToArray 不能未经调用时序核对计为采集窗口常态；采集自身开销要归因。 |

## 全项目覆盖与未决项

| 范围 | 覆盖情况 | 后续不能遗漏的边界 |
| --- | --- | --- |
| Simulation/Core Fixed/Float32、控制、GE、装备 | 完成候选扫描，深入 G01–G06 及事务克隆 | 状态页、savepoint、效果 journal、设备切换、失败清理；两数值域分别记录 |
| Session/Pipeline/快照/回滚 | 扫描全部可见源码，深入 G02/G10/G11 | 一渲染帧多步、预测/重放、历史保留、Finalize和异步消费者 |
| Timeline、EventGraphs | 扫描直接运行与事件图源码，深入 G09 | Begin/Stop/Pending/Commit/Capture/Restore、Marker dispatch；所有事件图节点未逐项证明 |
| Pose/ACL/Animancer/Blend/惯性化 | 深入 G07/G08，其余候选入账 | 原生图调用、委托、装箱、共享源、首次动作切换和资源释放 |
| Foot/Goal/FBBIK | 查询数组 G19 已核对，算法源码候选扫描 | FinalIK等供应方内部、诊断开关、异常边界；没有据此宣称整个IK零分配 |
| Camera/Input/AI | G17/G18 已核对，行为绑定与原生插件边界列入 | Behavior Designer 正式任务、Input System回调、目标查询、AI刷新；本轮未逐任务复验 |
| World/KCC/DotRecast | G12、KCC查询固定数组候选已区分 | 成对碰撞、导航路径、每批结果数组、solver snapshot |
| Networking/Endpoint/Fantasy | G11/G13/G14 与协议候选扫描 | 序列化、收发 buffer 所有权、重传、心跳、断连清理与线程分配 |
| HotFix/UI/资源/启动 | G15/G16 与可见HotFix全部源码扫描 | TEngine/UGUI/TMP、打开关闭、文本更新、委托订阅、加载与卸载；启动产品未闭环不能视为当前已加载 |
| Rendering/特效/音频 | G20 与项目渲染源码、其它 Assets 候选扫描 | 实例化、材质、Renderer属性、RenderGraph、VFX/Audio事件接收者实际装配；native/GPU内存另记 |
| 服务端 Gate/Authority/Startup/共享Host | 源码扫描，事件处理 G14 深入 | timer tick、房间状态、队列、协议生成器和存储调用；服务端进程需独立指标 |
| Shared/portable | 已扫描共享源码与Tools portable工程 | linked source由同一源文件负责修复，不能创建服务端另一实现 |
| Diagnostics/Performance/GM | G06/G21 与开发工具扫描 | collector本身、ring满、跨线程消费、采样停止与文件导出 |
| Editor/作者工具/Build/离线Tools | 全部纳入清单，非运行常态分组 | OnInspectorGUI重操作禁止；缓存泄漏和重复订阅需生命周期审计，不强行把资产构建做成每帧0 GC |
| 第三方及其它Assets | Plugins、Plugin、ParadoxNotion、KINEMATION等已列清单 | 不是每个vendor文件都是当前依赖；DLL/native和被忽略源码缺口明确保留，不能直接删或改 |

## 分类规则

- 新建引用类型或数组并且方法位于已确认周期入口：重复分配；数量仍需正式采样。
- 字段集合与构造阶段分配：生命周期分配；未查容量前不判每帧分配，也不判永久零分配。
- `new struct`、Span、按值局部变量不因关键字 new 就计入；接口装箱和迭代器需看具体类型/生成代码。
- 异常文本仅在失败分支构造，与每帧调用前已插值字符串分开。
- 内存泄漏、GC Alloc、GC回收停顿和native/GPU内存是不同指标，不能互相替代。

## 当前结论边界

初始审计与后续实施需要分别阅读，不能由初始表推断今天的剩余数量。Pose 图节点集合的周期获取已由 AP03 收口；动态请求容量、首次使用、诊断观察和其它未完成边界仍按下文分别记录。尚不能排列实测收益、报总字节数、保证全部第三方节点零分配，或宣称全项目每条调用链均已人工审计。下列编号用于追踪，不表示实施优先级。

## 2026-09-28 局部归档与复核状态

本轮代码基线：`59a17c56e9f31b6b90e7f3aec1a6b3dec4de07c2`。以下“已收口”只指明示入口，不表示所属整个模块已完成或已取得 Player 性能证据。

| 原条目或历史切片 | 本轮核对结果 | 仍需保留的边界 |
| --- | --- | --- |
| G07、4.2.9：Blend Stack 每帧 new 请求 List/身份 HashSet/PendingSource | 已收口：`CharacterPoseNativeBlendStackSourceModuleBinding` 构造时持有集合，PrepareFrame 清空复用，PendingSource 为值类型 | 内容容量、其它源和整条混合链另算；不能再将旧 List/HashSet 构造计为每帧成本 |
| G08、7.24：SourceCatalog 重建及 Plans/Values 数组副本 | 已收口：工厂持有唯一目录，旧 Plans 数组入口已删除 | 当前 ACL 动作身份查找仍线性扫描，见 AP15；这不是旧数组分配复发 |
| 4.2.7：三个 Player 的单元素请求数组 | 已收口：Clip/BlendSpace/Selected Player 持有请求槽 | 汇总列表容量与请求去重另算，见 AP16 |
| 4.2.8：每帧请求汇总 List 和去重 HashSet 构造 | 已收口的是重复构造；Evaluator 持有汇总列表，SourceDemand 按前序请求比较 | 去重变成平方比较是原记录明确的取舍，保留 AP16，不撤销已有正确改动 |
| EventGraph 正常更新与发布，提交 `032794d72` | 类型化读取器、双页输出、值类型帧和合同共享已经落地；现有 [实施说明](../extend-modify-bone-and-add-corin-lean/implementation.md) 记录了 Mono 独立实例预热后的零分配测量 | 本轮没有重测；不涵盖 Pose 图拓扑访问、首次建图、所有节点、Player/IL2CPP 或整角色 |
| Pose 顶层输出预绑定、运行端口定义缓存、输出包装复用 | 保留历史已完成结论，当前原生输出按节点/端口/阶段缓存 | 子图边界仍逐帧查找，见 AP04；FlowCanvas 纯函数不等同于原生 Pose 输出，见 AP12 |
| 7.53：删除构造器内第二遍 Rig/RootHierarchy 校验 | 已收口的是装配重复检查 | 每帧发布和物理写回的重复遍历仍存在，见 AP10；不能混称旧改动未完成 |

## 动画链当前开放项

每项说明输入、当前工作、可讨论的改法及业务取舍。这里记录审计发现，不授权修改运行行为，不新增手动验证任务。来源和行号按本轮代码基线记录。

### AP01 固定参数合同仍逐帧校验（已实施，未实跑）

- 证据：[CharacterAnimationPoseInputFrame.cs:25](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/Contracts/Pose/CharacterAnimationPoseInputFrame.cs#L25)，由表现域 RunPoseFrame 调用。
- 输入是已发布变量帧和绑定时确定的参数 ID 列表；每帧查参数存在，并两层循环查重复。参数数量为 P 时，重复检查为 P(P−1)/2 次比较，还叠加名称查找。
- 可在正式装配时校验固定声明，帧内只验发布身份及失效状态。角色表现不变；代价是换图或换合同时必须重新装配，不能缓存跨版本的绑定。
- 实施结果：BindPoseDomain 一次校验 Control 声明存在和唯一性，保存正式变量合同；FromPublishedVariables 每帧只检查当前发布有效及源合同引用匹配。删除长期保存的参数 ID 数组与帧内双层循环。

### AP02 参数名称仍重复线性查找（主运行与lean采样已实施，未实跑）

- 证据：[EventGraphContracts.cs:459](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/BTSMTL/EventGraphs/EventGraphContracts.cs#L459) 的变量布局名称解析。
- 输入是变量名，输出是同帧变量；多个 Pose 消费者和诊断重复比较字符串。零分配发布已经完成，并不消除读取端的查找成本。
- 可在装配时解析带布局身份的位置，运行时直接读值。变量多、角色多时减少重复查找；代价是布局替换后位置失效，不能只缓存裸整数。
- 实施结果：正式 EventGraphVariableBinding 保存布局对象身份与位置，读值同时检查发布版本和布局引用；Pose 参数节点、Action Slot 标量参数在装配时绑定，帧内直接按位置读。Character Host 直接包装 NativeEventGraphRuntime 的唯一变量合同，删除独立重建的同名合同。仍按名称读的消费者改走布局持有的 Ordinal 字典，旧线性扫描删除。
- 采样续步：会话装配时绑定 lean 的七个变量位置，两种采样事件共用同一个构造入口；帧内不再按名称逐字段解析。没有 lean 声明的图仍输出 LeanAvailable=false；有声明却缺其它所需字段时在绑定阶段明确失败。未接入正式工厂的 BlendSpace 类不假称已改成位置读取；其它按名称访问已走同一布局字典。布局替换不会静默复用旧 binding，旧帧失效机制保留。
- ParameterResolve 续查：原 ResolveParameters 每次对每条固定策略调用 FindParameterIndex，线性扫描 InputContract.Parameters，并反复读取作者策略声明。现 Initialize 按原策略顺序保存 ParameterId、位置和策略枚举；缺失参数、输出布局越界及非法策略在装配时失败。逐帧仍读取本次 base/source 值与可用性，沿原 Base/Overlay/Weighted/Max/Min 公式合成，不缓存参数值、不合并重复策略。代价是每节点一份固定策略数组，修改图/合同需重新装配；骨骼页遗漏的独立正确性修复见下文。仅静态核对，没有该节点实际执行次数或耗时数据。

### AP03 图节点 getter 创建周期数组（已实施，未实跑）

- 证据：[CharacterPoseCanvasGraph.cs:35](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/Contracts/Pose/CharacterPoseCanvasGraph.cs#L35) 的 Nodes 每次执行 OfType/ToArray；[CharacterPoseNativeGraphEvaluator.cs:238](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeGraphEvaluator.cs#L238) 每次 PrepareFrame 读取它。
- 图拓扑未变也会重新筛选并生成数组；每个实际执行 PrepareFrame 的实例都会经过此入口。不是看到初始化 new 就猜成每帧分配。
- 可由运行实例在装配时持有固定节点/handler 集合。业务节点顺序不变；代价是编辑器变更后必须明确重装配，不能把作者可变集合直接作为不受约束的运行数据。
- 实施结果：GraphRuntime 初始化时获取一次实例节点集合；Evaluator 按原节点顺序准备 node/handler 对应表，PrepareFrame 直接遍历。其它帧阶段使用按原 handler 顺序筛选的活动列表，删除逐阶段可达性查找；Start/Reset/Stop/Dispose 仍覆盖原完整 handler 集合。作者资产 getter 未加可失效的全局缓存。

### AP04 子图固定边界和端口映射仍逐帧解析（已实施，未实跑）

- 证据：[CharacterPoseNativeSubgraphHandler.cs:299](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeSubgraphHandler.cs#L299) 在 Prepare/Evaluate 两阶段调用 BindInputs；[CharacterPoseNativeGraphRuntime.cs:558](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeGraphRuntime.cs#L558) 每个输入绑定又扫描 GraphInput；子图输出还有 FindBoundary/ReadGraphOutput 扫描。
- 输入为同一子图实例的新帧值，重复工作是寻找不变的边界、接口端口和连接。仅把 Nodes 换成数组缓存，仍保留这些扫描。
- 可装配时建立父输入到子输入的正式绑定。代价是维护绑定生命周期；必须保留普通参数先绑定、姿势等输入延后绑定的时序，不能提前求姿势来省扫描。
- 实施结果：GraphRuntime 准备端口定义时固定 GraphInput/GraphOutput 边界；Subgraph 在 Start、即 GatherPorts/BindPorts 完成之后建立两组输入映射和输出映射，帧内只读取当前值并传给子图。保留当前帧身份、重复绑定检查和原 Prepare/Evaluate 顺序。旧 FindBoundary 与按 PortId 查动态端口的循环入口删除。

### AP05 首次执行和首次状态进入仍创建实例（状态图策略和EventGraph初次准备已实施）

- 证据：[NativeEventGraphRuntime.cs:91](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/BTSMTL/EventGraphs/NativeEventGraphRuntime.cs#L91) 首次 Execute 克隆/启动图并准备变量输出；[CharacterPoseNativeStateMachineSource.cs:402](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeStateMachineSource.cs#L402) 在 PrepareFrame 调用 EnsureState，未出现过的状态创建子图。
- 创建期间 [CharacterPoseNativeGraphEvaluator.cs:435](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeGraphEvaluator.cs#L435) 的可达性扫描反复访问 Connections；其 getter 每次收集、去重、排序、生成数组。此处不能统计成每个稳定帧都执行。
- 装配时预建可把首次动作成本移出游玩帧，但增加准备时间和常驻内存；按需创建减少未使用状态占用，却保留首次进入成本。准备期反射本身在用户允许边界内，审计问题是创建是否落在游戏更新中。
- Evaluator 初始化取一次连接集合，供可达性和输出绑定共用。状态图创建时机进入正式 Presentation Profile：`DuringPreparation` 在 handler Start、端口绑定完成后递归创建状态子图，`OnFirstEntry` 在首次进入时创建并保留。两种模式使用同一个 EnsureState/CreateChild 和原有 Reset/Dispose；新配置与 Corin 正式资产选择准备时创建，作者可在 Profile 切换。非法枚举报错，不存在隐式降级。
- 业务取舍：准备时创建增加角色准备耗时与常驻图内存，移除正常状态首次进入的建图工作；首次进入模式节省未用状态的图内存，但不能保证首次进入零分配。创建不推进状态时间，也不提前执行状态帧。创建失败仍沿原装配失败与 Dispose 链处理。修改配置在下次装配生效，不支持运行中悄悄切换。
- 资源仓库已有异步准备、租约和按预算淘汰闲置 ACL 组，但该配置仅控制图实例，未实现状态资源缺失时的等待/切换合同；不能称为完整流式。LOD 的更新频率、骨骼/IK裁剪也没有被此配置替代。
- EventGraph 构造时准备实例、端口、宏子图和变量输出读取器；Start 事件仍在首次真实 invocation 的 BeginInvocation 之后执行，Update 仍只执行一次，没有伪造输入或提前执行 Start。准备失败释放克隆。Reset 的销毁/重建语义保留，Reset 后首次调用仍有创建成本，Start 的节点回调成本也保留，AP05 未全部完成。

### AP06 无影响 Modify Bone 仍复制并重建（计算已实施，独立页复制保留）

- 证据：[CharacterPoseNativeModifyBoneHandler.cs:149](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeModifyBoneHandler.cs#L149)。外层复制元数据和姿势；变换执行还涉及后代与虚拟骨骼。
- 输入权重为零，或最终变换没有影响时，可减少变换计算和重建。判断应基于通用变换模式，不能为 Corin 写角色专用捷径。
- 业务要求是直跑回正后的姿势与身份不变。必须保留独立输出页、CompletionIdentity、Commit/Discard；不允许直接外借输入指针来绕过正式生命周期。
- 实施结果：先读取并校验本帧变换参数；权重零、全部 Ignore 或中性 Add 直接复制已验证输入到自己的输出页，不再构造整副组件空间 scratch 或反复归一化。Replace/非中性变换在求出目标后按精确分量比较；无变化则不保存/重建后代与虚拟骨骼。没有引入浮点阈值，也未省略动态输入合法性检查；输出页和元数据仍正式提交。

### AP07 Blend Pose 在端点仍求两侧姿势并全骨骼混合（端点骨骼计算已实施，源采样保留）

- 证据：[CharacterPoseNativeBlendPoseHandler.cs:115](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeBlendPoseHandler.cs#L115) 先读取 base/overlay，再解析权重；BlendPose 的逐骨骼循环没有权重 0/1 分支。
- 可减少端点时的骨骼插值、旋转归一化和速度混合。但现行参数合并允许另一侧补充缺失参数，单边返回未必等价。
- 只省骨骼混合较易维持时间推进；进一步裁剪源采样可能省更多工作，但必须定义播放器时间、相位、过渡、缺参和连续性行为。不能仅凭权重为零停掉整个分支。
- 实施结果：有效 base/overlay 权重有一侧为零时，骨骼姿势与速度批量复制到独立输出页，不再执行无意义的加权、除法与旋转归一化。两侧输入仍按原顺序求值，参数缺失补充、有效贡献、脚部特征、连续性和不连续事件仍按原逻辑计算。没有把参数处理简化为单边复制，也没有引入按帧缓存或停播放器。端点减少了浮点往返运算，不承诺与旧重复归一化后的结果逐位相等。

### AP08 通用元数据复制按容量搬运，计算节点随后覆盖（主要计算节点已拆分继承与生成）

- 证据：[CharacterPoseNativeSpaceConversionHandler.cs:48](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeSpaceConversionHandler.cs#L48) 的 CopyMetadata 复制骨骼速度、参数、贡献表及贡献×骨骼权重，并清零容量尾部；Blend Pose 在调用后又计算写回其中多项。
- 输入页和输出页布局相同不表示所有字段都要先复制；可让计算节点直接写自身负责的结果，只复制继承字段。
- 业务不变的前提是消费者严格按有效数量读取。取消尾部清理须先统一这一合同；跨节点共享不可变数据需要正式页寿命支持，不能引入第二条借用路径。
- 实施结果：公共复制使用 NativeSlice 批量复制速度、参数、可用性及有效贡献/权重；贡献只复制有效数量，尾部统一清零，不传播旧页无效尾部记录。Blend Pose 已完整写回全部输出字段，因而删除其“先复制再覆盖”，只保留布局校验和输出贡献尾部清理。其它节点仍经同一 CopyMetadata 继承需要的字段，未引入共享页或绕过 Commit 的借用。
- 续实施：Layered Bone Blend 与状态过渡直接生成各字段，不再预复制随后覆盖；生成贡献后统一清理尾部。公共 CopyAttributes 只继承参数、贡献、足部和状态信息，CopyMetadata 在其上增加速度复制。Additive 使用 CopyAttributes，速度由自身生成；惯性节点仅在不施加残差时复制骨骼与速度，激活残差时直接生成二者，仍逐帧记录完整历史。Additive 零权重以及 Layered 的单侧骨骼权重为零时复制有效侧骨骼，保留两侧求值、参数补缺、贡献与连续性规则；浮点结果不承诺与旧归一化过程逐位一致。
- 纯复制续收口：EntryPose、LinkedPose、MotionMatching、StateMachine 输出桥、AnimationSlot 的透传/NoPose、RootOrientationWarp 输入及 FullBodyIK 工作页的7处逐元素姿势复制改为 NativeSlice.CopyFrom。原长度/布局预检仍在复制前，修改/求解仍在独立输出页进行；没有改成共享输入页或跳过逐骨骼有效性检查。

### AP09 惯性混合七组残差无条件复制（已实施，未实跑）

- 证据：[CharacterPoseNativeInertializationHandler.cs:192](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeInertializationHandler.cs#L192) 每帧复制位置、旋转、缩放、三类速度与参数残差；写入残差的实际计算集中在 BeginTransition。
- 稳定播放、没有正在进行的过渡时也搬运整组数据。可按是否产生新残差管理待提交页，持续过渡阶段读取原残差和新的 elapsed。
- 下一次过渡仍依赖连续历史姿势，CommitHistory 不能一并停掉。代价是明确残差只读期与新过渡写入期，并保留丢帧不污染已提交数据的规则。
- 实施结果：七组数组归入两页 ResidualPage。新过渡完整重算待提交页，计算成功才标记新页；持续过渡读取已提交残差，Commit 仅在新页产生时交换残差页，Discard 不交换。BeginFrame 的七次 Array.Copy 删除；历史姿势、速度、参数和脚部历史仍沿原来每帧提交更新。

### AP10 Final Pose 发布与物理写回重复遍历（写回解析与调用已实施，预检保留）

- 证据：[CharacterFinalPoseNativePublication.cs:283](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/Final/CharacterFinalPoseNativePublication.cs#L283) 校验/复制所有骨骼；[CharacterFinalPosePhysicalWriter.cs:87](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/Final/CharacterFinalPosePhysicalWriter.cs#L87) 再遍历解析/校验，然后第三遍解析/写入，位置、旋转、缩放分别调用 setter。
- 可统一使用已验证发布页，减少重复解析；评估位置/旋转合并写入。缩放是否可省必须依据正式动画和骨骼写入所有权，不能假设永远不变。
- 必须保留全量预检后写入，不能改成逐骨骼检查后立即写而留下半帧姿势。引擎调用成本尚未测量，减少调用数量不等同于已证实总帧耗时下降。
- 实施结果：物理 writer 按 Rig 骨骼数准备值类型写入数组，预检时一次解析并存放所选姿势，全部预检成功后直接消费该数组。位置/旋转用 SetLocalPositionAndRotation 合并写入，缩放仍写；发布页的有限性检查与写入前 Transform 存活检查均保留。增加一副定容本地姿势数组，换取删除第二次 ResolvePose 和减少逐骨骼引擎调用。

### AP11 诊断关闭后仍有字段采集与观察字典维护（采样拆分、字典移交及普通Player编译裁剪已实施）

- 证据：[CharacterPresentationDomainRuntime.cs:716](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/CharacterPresentationDomainRuntime.cs#L716) 无条件构造含 lean 读取的诊断帧；[CharacterFinalPosePhysicalWriter.cs:116](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/Final/CharacterFinalPosePhysicalWriter.cs#L116) 在足部采集开关外读取骨盆/双踝 Transform 并转换空间。
- [CharacterPoseNativeGraphRuntime.cs:1069](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeGraphRuntime.cs#L1069) 每个首次求值输出写观察字典，CommitGraphOutput 再逐项复制到已提交字典，不以观察订阅为前提。
- 可沿现有订阅按需采集，但 PhysicalWrite.IsAvailable 当前参与提交校验：须把写回成功身份与诊断坐标分开，保留故障与 Commit 证明。不能整段关闭而破坏发布合同，也不能让关闭观察后返回上次残留字段。
- 实施结果：原生表现采样未订阅时不构造诊断帧，清除该帧诊断结果；发布时检查诊断帧对应当前 renderFrame，避免订阅在帧中变化后发布旧数据。节点观察仍保留原读取能力，但 Commit 改为交换工作/已提交字典并清空旧页，删除逐项复制；Discard 不替换已提交字典。
- 用户明确普通运行不做采样，采样由编译选项控制。物理写入现在返回完成身份，publication 在写入与 Commit 时核对当帧身份；足部世界坐标采集、缓存和提交复制仅在 `KK_DIAGNOSTIC_SAMPLING && KK_DIAGNOSTIC_FOOT` 编译分支存在，且仅有订阅时读取 Transform。采样包含原有根、骨盆、双踝及双脚趾字段。旧 `AnimationPhysicalBoneWriteDiagnostics` 删除，不再把诊断结构作为业务提交证明。
- 行为边界：本地姿势全量预检仍保留；普通提交不再借诊断坐标拦截父变换引起的异常世界坐标。节点观察尚无正式订阅寿命合同，Editor 与采样构建保留原观察能力，不增加运行时订阅开关。
- 2026-09-29 续查：沿用户“正常运行没有采样工作、由编译选项控制”的要求，将节点观察类型、接口、Graph/Node/Role/Session/Domain 读取链，以及工作/提交字典的创建、扩容、写入、清理和交换统一放入 `UNITY_EDITOR || KK_DIAGNOSTIC_SAMPLING`。普通 Player 不再整理每个节点的观察字段或维护观察字典；接口整体不编译，不用返回默认结果的空实现替代。Editor 和开启采样宏的 Player 保留原记录及读取行为。
- 业务 `m_OutputCache`、求值环检测、帧身份、最终姿势读取和 Commit/Discard 均保留；普通构建只去掉专供观察记录的 catch 后立即 rethrow，原 finally 仍移除求值中标记，异常继续向原帧边界传播。静态追踪全部观察类型与读取接口，核对预处理条件覆盖及差异；未执行任何编译或运行，不以静态条件核对代替多构建配置验证。开启采样宏但无人读取时，节点观察仍维护完整记录，这部分尚未做订阅生命周期设计。

### AP12 FlowCanvas 共享纯计算可能重复执行（取决于正式图连接）

- 证据：[PureFunctionNode.cs:26](../../../3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/Nodes/Functions/Implemented/PureFunctionNode.cs#L26) 按读取触发计算。仅在多个消费者读取同一计算结果时构成重复工作，不能假定每个图都命中。
- 可在图中显式保存确实复用的中间结果，代价是作者维护少量变量和明确写入顺序。同一帧 SetVariable 后必须能读到新值，禁止整帧缓存。
- 本轮解析 Corin 正式 EventGraph 的序列化连接，已确认共享纯计算：`calculate.velocity-planar` 有4条输出连接，`calculate.horizontal-speed`、`calculate.vertical-speed`各2条，`lean.eligible`有3条，`lean.direction-angle`与`lean.return-before-switch`各2条。连接数不等于实际每帧执行次数，分支与下游重复读取还会影响次数。历史方向、当前倾角在同一更新中有写入，不能以节点共享为由做整帧缓存。当前尚未改图资产；这项需要沿正式 authoring 链重接写入与读取顺序，不直接编辑序列化连接或另建运行时缓存。
- 原生 Pose 已有 [节点/端口/执行阶段输出缓存](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeGraphRuntime.cs#L1056)，不应再为它添加重复的整帧缓存。

### AP13 ACL 每帧重设不变 Clip Job，并全容量清权重（已实施，未实跑）

- 证据：[CharacterAclSourceGraph.cs:105](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/ACL/CharacterAclSourceGraph.cs#L105)。每次 Apply 遍历 clipCapacity，重建值类型 Job 并 SetJobData，然后所有输入权重设零，再设当前活动权重。
- Clip Job 的 handles、解压值数组和骨骼映射在构造时已绑定，SourceInstance 更新数组内容，没有每帧更换这些容器。这里的 new Job 是 struct，不作为托管 GC 证据；问题是重复校验和引擎调用。
- 可保留装配时的 Clip Job 绑定，只维护活动输入权重；从上一批退出的输入必须清零。业务上减少未活动 clip 槽位的工作，代价是记录上次活动槽。Capture Job 携带每帧页绑定，不能一并停止更新；ClearInputs、Discard 和池复用也必须重置活动记录。
- 实施结果：Clip Job 只在构造 Playable 时绑定；新增按正式 clipCapacity 准备的活动索引数组，Apply 只清上次活动输入并写本次权重，ClearInputs 同时归零活动数量。Capture Job 仍每帧绑定当前捕获页。已核对 SourceInstance 的 ResetForReuse 沿同一 ClearInputs 清理，没有改动解压、混合顺序或池复用入口。

### AP14 足部支撑候选完整排序，但只消费最优项（已实施，未实跑）

- 证据：[CharacterFootPlacementWorldQueryBackend.cs:523](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/FootPlacement/CharacterFootPlacementWorldQueryBackend.cs#L523)。过滤支撑候选后插入排序全部 validCount；落点查询读取 m_LandingHits[0]，当前支撑查询读取 m_CurrentSupportHits[0]，其它输出是数量和拒绝统计。
- 在只需要最优项的当前消费者合同下，可扫描保留最小项，比较由最坏平方次数降为线性；候选很少时实际收益可能很小。
- 必须沿原 CompareCurrentSupport 的完整规则选择，完全相等时保留先出现者；容量溢出、坡度、自碰撞过滤、候选 raycast 和统计均保持。球扫后的 collider.Raycast 用来确认真实支撑位置，不因“看起来查了两次”而删除。此项不修改其它窗口正在调整的脚部业务代码。
- 实施结果：过滤阶段同时保留按原 CompareCurrentSupport 比较的最小候选，末尾只写 hits[0]；相等时不替换，保留原稳定排序的首项。候选总数、有效数、两类拒绝统计及全部物理查询不变。删除完整插入排序；没有更改脚部目标、骨盆、权重或求解业务。

### AP15 ACL 动作足部来源每次解析扫描全部动作目录（已实施，未实跑）

- 证据：[CharacterPoseNativeDomainResourceCatalogs.cs:91](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeDomainResourceCatalogs.cs#L91) 对 ACL ClipSample 按 ResourceCatalogIndex/GroupClipIndex 遍历 m_ActionPlans.Values；[CharacterPoseNativeDomainServiceFactory.cs:222](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeDomainServiceFactory.cs#L222) 的 ResolveFootMotion 在 Timeline 贡献路径调用它，委托由 CharacterPoseWorldContextAdapter 消费。
- 输入是已经确定的 ACL 资源/clip 身份，输出为正式动作源计划；动作目录越大，单次足部来源解析比较越多。仅 Timeline 且 ACL 的对应路径命中，不能算成所有 locomotion 帧的全目录查找。
- 可装配时建立同一目录内的复合身份索引。增加一份查找索引内存，换取不随动作总量增长的查找；装配时须明确重复 ACL 身份是否允许，不能擅自从原首个匹配变成另一动作。该索引引用同一正式计划，不创建第二数据源。
- 实施结果：在同一目录构造与 ACL manifest 校验阶段，按原 m_ActionPlans.Values 顺序建立资源/clip 二元键索引，键重复时保留原首个匹配项；运行时一次 TryGetValue。索引只引用原计划，不复制计划、不新增配置、不更改非 ACL 入口或缺失报错。

### AP16 动态源请求去重与汇总容量（已实施身份集合和装配期请求上界，未实跑）

- 证据：[CharacterPoseNativeRuntimeContracts.cs:697](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/Contracts/Pose/CharacterPoseNativeRuntimeContracts.cs#L697)，每条请求与所有前序请求比较 ScopeInstanceId/NodeId/SourceId。子图请求汇总到父图后，还会经过父图 demand 校验。
- 这是历史 4.2.8 删除每帧 HashSet 构造时主动选择的实现，已经没有原构造分配。与 AP01 不同，这里的活动源随状态和过渡变化，不能简单移到装配时查一次。
- 实施结果：每个图实例持有一个 `HashSet<CharacterPoseNativeSourceDemandKey>`，每帧清空复用，按完整的 ScopeInstanceId/NodeId/SourceId 三元身份检查重复请求。删除可选 HashSet 参数及空参时的旧平方扫描分支；全仓 C# 仅有 GraphRuntime 一个构造调用者。请求顺序、重复报错和子图汇总语义不变；不再为每条请求扫描全部前序请求，也不在运行帧新建 HashSet。
- 最初的节点数乘二、handler 数及 contributionCapacity 估算已删除。DomainServiceFactory 在装配期依据正式图引用和源策略生成唯一 SourceRequestLayout，通过实例 Context 传给根图及全部子图。单播放器按1条请求，BlendStack/AnimationSlot 按 StackPolicy.MaxActiveSourceEntries；每条子图调用边分别累计，同一模板复用不会被只算一次。状态机按不同状态中最大的两个请求容量之和预留，覆盖当前态与过渡目标同时活动；自过渡、单状态及零源图同样被覆盖。循环引用、整数溢出及缺失图会在准备时失败。
- GraphEvaluator 与 StateMachineSource 汇总请求改用现有 FixedCapacityFrameBuffer；正常帧按下标追加和清理有效部分，超出声明上界明确报错，不自动扩容。图 HashSet 按同一图上界预分配，接收的请求已受汇总缓冲容量约束。各图保守包含所有节点而非仅当前可达节点，代价是固定常驻容量；没有新增手填配置、预执行状态或改变 DuringPreparation/OnFirstEntry 策略。后者首次创建状态图本身仍有生命周期分配。
- 静态容量依据：三个 Player 的请求数组长度为1；BlendStackSourceBinding 的请求数不超过 stack.EntryCount，EntryCapacity 来自同一 MaxActiveSourceEntries；Subgraph 透传子图请求；CollectActiveStates 最多返回2个不同状态；其余当前装配的计算节点不发请求。LinkedPose/MotionMatching/EntryPose 的实际工厂仍明确拒绝未装配实现，不能将本结论扩大到未来外接来源。新增来源须随正式装配同步声明请求容量。最终贡献页容量是不同合同，未将其替换为请求数量。
- 状态机 PrepareEvaluation 原先逐条扫描父 demand.Requests，确认子请求没有在进入求值屏障前丢失。现由 Demand 私有引用本图已建立的同一个身份集合并提供 Contains；沿原 ScopeInstanceId/NodeId/SourceId 比较，不新增集合、不重复建索引，原缺失报错、请求顺序和 Required/SourceSlot 语义不变。每图有自己的集合，子图 Prepare 不会清父图集合；状态机只在已校验当前 lineage 的 PrepareEvaluation 内查询，集合在下一次本图 Prepare 时复用，与原 Requests 列表的有效期相同。静态核对唯一构造入口和全部消费者，未实跑。

### AP17 惯性包络反复计算固定曲线端点导数（续查并实施，未实跑）

- 证据：CharacterPoseNativeInertializationHandler.EvaluateEnvelope 对每根骨骼以及参数/脚部包络重复计算同一条编译曲线在 0 和 1 的导数；曲线由构造阶段 CompileCurve 固定。
- 实施结果：两个端点导数在构造阶段计算并持有，逐帧仍计算当前 normalized 对应的曲线值和导数。曲线公式、计算项顺序和过渡时间不变，减少的是固定端点求值；没有新增运行分配，也没有缓存变化中的当前采样值。

### AP18 图求值集合容量在首次求值时增长（本轮续查，已实施）

- GraphRuntime 的输出缓存、工作/已提交观察字典、循环检测集合和边界输入字典原本从空容量开始。预建图并不代表这些容器已准备好，因此初次求值仍可能分配。
- BuildPortDefinitions 统计正式输出端口及 GraphInput 输出数量；按 Prepare/Evaluate 两个阶段的输出键总数准备集合容量，边界输入按接口端口数量准备。保留观察功能、循环报错、帧清理和字典交换，不通过关闭观察来消除分配。增加装配期常驻容量；未证明所有节点自身的首次结果包装与动态源集合已零分配。

### AP19 相位播放器遍历反复筛选全部活动节点（本轮续查，已实施）

- GraphEvaluator 的 PhasePlayerCount 与 ReadPhasePlayer 原先扫描每个活动 handler 并判断是否提供相位播放器；嵌套状态同步会反复调用。
- 初始化时按原活动顺序绑定相位源列表，运行时只访问这些源。各状态当前播放器数量仍动态读取，没有缓存跨状态变化的播放器或停止时间推进。

### AP20 Provider 与 Selected Player 捕获临时样本对象（本轮续查，已实施）

- `CharacterPoseSourceModule.ResolveProviderSample` 原先每次创建 `AnimationResolvedPoseSourceSample`；Blend Stack 的 PrepareFrame 和 PrepareNativeProviderSource 各调用一次。`AnimationSelectedPosePlayerRuntime.PrepareCapture` 也每次构造同类临时包装。它们是 class 分配，与值类型请求的 new 不同；是否在当前内容命中及具体次数未实测。
- 改为 `CreateProviderRequest` 返回既有值类型请求；Blend Stack、Selected Player 和 Source Workspace 沿同一捕获入口传请求与左右足特征，删除临时包装构造器及旧 ResolveProviderSample。持续持有的 Action Slot 样本仍在装配期创建并复用，不使用共享可变临时对象，也不外借生命周期不明的缓存。请求、捕获身份、参数和足部有效性检查保留，底层工作页与 Commit/Discard 不变。
- 静态搜索确认剩余 `new AnimationResolvedPoseSourceSample` 仅在 Action Slot 构造阶段。已核对捕获调用点及差异；未执行编译或分配采样。

### AP21 源绑定页每帧按三组总容量清空（本轮续查，已实施）

- `CharacterPoseSourceBindingPage.Clear` 原先全量清空 Direct、Clip、BlendSpace 三组结构体数组；Begin、Discard、Reset、Dispose 共用此清理入口。
- 三类绑定表各持有与正式容量相同的已写索引数组；首次写某个位置记录索引，同帧再次写仍覆盖原位置而不重复记账。Clear 只清实际写过的绑定，随后将有效数量和页完成身份清零。占用判断使用不可变的 PhysicalIdentity，不依赖资源是否已释放的动态 ScalarReadView 有效性。旧引用会真实清除，不用帧号掩盖长期残留对象。
- 代价是每个配置槽多一个 int 的常驻索引；活动源远少于配置总量时减少清理范围。容量来自原配置，无运行扩容、第二绑定路径或额外设置。已静态核对全部清理入口、重复写和空页行为，未实跑。

### AP22 表情属性重复检查同一渲染器及固定配置（已实施装配绑定与唯一目标检查）

- Corin 正式 Profile 配置41个 BlendShape，全部来自 `Corin_face`。FinalPosePropertyWriter 原先按每个属性重复执行同一 Renderer 的存在、根层级、期望 Mesh、Hash 格式检查，再读取对应 BlendShape 名称。BlendShape 是模型预制变形（如眨眼、闭眼、眉毛表情），权重仍需要正常更新。
- 最终实现：装配时核对名称、Hash、输入声明和 Renderer 配置，按实际 SkinnedMeshRenderer 绑定唯一目标及该目标需要的最大形状编号。正常帧不再逐属性验证这些固定配置；每个实际 Renderer 只检查一次对象存活、Mesh 身份、形状数量能否覆盖最大编号以及是否仍属于装配根。Corin 的41个属性共用1个目标检查，运行路径没有名称读取、Hash格式扫描、Hash/配置字符串比较或逐属性 Mesh 查询。写入持有直接 Renderer 引用；WriteDefaults/RestoreInitial 共用目标有效性入口。
- 续查规则依据：`character-animation-scalar-presentation/spec.md` 要求 Build 固定目标索引，并在构建或 Actor 装配时拒绝 Mesh revision/BlendShape 映射不一致；运行时仍须验证 Renderer/Mesh/目标索引。全 Assets C# 搜索未发现 ClearBlendShapes/AddBlendShapeFrame 调用，项目业务代码未发现运行时替换 sharedMesh 的赋值。第三方布料存在更换 sharedMesh 的入口，因此 Mesh 身份检查继续保留。
- 名称/索引对应关系由构造期 ValidateNativeBinding 完整检查；当帧参数可用性、有限值和整体发布身份仍需检查。这是固定模型映射的正式使用边界；原地重建形状列表或修改绑定配置必须重新装配，不支持不通知 owner 就改变映射。改变 BlendShape 权重（表情、眨眼或捏脸）不属于改变映射，不需要重新装配。没有新增设置或替代 writer，未做分配实测。

### AP23 状态机规则每次判断重建操作表（已实施装配缓存）

- 状态机每次尝试过渡时原先清空并重新遍历规则操作，把 OperationId 重新放入字典；字典本身在装配期复用，但每条候选规则仍重复做拓扑装配、重复身份检查和字符串到对象的读取。
- 状态机 Start 装配阶段按正式 RuleGraph 建立唯一操作表，并把 AnimationVariableInput 绑定到变量布局位置；检查缺失/重复操作及输出操作一次。运行时只按已建立的表清空值缓存和访问标记，仍由原 `EvaluateOperation` 递归执行，保留 And/Or 短路、输入变量当帧读取、事实读取、时间值和异常语义。Root Orientation Warp 的 FacingError 也改为 Initialize 时绑定位置。RuleGraph 或变量布局变化需重新装配，和状态图其它绑定一致。
- 新增的是每条规则的装配期字典常驻内存；没有整帧结果缓存，不会阻止同一帧变量修改后被规则读取。未编译、未运行，耗时收益待采样。

### AP24 Slot Blend 计划换页按最大容量清空（已实施写入索引清理）

- `AnimationSlotBlendPoseWorkspace.ClearPlanPage` 原先每次准备或放弃页都清空整页 entries、dense bone weights 和两组写入标记，即使本帧只写了少量贡献。
- 计划页现在为每个页保存实际写入的 entry 与 dense weight 索引；首次写入记录绝对索引，重复写入仍沿原规则报错。换页只清这些索引并归零标记和计数，Reset/Dispose 仍完整清理所有 NativeArray。计划内容校验仍依赖写入标记，不通过旧值或页身份掩盖缺项。
- 代价是两组定容索引 NativeArray 和两个页计数，容量沿原正式页容量；正常贡献数低于上限时减少清理写入。只做静态检查，未测 NativeArray 写带宽。

### AP25 惯性包络按骨骼重复求相同时长曲线（已实施，未实跑）

- 输入为同一次 ApplyResiduals 的过渡时长、elapsed、固定曲线与逐骨骼时长倍率。原来每根骨骼完整求包络，足部再次求双踝对应包络；每次曲线值与导数还各自扫描同一曲线段。
- 装配时按已校验的有限正倍率精确分组，保存骨骼到组的索引及每组工作结果。每次 ApplyResiduals 重新计算所有组，骨骼读取对应组，足部读取踝骨对应组；没有按近似值合并、跨求值缓存或运行期扩容。参数仍按原始过渡时长独立求值，不把倍率规则套给参数。
- AnimationBlendCurveEvaluator 增加同时返回值与导数的入口，共用一次 ResolveSegment，计算公式与段选择边界保持。只需要值或导数的其它调用者保留各自入口，避免多算未消费结果。
- 如果 B 根骨骼有 U 个不同倍率，带足部的旧路径每次执行 B+3 次包络求值，新路径为 U+1 次；这是源码调用次数，不是耗时测量。代价为替代原 float[B] 的 int[B] 索引、float[U] 倍率和四个 float 组成的 U 个工作样本。倍率都不同时不会减少骨骼包络次数，仍增加索引读取与工作存储；需实际内容和采样评价总收益。
- 静态核对 BuildDense 拒绝非有限/非正倍率、曲线求值无写入副作用、ApplyFootFeatures 只有此处调用，所有组在本次读取前覆盖；elapsed 仍在骨骼/参数/足部处理后递增。Commit/Discard、残差双页、输出页和历史页均未变。未编译、未运行或采样。

### AP26 动作请求工作区全容量清理及逐元素租约扫描（已实施，未实跑）

- `AnimationPoseRequestWorkspace` 当前唯一运行创建者是 ActionSlotSource。原 BeginFrame 清三组全容量身份数组；PrepareRow 再清该行的 clip/参数/可用性；Commit、Discard 和 Dispose 经 Reset 又清六组全容量数组。每次 AnimationReadOnlyBuffer.Count、下标或 ElementAt 访问还通过租约接口线性扫描全部行。
- 工作区没有逐行释放入口，行只从下标0连续追加，Count覆盖所有占用行，包括准备中途失败的行。新行直接取 Count，删除找空槽扫描；查重复只遍历已占用部分。BeginFrame/Reset 共用 ClearRows，只清实际占用行及对应数据段，然后 Count归零。构造数组本来为零，生命周期清理覆盖所有用过的行，因此 PrepareRow 不再重复清零；旧引用真实清除，不靠换代号掩盖残留。未被调用的私有 ClearRow 删除。
- 租约生成选择严格大于上次编号、且 `(lease - 1) % SourceCapacity == rowIndex` 的下一个编号。每次 buffer 访问按该式定位一行，再核对完整编号、source身份、准备完成身份及当前帧；删除全行扫描，没有新增索引集合。编号可能跳号，但只在此工作区用于相等性/有效期检查，不参与动作时间、参数页身份或播放顺序；Reset 不回退编号，重复准备仍先更换租约再报错，旧租约失效语义保持。到 ulong 边界时显式失败，不回绕。
- 输入/输出仍是同一请求行、同一租约保护的数组视图，Commit/Discard 的调用顺序不变。静态追踪了初始空帧、连续BeginFrame、准备失败、重复准备、Reset/Dispose和行复用路径；没有编译或实跑。租约检查从随容量增长的比较改为取模和定点核对，小容量下不预判哪种机器指令更快；本项不宣称新增GC收益。

### AP27 动作播放游标反复从头分配空槽（已实施，未实跑）

- `ActionPresentationSampleProjector.ValidateFrame` 经正式 ActionPresentationClockPolicy 提交预检调用。原来先清整个 reserved 数组再按 Occupied 写入true；为每个新增游标从下标0扫描第一个空槽，新增项多时反复比较同一前缀。
- 现在逐槽直接覆盖 Occupied，删除先清后写；处理完全部待移除游标后，只用一个局部下标向前寻找空槽。该阶段只占用槽、不再释放槽，因此按原 pending 顺序选出的槽位仍是同一个最小空下标；每次 ValidateFrame 从0重新开始，重复预检不会借用上次临时位置。没有新增字段、集合或配置，保留先预检再 Commit、容量不足报错及 Discard 不写 committed 游标。
- 静态核对 reserved 全部写入点、移除与新增顺序及调用者；扫描上界从每个新增项遍历容量，变为本次分配合计遍历容量。未编译、未运行或测量游标容量下的耗时。

### AP28 ACL 完成帧后按全部来源容量清理采样计划（已实施，未实跑）

- `CharacterAclPoseSamplingBackend.FinalizeAppliedFrame` 原来清空 SourceCapacity×ClipCapacity 的整个 pending plan 数组。准备路径按 journal.MutationCount 顺序分配一行，行内实际 clip 数不超过 ClipCapacity；失败时 catch 清掉本次尝试写入的范围。
- 正常 Finalize 只清 MutationCount×ClipCapacity 的已使用前缀，然后沿原顺序清 journal 和关闭帧。保留行内余量清理，不增加索引数组或第二套计数；Rollback/Discard 仍按原日志范围清理与回收资源。此前使用过但本帧未用的行已在其 Finalize/Discard/失败路径清除，不能长期残留旧 clip 引用。
- 静态核对全部 pending plan 写入、成功登记、失败清理和关闭入口；未测内存写带宽。本项减少的是按闲置来源容量执行的清理，不是删除每帧对象分配。

### AP29 动作采样历史提交重复复制整份准备结果（已实施，未实跑）

- `ActionCommittedSampleHistory.ValidateFrame` 在独立 prepared Entry 中组装完整历史；Commit 原先清 committed Entry、复制 prepared 的所有样本，再在 Close 清掉 prepared。同一帧已完成的样本结果被复制一次并随即清除一次。
- Entry 与其样本数组均由模块私有持有，构造时所有 committed/prepared Entry 等容量且互不共享。预检固定唯一目标槽后，Commit 交换目标槽与对应 prepared 槽的 Entry 引用；Close 只清换出的旧页。删除项仍先清 committed 槽，随后新条目可按原顺序占用该空槽。Discard 不交换页，重复预检仍重新准备；公开投影窗口返回样本值，不外借 Entry 或样本数组，因此没有改变外部数据寿命。
- 同时将历史预检的 reserved 标记改为逐槽覆盖，用单向游标按原 prepared 顺序分配最小空槽，与 AP27 使用相同规则；删除 FindFreeReservedEntry。保留 BuildWindowSamples、排序、修剪、重复事件及时间顺序检查，不用省复制绕过准备隔离。
- 无新增工作页、集合或配置。静态核对所有 Entry 赋值/访问、目标槽唯一性、移除后复用、Commit/Close、Discard和Reset路径；未编译、未实跑。减少的是提交时的数据搬运与新结果清理，不能据此宣称整条历史链已无重复工作或已测得CPU收益。

### AP30 回放热路径哈希、进度字符串与输入重复制（2026-09-29，已实施，未运行）

- `FixedCharacterInputTraceModule.Status` 原来每次读取都对固定起始 Body 做 SHA256；现在在注册起始 Body 或从 checkpoint 恢复时计算一次，Status 和最终证据复用同一哈希，Reset 清除。逐 Tick 进度改为固定文本，数量继续由 Status 的结构化计数提供。
- 回放逐 Tick 只保存 ReplayTick 与已发布的 WorldBodyState 值快照。输入哈希和 Body 哈希移到完成后的 CaptureReplayEvidence，沿用原哈希算法、原始录制输入和对应 Body；未请求完整证据的性能采集不再支付这些证明哈希的成本。Tick 连续性、重复消费、输入与 Body 配对、暂停和完成计数仍由回放协议检查。
- `SimulationInput.RemapTicks` 共享其私有、不可变的 Values 数组，只创建一次改时间戳后的 Requests 数组；Sequence/RequestId 顺序不变，不再复制及排序。HoldValues 共享相同数值并清空请求。RebindSource 统一使用同一私有快照构造入口，删除无职责 bool 参数和内部数组空值兜底。
- 每 Tick 输入快照对象及有请求时的请求数组仍分配，未通过复用可变对象破坏历史持有关系。本项降低的是回放/诊断负担，不能记成纯角色业务的全部 GC 降幅；仅做差异与调用链静态检查，未编译、未运行。

### AP31 采样任务每帧重查参考姿势与骨架拓扑（2026-09-29，已实施，未运行）

- ACL SourceGraph.Apply 和 Animancer EnterEvaluateBarrier 原来每次重新构造 AnimationSourcePoseCaptureJob，逐骨检查固定 referencePose；Job 执行时 Derive 还逐帧检查父节点顺序、根数量、虚拟骨配置及重复身份。
- 任务构造只在 source 创建时执行，保存固定骨架、参考姿势及策略；每帧 BindFrame 只接收正式 AnimationPoseSourceCaptureBinding，核对该帧骨骼页与任务骨架的长度并更新页、身份和时间。删除 validateBinding 开关和重复的完整绑定校验，绑定字段本身由既有构造边界负责。ACL 与 Animancer 都使用同一 Job 接口，没有留下旧构造路径。
- 骨架拓扑检查集中到 ValidateLayout，Job 构造和 reference Virtual Bone 编译入口各在接收布局时调用一次；Derive 只处理当帧姿势与输出页。删除进入 TryCreateComponent 前对同一 local 的重复 IsValid；每根虚拟骨共享一次 inverseSource，保持计算公式和归一化次数。
- 不改变 Playable 的求值、帧绑定更新时机、双页提交、Discard 或资源释放顺序。保留引擎输入及派生结果的数值失败语义。静态核对全部4处原 Job 构造点、两个 Derive 调用入口和引用；未编译、未运行，不宣称 CPU 收益数值。

### AP32 Pose 求值反复从序列化字符串构造身份（2026-09-29，已实施，未运行）

- Canvas Node 的 NodeId getter 每次都构造 PoseNodeId 并扫描字符串；图端口缓存、观察记录、端口定义索引和 handler 查找在求值中反复走它。现在图实例 Initialize 时绑定非序列化 NativeNodeId，运行索引及观察读取该身份；authoring 的 NodeId 解析仍用于编辑和装配，未修改序列化数据。图变化按原合同重建实例。
- Clip Player、BlendSpace Player 和 Blend Stack 在各自构造期保存编译描述符的 NodeId，播放、相位、来源、过渡检查统一读取同一值。空间转换节点的输入输出 PosePortId 也在构造期解析。
- 没有关闭逐帧身份匹配、租约或 Commit/Discard 校验，没有按帧缓存计算结果。减少的是固定字符串格式扫描；不宣称这些值类型构造本身存在 GC。仅静态核对初始化顺序与调用路径，未编译或采样。

### AP33 BlendSpace 参数与采样时间逐帧线性查找（2026-09-29，已实施，未运行）

- BlendSpace WriteParameters 原来逐参数找策略，再逐活动样本找 Sample、按参数 ID 找数值；生成 ClipSamplePlan 又分别线性找 Sample、Time 和索引，属性采样再找一次 Sample。
- 构造期建立 SampleId 到计划位置的索引，以及实际需要混合的参数策略和各样本静态参数值。每次权重求解后只解析一次活动样本位置，参数、时间和 Clip 计划共享它；Native 属性使用 ClipBindingIndex 直接读同一计划。
- 已核对 PhasePlan.Create 保持 Samples 顺序，两个成功的 PhaseMapper 路径均按该顺序完整写 TimePage。参数计算仍按原活动权重顺序累加；Unavailable 不参与，RequireAllSamplesWeighted 仍只在缺参样本实际激活时失败，未提前拒绝永不激活的缺参样本。缺失参数策略在装配期明确报错。
- 常驻代价为一张索引字典、活动位置数组和非动画属性参数的样本值表；无每帧分配、跨帧权重缓存或配置 fallback。删除仅用于该链路的 FindTime/FindSampleIndex 和动画属性自赋值。未编译、未运行。

### AP34 足部预测来源身份逐帧格式化字符串（2026-09-29，已实施，未运行）

- Clip Player 每次采样左右脚、BlendSpace 每次采样每个活动样本的左右脚，都调用 SourceIdentity(AnimationPoseSourceId)，内部先 ToString 再 HashText，包含插值字符串和枚举/数值格式化。
- Player 在 SourceId 生成或清除时同时更新 PredictionSourceIdentity，保存于原 committed/pending State 内。BeginFrame、Commit、Discard、Reset 继续整体复制或清除该 State，来源身份与其哈希不会跨事务错配。来源代次变更时仍使用原字符串算法一次，不更改既有事件身份。
- BlendSpace 将已解析来源身份与样本 discriminator 按原 Hash 公式组合，一次结果同时交给左右脚。删除逐脚重复格式化；不修改最终 landing identity、相位或周期。来源切换仍可能产生一次字符串分配，本项只消除稳定播放期间该调用的周期分配，不能宣称整条链路 0 GC。
- 静态核对全部 SourceId 写入点、State 页复制与哈希组合顺序；未编译、未运行。

### AP35 运行期状态合同的枚举装箱（2026-09-29，已实施，未运行）

- ACL Ready/Pending/Invalid 结果的公共构造器每次对两个枚举调用 Enum.IsDefined(Type, object)，需要装箱。SourceResourceResolver 的正常就绪结果和 ResourceStore 租约申请均经过该入口。AssetLoadResult 的轮询状态也有同类检查。
- 按当前 byte 枚举的连续取值范围进行原生比较，保留状态与失败码、资源是否存在的组合规则。MotionMatchingTrajectorySourceFrame 的两种来源枚举改为直接比较，保留输入合同；该分支未证明由当前 Corin 采集命中。
- 只更改已确认的运行结果/帧构造路径，未为消除初始化分配去改 authoring 和编译器 Enum API。有效/无效取值集合不变，无新增防御检查。静态核对枚举定义和调用点，未编译或采样。

### AP36 原生节点贡献页按最大容量清尾（2026-09-29，已实施，未运行）

- 公共 CopyAttributes 及普通/分层 Blend、状态过渡原先每次清空 contributionCount 到容量末尾的所有贡献及逐骨权重。页面由 NodePoseBuffer 以 ClearMemory 创建，实际写入始终为连续前缀；空闲容量没有必要逐帧重新清零。
- 沿用该页已有 ContributionCount：批量复制前或单条写入前，ExtendContributionPrefix 将计数扩大至本次可能触及的前缀；CompleteContributions 只清最终数量到该前缀上界的旧尾部，清理成功后再写最终数量。计数在未发布页内承担写入范围，发布后仍是正式贡献数量，没有新增缓存标志、第二计数或工作数组。
- 已覆盖公共元数据复制、Blend、Layered、StateMachine 和 AnimationSlot 的逐项写入。后者原先未清旧尾部，现在采用同一完成入口。写入容量检查仍先于范围登记；构造单条贡献或复制权重中途失败时，范围已经登记，Discard 不交换 committed 页，后续复用仍能清掉半写数据。Stop/Reset 不清数据也不缩小这段范围；实际销毁沿原 NativeArray Dispose。
- 静态检查了新页、数量增减、不变与归零、追加中途异常、重复 Discard 后复用及成功后再失败的路径；确认节点双页独立创建，惯性节点只改已完成前缀内的贡献。保留输出完成身份与发布时序。增加逐条写入时一次前缀比较，减少空闲容量清零；未编译或实测，不预判小容量下净收益。

### AP37 BlendSpace 足部曲线采样触发全曲线校验和 keys 分配（2026-09-29，已实施，未运行）

- BlendSpace 对每个活动样本的左右脚调用 AnimationFootFeatureCurveSet.Sample。该入口每次 RequireValid，递归检查当前/下一步及生物力学曲线；RequireCurve 读取 AnimationCurve.keys 创建关键帧数组，RequireRoute 为每条曲线拼接字段名。属于明确的稳定播放分配，并非所有 new 都只是值类型构造。
- Player 装配时对具有足部特征的样本完成左右曲线校验；运行时沿 Clip Player 已有的 SamplePrepared 入口采样，不再逐帧读取 keys 或格式化校验字段。原始 Evaluate、Clamp、曲线插值和样本构造公式未变，不涉及四元数精度取舍。固定曲线变更需重新装配。
- 全曲线非法配置现在在装配时拒绝，不再延迟到该样本激活；没有为不合法配置增加默认曲线。生物力学 Sample 本身不重复 RequireValid。当前 MotionMatching 消费离线样本，未命中这条曲线校验路径，未将其计入收益。
- 静态核对两个 BlendSpace 调用点和既有 Clip Player 入口，确认原 keys 读取及字符串创建已退出此运行链。未编译、未实跑，尚不能把此前报告的 GC 全部归因于此项。

### AP38 同一骨骼旋转重复归一化（2026-09-29，用户已确认允许一次归一化，已实施，未运行）

- 用户明确“那就只归一化一次吧”，允许删除重复归一化带来的浮点末位变化，不再要求这些旋转结果逐位一致。该授权不包含改变旋转顺序、混合权重、动画更新频率或播放时序。
- BlendWeighted、Slot Blend 权重结果、Additive、组件/本地空间乘法和虚拟骨旋转，去掉结果构造器前紧邻的 normalized，由姿势生成构造器统一归一化。Slot Blend 已先确认模长有限且高于退化阈值，因此去掉重复归一化后的有限性重查，原有效域检查保留。
- Final IK 加权旋转交给 PoseBufferBackend 生成一次归一化的目标姿势，再用该姿势旋转计算后代 delta；后代自身新旋转继续在生成时归一化。仅平移的姿势更新直接保留已有旋转和缩放，只校验新位置，不再重新归一化未改变的旋转。
- Local/Component 两种结构之间的内部拷贝直接复制已生成的姿势，沿调用者既有输入检查与生成边界使用；没有公开 unchecked/bool 跳校验入口。采样 Job 先选定根策略和缩放策略，再构造一次姿势，避免为替换 scale 连建两次姿势。
- QuaternionLog/Exp 的必要归一化、Modify Bone 在 Slerp 前准备输入旋转、Pose Bone IK Goal 的唯一归一化仍保留：它们不是对同一结果连续做两次，直接删除会改变后续运算的前提。静态核对全部新增内部拷贝调用点及 IK backend 具体实现；未编译、未回放或采样，视觉与数值回归待验证。

### AP39 Additive 逐骨重建固定参考姿势与逆旋转（2026-09-29，已实施，未运行）

- Additive 非零权重时每根骨骼调用 Rig.GetReferenceLocalPose，重新校验和归一化固定参考姿势，再对同一参考旋转求逆。现在构造期按骨骼位置建立位置、逆旋转和缩放的值类型数组，运行时读取；没有保存用不到的原参考旋转，也不把除法改成乘预计算倒数。
- 零权重路径由逐骨判断和复制改成两次 NativeSlice.CopyFrom，复制基础姿势及速度到本节点独立输出页。权重、连续性、贡献元数据、输出完成身份及 Commit/Discard 顺序保持；上游两路仍正常求值，不改变播放器时间和过渡语义。
- 代价为每个 Additive 实例一组定容参考数据，Rig 变更需按原合同重建。其余 GetReferenceLocalPose 调用经检查位于初始化/资源身份/校准阶段，未扩大缓存。只做差异、调用点和初始化顺序静态检查，未编译或采样。

### AP40 脚步预测按需裁剪生物力学路线采样（2026-09-29，已撤回）

- 曾将完整25点采样改为仅计算 eventPhase 两侧的点。该修改没有回放证据，且跳过了未消费点的派生样本检查，不能据静态公式相同认定完整行为等价。
- 用户明确要求保护长期调试的脚步预测。已撤回该项：恢复 Sample 完整生成25点路线，再由 SamplePrepared 按原索引及 Interpolate 取当前相位；删除本次新增的 SamplePhase、SampleFrame 和 SampleRoutePoint。
- 本项不计入当前性能优化成果。本轮静态优化停止改动脚步预测的采样、插值和落点逻辑；未编译、回放或采样，不宣称运行行为已经验证。

### AP41 足部混合传递和返回未消费的大型值样本（2026-09-29，已实施，未运行）

- AnimationFootFeatureBlendAccumulator.Add 原先按值接收双预测步样本，两个参数重载再按值转交；当前/候选 authority 也各复制一份含路线的预测步。改为 in 输入和只读引用选择 authority，引用只在本次调用内使用，不外借、不跨帧保留。
- 原 Select 返回整份当前或候选预测步，但唯一调用者丢弃返回值，只消费 out bool。替换为 ShouldSelect，只返回布尔值，保留无事件处理、分数容差、来源身份、周期、事件序号的完整决胜顺序；旧入口删除。
- ApplyTimeScale 在完成原有非法倍率与无有效事件分支后，倍率恰为1时直接返回已有值。非单位倍率仍执行原换算；避免重建相同路线与重复归一化，符合用户已确认的归一化口径。最终胜出的一对样本仍复制进 accumulator 自己的状态，未改为共享可变结果。
- 这项减少值类型复制、无效返回及单位倍率重构，不把 FixedList 值成员误记成托管 GC。仅静态核对全部 Select 调用、Add 使用方式及局部引用寿命，未编译、回放或采样。

### AP42 Blend Stack 重复扫描固定过渡与来源表（2026-09-29，已实施，未运行）

- Action Slot 准备帧向 Blend Stack 查询当前端点到目标端点的精确过渡，Push 接收请求时再次查询。原先每次扫描全部 Transitions，并对每个候选读取 payload.NodeId、重新执行字符串身份解析；首次来源绑定还会扫描同一张表解析来源名称。
- AnimationBlendStackRuntime 装配时建立过渡身份到原 payload 对象、来源名称到 owner index 的两个字典。过渡身份沿用节点、来源索引及类型、目标索引及类型的完整相等规则；名称使用 Ordinal 比较。运行时只查询，不修改字典，不扫描固定表。代价是每个实例持有与固定配置规模相关的索引内存。
- 原有重复精确过渡、同名来源索引冲突检查移到装配期；未知来源和缺失精确过渡仍在请求入口报错。请求仍须持有查得的原对象，删除 ReferenceEquals 已成功后对同一对象重复构造并比较身份的表达式。时间推进、过渡曲线、权重、采样及 Commit/Discard 不变。
- 追踪正式工厂确认过渡由 BuildTransition 校验后构造，数组在创建 payload 时独立生成，运行实例无配置修改入口。删除 payload 上旧线性 RequireTransition，全部调用统一进入 runtime 索引；配置更换按既有装配生命周期重建实例。仅完成差异和调用链静态检查，未编译、运行或采样，不宣称实测耗时或 GC 收益。

### AP43 Blend Stack 同倍率骨骼重复计算混合曲线（2026-09-29，已实施，未运行）

- PrepareCrossFadePlan 原先对每个条目、每根骨骼重新解析曲线和 Profile，并调用 EvaluateBoneAlpha。该函数的逐骨输入只有 DenseDurationMultipliers；时钟、基础时长、深度倍率和曲线属于同一个条目，本次计划生成期间不改变。
- 装配时按 Profile 中完全相等的正有限倍率建立骨骼分组，保存各组首根骨骼索引。每次生成计划先按原 EvaluateBoneAlpha 对各组代表求值、检查范围，再供逐骨循环读取；没有近似合并，也没有改乘除次序、曲线公式、源可用性和逐骨 residual/weight 累积顺序。
- 组结果工作区在装配期定容，每次计划都覆盖当前条目的全部有效组；仅在该次调用内消费，不依赖跨帧结果，不需要新增有效标志或提交状态。额外内存为每个 Profile 的骨骼分组表与条目容量乘最大组数的 float 工作区。所有倍率都不相同时不会减少曲线求值次数，额外索引成本未实测。
- 静态读取 CorinLocomotionBlendProfile.asset 和 CorinActionBlendProfile.asset，各203根骨骼倍率均为1：使用这两份配置时，每条目逐骨曲线求值由203次变为1次，逐骨权重仍计算203份，独立的全局标量曲线照常计算。该计数不等于整体耗时收益，也不是删除托管 GC 分配的结论。
- 未改动脚步预测采样、插值或落点逻辑，未改来源采样和 Commit/Discard。完成配置、公式、工作区读写范围及差异静态核对；未编译、回放或采样。

### AP44 Blend Stack 计算前重复清零骨骼权重工作区（2026-09-29，已实施，未运行）

- ClearPlannedWeights 在每次 PrepareCrossFadePlan 前清空条目容量乘骨骼数量的 m_EntryBoneWeights；随后的双循环无条件覆盖当前全部条目、全部骨骼，包括来源不可用时写入0。完整覆盖后才调用 WriteCrossFadePlan，直接读点及 GetStoredResidualForBone 都只访问当前条目范围。
- 删除该数组的整容量清零；无输出/不可用分支不读取该工作区，计划生成中途失败也不会发布尚未完成的计划，下一次计算会重新覆盖有效范围。数组不含托管引用，不新增清理标志或备用页，不影响独立输出页和 Commit/Discard。
- 保留标量权重、Raw/Eased Alpha、条目最大权重和 stored 最大权重的原清理：它们还被诊断或释放流程读取，不能将上述局部读写证明套用到这些字段。该项只减少冗余内存写入，不宣称消除 GC 分配。完成全部字段读写点、唯一计划写入调用及差异静态检查；未编译、回放或采样。

### AP45 BlendSpace 权重页清理和归一化遍历总容量（2026-09-29，已实施，未运行）

- WeightEvaluator 每次 Evaluate 先 Reset 清零全部 Scratch，再由二维 SolveGradientBand 逐样本完整覆盖；Normalize 的两遍循环按页容量遍历，还处理了当前计划之外的零尾部。
- Reset 只重置公开有效数量 Count。二维成功求解已写满当前 SampleCount；一维只写端点或相邻两项，因此将必需的 Array.Clear 放进 SolveLinear，范围为当前 SampleCount。Normalize 两遍均按 SampleCount 读取，页复用到更小/更大的计划时只消费本次已覆盖或清理的范围，不要求容量尾部清零。
- 求解失败仍将 Count 置0，后续消费者只能按 Count 读取；Scratch 的唯一业务读取入口为本文件求解器，未增加跨帧缓存或动态分配。公式、MinimumWeight、累加顺序、归一化除法及正权重样本输出顺序不变。当前 Player 页容量等于计划样本数，主要减少二维预清理；容量大于计划时才额外减少尾部遍历。
- 静态核对单样本、线性端点/区间、二维完整覆盖、失败后复用及容量边界，差异检查通过；未编译、回放或采样，不宣称实测收益。脚步预测与相位采样代码未改动。

## 可靠性问题独立保留

- 续查 ParameterResolve 发现独立输出页未写入骨骼：EvaluateOutput 仅调用 CopyMetadata，而该公共方法只复制参数/贡献/速度及帧元数据，随后 ResolveParameters 也不写骨骼。现显式将 base 的 DenseLocalPoses 批量复制到本节点输出页，保持“base骨骼＋按策略合成参数”的完整输出。沿现有 CopyMetadata 的布局检查和独立双页提交，不直接返回输入页。该项是正确性修复，会补上必需的复制工作，不计为 CPU 优化收益；未编译、未实跑，当前资产是否执行此节点尚未采样。

- 保存后恢复校验、变量 ID 与名称解析统一，解决的是配置看似存在却未生效，不作为 CPU 优化的完成条件混入上述条目。
- Corin Lean 实施说明已经记录个别初始化绑定及常量恢复修复；这些修复不等于通用 authoring/变量身份缺口全部收口。
- AP05 的创建策略、AP07 的采样裁剪、AP08 的页共享需要保持正式生命周期及业务合同，不设临时旁路。AP11 的提交证明已按用户的采样隔离要求拆分。

## 后续证据范围

2026-09-29 已取得一份优化前 Player 实测：`capture.20260928-161732.1d0ec10f9dd94ede8de4ab5b205bc7f2`，位于项目 `Library/Performance/Captures/`，总览为 `Library/Performance/Reports/overall-performance-20260929.md`。双角色、1920×1080、关闭 VSync 与 FPS 上限，完整采集约101.49 FPS，主线程均值9.82 ms、P95 13.17 ms，GC.Alloc 均值约99.8 KiB/帧。`presentation.animation` 包含整个表现域，均值约5.44 ms/帧；Pose Evaluate 累计约2.80 ms/帧包含嵌套图，不能与父级直接相加。GC 含回放与诊断开销，原生调用栈约70.25%未解析，尚不能完整归因。

上述是 AP30–AP34 修改前的单次观测，不是本轮优化后的收益证明。当前用户要求静态检查，不启动编译或采样，并限制后续性能采样最多两次；本轮新增采样次数为0。此前条目的“未实跑”只表示各条改动缺少独立前后验证，不再表示项目从未取得 Player 性能数据。

当前已覆盖 EventGraph → Pose 输入 → 图/子图准备 → 混合/惯性/变换 → ACL 输入设置 → Final Pose 写回，以及足部支撑选择与动作来源解析。没有新增性能采集链。

仍未确定 ACL native 解压、Playable 求值、FBBIK、世界物理查询各自耗时、线程等待、活动角色/源数量和采样开关差异。原生插件内存、托管 GC、内存带宽和引擎调用次数分别记录；不得凭源码循环数量给出毫秒收益，也不把非当前内容使用的分支当作已命中热点。

## 2026-09-28 性能优化实施

用户已授权开始性能优化。上文未实施条目继续作为候选，标明“已实施”的条目不再计为未修复；编译、运行和耗时证据分别记录，不用源码修改代替验证结果。

Center 改动：`动画链固定绑定与重复工作优化`，change_id=`f2e964ebf61c4cd3a5300db396ad91a5`。修改前正式 compile 返回 `WorkspaceEditorInUse`，没有产生 RunId，也没有启动 Unity。随后用户明确“不用你起进程”，本轮不再启动 Unity、编译或采样进程，不建立替代验证路径。仅做代码与差异检查；不新增测试，运行结果和性能收益未验证。

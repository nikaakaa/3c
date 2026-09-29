# 全项目托管分配与相关 CPU 开销审计

本文件是当前审计入口，最近更新为 2026-09-29。区分托管分配、持续 CPU 工作和首次使用成本，不把减少分配直接等同于减少耗时。最初完成只读审计，随后用户授权按小步修改运行代码；下文 AP 条目记录当前实施范围。本次续查仅做静态检查，没有启动 Editor/Player/服务器、编译、编写测试或采集性能；没有新的 bytes/frame、GC 次数或耗时数据。

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
- 2026-09-29 静态续查：正式生成源已准备新增 `animation.current-planar-velocity`、`animation.lean.direction-angle` 和 `animation.lean.return-target-angle`。Corin 更新 Split 先保存当前平面速度，再按原相对顺序执行水平速度、垂直速度、方向、期望方向、朝向误差、加速度分支、运动阶段分支和历史保存；Lean 在保存 eligible 前保存方向角，在保存平滑角前保存回正目标。原纯函数改为一次写入，消费者读变量；FlowCanvas Instant Split、SetVariable 赋值与 GetVariable 读取时机已静态核对。该改动尚未由 Unity 内正式 authoring 重建 `CorinAnimationEventGraph.asset`，运行图仍是旧结构，不能计为运行行为已实施或宣称收益。
- 继续核对方向角与回正目标的分支语义：`AND` 对第二输入短路，而三分支 `EventGraphFloatSelectNode` 的三个输入在 `Invoke` 前求值；旧图的 `save-turn-rate` 和 `save-angle` 无条件执行，方向角与回正目标在 false 分支仍会被读取。新的先保存后读取保持可见输出不变，只把旧的一次或两次求值收敛为一次。当前图资产已含 Lean 常量，生成源中的同一常量在重建时会重新写入；这是正式重建的既有行为，不是本次新增 fallback。

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
- 续查内置 ParameterInputHandler 与 ActionPlaybackInputHandler：两者 Reuse 输出包装时仍读取 authoring NodeId；参数节点还每次解析 ParameterId，动作节点每比较一条命令就解析 AnimationChannelId。现在复用 handler 自有 NodeId，在 Initialize 保存参数/通道身份；参数仍沿原布局绑定读取当前变量，动作仍逆序扫描当前命令并选择首个同通道项，未缓存值或动作结果。仅固定身份移到装配阶段，缺失输出、类型不符及无匹配命令的失败语义保留；更换配置按既有图重装配合同生效。端口值包装经核对已持有并复用，未将其首次创建误记为周期分配。
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

### AP46 Fixed／Float32 递归值输入缓冲重复清理（2026-09-29，已实施，未运行）

- 扩展到运行链后复核 G01/G04/G05：当前 invocation/workspace 已由 Actor 持有复用，输入缓冲按 operations.Count+1 在准备期建立，ActionTraceContextScope 已是值类型。初始审计中的每 Tick 重建和 class scope 描述不能继续作为当前分配证据。
- 当前残留重复工作为：ReadInputs 租用空缓冲前 Clear，租约释放时再次 Clear，workspace.Reset 又扫描所有预留深度逐一 Clear。预留深度取决于操作总数，未实际使用的层也会在每次工作区重置时被遍历。
- 沿 Fixed、Float32 和 Float32 表现图读取链核对，成功返回的输入租约由 using 释放；填充期间失败则由 ReadInputs 的 catch 调用同一个 ReleaseInputBuffer。缓冲首次构造为空，Values 唯一写入入口为 ReadInputs，释放会清掉已写值及其引用。因此删除租用前清理与 Reset 全层清理，仅由释放边界清理。
- 保留深度容量边界、LIFO 租约匹配、BeginEvaluation 跨求值递归状态检查及 ValueStack 清理。未改变短路求值、递归执行顺序、状态事务、快照或 Commit/Discard；不新增计数器、有效标志或兜底路径。只减少重复 Clear 和按预留深度扫描的 CPU 工作，不宣称该修改删除托管分配。
- 静态核对首次租用、嵌套租用、正常释放、填充异常、下游异常和工作区复用路径；差异检查通过，未编译或运行。数值算法与脚步预测未改动。

### AP47 逻辑技能与控制重复线性读取有序输入（2026-09-29，已实施，未运行）

- FixedInputRuntime／Float32InputRuntime、两域的移动控制及控制条件分别维护同样的逐项 InputId 查找；同一 Tick 的多个技能和控制节点重复扫描同一份输入。OperationExecutionTopology.FirstReference 经复核已经按操作/引用种类索引，不再为它增加缓存。
- SimulationInput 普通构造按 Ordinal 排序并拒绝重复 ID；Fixed 重绑定/回放沿用该数组，Float32 FromOwnedArrays 在接收边界检查严格有序且唯一。AbilityExecutionInput.Begin 现在直接接收对应域的 SimulationInput，而不接收可任意排列的裸列表与独立 Sequence，沿用原已验证输入建立本 Tick 视图。
- 两个数值域各由 AbilityExecutionInput.ReadValue 执行 Ordinal 二分查找，技能输入端口、移动输入和条件输入统一调用；删除控制模块的重复查找实现。无每 Tick 新索引或额外数组，不缓存值，不假定不同 Tick 的输入布局相同。空输入、缺失 ID、类型不匹配仍报原 InvalidOperationException，命中后返回同一份当前值，数值计算不变。
- 比较次数由最坏线性改为对数级，输入很少时实际耗时不保证更低。静态核对两域构造/所有权入口、全部 Begin 调用、三个消费类别和差异；未编译、回放或采样，未修改网络、状态事务、快照持有规则或脚步预测。

### AP48 DotRecast 接触候选接口转换装箱（2026-09-29，已实施，未运行）

- G12 复核确认候选集合已经改为 Solver 持有数组复用，初始审计的逐批 new List 已不成立。当前 DotRecastWorldSolver 将 ArraySegment<ActorContactCandidate> 传入 Resolve 与 ValidateFinal 的 IReadOnlyList 参数，值类型转接口会装箱；不是数组片段构造本身分配。
- 候选写入页改为当前有效范围的 Span，Resolve、初始重叠、扫掠、最终验证和名单检查统一接收 ReadOnlySpan；最终位置也按本批数量传入只读切片。读取无接口转换，不增集合或复制；不读取数组扩容后的无效尾部。
- 全链同步使用，无异步等待、迭代器、字段保存或闭包捕获 Span；候选仍由原数组持有，排序、成对顺序、迭代次数和数值公式不改。返回的世界状态与结果数组继续按原快照所有权创建，未将其改成可变共享页；观察对象增加时工作区扩容仍存在，不能宣称整个 Solver 零分配。
- 同链发现的 ValidateFinal 自递归错误已修复，详见可靠性段，不能把修复后能执行验证计为 CPU 优化收益。本项未运行 DotRecast，不把该路径当作当前 KCC 场景的实测热点；仅核对唯一正式消费者、切片寿命、接口类型与差异，未编译或采样。

### AP49 KCC 接触求解在普通构建中仍整理逐对诊断（2026-09-29，已实施，未运行）

- KCC 正式消费者传入数组引用，并没有 AP48 的 ArraySegment 装箱，不将 DotRecast 结论照搬。另查到接触求解无条件构造 Sweep/NormalClip/Depenetration/Validation/Failure 记录，写工作区，再逐阶段复制到 WorldSolver 汇总列表；PublishActorContactDiagnostics 到最后才检查 sink.IsEnabled。
- 按既有普通运行不采样的编译口径，工作区 AddTrace 与世界求解器 AppendTraces 使用 UNITY_EDITOR／KK_DIAGNOSTIC_SAMPLING 的 Conditional 属性：两个条件都关闭时，调用及记录构造参数整体不进入调用方 IL，逐对字段整理、列表写入和汇总复制被裁剪。相应每批列表清理也处于同一条件。
- Editor 或开启采样宏的构建保留原记录、容量门禁、失败轨迹及汇总；普通构建仅创建两个空列表作为现有结果/异常结构的诊断容器，不预分配按配对数和迭代数增长的记录数组，也不会追加记录。普通构建中的求解失败仍按原消息抛异常，但不含逐对诊断历史；不存在“缺诊断数据则改变碰撞结果”的路径。
- 碰撞候选、纠正向量、接触标志、迭代次数、计数摘要和最终几何校验均保留；AddTrace 参数逐项核对仅为已计算局部值/只读字段，无业务状态写入。计时及摘要发布仍沿原 sink 状态处理，本项不宣称整个 KCC 诊断链全部无工作。返回世界快照分配也未改。
- 核对六处记录构造、两份列表唯一写入入口、调用方编译条件及差异；未编译、回放或采样。KCC 运动算法及脚步预测未改，实际性能收益未知。

### AP50 KCC 每次状态编码重建 writer 与临时缓冲（2026-09-29，已实施，未运行）

- DeterministicKccStateCodec.Write 原先每次新建 CanonicalWriter，默认分配256字节缓冲，按状态大小扩容后再 ToArray 复制出结果；CreateState 在每次世界求解结束都会调用该入口。最终 payload 由世界历史独立持有，不能直接返回可复用缓冲。
- Solver 实例现在持有唯一 m_StateWriter。CreateState 先 Reset，再调用唯一编码函数，最后 ToArray 创建独立 payload；Codec.Write 改为接收 writer 并只负责原有字段编码，删除内部临时 writer 和返回副本职责，没有保留旧重载。全仓正式调用点只有该 Solver。
- 字段顺序、Magic/Version、UTF-8、定点原始值写入、Actor 排序检查全部保留；CanonicalWriter.Reset 只重置位置和长度，ToArray 复制有效范围，不向历史外借内部缓冲。编码失败不会创建/发布新 payload，后续调用仍先 Reset。writer 随 Solver.Dispose 释放，现有 Dispose 实现为空，不涉及新增资源释放协议。
- 省掉周期 writer 对象及临时缓冲的重新分配/扩容，最终快照数组分配仍保留。首次写入超过256字节时仍会扩容：通常在 Create 初始化阶段完成，若从恢复状态开始而未执行 Create，则首次编码仍有扩容成本。本项不宣称完整0 GC；后续是否按固定名单预留容量仍需结合实际状态规模评估，未为初始化引入重复的序列化长度算法。
- 静态核对唯一调用点、同步 writer 寿命、Reset/ToArray 实现及字段写入差异；未编译、回放或采样，未修改 KCC 数值算法或脚步预测。

### AP51 CanonicalWriter 整数写入经栈缓冲再次复制（2026-09-29，已实施，未运行）

- WriteInt32/UInt32/UInt16/Int64/UInt64 原先用 BinaryPrimitives 写入2/4/8字节栈缓冲，再通过 WriteRaw 检查容量并 CopyTo 主缓冲。KCC 定点状态、长度前缀、各域数值编码均复用这些基础入口；栈缓冲不是托管分配，不能将该项计入 GC 降幅。
- 五个入口沿用相同 BinaryPrimitives LittleEndian 方法，先 EnsureCapacity，再直接写入当前位置的定长 Span，最后按原宽度推进 Position 并 TrackLength。删除中间栈缓冲和显式复制，不改变字节序、数值转换、长度跟踪及公开 API。
- 容量不足/溢出仍在修改主缓冲、Position、Length 前失败；覆盖已有内容时 TrackLength 仍保留原最大长度，长度前缀回填沿原 Position 恢复逻辑。WriteDouble 的有限性与负零处理、UTF-8 分块编码、Raw 写入、Reset、ToArray 和快照所有权均保持，不改协议版本。
- 静态逐方法对照旧路径的写入宽度、容量检查与状态更新顺序，差异检查通过；未编译、执行字节对比或采样，实际 JIT/IL2CPP 是否已有消除中间复制及本次耗时收益未知。

### AP52 KCC 每批重复比较同一个世界状态对象（2026-09-29，已实施，未运行）

- ResolveBatch 使用 StateEquals 比较 request.BeforeWorldState 与 m_Current；WorldSolveBatchRequest 保留传入 WorldSimulationState 对象，不克隆。即使两者为同一实例，原实现仍逐字段比较身份、逐字节比较 SolverStatePayload，再逐角色比较所有定点 Body 字段。
- 保留空对象返回false，在此之后对 ReferenceEquals 成立的状态直接返回true；这是现有固定数值、数组快照比较的自反关系，不保存跨 Tick 的校验结果。不同实例仍执行全部原有身份、角色数量、payload 及 BodyEquals 比较。请求与绑定名单、KCC 状态名单、能力和几何校验保持。
- payload 比较使用项目已使用的 ReadOnlySpan.SequenceEqual，删除唯一调用的手写逐字节 BytesEqual；长度及内容相等规则不变，不新增缓存或数组。没有将仅哈希相等、相同Tick或同版本当成状态相等。
- 同实例路径省去随角色和序列化字节数增长的重复比较，不据源码推断实际命中率。静态核对状态持有方式、固定 Body 相等字段、空值分支和不同实例路径；未编译、回放或采样，未更改状态隔离、恢复校验和脚步预测。

### AP53 世界状态哈希重复生成中间序列化数组（2026-09-29，已实施，未运行）

- Fixed／Float32 的 WorldSolveBatchCodec 在请求和结果哈希中调用 WorldSimulationStateCodec.Write，先创建内层 CanonicalWriter 及其缓冲、ToArray 生成独立数组，再由外层 WriteBytes 复制整段世界状态。该数组只用于这次哈希，没有快照持有者。
- 两域 WorldSimulationStateCodec 增加内部 WriteLengthPrefixed：使用已有 BeginLengthPrefixedBlock，调用唯一的 WriteCanonical，随后 EndLengthPrefixedBlock 回填长度。请求和结果直接写入各自原有 writer，删除内层 writer、缓冲、ToArray 数组及中间复制；没有新增序列化格式或另一套字段编码。
- 原格式为小端 Int32 长度后接规范世界状态，回填长度为当前编码结束位置减起始位置再减4。WriteCanonical 不写绝对偏移，Magic/Version、字段顺序、数值编码、名单顺序和 payload 均未改变；请求/结果接收边界已确认世界状态非空，内部不重复检查。编码失败不会发布哈希。
- 恢复事务的目标/捕获状态校验、启动世界状态身份改用已有 WorldSimulationStateCodec.ComputeHash，直接对编码缓冲有效范围运行同一个 SimulationCanonicalPayloadHash.Compute。恢复校验和 SHA-256 算法保留，仅省去 ToArray；这部分发生在恢复或启动阶段，不混作周期收益。
- 真正持有独立字节的世界快照及预测协调器仍调用 Write；外层 writer/缓冲扩容、哈希对象及哈希字符串分配仍存在，不宣称整条链0 GC。静态核对两域全部变更、长度前缀回填、哈希入口与剩余 Write 调用，差异检查通过；未编译、执行字节对比、回放或采样，未改世界求解算法和脚步预测。

### AP54 身体运动计划在消费时重复编码与哈希（2026-09-29，已实施，未运行）

- Fixed／Float32 的 CharacterBodyMotionRuntime.Prepare 使用 Actor、Tick、绑定身份、步长和已计算位移/速度生成计划哈希，Finalize 的 RequirePlan 又以计划的同一组字段调用 ComputeIdentity，创建 CanonicalWriter、缓冲、SHA-256 实例及结果字符串，再与原哈希比较。
- 两域 BodyMotionIntegrationPlan 都是 readonly struct，字段为只读标量/向量/身份值及不可变字符串，构造入口 internal；当前各自唯一 new 调用是同文件 Prepare，哈希输入与构造字段逐项相同。检索客户端、服务端及 Tools 源码，没有计划反序列化、外部哈希注入或其它构造入口；请求编码只写出该计划，不建立另一条读回路径。
- 删除 RequirePlan 中重复 ComputeIdentity 及哈希比较。Prepare 仍生成同一身份，请求仍检查 Actor/Tick、计划身份、原垂直速度与请求位移匹配，Finalize 仍检查 Actor、正步长、计划步长与求解前垂直速度；默认计划的零步长不能通过。没有通过新增有效标志或校验缓存绕过边界，也没有删除世界请求/结果哈希和恢复校验。
- KCC 与 Unity CharacterController 的消费链都传递请求中原只读计划；原始输入到计划的重力、终端速度、位移积分，及碰撞后的落地/撞顶垂直速度规则、最终速度除法保持原样。每次成功 Finalize 少一次完整计划编码和 SHA-256 计算及其分配，Prepare 的编码/哈希分配仍存在，不能宣称完整0 GC或实际耗时降幅。
- 静态核对所有构造/消费/编码引用、哈希与构造参数顺序、readonly 数据组成及差异；未编译、回放或采样。脚步预测、碰撞算法、数值公式及运算顺序未改动。

### AP55 世界求解器重复计算固定逻辑步长（2026-09-29，已实施，未运行）

- DeterministicKccWorldSolver 与 UnityCharacterControllerWorldSolver 构造时检查并锁定正整数 TickRate，但每次 ResolveBatch 仍通过对应 Scalar 的 One / FromInt64(TickRate) 计算同一个步长。TickRate 字段没有其它消费方，也不在 Restore/Reconstruct 时修改。
- 两个求解器改为构造时使用原表达式计算 readonly m_TickDelta，替换原仅用于求步长的 m_TickRate；批求解读取同一值，不并存派生缓存与可修改源，不新增有效标志。定点数仍沿同一 Q32.32 除法和舍入，Float32 仍沿同一转换、除法和位存储，没有以乘倒数替换角色速度除法或改变计算顺序。
- TickRate 的正值检查仍先执行；输入为正 int 时定点转换在 long 范围内且除数非零，Float32 转换/倒数有限。仅将这次固定运算从逐批移至装配，不改变允许的 TickRate、世界状态格式、恢复生命周期、碰撞和角色逐步计算。预测接口使用其传入步长的路径未改动。
- 减少周期固定转换与除法；原操作为值类型计算，本项不计为托管分配消除。核对字段全部引用、数值实现、构造与恢复路径，差异检查通过；未编译、回放或采样，不宣称耗时收益，脚步预测保持不变。

### AP56 周期身份哈希反复创建编码 writer（2026-09-29，已实施，未运行）

- AP53 去除嵌套世界状态临时数组后，两域 WorldSolveBatchCodec 的请求/结果哈希仍各自 new CanonicalWriter；AP54 去除消费端重复哈希后，身体运动 Prepare 仍逐角色创建 writer。Float32 的 ObservedWorldConstraintCodec.ComputeHash 也在每次生成观察帧身份时创建 writer，包括空观察帧。每次都会新建默认256字节缓冲，并可能重复扩容。
- 沿项目既有线程内哈希工作区方式，两个数值域的批哈希、身体运动哈希以及 Float32 观察帧哈希分别持有私有 ThreadStatic writer。每次完整编码前 Reset，批请求和批结果在同一 codec 内复用同一工作区，其余编码模块各自持有；没有新增进程、线程、对象池或另一套编码实现。
- 静态核对入口均为同步执行：计划为只读值，批请求/结果、世界状态、观察帧为封闭类型且集合由内部数组持有；写入只读取字段和数值位，不调用外部枚举器、业务回调或再次进入同一哈希入口。批请求中的观察约束使用 Write 接收外层 writer，不调用观察帧 ComputeHash。线程之间通过 ThreadStatic 隔离，不把可变 writer 存进返回结果。
- Reset 同时归零 Position/Length，后续完整覆盖本次有效范围，ComputeHash 只读取该范围，因此短记录不会包含之前长记录的尾部；编码异常不发布哈希，下次调用仍先 Reset。SHA-256 输出字符串每次独立生成，身份、格式、数值位、字段顺序和快照所有权保持；SHA-256 对象复用后续单独收口见 AP66。CanonicalWriter 只持有托管数组，Dispose 当前为空，不存在被省略的非托管释放。
- 代价为每个实际调用线程、每个上述模块保留其最大编码缓冲；首次使用、编码超过已有容量仍会分配，没有承诺准备阶段已覆盖所有线程/数据规模。只消除容量稳定后的周期 writer 与缓冲重建，哈希字符串、业务快照分配仍保留，不宣称全链0 GC或耗时收益。
- 完成调用链、线程内非重入范围、失败后重置、长短记录范围与差异静态检查；未编译、执行字节对比、回放或采样。脚步预测、碰撞算法及提交规则未改动。

### AP57 文本哈希拼接与整段 UTF-8 临时数组（2026-09-29，已实施，未运行）

- StableHash.Compute 原先调用 SimulationIdentity.Hash，先以 U+001F 连接所有文本生成 joined 字符串，再 Encoding.UTF8.GetBytes 分配整段字节数组；SHA-256 已返回经过检查的 StableHash，调用链却取出 Value 后再构造一次 StableHash，重复扫描64位小写十六进制。两域 SessionSnapshot 等消费此入口，配置期调用与快照期调用不混为同一种频率。
- StableHash.Compute 使用私有线程内 CanonicalWriter，Reset 后依次写入字段及字段间0x1F字节，直接返回 writer.ComputeHash 的值。删除只返回字符串的 SimulationIdentity.Hash；仅有的两个 Timeline 空目录调用改用 StableHash.Compute(...).Value，目录身份内容、时序与 Timeline 行为不变。
- 将 CanonicalWriter.WriteString 原有256字符分块编码提取为内部 WriteRawUtf8(ReadOnlySpan<char>)，保留代理对跨块时回退一字符、相同 UTF-8 编码器和写入顺序；WriteString 仍先写原长度前缀。流水线快照已有的同样分块实现迁到此入口，删除重复 WriteHashText 与单字节 WriteSeparator，不并存两套编码逻辑，现有快照文本片段顺序及数值格式化不变。
- 空参数数组/null数组仍编码零字节，null/空字段仍不写内容但保留字段间分隔符；分隔符阻断相邻字段的代理对连接，同一字段的有效代理对不会在块边界被拆开，沿原 UTF-8 规则编码非法代理字符。外部字符串创建 StableHash 的格式检查保留，仅去掉生成结果拆成字符串后再校验一次的往返。
- 容量稳定后省去 joined 字符串与整段 UTF-8 byte[]，以及重复64字符检查；params 数组、调用方 ToString、SHA-256对象及最终哈希字符串仍可能分配。线程首次使用和容量增长仍分配，线程保留最大编码缓冲。不声称实测收益或全链0 GC。
- 静态核对所有旧入口调用、程序集边界、编码分块、分隔符/空值规则、快照字段序列及差异；未编译、运行字节或哈希对比、回放或采样。脚步预测和数值算法未改。另确认 Unity CharacterController 周期转换会拼错误来源字符串，但绑定身份仍可运行时重配，尚未将其缓存以改变重绑定后的报错来源。

### AP58 流水线快照哈希的数字临时字符串（2026-09-29，已实施，未运行）

- 复核 AP16，当前图/状态机请求页与去重集合已经按装配 SourceRequestLayout 上界预留，未重新实施或扩大其结论。续查 SimulationPipelineStateSnapshot.ComputeHash，流水线 SchemaVersion、LastCompletedTick 和每个参与者 StateSchemaVersion 仍先 ToString，再立即写入 UTF-8 哈希缓冲。
- 每次 ComputeHash 在栈上建立一份20字符工作区，三类数字使用 TryFormat 写入并立即按实际长度编码。两个版本号保留原 int.ToString 的默认格式与当前区域设置，Tick 保留 InvariantCulture；没有改用二进制整数、补零格式或改变哈希字段顺序。
- 两种 SchemaVersion 在正式构造边界要求正 int，十进制最多10位；Tick 为 ulong，最多20位，工作区覆盖完整范围，无须扩容或失败兜底。数字只在本次同步调用内消费，不暴露缓冲、不缓存 Tick 值；参与者循环复用同一栈空间且仅读取本次写入长度。
- 每次快照哈希去掉2加参与者数量次 ToString 调用及其临时字符串需求，不据此假定具体运行时的小整数字符串缓存行为或实际分配字节。最终哈希字符串、SHA-256对象及其它快照分配仍存在。静态核对数字类型/正值边界、既有 TryFormat 用法、格式参数、字段顺序与差异；未编译、执行哈希对比、回放或采样，脚步预测及数值算法未改。

### AP59 活动角色与观察约束名单交叉扫描（2026-09-29，已实施，未运行）

- Float32 WorldSolveBatchRequest.Reset 在每批接收观察约束时，对每个观察角色遍历全部活动请求，检查同一 ActorId 不得同时处于活动/观察两类。该检查原为活动数乘观察数的逐项比较，不产生额外集合，但角色数量增长时重复工作扩大。
- 活动请求已在 Reset 前段按 ActorId.CompareTo 排序并检查重复；ObservedWorldConstraintFrame 的两个构造入口最终均排序并检查重复/目标Tick，空帧和预测状态读回也沿相同构造。ActorId 的排序与相等均使用 Ordinal。将活动索引放到观察循环外，按观察角色递增顺序只向前推进，在当前位置检查相等。
- 活动索引最多推进活动数次，观察循环仍按原顺序、最多执行观察数次；活动名单耗尽后，后续观察 Actor 不可能再重叠，直接结束。整体比较为两份名单数量之和的量级，不建临时 HashSet/Dictionary、不增加缓存字段或重排请求。首个重叠的观察 Actor、异常类型/文本/参数及发生在 RequestHash 发布前的位置不变；无观察、无活动、全部观察在活动之前/之后及交错名单均沿同一实现处理。
- 固定数值域没有这份观察约束合同，未强加新逻辑。当前 KCC 实测场景的热点证据不能用于该 Float32 路径，不宣称该修改改善已采集场景或给出耗时收益。静态核对全部观察帧构造/读回入口、排序比较规则与差异；未编译、运行或采样，脚步预测、碰撞和 Commit/Discard 未改。

### AP60 KCC 接触批次诊断重复格式化同一摘要（2026-09-29，已实施，未运行）

- PublishActorContactDiagnostics 在 diagnostics.IsEnabled 为真时，逐绑定角色发布批次摘要记录。每条记录都使用相同的接触 summary，却在循环内重新插值 pairs/checks/sweeps/clips/depenetrations/iterations/validations 文本，只有 ActorId 不同。
- 将原表达式移到已启用诊断的分支内、角色循环之前，同批记录共享一份不可变 detail 字符串；记录结构、数量、顺序、每条 ActorId、时间、成功/失败标记与逐对轨迹发布保持。KCC 构造要求非空绑定名单，故不会为原本零记录的批次额外构造文本；SimulationWorldTraceRecord 直接持有字符串，不修改内容或复制字符。
- 一次批次诊断由角色数次格式化减少为1次，只有多角色且诊断启用时减少重复字符串构造。未启用诊断仍在整理文本前返回，不将此项计为普通运行的收益，也不宣称实测 GC 或耗时降幅。正常/失败发布都沿同一函数，没有新增缓存状态或改变碰撞计算。
- 静态核对名单非空约束、记录字符串归属、两个发布调用与差异；未编译、运行或采样。KCC 仍有无条件 Stopwatch 读取及汇总工作，AP49 并未覆盖这些部分，本次也没有将其误记为已裁剪；脚步预测未改。

### AP61 KCC 普通构建仍读取诊断计时器与调用发布入口（2026-09-29，已实施，未运行）

- AP49 只裁剪逐对轨迹，AP60 只共享启用诊断时的摘要文本；ResolveBatch 仍无条件记录每个 Motor 的起止时间和接触批次时间，再由发布方法检查 sink.IsEnabled。FixedSimulationDiagnosticsAggregate.IsEnabled 本身还会扫描 Actor sink，因此关闭采样不等于这些调用没有成本。
- 按既有 UNITY_EDITOR 或 KK_DIAGNOSTIC_SAMPLING 编译边界，将 Motor/接触批次计时、四类发布调用及五个私有诊断格式化/发布方法放入诊断分支；候选页的 ElapsedStopwatchTicks 字段、构造参数及赋值也在同一条件。普通构建不会向传入 sink 发布这些 KCC 诊断，即使外部 sink 声明启用；这与用户确认的普通构建不采样规则一致，不提供旁路开关。
- 两处 catch 原先仅发布诊断后 throw，不负责撤销或修正业务状态。普通构建移除仅为诊断服务的 try/catch，保留原求解语句块，异常直接传播；诊断构建保留原捕获、记录和重新抛出。Motor.Move、接触迭代、最终验证、角色状态生成、状态编码与提交顺序不变，没有增加吞错或默认成功。
- 查询/接触 summary 的原计算与 checked 算术仍保留，没有借编译裁剪顺带改变原错误边界；部分诊断字段和计数仍由求解器计算，本项不宣称整个 KCC 无诊断工作。已开启诊断但 sink 未启用时仍沿原计时规则，不引入中途启停时间不完整的缓存标志。
- 对两份源文件进行预处理条件的静态文本展开：两个宏均关闭时 Stopwatch 调用和计时字段引用均为0；任一开启时保留原六个计时调用位置，构造调用/签名与字段赋值同步存在。核对全部私有发布和计时字段引用及差异；这不是 C# 编译验证，未启动编译、回放或采样。无新增测试，脚步预测和碰撞数值算法未改，实际收益未知。

### AP62 KCC 普通构建仍复制候选页诊断字段（2026-09-29，已实施，未运行）

- AP61 裁剪发布入口后，ActorSolveCandidate 仍无条件保存 StepDiagnostics、Remaining、MovementIterations、HasBlockingContact、BlockingContact、BlockingContactCount、Termination、NoProgressConfirmationCount 八个字段；它们的唯一读取点为已按诊断条件裁剪的 PublishDiagnostics 调用。候选页在接触修正、静态重约束及最终验证间按值读取/写回，普通构建仍携带这些不消费的字段。
- 将八个字段、构造参数、赋值及 MotorResult 取值统一放入 UNITY_EDITOR 或 KK_DIAGNOSTIC_SAMPLING 分支。普通构建不再取代表阻挡接触或复制这些候选数据；开启诊断时仍保存并发布相同内容。没有另外建简化候选类型、运行时标志或第二条求解路径。
- Requested 和 PreviousState 仍供 ReconstraintAfterMovement 使用，QuerySummary 仍按原 checked 规则汇总，均保留。Position/Ground/Collision、Actor 请求、Motor 内部步阶/阻挡/终止计算及结果结构保持；未通过减少记录字段去裁剪运动算法。BlockingContactAt(0) 原调用受 HasBlockingContact 控制，Move 先复制活动接触后返回计数，本次只消除供诊断读取的代表值。
- 对变更前后两份文件进行条件静态展开：诊断开启分支去掉空白后源码一致，普通分支无这八类 candidate/motorResult 诊断读取；全部部分类型字段引用和构造调用已核对，差异检查通过。减少普通构建候选结构的诊断存储及传值工作，不报告未测结构尺寸、GC降幅或毫秒收益。未编译、回放或采样，脚步预测未改。

### AP63 世界快照编码重复创建临时 writer（2026-09-29，已实施，未运行）

- 两域 WorldSimulationStateCodec.Write 在生成世界快照字节时仍逐次 new CanonicalWriter；ComputeHash 在启动/恢复验证时也重建同类工作区。AP53/56 已优化批请求与结果的外层哈希，但不覆盖这里直接返回独立世界状态字节的入口。
- 每个数值域的 codec 使用独立私有 ThreadStatic writer，Write/ComputeHash 在检查输入后由 PrepareWriter 重置位置和长度，再调用原唯一 WriteCanonical。Write 仍通过 ToArray 分配并复制独立结果，ComputeHash 仍返回独立哈希；不将 WrittenSpan 或内部数组借给快照。
- WriteLengthPrefixed 继续只使用调用方提供的外层 writer，没有改为取内部工作区，不会重置批请求/结果已经写入的字段。原 Magic/Version、字段顺序、数值位、长度前缀、空值错误及读取端全部保持；源为封闭 WorldSimulationState 及其数组数据，编码没有业务回调或同 codec 重入。
- Reset 后只读取本次有效长度；异常不返回部分结果，下一次编码重新 Reset。首次使用与超过已有容量仍会分配，每线程每域保留最大编码缓冲；独立快照字节、哈希对象和哈希字符串仍有分配。本项去掉容量稳定后的 writer/缓冲重建，不宣称快照链0 GC或实测收益。
- 核对三个独立字节消费位置、启动/恢复哈希调用、外层长度前缀调用及 ToArray 所有权；差异静态检查通过，未编译、运行字节对比、回放或采样。世界快照已有数组内部构造入口，本次未重复改造；ActorSnapshot 的状态字节复制仍需沿独立所有权审计，未因同处快照链而擅自取消。脚步预测未改。

### AP64 Fixed ActorSnapshot 重复复制已独立的状态字节（2026-09-29，已实施，未运行）

- 当前 Fixed SimulationActorSnapshot 构造对输入字节再次 Clone；两个正式构造调用分别来自快照生成的 FixedCharacterRuntimeStateCodec.Write 和快照读回的 CanonicalReader.ReadBytes。前者用 ToArray 产生独立数组，后者按长度复制输入字节，不是工作区借用或历史页引用。生成端在构造后不再保存或修改该数组，StateBytesBuffer 的唯一后续用途为编码读取。
- 将 Fixed 参数及生成端局部变量明确命名为 ownedStateBytes，构造直接接管数组，沿用项目 Float32 已有的同一所有权约定。调用方交付后不得写入；两个现有 Fixed 调用都满足此约定。身份/codec 检查、非空检查、解码与哈希验证保持，空参错误参数名随正式参数改为 ownedStateBytes。
- 快照生成每 Actor、省去对整份状态的一次新数组与复制；读回也去掉 ReadBytes 之后的第二次复制。最初编码/读取的独立数组仍保留，没有共享可复用缓冲、取消历史隔离或增加复制开关/兼容构造入口。
- 复核纠正：Float32 当前已经直接接管 ownedStateBytes，不能把 Fixed 的 Clone 推断到另一数值域。Float32 权威基线合并原本使用基线持有的字节，本次未改变该已存在的共享规则，也没有新增 Clone；实际代码改动仅在 Fixed 世界快照文件。
- 静态核对全部 SimulationActorSnapshot 源码引用、两个 Fixed 生产者、读取/编码消费者、ToArray/ReadBytes 独立数组语义及差异；未编译、运行快照隔离/哈希对比、回放或采样。独立快照数组和对象分配仍存在，不宣称全链0 GC或实测收益，脚步预测未改。

### AP65 Pose Graph 输入端口重复解析（2026-09-29，已实施，本轮未编译）

- GraphRuntime 原已在 `ReadInput<T>` 每次调用 `node.GetInputPort` 并做类型转换；Additive、Blend Pose、Layered Bone Blend、Modify Bone、Foot Placement 和 FullBodyIK 的可选权重/目标输入还会先查一次连接，再通过 `ReadInput` 第二次查同一端口。这些输入都在正式 RuntimeShape 中，节点和端口身份在图装配后固定。
- `BuildPortDefinitions` 在 `BindNativeIdentity` 后解析正式端口定义时，一次性取得每条 Input 的 `FlowCanvas.ValueInput`，核对其 `type` 等于 `RuntimeBindingType(kind)`，按 NativeNodeId 和 PortId 缓存。`ReadInput`、`ReadInputValue` 改读缓存；端口缺失、类型不符和重复端口仍在图初始化边界失败。
- 新增 `TryReadInput` 沿缓存执行同一次类型检查和 `isConnected` 判断；未连接返回 false，连接后读取值并保留原“值缺失”错误。Additive、Blend Pose、Layered、Modify Bone 的未连接默认权重，Foot Placement 的 weight override，FullBodyIK 的 optional contribution/goals 改为一次查询。Required contribution 仍短路后由 `ReadInput` 报缺失；端口形状、连接语义、默认值和异常边界不变。
- 子图 `BindInterface` 中的 `GetInputPort` 保留：它在 Start 后的接口绑定阶段执行一次，用于建立父/子端口映射，不是帧路径重复拓扑查找。图实例新增一个定容输入端口字典；这是装配期常驻状态，未新增配置、fallback 或第二条运行路径。
- 已静态核对 `RuntimeShape` 对全部相关可选端口的声明、初始化与运行阶段顺序、可选连接和 Required 分支、Dispose 清理及差异。本轮只做静态检查，未编译、回放或采样；不宣称耗时收益。

### AP66 周期哈希反复创建 SHA-256 对象（2026-09-29，已实施，本轮未编译）

- `SimulationCanonicalPayloadHash.Compute` 每次调用 `SHA256.Create()`；该入口被 pipeline snapshot、world/batch/body/observation 等正式哈希共用，AP53–AP56 已复用编码 writer，但 cryptograhy 对象仍在每次 Compute 重建。
- 改为线程内 `ThreadStatic SHA256`，首次使用创建；`TryComputeHash` 仍写入栈上32字节结果，摘要、十六进制输出字符串、字段顺序和错误检查保持。线程之间不共享可变 cryptography 对象，返回值不借用内部状态。
- 首次使用和线程创建仍分配；最终64字符哈希字符串与业务快照所有权分配保留。该修改不宣称 SHA256 CPU 耗时、GC 字节或整链收益。已静态核对 Compute 两个入口、using 生命周期替换、ThreadStatic 边界和调用者同步消费；本轮未编译、运行哈希对比、回放或采样。

### AP67 EventGraph 宿主输入重复解析合同（2026-09-29，已实施，本轮未编译）

- `HostEventGraph.ReadInput<T>` 原来每次都按字符串扫描 `EventGraphHostContract` 输入数组，再做节点类型与描述符类型检查；`EventGraphHostInputNode<T>` 和 Delta 节点的每个输出读取都会重复这条合同查找。输入节点数量与消费者数量增大时，周期扫描按读取次数增长。
- `EventGraphHostInputNodeMarker` 现在统一暴露正式 `InputId`、`ValueType` 和 `BindInput`。源图校验删除逐个具体输入节点类型的分支，改用同一 marker 合同核对输入 ID 和类型；克隆实例在 `InitializeVariableOutput` 阶段按合同定位描述符并绑定到节点。类型匹配仍只在源图 `EventGraphAssetValidator.Require` 边界确认，实例绑定只负责找到对应描述符，不重复类型校验。
- 运行时 `ReadInput` 改为直接消费已绑定描述符，仅检查当前 invocation 和 `IEventGraphHostContext.TryRead`；普通节点读取仍调用一次 `EventGraphValue.As<T>`。激活 invocation、缺失输入、类型不匹配的校验时机和异常传播保持。没有新增输入缓存值、整帧缓存、fallback 或第二条图执行路径。
- 装配期增加每个输入节点的描述符引用；Reset 后重建克隆时沿原初始化链重新绑定。该修改减少周期合同数组扫描和重复类型检查，不宣称宿主上下文自身查找、EventGraph 总帧耗时或 GC 的实测收益。已静态核对正式输入节点、Delta 节点、源图校验、克隆初始化和旧 `ReadInput<T>` 调用清理；未编译、运行或采样。

### AP68 Fixed 角色评估开头重复清理工作区（2026-09-29，已实施，本轮未编译）

- `FixedCharacterEvaluationRuntime.Evaluate` 原来在入口清理 action runtime 字典、共享 Effect scratch、全部 ability workspace、timeline advance/stop 名单和 evaluation output；同一函数的成功返回和 catch 路径也已调用对应清理。
- `FixedAbilityEvaluatePass` 是唯一正式 Evaluate 入口，按 roster 同步执行；`SimulationActorBinding` 构造时这些容器为空，成功路径和异常路径的清理构成完整生命周期边界。入口参数检查与 `RuntimeState.Restart` 不写入这些工作区，因此入口重复清理只维护一个不存在的中间状态。
- 删除入口的字典清空、scratch Reset、workspace Reset、timeline 名单清理和 output 清理。成功结果仍先复制数组，随后清理；异常路径仍丢弃 timeline pending 并清理全部工作区。评估业务顺序、候选状态提交、Discard 语义和异常传播不变。
- 每个 Fixed actor 每次 Evaluate 少一轮重复容器清理；字符串字典、ulong 集合和多个 List 的 Clear 工作随成员数量变化。没有新增状态标志、备用入口或生命周期假设。静态核对唯一 Evaluate 调用、actor 容器构造、成功/异常清理链和 timeline 读写；未编译、运行回放或采样。

### AP69 Float32 角色评估开头重复清理工作区（2026-09-29，已实施，本轮未编译）

- `Float32CharacterEvaluationRuntime.Evaluate` 与 Fixed 同样在入口清理 action runtime 字典、共享 Effect scratch、ability workspace、timeline advance/stop 名单和 evaluation output；其成功返回和 catch 路径已经执行同类清理。
- `Float32AbilityEvaluatePass` 是唯一正式 Evaluate 入口；`SimulationActorBinding` 构造时相关容器为空，成功与异常出口构成完整清理边界。入口参数检查与 `RuntimeState.Restart` 不写入这些工作区，入口清理不保护任何可达状态。
- 删除入口的重复字典清空、scratch Reset、workspace Reset、timeline 名单清理和 output 清理。结果数组先复制后清理，异常路径仍丢弃 pending 并清理；求值顺序、候选状态、Commit/Discard 和异常传播不变。
- 每个 Float32 actor 每次 Evaluate 少一轮重复容器清理；字典、集合和 List 的实际工作随能力和成员数量变化。静态核对唯一 Evaluate 调用、actor 容器构造、成功/异常清理链；未编译、运行回放或采样。

### AP70 Evaluate Pass 入口重复清理 Ingress（2026-09-29，已实施，本轮未编译）

- Fixed 和 Float32 `AbilityEvaluatePass` 原来在 `PrepareIngress` 前逐 actor 清空 ingress 数组和计数；同一次 `Execute` 的 finally 也执行同一清理。上一轮成功或异常后，finally 已把数组有效长度清零并复位计数，因此入口清理维护的是不存在的残留状态。
- 将 `PrepareIngress` 移入现有 try。若复制 ingress 中途异常，catch 仍只重新抛出；此时上一轮结果已被 finally 置空，catch 的 `DiscardUnconsumed` 不会重复处理。finally 立即清理本轮已填充的有效长度，构造新数组时的 Array.Copy 行为不变。
- 删除每个 pass 每次执行前的逐 actor `Array.Clear` 和计数清零；保留 finally 边界清理和容量增长逻辑。pipeline 写入顺序、actor 排序检查、评估调用和异常传播不变。
- 两个数值域同步修改，静态核对 pass 构造、上一轮 finally、catch 空结果处理、PrepareIngress 填充和 finally 清理；未编译、运行回放或采样。

### AP71 Commit 批内重复清理值类型 Dispositions（2026-09-29，已实施，本轮未编译）

- Fixed 和 Float32 Committer Adapter 在每个 step 前清除上一次 disposition 数组有效范围，finally 再次清除；数组元素是 `readonly struct`，唯一消费者 `SimulationCommitter.Commit` 在同一同步调用内按 count 索引后立即释放，不保存数组引用。
- 删除两处 `Array.Clear`，保留每 step 写入前计数复位和 finally 计数复位。扩容时只复制上一轮有效数量，新增尾部不会被本轮 count 消费；输出事件数量检查、disposition 去重、排序和提交顺序不变。
- 两个数值域同步修改，静态核对 `SimulationOutputDisposition` 类型、Adapter 到 Committer 的同步调用、count 构造和异常清理链；未编译、运行回放或采样。

### AP72 Commit 排序工作区逐 Actor 重复清理（2026-09-29，已实施，本轮未编译）

- 两域 `SimulationCommitter` 每个 Actor 排序前清除上一次 `OrderedOutput` 有效范围；该 struct 只含两域 readonly struct 输出和 bool，不持托管引用。数组由 Committer 私有复用，排序和提交只读取当前 `m_OutputCount`，不返回或保存切片。
- 删除逐 Actor `Array.Clear`，保留写入前计数复位。同一 Commit 内新 Actor 覆盖当前有效范围；扩容只复制旧有效数量，容量尾部不进入排序或发布。输出数量检查、排序键、Suppress、Replace/Retire 和异常传播不变。
- 两个数值域同步修改，静态核对 OrderedOutput 字段、数组唯一消费边界、容量增长和异常路径；未编译、运行回放或采样。

### AP73 Commit Disposition 字典入口重复清理（2026-09-29，已实施，本轮未编译）

- 两域 Committer Adapter 在 Commit 入口清空 disposition 字典，finally 又清空一次。字典由同一 Adapter 私有复用，值是 readonly struct；上一轮成功或异常的 finally 已经形成唯一空表边界。
- 删除入口重复 Clear，保留 finally 清理。批内 disposition 仍按 SourceEventId 加入并检查重复，随后由 Commit 消费；异常时保留的中间表不会跨 Commit 暴露。
- 两个数值域同步修改，静态核对字典唯一写入消费边界、上一轮 finally、异常传播和重复 disposition 检查；未编译、运行回放或采样。

### AP74 外层事务开始重复清理 Completed Port（2026-09-29，已实施，本轮未编译）

- 两域 Pipeline Transaction 在 `BeginOuterTransaction` 清空 completed port；Coordinator 每次事务 finally 的 `ClearTransientState` 已同时清理 working 和 completed port，Port 构造和首次事务前没有残留。
- 删除外层开始时的重复 completed 清理，保留 finally 唯一清理边界和每 step 后的 `SetCompletedSteps` 覆盖。Product 生命周期重置、step 连续性检查、egress 和 commit batch 生成不变；即使 `BeginOuterTransaction` 内部部分失败，finally 仍会完成 transient 清理。
- 两个数值域同步修改，静态核对 Coordinator 成功、Pending、恢复、异常和 finally 调用顺序；未编译、运行回放或采样。

### AP75 Source Egress 读取前重复清理工作区（2026-09-29，已实施，本轮未编译）

- 两域 `ReadSourceEgress` 在复制前清空 `workspace.Egress`；Session workspace 在 `BeginTransaction` 重置，上一轮 finally 的 `EndTransaction` 也重置。`FreezeCommitBatch` 每个外层事务只调用一次该读取入口。
- 删除读取前重复 Clear，保留 workspace 事务边界清理和按 slot count 覆盖写入。缺失 Source Egress 产品时返回空 workspace，冻结后的独立数组长度不变；异常路径仍由 workspace 结束边界清理。
- 两个数值域同步修改，静态核对 workspace 重入检查、Begin/End 生命周期、唯一读取调用和容量增长；未编译、运行回放或采样。

### AP76 ForceStop 访问集合入口重复清理（2026-09-29，已实施，本轮未编译）

- `OperationExecutionLifecycleRuntime<TTarget>.ForceStop` 根层调用先清空 `m_ForceStopVisited`；同一根层 finally 也清空。构造时集合为空，任何成功、取消或异常路径都由 finally 建立唯一空集合边界，嵌套调用不会重复拥有。
- 删除根入口重复 Clear，保留深度计数、嵌套判定和 finally 清理。递归环检查、根操作重复停止、诊断和异常传播不变；下次 ForceStop 起点仍是空集合。
- 静态核对 ForceStop 唯一根入口、嵌套递归、finally 异常路径和 HasTransientState；未编译、运行回放或采样。

### AP77 Ability Action Store 评估开始重复清理名单（2026-09-29，已实施，本轮未编译）

- Fixed 和 Float32 ActionStateStore 在 BeginEvaluation 清空 `m_EvaluatedActions`；上一轮 Complete 或 Abort 已通过 EndEvaluation 清空。Begin 的栈检查继续拒绝残留 skill execution stack，但不重复清理已建立为空的名单。
- 删除 Begin 的重复 Clear，保留 End 唯一清理边界和每轮 RetainEvaluatedAction 写入。执行栈状态检查、技能执行上下文、评估名单顺序和异常 Abort 链不变。
- 两个数值域同步修改，静态核对 Complete、Abort、Begin/End 生命周期和异常传播；未编译、运行回放或采样。

### AP78 Blackboard 评估开始重复清理投影（2026-09-29，已实施，本轮未编译）

- 两域 BlackboardRuntime 的 projection List 和 key HashSet 与 AbilityExecutionWorkspace 共享。上一轮 Complete/Abort 后 actor 调用 ClearWorkspaces，Workspace.Reset 已清理这两类容器；构造和首次评估时也为空。
- 删除空职责 BeginFrame 入口及其重复 Clear，保留 EndFrame 在 Flush 后清空、Workspace.Reset 边界和投影去重键维护。投影查询、写入和提交顺序不变。
- 两个数值域同步修改，静态核对共享容器唯一所有权、Begin/End 生命周期、异常 Abort 后 workspace 清理；未编译、运行回放或采样。

### AP79 GameplayEffect Target 开始重复清理暂存（2026-09-29，已实施，本轮未编译）

- 两域 GameplayEffectTarget 的 `Changes` 和 `Causes` 是 shared Execution Scratch 的唯一容器。上一轮 Target.End 清空，actor 完成后 Scratch.Reset 再建立空边界；首次构造也为空。
- 删除 Target.Begin 对 Changes/Causes 的重复 Clear，保留 End 唯一生命周期清理和 Begin 重建 active causes。变更追加、Trim、causes 重建、Savepoint/Discard 和异常传播不变。
- 两个数值域同步修改，静态核对 Begin/End 调用、shared scratch Reset、嵌套暂存生命周期和异常路径；未编译、运行回放或采样。

### AP80 Ability Trace 采样计数和序号重复复位（2026-09-29，已实施，本轮未编译）

- 两域 TraceSink 在 Begin 复位 value sample count 和诊断序号；上一轮 End 已复位 count 并释放 frame/sequence，Frame.Begin 的 Bind 也会重建 sequence 并复位采样计数。
- 删除 Begin 的重复复位，保留开关设置、End 唯一释放边界、采样上限和序列生成。开关切换、trace 事件顺序、诊断关闭路径和异常传播不变。
- 两个数值域同步修改，静态核对 Begin、Bind、End 生命周期、Abort 后 Frame.End 和采样上限计数；未编译、运行回放或采样。

### AP81 Ability Frame 输出列表重复清理（2026-09-29，已实施，本轮未编译）

- Fixed 和 Float32 AbilityExecutionFrame 的 Facts/Presentation/Trace List 来自 AbilityExecutionWorkspace。Complete 先复制再 Frame.End，Abort 也走 Frame.End；随后 CharacterEvaluationRuntime 的成功或异常出口调用 ClearWorkspaces，Workspace.Reset 再清同一容器。
- 删除 Frame.End 的重复列表 Clear，保留 trace 状态释放和 action trace context 复位。Complete 的复制顺序、Savepoint 截断、异常 Abort 和 workspace 唯一清理边界不变。
- 两个数值域同步修改，静态核对 End 唯一调用点、Complete 复制、Abort 生命周期和 Workspace.Reset 边界；未编译、运行回放或采样。

### AP82 Ability Domain 首次名单复制重复清理（2026-09-29，已实施，本轮未编译）

- 两域 AbilityDomainRuntime 每次 Tick 调用 CopyCurrentActions 两次；方法原先每次清空 results。第一次进入时，上一轮 finally 已清空 m_CurrentActions，构造和首次评估也为空；第二次是提交 pending control 后刷新同一名单，仍需要清空。
- 将 Clear 移到第二次调用前，CopyCurrentActions 只负责按当前 Action 状态追加。ProcessExisting/TickActive 读取的名单、删除实例后的刷新结果和 finally 清理边界不变。
- 两个数值域同步修改，静态核对唯一调用链、首次空列表前提、第二次刷新和异常 finally；未编译、运行回放或采样。

### AP83 Skill 执行评估开始重复清空状态缓存（2026-09-29，已实施，本轮未编译）

- 两域共用的 `GameplayAbilityExecutionManager<TValue>` 在 `BeginEvaluation` 清空 `m_States` 和 `m_StatesShared`；上一轮成功或异常的 `EndEvaluation` 在 active frame 检查通过后执行同一清空。构造和首次评估也为空，active frame 残留时 Begin 与 End 都直接报错，不会清理。
- 删除 Begin 的重复复位，保留 End 唯一空缓存边界。评估内 Enter/Remove/写状态仍通过该缓存读写 storage，评估外 `WriteState` 触发的 `Remove` 会重新从 storage 建立共享聚合；`HasFrame` 本来只读 storage。Action 执行栈检查、frame 身份检查、克隆/写入和异常传播不变。
- 静态核对 manager 全部字段引用、ActionStateStore Begin/End 唯一调用、评估内 Enter/Exit 和评估外写状态链；未编译、运行回放或采样。

### AP84 状态机入口边周期重复解析（2026-09-29，已实施，本轮未编译）

- `OperationStateMachineRuntime.Tick` 每次执行 StateMachine 都通过 `FindOwnedEntry` 扫描 Enter 边查找 AnyState；首次无 active state 时又扫描同一列表找第一条非 AnyState。拓扑和控制边在装配后固定，这些结果是派生身份。
- `OperationExecutionTopology` 构造期按既有排序后的 Enter 边预解析 StateMachine 的 initial 和 AnyState 目标。initial 保持原两段逻辑：先取第一条非 AnyState，否则取第一条边；AnyState 仍取第一条 AnyState。状态机 Tick 直接读取预解析结果，删除原周期扫描入口。
- 每个状态机每次 Tick 少一次 Enter 边扫描，首次激活少两次；无 AnyState 或仅有 AnyState 的图结果不变。拓扑新增两个定容目标数组，不新增运行时配置、fallback 或第二条执行路径。静态核对唯一调用链、边排序、端口匹配、OperationHandle 默认无效值和差异；未编译、运行回放或采样。

### AP85 状态机身份槽周期生成 handle 字符串（2026-09-29，已实施，本轮未编译）

- 状态机激活和过渡每次用 `FormatHandle` 把 active/exiting/pending 的固定 OperationHandle 转成十进制字符串；诊断记录也重复生成同一字符串。OperationHandle 由拓扑装配固定，十进制身份不变。
- `OperationExecutionTopology` 构造期为全部 operation 预生成 InvariantCulture 身份字符串，新增 `OperationIdentity`。状态机写身份槽与状态过渡诊断改为读取同一字符串，删除状态机私有 `FormatHandle`。身份槽存储内容、诊断字段顺序和空 handle 不出现在这些正式路径的规则不变。
- 每次状态激活或过渡少两次到三次临时字符串；首次装配增加定容字符串数组。路径、过渡身份和状态槽检查顺序不变。静态核对全部旧调用、目标身份语义、诊断专属字符串和差异；未编译、运行回放或采样。

### AP86 状态机执行路径临时数字字符串（2026-09-29，已实施，本轮未编译）

- 每次激活状态生成 execution path 时，机器 handle、状态 handle 和 generation 仍先调用 `ToString`/插值再拼接。AP85 已去掉 handle 临时字符串，但 generation 的临时字符串和最终拼接分配仍在。
- 使用 `string.Create` 按父路径、固定文本、两个 handle 和 generation 的精确字符数分配一次最终字符串，静态局部函数用 InvariantCulture `TryFormat` 写入。原路径格式 `parent/sm:machine/state:state@generation`、handle 十进制、generation 非零范围和写入 slot 的所有权不变。
- 每次状态激活去掉 generation 临时字符串，并合并中间拼接目标；最终 path 字符串仍必须分配。静态核对长度公式、ulong 位数、文本顺序、插槽消费和差异；未编译、运行回放或采样。

### AP87 Action 拒绝记录先构造后判断采样（2026-09-29，已实施，本轮未编译）

- `ActionSkillActivationFlow` 的 immediate、pending control 和 pending replacement 拒绝路径先插值生成 detail，再交给内部检查 `TraceEnabled` 的 Trace；pending control 还直接调用结果记录。关闭采样时仍构造 rejection 字符串。请求源缺失路径也先访问 Tick 再格式化。
- 四处将 `!TraceEnabled` 提前返回移到 detail 构造前；同一条拒绝的 action trace 和 action result 继续共用同一字符串，记录类型、代码、顺序和返回值不变。请求不可用时先设置 default request 再跳过 trace。Trace 开关由评估期 TraceSink 固定，单次流程内不会中途切换。
- 关闭采样时这些 rejection/请求缺失路径不再做字符串插值；开启采样时输出不变。静态核对四个入口、共用 detail、Trace/TraceActionResult 内部边界和差异；未编译、运行回放或采样。

### AP88 Execution Plan ingress 身份周期 ToString（2026-09-29，已实施，本轮未编译）

- 两域 Pipeline Transaction 的 `ValidateIngress` 每条 ingress 都把 header 的 `StableHash.FactIdentity` 转成 64字符字符串，再与 execution plan 已持有的 string 身份做 Ordinal 比较。该验证每 step 执行，身份本身已固定。
- 改为读取 `StableHash.Value` 直接比较；StableHash 构造边界已保证 64位小写十六进制，execution plan 的 FactIdentity 字符串排序与去重规则不变，异常条件和消息不变。
- 每条每 step ingress 少一次整段身份字符串分配；匹配/不匹配行为不变。静态核对 StableHash 合同、两域唯一修改点、Ordinal 比较和差异；未编译、运行回放或采样。

### AP89 Action Lifecycle Ingress 身份重复字符串化（2026-09-29，已实施，本轮未编译）

- 两域 ActionRuntime 把 ingress header 的 StableHash 转成 string 后传给内部 `AbilityLifecycleIngress`，该 identity 只用于匹配失败异常文本，却每条 lifecycle ingress 都分配。StableHash 本身已承载 64字符正式身份。
- 内部 ingress 结构 Identity 改为 StableHash，两个唯一 ActionRuntime 创建点直接传递 header 身份；匹配失败异常插值保持原字符串内容，仅在错误路径调用 ToString。匹配字段、优先级、状态更新和事实输出不变。
- 每条 Action lifecycle ingress 少一次 64字符分配；异常路径文本不变。静态核对结构唯一创建/消费链、内部访问边界和差异；未编译、运行回放或采样。

### AP90 Motion 来源身份周期拼接（2026-09-29，已实施，本轮未编译）

- `SimulationExecutionSource.Identity` 每次访问都拼 `skill-operation:` 或 character-control 组合文本。Fixed/Float32 Motion 解析对每个贡献调用它计算 fingerprint，owner 输出再调用同一文本；控制运动还会在 clock、timeline 和 contribution 间重复调用，Timeline motion 也按 contribution 间接重复。
- 两域 Ability Execution Services 在装配期从已有 SourcePath 预生成全部 `skill-operation:{path}` 身份。Corin control module 按状态缓存 execution source 和 identity；CharacterControlMotionRequest、AbilityTimelineLogicMotion 与 SimulationMotionContribution 携带来源 identity，Timeline 复制时一次生成并给所有 motion 复用。ResolvedMotionChannel 存储 owner identity，fingerprint 和输出直接读取。
- 每个 Skill/Timeline motion contribution 在周期解析中不再生成 identity；控制运动由每 tick 多次生成收敛为按状态首次一次。贡献来源、fingerprint 输入、owner 文本、clock/timeline 匹配、混合顺序和异常文本不变。新增定容身份数组、状态缓存和请求字段，不加 fallback 或第二执行路径。静态核对全部创建点、接口实现、默认通道返回、消费链和差异；未编译、运行回放或采样。

### AP91 状态生命周期事实周期字符串化（2026-09-29，已实施，本轮未编译）

- 两域 `NotifyStateLifecycle` 每次状态进入/退出都拼接 `state:{handle}`，并把 `OperationStateLifecyclePhase.ToString()` 写入正式事实。state handle 和 lifecycle 枚举在拓扑装配后固定。
- `OperationExecutionTopology` 装配期为 State operation 预生成 `state:{handle}`；`OperationTraceText` 增加与原枚举名一致的 Entered/Exited 静态文本。两域事实输出读取固定字符串，fact 类型、字段顺序、数值和发布时机不变。
- 每次状态 lifecycle 少两次临时字符串；首次装配增加定容数组。静态核对唯一 lifecycle 发布链、State operation 范围、枚举文本和差异；未编译、运行回放或采样。

### AP92 Gameplay Cue Trigger 事实 ToString（2026-09-29，已实施，本轮未编译）

- 两域 GameplayEffect projection 每个 Cue 变化都调用 `PortableCueTrigger.ToString()` 写入正式 Cue fact。trigger 是五值 byte 枚举，名称在合同中固定。
- 两域 RuntimeChanges 合同旁增加静态 trigger 文本映射，覆盖 OnActive、Executed、WhileActive、Removed、Expired 并保持原枚举名。Cue projection 读取同一字符串；fact 字段、producer、presentation 命令和异常传播不变。
- 每个 Cue fact 少一次枚举格式化分配。Catalog 文件当前有其它窗口改动，本次不触碰。静态核对枚举全集、两域唯一 projection 点和差异；未编译、运行回放或采样。

### AP93 Transition 请求身份参数数组和数字临时字符串（2026-09-29，已实施，本轮未编译）

- `TransitionRoutingRuntime` 每次生成新 inertialization 请求时，把 plan、owner、rule、两个 endpoint、selection/request generation 和 module generation 转成字符串数组交给 `StableHash.Compute`。其中 3 个 64 位代数每次分配十进制字符串，params 调用也建立临时数组；请求身份只在建立新请求时执行，不是每帧必然执行。
- `StableHash.BeginHash` 暴露同一 ThreadStatic canonical writer 的正式重置入口。Transition routing 按原参数顺序写入固定文本和已有 string identity，3 个 64 位数字用栈上 20 字符工作区按 InvariantCulture 格式化后写入原始 UTF-8；字段间仍写入 `0x1f`，最后复用同一 `ComputeHash`。
- 该修改保持原文本内容、UTF-8 编码、字段分隔、哈希算法和 `TransitionRequestEventId` 合同；空 identity 沿原有 `ToString` 结果编码。首次使用或 writer 扩容仍分配，最终 hash 对象仍由 canonical 链构造。静态核对唯一生成点、数值上界、字段顺序、写入方法和 workspace 生命周期；未编译、运行哈希对比、回放或采样。

### AP94 Session Snapshot 身份周期字符串化（2026-09-29，已实施，本轮未编译）

- Fixed 与 Float32 的 Session Snapshot 构造把 composition、world 和 pipeline 三个 StableHash 先转成 64 字符 string，再连同域文本交给 `StableHash.Compute`；params 调用还会建立字符串数组。该入口用于 checkpoint capture/read-back 和权威预测合并，构造频率由调用方哈希节奏决定，不宣称每帧必经。
- 两域 Snapshot Hash 改为复用 ThreadStatic canonical writer，按原顺序写入域文本和三个 hash `Value`，字段间保留 `0x1f`，最后调用同一 `ComputeHash`。域文本、字段顺序、UTF-8 内容、分隔符和返回的 `SnapshotHash` 合同不变。
- 每次 snapshot 身份计算去掉 params 数组和三次 64 字符临时字符串；首字段前不写分隔，后续三处字段间保留 `0x1f`。canonical writer 首次使用或扩容仍分配，快照 payload、checkpoint 对象和其余哈希所有权不变。静态核对两域唯一构造入口、StableHash 格式边界、checkpoint 消费链和差异；未编译、运行哈希对比、回放或采样。

### AP95 Skill 执行只读进入复制聚合名单（2026-09-29，已实施，本轮未编译）

- `GameplayAbilityExecutionManager.Enter` 在查找已有 frame 前总是调用 `EnsureMutableShell`，即使本次只读取默认值或最后原样退出，也会为共享 committed aggregate 新建 frame List；`Remove` 在目标不存在时也先建立 shell。
- Enter 改为先在共享 aggregate 上查找；只有需要新增 frame 时建立可变 shell。Remove 先确认目标存在，再建立 shell。已有 frame 的首个 `BindGeneration`、状态写入或 reset 仍由 active frame 所有权检查在当前 shell 内只克隆目标 frame；generation 未变的只读退出继续共享 committed aggregate，事务相等性判断仍得到未变更。
- 只读进入和 absent Remove 不再分配聚合名单；实际新增、移除或写 frame 时的 List/目标 frame 分配保留。查找、新增、移除、Savepoint、Commit 和 Discard 顺序不变。静态核对 Enter/Remove/Exit、MakeActiveFrameMutable、事务 Get/Set 和两域 ActionStateStore 调用链；未编译、运行回放或采样。

### AP96 Skill 执行 no-op 状态重复克隆与比较（2026-09-29，已实施，本轮未编译）

- `BindGeneration` 对相同 generation、`TrySet` 对已存在且相同的值、`TryReset` 对已存在且已经是默认值的槽位，都会先克隆 active frame 并写事务；事务随后再做一次聚合相等扫描才能发现没有变化。异常代数仍沿原 `BindGeneration` 合同失败。
- 三个入口在修改前比较正式值：相同 generation、已存在且相同的写入值、已存在且已是默认值的 reset 直接返回成功。frame 中尚不存在的槽位仍按原路径物化，因为 codec 会写入 frame Values 数量，省略默认项会改变状态字节和回放合同。只有真实代数推进或值变化才克隆当前 shell 内的目标 frame 并写 aggregate。类型、slot 合同和值校验边界保持；代数为零或非零不匹配仍沿原有 ArgumentOutOfRange/InvalidOperationException 边界失败。
- no-op 状态调用省去目标 frame 克隆、aggregate 写回和事务聚合相等扫描；真实变化路径与失败异常语义不变。静态核对 manager 值默认规则、两域 ActionStateStore 接口和 Transaction Set/Get 生命周期；未编译、运行回放或采样。

### AP97 Locomotion 周期重复解析 continuation 子树（2026-09-29，已实施，本轮未编译）

- Fixed 与 Float32 Timed Locomotion 每次解析 timeline 时调用 `TryFindSingleLocomotion`，对 completion transition 目标 state 新建 `Stack<OperationHandle>` 并遍历 Child 子树查找唯一 locomotion。state 拓扑、Root 边和 operation code/Integer 常量装配后固定，这是周期重复拓扑查找和托管分配。
- `OperationExecutionTopology` 构造期基于已有 Root 边和 Child 出边预解析每个 state 的唯一 LocomotionInputMotion；发现第二个则按原逻辑判定不唯一。Motion runtime 改为读取 `StateSingleLocomotion`，保留目标必须为 Continuous ConstantSpeed、Move Speed 类型和数值校验、owner identity 与 continuation velocity 语义。
- Timed Locomotion 每 tick 少一次 state 子树遍历和 Stack 分配；装配期新增定容 handle 数组。目标无 Root、无 locomotion、多 locomotion 或 continuation 不满足模式时结果不变。静态核对 Root/Child 出边构造、装配期 Stack 清理、两域调用和差异；未编译、运行回放或采样。

### AP98 Action Motion Curve 周期总量重复求值（2026-09-29，已实施，本轮未编译）

- Fixed 与 Float32 Action Motion Curve 在 looping 模式下，X/Z 的 from 和 to 各自调用 `SampleCumulative`，每次都重新执行 `curve.Evaluate(duration)` 计算周期总量；同一 tick、同一曲线的该值重复计算四次。周期总量只依赖曲线、duration 和求值器纯函数。
- 每条曲线在本次位移解析中先计算一次 `cycleTotal`，非 looping 仍按原 clamp 后求值，不预计算 endpoint。`SampleCumulative` 只接收并复用该总量，`cycle`、local time、from/to 和 local 求值顺序不变。
- looping 位移解析从每轴四次求值收敛为三次（周期总量加 from/to），两轴从八次收敛为六次；数值顺序、舍入输入和可见输出不变。静态核对两域曲线求值器无状态、唯一调用链和差异；未编译、运行回放或采样。

### AP99 State 执行路径身份周期解析（2026-09-29，已实施，本轮未编译）

- `OperationControlRuntime.FindStateExecutionPath` 每次 Push state scope 都读取 active/exiting identity slot，并用 `int.TryParse` 解析回 OperationHandle 后与当前 state 比较。identity slot 只写入 `OperationExecutionTopology.OperationIdentity` 预生成字符串或空串，解析结果不参与其它语义。
- 改为持有当前 state 的预生成身份并做 `StringComparison.Ordinal` 比较；命中后仍读取同一 execution path slot。空串、其它 operation 身份和无效身份都不命中；命中条件、路径读取和返回值不变。该方法内不再保留解析入口，状态机自身的过渡 handle 解析不在此项范围内。
- 每次激活、过渡评估、子图或 state 进入时少一次到两次固定数字字符串解析；无新增状态、缓存或第二执行路径。静态核对 identity slot 全部写入、Push scope 调用链和字符串比较边界；未编译、运行回放或采样。

### AP100 Character Input 配置与请求交叉扫描（2026-09-29，已实施，本轮未编译）

- Fixed 与 Float32 Character Input 的 `ApplyRequests` 原先对每个配置请求 ID 线性扫描整批 incoming requests，匹配同一 Tick 内多条请求和多个配置时形成乘积级字符串比较。配置 ID 已在构造期按 Ordinal 排序并拒绝重复。
- 先按原顺序重置每个配置 ID 的过期状态；随后对每条 incoming request 用排序配置表做 Ordinal 二分定位，只访问命中的配置 ID，并沿用原有 validity、Priority、Sequence 决胜和字段顺序。未命中配置的请求仍被忽略，未命中的配置仍保留过期重置结果。
- 请求应用从配置数乘请求数次比较收敛为配置数加请求数次对数定位访问；同一请求名单的最终 Priority/Sequence 状态不变。静态核对构造排序唯一性、空请求、过期重置、多条同 ID 请求和两域接口；未编译、运行回放或采样。

### AP101 Value 输入周期端口合同查找和字符串环检查（2026-09-29，已实施，本轮未编译）

- Fixed 与 Float32 Value Graph 每读取一条 Operation 输入，都调用 `ValueSourceOutputPort` 重新解析源操作端口合同并取出 identity；同一 identity 随后成为环检查键，`FixedValueEvaluationKey`／`Float32ValueEvaluationKey` 每次对它计算 Ordinal 哈希并做字符串相等比较。
- `CompiledValueInputBinding` 在装配期固化 `SourceOutputPortIdentity`。Value 边构造时已有 `sourcePort`，SubGraph 的 graph frame 输出按 ParameterName/PortId 排序后生成连续 Order，`ReadSubGraphOutput` 消费的同一 frame 输出列表也保持该顺序，因此 identity 和 `SourceOutputPortIndex` 可以随 binding 一起正式保存。
- 两域周期 `ReadInputs` 直接传入 binding identity 和 index，删除 `ValueSourceOutputPort` 周期合同查询；本地 binding 改为 `ref readonly`，不因新增 identity 字段复制更大结构。环检查工作区改为 `HashSet<long>`，用 operation handle 和端口 index 组成键，finally 移除键和容量准备不变。
- 每条 Operation Value 输入少一次静态端口合同解析、字典查找和字符串哈希；环检查键比较不再读取端口文本。诊断、SubGraph 状态读取、GameplayEffect 输出选择和异常文本仍使用同一 identity；实际 evaluation 输出、求值顺序和 Cycle 拒绝语义不变。静态核对 binding 唯一构造点、Graph frame/contract 顺序、两域 Evaluate 调用链、trace 和差异检查；未编译、运行回放或采样。

### AP102 GameplayEffect Freeze 重复判断 Tag 脏标（2026-09-29，已实施，本轮未编译）

- Fixed 与 Float32 `SimulationGameplayEffectState.Freeze` 的快速路径连续判断两次 `!m_TagsDirty`；该字段是同一个实例布尔值，第二次不提供额外约束。
- 删除重复判断，保留 attributes、active effects、periods、journal、change cursor 和 lifecycle revisions 的原判断顺序。全部未变时继续直接返回 baseline，任一变化时继续走 `CreateChangedFrom`，冻结对象和脏标语义不变。
- 每次 Freeze 快速路径少一次布尔判断。静态核对 `Freeze` 成功、恢复后提交和异常保存点调用链，以及两域差异检查；未编译、运行回放或采样。

### AP103 GameplayEffect Advance 重复扫描 Active 存在性（2026-09-29，已实施，本轮未编译）

- Fixed 与 Float32 共用的 GameplayEffect `Advance` 先把当前 active 复制到 scratch 快照，再对每个快照项调用 `FindActiveByHandle` 做全表扫描。快照建立后，本轮循环内只会移除当前项，Additional effect 全部入队并在快照循环后的 `FlushAdditional` 执行，因此快照项不会因其它项先失效而变脏。
- 删除每个 active 开始前的存在性扫描；`RemoveActive` 保留按 handle 查找的唯一存在边界，移除目标缺失时继续跳过。Removal、ongoing/inhibited、period、while-active、expiration 和异常回滚顺序不变。
- 每次 Advance 从 active 数量级全表扫描收敛为只在实际移除时检查一次；效果越多收益越大。静态核对两域 `AcquireActiveEffects` 快照所有权、`RemoveActive` 调用链、Additional 队列时机和差异检查；未编译、运行回放或采样。

### AP104 Equipment Action Route 周期线性扫描（2026-09-29，已实施，本轮未编译）

- Fixed 与 Float32 Equipment Runtime 的 `HasActionRoute` 每次线性扫描固定 Routes 名单；EquipmentProgramLayout 构造期已经用 `m_RouteById` 建立唯一 Route 索引。
- Layout 增加正式 `HasRoute`，有效 route 直接查询现有索引；两域 Runtime 在 capability 和 route 有效性检查后统一调用。未知 route、无效 route、capability disabled 和后续 `TryReadActionContext` 行为不变。
- Action event/context 解析中的 route 存在检查从 route 数量级字符串/结构比较收敛为一次字典查找；无新增缓存、fallback 或第二配置源。静态核对 layout 构造唯一索引、两域接口调用链和差异检查；未编译、运行回放或采样。

### AP105 Action Admission 父链重复解析（2026-09-29，已实施，本轮未编译）

- 两域共用的 Action Admission 在每次 tag query 匹配时，对每个 owned candidate 反复沿 GameplayTag Parent 链调用 catalog 查找；同一 Admission、同一批 owned/source tags 会随 Required/Block/Cancel 各段重复解析同一父链。
- 现在将 owned tag 和 active source tag 装入私有 HashSet 时一次性加入其全部祖先，保留 64 层上限、自身父节点错误和空白 tag 规则。查询匹配改为直接 `HashSet.Contains`；候选或父节点已存在时停止重复展开。
- Required/Block/Cancel 的匹配结果不变，包括父 tag 命中子 owned tag、None 阻断和跨 Required/Block 的祖先共享。静态核对两域 `TryGetGameplayTagParent`、profile Tags/queries 消费、私有集合生命周期和差异检查；未编译、运行回放或采样。

### AP106 Action Admission 父链跨评估重复解析（2026-09-29，已实施，本轮未编译）

- AP105 后每次 Admission 仍会对相同 owned/source tag 重新调用 GameplayTag catalog 解析父链；`ActionAdmissionControl` 与两域 Action Runtime 生命周期相同，而 Parent 关系来自装配后固定的 catalog。
- 为每个 tag 在 Control 实例内缓存从自身到根的 ancestor 数组；首次解析保留 64 层上限和自身父节点错误，后续装载路径直接展开缓存。owned/source HashSet 仍每次 Evaluate 清理，不跨评估保留业务标签。
- 相同 tag 的重复激活不再重复 catalog 查找和父链构造；配置更换沿正式 runtime 重建生命周期，不引入运行时 fallback。静态核对 Control 生命周期、catalog 只读合同、缓存字段唯一消费和差异检查；未编译、运行回放或采样。

### AP107 State Machine 身份周期字符串解析（2026-09-29，已实施，本轮未编译）

- `OperationStateMachineRuntime` 每次 Tick、继续过渡和停止状态机时，把 active/exiting/pending identity slot 的预生成数字字符串用 `int.TryParse` 解析回 `OperationHandle`；AP99 已收口 Push state scope 的解析，但状态机自身过渡 handle 解析仍是周期重复工作。
- `OperationExecutionTopology` 装配期已有 `m_Identities`，现在同时建立唯一的 identity 到 handle 的 Ordinal 字典；状态机读取 slot 后通过 `TryResolveOperation` 直接取得 handle，删除原 `ParseHandle`。identity slot 的正式写入仍只有拓扑数字身份或空串，过渡和停止状态不变。
- 每次状态机推进少一次整数字符串解析；未知或空 identity 继续按无效 handle 处理。静态核对 identity slot 全部写入点、Restore/Stop 读取链、两域和 Presentation 控制目标调用；未编译、运行回放或采样。

### AP108 Locomotion 曲线周期字典查找（2026-09-29，已实施，本轮未编译）

- Fixed 与 Float32 Locomotion 的 Action Motion Curve 每次 X/Z 位移解析都通过 `RequireTimelineCurve` 在 `Dictionary<int, Curve>` 中查找两次；曲线、constant index 和 Ability 执行数据在装配后固定。
- 两域执行曲线改为与 `data.Constants` 对齐的定容数组，装配期按原去重规则解码 Timeline Clip、Motion Curve 和 Action Motion 曲线；运行期用 constant index 直接定位，缺失 curve 仍按原异常失败。
- 每次 Action Motion Curve 解析少两次 hash 查找，曲线求值次数、AP98 的周期总量复用和输出不变。静态核对 `RequireTimelineCurve` 唯一周期消费、两域 BuildExecutionCurves 去重和差异检查；未编译、运行回放或采样。

### AP109 Locomotion 常量周期重复解析（2026-09-29，已实施，本轮未编译）

- Fixed 与 Float32 Locomotion 每次 Submit 都重新定位并校验 Turn/Move/Duration/Action Motion Curve 常量；同一 operation 的模式、数值和曲线来自装配后固定 layout，重复校验不产生新业务约束。
- 两域 Locomotion Runtime 构造期按原检查顺序编译 per-operation profile，保存执行模式、位移模式、Turn/Move 速度、曲线时长、Timed duration ticks 和已解码 X/Z 曲线。周期 Submit、位移解析和 timeline duration 直接读取 profile，删除重复 FindConstant 与逐帧校验。
- 周期路径少 3 到 6 次常量定位和全部重复类型/范围校验；无效配置仍在 runtime 构造期以原异常语义失败，输出公式和 AP98 曲线求值不变。静态核对两域常量字段、原校验顺序、Timed 与 Continuous 分支和差异检查；未编译、运行回放或采样。

### AP110 Equipment Begin 重复冻结 aggregate（2026-09-29，已实施，本轮未编译）

- 两域 Equipment 每次 BeginEvaluation 先调用 InitializeContributions 读取并冻结完整 Equipment aggregate，再调用 CancelOrphanedPending 再次读取；两次调用之间没有其它状态写入。EndEvaluation 保留 orphan pending 检查。
- EquipmentRuntimeControl 新增 PrepareEvaluation，一次读取 aggregate 后判断并安装缺失 contribution；mutation 提交后的本地 aggregate 已包含写入结果，继续用同一 aggregate 判断并取消 orphan pending。原 mutation try/catch、WriteState、effect 提交和 lifecycle 顺序不变。
- 每个 Equipment enabled Ability evaluation 少一次完整 aggregate 冻结和 slots 复制；contribution 缺失与 pending 取消同帧时的最终状态不变。静态核对两域 Begin/End 调用链、mutation 异常路径和 aggregate WithSlot/ResolvePending 所有权；未编译、运行回放或采样。

### AP111 Equipment Slot 状态线性扫描（2026-09-29，已实施，本轮未编译）

- EquipmentStateAggregate 构造和 AdoptPrepared 都把 Slots 按 SlotId Ordinal 排序并拒绝重复，但 RequireSlot、WithSlot 和 WithSlotAndResolvedPending 每次从头线性比较；Equipment operation 和 mutation 会重复访问同一 aggregate。
- 增加 FindSlot 在既有排序合同上做 Ordinal 二分定位；Require/替换/同 slot 提交使用同一索引，未命中异常、no-op 返回、数组复制和 pending 解析语义不变。
- Slot 查找从 Slot 数量级收敛为对数比较；无新集合和第二数据源。静态核对 aggregate 构造排序唯一性、三条消费路径、异常文本和两域调用；未编译、运行回放或采样。

### AP112 Equipment LocalState 线性扫描（2026-09-29，已实施，本轮未编译）

- EquipmentStateAggregate 的 LocalStates 构造期按 FeatureId 加 StateId 的 joined identity 排序并拒绝重复，但 RequireLocalState 和 WithLocalState 每次线性比较；Equipment 换装时会为同一 Feature 的多个 state 重复进入。
- 增加 FindLocalState，用原 CompareJoinedIdentity 对既有排序做二分定位；Require、no-op 返回、value kind 检查、数组复制和 aggregate 替换语义不变。
- LocalState 查找从 state 数量级收敛为对数比较；比较仍使用原无分配 joined 合同，无新字典。静态核对构造排序、两条消费路径、异常和两域换装链；未编译、运行回放或采样。

### AP113 GameplayEffect Active 身份线性查找（2026-09-29，已实施，本轮未编译）

- 两域 GameplayEffectState 用同一个 List 保存 active effects，FindActiveByHandle、FindActiveByInstance、AddActive 去重和 attribute modifier 校验都反复全表扫描；active handle 与 instance 是正式唯一身份。
- 为每个 state 实例增加 handle 和 instance 两个生命周期字典，Add/Remove 同步维护；Restore 替换 active list 后重建索引，ClearCollections 一并清理。查找改为字典访问，Add 去重不再两次查找。
- Active 查找和 Advance 内的 modifier/period 校验从 effect 数量级收敛为一次 hash 查找；排序输出、Remove 语义、Restore/Validate 闭环和重复身份异常不变。静态核对两域 list 唯一写入点、Restore/Clear/Add/Remove、modifier 和 period 消费；未编译、运行回放或采样。

### AP114 Timeline MotionWarp ability 交叉扫描（2026-09-29，已实施，本轮未编译）

- 两域 Character Evaluation 复制每条 pending Timeline MotionWarp 后，先遍历全部 Ability invocation，再对每条 warp 比较 `AbilityId`，形成 invocation 数乘 warp 数的周期扫描。pending warp 由同一 actor 的 Timeline active playback 生成，其 AbilityId 来自启动该 playback 的 Action context；Timeline operation 又只在该 Ability invocation 执行时用当前 Action context 启动。Actor binding 构造期为每个已安装 Ability 建立唯一 invocation。
- 两域 binding 新增构造期 `AbilityId -> invocation` 字典。周期复制后按 warp 的 `AbilityId` 直接取得 invocation 并加入 workspace；未知 identity 沿字典失败，不添加静默 fallback。`AddTimelineMotionWarp` 只保留 evaluation 生命周期边界，删除调用方已由同一构造期安装表保证的重复 ability 比较。
- 每个 pending warp 从 invocation 数量级比较收敛为一次 Ordinal identity 哈希查找；Copy、Add 和应用顺序不变。静态核对 Timeline 启动、active playback provenance、pending 复制、唯一安装表和两域差异；未编译、运行回放或采样。

### AP115 Action lifecycle owner 交叉扫描（2026-09-29，已实施，本轮未编译）

- 两域 Character Evaluation 对每个 Ability invocation 处理 ActionLifecycle ingress 时，都调用 `OwnsAction` 线性扫描 source state 的全部 ActionInstances，形成 invocation 数、ingress 数和 action 数的周期乘积。ActionInstanceId 由 Character Handle Allocator 生成唯一非零句柄，source state 的 ActionInstances 是正式 owner 名单。
- 两域 binding 新增按正式 action capacity 预留的生命周期字典；每次 Evaluate 先用 source ActionInstances 重建 `InstanceId -> invocation`，周期分发用一次 ulong 查找定位 owner，未知 instance 继续忽略。删除重复线性 `OwnsAction`，用 invocation 引用相等判定目标。
- owner 重建从 action 数量级执行一次；ingress 分发从每次全 action 扫描收敛为一次哈希查找。ingress 顺序、未知行为、ability 过滤和 ApplyActionIngress 时序不变。静态核对 allocator 唯一性、两域调用链、字典生命周期和差异检查；未编译、运行回放或采样。

### AP116 Action Window ability 线性查找（2026-09-29，已实施，本轮未编译）

- 两域 Character Runtime Ports 把窗口查询委托回 Character Evaluation，每次都按 skill 线性遍历全部 invocation；Control module 的连招、恢复和退出窗口检查会重复这项定位。AP114 已建立构造期 `AbilityId -> invocation` 唯一索引。
- 两域 binding 增加正式窗口查询：未知 skill 继续返回 false，命中 skill 直接定位 invocation 并读取 projection；两域 Runtime Ports 改为绑定该方法，删除 Character Evaluation 的重复线性辅助。projection 扫描和 evaluation 生命周期边界保持。
- 每次窗口查询从 invocation 数量级比较收敛为一次 Ordinal identity 哈希查找；可见输出、未知 skill 行为和查询时序不变。静态核对 Control delegate 合同、窗口 projection 唯一消费、两域 Runtime 构造和差异检查；未编译、运行回放或采样。

## 可靠性问题独立保留

- 2026-09-29 GameplayAbilityExecution 写时复制别名：`CreateMutableShell` 只复制 frame 名单，`MakeActiveFrameMutable` 在脱离共享后没有继续克隆 active frame；现有 frame 的 `BindGeneration`、`TrySet` 和 `TryReset` 会写穿到 committed aggregate 持有的同一对象。事务随后用聚合相等性判断变更时可能得到 false，破坏 Savepoint/Commit/Discard 语义。现在 manager 跟踪 active frame 所有权，脱离共享后首个写入只在当前 shell 内克隆目标 frame；新增 frame 与移除路径维持原语义。静态核对两域 ActionStateStore、事务 Get/Set、聚合相等性和生命周期调用链；未编译、运行回放或采样。

- 2026-09-29 DotRecast ActorContactSolver：三参数 ValidateFinal 本应将独立诊断列表传给四参数验证实现，却调用了自身；Resolve 也进入该递归入口。现在 Resolve 将当前有效位置切片交给四参数实现并使用 m_ResolveTraces，外部重约束验证使用 m_ValidationTraces。两条诊断记录仍分别归属原结果；按有效数量传入位置，避免工作区曾扩容后将容量误作名单长度。静态可确认原调用自递归及新调用落到现有成对验证实现，但尚未编译或运行；该项是正确性修复，独立于装箱优化。

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

## 2026-09-29 续查：Span Capture 39KB/帧静态复核

本轮基于 capture.20260929-134336.3c5acd66f8e340e78cb73390ef29c09e（Player 7eda33a33be80142a355f59d，Span 模式，instrumentation_identity 4c905aef...）。该 Player 已包含 AP01–AP116 全部已实施改动。仅做静态检查和原始采集数据解析，未启动 Unity、编译或采样。

### 已确认数据

| 指标 | 结果 |
| --- | ---: |
| GC 总量 | 332,888,717 B |
| GC 均值/帧 | 39,172.6 B |
| GC 中位数/帧 | 2,698 B |
| GC P75 / P95 / P99 | 46,829 / 124,207 / 199,744 B |
| 零分配帧 | 0 / 8498 |
| 分配 >20KB 帧 | 2536 帧（29.9%），贡献约 224.8 MB |
| 最大帧 | 16.65 MB × 4 帧（render_frame 2088、4980、5803、6353），合计 66.5 MB |

写屏障 GC_end_stubborn_change 占 1819/83074 = 2.19% CPU 样本。实际分配入口（Object::New、Array::NewSpecific、GC_malloc_kind、GC_gcj_malloc、GC_allocobj）合计约 0.08%。GC 相关 CPU 主要来自引用字段写入的 dirty 标记，不是分配调用本身。

### 本轮逐路径静态排查结果

以下路径已逐文件读源码并确认当前无周期性托管分配：

| 路径 | 结论 |
| --- | --- |
| CharacterPoseNativeGraphRuntime m_OutputCache | 已用 Reuse 模式，PortValue 对象逐 handler 复用；每帧仅 Dictionary.Add 写已有实例引用 |
| CharacterPoseNativePortValues.cs 全部 8 种 PortValue | sealed class，静态 Reuse 方法刷新字段，new 仅首次 handler 调用触发 |
| CharacterPoseNativeGraphEvaluator PrepareFrame/EvaluateFrame | 字段集合 m_PrepareOrder/m_ActiveHandlers/m_SourceRequests 构造期持有，帧内 Clear/Add |
| CharacterPoseNativeFrameCoordinator.RunFrame | 返回 readonly struct CharacterPoseNativePublicationResult |
| CharacterPoseNativeEvaluationResult | readonly struct |
| TimelineRuntimePresentationEvaluator | TimelineRuntimeEvaluationSegments/Operations/ScenePresentationSample 均 readonly struct |
| TimelineRuntimePlaybackSnapshot | 仅 Capture 调用（快照入口，非逐帧） |
| TimelineRuntimeService.Prepare 路径 timeline:{...} | 创建路径，非逐帧 |
| AnimationSlotBlendJob | 所有 new 为 struct 写入预分配数组，无托管分配 |
| AnimationBlendStackRuntime.BuildBoneAlphaLayout | 仅构造期调用（line 460） |
| FootPlacement 全部 Result 类型 | readonly struct（HardConstraintResult、LandingQueryResult、InterpolationResult、ResolvedFootResult 等） |
| ACL SourceGraph/CaptureJob | NativeArray + struct job，无 managed alloc |
| CharacterPoseNativeDomainInstance/FrameCoordinator | 委托包装，无帧内 new |

异常路径字符串拼接（CharacterPoseNativeGraphRuntime.cs 等）仅在 catch 块触发，不构成周期分配。

### 剩余边界

39 KB/帧 均值来源分散，静态分析无法进一步归因到具体单行代码。剩余因素包括：

1. **写屏障压力**：m_OutputCache.Add(key, value) 每帧为每个节点每个端口写一次 class 引用到 Dictionary。Dictionary 内部 bucket 和 PortValue 对象都在老生代，触发 Boehm GC 的 stubborn change 检查。改为定容数组索引可消除哈希查找成本，但写引用到数组同样触发写屏障，写屏障本身不因替换容器消除。
2. **Unity 引擎内部分配**：SkinnedMesh 更新、渲染管线、开发构建 Profiler 采样自身均可能在 GC 堆上分配。不在本仓库源码控制范围内。
3. **IL2CPP 运行时**：泛型共享、接口调用等有元数据/委托开销，但不是代码级可修的周期分配。
4. **分配调用栈缺口**：当前 unity-profiler.raw 未解析为分配调用栈，无法把每帧 39 KB 对齐到具体函数。需要 Editor 内开 Allocation Call Stacks 重新采集后才能继续归因。

### 2026-09-29 仿真链与 Camera/EventGraph 续查

以下路径已逐文件读源码并确认当前无周期性托管分配：

| 路径 | 结论 |
| --- | --- |
| CameraDomainRuntime 逐帧路径 | ActiveCameraRequest/CameraSequenceRequest/CameraEffectRequest/CameraResponseRequest/CameraTargetSelectionRequest/CameraFrameInput 均 readonly struct，写入 field-held List |
| NativeEventGraphRuntime Execute | 正常路径无 new 引用类型；EventGraphExecutionFailure 仅异常路径 |

### 仿真 Pipeline 合同周期分配

每次 Fixed Pipeline Transaction Execute 的托管分配链（Fixed 域）：

1. FixedCharacterEvaluationRuntime.Evaluate 返回 `new FixedCharacterEvaluationResult(...)`（sealed class），内含 `facts.ToArray()`、`presentation.ToArray()`、`trace.ToArray()`、`timelineAdvances.ToArray()`、`timelineStops.ToArray()` 共 5 个数组。
2. FixedPipelineTransaction.CompleteStep 构造 `new SimulationActorTickResult[finalizedCount]`（class 数组）和 `new SimulationActorState[...]`（struct 数组）。
3. `SimulationActorTickResult.FromOwnedOutputs` 每角色 `new SimulationActorTickResult(...)`（sealed class），持有上述评估结果中的 3 个数组。
4. `SimulationTickResult.FromSortedOwnedActors` 每次 `new SimulationTickResult(...)`（sealed class）。

2 角色每 Tick 约 17 个托管分配（2 评估结果 + 10 数组 + 2 TickResult + 3 外层）。这些是管线不可变结果所有权合同的一部分：评估内部工作 List 每帧复用，结果必须拷贝到独立数组供下游只读消费，否则复用会覆盖已发布数据。消除此分配需要改为池化+消费后归还的生命周期管理，涉及 Commit/Discard 语义变更，需要业务决策后实施。

### 结论

业务代码层面，逐帧/逐 Tick 路径已无可静态消除的托管分配。剩余 39 KB/帧 均值中：
- 写屏障来自 PoseGraph 输出缓存 Dictionary.Add 写 class 引用（不可通过替换容器消除）。
- 仿真合同分配约 17 个/ Tick，需要所有权模型变更。
- Unity 引擎内部和 IL2CPP 运行时分配不在本仓库控制范围内。
- 具体函数归因需要 Allocation Call Stacks 采集。

### AP117 PoseGraph 输出缓存定容数组（2026-09-29，已实施，本轮未编译）

- CharacterPoseNativeGraphRuntime 的 m_OutputCache 原为 Dictionary<CharacterPoseNativePortKey, CharacterPoseNativePortValue>，每次 Read 输出端口时按 (NodeId, PortId, Stage) 三元组哈希查找，每帧 Clear 后逐端口 Add class 引用。
- 改为 m_OutputSlotIndices（Dictionary<(PoseNodeId, string), int>，Initialize 时按输出端口注册顺序分配连续索引）和 m_OutputCacheSlots（CharacterPoseNativePortValue[]，容量 = outputPortCount * 2）。Read 路径从 slot 字典取索引后按 stage 偏移直接数组读写，消除 PortKey 哈希计算和 Dictionary bucket 遍历。
- 4 处 Clear 统一走 ClearOutputCache() 内 Array.Clear。PoseNodeId 实现 IEquatable，string 用 Ordinal 比较，tuple key 语义与既有 m_InputPorts 一致。
- 减少的是逐端口哈希查找成本；写引用到数组仍触发 Boehm GC 写屏障，不因容器替换消除。静态核对端口注册唯一性、stage 枚举范围（Prepare=2/Evaluate=3 偏移 0/1）和 Clear 调用点语义；未编译。

### 补充排查确认（2026-09-29 第二轮）

| 路径 | 结论 |
| --- | --- |
| AnimationBlendStackRuntime BeginFrame/Advance/PrepareSlotJob | 字段预分配数组和 struct 操作，无托管分配 |
| TimelineRuntimeStepCoordinator | 无周期 new 引用类型 |
| TimelineRuntimeComposition.PresentationPlaybackState | 构造期分配，非逐帧 |
| CharacterFinalPoseNativePublication / PropertyWriter | 构造期数组，帧路径无 new |
| m_GraphInputs Dictionary | graph input 端口数量远小于 output，保留 Dictionary（收益不够大，不为此增加映射结构） |

### 独立缺陷：EventIdBuilder.Transform 栈溢出越界（2026-09-29，偶发）

- Capture capture.20260929-230200（Player 7eda33a33be80142a355f59d，Span 模式）中 Player transport 在 800 帧后断开。
- Player log 中 `EventIdBuilder.Transform()` 抛出 `IndexOutOfRangeException`，沿 `WriteByte` → `WriteText` → `Append` → `TimelineToActionCommandBridge.CreateEventId` 调用。
- `EventIdBuilder` 是 `ref struct` SHA-256 实现，`m_Block` 固定 `Span<byte>` 64 字节。`Transform` 内 `stackalloc uint[64]`（256B）和 `WriteText` 内 `stackalloc byte[768]` 在调用链深度较大时合计超过 1KB 栈分配。WPR 采样增加 CPU 压力，偶发触发栈溢出破坏 `m_Block` 指针。
- 同一 Player 后续 Capture capture.20260929-230500 正常完成（8611 帧/2536 tick/0 丢弃），确认这是偶发条件。
- 该 bug 与托管分配无关，属于 `ref struct` 栈分配在高压下的可靠性缺陷。修复方向：将 `WriteText` 和 `Transform` 的 stackalloc 改为 `stackalloc` 上限或预分配字段，或限制递归深度。需业务决策后实施。

### 2026-09-29 Capture capture.20260929-230500 结果（与基线同构建身份）

| 指标 | 值 |
| --- | ---: |
| FPS | 203.85 |
| Tick/s | 60.04 |
| 丢弃 Tick | 0 |
| GC 均值/帧 | 38,671 B |
| GC P95 | 124,199 B |
| Main Thread 均值 | 4.88 ms |
| Main Thread P95 | 6.52 ms |

该 Player 未包含 AP117 PoseGraph 输出缓存改动（改动在工作区但未构建）。FPS 和 GC 与基线差异属运行波动范围。

### AP118 表现域 Actor 上下文探针补齐（2026-09-29，已实施，本轮未编译）

- `CharacterPresentationDomainRuntime.PresentationFrame` 在方法体开头调用 `PerformanceInstrumentationContextRuntime.BeginActor(m_ActorId.Value, 0, 0)`，在 finally 块的清理路径末尾调用 `EndActor()`。
- 此前 `BeginActor` 在整个仓库无调用者，导致所有 Span 采集的 `actorId` 均为 0（global），无法在采集数据中按角色拆分 PoseGraph Evaluate、Foot Placement 等表现阶段耗时。
- 加上后，`PresentationFrame` 内的所有嵌套探针（`pose-graph.evaluate`、`pose-graph.prepare`、`foot-placement`、`full-body-ik` 等）会继承 actor 上下文，Span 记录的 `actorId` 字段将携带真实 ActorId。
- `presentation.animation` 外层探针仍在 `BeginActor` 之前由织入器捕获上下文，外层保持 global；拆分在子探针级别生效。
- 静态核对 `EndActor` 在 finally 块中位于所有资源清理之后，确保异常路径也能正确弹出上下文。未编译。

## 2026-09-29 补充采集与 Foot 路由续查

按用户要求重新采集：`capture.20260929-164123.11c54cf735404c29b63af6a4fe309738`，Player `c0dd70576c417e0e223ec658`，Span 模式，固定输入 2536 Tick，耗时约 42.246 秒。该 Player 包含 AP01–AP116 及 AP117 的当时工作区输入，不包含 AP118 和本节 AP119。结果为 FPS 约 151.54、Tick/s 约 60.03、0 丢弃 Tick、Main Thread 均值约 6.57 ms、GC 均值约 50.1 KiB/帧。表现事务均值约 3.74 ms/帧，Pose Evaluate 均值约 1.90 ms/帧，Foot Placement 均值约 0.35 ms/帧；逻辑 Tick 均值约 1.67 ms，KCC 约 0.30 ms。单个 Tick 322 的 Character Evaluate 慢样本约 44.66 ms，其中 Ability Tick 约 36.28 ms，需要按原始 Span 定位到具体业务调用后再改。

### AP119 Foot 生物力学路由只求运行时消费点（2026-09-29，已实施，本轮未编译）

- 新采集的解析热点包含 `AnimationFootBiomechanicalStepCurveSet.Sample`（约 1200 个包含样本）。源码核对发现运行链把 25 个路由点先构造进 `FixedList4096Bytes<AnimationFootBiomechanicalRouteSample>`，随后只按事件相位取相邻两点并调用 `Interpolate` 得到一个当前样本；其余 23 个点不进入运行时输出。
- `AnimationFootBiomechanicalStepCurveSet` 新增 `SampleCurrent`：保留 landing phase、opposing rotation、索引计算和两点构造的同一公式，只求相邻两点后复用原 `Interpolate`。`AnimationPredictedFootStepCurveSet.SamplePrepared` 改走该方法，删除临时 25 点列表和二次索引。
- 整条 25 点路由的 `Sample` 保留，编辑器 Foot 重建校验仍用它核对每个路由点；这不是兼容旁路。预测步根骨骼、踝、髋、planar route 和 clearance 仍由正式 `AnimationBiomechanicalRoutePage` 承载，Blend 校验、Motion Matching 编解码和 Bind 复制继续消费这些完整页。
- 静态核对唯一运行时调用点、编辑器整条路由校验、样本构造公式和插值边界；未编译、未重新构建 Player、未重新采样，不能声称实测耗时或分配收益。

### AP120 Action Instance 生命周期索引（2026-09-30，已实施，本轮未编译）

- Tick 322 的原始 Span 树显示整 Tick 约 59.77 ms、Character Evaluate 约 44.66 ms、Ability Tick 聚合约 36.28 ms；`ability-tick` 下没有更深业务探针，因此沿源码继续排查。`OperationExecutionTopology` 的状态槽查找已经按 operation/semantic 定容索引，不是周期扫描来源。
- 两域 `ActionStateStore` 的 `TryGetInstance`、`TryFindInstanceIndex` 和 `ContainsInstance` 原来都线性扫描全部 Action instance；这些入口服务技能执行栈、action lifecycle 和写入替换，属于运行链重复拓扑查找。
- Fixed/Float32 `CharacterActionRuntimeState` 现在各自持有 `InstanceId -> index` 定容字典。`AddActionInstance`、`ReplaceActionInstanceAt` 同步维护；`Restart`/`Restore` 整表替换后重建并在重建时拒绝重复正式身份。索引是 Action state 的正式读取结构，不是 Store 侧缓存或兼容旁路。
- 两域 Store 的三条实例查找改为一次字典访问；`RequireActive`、`RequireActiveTransient`、`RequireActive(FixedActionInstanceReference)` 等调用沿用原状态检查和语义。按 Context/Skill/请求匹配的扫描保留，因为它们不是唯一身份查找。静态核对两域接口唯一实现、状态事务克隆、Restore 和 Store 写入链；未编译、未采样。

### AP121 Pose 约束结果重复校验收敛（2026-09-30，已实施，本轮静态修复重载缺陷）

- capture.20260929-164123 的已解析 CPU 热点中，`CharacterPoseConstraintMath.TryCreateComponent` 约 0.432%、`AnimationLocalBonePose` 构造约 0.251%、`AnimationLocalBonePose.get_IsValid` 约 0.359%。这些函数位于 Pose 空间转换、Modify Bone 重建和虚拟骨骼派生链。
- `TryCreateComponent` 先检查组合结果的 position、rotation、scale finite 和四元数非零，随后公共 `CharacterComponentBonePose` 构造器再次执行同一组检查。现在在原检查点完成校验后直接归一化，并写入最终页；公共构造器继续保留外部输入校验和归一化合同。静态复查发现原先按同签名构造器表达内部入口会造成 C# 重载冲突，已改为 `CreateNormalized` 静态工厂并修正调用点。
- `TryCreateLocal` 的 `AnimationLocalBonePose` 构造器会在结果非法时抛出异常，成功后原代码又调用 `local.IsValid` 重复执行同一 finite/非零检查。现在构造成功即返回 `true`，调用方的异常路径和输出局部姿态不变。
- 该改动减少 PoseGraph 每骨骼转换链中的重复浮点检查和二次 `IsValid` 调用；不删除归一化，不改变无效输入失败语义、输出页身份或 Commit/Discard 时序。静态核对 NativeArray 转换、Modify Bone 派生和虚拟骨骼调用链；未编译、未采样，不能声称实测耗时收益。

### AP122 Q32.32 除法位循环收敛（2026-09-30，已实施，本轮未编译）

- capture.20260929-164123 的已解析 CPU 热点中，`FixedScalar.DivideScaled` 约 0.391%。该私有函数同时服务 `FixedScalar.operator /` 和 `FromRatio`，当前每次调用从 bit 95 到 bit 0 做 96 次长除法循环。
- 新实现先用一次 64/64 整除和取余得到商的整数部分，再只对 32 个小数位执行原长除法；128 位余数用高低两个 `ulong` 承载。这样避免对已确定的整数商逐位试减，同时保持最终商和余数与原 96 位算法一致。
- 商整数部分超过 `uint.MaxValue` 时在移位前按原 Q32.32 溢出语义失败；后续仍沿用 `FromRoundedMagnitude` 的银行家舍入、最小值、符号和溢出检查。零除数仍由 `operator /` 和 `FromRatio` 的现有边界拒绝。
- 该改动减少逻辑链高频定点除法的循环次数，不引入新集合、托管分配或第二数值路径。静态核对两个私有调用点、绝对值转换、符号计算和舍入合同；未编译、未执行数值对比或采样，不能声称实测耗时收益。

### AP123 源姿态采集重复校验收敛（2026-09-30，已实施，本轮未编译）

- capture.20260929-164123 的已解析 CPU 热点包含 `Quaternion.get_normalized` 约 0.597%、`AnimationLocalBonePose.get_IsValid` 约 0.359% 和本地骨骼姿态构造约 0.251%。`AnimationSourcePoseCaptureJob.ProcessAnimation` 对每个源骨骼先手动检查 position/rotation/scale finite 与四元数非零，再进入本地姿态构造器重复检查，最后又调用 `pose.IsValid` 重复检查。
- `AnimationLocalBonePose` 增加仅内部使用的已归一化构造器，构造器参数顺序明确 `normalizedRotation -> position -> scale`。源姿态采集在外层完成一次业务边界校验后调用该构造器，保留必要归一化，删除公共构造器和循环尾部的重复 finite/非零检查。
- 排除源根骨骼时改为只校验正式 reference pose；普通源骨骼路径外层校验后直接写入当前姿态页。失败状态仍写入 `PhysicalPoseInvalid` 并终止本次源采集，当前姿态、速度差分和虚拟骨骼派生的消费时序不变。
- 该改动减少每骨骼每次采集的重复浮点检查，不新增分配、缓存或第二姿态数据源。静态核对 Animancer 源捕获 Job、reference pose 路径和本地姿态消费链；未编译、未采样，不能声称实测耗时收益。

### AP124 加权姿态构造重复校验收敛（2026-09-30，已实施，本轮未编译）

- `AnimationPoseMath.BlendWeighted` 和 `AnimationSlotBlendJobMath.TryResolveWeightedPose` 先执行退化旋转、finite 值或 magnitude 检查，随后仍进入 `AnimationLocalBonePose` 公共构造器重复 finite、四元数非零检查并再次归一化。这些路径服务 Slot Blend 与 PoseGraph 加权姿态输出。
- 两处现在在既有边界完成有限值检查后显式归一化一次，再使用 AP123 的内部已归一化构造器。`BlendWeighted` 保持退化旋转 `InvalidOperationException` 和非法值 `ArgumentException`；`TryResolveWeightedPose` 对除法后 rotation 可能溢出的极端路径保持与原公共构造器相同的 `ArgumentException`。
- 加权姿态的位置、缩放、旋转数值、失败类型和输出页消费时序不变；不新增分配、缓存或第二合成路径。静态核对两处唯一调用链和 `AnimationLocalBonePose` 内部构造合同；未编译、未采样，不能声称实测耗时收益。

### AP125 Evaluated Action 身份索引（2026-09-30，已实施，本轮未编译）

- Fixed 与 Float32 `ActionStateStore` 的 `m_EvaluatedActions` 按 `InstanceId` 保存 Timeline motion 仍需消费的动作快照；`RetainEvaluatedAction` 在每次 `EnterSkillExecution` 和 `BindSkillExecution` 后线性查找替换点，`TryGetEvaluatedInstance` 再线性查找读取。
- 两域 Store 增加构造期 `InstanceId -> list index` 字典，保留、读取和评估结束清理同步维护列表与索引。正式 InstanceId 唯一，替换原位置，新增写入列表尾部索引；`EndEvaluation` 同时清空列表和索引。
- Timeline motion 的延迟读取从 evaluated action 数量级扫描收敛为一次 ulong 哈希查找，且不新增周期分配或第二动作数据源。静态核对两域保留、读取、清理和 Timeline 消费调用链；未编译、未采样，不能声称实测耗时收益。

### AP126 FullBodyIK effector 正式索引（2026-09-30，已实施，本轮未编译）

- capture.20260929-164123 中 FBBIK `SetComponentRotation` 约 0.240% CPU 样本。FullBodyIK 每处理一个 goal 都执行 `CharacterFullBodyIkEffectorSlot -> FullBodyBipedEffector` switch，再调用 FinalIK 的 `GetEffector` 二次 switch。这些映射由 rig 和 `SetToIndexedReferences` 一次建立，运行帧内不变化。
- `CharacterFinalIkFullBodySolver` 在 `Prepare` 建立 references 后，把 9 个正式 effector 引用按业务 slot 写入定容数组；goal 应用、pre-solve 旋转、identity 判定、求解校验和诊断读取改为一次 slot 数组访问。`Prepare` 失败仍保持 `m_Prepared=false`，已缓存引用不会进入求解路径。
- 该改动减少 goal 处理中的重复枚举映射和 vendor 查找，不改变 goal 验证、应用顺序、输出页、Commit/Discard 或诊断内容。静态核对 FinalIK `GetEffector` 返回稳定引用和 5 个运行时读取点；未编译、未采样，不能声称实测耗时收益。

### AP127 FullBodyIK 旋转写入重复校验收敛（2026-09-30，已实施，本轮未编译）

- 同一份热点中 `SetComponentRotation` 的 exclusive 成本来自每次足部 pre-solve 旋转写入。入口 `RequireFinite` 已完成 finite 和非零检查，随后主骨骼 `AnimationLocalBonePose` 公共构造器重复同一检查并再次归一化。
- 主骨骼现在在既有写入边界归一化一次，并使用 AP123 的内部已归一化构造器；子孙骨骼的位置差、旋转合成和公共构造行为保持不变。绑定入口继续验证整页姿态，旋转入口继续拒绝无效参数。
- 该改动减少每次旋转写入的主骨骼重复浮点检查，不改变有效输入输出、无效输入失败类型、子孙传播顺序或输出页身份。静态核对 TryBind、位置/旋转写入和 FBBIK 调用链；未编译、未采样，不能声称实测耗时收益。

### AP128 虚拟骨骼合成重复校验收敛（2026-09-30，已实施，本轮未编译）

- source capture 和 FBBIK 输出重建都会派生虚拟骨骼。source capture 已在外层拒绝无效 source pose，FBBIK `TryBind` 已验证整页 component pose；随后 `CreateVirtualComponent` 又用公共构造器对 target position/rotation 和 source scale 重复 finite/非零检查并重复归一化 target rotation，输出 `AnimationLocalBonePose` 再次检查和归一化。
- 虚拟组件合成的两个重载现在只接受正式 local/component 页并使用 `CreateNormalized`；派生前已按原合同检查 local position/rotation finite 和非零，输出 local pose 改用已归一化构造器。FBBIK 不再把已验证 local pose 复制成 component pose 后再合成。
- 该改动减少每虚拟骨骼每帧的重复浮点检查和归一化，保持虚拟骨骼位置、旋转、缩放、失败码和输出页写入顺序不变。静态核对 source capture、rig 构造、Modify Bone 和 FBBIK 的页面有效性边界；未编译、未采样，不能声称实测耗时收益。

### AP129 PoseGraph cycle 与观测定容索引（2026-09-30，已实施，本轮未编译）

- AP117 已为输出值建立 `(node, port) -> slot` 构造期索引，但 cycle detection 仍构造 `(node, port, stage)` 哈希键写入 `HashSet`，诊断观测也维护两个同键 Dictionary；capture.20260929-164123 仍记录 `CharacterPoseNativePortKey.Equals` 热点。
- cycle 标记改为与输出缓存同长的 bool slot 数组，Prepare/Evaluate 观测改为定容 observation 数组并在 Commit 时原位交换。读取 miss、求值异常、TryObserve、Clear/Discard/Reset/Dispose 的记录和清理语义保持；输出 slot 索引继续是唯一正式端口身份。
- 正常求值不再构造或哈希 `CharacterPoseNativePortKey`，该类型唯一使用点已删除。观测内容、fault 记录、Evaluate/Prepare 回退、InstanceId/CompletionIdentity 检查、输出值缓存和阶段边界不变。静态核对 BuildPortDefinitions、Read、TryObserve、Commit/Discard 和条件编译范围；未编译、未采样，不能声称实测耗时收益。

### AP130 骨骼父子拓扑 handler 索引（2026-09-30，已实施，本轮未编译）

- capture.20260929-164123 中 `CharacterAnimationRigPayload.GetPoseParentIndex` 约 0.056% exclusive、238 个包含样本；该入口每次检查索引并按 physical/virtual 分支访问 payload 列表。Modify Bone 的目标、每个子孙保存与重建，以及空间转换的每骨骼 local/component 变换都在运行帧重复查询不变拓扑。
- Modify Bone 在 Initialize 解析目标父索引后，把子孙父索引与 descendant 数组同序缓存；空间转换在构造期缓存完整 PoseBone parent 数组。rig payload 构造后不可变，这些数组是 handler 对正式 rig 拓扑的定容读取结构，不是第二数据源。
- 运行帧的父子解析收敛为数组访问；节点绑定、拓扑判定、变换公式、失败文本和输出写入顺序不变。静态核对 rig `RequireValid`、handler 初始化唯一调用和两条转换链；未编译、未采样，不能声称实测耗时收益。

### AP131 数组父姿态重复校验收敛（2026-09-30，已实施，本轮未编译）

- `TryCreateComponent` 的 component 数组重载只服务 Modify Bone 与空间转换。两个调用方的父姿态有效边界已建立：Modify Bone 先逐骨骼拒绝无效 Component Pose；空间转换按父序生成并立即拒绝失败结果。原实现仍经公共 parent 重载对同一父姿态再次 `IsValid`。
- 公共 parent 重载继续承担外部父输入校验，先检查后进入 `TryCreateComponentWithValidParent`；数组重载在父序有效的前提下直接进入同一核心。local 有效性、结果 finite/非零检查、归一化、失败返回和输出值不变。
- 该改动删除已证明有效父姿态的重复逐骨骼校验，不新增调用路径或数据复制。静态核对两个数组重载唯一消费链、NativeArray 重载继续校验父输入、核心公式和失败语义；未编译、未采样，不能声称实测耗时收益。

### AP132 Foot 特征消费端校验收敛（2026-09-30，已实施，本轮未编译）

- `AnimationFootFeatureSample` 构造器已拒绝 non-finite 速度/高度并归一化 plant confidence；每个指定的 `AnimationPredictedFootStepSample` 构造器已校验 confidence、时间、phase、phase 顺序、opposing landing 配对，并通过 `AnimationBiomechanicalRoutePage` 构造器校验 25 点 foot/ankle/hip/planar/clearance 页。默认预测步允许存在且当前消费语义已视为通过。
- Slot Blend 原来对同一 sample 的基础字段再查一遍，并对每个指定预测步重建 scalar/route 检查，每次最多重复扫描 100 个 route 元素。`IsValidFoot` 现在只区分正式构造样本与默认未指定样本，删除下游重复 scalar/route 扫描。
- 特征输入、默认预测步行为、Accumulate 和 authoritative prediction 的返回结果不变；构造器继续是 foot feature 唯一业务校验边界。静态核对两个 feature 页构造来源、预测步所有构造器、`RoutePage` 校验和 Slot Blend 调用点；未编译、未采样，不能声称实测耗时收益。

### AP133 Foot 已准备时间入口收敛（2026-09-30，已实施，本轮未编译）

- AP119 的 `SampleCurrent` 是 `SamplePrepared` 的唯一调用点，调用方已经执行 `Mathf.Clamp01(normalizedTime)` 并传入 `time`；方法签名仍接收 normalized time 并重复 clamp。
- `SampleCurrent` 改为 internal 已准备时间入口，参数与公式直接使用 `time`；整条 25 点 `Sample` 编辑器校验入口保持自己的 clamp。采样、索引和输出公式不变。
- 该改动删除 foot 路由每次采样的重复 clamp，并保持唯一正式运行入口。静态核对全仓库唯一调用点和 public `Sample` 的编辑器消费边界；未编译、未采样，不能声称实测耗时收益。

### AP134 Foot feature 已准备时间分层（2026-09-30，已实施，本轮未编译）

- `AnimationFootFeatureCurveSet.SamplePrepared` 先 clamp 得到 `time`，当前与 incoming 两个 `AnimationPredictedFootStepCurveSet.SamplePrepared` 又各自重复 clamp。运行时同一 feature 采样会执行 3 次同一 clamp。
- 两个预测步入口拆出 `SamplePreparedAt`；feature 入口仍负责 normalized time 边界一次，随后 current/incoming 直接消费同一已准备时间。原 `SamplePrepared` 继续作为 normalized time 边界，采样公式和输出顺序不变。
- 该改动删除同一 feature 的两次重复 clamp，不增加第二数据源或兼容入口。静态核对三个入口的唯一调用链、public Sample 边界和 current/incoming 参数；未编译、未采样，不能声称实测耗时收益。

### AP135 Action step clock 已验证入口（2026-09-30，已实施，本轮未编译）

- `AnimationPredictedFootStepSample` 构造器在创建 clock 前已经验证 `EventPhase`、`LiftOffPhase` 和 `TimeToLandingSeconds`；公共 `AnimationActionStepClockSample` 构造器随后重复这三项。duration 仍属于预测步边界，原由 clock 内部再查。
- 预测步先在原校验位置拒绝负 duration，再用 `FromValidated` 直接写入四个字段；公共构造器继续服务外部输入。异常触发位置保持，duration 的 `ParamName` 继续为 `durationSeconds`。
- 该改动删除预测步采样链的重复 phase/time 校验，不改变 clock 输出、异常类型或后续字段赋值顺序。静态核对唯一 `FromValidated` 调用点、原 duration 校验顺序和公共构造器保留边界；未编译、未采样，不能声称实测耗时收益。

### AP136 Action step clock 只读入口修复（2026-09-30，已实施，本轮未编译）

- 静态复查发现 AP135 在 `readonly struct AnimationActionStepClockSample` 上使用 private setter 和对象初始化器；C# 只读结构不允许该实例状态写入方式，属于会阻断编译的结构缺陷。
- 公共构造器委托给显式 `validateInputs` 内部构造器：外部入口先执行 phase、duration 和 time 校验；`FromValidated` 传入已验证边界并直接赋值。四个属性恢复只读 getter，字段值、异常类型和异常顺序不变。
- 该修复保留 AP135 的已验证入口，不引入兼容路径或第二数据源。静态核对 struct 修饰、两个构造器委托、唯一 internal 调用点和公共构造器外部合同；未编译，本轮不做运行验证。

### AP137 SpaceConversion typed input 定容绑定（2026-09-30，已实施，本轮未编译）

- SpaceConversion 每次求输出时先按 `(NodeId, PortId)` 查端口 definition，再按 Kind switch 选择值类型，最后在 `m_InputPorts` Dictionary 中重复 tuple 哈希并做类型测试。其输入端口和类型在 graph 初始化后固定。
- runtime 新增构造期 `RequireInputPort<T>` 和已绑定端口的 `ReadInput<T>` 正式入口；SpaceConversion 在 Initialize 解析唯一 Local/Component typed port，帧内直接读取该 port。`RequireEvaluationStage`、空值错误、类型错误和输出处理时序保持。
- 该改动把每帧三次查找收敛为构造期一次，随后只有 port value 读取；不新增 fallback 或第二数据源。静态核对 BuildPortDefinitions 先于 evaluator Initialize、graph clone 生命周期、两个输入 Kind 和原错误文本；未编译、未采样，不能声称实测耗时收益。

### AP138 ModifyBone typed input 定容绑定（2026-09-30，已实施，本轮未编译）

- Modify Bone 每次求输出都通过 `(NodeId, "pose")` 哈希查找并类型测试同一个 Component Pose 输入；该端口在 graph 初始化后固定。
- handler 在 Initialize 用 AP137 的 typed port 解析入口缓存 `FlowCanvas.ValueInput<ComponentPoseValue>`，帧内直接读取 port value。求值阶段检查、空值错误、输出页复制和修改应用顺序不变。
- 该改动消除 Modify Bone 主姿态输入的每帧 tuple 查找和类型测试；不新增第二数据源。静态核对 Initialize 唯一绑定点、帧读取点和 Component Pose 合同；未编译、未采样，不能声称实测耗时收益。

### AP139 ParameterResolve typed input 定容绑定（2026-09-30，已实施，本轮未编译）

- Parameter Resolve 每次求输出都按 `(NodeId, port)` 分别哈希查找 base pose 与 parameter source pose，并重复做 Local Pose 类型测试；两个输入端口在 graph 初始化后固定。
- handler 在 Initialize 缓存两个 typed `ValueInput`，帧内直接读取 port value。求值阶段检查、空值错误、可用性校验、骨骼复制和参数合成顺序不变。
- 该改动把每帧四次查找合并为构造期两次端口解析，输出页身份不变。静态核对两个端口名、类型、Initialize 唯一调用和 Evaluate 读取链；未编译、未采样，不能声称实测耗时收益。

### AP140 BlendPose typed input 定容绑定（2026-09-30，已实施，本轮未编译）

- Blend Pose 每次求输出都按 `(NodeId, port)` 哈希查找 base 与 overlay 的 Local Pose 输入并重复类型测试；两个端口在 graph 初始化后固定。optional weight 仍走原 Try 读取合同，本项不改变其存在性语义。
- handler 在 Initialize 缓存 base/overlay 的 typed `ValueInput`，帧内直接读取。空值错误、可用性校验、权重解析、continuity 生成和混合顺序不变。
- 该改动消除两个主姿态输入的每帧 tuple 查找；不新增第二数据源或兼容入口。静态核对端口名、类型、Initialize 唯一绑定和混合调用链；未编译、未采样，不能声称实测耗时收益。

### AP141 AnimationSlot typed input 定容绑定（2026-09-30，已实施，本轮未编译）

- Animation Slot 每次求输出都按 `(NodeId, "source-pose")` 哈希查找并类型测试同一个 Local Pose 输入；端口在 graph 初始化后固定。
- handler 在 Initialize 缓存 typed `ValueInput`，帧内直接读取 port value。Action/source 可用性、权重 continuity、混合和输出页写入顺序不变。
- 该改动消除 source pose 的每帧 tuple 查找和类型测试；Action Pose 仍来自同一正式 source 生命周期。静态核对端口名、类型、Initialize 绑定和 Evaluate 读取链；未编译、未采样，不能声称实测耗时收益。

### AP142 AdditivePose typed input 定容绑定（2026-09-30，已实施，本轮未编译）

- Additive Pose 每次求输出都按 `(NodeId, port)` 分别哈希查找 base 与 overlay，并重复做 Local Pose 类型测试；两个端口在 graph 初始化后固定。
- handler 在 Initialize 缓存两个 typed `ValueInput`，帧内直接读取。optional weight 继续使用原 Try 读取合同；可用性校验、reference delta、scale policy 和 continuity 顺序不变。
- 该改动消除两个主姿态输入的每帧 tuple 查找；不新增第二数据源。静态核对端口名、类型、Initialize 唯一绑定和 additive 调用链；未编译、未采样，不能声称实测耗时收益。

### AP143 LayeredBoneBlend typed input 定容绑定（2026-09-30，已实施，本轮未编译）

- Layered Bone Blend 每次求输出都按 `(NodeId, port)` 分别哈希查找 base 与 overlay，并重复做 Local Pose 类型测试；两个端口在 graph 初始化后固定。
- handler 在 Initialize 缓存两个 typed `ValueInput`，帧内直接读取。optional weight 继续使用原 Try 读取合同；bone mask、可用性校验、continuity 和逐骨骼混合顺序不变。
- 该改动消除两个主姿态输入的每帧 tuple 查找；不新增第二数据源。静态核对端口名、类型、formal Bone Mask 边界和 Evaluate 读取链；未编译、未采样，不能声称实测耗时收益。

### AP144 Inertialization typed input 定容绑定（2026-09-30，已实施，本轮未编译）

- Inertialization 每次求输出都按 `(NodeId, "pose")` 哈希查找并类型测试同一个 Local Pose 输入；端口在 graph 初始化后固定。
- handler 在 Initialize 缓存 typed `ValueInput`，帧内直接读取 port value。Local Pose 校验、pending/history 捕获、foot feature、residual 和输出页写入顺序不变。
- 该改动消除输入姿态的每帧 tuple 查找和类型测试；不新增第二数据源。静态核对端口名、类型、policy 绑定和 Evaluate 读取链；未编译、未采样，不能声称实测耗时收益。

### AP145 RootOrientationWarp typed input 定容绑定（2026-09-30，已实施，本轮未编译）

- Root Orientation Warp 每次求输出都按 `(NodeId, "pose")` 哈希查找并类型测试同一个 Local Pose 输入；端口在 graph 初始化后固定。
- handler 在 Initialize 与 FacingError contract 一起缓存 typed `ValueInput`，帧内直接读取 port value。当前帧校验、FacingError 读取、yaw warp 和 pending/commit 顺序不变。
- 该改动消除输入姿态的每帧 tuple 查找和类型测试；不新增第二数据源。静态核对端口名、类型、formal yaw curve 边界和 Evaluate 读取链；未编译、未采样，不能声称实测耗时收益。

### AP146 Constraint pose typed input 定容绑定（2026-09-30，已实施，本轮未编译）

- Foot Placement 与 Pose Bone IK Goals 每次求输出都按 `(NodeId, "pose")` 哈希查找并类型测试同一个 Component Pose 输入；端口在 graph 初始化后固定。
- 两个 handler 在 base Initialize 校验节点身份后分别缓存 typed `ValueInput`，帧内直接读取 port value。Pose 可用性校验、weight override、Constraint 调用、goal header 和 pending 校验顺序不变。
- 该改动消除两个 Constraint 主姿态输入的每帧 tuple 查找和类型测试；不新增第二数据源。静态核对 base 生命周期、端口名、类型和 Evaluate 读取链；未编译、未采样，不能声称实测耗时收益。

### AP147 FullBodyIK 固定输入绑定（2026-09-30，已实施，本轮未编译）

- Full Body IK 每次求输出都按 `(NodeId, "pose")` 哈希查找 Component Pose，并重新遍历 dynamic ports 过滤 Goal Contribution；对 optional contribution 连接时还会重复执行 Try 查找和泛型 Read 查找。这些端口集合在 graph 初始化后固定。
- handler 在 Initialize 缓存 pose typed `ValueInput`，并按原 `DynamicPorts` 顺序缓存 contribution 的 typed `ValueInput`、required 标记和端口名。帧内 required 或 connected 才读取；未连接的 optional contribution 仍跳过，goal binding、solve、输出页和 Commit 语义不变。
- 该改动消除主 pose 的每帧查找和 dynamic contribution 的每帧过滤与重复查找；不新增第二数据源。静态核对初始化前端口表、原 dynamic 顺序、required/optional 分支、null value 错误文案和双缓冲 Commit；未编译、未采样，不能声称实测耗时收益。

### AP148 ModifyBone 参数端口定容绑定（2026-09-30，已实施，本轮未编译）

- Modify Bone 每次输出都重复判断 payload 是否使用 position/rotation/scale 端口，并按 `(NodeId, port)` 哈希读取 weight 和参数；optional weight 还重复字典查找和类型测试。这些端口由固定 payload 和 graph 端口表决定。
- runtime 新增已缓存端口的 typed Try 读取，保留 `isConnected`、null value 和错误文案合同；Modify Bone 在 Initialize 缓存 weight，并只在 payload 使用对应 transform 时缓存该端口。帧内直接用缓存端口读取，同帧与 Kind 校验顺序不变。
- 该改动消除 weight 与 transform 参数的每帧 tuple 查找、类型测试和重复 payload 判断；不改变未连接 weight 回退 payload weight 的语义。静态核对端口形状、缓存唯一入口、原错误文本和输出页写入顺序；未编译、未采样，不能声称实测耗时收益。

### AP149 optional weight 定容绑定（2026-09-30，已实施，本轮未编译）

- Blend Pose、Additive Pose、Layered Bone Blend 和 Foot Placement 每次输出都按 `(NodeId, "weight")` 哈希查找 optional weight 并重复类型测试；weight 端口在 graph 初始化后固定。
- 四个 handler 在 Initialize 缓存 typed `ValueInput`；混合类同时缓存节点默认 weight。帧内用 AP148 的 typed Try 读取，未连接仍回退默认值或关闭 override，connected value 的 `[0,1]` 校验、continuity、Constraint 输入和输出顺序不变。
- 该改动消除四类 optional weight 的每帧字典查找和类型测试；不新增第二数据源。静态核对端口形状、默认值来源、原错误文本、continuity 和 pending/Commit 语义；未编译、未采样，不能声称实测耗时收益。

### AP150 FullBodyIK goals 定容绑定（2026-09-30，已实施，本轮未编译）

- Full Body IK 每次输出都按 `(NodeId, "goals")` 哈希查找 optional goal set 并重复类型测试；端口在 graph 初始化后固定。
- handler 在 AP147 的固定输入初始化中缓存 goals typed `ValueInput`，帧内用 typed Try 读取；未连接仍不绑定，connected goal set 的 BindPendingGoalSet、IK solve、输出页和 Commit 顺序不变。
- 该改动消除 optional goals 的每帧 tuple 查找和类型测试；不新增第二数据源。静态核对端口形状、未连接分支、goal 绑定生命周期和双缓冲语义；未编译、未采样，不能声称实测耗时收益。

### AP151 Subgraph input 定容绑定（2026-09-30，已实施，本轮未编译）

- Pose Subgraph 每次绑定 immediate/deferred 输入都按 parent port 重复查找 runtime port definition，再按 Kind switch 进入端口字典并做类型测试；接口绑定时已经定位并检查了连接。
- input binding 在 Start 缓存 parent `ValueInput`、parent/child 端口和 Kind；runtime 新增已缓存端口的读取入口，先按 Kind 分派，再保留 null value 与 typed native 错误文案。BuildPortDefinitions 已在构造期校验同一端口的 FlowCanvas 原生类型。
- 该改动消除每帧 parent/child interface 的重复 definition 查找和 tuple 端口二次查找；不改绑定顺序、deferred 分类、输出页身份和 graph input 提交合同。静态核对 Start 时子图端口表、连接检查、错误文案和 BindInputs 两条调用链；未编译、未采样，不能声称实测耗时收益。

### AP152 Subgraph output 定容绑定（2026-09-30，已实施，本轮未编译）

- Pose Subgraph 每次求输出都通过 child `ReadGraphOutput` 重新定位 GraphOutput 边界、查找 port definition、分派 Kind、查端口字典并做类型测试；输出映射和 child GraphOutput 输入端口在 Start 后固定。
- input/output binding 统一缓存源节点、child 端口、typed `ValueInput` 和 Kind；EvaluateOutput 用缓存绑定直接读取 child GraphOutput。缺失输出仍按 parent port 报原错误，child graph input 读取的 null value 与类型错误文案保持。
- 该改动消除每次 subgraph 输出的重复边界和 definition 查找，并删除全仓库无调用的 `ReadGraphOutput` 入口；不改输出页映射、求值阶段、Commit/Discard 和输出值身份。静态核对 Start 缓存、GraphOutput NativeNodeId、Kind 分派和 EvaluateOutput 调用链；未编译、未采样，不能声称实测耗时收益。

### AP153 LinkedPose output 定容绑定（2026-09-30，已实施，本轮未编译）

- Linked Pose 每次输出都重新扫描 dynamic ports 查找并复验 Local Pose output；Initialize 已经要求该节点有且只有一个 Local Pose output。
- handler 在 Initialize 扫描同一集合时缓存唯一 output `PosePortId`，帧内先比较 output 身份再进入原求值缓存和双缓冲流程。无对应 Local Pose output 的错误文案不变，Source Prepare/Evaluate/Commit/Discard 不变。
- 该改动消除每次输出的 dynamic port 扫描和重复 Kind 判断，并删除无调用的 `FindOutputPort`；不新增第二数据源。静态核对唯一输出约束、`PosePortId` 比较、错误路径和输出页身份；未编译、未采样，不能声称实测耗时收益。

### AP154 source request 配置定容绑定（2026-09-30，已实施，本轮未编译）

- Clip Player、Blend Space Player、Selected Pose Player 和 Blend Stack 每次准备 source request 都重新通过 `node.PresentationPoseSourceSlot` switch/cast payload；Clip 还重复读取 `AnimationChannelId`，Blend Stack 对每个 source 重复读取同一 slot。这些配置来自 Initialize 已校验的固定 payload。
- 四类 handler 在 Initialize 缓存 source slot；Clip 同时缓存 channel。Blend Stack 正式 source binding 接口从传入 node 改为传入已解析 slot，每个 source request 使用同一值。
- 该改动消除 source request 准备期的重复 payload 分派；不改 source kind 校验、request 内容、延迟源分类和 Prepare/Evaluate 时序。静态核对 payload kind、slot 类型、唯一接口实现和四条 PrepareFrame 调用链；未编译、未采样，不能声称实测耗时收益。

### AP155 AdditivePose scale policy 定容绑定（2026-09-30，已实施，本轮未编译）

- Additive Pose 每次准备输出都通过 `node.AdditiveScalePolicy` switch/cast payload，再传给 additive job；Initialize 已完成 Multiply/AddDelta 正式边界校验。
- handler 在 Initialize 缓存 scale policy，帧内直接使用同一值。reference pose/space 校验、base/additive 读取、weight continuity、delta 计算和输出页写入顺序不变。
- 该改动消除 additive 输出准备期的重复 payload 分派；不新增第二数据源。静态核对 policy 校验位置、调用链和 ApplyAdditive 参数顺序；未编译、未采样，不能声称实测耗时收益。

### AP156 AnimationSlot source slot 同步定容（2026-09-30，已实施，本轮未编译）

- AP154 将 BlendStack source binding 的正式入口改为已解析 source slot 后，AnimationSlot source 链仍把 `CharacterPoseCanvasNode` 传入该入口，静态复查发现会造成接口不匹配；同时 AnimationSlot 每帧还会重复读取 presentation source slot。
- AnimationSlot handler 在 Initialize 缓存 source slot，`ICharacterPoseNativeAnimationSlotSource` 与 BlendStack binding 一样直接接收 source slot。Action source frame、slot selection、request 内容、retirement 和 Commit/Discard 顺序不变。
- 该改动修复 AP154 静态复查发现的跨接口断点，并删除 AnimationSlot source 准备期的重复 payload 分派；不保留 node/slot 双路径。静态核对唯一接口实现、PrepareFrame 调用链和错误边界；未编译、未采样，不能声称实测耗时收益。

### AP157 StateMachine active state 求值准备收敛（2026-09-30，已实施，本轮未编译）

- StateMachine 在 `PrepareEvaluation` 先由 `SynchronizeTransition` 调用 `CollectActiveStates`，随后又立即调用一次；同一 pending state/transition 在两次调用之间不变，每次都重复 state dictionary 查找和 active slot 排列。
- `SynchronizeTransition` 改为接收已求得的 active state count；evaluation preparation 仍先调用一次收集，再用同一 count 做 phase 同步和子图准备。active states 内容、phase 同步阈值、异常时机和后续 Evaluate/Commit 语义不变。
- 该改动删除同一准备阶段的重复 active state 收集；不在跨阶段引入缓存状态。静态核对 pending state/transition 的修改点、`CollectActiveStates` 排列和 PrepareEvaluation 调用顺序；未编译、未采样，不能声称实测耗时收益。

### AP158 frame input 只读引用入口（2026-09-30，已实施，本轮未编译）

- graph 与 role runtime 的 `CurrentInput` 按值返回 `CharacterPoseNativeFrameInput`；handler 每次直接读取 `ParameterFrame`、`FactFrame` 或 scalar 时都会先复制整份 readonly frame。字段在 BeginFrame 写入后整个帧阶段只被消费。
- 两个 internal 属性改为返回 `ref readonly`，直接字段访问不再复制整份 frame；显式赋值给局部 frame 的调用仍按 C# 语义复制并保持多个字段消费方式不变。
- 该改动统一 frame input 的正式只读入口，不新增第二数据源或可变写入路径。静态核对 `CharacterPoseNativeFrameInput` readonly struct、`m_FrameInput` 帧生命周期、两个 runtime 属性和现有直接访问/局部赋值调用；未编译、未采样，不能声称实测耗时收益。

### AP159 handler frame input 只读局部化（2026-09-30，已实施，本轮未编译）

- Constraint 四条输出链和 RootOrientationWarp 仍把 `CurrentInput` 复制到按值局部变量；RootOrientationWarp 还两次直接读取 FactFrame。frame 在同一次输出求值内不变。
- 这些调用点改为 `ref readonly` 局部引用，随后沿原顺序消费 Parameter/Fact frame 和 scalar；Foot Placement、Pose Bone IK Goals、Goal Assembler、Full Body IK 和 root warp 的校验、计算、输出页与 Commit 顺序不变。
- 该改动删除 handler 输出链的整份 frame input 拷贝；不依赖或修改其他窗口的 GraphEvaluator。静态核对 AP158 readonly 引用入口、各局部使用字段和原有分支顺序；未编译、未采样，不能声称实测耗时收益。

### AP160 StateMachine active state 帧内定容（2026-09-30，已实施，本轮未编译）

- StateMachine 在 phase 访问、PrepareEvaluation、Evaluate 和 Commit 重复执行 `CollectActiveStates`；同一帧 pending state/transition 在 PrepareFrame 完成推进后不再变化，重复执行只重复 dictionary 查找和 active slot 写入。
- PrepareFrame 在 pending state/transition 完成本帧推进后收集一次 active states，并把 count 与 slots 作为帧内正式状态；phase、evaluation 和 commit 直接消费同一数组。discard/commit 清理子帧时同步清空 count。
- 该改动删除同一打开帧内跨阶段的重复 active state 解析；不新增 fallback 或第二数据源。静态核对 pending state/transition 的全部修改点、PrepareFrame 收集时机、子帧 open/discard 生命周期和 phase 访问时序；未编译、未采样，不能声称实测耗时收益。

### AP161 frame lineage 只读引用入口（2026-09-30，已实施，本轮未编译）

- graph 与 role runtime 的 `CurrentLineage` 按值返回 readonly lineage；handler 每次读取 `CompletionIdentity` 或参与 identity 比较前都会复制包含 graph/rig/contract identity 的整份 lineage。completed lineage 在 commit 后整帧保持不变。
- 两个 internal 属性改为返回 `ref readonly`，直接字段读取不再复制整份 lineage；显式赋值和按值 equality 参数仍按 C# 语义复制。
- 该改动统一 lineage 的正式只读入口，不改变 identity 内容、输出页完成标记和 Commit/Discard 判定。静态核对 readonly lineage、completed lineage 生命周期、两个 runtime 属性和现有字段读取/局部赋值调用；未编译、未采样，不能声称实测耗时收益。

### AP162 lineage 全量匹配引用化（2026-09-30，已实施，本轮未编译）

- source demand barrier 与 StateMachine frame 输入使用 `!=` 比较 lineage；按值 operator 会把 readonly 引用两侧各复制一次。identity 比较本身只需要读取同一字段集合。
- lineage 新增 `Matches(in lineage)` 逐字段正式匹配入口；BlendStack、Blend Space、Clip、Selected Pose 的 demand barrier 和 StateMachine PrepareFrame 输入改用该入口。匹配结果、异常时机、输出页完成判定和后续阶段不变。
- 该改动删除这些全量 lineage 比较的两侧整份拷贝；不新增第二 identity 数据源。静态核对 `Matches` 字段集合与原 Equals 一致、readonly 引用入口和五个调用点；未编译、未采样，不能声称实测耗时收益。

### AP163 nested frame 只读引用入口（2026-09-30，已实施，本轮未编译）

- `CharacterPoseNativeFrameInput` 的 Body/Fact/Parameter frame 是按值属性；外层 `CurrentInput` 引用化后，handler 每次读取嵌套帧仍会复制整份。frame 在 BeginFrame 写入后同帧只被消费。
- 三个嵌套帧改为 readonly 字段加 `ref readonly` 属性正式入口；Action Slot、Blend Space、Clip、Foot Placement、RootOrientationWarp 和 StateMachine 的按值局部引用改为 `ref readonly` 局部引用。字段读取、校验顺序、输出内容和 Commit/Discard 语义不变。
- 该改动删除周期链中嵌套 frame 的整份拷贝；不新增第二数据源或可变写入路径。其他窗口持有的 GraphEvaluator 文件未修改，其直接嵌套帧访问会继续使用同一属性。静态核对构造赋值、全部当前调用点、readonly 局部生命周期和原有分支；未编译、未采样，不能声称实测耗时收益。

### AP164 frame lease 匹配引用化（2026-09-30，已实施，本轮未编译）

- Source 与 Constraint frame lease 每次 `Matches` 都调用 `WithCompletion(0)` 构造新 lineage，再通过按值 operator 复制两侧；native frame lease 也使用按值 operator。三个 lease 的 `Lineage` 属性本身还按值返回整份 lineage。
- lineage 新增忽略 CompletionIdentity 的正式引用匹配入口；source/constraint lease 用它保留原有开放帧 identity 合同，native lease 继续全字段匹配。三个 lease 的 Lineage 改为 readonly 字段加 `ref readonly` 属性，Matches 参数改为 `in`。比较结果、异常时机和阶段校验顺序不变。
- 该改动删除 lease 匹配期的临时 lineage 与两侧整份拷贝；不新增第二数据源。静态核对三个 lease 的构造约束、新旧字段集合、全部 Matches 调用和 Lineage 消费语义；未编译、未采样，不能声称实测耗时收益。

### AP165 native frame lease 参数引用化（2026-09-30，已实施，本轮未编译）

- graph 与 role runtime 的 Prepare、PrepareEvaluation、Evaluate、ValidatePending、Commit、Discard 都按值接收 `CharacterPoseNativeFrameLease`；BeginFrame 还把 lease.Lineage 复制到局部 lineage。这些参数在阶段间不变。
- graph 的 8 个阶段入口和 role 的 6 个转发入口统一改为 `in` 参数；BeginFrame 的 openLineage 改为 readonly 引用。调用点继续传同一 lease，RequireLease、异常时机、阶段推进和 Commit/Discard 语义不变。
- 该改动删除每个阶段的整份 lease 拷贝；不新增第二生命周期路径。静态核对 graph/role 全部按值参数、调用绑定、readonly 引用生命周期和 BeginFrame 异常清理；未编译、未采样，不能声称实测耗时收益。

### AP166 source demand lineage 引用化（2026-09-30，已实施，本轮未编译）

- `CharacterPoseNativeSourceDemand.Lineage` 按值返回整份 identity；graph、StateMachine handler/source 和 AnimationSlot source 在 barrier 校验中还用按值 operator 比较。demand 在构造后同一帧保持只读。
- demand Lineage 改为 readonly 字段加 `ref readonly` 属性；5 处 identity 比较改为现有 `Matches(in lineage)`。Requests、request key、错误文案和异常时机不变。
- 该改动删除 demand 读取和 identity 校验的整份 lineage 拷贝；不新增第二 demand 数据源。静态核对构造赋值、全部按值 equality 消失、5 个比较点字段集合和 barrier 顺序；未编译、未采样，不能声称实测耗时收益。

### AP167 native result lineage 引用化（2026-09-30，已实施，本轮未编译）

- Preparation、Evaluation、Publication、Validation result 的 `Lineage` 按值返回整份 identity；graph runtime 在 Validate 和两条 Commit 路径用按值 operator 与 completed lineage 比较。result 构造后同帧只被消费。
- 四个 result 的 Lineage 改为 readonly 字段加 `ref readonly` 属性；graph runtime 的 5 处 identity 比较改用 `Matches(in lineage)`。result 状态、output、错误文案和异常时机不变。
- 该改动删除 result 读取和 Validate/Commit 校验的整份 lineage 拷贝；不新增第二 result 路径。静态核对四个构造器、字段集合、全部 graph runtime 比较点和默认/失败结果生命周期；未编译、未采样，不能声称实测耗时收益。

### AP168 周期 result demand 与 lineage 引用化（2026-09-30，已实施，本轮未编译）

- Native Preparation 的 source demand、Source Frame 的 demand、prepared resources、committed result 和 Constraint result 都按值返回或存储读取；`SourceFrame.Lineage` 还会先复制整份 demand。这些对象构造后同帧只读。
- 相关 demand/lineage 入口改为 readonly 字段加 `ref readonly` 属性；source、constraint、final publication 和 physical diagnostics 的 10 处 identity 比较改用 `Matches(in lineage)`。请求集合、readiness、goal、输出页和异常文本不变。
- 该改动删除周期 result 的整份 demand/lineage 读取拷贝；不新增第二数据源或可变路径。静态核对构造赋值、嵌套引用生命周期、10 个新旧比较字段集合和调用边界；未编译、未采样，不能声称实测耗时收益。

### AP169 周期 result 局部只读引用化（2026-09-30，已实施，本轮未编译）

- FrameCoordinator、RoleRuntime、Subgraph、StateMachineSource、FinalPublication 和 SourceModule 在阶段转发前仍把 readonly result 的 demand 或 lease 的 lineage 显式复制到按值局部；共 8 个周期路径调用点。这些局部同方法内只读。
- 8 个局部改为 `ref readonly` 引用，继续沿原顺序传给 `in` 参数或构造器；FinalPublication 保持读取 lineage、Clear pending、再用同一 lineage 构造 committed result 的顺序。请求集合、约束完成、输出页和异常语义不变。
- 该改动删除周期阶段间的 demand/lineage 局部拷贝；不延长数据生命周期或新增缓存。静态核对每个引用的宿主生命周期、全部消费点、Clear 顺序和原有失败路径；未编译、未采样，不能声称实测耗时收益。

### AP170 source frame lease 参数引用化（2026-09-30，已实施，本轮未编译）

- Source frame lease 包含整份开放帧 lineage；接口、Animancer/ACL backend、backend set、pending page 和 source module 的 Prepare/Validate/Barrier/Commit 阶段大多按值接收，每次转发都重复复制。
- 56 个不会修改 lease 的正式参数改为 `in`；SourceModule 与 BackendSet 的 `DiscardFrame` 保留按值，因为清理 lambda 必须捕获 lease。接口两个实现同步更新，调用点继续传同一 lease。
- 该改动删除 source 阶段转发的整份 lease 拷贝；不改变 lease identity、rollback 顺序或 commit/discard 语义。静态核对接口实现全集、lambda 捕获例外、方法组和阶段调用链；未编译、未采样，不能声称实测耗时收益。

### AP171 constraint lease 参数引用化（2026-09-30，已实施，本轮未编译）

- Constraint runtime 的 Complete、Seal、Discard 和 pending lease 校验都按值接收包含整份 lineage 的 lease；这些阶段只读取 identity 并推进同一 pending 帧。
- 四个参数改为 `in`，RoleRuntime 可直接传字段引用；清理时先复制再清空字段的路径保留。identity 校验、Foot Placement 发布、bank 切换和 Discard 顺序不变。
- 该改动删除 constraint 阶段转发的整份 lease 拷贝；不新增第二生命周期。静态核对全部按值参数、调用点、字段清空顺序和失败路径；未编译、未采样，不能声称实测耗时收益。

### AP172 source demand 返回引用化（2026-09-30，已实施，本轮未编译）

- Pending page 和 SourceModule 的 `RequireDemand` 按值返回整份 demand；PrepareFrameResult 与 RequirePreparedResources 再复制到按值局部。demand 在 lease 校验后同一开放帧内只读。
- 两个返回入口改为 `ref readonly`，两个局部改为 readonly 引用；构造 SourceFrame 与 PreparedResources 时继续传 `in`。lease 校验、错误时机、result 字段和输出页身份不变。
- 该改动删除 source demand 返回和局部读取的整份拷贝；不暴露可变写入路径。静态核对 backing 字段生命周期、全部 RequireDemand 调用、构造参数和原有异常路径；未编译、未采样，不能声称实测耗时收益。

### AP173 source demand 嵌入数据引用化（2026-09-30，已实施，本轮未编译）

- 旧 `CharacterPoseSourceDemand` 的 Lineage 和 Preparations 仍按值属性返回；pending page 比对 preparation 前还会复制 expected view。这是 SourceFrame.Lineage 引用链底层的重复读取。
- Lineage 与 Preparations 改为 readonly 字段加 `ref readonly` 属性；ConsumePreparation 的 expected 改为只读引用后直接 Matches。provider demand、计数、lease 校验和错误文本不变。
- 该改动删除 source demand 嵌入 lineage/view 的整份拷贝；不新增第二 preparation 数据源。静态核对构造赋值、全部 Preparations/Lineage 消费点、Matches 参数和 page 生命周期；未编译、未采样，不能声称实测耗时收益。

### AP174 StateMachine demand 循环定容（2026-09-30，已实施，本轮未编译）

- StateMachine 在 PrepareFrame 收集请求和 PrepareEvaluation 校验子请求时，循环条件与索引访问反复执行 `state.Preparation.Demand.Requests`。Preparation 是状态对象字段，demand 同帧只读。
- 每个状态把 demand 定成 `ref readonly` 局部，request list 缓存为同一 `IReadOnlyList` 引用；收集、丢失校验和 child PrepareEvaluation 使用同一 demand。请求顺序、错误文本和 barrier 语义不变。
- 该改动删除周期循环中的重复只读入口调用；不复制 demand 或 request。静态核对三个状态 demand 入口、原有循环边界、异常路径和活跃状态生命周期；未编译、未采样，不能声称实测耗时收益。

### AP175 source cleanup 去委托化（2026-09-30，已实施，本轮未编译）

- Source commit 回滚、backend discard 和 SourceModule discard 原先用 `Action` 包装每个清理步骤；discard 正常路径每帧会构造闭包/委托，源模块还会为 pending registration 循环分配捕获变量。
- 周期清理改为专用正式方法或直接 try/catch，失败仍按原顺序记录；BackendSet 保留原有 AggregateException 规则，SourceModule 保留 pending count、倒序 disconnect、backend、frame page、physical source 的清理顺序。两个 DiscardFrame lease 参数随闭包消除改为 `in`。
- 该改动删除 Source discard/commit 异常清理路径的周期委托分配；不改变失败聚合、rollback/discard 语义或数据生命周期。静态核对两文件的全部剩余 lambda 仅限一次性 Dispose、异常捕获顺序和大括号配对；未编译、未采样，不能声称实测耗时收益。

### AP176 body branch replacement tick 复用（2026-09-30，已实施，本轮未编译）

- Body Presentation 的 committed branch replacement 每次移除旧尾部时新建 `List<ulong>`；该路径由正式提交流分支替换触发，不是初始化专用。
- runtime 持有复用 tick list；每次 Clear 后按当前 committed count 预留容量，再按原有序字典枚举顺序收集 `>= firstTick` 的 tick，并按同一顺序移除 body 和 yaw velocity。
- 该改动删除分支替换的每次局部 List 分配；不改变 replacement 判定、tick 排序、retarget 或字典删除顺序。静态核对两个调用点、容量上界、枚举只读和 Dispose/Reset 生命周期；未编译、未采样，不能声称实测耗时收益。

### AP177 physical pose per-bone 只读引用化（2026-09-30，已实施，本轮未编译）

- Final Publication 的物理写回在每根骨骼循环中通过 `AnimationReadOnlyBuffer<T>` 索引器取值；`AnimationLocalBonePose` 包含 position、rotation 和 scale，索引器按值返回后在写回前重复复制整份 pose。
- 物理 writer 改用已有 `ElementAt` 正式只读引用入口；root policy 分支直接绑定 root reference pose 或 dense pose 元素，随后按原顺序读取字段并写回 Transform。
- 该改动删除逐骨的整份 pose 拷贝；不改变 bone 可用性校验、root 排除规则、Transform 写入顺序或 foot IK capture 时机。静态核对 buffer readonly struct、`ElementAt` 返回合同和 Write 唯一循环；未编译、未采样，不能声称实测耗时收益。

### AP178 publication dense pose 单次拷贝（2026-09-30，已实施，本轮未编译）

- Final Publication 准备 pending page 时，bone 循环先对 `DenseLocalPoses[bone]` 做一次按值读取校验，再按值读取同一元素写入目标；循环内还重复读取 binding slice、pending page 和 bone count。
- 准备阶段缓存 output slice 和 pose offset，每骨绑定 `NativeSlice` 元素的 `ref readonly` 引用；同一引用先完成 `IsValid` 校验，再写入 dense page。
- 该改动把每骨两次整份 pose 读取收敛为一次目标写入拷贝，并删除循环内的重复 offset 计算；不改变布局校验、异常文本、页身份或写入顺序。静态核对 `NativeSlice<T>` 元素引用、原长度校验和 pending 页生命周期；未编译、未采样，不能声称实测耗时收益。

### AP179 publication contribution 循环定容（2026-09-30，已实施，本轮未编译）

- Contribution 循环每次读取 primitive 和 weight 都重新取 read binding 的 `NativeSlice` 属性；resolved source 写入目标数组后又按值复制一次读取 Kind、SourceId 和 NodeId；每骨还重复计算同一个 contribution 的 weight offset。
- 循环外缓存 primitive/weight slice、contribution 和 dense weight offset；resolved source 写入后用目标数组元素引用直接生成 clip sample，contribution 的 weight offset 每层只算一次。
- 该改动删除 resolved contribution 的读回拷贝和循环内重复 offset 计算；不改变 Resolve 校验、Live clip sample 查询、权重校验、异常文本或数组写入顺序。静态核对 `NativeSlice` 引用合同、目标数组生命周期和 contribution/dense weight 容量；未编译、未采样，不能声称实测耗时收益。

### AP180 publication parameter slice 定容（2026-09-30，已实施，本轮未编译）

- Final Publication 的参数准备循环每项重新读取 `PoseParameters` 和 `PoseParameterAvailability` 两个 `NativeSlice` 属性，并按原 page offset 写入。
- 循环前缓存两个 slice 和 parameter offset；参数仍逐项读取并完成 finite/availability 校验后写入 pending page。
- 该改动删除逐参数的 slice 结构重复读取；不改变参数数量校验、可用值合同、异常文本或输出页写入顺序。静态核对两个 `NativeSlice` 长度已在同一入口校验、目标数组生命周期和原循环边界；未编译、未采样，不能声称实测耗时收益。

### AP181 AdditivePose bone 输入引用化（2026-09-30，已实施，本轮未编译）

- Additive 混合每根骨骼把 base pose、additive pose、additive velocity 和 base velocity 各复制成按值局部，随后只读取字段；reference bone 也从数组按值复制。
- 混合分支缓存四个输入 `NativeSlice`，base/additive pose、velocity 和 reference 都绑定 `ref readonly` 元素引用；position、rotation、scale 公式和 `Multiply/AddDelta` 分支保持不变。
- 该改动删除逐骨的四类输入结构拷贝；不改变 output 写入、continuity、foot/publication 顺序或异常时机。静态核对 `NativeSlice<T>` 与数组元素引用合同、input/output 长度校验和 ApplyAdditive 唯一循环；未编译、未采样，不能声称实测耗时收益。

### AP182 BlendPose 输入引用化（2026-09-30，已实施，本轮未编译）

- Blend Pose 双源混合每骨复制 base/overlay pose 与 velocity；参数循环每次又重新读取 base/overlay 的 parameter 和 availability slice。
- 混合分支缓存四个输入 slice 并绑定 `ref readonly` 元素引用；参数循环缓存四个输入 slice。`BlendWeighted` 的 reference 按值合同暂不改变，输出计算、可见权重校验、availability 合并和 contributions 顺序不变。
- 该改动删除 bone 混合的中间 pose/velocity 拷贝和逐参数 slice 重复读取；不改变参数公式、异常文本、输出页身份或 Commit/Discard。静态核对 read binding 长度校验、`NativeSlice` 引用合同和双分支唯一消费点；未编译、未采样，不能声称实测耗时收益。

### AP183 LayeredBoneBlend 输入引用化（2026-09-30，已实施，本轮未编译）

- Layered Bone Blend 的 bone 循环在双权重分支复制 base/overlay pose 与 velocity；参数循环每次重新读取 base/overlay parameter 和 availability slice。
- bone 循环前缓存四个输入 slice，双权重分支绑定 `ref readonly` 元素引用；参数循环缓存四个输入 slice。per-bone mask、早退分支、可见权重校验、参数公式和 contributions 顺序不变。
- 该改动删除逐骨中间 pose/velocity 拷贝和逐参数 slice 重复读取；不改变 bone mask 业务权重、输出页身份或 Commit/Discard。静态核对 read binding 长度校验、mask 数组生命周期和唯一 BlendPose 循环；未编译、未采样，不能声称实测耗时收益。

### AP184 AnimationSlot bone 输入引用化（2026-09-30，已实施，本轮未编译）

- Animation Slot 双源混合每根骨骼把 source/action pose 和 velocity 复制为按值局部；这些局部随后只被字段读取。
- bone 循环前缓存四个输入 `NativeSlice`，source/action pose 与 velocity 绑定 `ref readonly` 元素引用；per-bone contribution 权重、pose 可用性校验、混合公式、parameters、contributions 和 feet 顺序保持不变。
- 该改动删除逐骨四类输入结构拷贝；不改变输出页身份、continuity 或 Commit/Discard。静态核对 `BoneOutputWeight` 不访问 pose/velocity slice、read binding 长度校验和唯一 BlendPoses 循环；未编译、未采样，不能声称实测耗时收益。

### AP185 Inertialization bone 输入引用化（2026-09-30，已实施，本轮未编译）

- Inertialization 在 BeginTransition、ApplyResiduals 和 CommitHistory 的逐骨循环中按值复制 input、history 或 output 的 pose/velocity；ApplyResiduals 还复制 envelope sample。
- 三段循环分别缓存 input/output slice；history、target、velocity 绑定数组或 `NativeSlice` 元素只读引用，envelope sample 改为只读引用。residual 公式、有效值校验、异常文本、参数/foot 应用和 history swap 顺序不变。
- 该改动删除 inertialization 周期 bone 链的中间结构拷贝和重复 binding 读取；不改变事件 identity、pending/committed 页语义或 Commit/Discard。静态核对三个唯一循环、字段消费只读和 `EnvelopeSample` 生命周期；未编译、未采样，不能声称实测耗时收益。

### AP186 StateMachine transition bone 引用化（2026-09-30，已实施，本轮未编译）

- StateMachine transition 混合每根骨骼重复读取 source/target/output 的 pose 和 velocity binding 属性，并把 source/target pose 与两个 velocity 复制为按值局部。
- bone 循环前缓存六个 `NativeSlice`；source/target pose 和 velocity 绑定 `ref readonly` 元素引用，输出仍写原 dense page。全局权重、混合公式、参数、contribution、foot 和输出页顺序不变。
- 该改动删除 transition 每骨的四类结构拷贝和重复属性读取；不改变 child 输出身份、active state 生命周期或 Commit/Discard。静态核对布局校验、`NativeSlice` 引用合同和唯一 BlendTransition bone 循环；未编译、未采样，不能声称实测耗时收益。

### AP187 StateMachine transition parameter 定容（2026-09-30，已实施，本轮未编译）

- StateMachine transition 参数循环每项重复读取 source/target 的 parameter 与 availability binding 属性，以及 output 的 parameter 与 availability binding 属性；这些 `NativeSlice` 在同一输出页内不变。
- 循环前缓存六个 slice；source/target availability 仍逐项读取并按原三分支合并，参数公式和输出 availability 写入顺序不变。
- 该改动删除 transition 参数链的重复 binding 结构读取；不改变参数数量合同、输出页身份或 Commit/Discard。静态核对 layout 校验中 pose parameter 与 availability 等长、`NativeSlice` 结构合同和唯一 BlendParameters 循环；未编译、未采样，不能声称实测耗时收益。

### AP188 StateMachine transition contribution 定容（2026-09-30，已实施，本轮未编译）

- StateMachine transition 追加 contribution 时每次读取 input/output 的 contribution 与 dense weight binding 属性；primitive 按值复制，每骨重复计算 `count * boneCount` 和 `i * boneCount`，循环条件还重复读 output pose 长度。
- factor 分支后缓存 input/output 的 contribution 与 weight slice、bone count；primitive 绑定只读引用，每个 contribution 预计算两侧行 offset。容量校验、`ExtendContributionPrefix`、字段换算、权重公式和 source/target 顺序不变。
- 该改动删除 contribution 追加链的重复 binding 读取、primitive 拷贝和逐骨重复乘法；不改变 count 推进或输出页身份。静态核对 read binding 的 dense weight 行容量、output 容量校验和 `ExtendContributionPrefix` 只更新 count；未编译、未采样，不能声称实测耗时收益。

### AP189 ModifyBone pose 输入引用化（2026-09-30，已实施，本轮未编译）

- Modify Bone 的 pose 分支每骨按值复制 input pose，`modifies` 时再转换 scratch；输出阶段又按值复制每个 component scratch pose，NoPose 分支每次重复读取 input binding 属性。
- pose 与 NoPose 分支缓存 input/output slice；input 绑定只读元素引用，修改输出绑定 scratch 数组只读引用。invalid 校验、`modifies` 分支、ApplyModification 写入 scratch 和输出转换顺序不变。
- 该改动删除 Modify Bone 周期 pose 输入和输出阶段的中间结构拷贝；不改变 Component Pose 空间合同或输出页身份。静态核对 input/output 长度校验、readonly struct 的 `in` 构造合同和三条唯一循环；未编译、未采样，不能声称实测耗时收益。

### AP190 SpaceConversion pose 引用化（2026-09-30，已实施，本轮未编译）

- Space Conversion 转换 local/component pose 时按值复制每个 input 元素，ComponentToLocal 的 root 分支再按值读取 scratch；底层 `TryCreateComponent` 和 `TryCreateLocal` 也按值接收 pose。
- `TryCreateComponent` 的 local 与 parent、`TryCreateLocal` 的 component 与 parent 正式边界改为 `in`；两条转换循环缓存 input/output slice，input 与 root/current/parent scratch 绑定只读引用。父索引推导、有效值校验、错误文本和 scratch 写入顺序不变。
- 该改动删除 Space Conversion 周期转换的中间 pose 拷贝，并统一 pose 数学边界的只读传参；不改变 Local/Component 输出空间或输出页身份。静态核对全部现有调用仍可传值、方法本体不写参数、readonly struct 字段只读和两个唯一转换循环；未编译、未采样，不能声称实测耗时收益。

### AP191 AnimationSlot parameter 定容（2026-09-30，已实施，本轮未编译）

- Animation Slot 参数混合每项重复读取 source/action 的 parameter 和 availability binding 属性；同一方法内的 output slice 已缓存，但输入 `NativeSlice` 每次索引都重新取。
- 循环前缓存 source/action 的 parameter 与 availability slice；source/action 可用性仍逐项读取，并按原三分支完成参数混合或 fallback availability 写入。
- 该改动删除 Animation Slot 参数链的重复 binding 结构读取；不改变参数公式、availability 合并、输出页身份或 Commit/Discard。静态核对 read binding 的参数/可用性等长校验、pose graph layout 校验和唯一 BlendParameters 调用；未编译、未采样，不能声称实测耗时收益。

### AP192 AnimationSlot contribution 定容（2026-09-30，已实施，本轮未编译）

- Animation Slot 追加 contribution 时每层按值复制 input primitive，并重复读取 input 的 contribution 与 dense weight binding 属性；逐骨权重写入还重复计算 output/input 两侧行偏移。
- 循环前缓存 input contribution 与 weight slice；primitive 绑定只读元素引用，每个 contribution 预计算 output/input 行偏移。output 容量校验、`ExtendContributionPrefix`、字段换算、权重公式和 source/action 追加顺序不变。
- 该改动删除 Animation Slot contribution 链的 primitive 拷贝、重复 binding 读取和逐骨乘法；不改变 count 推进或输出页身份。静态核对 read binding 的 dense weight 行容量、output 容量校验和唯一 AppendContributions 循环；未编译、未采样，不能声称实测耗时收益。

### AP193 BlendPose contribution 定容（2026-09-30，已实施，本轮未编译）

- Blend Pose 追加 contribution 时每层按值复制 input primitive，并重复读取 input 的 contribution 与 dense weight binding 属性；逐骨权重写入重复计算 output/input 行偏移。
- factor 早退后再缓存 input slice；primitive 绑定只读元素引用，每个 contribution 预计算两侧行偏移。weight 过滤、容量校验、`ExtendContributionPrefix`、权重公式和 base/overlay 顺序不变。
- 该改动删除 Blend Pose contribution 链的 primitive 拷贝、重复 binding 读取和逐骨乘法；不改变输出 count、可见权重或输出页身份。静态核对 read binding 的 dense weight 行容量、output 容量校验和唯一 AppendContributions 循环；未编译、未采样，不能声称实测耗时收益。

### AP194 LayeredBoneBlend contribution 定容（2026-09-30，已实施，本轮未编译）

- Layered Bone Blend 追加 contribution 时每层按值复制 input primitive，并重复读取 input 的 contribution 与 dense weight binding 属性；逐骨权重写入重复计算 output/input 行偏移。
- global factor 早退后缓存 input slice；primitive 绑定只读元素引用，每个 contribution 预计算两侧行偏移。weight 过滤、容量校验、`ExtendContributionPrefix`、bone mask 因子和 base/overlay 顺序不变。
- 该改动删除 Layered Bone Blend contribution 链的 primitive 拷贝、重复 binding 读取和重复行乘法；不改变 per-bone mask 权重或输出页身份。静态核对 read binding 的 dense weight 行容量、output 容量校验和唯一 AppendContributions 循环；未编译、未采样，不能声称实测耗时收益。

### AP195 Inertialization parameter 定容（2026-09-30，已实施，本轮未编译）

- Inertialization 的 residual 构造、residual 应用和 history 提交逐参数重复读取 input/output/history 的 parameter 与 availability binding；这些数据在同一方法调用内不变。
- 三段参数循环前分别缓存 input、history 或 output 的 `NativeSlice`；Inertialize 判断、residual 计算、权重应用、history 参数与 availability 写入顺序不变。
- 该改动删除 Inertialization 参数链的重复 binding 结构读取；不改变 pending/committed history 生命周期、输出页身份或 Commit/Discard。静态核对 buffer 参数布局、字段 swap 边界和三个唯一循环；diff 复核中已修正首轮缓存误置循环内与缺失 output 声明；未编译、未采样，不能声称实测耗时收益。

### AP196 Inertialization foot feature 定容（2026-09-30，已实施，本轮未编译）

- Inertialization 应用 foot feature 时分别重复读取 input 的 left/right foot binding；重写 contribution 前还把每个 primitive 按值复制到 source 局部。
- pending/active 检查通过后缓存 input left/right foot slice 和 output contribution slice；每个 primitive 绑定只读元素引用，envelope 判断、accumulator 添加顺序、pending 更新和 contribution 字段换算不变。
- 该改动删除 foot feature 周期链的重复 binding 读取和 primitive 中间拷贝；不改变 contribution count、foot 输出或输出页身份。静态核对 foot slice 单元素合同、readonly struct 字段只读和唯一 ApplyFootFeatures 循环；未编译、未采样，不能声称实测耗时收益。

### AP197 Foot Placement 输入定容（2026-09-30，已实施，本轮未编译）

- Foot Placement 构建输入时重复读取 contribution count、parameter 和 availability binding；`ResolveContributions` 再次读取 count，每个 primitive 按值传入 resolver。
- 入口缓存 count、parameter、availability 和 primitive slice；count 校验后作为唯一实参传入 void resolution。`CharacterFinalPoseContributionResolver.Resolve` 的 primitive 边界改为 `in`，Final Publication 与 Foot Placement 两个调用点共享同一只读合同。
- 该改动删除 Foot Placement 周期输入的重复 binding 读取和 primitive 拷贝；不改变 count 校验、Live source 选择、resolver 异常或输出 frame 顺序。静态核对 read binding slice 生命周期、两个唯一 Resolve 调用和方法本体不写 primitive；未编译、未采样，不能声称实测耗时收益。

### AP198 AnimationSlot bone weight 定容（2026-09-30，已实施，本轮未编译）

- Animation Slot 每根骨骼调用 `BoneOutputWeight` 时重复读取 action 的 contribution count、pose count 和 dense contribution weight binding；这些数据在同一个 `BlendPoses` 调用内不变。
- bone 循环前缓存三项并传入专用求和入口；仍逐 contribution 累加同一行权重并执行 `Clamp01`。per-bone contribution 权重、混合公式和异常时机不变。
- 该改动删除逐骨重复 binding 结构读取；不降低贡献求和的业务复杂度，也不新增缓存结果。静态核对唯一调用点、read binding 长度合同和 `NativeSlice` 生命周期；未编译、未采样，不能声称实测耗时收益。

### AP199 ActionSlot Materialize row 定容（2026-09-30，已实施，本轮未编译）

- Action Slot Source 的 `Materialize` 在写 sample、参数循环和构造三个 request buffer 时反复读取同一 row 的 clips、pose parameters、availability、offset、count 和 lease generation；参数循环还每项重复读取 input contract 的 `Parameters` 属性。
- `PrepareRow` 返回后一次性绑定同一 row 的数组、offset、count、lease generation 和 contract 参数列表。clip 写入、animated property 默认值、事件图值读取、availability 写入、buffer 长度和构造参数顺序不变。
- 该改动删除同一次 Materialize 内的重复只读结构读取；不改变 workspace 校验、lease 生命周期、request identity 或 Commit/Discard。静态核对 row 是 readonly struct、数组引用来自同一 prepared row 和唯一 `PrepareRow` 调用；未编译、未采样，不能声称实测耗时收益。

### AP200 BlendStack entry 只读引用化（2026-09-30，已实施，本轮未编译）

- `ReadEntry` 和 `GetEntryState` 从 entry 数组按值返回整份 `AnimationBlendEntryState`；三个 `GetEntryState` 正式调用点只读取 entry identity 与 source 信息，但每次都复制包含 fade clock 的状态。
- entry 数组读取和 `GetEntryState` 改为 `ref readonly` 返回，三个调用侧绑定只读引用。纯读取属性补充 `readonly` 合同，避免 readonly 接收器触发防御拷贝；内部仍按值消费 entry 的调用保持原拷贝语义。
- 该改动删除周期 stack 遍历的整份 entry 拷贝；不改变 pending/committed 选择、entry 修改入口、索引校验或异常时机。静态核对 backing entry 数组生命周期、全部 `ReadEntry` 消费点和 readonly 属性不写状态；未编译、未采样，不能声称实测耗时收益。

### AP201 ActionSlot usage 计数定容（2026-09-30，已实施，本轮未编译）

- `ReportUsage` 的 frame 循环条件每次读取 `Frames.Count`，每个 channel 匹配 frame 的 entry 循环又重复读取 `stack.EntryCount`。
- frame 视图 count 在循环前绑定一次；entry count 在每个 frame 的内层循环前绑定一次。usage 判定、Sample 早退、OutgoingHandoff 后继续查找和 `ReportSlotUsage` 顺序不变。
- 该改动删除报告循环中的重复 count 属性读取；不新增索引缓存或第二遍遍历。静态核对 `ReportSlotUsage` 只登记 playback 结果、不替换 frame 视图，也不修改 stack entry count；未编译、未采样，不能声称实测耗时收益。

### AP202 ActionSlot frame 遍历定容（2026-09-30，已实施，本轮未编译）

- `ApplyFrames` 的收集循环在 channel 过滤和插入排序时重复索引同一 frame；`PrepareRetained` 每次循环读取 entry count；`Prune` 每个待清理 source 的 frame 循环重复读取 frame view count。
- 三个循环分别定容 count；`ApplyFrames` 每个候选 frame 只按值读取一次，并把当前 command sequence 传入移位比较。排序键、插入位置、retained Materialize 和 pending 清理顺序不变。
- 该改动删除同一次候选扫描内的重复 frame 拷贝和 count 读取；不改变容量异常时机或 stack push 语义。静态核对 `Materialize` 不修改 stack entries、Prune 不修改 frame view 和 frame view 由 playback frame lease 固定；未编译、未采样，不能声称实测耗时收益。

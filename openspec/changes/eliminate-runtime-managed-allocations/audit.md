# 全项目托管分配与相关 CPU 开销审计

本文件是当前审计入口，最近更新为 2026-09-28。区分托管分配、持续 CPU 工作和首次使用成本，不把减少分配直接等同于减少耗时。本轮只读代码并整理文档，没有修改运行代码、启动 Editor/Player/服务器、编写测试或采集性能；没有新的 bytes/frame、GC 次数或耗时数据。

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

初始审计与后续实施需要分别阅读，不能由初始表推断今天的剩余数量。本轮确认 Pose 图节点集合仍有周期分配，动画计算和查询仍有重复工作；尚不能排列实测收益、报总字节数、保证全部第三方节点零分配，或宣称全项目每条调用链均已人工审计。下列编号用于追踪，不表示实施优先级。

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

### AP05 首次执行和首次状态进入仍创建实例（状态图创建策略已配置，EventGraph仍待收口）

- 证据：[NativeEventGraphRuntime.cs:91](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/BTSMTL/EventGraphs/NativeEventGraphRuntime.cs#L91) 首次 Execute 克隆/启动图并准备变量输出；[CharacterPoseNativeStateMachineSource.cs:402](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeStateMachineSource.cs#L402) 在 PrepareFrame 调用 EnsureState，未出现过的状态创建子图。
- 创建期间 [CharacterPoseNativeGraphEvaluator.cs:435](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeGraphEvaluator.cs#L435) 的可达性扫描反复访问 Connections；其 getter 每次收集、去重、排序、生成数组。此处不能统计成每个稳定帧都执行。
- 装配时预建可把首次动作成本移出游玩帧，但增加准备时间和常驻内存；按需创建减少未使用状态占用，却保留首次进入成本。准备期反射本身在用户允许边界内，审计问题是创建是否落在游戏更新中。
- Evaluator 初始化取一次连接集合，供可达性和输出绑定共用。状态图创建时机进入正式 Presentation Profile：`DuringPreparation` 在 handler Start、端口绑定完成后递归创建状态子图，`OnFirstEntry` 在首次进入时创建并保留。两种模式使用同一个 EnsureState/CreateChild 和原有 Reset/Dispose；新配置与 Corin 正式资产选择准备时创建，作者可在 Profile 切换。非法枚举报错，不存在隐式降级。
- 业务取舍：准备时创建增加角色准备耗时与常驻图内存，移除正常状态首次进入的建图工作；首次进入模式节省未用状态的图内存，但不能保证首次进入零分配。创建不推进状态时间，也不提前执行状态帧。创建失败仍沿原装配失败与 Dispose 链处理。修改配置在下次装配生效，不支持运行中悄悄切换。
- 资源仓库已有异步准备、租约和按预算淘汰闲置 ACL 组，但该配置仅控制图实例，未实现状态资源缺失时的等待/切换合同；不能称为完整流式。LOD 的更新频率、骨骼/IK裁剪也没有被此配置替代。EventGraph 的首次执行和 Reset 后重建仍需单独处理，AP05 未全部完成。

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

### AP08 通用元数据复制按容量搬运，计算节点随后覆盖（批量复制与Blend Pose已实施，其它节点待收口）

- 证据：[CharacterPoseNativeSpaceConversionHandler.cs:48](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeSpaceConversionHandler.cs#L48) 的 CopyMetadata 复制骨骼速度、参数、贡献表及贡献×骨骼权重，并清零容量尾部；Blend Pose 在调用后又计算写回其中多项。
- 输入页和输出页布局相同不表示所有字段都要先复制；可让计算节点直接写自身负责的结果，只复制继承字段。
- 业务不变的前提是消费者严格按有效数量读取。取消尾部清理须先统一这一合同；跨节点共享不可变数据需要正式页寿命支持，不能引入第二条借用路径。
- 实施结果：公共复制使用 NativeSlice 批量复制速度、参数、可用性及有效贡献/权重；贡献只复制有效数量，尾部统一清零，不传播旧页无效尾部记录。Blend Pose 已完整写回全部输出字段，因而删除其“先复制再覆盖”，只保留布局校验和输出贡献尾部清理。其它节点仍经同一 CopyMetadata 继承需要的字段，未引入共享页或绕过 Commit 的借用。

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

### AP11 诊断关闭后仍有字段采集与观察字典维护（采样拆分与字典移交已实施）

- 证据：[CharacterPresentationDomainRuntime.cs:716](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/CharacterPresentationDomainRuntime.cs#L716) 无条件构造含 lean 读取的诊断帧；[CharacterFinalPosePhysicalWriter.cs:116](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/Final/CharacterFinalPosePhysicalWriter.cs#L116) 在足部采集开关外读取骨盆/双踝 Transform 并转换空间。
- [CharacterPoseNativeGraphRuntime.cs:1069](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeGraphRuntime.cs#L1069) 每个首次求值输出写观察字典，CommitGraphOutput 再逐项复制到已提交字典，不以观察订阅为前提。
- 可沿现有订阅按需采集，但 PhysicalWrite.IsAvailable 当前参与提交校验：须把写回成功身份与诊断坐标分开，保留故障与 Commit 证明。不能整段关闭而破坏发布合同，也不能让关闭观察后返回上次残留字段。
- 实施结果：原生表现采样未订阅时不构造诊断帧，清除该帧诊断结果；发布时检查诊断帧对应当前 renderFrame，避免订阅在帧中变化后发布旧数据。节点观察仍保留原读取能力，但 Commit 改为交换工作/已提交字典并清空旧页，删除逐项复制；Discard 不替换已提交字典。
- 用户明确普通运行不做采样，采样由编译选项控制。物理写入现在返回完成身份，publication 在写入与 Commit 时核对当帧身份；足部世界坐标采集、缓存和提交复制仅在 `KK_DIAGNOSTIC_SAMPLING && KK_DIAGNOSTIC_FOOT` 编译分支存在，且仅有订阅时读取 Transform。采样包含原有根、骨盆、双踝及双脚趾字段。旧 `AnimationPhysicalBoneWriteDiagnostics` 删除，不再把诊断结构作为业务提交证明。
- 行为边界：本地姿势全量预检仍保留；普通提交不再借诊断坐标拦截父变换引起的异常世界坐标。节点观察尚无正式订阅寿命合同，本轮不加全局开关或静默关闭观察。已静态核对采样宏开关分支及旧类型全仓消费者，未执行编译。

### AP12 FlowCanvas 共享纯计算可能重复执行（取决于正式图连接）

- 证据：[PureFunctionNode.cs:26](../../../3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/Nodes/Functions/Implemented/PureFunctionNode.cs#L26) 按读取触发计算。仅在多个消费者读取同一计算结果时构成重复工作，不能假定每个图都命中。
- 可在图中显式保存确实复用的中间结果，代价是作者维护少量变量和明确写入顺序。同一帧 SetVariable 后必须能读到新值，禁止整帧缓存。
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

### AP16 动态源请求重复检查仍为平方比较（本轮续查，既有取舍）

- 证据：[CharacterPoseNativeRuntimeContracts.cs:697](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/Contracts/Pose/CharacterPoseNativeRuntimeContracts.cs#L697)，每条请求与所有前序请求比较 ScopeInstanceId/NodeId/SourceId。子图请求汇总到父图后，还会经过父图 demand 校验。
- 这是历史 4.2.8 删除每帧 HashSet 构造时主动选择的实现，已经没有原构造分配。与 AP01 不同，这里的活动源随状态和过渡变化，不能简单移到装配时查一次。
- 保留顺序比较：活动源少时结构简单、无额外工作集合；改用实例持有且按正式容量准备的去重集合：源多或嵌套深时减少比较，但增加常驻内存、清空与哈希成本。两种都必须保持原请求顺序、完整三元身份和重复报错；没有规模与耗时数据前不替用户选方案。

### AP17 惯性包络反复计算固定曲线端点导数（续查并实施，未实跑）

- 证据：CharacterPoseNativeInertializationHandler.EvaluateEnvelope 对每根骨骼以及参数/脚部包络重复计算同一条编译曲线在 0 和 1 的导数；曲线由构造阶段 CompileCurve 固定。
- 实施结果：两个端点导数在构造阶段计算并持有，逐帧仍计算当前 normalized 对应的曲线值和导数。曲线公式、计算项顺序和过渡时间不变，减少的是固定端点求值；没有新增运行分配，也没有缓存变化中的当前采样值。

## 可靠性问题独立保留

- 保存后恢复校验、变量 ID 与名称解析统一，解决的是配置看似存在却未生效，不作为 CPU 优化的完成条件混入上述条目。
- Corin Lean 实施说明已经记录个别初始化绑定及常量恢复修复；这些修复不等于通用 authoring/变量身份缺口全部收口。
- AP05 的创建策略、AP07 的采样裁剪、AP08 的页共享需要保持正式生命周期及业务合同，不设临时旁路。AP11 的提交证明已按用户的采样隔离要求拆分。

## 后续证据范围

当前已覆盖 EventGraph → Pose 输入 → 图/子图准备 → 混合/惯性/变换 → ACL 输入设置 → Final Pose 写回，以及足部支撑选择与动作来源解析。没有新增性能采集链。

仍未确定 ACL native 解压、Playable 求值、FBBIK、世界物理查询各自耗时、线程等待、活动角色/源数量和采样开关差异。原生插件内存、托管 GC、内存带宽和引擎调用次数分别记录；不得凭源码循环数量给出毫秒收益，也不把非当前内容使用的分支当作已命中热点。

## 2026-09-28 性能优化实施

用户已授权开始性能优化。上文未实施条目继续作为候选，标明“已实施”的条目不再计为未修复；编译、运行和耗时证据分别记录，不用源码修改代替验证结果。

Center 改动：`动画链固定绑定与重复工作优化`，change_id=`f2e964ebf61c4cd3a5300db396ad91a5`。修改前正式 compile 返回 `WorkspaceEditorInUse`，没有产生 RunId，也没有启动 Unity。随后用户明确“不用你起进程”，本轮不再启动 Unity、编译或采样进程，不建立替代验证路径。仅做代码与差异检查；不新增测试，运行结果和性能收益未验证。

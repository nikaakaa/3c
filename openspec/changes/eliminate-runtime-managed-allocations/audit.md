# 全项目托管分配审计

本轮为静态覆盖与重点调用链审计，不是 Unity Player/服务端实测。未启动 Editor、Player、服务器或采集器，没有 bytes/frame、GC 次数和停顿数据。可确认源码中的对象构造及调用边界，不能确认所有候选在当前场景都执行。

## 范围与可复查依据

扫描 `rg --files --hidden` 在 3cDemo 与 Tools 返回的 C# 源码，排除 Library、Temp、obj、bin、.git 并遵循仓库忽略规则。忽略的第三方源码、Packages 缓存、仅 DLL/native 插件和未加载产品不冒充已逐行审计。目录名 Runtime 内仍可能有 Editor 条件代码。统计中的候选不是缺陷数量。

共 5451 个源码文件、2180 个候选文件、89 个归属分组。完整分组计数、候选行和当时文件 SHA256 在 [source-inventory.json](source-inventory.json)。源码基线 HEAD 为 `cdb06993e86e421951edb84ae4ac4cd15fd4c75f`，同时包含当时未提交工作区内容，以各文件 hash 区分。

## 已核对的代表性路径

本节 G01–G21 保留初始审计结论；后续已实施的小步见文末进展，不把初始候选数量当作当前未修复数量。

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

已能证明常态模拟链有重复托管分配，并且表现、回滚、网络等领域各自存在分配入口。尚不能排列各项实测收益、报总字节数、保证全部正式第三方节点零分配，或宣称全项目每条调用链均已人工审计。proposal覆盖这些范围，实施逐域沿正式入口完成证据闭合。

## 2026-09-20 相机独立小步实施

对应 tasks.md 的 4.4，下面仅记录已提交的分配入口清理，不代表整个 4.4 或相机整链完成。此次未改 Timeline、共享动作播放、事务和回滚生命周期。

源码位置以 `3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/` 为基准。

| 提交 | 源码入口 | 修改前 | 修改后与业务边界 |
| --- | --- | --- | --- |
| `0a1ed8e82` | Character/Camera/Solver/CameraPipelineResolvers.cs：FirstKey | 每次传入三／四个键都会创建 params 数组 | 直接比较参数；仍按原顺序取首个非空键，全部为空返回空字符串。每个目标槽原有两次调用，选中有效请求后另有两次调用 |
| `fe0162499` | 同文件：CameraTargetBindingResolver.Resolve | 将复用的 List 经 IEnumerable 遍历，产生枚举器装箱 | 输入改为 IReadOnlyList，按下标读取；唯一运行调用者仍传原 List，不转换、不复制，保持请求顺序与优先级规则 |
| `f3854c086` | CameraContracts/Projection/CameraFrameInput.cs；CameraContracts/CameraEnvironmentContracts.cs；Character/Camera/Solver/CharacterCameraSequenceTransition.cs | 五处运行期 Enum.IsDefined(Type, object) 校验装箱 | 直接比较现有合法成员；保留原异常类型、抛出位置及合法集合，不改变时间增量或碰撞计算 |
| `b96234f54` | Character/Camera/Solver/CharacterCameraFramePlanner.cs：SampleTrack | 空轨道／多轨道采样创建 CameraTrackOrbitPayload 引用对象 | 以具名值元组返回高度、半径；保留空／单／多轨道分支和插值顺序，作者 Payload 类型与资产格式不变 |
| `affbf9883` | Character/Camera/Solver/CameraPipelineResolvers.cs：TryResolvePoint | 实时绑定缺失或失效时先生成错误字符串，即使随后命中快照 | 先按原顺序查实时绑定和快照，仅全部失败才生成原错误文本；成功路径不再制造丢弃的错误字符串 |
| `53f2e3a47` | CameraContracts/CameraEnvironmentContracts.cs；Character/Camera/Runtime/UnityCameraEnvironmentQuery.cs；Character/Camera/Solver/CameraEnvironmentConstraintSolver.cs | 命中 Collider 后将实例编号转成字符串，沿查询和碰撞结果传递 | 全链统一为 int ColliderInstanceId，无命中为 0；删除旧字符串属性及转换。已查项目源码消费者，无相机编号的文本解析、持久化和网络协议消费者；物理求解不变 |

### 已做检查与证据限制

- 初始两笔完成修改文件的独立编译；后四笔沿 Unity 现有编译响应文件引用，独立编译完整 ThirdPersonCamera.Contracts，并使用新合同编译 Character/Camera 运行目录。产物位于系统临时目录，不写入 Unity Library 或 Assets。
- 编译通过；相机目录仍有既有 `CameraShotRigBinding.m_VirtualCamera` 序列化字段 CS0649 警告。各笔提交前的 `git diff --check` 通过。
- 上述检查不是完整项目编译或 Player 运行证明。未新增测试、未主动刷新 Editor、未启动性能采样；没有实测 bytes/frame、峰值占用和 GC 停顿数据。
- 源码可以确认这些具体数组、装箱、临时对象和字符串构造入口已删除，不能据此推断第三方相机内部及整条表现链无分配。

### 仍未完成

- CameraEffectRuntimeStateStore.Add 仍创建效果状态对象；效果请求、可见状态、输出贡献和完成事件去重集合仍需明确准备容量与存活上限。
- 效果停止、撤销和完成事件保留与并行 Timeline 的事件生命周期相交；不能随意清空、复用或引入无界缓存。
- Input、Behavior Designer、第三方相机及其它模块仍按原任务清单处理，4.4 保持未完成。

## 2026-09-20 GameplayEffect 属性捕获清单准备

对应 tasks.md 的 2.6。本节与代码同步提交，具体版本可由本文件及以下源码的 Git 历史追溯。

- 原调用：公共 `GameplayEffectApplicationAdmissionRuntime.TryPrepare` 分别请求来源、目标属性清单；Fixed／Float32 的 `GameplayEffectTarget.CollectSnapshotAttributes` 每次扫描效果定义并创建 SortedSet，随后经 IEnumerable 遍历。集合节点及枚举器属于重复分配。
- 新调用：两数值域的 `PortableEffectDefinition` 构造时生成 `SourceSnapshotAttributes` 和 `TargetSnapshotAttributes`，由既有 RuntimeCatalog 持有；AdmissionPort 返回 IReadOnlyList，公共 TryPrepare 按下标读取。删除两套 Target 中的原运行期收集函数。
- 输入仍是效果定义的时长、周期、Modifier、属性要求和 Execution mutation。沿用原 Ordinal 排序、去重及来源／目标筛选规则，没有合并数值计算或改变属性读取顺序。
- 输出只保存“需要捕获哪些属性”的名称，不保存属性值。每次效果应用仍读取本次上下文和当前状态，缺少属性的失败结果、独立 Spec 和事务边界不变。
- 清单容量由已加载定义精确产生，不引入运行期扩容或任意上限。两个字符串引用数组随目录存活，换取取消每次应用的集合重建；该准备阶段允许分配，未将战斗内申请伪装成准备阶段。
- 代码入口：`Simulation/Core/Execution/GameplayEffectControlContracts.cs`，以及两数值域 Execution 下的 `GameplayEffectRuntimeCatalog`、`GameplayEffectAdmissionPort`、`GameplayEffectTarget` 文件。
- Fixed 与 Float32 portable 工程分别以 `dotnet build --no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译通过，均为零警告、零错误；每次构建后执行 `dotnet build-server shutdown`。未新增测试、未主动刷新 Unity，未做 Player 分配采样。
- 效果 Spec 创建、属性快照字典、标签复制和事务克隆仍有分配，不由本小步宣称完成。

## 2026-09-20 GameplayEffect 参数和标签查询

对应 tasks.md 的 2.7，与代码同笔提交。

- 参数入口：两数值域 AdmissionPort 原通过带 Ordinal 比较器的 LINQ Contains 查声明数组；现在公共 GameplayEffectApplicationAdmissionRuntime 通过已有 RequiredParameterCount／RequiredParameterId 按下标逐项比较。移除接口和两后端的 DeclaresParameter 实现，不增加第二套参数集合，未声明参数仍走原失败结果。
- 标签入口：Fixed／Float32 RuntimeCatalog.Matches 原先 ToArray 复制输入，随后通过捕获 query、下标和 required 的多层 LINQ Any 查询；现在要求 IReadOnlyList 并顺序遍历。已核对调用者均提供已有列表或数组，不新增转换；空输入、All／Any／None 的顺序及短路位置保持一致。
- 父链入口：IsTagOrParent 原每次创建 HashSet 记录访问节点。RuntimeCatalog 构造先 ReadBinding 再 ValidateClosure，后者已经拒绝缺失父引用和循环；父映射没有正式运行期修改入口。因此保留准备期校验，删除重复运行期集合，仍按原链条逐级比较，不使用结果缓存。
- 业务取舍：查询直接读取调用期间不变的列表，省去每次独立副本；目录合法性在准备入口统一保证，未加入运行期修补或兼容分支。该改动不改变实际 OwnedTags 的收集和状态快照寿命。
- Fixed／Float32 portable 构建均零警告、零错误，沿用禁用构建服务器及共享编译参数并在每次结束后 shutdown；diff 空白检查通过。无新增测试、无 Unity 主动刷新、无 Player 分配采样。
- 剩余：SimulationGameplayEffectState.CopyOwnedTags 仍会整理标签集合；效果 Spec、快照、标签复制与事务分配仍未完成，不能把查询内部入口清理视为整个效果运行无分配。

## 2026-09-20 GameplayEffect 标签整理树节点清理

对应 tasks.md 的 2.8，与代码同步提交。

- Fixed／Float32 的 GameplayEffectExecutionScratch.OwnedTagSet 原为 SortedSet。每次 CopyOwnedTags 先 Clear 再逐个 Add，即使集合实例复用，树节点仍需重新创建。
- 改为使用 Ordinal HashSet 去重，再填入既有 OwnedTags 列表并按 Ordinal 排序；排序 Comparison 在类型初始化时保存，不在每次调用转换委托。返回内容、唯一性和顺序保持不变，没有改变借用列表或效果快照的寿命。
- 两数值域的 SimulationGameplayEffectState.HasTag 及 GameplayEffectTarget 对已提交状态的 HasTag 改为 IReadOnlyList 下标读取，取消接口 foreach；保留查到即返回的顺序。
- 取舍：散列表保留桶／条目数组，输出列表需一次排序；相较清空再构造排序树，取消每次插入的节点对象。具体 CPU 和常驻容量变化未经采样，不宣称更快或整链 0 GC。
- 已知未完成：scratch 当前仍由 CharacterEvaluationRuntime 每步创建；HashSet／List 容量增长、m_TagSources 的 SortedDictionary 遍历，以及无 scratch 的既有调用仍存在分配来源。本步没有为避开这些问题新增另一存储入口。
- Fixed 与 Float32 portable 编译均零警告、零错误，diff 空白检查通过；构建参数与结束 shutdown 同前。未新增测试、未主动刷新 Unity、未做 Player 采样。

## 2026-09-20 执行来源和效果移除枚举校验

对应 tasks.md 的 2.9，与代码同步提交。

- SimulationExecutionSource 的构造入口由两数值域 Action、Motion、Blackboard、Domain 等运行代码调用；原 Enum.IsDefined(Type, object) 使值类型 kind 装箱。现直接接受 SkillOperation／CharacterControl 两个合法成员，仍对其它值抛出原 ArgumentOutOfRangeException，其余身份完整性校验不动。
- Fixed／Float32 GameplayEffectOperationRuntime.Remove 原先对转换后的选择器调用 Enum.IsDefined。现直接比较 Handle、EffectId、SourceActor、EffectTagQuery，保留转换前的 byte 范围检查及原 InvalidOperationException，不改变请求、删除对象或输出事件。
- 规范化函数对已规范化身份直接返回原字符串，本次没有为此引入缓存或调整身份格式；执行来源的 codec 和输出协议不变。
- Fixed／Float32 portable 构建均零警告、零错误，diff 空白检查通过；每次构建按规定禁用服务器和共享编译并结束 shutdown。未新增测试、未主动刷新 Unity、未做 Player 分配采样；此结论只覆盖三个装箱入口。

## 2026-09-20 Canonical Span 写入中转数组清理

对应 tasks.md 的 5.6，与代码同步提交。

- `Simulation/Core/Serialization/CanonicalData.cs` 的 WriteBytes(ReadOnlySpan<byte>) 原先写入长度前缀，再租 ArrayPool 字节数组、复制 Span、写流并归还。Fixed／Float32 的世界状态编码通过此入口写 SolverStatePayload.Span。
- 改为写入相同长度前缀后直接调用现有 MemoryStream.Write(ReadOnlySpan<byte>)，空输入仍只写前缀。不修改 byte[] 重载、字符串编码、ToArray、哈希、writer 所有权或消息与快照结果寿命，不创建新序列化入口。
- 原数组池命中时不一定产生托管分配，因此不能按每次调用计为已消除一个数组；本次确实删除中转租借、复制和池未命中时的临时数组申请需求。MemoryStream 扩容及最终输出数组仍存在，未宣称序列化整链 0 GC。
- portable Core 编译零警告零错误，按规定禁用构建服务器与共享编译并结束 shutdown；沿当前 Unity Core 响应文件的引用单独编译完整 Core 也通过，产物位于系统临时目录。diff 空白检查通过，未新增测试或做 Player 采样。
- 此时默认 Editor.log 来自其它 Unity 项目；3C 实例 e852139597e42532 的只读状态检查经历超时和断开，观察到的 csc／bee 编译进程结束后才修改源码。本次未主动刷新、重启或控制编辑器，独立编译不代表共享 Editor 当前验证通过。

## 2026-09-20 回滚输入 canonical 比较副本清理

对应 tasks.md 的 5.7，与代码同步提交。

- RollbackInputCodec.ReadInput／ReadBundle 原在解码及 RequireComplete 后调用返回 byte[] 的公开写入方法，仅为 BytesEqual 比较生成一份完整 ToArray 临时副本。
- CanonicalWriter.ContentEquals 先比较长度，再用固定 256 字节栈缓冲读取流并逐段比较，在 finally 恢复原 Position。此块大小只是比较的分段大小，不是协议容量或业务上限；支持原来可传入的 MemoryStream，不要求暴露内部数组，不租托管缓冲或复制完整 payload。
- 两个读取入口继续完整重新编码并比较所有字节，仍拒绝非 canonical 输入和尾部数据；输入束编码抽为同一私有 WriteBundle(writer, bundle)，公开写入和校验共用它。删除原 BytesEqual 方法，没有第二套编码器、放宽校验或修改协议版本。
- 每次成功到达比较阶段不再生成该份完整字节数组；writer、流容量、解码对象、哈希和公开写入的独立结果数组仍存在。此次未修改回滚历史和异步发送的存储归属。
- 回滚 portable 工程连带 Core／Fixed 编译零警告零错误，构建结束 shutdown；沿当前 Unity 引用独立编译 Core 后，再用该 Core 编译 DeterministicRollback 也通过。diff 空白检查通过，未新增测试或做 Player 采样。
- 编辑前显式读取 3C 实例 e852139597e42532 的状态，确认为 idle、非 Play、非编译、无待重载；未主动刷新或控制 Editor。

## 2026-09-20 回滚封套校验与输入束哈希副本清理

对应 tasks.md 的 5.8，与代码同步提交。

- RollbackProtocolCodec.Read 和 ReadCanonicalPayload 原在完整重新编码后 ToArray，再调用本类 BytesEqual。现公开写入与校验共用 WriteEnvelope／WriteCanonicalPayload(writer, payload)，校验调用既有 ContentEquals，不再生成仅用于比较的完整副本，并删除旧 BytesEqual。
- RollbackInputCodec.ComputeBundleHash 原把 WriteBundle 返回的新数组交给 SimulationCanonicalPayloadHash.Compute；现同一 WriteBundle(writer, bundle) 写完后调用 writer.ComputeHash。已核对两哈希实现均是原始完整 payload 的 SHA-256 和小写十六进制 StableHash，无额外前缀、字段或版本差异。
- 公共写入 API、重新编码校验、RequireComplete、协议版本、字段顺序、空参数及非 canonical 报错均保留；不改变正式结果的独立寿命或可靠事件存储。
- 此处减少三类入口各自生成的一份完整临时数组，不代表 writer、哈希对象或嵌套 WriteInput／WriteBundle 的中间数组已消除；嵌套数组、解码对象和发送容量仍待治理。
- 回滚 portable 工程及其 Core／Fixed 依赖编译零警告零错误，按规定禁用构建服务器与共享编译并结束 shutdown，diff 空白检查通过。未新增测试、未主动刷新 Unity、未做 Player 分配或性能采样。

## 2026-09-20 回滚嵌套输入直接编码

对应 tasks.md 的 5.9，与代码同步提交。

- 原 RollbackProtocolCodec 在输入批次、转发输入、canonical bundle 和确认批次四类入口调用 `writer.WriteBytes(RollbackInputCodec.WriteInput/WriteBundle(...))`。每个子项先建立独立 writer／MemoryStream 并 ToArray，再复制到外层。
- CanonicalWriter 增加 BeginLengthPrefixedBlock／EndLengthPrefixedBlock：记录当前 Position 并预留四字节，写完后以实际位置差计算 int 长度，沿 WriteInt32 回填，再恢复结束位置。不用流总 Length 代替 Position，因此保留原可传入 MemoryStream 的相对位置语义；不分配作用域对象或委托。
- RollbackInputCodec 的两个内部长度前缀写入入口调用同一私有 WriteInput／WriteBundle；四类外层调用统一使用该入口。Magic、Version、字段顺序和小端长度编码均沿原实现，公开返回独立数组的方法保留其真实消费者。
- 删除嵌套子项临时 writer、流、primitive buffer 和最终 ToArray 数组，不改变外层独立结果、消息历史或异步发送寿命；失败时不返回部分包。读取侧 ReadBytes 中间数组、快照响应副本、外层 writer 容量和发送存储仍未完成。
- 回滚 portable 及 Core／Fixed 依赖编译零警告零错误，按规定构建参数及结束 shutdown，diff 空白检查通过。已查当前 portable Tests 工程不引用回滚工程且未找到对应 codec 用例，未新增测试、未宣称协议运行或 Player 性能已验证。

## 2026-09-20 回滚嵌套输入同步借用解码

对应 tasks.md 的 5.10，与代码同步提交。

- CanonicalReader 支持 ArraySegment 构造和 ReadBytesSegment。byte[] 构造沿同一初始化链；片段 reader 保存绝对 Offset 和 End，Remaining 改为 End 减 Offset，现有所有基础读取、字符串和字节读取仍先 Require，因此不能越过子消息边界。
- ReadBytesSegment 沿原负长度和剩余长度检查取得原包片段，并推进父 reader；不创建字节数组。ReadBytes／ReadRawBytes 仍为真实需要独立持有字节的消费者提供复制语义，不是兼容路径。
- RollbackInputCodec 的公开 byte[] 入口统一进入内部片段解码；内部仍执行原 ReadInput／ReadBundle、RequireComplete 和完整重新编码，只把 ContentEquals 的输入限定为 bytes.AsSpan() 的实际片段。
- RollbackProtocolCodec 的 canonical bundle、输入批次、转发输入和确认批次四类调用切换为片段入口。已核对解码结果只保存独立字符串、数值和输入集合，不保存 reader 或原包引用；借用范围限于同步调用。快照响应字节的读取与持有没有改动。
- 每个嵌套子项少一次完整 byte[] 创建与复制；reader 对象、解码输入集合、重新编码 writer、哈希及长期结果存储仍有分配。
- 回滚 portable 连带 Core／Fixed 编译零警告零错误；Unity 当前引用下独立编译 Core，再用新 Core 编译回滚程序集也通过。diff 空白检查通过，按规定构建后 shutdown；未新增测试、未主动刷新 Unity、未做 Player 采样或宣称运行验证完成。

## 2026-09-20 回滚快照响应中转副本清理

对应 tasks.md 的 5.11，与代码同步提交。

- RollbackSnapshotResponse 构造参数改为 ReadOnlySpan<byte>，检查非空后 ToArray 一次保存私有存储。原 byte[] 调用者可直接提供 Span，未新增兼容构造重载；输入 byte[] 为 null 时对应空 Span，仍抛原不完整响应 ArgumentException。
- 新 SnapshotBytes 只读 Span 供同步编码使用，RollbackProtocolCodec 不再先调用 CopySnapshotBytes 克隆再写流。此视图不能作为普通字段跨异步保存，响应对象仍是字节存储所有者。
- 解码端从 ReadBytesSegment 直接把片段 Span 传入响应构造，不再先 ReadBytes 复制一份再 Clone。响应构造仍取得一份独立数组，不把网络接收缓冲当作长期快照存储。
- 已核对另一个 CopySnapshotBytes 消费者位于 RollbackEndpointRuntimeBridge.ReceiveSnapshotResponse。它仍向 SnapshotCodec.Read 传独立副本；本小步保留该真实入口，不修改恢复快照、哈希校验或状态生命周期。
- Endpoint portable 工程连带 Core／Fixed／Rollback 编译零警告零错误；Unity 当前引用下依次编译 Core、Rollback、Endpoint 也通过。diff 空白检查通过，按规定构建结束 shutdown。未新增测试、未主动刷新 Unity、未做 Player 采样；最终消息数组与快照自身数组仍有分配。

## 2026-09-20 Datagram 分片与准备容量清理

对应 tasks.md 的 5.12，与代码同步提交。

- RollbackDatagramPacket 构造改接收 ReadOnlySpan，保留原数据报身份、确认包和分片合法性校验，随后复制一次拥有 payload；构造和 ReadKind 对 Payload／Acknowledgement 直接比较，取消两处枚举装箱。
- Channel.Send 直接把编码消息的片段 Span 传给 packet，删除临时 fragment 数组及 BlockCopy；codec.Read 直接传 reader 的字节片段，删除 ReadBytes 再 Clone 的中间副本。codec.Write 读取 packet.Payload 只读 Span，删除只为编码生成的 CopyPayload 副本。
- packet 不持有原发送或接收缓冲；重组 FragmentAssembly 仍通过 CopyPayload 保存独立数据，重传队列仍持有 packet。没有改变完成、超时、重复片段、发送线程和队列所有权。
- Channel 的定义、SessionId、LocalPeerId 和 MaximumDatagramBytes 为只读，故构造时沿现有 codec.GetMaximumFragmentPayloadBytes 计算一次并保存。FitsSingleDatagram／Send 直接读取该准备结果，删除运行期重复包头 writer。codec 计算容量时改读 writer.Length，不为取长度 ToArray；包头字段与 UTF-8 编码仍只有一套。
- 业务变化：身份字段占满 MTU 的非法配置现在创建通道即失败，而不是等首次发送；没有新增默认容量或改变合法配置的分片大小。保留现有 MaximumFragmentsPerMessage、队列和 MTU 限制。
- Endpoint portable 及 Core／Fixed／Rollback 依赖编译零警告零错误，按规定参数构建并结束 shutdown，diff 空白检查通过。未新增测试、未主动刷新 Unity、未做 Player 或网络实测。最终数据报数组、packet、重组和发送存储仍有分配。

## 2026-09-20 Datagram 通道容器按正式上限准备

对应 tasks.md 的 5.13，与代码同步提交。

- 既有 MaximumQueuedMessages 为 N，Send 对可靠待确认消息、Process 对重组和接收队列均在插入前检查 N。将这两个 Dictionary 和一个 Queue 的初始容量移到通道构造阶段按 N 分配，取消合法峰值内的运行期扩容。
- 既有完成消息历史保留 2N 条，但 RememberCompleted 先 Add／Enqueue 再淘汰，临时峰值为 2N＋1。因此完成 HashSet 和 Queue 按 2N＋1 准备，正式保留上限仍为 2N，未改变去重窗口及淘汰顺序；乘法和加法均 checked，超出可表示范围在准备阶段失败。
- Pump 直接遍历具体 Dictionary 的 KeyValuePair，再取 Value，避免首次访问 Values 创建集合视图；遍历顺序及 NextSendTimestamp 更新仍沿原字典和重传规则。
- 业务取舍：每条 peer 通道提前持有声明容量，降低合法峰值第一次出现时的存储增长；未增加任意配置或业务上限。Dictionary／HashSet 实际桶容量可按其实现取整，需后续采样常驻内存，不用消息容量冒充字节占用。
- 只覆盖这些容器的存储，PendingReliableMessage、FragmentAssembly、packet、payload、最终数组和 ConcurrentQueue 段等仍有分配，不能宣称传输端到端 0 GC。
- Endpoint portable 全依赖零警告零错误，Unity 当前引用下 Endpoint 独立编译通过，diff 空白检查通过；按规定构建并结束 shutdown，未新增测试、未主动刷新 Editor、未做网络或 Player 实测。

## 2026-09-20 单数据报长度判断副本清理

对应 tasks.md 的 5.14，与代码同步提交。

- RollbackDatagramChannel.FitsSingleDatagram 原调用 Encode(...).Length，完整输出数组只用于读长度；RollbackPeerEndpoint 在发送冗余输入批次时会调用它，并按 MTU 不断裁掉最旧帧。
- RollbackProtocolCodec.GetEncodedLength 沿同一 WriteEnvelope 执行完整编码后读取 writer.Length，不执行 ToArray；Channel 使用相同 SessionId、PeerId、当前消息序号和 payload 调用它。没有额外尺寸公式、payload 缓存或序号预占，所有消息种类仍由原 WritePayload 分派。
- 每次候选批次长度判断少一份完整数组，但 envelope、writer、流容量和编码 CPU 仍存在；不是无分配测长器。实际 Send 的独立结果与后续分片寿命不变，候选裁剪和过大单帧报错不变。
- Endpoint portable 与 Core／Fixed／Rollback 依赖编译零警告零错误，diff 空白检查通过，按规定构建并结束 shutdown；未新增测试、未主动刷新 Unity、未做 Player 或网络实测。

## 2026-09-20 peer 冗余发包历史与候选存储准备

对应 tasks.md 的 5.15，与代码同步提交。

- RollbackPeerEndpoint 的本地输入冗余历史已有 inputRedundancyCount=N，且不大于 MaximumQueuedMessages。原 SortedDictionary 每个新 tick 分配树节点，获取最旧 tick 还通过 Keys 枚举；现用 SortedList 并在构造时准备 N＋1 容量，保留加入后淘汰的临时峰值，按 RemoveAt(0) 删除同一个最旧 tick，删除 FirstInputTick 辅助函数。
- 构造阶段取得一次 Values 只读来源视图，准备 N 容量的 m_InputBatchFrames。每次发送按下标填入，仍用 RollbackActorInputBatch 构造、精确测长和逐个裁掉最旧帧；finally 清空工作引用，Dispose 同步清理。
- 已核对 RollbackActorInputBatch 构造会建立自己的 List 和只读包装，所以复用候选工作列表不会覆盖已经构造的批次。重复 tick 的身份校验、连续 tick 校验、超 MTU 单帧失败和发送顺序保持原样。
- 取舍：有序数组在插入／移除时可能搬移引用，换取不再创建树节点和临时键枚举器；没有实测 CPU 收益。容量完全来自正式 N，未新增任意默认上限，不修改 canonical bundle／confirmation 容器或模拟回滚历史。
- 通道构造现在承担正式容量准备，可能在首次发送前失败；peer 在创建 Endpoint 后构造 Channel 的步骤增加异常清理，调用既有 Endpoint.Dispose 释放 socket／线程后重新抛出，不增加重试或替代路径。
- Endpoint portable 全依赖编译零警告零错误，diff 空白检查通过，按规定构建并结束 shutdown；未新增测试、未主动刷新 Unity、未做网络或 Player 实测。批次及其独立列表、编码和消息对象仍有分配。

## 2026-09-20 资源快照中间集合清理

对应 tasks.md 的 6.5，与代码同步提交。

- 核对产品快照接口与 ProductShellViewController 后，当前界面通过 Current 和 Changed 读取资源、启动和检查点状态；未找到产品 History 的正式读取调用。因此没有把 History.ToArray 认定为已发生的每帧热点，也没有改变其独立历史结果语义，6.1 仍未完成。
- ProductResourceRuntime.PublishSnapshot 原先获取一次对象池数组用于计数，又在 GetAssetPoolMetrics 内重新查询一次。TEngine 的 GetAllObjectPools(false) 每次按当前池数创建数组；现在把同一份数组传入静态指标计算函数，池总数和 Asset Pool 指标来自同一次同步查询。
- 资源维护的前后指标仍分别重新查询，在异步卸载两侧保留各自采集时点；未跨 await 缓存对象池数据。所有 GetAssetPoolMetrics 调用均迁移到显式输入数组，没有增加另一套查询实现。
- 作用域按当前数量直接创建最终数组并填充，沿原 Id.Value 排序；标签用 HashSet.CopyTo 填充最终数组，再按原 Ordinal 排序。删除两个临时 List、其存储和 ToArray 复制；ResourceRuntimeSnapshot 仍分别持有本次数组，旧快照不会被后续发布覆盖。
- 业务边界：这是加载、租用、实例与维护事件触发的快照发布清理，未证明每帧发生。快照对象、作用域对象、最终数组、对象池首次查询数组和历史 getter 副本仍会分配，没有宣称资源链 0 GC。
- 以现有 GameLogic.rsp 的 Unity 引用独立编译完整 GameLogic，退出码 0，未输出诊断；产物仅写系统临时目录。未新增测试、未采样 Player，也未主动刷新或控制共享 Editor。编辑前 Unity MCP 状态查询返回 503，随后本机进程检查未发现 csc 或 bee 编译进程；这不等同于共享 Editor 验证通过。

## 2026-09-20 资源池统计工作列表复用

对应 tasks.md 的 6.6，与代码同步提交，继续清理 6.5 保留的对象池查询数组。

- ProductResourceRuntime 通过 IObjectPoolModule 已有 GetAllObjectPools(List<ObjectPoolBase>) 填充自身工作列表。GetAssetPoolMetrics 同步返回池数量、资源对象数和可释放数，快照发布与维护前后都调用此唯一入口，删除产品资源运行时中的返回数组查询调用。
- 列表在构造时按 IObjectPoolModule.Count 准备，finally 清空池引用但保留数组容量；不跨 await、Changed 通知或快照寿命保存池引用。维护前后仍各自查询，未缓存统计值或改变采集时点。
- 已核对 TEngine 实现：列表填充清空后遍历实际池集合；正式 Asset Pool 由 ResourceModule 为私有 AssetObject 创建，AssetObject 未重写 CustomCanReleaseFlag，沿 ObjectBase 的常量 true；本统计链不会调用资源业务通知。历史快照仍只持有数值结果。
- 取舍：运行时多保留一个工作列表的容量，取消每次查询新建完整数组。初始池数不是后续池数的正式上限，动态新增池仍可能触发扩容；CanReleaseCount 内部工作集合增长也未在此步治理，不能宣称资源统计全程无分配。
- 资源作用域释放存在逐项通知，未把原复制清单直接改成共享可变视图；UnityFixedCharacterInputAdapter 已有并行未提交修改，保持原现场。本步不修改这些生命周期或输入链。
- 当前 Unity 引用下独立编译完整 GameLogic 退出码 0、无诊断，产物位于系统临时目录，diff 空白检查通过。编辑前未发现 csc／bee 编译进程；MCP 仍返回 503，未控制或刷新 Editor。未新增测试，未做 Player 分配采样。

## 2026-09-20 产品诊断历史队列容量准备

对应 tasks.md 的 6.7，与代码同步提交。

- ProductStartupCoordinator 从正式 RuntimeDefinition.DiagnosticsHistoryCapacity 取得 N，传给 ProductResourceRuntime、ProductRuntimeSnapshotStore、ProductDiagnosticsStore 和 ProductFaultLab。四者原先创建零容量 Queue，在记录增加时扩容。
- 四个构造入口通过原有正数检查后，按 checked(N＋1) 创建各自队列。发布逻辑原先先 Enqueue 再在超出 N 时 Dequeue，因此瞬时峰值是 N＋1；保持该顺序及 Changed 通知时机，不修改公开历史接口、最终保留数量、记录对象和旧快照寿命。
- 取舍：提前占用配置容量的引用数组，消除窗口填充过程和满窗口写入的队列扩容。极端容量导致的整数溢出或内存不足在准备入口直接失败，不新增任意上限或备用存储。History.ToArray 与各类记录对象分配仍存在；故障事件属于显式诊断行为，未称为每帧热点。
- 以当前 Unity GameLogic.rsp 引用独立编译完整 GameLogic，退出码 0、无诊断，产物仅写系统临时目录；diff 空白检查通过。编辑前本机未发现 csc／bee 编译进程，未控制或刷新共享 Unity，未新增测试，未做 Player 分配采样。

## 2026-09-20 正式性能控制连接收发分配清理

对应 tasks.md 的 7.5，与代码同步提交。

- ThirdPersonPerformanceCaptureAgent 使用 PerformanceLoopbackClient 与既有控制器通信。其 ReadLoop／WriteLoop 已分别运行在专用后台线程，原调用 ReadLineAsync／WriteAsync 后立即 GetAwaiter().GetResult；现在线程内直接 ReadLine／Write，删除每次消息的异步操作任务入口，不把阻塞 IO 移到 Unity 主线程。
- 发送原先 Encoding.UTF8.GetBytes(message + 换行) 每次拼接字符串并创建字节数组。现构造时保存单一发送缓冲，按 UTF-8.GetMaxByteCount(max(256, hello.Length)) 加一准备。256 来自既有普通命令长度限制；HELLO 直接入队，故必须按实际握手字符串长度覆盖，不能假定同样受 256 限制。
- 唯一写线程编码消息到缓冲后追加单字节 LF，并在同步 Write 完成后复用；保留编码器默认替换行为、无 BOM、消息先后、CompleteWrite 时机以及原错误发布路径。读取结果仍是独立字符串，控制器协议和收发队列消费者均不变，没有新增控制面。
- 取舍：连接常驻一个准备缓冲，避免每条发送消息的拼接和数组；线程原本就在等待异步操作完成，现直接承担同步 IO。连接建立阶段的 ConnectAsync、入站字符串、出站消息构造、队列扩容和故障文本仍会分配。控制消息不是每帧采样数据，不能把此改动描述为每帧消除固定分配量。
- 当前 Unity 引用下独立编译完整 ThirdPersonPerformance.Runtime，退出码 0、无诊断，产物位于系统临时目录；diff 空白检查通过。编辑前本机未发现 csc／bee 编译进程，未新增测试、未刷新或控制 Editor，未进行控制器联调或 Player 采样。

## 2026-09-20 Datagram 接收缓冲同步借用解码

对应 tasks.md 的 5.16，与代码同步提交。

- RollbackDatagramEndpoint.ReceiveLoop 已有单线程复用的 MTU＋1 接收缓冲，但每次有效长度接收后仍创建 received 大小的数组并 BlockCopy，随后解码。现直接将 buffer 的 [0, received) ArraySegment 传给 RollbackDatagramCodec.Read，删除整包中转数组和复制。
- 全仓调用搜索仅有该正式接收入口；codec 的 Read 参数统一为 ArraySegment，不新增兼容重载。使用既有 CanonicalReader 片段构造，尺寸检查改查 Count，空片段仍报原尺寸错误。Require 与 RequireComplete 按片段结束位置检查，不能读到上一次较长数据报遗留的缓冲尾部。
- ReadString 产生独立字符串；packet 构造通过 payload.ToArray 保存自身存储。同步 Read 返回后才进入接收队列和下一次 ReceiveFrom，消息不保存 reader 或原接收缓冲引用，重组与重传仍使用原所有权。
- MTU 拒绝、截断包异常处理、接收队列超限丢弃及统计时机不变。每次解码少一份整包数组，但 reader、packet、payload、身份字符串、端点和队列段分配仍未完成，不能宣称网络收包 0 GC。
- Endpoint portable 连带 Core／Fixed／Rollback 编译零警告零错误，按规定禁用构建服务器与共享编译，结束后 shutdown 成功；diff 空白检查通过。编辑前本机未发现 csc／bee 编译进程，未新增测试、未控制或刷新共享 Unity，未做网络联调或 Player 采样。

## 2026-09-20 重组分片保留不可变 packet

对应 tasks.md 的 5.17，与代码同步提交。

- FragmentAssembly 原为每个首次收到的分片调用 packet.CopyPayload，建立 byte[][]。packet 构造已经复制拥有 payload，所有字段只读且 payload 只暴露 ReadOnlySpan，因此这次克隆没有承担隔离可变数据的职责。
- 重组器改为 RollbackDatagramPacket[]，首次收到时保留该 packet；完成时按分片下标读取 Payload 并复制到最终消息数组。重复分片仍忽略，Reliable／FragmentCount／TotalPayloadBytes 检查、累计字节溢出与总长度检查均保留，最终解码消息的独立数组仍存在。
- 全仓调用核对后，RollbackDatagramPacket.CopyPayload 的唯一消费者是该重组器，迁移后删除旧入口；其它快照、服务器消息等同名 CopyPayload 不属于本类型，保持原有真实消费者。
- 取舍：不再创建逐片字节副本，但直到重组结束会保留 packet 本体及其身份字符串，常驻内存变化取决于分片尺寸和重组等待时间，未经采样不宣称总内存下降。packet 自身 payload、重组引用数组、最终消息数组及解码对象仍会分配，未引入复用池或提前回收。
- 编辑前检测到 Unity bee 和 csc 进程，等待具体进程结束并重新确认无编译进程后才修改。Endpoint portable 连带 Core／Fixed／Rollback 编译零警告零错误，按规定构建后 shutdown 成功；diff 空白检查通过。未新增测试、未刷新或控制共享 Editor、未做网络联调或 Player 采样。

## 2026-09-20 Canonical 基础数值写入栈缓冲

对应 tasks.md 的 5.18，与代码同步提交。

- CanonicalWriter 原字段 m_PrimitiveBuffer 为每个 writer 创建 byte[8]，供五个整数写入方法同步使用。现删除字段，WriteInt32／UInt32／UInt16／Int64／UInt64 各自 stackalloc sizeof(对应数值类型) 的缓冲。
- 仍调用相同 BinaryPrimitives 小端写入函数，再同步调用现有 MemoryStream.Write(Span)。每个函数单次使用 2／4／8 字节，退出函数后不保留引用；没有循环内累计栈申请，也没有托管池或共享可变缓冲。
- WriteDouble 继续通过 WriteInt64 写位模式，长度前缀及回填继续通过 WriteInt32；协议字段、负零规范化、异常和流所有权不变。消除的是每个 writer 的一个托管数组，不是每个整数一个数组；writer 对象、流容量、字符串编码租借和 ToArray 仍存在。
- portable Core 构建零警告零错误，按规定禁用构建服务器和共享编译并在结束 shutdown；当前 Unity 引用下独立编译完整 Core 也通过，产物仅在系统临时目录。diff 空白检查通过，编辑前未发现 csc／bee 编译进程；未新增测试、未主动刷新或控制共享 Unity、未做 Player 或协议实测。

## 2026-09-20 Canonical 哈希格式化统一

对应 tasks.md 的 5.19，与代码同步提交。

- CanonicalWriter.ComputeHash 和 SimulationCanonicalPayloadHash.Compute 原先各自计算 SHA-256，再创建 char[64] 并复制成字符串。现为既有 SimulationCanonicalPayloadHash 增加内部 ArraySegment 入口，公开 byte[] 入口保留原 null 检查后进入同一实现；writer 也直接使用该实现，删除私有 ToHex 和重复 SHA 装配。
- 哈希输入仍是原始完整字节，无新前缀。writer 的可见缓冲沿原 Offset 和流 Length 传入片段，非公开缓冲流仍沿原 ToArray 路径取得实际内容，未读取 Capacity 或改变 Position。此处没有改变任何快照保存、恢复、归还流程。
- 十六进制转换使用 string.Create 和无捕获 static 回调，直接填充最终字符串的全部字符；保留原高低半字节次序、小写字母及 StableHash 的 64 字符校验。每次少一个中间字符数组，首次回调委托初始化、SHA 对象、摘要 byte[] 和最终字符串仍存在。
- portable Core 零警告零错误，按规定构建后 shutdown 成功；Unity 当前引用下独立编译完整 Core 也通过，确认 string.Create 与静态回调在项目编译环境可用。diff 空白检查通过，编辑前未发现 csc／bee 编译进程；未新增测试、未控制或刷新共享 Unity，未做运行哈希对比或 Player 分配采样。

## 2026-09-20 网络检查点哈希副本清理

对应 tasks.md 的 5.20，与代码同步提交。

- NetworkCheckpointLayout 构造及 NetworkCheckpoint.ComputeHash 原写入 CanonicalWriter 后 ToArray，仅把该副本传给 SimulationCanonicalPayloadHash.Compute。现两处直接使用 writer.ComputeHash，后者已进入同一 SHA-256 实现，默认 writer 的流缓冲可直接读取，删除完整输出副本。
- 保留全部写入字段、顺序、版本字符串和哈希格式；不改检查点 m_StateBytes 的独立持有、StateBytes 复制读取、ValidateState 或恢复流程。布局计算属于准备阶段，内容哈希随检查点创建执行，两者没有混报为每帧热点。
- 此步仍有 writer、流、SHA 对象、摘要数组和最终哈希字符串分配。没有把检查点改为借用可变存储，也没有进入并行 Timeline 或回滚事务生命周期。
- ServerAuthoritative portable 连带 Core／Float32 编译零警告零错误，按规定禁用构建服务器与共享编译并结束 shutdown；diff 空白检查通过。编辑前未发现 csc／bee 编译进程，未新增测试、未主动刷新或控制 Unity，未做运行哈希对比或 Player 采样。

## 2026-09-20 权威同步数据报编码副本与类型装箱清理

对应 tasks.md 的 5.21，与代码同步提交。

- ServerAuthoritativeDatagramPacket 原在构造时复制持有 payload，ServerAuthoritativeGameplayDatagramCodec.Write 又调用 CopyPayload 克隆后立即写流。现在 packet 提供 ReadOnlySpan Payload，编码同步写入该只读视图，删除每次编码的中转数组。packet 的构造复制及最终数据报数组仍保留。
- 客户端通道和 AuthoritySourceRuntime 仍有 CopyPayload 的独立数据消费者，保持此真实入口，未把解码或跨步数据擅自改成借用存储。既有版本、身份字段、长度前缀、MTU 检查和发送队列数据寿命不变。
- ServerAuthoritativeDatagramHeader 构造与 codec.Read 共用 IsSupportedKind，显式接受 Hello、HelloAck、Command、Snapshot 四种既有枚举值，删除两处 Enum.IsDefined(Type, object) 装箱；各自仍抛原 ArgumentException／InvalidDataException，不更改非法值行为。
- ServerAuthoritative.Transport portable 连带 Core／Float32／ServerAuthoritative 编译零警告零错误，按规定构建后 shutdown 成功；diff 空白检查通过。编辑前未发现 csc／bee 编译进程；未新增测试、未主动刷新或控制共享 Unity，未做网络联调或 Player 采样。解码中间数组、writer、最终包和队列分配仍未完成。

## 2026-09-20 权威数据报接收解码片段借用

对应 tasks.md 的 5.22，与代码同步提交。

- ServerAuthoritativeDatagramEndpoint 原在接收后复制完整 received 字节，codec.Read 又 ReadBytes 复制 payload，packet 构造再 Clone。现唯一正式 Read 调用直接提供 [0, received) ArraySegment，codec 同步借用片段并通过 ReadBytesSegment 将 payload 交给 packet 构造，删除前两层中转。
- codec.Read 统一接收 ArraySegment，按 Count 检查尺寸，CanonicalReader 按片段结束边界读取并 RequireComplete，接收缓冲未使用区域不能混入解析。原 MTU、格式错误、路由、队列和丢包统计分支保持不变。
- packet 的内部 Span 构造执行唯一一次 ToArray 取得所有权。公开 byte[] 构造仍供两个正式发送生产者使用，保留 null 抛 ArgumentNullException 后进入同一个构造实现；没有复制两套校验或编码。空 payload 和 header 长度匹配规则不变。
- 解码身份字符串和 packet payload 均独立持有，下一次接收不会覆盖排队消息；未修改应用层消息、检查点或 Timeline 生命周期。reader、packet、身份字符串、payload 本身、端点和队列仍有分配。
- ServerAuthoritative.Transport portable 及 Core／Float32／ServerAuthoritative 依赖编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过；编辑前未发现 csc／bee 编译进程。未新增测试、未主动刷新或控制共享 Unity，未做网络联调或 Player 采样。

## 2026-09-20 快照数据报 delta 中转副本清理

对应 tasks.md 的 5.23，与代码同步提交。

- SnapshotDatagram 编码原 CopyDeltaPayload 后立即写流，现通过 DeltaPayload 只读 Span 直接写入；解码原 ReadBytes 后交构造 Clone，现 ReadBytesSegment 交内部片段构造 ToArray 一次持有，删除编码和解码各一份中转数组。
- 正式发送仍使用 byte[] 构造，该入口转换片段后统一执行既有身份检查和存储复制；null 转默认片段，在原身份校验之后抛 ArgumentNullException，空但非 null 数组仍合法。没有新增第二套构造校验或借用结果对象。
- ServerAuthoritativeCheckpointReconstructionModule 是 CopyDeltaPayload 的实际消费者，继续获得独立副本；本步不改 checkpoint 重建、恢复、delta 应用或历史生命周期。消息元数据顺序、schema、尾部检查和最终存储所有权不变。
- ServerAuthoritative.Transport portable 及全部依赖编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过。编辑前未发现 csc／bee 编译进程；未新增测试、未主动刷新或控制 Unity、未做网络联调或 Player 分配采样。快照对象、自身 delta 数组和最终编码数组仍会分配。

## 2026-09-20 权威输入与基线 canonical 比较副本清理

对应 tasks.md 的 5.24，与代码同步提交。

- ServerAuthoritativeCanonicalCodec.ReadInput／ReadBaseline 原调用公开 WriteInput／WriteBaseline 得到完整 byte[] 后逐字节比较，该结果只用于判断是否 canonical。现各自的公开写入和解码校验共用私有 writer 写入函数，校验通过已有 CanonicalWriter.ContentEquals 直接读取流比较，不再 ToArray。
- 两类消息仍完整重新编码，保留原字段顺序、schema、尾部 RequireComplete、基线 ActorId 与 BodyHash 检查及不匹配 InvalidDataException 文本。公开 WriteInput／WriteBaseline 仍为发送和保存消费者产生独立数组；没有使用哈希替代字节比较或放宽验证。
- 只删除输入和基线每次 canonical 比较的一份完整数组。重新编码 writer／流、基线 CopyCharacterStateBytes、解码对象及公开结果仍分配；未修改基线或回滚存储寿命。
- ServerAuthoritative portable 连带 Core／Float32 编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过；编辑前未发现 csc／bee 编译进程。未新增测试、未主动刷新或控制共享 Unity、未做协议运行对比或 Player 分配采样。

## 2026-09-20 权威输入解码枚举装箱清理

对应 tasks.md 的 5.25，与代码同步提交。

- ServerAuthoritativeCanonicalCodec 的泛型 ReadEnum 仅用于输入来源和输入值类型，先 Enum.ToObject 产生枚举对象，再 Enum.IsDefined(Type, object) 检查。删除该入口后，来源按 LocalLogic／Authoritative／Replay 显式比较；输入值直接转换并进入既有解码 switch。
- 已核对 Float32 SimulationInputValueKind 的全部六个成员与解码分支一致。未知字节进入 switch 默认分支时仍抛原泛型校验的 InvalidDataException 和数值文本，不再保留原本不可达的 Unsupported 分支。未知来源仍在读取 clockId 前拒绝，未知输入值仍在读取值数据前拒绝。
- 不改变来源身份、输入排序、值解码、canonical 重编码或协议 schema；减少每条来源和每个输入值的反射转换／装箱入口，未宣称整个输入对象创建无分配。
- ServerAuthoritative portable 连带 Core／Float32 编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过。编辑前未发现 csc／bee 编译进程；未新增测试、未主动刷新或控制共享 Unity，未做协议运行或 Player 分配采样。

## 2026-09-20 权威输入嵌套直接编码

对应 tasks.md 的 5.26，与代码同步提交。

- 全部四处 writer.WriteBytes(ServerAuthoritativeCanonicalCodec.WriteInput(...)) 分别位于输入数据报、Egress 输入、AuthorityPasses 状态写入和 PredictionStateCodec 状态写入。原先每个子输入创建 writer／MemoryStream，再 ToArray 并复制到外层。
- 新 WriteLengthPrefixedInput 调用已有 CanonicalWriter.BeginLengthPrefixedBlock／EndLengthPrefixedBlock，在外层预留并回填四字节长度；内部沿同一个私有 WriteInput，不复制字段编码。该入口因 Transport 是独立程序集而公开，四个调用点统一迁移，公开返回独立输入数组的真实入口保留。
- 输入 Magic／schema、字段顺序、小端长度及外层遍历次序不变。状态写入两个文件仅替换这一行，不改变保存、恢复、确认、事务或并行 Timeline 生命周期。写入失败不返回外层部分包，子项不创建独立结果；外层 writer、流和最终数组仍分配。
- ServerAuthoritative.Transport portable 连带 Core／Float32／ServerAuthoritative 编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过。编辑前目标文件无其它未提交修改，未发现 csc／bee 编译进程；未新增测试、未主动刷新或控制共享 Unity，未做协议字节实测或 Player 分配采样。

## 2026-09-20 权威嵌套输入片段解码

对应 tasks.md 的 5.27，与代码同步提交。

- 全仓 ReadInput 调用只有输入数据报、Egress 输入、AuthorityPasses 和 PredictionStateCodec 四处，原先均 ReadBytes 建立独立子数组。现在统一 ReadBytesSegment 并将正式 ReadInput 参数迁移为 ArraySegment，没有保留无消费者的 byte[] 重载。
- 共用 Reader 以片段构造 CanonicalReader，所有基础读取与 RequireComplete 使用片段边界；重新编码仍完整执行，ContentEquals 只比较 bytes.AsSpan() 的实际范围。父消息其它字段或尾部缓冲不能混入输入解析与 canonical 比较。
- ReadInput 的结果由独立字符串、数值、值数组和请求数组构成，不保存 reader、片段或源字节引用，因此借用结束于同步调用。状态保存恢复两个文件只替换子输入读取调用，不改变事务或存储寿命。ReadBaseline 仍从整个 byte[] 进入统一 Reader，原 null 异常和长期 stateBytes 复制保持不变。
- 删除每个嵌套输入的一份字节数组；reader、解码对象集合和重新编码 writer 仍分配。ServerAuthoritative.Transport portable 及 Core／Float32／ServerAuthoritative 编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过。编辑前目标文件无其它未提交修改、无 csc／bee 编译进程；未新增测试、未刷新或控制共享 Unity，未做协议运行或 Player 分配采样。

## 2026-09-20 权威快照模型 MTU 检查副本清理

对应 tasks.md 的 5.28，与代码同步提交。

- AuthoritySourceRuntime 发送 delta 快照前调用 GameplayDatagramCodec.Write 后丢弃数组，仅依靠超限异常判断是否进入原全量检查点处理。Endpoint.EnqueueSend 还会按自身预算实际编码；两个入口接收的预算来源不同，因此保留模型检查而非直接删除。
- codec 的 Write 与新 RequireFits 共用唯一私有写入函数，完成原字段编码后按 writer.Length 检查 maximumBytes。RequireFits 不 ToArray；正式 Write 通过预算检查后才输出独立数组，因此超限写入也不再生成最终数组。原参数异常、MTU 文本和 InvalidDataException 处理保留。
- 不计算另一套尺寸公式、不缓存可变 payload，也不改变序号、路由、队列、StoreSent 或原超限业务分支。仍执行完整编码，writer／流和编码 CPU 未消除；不宣称此检查无分配。
- ServerAuthoritative.Transport portable 及全部依赖编译零警告零错误，按规定构建后 shutdown 成功；diff 空白检查通过。编辑前目标文件无其它未提交修改且未发现 csc／bee 编译进程。未新增测试、未主动刷新或控制共享 Unity，未做网络联调或 Player 采样。

## 2026-09-20 Egress 三类消息 canonical 副本清理

对应 tasks.md 的 5.29，与代码同步提交。

- ServerAuthoritativeEgressCodec 的 ReadOwnerInput／ReadAuthorityReplication／ReadRemotePresentation 原完整重新编码并 ToArray，只为 RequireCanonical 逐字节比较。现公开写入与读取校验共用各自唯一私有 writer 写入函数，RequireCanonical 调用既有 ContentEquals，不产生最终比较数组。
- 保留原 schema、字段次序、嵌套消息编码和完整重新编码检查；尾部与集合数量检查、错误文本和返回消息独立数据不变。只修改编解码内部存储，不改表现事件消费、Timeline 播放、事务或回滚生命周期。
- 嵌套基线及远端表现子消息仍会创建独立 writer 和数组，此步不宣称它们已完成；外层 writer、流、解码集合、公开编码结果也仍分配。
- ServerAuthoritative portable 连带 Core／Float32 编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过。编辑前目标文件无其它未提交修改且未发现 csc／bee 编译进程；未新增测试、未主动刷新或控制共享 Unity，未做协议运行对比或 Player 分配采样。

## 2026-09-20 公共数值配置解码装箱清理

对应 tasks.md 的 5.36，与代码同步提交。

- SimulationNumericProfileCodec.Read 原通过泛型 ReadEnum 为舍入和溢出字段分别 Enum.ToObject，再 Enum.IsDefined。现直接转换并比较 Ieee754NearestEven／FixedNearestEven、RejectNonFinite／RejectOverflow，全部消费者迁移后删除反射泛型入口。
- 合法成员和非法零值处理与当前枚举声明一致，两个字段读取先后、后续 deterministicReplay 读取和 SimulationNumericProfile 构造规则不变；错误文本中的类型名由 nameof 提供，与原 typeof(T).Name 相同。没有修改浮点或定点舍入、溢出算法，也没有新增运行缓存。
- portable Core 编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过；目标文件此前无其它未提交修改，编辑前未发现 csc／bee 编译进程。未新增测试、未主动刷新或控制共享 Unity，未做协议运行或 Player 分配采样；此步仅覆盖两个枚举对象入口。

## 2026-09-20 回滚输入枚举校验装箱清理

对应 tasks.md 的 5.37，与代码同步提交。

- 核对 RollbackInputProvenance 六种、Fixed SimulationInputValueKind 六种、SimulationTickSourceKind 三种正式成员后，RollbackInputCodec 的三个读取校验函数改为显式 byte 成员比较，删除 Enum.IsDefined(Type, object) 的值类型装箱。
- 接受集合、非法零值及其它字节的 InvalidDataException 文本、读取先后和返回枚举值保持不变。不改变预测来源、输入提交、确认、历史或回滚流程，仅替换协议类型校验方式。
- 同时检查 SimulationNumericProfile 的 Equals／GetHashCode，当前使用强类型比较和整数枚举参数，没有基于此检查做无证据改动。
- DeterministicRollback portable 连带 Core／Fixed 编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过。编辑前目标文件无其它未提交修改且未发现 csc／bee 编译进程；未新增测试、未主动刷新或控制共享 Unity，未做协议运行或 Player 分配采样。

## 2026-09-20 公共身份哈希逐字节格式化清理

对应 tasks.md 的 5.40，与代码同步提交。

- SimulationIdentity.Hash 是 StableHash.Compute 和事件身份等路径的共用实现。原 SHA-256 摘要逐个 byte.ToString("x2", InvariantCulture)，产生 32 个两字符字符串，再经 StringBuilder 复制成最终字符串。
- 现在保留原 string.Join 的 U+001F 分隔符、null 数组按空数组处理以及 Encoding.UTF8.GetBytes，交给既有 SimulationCanonicalPayloadHash.Compute，取得相同原始字节 SHA-256 的小写 Value。统一入口通过 string.Create 填充结果，不再保留另一套哈希格式化实现。
- 没有改变输入字段、分隔符、编码、摘要算法或身份格式，也不改变事件生成次数和排序。移除逐字节小字符串、builder 及其存储；params 输入数组、joined 字符串、UTF-8 数组、SHA／摘要和最终字符串仍会分配。
- portable Core 编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过；编辑前文件无其它未提交修改且未发现 csc／bee 编译进程。未新增测试、未主动刷新或控制共享 Unity，未做运行哈希对比或 Player 分配采样。

## 2026-09-20 统一 SHA-256 摘要栈存储

对应 tasks.md 的 5.41，与代码同步提交，继续处理 5.19／5.40 保留的摘要结果数组。

- SimulationCanonicalPayloadHash 改用 SHA256.TryComputeHash，将实际 ArraySegment 的 Span 写入固定 32 字节栈缓冲；若提供者未产生完整摘要则抛 CryptographicException，不对不完整数据生成身份。
- 十六进制结果由固定 64 字符栈缓冲填充，再创建最终字符串，替代原返回 byte[] 后 string.Create 的流程。保留相同 SHA-256 算法、输入片段 Offset／Count、小写高低半字节顺序和 StableHash 校验；没有改变哈希输入或使用替代摘要算法。
- 栈缓冲大小由 SHA-256 固定格式确定，单次调用共 32 字节加 64 个 char，不跨调用保留。源码不再请求返回摘要数组；SHA 实例与最终字符串仍分配，提供者内部是否还有临时存储未采样，不宣称哈希全程 0 GC。
- portable Core 编译零警告零错误，按规定构建后 shutdown 成功；当前 Unity 引用下独立编译完整 Core 也通过，确认 TryComputeHash 和 Span 字符串构造在项目目标可用，产物只写系统临时目录。diff 空白检查通过，编辑前无其它目标修改且未发现 csc／bee 编译进程。未新增测试、未主动刷新或控制 Unity、未做运行哈希对比或 Player 分配采样。

## 2026-09-20 GameplayEffect 相同来源标签数组复用

对应 tasks.md 的 2.10，与代码同步提交。

- Fixed／Float32 SimulationGameplayEffectState.SetTagSource 原先每次规范化、排序、去重并 ToArray，再与现有来源数组比较；即使内容相同也丢弃刚分配的数组。现在将现有数组传给唯一 CanonicalTags，在工作列表上按下标 Ordinal 比较，相同时返回已有数组，变化才 ToArray。
- SetTagSource 通过引用相同识别未变化结果。规范化和错误校验仍完整执行，不根据输入引用跳过处理；排序、重复标签去除、空结果移除来源、m_TagsDirty 的设置及变更后的独立数组保持不变。初始标签仍沿同一函数且没有现有数组可复用。
- 工作列表仍在 finally 清空，未将列表借给长期状态，也未调整 scratch 创建、事务保存或跨步寿命。没有 scratch 的原调用仍创建临时 List，IEnumerable 遍历和 NormalizeTag 等成本也保留；本步只消除内容相同的结果数组。
- 两个状态文件编辑前均无其它未提交修改；相邻 AbilityExecutionFrame／OperationControlRuntime 有并行修改，未触碰。编辑前未发现 csc／bee 编译进程。Fixed 与 Float32 portable 各自编译零警告零错误，每次按规定构建后 shutdown 成功，diff 空白检查通过；未新增测试、未刷新或控制共享 Unity、未做 Player 分配采样。

## 2026-09-20 动作标签来源键数字中转清理

对应 tasks.md 的 2.11，与代码同步提交。

- 两数值域 GameplayEffectTarget 的 SetActionTags／RemoveActionTags 都调用公共 GameplayTagSourceIdentity.ActionInstance。原先 ulong.ToString(InvariantCulture) 创建数字字符串，再拼接 action: 创建最终键；现把前缀和十进制数字写入栈缓冲，最后只创建结果字符串。
- 容量由 action: 的 7 字符与 ulong 十进制最大 20 位确定，覆盖全部非零 ulong；仍使用 InvariantCulture、默认十进制格式和原零值异常。没有添加键缓存、改变来源标识、标签存储、动作生命周期或事务边界；最终键字符串仍分配。
- 标签 Normalize 对已规范化输入直接返回原字符串，本轮未修改该正确路径。动手前检测到 Unity 编译进程，等待具体 bee／csc 进程结束并重新确认无编译进程后才编辑。
- portable Core 编译零警告零错误，按规定构建后 shutdown 成功；当前 Unity 引用下完整 Core 独立编译也通过，确认 ulong.TryFormat 与 Span 字符串构造可用。diff 空白检查通过，未新增测试、未刷新或控制共享 Unity、未做运行身份对比或 Player 分配采样。

## 2026-09-20 效果快照标签汇总与列表复制清理

对应 tasks.md 的 2.12，与代码同步提交。

- Fixed／Float32 效果快照 CollectOwnedTags 原用 SortedSet，每次汇总为唯一标签建立树节点；现在 HashSet 按 Ordinal 去重，CopyTo 最终独立数组，再按 Ordinal 排序。内容唯一性、顺序、字符串引用和返回数组归属保持不变。
- 取舍：散列表使用桶／条目数组并增加一次最终排序，取消逐标签节点对象；新集合及容量增长仍分配，实际常驻内存和 CPU 收益未经采样。未把快照数组改为共享工作列表或建立跨步缓存。
- 同一快照类的 Copy<T> 唯一调用源是 CloneActiveEffects 返回的 List，改为 IReadOnlyList 按下标读取，删除 IEnumerable 枚举器装箱；元素仍来自原深拷贝，字典复制、活动效果克隆及恢复过程未改。
- 两个目标文件此前无其它未提交修改，编辑前未发现 csc／bee 编译进程。Fixed／Float32 portable 各自零警告零错误，每次按规定构建后 shutdown 成功，diff 空白检查通过。未新增测试、未主动刷新或控制共享 Unity、未做快照运行对比或 Player 分配采样。

## 2026-09-20 效果快照字典复制枚举装箱清理

对应 tasks.md 的 2.13，与代码同步提交。

- 核对两个快照实现中 Copy<TKey,TValue> 全部八个调用：标签、属性、period、journal、revision 克隆返回 SortedDictionary，Spec 的 SetByCaller／SourceAttributes／TargetAttributes 也声明为 SortedDictionary。原私有辅助函数经 IEnumerable 枚举这些具体容器，会装箱枚举器。
- 源参数改为实际 SortedDictionary，foreach 直接使用其具体枚举器；目标仍为 IDictionary，保持调用方存储接口。未新增类型分支、转换副本或另一套复制实现，元素、遍历次序和深拷贝隔离关系不变。
- 此步不移除 SortedDictionary 枚举内部树遍历所需存储，也不移除克隆字典和树节点；不宣称复制过程 0 GC。没有改变快照保存、恢复或事务寿命。
- Fixed 与 Float32 portable 分别编译零警告零错误，每次按规定构建后 shutdown 成功，diff 空白检查通过；编辑前目标文件无其它未提交修改且未发现 csc／bee 编译进程。未新增测试、未主动刷新或控制共享 Unity、未做 Player 分配采样。

## 2026-09-20 属性 modifier 快照列表精确容量

对应 tasks.md 的 2.14，与代码同步提交。

- 两数值域 CloneAttribute 原创建零容量 Modifiers 列表后逐个深拷贝 Add，数量超过 List 初始增长档位时会创建多个存储数组。现在在 Add 循环前按 source.Modifiers.Count 设置容量，空源保持零容量，整个复制期间不再增长。
- 每个 modifier 仍通过 CloneModifier 独立构造，属性定义、值、revision 和顺序不变；没有共享可变元素、取消克隆或改变快照／工作状态隔离。
- 取舍：容量恰好覆盖本次快照内容，不保留 List 自动增长的额外余量；恢复后若继续新增 modifier 仍可能再次扩容，甚至比原有余量更早增长。此次只保证复制内不逐级扩容，不宣称后续更新或全链分配下降，后续正式容量仍需结合事务 owner 处理。
- Fixed／Float32 portable 分别编译零警告零错误，每次按规定构建后 shutdown 成功，diff 空白检查通过。编辑前目标文件无其它未提交修改且未发现 csc／bee 编译进程；未新增测试、未主动刷新或控制共享 Unity、未做 Player 分配采样。

## 2026-09-20 GameplayEffect 变更记录闭包清理

对应 tasks.md 的 2.15，与代码同步提交。

- Fixed／Float32 GameplayEffectTarget 的生命周期、属性、Cue 和失败记录原各创建捕获 lambda，交 AddChange(Func<ulong,...>) 立即调用。八处迁移为 m_Changes.Add(new 对应记录(NextChangeCursor(), ...))，删除闭包／委托构造及无消费者工厂入口。
- NextChangeCursor 保留原 checked 递增和溢出异常，返回本次游标。执行仍是游标推进、记录构造、加入列表；生命周期无效 instance／revision 的提前返回、属性逐项顺序和 Cue 预测标记仍在原位置。记录构造失败时的游标状态与原逻辑一致。
- 四种记录对象及变更列表存储仍存在，不改事件内容、游标协议、变更消费、TrimChanges 或保存恢复边界。此步删除的是工厂封装带来的分配，不宣称事件记录全程无分配。
- 两个目标文件无其它未提交修改；本机编译进程查询未返回 csc／bee。Fixed／Float32 portable 均编译零警告零错误，构建后 shutdown 均成功，diff 空白检查通过。未新增测试、未主动刷新或控制共享 Unity、未做运行事件对比或 Player 分配采样。

## 2026-09-20 GameplayEffect 授予标签准备清单

对应 tasks.md 的 2.16，与代码同步提交。

- Fixed／Float32 ActivateGrantedTags 原在每次效果激活时扫描定义组件，用 SortedSet 合并 GrantedTagsComponent 的标签。该数据来自已加载定义，与当前属性值、堆叠和 Tick 无关；现移到 PortableEffectDefinition 构造，在 Components 赋值后生成 GrantedTags 清单，由原目录持有。
- 清单继续采用原 Ordinal 去重和排序，合并全部授予标签组件；无授予标签时为空。激活沿原 effect:handle 来源调用 SetTagSource，仍生成／比较活动来源自己的规范化结果，未将定义数组作为可变状态或改变 DeactivatePersistent 的移除行为。
- 取舍：每个定义增加一个长期字符串引用清单，换取取消每次激活的组件扫描、排序树和树节点创建；容量由实际内容精确决定，没有任意上限或旁路配置。目录准备仍可分配，SetTagSource、来源字符串和其它效果记录仍有分配。
- 四个目标文件修改前均无其它未提交修改，编辑前未发现 csc／bee 编译进程。Fixed／Float32 portable 各自编译零警告零错误，每次按规定构建后 shutdown 成功，diff 空白检查通过；未新增测试、未刷新或控制共享 Unity、未做效果运行对比或 Player 分配采样。

## 2026-09-20 效果标签来源键统一栈格式化

对应 tasks.md 的 2.17，与代码同步提交。

- 两数值域 ActivateGrantedTags／DeactivatePersistent 原分别插值 effect: 与 ulong handle，在当前 C# 9 编译路径经过数值格式化装箱。四处迁移至已有 GameplayTagSourceIdentity 的 EffectHandle，和 ActionInstance 共用私有栈格式化实现，最终只建立键字符串。
- 容量仍由固定前缀长度与 ulong 最大十进制 20 位确定。ActionInstance 保留零值拒绝和 InvariantCulture；EffectHandle 沿旧插值保留 CurrentCulture 及原零值可格式化行为，不新增业务约束或把不同键类型混用。
- 其它 effect:effectId 是效果定义目录身份，非活动效果 handle，本次未迁移。激活与移除读取相同格式化规则，标签清单和来源存储不变；最终键仍有分配，没有引入跨步字符串缓存或改变事务 owner。
- Fixed／Float32 portable 分别编译零警告零错误，按规定构建后 shutdown 均成功，diff 空白检查通过；编辑前目标文件无其它未提交修改且未发现 csc／bee 编译进程。未新增测试、未主动刷新或控制共享 Unity、未做运行身份对比或 Player 分配采样。

## 2026-09-20 效果快照空标签数组共享

对应 tasks.md 的 2.18，与代码同步提交。

- 两数值域 CloneSpec 的 SourceTags／TargetTags 以及 CloneTagSources 原只对 null 使用 Array.Empty，零长度数组仍 Clone 成新的空数组。现在 null 与零长度统一复用 Array.Empty，非空分支保持原数组克隆。
- 空数组没有可修改元素，既有状态也已使用 Array.Empty 表示无标签；集合长度、遍历结果和序列化数据不变。非空数组的隔离关系、Spec 对象和来源字典仍独立，不修改事务或快照归还边界。
- 同时核对附加效果应用，现有工作列表通过 Acquire／finally Release 使用，公共 Admission 已在来源标签数量为零时避免新建数组，本轮不重复修改这些正确路径。
- Fixed／Float32 portable 各自零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过；目标文件此前无其它未提交修改，编辑前未发现 csc／bee 编译进程。未新增测试、未主动刷新或控制共享 Unity、未做运行对比或 Player 分配采样。

## 2026-09-20 效果请求构造中间列表清理

对应 tasks.md 的 2.19，与代码同步提交。

- SimulationGameplayEffectApplication 的 CopySetByCaller／CopyAttributes／CopyIdentities 原先复制到 List，排序校验后 ToArray。两数值域改为 Enumerable.ToArray 取得独立最终数组，再沿原 Ordinal 比较排序、身份规范化和重复项校验；null 使用原空结果语义。
- SimulationGameplayEffectLifecycleIngress 的参数只需保留输入顺序，改为直接 ToArray，不再创建 List。FromCompiled 的既有准备数组入口不变，也不新增旁路缓存。
- 对正式数组和 List 输入，直接 ToArray 利用集合数量复制，取消中间 List 对象和其存储到最终数组的再次复制。一般 IEnumerable 的框架内部仍可能使用增长缓冲及枚举器，结果数组自身仍分配；该改动不等于请求无分配。
- 调用方数据不会被排序或规范化原地修改，结果仍独立持有。重复参数／属性／标签错误文本和校验顺序保留，生命周期接收规则未改。
- 编辑前检测到 Unity bee 进程，等待该进程结束并确认无 csc／bee 后修改；两个目标文件无其它未提交修改。Fixed／Float32 portable 分别编译零警告零错误，每次按规定构建后 shutdown 成功，diff 空白检查通过。未新增测试、未刷新或控制共享 Unity、未做请求运行对比或 Player 分配采样。

## 2026-09-20 属性初始化及重算字典视图清理

对应 tasks.md 的 2.20，与代码同步提交。

- 两数值域属性初始化先遍历 m_Attributes.Keys 求值，再遍历 Values 设置 revision；RecalculateAll 也先读取 Keys。现直接遍历具体 SortedDictionary 的 KeyValuePair，读取对应 Key／Value，取消这些入口的集合视图访问。
- 按原字典顺序执行，初始化仍先完成全部计算再设置全部 revision=1，没有合并两轮或提前修改 revision；重算的排除属性、cache、递归 stack 和变化列表行为不变。SortedDictionary 树遍历存储仍可能分配；只删除上述 Keys／Values 视图创建需求，不把它算成每次必定新建视图或整个重算无分配。
- 本轮重新读取并行 Timeline 任务，状态仍为 inProgress；工作树中 Fixed／Float32 AbilityExecutionFrame 有其它未提交修改。两个 CharacterEvaluationRuntime 仍每步创建效果 scratch，因此 2.1／2.2 的跨步 owner 迁移保持未完成，不在本轮介入共享执行帧。
- 同时核对 SimulationInput 构造，ICollection 路径已经按 Count 一次复制，保留该正确实现。两个实际修改文件此前无其它改动，编辑前无 csc／bee 编译进程。Fixed／Float32 portable 各自零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过；未新增测试、未刷新或控制共享 Unity、未做 Player 分配采样。

## 2026-09-20 渲染查询调用阶段复核

本节补充任务 6.3 的代码证据，不将 6.3 标为完成。本轮不修改渲染代码，也未做编译、运行或 Player 采样。

- LocalHeatDistortionAreaSource.GetComponentsInChildren<ParticleSystem>(true) 位于 OnValidate，且只在序列化粒子数组为空时执行；不能据此认定 TryResolveArea／TryBuildAreaSettings 每帧产生数组。ActiveSources 在 OnEnable／OnDisable 修改，稳态查询按下标遍历；动态激活造成的列表扩容仍未治理。
- CharacterShapeProjectionSource.sharedMaterials 位于 ValidateSource。源码调用点为 PrepareAndRegister、CharacterShapeProjectionRuntimeWorkspace 构造和 Editor Inspector 显式校验，不属于直接的帧 Execute 路径。工作区重建时仍可能执行并分配，重建触发频率没有运行证据，不能将它报告为已实现 0 GC。
- ShapeProjectionFrameSlot 构造保存 readbackCallback=OnReadbackCompleted，RecordReadback 复用该委托并写入已有 NativeArray，未发现每次提交新建 lambda。该正确复用不重写；GPU/native 存储及回调内部行为仍需分别归因。
- LocalHeatDistortionRenderPass.Execute 使用已有 ProfilingSampler、共享 MaterialPropertyBlock、CommandBufferPool.Get／Release；new Vector4 是值类型，不能按关键字计为托管对象。RTHandle 的 ReAllocateIfNeeded 与底层 Unity 调用需要尺寸变化及首次／稳态采样，当前仅有源码证据。
- 后续 6.3 应追踪实际工作区重建、动态源启停、渲染目标尺寸变化和 Unity/native 内部分配，保留原正式性能采集链；不以替换这些已有复用代码冒充治理进展。

### ShapeProjection 帧入口与工作区创建边界

- 继续沿 CharacterShapeProjectionMaskPass.Execute 核对：每帧先 pool.Sweep，再按来源下标调用 GetOrCreate、UpdateCamera、ProcessCompletedContours、TryPrepareSubmission、RecordMask 和 RecordReadback。GetOrCreate 的 key 包含 CameraId、SourceInstanceId、SourceGeneration；已存在 key 直接返回工作区，不重复构造。
- pool 的 Dictionary 和 staleKeys List 均按 maxWorkspaces 准备；创建前检查容量，Sweep 直接枚举具体 Dictionary，待淘汰 key 不超过工作区数量。现有源码已有明确容量与复用，不为此新增另一套池。原 maxWorkspaces 的配置校正行为本轮未改。
- 首个相机／来源／代次组合仍会在 Execute 内创建工作区，构造包括烘焙 Mesh、渲染器数组、按 VertexCount 准备的顶点 List、固定 NativeArray／GraphicsBuffer 和读回槽。来源失效、禁用或代次变化在 Sweep 释放；随后重新进入可创建新工作区。首次创建与重建不是稳态无分配证据，应分别采样并追踪容量来源。
- PublishDiagnostics 的 ShapeProjectionDiagnosticsSnapshot 是 struct；Job、Vector4、Hash128 也为值类型，不能因 new 关键字列为托管对象。Recorder 在静态初始化建立，仍需要区分第一次访问与后续读取，底层 native／Unity 调用开销未验证。
- 更早准备工作区需要正式相机与来源可用时机，复用旧代次存储需要保证未完成 GPU readback／Job 和发布结果的所有权；本轮不为绕开这些边界建立共享全局工作区。任务 6.3 继续未完成，下一步证据应覆盖首次、重建、稳态和在途释放；本轮仅更新审计，没有修改渲染代码或运行 Editor。

## 2026-09-20 回滚输入帧来源构造校验统一

对应 tasks.md 的 5.38，与代码同步提交。

- RollbackInputCodec 的来源读取装箱虽已删除，随后 RollbackActorInputFrame 构造仍 Enum.IsDefined 校验，且本地生成输入也经过该构造。现在由帧类型提供内部 IsValidProvenance，按原六个正式成员比较，构造与 codec 共用该规则，删除构造装箱及两处分别维护的来源清单。
- codec 仍在原字节读取位置以 InvalidDataException 拒绝非法来源；直接构造仍沿原合并检查抛 ArgumentException。没有省略身份、数值配置、序号和 Tick 校验，也没有改 InputHash／GameplayHash、预测、确认或回滚流程。
- 相机效果存储检查发现停止／撤销／完成依赖尚与 Timeline 相交，本轮未改其复用；ACL 租用表已有按容量准备的槽位，也未重复改造。
- DeterministicRollback portable 连带 Core／Fixed 编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过。编辑前目标文件无其它未提交修改且未发现 csc／bee 编译进程；未新增测试、未主动刷新或控制共享 Unity，未做运行或 Player 分配采样。

## 2026-09-20 回滚协议封套枚举校验清理

对应 tasks.md 的 5.39，与代码同步提交。

- RollbackProtocolCodec.ReadKind 改为显式匹配十种正式消息；ReadComponentIdentity 匹配八种当前组件角色。删除 Enum.IsDefined(Type, object) 的 byte 装箱，保留原 InvalidDataException 文本与读取位置。
- SimulationComponentRole 正式值从 ExecutionBackend=2 开始，0 和 1 仍拒绝；未使用粗略非零判断或包含废弃编号的范围。消息解码、组件身份字符串读取、canonical 检查和协议版本均不变。
- 角色读取属于握手身份处理，消息类型读取覆盖协议消息，两者不混报为相同频率热点。后续身份对象和消息对象分配仍存在，不宣称整个封套无分配。
- DeterministicRollback portable 连带 Core／Fixed 编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过。编辑前目标文件无其它未提交修改且未发现 csc／bee 编译进程；未新增测试、未主动刷新或控制共享 Unity，未做协议运行或 Player 分配采样。

## 2026-09-20 权威复制子消息直接编码

对应 tasks.md 的 5.30，与代码同步提交。

- 全仓嵌套基线和远端表现写入各一处，均在 Egress.WriteAuthorityReplication。原每个子消息独立 Write 返回数组，再 WriteBytes 复制到外层。现通过内部 WriteLengthPrefixedBaseline 和私有 WriteLengthPrefixedRemotePresentation 写外层，再沿已有 Begin／EndLengthPrefixedBlock 回填四字节长度。
- 两个辅助入口只处理长度边界，仍调用各自唯一字段写入函数，保留 null 参数异常、Magic／版本／字段与集合次序。公开 WriteBaseline／WriteRemotePresentation 的实际独立结果消费者保留，不另建编码协议。
- 每个子项不再创建 writer、流和完整输出数组。读取侧 ReadBytes、基线自身角色状态字节复制、外层 writer 与最终数组仍分配；不改变远端表现消费、事件或状态恢复生命周期。
- ServerAuthoritative portable 连带 Core／Float32 编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过。编辑前目标文件无其它未提交修改且未发现 csc／bee 编译进程；未新增测试、未主动刷新或控制共享 Unity，未做协议字节对比或 Player 分配采样。

## 2026-09-20 权威复制子消息片段解码

对应 tasks.md 的 5.31，与代码同步提交。

- ReadAuthorityReplication 的基线及远端表现循环改用 ReadBytesSegment。ReadBaseline 全仓唯一调用迁移后，正式入口统一接收 ArraySegment；远端表现仍有独立 byte[] 消费者，公开入口保留原 null 检查后进入同一私有片段解码，不复制另一套解析实现。
- 两类子消息的 CanonicalReader 与 ContentEquals 都限定为实际片段，继续执行 schema、集合上限、RequireComplete 和完整重新编码比较。原包中相邻子消息不参与本片段校验，公开 byte[] 消费者的输入范围不变。
- 基线内部角色状态仍 ReadBytes 并由结果复制持有；远端表现结果从解析出的独立字符串、数值和结果集合构造，不保存原包或 reader。删除的是每个外层子消息的字节副本，不改长期基线、事件、播放、事务或恢复生命周期。
- 私有 RequireCanonical 的源参数改为 ReadOnlySpan；null 已由全部公开读取入口在解析前拒绝，不再保留不可达的源 null 判断。错误文本及结果数据校验不变。
- ServerAuthoritative portable 连带 Core／Float32 编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过。编辑前及续做前未发现 csc／bee 编译进程；未新增测试、未主动刷新或控制共享 Unity，未做协议运行对比或 Player 分配采样。

## 2026-09-20 Egress 高层消息类型装箱清理

对应 tasks.md 的 5.32，与代码同步提交。

- ReadGameplayFact 原反射读取 GameplayFactKind 后再 switch，已核对当前全部七种正式成员均有解码分支。现直接转换后进入原 switch，未知字节在读具体字段前抛出原泛型校验的 InvalidDataException 数值文本。
- ReadPresentationCommand 的类型改用显式列出当前十二种成员的 switch 校验，在读取 producerId 前拒绝其它值，保留原异常文本。TimelineProgress 成员继续沿原 AbilityTimelineProgressCodec，不改其字段、播放或事件消费边界。
- 两处不再调用 Enum.ToObject／Enum.IsDefined，不建立额外运行缓存。Action transition／phase／state 和效果操作／应用模式仍使用原泛型入口，此次不宣称 Egress 枚举分配全部完成。
- ServerAuthoritative portable 连带 Core／Float32 编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过。编辑前目标文件无其它未提交修改且未发现 csc／bee 编译进程；未新增测试、未主动刷新或控制共享 Unity，未做协议运行或 Player 分配采样。

## 2026-09-20 基线编码与检查点重复构造清理

对应 tasks.md 的 5.34，与代码同步提交。

- AuthoritativeActorBaseline 已有正式 ReadOnlyMemory CharacterStateBytes。CanonicalCodec.WriteBaseline 现在通过其 Span 同步写入，删除 CopyCharacterStateBytes 的编码中转；不新增访问接口，基线构造的独立克隆及预测恢复消费者继续保留。
- NetworkCheckpointCodec.Capture 原先创建一份 checkpoint 交 layout.Require，再重新复制相同 baseline 字节并构造第二份结果。现构造一次，完整校验后返回该对象。已核对 Require 仅验证身份并读取 checkpoint.StateBytes 的副本进行解码／哈希检查，不修改或保留 checkpoint，因此无需再生成第二个同内容对象。
- 减少每次基线编码的一份状态字节副本，以及 Capture 第二次基线复制、checkpoint 自身复制、对象及哈希过程。仍保留首次构造的独立数据、校验解码与错误；未改历史存储、回滚恢复或事务归还边界。
- ServerAuthoritative portable 连带 Core／Float32 编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过。编辑前目标文件无其它未提交修改且未发现 csc／bee 编译进程；未新增测试、未主动刷新或控制共享 Unity，未做检查点运行对比或 Player 分配采样。

## 2026-09-20 紧凑检查点固定哈希字段缓冲清理

对应 tasks.md 的 5.35，与代码同步提交。

- NetworkCheckpointCodec.WriteHash 原为每个哈希创建 byte[32]，现使用固定 32 字节栈缓冲，仍按原高低半字节规则解码 64 位十六进制字符，再同步原样写流，不加长度前缀。
- CanonicalWriter 增加实际供该调用使用的 ReadOnlySpan 原始写入入口；原 byte[]／offset／count 入口保留现有消费者和参数检查。CanonicalReader 增加 ReadRawBytesSegment，按负长度及剩余字节检查后推进游标，已有 ReadBytesSegment 共用它；需要独立数组的 ReadRawBytes 消费者保持不变。
- ReadHash 原先复制 byte[32]，填 char[64]，再生成字符串。现借用 32 字节片段，通过无捕获 string.Create 回调直接填最终 64 字符字符串，严格使用片段 Offset，保留小写格式及 StableHash 校验。回调同步结束后不保留源包，最终字符串仍独立持有。
- 每个固定哈希写入少一个字节数组，读取少一个字节数组和字符数组；最终字符串及其它消息存储仍分配。协议字段、前缀和 checkpoint 恢复生命周期不变。
- ServerAuthoritative portable 连带 Core／Float32 编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过。编辑前目标文件无其它未提交修改且未发现 csc／bee 编译进程；未新增测试、未主动刷新或控制共享 Unity，未做协议运行对比或 Player 分配采样。

## 2026-09-20 Egress 剩余枚举解码装箱清理

对应 tasks.md 的 5.33，与代码同步提交。

- 按当前 Float32 正式声明逐项核对动作转换 8 个、动作阶段 5 个、动作状态 9 个、效果操作 11 个、应用模式 2 个成员。分别用强类型 switch 匹配替换原 ReadEnum 调用，不以连续数字范围代替成员声明，也不生成运行缓存。
- 未知 byte 在原调用位置抛相同 InvalidDataException 文本；合法零值和各成员保留，读取次序不变。此次只改序列化校验，不修改动作推进、效果执行或任何生命周期边界。
- 五个消费者迁移后删除泛型 ReadEnum；源码检索确认本 codec 无 Enum.ToObject／Enum.IsDefined。此结论只覆盖 codec 自身，不能推广到构造函数、嵌套 codec 或整条事件链。
- ServerAuthoritative portable 连带 Core／Float32 编译零警告零错误，按规定构建后 shutdown 成功，diff 空白检查通过。编辑前目标文件无其它未提交修改且未发现 csc／bee 编译进程；未新增测试、未主动刷新或控制共享 Unity，未做协议运行或 Player 分配采样。

## 2026-09-20 效果状态剩余值视图遍历清理

对应 tasks.md 的 2.21，勾选仅表示该源码切片完成，Float32 编译尚未通过。

- 两数值域 CollectOwnedTags／CopyOwnedTags、AddModifier、RemoveModifiersByEffect 和 ValidateRuntimeClosure 改为遍历具体 SortedDictionary 的 KeyValuePair，再读取 Value。私有 CollectOwnedTags 唯一调用者是聚合状态构造函数，输入本就是自有 SortedDictionary，因此收紧私有参数类型以删除接口枚举器装箱；公开合同不变。
- 保留字典顺序、标签去重及 Ordinal 排序、modifier 重复检查、逐项移除和每次重算顺序、活动效果引用检查；没有共享可变快照或改变工作状态、事务及 scratch 寿命。
- 删除上述入口对 Values 视图的创建需求，不将缓存视图误报为每次必分配。SortedDictionary 枚举内部存储、标签集合与最终数组仍可能分配，未新增池或容量配置，不宣称全链 0 GC。
- 编辑前两目标文件和本治理文档没有其它未提交修改，未发现 csc／bee 进程。Fixed portable 编译零警告零错误；Float32 portable 因另一个任务未跟踪的 Float32GraphValueRuntime.cs 第 270 行起语法错误失败，共 37 个错误，读取现场确认存在方法外语句及截断字符串，未修改该文件。两次构建均执行 build-server shutdown 成功。
- 本切片 diff 空白检查通过；未新增测试、未控制或刷新共享 Unity、未做 Player 采样。Float32 的编译证据待共享 GraphValueRuntime 修复后补齐，整体目标继续未完成。
## 2026-09-20 权威主机路由身份校验清理

对应 tasks.md 的 5.42。

- ServerAuthoritativeAuthorityHostIdentity 构造及 IsValid 原通过 Enum.IsDefined(Type, object) 检查路由，产品描述构造重复同一规则。统一调用身份类型的内部 IsValidRouteKind，明确接受 ExternalAuthorityWorker／InProcessAuthorityScene，其它值（包括默认零值）继续拒绝。
- 读取正式消费者确认 IsValid 由端点合同构造、控制会话主机验证以及 FantasyEndpointRuntime 的就绪判断和主机身份比对调用；本次仅修改公共身份和产品描述文件，不修改网络框架、会话推进或路由行为。没有声称这些调用每帧都执行，频率未采样。
- 三处删除枚举参数装箱及反射校验，保留短路次序、房间和产品身份条件、异常文本、公开合同、序列化和哈希内容。不存在新增存储、缓存或容量配置，进程角色等其它枚举校验未纳入此次完成范围。
- 编辑前两个目标文件无其它未提交修改，未发现 csc／bee 编译进程。Core portable 编译零警告零错误，build-server shutdown 成功，本切片 diff 空白检查通过。未新增测试、未控制或刷新共享 Unity、未做 Player 采样。
- 同轮复查上一切片 Float32 编译，仍在并行 Float32GraphValueRuntime.cs 第 270 行起失败（37 个语法错误），构建服务已清理，未修改该现场。2.21 的 Float32 编译证据仍待补齐，不阻止本次独立 Core 源码改动。
## 2026-09-20 描述及身份哈希枚举格式化清理

对应 tasks.md 的 5.43。

- 全部公共 Core 源码的十处 Convert.ToUInt64(enum, CultureInfo.InvariantCulture) 均用于哈希输入字符串，分别位于会话组合描述、Pipeline 描述、执行后端描述、权威产品描述以及 Pipeline／世界兼容身份。改为 checked((ulong)value).ToString(CultureInfo.InvariantCulture)，删除 Convert 的 object 参数装箱，沿用现有最终字符串及 StableHash 链。
- WorldCapability 底层为 ulong，后端 Capability 为 ushort，WorldFeature 底层为 int。保留 checked 转换，使 WorldFeature 非法负值仍以 OverflowException 拒绝，而不是转换成大的正数改变哈希；不承诺框架生成的异常文本逐字相同。合法值及组合标记的十进制输入不变，没有替换为枚举名称。
- 权威 Pipeline 身份由 SessionConfigurationDefinition 构造，世界身份由端点构造／解析入口产生；其余为描述准备入口。本轮清理的是这些构造的重复装箱，未证明逐帧频率，不计为稳态逐帧收益。最终数字字符串、哈希拼接及描述对象仍分配，未改变身份协议或增加缓存。
- 五个目标文件修改前均无其它未提交修改，修改前未发现 csc／bee 编译进程。Core portable 编译零警告零错误，build-server shutdown 成功，修改范围 diff 空白检查通过；未新增测试、未操作共享 Unity、未做哈希运行对比或 Player 分配采样。
## 2026-09-20 运行时诊断记录枚举校验清理

对应 tasks.md 的 7.6，并补齐 2.21 的 Float32 编译证据。

- Fixed／Float32 SimulationSessionContracts 中四类诊断记录及 Pipeline Pass 阶段校验原调用 Enum.IsDefined(Type, object)，每次到达该校验都会装箱枚举。改为 C# 9 常量模式直接匹配声明成员：边界 14 种、Pipeline 14 种、模型 6 种、世界 4 种、阶段 4 种。默认零值和其它非法值继续拒绝，不使用可能接纳编号缺口的数值范围。
- 保留各检查的原位置、短路次序、异常文本，Pass 阶段仍仅在 PassCompleted／PassFailed 时校验。构造字段、数值域、发布接口、订阅者及诊断开关均未改变，没有新增日志链、分配缓存或跨步所有权。
- 已检索到正式生产者：FixedPipelineTransaction、DotRecastWorldSolver、DeterministicKccWorldSolver.Diagnostics、RollbackOutputCommitter 和权威／预测 Pass。记录本身是 struct，本轮不把 struct 的 new 视为对象分配；调用方字符串、发布存储及采样器内部开销仍未治理和实测。
- 两文件修改前无其它未提交修改，未发现 csc／bee 编译进程。Fixed／Float32 portable 分别零警告零错误，每次 build-server shutdown 成功，本切片 diff 空白检查通过。并行 GraphValueRuntime 已恢复到可编译状态，未由本任务修改，因此 2.21 的 Float32 编译阻断已解除；历史失败证据保留。
- 未新增测试、未主动刷新或控制共享 Unity、未做运行诊断对比或 Player 分配采样；7.1 等整体验收范围继续未完成。
## 2026-09-20 Fixed 输入回放哈希中间列表清理

对应 tasks.md 的 7.7。

- FixedCharacterInputTraceModule.Replay 每次取到录制帧后调用 ComputeInputHash(frame.Input)，将结果保存到该帧 ReplayFrameBuilder。原计算先构造 List<string>，填充后 ToArray 交 StableHash.Compute；现按原容量公式直接建立最终数组并用下标填写。
- 源码逐字段核对：头部 5 项，每个 SimulationInputValue 15 项，每个请求 5 项，数组长度为 5＋Values.Count×15＋Requests.Count×5。字段及遍历顺序、InvariantCulture、布尔文本、空值处理和 StableHash.Compute 均保持原路径，不改变回放推进、证据写入时机或提前计算输入哈希。
- 每次删除一个 List 对象和其存储到最终数组的复制，保留一个最终字符串数组。数字格式化字符串、params 哈希内部拼接／UTF-8、回放输入重映射和记录增长仍存在。该入口是 Fixed 回放证据链，Float32 输入记录模块没有同名哈希计算，不为了对称增加入口。
- 同时检查 Float32SimulationDiagnosticsAggregate：发布使用已有按 Actor 索引的字典与有序列表，循环按下标读取；初始化集合不是每次发布创建，本轮不改该正确发布路径。诊断最终存储和容量治理仍需继续追踪。
- 编辑前目标文件无其它未提交修改，未发现 csc／bee 编译进程。Fixed portable 编译零警告零错误，build-server shutdown 成功，本切片 diff 空白检查通过；未新增测试、未控制或刷新共享 Unity、未运行回放或 Player 分配采样。
## 2026-09-20 回放来源身份按回放寿命复用

对应 tasks.md 的 7.8，源码完成，编译检查受公共依赖阻断。

- Fixed／Float32 的 Remap 原每帧插值数值域前缀和 s_Replay.TraceId，Fixed 的 HoldLastReplayFrame 也重复生成同一字符串。现在 PrepareReplay 在原 trace 校验和绑定后构造 s_ReplayInputSourceIdentity，三个消费者直接传入该字符串；前缀、TraceId 和 SimulationInput 构造校验保持不变。
- Fixed ResetState 与 Float32 ResetActiveState 清除模块引用。普通结束、停止、清理和下一次准备仍走原入口；Fixed 暂停／保持期间保留本次来源身份。已返回 SimulationInput 持有不可变字符串，不会因模块重置变为空，也不共享可变请求数组。
- 存储上限是每个既有回放模块一个活动字符串，内容长度来自原 trace 身份，不建立字典缓存或新增配置。只移除重复身份插值；每帧 SimulationInput、请求重映射数组、状态消息和哈希分配仍存在。
- 编辑前两个目标文件无其它未提交修改，曾观察到 bee_backend PID 35648；等待后该句柄消失，再次进程扫描无 csc／bee 才编辑。Fixed／Float32 portable 均在公共 Core 编译失败，CameraProgramRequestFactory.cs 的 ProgramConstantKind／ProgramConstant／GameplayAbilityExecutionLayout 等类型引用报 8 个错误；未修改该文件或增加编译旁路，两次 build-server shutdown 均成功。
- 本切片 diff 空白检查通过；未新增测试、未主动刷新或控制共享 Unity，未运行回放或采样。后续需在公共依赖恢复后补齐两数值域编译证据，历史已通过的检查不冒充本次通过。
## 2026-09-20 空输入集合数组复用

对应 tasks.md 的 5.44，并补齐 7.8 编译证据。

- RollbackInputCodec 和 ServerAuthoritativeCanonicalCodec 的输入解码，原在计数为零时仍各 new 值数组和请求数组；Fixed／Float32 回放 Remap 也为无请求帧创建零长度数组。六处改为计数为零使用 Array.Empty，非零仍分配独立数组并执行原填写循环。
- 两数值域 SimulationInput.Copy 已对零长度 ICollection 返回 Array.Empty；此次消除的是到达该正确入口前的空数组。保留读入计数及上限校验、读取顺序、请求 Tick 重映射、排序／重复检查、canonical 比较和结果隔离。空数组无可修改元素，无需租用或归还，不改非空请求存储。
- 先前观察到 bee_backend PID 125504 和 csc 所在 dotnet PID 81640，等待两句柄结束并再次扫描无编译进程后编辑。四目标文件修改前无其它改动。DeterministicRollback 与 ServerAuthoritative portable 连带两数值域编译零警告零错误，构建服务关闭成功；本切片 diff 空白检查通过。
- 本轮公共 Core 类型引用阻断已解除，未由本任务修复；7.8 两数值域回放身份改动现已包含在通过的编译内。未新增测试、未控制共享 Unity、未做回放／协议运行对比或 Player 分配采样，输入对象及非空集合复制仍未完成治理。
## 2026-09-20 预测请求中转与恢复所有权复核

本节为任务 5.2／5.4 的调用链证据，未将父任务勾选完成，本轮不修改预测事务代码。

- PredictionIngressAndSchedulePasses 的 CaptureCheckpoint 调用 CaptureCorrectionCheckpoint，并返回持有该 checkpoint 的恢复回调；Execute 则取输入、计算当前步数并调用 ScheduleRequests。本轮确认了两个入口的共享状态依赖，未进一步核对外层事务调用两者的完整时序。零步时请求保留在 ConfirmationState，消费时按序生成独立数组并 Clear 内部 pending 字典。
- 同步下游 WithRequests 将该数组传入 SimulationInput，构造再次复制并排序，随后创建 OwnerCanonicalInputBatch；因此非空调度路径确有中转数组。但把返回值改成共享列表或字典视图会影响 Clear 后的可见内容，必须同时迁移生产者、输入构造及所有使用者，不能只删 ToArray／new。
- 确认状态还被 PrepareAck、PrepareBaseline、Capture 使用；checkpoint 当前复制 pending 请求至只读列表。Restore 先完整建立新 SortedDictionary，再替换字段，避免重建失败时破坏原 pending。快照不仅用于编码，也由事务恢复回调持有，不能用当前可变字典代替。
- pending 正式容量来自 requestCapacity，RetainRequest 在加入新序号前检查上限并拒绝同序号内容变化。容量为未来有界存储提供依据；目前 SortedDictionary 的逐节点分配、消费结果及 checkpoint 复制仍在，不能仅凭有容量上限视为 0 GC。
- 业务取舍：保留独立快照会继续产生复制，但失败时可恢复消费前的请求和确认游标；改为准备好的事务工作存储可以减少复制，但必须一起落实提交／失败恢复的存储归还边界。该范围触及当前目标明确要求保留的共享事务／回滚边界，因此保留现场，后续独立治理不以旁路缓存绕过。
- 实际检查文件为 PredictionConfirmationState、PredictionIngressAndSchedulePasses、PredictionState 和 PredictionStateCodec；codec 的独立解析字典还负责拒绝重复序号并排序，删除它不能省略这两项行为。本轮仅记录源码证据，无新增编译、运行或 Player 采样结论。
## 2026-09-20 诊断实时状态淘汰节点复用

对应 tasks.md 的 7.9。

- RuntimeLiveStateStore.Upsert 对已有 key 原已复用 LinkedListNode 并移到尾部，该正确路径保持不变。新 key 到达 m_MaxChanges 时，旧实现 RemoveFirst 后 AddLast(key) 创建新节点；现取出最旧节点，删除旧 key 的两个字典项，替换节点 Value 后 AddLast(node)，新 key 索引保存同一节点。
- 节点只存在于私有 m_Recency 和 m_RecencyNodes，ReadSince 返回独立 RuntimeLiveStateChange 值记录与列表，不暴露节点，故复用不改变已返回结果。旧 Value 被覆盖，不额外保留旧状态引用；顺序、m_LastEvictionVersion、m_EvictedStates、m_Version、changes 队列及全量同步判定均沿原逻辑。
- 节点数量上限沿既有 m_MaxChanges，未增加配置、池或旁路存储。未满时仍创建节点；Clear 保留原清除行为，之后重新填充仍分配。字典／队列增长、变化读取列表、事件 payload 和捕获段仍未完成治理，不宣称诊断稳态整链 0 GC。
- 目标文件编辑前无其它未提交修改，编辑前未发现 csc／bee 编译进程。使用当前 Unity BTSMTL.Diagnostics.rsp 的引用与源码独立调用 csc，输出及临时 rsp 位于系统 TEMP，退出码 0，无诊断输出；未改 Assets 编译产物、未刷新或控制 Editor。diff 空白检查通过，未新增测试或执行 Player 分配采样。
## 2026-09-20 捕获读取结果按实际数量准备

对应 tasks.md 的 7.10。

- RuntimeCaptureStore.ReadSince 原从头过滤到无初始容量的 List，增量较多时反复扩容复制。m_Changes 只按递增 revision 追加，TrimToCapacity 只删前缀，Dispose 清空；被丢弃的事件不加入列表，可能产生 revision 缺口但不改变顺序。现二分定位首个 Revision 大于 cursor 的位置，再 GetRange 一次取得独立后缀列表。
- 保留 cursor 大于等于版本的空结果返回、最早可读版本及淘汰版本触发的全量同步分支。没有按 revision 差值推算数量，因而不依赖编号连续；GetRange 仍返回独立 List，不借用后续可被截断的 m_Changes。
- CollectAllChanges 保留逐 segment 的原顺序，只以 m_Changes.Count 初始化容量。成功收录事件同时加入 segment.Events 与 m_Changes，超限丢弃两者都不加入，段淘汰同时删对应前缀，因此该数量覆盖现有段事件总数；不引入最大容量级别的过量预分配。
- 减少读取过程中结果列表增长的中间数组与复制，最终 List／数组、Freeze 快照和采集段自身仍分配。没有改变捕获发布、Timeline 播放、事务或存储所有权，不宣称捕获链 0 GC。
- 编辑前目标文件无其它未提交修改，未发现 csc／bee 编译进程。沿当前 Unity BTSMTL.Diagnostics.rsp 独立 csc 编译到系统 TEMP，退出码 0、无诊断输出；diff 空白检查通过。未新增测试、未操作共享 Unity、未执行捕获读取运行对比或 Player 采样。
## 2026-09-20 实时状态读取独立数组

对应 tasks.md 的 7.11；本节为调试面板读取开销，不能当成 Player 每帧收益。

- RuntimeLiveStateStore.ReadSince 的全量分支直接建立 m_Current.Count 长度数组，按原字典遍历顺序填写；增量分支使用 m_Version−cursor 作为长度，按原变化队列次序填充。删除两个 List 对象及增量列表扩容，返回合同仍为 IReadOnlyList，结果仍独立持有。
- 数量推导仅用于实时状态：每次成功 Upsert 恰好增加一个 revision 并加入一条变化，等价状态不推进版本；Clear 虽推进版本但设置 m_LastEvictionVersion，旧 cursor 会进入原全量分支；队列淘汰也由 earliestAvailable 分支拦截。因此进入增量分支时差值等于保留变化数且不超过队列容量。保留 checked 转换，不对捕获存储应用该推导，捕获可能有丢弃事件造成编号缺口。
- 已确认 RuntimeDebugTargetProvider.ReadLiveStateSince 的结果使用 Count 和下标，没有依赖 List 的转换。源码已存在 Array.Empty 返回，所以数组并非新的公开结果类型约束。全量空状态仍可创建空数组，本轮未宣称读取零分配。
- 同轮核对 RuntimeCaptureSnapshot.GetEvents 的三个调用点均在 Editor（RuntimeExecutionTimeline 两处、RuntimeDebugTargetProvider 一处）；本轮未改 GetEvents，后续应将这些读取和运行采集开销分开计量。
- 编辑前目标文件无其它未提交修改，未发现 csc／bee。使用 Unity 现有 BTSMTL.Diagnostics.rsp 独立编译到系统 TEMP，退出码 0、无编译诊断；diff 空白检查通过。未新增测试、未操作共享 Editor、未采样 Player 或面板刷新分配。
## 2026-09-20 诊断发布入口与实例来源映射容量复核

本节补充任务 7.1 的运行发布证据，不将整项勾选完成。本轮仅审计，没有修改代码或新增编译结论。

- RuntimeDiagnosticsStore.Publish 在 m_Gate 锁内读取已有 m_LiveChannels／m_CaptureChannels 和捕获等级后分派；ShouldPublish／IsInterested 同样读取已有位标记。RecalculateLiveChannels 只由 AcquireInterest／ReleaseInterest 调用，不能将兴趣字典 Values 的查询成本报告成每条事件都发生；本轮保留正确的发布分派。
- RuntimeDiagnosticsContext 的 SourceKey Publish 重载在兴趣检查后解析 handle，并以有效 runtimeInstance 更新 m_InstanceSources；SourceHandle 重载在 source 无效而实例有效时读取该表。当前表没有正式容量，只有 AdoptRuntime 清空；PopRuntimeInstance 只弹栈，SetExecutionBranch 只清时钟信息和实时状态，并不清该映射。
- RuntimeInstanceKey 是 struct，实现强类型 Equals/GetHashCode；Character 等工厂直接构造值类型，不能因 new 关键字列为托管分配。其身份同时包含 ActivationGeneration、TimelinePlaybackId、ActionInstanceId 和 InvocationGeneration 等，新代次可产生新字典键，因此长时间事件流可能导致映射增长；本轮没有运行数据证明具体增长速率或泄漏量。
- 该映射用于补齐后续无 source 的事件来源。按大小随意淘汰、在 Pop 时删除或分支切换时清空，可能让迟到／结束事件丢失来源；正确迁移需要明确各实例最后事件与释放边界，覆盖动作、技能、Timeline／TreeClip 和上下文切换。该范围与并行播放生命周期相交，保留现场，不能用任意固定上限或第二来源缓存绕过。
- 现有 live 状态 m_MaxChanges 和 capture 的 maxSegments／maxEvents 只约束各自存储，不能当作 m_InstanceSources 的容量依据。后续 7.1 应把上下文实例映射和实例栈的准备容量、代次结束归还纳入同一生命周期改造；本轮没有新增配置、手动验证任务、Editor 操作或 Player 采样。
## 2026-09-20 Canonical 字符串编码固定栈缓冲

对应 tasks.md 的 5.45。

- CanonicalWriter.WriteString 原 GetByteCount 后按整字符串 UTF-8 长度租借数组，编码、写入再归还。现保留 GetByteCount 和原四字节长度前缀，使用一次 768 字节栈缓冲，循环处理最多 256 个 UTF-16 code unit；每块编码后同步写入已有 MemoryStream，不租借托管中转数组。
- 容量推导：默认 UTF-8 对单个普通／替换字符最多三字节，合法代理对占两个 code unit、输出四字节，256×3 足够。若块尾是高代理且下一字符是低代理，本块缩短一位，下一块完整处理该代理对；不按字符任意拆字节，不分裂合法 Unicode 字符。null／空串仍写零长度后返回。
- 源码核对了普通字符、代理对恰跨边界、孤立代理及尾块的控制流，未执行字节对比测试。沿 Encoding.UTF8 的既有无效字符替换语义；编码器处理非法输入的内部辅助分配没有采样，不宣称任意字符串全链零分配。
- 取舍：固定栈占用与字符串总长度无关，删除池未命中和大字符串中转数组；长串会有多次同步流写入，CPU／吞吐影响未测。MemoryStream 扩容、writer 对象及最终输出仍分配，不改变外部流持有关系或另建编码入口。
- 目标文件编辑前无其它未提交修改，未发现 csc／bee。Core portable 编译零警告零错误并关闭构建服务；另沿 Unity 当前 Core.rsp 独立编译至系统 TEMP，退出码 0、无诊断输出，确认 Span 编码 API 可用。diff 空白检查通过，未新增测试、未刷新或控制共享 Editor、未做协议运行或 Player 采样。
## 2026-09-20 Canonical 空字节读取复用

对应 tasks.md 的 5.46。

- ReadRawBytes 原无论长度是否为零均 new byte[length]，随后 BlockCopy。现保留负长度检查和 Require(length)，仅在长度为零时返回 Array.Empty<byte>；其余分配、复制和游标推进完全不变。零长度原游标加零，新分支也不推进。
- ReadBytes 已通过同一 ReadRawBytes 实现，所有消费者自然覆盖，不新增替代解码入口。检索到效果／装备绑定、语义与执行数据、世界状态及快照等消费者；这些场景允许空字节时才减少分配，不将所有读取一概称为空或逐帧调用。
- 空数组无可变元素，不影响独立非空载荷的所有权；后续业务对空载荷的拒绝仍由原消费者执行。ReadString 已直接对原数组片段 GetString，无中间字节数组，本轮保留该正确路径。
- 目标文件修改前无其它未提交修改，未发现 csc／bee。Core portable 编译零警告零错误并成功关闭构建服务；diff 空白检查通过。未新增测试、未操作共享 Unity，未做边界运行对比或 Player 分配采样；非空字节复制、返回字符串、reader 对象等仍未完成治理。
## 2026-09-20 效果状态子块同步片段解码

对应 tasks.md 的 2.22。

- Fixed／Float32 GameplayEffectStateAggregateCodec.Read 的标签、属性、活动效果、周期、预测日志五个子块，原各 ReadBytes 复制完整数组后创建内部 reader。现调用已有 ReadBytesSegment，五个私有解码函数及唯一 Reader 工厂统一接收 ArraySegment，删除每次完整效果状态读取的五个中转数组，两数值域共十处。
- 子 reader 沿已有片段构造记录 Offset／End，头部 Magic、版本、数量上限、顺序／重复／目录检查以及各块 RequireComplete 均保留。私有工厂不再保留不可达的 byte[] null 检查；片段由已校验的外层 reader 产生，CanonicalReader 仍拒绝无底层数组的片段。
- 解析只产生独立字符串、值对象及集合，没有存储 reader 或 ArraySegment；后续 aggregate 构造、SimulationGameplayEffectState 校验和 Freeze 深拷贝保持原样。不将借用字节扩展至快照结果，不改变提交／恢复或并行执行帧的存储归还边界。
- 写入侧五个子 writer／数组及读取侧结果集合、深拷贝仍存在，本次仅清理同步解析字节中转，不宣称效果快照无分配。外层读取游标仍先消费完整子块，再由私有 reader 解析，不更改失败时外层推进次序。
- 编辑前两个文件无其它未提交修改，未发现 csc／bee 编译进程。Fixed／Float32 portable 分别零警告零错误，逐次关闭构建服务成功，diff 空白检查通过；未新增测试、未操作共享 Unity、未做状态恢复运行对比或 Player 分配采样。
## 2026-09-20 效果状态子块直接编码

对应 tasks.md 的 2.23。

- 两数值域 WriteTags／WriteAttributes／WriteActiveEffects／WritePeriods／WriteJournal 原分别构造 CanonicalWriter 和 MemoryStream，ToArray 后再由外层 WriteBytes 复制。五个私有函数全部改为接收外层 writer，BeginLengthPrefixedBlock 保留四字节长度位置，沿原字段写入后 EndLengthPrefixedBlock 回填，不保留旧返回数组入口。
- 每次完整效果状态写入删除五套子 writer／流／输出数组及数组到外层的复制。Magic、StateVersion、每个字段与集合顺序以及末尾 ChangeCursor 均保留；沿公共长度回填实现恢复流末尾位置，不使用闭包或另外的序列化协议。
- 实际上层调用是两数值域 CharacterRuntimeStateCodec 内部 effectWriter，失败则异常离开 using 作用域，没有发布部分结果。新方式失败时 effectWriter 可能已有未完成子块，未承诺失败后的流内容相同；现有调用者不捕获后继续使用它。最终角色封装仍有 effectWriter.ToArray，本次未迁移外层。
- aggregate.CopyTo 的工作集合和深拷贝、外层结果及流扩容仍存在；不改状态提交、恢复、归还或 Timeline 执行帧边界。读取继续沿上一小步的片段接口，尚未做编码字节对比或状态往返运行。
- 编辑前两目标文件无其它未提交修改，未发现 csc／bee。Fixed／Float32 portable 分别零警告零错误，逐次关闭构建服务成功，diff 空白检查通过。未新增测试、未刷新或控制共享 Unity、未采样 Player 分配。
## 2026-09-20 角色状态效果封装直接读写

对应 tasks.md 的 2.24。

- Fixed／Float32 CharacterRuntimeStateCodec.WriteCanonical 的效果分支原建 effectWriter，完整编码后 ToArray 再 WriteBytes。现在角色 writer 预留长度，调用同一个 GameplayEffectStateAggregateCodec.Write 后回填长度；读取分支由 ReadBytes 改为 ReadBytesSegment，再构造有限片段 reader。
- 保留效果存在标记、四字节外层长度、内部五个子块及 ChangeCursor 次序，effectReader.RequireComplete、效果目录安装检查和结果构造保留。嵌套长度回填均记录各自位置，结束后回到原末尾；Write 与 ComputeHash 继续共用唯一 WriteCanonical，未新增协议或快照路径。
- 删除效果整体封装的一套 writer／流／最终中转数组，以及读取的一个整体字节副本。结果不保存输入片段，聚合状态仍独立；序列化异常沿原外层 using 离开，不发布部分字节。未改变恢复、事务或 Timeline 快照执行逻辑。
- 角色最终数组／哈希、效果 CopyTo 深拷贝、装备与控制状态封装仍存在，不宣称角色状态全链无分配。目标文件修改前无其它未提交修改，未发现 csc／bee，修改仅限效果分支。
- Fixed／Float32 portable 各自零警告零错误，逐次 build-server shutdown 成功，diff 空白检查通过；未新增测试、未操作共享 Unity、未做哈希字节对比或恢复运行及 Player 分配采样。
## 2026-09-20 角色状态装备封装直接读写

对应 tasks.md 的 2.25。

- 两数值域 CharacterRuntimeStateCodec 的装备分支统一使用现有角色 writer 预留长度、调用唯一 EquipmentStateAggregateCodec.Write、回填长度；读取用 ReadBytesSegment 构造有限片段 reader，保留 equipmentReader.RequireComplete。删除装备子 writer、MemoryStream、ToArray 中转及读取整体副本。
- 检查公共 EquipmentStateAggregateCodec.Read：目录 hash、槽位数量／身份、装备贡献、局部状态类型及待定变更均沿原验证；读取产生字符串、数值、数组和 EquipmentStateAggregate，不保存字节片段。没有改变装备对象、安装／撤销流程或事务所有权。
- 存在标记、长度前缀、字段顺序、角色 Write／ComputeHash 共用 WriteCanonical 均不变。失败沿外层 writer 的原 using 离开，不发布部分结果；不承诺异常后的临时流内容相同。控制状态、Timeline 快照和装备内部 LINQ／集合复制未在本轮修改。
- 编辑前两目标文件无其它未提交修改，未发现 csc／bee；Fixed／Float32 portable 分别零警告零错误，逐次关闭构建服务成功，diff 空白检查通过。未新增测试、未操作共享 Unity，未做字节对比、装备恢复运行或 Player 分配采样。
## 2026-09-20 装备状态枚举构造与解码规则统一

对应 tasks.md 的 2.26。

- EquipmentRuntimeStateValue 提供内部 IsValidKind，明确接受八个正式成员；值构造、EquipmentProgramLocalState 定义构造和 EquipmentStateAggregateCodec 值类型读取共用规则。PendingEquipmentChange 提供内部 IsValidState，构造和 codec 的变更状态读取共用 Pending／Committed／Cancelled 三成员规则。
- 删除 codec 的 ReadEnum 泛型反射入口，两个消费者分别进入强类型读取函数；原 Enum.ToObject／Enum.IsDefined 装箱不再发生于这些入口。默认零值仍拒绝，构造保留 ArgumentException，解码保留 InvalidDataException 及原类型名／字节错误文本，读取和短路顺序不变。
- 不删除构造层校验，因为直接构造与反序列化均为实际路径；只统一成员判断，不改装备事务、准备／提交／撤销行为和局部状态存储。其它装备枚举和准备编译器仍有校验分配，不宣称装备全链完成。
- 两目标文件修改前无其它未提交修改，编辑前无 csc／bee；最终 Core portable 编译零警告零错误并成功关闭构建服务，diff 空白检查通过。未新增测试、未操作共享 Unity，未做装备运行或 Player 分配采样。
## 2026-09-20 角色控制状态枚举解码清理

对应 tasks.md 的 2.27。

- CharacterControlRuntimeStateCodec 四处调用原 byte／ushort 泛型 ReadEnum，先 Enum.IsDefined 再 Enum.ToObject。现分别调用 ReadValueKind(byte) 与 ReadSemantic(ushort)，值类型接受 Boolean／Int32／UInt64／Identity，语义接受 ActiveState／EnteredTick／Transition／StateValue，其它值包括零继续拒绝。
- 删除两个无消费者的泛型入口，不引入另一解码路径。保留字段读取宽度、读取次序、InvalidDataException 和原错误文本（包括原标签重复措辞）；字段／schema 匹配、值解码、状态哈希及返回结果所有权不变。
- 这里消除的是枚举元数据查询与转换装箱，不是动态调用业务方法的反射。CharacterControlStateFieldDescriptor 等其他构造层仍有枚举检查，不能将 codec 清理推广为所有角色控制状态均无装箱。
- 目标文件编辑前无其它未提交修改，进程扫描无 csc／bee。SimulationGraphContracts.cs 存在并行修改，其中 scope／相机有效性检查未触碰。Core portable 编译零警告零错误，构建服务关闭成功，diff 空白检查通过；未新增测试、未操作共享 Unity、未做运行或 Player 分配采样。
## 2026-09-20 角色控制字段重复枚举检查清理

对应 tasks.md 的 2.28。

- CharacterControlStateFieldDescriptor 构造原先分别 Enum.IsDefined 检查 valueKind 和 semantic，再调用已有 IsValid(valueKind, semantic)。该组合判断已明确限定 ActiveState／Transition 只能 Identity、EnteredTick 只能 UInt64、StateValue 只能四种正式值类型，未知 semantic 返回 false；因此前两项检查完全冗余。
- 删除两次枚举装箱，保留 id.IsValid、现有组合判断及同一 ArgumentException。没有增加另一份枚举成员清单或弱化类型／语义关系，未知类型和零值仍被原组合判断拒绝。
- CharacterControlRuntimeStateCodec 的无外部 schema 读取分支会逐字段构造该描述，因此上一轮解码去装箱后这里仍是真实后续检查；同时覆盖直接构造和内容准备，不将所有构造次数都计为逐帧。
- 文件修改前无其它未提交修改，进程查询完成后确认无 csc／bee。Core portable 编译零警告零错误并成功关闭构建服务，diff 空白检查通过；未新增测试、未操作共享 Unity、未做运行或分配采样。字段对象、schema 与结果存储仍分配。
## 2026-09-20 角色控制状态规范编码比较清理

对应 tasks.md 的 2.29。

- CharacterControlRuntimeStateCodec.Read 在 schema／字段／状态 hash 校验后，原 Write(result) 生成完整 byte[]，再比较长度和每个字节。现直接新建局部 CanonicalWriter，调用已有 WriteCanonical，再使用正式 ContentEquals 比较原输入，删除仅供比较的 ToArray 副本。
- 不取消重新编码：公开 Write 与此校验仍共用唯一字段写入实现；ContentEquals 先比较总长度，再通过固定栈块比较所有字节，并恢复流位置。原非规范编码 InvalidDataException 文本保持不变，解析顺序、状态 hash 和返回对象不变。
- Read(bytes) 自建 schema 后进入同一 Read(bytes, schema)，两条公开读取路径均覆盖；角色 Fixed／Float32 codec 是实际消费者。公开 Write 的独立数组仍保留，schema 的重复解析、writer／MemoryStream、结果对象及非空数据存储仍分配。
- 目标文件修改前无其它未提交修改，编辑前无 csc／bee。Core portable 编译零警告零错误、build-server shutdown 成功，diff 空白检查通过。未新增测试、未操作共享 Unity、未执行状态往返或非规范输入对比及 Player 分配采样。
## 2026-09-20 角色控制状态直接嵌套写入

对应 tasks.md 的 2.30。

- 搜索 Assets 与 Tools 的 C# 调用，CharacterControlRuntimeStateCodec.Write 的两个消费者均为 Fixed／Float32 CharacterRuntimeStateCodec 内 WriteBytes 嵌套写入；上一轮 canonical 比较已不依赖返回数组。现以明确的 WriteLengthPrefixed(writer, state) 替换旧公开入口，迁移全部实际调用，删除旧 byte[] 返回实现。
- 新入口沿公共 Begin／EndLengthPrefixedBlock，使用原 WriteCanonical 写字段；解码后重新编码比较仍直接调用同一 WriteCanonical，不将长度前缀混入内部 canonical 比较。原角色控制存在标记、外层四字节长度、内部版本／身份／字段／hash 次序保留。
- 每次嵌入控制状态删除一套子 writer／流／最终数组及到外层复制。保留原 state null 拒绝，为新 writer 参数提供直接 null 拒绝；异常沿角色外层 using 释放，不发布部分结果。没有增加兼容入口或第二编码协议。
- 读取侧整体 ReadBytes 和两遍 schema 解析、控制状态对象与快照存储仍分配；未改事务生命周期或 Timeline 部分。三文件修改前无其它未提交修改，编辑前无 csc／bee。Fixed／Float32 portable 各自零警告零错误，逐次关闭构建服务成功，diff 空白检查通过。
- 未新增测试、未操作共享 Unity、未做字节对比或状态恢复运行及 Player 分配采样。
## 2026-09-20 运行期反射边界与 MotionWarp 持续执行校验

对应 tasks.md 的 2.31。

- 用户明确初始化反射可接受、运行中反射不可接受。后续按真实调用阶段区分：准备期反射不再单独作为清理目标；更新、事件、回放和状态恢复中的枚举元数据查询及反射调用继续治理，不以不发生于每帧为保留理由。
- Fixed／Float32 MotionRuntime.ApplyMotionWarp 经 ApplyMotionWarpCore 读取已有 MotionWarp 状态，ResolveLifecycle 后分 Initialize 与已有状态分支。已有状态分支原每次 Enum.IsDefined 校验 storedState.LimitResult，再排除 PreservedByLimitPolicy；因此该位置会随持续执行进入，不只是内容准备或一次恢复。
- 正式枚举只有 Applied=0、AppliedClamped=1、PreservedByLimitPolicy=2，现直接匹配前两项，拒绝其它值。保留原 Fail 调用和错误文本、零值合法性、进度读取与后续 EvaluateWarpPose／状态写入；不修改 Timeline 播放身份、生命周期判定或状态存储归属。
- 两文件编辑前无其它未提交修改，进程扫描无 csc／bee。Fixed／Float32 portable 分别零警告零错误，逐次 build-server shutdown 成功，diff 空白检查通过。未新增测试、未操作共享 Unity、未做 MotionWarp 运行或 Player 分配采样，实际执行频率和数值收益尚无采样证据。
## 2026-09-20 Fixed 输入源状态通知枚举校验

对应 tasks.md 的 2.32。

- FixedLocalInputSourcePort.NotifyStateDisposition 原 Enum.IsDefined 校验参数，再依绑定顺序通知各控制源。改为直接接受 Prepared／Committed／Discarded／Restored 全部四成员，其它值仍抛相同 ArgumentOutOfRangeException；没有根据当前调用点缩窄合法集合。
- 运行链证据：FixedLocalControlInputCheckpoint.Restore 在恢复输入后通知 Discarded，Dispose 在未恢复时通知 Committed；FixedLocalControlInputRestoreTransaction.Apply 恢复替换状态后通知 Restored。这些属于运行中的提交和恢复流程，不是初始化专用路径。
- 修改仅一处校验，不改检查点、恢复事务、通知消费者或状态数据生命周期。目标文件无其它未提交修改，编辑前无 csc／bee。最终 Fixed portable 编译零警告零错误并关闭构建服务，diff 空白检查通过；未新增测试、未控制共享 Unity、未做提交／恢复运行或 Player 采样。
## 2026-09-20 运行事务结果构造枚举校验

对应 tasks.md 的 2.33。

- Fixed／Float32 PipelineTransaction.Execute 在 coordinator 执行后每次构造各自 TransactionResult，原结果构造用 Enum.IsDefined 检查 Outcome。这是运行事务返回路径，不是初始化构造。
- 改为显式 Pending／Committed 两成员判断，拒绝零及其它非法值；保留 transactionIdentity 校验以及只有 Committed 才能有 commitBatch 的双向关联检查，异常文本和检查先后不变。
- 只修改两个结果类的成员判断，不改 coordinator、事务执行、快照、提交批次或恢复行为。结果 class 本身仍分配，本次删除枚举元数据查询和装箱，不声称事务无分配。
- 两文件修改前无其它未提交修改，编辑前无 csc／bee。Fixed／Float32 portable 各自零警告零错误，逐次关闭构建服务成功，diff 空白检查通过。未新增测试、未操作共享 Unity、未运行事务对比或 Player 分配采样。
## 2026-09-20 预测观测约束采样类型校验

对应 tasks.md 的 2.34。

- Float32 ObservedWorldConstraint 构造原使用 Enum.IsDefined 校验 samplingKind；实际生产者包括 PredictionHistory 的 ToObservedConstraint（构造观测约束帧）和 PredictionStateCodec.ReadObservedFrame（恢复历史）。属于运行中的预测／恢复数据，不是初始化专用描述。
- 改为直接匹配 Exact／Interpolation／ConstantVelocityExtrapolation，仍拒绝零及其他成员。保留 Actor 身份、目标和来源 Tick、前后 Body 一致性、来源时序与 contactShapeConfigurationHash 校验以及原异常；没有修改采样算法、预测历史寿命或回滚逻辑。
- 同轮搜索 CharacterControlAbilityStopRequest：存在类型声明、提交接口和处理者，但当前 Assets／Tools C# 检索没有实际构造调用证据。该入口暂未修改，不将文件名含 Runtime 或存在消费者直接推断为每帧构造。
- 目标文件修改前无其它未提交修改，编辑前无 csc／bee。ServerAuthoritative portable 连带 Core／Float32 编译零警告零错误，构建服务关闭成功，diff 空白检查通过；未新增测试、未操作共享 Unity、未做预测运行或 Player 分配采样。
## 2026-09-20 MotionWarp 恢复解码成员匹配

对应 tasks.md 的 2.35。

- 两数值域 CharacterRuntimeStateCodec.Read 经 ReadMotionWarpStates／ReadMotionWarpState 读取已保存的运动修正状态，原限制结果调用泛型 ReadEnum。现使用强类型 ReadMotionWarpLimitResult，直接匹配 Applied／AppliedClamped／PreservedByLimitPolicy 三个正式成员，删除该入口的枚举转换与元数据查询装箱。
- 恢复解码合法集合沿旧 Enum.IsDefined 保留三值，不能复用 2.31 活动执行只接受两值的条件。读取宽度、位置、非法字节 InvalidDataException 类型和文本、后续状态构造及 layout 校验均保持不变。
- 只迁移运动修正结果的两个实际调用点，不改变保存状态、回放或事务寿命。通用 ReadEnum 仍有动作／作用域／Timeline 等真实消费者，本轮保留，不宣称整个角色 codec 已无反射。
- 两目标文件修改前无其它未提交修改，编辑前无 csc／bee。Fixed／Float32 portable 分别零警告零错误，逐次关闭构建服务成功，diff 空白检查通过；未新增测试、未操作共享 Unity、未做恢复运行或 Player 分配采样。
## 2026-09-20 角色状态值解码复用分派校验

对应 tasks.md 的 2.36。

- Fixed／Float32 CharacterRuntimeStateCodec.ReadValue 原通过 ReadEnum 校验后 switch 解码。核对 ProgramStateValueKind 当前十一种成员均已有分支，现直接将读取 byte 转成枚举并进入该 switch，未知值在读取任何值载荷前由 default 拒绝，不增加另一份合法成员清单。
- 默认分支改为原 ReadEnum 实际抛出的类型名／字节 InvalidDataException 文本；保留合法成员包括 ActionTargetSnapshot=24，11 至 23 等缺口仍不接受。十一种具体值的读取、Blackboard token／stamp 和目标快照构造保持原样，不改变恢复状态或 Timeline 数据结构。
- 运行路径为角色状态恢复中的变量值读取，删除每个读取值的 Enum.ToObject 和 Enum.IsDefined；其它类型仍调用真实存在的通用 ReadEnum，因此该工具尚不能删除，也不宣称完整 codec 无枚举反射。
- 两文件修改前无其它未提交修改，编辑前无 csc／bee。Fixed／Float32 portable 各自零警告零错误，逐次关闭构建服务成功，diff 空白检查通过。未新增测试、未操作共享 Unity、未执行非法字节或状态恢复运行对比及 Player 分配采样。
## 2026-09-20 动作实例恢复枚举解码

对应 tasks.md 的 2.37。

- Fixed／Float32 CharacterRuntimeStateCodec 的动作实例读取改用 ReadActionPhase／ReadActionState／ReadActionTransition，分别列出正式 5／9／8 个成员，替换原三个 ReadEnum 调用。两数值域共六个恢复字段不再 Enum.ToObject／Enum.IsDefined。
- 保留 Startup、Requested、None 等合法零值；其他 byte 仍在原字段位置抛原类型名／数值的 InvalidDataException，未使用粗略非零或范围判断。实例字段顺序、构造、身份与上下文关联保持原样，不改变动作状态推进、Timeline 播放或回滚所有权。
- 运行入口为已有角色状态恢复的 ReadActionInstances，不将此项报告为每帧必经。Timeline 与 scope 的通用枚举读取仍有真实消费者，原泛型工具继续保留，完整 codec 尚未无反射。
- 修改前两目标文件无其它未提交修改，编辑前无 csc／bee。Fixed／Float32 portable 各自零警告零错误，逐次构建服务关闭成功，diff 空白检查通过；未新增测试、未操作共享 Unity、未执行动作恢复运行对比或 Player 分配采样。
## 2026-09-20 远端身体采样选择上游校验

对应 tasks.md 的 2.38，补充 2.34 的上游调用链。

- ServerAuthoritativePredictionHistory 中远端身体采样流程先生成 ServerAuthoritativeRemoteBodySelection，再通过 ToObservedConstraint 转成观测约束。2.34 已清理下游构造，但上游 selection 构造仍 Enum.IsDefined，现同样直接接受 Exact／Interpolation／ConstantVelocityExtrapolation 三成员。
- 已检索到生成 selection frame 的循环及实际 selection 构造入口，不是初始化目录。保留 Actor／Tick／Body 关联和来源时序检查、原 ArgumentException，未改采样算法、精确／插值／外推选择规则或历史持有关系。
- 再次核对 SimulationGraphContracts.cs 仍有并行未提交修改，scope 校验未介入。实际目标文件此前无其它修改，编辑前进程查询完成且无 csc／bee。ServerAuthoritative portable 连带 Core／Float32 零警告零错误，构建服务关闭成功，diff 空白检查通过。
- 未新增测试、未操作共享 Unity、未运行网络采样对比或 Player 分配采样；selection frame 数组及历史存储仍分配，不能将上下游两处校验清理等同整条预测链无分配。
## 2026-09-20 预测事件日志结果校验统一

对应 tasks.md 的 2.39。

- ServerAuthoritativeJournalEntry 新增内部 IsValidDisposition，明确匹配 PredictedCommitted／AuthorityConfirmed／SuppressedDuplicate／PredictedRejected。构造与 PredictionStateCodec.ReadJournal 共用该规则，删除两处 Enum.IsDefined，未知值和零继续拒绝。
- 运行调用包括预测日志确认时生成更新条目、PredictionState 发布条目及历史日志恢复；不是初始化专用校验。解码保留身份／Tick／sequence 和重复 eventId 检查，并继续抛原 InvalidDataException；直接构造保留原 ArgumentException，检查次序不变。
- 没有删除构造校验或改变日志确认／拒绝／去重行为，不改 checkpoint、历史保留或恢复的所有权。条目是 struct，本轮仅消除枚举查询装箱，字典与更新集合仍可能分配。
- 两目标文件此前无其它未提交修改，编辑前无 csc／bee。ServerAuthoritative portable 连带 Core／Float32 编译零警告零错误，构建服务关闭成功，diff 空白检查通过；未新增测试、未操作共享 Unity、未做事件运行或 Player 分配采样。
## 2026-09-20 运行期预测修正决策校验

对应 tasks.md 的 2.40。

- PredictionCorrectionDecision 由 PredictionReconciler 的基线比较分支及 PredictionIngressAndSchedulePasses 的无基线分支创建，属于运行调度／修正结果，非初始化策略定义。构造中的两个 Enum.IsDefined 改为直接匹配三种决策和七种原因。
- 完整保留 NoCorrection／RestoreReplay／HardRecovery 及所有正式 reason，零和未知值继续拒绝。baselineTick 有效性、决策与 restoreTick 的关联、回放范围成对有效及先后顺序检查和异常文本不变；不改变修正算法或状态恢复生命周期。
- ServerAuthoritativePolicy 中初始化策略的枚举检查未因本项修改，遵守初始化允许反射的边界。目标文件此前无其它未提交修改，编辑前无 csc／bee。
- ServerAuthoritative portable 连带 Core／Float32 编译零警告零错误，确认构建及 build-server shutdown 全部结束，diff 空白检查通过；未新增测试、未操作共享 Unity、未做运行决策对比或 Player 采样。决策对象自身仍分配。
## 2026-09-20 会话诊断按需构造校验

对应 tasks.md 的 7.12。

- Fixed／Float32 PassPipelineRuntimeHandle.Diagnostics getter 直接调用 BuildDiagnostics，每次读取重建组件列表与 SimulationSessionDiagnosticsSnapshot；不是只在句柄构造时生成。条目状态及快照 lifecycle／preparation 原分别 Enum.IsDefined，现按正式 5／5／3 成员直接匹配。
- 保留合法状态、非法零值拒绝、条目 ArgumentOutOfRangeException 与快照 ArgumentException、sessionId 校验及原检查次序。组件身份规范化、排序、重复检查和只读结果寿命不变，不缓存可变会话快照。
- 这里只确认按需 Diagnostics 读取会经过构造，尚未确认用户运行场景中的刷新频率，不报告为每帧必经或固定 GC 节省值。BuildDiagnostics 的插值、列表、快照对象以及 Phase.ToString 仍有开销，未纳入本次完成范围。
- 目标文件此前无其它未提交修改，编辑前无 csc／bee。Core portable 编译零警告零错误，构建服务关闭成功，diff 空白检查通过；未新增测试、未操作共享 Unity、未做诊断刷新运行或 Player 采样。
## 2026-09-20 会话诊断组件快照直接数组复制

对应 tasks.md 的 7.13。

- SimulationSessionDiagnosticsSnapshot 原复制 components 至 List，排序校验后保存其只读包装。现 ToArray 取得独立最终数组，Array.Sort 使用原比较规则，按 Length 执行原重复身份检查，最后 Array.AsReadOnly；null 使用 Array.Empty。调用方集合不会原地排序或暴露可变结果。
- 实际会话句柄 BuildDiagnostics 提供 List 输入，ToArray 可按集合数量直接复制，删除快照内部的 List 对象；原 List 的底层数组由最终数组替代，不将其算成额外减少一整份数组。只读包装继续存在，不改变 Components 对外行为。
- 一般 IEnumerable 输入仍可能在框架内部使用增长缓冲，最终数组／包装／快照和上游 BuildDiagnostics 的列表及插值仍分配。该路径为按需诊断读取，未声称 Player 每帧收益。
- 文件此前无其它未提交修改，编辑前无 csc／bee。Core portable 编译零警告零错误，构建服务关闭成功，diff 空白检查通过；未新增测试、未操作共享 Unity、未做诊断运行或分配采样。
## 2026-09-20 会话诊断生产数组按实际数量准备

对应 tasks.md 的 7.14。

- Fixed／Float32 PassPipelineRuntimeHandle.BuildDiagnostics 固定生产五个系统组件和一个 Pipeline 条目，再为每个 Pass 追加条目。原无容量 List 在固定六项初始化中已会增长；现直接建立长度 6＋m_Passes.Count 的数组，固定项写 0 至 5、Pass 写 6＋i。
- 顺序、每次 DiagnosticState 读取、各身份和 Phase 文本构造保持原样；没有跨查询缓存可变状态。下游 SimulationSessionDiagnosticsSnapshot 仍复制并排序，返回结果独立，生产数组在同步调用后不保留。
- 删除上游 List 对象及扩容中的额外数组／复制，不宣称去掉下游最终快照数组。正式数量直接来自既有 Pass 列表，无新容量配置或临时旁路；按需读取实际频率仍未采样。
- 编辑前两文件无其它未提交修改，进程扫描无 csc／bee。Fixed／Float32 portable 分别零警告零错误，逐次构建服务关闭成功，diff 空白检查通过。未新增测试、未操作共享 Unity、未做诊断运行或 Player 分配采样。
## 2026-09-20 Pass 阶段名称在组装时准备

对应 tasks.md 的 7.15。

- 正式 PassPipelineRuntimeHandle 由两数值域 PassExecutionBackend 创建，接收本次组装 runtimes.AsReadOnly；正式 Ingress／Schedule／Step／Egress 基类 Phase 返回固定阶段。句柄未提供替换 Pass 的运行入口，因此阶段名称可随句柄寿命保存。
- 两数值域构造新增按 m_Passes.Count 定长的 m_PassPhaseNames，每项初始化时执行原 Phase.ToString，BuildDiagnostics 直接读对应文本。初始化仍允许枚举格式化，运行诊断读取不再逐 Pass 查询枚举名称／装箱；输出文本沿原格式。
- 存储为每个句柄一个与正式 Pass 数一致的数组，没有按 tick 增长的缓存或新配置。生命周期状态、错误、最新 tick 和诊断条目仍每次读取生成，不缓存动态快照。身份插值、生产与结果数组等仍分配，未声称整个 Diagnostics 无分配。
- 两文件此前无其它未提交修改，编辑前无 csc／bee。Fixed／Float32 portable 分别零警告零错误，逐次关闭构建服务成功，diff 空白检查通过；未新增测试、未操作共享 Unity、未运行诊断刷新或 Player 分配采样。
## 2026-09-20 角色控制状态同步片段读取

对应 tasks.md 的 2.41。

- Assets／Tools C# 检索显示 CharacterControlRuntimeStateCodec.Read 的实际外部消费者为 Fixed／Float32 角色 codec 的控制状态分支。两处 ReadBytes 改为 ReadBytesSegment；控制状态的公开 Read、带 schema 的 Read 及私有 ReadSchema 统一接收 ArraySegment，移除旧 byte[] 签名而不保留兼容路径。
- 两次解析都经 CanonicalReader 片段构造使用 Offset／End，仍各自 RequireComplete；重新编码比较使用 bytes.AsSpan()，不会读入相邻装备／效果数据。底层数组不存在时由 CanonicalReader 拒绝，带 schema 的入口仍先检查 schema null。
- 返回 schema、值对象、字符串及 CharacterControlRuntimeState 均不持有输入片段。只删除嵌套控制状态的整体字节副本，保持解析顺序、hash 校验、状态深拷贝和事务所有权；两遍 schema／值解析仍未合并，结果对象仍分配。
- 三文件修改前无其它未提交修改，编辑前无 csc／bee。Fixed／Float32 portable 各自零警告零错误，逐次构建服务关闭成功，diff 空白检查通过；未新增测试、未操作共享 Unity、未执行片段边界／恢复运行对比或 Player 分配采样。
## 2026-09-20 完整角色状态规范比较清理

对应 tasks.md 的 2.42。

- Fixed／Float32 CharacterRuntimeStateCodec.Read 末尾原 RequireCanonical(bytes, Write(state), label)，Write 为比较生成完整角色状态数组。现局部 writer 调用唯一 WriteCanonical，再 ContentEquals 原输入；各文件只有一个 RequireCanonical 调用，迁移后删除旧数组比较函数。
- 保留完整重新编码与长度／逐字节比较，不以 hash 相同替代协议规范校验；两个数值域的原 InvalidDataException 文本保留。公开 Write 的真实独立结果消费者保持原实现，hash 路径也仍共用 WriteCanonical。
- 减少每次角色恢复校验的一份完整输出副本，不改变输入读取、返回状态、Timeline 字段、事务恢复或任何结果所有权。writer／流、聚合集合、最终状态对象和其它枚举读取仍有分配／查询，本项不是完整角色状态 0 GC。
- 两目标文件修改前无其它未提交修改，编辑前无 csc／bee。Fixed／Float32 portable 分别零警告零错误，逐次构建服务关闭成功，diff 空白检查通过；未新增测试、未操作共享 Unity、未做规范字节／恢复运行对比或 Player 分配采样。
## 2026-09-20 世界状态规范比较清理

对应 tasks.md 的 5.47。

- Fixed／Float32 WorldSimulationStateCodec.Read 原在绑定校验及结果构造后调用 RequireCanonical(bytes, Write(result), label)。现使用局部 writer 调用原 WriteCanonical，再 ContentEquals 输入，删除两份只供比较的完整输出数组路径和各自无消费者的数组比较工具。
- 完整重新编码、长度与所有字节一致性检查保留，原 World state is not canonical 异常不变。Write、ComputeHash、读取后校验仍共用唯一字段编码；没有省略 numeric profile、solver、revision、计数或尾部检查。
- 世界 Bodies、求解器 payload 的读取及构造复制未改，长期存储保持独立，不改变求解、快照恢复或事务生命周期。只减少比较用副本，reader／writer／流和实际状态仍分配；ReadPersistenceMode 的枚举查询仍待另行处理。
- 两目标文件修改前无其它未提交修改，编辑前无 csc／bee。Fixed／Float32 portable 分别零警告零错误，逐次构建服务关闭成功，diff 空白检查通过；未新增测试、未操作共享 Unity、未执行世界状态往返／非规范输入运行对比或 Player 分配采样。
## 2026-09-20 世界状态恢复持久化模式校验

对应 tasks.md 的 5.48。

- 两数值域 WorldSimulationStateCodec.ReadPersistenceMode 原 Enum.IsDefined(Type, object) 校验读取的 byte，产生装箱及枚举元数据查询；现直接匹配正式 Reconstruct=1／Snapshot=2，零与其它值继续拒绝。
- 保留读取字段位置和宽度、原 InvalidDataException 文本以及后续 WorldSimulationState 构造；不改持久化模式含义、求解器重建／快照恢复行为或 payload 所有权。构造器原未做相同枚举检查，本轮不附加新约束。
- 修改前两目标文件无其它未提交修改，编辑前无 csc／bee。Fixed／Float32 portable 分别零警告零错误，逐次构建服务关闭成功，diff 空白检查通过；未新增测试、未操作共享 Unity、未执行世界恢复运行或 Player 分配采样。
## 2026-09-20 世界状态构造中间集合与空载荷清理

对应 tasks.md 的 5.49。

- Fixed／Float32 WorldSimulationState 构造原将 bodies 复制到 List、排序后保存 AsReadOnly。现 ToArray 取得独立数组，Array.Sort 沿原 ActorId 比较，重复身份校验按 Length 执行，Array.AsReadOnly 保留对外只读包装。输入集合不会原地修改，状态数据寿命不变。
- 对数组／List 等 ICollection 输入，减少一个 List 对象；最终数组替代原 List 底层数组，并非同时少一整份结果存储。一般 IEnumerable 的框架复制仍可能增长，未宣称构造无分配。
- solverStatePayload 为 null 或长度零时统一 Array.Empty，非空继续 Clone，保留求解器载荷独立所有权。空数组没有可修改元素，不新增租用池、缓存或恢复旁路。
- 运行中的世界状态构造及解码重建都会经过该入口；未修改求解器、事务发布或回滚存储边界。修改前两文件无其它未提交修改，编辑前进程扫描无 csc／bee。Fixed／Float32 portable 均零警告零错误，逐次构建服务关闭成功，diff 空白检查通过。
- 未新增测试、未操作共享 Unity、未做世界状态运行对比或 Player 分配采样。对象、最终数组／包装及非空 payload 克隆仍分配。

## 2026-09-20 世界状态解码载荷单次复制

对应 tasks.md 的 5.50。

- Fixed／Float32 WorldSimulationStateCodec.Read 原经 CanonicalReader.ReadBytes 复制求解器 payload，再传入 WorldSimulationState 构造器 Clone，形成两份连续数组复制。现解码同步读取 ArraySegment，并交给内部 ReadOnlySpan 构造入口；最终状态仅 ToArray 一次，继续独立持有 payload。
- 公共 byte[] 构造入口保留，并统一转交同一只读片段实现；null／空载荷仍得到 Array.Empty，非空调用方输入仍不会被状态暴露或后续修改影响。reader 片段只在同步构造期间借用，不跨方法保存。
- 字段顺序、长度检查、RequireComplete、身份绑定校验及完整 canonical 重新编码比较均未修改。只删除恢复解码的中间 payload 数组，不改变求解器恢复、世界状态发布或事务生命周期；最终状态数组仍是必要分配。
- 两目标文件继承前一提交后无其它未提交修改。Fixed／Float32 portable 分别零警告零错误，逐次构建服务关闭成功，diff 空白检查待提交前执行；未新增测试、未操作共享 Unity、未做世界状态恢复运行对比或 Player 分配采样。

## 2026-09-20 动作生命周期转换运行校验

对应 tasks.md 的 2.43。

- AbilityExecution.RequireTransition 被运行中的外部生命周期提交、AbilityLifecycleIngress 应用和 GameplayAbility 结束规则解析调用。原将 int 缩到 byte 枚举后执行 Enum.IsDefined(Type, object)，每次发生装箱和枚举元数据查询。
- AbilityLifecycleTransition 的正式非空成员连续为 Confirm=1 至 Abort=7。现直接检查该闭区间；None、负数、超过 byte 以及 8 至 255 的未知值继续抛出原 InvalidOperationException。RequireTerminalTransition 仍单独限制 Complete／Cancel／Interrupt／Abort。
- 只删除运行事件到达时的通用枚举查询，不改变动作匹配、状态转换、结果种类、原因文本、来源 Tick 或任何生命周期分支。该入口不是普通每帧必经，未将其记录为逐帧收益。
- 目标文件修改前无其它未提交修改。Fixed／Float32 portable 分别零警告零错误，逐次构建服务关闭成功，diff 空白检查待提交前执行；未新增测试、未操作共享 Unity、未执行动作生命周期运行回放或 Player 分配采样。

## 2026-09-20 Pose Native 枚举值域校验

对应 tasks.md 的 4.2.1。

- CharacterPoseNativePreparationResult 由 Pose Native BeginFrame／PrepareFrame 每帧产生，构造和后续 IsValid 原分别查询 FrameStatus 与 FailureCode 枚举；图准备请求、绑定和准备结果还存在同类查询。四类枚举的正式成员均为连续 byte 值域。
- 新增 CharacterPoseNativeEnumValues，集中定义 PreparationStatus、GraphBoundary、FailureCode 与 FrameStatus 的正式首末成员；十一处 Enum.IsDefined 全部迁移为该值域判断。未知零值、越界值以及原状态／失败码／Demand／Binding 组合仍由原条件拒绝。
- 每帧链删除四次装箱与元数据查询，其余七次属于图准备和准备结果校验，不计作逐帧收益。未修改 Pose Graph 求值、资源准备、节点处理、发布结果或原生数据边界。
- Unity 生成的 Runtime 工程首次 --no-restore 因 Temp 资产文件缺失未启动编译；随后由同一正式工程完成还原和编译，ThirdPersonClient.Runtime 及依赖零错误，存在三十四个既有包／项目警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做 Player 分配采样。

## 2026-09-20 Blend Stack 运行目标枚举校验

对应 tasks.md 的 4.1.1。

- AnimationBlendStackRuntime.RequireTarget 由 SourcePose 快捷入口和统一 Push 请求校验调用。目标运行入口只接受 SourceOwner／SourcePose，原 Enum.IsDefined 后再拒绝 NoPose；现直接匹配两个正式成员，未知值与 NoPose 继续进入同一 ArgumentException。
- SourcePose 快捷入口会在包装统一请求前校验一次，随后 Push 的 RequireRequest 再校验一次，因此一次目标切换可删除两次枚举装箱和元数据查询；普通 Push 删除一次。来源身份、OwnerIndex、请求序号和动作通道约束保持不变。
- 同文件构造期 AnimationSelectionAvailabilityPolicy 仅有 RequireSelection／AllowEmpty 两个正式成员，改为直接匹配；该处只计准备阶段清理，不计逐帧收益。
- ThirdPersonClient.Runtime 正式工程及依赖编译零错误，存在三十四个既有包／项目警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做 Blend Stack 运行回放或 Player 分配采样。

## 2026-09-20 DotRecast 接触候选运动性校验

对应 tasks.md 的 5.51。

- DotRecastWorldSolver 每批 Solve 在表面求解后，为全部本地请求及观测约束构造 ActorContactCandidate，再排序并进入 ActorContactSolver。构造器原对每个候选的 ActorContactMobility 执行 Enum.IsDefined。
- ActorContactMobility 正式成员只有 ActiveSimulated 与 ObservedKinematic。现直接匹配两者，未知零值及其它 byte 继续与非法 ActorId 进入原 ArgumentException；位置、形状、候选顺序和接触求解输入均未改变。
- 删除的是每批按本地角色数加观测角色数重复的装箱与枚举元数据查询，不宣称 ActorContactSolver 的数组、列表、结果或诊断已经无分配。
- ThirdPersonSimulation.DotRecast portable 编译零错误，存在 DotRecast 包内两个既有 nullable 警告，构建服务关闭成功。未新增测试、未操作共享 Unity、未做接触求解运行对比或 Player 分配采样。

## 2026-09-20 KCC 碰撞特征身份枚举校验

对应 tasks.md 的 5.52。

- DeterministicCapsuleQueries 的距离、射线、三角形几何等运行查询在产生命中特征时反复构造 DeterministicCollisionFeatureId；原构造器和 IsValid 都执行 Enum.IsDefined，后续比较、命中结果和 KCC 状态还会读取该身份。
- DeterministicCollisionFeatureKind 正式成员连续为 PlaneFace=1 至 BoxFace=5，现由身份类型集中按该闭区间判断；未知零值及其它 byte 和负索引继续拒绝。碰撞资产图元的 Plane=1 至 Box=3 同步改为边界校验。
- 删除的是每次碰撞特征生成及有效性读取的装箱和元数据查询；图元构造只属于世界准备，不计逐帧收益。未修改特征 Index、命中排序、距离算法、法线、表面或 KCC 状态语义。
- ThirdPersonSimulation.DeterministicKcc portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 KCC 运行回放或 Player 分配采样。

## 2026-09-20 Blend transition 身份端点校验

对应 tasks.md 的 4.1.2。

- AnimationBlendStackRuntime.RequireRequest 在每次 Push 中分别读取编译 transition 与请求 transition 的 AnimationBlendTransitionIdentity；每个身份构造和 IsValid 都通过 IsValidEndpoint 检查源、目标端点。原检查对每个端点执行 Enum.IsDefined。
- IsValidEndpoint 现显式匹配 SourceOwner、SourcePose、NoPose：SourceOwner 要求索引非负，另外两种要求索引为 -1，未知值直接失败。身份字段、相等比较、hash、transition 精确引用检查和错误路径均未改变。
- 一次 Push 比较两个新构造身份时，最多删除四次枚举装箱和元数据查询；其它调用 IsValidEndpoint 的身份有效性读取同步受益。未修改 Blend 时长、曲线、Profile、栈容量或 Pose 生命周期。
- 首次 Runtime 增量编译受并行 Timeline 合同中间状态的五个错误阻断，目标文件无错误；并行提交闭合后重新执行 ThirdPersonClient.Runtime 全依赖编译，零错误、三十四个既有警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做 Player 分配采样。

## 2026-09-20 Pose 源释放后端校验

对应 tasks.md 的 4.2.2。

- AnimationPoseSourceReleaseToken 由 ACL frame journal 的 StageRelease 生成，并在 journal 提交和具体 sampling backend Release 前反复检查 IsValid。构造器与 IsValid 原都对 CharacterAnimationSamplingBackendKind 执行 Enum.IsDefined。
- sampling 后端正式成员只有 NativeClip 与 Acl，现由 token 内同一值判断直接匹配；未知零值及其它 byte、负 permission index、零 generation 继续拒绝。SourceId、PlayerNodeId、permission 和 generation 生命周期未改。
- 删除释放令牌生成与消费中的装箱和枚举元数据查询，不改变 ACL／NativeClip 路由、frame journal 提交顺序或物理 Pose 源释放时机。
- ThirdPersonClient.Runtime 目标程序集增量编译零错误，存在一个既有未使用字段警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做 Pose 源释放运行回放或 Player 分配采样。

## 2026-09-20 Pose 源 readiness 页面枚举校验

对应 tasks.md 的 4.2.3。

- CharacterPoseSourceReadinessJournal 每帧建立 key 并向固定容量 page 记录当前／延后目标；key 构造和 IsValid 原查询 PreparationKind，entry 构造及 page Record／Remove 原查询 Category。同一记录会在这些边界重复校验。
- 新增 CharacterPoseSourceReadinessEnumValues，PreparationKind 按 Action=1 至 BlendSpacePlayer=5、Category 按 Current=1 至 DeferredTarget=2 的正式连续值域判断。未知零值和其它 byte 继续与原身份／binding／readiness 条件共同拒绝。
- 删除 key、entry、page 运行链五处装箱和枚举元数据查询，不改变 completion identity、page generation、覆盖非法结果、容量、Seal 或聚合顺序。
- ThirdPersonClient.Runtime 目标程序集增量编译零错误，存在一个既有未使用字段警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做 readiness 页面运行回放或 Player 分配采样。

## 2026-09-20 Pose 源 readiness 资源目标枚举校验

对应 tasks.md 的 4.2.4。

- CharacterPoseSourceReadinessTarget 在每帧 preparation 转换及延后资源目标记录中构造；原构造器查询 PreparationKind 与 TargetInput，赋值后 IsValid 再查询 PreparationKind，并在 Resource 分支查询 sampling backend。
- 构造器复用 CharacterPoseSourceReadinessEnumValues 的 Kind 规则，TargetInput 按 ClipSamples=1 至 BlendSpaceSamples=3 判断；IsValid 既有三分支继续完整拒绝未知 Input，Resource 分支直接匹配 NativeClip／Acl。
- 删除 target 构造与有效性读取四处装箱和枚举元数据查询，不改变 clips、resource index、group clip、blend space samples 或 readiness 资源解析结果。
- ThirdPersonClient.Runtime 目标程序集增量编译零错误，存在一个既有未使用字段警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做资源 readiness 运行回放或 Player 分配采样。

## 2026-09-20 KCC 身体状态 Ledge 枚举校验

对应 tasks.md 的 5.53。

- DeterministicKccWorldSolver 每步求解完成后为每个 Actor 构造 DeterministicKccBodyState；构造器原在地面身份、法线和吸附约束之后对 LedgeState 执行 Enum.IsDefined，因此随求解 Actor 数重复装箱。
- LedgeState 正式成员连续为 None=0 至 EmptySide=2，身体状态直接检查该闭区间。恢复解码的 FeatureKind 同步按 PlaneFace=1 至 BoxFace=5、Ledge byte 按 0 至 2 检查，未知值继续抛出原 InvalidDataException。
- 删除每步身体状态和低频恢复解码中的三处枚举元数据查询；ActorId、地面身份、法线、稳定性、吸附、状态顺序和 canonical 字段均未改变。
- ThirdPersonSimulation.DeterministicKcc portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 KCC 状态运行回放或 Player 分配采样。

## 2026-09-20 GameplayEffect modifier 恢复枚举校验

对应 tasks.md 的 2.44。

- Fixed／Float32 GameplayEffectStateAggregateCodec 在角色状态恢复时逐个读取 PortableAttributeModifierState；每个 modifier 原分别对 Operation 和 ClampBound 执行 Enum.IsDefined，因此分配随恢复的 modifier 数量增长。
- PortableModifierOperation 正式成员连续为 Additive=0 至 Clamp=3，PortableClampBound 连续为 Minimum=0 至 Maximum=1。两数值域同步按闭区间判断，未知 byte 继续抛出原 InvalidDataException。
- 只删除状态恢复解码中的装箱和枚举元数据查询，不改变 modifier handle、来源效果、幅值、优先级、实时属性、插入顺序或聚合结构；该路径不计普通逐帧收益。
- Fixed／Float32 portable 分别零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做 GameplayEffect 状态恢复运行对比或 Player 分配采样。

## 2026-09-20 黑板 OwnerToken 作用域校验

对应 tasks.md 的 2.45。

- Fixed／Float32 BlackboardRuntime 在运行中根据 Character、Graph、State、ActionInstance、Frame 作用域解析 owner generation，并构造 BlackboardOwnerToken 写入或对比状态。token 构造器和 IsValid 原分别执行 Enum.IsDefined。
- ProgramScopeKind 正式成员连续为 Character=1 至 Frame=5，BlackboardOwnerToken 内部统一按闭区间判断；未知零值及其它 byte、负 compiled owner index、零 generation 继续由原异常或 IsValid=false 拒绝。
- 删除两数值域运行黑板 owner 解析、物化和后续有效性读取中的装箱与枚举元数据查询；不改变作用域 generation、所有权比较、状态槽或黑板生命周期。
- Fixed／Float32 portable 分别零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做黑板运行回放或 Player 分配采样。

## 2026-09-20 相机表现请求 Kind 与生命周期校验

对应 tasks.md 的 4.4.9。

- CameraProgramRequestFactory 和 TimelineToActionCommandBridge 在运行中为 Sequence、Effect、Response、Target 的激活／退役生成 PresentationCameraRequest。请求构造器原分别查询 Kind 与 Lifecycle，消费者读取 IsValid 时再查询两次。
- PresentationCameraRequestKind 正式成员连续为 Sequence=1 至 Target=4，Lifecycle 连续为 Activate=1 至 Retire=2；请求类型内部统一按闭区间判断，未知零值及其它 byte 继续由原异常或 IsValid=false 拒绝。
- 每个运行请求构造和有效性读取合计最多删除四次装箱及枚举元数据查询；身份规范化、权重、Blend 时长、模式、优先级和相机请求路由保持不变。
- Fixed／Float32 portable 分别零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做相机请求运行回放或 Player 分配采样。

## 2026-09-20 物理 Pose 源后端与资源身份校验

对应 tasks.md 的 4.2.5。

- PhysicalPoseSourceMetadata.IsValid 被物理源 pending/committed 身份、注册结果、诊断视图和注册表消费共用。原先先执行 Enum.IsDefined，再按 NativeClip／Acl 分支约束 resource catalog index。
- 正式组合现直接表达为 NativeClip 且 index=-1，或 Acl 且 index>=0；未知后端自然失败。source generation、pose source id、owner、提交和释放代次均未改变。
- 删除物理 Pose 源注册与身份读取中的装箱和枚举元数据查询，不改变 backend 路由、资源索引含义、pending/committed page 或释放流程。
- 并行 Timeline Camera 合同处于中间状态时首次增量编译被八个非目标错误阻断；对应并行提交闭合后，ThirdPersonClient.Runtime 全依赖编译零错误、三十四个既有警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做 Player 分配采样。

## 2026-09-20 角色 locomotion 表现枚举值域

对应 tasks.md 的 4.5。

- CharacterLocomotionPresentationPlan、FactLineage 和 PreparedBinding 的 IsValid 在表现准备、movement fact 校验及运行绑定读取中重复执行；原分别查询 ClockMode、BodySource、CorrectionMode。CharacterDomainRuntimeFact 构造还查询 Kind 与 State。
- 新增 CharacterPresentationEnumValues，五类枚举分别按现有正式连续首末成员判断：ClockMode、BodySource、BodyCorrectionMode、DomainFactKind、DomainFactState。未知零值及其它 byte 继续使 IsValid=false 或抛出原异常。
- 删除三类 locomotion 表现有效性读取与 domain fact 构造中的六处装箱和枚举元数据查询；Plan 身份、movement lineage、body profile、runtime fact 内容及校验顺序不变。
- 首次编译前检测到 Unity/C# 编译进程并主动跳过；确认进程结束后，ThirdPersonClient.Runtime 目标程序集增量编译零错误，存在一个既有未使用字段警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做表现运行采样。

## 2026-09-20 Fixed Unity 输入适配器枚举校验

对应 tasks.md 的 4.4.10。

- FixedLocalInputIngressPass 的提交、丢弃、恢复及 Rollback endpoint 丢弃都会调用 UnityFixedCharacterInputAdapter.NotifyStateDisposition；适配器原在每次通知执行 Enum.IsDefined。适配器状态恢复还逐条校验 pending request 的 TimingClass。
- disposition 正式成员连续为 Prepared=1 至 Restored=4，TimingClass 连续为 Immediate=1 至 Offensive=2；现直接按闭区间判断，未知零值及其它 byte 继续进入原异常。
- 删除输入运行通知及恢复时按 pending request 数重复的装箱和枚举元数据查询，不改变请求序号、捕获帧、buffer 秒数、优先级、capture/eligible tick 或输入提交顺序。
- ThirdPersonClient.Runtime 目标程序集增量编译零错误，存在一个既有未使用字段警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做输入运行回放或 Player 分配采样。

## 2026-09-20 Float32 输入事务通知枚举校验

对应 tasks.md 的 2.46。

- Float32LocalInputSourcePort 在输入事务提交、丢弃及状态恢复后调用 NotifyStateDisposition，并向全部 ICharacterControlSourceTransactionObserver 广播。该入口原在每次广播前执行 Enum.IsDefined。
- CharacterControlSourceStateDisposition 正式成员连续为 Prepared=1 至 Restored=4，现直接按闭区间判断；未知零值及其它 byte 继续抛出原 ArgumentOutOfRangeException。Fixed 对应端口已经使用同一正式成员判断，本项补齐 Float32。
- 不改变 observer 筛选、通知顺序、输入状态捕获／恢复或事务提交语义，只删除运行通知入口的装箱和枚举元数据查询。
- Float32 portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做输入事务运行回放或 Player 分配采样。

## 2026-09-20 Clip Player 每帧 DemandKind 校验

对应 tasks.md 的 4.2.6。

- CharacterPoseNativeClipPlayerHandler 与 BlendSpacePlayerHandler 的 PrepareFrame 每帧调用 AnimationClipPlayer.SetRelevant(true)。该入口原在 relevant=true 时对 PoseSourceProviderDemandKind 执行 Enum.IsDefined，即使已经处于 relevant 状态也会重复查询。
- DemandKind 正式成员连续为 Entry=1 至 TransitionSource=4，现直接按闭区间判断；未知零值及其它 byte 继续抛出原 ArgumentOutOfRangeException。相关性切换、source generation、continuity、reset 和释放逻辑不变。
- 删除 Clip Player 与 BlendSpace Player 每帧准备中的装箱和枚举元数据查询；PrepareFrame 返回的单元素请求数组仍分配，留作独立寿命迁移小步。
- ThirdPersonClient.Runtime 目标程序集增量编译零错误，存在一个既有未使用字段警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做 Player 分配采样。

## 2026-09-20 Pose Player 单请求槽复用

对应 tasks.md 的 4.2.7。

- CharacterPoseNativeClipPlayerHandler、BlendSpacePlayerHandler 和 SelectedPosePlayerHandler 的 PrepareFrame 每帧各自构造单元素 SourceRequest 数组；CharacterPoseNativeGraphEvaluator 会在同一次同步调用中逐项复制到本帧汇总列表，不持有 handler 返回容器。
- 三类 handler 现在各自长期持有一个单元素请求槽，每帧只覆盖值并返回；NodeId、PoseSourceSlot、SourceId、Required 和 ScopeInstanceId 均保持原值，Evaluator 汇总后的 demand 生命周期不变。
- 删除每帧三份单元素数组。Evaluator 的汇总 List 及 SourceDemand 构造校验仍有独立分配，留待后续按完整帧寿命继续收口。
- 定向 diff 校验通过；首次 ThirdPersonClient.Runtime 增量编译被并行相机改动的 CameraEffectEvaluator 新构造参数尚未同步阻断，调用点补齐后与 4.2.8 同轮增量编译零错误、存在一个既有未使用字段警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做 Player 分配采样。

## 2026-09-21 Pose Demand 汇总与合法性校验复用

对应 tasks.md 的 4.2.8。

- CharacterPoseNativeGraphEvaluator.PrepareFrame 原为每个图实例每帧新建 SourceRequest List，再把各 handler 请求复制进去；Evaluator 现在长期持有一个汇总列表，初始容量取 handler 数，帧开始清空并复用，活动源超过该数时只发生首次扩容。
- CharacterPoseNativeSourceDemand 原为每帧新建重复键 HashSet，并在每条合法请求上提前拼装仅异常才使用的描述字符串。合法性检查现只在实际异常分支构造文本，重复身份按已验证的前序请求顺序比较，不改变 ScopeInstanceId、NodeId、SourceId 三元身份规则。
- 顺序重复检测以活动 Pose 源通常较少为取舍，删除每图每帧 HashSet 的固定成本；当单图活动源数量显著增加时比较次数呈平方增长，后续需要以真实图规模和采样结果判断是否值得引入实例级集合工作区。
- ThirdPersonClient.Runtime 目标程序集增量编译零错误，存在一个既有未使用字段警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做 Player 分配采样。

## 2026-09-21 Blend Stack 源准备工作区复用

对应 tasks.md 的 4.2.9。

- CharacterPoseNativeBlendStackSourceModuleBinding.PrepareFrame 原每帧新建 SourceRequest List 和 SourceId HashSet，并为每个 Timeline／MotionMatching 活动源 new 一个 PendingSource 对象；pending 列表虽已长期持有，但对象本身仍逐源分配。
- binding 现在长期持有 pending、request、source identity 三个工作区，每帧统一 Clear；PendingSource 改为只读值记录，保存相同 SourceId、owner、sample、capture 和 provider 标记，PrepareEvaluation 仍按原列表顺序准备来源。
- 无当前 selection 时返回同一个空 request 工作区；正常帧返回的 request 列表只在 CharacterPoseNativeGraphEvaluator 同步复制期间借用，pending 列表继续持有到同帧 PrepareEvaluation。首次达到更高活动源数量时容器仍可能扩容。
- ThirdPersonClient.Runtime 目标程序集增量编译零错误，存在一个既有未使用字段警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做 Blend Stack 运行采样或 Player 分配采样。

## 2026-09-21 Pose StateMachine 两态工作区

对应 tasks.md 的 4.2.10。

- CharacterPoseNativeStateMachineSource 的活动集合业务上只有当前态，以及普通混合期间不同于当前态的目标态。原实现用 yield 返回这一个或两个 StateRuntime，并在 PrepareFrame、PrepareEvaluation、Evaluate、CommitFrame 四个阶段分别创建迭代器。
- 现用固定两槽 StateRuntime 工作区显式收集活动态，四阶段按数量直接循环；活动顺序仍是当前态在前、目标态在后，Inertialization 直接切态和普通双态混合语义不变。
- PrepareFrame 复用按 contribution capacity 预备的子请求列表；Evaluate 用两个局部引用替代输出 List；PrepareEvaluation 用普通循环查找父 demand 中的子请求，删除捕获 expected 的 LINQ Any 委托／闭包。身份仍按 ScopeInstanceId、NodeId、SourceId 三项匹配。
- ThirdPersonClient.Runtime 目标程序集增量编译零错误，存在一个既有未使用字段警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做 StateMachine 运行采样或 Player 分配采样。

## 2026-09-21 Pose Graph 输出边界预绑定

对应 tasks.md 的 4.2.11。

- CharacterPoseNativeGraphEvaluator.EvaluateGraphOutput 原每帧用 FindNodes 分别查找 OutputPose／GraphOutput，FindNodes 会创建 List 再 ToArray；GraphOutput 路径还会重新创建运行端口形状并查找连接目标定义，随后 ReadInputValue 再创建一次形状。
- Evaluator 初始化时现一次确认唯一 OutputPose，或唯一 GraphOutput、唯一输入连接和目标端口定义；逐帧 OutputPose 直接读取固定 pose 输入，GraphOutput 通过新增的已绑定端口重载按缓存 definition.Kind 路由 typed input。
- 该绑定服从现有 PreparedBinding 的图 revision：运行实例启动后拓扑和动态端口不得原地变化；需要变化时仍应走正式重建实例链路，不增加运行期失效探测或兼容路径。
- ThirdPersonClient.Runtime 目标程序集增量编译零错误，存在一个既有未使用字段警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做图输出运行采样或 Player 分配采样。

## 2026-09-21 Pose StateMachine 候选迁移预排

对应 tasks.md 的 4.2.12。

- CharacterPoseNativeStateMachineSource.SelectTransition 原在每个未处于普通混合的帧上，从全部 transition 执行 Where、OrderBy、ThenBy、ToArray；alias 来源判断还用 SingleOrDefault 和捕获 stateId 的 Any。
- 实例构造完成 state 定义校验后，现为每个 state 一次解析直接来源或 alias 来源，按 Priority 升序、TransitionId ordinal 升序保存候选数组；SelectTransition 只按原顺序执行规则并返回首个命中项。
- 空候选复用 Array.Empty；状态机 definition 属于实例生命周期内的只读 authoring 输入，运行期不支持原地修改 transition 或 alias，变更仍须重建正式实例。
- ThirdPersonClient.Runtime 目标程序集增量编译零错误，存在一个既有未使用字段警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做迁移选择运行回放或 Player 分配采样。

## 2026-09-21 Pose StateMachine 规则求值工作区

对应 tasks.md 的 4.2.13。

- CharacterPoseNativeStateMachineSource.EvaluateRule 原每尝试一个候选 transition 都新建 Operation 字典、RuleValue 字典和递归 visiting 集合，再按 rule.Operations 填充并执行输出操作。
- 状态机实例构造时现取全部 transition rule 的最大操作数作为三份工作区容量；每次求值前 Clear，重新填充相同 operation identity 映射，并沿原递归、缓存、环检测和短路逻辑执行。
- 分配从逐候选运行路径移到状态机实例构造；候选规则间不共享值或 visiting 状态，异常后的下一次求值也会先清空。规则图本身仍按候选重新填充 operations 工作区，后续是否预编译不在本小步内。
- ThirdPersonClient.Runtime 目标程序集增量编译零错误，存在一个既有未使用字段警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做规则命中运行回放或 Player 分配采样。

## 2026-09-21 Pose Graph 运行端口定义缓存

对应 tasks.md 的 4.2.14。

- CharacterPoseNativeGraphRuntime.FindPort 原每次调用 CharacterPoseCanvasNativePorts.GetRuntimeShape；该方法会新建 List、添加静态端口并排序投影动态端口。SpaceConversion 的动态输入读取及 Subgraph 每帧输入绑定都会重复经过此路径。
- 克隆图通过正式 Validator 后、Evaluator 初始化前，Runtime 现一次建立 NodeId、PortId、Direction 到 CharacterPosePortDefinition 的映射；BindGraphInput 与 ReadInputValue 继续执行原方向、类型及 typed read 校验，但 FindPort 只做值键字典查询。
- 缓存覆盖克隆图当时的静态和动态端口，并在 Runtime Dispose 时清空；图 revision 生命周期内不支持原地改端口，变更仍通过正式实例重建生效。
- ThirdPersonClient.Runtime 目标程序集增量编译零错误，存在一个既有未使用字段警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做 Subgraph／SpaceConversion 运行采样或 Player 分配采样。

## 2026-09-21 Blend Stack push 目标端校验

对应 tasks.md 的 4.2.15。

- AnimationBlendPushRequest 在 Blend Stack 接受 Timeline／Provider 新来源时构造；目标端业务只允许 SourceOwner 或 SourcePose，原校验先排除 NoPose，再调用 Enum.IsDefined。
- 构造器现直接匹配两个允许值，零值、NoPose 及其它 byte 继续进入原 ArgumentException；source id、owner index、transition target、request sequence 和 hard cut 约束不变。
- 该入口按来源切换触发，不计普通无切换帧收益，只删除实际运行 push 时的一次枚举装箱和元数据查询。
- ThirdPersonClient.Runtime 目标程序集增量编译零错误，存在一个既有未使用字段警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做 Blend Stack 来源切换回放或 Player 分配采样。

## 2026-09-21 脚步 Motion Event 相位校验

对应 tasks.md 的 4.3.1。

- AnimationFootMotionEventFrame 由脚步相位求解、Bind 和预测／接触分支构造，左右脚运行帧会重复进入；构造器原对 AnimationFootMotionEventPhase 执行 Enum.IsDefined。
- 正式相位连续为 Unavailable=0、PreSwing=1、Swing=2、ApproachContact=3、Contact=4，现按 byte 上界直接判断；未知值继续进入原 ArgumentException，Contact、下一落脚、摆动进度及 approach 组合约束不变。
- 仅删除正式事件帧构造中的枚举装箱和元数据查询，不改变事件身份、落脚周期、连续性或脚侧绑定。
- ThirdPersonClient.Runtime 目标程序集增量编译零错误，存在一个既有未使用字段警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做脚步事件运行回放或 Player 分配采样。

## 2026-09-21 脚步 Motion Runtime 锁定模式校验

对应 tasks.md 的 4.3.2。

- AnimationFootMotionRuntimeSample 由左右脚运行曲线读取和绑定结果逐帧构造；原在高度、速度、误差、接触值校验中对 LockMode 执行 Enum.IsDefined。
- 正式锁定模式连续为 Unlocked=0、Sliding=1、Locked=2，现按 byte 上界直接判断；未知值继续进入原 ArgumentOutOfRangeException，其它数值归一化、事件有效性和预测落脚语义不变。
- 仅删除正式脚步运行样本构造中的枚举装箱和元数据查询，不改变锁定权重、支撑权重或事件帧内容。
- ThirdPersonClient.Runtime 目标程序集增量编译零错误，存在一个既有未使用字段警告，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做脚步曲线运行回放或 Player 分配采样。

## 2026-09-21 Pipeline Coordinator 控制结果枚举校验

对应 tasks.md 的 2.47。

- 2.33 已处理 Fixed／Float32 对外 TransactionResult；共享 PipelineTransactionCoordinator 内部仍会在 Pending 和 Committed 两条外层 tick 返回路径构造 PipelineTransactionControlResult，并在其构造器执行 Enum.IsDefined。
- 内部 outcome 正式成员只有 Pending=1、Committed=2，现直接匹配两值；零值和其它 byte 继续进入原 ArgumentException，事务身份及只有 Committed 才允许 commit batch 的双向约束不变。
- 结果 class 自身仍分配，本小步只删除每个外层 tick 控制结果构造中的枚举装箱和元数据查询，不重复计入 2.33 的对外结果修改。
- ThirdPersonSimulation.Core portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 Pipeline 事务运行对比或 Player 分配采样。

## 2026-09-21 ExecutionPlan 运行枚举值域

对应 tasks.md 的 2.48。

- Local、Rollback 与 ServerAuthoritative 调度在运行中构造 SimulationSessionExecutionPlan、SimulationPipelineStepProvenance、具体 Step 和 SourceMapping；原四个构造层分别查询 PlanStatus、ExecutionKind 或 TickSourceKind。
- PlanStatus 正式成员连续为 Pending=1 至 NoStep=3，ExecutionKind 连续为 Forward=1 至 Authoritative=4，TickSourceKind 连续为 LocalLogic=1 至 Replay=3；现统一按对应 byte 闭区间判断，未知零值及其它值继续进入原异常。
- 不改变 Pending／NoStep／Executable 的步骤和 requirement 组合，Step provenance 身份、actor 排序、来源映射和 replay／authoritative 路由保持原逻辑；计划及步骤对象自身分配仍存在。
- 首次编译前检测到 Unity Bee／C# 编译进程并主动跳过；进程结束后 ThirdPersonSimulation.Core portable 编译零警告零错误，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做调度运行回放或 Player 分配采样。

## 2026-09-21 Pipeline step 事务上下文执行类型

对应 tasks.md 的 2.49。

- PipelineTransactionCoordinator 为 ExecutionPlan 中每个 step 构造 SimulationPipelineStepTransactionContext；即使 provenance 和 Step 已校验 ExecutionKind，该边界仍需独立拒绝 default 或伪造上下文，原实现再次调用 Enum.IsDefined。
- ExecutionKind 正式成员连续为 Forward=1 至 Authoritative=4，事务上下文现按 byte 闭区间判断；session、pipeline、tick、step index/count 和 transaction identity 校验顺序及异常保持不变。
- 只删除每执行一个 step 时的一次枚举装箱和元数据查询，不改变 pass 调用、步骤数量或事务阶段。
- 首次编译前检测到 Unity Bee 编译进程并主动跳过；进程结束后 ThirdPersonSimulation.Core portable 编译零警告零错误，构建服务关闭成功。未新增测试、未主动刷新 Unity、未做逐步事务运行回放或 Player 分配采样。

## 2026-09-21 Ability Tree Clip hook 校验

对应 tasks.md 的 4.2.16。

- CharacterTimelineHost 在 Root 更新以及 Clip enable、disable、destroy 路径构造 AbilityTreeClipInvocation；构造器原对每次 hook 执行 Enum.IsDefined。
- AbilityTreeClipHook 正式成员连续为 OnEnable=0、OnDisable=1、OnDestroy=2、Root=3，现按 byte 上界判断；未知值继续进入原 ArgumentOutOfRangeException，clip authoring id、tree graph id、cycle、action instance 和 timeline runtime handle 约束不变。
- Root 更新路径仍会构造 invocation，本小步只删除其中的枚举装箱和元数据查询，不声称 Timeline Host 已无分配。
- ThirdPersonSimulation.Core portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 Ability Tree Clip 生命周期回放或 Player 分配采样。

## 2026-09-21 Timeline Runtime Snapshot 枚举值域

对应 tasks.md 的 2.50。

- AbilityTimelineRuntimeSnapshot 由 CharacterTimelineHost 捕获，并由 Fixed／Float32 CharacterRuntimeStateCodec 恢复构造；原构造器分别查询 PlaybackMode 和 State。
- PlaybackMode 正式成员为 Once=0 至 Loop=1，State 连续为 Prepared=0 至 Disposed=6，现按各自 byte 上界校验；未知值继续抛出原 ArgumentOutOfRangeException，运行身份、cursor、cycle、stop context 和 action context 约束不变。
- Snapshot 仍为独立对象，TreeDecisionExits、PendingTreeDecisionExits 和 ActiveClipIds 的深拷贝继续保留；本小步只删除两次枚举装箱和元数据查询，不把快照路径计作逐帧零分配。
- ThirdPersonSimulation.Core portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 Timeline 快照捕获／恢复对比或 Player 分配采样。

## 2026-09-21 角色控制 Ability 停止模式校验

对应 tasks.md 的 2.51。

- CharacterControlAbilityStopRequest 是角色控制向 Fixed／Float32 Action Runtime 提交技能停止意图的共享值请求；构造器原对 StopMode 执行 Enum.IsDefined。
- 正式模式只有 Graceful=1 和 Force=2，现直接匹配两值；未知零值及其它 byte 继续进入原 ArgumentException，CharacterControl 来源、AbilityId、reason、action instance 和 window type 内容不变。
- 两数值域运行时仍按 Graceful 映射 Cancel、Force 映射 Abort；本小步只删除请求构造中的枚举装箱和元数据查询。
- ThirdPersonSimulation.Core portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做技能停止运行回放或 Player 分配采样。

## 2026-09-21 SimulationStep actor 列表所有权

对应 tasks.md 的 2.52。

- FixedSimulationStep／Float32SimulationStep 经 TargetSimulationPipelineStep 构造时，CollectActors 原先先创建 actor List，SimulationPipelineStep 基类再从 IEnumerable 复制成第二个 List，最后 AsReadOnly 创建包装对象；该链每个运行 step 都执行。
- 基类保留原受保护 IEnumerable 构造入口；同程序集新增 List 所有权入口，TargetSimulationPipelineStep 将自身刚创建、无外部引用的 actor 列表直接交给基类。基类仍原地排序、拒绝空列表、非法或重复 ActorId，并只通过 IReadOnlyList 暴露。
- 删除每个 step 的第二份 actor List、其内部数组复制及 ReadOnlyCollection 包装；inputs／ingress 的独立所有权、排序和只读包装本小步不变。
- ThirdPersonSimulation.Core portable 连续两次编译均零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 Fixed／Float32 调度运行对比或 Player 分配采样。

## 2026-09-21 SimulationStep 输入列表只读暴露

对应 tasks.md 的 2.53。

- TargetSimulationPipelineStep 已分别从调用输入复制出私有 inputValues 和 ingressValues，并在构造器内完成排序、actor 绑定及重复检查；原完成后仍对两份列表调用 AsReadOnly，各创建一个只读包装对象。
- 两个字段现以 IReadOnlyList 保存并直接引用各自私有 List。调用者仍只能通过 Inputs／Ingress 的 IReadOnlyList 接口读取，不获得底层 List 引用；步骤构造完成后的内容和顺序保持不变。
- 删除每个 Fixed／Float32 SimulationStep 的两个 ReadOnlyCollection 包装对象；输入和 ingress 列表本体、独立复制及元素存储仍保留。
- ThirdPersonSimulation.Core portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做步骤输入运行对比或 Player 分配采样。

## 2026-09-21 ExecutionPlan 列表只读暴露

对应 tasks.md 的 2.54。

- SimulationSessionExecutionPlan 构造时已从调用输入复制出私有 steps List，并由 FreezeMappings 复制、排序和校验私有 source mappings List；原完成后分别通过 AsReadOnly 创建包装对象。
- 两个字段现以 IReadOnlyList 保存并直接引用各自私有 List。Steps／SourceMappings 的公开类型和只读调用方式不变，调用者不持有构造器内部新列表的可变引用。
- 删除每个 Fixed／Float32 外层 tick 计划的两个 ReadOnlyCollection 包装对象；steps 和 mappings 列表本体、独立复制、映射排序及 roster／source 校验仍保留。
- ThirdPersonSimulation.Core portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 ExecutionPlan 调度运行对比或 Player 分配采样。

## 2026-09-21 SimulationStep actor 容量来源

对应 tasks.md 的 2.55。

- TargetSimulationPipelineStep.CollectActors 为每个 input 精确添加一个 ActorId，原 actor List 使用零容量构造，多 Actor step 填充时按 List 默认策略扩容。
- actor 列表现直接按 inputs.Count 准备容量；添加顺序、后续排序、空列表拒绝和重复 ActorId 校验不变，容量来源就是本 step 已独立持有的输入数量。
- 只删除多 Actor step 的内部扩容及旧数组迁移，actor List 和每个 step 的最终存储仍保留。
- ThirdPersonSimulation.Core portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做多 Actor 调度运行对比或 Player 分配采样。

## 2026-09-21 Pose committed output 死状态清理

对应 tasks.md 的 4.2.17。

- CharacterPoseNativeGraphRuntime 原在 CommitGraphOutput 把 evaluation.Output 保存到 m_LastCommittedOutput，并由 CharacterPoseNativeRoleRuntime 转发；全项目源码检索确认没有读取消费者，reset 两分支只负责清空该引用。
- 删除字段、提交赋值、reset 清空及 Role 转发属性；正式跨帧结果仍由 CharacterFinalPoseNativePublication 提供，节点 observation 仍复制到 m_CommittedObservations，行为和诊断边界不变。
- 该清理本身不减少端口值构造次数，但移除了无消费者的跨帧引用，明确端口值只借给当前 evaluation／validate／commit 链，为后续按 handler 复用值对象消除错误寿命信号。
- 定向引用检索及 diff 空白校验通过。ThirdPersonClient.Runtime 构建被并行提交 730d5216d 的 TimelineRuntimeAdvanceResult／SampleView 六个消费者接口错误阻断，目标文件没有编译诊断；未新增测试、未主动刷新 Unity、未做 Pose 运行回放或 Player 分配采样。

## 2026-09-21 Pipeline 事务身份 UTF-8 直写

对应 tasks.md 的 2.56。

- PipelineTransactionCoordinator 每个外层 tick 原调用 StableHash.Compute，依次传入 domain、composition hash、plan hash、source kind、clock id、source tick、completed tick。入口会产生三个数字字符串和 params 数组，SimulationIdentity.Hash 再产生 join 字符串及完整 UTF-8 数组。
- 专用事务身份入口现从已有 StableHash.Value 直接取得 composition／plan 文本，三个非负数字使用与原 ToString 相同的 CurrentCulture 写入栈 char 缓冲；七段内容仍按原顺序，以 U+001F 的同一 UTF-8 单字节 0x1F 分隔，最终片段同步交给原 SHA-256 canonical hash。
- UTF-8 容量按七段实际 byte count 加六个分隔符精确计算并从 ArrayPool 租用，hash 完成后立即归还；不跨 tick 保存 payload。最终 StableHash 的 64 字符串及 SHA 提供者仍分配，池首次扩容也可能分配，本项不宣称事务身份已达到 0 GC。
- ThirdPersonSimulation.Core portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做不同 Culture 的事务身份运行对比或 Player 分配采样。

## 2026-09-21 Pipeline Coordinator 值结果

对应 tasks.md 的 2.57。

- PipelineTransactionCoordinator 每个外层 tick 在 Pending 或 Committed 分支 new PipelineTransactionControlResult；FixedPipelineTransaction／Float32PipelineTransaction 收到后只立即读取 Outcome、TransactionIdentity、LastCompletedTick 和 CommitBatch，再构造各自公开结果。
- 内部结果没有 null 分支、引用身份比较或跨调用持有，现改为 readonly struct；构造校验、四个只读属性以及只有 Committed 才能携带 commit batch 的约束不变。
- 删除每个外层 tick 的一个内部控制结果对象；Fixed／Float32 对外 TransactionResult class 和 CommitBatch 自身仍按原生命周期分配。
- ThirdPersonSimulation.Core portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做事务返回运行对比或 Player 分配采样。

## 2026-09-21 Pipeline 事务死返回链删除

对应 tasks.md 的 2.58，取代 2.33、2.47、2.57 中保留结果类型的中间状态。

- 全项目源码核对确认 FixedPassPipelineRuntimeHandle 与 Float32PassPipelineRuntimeHandle 调用 Transaction.Execute 后直接丢弃返回值；内部 ControlResult 只被两域 wrapper 读取，两域公开 TransactionResult 没有其它消费者。
- Coordinator.Execute／ExecuteTransaction、FixedPipelineTransaction.Execute、Float32PipelineTransaction.Execute 统一改为 void。Pending 分支恢复 pipeline checkpoint 后直接返回，Committed 分支仍冻结 commit batch、发布状态并完成 External Commit 后返回；事务行为和异常传播不变。
- 删除 PipelineTransactionControlResult、FixedPipelineTransactionResult、Float32PipelineTransactionResult 以及对应三套 Outcome 枚举，不保留兼容返回路径。TransactionIdentity 仍沿 step／egress context、output disposition 和 commit batch 正式链路传递。
- 每个外层 tick 不再创建两域公开 TransactionResult 对象，也不再复制无人读取的内部结果字段。ThirdPersonSimulation.Core、Fixed、Float32 portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做事务运行对比或 Player 分配采样。

## 2026-09-21 CommitBatch 列表只读暴露

对应 tasks.md 的 2.59。

- FixedSimulationCommitBatch／Float32SimulationCommitBatch 构造时已从 completed steps 与 source egress 调用输入复制出私有 List，并完成 step tick 顺序、event disposition 覆盖及空 egress 检查；原完成后仍分别调用 AsReadOnly。
- 两域四个字段现以 IReadOnlyList 保存并直接引用各自私有 List。Steps／SourceEgress 的公开类型和只读消费方式不变，调用者无法取得构造器内部列表的可变引用；Committer 仍可在提交调用期间或之后持有完整独立批次。
- 删除每个已提交外层 tick 的四个 ReadOnlyCollection 包装对象；CommitBatch class、steps／egress 列表本体、OutputDispositions 及事件覆盖校验仍保留。
- ThirdPersonSimulation.Fixed／Float32 portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做 Committer 运行对比或 Player 分配采样。

## 2026-09-21 CommitBatch 事件覆盖容量

对应 tasks.md 的 2.60。

- Fixed／Float32 CommitBatch 会汇总全部 completed step 中的 GameplayFacts 与 PresentationCommands，再排序并要求数量、EventId、ActorId 与 OutputDispositions 一一对应；原 outputEvents List 从零容量开始增长。
- 构造器现直接使用 OutputDispositions.Dispositions.Count 作为容量。正常提交路径中该数量就是最终事件数；异常不一致路径仍允许 List 扩容后进入原覆盖数量或身份错误，不改变错误判定。
- 只删除正常已提交 tick 汇总事件所有权时的 List 扩容和旧数组迁移，事件列表、排序及逐项覆盖检查仍保留。
- ThirdPersonSimulation.Fixed／Float32 portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做多事件提交运行对比或 Player 分配采样。

## 2026-09-21 CanonicalInputBatch 列表只读暴露

对应 tasks.md 的 2.61。

- FixedCanonicalInputBatch／Float32CanonicalInputBatch 已从 ingress 输入复制出私有 actor input List，排序并校验非空、Actor 唯一及 TickSource 一致；原完成后仍调用 AsReadOnly。
- 两域字段现以 IReadOnlyList 保存并直接引用各自私有 List。Inputs 公开类型和只读消费方式不变，调用者没有内部 List 的可变引用；canonical input batch 仍独立持有输入顺序。
- 删除每个 ingress tick 的两个 ReadOnlyCollection 包装对象；输入 List、排序、来源时钟和重复 actor 校验不变。
- ThirdPersonSimulation.Fixed／Float32 portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做 ingress 输入运行对比或 Player 分配采样。

## 2026-09-21 TypedIngressBatch 列表只读暴露

对应 tasks.md 的 2.62。

- FixedTypedIngressBatch／Float32TypedIngressBatch 已从调用输入复制出私有 ingress List，按 Actor、SourceTick、Sequence、FactIdentity 排序并拒绝重复事实；原完成后仍调用 AsReadOnly。
- 两域字段现以 IReadOnlyList 保存并直接引用各自私有 List。Ingress 公开类型和只读消费方式不变，调用者无法修改内部列表；事实排序和身份组合保持原规则。
- 删除每个 ingress tick 的两个 ReadOnlyCollection 包装对象；ingress List 本体和独立元素存储仍保留。
- ThirdPersonSimulation.Fixed／Float32 portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做 typed ingress 运行对比或 Player 分配采样。

## 2026-09-21 OutputDispositionSet 列表只读暴露

对应 tasks.md 的 2.63。

- Fixed／Float32 各自的 SimulationPipelineOutputDispositionSet 已从 egress 结果复制出私有 disposition List，按 SourceEventId 排序并拒绝重复事件所有权；原完成后仍调用 AsReadOnly。
- 两域字段现以 IReadOnlyList 保存并直接引用各自私有 List。Dispositions 公开类型、事务身份和只读消费方式不变，CommitBatch 的事件覆盖校验继续读取同一独立列表。
- 删除每个 egress tick 的两个 ReadOnlyCollection 包装对象，并清理两文件不再使用的 Collections.ObjectModel 引用；disposition List 本体仍保留。
- ThirdPersonSimulation.Fixed／Float32 portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做 egress disposition 运行对比或 Player 分配采样。

## 2026-09-21 本地输入值帧

对应 tasks.md 的 2.64。

- FixedLocalInputFrame／Float32LocalInputFrame 只在 SourcePort.Read 返回后由 LocalInputIngressPass.Execute 同步读取两个 batch 属性并写入 Product，源码检索未发现缓存、引用身份比较、继承或可空消费。
- 两域 frame 改为 readonly struct，构造器仍拒绝缺失的 CanonicalInputs／TypedIngress；接口返回类型、属性类型、调用顺序及两个 batch 的独立所有权不变。
- 删除每个本地输入 ingress tick 只为打包两个 batch 引用创建的一个 frame 对象。CanonicalInputBatch、TypedIngressBatch 及其内部列表仍分配，未扩大本项结论。
- ThirdPersonSimulation.Fixed／Float32 portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做本地输入运行对比或 Player 分配采样。

## 2026-09-21 空 TypedIngressBatch 共享

对应 tasks.md 的 2.65。

- 全项目源码检索确认 TypedIngressBatch 的三个正式构造点均传入 Array.Empty：Fixed／Float32 本地输入源各一处，Fixed 回滚 Endpoint 一处；没有非空实例构造点。
- 两域 batch 新增只读 Empty 单例，内部直接持有对应元素类型的 Array.Empty；三个输入源统一引用该实例。batch 没有可变成员或写接口，Product slot 只在 tick 内保存引用，共享不会串写状态。
- 公开 IEnumerable 构造器及原排序、重复身份校验完整保留，未来正式非空 ingress 仍取得独立列表。当前空路径每 tick 不再创建 batch 对象和空 List。
- ThirdPersonSimulation.Fixed、Float32、DeterministicRollback.Endpoint portable 分别编译零警告零错误，逐次构建服务关闭成功。客户端 Runtime 构建被并行 CharacterTimelineHost 缺失 AbilityTimelineAdvancePending／AbilityTimelineStopPending 的六处错误阻断，错误不在本项文件。未新增测试、未操作共享 Unity、未做运行对比或 Player 分配采样。

## 2026-09-21 CanonicalInputBatch 接管输入数组

对应 tasks.md 的 2.66。

- 全仓源码检索确认 FixedCanonicalInputBatch／Float32CanonicalInputBatch 各只有对应 LocalInputSourcePort 一个构造点；SourcePort 为当前 tick 新建输入数组，填充后立即交给 batch，不再持有或修改。
- 两域构造器收窄为程序集内部数组入口，直接原地排序并执行原非空、Actor 唯一、输入存在及 TickSource 一致校验，随后以 IReadOnlyList 暴露同一数组；没有新增可变数组出口。
- 删除每个本地输入 ingress tick 的一个 List 对象、数组到 List 底层存储的逐项复制及第二份元素存储。SourcePort 的输入数组和 CanonicalInputBatch 对象仍分配。
- ThirdPersonSimulation.Fixed／Float32 portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做本地输入运行对比或 Player 分配采样。

## 2026-09-21 空 Step ingress 复用

对应 tasks.md 的 2.67。

- 本地 Fixed／Float32 Schedule 将 TypedIngressBatch.Ingress 传入 TargetSimulationPipelineStep；当前正式空批次已由数组实现，但原 step 构造仍无条件 new List 复制该空集合。
- 通用 step 在输入排序与重复 Actor 校验后，对 null 或 Count 为零的 IReadOnlyCollection 直接保存 Array.Empty 并结束 ingress 分支。非空枚举仍复制到独立 List，按 Actor／SourceTick／Sequence／FactIdentity 排序并执行目标 Actor 和重复身份校验。
- 删除当前本地单步调度每个 step 的一个空 List；输入 List、Actor List、step、execution plan 等对象仍分配，网络或未来非空 ingress 路径不共享可变事实。
- ThirdPersonSimulation.Core、Fixed、Float32 portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做调度运行对比或 Player 分配采样。

## 2026-09-21 控制源能力位掩码判断

对应 tasks.md 的 2.68。

- 全运行源码检索只剩两处 Enum.HasFlag：Float32LocalInputSourcePort 构造时核对 TransactionalState 声明与状态接口，以及 LocalSimulationSessionSourceDefinition 组装时判断 CommittedObservation 能力。
- 两处统一改为 capabilities 与单一能力位按位与后比较零。第一处仍比较“声明能力”和“实现接口”的布尔结果，第二处仍只在声明已提交观测时要求 roster runtime；未知能力位检查不变。
- 清除运行代码最后两处 HasFlag 装箱语义。两处都在端口初始化／会话组装，不是每 tick 热路径，不将它们记录为稳态帧收益。
- ThirdPersonSimulation.Float32 portable 编译零警告零错误，构建服务关闭成功。客户端 Runtime 构建仍被并行 CharacterTimelineHost 缺失 AbilityTimelineAdvancePending／AbilityTimelineStopPending 的六处错误阻断，错误不在本项文件。未新增测试、未操作共享 Unity、未做运行或 Player 分配采样。

## 2026-09-21 Pipeline step 投影模式校验

对应 tasks.md 的 2.69。

- SimulationPipelineStateCapture.CaptureStepProjection 每次捕获都会遍历正式 state participant，原对 StepProjectionMode 调用 Enum.IsDefined 后再次读取属性决定跳过还是 CaptureState。
- SimulationPipelineStepProjectionMode 只有连续的 Include=1 与 ReconstructForRestore=2；现单次读取局部值并按 byte 范围校验，非法零值及大于二的值继续抛同一完整性错误，重建型 participant 继续跳过捕获。
- 删除每次 step 投影按 participant 的枚举反射查询／装箱，并减少一次属性读取。participant 列表、snapshot 列表和每个被包含状态的 CaptureState 分配仍存在。
- ThirdPersonSimulation.Core portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 step 投影运行对比或 Player 分配采样。

## 2026-09-21 Fixed Neutral 控制源事务通知校验

对应 tasks.md 的 2.70。

- FixedNeutralCharacterControlSource.NotifyStateDisposition 会在 Fixed 本地输入事务提交、丢弃或恢复时收到状态通知；原每次通过 Enum.IsDefined 校验 disposition。
- FixedCharacterControlSourceStateDisposition 只有连续的 Prepared=1、Committed=2、Discarded=3、Restored=4。现按 Prepared 至 Restored 范围判断，零值及大于四的非法值继续抛 ArgumentOutOfRangeException，并与 UnityFixedCharacterInputAdapter 的正式校验写法一致。
- 删除 Neutral 实现每次事务状态通知的枚举反射查询／装箱；方法仍不保存状态。其它控制源、通知分发顺序和事务行为不变。
- 专用 ThirdPersonSimulation.Fixed.Unity 首次无 restore 构建因缺少 project.assets.json 停止；允许生成临时 assets 后编译又因 no-dependencies 下缺少 Unity 生成的 DeterministicKcc／Fixed DLL 停止。客户端 Runtime 仍受并行 Timeline 类型迁移错误阻断，因此本项只有定向源码校验和 diff 校验，没有可用程序集编译证据。未新增测试、未启动 Unity、未做运行或 Player 分配采样。

## 2026-09-21 ExecutionPlan 空集合复用

对应 tasks.md 的 2.71。

- 正式 Rollback NoStep、ServerAuthoritative Pending 等构造点向 SimulationSessionExecutionPlan 传入 Array.Empty 的 steps 与 source mappings；原构造器仍分别 new 空 List 并保存。
- plan 现对 null 或 Count 为零的 IReadOnlyCollection 直接保存相应 Array.Empty。非空 steps 仍复制到独立 List并执行状态、tick、source mapping、roster 和 plan sequence 校验；非空 mappings 仍复制、排序并检查 outer clock 与重复映射。
- 删除每个 Pending／NoStep plan 的两个空 List 对象。plan 对象、非空 executable 列表、restore 和其它正式结果仍按原生命周期分配。
- ThirdPersonSimulation.Core、Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做 plan 运行对比或 Player 分配采样。

## 2026-09-21 SimulationStep Actor 数组

对应 tasks.md 的 2.72。

- TargetSimulationPipelineStep 已持有完整 inputs，Actor 集合仅从每项 ActorId 派生。原 CollectActors 按 inputs.Count 创建 List 并 Add，base 排序校验后以 IReadOnlyList 保存该 List。
- CollectActors 现按 inputs.Count 创建精确 ActorId 数组并直接填充；SimulationPipelineStep 的程序集内部数组入口接管该新数组，沿原规则排序，拒绝空集合、非法 ActorId 和重复 Actor。外部受保护 IEnumerable 构造入口及其独立 List 路径不变。
- 删除每个 Fixed／Float32 step 的 Actor List 对象，元素存储仍为一份精确数组。step 对象、inputs 存储、ingress 及后续 plan 仍按原生命周期存在。
- ThirdPersonSimulation.Core、Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做 step 运行对比或 Player 分配采样。

## 2026-09-21 SimulationStep 数组输入存储

对应 tasks.md 的 2.73。

- TargetSimulationPipelineStep 原将所有 inputs 枚举复制到 List，再在构造器排序并只读保存；本地 CanonicalInputBatch 的公开 IReadOnlyList 底层为数组，正式服务器权威路径也存在数组输入。
- inputs 动态类型为数组时，现 Clone 取得独立副本，在副本上按 ActorId 排序并直接作为 IReadOnlyList 保存。调用方数组仍不暴露、不共享，重复 Actor 校验及后续 Actor 派生不变。其它 IEnumerable 继续复制到独立 List 后排序。
- 数组输入的每个 step 删除一个 List 对象，以克隆数组作为原来 List 底层数组的等价元素存储；数组复制本身仍保留，step 不借用上游可变数据。
- ThirdPersonSimulation.Core、Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做 step 运行对比或 Player 分配采样。

## 2026-09-21 ExecutionPlan 数组存储

对应 tasks.md 的 2.74。

- 本地 Fixed／Float32 单步与 ServerAuthoritative 权威单步使用新建数组传入一个 step 和一个 source mapping；原 plan 将两数组分别复制到 List，mapping 再排序。
- plan 现对非空数组输入分别 Clone，steps 直接只读保存，mapping 在克隆数组上按 StepClockId／SourceKind 排序后保存；调用方数组仍不共享。非数组 IEnumerable 继续复制到独立 List，所有 plan 状态、tick、mapping、roster 和 sequence 校验不变。
- 数组输入的 executable plan 删除 steps 与 source mappings 两个 List 对象，以克隆数组作为各自原 List 底层存储的等价结果。传入数组和克隆数组仍各自存在，本项没有改变公开构造器所有权语义。
- ThirdPersonSimulation.Core、Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做 plan 运行对比或 Player 分配采样。

## 2026-09-21 OutputDispositionSet 最终数组

对应 tasks.md 的 2.75，取代 2.63 中仍保留结果 List 的状态。

- Fixed／Float32 本地立即输出、Rollback 输出及 ServerAuthoritative 输出均用 List 收集 dispositions，并在 SimulationPipelineOutputDispositionSet 同步构造后清空或复用上游列表；结果必须独立持有，不能直接借用该 List。
- 两域构造入口收窄为 IReadOnlyList，按 Count 创建最终数组并逐项复制，在数组上按 SourceEventId 排序和执行原重复所有权校验；null 或空集合统一保存 Array.Empty。公开 Dispositions 仍为 IReadOnlyList。
- 删除每个 egress tick 的结果侧 List 对象，以最终数组替代原 List 底层数组，不删除上游可复用收集列表。OutputDispositionSet 对象和非空元素数组仍分配。
- ThirdPersonSimulation.Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做 egress 运行对比或 Player 分配采样。

## 2026-09-21 CommitBatch 最终数组

对应 tasks.md 的 2.76，取代 2.59 中仍保留 steps／source egress 结果 List 的状态。

- PipelineTransaction.FreezeCommitBatch 传入 completedSteps 与从 workspace 读取的 sourceEgress，二者均为 IReadOnlyList，底层 workspace 会在后续事务清空和复用；CommitBatch 必须复制，不能借用。
- 两域 CommitBatch 构造入口收窄为 IReadOnlyList，按 Count 分别创建 completed step 与 source egress 最终数组，在复制时执行原 step 非空／tick 严格递增和 egress 非空记录校验；空集合复用 Array.Empty，公开属性仍为 IReadOnlyList。
- 删除每个已提交外层 tick 的 steps 与 source egress 两个结果 List 对象，以最终数组替代各自原 List 底层数组。CommitBatch 对象、非空数组、事件覆盖列表和 disposition 仍存在。
- ThirdPersonSimulation.Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做提交运行对比或 Player 分配采样。

## 2026-09-21 CommitBatch 事件覆盖数组

对应 tasks.md 的 2.77，取代 2.60 中仍保留事件覆盖 List 的状态。

- CommitBatch 事件覆盖校验最终必须与 OutputDispositions.Dispositions.Count 完全相等；原实现以该数量预备 List 容量，再 Add 全部 GameplayFacts 与 PresentationCommands，排序并逐项核对 EventId／ActorId。
- 两域现以 disposition 数量创建精确 OutputEventOwner 数组并顺序填充。事件超过数组时立即抛原“dispositions do not cover every Step EventId”参数错误，填充完成后数量不足仍抛同一错误；数量相等才原地排序并执行原身份与重复 EventId 校验。
- 正常提交路径删除一个覆盖校验 List 对象，以相同元素数组完成同步验证；数组只活到 CommitBatch 构造结束。CommitBatch 持久结果和 OutputDispositionSet 不引用该数组。
- ThirdPersonSimulation.Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做多事件提交运行对比或 Player 分配采样。

## 2026-09-21 PipelineStateSnapshot 最终数组

对应 tasks.md 的 2.78。

- Capture／CaptureStepProjection 及状态解码先收集 participant snapshots，再构造 SimulationPipelineStateSnapshot；原构造器再次复制到 List、排序校验并创建 ReadOnlyCollection，形成结果侧额外 List 对象与包装。
- 构造入口收窄为 IReadOnlyList，按 Count 复制到最终数组，在数组上按 PassId 排序并执行原缺失 participant 与重复 PassId 校验；空集合复用 Array.Empty。Participants 公开类型仍为 IReadOnlyList，SnapshotHash 继续按同一排序读取。
- 每个完整或 step 投影快照删除结果侧 List 与 ReadOnlyCollection 两个对象，以最终 participant 数组替代原 List 底层数组。上游 capture 收集 List、每个 pass snapshot、hash 字符串与 Snapshot 对象仍分配。
- ThirdPersonSimulation.Core、Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做快照运行对比或 Player 分配采样。

## 2026-09-21 Pipeline 状态捕获收集数组

对应 tasks.md 的 2.79。

- SimulationPipelineStateSnapshotCoordinator.Capture 原按全部 participant 容量建 List 后 Add；CaptureStepProjection 同样建最大容量 List，再跳过 ReconstructForRestore participant。
- 完整捕获现按验证后的 participant 数创建精确数组并按索引写入。step 投影先遍历并校验全部 StepProjectionMode，同时统计 Include 数量，再创建精确数组并在第二遍只捕获 Include participant；非法模式仍在任何 CaptureState 之前失败。
- 删除每次完整或 step 投影捕获的上游 snapshots List 对象，以精确数组替代其底层存储。SimulationPipelineStateSnapshot 仍复制到自己的最终数组以保持独立结果寿命，因此两份数组复制边界仍存在。
- ThirdPersonSimulation.Core、Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做快照运行对比或 Player 分配采样。

## 2026-09-21 PipelineStateSnapshot 接管捕获数组

对应 tasks.md 的 2.80，收口 2.79 中保留的第二份数组复制。

- Coordinator.Capture／CaptureStepProjection 创建的 participant snapshot 数组只用于紧接着构造 SimulationPipelineStateSnapshot，调用后没有保留者或后续修改；该数组已经按正式 participant 顺序填满。
- Snapshot 新增程序集内部数组入口，直接接管该数组后执行原空项、PassId 排序和重复检查并计算 hash。公开 IReadOnlyList 构造器仍先复制到新数组再进入同一实现，外部调用方不能通过后续修改输入集合影响 Snapshot。
- 完整与 step 投影捕获不再创建第二份 participant 数组或复制元素；最终 Snapshot 仍独立持有唯一数组。状态解码和其它程序集调用继续走公开复制边界。
- ThirdPersonSimulation.Core、Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做快照运行对比或 Player 分配采样。

## 2026-09-21 Pipeline participant 校验数组

对应 tasks.md 的 2.81。

- PipelineTransactionServices.StateParticipants 的正式类型为 IReadOnlyList，Checkpoint、完整 Snapshot、step 投影和 Restore 四条 Coordinator 调用链都传该固定集合；原 ValidateParticipantSet 每次把运行 participant 复制到 List，并另建 List 从 compiled plan 筛选期望 participant。
- 四个 Coordinator 入口统一接收 IReadOnlyList。校验先统计 plan 中 SnapshotParticipant 数量并填充精确 expected 数组，再按运行 Count 复制精确 values 数组；两数组分别按 PassId 排序后执行原数量、版本、owner、schema 与重复 participant 检查。
- 每次 checkpoint／snapshot／restore participant 校验删除两个 List 对象，以等价的两份精确数组承载元素。完整性检查没有缓存或跳过；plan 与 runtime 集合每次仍重新对齐验证。
- ThirdPersonSimulation.Core、Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做 checkpoint／restore 运行对比或 Player 分配采样。

## 2026-09-21 Pipeline checkpoint 收集数组

对应 tasks.md 的 2.82。

- 每个外层事务由 PipelineTransactionCoordinator 捕获全部已验证 state participant 的 checkpoint；原 Coordinator 按 participant 数建 List、逐项 Add，再创建 ReadOnlyCollection 交给 SimulationPipelineStateCheckpointSet。CheckpointSet 本身只保存 IReadOnlyList，不再次复制。
- 捕获现按已验证 participant 数创建精确接口数组并按索引写入，成功时直接交给 CheckpointSet；异常时以已接纳数量为界逆序 Dispose，保持原失败清理顺序和边界。空 participant 集合复用 Array.Empty。
- 删除每个外层事务 checkpoint 捕获的 List 与 ReadOnlyCollection 两个对象，以一份精确数组替代原 List 底层存储。每个 pass checkpoint 和 CheckpointSet 对象仍按事务生命周期存在。
- ThirdPersonSimulation.Core、Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做 checkpoint 恢复运行对比或 Player 分配采样。

## 2026-09-21 Pipeline 已验证 participant set

对应 tasks.md 的 2.83，收口 2.81 中每次仍创建两份校验数组的状态。

- Fixed／Float32 backend 按 compiled pass 顺序组装的 stateParticipants 在 PipelineTransactionRuntimeServices 生命周期内不会增删；plan、runtime 实例及各 participant 的 StateIdentity 均在激活后锁定。此前每个事务 checkpoint 和每个状态捕获仍重新复制、排序并比对 plan。
- Services 构造时通过原 ValidateParticipantSet 创建一次不可变 SimulationPipelineStateParticipantSet，内部保存 plan 引用和已按 PassId 排序、完整校验的数组。后续 Coordinator 遇到该正式 set 时要求同一 compiled plan 并直接返回内部数组；跨 plan 误用新增明确完整性错误。
- 事务期 checkpoint、完整 snapshot、step 投影和 restore 不再创建运行 participant 与期望 participant 两份数组，也不重复扫描 plan schema。Backend 初始 restore／capture 仍传原始列表并走完整校验，Services 组装本身也执行一次完整校验。
- ThirdPersonSimulation.Core、Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，逐次构建服务关闭成功。未新增测试、未操作共享 Unity、未做事务运行对比或 Player 分配采样。

## 2026-09-21 ObservedWorldConstraintFrame 最终数组

对应 tasks.md 的 5.54。

- Float32 本地与权威调度在无观测接触时每 step 调用 ObservedWorldConstraintFrame.Empty(tick)；原构造即使空集合也创建 List 和 ReadOnlyCollection。状态解码与远端身体选择则先生成数组，构造器再复制到 List。
- Frame 现以 IReadOnlyList 字段保存最终数组。公开 IReadOnlyList 构造先复制到独立数组；程序集内部数组入口直接接管解码／历史转换刚创建且不再使用的数组。两条入口统一在数组上按 ActorId 排序并执行原 Tick 与重复 Actor 校验。
- 空 Frame 直接持有 Array.Empty，删除每 step 的空 List 和只读包装；内部数组调用同时删除第二份元素复制。Frame 对象与包含 Tick 的 FrameHash 仍必须每 step 独立生成，hash 内部分配未处理。
- ThirdPersonSimulation.Float32 portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做观测约束运行对比或 Player 分配采样。

## 2026-09-21 WorldSolveBatchRequest 最终数组

对应 tasks.md 的 5.55。

- Fixed／Float32 AbilityEvaluatePass 持有按 roster 定长的 m_Requests workspace 数组，每 step 填充后构造 WorldSolveBatchRequest，并在 finally 中清空引用。原 Batch 将该数组再复制到 List、排序并创建 ReadOnlyCollection。
- 两域 Batch 构造入口收窄为 IReadOnlyList，按 Count 复制到独立最终数组，在数组上执行原 ActorId 排序、数量、numeric profile、tick、before body 与重复 Actor 校验；公开 Requests 仍为 IReadOnlyList。
- 删除每个 simulation step 的 WorldSolveBatchRequest 结果 List 与 ReadOnlyCollection 两个对象，以最终数组替代原 List 底层存储。Batch 仍独立持有请求，因此 EvaluatePass 清空 workspace 不会影响求解和 request hash。
- ThirdPersonSimulation.Fixed／Float32 portable 分别编译零警告零错误，逐次构建服务关闭成功。Float32 首次构建暴露一处旧 Count 访问，改为数组 Length 后通过；未新增测试、未操作共享 Unity、未做求解运行对比或 Player 分配采样。

## 2026-09-21 WorldSolveBatchResult 最终数组

对应 tasks.md 的 5.56。

- DeterministicKcc、DotRecast 与 UnityCharacterController solver 均按 request 数创建 CharacterWorldSolveResult 数组并填充，再构造对应数值域 WorldSolveBatchResult；原 Batch 将结果再次复制到 List、排序并创建 ReadOnlyCollection。
- 两域 Result 构造入口收窄为 IReadOnlyList，按 Count 复制到独立最终数组，在数组上执行原 ActorId 排序、数量、numeric profile、request identity、tick、solver identity、final body 与 next world state 校验；Summary 继续使用最终数量和同一 hash。
- 删除每批世界求解结果的 List 与 ReadOnlyCollection 两个对象，以最终数组替代原 List 底层存储。solver 输入数组与 Batch 结果数组仍隔离，未引入所有权转移或 solver 后续修改风险。
- ThirdPersonSimulation.Fixed、Float32、DeterministicKcc、DotRecastAuthority portable 编译零警告零错误；DotRecast portable 编译通过并保留依赖包 RcVec2i／RcVec3i 的两条既有 nullable-context 警告。构建服务逐次关闭。未新增测试、未操作共享 Unity、未做求解运行对比或 Player 分配采样。

## 2026-09-21 WorldSolveBatchResult 结果数组所有权转移

对应 tasks.md 的 5.57，收口 5.56 中保留的 solver 数组到 Batch 数组复制。

- DeterministicKcc、DotRecast、UnityCharacterController 三套正式 solver 的 results 均在 SolveBatch 方法内按 request 数新建，逐项填充后只用于紧接着 return WorldSolveBatchResult，没有缓存、复用或返回后的修改者。
- 两域 Result 保留公开 IReadOnlyList 构造，其输入继续复制到独立数组；新增显式 FromOwnedResults 入口接管调用方声明转移的数组并进入同一排序、数量、身份、final body 与 summary hash 校验。三套 solver 统一改用该正式入口。
- 每批世界求解删除第二份 CharacterWorldSolveResult 数组分配与逐项复制，Batch 成为原 solver 数组的唯一持有者。WorldSolveBatchResult 对象、结果元素对象和 hash 分配仍存在。
- ThirdPersonSimulation.Fixed、Float32、DeterministicKcc、DotRecastAuthority portable 编译零警告零错误；DotRecast portable 编译通过并保留依赖包两条既有 nullable-context 警告；ThirdPersonSimulation.Unity 全依赖构建通过并保留 Unity 包及既有 Editor 代码共十七条警告。构建服务均关闭。未新增测试、未刷新共享 Unity、未做求解运行对比或 Player 分配采样。

## 2026-09-21 CharacterWorldSolveResult 值结果

对应 tasks.md 的 5.58。

- 全运行源码检索确认 CharacterWorldSolveResult 只由三套 solver 构造后写入定长数组，经 WorldSolveBatchResult 校验并由 Finalize／codec 按值读取；没有继承、引用身份比较、null 业务分支或跨数组单独持有。
- Fixed／Float32 类型统一改为 readonly struct，构造校验和全部只读字段保持。WorldSolveBatchResult 不再检查元素 null，而由原 numeric profile、ActorId、RequestId、Tick、SolverId 和 final body 对齐校验拒绝未填充的 default 元素。
- 删除 KCC／DotRecast／UnityCharacterController 每 Actor 每 simulation step 的一个 managed 结果对象；结果仍以内联结构体数组随 Batch 持有，WorldBodyState 等引用字段寿命不变。结构体复制成本增加，但字段规模固定且消费均为局部按值读取。
- ThirdPersonSimulation.Fixed、Float32、DeterministicKcc、DotRecastAuthority、ThirdPersonSimulation.Unity 编译零警告零错误；DotRecast portable 编译通过并保留依赖包两条既有 nullable-context 警告。构建服务逐次关闭。未新增测试、未操作共享 Unity、未做求解运行对比或 Player 分配采样。

## 2026-09-21 CharacterWorldSolveRequest 值请求

对应 tasks.md 的 5.59。

- 全运行源码检索确认 CharacterWorldSolveRequest 由 Fixed／Float32 Evaluate 每 Actor 构造并写入定长 workspace 数组，Batch 独立复制后由 KCC／DotRecast／Unity solver 及 Finalize 按值读取；没有继承、引用身份比较或业务 null 分支。
- 两域类型统一改为 readonly struct。WorldSolveBatchRequest 不再检查元素 null，而由原 numeric profile、ActorId、RequestId、Tick、before body 与 roster 对齐校验拒绝 default 元素；EvaluatePass finally 改为写 default，继续释放 Motion／BodyMotionPlan 等引用字段。
- KCC ActorSolveCandidate 原 null 防御同步删除，候选仍携带完整值请求。每 Actor 每 simulation step 删除一个 managed 请求对象；代价是固定字段结构体在候选和局部之间按值复制，不产生 managed 分配或装箱。
- ThirdPersonSimulation.Fixed、Float32、DeterministicKcc、DotRecastAuthority、ThirdPersonSimulation.Unity 编译零警告零错误；DotRecast portable 编译通过并保留依赖包两条既有 nullable-context 警告。首次 KCC 编译暴露旧 null 合并，改为值赋值后通过。未新增测试、未操作共享 Unity、未做求解运行对比或 Player 分配采样。

## 2026-09-21 WorldSolveBatchRequest workspace

对应 tasks.md 的 5.60。

- WorldSolveBatchRequest 的正式 Product lifetime 为 SimulationStep：AbilityEvaluatePass 生产，WorldResolve 与 Finalize 同步消费，CompleteStep 只将 WorldSolveBatchSummary、next state 与 actor results 写入持久 CompletedStep；下一 step 不保留 Batch。KCC candidate 已保存值请求副本，不引用 Batch 数组。
- Fixed／Float32 AbilityEvaluatePass 在按锁定 roster 构造时各创建一个定长 WorldSolveBatchRequest workspace。每 step Reset 将复用的 m_Requests 值数组复制进 Batch 自有定长数组，原地排序并执行原 roster／world／observed constraint 校验，再更新 Tick、BeforeWorldState、能力和 RequestHash。
- 删除每 simulation step 的 WorldSolveBatchRequest 对象和内部请求数组分配；Batch 与数组随 Pass runtime 生命周期复用。Evaluate 的输入 workspace 仍独立并在 finally 写 default，Batch 不借用其可变存储。RequestHash 及 CanonicalWriter 内部分配仍逐 step 存在。
- ThirdPersonSimulation.Fixed、Float32、DeterministicKcc、DotRecastAuthority 编译零警告零错误；DotRecast portable 编译通过并保留依赖包两条既有 nullable-context 警告；ThirdPersonSimulation.Unity 全依赖构建通过并保留 Unity 包及既有 Editor 代码十七条警告。构建服务逐次关闭。未新增测试、未操作共享 Unity、未做多 step 事务或 Player 分配采样。

## 2026-09-21 FinalizedStepResult 直接业务结果

对应 tasks.md 的 2.84。

- FixedFinalizedActorResult／Float32FinalizedActorResult 各自只保存一个非空 SimulationActorTickResult。Finalize 每 Actor new 包装后写 append Product，Transaction 和本地 Output Pass 随后只读取 Value.Result；Rollback 与 ServerAuthoritative 只依赖同一 Product 类型，没有包装特有语义。
- 两域 Product slot factory、Finalize writer、Transaction、LocalImmediateOutput，以及 Rollback History／OutputDisposition、ServerAuthoritative Authority／Prediction readers 统一改为直接承载 SimulationActorTickResult。Transaction 与输出消费直接读取 entry.Value。
- 删除两套包装类型和每 Actor 每 simulation step 的一个 managed 包装对象；SimulationActorTickResult 本身仍是跨 Finalize、CompletedStep 和 Committer 的正式业务结果，其寿命与排序不变。不保留兼容 Product 路径。
- ThirdPersonSimulation.Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，逐次构建服务关闭成功；全运行源码检索确认旧类型及 Value.Result 链均已清空。未新增测试、未操作共享 Unity、未做输出运行对比或 Player 分配采样。

## 2026-09-21 SimulationWorldStateSet 最终 Actor 数组

对应 tasks.md 的 5.61。

- Fixed／Float32 CompleteStep 使用可复用 ExecutionWorkspaceBuffer<SimulationActorState> 收集下一状态，随后构造 SimulationWorldStateSet；原构造器复制到 List、排序校验并创建 ReadOnlyCollection。恢复与初始组装也提供数组或可计数列表。
- 两域构造入口收窄为 IReadOnlyList，按 Count 复制到独立最终数组，在数组上执行原 null、ActorId 排序、非空、world body 数量、numeric profile 与稳定 roster 校验；公开 Actors 仍为 IReadOnlyList。
- 删除每 completed simulation step 的 Actor List 与 ReadOnlyCollection 两个对象，以最终数组替代原 List 底层存储。状态集继续独立持有 SimulationActorState 引用，不借用下一 tick 会复用的 workspace。
- ThirdPersonSimulation.Fixed、Float32、DeterministicRollback 编译零警告零错误；DotRecastAuthority 编译通过并仅带 DotRecast 依赖包两条既有 nullable-context 警告；ThirdPersonSimulation.Unity 全依赖构建通过并保留 Unity 包及既有 Editor 代码十七条警告。构建服务逐次关闭。未新增测试、未操作共享 Unity、未做状态发布运行对比或 Player 分配采样。

## 2026-09-21 SimulationWorldSnapshot 数据所有权转移

对应 tasks.md 的 5.62。

- 两域 Snapshot Factory、Codec 读取及 Float32 权威状态合并均按 Actor 数新建快照数组并生成新的 world-state bytes，构造完成后调用方不再修改或保留这些存储；原构造器仍将 Actor 集合复制进 List、排序后创建 ReadOnlyCollection，并克隆 world-state bytes。
- SimulationWorldSnapshot 现以最终数组保存 Actor。公开 IReadOnlyList 构造继续复制 Actor 并克隆 bytes，保持外部输入隔离；程序集内部数组构造显式接管新建数组和 bytes，在同一存储上执行原排序、非空、null 与重复 Actor 校验后计算 WorldHash。
- Factory、Codec 与权威合并的正式调用自动命中内部数组入口，删除每个快照的 List、ReadOnlyCollection、第二份 Actor 存储及 world-state bytes 克隆；Snapshot 对象、每 Actor 快照和序列化生成的初始 bytes 仍按快照寿命独立存在。
- ThirdPersonSimulation.Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误；DotRecastAuthority 编译通过并仅带 DotRecast 依赖包两条既有 nullable-context 警告；ThirdPersonSimulation.Unity 全依赖构建通过并保留 Unity 包及既有 Editor 代码十七条警告。构建服务逐次关闭。未新增测试、未操作共享 Unity、未做快照运行对比或 Player 分配采样。

## 2026-09-21 SimulationWorldSnapshotFactory Actor 工作数组

对应 tasks.md 的 5.63。

- 全部四处正式 Capture 调用分别传入 SimulationWorldStateSet.Actors 或 ExecutionWorkspaceBuffer，均为可计数 IReadOnlyList；原 Factory 仍通过 IEnumerable 构造 List，再在 List 上排序并生成最终快照数组。
- 两域 Capture 输入收窄为 IReadOnlyList，按准确 Count 新建 Actor 状态工作数组、逐项复制并在数组上执行原 null、排序、roster 数量和身份校验。快照仍不借用下一 step 会清空复用的 workspace，也继续接受未排序的列表输入。
- 删除每次世界快照捕获的 List 对象；Actor 工作数组仍是必要的隔离和排序存储，SimulationActorSnapshot 数组及每 Actor 序列化数据仍按快照生成。
- ThirdPersonSimulation.Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，构建服务逐次关闭。Fixed 构建后曾检测到共享 Unity 的 Bee 编译，后续构建等待其退出再执行，未与 Editor 抢编译。未新增测试、未操作共享 Unity、未做快照运行对比或 Player 分配采样。

## 2026-09-21 Presentation Schedule ClockMode 值域校验

对应 tasks.md 的 7.16。

- GameplayTickSystem 在实时 Presentation Schedule 捕获期间每 render frame 构造 GameplayPresentationScheduleFrame；脚本化输入也构造 GameplayScriptedPresentationFrame。两类帧构造原先都通过 Enum.IsDefined(Type, object) 校验 ClockMode，引入枚举装箱。
- GameplayPresentationDebugClockMode 的正式值域只有连续的 LivePresentation=0 与 LogicLockedPresentation=1。两处构造改为无符号上界比较，负值和大于 1 的非法底层值仍进入原 ArgumentException，其他时间、tick 和插值校验不变。
- ThirdPersonGameplay 全依赖构建通过，保留 Unity 包、UniTask、TEngine 的二十五条既有警告，零错误；构建服务关闭成功。未新增测试、未操作共享 Unity、未做 Presentation Schedule 运行采样。

## 2026-09-21 EventGraphValue typed 读取去装箱

对应 tasks.md 的 2.85。

- EventGraphValue.As<T> 在确认 T 与存储 Kind 对应后，原实现仍把 bool、int、float、Vector2、Vector3、Quaternion 值转成 object 再强制转回 T；角色动画变量适配器的 float／int／bool typed 读取会经过该入口。
- 六种固定值类型现通过项目已有 System.Runtime.CompilerServices.Unsafe 引用直接重解释为已验证的 T，不生成 object。类型和 Kind 条件、错误分支以及值布局不变；ToObject 明确要求 object 的 API 和通用 Enum.ToObject 路径仍保留原装箱语义。
- BTSMTL.EventGraphs 全依赖构建通过，仅保留 Unity Test Framework 两条既有未赋值字段警告，零错误；构建服务关闭成功。首次构建前检测到共享 Unity Bee 编译并等待其退出。未新增测试、未操作共享 Unity、未做 EventGraph 运行采样。

## 2026-09-21 角色状态剩余枚举恢复去反射

对应 tasks.md 的 2.86。

- Fixed／Float32 Character runtime state codec 的泛型 ReadEnum 只剩四类调用：AbilityTimelineSnapshotMode、AbilityTimelineSnapshotState、AbilityTimelineSnapshotStopCause 与 ProgramScopeKind。每次恢复仍经 Enum.ToObject、Enum.IsDefined 和 object 强转完成。
- 四类枚举均有稳定连续正式值域：Once 至 Loop、Prepared 至 Disposed、None 至 Shutdown、Character 至 Frame。两域改为各自具体读取函数，直接校验 byte 上下界并强类型转换；非法值仍抛带数值和枚举名的 InvalidDataException。
- 删除两域角色状态恢复文件最后一条泛型枚举反射和装箱拆箱路径，不改变快照字段宽度、读取顺序、默认黑板 OwnerToken 表示或 Timeline 恢复语义。
- ThirdPersonSimulation.Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，构建服务逐次关闭。未新增测试、未操作共享 Unity、未做状态恢复运行对比或 Player 分配采样。

## 2026-09-21 Character Control Motion binding 枚举解码

对应 tasks.md 的 2.87。

- CharacterControlMotionBindingCodec 的泛型 ReadEnum 只有 EvaluationMode 一个调用，读取 catalog 时仍执行 Enum.IsDefined 与 Enum.ToObject，并产生反射查询及装箱拆箱。
- EvaluationMode 正式值域只有连续的 FullLocalDelta=1 与 ForwardDistanceYaw=2。读取改为 byte 上下界校验后直接强类型转换，非法值异常、字段宽度、catalog 构造和后续 canonical 比较保持。
- 删除无剩余消费者的泛型 ReadEnum。ThirdPersonSimulation.Core.Portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 catalog 运行加载或 Player 分配采样。

## 2026-09-21 Action Presentation 时间快照枚举校验

对应 tasks.md 的 7.17。

- ActionPresentationTimeSnapshot 构造结束会读取 IsValid；该属性原先分别对 LifecyclePhase 与由构造器派生的 ProjectionKind 调用 Enum.IsDefined。时间诊断采集生成快照时因此执行两次枚举装箱查询。
- 可发布生命周期正式集合正好是连续的 Selected、Retained、RetirementPermitted；PendingFirstSample 与 Retired 原本也会被后续条件排除。投影类型则连续为 LatestCommitted 至 BoundedExtrapolation。IsValid 改为两组直接上下界判断，合法集合完全不变。
- ThirdPersonClient.Runtime 全依赖构建通过，保留 Unity 包、第三方包、启动视图和既有 Runtime 字段共三十四条警告，零错误；构建服务关闭成功。未新增测试、未操作共享 Unity、未做时间诊断运行采样。

## 2026-09-21 Session checkpoint 回滚清理缓冲

对应 tasks.md 的 5.64。

- SimulationSessionHost 提交 restore 分支时需要从 SortedDictionary 删除 checkpoint.Tick 之后的条目；原实现通过 Keys.Where 后 ToArray 物化待删 tick，产生 LINQ 迭代器和结果数组，才能避开枚举期间修改字典。
- Host 现按正式 MaxCheckpointCount=32 在生命周期内持有 ulong 清理列表。提交 restore 分支先枚举并收集未来 tick，再按下标删除并清空缓冲；排序字典、删除集合、分支身份和 Actor 重绑定顺序不变。
- 回滚提交后的重复清理不再创建迭代器和数组；Host 构造时的一次列表及其定长容量保留。ThirdPersonClient.Runtime 无依赖重编通过，仅保留 CharacterInputValueNodes 一条既有未使用字段警告，零错误；构建服务关闭成功。未新增测试、未操作共享 Unity、未做回滚运行对比或 Player 分配采样。

## 2026-09-21 Session Host 剩余 checkpoint LINQ

对应 tasks.md 的 5.65。

- Session Host 每次判断表现 checkpoint 能力时原先经 Enumerable.All 遍历注册列表；读取最旧 tick 和 checkpoint 超限淘汰则经 Enumerable.First 读取 SortedDictionary.Keys。三处都把已有具体集合提升为 IEnumerable LINQ。
- 能力检查改为 List 下标短路遍历，继续要求非空 roster 且每个注册同时支持捕获和恢复。最旧 tick 通过 SortedDictionary KeyCollection 的具体 foreach 读取；空集合返回 0，超限淘汰只在非空时调用，原语义保持。
- 文件不再需要 System.Linq。ThirdPersonClient.Runtime 无依赖重编通过，仅保留 CharacterInputValueNodes 一条既有未使用字段警告，零错误；构建服务关闭成功。未新增测试、未操作共享 Unity、未做 checkpoint 运行采样。

## 2026-09-21 角色 Timeline snapshot 更新去 LINQ

对应 tasks.md 的 2.88。

- Fixed／Float32 角色状态在 Timeline advance 时复制快照 List 后用捕获 snapshot 的 RemoveAll 去重；Timeline stop 则先用捕获 runtimeHandle 的 Enumerable.All 查询，再复制 List 并用第二个捕获 RemoveAll 删除。两条链路由能力执行结果提交直接调用。
- 两域加入路径按现有 Count+1 准备列表并下标复制不同 handle，随后追加新 snapshot；移除路径先下标定位，未命中直接返回原状态，命中后按 Count-1 准备并复制其余项。新状态构造仍负责排序、重复校验和独立持有。
- 删除两个文件的 System.Linq、stop 的 All 迭代入口和加入／移除的捕获 RemoveAll 委托；角色状态及内部集合克隆仍按原边界发生，未宣称 Timeline 状态更新已无分配。
- ThirdPersonSimulation.Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，构建服务逐次关闭。未新增测试、未操作共享 Unity、未做 Timeline 运行对比或 Player 分配采样。

## 2026-09-21 OperationModule catalog field 查询去 LINQ

对应 tasks.md 的 2.89。

- Fixed／Float32 OperationModule 为兼容字符串 fieldName 的操作执行入口分别提供 CatalogConstant、CatalogIdentity 与 TryCatalogIdentity；三处均通过捕获 fieldName 的 FirstOrDefault 扫描 entry.Fields，合计六个运行查询点。
- 两域各自统一到 FindCatalogField，下标遍历 IReadOnlyList 并保持第一个 Ordinal 同名字段语义。Constant／Identity 类型检查、必需字段错误与可选 identity 的 false 返回保持不变。
- 删除两个运行模块的 System.Linq 依赖和六处捕获委托／LINQ 枚举入口。ThirdPersonSimulation.Fixed、Float32、DeterministicRollback portable 分别编译零警告零错误，构建服务逐次关闭；首次构建前检测到共享 Unity Bee 编译并等待其退出。未新增测试、未操作共享 Unity、未做操作执行采样。

## 2026-09-21 WorldSimulationState 最终 body 数组

对应 tasks.md 的 5.66。

- Fixed／Float32 WorldSimulationState 由 KCC、DotRecast、Unity solver、codec、预测权威合并及诊断 clone 构造；原构造通过 IEnumerable.ToArray 复制 body 后排序，再创建 ReadOnlyCollection 包装。solver payload 同时复制以保持状态独立。
- 两域 body 输入收窄为 IReadOnlyList，按准确 Count 复制到最终 WorldBodyState 数组，在同一数组执行原 ActorId 排序与重复检查，并直接作为 IReadOnlyList 保存。空 body 复用 Array.Empty；公开构造仍复制 body，payload 仍复制，不借用外部可变存储。
- 删除每个世界状态的 LINQ ToArray 枚举入口和 ReadOnlyCollection 对象；body 最终数组及 payload 独立副本仍按状态寿命存在。ThirdPersonSimulation.Fixed、Float32、DeterministicRollback、DeterministicKcc portable 编译零警告零错误；DotRecastAuthority 编译通过并保留 DotRecast 依赖两条既有 nullable-context 警告；ThirdPersonSimulation.Unity 无依赖重编零警告零错误。构建服务逐次关闭。未新增测试、未操作共享 Unity、未做世界状态运行采样。

## 2026-09-21 WorldSimulationState 数组所有权转移

对应 tasks.md 的 5.67，收口 5.66 中保留的新建数组二次复制。

- World state codec 已新建完整 body 数组，KCC create／step、DotRecast step 与 Unity step 均在方法内新建最终 body 数组；KCC codec 同时新建 solver payload。Float32 权威基线合并也新建单 Actor body 数组和独立 payload 数组。这些调用在状态构造后均不再修改或保留数组。
- 两域新增显式 FromOwnedState，接管调用方声明转移的 body 与 payload 数组，在接管的 body 上执行原排序和重复 Actor 校验。普通公开构造继续复制 IReadOnlyList 并克隆 payload，初始外部 roster、诊断 clone 和其它非独占输入不会被借用。
- codec 直接转移解码 body 数组，payload 从输入片段只复制一次；KCC、DotRecast、Unity 正式 step 及权威合并统一迁移，删除各入口的第二份 body 数组，KCC 同时删除第二份 payload。空 payload 统一复用 Array.Empty。
- ThirdPersonSimulation.Fixed、Float32、DeterministicRollback、DeterministicKcc portable 编译零警告零错误；DotRecastAuthority 编译通过并保留 DotRecast 依赖两条既有 nullable-context 警告。ThirdPersonSimulation.Unity 首次无依赖构建因 Temp 中旧 Float32 DLL 看不到新 API 失败，全依赖刷新后构建通过并保留十七条既有 Unity 包／Editor 警告；所有构建服务已关闭。未新增测试、未操作共享 Unity、未做求解运行对比或 Player 分配采样。

## 2026-09-21 WorldSimulationState 统一 Clone

对应 tasks.md 的 5.68。

- KCC、DotRecast 与 Unity solver 各自实现 CloneState，用 state.Bodies 调公开构造并先对 SolverStatePayload.ToArray；公开构造随后再次克隆 payload，因此 create 返回、reconstruct 保存、公开 current state 和每 step result 均产生两份 payload 数组。
- 两域 WorldSimulationState 新增 Clone，在类型内部各复制一次 body 最终数组和 payload，并直接进入私有接管构造。三套 solver 的 create／reconstruct／step 调用统一改为 state.Clone，删除重复 CloneState 实现。
- 每次世界状态克隆仍产生一份独立 body 数组和一份独立 payload，保持 solver 内部 m_Current 与外部结果隔离；只删除 payload 的第二次克隆及重复实现。ThirdPersonSimulation.Fixed、Float32、DeterministicKcc portable 编译零警告零错误；DotRecastAuthority 编译通过并保留依赖两条既有 nullable-context 警告；Unity 生成的 Float32 与 Unity solver 项目无依赖重编均零警告零错误。构建服务逐次关闭。未新增测试、未操作共享 Unity、未做 solver 状态运行对比或 Player 分配采样。

## 2026-09-21 SimulationActorState 值状态

对应 tasks.md 的 2.90。

- Fixed／Float32 CompleteStep 每 Actor 只需把 ActorId 与已生成 CharacterRuntimeState 引用成对写入 workspace，随后 SimulationWorldStateSet 复制进最终数组；原 SimulationActorState 是只包含这两个字段的 sealed class，因此每 Actor 每 completed step 额外创建一个外壳对象。初始状态与恢复也使用同一类型。
- 两域 SimulationActorState 改为 readonly struct，构造时继续拒绝无效 ActorId 和 null State。Factory 与 StateSet 对 default 元素的校验改为检查 State 引用；排序、重复 Actor、数值域和 roster 校验保持。
- restore transaction 原 FindActor 以 null 表示缺失，统一改为 TryFindActor(out actor)，避免值类型 default 被误读。正式源码检索确认没有其它引用身份、继承或 null 语义消费者。
- ThirdPersonSimulation.Fixed、Float32、DeterministicRollback portable 编译零警告零错误；DotRecastAuthority 编译通过并保留依赖两条既有 nullable-context 警告；ThirdPersonSimulation.Unity 无依赖重编零警告零错误。首次 Portable 构建前检测到 Unity Bee 正在编 ThirdPersonClient.Runtime 并等待其退出。构建服务逐次关闭。未新增测试、未操作共享 Unity、未做 completed step 或 restore 运行采样。

## 2026-09-21 权威预测历史 replay 与确认裁剪

对应 tasks.md 的 5.69。

- ServerAuthoritativePredictionHistory.GetReplayAfter 在 baseline reconcile 与 replay 计数中生成独立结果 List，原返回前再创建 ReadOnlyCollection 包装。PreparePruneConfirmedThrough 则先复制完整 SortedDictionary，再收集待删 tick 到新 List，最后逐项从副本删除。
- replay 结果仍是方法内独立 List，仅按 IReadOnlyList 暴露，删除额外包装对象。确认裁剪直接遍历当前历史，把 InputSequence 大于确认序列的记录加入新 SortedDictionary；最终记录集合、排序和 checkpoint 独立所有权不变。
- 删除每次 replay 查询的包装对象，以及每次确认裁剪的 remove List 和树删除操作；checkpoint、远端身体时间线捕获及最终独立字典仍保留。ThirdPersonSimulation.ServerAuthoritative portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 reconciliation 运行采样。

## 2026-09-21 权威预测 disposition confirmation 中转清理

对应 tasks.md 的 5.70。

- PrepareConfirmation 已持有当前 journal 的独立 entries 副本，却仍先把每条待确认／拒绝记录收集进 updates List，再第二次循环写入 entries。checkpoint 构造又把输入复制成 List 后创建 ReadOnlyCollection 包装。
- confirmation 现遍历只读的 m_Entries 时直接调用 Record 写 entries 副本，原 Tick／Sequence 顺序、rejectedCount、cursor 递增、容量与 prune 校验不变。checkpoint 仍复制输入为独立 List，只按 IReadOnlyList 暴露，删除包装对象。
- 每次 authority confirmation 删除 updates List 及第二次循环，每次 journal checkpoint 删除 ReadOnlyCollection；最终 entries 副本和 checkpoint List 仍按事务独立存在。ThirdPersonSimulation.ServerAuthoritative portable 编译零警告零错误，首次构建前检测到 Unity Bee 编译并等待其退出，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 confirmation 运行采样。

## 2026-09-21 Rollback 排序历史边界移除

对应 tasks.md 的 5.71。

- RollbackInputHistory、RollbackSnapshotHistory 与每个 peer 的 RollbackStateHashHistory 共用 RemoveThrough。字典 key 均为升序 tick，原实现仍先把边界内 key 收集进新 List，再第二次循环删除。输入历史 CaptureEntries 还对已独立的结果 List 创建 ReadOnlyCollection。
- RemoveThrough 现只要字典非空就读取最小 key，超过边界立即结束，否则删除后继续；不会在枚举期间修改活动枚举器，也不扫描确认边界之后的条目。删除顺序和三个历史的 count／floor 语义保持。
- 输入历史捕获仍返回独立 List，仅按 IReadOnlyList 暴露。删除每次 confirmed horizon 释放的 key List，以及每次输入历史捕获的只读包装。ThirdPersonSimulation.DeterministicRollback 与 Endpoint portable 分别编译零警告零错误，构建服务逐次关闭。未新增测试、未操作共享 Unity、未做 rollback 历史运行采样。

## 2026-09-21 Rollback Endpoint 显式输入确认释放

对应 tasks.md 的 5.72。

- RollbackEndpointInputSourcePort 为每个 Actor 持有按 tick 排序的显式输入字典。确认推进时原实现为每个 Actor 新建 remove List，先收集 confirmed 边界内 tick 并记住最后帧，再第二次循环删除并更新全局数量。
- 释放现反复读取该 Actor 字典的最小键；最小 tick 超过 confirmed 立即结束，否则记录为 latest、删除并递减 m_ExplicitCount。循环结束后仍把最后删除帧转换为 ConfirmedExplicit 写入 m_LastConfirmed。
- 删除每次确认释放按 Actor 创建的 key List，不改变最后确认输入选择、Actor 遍历、历史容量或预测缺帧逻辑。ThirdPersonSimulation.DeterministicRollback.Endpoint portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 Endpoint 输入运行采样。

## 2026-09-21 Rollback RuntimeBridge tick 索引顺序

对应 tasks.md 的 5.73。

- RuntimeBridge 的 local reports、按 tick 聚合的 remote reports 和 requested snapshots 都以 tick 为唯一键，并受 HistoryLengthTicks／MaximumQueuedSnapshots 限制；原先使用普通 Dictionary，窗口释放只能新建 remove List 扫描全部键后第二次删除。
- 三张外层表统一为 SortedDictionary，peerId 内层表继续使用 Ordinal Dictionary。RemoveBefore 反复读取最小 tick，小于 floor 就删除，达到 floor 立即结束；诊断 latest、恢复候选和容量计数语义保持，遍历顺序从未指定改为稳定 tick 顺序。
- 取舍是单条 report／request 查找从哈希平均 O(1) 变为有界历史上的 O(log n)，换取每次 Pump 窗口释放不再创建 key List，并使 tick 顺序成为容器正式语义；网络在项目中是压力场景，历史规模由策略明确限制。
- ThirdPersonSimulation.DeterministicRollback.Endpoint portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 Endpoint 网络运行采样。

## 2026-09-21 RollbackIngressBatch arrival 数组所有权

对应 tasks.md 的 5.74。

- Endpoint Read 每 tick 先收集 relayed explicit 到本地 List，并按 m_CanonicalPending.Count 新建 canonical arrival 数组；原 RollbackIngressBatch 再把两者分别复制到 List、排序并创建 ReadOnlyCollection。canonical 数组因此被完整二次复制。
- Batch 构造收窄为本 tick 独占的两类数组，在接管数组上执行原 tick／Actor、provenance、null 与重复项校验并直接按 IReadOnlyList 保存。Endpoint 将 explicit List 一次物化为最终数组，canonical 数组直接转移；空集合复用 Array.Empty。
- 删除每个 rollback ingress tick 的两只结果 List、两只 ReadOnlyCollection，以及 canonical arrival 的第二份存储；explicit 收集 List 和必要的最终数组仍存在，后续可在有正式容量边界时再治理收集 workspace。
- ThirdPersonSimulation.DeterministicRollback 与 Endpoint portable 分别编译零警告零错误，构建服务逐次关闭。未新增测试、未操作共享 Unity、未做 ingress 运行对比或 Player 分配采样。

## 2026-09-21 Rollback 输入协议最终数组

对应 tasks.md 的 5.75。

- RollbackActorInputBatch、RollbackRelayedExplicitInputBatch、RollbackCanonicalInputBundle 与 RollbackCanonicalConfirmation 都处于每包或每 tick 协议链路。原构造从 IEnumerable 创建 List，在 List 上排序校验，再创建 ReadOnlyCollection 包装；协议对象仍依靠 List 的底层数组保存独立结果。
- 四类公开构造收窄为 IReadOnlyList，按准确 Count 复制到最终数组，在同一数组上执行原排序和完整性校验，并直接以 IReadOnlyList 暴露。外部输入仍不会被借用，空输入沿用原拒绝语义。
- 每个协议对象删除一只结果 List 和一只 ReadOnlyCollection；最终独立数组仍按协议对象寿命存在。解码器和组装器已经新建数组时仍会经过公开构造复制，后续需要以显式所有权入口单独收口，避免混淆公开安全构造与内部转移。
- ThirdPersonSimulation.DeterministicRollback 与 Endpoint portable 分别编译零警告零错误，构建服务逐次关闭。未新增测试、未操作共享 Unity、未做协议吞吐或 Player 分配采样。

## 2026-09-21 Rollback 输入协议数组所有权转移

对应 tasks.md 的 5.76，收口 5.75 中保留的新建数组二次复制。

- 输入 batch、relayed batch、canonical bundle 与 confirmation 解码均已按协议数量新建完整数组；canonical assembler、Endpoint 预测、迟到输入替换和 relay 转发也在当前方法内新建最终 frame 数组。confirmation 捕获返回的连续 bundle 数组同样只交给随后创建的 confirmation。
- 四类协议对象新增显式 FromOwnedFrames／FromOwnedActors／FromOwnedBundles 入口，在接管数组上执行原排序和校验。上述调用统一转移数组；CaptureCanonicalRange 的返回类型收窄为数组，使连续确认捕获直接成为 confirmation 的最终存储。
- 每个迁移调用删除一份完整数组复制，保留协议对象自身所需的最终数组。RollbackPeerEndpoint 的输入冗余发送列表会在缩减包体时继续改写，因此仍调用公开复制构造，不转移工作区；外部一般集合也继续由公开构造独立复制。
- ThirdPersonSimulation.DeterministicRollback 与 Endpoint portable 分别编译零警告零错误，构建服务逐次关闭。未新增测试、未操作共享 Unity、未做网络协议吞吐或 Player 分配采样。

## 2026-09-21 Rollback state hash 最终数组

对应 tasks.md 的 5.77。

- RollbackHashEgressPass 在每个 hash tick 已按 world Actor 数新建完整 RollbackActorHash 数组，协议解码也按报文 Actor 数新建完整数组；原 RollbackStateHashReport 随后又从输入创建 List、排序，并创建 ReadOnlyCollection 包装。
- report 公开构造收窄为 IReadOnlyList，按准确 Count 复制到最终数组后原地排序校验；新增 FromOwnedActors 供 hash 生产与协议解码转移本方法新建数组。通用复制帮助类改名为 RollbackProtocolArray，供输入与 hash 协议共用。
- 每个本地或解码 state hash report 删除结果 List、ReadOnlyCollection 与一份完整 Actor hash 数组复制；最终数组及每 Actor hash 对象仍按报告寿命存在。外部一般集合继续通过公开构造独立复制，初始化期 RollbackRoster 暂未混入本步。
- ThirdPersonSimulation.DeterministicRollback 与 Endpoint portable 分别编译零警告零错误，构建服务逐次关闭。未新增测试、未操作共享 Unity、未做 hash 发送频率或 Player 分配采样。

## 2026-09-21 Rollback policy 枚举具体校验

对应 tasks.md 的 5.78。

- RollbackMissingInputPolicy 的正式集合只有 ContinuousValuesWithEmptyRequests 与 NeutralValuesWithEmptyRequests，RollbackSnapshotAuthority 只有 LowestPeerId；源码没有保留值或位组合语义。原 policy 构造分别调用 Enum.IsDefined(Type, value)，Server manifest 的泛型 RequireEnum 解析后也调用同一路径。
- policy 构造改为直接比较正式成员。manifest 拆成 RequireMissingInputPolicy 与 RequireSnapshotAuthority 两个具体入口，继续使用区分大小写的 Enum.TryParse，并对解析结果按相同正式集合校验；有效名称、有效数字字符串和异常结果不变。
- 删除 rollback 模型构造及服务端 manifest 配置解析的 Enum.IsDefined 装箱。该配置链主要发生在初始化而非每帧，本步只记录静态分配入口删除，不宣称稳态收益。
- ThirdPersonSimulation.DeterministicRollback 与 Endpoint portable 分别编译零警告零错误，构建服务逐次关闭。未新增测试、未操作共享 Unity、未做配置加载分配采样。

## 2026-09-21 Rollback relay accepted input 工作区

对应 tasks.md 的 5.79。

- RollbackCanonicalInputAssembler.SubmitBatch 只有 relay 的 ReceiveInput 一个消费者。原实现按输入帧数创建 accepted List，逐帧提交后再创建 ReadOnlyCollection；消费者随即把 accepted 帧转换为新的 relayed frame 数组，结果不会跨调用保存。
- relay 以 inputRedundancyCount 这一正式包内帧数上限预分配 accepted input 工作列表。SubmitBatch 改为清空并填充调用方列表；ReceiveInput 在同一同步调用内计算去重数并生成最终 relayed 数组，finally 清空列表，正常返回和异常路径都不保留帧引用。
- 删除每个接收输入包的一只 accepted List、其底层数组和一只 ReadOnlyCollection。最终 relayed frame 数组与每帧协议对象仍按广播消息寿命存在；assembler 的显式输入历史所有权不变。
- ThirdPersonSimulation.DeterministicRollback 与 Endpoint portable 分别编译零警告零错误，构建服务逐次关闭。未新增测试、未操作共享 Unity、未做 relay 包速率或 Player 分配采样。

## 2026-09-21 Rollback Endpoint explicit arrival 工作区

对应 tasks.md 的 5.80。

- Endpoint InputSource 每次 Read 会在前后两次 Pump 后排空所有 relayed explicit 包，同 tick 到达量可能包含多个 Actor、多个冗余包和多个 tick，不能仅按 roster 数预分配。原实现每次 Read 新建 List，最终再 ToArray 交给 RollbackIngressBatch。
- InputSource 现跨 tick 持有一只 arrival 工作列表，Read 开始时清空、两次 drain 共用，返回时只生成 ingress batch 必须独立持有的最终数组，并在 finally 清除所有帧引用。列表容量随实际观测峰值增长并保留，不按 HistoryLengthTicks 与 MaximumQueuedBundles 的理论总容量预分配大块常驻存储。
- 删除每次 Read 的 List 对象及稳定容量后的工作缓冲数组分配；首次增长和更高突发峰值仍可能扩容，最终 arrival 数组仍按 batch 寿命存在。canonical arrival 已直接填最终数组，不受本步影响。
- 首次验证前检测到共享 Unity Bee 正在编译并等待其退出；随后 ThirdPersonSimulation.DeterministicRollback.Endpoint portable 编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 Endpoint 到达突发或 Player 分配采样。

## 2026-09-21 Rollback solver payload 直接哈希

对应 tasks.md 的 5.81。

- RollbackHashEgressPass 每个 hash tick 解码 WorldSimulationState 后，只需计算 solver payload 的 SHA-256；原调用先对状态内部 ReadOnlyMemory<byte> 执行 ToArray，再把新数组交给 SimulationCanonicalPayloadHash。
- SimulationCanonicalPayloadHash 新增 ReadOnlySpan<byte> 正式入口，原 byte[] 入口继续做 null 校验后委托给 span 实现。hash egress 直接传 SolverStatePayload.Span，摘要算法、十六进制格式和 KccHash 字段不变。
- 删除每个 rollback hash tick 一份与 solver payload 等长的 byte[] 复制。SHA256 实例、最终哈希字符串和 world state 解码自身成本仍保留，本步不宣称整条 hash egress 无分配。
- ThirdPersonSimulation.DeterministicRollback portable 连同 Core 与 Fixed 依赖编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 hash cadence 或 Player 分配采样。

## 2026-09-21 ExecutionPlan 最终数组

对应 tasks.md 的 2.91。

- SimulationSessionExecutionPlan 是 Fixed、Float32、rollback 与 server-authoritative 共用的每 outer tick 计划对象。原 steps 若不是数组会再创建 List，source mappings 同样创建并排序第二只 List；rollback 和预测权威生产者本身已经用 List 组装，因而计划构造会再产生一只集合对象和一份带容量的底层存储。
- 构造输入收窄为 IReadOnlyList。steps 按准确 Count 复制到最终 TStep[]；mappings 按准确 Count 复制到最终数组后原地排序并执行原 outer clock／重复映射校验。空输入继续复用 Array.Empty，计划仍不借用调用方集合。
- 删除非数组生产者每个计划的第二只 List 及其容量冗余，最终独立数组仍按 plan 寿命存在。数组生产者当前仍由公开构造复制，显式所有权转移需要在各调度器证明数组不再修改后另行处理。
- 首次 Fixed portable 构建发现局部 values 已收窄为数组但五处校验仍使用 Count，改为 Length 后，Fixed、Float32、DeterministicRollback 与 ServerAuthoritative portable 均编译零警告零错误；构建服务逐次关闭。未新增测试、未操作共享 Unity、未做 schedule 运行对比或 Player 分配采样。

## 2026-09-21 ExecutionPlan 单步数组所有权

对应 tasks.md 的 2.92，收口 2.91 中单步数组生产者仍保留的二次复制。

- Fixed 本地、Float32 本地和 Float32 authority schedule 都在当前方法内分别新建单元素 source mapping 数组与 step 数组，构造 plan 后不再持有或修改。原公开构造为保持通用输入独立性，会再次复制这两只数组。
- SimulationSessionExecutionPlan 新增 FromOwnedArrays，接管 mappings 后原地执行既有排序与重复映射校验，并直接保存 steps。三条单步生产路径迁移到所有权入口；公开构造仍按 IReadOnlyList 复制，动态列表生产者尚未借用工作区。
- 每个本地或 authority 单步计划删除一份 mapping 数组克隆和一份 step 数组克隆；原始单元素数组直接成为 plan 最终存储。空计划继续复用 Array.Empty。
- 首次构建发现 FreezeMappings 异常参数名仍引用重命名前的 source，改为 sourceMappings 后 Fixed 编译通过；Unity 随后启动 Bee 编译，等待退出后 Float32、ServerAuthoritative 与 DeterministicRollback portable 均编译零警告零错误。构建服务逐次关闭。未新增测试、未操作共享 Unity、未做 schedule Player 分配采样。

## 2026-09-21 Rollback schedule 最终数组

对应 tasks.md 的 5.82。

- Rollback schedule 的 replay steps 数量由 replayStart 到 CurrentCompletedTick 的闭区间确定；是否附加 current step 由 prediction lead 与 replay 是否为空确定。source mapping 对每种实际 step clock 各一条，因此数量同样可在构建前准确得到，最大为 replay 与 current 两条。
- BuildPlan 先确定 restore／replay 边界，再计算 replayStepCount 与 includeCurrentStep，按准确数量创建最终 steps 和 mappings 数组。replay 通过整数索引填入原连续 tick 序列，planSequence 仍从 1 严格递增；current step 的 Forward／Current 分类、typed ingress 和 paced NoStep 条件保持。
- 两只最终数组直接交给 SimulationSessionExecutionPlan.FromOwnedArrays。删除每个 rollback outer tick 的 steps List、mappings List、各自增长存储及 plan 内二次复制；每个实际 step、Actor input 数组和最终 plan 数组仍按执行计划寿命存在。
- ThirdPersonSimulation.DeterministicRollback portable 连同 Core 与 Fixed 依赖编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 rollback replay 运行对比或 Player 分配采样。

## 2026-09-21 预测权威 schedule 最终数组

对应 tasks.md 的 5.83。

- ServerAuthoritative prediction schedule 的 plan step 数严格等于 replay.Count 加 currentStepCount，后者已被正式限制在 0～2；source mapping 只在 current 或 replay 实际存在时各创建一条。原实现分别用 List 逐项加入，ExecutionPlan 再复制到最终数组。
- BuildPlan 按准确数量创建 steps 与 mappings 数组并以索引填充，replay/current 顺序、tick 递增、input sequence floor、planSequence、ObservedWorldConstraintFrame 和 requirement 判定保持。数组直接转交 SimulationSessionExecutionPlan.FromOwnedArrays。
- 删除每个预测 outer tick 的 steps List、mappings List、各自增长存储及 plan 二次复制。selectedRemoteBodies 是随后提交给独立产品的业务输出，本步继续使用原 selectedBodies List 与只读包装，不混淆其寿命。
- ThirdPersonSimulation.Float32 与 ServerAuthoritative portable 分别编译零警告零错误，构建服务逐次关闭。未新增测试、未操作共享 Unity、未做 prediction replay 运行对比或 Player 分配采样。

## 2026-09-21 Camera Projection 枚举直接校验

对应 tasks.md 的 7.18。

- Camera Projection payload 的 RequireValid 在 FrameTwoPoints、OverrideTrack、Sequence、Shake、Shot、Stretch、TargetSlot、Zoom 与 CharacterProjection 九类对象中调用十四次 Enum.IsDefined。涉及 CameraEffectStackingType、CameraFovVariationType、CameraSpace、CameraTimeDomain 与 CameraSequenceStageKind；五类均为 byte 枚举，正式成员从首项到末项连续且无 Flags 或保留空洞。
- 十四处校验改为对首尾正式成员的 byte 区间比较。原空值、数值有限性、范围、资源引用、stage 类型与目标槽规则保持；非法 0、超出末项和所有原合法成员的结果不变。
- 删除这些 Camera payload 每次 RequireValid 的枚举装箱与反射查询。实际调用频率可能包含资源准备和运行投影重校验，本步未把静态入口删除等同于每帧收益。
- ThirdPersonClient.Runtime 无依赖构建被并行 Timeline API 迁移的六处缺失成员错误阻断，错误均位于 CharacterTimelineHost，另有一条既有未使用字段警告；未触碰并行文件。ThirdPersonCamera.Contracts 无依赖编译零警告零错误，构建服务逐次关闭。未新增测试、未操作共享 Unity、未做 Camera Player 分配采样。

## 2026-09-21 Rollback output Actor 工作区

对应 tasks.md 的 5.84。

- RollbackOutputCommitter.ResolveActorTick 对每个 completed Actor/tick 创建 existing slot List、current record List 与 seen slot HashSet；FlushConfirmed 每次确认推进再创建 release List。四类集合只服务当前同步解析，不进入 output operation、registry 或事务结果。
- committer 现持有四只私有工作集合。Actor 解析开始清空并复用，current records 直接填入工作列表后排序；确认释放收集到共用 release slots 后排序删除。两条路径均用 finally 清空，正常、提前异常都不会长期保留 RollbackOutputRecord 引用。
- 删除每个 Actor/tick 三只集合对象及稳定容量后的底层存储分配，也删除每次确认释放的 List 对象；首次填充或更高输出峰值仍可能扩容。事务 records 副本、disposition 索引与 operation List 保持原独立失败边界，本步未混入。
- ThirdPersonSimulation.DeterministicRollback portable 连同 Core 与 Fixed 依赖编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 output commit 运行对比或 Player 分配采样。

## 2026-09-21 Rollback output Commit 工作区

对应 tasks.md 的 5.85。

- RollbackOutputCommitter 每次 Commit 都从 disposition batch 新建 EventId 索引 Dictionary，并新建 output operation List；两者只用于当前事务规划和发布，不进入 m_Records 或提交后的业务状态。
- committer 现持有 disposition index 与 operations 两只提交级工作区。公开 Commit 负责清空、调用 CommitPrepared，并在 finally 再次清空；索引冲突、容量失败、source egress 失败、output Abort 或成功完成均不会保留本批键值和 RollbackOutputRecord 引用。
- 删除每次 Commit 的 Dictionary 与 List 对象，以及达到观测峰值后的桶／数组重复分配；首次填充和更高峰值仍可能扩容。m_Records 的 tentative 副本继续每次独立创建，成功前不覆盖正式 registry，原事务失败边界保持。
- ThirdPersonSimulation.DeterministicRollback portable 连同 Core 与 Fixed 依赖编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 output commit 失败注入或 Player 分配采样。

## 2026-09-21 Rollback output registry 双缓冲

对应 tasks.md 的 5.86。

- output Commit 原先通过 Dictionary 拷贝构造为每次事务创建完整 next registry，所有 Resolve／Flush 只改 next，发布完成后才替换 m_Records。这个独立副本是失败隔离需要，但 Dictionary 对象和桶不必每次重新创建。
- committer 现持有正式 m_Records 与 tentative m_RecordWorkspace 两张表。CommitPrepared 先把正式条目复制进空 workspace，全部规划、容量校验与发布仍只作用于 workspace；成功后交换两张表，外层 finally 清空交换后的旧正式表。任意成功前异常则清空 tentative 表，正式 registry 不变。
- 删除每次 Commit 的 records Dictionary 对象及稳定容量后的桶／entry 数组分配；逐条复制和两张表的峰值常驻容量仍存在，这是保留事务隔离的明确成本。未改为原地修改、撤销日志或兼容路径。
- ThirdPersonSimulation.DeterministicRollback portable 连同 Core 与 Fixed 依赖编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 output commit 失败注入或 Player 分配采样。

## 2026-09-21 Rollback output disposition 工作区

对应 tasks.md 的 5.87。

- RollbackOutputDispositionPass 每次 outer tick 遍历 completed steps，将 gameplay facts 与 presentation commands 映射为 disposition；原实现每次 Execute 新建 List。下游 SimulationPipelineOutputDispositionSet 会复制并排序为独立最终数组，生产列表不会跨 Execute 保存。
- pass 现持有一只 disposition 工作列表，Execute 开始清空、填充后交给 set 的公开复制构造，并在 finally 清除。正常写入和任意中途异常都不保留本 tick 内容；实现与 Fixed／Float32 local immediate output pass 的既有工作区模式统一。
- 删除每个 rollback outer tick 的 List 对象及达到观测峰值后的底层数组重复分配；首次填充和更高事件峰值仍可能扩容，最终 disposition 数组保持必要的独立寿命。
- ThirdPersonSimulation.DeterministicRollback portable 连同 Core 与 Fixed 依赖编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 disposition 事件峰值或 Player 分配采样。

## 2026-09-21 Rollback Endpoint diagnostics 最终数组

对应 tasks.md 的 7.19。

- Endpoint InputSource 的 remote diagnostics 字典在 roster 锁定后包含全部 Actor，其中本地 Actor 由 m_LocalActorId 明确标识。原 CaptureDiagnostics 先把其余 Actor 加入 List、排序，再 ToArray 生成快照最终存储。
- 捕获现以字典数量减去实际存在的本地 Actor 得到准确长度，直接填充 RollbackRemoteActorInputDiagnosticsSnapshot 数组并原地按 ActorId 排序。未锁定 roster 时仍得到 Array.Empty 等价的零长度数组，远端字段与排序保持。
- 每次 diagnostics 捕获删除 List 对象、List 底层数组和 ToArray 的第二次复制；最终独立数组仍按诊断快照寿命存在。该入口是否按帧读取未采样，不把静态删除宣称为稳态 Player 收益。
- ThirdPersonSimulation.DeterministicRollback.Endpoint portable 连同依赖编译零警告零错误，构建服务关闭成功。未新增测试、未操作共享 Unity、未做 diagnostics 刷新频率或 Player 分配采样。

## 2026-09-21 SimulationActorTickResult 最终数组

对应 tasks.md 的 2.93。

- Fixed／Float32 AbilityFinalize 为每个 Actor 每个 completed tick 创建 SimulationActorTickResult。原结果对象分别从 evaluation 的 gameplay facts、presentation commands 与 trace records 创建 List，再为三只 List 各建一只 ReadOnlyCollection；这些集合是 result 的最终长期存储。
- 两域构造输入收窄为 IReadOnlyList，按准确 Count 将三类输出复制到最终数组，空输入复用 Array.Empty。原 header 数值域、ActorId 与 tick 校验继续遍历最终数组，公开结果仍不借用 evaluation 集合。
- 每个 Actor completed tick 删除三只 List 和三只 ReadOnlyCollection，并消除 List 容量冗余；三只必要的最终数组仍按 result 寿命存在。evaluation 到 finalize 仍有一层独立复制，待明确消费后所有权转移另行收口。
- ThirdPersonSimulation.Fixed、Float32、DeterministicRollback 与 ServerAuthoritative portable 均编译零警告零错误，构建服务逐次关闭。未新增测试、未操作共享 Unity、未做 completed tick Player 分配采样。

## 2026-09-21 CharacterEvaluationResult 数组所有权

对应 tasks.md 的 2.94。

- Fixed／Float32 CharacterEvaluationRuntime 在每 Actor 每 step 内用 List 聚合多次 ability invocation 的 gameplay facts、presentation commands、trace records，以及 timeline advance／stop；返回 evaluation 后这五只本地 List 不再使用。原 evaluation 构造又为五类结果各创建 List 并创建 ReadOnlyCollection。
- 两域 runtime 在成功返回边界把五只聚合 List 各物化一次为准确数组，CharacterEvaluationResult 内部构造直接接管并按 IReadOnlyList 暴露。候选状态、Timeline runtime、Consume／Discard 及 timeline commit 顺序不变；异常发生在返回前时仍走原 discard 清理。
- 每个 Actor evaluation 删除五只结果 List、五只 ReadOnlyCollection 及从聚合 List 到结果 List 的二次元素复制；五只最终数组仍按 evaluation 寿命存在。Finalize 到 SimulationActorTickResult 的安全复制暂时保留。
- ThirdPersonSimulation.Fixed、Float32、DeterministicRollback 与 ServerAuthoritative portable 均编译零警告零错误，构建服务逐次关闭。未新增测试、未操作共享 Unity、未做 ability evaluation Player 分配采样。

## 2026-09-21 Evaluation 到 ActorTickResult 输出转移

对应 tasks.md 的 2.95，收口 2.93／2.94 之间保留的 evaluation 到 finalize 数组复制。

- AbilityFinalize 先 Consume evaluation，提交 Timeline 输出并得到最终 CharacterRuntimeState，随后原构造会把 evaluation 已独立持有的 facts、presentation 与 trace 三只数组再复制到 SimulationActorTickResult。evaluation 在 finalize 后不再消费这些输出。
- 两域 CharacterEvaluationResult 增加一次性 TakeOutputs：只允许 Consume 成功标记 committed 后调用，取走三只数组后把内部引用置为 Array.Empty，再次取走或 discard 路径调用会失败。未使用的直接集合 getter 删除，输出所有权只有这一条正式链路。
- 两域 SimulationActorTickResult 增加 internal FromOwnedOutputs，在接管数组上执行原 header 数值域／Actor／tick 校验；公开构造继续复制一般 IReadOnlyList。Finalize 先计算状态哈希，再取走数组并进入 owned 构造。
- 每个 Actor completed tick 删除三只与输出数量等长的数组复制，最终数组及所有事件对象仍按 ActorTickResult 寿命存在。ThirdPersonSimulation.Fixed、Float32、DeterministicRollback 与 ServerAuthoritative portable 均编译零警告零错误，构建服务逐次关闭。未新增测试、未操作共享 Unity、未做 finalize Player 分配采样。

## 2026-09-21 CompleteStep 结果数组所有权

对应 tasks.md 的 2.96。

- Fixed／Float32 CompleteStep 原先把 Finalize Product 的每 Actor 结果加入 `workspace.ActorResults` List，排序和 Roster 校验后，再由 `SimulationTickResult` 复制成长期数组。该 workspace List 只有两域 CompleteStep 使用，外层事务其它阶段不读取。
- 两域现按 `finalizedCount` 直接创建最终 `SimulationActorTickResult[]`，填充后原地排序并执行原 Roster 校验；`SimulationTickResult.FromOwnedActors` 接管该数组并继续生成 OutputEvent 数组。公开 `SimulationTickResult` 构造仍复制一般 `IReadOnlyList`，输入隔离没有放宽。
- 删除每个 session workspace 的 ActorResult List、底层容量和一轮 List 到最终数组的元素复制；最终 Actor 数组和事件数组仍是跨 completed step 必须保留的结果。workspace 的 ActorState／Egress／CompletedStep 存储及提交寿命不变。
- 同步收窄 `SessionExecutionWorkspace` 类型参数和 PipelineTransaction 接口／Coordinator，未保留无消费者的 ActorResult 泛型入口。Fixed、Float32、DeterministicRollback、ServerAuthoritative portable 均编译零警告零错误，构建服务逐次关闭；未新增测试、未操作共享 Unity、未做 completed step Player 分配采样。

## 2026-09-21 FBBIK 配置准备边界与重复校验清理

对应 tasks.md 的 7.20。

- `CharacterAnimationPresentationProfile.CollectConfigurationErrors`、Pose authoring/content preparation 负责 Full Body IK Profile 的 schema、Profile identity、revision、枚举和值域；正式运行实例只接收已经装配到 Pose 域的 Profile、Rig 和资源 binding。
- `CharacterFinalIkFullBodySolver` 删除构造时对 Rig/Profile 的重复 `RequireValid`，删除已通过 `PrepareTuningCandidate` 的 candidate 在 Commit 时的二次校验。`CharacterFinalIkPoseBufferBackend` 的构造和 Biped reference 创建只消费上游已确认的 Rig，不再重复遍历 Rig 配置。
- FBBIK Profile 与仍用于 runtime tuning 输入的 ActiveTuning 保留原合法成员和范围，但 smoothing 改为显式连续值域判断，删除 `Enum.IsDefined` 装箱；未改变 ApplyTuning、Prepare/Commit/Discard 顺序或 solver 公式。
- 保留 `Prepare`／`SolvePrepared` 的 pose page、goal workspace、lineage、重复 effector、非有限输出和 solver residual 检查；这些保护当前事务和外部求解结果，不属于静态配置重复校验。
- `ThirdPersonClient.Runtime.csproj` 使用 `dotnet build --no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译成功，保留 1 条既有 `CharacterInputValueNodes.cs` CS0414 警告；随后 `dotnet build-server shutdown` 成功。未新增测试、未操作共享 Unity、未做 Player 分配采样。

## 2026-09-21 Pose Graph 准备结果复用

对应 tasks.md 的 7.21。

- `CharacterPoseNativeGraphRuntime.Prepare` 已通过 `CharacterPoseNativeGraphValidator.RequireValid` 产生 `CharacterPoseNativePreparedBinding`；`Create` 后的 `InitializeGraph` 原先又对同一 GraphAsset、Graph 和 Boundary 做完整拓扑/端口/边界遍历。
- 删除初始化阶段的第二次整图校验，运行实例直接使用准备结果；`BuildPortDefinitions`、handler 创建/身份匹配、Attach/Start 以及每帧 lineage、buffer 和提交事务校验保持。没有把图外数据重新读取成另一条链路。
- 删除每次 Pose 实例创建的重复图校验遍历及其临时集合分配；authoring/content preparation 仍是静态图合法性的唯一入口。
- `ThirdPersonClient.Runtime.csproj` 使用 `dotnet build --no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译成功，保留 1 条既有 `CharacterInputValueNodes.cs` CS0414 警告；随后 `dotnet build-server shutdown` 成功。未新增测试、未操作共享 Unity、未做 Pose 实例分配采样。

## 2026-09-21 Pose handler Rig 校验边界

对应 tasks.md 的 7.22。

- 正式入口先由 `CharacterAnimationRigBinding.RequireValid(animationRig)` 校验 Projection Rig，随后 `CharacterPoseNativeGraphPrepareRequest` 通过 `Rig.RequireValid` 形成 PreparedBinding；handler factory 只从该准备结果创建 Additive、Inertialization、Layered Bone Blend、Modify Bone、Root Orientation Warp 和 Space Conversion handler。
- 删除上述六类 handler 构造器及 `CharacterPoseNativeInstanceContext` 的第二次 Rig schema 遍历；保留 Rig 与场景 binding 的身份比对、节点配置/资源类型、骨骼索引、buffer 形状、曲线和输入变量等正式链路约束。
- 没有改变 handler 创建顺序、资源释放、Attach/Start、帧 Begin/Prepare/Evaluate/Commit/Discard 或 publication 事务；删除的是同一份静态 Rig 配置在运行构造链的重复读取和校验。
- `ThirdPersonClient.Runtime.csproj` 使用 `dotnet build --no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译成功，保留项目既有警告；随后 `dotnet build-server shutdown` 成功。未新增测试、未操作共享 Unity、未做 Pose 实例分配采样。

## 2026-09-21 Blend Stack 准备 payload 复用

对应 tasks.md 的 7.25。

- `CharacterPoseNativeDomainServiceFactory.CreateBlendStack` 已在创建 `AnimationBlendStackPolicyPayload`、曲线 catalog 和 profile payload 时完成对应配置、曲线和值域校验；profile 额外在此处完成 Rig identity、revision 和 dense bone shape 对齐，transition 在 curve/profile index 已生成后完成一次完整校验。
- 删除 `AnimationBlendStackRuntime` 对同一 Rig、stack policy、curve catalog、profile catalog 和 transition 的第二次完整遍历，避免 catalog 校验中的 HashSet/Dictionary 及枚举反射重复分配；运行实例仍保留 slot/node、owner/provider、catalog 非空和 final buffer layout 约束。
- 不改变 Blend Stack 的容量、初始 page、source workspace、frame Begin/Prepare/Evaluate/Commit/Discard 或 tuning generation 语义；静态错误仍在正式实例创建前的 payload 准备阶段抛出。
- `ThirdPersonClient.Runtime.csproj` 使用 `dotnet build --no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译成功，保留项目既有警告；随后 `dotnet build-server shutdown` 成功。未新增测试、未操作共享 Unity、未做 Blend 实例分配采样。

## 2026-09-21 Motion Matching Clip binding 单次校验

对应 tasks.md 的 7.26。

- `MotionMatchingDatabasePayload` 构造阶段的 `ValidateCanonical` 已遍历所有 Clip binding 并调用一次 `MotionMatchingClipBindingPayload.RequireValid`；`CharacterMotionMatchingRuntimeDatabase` 只持有该 canonical payload。
- 删除 `MotionMatchingClipSamplePlan` 每次选样对 binding 的第二次完整 `RequireValid`，并让 source resolve 只保留当前 sample 的 Clip binding 存在性检查；selected sample index、时间范围、Loop 连续时间、AnimatorStateSpeed、Foot 参数身份和最终输出 `IsValid` 仍保留。
- 没有改变数据库、selection generation、sample 选择、pose source 输出或 frame 提交顺序；删除的是不可变 canonical binding 的 per-selection 重复 schema 校验。
- `ThirdPersonClient.Runtime.csproj` 使用 `dotnet build --no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译成功，保留项目既有警告；随后 `dotnet build-server shutdown` 成功。未新增测试、未操作共享 Unity、未做 Motion Matching 分配采样。

## 2026-09-21 Timeline NumericTarget 校验边界

对应 tasks.md 的 7.27。

- Timeline composition 创建时的 NumericTarget 是固定配置，`TimelineRuntimePlaybackRequestFactory` 原先又对它调用一次 `Enum.IsDefined`；正式 Prepare 链随后由 `TimelineRuntimePrepareRequest` 对进入准备边界的 NumericTarget 和动态 PlaybackMode 做一次校验。
- 删除 request factory 的重复 NumericTarget 反射校验，保留 PrepareRequest 的唯一请求边界校验；没有触碰 Timeline snapshot 的 managed reference clone、Capture/Restore、handle/generation、event ordering 或 pending/committed 事务。
- `ThirdPersonClient.Runtime.csproj` 使用 `dotnet build --no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译成功，保留项目既有警告；随后 `dotnet build-server shutdown` 成功。未新增测试、未操作共享 Unity、未做 Timeline 分配采样。

## 2026-09-21 Pose ResourceCatalog Rig 校验边界

对应 tasks.md 的 7.28。

- Presentation Domain 外层先通过 `CharacterAnimationRigBinding.RequireValid(animationRig)`，Pose graph preparation 又在 `CharacterPoseNativeGraphPrepareRequest` 形成 PreparedBinding 时校验 Rig；Source Catalog、Foot Placement Module 和 Managed Source Catalog 原先各自再次完整执行 `Rig.RequireValid`。
- 删除这三处重复 Rig schema 遍历；保留 source plan/descriptor 合法性、Rig identity/revision 对齐、ACL resource registration、Foot profile/calibration、linked implementation、bone index、capacity 和 Native shape 校验。
- SourceCatalog 仍只在 ServiceFactory 创建阶段准备一次，Foot/Managed resource 仍按原顺序创建和释放；没有改变资源租约、handler 组合、约束求解或帧事务。
- `ThirdPersonClient.Runtime.csproj` 使用 `dotnet build --no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译成功，保留项目既有警告；随后 `dotnet build-server shutdown` 成功。未新增测试、未操作共享 Unity、未做 Pose 资源分配采样。

## 2026-09-21 Blend SourceWorkspace Rig 校验边界

对应 tasks.md 的 7.30。

- Clip Player、Selected Pose Player、Blend Space Player 和 Blend Stack 都从正式 Pose Domain ServiceFactory 的 Rig 创建 `AnimationBlendSourcePoseWorkspace`；外层 Presentation Rig binding 与 Pose preparation 已完成 Rig schema 校验。
- 删除 Workspace 构造器的 `rig.RequireValid`，保留 Rig null、parameter/source capacity、bone count 派生、NativeArray 分配和每帧 source/page/lease 事务校验；不改变四类 player 的 source handoff 或提交顺序。
- 这一步删除的是多个 workspace 实例创建时重复的静态 Rig 遍历及其临时校验集合，不删除当前 buffer shape 或 source completion 事实校验。
- `ThirdPersonClient.Runtime.csproj` 使用 `dotnet build --no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译成功，保留项目既有警告；随后 `dotnet build-server shutdown` 成功。未新增测试、未操作共享 Unity、未做 Blend Workspace 分配采样。

## 2026-09-21 删除旧 Blend Stack kernel

对应 tasks.md 的 7.31。

- `CharacterAnimationBlendStackKernel` 中的 push request、owner workspace 和 frame plan 在项目内没有任何调用者；现行 Pose Blend Stack 由 `AnimationBlendStackRuntime` 创建并承接全部 source、frame plan 和提交事务。
- 删除旧 kernel 及其 Unity meta，消除无消费者的重复 Blend Stack 实现、数组工作区和 policy 静态校验入口；不改现行 runtime 的容量、采样、frame 或 Commit／Discard 语义。
- `ThirdPersonClient.Runtime.csproj` 使用 `dotnet build --no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译成功，保留项目既有警告；随后 `dotnet build-server shutdown` 成功。未新增测试、未操作共享 Unity、未做 Blend Stack 分配采样。

## 2026-09-21 Blend source binding 工作集合容量定型

对应 tasks.md 的 7.32。

- Blend Stack 的 `EntryCapacity` 在创建 `AnimationBlendStackRuntime` 时已经由 prepared policy 定型，source binding 的 Pending、Request 和 source identity 三只工作集合的峰值不超过该容量。
- 将三只集合改为构造阶段按 `SourceCapacity` 预分配，删除首个运行帧因动态增长产生的托管数组分配；每帧仍只清空并复用集合，不改变 Timeline／Motion Matching source 去重、request 顺序、source preparation 或 ResetFrame。
- `ThirdPersonClient.Runtime.csproj` 当前完整编译受用户现有删除的 `Timeline.ActionCue.cs` 阻断，生成 csproj 仍引用该文件并产生 `CS2001`；本步未触碰该文件，待工作区现有删除收口后再执行完整编译确认。未新增测试、未操作共享 Unity、未做 Blend source 分配采样。

## 2026-09-21 Pose Action command 工作区复用

对应 tasks.md 的 7.33。

- `CharacterPoseNativeActionCommandSource` 原先在每次 `BeginFrame` 按 Inbox 当前数量新建命令数组；Inbox 已有固定容量和读租约，改为实例创建时建立同容量 `FixedCapacityFrameBuffer`。
- 每帧只清空、按原 Inbox 顺序填入命令，失效 frame 仍返回空数组；Commit、Discard、lease identity 和命令读取结果保持不变，删除 steady-state 命令数组分配。
- `ThirdPersonClient.Runtime.csproj` 当前完整编译仍受用户现有删除的 `Timeline.ActionCue.cs` 阻断，生成 csproj 产生 `CS2001`；本步未修改 Inbox 或 Timeline 文件。未新增测试、未操作共享 Unity、未做命令分配采样。

## 2026-09-21 ACL Pose sampling Rig 校验边界

对应 tasks.md 的 7.34。

- Presentation Domain 创建前已由 `CharacterAnimationRigBinding.RequireValid(animationRig)` 完成 Rig schema 与场景 binding 校验，随后同一正式链创建 `CharacterPoseSourceModule` 和 ACL backend。
- 删除 `CharacterAclPoseSamplingBackend` 构造器的第二次 `rig.RequireValid`；保留 `rigBinding.RequireValid(rig)` 的绑定关系、Animator 归属、PlayableGraph、source/clip/parameter capacity 和 Native 资源形状校验。
- 不改变 ACL source 注册、frame journal、deferred release 或 backend Commit/Discard；本步只删除静态 Rig 重复遍历。当前完整编译仍受工作区生成 csproj 未刷新阻断。未新增测试、未操作共享 Unity、未做 ACL 分配采样。

## 2026-09-21 删除旧 Motion Matching Provider runtime

对应 tasks.md 的 7.35。

- `CharacterMotionMatchingProviderRuntime` 在项目全部 Assets 中只有自身定义，没有构造、字段、接口或反射调用者；当前 Pose Graph 通过 `MotionMatchingPoseSourceRuntime` 和 `CharacterMotionMatchingSelectionRuntime` 承接正式数据库、选样和 source completion 链。
- 删除旧 Provider runtime 及 Unity meta，移除其重复的 Rig schema 校验、旧 frame/history 工作区和另一套 provider 生命周期；不触碰当前 Motion Matching payload、selection generation、sample resolve 或 source handoff。
- 当前完整编译仍需 Unity 重新生成删除文件后的 csproj；没有修改生成项目文件。未新增测试、未操作共享 Unity、未做 Motion Matching 分配采样。

## 2026-09-21 Motion Matching Pose 输出包装复用

对应 tasks.md 的 7.36。

- `CharacterPoseNativeMotionMatchingHandler` 原先在每次 `EvaluateOutput` 都创建新的 `CharacterPoseNativeLocalPoseValue`，但正式链在提交当前 frame 后不会继续读取上一 frame 的包装对象。
- `CharacterPoseNativeLocalPoseValue.Reuse` 只在 handler 首次产出时创建对象，后续先完成相同的 identity、Native binding、Local space 和 availability 校验，再更新同一对象；旧对象在校验失败时保持不变。
- handler 不再在 BeginFrame/ClearFrame 丢弃包装对象，以 completion identity 区分当前 frame 输出；source pose、Native 双页、ValidatePending 和 CommitFrame 的事务顺序未改变。
- 本步只收 Motion Matching Pose handler，不把其他 Pose 节点的输出包装迁移混入同一提交。当前完整编译仍受用户删除文件和 Unity 生成 csproj 未刷新阻断；未新增测试、未操作共享 Unity、未做运行时分配采样。

## 2026-09-21 Modify Bone Component Pose 输出包装复用

对应 tasks.md 的 7.37。

- `CharacterPoseNativeModifyBoneHandler` 原先在每次 Component Pose 输出完成后创建新的 `CharacterPoseNativeComponentPoseValue`，对象只服务当前 frame 的 graph output 读取。
- `CharacterPoseNativeComponentPoseValue.Reuse` 首次创建后复用同一包装对象；更新前保持 producer identity、Native binding、Component space 和 availability 校验，失败时不修改上一结果。
- handler 以 completion identity 判断当前输出，保留 Bone scratch、Native 双页、ValidatePending、Commit/Discard 和提交页索引语义；BeginFrame/ClearFrame 不再丢弃包装对象。
- 本步只收 Modify Bone，不改变其它 Component Pose 节点。当前完整编译仍受生成 csproj 的删除文件引用阻断；未新增测试、未操作共享 Unity、未做运行时分配采样。

## 2026-09-21 Space Conversion Pose 输出包装复用

对应 tasks.md 的 7.38。

- `CharacterPoseNativeSpaceConversionHandler` 的输出空间由 handler kind 固定，原先每次 `EvaluateOutput` 都创建新的 Local 或 Component Pose 包装对象。
- 按固定输出空间调用对应 `Reuse`，只在首次输出时创建包装对象，后续以当前 completion identity 识别输出；Local/Component 类型约束仍在 `ValidatePending` 保留。
- 不改变 Local/Component 转换计算、Native 双页、输入读取、ValidatePending、Commit/Discard 或提交页索引；本步只删除稳态托管包装分配。
- 当前完整编译仍受生成 csproj 的删除文件引用阻断；未新增测试、未操作共享 Unity、未做运行时分配采样。

## 2026-09-21 纯 Pose 组合节点输出包装复用

对应 tasks.md 的 7.39。

- `CharacterPoseNativeBlendPoseHandler`、`CharacterPoseNativeAdditivePoseHandler` 和 `CharacterPoseNativeLayeredBoneBlendHandler` 原先每次计算结果都会创建新的 Local Pose 包装对象。
- 三个 handler 现在复用 `CharacterPoseNativeLocalPoseValue`；以当前 completion identity 区分本 frame 输出，更新前仍检查 Local space、Pose availability 和 CompletedAt，失败时不覆盖上一结果。
- 不改变各自的 Pose 混合/叠加计算、连续性 identity、Native 双页、ValidatePending、Commit/Discard 和提交页索引；只删除稳态托管包装分配。
- 当前完整编译仍受生成 csproj 的删除文件引用及依赖 DLL 文件锁影响；未新增测试、未操作共享 Unity、未做运行时分配采样。

## 2026-09-21 Entry Pose 与 Linked Pose 输出包装复用

对应 tasks.md 的 7.40。

- `CharacterPoseNativeEntryPoseHandler` 与 `CharacterPoseNativeLinkedPoseHandler` 原先在每个 source 复制结果上创建新的 Local Pose 包装对象。
- 两个 handler 现在复用 `CharacterPoseNativeLocalPoseValue`；EvaluateOutput 只接受当前 completion identity 的缓存对象，ValidatePending 仍会在本帧没有有效输出时失败。
- 不改变 source Evaluate/Commit 顺序、输入布局检查、Native 双页复制、CompletedAt/availability 校验和提交页索引；本步只删除稳态托管包装分配。
- 当前完整编译上一轮已通过，未新增测试、未操作共享 Unity、未做运行时分配采样。

## 2026-09-21 Graph Evaluator 输入值对象复用

对应 tasks.md 的 7.49。

- Graph Evaluator 的 Program Parameter 与 Action Playback builtin handler 原先每次读取输出都会创建新的 Parameter/Action wrapper，且同一 frame 的 stage cache 会持有这些对象。
- 两个 handler 现在各自复用值对象，更新前保持 producer/completion identity、参数类型和值、Action command 有效性校验；同一 frame 的 Prepare/Evaluate 读取仍返回等价值。
- 不改变参数读取、Action channel 倒序选择、Graph output cache、节点 handler 生命周期和 frame identity；本步只删除正式运行链的值对象分配。
- 当前完整编译上一轮已通过，未新增测试、未操作共享 Unity、未做运行时分配采样。

## 2026-09-21 State Machine source 输出包装复用

对应 tasks.md 的 7.48。

- `CharacterPoseNativeStateMachineSource` 原先每次合成 active child state 的 Native 输出后创建新的 Local Pose wrapper，并在 Commit/Discard/Reset 清空引用。
- source 现在复用 `CharacterPoseNativeLocalPoseValue`，更新前仍通过 Native Local binding 校验；对象生命周期不再制造稳态逐帧分配。
- 不改变 active state 收集、子图 Begin/Prepare/Evaluate/Commit/Discard、transition 状态迁移、Native 双页或 completion identity；本步只删除结果包装分配。
- 当前完整编译上一轮已通过，未新增测试、未操作共享 Unity、未做运行时分配采样。

## 2026-09-21 约束节点输出包装复用

对应 tasks.md 的 7.47。

- Foot Placement 与 Pose Bone IK 原先每帧创建 Goal Contribution wrapper；Goal Assembler 创建 Goal Set wrapper；Full Body IK 创建 Component Pose wrapper。
- 四类 handler 现在分别复用 `CharacterPoseNativeGoalContributionValue`、`CharacterPoseNativeFullBodyIkGoalsValue` 和 `CharacterPoseNativeComponentPoseValue`，读取时按当前 completion identity 区分本帧输出。
- 更新前保留 header/goal 数量、Goal Set、Component binding、availability、CompletedAt、约束结果和提交页索引校验；不改变 service 调用、Native 双页、Commit/Discard 或事务边界。
- 当前完整编译上一轮已通过，未新增测试、未操作共享 Unity、未做运行时分配采样。

## 2026-09-21 Animation Slot 输出包装复用

对应 tasks.md 的 7.46。

- Animation Slot handler 原先在 WriteNoPose、CopyPose 和 BlendPoses 三条分支分别创建 Local Pose wrapper；其正式 source 的 `Evaluate` 还会再次创建一个 wrapper。
- handler 与 `CharacterPoseNativeAnimationSlotSourceBinding` 各自复用 `CharacterPoseNativeLocalPoseValue`，handler 以 completion identity 区分当前输出，source 以当前 binding 校验后更新对象。
- 不改变 slot source/action 选择、连续性 identity、三种输出分支、Native 双页、source Commit/Discard 或 frame 事务边界；本步只删除稳态托管包装分配。
- 当前完整编译上一轮已通过，未新增测试、未操作共享 Unity、未做运行时分配采样。

## 2026-09-21 Selected Pose Player 输出包装复用

对应 tasks.md 的 7.45。

- `CharacterPoseNativeSelectedPosePlayerHandler` 原先每次选样 job 完成后创建 Local Pose 和 discontinuity 两个 wrapper。
- handler 现在复用两类输出对象，并用当前 completion identity 阻止上一 frame 的 wrapper 被读取；更新前保持 Native binding、Pose availability 和 CompletedAt 校验。
- 不改变 Motion Matching sample resolve、Playable job、source binding Prepare/Reset、Player Complete/Commit/Discard、Native 双页或提交页索引；本步只删除稳态托管包装分配。
- 当前完整编译上一轮已通过，未新增测试、未操作共享 Unity、未做运行时分配采样。

## 2026-09-21 Inertialization 与 History Collector 输出包装复用

对应 tasks.md 的 7.44。

- Inertialization 原先每次 Native Local 输出完成后创建 wrapper；History Collector 原先同时每帧创建 Local Pose wrapper 和 History wrapper。
- 两个节点现在复用 `CharacterPoseNativeLocalPoseValue`，History Collector 额外复用 `CharacterPoseNativeHistoryValue`；所有更新先通过 identity、Native binding 和 history view 校验。
- 不改变 inertialization 的 pending/committed 状态与数组交换、History source Begin/Prepare/Commit/Discard、Native 双页和提交页索引；只删除稳态托管包装分配。
- 当前完整编译上一轮已通过，未新增测试、未操作共享 Unity、未做运行时分配采样。

## 2026-09-21 Parameter Resolve、State Machine、Root Orientation Warp 输出包装复用

对应 tasks.md 的 7.43。

- 三个节点原先在各自 Native 双页结果完成后创建新的 Local Pose wrapper，并在 frame 清理时丢弃引用。
- 三个 handler 现在复用 `CharacterPoseNativeLocalPoseValue`，按当前 completion identity 读取，更新前保持原有 Local binding、availability 和 CompletedAt 校验。
- 不改变 Parameter Resolve 的参数合成、State Machine 的 source commit、Root Orientation Warp 的 pending/committed state 或任何提交页索引；本步只删除稳态托管包装分配。
- 当前完整编译上一轮已通过，未新增测试、未操作共享 Unity、未做运行时分配采样。

## 2026-09-21 Blend Stack 输出包装复用

对应 tasks.md 的 7.42。

- `CharacterPoseNativeBlendStackHandler` 原先在每次 Blend Stack job 完成后创建新的 Local Pose wrapper，并在每个 frame 清空引用。
- handler 现在复用 `CharacterPoseNativeLocalPoseValue`，EvaluateOutput 只返回匹配当前 completion identity 的对象；更新前保留 Local binding 和可用性校验。
- 不改变 stack CompleteFrame/CommitFrame、source binding ResetFrame、Native 双页、ValidatePending 或提交页索引；本步只删除稳态托管包装分配。
- 当前完整编译上一轮已通过，未新增测试、未操作共享 Unity、未做运行时分配采样。

## 2026-09-21 Clip Player 与 Blend Space Player 输出包装复用

对应 tasks.md 的 7.41。

- Clip Player 与 Blend Space Player 原先在每次 job 完成时分别创建 Local Pose 和 discontinuity 两个托管 wrapper，并在每个 frame 清空引用。
- 两个 handler 现在复用 `CharacterPoseNativeLocalPoseValue` 与 `CharacterPoseNativeDiscontinuityValue`；EvaluateOutput 只返回匹配当前 completion identity 的对象，旧 frame 对象不会被当作当前结果。
- 不改变 player Complete/Commit/Discard、Playable job、Native 双页、Pose availability、CompletedAt 和 source binding reset 顺序；discontinuity 数据仍来自同一份 Native binding。
- 当前完整编译上一轮已通过，未新增测试、未操作共享 Unity、未做运行时分配采样。


## 2026-09-21 SourceCatalog 死接口清理

对应 tasks.md 的 7.29。

- SourceCatalog 的 `Plans` 与 `Resources` 属性只把内部 Dictionary.Values 转成新数组，当前正式链路没有调用者；Source/Foot 创建已改为按 index 读取。
- 删除两个死接口，保留 `RequirePlan`、`RequireDescriptor` 作为唯一索引访问入口，不改变资源注册、source readiness、lease 或采样顺序。
- `ThirdPersonClient.Runtime.csproj` 使用 `dotnet build --no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译成功，保留项目既有警告；随后 `dotnet build-server shutdown` 成功。未新增测试、未操作共享 Unity、未做 Pose 资源分配采样。

## 2026-09-21 Pose 运行链枚举反射清理

对应 tasks.md 的 7.23。

- `CharacterPoseNativeAnimationSlotHandler` 的 availability 在 authoring node definition、输入契约和 source binding 中已定型，handler 初始化还会比较 graph node 与 source 的同值关系；删除构造器的 `Enum.IsDefined`，保留 identity、source availability 和 graph node 一致性校验。
- handler registry 的 kind 来自正式注册表常量，evaluator 的 kind 来自正式 handler 实现，并在初始化阶段再次比较 graph node kind；删除两处 `CharacterPoseNodeKind` 反射检查，保留 sealed、builtin、creator、重复 NodeId、handler identity 和 dispose 语义。
- Modify Bone 的 reference space 已由 authoring node definition 校验，运行 handler 仍保留节点类型、BoneId、操作掩码和 Rig 骨骼索引检查；删除 `Enum.IsDefined`，不改变 Local/Mesh 的求解分支。
- 这些修改只移除静态枚举反射/装箱，不改变节点创建顺序、source 准备、handler 生命周期和每帧事务。`ThirdPersonClient.Runtime.csproj` 使用 `dotnet build --no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译成功，保留项目既有警告；随后 `dotnet build-server shutdown` 成功。未新增测试、未操作共享 Unity、未做 Pose 实例分配采样。

## 2026-09-21 Pose SourceCatalog 单次准备

对应 tasks.md 的 7.24。

- `CharacterPoseNativeDomainServiceFactory` 原先在 `Create`、每个 Clip Player 工厂和 Foot Motion 解析中重复创建 `CharacterPoseNativeSourceResourceCatalog`；每次都会重建 source/resource 字典、重复执行 plan/descriptor/Rig 校验，并重复向 resource scope 注册 ACL descriptor。
- 将 SourceCatalog 固定为 ServiceFactory 创建阶段的唯一准备结果；Clip Player 直接按 source index 读取，Foot Motion 直接按 `PresentationPoseSourceIndex` 读取，删除 `FirstOrDefault` 和 `Plans.ToArray` 路径。SourceCatalog 的索引、Rig identity、ACL binding 与 descriptor 校验仍执行一次。
- 没有改变 source module 创建、resource lease、handler factory、Clip/Foot 运行顺序，也没有改变资源 scope 的唯一注册结果；运行期只保留按索引查找，不再创建中间集合。
- `ThirdPersonClient.Runtime.csproj` 使用 `dotnet build --no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译成功，保留项目既有警告；随后 `dotnet build-server shutdown` 成功。未新增测试、未操作共享 Unity、未做 Pose 实例分配采样。

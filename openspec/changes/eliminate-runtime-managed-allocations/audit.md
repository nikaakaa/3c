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

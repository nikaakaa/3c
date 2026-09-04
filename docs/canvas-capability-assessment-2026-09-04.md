# NodeCanvas／FlowCanvas 与当前 3C 的能力对账

检查日期：2026-09-04。检查对象为本地导入的 NodeCanvas、FlowCanvas、CanvasCore 源码，CanvasCore 的 FRAMEWORK_VERSION 标记为 3.42；这是源码标记，不擅自等同于两个产品的完整发行版本。检查时 HEAD 为 b00167792，工作区另有未提交内容，因此本报告针对当时实际工作区。

本报告是源码、项目配置与官方文档的研究记录，不是已经批准的新架构。本轮没有改插件或业务代码，没有运行 Unity／Player、构建、性能测试或新增测试代码。原有 BTSMTL 重构与预览 change 未被修改。

**结论：Canvas 能承接普通行为编排，也能通过自定义任务接入现有业务；它不能直接等价替换当前完整角色执行、Timeline、Pose、状态恢复和 Agent Document 链路。原生 DOTS、整图 Burst、图到 C#／Program 的编译、紧凑二进制执行格式以及完整执行状态热迁移，在导入版中均未发现。图数据重新加载是现有能力，代码热更依赖项目自己的热更系统。**

**1．先区分三个“能实现”。**

- 直接配置：用已提供节点即可完成普通条件、变量、事件、行为树、状态机、子图和动作列表。
- 业务接入：实现明确的 ActionTask／ConditionTask 或 FlowNode，调用项目正式输入、请求和观察入口。Canvas 组织调用，业务模块继续拥有规则和结果。
- 新增核心系统：编译成现有 .csim、完整快照恢复、固定数值目标、原生 DOTS、完整动作 Timeline 等，需要新增或迁移实质实现。不能把它们描述成“配几个节点”。

| 当前能力 | Canvas 的对应能力 | 对当前项目的结论 |
|---|---|---|
| Sequence、Selector、条件装饰、循环、并行、子树 | NodeCanvas 已有相应行为树模块 | 普通结构可直接重建；动态重评估、顺序、停止规则仍需逐项对齐，资产不能原格式直接导入 |
| Gameplay FSM、嵌套状态、条件迁移 | FSM、Action State、子 FSM／子 BT | 能表达状态行为；当前异步退出屏障与 Action 生命周期不能默认等价 |
| 事件、执行线、数据线、函数、变量、复用子图 | FlowCanvas 的事件、Flow／Value 端口、方法节点、Macro | 可直接使用其语言能力；当前全部 Gameplay 类型不是已提供的业务节点 |
| Blackboard | 图／组件黑板、BBParameter 字面量或变量绑定 | 普通变量可直接使用；Frame／State activation／ActionInstance 作用域和来源身份不会自动继承 |
| AI 感知、记忆、目标、Intent | 条件与动作任务可承接业务 API | 需要读取正式观察并输出正式输入；现有 AI CaptureState／RestoreState 仍有缺口 |
| Action 准入、Tag、GE、Equipment | 自定义任务或方法调用 | 能组织业务调用；不能用插件任务状态代替正式 Action／GE／Equipment 状态 |
| Hit／Cancel／Combo 时间窗口 | 等待、条件、变量、动作列表可表达近似流程 | 没有当前同 Tick Decision／Commit、Frame 事实及 Action Context 的直接等价节点 |
| 动画片段、Motion Curve／Warp、Cue、相机 Timeline | ActionList 有顺序／并行和启动延迟；FlowCanvas 可调用方法 | 不是现有 Timeline 的替换成品；连续采样、曲线、循环跨越、打断和资源引用仍需领域实现 |
| Pose Graph、BlendStack、Inertialization、Foot IK、FBBIK | 普通方法／任务可以调用外部能力 | 不提供当前 Pose IR、workspace、Native 求值计划和单一骨骼 Writer；不能用 Animator 内置任务替代 |
| 显式 Character Build、Numeric Target、.csir／.csim | 未找到对应编译后端 | 若保留这些产物，必须开发正式 Canvas 前端与受支持节点集合 |
| 无 Unity 运行时的普通 .NET Host | Graph／Owner／任务直接引用 Unity 类型和生命周期 | 导入版不是当前 portable Program 的宿主替代品；Unity Server 宿主也应与普通 .NET 产品区分 |
| 固定输入回放 | 可通过控制宿主、时间及业务输入进行接入 | 没有现成的项目 Replay／Proof；手动 Tick 不代表已具备确定性或完整回滚 |
| Agent Document v4、完整事务、引用与 hash | 有图序列化和编辑 API | JSON 导入导出不等于现有跨 Graph／Timeline／Profile 的 Document 对账与资产事务 |

NodeCanvas 与 FlowCanvas 共用 CanvasCore，但本地未找到两者的专用互嵌 Bridge。官方将其作为单独下载项，页面记录更新日期为 2026-02-17；本轮没有下载或安装。参见[官方下载列表](https://nodecanvas.paradoxnotion.com/downloads/)。

**2．实际执行方式决定了它能直接替换哪一层。**

Canvas 的典型链路是：Owner 取得图定义及 Unity 对象引用 → 创建图实例 → JSON 反序列化与引用绑定 → 初始化任务／端口 → NodeCanvas 执行节点，或 FlowCanvas 沿已绑定委托调用 → C# 业务。

FlowCanvas 的端口会绑定为 FlowHandler 委托，运行时调用这些委托；这不是每次执行都重新解析 JSON，也不是一律使用反射调用。反射方法节点与专用节点的执行成本不同。它同样没有把整张图生成 C# 或项目的静态 operation image。

当前项目则沿正式输入进入 Session，执行 Program 与 World，完成提交后再由表现系统消费结果。[UnityCharacterSimulationInputAdapter.BuildInput](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Unity/UnityCharacterSimulationInputAdapter.cs:132) 会构造输入值、带序号／Tick／过期时间的请求和完整输入身份。因此“插件能调用任意 C# 方法”不等于现有内核已经提供随时可调用的 StartAttack 或 SetPosition；接入需要遵守正式请求与观察边界。

一个合理的迁移示例是：NodeCanvas 决定需要接近或攻击 → 项目任务提交正式移动／攻击输入 → 当前角色业务处理准入、时间与输出 → Canvas 读取已提交结果决定后续行为。这个例子只能证明高层接入可设计，不代表插件图状态已经符合项目的预测／回滚合同。

内置 Seek 任务实际操作 NavMeshAgent.SetDestination；内置 Animator Trigger 任务直接调用 Animator.SetTrigger。它们不能直接替换当前 KCC／World Solve 与 Animancer／Pose Plan 链。依据：[MoveToPosition](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Movement/Pathfinding/MoveToPosition.cs:12)、[MecanimSetTrigger](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/NodeCanvas/Tasks/Actions/Animator/MecanimSetTrigger.cs:12)。

**3．DOTS、Jobs 和 Burst。**

导入源码及 asmdef 搜索未发现 Unity.Entities、Unity.Jobs、Unity.Burst、IJob、IJobEntity、NativeArray、BlobAsset、EntityManager 或 BurstCompile 接入。Graph 继承 ScriptableObject，Owner／Blackboard 是 MonoBehaviour，节点／任务与端口是托管对象，运行依赖列表、字典、委托和 Unity 对象。项目当前装有 Burst／Collections，但依赖清单没有 Unity.Entities。

结论分三层：

- 与 DOTS 项目共存：可以设计，例如在主线程由任务提交 ECS 请求或启动已有 Job。
- 让耗时业务在 Burst／Job 中计算：可以由项目自己实现，再把结果交回图；需要明确数据交接和依赖完成点。
- 整张 NodeCanvas／FlowCanvas 图编译为原生 ECS／Burst 执行计划：导入版没有提供。只加 BurstCompile 或更换更新循环不能完成这个转换。

Graph.LoadOverwriteAsync 使用 Task.Run 做加载／初始化，并把部分 Unity 操作延后到主线程。这不等于图在 Job 中并行执行；共享 JSONSerializer 还使用全局 serializerLock，不能据异步 API 承诺多图解析线性加速。

依据：[Graph 类型与加载](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Framework/Runtime/Graphs/Graph.cs:18)、[异步加载](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Framework/Runtime/Graphs/Graph.cs:485)、[共享 JSONSerializer](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Common/Runtime/Serialization/JSONSerializer.cs:89)。Unity 官方说明 Burst 不支持一般托管对象与引用类型，见[Burst 类型支持](https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/csharp-type-support.html)。

对当前少量角色的演示，缺少原生 DOTS 不能单独证明插件太慢；本轮没有测量 Player 的帧时间、分配或加载峰值。

现有普通 .NET Host 不能直接按原 portable Program 的方式装配这些 ScriptableObject／MonoBehaviour 图。若选择 Unity Server 来运行插件，需要另外核对该宿主的产品与构建合同，不能把它描述为原 .NET 产品无需迁移。

**4．“编译压缩”需要拆成不同能力。**

| 所指能力 | 导入版情况 |
|---|---|
| 插件 C# 经过 Unity／IL2CPP 编译 | 支持，仍需正确 AOT／裁剪配置 |
| 自动把已有方法显示为节点 | 支持，不等于整图代码生成 |
| 节点连接预绑定／反射调用优化 | 存在，FlowOutput 直接调用绑定委托 |
| 整图生成 C#／程序集 | FlowCanvas 官方明确不生成 C#；NodeCanvas 导入源码中未找到相应生成后端 |
| 生成类似当前 .csim／Pose Program Image 的静态运行计划 | 未提供 |
| DOTS／Burst 编译后端 | 未提供 |
| AOTClasses.cs 与 link.xml 生成 | 有，目的是泛型实例可用和防止代码裁剪 |
| 图定义序列化 | JSON＋Unity 对象引用表 |
| 紧凑二进制图格式／专用压缩器 | 导入源码未找到 |
| 去掉部分编辑字段 | 存在 fsIgnoreInBuild；它是字段序列化策略，不是图编译器 |
| AssetBundle／资源包压缩 | 可交给项目资源管线；不改变解压后的对象图执行成本 |
| ACL 等动画数据压缩 | 不属于这两个包提供的能力 |

AOTClassesGenerator 生成的主要是未直接使用的泛型类型／方法声明，供 AOT 编译器保留实例；不是沿图控制流生成程序。GenerateLinkXML 默认保留 Assembly-CSharp 和 ParadoxNotion 相关公共程序集全部内容，再列出其他所需类型。它首先解决运行可用性，不能当作包体压缩功能。依据：[AOT 生成器](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Common/Design/PartialEditor/AOTClassesGenerator.cs:53)、[link.xml 生成](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Common/Design/PartialEditor/AOTClassesGenerator.cs:200)，以及[官方 AOT 说明](https://flowcanvas.paradoxnotion.com/documentation/?section=working-with-aot-platforms)。

fsIgnoreInBuild 的判断位于非 UNITY_EDITOR 分支，Graph 同时保存已有的 JSON 字符串。因此本轮只能确认字段过滤机制存在，没有验证最终 Player／资源包是否已删除全部编辑元数据，也没有任何压缩比例数据。

依据：[图序列化](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Framework/Runtime/Graphs/Graph.cs:128)、[字段过滤](<D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Common/Runtime/Serialization/Full Serializer/fsMetaType.cs:78>)、[Flow 端口调用](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/Ports.cs:217)。FlowCanvas 的不生成 C# 声明见[官方性能说明](https://flowcanvas.paradoxnotion.com/documentation/?section=performance-considerations)。

**5．热更新：图数据、代码和执行中状态是三件事。**

| 热更对象 | 能力判断 | 项目还需要做什么 |
|---|---|---|
| 修改现有节点参数、分支和连接 | 可以用新图数据实现 | 资源版本、依赖引用、验证以及明确的新实例切换边界 |
| 发布新的图资产 | 可由 AssetBundle／现有资源系统提供 | 正式资源收集和依赖闭包；Canvas 本身不是下载／更新框架 |
| 发布包含既有节点类型的新 JSON | Graph.Deserialize 提供基础能力 | 同时提供正确 Unity 对象引用表；裸 JSON 不会自动下载和定位资源 |
| 新增自定义 Task／FlowNode 的 C# 实现 | Canvas 自身不加载热更 DLL；可研究接入 HybridCLR | 热更程序集、AOT 依赖／泛型、类型发现、反射缓存与图加载顺序 |
| 修改已打入 AOT 包的 Canvas 基础类 | 当前项目配置不会把三个 Canvas 程序集作为热更 DLL 发布 | 如确有此需求，需重新设计程序集分工和采用的热更能力 |
| 正在等待／攻击时换图，并从原执行位置继续 | 未提供通用迁移合同 | 明确旧任务结束、旧资源释放、新旧节点状态映射和版本身份 |
| Editor Play Mode 中实时修改图／变量 | 有编辑与运行观察能力 | 不能把 Editor 域重载、反射刷新和资源引用环境视为 Player 热更验证 |

已有节点与代码均在 Player 中时，修改“等待 0.3 秒为 0.5 秒”或改变分支，可以通过更新图数据交给后续运行实例；这不必生成新的图 C# 程序。若新增节点需要新的 C# 类型，JSON 本身不能把实现带进 Player。

Graph.Deserialize 会改写当前图数据，但它没有自动迁移正在运行的任务，也不会单独完成 FlowScript 的全部端口、函数与子图初始化。不能将“API 允许调用”解释为运行中直接覆盖的安全保证。可设计明确停止／释放后创建新实例，或在自然业务边界切换；若要求不断流地保留执行位置，这是额外核心需求。

当前 HybridCLRSettings 已启用，但 hotUpdateAssemblies 只列 GameBase、GameProto、BattleCore、GameLogic；ParadoxNotion、NodeCanvas、FlowCanvas 不在其中。当前 GameLogic asmdef 也未声明这三个程序集的正式引用。本轮未发现 Canvas 与 HybridCLR 的专用集成代码。依据：[HybridCLR 配置](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/ProjectSettings/HybridCLRSettings.asset:20)、[GameLogic 程序集](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/HotFix/GameLogic/GameLogic.asmdef:1)。

一种有价值的程序集分工是 Canvas 基础设施留在 AOT，业务 Task／Node 位于明确热更程序集；另一种是将部分插件代码本身划入热更程序集。前者减少插件版本变化范围，后者允许改插件实现但扩大热更依赖和兼容成本。两者都不是当前已安装事实。HybridCLR 官方允许合理划分第三方程序集，并要求正确的引用方向；泛型还涉及 AOT 实例或补充元数据。参见[程序集配置](https://www.hybridclr.cn/en/docs/basic/hotupdateassemblysetting)、[AOT 泛型](https://www.hybridclr.cn/en/docs/basic/aotgeneric)。

还有一个具体接入问题：ReflectionTools 缓存已加载程序集、全部类型及名字解析结果，失败的类型查找也可能缓存 null；主要自动刷新点是 Editor 脚本重载。若 Player 在首次扫描之后才加载业务 DLL，应在正式装配入口处理类型／序列化缓存刷新，或者保证 DLL 先加载再发现图类型。SerializedTypeInfo 还保存已解析的 Type。源码中有 FlushMem，但没有发现 AssemblyLoad 自动联动。这是需要验证的接入条件，不是本轮已复现的运行缺陷。依据：[反射缓存](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Common/Runtime/ReflectionTools.cs:46)、[类型引用缓存](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Common/Runtime/Serialization/SerializedTypeInfo.cs:11)、[序列化缓存](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Common/Runtime/Serialization/JSONSerializer.cs:25)。

启动时加载新版本 DLL，与不中断进程地卸载／替换同名程序集及其存活对象，也应分别设计。不能将 HybridCLR 一般代码热更能力直接视为任意在途图状态热迁移。[HybridCLR FAQ](https://www.hybridclr.cn/en/docs/help/faq) 说明其反射与动态程序集能力；NodeCanvas 的图状态迁移仍需独立处理。

**6．回放、停止与状态语义是当前最大的等价迁移缺口。**

输入回放是从明确初态重放输入；网络回滚需要恢复某个 Tick 的完整状态再重新执行。黑板 Save／Load 只覆盖部分变量，不能替代后者。

本地源码中的例子：

- Node._status 明确标记 NonSerialized；Node.timeStarted 也是运行期信息。
- ActionTask.status／timeStarted 保存在任务内部。
- Sequencer.lastRunningNodeIndex 是私有运行游标。
- FSM.currentState、previousState、stateStack 保存在实例中。
- Flow 还可能持有返回回调、临时参数与等待流程。

这些不会因为 Graph.Serialize 或 Blackboard.Serialize 就自动形成完整可恢复快照。当前 AICharacterControlSource 已明确提供 CaptureState／RestoreState，改成插件图后必须继续满足实际调用方需要，或明确变更该合同。

依据：[节点运行状态](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Framework/Runtime/Graphs/Node.cs:49)、[任务运行状态](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Framework/Runtime/Tasks/ActionTask.cs:33)、[Sequence 游标](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/Nodes/Composites/Sequencer.cs:22)、[AI 状态恢复入口](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/AI/AICharacterControlSource.cs:131)。厂商历史答复也明确区分黑板保存与任务内部状态保存；该答复较早，本报告结论以当前导入源码再次核对为依据，见[运行时保存／加载说明](https://paradoxnotion.com/forums/topic/how-to-save-load-graph-in-game-at-run-time/)。

时间也不能只改最外层：Graph.UpdateGraph(float) 能接收显式 dt，但 BehaviourTree 在启用非零 updateInterval 时仍累加 Time.deltaTime。普通 Wait 使用 ownerSystem.elapsedTime，部分内置任务还直接使用 Unity 时间或随机数。固定输入复现必须核对实际采用的节点集合，不能把 Manual 模式当作全库确定性保证。依据：[Graph 显式更新](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Framework/Runtime/Graphs/Graph.cs:689)、[BT 更新间隔](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/NodeCanvas/Modules/BehaviourTrees/BehaviourTree.cs:80)。

停止边界存在更明显的差异。当前 TimelineControlRuntime 允许 Disable／Root Stop／Destroy 分阶段进行，并返回 OperationStopStatus.Running 等待后续 Tick。Canvas 的 ActionTask.EndAction 会调用同步 void OnStop；FSM.EnterState 重置旧状态后进入新状态。可以设计专用退出状态或等待领域停止完成的任务，但不能认为默认 OnStop 已等价实现原停止屏障。

依据：[Canvas 任务停止](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Framework/Runtime/Tasks/ActionTask.cs:113)、[Canvas FSM 切换](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/NodeCanvas/Modules/StateMachines/FSM.cs:143)、[现有 Timeline 停止屏障](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/Core/Execution/TimelineControlRuntime.cs:501)。

**7．性能、包体和错误处理的实际边界。**

- 导入版有异步加载、任务实例复用、端口预绑定和反射缓存；这些是优化措施，不等于全图零分配或 Burst。
- Flow.WriteParameter 使用 Dictionary<string, object>；CallFunction 接收 params object[]。在具体使用路径上可能产生对象数组、字典或装箱分配。
- 官方性能文档提醒某些值类型转换和 Macro 值端口可能装箱，并区分 JIT 与 AOT 的反射成本。文档描述不能替代当前版本和本项目的 Player 测量。
- 每个运行 Owner 有图／任务实例及局部状态；共享一个图资产不等于所有角色共享一份可变运行状态，也不等于原生批处理。
- 程序集、AOTClasses、link.xml、资源包和 JSON 解析分别影响包体、启动与运行内存，应分别测量；没有证据支持给出统一的性能改善比例。
- Graph.Validate 有捕获异常、记录日志后继续的分支；部分任务用 Failure／Optional／默认返回表达缺失。现有项目的严格构建失败、事务丢弃与 Actor Fault 合同不会自动继承，不能将插件普通失败误记为一次成功的回放。

依据：[Flow 参数](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/Flow.cs:25)、[函数调用](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/FlowScript.cs:35)、[Graph 校验](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Framework/Runtime/Graphs/Graph.cs:206)、[官方性能说明](https://flowcanvas.paradoxnotion.com/documentation/?section=performance-considerations)。

**8．对 BTSMTL 去留的影响。**

这次深入检查不足以支持“导入 Canvas 后即可删除整套 BTSMTL”。能明确确认的，是通用作者与行为编排存在替换候选；不能直接替代的，是项目自己的动作时序、编译／数值／状态合同、Pose 数据模型和完整资产对账。

| 路线 | 能获得的业务收益 | 要承担的工作 |
|---|---|---|
| Canvas 运行高层行为，领域代码拥有正式动作和表现 | 利用现成 BT／FSM／Flow 作者与诊断能力；减少通用运行器维护 | 正式请求和观察接入，明确插件图状态是否需要被预测／回滚，避免双重行为所有权 |
| Canvas 只作作者前端，仍编译到当前 Program | 保留现有数值与状态恢复边界，借用作者交互 | 明确可编译节点集合、语义转换、资产身份、source map、调试、Document；不能使用任意 Unity 方法节点 |
| 保留并收窄 BTSMTL 作者模型 | 维持现有已接通链路，并减少简单业务的过度图化 | 继续承担作者工具维护；需要用实际动作制作证明效率收益 |

这些路线的实施优先级未由本报告替用户决定。本报告也没有为任何路线新增兼容开关、双运行器、临时输入驱动或第二数据真相。

**9．尚未验证的具体项目问题。**

以下是选型证据缺口，不是新建测试任务，也没有写入 OpenSpec tasks：

- 一个真实 Corin 动作在正常、取消、打断和结束路径上是否与当前行为一致。
- 非零 AI 更新间隔、暂停／恢复、循环跨越与多 Actor 时的实际时间行为。
- IL2CPP Player 中普通节点、自定义泛型任务、反射节点和裁剪配置是否完整。
- 图资产经现有资源管线发布后，Unity 引用能否完整恢复。
- 已有节点的新图数据能否在明确业务边界切换；新热更 Task 类型能否在正确加载顺序下被发现。
- 若继续要求完整状态恢复，任务游标、等待、FSM、子图和业务状态怎样形成唯一快照。
- 实际 Player 的稳定帧分配、加载耗时、加载峰值内存与多 Actor 成本。
- 当前 Scene Preview、NodeCanvas／FlowCanvas Bridge、Document 和诊断接点的最终单一所有者。

本次只交付本报告与源码依据。后续只有在用户选择正式路线后，才应重写相应迁移提案；原有 56 项作者重构和 54 项场景预览计划不能被本报告自动视为已经修改或废弃。

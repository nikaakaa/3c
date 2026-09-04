# 动作编排、角色控制与模拟管线：现成方案调研

日期：2026-09-04。本文是调研记录和方案取舍，不是已批准的迁移设计。没有修改代码、作者资产、当前 spec 或已有 change，也没有安装、运行或购买所比较的产品。在线文档版本不代表项目导入版本。

本次问题：BTSMTL 是否适合收至动作级；现成方案是否主要采用 Timeline／Tree；角色控制、网络 Pass、状态恢复和编译各应如何评价。用户消息末尾尚有未补完的“还有就是”，本文先覆盖已经明确的范围。

**调研结论。**

动作级编排有明确的现成案例，但这些样本不足以证明“行业基本都是动作级 Timeline＋Tree”。实际出现了时间轴任务、能力任务、C# 状态机配动画事件，以及高层行为树等不同组合。Tree 的位置也不固定：可以决定接下来做什么，也可以表达动作内部的局部流程，未必需要每个 Clip 内嵌一棵树。

对本项目，更有参考价值的共同点是：行为作者、动作生命周期、模拟调度和可恢复状态可以分别设计。现有 Session／Pass 的价值不依赖于角色控制流程必须由 BTSMTL 编译生成。与此同时，角色规则改成 C# 也不意味着状态布局、序列化或数据构建的生成能力失去价值。

**1．样本实际表达的范围。**

| 样本 | 作者主要组织什么 | 树／图／时间轴的实际位置 | 对本项目的参考价值与限制 |
|---|---|---|---|
| Unreal GAS | 单项能力的激活、持续执行、取消，以及与属性、效果、动画的协调 | 能力可以用 C++ 或 Blueprint；Ability Task 承担跨帧工作 | 参考能力生命周期和任务结束时的清理。它拥有 UE 的复制／预测机制，不能当作本项目确定性回滚的直接实现。见 [GAS 概述](https://dev.epicgames.com/documentation/en-us/unreal-engine/understanding-the-unreal-engine-gameplay-ability-system)。 |
| Able | 在一条能力时间轴上安排任务 | Timeline 放动画、查询、特效等任务；还可使用条件分支和 Blueprint 扩展 | 是动作级编辑器的直接样本。所读文档主要写于 2016—2020 年，仅用于研究结构，不据此确认当前引擎兼容性或购买适用性。见 [Ability](https://extralifestudios.atlassian.net/wiki/spaces/ABLE/pages/2195477/Ability)。 |
| Animancer | C# 状态机组织角色行为，动画内容通过 Transition 和事件配置 | 行为状态机与动画系统分开，不要求再有一棵动作树 | 可参考输入、准入和动作执行的分工。其示例直接使用 Unity 对象，不等于已经接入本项目的 Tick、快照和 World Solver。见 [FSM Overview](https://kybernetik.com.au/animancer/docs/manual/fsm/overview)、[Brains](https://kybernetik.com.au/animancer/docs/samples/fsm/brains/)。 |
| NodeCanvas | BT／FSM 组织行为，叶子调用动作和条件 | 行为树包含选择、顺序、循环、打断等控制流程，范围可以高于单个动作 | 保留角色／AI 级图有现成依据；它也不是动作 Timeline 的直接替代品。见 [Behaviour Trees](https://nodecanvas.paradoxnotion.com/documentation/?section=behaviour-trees)。项目导入版的状态和预览限制见下文已有报告链接。 |
| BehaviorTree.CPP／Groot | 图形化组织 C++ 节点执行的任务 | XML 描述组合关系；叶子承担具体动作，可持续运行并响应停止 | 与“独立 Qt 作者工具＋领域执行能力”的起点相似，但不能据此识别用户曾见过的中台实现。它不是 Unity 角色控制器。见 [BT.CPP](https://behaviortree.dev/docs/intro/)、[异步动作](https://behaviortree.dev/docs/guides/asynchronous_nodes/)。 |
| Photon Quantum 3 | C# System 执行玩法，可变数据进入可回滚 Frame | 核心模拟不要求角色图；Qtn 描述状态并生成代码 | 参考执行与状态分离，以及按顺序运行系统。它是完整外部模拟产品，本次不建议替换现有 Session，也不把 Quantum System 等同于本项目 Pass。见 [Systems](https://doc.photonengine.com/quantum/v3/manual/quantum-ecs/systems)。 |

这是一组有代表性的公开架构样本，不是市场占比调查。官方产品文档里的性能、低带宽、易用性宣传，没有被当作本项目的实测结果。

**2．动作级不等于只有动画播放，也不要求 Timeline＋Tree 成套出现。**

三个具体需求应当分别表达：

| 需求 | 合适的表达 | 业务例子 |
|---|---|---|
| 在动作进度的指定区间发生事情 | 时间轴、区间、曲线 | 某段时间允许连段；某段时间采用位移曲线 |
| 根据输入和事件改变当前动作内部流程 | 任务、局部状态机、受限流程图或树 | 按住蓄力，松手释放，被打断时退出并清理 |
| 根据整体状态决定下一步行为 | C# 控制流程或角色／AI 级 BT／FSM | 接近目标、选择攻击、闪避、切换控制模式 |

条件表达式负责得到布尔值或数值；持续任务还需要保存进度并处理退出。两者不能因为都能画成节点，就假定具有相同的运行和恢复语义。

Able 的编辑器把单次能力拆为有起止时间的任务，保存时会按开始时间排序；它提供播放、暂停、单步和预览对象重建。这说明专用时间编辑和数据整理本身就有作者价值。其预览文档同时说明停止能力后某些动画或粒子仍可能继续，因此不能把该预览能力直接等同于完整模拟状态恢复。见 [Ability Editor](https://extralifestudios.atlassian.net/wiki/spaces/ABLE/pages/2195482/Ability%2BEditor)。

Animancer 的事件机制允许在动画经过某个时间点时调用代码，是另一种比完整动作图更小的表达。见 [Animancer Events](https://kybernetik.com.au/animancer/docs/manual/events/animancer/)。对于本项目的正式 Gameplay，采样依据仍应来自统一模拟时间；动画显示帧的回调不能另行成为第二个 Gameplay 推进入口。这是根据本项目网络管线做出的接入判断。

**3．收到动作级，也存在“连段关系放在哪里”的真实选择。**

Able 的 Branch Task 可以在一个时间区间持续检查条件，然后进入另一项能力。它使用输入条件或自定义判断，说明现成动作编辑器也可能包含跨动作组合关系。见 [Branch](https://extralifestudios.atlassian.net/wiki/spaces/ABLE/pages/3768375/Branch)。这并不要求本项目采用相同边界。

| 同级选择 | 作者能做什么 | 业务收益 | 代价 |
|---|---|---|---|
| 角色代码决定后续动作 | 动作资产只提供阶段、窗口和内容；C# 决定 Attack1 接哪个动作 | 程序员能直接沿代码核对选择与中断 | 调整连段关系需要修改代码或角色级配置 |
| 动作数据声明后续候选 | 动作资产可配置后续动作引用、适用区间和受限条件；统一控制模块处理实际启动和退出 | 作者能直接调整动作组合 | 动作数据需要引用校验、选择规则和相应诊断；工具范围比纯时间轴大 |

无论选择哪种，都应使用一个正式的动作启动／停止入口。动作 Clip 直接更换控制器状态、绕过 Session，不能作为迁移方案。第二种选择也不必扩展为任意角色状态关系或通用 Blueprint。

**4．“支持网络”必须对照具体机制。**

GAS 的 Prediction Key 把预测行为及其副作用关联起来，处理确认、拒绝以及重复效果；文档也列出预测范围限制。这里能借鉴的是生命周期与副作用管理，不能从“支持客户端预测”推导出“任意 C++／Blueprint 任务都能按本项目完整世界快照恢复”。见 [FPredictionKey](https://dev.epicgames.com/documentation/unreal-engine/API/Plugins/GameplayAbilities/FPredictionKey)。

Quantum 的系统以 C# 类实现，可变模拟数据放入 Frame；系统执行顺序也有明确配置。它说明代码作者与严格回滚状态能够并存。状态写在普通对象的私有字段里，并不会仅因使用 C# 就自动可恢复。该原则可供本项目参考，但本项目允许 Pass 作为显式状态参与者，不能照抄成“所有 Pass 必须没有状态”。见 [Systems](https://doc.photonengine.com/quantum/v3/manual/quantum-ecs/systems)。

本项目当前事实：

- [PipelineTransactionCoordinator](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/Core/Pipeline/PipelineTransactionCoordinator.cs:96) 统一执行输入收集、调度、可选恢复、逐 Tick 模拟和输出提交。
- [RollbackPipelineRuntimePackage](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/DeterministicRollback/Pipeline/RollbackPipelineRuntimePackage.cs:40) 明确装配网络 Pass 与 Program Evaluate、World Resolve、Program Finalize。
- [NetworkCheckpointLayout](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/Core/Float32/Network/ServerAuthoritative/ServerAuthoritativeNetworkCheckpoint.cs:14) 当前按 Program 的状态槽位建立检查点布局，绑定 ProgramHash 和 LayoutHash。

因此，保留管线有具体实现基础；更换角色执行方式仍需要迁移状态和接入合同。不能把一个普通 MonoBehaviour／第三方图实例直接塞入 Pass，就宣称完成相同的回滚保证。

**5．编译需要按产物拆开评估。**

Quantum 的 Qtn 解析状态定义并生成 C# 结构，处理内存布局、序列化和校验等工作；它并不要求先把全部角色控制逻辑画成图。见 [DSL](https://doc.photonengine.com/quantum/v3/manual/quantum-ecs/dsl)。

这个样本给前面讨论补充了一个具体区别：控制流编译与状态代码生成可以分开。它既不能证明必须保留现有完整 Gameplay 编译器，也不能证明需要在本项目新造一门 Qtn。

| 能力 | 收到动作级后的判断 | 保留所换来的收益／成本 |
|---|---|---|
| 角色图控制流编译 | 如果控制流程迁入 C#，对应旧路径应删除 | 减少角色图语义和映射维护；失去角色流程资产化 |
| 动作时间／曲线／引用整理 | 独立于是否采用树，可继续构建 | 减少运行时查找并提前报错；要维护数据版本和构建入口 |
| 动作局部流程编译 | 由实际作者复杂度决定 | 灵活组合局部任务；付出编译和调试映射成本 |
| 状态布局、编码、恢复辅助 | 独立评估，不能因取消角色图一起丢弃 | 统一状态约束；显式 C# 数据或生成数据都要付出维护成本 |
| Pipeline 编译 | 当前与 BTSMTL 角色图编译属于不同职责 | 检查 Pass 输入输出、绑定和状态归属；继续服务统一 Session |
| Pose／动画执行计划 | 属于表现执行链，另行评价 | 不能由 BTSMTL 收缩范围直接推导去留 |

当前 [SimulationPipelineCompiler](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/Core/Pipeline/SimulationPipelineCompiler.cs:83) 已执行数据生产／消费和状态所有权检查。[TimelineControlRuntime](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/Core/Execution/TimelineControlRuntime.cs:90) 则执行动作时间推进与停止过程。两个职责不应再用一句“编译没有作用／全部要保留”一起决定。

Able 概述包含“能力被编译成代码”的产品表述，但本次没有获得其编译后端源码，不能据此声称生成原生机器码，或拿它证明本项目通用指令体系的必要性。见 [Overview](https://extralifestudios.atlassian.net/wiki/spaces/ABLE/pages/2195469/Overview)。

**6．项目有价值的两个同级范围选择。**

| 选择 | 输入／输出与作者边界 | 主要收益 | 主要代价 |
|---|---|---|---|
| BTSMTL 保持角色级 | 角色输入与状态进入图所定义的规则，输出动作请求及模拟结果；作者编辑角色关系和动作内容 | 复用已有作者资产和状态体系；可直接配置角色流程差异 | 继续维护角色图、编译、Document 和运行来源映射 |
| BTSMTL 收到动作级 | C# 控制模块选择动作；动作数据接受进度／输入／上下文，输出阶段事实、求解请求和表现命令 | 作者工具集中服务动作调节；程序员直接维护角色控制流程 | 迁移现有角色级执行与状态；角色流程修改更多依赖代码 |

两者都保留统一的 Session／Pass、世界求解、状态恢复和对外输出入口。两者都能够设计热更，热更不构成强迫选择某个作者范围的理由。

若选择动作级，内部另有两个同级实现选择：结构化时间／任务数据配固定执行器，或者动作局部图编译为执行计划。前者适合机制主要由程序员扩展、作者主要调内容的情况；后者适合作者需要频繁组合等待、分支、循环和退出流程的情况。复杂动作示例应决定这项取舍，不能仅由是否已经存在一个编译器决定。

**7．热更、预览和现成工具的接入边界。**

已有执行能力内调整动作参数和组合，可以更新数据；新增执行能力需要更新代码。HybridCLR 允许划分热更程序集，但要求正确安排引用关系，AOT 程序集不能直接引用热更程序集。见 [程序集配置](https://www.hybridclr.cn/docs/basic/hotupdateassemblysetting)。

当前 [HybridCLRSettings.asset](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/ProjectSettings/HybridCLRSettings.asset:20) 仅列出 GameBase、GameProto、BattleCore、GameLogic。没有据此验证模拟执行核心的新逻辑可通过现有发布链热更。按局锁定模拟版本、下一局采用新版本是一种明确的业务选择；对局中替换存活状态需要另外定义迁移语义。运行数据、代码可下载，并不自动解决后者。

Quantum 文档区分“切换状态中的资产引用”和“直接修改资产对象内部数据”：后者不在其回滚状态中。这可帮助理解为何局内任意改动作数据会影响重算，但不代表本项目已经实现 Quantum 的资产管理方式。见 [Assets in Simulation](https://doc.photonengine.com/quantum/v3/manual/assets/assets-simulation)。

完整角色预览仍应通过本项目正式输入、Session、世界求解和表现链执行；编辑游标、动作预览和完整世界恢复是不同能力。NodeCanvas／FlowCanvas 已导入，但其普通运行图不是当前 Program 快照的直接替代。详细源码结论见 [Canvas 能力核对](D:/Unity_Project_1/3C/docs/canvas-capability-assessment-2026-09-04.md:1)。本次外部案例为结构参考，没有授权或实施新增运行路径。

与用户最初的 Qt 经历相似的公开例子是 [Groot](https://github.com/BehaviorTree/Groot)：旧仓库明确使用 C++／Qt，同时说明旧版仅面向 BT.CPP 3.8.x，已进入维护模式，后继为 Groot 2。它证明独立作者工具与领域执行器可以分离，不能作为直接适配本项目的推荐。

**8．调研后仍不能直接下的结论。**

没有证据可以断言：多数商业动作游戏都采用 Timeline＋Tree；动作级一定比角色级快；所有编译都应删除；任意第三方任务都能够正确回滚；导入图插件就能免掉正式角色预览与状态恢复的接入工作。

本次更支持按职责做选择：保留已有模拟管线；分别判断角色流程是否需要资产化、动作作者需要多大组合能力，以及哪些数据／状态生成工作确实减少了维护成本。本文没有替用户决定实施顺序，也没有修改尚未重新确认范围的重构计划。

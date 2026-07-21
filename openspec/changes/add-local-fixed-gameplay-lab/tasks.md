## 1. 基线与并行边界

- [x] 1.1 读取本change的proposal、design、tasks和全部spec delta。
- [x] 1.2 记录当前未归档change列表、任务计数与严格校验结果。
- [x] 1.3 记录当前共享工作树改动与对应并行change owner。
- [x] 1.4 锁定当前Float Local Composition、Source、Pipeline、Solver与actor registration类型清单。
- [x] 1.5 锁定当前Fixed Core Composer、Unity Fixed Composer、Source、Pipeline、Solver与actor registration类型清单。
- [x] 1.6 锁定Rollback Source、Endpoint、history、restore、committer和network diagnostics类型清单。
- [x] 1.7 锁定Corin Float32/Fixed Program identity、LayoutHash与Projection identity。
- [x] 1.8 锁定Deterministic collision artifact MapId、ContentHash与KCC ConfigurationHash。
- [x] 1.9 锁定CharacterMovementTestEnvironment与OpenKCCMovementCourse唯一作者来源。
- [x] 1.10 确认AI Controller change的Committed Actor Observation合同已安装或保持本change对应任务未开始。
- [x] 1.11 确认Equipment change占用的Semantic compiler、definition与Corin资产不在当前修改边界。
- [x] 1.12 确认实施不需要新增fallback、旧类型wrapper、单Peer本地模式或第二Composer。

## 2. Model-neutral Unity Fixed程序集

- [x] 2.1 创建model-neutral Unity Fixed runtime程序集目录与asmdef。
- [x] 2.2 设置程序集只引用公共Simulation、Fixed Core、Presentation、Input、Diagnostics与Unity必要模块。
- [x] 2.3 禁止新程序集引用DeterministicRollback model、endpoint、transport或history程序集。
- [x] 2.4 迁移`FixedProgramRuntimeDefinition`到新程序集和正式Fixed命名空间。
- [x] 2.5 迁移`FixedPassExecutionBackendDefinition`到新程序集和正式Fixed命名空间。
- [x] 2.6 迁移Fixed runtime package builder的model-neutral部分。
- [x] 2.7 迁移Fixed Program catalog与actor binding lowering。
- [x] 2.8 迁移Fixed Simulation snapshot codec的model-neutral合同。
- [x] 2.9 迁移Fixed Source ports的model-neutral合同。
- [x] 2.10 迁移Fixed Runtime Launcher合同。
- [x] 2.11 更新公共Composition compatibility校验识别Fixed Target而不识别Rollback。
- [x] 2.12 删除Rollback目录中的旧Program Runtime Definition类型。
- [x] 2.13 删除Rollback目录中的旧Backend Definition类型。
- [x] 2.14 删除旧Fixed公共合同的兼容命名和wrapper。

## 3. Fixed Actor registration与输出合同

- [x] 3.1 定义model-neutral`IFixedSimulationActorRegistration`。
- [x] 3.2 保留ActorId、Program、ProgramIdentity、WorldBodyBindingId与InitialBody合同。
- [x] 3.3 将Presentation output收窄为Fixed model-neutral output port。
- [x] 3.4 将Simulation diagnostics保持为Fixed numeric-target sink。
- [x] 3.5 保留BeginLogicTick生命周期。
- [x] 3.6 保留transactional result commit begin合同。
- [x] 3.7 保留transactional result commit complete合同。
- [x] 3.8 保留transactional result commit abort合同。
- [x] 3.9 迁移Fixed output aggregate到model-neutral程序集。
- [x] 3.10 迁移Fixed diagnostics aggregate到model-neutral程序集。
- [x] 3.11 迁移Fixed Unity Presentation output adapter到model-neutral程序集。
- [x] 3.12 迁移Fixed published Actor result observation到model-neutral registration。
- [x] 3.13 从基础registration移除Rollback runtime state字段。
- [x] 3.14 从基础registration移除Rollback output committer字段。
- [x] 3.15 从基础registration移除network diagnostics字段。
- [x] 3.16 从基础registration移除`IRollbackLocalInputAdapter`类型依赖。
- [x] 3.17 保持activation、deactivation、presentation target与diagnostics registry异常安全。
- [x] 3.18 保持registration Dispose owner顺序唯一。

## 4. 唯一Fixed Unity Composer

- [x] 4.1 创建model-neutral`UnityFixedSimulationSessionComposer`。
- [x] 4.2 只接受Fixed Program Runtime Definition。
- [x] 4.3 只接受Fixed Pass Execution Backend Definition。
- [x] 4.4 强类型校验Fixed actor registration roster。
- [x] 4.5 校验Program semantic、ProgramHash与LayoutHash一致。
- [x] 4.6 校验Pipeline numeric profile与Target ABI一致。
- [x] 4.7 校验WorldSolver identity与capability完整。
- [x] 4.8 校验Snapshot codec与state ABI一致。
- [x] 4.9 从Prepared Source获取Fixed runtime package与ports。
- [x] 4.10 通过Core`FixedSimulationSessionComposer`创建唯一runtime。
- [x] 4.11 创建model-neutral Fixed output aggregate。
- [x] 4.12 创建model-neutral Fixed diagnostics aggregate。
- [x] 4.13 将source-specific committer作为显式port传入而不识别模型类型。
- [x] 4.14 将source-specific restore port作为可选正式能力传入。
- [x] 4.15 让缺失required restore capability的Pipeline在创建前失败。
- [x] 4.16 让不要求restore的Local Pipeline不创建空Rollback state。
- [x] 4.17 返回numeric-neutral runtime handle与output lifecycle。
- [x] 4.18 删除旧Rollback专属Fixed Composer源码。
- [x] 4.19 全局确认只有一个Unity Fixed Composer构造入口。

## 5. Rollback adapter迁移

- [x] 5.1 定义Rollback registration extension port。
- [x] 5.2 将Rollback local input owner信息移入Rollback extension。
- [x] 5.3 将Rollback runtime diagnostics binding移入Rollback extension。
- [x] 5.4 让Rollback Source校验基础Fixed registration与Rollback extension同时存在。
- [x] 5.5 保持Endpoint roster与Actor order校验不变。
- [x] 5.6 保持local Peer/Player/Actor identity校验不变。
- [x] 5.7 保持exact、predicted、canonical与confirmed input阶段不变。
- [x] 5.8 保持restore source与snapshot history语义不变。
- [x] 5.9 保持Rollback output committer的confirmed tick语义不变。
- [x] 5.10 保持network diagnostics来源不变。
- [x] 5.11 让Rollback Runtime Launcher调用唯一Fixed Composer。
- [x] 5.12 删除Rollback launcher中的Program runtime重复构造。
- [x] 5.13 删除Rollback launcher中的Pipeline compile重复构造。
- [x] 5.14 删除Rollback launcher中的KCC创建重复构造。
- [x] 5.15 删除Rollback launcher中的output aggregate重复构造。
- [x] 5.16 更新Rollback asmdef只单向引用model-neutral Unity Fixed程序集。
- [ ] 5.17 更新Rollback Composition资产到迁移后的正式类型。
- [ ] 5.18 更新Rollback Prefab与Scene序列化引用。
- [x] 5.19 全局删除旧Fixed Rollback公共命名空间引用。

## 6. Local Fixed Source

- [x] 6.1 定义`LocalFixedSimulationSessionSourceDefinition`。
- [x] 6.2 定义Local Fixed Source identity与semantic version。
- [x] 6.3 要求Fixed Numeric Profile与Target ABI精确匹配。
- [x] 6.4 要求roster中每个Actor都是model-neutral Fixed registration。
- [x] 6.5 要求每个本地可控Actor提供显式Fixed input adapter。
- [x] 6.6 允许Neutral Actor提供正式neutral input source。
- [x] 6.7 按ActorId稳定排序Local Fixed input ports。
- [x] 6.8 在每个logic tick锁存当前render-frame input。
- [x] 6.9 生成tick-bound、非null的Fixed input frame。
- [ ] 6.10 生成正式空Observed World Constraint frame。
- [x] 6.11 提供Local Fixed prepared source。
- [x] 6.12 提供Local Fixed runtime package。
- [x] 6.13 提供Local Fixed Runtime Launcher。
- [x] 6.14 提供Immediate output commit port。
- [x] 6.15 Local Fixed Source不创建Model Definition。
- [x] 6.16 Local Fixed Source不创建Endpoint或Transport。
- [x] 6.17 Local Fixed Source不创建Peer、Player handshake或roster packet。
- [x] 6.18 Local Fixed Source不创建history、prediction或restore state。
- [x] 6.19 缺输入port时fail-closed且不切换Rollback或Float Source。
- [x] 6.20 将Source配置纳入Session identity与diagnostics。

## 7. Standard Fixed Local Pipeline

- [x] 7.1 定义Standard Fixed Local Pipeline identity与revision。
- [x] 7.2 定义Fixed Local Input Ingress Pass。
- [x] 7.3 定义Fixed Local Single Step Schedule Pass。
- [x] 7.4 复用Fixed Program Evaluate Pass。
- [x] 7.5 复用Fixed Body Motion与Vertical Motion pass语义。
- [x] 7.6 复用Fixed World Solve Pass。
- [x] 7.7 复用Fixed Body Motion Finalize Pass。
- [x] 7.8 定义Fixed Local Immediate Egress/Commit Pass。
- [x] 7.9 定义各Pass required input product。
- [x] 7.10 定义各Pass produced product。
- [x] 7.11 锁定Pass顺序和execution support。
- [x] 7.12 校验Pipeline不要求Replay capability。
- [x] 7.13 校验Pipeline不要求Restore capability。
- [x] 7.14 校验Pipeline要求Deterministic KCC的Body Motion、Grounding与Collision能力。
- [x] 7.15 校验Body Motion要求AirborneVerticalMotion。
- [x] 7.16 Local Schedule每个outer logic tick只产生一个simulation step。
- [x] 7.17 Immediate Commit只发布当前step结果。
- [x] 7.18 Immediate Commit不伪造confirmed network tick。
- [x] 7.19 让Pipeline Hash包含全部Pass identity与配置。
- [x] 7.20 删除任何通过跳过Rollback pass形成Local的配置。

## 8. Model-neutral Fixed Character Host

- [x] 8.1 定义Fixed Character Control Source抽象合同。
- [x] 8.2 定义Fixed Player Control Source资产。
- [x] 8.3 定义Fixed Neutral Control Source资产。
- [x] 8.4 定义Fixed Presentation Role的LocalOwner与SimulatedActor。
- [x] 8.5 创建model-neutral Fixed Character Host。
- [x] 8.6 Host显式引用SimulationSessionHost。
- [x] 8.7 Host显式引用Fixed Program asset。
- [x] 8.8 Host显式引用Presentation Projection asset。
- [x] 8.9 Host显式保存ActorId与WorldBodyBindingId。
- [x] 8.10 Host从Logical Spawn构造唯一Fixed InitialBody。
- [x] 8.11 Host按Control Source创建Fixed input adapter。
- [x] 8.12 Player Control Source读取正式Character Input Profile。
- [x] 8.13 Neutral Control Source按Fixed Program input catalog生成typed neutral值。
- [x] 8.14 LocalOwner role创建唯一Camera runtime与look binding。
- [x] 8.15 SimulatedActor role不要求Camera或设备输入。
- [x] 8.16 两种role复用同一Presentation Projection、Animancer、Body Presentation和Foot Placement。
- [x] 8.17 创建model-neutral Fixed registration。
- [x] 8.18 将registration注册到唯一SimulationSessionHost。
- [x] 8.19 保持Host disable时停止整个不可变Session。
- [x] 8.20 保持Host/registration/presentation/input释放顺序异常安全。
- [x] 8.21 从model-neutral Host移除Endpoint引用。
- [x] 8.22 从model-neutral Host移除Peer local/remote判断。
- [x] 8.23 从model-neutral Host移除Rollback runtime diagnostics类型。

## 9. Rollback Character装配迁移

- [ ] 9.1 将现有Rollback角色Prefab迁移到model-neutral Fixed Character Host。
- [ ] 9.2 为Rollback本地Actor配置Fixed Player Control Source。
- [ ] 9.3 为Rollback远端Actor配置不拥有设备的正式Control Source。
- [ ] 9.4 创建Rollback Actor adapter组件或资产。
- [ ] 9.5 adapter显式引用Endpoint与基础Fixed Host identity。
- [ ] 9.6 adapter校验Endpoint local Actor与Player Control Source一致。
- [ ] 9.7 adapter为基础registration安装Rollback extension port。
- [ ] 9.8 adapter绑定network diagnostics而不重建Presentation。
- [ ] 9.9 adapter不读取Visual Transform构造Gameplay状态。
- [ ] 9.10 删除旧`DeterministicRollbackCharacterHost`类型。
- [ ] 9.11 删除旧Host序列化字段与meta引用。
- [ ] 9.12 更新Rollback Peer Scene角色实例。
- [ ] 9.13 更新Rollback Prefab Variant identity。
- [ ] 9.14 全局确认InitialBody和Presentation只由基础Fixed Host创建一次。

## 10. Committed Actor Observation与Fixed目标输入

- [ ] 10.1 重新读取AI Controller change安装后的Committed Actor Observation合同。
- [ ] 10.2 确认Observation port位于model-neutral程序集。
- [ ] 10.3 确认Observation snapshot来自已提交World Body。
- [ ] 10.4 确认Observation不读取Visual Root、Animator或Transform。
- [ ] 10.5 让Fixed registration发布同一Observation snapshot。
- [ ] 10.6 让Fixed Player Control Source消费显式目标Actor binding。
- [ ] 10.7 将Observation position降低为Fixed ActionTargetSnapshot position。
- [ ] 10.8 将Observation yaw降低为Fixed ActionTargetSnapshot yaw。
- [ ] 10.9 保持TargetId使用稳定ActorId。
- [ ] 10.10 Observation不可用时写入正式None target value。
- [ ] 10.11 不缓存上一帧target作为fallback。
- [ ] 10.12 不创建Fixed专用Actor Body registry。
- [ ] 10.13 不修改AI Controller的Observation owner。
- [ ] 10.14 将target input配置identity纳入Fixed input adapter identity。
- [ ] 10.15 在Input diagnostics显示Observation tick与target Actor。

## 11. MotionWarp与KCC裁决诊断

- [ ] 11.1 保持Fixed MotionWarp消费ActionInstance冻结target snapshot。
- [ ] 11.2 保持MotionWarp只修正resolved MotionCurve request。
- [ ] 11.3 保持MotionWarp不直接写Fixed World Body。
- [ ] 11.4 保持MotionWarp request进入唯一World Solve Pass。
- [ ] 11.5 保持World Solve调用唯一Deterministic KCC。
- [ ] 11.6 在Fixed trace记录Warp source identity。
- [ ] 11.7 在Fixed trace记录target snapshot identity与capture tick。
- [ ] 11.8 在Fixed trace记录position/yaw progress。
- [ ] 11.9 在Fixed trace记录requested displacement与yaw。
- [ ] 11.10 在World trace记录KCC applied displacement与yaw。
- [ ] 11.11 在World trace记录Grounded与contact summary。
- [ ] 11.12 增加Applied、Clamped、Blocked与NoTarget的只读展示映射。
- [ ] 11.13 展示映射不重新计算Warp或碰撞结果。
- [ ] 11.14 隔墙或不可达目标时不调用NavMesh或绕路。
- [ ] 11.15 删除任何Warp后的Transform位置补偿。

## 12. KCC正式配置与手感参数边界

- [x] 12.1 盘点DeterministicKccWorldSolverDefinition全部序列化字段。
- [x] 12.2 盘点DeterministicKccConfiguration全部runtime字段。
- [x] 12.3 核对capsule radius进入ConfigurationHash。
- [x] 12.4 核对capsule height进入ConfigurationHash。
- [x] 12.5 核对skin width进入ConfigurationHash。
- [x] 12.6 核对minimum ground normal进入ConfigurationHash。
- [x] 12.7 核对maximum step height进入ConfigurationHash。
- [x] 12.8 核对ground snap distance进入ConfigurationHash。
- [x] 12.9 核对movement/query tolerance进入ConfigurationHash。
- [x] 12.10 核对candidate/contact/pair capacity进入ConfigurationHash。
- [x] 12.11 核对sweep/contact/pair iteration进入ConfigurationHash。
- [x] 12.12 核对策略semantic version进入ConfigurationHash。
- [ ] 12.13 让Local Fixed与Rollback引用同一个CorinDeterministicKcc资产。
- [ ] 12.14 让Local Fixed与Rollback引用同一个collision world asset。
- [ ] 12.15 让GameplayLab显示KCC ConfigurationHash。
- [ ] 12.16 让GameplayLab显示collision ContentHash。
- [x] 12.17 盘点Program中的move speed、turn speed、gravity与stop threshold正式参数。
- [ ] 12.18 为加速、减速、急停、空中控制和落地衔接确认唯一正式owner。
- [ ] 12.19 缺少正式owner的手感参数先扩展Program authoring合同。
- [ ] 12.20 将新增Program参数纳入SemanticHash、ProgramHash与两个Numeric Target。
- [ ] 12.21 删除任何EditorPrefs、static field或Development组件中的手感倍率。
- [ ] 12.22 保持Foot Placement参数只影响Presentation。

## 13. Gameplay Lab场景与Variant

- [ ] 13.1 创建`Assets/Scenes/GameplayLab`目录。
- [ ] 13.2 创建`GameplayLab.unity`。
- [ ] 13.3 在场景中放置唯一GameplayTickSystem。
- [ ] 13.4 在场景中放置唯一GameplayLabBootstrap。
- [ ] 13.5 在场景中复用CharacterMovementTestEnvironment Prefab。
- [ ] 13.6 确认环境内OpenKCCMovementCourse只存在一次。
- [ ] 13.7 确认场景中没有预激活SimulationSessionHost。
- [x] 13.8 定义`GameplayLabSessionVariantDefinition`资产类型。
- [x] 13.9 Variant保存稳定VariantId。
- [x] 13.10 Variant显式引用一个runtime root Prefab。
- [x] 13.11 Variant保存预期Numeric Profile identity。
- [x] 13.12 Variant保存预期Source identity。
- [x] 13.13 Variant保存预期Pipeline identity。
- [x] 13.14 Variant保存预期Solver identity。
- [x] 13.15 Bootstrap要求唯一Variant引用。
- [x] 13.16 Bootstrap实例化一个runtime root。
- [x] 13.17 Bootstrap拒绝场景已有第二个Session root。
- [x] 13.18 Bootstrap在Session进入Preparing后锁定Variant。
- [x] 13.19 Bootstrap不按enum、名称或可用类型选择Variant。
- [x] 13.20 Bootstrap不在失败时实例化另一个Variant。
- [ ] 13.21 创建Local Float32 runtime root Prefab。
- [ ] 13.22 Float root复用现有Float32 Composition与Character Host链。
- [ ] 13.23 Float root使用Unity CharacterController Solver。
- [ ] 13.24 创建Local Fixed runtime root Prefab。
- [ ] 13.25 Fixed root使用Local Fixed Composition。
- [ ] 13.26 Fixed root使用model-neutral Fixed Character Host。
- [ ] 13.27 Fixed root使用Deterministic KCC。
- [ ] 13.28 两个root使用相同ActorId集合与初始作者位置。
- [ ] 13.29 两个root复用同一Corin authoring源对应的target-specific Program。
- [ ] 13.30 两个root复用同一Presentation Projection语义。
- [ ] 13.31 创建Local Float32 Variant资产。
- [ ] 13.32 创建Local Fixed Variant资产。
- [ ] 13.33 场景默认Variant必须是显式资产引用而非代码默认。

## 14. Gameplay Lab训练目标与障碍布局

- [ ] 14.1 在两个runtime root中注册玩家Actor。
- [ ] 14.2 在两个runtime root中注册训练目标Actor。
- [ ] 14.3 训练目标使用正式SimulatedActor Presentation Role。
- [ ] 14.4 训练目标使用Neutral Control Source。
- [ ] 14.5 玩家显式绑定训练目标ActorId。
- [ ] 14.6 两个Actor进入同一Session roster。
- [ ] 14.7 两个Actor进入同一WorldSolver batch。
- [ ] 14.8 复用环境中的平地测试区域。
- [ ] 14.9 复用环境中的坡面测试区域。
- [ ] 14.10 复用环境中的台阶测试区域。
- [ ] 14.11 复用环境中的墙角测试区域。
- [ ] 14.12 复用环境中的薄墙测试区域。
- [ ] 14.13 复用环境中的狭窄通道测试区域。
- [ ] 14.14 增加位置与朝向可识别的只读场景标牌。
- [ ] 14.15 标牌不包含Gameplay collider或隐藏逻辑。
- [ ] 14.16 不复制第二份测试地形Prefab。

## 15. Gameplay Lab只读诊断

- [ ] 15.1 定义Gameplay Lab diagnostics snapshot。
- [ ] 15.2 显示VariantId。
- [ ] 15.3 显示Numeric Profile与Target ABI。
- [ ] 15.4 显示SourceId。
- [ ] 15.5 显示PipelineId、Revision与Hash。
- [ ] 15.6 显示SolverId与capabilities。
- [ ] 15.7 显示collision world MapId与ContentHash。
- [ ] 15.8 显示KCC ConfigurationHash。
- [ ] 15.9 显示Actor roster与committed tick。
- [ ] 15.10 显示玩家committed Body position、yaw、velocity与Grounded。
- [ ] 15.11 显示目标committed Body position与yaw。
- [ ] 15.12 显示MotionWarp source与target snapshot。
- [ ] 15.13 显示MotionWarp requested/applied displacement差值。
- [ ] 15.14 显示World contact与solver disposition。
- [ ] 15.15 显示Foot Placement solver与最新只读状态。
- [ ] 15.16 显示Animation Marker Sync group与phase状态。
- [ ] 15.17 使用“Walk/Run步态相位匹配”命名。
- [ ] 15.18 不显示不存在的Motion Matching、Pose Search或Stride Warping能力。
- [ ] 15.19 诊断只读取正式runtime trace与snapshot。
- [ ] 15.20 诊断不调用Transform、Physics query或Graph重新求值。

## 16. computer-use可操作输入边界

- [ ] 16.1 盘点Gameplay Lab玩家Input Action绑定。
- [ ] 16.2 为Attack提供不移动鼠标的离散键盘binding。
- [ ] 16.3 键盘binding进入同一个Character Input Profile request。
- [ ] 16.4 键盘binding不直接激活Action。
- [ ] 16.5 键盘binding不写Blackboard。
- [ ] 16.6 键盘binding不写Transform。
- [ ] 16.7 保留原左键Attack绑定。
- [ ] 16.8 为需要的镜头/朝向操作提供正式Input Action。
- [ ] 16.9 不增加自动传送到目标的调试命令。
- [ ] 16.10 不增加绕过KCC的目标位置按钮。

## 17. Editor入口与文档路径统一

- [x] 17.1 为Gameplay Lab增加明确Editor launcher入口。
- [x] 17.2 launcher只打开`Assets/Scenes/GameplayLab/GameplayLab.unity`。
- [x] 17.3 launcher不进入ProductBootstrap。
- [x] 17.4 launcher不修改普通Build Settings。
- [x] 17.5 launcher不填充ResourceEndpoint或AuthEndpoint。
- [x] 17.6 launcher不作为商业启动失败fallback。
- [ ] 17.7 保留StandaloneGameplay产品入口。
- [ ] 17.8 保留DeterministicRollback Peer Scene入口。
- [ ] 17.9 保留ServerAuthoritative Scene入口。
- [ ] 17.10 从`openspec/project.md`删除不存在的SandBox路径。
- [ ] 17.11 在`openspec/project.md`区分StandaloneGameplay与GameplayLab职责。
- [ ] 17.12 修正商业change proposal中的Local Play SandBox路径。
- [ ] 17.13 修正商业change design中的Local Play SandBox路径。
- [ ] 17.14 保持商业spec的StandaloneGameplay产品链不变。
- [ ] 17.15 全局确认没有第三个Sandbox场景名称或旧launcher命令。

## 18. 旧路径删除

- [x] 18.1 删除Rollback命名空间中的通用Fixed Program Runtime Definition。
- [x] 18.2 删除Rollback命名空间中的通用Fixed Backend Definition。
- [x] 18.3 删除Rollback命名空间中的通用Fixed registration合同。
- [x] 18.4 删除Rollback命名空间中的通用Fixed output aggregate。
- [x] 18.5 删除Rollback命名空间中的通用Fixed diagnostics aggregate。
- [x] 18.6 删除Rollback专属Unity Fixed Composer。
- [ ] 18.7 删除旧DeterministicRollbackCharacterHost。
- [ ] 18.8 删除Endpoint驱动Presentation Role的旧字段。
- [ ] 18.9 删除Endpoint驱动Input创建的旧字段。
- [ ] 18.10 删除任何Local Fixed loopback endpoint配置。
- [ ] 18.11 删除任何Local Fixed fake peer配置。
- [ ] 18.12 删除任何第二KCC Definition或collision artifact副本。
- [ ] 18.13 删除任何Transform target provider。
- [ ] 18.14 删除任何Warp后Transform补偿。
- [ ] 18.15 删除任何runtime Variant enum或fallback。
- [ ] 18.16 删除不存在SandBox路径的代码与文档引用。

## 19. 资产身份与生成产物

- [ ] 19.1 更新Fixed Unity程序集引用后的脚本meta与资产类型引用。
- [ ] 19.2 更新Rollback Composition资产。
- [ ] 19.3 创建Local Fixed Composition资产。
- [ ] 19.4 Local Fixed Composition引用现有Fixed Program Runtime Definition。
- [ ] 19.5 Local Fixed Composition引用现有Fixed Backend Definition。
- [ ] 19.6 Local Fixed Composition引用Standard Fixed Local Pipeline。
- [ ] 19.7 Local Fixed Composition引用Local Fixed Source。
- [ ] 19.8 Local Fixed Composition引用现有CorinDeterministicKcc。
- [ ] 19.9 核对Local Fixed与Rollback Fixed ProgramHash一致。
- [ ] 19.10 核对Local Fixed与Rollback LayoutHash一致。
- [ ] 19.11 核对Local Fixed与Rollback collision ContentHash一致。
- [ ] 19.12 核对Local Fixed与Rollback KCC ConfigurationHash一致。
- [ ] 19.13 核对Local Fixed与Rollback Solver identity一致。
- [ ] 19.14 更新受迁移影响的Session、Pipeline与Source identity。
- [ ] 19.15 不生成旧identity reader或converter。

## 20. 编译与规范校验

- [x] 20.1 构建portable Simulation Core工程并禁用build server/shared compilation。
- [x] 20.2 构建Fixed Core工程并使用相同参数。
- [x] 20.3 构建Deterministic KCC工程并使用相同参数。
- [x] 20.4 构建model-neutral Unity Fixed工程并使用相同参数。
- [x] 20.5 构建DeterministicRollback portable工程并使用相同参数。
- [x] 20.6 构建DeterministicRollback Unity工程并使用相同参数。
- [x] 20.7 构建Character runtime工程并使用相同参数。
- [ ] 20.8 构建Gameplay runtime/editor工程并使用相同参数。
- [x] 20.9 每轮dotnet构建后立即执行`dotnet build-server shutdown`。
- [x] 20.10 运行`openspec validate add-local-fixed-gameplay-lab --strict --no-interactive`。
- [ ] 20.11 运行`openspec validate --all --strict --no-interactive`。
- [x] 20.12 全局搜索确认公共Fixed程序集不引用Rollback类型。
- [x] 20.13 全局搜索确认Local Fixed不引用Endpoint、Relay、Peer或history类型。
- [x] 20.14 全局搜索确认只有一个Unity Fixed Composer。
- [ ] 20.15 全局搜索确认只有一个Committed Actor Observation owner。
- [ ] 20.16 全局搜索确认不存在SandBox旧路径。
- [x] 20.17 核对tasks勾选只反映已完成实施，不以实机手动观察代替。

## 21. Live Debug作者内容版本闭环

- [x] 21.1 为Program SourceMap entry增加作者内容hash字段。
- [x] 21.2 让Character source location携带作者内容hash。
- [x] 21.3 让Graph Node source使用Graph authoring fingerprint。
- [ ] 21.4 让Timeline root、Track与Clip source使用Timeline authoring fingerprint。
- [x] 21.5 将作者内容hash写入Semantic IR canonical payload。
- [x] 21.6 提升Semantic IR artifact与payload版本并拒绝旧版本。
- [x] 21.7 将作者内容hash写入Float Program canonical SourceMap。
- [x] 21.8 提升Float Program artifact、payload与SourceMap table版本并拒绝旧版本。
- [x] 21.9 将作者内容hash写入Fixed Program canonical SourceMap。
- [x] 21.10 提升Fixed Program artifact、payload与SourceMap table版本并拒绝旧版本。
- [ ] 21.11 提升Character compiler版本使正式source revision变化。
- [x] 21.12 让Runtime Debug优先解析Timeline、Track与Clip source identity。
- [x] 21.13 让Runtime Debug从SourceMap建立Graph与Timeline容器内容hash。
- [x] 21.14 让同一作者容器出现冲突hash时fail-closed。
- [x] 21.15 删除以整个ProgramHash充当Graph或Timeline作者hash的旧行为。
- [ ] 21.16 重新生成Corin Semantic IR、Float Program、Fixed Program与Projection。
- [ ] 21.17 核对CorinAttack1Timeline authoring id与runtime SourceMap identity一致。
- [ ] 21.18 核对Graph与Timeline Live Debug仍使用identity加content hash精确匹配且没有名称fallback。

## 22. 统一Editor Launcher

- [x] 22.1 将工具栏Launcher按钮绑定到唯一`Tools/3C/Launcher`面板。
- [x] 22.2 在面板中增加`单机 / Gameplay Lab`分组。
- [x] 22.3 从Gameplay Lab实际Variant资产读取Float32与Fixed选项。
- [x] 22.4 在进入Play前把所选Variant写入Gameplay Lab Bootstrap。
- [x] 22.5 Gameplay Lab入口不创建ProductBootstrap、CDN、Auth、Relay或远端客户端。
- [x] 22.6 在面板中增加`正式启动 / Published Player`分组。
- [x] 22.7 正式Player入口先校验发布闭包再从Bootstrap进入正式资源管理链。
- [x] 22.8 Product配置无效时禁用正式Player Run与Player构建且不提供本地fallback。
- [x] 22.9 允许Content在Endpoint部署前独立构建正式YooAsset版本。
- [x] 22.10 在Product分组保留资源版本输入、Content构建与Player构建。
- [x] 22.11 在`双端验证 / Network Test Products`分组增加三个产品的Prepare、Build与Run操作。
- [x] 22.12 在面板中增加`编辑器启动 / Bootstrap Play`分组并只从Bootstrap进入正式链。
- [x] 22.13 删除旧Standalone直进、独立商业构建窗口和分散网络测试菜单。
- [x] 22.14 普通Player Build Settings只保留Bootstrap。
- [x] 22.15 普通Player构建校验要求Build Settings精确等于Bootstrap单场景闭包。
- [ ] 22.16 编译受影响的Editor程序集并使用禁用build server与shared compilation参数。
- [x] 22.17 运行当前change严格规范校验。

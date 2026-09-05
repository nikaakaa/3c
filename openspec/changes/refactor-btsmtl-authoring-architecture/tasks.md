## 1. 固定迁移基线与边界

- [ ] 1.1 记录当前工作区差异、有效Definition／Composition／Target／Program／Projection和既有输入trace，交付保护清单与可重建基线；明确TrainingEnemy等既有无效目标，不通过修补它们取得基线。
- [x] 1.2 沿角色RootTree、状态机、Action和装备Route列出“C# Locomotion State／Transition、技能请求规则、技能内容、删除”的业务映射，明确外层动作状态机直接拆除，交付每个正式入口及引用的迁移表。
- [x] 1.3 对账设计中的33项capability及预览、Pose、装备后续change的共享接口，交付明确替换／保留项；同步接口描述不接管外部change功能。
- [ ] 1.4 用既有Replay／Proof与业务观察确定同版本重复性和跨实现比较字段，交付输入／Body／动作阶段／输出基线及来源映射规则；不得以不同ABI的StateHash直接判定回归。

## 2. 角色与技能基础合同

- [ ] 2.1 定义C#显式StateMachine／State／Transition及角色控制模块合同，覆盖Enter／Tick／Exit、来源／目标、纯条件、优先级和稳定顺序、输入／结果、参数、typed状态schema与代码版本；通过既有登记／组合校验交付唯一模块目录。
- [ ] 2.2 定义SkillDefinition、入口签名、ActionProfile引用、子图依赖及允许的后续候选，交付相同策略被多个技能引用时仍可精确选中技能的作者／校验结果。
- [ ] 2.3 明确ActionInstance与SkillExecutionState的唯一owner关系，交付Context、模板、实例、调用点和generation的typed地址及生命周期合同，删除第二生命周期候选设计。
- [ ] 2.4 定义角色运行包中的控制binding、SkillProgram目录、组合布局与显式容量，交付缺失模块、非法依赖、并发／容量不符的正式诊断。

## 3. 语义与Target编译

- [ ] 3.1 将角色组合Discovery改为读取控制合同和技能闭包，迁移原Character／Equipment graph roots；通过正式Frontend报告验证根目录唯一且旧角色root不再生成。
- [ ] 3.2 提取技能节点业务族的语义发射与typed端口合同，复用唯一操作目录；交付UI能力、Emitter和Target支持集一致的验证结果。
- [ ] 3.3 接入子图输入／输出签名、调用点及occurrence绑定，交付完整引用链与类型校验；子图递归被拒绝，显式Loop及跨技能候选分别验证。
- [ ] 3.4 迁移Tree／Timeline／TreeClip／局部状态机发射和状态声明，保持Decision／Commit及停止顺序；通过正式IR Inspector和source map核对对应关系。
- [ ] 3.5 将C#控制参数／state合同、GE／Equipment／Body Motion描述及技能目录纳入同一IR／角色运行包，交付canonical identity与依赖闭包结果。
- [ ] 3.6 分别完成Float32与Fixed降低、组合state layout、SkillProgram与codec升级，交付同语义双Target的构建和旧ABI拒绝结果，不复制业务控制规则。
- [ ] 3.7 更新既有.csir／.csim store、wrapper和原子发布组，交付精确重读及混版拒绝结果；Projection只迁移技能producer来源，保持当前Pose实现。

## 4. 技能解释器与实例状态

- [ ] 4.1 将现有控制解释器的正式使用范围收至技能，迁移调用者并删除角色RootTree调度分支；以引用检查确认正式Character只走代码控制和同一技能解释器。
- [ ] 4.2 接入ActionInstance拥有的节点、Timeline、局部状态机和等待状态，交付跨Tick字段清单及既有状态coverage校验结果。
- [ ] 4.3 实现子图按值入参、声明返回值和中止不提交输出的调用frame，交付多个调用点复用同一子图时的独立地址与运行诊断。
- [ ] 4.4 接入合法并发释放、重复激活与调用generation，交付模板共享、实例隔离和容量失败的现有Runtime／Validator结果，不增加对象clone。
- [ ] 4.5 迁移技能变量与Frame投影，角色控制字段通过只读事实暴露；交付已删除Character Blackboard输入镜像、无跨实例写入的引用及布局检查。
- [ ] 4.6 接通父级Complete／Cancel／Interrupt／Reject／Abort／teardown对全部子图与Timeline的停止，交付graceful进度、force释放和重复停止的生命周期事实。
- [ ] 4.7 对技能内Motion、GE、Equipment与Presentation叶子收敛唯一请求／输出端口，交付无Transform、播放器、WorldSolver或网络旁路调用的定向检查。

## 5. C#角色控制迁移

- [ ] 5.1 将有效角色的Gameplay Locomotion迁入C#显式State／Transition，保留原输入、数值与同Tick转换顺序；通过既有业务观察比较核对控制状态、Body／Intent时序，不复制Presentation Pose State。
- [ ] 5.2 将动作候选、输入消费、连段与取消迁为控制代码中的独立技能请求规则，允许State保持active时请求技能；删除外层动作图及角色级连招状态机，不新增角色总状态机或C#技能阶段镜像，交付输入到精确Skill请求的诊断链。
- [ ] 5.3 复用唯一准入、Required Tag、TargetRequirement与目标快照规则，交付纯查询与最终提交读取同一候选的现有验证结果。
- [ ] 5.4 接入显式replacement及source stop barrier，区分独立并发请求；交付来源、退出原因、停止进度和新实例建立顺序的事实。
- [ ] 5.5 使当前Decision窗口在同Tick角色决策前可读，迁移原连段／取消规则；通过既有Replay业务事件核对不额外延后一渲染帧。
- [ ] 5.6 保持AIIntentProgram与CharacterSimulationInput边界，更新只读输入合同引用；交付AI不访问控制／技能私有状态的依赖与Validator结果，不修复TrainingEnemy资产。

## 6. 装备核心接入

- [ ] 6.1 将Feature Persistent／Route角色图入口迁成代码binding与Skill引用，更新作者目录及IR；交付稳定Slot／Route／Feature／参数身份和旧Host opcode零引用结果。
- [ ] 6.2 保留装备Begin／Commit／Cancel事务、Tag／Effect贡献及Feature generation，将控制状态接入同一typed布局；交付既有事务和上下文合同的验证结果。
- [ ] 6.3 更新Action Equipment Context、参数查找和已有效装备数据的调用者，交付精确Skill绑定与snapshot覆盖；不补做装备样例或网络装备业务。

## 7. Session、状态与网络恢复

- [ ] 7.1 在原Evaluate／WorldResolve／Finalize Step内接入控制模块和技能解释器，交付原四阶段、多Tick与Commit入口的调用图及Pipeline编译结果。
- [ ] 7.2 扩展两个Target的状态transaction、copy、codec和hash，覆盖控制State identity、必要进入Tick／转换进度／输入缓存、ActionInstance、子图frame、参数、Timeline及停止进度；恢复直接还原数据，不重放Enter／Exit或技能请求，交付完整状态schema与旧版本拒绝结果。
- [ ] 7.3 更新Composition、ProgramCatalog和模块装配校验，交付缺模块、混版、能力不足及不兼容Target在Active前失败的正式报告。
- [ ] 7.4 更新ServerAuthoritative owner checkpoint、Full／Delta与Correction恢复，交付完整技能状态恢复及原Remote观察体边界的现有证明。
- [ ] 7.5 更新Fixed Rollback snapshot、history、分层hash与恢复投影，交付同输入重算中实例／调用状态一致的现有Proof；Relay继续只路由。
- [ ] 7.6 更新EventId来源、output disposition与state publish衔接，交付确认／替换／抑制及重复输出的既有诊断，保证代码来源不伪造Graph节点。

## 8. 规则与数据发布

- [ ] 8.1 将共享合同、稳定解释器与可更新控制／技能规则按设计分程序集，交付单向依赖及portable规则不引用Unity／Fantasy对象的编译结果。
- [ ] 8.2 将规则程序集接入现有HybridCLR构建、依赖、裁剪／泛型生成与启动加载，交付精确模块版本和现有资源发布闭包；不新装热更框架。
- [ ] 8.3 更新Unity客户端／Authority与普通.NET Authority产品的规则模块和技能产物发布清单，交付相同语义版本、完整依赖及缺失模块拒绝结果；Relay产品不安装Gameplay执行。
- [ ] 8.4 接通新Session采用新代码／技能版本和活动Session版本锁定，交付正式manifest及加载状态报告，不增加对局中状态迁移、旧ABI读取或兼容开关。

## 9. 技能作者模块与共享框架

- [ ] 9.1 将技能定义、Flow／局部状态机、Timeline／TreeClip、变量／参数和领域叶子的作者规则从中央类迁出，交付各模块输入输出及唯一Capability装配。
- [ ] 9.2 将节点创建、配置、复制粘贴和端口变化统一接入现有Port Shape与typed Mutation，交付作者目录／Validator一致结果并删除重复字段表。
- [ ] 9.3 增加技能定义、签名与inline／shared子图编辑，交付从定义到Tree／Timeline／调用点的精确owner导航与原正式转换命令。
- [ ] 9.4 将角色入口改为代码控制binding／参数及技能目录，删除角色图创建菜单和无效页面；交付无假RootTree及无任意代码调用节点的能力清单。
- [ ] 9.5 保留AI／Pose共享画布及独立领域数据，实现窗口重载恢复、字段草稿保护和单次订阅释放；交付现有窗口状态／生命周期诊断，不在OnInspectorGUI执行重计算。

## 10. Document v5整包闭合

- [ ] 10.1 定义v5控制配置与skill definition分片、精确允许文件族及local身份规则，交付schema与只读context／生成数据边界文档。
- [ ] 10.2 更新Exporter与strict Codec／Mapper并按内容分责，交付canonical往返、整包hash和未知／旧字段拒绝的现有校验结果。
- [ ] 10.3 更新Reconciler／planning symbol及Mutation lowering，使新技能、子图、Timeline和控制binding形成完整有序计划；交付dry-run依赖与删除顺序报告。
- [ ] 10.4 接入所有新owner的同一Undo、保存、失败恢复和reverse export，交付任一分片失败不发布半包的既有事务结果。
- [ ] 10.5 删除v4及更早reader／writer／manifest分支和角色RootTree正文入口，沿用两个domain与五生命周期工具；交付旧包明确拒绝、重新checkout生成v5的结果。
- [ ] 10.6 更新btsmtl-agent-authoring技能、MCP合同描述和实际代码地图，交付路径／字段可解析且与唯一v5实现一致的检查结果，不新增局部写工具。

## 11. 技能工作区与诊断

- [ ] 11.1 将Action Workspace统一到SkillDefinition、ActionProfile及调用点上下文，支持Tree-only和多个／嵌套Timeline；交付不猜唯一Timeline的typed页面状态。
- [ ] 11.2 打通技能到AnimationClip、producer、Profile和AnimationSlot的原owner导航，交付无镜像字段或第二动画资源配置的引用检查。
- [ ] 11.3 扩展source map和Trace区分C#模块／State／Transition来源、技能模板、ActionInstance、调用点和generation，交付控制转换与技能激活可分别追溯、同模板多实例隔离的诊断输出，不伪造角色图节点。
- [ ] 11.4 更新IR Inspector、Live Debug和窗口Follow／Pin绑定，交付控制合同及技能执行根可查看、过期目标不选其他实例的状态报告。
- [ ] 11.5 向独立场景预览提供精确技能选择、正式请求及只读实例接口，交付双方接口对账；不创建场景、SkillPreviewRuntime或重复实现旧播放器删除。

## 12. 资产迁移与旧路径清理

- [ ] 12.1 通过现有正式作者事务转换全部选定有效Character根及其技能依赖，交付角色代码／技能映射、仍有效的稳定业务identity与完整引用报告；Graph／Node kind变化时创建新identity并替换引用，不能原地改kind，受保护无效资产继续明确报错。
- [ ] 12.2 转换有效装备入口、输入／变量绑定及有限producer来源，交付新控制／技能目录可构建结果，保持既有Motion曲线、Warp及表现资源内容。
- [ ] 12.3 显式构建并发布所选Target、技能目录和同组Projection，更新现有Launcher／Variant／Profile引用；交付exact artifact与产品引用一致报告。
- [ ] 12.4 删除已替代角色控制图入口、activation／Equipment Host编译注册、旧schema、菜单、字段、别名及废弃文件，交付定向零引用与仍保留AI／Pose／独立预览依赖的业务清单。
- [ ] 12.5 按设计整理最终目录、类型和公开命名，交付无临时桥接、双运行入口或兼容配置的最终代码地图。

## 13. 集成证据与规范收口

- [ ] 13.1 运行现有portable／Editor／产品构建和依赖检查，交付实际构建结果；dotnet／msbuild使用禁用build server参数并立即shutdown，本机Unity CLI按明确项目路径退出且保留主验收Editor，CI禁令不变。
- [ ] 13.2 使用已有Validator、Document生命周期与Replay／Proof覆盖完整新链，交付同版本重复性及跨实现输入／Body／动作阶段／输出比较；不编写新测试、不忽略缺帧或运行错误、不把ProgramHash变化当作行为通过或失败。
- [ ] 13.3 安装本change的delta并同步当前项目口径、Purpose及关联接口说明，交付现行规范与预览／其它change不存在相反共享要求的对账；保留独立预览和受保护任务范围。
- [ ] 13.4 执行严格OpenSpec校验与限定改动diff检查，按完整迁移单元形成中文小步提交，交付文件跳转、删除清单、实际证据及剩余明确错误，不以类行数或文件数宣称完成。

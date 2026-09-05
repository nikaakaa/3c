## 当前进度

更新于 2026-09-05（Asia/Shanghai）。本次核对代码到 `6b94d16f231d03dc3e8cba51f7dc54f55bebadd7`，生成的Gameplay Lab根资产仍到 `6ca8e09a718a9823d43518b9087bbb14481661e8`，位置为主目录 `D:/Unity_Project_1/3C` 的 `main`。后续提交必须继续更新下面对应任务，不能把这里的时点当成永久状态。

本文件是本 change 的当前执行进度；`baseline.md`保留实施起点的历史身份。勾选只表示该条完整要求已满足；未勾任务另外写明“已实现，待验证”“部分完成”“尚未完成”或“待验证”，不再用同一个空框掩盖不同阶段。静态审查、源码编译、产物发布和Replay分别记录，不互相替代。

按原66项完整要求记录：已完成4项，已实现待验证8项，部分完成33项，尚未完成18项，单独待验证3项。该计数不是工作量百分比；具体已落地部分和剩余条件见每条任务下的说明。

### 已有代码交付与剩余边界

| 工作块 | 已有交付与审查 | 尚未完成 |
|---|---|---|
| C#控制合同与接线 | 显式State/Transition、typed状态和唯一模块目录；Corin规则经两Target进入同一Evaluate | 原输入/移动时序比较、所有旧作者入口清理及完整装备迁移 |
| Action与技能实例状态 | `6db20c5f2`共用Activation/Commit/Lifecycle、SlotMap和实例状态管理；`ce72111f2`修正窗口Fact/Trace来源；`1b80885e3`、`c26a15721`和`0a4bc76b3`已接入参数化子图调用frame及按值输入/成功输出边界 | 合法并发/容量、完整停止与恢复运行证据 |
| 编译器职责拆分 | `b419fcdb9 → 770ecfc51`迁出Blackboard声明/状态及领域绑定；`3e1355410`迁出技能目录；`51aed878f → be440e2f5`迁出控制合同发射；`adc29ea30`迁出Timeline/TreeClip编排；`705f01d82`迁出Action/Behavior目录；`bf1289be3`统一Asset/Node来源；`ae28b8998`迁出Equipment目录；`147556f2f`迁出Input目录；`0c416ac24 → a23eb2ae8`迁出Tag/Attribute目录与协调器；`9a036e8ad`迁出全局状态；`475cfa9d0 → 51b3a07c0`按领域拆分节点登记与目录绑定。各模块已参与Editor/Frontend构建，局部代码对照确认原语义保留 | UI能力／Emitter／Target支持集一致性报告；解释器本体的大类拆分与作者/发布职责 |
| 状态与网络身份 | `121ec4a49`、`6dcdfd82a`、`304d83880`等已迁移codec/source/skill/generation及恢复读取边界 | 当前完整控制/技能状态的checkpoint、Rollback及输出对账Proof |
| 当前Corin产物发布 | `00c47f3c`统一两Target与Projection的一次Publish，`31ac3821`提交正式产物；后续控制、Timeline及Action目录拆分后分别正式重建，Program身份不变，最新根Prefab/Scene生成提交为`6ca8e09a7`；12.3已关闭 | 本组实际运行和Replay比较仍归13.2；不等同于全部产品发布完成 |
| 作者工具、Document、发布 | 规划边界已明确；Control.Rules程序集与部分产品装配已有代码 | 作者模块/工作区、唯一Document v5、旧schema删除、热更发布和全部产品装配仍有实质实施工作 |

### 当前构建与回放证据

- Runtime源码编译由实施任务报告通过。当前按要求构建`ThirdPersonClient.Editor.csproj`及其新增语义模块，结果为0错误；输出中的警告均为现有依赖或既有字段警告。主审已确认各新Emitter、节点登记模块、目录绑定模块及共享BehaviorCatalogFields均进入实际Editor工程，正式Frontend的上一次有效产物仍为6ca8e09a7。Editor编译、构建调用与产品receipt的完整日志归位仍由13.1收口。
- 正式资产操作在项目`D:/Unity_Project_1/3C/3cDemo/Client/3C_Client`的主Editor执行。首轮Gameplay Lab重建因旧端点字段抛异常，已由`c3b2e3ed0`修正。后续唯一重建请求虽CLI超时，日志后来明确出现`Shared Gameplay Lab synchronized`；不能继续记为“尚未执行完”，也不能据此直接判定产物一致。
- 18:17核对曾发现新Fixed与旧Float32语义不一致。有效修复代码在`00c47f3c`：同一`CharacterSimulationBuildOrchestrator`请求同时声明Float32和Fixed Target，沿原原子发布组生成Projection；后继`f56064f3`与其Git tree相同，不另算代码进展。正式产物于`31ac3821`提交，之前的混版状态已关闭。
- 控制模块迁出Builder的首步`51aed878f`将控制状态SourceMap误写为普通StateSlot；`be440e2f5`已恢复ControlState，Builder通过显式来源种类保留通用写入。主审对照迁出前代码确认目录字段、状态默认值、顺序及State/Transition来源身份保留，并在当前`Editor.log:44118`看到修正后的正式重建完成，调用栈为`GameplayLabAssetBuilder.cs:149`。实施报告Console为0错误。首轮错误来源类型生成的临时产物不作为交付；下列Program身份已恢复并由主审直接读取核对，根资产更新见`bb41cb4ca`。
- Timeline编排迁移`adc29ea30`保留同一Track/Clip注册表、EmissionSession及Builder；主审对照父提交确认TreeClip入口为Enter/order 0，OnEnable为Enter/order 1，OnDisable与OnDestroy分别为Exit/order 0和1，嵌套CompileGraph继续传递原stateScopeOwner。原中央Timeline与生命周期方法已删除；`bf1289be3`将Node/Asset来源收敛到`CharacterSemanticSourceFactory`。本轮正式重建完成日志为此前`Editor.log:45734`，调用栈为`GameplayLabAssetBuilder.cs:149`；此处只确认代码编排与构建身份，不作为Replay行为通过。
- Action/Behavior目录迁移`705f01d82`保留action/behavior稳定ID、版本3/1、Required/Block/Cancel下的All/Any/None字段、ActionRequestBuffer及ActionInstance状态归属和声明顺序。原CompileActions/CompileBehaviors及公共BehaviorFields已删除，Action/Behavior/GE实际共用CharacterSemanticBehaviorCatalogFields；`bf1289be3`之后Action、GE、目录Compiler共用唯一Asset来源工厂。
- `ae28b8998`迁出Equipment目录，`147556f2f`迁出Input目录，`0c416ac24 → a23eb2ae8`迁出Tag/Attribute目录，`9a036e8ad`迁出全局状态；这些模块仍写入同一个CatalogIndex与Builder，原中央分支已删除。`475cfa9d0`及`51b3a07c0`又把Root/State/Timeline、Input/Blackboard、Action、Camera、Gameplay、Equipment、Motion的节点登记按领域拆开，并把目录绑定拆成共享目录引用、Input、Equipment、Action、Gameplay模块。当前仅有源码构建证据，最新拆分后尚未重新正式发布产物。
- 当前有效两Program均为compiler/24、ProgramId `character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e`、SourceRevision `ba8dcda4ece0f958a5b78cd6f2ad1d2ee8b0ca0b2c5f1efb3a0a31701cb7c68b`、SemanticHash `fe5bbbd7c89aec250a7d0eabbd24ea7392bbd964a6b9bb3ce8e19e059428e969`。Float32 ProgramHash为`00ca45fd7f86f03516b30aef09edfaa4bbccb99291c9d9a2521bc28cf06328e9`，Fixed为`63f7ee3716847d7ebd3c1458fad3a96f16c5c9a1f65c24d6e1d111c7a23e1091`；Target ProgramHash不同是正常数值/布局差异。该身份是上一次有效产物的记录，不能替代本轮正式重建。
- 主审核对三个Variant：LocalFloat32引用Program GUID `5740a6cfbfb0fe542ad6a6cb66fe1a80`，LocalFixed及Rollback引用`91063668fd0eaa84d9b7d688aadbc90a`，三者均引用Projection GUID `f365735adcfd49c4e96070df6bcd3bc4`。实施已报告同组Projection与Launcher Validate完成，代码审查确认原统一Publish检查路径；此结论关闭12.3，不关闭13.2。
- 本轮Corin Replay尚无完整通过证据。历史输入/Proof的精确路径见`baseline.md`，其中比较帧数为0的旧Proof不作为本轮回归通过。
- 下一份可交付验证结果应包含对应任务号、代码提交、正式入口/日志、实际产物身份及Replay运行/比较位置。遇到一次请求超时先查该请求最终结果，不重复发起有副作用的构建，不手改生成资产。

### 执行与沟通规则

- 实现依据本change的proposal、design、delta specs和任务要求自主推进，继续中文小步提交，并随业务提交更新本文件对应任务的状态、证据和剩余条件。已明确的职责迁移、重复代码清理和验证由实现直接完成。
- 常规进度写入本文件，不按类或提交向规划窗口发送完成消息，不等待逐项审查或同意。规划窗口按文档和代码查看进展，不用消息往返代替任务记录。此前逐小步汇报的安排停止使用；小步提交不等于小步汇报，也不要求每次拆类都重建完整资产作为继续开发的门槛。
- 只有文档存在影响实现的歧义或缺口、现有正确业务或其他owner接口与设计冲突、需要改变既定职责/合同或必须绕过现有系统时，才向规划窗口提出具体问题。问题一次说明相关任务、代码事实、无法自行确定的决策及业务取舍；其余明确且不受冲突影响的工作继续推进。常规代码错误和构建失败按已有链路修复，不自动转成规划审批。
- 整个change完成后统一提交任务结果、结构迁移/删除清单及构建/Replay证据，再进行整体审查。用户询问进展时直接读取当前任务记录和代码；除上述不确定事项及最终交付外，不主动发送例行进度消息。

只有条款全部满足才勾选，不能通过删掉验证要求或改小范围来取得完成状态。静态结构审查与构建、Replay行为证据继续分别保留。

## 1. 固定迁移基线与边界

- [ ] 1.1 记录当前工作区差异、有效Definition／Composition／Target／Program／Projection和既有输入trace，交付保护清单与可重建基线；明确TrainingEnemy等既有无效目标，不通过修补它们取得基线。

  当前状态：**部分完成**。基线文件已记录工作区、受保护资产、两Target及输入身份；迁移前完整源码/外部依赖和正式receipt证据仍未闭合。此缺口不重新作为已授权实施的开工门槛。
- [x] 1.2 沿角色RootTree、状态机、Action和装备Route列出“C# Locomotion State／Transition、技能请求规则、技能内容、删除”的业务映射，明确外层动作状态机直接拆除，交付每个正式入口及引用的迁移表。

  当前状态：**已完成**。源到目标的Locomotion、Transition、7个技能入口、Timeline/TreeClip/窗口映射见baseline.md；这是迁移映射完成，不是后续行为验证完成。
- [x] 1.3 对账设计中的33项capability及预览、Pose、装备后续change的共享接口，交付明确替换／保留项；同步接口描述不接管外部change功能。

  当前状态：**已完成**。baseline.md已交付33项capability的替换/保留与领域所有权矩阵；后续并行变更的最终组合安装仍由13.3对账。
- [ ] 1.4 用既有Replay／Proof与业务观察确定同版本重复性和跨实现比较字段，交付输入／Body／动作阶段／输出基线及来源映射规则；不得以不同ABI的StateHash直接判定回归。

  当前状态：**待验证**。已有trace f169da25c67742aaafa0e9860ae4a230及旧Proof 52d757e59fb841f9968feba2eab66461；该Proof的baseline_available=false、比较帧数为0，不是本轮回归通过。

## 2. 角色与技能基础合同

- [x] 2.1 定义C#显式StateMachine／State／Transition及角色控制模块合同，覆盖Enter／Tick／Exit、来源／目标、纯条件、优先级和稳定顺序、输入／结果、参数、typed状态schema与代码版本；通过既有登记／组合校验交付唯一模块目录。

  当前状态：**已完成**。cbcd7fa42、c2d9a203d及规则装配提交已提供显式State/Transition、只读条件端口、稳定选择顺序、typed控制状态、版本和唯一CharacterControlModuleCatalog。合同/目录/组合校验代码已核对，Runtime编译及当前正式Frontend构建已有成功证据；移动行为比较仍属于5.1和13.2。
- [ ] 2.2 定义SkillDefinition、入口签名、ActionProfile引用、子图依赖及允许的后续候选，交付相同策略被多个技能引用时仍可精确选中技能的作者／校验结果。

  当前状态：**部分完成**。a47532948、ceb50eb9e已接入SkillDefinition、ActionProfile引用和精确SkillId/入口目录；参数化子图签名、依赖及允许的后续候选尚未完整交付。
- [ ] 2.3 明确ActionInstance与SkillExecutionState的唯一owner关系，交付Context、模板、实例、调用点和generation的typed地址及生命周期合同，删除第二生命周期候选设计。

  当前状态：**部分完成**。d90da602b、6db20c5f2、ce72111f2已使ActionInstance拥有技能局部状态，并修正实例/generation与窗口来源；独立子图调用frame和全部调用点地址仍缺，不能用一次技能实例frame代替它们。
- [ ] 2.4 定义角色运行包中的控制binding、SkillProgram目录、组合布局与显式容量，交付缺失模块、非法依赖、并发／容量不符的正式诊断。

  当前状态：**部分完成**。控制binding、技能目录、typed布局及共享执行aggregate已接入；显式并发容量、容量错误与完整非法依赖诊断仍未闭合。

## 3. 语义与Target编译

- [ ] 3.1 将角色组合Discovery改为读取控制合同和技能闭包，迁移原Character／Equipment graph roots；通过正式Frontend报告验证根目录唯一且旧角色root不再生成。

  当前状态：**部分完成**。ceb50eb9e已将Corin Discovery接到C#控制合同与技能记录；旧Character/Equipment作者入口和编译注册尚未全部删除，正式根目录唯一性报告未交付。
- [ ] 3.2 按设计12的职责迁移表，从CharacterSemanticEmitter迁出技能节点业务族及变量／装备／GE绑定发射，复用唯一操作目录与typed端口；CharacterSimulationProgramBuilder仅保留通用IR写入／索引／一致性约束。交付模块输入输出、实际调用链、中央分支删除清单及UI能力／Emitter／Target支持集一致结果；Timeline发射按已分配的owner接口接入。

  当前状态：**部分完成**。b419fcdb9→770ecfc51、3e1355410已迁出Blackboard声明/状态/作用域、领域绑定和技能目录；51aed878f→be440e2f5迁出控制合同；adc29ea30迁出Timeline编排并通过窄回调复用Graph拓扑。705f01d82进一步把Action/Behavior目录、标签条件及动作状态槽发射移入CharacterSemanticActionCatalogEmitter；`bf1289be3`统一Node/Asset来源工厂；`ae28b8998`、`147556f2f`、`0c416ac24 → a23eb2ae8`、`9a036e8ad`分别迁出Equipment、Input、Tag/Attribute及全局状态目录。`475cfa9d0 → 51b3a07c0`将节点登记和目录绑定继续按领域拆开，所有模块仍写入共享CatalogIndex与Builder，中央业务分支已删除。各模块进入实际Editor构建，局部对照确认语义保留；UI/Emitter/Target能力一致性报告、其余作者目录和最终发布证据尚未完整交付。
- [ ] 3.3 接入子图输入／输出签名、调用点及occurrence绑定，交付完整引用链与类型校验；子图递归被拒绝，显式Loop及跨技能候选分别验证。

  当前状态：**已实现，待验证**。`1b80885e3`接入Graph signature、occurrence CallFrame、IR/两Target codec及类型绑定；`c26a15721`建立SubGraph动态输入/输出值端口并将未连接输入转为正式默认常量，`0a4bc76b3`接通编译布局；递归拒绝沿既有Discovery的`graph_cycle`路径保留。显式Loop、跨技能候选及运行行为仍待Validator/Replay核对。
- [ ] 3.4 迁移Tree／Timeline／TreeClip／局部状态机发射和状态声明，保持Decision／Commit及停止顺序；通过正式IR Inspector和source map核对对应关系。

  当前状态：**部分完成**。既有Tree/Timeline/TreeClip/局部状态机继续经过同一IR，6dcdfd82a等已迁移精确实例来源；adc29ea30将Timeline编排与TreeClip生命周期发射从中央类迁出，`6f7216303`又将边、状态生命周期编排交给独立GraphFlow emitter，代码对照确认入口/启用/停用/销毁顺序、状态owner及EmissionSession.Complete顺序保留；`bdedba175`将状态机运行、状态切换和状态停止迁出OperationControlRuntime。局部状态机调用参数、状态声明及IR Inspector完整核对仍未完成；本步未改公共Timeline运行或另建发射注册表，公共部分继续按独立owner接口对齐。
- [ ] 3.5 将C#控制参数／state合同、GE／Equipment／Body Motion描述及技能目录纳入同一IR／角色运行包，交付canonical identity与依赖闭包结果。

  当前状态：**部分完成**。ceb50eb9e及后续目录发射提交已写入控制合同、参数/state、Body Motion和技能目录；51aed878f→be440e2f5将控制描述与来源发射集中到独立模块，`05a7471ae`、`d344670ac`又把控制配置解析和默认参数收进正式Contract/IR链。`705f01d82`之后Action、GE、Equipment、Input、Tag/Attribute和全局状态目录继续共用同一IR；Action与GE的Behavior字段来自同一实现，节点绑定也经共享目录引用模块进入同一Builder。当前正式重建的canonical SemanticHash保持fe5bbbd7c89aec250a7d0eabbd24ea7392bbd964a6b9bb3ce8e19e059428e969，Float32/Fixed身份已直接核对；最新拆分后的正式产物及GE/Equipment完整依赖闭包报告仍未交付。
- [ ] 3.6 分别完成Float32与Fixed降低、组合state layout、SkillProgram与codec升级，交付同语义双Target构建和旧ABI拒绝结果；列明两端保留差异的数值／存储／编码原因，控制与Action业务流程必须调用共享实现，不能复制后要求同步维护。

  当前状态：**部分完成**。两Target的技能状态、Graph CallFrame、动态Value端口和codec已有实现；`00c47f3c`、`31ac3821`完成同一IR的Float32/Fixed构建和产物身份核对，`1b80885e3`、`c26a15721`、`0a4bc76b3`继续保持两端同一调用合同。旧ABI拒绝及完整Target能力一致性结果尚未收口。
- [ ] 3.7 更新既有.csir／.csim store、wrapper和原子发布组，交付精确重读及混版拒绝结果；Projection只迁移技能producer来源，保持当前Pose实现。

  当前状态：**部分完成**。00c47f3c改为一次正式Publish同时生成两Target和Projection，31ac3821已提交本轮同组产物并核对实际引用；完整store重读、混版拒绝及发布失败边界的结果仍待整理，不能将有效组构建成功扩大为所有失败条件已验证。

## 4. 技能解释器与实例状态

- [ ] 4.1 将现有控制解释器的正式使用范围收至技能，从OperationControlRuntime按组合控制、局部状态机、执行范围生命周期迁出职责，保留唯一分派／状态／调度入口；迁移调用者并删除角色RootTree调度及重复启停分支，交付模块输入输出、实际调用链和删除证据，不以partial或转发壳替代拆分。

  当前状态：**部分完成**。C#控制/技能入口及实例作用域已迁移；`bdedba175`将局部状态机运行从OperationControlRuntime移入OperationStateMachineRuntime，`f56974a63`又将激活、停止、强制释放和执行范围完成移入OperationExecutionLifecycleRuntime，`603a5951e`进一步拆出组合节点执行，`cc1eb16f6`和`0a4bc76b3`接入SubGraph调用入口及调用帧。OperationControlRuntime保留组合遍历与唯一公开Cursor/分派入口。角色RootTree调度、重复启停分支及旧作者入口仍未清理。
- [ ] 4.2 接入ActionInstance拥有的节点、Timeline、局部状态机和等待状态，交付跨Tick字段清单及既有状态coverage校验结果。

  当前状态：**已实现，待验证**。d90da602b→6db20c5f2已接入ActionSkillExecutionFrame/Aggregate/Manager及两端state/layout/codec；跨Tick覆盖清单和现有coverage/运行证据尚未完整交付。
- [ ] 4.3 实现子图按值入参、声明返回值和中止不提交输出的调用frame，交付多个调用点复用同一子图时的独立地址与运行诊断。

  当前状态：**已实现，待验证**。`1b80885e3`建立调用点、输入/输出绑定、IR/Target codec与拓扑索引；`c26a15721`将SubGraph值端口按调用frame签名解析，并编译未连接输入默认值；`0a4bc76b3`在两Target的控制Tick前写入输入，成功后读取输出，失败/停止时以默认值隔离旧输出；`6b94d16f`在重复激活前重置输出状态。多个调用点通过occurrence route拥有独立state slot，完整运行诊断和中止/并发覆盖仍待验证。
- [ ] 4.4 接入合法并发释放、重复激活与调用generation，交付模板共享、实例隔离和容量失败的现有Runtime／Validator结果，不增加对象clone。

  当前状态：**部分完成**。6dcdfd82a、304d83880、6db20c5f2已加强重复激活与实例/generation隔离；`1b80885e3`及后续调用帧按occurrence保持状态地址隔离，重复激活时输出状态也会重置。当前仍有单active Action查找约束，合法并发、容量边界及完整运行结果尚未闭合。
- [ ] 4.5 迁移技能变量与Frame投影，角色控制字段通过只读事实暴露；交付已删除Character Blackboard输入镜像、无跨实例写入的引用及布局检查。

  当前状态：**部分完成**。技能局部slot已禁止缺实例时回落全局状态，ce72111f2修正Frame窗口发布来源；角色输入镜像清理、变量作用域完整迁移及全量布局检查未完成。
- [ ] 4.6 接通父级Complete／Cancel／Interrupt／Reject／Abort／teardown对全部子图与Timeline的停止，交付graceful进度、force释放和重复停止的生命周期事实。

  当前状态：**部分完成**。6db20c5f2已统一Action生命周期并将停止处理放入精确实例作用域；全部父/子图/Timeline的graceful、force、重复停止和teardown尚缺完整运行证明。
- [ ] 4.7 对技能内Motion、GE、Equipment与Presentation叶子收敛唯一请求／输出端口，交付无Transform、播放器、WorldSolver或网络旁路调用的定向检查。

  当前状态：**部分完成**。现有Motion/GE/Equipment请求端口继续复用，控制输出通过正式端口进入同一Step；所有领域叶子的迁移清单、依赖/旁路定向检查尚未完整交付。

## 5. C#角色控制迁移

- [ ] 5.1 将有效角色的Gameplay Locomotion迁入C#显式State／Transition，保留原输入、数值与同Tick转换顺序；通过既有业务观察比较核对控制状态、Body／Intent时序，不复制Presentation Pose State。

  当前状态：**已实现，待验证**。Corin显式Locomotion模块及两Target适配已有提交，控制代码仍在原Evaluate内执行；同输入数值、Body/Intent与同Tick转换顺序尚未完成回放比较。
- [ ] 5.2 将动作候选、输入消费、连段与取消迁为控制代码中的独立技能请求规则，允许State保持active时请求技能；删除外层动作图及角色级连招状态机，不新增角色总状态机或C#技能阶段镜像，交付输入到精确Skill请求的诊断链。

  当前状态：**部分完成**。C#控制已按SkillId组织输入、连段/取消请求，运行不新增角色总状态机；旧外层动作图、菜单和作者引用尚未完全删除，完整来源诊断仍待验证。
- [ ] 5.3 将FixedActionRuntime／Float32ActionRuntime中的准入、来源检查、replacement／stop barrier、输入消费、请求暂存、最终提交及生命周期转换收敛到一份共享业务实现，Target只保留必要状态／数值适配；复用唯一Required Tag、TargetRequirement及目标快照规则。交付两端调用链、重复分支删除清单，以及纯查询／最终提交、既有同Tick顺序和实例身份的验证结果。

  当前状态：**已实现，待验证**。6db20c5f2已共用Activation/Commit/Lifecycle及实例管理，ce72111f2补齐窗口来源发布；两端重复业务分支删除和小步静态审查已有证据。纯查询/最终提交、同Tick顺序与实例身份仍需本轮运行结果闭合。
- [ ] 5.4 接入显式replacement及source stop barrier，区分独立并发请求；交付来源、退出原因、停止进度和新实例建立顺序的事实。

  当前状态：**部分完成**。显式replacement与source stop barrier已有代码，停止scope已修正；独立并发请求、退出原因和新实例建立顺序尚无完整现有运行验证。
- [ ] 5.5 使当前Decision窗口在同Tick角色决策前可读，迁移原连段／取消规则；通过既有Replay业务事件核对不额外延后一渲染帧。

  当前状态：**待验证**。Evaluate保持Decision窗口先于C#控制，ce72111f2保存窗口来源至最终发布；尚无当前候选的Replay事件比较，不能据代码顺序直接认定行为一致。
- [ ] 5.6 保持AIIntentProgram与CharacterSimulationInput边界，更新只读输入合同引用；交付AI不访问控制／技能私有状态的依赖与Validator结果，不修复TrainingEnemy资产。

  当前状态：**部分完成**。当前AI仍通过正式Character输入接入，未接管AI运行；AI插件替换由其独立任务负责，本项输入合同、私有状态访问和组合规范仍需最终对账。

## 6. 装备核心接入

- [ ] 6.1 将Feature Persistent／Route角色图入口迁成代码binding与Skill引用，更新作者目录及IR；交付稳定Slot／Route／Feature／参数身份和旧Host opcode零引用结果。

  当前状态：**尚未完成**。Equipment Persistent/Route/Host作者和编译分支仍存在；当前只保留并修正其正式catalog绑定，没有完成向代码binding/Skill引用的整套迁移。
- [ ] 6.2 保留装备Begin／Commit／Cancel事务、Tag／Effect贡献及Feature generation，将控制状态接入同一typed布局；交付既有事务和上下文合同的验证结果。

  当前状态：**部分完成**。既有Equipment事务、贡献和上下文保留，控制typed状态已新增；两者完整整合及现有事务验证未交付，不把“没有改坏旧逻辑”计为本项完成。
- [ ] 6.3 更新Action Equipment Context、参数查找和已有效装备数据的调用者，交付精确Skill绑定与snapshot覆盖；不补做装备样例或网络装备业务。

  当前状态：**尚未完成**。Skill与Action的基本身份已接入，但有效Equipment路由到精确Skill、参数和snapshot闭包尚未完整迁移；仍不补装备样例或网络装备业务。

## 7. Session、状态与网络恢复

- [ ] 7.1 在原Evaluate／WorldResolve／Finalize Step内接入控制模块和技能解释器，交付原四阶段、多Tick与Commit入口的调用图及Pipeline编译结果。

  当前状态：**已实现，待验证**。c2d9a203d及后续提交已在同一Evaluate依次执行C#控制和技能，再走原WorldResolve/Finalize；两者共用Actor/Tick transaction。完整Pipeline编译及当前候选运行证据尚待交付。
- [ ] 7.2 扩展两个Target的状态transaction、copy、codec和hash，覆盖控制State identity、必要进入Tick／转换进度／输入缓存、ActionInstance、子图frame、参数、Timeline及停止进度；恢复直接还原数据，不重放Enter／Exit或技能请求，交付完整状态schema与旧版本拒绝结果。

  当前状态：**部分完成**。121ec4a49、6dcdfd82a、304d83880、d90da602b、6db20c5f2已扩展两端codec/hash与控制/Action/技能frame身份；`1b80885e3`已加入Graph CallFrame codec/hash，`c26a15721`和`0a4bc76b3`接通动态端口与调用状态；完整状态覆盖及旧版本拒绝结果仍缺。
- [ ] 7.3 更新Composition、ProgramCatalog和模块装配校验，交付缺模块、混版、能力不足及不兼容Target在Active前失败的正式报告。

  当前状态：**部分完成**。模块目录、绑定版本和Program/State基础组合检查已落地；本轮产物混版已通过统一Publish修复。缺模块、混版、能力不足和Target不兼容的完整正式拒绝报告仍待交付。
- [ ] 7.4 更新ServerAuthoritative owner checkpoint、Full／Delta与Correction恢复，交付完整技能状态恢复及原Remote观察体边界的现有证明。

  当前状态：**已实现，待验证**。121ec4a49及来源身份提交已更新owner checkpoint、Full/Delta和恢复读取边界；尚无当前技能实例完整恢复和Remote边界的本轮Proof。
- [ ] 7.5 更新Fixed Rollback snapshot、history、分层hash与恢复投影，交付同输入重算中实例／调用状态一致的现有Proof；Relay继续只路由。

  当前状态：**已实现，待验证**。Fixed snapshot/codec/history身份路径已随新状态接线；本轮同输入重算与实例/调用状态一致性尚未获得Replay/Proof。
- [ ] 7.6 更新EventId来源、output disposition与state publish衔接，交付确认／替换／抑制及重复输出的既有诊断，保证代码来源不伪造Graph节点。

  当前状态：**部分完成**。代码/技能来源已使用typed Source，ActionWindow最终Fact/Trace发布不再晚读局部帧；确认/替换/抑制、重复输出与所有消费者还需完整运行对账。

## 8. 规则与数据发布

- [ ] 8.1 将共享合同、稳定解释器与可更新控制／技能规则按设计分程序集，交付单向依赖及portable规则不引用Unity／Fantasy对象的编译结果。

  当前状态：**部分完成**。ceb50eb9e、0cdb043dd已建立portable Control.Rules程序集及主要消费者引用；稳定解释器与可更新技能叶子规则的完整程序集/版本边界尚未全部交付。
- [ ] 8.2 将规则程序集接入现有HybridCLR构建、依赖、裁剪／泛型生成与启动加载，交付精确模块版本和现有资源发布闭包；不新装热更框架。

  当前状态：**尚未完成**。尚无本轮规则程序集进入HybridCLR构建、裁剪/泛型生成和启动加载的完整提交与验证证据。
- [ ] 8.3 更新Unity客户端／Authority与普通.NET Authority产品的规则模块和技能产物发布清单，交付相同语义版本、完整依赖及缺失模块拒绝结果；Relay产品不安装Gameplay执行。

  当前状态：**尚未完成**。已有部分Unity/Fixed/DotRecast规则装配；各正式产品的模块、技能产物和发布manifest依赖尚未统一交付。
- [ ] 8.4 接通新Session采用新代码／技能版本和活动Session版本锁定，交付正式manifest及加载状态报告，不增加对局中状态迁移、旧ABI读取或兼容开关。

  当前状态：**尚未完成**。既有Session版本锁定基础继续保留；新代码/技能发布后由新Session采用的完整加载与manifest结果尚未交付。

## 9. 技能作者模块与共享框架

- [ ] 9.1 将技能定义、Flow／局部状态机、Timeline／TreeClip、变量／参数和领域叶子的作者规则从中央类迁出，交付各模块输入输出、唯一Capability装配、实际消费调用链和中央字段／节点特例删除清单；UI、Document与Compiler复用相同局部语义，不把重复规则平移进helper。

  当前状态：**尚未完成**。编译发射器拆分不等于作者规则拆分；中央Capability、窗口及领域作者规则尚未完成本项迁移和重复特例删除。
- [ ] 9.2 将节点创建、配置、复制粘贴和端口变化统一接入现有Port Shape与typed Mutation，交付作者目录／Validator一致结果并删除重复字段表。

  当前状态：**尚未完成**。SubTree运行编译已复用既有PropertyPort/PropertyEdge身份并接入动态值合同；技能创建、配置、复制和作者侧动态端口的统一Mutation迁移及重复字段表清理尚未交付。
- [ ] 9.3 增加技能定义、签名与inline／shared子图编辑，交付从定义到Tree／Timeline／调用点的精确owner导航与原正式转换命令。

  当前状态：**尚未完成**。尚无技能签名、inline/shared子图工作面及精确owner导航的完整实施交付。
- [ ] 9.4 将角色入口改为代码控制binding／参数及技能目录，删除角色图创建菜单和无效页面；交付无假RootTree及无任意代码调用节点的能力清单。

  当前状态：**部分完成**。Definition已有控制模块与技能定义字段；旧角色图菜单/页面和相关能力仍未全部删除，不能据配置字段存在判定作者入口已完成。
- [ ] 9.5 保留AI／Pose共享画布及独立领域数据，实现窗口重载恢复、字段草稿保护和单次订阅释放；交付现有窗口状态／生命周期诊断，不在OnInspectorGUI执行重计算。

  当前状态：**尚未完成**。既有AI/Pose共享作者框架保留；窗口重载、草稿保护及订阅生命周期这一轮改进尚无实施/验证交付。

## 10. Document v5整包闭合

实施归属：任务 10.1–10.6 由规划窗口 `01a07206-ec83-74e3-866a-7ccb6a158217` 与唯一同目录实现窗口 `01a0720a-6105-72b1-bf2f-bbfeb6654773` 独立负责；原 BTSMTL 实现不再修改本节，最终集成与全链 Replay 仍由原 BTSMTL 实施负责。

- [x] 10.1 定义v5控制配置与skill definition分片、精确允许文件族及local身份规则，交付schema与只读context／生成数据边界文档。

  当前状态：**已完成实现，待13.2整链复核**。`AgentDocumentControlConfiguration`、控制参数、Skill definition、Graph/Timeline完整分片和local canonical目录已落地；`SKILL.md`与current contract已同步v5，代码实现明确只读context与generated边界。
- [x] 10.2 更新Exporter与strict Codec／Mapper，按控制配置、技能、Graph、Timeline及Presentation内容分责；AgentAuthoringPackageMapper及Package Codec只保留整包协调和跨分片引用。交付模块输入输出、中央字段分支删除清单、canonical往返、整包hash和未知／旧字段拒绝的现有校验结果，保持Presentation原owner。

  当前状态：**已完成实现，待13.2整链复核**。Exporter按技能、控制配置和技能可达Graph/Timeline选集；strict codec拒绝未知字段、注释、非有限数值、旧schema与未登记文件；package hash固定由规范路径和内容hash计算。Presentation仍由原Presentation owner负责。
- [ ] 10.3 将AgentDocumentReconciler／Planner中的领域diff、依赖计划和Mutation lowering迁入对应内容模块，中央服务只协调完整有序计划与跨分片引用；新技能、子图、Timeline和控制binding仍共用同一事务。交付模块调用链、原中央业务分支删除清单及dry-run依赖／删除顺序报告，不新增分片apply入口。

  当前状态：**部分完成**。Skill diff/export与Control configuration diff/handler已迁出并注册到同一MutationHandlers；Graph/Timeline既有lowering仍由中央Reconciler协调，新增通用Graph创建尚未形成正式Mutation闭包，故不宣称本项闭合。
- [ ] 10.4 接入所有新owner的同一Undo、保存、失败恢复和reverse export，交付任一分片失败不发布半包的既有事务结果。

  当前状态：**部分完成，待整链验证**。控制与Skill owner已进入同一Store staging、Undo、rollback与reverse export路径；真实apply失败回滚仍需在Unity编译恢复后复验，不能用源码静态检查替代。
- [x] 10.5 删除v4及更早reader／writer／manifest分支和角色RootTree正文入口，沿用两个domain与五生命周期工具；交付旧包明确拒绝、重新checkout生成v5的结果。

  当前状态：**已完成实现，待13.2整链复核**。AgentAuthoring下V4文件、V4类型名与正文入口已删除；strict reader对旧schema明确报unsupported，Character checkout只保留技能可达Graph/Timeline正文，RootTree路径仅在context保存。
- [x] 10.6 更新btsmtl-agent-authoring技能、MCP合同描述和实际代码地图，交付路径／字段可解析且与唯一v5实现一致的检查结果，不新增局部写工具。

  当前状态：**已完成实现，待最终对账**。技能说明、current contract、MCP五个独立工具描述和代码地图已同步；`status`只作为异步轮询动作，不是第六个工具，也未增加局部写入口。

## 11. 技能工作区与诊断

- [ ] 11.1 将Action Workspace统一到SkillDefinition、ActionProfile及调用点上下文，支持Tree-only和多个／嵌套Timeline；交付不猜唯一Timeline的typed页面状态。

  当前状态：**尚未完成**。Action Workspace尚未完成围绕SkillDefinition、调用点及多/嵌套Timeline的完整页面状态迁移。
- [ ] 11.2 打通技能到AnimationClip、producer、Profile和AnimationSlot的原owner导航，交付无镜像字段或第二动画资源配置的引用检查。

  当前状态：**尚未完成**。动画/Pose原owner保持；从新技能目录到资源、producer与Slot的正式导航尚未完整接通。
- [ ] 11.3 扩展source map和Trace区分C#模块／State／Transition来源、技能模板、ActionInstance、调用点和generation，交付控制转换与技能激活可分别追溯、同模板多实例隔离的诊断输出，不伪造角色图节点。

  当前状态：**部分完成**。运行来源、SkillId、实例/调用generation及ActionWindow来源已有改动；控制转换、技能激活与多实例在全部诊断入口上的精确展示尚未收口。
- [ ] 11.4 更新IR Inspector、Live Debug和窗口Follow／Pin绑定，交付控制合同及技能执行根可查看、过期目标不选其他实例的状态报告。

  当前状态：**部分完成**。新控制/技能记录已进入IR，现有Inspector基础可读取目录；Live Debug、Follow/Pin精确实例绑定与过期目标行为尚未完成本轮迁移。
- [ ] 11.5 向独立场景预览提供精确技能选择、正式请求及只读实例接口，交付双方接口对账；不创建场景、SkillPreviewRuntime或重复实现旧播放器删除。

  当前状态：**部分完成**。c2d9a203d等已提供正式控制/技能请求和读取合同；与独立预览当前候选的全部选择/观察接口及组合说明仍待交接对账，预览功能本身不计入本项实施。

## 12. 资产迁移与旧路径清理

- [ ] 12.1 通过现有正式作者事务转换全部选定有效Character根及其技能依赖，交付角色代码／技能映射、仍有效的稳定业务identity与完整引用报告；Graph／Node kind变化时创建新identity并替换引用，不能原地改kind，受保护无效资产继续明确报错。

  当前状态：**部分完成**。ceb50eb9e已迁移Corin Definition及技能配置；所有选定有效根的可达闭包、稳定引用与旧角色图资产清理尚未完整交付。
- [ ] 12.2 转换有效装备入口、输入／变量绑定及有限producer来源，交付新控制／技能目录可构建结果，保持既有Motion曲线、Warp及表现资源内容。

  当前状态：**部分完成**。Corin移动/动作来源已有接线并保留原Motion/Pose资源；有效装备入口及全部输入/变量/producer引用迁移尚未闭合。
- [x] 12.3 显式构建并发布所选Target、技能目录和同组Projection，更新现有Launcher／Variant／Profile引用；交付exact artifact与产品引用一致报告。

  当前状态：**已完成**。00c47f3c将Gameplay Lab接入一次正式Publish，31ac3821提交两Target Program、Projection及场景/prefab产物；两Program的ProgramId/SourceRevision/SemanticHash相同，Variant仍精确引用对应wrapper与共享Projection，Launcher校验已由实施报告完成。be440e2f5、adc29ea30及705f01d82后分别正式重建，Program身份保持一致，最新三个根Prefab及GameplayLab场景生成更新见6ca8e09a7。该条是构建/发布/引用闭合，运行行为仍由13.2验证。
- [ ] 12.4 删除已替代角色控制图入口、activation／Equipment Host编译注册、旧schema、菜单、字段、别名及废弃文件，交付定向零引用与仍保留AI／Pose／独立预览依赖的业务清单。

  当前状态：**部分完成**。已删除部分旧控制代码位置、重复Action流程及中央发射分支；旧角色图、Equipment Host注册、v4 schema、菜单和别名尚未全量清除。
- [ ] 12.5 按设计12逐项核对最终目录、类型和公开命名，交付原职责→正式模块→输入输出→调用者→已删除旧实现的代码地图；确认没有重复Action业务流程、中央领域特例、partial拆分、转发壳、万能Context、临时桥接、双运行入口或兼容配置，不能只以新增类数或行数降低收口。

  当前状态：**部分完成**。公共Action运行模块及Blackboard、DomainBinding、SkillProgram、ControlModule、Timeline、ActionCatalog发射模块已形成实际职责迁移，Behavior字段规则由Action与GE共用；对应旧分支已删除，字段、SourceMap及生命周期顺序经局部对照保留。`CharacterSemanticSourceFactory`已消除Timeline/中央Emitter的NodeSource和Action/CatalogCompiler的AssetSource重复构造；`475cfa9d0`之后节点登记与`51b3a07c0`之后目录绑定也已按领域拆分。组合控制、作者中央类、Document及最终目录/命名地图仍待收口。

## 13. 集成证据与规范收口

- [ ] 13.1 运行现有portable／Editor／产品构建和依赖检查，交付实际构建结果；dotnet／msbuild使用禁用build server参数并立即shutdown，本机Unity CLI按明确项目路径退出且保留主验收Editor，CI禁令不变。

  当前状态：**部分完成**。按要求执行的`ThirdPersonClient.Editor.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false`最近一次完整构建受共享工作区`ActionAnimationAuthoringWorkspaceResolver.cs(29/31)`错误阻断（缺少`ResolveTimelineCandidates`，且`ResolveTimeline`调用缺少`failures`参数）；Agent范围使用`--no-dependencies`定向构建为0警告/0错误并立即执行`dotnet build-server shutdown`。此前完整Editor构建曾为0错误，新增源码静态检查已通过；正式Rebuild、产品构建和Unity编译恢复后的完整receipt仍待交付。
- [ ] 13.2 使用已有Validator、Document生命周期与Replay／Proof覆盖完整新链，交付同版本重复性及跨实现输入／Body／动作阶段／输出比较；不编写新测试、不忽略缺帧或运行错误、不把ProgramHash变化当作行为通过或失败。

  当前状态：**部分完成，待整链复核**。明确实例`e852139597e42532`上的v5 Character checkout成功（root `c7a7c1e3f7e64d81b5a04a90cbeb8d4e`，`editableHash=07390110c2c6173f43f0f92f208cc558e428a84069c44a0ea434900ef0917930`，`contextHash=05a7444d544becb03c6e3431ef214d277b8d49060c8c24336f14e5e7349e2994`，`documentHash=b04cd5547d92a9b0709fc343a89fe03f2fe8f785463813416868f62c385e2f51`）与formal validate成功；dry-run尚受Unity实例未加载最新Reconciler及外部`generated-diagnostic-sampling`编译错误影响，Replay/Proof未开始。
- [ ] 13.3 安装本change的delta并同步当前项目口径、Purpose及关联接口说明，交付现行规范与预览／其它change不存在相反共享要求的对账；保留独立预览和受保护任务范围。

  当前状态：**尚未完成**。尚未安装本change全部delta和更新最终项目口径；AI/Timeline/预览/Pose等并行规范仍需按实际采用版本完成组合对账。
- [ ] 13.4 执行严格OpenSpec校验与限定改动diff检查，按完整迁移单元形成中文小步提交；分别交付设计12的结构迁移证据和构建／Replay行为证据，附文件跳转、删除清单及剩余明确错误。仍有重复业务流程或未迁出的中央职责时保持对应任务未完成，不以编译通过、类行数或文件数宣称整个重构完成。

  当前状态：**部分完成**。V4路径零引用、v5工具数量、strict文件族和定向构建结果已完成静态收口；本实现窗口已形成中文小步提交`84812507b`、`346e4024d`和`5538b80fc`，但共享工作区仍有Action Workspace编译错误、Unity最新assembly reload与dry-run复验缺口，保持整项未完成。

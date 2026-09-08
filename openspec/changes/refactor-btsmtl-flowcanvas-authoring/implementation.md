# BTSMTL FlowCanvas实施记录

## 当前进度快照

本节反映当前状态；后文按小步实施顺序记录，早期的待迁移描述不代表当前代码仍停留在该阶段。

- 已提交：5f700e6b8接通Definition.SkillGraphs到原生技能编译；da820d615接通Timeline／TreeClip及表现调用点；2e0bd1e9a接通原生黑板声明快照、作用域和读写节点。
- 尚未完成：完整公开能力对账、编辑器全部写入的统一Mutation、实例入口装配、Macro完整运行调用定位、真实边与端口值采集、Document v6及最终资产迁移。tasks.md中的对应集成任务保持未勾选。
- 原生窗口查询已接入发现阶段：逐次遍历Macro、状态、条件和TreeClip调用，按实际祖先声明owner匹配Decision阶段投射。共享定义的不同调用分别校验；缺少窗口类型或只有不可见／其他阶段投射时返回明确诊断和候选路径。该检查确认静态阶段与作用域合法性，不代替运行中窗口实际开放的记录。
- 原生LocomotionInputMotion节点通过ILocomotionInputMotionAuthoring共享运动参数读取合同，复用原有速度／曲线校验和Program发射；黑板InputBinding限定为正式输入目录支持的ActionTargetSnapshot及Character／Spawn作用域。Document能力目录接入仍未完成。
- Unity实例恢复后已再次请求脚本刷新，等待结果时插件会话断开，随后Console读取返回HTTP 503。本批代码尚无成功编译结果，之前的零CS错误记录只适用于此前提交；新增脚本的meta已由Unity生成。
- Document v6由作者指定的「agent工具」任务负责，范围和正式合同直接记在design.md第6节、tasks.md第5节。没有新增独立交接文档；未经作者明确允许，不向其他窗口发送消息。
- 资产迁移、Document apply、内容Build和回放均未因本次整理而执行。

## 实施基线与归属

最新补充授权：允许Unity刷新和脚本编译检查，只修本次BTSMTL错误，不处理其他领域报错；不触发内容Build、整根Validator或回放。以下较早的禁止脚本编译记录由本条取代。

执行顺序：先完成代码、编辑器及实例观察，资产迁移最后执行。当前不触发内容Build、整根校验及回放。接入入口必须从实例工作区定位，不恢复`Character Definition -> Open Root Tree`；精确Definition路径只保留为已有资源关联及历史检查定位。

- 目录：`D:/Unity_Project_1/3C`；开始本轮apply时HEAD：`9c447aa4b5f2ddbc7d114476bccb59910d785e6c`。
- 工作区已有未提交资产、Pose、相机和插件变更，不把整个工作区视为干净基线，不覆盖这些改动。
- Center：`BTSMTL技能FlowCanvas作者与编译接入`，`change_id=2d129d09136d4ac3b77312b9e43d42f8`。
- 精确根：`Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`。
- 原`refactor-btsmtl-authoring-architecture`继续负责角色C#控制、技能业务、状态及生命周期；本文负责技能FlowCanvas作者入口、直接编译和观察。
- 当前观察按最新设计消费普通Unity Play中的真实Actor／ActionInstance，不以前述独立Scene Play方案为前置条件，也不创建预览运行控制器。
- PoseGraph及原生runtime方案独立管理，不迁其资产、执行器或算法。

## 已核对的代码依赖

| 业务 | 现有代码 | 迁移处理 |
|---|---|---|
| 技能定义及入口 | CharacterSkillAuthoringDefinition.EntryGraphAuthoringId | 需接正式FlowCanvas技能闭包，不增加第二入口fallback |
| 技能发现 | CharacterSkillCompilationDiscovery | 当前从CharacterAuthoringGraphOccurrence中找入口及调用；必须解耦旧图遍历 |
| 图与调用发现数据 | CharacterAuthoringSourceCompilationModel | Graph、Node、GraphReference和TreeClip记录直接持有旧类型，不能简单包FlowNode后继续用旧对象树 |
| 业务语义发射 | CharacterSimulationCore/Action/Input等NodeEmitterRegistration | 保留操作含义和参数，迁移读取业务payload的部分 |
| Program写入 | CharacterSimulationProgramBuilder | 保留唯一常量、operation及端口binding写入机制 |
| 技能入口发射 | CharacterSemanticSkillProgramEmitter | 保留技能目录、follow-up、输入和动作关系 |
| 状态与运行 | ActionSkillExecutionRuntime | 保留ActionInstance、predictionKey、generation和调用状态合同 |

这不是全能力清单完成声明；任务1.2仍须结合正式资产闭包完成节点与端口逐项盘点。

## 第一小步：隔离操作写入与旧节点读取

`CharacterSimulationNodeEmitterContext`继续从当前合法旧作者节点读取已校验的未连接输入，随后将明确的来源、值、类型、端口和操作描述交给`CharacterSimulationOperationEmitter`。后者只调用唯一Program Builder，不接收BaseGraph、BaseNode或PropertyPort，也不创建作者图。

输入为来源位置、操作描述及有序常量输入；输出为OperationHandle及同一Builder中的常量／端口binding。常量声明顺序仍为输入默认值在前、节点附加常量在后，operation及binding发布顺序保持原实现。

这是后续FlowCanvas直接编译入口的正式内部边界，已由现有调用者使用，不是旧图转换器。但FlowCanvas图闭包、领域节点、Macro、Document v6和原生观察尚未接入，不能关闭4.1或其他整项任务。

当前改动不改变Document字段、identity、ownership、Reconciler或Validator；Agent合同仍为实际v5，不能只因提案目标为v6就修改运行schema。

## 验证事实

## 原生作者基础、结构页面与编辑接口

本阶段新增的正式类型位于`Runtime/Character/Control/Authoring/FlowGraphs`。技能、状态机、状态内容和条件页面使用FlowGraph；参数化子图使用原生Macro子类，调用与接口锚点使用原生MacroNodeWrapper／MacroInputNode／MacroOutputNode，没有生成旧BaseGraph或BaseNode镜像。

已实现的结构节点包括技能入口、顺序、选择、并行、循环、成功、状态机、状态、进入／任意／退出状态、OnEnter／OnExit、条件返回、状态主体完成及退出原因。输入读取和动作上下文、窗口、准入、生命周期提交、移动朝向夹角提供正式FlowNode字段与端口。AND／OR／NOT及浮点／整数六种比较使用原生SimplexNodeWrapper，编译目录只描述原生端口到现有操作端口的映射。

组合节点每个步骤拥有独立稳定Flow输出。步骤编辑器支持名称、条件页、次序、转换优先级和中断策略；换序不改变identity，连接中的端口拒绝直接删除。系统节点限制到对应页面，不能单独删除。创建状态机或状态的原生菜单操作会在所属技能根资产内创建私有内容页并初始化入口；本阶段只编写该代码，尚未在业务资产上执行。

创建节点、创建连接、删除节点／连接、断开整个端口和步骤修改已进入统一编辑事务；子操作复用外层Undo，失败回滚。图闭包检查拒绝跨领域引用、递归、重复图／节点／边身份和连接容量错误。技能行为／值连接拒绝直接成环，状态机转换允许返回之前状态。人工批量复制、共享Macro接口编辑涉及的全部caller owner、私有闭包删除／回收及Document整包事务尚未完成，不据此关闭2.3、2.4或3.1。

原生Macro增加外部执行合同：技能Macro收集端口只创建端口声明，不写入共享Macro的entry／exit运行委托表；不允许原生运行克隆，也不在读取缺失入口时自动修复作者资产。其他Macro保持原运行行为。

`BtsmtlSkillFlowLeafEmitter`读取真实FlowNode的字段和serializedValue，经原生端口映射与现有OperationValuePortContract核对后进入唯一CharacterSimulationOperationEmitter。它不调用getter。完整技能发现、嵌套调用状态布局、Timeline及黑板作用域遍历仍未替换旧读取链，不能宣布直接编译接入完成。

原生编辑框架新增只读IGraphEditorObservation接口。节点高亮、连接状态和端口文字可读取外部缓存；连接绘制在外部观察时不读取graph.isRunning作为前置条件，也不执行原生Blink回调。只读且禁止原生执行的父子图允许在普通Play时下钻。该接口尚未绑定正式技能实例观察器，未声明7.1或7.2完整完成。

后续代码进展：新增BtsmtlSkillFlowObservation，明确接收正式图、作者hash和RuntimeInstanceKey，复用RuntimeDebugViewBinding订阅现有Graph／StateMachine诊断。节点显示只读取进入／完成／停止事件，RuntimeNodeExecutionObservation不使用日志级别推测成功或失败。观察缓存仅保留当前图的节点与边；版本不匹配清除显示，切换页面、关闭原生窗口或退出Play释放订阅。原生正式技能入口尚未装配该适配器，端口无采集记录时明确显示缺失，不调用getter补值。

普通停止／强制停止保留明确文字，不伪装成原生Failure；原生节点内容区展示阶段和记录Tick。原生颜色及成功／失败图标继续使用原生绘制。最新一次脚本刷新后Console返回0条错误；本批没有运行交互、内容Build、回放或资产迁移。提交一度被遗留空锁阻塞；确认该锁自18:57未变化、没有写入进程及持有句柄后，已备份到`.git/index.lock.stale-btsmtl-20260908-193632`并恢复正常提交，没有终止其他进程。

## 本批小步提交

普通黑板接续：原生Variable是名称／类型／默认值的唯一存储，BtsmtlSkillBlackboardDeclaration仅按变量ID记录作用域、生命周期、分类及输入／窗口绑定。Variable增加直接读取已存值的接口，不调用绑定getter；技能变量禁止原生属性／数据绑定。原生Get／Set端口覆盖布尔、整数、数值、Identity、Vector2和Vector3，Config写入在编译时拒绝。

CharacterAuthoringBlackboardDeclaration现捕获与作者图类型无关的声明快照；旧作者数据和原生变量分别读取自身正式数据，不构造旧节点或旧图。原生编译进入同一个CharacterSemanticBlackboardEmitter的图作用域栈，参数化调用的普通局部变量按出现路径分配。替换中发现并修正了3条本领域CS类型错误；后续筛选error CS返回0。窗口查询的Decision／Commit可用性检查仍需同步到原生节点，编辑器和Document写入口也尚未收口。

Timeline代码接续：技能Timeline只引用TimelineAsset，私有内容由根子资产持有，避免原生节点JSON内嵌数据没有真实Unity属性路径。TreeClip增加唯一资产图来源及领域无关ITimelineTreeGraphAsset接口，设置后清空旧树字段；原生TimelineBody保留Root和启用／停用／销毁入口。技能图闭包纳入Timeline内的原生图及跨根私有引用检查。

BtsmtlSkillTimelineCompiler复用TimelineSemanticEmitter，TreeClip回调进入同一个BtsmtlSkillGraphCompiler；技能Timeline也加入CharacterAuthoringCompilationModel.Timelines和表现侧调用点收集。版本捕获移入Editor专用作者模块，供编译和Timeline内容依赖共用。脚本检查按error CS筛选无错误；未运行精确根Build、回放、TreeClip交互或资产迁移，4.3整体仍保留未完成。

最新决定及入口接续：用户确认节点＋Timeline是技能主链，单入口Macro提供多个值参数，父节点等待主体完成后再读取输出与结束状态。8542cf521固定接口与端口ID合同，45beb5032补原生黑板读取和类型核对，9d7b3b765实现原生图递归编译、Macro参数槽、GraphCallFrame及输出写入阶段。

正式发现现读取Definition的Editor专用SkillGraphs引用，SkillDefinition仍使用稳定入口ID；CharacterSemanticEmitter已将技能交给BtsmtlSkillGraphCompiler，不再从旧树出现位置中寻找或编译技能。作者图类型及根引用不进入Player编译。Character编译器版本更新为26，独立Timeline版本更新为2，以区分新发射合同。存量资产未迁移，缺少原生根时报告错误而不回到旧路径。普通黑板局部声明、Timeline／TreeClip、Document v6及正式窗口接入仍未完成。

当前验证：脚本刷新后按`error CS`筛选返回0条；Console另有Pose工作区的Graph.UpdateNodeBBFields空引用、StateMachine details未绑定及GUI布局异常，调用栈属于CharacterPoseCanvasBinding／CharacterPoseGraphWorkspace，按范围未处理。没有运行内容Build、回放或资产迁移。

接续实施：BtsmtlSkillGraphOccurrence只引用实际FlowNode／BinderConnection，为每个Macro、状态机和状态内容引用建立不同的调用路径，条件页面沿连线路径定位。BtsmtlSkillGraphFingerprint使用独立GraphSource包装原生节点做版本捕获，递归纳入明确资产引用；不调用会重排作者节点的Graph.Serialize。原生序列化仅捕获输入常量缓存，不执行Flow／Value委托，不保存资产。正式技能发现尚未改用该记录。

运行层状态归属从作者Graph ID匹配改为显式State operation到StateMachine operation的ProgramReference。这样相同作者图出现在不同调用路径时，不再因Graph ID重复被拒绝。现有Character和独立Timeline树编译器都发射该引用；不保留旧Graph ID查找作为fallback。旧编译产物缺少关系时会明确拒绝，最终资产阶段通过正式Build更新，不在当前阶段偷偷补建。

| 提交 | 独立内容 |
|---|---|
| 00761c2aa | 原生图领域规则、Macro外部执行及观察绘制接口 |
| ebc41bfcc | 技能作者图、状态页面、步骤编辑与闭包检查模块 |
| ee557a353 | 输入／动作节点及已有Program操作发射合同 |
| 87f16e650 | 浮点／定点技能根诊断generation |
| 01f254500 | 节点生命周期读取及原生观察适配器 |

共享编辑器文件只提交本任务的接口与绘制改动；其他任务的页面导航、连线下钻和Pose改动保留。后续按独立功能块及时提交，不再累计到完整迁移结束。以上提交不表示OpenSpec整项全部完成，正式图发现、Macro调用编译、完整实例定位、Document v6和最终迁移仍按任务表收口。

浮点和定点SimulationTraceRecord新增SkillExecutionGeneration。现有Action trace作用域携带技能入口operation；写trace时从当前技能实例的根激活状态读取generation，退出作用域恢复之前的入口。该值与原操作Activation.Generation分别保留，不替换Gameplay状态或凭操作编号推测释放代次。两条诊断adapter均发布该字段；RuntimeDiagnosticsStore的payload差异判定同步包含技能身份、动作实例、调用点及两个generation。完整Macro调用范围仍须由编译来源补齐。

诊断链仍有需要完成的身份工作：CharacterSimulationDiagnosticsAdapter当前以操作编号填充SkillExecution的CallSiteId，Payload.Status保存的是trace severity。不能直接把它当作整次子图调用身份或节点运行状态；需按编译调用范围及正式完成／中断事件补齐之后再接高亮，避免一个节点一次执行冒充整个技能实例。

## 实例观察入口接入中的第二小步

诊断事件打开来源现在传递完整RuntimeDebugEventView，依据事件中的CharacterRuntimeId查询既有Registry，核对Session、ProgramRevision及SourceMap handle与source一致，再定位该Host关联的作者资源。删除扫描全部Definition／Timeline的运行事件入口；静态Semantic IR检查器仍使用它显式拥有的Definition，不把它当成运行实例入口。

作者上下文通过通用IRuntimeDebugInstanceContext携带RuntimeInstanceKey，当前图观察面读取该身份并Pin。修正首次Pin后的刷新丢失绑定：Pinned目标不受场景选择自动替换，不匹配时显示PinnedTargetNotAttached并保留原实例。Follow遇到多个执行实例时要求明确选择，不取列表第一项。

目前底层来源资源解析仍读取现有作者存储，不能据此宣布FlowCanvas技能图已迁移；后续正式图和编译入口完成后替换该资源解析，不能再增加旧图镜像。Timeline独立页面、Macro调用导航及完整原生图观察仍待接入，7.2等整项不勾选。

本小步只做调用者搜索和diff静态检查；按用户要求未编译、未Build、未运行验证。运行状态及来源导航不改变Document可写字段或实际v5协议。

- 正式RunHost基线编译返回`WorkspaceEditorInUse`，没有RunId；保留主验收Editor，没有另建临时运行器。
- 同实例`e852139597e42532`完成脚本refresh／重载后，Console返回0条错误。重载期间CLI断开后原实例已恢复。
- 尚未生成同输入前后回放比较，不能用脚本编译证明语义和性能完全一致。
- 正式Validate job `4c215d1c6c344cf7a6b480324e81dcb9`已结束：success=true、compileSuccessCount=1、semanticValidCount=1，未apply或保存资产。sourceRevision=`5f692b49fd460f45732b1fb707a33afda75c4493c89bafeff7eeabe532577479`。用户要求停止编译后不再启动同类检查。
- 未新增测试代码，未修改Unity YAML，未调用局部资产修复工具。

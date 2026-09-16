本清单自 `parallel-20260914-domain-01-planning-update` 起只记录核心实现和公共集成。Timeline唯一实施清单在 `../restyle-timeline-editor-slate-style/tasks.md` 第12组；Pose唯一实施清单在 `../refine-pose-graph-readonly-blackboard/tasks.md` 第3组。各实现已获授权推进。用户本轮要求直接更新各自规划并通知对应实现，执行补充见design D22（DOMAIN-BOUNDARIES-20260914-03）；不通知规划窗口、不索取回执、不重复派工。原领域事项移出不表示已完成，不复制第二份领域清单。

D22在原条目内的交付范围：1.11一并收口技能局部接口、按需服务、角色每帧工作迁出与无WorldRequest候选，取消不得Abort角色；2.6包含单一角色Step工作状态及输入共享消费/恢复，保留已完成多Ability/角色Hash；2.1/3.2将按Actor的Pending批次改为角色结果，汇集实际多技能/领域候选并接原Backend；3.1由正式Session提供NumericProfile/TickRate，技能只校验匹配；3.8接Timeline实际Advance/Stop候选和恢复，4.7/6.4接真实Pose服务至Host。以上为现有事项的完整含义，不新增checkbox或验证任务。

2026-09-15主集成归属更新：`skill编译边界`实现窗口拥有D23定义的主集成装配。1.5／1.6、2.8、3.8、4.7／4.8仍是跨领域集成任务；Timeline、Pose和Skill各自域内实现清单不复制。主集成窗口负责装配和真实服务接线，不重写领域内部算法。
2026-09-14执行更新（检查至dd0708d46及工作区）：旧根类型错误已撤回，28个编译文件删除及后续清理保留。核心已有独立技能安装／服务、角色绑定、新CharacterRuntime port和两个数值目标状态codec；但CharacterRuntimeState仍以单个Ability安装／Hash为根，效果绑定还会拒绝不使用该能力的技能，按D19纠正。Timeline已有数值／资源准备、游标Advance候选与Commit／Discard，仍缺跨边界Clip业务和完整停止／恢复。Pose已有多种值节点、Constraint适配和原生Final入口，StateMachine仍缺具体Source实现，角色Host仍走旧Program／Projection。原已完成小步只代表当时交付，不能用作最新运行完成事实。

当前接线重点：核心1.10／2.1／2.6／6.4负责独立技能到角色入口；1.5／3.8消费Timeline真正可Advance／Commit／Discard／Restore的结果；4.7／6.1接Pose实际采样至最终姿态；3.1—3.6保留网络Pass职责并迁移领域状态。新增目录、布局、准备实例或节点注册均只记局部进展，未接完整不勾选。

D20补正原规划遗漏：Skill独立必须覆盖定义、编译、加载和执行，不仅是独立数据文件。技能不得创建角色事务或组装Control／Effect／Equipment模块；实际调用方提供技能声明所需的typed服务和调用输入，并拥有外层提交。角色只是一种调用方，1.11必须与角色接线同步收口，不能因1.10独立格式存在判定Skill已独立。

D21领域分工提报DOMAIN-BOUNDARIES-20260914-02已审阅，具体接线按D22：主清单保留核心任务，Timeline／Pose继续使用各自唯一清单，不新增任务或重分文件owner。按有实际依赖的业务切面接续，每批包含状态、真实算法和消费者，不设先搭完全部接口的阶段。1f26e07c0已交付角色多Ability聚合，效果安装也已按技能能力选择绑定；这些历史问题按剩余边界处理，不重复撤回正确工作。

执行顺序以用户2026-09-14“先大删除再做”为准：先执行下列首批删除，再完成独立技能、角色装配及其它保留业务的接线。旧消费者仍引用取消的类型不是延迟删除的条件；允许中间提交编译失败或功能明确不可用，记录错误所属消费者并接到正式接口，不加兼容层、占位类型、假结果或新旧开关。已完成小步保留，整个迁移必须等保留业务接通才算完成。Timeline／Pose在各自唯一清单执行同样顺序，不在这里复制领域任务。

## 0. 首批删除已取消职责

原8.2／8.4分别前置为0.1／0.2，后文不再重复列项；其它编号和已完成历史保持不变。共享文件中的有效技能／动画算法只做必要提取或解除旧载体类型依赖，不要求新Runtime完整可用后才删。1.6中的共享Timeline发射调用与旧转换Factory一起先退役，保留业务接线仍由原条目收口。

- [x] 0.1 先删除核心拥有的整角色Frontend／总Builder／BuildService、Program wrapper／旧codec／artifact、角色全局布局、失效缓存与共享旧构建入口，以及Program到Ability的转换Factory／FromProgram装配；同文件中仍需使用的技能编译算法和格式收窄为技能职责并正确命名。旧消费者的编译错误交由1.x／2.x／3.x／7.x接续，不为消错复活旧类型。Timeline／Pose专属旧链由对应任务先删，公共引用归核心处理。
- [ ] 0.2 先删除旧Projection总包、wrapper／reader／生成发布入口及Image／整角色身份字段；保留实际Rig／源资源／动作／Slot／Camera数据与算法，混合文件只提取有效职责。6.3／6.4／6.6随后接回正式领域绑定，不建立替代总包、兼容别名或新旧开关。

## 1. 独立技能数据与领域合同

Timeline原1.7的portable轨道／Clip数据以及原1.5—1.6的域内运行部分已交接至Timeline清单；下面1.5—1.6只保留核心调用与共享编译接线。

- [x] 1.1 将技能构建根迁为 GameplayAbilityDefinition，交付只包含其私有 Graph／FSM／条件／子图引用／Timeline 的发现模型与实际依赖修订。
- [x] 1.2 已交付Ability根的技能操作、调用帧、黑板、常量、来源与能力前端，当前仍落入旧Program容器；最终独立执行数据和角色级状态迁出由1.8、1.10接续。
- [x] 1.3 接入 Float32／Fixed 技能数值降低、唯一 codec 与 artifact store，交付按 Ability identity 保存和读取的正式产物。
- [x] 1.4 已交付Input／Effect／Equipment／CharacterState的typed Provider种类、owner identity声明及Load身份检查入口；真实成员／类型／版本绑定由1.9收口。
- [ ] 1.5 将Timeline owner的独立Prepare／CreatePlayback结果接入技能调用与非Skill调用装配；消费前确认数值目标、资源／成员和必要TreeClip服务已经实际匹配。现有准备实例不等同可推进播放，不能仅因IsReady或generation存在就报告完整可用；Advance／取消／Commit／Discard／Restore仍由Timeline交付并通过正式调用方接通。
- [x] 1.6 由主实现唯一修改BtsmtlSkillTimelineCompiler和共享技能调用入口，先删除其中Timeline轨道／Clip发射调用与专属适配，再接直接内容引用；保留TreeClip技能图编译与Step-scoped调用服务，不与Timeline任务共写该文件。

- [x] 1.8 移出Ability前端无条件声明的GameplayEffectAggregate、runtime:rng、runtime:handle-allocator、runtime:fact-sequence等角色级状态，由原正式领域owner唯一提供；技能仅声明局部执行状态和必要服务引用。
- [x] 1.9 将Provider绑定补为真实提供者合同解析，覆盖被引用成员的存在性、值类型、实际合同版本及运行句柄；服务由对应领域拥有、调用方提供，技能只绑定自己声明需要的成员。角色调用场景中修正InstallationSet给全部技能传同一Effect binding、Installation却拒绝未声明Effect技能的矛盾；不要求所有调用方拥有角色配置，必需服务缺失仍失败，不用空实现或全局启用能力绕过。
- [x] 1.10 将独立Ability数据的加载、格式、执行拓扑／布局接到Float32／Fixed实际执行与实例状态；复用已交付的自有拓扑和GameplayAbilityExecutionLayout，不能把创建布局视作执行完成。补齐读入到Tick／取消／恢复消费者，继续清除整角色Program解码／复制依赖，不恢复已撤回的FromProgram转换，保留必要技能编译与唯一技能格式。
- [x] 1.11 解除技能执行入口对角色装配的依赖：Float32／Fixed AbilityExecutionFrame只消费技能局部状态、调用输入／目标／时间和实际需要的typed服务，不强制接收CharacterRuntimeState、整角色Input／Body或在内部创建CharacterRuntimeStateTransaction；AbilityControlRuntime不再组装Control／Effect／Equipment领域模块，改调用外部正式服务。Frontend按可达节点声明能力，删除无条件GameplayEffect要求；依赖角色事实的节点只要求该事实服务。角色与TreeClip等调用方适配同一执行入口，禁止假角色、完整角色上下文包装或第二套技能执行器。

1.11执行边界补充：PendingAbilityEvaluation只承载技能局部候选及实际产生的领域请求，不强制附带CharacterWorldSolveRequest／角色事务，也不得在AbortUnconsumed内中止外层角色事务。BodyFacts等只读服务可以按需不提供，但实际读取该能力时必须拒绝缺失／无效数据，不接受默认结构的零值充当真实事实。

## 2. 角色领域运行与状态

- [ ] 2.1 按明确角色配置、已绑定独立技能集合和领域状态创建角色运行实例，接入正式Host及保留Pass的Evaluate／Finalize；关闭Host对旧Program／Projection的必要条件和Load调用。根类型错误接线已撤回，不恢复旧工厂或修改根校验；输入必须是真实领域绑定，输出必须是可推进的角色实例。
- [x] 2.2 将 ControlModule 参数、静态 Motion 描述和控制状态迁出 Program catalog／slots，保留 C# UnityHFSM 及全部已有走跑转身规则。
- [x] 2.3 消费曲线任务提供的RootMotionCurveAsset及Timeline唯一时间映射，将C# Control／Motion接到正式portable绑定，删除CharacterControlMotionCatalogEmitter依赖并保留MovingTurn和CameraRelative行为。
- [x] 2.4 将 BodyMotion 配置接到原数值目标运动模块，保留垂直积分、Motion 仲裁、WorldResolveBatch 和 Solver 能力要求。
- [x] 2.5 将 Effect／Equipment 的目录和运行状态交回对应模块，技能只保留请求接口，不复制全角色配置。
- [ ] 2.6 将Float32／Fixed CharacterRuntimeState与codec改为角色级领域聚合；控制／请求／效果／装备／序号归角色，多个技能按技能及调用实例分区，Timeline私有播放状态由Timeline提供，不挂在某一个Ability安装对象之下。以同一Step完整Capture／Restore控制机器、技能调用、目标及跨Tick运动状态，保留已有codec和算法的有效部分。

2.6执行边界补充：多Ability聚合已经建立，继续移出AbilityRuntimeState中的共享InputRequests／Consumed记录。相同请求由输入领域保存一份消费状态，多个技能查询／消费同一事实；技能自身等待和局部变量仍独立。动作准入／替换沿既有Action规则接入，不把窗口统一交Timeline保存或在Control重写技能规则。
- [x] 2.7 将Actor roster、角色内容identity与状态schema接到完整领域状态，使用真实角色配置／技能集合／模块合同身份；删除CharacterRuntimeState.GameplayContentHash直接取单个Ability ContentHash的映射，快照不能挑一个技能身份代表整角色，保留角色、技能分区、World／Pipeline各自身份与完整恢复。

- [ ] 2.8 由角色工厂装配Ability／Timeline／Pose／Camera／Motion的分型准备结果，汇集各领域owner确认的实际采用事实与失败原因；核心只发布自己拥有的Ability安装事实，不替其它领域决定版本或状态。

## 3. 网络Pipeline与产品接线

- [x] 3.1 将原Program Runtime安装项迁为Gameplay Runtime的数值与模块服务，保留五个显式组合维度和唯一会话装配职责；旧CompositionRequest／Preparation删除后按领域输入接回既有Pipeline／Backend，不要求复活旧类。SimulationPipelineCompiler的顺序、产品、能力校验和不可变计划继续保留，删除旧载体不能扩展为重写或取消网络体系。
- [ ] 3.2 将保留Pass的旧ProgramRuntime port改接领域执行服务，恢复Evaluate／Finalize和WorldResolveBatch调用；保留Ingress／Schedule／Step／Egress顺序、Product owner、能力校验及Backend原子Commit，不为修引用错误删除这些职责或另起旁路时钟。
- [x] 3.3 将 Authority baseline、Prediction History、Reconciler 和恢复事务迁到完整领域状态，保留 state/body 误差裁决、remote observed body 和 EventId journal。
- [x] 3.4 将 Fixed Rollback 的角色快照与内容 identity 接到新状态格式，保留输入排序、History、Hash、恢复／重放、确认输出和 Relay-only 职责。
- [ ] 3.5 更新握手与兼容 Pair，分别锁定控制／技能／配置内容、状态格式、NumericProfile、World／Solver 与 Pipeline／Backend 身份。
- [ ] 3.6 迁移 Unity Authority、普通 .NET Authority、Local／Fixed／Rollback 的显式 launch 和 manifest 读取；保留产品分工及原不支持能力的拒绝行为。
- [ ] 3.7 升级实际改变的网络／状态／产品格式并删除旧 reader，使旧角色 Program 产物不能被新会话隐式接受。

- [ ] 3.8 将Timeline owner提供的typed Capture／PrepareRestore／ApplyRestore接入完整角色／网络快照，并把Advance及停止候选纳入角色Step的统一Commit／Discard；移出技能Services／Layout中残留的TimelinePlayback／Loop／LogicTime私有状态前提，不在核心再定义cursor／loop／活动Clip状态。TreeClip图执行帧仍归技能，不能随Timeline状态一起双存。

## 4. Pose公共接入

Pose内部实现唯一清单由 `../refine-pose-graph-readonly-blackboard/tasks.md` 的Runtime接收章节维护。原4.1—4.6、5.1—5.8、7.3、8.3迁出本清单；已完成只读输入与现有正确算法不重开。主实现只做以下公共外壳接线：

- [ ] 4.7 将Pose owner已开始实现的Clip／BlendSpace／Selected Player及后续正式节点接入角色表现工厂与真实资源服务，沿Prepare／唯一Animancer Barrier／Evaluate／ValidatePending／Commit或Discard到唯一Final Publication；以实际Host调用和最终姿态消费完成公共接线，不能只调用Create或注册handler。Pose内部算法和缓冲仍归Pose任务，公共外壳退出Image依赖。
- [ ] 4.8 将Pose owner确认的实际GraphRevision／InstanceId／ResetGeneration及完成结果汇入角色观察接口；不在核心重建其状态或制造采用版本。

## 6. 输入、动作与资源装配

- [ ] 6.1 在角色表现外壳按既有顺序把EventGraph成功发布的typed Frame交给Pose owner接口，保持变量Set与只读消费的原owner，不修改Pose节点内部Get实现。
- [ ] 6.2 将技能／Timeline 的有限播放请求接到原 ActionPlayback／Slot 生命周期，保留实例、generation、退出淡出与 Gameplay 停止分离。
- [ ] 6.3 将 Projection 中仍有效的 Rig／动画源／动作／Slot／Camera 数据迁到正式领域配置、资源产品和只读实例绑定，取消对整角色 Hash 的要求。
- [ ] 6.4 将角色 Host 的启动依赖从 Program／Projection 改为领域运行与原生表现绑定，保留显式资源错误、角色隔离及 World owner。
- [ ] 6.5 保留 ACL、Motion Matching 与 Foot 数据的独立产品和加载入口，移除无关技能构建对其扫描和重建的依赖。

- [ ] 6.6 调用Camera任务的只读绑定准备／采用接口并迁出旧总Projection挂接，保留其字段、资源、求解与目标规则，不修改Camera Builder／payload／Timeline.Camera.cs。

## 7. 作者工具、预览与观察

- [ ] 7.1 将 Definition／Ability／Pose 的状态和操作入口按领域分离，取消整角色 Program／Projection 构建 UI，保持 Inspector 轻量读取。
- [ ] 7.2 向预览owner提供正式领域装配和观察入口，消费Timeline／Pose自己的运行与采用事实；不在主实现新增预览播放器、图Factory或改写ScenePlay协调器。
- [ ] 7.4 将技能 Inspector／普通 .NET Reader 改为独立 Ability 产物入口，保留精确来源导航和结构化值输入观察。
- [ ] 7.5 向预览任务交付领域公开操作及准备／采用／运行观察结果，显示请求版本与实际版本；ScenePlay协调器由预览owner接入，不保留Character Build／ProgramEpoch或假全局版本。
- [ ] 7.6 将核心技能／角色配置接口变化和精确依赖提供给C# authoring owner；Pose／Timeline定义与导出适配由各自既有owner接入，不覆盖正确生成实现。

- [ ] 7.7 汇集各领域owner正式发布的活动技能启动版本、Timeline播放身份、Pose重建generation和Camera／Motion采用事实，接回原Session重新准备规则，不由核心或预览推断成功。

## 8. 资产迁移与旧链清理

- [ ] 8.1 通过正式owner API迁移明确的Corin角色Definition、技能引用、场景和Variant公共接线；Pose图／Timeline内容资产由各自领域任务维护，保留已正确配置与ACL产品，不处理TrainingEnemy。
- [ ] 8.5 更新正式 Build／Run 产品引用和部署输入，使客户端与保留服务端消费对应版本的技能、模块配置和资源。
- [ ] 8.6 按 spec-audit 同步 current specs 与 project.md 的运行方向，并在原 owner 配合下收敛活跃提案的旧 Program／Image 假设，保留历史完成事实。
- [ ] 8.7 按领域小步形成中文提交和实现记录，记录每批实际迁移、删除范围及未接通合同，不混入其它窗口修改。

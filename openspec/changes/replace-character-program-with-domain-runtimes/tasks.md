本清单自 `parallel-20260914-domain-01-planning-update` 起只记录核心实现和公共集成。Timeline唯一实施清单在 `../restyle-timeline-editor-slate-style/tasks.md` 的Runtime接收章节；Pose唯一实施清单在 `../refine-pose-graph-readonly-blackboard/tasks.md` 的Runtime接收章节。原领域事项移出不表示已完成，迁移编号和接收边界见design D13—D15及spec-audit。接收规划负责续写自己的清单，本窗口不复制或改写它们。当前只更新规划，不启动／通知实现。

2026-09-14审查口径：保留1.1—1.3的前端／目标／存储小步与1.4的Provider身份检查成果；1.2和1.4的已完成描述按实际交付收窄，1.8已完成，1.9—1.10仍未完成。2.1重新标为未完成，现有Factory仅封装旧Program的Workspace／Evaluator，不等于最终领域工厂。2.4已完成，BodyMotion已由Definition运行绑定接入原数值目标运动链；2.5已完成，Effect／Equipment目录、aggregate状态和Equipment local-state已由对应领域持有。2.6及后续仍未完成。审查依据与代码链见design D12及spec-audit；implementation.md的历史记录由实现窗口维护。

## 1. 独立技能数据与领域合同

Timeline原1.7的portable轨道／Clip数据以及原1.5—1.6的域内运行部分已交接至Timeline清单；下面1.5—1.6只保留核心调用与共享编译接线。

- [x] 1.1 将技能构建根迁为 GameplayAbilityDefinition，交付只包含其私有 Graph／FSM／条件／子图引用／Timeline 的发现模型与实际依赖修订。
- [x] 1.2 已交付Ability根的技能操作、调用帧、黑板、常量、来源与能力前端，当前仍落入旧Program容器；最终独立执行数据和角色级状态迁出由1.8、1.10接续。
- [x] 1.3 接入 Float32／Fixed 技能数值降低、唯一 codec 与 artifact store，交付按 Ability identity 保存和读取的正式产物。
- [x] 1.4 已交付Input／Effect／Equipment／CharacterState的typed Provider种类、owner identity声明及Load身份检查入口；真实成员／类型／版本绑定由1.9接续。
- [ ] 1.5 将Timeline owner的独立PrepareContent／CreatePlayback结果接入技能调用与非Skill调用装配，绑定精确内容、资源、数值目标和调用上下文；直接内容Runtime与播放实现由Timeline任务交付。
- [ ] 1.6 由主实现唯一修改BtsmtlSkillTimelineCompiler和共享技能调用入口，移除其中Timeline轨道／Clip发射调用，改接直接内容引用；保留TreeClip技能图编译与Step-scoped调用服务，不与Timeline任务共写该文件。

- [x] 1.8 移出Ability前端无条件声明的GameplayEffectAggregate、runtime:rng、runtime:handle-allocator、runtime:fact-sequence等角色级状态，由原正式领域owner唯一提供；技能仅声明局部执行状态和必要服务引用。
- [ ] 1.9 将Provider绑定补为真实提供者合同解析，覆盖被引用成员的存在性、值类型、实际合同版本及运行句柄，拒绝同GUID下已删除或类型不符的依赖。
- [ ] 1.10 将AbilityDataAsset／FixedAbilityDataAsset的Load结果和消费接口迁为真正独立技能执行数据，删除对CharacterSimulationProgram／角色全局布局的返回和解码依赖。

## 2. 角色领域运行与状态

- [ ] 2.1 按明确角色配置、已绑定独立技能集合和领域状态创建角色运行实例，接入正式Host及现有Pass的Evaluate／Finalize；Factory不得继续以旧Program／ExecutionLayout创建旧Evaluator作为交付终点。
- [x] 2.2 将 ControlModule 参数、静态 Motion 描述和控制状态迁出 Program catalog／slots，保留 C# UnityHFSM 及全部已有走跑转身规则。
- [x] 2.3 消费曲线任务提供的RootMotionCurveAsset及Timeline唯一时间映射，将C# Control／Motion接到正式portable绑定，删除CharacterControlMotionCatalogEmitter依赖并保留MovingTurn和CameraRelative行为。
- [x] 2.4 将 BodyMotion 配置接到原数值目标运动模块，保留垂直积分、Motion 仲裁、WorldResolveBatch 和 Solver 能力要求。
- [x] 2.5 将 Effect／Equipment 的目录和运行状态交回对应模块，技能只保留请求接口，不复制全角色配置。
- [ ] 2.6 交付领域分区的 Capture／Restore 和统一角色 Step 事务，完整覆盖控制机器内部状态、请求、技能调用、目标、效果、装备与跨 Tick MotionWarp。
- [ ] 2.7 将 Actor roster、内容 identity 与状态 schema 接到完整领域状态，清除快照对整个 Character Program Layout 的依赖。

- [ ] 2.8 由角色工厂装配Ability／Timeline／Pose／Camera／Motion的分型准备结果，汇集各领域owner确认的实际采用事实与失败原因；核心只发布自己拥有的Ability安装事实，不替其它领域决定版本或状态。

## 3. 网络Pipeline与产品接线

- [ ] 3.1 将原 Program Runtime 安装项迁为 Gameplay Runtime 的数值与模块服务，保留五个显式组合维度和唯一 Composer。
- [ ] 3.2 迁移 Evaluate／Finalize Pass 的输入输出类型与调用，保留 WorldResolveBatch、四阶段顺序、Product owner、能力校验及原子 Commit。
- [ ] 3.3 将 Authority baseline、Prediction History、Reconciler 和恢复事务迁到完整领域状态，保留 state/body 误差裁决、remote observed body 和 EventId journal。
- [ ] 3.4 将 Fixed Rollback 的角色快照与内容 identity 接到新状态格式，保留输入排序、History、Hash、恢复／重放、确认输出和 Relay-only 职责。
- [ ] 3.5 更新握手与兼容 Pair，分别锁定控制／技能／配置内容、状态格式、NumericProfile、World／Solver 与 Pipeline／Backend 身份。
- [ ] 3.6 迁移 Unity Authority、普通 .NET Authority、Local／Fixed／Rollback 的显式 launch 和 manifest 读取；保留产品分工及原不支持能力的拒绝行为。
- [ ] 3.7 升级实际改变的网络／状态／产品格式并删除旧 reader，使旧角色 Program 产物不能被新会话隐式接受。

- [ ] 3.8 将Timeline owner提供的typed Capture／PrepareRestore／ApplyRestore接入完整角色／网络快照，并把其Pending结果纳入角色Step的统一Commit／Discard；不在核心重复定义cursor／loop／活动Clip私有状态。

## 4. Pose公共接入

Pose内部实现唯一清单由 `../refine-pose-graph-readonly-blackboard/tasks.md` 的Runtime接收章节维护。原4.1—4.6、5.1—5.8、7.3、8.3迁出本清单；已完成只读输入与现有正确算法不重开。主实现只做以下公共外壳接线：

- [ ] 4.7 调用Pose owner的Prepare／Create／PrepareDemand／Evaluate／Commit／Discard／Stop接口，接入角色表现工厂、唯一Animancer Barrier与原帧提交边界，不修改Pose内部Graph／Node／Buffer实现。
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
- [ ] 8.2 删除核心拥有的整角色Program生成器、wrapper、codec、控制catalog／全局布局和共享构建旧入口；Timeline／Pose专属旧链由对应任务删除，主实现只集成共享消费者退出。
- [ ] 8.4 删除旧 Projection 总包及其已迁移字段／reader／引用，不复制为另一个统一生成包，不留下兼容别名或新旧运行开关。
- [ ] 8.5 更新正式 Build／Run 产品引用和部署输入，使客户端与保留服务端消费对应版本的技能、模块配置和资源。
- [ ] 8.6 按 spec-audit 同步 current specs 与 project.md 的运行方向，并在原 owner 配合下收敛活跃提案的旧 Program／Image 假设，保留历史完成事实。
- [ ] 8.7 按领域小步形成中文提交和实现记录，记录每批实际迁移、删除范围及未接通合同，不混入其它窗口修改。

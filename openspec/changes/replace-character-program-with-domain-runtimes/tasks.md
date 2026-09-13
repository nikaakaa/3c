2026-09-14审查口径：保留1.1—1.3的前端／目标／存储小步与1.4的Provider身份检查成果；1.2和1.4的已完成描述按实际交付收窄，原完整要求由1.8—1.10接续。2.1重新标为未完成，现有Factory仅封装旧Program的Workspace／Evaluator，不等于最终领域工厂。审查依据与代码链见design D12及spec-audit；implementation.md的历史记录由实现窗口维护。

## 1. 独立技能数据与领域合同

- [x] 1.1 将技能构建根迁为 GameplayAbilityDefinition，交付只包含其私有 Graph／FSM／条件／子图引用／Timeline 的发现模型与实际依赖修订。
- [x] 1.2 已交付Ability根的技能操作、调用帧、黑板、常量、来源与能力前端，当前仍落入旧Program容器；最终独立执行数据和角色级状态迁出由1.8、1.10接续。
- [x] 1.3 接入 Float32／Fixed 技能数值降低、唯一 codec 与 artifact store，交付按 Ability identity 保存和读取的正式产物。
- [x] 1.4 已交付Input／Effect／Equipment／CharacterState的typed Provider种类、owner identity声明及Load身份检查入口；真实成员／类型／版本绑定由1.9接续。
- [ ] 1.5 将技能调用与独立Timeline产品统一接到直接内容调度入口，保留正式时钟、窗口、循环／Section、取消及TreeClip阶段。
- [ ] 1.6 删除Timeline轨道／Clip／MotionWarp到IR与operation的发射，技能只保存调用和内容引用，TreeClip图继续独立编译。
- [ ] 1.7 交付同一Timeline字段合同的portable内容导出、资源引用与Float32／Fixed绑定，使普通.NET直接运行而不回读Unity或生成临时程序。

- [ ] 1.8 移出Ability前端无条件声明的GameplayEffectAggregate、runtime:rng、runtime:handle-allocator、runtime:fact-sequence等角色级状态，由原正式领域owner唯一提供；技能仅声明局部执行状态和必要服务引用。
- [ ] 1.9 将Provider绑定补为真实提供者合同解析，覆盖被引用成员的存在性、值类型、实际合同版本及运行句柄，拒绝同GUID下已删除或类型不符的依赖。
- [ ] 1.10 将AbilityDataAsset／FixedAbilityDataAsset的Load结果和消费接口迁为真正独立技能执行数据，删除对CharacterSimulationProgram／角色全局布局的返回和解码依赖。

## 2. 角色领域运行与状态

- [ ] 2.1 按明确角色配置、已绑定独立技能集合和领域状态创建角色运行实例，接入正式Host及现有Pass的Evaluate／Finalize；Factory不得继续以旧Program／ExecutionLayout创建旧Evaluator作为交付终点。
- [x] 2.2 将 ControlModule 参数、静态 Motion 描述和控制状态迁出 Program catalog／slots，保留 C# UnityHFSM 及全部已有走跑转身规则。
- [x] 2.3 消费曲线任务提供的RootMotionCurveAsset及Timeline唯一时间映射，将C# Control／Motion接到正式portable绑定，删除CharacterControlMotionCatalogEmitter依赖并保留MovingTurn和CameraRelative行为。
- [ ] 2.4 将 BodyMotion 配置接到原数值目标运动模块，保留垂直积分、Motion 仲裁、WorldResolveBatch 和 Solver 能力要求。
- [ ] 2.5 将 Effect／Equipment 的目录和运行状态交回对应模块，技能只保留请求接口，不复制全角色配置。
- [ ] 2.6 交付领域分区的 Capture／Restore 和统一角色 Step 事务，完整覆盖控制机器内部状态、请求、技能调用、目标、效果、装备与跨 Tick MotionWarp。
- [ ] 2.7 将 Actor roster、内容 identity 与状态 schema 接到完整领域状态，清除快照对整个 Character Program Layout 的依赖。

- [ ] 2.8 交付D10的Ability／Pose／Camera／Motion分型准备请求、状态、缺失原因及实际采用结果，由角色领域实例工厂执行装配并发布真实版本。

## 3. 网络Pipeline与产品接线

- [ ] 3.1 将原 Program Runtime 安装项迁为 Gameplay Runtime 的数值与模块服务，保留五个显式组合维度和唯一 Composer。
- [ ] 3.2 迁移 Evaluate／Finalize Pass 的输入输出类型与调用，保留 WorldResolveBatch、四阶段顺序、Product owner、能力校验及原子 Commit。
- [ ] 3.3 将 Authority baseline、Prediction History、Reconciler 和恢复事务迁到完整领域状态，保留 state/body 误差裁决、remote observed body 和 EventId journal。
- [ ] 3.4 将 Fixed Rollback 的角色快照与内容 identity 接到新状态格式，保留输入排序、History、Hash、恢复／重放、确认输出和 Relay-only 职责。
- [ ] 3.5 更新握手与兼容 Pair，分别锁定控制／技能／配置内容、状态格式、NumericProfile、World／Solver 与 Pipeline／Backend 身份。
- [ ] 3.6 迁移 Unity Authority、普通 .NET Authority、Local／Fixed／Rollback 的显式 launch 和 manifest 读取；保留产品分工及原不支持能力的拒绝行为。
- [ ] 3.7 升级实际改变的网络／状态／产品格式并删除旧 reader，使旧角色 Program 产物不能被新会话隐式接受。

- [ ] 3.8 将直接Timeline的cursor、loop／section、活动Clip及TreeClip调用状态接入原网络快照和重放，不通过每Clip的Program状态槽恢复。

## 4. 原生Pose图与节点

- [ ] 4.1 将 Pose Graph 接入 FlowCanvas 原生运行初始化和 Manual 驱动，移除作者图初始化时的执行禁令并保留正式图 identity。
- [ ] 4.2 将 Pose Node／Connection 和端口从 Editor 占位实现迁为真实 typed 原生绑定，交付 Local／Component Pose、参数、Fact 和 Goal 端口。
- [ ] 4.3 接入每 actor 原生图实例及状态子图／Linked Pose 调用实例生命周期，使共享资产不保存运行状态。
- [ ] 4.4 接入节点按调用实例、求值身份和阶段缓存结果，保证共享分支不重复推进 Player、状态转换或求解。
- [ ] 4.5 迁移必要图校验到唯一正式入口，交付空间、递归、引用、唯一 Output、目标槽冲突和绑定容量错误定位。
- [ ] 4.6 让 UI、Clipboard、Mutation 与 C# authoring 使用同一节点字段和端口定义，移除仅为 Compiler 使用的第二份节点映射。

## 5. 原生Pose业务与表现帧

- [ ] 5.1 交付原生图的 source demand 准备和姿态求值入口，接入现有表现时钟、唯一 Animancer Barrier 和帧身份。
- [ ] 5.2 将 Player、PoseState、Slot、BlendStack 与惯性化状态接到原生节点，保留准入、时序、权重、relevance、历史和 capture／release 语义。
- [ ] 5.3 迁移 Phase 同步、state-local source 与 Linked Pose 绑定，保持正式资源身份、continuation 和 readiness 行为。
- [ ] 5.4 将采样与物理资源交接接回唯一 Source 模块，保留 ACL／Playable 生命周期，不引入 direct Play 或资源回退。
- [ ] 5.5 将 Foot／Goal 聚合／FBBIK 接到正式原生节点和唯一 Constraint 模块，保持现有算法、输入顺序、Goal 与初始化／Reset 结果。
- [ ] 5.6 接入节点复用缓冲与只读分支输入，移除全图 Value Lifetime／Workspace 计划依赖；节点内部 Native 算法继续由其原 owner 管理。
- [ ] 5.7 将原生 Output 接到唯一 Final Publication，保持整 Rig 检查、完整骨骼提交及 Barrier 前后失败边界。
- [ ] 5.8 完成原生实例 Reset／Replacement／Dispose，交付旧调用停止、在途工作完成、generation 失效与资源释放顺序。

## 6. 输入、动作与资源装配

- [ ] 6.1 将 Pose Get／条件／BlendSpace 等输入接到事件图唯一 Contract／Layout／Frame，保持 EventGraph Set 与 Pose 只读分工。
- [ ] 6.2 将技能／Timeline 的有限播放请求接到原 ActionPlayback／Slot 生命周期，保留实例、generation、退出淡出与 Gameplay 停止分离。
- [ ] 6.3 将 Projection 中仍有效的 Rig／动画源／动作／Slot／Camera 数据迁到正式领域配置、资源产品和只读实例绑定，取消对整角色 Hash 的要求。
- [ ] 6.4 将角色 Host 的启动依赖从 Program／Projection 改为领域运行与原生表现绑定，保留显式资源错误、角色隔离及 World owner。
- [ ] 6.5 保留 ACL、Motion Matching 与 Foot 数据的独立产品和加载入口，移除无关技能构建对其扫描和重建的依赖。

- [ ] 6.6 调用Camera任务的只读绑定准备／采用接口并迁出旧总Projection挂接，保留其字段、资源、求解与目标规则，不修改Camera Builder／payload／Timeline.Camera.cs。

## 7. 作者工具、预览与观察

- [ ] 7.1 将 Definition／Ability／Pose 的状态和操作入口按领域分离，取消整角色 Program／Projection 构建 UI，保持 Inspector 轻量读取。
- [ ] 7.2 将 Pose 预览与正式运行接到同一原生 Factory 和节点算法，Timeline 预览继续通过正式 Action adapter，移除旧 Image 预览路径。
- [ ] 7.3 将 Live Debug／Pose Watch 映射到原生图、节点、端口、调用实例和已完成结果，保留按需诊断和采样 owner。
- [ ] 7.4 将技能 Inspector／普通 .NET Reader 改为独立 Ability 产物入口，保留精确来源导航和结构化值输入观察。
- [ ] 7.5 向预览任务交付领域公开操作及准备／采用／运行观察结果，显示请求版本与实际版本；ScenePlay协调器由预览owner接入，不保留Character Build／ProgramEpoch或假全局版本。
- [ ] 7.6 迁移两个显式 C# authoring 入口的技能／Pose 输出与重建支持，保留业务 identity、端口、子图引用和作者布局。

- [ ] 7.7 发布活动技能固定启动内容、Pose重建重置历史、玩法内容／schema变更重新准备Session的实际采用结果，不由预览推断成功。

## 8. 资产迁移与旧链清理

- [ ] 8.1 通过正式 owner API 迁移明确的 Corin 角色、技能、Pose、场景和 Variant 引用，保留已正确配置与已有 ACL 产品，不处理 TrainingEnemy。
- [ ] 8.2 删除整角色 Program 的生成器、wrapper、codec、控制 catalog／全局状态布局及已无消费者的缓存入口，保留实际技能与独立 Timeline 实现。
- [ ] 8.3 删除 Pose IR、ProgramImage、Execution View、全图操作数组／调度／Worker 编排和专属 Compiler／产物入口，保留节点算法与必要资源管理。
- [ ] 8.4 删除旧 Projection 总包及其已迁移字段／reader／引用，不复制为另一个统一生成包，不留下兼容别名或新旧运行开关。
- [ ] 8.5 更新正式 Build／Run 产品引用和部署输入，使客户端与保留服务端消费对应版本的技能、模块配置和资源。
- [ ] 8.6 按 spec-audit 同步 current specs 与 project.md 的运行方向，并在原 owner 配合下收敛活跃提案的旧 Program／Image 假设，保留历史完成事实。
- [ ] 8.7 按领域小步形成中文提交和实现记录，记录每批实际迁移、删除范围及未接通合同，不混入其它窗口修改。

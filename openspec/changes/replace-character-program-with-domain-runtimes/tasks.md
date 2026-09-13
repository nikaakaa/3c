## 1. 独立技能数据与领域合同

- [x] 1.1 将技能构建根迁为 GameplayAbilityDefinition，交付只包含其私有 Graph／FSM／条件／子图引用／Timeline 的发现模型与实际依赖修订。
- [x] 1.2 将原 Character Builder 中的技能操作、调用帧、局部状态、常量、来源与能力要求迁为独立 Ability 数据接口，不携带角色控制或全角色目录。
- [x] 1.3 接入 Float32／Fixed 技能数值降低、唯一 codec 与 artifact store，交付按 Ability identity 保存和读取的正式产物。
- [ ] 1.4 将 Input／Effect／Equipment／角色状态等技能外部引用迁为 typed provider 合同，使缺失引用在角色绑定时明确失败。
- [ ] 1.5 保留既有独立 Timeline 内容处理和产品入口，令其复用唯一内容语义实现，不依赖被删除的 Character 根。

## 2. 角色领域运行与状态

- [ ] 2.1 交付角色领域运行实例工厂与 Evaluate／Finalize 接口，保持控制、技能、效果、运动和提交顺序，由现有 Pass 调用。
- [ ] 2.2 将 ControlModule 参数、静态 Motion 描述和控制状态迁出 Program catalog／slots，保留 C# UnityHFSM 及全部已有走跑转身规则。
- [ ] 2.3 接入独立控制 SourceCurve 资源与输入适配绑定，保留 MovingTurn 的位移／yaw、时间换算及 CameraRelative 输入行为。
- [ ] 2.4 将 BodyMotion 配置接到原数值目标运动模块，保留垂直积分、Motion 仲裁、WorldResolveBatch 和 Solver 能力要求。
- [ ] 2.5 将 Effect／Equipment 的目录和运行状态交回对应模块，技能只保留请求接口，不复制全角色配置。
- [ ] 2.6 交付领域分区的 Capture／Restore 和统一角色 Step 事务，完整覆盖控制机器内部状态、请求、技能调用、目标、效果、装备与跨 Tick MotionWarp。
- [ ] 2.7 将 Actor roster、内容 identity 与状态 schema 接到完整领域状态，清除快照对整个 Character Program Layout 的依赖。

## 3. 网络Pipeline与产品接线

- [ ] 3.1 将原 Program Runtime 安装项迁为 Gameplay Runtime 的数值与模块服务，保留五个显式组合维度和唯一 Composer。
- [ ] 3.2 迁移 Evaluate／Finalize Pass 的输入输出类型与调用，保留 WorldResolveBatch、四阶段顺序、Product owner、能力校验及原子 Commit。
- [ ] 3.3 将 Authority baseline、Prediction History、Reconciler 和恢复事务迁到完整领域状态，保留 state/body 误差裁决、remote observed body 和 EventId journal。
- [ ] 3.4 将 Fixed Rollback 的角色快照与内容 identity 接到新状态格式，保留输入排序、History、Hash、恢复／重放、确认输出和 Relay-only 职责。
- [ ] 3.5 更新握手与兼容 Pair，分别锁定控制／技能／配置内容、状态格式、NumericProfile、World／Solver 与 Pipeline／Backend 身份。
- [ ] 3.6 迁移 Unity Authority、普通 .NET Authority、Local／Fixed／Rollback 的显式 launch 和 manifest 读取；保留产品分工及原不支持能力的拒绝行为。
- [ ] 3.7 升级实际改变的网络／状态／产品格式并删除旧 reader，使旧角色 Program 产物不能被新会话隐式接受。

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

## 7. 作者工具、预览与观察

- [ ] 7.1 将 Definition／Ability／Pose 的状态和操作入口按领域分离，取消整角色 Program／Projection 构建 UI，保持 Inspector 轻量读取。
- [ ] 7.2 将 Pose 预览与正式运行接到同一原生 Factory 和节点算法，Timeline 预览继续通过正式 Action adapter，移除旧 Image 预览路径。
- [ ] 7.3 将 Live Debug／Pose Watch 映射到原生图、节点、端口、调用实例和已完成结果，保留按需诊断和采样 owner。
- [ ] 7.4 将技能 Inspector／普通 .NET Reader 改为独立 Ability 产物入口，保留精确来源导航和结构化值输入观察。
- [ ] 7.5 将 Scene Play 的版本、刷新、暂停／推进和观察接到领域内容及原生实例，交付明确的 Pose 实例重建／历史重置语义。
- [ ] 7.6 迁移两个显式 C# authoring 入口的技能／Pose 输出与重建支持，保留业务 identity、端口、子图引用和作者布局。

## 8. 资产迁移与旧链清理

- [ ] 8.1 通过正式 owner API 迁移明确的 Corin 角色、技能、Pose、场景和 Variant 引用，保留已正确配置与已有 ACL 产品，不处理 TrainingEnemy。
- [ ] 8.2 删除整角色 Program 的生成器、wrapper、codec、控制 catalog／全局状态布局及已无消费者的缓存入口，保留实际技能与独立 Timeline 实现。
- [ ] 8.3 删除 Pose IR、ProgramImage、Execution View、全图操作数组／调度／Worker 编排和专属 Compiler／产物入口，保留节点算法与必要资源管理。
- [ ] 8.4 删除旧 Projection 总包及其已迁移字段／reader／引用，不复制为另一个统一生成包，不留下兼容别名或新旧运行开关。
- [ ] 8.5 更新正式 Build／Run 产品引用和部署输入，使客户端与保留服务端消费对应版本的技能、模块配置和资源。
- [ ] 8.6 按 spec-audit 同步 current specs 与 project.md 的运行方向，并在原 owner 配合下收敛活跃提案的旧 Program／Image 假设，保留历史完成事实。
- [ ] 8.7 按领域小步形成中文提交和实现记录，记录每批实际迁移、删除范围及未接通合同，不混入其它窗口修改。

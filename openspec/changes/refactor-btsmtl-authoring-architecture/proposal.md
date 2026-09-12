## Why

技能原生FSM作者、Agent与直接编译接入由[专注作者变更](../integrate-native-fsm-skill-authoring/proposal.md)负责，通用运行观察由[观察收尾](../finish-skill-runtime-observation/proposal.md)负责；本变更继续负责角色控制、技能业务和状态拆分，不重复建立图接入路径。

BTSMTL 的目标已明确为重度技能编辑器：保留 Tree、Timeline、局部状态机和子图嵌套，以纯数据编译后的 Program 解释执行复杂技能。原提案只拆分作者工具大类、保持整个角色控制图和 ABI 不变，已经不能表达新的职责边界；本变更将角色级控制迁入 C#，同时保留并重整已有技能编译、ActionInstance、Session／Pass 与状态恢复基础。

## What Changes

- **BREAKING**：Character 的 Gameplay Locomotion 控制改由 C# 显式 StateMachine／State／Transition 承担；输入消费、技能选择与装备路由由控制代码组织，经唯一准入与动作事务处理。控制状态转换与技能激活是独立操作，不新增统管 Locomotion 与技能的角色总状态机，不在 C# 中镜像技能阶段。删除对应 RootTree／角色 StateMachine／Equipment Host 作者图编排和旧调用者；AI 继续只产生正式 Character 输入，Pose Graph 继续只负责表现。
- **BREAKING**：建立技能定义、只读 Skill Program 与实例执行状态的分工。技能定义拥有执行图并引用唯一 ActionProfile；Tree、Timeline、TreeClip、局部状态机、参数化子图保持一等作者能力。ActionInstance 继续是一次释放的唯一身份与生命周期，技能执行状态归属该实例，不新增第二个 Ability／Skill 生命周期。
- 保留“作者数据 → numeric-neutral Semantic IR → Numeric Target Program → C# 解释器”的正式链。角色运行包继续锁定 C# 控制模块合同、技能目录、策略与状态布局；技能节点不调用 Unity 对象解释器，不运行原始作者图，不新增第二条解释路径。
- Float32 与 Fixed 共用一份与数值无关的 Action 业务流程：准入、来源检查、显式 replacement、输入消费、请求暂存、最终提交与生命周期转换。Target 只提供实际需要区分的数值运算、typed 状态访问、布局和 codec；不能各自维护一份相同流程，再要求修改时同步两处。
- **BREAKING**：将完整角色状态明确分为控制状态、ActionInstance／技能执行状态以及既有 GE／Equipment 聚合；补齐子图调用、并发释放、重复激活和停止中的状态隔离，并同步 Float32／Fixed codec、hash、checkpoint、snapshot 和发布身份。旧 Program／State ABI 不继续读取。
- 保留 Session Source、Ingress／Schedule／Step／Egress Pass、Evaluate／WorldResolve／Finalize 和唯一 Commit 边界；角色代码与技能解释器共同进入既有 Step，不创建控制器私有 Update、网络 Tick、物理写入或状态恢复链。
- 按技能业务拆分作者规则、编译发射、运行叶子与编辑命令，继续使用共享 Graph Authoring Framework、唯一 Capability／Port Shape、整包 Document 事务和领域 Mutation。技能工作区提供定义、Tree／Timeline／子图导航及精确实例观察；保留原提案中窗口重载恢复和不打断字段编辑的行为改进。
- 将中央类拆分列为独立的结构完成条件：Action Runtime、Semantic Emitter、Program Builder、技能控制解释器和 Document Codec／Mapper／Reconciler 必须按 `design.md` 的职责迁移表收口。每项交付真实调用链、模块输入输出和已删除的旧分支；新增 helper、拆 partial、转发壳或编译通过不能代替业务职责迁移。
- **BREAKING**：Document 升级为唯一 v5，增加技能定义与 C# 控制配置的正式作者闭包，删除 Character 角色图正文入口；沿用 CharacterController／AIController 两个整包 domain 和五个生命周期工具，保留 Presentation 分片原有所有权，不提供 v4 兼容读写。
- 明确数据更新与代码更新：技能数据经正式 Build／资源发布采用；可更新的角色规则与技能叶子通过版本化规则程序集接入现有启动加载和服务端发布。Session 锁定模块和产物版本，不新增对局中无损换代码／换状态能力。
- 子图嵌套和 Tree 是本次范围。弹道、命中／伤害闭环、新 VFX／Audio consumer、通用蓝图与插件替换不在本次实施范围；以后新增能力必须进入现有模拟请求与状态所有权边界，不建立占位弹道 Pass。
- 场景预览仍属于独立的 `rebuild-btsmtl-preview-with-scene-play`。本变更提供技能作者／实例观察及运行接口，预览 change 负责场景启动、控制、试验重建、运行权限与旧预览播放器删除；两者只对账共享接口，不重复实施预览，也不要求先完成整份预览 change。

## Capabilities

### New Capabilities

- `character-control-runtime`：C# 显式控制状态机、State／Transition 合同、独立技能请求、完整状态恢复与版本化规则装配。
- `btsmtl-skill-program-runtime`：技能定义、纯数据编译与解释执行、嵌套子图参数、实例状态隔离、组合请求及生命周期闭合。
- `character-simulation-domain-events`：逻辑到表现的领域事件通道，首个事件 ActionSegmentChanged；与 presentation command 同 Step 事务、同 SimulationEventHeader 信封发布，rollback 随 tick 重放；表现层语义推进（段转移终结旧 playback 条目、推进 generation）由事件驱动，不新增进程内事件总线。

### Modified Capabilities

- `btsmtl-gameplay-semantic-ir`：角色控制改为代码合同，技能图成为执行语义根，装备入口不再是角色图 root。
- `btsmtl-compiled-simulation-program`：角色运行包组成、Skill Program、完整状态布局、版本与原子发布。
- `character-simulation-kernel`：统一 Step 内调用 C# 控制与技能解释器，保留事务、数值和世界边界。
- `character-action-instance-runtime`：允许技能定义拥有执行图，继续以唯一 ActionInstance 拥有释放和技能状态。
- `character-action-activation-flow`：角色代码通过唯一准入与动作事务启动技能，技能后续请求不旁路激活。
- `character-action-authoring-closure`：技能定义、ActionProfile、执行图、退出语义和输出工作面的作者闭合。
- `btsmtl-runnable-timeline-node`：Timeline 与 TreeClip 在技能实例内推进，保持 Decision／Commit 和停止顺序。
- `btsmtl-sm-node-authoring`：Gameplay Locomotion 状态机迁入 C#，删除角色外层动作状态机，图内状态机保留为技能局部流程。
- `character-pipeline-blackboard`：技能变量与 C# 控制状态分责，完整实例／调用作用域与输入只读投影。
- `character-equipment-feature-authoring`：Feature 提供控制模块配置与技能绑定，删除 Persistent／Route 角色图入口。
- `character-equipment-runtime`：装备路由由代码控制，保留唯一装备事务、GE 贡献及 Action Equipment Context。
- `gameplay-simulation-pipeline`：同一 Step 支持角色代码和技能执行，保持四阶段与输出提交合同。
- `gameplay-simulation-session-composition`：Active 前锁定控制模块、技能闭包与完整状态合同。
- `server-authoritative-prediction-correction-pipeline`：Owner baseline 和重算覆盖控制及技能实例状态，保持 Remote 观察体边界。
- `deterministic-rollback-network-model`：完整 Fixed snapshot、哈希与兼容校验覆盖代码合同和技能执行状态。
- `btsmtl-graph-core`：正式 Character 图作者范围变为技能，运行状态始终由编译后的实例存储拥有。
- `graph-authoring-domain-framework`：共享框架装配技能、AI 和 Pose 等隔离领域，代码控制配置不伪装为角色图。
- `graph-authoring-editor-shell`：窗口恢复、只读刷新和订阅生命周期继续保持明确边界。
- `btsmtl-agent-authoring-document-sync`：唯一 v5 技能／控制配置分片、严格对账和整包事务。
- `btsmtl-ai-controller-authoring`：复用共享作者基础，继续只绑定正式 Character 输入合同。
- `btsmtl-runtime-diagnostics`：区分 C# 控制来源、技能模板、子图调用点和具体 ActionInstance。
- `btsmtl-semantic-ir-inspection`：查看控制模块合同与技能执行根，保留精确来源及构建状态。
- `unity-simulation-assembly-ownership`：控制、技能、作者工具与可更新规则模块的单向依赖。
- `agent-ai-controller-synthesis`：统一Document v5，保持AI独立输入与状态边界。
- `agent-character-controller-synthesis`：Agent目标改为控制配置与技能内容，清理RootTree和旧输入镜像描述。
- `btsmtl-agent-authoring-mcp-bridge`：五个工具透传唯一v5整包，不增加局部或技能专用工具。
- `character-state-timeline-authoring-loop`：Corin角色图迁成代码控制和技能定义，保留既有移动／窗口／表现行为。
- `character-animation-presentation-authoring`：导航来源改为技能目录，Document版本对齐，保持表现owner。
- `character-presentation-pose-graph`：仅同步Document v5引用，保留当前Pose作者与执行语义。
- `character-pose-plan-compilation`：仅同步Document v5引用，不改变Pose编译或运行算法。

## Impact

- 运行与构建：`Main/Runtime/Simulation/Core` 的 Program、Kernel、状态及共享控制执行模块；Numeric Target、Character Definition／Composition、编译 Frontend／Target Build、产品发布与现有网络 checkpoint 接入。
- 作者与工具：BTSMTL Tree／Timeline、共享 Graph Framework、Agent Document、Capability、Exporter／Reconciler／Mutation／Validator 及 `btsmtl-agent-authoring` 合同。
- 实施分工：本change的Agent Document、Agent工具窗口、五个MCP工具及对应作者技能由独立Agent作者工具任务承接，包括任务10.1–10.6及相关旧路径删除、结构说明和Document验证。原BTSMTL实现继续普通编译／运行／作者模块重构、共享合同、最终产物发布与全链Replay；完整change仍需两侧交付及统一集成验收，具体文件和交付归属见design第10节。
- 数据迁移：正式可发布 Character composition 可达的角色控制图、技能图、装备入口和产物引用需要一次性迁移。保留业务稳定身份与作者引用；生成索引、ProgramHash、LayoutHash、EventId 来源映射及 ABI 可以随新结构变化，不要求跨 ABI 字节相同。
- 保护范围：不改动已正确的 KCC、MotionWarp 数学、Pose／IK／Camera／渲染算法，不顺带修复 TrainingEnemy 的现有资产阻塞，也不实现尚缺的装备样例、网络装备业务或战斗 consumer。已存在的能力保留正式迁移接口；无效资产继续明确报错。
- 规范对账：当前“技能资产不得拥有执行图”“角色动作只能由图 operation 激活”“角色／装备流程必须编译为图 root”“Document 固定 v4”等约束需要本 change 的 delta 替换。现行 Pipeline、世界求解和表现所有权继续保留。详细对账及独立预览／其它 active change 的重叠项见 `design.md`。
- 实施按完整迁移单元形成中文小步提交，结构验收与行为验收分别提供证据；current specs 安装与项目口径更新由任务 13.3 收口。验收复用已有构建、Validator 和 Replay／Proof；不新增测试代码，不把手动操作列为实施任务。代码已接通但仍保留重复业务流程或中央业务分支时，对应重构任务不得标为完成。

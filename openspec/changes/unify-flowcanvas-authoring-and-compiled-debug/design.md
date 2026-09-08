## Context

当前执行范围（2026-09-08最新指示）：只实施BTSMTL技能及其必要共用基础。以下涉及Pose编译执行、Pose资产迁移和Pose运行观察的设计暂不实施；Pose是否采用原生runtime须在恢复该领域工作前统一修订相关规范。共用代码改动必须保留当前Pose行为，不把共用基座作为暗中迁移Pose的入口。

动机见[proposal.md](proposal.md)。本设计记录2026-09-08最后确认的路线：FlowCanvas是作者图和编辑器基础，角色仍执行编译产物。此前讨论过的原生委托runtime不是本提案目标。

本地源码证据：`Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/`下的`FlowNode.cs`提供端口编辑；`Macros/Macro.cs`和`MacroNodeWrapper.cs`提供接口、引用、实例及下钻；`BinderConnection.cs`与`BinderConnection(T).cs`在真实端口调用后触发闪烁并缓存传递值。该原生运行观测依赖UNITY_EDITOR和Play Mode，不能直接观察项目的编译执行器。`FlowScript`已有手动更新，但本方案不运行它。原生`Sequence`是轮流输出的Flip Flop，不能替代技能等待完成的顺序语义。

当前技能链拥有ActionInstance、执行代次、调用状态和Numeric Target；Pose链拥有Program Image、阶段调度、帧页、Worker及唯一Final Publication。保留这些运行责任，只替换作者入口并补齐编译来源映射。

### 规范与在途变更对账

| 来源 | 实际要求或冲突 | 本提案处理 |
|---|---|---|
| `openspec/project.md`编译链、Pose约束 | Semantic IR／Numeric Program／Pose Program执行，禁止解释作者图 | 保留；安装时补充FlowCanvas作者入口，不删除运行合同 |
| `btsmtl-graph-core` | BaseGraph普通C#图、私有图必须inline、Tree窗口 | 技能／Pose退出该旧实现约束；未迁移领域保留；私有Macro采用根拥有的原生图子资产 |
| `graph-authoring-domain-framework` | 强制从旧UI原地抽取、禁止共享序列化图基类 | 技能／Pose共用FlowCanvas序列化及编辑基础，领域payload、校验和运行仍隔离 |
| `graph-authoring-editor-shell` | 唯一GraphView及旧Shell负责通用交互 | 技能／Pose改由原生GraphEditor负责；领域详情、导航、目录、底部观察区域保留 |
| `character-presentation-pose-graph`与runtime spec | Pose IR／Program Image、单次求值、Frame／Writer责任 | 保留并补充作者映射；不允许通过诊断旁路执行作者图 |
| Document current spec／project.md | 存在v3、v4描述；工作区已使用v5 | 明确这是历史版本漂移，迁移批次统一为v6及唯一读写；不在本提案期间覆盖current文本 |
| Pose原change Decision 25、22.x | 专用CanvasCore节点及端口适配 | 本提案实施后替代作者基础目标，保留已有正确业务及真实验证记录 |
| Pose原change 23.x／24.x | 独立原生runtime实验／正式原生求值迁移 | 与本提案正式路线互斥；实施前标为被本提案取代，不同时实施，不将实验标为生产验收 |
| `refactor-btsmtl-authoring-architecture` | 角色控制C#化、技能纯数据执行、ActionInstance唯一 | 继续由原change负责业务拆分；本提案拥有FlowCanvas作者与编译入口，不迁回角色总控制图 |
| `rebuild-btsmtl-preview-with-scene-play` | 唯一独立场景协调器、正式执行、无窗口播放器 | 复用其生命周期；本提案实现观测源绑定及显示。协调器未就绪时明确缺口，不能另建预览器 |
| AI替换、Timeline解耦、ACL、Foot／Camera changes | 各自领域方向及未完成任务 | 不扩大迁移到AI，不改变算法／资源／独立Timeline运行；共享入口适配时逐项对账 |

以上不是归档声明。实施开始时在原change加入精确替代引用和任务归属，发生实际源码冲突由作者决定；不会覆盖其他正确改动。当前空Clip节点清理未apply、布局Conflict及作者选择“保留清理前布局”属于待恢复事务，不算迁移证据。

## Goals / Non-Goals

**Goals:**

- 一份正式作者图、一条编译执行链、一份可追溯的编译来源映射。
- 原生端口、选择、创建、复制粘贴、撤销和Macro导航可用，领域规则在所有入口一致。
- 作者能够选择角色与技能释放实例，看到主图及子图中哪些节点执行过、正在等待或已中断。
- 正式Scene Play预览和游戏运行消费同版本产物及相同诊断合同。

**Non-Goals:**

- 不采用FlowCanvas委托／协程执行技能或Pose，不把原生节点库全部开放，不改现有动画、IK、伤害或移动算法。
- 不迁移AI，不新增网络协议、对局热换产物、Player远程调试、历史seek或指令级可恢复断点。
- 不保证每个原生Sub Flow／Macro行为都可编译；只开放有完整业务及编译合同的能力。

## Decisions

### 1. 作者数据直接进入正式编译前端

链路：`FlowCanvas图闭包 -> 领域只读遍历 -> 现有语义发射 -> 技能Numeric Program／Pose Program Image -> 原Session执行`。只读遍历是接口，不生成或保存旧BaseGraph、旧Pose图对象副本。共享前端负责身份、闭包与来源，领域编译模块负责动作、条件、Timeline和Pose语义。

业务取舍：原生runtime方案能使用更多现成节点，但需要重做可恢复技能状态及Pose求值责任；纯UI镜像方案迁移较少，却维护两份作者拓扑。本方案保留已有执行行为，代价是维护明确的领域编译合同，并不宣称复用原生runtime。

### 2. 原生图与领域语义分开

技能与Pose使用领域FlowGraph／FlowNode基础；正式连接使用BinderConnection的端口身份，编译从持久化连接读取，绝不通过getter取作者值。Capability是唯一kind、字段、端口及role声明；Graph类型、端口注册和Document目录都由同一声明消费。原生自动TypeConverter和按类型自动选首个端口在这些领域不启用；多个合法输入须明确选择。

技能条件图只允许纯求值，技能组合节点明确等待完成、失败、取消及并行结束语义。局部技能StateMachine与PoseStateMachine保留各自Entry／State／Alias／Transition及规则数据和语义；通过领域FlowGraph结构节点及显式转换表示，不能拿Macro或Flip Flop代替状态机。状态及规则页面复用同一GraphEditor，不保留第二份可写状态图。

原生编辑行为触发现有typed Mutation的预检和资产级事务。已连接／未连接端口样式、布局、命中及导航优先复用；确有缺少的正式拦截点才增加domain-neutral vendor接口，注明3C。人工单操作Undo与Document整包Undo各有一个事务owner，内部handler不再次记Undo。不能以“原生”名义允许直写绕过校验。

业务取舍：自由蓝图节点库创作范围更大，但无法保证Character语义和编译；受控目录保持作者能创建的内容都可保存、编译和诊断。

### 3. Macro是正式子图，所有权明确

复用原生Macro接口、Macro调用及IGraphAssignable导航。私有Macro自动创建为唯一根资产的子资产，持久身份和引用显式记录；用户不必先创建外部资产。共享Macro为显式独立资产。两者都不保存另一份inline图副本。删除调用时只回收无剩余owner的私有闭包，不删除共享资产。

端口身份独立于名称／显示顺序；接口改动检查所有纳入发布闭包的调用点，失效引用报错。Macro递归调用和闭包环本批明确拒绝；复用形成DAG。跨领域调用拒绝。Sub Flow保留框架原能力但不出现在正式领域创建目录，现有业务子图先迁Macro，不增加第二嵌套表示。

同一Macro由多个调用点引用时，编译按调用路径建立实例布局；执行状态属于ActionInstance或Pose Actor及其调用实例，不放在作者Macro对象上。技能同一调用点的并发激活另有调用执行身份，不能仅用图GUID区分。

业务取舍：私有子资产比全共享资产多一项根事务责任，但避免简单技能被迫管理一堆外部文件；共享Macro便于统一修改，同时要求发布闭包检查。

### 4. 来源映射随同产物发布

编译过程建立`领域 + 作者根identity/revision + 产物identity + 调用点路径 + 节点/端口/边identity`与运行operation/value/state slot之间的映射。一个节点生成多条operation时保留阶段；优化消除／常量折叠明确标记。映射作为现有编译产物组的只读调试附件原子发布，校验绑定哈希，不建立第二发布服务，不写入Gameplay snapshot或网络状态。

运行诊断沿既有Diagnostics发布紧凑operation与调用执行身份，Editor解析显示；运行时不依赖FlowCanvas作者对象。图或产物版本不匹配时停止叠加，显示“观察版本与作者版本不匹配”，保留版本信息，不猜最近节点。缺少映射明确显示不可定位。

### 5. 显示真实执行，而非模拟原生运行

复用CanvasCore／FlowCanvas的视觉绘制，引入只读观测源接口，输入为当前编译版本的节点／边／值状态。这个接口不拥有作者拓扑。外部诊断模式不调用StartGraph、BindPorts、端口getter或FlowOutput；不通过伪造graph.isRunning启用动画，原生渲染的状态／时间门禁由观测源显式提供，普通FlowCanvas图继续原行为。

瞬时节点显示本步经过，持续节点显示Running／Waiting，结束显示Completed／Cancelled／Interrupted及原因；Pose显示本帧求值和必要权重，不能把“值被读取”解释为“最终权重大于零”。只有实际分支／读取事件支持时才亮边。多次执行可显示计数，数值为完成帧中最后一次已采集值；UI刷新不重新求值。

诊断兴趣按选中实例及页面订阅，使用已有有界采集机制，跨线程数据在正式完成边界成为只读快照；丢失或覆盖明确标记，不造成执行阻塞。观测关闭不维护无限历史，开销记录分为关闭、节点观测和端口值观测三档，不预填性能承诺。

### 6. 子图观察绑定调用实例

选择键包含Session／Actor／ActionInstance或Pose实例、generation、产物identity与调用路径。父图显示调用节点的当前聚合状态；双击沿原生导航进入该次调用，面包屑保留调用上下文。共享定义编辑与实例观察分别标识。多次调用不能合并高亮；结束的实例保留最后完成快照并标明已结束，不能自动切到另一次释放。

业务取舍：仅按图GUID观察实现简单，但复用子图时会显示错误状态；调用路径映射增加调试数据，却能回答“这次攻击现在卡在哪里”。

### 7. 预览只消费正式运行

使用Scene Play协调器提供的精确启动／目标绑定／停止／重建与暂停合同。Build只由显式命令触发；打开图、选择子图、切换观察对象不构建。产物stale时显示构建并重启需求，不执行旧内容冒充新图。

暂停与单步发生于正式安全更新边界，不在Evaluate／WorldResolve／Finalize中间制造半帧。一步采用正式协调器定义的完整更新单位，并显示Simulation tick及Presentation frame；不得把不同频率两个时钟伪装成同一个tick。节点显示使用完成快照。子图缺少角色／输入时从父图运行并观察，不填假数据。

精确节点断点需要执行器可恢复的安全点，不复用原生协程断点来暂停编译执行；本批UI不提供虚假的节点级单步或断点。后续若需要，应独立扩展正式执行合同。

### 8. Document v6与原子迁移

v6保持稳定业务kind、typed字段、逻辑端口和整包hash，新增显式Macro定义／接口／调用owner闭包；第三方类型名、私有字段、运行委托和调试状态不进入editable。五生命周期继续唯一，旧v5及更早包拒绝并重新checkout。未改变的非图业务字段保持原意义。

迁移按精确Definition闭包执行，保存原资产身份与旧到新实体映射用于审计。Graph／Node／Port身份可保留时保留，冲突明确报错；对新子资产fileID反向导出正式引用。事务涵盖根、私有Macro、共享依赖中的实际修改owner、Profile引用和Document。失败恢复整组资产；不得逐图发布半迁移角色。

## Risks / Trade-offs

- [原生编辑API可能绕开领域Mutation] → 对创建、连接、重连、复制、删除、Macro接口及Undo列出入口表，逐项接入正式钩子；发现需绕过当前系统先报告，不叠加临时写服务。
- [原生FlowNode／BinderConnection含执行相关代码] → 依赖审计证明正式运行工厂只加载编译产物，不引用作者图或启动FlowScript；第三方包存在不等于项目执行它。
- [旧StateMachine／TreeClip依赖BaseGraph] → 移植作者遍历接口并直接发射语义，保留顺序、中断及Timeline所有权，不用旧对象树作转换中间层。
- [私有Macro子资产改变旧inline身份与保存范围] → 明确破坏迁移与v6；保存／回滚清单覆盖完整闭包，禁止兼容读写。
- [多operation映射或高频事件误导观察] → 显示状态分类、计数、采样缺口和版本；不能凭连线几何猜经过。
- [Scene Play未完成] → 只接既有正式观察接口并保留未完成项，不建立可运行的临时预览器，不认领完整预览闭环。
- [current与多个active change冲突] → 以下迁移第一步先登记替代范围；不自动归档旧change，不顺带实施AI／网络／IK重构。

## Migration Plan

1. 固定实施基线、能力清单、原change替代关系和源码owner；整理技能及Pose节点／状态／Macro／Timeline输入输出及对应编译模块。
2. 完成共用作者合同和原生编辑事务接入，再迁技能与Pose领域节点，保留现有业务规则。
3. 编译器直接读取新正式图，建立Macro闭包与来源映射；无编译合同节点在创建及Build前拒绝。
4. 完成Document v6与同一资产事务迁移，先做只读迁移计划与冲突清单，再显式应用精确角色闭包。
5. 接入执行诊断、只读原生视觉和子图调用观察；由Scene Play提供正式暂停／单步与生命周期。
6. 用已有正式CLI完成编译、Document往返、Build、同输入回放及诊断证据。验收记录区分作者交互、业务结果和观测开销，不新增测试代码，不将人工验证写为tasks。
7. 切换唯一入口并删除无消费者旧图／端口／窗口／转换代码，更新project与current specs；保留仍服务AI等未迁移领域的实现并注明消费者。

回退策略：实施按小步中文提交；资产事务失败立即完整回滚。正式迁移后发现不可接受问题时，回退整个源码／资产／产物批次到明确基线并重新核对，不能在运行时选择旧模型fallback。具体回退用户改动必须先由作者决策。

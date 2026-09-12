## Context

动机见 [proposal](proposal.md)。本设计是用户在讨论“通用可视化事件图，当前先给动画使用”后明确调用 openspec-propose 生成的正式草案。用户没有补充第一版必备事件；首个应用采用已经讨论的初始化、每帧更新、角色数据、作者 Get/Set 与 Pose 消费，不把此前未经确认的长草案当作已批准决定。

规划修订为 r1，用户已在确认“事件图直接用 FlowCanvas runtime、PoseGraph 保持编译执行”后回复“可以”并显式调用 derive-implementation，授权按本修订开始实现。确认业务基线为提交 3c7ad533273175f25df6bc063fc278a899707e12；以下确认与窗口绑定记录不改变业务方案。规划窗口为 01a095f2-ed45-7502-93f7-e9c9df0b7279，本目录 proposal/design/specs/tasks 由该窗口唯一维护，实现窗口只维护本目录 execution.md。

### 已核对的当前代码

所有 Assets 路径均位于 3cDemo/Client/3C_Client；源码表中的路径相对 Assets/GameScripts/Main。

| 入口 | 已知事实 |
|---|---|
| 原生 FlowScript / Graph | FlowScript 已收集 IUpdatable 并绑定端口；Graph.StartGraph 支持 Manual，UpdateGraph(float) 可由宿主传时间 |
| 原生 GetVariable / SetVariable | Get 在读取时取当前变量，Set 沿执行线赋值；部分 perSecond 选项直接读取 Time.deltaTime |
| 原生 SwitchBool / Split / Sequence | SwitchBool 执行选中分支后执行 Then；Split Instant 才是同次顺序分支；Sequence 是跨次 Flip Flop |
| 原生 Ports / Node.Error | Editor 条件路径可能捕获异常并继续返回值，与 Player 直接抛错不同；不能只依赖外层 try/catch |
| Runtime/Character/Control/Authoring/FlowGraphs/BtsmtlSkillFlowGraph.cs | 现有 Skill 作者图禁止原生运行；NativeNodeCatalog 的少量比较/逻辑映射不是通用事件编译能力 |
| Runtime/Character/Pipeline/Animation/Contracts/Pose/CharacterPoseCanvasGraph.cs | 现有 Pose 作者图禁止原生运行，Blackboard 为只读投影，Get 经正式写入路由生成 |
| Runtime/Character/Pipeline/Presentation/CharacterSimulationPresentationRuntime.cs | 当前主链为 FactProjector.Project → 固定 FromFact 参数帧 → Animation.BeginPresentation |
| Runtime/Character/Pipeline/Animation/Contracts/Pose/CharacterPresentationProgramParameterFrame.cs | 只支持三个 motor ID；FromBody、FromFact、FromDirect 都存在；Preview 也有调用 |
| Editor/CharacterSimulation/Compilation/Presentation/CharacterPoseCompilerModule.cs | Pose 继续通过现有固定 Pass 编成 Program Image |
| Editor/CharacterPipeline/Authoring/PresentationDocument/ | 已有唯一 Package、Exporter、Reconciler、Mutation 和 Validator |

共享工作树有其它任务提交及未提交改动。尤其 PoseGraph 资产、BlackboardEditor、Agent 通用事务和 Skill 作者模块已有交叉改动；实施前必须重读差异，不能覆盖。静态源码核对不能证明当前运行成功。

## Goals / Non-Goals

**Goals:**

- 作者直接用原生节点表达事件逻辑，先完成动画变量生产；不同宿主通过事件与数据合同扩展。
- 输入是正式角色表现事实、本次 delta 和图内状态；输出是同一次完整更新后的 typed 只读变量帧。
- 从原来的固定 motor 提供者，改为作者可创建变量、计算、写入，并由唯一 Pose 输入合同消费。
- 通过同一作者事务与正式 Build 交付，不做 UI-only 能力、临时变量表或图后台预览器。

**Non-Goals:**

- 本次不实际接关卡、相机或其它 Gameplay 宿主，不增加全局事件总线。
- 不提供动画播放/停止、技能请求、伤害、角色移动、世界查询、Foot/IK 或骨骼写入节点。
- 不重写 Pose/Skill runtime，不增加 EventGraph IR、节点 lowering、虚拟机或 fallback。
- 不接管 Pose 只读输入、曲线分类、Body Foot 入口、资源作者改革和旧重复 Pose 子图清理。
- 不新增测试代码，不在 tasks 写手动验证。本文记录未来验收条件，不声称本轮已运行。

## Decisions

### D1. 原生执行事件图，编译执行PoseGraph

选择原生 FlowScript 运行。每实例克隆作者图，以 Manual 方式由正式宿主推进；不挂第二个自动 FlowScriptController，也不改开原 Skill/Pose 作者图的运行禁令。

业务收益是作者直接使用原生节点与执行顺序，不需要每增加一个计算节点再做一次项目指令翻译。另一条有价值的路线是将原生作者节点编译进动画 Program，可共享现有状态事务和密集布局，但要逐个维护节点 lowering，覆盖范围会受它约束。本提案选择直接复用，并明确承担 D5 的原生状态推进规则；不同时保留两种执行模式。

仍需正式 Build 校验和发布：它验证图/宿主/变量/消费者接口、版本及运行依赖，并使修改后的内容成为可使用版本。它不把事件连线翻译为另一份事件程序；Pose 自己的 Compiler 保持原链。

### D2. 通用基础只接宿主，动画提供业务数据

拟定三块实现：

| 模块 | 输入与输出 | 唯一职责 |
|---|---|---|
| HostEventGraph + NativeEventGraphRuntime | 原生图、宿主合同、事件调用 → 原生执行结果 | 复用 FlowScript、实例化、绑定、触发、故障和销毁 |
| EventGraphHostContract | 宿主事件/输入/节点权限声明 → Capability 和接入约束 | 通用机制不引用 Actor、Pose、Motor 或 CharacterPresentationFactFrame |
| CharacterAnimationEventGraphHost | 正式 FactFrame、delta、Reset → 动画变量输出 | 动画时机、事实接入及对外只读交接 |

通用代码放在领域无关的 GraphAuthoring/EventGraphs 模块；角色接入归 Character/Pipeline/Animation/EventGraphs。现有 Character 程序集已引用 FlowCanvas/NodeCanvas；不拷贝插件，也不把动画宿主塞入 Skill 图目录。

直接把新事件图增加成 BtsmtlSkillFlowGraph 的一个 role 会少建入口，但会把 Skill 编译、变量 scope 和动画运行绑在一起。独立宿主适配多一个明确边界，却允许以后关卡提供事件而不理解动画；这是通用性的业务价值。第二宿主本轮不实施，也不宣称已经跑通。

### D3. 原生Blackboard是唯一变量声明和状态

HostEventGraph 的原生 Blackboard 保存变量 ID、作者名称、类型与初值，不增加可写的 AnimationVariableDeclaration 镜像。运行时实例 Blackboard 唯一保存变量值及原生节点的内部历史。

公开合同由原生声明派生，拟定为：

| 合同 | 必须包含 |
|---|---|
| EventGraphVariableReference | 图稳定身份 + 原生 Variable.ID；显示名不参与运行寻址 |
| CharacterAnimationVariableContract | 图/合同 revision，变量引用、精确类型、初值和读权限；从原生声明只读投影 |
| CharacterAnimationVariableLayout | 发布前生成的唯一密集列及类型，不保存运行值；所有消费者引用同一 layout identity |
| CharacterAnimationVariableFrame | Actor/实例、表现采样身份、Simulation sample tick、Reset generation、图/合同/layout 版本、typed 只读值 |

首个 Pose 交接支持数值 Float、开关 Bool、计数 Int；Int 不以 float 中转。Vector2/Vector3 在事件图内保持原生类型，按实际需要提取分量交给当前标量 Pose 输入。对象、集合、任意枚举和直接 Pose 向量口不在首个输出合同中，不把“通用”理解成所有 CLR 类型天然能送进 Pose 程序。这是面向当前速度/判断业务的可审阅范围，不是用户已逐项选择过的类型限制。

Pose 可见变量及跨图权限由独立只读 change 按同一合同投影。事件图负责 Get/Set，PoseGraph 只 Get；变量重命名保留 ID。曲线、BlendShape 与脚权重继续由 Pose 采样/混合链拥有，不能按当前 Usage.Control 标签直接迁入事件图。

备选是单建动画变量资产供两张图读写，能让变量脱离事件图复用，但会重新建设 FlowCanvas 已有的声明与编辑适配。本提案直接沿用原生声明；运行输出快照是只读发布结果，不是第二份可写状态。

### D4. 作者与节点范围沿原生能力接入

使用原生 GraphEditor、Blackboard、节点/连线 Inspector、breadcrumb、Macro 与 Undo。动画图从正式动画作者入口打开，不新建常驻 Details 面板。已有只读 Blackboard 可拾取/拖拽的改动必须保留。

第一版准入原生 StartEvent、UpdateEvent、Get/Set、逻辑/比较、合规数值方法、SwitchBool、Split Instant、Sequence 和同步 Macro。原生数值方法包装器直接调用原有方法，例如限速趋近、插值或 clamp；不复制算法。宿主目录登记成员身份、端口和副作用权限，同步提供 UI/Document/Validator，Player 所需反射成员和泛型保留进入正式构建。

每图一个初始化入口和一个每帧入口，多个处理分支由原生 Split Instant 明确排序。保留 SwitchBool 的 Then 与 Sequence 的 Flip Flop 含义。Get 值按原生消费时机读取；需要“写入前的值”时作者显式保存，不能假定值线先统一执行一次。

动画宿主只提供一次同步更新。UpdateInterval 固定为 0；拒绝 Wait、Timed Split、协程与使调用跨帧悬挂的断点。Set 的 perSecond、DeltaTimed 等直接读全局时间的模式不可用；作者显式将宿主 delta 连接到原生算术节点。该限制只属于本动画宿主，不全局删除 FlowCanvas 的等待/延时能力，以免以后关卡流程无法沿用。

放开全部节点能最快获得原生功能，但会把副作用、跨帧状态和任意 Unity 时间带入动画。宿主准入需要一次元数据工作，却不重写节点行为，同时明确哪些能力真的可用于这个业务。

### D5. 更新图按表现事件推进，不伪装成Pose状态事务

实际顺序：

1. 现有 FactProjector 完成本次 Body/Intent 时间对齐。
2. 首次有效输入先绑定数据、原生 Start，再执行同次 Update；以后只执行一次 UpdateGraph(animationDeltaSeconds)。
3. 原生调用完整成功后冻结变量输出；失败不发布，不启动本次 Pose。
4. 既有 Animation.BeginPresentation 接收该只读输入，继续 Action、PoseAdvance、Source、Evaluate 和 Seal。

更新成功后，如果 Pose source 尚未准备好，图中的平滑、计数与 Flip Flop 状态仍然保留；下一次表现事件继续更新。业务上，速度平滑跟随已经经过的时间，不等待资源准备才前进。Pose 的 Pending、Committed 和 Physical Writer 保持原有规则，输入“更新完成”不能被标成姿势“显示完成”。

另一种业务要求是计数等也随 Pose 成功才前进。这需要完整的图状态事务或编译路线，不能只备份 Blackboard：原生 Sequence、事件计时、Macro 等另有状态。本提案明确选择原生推进，不引入反射快照、每帧克隆或失败后重放。

Actor/Body discontinuity、Reset、Replacement 结束旧图实例及其节点状态，下一次有效输入重新初始化；普通 PoseState 切换不重建事件图。Pause/无正 delta 不推进。源输入错误、图错误或 Pose Actor 已 Faulted 时停止该实例，直到正式 Reset/Replacement。

### D6. 失败与输出寿命由正式边界保证

只读输入在声明/连接和实际宿主写入边界拒绝 Set；不是只禁用 UI。节点异常或非法数值使该次调用失败；已部分 Set 的原生实例不再被消费，不声称恢复了全部内部状态。

原生 Ports.cs 的 Editor 捕获分支会调用 Node.Error，外层 catch 不一定收到异常。因此在 Node/Graph 正式错误边界提供按图实例的执行失败通知，统一交给当前调用结果。不能扫描 Console、依赖 Logger 是否开启、复制第二份 Ports 或修改全局节点执行行为来补洞。

成功输出使用预分配 typed 缓冲与明确只读租约，在所有 Pose/Worker 消费结束前不得重写。绑定时解析原生变量与密集列，每帧不按名称搜索，不把可变 Variable 或 Unity 对象传入 Job。观察区分原生执行、已发布输入和已提交 Pose，只在正式场景暂停/单步时控制时钟。

### D7. 与Pose只读输入change的唯一分工

| 事项 | 本change | refine-pose-graph-readonly-blackboard |
|---|---|---|
| 原生变量声明、Set、事件执行、Reset | 唯一拥有 | 不实现另一个更新器 |
| Contract/Layout/Frame 类型及生产语义 | 唯一拥有，放动画公共合同 | 引用同一类型，不能创建同义布局 |
| 更新图资产及其动画根精确引用 | 拥有该新增引用字段与闭包 | 读取；不再添加另一变量owner |
| Blackboard 可见范围、Pose Get、子图接口 | 不重复实现 | 唯一拥有 |
| Pose Compiler/Program/BlendSpace/条件消费 | 提供接口和实际输出 | 唯一完成 typed 读取与消费者调用迁移 |
| 固定参数桥删除 | 所有调用迁移后删除旧类型、生产方法及配置 | 移除消费端的固定 ID 限制和旧签名 |
| 曲线、Body Foot、重复声明/子图 | 不接管 | 保留原change所有权 |
| Agent通用服务/版本 | 只增本图种适配 | 共用同一事务；全局版本需与FSM发布所有者合并 |

输入契约形成后，消费侧从同一 Contract/Layout 生成句柄，不等待一次假 Gameplay 运行。输出合同不包含 Gameplay mutable address。任何同字段冲突先保留现场并由用户决定归属；不能用临时接口让两边各自完成。

本 change 的生产能力可分步实现，但必须等消费侧真实接通后才移除旧桥和宣称动画闭环。其旧 motor 参数桥不是长期保留的另一正式模式。完整场景验证继承消费侧明确的角色/Fixture 与消费位置；不为了事件图新建角色或重写 Corin locomotion。

### D8. Document、Build与版本

事件图和可达 Macro 通过现有 PresentationDocument 域扩展表达。拟定目录为 editable/presentation/event-graphs/<canonical-id> 的 graph.json/layout.json，Macro 同样使用原生接口、稳定参数和独立闭包。Context 提供宿主事件、只读输入与合规原生成员；只接受稳定 capability kind、typed fields 和逻辑端口，不让 JSON 写 C# 类型名或私有序列化字段。

Exporter、Reconciler、typed Mutation、Validator、完整 owner 事务和反向导出一次接通，不增加 EventGraph MCP 或另一个 save/apply 服务。变量删除与消费引用在同批目标中验证，不能保存悬空引用。

当前现行 Document 文本为 v7，原生 FSM change 正在推进 v8。本任务新增图种须并入届时唯一正式 schema 发布：若与 FSM 同批发布，共用一次新版本；若其已发布，按既有规则发布下一版。具体版本整数属于实施基线登记，不改变这里的严格单版本协议和任务范围；不得各自发布不兼容的同版本，也不保留旧包 reader。全局版本正文由对应发布所有者维护，本任务不抢改其规划。

Build 冻结事件图/宿主/变量依赖版本和 AOT 要求，Pose 编译仍走原 Compiler。语义修改使依赖 Stale；显式 Build 后以新实例替换，绝不运行时修复旧输入布局。

## Risks / Trade-offs

- [原生行为与Pose提交不同] → D5 明确规定事件状态保留，诊断分别显示输入和Pose完成，不冒充共同回滚。
- [节点能在Editor运行却不能在目标Player运行] → 合规成员、泛型和AOT保留纳入原发布链；原生错误通知覆盖Editor/Player差异。
- [限定动画节点导致通用性被误解] → 限制属于动画宿主；原生等待、关卡事件等不从通用基础删除，本轮也不声称完成第二宿主。
- [原参数名称掩盖坐标差异] → FromBody 按旋转逆变换取局部速度，FromFact 当前按 MovementDirection 分量生成值；迁移先对照真实消费者坐标，不借清理静默改变方向语义。
- [共享工作树和Document版本交叉] → 只维护本change的字段/适配，不覆盖已经正确的其它改动；接口未闭合时如实保留未完成，不添绕行路径。
- [误把当前资源存在当成演示就绪] → 已按脚本GUID搜索 Assets/Configs，未发现正式 BlendSpace 资产引用；BlendSpace 是能力检验例子，不据此宣称 Corin 已运行该链，不新增独立八向演示。

## Migration Plan

1. 按 D7 固定输出合同、文件/字段所有权和实施基线；确认现有消费侧的正式角色/Fixture。
2. 原地增加宿主适配、原生运行与变量合同；同时接通作者和 Document，保持原生变量 ID、节点、端点与 Macro 接口。
3. 在正式 Fact 输入后安装唯一动画宿主，加入失败、Reset 和实例销毁；原动画根调度不搬入图。
4. 消费侧 change 完成同一 typed Frame/Get/条件/BlendSpace 接入。旧固定 ID 仅按精确消费者语义映射，不能按显示名猜测。
5. 本 change 删除 CharacterPresentationProgramParameterFrame 的三个固定字段、Supports、FromBody/FromFact/FromDirect 及旧生产路径；所有引用必须已迁移。AnimationPreviewEngine 的资源查看用途由其原正式资源输入合同承接，角色预览沿同一宿主，不留旧直传桥。
6. CharacterPresentationFrameCoordinator 本轮类名搜索只见定义/构造，未见消费者；实施确认所有引用与编译条件后，只删除确实无消费者且受旧桥删除牵连的旧类，不将它接成第二主链。
7. 通过正式资产事务接入明确动画内容，沿精确 Definition 正式 Build 发布。旧 ABI/包或布局显式失效，不保留兼容 reader。
8. 作者事务失败沿原机制整体恢复；代码回退以本change独立提交和对应重新发布的正式产物为单位，不能长期保留两套运行方案。
9. 仅本change规划文件作为方案来源。旧 docs 规划入口改为本目录索引，静态证据标记为历史读取；不维护旧 N/C 双方案正文。

### 完成定义与验证条件

任务清单只列实现和清理，不新增测试或手动验收任务。未来 execution.md 记录真实检查、失败和用户验证状态，至少说明：

| 场景 | 完成证据 |
|---|---|
| 原生事件编辑 | 原生UI与Document可创建变量、接Get/Set和分支，改名/Undo/重载保持稳定引用 |
| 同次变量交接 | 初值1，经Set变2，同次Pose读2；限速趋近例子得到0.2而非原始角色速度4 |
| 实例与类型 | 两角色变量/节点状态隔离；Bool/Int没有float伪装；缺失和类型错误明确定位 |
| 更新与Pose | 每次表现事件只运行一次，Source未就绪保留输入状态，Pose不发布Pending |
| 错误与重置 | Editor/Player节点错误都阻止部分输出；Reset同时重建变量和节点状态，另一Actor不受影响 |
| 旧链删除 | 正式调用者全部采用同一输入合同，旧motor桥和补值注册无剩余消费者 |
| 曲线与最终显示 | 原Foot/BlendShape曲线仍由原链处理，正式场景消费新变量后产生可观察的Pose结果 |
| 发布 | 图变更正确Stale，正式Build/Replacement采用新依赖；编译成功不代替场景证据 |

本轮只检查规划文档，不启动 Unity、Build、Play。后续不默认新增测试；Unity检查和当前Console读取服从项目规则。

## Current Spec Comparison

| 规范/关联提案 | 矛盾或交叉 | 处理 |
|---|---|---|
| graph-authoring-domain-framework 的 Authoring节点与Runtime执行描述必须分离 | 现行文本要求每个领域都编译，直接原生事件执行与之冲突 | 本change提供完整MODIFIED条款，仅明确EventGraph边界，保留Pose/Skill编译 |
| project.md 的 Program/Session 与不恢复对象解释器口径 | 新事件输入域尚未列入，不能声称已允许 | 归并时同步项目口径，限制为原生事件输入生产，不取代Gameplay/Pose；本轮不改成已实现 |
| character-animation-pipeline 的唯一编译Pose链 | 需补输入生产时机和状态保证 | 本change ADDED 唯一事件宿主条款，不覆盖Action/IK等原合同 |
| character-presentation-pose-graph 的 committed parameter page、Transition只读Fact | 作者变量来源、同次快照与条件可见范围需衔接；readonly当前delta已改参数来源，但尚未改Transition只允许Fact的完整条款 | 交给已拥有完整消费侧的readonly规划补齐该条款和同次输入身份，再实施条件消费及归并；本change不重复接管其MODIFIED delta |
| refine-pose-graph-readonly-blackboard | 当前已引用旧待确认事件图，明确依赖本窗口变量合同 | 本目录替代旧方案入口；其任务/规范仍由原规划所有者维护，不未经授权写回 |
| btsmtl-agent-authoring-document-sync / 原生FSM | 现行v7与计划v8，新增图种有目录和版本交叉 | 本change只增加事件闭包/事务要求，按D8进入一次唯一正式版本 |
| unify-skill-authoring-data-model | 只抽Skill共同参数，不拥有事件运行 | 可使用已完成共享元数据，不扩大其范围，不复制其定义或任务 |
| integrate-pose-flowcanvas-editor-preview / 旧Pose架构 | 资源作者、Slot和曲线仍有active增量，原生替换Pose实验已经撤回 | 本change不重开该实验，不接管其它作者改革；原生EventGraph和编译PoseGraph职责分别保持 |
| graph-authoring-editor-shell / BlendSpace资源条款 | 尚有旧自有Shell、SourceSlot等与当前作者方向不一致的文本 | 已记录既存差异，不据此恢复旧UI或抢改资源所有权，消费侧沿其正式迁移处理 |

这些冲突已在提案中明确提出或交给唯一消费/发布所有者，不把所有规范声称为已经一致。

## Open Questions

- 正式内容接入时的显示名称、现有角色/Fixture及具体消费位置，由消费侧既有迁移清单确定；不得改变本提案的变量生产职责或顺便重做关卡/Locomotion。
- Document发布使用的具体版本号取决于实施时已发布基线；协议必须遵守D8的同批一次升级或下一版，不存在兼容模式选项。

上述是内容登记与版本编号，不留原生/编译双选或状态回滚等会改变规范的未决实现路线。用户已确认 r1；已有跨change依赖继续按D7/D8处理，不扩大实现范围或建立临时路径。

## Workflow Binding

实现开始前必须完整读取 C:/Users/Lenovo/.codex/skills/derive-implementation/references/implementation-protocol.md。实现只改授权代码和自己的 execution.md，不修改本目录规划文件，也不改其它任务的规划。任务5.2的“提供合同”通过代码和实现记录完成，不是跨窗口报进度。

正常实现、工具/权限问题、编译、运行、验证、提交与完成都留在实现窗口和 execution.md，不发送回执、进度或完成汇报，不查找或联系其它任务。只有确认文档与真实代码/规范/公共合同无法同时成立，且继续必须改变业务方向、公共所有权或范围时，才在 execution.md 写全 ACTUAL_CONFLICT_REPORT，并向本规划窗口发送一次 ACTUAL_CONFLICT 文档指针。无关工作继续，被阻塞部分不绕行。

```text
PLANNING_DOCUMENT
planner_thread_id: 01a095f2-ed45-7502-93f7-e9c9df0b7279
planning_document_paths:
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/proposal.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/design.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/tasks.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/specs/flowcanvas-event-graph/spec.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/specs/character-animation-event-graph/spec.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/specs/graph-authoring-domain-framework/spec.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/specs/character-animation-pipeline/spec.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/specs/btsmtl-agent-authoring-document-sync/spec.md
implementation_document_path: D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/execution.md
confirmed_by_user: true
confirmed_revision: r1@3c7ad533273175f25df6bc063fc278a899707e12
```

```text
IMPLEMENTATION_LINK
planner_thread_id: 01a095f2-ed45-7502-93f7-e9c9df0b7279
implementation_thread_id: 01a09627-3d34-7e61-822f-76aafa68765e
task_title: FlowCanvas事件图
shared_directory: D:/Unity_Project_1/3C
planning_document_paths:
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/proposal.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/design.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/tasks.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/specs/flowcanvas-event-graph/spec.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/specs/character-animation-event-graph/spec.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/specs/graph-authoring-domain-framework/spec.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/specs/character-animation-pipeline/spec.md
  - D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/specs/btsmtl-agent-authoring-document-sync/spec.md
implementation_document_path: D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/execution.md
model: gpt-5.6-luna
reasoning_effort: max
```

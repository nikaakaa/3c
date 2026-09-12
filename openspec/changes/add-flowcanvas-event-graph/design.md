## Context

动机见 [proposal](proposal.md)。当前规划修订 r2（2026-09-13）执行用户广播 2026-09-13-authoring-r2-plan，协调来源为 2026-09-13-eventgraph-authoring-r2。公共基线为 [remove-agent-authoring-use-native-csharp/design.md 的 r2](../remove-agent-authoring-use-native-csharp/design.md)：只提供显式 export_code 与 generate_assets，不保留 Agent Document、五工具、重复 Validator 或同步中转。

此前 r1 已确认原生 FlowCanvas 事件执行、动画宿主、变量生产与 Pose 分工，仍是本次运行设计基础。r2 只替换作者调用与代码输出接入，不重新选择原生/编译路线，不增加变量声明或运行布局副本。

本窗口 01a095f2-ed45-7502-93f7-e9c9df0b7279 唯一维护本目录 proposal/design/tasks/specs；实现窗口只维护 execution.md。本次是 PLAN 广播，仅修订文档，不改代码/资产、不向任何窗口发送回执、结果或执行消息；r2 尚未下发实现。

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
| Runtime/BTSMTL/EventGraphs/HostEventGraph.cs / HostEventGraphEditorMutation.cs | 已有原生创建/连接、身份及局部 Undo；仍暴露 ApplyAuthoringDocument → ApplyDocument 旧入口 |
| Runtime/BTSMTL/EventGraphs/EventGraphAuthoringDocument.cs | 保存变量/节点/边的中间结构，属于本次需退役的必经模型 |
| Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseGraphAuthoringAdapter.cs | ApplyEventGraphMutation 仍调用 Agent Mapper 再进入事件图 ApplyDocument，不能只删 Agent 目录 |

共享工作树有其它任务提交及未提交改动。尤其 PoseGraph 资产、BlackboardEditor、Agent 通用事务和 Skill 作者模块已有交叉改动；实施前必须重读差异，不能覆盖。静态源码核对不能证明当前运行成功。

## Goals / Non-Goals

**Goals:**

- 作者直接用原生节点表达事件逻辑，先完成动画变量生产；不同宿主通过事件与数据合同扩展。
- 输入是正式角色表现事实、本次 delta 和图内状态；输出是同一次完整更新后的 typed 只读变量帧。
- 从原来的固定 motor 提供者，改为作者可创建变量、计算、写入，并由唯一 Pose 输入合同消费。
- 原生 UI 和生成代码使用同一正式配置 API 与既有局部 Undo/保存；公共工具显式导出/生成，Build 独立，不做 UI-only 能力或临时变量表。

**Non-Goals:**

- 本次不实际接关卡、相机或其它 Gameplay 宿主，不增加全局事件总线。
- 不提供动画播放/停止、技能请求、伤害、角色移动、世界查询、Foot/IK 或骨骼写入节点。
- 不重写 Pose/Skill runtime，不增加 EventGraph IR、节点 lowering、虚拟机或 fallback。
- 不接管 Pose 只读输入、曲线分类、Body Foot 入口、资源作者改革和旧重复 Pose 子图清理。
- 不增加中央 Validator、整包同步事务、结构 DTO 中转、自动源码同步或事件图专用 MCP。
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

当前通用接入已经位于 Runtime/BTSMTL/EventGraphs，角色接入归 Character/Pipeline/Animation/EventGraphs；本次收口沿用现有位置，不因协议退役搬迁已有效的运行模块。现有 Character 程序集已引用 FlowCanvas/NodeCanvas；不拷贝插件，也不把动画宿主塞入 Skill 图目录。

直接把新事件图增加成 BtsmtlSkillFlowGraph 的一个 role 会少建入口，但会把 Skill 编译、变量 scope 和动画运行绑在一起。独立宿主适配多一个明确边界，却允许以后关卡提供事件而不理解动画；这是通用性的业务价值。第二宿主本轮不实施，也不宣称已经跑通。

### D3. 原生Blackboard是唯一变量声明和状态

当前 HostEventGraph 的原生 Blackboard 是正式对象上的变量声明，保存变量 ID、作者名称、类型与初值；不增加可写的 AnimationVariableDeclaration 镜像。显式导出把这些声明表达为可重建 C#，显式生成按代码重建声明，二者不是同时可写的同步数据源。运行时每实例只有一份 Blackboard 变量状态，原生节点历史由同一实例的原生节点持有。

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

第一版准入原生 StartEvent、UpdateEvent、Get/Set、逻辑/比较、合规数值方法、SwitchBool、Split Instant、Sequence 和同步 Macro。原生数值方法包装器直接调用原有方法，例如限速趋近、插值或 clamp；不复制算法。宿主目录登记成员身份、端口和副作用权限，供原生 UI、直接 C# API 和领域局部规则共用；代码输出薄适配只读同一描述及正式对象。Player 所需反射成员和泛型保留进入正式构建，不新增中央 Validator。

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

### D7. 与Pose及C# authoring任务的唯一分工

下表源码路径相对 Assets/GameScripts/Main。各方只修改自己拥有的文件或已明确划分的字段；接口实现和删除必须依赖实际调用迁移，不能用临时桥让各任务单独报完成。

| 事项或文件 | 本任务唯一职责 | 其它任务职责 |
|---|---|---|
| Runtime/BTSMTL/EventGraphs/EventGraphAuthoringDocument.cs | 正式 API 可表达其有效配置后删除结构模型与附属 DTO；不得换名保留 | C# authoring 删除 Agent Mapper 等公共调用者；Pose 改自身调用 |
| Runtime/BTSMTL/EventGraphs/HostEventGraph.cs | 保留原生运行/身份/创建/连接，取消 ApplyAuthoringDocument 强制中转，暴露有效直接 API | 其它任务消费 API，不同时编辑本文件 |
| Runtime/BTSMTL/EventGraphs/HostEventGraphEditorMutation.cs | 从原有效实现抽出直接变量/节点/连接/配置能力，保留局部 Undo 与真实规则，删除 ApplyDocument 及协议解析 | 不将该文件整体移入公共输出器或另建事件图事务 |
| Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseGraphAuthoringAdapter.cs | 提供替代 API 和事件图类型合同，不接管该文件 | Pose 任务将 ApplyEventGraphMutation 调用改为直接 API |
| Editor/CharacterPipeline/Authoring/PresentationDocument/AgentAuthoringEventGraphDocumentMapper.cs 及公共 Agent 协议 | 不接管公共文件，不搬迁其中完整结构模型 | C# authoring 任务在调用方迁出后删除 Mapper、协议 DTO 和五工具等 |
| 事件图完整对象读取及正式 API 输出薄适配 | 读取原生变量/节点/配置/连接/Macro/布局，向共同代码输出器提供薄扩展 | C# authoring 拥有通用遍历/语句输出、生成上下文、两个 MCP、文件写入和保存编排 |
| 动画根事件图引用、Contract/Layout/Frame、事件更新 | 本任务唯一拥有；生成后根引用通过正式 API 明确恢复 | Pose 只读取同一声明/布局，不自建更新器或变量表 |
| Pose Get、作用范围、条件、BlendSpace、Compiler/Program消费和曲线 | 提供唯一输出与身份合同，不重做消费 | Pose 任务唯一负责全部消费适配和曲线/Body清理 |
| 固定 motor 桥 | 全部真实消费者迁入同一输入合同后，删除旧生产类型/方法/配置 | Pose 任务完成 Get/条件/BlendSpace/运行及Preview读取与签名迁移 |

本次真实冲突链已重新读取：
CharacterPoseGraphAuthoringAdapter.ApplyEventGraphMutation → AgentAuthoringEventGraphDocumentMapper.Map → EventGraphAuthoringDocument → HostEventGraph.ApplyAuthoringDocument → HostEventGraphEditorMutation.ApplyDocument。后者先 ClearNativeNodes 和 blackboard.variables.Clear，再按 DTO 重建变量、节点及边。只删除 Agent 目录仍留下领域中的结构模型和强制重建入口，不能满足 r2。

直接 API 的参数应是该操作所需的对象、稳定 ID 和 typed 配置；不能仍收取一份包含整图变量/节点/边的“新配置模型”，再清空整图执行它。有效的 AddNode、CreatePortConnection、变量绑定、ConfigureIdentity、局部 Apply/Undo、dirty 和失败恢复继续服务人工编辑与 C# 调用，不为去 Agent 而删除这些正确能力。

### D8. 公共C#输出/生成与事件图薄适配

公共行为严格采用 authoring r2：

| 入口 | 输入和输出 | 事件图侧责任 |
|---|---|---|
| btsmtl.export_code | 明确当前资产、Definition/owner 与代码输出路径 → 完整 C#、根入口、外部依赖和诊断 | 完整读取所拥有的事件图闭包，向共同输出器表达正式创建/配置/连接调用；不读旧源码，不修改输入资产 |
| btsmtl.generate_assets | 明确源码、对应已编译 recipe_type、Definition/owner、生成范围和输出路径 → 根对象及实际保存结果 | 提供同一正式 API，重建范围内图/变量/节点/边并恢复指定 Profile/根挂接；不提供任意 eval 或另一生成服务 |

工具、recipe 基础合同、通用值/语句输出和范围保存归 C# authoring 任务。事件图适配覆盖稳定图 ID、原生 Variable.ID/类型/初值、节点真实类型与配置、宿主输入、Get/Set 目标、赋值模式、动态端口与顺序、Macro 接口/共享关系、连接稳定身份、布局以及正式外部引用。语义读取直接来自当前对象及已有字段/方法合同，不先建 EventGraphAuthoringDocument，也不经 JSON 转换。

输出按对象身份去重，先创建生成范围内对象，再配置字段/端口、绑定内部引用，最后连线与恢复明确根引用。输出过程中“对象→本次局部变量”的字典和依赖集合只活在当前调用，不成为持久化结构模型。内部引用使用本次创建对象；真正范围外资源作为明确输入。不能把旧生成子资产 GUID 当内部依赖，否则删除生成资产后无法重建。

C# 明确保存逻辑身份，重建后 Pose Get 继续按同一图/Variable.ID 解析；物理 Unity 实例、GUID/local file ID 不要求永久不变。Profile 根及本次明确的外部挂接必须显式恢复，不全局扫描猜消费者。一个共享内部对象只创建一次，范围外共享资源不复制或删除。

完整输出不能省略当前正式内容。薄适配不支持某节点、成员、Macro 字段、布局或引用时，向共同输出器返回精确对象/字段与原因，使完整导出失败；不能输出占位、默认值或半份代码替换旧文件。尚未实现的事件图适配不能被公共输出器声称为已支持。

人工修改/保存图不导出代码；只有显式 export_code 才把当前结构写入源码。显式 generate_assets 按指定已编译代码重建明确范围，不自动合并未导出的人工调整，不读取旧源码做同步，也不追加“创建后再删除”的操作日志。正常 C# 编译只让生成入口可执行，不是把事件图转换成项目事件程序。两个作者工具不自动 Build/Play。

Graph/宿主校验变量类型、节点、端口及只读输入，Pose 校验消费、布局和拓扑，正式 Compiler 校验运行产品。输出器只检查输出完整性，不复制业务校验，不建新的中央 Validator、整包 hash/session/reconcile/sync 或反向导出事务。保留人工编辑现有 Undo 与局部保存；generate_assets 的明确范围保存由公共作者服务调用现有能力完成。

Build 继续独立校验事件图/宿主/唯一变量合同与消费者依赖、AOT/泛型和运行版本，图语义变更沿既有 Stale/Replacement 规则处理。Document 包版本升级从本任务完全撤销；不再以等待 v8/v9 为由保留旧协议。

业务取舍：完整图导出得到的是当前展开结构，可删除重建且不依赖旧资产；不承诺保留原手写算法、变量名和排版。沿用共同输出器需要事件图补齐真实读取/配置能力，但避免维护另一套遍历、C#打印器或重复规则。

## Risks / Trade-offs

- [原生行为与Pose提交不同] → D5 明确规定事件状态保留，诊断分别显示输入和Pose完成，不冒充共同回滚。
- [节点能在Editor运行却不能在目标Player运行] → 合规成员、泛型和AOT保留纳入原发布链；原生错误通知覆盖Editor/Player差异。
- [限定动画节点导致通用性被误解] → 限制属于动画宿主；原生等待、关卡事件等不从通用基础删除，本轮也不声称完成第二宿主。
- [原参数名称掩盖坐标差异] → FromBody 按旋转逆变换取局部速度，FromFact 当前按 MovementDirection 分量生成值；迁移先对照真实消费者坐标，不借清理静默改变方向语义。
- [公共输出与领域直接API交叉] → 按D7逐文件分工，API与调用者未完成迁出时不先删旧模型，不用同义DTO或临时接口维持两条链。
- [完整输出遗漏或仍引用旧生成资产] → 未支持内容明确失败；逻辑ID写入C#，内部引用使用本次对象，根挂接明确恢复。
- [人工修改与源码被误当同步] → 两个工具各自显式执行，生成不合并未导出的人工修改，不新增自动导出或源码同步。
- [误把当前资源存在当成演示就绪] → 已按脚本GUID搜索 Assets/Configs，未发现正式 BlendSpace 资产引用；BlendSpace 是能力检验例子，不据此宣称 Corin 已运行该链，不新增独立八向演示。

## Migration Plan

两项删除拥有不同先决条件，不按“去掉旧代码”合并执行：

1. 读取当前调用关系，按D7固定领域API、Pose调用适配与Agent Mapper文件归属；确认共同输出器实际扩展合同。原生运行、动画变量合同和已正确代码作为保留基线。
2. 在 HostEventGraph 与 HostEventGraphEditorMutation 补齐直接声明/读取/配置/连接和稳定身份 API，从旧处理链复用有效局部规则与 Undo；不把整图 DTO 换名为 API 参数。
3. Pose 任务将 CharacterPoseGraphAuthoringAdapter 的旧内容修改调用迁到直接 API；C# authoring 任务删除/替换 Agent Mapper 等公共协议调用。两侧实际消费者脱离后，本任务删除 EventGraphAuthoringDocument、ApplyAuthoringDocument/ApplyDocument 及无消费者解析代码。
4. 事件图薄适配加入共同代码输出器：读取当前完整闭包，输出直接 API 调用，恢复内部对象引用、逻辑 ID、Macro 和布局；未知正式内容必须拒绝完整导出。公共任务拥有两个 MCP 和生成范围/保存服务，本任务不另建入口。
5. 在同一正式外部资源/API版本下，完整导出代码必须能在旧生成范围不存在时重建等价图，并显式恢复指定 Profile 根引用和跨 Pose Get 身份。人工编辑不隐式导出，再生成不自动合并人工修改；不以原位更新替代删除重建能力。
6. 动画生产接入保持正式 Fact → 一次原生事件更新 → 发布只读变量 → Pose。输出仍按实例、表现采样、Simulation tick、Reset 代际和合同/layout版本交接，消费完成前不覆盖。Pending 不回退成功事件状态，失败不发布部分值。
7. **固定 motor 桥独立等待**：当前 CharacterPoseStateSourceRuntime、AnimationBlendSpacePlayerRuntime、Pose Program、AnimationPreviewEngine、PoseSourcePlanCompiler 和运行入口仍引用 CharacterPresentationProgramParameterFrame。只有 Pose/条件/BlendSpace/运行/Preview 全部改用同一变量合同后，本任务才删除旧帧、Supports、FromBody/FromFact/FromDirect 与注册配置。保留真实坐标语义，不默认补值。
8. 确认无消费者的旧协调类只在固定桥清理依赖内删除；不顺便重做状态机、曲线、Foot/IK或资源作者。原生资产及范围外素材不在本轮删除。
9. 正式作者保存与运行产品 Build 分开。显式生成成功不等于 Program/Projection 已发布，正式 Build/运行证据仍分别记录；代码/生成失败按既有范围保存能力如实报告，不恢复整包同步事务。
10. 本轮仅把上述要求写入本任务规划，不执行任何迁移，不通知实现。后续执行记录继续归原实现窗口；本次PLAN不能被解释为新实现指令。

### 完成定义与验证条件

不新增测试代码，不在 tasks 列手动验证。未来实际执行记录必须区分静态、生成/保存和运行证据：

| 场景 | 需要保留的完成证据 |
|---|---|
| 原生编辑与直接API | 创建/配置变量和节点、Get/Set、连接与Undo行为保持；调用不再必经Agent Mapper或整图DTO |
| 完整对象往返 | 所有变量/节点/配置/接口/动态端口/边/布局/内部与外部引用均可读出并重建；不支持内容使导出明确失败 |
| 删除重建与身份 | 相同外部输入下不依赖旧生成GUID；图ID、Variable.ID和内部共享关系恢复，Profile根显式重绑，生成后Pose Get解析正确 |
| 显式非同步 | 人工修改/保存不导出；指定代码重新生成不合并未导出修改；作者工具不自动Build |
| 同次变量交接 | Set(2)后同次Pose读取2，平滑例子得到0.2；Float/Int32/Bool精确且没有第二布局 |
| 实例/时间/错误 | 两Actor隔离，Source Pending保留更新状态，错误不发布部分值，Reset重建变量及原生历史，消费期只读帧不被覆盖 |
| 两项删除 | Agent调用脱离旧模型与motor桥全部消费者迁移各有独立证据，不互相替代 |
| 曲线与发布 | Foot/BlendShape仍沿原链，独立Build/Stale与真实姿势显示分别有证据，不以生成成功冒充运行完成 |

## Current Spec Comparison

| 正式规范或并行计划 | 当前交叉 | 本r2处理 |
|---|---|---|
| remove-agent-authoring-use-native-csharp r2 / character-csharp-authoring | 两个显式工具、完整输出/删除重建、直接API和协议退役是新公共基线 | 复用其公共合同，仅新增事件图读取/输出薄适配，不重复输出器、MCP或中央Validator |
| graph-authoring-domain-framework 的 Authoring节点与Runtime执行描述必须分离 | C# authoring 的同名MODIFIED仍笼统要求领域图编译，与本任务已确认原生事件运行不完整一致 | 本任务delta采用C#作者术语并保留原生EventGraph例外；最终归并必须保留这个合并条款，不能由另一份笼统条款覆盖；不修改对方文档 |
| 现行Agent专属规范与原本任务Document增量 | 现行仍有目录包、五工具、Reconciler等；新基线明确删除 | 撤下本任务 btsmtl-agent-authoring-document-sync 增量，公共删除delta归C# authoring；不再为事件图追加协议目录/版本 |
| character-animation-pipeline | 唯一生产入口与原Pose提交保证仍需明确区分 | 保持事件运行及变量输出delta，不因作者协议改变移动运行职责 |
| refine-pose-graph-readonly-blackboard | Get/条件/BlendSpace/曲线消费与Profile调用适配属Pose，旧桥仍有调用 | 本任务只维护生产合同及D7三份领域文件；条件允许变量和消费者规范由Pose维护 |
| 原生FSM / 共同节点定义 / attribute-driven | 保留类型、稳定端口、元数据、规则与编译，Document扩展退役 | 不再以文档版本为任务依赖，不整体回退已正确作者能力 |
| project.md与既有原生Pose替换实验 | 现行仍强调Program边界，旧Pose原生替换已撤回 | 只保留EventGraph的原生输入生产；Pose继续编译，项目口径由后续正式归并同步 |
| 现行Source Slot/直接资源、旧Shell等差异 | 已有其它作者计划范围，不是协议退役本身 | 不借代码输出回退资源/曲线或重做画布，实际使用当前有效正式API |

本轮仅修改本change的规划与delta，不把公共r2或其它任务的文档收敛声称为已实施。上表同名共享Requirement的归并覆盖风险已明确指出。

## Open Questions

- 公共输出器的确切扩展类型/方法名取决于 C# authoring 的实际交付；职责固定为通用遍历/输出/生成合同，事件图只作完整读取与调用输出薄适配，不保留原Document方案作为备选。
- 具体角色/Fixture、生成范围与明确根绑定参数沿现有内容迁移清单登记，不全局扫描或扩大为重建所有角色。

以上仅是正式接口名称和内容登记，不重开原生runtime、双工具、唯一变量、非同步语义或删除条件。

## Workflow Binding

本次用户广播为单向 PLAN，不发回执、不联系协调/规划/实现窗口，不发送 DOCUMENT_UPDATED 或其它执行消息。原已派生实现关系保留；下列 r2 文档不是新的实施派发。历史 r1 确认与派发内容可由 Git 追溯，不能将本次作者协议修订套用到旧确认记录中。

- planning_revision: r2
- revision_date: 2026-09-13
- revision_source: USER_BROADCAST / 2026-09-13-authoring-r2-plan
- coordination_proposal: 2026-09-13-eventgraph-authoring-r2
- authoring_baseline: remove-agent-authoring-use-native-csharp/design.md r2
- action: PLAN
- planning_document_owner: 01a095f2-ed45-7502-93f7-e9c9df0b7279
- last_dispatched_implementation_revision: r1@3c7ad533273175f25df6bc063fc278a899707e12
- implementation_dispatched_for_r2: false
- planning_document_paths: 本目录 proposal.md、design.md、tasks.md 和 specs 下四份当前规范增量
- implementation_document_path: D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/execution.md

实现仍只修改获授权代码和自己的 execution.md，不修改规划文件。普通工具、编译、验证、提交和完成结果留在实现窗口；只有实际合同矛盾按既有文档实施协议处理。任务5.2“提供合同”通过代码与实现记录完成，不是跨窗口发送进度。

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
implementation_document_path: D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/execution.md
model: gpt-5.6-luna
reasoning_effort: max
```

```text
COORDINATOR_REFERENCE
protocol_version: workflow-coordinator/2
coordinator_thread_id: 01a0962e-319b-7c31-997d-01cd01230536
project_id: local-9736456dbc652f9fbe79876a1464c9fb
```

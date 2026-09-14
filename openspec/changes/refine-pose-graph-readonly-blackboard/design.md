# Design: PoseGraph只读输入与原生Runtime r3

日期：2026-09-14。规划修订：r3。依据用户广播`parallel-20260914-domain-01-planning-update`，已读取协调文档的`PARALLEL-20260914-DOMAIN-01`审阅（提交2801861c1）及主方案D9—D13。本轮只更新规划、任务归属和必要delta，不修改业务代码/资产，不启动实现，不向任何窗口发送执行消息或回执。

- planning_thread_id: 01a09594-1751-7512-b8c0-08b04185055b
- implementation_thread_id: 01a081f3-46f4-7c91-8930-73923ff7950b
- planning_revision: r3
- native_runtime_implementation_dispatch: DISPATCHED_TO_EXISTING_IMPLEMENTATION
- implementation_record: execution.md（已有实现窗口维护，本轮不改）

本目录继续唯一维护原只读Blackboard范围；第1、2组20项均已完成并保持原文。新增原生Runtime接收范围见第11节和tasks第3组，其余旧架构任务不并入。第1—10节的输入、作者API与曲线成果继续保留，原Image/专属编译前置由r3替代。协调文档“待用户决定”是发送本轮PLAN通知前的审阅状态；本轮已经授权登记接收范围，但没有授权开始该实现批次。

### 2026-09-14 实施授权

用户随后明确要求让既有实现窗口执行，现授权PoseGraph实现窗口01a081f3-46f4-7c91-8930-73923ff7950b开始本r3第11节、tasks第3组的18项工作。前文及第11节末尾的“仅规划、未启动”描述的是广播规划阶段，不再作为阻止本次已授权实施的条件。原20项完成记录保持；不创建新窗口，不扩大到主实现拥有的共享Host/装配壳/快照/总Projection或IK算法。

```text
PLANNING_DOCUMENT
planner_thread_id: 01a09594-1751-7512-b8c0-08b04185055b
implementation_thread_id: 01a081f3-46f4-7c91-8930-73923ff7950b
planning_document_paths: D:/Unity_Project_1/3C/openspec/changes/refine-pose-graph-readonly-blackboard/proposal.md; design.md; tasks.md; specs/character-presentation-pose-graph/spec.md
implementation_document_path: D:/Unity_Project_1/3C/openspec/changes/refine-pose-graph-readonly-blackboard/execution.md
confirmed_by_user: true
confirmed_revision: r3 / design section 11 / tasks section 3
```

实现按职责清楚的小步中文提交，仅维护execution.md中的实际改动、提交与未完成边界，不改规划正文，不夹带共享工作树其它改动，不新增测试或验证任务。无需向规划或协调窗口发送收到回执、日常进度或完成汇报。

## 1. 业务目标与输入输出

作者在PoseGraph找到当前图可读取的变量、事实和公开入参，拖出稳定Get并用于混合或条件；Body内明确读取输入姿势的脚权重曲线。改变作者名称不破坏引用，代码重建图后仍绑定同一业务对象。

| 输入类别 | 唯一来源 | 本任务输出/消费 |
|---|---|---|
| 动画实例变量 | 原生EventGraph声明、更新及唯一Contract/Layout/Frame | 只读Blackboard、Get、条件与BlendSpace句柄，不复制声明和布局 |
| 角色表现事实 | 既有同帧Presentation Fact合同 | 保留Fact类型与权限，包括原Identity事实；拒绝Gameplay可变地址 |
| 子图公开入参 | Subgraph/Linked Pose的正式调用接口 | 按调用作用范围绑定，不复制为全局动画变量 |
| source-local曲线/动画属性 | 指定Pose的采样、混合和惯性响应 | 保留曲线列和最终属性消费者，不由EventGraph Set |
| 固定配置/资源 | 正式Pose类型化字段、节点与资源owner | 原生Inspector、正式配置API及公共C#输出薄适配 |

输出包括同一声明的只读界面、typed读取和消费签名、完整对象读取/配置能力与准确诊断。Pose继续组织状态机、采样、曲线、混合及当前正式IK调用，执行载体改为第11节原生Runtime，最终骨骼仍由Final Publication写入；普通共享变量只读不代表Pose节点没有状态。

## 2. 公共所有权与具体调用迁移

| 所有者 | 本轮唯一负责的内容 |
|---|---|
| 事件图任务add-flowcanvas-event-graph | HostEventGraph及其原生操作API、变量声明/更新、EventGraphVariableReference、CharacterAnimationVariableContract/Layout/Frame和发布/租约语义 |
| 本任务 | 已完成Pose作者/只读输入能力；新增原生Graph/Node/Connection/NativePorts、Pose内部运行与节点观察；共享壳的后续改动按第11节交主实现 |
| C# authoring任务 | export_code/generate_assets两个显式MCP、公共对象到代码输出、生成入口及Agent Mapper/DTO/协议退役；输出薄适配消费正式Pose能力 |

`CharacterPoseGraphAuthoringAdapter`已不再拥有或执行事件图内容 Mutation；Pose 只通过 Profile 的正式事件图引用消费唯一 Contract/Layout/Frame。事件图内容由 `HostEventGraph` 原生 API 和公共 C# authoring 入口负责，Pose Mutation 不携带 Agent DTO 或替代 Document 模型。

保留正式Pose类型化修改、Node Definition、Port Shape和领域校验。旧Agent Document、五生命周期、专属Validator、Exporter/Reconciler与反向导出不再成为创建、保存、导出或生成的前置条件。只存在旧Agent层的必要业务规则归回现有Pose修改或编译入口，不增加中央Validator、整包同步事务、反射私有字段或第二Pose模型。

## 3. 唯一变量合同与输入装配

共享变量引用使用同一图业务ID和原生Variable.ID；显示名不参与寻址。Float、Int32、Bool保持精确类型，Int32/Bool不通过float中转。Contract/Layout/Frame由事件图唯一生成，Pose输入合同只能持有其引用及消费者绑定，不能重新排列另一份全局变量列或复制初值作为运行来源。

CharacterAnimationInputContract保留正式Fact、Slot、World能力、子图调用入参和source-local曲线。共享实例变量部分引用事件图合同和唯一布局；Curve与Fact不能因为同名或同为Float被合并进事件图变量。已完成的root.Parameters清理保持；原生绑定继续读取正式输入和曲线合同，不重新复制图内变量声明或补默认motor值。

原生Pose准备只需要合法图、资源/Rig绑定、共享变量合同和其它正式输入声明，不先编译SkillGraphs或生成Pose Image。准备只解析现有节点/引用、类型、资源和实例容量，不生成IR、全图操作表或调度计划；角色装配在创建实例与每帧调用时提供实际Actor和同次发布的变量帧。

## 4. 同次发布、类型、作用范围与条件

Get、Transition Rule和BlendSpace必须消费同次成功发布的typed变量帧。该帧带动画实例/Actor、表现采样、Simulation sample tick、Reset代际及图/合同/layout版本；读取前在现有交接边界检查一致性。事件图消费者完成前不能重写输出，Pose节点及其内部算法只读取冻结typed值，不持有可变Blackboard Variable对象。

Source Pending不回退已成功更新的事件图变量或原生节点状态。下一次更新按正式表现时间继续，不能重放旧更新。Pose按自身Pending/Committed和Fault边界执行，输入发布成功不能作为最终姿势已提交、已显示或Writer成功的证据。观察明确区分原生更新、变量输入发布与最终Pose提交。

Transition Rule新增同次动画变量读取，同时保留同帧Fact、TimeInState、StatePoseRemainingTime、短路求值、priority、stable order和MaxTransitionsPerFrame。变量Get不是写入口；禁止读取Gameplay Blackboard mutable address、ActionInstance、Timeline operation、Unity Transform或World query。

Root、StatePose、Subgraph、Linked Pose只展示接口允许的输入；共享变量不在每张图重建声明，子图入参不变成全局可写变量。合法未使用声明可以标记/筛选，但不能因暂未连线删除。主要显示名称、类型、来源、范围和使用情况，内部身份留在详情诊断。

## 5. Body曲线与已有实现保护

Body内FootPlacement Weight读取指定输入Pose经过上游正式混合后的曲线值。有真实外部控制需求时使用明确公开输入，不按节点名称或固定字符串猜来源。曲线绑定和通用曲线读取可以是不同作者表达，但必须进入同一曲线来源与执行语义，不能各自采样。

保留现有Foot、Pelvis、FBBIK和曲线数学；脚权重只控制既有Goal可见权重，不释放Anchor、清零连续历史或改变Landing Reach准入。已有输入Pose曲线门禁、显示名修正和属性声明拆分不回退。这些r2成果已完成。新增原生运行只更换执行接入，IK算法和私有状态继续由IK任务的当前正式接口拥有，不用历史Constraint版本冻结其已获准的新实现。

## 6. 完整C#导出、生成与业务身份

两个公共操作由C# authoring任务实现：`btsmtl.export_code`读取当前资产完整导出C#，`btsmtl.generate_assets`执行对应已编译代码，重建并保存明确生成范围。本任务提供Pose对象的正式读取与配置API，公共输出适配使用这些API，不先构造Agent JSON、Source Snapshot或第二份节点模型。

完整往返覆盖图角色、状态/规则、节点类型和字段、Get稳定变量引用、完整曲线键/切线/权重/WeightedMode/wrap、布局、固定/条件/动态端口、端口顺序、资源和跨对象引用。未知字段、资源或端口不能静默省略；输出完整性由输出服务检查，业务有效性仍由现有Pose领域规则检查。

图、节点、变量和端口的业务ID在重建后保持一致；物理Unity对象、GUID和local file ID可以变化。生成范围内引用必须使用本次创建的新对象，Get仍绑定同一图/变量业务引用；真正范围外资源通过明确参数或正式资源身份提供。Profile/Definition的明确根挂接必须恢复，不能只生成孤立图，也不能依赖旧生成子资产GUID找回范围内内容。

人工编辑/保存不自动写源码。重新生成不自动合并未导出的人工修改，不新增rebase、源码同步或增长日志。生成范围外资产不被扫描删除，现有Undo与保存机制保持。变更按真实图/资源依赖显示Stale并显式准备/采用新实例。r3不再要求Pose Image编译或旧整角色Build；ACL等真实资源产品仍按所属领域显式构建。export_code/generate_assets不自动Build，公共生成入口由C# authoring原owner维护。

## 7. 运行与Preview消费迁移

完整Pose/角色Preview使用同一CharacterAnimationEventGraphHost和唯一变量Contract/Layout/Frame，携带同一实例、采样、tick、Reset与版本身份，不创建固定motor桥或Preview私有变量更新器。普通Play观察仍只消费真实角色和已发布事实，不取得时钟或执行权。

单AnimationClip/BlendSpace等资源查看使用原正式资源调参合同，不能为缺少角色变量帧补默认motor值，也不强迫单资源工具构造完整角色/事件图。两类入口按业务目的区分；不得以保留CharacterPresentationProgramParameterFrame.FromDirect作为资源Preview长期兼容路径。

r2已完成CharacterSimulationPresentationRuntime、Pose帧协调、Program/StateSource/BlendSpace、条件/Get及AnimationPreviewEngine等旧消费签名迁移；这些完成事实保留，r3共享表现壳和预览协调器的后续写入分别归主实现与预览owner。旧 `CharacterPresentationProgramParameterFrame` 及其旧生产方法已无业务代码引用并完成删除。执行记录保留精确迁移文件清单和剩余引用。

## 8. r2已完成实现顺序与完成定义

1. 消费事件图唯一公共变量类型和原生操作API，明确本任务拥有的Adapter、Mutation和输入签名。
2. 删除Pose对Agent Mapper/DTO的依赖，保留正式领域修改和校验；接通输入合同中的共享实例变量引用。
3. 迁移Get、条件、BlendSpace以及运行和完整Preview到同次typed帧，保留资源查看的原合同。
4. 补齐Pose对象完整读取/配置、稳定业务ID和新对象引用恢复，供公共代码输出/生成使用。
5. 迁移完消费签名后，由事件图任务删除旧生产类型；C# authoring任务退役Agent协议。三者通过正式文档和代码依赖对齐，不建立临时接口。
6. 在明确生成范围中清理废弃声明、端口与无引用重复子图，恢复根挂接；既有Build单独发布产物。

设计/证据条件：两实例隔离；精确类型和跨范围错误可定位；同次Get/条件/BlendSpace输入一致；Source Pending不回退事件状态；完整Preview与运行同合同；导出/删除重建后业务ID、曲线、布局、动态端口、资源和根挂接等价；旧Agent调用和固定motor消费签名消失。条件留在本设计与执行证据，不新增测试或手动验证tasks。

## 9. r2规范对接记录

- 现行Transition仅允许Fact/时间的条款在本change delta中补充typed动画变量帧；Gameplay mutable address禁令、时间范围和状态机选择规则保留。
- 旧committed parameter措辞细分为输入发布与Pose提交，不能让Source Pending反向改写事件图状态。
- 原生EventGraph执行已按用户广播确定，不再保留“待选择原生/编译两种模式”的规划阻塞。事件图具体API仍由其唯一维护者提供，不在本任务设计第二套合同。
- 旧`integrate-pose-flowcanvas-editor-preview`的公共输入与authoring协议条款由本轮对接，旧任务其它作者组织、资源与Action/Slot业务保持原归属；独立Pose编译方式由r3原生运行替代，IK算法按当前owner接入。目录内未发现另一明确planner身份；本轮依据用户指定只修改相关规划条款，实施记录与原代码不改。
- 旧方案中Document v7升级、整包同步和反向导出delta退出待实施规范。Agent能力删除及共享目录的Document要求退役由C# authoring change唯一拥有；旧Pose方案不再次修改已退役Requirement或重新安装协议。
- 其它窗口尚在更新的公共条款按本广播及C# authoring r2解释；例如笼统“领域图都必须编译”不能重新施加到原生事件图。若相关文件仍残留旧句，记录其所有者和依赖，不修改其它规划窗口文件。

## 10. r2历史边界

r2的公共作者协议迁移已完成，证据留在execution.md；其中Document路径是当时事实，不是后续执行入口。r3不改写该历史，也不重新打开已完成只读任务。

## 11. r3原生FlowCanvas Pose Runtime接收范围

### 11.1 接收与保护边界

来源为主方案D13的4.1—4.6、5.1—5.8、6.1中Pose消费、7.3节点观察和8.3 Image专属链清理。唯一可勾选清单是本目录tasks.md第3组；主方案中的原编号仅为来源映射，其owner负责保留责任指针与公共集成项，不在两处并行实施。新增Runtime尚未实现，不能从第1/2组20项完成推断本组完成。

保留已完成的只读Blackboard、Get/条件/BlendSpace唯一变量引用、精确Float/Int32/Bool、source-local曲线及Body内部脚权重。EventGraph仍唯一更新动画变量；Source Pending不回退已发布事件状态。有限动作由原ActionPlayback/Slot接收技能/Timeline的明确请求，不经EventGraph转发，不重新仲裁Gameplay动作。

Pose使用FlowScript原生初始化、真实Node/Connection/Port和Manual驱动，不在加载或首次帧生成Image、IR、操作表或另一套Runtime图DTO。字段、端口、规则和资源依赖继续来自同一正式领域定义；只建立实际原生实例、资源绑定、节点状态和本次缓存。

### 11.2 当前缺口与文件唯一写入者

本轮只读源码可见CharacterPoseCanvasGraph.OnGraphInitialize仍抛出拒绝原生执行；CharacterPoseCanvasNativePorts处于UNITY_EDITOR条件内，并包含LocalPose/ComponentPose等占位类型及抛异常的输出getter。它们说明运行接入尚待实施，不影响此前Blackboard和C#作者能力已经完成的记录。

下表路径均相对`3cDemo/Client/3C_Client/Assets/GameScripts/Main/`，按文件/符号分责，不把整个Animation目录交给同一任务。

| 文件/符号 | 唯一实现owner | 本任务交接方式 |
|---|---|---|
| Runtime/Character/Pipeline/Animation/Contracts/Pose/CharacterPoseCanvasGraph.cs、CharacterPoseCanvasNode.cs、CharacterPoseCanvasConnection.cs、CharacterPoseCanvasNativePorts.cs | 既有Pose实现01a081f3-46f4-7c91-8930-73923ff7950b | 实际原生初始化、typed端口和连接、每实例执行 |
| Animation/PoseGraph内部实例、阶段执行、状态、缓冲和节点观察；其内部Composition入口 | Pose实现 | 只装配Pose实例及注入的领域服务，不接管角色Factory |
| CharacterPoseCompilerModule、CharacterPoseCompilationContracts、PoseGraph/Passes、ProgramImage/ExecutionView/Operation页面及Image专属Worker编排 | Pose实现 | 先保留/迁出真实节点算法与规则，再删除专属发射、表和消费者，不按目录盲删 |
| Source/Constraint/Final Publication与Player/Blend/Phase等当前正式服务 | 原领域算法owner保持；Pose只修改明确的调用和存储适配 | 复用真实服务；IK公式/私有状态归IK任务，ACL/资源算法不重写 |
| CharacterPresentationRuntime.cs、CharacterSimulationPresentationRuntime.cs、CharacterPresentationRuntimeFactory及旧总Projection挂接 | 主实现01a09a5f-8d64-7c11-a461-7889623b7459 | Pose提供本节生命周期签名、实际返回结果和旧引用清单；不同时写共享壳 |
| CharacterPipelineHost、CharacterDomainRuntimeFactory、角色Step/state codec、网络checkpoint/manifest、共享Program/artifact和Character Build | 主实现 | Pose不增加角色快照、不改网络Pass；网络重放不推进Pose |
| ScenePlay协调器、会话与观察控制 | 预览原owner | 消费Pose公开准备/采用/观察，不创建另一工厂、时钟或fake ProgramEpoch |
| Camera资源/绑定/采用；Motion源/Clip/Warp映射；公共C#输出/生成入口和同批Corin生成源码 | 各既有owner | Pose提供正式节点/资源接口及字段需求，不覆盖其代码或重复源迁移 |

共享文件中的Pose调用由主实现一次接入；即便上一阶段由本任务改过这些文件，本轮也按上述owner执行。源码、字段或资源接口存在实际冲突时保留现场，由原owner处理，不用临时桥补齐。编译/Unity刷新由指定执行owner组织，编译期间不改源码或重复刷新；不新增锁服务、测试或验证任务。

### 11.3 公开准备与采用合同

以下是需要交付的类型化操作合同，不是声明当前已有同名方法。最终可复用已有正式类型/命名，但不能省略身份、状态、资源和完成含义；不使用object字典、空实现或另一份可执行总包。

| 操作 | 输入 | 输出与职责 |
|---|---|---|
| PrepareGraph | RequestId、GraphId/GraphRevision、明确图引用、Rig/资源版本及绑定、动画输入合同、目标Actor/角色能力 | PosePrepareResult：原请求身份、Status、typed原因（来源/字段/资源/原因码）；仅Ready含PosePreparedBinding |
| CreateInstance / ReplaceInstance | Ready绑定、明确Actor/实例上下文、当前采用generation和替换原因 | Pose实际创建原生图并确认安装；成功发布PoseAdoptedResult，包含实际GraphRevision、资源版本、InstanceId、ResetGeneration |
| BeginFrame / PrepareFrame | 真实已采用实例、本次表现采样/Simulation tick/delta、Body/Intent、同次EventGraph只读Frame租约、有限动作请求、World能力 | 当前PoseFrameLease和PosePreparationResult；包含本次活跃分支/source demand、准备状态和可用于后续Barrier的正式源结果引用 |
| Evaluate | 同一FrameLease、同次Source完成结果和唯一Animancer Barrier完成凭据 | PoseEvaluationResult；仅完整成功时含本帧候选姿态、曲线与完成身份，不宣称已写骨骼 |
| Commit | 完整EvaluationResult及主表现帧允许提交的令牌 | 调用唯一Final Publication完成最终校验/写入和原no-throw Seal；发布实际PosePublicationResult |
| Discard | FrameLease及失败阶段/原因 | 按原owner撤销Pose节点和领域服务的Pending状态，释放本帧租约；不回滚EventGraph已经成功发布的状态 |
| Stop / Dispose | 精确InstanceId/generation及停止原因 | 先拒绝新调用，再完成在途工作、失效旧结果和释放原生图/资源/缓冲；返回实际停止结果 |
| Observe | InstanceId、Node/Port、子图调用身份、订阅兴趣 | 只返回对应阶段已经完成且允许发布的结果/租约；不重新求值或创建Image |

公共Status语义沿主方案D10，不另建同义状态集。Pending必须来自真实资源或绑定准备未完成；Missing/Invalid/Failed分别携带明确原因，不能读旧Image假装Ready。准备失败不更改当前活动实例和实际采用版本。Ready只表示可以建立实例，不是Adopted。

实际采用结果由Pose owner在安装成功后发布，主实现只转交和汇集。替换先准备合法新实例及资源，再在明确边界停止旧实例、切换并重置Pose历史；初始化失败不发布新采用身份。旧实例被停止后的异常必须如实返回停止/故障状态，不能伪称旧实例仍活动。Camera采用不借Pose Reset代替，预览不根据资产已保存自行增加版本。

### 11.4 同一原生图的准备与求值

主表现装配壳按已有时钟依次触发EventGraph发布、Pose Prepare、唯一Source/Animancer Barrier、Pose Evaluate和Commit/Discard。主实现修改调用壳，实际采样由原Source服务完成；Pose只接收同次Barrier凭据，不再Evaluate一次PlayableGraph。

Prepare通过原生连接确定当前活跃状态/分支及source demand，只推进属于本阶段的节点Pending控制状态。Source完成资源和采样准备后，Evaluate沿相同原生连接读取本次样本并计算姿态/曲线/约束。Prepare返回的句柄不能作为完成姿态输出，ValueInput也不能把未Ready输入默认为零。

每个节点的缓存键包含Actor实例、图调用实例、本次求值身份和阶段。Prepare与Evaluate各有自己的结果与失败状态；同阶段重复getter读取只复用结果，Player时间和状态转换不能在两阶段各推进一次。缓存只存本次结果、引用和完成身份，不保存线性Operation表、全图计划或第二份拓扑。

StatePose/Subgraph调用分别持有需要独立的历史；Linked Pose只按已存在的显式组/调用合同共享，不能因引用同一资产就跨Actor共享。资源准备失败、图递归或节点错误应返回typed失败，不靠Console日志判断成功，也不吞异常后继续返回旧getter值。

### 11.5 业务状态、缓冲和提交

Player、PoseState、Slot、BlendStack、Inertialization、Phase与Linked Pose保留原时间、权重、relevance、continuation、readiness及capture/release行为。PoseState业务节点调用已有动画状态逻辑，不改成通用插件FSM。原生实例的作者字段与运行历史分离；失败帧不把半更新节点历史提交为下一帧状态，不用每帧反射快照、克隆整图或重放补救。

输入姿态只读，修改节点使用独立输出。按实例初始化复用缓冲或使用现有领域pool，容量来自Rig、节点声明及真实资源需求；容量不足明确失败，不调用全图Value Lifetime/Workspace Compiler重新规划。别名分支不能原地覆盖共同输入。缓存只持有结果/租约，不能复制Source资源表、Foot Context或FBBIK持久历史。

Source唯一持有ACL/Playable及采样/释放；Constraint唯一持有当前IK算法的Foot/Goal/FBBIK状态；Final Publication唯一持有最终骨骼输出与物理写入。Pose节点不得直接写Transform。节点内部已有Native/Job算法可以继续使用，由算法owner完成自身工作并在读取/释放前完成；删除的是旧跨节点/跨ActorProgram Worker编排，不是全部Job能力。

Commit前完成可失败的输入、候选姿态、整Rig和资源检查。Barrier前失败沿原Discard规则保持Committed结果；Barrier内/后及Writer异常沿原Fault规则，不承诺回滚已经发生的物理骨骼写入。成功Writer后只做原安全Seal，观察只发布允许的完成结果。Stop/Replacement/Dispose必须拒绝旧generation晚到结果，依次停止调用、完成在途工作、解除订阅和释放租约/缓冲/资源。

### 11.6 当前IK、观察与资源边界

IK任务继续推进自己的正式算法和状态，本迁移只适配其当前接口，不冻结过去的Constraint类或旧算法，不保存另一份IK状态。Goal合法性和一次FBBIK/最终Writer约束保持；输入顺序或配置存在实际差异时在实现记录定位，不擅自改变数学补偿迁移。

节点观察使用真实Graph/Node/Port业务ID、调用实例、阶段和完成身份，不再依赖编译Operation index或旧SourceMap。跨版本、Reset或已释放租约的结果不可继续显示；不能为了Pose Watch重新采样、执行IK或生成隐藏Image。ScenePlay只管理会话、选择和订阅，由预览owner接本任务返回的真实Prepared/Adopted/运行事实。

准备状态与实际采用状态可以分别显示；Pose Watch的姿态、曲线和约束可视结果只在本帧成功提交后发布。已完成的准备阶段或候选求值不能伪装成已提交Pose，失败帧的候选结果不覆盖最后合法结果。

保留现有ACL、Motion Matching、Foot与Rig资源产品和准备服务，取消Pose图编译不取消资源构建。新图/资源版本通过显式领域准备与实际采用生效，不自动改运行实例，不调用旧Character Build获得缺失Pose数据。生成源码和资产重建继续使用既有正式C# API，由其owner更新受影响输出适配。

### 11.7 删除顺序与唯一实施映射

| 主方案来源 | 本任务唯一清单 | 本任务边界 |
|---|---|---|
| D10准备/采用与D13公共Pose接口 | 3.1、3.15 | 提供真实领域结果；共享壳安装调用由主实现接 |
| 4.1—4.6 | 3.2—3.7 | 原生Graph/Node/Connection/NativePorts、实例、缓存与图规则 |
| 5.1—5.8 | 3.8—3.15 | 两阶段、状态、源/约束/最终发布、缓冲与生命周期 |
| 6.1中的Pose消费 | 3.16 | 原生端口复用已完成EventGraph合同；不重新做变量系统 |
| 7.3 | 3.17 | 节点结果与观察；预览会话和共享UI壳由原owner接入 |
| 8.3 | 3.18 | Image专属IR/Compiler/ExecutionView/Worker/产物链删除 |

先形成真实原生实例与阶段接口，再迁入现有业务节点/算法服务，并提供共享壳需要的调用与旧引用清单。主实现完成一次Host/表现壳和旧总Projection挂接后，Pose删除无消费者的Image专属代码与数据；混合文件内仍有效的Source、资源解析、曲线或IK算法先按原owner迁出，不复制另一份。

Image专属删除包括CharacterPoseCompilerModule及只服务其IR/Pass的入口、ProgramImage/ExecutionView/Operation镜像、全图Workspace/ValueLifetime规划和Worker调度。实际仍使用的标量资源、曲线、Rig绑定和Native算子不得因名称中含Compiler/Program被盲删。共享Character Build、角色Program/Projection容器、Host、快照和网络代码只由主实现删除/接线，Pose不给它们加兼容层。

不会在加载、第一次求值、Preview或C#生成中重建Image；缺失原生节点/资源时明确未完成或失败，不保留旧reader、默认Idle、第二变量更新器或原生/编译双模式。单独节点算法需要内部准备的数据仍由该算法模块管理，不能借此恢复全图Compiler。

### 11.8 现行spec冲突与替代归属

| 现行/活跃条款 | r3明确替代 | 规范增量owner |
|---|---|---|
| character-presentation-pose-graph：固定Compiler Pass、ProgramImage、State平面plan、编译Routing/SourceMap | 原生节点/连接与实际实例绑定，保留类型、递归、readiness、时间与曲线规则 | 主领域运行change持有旧运行条款替代；本delta补输入与领域接口 |
| character-pose-plan-compilation：独立Pose Compiler、IR、ValueLifetime、Workspace/Worker Plan及Image Seal | 删除专属载体；正式原生图规则、节点缓存和实例缓冲接管实际约束 | 主方案已提供REMOVED/ADDED delta，本任务不复制 |
| character-pose-graph-runtime-architecture：Program Runtime/Image/ExecutionView与跨ActorWorker | 原生托管图调度，Source/Constraint/Final Publication继续唯一，内部Job由算法owner管理 | 主方案runtime/native-flowcanvas-pose-runtime增量 |
| 本目录r2：保留原Compiler/独立Pose编译 | 已完成阶段的历史事实保留；r3不再要求该执行方式 | 本目录proposal/design/delta，已完成tasks不重开 |
| 主方案PoseStateMachine delta仍只有Fact/时间 | 保留本任务已交付的同次typed动画变量条件；不得在归并时丢失 | 本目录完整输入delta；主方案owner需保留该责任指针 |
| D10“结果发布者”表述与协调审阅R7 | Pose实际安装后发布Adopted；主实现汇集，预览只读 | 本节接口与本delta；不修改主方案文件 |
| 主方案D13仍有来源任务checkbox | 接收工作唯一可勾选清单为本目录第3组；主方案保留来源和集成责任 | 主方案owner维护其文档，本轮不越权改写 |
| 旧integrate-pose-flowcanvas-editor-preview与其它活跃方案的Image/独立Compile要求 | 列为待相应owner随原生接口迁移的旧前提，不用旧描述恢复Image | 本轮仅记录依赖，不覆盖其它规划 |

本次没有修改current specs；它们仍描述切换前合同。主方案负责广义运行替代，本change只补Pose领域接口及输入要求，正式归并必须保留双方不重复的内容，不能用整块覆盖抹掉已完成输入能力。

### 11.9 业务取舍、完成定义与本轮限制

直接原生执行减少IR/Image和自建全图执行器，代价是端口调用、每实例节点状态和缓冲峰值可能增加，不承诺性能等价。保留预编译Image能保留全图优化，却继续维护第二执行表示；加载期生成Image也保留Compiler，均不是本轮路线。保留原帧事务和领域资源服务有必要的接入成本，但不形成新图表示。

完成后的角色和正式Preview通过同一公开Pose实例接口读取真实图/资源版本，沿同一时钟和Barrier完成准备、求值、提交或失败处理；同一阶段重复读取不重复业务，子图/Actor状态隔离，观察来自真实完成结果，Image专属链已经退出且共享壳只由主实现接入。输入、曲线和C# authoring已完成成果保持；IK通过其当前正式接口接续。

上述是设计/证据条件，不是测试或验证tasks。本轮仅登记接收范围、配对和规范增量，不运行Unity、Build，不修改业务代码/资产，不派发实现、不新建窗口、不向其它窗口发送任何消息。

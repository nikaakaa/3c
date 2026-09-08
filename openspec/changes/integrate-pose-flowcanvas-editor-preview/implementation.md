# Pose FlowCanvas编辑器实施记录

## 当前实现状态（2026-09-08，代码优先）

按照作者最新要求，tasks已删除检查、证据、性能对照和验收任务，只保留实现、清理及产物发布。作者资产格式和identity没有变化，已撤去两项没有必要的迁移任务。清单现在为21项，其中20项代码与文档工作完成，仅3.3的v28产物发布未完成。历史6/26和20/23描述仅记录此前进度，不代表当前清单。

没有新增测试、运行回放或迁移业务资产。编译和运行检查的历史结果保留在下方，与实现完成状态分开记录；代码完成不宣称产物已经发布或作者已经验收。

### 正式v28发布尝试

用户确认“开始”后，调用正式`character.build_fixed_products`，目标Definition为`Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`，Fixed wrapper为`Assets/Configs/Simulation/DeterministicRollback/Programs/CorinFixedProgram.asset`。该入口统一发布Float32、Fixed和共享Projection，不重复启动两次Build，不修改作者数据。

实际返回`success=false`、`character_build_failed`，失败阶段为AuthoringDiscovery。三条`skill_entry_graph_missing`分别对应Attack、DodgeBack、DodgeForward：它们的技能入口尚未进入Definition正式SkillGraphs。遵守用户不处理其它领域报错的范围，不修改技能配置、不绕过共享编译链，也未将Build任务勾选完成。

构建前后读取Definition、Pose作者资产、Float32 Program、Fixed Program及Presentation Projection的SHA-256，五个文件均未变化。原始结果为`.codex-tmp/canvas-core/pose-v28-build-result.json`，前后记录为同目录`pose-v28-build-before.json`和`pose-v28-build-after.json`。这次没有发布新产物。

### 本轮代码链

| 作者操作／输入 | 实现与结果 |
|---|---|
| 修改状态名称或进入重置策略 | State Capability → Details → SetPoseStateFieldMutation → 原状态owner；保留StateId、子图和转换引用。 |
| 修改状态别名名称和成员 | 共享别名详情 → ConfigureStateAlias → 既有ConfigurePoseStateMachineMutation；成员使用正式State／Alias identity，空成员、未知成员及循环引用进入原事务约束。Document继续保存原aliases正文。 |
| 修改节点名称、普通参数或策略参数失败 | Mutation失败返回到当前详情，重新读取正式值后显示错误；不再以“已处理”吞掉失败。转换duration请求也进入正确的SetTransitionField分支。 |
| 保存图和调参策略 | 显式保存序列化图、根owner、Profile及当前图引用的Foot／FBBIK／Blend／Inertialization调参owner；不调用全项目SaveAssets或Build。这里只补实现，本轮未触发保存。 |
| 修改状态、转换或条件 | 版本变化后刷新同一原生文档视图，更新标题、转换标签及端口，保留仍存在的NodeId／EdgeId选择。 |
| 查看转换条件运行结果 | Rule Compiler保留作者OperationId；正式求值记录实际输入读取位，完成结果带编译状态机NodeId、作者OperationId、ReadInputA／ReadInputB。只读projector按当前调用和转换匹配，当前条件与目标预判分开选择。 |
| And／Or短路 | 只有RequireBool实际读取时才记录输入位，保留原短路语义；UI显示本次未读取、未采集、False及0各自含义，边高亮来自ReadInput记录。 |
| 修改子图或Linked图后继续观察 | Source Map保留每张图GraphRevision，窗口核对所有相关作者图版本、PlanHash和Rig版本，停止版本失配结果叠加。调参变化记录同时覆盖Linked作者图。 |

运行算法仍属于现有Native／Job及状态机求值链。本轮运行层只增加诊断来源字段和读取标记，没有新播放器、假输入或Editor侧求值。Pose Program Image为v28，Transition Rule合同为v3；旧v27产物不会被当作新字段齐全的产物读取，等待最终统一Build。

共享请求和UI合同没有新增Agent Document版本：状态name和aliases原本已在v5中，Editor和Document最终进入相同Presentation Mutation owner。原生角色绑定、完成帧租约、Pose Watch容量、关闭／退出／重载解绑沿用既有实现。

本轮代码提交：`b16397bee`提供条件观察来源、读取标记与共享别名展开；`70ee5b50f`完成作者详情、条件显示、版本绑定及保存owner接入。别名展开算法从编译器移到唯一CharacterPoseStateAliasResolver，配置请求在提交前调用，正式作者约束和编译复用同一实现。

代码检查记录单独放在本节：Unity刷新过程中发生过Domain Reload断连，随后恢复，最近一次Console读取为零错误；未新增测试，未执行回放、业务asset apply或Character Build。OpenSpec格式检查通过。此记录不占tasks项目，也不代表用户已完成端到端验收。

## 基线与归属

- 提案：`integrate-pose-flowcanvas-editor-preview`；目录：`D:/Unity_Project_1/3C`。
- 本轮读取的HEAD：`f74e9b2aba6d4d9d4476b48933b5fe01cfb29b2c`。工作区存在其它任务及此前Pose准备改动，不是干净checkout。
- 精确Definition：`Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`。
- 作者资产：`Assets/Configs/Character/Corin/Pipeline/Presentation/PoseGraphs/CorinPresentationPoseGraph.asset`。
- 现有Program／Projection为Definition同目录Generated中的正式资产。本轮没有重建它们。

| 文件 | 本轮读取时SHA-256 |
|---|---|
| Definition | `005e281c11a2993e3256a2b9b5064cd17bf498720897520a7c7bb23a42856768` |
| Pose作者资产 | `2766503f845a89d7b743a2bd29d4381d3650abbd8a026c55fde35305920e7a39` |
| PresentationProjection | `a14e8124db25d26ad784e73d587f5529f947dc870cd2648136d69f827e983b1f` |
| SimulationProgram | `f5bf7bc97e38da263afdd2e339c6067996fdcabb8a824fcaeefd49bfc8c7ff76` |

本机读取记录：`.codex-tmp/canvas-core/pose-editor-apply-baseline.json`。这些哈希只是区分工作区内容，不证明行为或性能通过。

| 区域 | 本提案职责 | 技能侧边界 |
|---|---|---|
| Pose作者Graph／Node／Connection、窗口和详情 | 原生UI、同一Mutation、图闭包和Play观察 | 不由技能迁移替换 |
| Pose Compiler及Source Map | 直接消费同一作者图、增加必要观察映射 | 技能Semantic IR／发射仍归技能侧 |
| 共享作者合同 | 复用已分离的TreeDesigner.Authoring类型及唯一Port Shape | 不复制第二份合同，不改变技能业务 |
| FlowCanvas／CanvasCore钩子 | 最小领域无关的原生创建／改接和外部观察接入；修改前核对文件 | 技能侧复用同一钩子，实际同段冲突交给作者决定 |
| Document | Pose owner与现行业务分片适配 | 技能Macro／v6协议迁移仍归技能侧；此处不创建协议分支 |
| Runtime | 保留Program Image、Native页、Worker、Source／Constraint／Final Publication | 不启用FlowCanvas角色执行，不改技能runtime |

最新用户指令允许刷新Unity检查本批接入。只修复Pose／FlowCanvas接入造成的错误，其它领域错误记录但不修改；若阻止程序集加载，明确报告验证受阻。整根Build与回放仍按收口阶段实际条件执行，未执行不勾选通过。

## 历史证据的使用边界

此前已完成一次空节点清理、正式Document apply和Float32 Build，原始日志位于`.codex-tmp/canvas-core/`。当前源码已有后续变化，因此这些日志不能代替本提案的修改后验证。当前仍没有本提案的作者交互、同输入回放或观察开销闭环证据。

## 本轮代码与验证记录

代码提交 `41ac26f1a`。本轮继续实施，不视为全部任务完成，不归档。

- Graph、Node、BinderConnection 已使用原生 FlowCanvas 基类。保持原有序列化类型名、稳定 ID、GraphCatalog 与 Payload；编译器直接消费它们，没有作者图中转或原生 getter 求值。
- 原生创建、连接、改接与批量断口进入 typed Mutation；布局移动原地修改并由真实 Graph owner 记录 Undo，选择与重绘不标脏。
- 状态／规则页面也使用原生 FlowGraph / FlowNode / BinderConnection 的不持久化视图；删除旧自绘端口和拖线器。
- 独立窗口播放器、假速度／Grounded 输入、Seek 和预览角色面板已移除。Profile 入口按正式 Definition 引用解析，不再要求 Preview Fixture。
- 普通 Play 通过运行目标 Registry 精确绑定 Host.Definition 与 Pose 资产；窗口各自拥有兴趣 ID，只有唯一匹配实例才自动选择。下钻利用 Compiler 同一 Scope 函数及已发布 SourceMap 定位；多调用不混合结果。
- 原生节点与端口悬停只读取 10 Hz 更新得到的显示文本。Pose Watch 复用正式接口，每窗口上限 8 项；不调用 Job.Complete，不读 Pending 页，不执行作者 getter。
- 输出端口元数据新增到 Pose Plan v27，含作者节点、调用范围、端口种类和工作区索引。没有边读取证据时不播放边动画；未采集值不显示成零。

### 已完成的正式 Document 检查

同一精确 Corin Definition 的 checkout 与未修改正文 dry-run 都成功，`syncState=Clean`。dry-run 的 `plannedDiff=[]`、`touchedOwners=[]`、`diffSize=0`。

- SourceRevision：`5f692b49fd460f45732b1fb707a33afda75c4493c89bafeff7eeabe532577479`
- DocumentHash：`2562bc4d067334f8016bd326210ff48cdc24cdc98c1ef34e759130d9270836f2`
- PlanHash：`4f53cda18c2baa0c0354bb5f9a3ecbe5ed12ab4d8e11ba873c2f11161202b945`
- Package：`3cDemo/Client/3C_Client/AgentAuthoring/Documents/CharacterController/c7a7c1e3-001dd30a08d99da6.btsmtl`

无业务修改，因此上述动作是 `applied=false / saved=false`，不能写成资产 apply 成功。它证明的是导出／对账没有伪修改，不是交互操作全部验证通过。

### 编译和构建证据边界

本轮 Unity 刷新中出现的 Pose 接入编译错误已修正，之后 Console 返回零错误。一次中间 v26 Build 在 Editor.log 记录编译 89509 ms、发布及收尾 15764 ms、总计 105273 ms；产物中实际读取到 v26 与 25 条作者节点 SourceMap。它不替代最终 v27 构建验证。

Editor 在程序集重载期间曾无响应并导致 CLI 超时，随后自行恢复；未关闭或重启用户 Editor。最终 v27 Build 及运行检查结果继续在下方登记。

### 最终 v27 Build

正式 `character.build_float32_products` 返回 `success=true`，消息为精确 Float32 Program 与 Presentation Projection 已发布。结果对应同一 Corin Definition：

- ProgramHash：`847605d62325acc923ac06f70ac685bc058e0c28f96eedc7976b4d7ab077dd92`
- ProjectionRevision：`d97b4d79323aefbf04df5a98d41c12421d5b27afabcb3c0d172de6e609b659aa`
- SourceRevision：`c8a078e38b09eaa4457f38b781f168b54efbb0a0d590872d9db7d21f98d0f16c`
- NumericProfile：`float32-ieee754`；Target ABI：8。
- 实际产物 `m_SchemaVersion=character-presentation-pose-plan/v27`，25 条 SourceMap、24 个输出端口来源、8 个调用范围。来源摘录为 `.codex-tmp/canvas-core/pose-v27-source-map.json`。
- Build 信息只有 Float32 常量舍入与 ABI 说明，没有构建错误。生成资产此前已有工作区修改，本批未把这些混合资产改动整包提交。

### 观察生命周期

| 条件 | 窗口行为 |
|---|---|
| Edit Mode | 显示未播放，允许作者编辑，不创建角色 |
| Play 且没有匹配 Host | 等待运行角色 |
| 同 Definition 唯一匹配 Host | 自动绑定 Registry 的具体 RuntimeInstanceId |
| 多个匹配 Host | 等待明确选择，列表带实例 ID 摘要 |
| 目标有效且产物版本一致 | 读取完成帧，10 Hz 更新纯显示文本 |
| 作者／产物版本变化 | 停止旧值叠加，显示版本不匹配 |
| Unity Pause | 不推进时钟，读取最后完成帧 |
| 目标移除、退出 Play、关闭窗口、脚本重载 | 解除该窗口 Diagnostics 与 Pose Watch 兴趣，不缓存 Native 页 |

真正显示的数据包括已完成节点可用性、权重、完成帧、已采集参数、已订阅 Pose Watch；输出端口值从 SourceMap 指定类型与索引关联。没有边读取采集时不画执行动画。上表为实现路径说明，不能替代每种场景的实际运行覆盖证据。

## 用户截图暴露的未完成项与新的执行顺序

用户指出侧栏挤占、内容重叠、节点缩放过小，以及未使用原生子图导航。此前的编译、Document、Build 记录不代表作者界面可用；作者 UI 验收没有通过。

用户明确要求：先完成代码和 UI，再迁移资产。自该指令起不再执行资产迁移、业务保存或 Build；只做代码修正与 Editor 刷新。

本次修正侧栏为 320 点宽、详情／图目录／运行观察页签，工具栏换行，默认 100% 阅读缩放；删除自定义 Breadcrumb Host 和 PageStack，Pose、状态及规则进入沿用原生画布子图导航。状态页面绑定端口类型从声明读取，不再依赖另一端端口已注册，修正已定位的空引用。

普通 GameplayLab Play 曾被其它领域错误阻止：`ProgramStateSemantic=144` 在 Fixed Program 读取中不合法，调用链为 `SimulationProgramSemanticsCodec → CharacterSimulationProgramCodec → FixedCharacterSimulationProgramAsset → DeterministicRollbackSessionSourceDefinition → GameplayLabBootstrap`。按用户要求未修改该领域；未因此绕开正式启动或创建替代角色。正常角色运行观察和性能对照尚无通过证据。

### 截图后的代码验证状态

修正后再次读取 Console 为零错误，并通过正式菜单重新打开 Corin 图。UXML 中代码要求的所有区域均存在，自定义 Breadcrumb Host / PageStack 引用数为零。原生双击入口为 `Node.TryOpenEditorChild → GraphEditor.OpenEditorChild → Graph.SetCurrentEditorChild`，返回沿原生画布面包屑；Pose 状态／规则页面仍从唯一作者 Document 建立不持久化视图。

作者反馈新版已经能看，但明确认为只达到可读，UI 仍未完成，不能据此写成整体视觉验收通过。后续仅继续代码修正，资产迁移与最终 Build 放在代码和 UI 完成之后。

### 属性编辑与选中卡顿修正

用户后续要求按 AnimGraph 作者习惯整理属性，不展示空运行状态和无用元信息。代码新增领域结构化字段编辑器接口，Pose 提供参数策略列表与 IK 效应器绑定的具体控件，提交仍进入现有 typed Mutation。默认 Pose 详情采用 authoring-only 展示，运行目标控件移入运行观察页；默认不渲染 Runtime Inputs、Applied Values、空 References 等栏目，状态转移保留可编辑策略和条件图入口。

选中路径原先重复调用 `SimulationProgram.Load → Codec.ReadArtifact → Projection.Load`。新增按 ProgramHash 和 ProjectionRevision 绑定的窗口级只读缓存，同一版本的重复选择不重新解码；失败同样按版本记录，不在每个选择事件重试重加载。没有修改 Runtime 执行或引入替代产物。

原生曲线命中范围原先只覆盖端点矩形，已改为覆盖控制点包围范围，并用连续线段距离判断；状态转移增加方向箭头和双击条件图入口。原生面包屑在领域侧栏模式下不再因为有选中对象而隐藏。

这些修正尚无完整交互通过证据，资产迁移仍未继续。

### 第二批代码提交与转移详情故障

代码提交：`149273005`（35 个文件）。用户已经确认节点选中卡顿改善；其后截图仍显示状态转换选中后没有策略，不能标记交互闭环。

实际堆栈进一步定位到 `Graph.UpdateNodeBBFields`：新建的只读文档视图未经历序列化收集，GraphSource 的 allParameters / allTasks 尚未初始化，导致切换在详情绑定前中断。已在 GraphSource 建立集合初始化不变量；不是跳过校验或返回备用图。刷新后当前 Console 无错误。

页面绑定现在以原生 currentGraph 为依据，构造和绑定阶段不发布中间选中状态。每份状态／规则文档拥有独立视图及绑定；状态转移对象按正式 EdgeId 复用，参数变化不会重建选中身份。原生导航按 NodeId / EdgeId 还原路径与选择，使用同一 editorObservation 接口提供只读绘制数据。

编辑入口核对：根 Pose 图创建／连接／改接／字段写入进入 CharacterPoseCanvasEditorWriteSession 和 typed Mutation；状态转换改接使用既有 source / target-state-id Mutation，保留条件与混合配置；规则改接采用一次批量断开／连接。剪贴板遵守现有 Copyable 与文档边界，不给不支持的页面提供可执行的复制入口。

尚未完成的证据：完整编辑交互覆盖、资产最终往返、正常角色运行观察与观察开销对照。用户要求继续完成代码后再处理资产，当前没有继续迁移或 Build。

### 2026-09-08：目录导航、详情写入与观察调用定位

本批修改由当前任务单独实施，没有新增测试或迁移业务资产。

- 目录原先直接打开目标图，丢掉父页面。现在从唯一根图读取状态机、状态和Subgraph调用关系，使用相同的原生NodeId进入链导航；Corin目录的Idle入口会经过Root Pose Graph、Locomotion State Machine、Idle。共享图多处调用时显示调用路径菜单。未被根图引用的目录图仍明确作为独立编辑根打开，不伪造调用来源。
- 详情中的OpenChildSurface也进入GraphEditor.OpenEditorChild，和节点按钮、双击使用相同父子关系；没有新增活动PageStack或第二画布。
- Linked Pose目录构建和打开函数原先未被调用，现接回既有NavigatorDataSource；不新增Linked Pose资产或实施技能迁移。
- 原生居中选择不再把多选连线强转为Node，按节点矩形与连线中点矩形计算选择范围。
- 共享详情在预检或领域字段校验失败后重新读取正式值，并在同一详情面板显示错误，避免输入框保留未提交值。状态转移的Custom模式仍先等待Curve选择，再通过既有typed Mutation提交，不写半套配置。
- 运行完成帧只更新已采集的观察文字和高亮，不再每10Hz重建作者属性控件。状态机高亮同时检查StateMachineId和SourceMap中的GraphId、AuthorNodeId、CallSite、编译NodeId，防止同一作者状态机不同调用的结果合并。

源码核对排除了“Document永久持有旧状态机对象”的猜测：CharacterPoseStateMachineDocument及CharacterPoseTransitionRuleDocument已经按owner GraphId和NodeId取得当前Payload，未修改这条正确实现。

当前作者资产重新只读解析：8张图、25个节点、19条Pose边；SHA-256仍为`2766503f845a89d7b743a2bd29d4381d3650abbd8a026c55fde35305920e7a39`，与盘点基线一致。状态机、规则和资源引用保存在`.codex-tmp/canvas-core/pose-authoring-audit-20260908.json`。本次没有改写YAML。

验证过程：已通过Unity实例`e852139597e42532`发起刷新。期间编译被其它领域CharacterAuthoringSourceCompilationModel中的3个错误阻止：CharacterBlackboardDeclarationSnapshot不能转成BaseExposedProperty，以及两处CharacterAuthoringBlackboardDeclaration.Graph不存在。错误行号随其它任务编辑移动，曾读取为592、597、599。本任务没有修改或绕开这些错误。

22:11:59生成新程序集，随后实例恢复且Console为零错误。通过既有菜单重新打开Corin，get_windows确认唯一Canvas窗口为NodeCanvas.Editor.GraphEditor；这些结果只证明本批编译加载和窗口入口，不代替完整编辑交互。

随后调用精确Corin Definition的`btsmtl.validate`，正式校验返回`skill_entry_graph_missing`：Attack、DodgeBack、DodgeForward引用的技能入口不在Definition.SkillGraphs中。失败路径分别为compiler/AuthoringDiscovery/Attack、DodgeBack、DodgeForward；applied=false、saved=false、touchedOwners为空。依照用户要求，不修技能、不绕过唯一Definition校验，也未继续资产apply或Build。

普通Play检查：从未播放且未dirty的GameplayLab发起editor play；后续character.fixed_input_trace/status显示playing=false、mode=Idle、actor_id为空。Console显示两条“referenced script (Unknown) ... missing”，而manage_scene/validate报告GameplayLab为零缺失脚本、零损坏Prefab；未据此臆测或修改报错资产，也没有生成替代角色。真实运行观察、开关对照和性能采样没有获得通过结果。

本批小步提交曾被仓库已有.git/index.lock阻止；未删除锁或终止其它git进程。锁正常释放后，5个代码文件已提交为`5b529238a`（补齐Pose目录导航与属性编辑反馈）。149273005不包含本批新增修正。

任务1.2和6.3按已有盘点、当前资产哈希以及删除消费者源码搜索收口；总计6/26。其它编辑、版本化运行、最终资产往返、正式Build与性能证据仍未收口，未把局部修正勾成整组完成。

## Context

动机和范围见 `proposal.md`。本次只规划完整角色的场景运行式预览，保留已经建立的共享作者框架、正式编译器和运行算法。

### 已确认的事实

| 位置 | 当前行为 | 对本设计的影响 |
|---|---|---|
| `TimelineEditorWorkspaceView.cs` | 界面定时器推进 `TimelinePreviewSession`；进入或退出 Play 都暂停并清空目标 | 迁移播放控制与目标绑定，保留 Timeline 编辑、几何和绘制 |
| `TimelinePreviewRuntimeSession.cs` | 每个页面管理时间、会话 identity、目标和求值 | 删除完整角色预览的窗口级执行会话；编辑游标保留为视图数据 |
| `CharacterPipelineAuthoringPreviewController.cs`、`AnimationPreviewEngine.cs`、`AnimationPreviewAdapters.cs` | 独立组织预览 Action、Fact、Query、动画时钟及视觉位置 | 这些完整角色预览入口由场景真实 Actor 替代，不能只改类名继续运行 |
| `CharacterAnimationPreviewFixture.cs`、`CharacterPoseAuthoringBottomDock.cs` | Pose 页面创建预览场景和 fixture，并提供 Pose 调参与观察 | 保留作者面板和正式调参能力，删除私有场景及 fixture 驱动 |
| `EditorPlayModeSceneLauncher.cs` | 进入前打开场景并执行 prepare，退出后按一个路径恢复 | 扩展同一启动入口，处理独立场景、完整编辑场景布局与请求恢复；不能继续假定 prepare 的写入会在退出后自动撤销 |
| `SimulationSessionHost` | 唯一正式 Session owner，具有准备、运行、Quiesce、释放和重新初始化边界 | 预览场景使用同一实例类型和 Composition，不创建 Preview Kernel |
| `CharacterPoseAuthoringBottomDock.SubmitTuningValue` | 先经正式作者 Mutation 修改资产，再向真实 Actor 提交参数候选 | 用户确认保留此方向；需要统一运行采用状态和跨窗口失效处理 |
| `CharacterSimulationBuildOrchestrator` | 编译 Semantic IR、表现计划、Numeric Target，发布资产；可补生成缺失或过期脚部分析 | Build 必须显式，独立记录阶段耗时，不能把分析生成和 C# 重载混称“图编译” |

上表是代码阅读结果，不是故障复现。此前电脑操作只到 Unity 主窗口便被用户停止，尚未实际操作 BTSMTL 窗口复现预览问题。没有独立数据编译耗时基线。

用户已确认：使用可配置的独立 Unity 场景；进入真实 Play Mode；正式运行链产生结果；运行中调参直接修改并保留正式作者数据，通过 Mutation 和 Undo 管理。

## Goals / Non-Goals

**Goals:**

- 同一份正式角色配置在预览场景和游戏场景中执行相同的 Gameplay、世界求解、动画、IK 和已存在的相机表现逻辑。
- 作者能够选择场景与 Actor、运行、暂停、继续、通过正式输入试动作、直接调参、重建试验和结束预览。
- 场景、运行、视图和作者资产各有唯一 owner；切换页面不会更换播放器，也不会丢失整个试验。
- 从作者视角明确显示“在哪里试、正在控制谁、哪个参数已生效、为什么不能继续”。

**Non-Goals:**

- 不重写 Graph/Timeline 数据结构、Semantic IR、Program ABI、Pose 计算、Foot Placement、FBBIK 或相机算法。
- 不添加任意时间 seek、Gameplay 倒放、状态快照恢复或新的输入录制格式。已有 Capture 历史仍只读浏览。
- 不实现 C# 热更新，不安装插件，不自动关闭 Domain Reload 或 Scene Reload，不承诺启动毫秒数。
- 不修补 TrainingEnemy，不实现缺失的战斗或表现 consumer，不在本变更启动网络进程、匹配或多实例联调。
- 原生 Animation Window 的素材与注册曲线编辑、Blend Space 采样点/几何绘制仍是作者数据工具，不归入待删除的完整角色播放器。
- 离线动画分析、校准采样和明确标注未连接完整 Pose 的模块诊断 Fixture 保留原有用途，不接入完整角色播放按钮，也不按名字包含 Preview 就删除。

## Decisions

### 1. 场景文件保存试验条件，窗口只选择要运行的场景

采用正式 `.unity` 场景和其中的 Prefab 引用保存角色、目标、地形、光照、相机和初始条件。提供 `Assets/Scenes/Authoring/BtsmtlPreview.unity` 作为初始 Corin 场景；允许作者复制场景建立其它正式试验环境，窗口通过 `SceneAsset` 对象选择器明确选择。

场景内配置一个显式预览上下文组件，持有真实 `SimulationSessionHost` 的对象引用和允许控制的角色引用；角色、Composition、Definition、Program 与 Projection 仍由现有正式组件拥有。该组件只声明场景上下文及向编辑器发布已准备好的绑定，不编译、不推进 Tick，也不持有第二份角色配置。由其 OnEnable/OnDisable 显式登记/撤销上下文；协调器只接受本次请求加载的场景实例中唯一登记的上下文，不通过场景搜索、对象名或当前 Selection 猜目标。

窗口保存所选场景 GUID、当前页面的 Actor identity 和视图状态。运行前固定预览场景；角色列表只来自该场景正式登记并与 Session roster 对账的对象，只有唯一候选时也不把其它场景中的对象作为补充。无有效预览场景仍可编辑资产，运行按钮显示缺失项。

启动前只能检查当前已知的精确作者上下文和场景引用，不能假定无需加载就能取得所有场景对象。场景上下文登记时还需发布它明确引用的 Definition 与构建目标，并在 Session 准备前完成对账。若加载后才发现缺失或过期产物，结束本次受控 Play、返回 Edit Mode 并展示实际目标的“构建并开始”；没有精确目标时不提供猜测性的 Build。所有 Actor 必须匹配场景声明，不用当前窗口的 Definition 替换整个 roster。

**取舍：** 场景文件配置能直接在 Unity 中布置空间，并复用现有 Prefab Override；独立 Profile 资产配置更便于程序化组合很多试验，但会增加环境和角色引用的装配层。本次选择场景作为唯一环境配置，不另存一份镜像 Profile。

### 2. 同一 Unity 实例只有一个受控预览运行，所有窗口复用它

新增 editor-only 场景预览协调器，经共享操作合同对外提供 Start、Pause、Resume、Reset、Stop 和只读状态。协调器拥有请求 identity、场景准备状态和运行控制权限；真实 Simulation/Presentation 生命周期仍由场景里的正式 Host 拥有。

Graph Shell 只接收领域提供的按钮、状态和可选场景/角色选择表面，不识别 Character、Action、Pose 或相机字段。Timeline 与 Action Workspace 复用同一操作合同，不分别创建启动器。`RuntimeDebugSession` 继续聚合只读数据，各窗口保留自己的 `RuntimeDebugViewBinding`、selection、观察 Actor 和 Timeline playback instance。

关闭一个窗口只撤销它的观察 interest 和本地绑定；切换 Graph、Timeline、Pose Graph 或折叠 Bottom Dock 都不停止预览。明确点击“结束预览”或 Unity Stop 才结束运行。第二个 Start 在当前预览运行未结束时返回占用信息，不能静默换场景。已有不属于本协调器的 Play 只能走现有 Live 观察入口，不能被预览窗口自动暂停、重置或停止。

**取舍：** 每窗口一个运行便于同时做完全独立试验，但 Unity Play Mode 本身是实例级状态，容易出现窗口抢时钟和物理输出。实例级协调器使作者只有一次明确的运行，代价是同一 Unity 实例不能同时启动两个独立预览场景；多角色对照在同一正式场景中配置。

### 3. 扩展现有场景启动器，保持进入与恢复为一个操作

保留 `EditorPlayModeSceneLauncher` 为通用进入 Play 的唯一入口，将其从单个 active scene path 扩展为显式启动请求。优先使用 Unity 的 `EditorSceneManager.playModeStartScene` 指定预览场景，记录并恢复它原来的值；不在 Edit Mode 把角色位置、目标或 runtime 参数写进作者场景后再假设会自动恢复。

Scene setup 与启动场景切换使用 Unity 2022.3 的正式编辑器接口。普通 Scene/Prefab 引用继续由 Unity 序列化；不复制 `.unity` 文件作为临时运行路径。预览结束只恢复编辑环境，不对作者资产做整目录回滚。

启动器记录原来的完整 Scene setup，包括加载场景顺序、是否加载和 active scene。未保存场景按 Unity 正式保存流程处理，用户取消时整次启动取消。利用 Unity 的场景备份与恢复，必要的显式 Scene setup 恢复也由同一启动器完成；预览协调器不再实现第二份 OpenScene/RestoreScene 逻辑。

通过 `SessionState` 只保存跨 Domain Reload 必需的请求标识、场景身份、编辑场景布局、先前启动场景设置及目标定位信息；不序列化 Runtime、GameObject 实例引用、PlayableGraph、会话内状态或编译对象。EnteredPlayMode 后重新等待场景上下文和正式 Session Active，再按身份连接。Exited/Entered Edit、取消、失败或请求失效都由同一请求清理，不能自动重放未完成命令。

```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Checking: 开始预览
    Checking --> NeedsBuild: 产物缺失或过期
    NeedsBuild --> Checking: 明确构建成功
    Checking --> EnteringPlay: 场景与请求有效
    EnteringPlay --> Preparing: Unity进入Play
    Preparing --> Running: 正式Session就绪且绑定成功
    Preparing --> Stopping: 发现产物需要构建
    Running --> Paused: 暂停
    Paused --> Running: 继续
    Running --> Resetting: 重建试验
    Paused --> Resetting: 重建试验
    Resetting --> Preparing: 同一场景重新加载
    Running --> Stopping: 结束预览
    Paused --> Stopping: 结束预览
    Preparing --> Stopping: 取消
    Checking --> Faulted: 检查失败
    EnteringPlay --> Faulted: 启动失败
    Preparing --> Faulted: 准备失败
    Running --> Faulted: 正式运行失败
    Resetting --> Faulted: 重建失败
    Faulted --> Stopping: 清理受控运行
    Stopping --> Idle: 恢复编辑环境
    Stopping --> NeedsBuild: 已恢复且等待明确构建
```

失败信息独立保存为本次操作结果，恢复 Idle 不擦除失败原因。暂停采用 Unity 的实际暂停状态，继续由正式调度推进；本次不添加另一套 Preview Delta 或倍速时钟。

**取舍：** 在 Edit Mode 手动打开预览场景可以复用现有简单入口，但多场景和准备写入更难恢复。使用 Unity 启动场景机制能减少编辑环境切换，需补齐通用启动器的请求与重载恢复；原有 Launcher 调用者一同迁移到该请求合同，不保留两个启动实现。

### 4. 试动作只改变正式输入，不直接指定运行结果

场景使用正式 Composition、Numeric Target、WorldSolver、Actor roster 和输入源配置。运行中的位置、速度、Grounded、Action winner、当前状态、Timeline membership、动画选择和相机请求全部由正式执行产生。

通过 Game View 的正常玩家输入操作角色。需要在 BTSMTL 提供动作试验按钮时，场景明确配置输入映射：按钮提交目标角色声明过的输入值或请求，经既有 Control Source/Ingress 进入唯一输入采样；该 Actor 每个阶段仍只有一个输入 owner。不能把“选中 Attack Timeline”解释为直接创建 Attack Selection，不能强制进入某个 Graph State，也不能绕过 Action admission 直接创建伤害、Motion、Warp 或相机效果。

定义没有可达输入路径、admission 拒绝、缺少目标等情况显示真实原因。UI 可以导航到 Action 的输入和调用点，不能补造触发规则。Locomotion、Blend Space、MM 的测试通过角色正式移动和输入产生对应 Fact/Query；数据编辑器显示采样权重或曲线形状时不输出角色 Pose。

**取舍：** 正式输入试验能看到状态条件、位移和动画共同产生的结果；直接触发某个内部节点更易快速看单个效果，但无法验证它真实如何进入和退出。本次选择正式输入，并把试验准备作为可见的场景配置。

### 5. 参数直接写作者资产，运行采用走原有原子更新协议

用户于本轮明确选择“直接修改并保留正式作者参数”。沿用 `CharacterPoseTuningAuthoringService` 与正式 Mutation；不增加试用资产、副本或保存预览参数按钮。

领域适配器按共享 Capability 与现有参数布局提供字段编辑资格，不由 UI 根据字段名猜测。字段分为已有支持的运行参数、需要 Build 的作者字段、纯编辑布局和只读运行观察。运行参数保持原有值域、作用域、NextFrame/NextActivation 与 `resetOwnerState` 语义；不得为完成此变更把所有数值强行改成可热更新。

```mermaid
flowchart LR
    A[作者修改字段] --> B[同一Capability与Mutation验证]
    B --> C[正式作者资产与Undo]
    C --> D[领域参数候选编译]
    D --> E[精确Actor的正式调参入口]
    E --> F[正式帧或下一次激活边界提交]
    F --> G[只读运行状态确认采用]
```

候选必须携带准确 Actor、已发布 Program/Projection、参数布局、作者来源和候选 generation；运行侧复用现有 `CharacterPoseTuningCoordinator`，不修改共享 Program Image、Execution View 或静态 Projection。输入合法但运行应用失败时，作者修改与 Undo 仍成立，运行保持上一份已提交参数，UI 显示“作者数据已修改，运行未应用”及原因，不回退用户编辑。

UI 使用业务状态“已修改作者参数”“等待下一帧／下一次激活”“运行已采用”“需要构建”“运行应用失败”；revision/hash/generation 只在折叠 Diagnostics 中提供。单纯收到提交成功不能显示已生效，必须等待运行端确认。暂停期间可以保存作者参数，但只显示待生效，不在 Inspector 绘制中主动执行一帧。

Undo/Redo 同样从真实作者数据生成新候选，不从缓存恢复运行对象。共享 Profile 只提交到明确选中的 Actor，其它 Actor 不被暗中更新；资产的默认值对后续 Build 和运行有效，当前其它 Actor 的采用状态必须单独显示。结构修改、布局改变或外部资产变更导致已发布拓扑不匹配时，停止相应的可执行预览并要求显式构建，不能把它当成参数变更绕过。

**取舍：** 直接保存便于持续调好角色，也与现有 Pose 作者流程一致；独立试用再保存便于丢弃实验，但增加作者默认值、运行试用值和保存事务的区分。用户选择前者，系统通过 Undo 和明确生效状态支持撤回与排查。

### 6. 重建试验重载完整预览场景，普通游标只做编辑和观察

第一次进入 Play 后，重复输入和参数调整无需退出。点击“重建试验”时保持 Play，停止接受本轮控制请求，使观察绑定失效，经正式 Quiesce/Dispose 释放旧 Session，使用 Unity 的 Play Mode 场景加载接口重载选定的同一正式场景，再从场景配置构造新 Session；场景 generation 改变后重新解析所有窗口目标。

从暂停状态重建时保存作者的暂停意图。场景加载和正式 Preparation 需要推进的阶段可以由受控操作解除 Unity 暂停，期间关闭本轮试验输入；正式准备完成后恢复暂停，再允许作者继续。UI 在此期间显示“重建／准备中”，不能卡在暂停状态等待永远不会执行的 Preparation Tick，也不能用 Editor 手动调用业务 Update 绕过正式调度。

不能仅把角色移回出生点或清空时间：输入边沿、Action、GE、世界实体、相机、动画 source、Foot/IK、事件订阅及诊断绑定均须在各自正式 owner 生命周期结束。场景采用专属物理环境和明确运行根；预览拥有的对象不能通过 DontDestroyOnLoad 逃离其清理边界。全局模块仍按项目生命周期注册/注销，不能在 Editor 对全局静态对象做任意反射清空。

重建前必须通过正式产物匹配检查。运行中调参后，作者默认值与已发布产物可能不同；若正式检查要求重新发布，则显示“需要构建并重启”，使用下一条决策的明确操作，不拿旧 Projection 创建新 Actor 后再补参数绕过构造校验。同一个已经存活且成功采用参数的 Actor 可以继续接收输入、重复动作，不需要为了每次动作重新进入 Play。

编辑游标可以定位 Clip、Curve 和作者内容，不再 Evaluate 角色。Capture 历史位置只选择历史 snapshot，不改变当前角色。实时运行标记使用真实 logic tick/visual time。原先依赖拖动游标查看任意角色时刻的功能明确撤除；以后需要确定性 seek，应另行基于完整正式输入及状态合同设计。

**取舍：** 只重置 Actor 更快，但容易漏掉目标、场景物理或效果状态；重载完整独立场景更容易得到可重复的初始条件，代价是一次场景重载。此选择不引入第二种运行算法，也不要求反复进入 Play。

### 7. 编译和进入 Play 分开报告，结构更新显式重启

“开始预览”只检查配置、已发布产物和运行环境，不隐式 Build。缺少或过期时给出精确 Definition/Target 与原因，显示“构建并开始”。“构建并重启”是一个明确操作：结束受控 Play、恢复 Edit Mode、调用现有精确 Character Build、成功后再次启动原预览场景。构建失败保留编辑状态和诊断，不自动进入 Play。

窗口运行观察保持只读；支持的参数在独立作者字段中编辑，需要 Build 的结构编辑在 Edit Mode 完成。其它入口或 Undo 引起结构变更时，同样失效当前绑定并要求明确构建。保存参数不等于发布 Program，这一点在状态中保持可见。

在现有 Build report 和任务结果中记录前端发现/生成与确定性检查、分析复用/生成、表现计划、Numeric Target、资产发布耗时；预览请求记录检查、进入 Play、Session 准备、目标连接和场景重建耗时。只记录真实经过的阶段，并区分缓存复用与重新生成，不用轮询等待总时间代表编译本身。没有测量值显示未测量，不预设耗时目标，不为了“优化”删除确定性检查或修改算法。

**取舍：** 每次预览自动构建省去一次点击，但把潜在分析生成隐藏到播放操作中；明确构建使作者知道等待来自哪里，并保留现有发布边界。热更新插件只能作为以后独立评估的开发工具，不成为此方案依赖。

### 8. 作者、场景和运行数据保持单一归属

| 模块 | 输入 | 输出与唯一职责 | 不拥有 |
|---|---|---|---|
| 独立场景与预览上下文 | Prefab、环境、正式 Session/Actor 引用、输入映射 | 可保存的试验条件、准确运行上下文登记 | Graph 镜像、Program 构建、Tick |
| 通用场景启动器 | 明确场景启动请求、原 Scene setup | Unity Play 状态转换与编辑环境恢复 | 角色执行、动画求值 |
| 场景预览协调器 | 作者命令、Unity 生命周期、正式就绪结果 | 请求状态、控制权限、场景 generation、阶段耗时 | Simulation/Presentation 状态、第二播放器 |
| 领域适配器 | 当前文档、Capability、精确 Actor/产物 | 输入映射、编辑资格、参数候选、只读状态投影 | Undo 副本、跨领域字段规则 |
| 正式 Session 与 Actor | 正式配置、输入、已发布产物 | Gameplay、世界求解、完整表现与调参提交 | Editor 选择、窗口、场景恢复配置 |
| Diagnostics 与窗口本地绑定 | 正式 committed snapshot、source map、interest | 节点/状态/Timeline/Pose/相机观察 | 执行控制、补算结果、作者数据写入 |
| Document v4 | 正式作者资产和严格 editable/context package | 既有 checkout/rebase/dry-run/apply/validate | 预览 runtime 状态、场景镜像、第二写入事务 |

Editor 的场景编排依赖正式公共 Composition 与 Character 公开端口；公共 Simulation 程序集不反向引用 Editor、BTSMTL 视图、Animancer 或具体 Network Model。场景上下文的数据声明位于已有客户端 Unity 边界；没有新 portable Preview Program、Preview Pipeline 或 Preview Backend。

人工运行调参使已有 Document 基线呈现正常 TreeDirty/Conflict，不能自动改 package 或执行 rebase/apply。五个 Document 工具的 Play Mode 门禁继续生效，退出后按现有工作流刷新。作者字段和运行编辑资格变化同步检查 Capability、Exporter/Codec/Reconciler/Mutation/Validator 与 `btsmtl-agent-authoring`；运行资格如需向 Agent 描述，只进入只读 context，不成为可写 runtime identity。

原生 Animation Window 的素材编辑目标继续通过已有 typed navigation context 显式提供，不能从 Live Actor 观察绑定反推，也不能给正式生产 Prefab 安装素材曲线接收器。运行期间涉及素材采样的编辑操作显示需要先结束运行；不能与真实 Actor 竞争同一物理动画输出。移除 `TimelinePreviewTarget` 类型时同步迁移导航签名，但保留原生素材编辑及其独立目标合同，不另造素材时间轴。

**取舍：** 把所有操作集中到一个窗口类容易首次接通，但后续每个作者页面都必须重复处理生命周期；将场景请求、领域命令、正式执行和只读观察分开，新增页面只适配现有端口，同时保留单一数据写入链。

### 9. 逐项对账旧规范与并行提案

| 当前来源 | 冲突或必须保留的规则 | 本次处理 |
|---|---|---|
| `btsmtl-timeline-editor-preview` 的窗口 session、target、隔离和独立采样要求 | 每窗口自己播放表现，与统一场景运行冲突 | 删除这些旧合同；场景运行归新能力，窗口保留本地文档与观察绑定 |
| 同 spec 的 TreeClip、Preview/Live 和非连续 seek | Preview 禁止 Gameplay、拖游标重新采样 | 改为正式 Session 执行；编辑游标/历史观察不控制角色，撤除任意 seek |
| `character-action-animation-authoring-workspace` | Preview 不允许 Session、Gameplay Timeline、Motion/Warp 等 | 替换为正式输入触发和真实运行观察，保留跨 owner 唯一写入 |
| `character-animation-pipeline`、`character-animation-layer-runtime`、`character-animation-selection-runtime` | 规定 Action/Fact/Query fixture 和独立 AnimationPreviewRuntime | 替换入口，保留全部正式 Pose、Action、Routing、source、writer 算法与顺序 |
| `character-motion-matching-presentation-module`、`character-pose-inertialization` | MM Query Fixture 输出完整角色且禁止 Program；存在窗口非连续 seek 重置触发 | MM 完整预览改为真实 Actor，移除旧 seek 输入；保留正式 MM 与惯性算法 |
| `character-presentation-pose-graph`、`character-pose-graph-runtime-architecture` | 独立 Preview 装配；Live 下作者字段一律不可写 | 改为真实 Actor；Live snapshot 仍只读，明确作者字段可沿正式调参协议修改 |
| `gameplay-simulation-session-composition`、`unity-simulation-assembly-ownership` | Timeline Preview 不允许依赖公共 Composition | 允许场景经公共入口创建正式 Session；继续禁止复制 Composer 和反向依赖 |
| Graph Shell/Domain Framework | 通用交互、能力目录、只读运行观察 | 保留；新增领域操作承载和字段编辑影响合同，不另造工作台 |
| Document v4 同步规范与技能 | 唯一作者事务、运行数据只读、Play Mode 门禁 | 保留；人工修改通过正常树侧变化被识别，不开放运行中 package apply |
| `btsmtl-timeline-animation-authoring-surface`、`character-animation-clip-authoring`、`character-animation-presentation-authoring` | 原生素材工具需要显式编辑目标，曲线接收器不能装在生产 Prefab | 保留素材编辑合同；目标导航与正在执行的 Actor 分离，不因删除完整角色播放器破坏素材工具 |
| `character-animation-transition-routing-module` | 独立规则 Fixture 明确不连接 Pose Evaluation | 保留模块级诊断用途，不作为完整角色预览入口 |
| `character-animation-foot-analysis-artifact` 与现有 Build 代码 | spec 要求显式生成分析、Definition Build 只读取；现有 Orchestrator 会向 Resolver 传入生成开关 | 这是已存在的代码/spec 不一致；本变更只增加耗时可见性，不授权改变分析生成所有权，不把自动生成写成预览要求，遇到该分歧须明确报告 |
| `openspec/project.md` 的 Presentation/Authoring/目录说明 | 仍写 Fact Preview、Action Preview、隔离 Preview host；一处 canonical v3 与后文 v4 不一致 | 安装本变更时统一说明与当前 v4，不把现有正文当作新方案已实现 |
| `rebuild-character-camera-from-zzz/design.md` 与其 Timeline Preview delta | 要给 TimelinePreviewSession 增加相机、禁止 Gameplay、独立相机命令 fixture、实现状态 seek | 此设计已被用户新的场景方向替代；相机变更实施前需改为正式 Actor 相机输入、输出和场景绑定，删除对应 fixture/seek 任务 |
| `rebuild-character-camera-from-zzz` 的相机算法、资源、Document、唯一 Presentation 入口 | 与本次方向一致 | 保留；本次不提前实现相机能力，也不更改其源行为证据要求 |
| `refactor-character-pose-graph-architecture` | 已建立 Module、根事务、Program Image、Tuning；其中 Preview 表述仍有旧入口 | 已完成实现复用，重叠 Preview 文本按本提案统一；不改 Foot 算法或已验证执行顺序 |
| `add-acl-animation-runtime` | Preview 与正式运行使用同一 dense 动画资源合同及 Source Module | 与本设计一致；后续场景 Actor 使用正式 backend，不额外实现 ACL 预览路径 |

当前只在本 change 写 delta 和对账记录，不覆盖这些并行工作区文件。后续 apply 的规范收口步骤要同步冲突段落和任务；不能同时按旧相机 Preview 任务与新场景任务实施。

## Risks / Trade-offs

- [Play 中修改 ScriptableObject 不会随退出自动回退] → UI 明确是正式作者参数；Undo 可撤回，绝不把资产变化当可丢弃场景状态。
- [作者值已经保存而运行采用失败] → 分别展示两种状态和失败原因，保留原子运行快照，不回退作者资产或静默显示成功。
- [场景重建漏掉全局订阅、输入边沿或资源] → 复用正式 Quiesce/Dispose 和重新登记合同，禁止对象逃离预览场景清理边界，不用编辑器反射清全局状态。
- [Domain Reload 导致窗口、协调器与目标失联] → 只恢复请求和稳定定位信息；重新等待正式登记，旧对象与 generation 一律失效，失败后恢复编辑环境。
- [直接打开的 Timeline 不存在正式角色调用点] → 仍可编辑，运行按钮报告缺少可执行上下文；不合成 Gameplay producer。
- [已有场景或并行任务正在运行] → 受控请求只操作自己的 Play；外部运行保持现有只读 Live 入口，不抢占。
- [首次 Build 还生成动画分析] → 分阶段计时并展示生成/复用情况；本次不承诺进入 Play 或编译性能数值。
- [调参后重建场景遇到作者与产物不匹配] → 明确要求构建并重启；当前 Actor 内重复输入保持可用，不旁路已有构造验证。
- [相机、Foot、Pose 和性能变更正在并行] → 核对实际文件与当前产物，独立小步提交；不回退其它任务改动，不把未完成模块作为预览所需的隐藏默认值。
- [Editor 运行与 Player 仍可能因配置和输入不同而不同] → 明确记录所用正式场景、Numeric Target、产物和输入来源，保证同链执行，不宣称跨配置绝对一致。

## Migration Plan

1. 固定本设计的业务合同，盘点所有完整角色预览入口、调用者、序列化字段、fixture 资产及其引用；保留既有编辑器交互与正式运行算法的责任边界。
2. 扩展唯一场景启动器和领域操作合同，增加预览场景上下文、协调器及显式输入端口；新实现接入前不把不可用按钮作为完成结果。
3. 以正式 Corin Prefab/Composition 建立独立场景，完成真实 Session 准备、目标身份解析、暂停/继续、完整场景重建和退出恢复。
4. 同步迁移 Timeline、Graph、Pose Graph、Blend Space、MM 关联入口与 Action Workspace；以现有 RuntimeDebugSession/本地 binding 显示真实结果，接通直接作者调参及运行确认。
5. 在同一迁移步骤删除被替代的类、字段、资源、菜单、旧 target 选择和 fixture 输入，确认只有正式场景路径。无法解释的调用者先定位业务归属，不以兼容开关保留。
6. 为精确 Build 和预览请求补齐阶段计时、失败状态及明确的构建重启流程，同步 Document context/技能和所有受影响 current spec 的安装内容。
7. 对齐相机和 Pose active change 的重叠 Preview 段落，再收口文档及资源引用。实施回退以本变更的独立 Git 提交为单位，不在运行时保留双路径开关；不回退用户随后产生的作者参数或并行工作。

## Open Questions

没有需要实施者重新决定的业务保存规则。具体按钮排列、初始场景物件位置和阶段耗时数值在实施时依据现有 UI 与真实测量确定，不改变本设计的场景、运行、作者数据和观察边界。

Unity 生命周期依据：[进入 Play 的阶段](https://docs.unity3d.com/2022.3/Documentation/Manual/ConfigurableEnterPlayModeDetails.html)、[指定 Play 启动场景](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/SceneManagement.EditorSceneManager-playModeStartScene.html)、[ScriptableObject 资产保存](https://docs.unity3d.com/2022.3/Documentation/Manual/class-ScriptableObject.html)。

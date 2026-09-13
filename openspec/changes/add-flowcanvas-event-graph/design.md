## Context

2026-09-13，用户明确确认“参考UE，现有动画层的计算、判断和变量更新尽量交给EventGraph，C#保留数据接入和底层执行”。这修正了上一版偏向输入清理、允许Corin不接事件图的范围。当前正式修订标识为r4-ue-animation-update；它是文档修订标识，不是运行ABI或资产版本号。

原生FlowCanvas runtime、唯一变量合同和C# authoring双工具继续保留。不新增步频、播放倍率或平滑效果；这里迁移的是当前代码已经计算的动画数据及其已有消费者。不能把所有动画规则包装成一个新的C# Update方法再摆一颗节点，也不能继续让Corin只保留Start/Update。

### UE官方参考及采用范围

- [EventGraph](https://dev.epicgames.com/documentation/en-us/unreal-engine/event-graph-in-unreal-engine)：以事件为入口连接函数、流程和变量，可服务普通对象和关卡。本项目复用这种作者方式，动画是首个宿主。
- [动画变量](https://dev.epicgames.com/documentation/en-us/unreal-engine/how-to-get-animation-variables-in-animation-blueprints-in-unreal-engine)：官方示例从角色速度计算水平速度、移动判断等变量，也提供线程安全函数实现。采用“正式观测进入作者逻辑、变量供姿势图使用”的分工，不照抄示例阈值。
- [动画蓝图中的图](https://dev.epicgames.com/documentation/en-us/unreal-engine/graphing-in-animation-blueprints-in-unreal-engine)：EventGraph更新AnimGraph所需变量；AnimGraph负责姿势组合。文档同时介绍Node Functions、Property Access和线程安全更新，并提示游戏线程EventGraph的成本。

这不意味着UE要求所有逻辑只能在全局EventGraph。节点相关性、State时间、播放器时钟、姿势混合、IK及底层执行继续归Pose或既有节点局部逻辑。本次不另建Node Functions框架或线程执行器；先用已确认的FlowCanvas原生更新，在进入现有Pose/Worker前冻结输入。UE示例使用XY平面，本项目继续使用Unity现有XZ平面。

## Goals / Non-Goals

**Goals:**

- 把当前动画预处理公式、分支、变量读写和所需历史显式画到原生事件图中，作者能查看和修改原逻辑。
- 形成唯一链路：正式原始Fact → 图内计算/Set → typed只读动画变量 → 当前真实Pose消费者。
- 移除同义派生Fact计算/字段/旧读取，保留行为和明确所有权；完整C#导出/生成能够重建同一图内容和引用。

**Non-Goals:**

- 不新增移动、步频、播放倍率、平滑策略、转移阈值或另一套Gameplay状态。
- 不修改Action/Foot/BlendShape权重归属，不把曲线作为更新图可写变量。
- 不把Pose状态机选择、source采样、混合、相关性、IK、根事务或Writer搬进全局事件图。
- 不恢复Document、motor桥、第二变量表、兼容Fact回填、fallback或另一个MCP。
- 不新增测试或验证任务，不以反复Build/回放作为任务；用户自行端到端验证。

## Decisions

### D1. 固定数据接入、动画更新、姿势消费三个职责

C#的事实接入负责Body/Intent时间对齐、选择已提交采样、身份/Reset代际、原始值与时钟的只读发布。它不再拥有下表列出的动画派生公式和跨帧加速度历史。

EventGraph负责对原始输入执行普通数学、判断、变量Set/Get及动画实例历史。数学节点可以调用已有纯函数；业务阈值、分支和状态更新必须在图上表达，不以“计算全部动画变量”的大节点隐藏原C#段落。

PoseGraph及其底层模块读取这些结果后继续原算法；不是把最后渲染姿势反推成角色事实。MovementMode仍是正式Gameplay/Intent事实，原21处使用它的Corin转移规则不替换为新的速度判断。

### D2. 原始Fact输入合同

CharacterPresentationFactFrame保留正式只读接入位置，但拆掉被迁移的派生字段。原始输入从对齐的Body/Intent提供，至少包括：

| 输入 | 来源与类型 | 用途 |
|---|---|---|
| Velocity | Body.VisibleVelocity，Vector3 | 图中提取XZ/Y、计算速度与方向 |
| Rotation | Body.VisibleRotation，Quaternion | 图中从forward常量得到当前朝向，再计算原朝向误差 |
| Grounded | Body.TargetGrounded，Bool | 原运动阶段分类的外部事实 |
| DesiredPlanarVelocity | 同次Intent的Vector2 | 原期望方向计算 |
| DesiredFacing | 同次Intent的Vector2 | 原朝向误差计算 |
| HasMotion | 同次Intent的Bool | 原GroundedMoving/Stationary判断 |
| LocomotionPlanarBasis | 同次Intent已有Vector2 | 保留既有只读基准，不新计算第二份 |
| MovementMode与movement clock/timeline/time | 已提交Intent | 保留原状态规则和逻辑时钟，不在图中重建Gameplay状态 |
| delta、Actor/采样/tick/Reset代际 | 正式宿主/帧身份 | 图更新、历史初始化与对外帧一致性 |

输入字段的正式identity和类型由事实所属模块统一声明，宿主从同一schema及typed读取能力适配，不维护独立业务字段表。身份/时钟可以保留在帧头；无业务读取需求时不必全部变成作者端口。旧派生字段不能作为原始输入继续供应同一计算。

正式节点使用AddAuthoringNode、ConfigureHostInput、ConnectAuthoringPorts等已有API；Velocity/Rotation等节点必须实际连接到下表计算。HostContext能读取值本身，不构成图内容接入。

### D3. 明确迁移清单与原公式

当前源码基线为CharacterPresentationFactProjector.Project及ResolveMotionPhase。以下是必须迁移的现有动画数据，不再标成可有可无的候选；保留同一数据可用能力，不因当前Corin某节点暂未消费就随意改变原合同。

| 原字段 | 图变量稳定ID | 图内表达，保持原数学 | 消费迁移 |
|---|---|---|---|
| HorizontalSpeed | animation.horizontal-speed，Float | planar=(Velocity.x,Velocity.z)，speed=length(planar) | 原速度Fact读取改为此变量；不添加新的播放倍率消费 |
| VerticalSpeed | animation.vertical-speed，Float | Velocity.y | 原垂直速度读取及本图MotionPhase分类 |
| MovementDirection | animation.movement-direction，Vector2 | speed > 0.0001 时 planar/speed，否则零向量 | 原方向消费者按typed变量读取 |
| DesiredDirection | animation.desired-direction，Vector2 | DesiredPlanarVelocity.sqrMagnitude > 0.00000001 时normalize，否则零向量 | 原期望方向消费者 |
| HorizontalAcceleration | animation.horizontal-acceleration，Float | 有上次样本且delta>0时length(planar-previousPlanar)/delta，否则0 | 原加速度数据消费者；历史留在图实例 |
| FacingError | animation.facing-error，Float | forward3=Rotation*Vector3.forward，facing=normalize(forward3.xz)，SignedAngle(facing,DesiredFacing) | RootOrientationWarp等原朝向误差消费者；求解数学不变 |
| MotionPhase | animation.motion-phase，现有MotionPhase枚举 | 未落地：VerticalSpeed>0为AirborneRising，否则AirborneFalling；已落地：HasMotion或speed>0.0001为GroundedMoving，否则GroundedStationary | PoseStateMachine.SelectPredictiveTarget、相关条件/MM选择等原阶段消费者 |

MotionPhase保留现有枚举类型语义及值：GroundedStationary=1、GroundedMoving=2、AirborneRising=3、AirborneFalling=4。本次只改变其生产owner和读取来源，不新增运动状态，不把Int/Float或字符串假装成枚举。

首次运行与Reset时图内hasPreviousSample=false，previousPlanarVelocity初始化为零。更新先计算公开变量，再保存本次planar并设置hasPreviousSample=true。旧Projector仅为这项计算保存的previous frame/velocity历史随迁移删除；Body/Intent插值队列、时间和分支身份继续由事实接入层持有。

这些公式可以组织成原生Macro以便阅读，但不能封装回一个包含全部决策的C#业务方法。先后顺序、阈值、分量、normalize和Set须能在图内追踪。Graph不反写原始Fact或Gameplay。

### D4. 类型和唯一变量合同

当前仅覆盖Float/Int32/Bool的Pose交接不足以迁移方向和MotionPhase，本版明确补齐本次所需的Vector2/Vector3、Quaternion只读输入及MotionPhase枚举。只增加真实迁移所需类型，不要求一次支持任意CLR对象/集合。

原生变量声明、值编码、宿主输入、配置API、能力目录、C#输出、帧读取和对应Pose绑定使用同一类型。枚举携带正式枚举类型身份和值，不以不透明object跨运行边界；方向不能通过多份临时全局Float别名模拟。Quaternion为原始观测输入，若无持久变量需求，不强迫作者另存一份旋转状态。

仍只有一份原生Blackboard声明和运行状态、一份派生Contract/Layout以及一次成功更新后的只读Frame。Pose需要的消费句柄在原Compiler或实例绑定阶段解析，不每帧扫描全部规则重建输入表。曲线和节点配置不参与实例变量声明合并，旧Parameters对同一实例变量的重复作者定义删除；允许编译器生成只读执行页。

### D5. Corin必须完成真实内容与消费

保留CorinAnimationEventGraphAuthoringCode作为正式生成入口，改为使用原生输入、计算、分支、Get/Set、Macro及连接生成D3的实际更新逻辑。它不再是只创建Start/Update的占位recipe，也不能因上一版允许“无图”而被删除。若旧占位资产已移除，沿同一生成入口恢复明确范围，保持稳定逻辑图/变量ID及Profile绑定，不手改YAML。

Corin通过真实图得到既有动画派生量；Pose侧必须将原消费者接回对应变量。至少包括：
- 现有状态机预测选择读取本图MotionPhase，类型和分支结果保持原样。
- 朝向误差的既有RootOrientationWarp读取改为本图FacingError，存在该能力的图按显式消费绑定。
- 原Fact条件、MM chooser和其它已登记派生Fact能力，逐项迁移到同一动画变量来源，不能留下旧Fact getter作为兼容。
- Corin现有MovementMode条件保持直接读取原始正式事实，不能为了出现变量Get而改写其判断。
- Pose根/节点/规则中明确声明真实使用的变量引用或输入，不能仅通过运行器硬编码某个变量名取值，不能将未消费的声明计数当成接线完成。

这里承接的是已有算法所需数据，不要求新增BlendSpace资源或让新变量改变当前动画效果。没有对应能力的角色不强行安装Warp/MM；但不能据此删除D3迁移工作或让Corin继续空图通过。

无任何动画更新需求的其它根仍可遵从明确无图合同。Corin已有D3动画计算，因此本次不能用“无需求、去掉空图”作为完成结果。显式绑定的合法图仍按原生语义执行，缺必需变量或类型不匹配仍失败，不补默认值。

### D6. 保留原生运行与更新顺序

正式顺序是：对齐原始Fact → 初始化/本次原生Update → 计算并Set动画变量及历史 → 冻结typed帧 → Pose推进和原底层求值。C#宿主仅驱动与交接，FlowCanvas仍原生运行，不创建事件IR/Compiler或第二时钟。

已经成功的原生更新状态在随后Source Pending时保留；Pose继续原Pending/Committed、Barrier和Fault规则。节点错误或部分Set失败不发布结果，故障实例停止；Reset/Body discontinuity/Replacement完整重建原生历史。所有消费者完成前不覆盖只读帧，不向Worker传可变Variable或Unity对象。

现有同步事件合同、宿主delta、未准入Wait/Timed Split/全局时间模式限制继续保留。这是本动画宿主约束，不删除未来关卡宿主可使用的原生能力。

### D7. 逐文件所有权和迁移依赖

| 所属任务 | 唯一负责内容 |
|---|---|
| EventGraph本任务 | Runtime/BTSMTL/EventGraphs的输入节点/类型/原生API/实例变量与帧；CharacterAnimationEventGraphHost；CorinAnimationEventGraphAuthoringCode及其真实图内容；EventGraphAuthoringCodeAdapter薄适配 |
| Pose/Presentation任务 | CharacterPresentationFactFrame/Projector的原始数据合同和派生字段/历史移除；CharacterAnimationInputContract、CharacterAnimationPoseInputFrame；Pose节点/条件/StateMachine、RootOrientationWarp和MM等原派生Fact消费者；Compiler绑定、Blackboard与完整Preview消费 |
| C# authoring任务 | 公共输出器、生成上下文、两个MCP与通用表达/保存；事件图只提供领域类型和调用输出适配 |

本版明确要求跨任务接口变化，但不授权本实现窗口直接改其它Owner的文件。先交付原始Fact和typed变量合同，图内容与消费接口按同一版本接通；不能一边保留原C#派生公式，一边再在图中计算相同真相，也不能用临时回填Fact绕过消费侧迁移。

现有旧Document/motor桥的删除结果保持，不将它们重新引入。派生Fact迁移是新的明确删除范围：只删除D3计算及其被替换的声明、字段、getter、专用历史、旧条件/选择配置；保留原始数据、时间对齐、MovementMode、素材曲线及引擎算法。

需要对接的具体消费签名和布局仍由Pose任务维护，本任务通过正式文档和自己的实现记录提供合同；不联系其它实现窗口或抢改文件。如果真实代码与本版要求出现必须改变Owner/业务范围的矛盾，按既有协议记录实际冲突，不用普通编译进度触发协调。

### D8. C#作者与资产重建

继续复用公共btsmtl.export_code和btsmtl.generate_assets。前者读取当前完整图，后者执行精确已编译创建代码并保存明确范围；人工编辑不自动导出，生成不合并未导出的修改，二者不自动Build。

新增向量/旋转/枚举、历史变量、Macro、分支和连接都必须通过同一直接API与薄适配完整表达；不支持字段明确拒绝完整导出，不以删节点、常量或默认值补齐。原生图/变量逻辑ID写入C#，内部引用使用本次生成对象，Profile根明确恢复；旧生成GUID不作为内部依赖。保留EnsureRoot和正式保存，不重建Agent协议或中央Validator。

## Risks / Trade-offs

- 这次会破坏旧派生Fact读取合同，需要生产、消费者和内容共同迁移；好处是动画计算真正由作者图拥有，而不是仅增加空运行壳。
- 把规则集中到一颗C#节点能少接线，但作者仍看不到原判断。本版采用可追踪原生计算和分支，图可用Macro组织。
- 原生图增加每帧执行成本；只迁移现有动画更新业务，保留Pose/Worker和节点局部引擎逻辑，不宣称复制了UE线程安全执行机制。
- 枚举/向量迁移不能靠float或字符串代替；必须在唯一类型合同与实际消费者一起落地，不发布双reader过渡模式。
- 原算法行为必须保持，尤其XZ坐标、0.0001/0.00000001阈值、首帧加速度和Reset历史；不照抄UE示例的0.1阈值。
- 不新增测试或验证任务，用户自行端到端验证；实现记录如实说明已改范围，不以空图、Profile绑定或Build成功代替业务接入。

## Migration Plan

1. 固定D2原始观测与D3变量/公式/消费对照，撤销上一版“保留MotionPhase在C#、删除Corin事件图接入目标”的任务方向。
2. Pose/Presentation提供原始Fact字段与声明，EventGraph补齐同一typed输入、变量和输出合同，保持原时间对齐与Gameplay真相。
3. 将D3数学、分支和历史生成到Corin原生事件图；初始化和Update按明确顺序连接，保留原数值行为。
4. Pose侧修改实际消费者、来源声明和静态绑定；保留MovementMode原规则、Pose局部算法与曲线链。本任务不跨Owner修改其文件。
5. 在同一正式链可表达全部数据后，删除旧C#派生公式、字段、专用历史和旧Fact读取；不回填、不兼容、不保留两个生产者。
6. 通过原直接API与公共C#入口更新明确图资产和根引用；缺少必要内容按正式错误处理，不能清空图来通过产品构建。
7. 同步本任务规范和实际实施记录，任务只列实现/迁移/删除/文档，不新增测试、验证或反复Build任务。

### 完成定义

作者可在Corin事件图中沿输入节点找到D3已有计算、判断、Set和历史；Pose的实际原消费者读取同一次唯一变量结果。C#不再重复生产这些派生量，仍负责原始输入和底层执行；原动作表现和数值规则保持。通用runtime、数据类型、UI、直接API和完整C#输出共同表达真实内容。空Start/Update、变量列表或Profile绑定不满足该定义。

## Current Spec Comparison

| 来源 | 当前差异 | 本版处理 |
|---|---|---|
| UE官方作者分工 | EventGraph/更新函数产出动画变量，AnimGraph求姿势；另有Node Functions/线程安全函数 | 采用职责分工，保留本项目原生FlowCanvas与Pose编译，不声称UE所有计算都必须全局事件执行 |
| 上一版本change | 将MotionPhase留在C#，Corin可以没有业务事件图 | 明确被本版替代；D3现有动画计算必须迁移，Corin真实接入不能删除 |
| character-presentation-pose-graph现行Fact/参数条款 | 派生Fact与条件读取来源需变更 | Pose任务修改其完整输入/条件/MM等delta；本任务规定唯一生产合同，不重复抢改同名规范 |
| character-animation-pipeline | 接入顺序和数据所有权需体现原始Fact与动画变量区别 | 本任务delta补齐派生生产迁移及消费要求，底层事务不变 |
| readonly-blackboard任务 | 既有输入整理/完成状态不覆盖本次明确的派生计算迁移 | 按D7列为新接口依赖，不能拿旧勾选状态替代新的消费修改 |
| 公共C# authoring | 新类型/真实图内容需要完整读取和输出 | 保留公共协议，补本领域薄适配，不增加新的MCP或源码同步 |
| 已删除Document/motor桥 | 不是本次业务计算迁移的替代成果 | 保持删除，不恢复旧数据源 |

本轮只修改本任务文档，不修改其它规划窗口的文件。公共原始Fact/消费者范围已在本设计中明确，不让实现通过临时接口自作解释。

## Open Questions

- 各领域直接API的具体方法名以实际正式实现为准，业务字段、类型、公式和消费归属以D2/D3/D7为准。
- 若代码发现仍有表外的原动画派生字段，应先定位原生产者和消费者，再按同样边界补入迁移清单；不扩大为Gameplay逻辑迁移或添加新的动画效果。

不存在“是否迁移MotionPhase”“是否让Corin图保持空白”的未决路线。

## Workflow Binding

用户在已授权实现过程中确认UE式作者方向，当前修订是对活跃任务的纠正。完成文档后只向原实现窗口发送一次DOCUMENT_UPDATED，停止按上一版删除Corin接入目标的方向推进；不创建新窗口，不向协调或其它任务发送进度。

- planning_revision: r4-ue-animation-update
- revision_date: 2026-09-13
- authorization_source: 用户确认“对，要参考ue的”
- authoring_baseline: remove-agent-authoring-use-native-csharp/design.md r2
- action: IMPLEMENT
- direction_confirmed_by_user: true
- confirmed_by_user: true
- confirmed_revision: r4-ue-animation-update
- planning_document_owner: 01a095f2-ed45-7502-93f7-e9c9df0b7279
- implementation_dispatched_for_r4: true
- implementation_dispatch_status: DOCUMENT_UPDATED_r4_sent
- last_dispatched_implementation_revision: r4-ue-animation-update
- planning_document_paths: 本目录proposal.md、design.md、tasks.md及specs下四份规范
- implementation_document_path: D:/Unity_Project_1/3C/openspec/changes/add-flowcanvas-event-graph/execution.md

实现只改授权代码和自己的execution.md，不修改规划文档，不新增测试或验证任务。只在真实合同/所有权冲突时按正式协议发送一次文档指针，不发送普通编译、运行或完成汇报。

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

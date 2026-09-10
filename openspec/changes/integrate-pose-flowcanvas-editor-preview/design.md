## Context

本文件是`integrate-pose-flowcanvas-editor-preview`的新目标设计，直接替换原“接通FlowCanvas UI即可收口”的方案。动机见proposal.md。2026-09-09作者明确要求基本照搬UE的作者组织：分图、层内状态机与Slot、Montage路由、骨骼混合与Control Rig；保留现有编译／Native运行方向。

已有基础：原生画布与端口、typed Mutation、稳定identity、flat graph catalog、状态／规则下钻、真实Actor观察、v28来源元数据、Native／Job帧事务。现有Corin根图为11个节点、12条连接，完整作者资产为8张Pose图、25个节点，状态机为7个状态与21条转换。历史计数只描述盘点基线，不是新模型容量限制。

尚未完成的是作者职责重组：根图仍展示Action Playback Input、Pose Parameter Resolve、Foot Placement、Goal Assembler与FBBIK内部流程；Slot仅允许根图，Player还通过Source Slot／Profile Binding间接选资源。旧UI阶段曾被记为“20/21、只剩Build”，该完成结论已撤回；implementation.md现将它与新范围状态分开记录。

本方案重规划时的Document基线为v6，后续实施已报告v7代码增量，实际状态见implementation.md；现行spec与部分skill仍有旧版本文字，需要一并同步。原Build因Attack、DodgeBack、DodgeForward缺失SkillGraphs而在Pose编译前停止，暴露了动画编译入口绑定Character前端的问题。本方案要求拆开这条依赖，不能只把它登记为永远等待的外部阻塞。

精确资产范围：Definition为`Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`；现有Pose作者根为`Assets/Configs/Character/Corin/Pipeline/Presentation/PoseGraphs/CorinPresentationPoseGraph.asset`；Fixed产物目的地为`Assets/Configs/Simulation/DeterministicRollback/Programs/CorinFixedProgram.asset`。新增层或Rig作者资产必须进入该Definition的正式引用闭包，不通过名称或当前Selection猜根。

## Goals / Non-Goals

**Goals:**

- 作者按UE的AnimGraph、Animation Layer、State Machine、State Pose、Transition Rule、Action Timeline（Montage职责）、Control Rig分工编辑，操作和数据组织一致，而非只替换节点标题或外观。
- 每份动画、图、Mask、Profile、Montage与Rig配置只有一个可编辑owner；主图引用子图和资产，不复制内部内容。
- 动画模块可在没有合法SkillGraphs、Numeric Program或运行角色时独立编辑与编译；跨Gameplay的实际输入绑定由角色装配负责，缺失动画自身输入仍明确失败。
- 主图表达基础动画、动作插入、分层混合、惯性化和身体修正；内部读取、组装和调度由同一Compiler展开。
- 当前Corin的动作、动画资源、过渡设置、Foot／IK算法和单次最终写入在迁移时保持；无法无损表达的旧配置明确停止迁移并定位冲突。

**Non-Goals:**

- 不运行FlowCanvas getter／协程，不引入RigVM、第二PlayableGraph、第二动作时钟或第二骨骼Writer。
- 不新建平行动画Montage资产，不将所有通用Timeline重命名为Montage，不重做技能图、Action admission、战斗窗口、Gameplay root motion或网络模型。
- 不移植UE引擎源码、UE资产格式或整个Control Rig工具箱。Backwards Solve、Sequencer录制／烘焙、通用RigVM脚本、编辑器控制器操纵器不属于本次作者组织重构。
- 不把已有FinalIK FBBIK伪装成UE PBIK：对齐骨骼／目标／权重的作者分工，算法参数按真实后端提供，不能摆出没有实现效果的UE专用参数。
- 不创建预览场景、假输入或窗口播放器；观察只消费普通Unity Play中的真实实例。
- 作者数据迁移只处理Corin；TrainingEnemy等未稳定资产不在范围，不能顺手重建、修曲线或迁移其Rig。

## Decisions

### 1. 图层次与作者入口

| 作者表面 | 输入／输出 | 作者在这里做什么 | 不属于这里的内容 |
|---|---|---|---|
| AnimGraph | Fact／参数／资源引用 → 最终Local Pose | 组合状态机、Slot、动画层、Blend、Inertialization、Control Rig | 动作时间轴正文、Goal打包、Native调度 |
| Animation Layer | 明确的Pose／参数／资源接口 → Pose | 封装可复用身体部位、武器或姿势功能；可包含状态机和Slot | 隐含骨骼范围、私有Montage播放实例 |
| State Machine | 表现Fact → 当前状态组合Pose | Entry、State、Alias、Transition及转换策略 | 普通数值线式状态连线、玩法状态机逻辑 |
| State Pose Graph | 本状态输入 → Pose | Sequence Player、Blend Space、Blend或层调用，也可放Slot | 状态转换条件、动作准入 |
| Transition Rule | Fact／状态时间／相关动画剩余时间 → Bool | 进入下一状态的条件 | 播动画、World query、写骨骼 |
| Control Rig | 输入姿势形成的Rig hierarchy＋目标／参数 → 修正姿势 | Forwards Solve、控制目标、Foot Placement、FBIK及已有骨骼控制 | Montage播放、状态选择、Goal Assembler作者节点 |
| 有限Action Timeline（Montage职责） | 原生Clip、Slot轨道、Sections、混合设置与原玩法轨道 → 已提交动作播放结果 | 在原时间轴编排动作动画与同步事件 | 第二Montage资产、复制Clip曲线或独立动画时钟 |

上述图统一使用已有原生GraphEditor及领域合同。State Machine的连接表达“允许转换”；AnimGraph连接表达Pose／数据依赖；Rig图的执行连接表达Forwards Solve顺序，目标数据连接保持独立。不能用同一种普通Value Port表现所有图。

serialized目录继续是flat catalog：State、Layer和Control Rig调用引用稳定GraphId／EntryId及接口签名。库资产拥有自己的catalog，根Definition持有引用闭包。嵌套是作者导航关系，Runtime不展开可变对象树。

### 2. Corin根图和动画层的目标组织

基础全身组织不是固定必经链。Locomotion是持续基础姿势生产者，不是Animation Slot；BlendStack只在该分支确实需要多源选择历史时使用；Inertialization是一个通用Pose节点，只在对应Transition Rule选择该逻辑时经过。Action与Locomotion可以在不同图边界放置同一种Inertialization节点，但不能创建两个按业务命名的专用Node Kind。

```text
Locomotion State Machine
    → [BlendStack：需要多源选择历史时]
    → [Inertialization：转换规则选择时]
    → Base Pose
    → FullBody Action Slot
    → [Layered Blend Per Bone：局部身体层需要时]
    → [Inertialization：对应规则选择时]
    → Control Rig: Body
    → Output Pose
```

Full Body Slot接在Locomotion之后是UE允许的Full Body Action插入方式，但它不能把Locomotion连续性、Action历史和惯性化混成一个owner。Slot自己的Action Stack只管理Action endpoint；Runtime不得因为存在Slot而自动创建BlendStack或Inertialization。图中是否出现这些节点，必须由实际作者拓扑和对应规则决定。

分层动作采用以下语义，不强制把所有内容放进根图：

```text
Locomotion Base〔State Machine → BlendStack → Inertialization〕
    ──────────────────────────────────────→ Base Pose
UpperBody Layer〔State Machine → Slot → Inertialization〕
    ──────────────────────────────────────→ Layer Pose
                                              ↓
                                  Layered Blend Per Bone〔Mask、Alpha〕
                                              ↓
                              FullBody Action Slot（需要时）
                                              ↓
                               Action Inertialization
                                              ↓
                                  Control Rig → Output
```

动画层有两种合法接口：只输出一份姿势，由调用者配置按骨骼混合；或显式接收Base Pose，在层内部完成按骨骼混合后输出。由接口和图中节点决定，不增加隐含的UpperBody／LowerBody运行类型。层名称、Slot名称都不会自动生成Mask；同一层不能在内部和外部不知情地重复应用Mask。

Animation Layer复用现有Linked Pose Interface、Implementation、Group和selector体系，统一作者名称和职责，不再创建第二套Layer runtime。组用于明确的Implementation／实例共享，默认调用状态按Actor、implementation generation和call-site隔离。共享组也不能把两个角色的状态合并。

Source Pose被多个分支使用时，提供Save／Use Cached Pose作者表达或明确的同帧值复用。缓存是当前帧、当前调用范围内的同一求值结果，不是上一帧姿势，不拥有播放器、状态机或独立时钟。层调用和缓存使用完整接口，递归与跨作用域悬空引用在编译前失败。

业务取舍：直接在AnimGraph串接骨骼控制更适合少量局部修正；Control Rig层适合多个目标共享一次身体求解。Corin采用后者，局部Modify Bone等明确Pose控制仍可以按其空间合同使用，不强迫所有图都包含Control Rig。

### 3. Player直接选动画资源

作者名称对齐Sequence Player、Blend Space Player及项目已有Motion Matching能力。Sequence Player直接显示原生AnimationClip对象选择器；名称不代表恢复已删除的Animation Sequence包装资产。资源也可来自动画层明确声明的typed资源参数，两种绑定只能选一种。

Player设置包含Animation、Play Rate、Start Position、Loop Animation及相关性／进入重置行为。Loop成为本次source usage的明确播放策略；导入资源的循环信息仍属于素材信息，不再强制决定每个使用点。迁移时把原Clip循环语义写成对应Player配置，保持Corin原行为；Sync Group与Phase编译按实际usage clock处理，不能把有限播放错误当成循环成员。

删除作者层Source Slot与Profile source binding的两次选择。Player或资源参数是引用的唯一作者owner，Compiler从它与Rig／分析合同生成dense binding、source usage及采样计划。Profile继续装配Rig、Foot Analysis、控制图、层Implementation、Slot定义等角色配置，不复制节点动画引用。

### 4. Montage、Slot、Group与内部AnimationChannel

四者明确分开：

| 概念 | 唯一职责／owner |
|---|---|
| Montage职责 | 由现有有限Action Timeline承担；Slot轨道、Clip片段、Sections、Blend In／Out、Blend Profile In／Out均由该资产拥有 |
| Slot | Rig／Skeleton对应目录中的稳定动画插入点；AnimGraph节点引用它 |
| Slot Group | 已接受Montage播放的互斥组；不描述骨骼范围 |
| AnimationChannel | 已有Program到Presentation的内部动画指令路由，编译时与Slot Group／Slot合同绑定，不直接充当作者Slot名称 |

Layer的实例共享Group与动画Slot Group是两个不同合同：前者决定层Implementation的状态共享，后者决定动作播放互斥。它们不共用identity，也不能因为名字相同而自动关联。

Slot节点保留Source Pose、Slot Name和Always Update Source Pose等作者设置，输出Pose。删除Action Playback输入端口和独立Action Playback Input作者节点。没有活动Montage时透传合法Source Pose；存在活动Montage时消费同一已完成播放结果；释放时回到当前Source Pose。Corin迁移保持原持续更新基础姿势的行为。

Slot可以位于动画层或状态Pose图。层内Slot仍使用角色动画实例的Slot路由，放入子图不自动私有化。多个Slot引用同一播放入口时共享播放实例与source采样，分别在各自Pose上下文混合；不能因多个消费者重复推进动作。重复使用同一姿势优先通过Cached Pose表达，并保留准确的每个调用观察。

Montage轨道选择Slot；同一Montage的轨道属于同一Group。不同Group可以并行，前提是Gameplay已经允许对应动作且AnimGraph有明确组合；同一Group的新合法播放使用既有Action播放owner处理旧播放的退出。Group规则处理动画播放，不在Slot节点中重新判断技能准入、优先级或取消。稳定事件顺序与generation由现有committed指令合同提供，不采用Editor时间或到包顺序。

现有有限Action Timeline提供Clip片段的时间范围／速率，并扩展Section名称和下一Section、Blend In／Out时间／方式／Curve、Blend Profile In／Out及Auto Blend Out。Play、Stop、Jump to Section、Set Next Section由同一Timeline控制入口处理；Section循环受该Action lifetime约束。动画采样、玩法窗口、Motion与MotionWarp使用同一Section选择和raw time结果，没有另一份Montage游标或Update时钟。跳段必须遵循原Timeline对跳过、退出和重新进入窗口的正式事件规则，不能只跳动画而让伤害窗口继续旧时间。

BTSMTL Timeline的动画轨道继续直接引用原生AnimationClip，轨道上声明Animation Slot；游戏窗口、Cue、Motion与MotionWarp仍是同一Timeline的原有内容。根位移继续由Gameplay Motion处理，不能从最终动画反推角色移动。UE Montage的Notifies对应这里已有的事件／窗口作者能力，不另加一套重复事件数据。

这里沿用已有owner，只对齐UE Montage的动画组织和设置。正式名称保留Action Timeline，文档用“Montage职责”解释对应关系；通用Scene／工具Timeline不因此获得角色Slot规则。当前动画片段、资源引用和玩法窗口原位保留，只迁入缺少的Slot／Group及混合配置，不重新复制一份时间轴。

### 5. 骨骼范围、权重和混合策略

| 设置 | 定义在哪里 | 使用在哪里 |
|---|---|---|
| Branch Filter | Layered Blend Per Bone节点 | 按指定骨骼、后代及深度展开范围 |
| Blend Mask | Rig／Skeleton对应资产，逐骨骼权重 | Layered Blend Per Bone选择引用 |
| Layer Alpha／Blend Weight | 节点字段或显式数据输入 | 控制该姿势层当前贡献 |
| Transition Duration／Blend Mode／Curve | 转换边 | 控制状态之间怎样过渡 |
| Montage式Blend In／Out | 各有限Action Timeline的动画设置 | 控制动作进入与退出 |
| Blend Profile | Rig拥有的共享逐骨骼混合配置 | 转换、Montage或惯性化请求引用 |
| Effector Position／Rotation Weight | Control Rig目标或FBIK配置 | 控制目标对求解的影响，不替代动画Mask |

例如攻击和换弹可引用同一UpperBody Slot，经同一上半身Mask组合；两份Montage各自拥有不同淡入淡出设置。Slot Group不会替代Mask，Blend Profile也不作为Slot私有骨骼列表。

Mask覆盖Physical与Virtual Pose Bone；未知骨骼、跨Rig数据和缺失required映射明确失败。Mask为0表示该骨骼的混合结果选择Base对应数据，不承诺其世界位置固定，因为祖先骨骼仍会影响它。下游Control Rig也可以继续修正动画混合结果；不引入“蒙太奇永久认领骨骼”的第二ownership系统。

Layered Blend Per Bone提供Branch Filter／Blend Mask、输入层顺序、Alpha、Mesh Space Rotation／Scale Blend和Curve Blend Options。冲突层按明确图顺序与权重组合，不按节点位置或显示名称选赢家。Root Motion相关显示必须说明它受本项目Gameplay位移合同约束。

### 6. 曲线、参数和惯性化

普通变量／Fact输入仍可接权重端口；可作为常量的值直接在节点详情设置，也可明确暴露为输入。Curve Blend Options属于实际组合节点，参数默认传播由Compiler展开。删除每张根图必须接Pose Parameter Resolve的要求；Corin同一Slot结果接两次Resolve的模式迁到相应组合配置。确有单独改曲线需求时使用明确的Modify Curve作者能力；不能借此恢复通用内部Resolver节点。旧曲线规则无法表达为目标节点设置时报告具体规则并停止迁移。

Inertialization仍是显式Pose节点，不能藏在Slot、状态机或Output中。Transition和Montage混合设置声明惯性请求，沿真实Pose依赖传到下游指定处理节点；Source Map保留请求来源。不同局部分支各有自己的history，正常首帧、NoPose、Invalid和Reset不伪造连续目标。

UE式同一处理节点可以承接多项上游请求；请求集合必须有界，采用最短请求duration，其他设置绑定到该请求，等长按稳定owner顺序选择。每个请求自身有唯一时间设置owner，不继续要求“整个下游节点只能有一个直接Player／时间owner”。现有残差、rebase和Foot前置阶段复用，不能改成全局request bus。Corin原Standard Blend保持，不能为了补一颗可见节点而改变过渡方式。

### 7. Control Rig作者图与单一身体求解

Control Rig引用已有Rig定义，Entry把输入Pose放入本次求解的hierarchy；Forwards Solve执行关系安排控制步骤，数据端口传递目标。可用作者能力包括已有Modify Bone／空间转换、目标Transform与权重、Foot Placement目标生成、FBIK及明确输入／输出。骨骼和目标选择来自Rig层级，不手填运行时slot index。

```text
执行关系：Forwards Solve → Foot Placement → Full Body IK → Return
数据关系：Foot Placement的左右脚目标 ─┐
          手／其它明确控制目标 ────────┴→ FBIK Effectors
          输入Pose ─────────────────────→ 本次Rig hierarchy
```

Foot Placement参数按角色已有Profile组织；作者选择脚部配置和Alpha，得到带可用性、目标骨骼与权重的目标。FBIK配置Root、Effectors、目标位置／旋转权重及真实后端支持的求解参数。多个来源竞争同一Effector时必须通过显式目标混合／选择表达，不能由连线顺序覆盖。Foot内部Pelvis响应、可达处理、Goal编码和BendHistory维持原算法与owner。

Control Rig节点对AnimGraph声明Local Pose输入／输出，Compiler在该声明边界展开必要Local／Component转换；Rig目标的Component／World空间仍明确，任意不同空间连接不能自动cast。独立使用Skeletal Control或空间转换节点仍遵守其显式端口合同。

编译后的Goal Contribution、唯一Goal Assembler、Goal Set与FBBIK步骤保留。它们由FBIK作者请求和Effectors连接确定，不由Runtime临时补建。当前项目允许每个角色最终计划一个身体求解位置、每帧至多一次Foot事务和一次FBBIK；没有Control Rig／FBIK请求就不生成求解链。多处复用已完成结果不会重复求解。

与UE的明确差异：本项目继续使用现有FinalIK FBBIK，不将PBIK的Preferred Angles、Stiffness等无对应实现的字段摆成可编辑假功能。已有可对应的Root、目标、位置／旋转权重直接提供；后端特有的Pull／Reach等真实参数在Advanced里按原名说明。多个独立FBIK求解器、Backwards Solve及RigVM脚本不是本次迁移的目标。

### 8. 作者节点到内部operation的展开

| 作者意图 | 唯一Compiler展开 |
|---|---|
| Sequence／Blend Space Player | 资源binding、usage／clock、demand和source capture |
| Slot | 已有Action播放读取、Source／Montage混合、release、曲线传播 |
| Layered Blend Per Bone | dense mask／branch expansion、各层骨骼与曲线混合 |
| Animation Layer／Control Rig调用 | 精确接口绑定、call-site展开及状态／值范围 |
| FBIK Effectors | typed Goal编码、一次Assembler、一次Goal Set和FBBIK |
| Cached Pose | 同帧producer唯一求值、值引用复用与固定寿命 |
| Output Pose | 本图return；只有最终根输出进入Final Publication |

保留既有Compiler Pass链，在Node Definition的语义展开中生成typed IR和内部依赖，再交统一Topology、Stage、Value Lifetime、Workspace和Seal。生成规则必须由作者节点／配置唯一确定；Source Map把内部步骤归到其作者owner，并保留内部步骤种类便于按需展开。

删除旧Action Playback Input、Pose Parameter Resolve、Goal Assembler作为可创建／可序列化作者节点的路径。对应运行operation按实际消费者保留，不因为隐藏作者步骤而删除正确的后端实现。禁止第二套旧作者图中转、运行时默认补节点或为未迁移资产切换旧Executor。

### 8.1 独立Pose编译与角色装配边界

当前错误链为“Pose窗口Compile → Character Semantic Frontend → SkillGraphs发现 → Presentation／Pose编译”。改成以下正式依赖关系：

```text
动画根＋Rig＋资源＋Animation Input Contract
                 ↓
          唯一Pose Compiler
                 ↓
       独立Pose编译结果／依赖清单
                 ──────────────┐
                              ↓
Gameplay／技能 → Gameplay编译结果 → Character装配与接口绑定 → 原子发布
```

两个入口调用同一个Pose Compiler。独立编译不是绕过总Build的特殊模式；总Build本来就应该是两个模块的调用者。Pose Compiler不能向上调用Character前端，不能为了获得动画输入声明而先遍历技能图。

**正式输入：**动画根或完整动画Profile、Rig及骨骼／Mask／Profile目录、可达Pose／Layer／Rig图、直接动画资源、所引用Timeline的动画内容，以及Animation Input Contract。具体资源参数必须在动画根的绑定中明确提供；库图仅声明接口时可以产生接口诊断，不能凭空生成可执行资源。

**Animation Input Contract：**由动画作者侧拥有，声明所需Fact／参数的稳定identity、类型／单位、合法默认值，Slot／Group接收的播放消息形状及必要World能力。它不含假速度、假Grounded、假Action或预览角色；这是输入接口声明，不是运行采样值，也不从过期Gameplay Program反推。

**不允许的输入依赖：**SkillGraph对象、技能执行拓扑、完整Character Semantic IR、Numeric Target布局、Character ProgramHash、网络模式和场景角色实例。参数若最终由Gameplay提供，独立编译只绑定动画侧typed输入handle；Gameplay地址／offset在角色适配表中处理，不能烙进可复用Pose模块。

**正式输出：**不可变Pose Program Image、Rig／资源manifest、Source Map、各图及资源依赖identity／hash、动画输入接口和所需运行能力。资源清单与Image分责，Image不保存Editor对象。结果由同一编译模块序列化和缓存，独立编译成功可以保存这一结果，不需要先有Character产物；它不能冒充已经完成接口装配的角色Projection。

**角色装配：**分别调用Gameplay Compiler和同一个Pose Compiler，或复用输入hash精确匹配的正式结果；将committed Fact、参数和Timeline播放指令映射到动画输入handle，并绑定资源、Rig及World能力。此时才要求SkillGraphs、producer、Slot路由和Numeric Target完整，并生成角色的Program／Projection绑定与统一发布组。角色集成失败不能删除、伪装失败或重新定义已成功的独立Pose编译结果。

**运行边界：**既有Pose Runtime只接受自己的Program／资源／Rig绑定和typed帧输入，不回查技能图或Character作者对象。适配器由角色装配层拥有，负责把Gameplay提交结果送入动画模块。仍然只有现有Native／Job与一个Final Writer；独立编译不附带另一套Runtime、预览场景或窗口时钟。

**Timeline边界：**独立Pose编译只读取现有Action Timeline声明的动画轨道、Slot／Section／混合设置等动画合同，不编译该Timeline的技能控制或战斗逻辑来“发现”这些字段。Timeline仍是单一作者owner和单一时钟；真正运行时由原Timeline提交播放结果，动画侧只消费。不能复制成动画专用Timeline或独立Montage资产。

**Editor与Document：**图窗口默认命令是“编译动画”，只需动画上下文；“发布角色”是另一个明确的装配命令，只有具备角色上下文时可用。动画详情、资源目录和结构约束来自动画模块，不借Gameplay编译填充。Document仍使用同一五生命周期和整包事务，动画字段处理不依赖Gameplay编译成功；真实跨域引用冲突仍按原事务报告，不增加skip-validation开关或第二个局部写入服务。

**失败归属：**缺少动画参数声明、Rig、资源或控制目标属于Pose编译错误；缺少SkillGraphs、producer或角色接口映射属于角色装配错误；运行时缺少实际合法输入按既有Unavailable／Fault处理。三类错误分别定位，不用默认值、旧Program或简化模式掩盖。

业务收益是动画模块可以独立制作和诊断，技能作者的未完成工作只影响角色集成。代价是将当前隐含输入改为明确的Animation Input Contract，并维护唯一角色绑定表；这份表是模块连接，不是第二份动画作者数据。

### 9. 编辑体验和普通Play观察

节点创建目录按当前图角色组织；状态机只显示状态／Alias／转换，Rig图显示控制与求解，根图不展示内部operation。状态和转换的命中、箭头、平行边与Details采用UE式语义，转换不再借普通数据端口连线表达。

导航由唯一原生图编辑器拥有。双击、目录、Details命令与运行定位均携带完整调用路径，面包屑返回正确父页面；多次引用同一层时不能默认跳到第一处。作者布局按图保存，平移／缩放／选中与观察目标属于窗口状态。

Details默认显示资源、数值、策略和必要命令。Mask、Blend Profile、Slot／Group、Montage引用使用typed资产或声明选择；GUID、hash、compiled index与空运行字段默认隐藏。普通选中、拖动、悬停、Inspector重绘不解码大型Program或进行Build。

普通Play绑定真实Actor，精确区分Session／Actor、generation、Program／Projection／Rig、作者图版本、层Implementation和call-site。节点与端口只显示已完成采集结果；条件边只使用实际读取，当前条件与预判分别选择；未采集、覆盖和0分开。作者一个Slot或Control Rig节点可以按需展开其内部操作诊断，不把这些操作重新变成作者节点。

退出Play、关闭、销毁、替换和重载释放兴趣与租约；Unity暂停保留最后完成帧。观察不推进Montage、状态机、Clip或Solver，不提供窗口播放器。

### 10. 唯一数据owner和Document v7

| 数据 | 作者owner |
|---|---|
| 动画骨骼轨迹与注册Curve | 原生AnimationClip |
| 动画组合与节点参数 | 所属AnimGraph／State Pose／Animation Layer图 |
| 状态、Alias、转换与Rule | 所属State Machine与Rule图 |
| Layer接口／Implementation／Group绑定 | 现有Linked Pose体系演进后的唯一合同 |
| Slot定义／Group、Mask与Blend Profile | Rig／Skeleton对应资产；Profile只引用 |
| Montage式Slot轨道、Sections与混合设置 | 现有有限Action Timeline资产，与原动画片段和玩法轨道共用owner |
| Rig控制拓扑与目标绑定 | Control Rig资产；共享Rig／Profile引用不复制 |
| 玩法时序、窗口、位移与动画播放命令 | 既有BTSMTL Timeline／Action |
| dense binding、Goal组装、Stage、Workspace | generated Program／Projection，作者不可写 |

以当前v6为唯一输入基线，目标v7一次升级。v7保留技能Macro及其它领域全部现行业务字段，新增／调整动画图角色、层接口、Timeline动画设置、Slot目录、Mask／Profile与控制图分片，并移除旧Source Slot／作者内部节点字段。版本和新字段必须同时进入Catalog、严格codec、Exporter、Reconciler、Mutation、owner事务与MCP说明，不只改UI。

建议的正式分片职责：profile.json保存装配引用；graphs的graph.json／layout.json保存各图正文和布局；state-machines保存状态与转换；既有timeline.json／curves.json原位扩展Slot轨道、Sections和动画混合设置；Rig目录保存Slot／Group／Mask／Blend Profile；Linked接口／Implementation沿用既有owner。具体目录使用当前Store的canonical identity分段规则，不另加watcher或manifest外自由文件。

五生命周期仍是checkout、dry-run、apply、rebase、validate；不新增逐节点／逐骨骼写入API。v7严格拒绝旧包并要求重新checkout，不常驻v6兼容reader。旧Unity作者资产仅通过显式、一次性的Editor迁移入口转换，不能放在Runtime加载、窗口打开或Getters中。

### 11. 旧数据与代码去向

| 现有内容 | 新去向／删除边界 |
|---|---|
| Pose根图、State图及稳定引用 | 保留可复用identity，补图角色与接口 |
| Source Slot／Profile source binding | 还原精确资源引用到Player／显式资源参数；无消费者的子资产删除 |
| Action Timeline里的动画编排 | 原位保留；扩展轨道Slot、Sections和动画混合设置，不创建Montage资产副本 |
| Action Playback Input作者节点 | 删除，归入Slot展开及其Source Map |
| Slot上的时间混合Policy | 按入场／退场方向迁到对应Action Timeline的动画Blend设置；不可表达的exact pair规则报告冲突 |
| Pose Parameter Resolve作者节点 | 迁入真正组合节点的Curve设置；无法表达的规则停止迁移 |
| Foot Placement／目标／FBIK配置 | 进入Control Rig；Goal Assembler作者节点删除，内部operation保留 |
| Inertialization | 保留显式节点，按实际混合配置放置；不自动把Standard改成惯性化 |
| 当前v28来源元数据 | 复用并扩展到新作者边界，最终正式版本一次升级 |
| 旧窗口、旧GraphView、旧作者kind及失效配置 | 完成转换后删除，不保留可编辑副本或运行fallback |

### 12. 现行规范对账与必须同步的冲突

正式规划由status列出的8份文件承载；implementation.md与authoring-inventory.md分别记录实施事实、历史基线和旧能力去向，不把未实施设计写成current truth。下表列出必须随实施同步的其它现行规范；它们的旧条款仍存在，不能在apply时忽略。已有delta文件承载本方案完整行为要求，不另建change。

| 现行来源／精确条款 | 与新方案关系 | 本提案处理 |
|---|---|---|
| character-presentation-pose-graph：Pose Graph必须唯一表达完整表现拓扑；Pose端口必须显式区分空间并允许typed控制目标 | 强制作者摆Contribution／Assembler | 本delta改为作者分层＋编译后显式内部拓扑；保留Goal合法性与唯一输出 |
| 同spec：State-local source必须由Profile binding和provider解析 | Source Slot两次选资源、禁止Player Loop | 本delta改为直接资源／typed参数及usage播放策略；保留provider、readiness和原生Clip唯一性 |
| 同spec：AnimationSlot必须是有限Action的唯一Pose插入口；Pose参数必须通过typed页面和显式解析传播 | 暴露Action输入与必接Resolver | 本delta隐藏内部读取／汇总，Slot／Curve业务设置仍显式 |
| character-animation-presentation-authoring：Presentation Profile必须唯一绑定Pose source；Pipeline Definition 必须引用唯一 Animation Presentation Profile | Profile仍强制保存旧source wrappers和完整低层作者节点清单 | 实施同步改为Profile装配＋Player直接资源＋层／Rig／Montage引用；删除旧authoring消费者，不增加第二来源 |
| character-animation-layer-runtime：持续Pose与有限Action控制边界必须分离；每类连续性必须只有一个明确owner | 禁止旧Layer catalog有保留价值，但不能误杀新的作者Animation Layer | 保留旧runtime Layer删除结论；新Layer基于现有Linked实现与编译范围，Runtime owner仍唯一 |
| character-pose-inertialization：Inertialization时间数学必须由触发owner唯一提供；Inertialization必须位于native Pose阶段且早于FootPlacement | 当前限制恰好一个直接owner／Player，与UE下游接收请求不一致 | 实施同步为每请求唯一owner、节点处理有界请求集合；保留局部history、残差与Foot前置阶段 |
| character-pose-plan-compilation：Typed Lowering／Topology／Family／Source Map相关条款 | 同一Pass链可复用，但作者节点与operation不再一一对应，入口目前依赖Character前端 | 保留唯一Compiler；加入独立动画输入／结果合同，角色Build复用它；内部展开不再依赖技能发现 |
| btsmtl-agent-authoring-document-sync：Presentation分片、typed字段、v4替代v3及失败恢复 | current仍写v4，实际已为v6；作者合同再次改变 | 本delta明确v6基线与v7一次升级，保留完整技能v6字段和整包事务 |
| graph-authoring-domain-framework／editor-shell：旧GraphView、Details、目录、状态表面 | 实现与组织限制旧 | 既有delta更新为原生角色化表面、调用导航和作者有意义的字段 |
| character-animation-clip-authoring：原生AnimationClip唯一owner | 与新方案相容 | Montage不复制素材曲线；Sequence Player名称不恢复Sequence包装资产 |
| btsmtl-timeline-animation-authoring-surface／animation-layer-runtime：Timeline本地编排及有限Action命令 | 原动画轨道扩展Montage式设置及Sections | 实施同步现有payload和Section控制；保留窗口与动画同一时间，不新增Montage资产或播放器 |
| character-animation-transition-routing-module | 唯一Routing与capture／release可复用 | 接收转换／Montage拥有的设置，保持握手与generation，不由Slot另做动作仲裁 |
| character-pose-graph-runtime-architecture：四个Owner、帧事务、单Writer与IK基线 | 与新方案相容，是必须保留的运行边界 | 不更换Foot／FBBIK算法，不新增RigVM、调度器或骨骼写入链 |
| project.md Presentation Direction及全局Authoring编译链 | 显式低层作者节点、旧版本口径以及先Character前端再Pose的强耦合 | 实施同步作者／编译两层口径；有限Action Timeline承接Montage职责，通用Timeline名称保留；AnimationChannel与Slot仍是不同概念；普通Play观察保留；独立Pose编译不经过SkillGraphs，角色总Build仅在装配层要求完整Gameplay输入 |

现有Node Definition中的root-only Slot、旧Source字段、旧29能力数量不能继续充当新范围完成标准。此前代码与实施记录属于可复用基础，任务只保留经过源码确认不受新语义影响的已完成项。

## Risks / Trade-offs

- [作者UI看起来相同但语义不同] → 用上述owner／输入输出合同约束实现，FBBIK／Gameplay位移等差异在字段说明中明确，不放无效选项。
- [把Montage概念做成第二份Timeline] → 动画片段、Sections、Slot设置和玩法轨道都留在原Action Timeline，只有一个时钟、游标和生命周期；不新增Montage资产。
- [Slot、Group与骨骼范围混淆] → 独立typed身份和详情；层内封装不创造隐式Mask或私有播放实例。
- [多个层重复求值或重复身体求解] → 完整call-site、同帧缓存和唯一身体求解位置进入统一编译拓扑；不是运行时看到重复后跳过。
- [旧exact pair混合或曲线规则不能表达为UE设置] → 迁移生成精确冲突清单，不保留隐藏兼容Policy、不猜默认值；交由作者决定业务取舍。
- [共享规范或技能v6仍在修改] → 实施前读取当前合同，保留技能字段；真实同段冲突停止并报告，不覆盖其它正确改动。
- [通过旧Program或跳过技能错误伪装独立编译] → 正式拆出动画输入与结果合同，窗口和角色Build调用同一Compiler；角色发布保留全部接口约束。
- [作者图仍能打开但新产物尚未发布] → 明确Stale；只有显式Build更新产物，不用旧v28冒充新组织完成。

## Migration Plan

1. 在改动作者schema前，使用正式Document导出精确Corin的图、资源、动作动画、Slot与Policy目标；固定完整closure及可回滚owner。只读导出不是提前迁移资产。
2. 完成新Capability、图角色、直接资源、Layer／Slot／Montage、Mask／Profile、Control Rig、编译展开、Document v7和编辑器代码。一次性旧资产读取器仅存在于显式Editor迁移模块，Runtime和普通编辑入口拒绝旧格式；不保留长期双写。
3. 补齐第12节的现行规范与共享合同同步。完成代码后，按唯一事务生成并应用Corin的新作者目标；对保留节点沿用identity，对被内部化的节点保存迁移对应记录，退役旧作者ID而非另留可编辑图。
4. 保留原有限动作片段与玩法窗口，在同一Timeline补齐Slot轨道、Sections与Blend设置；将原Foot／目标／FBIK迁到Control Rig。布局按作者职责重建，状态条件、资源、Loop、时间和IK设置不擅自改变。
5. 事务保存与规范反向导出成功后删除无消费者的旧Source Slot、Binding、作者内部节点及过期编辑代码。任一owner转换或保存失败，原组资产与Document目标一起回滚。
6. 先由动画侧输入完成独立Pose编译并保存正式模块结果，再由精确Corin的Character Build组装Gameplay与同一Pose结果，一起发布Float32、Fixed和共享Projection。SkillGraphs不完整时只阻止角色装配／发布，不阻止Pose编辑或独立编译；不降低集成约束、不绕行、不虚勾发布。

检查、运行结果和性能记录留在实施记录中，不写进tasks。当前原始UE组织决定已确定；字段的代码命名和文件拆分由实施遵守既有模块边界，不作为新增架构分支。

## References

以下来源说明UE行为；上文的Unity资产owner、编译展开和既有后端映射是本项目设计，不宣称UE内部使用同样实现。

- [Animation Slots](https://dev.epicgames.com/documentation/en-us/unreal-engine/animation-slots-in-unreal-engine)：Slot插入位置、Source Pose与Slot Group。
- [Montage Editor](https://dev.epicgames.com/documentation/en-us/unreal-engine/animation-montage-editor-in-unreal-engine)：Slot轨道、Sections、Blend In／Out和Blend Profile。
- [Blend Nodes](https://dev.epicgames.com/documentation/en-us/unreal-engine/animation-blueprint-blend-nodes-in-unreal-engine)：骨骼混合、Branch Filter、Curve设置与下游惯性请求。
- [Blend Masks and Profiles](https://dev.epicgames.com/documentation/en-us/unreal-engine/blend-masks-and-blend-profiles-in-unreal-engine)：骨骼范围与逐骨骼过渡的不同职责。
- [State Machines](https://dev.epicgames.com/documentation/en-us/unreal-engine/state-machines-in-unreal-engine)与[Transition Rules](https://dev.epicgames.com/documentation/unreal-engine/transition-rules-in-unreal-engine?lang=en-US)：状态／规则子图、Alias与边详情。
- [Animation Blueprint Linking](https://dev.epicgames.com/documentation/unreal-engine/animation-blueprint-linking-in-unreal-engine?lang=en-US)：Layer接口、Pose／参数输入与显式共享组。
- [Sequence Player](https://dev.epicgames.com/documentation/en-us/unreal-engine/python-api/class/AnimNode_SequencePlayer?application_version=5.0)：资源、速率、起始位置和Loop设置；本项目不采用UE资产序列化。
- [Control Rig in AnimGraph](https://dev.epicgames.com/documentation/en-us/unreal-engine/control-rig-in-animation-blueprints-in-unreal-engine)与[FBIK](https://dev.epicgames.com/documentation/en-us/unreal-engine/control-rig-full-body-ik-in-unreal-engine)：控制图入口、Root、Effectors及UE PBIK职责。

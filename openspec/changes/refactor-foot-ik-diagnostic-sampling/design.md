## Context

见[proposal.md](proposal.md)。当前Foot采样的上游事实分散在`CharacterFootLandingPredictionDebugRegistry.Published`、`AnimationPresentationRuntimeSnapshotPublisher`、Pose Watch与场景Root Hierarchy读取；Sampler先接Foot事件，再等待同Frame/Completion的Animation Debug View补齐FBBIK与Physical结果。下游虽已把Header、Writer与Reader收进typed Column binding，但仍以大量`Source -> Column getter/setter -> Analyzer Record`手写映射表达一个上千列单体Schema。

current `character-foot-placement-presentation`已经要求Diagnostics只读Committed正式结果；current `btsmtl-runtime-diagnostics`进一步禁止Editor轮询可变Runtime对象。active `refactor-character-pose-graph-architecture`建立`CharacterPoseDiagnosticsProjector`，并唯一生产和控制具体`CharacterFootIkCommittedCaptureViewLease`；本change只是正式下游领域切片，不定义第二View，也不读取Module内部页。新的`add-generated-diagnostic-sampling-framework`唯一拥有Attribute contracts、Schema Compiler、Source Generator、Generated Program ABI、typed packet、Capability Session、Writer、Host Reader／Finalizer和通用manifest；本change必须收窄为Foot IK领域插件。完成但未归档的`add-gameplay-performance-capture-workflow`已经拥有唯一Performance Player BuildIdentity和Comparer，只能通过框架`DiagnosticCapabilitySet`接入Foot Disabled/Capture身份。

## Goals / Non-Goals

**Goals:**

- 让Runtime只发布一个同lineage、成功Seal后的Foot IK committed capture view，Sampler不再二次join或反推Transform。
- 让Foot IK以通用框架Attribute声明字段identity、类型、单位、availability、表与复用分组，并由框架唯一编译器生成Editor与IL2CPP Player共用的具体静态Capture程序。
- 支持多个Foot Sampler Definition在一个Program Definition中组合，共享一次上游冻结与一次union字段提取，由通用Host生成各自基础产物，Analyzer／Publisher只作为下游消费者。
- 保持现有Foot、Pelvis、Goal、Solved、Physical、诊断规则和评分数学，只替换采样、列映射与装配Implementation。
- 为通用`DiagnosticCapabilitySet`提供`character-foot-ik` Disabled/Capture descriptor，不拥有第二Performance构建解释。

**Non-Goals:**

- 不修改Foot、Pelvis、Goal Assembler、FBBIK、BendHistory、Final Writer或评分政策。
- 不建立运行时表达式编译、通用对象反射、字符串字段Dictionary、可写诊断黑板或万能序列化器。
- 不让Sampler插件新增第二World Query、FBBIK、Pose Watch、Physical读取或运行控制权。
- 不实现外部进程私有地址扫描、进程暂停、PDB对象布局恢复、IL2CPP运行时表达式编译或Shared Memory Collector。
- 不兼容读取、迁移或覆盖旧采样包。

## Decisions

### Decision 1: PoseGraph唯一提供具体Foot IK短租约Committed Capture View

`CharacterPoseDiagnosticsProjector`属于PoseGraph Runtime架构。PoseGraph在Frame开始冻结具体`CharacterFootIkCaptureInterest`和View固定容量，只按该interest从Source、Program、Constraint与Final Publication的Committed typed Result组合唯一`CharacterFootIkCommittedCaptureViewLease`。View包含完整lineage以及Foot IK采样合同允许观察的Foot、FBBIK、Physical与Root publication typed事实，但不包含Module引用、Pending页、Vendor对象或Transform引用。字段组选择、Schema和packet容量不进入PoseGraph。

PoseGraph唯一控制该View的生产、Post-Seal同步有效期和失效，并只在成功Seal后发布携带该租约的Foot CommittedSample Event。Generated Handler必须在租约内取得框架packet lease并调用匹配Generated Program，把选中字段复制进框架预分配packet；框架Session和后台线程只持有packet，绝不持有View或下一帧会复用的Runtime页。Foot不实现Bridge。

选择该方案而不直接暴露Committed Bank，是因为Bank仍包含正式Owner的内部布局和下帧可复用存储。选择具体Foot IK短租约View而不是通用Diagnostics DTO，是为了让业务边界和寿命可由PoseGraph静态表达，并彻底禁止第二Snapshot和万能View。

```text
Foot / Constraint / Final Publication Committed Result
                         |
                         v
          CharacterPoseDiagnosticsProjector
                         |
               Foot IK Capture View
                         |
        Foot CommittedSample Event
                         |
       Generated typed Event Handler
                         |
      Framework Program -> typed packet
                         |
      Framework Writer -> Schema-driven CSV
                 /       |       \
             Full     Solver    其它Sampler
```

### Decision 2: Attribute标记AOT-safe诊断Extractor，不污染正式Runtime Result

Foot插件使用通用框架Attribute标记独立Foot IK诊断定义中的Extractor成员，而不是放到Foot State、Constraint Result或FBBIK Runtime类型上。诊断定义不得引用`UnityEditor`，每个Extractor必须是可由IL2CPP静态编译的普通纯函数，只接收Committed Capture Context并返回框架支持的基础值或固定容量表记录；Attribute声明：

- 稳定FieldId与Extractor revision；
- codec kind、单位、availability FieldId/value；
- 主表或子表identity；
- 一个或多个可复用字段分组；
- 原始正式字段或Sampler派生字段分类。

字段分组使用可发现的稳定定义类型和显式artifact identity。Foot Sampler Definition选择字段分组、自己的专项Extractor和通用输出格式；字段不保存具体Sampler列表。因此新增只复用现有字段的新Sampler只需新增Definition，不修改旧Extractor、框架或Host代码。只有新业务字段或新派生事实才新增对应Extractor。Extractor不得使用`object`、`dynamic`、`MethodInfo.Invoke`、`DynamicInvoke`、运行时成员路径、World Query、Vendor对象或场景Transform；派生Extractor只读同一Committed Capture Context。

每个业务字段组使用自己的独立Extractor类型。禁止把全部字段挂在`partial CharacterFootIkDiagnosticFields`一类中央容器上，也禁止依赖另一个字段组的private成员完成读取。可共享代码只限无Attribute的基础值投影，不得承担Left／Right选择、领域分发、Schema注册或Sampler分支；每个Dimension进入Extractor前已经绑定自己的具体View与Metadata。

Foot诊断定义只在`character-foot-ik` Capture构建及Editor诊断编译中存在，Disabled构建由通用Capability编译约束连同typed Event Handler和生成程序一起排除。选择独立诊断Extractor而不在Runtime Result上打Attribute，是为了保持正式运行结果与CSV、单位和插件知识分离；选择普通静态函数和typed Event而不是Editor-only实现，是为了让框架为Editor与IL2CPP Player生成同一调用链。Foot change不复制Attribute、codec、packet或生命周期实现。

### Decision 3: Foot只声明Program Definition，通用Compiler生成AOT程序

Foot插件声明稳定`character-foot-ik` Capability Definition以及Full、Solver、Landing等Capture Program Definition；每个Program Definition显式列出一套或多套Foot Sampler。`add-generated-diagnostic-sampling-framework`的唯一Schema Compiler从编译符号读取这些领域定义，校验闭包、求字段并集、分配dense typed handle并生成具体程序。Foot change不得创建`CharacterFootIkCaptureSourceGenerator`、第二反射Catalog或第二identity算法。

Compiler生成普通C#与canonical Schema descriptor：

1. 以Foot具体Committed View为输入、框架Generated packet layout为输出的静态批量函数；函数直接调用Foot Extractor，同一字段无论被多少Sampler复用，每只脚每帧只求值一次。
2. 通用packet到Foot Sampler列视图的无复制dense handle计划。
3. 主表与固定容量子表的packet layout、codec、Schema identity、Generated Program hash与Generator revision。
4. Host侧`sealed packet -> 主表／子表CSV／typed artifact／manifest`唯一Reader/Writer计划，供现有Analyzer只读消费。

生成程序由框架Source Generator产出并由Unity C#编译器与IL2CPP处理。Editor Play Mode与Capture Player必须调用同一Program identity，Foot插件不得保留表达式委托、反射调用或第二套手写提取器。Vector和Quaternion继续由框架codec稳定展开；Ground Contact、Envelope与其它一对多Foot数据由插件声明独立表Extractor和固定容量record layout，不塞回主行可选列。

Foot CommittedSample Event在Post-Seal View租约内携带同一Frame／Completion lineage与`CharacterFootIkCaptureMetadata`；Generated Handler按声明的Left、Right维度分别从同一框架Session取得packet lease、调用同一个生成函数并提交。PoseGraph仍只发布一次View，Program仍只生成一套字段，Host无需把两个脚复制成两套Field identity。框架Writer与Host生成版本化typed packet、主表／geometry子表CSV和manifest。Analyzer不保存字段Dictionary或第二份列名、顺序和单位清单，只读取一次生成artifact并把同次事实交给Publisher，保持`compact-foot-diagnostic-publication`现行单次解析合同。

选择每脚一个主packet而不是容量2的万能Foot表，是因为框架Table按Sampler整体选择；把全部主字段塞进同一表会使Landing、Solver等Sampler无法独立选择字段组，也无法再表达每脚自己的Ground Geometry子表。选择Side metadata而不是Left／Right两套Field identity，是为了保留现行每脚主行业务语义，并确保多个Sampler复用同一字段时仍只生成一次Extractor调用。

Foot接受框架“显式Program Definition -> 直接展开Union函数”的政策。代价是Sampler组合或Extractor变化必须重新编译Capture Player；收益是Foot不再拥有编译器，且Editor／IL2CPP使用同一可复现Program identity。

### Decision 4: Foot只声明Sampler与生命周期Event，Analyzer位于产物下游

每个Foot Sampler Definition通过通用Attribute声明稳定SamplerId、revision、字段分组、专项字段、表和输出格式。三个Foot生命周期Event声明稳定EventId与Started／CommittedSample／Stopped kind；框架编译器自动发现Definition并生成typed Handler，但不识别Full、Solver、Landing、Left或Right业务语义。Foot Analyzer与Publisher只属于Editor或Controller中的下游产物消费链；Player只包含框架选择的Event Handler、Program、Schema和Writer闭包。

构建Capture Player前，作者选择一个已编译Foot Program Definition；Editor Play Mode也通过同一Program identity启动框架Session。Foot插件从Program的Sampler Set合并唯一typed interest并在下一根表现帧开始冻结。新增Sampler若只消费现有Capture View字段，不修改Runtime；若它需要新的FBBIK瞬时量或正式阶段事实，必须显式扩展Committed Capture合同和对应Owner冻结点，不能通过私有反射绕过。

所有Foot Sampler共享框架为`character-foot-ik`分配的单一有界packet流。通用Host按固定Sampler顺序从同一sealed流生成各自主表／子表CSV和逐Sampler manifest；框架Capability manifest引用这些基础manifest。Analyzer／Publisher在Capability Completed后读取生成产物并发布独立报告身份，其失败不回写已闭合采样manifest。

选择领域Definition而不是Foot中央列表，是为了新增Sampler不修改框架、Host或既有Sampler。选择框架每Capability单一packet流，是为了保证所有生成产物观察完全相同的Frame序列并避免多次大结构复制。

### Decision 5: Foot通过通用`DiagnosticCapabilitySet`进入Performance Build

普通Editor Play Mode在没有选中Sampler时不创建Session、不合并interest、不冻结Foot IK Capture View。选择Sampler后必须先生成并编译匹配Schema identity的Capture Program，再允许开始Session；不得发现生成程序缺失后回退反射或解释执行。

Foot插件向通用`DiagnosticCapabilitySet`贡献`character-foot-ik` descriptor，并声明以下一种模式：

- `Disabled`：用于纯性能基线；Foot Definitions、typed Event Handler、Generated Program、capture页、packet队列与interest不进入Player闭包。
- `Capture`：用于IL2CPP实机行为采样；descriptor锁定Foot Event Set、Program Definition、Sampler Set、Schema、Generated Program、维度、packet layout/capacity与Writer transport，由框架和Performance工作流完成AOT闭包。

Performance workflow继续是唯一Build、Gate、Capture与Comparer Owner，通用框架唯一解释Capability Set identity；Foot change不建立第二Performance Player、Foot专属Build字段、第二identity算法或第二Controller。Capture Player不得在启动后切换Foot Program Definition或加载新字段代码。

选择显式构建变体而不是Runtime bool，是因为同一二进制中的bool仍保留分支、类型和常驻容量。Disabled与Capture、两个不同Sampler Set、两个不同Schema/Generated Program或两个不同packet容量都属于不同BuildIdentity，Comparer必须拒绝它们之间的性能差值；只有完整diagnostics capability identity相同的Capture才能比较。

### Decision 6: 旧采样包只保留证据，新Schema不保留兼容路径

内建Full Sampler迁移当前字段业务含义、单位、availability与主行／geometry拆分；通用Host生成新的基础CSV、顶层manifest、Sampler manifest和Schema identity。Analyzer facts、Publisher明细和七维评分只读新产物。旧`samples.csv`、geometry、analysis与diagnoses保持原目录不变；新Reader不读取旧Schema，也不提供别名、默认值或迁移命令。

回退只通过独立Git提交恢复上一完整Implementation，并继续产生旧identity的新包；运行时不保留双Writer、双Reader或切换开关。

### Decision 7: Host只消费正式packet，不扫描Player私有内存

直接`ReadProcessMemory`无法证明跨Foot、FBBIK与Physical阶段的一致Frame，也无法恢复已经离开栈的Solver瞬时量。它还把IL2CPP布局、ASLR、符号和对象寿命变成未版本化接口，因此不进入Sampler插件合同。

通用框架把格式化与Host Finalization放到Editor或现有Performance Controller；Foot Analyzer／Publisher只读取框架生成并验证的artifact／manifest，不读取Player地址空间。若未来需要跨机器实时流式采集，由框架独立change扩展通用transport；Foot插件不得私自增加Shared Memory、socket或地址扫描fallback。

## Risks / Trade-offs

- [Attribute或Extractor修改后忘记升级revision] → Schema identity同时包含Field descriptor、Sampler descriptor、程序集构建identity与显式Extractor revision；preflight拒绝同identity不同闭包。
- [通用框架Generator版本变化] → Foot Program Definition只消费框架发布的Generator revision和identity；版本变化产生新Foot Schema／Program identity，不在Foot插件复制或钉住第二Generator。
- [生成程序与Foot Attribute声明不一致] → 框架Schema identity闭合Foot descriptor、Sampler Set、Generated Program和assembly identity；Editor Capture、Build与Player握手任一不匹配都在订阅interest前失败，不回退Foot旧程序。
- [选中Sampler Set变化需要重新构建] → Capture Program Request与Build分离并显式显示当前生成身份；构建只消费已完成且hash匹配的程序，不在Build过程中现场换集合，也不在Player启动后补字段。
- [多个Sampler造成IO和分析积压] → Foot复用框架为该Capability分配的固定容量packet池与单一有界流，基础CSV由通用Host生成，Analyzer／Publisher在Completed产物之后运行；溢出由框架使Foot Capability Faulted，Performance再决定顶层Capture结果。
- [短租约Capture View被后台持有] → CommittedSample Generated Handler不得直接入队View；唯一Generated Program在租约内复制到framework-owned packet，类型层不向后台暴露Runtime引用。
- [所谓插件需要新的Runtime瞬时量] → 插件能力只对现有Committed Capture合同开放；新增瞬时量必须修改正式Owner冻结合同并升级Schema，不能私有反射。
- [Performance Disabled与Capture二进制布局不同] → 两者作为不同BuildIdentity；纯性能结论只在相同Disabled身份之间比较，实机诊断只在完整Capture capability identity相同时比较，不宣称Capture开销为零。
- [现有Analyzer/评分仍在变化] → `consolidate-foot-diagnostic-scoring`剩余5.3与本change修改相同Analyzer/Publisher Owner；实施前先冻结其最终schema或发现冲突后停止由用户决策，不覆盖已改对规则。

## Migration Plan

1. 先完成`add-generated-diagnostic-sampling-framework`的Contracts、Source Generator、Generated Program ABI、packet、Session、Writer、Host Reader／Finalizer和Capability Set合同；本change不得先实现Foot专属副本。
2. 按已经同步的`refactor-character-pose-graph-architecture`任务13完成Source、Program、Constraint与Final Publication Committed Result、Projector和PoseGraph-owned具体`CharacterFootIkCommittedCaptureViewLease`；本change只从该租约开始。
3. 在PoseGraph具体View合同与框架Generated Program／packet ABI都闭合后定义Foot CaptureStarted／CommittedSample／CaptureStopped Event与样本维度；不得建立Bridge、第二Capture View或临时Adapter。
4. 使用框架Attribute建立Foot字段目录、字段分组、Sampler Definition、Program Definition、表record layout和输出格式，不修改框架中央代码。
5. 将当前全量Foot Schema迁移为内建Full Sampler和通用Host基础产物，并把现有Analyzer／Publisher／评分接到生成artifact／manifest；保持业务语义，使用新identity。
6. 通过Generated Event Handler、框架Session、packet Writer和Host Finalizer接入Foot多Sampler Capture及现有Launcher控制面，不建立Foot专属Session、队列、Writer或Orchestrator。
7. 向Performance通用`DiagnosticCapabilitySet`注册`character-foot-ik` Disabled/Capture descriptor，并确保Disabled排除Foot插件、Capture只包含匹配Program。
8. 一次删除旧`CharacterFootLandingPredictionDebugRegistry`订阅、PendingFrame join、Pose Watch注册、Root Transform读取、手写Column Source/Record/Header/Reader和旧菜单内部路径，不保留wrapper或双写。
9. 更新current project口径及相关active change对账；严格校验框架、Foot、PoseGraph、Performance与全量OpenSpec。

## Current Spec And Active Change Comparison

- current `character-foot-placement-presentation`与本change方向一致，但只要求“读取Committed页”，没有规定PoseGraph-owned唯一Foot IK具体View、多Sampler共享和无interest时不构造payload；本change以完整MODIFIED requirement收紧。
- current `btsmtl-runtime-diagnostics`禁止Editor读取可变Runtime对象，因此私有虚拟地址扫描、Transform反推和Pending Workspace读取与current spec冲突；本change明确不采用。
- completed未归档`refactor-character-ik-maintenance-boundaries`要求唯一typed列绑定。通用框架从Foot Attribute生成的Field descriptor、packet、Writer、Reader和validator仍是一份typed绑定，因此行为合同兼容；其当前手写Column Implementation不再保留。
- completed未归档`compact-foot-diagnostic-publication`要求Sealed CSV/geometry只解析一次并直接交给Publisher。本change保留该后台顺序，不改成第二Reader或逐报告重扫。
- active `consolidate-foot-diagnostic-scoring`要求采样、Analyzer、Publisher保持一条链且剩余5.3仍修改相同Owner。本change不改变评分，但实施存在真实文件冲突，必须串行冻结或交由用户裁决。
- active `add-generated-diagnostic-sampling-framework`唯一拥有通用Attribute、Schema Compiler、Source Generator、Generated Program ABI、packet、Session、Writer、Host Reader／Finalizer、manifest和Capability Set；本change只实现Foot领域插件。
- active `refactor-character-pose-graph-architecture`唯一拥有Committed Result Projector、具体`CharacterFootIkCommittedCaptureViewLease`及其interest冻结和寿命，本change拥有Foot三个生命周期Event、字段／Sampler／Program Definitions与下游Analyzer／Publisher；框架负责AOT Event Handler、packet、CSV和manifest生命周期且不拥有View。
- completed未归档`add-gameplay-performance-capture-workflow`尚未安装为current spec；现有代码尚无Diagnostic Capability Set，也没有Foot专属构建字段。它必须直接新增通用`DiagnosticCapabilitySet`，同一入口显式承载`character-foot-ik` Disabled/Capture身份，禁止先建立Foot专属字段或第二性能能力。

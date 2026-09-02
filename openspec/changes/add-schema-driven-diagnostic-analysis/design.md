## Context

见[proposal.md](proposal.md)。当前`generated-diagnostic-sampling`已经形成multi Fact Root、AOT直接成员访问、typed packet、Schema-driven CSV／manifest和Disabled零闭包；3C Foot已经接入该链，但提交`b601d933b`同时删除了旧Foot Analyzer、规则Publisher和七维报告，只保留固定输入回放的一小段CSV证据读取。现行生成字段声明仍把revision、unit和groups放在`DiagnosticField`中，Sampler按字段组工作，但字段分类、Host读取和领域分析尚未形成可以扩展到Combat、Network或Animation的独立合同。

用户只要求使用最新Analyzer诊断最新采样，不要求新Analyzer兼容或迁移旧Capture、旧Schema和旧Plan。设计因此允许当前Schema变化后直接修正或删除当前Plan，不建立版本路由、alias、模糊绑定或旧格式Reader。约束继续是Unity 2022.3、Roslyn 3.8、IL2CPP AOT、Player热路径零反射／表达式、普通发布零采样闭包，以及Analyzer绝不进入Player。

## Goals / Non-Goals

**Goals:**

- 让不同业务以Capability隔离，一个真实成员只声明一次采样与必要分类信息。
- 让字段分类保持聚焦：Field、Key、Group分别表达允许采样、稳定关键身份和采样集合。
- 让Sampler按Group选择并由Generator展开字段，不再维护第二份Getter、路径或Field列表。
- 让当前Plan在Host侧动态绑定当前Schema，字段与测试变化不修改Player采样框架或Operator算法。
- 把旧Foot诊断业务算法迁成独立Operator与默认Plan，恢复可定位结论和七维评分。
- 让同一通用Reader／Plan／Operator基础设施以后服务Combat、Network和Animation，而不认识其领域名词。

**Non-Goals:**

- 不兼容旧Capture、旧Schema、旧Plan、旧samples.csv、旧合成geometry或旧Diagnosis Store。
- 不建立运行时诊断脚本、表达式DSL、反射插件发现、动态程序集加载或远程规则下发。
- 不允许Plan创造本次Capture没有采集的事实；新增采样字段仍需要重新编译Capture Player。
- 不把Foot规则、阈值、评分或报告移入通用采样包。
- 不让跨Capability分析成为运行时组合DTO、同步Commit或统一业务Frame语义。

## Decisions

### Decision 1: 使用三个聚焦Annotation而不是巨型Field Attribute

Annotations提供：

```text
DiagnosticField                 允许采样真实成员
DiagnosticKey("stable-key")     稳定关键身份
DiagnosticGroup("group-id")     采样集合成员，可重复
```

普通成员只有`DiagnosticField`。长期被多个Plan依赖的关键成员额外声明`DiagnosticKey`；需要被一个或多个Sampler选择的事实类型、结构分支或叶子声明`DiagnosticGroup`。三个Attribute均受`KK_DIAGNOSTIC_SAMPLING`条件控制，Disabled业务程序集不保留metadata或Annotations引用。

Key的完整作用域固定为`Capability / Table / Fact Root / Key`。它允许同一Fact Root内的成员结构搬家，但Table或Fact Root改变会因基数或事实角色变化而主动破坏绑定。Group identity只在Capability内有意义；Generator从Fact Root到叶子传播Group并合并叶子附加组。

选择独立Attribute而不是继续给`DiagnosticField`增加参数，是为了让字段标记保持稳定，并让Key或Group以后删除时不改变采样资格。备选方案是单一`DiagnosticField(Key, Groups, Unit, Revision...)`，写法短但会持续膨胀且混淆采集、兼容和分析职责，因此不采用。完全无Key／Group的备选方案虽然最小，但会迫使Sampler和Plan重复列字段路径，无法满足维护目标。

### Decision 2: Capability负责业务隔离，Group只负责业务内部采样集合

Foot IK、Combat Hit Resolution、Network Prediction和Animation Pose分别声明Capability与Fact Root。Generator只在一个Capability内发现字段、Key、Group、Table和Dimension；同一个Capture Run可以选择多个Capability，但每个Capability保留独立Program、Session、packet和manifest。

Group不承载业务归属，也不把Combat与Foot放进同一字段集合。典型Foot Group为`core`、`landing`、`ground`和`solver-detail`；Combat可以独立使用自己的`core`、`damage`或`hit-window`，同名Group也不会跨Capability合并。

选择Capability隔离而不是全局Group，是因为不同业务拥有不同Commit时机、Frame／Tick和availability；强行合并会再次产生万能DTO和隐含join。跨业务关联只由上层Capture Run提供公共Run、Actor及显式时间身份，并由分析Plan声明需要的关联输入。

### Decision 3: Sampler只引用Group或IncludeAll

Sampler Definition只保存Sampler identity、选择的Group集合以及`IncludeAll`模式。Generator根据Group闭包生成最终字段集合；Sampler不得列成员路径、Field identity、Getter或Extractor。一个字段加入已经标记的事实分支后，所有选择该Group的Sampler自动包含它。

`core`不是Key的隐式副作用。稳定但昂贵的字段可以有Key却只属于`solver-detail`或`ground-full`；需要进入Core的字段显式属于`core` Group。Full Sampler始终包含全部`DiagnosticField`，不受Group变化影响。

备选方案是用Key存在与否自动生成Core，字段写法更少，但会把“稳定身份”和“低成本常用采样”绑定成一个概念，使昂贵关键表不断扩大Core，因此不采用。Sampler再次列IncludePath的备选方案会产生第二份字段选择真相，也不采用。

### Decision 4: Generator编译唯一Field分类与Sampler闭包

Generator构造一个normalized descriptor graph：

```text
Capability
  -> Dimension
  -> Main / Table
  -> Fact Root
  -> structural member path
  -> Field + optional Key + inherited Groups
  -> Sampler field closure
```

编译期检查Key重复、Group identity非法、Sampler选择空组、Field不可读、类型／codec非法、Table行闭包、availability、Dimension结构、packet布局和容量。Schema同时保存结构Field identity、可选Key和Group集合；运行Capture仍只执行生成的直接成员访问，不读取Attribute或字符串路径。

Group与Key只改变Schema和Sampler closure，不改变业务事实、内存布局或Commit调用。不存在Capture时不访问业务getter；普通发布没有生成Program。

### Decision 5: Artifact Reader提供动态列解析但执行阶段使用typed handle

Host新增通用`DiagnosticDataset`入口。Reader从Capability manifest定位Schema和Artifact，验证Completed、hash、Sampler、表集合与编码后，把CSV header解析为当前Schema Field descriptor。Plan编译阶段将Key或Field identity解析为整数typed handle；Operator循环中只通过handle读取typed scalar、vector、quaternion或Table行，不进行每行字符串查找、字典反射或领域转换。

Dataset只提供：

```text
Field catalog
Main row cursor
Table row cursor
Dimension
opaque sample key
Frame / Tick / Actor correlation values（仅当Capability显式采集）
availability
```

Reader不知道Foot、Combat或其它业务。选择Schema-driven cursor而不是为每个Sampler生成Host DTO，是为了让字段变化不要求重新编译Reader或维护Column绑定。备选方案是直接把CSV反序列化为FootFrame，迁移最快但会恢复固定1249列模型和第二字段真相，因此不采用。

### Decision 6: Plan只负责当前绑定和参数，Operator只负责算法

Plan是Host侧source-controlled JSON，包含：

```text
plan id
一个或多个Capability / Sampler输入
Operator id
input slot -> Key或当前Field identity
参数、窗口、过滤、Dimension选择
规则组合与评分组合
```

Plan不保存人工Schema版本；内容hash只用于说明本次实际执行的内容。每次分析都针对选中Capture的当前Schema重新编译Plan。无关字段变化不影响已绑定规则；必需输入缺失或类型／基数不符时产生绑定错误或`MissingEvidence`。当前业务修改Schema后直接修改或删除当前Plan，不迁移旧Plan，也不保留alias。

Operator是显式注册的Host代码，声明typed input slots、参数合同、适用条件和Category。Plan可以改变阈值、窗口、过滤、组合和评分权重，但不能表达任意循环、公式或脚本。新增真正算法时新增Operator并重新编译Host，不重新编译Player采样代码。

选择Plan／Operator分离，是因为实际业务中测试目的和阈值变化频率高于算法结构；把两者都写死在Analyzer会再次形成单体。采用任意表达式DSL虽然表面更动态，但会引入第二解释器、难以审查的运行逻辑和不受控错误面，因此不采用。

### Decision 7: 结果必须把无问题、不适用和证据缺失分开

统一规则结果为：

```text
Passed
Failed
NotApplicable
MissingEvidence
```

Plan绑定失败、字段availability缺失、Table行不完整或窗口断裂不能产生Passed。Operator拿到完整证据但业务状态从未进入适用域时产生NotApplicable。Report按Category、Dimension和Frame／Tick范围组织Finding，并显式汇总四种状态。

结果目录作为Capture目录旁的独立不可变输出，核心文件为`diagnosis.json`和`report.md`。`diagnosis.json`记录源Manifest和文件hash、Schema hash、Plan内容hash、Analyzer binary hash、规则输入证据和评分分母；它不是第二Capture manifest，也不回写采样状态。

### Decision 8: 多Capability Capture共享Run但不共享事实模型

顶层Capture workflow可以选择例如`character-foot-ik/full`和`combat-hit-resolution/core`。每个Capability在自己的Commit点调用自己的generated lifecycle，公共Capture Run identity由工作流写入各自Metadata。Host分别封存Capability Artifact；单领域Plan读取一个Dataset，跨领域Plan显式读取多个Dataset并绑定公共Actor、Frame／Tick或时间输入。

通用框架不假设Presentation Frame等于Simulation Tick，也不自动按列名join。缺少可证明的关联输入时，跨Capability Plan产生MissingEvidence。选择独立数据集而不是组合Program，是为了避免AOT参数签名和业务生命周期发生组合爆炸。

### Decision 9: Foot只恢复算法语义并按当前Schema重建Plan

从`b601d933b`父提交读取旧诊断源码，逐条建立“规则 -> 原始事实依赖 -> 当前Key／Field -> 当前Operator”的迁移清单。恢复范围包括Contact Plane Penetration、Locked Sole Motion、Landing Path Continuity、Landing State Consistency、Swing Path Jitter、Step Time Candidate、Pelvis／Reach和七维评分。

旧`CharacterFootMotionDiagnosticAnalyzer`中的CSV解析、FootFrame大DTO、旧Column、旧合成Geometry、Publisher、Store、Query索引和旧格式兼容不恢复。可以从当前主表和三张Ground表计算的派生量放入对应Operator；当前Schema缺少真正原始事实时，在实际业务事实成员上补`DiagnosticField`／Key／Group后重新生成，不用近似值或诊断DTO补洞。每条规则只有在当前Full Capture具备完整证据并产生报告后才算迁移完成。

Foot提供两个当前默认Plan：Core Plan只选择Core Capture能够完整支持的规则；Full Plan选择全部已迁移规则和七维评分。Launcher、固定输入回放和MCP共用一个Editor前端服务，Stop只封存，Analyze Last／Analyze Existing显式启动Host分析。分析工作不得发生在`OnInspectorGUI`或Player主线程。

### Decision 10: 通用分析留在独立包，Foot规则留在3C领域Editor程序集

独立`generated-diagnostic-sampling`包新增Annotations／Generator分类能力和Host Analysis基础设施，但不包含Foot Operator、Plan或报告文案。3C新增独立Foot Analysis Editor程序集，只引用通用Host和必要的JSON／基础数学合同，不引用Runtime Fact Root或PoseGraph实现。

选择这一分层而不是把Foot Analyzer放回现有Character Editor大程序集，是为了让未来Combat等领域只依赖通用Reader／Plan引擎，并让Foot删除或重写规则不影响采样包。Foot前端可以由现有Launcher和MCP调用，但二者不直接读取CSV或持有规则。

### Decision 11: Disabled与Capture构建继续按程序集闭包分离

`DiagnosticKey`与`DiagnosticGroup`和`DiagnosticField`一样使用Conditional Attribute。Generator在`KK_DIAGNOSTIC_SAMPLING`缺失时零输出；Capability Capture程序集继续使用领域define constraint。Host Analysis与Foot Analysis都是Editor／Host-only，不进入任何Player。Capture Player只有选中的generated samplers、Runtime和Writer；普通Player继续满足零Attribute、零AssemblyRef、零Program／Session／packet／identity字符串Gate。

## Risks / Trade-offs

- [字段语义改变但保留旧Key会静默误诊] → Key必须描述完整稳定业务事实；坐标空间、执行阶段、对象或单位语义变化时删除旧Key并让当前Plan失败，不提供alias。
- [Group传播使Core意外变大] → Schema输出每个Sampler字段数、Table容量和packet上限；Generator在超容量时失败，实施必须缩小Group而不是扩容掩盖。
- [Full Capture对实机性能扰动较大] → Core与Full保持独立Sampler，由Capture workflow显式选择并在Manifest记录；不把Full设为隐式默认。
- [动态Plan演化成编程语言] → Plan只组合注册Operator和参数；任意公式、循环、反射、脚本和程序集名加载均拒绝。
- [MissingEvidence被误当作通过] → 四态结果与评分资格强制分离，存在MissingEvidence时总分必须标记不完整。
- [旧规则依赖已删除的派生列] → 逐规则追溯到当前原始事实；缺失就补真实采样或保持规则未迁移，不复活旧DTO／Column。
- [跨Capability时间轴无法可靠关联] → Plan必须绑定显式关联字段；框架不猜测Frame与Tick关系，无法证明时返回MissingEvidence。
- [新增Focused Attribute增加业务源码标记] → 普通字段仍只有`DiagnosticField`；Key和Group只用于长期核心或可复用分支，并可在结构分支一次声明后继承。

## Migration Plan

1. 先在独立包拆分`DiagnosticField`、`DiagnosticKey`与`DiagnosticGroup`合同，升级descriptor、Schema、Group闭包和Generator诊断；删除旧revision／unit／groups Field构造，不保留兼容重载。
2. 在通用Host增加Manifest／Schema验证、typed dataset、Plan compiler、Operator registry、四态结果与原子Report Writer；保持现有Schema-driven CSV Finalizer为唯一产物来源。
3. 把3C Foot现有字段声明迁到Field／Key／Group，定义Core和Full Sampler并重新生成唯一Foot Program；不改PoseGraph事实结构、业务计算或Commit调用语义。
4. 从Git历史逐条提取Foot算法和依赖，先迁当前Schema完整覆盖的Operator，再为缺失的真实原始事实补采样声明；旧采样、Column、DTO、Store和Publisher持续保持删除。
5. 建立当前Foot Core／Full Plan、`diagnosis.json`／`report.md`输出和统一Editor前端，接入Launcher、固定输入回放与MCP。
6. 同步`add-generated-diagnostic-sampling-framework`、`extract-generated-diagnostic-sampling-package`、`refactor-foot-ik-diagnostic-sampling`及三项正式动画／Foot规格，删除“Host产物就是最终诊断结论”的冲突口径。
7. 完成独立包与3C受影响程序集编译、OpenSpec strict validation、当前Schema Plan绑定检查及Disabled/Capture产物Gate后，再进入实机端到端采样与诊断。

迁移不提供旧路径并行开关。若某条Foot规则尚未得到当前原始证据，该规则保持MissingEvidence／未完成，不能恢复旧Reader或旧Analyzer作为fallback；其余已经迁移的Operator继续通过当前Plan工作。

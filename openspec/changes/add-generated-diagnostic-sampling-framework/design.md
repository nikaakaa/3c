## Context

见[proposal.md](proposal.md)。当前尚未实现新的Attribute/AOT采样链；Foot change已经提出Source Generator、Generated Program、typed packet与Host Finalizer需求，而Performance代码尚无任何Diagnostic Capability Set或Foot专属Disabled／Capture字段。若先按Foot命名实现通用基础设施，或先给Performance加Foot专属字段，以后增加Camera、Simulation或AI采样时只能复制或再次迁移。

现有约束：Unity版本为2022.3，Source Generator使用该版本支持的Roslyn API与.NET Standard 2.0边界；正式实机目标是IL2CPP AOT；Runtime diagnostics不得反向驱动业务状态；性能构建只有一个Build、Player、Controller和Comparer；项目不接受运行时反射fallback、兼容wrapper或第二采样路径。

## Goals / Non-Goals

**Goals:**

- 形成不依赖Character、PoseGraph或Foot程序集的通用采样contracts、编译器、Runtime packet和Host生命周期。
- 让领域正式Runtime Owner提供具体Committed View，领域插件另行提供具体Capture Metadata，字段／Sampler定义通过两个具体输入类型编译，新增领域不修改框架中央代码。
- 让每个Capture Program在编译期形成具体静态函数，Editor与IL2CPP执行相同程序。
- 让BuildIdentity、packet和Host产物拥有可闭合的统一identity，同时保持不同领域Schema和lineage隔离。
- 以Foot IK作为首个完整纵向插件验证框架深度，完成后删除Foot专属通用基础设施命名。

**Non-Goals:**

- 不建立万能反射序列化器、运行时表达式／脚本语言或动态字段黑板。
- 不统一各领域的Frame、Tick、Completion、availability或评分语义。
- 不替代BTSMTL Trace、Unity Profiler、WPR、日志、线上遥测或数据库。
- 不实现跨机器实时传输、Shared Memory、远程配置或商业Player诊断开关。
- 不让框架直接读取任何Module、Workspace、Transform、World Query或Vendor对象。

## Decisions

### Decision 1: 通用框架按Contracts、Generator、Runtime和Host四层分离

依赖方向固定为：

```text
Generated Diagnostic Sampling Contracts
  ├─ Domain Definitions
  │    ├─ Foot IK Fields / Samplers
  │    └─ Future Domain Fields / Samplers
  ├─ Source Generator
  │    └─ Generated concrete Capture Programs
  ├─ Runtime Session / Packet / Writer
  │    └─ Generated typed lifecycle handlers
  └─ Host Reader / Finalizer
       ├─ Built-in Schema-driven Table / CSV Finalizer
       └─ Domain Analyzer / Publisher artifact consumers
```

Contracts保存Capability、三个生命周期Event kind／identity、Field、Group、Table、样本维度、Sampler输出格式、Program Definition、Schema、packet、manifest和failure identity，不引用`UnityEditor`或具体领域。Generator只在编译期间运行，不进入Player。Runtime只处理生成事件处理器、packet lease、固定容量队列、Writer和状态机。Host内建Schema驱动的主表／子表／CSV与manifest Finalizer。领域Analyzer／Publisher位于生成产物之后，只读artifact与manifest，不进入框架Finalizer或Capture状态机。

领域Definitions可以引用正式Runtime Owner发布的具体Committed View合同和通用Attribute，并自行定义只保存采样固定上下文的具体Capture Metadata与三个生命周期Event。正式领域Runtime Result不得引用Capture Metadata、CSV、Sampler或Generator。领域Owner只在正式边界发布Event：开始时发布`CaptureStarted`，成功Post-Commit／Post-Seal时发布携带短View租约的`CommittedSample`，结束时发布`CaptureStopped`。生成的typed处理器取得lease、调用Capture、提交并封存；领域不再实现Bridge。框架Runtime不定义共同View／Metadata DTO，也不保存短租约，从而避免`object`、boxing、未知泛型和生命周期泄露。

选择四层而不是一个`DiagnosticSamplerManager`，是因为编译期发现、Player热路径、后台传输和Host分析拥有不同依赖与寿命。选择typed Event而不是领域Bridge或框架主动拉取，是为了让正式Runtime Owner唯一决定合法生命周期边界，同时把Session、租包、提取、提交和封存的机械代码全部交给Generator。

### Decision 2: 编译输入使用显式Capability与Capture Program Definition

每个领域通过稳定Capability Definition声明CapabilityId、revision、由正式Runtime Owner提供的具体Committed View类型、领域具体Capture Metadata类型，以及只供该Capability packet验证的typed lineage与cadence descriptor。三个生命周期Event分别声明稳定EventId与Started／CommittedSample／Stopped kind；CommittedSample Event携带短租约View、lineage与metadata。Capture Metadata用于Sample identity、采样时间、目标实例和声明的样本维度，不得回填进Committed View；框架不解释其业务字段。每个Sampler Definition只声明字段组、专项字段、固定表和通用输出格式，不声明Bridge或Host Adapter identity。一个Capture Program Definition以稳定ProgramId显式列出同一Capability下的一套或多套Sampler；它是编译期Sampler Set真相。

Source Generator从Roslyn compilation symbols读取这些定义，构造normalized descriptor并依次执行：

```text
Discover Definitions
-> Validate Lifecycle Events and Sample Dimensions
-> Validate Identities and AOT Signatures
-> Resolve Field Groups and Derived Dependencies
-> Build Sampler Union
-> Assign Dense Typed Handles and Table Layouts
-> Emit Schema Descriptor
-> Emit Concrete Capture Program
-> Emit Typed Lifecycle Handlers
-> Seal Program Identity
```

Editor编译可以生成全部合法Program Definition，便于本地选择；Player Build Request通过Player专属编译输入选择一个已声明ProgramId，Capture闭包只静态引用匹配程序。Build Request不传任意字符串字段集合，也不在Build过程中生成新业务定义。作者需要新的Sampler组合时新增或修改Program Definition并重新编译，然后构建引用其新identity。

选择显式Program Definition而不是为任意Sampler组合运行时组装，是为了生成一个直接展开的Union函数并固定实机开销。代价是新的组合需要编译，但同一Player不会携带无法证明使用的所有组合或运行时dispatcher。

### Decision 3: 每个Program生成具体静态类型，不建立运行时未知泛型

Generator按CapabilityId与ProgramId生成稳定、冲突可诊断的内部类型，例如：

```text
GeneratedDiagnosticCaptureProgram_<CapabilityHash>_<ProgramHash>
GeneratedDiagnosticCaptureSchema_<SchemaHash>
```

生成Capture函数使用领域具体Committed View和通用具体packet layout：

```text
Capture(in DomainCommittedView source, in DomainCaptureMetadata metadata, ref DiagnosticCapturePacket output)
```

函数体直接调用`Extractor(in View, in Metadata)`；Table Count使用同样双输入，Table Field再接收row index，并写入dense typed页。相同FieldId只生成一次求值；Sampler列视图只引用Schema拥有的canonical handle，不复制值或第二份字段descriptor。生成代码不得构造表达式树、反射成员、生成每列Delegate或调用领域中央switch。

选择生成具体函数而不是`ICaptureSource<object>`、反射getter或开放运行时泛型，是为了让IL2CPP看到完整调用图和类型闭包，并使Editor／Player执行身份可比较。生成类型名不成为外部API，外部只依赖Program与Schema identity。

### Decision 4: Schema只支持封闭基础类型族和固定容量表

框架核心type family固定覆盖诊断所需的布尔、有符号／无符号整数、浮点、稳定identity、Vector与Quaternion等明确值类型。领域复合事实必须在编译期展开为这些字段，或声明为拥有稳定record layout和固定容量的子表；不得在packet中保存任意对象、managed引用、Dictionary或运行时type tag。

每个Field descriptor固定FieldId、revision、type family、unit、availability、table、dense handle与source kind。Schema identity闭合Capability、Program Definition、Sampler descriptor、Field descriptor、table layout、codec revision、Generator revision和生成source hash。packet header保存Schema、Program、layout、sample key与领域lineage；主表按type family拥有预分配dense页，子表拥有count、capacity和固定record页。

选择封闭类型族而不是任意codec插件，是为了保持AOT、文件格式和Host Reader可验证。以后确实需要新基础类型时必须升级框架codec与packet layout revision，不能由领域私自写opaque blob规避。

### Decision 5: Runtime Session只管理容量、packet和状态，不拥有领域选择

每个Capability Session的状态固定为`Prepared -> Capturing -> Finalizing -> Completed/Faulted/Cancelled`。Generated `CaptureStarted`处理器创建Session并冻结Program／Schema identity、Sampler interest、cadence identity、packet pool、queue容量、Writer transport与输出闭包。Generated `CommittedSample`处理器只在Event携带的具体View短租约内构造／读取Metadata，按声明的enum／稳定ID样本维度循环取得预分配struct packet lease、调用生成程序并消费式提交；复制、重复提交或过期lease会被version拒绝。Generated `CaptureStopped`处理器提交Completed／Cancelled／Faulted outcome并触发封存。Session不知道Presentation Frame、Simulation Tick、Left／Right或其它领域语义。

主线程不得等待Writer、格式化或Host。Finalize只发布非阻塞请求，由外部轮询sealed Runtime manifest；队列溢出、table溢出、sequence断裂、非法状态转换或Writer失败会发布typed failure、停止接收新sample并使该Capability Session进入Faulted。不同Capability各自拥有独立Program、Schema、cadence、lineage、packet流与Capability manifest；框架不组合顶层Capture，也不把它们合并为万能行。

选择每Capability独立packet流而不是跨领域共享一张Union表，是因为Presentation Frame、Simulation Tick、Camera sample和AI event没有共同sample key。Performance工作流只在顶层编排身份和完成结果，不重新对齐不同领域事实；框架不拥有该顶层编排。

### Decision 6: Host内建Schema驱动基础产物，领域Processor只做业务处理

Player Writer只写packet stream、Schema descriptor引用、运行manifest和必要failure证据。Host Reader验证hash闭包后，以Sampler Schema建立typed row和无复制handle view，并由框架直接生成基础产物：有主字段时输出一个主表CSV，每个被选固定表输出一个子表CSV；主表按sample key排序，子表额外携带sample key与row index。标量对应一列，Vector2／3／4与Quaternion按稳定组件列展开，availability不满足时写空单元格；CSV固定UTF-8无BOM、RFC 4180 quoting与canonical identity转义文件名。

基础CSV与逐Sampler manifest不需要领域代码。领域Analyzer／Publisher只在Capability基础产物Completed后读取typed artifact／manifest，计算评分、报告或发布下游产物；它们不得重新声明Column、Header、CsvBinding、调用Extractor、访问Player进程、查询世界或持有Runtime View，也不得改变采样Capability的生命周期结果。Performance工作流可以独立记录下游分析状态，但不能让Analyzer充当采样Adapter。

选择框架内建CSV而不是每Sampler Host Adapter，是为了让新增普通Sampler真正只需生命周期Event／Field／Sampler／Program Attribute，并保证Schema、CSV和manifest只有一个Owner。代价是框架必须固定通用CSV编码、组件展开、文件命名和表连接规则；领域特殊展示通过下游Analyzer／Publisher产物实现，不能篡改基础CSV。Capture完成基础Finalization后成为Completed，不能把仅有packet的staging冒充最终产物；下游报告拥有自己的成功或失败身份。

### Decision 7: Performance工作流消费通用Diagnostic Capability Set

框架提供canonical `DiagnosticCapabilityDescriptor`与稳定排序`DiagnosticCapabilitySet`。每项保存CapabilityId、Mode、Sampler Set identity、Schema identity、Program identity、cadence identity、packet capacity和transport identity；Program identity闭合Generated Program hash、Generator binary identity、程序集binding与packet layout revision。框架同时提供Capability Set codec与`DiagnosticCompilationClosureProof`：领域插件声明Capture程序集和scripting define闭包，Disabled进入排除证明，Capture进入包含证明。现有Performance Build Request保存该Set并把proof转成Player专属编译输入；Player manifest、Run Request、握手、Capture manifest与Comparer复用同一Set codec和identity。

`Disabled`不编译该Capability的Definitions、typed lifecycle handlers、Generated Program、page、queue或interest。`Capture`只包含选中Program Definition与匹配Event handler的AOT闭包。Performance工作流继续唯一拥有Build、Player、Controller、Gate、Capture根和Comparer；框架不启动Player、不实现第二Controller或另建产物根。

选择Capability Set而不是Foot专属字段，是为了未来新增领域时只增加descriptor。选择编译期Disabled而不是运行时bool，是为了纯性能基线没有诊断布局和热路径；任何Capability身份差异都阻止性能差值比较。

### Decision 8: Foot IK只作为首个领域插件验证框架

Foot IK插件拥有：

- `character-foot-ik` Capability Definition、领域具体Capture Metadata与三个typed生命周期Event；
- Foot、Pelvis、Goal、Solver、Physical与Geometry字段／表Extractor；
- Full、Solver、Landing等Sampler Definition与Program Definition；
- 下游Full Analyzer／Publisher、评分和历史产物政策。

通用框架拥有Source Generator、descriptor validator、dense handle、packet pool、queue、Writer、Host Reader、Schema-driven CSV Finalizer、基础Sampler manifest、可选Processor编排、Capability manifest和Capability Build descriptor。Performance唯一拥有顶层Capture manifest与组合状态。实现后删除`CharacterFootIkCaptureSourceGenerator`等把通用Owner绑定Foot的命名，不保留wrapper；生成的具体Foot程序可以带Capability hash，但不是第二框架实现。

选择Foot作为首个插件，是因为它覆盖数百字段、availability、Vector／Quaternion、固定容量Geometry、多Sampler、IL2CPP和Host Analyzer，足以证明框架深度。框架本change不同时发明Camera或AI字段，只保证它们可通过同一合同接入。

## Risks / Trade-offs

- [为未来领域过度抽象] → 框架只提取Foot实现已经需要的稳定机制；Frame语义、领域View、Analyzer和评分继续留在插件，不新增未被首个插件使用的查询语言或传输。
- [Source Generator与Unity Roslyn版本漂移] → 生成器固定.NET Standard 2.0和Unity 2022.3支持的Roslyn API，Generator revision与二进制hash进入Schema和BuildIdentity；版本不匹配直接编译失败。
- [Editor生成全部Program而Player只生成一个导致差异] → 每个Program Definition拥有相同normalized descriptor和source hash；Player manifest与Editor Capture都核对相同Program identity，生成函数不得根据执行后端改变内容。
- [通用packet为Foot需求变得过宽] → 核心只保留封闭基础类型族、dense页和固定表；Foot专有Geometry record layout仍由Foot插件声明。
- [多个Capability同时Capture造成IO积压] → 每Capability独立有界队列和packet流；任一溢出只使该Capability Faulted，Performance再使包含它的顶层Capture Faulted，不动态借容量或丢帧冒充完成。
- [框架与Performance工作流争夺Owner] → 框架只提供Capability descriptor和Host Finalizer合同，Performance继续唯一拥有Build、Player、Controller、Gate、Capture根与Comparer。
- [Foot change与框架change并行修改同一类型] → 先完成框架Contracts／Generator／Runtime／Host，再让Foot change实现领域插件；发现同文件Owner重叠时停止，不复制临时Foot版本。

## Migration Plan

1. 建立通用Contracts与canonical descriptor／identity，不修改现有Foot采样路径。
2. 建立唯一Source Generator、Schema Compiler和Generated Program ABI，固定Unity 2022.3编译边界。
3. 建立typed packet、固定容量子表、Session、Writer、sealed Reader、Formatter与Host Finalizer。
4. 在Performance工作流新增通用`DiagnosticCapabilitySet`，并禁止引入Foot专属能力字段，保持唯一Build、Player、Controller与Comparer。
5. 将`refactor-foot-ik-diagnostic-sampling`中的通用Attribute、Generator、packet、Session、Writer、Host编排任务迁入本change；Foot change只实现首个Capability插件。
6. 在PoseGraph具体`CharacterFootIkCommittedCaptureViewLease`完成后发布Foot三个typed生命周期Event，并用生成处理器和同一Generated Program覆盖Editor与IL2CPP Capture Player。
7. 删除旧Foot事件／Snapshot join、手写Column链及任何Foot专属通用框架类型，不保留表达式、反射或兼容路径。
8. 更新current specs和`openspec/project.md`，只在实现闭合后安装通用框架与Foot首个插件真相。

回退只通过独立Git提交恢复前一套完整采样Implementation；运行时不保留旧／新Generator、packet、Writer或Sampler双链。

## Current Spec And Active Change Comparison

- current `btsmtl-runtime-diagnostics`要求Editor不轮询可变Runtime对象且诊断不反向驱动运行；本框架只消费领域Committed View并保持该约束，但不进入RuntimeDebugSession Trace Store。
- current `character-foot-placement-presentation`只要求Foot diagnostics读取Committed事实；Foot插件继续负责具体View和字段，本框架不改变Foot业务要求。
- active `refactor-character-pose-graph-architecture`唯一拥有Committed Result Projector和具体`CharacterFootIkCommittedCaptureViewLease`的生产与寿命；框架和Foot插件都不得读取PoseGraph内部页，框架也不得定义通用View。
- active `refactor-foot-ik-diagnostic-sampling`现已回写为依赖本框架，只拥有Foot生命周期Event、Definitions与下游Analyzer／Publisher；后续实施不得恢复Foot Bridge、Host Adapter或Foot专属通用类型。
- completed未归档`add-gameplay-performance-capture-workflow`现已回写为消费通用Diagnostic Capability Set，并继续唯一拥有Player BuildIdentity和Comparer；后续实施不得增加领域专属构建字段。

## Purpose

定义不同运行领域如何通过同一套multi Fact Root编译期Schema、IL2CPP AOT静态采样、typed packet与Schema-driven Host建立可组合、可追溯且可在Disabled Player完全退出的诊断能力。

## ADDED Requirements

### Requirement: 采样框架必须与领域语义和业务事实所有权解耦

框架 MUST只认识Capability、Dimension、Fact Root、Metadata、Field、Table、Sampler、Program、packet与manifest。每个Capability MUST注册一个Metadata类型和一个或多个带稳定Root ID的现有强类型Fact Root；框架 MUST不定义通用View、Dimension View、领域诊断DTO或生命周期Event DTO，也 MUST不识别Foot、Landing、PIK、PoseGraph、角色、战斗、网络或动画等领域概念。业务Runtime MUST继续拥有事实的构造、Commit和生命周期，采样定义 MUST不改变业务结果类型、字段布局或运行算法。

#### Scenario: Foot以多个现有事实接入

- **WHEN** Foot消费方把Landing、Motion、Goal、Solved、Pelvis和Metadata注册为同一Capability的输入
- **THEN** 框架 MUST按通用Fact Root descriptor生成Schema和采样入口
- **AND** 消费方 MUST不创建`CommittedFoot`、`LandingView`或其它重复诊断DTO

#### Scenario: 增加其它领域Capability

- **WHEN** Camera、战斗或网络领域只使用框架已有的Dimension、Fact Root、Metadata和Field合同
- **THEN** 系统 MUST生成该领域独立的Program、Schema和packet identity
- **AND** 通用框架及其它领域插件 MUST不因新领域名称而修改

### Requirement: 普通字段必须由现有成员上的唯一声明形成Schema

每个普通采样值 MUST只在Fact Root或Metadata可达的现有readonly field、getter-only auto-property或调用方可读不可写计算getter上声明一个`DiagnosticField`。Generator MUST从CLR类型推断codec，并从Capability、主表或Table、Fact Root与成员路径生成默认Field identity；同一业务类型作为多个Fact Root或经多条公开成员路径复用时 MUST按Root／Path生成不同identity而不重复叶子Attribute。`AvailabilityMember` MUST在当前Root与路径内解析同级成员。普通成员读取、现有计算getter、enum编码和Unity Vector／Quaternion转换 MUST不要求Getter、Extractor、Projection、Column、CsvBinding或Derived转发。

#### Scenario: 同一事实类型用于输入与输出

- **WHEN** 同一个readonly业务类型分别注册为`formal-input`与`formal-output`两个Fact Root
- **THEN** Generator MUST从同一组成员Attribute生成两个Root路径下的独立字段identity
- **AND** 消费方 MUST不复制类型、不重复Attribute或手写输入／输出映射

#### Scenario: 采样现有计算getter

- **WHEN** 无setter计算getter已经是业务事实且声明了`DiagnosticField`
- **THEN** Generator MUST生成对该getter的直接静态访问并推断其返回类型codec
- **AND** MUST不因它不是auto-property而拒绝、复制为DTO字段或要求Derived转发

#### Scenario: 字段合同非法

- **WHEN** 字段使用未知codec、Root／Path identity冲突、成员图存在循环或availability无法解析
- **THEN** 编译 MUST在Player生成前失败并定位Capability、Fact Root与成员路径
- **AND** 系统 MUST不跳过字段、回退反射或只生成其余Sampler

### Requirement: 固定表必须直接读取现有buffer或class page

固定一对多事实 MAY在现有readonly struct buffer或对调用方只暴露稳定`Count`与只读索引器的class page上声明`DiagnosticTable`。Generator MUST验证capacity、count、索引器和row shape，并从真实row成员的`DiagnosticField`生成固定子表；Capture期间 MUST不把page复制为新集合、诊断表DTO或动态数组。

#### Scenario: class page作为Table根

- **WHEN** Fact Root暴露一个带`DiagnosticTable`的class page，page只有可读Count与索引器
- **THEN** Generator MUST生成直接Count／索引器访问和固定row packet布局
- **AND** 领域代码 MUST不实现表Getter、逐行映射或第二份row结构

#### Scenario: Table超过声明容量

- **WHEN** 当前sample的Table Count超过Program固定capacity
- **THEN** 对应Capability Session MUST以结构化Overflow进入Faulted
- **AND** MUST不截断为Completed、运行时扩容或把剩余row塞入主表

### Requirement: Derived必须只表达真实公式并声明实际Fact Root依赖

只有无法从现有成员直接读取的公式 MAY声明`DiagnosticDerivedField`。每个Derived方法 MUST只接收实际读取的Fact Root，参数名 MUST确定性映射Capability Root ID、类型 MUST一致且使用`in`，最后 MUST接收`in Metadata`；Table公式 MUST再接row index。Generator MUST拒绝未知Root、重复Root参数、错误类型、availability／依赖断裂和依赖环。Derived MUST不承担Side分发、普通成员转发、类型转换或字段搬运。

#### Scenario: 公式读取两个事实根

- **WHEN** 一个诊断公式同时读取Goal与Solved两个Fact Root
- **THEN** 方法 MUST直接声明`in goal`、`in solved`和最后的`in metadata`
- **AND** 消费方 MUST不创建组合View或把两个Root复制进Metadata

#### Scenario: Derived声明未注册Root

- **WHEN** Derived参数名或类型无法匹配Capability中的Fact Root
- **THEN** 编译 MUST拒绝该公式并指出缺失或错误Root
- **AND** MUST不从其它DTO、全局对象或Side选择helper绕过合同

### Requirement: 采样时机必须由DiagnosticEvent和generated typed handler连接

每个Program MUST声明Event identity、稳定Dimension ID数组和Sampler集合。业务 MUST在自选同步Commit边界声明带`DiagnosticEvent`的private static partial void方法，参数直接使用目标、真实lineage与每个Dimension的全部`in` Fact Root；Generator MUST按Event identity和完整参数签名生成目标隔离typed dispatcher及匹配Program handler。参数顺序 MUST由Event签名、Program Dimension顺序与Fact Root ID稳定顺序确定；每个Dimension MUST具有相同Fact Root结构与Metadata类型。Program handler MUST使用业务lineage为每个Dimension自动完成packet租用、Capture与消费式提交，Host workflow MUST在Start提供并冻结共享或逐Dimension Metadata，唯一控制Session Start／Stop和订阅寿命。领域 MUST不定义Started／CommittedSample／Stopped Event DTO、Bridge、Session控制、左右租包循环、Side选择或通用Event Bus。

#### Scenario: 同步提交左右两个Dimension

- **WHEN** 业务在成功Commit边界调用一行Foot `DiagnosticEvent` partial方法并以`in`传入目标、真实lineage及Left与Right的现有事实
- **THEN** generated typed dispatcher MUST调用匹配Program handler并用同一Capture程序分别产生两个Dimension packet提交
- **AND** 调用方 MUST不构造View、不复制采样事实且不逐字段写packet

#### Scenario: Capture停止为Faulted

- **WHEN** 调用方以Faulted outcome调用Generated `Stop`
- **THEN** Session MUST停止接收新sample并封存结构化failure、已有packet证据与runtime manifest
- **AND** MUST不发布Completed身份或要求领域Bridge补做清理

### Requirement: Capture执行必须是编译期生成的AOT静态程序

框架 MUST按Capability与Sampler字段并集生成普通C#具体静态`Capture(in Root0, ..., in Metadata, ref Packet)`。生成函数 MUST展开真实成员访问、按dense typed handle写入预分配主表和固定子表，并只对真正Derived字段调用领域公式；相同Field identity在一个sample中 MUST只求值一次。Editor与IL2CPP Capture Player MUST执行相同Generated Program identity和packet layout。运行时 MUST不构造表达式树、不反射成员、不使用`DynamicInvoke`／`MethodInfo.Invoke`、不解析字符串路径，也 MUST不使用每字段Delegate、解释器或领域switch补齐程序。

#### Scenario: Editor与IL2CPP执行同一输入

- **WHEN** Editor与IL2CPP Capture Player使用相同Capability、Dimension Set、Fact Root Set、Sampler Set、Schema和Generator revision
- **THEN** 两者 MUST声明相同Generated Program hash、字段顺序与packet layout
- **AND** 相同Fact Root／Metadata输入 MUST写入相同typed值

#### Scenario: Capture Request引用旧程序

- **WHEN** Fact Root、成员路径、Field、Sampler、Generator或程序集变化导致Program identity变化
- **THEN** Editor preflight、Player Build或握手 MUST在采样前拒绝旧Request
- **AND** MUST不加载旧Program或回退动态访问

### Requirement: typed packet与Session必须固定、有界且非阻塞

每份packet流 MUST锁定Capability、Dimension Set、Fact Root Set、Sampler Set、Schema、Generated Program、Generator revision、layout revision、容量、sample key和opaque lineage。主表 MUST按封闭类型族保存dense值，Table MUST使用固定record页；不得使用`object[]`、字符串值Dictionary、managed业务引用或Capture期间扩容。每个Capability Session MUST独立执行`Prepared -> Capturing -> Finalizing -> Completed/Faulted/Cancelled`，主线程 MUST只同步读取传入事实并非阻塞提交到单一有界队列，不得等待Writer、Host、Analyzer或Publisher。

#### Scenario: Writer或sequence失败

- **WHEN** queue溢出、sample sequence断裂、非法状态转换或Writer失败
- **THEN** 对应Capability MUST进入Faulted并保留已有可验证证据
- **AND** MUST不影响其它Capability、丢帧冒充Completed或动态借用容量

#### Scenario: Reader打开错误Schema

- **WHEN** sealed packet的Schema、Program、layout或文件hash与manifest不匹配
- **THEN** Reader MUST在生成任何Sampler产物前拒绝整份流
- **AND** MUST不按列数猜测、补默认值或兼容解释旧0.3文档

### Requirement: Host必须只依据Schema和sealed packet生成基础产物

Host MUST只读取sealed packet、Schema descriptor和manifest，由通用Finalizer为每个Sampler自动生成主表、固定子表、UTF-8无BOM RFC 4180 CSV、基础Sampler manifest和Capability manifest。主表 MUST携带sample key与Dimension，子表 MUST额外携带row index；字段顺序、Vector／Quaternion组件和availability空单元格 MUST由Schema确定。Host MUST不持有Fact Root／Metadata、不重新读取业务成员、不调用Derived公式、不访问Player私有地址或执行World Query。基础产物 MUST是采样最终输出而非领域诊断结论；`add-schema-driven-diagnostic-analysis`定义的领域Operator／Plan／Report MUST只消费Completed artifact，不声明Column、Header、CsvBinding或Host Adapter，也不得参与Capture生命周期。

#### Scenario: 普通Sampler没有领域处理器

- **WHEN** Sampler只声明字段组、Table和CSV输出格式
- **THEN** 通用Host MUST仍生成完整基础产物并允许Capability进入Completed
- **AND** 领域代码 MUST不需要Bridge、Adapter、Null Processor或字段映射

#### Scenario: 多个Sampler复用union packet

- **WHEN** Full与专项Sampler选择部分相同字段
- **THEN** Host MUST从同一packet流和canonical handle生成各自产物
- **AND** Player MUST不重复采样相同Field或写第二packet流

### Requirement: Disabled Player必须通过编译期零闭包和产物Gate

框架 MUST使用全局`KK_DIAGNOSTIC_SAMPLING`及Capability专属define控制采样编译。全局符号缺失时Conditional Attribute MUST不写入业务metadata，Generator MUST零输出，Capture Runtime与领域Diagnostics程序集 MUST通过define constraints退出，Host MUST保持Editor-only。虽然Annotations可在源码编译时解析Attribute语法，Disabled Player最终闭包 MUST不把Annotations作为根程序集保留。发布Gate MUST用Cecil与IL2CPP产物检查证明零Diagnostic custom attribute、零Sampling AssemblyRef、零Annotations／Runtime／领域Diagnostics程序集、零Generated Program／Session／packet／queue／interest和零Capability／Field identity。运行时开关、空实现、未订阅、Linker推测或构建后删除 MUST不能替代该证明。

#### Scenario: 构建普通发布Player

- **WHEN** Build Request未定义`KK_DIAGNOSTIC_SAMPLING`且全部Capability为Disabled
- **THEN** Generator MUST零输出且业务程序集Cecil检查 MUST确认零Attribute与零Sampling AssemblyRef
- **AND** Managed及IL2CPP输出 MUST通过全部程序集、类型和identity零残留检查

#### Scenario: 构建Capture Player

- **WHEN** Build Request定义全局和目标Capability符号并选择合法Program
- **THEN** Player MUST只包含匹配Capability的Annotations、Definitions、Generated Program、Lifecycle与Runtime闭包
- **AND** Player manifest MUST保存匹配的Capability Set、Fact Root Set、Schema和Program identity

### Requirement: Player构建必须显式声明通用Diagnostic Capability Set

Player Build Request MUST保存canonical、稳定排序的`DiagnosticCapabilitySet`。每项descriptor MUST包含Capability mode、Dimension／Fact Root Set、Sampler Set、Schema、Program、cadence、packet capacity和transport identity，Program identity MUST闭合Generator binary与packet layout。Player manifest、Run Request、握手、Runtime／Capability manifest和Comparer MUST复用同一codec与identity；Comparer MUST拒绝Capability模式或任一Capture identity不同的性能差值。框架 MUST不创建第二Player、Controller、顶层Capture或Comparer。

#### Scenario: 同时启用两个Capability

- **WHEN** Build Request把Foot与Camera分别声明为Capture
- **THEN** Player manifest MUST保存两个独立Capability闭包且各自读取自己的Fact Root Set
- **AND** 框架 MUST不把两者的lineage、packet layout或Sampler namespace合并成万能Schema

### Requirement: 采样不得反向驱动业务运行结果

Generated Event dispatcher、Program handler与Capture MUST只在同步Commit边界读取调用方提供的现有Fact Root与真实lineage，并只使用Lifecycle Start冻结的Metadata。Metadata MUST只承载采样固定上下文，不得承载Side选择或业务事实。没有匹配目标Capture订阅时dispatcher MUST立即返回；昂贵事实Owner MAY声明`Query{EventMethodName}Interest` private partial方法，Generator只在存在该声明时生成目标兴趣实现。Disabled编译时Event与Query调用及参数求值 MUST由编译器消除，业务 MUST不为采样构造事实、执行额外坐标变换或运行采样getter。普通事实Event MUST不被强制声明Query。任何采样组件不得创建第二业务Tick、查询世界、调用求解器、写Gameplay／Presentation状态或改变权重、目标、配置、随机数、时钟及下一帧事实。Capture写入packet属于采集输出，不是业务DTO复制。

#### Scenario: Sampler产生派生公式

- **WHEN** Capture从两个Committed Fact Root计算仅供报告的派生量
- **THEN** 派生量 MUST只进入Schema、packet和Host产物
- **AND** 任何业务Runtime MUST不读取该派生量参与下一帧决定

#### Scenario: 没有活动Capture

- **WHEN** 当前没有启动对应Generated lifecycle或Player为Disabled构建
- **THEN** 业务提交链 MUST不构造诊断View／Event DTO、不执行额外事实复制或坐标换算
- **AND** 正式业务结果与调用图 MUST不因采样定义而分裂

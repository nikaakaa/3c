## Purpose

定义项目内不同运行领域如何通过同一套编译期Schema、AOT静态采样程序、typed packet与Host Finalizer建立可组合、可追溯且不反向影响业务结果的诊断采样能力。

## ADDED Requirements

### Requirement: 采样框架必须与领域语义解耦

框架 MUST只定义Diagnostic Capability、Sampler Set、Field／Group／Table descriptor、Capture Program Request、Schema identity、Generated Capture Program ABI、typed packet、Capability Session、Writer／Reader、Host Finalizer和manifest合同。每个领域正式Runtime Owner MUST自行定义并发布具体只读Committed Capture View及其lineage、availability与租约语义；领域插件 MUST另行定义具体Capture Metadata保存Sample identity、采样时间、目标实例等固定上下文，并声明双输入Extractor、Sampler、Analyzer与Publisher语义。框架 MUST不定义通用Committed View／Capture Metadata接口、基类或DTO，不把Metadata塞回View，不识别Foot、PoseGraph、Presentation Frame、Simulation Tick、Camera、AI或其它具体领域identity，也 MUST不决定领域Frame／Tick何时成功、哪些事实允许冻结或如何评分。

#### Scenario: Foot IK成为首个领域插件

- **WHEN** Foot IK插件分别注册PoseGraph-owned具体Committed View类型、领域具体Capture Metadata类型、字段、表和Sampler Set
- **THEN** 框架 MUST只按通用descriptor生成和运行采样程序
- **AND** 通用框架 MUST不增加Foot、FBBIK、Goal、Geometry或七维评分专用分支

#### Scenario: 增加Camera诊断插件

- **WHEN** 新Camera插件只使用框架已支持的descriptor、codec、packet和生命周期合同
- **THEN** 系统 MUST能够生成独立Camera Capture Program与Schema identity
- **AND** Foot插件、框架中央代码和现有Sampler MUST不因Camera identity而修改

### Requirement: 字段与Sampler必须通过唯一编译期声明形成Schema

每个Capability MUST在Attribute中同时注册两个不同的具体值输入类型：Committed View与Capture Metadata，并声明唯一Started、CommittedSample与Stopped typed Event identity。每个领域字段 MUST通过唯一Attribute声明稳定Field identity、revision、codec、单位、availability、表归属和可复用字段分组；主字段Extractor MUST使用`(in View, in Metadata)`，Table Count MUST使用相同双输入，Table Field MUST再接收row index。样本维度 MUST通过封闭enum或稳定ID metadata声明，由Generator展开，不得由Bridge手写Left／Right等循环。每个Sampler MUST只声明稳定Sampler identity、revision、通用输出格式、字段分组、专项派生字段与表，不得被要求提供Bridge、Host Adapter或Processor identity。Capture Program Request MUST显式选择一个Capability及一套或多套Sampler。编译期Schema MUST拒绝生命周期Event缺失／重复、未知维度、重复identity、未知分组、断裂availability、非法codec／输出格式、AOT非法Extractor签名和派生依赖环，并按稳定identity排序形成canonical Schema descriptor。

#### Scenario: 多个Sampler复用同一字段

- **WHEN** 同一Capability内三个Sampler选择包含相同Field identity的分组
- **THEN** Schema MUST只保存一份字段descriptor并为三个Sampler发布各自无复制列视图
- **AND** 领域插件 MUST不为每个Sampler重复声明getter、单位、availability或codec

#### Scenario: 插件声明非法字段

- **WHEN** Extractor使用不受支持的返回类型、运行时动态调用或引用未知availability字段
- **THEN** 编译 MUST在生成Player或启动Capture前失败并定位Capability、Sampler和Field identity
- **AND** 系统 MUST不跳过该字段、降级反射访问或只启动其余Sampler

### Requirement: Capture生命周期必须由三个typed Event驱动

每个领域 MUST只在正式Owner边界发布三个采样Event：`CaptureStarted`创建并冻结Session、Program、Schema、容量与interest；`CommittedSample`只在正式事实成功Commit／Seal后携带短租约具体View、lineage与必要Metadata；`CaptureStopped`携带Completed、Cancelled或Faulted outcome。Source Generator MUST为具体Event类型生成AOT typed处理器，并自动完成Session选择、样本维度展开、packet租用、Attribute字段提取、写入、提交、Writer封存、artifact与manifest发布。领域 MUST不实现Capture Bridge、手写Session控制或第二生命周期。

#### Scenario: Foot成功提交一个表现帧

- **WHEN** PoseGraph成功Seal一帧并发布一个携带短租约View、frame lineage与Foot Side维度metadata的CommittedSample Event
- **THEN** 生成处理器 MUST在租约内按稳定维度自动生成对应packet并提交
- **AND** Foot领域 MUST不手写Left／Right租包循环、Extractor调用或Session提交

#### Scenario: Capture以Faulted停止

- **WHEN** CaptureStopped Event声明Faulted outcome
- **THEN** 生成处理器 MUST停止接收新sample并封存结构化failure、已有packet证据与manifest
- **AND** MUST不发布Completed身份或要求领域Bridge补做清理

### Requirement: Capture执行必须是编译期生成的AOT静态程序

框架 MUST按Capability与Sampler Set的字段并集生成普通C#具体静态`Capture(in ConcreteView, in ConcreteMetadata, ref Packet)`。生成程序 MUST直接调用AOT-safe双输入Extractor并按dense typed handle写入预分配主表与固定容量子表；相同Field identity在一个sample中 MUST只求值一次。Editor与IL2CPP Capture Player MUST执行相同Generated Program identity和packet layout。运行时 MUST不构造或编译表达式树、不反射成员、不使用`DynamicInvoke`／`MethodInfo.Invoke`、不解析字符串路径，也 MUST不以解释器、Host手写列或第二提取链补齐缺失程序。

#### Scenario: Editor与IL2CPP执行同一程序

- **WHEN** Editor Capture与IL2CPP Capture Player使用相同Capability、Sampler Set、Schema和Generator revision
- **THEN** 两者 MUST声明相同Generated Program hash、字段顺序和packet layout
- **AND** 相同Committed View输入 MUST通过同一Extractor调用图写入相同typed值

#### Scenario: Capture Request引用过期程序

- **WHEN** Field、Sampler、Generator或程序集变化导致Schema identity或Generated Program hash变化
- **THEN** Editor preflight、Player Build或Player握手 MUST在采样前拒绝旧Request
- **AND** MUST不加载旧Generated Program或回退动态访问

### Requirement: typed packet必须固定布局并携带完整身份

每份packet流 MUST锁定Capability、Sampler Set、Schema identity、Generated Program hash、Generator revision、packet layout revision、容量、sample key和领域lineage。主表 MUST按受支持类型族保存dense值，变长或一对多事实 MUST进入声明容量的独立子表页；不得把不同类型装箱进`object[]`、使用字符串Dictionary保存值或在Capture期间扩容。packet、Schema descriptor和manifest MUST形成hash闭包，Reader MUST拒绝identity、布局、容量或文件hash不匹配的流。

#### Scenario: 固定容量子表溢出

- **WHEN** 当前sample的一对多记录超过Capture Program Request声明容量
- **THEN** Session MUST发布结构化Overflow并使整个Capture进入Faulted
- **AND** MUST不截断为Completed、现场扩容或把剩余记录塞入主表可选列

#### Scenario: Reader打开错误Schema的packet

- **WHEN** Host Reader收到的packet Schema identity或layout revision与manifest不匹配
- **THEN** Reader MUST在生成任何Sampler产物前拒绝整份流
- **AND** MUST不按列数量猜测、补默认值或兼容解释旧布局

### Requirement: Capture生命周期必须有界、非阻塞且原子

每个Capability Session MUST由Generated Started handler在开始前独立冻结Sampler Set、Schema、Generated Program、Event Set、维度、输出格式、cadence identity、packet容量、Writer transport和全部Host输出闭包。Generated CommittedSample handler MUST只在Event携带的具体Committed View租约内读取Metadata、取得预分配struct packet lease并同步调用生成程序；lease MUST以version防止复制后重复提交或过期使用。主线程 MUST只消费式提交到该Capability的单一有界队列，Generated Stopped handler MUST只发布非阻塞封存请求，不得等待文件IO、格式化、Analyzer或Publisher。各Capability MUST拥有独立cadence、opaque typed lineage、sample key、packet流、Writer、Runtime manifest和Capability manifest；框架 MUST不解释Presentation Frame、Simulation Tick、Left／Right或跨Capability对齐关系。任一生成错误、非法状态转换、队列溢出、sample序列断裂、Writer故障或hash不闭合 MUST只使对应Capability Session与manifest成为Faulted，并保留已有证据但不得发布该Capability的部分Completed身份。Performance工作流 MAY依据所选Capability结果决定顶层Capture状态，框架 MUST不拥有该顶层状态机。

#### Scenario: 多Sampler正常完成

- **WHEN** 一个Capability内全部选中Sampler完成相同sample范围、Host产物和hash闭包
- **THEN** Capability manifest MUST引用每个Sampler manifest及共同Schema、Program和sample范围
- **AND** 该Capability MUST只原子发布一个Completed身份

#### Scenario: 下游Analyzer失败

- **WHEN** Capability基础CSV与manifest已经Completed，但领域Analyzer读取产物后失败
- **THEN** Analyzer／Publisher工作流 MUST记录自己的下游失败身份
- **AND** MUST不把Analyzer当成采样Adapter或回写已闭合Capability manifest

### Requirement: Player与Host必须通过sealed packet分工

Capture Player MUST只执行Generated lifecycle handler与Program、提交typed packet并通过声明的Writer transport封存packet流与运行manifest。Host MUST只读取sealed packet、Schema descriptor和manifest，由框架内建Finalizer为每个Sampler自动生成主表、固定子表、CSV和基础Sampler manifest。主表 MUST携带sample key并按稳定Schema字段顺序输出；子表 MUST额外携带row index；Vector／Quaternion MUST按稳定组件列展开；不可用字段 MUST输出空单元格；CSV MUST使用UTF-8无BOM、RFC 4180 quoting和canonical identity文件名。领域Analyzer／Publisher MUST只消费生成artifact／manifest，不得重新声明Column、Header或CsvBinding，也不得参与Capture生命周期。Host MUST不持有Runtime View、访问Player私有地址、重新执行Extractor、World Query或业务求解。字段格式化和分析 MUST不发生在Player主线程。

#### Scenario: Host生成多个Sampler产物

- **WHEN** sealed union packet同时服务Full与专项Sampler
- **THEN** Host MUST从同一packet流建立两个无复制字段视图并自动完成各自主表／子表CSV与基础manifest
- **AND** MUST不要求Player为每个Sampler重复采样或写第二packet流

#### Scenario: 普通Sampler没有领域Processor

- **WHEN** Sampler只声明生命周期Event、字段组、样本维度、表与CSV输出格式
- **THEN** 通用Host MUST仍自动生成完整基础产物并允许Capability进入Completed
- **AND** 领域代码 MUST不需要实现Bridge、空Adapter、Null Processor或字段映射

#### Scenario: Host尝试扫描Player内存

- **WHEN** Host缺少正式packet字段而尝试读取Player私有地址或反射Runtime对象
- **THEN** Capture MUST拒绝该输入进入Completed闭包
- **AND** MUST不把扫描事实与正式packet合并

### Requirement: Player构建必须显式声明通用`DiagnosticCapabilitySet`

Player Build Request MUST保存canonical、稳定排序的`DiagnosticCapabilitySet`，每项`DiagnosticCapabilityDescriptor` MUST显式保存CapabilityId、Mode、Event Set、Sampler Set identity、Schema identity、Program identity、维度、cadence identity、packet capacity与transport identity；Program identity MUST闭合Generated Program hash、Generator binary identity、程序集binding与packet layout revision。领域 MUST声明Capability专属Capture程序集与scripting define，`DiagnosticCompilationClosureProof` MUST让Disabled进入排除闭包、Capture进入包含闭包。Disabled MUST在编译期排除该领域诊断定义、typed lifecycle handler、Generated Program、capture页、队列和interest；Capture MUST把匹配Event handler与Program纳入AOT闭包。Player manifest、Run Request、握手、Runtime／Capability manifest与Comparer MUST使用同一Capability Set codec和identity。比较器 MUST拒绝任一Capability模式或Capture身份不同的性能差值。

#### Scenario: 全部Capability关闭的性能基线

- **WHEN** Build Request把全部诊断Capability声明为Disabled
- **THEN** Player闭包 MUST不包含任何领域生命周期Event handler、采样程序、packet队列、capture页或空占位manifest
- **AND** 运行时bool、Null Adapter或未订阅Session MUST不能冒充编译期Disabled

#### Scenario: 同时启用两个领域Capability

- **WHEN** Build Request将Foot IK与Camera诊断声明为Capture并分别提供合法程序身份
- **THEN** Player manifest MUST保存两个独立Capability闭包且各自只读取自己的Committed View
- **AND** 框架 MUST不把两个领域lineage、packet layout或Sampler namespace合并成万能Schema

### Requirement: 采样不得反向驱动领域运行结果

Generated lifecycle handler与Program MUST只读取正式Runtime Owner在CommittedSample Event中交付的具体Committed View短租约与领域具体Capture Metadata；Capture Metadata MUST只承载采样固定上下文，领域Runtime MUST不读取它，框架Session、Writer／Reader与内建Finalizer MUST只读取packet、Schema与manifest，不得持有Runtime View或Metadata。任何采样组件不得创建第二业务Tick、查询世界、调用求解器、写Gameplay／Presentation状态、改变权重、目标、配置、随机数、时钟或下一帧事实。Analyzer／Publisher只读生成产物。Disabled与Capture构建在相同业务输入下 MUST遵守同一正式运行算法；Capture开销属于独立BuildIdentity，不得伪装为零成本或与Disabled直接比较。

#### Scenario: Sampler派生诊断字段

- **WHEN** Sampler从Committed字段计算一个只用于报告的派生量
- **THEN** 派生量 MUST只进入该Sampler Schema与Host产物
- **AND** 任何领域Runtime MUST不读取该派生量参与下一帧决定

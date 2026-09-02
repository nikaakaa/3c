## Purpose

定义Foot IK如何直接把成功提交后的多来源只读事实交给Generated Diagnostic Sampling，并由通用Host发布基础产物、由领域下游完成分析。

## ADDED Requirements

### Requirement: Foot Capability必须直接声明多个现有Fact Root

character-foot-ik Capability MUST以DiagnosticFactRoot注册现有强类型事实，不得要求消费方合并成采样专用结构。每个Dimension MUST能同时接收foot、input、formal-input、formal-output、effector、leg、pelvis、pelvis-goal、solver、stride、primary-support和frame等所需根；Metadata MUST只保存采样会话固定身份，不得保存Side或业务事实。没有字段的类型 MUST不作为Fact Root，也不得为了维持根集合构造读取DTO。

Left与Right MUST使用同一组Fact Root id和同一Schema。公共Pelvis、Solver、Stride或Frame事实 MAY同时作为两套Dimension参数传入，但 MUST不复制成左右专用字段结构。框架 MUST只理解Dimension、Fact Root、Metadata、Field和Table，不得包含Foot、Landing、PIK或其它领域分支。

#### Scenario: 捕获一帧左右脚事实

- **WHEN** 当前Frame已经成功Seal且同步Commit边界可读取同一Frame与Completion的左右脚和公共事实
- **THEN** Foot业务 MUST只调用一行带`DiagnosticEvent`的partial方法并把对应现有根作为in参数传入
- **AND** MUST不构造CommittedFoot、LandingView、Dimension View、Projection或Side选择对象

#### Scenario: 左右根类型不一致

- **WHEN** Left与Right为同一个Fact Root id传入不同类型、缺失根或不同结构
- **THEN** 生成程序调用 MUST在编译期失败
- **AND** MUST不在运行时按默认值、反射或Side分支补齐

### Requirement: 普通字段必须只由真实成员上的单一Attribute声明

每个被采样的现有readonly field或可读成员 MUST只使用一个path-scoped DiagnosticField声明revision、unit、groups和可选availability。Generator MUST从Capability、main或Table、Fact Root id与递归成员路径形成稳定Field identity，并从真实CLR类型推断codec。Foot MUST不再维护字段Getter、Extractor、Projection、Column、CsvBinding、字段类型表、单位表或Sampler到字段的反向登记。

Foot Player Schema MUST不包含DiagnosticDerivedField。旧跨字段统计、穿透、七维评分、规则报告、Diagnosis Store与Publisher MUST不迁入Generated Capture热路径。`add-schema-driven-diagnostic-analysis` MAY从Completed基础产物恢复算法语义，但 MUST迁成Host-only Operator、Plan与独立报告，不得恢复旧单体Analyzer、Store、Publisher、第二字段映射或旧CSV格式。固定输入回放 MAY按生成Field ID读取自己确实需要的最小证据。

#### Scenario: 新增普通Foot字段

- **WHEN** 作者需要采样一个现有事实中的合法readonly成员
- **THEN** 作者 MUST只在该真实成员添加DiagnosticField并让Sampler选择其group
- **AND** MUST不新增getter、DTO、Projection、CSV Binding或Host Adapter

#### Scenario: 成员类型非法

- **WHEN** 标记成员的类型没有稳定codec，或递归成员图存在循环、重复Field identity、断裂availability
- **THEN** Generator MUST报告编译错误并生成零可用Program
- **AND** MUST不延迟到Player首帧才忽略字段

#### Scenario: Disabled编译字段源码

- **WHEN** 构建未定义KK_DIAGNOSTIC_SAMPLING
- **THEN** Conditional DiagnosticField MUST不进入业务程序集metadata
- **AND** 业务程序集最终 MUST不因这些源码标记保留Sampling AssemblyRef或Field identity

### Requirement: Capture必须由Roslyn生成的AOT静态程序完成

Foot MUST声明DiagnosticSampler和DiagnosticCaptureProgram，并在唯一业务Post-Commit边界声明一个带`DiagnosticEvent`的private static partial void方法。Roslyn Source Generator MUST在编译期遍历多Fact Root和Metadata，按Event identity及完整参数签名生成target-scoped dispatcher、可选partial interest Query、统一Schema、typed packet layout、左右Dimension直接成员访问和匹配Program handler。Event MUST只传target、真实lineage与Fact Root，Metadata MUST由Lifecycle Start冻结。生成代码 MUST是普通C#静态访问，并由Unity C# Compiler与IL2CPP AOT编译。

Foot Runtime MUST在成功Seal后的同步Commit调用栈内只调用该partial方法一次。Generated dispatcher无订阅时 MUST立即返回；匹配Program handler MUST完成左右展开、packet rent、Capture、submit与Fault，Host workflow MUST唯一控制Session Start／Stop。采样链 MUST删除`ICharacterFootIkCommittedCaptureConsumer`、Capture Binding和手写`CharacterFootIkGeneratedCapture.TryCapture`，也不得创建或消费CharacterFootIkCommittedCaptureViewLease、Runtime Snapshot、Dimension View或其它聚合DTO，不得拥有字段映射、packet layout、领域Writer、Host Finalizer、表达式树、反射、dynamic或字符串成员路径执行。

#### Scenario: Capture Program处理Committed帧

- **WHEN** Foot Capture已Start且当前Frame成功Seal并进入同步Commit
- **THEN** Foot业务 MUST一次调用`DiagnosticEvent` partial方法并传入Left与Right的完整根集合
- **AND** generated dispatcher／Program handler MUST在当前调用栈内把两套Dimension写入framework-owned typed packet，后台不得持有业务page

#### Scenario: 构建IL2CPP Capture Player

- **WHEN** Capture Player定义KK_DIAGNOSTIC_SAMPLING与KK_DIAGNOSTIC_FOOT
- **THEN** Player MUST包含与Schema identity匹配的生成静态函数
- **AND** 运行时 MUST不调用Expression.Compile、MethodInfo.Invoke、DynamicInvoke或解释器

### Requirement: Ground Geometry必须使用三张真实class page表

Ground Geometry MUST分别使用ground-contacts、ground-envelope和ground-surfaces三张表。表根 MUST是现有CharacterFootGroundContactPage、CharacterFootGroundEnvelopePage和CharacterFootGroundSurfacePage；每个page MUST提供Count与只读索引访问，行 MUST是现有CharacterFootGroundContact、CharacterFootGroundEnvelopeVertex和CharacterFootGroundSurfaceSegment，行成员继续只使用DiagnosticField。

系统 MUST不恢复按三个Count最大值拼接的旧合成表，不得逐行复制到固定诊断DTO，也不得为每行重复Sample、Frame、Completion、Side和三个虚拟index字段。Dimension、lineage和表行关系 MUST由packet与manifest表达。

#### Scenario: 三种geometry行数不同

- **WHEN** 同一Foot Dimension具有不同数量的Contact、Envelope和Surface行
- **THEN** Generated Program MUST分别按三张真实page的Count写入对应子表
- **AND** MUST不以默认行或负index把它们补成相同长度

#### Scenario: page超过声明容量

- **WHEN** 任一真实page的Count超过Program Schema声明容量
- **THEN** Capture MUST以明确Table或Capacity故障终止该Capability
- **AND** MUST不截断后发布Completed manifest

### Requirement: 多Sampler必须共享同一Generated Program与packet流

每个Foot Sampler MUST只声明稳定Sampler identity、revision、输出格式、groups与Tables。一个Capture Program MAY组合多个Sampler；Generator MUST求选择字段并集，并让同一Field identity在每个Dimension只生成一个Schema位置。多个Sampler MUST共享同一Committed输入和Capability packet流，不得增加Foot查询、Goal Assembly、FBBIK、Final Publication或事实page数量。

#### Scenario: Full与专项Sampler同时启用

- **WHEN** 一个Program组合Full和另一个只选择部分groups的Sampler
- **THEN** 两者 MUST观察相同Frame、Completion与Dimension序列
- **AND** Foot、通用框架和Host MUST不增加针对Sampler identity的手写字段分支

#### Scenario: 未选择Foot Sampler

- **WHEN** 当前构建或Session没有选择character-foot-ik Sampler
- **THEN** 系统 MUST不创建Foot采样Session、packet、queue或输出
- **AND** Foot业务结果与PostCommit行为 MUST保持不变

### Requirement: 通用Host必须自动生成基础采样产物

通用Host MUST仅依据Generated Schema和sealed packet自动生成每个Sampler主表、ground-contacts、ground-envelope、ground-surfaces、Sampler manifest和Capability manifest。Host MUST保存Schema／Program identity、Dimension、Frame范围、文件hash、字段类型、单位和availability，不得要求Foot Column、CsvBinding或Host Adapter。

Foot采样模块 MUST不再拥有旧单体Analyzer、Publisher、七维评分、规则报告、Diagnosis Store或第二manifest。基础产物 MUST不冒充领域诊断结论；`add-schema-driven-diagnostic-analysis`定义的Foot Analysis MAY只读Completed artifact执行当前Operator／Plan并生成独立诊断结果。Launcher与MCP MUST通过唯一workflow分别展示采样artifact和显式分析结果；固定输入回放 MUST只按生成Field ID或通用Dataset handle读取自己需要的证据。

#### Scenario: Host完成基础Finalization

- **WHEN** sealed packet流完整且全部文件hash验证通过
- **THEN** Host MUST发布引用主表与三张子表的Completed Capability manifest
- **AND** Launcher、MCP与固定输入回放 MUST只通过当前Capability workflow取得manifest中的精确产物

#### Scenario: Host写入失败

- **WHEN** 任一基础CSV或manifest写入失败
- **THEN** Capability MUST保持Faulted并保留可诊断staging
- **AND** MUST不发布旧Foot报告、第二manifest或部分Completed身份

### Requirement: Disabled Player必须从编译闭包剥离采样能力

普通发布构建 MUST不定义KK_DIAGNOSTIC_SAMPLING和KK_DIAGNOSTIC_FOOT。Generator MUST生成零Foot代码；Foot Diagnostics与Sampling Runtime程序集 MUST不进入Player；Annotations MUST不作为Player根程序集残留；业务程序集 MUST没有Diagnostic custom attribute、Sampling AssemblyRef或Field／Capability identity。

关闭 MUST通过编译输入和程序集约束完成，不得依赖runtime bool、空实现、Linker猜测、fallback或构建后静默删除。Cecil与IL2CPP Gate MUST检查Managed和native输出。

#### Scenario: 验证Disabled Player

- **WHEN** 唯一Performance入口构建Disabled Player
- **THEN** Cecil MUST确认业务程序集零Diagnostic Attribute和Sampling AssemblyRef
- **AND** IL2CPP输出 MUST不包含Foot Diagnostics、Generated Program、Sampling Runtime、Session、packet、queue、interest或Field／Capability identity字符串

#### Scenario: 比较不同诊断身份的性能结果

- **WHEN** Baseline与Candidate的Capability mode、Sampler Set、Schema、Program、packet capacity或transport不同
- **THEN** Performance Comparer MUST拒绝数值比较并列出身份差异
- **AND** MUST不把采样闭包差异归因于Foot或Gameplay性能变化

### Requirement: Foot采样必须保持只读且不扫描私有内存

Generated Event dispatcher、Program handler、Generated Program、Host与Editor workflow MUST不修改Foot、Goal、FBBIK、Final Pose、Gameplay或Network状态。Player采样 MUST不重新执行World Query、坐标变换、FBBIK或Physical读取。外部工具 MUST不读取Player私有虚拟地址、解析对象布局、暂停进程、扫描托管堆或按PDB地址重建事实。

#### Scenario: 外部工具请求地址扫描

- **WHEN** 工具无法从正式packet取得某个Foot事实
- **THEN** 工作流 MUST报告Schema或Fact Root缺口
- **AND** MUST不使用Shared Memory私有旁路、地址扫描或当前Transform补全

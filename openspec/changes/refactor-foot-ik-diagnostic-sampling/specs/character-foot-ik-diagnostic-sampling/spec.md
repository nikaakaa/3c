## Purpose

定义Foot IK作为Generated Diagnostic Sampling Framework首个领域插件时，如何从唯一Committed表现帧按需冻结事实，并通过Foot字段／Sampler／Program Definitions与Host Adapter生成可追溯、可校验且不反向影响运行结果的采样产物。

## ADDED Requirements

### Requirement: Foot IK采样必须只消费PoseGraph唯一具体Committed Capture View

Foot IK采样 MUST只接收PoseGraph在根表现帧成功Seal后交付的一份`CharacterFootIkCommittedCaptureViewLease`，并核对匹配Frame、Completion、Program、Projection、Rig与Tuning Generation。该具体View MUST由PoseGraph从正式Foot、Constraint/FBBIK和Final Publication Result单向组合输入、Transition、Target、连续状态、Goal、Solved与Physical事实，并由PoseGraph唯一控制租约寿命；Foot插件 MUST不定义第二View、Capture Frame或事实页，任一Sampler MUST不再通过Foot事件、Animation Snapshot、Pose Watch、场景Transform或可变Workspace补齐同一帧。

#### Scenario: 完整Foot IK帧成功提交

- **WHEN** Foot、Goal、FBBIK与Final Publication在同一根表现帧成功完成并Seal
- **THEN** PoseGraph MUST向Foot Bridge交付一份同lineage的`CharacterFootIkCommittedCaptureViewLease`
- **AND** Generated Program MUST在该租约内一次提取全部已选Sampler字段而不得重新执行查询、求解或物理读取

#### Scenario: 表现帧被丢弃或Fault

- **WHEN** 当前表现帧在Seal前被Discard或在Barrier后Fault
- **THEN** PoseGraph MUST不发布该帧的Foot IK Capture View租约
- **AND** Sampler MUST不借用上一帧、Pending页或当前Transform补成一条记录

### Requirement: Attribute Schema必须成为采样字段的唯一声明

每个Foot可采样字段 MUST通过`generated-diagnostic-sampling-framework`提供的唯一Attribute在AOT-safe诊断Extractor上声明稳定字段identity、数据类型、单位、availability关系、表归属与一个或多个可组合字段分组。每个Foot Sampler Definition MUST只选择字段分组与自己新增的专项派生字段，不得要求已有字段反向登记新Sampler identity。Extractor MUST是只读`CharacterFootIkCommittedCaptureView`的普通静态函数，不得引用`UnityEditor`、`object`动态调用、运行时成员路径、World Query、Vendor对象或场景Transform。框架唯一Schema Compiler MUST在C#编译期发现全部Foot声明、校验Schema闭包并生成不可变Schema descriptor与静态Capture程序；Foot插件 MUST不实现第二Compiler、Attribute、codec或identity算法。Schema字段、顺序、Program Definition或派生规则变化 MUST生成新的稳定Schema identity与Generated Program hash。

#### Scenario: 多个Sampler复用同一字段

- **WHEN** Full、Solver与Landing三个Sampler同时选择包含同一正式字段的分组
- **THEN** 框架Schema Compiler MUST为三个Foot Sampler复用同一字段声明并在Union函数中只生成一次字段求值
- **AND** 系统 MUST不要求为每个Sampler重复实现getter、setter、单位或availability

#### Scenario: Attribute声明不完整

- **WHEN** 任一字段缺少稳定identity、类型、单位、表归属或引用了未知Sampler
- **THEN** Schema编译或Capture Program preflight MUST在生成Player或订阅Runtime interest前拒绝全部请求
- **AND** MUST不开始部分Sampler或运行到首帧后再跳过该字段

#### Scenario: Capture Request引用过期Schema

- **WHEN** Attribute、Extractor、Sampler Set或代码更新产生不同Schema identity或Generated Program hash
- **THEN** Editor Capture、Player Build或Player握手 MUST在开始采样前拒绝过期Request
- **AND** MUST不回退反射、解释执行或在同一Capture中混合两个Schema

### Requirement: Capture执行必须由编译期生成的AOT静态程序完成

Foot插件 MUST声明一个或多个稳定Capture Program Definition，每个Definition显式组合一套或多套Foot Sampler。框架唯一Schema Compiler MUST按该Sampler Set求字段并集，并生成使用Foot具体Committed View与框架packet layout的普通C#静态Capture Program；同一字段每只脚每帧 MUST只求值一次。Unity C# Compiler与IL2CPP MUST把该普通函数纳入Capture Player AOT闭包；Editor Play Mode MUST执行同一Generated Program identity。Foot插件 MUST不创建Foot专属Source Generator、表达式委托、每列动态Delegate、运行时反射或第二手写提取路径。

#### Scenario: 构建IL2CPP Capture Player

- **WHEN** `DiagnosticCapabilitySet`选择`character-foot-ik` Capture并引用合法Foot Program Definition与Schema identity
- **THEN** Player闭包 MUST包含匹配Generated Program hash的静态Capture函数及其全部Extractor
- **AND** Player运行时 MUST不调用`Expression.Compile`、`MethodInfo.Invoke`、`DynamicInvoke`或解释器生成访问逻辑

#### Scenario: Editor执行相同Sampler Set

- **WHEN** Editor Capture选择与Capture Player相同的Sampler Set、Schema和Generator revision
- **THEN** Editor MUST执行相同Generated Program identity和dense packet layout
- **AND** MUST不使用另一套表达式或反射实现产生看似相同的CSV

### Requirement: 多套Sampler必须可同时组合且共享唯一上游采样

每个Foot Sampler Definition MUST声明稳定Sampler identity、revision、字段集合、派生事实、表集合、Analyzer与发布物。作者 MUST通过Foot Capture Program Definition显式组合一套或多套Sampler；该Program MUST在Editor Capture编译或Player Build前冻结。全部选中Sampler MUST通过一个Generated Program消费同一`CharacterFootIkCommittedCaptureViewLease`并各自发布带Schema identity的产物。新增Sampler Definition MUST不要求修改既有Sampler Implementation、通用框架或Foot/FBBIK Runtime。多个Sampler同时启用 MUST不增加Foot查询次数、FBBIK执行次数、Final Publication次数或上游View数量。

#### Scenario: 同时运行Full与Solver Sampler

- **WHEN** 作者在一次Capture中同时选择Full Foot和FBBIK Solver两个Sampler
- **THEN** 两者 MUST收到相同Frame与Completion序列并分别发布自己的Schema和产物
- **AND** PoseGraph MUST仍只冻结并发布一次Foot IK具体View租约

#### Scenario: 插入新的专项Sampler

- **WHEN** 编译输入中出现一个Schema合法且identity唯一的新Sampler Definition
- **THEN** 作者 MUST能在下一份Capture Program Request中选择它并与现有Sampler组合后重新生成程序
- **AND** 通用Schema Compiler与既有Foot Sampler MUST不增加针对该Sampler identity的手写条件分支

#### Scenario: 未选择任何Sampler

- **WHEN** 当前没有显式选择Foot IK Sampler
- **THEN** 系统 MUST不注册Foot IK capture interest、不创建Capture Session或发布空产物
- **AND** Runtime Foot、FBBIK与Final Publication结果 MUST保持不变

### Requirement: Sampler必须保持只读并隔离派生事实

Sampler Definition的Extractor MUST只读取PoseGraph-owned具体Committed Capture View和当前Capture固定metadata，Host Adapter MUST只读取sealed packet或由同一Schema生成的typed row。它们 MUST不访问Foot持久状态、FBBIK Vendor对象、Physical Transform、World Query、Gameplay State或可写Runtime Target，也 MUST不向Runtime回传Decision、目标、权重或配置。派生事实 MUST归属具体Sampler Schema，不得伪装成Runtime正式字段或被下一帧消费。

#### Scenario: Sampler计算穿透诊断

- **WHEN** 专项Sampler从同一Committed Frame计算Contact平面穿透与事件级统计
- **THEN** 计算结果 MUST只进入该Sampler产物并携带来源字段identity
- **AND** Foot State、Goal、FBBIK与Physical Pose MUST不读取该派生结果

### Requirement: 多Sampler Capture必须有界、非阻塞且原子发布

一次Foot Capture MUST通过独立通用框架Capability Session冻结Program Definition、Sampler Set、Schema、Generated Program、packet layout/capacity、Writer和Foot Host输出闭包。Foot领域Bridge MUST只在具体Committed View租约内取得框架packet lease、调用一次生成函数并提交，不得等待文件写入或离线分析。框架Writer MUST只封存版本化typed packet流与Capability manifest；Foot Host Adapter MUST从框架验证后的sealed packet view生成各Sampler CSV、运行Analyzer和Publisher。任一Foot Sampler失败或框架发布Overflow、Sequence、Writer、Host或hash故障时，Foot Capability MUST为Faulted且不得生成部分Completed身份；Performance工作流 MUST再依据该Capability结果决定顶层Capture状态。

#### Scenario: 一个Sampler后台写入失败

- **WHEN** Full与Solver同时运行且Solver Writer发生IO失败
- **THEN** Foot Capability manifest MUST标记Faulted并记录失败Sampler、阶段与已有文件，Performance顶层Capture MUST据此Faulted
- **AND** Full Sampler已写内容 MUST不被伪装成完整多Sampler Capture

#### Scenario: 所有Sampler正常完成

- **WHEN** 全部选中Sampler完成相同Frame范围的写入、分析与hash闭包
- **THEN** Foot Capability manifest MUST引用每个Sampler manifest、Schema identity、Frame范围与产物
- **AND** 消费者 MUST只通过manifest精确路径打开对应产物

### Requirement: 内建Foot全量Sampler必须保留正式业务语义

系统 MUST提供一个内建Full Foot Sampler，覆盖现行主Foot行、Ground Geometry、离线Analyzer、诊断Publisher、明细存储与评分结果。迁移 MUST保持现行字段的业务含义、单位、availability、事件分母、诊断规则和评分数学，但新产物 MUST使用新的Schema与manifest identity。既有封存采样包 MUST保持不可变且不得迁移、覆盖或通过兼容reader解释为新Schema。

#### Scenario: 新Full Sampler生成诊断

- **WHEN** 相同正式输入在迁移后由Full Foot Sampler完成Capture
- **THEN** 新产物 MUST继续表达相同Foot、Pelvis、Goal、Solved、Physical、事件与评分业务含义
- **AND** 实现 MUST不保留旧手写Column链或旧CSV reader作为第二Schema真相

### Requirement: Performance Player必须显式区分Disabled与Capture能力身份

唯一Performance Player Build Request MUST通过稳定排序`DiagnosticCapabilitySet`显式声明`character-foot-ik`为`Disabled`或`Capture`。对应descriptor MUST保存Mode、Sampler Set、Schema、Program、packet capacity与transport identity。Disabled构建用于纯性能基线，MUST不编译或装配Foot IK Definitions、Bridge、Generated Program、capture page、队列或interest；Capture构建用于IL2CPP实机采样，MUST引用合法Foot Program Definition。Foot change MUST不新增专属Build字段、manifest codec、握手或Comparer算法；Performance工作流 MUST拒绝完整Capability identity不同的性能数值比较。

#### Scenario: 构建纯性能基线Player

- **WHEN** 作者以Foot IK diagnostics Disabled执行Performance Player Build
- **THEN** Player manifest MUST声明Foot IK diagnostic sampling为Disabled且构建闭包不包含该能力
- **AND** Foot、FBBIK与Final Publication热路径 MUST不为该能力创建页、分支或interest

#### Scenario: 构建IL2CPP实机采样Player

- **WHEN** 作者以`character-foot-ik` Capture和合法Foot Program Definition执行同一Performance Player Build入口
- **THEN** Player manifest MUST锁定Sampler Set、Schema identity、Generated Program hash、Generator revision、packet layout/capacity和Writer transport
- **AND** Player闭包 MUST包含匹配的静态Capture程序、capture页与有界packet队列且不得包含运行时表达式编译器

#### Scenario: 比较两个能力身份不同的Capture

- **WHEN** Baseline与Candidate的Foot IK diagnostic sampling capability、Sampler Set、Schema、Generated Program、容量或transport identity不同
- **THEN** Comparer MUST列出身份冲突并拒绝数值比较
- **AND** MUST不把差异归因于Gameplay或IK性能变化

### Requirement: Foot IK采样不得扫描目标进程私有内存

本能力 MUST不通过外部进程读取Unity私有虚拟地址、解析未版本化对象布局、暂停Player、扫描托管堆或按PDB地址重建Foot/FBBIK状态。Foot Host Adapter MUST只消费通用框架验证后的sealed packet view、Schema与manifest。若后续需要跨机器实时采集，MUST由通用框架的独立change定义传输和Collector合同；Foot插件不得增加私有Shared Memory、socket或地址扫描fallback。

#### Scenario: 外部工具请求读取Player私有地址

- **WHEN** Foot IK Capture没有正式共享页合同而外部工具尝试按地址读取Player状态
- **THEN** 工作流 MUST拒绝该Collector进入Completed Capture
- **AND** MUST不把地址扫描结果与正式Committed Frame合并

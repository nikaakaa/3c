## Why

当前Foot IK采样把Foot提交事件、Animation Snapshot、Pose Watch、场景Transform和手写CSV Schema再次拼成一份诊断事实：同一帧需要二次对齐，新增字段还要同步Runtime Diagnostics、Sampler包装、列getter/setter和Analyzer Record。现有interest关闭只跳过逐帧填充，不能证明纯性能基线Player没有Foot IK诊断页、常驻缓冲或热路径分支；同时现有单体万能Schema既无法让多套独立诊断按需组合，也不能成为通用AOT诊断采样框架的领域插件。

## What Changes

- 由`refactor-character-pose-graph-architecture`唯一新增并拥有具体`CharacterFootIkCommittedCaptureViewLease`：PoseGraph在根表现帧开始冻结typed interest，只在成功Seal后从同lineage的Foot、Constraint/FBBIK与Final Publication typed Result组合正式输入、目标、Solved与Physical事实，并控制租约失效；本change不定义第二View或第二事实页，直接删除Foot事件与Animation Snapshot/Pose Watch二次对帧、场景Transform反推和PendingFrame等待链。
- 作为`add-generated-diagnostic-sampling-framework`的首个领域插件，新增Foot IK Capability Definition、消费上述具体View的Foot Bridge、AOT-safe Foot字段／表Extractor、Full／Solver／Landing等Sampler Definition、Capture Program Definition与Host Adapter。Foot插件只声明领域内容；通用Attribute、Schema Compiler、Source Generator、Generated Program ABI、typed packet、Capability Session、Writer、Host Reader／Finalizer和manifest生命周期由框架唯一拥有。
- 一次Foot Capture Program Definition MAY显式组合一套或多套Sampler；通用编译器按该Sampler Set生成唯一具体AOT Capture Program，同一Foot字段每只脚每帧只求值一次。全部Sampler MUST共享唯一上游冻结页，不得各自请求Foot查询、FBBIK、Pose Watch或Physical读取；Foot插件不得建立第二Generator、packet、Session或Host Orchestrator。
- Foot领域Bridge只在根表现帧成功Seal后的短租约View内取得框架packet lease、调用匹配Generated Program并提交；Player通用Writer封存版本化packet流，Editor或现有Performance Controller中的Foot Host Adapter再生成各Sampler CSV、运行Analyzer并发布结果。任一显式请求的Foot Sampler失败、框架队列溢出或Schema不一致时，整体Capture不得发布Completed；其它已产生证据保留为Faulted staging。
- 把现有Foot全量CSV、Ground Geometry、Analyzer、Publisher、明细和评分迁移为内建Sampler Adapter，保持现行字段业务含义与评分数学；手写Column Source/Record双映射、手写Header、旧DebugRegistry和旧二次join直接删除，不保留兼容reader。
- **BREAKING** 新Capture使用新的Schema与manifest identity；既有封存采样包继续作为不可变历史证据，不迁移、不覆盖，也不由新Reader兼容读取。
- 通过稳定排序`DiagnosticCapabilitySet`扩展唯一Performance Player构建入口，为`character-foot-ik`显式选择`Disabled`纯性能基线或`Capture`实机采样身份。Foot插件只提供Capability／Program descriptor和领域Bridge；Build Request、编译闭包、Player／Run／Capture manifest、握手与Comparer由Performance工作流消费通用框架合同，不建立Foot专属Build字段或第二构建解释。
- 外部进程扫描Unity私有虚拟地址、按PDB/ASLR猜测对象布局、暂停进程取样和运行时IL2CPP表达式编译不进入本次范围。Player只通过正式Capture Program和版本化packet输出事实；未来若需要跨机器Collector，必须以新的显式传输change消费该packet合同，不得扫描私有内存或作为当前Writer失败时的fallback。

## Capabilities

### New Capabilities

- `character-foot-ik-diagnostic-sampling`: 定义Foot IK作为通用Generated Diagnostic Sampling Framework首个领域插件时，如何消费PoseGraph-owned具体Committed Capture View，并拥有字段／表、Sampler Program、Host Adapter、Analyzer、Publisher与评分迁移合同。

### Modified Capabilities

- `character-foot-placement-presentation`: 将Foot、FBBIK与Physical诊断收敛为同一成功Seal后的唯一capture事实来源，并禁止旧事件/Snapshot/Pose Watch/Transform拼接链。

## Impact

- Affected runtime: Foot Placement committed diagnostics、CharacterPoseConstraint Result、FBBIK transient diagnostic capture、Final Publication Result、Foot diagnostics interest、capture page、领域Capture Bridge与框架Generated Program装配。
- Affected editor/tooling: `CharacterFootLandingPredictionSampler`、Foot CSV bindings/column groups、Ground Geometry writer、Foot字段／Sampler／Program Definitions、Foot Host Adapter、Analyzer输入、Publisher与评分接线。
- Affected framework: `add-generated-diagnostic-sampling-framework`唯一提供Attribute、Schema Compiler、Source Generator、Generated Program ABI、typed packet、Session、Writer、Host Reader／Finalizer、manifest和Diagnostic Capability Set合同。
- Affected build: 唯一Performance Player Build Request通过通用Diagnostic Capability Set承载`character-foot-ik` Disabled/Capture身份，不新增Foot专属构建入口或identity算法。
- Affected pending spec: `add-gameplay-performance-capture-workflow`尚未归档，`gameplay-performance-capture-workflow`还不是current capability；本change已经同步其proposal/design/spec/tasks，以同一Performance Build Request、BuildIdentity、Player/Run/Capture manifest、握手与Comparer承载Foot IK Disabled/Capture身份，不能创建第二BuildIdentity解释。
- Affected active changes: 本change是`refactor-character-pose-graph-architecture`任务13的正式下游领域切片，并依赖`add-generated-diagnostic-sampling-framework`的通用基础设施。PoseGraph唯一拥有`Source / Program / Constraint / Final Publication Committed Result -> CharacterPoseDiagnosticsProjector -> CharacterFootIkCommittedCaptureViewLease`及其interest冻结和租约寿命；框架拥有通用生成、packet、Capability Session、Writer／Reader与manifest生命周期；本change只拥有租约之后的Foot Capability Bridge、字段／Sampler／Program Definitions与Host业务插件。PoseGraph与框架接口可分别闭合，Foot插件只能在两者完成后串行接入，禁止在旧Sampler上建立临时Adapter。`stabilize-character-foot-path-and-landing`继续拥有Foot行为与诊断字段语义，本change不得修改Foot、Pelvis、Goal或FBBIK数学。
- Existing completed changes: `refactor-character-ik-maintenance-boundaries`、`compact-foot-diagnostic-publication`与`consolidate-foot-diagnostic-scoring`已形成的正式字段含义、离线事实与评分规则继续保留，但其手写采样存储实现不再作为必须保留的架构。

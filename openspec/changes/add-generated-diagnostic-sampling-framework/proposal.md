## Why

`refactor-foot-ik-diagnostic-sampling`已经需要Attribute字段声明、编译期AOT函数生成、typed packet、多Sampler组合、Host Reader和构建身份；如果这些能力继续以Foot IK命名和实现，后续Camera、Animation、Simulation或AI诊断会复制同一套编译器、packet与生命周期。现在实现尚未开始，应先把稳定基础设施提取为项目级通用框架，让Foot IK成为首个完整领域插件，而不是未来再做第二次迁移。

## What Changes

- 新增项目级Generated Diagnostic Sampling Framework。框架只定义Capability、Field／Group／Table／Sampler Attribute、Capture Program Request、Schema descriptor、Generated Capture Program ABI、codec、typed packet、Capability Session、Writer／Reader、Host Finalizer与manifest合同，不定义通用Committed View，也不理解Foot、PoseGraph、Presentation Frame、Simulation Tick、Camera、FBBIK、评分或具体CSV字段。
- 新增唯一.NET Standard 2.0 Source Generator与编译期Schema Compiler。它从编译符号发现领域Capability、三个typed生命周期Event、字段、样本维度和Sampler声明，按Capability与Sampler Set校验闭包并生成普通C#具体事件处理器与Capture Program；Editor与IL2CPP Player执行同一Generated Program，运行时不得编译表达式、反射成员、使用`object`动态调用或解析字符串路径。
- 每个领域正式Runtime Owner必须提供自己的具体只读Committed Capture View并拥有其lineage／availability与租约语义；领域手写采样代码只定义并发布`CaptureStarted`、`CommittedSample`、`CaptureStopped`三个typed事件，以及具体Capture Metadata、字段Extractor和Sampler Definition。普通Sampler不声明Bridge或Host Adapter。生成类型按Capability identity隔离；框架不提供共同View／Metadata DTO，不把Capture Metadata塞回Committed View，也不使用`object`、运行时未知泛型、万能DTO或中央领域`switch`。
- 新增固定类型族的dense packet与固定容量子表协议。Generated `CaptureStarted`处理器创建Session并冻结Program／Schema／容量／interest；Generated `CommittedSample`处理器在短View租约内按声明的枚举／稳定ID维度自动租packet、调用Extractor、写入并提交；Generated `CaptureStopped`处理器按Completed／Cancelled／Faulted结果封存packet、artifact和manifest。Host依据同一Sampler Schema自动生成主表、固定子表、RFC 4180 CSV、逐Sampler manifest和无复制typed view；领域Analyzer／Publisher只读取生成产物，不参与采样适配、字段映射或生命周期。
- 新增通用诊断Capability构建身份合同。每个Player Build Request显式声明Capability为`Disabled`或`Capture`；Capture锁定Sampler Set、Schema identity、Generated Program hash、Generator binary identity、程序集binding、cadence、packet layout/capacity与transport，Disabled通过`DiagnosticCompilationClosureProof`在编译期排除对应插件和生成程序。Player manifest、Run Request、握手、Runtime／Capability manifest与Comparer共享同一Capability Set codec；框架不创建第二Player、Controller、顶层Capture或Comparer。
- `refactor-foot-ik-diagnostic-sampling`改为首个领域插件：只定义Foot三个生命周期Event、具体Capture Metadata、字段／表／Sampler声明以及下游Full Analyzer／Publisher和评分迁移，不再拥有Foot Bridge、手写Column／CsvBinding、每Sampler Host Adapter、第二Source Generator、通用packet、Session、Writer、manifest或Host编排框架。
- 不建立通用对象序列化器、运行时脚本／表达式引擎、远程遥测、跨机器实时Collector、数据库、通用查询语言或线上配置系统；这些能力不能作为当前框架失败时的fallback。

## Capabilities

### New Capabilities

- `generated-diagnostic-sampling-framework`: 定义跨领域的编译期Schema、AOT Generated Capture Program、typed packet、多Sampler组合、Session／Writer／Host Finalizer、manifest和构建capability合同。

### Modified Capabilities

## Impact

- Affected runtime/tooling: 通用诊断contracts、AOT-safe Attribute定义、Source Generator、Generated Program ABI、typed packet／固定容量子表、Session、Writer、Host Reader与manifest identity。
- Affected active change: `refactor-foot-ik-diagnostic-sampling`从通用基础设施Owner收窄为Foot IK领域插件和首个纵向验证；不改变Foot、Pelvis、Goal、FBBIK或评分业务语义。
- Affected active change: `add-gameplay-performance-capture-workflow`新增通用`DiagnosticCapabilitySet`接入并明确禁止Foot专属构建字段，继续唯一拥有Build、Player、Controller、Gate、顶层Capture与Comparer。
- Affected active change: `refactor-character-pose-graph-architecture`唯一生产并控制具体`CharacterFootIkCommittedCaptureViewLease`；它不定义通用View，也不引用框架Source Generator、Generated Program、packet或Performance Build身份。
- Affected dependencies: Unity 2022.3支持的Roslyn API版本、.NET Standard 2.0生成器编译边界和Player专属编译输入必须进入Generator／Build identity。
- Current specs: `btsmtl-runtime-diagnostics`的只读、source identity和不反向驱动运行结果要求保持；本change新增独立采样基础设施，不把性能样本或领域packet写入RuntimeDebugSession Trace Store。

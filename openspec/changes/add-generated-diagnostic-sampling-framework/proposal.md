## Why

`refactor-foot-ik-diagnostic-sampling`已经需要Attribute字段声明、编译期AOT函数生成、typed packet、多Sampler组合、Host Reader和构建身份；如果这些能力继续以Foot IK命名和实现，后续Camera、Animation、Simulation或AI诊断会复制同一套编译器、packet与生命周期。现在实现尚未开始，应先把稳定基础设施提取为项目级通用框架，让Foot IK成为首个完整领域插件，而不是未来再做第二次迁移。

## What Changes

- 新增项目级Generated Diagnostic Sampling Framework。框架只定义Capability、Field／Group／Table／Sampler Attribute、Capture Program Request、Schema descriptor、Generated Capture Program ABI、codec、typed packet、Capability Session、Writer／Reader、Host Finalizer与manifest合同，不定义通用Committed View，也不理解Foot、PoseGraph、Presentation Frame、Simulation Tick、Camera、FBBIK、评分或具体CSV字段。
- 新增唯一.NET Standard 2.0 Source Generator与编译期Schema Compiler。它从编译符号发现领域插件声明，按Capability与Sampler Set校验字段闭包并生成普通C#具体静态Capture Program；Editor与IL2CPP Player执行同一Generated Program，运行时不得编译表达式、反射成员、使用`object`动态调用或解析字符串路径。
- 每个领域正式Runtime Owner必须提供自己的具体只读Committed Capture View并拥有其lineage／availability与租约语义；领域插件只声明该具体输入类型、AOT-safe Extractor、Sampler Definition和Host Adapter。生成类型按Capability identity隔离；框架不提供共同View接口、基类或DTO，不使用`object`、运行时未知泛型、万能DTO或中央领域`switch`。
- 新增固定类型族的dense packet与固定容量子表协议。主线程只在View租约内调用一次生成程序并提交预分配packet；后台Writer只封存版本化packet流，Host依据同一Schema descriptor生成Sampler列视图、CSV／其它格式、Analyzer与Publisher输入。
- 新增通用诊断Capability构建身份合同。每个Player Build Request显式声明Capability为`Disabled`或`Capture`；Capture锁定Sampler Set、Schema identity、Generated Program hash、Generator revision、packet layout/capacity与transport，Disabled在编译期排除对应插件和生成程序。框架只提供identity与闭包合同，具体Player、Controller、Gate和Comparer继续由现有Performance工作流拥有。
- `refactor-foot-ik-diagnostic-sampling`改为首个领域插件：只消费PoseGraph-owned具体`CharacterFootIkCommittedCaptureViewLease`，并拥有Foot Bridge、字段／表／Sampler声明、Full Host Adapter、Analyzer、Publisher和评分迁移，不再拥有第二Source Generator、通用packet、Session、Writer、manifest或Host编排框架。
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

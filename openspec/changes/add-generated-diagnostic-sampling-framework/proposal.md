## Why

通用采样核心已经具备AOT Capture、typed packet和Schema-driven Host，但旧单View合同仍迫使消费方把Landing、Motion、Goal、Solved、Pelvis等现有事实复制进诊断DTO。独立仓库0.4.0已经确立multi Fact Root合同，3C必须以该合同为唯一真相，删除单View、生命周期Event DTO和逐字段映射。

## What Changes

- **BREAKING**：`DiagnosticCapability`只声明Capability identity、revision和Metadata类型；一个Dimension需要的每个现有事实类型分别通过`DiagnosticFactRoot(rootId, type)`注册，不再声明Dimension View或通用View。
- **BREAKING**：`DiagnosticCaptureProgram`直接声明稳定Dimension ID与Sampler集合；Generator生成`DiagnosticLifecycle.Start`、多Fact Root `HandleCommitted`和`Stop`，领域不再定义`CaptureStarted`、`CommittedSample`、`CaptureStopped`三个Event DTO。
- 一个Dimension可以同时接收多个现有强类型Fact Root和一个Metadata。Commit点按生成签名把左右或其它Dimension的事实直接以`in`传入，不构造诊断DTO、不复制字段、不执行Side选择。
- 普通字段只在现有readonly field或调用方可读不可写property上声明一个`DiagnosticField`；现有业务计算getter同样允许直接标记。Generator从Fact Root与成员路径生成默认Field identity、推断codec并展开直接访问。
- 同一业务类型可以作为多个Fact Root或经多条成员路径复用，叶子Attribute只声明一次；Schema通过Fact Root ID和成员路径区分位置。`DiagnosticDerivedField`只保留真正公式，方法只接收实际读取的Fact Root，参数名映射Root ID，最后接收`in Metadata`。
- 固定一对多事实可在现有readonly struct或只向调用方暴露`Count`与只读索引器的class page上声明`DiagnosticTable`；行内字段继续使用真实成员`DiagnosticField`，不创建表DTO。
- Generated lifecycle自动完成Session创建、packet租用、每Dimension Capture、提交与封存。Host只依据sealed packet和统一Schema自动生成主表、子表、RFC 4180 CSV、Sampler manifest及Capability manifest；不存在Bridge、Column／CsvBinding或Host Adapter。
- `KK_DIAGNOSTIC_SAMPLING`缺失时Attribute不写入业务metadata、Generator零输出、Capture Runtime与领域Diagnostics程序集不进入Player。Cecil与IL2CPP Gate必须证明Annotations不作为Player根程序集残留，并证明零Sampling AssemblyRef、零Generated Program／Session／packet／queue／interest和零Capability／Field identity。
- `refactor-foot-ik-diagnostic-sampling`只负责把现有Foot事实注册为多个Fact Root、声明字段／表／Sampler／Program、在同步Commit点调用生成入口，并在基础产物完成后运行领域Analyzer／Publisher；PoseGraph不为采样增加View、DTO、Event或运行分支。
- 不建立通用对象序列化器、运行时表达式／脚本引擎、反射fallback、动态字段字典、远程遥测或旧0.3兼容链。

## Capabilities

### New Capabilities

- `generated-diagnostic-sampling-framework`: 定义跨领域的multi Fact Root编译期Schema、AOT直接成员访问、generated lifecycle、typed packet、Session／Writer／Reader、Schema-driven CSV／manifest和Disabled零闭包。

### Modified Capabilities

## Impact

- Affected dependency: 3C统一消费独立`com.kk.generated-diagnostic-sampling` 0.4.0及其唯一Analyzer identity，不再维护项目内第二份Generator或旧单View ABI。
- Affected runtime/tooling: Annotations、Source Generator、Generated Program ABI、multi Fact Root Schema、typed packet、固定容量Table、Runtime Session、Writer、Host Reader／Finalizer和构建闭包Gate。
- Affected active change: `refactor-foot-ik-diagnostic-sampling`迁移为0.4.0消费方，删除诊断View、三个Event DTO、普通Getter／Extractor、Bridge、Column／CsvBinding和Host Adapter。
- Affected active change: `add-gameplay-performance-capture-workflow`继续唯一拥有Build、Player、Controller、顶层Capture、Gate与Comparer，只消费通用`DiagnosticCapabilitySet`。
- Affected active change: `refactor-character-pose-graph-architecture`继续独立维护正式Committed Result、事务、Seal和既有Post-Commit边界；采样只在现有事实成员上保留条件Attribute并由外部Commit调用生成入口。
- Affected build targets: Unity 2022.3 Editor、Windows x64 IL2CPP Capture Player与不含采样闭包的Disabled Player。

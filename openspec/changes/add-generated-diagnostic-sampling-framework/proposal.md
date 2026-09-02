## Why

通用采样核心已经具备AOT Capture、typed packet和Schema-driven Host，但旧单View合同仍迫使消费方把Landing、Motion、Goal、Solved、Pelvis等现有事实复制进诊断DTO。独立仓库0.5.1已经确立multi Fact Root、业务`DiagnosticEvent`和可选partial interest合同，3C必须以该合同为唯一真相，删除单View、生命周期Event DTO、公开`HandleCommitted`和逐字段映射。

## What Changes

- **BREAKING**：`DiagnosticCapability`只声明Capability identity、revision和Metadata类型；一个Dimension需要的每个现有事实类型分别通过`DiagnosticFactRoot(rootId, type)`注册，不再声明Dimension View或通用View。
- **BREAKING**：`DiagnosticCaptureProgram`直接声明稳定Dimension ID与Sampler集合；业务在自选Post-Commit边界声明一个带`DiagnosticEvent`的private static partial void方法，Generator按Event identity与完整参数签名生成typed dispatcher和Program handler。Host workflow仍拥有Session Start／Stop，领域不再定义`CaptureStarted`、`CommittedSample`、`CaptureStopped`三个Event DTO。
- 一个Dimension可以同时接收多个现有强类型Fact Root和一个Session Metadata。Post-Commit只调用一行partial Event并把目标、真实lineage及左右或其它Dimension的既有事实直接以`in`传入；Metadata由Lifecycle Start冻结，不随Event逐帧传递。无匹配目标订阅立即返回，Disabled编译消除Event及可选interest Query调用与参数求值。
- 普通字段只在现有readonly field或调用方可读不可写property上声明一个`DiagnosticField`；现有业务计算getter同样允许直接标记。Generator从Fact Root与成员路径生成默认Field identity、推断codec并展开直接访问。
- 同一业务类型可以作为多个Fact Root或经多条成员路径复用，叶子Attribute只声明一次；Schema通过Fact Root ID和成员路径区分位置。`DiagnosticDerivedField`只保留真正公式，方法只接收实际读取的Fact Root，参数名映射Root ID，最后接收`in Metadata`。
- 固定一对多事实可在现有readonly struct或只向调用方暴露`Count`与只读索引器的class page上声明`DiagnosticTable`；行内字段继续使用真实成员`DiagnosticField`，不创建表DTO。
- Generated lifecycle自动完成Session创建、packet租用、每Dimension Capture、提交与封存。Host只依据sealed packet和统一Schema自动生成主表、子表、RFC 4180 CSV、Sampler manifest及Capability manifest；不存在Bridge、Column／CsvBinding或Host Adapter。
- `KK_DIAGNOSTIC_SAMPLING`缺失时Attribute不写入业务metadata、Generator零输出、Capture Runtime与领域Diagnostics程序集不进入Player。Cecil与IL2CPP Gate必须证明Annotations不作为Player根程序集残留，并证明零Sampling AssemblyRef、零Generated Program／Session／packet／queue／interest和零Capability／Field identity。
- `refactor-foot-ik-diagnostic-sampling`负责把现有Foot事实注册为多个Fact Root、声明字段／表／Sampler／Program，并保留Post-Commit业务事实边界；`add-schema-driven-diagnostic-analysis`把该边界收敛为一行Foot `DiagnosticEvent` partial调用和generated typed handler，同时只消费Completed基础产物恢复Foot规则Operator、Plan与报告。旧单体Foot Analyzer／Publisher与评分报告已从采样链删除。PoseGraph不为采样增加View、DTO、Event payload或运行分支。
- 不建立通用对象序列化器、运行时表达式／脚本引擎、反射fallback、动态字段字典、远程遥测或旧0.3兼容链。

## Capabilities

### New Capabilities

- `generated-diagnostic-sampling-framework`: 定义跨领域的multi Fact Root编译期Schema、AOT直接成员访问、generated lifecycle、typed packet、Session／Writer／Reader、Schema-driven CSV／manifest和Disabled零闭包。

### Modified Capabilities

## Impact

- Affected dependency: 3C统一消费独立`com.kk.generated-diagnostic-sampling` 0.5.1及其唯一Analyzer identity，不再维护项目内第二份Generator、0.5.0 interest入口或旧单View ABI。
- Affected runtime/tooling: Annotations、Source Generator、Generated Program ABI、multi Fact Root Schema、typed packet、固定容量Table、Runtime Session、Writer、Host Reader／Finalizer和构建闭包Gate。
- Affected active change: `refactor-foot-ik-diagnostic-sampling`迁移为0.5.1消费方，删除诊断View、三个Event DTO、普通Getter／Extractor、Bridge、Column／CsvBinding和Host Adapter。
- Affected active change: `add-schema-driven-diagnostic-analysis`在同一通用package内增加Host-only Dataset／Plan／Operator／Report基础设施，并在3C领域Editor程序集恢复Foot离线诊断；本change不拥有具体领域规则。
- Affected active change: `add-gameplay-performance-capture-workflow`继续唯一拥有Build、Player、Controller、顶层Capture、Gate与Comparer，只消费通用`DiagnosticCapabilitySet`。
- Affected active change: `refactor-character-pose-graph-architecture`继续独立维护正式Committed Result、事务、Seal和既有Post-Commit边界；业务只在该边界调用一行条件`DiagnosticEvent` partial方法，不反向引用Sampling Runtime或领域诊断程序集。
- Affected build targets: Unity 2022.3 Editor、Windows x64 IL2CPP Capture Player与不含采样闭包的Disabled Player。

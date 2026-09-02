# 实施证据

## 实施范围

本change只实现通用Generated Diagnostic Sampling Framework，不实现PoseGraph、Foot IK业务字段全集、Performance Player／Controller或顶层Comparer。正式领域输入保持两条独立具体类型：Runtime Owner发布的Committed View与领域插件定义的Capture Metadata；框架不定义共同View／Metadata DTO。

## 小步提交

- `82f90ac32 建立通用AOT诊断采样框架`：建立Unity local package、Contracts、Runtime Session、typed packet、Writer、Host Reader／Finalizer、Roslyn Generator和portable Probe。
- `eb26c8fa5 补全诊断字段依赖闭包`：增加派生依赖闭包、未知依赖与依赖环编译诊断。
- `e98d86425 严格校验诊断密封包`：在同一只读句柄核对文件大小、SHA-256、完整layout／capacity、lineage和sequence。
- `028d68fd5 生成无复制诊断采样视图`：生成canonical Schema handle，Sampler只引用同一handle与packet，Host只接收本Sampler无复制视图。
- `d633e83c1 闭合诊断能力清单身份`：增加Schema、Capability Set、Runtime manifest、最终Capability manifest codec与原子artifact闭包。
- `41711cbf9 补齐双输入采样与会话租约`：增加具体Capture Metadata双输入ABI、cadence identity、versioned struct packet lease、非阻塞Finalize与Player编译闭包证明。

## 编译与Unity证据

- `ThirdPerson.GeneratedDiagnosticSampling.csproj` Release：0 warning、0 error。
- `ThirdPerson.GeneratedDiagnosticSampling.Generator.csproj` Release：0 warning、0 error。
- `ThirdPerson.GeneratedDiagnosticSampling.Host.csproj` Release：0 warning、0 error。
- `ThirdPerson.GeneratedDiagnosticSampling.Probe.csproj` Release：0 warning、0 error。
- 所有dotnet build均使用`--disable-build-servers /nr:false /p:UseSharedCompilation=false`，每轮结束立即执行`dotnet build-server shutdown`。
- Probe生成源码签名为`Capture(in ProbeCommittedView, in ProbeCaptureMetadata, ref DiagnosticCapturePacket)`；主字段、Table Count与Table Field均直接调用双输入静态Extractor，View与Metadata为两个不同具体类型。
- package Analyzer与最终Generator Release DLL的SHA-256均为`60B7970E709F48F23FF7FA85E88709F21A124EC0262F8D2DAFE1A7CC15AA1656`；重复直接构建的Generator MVID稳定，最终package不再携带旧二进制。
- 3C Unity 2022.3 force refresh已实际加载package与Analyzer；Foot插件以第4个具体Metadata type及首批双输入Field Extractor通过生成器构建，未出现Generated Sampling、Analyzer或package编译错误。

## 边界搜索

- 通用package中`Foot`、`FBBIK`、`PoseGraph`、`Camera`、`AI`领域分支为零。
- 通用package中不存在共同Committed View／Capture Metadata接口、基类或DTO。
- Runtime与Host路径中不存在Expression编译、Reflection member getter、`DynamicInvoke`、`MethodInfo.Invoke`或`object[]`值页。
- Host只读取sealed Schema／packet／manifest和Sampler handle view，不持有领域View／Metadata，不查询World或Player私有地址。
- Framework没有创建Performance顶层Player、Controller、Capture根或Comparer。

## 校验结果

- `add-generated-diagnostic-sampling-framework --strict`：通过。
- `refactor-foot-ik-diagnostic-sampling --strict`：通过。
- `add-gameplay-performance-capture-workflow --strict`：通过。
- `refactor-character-pose-graph-architecture --strict`：通过。
- 全量OpenSpec：96项通过、7项既有失败；失败属于其它change／current spec，因此任务7.5保持未完成，也不提前修改`openspec/project.md`与current specs。
- Repository Policy已执行并报告36项既有跨仓违规：7个孤立Unity meta与29个旧工程allowlist问题；本框架四个portable／generator工程未出现在违规列表。

## 未完成边界

- 任务7.5等待Foot IK、Performance与PoseGraph相关change全部完成并处理全量OpenSpec既有失败后，再安装`openspec/project.md`与current specs真相。
- Foot字段全集、Domain Bridge、可选Analyzer／Publisher Processor与Performance接入由对应change继续实施，不在本change复制第二路径。

## 2026-09-02 Host合同修正

Foot字段迁移证明原实现仍强制每个Sampler提供`hostAdapterId`和一份`IDiagnosticHostAdapter`，并仍要求Foot Bridge手写Session、Left／Right租包、Capture调用与提交，因此“新增Sampler只声明生命周期Event与Attribute”尚未成立。已有Field Attribute／双输入Extractor／AOT Capture／typed packet／Session／Writer证据继续有效；任务2.1、3.1至3.5、4.2至4.4、5.2、5.3、6.2、6.3、7.1和7.4重新打开。正式目标改为三个typed生命周期Event驱动的生成处理器与框架内建Schema-driven主表／子表／CSV／manifest；领域不保留Bridge或Host Adapter，Analyzer／Publisher只读生成产物。
# 历史执行记录（已由0.4合同替代）

本文件只保留单View／生命周期Event阶段的执行证据，不再描述当前框架。当前唯一合同见本change的`proposal.md`、`design.md`、`specs/`与独立包`D:/Unity_Project_1/generated-diagnostic-sampling`：Capability声明多个Fact Root，Program生成`Start/HandleCommitted/Stop`，Host按Schema生成CSV／manifest，Disabled构建剥离全部采样闭包。以下View、Event DTO、Bridge与旧任务结论不得作为实现依据。

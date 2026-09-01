## 1. 冻结通用Owner与程序集边界

- [x] 1.1 盘点`refactor-foot-ik-diagnostic-sampling`中Attribute、Schema Compiler、Generated Program、typed packet、Session、Writer、Host Reader、manifest与Build descriptor的通用责任，输出唯一Owner迁移表并用全文搜索确认Foot change只保留领域插件责任
- [x] 1.2 固定Contracts、Generator、Runtime、Host与Domain Plugin的程序集依赖方向，确认Contracts不引用UnityEditor／具体领域、Generator不进入Player、Host不被Runtime引用且不存在循环asmdef
- [x] 1.3 固定Unity 2022.3、.NET Standard 2.0、Roslyn API版本和Generator binary identity，使用规定的`--disable-build-servers /nr:false /p:UseSharedCompilation=false`参数构建生成器工程并立即执行`dotnet build-server shutdown`

## 2. 建立canonical Contracts与Identity

- [x] 2.1 定义Diagnostic Capability、具体Committed View／Capture Metadata双输入、Sampler、Capture Program Definition、Field、Group、Table、availability与Host plugin descriptor，使用构造校验确认框架合同不包含Foot、PoseGraph、Simulation、Camera或AI专用字段
- [x] 2.2 定义normalized descriptor排序与Capability／Sampler Set／Schema／Generated Program／packet layout／Capture identity算法，通过重复编译产物hash核对确认相同输入身份稳定
- [x] 2.3 定义封闭基础类型族、dense typed handle、固定容量table record和codec revision，全文搜索确认packet合同不包含`object[]`、值Dictionary、managed Runtime引用或opaque领域blob
- [x] 2.4 定义Compile、Preflight、Capture、Writer与Host Finalization的typed failure合同，确认错误携带Capability、Program、Sampler、Field／Table与阶段identity且不存在跳字段或兼容默认值

## 3. 实现唯一编译期Schema Compiler与Source Generator

- [x] 3.1 建立唯一Source Generator入口，从Roslyn compilation symbols发现Capability、Program、Sampler、Field、Group与Table定义，并用生成诊断确认无需Unity类型目录或运行时反射Catalog
- [x] 3.2 实现编译期Schema闭包校验，拒绝重复identity、跨Capability引用、未知分组、断裂availability、非法codec、AOT非法双输入Extractor签名、派生环和Host必需字段缺失
- [x] 3.3 实现Sampler union、稳定Field排序、dense typed handle、table layout与capacity plan，检查生成descriptor确认同Field identity在一个Program中只有一个求值位置
- [x] 3.4 生成领域具体`Capture(in CommittedView, in CaptureMetadata, ref Packet)`普通C#静态函数和Schema常量，检查生成源码确认直接调用Extractor且不存在Expression、Reflection、DynamicInvoke、每列Delegate或领域中央switch
- [x] 3.5 生成Program source hash、Generator binary identity与assembly binding并接入compile diagnostics，确认Editor与Player编译对相同Program Definition发布相同Schema、Program和packet layout identity

## 4. 实现通用Runtime Session与typed packet

- [x] 4.1 实现按type family分页的预分配packet pool、固定容量子表页与versioned struct lease，检查分配点确认Capturing期间不扩容、不boxing且不创建字段Dictionary
- [x] 4.2 为每个Capability实现独立`Prepared -> Capturing -> Finalizing -> Completed/Faulted/Cancelled` Session／manifest状态机并冻结其Program、Schema、容量、Writer和Host输出闭包，确认非法转换发布typed failure且不存在跨Capability共享Session
- [x] 4.3 实现每Capability独立cadence identity、opaque typed lineage、sample key、packet流与单一有界队列，确认框架不识别Presentation Frame／Simulation Tick，两个Capability不会合并身份、共享packet layout或互借容量
- [x] 4.4 实现非阻塞packet提交与Finalize请求、Overflow／Sequence fault和后台sealed Writer，检查线程调用点确认领域主线程不等待IO、格式化、Analyzer或Publisher
- [x] 4.5 实现packet、Schema descriptor、runtime manifest与文件hash闭包，确认未闭合stream不能发布Completed

## 5. 实现通用Host Reader与Finalizer

- [x] 5.1 实现sealed packet Reader并严格核对Capability、Program、Schema、layout、capacity、sample key、lineage和文件hash，确认不存在列数猜测、默认值补全或旧layout兼容
- [x] 5.2 实现每Sampler无复制dense handle view与通用格式化边界，确认多个Sampler复用同一packet且Host不重新执行Extractor或复制第二字段Schema
- [x] 5.3 实现Host Adapter目录、固定顺序Finalizer、逐Sampler manifest和Capability manifest，确认Host插件故障只使对应Capability Faulted且不会产生部分Completed；框架不得创建Performance顶层Capture状态机
- [x] 5.4 收紧Host权限与依赖，全文搜索确认Host不持有Committed View／Capture Metadata、不引用领域Runtime Module、不扫描Player私有地址且不执行World Query或业务求解

## 6. 建立通用Diagnostic Capability构建合同

- [x] 6.1 定义canonical `DiagnosticCapabilityDescriptor`与稳定排序`DiagnosticCapabilitySet` identity，每项覆盖Mode、Sampler Set、Schema、Program、cadence、packet capacity和transport identity，并由Program identity闭合Generated Program、Generator binary与packet layout revision
- [x] 6.2 定义Player专属`DiagnosticCompilationClosureProof`，确认Disabled进入领域Definitions／Bridge／Generated Program排除闭包，Capture只包含匹配程序集与scripting define
- [x] 6.3 定义Player manifest、Run Request、握手、Runtime／Capability manifest和Comparer共享的Capability Set codec，确认任一模式、Program、Schema、cadence、容量或transport变化均可被精确定位
- [x] 6.4 向`add-gameplay-performance-capture-workflow`交付唯一Build／Controller集成合同，全文搜索确认框架没有创建第二Player、第二Controller、第二Capture根或第二Comparer

## 7. 收口首个插件接入边界与最终一致性

- [x] 7.1 向`refactor-foot-ik-diagnostic-sampling`交付Capability Definition、具体Capture Metadata、Program Definition、Domain Bridge、Host Adapter与manifest扩展点，并搜索确认Foot插件不再拥有第二Generator、通用packet、Session、Writer或Host Orchestrator
- [x] 7.2 与`refactor-character-pose-graph-architecture`对齐PoseGraph唯一生产并控制具体`CharacterFootIkCommittedCaptureViewLease`的边界；搜索确认框架不存在通用Committed View／Capture Metadata接口／基类／DTO，PoseGraph不引用框架Generator、Generated Program、packet、Host或Capability Build类型
- [x] 7.3 删除所有把通用Owner绑定到Foot的类型、配置和命名，不保留wrapper；用程序集与符号搜索确认通用模块中`Foot`、`FBBIK`、`PoseGraph`、`Camera`和`AI`领域分支为零
- [x] 7.4 使用规定参数构建受影响portable／generator工程并立即执行`dotnet build-server shutdown`，执行Repository Policy、`git diff --check`及禁止Expression／Reflection／fallback路径搜索且不运行Unity batchmode；Repository Policy报告36项既有跨仓违规，本框架四个工程均已进入allowlist且未新增违规
- [ ] 7.5 严格校验本change、Foot IK采样、Performance工作流、PoseGraph及全量OpenSpec；实现闭合后再更新`openspec/project.md`和current specs，不提前安装未实施框架真相

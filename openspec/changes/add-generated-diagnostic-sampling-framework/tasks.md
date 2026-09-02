## 1. 冻结通用Owner与程序集边界

- [x] 1.1 盘点`refactor-foot-ik-diagnostic-sampling`中Attribute、Schema Compiler、Generated Program、typed packet、Session、Writer、Host Reader、manifest与Build descriptor的通用责任，输出唯一Owner迁移表并用全文搜索确认Foot change只保留领域插件责任
- [x] 1.2 固定Contracts、Generator、Runtime、Host与Domain Plugin的程序集依赖方向，确认Contracts不引用UnityEditor／具体领域、Generator不进入Player、Host不被Runtime引用且不存在循环asmdef
- [x] 1.3 固定Unity 2022.3、.NET Standard 2.0、Roslyn API版本和Generator binary identity，使用规定的`--disable-build-servers /nr:false /p:UseSharedCompilation=false`参数构建生成器工程并立即执行`dotnet build-server shutdown`

## 2. 建立canonical Contracts与Identity

- [ ] 2.1 收口Diagnostic Capability、Started／CommittedSample／Stopped Event、具体Committed View／Capture Metadata双输入、样本维度、Sampler、Capture Program Definition、Field、Group、Table、availability与通用输出格式descriptor，删除Bridge／Host Adapter／Processor参与采样生命周期的合同并确认框架不包含领域专用字段
- [x] 2.2 定义normalized descriptor排序与Capability／Sampler Set／Schema／Generated Program／packet layout／Capture identity算法，通过重复编译产物hash核对确认相同输入身份稳定
- [x] 2.3 定义封闭基础类型族、dense typed handle、固定容量table record和codec revision，全文搜索确认packet合同不包含`object[]`、值Dictionary、managed Runtime引用或opaque领域blob
- [x] 2.4 定义Compile、Preflight、Capture、Writer与Host Finalization的typed failure合同，确认错误携带Capability、Program、Sampler、Field／Table与阶段identity且不存在跳字段或兼容默认值

## 3. 实现唯一编译期Schema Compiler与Source Generator

- [ ] 3.1 扩展唯一Source Generator入口，从Roslyn compilation symbols发现Capability、三个生命周期Event、Sample Dimension、Program、Sampler、Field、Group与Table定义，并用生成诊断确认无需领域Bridge、Unity类型目录或运行时反射Catalog
- [ ] 3.2 更新编译期Schema闭包校验，拒绝重复identity、跨Capability引用、未知分组、断裂availability、非法codec／输出格式、AOT非法双输入Extractor签名、派生环和Processor必需字段缺失，并确认普通Sampler不再要求Host Adapter
- [ ] 3.3 更新Sampler union、稳定Field排序、sample dimension展开、dense typed handle、table layout与capacity plan，删除Host Adapter identity后检查生成descriptor确认同Field identity在一个Program中只有一个求值位置
- [ ] 3.4 生成领域具体`Capture(in CommittedView, in CaptureMetadata, ref Packet)`与Started／CommittedSample／Stopped typed事件处理器，检查生成源码确认自动Session、租包、维度循环、Extractor、提交与封存且不存在Bridge、Expression、Reflection、DynamicInvoke或领域中央switch
- [ ] 3.5 重建Program source hash、Generator binary identity与assembly binding并接入compile diagnostics，确认删除Host Adapter identity后的Editor与Player编译发布相同Schema、Program和packet layout identity

## 4. 实现通用Runtime Session与typed packet

- [x] 4.1 实现按type family分页的预分配packet pool、固定容量子表页与versioned struct lease，检查分配点确认Capturing期间不扩容、不boxing且不创建字段Dictionary
- [ ] 4.2 把每Capability独立`Prepared -> Capturing -> Finalizing -> Completed/Faulted/Cancelled` Session／manifest状态机接入生成生命周期处理器，确认领域只发布三个Event、非法转换发布typed failure且不存在手写Session控制或跨Capability共享Session
- [ ] 4.3 实现每Capability独立cadence identity、opaque typed lineage、sample key、声明式enum／稳定ID维度展开、packet流与单一有界队列，确认框架不识别Presentation Frame／Simulation Tick／Left／Right业务语义
- [ ] 4.4 实现Generated CommittedSample非阻塞packet租用／提交与Generated CaptureStopped封存、Overflow／Sequence fault和后台sealed Writer，检查领域主线程不等待IO且没有Foot Bridge
- [x] 4.5 实现packet、Schema descriptor、runtime manifest与文件hash闭包，确认未闭合stream不能发布Completed

## 5. 实现通用Host Reader与Finalizer

- [x] 5.1 实现sealed packet Reader并严格核对Capability、Program、Schema、layout、capacity、sample key、lineage和文件hash，确认不存在列数猜测、默认值补全或旧layout兼容
- [ ] 5.2 实现每Sampler无复制dense handle view与内建Schema-driven主表／固定子表／RFC 4180 CSV Finalizer，确认多个Sampler复用同一packet、稳定展开Vector／Quaternion／availability且Host不重新执行Extractor或复制第二字段Schema
- [ ] 5.3 实现无需Adapter的逐Sampler基础manifest和Capability manifest；确认普通Sampler可独立Completed，Analyzer／Publisher只读生成artifact并拥有独立下游结果，不参与采样生命周期，框架不得创建Performance顶层Capture状态机
- [x] 5.4 收紧Host权限与依赖，全文搜索确认Host不持有Committed View／Capture Metadata、不引用领域Runtime Module、不扫描Player私有地址且不执行World Query或业务求解

## 6. 建立通用Diagnostic Capability构建合同

- [x] 6.1 定义canonical `DiagnosticCapabilityDescriptor`与稳定排序`DiagnosticCapabilitySet` identity，每项覆盖Mode、Sampler Set、Schema、Program、cadence、packet capacity和transport identity，并由Program identity闭合Generated Program、Generator binary与packet layout revision
- [ ] 6.2 更新Player专属`DiagnosticCompilationClosureProof`，确认Disabled进入领域Definitions／typed lifecycle handlers／Generated Program排除闭包，Capture只包含匹配Event与Program程序集和scripting define
- [ ] 6.3 更新Player manifest、Run Request、握手、Runtime／Sampler／Capability manifest和Comparer共享的Capability Set codec，删除Host Adapter／Processor字段并确认任一模式、Event Set、Program、Schema、输出格式、维度、cadence、容量或transport变化均可被精确定位
- [x] 6.4 向`add-gameplay-performance-capture-workflow`交付唯一Build／Controller集成合同，全文搜索确认框架没有创建第二Player、第二Controller、第二Capture根或第二Comparer

## 7. 收口首个插件接入边界与最终一致性

- [ ] 7.1 向`refactor-foot-ik-diagnostic-sampling`交付Capability Definition、三个typed生命周期Event、具体Capture Metadata、Sample Dimension、Program Definition与内建CSV，搜索确认Foot插件不拥有Bridge、手写Column／CsvBinding、Host Adapter、通用packet、Session、Writer或Host Orchestrator；Analyzer／Publisher只读生成产物
- [x] 7.2 与`refactor-character-pose-graph-architecture`对齐PoseGraph唯一生产并控制具体`CharacterFootIkCommittedCaptureViewLease`的边界；搜索确认框架不存在通用Committed View／Capture Metadata接口／基类／DTO，PoseGraph不引用框架Generator、Generated Program、packet、Host或Capability Build类型
- [x] 7.3 删除所有把通用Owner绑定到Foot的类型、配置和命名，不保留wrapper；用程序集与符号搜索确认通用模块中`Foot`、`FBBIK`、`PoseGraph`、`Camera`和`AI`领域分支为零
- [ ] 7.4 Host合同收口后使用规定参数重建受影响portable／generator工程并立即执行`dotnet build-server shutdown`，执行Repository Policy、`git diff --check`及禁止Expression／Reflection／fallback路径搜索且不运行Unity batchmode；区分既有跨仓违规与本次新增问题
- [ ] 7.5 严格校验本change、Foot IK采样、Performance工作流、PoseGraph及全量OpenSpec；实现闭合后再更新`openspec/project.md`和current specs，不提前安装未实施框架真相

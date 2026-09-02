## 1. 固定独立0.5.1 Owner与发布身份

- [x] 1.1 将Annotations、Source Generator、Capture Runtime和Editor Host迁入独立`com.kk.generated-diagnostic-sampling` Owner，并以仓库、package路径和命名空间搜索确认通用代码不含Foot／PIK／PoseGraph领域分支
- [x] 1.2 固定Annotations → Domain／Generator、Generated Program → Runtime、Host Editor-only的单向程序集依赖，检查asmdef与portable工程确认无循环且Generator不进入Player
- [x] 1.3 使用规定的`--disable-build-servers /nr:false /p:UseSharedCompilation=false`参数完成0.5.1 Release构建并立即执行`dotnet build-server shutdown`，核对package Analyzer SHA-256、MVID、assembly name和Generator identity

## 2. 收口multi Fact Root编译合同

- [x] 2.1 将Capability合同改为一个Metadata类型加多个`DiagnosticFactRoot(rootId,type)`，将Program改为Dimension ID数组加Sampler集合，并通过Probe Schema确认不含单View／Dimension View或生命周期Event DTO descriptor
- [x] 2.2 实现path-scoped `DiagnosticField`，按Capability／Table／Fact Root／成员路径生成identity并从CLR类型推断codec；Probe确认同一类型在多个Root／Path复用时叶子Attribute只有一份
- [x] 2.3 放宽现有无setter计算getter并支持外部只读class page Table，检查generated source确认直接调用getter、Count与只读索引器且不构造DTO或复制集合
- [x] 2.4 将`DiagnosticDerivedField`限制为真正公式和实际Fact Root参数子集，验证参数名到Root ID、`in`类型、Metadata尾参数、availability与依赖环诊断
- [x] 2.5 升级Dimension／Fact Root Set、Field Path、Schema、codec、Program和packet identity，确认0.4 Reader拒绝0.3单View文档与旧Analyzer

## 3. 生成AOT Capture与Lifecycle

- [x] 3.1 扩展唯一Roslyn Generator发现multi Fact Root、Metadata、Field／Table、Sampler与Program；缺少`KK_DIAGNOSTIC_SAMPLING`时Probe确认零generated source和零identity
- [x] 3.2 生成`Capture(in Root0, ..., in Metadata, ref Packet)`并直接展开成员访问、enum与Unity值转换，检查generated source不存在普通Getter／Extractor、Side选择、Expression、Reflection、DynamicInvoke或字符串路径执行
- [x] 3.3 在既有静态Capture与Session基础上生成目标隔离`DiagnosticEvent` typed dispatcher、可选partial interest Query和Program handler，跨程序集Probe以两个Dimension和两个Fact Root确认真实lineage、Start冻结Metadata、自动rent、逐Dimension Capture、submit与Fault，Disabled Probe确认Query调用与参数求值消失
- [x] 3.4 生成Sampler union、dense typed handle、固定Table layout与Schema descriptor，检查同一Field identity在一个sample中只有一个求值位置
- [x] 3.5 将Generator binary、生成source hash、Fact Root type identity、Dimension Set与assembly binding闭合进Program identity，并通过重复Release构建hash核对确定性

## 4. 实现有界Runtime与sealed packet

- [x] 4.1 实现按type family分页的预分配packet pool、固定容量Table页和versioned struct lease，检查Capturing路径不扩容、不boxing且不保存业务managed引用
- [x] 4.2 实现每Capability独立`Prepared -> Capturing -> Finalizing -> Completed/Faulted/Cancelled` Session，Probe确认Start冻结Program／Schema／容量／interest且非法状态产生typed failure
- [ ] 4.3 实现generated Program handler的非阻塞租用／提交、Host workflow Stop退订与后台封存，检查业务一行partial Event调用不等待IO且领域不持有Session／packet
- [x] 4.4 实现Overflow、sequence、Writer和hash failure闭包，确认Faulted保留已有证据但不发布部分Completed身份
- [x] 4.5 实现packet、Schema descriptor、runtime manifest与文件hash闭包，Reader验证identity、layout、capacity、sample key和opaque lineage后才允许读取

## 5. 实现Schema-driven Host

- [x] 5.1 实现sealed packet Reader并拒绝错误Schema／Program／layout／capacity／文件hash，确认不存在列数猜测、默认值补全或旧layout兼容
- [x] 5.2 实现每Sampler无复制canonical handle视图和Schema-driven主表／固定子表／RFC 4180 CSV Finalizer，验证Vector／Quaternion稳定展开及availability空单元格
- [x] 5.3 实现不需要Adapter的Sampler manifest与Capability manifest，Probe确认普通Sampler在零领域Processor时仍可Completed
- [x] 5.4 收紧Host依赖，全文搜索确认不存在Host Adapter、Column／CsvBinding、Fact Root／Metadata持有、业务成员反读、Derived调用、Player内存扫描或World Query
- [x] 5.5 固定Analyzer／Publisher为Completed artifact消费者，确认它们不参与Capture生命周期、不映射字段且拥有独立下游结果身份

## 6. 建立通用Build与Disabled闭包

- [x] 6.1 实现Conditional Annotations、Generator零输出、Runtime define constraints和Editor-only Host，portable Disabled Probe确认业务DLL零Diagnostic Attribute、零Sampling AssemblyRef与零identity
- [x] 6.2 实现canonical `DiagnosticCapabilityDescriptor`、稳定排序`DiagnosticCapabilitySet`和`DiagnosticCompilationClosureProof`，核对descriptor覆盖Mode、Dimension／Fact Root Set、Sampler、Schema、Program、cadence、capacity与transport
- [x] 6.3 实现Cecil／IL2CPP Disabled Player Gate脚本，检查项覆盖Annotations根程序集、Runtime／领域Diagnostics程序集、Generated Program、Session／packet／queue／interest类型及Capability／Field identity
- [ ] 6.4 将3C Performance Build Request、Player manifest、Run Request、握手、Runtime／Capability manifest与Comparer接入同一Capability Set codec，并以全文搜索确认没有Foot专属构建字段或第二Comparer
- [ ] 6.5 用3C真实Disabled IL2CPP Player执行Gate，保存零Attribute／AssemblyRef／程序集／类型／identity的机器可读闭包证据

## 7. 迁移3C Foot消费方

- [x] 7.1 将3C package依赖指向独立`com.kk.generated-diagnostic-sampling` 0.5.1路径，并核对消费到的package version与唯一Analyzer发布身份
- [x] 7.2 将Foot Capability收口为现有Fact Root集合与Metadata，将Program声明Event与Left／Right Dimension，并在同步Commit点只调用一行target-scoped Foot `DiagnosticEvent` partial方法；删除手写GeneratedCapture／Consumer／Binding转发，Metadata与Start／Stop留在Host workflow
- [x] 7.3 把全部普通Foot字段迁到现有readonly field／property／计算getter的单个path-scoped Attribute，将固定集合迁到现有buffer／class page Table，并以搜索确认旧方法式Getter／Extractor为零
- [x] 7.4 删除Foot运行时公式与无字段root，通过Generator diagnostics确认没有Capture View、Side选择、普通字段转发或未注册Root
- [ ] 7.5 删除Committed／Dimension View、Started／CommittedSample／Stopped领域Event DTO、Projection／Group根、Bridge、Column／CsvBinding、Host Adapter、旧Reader及全部兼容wrapper，并用程序集与符号搜索确认零残留
- [x] 7.6 接通Schema-driven Host主表／子表／manifest作为唯一基础采样产物，并从采样链删除旧单体Analyzer／Publisher、评分报告与第二字段映射；领域离线诊断由`add-schema-driven-diagnostic-analysis`独立恢复
- [ ] 7.7 用3C Capture IL2CPP Player核对multi Fact Root `in`调用、Generated Program／Schema／packet／CSV／manifest identity和左右Dimension数据闭合

## 8. 收口项目一致性

- [ ] 8.1 与`refactor-character-pose-graph-architecture`核对剥离边界：PoseGraph只拥有正式Committed Result／事务／Seal／Post-Commit，不定义或等待Sampling View、Event、Session、packet与interest
- [ ] 8.2 使用规定参数重建受影响portable／Generator工程并立即关闭build server，执行Repository Policy、`git diff --check`及禁止View／Event DTO／Expression／Reflection／Host Adapter／fallback路径搜索且不运行Unity batchmode
- [ ] 8.3 严格校验本change、`refactor-foot-ik-diagnostic-sampling`、`add-gameplay-performance-capture-workflow`、`refactor-character-pose-graph-architecture`及全量OpenSpec，区分既有失败与本次新增问题
- [ ] 8.4 只有在3C Capture与Disabled Player闭包均完成后才更新`openspec/project.md`和current specs；用户验收后再归档，不提前安装未完成消费方真相

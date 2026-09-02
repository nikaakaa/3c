## 1. 对账Owner与串行接入点

- [ ] 1.1 重新同步PoseGraph与通用框架合同：PoseGraph唯一拥有Committed View租约生产／寿命，框架拥有typed生命周期处理器、Session、packet、Writer、Schema-driven CSV与manifest，本change只拥有Foot Event／Attribute和下游Analyzer／Publisher；确认Bridge／Host Adapter Owner为零
- [ ] 1.2 重新同步Performance Build Request、Player／Run／Capture manifest、握手与Comparer合同：同一Performance入口通过通用Capability Set承载Foot Event Set和Disabled／Capture身份，不建立Foot专属Build字段、Session控制、第二Player或Controller
- [x] 1.3 对账`consolidate-foot-diagnostic-scoring`剩余5.3与当前工作区Analyzer/Publisher差异，输出唯一Owner清单；若同字段或同规则仍在修改则停止并报告具体冲突，不覆盖已改对实现
- [x] 1.4 建立现行主行、geometry、Runtime来源、派生字段、availability、Analyzer消费与评分消费的字段迁移清单；以当前唯一Schema展开结果和全部诊断Required Field集合核对零遗漏、零重复

## 2. 验收PoseGraph唯一Committed Foot IK Capture合同

- [x] 2.1 验收PoseGraph change定义的`CharacterFootIkCaptureInterest`、完整Frame/Completion/Program/Projection/Rig/Tuning lineage和`CharacterFootIkCommittedCaptureViewLease`合同；用构造校验和编译确认本change没有第二View类型，合同不暴露Module、Bank、Workspace、Vendor或Transform引用
- [x] 2.2 验收Foot、Constraint/FBBIK与Final Publication只在PoseGraph于Frame开始冻结的interest要求下，把允许观察的Pending Result写入各自Owned诊断页；用代码搜索确认无interest分支不构造Foot IK payload
- [x] 2.3 验收`CharacterPoseDiagnosticsProjector`只在根Frame成功Seal后按同lineage组合并交付唯一Foot IK View租约；用Frame/Completion校验入口确认Discard、Fault和租约失效后均不可读取
- [x] 2.4 验收Root/Physical空间事实归入Final Publication Committed Result并由PoseGraph具体View读取；用全文搜索确认新链不读取场景Transform、RootHierarchy或Physical Bone反推结果
- [ ] 2.5 让Generated CaptureStarted Handler把Foot Program descriptor归一化为单一`CharacterFootIkCaptureInterest`请求并交给PoseGraph冻结，确认PoseGraph不引用具体Sampler identity、框架packet或Host类型

## 3. 建立Foot Capability、字段与Program Definitions

- [x] 3.1 定义`character-foot-ik` Capability Definition、领域typed lineage descriptor与PoseGraph-owned具体Committed View类型引用，使用框架构造校验确认Foot插件不重定义View且不暴露Module、Bank、Workspace、Vendor、Transform或Foot写权限
- [ ] 3.2 使用框架Attribute定义AOT-safe Foot Field、Group、Table与Derived Extractor及稳定identity/revision；每个业务组使用独立Extractor类型，删除中央`partial CharacterFootIkDiagnosticFields`和跨字段组private读取，且Extractor不得引用`UnityEditor`、动态调用、运行时成员路径、World Query、Vendor或Transform
- [ ] 3.3 定义Full、Solver、Landing等Foot Sampler Definition、通用输出格式与Left／Right样本维度，不声明Host Adapter／Analyzer identity；通过框架Schema preflight确认Event、维度、重复identity、未知分组、availability、codec、AOT签名和派生环错误在编译期失败
- [ ] 3.4 定义稳定Foot Capture Program Definition，每个Program显式组合一套或多套Sampler；检查框架生成descriptor确认同Field identity只有一个dense handle和求值位置
- [ ] 3.5 接入框架为Foot具体View／Metadata与三个生命周期Event生成的typed Handler／Capture Program，核对Editor与IL2CPP Player使用相同Event Set／Schema／Program／packet layout identity且Foot代码中不存在Bridge、第二Source Generator、Expression或反射Catalog
- [ ] 3.6 声明Foot Sampler输出格式和Analyzer artifact需求，确认通用Host自动生成Header、Writer、Reader、主表／子表名称、顺序、类型、单位和availability，Foot不保留Column／CsvBinding或第二Schema
- [ ] 3.7 用框架codec声明Vector、Quaternion、枚举、布尔、identity与Ground Geometry固定容量record layout，确认Foot插件不增加通用codec、opaque blob或手写位置协议

## 4. 实现三个Event驱动的多Sampler Capture生命周期

- [ ] 4.1 将选中Foot Program Definition的Sampler Set合并为Runtime唯一typed interest，确认同时选择多个Sampler时Foot查询、Goal Assembly、FBBIK、Final Writer与Capture View数量不增加
- [ ] 4.2 定义Foot CaptureStarted Event并由Generated Handler创建Session、冻结Program／Schema／容量／interest，检查调用图确认Foot不直接构造Session、packet池、队列或Writer
- [ ] 4.3 定义只在Post-Seal短租约View发布的Foot CommittedSample Event，携带Frame／Completion lineage、Metadata和Left／Right声明维度；检查Generated Handler自动租包、调用Program、提交且Foot没有Bridge或手写双脚循环
- [ ] 4.4 定义Foot CaptureStopped Event携带Completed／Cancelled／Faulted outcome，检查Generated Handler自动封存packet、基础CSV、Sampler／Capability manifest并统一传播Overflow、Sequence、Writer、Host与hash故障
- [ ] 4.5 将现有Start、Controlled Capture Window、Stop、Finalizing、状态和产物打开入口迁移到三个Event与Foot Program选择；用Launcher状态与菜单/MCP调用图确认只有一个Capture生命周期Owner

## 5. 迁移内建Full Foot Sampler

- [ ] 5.1 按字段迁移清单把现行Identity、Timing、Formal Input/Output、Landing、Ground Path、Lifecycle、Response、Pelvis、Goal、Solver与Physical字段迁入Attribute Extractor分组；用新旧Schema业务字段清单核对含义、单位和availability
- [ ] 5.2 把Ground Contact、Envelope与Surface geometry迁入Attribute Table Extractor和固定容量packet页；用表manifest核对行identity、Frame/Completion/Side与主行关联完整
- [ ] 5.3 把Envelope交点、穿透、可见输出运动学及其它Sampler派生事实迁入纯Derived Extractor；用依赖清单确认只读取Committed字段且不调用World Query、FBBIK或Transform
- [ ] 5.4 将现有Analyzer唯一解析入口、Publisher、details/index、七维评分和剩余正式规则接到生成基础CSV／typed artifact／manifest；用Required Field与schema identity核对没有第二Reader、Column binding或规则副本，且下游失败不回写Capability生命周期
- [ ] 5.5 发布新的Full Sampler、Foot Capability manifest与artifact Schema identity，保留旧采样目录不变；Performance继续唯一发布顶层Capture，路径与reader搜索确认新实现不迁移、不覆盖也不兼容读取旧Schema

## 6. 接入Performance Player的Disabled与Capture AOT身份

- [ ] 6.1 向稳定排序`DiagnosticCapabilitySet`注册`character-foot-ik` Disabled/Capture descriptor，Capture引用合法Foot Event Set／Program Definition，Disabled不提供Foot Definitions／typed Handler／Generated Program／页／interest闭包
- [ ] 6.2 接入Performance工作流提供的Player专属编译输入，确认Foot插件不修改全局配置、不实现`BuildPlayerOptions`第二封装且不建立第二Build入口
- [ ] 6.3 让Player manifest、Run Request、Capture manifest与握手通过通用codec保存并核对Foot Capability identity，搜索确认不存在Foot专属重复manifest字段或identity算法
- [ ] 6.4 使用通用Comparer核对Foot Disabled/Capture、Program、Sampler Set、Schema／Generated Program、容量和transport，确认身份不同会精确拒绝性能差值而Foot插件不实现第二Comparer
- [ ] 6.5 搜索Performance Player创建链，确认Disabled不含Foot Definitions、typed Event Handler、Generated Program、capture页或interest，Capture不含Bridge、运行时表达式／反射、Foot专属队列、第二Player或第二Controller

## 7. 激进删除旧采样链

- [ ] 7.1 删除`CharacterFootLandingPredictionDebugRegistry`采样订阅、PendingFrame等待、RuntimeTarget轮询、Pose Watch注册和RootHierarchy读取；用`rg`确认旧事件/Snapshot join类型与调用点为零
- [ ] 7.2 删除手写`CharacterFootCsvColumn/Group`及各领域Column Source/Record getter/setter、Geometry Header和旧Reader绑定；用`rg`和编译确认不存在第二Schema声明
- [ ] 7.3 删除旧单体Sampler内部Writer／Finalizer装配、Foot Bridge、每Sampler Host Adapter和旧菜单Session控制，只保留三个生命周期Event与生成artifact打开入口；用调用图搜索确认没有Foot专属Writer、双Finalizer或兼容wrapper
- [ ] 7.4 收紧Foot Runtime、领域Event／Definitions与Analyzer／Publisher依赖方向；确认Foot不拥有Bridge、Source Generator、通用packet／Session／Writer／Reader／CSV映射，正式Runtime Result不引用Attribute／CSV知识且Capture不引用Expression或运行时反射

## 8. 构建、规范与最终一致性

- [ ] 8.1 使用`--disable-build-servers /nr:false /p:UseSharedCompilation=false`构建受影响Runtime、Editor与Controller工程并在每次结束后立即执行`dotnet build-server shutdown`；确认零编译错误且不运行Unity batchmode
- [ ] 8.2 执行Repository Policy、`git diff --check`和禁止路径全文搜索，确认没有私有虚拟地址扫描、Shared Memory fallback、运行时表达式编译／反射、旧Schema reader、Editor/Player双提取实现或第二Performance入口
- [ ] 8.3 严格校验`add-generated-diagnostic-sampling-framework`、本change、PoseGraph、Performance及全量OpenSpec，确认通用Owner与Foot插件Owner不重叠
- [ ] 8.4 更新`openspec/project.md`为通用Generated Diagnostic Sampling Framework与首个Foot IK插件的实际真相，并用current spec对比确认没有把未实施能力提前写成已安装状态

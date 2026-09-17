## 1. 已完成的Foot生成采集链

- [x] 1.1 定义character-foot-ik多Fact Root Capability与固定Capture Metadata；Metadata不保存Side或业务结果
- [x] 1.2 把普通采样字段迁到现有真实readonly成员的单一path-scoped DiagnosticField，并由CLR类型推断codec
- [x] 1.3 删除Foot Runtime的一字段一Getter／Extractor、Projection、中央字段容器和DiagnosticDerivedField
- [x] 1.4 把Ground Contact、Envelope与Surface声明为ground-contacts、ground-envelope、ground-surfaces三张真实class page表，行字段直接标记真实成员
- [x] 1.5 定义独立Core／Full Sampler与Capture Program，由Roslyn从Group／IncludeAll分别生成Schema、typed packet layout，并让两个Program绑定同一个`DiagnosticEvent` typed dispatcher和左右handler
- [x] 1.6 在成功Seal后的同步Commit边界只调用一行Foot `DiagnosticEvent` partial方法并以in传入target、真实lineage、现有Left／Right与公共Fact Root；删除`ICharacterFootIkCommittedCaptureConsumer`、Capture Binding和手写GeneratedCapture／TryCapture转发
- [x] 1.7 编译字段、Program、Event dispatcher和handler，确认业务调用不包含Side选择、DTO构造、Projection、Getter、表达式树或运行时反射，且target无订阅立即返回、Disabled Event／Query调用与参数求值被消除
- [x] 1.8 删除三个Foot typed Event、Dimension类型、Metadata Side、旧DiagnosticTableCount和旧max-count Ground Geometry合成表
- [x] 1.9 删除没有任何采样字段却要求构造AnimationBiomechanicalStepReadPage的foot-steps根

## 2. 通用Host基础产物

- [x] 2.1 将3C Foot Capture的sealed packet接入通用Schema-driven Host Finalizer
- [x] 2.2 由通用Host生成Full Sampler主表、ground-contacts、ground-envelope和ground-surfaces CSV
- [x] 2.3 生成并封存Sampler manifest与Capability manifest，保存Schema／Program identity、Dimension、单位、availability与文件hash
- [x] 2.4 自动检查Table容量、packet序列、Writer与Host失败，确保任一基础产物失败时Capability不发布Completed
- [x] 2.5 删除Foot Editor中的CharacterFootCsvColumn、CsvBinding、手写Header、旧Reader字段映射和领域Host Adapter

## 3. 从采样链删除旧Foot报告系统

- [x] 3.1 从采样链删除旧Foot单体Analyzer、Publisher、七维评分、规则报告与诊断Store；领域算法语义与当前报告由`add-schema-driven-diagnostic-analysis`独立迁移，不恢复第二套采样或字段映射
- [x] 3.2 删除旧字段迁移清单、旧Schema reader、别名、默认值与历史包兼容读取
- [x] 3.3 固定输入Presentation Schedule证据只直接读取生成主表、ground-contacts与ground-envelope，不重建旧samples.csv或合成geometry

## 4. 唯一Editor诊断入口

- [x] 4.1 用通用Editor workflow registry暴露编译进来的诊断Capability，不让主Editor程序集依赖条件Foot程序集
- [x] 4.2 Capture构建通过KK_DIAGNOSTIC_SAMPLING与KK_DIAGNOSTIC_FOOT包含Foot Program、Runtime与Editor workflow
- [x] 4.3 Launcher、固定输入回放和MCP状态统一调用生成式Foot workflow，旧菜单与旧Sampler删除
- [x] 4.4 支持普通采样与受控窗口采样；Stop后由通用Host封存主表、三张子表与Capability manifest

## 5. Disabled与Capture Player硬门禁

- [ ] 5.1 构建Disabled Player并用Cecil确认业务程序集零Diagnostic custom attribute与零Sampling AssemblyRef
- [ ] 5.2 检查Disabled IL2CPP输出，确认零Foot Diagnostics／Sampling Runtime程序集、零Generated Program／Session／packet／queue／interest和零Field／Capability identity字符串
- [ ] 5.3 构建Capture Player并确认IL2CPP闭包包含匹配Schema identity的静态Capture程序与三张Table layout
- [ ] 5.4 确认关闭只由编译符号和asmdef约束完成，不存在runtime bool、空实现、Linker猜测、fallback或构建后静默删除

## 6. 自动化一致性检查

- [x] 6.1 执行受影响Runtime、Editor与Controller编译；dotnet命令使用--disable-build-servers、/nr:false和/p:UseSharedCompilation=false并立即shutdown build server
- [ ] 6.2 执行Repository Policy、git diff --check和禁止路径搜索，确认无旧Getter、Projection、Side选择、合成geometry、Column／CsvBinding、领域Writer与第二采样链
- [ ] 6.3 严格校验本change、generated diagnostic sampling framework、PoseGraph和Performance相关OpenSpec
- [ ] 6.4 完成代码与current specs对账；用户端到端验收前不归档，不把未完成Player Gate写成已安装能力

## 1. 已完成的Foot生成采集链

- [x] 1.1 定义character-foot-ik多Fact Root Capability与固定Capture Metadata；Metadata不保存Side或业务结果
- [x] 1.2 把普通采样字段迁到现有真实readonly成员的单一path-scoped DiagnosticField，并由CLR类型推断codec
- [x] 1.3 删除Foot Runtime的一字段一Getter／Extractor、Projection、中央字段容器和DiagnosticDerivedField
- [x] 1.4 把Ground Contact、Envelope与Surface声明为ground-contacts、ground-envelope、ground-surfaces三张真实class page表，行字段直接标记真实成员
- [x] 1.5 定义Full Sampler与Capture Program，由Roslyn生成统一Schema、typed packet layout、Lifecycle和左右HandleCommitted
- [x] 1.6 实现薄CharacterFootIkGeneratedCapture，在成功Seal后的同步Commit边界直接绑定现有Left／Right与公共Fact Root并以in调用HandleCommitted；采样链不消费CharacterFootIkCommittedCaptureViewLease
- [x] 1.7 编译字段、Program和GeneratedCapture，确认生成调用不包含Side选择、DTO构造、Projection、Getter、表达式树或运行时反射
- [x] 1.8 删除三个Foot typed Event、Dimension类型、Metadata Side、旧DiagnosticTableCount和旧max-count Ground Geometry合成表
- [x] 1.9 删除没有任何采样字段却要求构造AnimationBiomechanicalStepReadPage的foot-steps根

## 2. 通用Host基础产物

- [ ] 2.1 将3C Foot Capture的sealed packet接入通用Schema-driven Host Finalizer
- [ ] 2.2 由通用Host生成Full Sampler主表、ground-contacts、ground-envelope和ground-surfaces CSV
- [ ] 2.3 生成并闭合Sampler manifest与Capability manifest，保存Schema／Program identity、Dimension、Frame范围、单位、availability与文件hash
- [ ] 2.4 自动检查Table容量、packet序列、Writer与Host失败，确保任一基础产物失败时Capability不发布Completed
- [ ] 2.5 删除Foot Editor中的CharacterFootCsvColumn、CsvBinding、手写Header、旧Reader字段映射和领域Host Adapter

## 3. Analyzer与Publisher下游迁移

- [ ] 3.1 将Foot Analyzer改为只通过生成manifest读取主表与三张Ground子表，不维护第二字段映射
- [ ] 3.2 将Envelope统计、穿透、事件统计和其它跨字段公式从Player采集链迁入Analyzer
- [ ] 3.3 将现有诊断规则、明细、七维评分、资格与分母接到新Analyzer事实，保持业务数学
- [ ] 3.4 将Publisher改为只消费Analyzer结果和生成manifest，不参与Session、packet、Writer或Host生命周期
- [ ] 3.5 保持历史封存采样包不可变，删除旧Schema兼容reader、别名、默认值和双写路径

## 4. Performance工作流接入

- [ ] 4.1 通过唯一DiagnosticCapabilitySet为character-foot-ik声明Disabled与Capture构建身份
- [ ] 4.2 Capture构建定义KK_DIAGNOSTIC_SAMPLING与KK_DIAGNOSTIC_FOOT并锁定Sampler、Schema、Program、packet capacity与transport identity
- [ ] 4.3 将Foot Capability结果写入现有Player、Run与Capture manifest和握手，不增加Foot专属Build字段或第二identity算法
- [ ] 4.4 让唯一Comparer拒绝Capability mode、Sampler Set、Schema、Program、容量或transport不一致的性能比较
- [ ] 4.5 将现有Launcher／MCP控制面接到同一Performance Build、Run、Capture和产物打开链，不建立Foot专属Player或Controller

## 5. Disabled与Capture Player硬门禁

- [ ] 5.1 构建Disabled Player并用Cecil确认业务程序集零Diagnostic custom attribute与零Sampling AssemblyRef
- [ ] 5.2 检查Disabled IL2CPP输出，确认零Foot Diagnostics／Sampling Runtime程序集、零Generated Program／Session／packet／queue／interest和零Field／Capability identity字符串
- [ ] 5.3 构建Capture Player并确认IL2CPP闭包包含匹配Schema identity的静态Capture程序与三张Table layout
- [ ] 5.4 确认关闭只由编译符号和asmdef约束完成，不存在runtime bool、空实现、Linker猜测、fallback或构建后静默删除

## 6. 自动化一致性检查

- [ ] 6.1 在Host和Analyzer迁移完成后执行受影响Runtime、Editor与Controller编译；dotnet命令使用--disable-build-servers、/nr:false和/p:UseSharedCompilation=false并立即shutdown build server
- [ ] 6.2 执行Repository Policy、git diff --check和禁止路径搜索，确认无旧Getter、Projection、Side选择、合成geometry、Column／CsvBinding、领域Writer与第二采样链
- [ ] 6.3 严格校验本change、generated diagnostic sampling framework、PoseGraph和Performance相关OpenSpec
- [ ] 6.4 完成代码与current specs对账；用户端到端验收前不归档，不把未完成Host、Analyzer、Performance或Player Gate写成已安装能力

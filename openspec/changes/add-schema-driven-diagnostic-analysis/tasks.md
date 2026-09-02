## 1. 合同对账与迁移输入

- [ ] 1.1 从`b601d933b`父提交提取旧Foot规则、阈值、窗口、分母、评分和原始字段依赖，形成逐规则迁移清单，并确认清单不包含旧Sampler、Column／CsvBinding、FootFrame DTO、Store或Publisher
- [ ] 1.2 对照当前Foot主表、ground-contacts、ground-envelope与ground-surfaces Schema给每条规则标出可直接绑定、可离线派生或缺失原始事实，确认所有缺失项都有唯一真实业务Owner而非诊断DTO方案
- [ ] 1.3 同步`add-generated-diagnostic-sampling-framework`、`extract-generated-diagnostic-sampling-package`和`refactor-foot-ik-diagnostic-sampling`的proposal、design、spec与tasks，使用OpenSpec strict validation确认不再把Host CSV当作最终诊断结论

## 2. 聚焦Annotations与生成分类

- [ ] 2.1 在独立包把`DiagnosticField`收敛为无参数采样标记，新增Conditional `DiagnosticKey`与可重复`DiagnosticGroup`，通过Annotations项目编译确认删除旧revision／unit／groups构造且没有兼容重载
- [ ] 2.2 扩展Generator descriptor收集Capability、Table、Fact Root、结构路径、Key和继承Group，使用Generator编译诊断确认重复Key、非法Group和不可读成员会失败
- [ ] 2.3 实现事实类型、结构分支和叶子Group传播以及Capability作用域隔离，通过生成Schema检查同名Combat／Foot Group不会跨Capability合并
- [ ] 2.4 把Sampler选择收敛为Group集合或`IncludeAll`并生成唯一字段闭包，通过生成输出检查Sampler不保存成员路径、Field identity、Getter或Extractor列表
- [ ] 2.5 补齐空Group、Table闭包、Dimension结构和packet容量编译诊断，通过独立Generator项目编译及生成源码检查确认错误发生在编译期
- [ ] 2.6 在Schema和manifest加入稳定Key、Group集合、Sampler字段数、Table容量与packet布局，通过Host Finalizer输出检查Schema hash覆盖新的分类闭包

## 3. 通用Artifact Dataset与Plan执行

- [ ] 3.1 在独立Host实现以Capability manifest为入口的Completed、文件hash、Schema、Sampler和Table闭包验证，通过Host项目编译和已有生成Artifact读取命令确认损坏输入返回确定错误
- [ ] 3.2 实现Schema-driven主表／子表cursor与Boolean、整数、浮点、Identity、Vector、Quaternion和availability typed读取，通过现有Foot生成CSV检查不需要领域Adapter或逐列Binding
- [ ] 3.3 实现Key／Field identity到整数typed handle的Plan绑定和Operator行循环接口，通过静态搜索确认逐行路径中不存在字符串列查找、反射或dynamic
- [ ] 3.4 实现多Capability Dataset Set及显式Run、Actor、Frame／Tick关联绑定，通过Host编译检查缺少关联字段时只能产生MissingEvidence而不能自动join
- [ ] 3.5 实现source-controlled JSON Plan模型、参数合同、窗口、过滤、Dimension和评分组合，通过Plan编译命令确认未知Operator、缺失Key／Field、类型或表基数错误被拒绝
- [ ] 3.6 实现显式Operator registry及`Passed`、`Failed`、`NotApplicable`、`MissingEvidence`四态结果，通过Host编译和规则执行输出检查MissingEvidence不计为Passed
- [ ] 3.7 实现原子`diagnosis.json`与`report.md` Writer，通过产物检查确认记录Manifest、Schema／Plan／Analyzer hash、规则证据和评分分母且不修改Capture目录
- [ ] 3.8 发布新的独立包版本并更新3C唯一package消费身份，通过两边程序集hash／MVID检查确认3C没有第二份Annotations、Generator或Host实现

## 4. Foot采样分类迁移

- [ ] 4.1 把现有Foot真实成员迁为无参数`DiagnosticField`并删除旧revision／unit／group实参，通过Foot Generator程序集编译确认没有恢复Getter、Extractor、Projection或DTO
- [ ] 4.2 为Frame关联、Final Solved Sole、Goal、Lifecycle、Contact、Anchor、Ground、Pelvis等长期关键事实声明稳定`DiagnosticKey`，通过Schema检查Key在Capability／Table／Fact Root作用域唯一
- [ ] 4.3 在可复用事实分支和必要叶子声明`core`、`landing`、`ground`、`motion`、`pelvis`与`solver-detail` Group，通过Schema检查继承结果和每组字段规模
- [ ] 4.4 定义Foot Core与Full Sampler并让现有Foot Capture Program消费生成闭包，通过生成源码检查Core只来自选择Group、Full包含全部Foot字段且两者没有逐字段列表
- [ ] 4.5 为迁移清单确认缺失的原始事实补真实成员采样声明，通过Runtime程序集编译和diff检查确认业务结果、结构布局、Commit时机与`in`直接事实根入口不改变
- [ ] 4.6 更新Foot Host workflow选择Core或Full并在Capability manifest记录实际Sampler，通过Editor程序集编译确认Stop仍只执行通用封存

## 5. Foot离线Operator迁移

- [ ] 5.1 新建独立Foot Analysis Editor程序集、Operator catalog、Plan catalog和统一分析入口，通过asmdef依赖检查确认只引用通用Host／基础序列化合同而不引用PoseGraph Runtime或Foot Capture Runtime
- [ ] 5.2 从Git历史迁移Contact Plane Penetration与Locked Sole Motion算法，通过当前typed dataset输出确认规则只使用Plan绑定handle并保留原阈值、窗口和严重度
- [ ] 5.3 迁移Landing Path Continuity与Landing State Consistency算法，通过当前typed dataset输出确认状态边界、事件窗口和MissingEvidence分支完整
- [ ] 5.4 迁移Swing Path Jitter与Step Time Candidate算法，通过当前typed dataset输出确认候选、窗口和Dimension证据可定位
- [ ] 5.5 迁移Pelvis、Leg Reach与Support相关算法，通过当前typed dataset输出确认坐标事实来自生成字段而非场景Transform反查
- [ ] 5.6 迁移Coverage、资格、分母和七维评分组合，通过`diagnosis.json`检查总分能够追溯到规则结果且MissingEvidence会标记评分不完整
- [ ] 5.7 建立当前Foot Core与Full默认Plan，通过Plan编译结果确认Core只启用现有Core Capture可完整支持的规则、Full覆盖全部已迁移Operator
- [ ] 5.8 删除迁移过程中产生的临时字段清单、旧CSV模型或中间Adapter，通过`rg`确认没有恢复`CharacterFootMotionDiagnosticAnalyzer`、`CharacterFootCsvColumn`、`CharacterFootCsvBinding`、`FootFrame`和`CharacterFootDiagnosticStore`

## 6. Foot诊断前端接入

- [ ] 6.1 在唯一Foot workflow registry增加Analyze Last、Analyze Existing、Open Last Report状态与路径，通过Editor程序集编译确认采样和分析是两个独立操作
- [ ] 6.2 接入Gameplay Launcher并只显示采样状态、当前Plan、分析状态和结果路径，通过静态检查确认`OnInspectorGUI`不解析CSV或执行Operator
- [ ] 6.3 接入Foot MCP并复用同一分析入口和状态，通过MCP合同检查确认没有第二套Reader、规则registry或报告路径
- [ ] 6.4 把固定输入回放的最小证据读取迁到通用Artifact Reader，通过Editor程序集编译和引用搜索确认不再直接维护CSV header／Field字符串解析器

## 7. 构建闭包与规格收口

- [ ] 7.1 编译独立Annotations、Generator、Runtime和Host项目，命令统一带`--disable-build-servers /nr:false /p:UseSharedCompilation=false`并在完成后执行build server shutdown，确认零编译错误
- [ ] 7.2 分别编译3C Runtime、Foot Generator、Foot Capture Editor和Foot Analysis Editor程序集，使用相同build server关闭规则确认新依赖闭包完整
- [ ] 7.3 扩展Managed／IL2CPP产物Gate覆盖`DiagnosticKey`、`DiagnosticGroup`、Analyzer、Plan、Operator和Report identity，通过Gate命令确认Disabled闭包为零且Capture闭包不包含离线Analyzer
- [ ] 7.4 对新Change及三个受影响采样Change执行OpenSpec strict validation，并用`git diff --check`和冲突文本搜索确认current specs不再宣称Foot没有独立诊断能力
- [ ] 7.5 对照proposal列出的全部Capability和受影响规格检查实现归属，通过依赖搜索确认通用包没有Foot／Combat概念、Foot Analysis没有PoseGraph运行引用且旧诊断基础设施保持删除

## 1. 合同对账与迁移输入

- [x] 1.1 从`b601d933b`父提交提取旧Foot规则、阈值、窗口、分母、评分和原始字段依赖，形成逐规则迁移清单，并确认清单不包含旧Sampler、Column／CsvBinding、FootFrame DTO、Store或Publisher
- [x] 1.2 对照当前Foot主表、ground-contacts、ground-envelope与ground-surfaces Schema给每条规则标出可直接绑定、可离线派生或缺失原始事实，确认所有缺失项都有唯一真实业务Owner而非诊断DTO方案
- [x] 1.3 同步`add-generated-diagnostic-sampling-framework`、`extract-generated-diagnostic-sampling-package`和`refactor-foot-ik-diagnostic-sampling`的proposal、design、spec与tasks，使用OpenSpec strict validation确认不再把Host CSV当作最终诊断结论

## 2. 聚焦Annotations与生成分类

- [x] 2.1 在独立包把`DiagnosticField`收敛为无参数采样标记，新增Conditional `DiagnosticKey`、可重复`DiagnosticGroup`与独立`DiagnosticAvailability`，通过Annotations项目编译确认删除旧revision／unit／groups／availability构造且没有兼容重载
- [x] 2.2 扩展Generator descriptor收集Capability、Table、Fact Root、结构路径、Key、继承Group和Availability合同，使用Generator编译诊断确认重复Key、非法Group、非法Availability引用和不可读成员会失败
- [x] 2.3 实现事实类型、结构分支和叶子Group传播以及Capability作用域隔离，通过生成Schema检查同名Combat／Foot Group不会跨Capability合并
- [x] 2.4 把Sampler选择收敛为Group集合或`IncludeAll`并生成唯一字段闭包，通过生成输出检查Sampler不保存成员路径、Field identity、Getter或Extractor列表
- [x] 2.5 补齐空Group、Table闭包、Dimension结构和packet容量编译诊断，通过独立Generator项目编译及生成源码检查确认错误发生在编译期
- [x] 2.6 在Schema和manifest加入稳定Key、Group集合、Sampler字段数、Table容量与packet布局，通过Host Finalizer输出检查Schema hash覆盖新的分类闭包
- [x] 2.7 新增`DiagnosticEvent` partial方法发现、目标隔离typed dispatcher contract、可选partial interest Query和下游Program handler绑定，使用跨程序集Probe确认普通Event不强制Query、昂贵事实Owner可查询目标、业务只有一行Event调用、参数为目标／真实lineage／现有`in`事实、Metadata在Start冻结、无匹配目标订阅立即返回、Disabled编译消除Event／Query调用及参数求值且没有Event DTO／通用Event Bus

## 3. 通用Artifact Dataset与Plan执行

- [x] 3.1 在独立Host实现以Capability manifest为入口的Completed、文件hash、Schema、Sampler和Table闭包验证，通过Host项目编译和已有生成Artifact读取命令确认损坏输入返回确定错误
- [x] 3.2 实现Schema-driven主表／子表cursor与Boolean、整数、浮点、Identity、Vector、Quaternion和availability typed读取，通过现有Foot生成CSV检查不需要领域Adapter或逐列Binding
- [x] 3.3 实现Key／Field identity到整数typed handle的Plan绑定和Operator行循环接口，通过静态搜索确认逐行路径中不存在字符串列查找、反射或dynamic
- [x] 3.4 实现多Capability Dataset Set及显式Run、Actor、Frame／Tick关联绑定，通过Host编译检查缺少关联字段时只能产生MissingEvidence而不能自动join
- [x] 3.5 实现source-controlled JSON Plan模型、参数合同、窗口、过滤、Dimension和评分组合，通过Plan编译命令确认未知Operator、缺失Key／Field、类型或表基数错误被拒绝
- [x] 3.6 实现显式Operator registry及`Passed`、`Failed`、`NotApplicable`、`MissingEvidence`四态结果，通过Host编译和规则执行输出检查MissingEvidence不计为Passed
- [x] 3.7 实现原子`diagnosis.json`与`report.md` Writer，通过产物检查确认记录Manifest、Schema／Plan／Analyzer hash、规则证据和评分分母且不修改Capture目录
- [x] 3.8 发布独立包0.5.2并更新3C唯一package消费身份，通过Owner提交`e43af24`、Analyzer SHA-256 `47EE5F876377EBE453E98009E5AEA6D95FECC8DC4491B1A9EBE881812C55AA87`与MVID `9b3d5a64-d7e9-46c4-a687-52e87a5bc84a`确认3C没有第二份Annotations、Generator或Host实现；0.5.2保持Event ABI并补全不完整评分固定权重与上下界输出

## 4. Foot采样分类迁移

- [x] 4.1 把现有Foot真实成员迁为无参数`DiagnosticField`并删除旧revision／unit／group实参，通过Foot Generator程序集编译确认没有恢复Getter、Extractor、Projection或DTO
- [x] 4.2 为Frame关联、Final Solved Sole、Goal、Lifecycle、Contact、Anchor、Ground、Pelvis及Physical Ankle等长期关键事实声明稳定`DiagnosticKey`，通过Schema检查Key在Capability／Table／Fact Root作用域唯一
- [x] 4.3 在可复用事实分支和必要叶子声明`motion-core`、`lifecycle`、`ground-*`、`pelvis-*`、`solver-*`等领域内Group，通过Schema检查继承结果、Capability隔离和每组字段规模
- [x] 4.4 定义独立Foot Core／Full Sampler与Program，通过生成源码检查Core只选择`body-correction`、`lifecycle`、`motion-core`、`physical`、`resolved-core`、`timing` Group，Full包含全部Foot字段且两者没有逐字段列表
- [x] 4.5 为迁移清单首批Operator在Foot Result与Final Physical诊断页补Source Ankle／Physical Ankle真实成员采样声明，在成功Seal后的唯一业务Commit点声明并调用一行Foot `DiagnosticEvent` partial触发方法，删除`ICharacterFootIkCommittedCaptureConsumer`、Capture Binding和手写`TryCapture`转发，通过Capture／Disabled Runtime程序集编译和diff检查确认业务求解结果与Commit时机不改变、Physical事实只在匹配interest时冻结
- [x] 4.6 更新Foot Host workflow选择Core或Full并在Capability manifest记录实际Sampler，通过Editor程序集编译确认Stop仍只执行通用封存

## 5. Foot离线Operator迁移

- [x] 5.1 新建独立Foot Analysis Editor程序集、Operator catalog、Plan catalog和统一分析入口，通过asmdef依赖检查确认只引用通用Host／基础序列化合同而不引用PoseGraph Runtime或Foot Capture Runtime
- [x] 5.2 从Git历史迁移Contact Plane Penetration与Locked Sole Motion算法，通过当前typed dataset输出确认规则只使用Plan绑定handle、从Source／Physical Ankle刚体重建Heel／Toe并保留原阈值、窗口和严重度
- [x] 5.3 迁移Landing Path Continuity与Landing State Consistency算法，通过当前typed dataset输出确认状态边界、事件窗口和MissingEvidence分支完整
- [x] 5.4 迁移Swing Path Jitter与当前Formal Step Time Selection算法，通过当前typed dataset输出确认选择、窗口和Dimension证据可定位，并删除已不存在的Current／Incoming候选假字段
- [x] 5.5 迁移Pelvis、Leg Reach与Support相关算法，通过当前typed dataset输出确认坐标事实来自生成字段而非场景Transform反查
- [ ] 5.6 迁移Coverage、资格、分母和七维评分组合，通过`diagnosis.json`检查总分能够追溯到规则结果且MissingEvidence会标记评分不完整
- [x] 5.7 建立当前Foot Core与Full默认Plan，通过Plan编译结果确认Core只启用现有Core Capture可完整支持的规则、Full覆盖全部已迁移Operator
- [x] 5.8 删除迁移过程中产生的临时字段清单、旧CSV模型或中间Adapter，通过`rg`确认没有恢复`CharacterFootMotionDiagnosticAnalyzer`、`CharacterFootCsvColumn`、`CharacterFootCsvBinding`、`FootFrame`和`CharacterFootDiagnosticStore`

## 6. Foot诊断前端接入

- [x] 6.1 在独立analysis workflow registry增加Analyze Last、Analyze Existing、Open Last Report状态与路径，并由Foot Editor workflow显式调用统一Analyzer，确认Stop只封存采样、分析写入Capture目录外的独立结果目录
- [x] 6.2 接入Gameplay Launcher的Analyze Last、Analyze Existing与Open Last Report按钮，只显示分析状态、失败和结果路径，确认绘制回调不解析CSV或执行Operator
- [ ] 6.3 接入Foot MCP并复用同一分析入口和状态，通过MCP合同检查确认没有第二套Reader、规则registry或报告路径
- [ ] 6.4 把固定输入回放的最小证据读取迁到通用Artifact Reader，通过Editor程序集编译和引用搜索确认不再直接维护CSV header／Field字符串解析器

## 7. 构建闭包与规格收口

- [x] 7.1 编译独立Annotations、Generator、Runtime和Host项目，命令统一带`--disable-build-servers /nr:false /p:UseSharedCompilation=false`并在完成后执行build server shutdown，确认零编译错误
- [x] 7.2 分别编译3C Runtime、Foot Generator、Foot Capture Editor和Foot Analysis Editor程序集，使用相同build server关闭规则确认新依赖闭包完整
- [ ] 7.3 扩展Managed／IL2CPP产物Gate覆盖`DiagnosticKey`、`DiagnosticGroup`、Analyzer、Plan、Operator和Report identity，通过Gate命令确认Disabled闭包为零且Capture闭包不包含离线Analyzer
- [ ] 7.4 对新Change及三个受影响采样Change执行OpenSpec strict validation，并用`git diff --check`和冲突文本搜索确认current specs不再宣称Foot没有独立诊断能力
- [ ] 7.5 对照proposal列出的全部Capability和受影响规格检查实现归属，通过依赖搜索确认通用包没有Foot／Combat概念、Foot Analysis没有PoseGraph运行引用且旧诊断基础设施保持删除

# schema-driven-diagnostic-analysis Specification

## Purpose
建立跨战斗、IK、网络和动画业务复用的Schema-driven离线诊断合同，让真实字段只声明一次采样、稳定Key与Group信息，当前诊断Plan可以在不修改Player采样热路径的情况下动态组合typed Operator并产生可审查结论。

## Requirements

### Requirement: 字段分类必须由聚焦特性单点声明

系统 MUST使用无参数`DiagnosticField`标记允许采样的真实只读成员，并分别使用可选`DiagnosticKey`、可重复`DiagnosticGroup`和可选`DiagnosticAvailability`声明稳定关键身份、采样集合与有效条件。`DiagnosticField` MUST不保存人工version、unit、group、availability、阈值、评分、报告或诊断规则；无额外语义的普通字段 MUST只需要`DiagnosticField`。Generator MUST从C#类型推断codec，从Fact Root、成员路径与Table推导结构身份。

#### Scenario: 普通调查字段接入

- **WHEN** 消费方在现有真实只读成员上只声明`DiagnosticField`
- **THEN** Generator MUST把该成员加入所属Capability的结构Schema和Full选择
- **AND** 消费方 MUST不再声明Getter、Extractor、Projection、Column或CSV Binding

#### Scenario: 关键字段接入

- **WHEN** 一个长期诊断事实额外声明稳定`DiagnosticKey`和一个或多个`DiagnosticGroup`
- **THEN** Generator MUST在所属Capability和Fact Root作用域生成唯一Key与Group成员关系
- **AND** Key MUST不隐式决定该字段属于Core、Full或任一其它Sampler

#### Scenario: 条件有效字段接入

- **WHEN** 一个真实字段只在同一事实分支的可读状态或另一个稳定字段满足确定值时有效
- **THEN** 消费方 MUST用独立`DiagnosticAvailability`声明该条件且Generator MUST编译对应直接读取
- **AND** `DiagnosticField` MUST保持无参数，运行Capture MUST不通过反射或字符串路径求值有效性

### Requirement: Capability必须形成唯一业务分组边界

战斗、IK、网络、动画等不同业务 MUST分别拥有独立Capability。每个Capability MUST只通过Fact Root声明本业务可提供的现有强类型事实；Generator MUST递归发现根下带`DiagnosticField`的成员。系统 MUST不要求每个字段重复声明Capability，不得为跨业务采样构造组合DTO、万能View或公共字段Bank。

#### Scenario: 同一Capture Run选择IK与战斗

- **WHEN** Capture workflow同时选择Foot IK Sampler与Combat Sampler
- **THEN** 两个Capability MUST在各自Commit边界提交独立typed packet并共享Run和调用方显式提供的关联身份
- **AND** 系统 MUST不创建CombatAndFoot组合事实或让任一Capability读取另一业务对象

### Requirement: 采样时机必须由业务事件触发点声明

业务 MUST在自己选择的同步Commit边界声明一个带`DiagnosticEvent`的private static partial void方法，并以目标、现有真实lineage和多个现有Fact Root的`in`参数表达本次事实。业务执行路径 MUST只调用该方法一次，不得手写Session、packet、左右循环、Sampler switch或Capture Bridge。Generator MUST为Capture构建生成目标隔离的typed dispatcher，并把匹配Program的generated handler绑定到Event；Session Start／Stop仍由外部Capture workflow控制，Capture Metadata MUST只在Start时提供并冻结，不得作为逐帧Event参数。只有需要按订阅延迟准备昂贵事实的Owner MAY声明约定private partial Query，Generator MUST不强制普通Event增加Query。

#### Scenario: Foot在Post-Commit触发采样

- **WHEN** Foot业务把带`DiagnosticEvent`的partial方法调用放在成功Seal后的同步Commit调用栈
- **THEN** 有匹配目标Session订阅时generated handler MUST使用调用方提供的真实lineage，按Program Dimension和Fact Root闭包采集本次现有事实
- **AND** 业务方法 MUST不构造CommittedSample Event DTO、Dimension View或逐字段副本

#### Scenario: 只准备匹配目标的延迟诊断事实

- **WHEN** Host只为一个运行目标启动Session且昂贵事实Owner在帧开始调用可选partial interest Query
- **THEN** 只有匹配目标 MUST准备允许延迟冻结的诊断页
- **AND** 其它目标与没有订阅的帧 MUST不构造诊断事实或执行额外坐标变换

#### Scenario: 业务选择另一个采样时机

- **WHEN** Combat Capability把自己的`DiagnosticEvent`调用放在Damage Commit而不是动画Frame Commit
- **THEN** Generator MUST按Combat Event和Program合同生成独立调用图
- **AND** 通用框架 MUST不假设所有Capability共享Update、Frame或Tick时机

#### Scenario: Disabled构建触发调用消失

- **WHEN** 构建未定义`KK_DIAGNOSTIC_SAMPLING`且Generator不生成partial方法实现
- **THEN** C#编译器 MUST消除该partial调用及其参数求值
- **AND** 业务程序集 MUST不保留Event dispatcher、partial interest Query、Sampling Runtime引用或Event identity

#### Scenario: Capture处理失败

- **WHEN** generated Event handler在rent、字段读取、submit或Writer路径失败
- **THEN** 对应Capture Session MUST进入Faulted并停止处理该Event
- **AND** 异常 MUST不逃逸到业务Commit或改变已提交业务结果

### Requirement: Group必须在事实声明处继承并由Sampler选择

`DiagnosticGroup` MUST可以声明在Fact Root可达的事实类型、结构分支或叶子字段上；Generator MUST把父级Group传播到可达的已标记叶子，并合并叶子自己的Group。Sampler MUST只选择Group identity或显式`IncludeAll`，不得再次列出字段Getter、成员路径或Field identity。Group选择 MUST在编译期拒绝不存在或为空的Group、重复Key、非法Table闭包、左右Dimension结构不一致和超出声明容量的packet布局。

#### Scenario: 结构分支加入Landing组

- **WHEN** Landing事实分支声明`DiagnosticGroup("landing")`并新增一个`DiagnosticField`叶子
- **THEN** 选择landing组的所有Sampler MUST自动包含该叶子
- **AND** Sampler定义 MUST不因新增叶子而修改

#### Scenario: Full选择

- **WHEN** Sampler声明`IncludeAll`
- **THEN** Generator MUST包含Capability全部可达`DiagnosticField`与所需Table结构
- **AND** 显式Group变化 MUST不使字段从Full中消失

### Requirement: Artifact Reader必须只读取已完成的当前Schema产物

通用Artifact Reader MUST以一个或多个`capability.manifest.json`为入口，验证Completed状态、文件hash、Schema hash、Sampler identity、主表和子表闭包后暴露当前Schema的typed dataset。Reader MUST按Schema而非领域Adapter解析Boolean、整数、浮点、Identity、Vector、Quaternion、availability、Dimension、Frame／Tick和Table行；MUST不引用业务Fact Root、运行Session、packet或旧CSV格式。

#### Scenario: 读取完整Capture

- **WHEN** 调用方提供Completed且hash与Schema闭合的Capability manifest
- **THEN** Reader MUST按当前Schema返回可枚举主行、子表行、Field目录和关联身份
- **AND** Foot、Combat或其它领域 MUST不提供Host Adapter或逐列Binding

#### Scenario: 产物不完整

- **WHEN** Manifest未完成、文件缺失、hash不匹配或Schema声明的Table不存在
- **THEN** Reader MUST拒绝整个输入并返回确定的结构错误
- **AND** MUST不把缺失文件降级为无数据或尝试旧Schema Reader

### Requirement: Plan必须动态绑定当前Schema而不维护兼容链

诊断Plan MUST只声明Operator、typed输入槽到当前Key或Field identity的绑定、参数、窗口、过滤、规则组合和评分组合。Plan装载时 MUST针对选中dataset重新解析全部绑定；无关字段新增或删除 MUST不影响Plan，必需Key／Field缺失、类型或表基数不匹配 MUST阻止对应规则给出Passed或Failed。系统 MUST不迁移旧Plan、不保存Field alias、不按相似名称猜测、不对旧Schema提供fallback。

#### Scenario: 当前Schema新增无关字段

- **WHEN** 新Capture Schema只增加当前Plan未绑定的字段
- **THEN** 当前Plan MUST继续通过绑定检查
- **AND** 诊断结果 MUST只记录实际消费的输入证据

#### Scenario: 必需字段变化

- **WHEN** 当前Plan绑定的Key或Field在最新Capture中不存在或类型改变
- **THEN** 对应规则 MUST产生`MissingEvidence`或Plan绑定错误
- **AND** 系统 MUST不选择另一个名称相似或类型相同的字段替代

### Requirement: Operator必须保存稳定算法而不是解释任意脚本

每个Operator MUST通过显式注册声明identity、typed输入槽、表基数、适用条件、参数合同和结果类别，并只读取Plan已经绑定的dataset输入。Operator MUST不控制Capture、不查询业务世界、不访问Unity Runtime对象，也不得通过表达式树、反射、dynamic或任意脚本执行诊断公式。阈值、窗口、过滤和组合 MAY由Plan改变；新的计算逻辑 MUST通过新的或修改后的Operator实现。

#### Scenario: 同一算法使用不同阈值

- **WHEN** 两个当前Plan选择同一Operator但提供不同合法阈值与窗口
- **THEN** Analyzer MUST使用同一Operator代码分别执行两个Plan
- **AND** Sampling Capability和Schema MUST不因阈值变化而重新定义

#### Scenario: Plan请求未知Operator

- **WHEN** Plan引用当前Analyzer没有显式注册的Operator identity
- **THEN** Analyzer MUST拒绝该Plan
- **AND** MUST不从字符串加载类型、程序集或脚本作为替代

### Requirement: 规则结果必须区分结论与证据缺失

每次规则执行 MUST产生`Passed`、`Failed`、`NotApplicable`或`MissingEvidence`之一。`Passed`与`Failed`只能在全部必需输入和适用窗口完整时产生；业务条件不适用 MUST产生`NotApplicable`；采样字段、行、availability或窗口证据缺失 MUST产生`MissingEvidence`。报告 MUST分别统计四种结果，不得把后三者合并为“没有发现问题”。

#### Scenario: 场景不适用

- **WHEN** 锁脚滑动规则获得完整证据但选中窗口从未进入锁脚状态
- **THEN** 规则 MUST产生`NotApplicable`
- **AND** MUST不产生Passed或MissingEvidence

#### Scenario: 锁脚位置缺失

- **WHEN** 窗口包含锁脚状态但实际脚位置字段不可用
- **THEN** 规则 MUST产生`MissingEvidence`
- **AND** 报告 MUST指出缺失输入和受影响窗口

### Requirement: 离线诊断结果必须独立且不得进入Player

Analyzer MUST只在Editor／Host边界执行，并输出机器可读诊断结果和人类可读报告。结果 MUST记录输入Manifest、当前Schema hash、Plan内容hash、Analyzer binary hash、规则identity、Dimension、Frame／Tick范围、严重度和证据；MUST不回写Capture manifest、不修改业务状态、不建立运行时Publisher或第二采样状态机。普通Player和Capture Player MUST都不包含Analyzer、Plan编辑、Report或领域离线规则程序集。

#### Scenario: 完成离线分析

- **WHEN** 当前Plan在完整dataset上完成执行
- **THEN** Host MUST原子发布诊断结果与报告，并保持原始Capture目录内容不变
- **AND** 后续修改Plan MUST通过一次新的显式分析产生新的结果目录

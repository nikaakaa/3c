# character-foot-diagnostic-analysis Specification

## Purpose
恢复并模块化Foot采样之后的真实诊断能力，让穿地、脚滑、Landing、Swing、Step Time、Pelvis与Reach问题可以从最新生成Artifact离线判断，同时保持采样框架、PoseGraph运行时和领域规则相互独立。

## Requirements

### Requirement: Foot诊断必须只消费生成Artifact

Foot诊断 MUST以Completed的`character-foot-ik` Capability manifest及其当前Schema、主表、ground-contacts、ground-envelope和ground-surfaces表为唯一输入。它 MUST不引用PoseGraph Runtime、Foot Context、Fact Root实例、Generated Capture、Session、packet或场景Transform，也不得重采样、执行world query或调用FBBIK。

#### Scenario: 分析最近一次Foot Capture

- **WHEN** 用户选择最近一次Completed Foot Capability manifest和当前Foot Plan
- **THEN** Foot诊断 MUST只通过通用Artifact Reader取得typed dataset并执行规则
- **AND** 采样结束前或Manifest未完成时 MUST拒绝启动分析

### Requirement: Foot采样必须由唯一Post-Commit事件一行触发

Foot业务 MUST在成功Seal后的现有同步Commit位置声明并调用唯一Foot `DiagnosticEvent` partial触发点。触发点 MUST直接接收目标、当前真实lineage、Left／Right各Fact Root和共享事实的`in`参数；Generator MUST把它绑定到当前Foot Core／Full Program。Capture Metadata MUST由Foot Host workflow在Session Start时冻结，不得由Commit逐帧传递。Foot Runtime MUST不保留`ICharacterFootIkCommittedCaptureConsumer`、Capture Binding、手写`TryCapture`参数转发或诊断Event DTO。

#### Scenario: Foot Capture Session已订阅

- **WHEN** 当前Foot Core或Full Session已经订阅Post-Commit Event且本帧成功Seal
- **THEN** 业务 MUST执行一行Event触发并由generated handler采集左右Dimension
- **AND** 调用方 MUST不选择Sampler、租packet或捕获采样异常

#### Scenario: 没有Foot Capture Session

- **WHEN** Capture构建中没有活动Foot Session订阅Event
- **THEN** generated dispatcher MUST立即返回且不访问标记字段
- **AND** 业务方传入参数 MUST只引用已经存在的Committed事实，不得为无订阅路径执行额外坐标变换或诊断DTO构造

#### Scenario: Foot Session只采集选定角色

- **WHEN** Foot Host workflow只为一个目标启动Session
- **THEN** generated target interest MUST只要求该目标准备Foot诊断页并只接收该目标Event
- **AND** 其它角色 MUST不因该Session增加诊断事实冻结成本

### Requirement: Foot规则必须从旧单体迁为独立Operator

Foot诊断 MUST提供穿地、锁脚滑动、Landing路径连续性、Landing状态一致性、Swing路径抖动、Step Time候选、Pelvis／Reach和既有七维质量计算所需的独立Operator。迁移 MUST保留仍适用于当前业务事实的公式、阈值、窗口、分母与严重度语义；旧Sampler、诊断DTO、Column／CsvBinding、合成Geometry、单体Analyzer、Diagnosis Store和运行时Publisher MUST不恢复。

#### Scenario: 执行完整Foot诊断Plan

- **WHEN** 当前Foot Full dataset满足默认完整Plan的全部输入
- **THEN** Analyzer MUST执行全部适用Foot Operator并产生问题明细、覆盖率和七维结果
- **AND** 任一Operator MUST不通过旧FootFrame DTO或旧CSV列号读取输入

#### Scenario: 当前规则依赖的原始事实缺失

- **WHEN** 从旧诊断算法迁移出的Operator在当前Schema中找不到等价原始事实
- **THEN** 该Operator MUST保持未完成并明确列出缺失证据
- **AND** 实施 MUST不使用近似字段、默认值或旧派生CSV列伪造等价

### Requirement: Foot Plan必须允许当前测试目的动态变化

系统 MUST提供当前Foot Core与Full默认Plan，并允许用户基于当前Schema选择Operator、Key／Field绑定、阈值、窗口、状态过滤、左右脚范围和评分组合。Plan变化 MUST不修改Foot Runtime、Sampler字段或Operator算法；当前Plan失效时 MUST直接更新或删除，不维护旧Plan兼容。

#### Scenario: 调整锁脚滑动阈值

- **WHEN** 用户只修改当前Plan的最大位移或持续窗口
- **THEN** 系统 MUST用同一Capture和同一锁脚Operator重新分析
- **AND** MUST不重新构建Player或修改采样字段

#### Scenario: 新诊断需要未采字段

- **WHEN** 新Plan需要的事实没有出现在本次Capture Schema
- **THEN** 系统 MUST报告需要新增或选择的`DiagnosticField`证据
- **AND** MUST不声称可以从现有CSV推导缺失事实

### Requirement: Foot前端必须把采样与分析作为两个显式动作

Foot诊断前端 MUST提供分析最近一次Capture、分析指定Capture和打开最近报告的显式入口，并与现有采样Start／Stop共享唯一workflow状态和路径真相。Stop MUST只封存采样；Analyze MUST在封存成功后独立执行。Launcher与MCP MUST调用同一前端服务，不得各自解析CSV或实现第二套规则。

#### Scenario: 停止采样

- **WHEN** 用户停止一次Foot Capture
- **THEN** workflow MUST完成通用Host封存并显示可分析的Manifest
- **AND** MUST不在Stop或Inspector绘制回调中隐式执行完整诊断

#### Scenario: 分析指定Capture

- **WHEN** 用户从Launcher或MCP选择一个合法Foot Manifest和Plan
- **THEN** 两个控制面 MUST启动同一个离线分析操作并返回同一结果路径
- **AND** 分析失败 MUST保留原始Capture并报告确定错误

### Requirement: Foot报告必须提供可定位证据而不是只有总分

Foot诊断结果 MUST按Contact、Landing、Motion、Pelvis、Coverage和Score组织Finding。每个Failed或MissingEvidence结果 MUST包含Rule identity、左右Dimension、Frame范围、关键输入值与来源Field identity；七维评分 MUST同时报告资格、分母、命中与未适用原因。总分 MUST不掩盖MissingEvidence或未覆盖规则。

#### Scenario: 报告穿地问题

- **WHEN** Contact Penetration Operator在完整窗口中检测到超过当前Plan阈值的穿地
- **THEN** 报告 MUST包含对应脚、Frame范围、Sole／Contact Plane证据和超出量
- **AND** Score摘要 MUST能追溯到该Finding而不是只输出一个数字

### Requirement: Foot离线诊断不得改变发布构建闭包

Foot Analyzer、Plan、Operator和Report程序集 MUST只存在于Editor／Host。Capture Player MAY包含被选中的Foot Core或Full生成采样程序，但 MUST不包含离线诊断规则；Disabled Player MUST继续同时排除Foot采样与离线诊断闭包。

#### Scenario: 构建普通发布Player

- **WHEN** 构建未定义Foot诊断采样符号的普通Player
- **THEN** 产物 MUST不包含Foot Capture、Analyzer、Plan、Operator、Report或其identity字符串
- **AND** 业务运行程序集 MUST不因离线诊断新增引用

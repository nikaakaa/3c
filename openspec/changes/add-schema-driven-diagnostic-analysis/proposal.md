## Why

现有生成采样链已经能把多个业务事实根编译为AOT Capture、Schema和CSV，但Foot旧Analyzer在迁移中被直接删除，导致系统只能采集证据，不能再给出穿地、脚滑、Landing连续性等诊断结论。同时，把revision、unit、group等持续塞进`DiagnosticField`或让Sampler再次列出字段，都会让战斗、IK、网络和动画接入后形成两份字段真相，无法承受字段与测试方案的持续变化。

本change建立独立于Player采样热路径的Schema-driven离线诊断能力：字段旁只保存必要的采样、稳定Key和Group声明，Capability划分业务，Sampler按Group选择而不逐字段映射；Host读取最新采样Schema，动态Plan绑定输入并组合稳定Operator。Foot作为首个领域消费者从Git历史迁移诊断规则，但不恢复旧Sampler、Column／CsvBinding、诊断DTO和一万行Analyzer单体。

## What Changes

- **BREAKING**：把现有`DiagnosticField(revision, unit, groups)`收敛为纯字段标记；稳定关键身份与采样分组分别使用小而独立的`DiagnosticKey`和可继承、可重复的`DiagnosticGroup`，不在字段Attribute保存人工版本、阈值、评分或报告信息。
- Capability成为战斗、IK、网络、动画等业务采样的唯一边界。Generator从Capability的Fact Root递归发现字段；一个真实成员只声明一次，不在Sampler、Host或Analyzer维护第二份字段清单。
- Generator把Fact Root、成员层级、Table以及显式Group编译进Schema；Sampler只选择Group或`IncludeAll`，编译期展开字段并检查空组、重复Key、非法类型、Table闭包、左右Dimension结构和packet容量。
- 提供通用Schema-driven Artifact Reader，以`capability.manifest.json`为唯一入口，验证Completed状态、hash和Schema后统一读取主表与子表，按当前Schema解析类型、Dimension、Frame／Tick和Table关联，不要求每个Capability编写CSV Binding或Reader Adapter。
- 提供Editor／Host-only动态诊断Plan。Plan只保存Operator选择、输入Key或当前Field identity绑定、阈值、窗口、过滤、规则组合与评分参数；字段和测试变化后只维护当前Plan，不迁移旧Plan、不猜测近似字段、不建立alias或兼容Schema。
- 提供显式Operator注册和执行合同。Operator声明typed输入槽、表基数与适用条件，输出`Passed`、`Failed`、`NotApplicable`或`MissingEvidence`及结构化证据；Plan不得成为任意表达式、反射或脚本执行引擎。
- 提供通用诊断结果与报告输出，记录当前采样Schema hash、Plan内容hash、Analyzer binary hash、规则结果、Frame／Tick范围、Dimension、严重度和证据；不建立旧Diagnosis Store、第二采样Manifest或运行时Publisher。
- 新增Foot离线诊断消费者，从`b601d933b`父提交提取穿地、锁脚滑动、Landing连续性／状态、Swing抖动、Step Time、Pelvis／Reach及七维评分的业务算法，重写为Operator与当前默认Plan；只读取生成Artifact，不引用PoseGraph、运行Fact Root、Session或packet。
- Foot Launcher、固定输入回放和MCP通过同一个诊断前端分析最近一次或指定的Completed Capture；采样停止与诊断执行保持两个显式动作，不在`OnInspectorGUI`或Player主线程运行重分析。
- 普通发布构建继续通过编译闭包剥离Sampling Runtime、领域Capture与Annotations；离线Analyzer、Plan和Report程序集只存在于Editor／Host，不进入Player。

## Capabilities

### New Capabilities

- `schema-driven-diagnostic-analysis`: 定义聚焦的Field／Key／Group分类、Capability业务边界、Group-based Sampler、Schema-driven Artifact Reader、动态Plan、typed Operator、四态结果和Host-only报告合同。
- `character-foot-diagnostic-analysis`: 定义Foot当前Schema上的穿地、脚滑、Landing、Swing、Step Time、Pelvis／Reach与评分诊断，以及默认Plan、Launcher／MCP入口和生成Artifact依赖边界。

### Modified Capabilities

- `character-foot-placement-presentation`: Foot运行时仍只提交已完成事实，但生成Artifact现在允许被独立离线Foot诊断器消费；“删除旧Analyzer”不再等于删除诊断业务能力。
- `character-animation-pipeline`: PoseGraph仍不拥有Analyzer、Plan或报告，但成功封存的生成Artifact可以在运行时事务之外进入独立离线诊断链。
- `character-pose-graph-runtime-architecture`: 明确独立离线诊断器不是PoseGraph维护的下游运行合同，不恢复Capture View、DTO、Bridge、Host Adapter或第二事实页。

## Impact

- Affected package: 独立`D:/Unity_Project_1/generated-diagnostic-sampling`的Annotations、Generator与Host API，新增聚焦Key／Group元数据、Group展开和通用Artifact Reader，不加入任何Foot或Combat概念。
- Affected 3C runtime definitions: Foot Capability／Fact Root／Field／Sampler／Program声明；业务计算、事实结构和内存布局不改变。
- Affected 3C editor tooling: 新增独立Foot诊断Core、当前Plan、报告Writer、Launcher／MCP前端；从Git历史迁移算法而非恢复旧基础设施。
- Affected active changes: `add-generated-diagnostic-sampling-framework`、`extract-generated-diagnostic-sampling-package`和`refactor-foot-ik-diagnostic-sampling`必须同步删除“通用Host产物即最终Foot诊断输出”口径，并保持单一生成采样链。
- Affected builds: Unity Editor、Windows x64 IL2CPP Capture Player与实机Capture；Analyzer始终Host-only，Disabled Player闭包要求不变。

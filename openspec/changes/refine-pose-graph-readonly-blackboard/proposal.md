# Change: 整理PoseGraph只读Blackboard与输入范围

## Why

当前PoseGraph把动画输入和动画属性混放在参数声明中，原生Blackboard又全部投影，作者难以分清值从哪里来、哪些图能读取、哪些是Body内部曲线。用户要求将这项工作独立实施，不能继续混在运行架构旧任务清单里。

本change从`refactor-character-pose-graph-architecture`移入原25.1—25.12及Decision 26，只负责PoseGraph读取和作者范围；原运行基础已经独立归档，其它运行架构剩余任务仍归旧change。

## What Changes

- 区分动画实例变量、外部只读表现事实、子图公开输入、随Pose传播的曲线与节点/资源配置。
- 为Root、StatePose、Subgraph和Linked Pose入口定义正式输入可见范围；Blackboard只读投影可访问声明，合法未使用声明仍可找到和拖出Get。
- Get绑定同一正式变量身份、类型和实例来源，PoseGraph主图不提供共享动画变量Set。作者名称与内部稳定身份分离，重命名不破坏引用。
- **BREAKING**：删除动画属性向每张PoseGraph复制声明的路径；编译完整收集正式输入与曲线依赖，保留曲线混合、惯性响应和最终BlendShape写入。
- **BREAKING**：为Body内部FootPlacement权重建立明确的输入Pose曲线绑定或真实公开控制输入；读取链完成后删除废弃根图Get、透传端口及确认无引用的重复子图。
- 同步共享Capability、Document、Mutation、Validator、Compiler source map与只读观察，沿用唯一作者和运行链。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `character-presentation-pose-graph`：补足只读Blackboard的输入作用范围，以及变量与source-local曲线分别读取、编译和传播的合同。

## Impact

- 作者：CharacterPoseCanvasGraph、Get拖拽入口、节点Inspector、图与子图引用。
- 数据与编译：参数/曲线声明、CharacterAnimationInputContract、属性导入、输入和曲线布局、typed读取与诊断。
- 运行：只调整Pose消费者读取来源；保留状态机、Source采样、Blend、Slot、Foot、FBBIK、Final Publication和既有曲线数学。
- 资产：在正式Document/Mutation链中迁移确认废弃的声明、端口和重复子图；不覆盖用户未提交修改。
- 文档：本change唯一持有原25.x范围；[旧架构change](../refactor-character-pose-graph-architecture/proposal.md)不再重复维护对应任务、设计正文或delta。

## Dependencies And Boundaries

- EventGraph正式规划由独立窗口维护，见[事件图方案](../../../docs/flowcanvas-event-graph-plan-2026-09-12.md)。分离时该方案为待用户确认的r1，原生运行与编译运行的选择、变量身份和帧交接尚未定稿；本change不代替该窗口裁决或实现更新图。
- 动画变量Get依赖EventGraph确认后的正式变量输出合同。不得以临时接口、第二变量布局、固定motor参数补值或另一套更新器绕过该依赖。
- 曲线来源分类、已有作者显示和Body内部依赖可以在其自身合同内推进，但不能把只隐藏UI描述为完整迁移，也不能把未完成接口从本change范围中删掉。
- Foot权重继续只控制既有Goal可见权重，不释放Anchor、清空历史或改变Landing Reach准入。
- 不新增测试或验证任务。实现与文档任务只列在tasks.md，实际执行和检查结果另写execution.md。

## Current Spec Comparison

- 现行`character-presentation-pose-graph`要求稳定ParameterId、类型、默认值、允许来源、committed输入和source-local曲线传播。本change保持这些边界，进一步明确作者列表与读取的作用范围，不把曲线改成共享可写变量。
- project.md现行Blackboard只读描述的是当前Pose消费表面；不等于禁止EventGraph作者变量Set。本change只更新Pose读取侧，事件执行合同仍由独立规划确认。
- 现行BlendSpace样本参数聚合和Inertialization响应数学保持不变。若后续实现必须改变它们的正式合同，应明确扩充对应delta，不能以本次任务分离视为已授权算法变更。
- 本change不恢复旧Canvas、旧Program或第二IK链，也不接管旧架构change中的Compiler质量、Preview、TrainingEnemy退役或全图资源复用任务。

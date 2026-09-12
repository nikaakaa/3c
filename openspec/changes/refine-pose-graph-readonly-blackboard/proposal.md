# Change: 整理PoseGraph只读Blackboard与输入范围

修订：r2，2026-09-13。按用户广播`2026-09-13-authoring-r2-plan`对齐[原生C# authoring r2](../remove-agent-authoring-use-native-csharp/design.md)。本轮只更新规划；原实现勾选不变，新增迁移任务保持未完成。

## Why

当前PoseGraph把动画输入和动画属性混放在参数声明中，原生Blackboard又全部投影，作者难以分清值从哪里来、哪些图能读取、哪些是Body内部曲线。用户要求将这项工作独立实施，不能继续混在运行架构旧任务清单里。

本change从`refactor-character-pose-graph-architecture`移入原25.1—25.12及Decision 26，只负责PoseGraph读取和作者范围；原运行基础已经独立归档，其它运行架构剩余任务仍归旧change。

## What Changes

- 区分动画实例变量、外部只读表现事实、子图公开输入、随Pose传播的曲线与节点/资源配置。
- 为Root、StatePose、Subgraph和Linked Pose入口定义正式输入可见范围；Blackboard只读投影可访问声明，合法未使用声明仍可找到和拖出Get。
- Get绑定同一正式变量身份、类型和实例来源，PoseGraph主图不提供共享动画变量Set。作者名称与内部稳定身份分离，重命名不破坏引用。
- **BREAKING**：删除动画属性向每张PoseGraph复制声明的路径；编译完整收集正式输入与曲线依赖，保留曲线混合、惯性响应和最终BlendShape写入。
- **BREAKING**：为Body内部FootPlacement权重建立明确的输入Pose曲线绑定或真实公开控制输入；读取链完成后删除废弃根图Get、透传端口及确认无引用的重复子图。
- Get、条件与BlendSpace共同消费事件图同次成功发布的精确类型变量帧，保留Fact、状态时间、子图参数和曲线各自来源；输入发布不冒充最终Pose提交。
- 复用正式Pose Capability、类型化修改API、领域校验、Compiler source map与只读观察；移除Pose层对Agent Mapper/DTO、Document、Exporter/Reconciler和反向导出的执行依赖。
- 提供完整Pose读取与配置能力，供公共C#输出薄适配使用。通过显式`btsmtl.export_code`导出当前结构，通过`btsmtl.generate_assets`执行已编译代码重建指定范围；两个工具由C# authoring任务实现，本任务不新增工具或源码同步。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `character-presentation-pose-graph`：补足只读Blackboard范围、变量与曲线分别传播、Transition变量消费，以及完整运行/Preview输入合同。

## Impact

- 作者：CharacterPoseCanvasGraph、Get拖拽入口、节点Inspector、图与子图引用。
- 数据与编译：CharacterAnimationInputContract只引用事件图唯一Contract/Layout；保留Fact、Slot、World、子图参数与曲线来源，调整typed读取及诊断，不生产另一份共享变量声明或布局。
- 运行：只调整Pose消费者读取来源；保留状态机、Source采样、Blend、Slot、Foot、FBBIK、Final Publication和既有曲线数学。
- 资产：由正式Pose类型化API修改；显式C#生成可替换物理对象，图/节点/变量业务ID必须重建一致，内部引用使用新生成对象，明确恢复Profile/Definition根挂接。未导出修改不会自动合并，不依赖旧生成子资产GUID。
- 文档：本change唯一持有原25.x范围；[旧架构change](../refactor-character-pose-graph-architecture/proposal.md)不再重复维护对应任务、设计正文或delta。
- 本轮按用户指定同时对接[旧FlowCanvas作者/预览方案](../integrate-pose-flowcanvas-editor-preview/proposal.md)的公共输入与authoring协议条款；旧方案其它作者业务、资源和任务不迁入本清单。

## Dependencies And Boundaries

- EventGraph正式规划由独立窗口维护，现以[add-flowcanvas-event-graph](../add-flowcanvas-event-graph/design.md)及本次广播为共同基线：事件图原生执行，唯一拥有声明、更新、Contract/Layout/Frame；本任务消费它们，不另设运行模式。
- 共享变量采用Float、Int32、Bool精确类型；Get、条件、BlendSpace使用相同图/变量身份和唯一布局，校验实例、表现采样、Simulation tick、Reset代际和版本。消费者结束前输出不得被重写。
- Source Pending不回退已成功更新的事件图状态；Pose仍遵守原提交规则。全部运行和完整Preview消费签名迁移完成后，事件图任务才删除CharacterPresentationProgramParameterFrame及旧生产方法；不补默认motor值。
- C# authoring任务拥有两个显式工具、公共代码输出/生成入口和Agent Mapper/DTO退役；事件图任务拥有HostEventGraph原生操作API；本任务拥有CharacterPoseGraphAuthoringAdapter、Pose Mutation、输入合同及消费文件的对应调用修改。
- 曲线来源分类、已有作者显示和Body内部依赖可以在其自身合同内推进，但不能把只隐藏UI描述为完整迁移，也不能把未完成接口从本change范围中删掉。
- Foot权重继续只控制既有Goal可见权重，不释放Anchor、清空历史或改变Landing Reach准入。
- 不新增测试或验证任务。实现与文档任务只列在tasks.md，实际执行和检查结果另写execution.md。

## Current Spec Comparison

- 现行`character-presentation-pose-graph`的Transition仅允许Fact/时间，与同次动画变量消费冲突。本delta补入正式typed变量帧，保留时间、作用范围、priority、stable order和Gameplay mutable address禁令。
- 旧“committed parameter page”必须区分输入成功发布与最终Pose提交；source-local曲线仍随Pose传播，不由事件图Set或Frame快照替代。
- project.md现行Blackboard只读描述的是当前Pose消费表面；不等于禁止EventGraph作者变量Set。本change只更新Pose读取侧，事件执行合同由独立规划按本轮已确认公共基线维护。
- 现行BlendSpace样本参数聚合和Inertialization响应数学保持不变。若后续实现必须改变它们的正式合同，应明确扩充对应delta，不能以本次任务分离视为已授权算法变更。
- 旧Document/五工具/专属Validator条款由C# authoring任务退役，本任务删除其调用依赖并保留真实领域规则，不创建中央Validator、整包同步事务或第二Pose模型。人工编辑不自动导出源码，显式生成不自动合并未导出修改，两者不自动Build。
- 本change不恢复旧Canvas、旧Program或第二IK链；只对接完整运行/Preview的公共输入签名，不接管旧方案的预览生命周期、其它Compiler质量、TrainingEnemy退役或资源作者改革。

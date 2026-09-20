# Change: PoseGraph只读输入与原生Runtime接入

修订：r3，2026-09-14。按用户广播`parallel-20260914-domain-01-planning-update`接收[领域运行主方案D9—D13](../archive/2026-09-17-replace-character-program-with-domain-runtimes/design.md)与[协调审阅](../../../docs/coordination-progress.md)中的Pose原生Runtime范围。复用规划窗口01a09594-1751-7512-b8c0-08b04185055b和已有Pose实现窗口01a081f3-46f4-7c91-8930-73923ff7950b，不创建任务或派发实现。

第1、2组20项已完成记录保持原文和勾选；第3组是新增Runtime接收范围的唯一实施清单。本轮只改规划与必要delta，不改代码、资产、其它owner文档或历史执行证据。

## Why

只读Blackboard、唯一EventGraph变量合同和曲线清理已经完成。当前Pose Graph仍拒绝原生初始化，NativePorts仍包含Editor占位类型和抛异常输出，角色依赖Image专属编译/执行链。用户现在要求已有Pose任务接收原生FlowCanvas Runtime迁移，不能把旧只读任务的完成当作Runtime已经交付。

本change保留原独立身份，在已完成范围之外新增Runtime章节。主方案保留角色Host、共享表现装配、角色快照与总Projection删除；本任务提供明确领域接口并独占Pose节点及Image专属执行迁移，不承接其它角色/技能任务。

## What Changes

以下只读输入与r2作者能力已经交付，继续保护；涉及Compiler/Program的旧实现方式由下方r3原生Runtime目标替代，不重新打开完成项。

- 区分动画实例变量、外部只读表现事实、子图公开输入、随Pose传播的曲线与节点/资源配置。
- 为Root、StatePose、Subgraph和Linked Pose入口定义正式输入可见范围；Blackboard只读投影可访问声明，合法未使用声明仍可找到和拖出Get。
- Get绑定同一正式变量身份、类型和实例来源，PoseGraph主图不提供共享动画变量Set。作者名称与内部稳定身份分离，重命名不破坏引用。
- **BREAKING**：删除动画属性向每张PoseGraph复制声明的路径；编译完整收集正式输入与曲线依赖，保留曲线混合、惯性响应和最终BlendShape写入。
- **BREAKING**：为Body内部FootPlacement权重建立明确的输入Pose曲线绑定或真实公开控制输入；读取链完成后删除废弃根图Get、透传端口及确认无引用的重复子图。
- Get、条件与BlendSpace共同消费事件图同次成功发布的精确类型变量帧，保留Fact、状态时间、子图参数和曲线各自来源；输入发布不冒充最终Pose提交。
- 复用正式Pose Capability、类型化修改API、领域校验、Compiler source map与只读观察；移除Pose层对Agent Mapper/DTO、Document、Exporter/Reconciler和反向导出的执行依赖。
- 提供完整Pose读取与配置能力，供公共C#输出薄适配使用。通过显式`btsmtl.export_code`导出当前结构，通过`btsmtl.generate_assets`执行已编译代码重建指定范围；两个工具由C# authoring任务实现，本任务不新增工具或源码同步。

### r3新增Runtime接收范围

- **BREAKING**：Graph/Node/Connection/NativePorts直接接FlowCanvas原生初始化、typed连接和Manual执行，删除Editor运行占位，不创建加载期Image或第二Runtime DTO。
- 每Actor及独立子图调用拥有自己的状态；按Actor、调用、求值身份和阶段缓存，使Player、转换、源采样和求解同阶段只执行一次。
- 提供分型准备/创建/替换、源需求准备、求值、提交/丢弃和停止/释放接口。沿同一表现时钟和唯一Animancer Barrier，接现有Source、IK任务当前Constraint接口和Final Publication。
- 发布真实Prepared与Adopted结果以及原生节点观察；主实现只调用和汇集，不计算假ProgramEpoch或替领域确认采用。预览消费这些接口，不建立实例工厂或隐藏Image。
- **BREAKING**：在消费者切换后删除Pose IR/Image/Execution View、全图操作表、专属Compiler/Worker和产物入口。保留真实节点算法、资源准备与节点内部Native/Job能力；共享Host/装配壳/总Projection由主实现唯一修改。

## Capabilities

### New Capabilities

无。

### Modified Capabilities

- `character-presentation-pose-graph`：保留已完成只读输入并补入原生Pose准备/采用、阶段运行接口与条件/曲线原生消费；取消本delta残留的Image/专属Compiler前置。

## Impact

- 作者：CharacterPoseCanvasGraph、Get拖拽入口、节点Inspector、图与子图引用。
- 输入与原生绑定：CharacterAnimationInputContract只引用事件图唯一Contract/Layout；保留Fact、Slot、World、子图参数与曲线来源，调整typed读取及诊断，不生产另一份共享变量声明或布局。
- 运行：接收原生图、节点、连接、阶段缓存与私有状态；状态机、采样、Blend/Slot和曲线业务继续保留，Foot/IK通过其当前正式接口接入，Final Publication仍唯一写骨骼。
- 资产：由正式Pose类型化API修改；显式C#生成可替换物理对象，图/节点/变量业务ID必须重建一致，内部引用使用新生成对象，明确恢复Profile/Definition根挂接。未导出修改不会自动合并，不依赖旧生成子资产GUID。
- 文档：本change继续持有原25.x完成事实，并唯一登记本次Runtime接收任务。主方案D13原编号作为来源映射，主方案owner维护责任指针和公共集成项，不在两处并行实现同一节点工作。
- 旧FlowCanvas作者/预览方案的r2公共对接事实保留；其残留Image/独立Pose编译条款列入本设计冲突表，本轮不覆盖旧方案或其它维护者文档。

## Dependencies And Boundaries

- EventGraph正式规划由独立窗口维护，现以[add-flowcanvas-event-graph](../archive/2026-09-17-add-flowcanvas-event-graph/design.md)及本次广播为共同基线：事件图原生执行，唯一拥有声明、更新、Contract/Layout/Frame；本任务消费它们，不另设运行模式。
- 共享变量采用Float、Int32、Bool精确类型；Get、条件、BlendSpace使用相同图/变量身份和唯一布局，校验实例、表现采样、Simulation tick、Reset代际和版本。消费者结束前输出不得被重写。
- Source Pending不回退已成功更新的事件图状态；Pose仍遵守原提交规则。运行和完整Preview消费签名已迁移，旧CharacterPresentationProgramParameterFrame及旧生产方法已删除；不补默认motor值。
- C# authoring任务拥有两个显式工具、公共代码输出/生成入口和Agent Mapper/DTO退役；事件图任务拥有HostEventGraph原生操作API；本任务保留Pose作者/输入与内部执行接入；r3共享Host、表现装配壳及总Projection对应调用由主实现唯一修改，具体文件边界见design第11节。
- 曲线来源分类、已有作者显示和Body内部依赖可以在其自身合同内推进，但不能把只隐藏UI描述为完整迁移，也不能把未完成接口从本change范围中删掉。
- Foot权重继续只控制既有Goal可见权重，不释放Anchor、清空历史或改变Landing Reach准入。
- 不新增测试或验证任务。实现与文档任务只列在tasks.md，实际执行和检查结果另写execution.md。

## Current Spec Comparison

- 现行`character-presentation-pose-graph`的Transition仅允许Fact/时间，与同次动画变量消费冲突。本delta补入正式typed变量帧，保留时间、作用范围、priority、stable order和Gameplay mutable address禁令。
- 旧“committed parameter page”必须区分输入成功发布与最终Pose提交；source-local曲线仍随Pose传播，不由事件图Set或Frame快照替代。
- project.md现行Blackboard只读描述的是当前Pose消费表面；不等于禁止EventGraph作者变量Set。本change只更新Pose读取侧，事件执行合同由独立规划按本轮已确认公共基线维护。
- 现行BlendSpace样本参数聚合和Inertialization响应数学保持不变。若后续实现必须改变它们的正式合同，应明确扩充对应delta，不能以本次任务分离视为已授权算法变更。
- 旧Document/五工具/专属Validator条款由C# authoring任务退役，本任务删除其调用依赖并保留真实领域规则，不创建中央Validator、整包同步事务或第二Pose模型。人工编辑不自动导出源码，显式生成不自动合并未导出修改，两者不自动Build。
- 本change不恢复旧Canvas、旧Program或第二IK链。r3接收Pose内部原生运行和Image专属Compiler清理；预览会话、角色/技能Compiler、Host/快照/共享装配壳、总Projection、TrainingEnemy及资源/IK算法仍归各正式owner。

## r3 Current Spec Comparison

现行character-presentation-pose-graph仍要求固定Compiler Pass、ProgramImage、Projection及编译SourceMap；character-pose-plan-compilation与character-pose-graph-runtime-architecture仍规定Image/Workspace/Worker执行。r3明确替换这些执行载体，保留类型、拓扑、资源、帧失败和唯一Writer约束。主方案已持有上述旧要求退役及native-flowcanvas-pose-runtime的通用delta；本任务只补自己的领域准备/阶段接口和已完成输入规则，不复制主方案全套delta或执行清单。

主方案的PoseStateMachine草案仍只写Fact/时间，遗漏已完成的动画变量Frame。本任务delta继续保留同次typed变量、短路与作用范围；归并不能用主方案较窄文字覆盖它。D10结果发布者以协调审阅解释为领域owner确认实际采用、主实现汇集；本轮不修改主方案正文。

旧“独立Pose编译”和“每ActorProgramImage执行”属于前一阶段的完成事实，不再约束本次最终运行方式。ACL/数据库/Foot数据等真实资源构建继续由原owner负责；取消图编译不等于删除所有构建或算法校验。

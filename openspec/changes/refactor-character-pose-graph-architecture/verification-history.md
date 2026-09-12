# PoseGraph验证与历史记录

用户于2026-09-12明确要求tasks.md只写任务，不列编译、回放、校验、对账和验收。以下保存移出的原记录及原状态，仅作历史证据，不是执行任务、不要求再次验证，也不把未完成改成通过。原执行证据继续保留在[execution.md](execution.md)。

## 移出的验证及撤销记录

| 原编号 | 原状态 | 原记录 |
|---|---|---|
| 3.9 | 原未完成 | 对账Foot、Support、Pelvis、Goal、Assembler、Bend与最终骨骼保持冻结基线；发现差异定位外层迁移，不修改已保留IK公式或配置 |
| 9.4 | 原已完成 | 对账Barrier前Discard、Barrier内/后Fault和Writer后no-throw Seal，确保收窄根Runtime不改变失败政策 |
| 12.3 | 原已完成 | 对照迁移表确认全部现行Operation Code恰有一个Family且没有Operation继续读取万能记录 |
| 13.11 | 原未完成 | 对账框架sealed packet经Schema-driven Host自动生成的主表／子表字段业务含义、原始输入／几何引用、评分权重／资格／分母保持；确认不存在Foot Host Adapter、手写Column／CsvBinding或第二Schema，保留历史原包且不用总分变化替代行为对账 |
| 14.5 | 原已完成 | 检查Module依赖方向，确保Contracts不引用Implementation、Runtime不引用Editor、Diagnostics不反向驱动运行结果且不存在asmdef循环 |
| 14.7 | 原未完成 | 使用规定参数编译Runtime与Editor工程，并在每次构建后立即执行`dotnet build-server shutdown` |
| 14.8 | 原未完成 | 执行`git diff --check`、本change严格校验和全量严格OpenSpec校验 |
| 14.9 | 原已完成 | 核对未恢复中央Foot状态机、骨盆Reach硬夹紧、末端夹脚、已撤销SmoothKnee或CurrentSupport替代Swing包络候选，保留指定基线的有符号膝向运输，已保留第一阶段IK维护成果，未接管其它未实施IK行为任务 |
| 14.10 | 原未完成 | 每个代码小步复用现有正式输入Replay／Proof和诊断链，对指定基线与上一保留小步分别保存输入、Body、source时间、Foot／Pelvis／Goal／Solved／Physical的差异；未解释业务差异时停止，不用调参或改评分补偿 |
| 14.12 | 原未完成 | 执行正式IL2CPP／Burst AOT产物闭包检查和Performance Capture，分别记录总Presentation、Main Thread、Worker、Job等待与多Actor批次规模；不通过运行时fallback适配缺失平台能力 |
| 14.13 | 原已完成 | 搜索并确认不存在动画预算、Phase Offset、跳帧、旧Pose复用或插值补帧路径；Worker资源压力只按现有精确Completion与Fault政策处理 |
| 15.2 | 原未完成 | 按`TrainingEnemy`名称、`corin-training-enemy` ActorId、`gameplay-lab-target`绑定、资产路径和GUID建立完整引用闭包，确认删除范围覆盖GameplayLab composition、Variant、AssetBundle collector、默认目录、构建入口、Profile、Prefab与generated数据 |
| 18.4 | 原未完成 | 对账迁移前后Corin Graph closure、typed IR、Stage、Operation Family、Program Image source map和资源引用一一对应；TrainingEnemy不得出现在迁移输入、输出或generated manifest |
| 20.1 | 原未完成 | 搜索并确认Pose只有一个Canvas作者资产、一个Mutation写入Owner、一个Node Definition目录、一个Compiler输入和一个Runtime Program链；BTSMTL与AI具体Canvas及资产未被迁移 |
| 20.3 | 原未完成 | 执行`git diff --check`、本change严格OpenSpec校验和全量严格OpenSpec校验，确认没有fallback、兼容alias、旧Canvas入口或TrainingEnemy残留清单 |
| 21.7 | 原未完成 | （2026-09-08由Decision 25取代，撤回完成勾选）明确`CharacterPoseCanvasView`仍是877行自建GraphView，保留Pose专用Node/Port/StateMachine交互与typed Mutation，并接受项目自行维护GraphView交互成本；未宣称接入ParadoxNotion现成Graph Editor，也未把当前实现描述成完整CanvasCore编辑表面。旧取舍作废：编辑表面改按`22.x`接入CanvasCore GraphEditor，本项保留为历史记录。 |
| 22.5 | 原未完成 | 用FlowCanvas `GraphEditor.OpenWindow`打开迁移后的Corin图验证：全部节点与边（Root图12条边、7个子图各1条）正确渲染，建/删节点、拖线、删线、复制粘贴、撤销全部可用，编辑后canonical作者表达式对账通过并走正式Character Build。 |
| 23.1 | 原已完成 | 确认PoseGraph使用FlowCanvas原生`GraphEditor`、`FlowGraph`、`FlowNode`、`Port`、`BinderConnection`、breadcrumb、Node/Connection Inspector、创建菜单与子图下钻；FlowCanvas只承担Editor交互，不进入Pose Runtime。 |
| 23.2 | 原已完成 | 确认`CharacterPoseCanvasNode.OnNodeInspectorGUI`与`CharacterPoseCanvasNodeEditorHooks`已接入节点Inspector；字段选项仍由Pose Capability、Profile和Rig上下文提供，不把Unavailable归因于FlowCanvas能力。 |

## 混合条目的原文

以下原条目同时包含实现与验证表述；tasks.md保留实现内容，原文在此留存，不新增验证要求。

| 原编号 | 原状态 | 原记录 |
|---|---|---|
| 10.11 | 原未完成 | 校验全部正式节点恰有一个Definition且Capability、Document、Mutation、Clipboard和Compiler不存在第二catalog；若Agent可见语义变化则同步`btsmtl-agent-authoring`当前合同 |
| 15.6 | 原未完成 | 使用`rg`和Unity资产依赖结果确认项目不再包含TrainingEnemy路径、类型、ActorId、Prefab／Profile GUID、Missing Script、Missing Asset或collector条目；随后把`openspec/project.md`更新为单Corin且TrainingEnemy已退役的实际真相 |
| 22.3 | 原未完成 | Undo owner定界：编辑器会话内CanvasCore Undo唯一，Mutation应用后不双记Document；外部入口（MCP、Inspector、Clipboard、正式写入命令）Document Transaction唯一不变；验收为编辑器内连续撤销不跳步、不残留半程状态。 |
| 22.8 | 原已完成 | 删除自建`CharacterPoseCanvasView`及其窗口装配，Pose图入口切换到`GraphEditor.OpenWindow(asset.Graph)`；全项目搜索确认无第二画布、无残留引用。 |
| 24.5 | 原未完成 | 为同一PoseGraph建立至少两个合法Binding的结构化对账入口，并验证缺失能力、Rig冲突和Slot/Policy冲突都按稳定Graph/Node/Field路径失败，不保留fallback或角色名猜测。 |
| 25.1 | 原未完成 | 对现有Graph参数逐项标记来源、正式声明owner、消费者和可访问范围，分清动画实例变量、只读表现事实、Pose曲线和节点/资源配置。 |
| 25.10 | 原未完成 | 通过正式Document/Mutation核对Graph Catalog与对象引用，删除确认无引用的重复子图与废弃声明；存在用户改动冲突时保留现场并交由用户决策。 |

## 从任务清单移出的阶段说明

以下保留原阶段说明，不作为任务、执行要求或新的完成结论。

## 任务真相修正（2026-09-05）

本次复审确认：旧Runtime大类、Staged Executor和Native Program已经删除，现有Runtime Module也已经形成实际深度；但这不等于Compiler、Family Binding、Projection Validator、Canvas读写边界和Preview已经收口。以下已勾选项只要描述了这些未完成边界，就撤回勾选；没有被事实否定的已完成项保留。`CharacterPresentationProjectionCompiler`、`CharacterPoseFamilyPayloadBindingPass`、`CharacterPoseGraphProjectionValidator`、`CharacterPresentationPoseGraphEditorWindow`、`CharacterPosePreviewViewport`和`CharacterPoseCanvasView`的现状不得再被描述为已完成的薄入口、唯一lowering Owner、完整CanvasCore编辑表面或Scene Play预览。

复审后重新打开的项为：`9.1`、`9.2`、`10.2`、`10.3`、`10.8`、`10.10`、`10.11`、`11.4`、`11.9`、`11.11`、`11.12`、`12.9`、`13.8`、`13.9`、`14.6`、`14.7`、`17.3`、`18.1`。`14.1`、`14.2`及已确认形成深度的Runtime Module不撤回；`19.x`继续保持未完成。

ACL依赖边界：当前Pose Graph迁移和质量整改没有直接引用ACL类型、路径或资产，现行Pose仍使用项目自己的typed Source Slot、AnimationClip、Pose Projection和Runtime链。`codex/acl`对共享Runtime/Projection文件的修改属于重叠源码，不自动构成Pose前置，也不能用其Program或Projection替代本分支产物。若后续`21.1`或`21.3`实际跨入ACL修改过的Source/Projection合同，必须接入完整ACL祖先链和作者版本，再通过唯一正式Character Build重建对应Target与同组Projection；不得只取最后五个收尾提交或复制脏工作区文件。ACL现有19/19质量报告可发布，但最终发布后的Build状态是`job_lost_after_publication_domain_reload`，不作为正常结束或游戏画面E2E证据；已确认的Scalar门限固定为`0.001`。

## 21. 后续架构质量收口

以下每一步都必须同时闭合Interface、Implementation、Depth和Locality，再进入下一步；不通过拆文件把同一个中央Owner改名成多个浅Module，不把未闭合的Runtime编译当成业务完成。

## 22. Pose编辑表面接入FlowCanvas GraphEditor（2026-09-08新增，Decision 25）

本批代码、调用链、删除项和真实验证范围见[Canvas接入记录](canvas-integration.md)。Document身份误报已修复，用户已确认拖动恢复；空节点清理尚未apply，后续dry-run存在布局Conflict，用户已选择保留清理前布局。22.3／22.5仍未闭环，19.x Scene Play不在本批冒领。

## 25. PoseGraph只读Blackboard与输入范围划分（待实施）

本组只负责PoseGraph消费侧。EventGraph事件、动画变量更新、Set及其生命周期由独立规划窗口负责；不得在本组创建第二变量合同。此处是新增待办，不代表已完成实现。

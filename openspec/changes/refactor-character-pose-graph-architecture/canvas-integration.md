# FlowCanvas Pose作者表面接入记录

## 2026-09-10 当前口径

本页早期提交和历史证据使用“CanvasCore”统称ParadoxNotion图编辑源码；当前代码层的Pose编辑表面明确是FlowCanvas：`GraphEditor`、`FlowGraph`、`FlowNode`、`Port`与`BinderConnection`。后续文档和代码说明以FlowCanvas为准确名称，历史提交标题不回写。

FlowCanvas原生负责Pose作者的画布、breadcrumb、节点/连线Inspector、创建菜单、端口交互、子图下钻、selection、clipboard与编辑器Undo。`CharacterPoseGraphWorkspace`的自定义Details、Graph Navigator和Preview页不是Pose节点编辑真相；它们应逐步收窄为运行观察、诊断、跨Graph检索和正式Preview/Build状态，不得替代FlowCanvas原生作者面板。

正式Preview不在FlowCanvas中重新实现。Preview使用精确Fixture/Definition和正式Projection内Program Image；Pose作者修改后只标记Stale，必须显式Build后才允许正式Preview消费新产物。Build使用精确Definition路径，Fixed入口同时发布Float32、Fixed与共享Presentation Projection；Pose-only编译只作为Build内部隔离步骤，继续保留`CreatePoseOnlyInput()`。

### 2026-09-10 正式Build证据

- `character.build_fixed_products`在目标Unity实例`e852139597e42532`成功返回`success=true`，发布Float32、Fixed与共享Presentation Projection。
- 本次发布的SourceRevision为`17f8802ccc3f6cd8b3ef9f16ad976146814370aaf7c03439bff6e192cb878de9`，SemanticHash为`9bc2aa05c373b4af85abdb728342707c7648977f57d2e14edf2555aac052830b`，ProjectionRevision为`6a1e48073be74c6de14fd8c48999a59002328da269c08e83bc826a7a14f39a22`。
- 生成Projection的`m_ActionPlaybackInputs`已包含7条`FullBodyAction`输入；这证明完整Character路径没有复用缺少Action producer的Pose-only Projection。
- `character.pose_reset_observation`当前仍返回`Pose Preview has no committed frame while animation resources are pending`。这是Preview资源加载/提交证据未完成，不登记为Pose Graph或Build失败，也不创建第二套Preview UI绕过它。

## 当前状态补充（2026-09-08）

本页保留此前ParadoxNotion图编辑源码接入证据。当前方向为[FlowCanvas原生作者UI与正式运行观察](../integrate-pose-flowcanvas-editor-preview/proposal.md)，保留已有编译和Native运行；原生runtime实验已撤回，历史见[决策记录](flowcanvas-experiment.md)。

- `6aede211a`修正状态摘要重复登记，正式checkout已成功返回Clean；下文五处identity错误为修复前记录。
- `a52cdc534`修正节点拖动与端口排版，用户实际确认“能拖动了”；端口完整视觉和连续Undo仍未验收。
- `ad779d225`修复内联图共享路径空值伪差异；随后正式apply job `92b0831046c048059f5244c31cdf786b`成功返回applied、saved及Clean，删除两个空Clip节点并按用户选择恢复清理前布局。
- 正式Float32 Build job `d3b622ce0d87440ebd87ddcbed6c33d3`已发布Program／Projection，checkout job `51ad61430e2e48afae34bff2576157ef`再次返回Clean。下文失败内容为修复前记录，不代表当前仍有同一阻塞，也不代替后续源码变更的验证。

更新：2026-09-08。实施目录：`D:/Unity_Project_1/3C`。本批只收口 Canvas 编辑表面及必要的作者写入／撤销，不认领整个 PoseGraph Runtime、PIK、Pose Correction 或 Scene Play 重构完成。

代码提交：`726315c59`（接入CanvasCore作为Pose唯一编辑表面）。

## 已接通的作者链

```text
Pose Graph 资产／Profile／精确 Definition 入口
  → FlowCanvas GraphEditor
  → CharacterPoseGraphWorkspace（导航、详情、状态机、规则、既有观察面板）
  → Definition／Port Shape
  → typed Presentation Mutation
  → preflight
  → 真实作者 owner 的 Undo／序列化
```

根图和 Pose 子图直接显示正式 `CharacterPoseCanvasGraph`。状态机和转移规则保留原来的 typed 作者数据；`CharacterPoseDocumentCanvas`只为同一 GraphEditor 提供不保存的编辑视图，节点只携带稳定身份和端口等显示信息，修改提交原 Document Mutation。视图不进入资产目录、Document 包、Compiler、Program 或角色运行，不保存第二份拓扑。

## 具体变更

| 业务 | 当前实现 |
|---|---|
| 正式打开 | 资产双击、Corin菜单、Profile／Linked Pose入口均进入GraphEditor；旧独立Pose窗口改为同窗口工作区 |
| 资产独立编辑 | 直接打开图资产不要求已有运行产物；没有Definition上下文时禁用Build／运行操作，不猜角色或建立假上下文 |
| 节点与连线 | 创建菜单按Definition及Graph Role过滤；显式端口索引解析到实际端口身份；禁止默选第一个不明确输入 |
| 从端口创建 | 菜单按源端口类型列出兼容目标输入，多个目标输入分别可选；不使用默认输入掩盖歧义 |
| 端口显示 | 固定／条件／动态端口读取正式投影，显示名称与类型，连接线锚定实际端口；保留类型／Pose空间校验 |
| 复制粘贴与删除 | 键盘、节点／多选菜单进入既有Document clipboard和批量Mutation；不靠FlowCanvas原始序列化绕过Pose Mutation写入资产 |
| 移动与名称 | 经正式Mutation修改；布局／重绘误差不写作者位置；仅实际拖动移动节点，选择和普通MouseUp不统一改整图布局 |
| 字段 | 字段、可见性、约束来自Definition；仅真实输入变更提交，普通重绘不把默认显示值写回；只读观察禁用作者写入 |
| 撤销与对象身份 | 图修改保留仍存在的节点／边编辑对象，避免选中对象失效；序列化前后进入Canvas Undo；状态机内容的实际Canvas子资产也纳入原事务 |
| 状态机与规则 | Entry／State／Alias／Transition及规则节点使用同一GraphEditor视图，沿原policy创建、连接、删除和配置；状态数据仍只归原作者owner |
| 观察 | 原有导航、详情、调参和观察面板挂入GraphEditor；运行高亮是纯视图数据，不改作者节点颜色 |
| 生命周期 | 关闭／脚本重载释放router、事件与临时视图；按精确资产和页面身份恢复；资产更新后重读稳定owner，不持有Undo前的状态机对象 |
| 框架边界 | 禁用Pose的通用Graph运行、原始JSON导入和通用反射重构入口；其它领域保留原默认行为；vendor钩子标明3C |

没有新增测试代码。原临时 `PoseCanvasEditorSessionVerification` 删除状态保留。

## 删除与改名

- 删除 `CharacterPoseCanvasView` 及meta，Pose不再保留项目自建GraphView。
- `CharacterPresentationPoseGraphEditorWindow`迁为普通`CharacterPoseGraphWorkspace`；它不是第二个EditorWindow，不绘制另一份画布。
- 原窗口与Tuning源码按新职责改名，保留对应meta身份；Profile、Linked Pose及观察面板调用者同步迁移。
- BTSMTL／AI自己的GraphView没有迁移或删除。
- 既有观察面板的完整Scene Play后端迁移仍归19.x／预览change，本批没有新建预览播放器或宣布该部分通过。

## 验证事实

Center改动：`Pose CanvasCore编辑器接入收口`，`change_id=d8f5ac20febd4916913a7f7ea71dfc09`。

| 检查 | 实际结果与限制 |
|---|---|
| RunHost编译基线 | `WorkspaceEditorInUse`拒绝主目录，没有RunId；保留主验收Editor，没有另建临时运行器 |
| 当前Unity脚本 | 经实例`e852139597e42532`正式refresh／重载，修正后Console无C#错误；重载期间CLI曾断开，随后核对同一实例 |
| 正式打开 | 已执行`Tools/3C/Pose Canvas/Open Corin in CanvasCore Graph Editor`；随后Console没有新增错误。这不等于逐项鼠标交互已经全部验收 |
| 静态删除检查 | 源码中旧`CharacterPoseCanvasView`和旧窗口类型零引用，改动范围diff检查通过 |
| Corin正式Validate | job `239fa6235b8b422fae6e895e7b03773c`完成，失败为两个Clip Player缺少Source Slot，Projection因此无效；不能登记Build通过 |
| Document checkout | job `be33afd0b8d04c3b8cf765e116a91e09`完成，staging重读失败；既有空节点约束错误之外，还有五个技能状态identity与Graph节点identity重复；没有发布可编辑新包，没有apply |

本次CLI原始返回位于仓库`.codex-tmp/canvas-core/`：`console-final.json`、`open-corin-final.json`、`authoring-validation-final.log`、`checkout-result.log`。它们是本机直接执行证据，不冒充RunHost已完成运行或既有回放。

两个缺少Source Slot的节点均在根图`ed8ff472330e4057a900af3eae5dfb8f`：

- `418ec50c418b472ea46473c88522c423`，Clip Player，位置`(-600, 300)`。
- `877f9049c33448e0864ad4eefc9b1ec2`，Clip Player，位置`(-600, 420)`。

它们不在核对时HEAD资产中、没有业务连线，位置与旧验证菜单固定位置一致。清理曾尝试进入正式Document链，但checkout拒绝了当前源树，尚未执行删除。没有给节点猜动画源，没有手改Unity YAML，没有覆盖其它作者布局或资产改动。

五条身份重复位于`editable.controller.stateMachines.states`与Graph `ba00b356-4bf5-4b38-9bec-7ac934b7a25c`的节点之间，原始错误保存在checkout结果。它属于主BTSMTL Document对账缺口，本批没有把它改成宽松解析或另建修复路径。

## 本批完成边界

Canvas编辑接入代码、正式窗口切换和必要调用者清理已落地；当前编译与打开入口已核对。22.3的连续Undo交互、22.5的完整作者表达式／正式Build验证，以及22.7关联的Scene Play仍不能凭这些证据关闭。原任务保留对应未完成状态，不能把本批代码交付描述成整个PoseGraph重构完成。

## 代码入口

- [正式入口](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/PoseGraph/PoseCanvasGraphEditorEntryPoint.cs)
- [同窗口工作区](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseGraphWorkspace.cs)
- [原语写入与端口解析](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseCanvasEditorWriteSession.cs)
- [端口和连线显示](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseCanvasPortsGUI.cs)
- [Document命令](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseCanvasCommands.cs)
- [状态机／规则编辑视图](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseDocumentCanvas.cs)
- [事务与序列化](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseGraphMutation.cs)

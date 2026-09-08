## Why

技能与Pose需要统一且成熟的节点、端口、子图和运行观察体验。目前旧GraphView、Pose专用Canvas适配及原生FlowCanvas运行实验的方向交错，作者难以判断哪份图是真相、哪个节点正在执行。本提案以本次讨论最后确认的编译执行路线收口：FlowCanvas保存唯一正式作者图，现有编译运行链输出真实诊断，编辑器复用原生显示能力观察执行。

## What Changes

- **BREAKING**：BTSMTL技能执行图、技能局部状态／规则图及Character Pose作者图使用FlowCanvas正式图、FlowNode和BinderConnection基础。保留领域Capability、稳定身份及严格端口规则；不将FlowCanvas图再同步为旧BaseGraph或旧Pose作者拓扑。
- 继续使用技能Semantic IR／Numeric Program和Pose Program Image编译执行，保持ActionInstance、Session Step、Float32／Fixed、Pose Frame／Worker及唯一最终输出职责。FlowCanvas的端口委托、协程、Graph Update不进入这些角色运行入口。
- 原生Macro作为参数化子图基础，复用接口、调用节点及下钻能力；私有Macro由根资产拥有，共享Macro显式引用。Sub Flow只在提供完整领域编译合同后允许创建，不因为原生菜单存在就自动开放。
- 建立与运行产物同版本的节点／端口／调用点调试映射，支持一对多编译、嵌套调用、优化节点和精确角色／技能实例定位。
- 增加只读运行观察接入，复用原生节点状态外观、连线动画、数据标签和子图导航；区分执行经过、持续运行、等待、完成和中断，不为亮灯启动第二份FlowCanvas图。
- 预览消费正式Scene Play协调器和同一编译运行链；观察、暂停、继续和安全更新边界单步不创建窗口私有时钟或执行器。图内精确指令断点、历史时间旅行及独立Player远程调试不在本批范围。
- **BREAKING**：迁移现有技能和Corin Pose闭包，删除被替代的作者模型、端口绘制和重复交互入口；保留仍有未迁移领域消费者的代码，禁止兼容开关和运行fallback。
- 本轮仅规划，不修改代码、资产、生成产物或current specs。旧资产清理的未完成事务不被本提案视作已完成。

## Capabilities

### New Capabilities

- `flowcanvas-compiled-authoring`：技能／Pose正式图、领域节点与端口、Macro所有权、直接编译、迁移及唯一写入合同。
- `flowcanvas-compiled-runtime-observation`：产物调试映射、实例与调用路径、真实运行事件、原生编辑器只读显示和场景预览观察。

### Modified Capabilities

- `btsmtl-graph-core`：原BaseGraph／内联图／Tree窗口合同保留给未迁移领域，技能与Pose迁入FlowCanvas正式作者合同。
- `graph-authoring-domain-framework`：技能／Pose共享FlowCanvas作者基座，停止对这两个领域强制复用旧GraphView及禁止共享序列化图基类。
- `graph-authoring-editor-shell`：技能／Pose由原生GraphEditor拥有画布与交互，现有领域区域按同窗口宿主接入。
- `btsmtl-agent-authoring-document-sync`：Document直接对账正式FlowCanvas图与Macro闭包，保持整包事务且不暴露第三方序列化内部字段。
- `character-presentation-pose-graph`：明确FlowCanvas作者模型和编译运行边界，运行观察与原生FlowCanvas执行解耦。

## Impact

- 作者及编译入口：`Authoring/SharedGraph`、`Authoring/PoseGraph`、技能工作区、Graph引用、Capability／Port Shape、Gameplay Frontend、Pose Compiler入口。
- 框架：本地`Assets/ParadoxNotion/FlowCanvas`和`CanvasCore`的正式扩展点；优先继承和复用，必要vendor修改须有明确输入输出与3C标记，不复制一份编辑器。
- 文档与事务：AgentAuthoring Exporter／Mapper／Reconciler／Mutation、资产owner、Undo、保存、迁移和依赖闭包。正式切换为Document v6以表达统一Macro所有权与接口；旧包明确拒绝并重新checkout，不兼容双读。
- 观察：现有Runtime Trace／Diagnostics、编译来源映射、Scene Play观察绑定；不增加写入Gameplay状态的调试旁路。
- 规范冲突与active change对账见`design.md`。本提案将替代Pose原任务22.x中的专用端口接入目标以及23.x／24.x中的正式原生runtime迁移目标；不关闭其尚未完成任务，不修改另一个工作区的实现。

## Why

2026-09-12 职责对账：本change保留转移线上编辑/调度合同及既有Step/Edge阶段成果。剩余最终验证、旧状态存储删除和协议归并对接 [refactor-btsmtl-flowcanvas-authoring/tasks.md](../refactor-btsmtl-flowcanvas-authoring/tasks.md) 第9节的原生FSM计划；不再转交unify-skill-authoring-data-model，也不为中间态再做一套最终模型。原commit与v7迁移证据保持不变，未完成项只在收到相应结果后勾选。

技能图（FlowCanvas 域）的状态机转移目前由 state 节点的有序步骤端口（`BtsmtlSkillStepPort`）表达：条件图、优先级、中止策略全部塞在源节点的端口定义里，画布上表现为多条同名端口（如 Attack1 的三个 `@exit`），条件与优先级完全不可见，作者无法从图上判断转移语义。同一插件家族中 FSM 模块的 `FSMConnection` 已验证"转移线携带条件"是框架支持的成熟形态，且 `FlowGraph.CreatePortConnection` 钩子（项目已加）允许在不魔改插件的前提下让 FlowCanvas 连线携带业务数据。

本变更把技能图状态机转移从"端口承载"迁到"连线承载"：转移条件、优先级、中止策略直接挂在连接对象上，画布可见、可选、可编译。这是作者体验的直接修复，也是技能图向老库 `BaseEdge` 语义（条件在转移上）对齐的第一步；数据模型的中立化归一由后续变更处理，不在本变更范围。

## What Changes

- **BREAKING**：`BtsmtlSkillStateFlowNode`、`@enter`、`@any` 不再继承 `BtsmtlSkillCompositeFlowNode`，状态机结构节点的 steps 端口整体退役；状态机转移改为携带条件数据的连接。
- 新增 `BtsmtlSkillFlowConnection : BinderConnection`：携带条件图引用（role=ConditionRule）、优先级、中止策略；线上显示条件摘要（`GetConnectionInfo`），选中线在 Inspector 编辑（走 `BtsmtlSkillFlowEditorMutation`，与现有节点编辑同一事务通道）。
- `BtsmtlSkillFlowGraph` 覆盖已有钩子 `CreatePortConnection`（`FlowGraph.cs:64`，项目自加的 virtual 工厂），使画布拖线与脚本建线自动产出条件连接；值绑定连线行为不变。
- 编译器切换数据来源：`BtsmtlSkillGraphOccurrence` 直接从连接读取条件/优先级/中止策略（主干本就沿 `outConnections` 遍历，仅去掉"按端口 ID 反查 steps"的绕行）；发射逻辑不变；闭包校验搬到连线（`@any` 的转移必须挂条件，同优先级按连线创建顺序稳定排序，沿 `btsmtl-sm-node-authoring` 现行条款）。
- `sequence`/`selector`/`parallel`/`loop` 的 steps 为顺序分支语义，保留不动；`BtsmtlSkillStepInspector` 移除状态机分支。
- 资产迁移：三项技能（Attack/DodgeBack/DodgeForward）状态机图内 state/`@enter`/`@any` 的 step 数据逐条写到对应连线，走 Document v7 checkout/dry-run/apply；旧 step 字段迁移后删除。
- 范围外（另立变更）：业务谓词节点黑板化（`action-window-active` 等替换为事实槽 + publisher）、双域（`btsmtl`/`btsmtl.skill`）语义目录合并、状态机数据模型中立化。本变更条件图内容仍可使用现有谓词节点。

## Capabilities

### New Capabilities

- `btsmtl-skill-transfer-connections`：技能图状态机转移连接合同 —— 连接携带条件图/优先级/中止策略、线上可见可编辑、状态机结构节点无步骤端口、编译与校验以连线为唯一转移数据来源、AnyState 条件强制。

### Modified Capabilities

（无 —— `btsmtl-sm-node-authoring` 描述的 TreeDesigner 状态机模型行为不变；技能图转移在该 spec 中从未被字面覆盖。对账结论见下。）

## Impact

- 代码：`Runtime/Character/Control/Authoring/FlowGraphs/`（连接子类、`BtsmtlSkillFlowGraph` 工厂覆盖、`BtsmtlSkillStructuralFlowNodes` 状态节点退役 Composite、StepInspector 裁剪）；`Editor/CharacterSimulation/Compilation/Skills/`（`BtsmtlSkillGraphOccurrence`、`BtsmtlSkillGraphFlowEmitter`、`BtsmtlSkillGraphClosure`）；Document v7 codec/validator 需登记新连接类型。
- 资产：三项技能根资产的迁移（checkout/dry-run/apply）；迁移期间编译报错为预期中间态。
- 与 `refactor-btsmtl-flowcanvas-authoring` 的关系：正交于其剩余收尾项（运行观察、网络证据、事务证据），不并入该 change；其 tasks 3.3.1「步骤采用独立 Flow 输出」中状态机结构节点部分由本变更替代。该 change 记录的 `syncState=TreeDirty` 需在资产迁移前裁决。
- spec 对账：本变更的转移条件语义与 `btsmtl-sm-node-authoring` 的「Transition 是同层 BaseEdge 语义」「Transition 调度元数据留在边上」「Transition Rule 条件必须由输入、黑板值和逻辑节点组合表达」一致；技能图以 `BtsmtlSkillFlowConnection` 实现同等语义，两域模型合并归由后续中立化变更处理。

## Context

2026-09-12 职责对账：此前向共同定义提案转交状态存储、端口和版本迁移的安排撤销。总FlowCanvas change现有第9—11节规划原生FSM、最终v8和旧状态存储清理，本专项向其提供已完成转移成果，剩余验证/删除/归并按tasks接收该计划结果。下文保留原Step/Edge阶段设计，不证明最终FSM完成；共同定义提案只负责业务参数与规则去重。

技能图状态机转移现由 `BtsmtlSkillCompositeFlowNode.Steps`（`BtsmtlSkillStepPort`）承载：端口名 = 步骤名 = 目标节点名，条件图/优先级/中止策略在源节点内部，画布不可见。编译主干（`BtsmtlSkillGraphOccurrence.ReadOccurrence`）沿 `outConnections` 遍历后按 `sourcePortID` 反查 steps。插件侧事实：`Connection` 可继承；FSM 模块 `FSMConnection` 为"线带条件"先例；`FlowGraph.CreatePortConnection`（virtual，项目自加钩子）是全部连线创建的必经点（`BinderConnection.Create` L79）；`BinderConnection.CreateValidated` 为 internal。Document v7 已有技能图身份/codec/validator 合同。约束沿 `openspec/project.md`：不写兼容层、迁移不留旧数据、编辑写入口唯一走 `BtsmtlSkillFlowEditorMutation`。

## Goals

- 状态机转移语义全部落在连线上，画布可见、可选、可编译、可校验。
- 状态机结构节点（state/`@enter`/`@any`）退役步骤端口，普通流程复合节点不受影响。
- 编译器以连线为唯一转移来源；迁移后不留旧数据路径。

## Non-Goals

- 谓词节点黑板化（`action-window-active` 等替换为事实槽）—— 另立变更。
- `btsmtl`/`btsmtl.skill` 双域语义目录合并、状态机数据模型中立化 —— 另立变更。
- 老库（TreeDesigner）状态机模型与 `btsmtl-sm-node-authoring` spec 的任何修改。
- 运行观察、网络证据（归 `refactor-btsmtl-flowcanvas-authoring` 收尾项）。

## Decisions

### D1 连接子类继承 BinderConnection，不抽中立语义层

写 `BtsmtlSkillFlowConnection : BinderConnection`，序列化字段：条件图引用（`BtsmtlSkillFlowGraph`，role 校验为 ConditionRule）、优先级（int ≥ 0）、中止策略（`ProgramAbortPolicy`）。编辑器照 `FSMConnection` 模式：`GetConnectionInfo()` 输出条件摘要 + 优先级 + 中止策略（线上直接显示）；`OnConnectionInspectorGUI()` 编辑三个字段，写入走 `BtsmtlSkillFlowEditorMutation`（与节点/步骤编辑同一事务与撤销链）。

备选与取舍：中立语义层（同时解决双写与 Pose 归一）被否——工程量数倍且不改善眼前画布；仅改步骤命名（治标）被否——条件仍不可见；魔改 FlowCanvas 连线创建核心被否——项目已有 `CreatePortConnection` 钩子，无需动插件核心路径。本决策不排斥后续中立化：连线携带的三个字段与 `BaseEdge` 语义同构，二次迁移数据形态不变。

### D2 连接类型按图 role 判定，不按端点判定

`BtsmtlSkillFlowGraph` 覆盖 `CreatePortConnection`：图 role 为 StateMachine 时 FlowOutput→FlowInput 连线创建 `BtsmtlSkillFlowConnection`，其余（值连线、非状态机图）走原逻辑。状态机图内全部 flow 连线均为转移（`@enter`→state、state→state/`@exit`），无歧义；普通流程图零行为变化。

备选：按端点是否为 `IBtsmtlSkillStateStructureNode` 判定——在普通流程图与状态机图交界处（`state-machine` 节点所在根图）语义模糊；按端点判定被否。

### D3 插件补丁：提升 CreateValidated 可见性

`BinderConnection.CreateValidated` 为 internal，技能域 asmdef 不可见。在插件内补一个 public 包装（带 `3C` 注释，与现有 `FlowGraph.cs:61`、`Editor.Node.cs:786` 补丁同清单管理），子类工厂调用它完成验证与绑定。不复制创建逻辑——避免与插件实现漂移。

### D4 编译器直读连线，不做双读兼容

`BtsmtlSkillGraphOccurrence` 删除"按 `sourcePortID` 反查 steps"分支，`BtsmtlSkillEdgeOccurrence` 以连线上的条件 occurrence/优先级/中止策略取代 step；`BtsmtlSkillGraphFlowEmitter` 数据来源换边、发射逻辑不变；`BtsmtlSkillGraphClosure` 校验随迁。状态机结构节点若检出旧步骤数据 → 报告迁移残留错误。不做"step 与连线双读"的过渡兼容——迁移期间编译报错是接受的中间态，资产 apply 后恢复。

### D5 状态机结构节点退役 Composite

`BtsmtlSkillStateFlowNode`/`@enter`/`@any` 改为普通单输入单输出节点；`sequence`/`selector`/`parallel`/`loop` 保留 Composite（顺序分支语义 ≠ 转移语义）；`BtsmtlSkillStepInspector` 移除状态机分支。端口名 = 步骤名的生成逻辑对状态机节点随之消失，连线显示由 `GetConnectionInfo` 承担。

### D6 资产迁移走 Document v7 正式流程

迁移映射：源节点 steps 按 `step.Id == 连线.sourcePortID` 匹配，将条件/优先级/中止策略写入连线，随后删除节点步骤数据。走 checkout → dry-run → apply → re-checkout（Clean 收口），与技能迁移同一套事务与回滚；Document codec/validator 同变更内登记新连接类型，拒绝未知类型。迁移前需先裁决 `refactor-btsmtl-flowcanvas-authoring` 记录的 `syncState=TreeDirty`。

## Risks / Trade-offs

- [Document v7 codec 未覆盖新连接类型，apply 丢字段或拒绝] → codec/validator 与代码同变更落地；dry-run 在 apply 前核对连线字段齐全。
- [复制粘贴/Undo 对自定义连接的覆盖未经插件级验证] → 复制路径（`BtsmtlSkillGraphCopy`）显式核对连接类型；Undo 走框架统一连接列表，端到端由主人验证，任务内仅列核对项。
- [`GetConnectionInfo` 在状态机图大量连线下的绘制开销] → 摘要为纯字段拼接（条件名 + 优先级），不做图遍历或资产加载。
- [迁移中间态编译红窗] → 代码与迁移同一变更内按序执行，apply 后消除；不引入双读兼容。
- [`refactor-btsmtl-flowcanvas-authoring` 的 TreeDirty 阻塞资产迁移] → 迁移任务前置一条"裁决外部改动"步骤，不静默 checkout。

## Migration Plan

1. 连接子类 + 工厂覆盖 + 插件补丁落地（旧资产行为不变，新拖线为新类型）。
2. 连线 Inspector / 线上摘要可用。
3. 编译器切读连线（旧资产编译红为预期）。
4. Document codec/validator 登记。
5. 三项技能根 checkout/dry-run/apply 迁移，re-checkout 验证 Clean。
6. 状态机结构节点退役 Composite，删除步骤旧路径。
回滚：apply 失败按 Document 事务回滚 owner；代码回滚为普通 revert（迁移未 apply 前无资产变更）。

## Open Questions

（无 —— 谓词黑板化、双域合并、中立化均已声明为后续变更范围。）

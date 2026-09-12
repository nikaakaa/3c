## 后续Skill数据层交接

2026-09-12：本表原完成记录和6.1—6.3的v7 apply/validate证据保持不变。原业务节点与FlowCanvas共同定义引起的Skill字段/端口消费适配，接收 [unify-skill-authoring-data-model/tasks.md](../unify-skill-authoring-data-model/tasks.md) 第2、3、5节结果，不再建立第二份同义节点定义。Presentation、Control、Clip及通用Document事务继续按原职责保留。

共同定义change不自行改变公开包形状或版本；原生FSM与最终协议迁移归总FlowCanvas change。此处v7证据只属于原阶段，后续消费其实际已发布的唯一合同，不能回退或双读。交接及规范对账见[新设计D8/D9](../unify-skill-authoring-data-model/design.md)。

## 1. 现有语义盘点

- [x] 1.1 盘点正式SkillGraph、Timeline、Blackboard、Pose Graph、PoseStateMachine、Slot/Mask、Profile和Clip Curve作者类型，与Agent侧`AgentPackage...`、Capability、字段、端口、owner定义逐项对账；交付唯一来源与删除清单。
- [x] 1.2 冻结当前v7 Agent可见业务形状，区分正式作者语义、Document包外壳、运行时状态和Compiler实现；确认内部实现变化不改变schema的条件。

## 2. Formal authoring metadata

- [x] 2.1 在正式作者类型、字段、引用关系和正式Mutation写入方法上补齐统一Agent metadata；交付稳定kind、typed field、logical port、owner和闭包关系清单。
- [x] 2.2 生成不可编辑的Editor静态metadata查找实现，并让共享Capability、Port Shape、原生UI、Document Parser、Validator和Compiler使用同一投影；不得引入运行时反射。
- [x] 2.3 覆盖Skill Graph、Macro、Skill Timeline、TreeClip、Blackboard、Pose Graph、PoseStateMachine、Animation Layer、Control Rig、Slot/Group、Mask、Blend Policy、Animation Producer和Clip Curve；未标记内容不得进入Agent合同。

## 3. Generic Document projection

- [x] 3.1 将Agent Document的Graph/Node/Edge/Timeline/Blackboard/Pose JSON读取与写出改为消费正式metadata，保留stable identity、typed properties、logical ports、references、owner和完整闭包。
- [x] 3.2 删除Skill/Pose领域中重复的字段表、端口表、owner推断和类型分支；未知字段、port、引用和owner继续由strict parser拒绝。
- [x] 3.3 保留从零创建或完整修改Skill闭包的能力，覆盖Entry Graph、嵌套Graph/State/Condition、Macro、Timeline、TreeClip和局部Blackboard；不得缩减为只修改SkillDefinition高层字段。
- [x] 3.4 保留同一Document事务中的Skill与Presentation目标，确保跨域引用使用正式owner和同一document hash。

## 4. Mutation与事务收敛

- [x] 4.1 将正式authoring Mutation Adapter改为消费metadata投影，不再从Agent专属模型推断作者字段或端口；保留完整Skill和Pose写入能力。当前节点 kind、节点规则、外部引用、Blackboard端口、Macro参数端口、复合步骤端口、Macro接口端口、Skill Graph必需anchor和Blackboard declaration规则已改为正式 metadata，动态端口来源由通用 capability 的 `DynamicPortSource` 声明，节点属性读写已下沉到正式 `BtsmtlSkillNodeAuthoringBinding`，Blackboard变量创建与定义校验已复用正式 declaration binding，空 optional inputBinding/factProjection 已在正式 declaration 与 Agent validator 两侧统一归一化；Pose字段值、dynamic port、discriminator、Graph role、Graph-owned Resource Slot、Source/Resource binding与Producer binding已复用正式 authoring 投影/工厂，Agent local Resource Slot 与 transient Resource Binding 也改走正式 factory，Timeline Clip 的校验、Apply、Export及Track类型已下沉到正式 Timeline binding，Graph/Timeline owner编排已分别独立为专用 Applier；Shared Graph capability 现在直接保存并注册通用 `GraphAuthoringCapabilityDescriptor`，删除 `NodeDescriptor` 来回转换；Skill package read/validate/export、Presentation package read/validate、跨owner校验与Reconciler计划构造也已拆成独立模块，Document package已删除旧Presentation shape扫描与不完整状态修复旁路，外层 Adapter 只保留事务顺序与 SkillDefinition 装配。Skill marker 已直接投影通用 `GraphAuthoringCapabilityDescriptor`，删除 `BtsmtlSkillNodeCapability` 中间定义；Agent Pose 导出、校验、闭包和Snapshot路径已统一消费 `CharacterPoseAuthoringMetadata` 投影，不再直接引用 Pose compiler `CharacterPoseNodeDefinitionModule`；跨owner校验中的Source、Animation Channel、Graph引用字段也改为由正式 capability field metadata解析。Presentation闭包已收口，Skill Graph/Timeline创建路径的非local兜底已删除，跨域事务的Hash证据仍由6.2与6.3收口。
- [x] 4.2 保留`AgentMutationPlan`作为内部事务动作集合，删除第二套Agent业务Mutation语义；人工入口与Document Apply继续使用同一正式Mutation。
- [x] 4.3 让唯一Document Transaction继续拥有owner收集、Undo、Rollback、Save、reverse export和canonical package发布；领域Adapter不得创建第二事务。
- [x] 4.4 对新Skill根、私有Macro/Timeline、共享Macro、Pose Graph pair、StateMachine pair和Clip Curve owner执行失败清理与跨owner回滚收口。

## 5. 删除重复实现

- [x] 5.1 删除被正式metadata和通用Document projection替代的Agent Skill/Pose模型、手写Capability表、重复Port Shape判断和旧Mapper分支；源码不得保留兼容reader、fallback或并行authoring入口。当前旧Capability文件、旧Mutation writer、一次性AnimationSlot DTO、Anchor port分支、旧`stateMachineGraphId`路径和旧包refresh兼容扫描已删除；Skill Blackboard、Macro、复合步骤、Skill Graph必需anchor的形状构造和Blackboard定义规则已归正式 Skill metadata，Shared Graph 的 ownerSlot/Anchor允许角色和字段默认值已归正式 Shared Graph capability metadata，Shared Graph capability本身也已移除AgentCompileReport、AgentSnapshotNode和Agent package依赖，且不再维护 Agent 专用的 create/configure/delete kind 白名单，也不再保留 Shared Graph 的 `NodeDescriptor` 双向转换层，Skill与Pose字段值校验、Pose Graph role、Graph-owned Resource Slot和退役节点标记已归正式 authoring 元数据；Skill marker index 不再生成 `BtsmtlSkillNodeCapability` 领域副本，Agent Pose 路径不再直接引用 compiler NodeDefinition，而是统一消费正式 `CharacterPoseAuthoringMetadata` 投影，Agent local Resource Slot/Binding 也不再直接构造正式对象，而是调用正式 factory，Pose resource ownership 也改为由正式 capability field metadata 与 StateMachine transition 投影收集，跨owner校验不再写死Pose字段名，Snapshot导出也改为通过正式Pose metadata读取Source、Channel与Animation Slot，废弃的 `AgentSnapshotNode`、`AgentSnapshotExposedProperty` 和 `AgentAnimationCurvePayload` 已删除，Skill Graph/Timeline闭包已拆成专用 Applier，Skill Mapper/Exporter/Validator、Presentation Codec/Validator、跨owner校验与Presentation Reconciler的职责文件已拆开，Pose Source Slot/Binding、Resource binding和Producer binding创建已归正式 authoring factory；Presentation闭包校验与资源关系下沉已收口，Skill Graph/Timeline创建路径不再保留非local兜底，剩余仅为最终事务证据。
- [x] 5.2 保留五个MCP生命周期Adapter和Job Scheduler为薄入口；删除节点/边/属性/Pose/Clip专用MCP入口及其调用者。
- [x] 5.3 更新Agent技能说明、当前project context和受影响current specs，说明Formal metadata、完整Skill闭包和单一事务关系；不新增第二份术语表。

本轮补充：Skill Definition Document、diff、validator、Mutation 已统一使用 `AgentPackageSkillDefinitionFile`，删除 `AgentSnapshotSkillDefinition` 重复模型；Control Document、Controller package、diff、Mutation 已统一使用 `AgentControlParameter`，删除同字段的 Snapshot/Package 双模型；共享 Document 记录已去除旧 Snapshot 命名；Action eligibility Mutation 已改走正式作者类型写入方法，删除 Agent 内部 `SerializedProperty` 写入旁路；Presentation 的多个 Mutation transaction 仅作为正式 Mutation 批次，Undo、Save、Rollback 生命周期仍由唯一 Document Transaction 持有；Action/Tag、Skill、Presentation 和 Clip owners 均由外层 collector 纳入同一 Undo 边界。

现行 specs 全量 strict 已通过（94/94），本 change 与 `btsmtl-agent-authoring-document-sync` 当前 spec 单独 strict 也通过；全量 changes 仍有其它未归档 change 的 MODIFIED scenario 缺失，不归入本 change。

## 6. 收口证据

- [x] 6.1 对精确Corin Definition重新checkout并确认v7包只包含合法metadata投影和完整Skill/Presentation闭包；不生成顶层Blackboard、旧Character Graph或旧Timeline分片。
- [x] 6.2 使用同一Document hash完成dry-run、apply、reverse export和re-checkout，核对stable identity、owner、Undo、Rollback、Hash与Clean结果。Corin精确Definition已完成 `DocumentHash=2ef089dec110c53a8fae67b0d57a537595b267bd4d4940703839ba4c43cad09e` 的dry-run与apply；apply返回 `success=true`、`applied=true`、`saved=true`、`syncState=Clean`，随后以 `DocumentHash=4b47c6e4a47dd221d5dc04be886a3977f82708eb135b014af34da0b1638d0725` 完成reverse export和re-checkout，状态仍为Clean。旧5个缺脚本Resource Slot由正式Mutation删除，新5个Slot由正式factory创建。
- [x] 6.3 运行OpenSpec严格校验，并完成正式Character validate；确认内部作者类型或Compiler变化不会引起未声明的Agent schema变化。`openspec validate --specs --strict --json` 通过94/94，目标change strict校验通过；Unity `btsmtl.validate` 最新job `4dcba86186264281a3ddafed33b669e8` 返回 `success=true`、`semanticValidCount=1`、`compileSuccessCount=1`、`compileFailureCount=0`、`assetResolveFailureCount=0`，仅保留float32字面量舍入和ABI/codec信息提示。拆分 `CharacterPoseResourceSlot.cs` 后，精确PoseGraph资产中已无 `m_Script: {fileID: 0}`，Console清空后无新错误。

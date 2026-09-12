## Why

FlowCanvas接入后，项目原有业务节点与新FlowCanvas节点重复定义同一业务：例如旧LocomotionInputMotionNode与新BtsmtlSkillLocomotionFlowNode分别保存移动参数并维护校验，动作窗口、输入和状态节点也有对应的两套声明。本change首先把原有有效业务定义抽成两种作者宿主共用的唯一参数与规则，FlowCanvas只适配画布；Inspector、Document和编译的一致性是这一统一带来的消费要求，Agent工具不是本次重构的中心。

## What Changes

- 用户已确定：集中节点参数、定义和引用规则，继续由 FlowCanvas 保存正式图拓扑、Node/Edge identity、布局和原生 Macro 接口；不建立项目自有的第二份 Graph/Node/Edge 集合。
- 从原有节点的有效参数、默认值、校验、逻辑端口与编译语义抽出按业务族组织的正式typed payload与节点定义，保留已正确的业务规则。仍有正式消费者的原节点与对应FlowCanvas节点必须使用同一种业务定义；不能只在FlowCanvas内部新建一套Skill定义而让原定义继续重复存在。
- FlowCanvas节点只持有本节点实例的一份参数并承担插件交互适配；保留的旧作者节点同样退为其框架适配。共用的是定义和规则，不是让不同节点实例共享一个可变参数对象。零消费者旧适配、重复字段声明与校验在迁移后删除。
- 让原生 Inspector、创建菜单、Document、校验、复制/删除、依赖收集和编译读取同一正式定义。删除按节点类型重复维护的字段读写、端口形状和引用遍历；保留必要的包格式转换与独立编译实现。
- 覆盖全部已登记 Skill 节点及原生逻辑 wrapper，包含结构、条件、输入、Blackboard、Character State、Action、Tag/Attribute/Effect、Locomotion、Macro 与 Timeline 引用；不能只改 Corin 已使用的节点。
- **BREAKING**：完成状态机转移的唯一 Edge payload、固定转移端口、条件图 edge owner 和显式稳定顺序；删除状态机旧 steps、旧字段补读和一次性迁移入口。普通组合节点仍使用自己的步骤语义。
- **BREAKING**：纯 payload 抽取不改变外部格式；本 change 同时改变状态机 owner、步骤和端口合同，因此以 Document v8 原子替代 v7。非 Skill 分片保留业务形状，五个生命周期工具和唯一事务不变，正式入口不保留旧版本 reader 或双写。
- 将三个原 change 中重叠的 Skill 数据定义、端口、引用和迁移收尾任务交给本 change；原完成记录保留为有日期的证据，运行观察、网络、Pose 与跨域产品工作继续由原 change 负责。

## Capabilities

### New Capabilities

- `btsmtl-skill-authoring-model`：原业务节点与FlowCanvas节点共用参数/规则定义及其引用、端口、编译读取合同，保持各正式图的实例身份与FlowCanvas技能拓扑存储。

### Modified Capabilities

- `graph-authoring-domain-framework`：明确 Skill 正式节点定义与 payload 是共享 metadata 的来源，插件节点与包 DTO 仅适配，原生 UI 和编译不再各自定义节点规则。
- `btsmtl-agent-authoring-document-sync`：集中消费正式 Skill 定义，明确转移 edge owner、稳定顺序及 Document v8 原子切换，保留完整 Skill/Presentation 包和唯一事务。

## Impact

- 原业务定义与宿主适配：`Runtime/Character/Pipeline/Graph/`、`Input/`、`Motion/`、相关BTSMTL原节点模块与`Runtime/Character/Control/Authoring/FlowGraphs/`。共用业务参数与规则归原业务模块，不能归Agent或某个画布后端；复用`TreeDesigner.Authoring`的Capability、typed field与Port Shape合同。
- Document：`Editor/CharacterPipeline/Authoring/SkillDocument/` 与 `Editor/CharacterPipeline/AgentAuthoring/` 的包版本、严格解析、目标对账和事务编排；不新增 MCP 工具或 apply 服务。
- 编译：`Editor/CharacterSimulation/Compilation/Skills/` 的作者读取和按业务族注册的 lowering；继续写入现有 Semantic IR、Program Builder、Float32/Fixed 产物与 SourceMap。
- 资产：精确 Definition 下的 Skill 根、私有/共享 Macro、条件图、Timeline/TreeClip 和 Blackboard 引用；保留合法稳定身份，迁移冲突逐字段报告，不覆盖用户改动。
- 文档：更新原 FlowCanvas、metadata 和转移 change 的交接入口；对账范围、任务映射与现行规范差异集中在本 change 的 `design.md`。旧 `btsmtl` 非 Skill 消费者单独辨认，不以相同 kind 名称删除合法领域。
- 本轮只生成规划工件，不实施代码或资产迁移，不将计划写成现行完成事实，也不自动归档原 change。

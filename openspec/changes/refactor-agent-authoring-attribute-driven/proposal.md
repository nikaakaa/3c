## Why

2026-09-12 后续范围：本change的共享metadata、Agent领域下沉、唯一事务和v7交付证据保留。Skill正式参数/节点定义、端口和引用仍分散的后续缺口由 [unify-skill-authoring-data-model](../unify-skill-authoring-data-model/proposal.md) 承接，不重复执行本change已完成的Pose/Control/Clip工作；目标v8只因状态机外部形状变化，不推翻“内部重构不升级schema”的原则。

当前 Agent Document 已经能够通过 JSON 表达完整 Skill 闭包和 Presentation 目标，但 Agent 侧又维护了一份节点、字段、端口、owner 和能力定义。正式作者模型一变化，Agent mapper、codec、reconciler、validator 和 schema 会一起变化，导致原本应当简单的 JSON 编辑工具持续膨胀并频繁升级。

这次变更把 Agent 收回为正式作者系统上的 JSON 编辑和事务适配层：保留完整 Skill 创建能力，不再维护第二套作者语义。

## What Changes

- 将正式 SkillGraph、Timeline、Blackboard、PoseGraph、PoseStateMachine、Slot/Mask、Profile 和 AnimationClip Curve 作者类型上的 metadata 作为 Agent 可见语义的唯一来源。
- 保留 Agent 从零创建或完整修改 Skill 闭包的能力，包括 Entry Graph、嵌套 Graph/State/Condition、Macro、Skill Timeline、TreeClip、局部 Blackboard 和 typed 引用。
- 保留现有 v7 JSON 工作包、五个 MCP 生命周期工具、单一 Document 包和单一事务链；不新增局部节点工具、第二个 JSON 形状或第二个 apply 入口。
- 将 Agent Document、Capability、Exporter、strict parser、Reconciler、Validator、Compiler 和原生作者 UI 改为消费同一份正式作者 metadata。
- 删除 Agent 侧重复的节点模型、字段模型、端口表、owner推断、Presentation模型和重复能力注册；编译期生成的查找实现只作为不可编辑的 Implementation。
- 保留 `AgentMutation` 作为内部事务动作，但不再用 Agent 专属 Mutation 模型重新定义正式作者语义。
- 不使用运行时反射、C#类型名、Unity序列化字段、SerializedProperty路径或Compiler operation作为 JSON 合同。

## Capabilities

### New Capabilities

无。此次变更收敛现有 Agent Authoring 能力，不新增业务领域。

### Modified Capabilities

- `btsmtl-agent-authoring-document-sync`：Agent v7必须从正式作者metadata投影完整Skill和Presentation闭包，不维护Agent侧节点语义副本。
- `graph-authoring-domain-framework`：Capability、Port Shape、Document、原生UI和Compiler必须消费同一正式作者metadata来源。

## Impact

- 影响 `AgentAuthoring` 下的 Document models、Codec、Store、Exporter、TargetMapper、Skill Flow Mapper、Presentation Codec/Reconciler、Mutation Planner/Handler 和五个 MCP 生命周期入口的内部实现。
- 影响正式 SkillGraph、Timeline、Blackboard、PoseGraph、PoseStateMachine、Slot/Mask、Profile 与 AnimationClip Curve 作者类型的 metadata 声明。
- 不改变 Character RootTree 已删除、Behavior Designer AI 不进入 BTSMTL Document、Skill 完整闭包和唯一事务的现行业务边界。
- 不进入 Character Program、Presentation Projection、Native Pose Program、网络运行状态或生成产品的业务定义；这些仍由现有显式 Build 生命周期负责。

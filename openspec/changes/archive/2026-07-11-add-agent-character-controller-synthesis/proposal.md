# Proposal: Agent 生成角色动作控制器 authoring 编译链路

## Why

当前 BTSMTL、StateMachine、Timeline、ActionProfile、CharacterPipeline 已经形成角色动作 authoring 主链路，但完整 graph 仍需要人手动组织。AI 时代下，目标作者不应该从零手编复杂 graph，而应该用自然语言或少量参数描述“二连击、闪避取消、受击打断、locomotion 状态”等业务意图，再由 Agent 生成可检查、可修复、可微调的 BTSMTL 结构。

直接让 LLM 生成 Unity asset、BTSMTL 内部集合或完整最终 graph 风险过高：BTSMTL 训练语料少，节点层级规则严格，Timeline、Action Context、TransitionRuleGraph 的位置错误会导致生成结果难以维护。本变更规划一条确定性的 editor-only authoring 编译链路，让 LLM 只输出受限中间表示，最终结构由编译器调用 BTSMTL 正式接口生成。

## What Changes

- 新增 Agent 生成角色动作控制器的 editor-only authoring 编译链路。
- 新增 Agent Snapshot、Agent Controller Intent、Macro、Patch IR、Compiler、Validator 和 Compile Report 的能力定义。
- 新增第一阶段角色动作控制器宏范围：locomotion 状态机、单段 Timeline 动作、二连击、闪避取消、受击反应。
- 新增面向 LLM 生成稳定性的评估口径。
- 明确 Agent JSON/IR 只是中间层，正式源数据仍是 BTSMTL asset 和 Unity 资产。

## 目标

- 定义 Agent 生成角色动作控制器的第一阶段架构：`Snapshot -> Intent -> Macro -> Patch IR -> Compiler -> Validator -> Compile Report`。
- 让 JSON/IR 成为 Agent 使用的机器中间层，而不是策划长期手写的产品 DSL。
- 让编译器只调用现有 BTSMTL authoring 入口，例如 `BaseGraph.CreateNode`、`BaseGraph.Link`、`BaseGraph.LinkProperty` 和正式模块/节点配置入口。
- 支持第一批角色动作控制器宏：locomotion 状态机、单段 Timeline 动作、二连击、闪避取消、受击反应。
- 提供静态评估口径，衡量 LLM 生成链路的 schema 合法率、编译成功率、语义合法率、修复轮数和生成 graph 的业务覆盖度。
- 保持 `BaseTreeAsset` / `CharacterPipelineDefinition` / `ActionProfile` / `Timeline` / `CharacterInputProfile` 为正式资产真相。

## 非目标

- 不在运行时调用 LLM。
- 不让服务端、网络同步或 Gameplay runtime 执行 Agent JSON。
- 不新增第二套 graph runtime、Workbench、端口协议或 Unity YAML 写入路径。
- 不把 Agent JSON 作为策划长期维护的正式源数据；正式源数据仍是 BTSMTL asset 和相关 Unity 资产。
- 不在第一阶段实现通用 CAD 式自由 DSL 或任意节点自由生成。
- 不接入 OpenAI、Claude、本地模型等具体模型 API；第一阶段只消费外部 Agent/Codex 生成的 JSON/IR。

## 方案概述

新增 editor-only 的 Agent authoring 编译链路：

1. `AgentGraphSnapshotExporter` 从当前 `CharacterPipelineDefinition`、RootTree、下钻 StateMachine、StateBehaviorSubTree、TransitionRuleGraph、ActionProfile、Timeline 和 InputProfile 导出只读 snapshot。
2. LLM 基于 snapshot 生成 `AgentControllerIntent` 或 `AgentPatchIR`。
3. `AgentMacroLibrary` 将业务意图展开为受限 Patch IR。
4. `AgentPatchCompiler` 将 Patch IR 应用到 BTSMTL asset，所有结构变更必须走现有 authoring API。
5. `AgentGraphValidator` 在应用前后检查 graph 类型规则、引用解析、Action Context、Timeline、ActionProfile 和输入 request。
6. `AgentCompileReport` 输出错误路径、原因、建议修复、生成 diff 摘要和评估指标，供 Agent 下一轮修复。

## 现有能力对比

- `btsmtl-graph-core` 已要求 `BaseGraph` 是唯一图结构数据，且节点创建必须尊重图类型规则；本变更复用该口径，不新增并行 graph。
- `btsmtl-sm-node-authoring` 已要求 `StateMachineGraph` 只表达状态结构，Timeline 和输入通过状态行为或 TransitionRuleGraph 接入；本变更把这些规则提升为 Agent compiler/validator 的硬约束。
- `btsmtl-runnable-timeline-node` 已要求 TimelineNode 只在行为图里请求播放 Timeline；本变更只生成该正式节点和引用，不新增 TimelineStateNode。
- `character-action-authoring-closure` 已要求 ActionProfile 是策略主入口，Graph 只提交 action activation request；本变更的 action 宏只引用 ActionProfile，不复制网络策略。
- `character-state-timeline-authoring-loop` 已要求 Corin 的动作闭环使用 Action StateMachine + Timeline；本变更将该模式抽为可复用宏和评估样例。

## Impact

- 新增 Agent authoring 相关 editor-only 模块和 schema。
- 新增一个 current spec：`agent-character-controller-synthesis`。
- 不修改 runtime 网络同步语义。
- 不要求 Unity batchmode、手动验证或端到端测试写入任务。

## 待确认

- 第一版宏库是否只覆盖 `locomotion`、`single_timeline_action`、`two_hit_combo`、`dodge_cancel`、`hit_reaction`，还是需要把 guard/parry 也纳入第一版。
- Agent Patch 应用失败时是否要求事务级回滚到应用前 graph，还是第一版先使用 dry-run + validate 通过后再 apply。
- stable authoring id 是否通过新增 editor-only metadata/module 保存到节点和边，还是第一版用 Display Name + graph path 生成弱稳定 id。

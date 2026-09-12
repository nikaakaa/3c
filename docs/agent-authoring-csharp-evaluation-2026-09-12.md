# AgentAuthoring 规划入口

2026-09-12：用户明确调用 `openspec-propose` 后，唯一正式规划为 [remove-agent-authoring-use-native-csharp](../openspec/changes/remove-agent-authoring-use-native-csharp/proposal.md)。

- [设计与现行规范冲突](../openspec/changes/remove-agent-authoring-use-native-csharp/design.md)
- [直接 C# authoring 合同](../openspec/changes/remove-agent-authoring-use-native-csharp/specs/character-csharp-authoring/spec.md)
- [细分实施任务](../openspec/changes/remove-agent-authoring-use-native-csharp/tasks.md)

原评估 r1 已撤下，不作为实施依据。正式方向是删除 Agent 层，直接复用领域 API 与校验；不把 Agent Validator、Session 或总事务框架换名搬迁。原评估可从 Git 历史追溯。

规划窗口为 `01a09634-fc59-7192-8cda-25fdd142b82d`，已有实现窗口为 `01a09635-2024-74b2-98b4-28c1e17d548b`。当前仅完成规划，尚未确认实施或发送实施指针；绑定以正式 design 为准。

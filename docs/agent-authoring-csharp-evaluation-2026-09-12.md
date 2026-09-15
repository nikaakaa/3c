# AgentAuthoring 规划入口

2026-09-12：用户明确调用 `openspec-propose` 后，唯一正式规划为 [remove-agent-authoring-use-native-csharp](../openspec/changes/remove-agent-authoring-use-native-csharp/proposal.md)。

- [设计与现行规范冲突](../openspec/changes/remove-agent-authoring-use-native-csharp/design.md)
- [直接 C# authoring 合同](../openspec/changes/remove-agent-authoring-use-native-csharp/specs/character-csharp-authoring/spec.md)
- [细分实施任务](../openspec/changes/remove-agent-authoring-use-native-csharp/tasks.md)

2026-09-13：规划已更新为 r2。激进删除旧Agent层，只提供两个显式作者MCP：`btsmtl.export_code`从当前资产完整导出干净C#，`btsmtl.generate_assets`执行C#创建并保存资产。手动修改或保存资产不会生成代码；不做源码解析、增量写回、操作历史或自动同步。

原评估与原提案r1的“资产为唯一来源”已被r2替代，不作为实施依据。历史内容可从Git追溯；当前完整范围与绑定以正式design为准。

规划窗口为 `01a09634-fc59-7192-8cda-25fdd142b82d`，已有实现窗口为 `01a09635-2024-74b2-98b4-28c1e17d548b`。当前仅完成规划，尚未确认实施或发送实施指针；绑定以正式 design 为准。

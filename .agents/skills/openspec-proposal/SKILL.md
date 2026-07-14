---
name: openspec-proposal
description: 为本项目创建并严格校验 OpenSpec 变更提案。Use when 用户提出新能力、破坏性变更、架构调整、跨模块重构、数据结构变化，或明确要求 proposal、spec、design、tasks。
---

# OpenSpec Proposal

1. 读取 `openspec/project.md`、`openspec/AGENTS.md`，运行 `openspec list` 与 `openspec list --specs`。
2. 搜索相关当前 spec、active change、代码和文档，明确现状、约束和影响范围。
3. 与现有 spec 对比；发现矛盾、重复或过期约束时，在 proposal 中明确指出，并同步建议更新或删除对应旧 spec。
4. 选择唯一且动词开头的 kebab-case `change-id`，在 `openspec/changes/<change-id>/` 创建 `proposal.md`、`tasks.md` 和每项能力对应的 spec delta；跨系统模型或存在业务取舍时创建 `design.md`。
5. proposal、design、tasks 和 spec delta 除固定格式关键字外使用中文。spec delta 使用 `## ADDED|MODIFIED|REMOVED|RENAMED Requirements`，每条 requirement 至少包含一个 `#### Scenario:`。
6. 不写实现代码。`tasks.md` 按小闭环细分，只列实际实施工作；默认不新增测试任务，也不写人工验证任务。
7. 执行 `openspec validate <change-id> --strict --no-interactive`，修复全部校验错误后再交付提案。

不新增 fallback、兼容路径或临时桥接。迁移方案必须收敛为一条正式数据与调用链。

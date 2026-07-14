---
name: openspec-apply
description: 实现已批准的本项目 OpenSpec change，并把任务状态与最终实现保持一致。Use when 用户要求 apply、实施、继续完成某个 OpenSpec change，或指定 `openspec/changes/<change-id>`。
---

# OpenSpec Apply

1. 确认 change-id，读取该 change 的 `proposal.md`、`design.md`（如有）、`tasks.md` 和全部 spec delta。
2. 读取 `openspec/project.md`、`openspec/AGENTS.md`，并检查关联 current spec 与现有实现，确认实现不会偏离已批准设计。
3. 按 `tasks.md` 的顺序完成细分任务。只在任务真实完成后将对应项改为 `- [x]`。
4. 严格保持单一正式链路：不新增 fallback、兼容层、临时桥接或并行旧路径；迁移完成后删除确定废弃的旧数据、旧配置、旧命名和旧实现。
5. 默认不新增测试；除非用户明确要求。不要运行 Unity batchmode，不把人工验证写入任务。
6. 实现完成后运行与改动相称的非 Unity batchmode 校验，并执行 `openspec validate <change-id> --strict --no-interactive`。
7. 交付时按调用链说明业务输入、处理、输出、改动前后差异，以及仍需用户在 Unity 中端到端确认的内容。

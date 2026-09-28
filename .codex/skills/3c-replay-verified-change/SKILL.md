---
name: 3c-replay-verified-change
description: 在 3C 项目中使用已有固定输入进行代码改动的回放验证、脚部诊断和同输入 A/B 比较，提供项目正式入口及证据读取规则。适用于实际回放或分析已有回放证据；不用于普通代码编辑、单独编译或 CPU 性能采集。
---

# 3C 同输入回放验证

使用当前可用的 `replay-verified-change` 通用 skill 处理改动拆步、基线比较和证据保留；本 skill 只维护 3C 项目入口及诊断合同，不重复通用流程。

- 执行或读取项目回放结果前，读取 [3C 正式入口与证据规则](references/3c-workflow.md)。已有报告可直接分析，不因调用本 skill 启动回放。
- 需要操作 Unity 时，读取 [3C Unity 操作](../3c-unity-mcp/SKILL.md)。仅修改文档或读取文件时不连接 Editor。
- 普通回放使用 `replay_start`；需要 Foot 与 Presentation 采样时使用 `diagnostic_replay_start`。依据任务目标选择，不把诊断采样变成所有回放的前置条件。
- CPU 与托管分配测量使用 [3C 性能诊断](../3c-performance-diagnostics/SKILL.md)，不以动作回放匹配代替性能数据。
- 仅在已明确进入并行开发时使用 [3C 并行开发验证](../3c-fast-development-validation/SKILL.md)，普通回放不自动引入 Center 或 worktree。
- 遵守当前项目的 OpenSpec 触发规则；通用 skill 对 spec/change 的引用不授权启动 OpenSpec 工作流。

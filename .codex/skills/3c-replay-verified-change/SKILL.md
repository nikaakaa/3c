---
name: 3c-replay-verified-change
description: 在 3C 项目中使用已有固定输入进行代码改动的回放验证、脚部诊断和同输入 A/B 比较，提供项目正式入口及证据读取规则。适用于实际回放或分析已有回放证据；不用于普通代码编辑、单独编译或 CPU 性能采集。
---

# 3C 同输入回放验证

使用当前可用的 `replay-verified-change` 通用 skill 处理改动拆步、基线比较和证据保留；本 skill 只维护 3C 项目入口及诊断合同，不重复通用流程。

- 执行或读取项目回放结果前，读取 [3C 正式入口与证据规则](references/3c-workflow.md)。已有报告可直接分析，不因调用本 skill 启动回放。
- Foot IK 候选与离线实验开始前，先读[历史经验](../../../docs/reference/foot-placement/implementation-lessons.md)及[已有业务对照](../../../docs/diagnostics/foot-placement/ik-tests/README.md)，再核对相关候选、撤回提交和原结果。接触、净空、腿长与动画混合各自已有失败边界；不能把只改触发范围、参数或使用另一采样包自动视为新机制。
- 需要操作 Unity 时，读取 [3C Unity 操作](../3c-unity-mcp/SKILL.md)。仅修改文档或读取文件时不连接 Editor。
- 普通回放使用 `replay_start`；需要 Foot 与 Presentation 采样时使用 `diagnostic_replay_start`。依据任务目标选择，不把诊断采样变成所有回放的前置条件。
- CPU 与托管分配测量使用 [3C 性能诊断](../3c-performance-diagnostics/SKILL.md)，不以动作回放匹配代替性能数据。
- 仅在已明确进入并行开发时使用 [3C 并行开发验证](../3c-fast-development-validation/SKILL.md)，普通回放不自动引入 Center 或 worktree。
- 遵守当前项目的 OpenSpec 触发规则；通用 skill 对 spec/change 的引用不授权启动 OpenSpec 工作流。

## Unity 内已有函数入口的程序集身份

- 已有入口以 `Assembly.Location` 计算执行程序集身份时，使用 `Assembly.LoadFile` 加载绝对路径。`Assembly.Load(byte[])` 的 Location 为空，会使入口末尾哈希失败，不能把前面的计算完成写成完整结果通过。
- `LoadFile` 会锁住所加载的 DLL。每个版本使用独立输出目录并记录 MVID 与文件哈希，不原地覆盖已经加载的文件。编译非零退出时停止后续执行，防止误跑上一次残留程序集；封存实际输入和结果后再推进下一版。
- 函数调用超时先核对该次指定的结果文件和实际状态，不重复执行已完成的窗口。无结果或状态未完成均不算通过；实例为空时按 Unity skill 的正式连接流程检查，不能把工具离线写成业务回归。

---
name: 3c-unity-mcp
description: 操作 3C 项目的 Unity Editor、执行正式构建或恢复 Unity MCP 连接时使用，补充项目实例选择、代码写入、batchmode 和作业查询规则。与通用 unity-mcp-orchestrator 配合；仅阅读源码或文档时不触发编辑器操作。
---

# 3C Unity 操作

先读取当前可用的 `unity-mcp-orchestrator`，沿其正式 MCP/CLI 能力发现与连接流程执行。若存在多个同名来源，优先使用带本机 CLI 连接参考的版本。本 skill 只维护 3C 专属规则，不复制通用工具文档。

## 项目与执行边界

- 从当前工作区确定 Unity 项目 `3cDemo/Client/3C_Client`。发现实例后核对 `project_path`；不能用显示名替代路径。每次编辑器请求显式传 `unity_instance`，CLI 传纯实例 hash，不使用 `set_active_instance` 或全局 `instance set`。
- 代码通过系统文件工具写入。Unity MCP 用于已授权的编辑器操作与状态检查，不用于写代码文件。
- 刷新、编译或构建前确认目标 Editor 已退出 Play；若其他任务占用，说明具体占用。Unity 正在编译时不改代码、不重复刷新或启动另一轮构建。
- 项目允许通过正式 CLI/executeMethod 按明确项目路径运行本机 Unity batchmode，任务结束后退出该进程并保留主验收 Editor；CI 的 Unity 禁令不变。当前会话的进程启动限制仍然有效，不能把此条当作绕过限制的授权。
- MCP 断开先按通用连接流程判断服务器、域重载和首次连接状态，不注入临时脚本、不改用界面自动化、不无故重启 Editor。batchmode 仅服务本次已授权的正式工作，不作为掩盖 MCP 故障的替代执行路径。

## 项目作业入口

- 回放、Foot 与 Presentation 诊断使用 [3C 同输入回放验证](../3c-replay-verified-change/SKILL.md)；CPU 与托管分配采集使用 [3C 性能诊断](../3c-performance-diagnostics/SKILL.md)。它们提供业务操作入口，连接机制仍由通用 skill 维护。
- 构建、回放或采集请求结果不确定时，先查询原 `job_id`/RunId，不重复提交。若当前操作由 3C Development Center 的独立 RunHost 管理，使用它的正式 `status` 命令查询，不依赖 Editor 重连，不改写状态文件。
- [3C 并行开发验证](../3c-fast-development-validation/SKILL.md) 仅用于已明确的并行开发；普通 Unity 操作不自动引入 Center 改动记录、新 worktree 或其他 Agent。

## 连接验证记录的范围

2026-09-04 在 Unity 2022.3.62f2c1 / `3C_Client` 验证过本机 `.venv` CLI 连接和实例发现；性能 Build 期间出现 503，Domain Reload 后同一实例重新注册，CLI 再次成功，未切换全局实例。

当日更早的首次连接由用户点击恢复。这只记录域重载后的恢复证据，不证明 CLI 能唤醒已停止的首次连接，也不能代替本次实例状态检查。

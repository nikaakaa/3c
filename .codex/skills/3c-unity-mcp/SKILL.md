---
name: 3c-unity-mcp
description: 操作 3C 项目的 Unity Editor、执行正式构建或恢复 Unity MCP 连接时使用，补充项目实例选择、代码写入、batchmode 和作业查询规则。与通用 unity-mcp-orchestrator 配合；仅阅读源码或文档时不触发编辑器操作。
---

# 3C Unity 操作

先读取当前可用的 `unity-mcp-orchestrator`，沿其正式 MCP/CLI 能力发现与连接流程执行。若存在多个同名来源，优先使用带本机 CLI 连接参考的版本。本 skill 只维护 3C 专属规则，不复制通用工具文档。

## 项目与执行边界

- 从当前工作区确定 Unity 项目 `3cDemo/Client/3C_Client`。发现实例后核对 `project_path`；不能用显示名替代路径。每次编辑器请求显式传 `unity_instance`，CLI 传纯实例 hash，不使用 `set_active_instance` 或全局 `instance set`。
- 代码通过系统文件工具写入。Unity MCP 用于已授权的编辑器操作与状态检查，不用于写代码文件。
- 新增源码文件尚未导入时，脚本范围刷新可能只请求编译（refresh_triggered=false），导致已有源码找不到新类型。先核对该路径的 MonoScript 是否存在；确认缺失且 Editor 非编译/非 Play 后，使用正式 refresh_unity 的 scope=all 导入，再检查编译。不要凭类型找不到就更改程序集依赖或移动文件。
- 刷新、编译或构建前确认目标 Editor 已退出 Play；若其他任务占用，说明具体占用。Unity 正在编译时不改代码、不重复刷新或启动另一轮构建。
- `isCompiling=false` 只表示编译已停止。构建前同时确认 `EditorUtility.scriptCompilationFailed=false`，并读取 Console 中的编译错误；不能把带错误结束的编译当成通过。
- 项目允许通过正式 CLI/executeMethod 按明确项目路径运行本机 Unity batchmode，任务结束后退出该进程并保留主验收 Editor；CI 的 Unity 禁令不变。当前会话的进程启动限制仍然有效，不能把此条当作绕过限制的授权。
- MCP 断开先按通用连接流程判断服务器、域重载和首次连接状态，不注入临时脚本、不改用界面自动化、不无故重启 Editor。batchmode 仅服务本次已授权的正式工作，不作为掩盖 MCP 故障的替代执行路径。

## 项目作业入口

- 排查资产加载失败时，读取完整 Console 与 Editor 导入日志。本机版本的 `editor console --type error` 曾漏掉 Unity 原生资产导入错误，而 `--type all` 能返回这些条目；仅 error 查询为空不能作为资产已修复的证据，必须确认目标资源实际加载及业务引用。
- 当前普通资产文件若仍包含旧 Git LFS 指针，先按指针的 SHA-256 与 size 确认本地原对象。被 `.gitignore` 排除、未进入 Git 索引的文件不会由 `git lfs checkout` 恢复；本地对象完整时使用 `git lfs smudge` 读取原内容，完整输出并核对身份后替换指针文件，保留 `.meta`，再在目标 Editor 批量导入和验证引用。这是旧文件恢复，不改变当前仓库的资产管理规则。
- 技能运行数据契约变化后，使用正式技能发布器重建目标 Definition 的 Fixed / Float32 产物；不在 Runtime 猜测旧声明绑定或补默认值。技能重建菜单只负责指定 Definition 和输出目录，技能输入由 GameplayAbilityExecutionDataAssetPublisher 与编译器确认，不把独立的 Pose、动画资源或相机配置校验放到这条 Build 链之前。
- `Tools/3C/Internal/Republish Corin Ability Data` 复用同一发布器。菜单返回 attempted 或客户端超时都不代表成功或失败；先检查原操作是否已写入产物，再用 Definition 的正式 Load 及技能安装入口确认契约，不能重复发起重建。
- 回放、Foot 与 Presentation 诊断使用 [3C 同输入回放验证](../3c-replay-verified-change/SKILL.md)；Windows IL2CPP Player 的 CPU 与托管分配采集使用 [3C 性能诊断](../3c-performance-diagnostics/SKILL.md)。Editor 性能按下节直接读取当前 Editor Profiler；连接机制仍由通用 skill 维护。
- 构建、回放或采集请求结果不确定时，先查询原 `job_id`/RunId，不重复提交。若当前操作由 3C Development Center 的独立 RunHost 管理，使用它的正式 `status` 命令查询，不依赖 Editor 重连，不改写状态文件。
- [3C 并行开发验证](../3c-fast-development-validation/SKILL.md) 仅用于已明确的并行开发；普通 Unity 操作不自动引入 Center 改动记录、新 worktree 或其他 Agent。

## Editor Profiler

- Editor Play、运行桥接和窗口绘制卡顿直接采目标 Editor 的 CPU 调用树。Player 构建与采样不包含这些 Editor 调用，不能代替 Editor 诊断。
- 在 Unity 2022.3.62f2c1 已确认：开启 CPU area、`ProfilerDriver.profileEditor` 和 `Profiler.enabled` 后，`EditorLoop` 会包含编辑器子调用。`ProfilerDriver.GetRawFrameDataView(frame, thread)` 返回 `UnityEditor.Profiling.RawFrameDataView`。优先保持 Deep Profile 关闭；域重载或新采样覆盖旧帧前，使用 `ProfilerDriver.SaveProfile` 将原始帧保存到 Assets 外。
- 记录同一帧对应的 Play、RuntimeDebug attachment、具体 Graph/Timeline 和执行状态。Detached 或无活跃技能、无具体 Graph 的帧不能证明桥接开启后或绘制中的性能；输入与窗口状态不同的数据不能作为性能 A/B。

## 连接验证记录的范围

2026-09-04 在 Unity 2022.3.62f2c1 / `3C_Client` 验证过本机 `.venv` CLI 连接和实例发现；性能 Build 期间出现 503，Domain Reload 后同一实例重新注册，CLI 再次成功，未切换全局实例。

当日更早的首次连接由用户点击恢复。这只记录域重载后的恢复证据，不证明 CLI 能唤醒已停止的首次连接，也不能代替本次实例状态检查。

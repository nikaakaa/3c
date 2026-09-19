# 实施记录

## 工作位置

- Goal：按本 change 完整实施，尚未完成。
- 分支：`codex/timeline-seconds-passive`。
- worktree：`D:/Unity_Project_1/3C-worktrees/timeline-seconds-passive`。
- 起点：`cdb06993e`；主目录未提交改动未复制或修改。
- Center change_id：`096ec1cf6812476495c366ae33bb9236`。

## 已完成与进行中

- 0.1 已完成：核对 `Simulation/Core/Fixed/Numerics/SimulationNumerics.cs`，选择与现有数值合同一致的 Q32.32 秒、nearest-even 输入舍入和累计余数规则，已写回 design。未把数值决定当作字段／资产迁移完成。
- 7.1 局部修复：原 Presentation driver 在 CommitStop 后只写 StopCommitted，后续 TryPresent 未读取；现于采样前退出并移除表现状态，禁止已接受停止的旧实例继续产生 Marker。完整终态／修正接线仍未完成，任务不勾选。
- 现有运行链含逐帧 List／结果对象分配，尚未完成 0 GC 迁移，不声称性能条件已满足。

## 修改前编译

- Run：`76b1360448ec47ed9a59744706edfe9d`，正式 RunHost 2.1.3 的 compile 工作流。
- 结果：Faulted，Unity 在 Package Manager 解析阶段退出，未进入 C# 编译。
- 原错误：`Failed to resolve packages: The "path" argument must be of type string. Received undefined. No packages loaded.`
- 日志：`D:/Unity_Project_1/3C-Artifacts/3c-gameplay/Runs/76b1360448ec47ed9a59744706edfe9d/Logs/unity-editor.log`。
- 已核对本 worktree manifest 的所有直接 file 依赖均存在 package.json，Unity 版本与 ProjectVersion 一致；未通过复制缓存、删包或 fallback 绕过，根因尚未确定。
- 当前没有成功编译或回放证据，主目录已有运行结果不能替代本 worktree。

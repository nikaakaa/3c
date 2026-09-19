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

## Marker 私有图重建候选

- 独立 Timeline 根现在通过 `BtsmtlSkillGraphClosure.Validate(TimelineAsset)` 收集 TreeClip / Marker 图，沿同一正式闭包处理节点、连线、Blackboard、嵌套内容与稳定身份。
- `EmitTimelineRoot` 复用原图注册、创建、配置、连接和清理阶段；Marker 私有图缺失闭包时明确失败，删除旧子资产路径代替重建的分支。
- `EnsureTimelineGraph` 复用编辑器的 `TimelineGraphAuthoring.EnsureSubAsset`，保留 TimelineBody / TimelineTrigger 角色，不另建生成器专用图创建规则。
- 任务 8.5 仍未勾选：候选尚未通过完整编译与真实导出／重建。

## 编译环境与依赖定位

- Run `17776ed34b634547ae425be647693db6` 再次在相同 UPM 阶段失败。
- 已定位环境原因：工具进程缺少 `ALLUSERSPROFILE`；安装的 UPM 在 `getGlobalConfigRoot` 中直接用它拼接路径。按 Windows `CommonApplicationData` 补齐正式子进程环境后，再使用同一 RunHost；未改包源、复制缓存或新增编译入口。
- Run `b7382578158f46c09b90e501cdcca17a` 已完成包解析并进入 C# 编译，最终 Faulted。四处独立错误均为现行基线缺少 `CharacterClipPlayerClockSource`，位于 `AnimationClipPlayer.cs:365` 和 `AnimationPhaseProjectionPlan.cs:286/295/321`；后续编译阶段尚无通过证据。
- 对应任务：`add-network-model-locomotion-presentation-policy`；提交 `12942171e` 已删除旧类型。主目录这两个运行文件及 `CharacterPoseNodeDefinitions.cs`、`CharacterPoseNativeDomainResourceSetCompiler.cs` 有尚未提交的消费者迁移。未恢复旧类型，未接管或复制该批未提交改动；已向用户询问依赖接收方式。
- 冷导入删除了 7537 个缺少配对资产的已跟踪 `.meta`，已仅恢复本 worktree 这些自动删除的原文件，未动主目录。UPM 自动从 lock 移除两项本 worktree 不存在的 embedded FSR 包；该 lock 差异未计入 Timeline 提交。
- 该 Run 已结束，进程 53988 已退出，可以继续修改候选源码；完整编译仍需先收口上述正式依赖。

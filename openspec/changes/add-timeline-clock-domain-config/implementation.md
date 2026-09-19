# 实施记录

## 工作位置

- Goal：按本 change 完整实施，尚未完成。
- 唯一实施分支与目录：`main`，`D:/Unity_Project_1/3C`。按用户明确要求，后续不创建或使用其它 worktree。
- 当前 Center change_id：`fa4c33f9eb594240994f011afd2996b6`，绑定主目录。
- 本次误用的 worktree 成果已通过 `802abd8c5` 全量迁回主线，并归并原提交历史；主目录原有未提交改动保持。
- 原 Center 记录 `096ec1cf6812476495c366ae33bb9236` 和其 Run 只作历史证据，不再用于继续实施或主线验收。

## 工作位置纠正

- 原分支 `codex/timeline-seconds-passive` 从 `cdb06993e` 建立，形成 `20a04cb79`、`bce79a177`、`ffbcc4eff` 三个提交，共 11 个文件。三个提交的全部增量与历史均已归入主线，不保留独立实现路径。
- 历史归并提交为 `43550907e`；原 worktree 目录与 `codex/timeline-seconds-passive` 分支已删除。
- 迁入时分别向工作区和索引应用本任务增量；作者适配器原有执行域导出修改仍为主目录原先的未提交内容，未被覆盖或混入本任务提交。
- 原 worktree 额外的 package lock 差异和自动生成的 Timelines.meta 来自冷导入；主目录已有正式包与目录身份，因此不迁入这两项自动变化。
- 撤销“必须等待 locomotion 任务另行提交后才能继续”的当前阻塞判断：其消费者迁移本来就在主目录。此前失败反映错误 worktree 的不完整基线，不代表主线缺少该批源码。后续基于主目录实际状态继续，不恢复已删除旧类型。
- 所有历史编译结果都不能证明迁回后的主线通过编译、运行或完整功能验收。任务仍按实际完成范围勾选。

## 已完成与进行中

- 0.1 已完成：核对 `Simulation/Core/Fixed/Numerics/SimulationNumerics.cs`，选择与现有数值合同一致的 Q32.32 秒、nearest-even 输入舍入和累计余数规则，已写回 design。未把数值决定当作字段／资产迁移完成。
- 7.1 局部修复：原 Presentation driver 在 CommitStop 后只写 StopCommitted，后续 TryPresent 未读取；现于采样前退出并移除表现状态，禁止已接受停止的旧实例继续产生 Marker。完整终态／修正接线仍未完成，任务不勾选。
- 现有运行链含逐帧 List／结果对象分配，尚未完成 0 GC 迁移，不声称性能条件已满足。

## 原 worktree 修改前编译（历史）

- Run：`76b1360448ec47ed9a59744706edfe9d`，正式 RunHost 2.1.3 的 compile 工作流。
- 结果：Faulted，Unity 在 Package Manager 解析阶段退出，未进入 C# 编译。
- 原错误：`Failed to resolve packages: The "path" argument must be of type string. Received undefined. No packages loaded.`
- 日志：`D:/Unity_Project_1/3C-Artifacts/3c-gameplay/Runs/76b1360448ec47ed9a59744706edfe9d/Logs/unity-editor.log`。
- 当时已核对该 worktree manifest 的所有直接 file 依赖均存在 package.json，Unity 版本与 ProjectVersion 一致；UPM 根因见下方历史定位。
- 该次失败不作为当前主线的编译结论。

## Marker 私有图重建候选

- 独立 Timeline 根现在通过 `BtsmtlSkillGraphClosure.Validate(TimelineAsset)` 收集 TreeClip / Marker 图，沿同一正式闭包处理节点、连线、Blackboard、嵌套内容与稳定身份。
- `EmitTimelineRoot` 复用原图注册、创建、配置、连接和清理阶段；Marker 私有图缺失闭包时明确失败，删除旧子资产路径代替重建的分支。
- `EnsureTimelineGraph` 复用编辑器的 `TimelineGraphAuthoring.EnsureSubAsset`，保留 TimelineBody / TimelineTrigger 角色，不另建生成器专用图创建规则。
- 任务 8.5 仍未勾选：候选尚未通过完整编译与真实导出／重建。

## 原 worktree 编译环境与依赖定位（历史）

- Run `17776ed34b634547ae425be647693db6` 再次在相同 UPM 阶段失败。
- 已定位环境原因：工具进程缺少 `ALLUSERSPROFILE`；安装的 UPM 在 `getGlobalConfigRoot` 中直接用它拼接路径。按 Windows `CommonApplicationData` 补齐正式子进程环境后，再使用同一 RunHost；未改包源、复制缓存或新增编译入口。
- Run `b7382578158f46c09b90e501cdcca17a` 已完成包解析并进入 C# 编译，最终 Faulted。四处独立错误均为现行基线缺少 `CharacterClipPlayerClockSource`，位于 `AnimationClipPlayer.cs:365` 和 `AnimationPhaseProjectionPlan.cs:286/295/321`；后续编译阶段尚无通过证据。
- 当时定位到 `add-network-model-locomotion-presentation-policy` 的消费者迁移位于主目录未提交改动；该记录仅解释旧 worktree 失败原因。当前已按上方“工作位置纠正”归回主线，不再等待独立依赖交接。
- 冷导入删除了 7537 个缺少配对资产的已跟踪 `.meta`，已仅恢复本 worktree 这些自动删除的原文件，未动主目录。UPM 自动从 lock 移除两项本 worktree 不存在的 embedded FSR 包；该 lock 差异未计入 Timeline 提交。
- 该 Run 已结束，进程 53988 已退出；旧 worktree 不再承担实施或验证，后续状态以主目录为准。

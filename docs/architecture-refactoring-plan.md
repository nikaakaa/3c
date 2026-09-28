# 角色运行与作者底层重构计划

更新：2026-09-29。

本计划已统一纳入 OpenSpec：[角色运行与作者底层职责整理](../openspec/changes/refactor-character-runtime-and-authoring-boundaries/proposal.md)。本文件只保留入口，不再维护第二份实施清单。

- [完整范围、当前状态、方案取舍与长任务排查协议](../openspec/changes/refactor-character-runtime-and-authoring-boundaries/design.md)：覆盖 Pose 帧事务、Timeline、Session 历史、Foot 采样、Pose Workspace、TreeDesigner 残留、多类型大文件、BlendStack 保留理由、闭环／性能证据与规格收尾。
- [实施切片草案](../openspec/changes/refactor-character-runtime-and-authoring-boundaries/tasks.md)：只记录实施项；后续先完整排查并给出方案，不因文档存在自动执行代码。
- 当前架构合同仍由 [project.md](../openspec/project.md) 与现行 specs 共同拥有；提案、提交和运行验证是不同状态。

此前“本轮完成”的表述仅覆盖 Pose／Timeline 等已提交切片，不代表所有架构候选已经处理。2026-09-29 核对时，SessionHistory 与 Foot AnimationSampler 仍有未提交代码；Pose Workspace、旧依赖和文件组织等仍需后续收口。最新状态以 OpenSpec design 的带日期记录及实际工作区为准。

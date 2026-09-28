# OpenSpec 现行文档索引

更新：2026-09-29。

本文只维护现行文档的读取入口和边界，不记录已完成比例、旧工作区状态、历史构建结果或实现快照。那些内容会随迁移快速失真，不能再作为当前架构或待办的依据。

## 真相归属

- 当前产品边界与整体装配口径由 [project.md](project.md) 和 `openspec/specs/` 共同拥有。
- 每个未归档 change 只拥有尚未收口的增量；`proposal.md` 说明范围，`design.md` 说明取舍，`tasks.md` 说明实施项。目录仍存在不代表能力已交付。
- `changes/archive/`、协调记录、实验、Replay 和构建日志只保留当时事实，不能反向成为当前实现入口。
- 已删除的 change 目录、整角色 `CharacterSimulationProgram`、`ProgramCatalog`、统一 `Projection` 和 Character Build 链路都不再是可引用的当前合同；需要历史原因时查 archive 或 Git 历史。

## 未归档 Change

| change | 入口 |
| --- | --- |
| 角色运行与作者底层职责整理 | [refactor-character-runtime-and-authoring-boundaries](changes/refactor-character-runtime-and-authoring-boundaries/proposal.md) |
| 编译期性能采样 | [add-compile-time-performance-instrumentation](changes/add-compile-time-performance-instrumentation/proposal.md) |
| 生成诊断采样框架 | [add-generated-diagnostic-sampling-framework](changes/add-generated-diagnostic-sampling-framework/proposal.md) |
| 网络模型 locomotion 表现策略 | [add-network-model-locomotion-presentation-policy](changes/add-network-model-locomotion-presentation-policy/proposal.md) |
| 开放式 TreeClip 预览 | [add-open-ended-treeclip-preview](changes/add-open-ended-treeclip-preview/proposal.md) |
| Timeline 时钟域 | [add-timeline-clock-domain-config](changes/add-timeline-clock-domain-config/proposal.md) |
| 作者、预览与运行调试工作台 | [design-btsmtl-authoring-runtime-workbench](changes/design-btsmtl-authoring-runtime-workbench/proposal.md) |
| Corin authoring Replay 闭环 | [integrate-corin-dump-authoring-replay](changes/integrate-corin-dump-authoring-replay/proposal.md) |
| 原生 FSM Skill authoring | [integrate-native-fsm-skill-authoring](changes/integrate-native-fsm-skill-authoring/proposal.md) |
| Pose FlowCanvas 编辑预览 | [integrate-pose-flowcanvas-editor-preview](changes/integrate-pose-flowcanvas-editor-preview/proposal.md) |
| ZZZ Camera 重建 | [rebuild-character-camera-from-zzz](changes/rebuild-character-camera-from-zzz/proposal.md) |
| Pose Graph 只读 Blackboard | [refine-pose-graph-readonly-blackboard](changes/refine-pose-graph-readonly-blackboard/proposal.md) |
| Behavior Designer AI 接入 | [replace-btsmtl-ai-with-behavior-designer](changes/replace-btsmtl-ai-with-behavior-designer/proposal.md) |
| Foot Path 与 Landing 稳定化 | [stabilize-character-foot-path-and-landing](changes/stabilize-character-foot-path-and-landing/proposal.md) |

## 维护规则

- 新增、归档或删除 change 时，只更新本表和受影响的现行 spec；不把 archive 的任务或实现记录复制回来。
- 当前规范出现的历史名词只能用于说明“已删除什么”，不得重新描述为运行依赖或待实施入口。
- 活跃规划若需要引用历史设计，链接必须明确指向 `changes/archive/`；不能指向已删除的原目录。
- `docs/` 中的参考资料、`Diagnostics/` 中的 Replay 归档和 `Tools/` 的工具说明按各自用途保留，不并入 OpenSpec 当前合同。

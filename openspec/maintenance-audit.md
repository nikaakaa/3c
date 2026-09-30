# OpenSpec 现行文档索引

更新：2026-09-30。

本文只维护现行文档的读取入口和边界，不记录已完成比例、旧工作区状态、历史构建结果或实现快照。那些内容会随迁移快速失真，不能再作为当前架构或待办的依据。

## 真相归属

- 当前产品边界与整体装配口径由 [project.md](project.md) 和 `openspec/specs/` 共同拥有。
- 每个未归档 change 只拥有尚未收口的增量；`proposal.md` 说明范围，`design.md` 说明取舍，`tasks.md` 说明实施项。目录仍存在不代表能力已交付。
- `changes/archive/`、协调记录、实验、Replay 和构建日志只保留当时事实，不能反向成为当前实现入口。
- 普通业务进展、抄录与参考的统一导航入口为 [docs/README.md](../docs/README.md)；历史实施记录按主题保存在 `docs/archive/records/`。
- 已删除的 change 目录、整角色 `CharacterSimulationProgram`、`ProgramCatalog`、统一 `Projection` 和 Character Build 链路都不再是可引用的当前合同；需要历史原因时查 archive 或 Git 历史。

## 未归档 Change

| change | 入口 |
| --- | --- |
| 编译期性能采样 | [add-compile-time-performance-instrumentation](changes/add-compile-time-performance-instrumentation/proposal.md) |
| 生成诊断采样框架 | [add-generated-diagnostic-sampling-framework](changes/add-generated-diagnostic-sampling-framework/proposal.md) |
| 正常运行托管分配清理 | [eliminate-runtime-managed-allocations](changes/eliminate-runtime-managed-allocations/proposal.md) |
| 作者、预览与运行调试工作台 | [design-btsmtl-authoring-runtime-workbench](changes/design-btsmtl-authoring-runtime-workbench/proposal.md) |
| Corin authoring Replay 闭环 | [integrate-corin-dump-authoring-replay](changes/integrate-corin-dump-authoring-replay/proposal.md) |
| Corin Rush 正式动作链 | [add-corin-rush-attack-formal-chain](changes/add-corin-rush-attack-formal-chain/proposal.md) |
| Pose FlowCanvas 编辑预览 | [integrate-pose-flowcanvas-editor-preview](changes/integrate-pose-flowcanvas-editor-preview/proposal.md) |
| ZZZ Camera 重建 | [rebuild-character-camera-from-zzz](changes/rebuild-character-camera-from-zzz/proposal.md) |
| Pose Graph 只读 Blackboard | [refine-pose-graph-readonly-blackboard](changes/refine-pose-graph-readonly-blackboard/proposal.md) |
| Behavior Designer AI 接入 | [replace-btsmtl-ai-with-behavior-designer](changes/replace-btsmtl-ai-with-behavior-designer/proposal.md) |
| Foot Path 与 Landing 稳定化 | [stabilize-character-foot-path-and-landing](changes/stabilize-character-foot-path-and-landing/proposal.md) |

## 已完成变更

2026-09-30 已归档角色职责重构、网络 locomotion 表现策略、骨骼变换与侧倾、TurnBack 运动/相位、Timeline 时钟、开放式 TreeClip 观察及原生 FSM authoring。有效合同由现行规格承接，被后续架构取代的旧 delta 不重新安装。目录、承接方式及整理验证见[文档整理报告](../docs/archive/maintenance/document-cleanup-20260930.md)。

## 维护规则

- 新增、归档或删除 change 时，只更新本表和受影响的现行 spec；不把 archive 的任务或实现记录复制回来。
- 当前规范出现的历史名词只能用于说明“已删除什么”，不得重新描述为运行依赖或待实施入口。
- 活跃规划若需要引用历史设计，链接必须明确指向 `changes/archive/`；不能指向已删除的原目录。
- `docs/` 中的参考资料、`Diagnostics/` 中的 Replay 归档和 `Tools/` 的工具说明按各自用途保留，不并入 OpenSpec 当前合同。

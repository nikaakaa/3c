# 项目文档入口

当前业务和架构从 [project.md](../openspec/project.md) 与[现行规格](../openspec/specs/)读取。未完成实施从[活跃变更索引](../openspec/maintenance-audit.md)读取；历史记录中的“当前”“待完成”和编译阻塞只表示记录当时的现场。

## 按业务查找

| 要解决的问题 | 入口 |
| --- | --- |
| 角色输入、移动、动作、动画与运行装配 | [现行规格与活跃变更](../openspec/maintenance-audit.md) |
| Foot Placement 的未解决问题与采样证据 | [脚部进展](status/foot-placement.md) |
| 默认轨道、Delay、Zoom/Stretch、震动与命中消费者 | [相机进展](status/camera.md) |
| 普攻、Rush、接招、命中与 Replay 证据 | [战斗进展](status/combat.md) |
| Corin 源数据、控制器与作者对照 | [抄录对照](replication/README.md) |
| CPU、托管分配、Player 构建与报告口径 | [性能采集工具](../Tools/ThirdPersonPerformanceCapture/README.md) |
| 网络测试会话、进程与产物 | [网络测试工具](../Tools/ThirdPersonNetworkTest/README.md) |
| 渲染恢复、参数与原始来源 | [Rendering](../Tools/Rendering/README.md)、[可琳渲染数据](../Tools/Rendering/CorinRenderData/README.md) |
| 粒子与特效 Transfer 的当前取证 | [原生 Transfer 证据](diagnostics/effect-particle-native-transfer-20260930.md) |
| 学习资料、源程序研究与失败经验 | [参考资料](reference/README.md) |
| 阶段交付、旧工作区、失败运行与已完成设计 | [历史记录](archive/README.md)、[OpenSpec archive](../openspec/changes/archive/) |

## 文档归属

- `openspec/project.md`、`openspec/specs/`：当前业务合同、架构与职责，不存放实施流水账。
- `openspec/changes/<change>/`：仍在实施的明确增量；完成后按归档流程退出活跃索引。
- `docs/status/`：各业务的证据入口和未闭环范围，不复制任务清单、参数表或运行状态。
- `docs/replication/`：源数据和正式作者对照；源帧号保留为来源单位，不能当作第二份 Timeline 作者时间。
- `docs/reference/`：可长期复用的研究、学习和经验，不能作为默认配置或已经实现的证明。
- `docs/diagnostics/`：仍在维护的实施/审计文档及原始证据目录；采样、JSON、取证指令和交互解释器保留原路径。
- `docs/archive/records/`：按主题保存阶段记录。归档记录不表示相应整个业务已经验收。
- `Tools/<tool>/README.md`：工具的正式操作入口；原始参数合同和工具数据跟随所属工具。

本轮整理的范围、移动/合并/删除明细和验证见[2026-09-30 整理报告](archive/maintenance/document-cleanup-20260930.md)。

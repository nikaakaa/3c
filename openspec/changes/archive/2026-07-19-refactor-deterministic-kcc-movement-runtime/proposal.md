# Change: 将 Deterministic KCC 重构为成熟的连续碰撞移动内核

## Why

当前 `DeterministicKccWorldSolver` 已接入 Fixed Program、Rollback Session、World Snapshot 与 Actor batch，但静态世界移动内核仍是简化实现：胶囊 sweep 主要通过“终点是否重叠”决定是否继续二分，角色从起点到终点穿过薄墙而终点无重叠时可能漏检；grounding、wall slide、step 与 penetration recovery 也尚未形成成熟 KCC 所需的统一接触流形、稳定地面判定和多平面约束。

这会直接影响当前 2v2vE 技术 Demo：冲刺、攻击位移、坡面移动、墙角滑动、台阶与地形边缘都可能产生穿透、卡角、错误吸地或不同 Peer 结果。继续依赖外部分段只能降低漏检概率，不能把它变成连续碰撞检测。

本 change 不再增加一套 Solver，也不修改 Rollback 网络模型。它原位替换现有简化 Fixed KCC 查询与移动内核，以 Philippe KCC 的角色运动行为、Rapier/Parry 的 shape-cast/KCC 结构和 PhysX CCT 的接触恢复策略作为参考，建立适用于当前业务范围的 Fixed Q32.32 成熟实现。

## What Changes

- 原位替换 `DeterministicCapsuleQueries` 的终点重叠式 sweep，建立有界、连续、确定性的胶囊 shape cast、overlap、distance 与 penetration 查询。
- 将静态 Primitive 降低为统一的 Fixed closest-feature 合同；Plane 使用解析路径，Triangle 与 Box 使用精确胶囊轴最近特征查询，再由同一保守推进器完成连续 cast。
- 为每个接触输出稳定 PrimitiveId、FeatureId、TOI、法线、见证点与 penetration/separation，并按固定规则形成 canonical contact set。
- 将角色移动重构为单一 Fixed KCC Motor：初始去穿透、连续 sweep、多平面 collide-and-slide、稳定 grounding、坡度限制、step up/down、ground snap 与最终 overlap 校验共享同一查询合同。
- 明确区分“碰到地面”和“稳定站立地面”，记录 support primitive/feature、稳定法线与上 Tick support，使坡面、边缘和下坡不再只依赖 `normal.y` 一次判断。
- 将 step 处理为完整的上抬、前探、下探、稳定落点确认事务；任一步失败即放弃整个 step candidate，不部分提交。
- 将 Unity MeshCollider 与 TerrainCollider 的静态作者数据编译到同一 canonical indexed-triangle artifact，并生成稳定 triangle adjacency/共享边身份；运行时不增加 Terrain 专用 Unity 查询路径。
- 保留现有 `ResolveBatch` 外层合同与 `DeterministicActorContactSolver`：先计算全部 Actor 的静态世界 candidate，再做 stable ActorId pair contact，再执行静态世界重约束，最后原子提交完整 batch。
- 将会影响后续 Tick 的 ground/support 状态纳入 KCC world state、snapshot 与 hash；将查询算法版本、容差、容量、迭代上限和 artifact schema 纳入 KccId/WorldConfigurationHash。
- 将 Tick 热路径改为预分配、有界 buffer；容量溢出或迭代不收敛时明确失败，不扩大容量、不跳过接触、不回退 Unity Physics。
- 从正式 Unity package manifest/lock 中移除未被产品代码使用的 `com.janooba.kcc` 依赖。它只作为本地行为参考，不进入 Fixed Runtime、asmdef 或 Player 依赖图。
- 对移植自 Apache-2.0/BSD-3-Clause 参考实现的算法保留来源与许可证声明；不复制 Philippe KCC 的 Asset Store 源码。

## Non-Goals

- 不实现通用 Rigidbody、质量、冲量、旋转刚体、布料、载具或完整物理引擎。
- 不实现 moving platform、动态破坏、运行时动态 MeshCollider 或任意方向胶囊；这些需要单独的 world state、snapshot 与接触所有权设计。
- 不把重力、跳跃、冲刺方向、攻击击退或输入响应策略塞进 KCC；这些仍由 Program 产生 `CharacterMotionRequest`。
- 不修改 DeterministicRollback 的 UDP、canonical input、history、restore/replay、hash exchange 或 Presentation 语义。
- 不增加 Float32 Philippe KCC Solver。若以后需要替换 Unity CharacterController，它是独立 change。
- 不加入旧/新 KCC 模式、feature toggle、兼容字段、fallback 或双写资产。

## Dependencies

- 依赖 `add-deterministic-rollback-kcc-model` 已安装的 Fixed Program、Fixed World ABI、`DeterministicKccWorldSolver`、Collision World Artifact 与 Actor contact 合同。
- 本 change 可在 `add-deterministic-rollback-kcc-model` 尚未归档时实施，但归档顺序 MUST 为先归档该基础 change，再归档本 change。
- 本 change 与 DotRecast/ServerAuthoritative 分支可并行；它不得修改 Float32 Program ABI、DotRecast Solver 或 ServerAuthoritative 网络语义。
- 若 `refactor-simulation-tick-hot-path` 后续修改 Fixed KCC 热路径，共享文件 MUST 串行合并，并以本 change 的预分配 query layout 为唯一实现。

## Current Spec Comparison

- `character-motion-simulation-boundary` 已规定一个 SimulationStep 只调用一次当前 `ICharacterWorldSolver.ResolveBatch`。本 change 保持这一边界，不增加 Character 级 Solver 调用。
- `gameplay-simulation-session-composition` 已规定 Solver 由 Composition 显式选择并锁定 identity/capability。本 change 只升级同一个 Deterministic KCC 实现及其 identity，不增加运行时切换。
- `add-deterministic-rollback-kcc-model` 的 delta 已要求 capsule cast/overlap、ground、slope、step、wall slide 与 penetration resolution；当前代码只完成了简化版本。本 change 增加可判定的连续查询、稳定地面、接触流形、地形编译和失败语义，使原要求真正可验收。
- 当前 specs 不允许 Fixed KCC 使用 Unity Physics 或 DotRecast。本 change 继续遵守，并明确第三方 KCC 只能作为算法/行为参考。
- 未发现现行 spec 要求 moving platform 或通用动态刚体，因此本 change 不扩大到这些能力。

## Impact

- 运行时：`Simulation/DeterministicKcc/Collision`、`Simulation/DeterministicKcc/Kcc`、Fixed world state/snapshot codec 与 diagnostics。
- Editor：Deterministic Collision World baker、artifact schema、Terrain/Mesh 静态表面编译与 identity 生成。
- 资产：Rollback Demo 的 Collision World Artifact、KCC profile/identity 与其引用需要按新 schema 正式重建，不保留旧 schema reader。
- 依赖：Unity manifest/lock 不再把 `com.janooba.kcc` 作为正式产品依赖；Fixed KCC 仍只依赖 portable Core 与 Fixed 数值模块。
- 文档：更新 `project.md` 与 rollback 实现清单，撤销“简化 KCC 已完整满足成熟移动能力”的过时描述。

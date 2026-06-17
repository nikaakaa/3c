## Context
本地 rollback 工具当前分成 core、simulation adapter、debug runner 和 recorder 多层，但 Unity 装配上容易被理解为“全部挂到角色上”。`LocalRollbackSynctestDebugRunner`、`LocalLatencyReconciliationDebugRunner` 和 `LocalRollbackSoakDebugRunner` 会在自身、父级和子级中扫描 `MonoBehaviour` 来寻找 recorder、simulation adapter 和 presentation helper；`FullBodyRollbackSimulation` 也通过层级解析角色 runtime 组件。

这种做法适合早期验证，但继续留在正式角色 prefab 上会产生两个风险：

- 角色正式 runtime 组件集合被测试支架污染，后续定位“谁在驱动角色”变困难。
- 自动层级扫描会掩盖装配错误，使 DebugRunner 引用到哪个 replay adapter 或 recorder 不够显式。

## Goals
- 把 rollback debug tooling 收敛到独立 `RollbackDebugRig` prefab。
- 保持 replay adapter 仍推进正式 `CharacterFrameRuntimeController` / `CharacterFramePipelineHost` 主线。
- 让正式角色 prefab 和正式场景实例不常驻 rollback debug runner / recorder / replay adapter。
- 用自动测试和静态边界校验防止工具重新长回角色 runtime。

## Non-Goals
- 不重写 rollback core、snapshot comparison 或 authority scope。
- 不把 replay adapter 改造成正式网络 rollback owner。
- 不把 Debug Rig 做成第二个角色 runtime host。
- 不用隐藏 fallback 或层级扫描替代正式引用缺失。

## Decisions

### Decision: Debug Rig 是独立 prefab，不是角色能力
F6/F7/F8、输入历史记录、快照记录、prediction source 和 replay adapter 归属 Debug Tooling / Simulation Adapter。它们可以是 MonoBehaviour，但正式开发入口 MUST 是独立 `RollbackDebugRig` prefab 的场景实例，而不是正式角色 prefab 的默认组件集合。EditMode 测试 MAY 使用 fixture，但 fixture 不替代 Debug Rig prefab 资产。

Rationale: 角色对象应该表达可玩角色运行时能力；Debug Rig prefab 表达验证工具。二者用显式引用连接，职责更清楚，也更容易在不同测试场景中复用。

### Decision: Replay Adapter 显式引用目标角色主线
`FullBodyRollbackSimulation` 或等价 adapter 继续通过 `CharacterFrameRuntimeController` 推进正式主线，但应从 Debug Rig 持有对目标角色 runtime 的显式引用。引用缺失时输出诊断并停止，不通过 fallback 默认配置或第二控制器继续运行。

Rationale: replay 验证的是当前主线是否可恢复、可重放；adapter 不应该自己成为一条新的主线。

### Decision: 自动查找只作为迁移辅助
实现期可以短暂保留 Reset/OnValidate 的自动填充，帮助迁移现有场景；正式运行时语义必须以显式引用为准。缺失关键引用时工具应 fail fast，而不是跨角色层级继续扫描并选择第一个匹配组件。

Rationale: Unity 早期装配可以借助自动填充省操作，但 runtime fallback 会隐藏错误。

### Decision: 先校验边界，再迁移场景
实施时先补静态/fixture 测试，明确哪些组件属于正式角色 runtime，哪些属于 Debug Rig prefab。随后创建或更新独立 prefab，再迁移 scene 中的工具装配，并补行为测试证明 F6/F7/F8 仍通过同一 pipeline。

Rationale: 先定边界可以避免迁移时产生又一套测试路径。

## Risks / Trade-offs
- 风险：拆出 Debug Rig 后现有快捷键工具可能因引用缺失不可用。
  - Mitigation: 增加装配测试，检查 Debug Rig 必需引用完整，并输出明确缺失字段。
- 风险：为了兼容旧场景保留自动扫描，导致边界没有真正收敛。
  - Mitigation: 静态边界测试限制正式 prefab / scene 不挂 debug tooling，并限制 runtime fallback 扫描不作为正式绑定。
- 风险：把 replay adapter 移到 Debug Rig 后被误认为不再验证真实角色主线。
  - Mitigation: 行为测试断言 adapter 仍调用目标角色的 `CharacterFrameRuntimeController` / `CharacterFramePipelineHost`。

## Migration Plan
1. 增加测试描述当前目标边界：正式角色对象不应持有 rollback debug tooling。
2. 增加 `RollbackDebugRig` prefab 装配测试，验证 runner、recorder、prediction source 和 replay adapter 的显式引用。
3. 调整 DebugRunner / Recorder / Adapter 的引用解析策略，使缺失引用变成诊断失败。
4. 迁移 Corin 正式 prefab / scene 中的 rollback debug tooling 到独立 `RollbackDebugRig` prefab 实例。
5. 运行定向 EditMode 测试和 OpenSpec 校验。

## Resolved Decisions
- Debug Rig 使用独立 prefab；场景中只放该 prefab 的实例。

## Open Questions
- 是否需要把 Debug Rig prefab 实例放入单独开发测试场景，而不是 Sandbox？这取决于当前场景使用频率，实施时应先保持可测试路径最短。

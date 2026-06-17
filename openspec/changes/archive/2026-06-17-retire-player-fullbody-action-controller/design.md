## Context
`promote-character-frame-runtime-controller` 已经把正式角色级入口推进到 `CharacterFrameRuntimeController`，`formalize-character-frame-arbitration-contract` 和 `retire-fullbody-integrated-frame-paths` 已经把 sibling submitter、`CharacterFramePlan` 和 legacy integrated path 的方向定下来。当前剩余问题是 `PlayerFullBodyActionController` 仍以组件名和类型依赖存在，导致旧 FullBody 主入口语义没有真正退场。

在删除 controller 之前，还存在一个更小但必须先处理的边界问题：`LocomotionFrameSubmitter` 与 `FullBodyActionFrameSubmitter` 仍可通过共享 `FullBodySubmissionBuilder` 汇合，最终输出来源仍可能表达为 `LegacyFullBodyIntegrated`。如果直接删除 controller，旧集成中心容易被搬进新的 runtime 类，形成“名字新、结构旧”的分裂路径。

## Goals / Non-Goals
- Goals:
  - 删除 `PlayerFullBodyActionController` 文件、组件绑定和生产端口依赖。
  - 保持唯一 `CharacterFrameRuntimeController -> CharacterFrameRuntimeHost -> CharacterFramePipeline` 主线。
  - 先拆开 Locomotion submitter、FullBody Action submitter 与 frame output source 的边界。
  - 将混杂职责拆到状态机运行时、FullBody Action runtime、output runtime、diagnostics 和 rollback adapter。
  - 保持现有 Locomotion、Dodge、TurnBack、rollback replay 行为等价。
- Non-Goals:
  - 不实现 Attack combo 本体。
  - 不新增 UpperBody、LowerBody、IK、AvatarMask 或并行状态机。
  - 不新增 fallback 配置。
  - 不重写统一状态机模型或 motion executor。

## Decisions
- Decision: `CharacterFrameRuntimeController` 是唯一 Unity gameplay runtime owner。
  - Reason: 它已经是角色级入口，保留 FullBody controller 会继续让 FullBody 看起来像第二主线。
- Decision: submitter 拆分先于 controller 删除。
  - Reason: 只有先让 Locomotion 与 FullBody Action 作为 sibling submitter 独立提交候选，后续删除 controller 才不会把 `FullBodySubmissionBuilder` 变成新的隐形大类。
- Decision: `LegacyFullBodyIntegrated` 不再作为正式 frame output source。
  - Reason: 该名称表达旧 FullBody 集成路径权威，会误导输出节点继续从 FullBody 集成中心而不是角色级仲裁结果派生。
- Decision: `CharacterStateMachineRuntime` 或等价模块拥有 `CharacterStateMachineRunner`、snapshot 和 restore。
  - Reason: runner 表达统一 FullBody/Locomotion/Action 状态树，不属于 Player，也不属于某个具体 Action controller。
- Decision: `FullBodyActionRuntime` 只承载 action request/config/policy/resistance/resolved action facts。
  - Reason: FullBody Action 是 Character pipeline 的 sibling submitter 能力，不是 Unity tick 入口。
- Decision: Output dependencies host 独立于 controller。
  - Reason: motion executor、animation presenter、input buffer、diagnostics 是 output apply 所需端口，不应通过一个 MonoBehaviour 大类间接暴露。
- Decision: rollback replay 复用角色级 host 和状态机 runtime restore。
  - Reason: replay 需要复用 live pipeline，不能继续把旧 controller Tick 当作重放主线。

## Risks / Trade-offs
- 风险：一次删除 controller 会影响大量测试 fixture 和 prefab YAML。
  - Mitigation: 先加静态边界测试和 prefab/scene binding 测试，再迁移 fixture，最后删除文件。
- 风险：runner owner 迁移可能导致 restore 后状态不一致。
  - Mitigation: 保留现有 FullBody restore state 数据结构语义，先迁移 owner，再跑 rollback replay 定向测试。
- 风险：output host 迁出时可能混入新的 fallback。
  - Mitigation: 所有 output dependency 缺失必须明确失败或跳过执行，不创建隐藏 executor/presenter。

## Migration Plan
1. 用测试锁定 `PlayerFullBodyActionController` 不再允许作为生产类型出现。
2. 先拆分 `LocomotionFrameSubmitter` 与 `FullBodyActionFrameSubmitter` 的 builder/port 边界。
3. 收口 frame output source，删除 `LegacyFullBodyIntegrated` 在正式路径中的权威含义。
4. 降级或删除 `FullBodyIntegratedFrameAdapter` 在生产图中的正式地位。
5. 引入状态机运行时模块并迁移 runner、snapshot、restore。
6. 引入 FullBody Action runtime/ports 并迁移 Dodge config、policy、resistance。
7. 迁出 output dependencies host。
8. 更新 `CharacterFrameRuntimeController` 和 runtime port 组合。
9. 迁移 rollback/snapshot recorder 和测试 fixture。
10. 更新 Corin prefab/scene 绑定并删除旧 controller 文件。

## Open Questions
- 无。用户已明确要求可以直接删除该 controller，并将混杂职责归属到合理模块。

## Context
`centralize-fullbody-hfsm-tree-data` 已经要求 FullBody HFSM 树资产只表达拓扑和绑定，不接管 Dodge 业务、动作动画、动作位移或打断策略。`add-dodge-action-profile` 已经要求动作逻辑只输出稳定 animation key，动画 Profile 不替代 Locomotion 配置。

当前落地仍有两个边界问题：
- 资产目录上，Dodge 动画 Profile 和 Locomotion 动画配置仍放在 `Statemachine/FullBody` 下。
- 代码上，`FullBodyActionSetSO` 作为动作配置入口同时引用 `ActionAnimationProfileSO`，让动作逻辑配置和动画表现配置混在一个定义里。

## Goals / Non-Goals
- Goals:
  - 让状态机配置集中，但只集中逻辑状态树和状态图。
  - 让动作逻辑配置和动作动画配置在代码类型上分开。
  - 让角色 prefab 通过同一个 FullBody 主调度入口显式引用逻辑配置和动画配置。
  - 让目录结构能直接看出逻辑状态、动作参数、动画表现三类资产归属。
- Non-Goals:
  - 不重写 Dodge runtime。
  - 不新增第二个 FullBody coordinator。
  - 不把 Animancer 播放参数塞进状态机配置。
  - 不让完整 Root Motion 接管动作或基础移动位移。
  - 不删除现有 log。

## Decisions
- Decision: `FullBodyHfsmTreeDefinitionSO` 保持中心状态树权威。
  - Reason: 后续 Roll、Jump、Attack 等 FullBody 节点必须从同一棵树接入，避免 per-action 状态树分裂。
- Decision: `FullBodyActionSetSO` 收窄为动作逻辑配置入口。
  - Reason: 运动参数和打断策略属于动作逻辑；动画 Profile 属于表现配置。拆开后可以替换角色动画而不改动作逻辑资产。
- Decision: 新增或调整 `FullBodyActionAnimationSetSO` 等价动画绑定入口。
  - Reason: 动画配置不能游离；它要通过显式 `ActionStateId -> ActionAnimationProfileSO` 绑定参与 FullBody 主入口。
- Decision: `PlayerFullBodyActionController` 作为装配交汇点。
  - Reason: 它已经是 FullBody 主调度入口，适合同时持有状态树、动作逻辑集和动作动画绑定集，但不应把三者合并成一个大资产。
- Decision: 资产移动必须保 GUID。
  - Reason: 当前 prefab 和子资产通过 GUID 引用，目录调整不能造成引用断裂。

## Proposed Directory Ownership
```text
Assets/Configs/3C/Statemachine/FullBody/
  DefaultFullBodyHfsmTreeDefinition.asset
  Locomotion/DefaultLocomotionStateGraph.asset

Assets/Configs/3C/Action/FullBody/
  CorinFullBodyActionSet.asset
  Dodge/DefaultDodgeActionConfig.asset
  Dodge/DefaultDodgeInterruptPolicySet.asset

Assets/Configs/3C/Animation/FullBody/Corin/
  CorinFullBodyActionAnimationSet.asset
  CorinDodgeActionAnimationProfile.asset

Assets/Configs/3C/Animation/Locomotion/Corin/
  DefaultRunLocomotionAnimationConfig.asset
  Bake/*MotionProfile.asset

Assets/Configs/3C/Animacer/Corin/
  Corin_TransitionLib.asset
  TransitionAsset/*.asset
  Pramater/*.asset
```

## Risks / Trade-offs
- Risk: 拆出动画绑定后，动画 Profile 变成游离配置。
  - Mitigation: 角色 prefab 的 FullBody 主调度入口必须显式引用动画绑定集，测试校验 `Action.Dodge` 能解析到 Profile。
- Risk: 目录移动导致 prefab 引用断裂。
  - Mitigation: 使用 Unity 资产移动或等价保 `.meta` 的文件移动，并用 EditMode 测试检查关键 GUID 引用。
- Risk: `ActionSet` 和状态树都配置 Action，形成两个权威。
  - Mitigation: 状态树只绑定 `ActionStateId`；ActionSet 只提供该 action 的逻辑配置；交叉校验只检查树中 action 是否能在 ActionSet 中解析。
- Risk: Locomotion 动画配置名称仍叫 `RunLocomotionAnimationConfigSO`，但已经包含 Walk/Run。
  - Mitigation: 本变更不强制重命名类型，先只修边界和目录；后续如要重命名另开 change。

## Migration Plan
1. 先调整代码类型边界和测试夹具。
2. 再移动资产目录并保留 GUID。
3. 最后更新 prefab 显式引用和路径测试。
4. 若任一步发现必须让动画配置反向驱动状态机或位移，停止实现并回到 OpenSpec。

## Open Questions
- `CorinFullBodyActionSet.asset` 最终放在 `Action/FullBody` 还是 `Statemachine/FullBody`：本 proposal 建议放在 `Action/FullBody`，因为它配置动作逻辑，不是状态树拓扑。

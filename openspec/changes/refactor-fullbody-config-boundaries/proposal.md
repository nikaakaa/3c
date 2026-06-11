# Change: 拆分 FullBody 逻辑状态配置和动画配置边界

## Why
当前 `Assets/Configs/3C/Statemachine/FullBody` 下同时放了 FullBody 树、Locomotion 状态图、Dodge 运动/打断配置、Dodge 动画 Profile 和 Locomotion 动画配置。代码里 `FullBodyActionSetSO` 也同时引用动作运动配置、打断策略和动作动画 Profile，导致“逻辑状态机配置集中”和“动画表现配置分离”的边界不够明确。

本变更用于把状态机拓扑和动画表现配置从代码与资产目录两层同时分开，同时保留一个 FullBody 主调度入口，避免产生第二套状态机、第二套动作入口或游离动画配置。

## What Changes
- 明确 `FullBodyHfsmTreeDefinitionSO` 只负责 FullBody 主状态树拓扑和 phase/action 绑定。
- 明确 Locomotion 状态图仍属于逻辑状态机配置，集中在 `Assets/Configs/3C/Statemachine/` 体系。
- 收窄 `FullBodyActionSetSO` 或等价动作主入口职责，使其只聚合动作逻辑、运动参数和打断策略，不直接持有动作动画 Profile。
- 新增或调整动作动画绑定入口，使 `ActionStateId -> ActionAnimationProfileSO` 归属于动画配置侧，并由 FullBody 主调度入口显式引用。
- 将现有配置资产重编排到状态机、动作逻辑和动画配置各自目录，移动时保留 GUID 和 prefab 引用。
- 增加 EditMode 测试、静态边界测试和 Play Mode 手动验证步骤，证明 Dodge、Locomotion 和动画替换行为没有回退。

## Impact
- Affected specs:
  - `fullbody-config-boundaries`
  - `project-structure`
- Related active changes:
  - `centralize-fullbody-hfsm-tree-data`
  - `add-dodge-action-profile`
  - `add-fullbody-action-framework`
- Affected code:
  - `Assets/Scripts/Character/Action/FullBody/Config/FullBodyActionSetSO.cs`
  - `Assets/Scripts/Character/Action/FullBody/Runtime/PlayerFullBodyActionController.cs`
  - `Assets/Scripts/Character/Animation/Config/`
  - `Assets/Tests/Editor/FullBodyActionFrameworkTests.cs`
  - `Assets/Tests/Editor/DodgeActionProfileTests.cs`
- Affected assets:
  - `Assets/Configs/3C/Statemachine/FullBody/**`
  - `Assets/Configs/3C/Action/FullBody/**`
  - `Assets/Configs/3C/Animation/**`
  - `Assets/Prefabs/Character/可琳.prefab`

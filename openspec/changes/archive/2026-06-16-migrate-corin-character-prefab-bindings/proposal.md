# Change: 迁移 Corin 角色 Prefab 与场景绑定

## Why
`可琳.prefab` 和 `可琳_Humanoid.prefab` 已经挂载角色主链组件并引用 `CorinCharacterConfig.asset`，但 YAML 中仍保留旧平铺配置字段和场景 override。若不单独迁移 prefab/scene，后续调试会继续看到旧字段、重复配置入口和 scene 实例覆盖，容易误判为运行时 fallback。

本变更负责将角色 prefab 和正式场景绑定收敛到角色配置根与正式 runtime 组件，不改变配置资产内部语义。

## What Changes
- 更新 `可琳.prefab` 和 `可琳_Humanoid.prefab` 的正式组件绑定。
- 清理 prefab 上的 legacy serialized config fields，使正式 Inspector 装配只依赖 `CharacterConfigSO` 和 runtime 组件引用。
- 同步 `Sandbox.unity`、CameraTest 等正式场景中角色实例的 override。
- 增加 prefab/scene 静态校验，防止旧字段、第二配置入口和旧 presenter 并存回流。
- 不修改 `CharacterConfigSO` 子配置语义，不合并 Animancer Presenter。

## Current Findings
- 只读检查显示 `可琳.prefab` 与 `可琳_Humanoid.prefab` 上的 `PlayerLocomotionController.characterConfig` 和 `PlayerFullBodyActionController.characterConfig` 都能解析到 `CorinCharacterConfig.asset`。
- 两个 prefab YAML 仍保留 `runAnimationConfig`、`config`、`stateMachineDefinition`、`interruptPolicySet`、`dodgeActionConfig` 等旧字段值。
- `可琳_Humanoid.prefab` YAML 出现过重复 `characterConfig` 序列化痕迹，Unity SerializedObject 实际读到正式根配置，但 YAML 残留仍需清理。
- `Sandbox.unity` 和 CameraTest 场景中存在角色组件或旧配置引用，prefab 正确并不代表场景实例 override 已正确。
- 当前角色仍同时存在 Locomotion Presenter 与 Action Presenter；统一 Presenter 的结构性替换由 `refactor-unified-animancer-presenter` 拥有。

## Detailed Scope
| Area | This change owns | This change must not own |
| --- | --- | --- |
| Prefab config binding | 清理旧配置字段残留，保持 controller 指向正式 root config。 | 修改子配置资产语义。 |
| Runtime references | 保持 input buffer、motion executor、facing provider、presenter 引用可解析。 | 新建第二 motion executor、runner 或 pipeline。 |
| Scene override | 清理正式场景实例的旧配置覆盖。 | 新建并行角色 prefab 或替换场景玩法布局。 |
| Presenter coexistence | 校验不新增第二正式 presenter 路径。 | 合并旧 Presenter；该职责属于 `refactor-unified-animancer-presenter`。 |

## Impact
- Affected specs:
  - `character-config-root`
  - `character-runtime-ports`
  - `fullbody-config-boundaries`
  - `basic-locomotion-animation`
  - `action-animation-profile`
- Affected assets:
  - `Assets/Prefabs/Character/可琳.prefab`
  - `Assets/Prefabs/Character/可琳_Humanoid.prefab`
  - `Assets/Scenes/Sandbox.unity`
  - `Assets/Scenes/CameraTest/*.unity`
- Affected tests:
  - Prefab/scene binding static tests
  - Character config root tests
  - FullBody rollback/replay tests
  - Unified state machine tests

## Dependencies
- MUST run after `align-character-config-authoring-contracts`.
- SHOULD run after `migrate-corin-character-config-assets`.
- SHOULD coordinate with `refactor-unified-animancer-presenter`; if unified presenter changes prefab component shape, run that change before this one or fold this change's presenter-specific tasks into that implementation.

## Stop Conditions
- 如果 `PlayerLocomotionController` 的 HIGH blast radius 未报告，必须停止。
- 如果 prefab 清理需要新增 fallback 配置或第二 root config，必须停止。
- 如果清理旧字段会断开当前正式 animation presenter，而统一 Presenter 尚未落地，必须停止并调整顺序。
- 如果 scene override 指向不同角色装配且无法确认是否正式场景，必须停止并先补场景范围说明。
- 如果 Unity 序列化修改后产生 missing script、missing reference 或 YAML 结构异常，必须停止并回到可验证的编辑方式。

## User Verification
用户可以通过以下方式确认本 change 完成：

- 打开两个 Corin prefab，确认 Locomotion 和 FullBody controller 都只通过 `CorinCharacterConfig.asset` 作为配置根。
- 打开 Sandbox/CameraTest 相关场景，确认角色实例没有恢复旧配置入口 override。
- Play Mode 验证 WASD、Directional Dodge、Backstep Dodge 仍走同一 Character frame pipeline。
- 运行 prefab/scene 静态测试、rollback/replay 定向测试、C# build 和 OpenSpec strict validate。

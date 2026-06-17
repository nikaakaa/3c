# Change: 迁移 Corin 角色配置资产闭环

## Why
`CorinCharacterConfig.asset` 已经存在并引用主要子配置，但正式资产闭环仍需要单独校验和迁移：配置根、状态机、动作逻辑、动作动画、Locomotion 动画、Animancer、输入和相机都必须从一个正式根追踪，且不得引用旧目录、测试命名资产或第二正式入口。

本变更只负责 `.asset` 配置闭环，不修改 prefab 或 scene 绑定。

## What Changes
- 校验并补齐 `CorinCharacterConfig.asset` 的正式子配置引用。
- 校验 Corin 配置资产目录位于 `Assets/Configs/3C/...` 的正式蓝图下。
- 清理或迁移正式配置引用中的旧目录、测试命名资产和 dangling GUID。
- 保持 `.meta` GUID 稳定，或在确需移动资产时同步更新正式引用。
- 不修改 `可琳.prefab`、`可琳_Humanoid.prefab` 或 scene override。

## Current Findings
- `CorinCharacterConfig.asset` 当前已经引用 StateMachine、Movement、LocomotionAnimation、FullBody request policy、Dodge action、Animancer rig variant、InputActions、InputActionReference 和 Camera config。
- 当前正式配置目录已经有 `Assets/Configs/3C/Character/Corin/`、`StateMachine/FullBody/`、`Action/FullBody/`、`Animation/Corin/`、`Movement/`、`Input/`、`InputReferences/`、`Camera/`。
- 旧目录和参考资产仍可能存在于工作树中，因此必须校验正式根的引用链，而不是只检查文件是否存在。
- Prefab 上旧字段仍指向同一批子配置，这些绑定由后续 `migrate-corin-character-prefab-bindings` 清理。

## Detailed Scope
| Area | This change owns | This change must not own |
| --- | --- | --- |
| Character root asset | 修正 `CorinCharacterConfig.asset` 的正式子引用。 | 修改 controller 字段或 prefab 绑定。 |
| Sub asset placement | 校验正式子资产位于批准目录。 | 删除参考资产或实验资产。 |
| GUID/ref integrity | 保持 `.meta` GUID 或同步更新 `.asset` 引用。 | 通过 Resources 或硬编码路径修复引用。 |
| Config semantics | 保持现有 Dodge/TurnBack/Locomotion 数值和状态语义。 | 新增动作、状态或 presenter。 |

## Impact
- Affected specs:
  - `character-config-root`
  - `fullbody-config-boundaries`
  - `project-structure`
- Affected assets:
  - `Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset`
  - `Assets/Configs/3C/StateMachine/FullBody/CorinFullBodyStateMachine.asset`
  - `Assets/Configs/3C/Movement/BasicMovementConfig.asset`
  - `Assets/Configs/3C/Action/FullBody/...`
  - `Assets/Configs/3C/Animation/Corin/...`
  - `Assets/Configs/3C/Input/...`
  - `Assets/Configs/3C/InputReferences/...`
  - `Assets/Configs/3C/Camera/...`

## Dependencies
- MUST run after `align-character-config-authoring-contracts`.
- SHOULD run before `migrate-corin-character-prefab-bindings`.
- SHOULD coordinate with `refactor-unified-animancer-presenter` if the animation binding asset shape changes.

## Stop Conditions
- 如果需要修改 `可琳.prefab`、`可琳_Humanoid.prefab` 或 `.unity` 才能完成，必须停止并转入 prefab binding change。
- 如果需要新增 fallback 配置、Resources 加载或全局单例来补缺失引用，必须停止。
- 如果发现正式根需要同时引用 Generic 和 Humanoid rig variant 才能运行，必须停止并重新评审角色变体策略。
- 如果迁移会改变 Dodge、TurnBack、MoveStart、MoveLoop 或 MoveStop 数值，必须停止并拆成独立 gameplay/config proposal。
- 如果资产移动无法保留 GUID 且无法可靠更新全部正式引用，必须停止并先建立引用迁移测试。

## User Verification
用户可以通过以下方式确认本 change 完成：

- 打开 `CorinCharacterConfig.asset`，从一个根资产能追踪全部正式子配置。
- 搜索正式根引用链，确认不指向旧 `Animacer`、`Statemachine`、`Pramater` 或测试命名资产。
- 运行 Corin 配置闭环 EditMode 测试、C# build 和 OpenSpec strict validate。

## Context
Corin 的正式配置目录已经重排到 `Assets/Configs/3C/Character/Corin`、`Action/FullBody`、`StateMachine/FullBody`、`Animation/Corin`、`Movement`、`Input`、`InputReferences` 和 `Camera`。当前风险不是缺少根配置，而是正式配置引用可能仍指向旧目录、测试资产或重复入口。

## Goals
- 建立 Corin 配置资产的单根闭环。
- 保证正式配置不依赖旧目录和测试命名资产。
- 保持 prefab/scene 迁移可独立执行。

## Non-Goals
- 不编辑 prefab 或 scene。
- 不新增配置 fallback。
- 不新增新动作或新动画 Presenter。
- 不改变 Dodge、TurnBack、MoveStart、MoveLoop、MoveStop 数值。

## Decisions
- 资产迁移必须优先保留 Unity `.meta` GUID；无法保留时必须更新所有正式引用并由测试覆盖。
- 正式 Corin 根配置必须能解析 StateMachine、Movement、LocomotionAnimation、FullBody request policy、Dodge action、Animancer rig variant、InputActions、InputActionReference 和 Camera config。
- Humanoid 资产可保留为参考或未来变体，但默认 Corin 根不应要求 Humanoid rig variant。

## Asset Graph
```text
Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset
  -> StateMachine/FullBody/CorinFullBodyStateMachine.asset
  -> Movement/BasicMovementConfig.asset
  -> Animation/Corin/Locomotion/CorinLocomotionAnimationConfig.asset
  -> Action/FullBody/RequestPolicy/CorinFullBodyStateRequestPolicySet.asset
  -> Action/FullBody/Dodge/CorinDodgeActionConfig.asset
  -> Animation/Corin/Animancer/RigVariants/Generic/...
  -> Input/CharacterInput.inputactions
  -> InputReferences/Player_Move.asset
  -> InputReferences/Player_Run.asset
  -> InputReferences/Player_Look.asset
  -> Camera/...
```

## Migration Rules
- `.asset` 内部引用必须使用正式资产 GUID，不通过路径字符串、Resources 或运行时查找补齐。
- 如果资产已经位于正式目录且 GUID 正确，不为整理美观而移动。
- 如果旧目录资产仍被正式根引用，优先找到等价正式目录资产；没有等价资产时停止并补 proposal。
- 如果正式根引用测试命名资产，必须替换为正式资产或停止并补正式资产创建 proposal。
- Humanoid 变体资产可保留为参考，但不能成为默认 Corin 根的必需依赖。

## Validation Matrix
| Check | Evidence |
| --- | --- |
| Root asset complete | EditMode test loads root and checks every required object reference. |
| Formal directories | Static test resolves asset path for each required reference. |
| No legacy directories | Static test scans resolved dependency paths. |
| No test assets | Static test rejects known test/legacy name patterns in formal chain. |
| No prefab mutation | Git diff excludes `Assets/Prefabs` and `Assets/Scenes`. |

## Risks / Mitigations
- 风险：移动资产导致 prefab/scene 引用丢失。
  - 缓解：本变更不改 prefab/scene，资产移动必须保持 GUID 或同步更新所有正式 `.asset` 引用。
- 风险：旧实验资产仍被正式配置引用。
  - 缓解：增加静态测试扫描正式根引用链。
- 风险：只校验根资产，漏掉子资产里的旧引用。
  - 缓解：测试必须递归或显式追踪正式根的一阶/关键二阶依赖。

## Validation
- 运行 Corin 配置闭环 EditMode 测试。
- 运行 C# build。
- 运行 `openspec validate migrate-corin-character-config-assets --strict --no-interactive`。

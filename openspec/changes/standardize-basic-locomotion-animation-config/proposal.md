# Change: 统一基础移动动画配置归属

## Why
当前基础移动动画已经通过 `BasicLocomotionAnimationConfigSO` 配置 Clip 和淡入参数，但 `Sandbox` 场景里仍通过角色实例覆盖手动新增 Presenter 和配置引用。这样会让角色动画表变成场景接线问题，不符合“全局表/角色动画表统一配置”的预期。

本变更只把现有基础移动动画配置收敛到角色 prefab 和现有 WASD 组装入口，不新增第二套动画播放路线，也不扩大到完整角色动画库。

## What Changes
- 约定基础移动动画配置资产作为当前角色的移动动画表，由角色 prefab 上的 `BasicLocomotionAnimancerPresenter` 持有。
- 调整当前 WASD 运行时组装：当 `locomotionPresenter` 未手动绑定时，优先从同对象或子对象查找现有 `BasicLocomotionAnimancerPresenter`。
- 将演示角色 prefab 补齐 `AnimancerComponent`、`BasicLocomotionAnimancerPresenter` 和基础移动动画配置引用。
- 清理场景级重复接线目标：场景只保留必要的输入、相机和移动配置引用，动画 Clip 配置不在每个场景实例重复维护。
- 保持 `BasicLocomotionAnimationConfigSO`、`MovementAnimationContext` 和 `BasicLocomotionAnimancerPresenter` 作为现有抽象边界，不引入全局单例或新 Catalog。

## Impact
- Affected specs: `basic-locomotion-animation`
- Affected code/assets:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Runtime/BasicWASDMovementController.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs`
  - `3cDemo/Client/3C_Client/Assets/Prefabs/Character/可琳.prefab`
  - `3cDemo/Client/3C_Client/Assets/Config/3C/Movement/BasicLocomotionAnimationConfig.asset`
  - `3cDemo/Client/3C_Client/Assets/Scenes/Sandbox.unity`
- Non-goals:
  - 不新增完整 `CharacterAnimationCatalogSO` 或角色 ID 到动画表的全局索引。
  - 不新增攻击、跳跃、闪避、受击、上半身或 IK 动画表。
  - 不改变 `CharacterMotionDriver` 位移权威，不启用 Root Motion 驱动基础移动。
  - 不删除现有 log，除非后续明确要求。

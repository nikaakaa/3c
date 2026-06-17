# 废弃配置面清单

## 删除

- `PlayerLocomotionController` 的旧平铺字段：`runAnimationConfig`、`config`、`stateMachineDefinition`。
- `FullBodyActionRuntime` 的旧平铺字段：`stateMachineDefinition`、`interruptPolicySet`、`dodgeActionConfig`。
- 正式 prefab/scene 中上述字段及 `turnInPlaceAnimationConfig`、`movingPivotTurnAnimationConfig`、`stateGraphConfig` 的空序列化键。
- `CorinHumanoidPresentationAssembler` 对旧 `Animacer/Pramater` 路径和旧双 Presenter 生成链路的依赖。

## 只读保留

- `FullBodyStateView` 仅作为 snapshot/metadata 派生的只读观察视图保留。
- 迁移后的 Dodge、RequestPolicy、Locomotion state graph GUID 保留，用于稳定正式资产引用。

## 仅迁移/测试可见

- `BasicLocomotionAnimancerPresenter` 与 `ActionAnimationAnimancerPresenter` 保留为迁移和历史测试类型，不得由正式 prefab、scene 或 Humanoid 装配器生成。
- `FullBodyActionTickAdapter` 与 `LocomotionTickAdapter` 保留为退役诊断组件，`Register` 必须返回 `false`，不得推进 gameplay tick。

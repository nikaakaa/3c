## Context
`add-basic-locomotion-animation` 已经定义了移动动画上下文、基础移动动画配置和 Animancer 外观层。当前代码中，`BasicLocomotionAnimancerPresenter` 消费 `MovementAnimationContext`，再根据 `BasicLocomotionAnimationConfigSO` 播放 `Idle / MoveStart / MoveLoop / MoveStop`。

现有缺口不在播放逻辑，而在配置归属：`Sandbox` 场景实例手动持有 Presenter 和配置引用。继续让场景实例维护动画配置，会让角色动画表变成场景接线问题。

## Goals / Non-Goals
- Goals:
  - 让角色 prefab 成为基础移动动画表的默认归属点。
  - 让 WASD 入口在未手动绑定时自动发现现有动画外观层。
  - 保留 `BasicLocomotionAnimationConfigSO` 作为动画资源表，不把 Clip 写死进代码。
  - 保持现有 `MovementAnimationContext -> BasicLocomotionAnimancerPresenter` 边界。
- Non-Goals:
  - 不做完整角色动画数据库。
  - 不做基于角色 ID 的运行时全局查表。
  - 不改动攻击、跳跃、闪避或动作仲裁。
  - 不新增第二套角色移动或动画播放入口。

## Decisions
- Decision: 当前阶段使用“角色 prefab 持有一个基础移动动画表”的归属方式。
  - Reason: 这是对现有实现最小的统一，能消除场景重复配置，又不引入未审批的全局 Catalog。
  - Alternative: 新增 `CharacterAnimationCatalogSO` 做角色 ID 到动画表映射。当前只有基础移动四阶段，过早引入会扩大任务范围。

- Decision: `BasicWASDMovementController` 只自动发现 Presenter，不创建 Presenter、不创建配置、不直接读动画 Clip。
  - Reason: WASD 入口是组装层，只负责把移动上下文交给外观层；组件创建和配置归属仍由 prefab 管理。
  - Alternative: WASD 入口自动 AddComponent 或从 Resources 加载默认表。这样会绕开 prefab 配置和现有 SO 边界，形成隐式路径。

- Decision: 自动发现顺序优先同对象，再必要时查子对象。
  - Reason: 当前演示角色和控制组件大概率在同一 prefab 根对象上；子对象兜底能兼容 Animancer/Animator 放在模型子节点的结构。
  - Alternative: 全场景查找 Presenter。会跨角色误连，尤其后续多角色或敌人出现时风险高。

## Runtime Shape
1. 角色 prefab 根对象保留移动控制、运动驱动和基础移动动画外观层。
2. `BasicLocomotionAnimancerPresenter` 持有 `AnimancerComponent` 和 `BasicLocomotionAnimationConfigSO`。
3. `BasicWASDMovementController.OnEnable` 或等价初始化点确认 `motionDriver`，并在 `locomotionPresenter` 为空时查找同对象/子对象 Presenter。
4. `Tick` 继续构建 `MovementAnimationContext`。
5. 如果找到 Presenter，则提交上下文；如果没有找到，则移动仍可运行，但不播放基础移动动画。

## Risks / Trade-offs
- Risk: 角色 prefab 和场景实例已有覆盖项，移动组件可能存在场景级新增。
  - Mitigation: 实现时先检查 prefab 与场景实例差异，只把动画外观层和配置归属收敛到 prefab，不重建角色结构。
- Risk: 自动查找子对象可能找到错误 Presenter。
  - Mitigation: 查找范围限制在当前角色对象层级内，不使用全局查找。
- Risk: 当前项目快速开发阶段不新增测试文件。
  - Mitigation: 使用 OpenSpec 校验、静态搜索和 Unity 手动验证记录替代新增测试文件；后续正式化时再补 EditMode 测试。

## Verification Notes
实现完成后，验证 `Sandbox` 中可琳 prefab 能在不重复配置动画 Clip 的前提下播放基础移动动画。手动验证必须确认 WASD 位移仍由 `CharacterMotionDriver` 执行，动画外观层没有调用移动 API，也没有启用 Root Motion 作为基础移动权威。

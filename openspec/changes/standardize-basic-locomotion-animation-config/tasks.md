## 1. 现状确认
- [x] 1.1 确认 `BasicLocomotionAnimationConfigSO` 仍是基础移动四阶段动画表。
- [x] 1.2 确认 `BasicLocomotionAnimancerPresenter` 仍是唯一 Animancer 播放外观层。
- [x] 1.3 确认 `BasicWASDMovementController` 不直接调用 `AnimancerComponent.Play`。
- [x] 1.4 确认 `Sandbox` 当前动画配置引用来自场景实例覆盖。

## 2. WASD 组装入口
- [x] 2.1 在 `BasicWASDMovementController` 增加一个小型 Presenter 解析方法。
- [x] 2.2 当 `locomotionPresenter` 已绑定时保持原引用不变。
- [x] 2.3 当 `locomotionPresenter` 为空时先查找同对象 `BasicLocomotionAnimancerPresenter`。
- [x] 2.4 同对象未找到时查找当前角色层级内的子对象 Presenter。
- [x] 2.5 不使用全场景查找。
- [x] 2.6 不在 WASD 入口中自动创建 Presenter、AnimancerComponent 或配置资产。
- [x] 2.7 保持移动逻辑和动画上下文构建顺序不变。

## 3. 角色 prefab 配置
- [x] 3.1 检查 `可琳.prefab` 是否已有 `Animator` 和模型根引用。
- [x] 3.2 在 `可琳.prefab` 归属位置补齐 `AnimancerComponent`。
- [x] 3.3 在 `可琳.prefab` 归属位置补齐 `BasicLocomotionAnimancerPresenter`。
- [x] 3.4 将 Presenter 的 `config` 指向 `BasicLocomotionAnimationConfig.asset`。
- [x] 3.5 将 Presenter 的 `animancer` 指向同 prefab 层级内的 `AnimancerComponent`。
- [x] 3.6 保持 `disableAnimatorRootMotion` 为禁用 Root Motion 驱动基础移动的策略。
- [x] 3.7 不在 prefab 中新增第二套移动控制入口。

## 4. Sandbox 场景引用收敛
- [x] 4.1 检查 `Sandbox` 中角色实例的 Presenter 可由 prefab 提供。
- [x] 4.2 检查 `Sandbox` 中 WASD 控制器可通过自动发现拿到 Presenter。
- [x] 4.3 移除 `Sandbox` 场景级重复动画 Clip 配置。
- [x] 4.4 保留 `Sandbox` 必要的输入、相机和移动配置引用。

## 5. 静态验证
- [x] 5.1 运行 `openspec validate standardize-basic-locomotion-animation-config --strict --no-interactive`。
- [x] 5.2 静态搜索确认 `BasicWASDMovementController` 没有调用 `AnimancerComponent.Play`。
- [x] 5.3 静态搜索确认 `BasicLocomotionAnimancerPresenter` 没有调用 `CharacterController.Move`。
- [x] 5.4 静态搜索确认 `BasicLocomotionAnimancerPresenter` 没有写入 `transform.position`。
- [x] 5.5 静态搜索确认没有新增 `Resources.Load` 或全局单例动画表路径。

## 6. Unity 手动验证
- [ ] 6.1 打开 `Sandbox`，进入 Play Mode。
- [ ] 6.2 不在场景实例重复绑定动画 Clip 的前提下，按 WASD 验证进入 `MoveStart`。
- [ ] 6.3 持续 WASD 验证进入 `MoveLoop`。
- [ ] 6.4 松开 WASD 验证进入 `MoveStop` 后回到 `Idle`。
- [ ] 6.5 验证角色位移仍由 `CharacterMotionDriver` 驱动，动画 Root Motion 未成为基础移动权威。

> 本次无法代跑 Play Mode：Unity MCP 当前无可用会话，batchmode 打开项目时提示项目已在另一个 Unity 实例中打开。

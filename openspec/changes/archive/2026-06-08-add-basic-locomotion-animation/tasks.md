## 1. 范围确认
- [x] 1.1 确认本变更只接入基础移动动画外观层。
- [x] 1.2 确认不接入 BBB 运行时主线。
- [x] 1.3 确认不实现 Root Motion、动作位移、跳跃、闪避或攻击。
- [x] 1.4 确认基础移动位移仍只进入 `CharacterMotionDriver`。

## 2. 动画上下文
- [x] 2.1 新建 `Assets/Scripts/Character/Animation/Model` 目录。
- [x] 2.2 定义 `MovementAnimationContext`。
- [x] 2.3 写入当前 `BasicMovementPhase`。
- [x] 2.4 写入是否存在移动意图。
- [x] 2.5 写入输入强度。
- [x] 2.6 写入当前世界移动方向。
- [x] 2.7 写入当前平面速度。
- [x] 2.8 确认上下文不依赖 Animancer 类型。

## 3. 动画配置
- [x] 3.1 新建 `Assets/Scripts/Character/Animation/Config` 目录。
- [x] 3.2 定义 `BasicLocomotionAnimationConfigSO`。
- [x] 3.3 配置 `Idle` 动画引用。
- [x] 3.4 配置 `MoveStart` 动画引用。
- [x] 3.5 配置 `MoveLoop` 动画引用。
- [x] 3.6 配置 `MoveStop` 动画引用。
- [x] 3.7 配置各阶段 fade duration。
- [x] 3.8 暴露只读访问属性。
- [x] 3.9 确认动画资源不写死在代码路径里。

## 4. Animancer 外观层
- [x] 4.1 新建 `Assets/Scripts/Character/Animation/Runtime` 目录。
- [x] 4.2 定义 `BasicLocomotionAnimancerPresenter`。
- [x] 4.3 绑定 `AnimancerComponent`。
- [x] 4.4 绑定 `BasicLocomotionAnimationConfigSO`。
- [x] 4.5 提供接收 `MovementAnimationContext` 的公开方法。
- [x] 4.6 根据 `Idle` 阶段播放待机动画。
- [x] 4.7 根据 `MoveStart` 阶段播放起步动画。
- [x] 4.8 根据 `MoveLoop` 阶段播放循环移动动画。
- [x] 4.9 根据 `MoveStop` 阶段播放停止动画。
- [x] 4.10 避免每帧重复重播同一阶段动画。
- [x] 4.11 暴露当前阶段、当前动画名和当前速度用于 Inspector 调试。
- [x] 4.12 确认外观层不调用 `CharacterController.Move`。
- [x] 4.13 确认外观层不写 `transform.position`。

## 5. WASD 组装接入
- [x] 5.1 在 `BasicWASDMovementController` 增加可选动画外观层引用。
- [x] 5.2 在移动命令执行后构建 `MovementAnimationContext`。
- [x] 5.3 将阶段、意图、世界方向和 `CharacterMotionDriver.CurrentSpeed` 写入上下文。
- [x] 5.4 如果动画外观层存在，则提交上下文。
- [x] 5.5 确认 WASD 入口不直接调用 `AnimancerComponent.Play`。
- [x] 5.6 确认未新增第二套移动入口。

## 6. 资源绑定
- [x] 6.1 在 `Assets/Config/3C` 下创建基础移动动画配置资产。
- [x] 6.2 绑定用户提供的 `Idle` 动画 Clip。
- [x] 6.3 绑定用户提供的 `MoveStart` 动画 Clip。
- [x] 6.4 绑定用户提供的 `MoveLoop` 动画 Clip。
- [x] 6.5 绑定用户提供的 `MoveStop` 动画 Clip。
- [x] 6.6 将配置资产绑定到当前 WASD 演示角色。
- [x] 6.7 确认角色上存在可用 `AnimancerComponent` 或按当前外观层要求补齐。

## 7. 验证
- [x] 7.1 运行 `openspec validate add-basic-locomotion-animation --strict --no-interactive`。
- [x] 7.2 静态搜索确认 `BasicWASDMovementController` 不直接调用 `AnimancerComponent.Play`。
- [x] 7.3 静态搜索确认动画外观层不调用 `CharacterController.Move`。
- [x] 7.4 静态搜索确认本变更不引用 BBB 运行时命名空间或类型。
- [x] 7.5 Unity 手动验证：进入 Play Mode 后不按键时播放 `Idle`。
- [x] 7.6 Unity 手动验证：按下 WASD 后进入 `MoveStart` 动画。
- [x] 7.7 Unity 手动验证：持续 WASD 后进入 `MoveLoop` 动画。
- [x] 7.8 Unity 手动验证：松开 WASD 后进入 `MoveStop` 并回到 `Idle`。
- [x] 7.9 Unity 手动验证：角色按相机相对方向移动，位移不由动画 Root Motion 驱动。
- [x] 7.10 本轮快速开发不新增 Unity 测试文件；若后续恢复自动化测试要求，再为上下文构建和阶段映射补 EditMode 测试。

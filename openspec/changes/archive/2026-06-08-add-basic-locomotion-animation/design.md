## Context
当前 `add-minimal-third-person-wasd` 已经定义并实现最小 WASD 闭环：输入被转换为 `MovementInputIntent`，相机相对方向由 `CameraRelativeMovementResolver` 计算，`BasicMovementStateMachine` 管理 `Idle / MoveStart / MoveLoop / MoveStop`，最终位移进入项目自有 `CharacterMotionDriver`。

项目目标要求动画播放通过 Animancer 外观层收敛，不能让移动组装入口直接散落大量 Animancer 细节。用户也明确不能直接复用 BBB 运行时主线，只能参考或局部复制后重新归属到当前项目模块。

因此本变更在 WASD 阶段后新增“基础移动动画外观层”：移动系统输出纯数据上下文，动画外观层根据配置播放角色动画。第一版只做表现，不改变位移权威。

## Goals / Non-Goals
- Goals:
  - 将现有 WASD 四阶段映射到角色移动动画。
  - 通过 ScriptableObject 配置动画 Clip 和淡入参数。
  - 让 Animancer 播放细节集中在一个外观层模块。
  - 让 WASD 入口只提交动画上下文，不直接播放动画。
  - 保持基础移动仍由 `CharacterMotionDriver` 执行。
- Non-Goals:
  - 不让动画 Root Motion 驱动基础循环移动。
  - 不接 BBB `PlayerBrain`、BBB 状态机或 BBB `MotionDriver`。
  - 不处理攻击、跳跃、闪避、翻越、受击等动作。
  - 不做完整动画状态机或动作仲裁。

## Decisions
- Decision: 新增 `MovementAnimationContext` 作为移动到动画的纯数据边界。
  - Reason: 动画外观层只需要知道当前阶段、输入强度、世界方向和速度，不应反向读取输入或运动组件内部细节。
  - Alternative: 让 Animancer 组件直接读取 `BasicWASDMovementController`。这样耦合更高，后续迁移到正式角色聚合点时改动更大。

- Decision: 使用 `BasicLocomotionAnimationConfigSO` 配置四阶段动画和淡入时间。
  - Reason: 用户已有动画 Clip，配置化能避免把资源路径写死在代码里，也符合项目配置优先原则。
  - Alternative: 在代码字段里直接序列化四个 Clip。短期更少文件，但后续不同角色复用和调参会变差。

- Decision: Animancer 细节只放在 `BasicLocomotionAnimancerPresenter`。
  - Reason: WASD、状态机和运动驱动不应该依赖 Animancer 类型；动画播放是外观层责任。
  - Alternative: 在 `BasicWASDMovementController.Tick` 里直接调用 `AnimancerComponent.Play`。这会把组装层变成业务、运动和表现混合点。

- Decision: 第一版动画不产生位移。
  - Reason: 当前运动权威已经收敛到 `CharacterMotionDriver`；直接启用 Root Motion 会绕开统一运动出口。
  - Alternative: 直接使用动画 Root Motion。观感可能更快，但会产生第二条位移路径，后续很难接急停、闪避和网络预测。

## Proposed Runtime Shape
- `MovementAnimationContext`
  - 保存 `BasicMovementPhase`、是否有移动意图、输入强度、世界方向和当前速度。
- `BasicLocomotionAnimationConfigSO`
  - 配置 `Idle / MoveStart / MoveLoop / MoveStop` 动画。
  - 配置每个阶段的 fade duration。
  - 提供缺失配置的安全判断。
- `BasicLocomotionAnimancerPresenter`
  - 持有 `AnimancerComponent` 和 `BasicLocomotionAnimationConfigSO`。
  - 接收 `MovementAnimationContext`。
  - 只在阶段变化或必要参数变化时切换动画。
  - 输出只读调试字段：当前阶段、当前动画名、当前速度。
- `BasicWASDMovementController`
  - 继续负责输入、相机、移动状态、命令和 `CharacterMotionDriver` 组装。
  - 在运动执行后构建 `MovementAnimationContext`。
  - 如果绑定了动画外观层，则提交上下文。

## Update Order
1. 读取 Move/Look 输入。
2. 更新相机目标和方向。
3. 生成移动意图。
4. 解析世界移动方向。
5. 更新移动阶段。
6. 生成并执行 `MovementCommand`。
7. 从移动阶段、意图、方向和 `CharacterMotionDriver.CurrentSpeed` 构建动画上下文。
8. 动画外观层根据上下文播放配置动画。

## Risks / Trade-offs
- Risk: 动画 Clip 资源命名和循环设置可能不统一。
  - Mitigation: 本变更只要求通过配置绑定 Clip；具体资源选择在 Unity Inspector 中完成，并在手动验证中确认循环、淡入和停止表现。
- Risk: 起步和停止动画与输入驱动位移可能脚滑。
  - Mitigation: 第一版接受轻微滑步，只验证阶段映射和播放闭环；Root Motion 烘焙或动作位移后续单独提 proposal。
- Risk: 后续正式角色聚合点回归 BBB 风格时需要迁移入口。
  - Mitigation: 保持动画上下文和外观层接口简单，避免把 `BasicWASDMovementController` 作为长期动画业务中心。

## Manual Verification Notes
实现交付时，在 Unity 内绑定角色的 `Idle / MoveStart / MoveLoop / MoveStop` 动画 Clip，进入 Play Mode 后按 WASD 验证四阶段动画切换，确认角色位移仍由 `CharacterMotionDriver` 输出，确认未启用 BBB 运行时主线或第二套移动入口。

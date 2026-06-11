## Context
`Sandbox` 中基础移动已经通过 `UnitySimulationTickDriver` 以 60 ticks per second 推进，`LocomotionTickAdapter` 会关闭 `PlayerLocomotionController.AutoUpdate` 并在 `ExecuteMotion` phase 中调用移动主线。该设计为预测、回滚和网络同步预留了固定步基础。

相机侧当前仍在渲染帧更新：CinemachineBrain 使用 LateUpdate，`ThirdPersonCameraController` 根据 `followAnchorSource` 输出 `CameraFollowTarget` 和 `CameraAimTarget`。当 `followAnchorSource` 直接指向角色真实 Transform 时，高刷新率下相机目标只在 simulation tick 产生时跳变，渲染帧之间没有连续表现位置。

进一步排查后发现，仅插值相机目标不足以解决用户观察到的 60 tick 抖动。`可琳.prefab` 中 `Root`、`Bip001`、`Corin_body`、`Corin_body_02`、`Corin_face`、`Corin_hair`、`Corin_Weapon` 等可见骨骼和 SkinnedMeshRenderer 仍挂在角色真实根下。也就是说，即使相机目标平滑，画面中的角色本体仍按 tick 阶梯移动；256 tick 只是提高表现采样密度。

## Goals
- 让角色可见表现根按渲染帧输出连续 pose，降低 60Hz tick 与渲染帧不同步导致的角色本体抖动。
- 让相机 Follow/LookAt 消费同一条表现层输出，避免相机目标与角色可见位置不一致。
- 保持基础移动权威仍在 tick 主线中，不增加第二套角色位移路径。
- 保留当前 `CameraFollowTarget` / `CameraAimTarget` 目标代理主路径，不回到过时的单锚点 spec 假设。
- 让插值算法有纯逻辑测试，不依赖 Cinemachine 或 Play Mode 才能验证。

## Non-Goals
- 不改变 locomotion 状态机、速度计算、输入读取或 `CharacterController.Move` 权威路径。
- 不实现网络回滚、远端实体插值或服务器校正。
- 不通过提高 tick rate、Cinemachine damping 或 Brain 固定帧设置掩盖表现层阶梯问题。
- 不引入第二套角色控制器、第二套相机目标写入路径或额外移动旁路。
- 不实现 Root Motion 位移权威迁移。
- 不删除现有 debug log。
- 不重构所有 vcam 模式，只保证 Free/Rail 当前主路径可以接入表现锚点。

## Decisions
- Decision: 新增通用表现层 Transform 插值，而不是只新增相机表现锚点。
  - Reason: 用户已验证 60 tick 仍抖、256 tick 才不抖，说明可见角色本体仍在消费 tick 阶梯化 Transform；相机插值只能解决跟随点连续，不能让角色渲染连续。
  - Alternative: 保留相机锚点插值。该方案会留下角色本体阶梯移动，表现问题仍存在。

- Decision: 拆分真实模拟根和可见表现根。
  - Reason: 真实根服务 `CharacterController`、碰撞、输入、状态和未来快照；表现根服务 Animator、骨骼、SkinnedMeshRenderer 和相机可见跟随。
  - Alternative: 直接插值角色真实 Transform。该方案会污染 gameplay 权威结果，破坏 tick 主线和未来预测/回滚边界。

- Decision: 表现根由渲染帧更新，数据源来自 tick 后的真实 pose 样本。
  - Reason: 表现层可以按渲染帧输出连续 pose，但只能消费 tick 结果，不能决定 gameplay 结果。
  - Alternative: 让基础移动退回每帧 `Update`。该方案会绕开 simulation tick 主线，不符合当前网络同步方向。

- Decision: 插值 pose 至少覆盖 position 和 yaw/rotation。
  - Reason: 角色位移与朝向都由 tick 主线产生；只插值 position 可能留下朝向阶梯感。
  - Alternative: 先只插值 position。该方案更小，但在当前角色转向由 `CharacterMotionDriver` 写 root rotation 的情况下可能保留旋转抖动。

- Decision: tick 系统暴露只读插值 alpha 或余量，不把相机逻辑放进 tick core。
  - Reason: `simulation-tick-system` 的 core 需要保持纯调度和纯数据边界，不能引用 Cinemachine 或 Unity 场景目标。
  - Alternative: 在 tick runner 的 PresentationBridge phase 直接驱动相机或可见模型。该方案会把表现写入塞进 tick phase，不符合“表现层按 render frame 更新”的目标。

- Decision: 当前 `CameraFollowTarget` / `CameraAimTarget` 继续作为相机目标代理。
  - Reason: 场景和 prefab 已经存在 `Third Person Rail CM vcam`、`CameraFollowTarget`、`CameraAimTarget`，且用户确认这些对象是当前场景事实；proposal 应基于当前实现事实。
  - Alternative: 立即删除目标代理回到单锚点。该方向与现状冲突，且会把“解决抖动”和“相机目标架构迁移”绑成大改。

## Risks / Trade-offs
- Risk: 表现根迁移破坏 Animator 绑定或序列化引用。
  - Mitigation: 优先让 Animator、Animancer 外观层、骨骼和 SkinnedMeshRenderer 同属表现根子树；迁移后用 EditMode 结构测试和 Play Mode 手动验证确认 Idle/Run 动画仍播放。

- Risk: 插值组件更新顺序晚于相机控制器或 CinemachineBrain。
  - Mitigation: 实现时明确执行顺序，保证表现根先于相机目标代理和 Cinemachine 采样更新。

- Risk: 低帧率一帧多 tick 时样本覆盖导致插值退化。
  - Mitigation: 保留上一 tick pose 和当前 tick pose；当 tick 数大于 1、样本缺失或 teleport 超距时允许 snap，避免拉长错误路径。

- Risk: 当前场景中存在多个 vcam 和目标代理，插值接入点选错会形成旁路。
  - Mitigation: 本变更只在统一相机目标代理上游增加表现锚点，不新增第二套 Cinemachine 目标更新路径。

## Verification
- EditMode 测试覆盖 pose 插值 alpha clamp、position/rotation 两样本插值、无 tick 样本 snap、teleport/snap 策略。
- EditMode 测试覆盖 simulation driver 暴露的只读 alpha 范围和不足一个 tick 时的余量语义。
- EditMode 或 Prefab 结构测试覆盖 `可琳` 真实根和表现根分离：真实根保留 `CharacterController` / locomotion 主线，表现根承载 Animator / 可见模型。
- 手动验证在 `Sandbox` 高刷新率或解除 VSync 时，WASD 直线移动角色本体和相机跟随都不再出现 60Hz 阶梯抖动。

## Context
当前相机链路由 `ThirdPersonCameraController` 维护 yaw/pitch 和 `CameraTarget`，Cinemachine vcam 使用 `3rd Person Follow` 输出最终相机。现有 prefab 和场景实例中同时存在 `CinemachineCollider` 与 `3rd Person Follow` 的 `CameraCollisionFilter`，两者都可能尝试修正相机位置。

用户反馈的核心问题是：当地面只有单面片，且玩家继续向下调整视角时，相机没有按预期缩臂，而是继续移动到不合理位置。Cinemachine 默认碰撞更适合作为障碍兜底，不负责基于 pitch 提前约束臂长，也无法保证单面片地面的稳定遮挡。

## Goals / Non-Goals
- Goals:
  - 让第三人称相机只有一个正式碰撞缩臂入口。
  - 保留 Cinemachine 的 vcam、blend、Follow/LookAt 和基础 `3rd Person Follow` 配置。
  - 使用 `SphereCast` 从锚点到期望相机位置修正相机最终位置。
  - 支持根据 pitch 计算当前最大允许距离，向下看时可提前缩臂。
  - 要求场景可通过专用相机碰撞几何验证地面和墙体防穿透。
- Non-Goals:
  - 不替换 Cinemachine。
  - 不实现多相机模式切换策略。
  - 不把相机碰撞逻辑写进角色移动或输入脚本。
  - 不把美术单面片当作可靠的正式碰撞源。

## Decisions
- Decision: 使用 Cinemachine Extension 承载缩臂约束。
  - Reason: Extension 可在 Cinemachine 管线末端修正 `CameraState`，保留现有 vcam 和 blend，不产生第二套相机控制器。
  - Alternative: 写独立相机跟随脚本。会绕开当前 vcam 系统，形成未审批的分裂路径。

- Decision: 将缩臂约束作为独立可插拔模块实现。
  - Reason: 模块通过 vcam 挂载、序列化配置和相机接口读取输入，移除或禁用后应回到基础 Cinemachine 行为，不影响角色移动、输入、动画和相机目标输出。
  - Alternative: 把缩臂逻辑写进 `ThirdPersonCameraController`。实现会更快，但会把目标输出、碰撞检测和 Cinemachine 修正混在一起，后续切换 vcam 或复用其它相机模式时边界不清晰。

- Decision: 禁用重复碰撞入口。
  - Reason: `CinemachineCollider`、`3rd Person Follow` 内置碰撞和自定义缩臂同时启用时，无法稳定判断相机最终位置是谁修正的。
  - Alternative: 三者叠加调参。短期可能局部有效，但会让墙体、地面、pitch 曲线互相干扰。

- Decision: 使用 `SphereCast` 而不是单线 `Raycast`。
  - Reason: 相机有体积，`SphereCast` 更接近实际相机半径，能减少擦边穿透。
  - Alternative: `CapsuleCast`。更稳但参数更多，第一版复杂度过高。

- Decision: pitch 距离限制与碰撞命中分开处理。
  - Reason: 向下看时缩臂是设计约束，不应完全依赖碰撞命中；碰撞命中只负责不穿墙和不穿地。
  - Alternative: 只靠碰撞命中缩臂。当地面薄、角度接近平行或从背面检测时仍可能失败。

- Decision: 引入或使用专用相机碰撞层。
  - Reason: 工业项目通常用简化厚碰撞代理服务相机，不直接依赖渲染网格或单面片。
  - Alternative: 继续用 Default/Ground 混合层。能快速验证，但正式项目中容易被角色、特效、装饰物污染。

## Proposed Runtime Shape
1. `ThirdPersonCameraController` 继续输出 `CameraTarget` 和当前 pitch。
2. Cinemachine vcam 使用 `3rd Person Follow` 计算基础期望位置。
3. `CameraArmCollisionConstraint` 作为 vcam 上的可插拔 Extension，在 Cinemachine 管线后段读取当前 `CameraState.RawPosition`。
4. 约束模块从自身配置、vcam 状态或明确相机接口读取锚点、pitch、碰撞层和距离参数。
5. 约束模块从配置锚点到期望相机位置做 `SphereCast`。
6. 约束模块取 `pitchDistanceScale` 与碰撞命中距离的较小值。
7. 约束模块使用不同平滑时间处理缩臂和恢复。
8. 约束模块写回修正后的相机位置。
9. 禁用或移除约束模块后，基础 Cinemachine 相机链路仍可独立运行。

## Risks / Trade-offs
- Risk: 单面 Plane 仍可能无法稳定命中。
  - Mitigation: 任务要求提供厚碰撞代理验证，不把单面片作为最终验收依据。
- Risk: 自定义 Extension 与 Cinemachine 更新阶段耦合。
  - Mitigation: 只在 Cinemachine Extension 边界内写最终位置修正，不改 `ThirdPersonCameraController` 的基础目标输出。
- Risk: pitch 曲线配置不当会让镜头过近。
  - Mitigation: 提供最小距离、最大距离、曲线和恢复平滑参数，并配套手动验证步骤。

## Open Questions
- 正式相机碰撞层命名是否使用 `CameraCollision`，还是先复用 `Default + Ground` 做快速验证？
- 第一版是否只处理 Free vcam，还是同步处理 Shooting、LockOn、Rail vcam 的碰撞入口关闭？

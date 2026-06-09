# Change: 将第三人称相机主路径切换为 Cinemachine FreeLook

## Why
当前第三人称相机由项目侧 `ThirdPersonCameraController` 维护 yaw/pitch 和 `CameraTarget`，再交给 Cinemachine vcam 输出画面，手感和构图不如 Cinemachine 自带 FreeLook。后续战斗相机还会有锁定、瞄准、技能镜头、震动和构图等多个影响源，继续把自研 yaw/pitch 解算作为主路径会让相机真相源变复杂。

本变更把 Free 模式主镜头切换为 `CinemachineFreeLook`，让 Cinemachine 负责基础轨道、阻尼、构图和镜头混合；项目侧只掌控相机影响源决策、输入适配、移动方向输出和必要的平面碰撞适配。

## What Changes
- 使用 `CinemachineFreeLook` 作为 Free 第三人称主相机。
- 将玩家 Look 输入接入 FreeLook 轴，而不是继续由 `ThirdPersonCameraController` 自行维护主 yaw/pitch。
- 保留项目侧相机影响源入口，用于后续战斗、锁定、瞄准和技能镜头提交意图。
- 保留 `ICameraMovementBasisProvider` 等项目接口，让移动系统继续读取项目接口，不直接依赖 `Camera.main` 或具体 Cinemachine 组件。
- 保留并适配 `CameraArmCollisionConstraint`，作为 Cinemachine 管线中的平面/薄地面适配约束。
- FreeLook 的 Follow 和 LookAt 使用同一个场景角色锚点，不再保留独立的相机跟随目标和瞄准目标。
- 将现有 prefab 和测试场景中的 Free 模式相机主路径统一到 FreeLook，避免 FreeLook 与旧 yaw/pitch target 主路径并存。

## Impact
- Affected specs: `cinemachine-third-person-camera`
- Related pending changes: `add-minimal-third-person-wasd`, `add-camera-arm-collision-constraint`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Camera`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement`
  - `3cDemo/Client/3C_Client/Assets/Prefabs/Camera/Third Person Camera Rig.prefab`
  - `3cDemo/Client/3C_Client/Assets/Scenes/CameraTest/CameraTest.unity`
  - `3cDemo/Client/3C_Client/Assets/Scenes/Sandbox.unity`

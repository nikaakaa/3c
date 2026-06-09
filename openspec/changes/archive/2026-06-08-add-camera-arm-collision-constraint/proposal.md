# Change: 添加相机臂碰撞约束

## Why
当前第三人称相机依赖 Cinemachine 自带碰撞配置，但在地面为单面片、相机下压未到 pitch 上限时，镜头会继续沿轨道移动并可能看到地面或墙体另一侧。继续叠加 `CinemachineCollider`、`3rd Person Follow` 内置碰撞和额外脚本会产生多个碰撞入口，调参边界不清晰。

参考 `Ref/3c_Ref_aa/Packages/com.opsive.ultimatecharactercontroller/Runtime/ThirdPersonController/Camera/ViewTypes/ThirdPerson.cs`，工业项目通常在相机求解阶段以锚点到期望相机位置的 `SphereCast` 修正最终位置，并配合专用相机碰撞几何，而不是只依赖美术单面片或多个后处理碰撞器兜底。

## What Changes
- 新增 `camera-arm-collision-constraint` 能力，用于描述第三人称相机的统一缩臂和防穿透约束。
- 保留 Cinemachine vcam、Brain、blend 和 `3rd Person Follow` 作为相机姿态和基础臂长来源。
- 新增一个统一的 Cinemachine Extension 规划，负责从相机锚点到期望相机位置做 `SphereCast`，并修正最终相机位置。
- 明确 `CinemachineCollider` 和 `3rd Person Follow` 的 `CameraCollisionFilter` 不再作为正式缩臂入口，避免多套碰撞系统同时改相机位置。
- 增加按 pitch 限制当前最大相机距离的配置能力，使向下看时可提前缩短臂长。
- 要求使用专用相机碰撞层或厚碰撞代理，降低单面片地面对相机检测的影响。
- 配套编译检查和手动场景验证要求，确保缩臂、恢复和重复碰撞入口关闭都可验证。

## Impact
- Affected specs: `camera-arm-collision-constraint`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Camera`
  - `3cDemo/Client/3C_Client/Assets/Prefabs/Camera/Third Person Camera Rig.prefab`
  - `3cDemo/Client/3C_Client/Assets/Scenes/CameraTest/CameraTest.unity`
  - `3cDemo/Client/3C_Client/Assets/Scenes/Sandbox.unity`
- Reference use:
  - 参考 Opsive 的锚点到期望位置 `SphereCast` 思路，不复制 Opsive 运行时代码。
  - 继续使用当前项目的 Cinemachine 相机路径，不引入 Opsive 依赖。
- Non-goals:
  - 不重写完整相机系统。
  - 不新增锁定、射击、轨道、技能镜头模式。
  - 不改角色移动、动画、Root Motion 或输入系统主路径。
  - 不依赖单面 Plane 作为正式相机遮挡方案。

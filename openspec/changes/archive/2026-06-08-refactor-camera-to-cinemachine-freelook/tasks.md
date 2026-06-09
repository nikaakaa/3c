## 1. 现状确认
- [x] 1.1 确认 Cinemachine 版本为 `2.10.7`，`CinemachineFreeLook` 可用。
- [x] 1.2 定位 `Third Person Camera Rig.prefab` 中 Free/Shooting/LockOn/Rail vcam。
- [x] 1.3 定位 `CameraTest.unity` 通过 prefab instance 引用相机 rig，`Sandbox.unity` 使用展开后的相机 rig。
- [x] 1.4 确认移动系统继续通过 `ICameraMovementBasisProvider` 读取平面方向，没有直接依赖 `Camera.main` 或 FreeLook。
- [x] 1.5 确认 `CameraArmCollisionConstraint` 作为 Cinemachine Extension 修正相机状态，并复用旧 GUID 避免场景引用断裂。

## 2. FreeLook 主路径
- [x] 2.1 在相机 prefab 的 Free 模式对象上建立 `CinemachineFreeLook` 主相机。
- [x] 2.2 场景中的 FreeLook Follow 和 LookAt 由开发者在 Cinemachine Inspector 中统一绑定到同一个角色锚点。
- [x] 2.3 从运行时路径移除 `CameraFollowTarget` / `CameraAimTarget` 双目标输出。
- [x] 2.4 保留开发者手动调整的 FreeLook 上/中/下三轨道参数。
- [x] 2.5 保留开发者手动调整的 FreeLook X/Y 轴范围、速度、反转和 recenter 配置。
- [x] 2.6 从 Free 模式主输出路径移除旧 `CinemachineVirtualCamera` 组件，避免同对象双 vcam base。
- [x] 2.7 确认 Free 模式对象只剩 FreeLook 作为启用的主相机真相源。
- [x] 2.8 删除项目代码初始化或运行 tick 中覆盖 FreeLook 配置的逻辑。

## 3. 输入适配
- [x] 3.1 新增 `CinemachineFreeLookInputAdapter` 作为 FreeLook 输入适配入口。
- [x] 3.2 将项目 Look 输入通过 `AxisState.IInputAxisProvider` 写入 FreeLook X/Y 轴。
- [x] 3.3 `ThirdPersonCameraController` 在 FreeLook 存在时不再自研 yaw/pitch 驱动 Free 视角。
- [x] 3.4 保留 `ManualLookSource` 的 enable/disable 边界，避免重复启停 InputAction。
- [x] 3.5 按当前快速开发指令未新增测试文件；用接线检查、YAML 检查和 Unity 编译尝试覆盖适配验证点。

## 4. 项目相机接口
- [x] 4.1 保留 `ThirdPersonCameraController` 作为项目相机状态读取入口。
- [x] 4.2 从 FreeLook live state 或输出相机旋转计算 `CameraPlanarForward`。
- [x] 4.3 从 FreeLook live state 或输出相机旋转计算 `CameraPlanarRight`。
- [x] 4.4 继续输出 `LookDirection` 和 `AimPoint`。
- [x] 4.5 WASD 继续依赖项目接口，不直接依赖 FreeLook。
- [x] 4.6 按当前快速开发指令未新增测试文件；通过静态搜索确认移动层没有直接读取 `Camera.main` 或 FreeLook。

## 5. 影响源边界
- [x] 5.1 新增 `CameraInfluenceRequest` 作为第一版影响源请求结构。
- [x] 5.2 新增 `CameraInfluenceResolver` 作为最小优先级/权重仲裁入口。
- [x] 5.3 本次接入 Free 模式默认影响源 `CameraInfluenceRequest.FreeDefault`。
- [x] 5.4 锁定、瞄准、技能镜头只预留入口，不实现具体模式。
- [x] 5.5 按当前快速开发指令未新增测试文件；通过 controller 接入点和静态搜索确认影响源没有散落到 Cinemachine 组件修改。
- [x] 5.6 新增 `ICameraInfluenceSource`，让影响源只提交项目侧请求，不直接依赖 FreeLook 或 Cinemachine 组件。
- [x] 5.7 新增 `CameraInfluenceStack`，支持多个运行时影响源统一注册、注销和仲裁。
- [x] 5.8 新增 `CameraInfluenceHandle`，给战斗、锁定、技能镜头等短生命周期来源提供可释放句柄。
- [x] 5.9 将 `ThirdPersonCameraController.SetInfluence/ClearInfluence` 接入同一套影响源栈，避免兼容 API 形成第二条路径。
- [x] 5.10 静态搜索确认相机影响源新增入口没有写入 FreeLook 轨道、Follow、LookAt 或轴配置。
- [x] 5.11 新增 `ICameraInfluenceSink`，让后续战斗、锁定和技能镜头依赖影响源写入入口，而不是依赖具体 `ThirdPersonCameraController`。
- [x] 5.12 在 `ThirdPersonCameraController` 暴露当前影响源数量，便于调试仲裁输入但不暴露内部栈实现。

## 6. 平面碰撞适配
- [x] 6.1 确认 `CameraArmCollisionConstraint` 挂在 FreeLook 同对象时可绑定到唯一 vcam base。
- [x] 6.2 将平面/薄地面适配保留在 `CameraArmCollisionConstraint`。
- [x] 6.3 保持 Cinemachine 自带 Collider 禁用，自定义约束作为唯一正式缩臂入口。
- [x] 6.4 自定义约束可禁用，禁用后 FreeLook 仍能作为基础 Cinemachine 相机运行。
- [x] 6.5 `CameraArmCollisionConstraint` 未显式配置 anchor 时从 FreeLook Follow/LookAt 解析同一个角色锚点。
- [x] 6.6 按当前快速开发指令未新增测试文件；通过 solver/constraint 静态检查和 YAML 接线确认碰撞边界。

## 6A. Cinemachine 输出适配
- [x] 6A.1 新增 `CinemachineResolvedTargetAdapter`，集中负责 `CameraFollowTarget` / `CameraAimTarget` 的查找、创建和写入。
- [x] 6A.2 将 FreeLook 的 Follow/LookAt 绑定逻辑收敛到 `CinemachineResolvedTargetAdapter`，不散落在 controller 主流程中。
- [x] 6A.3 保持 `ThirdPersonCameraController` 只调用适配模块输出 `CameraResolveResult`，不直接创建或查找目标子物体。
- [x] 6A.4 静态搜索确认适配模块只写 FreeLook Follow/LookAt，不写轨道、轴、Lens 或 Priority。

## 7. Prefab 与场景统一
- [x] 7.1 更新 `Third Person Camera Rig.prefab` 的 Free 模式相机配置。
- [x] 7.2 从 `Third Person Camera Rig.prefab` 删除 `CameraFollowTarget` / `CameraAimTarget` 子物体。
- [x] 7.3 `CameraTest.unity` 继续继承相机 prefab 的 FreeLook 配置。
- [x] 7.4 更新 `Sandbox.unity` 中当前 `FreeLook Camera`，使 Follow/LookAt 都指向 `可琳`。
- [x] 7.5 通过 YAML 检查确认旧 yaw/pitch 目标不再作为 Free 模式主视角驱动。
- [x] 7.6 通过 YAML 检查确认 FreeLook 是 Free 模式最高优先级相机。

## 8. 验证
- [x] 8.1 按当前快速开发指令未新增/运行 EditMode 测试文件。
- [x] 8.2 运行 `openspec validate refactor-camera-to-cinemachine-freelook --strict --no-interactive`，结果通过。
- [x] 8.3 静态搜索代码、prefab 和场景资源，确认旧双目标运行时路径无残留。
- [x] 8.4 静态搜索确认项目相机代码不再写入 FreeLook 轴配置、Follow 或 LookAt。
- [x] 8.5 Unity Console 无相机相关编译错误；仅保留既有第三方/动画 warning。
- [x] 8.6 手动验证步骤：进入 `Sandbox.unity` Play Mode，移动鼠标确认 FreeLook 视角手感正常。
- [x] 8.7 手动验证步骤：按 WASD 确认角色仍按相机相对方向移动。
- [x] 8.8 手动验证步骤：将镜头压向 Plane 或薄地面，确认平面适配能阻止明显穿透，并确认控制台无新增相机相关错误。

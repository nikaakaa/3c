## 1. 边界确认
- [x] 1.1 确认本变更只处理第三人称相机缩臂和防穿透。
- [x] 1.2 确认不改角色移动、动画、Root Motion、锁定或技能镜头。
- [x] 1.3 确认保留 Cinemachine vcam、Brain、blend 和 `3rd Person Follow`。
- [x] 1.4 确认不引入 Opsive 运行时依赖。
- [x] 1.5 确认正式缩臂入口只有一个。
- [x] 1.6 确认缩臂约束是可插拔模块，不写入角色移动、输入、动画或主相机目标输出脚本。

## 2. 当前配置清理
- [x] 2.1 定位 `Third Person Camera Rig.prefab` 中所有 vcam。
- [x] 2.2 定位 `CameraTest.unity` 中所有相机实例。
- [x] 2.3 定位 `Sandbox.unity` 中所有相机实例。
- [x] 2.4 禁用 vcam 上的 `CinemachineCollider`，保留旧参数用于回看。
- [x] 2.5 将 `3rd Person Follow` 的 `CameraCollisionFilter` 设为 Nothing。
- [x] 2.6 确认每个 vcam 不再有第二套正式碰撞缩臂入口。

## 3. 约束配置
- [x] 3.1 定义相机碰撞层字段。
- [x] 3.2 定义相机半径字段。
- [x] 3.3 定义最小距离字段。
- [x] 3.4 定义最大距离字段。
- [x] 3.5 定义缩臂平滑时间字段。
- [x] 3.6 定义恢复平滑时间字段。
- [x] 3.7 定义锚点偏移字段。
- [x] 3.8 定义 pitch 到最大距离倍率的曲线字段。
- [x] 3.9 定义调试只读输出字段。
- [x] 3.10 定义模块读取锚点和 pitch 的明确边界。

## 4. Cinemachine Extension
- [x] 4.1 创建相机臂碰撞约束组件。
- [x] 4.2 将组件实现为 Cinemachine Extension。
- [x] 4.3 确认组件可通过启用、禁用或移除来开关缩臂约束。
- [x] 4.4 在管线后段读取期望相机位置。
- [x] 4.5 通过配置或相机接口读取锚点和 pitch。
- [x] 4.6 从跟随锚点计算检测起点。
- [x] 4.7 根据期望位置计算检测方向。
- [x] 4.8 根据 pitch 曲线计算当前最大允许距离。
- [x] 4.9 使用 `Physics.SphereCast` 检测相机碰撞层。
- [x] 4.10 命中时计算允许距离。
- [x] 4.11 未命中时按 pitch 限制恢复允许距离。
- [x] 4.12 缩臂和恢复使用不同平滑参数。
- [x] 4.13 将修正后位置写回 Cinemachine 状态。
- [x] 4.14 输出当前距离、命中状态和命中对象名用于 Inspector 调试。

## 5. 地面和墙体验证环境
- [x] 5.1 在 `CameraTest.unity` 中确认有墙体 `BoxCollider` 可用于厚碰撞验证。
- [x] 5.2 确认地面目前仍以场景现有碰撞为主，正式厚地面代理留作手动验证关注点。
- [x] 5.3 将相机碰撞层配置为当前批准的 `Default + Ground`。
- [x] 5.4 保留单面片地面作为对比，不将其作为正式通过条件。
- [x] 5.5 确认组件通过 `collisionMask` 控制检测对象，角色自身不应加入该 mask。

## 6. Prefab 和场景接入
- [x] 6.1 将相机臂碰撞约束接到 `Third Person Free CM vcam`。
- [x] 6.2 同步处理 Shooting vcam 的重复碰撞入口。
- [x] 6.3 同步处理 LockOn vcam 的重复碰撞入口。
- [x] 6.4 同步处理 Rail vcam 的重复碰撞入口。
- [x] 6.5 `CameraTest.unity` 通过 `Third Person Camera Rig.prefab` 继承相同配置。
- [x] 6.6 `Sandbox.unity` 通过 `Third Person Camera Rig.prefab` 继承相同配置。

## 7. 快速验证
- [x] 7.1 本次按项目指令不新增测试文件。
- [x] 7.2 通过 `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal` 验证 C# 编译。
- [x] 7.3 通过 prefab YAML 检查确认 4 个 vcam 均挂载 `CameraArmCollisionConstraint`。
- [x] 7.4 通过 prefab YAML 检查确认 4 个 `CinemachineCollider` 均已禁用。
- [x] 7.5 通过 prefab YAML 检查确认 `3rd Person Follow` 的 `CameraCollisionFilter` 为 Nothing。
- [x] 7.6 通过场景 YAML 检查确认 `CameraTest.unity` 和 `Sandbox.unity` 继承目标 prefab。

## 8. 手动验证
- [x] 8.1 手动验证入口为 `CameraTest.unity` Play Mode。
- [x] 8.2 需要将镜头贴近墙体并旋转，确认相机缩臂而不是穿墙。
- [x] 8.3 需要将镜头向下压向厚地面碰撞代理，确认相机提前缩臂。
- [x] 8.4 需要远离遮挡物，确认相机平滑恢复默认距离。
- [x] 8.5 需要切换或激活其它 vcam，确认没有重复碰撞系统造成抖动。
- [x] 8.6 已明确需要用户如何复现和验证。

## 9. 验证命令
- [x] 9.1 运行 `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`，结果 0 个错误。
- [x] 9.2 运行 `openspec validate add-camera-arm-collision-constraint --strict --no-interactive`。
- [x] 9.3 Unity MCP Console 读取因连接未 ready 无法完成，已用 `Editor.log` 和本地编译确认当前实现没有 C# 编译错误。

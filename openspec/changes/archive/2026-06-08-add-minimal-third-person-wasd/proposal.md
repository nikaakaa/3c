# Change: 添加最小第三人称 WASD 闭环

## Why
当前需要先做一个能在 Unity 内跑起来、能演示方向感的最小第三人称 WASD 纵切。第一版重点不是完整 3C 系统，而是把输入、Cinemachine 基础相机方向、角色位移出口和简单状态闭环接通，作为后续动画、Root Motion、锁定和动作系统的统一起点。

现有代码里已经有旧 `CameraController`、旧 `PlayerMovementState`、`CharacterBase` 以及参考目录里的 BBBNexus 相关代码。直接沿用旧状态里的相机读取、Transform 旋转和动画移动会继续扩大耦合，所以本变更只做最小第三人称 WASD 主路径，并明确禁止产生第二套未经审批的角色移动路径。

## What Changes
- 新增 `minimal-third-person-wasd` 能力，用于描述第一版最小第三人称 WASD 闭环。
- 基于 Cinemachine 2.10.7 使用一个基础第三人称虚拟相机，跟随角色的标准 `CameraTarget`，相机最终跟随、构图和阻挡交给 Cinemachine。
- 保留一个轻量相机控制层读取 Look 输入，只维护 yaw/pitch、标准 `CameraTarget` 和 `CameraPlanarForward` / `CameraPlanarRight` 输出。
- 读取 Unity Input System 的 `Player/Move`，生成纯运行时移动意图，处理死区和斜向归一化。
- 使用相机平面 forward/right 计算 WASD 的世界平面移动方向。
- 通过项目自有 `CharacterMotionDriver` 提交基础移动命令；WASD 组装层不得直接移动 Transform 或调用 `CharacterController.Move`。
- 第一版只包含 `Idle / MoveStart / MoveLoop / MoveStop` 的最小逻辑阶段，不接入动画播放、完整 Root Motion、跳跃、闪避、攀爬、锁定、射击或技能镜头。
- 配套 Unity Test Framework EditMode 测试任务；本 proposal 阶段不写实现代码、不运行 Unity 测试。

## Impact
- Affected specs: `minimal-third-person-wasd`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Camera`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character`
  - `3cDemo/Client/3C_Client/Assets/Config/Input/CharactorInput.inputactions`
  - `3cDemo/Client/3C_Client/Assets/Config/Camera`
  - `3cDemo/Client/3C_Client/Assets/Tests/EditMode`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement`
- Reference use:
  - 本变更不参考 `Ref` 目录实现，不复制第三方控制器。
  - 不直接复用 `Ref/BBB` 下的 `BBBCharacterController` 或 `MotionDriver`。
- Non-goals:
  - 不实现多相机模式、锁定、射击、轨道、技能镜头、镜头震动或 zzzdemo 镜头迁移。
  - 不实现 Animancer 播放、动画事件、Root Motion 烘焙、Motion Warping 或动作位移。
  - 不引入 KCC sample、Opsive 或 BBB 参考工程运行时依赖。
  - 不删除现有 log。

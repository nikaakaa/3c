# cinemachine-third-person-camera Specification

## Purpose
TBD - created by archiving change refactor-camera-to-cinemachine-freelook. Update Purpose after archive.
## Requirements
### Requirement: FreeLook 主相机
系统 MUST 使用 `CinemachineFreeLook` 作为 Free 第三人称模式的主相机真相源，并 MUST 避免旧 yaw/pitch 目标控制器与 FreeLook 同时驱动 Free 模式视角。

#### Scenario: Free 模式启用 FreeLook
- **WHEN** Free 第三人称模式处于激活状态
- **THEN** live Cinemachine 相机 MUST 是 FreeLook 主相机或其有效输出
- **AND** 旧 yaw/pitch 目标控制器 MUST NOT 同时消费 Look 输入来驱动 Free 模式视角

#### Scenario: FreeLook 输出最终画面
- **WHEN** 玩家调整 Look 输入
- **THEN** FreeLook MUST 更新自身 X/Y 轴状态
- **AND** Cinemachine MUST 负责基础轨道、阻尼、构图和最终画面输出

#### Scenario: 手动维护 Cinemachine 配置
- **WHEN** 开发者在 Inspector 中调整 FreeLook 轴、轨道、Follow 或 LookAt 配置
- **THEN** 项目相机控制器 MUST NOT 在初始化或运行 tick 中覆盖这些 Cinemachine 配置
- **AND** 项目相机控制器 MUST 只读取 FreeLook 状态并提供项目侧移动方向接口

### Requirement: FreeLook 单锚点绑定
系统 MUST 使用同一个角色锚点作为 FreeLook 的 Follow 和 LookAt 来源，并 MUST NOT 为 Free 模式继续维护独立的 CameraFollowTarget 或 CameraAimTarget 场景目标。

#### Scenario: 场景角色锚点绑定
- **WHEN** 场景中的相机控制器配置了角色锚点
- **THEN** 开发者 MUST 在 Cinemachine Inspector 中将 FreeLook 的 Follow 指向该角色锚点
- **AND** 开发者 MUST 将 FreeLook 的 LookAt 指向同一个角色锚点

#### Scenario: Prefab 不保存场景目标
- **WHEN** 检查 `Third Person Camera Rig.prefab`
- **THEN** prefab MUST NOT 包含 `CameraFollowTarget` 或 `CameraAimTarget` 子物体
- **AND** prefab 中的 FreeLook MUST NOT 通过项目相机控制器在运行时强制改写 Follow 或 LookAt

### Requirement: 项目侧影响源掌控
系统 MUST 由项目侧相机影响源入口掌控移动、战斗、锁定、瞄准和技能镜头等影响源决策，并 MUST 通过统一适配边界影响 Cinemachine。

#### Scenario: 影响源不直接改 Cinemachine
- **WHEN** 战斗或技能系统需要提交镜头意图
- **THEN** 它 MUST 提交到项目侧影响源入口
- **AND** 它 MUST NOT 在本阶段直接散落修改 FreeLook 轴、轨道、优先级或 Follow/LookAt

#### Scenario: Free 模式默认影响源
- **WHEN** 没有锁定、瞄准或技能镜头请求
- **THEN** 相机影响源入口 MUST 输出 Free 模式默认意图
- **AND** FreeLook MUST 保持玩家可控的第三人称视角

#### Scenario: 多影响源统一仲裁
- **WHEN** 战斗、锁定、瞄准或技能镜头在同一帧提交多个镜头影响请求
- **THEN** 项目侧影响源入口 MUST 接收多个来源的请求
- **AND** 项目侧影响源入口 MUST 通过统一 resolver 输出一个当前有效请求
- **AND** 短生命周期影响源 MUST 能在结束时注销或释放自己的请求

### Requirement: 项目相机接口适配
系统 MUST 保留项目侧相机接口，为移动、调试和后续战斗逻辑提供相机平面方向与视线数据，并 MUST 避免这些系统直接依赖 `Camera.main` 或具体 FreeLook 实例。

#### Scenario: 移动方向读取项目接口
- **WHEN** WASD 移动系统计算相机相对方向
- **THEN** 它 MUST 读取项目侧相机方向接口
- **AND** 它 MUST NOT 直接读取 `Camera.main`、FreeLook 组件或场景相机 Transform

#### Scenario: 输出平面方向
- **WHEN** Cinemachine 已输出当前 live 相机状态
- **THEN** 项目相机接口 MUST 提供归一化的 `CameraPlanarForward` 和 `CameraPlanarRight`
- **AND** 这些方向 MUST 可用于现有相机相对移动解析

### Requirement: FreeLook 输入适配
系统 MUST 将项目输入系统读取到的 Look 输入适配到 FreeLook 轴，并 MUST 保证同一输入不会被旧控制器和 FreeLook 双重消费。

#### Scenario: Look 输入驱动 FreeLook 轴
- **WHEN** 玩家产生 Look 输入
- **THEN** 输入适配器 MUST 更新 FreeLook 的 X/Y 轴输入或等价状态
- **AND** FreeLook MUST 由该输入完成视角变化

#### Scenario: 禁止双重输入
- **WHEN** FreeLook 主相机启用
- **THEN** 旧 yaw/pitch 主驱动 MUST 关闭自动视角更新或退出 Free 模式主路径
- **AND** 同一帧内 Look 输入 MUST NOT 同时改变旧目标 yaw/pitch 与 FreeLook 轴

### Requirement: 平面碰撞适配约束
系统 MUST 保留可插拔的平面碰撞适配约束，用于处理 Plane 或薄地面导致的第三人称相机穿透问题，并 MUST 将该约束限制在 Cinemachine 管线边界内。

#### Scenario: 薄地面适配
- **WHEN** FreeLook 期望相机位置接近 Plane 或薄地面碰撞代理
- **THEN** 平面碰撞适配约束 MUST 能修正最终相机位置或距离
- **AND** 相机 MUST 避免明显穿透到不合理位置

#### Scenario: 约束可禁用
- **WHEN** 平面碰撞适配约束被禁用或移除
- **THEN** FreeLook MUST 仍能作为基础 Cinemachine 相机运行
- **AND** 输入、移动和影响源接口 MUST 不需要同步改代码才能编译

### Requirement: 相机主路径统一
系统 MUST 在 prefab 和演示场景中统一 Free 模式相机主路径，避免 FreeLook、旧 Free vcam 和旧 yaw/pitch target 输出同时作为主相机源。

#### Scenario: Prefab 主路径唯一
- **WHEN** 检查 `Third Person Camera Rig.prefab`
- **THEN** Free 模式 MUST 只有一个启用的主相机输出源
- **AND** 该输出源 MUST 指向 FreeLook 主相机

#### Scenario: 场景继承统一配置
- **WHEN** `CameraTest.unity` 或 `Sandbox.unity` 加载第三人称相机 rig
- **THEN** 场景 MUST 使用统一后的 FreeLook 配置
- **AND** 场景 MUST NOT 额外启用旧 Free 模式主相机旁路


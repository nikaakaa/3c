# cinemachine-third-person-camera Specification

## Purpose
定义 Cinemachine 第三人称相机接入、输入适配、跟随目标、Look 控制和现有相机主线边界。
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

#### Scenario: Cinemachine 配置边界
- **WHEN** 开发者在 Inspector 中调整 FreeLook 轴、轨道、镜头或阻尼配置
- **THEN** 项目相机控制器 MUST NOT 在初始化或运行 tick 中覆盖这些 Cinemachine 表现配置
- **AND** 项目相机控制器 MAY 通过统一目标代理绑定 Follow 和 LookAt

### Requirement: 相机目标代理绑定
系统 MUST 使用项目侧解析后的相机目标代理作为 FreeLook、Rail 和后续第三人称 vcam 的 Follow/LookAt 来源，并 MUST 保证这些代理来自同一条相机主路径。系统 MAY 使用独立的 `CameraFollowTarget` 与 `CameraAimTarget` 表达跟随点和瞄准点，但 MUST NOT 让业务系统直接散落写入这些目标代理。

#### Scenario: 场景锚点解析
- **WHEN** 场景中的相机控制器配置了角色锚点或表现层锚点
- **THEN** 相机控制器 MUST 通过统一主路径解析出 Follow 和 LookAt 代理目标
- **AND** 当前 live Cinemachine 相机 MUST 通过这些代理目标输出最终画面

#### Scenario: Prefab 保存目标代理
- **WHEN** 检查 `Third Person Camera Rig.prefab`
- **THEN** prefab MAY 包含 `CameraFollowTarget` 和 `CameraAimTarget` 子物体
- **AND** 这些子物体 MUST 只作为相机主路径的输出代理
- **AND** 业务移动、动作或战斗系统 MUST NOT 直接写入这些子物体

#### Scenario: 目标代理不形成旁路
- **WHEN** FreeLook、Rail 或后续第三人称 vcam 需要 Follow/LookAt
- **THEN** 它们 MUST 使用相机主路径提供的目标代理或其等价输出
- **AND** 它们 MUST NOT 各自维护与项目相机控制器无关的场景目标更新逻辑

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
系统 MUST 在 prefab 和演示场景中统一第三人称相机主路径，允许多个 vcam 作为模式候选存在，但 MUST 避免同一时刻多个未仲裁的相机输出或旧 yaw/pitch target 旁路同时作为主相机源。

#### Scenario: Live 输出唯一
- **WHEN** 检查 `Third Person Camera Rig.prefab`
- **THEN** prefab MAY 包含 FreeLook、Rail、Shooting 或后续第三人称 vcam 候选
- **AND** 当前 live 输出 MUST 由 Cinemachine 优先级、Brain 或项目侧相机模式仲裁决定
- **AND** 旧 yaw/pitch target MUST NOT 绕过 Cinemachine 直接输出主相机结果

#### Scenario: 场景继承统一配置
- **WHEN** `CameraTest.unity` 或 `Sandbox.unity` 加载第三人称相机 rig
- **THEN** 场景 MUST 使用统一后的相机目标代理和 Cinemachine 配置
- **AND** 场景 MUST NOT 额外启用旧 Free 模式主相机旁路

### Requirement: 相机消费表现层输出
系统 MUST 让 Cinemachine Follow/LookAt 目标代理消费角色表现层输出，使相机跟随位置与角色可见位置来自同一条表现主路径，而不是直接消费 tick 阶梯化角色真实 Transform。

#### Scenario: 高刷新率跟随表现根
- **WHEN** 角色真实 Transform 由 60Hz simulation tick 推进
- **AND** 渲染帧率高于 simulation tick rate
- **THEN** `CameraFollowTarget` / `CameraAimTarget` MUST 使用表现层输出更新
- **AND** Cinemachine MUST NOT 直接追随未插值的角色真实 Transform

#### Scenario: 相机目标代理保持统一
- **WHEN** `Third Person Rail CM vcam`、FreeLook 或后续第三人称 vcam 需要 Follow/LookAt
- **THEN** 它们 MUST 继续使用相机主路径提供的目标代理或等价输出
- **AND** 它们 MUST NOT 各自维护绕过表现层输出的场景目标更新逻辑

#### Scenario: 缺少 tick 信息安全退化
- **WHEN** 表现层输出缺少 tick driver 或有效样本
- **THEN** 相机目标代理 MUST 安全退化为跟随当前真实锚点或当前表现根
- **AND** 相机 MUST 不因为插值数据缺失而跳到无效位置

#### Scenario: 相机碰撞仍在 Cinemachine 边界
- **WHEN** 相机消费表现层输出
- **THEN** `CameraArmCollisionConstraint` MUST 继续在 Cinemachine 管线边界内修正最终相机位置
- **AND** 表现层插值 MUST NOT 新增第二套相机碰撞或缩臂路径

### Requirement: 相机本体保持 Local-Only
系统 MUST 将 Cinemachine、FreeLook、Main Camera 和相机目标代理视为本地表现层状态。预测回滚和本地 replay MUST NOT 捕获或恢复真实相机本体状态；需要重放 camera-relative 移动时 MUST 使用 simulation snapshot 中的 `RollbackCameraBasisState`。

#### Scenario: 回滚不恢复真实相机
- **WHEN** replay 从旧 tick 恢复角色 simulation 状态
- **THEN** 系统 MUST NOT 恢复 FreeLook X/Y 轴、Main Camera transform、CinemachineBrain 状态或真实 camera target 作为 gameplay rollback 状态
- **AND** 当前玩家看到的本地相机 MUST 保持由 local presentation/camera 主路径控制

#### Scenario: Camera-relative 输入使用 basis
- **WHEN** replay 需要用 Move 和 Look 重新计算世界移动方向
- **THEN** 系统 MUST 读取 `RollbackCameraBasisState` 作为输入解算起点
- **AND** MUST NOT 直接读取 `Camera.main`、FreeLook transform 或当前 live 相机 transform 作为 replay 起点

#### Scenario: Timing probe 只诊断不参与回滚
- **WHEN** Debug Tooling 输出相机 timing probe
- **THEN** probe MAY 记录 camera yaw、pitch、target position 或 Main Camera pose 用于诊断
- **AND** probe 数据 MUST NOT 写入 gameplay rollback snapshot
- **AND** probe 日志 MUST 标注 camera state 为 local-only 或等价语义


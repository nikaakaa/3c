## 1. 边界确认
- [x] 1.1 确认本次最小 WASD 入口为项目自有 `BasicWASDMovementController`，不直接复用 `Ref/BBB` 的 `BBBCharacterController`。
- [x] 1.2 确认本次最小 WASD 只接入项目自有 `CharacterMotionDriver`。
- [x] 1.3 确认同一对象上启用旧 `Player` 路径时，新 WASD 主路径会停止，避免双移动入口。
- [x] 1.4 确认第一版不接入 Animancer 播放、Root Motion、跳跃、闪避、锁定和技能镜头。
- [x] 1.5 确认 Cinemachine 版本为 2.10.7，且实现不使用 `CinemachineInputProvider` 读取玩家输入。

## 2. 最小相机
- [x] 2.1 定义最小第三人称相机配置字段。
- [x] 2.2 绑定或创建标准 `CameraTarget`。
- [x] 2.3 读取 `Player/Look` 输入。
- [x] 2.4 根据 look delta 更新 yaw。
- [x] 2.5 根据 look delta 更新 pitch。
- [x] 2.6 对 pitch 做配置化 clamp。
- [x] 2.7 将 yaw/pitch 应用到 `CameraTarget`。
- [x] 2.8 暴露 `CameraPlanarForward`。
- [x] 2.9 暴露 `CameraPlanarRight`。
- [x] 2.10 创建或绑定一个基础 Cinemachine vcam。
- [x] 2.11 将 vcam Follow/LookAt 指向 `CameraTarget`。
- [x] 2.12 设置基础 FOV、距离、高度、damping 和碰撞参数。
- [x] 2.13 确认相机控制层不手写最终相机 follow 插值。

## 3. 移动输入意图
- [x] 3.1 定义 `MovementInputIntent` 纯数据结构。
- [x] 3.2 读取 `Player/Move` 输入。
- [x] 3.3 对 Move 输入应用死区。
- [x] 3.4 对斜向输入做归一化，避免斜向超速。
- [x] 3.5 输出输入强度。
- [x] 3.6 输出是否有移动意图。
- [x] 3.7 确认移动输入层不依赖场景对象。

## 4. 相机相对方向
- [x] 4.1 定义相机移动方向 provider 接口。
- [x] 4.2 让最小相机控制层实现方向 provider。
- [x] 4.3 定义 `CameraRelativeMovementResolver`。
- [x] 4.4 使用 `CameraPlanarForward * input.y + CameraPlanarRight * input.x` 计算世界方向。
- [x] 4.5 清除世界方向 y 分量。
- [x] 4.6 对世界方向做归一化。
- [x] 4.7 输入为零时输出零方向。
- [x] 4.8 确认移动方向解析不读取 `Camera.main`。

## 5. 最小移动阶段
- [x] 5.1 定义基础移动阶段枚举。
- [x] 5.2 定义最小移动配置字段。
- [x] 5.3 定义 `BasicMovementStateMachine`。
- [x] 5.4 初始阶段为 `Idle`。
- [x] 5.5 有移动意图时从 `Idle` 进入 `MoveStart`。
- [x] 5.6 `MoveStart` 达到最短时间且仍有输入时进入 `MoveLoop`。
- [x] 5.7 `MoveStart` 输入释放时进入 `MoveStop`。
- [x] 5.8 `MoveLoop` 输入释放时进入 `MoveStop`。
- [x] 5.9 `MoveStop` 达到最短时间且无输入时进入 `Idle`。
- [x] 5.10 `MoveStop` 重新输入时回到 `MoveStart`。
- [x] 5.11 确认状态机不调用 Animancer。

## 6. CharacterMotionDriver 输出
- [x] 6.1 定义 `MovementCommand` 纯数据结构。
- [x] 6.2 将世界方向和配置速度转换为移动命令。
- [x] 6.3 将期望朝向写入移动命令。
- [x] 6.4 将阶段和 deltaTime 写入移动命令。
- [x] 6.5 在 `CharacterMotionDriver` 内执行平面位移。
- [x] 6.6 在 `CharacterMotionDriver` 内执行角色转向。
- [x] 6.7 确认 WASD 组装层不直接写 `transform.position`。
- [x] 6.8 确认 WASD 组装层不直接写 `transform.rotation`。
- [x] 6.9 确认 WASD 组装层不直接调用 `CharacterController.Move`。

## 7. 运行时组装
- [x] 7.1 定义最小 WASD 第三人称运行时组装组件。
- [x] 7.2 绑定 Move 输入引用。
- [x] 7.3 绑定 Look 输入引用。
- [x] 7.4 绑定相机控制层。
- [x] 7.5 绑定角色 `CharacterMotionDriver`。
- [x] 7.6 按相机、输入、方向、状态、命令、`CharacterMotionDriver` 顺序更新。
- [x] 7.7 提供当前阶段的只读调试输出。
- [x] 7.8 提供当前世界移动方向的只读调试输出。

## 8. 快速开发验证
- [x] 8.1 按当前“现在不用测试文件”要求，本轮不新增 EditMode 测试文件。
- [x] 8.2 静态确认移动组装层不依赖 Animancer 类型。
- [x] 8.3 静态确认移动方向解析不读取 `Camera.main`。
- [x] 8.4 静态确认最小 WASD 主路径不依赖 `BBBNexus` 或 `BBBCharacterController`。

## 9. 验证
- [x] 9.1 运行静态检查命令。
- [x] 9.2 运行 `openspec validate add-minimal-third-person-wasd --strict --no-interactive`。

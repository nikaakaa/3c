## ADDED Requirements
### Requirement: 最小 Cinemachine 第三人称相机
系统 MUST 提供一个最小第三人称相机链路，该链路使用 Cinemachine 虚拟相机跟随标准 `CameraTarget`，并由轻量控制层维护 yaw/pitch、`CameraPlanarForward` 和 `CameraPlanarRight`。

#### Scenario: 基础相机目标更新
- **WHEN** 玩家产生 Look 输入
- **THEN** 控制层更新标准 `CameraTarget` 的 yaw/pitch
- **AND** Cinemachine 虚拟相机通过 Follow/LookAt 使用该目标输出最终画面

#### Scenario: 移动方向输出
- **WHEN** 移动系统请求相机平面方向
- **THEN** 相机控制层提供归一化的平面 forward/right
- **AND** 输出不依赖 `Camera.main`

### Requirement: WASD 输入意图
系统 MUST 从 Unity Input System 的 `Player/Move` 读取输入，并转换为不依赖场景对象的移动意图数据。

#### Scenario: 斜向输入归一化
- **WHEN** 玩家同时按下两个垂直方向的 WASD 输入
- **THEN** 移动意图的归一化输入强度不超过 1

#### Scenario: 输入死区
- **WHEN** Move 输入幅度低于配置死区
- **THEN** 移动意图标记为无移动意图

### Requirement: 相机相对移动方向
系统 MUST 使用相机平面 forward/right 将移动意图转换为世界平面移动方向。

#### Scenario: 前向输入
- **WHEN** 玩家只输入前进
- **THEN** 世界移动方向等于相机平面 forward

#### Scenario: 零输入
- **WHEN** 玩家没有移动输入
- **THEN** 世界移动方向为零向量

### Requirement: 最小移动阶段闭环
系统 MUST 提供 `Idle / MoveStart / MoveLoop / MoveStop` 的最小移动阶段闭环，第一版阶段只表达移动逻辑，不播放动画。

#### Scenario: 起步
- **WHEN** 当前阶段为 `Idle` 且存在移动意图
- **THEN** 阶段切换为 `MoveStart`

#### Scenario: 循环移动
- **WHEN** 当前阶段为 `MoveStart` 且达到配置最短起步时间并仍存在移动意图
- **THEN** 阶段切换为 `MoveLoop`

#### Scenario: 停止
- **WHEN** 当前阶段为 `MoveLoop` 且移动意图消失
- **THEN** 阶段切换为 `MoveStop`

#### Scenario: 回到待机
- **WHEN** 当前阶段为 `MoveStop` 且达到配置最短停止时间并无移动意图
- **THEN** 阶段切换为 `Idle`

### Requirement: CharacterMotionDriver 输出
系统 MUST 将最小 WASD 位移提交到项目自有 `CharacterMotionDriver`；除 `CharacterMotionDriver` 内部外，WASD 组装层 MUST NOT 直接写 Transform 或直接调用 `CharacterController.Move`。

#### Scenario: 提交移动命令
- **WHEN** 存在有效世界移动方向
- **THEN** WASD 组装层生成 `MovementCommand`
- **AND** 将命令提交给 `CharacterMotionDriver` 执行

#### Scenario: 禁止绕路
- **WHEN** 实现发现必须绕过 `CharacterMotionDriver` 才能移动角色
- **THEN** 实现必须停止
- **AND** 向用户说明需要审批的运动边界变更

### Requirement: 最小第三人称 WASD 运行时组装
系统 MUST 提供一个最小运行时组装入口，将 Look 输入、Move 输入、相机方向 provider、移动阶段和 `CharacterMotionDriver` 按固定顺序连接起来。

#### Scenario: 可演示闭环
- **WHEN** 场景中绑定了角色、相机目标、Move 输入和 Look 输入
- **THEN** 玩家可以用鼠标调整第三人称视角
- **AND** 可以用 WASD 按相机相对方向移动角色

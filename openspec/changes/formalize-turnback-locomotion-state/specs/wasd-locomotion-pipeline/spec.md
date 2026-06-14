## ADDED Requirements
### Requirement: TurnBack 运动命令权威
系统 MUST 在 TurnBack 状态期间通过统一运动命令表达动画驱动转身，而不是让普通输入运动、动画外观层和 root motion 采样各自成为位移或旋转权威。TurnBack 的动画运动贡献或烘焙运动贡献 MUST 转换为 movement facts 或等价纯数据，再由现有 motion executor 执行。

#### Scenario: TurnBack 抑制普通输入运动
- **GIVEN** 当前 phase 为 `TurnBack`
- **WHEN** WASD/Locomotion pipeline 构建本帧 `MovementCommand`
- **THEN** command MUST 标记普通输入旋转被抑制
- **AND** command MUST 标记普通输入平面位移被抑制
- **AND** command MUST NOT 同时应用普通输入旋转和 TurnBack yaw

#### Scenario: TurnBack yaw 仍走 motion executor
- **GIVEN** TurnBack motion policy 从动画采样或 baked profile 产出本帧 yaw delta
- **WHEN** 系统构建本帧运动命令
- **THEN** yaw delta MUST 进入 `MovementCommand` 或等价运动事实
- **AND** MUST 由现有 motion executor 应用到角色根
- **AND** 状态机 runner 和动画 presenter MUST NOT 直接写角色根旋转

#### Scenario: TurnBack 平移默认来自烘焙 profile
- **GIVEN** TurnBack motion policy 的 translation source 为 baked motion profile 或等价配置
- **WHEN** TurnBack 播放窗口推进
- **THEN** pipeline MUST 从 baked profile 读取本帧 local planar delta
- **AND** MUST 将该 delta 写入 `MovementCommand` 或等价运动事实
- **AND** 角色普通跑步位移 MUST 在退出 TurnBack 后由 MoveLoop 命令恢复

#### Scenario: TurnBack 没有第二运动出口
- **WHEN** TurnBack 状态执行中
- **THEN** `CharacterController.Move` MUST 仍只通过现有 motion executor 或等价运动端口调用
- **AND** pipeline MUST NOT 新增绕过 `PlayerLocomotionController` 的 TurnBack 控制器
- **AND** pipeline MUST NOT 恢复 TurnInPlace、MovingPivotTurn 或旧的散落式 baked yaw/profile 运行路径

### Requirement: TurnBack 手动验证闭环
系统 MUST 提供能在 Sandbox 中验证 TurnBack 状态权威的手动路径，使用户能确认触发范围、转身窗口、输入抑制、回接普通移动和诊断日志。

#### Scenario: Sandbox RunLoop 前后反向急转
- **GIVEN** 用户在 Sandbox 使用 Generic 可琳
- **AND** Locomotion 与 Animation 诊断日志已启用
- **AND** 角色已经进入 RunLoop
- **WHEN** 用户先按 W 跑动再切换到 S
- **THEN** 日志 MUST 显示进入 `FullBody/Locomotion/TurnBack`
- **AND** TurnBack 期间 MUST 显示普通输入旋转和平面位移被抑制
- **AND** 转完点后 MUST 回到 `MoveLoop` 或 `Idle`

#### Scenario: 非 RunLoop 不触发 TurnBack
- **GIVEN** 用户在 Sandbox 使用 Generic 可琳
- **WHEN** 角色处于 Walk、MoveStart、MoveStop 或 Idle
- **AND** 用户输入反向移动
- **THEN** 系统 MUST NOT 直接进入 `FullBody/Locomotion/TurnBack`

#### Scenario: 横向切换不误触发前后 TurnBack
- **GIVEN** 用户在 Sandbox 使用 Generic 可琳
- **WHEN** 用户在 A/D 横向输入之间切换
- **THEN** 系统 MUST NOT 因前后 TurnBack 规则误触发 `FullBody/Locomotion/TurnBack`

#### Scenario: 诊断搜索关键字
- **WHEN** 用户复制诊断日志给开发者
- **THEN** 用户 MUST 能通过 `locomotion-turnback-state-policy|turnback-root-motion-consumed|animation-motion-executor|locomotion-animation-played` 或等价关键字找到 TurnBack 状态、动画和运动命令链路

## MODIFIED Requirements
### Requirement: FullBody 框架接入后的 Locomotion 模块边界
系统 MUST 允许现有 WASD/Locomotion 主链在 Character frame pipeline 接入后作为 FullBody 当前身体域下的 Locomotion builder、facts builder 或 adapter 被调度。该模块 MAY 继续负责移动意图、相机相对方向、`Idle / MoveStart / MoveLoop / MoveStop` 局部 phase、基础移动运动命令构建和基础移动动画上下文构建，但最终运动提交和 base layer 动画提交 MUST 服从 Character frame pipeline 的 output composer/applier。

#### Scenario: Locomotion 可被 Character 管线调度
- **WHEN** Character frame pipeline 请求 Locomotion 本帧结果
- **THEN** Locomotion 模块 MUST 能提供移动意图和世界方向事实
- **AND** MUST 能提供当前基础移动 phase
- **AND** MAY 提供基础移动运动命令和动画上下文供 Character output composer 选择提交

#### Scenario: Action active 时不提交 Locomotion 输出
- **GIVEN** 统一状态机当前 active state 选择 FullBody Action 作为本帧 base layer owner
- **WHEN** Locomotion 模块已经生成基础移动运动命令或动画上下文
- **THEN** 系统 MUST NOT 将该基础移动运动命令提交给 motion executor
- **AND** MUST NOT 将该基础移动动画上下文提交给 base layer presenter

#### Scenario: Locomotion 状态图职责保持
- **WHEN** 没有 active FullBody Action
- **THEN** Locomotion 模块 MUST 继续按现有规则处理 `Idle / MoveStart / MoveLoop / MoveStop`
- **AND** `MoveStop -> MoveStart` 仍 MUST 由统一状态图中的 Locomotion 子域规则处理
- **AND** FullBody Action framework MUST NOT 把 Walk/Run 建模为新的 Locomotion phase

#### Scenario: 不恢复第二主入口
- **WHEN** Character frame pipeline 接入完成
- **THEN** 系统 MUST NOT 同时保留一套独立 WASD 主入口和一套独立 FullBody Action 主入口共同提交平面位移
- **AND** 系统 MUST NOT 让 `PlayerDodgeActionController` 或等价 per-action controller 长期绕过 Character frame pipeline 提交 base layer 动画或平面位移

### Requirement: TurnBack 运动命令权威
系统 MUST 在 TurnBack 状态期间通过统一运动命令表达动画驱动转身，而不是让普通输入运动、动画外观层和 root motion 采样各自成为位移或旋转权威。TurnBack 的动画运动贡献或烘焙运动贡献 MUST 转换为 movement facts、movement submission 或等价纯数据，再由 Character frame pipeline 的 output applier 通过现有 motion executor 执行。

#### Scenario: TurnBack 抑制普通输入运动
- **GIVEN** 当前 phase 为 `TurnBack`
- **WHEN** Locomotion builder 构建本帧 `MovementCommand` 或等价 movement submission
- **THEN** command MUST 标记普通输入旋转被抑制
- **AND** command MUST 标记普通输入平面位移被抑制
- **AND** command MUST NOT 同时应用普通输入旋转和 TurnBack yaw

#### Scenario: TurnBack yaw 仍走 motion executor
- **GIVEN** TurnBack motion policy 从动画采样或 baked profile 产出本帧 yaw delta
- **WHEN** 系统构建本帧运动输出
- **THEN** yaw delta MUST 进入 `MovementCommand`、movement facts 或等价 movement submission
- **AND** MUST 由现有 motion executor 应用到角色根
- **AND** 状态机 runner 和动画 presenter MUST NOT 直接写角色根旋转

#### Scenario: TurnBack 平移默认来自烘焙 profile
- **GIVEN** TurnBack motion policy 的 translation source 为 baked motion profile 或等价配置
- **WHEN** TurnBack 播放窗口推进
- **THEN** Locomotion builder MUST 从 baked profile 读取本帧 local planar delta
- **AND** MUST 将该 delta 写入 `MovementCommand`、movement facts 或等价 movement submission
- **AND** 角色普通跑步位移 MUST 在退出 TurnBack 后由 MoveLoop 命令恢复

#### Scenario: TurnBack 没有第二运动出口
- **WHEN** TurnBack 状态执行中
- **THEN** `CharacterController.Move` MUST 仍只通过现有 motion executor 或等价运动端口调用
- **AND** Locomotion builder MUST NOT 新增绕过 `PlayerLocomotionController` 或 Character output applier 的 TurnBack 控制器
- **AND** Locomotion builder MUST NOT 恢复 TurnInPlace、MovingPivotTurn 或旧的散落式 baked yaw/profile 运行路径

## ADDED Requirements
### Requirement: FullBody 框架接入后的 Locomotion 模块边界
系统 MUST 允许现有 WASD/Locomotion 主链在 FullBody Action 框架接入后作为 Locomotion 子图或 adapter 被调度。该模块 MAY 继续负责移动意图、相机相对方向、`Idle / MoveStart / MoveLoop / MoveStop` 局部 phase、基础移动运动命令构建和基础移动动画上下文构建，但最终运动提交和 base layer 动画提交 MUST 服从 FullBody 主调度入口的 owner 选择。

#### Scenario: Locomotion 可被 FullBody 调度
- **WHEN** FullBody 主调度入口请求 Locomotion 本帧结果
- **THEN** Locomotion 模块 MUST 能提供移动意图和世界方向事实
- **AND** MUST 能提供当前基础移动 phase
- **AND** MAY 提供基础移动运动命令和动画上下文供 FullBody 主调度入口选择提交

#### Scenario: Action active 时不提交 Locomotion 输出
- **GIVEN** FullBody 主调度入口选择 active FullBody Action 作为本帧 owner
- **WHEN** Locomotion 模块已经生成基础移动运动命令或动画上下文
- **THEN** 系统 MUST NOT 将该基础移动运动命令提交给 motion executor
- **AND** MUST NOT 将该基础移动动画上下文提交给 base layer presenter

#### Scenario: Locomotion 状态图职责保持
- **WHEN** 没有 active FullBody Action
- **THEN** Locomotion 模块 MUST 继续按现有规则处理 `Idle / MoveStart / MoveLoop / MoveStop`
- **AND** `MoveStop -> MoveStart` 仍 MUST 由 Locomotion 局部状态图处理
- **AND** FullBody Action framework MUST NOT 把 Walk/Run 建模为新的 Locomotion phase

#### Scenario: 不恢复第二主入口
- **WHEN** FullBody Action framework 接入完成
- **THEN** 系统 MUST NOT 同时保留一套独立 WASD 主入口和一套独立 FullBody Action 主入口共同提交平面位移
- **AND** 系统 MUST NOT 让 `PlayerDodgeActionController` 或等价 per-action controller 长期绕过 FullBody 主调度入口提交 base layer 动画或平面位移

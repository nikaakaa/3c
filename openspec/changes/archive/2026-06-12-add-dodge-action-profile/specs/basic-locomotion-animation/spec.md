## MODIFIED Requirements
### Requirement: 移动动画上下文
系统 MUST 提供不依赖 Animancer 和场景对象的移动动画上下文，用于把当前基础移动阶段、Walk/Run 档位、输入强度、世界方向和当前速度传递给动画外观层。

#### Scenario: 上下文承载移动阶段
- **WHEN** 基础移动阶段更新为 `Idle / MoveStart / MoveLoop / MoveStop`
- **THEN** 移动动画上下文 MUST 记录当前阶段
- **AND** 该上下文 MUST 不包含 Animancer 运行时类型

#### Scenario: 上下文承载 Walk/Run 档位
- **WHEN** 普通移动选择 Walk 或 Shift FullBody 动作 `Directional` 完成后的 Run latch 选择 Run
- **THEN** 移动动画上下文 MUST 记录当前基础移动档位
- **AND** Walk/Run MUST NOT 替代 `BasicMovementPhase`
- **AND** Run 档位 MUST NOT 依赖 Shift 持续按住

#### Scenario: 上下文承载移动参数
- **WHEN** 角色执行基础移动命令后
- **THEN** 移动动画上下文 MUST 记录当前输入强度、世界移动方向和当前平面速度

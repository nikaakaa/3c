## ADDED Requirements

### Requirement: Locomotion 阶段可退出事实
系统 MUST 允许 UnityHFSM 基础 Locomotion 阶段机通过纯数据 `PhaseCanExit` 事实判断当前 phase 是否可退出。阶段机 MUST NOT 直接读取 Animancer、AnimationClip、TransitionAsset、TransitionLibrary、Animator 或场景对象。

#### Scenario: MoveStart 使用 PhaseCanExit 进入 MoveLoop
- **GIVEN** 当前阶段为 `MoveStart`
- **AND** 本帧持续存在移动意图
- **WHEN** `PhaseCanExit` 为 false
- **THEN** 阶段机 MUST 保持 `MoveStart`
- **WHEN** `PhaseCanExit` 为 true
- **THEN** 阶段机 MUST 切换到 `MoveLoop`

#### Scenario: MoveStop 使用 PhaseCanExit 回 Idle
- **GIVEN** 当前阶段为 `MoveStop`
- **AND** 本帧没有移动意图
- **WHEN** `PhaseCanExit` 为 false
- **THEN** 阶段机 MUST 保持 `MoveStop`
- **WHEN** `PhaseCanExit` 为 true
- **THEN** 阶段机 MUST 切换到 `Idle`

#### Scenario: MoveStop 输入打断优先
- **GIVEN** 当前阶段为 `MoveStop`
- **AND** `PhaseCanExit` 为 false
- **WHEN** 本帧重新存在移动意图
- **THEN** 阶段机 MUST 立即切换到 `MoveStart`
- **AND** MUST NOT 等待动画结束事实或时间退出事实

#### Scenario: 缺少事实时不自动退出
- **GIVEN** 当前阶段为 `MoveStop`
- **AND** 本帧没有移动意图
- **WHEN** 未向阶段机提供有效 `PhaseCanExit` 事实
- **THEN** 阶段机 MUST NOT 因缺失事实自动回到 `Idle`
- **AND** 实现 MAY 通过兼容路径把现有 `AfterDuration` 采样结果映射为 `PhaseCanExit`

### Requirement: Locomotion 状态图条件边界
系统 MUST 将状态图条件中的“当前阶段可退出”表达为纯逻辑条件。该条件只读取状态图上下文中的 facts，不知道 facts 的来源。

#### Scenario: PhaseCanExit 条件只读上下文
- **WHEN** 状态图 evaluator 评估 `PhaseCanExit` 条件
- **THEN** evaluator MUST 只读取 `LocomotionStateGraphContext` 或等价上下文中的纯数据 facts
- **AND** MUST NOT 读取 Run 动画配置资产
- **AND** MUST NOT 读取 Animancer 播放状态

#### Scenario: 旧时间条件兼容
- **WHEN** 现有测试或配置仍使用 `PhaseExitTimeReached`
- **THEN** 实现 MUST 提供明确迁移或兼容路径
- **AND** 默认 Locomotion 图最终 MUST 使用 `PhaseCanExit` 表达 `MoveStart -> MoveLoop` 和 `MoveStop -> Idle`

#### Scenario: 逻辑状态仍只有四阶段
- **WHEN** 基础移动使用 `OnAnimationEnd` 驱动 `RunEnd` 退出
- **THEN** Locomotion 阶段机 MUST 仍只输出 `Idle / MoveStart / MoveLoop / MoveStop`
- **AND** MUST NOT 新增 `RunStart / RunLoop / RunEnd` 作为逻辑状态
- **AND** MUST NOT 新增 `Walk` 逻辑状态

### Requirement: Locomotion Timeline Fact 测试
系统 MUST 为 `PhaseCanExit` 接入提供自动测试和静态边界验证，证明动画事实不会让状态机依赖播放层。

#### Scenario: 自动测试覆盖 PhaseCanExit
- **WHEN** 运行 Locomotion EditMode 测试
- **THEN** 测试 MUST 覆盖 `MoveStart` 在 `PhaseCanExit=false` 时保持起步
- **AND** MUST 覆盖 `MoveStart` 在 `PhaseCanExit=true` 且有输入时进入循环移动
- **AND** MUST 覆盖 `MoveStop` 在 `PhaseCanExit=false` 且无输入时保持停止
- **AND** MUST 覆盖 `MoveStop` 在 `PhaseCanExit=true` 且无输入时回到 Idle
- **AND** MUST 覆盖 `MoveStop` 重新输入时不等待 `PhaseCanExit`

#### Scenario: Controller 接入测试
- **WHEN** `PlayerLocomotionController` 绑定基础移动动画配置和播放进度来源
- **THEN** EditMode 测试 MUST 能验证 `OnAnimationEnd` 的播放结束事实会驱动 `MoveStop -> Idle`
- **AND** MUST 能验证播放未结束时不会驱动 `MoveStop -> Idle`

#### Scenario: 静态边界验证
- **WHEN** 检查 `Movement/Model` 和 `Movement/Solver` 源码
- **THEN** 静态搜索 MUST 能确认它们不引用 Animancer
- **AND** MUST 能确认它们不引用 `AnimationClip`
- **AND** MUST 能确认它们不引用 `TransitionLibrary`
- **AND** MUST 能确认它们不引用 `BBBNexus`

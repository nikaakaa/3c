## MODIFIED Requirements
### Requirement: 通用 transition 配置
系统 MUST 将角色状态切换表达为统一状态机中的 transition 配置。移动意图、无移动意图、Locomotion 决策事实、预输入请求、状态时间、动画可退出事实、优先级、抗性和打断窗口 MUST 作为 transition 条件或 transition policy 配置呈现，而不是藏在 Locomotion 图、Action 仲裁器或 transition evaluator 外部路径中。

#### Scenario: Locomotion transition 使用通用条件
- **WHEN** 系统表达基础移动四阶段和 TurnBack 切换
- **THEN** `Idle -> MoveStart` MUST 使用 `HasMoveIntent` 或等价通用条件
- **AND** `MoveLoop -> MoveStop` MUST 使用 `NoMoveIntent` 或等价通用条件
- **AND** `MoveStop -> Idle` MUST 使用 `NoMoveIntent + StateCanExit` 或等价通用条件
- **AND** `MoveStart/MoveLoop/MoveStop -> TurnBack` MUST 使用 `MoveTurnBackRequested` 或等价通用条件读取 Locomotion 决策事实
- **AND** 这些 transition MUST 与 Dodge transition 存在于同一张状态机配置中

#### Scenario: Dodge transition 使用通用条件
- **WHEN** 系统表达 Shift Dodge 进入
- **THEN** `Locomotion/* -> Dodge` MUST 使用 `HasInputRequest(Dodge)` 或等价通用条件
- **AND** priority、resistance、timing window 或等价打断规则 MUST 作为该 transition 的可见配置
- **AND** 系统 MUST NOT 通过状态图外部 Action-only arbiter 隐式选择 Dodge 目标状态

#### Scenario: 条件 evaluator 保持纯数据
- **WHEN** transition 条件被求值
- **THEN** evaluator MUST 只读取统一状态机 context 中的纯数据 facts
- **AND** MUST NOT 读取 Animancer runtime state
- **AND** MUST NOT 读取 `CharacterController`
- **AND** MUST NOT 读取 `InputAction` 或 `Camera.main`
- **AND** MUST NOT 读取人物 `Transform` 来临时派生 Locomotion 空间关系

#### Scenario: TurnBack 条件不临时解析空间事实
- **WHEN** `MoveTurnBackRequested` 或等价条件被求值
- **THEN** evaluator MUST 读取 context 中已经派生好的 TurnBack intent 或等价 Locomotion 决策事实
- **AND** MUST NOT 在 evaluator 内以当前 `FacingForward` 与当前 `WorldMoveDirection` 的即时夹角作为唯一触发来源
- **AND** MUST NOT 读取上一有效移动方向作为 TurnBack 触发来源

### Requirement: 输入、运动和动画 adapter 保持外围
系统 MUST 保留输入读取、运动执行、动画播放和相机处理作为统一状态机外围 adapter。adapter MUST 执行状态机输出或提供纯数据 facts，不得反向拥有逻辑状态切换权威。Locomotion 决策管线 MAY 构建 context facts，但 MUST NOT 绕过统一状态机直接进入具体逻辑状态。

#### Scenario: 输入缓冲只记录请求
- **WHEN** 玩家按下 Shift
- **THEN** 输入 adapter MUST 只写入 Dodge 请求或等价输入事实
- **AND** 是否消费该请求 MUST 由统一状态机 transition 和输出决定

#### Scenario: Locomotion facts 不直接切状态
- **WHEN** Locomotion 决策管线生成 TurnBack intent、Run 候选或其他移动派生事实
- **THEN** 这些 facts MUST 只进入统一状态机 context
- **AND** MUST NOT 直接调用状态切换 API
- **AND** MUST NOT 直接播放 Animancer 动画
- **AND** MUST NOT 直接提交 `MovementCommand`

#### Scenario: 运动执行只消费命令
- **WHEN** 统一状态机产出运动命令
- **THEN** 运动 adapter MUST 执行该命令
- **AND** 运动 adapter MUST NOT 选择当前逻辑状态
- **AND** 统一状态机 runner MUST NOT 直接调用 `CharacterController.Move`

#### Scenario: 动画外观只消费动画请求
- **WHEN** 统一状态机产出动画转换请求
- **THEN** Animancer adapter MUST 播放对应 transition
- **AND** Animancer adapter MUST NOT 选择当前逻辑状态
- **AND** 统一状态机 runner MUST NOT 直接调用 Animancer 播放 API

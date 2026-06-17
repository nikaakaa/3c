## ADDED Requirements
### Requirement: 状态输出只产出 Action Motion 规格
统一状态机状态输出 MUST 产出纯数据 Action motion spec 或等价运动意图，而不得在状态输出解析阶段计算本帧 `ActionMovementCommand`。本帧位移、完成状态和 run latch 完成派生 MUST 由 Action motion resolver 或等价外围纯逻辑模块计算。

#### Scenario: Dodge 输出运动规格
- **GIVEN** 当前状态为 `FullBody/Action/Dodge`
- **AND** 当前变体为 `Directional`
- **WHEN** 状态输出解析当前节点
- **THEN** 输出 MUST 包含动作位移规格
- **AND** 规格 MUST 包含距离、时长、转向策略、变体、锁定方向和 state time
- **AND** 输出解析 MUST NOT 直接构造 `ActionMovementCommand`

#### Scenario: OutputResolver 不计算帧距离
- **WHEN** 检查状态输出解析源码
- **THEN** `CharacterStateOutputResolver` 或等价模块 MUST NOT 根据 `deltaTime / duration / distance` 计算本帧动作距离
- **AND** MUST NOT 判断动作位移是否完成

#### Scenario: ActionMotionResolver 计算执行命令
- **GIVEN** 状态输出提供动作位移规格
- **WHEN** FullBodySubmissionBuilder 进入 Action motion submission 构建阶段
- **THEN** Action motion resolver MUST 将规格转换为 `ActionMovementCommand`
- **AND** MUST 输出本帧是否有动作位移和动作是否完成
- **AND** resolver result MUST 可被 runtime blackboard 和 rollback replay 消费

### Requirement: 状态输出解析不成为 Gameplay Motion Solver
状态输出解析 MUST 保持为状态配置到 frame intent 的纯数据解析模块。新增 Attack、Jump、HitReact 或等价动作位移时，MUST 通过新增 motion spec 或 motion resolver 逻辑扩展，而不是把动作运动数学写入状态输出解析核心。

#### Scenario: 新动作不修改输出解析数学
- **WHEN** 后续新增轻攻击位移、跳跃起跳或受击击退
- **THEN** 新动作 MAY 新增 motion spec 类型或 resolver 规则
- **AND** MUST NOT 在 `CharacterStateOutputResolver` 中新增动作位移数学分支

#### Scenario: 输出解析仍不执行副作用
- **WHEN** 状态输出解析生成 frame intent
- **THEN** 它 MUST NOT 调用 CharacterController、Transform、Animator、Animancer、InputAction 或 motion executor

#### Scenario: 状态输出不写 Action facts
- **WHEN** 状态输出解析生成 action motion spec
- **THEN** 它 MUST NOT 直接写入 runtime blackboard action facts
- **AND** action facts MUST 在 Character output applier 消费 resolver result 后写入

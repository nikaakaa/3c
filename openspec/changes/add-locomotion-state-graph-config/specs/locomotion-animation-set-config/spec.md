## ADDED Requirements

### Requirement: 基础移动动画集配置资产
系统 SHALL 提供一个基础移动动画集配置资产，用于将逻辑层输出的抽象移动表现意图映射到 Animancer transition key。

#### Scenario: 默认动画集映射
- **GIVEN** 使用默认基础移动动画集配置
- **WHEN** presenter 解析基础移动动画
- **THEN** `Idle` 能映射到 `Idle`
- **AND** `MoveStart + Walk` 能映射到 `WalkStart`
- **AND** `MoveStart + Run` 能映射到 `RunStart`
- **AND** `MoveLoop + Walk` 能映射到 `WalkLoop`
- **AND** `MoveLoop + Run` 能映射到 `RunLoop`
- **AND** `MoveStop + Walk` 能映射到 `WalkEnd`
- **AND** `MoveStop + Run` 能映射到 `RunEnd`

### Requirement: 动画变体不得成为逻辑状态
系统 SHALL 将 `RunEnd`、`WalkEnd` 等具体动画 key 视为动画层资源映射结果，不得将其作为基础移动逻辑状态。

#### Scenario: RunEnd 由 MoveStop 映射
- **GIVEN** 逻辑阶段为 `MoveStop`
- **AND** 上一次移动步态为 `Run`
- **WHEN** presenter 查询动画集
- **THEN** presenter 选择 `RunEnd` 动画 key
- **AND** 逻辑阶段仍保持 `MoveStop`

### Requirement: 动画层与逻辑层分离
系统 SHALL 让动画 presenter 消费 `MovementAnimationContext` 和动画集配置，不得让 presenter 修改基础移动状态机或运动执行器。

#### Scenario: Presenter 不驱动逻辑切换
- **GIVEN** presenter 播放基础移动动画
- **WHEN** 动画 key 被解析并提交给 Animancer
- **THEN** presenter MUST NOT 调用状态机切换 API
- **AND** MUST NOT 直接调用运动执行 API

### Requirement: 动画映射校验
系统 SHALL 提供可测试的动画集校验能力，用于发现当前基础移动四阶段所需动画映射缺失。

#### Scenario: 缺失 RunEnd 映射
- **GIVEN** 动画集配置缺少 `MoveStop + Run` 映射
- **WHEN** 运行动画集 validator 或 presenter 初始化校验
- **THEN** 系统返回可读错误
- **AND** 错误信息指出缺失 `MoveStop + Run`

### Requirement: 动画事件边界
系统 SHALL 仅允许动画层通过抽象事件或抽象窗口反馈逻辑层，不得通过具体 clip 名或 transition key 参与逻辑判断。

#### Scenario: 抽象动画完成事件
- **GIVEN** 后续逻辑需要等待停止动画完成
- **WHEN** 动画层反馈完成信息
- **THEN** 反馈 MUST 使用 `MotionComplete` 或等价抽象事件
- **AND** 逻辑层 MUST NOT 判断具体 key 是否为 `RunEnd`

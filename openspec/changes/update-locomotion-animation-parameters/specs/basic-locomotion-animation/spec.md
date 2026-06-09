## MODIFIED Requirements

### Requirement: 基础移动动画配置
系统 MUST 使用 ScriptableObject 配置基础移动动画资源映射、播放参数和逻辑层可读取的动画时长数据，避免在代码中写死动画资源路径、播放参数或停止动画退出时长。

#### Scenario: 四阶段动画可配置
- **WHEN** 设计者配置基础移动动画
- **THEN** 配置模块 MUST 暴露 `Idle / MoveStart / MoveLoop / MoveStop` 四个阶段所需动画映射
- **AND** `MoveStart / MoveLoop / MoveStop` MUST 能区分 `Walk / Run` 或等价步态
- **AND** 每个动画映射 MUST 能配置对应淡入时间、播放速度和起始归一化时间

#### Scenario: 动画资源不写死
- **WHEN** 更换角色或更换动画 key
- **THEN** 设计者 MUST 能通过配置资产替换动画映射
- **AND** 不需要修改移动逻辑代码

#### Scenario: 停止动画退出时长可配置
- **WHEN** 设计者配置 `MoveStop + Walk` 或 `MoveStop + Run` 动画映射
- **THEN** 配置模块 MUST 能为该停止动画提供逻辑层可读取的退出时长
- **AND** 该退出时长 MUST 不要求逻辑层查询 Animancer 当前播放状态

#### Scenario: 旧 key 语义保持
- **GIVEN** 使用默认基础移动动画配置
- **WHEN** presenter 解析基础移动动画
- **THEN** `Idle` MUST 能映射到 `Idle`
- **AND** `MoveStart + Walk` MUST 能映射到 `WalkStart`
- **AND** `MoveStart + Run` MUST 能映射到 `RunStart`
- **AND** `MoveLoop + Walk` MUST 能映射到 `WalkLoop`
- **AND** `MoveLoop + Run` MUST 能映射到 `RunLoop`
- **AND** `MoveStop + Walk` MUST 能映射到 `WalkEnd`
- **AND** `MoveStop + Run` MUST 能映射到 `RunEnd`

### Requirement: Animancer 基础移动外观层
系统 MUST 提供一个 Animancer 基础移动外观层，消费移动动画上下文和基础移动动画配置，并按配置中的动画 key、淡入时间、播放速度和起始归一化时间播放动画。

#### Scenario: 阶段驱动动画播放
- **WHEN** 移动动画上下文阶段为 `MoveLoop`
- **THEN** Animancer 外观层 MUST 播放配置中的循环移动动画
- **AND** 该播放逻辑 MUST 集中在动画外观层内

#### Scenario: 配置参数驱动播放
- **WHEN** 动画外观层解析到一个基础移动动画 entry
- **THEN** 外观层 MUST 使用 entry 中的动画 key 请求 Animancer 播放
- **AND** SHOULD 使用 entry 中的淡入时间、播放速度和起始归一化时间影响播放

#### Scenario: 避免重复重播
- **WHEN** 连续多帧收到相同移动阶段和相同动画 key
- **THEN** Animancer 外观层 MUST 避免每帧从头重播同一个阶段动画

#### Scenario: 调试状态可见
- **WHEN** 动画外观层接收移动动画上下文
- **THEN** 系统 MUST 暴露当前阶段、当前动画名和当前速度作为只读调试信息

#### Scenario: 禁止状态仲裁泄漏
- **WHEN** 动画外观层播放 `WalkEnd` 或 `RunEnd`
- **THEN** 外观层 MUST NOT 直接切换基础 Locomotion 状态
- **AND** 外观层 MUST NOT 调用运动执行端口

## ADDED Requirements

### Requirement: 基础移动动画时长解析
系统 MUST 提供基础移动动画时长解析能力，将动画配置转换为逻辑层可消费的纯数据时长结果。

#### Scenario: 停止动画时长解析
- **GIVEN** 基础移动动画配置为 `MoveStop + Run` 配置了退出时长
- **WHEN** 逻辑层请求当前停止动画时长
- **THEN** 系统 MUST 返回该退出时长
- **AND** 返回结果 MUST 不包含 Animancer runtime 类型
- **AND** 返回结果 MUST 不包含 Unity 场景对象引用

#### Scenario: Fallback 时长
- **GIVEN** 基础移动动画配置没有提供停止动画退出时长
- **AND** 调用方提供了 `MoveStopMinTime` fallback
- **WHEN** 逻辑层请求当前停止动画时长
- **THEN** 系统 MAY 返回 fallback 时长
- **AND** 该 fallback MUST 明确来自移动配置而不是 Animancer runtime

#### Scenario: 缺失时长可诊断
- **GIVEN** 基础移动动画配置缺少停止动画退出时长
- **AND** 调用方没有提供可用 fallback
- **WHEN** 运行动画配置校验
- **THEN** 系统 MUST 返回可读错误
- **AND** 错误信息 MUST 指出缺失的是 `MoveStop` 停止动画退出时长

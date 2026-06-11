## MODIFIED Requirements

### Requirement: 基础移动动画配置
系统 MUST 使用 ScriptableObject 或等价配置提供当前基础移动 Run-only 动画播放参数，避免在代码中写死 `Idle / RunStart / RunLoop / RunEnd` 的播放参数。本变更 MUST NOT 新增 `WalkStart / WalkLoop / WalkEnd` 配置。

#### Scenario: Run-only 动画可配置
- **WHEN** 设计者配置当前基础移动动画
- **THEN** 配置模块 MUST 暴露 `Idle / RunStart / RunLoop / RunEnd`
- **AND** 每个 entry MUST 能配置 alias key、fade duration、speed 和 normalized start time
- **AND** 配置模块 MUST NOT 暴露 `WalkStart / WalkLoop / WalkEnd`

#### Scenario: 动画资源不写死
- **WHEN** 更换角色或更换 Animancer alias
- **THEN** 设计者 MUST 能通过配置资产替换 `Idle / RunStart / RunLoop / RunEnd` 的 alias key 或播放参数
- **AND** 不需要修改移动逻辑状态机代码

#### Scenario: RunEnd 停止退出时长可配置
- **WHEN** 设计者配置 `RunEnd`
- **THEN** 配置模块 MUST 能提供 `RunEnd` stop exit duration
- **AND** 该时长 MUST 以纯数据形式供逻辑层使用

### Requirement: Animancer 基础移动外观层
系统 MUST 提供一个 Animancer 基础移动外观层，消费移动动画上下文并使用 Run-only 动画配置播放 `Idle / RunStart / RunLoop / RunEnd`。外观层 MUST 只负责动画播放，不负责状态机切换或位移执行。

#### Scenario: 阶段驱动 Run 动画播放
- **WHEN** 移动动画上下文阶段为 `MoveLoop`
- **THEN** Animancer 外观层 MUST 播放配置中的 `RunLoop`
- **AND** 该播放逻辑 MUST 集中在动画外观层内

#### Scenario: 停止阶段播放 RunEnd
- **WHEN** 移动动画上下文阶段为 `MoveStop`
- **THEN** Animancer 外观层 MUST 播放配置中的 `RunEnd`
- **AND** 外观层 MUST NOT 等待动画完成后主动切换逻辑状态

#### Scenario: 避免重复重播
- **WHEN** 连续多帧收到相同移动阶段和相同 alias key
- **THEN** Animancer 外观层 MUST 避免每帧从头重播同一个阶段动画

#### Scenario: 调试状态可见
- **WHEN** 动画外观层接收移动动画上下文
- **THEN** 系统 MUST 暴露当前阶段、当前动画名和当前速度作为只读调试信息

#### Scenario: 外观层不接管逻辑
- **WHEN** Animancer 外观层播放 `RunEnd`
- **THEN** 外观层 MUST NOT 调用状态机切换 API
- **AND** MUST NOT 调用运动执行端口
- **AND** MUST NOT 写入角色 Transform

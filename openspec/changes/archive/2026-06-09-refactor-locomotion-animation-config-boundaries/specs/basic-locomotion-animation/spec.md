## MODIFIED Requirements

### Requirement: 基础移动动画配置
系统 MUST 使用 ScriptableObject 或等价配置表达当前基础移动 Run-only phase 到 Animancer alias 的映射，并 MUST 保持 Animancer TransitionLibrary / TransitionAsset 作为 clip、fade、speed、normalized start time 和动画事件的播放参数权威。当前配置 MUST NOT 新增 `WalkStart / WalkLoop / WalkEnd`。

#### Scenario: 四阶段 alias 可配置
- **WHEN** 设计者配置当前基础移动动画
- **THEN** 配置模块 MUST 暴露 `Idle / RunStart / RunLoop / RunEnd` 的 alias key
- **AND** 配置模块 MUST NOT 暴露 `WalkStart / WalkLoop / WalkEnd`

#### Scenario: 播放参数由 Animancer 管理
- **WHEN** 设计者需要调整某个基础移动动画的 clip、fade、speed 或 normalized start time
- **THEN** 设计者 MUST 在 Animancer TransitionLibrary 或 TransitionAsset 中配置
- **AND** 项目侧 Run 基础移动配置 MUST NOT 重复暴露这些播放参数

#### Scenario: 更换角色 alias
- **WHEN** 更换角色或更换 Animancer alias
- **THEN** 设计者 MUST 能通过项目侧配置资产替换 `Idle / RunStart / RunLoop / RunEnd` 的 alias key
- **AND** 不需要修改移动逻辑状态机代码

#### Scenario: RunEnd 退出时长是逻辑数据
- **WHEN** 设计者配置 `RunEnd`
- **THEN** 配置模块 MUST 能提供 `RunEnd` 对应的 `MoveStop -> Idle` 退出时长
- **AND** 该时长 MUST 以纯数据形式供逻辑层使用
- **AND** 该时长 MUST NOT 要求状态机读取 Animancer、AnimationClip 或 TransitionAsset

### Requirement: Animancer 基础移动外观层
系统 MUST 提供一个 Animancer 基础移动外观层，消费移动动画上下文并使用 Run-only alias 配置播放 `Idle / RunStart / RunLoop / RunEnd`。外观层 MUST 只负责动画播放请求，不负责状态机切换、打断仲裁或位移执行。

#### Scenario: 阶段驱动动画播放
- **WHEN** 移动动画上下文阶段为 `MoveLoop`
- **THEN** Animancer 外观层 MUST 播放配置中的 `RunLoop` alias
- **AND** 该播放逻辑 MUST 集中在动画外观层内

#### Scenario: 停止阶段播放 RunEnd
- **WHEN** 移动动画上下文阶段为 `MoveStop`
- **THEN** Animancer 外观层 MUST 播放配置中的 `RunEnd` alias
- **AND** 外观层 MUST NOT 等待动画完成后主动切换逻辑状态

#### Scenario: Presenter 不覆盖 Animancer 播放参数
- **WHEN** Animancer 外观层请求播放基础移动 alias
- **THEN** 外观层 MUST NOT 从项目侧 Run 配置覆盖 Animancer state 的 fade duration
- **AND** MUST NOT 从项目侧 Run 配置覆盖 Animancer state 的 speed
- **AND** MUST NOT 从项目侧 Run 配置覆盖 Animancer state 的 normalized start time

#### Scenario: 避免重复重播
- **WHEN** 连续多帧收到相同移动阶段和相同 alias key
- **THEN** Animancer 外观层 MUST 避免每帧从头重播同一个阶段动画

#### Scenario: 调试状态可见
- **WHEN** 动画外观层接收移动动画上下文
- **THEN** 系统 MUST 暴露当前阶段、当前动画名和当前速度作为只读调试信息

#### Scenario: 外观层边界
- **WHEN** Animancer 外观层播放基础移动 alias
- **THEN** 外观层 MUST NOT 调用状态机切换 API
- **AND** MUST NOT 调用运动执行端口
- **AND** MUST NOT 写入角色 Transform

## ADDED Requirements

### Requirement: 基础移动动画配置校验
系统 MUST 提供基础移动 Run-only alias 配置的校验能力，帮助设计者在普通 Inspector 或轻量 editor 中发现缺失 alias 和逻辑退出时长问题，但该校验 MUST NOT 接管 Animancer TransitionAsset 的播放参数编辑。

#### Scenario: 空 alias 报错
- **WHEN** `Idle / RunStart / RunLoop / RunEnd` 任一 alias key 为空
- **THEN** 校验结果 MUST 报告对应 alias 缺失

#### Scenario: 必需退出时长报错
- **WHEN** 校验要求 `RunEnd` 必须显式配置退出时长
- **AND** `RunEnd` 退出时长缺失
- **THEN** 校验结果 MUST 报告 `RunEnd` 退出时长缺失

#### Scenario: 不校验 Animancer 播放参数
- **WHEN** 设计者运行项目侧 Run 配置校验
- **THEN** 校验器 MUST NOT 把 Animancer TransitionAsset 的 fade、speed 或 normalized start time 作为本配置的错误来源

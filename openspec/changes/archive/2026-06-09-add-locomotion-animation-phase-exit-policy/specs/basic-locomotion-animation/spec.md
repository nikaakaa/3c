## MODIFIED Requirements

### Requirement: 基础移动动画配置
系统 MUST 使用 ScriptableObject 或等价配置表达当前基础移动 Run-only 四阶段动画语义。配置 MUST 以 `Idle / MoveStart / MoveLoop / MoveStop` phase config 组织，每个 phase config MUST 至少提供 alias key、退出策略和退出时长。Animancer TransitionLibrary / TransitionAsset MUST 继续作为 clip、fade、speed、normalized start time 和动画事件的播放参数权威。当前配置 MUST NOT 新增 `WalkStart / WalkLoop / WalkEnd`。

#### Scenario: 四阶段 phase config 可配置
- **WHEN** 设计者配置当前基础移动动画
- **THEN** 配置模块 MUST 暴露 `Idle / MoveStart / MoveLoop / MoveStop` 四个 phase config
- **AND** 每个 phase config MUST 能配置对应 Animancer alias key
- **AND** 每个 phase config MUST 能配置退出策略和退出时长

#### Scenario: 当前 Run-only alias
- **WHEN** 使用默认基础移动动画配置
- **THEN** `Idle` phase MUST 默认使用 `Idle` alias
- **AND** `MoveStart` phase MUST 默认使用 `RunStart` alias
- **AND** `MoveLoop` phase MUST 默认使用 `RunLoop` alias
- **AND** `MoveStop` phase MUST 默认使用 `RunEnd` alias
- **AND** 配置模块 MUST NOT 暴露 `WalkStart / WalkLoop / WalkEnd`

#### Scenario: 播放参数由 Animancer 管理
- **WHEN** 设计者需要调整某个基础移动动画的 clip、fade、speed 或 normalized start time
- **THEN** 设计者 MUST 在 Animancer TransitionLibrary 或 TransitionAsset 中配置
- **AND** 项目侧 Run 基础移动配置 MUST NOT 重复暴露这些播放参数

#### Scenario: RunEnd 退出时长不再是特例
- **WHEN** 设计者配置停止阶段
- **THEN** 配置模块 MUST 使用 `MoveStop` phase config 表达 `RunEnd` alias
- **AND** MUST 使用 `MoveStop` phase config 的退出策略和退出时长表达 `MoveStop -> Idle` 的等待时间
- **AND** MUST NOT 使用顶层 `runEndExitDuration` 特例字段作为唯一来源

#### Scenario: 起步退出时长同源
- **WHEN** 设计者配置起步阶段
- **THEN** 配置模块 MUST 使用 `MoveStart` phase config 的退出策略和退出时长表达 `MoveStart -> MoveLoop` 的等待时间
- **AND** 该时长 MUST 能在缺失或迁移期间回退到现有移动配置的起步时长

#### Scenario: 状态机只接收纯数据
- **WHEN** phase config 的退出策略和时长进入逻辑层
- **THEN** 系统 MUST 先将其转换为状态机可读取的纯数据
- **AND** 状态机 MUST NOT 读取 Animancer、AnimationClip、TransitionAsset 或 alias key

### Requirement: Animancer 基础移动外观层
系统 MUST 提供一个 Animancer 基础移动外观层，消费移动动画上下文并使用 Run-only phase config 播放 `Idle / RunStart / RunLoop / RunEnd` alias。外观层 MUST 只负责动画播放请求，不负责状态机切换、退出策略、打断仲裁或位移执行。

#### Scenario: 阶段驱动动画播放
- **WHEN** 移动动画上下文阶段为 `MoveLoop`
- **THEN** Animancer 外观层 MUST 播放 `MoveLoop` phase config 中的 alias
- **AND** 该播放逻辑 MUST 集中在动画外观层内

#### Scenario: 停止阶段播放 RunEnd
- **WHEN** 移动动画上下文阶段为 `MoveStop`
- **THEN** Animancer 外观层 MUST 播放 `MoveStop` phase config 中的 alias
- **AND** 外观层 MUST NOT 等待动画完成后主动切换逻辑状态

#### Scenario: Presenter 不读取退出策略
- **WHEN** Animancer 外观层请求播放基础移动 alias
- **THEN** 外观层 MUST NOT 读取 phase config 的 `exitPolicy`
- **AND** MUST NOT 读取 phase config 的 `exitDuration`
- **AND** MUST NOT 注册 Animancer OnEnd 来驱动基础移动状态切换

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

### Requirement: 基础移动动画阶段退出策略
系统 MUST 提供基础移动动画 phase config 的退出策略，使逻辑层能用纯数据判断某个阶段是否达到可退出时间。第一版退出策略 MUST 至少支持 `Manual` 和 `AfterDuration`。

#### Scenario: Manual 不产生时间退出事实
- **GIVEN** 当前 phase config 的退出策略为 `Manual`
- **WHEN** 逻辑层查询该 phase 是否达到退出时间
- **THEN** 查询结果 MUST 为 false
- **AND** 其它非时间条件仍可驱动状态切换

#### Scenario: AfterDuration 产生时间退出事实
- **GIVEN** 当前 phase config 的退出策略为 `AfterDuration`
- **AND** 退出时长为非负数
- **WHEN** 当前 phase 计时达到该退出时长
- **THEN** 逻辑层 MUST 能得到当前 phase 已达到退出时间的事实

#### Scenario: 配置校验
- **WHEN** phase config 的 alias key 为空
- **THEN** 校验结果 MUST 报告对应 phase 的 alias 缺失
- **WHEN** phase config 的退出策略为 `AfterDuration` 且退出时长小于 0
- **THEN** 校验结果 MUST 报告对应 phase 的退出时长非法

#### Scenario: 不校验 Animancer 播放参数
- **WHEN** 设计者运行项目侧 Run 配置校验
- **THEN** 校验器 MUST NOT 把 Animancer TransitionAsset 的 fade、speed、normalized start time 或 event 作为本配置的错误来源

## ADDED Requirements

### Requirement: Timeline 时钟域必须以 tick 为唯一权威

Timeline 逻辑判定 MUST 以整数 tick 为唯一权威时钟：clip enter/exit、cue、TreeClip、Tick lifetime 绑定的判定 MUST 使用整数 tick 比较。秒与归一化时间 MUST 只是派生读数，供采样、显示与表现插值使用。Timeline 帧基准（timeline 帧率）是资产存储语义的固定常量，MUST NOT 依赖 static 可变全局字段；tick 率 MUST 来自 pipeline 正式配置，两者仅在推进换算处相乘。

#### Scenario: 默认配置下行为不变

- **WHEN** pipeline tick 率为 60 且 timeline 帧率取同值
- **THEN** 每个 tick 推进一帧，播放速度、cue 与 TreeClip 判定时刻 MUST 与现状一致

#### Scenario: tick 率偏离 60

- **WHEN** pipeline tick 率配置为非 60 值
- **THEN** timeline 播放速度与判定时刻 MUST 按配置换算保持真实秒时长不变
- **AND** 系统 MUST NOT 读取 static FrameRate 全局

### Requirement: tick 率与 timeline 帧率比率必须确定性推进

每 tick 推进的 timeline 帧数 MUST 按 tick 率与 timeline 帧率的比率经整数累加换算；比率非整除时累加器余数 MUST 作为确定性整数状态进入 Timeline 播放快照，回滚 Capture/Restore MUST 对称保留。

#### Scenario: 高 tick 率推进

- **WHEN** tick 率为 timeline 帧率的两倍
- **THEN** 累加器 MUST 使每两 tick 推进一帧，任何回滚重放 MUST 产生相同帧游标序列

#### Scenario: 回滚后重放

- **WHEN** 播放被回滚到含累加器余数的快照并重放
- **THEN** 后续推进 MUST 与首次播放逐 tick 一致

### Requirement: 表现时间驱动模式必须是业务策略而非固定实现

Timeline 表现层的时间驱动 MUST 是显式业务策略，以策略对象（`IActionPresentationClockPolicy`）注入播放器：策略实现 MUST 完整封装该模式下播放器时钟的每帧行为，播放器消费点 MUST NOT 因模式产生分支。业务 MAY 选择自由播策略（按渲染 delta 自走，非确定、不回滚），MAY 选择 committed movement clock 跟随策略（消费正式 `CharacterPresentationFactFrame.MovementPlaybackClock`），MAY 选择 committed Action sample 插值跟随策略（动画时间为 committed 逻辑采样的连续函数，支持修正重演）。无论哪种模式，逻辑消费动画时间的场景（motion curve、foot window、motion warp）MUST 使用 tick 域数据，MUST NOT 读取表现私有动画时钟。各跟随模式的能力组件 MUST 保持可用，其采用与否由具体业务和 Clip clock source 装配决定，MUST NOT 强制全业务统一。

#### Scenario: 表现自由播业务

- **WHEN** 业务装配自由播策略
- **THEN** 播放器时钟 MUST 按渲染 delta 平滑推进，收到播放事件后 MUST NOT 被 tick 采样阶梯约束

#### Scenario: 策略注入即插件开关

- **WHEN** 新业务需要不同的表现时钟行为
- **THEN** MUST 通过新增策略实现并在装配时注入接入
- **AND** 播放器消费点与既有策略实现 MUST NOT 被修改

#### Scenario: CommittedMovement Clip 跟随正式移动时钟

- **WHEN** Clip 的 `CharacterClipPlayerClockSource` 为 `CommittedMovement`
- **THEN** 播放器 MUST 从正式 `CharacterPresentationFactFrame.MovementPlaybackClock` 驱动连续时间
- **AND** 同一 Committed movement clock identity 下的渲染帧 MUST 不因渲染帧率变化而改变 Clip 的逻辑经过时长

#### Scenario: 跟随逻辑时间轴业务

- **WHEN** 业务选择 committed 插值跟随模式
- **THEN** 表现动画时间 MUST 由 committed 采样点之间连续插值得出
- **AND** 回滚重放替换 committed 分支时 MUST 从当前可见状态平滑接管，不倒退、不硬重置

### Requirement: 编辑器吸附粒度必须等于 tick 步长

Timeline 编辑器 clip/cue 边界吸附粒度 MUST 等于会话 tick 步长（`1/tickRate`），MUST NOT 提供运行时无法表示的亚 tick 位置。编辑器预览刻度 MAY 与运行时 tick 率独立配置。

#### Scenario: 拖拽 clip 边界

- **WHEN** 作者在时间轴拖动 clip 边界
- **THEN** 落点 MUST 量化到 tick 步长网格
- **AND** 编辑器显示位置 MUST 与运行时判定位置一致

### Requirement: TimelineData 不得包含全局时间缩放字段

`TimelineData` MUST NOT 持有全局时间缩放（Scale）字段；时间速率调整 MUST 由播放请求或 clip 级字段显式表达，MUST NOT 保留无运行时语义的残留字段。

#### Scenario: 加载历史资产

- **WHEN** 反序列化含旧 Scale 字段的历史 Timeline 资产
- **THEN** 系统 MUST 忽略该数据且不保留字段定义




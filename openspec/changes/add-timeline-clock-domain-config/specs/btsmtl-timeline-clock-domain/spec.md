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

Timeline 表现层的时间驱动 MUST 是显式业务策略，以策略对象（`IActionPresentationClockPolicy`）注入播放器：策略实现 MUST 完整封装该模式下播放器时钟的每帧行为，播放器消费点 MUST NOT 因模式产生分支。业务 MAY 选择自由播策略（按渲染 delta 自走，非确定、不回滚），MAY 选择 committed movement 进度跟随策略（消费正式 `CharacterPresentationFactFrame.MovementPlaybackClock`），MAY 选择 committed Action sample 插值跟随策略（动画时间为 committed 逻辑采样的连续函数，支持修正重演）。`CommittedMovementPlaybackClock` MUST 被视为逻辑时钟派生事实，不得作为第三个时间域。无论哪种模式，逻辑消费动画时间的场景（motion curve、foot window、motion warp）MUST 使用 tick 域数据，MUST NOT 读取表现私有动画时钟。各跟随模式的能力组件 MUST 保持可用，采用与否 MUST 由 Network Model、角色表现域或业务域装配决定，MUST NOT 由单个 Clip 强制全局策略。

#### Scenario: 表现自由播业务

- **WHEN** 业务装配自由播策略
- **THEN** 播放器时钟 MUST 按渲染 delta 平滑推进，收到播放事件后 MUST NOT 被 tick 采样阶梯约束

#### Scenario: 策略注入即插件开关

- **WHEN** 新业务需要不同的表现时钟行为
- **THEN** MUST 通过新增策略实现并在装配时注入接入
- **AND** 播放器消费点与既有策略实现 MUST NOT 被修改

#### Scenario: locomotion 表现域选择 CommittedMovement

- **WHEN** locomotion 表现域装配模式为 `CommittedMovement`，且节点被标记为 locomotion 参与节点
- **THEN** 播放器 MUST 从正式 `CharacterPresentationFactFrame.MovementPlaybackClock` 驱动连续时间
- **AND** 同一 Committed movement clock identity 下的渲染帧 MUST 不因渲染帧率变化而改变 Clip 的逻辑经过时长

#### Scenario: locomotion 默认自由播放

- **WHEN** Network Model 未显式选择严格逻辑跟随模式
- **THEN** locomotion MUST 使用 `FreeRunPresentationClockPolicy`
- **AND** 表现层 MUST NOT 因未选择严格模式而读取或创建额外时间域

#### Scenario: 跟随逻辑时间轴业务

- **WHEN** 业务选择 committed 插值跟随模式
- **THEN** 表现动画时间 MUST 由 committed 采样点之间连续插值得出
- **AND** 回滚重放替换 committed 分支时 MUST 从当前可见状态平滑接管，不倒退、不硬重置

### Requirement: Timeline Track 与 Clip 必须声明执行域

Timeline Track MUST声明默认执行域 `Logic`、`Presentation` 或 `DualProjection`。`DualProjection` 只表示同一作者内容同时拥有 Logic 与 Presentation 输出，不得被解释为第三个时钟、第二个 playback 或把同一逻辑图在两个时钟各执行一次。历史资产缺少执行域字段时 MUST固定解释为 `Logic`，保持既有行为。Clip MAY在 Track 允许范围内声明有效域，但 MUST不保存具体 Network Model、Endpoint、Transport、Rollback 或具体时钟实现。

Timeline Runtime MUST继续直接遍历正式只读 Timeline 内容。`Advance` 与 `Present` MAY分别形成当前调用的 Logic Evaluation 与 Presentation Evaluation 结果分区，但 MUST NOT把 Track / Clip 编译为 Semantic operation、持久化操作表或第二份 Timeline 内容。帧存储、编辑器布局和时间单位可以复用同一 TimelineData；Logic 输出 MUST只由 SimulationTick 推进，Presentation 输出 MUST只由 PresentationFrame 推进。

#### Scenario: 历史 Timeline 保持逻辑语义

- **WHEN** 反序列化的历史 Track 或 Clip 没有执行域字段
- **THEN** 有效执行域 MUST为 `Logic`
- **AND** 系统 MUST NOT自动把既有 TreeClip、Cue、Window 或 Action lifecycle 迁移到 Presentation

#### Scenario: 普通表现 Track

- **WHEN** Track 的执行域为 `Presentation`
- **THEN** 动画、特效、音效和相机 Clip MUST按 PresentationFrame 推进
- **AND** 多个 PresentationFrame 之间 MAY连续更新
- **AND** MUST不产生 Gameplay fact、canonical input 或 SimulationTick

#### Scenario: DualProjection 内容

- **WHEN** 同一作者 Clip 声明 `DualProjection`
- **THEN** 当前播放 MUST在各自推进调用中形成独立的 Logic 与 Presentation evaluation 输出
- **AND** Runtime MUST继续直读该 Clip 的正式内容，不得生成可跨播放复用的操作表

#### Scenario: Gameplay Track

- **WHEN** Track 的执行域为 `Logic`
- **THEN** Cue、Window、Gameplay TreeClip 和 Action lifecycle MUST按 SimulationTick 推进
- **AND** Enter / Update / Exit MUST进入既有 Commit / Discard 事务

### Requirement: TreeClip 必须分离逻辑图执行与表现 Marker

Logic TreeClip 的既有 `AssetTree` / TimelineBody 图 MUST只在 SimulationTick 执行；其 Enter / Update / Exit、`TreeDecision` 退出与 Commit / Discard MUST只属于 Logic 投影。Presentation TreeClip MUST以 typed Presentation Marker（MarkerId、时间、`Pulse` 或 `Stateful` 生命周期类型与正式 payload binding）作为唯一表现事件源，MUST NOT绑定或执行 TimelineBody 图。`DualProjection` TreeClip MAY同时持有 Logic `AssetTree` 与 Presentation Marker，但 AssetTree MUST只执行一次，且 MUST NOT在 PresentationFrame 执行。

#### Scenario: Presentation TreeClip 跨过 Marker

- **WHEN** Presentation TreeClip 的视觉游标跨过特效、音效、相机或表现动画 Marker
- **THEN** PresentationFrame MUST产生对应 Presentation Event
- **AND** 该事件 MUST不等待下一次 SimulationTick
- **AND** 该事件 MUST不进入 Gameplay Commit / Discard 事实链

#### Scenario: TreeDecision 只作用于逻辑投影

- **WHEN** DualProjection TreeClip 的 Logic `AssetTree` 请求 `TreeDecision` 退出
- **THEN** 该请求 MUST只按既有 Logic Advance / Commit 协议改变 Logic TreeClip 生命周期
- **AND** Presentation Marker MUST继续由表现游标与 playback 生命周期处理，不得执行 AssetTree 或产生 Gameplay fact

### Requirement: Presentation Event 必须具有可调和的稳定生命周期

Presentation Event 的 `EventId` MUST由 `PlaybackHandle`、`Generation`、`ClipId`、`MarkerId` 与 `TraversalIndex` 组成。`TraversalIndex` MUST标识同一 playback generation 对该 Marker 的循环/经过次序：同一经过在多次 PresentationFrame 重采样时保持同一 identity，下一次循环经过 MUST产生新的 identity。Marker MUST声明 `Pulse` 或 `Stateful`：`Pulse` 只在跨越时交付一次，不进入活动事件集；`Stateful` 在有效区间内参与完整活动集调和。Presentation runtime MUST对 `Stateful` 事件进行调和：相同 identity keep、旧分支不再存在的 identity cancel、新 identity add。调和、去重与 cancel MUST只改变表现状态，MUST NOT写 Gameplay fact、canonical input、SimulationState 或 rollback 决策。

#### Scenario: 同一 Marker 被多次表现帧采样

- **WHEN** 同一 `EventId` 所在的 Marker 区间被多个 PresentationFrame 重复采样
- **THEN** 消费端 MUST只接收一次 add
- **AND** 后续采样 MUST把该事件视为 keep

#### Scenario: 循环再次经过同一 Marker

- **WHEN** 同一 playback generation 完成一次循环并再次跨过相同 ClipId 与 MarkerId
- **THEN** 新事件 MUST使用新的 `TraversalIndex`
- **AND** 消费端 MUST能够再次触发该 Marker

#### Scenario: 分支替换或停止

- **WHEN** 表现游标重采样到替换分支，或对应 playback 停止、generation 变化
- **THEN** Runtime MUST保留仍存在的相同 `EventId`、cancel 已消失或已停止 generation 的活动事件，并 add 新分支事件
- **AND** MUST NOT通过 Logic Commit / Discard 重放、补写或撤销 Gameplay 事实

#### Scenario: 一次性与持续表现事件

- **WHEN** `Pulse` Marker 被重复重采样，或 `Stateful` Marker 因 playback 停止离开有效区间
- **THEN** `Pulse` MUST只交付一次且不得伪造 cancel
- **AND** `Stateful` MUST按其 `EventId` 收到 keep 或 cancel

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




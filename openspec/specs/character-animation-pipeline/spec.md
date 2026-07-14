# character-animation-pipeline Specification

## Purpose
定义角色管线中的 Timeline 和动画输出链路：`TimelineNode` 只提交播放请求，`TimelinePlaybackScheduler` 负责推进播放时间和采样事实，动画姿态在表现帧按插值后的 Timeline 时间重新采样，轨道输出进入 animation、gameplay、presentation 和 sync 边界，最终动画应用由正式表现层完成。
## Requirements

### Requirement: BTSMTL 内部 TimelinePlaybackScheduler 是 Timeline 播放权威

CharacterBTSMTLPhase 内部的 TimelinePlaybackScheduler MUST是角色管线模式下 Timeline logic time、逻辑事实采样与表现帧 animation sampling 的唯一权威。TimelineNode、Timeline track 与 TimelinePlayer MUST不自主推进同一个 Timeline。Scheduler MUST为每次 request 保存稳定 AnimationPlaybackId 与 generation；logic completion、terminal animation sample、outgoing PresentationRetention 与 runtime Timeline clone disposal MUST是明确分离的阶段。

#### Scenario: TimelineNode 提交请求

- **WHEN** TimelineNode 被 BTSMTL RootTree tick 到
- **THEN** 节点 MUST向正式管线上下文提交 Timeline request
- **AND** request MUST获得稳定 playback identity
- **AND** Scheduler MUST接管该 request
- **AND** 节点 MUST不直接调用 Timeline 播放 API

#### Scenario: Scheduler 推进 active Timeline

- **WHEN** Scheduler 拥有 active Timeline record
- **THEN** 它 MUST在 logic tick 使用 fixed delta 推进 logic time
- **AND** MUST记录 previous/current logic time 与 loop cycle
- **AND** MUST将完成、失败或取消写回 request 状态

#### Scenario: Once Timeline 到达结尾

- **WHEN** Once Timeline 在 logic tick 到达 duration
- **THEN** Scheduler MUST立即写回 Succeeded 并停止后续 gameplay facts
- **AND** MAY保留 terminal animation sample 与已授权 PresentationRetention 所需的最小记录
- **AND** runtime clone MUST在对应 playback lifecycle 进入 Retired 后释放
- **AND** Scheduler MUST不充当 mixer 或 transition arbitrator

### Requirement: Timeline 轨道采样输出管线数据

Timeline 轨道 MUST按对应时钟采样并输出管线数据。AnimationTrack MUST在表现帧按插值后的 active、selected、terminal-pending 或 retained-outgoing Timeline 时间输出 AnimationProducerSample/Release；Decision TreeClip、VFX、SFX、Camera、Motion 与表现 Cue 等非动画轨道 MUST在 logic tick 输出对应数据。AnimationTrack sample MUST表达 playback generation、LayerId、clip local time、producer 内部 Weight、ease 与 loop context，MUST不表达跨 producer Priority 或 winner。

#### Scenario: 动画轨道采样

- **WHEN** PresentationFrame 采样 selected 或 retained-outgoing playback
- **AND** Timeline 时间落在 AnimationTrack 的有效 clip 范围内
- **THEN** AnimationTrack MUST输出匹配 playback generation 的 AnimationProducerSample
- **AND** clip 时间 MUST来自表现帧 visual Timeline time
- **AND** AnimationTrack MUST不直接播放 clip

#### Scenario: 动画 clip 离开范围

- **WHEN** 表现采样确认上一帧有效的 None clip 已离开有效范围
- **THEN** Timeline producer MUST对该 clip slot 提交 Release
- **AND** lifecycle MUST不把历史 sample 当作隐式 Hold

#### Scenario: 非动画轨道采样

- **WHEN** logic tick 推进 active Timeline
- **AND** 时间落在 gameplay window、motion、camera 或 cue 轨道范围内
- **THEN** 轨道 MUST将结果写入对应 pipeline output
- **AND** retained-outgoing 表现采样 MUST不再次执行这些轨道

### Requirement: Timeline 动画采样必须和逻辑事实采样分离

logic tick MUST推进 active Timeline 时间并采样 motion、window、cue、camera response 等事实；presentation frame MUST使用最近两个 logic Timeline 时间和 InterpolationAlpha 计算 visual Timeline time，再采样 selected 与 retained-outgoing AnimationTrack。逻辑 selection、Timeline sample time 与 Animancer fade time MUST是独立合同。系统 MUST不在 terminal 或 outgoing 表现阶段重新产生 gameplay facts。

#### Scenario: 表现帧高于逻辑 tick

- **WHEN** 两次 logic tick 之间执行多个 PresentationFrame
- **THEN** 每个表现帧 MUST使用当前 InterpolationAlpha 重新采样 selected AnimationTrack
- **AND** Sample MUST更新同一个 playback generation
- **AND** 动画姿态 MUST不停止在上一 logic tick 的离散时间

#### Scenario: Loop Timeline 跨循环边界

- **WHEN** loop Timeline 在 logic tick 中从结尾回到开头
- **THEN** 表现帧 MUST通过 cycle/time 计算连续 visual Timeline time
- **AND** AnimationTrack MUST更新同一个 playback generation
- **AND** window、motion 与 cue MUST仍只按 logic tick 采样

#### Scenario: terminal presentation

- **WHEN** Timeline 已在 logic tick 完成
- **AND** selected 或 outgoing 视觉状态仍需最终 sample
- **THEN** PresentationFrame MAY执行 animation-only terminal sample
- **AND** MUST不再次输出 window、cue、motion、camera 或 SyncFacts

### Requirement: CharacterPresentationStage 是 Unity 动画应用边界

CharacterPresentationStage MUST是 animation command batch、Timeline visual sampling、AnimationPlaybackLifecycle、AnimancerPlaybackAdapter 与 Unity Animator 的聚合边界。Stage MUST在同一表现帧先取得每层最终 selection，再完成 selected/outgoing samples，随后原子更新 lifecycle，最后用真实 presentation delta 推进 Animancer。Timeline、StateMachine 与 Graph MUST不绕过 Stage 直接写 Animator/Animancer。

#### Scenario: target sample 与 selection 同批

- **WHEN** target 第一份合法 sample 与最终 selection 在同一批次到达
- **THEN** Stage MUST先完成 sample 收集
- **AND** lifecycle MUST原子切换 Current
- **AND** Animancer MUST不看到 source release 与 target sample 之间的空状态

#### Scenario: 多个 logic tick 同批

- **WHEN** 一个 PresentationFrame 前发生多个连续 selection
- **THEN** Stage MUST按 LayerId 使用最终 selection
- **AND** Complete、Release 与 playback generation MUST继续保序
- **AND** Stage MUST不构建 Driver 或 causal graph

### Requirement: 动画层预览只读取调试 Snapshot

系统 MUST从正式 AnimationPlaybackLifecycle 与 Animancer adapter 导出只读 AnimationPlaybackFrameSnapshot 或等价数据。Snapshot MAY包含每层 selection、sample time、PendingFirstSample、Current、Outgoing、Retired、Animancer state key 与 fade progress，MUST不参与 gameplay 决策或最终播放。Timeline 编辑器预览 MUST使用与正式链路相同的 sampling、lifecycle 和 Animancer adapter。

#### Scenario: 生成每帧预览数据

- **WHEN** 正式或 preview session 更新动画
- **THEN** 系统 MAY导出当前 layer/playback lifecycle snapshot
- **AND** 编辑器 MUST只读取该 snapshot

#### Scenario: 运行时禁用调试历史

- **WHEN** 项目关闭动画历史采集
- **THEN** 系统 MAY不保存历史 snapshot
- **AND** 正式播放 MUST不依赖 snapshot

### Requirement: 不新增 Timeline 播放分裂路径

系统 MUST只保留一条角色 Timeline 主链：TimelineNode 提交请求，TimelinePlaybackScheduler 推进 logic time，轨道采样输出管线数据，CharacterPresentationStage 将 animation sample 交给播放生命周期与 Animancer。Timeline 编辑器预览 MUST复用同一轨道采样和播放合同。系统 MUST不新增 Workbench、旧 SO/config、TimelinePlayer autonomous tick、独立 PlayableGraph 预览权威或第二套动画仲裁器。

#### Scenario: 迁移旧直接播放逻辑

- **WHEN** TimelineNode 直接播放逻辑已被请求链替代
- **THEN** 实现阶段 MUST删除旧字段、旧绑定和旧评估调用
- **AND** MUST不保留兼容分支

#### Scenario: 迁移旧编辑器预览逻辑

- **WHEN** Timeline 编辑器仍引用旧 TimelinePlayer 或独立 PlayableGraph
- **THEN** 这些入口 MUST迁移到正式 preview session 或删除
- **AND** preview MUST不形成第二权威

### Requirement: TimelinePlaybackScheduler 必须支持回绕稳定的循环播放

`TimelinePlaybackScheduler` MUST 支持由 `TimelineNode` 请求的 `Loop` 播放模式。循环 request MUST 使用同一个 active record 持续推进，播放相位到达 Timeline duration 后回绕到开头，request handle、source identity 和 request 状态 MUST 保持稳定。

#### Scenario: 循环 request 到达 duration

- **WHEN** `TimelinePlaybackScheduler` 推进 `Loop` request
- **AND** 当前播放时间越过 Timeline duration
- **THEN** scheduler MUST 回绕 Timeline 本地播放相位
- **AND** request 状态 MUST 保持 `Running`
- **AND** scheduler MUST NOT 将该 request 标记为 `Succeeded`

#### Scenario: 循环 request 被取消

- **WHEN** 持有 request 的 `TimelineNode` 被 stop 或 reset
- **THEN** scheduler MUST 取消对应 active record
- **AND** 后续 tick MUST NOT 再从该 record 采样动画、motion、window 或 cue

### Requirement: Timeline 回绕采样必须覆盖边界两侧

循环 Timeline 的轨道采样 MUST 能表达本帧采样区间是否跨过 Timeline duration 边界。跨边界时，scheduler MUST 将采样拆成尾段和头段，确保边界附近的动画、motion、window、cue 和其它轨道事实不丢失、不重复。

#### Scenario: 动画采样跨过循环边界

- **WHEN** 上一采样相位位于 Timeline 末尾
- **AND** 当前采样相位回到 Timeline 开头
- **THEN** scheduler MUST 采样末尾区间和开头区间
- **AND** 输出动画贡献 MUST 表达这是同一个循环 request 的连续播放

#### Scenario: 动作窗口跨过循环边界

- **WHEN** loop Timeline 中存在 action window 或 cue 轨道
- **AND** 本帧采样跨过 duration 边界
- **THEN** scheduler MUST 按正式轨道语义采样尾段和头段
- **AND** 同一边界上的 window 或 cue MUST NOT 重复输出
- **AND** 缺少 Action Context 时 MUST NOT 伪造动作归属

#### Scenario: Motion 曲线跨过循环边界

- **WHEN** loop Timeline 中存在 motion 曲线或等价 motion 轨道
- **AND** 本帧采样跨过 duration 边界
- **THEN** scheduler MUST 按尾段 delta 加头段 delta 计算 motion 输出
- **AND** motion 输出 MUST 继续进入正式 motion pipeline

### Requirement: TimelinePlaybackScheduler 必须分阶段推进 TreeClip

`TimelinePlaybackScheduler` MUST 将 Decision TreeClip 求值放在 `PrepareDecisionFacts`，将 Commit TreeClip 生命周期放在 `Commit`。Scheduler MUST 使用同一 active playback time、cycle、owner 和 cancel 状态解释两种阶段。Decision MUST按本 tick 的 previous/current segment 判断相交，而不是只检查 target time；Loop 跨 duration 时 MUST拆分尾段、中间 cycle 与头段，并保证每个 TreeClip 在对应阶段每 Tick、每 cycle 最多执行一次。PresentationFrame MUST NOT 执行任何 TreeClip 逻辑。

#### Scenario: Decision 后 source Timeline 被取消

- **WHEN** Decision TreeClip 在 Prepare 阶段写入 Frame Blackboard
- **AND** RootTree 随后通过 State Transition 取消 source Timeline
- **THEN** source State.OnExit MUST 仍能读取本 Tick Decision 值
- **AND** Scheduler MUST 丢弃该 playback 尚未提交的 Commit Tree 输出

#### Scenario: Loop Timeline 跨越 TreeClip 边界

- **WHEN** Loop Timeline 在一个 logic tick 跨过 duration 边界
- **THEN** Scheduler MUST 按尾段和头段解释 TreeClip active range
- **AND** 同一 clip/cycle 的 Decision MUST NOT 重复执行
- **AND** Commit Tree runtime MUST 按 cycle identity 正确退出和进入

#### Scenario: Tick 完整跨过短 Decision TreeClip

- **WHEN** previous time 位于 Decision TreeClip 之前
- **AND** current time 位于该 clip 之后
- **THEN** 对应 Decision TreeClip MUST在本 tick 执行一次
- **AND** MUST不因 target time 已离开 clip 而漏执行

#### Scenario: Pipeline deactivate

- **WHEN** Character pipeline deactivate 或 dispose
- **THEN** Scheduler MUST ForceStop 并释放全部 active 和 terminal-pending Tree runtime
- **AND** TreeClip MUST NOT 在表现收尾阶段继续 Tick

### Requirement: TreeClip 运行不得恢复 Timeline 双权威

角色管线模式下 TreeTrack MUST 使用 Scheduler 管理的专用 Tree runtime 解释入口。实现 MUST NOT 为 TreeTrack 调用旧 Timeline `Bind`、`Evaluate`、`SetTime` 或 `Unbind` 来旁路显式轨道采样。

#### Scenario: 迁移 TreeTrack

- **WHEN** TreeTrack 被接入角色 Timeline playback
- **THEN** TimelineNode MUST 继续只提交播放请求
- **AND** Scheduler MUST 继续是唯一逻辑时间权威
- **AND** Animation、Motion、Window、Cue、Camera 和 Tree MUST NOT 形成两个推进时钟

### Requirement: 动画生命周期通道必须分离事实写入与批次消费权限

系统 MUST使用明确接口分离逻辑 AnimationLayerSelection 写入、Timeline AnimationProducerSample/Complete/Release 写入，以及 CharacterPresentationStage 批次消费。具体持久队列 MUST由 CharacterPipeline 唯一构造并保持 tick、sequence 与 playback generation。BTSMTL Tree scheduler MUST不直接依赖 animation command sink。

#### Scenario: 逻辑提交 selection

- **WHEN** State/Action 逻辑完成每层所有权决定
- **THEN** 它 MUST通过逻辑 selection sink 提交
- **AND** selection MUST不携带 animation Priority 或 transition

#### Scenario: Timeline 提交 sample lifecycle

- **WHEN** AnimationTrack 提交 Sample、Complete 或 Release
- **THEN** scheduler MUST通过 animation sample sink 写入
- **AND** 命令 MUST携带 playback generation

#### Scenario: Presentation 消费批次

- **WHEN** CharacterPresentationStage 开始 commit
- **THEN** 它 MUST复制完整 selection/sample batch
- **AND** 只有 lifecycle 与 Animancer adapter 成功更新后才能 acknowledge

#### Scenario: Pipeline 重置通道

- **WHEN** CharacterPipeline deactivate 或 dispose
- **THEN** composition root MUST清理同一个具体队列
- **AND** producer 与 Stage MUST不保留镜像 command state

### Requirement: Animation 与 Presentation 模块必须保持单向依赖

Animation 模块 MUST定义 playback identity、selection/sample 合同与 producer lifecycle；Presentation 模块 MUST负责表现帧聚合和具体 Animancer adapter。BTSMTL Logic MAY通过 CharacterGraphContext 提交逻辑 selection，但 BTSMTL core MUST不依赖 Animation lifecycle、Animancer、LayerPlan、Driver 或 Presentation Definition。Presentation MUST不反向解析 Tree route 或 State priority。

#### Scenario: 普通 Tree 进入或释放节点

- **WHEN** RunnableNode 执行或停止
- **THEN** 它 MUST只使用 Tree 逻辑合同
- **AND** MUST不向 Animation 模块发布 owner 或 topology fact

#### Scenario: Presentation 应用播放

- **WHEN** CharacterPresentationStage 执行表现帧
- **THEN** 它 MUST消费最终 selection 与 producer samples
- **AND** MUST不重新解释 State、Action 或 Tree priority

### Requirement: 逻辑层必须为每个动画层提交唯一播放选择

角色逻辑 MUST在 StateMachine transition、Tree interruption、ActionOverride 和其它业务 priority 完成后，为每个需要更新的 LayerId 提交零或一个 AnimationLayerSelection。逻辑选择 MUST以 ActionRuntime 当前唯一 ActionInstance 所有权为输入，不能把 Timeline request 启动时保存的 `ActionContext.IsValid` 当作当前赢家。该选择 MUST引用稳定 AnimationPlaybackId 与 generation。Animation 模块 MUST不接收候选列表，也 MUST不读取 State、Action 或 Tree priority。

#### Scenario: Action 覆盖 Locomotion

- **WHEN** ActionOverride 判定 Dodge 或 Attack 获得 Base 所有权
- **THEN** logic tick MUST为 Base 选择对应 Action playback
- **AND** Locomotion playback MAY继续按其逻辑语义推进
- **AND** Animation 模块 MUST不再次比较两者

#### Scenario: 动作结束返回 Locomotion

- **WHEN** Action 所有权结束
- **THEN** logic tick MUST根据当前输入和 Locomotion 状态选择 RunLoop、RunEnd、Idle 或其它正式 playback
- **AND** Animation 模块 MUST不从历史 sample 或表现状态推断返回目标

#### Scenario: 已结束动作留下旧 ActionContext

- **WHEN** Timeline request 仍保存历史 ActionContext
- **AND** ActionRuntime 当前没有该 ActionInstance 所有权
- **THEN** 逻辑选择 MUST不再选择该 action producer
- **AND** Scheduler MUST不因 `ActionContext.IsValid` 将其视为覆盖 locomotion

### Requirement: 目标播放准备就绪必须来自第一份合法 Sample

系统 MUST只在 Timeline visual sampling 为 selected playback 提交第一份匹配 generation 的合法 AnimationProducerSample 后，才允许该 target 从 PendingFirstSample 进入 Current。State entered、Runnable executed、Timeline request 创建或 producer authoring 存在 MUST不表示动画已准备。

#### Scenario: target 已执行但尚未采样

- **WHEN** target State 与 TimelineNode 已开始逻辑执行
- **AND** selected playback 尚无第一份表现 sample
- **THEN** lifecycle MUST保持 PendingFirstSample
- **AND** Current MUST不被清空

#### Scenario: 第一份合法 Sample 到达

- **WHEN** selected playback 的第一份合法 sample 到达
- **THEN** lifecycle MUST在同一表现批次请求 Animancer 播放 target
- **AND** source 与 target 之间 MUST不暴露中间 Empty

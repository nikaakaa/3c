# character-animation-pipeline Specification Delta

## ADDED Requirements

### Requirement: BTSMTL 内部 TimelinePlaybackScheduler 是 Timeline 播放权威
系统 MUST 使用 `CharacterBTSMTLPhase` 内部的 `TimelinePlaybackScheduler` 作为角色管线模式下 Timeline 播放和采样的唯一权威。`TimelineNode`、Timeline 轨道和 `TimelinePlayer` MUST NOT 在该模式下自主推进同一个 Timeline。

#### Scenario: TimelineNode 提交请求
- **WHEN** `TimelineNode` 被 BTSMTL RootTree tick 到
- **THEN** 节点 MUST 向正式管线上下文提交 Timeline 播放请求
- **AND** `TimelinePlaybackScheduler` MUST 在本帧或后续帧接管该请求
- **AND** 节点 MUST NOT 直接调用 Timeline 播放 API

#### Scenario: Scheduler 推进 active Timeline
- **WHEN** `TimelinePlaybackScheduler` 拥有 active Timeline record
- **THEN** 它 MUST 使用 pipeline tick context 的 deltaTime 推进播放时间
- **AND** 它 MUST 将完成、失败或取消状态写回请求状态表

### Requirement: Timeline 轨道采样输出管线数据
系统 MUST 让 Timeline 轨道按当前播放时间采样并输出管线数据。动画轨道 MUST 输出动画命令或动画贡献数据；Gameplay 窗口、VFX、SFX、Camera、Motion 和 FootPhase 等轨道 MUST 输出对应 pipeline 数据。轨道 MUST NOT 直接结算命中、扣血、改写角色 Transform 或绕过管线直接控制最终表现。

#### Scenario: 动画轨道采样
- **WHEN** active Timeline 时间落在 AnimationTrack 的 clip 范围内
- **THEN** AnimationTrack MUST 输出包含来源、层、clip 时间、权重、fade 和混合模式的动画数据
- **AND** AnimationTrack MUST NOT 直接调用 Animator、TimelinePlayer 或 PlayableGraph

#### Scenario: 非动画轨道采样
- **WHEN** active Timeline 时间落在 gameplay window 或表现 cue 轨道范围内
- **THEN** 轨道 MUST 将结果写入对应 pipeline output
- **AND** 结果 MUST NOT 绕过 strict gameplay、presentation 和 network 分层

### Requirement: 动画混合模型是运行时核心
系统 MUST 在运行时维护精简的动画混合模型，用于合并来自 Timeline、StateMachine、Tree、Action 或后续其它来源的动画贡献。该模型 MUST 表达层、来源、权重、时间、fade、优先级和最终层结果。

#### Scenario: 多来源贡献同一动画层
- **WHEN** 同一帧有多个来源向同一动画层提交贡献
- **THEN** 动画混合模型 MUST 按正式规则生成该层最终结果
- **AND** 该结果 MUST 成为表现层应用动画的输入

#### Scenario: Timeline 和状态行为同时提交动画
- **WHEN** active state 行为和 Timeline 轨道同时提交动画贡献
- **THEN** 系统 MUST 在同一动画混合模型中合并它们
- **AND** 系统 MUST NOT 让其中任意一方直接绕过 mixer 应用到 Animator

### Requirement: CharacterPresentationStage 是 Unity 动画应用边界
系统 MUST 让 `CharacterPresentationStage` 或其下属正式 adapter 成为最终写入 Animator、PlayableGraph 和 Unity 表现对象的边界。Timeline 轨道、TimelineNode 和状态机 runtime MUST NOT 直接应用最终动画。

#### Scenario: 应用动画混合结果
- **WHEN** 动画混合模型生成本帧结果
- **THEN** `CharacterPresentationStage` MUST 消费该结果并写入 Animator 或 PlayableGraph
- **AND** 其它 stage MUST NOT 直接写入同一个最终动画状态

#### Scenario: TimelinePlayer 保留为 adapter
- **WHEN** 后续实现继续使用 `TimelinePlayer` 封装 PlayableGraph
- **THEN** 它 MUST 位于表现层边界内
- **AND** 它 MUST NOT 成为 TimelineNode 或 TimelinePlaybackScheduler 之外的自主 tick 来源

### Requirement: 动画层预览只读取调试 Snapshot
系统 MUST 支持从动画混合模型导出 `AnimationBlendSnapshot` 或等价调试数据，用于编辑器显示每 tick 的动画层混合。Snapshot MUST 只作为调试和预览输出，MUST NOT 参与 gameplay 决策、transition 条件或最终动画应用。

#### Scenario: 生成每 tick 预览数据
- **WHEN** 动画混合模型在某帧生成结果
- **THEN** 系统 MAY 导出包含每层贡献列表、来源、clip 时间、权重和最终结果的 snapshot
- **AND** 编辑器预览 MUST 从 snapshot 读取显示数据

#### Scenario: 运行时禁用调试历史
- **WHEN** 项目不需要动画层预览或调试历史
- **THEN** 系统 MAY 不保留历史 snapshot
- **AND** 正式运行时混合结果 MUST 不依赖 snapshot 存在

### Requirement: 不新增 Timeline 播放分裂路径
系统 MUST 只保留一条角色管线 Timeline 播放主链路：节点提交请求，BTSMTL 内部 TimelinePlaybackScheduler 推进，轨道采样输出数据，PresentationStage 应用表现。系统 MUST NOT 新增并行 Workbench、旧 SO/config、TimelinePlayer autonomous tick 或第二套 TimelineNode 播放路径。

#### Scenario: 迁移旧直接播放逻辑
- **WHEN** `TimelineNode` 直接播放逻辑已被管线请求链路替代
- **THEN** 实现阶段 MUST 删除旧字段、旧绑定和旧评估调用
- **AND** 系统 MUST NOT 保留兼容分支继续支持节点直接播放

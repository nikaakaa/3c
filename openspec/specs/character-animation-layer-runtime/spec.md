# character-animation-layer-runtime Specification

## Purpose
定义角色动画层运行时：逻辑侧为每层提交唯一 `AnimationLayerSelection`，Timeline 在表现帧提供匹配 generation 的 `AnimationProducerSample`，`AnimationPlaybackLifecycle` 管理可见 producer 寿命，Animancer 负责实际 state、layer 与 fade，避免逻辑优先级和动画混合形成两套仲裁真相。
## Requirements

### Requirement: 动画层定义来自管线定义

系统 MUST使用 CharacterPipelineDefinition 内联的 CharacterAnimationPresentationDefinition 作为角色动画 Layer catalog 的唯一来源。每个 layer MUST显式保存 identity、order、Animancer layer index、mask、blend mode 与 AnimationLayerOutputPolicy。正式 catalog 中的每个 layer MUST由 Animancer adapter 应用，不得保存无运行语义的 apply flag。Timeline、Graph、Presenter、旧 SO 或独立 Layer asset MUST不保存另一份 layer 真数据。

#### Scenario: Base layer 要求持续输出

- **WHEN** Corin Base layer 配置为 RequireOutput
- **THEN** 正常激活期间该层 MUST拥有 Current、PendingFirstSample 或明确 Invalid 状态
- **AND** 系统 MUST不静默把该层解释为 Empty

#### Scenario: Optional layer 允许为空

- **WHEN** 某 layer 显式配置为 AllowEmpty
- **THEN** 逻辑层 MAY选择 None
- **AND** Animancer MUST按正式 transition 将该层淡出到空
- **AND** 系统 MUST不创建 fallback clip

#### Scenario: selection 引用缺失 layer

- **WHEN** AnimationLayerSelection 或 producer binding 的 LayerId 不存在
- **THEN** Pipeline MUST报告配置错误
- **AND** 对应 selection MUST不进入播放生命周期

### Requirement: 基础姿态必须由正式来源输出

Base pose、Idle、Move 与其它基础动画 MUST来自正式 Graph/State/Action 所选择的 Timeline animation producer。RequireOutput layer 在 target 首样本到达前 MAY保持已有 Current，但 MUST保留 PendingFirstSample target identity。Pipeline、lifecycle 与 Animancer adapter MUST不内置隐藏基础姿态 producer。

#### Scenario: 首次激活缺少基础动画

- **WHEN** RequireOutput Base 没有 Current
- **AND** 逻辑层没有合法 selection 或 selected target 没有 sample
- **THEN** lifecycle MUST报告明确 Invalid
- **AND** 系统 MUST不选择 bind pose clip、旧 locomotion 或隐藏 Idle

#### Scenario: 已有输出后 incoming 延迟

- **WHEN** Base 已有 Current A 且 selection 已变为 B
- **AND** B 的第一份 sample 尚未到达
- **THEN** lifecycle MUST保持 A 并记录 PendingFirstSample B
- **AND** MUST不把 A 重新声明为逻辑 winner

### Requirement: 角色管线不依赖旧动画播放路径

角色管线和 BTSMTL Timeline 编辑器预览 MUST共用一条语义：逻辑提交每层 selection，Timeline 生成 animation sample，AnimationPlaybackLifecycle 管理 producer 寿命，Animancer 应用 state/mixer/fade。系统 MUST不读取旧 AnimationPresentationPolicySO、旧 locomotion/action SO、旧 bodyclaim policy，也 MUST不依赖 TimelinePlayer autonomous playback、Animator.Play、Animator.CrossFade 或独立 PlayableGraph 作为另一权威。

#### Scenario: 搜索旧直接播放入口

- **WHEN** 实现阶段发现角色运行路径仍直接调用旧动画播放入口
- **THEN** 该引用 MUST删除或迁移到正式 Animancer adapter
- **AND** 系统 MUST不保留兼容分支

#### Scenario: BTSMTL 编辑器预览播放 Timeline

- **WHEN** Timeline 编辑器预览角色动画
- **THEN** 预览 MUST复用正式 Timeline sampling、AnimationPlaybackLifecycle 与 Animancer adapter
- **AND** 预览 MUST不创建独立仲裁器或 PlayableGraph 权威

### Requirement: 循环动画必须由连续 visual Timeline time 重采样

循环 Timeline/clip 的 continuous visual time MUST由 TimelinePlaybackScheduler 的 logic time、cycle 与 PresentationFrame interpolation 计算。AnimationPlaybackLifecycle MUST只关联 selected/current/outgoing producer，不得自行推进 Timeline clock或在两个离散 clip time 之间插值。

#### Scenario: 循环回绕

- **WHEN** loop Timeline 从末尾回绕到开头
- **THEN** AnimationTrack MUST使用连续 visual Timeline time 重采样同一 playback generation
- **AND** Animancer state time MUST使用本帧正式 sample 更新

#### Scenario: source 已停止

- **WHEN** 循环 source 的逻辑所有权已 release
- **AND** 其 Animancer state 仍为 Outgoing
- **THEN** Scheduler MAY通过 PresentationRetention 继续 animation-only sampling
- **AND** Timeline gameplay tracks MUST不再推进

### Requirement: 动画片段 membership 必须显式提交和释放

Timeline producer MUST显式提交 AnimationProducerSample、Complete 与 Release。进入或继续处于有效动画片段时 MUST提交 Sample；离开 ExtraPolationMode=None 片段、playback 失败或 producer 正式销毁时 MUST提交 Release。AnimationPlaybackLifecycle MUST不因当帧缺少 Sample 自动释放 Current，也 MUST不因历史 sample 存在而把无效 target 当作 ready。

#### Scenario: None 片段结束但 Timeline 继续

- **WHEN** Timeline 时间已经超过某 AnimationClip 的 EndTime
- **AND** 该 clip 的 ExtraPolationMode 是 None
- **THEN** producer MUST对该 clip slot 提交 Release
- **AND** 后续 sample MUST不继续包含该历史 clip

#### Scenario: Hold 片段结束但 Timeline 继续

- **WHEN** Timeline 时间已经超过某 AnimationClip 的 EndTime
- **AND** 该 clip 的 ExtraPolationMode 是 Hold
- **THEN** AnimationTrack MUST继续提交正式 Hold sample
- **AND** Hold MUST不来自 lifecycle 或 Presenter 的隐式 fallback

### Requirement: 动画层输入必须是已解析播放选择与正式采样

Animation 模块 MUST只接收逻辑层已经解析完成的 AnimationLayerSelection，以及 TimelinePlaybackScheduler 生成的 AnimationProducerSample、Complete 和 Release。每个 selection MUST表达 LayerId、AnimationPlaybackId、generation、logic tick 与 sequence，MUST不携带 Priority、authority、Tree route、Driver 或候选列表。每个 sample MUST表达同一 playback 在该 layer 内部的 clip time、loop context 与 clip weights。

#### Scenario: Base 收到唯一 target

- **WHEN** 逻辑层在一次提交中为 Base 选择一个 AnimationPlaybackId
- **THEN** Animation 模块 MUST只等待和播放该 target
- **AND** MUST不扫描其它 active Timeline producer 重新选赢家

#### Scenario: 同层重复选择

- **WHEN** 同一次逻辑提交为同一 LayerId 提供两个不同 playback
- **THEN** Pipeline MUST报告逻辑配置错误并拒绝该批选择
- **AND** Animation 模块 MUST不按 Priority、sequence 或提交顺序选择其中一个

#### Scenario: RequireOutput 本 tick 没有选择变化

- **WHEN** RequireOutput layer 已有 Current，且本次逻辑提交没有该 LayerId 的 selection
- **THEN** lifecycle MUST继续保留当前正式 selection 与 Current
- **AND** Pipeline MUST不把“无变更”转换为 Empty
- **AND** 若该 layer 首次启动既无 Current 也无 selection，lifecycle MUST明确报错

### Requirement: 动画播放生命周期必须只管理可见 producer 寿命

每个 LayerId MUST拥有一个 AnimationPlaybackLifecycleState，并只使用 PendingFirstSample、Current、Outgoing 与 Retired 表达播放寿命。PendingFirstSample MUST等待选中 target 的第一份合法 sample；Current MUST对应当前交给 Animancer 的 target；Outgoing MUST对应 Animancer 正在淡出的旧 state；Retired MUST释放该 producer 的表现 retention 与播放资源。该生命周期 MUST不解释 State、Action、Tree interruption 或业务 Priority。

#### Scenario: target 首样本延迟

- **WHEN** Current A 已存在且逻辑选择 B
- **AND** B 尚未产生第一份合法 sample
- **THEN** lifecycle MUST记录 PendingFirstSample B 并继续显示 A
- **AND** MUST不选择默认 Idle、Empty、当前 clip 副本或其它 producer

#### Scenario: target 首样本到达

- **WHEN** PendingFirstSample B 收到匹配 playback generation 的合法 sample
- **THEN** lifecycle MUST原子地请求 Animancer 播放 B
- **AND** A MUST进入 Outgoing
- **AND** B MUST进入 Current

#### Scenario: outgoing 淡出完成

- **WHEN** Animancer 报告 A 的 fade 已完成
- **THEN** lifecycle MUST将 A 标记 Retired
- **AND** MUST释放 A 的 PresentationRetention

### Requirement: Animancer 必须是实际动画混合权威

Animancer MUST负责 state/mixer 创建后的 layer 混合、fade weight、重入和最终 Animator 输出。AnimancerPlaybackAdapter MAY创建或复用 AnimancerState/ManualMixerState、写入 Timeline 采样时间和 producer 内部 child weights、调用 TransitionLibrary.Play 或 AnimancerLayer.Play，并将 easing 交给 FadeGroup。项目代码 MUST不计算 LayerPlan、incoming/outgoing state weight、ActiveHandoff 或自定义 crossfade 进度。

#### Scenario: producer 包含多个 clip

- **WHEN** 同一 Timeline producer 在一个 layer 内采样到多个重叠 clip
- **THEN** Adapter MUST用 ManualMixerState 表达 producer 内部 clip weights
- **AND** Animancer MUST负责该 state 与其它 state 的 fade

#### Scenario: fade 期间再次切换

- **WHEN** 当前 Animancer 视觉图仍在淡出 A 时逻辑选择 C
- **THEN** Adapter MUST从 Animancer 当前视觉状态播放 C
- **AND** 项目 MUST不建立 handoff stack 或恢复中间逻辑状态

### Requirement: outgoing producer 必须使用纯表现 retention

逻辑 producer release 后，AnimationPlaybackLifecycle MAY持有只读 PresentationRetention 让 TimelinePlaybackScheduler 继续生成 outgoing animation sample，直到 Animancer fade 完成。retention MUST不恢复 producer 的逻辑 membership，也 MUST不重新运行 TreeClip、Motion、root motion、window、cue 或 SyncFacts。

#### Scenario: 攻击逻辑已结束但动画仍淡出

- **WHEN** Attack playback 已停止 gameplay 输出且其 Animancer state 仍为 Outgoing
- **THEN** scheduler MUST只推进该 playback 的 animation visual sample
- **AND** Attack window、motion 和 hit facts MUST不再产生

#### Scenario: Pipeline deactivate

- **WHEN** pipeline deactivate 或 dispose
- **THEN** lifecycle MUST立即清理 Current、Outgoing、PendingFirstSample 与全部 retention
- **AND** MUST不等待 fade duration

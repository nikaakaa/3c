## ADDED Requirements

### Requirement: 统一动画贡献注册表必须拥有播放实例生命周期

系统 MUST 在所有动画 producer 和 `CharacterAnimationLayerRuntime` 之间使用来源无关的统一动画贡献注册表。每个提交 MUST 携带稳定 playback instance identity、稳定 contribution instance identity、runtime owner scope、authoring source identity 和显式 lifecycle。Registry MUST 至少区分 Active、CompletedHeld、Outgoing 和 Retired。Timeline、State、Tree、Action 或后续其它来源 MUST 使用同一合同，MUST NOT 各自维护并行播放注册表。

#### Scenario: 同一 TimelineNode 再次播放

- **WHEN** 同一个 TimelineNode 在不同状态 activation 或后续执行中再次提交同一 Timeline
- **THEN** 新播放 MUST 获得新的 playback instance identity
- **AND** 旧播放的 CompletedHeld 或 Outgoing entry MUST NOT 被新播放误更新

#### Scenario: Once Timeline 完成但 owner 尚未退出

- **WHEN** Once Timeline 已完成 logic playback
- **AND** 当前仍有效的动画 contribution 已提交 Complete
- **AND** 对应 state owner 尚未 transition 或 release
- **THEN** Registry MUST 保持该 contribution 为 CompletedHeld
- **AND** LayerRuntime MUST 继续从同一统一 registry snapshot 读取它
- **AND** Timeline Scheduler MUST NOT 建立独立保活 mixer

#### Scenario: Owner 正式释放

- **WHEN** owner transition、standalone owner release 或 pipeline dispose 明确释放 contribution
- **THEN** Registry MUST 将对应 entry 转为 Outgoing 或 Retired
- **AND** entry 完成 handoff 后 MUST 从最终 layer snapshot 中移除

#### Scenario: Owner handoff 等待 target ready

- **WHEN** Registry 已收到 source 到 target 的 owner transition
- **AND** target activation 尚未提交 `OwnerReady`
- **THEN** Registry MUST 保持 handoff pending
- **AND** source entries MUST 继续作为当前合法输出
- **AND** target ready 后才可将 source 转为 Outgoing 或 Retired

### Requirement: 动画片段 membership 必须显式提交和释放

系统 MUST 要求 producer 显式维护 contribution membership。进入或继续处于有效动画片段时 MUST 提交 Sample；离开 `ExtraPolationMode=None` 片段、playback 失败或 owner 正式释放时 MUST 提交 Release。Registry MUST NOT 仅因当帧缺少 Sample 自动释放 entry，也 MUST NOT 因历史 entry 存在而把无效片段自动保持为 Hold。

#### Scenario: None 片段结束但 Timeline 继续

- **WHEN** Timeline 时间已经超过某 AnimationClip 的 EndTime
- **AND** 该 clip 的 `ExtraPolationMode` 是 None
- **THEN** producer MUST 对该 contribution 提交 Release
- **AND** Registry MUST NOT 继续输出该 clip 的历史 sample

#### Scenario: Hold 片段结束但 Timeline 继续

- **WHEN** Timeline 时间已经超过某 AnimationClip 的 EndTime
- **AND** 该 clip 的 `ExtraPolationMode` 是 Hold
- **THEN** AnimationTrack MUST 继续提交正式 Hold sample
- **AND** Registry MUST 通过该 Sample 保持 contribution
- **AND** Hold MUST NOT 来自 Presenter 或 Registry 的隐式 fallback

## MODIFIED Requirements

### Requirement: 动画贡献是动画层唯一输入合同

系统 MUST 使用统一动画贡献提交和生命周期注册表快照作为角色动画层的唯一输入。Timeline、状态行为、Tree、Action 或后续其它来源如果需要影响角色动画，MUST 写入同一种动画贡献合同。贡献 MUST 至少表达 playback instance、contribution instance、owner scope、authoring source、clip、layer id、priority、clip time、weight 和 lifecycle。系统 MUST NOT 让任意来源绕过 registry 和动画层直接写入 Animator、Animancer、TimelinePlayer 或 PlayableGraph。

#### Scenario: Timeline 输出动画

- **WHEN** active 或 terminal-pending Timeline 的 AnimationTrack 采样到有效 clip
- **THEN** 轨道 MUST 写入统一动画贡献提交
- **AND** 提交 MUST 使用该 Timeline playback 和 clip slot 的稳定 runtime identity
- **AND** 轨道 MUST NOT 直接播放该 clip

#### Scenario: 状态行为输出动画

- **WHEN** Idle、Move、Attack 或 Hit 状态行为需要播放动画
- **THEN** 状态行为 MUST 通过正式节点、模块或 Timeline 写入统一动画贡献提交
- **AND** 提交 MUST 归属于当前 state activation 或其它正式 runtime owner
- **AND** 状态行为 MUST NOT 直接调用 Animator 或 Animancer 播放动画

### Requirement: 动画层运行时负责仲裁

系统 MUST 使用角色动画层运行时合并统一 registry 当前快照并生成最终播放计划。仲裁 MUST 至少处理 layer 分组、非法 layer、priority、override 权重归一、additive 贡献保留和 snapshot 输出。LayerRuntime MUST NOT 根据 producer 当帧是否重复提交来推断播放生命周期；Animancer adapter MUST NOT 承担这些业务仲裁。

#### Scenario: 同一 layer 有多个 override 贡献

- **WHEN** registry snapshot 中同一 layer 存在多个 override 贡献
- **THEN** 动画层运行时 MUST 选择最高 priority 组
- **AND** 同 priority 的 override 贡献总权重大于 1 时 MUST 归一化
- **AND** 低 priority override 贡献 MUST 不进入最终播放计划

#### Scenario: 同一 layer 有 additive 贡献

- **WHEN** registry snapshot 中同一 layer 存在 additive 贡献
- **THEN** 动画层运行时 MUST 保留合法 additive 贡献
- **AND** additive 贡献 MUST 与该 layer 的 additive/mask 约束一致

#### Scenario: CompletedHeld contribution 参与仲裁

- **WHEN** registry snapshot 包含尚未 owner release 的 CompletedHeld contribution
- **THEN** LayerRuntime MUST 按与 Active contribution 相同的 layer、priority 和 weight 规则处理它
- **AND** LayerRuntime MUST NOT 自行决定其释放时间

### Requirement: Animancer 只是最终播放 adapter

系统 MUST 将 Animancer 限定为角色动画层的最终 Unity adapter。`AnimancerAnimationPresenter` MUST 只消费统一 registry、LayerRuntime 和 PresentationStage 生成的最终动画播放计划，并按计划设置 Animancer layer、state、time、weight、mask 和 additive。它 MUST NOT 决定动作状态、transition、打断、Idle fallback、Timeline 播放时间或 contribution 生命周期。

#### Scenario: 应用播放计划

- **WHEN** 表现层生成最终动画播放计划
- **THEN** Animancer adapter MUST 为计划中的 clip 创建或复用 Animancer state
- **AND** adapter MUST 根据计划设置 state time、speed 和 weight
- **AND** adapter MUST 根据计划设置 layer mask、additive 和 layer weight

#### Scenario: 正式最终计划为空

- **WHEN** Registry 已完成显式 Release、owner handoff 和 outgoing retirement
- **AND** 最终动画层没有任何播放计划
- **THEN** Animancer adapter MAY 停止或静音自己管理的 state
- **AND** adapter MUST NOT 自动播放隐藏 Idle、默认 clip 或 fallback controller state

#### Scenario: Producer 本帧没有重复提交

- **WHEN** 某 producer 本表现帧没有新的 Sample
- **AND** Registry 中对应 contribution 尚未收到 Complete、Release 或 owner handoff
- **THEN** Animancer adapter MUST NOT依据 producer 缺席自行停止该 state
- **AND** 是否继续输出 MUST 由 Registry 当前快照决定

## MODIFIED Requirements

### Requirement: 动画层运行时负责仲裁

系统 MUST 使用角色动画层运行时合并 Registry 当前 snapshot、TransitionRuntime 生成的正式加权 snapshot，并生成最终播放计划。仲裁 MUST 至少处理 layer 分组、非法 layer、priority、override 剩余权重填充、同优先级归一、additive contribution 保留和 snapshot 输出。LayerRuntime MUST NOT 根据 producer 当帧是否重复提交来推断生命周期；Animancer adapter MUST NOT 承担业务仲裁。

#### Scenario: 同一 layer 有多个 override 贡献

- **WHEN** 同一 layer 存在多个 override contributions
- **THEN** LayerRuntime MUST 按 priority 从高到低处理 contribution 组
- **AND** 高优先级 contribution MUST 只占用其当前实际权重
- **AND** 低优先级 contribution MUST 填充该层剩余权重
- **AND** 同优先级总权重超过当前剩余权重时 MUST 在组内归一化

#### Scenario: 高优先级攻击尚未满权重

- **WHEN** Action override contribution 优先级高于 locomotion
- **AND** Action 当前 transition 权重小于 1
- **THEN** Action MUST 占用其实际权重
- **AND** locomotion MUST 填充剩余权重
- **AND** 系统 MUST NOT 将剩余权重暴露为默认姿势

#### Scenario: 同一 layer 有 additive 贡献

- **WHEN** 同一 layer 存在合法 additive contributions
- **THEN** LayerRuntime MUST 保留它们
- **AND** additive contributions MUST 遵守该 layer 的 additive 和 mask 约束

#### Scenario: CompletedHeld contribution 参与仲裁

- **WHEN** Registry snapshot 包含尚未 membership release 的 CompletedHeld contribution
- **THEN** LayerRuntime MUST 按普通 layer、priority 和 weight 规则处理它
- **AND** LayerRuntime MUST NOT 自行决定其释放时间

### Requirement: 状态切换混合必须使用 owner handoff 的 Registry 真相

状态切换时，Registry MUST 提供 source owner 最后合法 contributions 和 target owner 当前合法 contributions；`CharacterAnimationTransitionRuntime` MUST 负责冻结、捕获、策略推进、supersede 和 visual retirement。Registry MUST NOT 保存 pending/active handoff、transition elapsed 或 active blend session。系统 MUST NOT 将上一帧 adapter 播放状态当作 contribution membership 权威，也 MUST NOT 为获得 outgoing 继续 tick source State、Timeline 或 Action。

#### Scenario: ContributionCrossFade 切换

- **WHEN** Locomotion edge 使用 ContributionCrossFade
- **THEN** TransitionRuntime MUST 从 Registry 冻结 source owner 最后合法 contribution snapshot
- **AND** target owner 当前 contributions MUST 作为 incoming 输入
- **AND** Registry MUST NOT 将 source entry 转为 Outgoing session 来推进 blend

#### Scenario: Inertialization 切换

- **WHEN** edge 使用 Inertialization
- **THEN** Registry MUST 只提供 source/target membership 与 contribution snapshot
- **AND** TransitionRuntime MUST 从最终 visual pose 捕获 source pose 和 velocity
- **AND** Registry MUST NOT 保存骨骼 pose history

#### Scenario: incoming 状态未产出动画

- **WHEN** target 已 Ready 但没有合法 contribution
- **THEN** LayerRuntime MUST 暴露真实 Empty target
- **AND** 系统 MUST NOT自动播放隐藏 Idle、旧 locomotion clip 或 adapter fallback

### Requirement: 统一动画贡献注册表必须拥有播放实例生命周期

系统 MUST 在所有动画 producer 和 LayerRuntime 之间使用来源无关的统一动画贡献 Registry。每个提交 MUST 携带稳定 playback instance identity、contribution instance identity、runtime owner scope、authoring source identity 和显式 producer lifecycle。Registry MUST 至少区分 Active、CompletedHeld 和 Retired；animation transition 的 WaitingTarget、Capturing、Running、Superseded 和 Completed MUST 由独立 TransitionRuntime 持有。Timeline、State、Tree、Action 或后续其它来源 MUST 使用同一 Registry，MUST NOT 各自维护并行播放注册表。

#### Scenario: 同一 TimelineNode 再次播放

- **WHEN** 同一个 TimelineNode 在后续 activation 中再次提交同一 Timeline
- **THEN** 新播放 MUST 获得新的 playback instance identity
- **AND** 旧播放的 CompletedHeld entry MUST NOT 被新播放误更新

#### Scenario: Once Timeline 完成但 owner 尚未退出

- **WHEN** Once Timeline 已完成 logic playback
- **AND** contribution 已提交 Complete
- **AND** owner membership 尚未 release
- **THEN** Registry MUST 保持 contribution 为 CompletedHeld
- **AND** Timeline Scheduler MUST NOT 建立独立保活 mixer

#### Scenario: Owner membership 正式释放

- **WHEN** source 逻辑 owner 在 stop barrier 内释放 membership
- **THEN** Registry MUST 停止接受该 owner 的新 Sample
- **AND** 已由 TransitionRuntime 捕获的 visual snapshot MAY 独立完成表现收尾
- **AND** Registry MUST NOT 自己创建或推进 transition session

#### Scenario: TargetReady 等待

- **WHEN** TransitionRuntime 已收到 request
- **AND** target 尚未提交 TargetReady
- **THEN** TransitionRuntime MUST 保持 WaitingTarget
- **AND** Registry MUST 继续只处理普通 contribution lifecycle


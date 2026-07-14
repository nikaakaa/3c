## MODIFIED Requirements

### Requirement: 动画 visual playback 必须来自表现帧重采样和生命周期注册表

系统 MUST 让 PresentationFrame 根据 Timeline scheduler 保存的 logic sample 历史与 `InterpolationAlpha` 计算 visual Timeline time并重采样 AnimationTrack。采样结果 MUST 以稳定 playback、contribution 和 owner identity 进入 Registry；animation transition request MUST 进入独立 TransitionRuntime；二者再由 LayerRuntime 和 PresentationStage 生成 visual playback plan。该链路 MUST NOT 改变 Timeline logic playback time、TimelineNode 状态、Action window、Action cue、root motion、motion warp 或 SyncFacts。Animancer adapter MUST 只消费最终 visual plan 与正式 output job 数据。

#### Scenario: 同一 Timeline playback 跨 tick 存在

- **WHEN** 同一 Timeline playback 在 previous/current logic sample 之间保持有效
- **THEN** PresentationFrame MUST 计算连续 visual Timeline time 并重采样 AnimationTrack
- **AND** 重采样 MUST 更新同一个 playback/contribution instance
- **AND** animation transition MUST 使用 presentation delta 独立推进
- **AND** TimelinePlaybackScheduler 的 logic time MUST 不被 PresentationFrame 改写

#### Scenario: 动作窗口与动画显示分离

- **WHEN** 攻击 Timeline 的 HitWindow 在 logic tick 触发
- **THEN** HitWindow fact MUST 只在 logic tick 中产生
- **AND** animation resampling、CrossFade 或 Inertialization MUST NOT 重复提交该 HitWindow
- **AND** 网络同步事实 MUST 继续使用 logic tick fact

#### Scenario: Animancer 手动求值

- **WHEN** presenter 使用手动 `Evaluate(0)` 应用已算好的 visual clip time
- **THEN** TransitionRuntime 和 output job MUST 显式接收真实 presentation delta
- **AND** `Evaluate(0)` MUST NOT 被当作 transition elapsed 的时间来源

### Requirement: 表现插值必须提供调试可追踪性

系统 SHOULD 暴露当前 visual pose、visual animation 和 animation transition 调试信息，至少包括 previous/current logic tick、interpolation alpha、visual transform、playback/contribution/owner identity、visual Timeline time、最终 clip time、transition instance identity、strategy、lifecycle、target ready、elapsed、duration 和终止原因。Inertialization debug SHOULD 提供骨骼数量、最大 pose offset 和 velocity 摘要。调试信息 MUST NOT 成为 gameplay、ConditionRuleGraph、黑板或网络决策输入。

#### Scenario: 排查动画跳变

- **WHEN** 开发者查看攻击或闪避抢占的 runtime debug
- **THEN** debug SHOULD 显示 source、target、strategy 和 lifecycle
- **AND** debug SHOULD 显示 transition 是否等待 target、是否被 supersede 及替代者 identity
- **AND** debug SHOULD 显示最终应用到 Animancer 的 visual clip time
- **AND** Inertialization 时 SHOULD 显示 pose offset 与 velocity 摘要

## ADDED Requirements

### Requirement: Inertialization 必须基于最终输出姿态并保持表现层纯度

系统 MUST 在 Animancer layer 合成之后、后续 IK/程序化姿态之前插入正式 Unity animation output job。该 job MUST 捕获当前与前一表现帧的最终 local pose，计算 pose velocity，并对新 target pose 应用按 definition curve 衰减的位置和旋转偏移。Job MUST NOT 继续求值 source clip，MUST NOT 产生 gameplay、motion 或 sync facts。

#### Scenario: 攻击被闪避抢占

- **WHEN** 攻击 transition 尚在 Running 时闪避 request 到达
- **THEN** 旧 transition MUST 记录 Superseded
- **AND** 新 Inertialization instance MUST 从当前已修正最终 pose 与 velocity 重新捕获
- **AND** source 攻击 Timeline MUST NOT 继续 tick

#### Scenario: 缺少有效 rig binding

- **WHEN** Inertialization request 到达
- **AND** presenter 没有有效 Animator stream handles 或 native storage
- **THEN** 系统 MUST 报告配置错误
- **AND** 系统 MUST NOT 自动降级到 ContributionCrossFade、Idle 或旧 pose 保活

#### Scenario: transition 完成

- **WHEN** Inertialization elapsed 达到 duration
- **THEN** output offset MUST 衰减到完成边界
- **AND** transition-owned native data MUST 被释放或重置
- **AND** target animation MUST 继续按自身 visual time 播放

### Requirement: Animation Transition 重入必须从当前视觉结果接管

同一 runtime activation scope 在 active transition 完成前收到新 request 时，系统 MUST supersede 旧 instance，并从当前视觉结果建立新 source。系统 MUST NOT 直接 retire 旧 source 后从未修正 target pose 重启，也 MUST NOT建立无限 transition stack。

#### Scenario: CrossFade 中再次切换

- **WHEN** ContributionCrossFade Running 期间命中新 edge
- **THEN** 新 instance MUST 冻结当前 source/target 加权视觉 snapshot
- **AND** 旧 instance MUST 在新 capture 完成后 Retire

#### Scenario: Inertialization 中再次切换

- **WHEN** Inertialization Running 期间命中新 edge
- **THEN** 新 instance MUST 捕获当前已惯性修正的最终 pose 和 velocity
- **AND** 视觉输出 MUST 不先跳回旧 transition 的 target 基准 pose


# character-presentation-interpolation Specification

## MODIFIED Requirements

### Requirement: 角色表现插值必须基于 logic sample 历史

Presentation MUST从 Pipeline Egress允许并由 Committer提交的 BodyState sample生成 visual interpolation history。Rollback Presentation MUST从 Pipeline atomic Commit提交的 predicted/confirmed BodyState samples维护同一份 visual history；动画命令的Replay替换或撤销MUST按ActorId/EventId更新，Body分支的Replay替换MUST按ActorId/Tick整批更新，不得逐Replay Step把中间Body写入visual history。Committed Body分支替换期间的表现Tick游标MUST保持单调推进，只有显式stream reset或HardRecovery MAY重建游标；visual recovery MUST从当前可见姿态重新锚定，并在每个PresentationFrame按render delta继续收敛，MUST不因连续分支替换而反复把恢复进度归零。上述更新MUST不修改 Fixed `SimulationWorldStateSet`或已提交 `SimulationWorldSnapshot`。Presentation MUST不直接读取 WorldSimulationState、WorldSolver、runtime clone或 MotionDebug作为逻辑真值。

#### Scenario: Local Pipeline 提交 Body Sample

- **WHEN** Standard Local Pipeline发布一个成功 SimulationTickResult的 BodyState sample
- **THEN** Committer MUST提交唯一 BodyState sample 给 visual history

#### Scenario: Replay 替换 Predicted Pose

- **WHEN** Tick T 的 predicted BodyState 被 replay result 替换
- **THEN** Rollback Output Commit MUST暂存同一outer transaction的全部BodyResult并只提交Replay后的最终连续分支
- **AND** visual history MUST只触发一次branch replacement并从当前visual pose平滑接管

#### Scenario: 连续移动输入产生高频分支替换

- **WHEN** 相邻PresentationFrame持续收到canonical差异并替换Committed Body分支
- **THEN** 表现Tick游标 MUST不回退到每个新分支的起点
- **AND** visual recovery MUST在每个PresentationFrame继续减少当前姿态与新target之间的offset
- **AND** MUST不因恢复计时器反复归零而先冻结后跳变

#### Scenario: 远端角色保持当前预测时间线

- **WHEN** Peer使用last-known continuous input预测尚未到达的远端输入
- **THEN** 远端Body与动画MUST继续消费predicted current timeline
- **AND** confirmed horizon MUST不被用作远端表现延迟缓冲
- **AND** canonical差异到达后MUST通过同一原子Body/动画提交事务纠正

### Requirement: 表现插值不得产生同步事实

PresentationFrame MUST保持为 committed/predicted presentation command 的消费阶段。表现插值、EventId keep/replace/cancel、Animancer fade 和 visual recovery MAY产生 visual pose、playback state 与 diagnostics snapshot，但 MUST不生成 canonical input、state hash、rollback decision或 gameplay fact，也 MUST不写入 CharacterSimulationState、WorldSimulationState、SimulationIngress、`SimulationActorTickResult` typed facts 或 Model Output Adapter queue。网络与 SimulationState MUST不读取 visual root作为真值。

#### Scenario: 高帧率表现帧

- **WHEN** 多个 PresentationFrame 发生在两个 SimulationTick 之间
- **THEN** visual root 与 Animancer MAY连续更新
- **AND** MUST不创建额外 gameplay fact、input command 或 world snapshot

#### Scenario: Visual Correction 进行中

- **WHEN** visual root 正平滑过渡到 replay body sample
- **THEN** world state hash MUST不因 visual interpolation 改变

### Requirement: 动画重入必须从 Animancer 当前视觉图接管

同一 LayerId 在旧 state 尚未淡出时收到新 selected target，或 replay后 producer command被替换或重入时，AnimationPlaybackLifecycle MUST将 EventId变化提交给 Animancer，AnimancerPlaybackAdapter MUST调用 Animancer正式 Play/Fade从当前视觉 graph/state/weight接管。项目 MUST不冻结 FinalOutput、回放中间逻辑状态、清空 layer、建立 handoff stack，Rollback Pipeline MUST不维护第二套 CrossFade或动画时间轴。

#### Scenario: Dodge 淡出时进入 Run

- **WHEN** Dodge 仍为 Outgoing 且 Run target 首样本 ready
- **THEN** adapter MUST从当前 Animancer layer 状态播放 Run
- **AND** 画面 MUST不先跳回 Dodge 或 Idle 基准姿势

#### Scenario: Replay 改变 Attack Producer

- **WHEN** 原 predicted Attack2 producer 在 replay 后不再有效
- **THEN** lifecycle MUST按 EventId cancel/replace command 从 Animancer 当前视觉状态接管

#### Scenario: Replay 修正同一 Playback 的采样时间

- **WHEN** replay替换当前playback generation的SampleProducer command
- **THEN** Presentation Runtime MUST保留替换前的当前视觉采样时间
- **AND** MUST在后续PresentationFrame向纠正后的sample推进
- **AND** MUST不先清空Layer或重新显示replay中间sample

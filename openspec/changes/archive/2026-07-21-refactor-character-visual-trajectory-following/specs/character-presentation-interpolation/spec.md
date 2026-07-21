## MODIFIED Requirements

### Requirement: 角色表现插值必须基于 logic sample 历史

Presentation MUST从 Pipeline Egress允许并由 Committer提交的 BodyState interval生成visual history。`CharacterPresentationBodyState` MUST保留ActorId、Position、Rotation、LinearVelocity与Grounded；这些值 MUST来自正式Float32、Fixed或observed World Body，不得从Transform或表现帧差分反推。`CharacterBodyPresentationRuntime` MUST是committed interval历史、selected interval、表现时钟、stream reset/replacement、target trajectory sampling、visual correction和visual root pose的唯一owner。Rollback Presentation MUST从Pipeline atomic Commit提交的predicted/confirmed BodyState interval维护同一份visual history；Replay产生替换或撤销时，Body Runtime MUST按ActorId/Tick和显式stream update整批更新历史，不得逐Replay step显示中间Body。Committed branch replacement MUST只删除replacement起点及之后的旧样本，并 MUST在同一presentation sample tick比较旧、新target trajectory。Presentation MUST不修改Float32/Fixed WorldState、已提交Snapshot、Prediction state或Solver输入，也 MUST不直接读取WorldSimulationState、WorldSolver、runtime clone、Network私有history、Transform或MotionDebug作为逻辑真值。

#### Scenario: Local Pipeline 提交 Body Interval

- **WHEN** Standard Local Pipeline发布一个成功SimulationTickResult的BodyState interval
- **THEN** Committer MUST向Body Runtime提交唯一canonical kinematic interval
- **AND** Body Runtime MUST按presentation delta生成并应用visible pose

#### Scenario: Replay 替换 Predicted Pose

- **WHEN** Tick T的predicted BodyState被replay result替换
- **THEN** Rollback Output Commit MUST暂存同一outer transaction的全部BodyResult并只提交Replay后的最终连续分支
- **AND** Body Runtime MUST在同一presentation sample tick比较旧、新target position、rotation与velocity
- **AND** visual correction MUST从上一帧visible pose与visible velocity接管
- **AND** canonical Body MUST立即保持replay后的结果

#### Scenario: 连续移动输入产生高频分支替换

- **WHEN** 相邻PresentationFrame持续收到canonical差异并替换Committed Body分支
- **THEN** 表现Tick游标 MUST保持单调推进
- **AND** 每次替换 MUST从当前visible状态重新计算相对误差
- **AND** MUST不累计旧offset或重置固定时长恢复计时器

#### Scenario: 远端角色保持当前预测时间线

- **WHEN** Peer使用last-known continuous input预测尚未到达的远端输入
- **THEN** 远端Body与动画 MUST继续消费predicted current timeline
- **AND** confirmed horizon MUST不被用作远端表现延迟缓冲
- **AND** canonical差异到达后 MUST通过同一原子Body/动画提交事务纠正

#### Scenario: Grounded target发生分支纠偏

- **WHEN** 新target Body为Grounded且水平姿态需要视觉纠偏
- **THEN** Follower MUST只对水平position error执行有界收敛
- **AND** visible Y MUST直接使用target Y

### Requirement: Prediction变步调度与Owner表现时钟必须解耦

Owner Presentation MUST由唯一 `CharacterBodyPresentationRuntime` 从Committer提交的simulation body interval历史维护独立表现时钟，并按presentation delta推进。Body Source MUST在runtime创建时显式锁定为`CommittedStream`或`SelectedStream`，只决定target history与cursor；Body Trajectory Profile MUST独立显式锁定为`Direct`或`BoundedCorrection`，只决定target不连续时的visual response。两者 MUST不由camera ownership、Network Model类型、Actor名称或是否存在CameraRig推断。`CommittedStream` MUST服务本地owner和当前进程完整模拟的Actor；`SelectedStream` MUST只消费Model Egress已选择的Body interval与显式Reset。连续interval的正常target运动 MUST直接由相邻sample重采样，MUST不持续运行第二次SmoothDamp。`BoundedCorrection` MUST只在branch replacement、显式Reset或合同允许的不连续时激活，并 MUST使用Presentation-owned正式profile的half-life、maximum error与settle threshold；缺失或非法profile MUST拒绝runtime创建。

#### Scenario: Prediction outer tick产生零步

- **WHEN** 当前outer logic tick没有提交新的simulation body interval
- **THEN** CommittedStream visual root MUST继续到达当前body区间终点并保持
- **AND** MUST不从alpha零重新播放同一body区间

#### Scenario: Prediction outer tick产生双步

- **WHEN** 当前outer logic tick提交两个连续simulation body interval
- **THEN** Source Cursor MUST按sample tick顺序消费两个区间
- **AND** MUST不覆盖或跳过中间body sample

#### Scenario: 正常连续移动

- **WHEN** 新Body interval与当前target trajectory连续且没有branch replacement或Reset
- **THEN** visible pose MUST直接跟随渲染帧重采样target
- **AND** MUST不因BoundedCorrection profile产生持续低通拖尾

#### Scenario: BoundedCorrection收到新分支

- **WHEN** 新canonical分支在当前presentation sample time改变target pose或velocity
- **THEN** Follower MUST从当前visible pose/velocity与新target pose/velocity建立相对误差
- **AND** position与yaw error MUST分别受profile maximum约束
- **AND** error MUST按presentation delta与profile half-life收敛
- **AND** 小于settle threshold的误差 MUST立即归零

#### Scenario: 连续revision重新定向

- **WHEN** 上一次visual correction尚未settle又收到新branch revision
- **THEN** Follower MUST保持当前visible pose与velocity连续
- **AND** MUST以新target重新计算当前相对误差
- **AND** MUST不叠加另一条固定时长correction尾巴

#### Scenario: Selected Stream显式重置

- **WHEN** Model Egress执行HardRecovery并提交Reset interval
- **THEN** Source Cursor MUST重置selected stream identity和target anchor
- **AND** Follower MUST按显式Profile重新锚定或有界接管
- **AND** Network adapter MUST不直接写visual root

### Requirement: 表现插值必须提供调试可追踪性

系统 SHOULD暴露Body SourceMode、TrajectoryMode、previous/current logic tick、interpolation alpha、target/visible pose、target/visible velocity、Grounded、correction error/velocity、active、clamped、settled、branch/reset identity、visual Timeline time、每层selection、playback generation、PendingFirstSample、Current、Outgoing、Retired、Animancer state key、fade progress、retention与错误。Graph、StateMachine、Timeline、Body trajectory和Animation channel MUST区分逻辑执行、target sample、visible correction与播放生命周期；Debug MUST不成为Gameplay、selection、Blackboard、Follower或网络输入。

#### Scenario: 排查远端移动漂移

- **WHEN** Rollback remote Actor连续收到canonical branch revision
- **THEN** Body trace MUST同时显示target与visible position/velocity
- **AND** MUST显示correction是否active、是否被maximum clamp以及何时settle
- **AND** MUST显示每次retarget对应的branch/reset identity

#### Scenario: 排查 Action 与 Locomotion 快速切换

- **WHEN** Action结束、Locomotion selection恢复且MovingTurn同tick生效
- **THEN** Logic Trace MUST显示最终Base selection
- **AND** Timeline Trace MUST显示target sample time
- **AND** Animation Trace MUST显示Current/Outgoing与Animancer fade

#### Scenario: duplicate selection

- **WHEN** 同一logic commit为Base提交两个不同playback
- **THEN** debug MUST显示两个逻辑来源与冲突
- **AND** MUST不显示伪Selected Driver或动画侧winner

#### Scenario: missing first sample

- **WHEN** selected target在release前始终没有合法sample
- **THEN** debug MUST显示playback generation、LayerId与lifecycle error
- **AND** MUST不伪造fallback output

### Requirement: Timeline pose time 与 Animancer fade time 必须独立连续推进

CharacterSimulationState MUST保存Timeline logic time，Presentation Source Cursor MUST提供表现帧重采样所需visual Timeline time，Animancer MUST以presentation delta推进fade。Body Visual Trajectory Follower MUST只修改visible body pose，不得修改AnimationSampleTick、AnimationSampleAlpha、Animancer delta或playback generation。四者 MUST不共享一个mutable clock，也 MUST不把表现时间或correction进度写回CharacterSimulationState。

#### Scenario: 两个 Logic Tick 之间渲染

- **WHEN** PresentationFrame在下一个SimulationTick前推进
- **THEN** Body target sample、Animancer fade和visual animation sample MUST连续推进
- **AND** Timeline gameplay state MUST保持不变

#### Scenario: Body correction正在收敛

- **WHEN** BoundedCorrection正把visible body收敛到新canonical target
- **THEN** 动画 MUST继续按Source Cursor的predicted presentation time采样
- **AND** MUST不按position error减速、重启playback或生成第二个动画clock

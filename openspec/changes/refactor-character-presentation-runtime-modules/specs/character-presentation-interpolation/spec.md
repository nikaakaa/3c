## MODIFIED Requirements

### Requirement: 角色表现插值必须基于 logic sample 历史

Presentation MUST从 Pipeline Egress允许并由 Committer提交的 BodyState interval生成visual history。`CharacterBodyPresentationRuntime` MUST是committed interval历史、selected interval、表现时钟、stream reset/replacement、visual recovery/convergence和visual root pose的唯一owner。Rollback Presentation MUST从 Pipeline atomic Commit提交的predicted/confirmed BodyState interval维护同一份visual history；Replay产生替换或撤销时，Body Runtime MUST按ActorId/Tick和显式stream update更新历史而不修改Fixed `SimulationWorldStateSet`或已提交`SimulationWorldSnapshot`。Committed branch replacement MUST只删除replacement起点及之后的旧样本，并 MUST以旧分支与新分支在同一presentation sample tick的姿态差生成recovery offset；MUST不把相邻表现帧之间的正常位移计入纠偏。ServerAuthoritative或其它Network adapter MUST只提交canonical selected Body interval和显式Reset，MUST不计算visual body、保存visual velocity或把SmoothDamp结果反向伪装成canonical Body。Presentation MUST不直接读取WorldSimulationState、WorldSolver、runtime clone、Network history或MotionDebug作为逻辑真值。

#### Scenario: Local Pipeline 提交 Body Interval

- **WHEN** Standard Local Pipeline发布一个成功SimulationTickResult的BodyState interval
- **THEN** Committer MUST向Body Runtime提交唯一canonical interval
- **AND** Body Runtime MUST按presentation delta生成并应用visible pose

#### Scenario: Replay 替换 Predicted Pose

- **WHEN** Tick T的predicted BodyState被replay result替换
- **THEN** Body Runtime MUST在同一presentation sample tick比较旧分支与replay分支并只平滑真实姿态差
- **AND** replacement起点之前的有效body历史 MUST继续保留
- **AND** 相邻表现帧之间的正常移动 MUST不被当作recovery offset
- **AND** canonical Body MUST立即保持replay后的结果

#### Scenario: Network Model 提交 Selected Body

- **WHEN** Prediction Schedule为observed actor提交新的selected Body interval
- **THEN** Network adapter MUST把该interval和Reset语义提交给同一Body Runtime
- **AND** Network adapter MUST不产生presentation-only visual Body

### Requirement: Prediction变步调度与Owner表现时钟必须解耦

Owner Presentation MUST由内部 `CharacterBodyPresentationRuntime` 从Committer提交的simulation body interval历史维护独立表现时钟，并按presentation delta推进。Body时钟策略 MUST在runtime创建时显式锁定为`CommittedStream`或`SelectedStream`，MUST不由camera ownership、Network Model类型、Actor名称或是否存在CameraRig推断。`CommittedStream` MUST服务本地owner和在当前进程完整模拟的无相机Actor；`SelectedStream` MUST只消费Model Egress已选择的Body interval与显式Reset，并使用Character Presentation所有的正式remote visual profile。Prediction restore/replay替换旧body历史时，Presentation MAY保留上一帧visible pose并在visual root上收敛到新canonical body，但 MUST不修改World body、Prediction state或Solver输入。

#### Scenario: Prediction outer tick产生零步

- **WHEN** 当前outer logic tick没有提交新的simulation body interval
- **THEN** CommittedStream visual root MUST继续到达当前body区间终点并保持
- **AND** MUST不从alpha零重新播放同一body区间

#### Scenario: 无相机 Simulated Actor

- **WHEN** Deterministic Rollback remote peer Actor在当前进程完整执行Program但不拥有CameraRig
- **THEN** Factory MUST为其选择CommittedStream
- **AND** Body表现时钟 MUST不因无相机而退化为外部alpha直通

#### Scenario: Selected Stream视觉收敛

- **WHEN** observed actor收到相邻selected Body interval且若干PresentationFrame没有新interval
- **THEN** SelectedStream MUST继续按presentation delta插值并有界收敛到已提交target
- **AND** MUST不自行读取原始authority buffer选择另一tick

#### Scenario: Selected Stream显式重置

- **WHEN** Model Egress执行HardRecovery并提交Reset interval
- **THEN** Body Runtime MUST重置selected stream identity和visual recovery状态
- **AND** Network adapter MUST不直接写visual root

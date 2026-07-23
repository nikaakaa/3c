# character-presentation-interpolation Specification

## Purpose
定义逻辑运动学轨迹到表现根姿态的采样与有界纠偏边界，以及visual Timeline重采样、动画播放生命周期、显式Player时间连续性与Pose Plan独立连续推进的职责分离。
## Requirements
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

### Requirement: Motion visual pose 必须和逻辑 Transform 分离

系统 MUST区分 WorldSimulationState body与显式 visual root。WorldSolve Pass与 Pipeline Runtime唯一更新逻辑 body；PresentationFrame MUST只根据 committed/predicted BodyState samples与 interpolation alpha写 visual root，MUST不调用 Solver、不申请 restore、不修改 World state或产生 correction result。

#### Scenario: Local Motion 插值

- **WHEN** previous/current committed body samples有效
- **THEN** PresentationFrame MUST计算并应用 visual pose
- **AND** WorldSimulationState MUST保持不变

#### Scenario: 后续模型执行 Hard Recovery

- **WHEN** Pipeline Runtime通过正式 restore恢复 World state
- **THEN** Committer MAY按模型 commit policy更新 visual sample history
- **AND** Presentation MUST不自行改写逻辑 body

### Requirement: Visual root 必须是正式配置

Character Host MUST显式持有 visual root/model root 与 Unity WorldSolver actor binding。缺少当前 composition 所需绑定时创建 MUST失败。系统 MUST不自动使用 CharacterController.transform、Animancer transform、子节点搜索、同名对象或 prefab扫描作为 fallback。

#### Scenario: Host 配置 Visual Root

- **WHEN** Host 创建 Local Corin
- **THEN** MUST将显式 visual root传入 Presentation adapter
- **AND** MUST将独立 actor body binding传入 Unity WorldSolver

#### Scenario: 缺少 Visual Root

- **WHEN** 角色需要表现插值但未配置 visual root
- **THEN** Host MUST报告配置错误

### Requirement: 动画 visual playback 必须来自表现帧重采样和生命周期注册表

`SimulationActorTickResult` MUST只提供AnimationChannel producer、EventId与playback intent。Presentation MUST把正式Timeline/MM采样降低为含raw visual time的`AnimationSelectionFrame`；显式MarkerSync、Player、Animancer source backend与编译Pose Plan MUST在PresentationFrame依次执行effective time解析、source sampling、时间连续性、空间合成和world-aware处理。Kernel MUST不记录Marker relation、Animancer state、Player entry、Pose Value或Pose Plan workspace。

#### Scenario: Attack Timeline 选中动画 Producer

- **WHEN** Committer 收到 compiled producer command
- **THEN** Presentation MUST通过Projection生成对应Animation Selection
- **AND** Pose Graph MUST只按typed edge消费其Player结果

### Requirement: 表现插值不得产生同步事实

PresentationFrame MUST保持为committed/predicted presentation command消费阶段。Visual interpolation、EventId keep/replace/cancel、Animation Selection、显式Player、Animancer source sampling、Pose Plan与visual recovery MAY产生visual pose、player state和diagnostics snapshot，但 MUST不生成canonical input、state hash、rollback decision或Gameplay fact，也 MUST不写CharacterSimulationState、WorldSimulationState、SimulationIngress、TickResult facts或Model Output queue。网络与SimulationState MUST不读取visual root、Player或Pose Graph作为真值。

#### Scenario: 高帧率表现帧

- **WHEN** 多个 PresentationFrame 发生在两个 SimulationTick 之间
- **THEN** visual root、Player、source sampling与Pose Plan MAY连续更新
- **AND** MUST不创建额外 gameplay fact、input command 或 world snapshot

#### Scenario: Visual Correction 进行中

- **WHEN** visual root 正平滑过渡到 replay body sample
- **THEN** world state hash MUST不因 visual interpolation 改变

### Requirement: 表现插值必须提供调试可追踪性

Diagnostics SHOULD暴露Body SourceMode、logic tick、interpolation alpha、raw visual Timeline time、AnimationChannel selection、playback generation、PoseNodeId、MarkerSync raw/effective time与relation、Player source usage、Blend Stack entry/Stored、Inertialization residual、Pose availability、参数来源、world-aware completion、final per-foot contribution与错误。Graph、StateMachine、Timeline、Body trajectory、Animation Channel、Player和Pose Graph MUST区分逻辑执行、target sample、visible correction、时间连续性与空间合成；Debug MUST不成为Gameplay、Selection、Player或Graph输入。

#### Scenario: 排查远端移动漂移

- **WHEN** Rollback remote Actor连续收到canonical branch revision
- **THEN** Body trace MUST同时显示target与visible position/velocity
- **AND** MUST显示correction是否active、是否被maximum clamp以及何时settle
- **AND** MUST显示每次retarget对应的branch/reset identity

#### Scenario: 排查 Action 与 Locomotion 快速切换

- **WHEN** Action结束、Locomotion selection恢复且MovingTurn同tick生效
- **THEN** Logic Trace MUST显示最终Base selection
- **AND** Timeline Trace MUST显示target sample time
- **AND** Animation Trace MUST显示action slot淡出、base slot transition和OutputPose来源

#### Scenario: duplicate selection

- **WHEN** 同一logic commit为Base提交两个不同playback
- **THEN** debug MUST显示两个逻辑来源与冲突
- **AND** MUST不显示伪Selected Driver或动画侧winner

#### Scenario: missing first sample

- **WHEN** selected target在release前始终没有合法sample
- **THEN** debug MUST显示playback generation、AnimationChannelId、PoseNodeId与lifecycle error
- **AND** MUST不伪造fallback output

### Requirement: Timeline pose time与显式Player time必须独立连续推进

CharacterSimulationState MUST保存Timeline logic time，Presentation Source Cursor MUST提供visual Timeline time，每个显式Player MUST以presentation delta推进自身sample或transition clock，Animancer MUST只按resolved sample descriptor采样。Body Visual Trajectory Follower MUST不修改Animation sample、Player delta、Pose Plan completion或playback generation。这些时钟 MUST不共享mutable state，也 MUST不把表现时间或correction写回CharacterSimulationState。

#### Scenario: 两个 Logic Tick 之间渲染

- **WHEN** PresentationFrame在下一个SimulationTick前推进
- **THEN** Body target sample、Player clock与visual animation sample MUST连续推进
- **AND** Timeline Gameplay state与Pose Graph topology MUST保持不变

#### Scenario: Body correction正在收敛

- **WHEN** BoundedCorrection正把visible body收敛到新canonical target
- **THEN** 动画 MUST继续按Source Cursor采样并按presentation delta推进Player/PoseGraph
- **AND** MUST不按position error减速、重启playback或生成第二个动画clock

### Requirement: 动画重入必须遵守显式Player连续性语义

同一AnimationChannel收到新selection identity或rollback替换时，`SelectedPosePlayer` MUST发布typed discontinuity；没有Inertialization时明确硬切。`BlendStack` MUST按其Blend Policy执行CrossFade或Stored Pose接管；局部`Inertialization` MUST按自身Policy决定HardCut或残差rebase。Animancer MUST只维护source sample；项目 MUST不冻结最终OutputPose、回放中间逻辑状态或建立第二套handoff stack，Rollback Pipeline MUST不维护第二套CrossFade、Inertial或动画时间轴。

#### Scenario: Dodge淡出到Empty

- **WHEN** FullBodyAction BlendStack的Dodge仍有贡献且action channel提交None
- **THEN** 该BlendStack MUST从当前视觉结果淡出到NoPose
- **AND** Pose Graph MUST连续显露BaseLocomotion，不先跳Idle或bind pose

#### Scenario: Replay 改变 Attack Producer

- **WHEN** 原 predicted Attack2 producer 在 replay 后不再有效
- **THEN** FullBodyAction Player MUST按图定义接管新source
- **AND** BaseLocomotion Player与Pose Graph topology MUST保持不变

#### Scenario: Replay 修正同一 Playback 的采样时间

- **WHEN** replay替换当前playback generation的SampleProducer command
- **THEN** Presentation Runtime MUST保留替换前的当前视觉采样时间
- **AND** MUST在后续PresentationFrame向纠正后的sample推进
- **AND** MUST不先清空Player、Pose Graph或重新显示replay中间sample

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

### Requirement: 稀疏网络动画Sample必须按authority tick区间重采样

Remote Presentation MUST允许当前Body插值区间右端tick的SampleProducer提前进入动画采样缓存，并按前后authority sample tick插值Timeline动画时间。可靠Select、Complete、Release、GameplayFact和Cue MUST仍只在authority presentation horizon到达后生效。存在合法右端SampleProducer时，Animation sampling MUST不把20Hz sample间隔作为过期条件转为无约束自由运行。

#### Scenario: 20Hz Snapshot驱动循环移动动画

- **WHEN** Remote body与SampleProducer分别在Tick 300和303形成当前插值区间
- **THEN** Presentation MUST按当前authority presentation time在两个Timeline sample之间插值
- **AND** Animancer MUST在渲染帧连续采样同一producer generation

#### Scenario: 新Producer样本早于可靠Selection到达horizon

- **WHEN** Tick 303的新Producer Sample已缓存但可靠Select尚未到达presentation horizon
- **THEN** Sample MUST只进入采样缓存
- **AND** 当前可见Selection MUST保持不变直到可靠Select正式发布

### Requirement: Remote Body表现与预测接触必须消费同一选择流

ServerAuthoritative Prediction Schedule MUST是Remote Body tick选择的唯一owner。Schedule为Current step产生并成功提交的selected Body frame MUST进入Remote Presentation Egress；声明`ObservedKinematicActorContact`能力的Composition还 MUST把同一选择转换为World观察约束。Remote Presentation MUST通过唯一 presentation-only visual pose convergence/filter 消费相邻 committed selected frame，在渲染帧插值，并在selected target被新权威信息替换后从当前visual pose有界收敛。filter MAY在零Current step的表现帧继续朝既有committed target收敛，但 MUST不重新读取原始authority Body选择另一tick、不维护独立Body delay cursor、不改变可靠事件horizon，也 MUST不把visual pose、visual velocity或error写回WorldSolver、Prediction state或contact body。

#### Scenario: 远端Actor阻挡本地owner

- **WHEN** Prediction使用Actor B的selected frame裁剪Actor A位移
- **THEN** Client A显示的Actor B Body target MUST来自同一selected frame
- **AND** visual filter MAY只改变到该target的渲染帧收敛过程
- **AND** MUST不出现碰撞体使用外推位置而可见角色使用另一延迟时间线

#### Scenario: 新权威样本替换短时外推

- **WHEN** 新remote authority Body使后续selected frame改变
- **THEN** canonical contact MUST从新frame立即参与后续World step
- **AND** Presentation MUST从当前visual pose有界收敛到新selected target
- **AND** 收敛参数 MUST来自Presentation Profile而不是Network Model

#### Scenario: Restore后执行Replay与Current

- **WHEN** 一个成功outer transaction先重放过去step再提交新的Current step
- **THEN** Remote Presentation MUST只接收Current step的selected Body frame
- **AND** Replay frame MUST不让可见远端角色倒退到历史tick

#### Scenario: Prediction当前产生零Current step

- **WHEN** clock correction使当前outer transaction没有新的Current step
- **THEN** visual filter MAY继续朝已经提交的Body target收敛或保持
- **AND** MUST不自行从原始authority样本选择新Body target

#### Scenario: Prediction执行HardRecovery

- **WHEN** formal HardRecovery替换当前Prediction分支
- **THEN** Model Egress MUST显式重置Remote selected Body stream
- **AND** visual filter MUST清除旧target、visual velocity和error state
- **AND** 后续成功Current step MUST以显式新anchor建立视觉区间

#### Scenario: 观察视觉误差

- **WHEN** visual pose尚未收敛到当前selected target
- **THEN** diagnostics MUST同时报告selected tick、target pose、visual pose和error
- **AND** diagnostics MUST不反向修改filter、Prediction或World state

### Requirement: Remote可靠表现事件必须服从selected Body horizon

Remote SampleProducer、Select、Complete、Release、GameplayFact与Cue MUST继续保留其authority tick和EventId。SampleProducer MAY提前进入采样缓存，但可靠事件 MUST不早于同tick selected Body frame已提交给Remote Presentation后发布。Presentation MUST不建立另一套Body authority timeline推进事件。

#### Scenario: 可靠Attack Select先于selected Body提交

- **WHEN** Remote Attack Select已到达但对应authority tick的selected Body尚未由成功transaction提交
- **THEN** Select MUST继续等待
- **AND** Body frame提交后 MUST按原EventId发布而不是生成新事件

### Requirement: Rollback 远端表现必须优先消费 Relayed Explicit Input 产生的当前分支

DeterministicRollback Peer MUST在Simulation执行阶段优先使用目标Tick已经到达的远端Relayed Explicit input，并将所得Body与动画producer lifecycle作为predicted current branch提交给现有Presentation Runtime。Presentation MUST不直接消费网络input、canonical packet或远端Transform。Confirmed horizon MUST不作为远端固定render delay。Canonical provenance晋升未改变GameplayHash时 MUST不生成Body correction、animation replace/retire或visual follower重新定向。

#### Scenario: 远端移动输入在执行前到达

- **WHEN** Peer B在Tick T执行前收到Peer A的Tick T Relayed Explicit MoveAxis
- **THEN** Fixed Program与KCC MUST用该输入生成Peer A的Tick T Body/动画输出
- **AND** Presentation MUST显示该predicted current branch

#### Scenario: Canonical Bundle 内容相同

- **WHEN** 后续canonical bundle与已经表现的Relayed Explicit input具有相同GameplayHash
- **THEN** Body history与animation lifecycle MUST保持当前分支
- **AND** visual correction MUST不启动

#### Scenario: Explicit Input 真正迟到

- **WHEN** Relayed Explicit input改变了已经执行的Tick T GameplayHash
- **THEN** Rollback output adapter MUST在同一outer transaction提交Replay后的最终Body与动画净分支
- **AND** visual follower MAY从当前visible pose收敛剩余误差

### Requirement: Rollback 动画同步必须来自同一 Gameplay 输入模拟

Rollback MUST从同一Gameplay input、Program执行与EventId replacement重新产生Animation Selection。网络协议 MUST不发送AnimationClip、Animator state、Animancer state、Player entry、Pose Value、normalized time或最终visual pose；PresentationFrame MUST按本地编译Pose Plan重新求值。进攻request的选择性延迟 MUST作用于Gameplay request eligible tick，使双方从同一SimulationTick开始对应动作，而不是由表现层等待或瞬切补齐。

#### Scenario: 双 Peer 进入 Attack Producer

- **WHEN** Offensive Attack request在Tick T变为eligible并进入双方同一Gameplay input history
- **THEN** 两端Fixed Program MUST从Tick T生成相同FullBodyAction producer lifecycle identity
- **AND** 各自PresentationFrame MUST在本地连续采样source并推进相同Player/Pose Plan合同

#### Scenario: 连续移动驱动循环动画

- **WHEN** Relayed MoveAxis持续到达且Locomotion状态保持Run
- **THEN** 远端BaseLocomotion producer MUST由本地模拟持续拥有
- **AND** 网络协议 MUST不逐帧同步Run动画时间

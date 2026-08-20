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

#### Scenario: Replay 替换已表现移动分支

- **WHEN** replay替换已经表现的Committed Body与Intent分支
- **THEN** Body branch sequence MUST表示新的history revision
- **AND** Presentation Fact的Pose discontinuity generation MUST保持不变
- **AND** PoseStateMachine、Clip Player、Root Orientation Warp与Presentation clock MUST继续当前Locomotion连续状态
- **AND** Foot Placement与Motion Matching trajectory MUST只重定向到新Body分支
- **AND** 只有Initialization或显式Selected Stream Reset MAY推进Pose discontinuity generation并执行硬重置

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

`SimulationActorTickResult` MUST提供构造Presentation Fact所需的committed Body/Intent、有限Action channel producer、EventId与playback intent。有限Action Sample MUST作为committed raw time锚点进入Action sample history，MUST不携带最终骨骼Pose。Presentation MUST重采样Body/Intent生成typed Fact，把正式Action Timeline采样降低为exact Action playback，并在相邻committed sample之间按presentation delta投影visual time。PoseStateMachine、ClipPlayer/BlendSpacePlayer、source-local Phase endpoint、Slot、Animancer source backend与编译Pose Plan MUST在PresentationFrame依次执行持续Pose选择、source phase/time解析、source sampling、Action插入、时间连续性、空间合成和world-aware处理。Kernel MUST不记录PoseState workspace、Phase relation、Animancer state、Player entry、Pose Value或Slot weight。

#### Scenario: 普通Locomotion表现帧

- **WHEN** Committer提供移动Body但没有Action producer
- **THEN** Presentation MUST重采样movement fact并求值Locomotion PoseStateMachine
- **AND** source-local Phase relation MUST只在匹配Profile Group时执行
- **AND** Slot MUST透传基础Pose

#### Scenario: Attack Timeline选中producer

- **WHEN** Committer收到compiled FullBodyAction producer command
- **THEN** Presentation MUST通过Projection生成对应Action playback
- **AND** FullBodyAction Slot MUST按typed edge消费

### Requirement: 表现插值不得产生同步事实

Visual interpolation、EventId keep/replace/cancel、Animation Selection、显式Player、Animancer source sampling、Pose Plan与visual recovery MAY产生visual pose、player state和diagnostics snapshot，但 MUST不生成canonical input、state hash、rollback decision或Gameplay fact，也 MUST不写CharacterSimulationState、WorldSimulationState、SimulationIngress、TickResult facts或Model Output queue。

#### Scenario: 高帧率表现帧

- **WHEN** 多个PresentationFrame发生在两个SimulationTick之间
- **THEN** visual root、Player与Pose Plan MAY连续更新
- **AND** MUST不创建额外Gameplay fact或world snapshot

### Requirement: 表现插值必须提供调试可追踪性

Diagnostics SHOULD暴露Body SourceMode、logic tick、interpolation alpha、Presentation Fact、PoseState active/target identity、TimeInState、Action channel selection、playback generation、Slot identity、source endpoint、Phase raw/effective time、actual coverage、source usage、BlendStack entry/Stored、Routing lifecycle、Inertialization residual、Pose availability、world-aware completion与错误。Debug MUST不成为Gameplay、State Rule、Selection、Slot、Player或Graph输入。

#### Scenario: 排查Action与Locomotion快速切换

- **WHEN** Action结束且Body从移动减速到静止
- **THEN** Logic Trace MUST显示Action release与committed Body
- **AND** Animation Trace MUST显示Slot退出、PoseStateMachine当前基础State与匹配的Phase relation

### Requirement: Timeline pose time与显式Player time必须独立连续推进

CharacterSimulationState MUST保存Gameplay Timeline logic time。`ActionCommittedSampleHistory` MUST保存已提交的Action raw sample锚点；`ActionPresentationSampleProjector` MUST按presentation delta在锚点之间生成独立`ProjectedPresentationSampleTime`；每个ClipPlayer、BlendSpacePlayer、Action Player与transition clock MUST只在PresentationFrame推进。新committed sample、rollback replacement或stream reset MUST按完整playback identity重基线表现投影。Animancer MUST只按resolved sample descriptor采样。Projected time MUST不覆盖committed raw time，不得写回Timeline或产生Window、Motion、Warp、Cue与Action lifecycle。持续Pose source MUST直接使用presentation-owned raw clock，并 MAY通过Projection Phase endpoint得到effective sample；它 MUST不进入Action committed sample history。Body Visual Trajectory Follower MUST不修改Animation sample、Player delta、Phase continuation、Pose Plan completion或playback generation。

#### Scenario: 两个Logic Tick之间渲染

- **WHEN** PresentationFrame在下一个SimulationTick前推进
- **THEN** Action projected time、ClipPlayer或BlendSpacePlayer、Slot transition与最终visual animation sample MUST连续推进
- **AND** Timeline Gameplay state、committed raw time与Action lifecycle MUST保持不变

### Requirement: 动画重入必须遵守显式Player连续性语义

同一Action AnimationChannel收到新selection identity或rollback替换时，AnimationSlot MUST按compiled Blend Logic执行Action source handoff；PoseStateMachine transition MUST独立按Fact和state edge执行。ClipPlayer、SelectedPosePlayer、BlendSpacePlayer、BlendStack与Inertialization MUST各自只拥有编译分配的连续性状态；Phase continuation MUST只属于source-local endpoint，不成为第二生命周期。Rollback Pipeline MUST不维护第二套CrossFade、Inertial、PoseState、Phase relation或动画时间轴。

#### Scenario: Replay改变Attack producer

- **WHEN** predicted Attack2在replay后被Attack1替换
- **THEN** FullBodyAction Slot MUST按图定义接管新source
- **AND** Locomotion PoseStateMachine与source-local Phase endpoint MUST继续由selected Body事实驱动

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

Rollback MUST从同一Gameplay input与Program执行重新产生committed Body/Intent、Action EventId replacement和有限Action Selection。Presentation MUST从修正后的Body/Intent重新构造Fact并本地求值PoseStateMachine、AnimationSlot与完整Pose Plan。网络协议 MUST不发送PoseState、AnimationClip、Animancer state、Player entry、Pose Value、normalized time或最终visual Pose。

#### Scenario: 对端修正动作与移动

- **WHEN** rollback重新产生新的Action selection和Body速度
- **THEN** 本地Slot MUST按新Action identity接管，PoseStateMachine MUST按新Fact重新求值
- **AND** 网络 MUST不发送或恢复旧Pose transition、Slot或BlendStack entry

### Requirement: Rollback Action 分支必须以确认边界提交终态

Rollback Output Adapter MUST在同一outer transaction内合并同一PlaybackId/generation的Select、Sample、Complete与Release候选，只向Action Playback Runtime提交最终Action branch revision。Select与Sample MAY预测提交并可被最终分支重基；Complete与Release MUST只在confirmed horizon后提交。撤销已消费的未确认Select或Sample MUST不调用会合成业务Release的通用Retire路径。confirmed terminal提交后，同generation的Sample MUST进入正式Faulted，不得恢复已确认终态。

#### Scenario: 未确认 Action 分支被撤销

- **WHEN** replay撤销已经表现的未确认Select或Sample
- **THEN** Action Playback Runtime MUST按最终branch revision重基
- **AND** MUST不生成业务Release
- **AND** Body Runtime、PoseStateMachine与Presentation clock MUST不因该Action重基被整体重置

#### Scenario: confirmed terminal 后收到同generation Sample

- **WHEN** CompleteProducer或ReleaseProducer已经在confirmed horizon提交后，同一generation再次提交SampleProducer
- **THEN** Action Playback Runtime MUST拒绝该命令并进入正式Faulted
- **AND** MUST不恢复已确认terminal对应的sample、Slot或source ownership

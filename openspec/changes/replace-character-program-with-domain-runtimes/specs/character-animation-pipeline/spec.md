## MODIFIED Requirements

### Requirement: CharacterSimulationPresentationRuntime必须执行唯一编译Pose Plan

角色表现 MUST使用唯一原生 Pose 图运行、Source、Constraint、Final Publication 和 Camera 装配。每帧 MUST消费已提交 Body／Intent、事件图变量和有限动作输入，按准备、唯一 Animancer Evaluate Barrier、原生图／IK 求值及最终提交运行。PoseState、Player、Slot、混合、惯性化、Foot、Goal、FBBIK 和骨骼写入顺序 MUST保持既有业务语义，不读取旧 Image 或补第二运行路径。

#### Scenario: 正常执行Foot Placement与FBBIK

- **WHEN** 原生Pose图实例执行到Foot Placement和PoseBone Goal节点并由Constraint Module形成合法Goal Set与FBBIK结果
- **THEN** 同一Frame MUST只发布一个Constraint Result、一个Program Output和一个Final Publication Result
- **AND** 外层Runtime MUST不理解Foot Context、Goal workspace或BendHistory

#### Scenario: Source target Pending

- **WHEN** Program发布候选target Demand且Source Module返回Pending
- **THEN** 是否保持当前合法State MUST只由Program节点语义决定并在Barrier前关闭Pending帧
- **AND** Source Module MUST不选择State，外层Runtime MUST不使用旧Timeline或默认Idle补洞

#### Scenario: Goal Slot重复

- **WHEN** 两个Goal Contribution尝试写入同一FBBIK Effector Slot
- **THEN** Constraint Module MUST在Physical Writer前使同一Frame typed Invalid
- **AND** Runtime MUST不按连接顺序覆盖、创建第二Goal Set或绕过FBBIK


### Requirement: 不得恢复Timeline或Preview分裂路径

系统 MUST只有一条技能 Timeline 运行路径和一条原生 Pose 图运行路径，通过 committed Body／Intent、EventId 和有限动作请求连接。旧 TimelinePlaybackScheduler、自主 TreeClip runtime、图外 root motion、Animancer direct Play 或独立 PlayableGraph MUST不得恢复。Timeline Preview MUST通过正式 Action adapter 接入同一表现运行，Pose Preview 与 MM Fixture 使用各自 typed 输入适配，不运行另一份姿态执行器。

#### Scenario: Runtime与Preview并存

- **WHEN** Editor预览Attack Timeline且游戏运行Corin Program
- **THEN** Preview state MUST不影响CharacterSimulationState
- **AND** Live Runtime MUST独占执行技能运行操作


### Requirement: 有限Action readiness必须来自第一份合法Sample

Action lifecycle MUST只以所选producer的第一份匹配generation的合法visual sample作为PendingFirstSample到Selected的readiness。Runnable completion、Kernel Finalize或Pipeline Commit MUST不伪造Ready。PoseState readiness MUST独立来自`PresentationPoseSourceSample`的Availability。

#### Scenario: 新Action尚无Sample

- **WHEN** Select已提交但合法Sample未到
- **THEN** Lifecycle MUST保持PendingFirstSample
- **AND** Slot MUST按已绑定播放语义维持当前合法输出


### Requirement: 动画表现帧必须使用预分配暂存事务

每 actor MUST使用唯一调参应用、Prepare、Validate、Animancer Evaluate Barrier 和 Seal 事务。原生图实例和对应节点 MUST在初始化时准备可复用状态与缓冲，跨帧 Committed 历史与本帧 Pending 结果分离；Source、Constraint 和 Final Publication 唯一拥有各自资源与结果页。根事务只保存身份、阶段和 typed 结果；正常帧不得通过复制整个图、状态或骨骼建立回滚点。所有 owner MUST共享帧身份与调参 generation，只有统一提交或丢弃能改变可见结果，不再按 Image 容量清单分配。

#### Scenario: 普通动画表现帧成功

- **WHEN** 一个Actor使用合法原生图资产完成普通Presentation Frame
- **THEN** 各Module MUST直接写自己的Pending页并由根事务统一提升同lineage结果
- **AND** 任一读者 MUST不观察到PoseState、Source、Foot、Goal、BendHistory或Final Pose的部分提交

#### Scenario: Evaluate前验证失败

- **WHEN** Pending Frame在Barrier前发现identity、容量、source ownership或binding非法
- **THEN** 根事务 MUST Discard全部Module Pending页、journal和prepared resource
- **AND** 原生图资产、Committed Actor State、Source ownership、Constraint Bank、Final Pose和Physical Bones MUST保持不变

#### Scenario: FBBIK后续阶段失败

- **WHEN** FBBIK已经更新Pending BendHistory但后续Pose stage或Writer验证失败
- **THEN** Committed Foot、Pelvis、Goal与BendHistory MUST全部保持上一成功帧
- **AND** 下一帧FBBIK MUST从上一Committed BendHistory重建Vendor状态

#### Scenario: Vendor存在未建模跨帧状态

- **WHEN** FBBIK Vendor对象中任一字段会影响下一帧结果但不能从Committed BendHistory、Profile和当前Goal精确重建
- **THEN** BendHistory迁移 MUST阻止实施完成并报告该状态所有权
- **AND** Runtime MUST不使用默认值、近似初始化或视觉相似结果替代8fc行为

#### Scenario: 正常提交Pose Constraint Bank

- **WHEN** Foot Placement、Goal Assembler、FBBIK和Final Writer全部通过同一Completion验证
- **THEN** Seal MUST只发布一个新的Committed Bank identity
- **AND** 任一正式读者 MUST不观察到左右脚、盆骨或BendHistory的部分提交


### Requirement: Dense状态与稀疏生命周期必须使用不同暂存策略

每帧完整生成的Pose、velocity、weight、parameter、Value、Operation completion、Inertialization next state、Constraint Result和Final Pose MUST直接写各Owner固定Committed/Pending页。PoseState、Player、ActionPlaybackInput lifecycle、Slot与Transition的小型状态 MUST使用原生图资产固定布局的Program pending state。Action command cursor、source ownership、usage、retirement与release handshake MUST使用固定容量mutation journal或prepared/deferred resource命令。在线调参 MUST使用Frame外的actor-local Graph/Source/Constraint Candidate Snapshot并一次提升Tuning Generation，不得写入原生图资产或混入Frame journal。系统 MUST不为了统一Interface复制完整Registry，也 MUST不把Dense Pose、Goal或Operation结果降低为逐项托管mutation。

#### Scenario: 本帧只有一个source release

- **WHEN** 当前Frame只释放一个旧source而其它source ownership不变
- **THEN** Source Module MUST只记录对应预验证release mutation与deferred command
- **AND** MUST不复制完整Physical Source Registry或把release字段写进原生图资产

#### Scenario: Program产生下一帧Pose

- **WHEN** 原生Pose图实例执行当前Frame全部活跃Pose节点
- **THEN** MUST把Value与completion直接写入原生Pose图实例自有Frame Pending页并只向根Transaction返回typed lease/result
- **AND** MUST不先复制上一Committed Value Workspace或通过旧Native Program持有两种寿命

#### Scenario: 本帧只有一个Action生命周期变化

- **WHEN** 当前帧只新增或推进一个Action playback而其它Registry entry不变
- **THEN** Runtime MUST只在固定journal中记录对应mutation
- **AND** MUST不复制完整Action registry或全部source ownership集合

#### Scenario: Pose Graph生成下一帧结果

- **WHEN** Native Pose Graph为当前帧求值全部PoseBone
- **THEN** Job MUST把结果直接写入Pending Native/Pose页
- **AND** MUST不先把Committed Pose页复制为Pending页


### Requirement: Animancer Evaluate必须是唯一不可逆提交门槛

唯一 Animancer Evaluate MUST继续作为表现帧不可逆 Barrier。进入前 MUST完成 Graph／Source／Constraint 调参候选的原子应用，以及图实例、资源、Rig、World、容量、readiness、观察需求和最终写入绑定检查。原生图准备入口 MUST完成活跃状态与 source demand，Source MUST完成对应采样／Playable／capture 准备，不提前提交任何 actor 状态或资源所有权。Barrier 及随后求值 MUST通过原生节点调用已有算法，各 Constraint 节点只调用一次对应模块，Final Publication 唯一写入。成功后只统一安装已验证 Pending 结果、journal、acknowledgement 和 deferred release；不得临时编译、扩容或重算诊断。Barrier 前失败丢弃，Barrier 内或之后失败阻止 Pending 发布并进入原故障边界。

#### Scenario: Barrier前Source Module失败

- **WHEN** Source sample或Prepared Resource在Barrier前Invalid
- **THEN** Runtime MUST不调用Animancer Evaluate并Discard全部Pending结果
- **AND** 原生Pose图实例 MUST不使用历史sample或默认Playable继续

#### Scenario: Barrier内Constraint失败

- **WHEN** Animancer Evaluate已经产生Component Pose但Constraint Result Invalid
- **THEN** Runtime MUST阻断后续Operation和Final Publication、Discard Pending并使Actor Runtime Faulted
- **AND** MUST不提交已经推进的Program、Source或BendHistory局部状态

#### Scenario: Barrier成功完成

- **WHEN** Program、Source、Constraint和Final Publication Result全部匹配同一lineage并完成
- **THEN** 根Seal MUST只执行预验证的no-throw页切换、journal、acknowledgement与deferred release
- **AND** Writer成功后 MUST不再运行可能失败的动画业务逻辑

#### Scenario: Barrier前Foot Placement静态准备失败

- **WHEN** Foot Placement的Profile、Rig、World Context、编译容量、静态Goal Slot或binding在进入Barrier前Invalid
- **THEN** Runtime MUST不执行FBBIK或Physical Writer
- **AND** 根Pending Bank MUST被Discard

#### Scenario: Barrier内Foot Placement运行结果失败

- **WHEN** Animancer Evaluate已经产生Component Pose，但Foot Placement Patch、运行时Goal lineage、Goal Assembler或FBBIK outcome在Barrier内Invalid
- **THEN** Runtime MUST阻断后续Pose stage与Physical Writer并Discard根Pending Bank
- **AND** 同一Actor Animation Runtime MUST进入Faulted，不得把该失败降级成可恢复的Barrier前Discard


### Requirement: Final Pose写入必须在整Rig验证后原子选择Committed或Pending结果

Final Publication MUST唯一拥有 Committed／Pending 最终姿态、完整物理骨骼绑定和 Publication Result。原生 Output 节点通过本 actor 的正式绑定交付结果，不保存编译 Output Family、layout handle 或第二份 Final Pose。静态图校验检查唯一 Output，实例工厂检查唯一 Writer。写入前 MUST验证骨骼数、availability、continuity、图／Constraint 完成状态、Rig 和本帧身份；合法时整体写入，失败时保持原帧事务的丢弃／故障语义。其它模块与诊断 MUST不写 Transform 或保存第二最终姿态。

#### Scenario: Pending Pose全部合法

- **WHEN** Program Output、Constraint Result和全部Physical binding合法
- **THEN** Final Publication MUST在同一Barrier一次写入完整Pending Pose并发布匹配completion的Result
- **AND** 根Seal MUST只提升该Result对应的Program、Source、Constraint与Final Pose页

#### Scenario: Pending Pose无效

- **WHEN** OutputPose、completion或任一Physical binding在Apply前无效
- **THEN** Final Publication MUST不留下部分Pending Physical Pose且根事务 MUST不提交任何Pending Module页
- **AND** Actor Runtime MUST进入现有Faulted路径，不得切换第二Writer、恢复Transform后继续或自动重建Runtime

#### Scenario: Writer成功后发布根Bank

- **WHEN** Writer已经完整写入匹配Completion的Pending Pose
- **THEN** Runtime MUST发布同Completion的Foot、Pelvis、Goal与BendHistory根Bank
- **AND** MUST不在发布前后执行新的业务查询或重新选择Goal


### Requirement: Physical Source资源生命周期必须延迟提交

新Source Visual、Mixer、Capture Playable、Clip State与物理source slot MAY在Prepare阶段创建为prepared resource，但 MUST不在Seal前取代Committed ownership。Prepare失败 MUST只释放本帧新建prepared resource。Committed source的disconnect、destroy、slot reuse与backend release MUST只由成功帧的固定deferred lifecycle command执行；容量 MUST来自明确图实例与资源绑定并在Runtime创建或Prepare时严格验证，不得动态扩容或回退其它source。

#### Scenario: 新Action source准备后帧失败

- **WHEN** Prepare为新Action创建了Source Visual但原生Pose图没有成功跨过Animancer Evaluate Barrier
- **THEN** Runtime MUST释放本帧prepared resource
- **AND** 原Committed source、usage和slot ownership MUST保持有效

#### Scenario: 旧source获得释放许可

- **WHEN** Slot usage消失、retirement permission和backend release依赖全部在成功帧内匹配
- **THEN** Runtime MUST在Seal后执行唯一deferred release command
- **AND** MUST不在原生Pose图成功前销毁旧Playable或复用其workspace槽位

### Requirement: Timeline回绕必须完整采样Gameplay边界

正式Timeline Runtime MUST在一个SimulationTick跨越loop边界时按尾段、中间cycle和头段稳定采样Gameplay tracks。Presentation sampler MAY按visual time回绕Action动画，但 MUST不补发Gameplay facts。持续Pose source的cycle MUST由state-local Player独立维护。

#### Scenario: 一Tick跨越Loop终点

- **WHEN** logic time从cycle尾部前进到下一cycle头部
- **THEN** Timeline Runtime MUST按正式区间顺序采样两侧Gameplay segment
- **AND** Presentation MUST不重复提交Window或Cue

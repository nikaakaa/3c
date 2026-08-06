## ADDED Requirements

### Requirement: 动画表现帧必须使用预分配暂存事务

`CharacterAnimationPresentationRuntime` MUST为每个Actor使用唯一`Prepare -> Validate -> Animancer Evaluate Barrier -> Seal`表现帧事务。Runtime创建时 MUST按Projection编译容量一次分配Committed/Pending Dense Pose页、Native workspace页、Inertialization页、Final Pose页、pending scalar state、mutation journal与source lifecycle command batch。PresentationFrame MUST只读取Committed状态并写Pending页或journal；MUST不通过`CaptureState`、`Clone`、`ToArray`、新建数组、Dictionary或List复制完整旧状态以建立回滚点。成功帧 MUST通过页索引交换和已验证journal提交新状态；失败帧 MUST只丢弃Pending，不恢复Committed对象图。

#### Scenario: 普通动画表现帧成功

- **WHEN** 一个Actor使用合法Projection完成普通PresentationFrame且没有诊断interest
- **THEN** Runtime MUST直接生成Pending Pose与Pending Module状态并在成功后交换Committed页
- **AND** 事务、Pose发布与关闭的diagnostics MUST不产生每帧托管分配
- **AND** Runtime MUST不复制完整BlendStack、Pose workspace、Inertialization history、Physical Source registry或骨骼Transform

#### Scenario: Evaluate前验证失败

- **WHEN** Pending帧在进入Animancer Evaluate前发现identity、容量、source ownership或release依赖不合法
- **THEN** Runtime MUST丢弃本帧Pending页、journal和prepared resource
- **AND** 已提交Action、PoseState、Slot、Transition、source ownership、Final Pose与Physical Bones MUST保持不变

### Requirement: Dense状态与稀疏生命周期必须使用不同暂存策略

每帧完整生成的Dense Pose、velocity、weight、parameter、Native result与Inertialization next state MUST直接写入固定Committed/Pending双页。Action registry、command inbox cursor、sample/Marker cursor、source ownership、usage、retirement与release handshake MUST使用固定容量pending scalar或mutation journal。journal MUST在Evaluate前完成identity、顺序、重复项、容量和依赖验证，Seal MUST只按固定顺序应用已验证mutation。系统 MUST不为了统一API把稀疏Registry整页复制，也 MUST不把Dense Pose降低为逐骨骼托管mutation对象。

#### Scenario: 本帧只有一个Action生命周期变化

- **WHEN** 当前帧只新增或推进一个Action playback而其它Registry entry不变
- **THEN** Runtime MUST只在固定journal中记录对应mutation
- **AND** MUST不复制完整Action registry或全部source ownership集合

#### Scenario: Pose Graph生成下一帧结果

- **WHEN** Native Pose Graph为当前帧求值全部PoseBone
- **THEN** Job MUST把结果直接写入Pending Native/Pose页
- **AND** MUST不先把Committed Pose页复制为Pending页

### Requirement: Animancer Evaluate必须是唯一不可逆提交门槛

唯一正式Animancer Graph Evaluate MUST作为动画表现帧不可逆Animancer Evaluate Barrier。进入Barrier前，Runtime MUST完成全部托管identity、容量、readiness、source、release、Job binding与Final Writer binding验证，并且 MUST不消费command acknowledgement、不提交lifecycle、不销毁Committed source、不发布release completion或Final Pose。Barrier之后的Seal MUST不执行动态查找、编译、扩容或业务输入验证，只可交换固定页、应用已验证mutation并发布成功结果。

#### Scenario: Barrier前Module准备失败

- **WHEN** Player、Slot、Routing或source backend在Prepare阶段报告不可提交结果
- **THEN** Runtime MUST不调用Animancer Evaluate
- **AND** MUST不修改Physical Bones或任何已提交外部资源

#### Scenario: Barrier成功完成

- **WHEN** Pending状态、Job binding与Final Writer binding全部通过验证且Graph Evaluate成功
- **THEN** Runtime MUST以同一completion identity Seal全部Module状态
- **AND** command acknowledgement、lifecycle、retirement、release completion与Final Pose MUST只属于该成功帧

### Requirement: Final Pose写入必须在整Rig验证后原子选择Committed或Pending结果

`AnimationFinalPosePhysicalWriter` MUST同时读取当前Committed Final Pose与本帧Pending Final Pose，并在写入任何Physical Bone前验证全部Physical Bone Transform binding、PhysicalBoneCount、Pose availability、continuity identity、graph completion和frame completion。全部合法时 MUST写入Pending Pose；typed Pending、Unavailable或Invalid时 MUST保持Committed Pose并禁止提交Pending页。由于该outcome在Evaluate Barrier内产生，外层Runtime MUST进入Faulted。Writer MUST不先写部分骨骼再报告失败，Physical Bone local pose MUST不再由表现帧事务提前捕获或在失败后恢复。

#### Scenario: Pending Pose全部合法

- **WHEN** Pose Graph完成且全部Physical Bone handle和Pending local pose合法
- **THEN** Final Writer MUST在同一Evaluate Barrier写入完整Pending Physical Pose
- **AND** Seal MUST把对应Pending Final Pose页提升为Committed

#### Scenario: Pending Pose无效

- **WHEN** Pose Graph发布typed Invalid、completion不匹配或任一Physical Bone在写入前验证失败
- **THEN** Final Writer MUST不把部分Pending Pose留在可见Rig
- **AND** Runtime MUST保持上一Committed Pose并丢弃本帧Pending结果
- **AND** 当前Actor Animation Presentation Runtime MUST进入Faulted并拒绝下一帧
- **AND** MUST不分配或恢复Physical Transform快照

### Requirement: Physical Source资源生命周期必须延迟提交

新Source Visual、Mixer、Capture Playable、Clip State与物理source slot MAY在Prepare阶段创建为prepared resource，但 MUST不在Seal前取代Committed ownership。Prepare失败 MUST只释放本帧新建prepared resource。Committed source的disconnect、destroy、slot reuse与backend release MUST只由成功帧的固定deferred lifecycle command执行；容量 MUST来自Projection并在Runtime创建或Prepare时严格验证，不得动态扩容或回退其它source。

#### Scenario: 新Action source准备后帧失败

- **WHEN** Prepare为新Action创建了Source Visual但Pose Plan没有成功跨过Animancer Evaluate Barrier
- **THEN** Runtime MUST释放本帧prepared resource
- **AND** 原Committed source、usage和slot ownership MUST保持有效

#### Scenario: 旧source获得释放许可

- **WHEN** Slot usage消失、retirement permission和backend release依赖全部在成功帧内匹配
- **THEN** Runtime MUST在Seal后执行唯一deferred release command
- **AND** MUST不在Pose Plan成功前销毁旧Playable或复用其workspace槽位

### Requirement: 动画表现异常必须区分Discard与Fault

预期的source Pending、readiness等待与Pose Unavailable MUST在Animancer Evaluate Barrier前通过正式outcome关闭Pending帧，不得依赖异常恢复。若typed Invalid只能在Barrier内确定，Runtime MUST保持Committed Physical Pose并进入Faulted。任何异常发生在Animancer Evaluate Barrier前时，Runtime MUST Discard Pending并向上抛错；发生在Barrier期间或之后时，Runtime MUST记录一次结构化Actor、PresentationFrame、BodyTick、completion与phase上下文，使该Actor的Animation Presentation Runtime进入Faulted并继续向上抛错。Faulted Runtime MUST拒绝后续Present调用，MUST不捕获全部骨骼和状态后尝试继续旧动画，也 MUST不自动重建Runtime或切换fallback路径。

#### Scenario: Prepare阶段发生不变量异常

- **WHEN** Animancer Evaluate Barrier前检测到非法generation或workspace identity
- **THEN** Runtime MUST Discard Pending并保留Committed状态
- **AND** 原异常 MUST继续向上报告

#### Scenario: Graph Evaluate发生不可预期异常

- **WHEN** Animancer Graph Evaluate或Barrier后的Seal发生无法证明无外部副作用的异常
- **THEN** 当前Actor Animation Presentation Runtime MUST进入Faulted并拒绝下一帧
- **AND** 系统 MUST不执行Physical Transform全量恢复后继续运行

## MODIFIED Requirements

### Requirement: 动画调试只能读取正式Snapshot

系统 MUST从`CharacterActionPlaybackRuntime`、PoseState provider、Player、Routing、source backend与Pose Plan导出只读snapshot。Snapshot MAY包含Action PlaybackId/channel/lifecycle、Projection-local dense source index、PlayerNodeId、generation、frame lease、Pending/Ready/Invalid、raw/effective sample、relation、source usage、transition、Inertialization residual、Pose contribution及ordered stage/FinalPublication completion。Snapshot MUST不参与Gameplay、source选择或最终播放，Editor MUST不从Animancer weight重建事实。Runtime MUST先读取显式Live、Capture、Pose Watch与detail interest；没有任何interest时 MUST不执行BlendStack、StateMachine、Inertialization、Operation contribution、Final Pose或逐骨骼weight复制。有interest时 MUST只从成功Seal的Committed页复制到预分配diagnostics页，不得读取或发布Pending帧。

#### Scenario: 导出每帧调试数据

- **WHEN** 正式Runtime或Preview完成表现帧且存在匹配diagnostics interest
- **THEN** MAY发布匹配Projection revision的只读snapshot
- **AND** Snapshot MUST只表达同一completion identity的Committed结果
- **AND** 关闭调试历史 MUST不影响正式播放

#### Scenario: 没有调试关注者

- **WHEN** 当前Actor没有Live、Capture、Pose Watch或detail interest
- **THEN** Runtime MUST跳过Operation、Final Pose、Pose Watch和逐骨骼diagnostics复制
- **AND** 正式Pose求值、Final Writer与completion MUST保持不变

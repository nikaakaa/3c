## ADDED Requirements

### Requirement: Motion Matching表现状态必须由唯一深Module拥有

当且仅当当前`CharacterPresentationProjection`包含合法Motion Matching payload时，`CharacterAnimationPlaybackRuntime` MUST唯一构造并拥有一个`CharacterMotionMatchingPresentationModule`。该Module MUST唯一拥有trajectory Adapter、最新Accepted Intent、Selected Body sequence、MM producer runtime、query、selection、Pose History、frozen Pose Source output、frame completion、diagnostics、Replay、Reset与Dispose状态。Factory、`CharacterSimulationPresentationRuntime`与Playback其它字段 MUST不保存这些状态的第二份副本；无MM payload时 MUST不构造Module或分配MM workspace。

#### Scenario: Projection包含MM producer

- **WHEN** Runtime加载包含合法MM producer binding与payload的Projection
- **THEN** Playback MUST构造唯一MM表现Module并把全部producer runtime装入该Module
- **AND** Factory与Simulation Presentation MUST不继续持有trajectory Adapter

#### Scenario: Projection没有MM payload

- **WHEN** 当前Definition没有声明MM producer且Projection省略MM payload
- **THEN** Runtime MUST不构造MM表现Module、trajectory Adapter、query、candidate、plan、history或Replay workspace
- **AND** Timeline producer链 MUST不经过伪MM帧处理

### Requirement: Trajectory Adapter具体类型必须隐藏在Module内部

Accepted Intent与Selected Body MUST作为`CharacterMotionMatchingPresentationModule`内部同一seam上的两个正式Adapter。外部Interface MUST只提交正式Body frame与可选Accepted Intent，并只读取`Enabled`、`AcceptsTrajectoryIntent`和帧结果；Factory、Simulation Presentation与Playback MUST不通过`is`、cast、Network Model名称、Actor名称或场景名称识别具体Adapter。Adapter MUST统一向Module生成`MotionMatchingTrajectorySourceFrame`，但该读帧Interface MUST不暴露给外部调用者。

#### Scenario: Local owner提交Accepted Intent

- **WHEN** 锁定为Committed Body source的MM Module收到Actor、sequence与reset identity匹配的Accepted Intent和当前Body frame
- **THEN** 内部Accepted Intent Adapter MUST生成唯一trajectory frame
- **AND** Simulation Presentation MUST不识别或调用该Adapter具体类型

#### Scenario: Observed actor使用Selected Body

- **WHEN** 锁定为Selected Body source的MM Module收到当前selected Body cursor
- **THEN** 内部Selected Body Adapter MUST使用target pose、velocity、yaw velocity、grounded、selected tick与真实sample age生成trajectory frame
- **AND** Runtime MUST不要求Accepted Intent或最新network packet

#### Scenario: 输入类型与锁定SourceMode不一致

- **WHEN** Selected Body Module收到Accepted Intent，或Committed Body Module缺少MM所需合法Intent
- **THEN** Module MUST返回明确能力拒绝或typed Invalid
- **AND** MUST不切换Adapter、使用默认速度或回退另一种来源

### Requirement: MM表现帧必须是固定Resolve与Complete事务

每个含MM demand的PresentationFrame MUST由同一Module执行一次Resolve和一次Complete。Resolve MUST消费Body/Intent、表现delta、reset identity与固定MM playback demand，执行trajectory、query、search、plan与selection lowering，并输出固定容量`AnimationSelectionFrame`集合和非零completion identity。Pose Graph MUST通过`MotionMatchingSelectionInput`消费该集合。绑定的history source PoseNode完成后，Complete MUST校验frame、reset、selection completion、PoseNodeId与plan completion identity，读取正式Pose Value并追加下一帧可用的Pose History。Runtime MUST拒绝重复Resolve、缺失Complete、重复Complete、跨帧Complete与reset不匹配。

#### Scenario: 正常MM表现帧

- **WHEN** 当前BaseLocomotion MM playback拥有合法Body/Intent输入和candidate
- **THEN** Resolve MUST生成普通`AnimationSelectionFrame`并返回completion identity
- **AND** Complete MUST只在绑定PoseNode完成后追加本帧Pose History

#### Scenario: History source节点本帧无合法Pose

- **WHEN** Pose Graph完成但绑定history source节点结果无效
- **THEN** Complete MUST记录typed history gap并关闭本帧事务
- **AND** MUST不复制上一帧Pose、bind pose或selected candidate冒充完成结果

#### Scenario: 上一帧尚未Complete

- **WHEN** 调用者在上一MM frame completion未关闭时开始下一帧Resolve
- **THEN** Module MUST以明确帧事务错误失败
- **AND** MUST不覆盖旧workspace或继续旧plan

### Requirement: Player continuity与source usage权威不得进入MM Module

Program Finalize MUST继续唯一决定每个`AnimationChannelId`的producer/playback selection。Pose Graph中的`SelectedPosePlayer`或`BlendStack` MUST唯一管理各自source usage；Blend Stack节点 MUST唯一管理entry、CrossFade clock、Stored Pose与source release；Inertialization节点 MUST唯一管理单Pose residual与rebase。MM Module MAY消费Pose Plan完成后发布的source usage identity，并在所有Player正式release后清理不可变selection output。Module MUST不重新仲裁channel winner，不复制Player或Inertialization状态，也 MUST不实现私有transition。

#### Scenario: MM Jump产生新Selection Generation

- **WHEN** 同一MM playback搜索到需要Jump的新sample
- **THEN** Module MUST生成新Selection source identity与普通AnimationSelectionFrame
- **AND** 图上的显式Player MUST按自身语义处理新旧source

#### Scenario: 旧MM source仍被Player使用

- **WHEN** 新source已Selected但旧MM source仍由某个Player节点报告使用
- **THEN** Module MUST保留旧source所需的不可变selection output
- **AND** MUST不重新Search、提升旧generation或复制Player transition clock

#### Scenario: Source usage identity缺少selection output

- **WHEN** Player仍引用一个MM source但Module已经没有对应不可变selection output
- **THEN** Runtime MUST产生typed Invalid并停止该帧提交
- **AND** MUST不改用当前winner、Timeline producer或隐藏Idle

### Requirement: MM Reset与Lifetime必须在Module内原子收敛

Body ResetSequence变化、Committed branch replacement、Selected stream reset、EventId Replace/Retire、Projection replacement、Presentation Reset与Dispose MUST通过唯一MM Module入口原子清理trajectory、Intent、Selected sequence、producer domain、query、plan、selection、Pose History、protected contact、frozen output、frame completion、Replay引用与diagnostics live state。Playback外层 MUST按`MM Module -> request workspace -> Blend Stack/Pose Graph`所需的合法job完成和资源依赖顺序执行Reset与Dispose，但 MUST不逐producer维护第二份reset state。

#### Scenario: Selected stream发生Reset

- **WHEN** Body Runtime提升ResetSequence并提交新的Selected Body anchor
- **THEN** MM Module MUST在下一次query前清理旧trajectory、history、plan、selection与frozen completion
- **AND** 新分支 MUST从Initialization语义开始且不得沿用旧Remote plan

#### Scenario: Projection被替换

- **WHEN** Character Runtime释放旧Projection并加载新的Projection identity
- **THEN** 旧MM Module MUST完整Dispose后才能构造新Module
- **AND** MUST不共享Database Runtime、history、Replay或selection generation

#### Scenario: Dispose发生在Pose job之后

- **WHEN** Actor或Session销毁且本帧Pose jobs已经安排
- **THEN** Runtime MUST先按正式PlayableGraph生命周期完成或关闭相关jobs，再释放MM Module workspace
- **AND** MUST不遗留producer、Database Runtime或frozen output

### Requirement: MM Diagnostics与Replay必须通过同一Module帧合同

MM Module MUST继续向统一`RuntimeDebugSession`发布既有Query、Trajectory、History、Admission、Reject、Search、Top-K、Plan、Selection、Pose Source与Reset payload，并增加Resolve completion identity、request count、Complete identity、history appended/gap与retained frozen output count。Replay capture MUST由Module按ProgramProducerId读取当前正式producer，并要求exact Projection、Profile、Database与Artifact identity；interest关闭时 MUST不构造candidate detail集合，无MM Module时 MUST不发布伪MM snapshot。

#### Scenario: Capture当前MM Search Replay

- **WHEN** RuntimeDebugSession对当前MM producer执行显式Capture
- **THEN** Playback MUST只委托唯一MM Module返回当前正式Search Replay Artifact
- **AND** Debug MUST不重新执行Search或读取第二份producer状态

#### Scenario: Diagnostics未关注candidate detail

- **WHEN** RuntimeDebugSession没有声明Candidate Reject Detail interest
- **THEN** Module与producer runtime MUST不构造或填充candidate detail集合
- **AND** 正式Search、request与frame completion MUST保持不变

### Requirement: Query Fixture Preview必须复用正式MM Module与唯一Pose链

Editor-only Query Fixture Preview MUST显式选择正式Definition、MM producer与exact query输入，构造与Runtime相同的MM Module，并复用正式Runtime Database、Admission、Search、Plan、Animation Selection lowering、编译Pose Graph Plan与Complete合同。Preview MUST不执行Program，也 MUST不创建简化MM runtime、临时PlayableGraph、直接Animancer Play或第二份history实现；world-aware阶段缺少正式上下文时 MUST明确Unavailable。

#### Scenario: Fixture预览一次合法Query

- **WHEN** 作者为exact Projection与Database运行Query Fixture Preview
- **THEN** Preview MUST通过同一MM Module生成普通`AnimationSelectionFrame`并进入正式MotionMatchingSelectionInput
- **AND** 显示结果 MUST来自同一编译Pose Plan的可用完成阶段

#### Scenario: Fixture identity已过期

- **WHEN** Fixture引用的Projection、Database或Artifact identity与当前Definition不一致
- **THEN** Preview MUST在Module Resolve前拒绝输入并显示identity mismatch
- **AND** MUST不迁移旧query、自动重建Artifact或选择其它Definition

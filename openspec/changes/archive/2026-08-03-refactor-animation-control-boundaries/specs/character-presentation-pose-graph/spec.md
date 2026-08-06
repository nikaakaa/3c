# character-presentation-pose-graph Specification

## ADDED Requirements

### Requirement: Pose Graph必须提供纯表现Pose StateMachine

`CharacterPresentationPoseGraphAsset` MUST支持显式`PoseStateMachine`节点及其inline `Entry`、`State`、`Transition`与`State Alias`图。每个State MUST拥有唯一stable identity和一个输出普通Pose Value的inline Pose subgraph。State Alias MUST只复用合法source state集合，MUST不拥有Pose或成为active runtime state。PoseStateMachine MUST编译进Presentation Projection，MUST不进入Gameplay Semantic IR或Numeric Target Program。

#### Scenario: 编译Grounded Locomotion状态机

- **WHEN** 作者配置Idle、Start、Locomotion、Stop与Turn状态
- **THEN** Compiler MUST生成稳定state catalog、entry state、ordered transition与固定workspace
- **AND** MUST不生成BTSMTL StateMachine operation或AnimationChannel winner

#### Scenario: State没有合法Pose输出

- **WHEN** 任一可达State的inline subgraph缺少唯一Pose output
- **THEN** Projection Build MUST失败并定位PoseStateMachine与State identity
- **AND** MUST不使用bind pose或上一个state作为fallback

### Requirement: PoseState inline graph必须使用非递归序列化catalog

State在作者语义上 MUST继续拥有inline Pose subgraph，但`CharacterPresentationPoseGraphAsset` MUST通过root-owned graph catalog保存flat`PoseGraphId -> CharacterPoseGraphData`记录，State与Subgraph call只保存stable GraphId。Unity authoring schema MUST不在State中递归序列化`CharacterPoseGraphData`。Compiler MUST对catalog执行可达性、唯一输出与递归调用检查，Runtime MUST只读取编译后的flat plan。

#### Scenario: State引用inline Pose graph

- **WHEN** 作者打开Locomotion State的inline graph
- **THEN** Editor MUST按stable GraphId导航到root-owned record
- **AND** State serialized data MUST不嵌套另一份Pose Graph对象树

#### Scenario: 两个Pose subgraph递归调用

- **WHEN** Graph A与Graph B通过GraphId形成递归
- **THEN** Validator与Compiler MUST失败并报告完整调用链
- **AND** Runtime MUST不尝试动态展开

### Requirement: Pose Transition Rule必须只读取typed Presentation Fact

Pose Transition Rule MUST由Pose Graph内的pure typed expression组成，只允许读取当前`CharacterPresentationFactFrame`、`TimeInState`和`StatePoseRemainingTime`。Rule MUST不读取BTSMTL Blackboard mutable address、ActionInstance、Timeline operation、Unity Transform或World query。Compiler MUST把Rule降低为固定operation span，Runtime MUST按stable priority和operation order求值。

#### Scenario: Idle进入Locomotion

- **WHEN** Rule读取HorizontalSpeed并满足配置阈值
- **THEN** PoseStateMachine MUST启动对应Transition
- **AND** MUST不要求Gameplay发送PlayRun事件

#### Scenario: 同帧多个Transition成立

- **WHEN** 同一active State存在多个true Rule
- **THEN** Runtime MUST按编译priority和stable order选择唯一target
- **AND** MUST遵守compiled MaxTransitionsPerFrame

### Requirement: Pose State必须支持显式SequencePlayer

Pose Graph MUST提供`SequencePlayer`节点。节点 MUST引用Graph-owned`CharacterSequencePoseSourceSlot`对象并显式声明loop、play rate、initial time、reset-on-entry与clock source；source resource、Rig、marker与Foot Analysis MUST由精确Presentation Profile typed binding解析。`PresentationDelta` MUST是唯一合法clock source并按表现delta推进；SequencePlayer MUST不绑定Gameplay MovementMode，也 MUST不读取Gameplay Timeline或MotionCurve sample。SequencePlayer MUST只采样Pose和发布source discontinuity，MUST不执行Gameplay Timeline、Motion、Window、Cue或Action lifecycle。

#### Scenario: Idle State循环播放

- **WHEN** Idle State使用loop SequencePlayer
- **THEN** Player MUST按Presentation delta连续采样绑定clip
- **AND** MUST不要求Idle TimelineNode或Gameplay producer存活

#### Scenario: Stop State重新进入

- **WHEN** Stop State的SequencePlayer配置reset-on-entry
- **THEN** 每次合法进入Stop MUST从编译起始时间重新初始化
- **AND** 离开State后的播放器保留策略 MUST只由compiled relevance配置决定

#### Scenario: MovingTurn使用Presentation时钟

- **WHEN** PoseStateMachine进入MovingTurn并激活Turn Sequence与RootOrientationWarp
- **THEN** SequencePlayer MUST按PresentationDelta推进自己的有限sample
- **AND** RootOrientationWarp MUST按同一Sequence sample读取作者Yaw曲线且不得读取Gameplay MotionCurve相位

### Requirement: Pose State transition必须复用唯一Transition Routing模块

每条Pose Transition edge MUST显式选择Standard Blend或Inertialization，并保存duration、curve、Blend Profile与target reset policy。Runtime MUST把已解析source state、target state、readiness和generation提交给唯一Transition Routing模块。Standard Blend MUST由PoseStateMachine transition runtime执行；Inertialization MUST通过typed route request交给branch-local consumer。MUST不在State、SequencePlayer或Output Pose中复制exact route算法。

#### Scenario: Locomotion到Stop使用Standard Blend

- **WHEN** exact transition选择Standard Blend
- **THEN** StateMachine runtime MUST在duration内同时求值source与target State Pose
- **AND** Routing模块 MUST不发布Inertialization request

#### Scenario: Turn被Hit覆盖后恢复

- **WHEN** branch-local transition选择Inertialization且target首Pose已就绪
- **THEN** producer MUST发布typed request并等待capture/release握手
- **AND** source state resource MUST不在capture permission前释放

### Requirement: PoseState target必须经过source readiness barrier

PoseStateMachine MUST先选择候选target并发布对应provider demand。Provider MUST返回`Pending`、`Ready`或`Invalid`。只有Ready target才可提交Transition Routing generation；已有合法source时Pending target MUST保持当前source且不得启动transition，Invalid MUST报告typed failure并阻止该帧publication。Entry required source为Pending时 MUST不发布Final Pose。系统 MUST不使用历史Selection、bind pose、默认Idle或旧Timeline作为fallback。

#### Scenario: Motion Matching target尚未完成首个query

- **WHEN** Transition Rule选择MM State但provider返回Pending
- **THEN** PoseStateMachine MUST保持当前合法State输出
- **AND** MUST不提前提交target transition generation

#### Scenario: Entry State source无效

- **WHEN** Entry State的source provider返回Invalid
- **THEN** Presentation frame MUST失败并报告对应State与provider
- **AND** MUST不发布bind pose或旧Selection

### Requirement: Transition Routing plan必须只在Projection编译

Projection Compiler MUST为每个PoseStateMachine与AnimationSlot生成完整exact Routing Plan、endpoint matrix、capture/release request layout、PlanId与Revision。角色Runtime与Preview MUST只装载并校验该计划，不得调用`TransitionRoutingCompiler`或按当前endpoint重新编译。

#### Scenario: Runtime装载PoseStateMachine

- **WHEN** Projection中的Routing PlanId与Pose Plan revision匹配
- **THEN** Runtime MUST直接建立固定workspace
- **AND** MUST不重新编译transition matrix

#### Scenario: Runtime发现Routing revision不匹配

- **WHEN** Routing Plan revision与Pose Plan不一致
- **THEN** preparation MUST失败
- **AND** MUST不现场编译或使用旧plan

### Requirement: Pose State source同步必须由Transition显式声明

PoseState Transition edge MUST显式选择`None`或`MarkerGroup` State Source Sync。选择MarkerGroup时Compiler MUST从source/target State的Sequence或BlendSpace source binding解析canonical SyncGroup、topology、role与marker，生成绑定该Transition identity的Source Sync Plan。Runtime MUST在State Pose采样前持续求值effective time。缺少plan时 MUST使用raw source time，MUST不自动扫描同名State或复用Action MarkerSync relation。

#### Scenario: Walk到Run启用MarkerGroup

- **WHEN** Transition edge选择MarkerGroup且两侧source共享Locomotion.Gait
- **THEN** Compiler MUST生成source/target player的稳定sync relation plan
- **AND** Runtime MUST在共同可见期间持续对齐marker fraction

#### Scenario: Idle到Start选择None

- **WHEN** Transition edge显式选择None
- **THEN** 两侧Player MUST使用各自raw time
- **AND** Runtime MUST不按source binding同组信息自动同步

### Requirement: Pose Graph必须提供显式Animation Slot

Pose Graph MUST提供显式`AnimationSlot`节点，拥有Source Pose输入、exact Action Playback输入、稳定Slot/AnimationChannel identity和node-local Blend Policy。没有Action playback时Slot MUST透传当前Source Pose；Action活跃时 MUST采样Action Pose并按compiled Blend Logic插入；Action release时 MUST过渡回同帧当前Source Pose。Slot MUST不判断Action admission、不推进Timeline、不提交Motion或Bone Mask。

#### Scenario: FullBodyAction为空

- **WHEN** FullBodyAction没有活动playback
- **THEN** Slot输出 MUST与当前Locomotion Source Pose一致
- **AND** MUST不创建默认Idle或Stored Pose常驻fallback

#### Scenario: FullBodyAction播放攻击

- **WHEN** Attack playback首份合法sample到达
- **THEN** Slot MUST从当前Source Pose过渡到Attack Pose
- **AND** Locomotion PoseStateMachine MUST继续更新Source Pose

#### Scenario: UpperBody Slot组合

- **WHEN** 作者把UpperBody Slot输出接入Layered Blend Per Bone
- **THEN** Slot MUST只输出普通Pose
- **AND** 骨骼覆盖范围 MUST只由Layered Blend Per Bone拥有

### Requirement: Animation Slot必须区分SourcePoseEndpoint与NoPose

AnimationSlot的无Action占用 MUST编译为`SourcePoseEndpoint`，表示输出当前持续更新的Source Pose。`NoPose` MUST只表示Required上游姿势不可用。Slot exact matrix MUST物化SourcePose到Action、Action到Action与Action到SourcePose规则；MUST不再物化含义不明确的Empty endpoint。

#### Scenario: Action淡出回Locomotion

- **WHEN** FullBodyAction完成Action到基础姿势transition
- **THEN** Slot route MUST结束在`SourcePoseEndpoint`
- **AND** 后续帧 MUST继续输出当前PoseState Source Pose

## MODIFIED Requirements

### Requirement: Pose Graph工作区必须显式解释完整表现拓扑

Pose Graph MUST唯一声明Presentation Fact Input、PoseStateMachine、SequencePlayer、ActionPlaybackInput、Motion Matching provider、SelectedPosePlayer、BlendSpacePlayer、BlendStack、AnimationSlot、Inertialization、Blend、Layered Blend Per Bone、Additive、Pose Parameter、ModifyBone、TwoBoneIK、FootPlacement与Output topology。Marker时间映射 MUST只属于PoseState Transition或AnimationSlot的source-local sampling plan，不得序列化独立MarkerSync节点。Compiler与Runtime MUST不在AnimationChannel、State、Slot或Output背后自动追加未显示的跨职责节点。复合节点内部的compiled operation、workspace、source usage与route MUST在工作区和Projection中可诊断。

#### Scenario: Locomotion与Action组合

- **WHEN** 作者把Locomotion PoseStateMachine连接到FullBodyAction Slot再连接Output
- **THEN** Projection MUST保存相同拓扑和全部compiled state/slot operation
- **AND** Runtime MUST不创建第二条BaseLocomotion Selection路径

### Requirement: Pose Graph Details必须分离Authoring、Live与References

Pose Graph Details MUST继续提供`Authoring`、`Live`和`References`三个互斥页。PoseState、Transition Rule、SequencePlayer和AnimationSlot的Authoring页 MUST只编辑各自正式拥有字段；Live页 MUST只读取匹配revision的PoseState、source usage、transition、Slot和route snapshot；References页 MUST把Pose source导航到Profile source binding，把Action playback导航到Timeline Track。任一页面 MUST不复制marker、curve、window、Motion或Action admission字段，也不得重新求值Pose Plan。

#### Scenario: 选择Run SequencePlayer

- **WHEN** 作者选择Run State中的SequencePlayer
- **THEN** References MUST显示Source Slot、Profile Binding和实际资源，并提供Open Source与Open Profile Owner
- **AND** Details MUST不提供Open Run Timeline或编辑Gameplay Motion

#### Scenario: 选择FullBodyAction Slot

- **WHEN** 作者选择FullBodyAction AnimationSlot
- **THEN** References MUST显示绑定AnimationChannel与可达Action producer
- **AND** Live MUST显示正式Slot source usage和transition route

### Requirement: Pose Graph画布必须提供source-mapped Live可视化

在匹配正式snapshot时，Graph Canvas MAY按PoseNodeId、PoseStateId和call-site显示active/target State、Transition progress、source readiness、AnimationSlot playback、source usage、weight、Sync Group和Output completion。连线权重与节点状态 MUST来自正式Pose operation trace，不得由Editor重新混合、重采样或按拓扑猜测。Authoring与Live Debug MUST保持窗口级边界，Live Debug下mutation命令 MUST只读。

#### Scenario: Action覆盖Locomotion

- **WHEN** FullBodyAction Slot以1权重输出Attack且Locomotion PoseState继续求值
- **THEN** Graph Canvas MUST显示Slot的Source Pose、Action Pose和正式输出贡献
- **AND** MUST不把Gameplay StateMachine显示为Animation State Machine

### Requirement: Pose Preview必须显式执行正式Pose Plan

Pose Graph Bottom Dock MAY提供Authoring Preview，但作者 MUST显式选择精确Definition和合法Preview Target。Preview MUST使用正式Presentation Fact、PoseStateMachine、source binding、Action Playback fixture、AnimationSlot、Transition Routing与完整Pose Plan；缺少、Invalid或Stale Projection时 MUST停止。Graph mutation、窗口恢复、资产事件或target变化 MUST不自动Build、创建临时Plan或恢复旧BaseLocomotion Timeline Preview。

#### Scenario: Preview改变Locomotion Fact

- **WHEN** 作者显式修改Preview的Grounded、Speed或Direction Fact
- **THEN** PoseStateMachine MUST通过正式Rule产生状态与transition结果
- **AND** Preview MUST不执行Gameplay StateMachine或发送动画事件

#### Scenario: Graph修改后继续Preview

- **WHEN** 作者修改PoseState或Slot使Projection变为Stale
- **THEN** Preview MUST停止消费旧Plan并显示Stale
- **AND** MUST等待显式Build

### Requirement: Pose Graph工作区必须准确使用UE对应术语

UI MAY对正式`PoseStateMachine`使用`Animation State Machine`，对正式`AnimationSlot`使用`Slot`，并继续使用`Anim Graph`、`Sequence Player`、`Transition Rule`、`State Alias`、`Layered Blend Per Bone`、`Inertialization`、`Sync Group`、`Pose Watch`与`Output Pose`。UI MUST保留项目serialized kind与stable identity，MUST明确区分BTSMTL Gameplay StateMachine和Pose Animation StateMachine。AnimationChannel仍是Gameplay action arbitration identity，不得直接改名为Slot；BTSMTL Action Timeline与UE Montage职责相近但不是Montage资产，UI MUST不伪装其类型。

#### Scenario: 显示Locomotion状态机

- **WHEN** Navigator显示Corin Locomotion PoseStateMachine
- **THEN** UI MAY显示Animation State Machine术语
- **AND** MUST标识它属于Pose Graph而不是Gameplay Program

#### Scenario: 显示FullBodyAction

- **WHEN** Navigator显示FullBodyAction AnimationSlot
- **THEN** UI MUST同时显示Slot identity和绑定的Action AnimationChannel
- **AND** MUST不把channel本身序列化为Slot

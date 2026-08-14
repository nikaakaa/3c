## MODIFIED Requirements

### Requirement: Presentation Profile必须唯一绑定Pose source

`CharacterPresentationPoseGraphAsset` MUST以类型明确的Source Slot子资产声明持续Pose source语义插槽。`CharacterAnimationPresentationProfile` MUST以Profile-owned typed binding把每个可达Sequence Source Slot唯一绑定到`CharacterAnimationSequenceAsset`，把Blend Space Slot绑定到Blend Space资产，把Motion Matching Slot绑定到对应Profile/Database。Sequence Binding MUST不复制AnimationClip、Rig、Loop、PlayRate、Marker、Time Mapping、Curve、Notify或Analysis输入；这些素材字段 MUST由被引用Sequence唯一拥有。Pose Graph Player MUST只引用Source Slot对象，不得保存Sequence、Clip或Binding副本。

#### Scenario: 作者替换Run Sequence

- **WHEN** 作者在Corin Presentation Profile为Run Source Slot选择另一份Sequence
- **THEN** mutation MUST只修改Profile Binding的精确Sequence引用
- **AND** PoseStateMachine topology与任一Sequence素材内容 MUST保持不变

#### Scenario: 两个Profile复用同一Sequence

- **WHEN** 两个Profile的不同Source Slot明确引用同一个Run Sequence
- **THEN** 两个Profile MUST共享Sequence Marker、Curve、Notify与Analysis owner
- **AND** 各Binding MUST只保存自身Slot到Sequence关系

#### Scenario: 作者替换Run动画

- **WHEN** 作者把Run Binding改为引用另一份正式Sequence
- **THEN** PoseStateMachine topology与Gameplay Program MUST不需要修改
- **AND** Projection MUST只在作者显式Build后重建

#### Scenario: 两个Profile复用同一Pose Graph

- **WHEN** 两个Profile引用同一Pose Graph与Run Source Slot
- **THEN** 两个Profile MUST分别保存自己的Slot到Sequence Binding
- **AND** Pose Graph MUST不复制或改写任一Sequence

## ADDED Requirements

### Requirement: 持续Sequence Pose Source必须从主时间编辑器打开Sequence

Profile、Pose Graph References与Source Binding Inspector MUST提供`Open Sequence`导航，使用精确Sequence对象打开主Timeline Editor的Sequence文档。Marker、Curve、Notify、Analysis和Preview MUST只在该文档中编辑；Profile Inspector MUST不提供普通CurveField、Marker文本、内嵌time ruler或独立`Pose Source Editor`。

#### Scenario: 从Run Binding编辑脚Marker

- **WHEN** 作者点击Run Binding的Open Sequence
- **THEN** 主Timeline Editor MUST打开Binding明确引用的Run Sequence
- **AND** mutation MUST写入Sequence而不是Profile Binding

## MODIFIED Requirements

### Requirement: CharacterAnimationPresentationProfile Inspector必须是唯一Presentation配置入口

Profile Inspector MUST唯一编辑Pose Graph、Source Slot到Sequence/Blend Space/Motion Matching的typed Binding、Blend/Inertialization Policy、角色Rig、有限Action producer binding与其它Profile级配置。Profile Inspector MUST不编辑Sequence内部Clip、Marker、Curve、Notify或Analysis Source；这些内容 MUST通过Open Sequence进入主Timeline Editor。Action Timeline Editor继续唯一编辑Action编排内容，但不得编辑Sequence素材数据。

#### Scenario: 查看shared Timeline与Sequence

- **WHEN** Profile同时引用shared Action Timeline和Run Sequence
- **THEN** Inspector MUST分别显示Open Timeline与Open Sequence导航及唯一owner
- **AND** MUST不在Profile中复制任一资源的内部时间数据

#### Scenario: 从Profile打开Timeline Analysis

- **WHEN** 作者从精确Profile context打开Action Timeline并选择Sequence Segment
- **THEN** Workspace MUST提供Open Sequence进入精确Sequence Analysis
- **AND** Timeline资产 MUST不因打开或分析变脏

#### Scenario: shared Timeline用于不同角色

- **WHEN** 两个Profile使用同一shared Timeline但Segment引用同一或不同Sequence
- **THEN** 各Profile MUST按自身正式Projection与consumer binding解析
- **AND** shared Timeline MUST不保存任一角色Analysis Source

### Requirement: Animation Marker Sync 必须由实际source owner唯一拥有

所有基于单段AnimationClip的Marker Sync作者数据 MUST由对应`CharacterAnimationSequenceAsset`唯一拥有，包括None/MarkerGroup、Time Mapping、SyncGroupId、Finite/Cyclic topology、SyncRole与ordered Point Marker。Profile Binding、Blend Space sample、Action Timeline Track/Segment、Pose transition、Rule、Blackboard与ActionProfile MUST不复制Marker。PoseState Compiler、Blend Space Compiler与Action producer Compiler MUST从各自精确Sequence引用解析Marker计划。

#### Scenario: 编辑Run marker

- **WHEN** 作者修改Run Sequence的Locomotion.Gait marker
- **THEN** PoseState与Blend Space消费者 MUST从同一Sequence读取新Marker
- **AND** Profile Binding与Blend Space sample MUST不保存副本

#### Scenario: 编辑Attack marker

- **WHEN** 作者修改Attack Sequence的finite marker
- **THEN** 引用它的Action Timeline Segment MUST保持编排范围不变并读取新Marker
- **AND** Timeline AnimationTrack MUST不成为Marker owner

#### Scenario: source明确不参与同步

- **WHEN** 作者把Sequence配置为None
- **THEN** Sequence authoring API MUST原子清空Time Mapping、Group、Topology、Role与Marker
- **AND** 全部消费者 MUST使用该Sequence raw time且不得保留override

### Requirement: Animation Clip控制曲线必须作为typed Curve Channel编辑

Sequence-local Foot Placement Weight及其它registered素材Curve MUST由Animation Sequence唯一拥有。Action Timeline Sequence Segment MUST只拥有segment-local Weight与Ease等编排Curve；State transition blend curve MUST继续由Transition Policy拥有。Sequence、Segment与Transition MUST不双写同一curve，generated每脚feature MUST不成为可编辑Curve Channel。

#### Scenario: 编辑Attack Foot Placement Weight

- **WHEN** 作者打开Attack Sequence修改Foot Placement Weight
- **THEN** Sequence Curve Editor MUST编辑Sequence typed channel
- **AND** Action Timeline Segment MUST只保留自己的Weight/Ease编排Curve

#### Scenario: 编辑Run Foot Placement Weight

- **WHEN** 作者从Run Binding打开Sequence并修改Foot Placement Weight
- **THEN** mutation MUST只更新Run Sequence完整curve
- **AND** Profile Binding MUST不保存source-local curve副本

#### Scenario: AnimationClip内容变化

- **WHEN** Sequence引用的AnimationClip imported content改变但作者Curve未改变
- **THEN** Projection Foot Analysis MUST变为Stale
- **AND** Sequence、Timeline与Profile MUST不被自动写入generated key

### Requirement: Marker Group 必须支持 Finite 与 Cyclic 序列

每个Animation Sequence MarkerGroup MUST声明Finite或Cyclic。Cyclic Sequence只能由允许循环的Pose Player、Blend Space Dynamic sample或Loop Action Timeline call site消费；Finite Sequence必须覆盖完整素材duration且不得回绕。Action Timeline Segment、Profile Binding与Blend Space sample MUST不覆盖Sequence topology。调用方式冲突、marker边界非法或素材coverage不完整 MUST编译失败。

#### Scenario: Cyclic Run被Once Action Segment引用

- **WHEN** Cyclic Run Sequence被一个只允许Once的有限Action call site引用
- **THEN** Compiler MUST报告Sequence与call site topology冲突
- **AND** MUST不在Segment上保存override修复

#### Scenario: Finite TurnBack覆盖完整素材

- **WHEN** TurnBack Sequence声明Finite并具有首尾完整Marker coverage
- **THEN** PoseState与Action消费者 MAY按各自业务引用该Sequence
- **AND** Runtime MUST不把它按Cyclic回绕

#### Scenario: shared Timeline全部以Loop调用

- **WHEN** 多个Action Segment call site引用同一Cyclic Sequence且全部为Loop
- **THEN** Compiler MUST接受共享Sequence topology
- **AND** 每次activation MUST继续使用独立playback generation

#### Scenario: shared Timeline混合Once与Loop

- **WHEN** 同一Cyclic Sequence被Once与Loop Action call site混合引用
- **THEN** Compiler MUST报告全部冲突call site
- **AND** MUST不为调用点生成隐式topology override

### Requirement: Marker Group 必须显式声明 handoff 同步角色

每个Animation Sequence MarkerGroup MUST显式声明`CanBeLeader`、`AlwaysLeader`或`AlwaysFollower`。PoseState Source Sync Plan、Blend Space phase plan与Action Slot relation MUST从Sequence读取同一角色解析规则；角色不得在Transition、Segment、Binding或sample中覆盖。角色冲突 MUST失败，MUST不按State、Action、Clip或Sequence名称猜测leader。

#### Scenario: RunEnd保持自身节奏

- **WHEN** RunEnd Sequence配置为Finite/AlwaysLeader并从RunLoop进入
- **THEN** compiled relation MUST让RunEnd在共同可见期领导同步
- **AND** Pose Transition与Timeline Segment MUST不复制角色字段

#### Scenario: 有限停步保持自身节奏

- **WHEN** RunEnd Sequence为MarkerGroup/Finite/AlwaysLeader并从RunLoop进入
- **THEN** Projection MUST保留AlwaysLeader角色
- **AND** Runtime MUST让RunEnd领导共同可见期

#### Scenario: 冲突角色不允许猜测

- **WHEN** relation两侧都要求AlwaysLeader或都要求AlwaysFollower
- **THEN** Build或Runtime MUST返回typed invalid reason
- **AND** MUST不按generation、名称或weight猜测方向

### Requirement: Marker Group 必须在 Projection 构建前完整校验

Projection Build MUST按Animation Sequence唯一校验duration、Marker identity、frame/time、有向pair、topology、role、Time Mapping、Clip/Rig/Analysis依赖和全部消费者coverage。Compiler MUST从Profile Binding、Blend Space sample与Action Segment收集精确Sequence引用并建立可达relation；任一缺失、跨Sequence冲突、调用拓扑冲突或generated plan无效 MUST阻止发布。系统 MUST不从旧Track/Binding/sample Marker或名称补全。

#### Scenario: Walk与Run共享同步组

- **WHEN** Walk与Run Sequence拥有兼容Group、Time Mapping、有向pair和合法artifact
- **THEN** Projection MUST为实际可达PoseState relation编译精确同步计划
- **AND** Profile Binding MUST不提供第二份校验输入

#### Scenario: Action Segment引用无效Sequence

- **WHEN** Action Timeline Segment引用的Sequence缺少完整Finite coverage
- **THEN** Projection Build MUST定位Sequence与Segment并失败
- **AND** MUST不使用Timeline Track旧Marker或raw normalized time作为fallback

#### Scenario: Walk与Run使用不同时序

- **WHEN** Walk与Run Sequence属于同组、拥有相同有向pair但marker frame不同
- **THEN** Compiler MUST接受各自真实occurrence并生成relation plan
- **AND** Projection MUST保存两份Sequence时间与精确映射identity

#### Scenario: 有限序列重复MarkerId

- **WHEN** Finite Sequence使用`LeftPlant -> RightPlant -> LeftPlant`覆盖完整one-shot
- **THEN** Validator MUST接受重复语义MarkerId
- **AND** 每个occurrence MUST拥有不同stable identity与frame

#### Scenario: 同组缺少有向segment

- **WHEN** 同组target Sequence缺少其它Sequence可能产生的有向pair
- **THEN** Compiler MUST报告精确Sequence group compatibility错误
- **AND** MUST不回退normalized time

#### Scenario: marker覆盖区存在无输出空洞

- **WHEN** Sequence Marker映射落入没有合法素材sample的区间
- **THEN** Validator MUST报告output coverage错误
- **AND** MUST不依赖Timeline Hold或默认Idle填补

### Requirement: Presentation Projection 必须保存规范化 Marker Sync 映射

Projection Compiler MUST把Animation Sequence的同步模式、canonical SyncGroupId、topology、role、Time Mapping、duration、ordered Marker与有向pair occurrence降低为不可变Sequence plan。Pose source、Blend Space sample与Action Segment MUST通过Projection-local Sequence binding引用该plan；relation-local generated warp仍按实际consumer pair编译。全部映射 MUST只服务表现采样，不进入Gameplay Program ABI、State codec、Snapshot或Network协议。

#### Scenario: 同一Sequence被多个消费者引用

- **WHEN** Run Sequence同时被Pose Source与Blend Space sample引用
- **THEN** Projection MAY复用同一Sequence marker plan并建立不同consumer binding
- **AND** 两个consumer MUST不携带Marker副本

#### Scenario: Marker Sync改变producer表现时间

- **WHEN** relation把target Sequence raw time映射为不同effective time
- **THEN** target Pose、Foot Analysis与Sequence素材Curve MUST按effective time求值
- **AND** Gameplay Timeline logic time MUST保持不变

#### Scenario: AnimationClip或Calibration变化

- **WHEN** Sequence Clip、artifact或Calibration revision改变
- **THEN** ProjectionRevision MUST更新且旧Projection MUST被拒绝
- **AND** Gameplay Program operation语义 MUST保持不变

## REMOVED Requirements

### Requirement: 持续Sequence Pose Source必须拥有完整时间编辑表面

**Reason**: 独立Pose Source Editor由主Timeline Editor的Sequence文档取代；时间编辑能力保留并提升为Sequence正式owner。

**Migration**: Profile Sequence Binding的Clip、Marker、Curve与Analysis输入迁入Sequence，Binding只保留精确Sequence引用。

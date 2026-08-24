## MODIFIED Requirements

### Requirement: State-local source必须由Profile binding和provider解析

`ClipPlayer`、`BlendSpacePlayer`与`SelectedPosePlayer` MUST引用类型匹配的Graph-owned Source Slot对象。Projection Compiler MUST从精确Definition/Profile解析唯一Binding；Clip Binding MUST直接提供AnimationClip。ClipPlayer MUST只保存Source Slot、Play Rate、Initial Time与Clock Source，不得保存Loop或Topology副本；Finite/Cyclic MUST只从AnimationClip正式Loop设置编译。Provider MUST发布带dense source index、generation、Projection revision与frame lease的`PresentationPoseSourceSample`。Pose Graph MUST不保存Sequence、AnimationClip副本、作者source字符串或Gameplay producer。

#### Scenario: ClipPlayer首次采样Idle

- **WHEN** Idle State的ClipPlayer获得entry relevance
- **THEN** provider MUST从Profile direct Clip Binding发布Ready sample
- **AND** Player MUST不解析Sequence或AssetDatabase

#### Scenario: ClipPlayer提交Loop字段

- **WHEN** 人工Capability或Document v4为ClipPlayer提供Loop、Topology或等价override
- **THEN** typed parser或Validator MUST在Compiler前拒绝该字段
- **AND** MUST不覆盖AnimationClip正式Loop设置

### Requirement: Pose State transition必须显式编译Routing并从source binding推导同步

每条Transition MUST继续显式配置Rule、Blend Logic、duration、Blend Mode、Custom Curve与Blend Profile。Compiler MUST从两侧State唯一source usage与Profile Locomotion Sync Group推导可选source-to-source Phase relation；Direct Clip与Blend Space MUST先降低为正式`AnimationSourcePhasePlan`。两侧不属于同组时生成None，同组时必须编译合法per-clip Phase、实际秒域coverage与Foot Analysis质量结果，并按clock authority与完整Blend窗口coverage写入固定leader。Transition authoring MUST不保存同步开关、Marker策略、SyncRole、leader override或phase容差；Projection relation MUST保存TransitionId，Runtime再与TransitionGeneration组合生命周期身份。

#### Scenario: Turn进入RunLoop

- **WHEN** Turn与RunLoop属于同一Locomotion Sync Group且Transition条件成立
- **THEN** target effective time MUST来自compiled Phase relation
- **AND** Blend Routing MUST独立使用edge-owned Standard Blend计划

### Requirement: Pose Graph UI必须保留准确术语和serialized identity

UI MUST使用Clip Player、Blend Space Player、Selected Pose Player、Animation State Machine、Slot、Layered Blend Per Bone、Inertialization、Locomotion Phase Group、Pose Watch和Output Pose等准确术语。序列化、Document、Mutation、Compiler source map和Diagnostics MUST使用同一Clip命名；MUST不保留Sequence Player显示名或旧node kind alias。

#### Scenario: 作者添加单Clip播放器

- **WHEN** 作者在Pose Graph添加单AnimationClip state-local player
- **THEN** Capability、节点标题、Document kind和编译诊断 MUST统一显示Clip Player
- **AND** MUST不存在Sequence Player兼容名称

### Requirement: Pose authoring必须使用共享Capability与类型化Presentation Mutation

Pose Graph、PoseStateMachine、Node、Port与Edge MUST使用共享typed domain document。

#### Scenario: 新增Pose节点能力

- **WHEN** 新Pose节点注册typed payload与compiler handler
- **THEN** 人工创建菜单、Document v4、Validator和Compiler MUST识别同一Capability
- **AND** MUST不复制Node/Port View或第二Compiler入口

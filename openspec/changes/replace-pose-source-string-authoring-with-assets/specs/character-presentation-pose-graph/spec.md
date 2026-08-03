## MODIFIED Requirements

### Requirement: State-local source必须由Profile binding和provider解析

`SequencePlayer`、`BlendSpacePlayer`与Motion Matching `SelectedPosePlayer` MUST引用类型匹配的Graph-owned`CharacterPresentationPoseSourceSlot`对象。Projection Compiler MUST从精确Definition/Profile上下文为每个可达Slot解析唯一Profile-owned typed binding子资产，并将其降低为Projection-local dense source index、typed resource plan与只读source map。Provider MUST发布`PresentationPoseSourceSample`的Pending、Ready或Invalid；Player只消费匹配自身Player identity、dense source index、generation、Projection revision与frame lease的sample。Pose Graph MUST不保存作者可编辑Source Id、Provider Id、AnimationClip、Profile binding副本，也不得把state-local source包装成Gameplay producer、AnimationChannel或PlaybackId。

#### Scenario: Idle SequencePlayer首次采样

- **WHEN** Idle State进入relevant且Source Slot对应的Profile binding合法
- **THEN** Sequence provider MUST向Idle Player发布带正确dense source index的Ready sample
- **AND** CharacterActionPlaybackRuntime MUST不登记该source

#### Scenario: Motion Matching sample投递到错误Player

- **WHEN** sample的Player identity、dense source index或Projection revision与当前demand不匹配
- **THEN** Runtime MUST拒绝该sample
- **AND** MUST不按Source Slot名称、资源名或旧Source Id猜测目标Player

### Requirement: Pose Graph工作区必须准确映射Authoring、Live与References

正式窗口 MUST提供Definition-scoped Navigator、唯一`GraphAuthoringCanvasView`、Details和可折叠Bottom Dock。Details MUST分离Authoring、Live与References：Authoring只通过正式Presentation Mutation修改当前owner字段；Live只读取匹配PoseGraphId、PoseGraphRevision与ProjectionRevision的snapshot；References只读显示Source Slot、Profile binding子资产、实际资源对象、source map、Action producer、Rig、Policy和call site。稳定identity、GUID、revision、hash与compiled index MUST默认隐藏。Live Debug模式下mutation MUST禁用，revision不匹配 MUST显示Stale并清空旧值。

#### Scenario: 查看Locomotion State

- **WHEN** 作者选中Locomotion State的Sequence或BlendSpace Player
- **THEN** Authoring MUST显示类型匹配的Source Slot对象选择器
- **AND** References MUST显示解析后的Profile binding、实际资源、owner与Open Source命令
- **AND** MUST不显示BaseLocomotion Gameplay producer或可编辑Source Id

#### Scenario: Runtime revision不匹配

- **WHEN** snapshot revision与当前文档或Projection不一致
- **THEN** Live MUST显示Stale
- **AND** MUST不从authoring默认值或Animancer state伪造结果

### Requirement: Pose Graph UI必须保留准确术语和serialized identity

UI MAY把正式`PoseStateMachine`显示为Animation State Machine、把`AnimationSlot`显示为Slot，并使用Anim Graph、Sequence Player、Transition Rule、State Alias、Layered Blend Per Bone、Inertialization、Sync Group、Pose Watch和Output Pose。系统 MUST在序列化、Undo、clipboard、Document、compiler source map与Diagnostics中保留项目serialized node kind和stable identity，但人工UI MUST默认使用业务显示名和Unity资源对象，不得把identity、GUID、hash或compiled index作为节点标题、Navigator项目、breadcrumb或可编辑字段。AnimationChannel仍是有限Action arbitration identity，BTSMTL Action Timeline职责近似Montage但不得伪装成Montage资产。

#### Scenario: 显示FullBodyAction

- **WHEN** Navigator选中FullBodyAction Slot
- **THEN** UI MUST显示Slot业务名与绑定Action AnimationChannel的业务名
- **AND** 原始SlotId与AnimationChannelId MUST只在显式Diagnostics中只读出现
- **AND** MUST不把AnimationChannel本身序列化为Slot

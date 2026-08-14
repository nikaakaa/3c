## MODIFIED Requirements

### Requirement: Agent必须保持Generated Foot Analysis只读

Agent Character Document package MUST把Animation Sequence、Graph、StateMachine、Timeline、Marker、Notify、registered editable Curve Channel和Profile binding放入各自正式editable分片。Sequence的素材Curve与Marker MUST只进入Sequence文件对；Timeline只表达Segment编排Curve，Profile与Blend Space只表达Sequence引用。Projection生成的foot feature与warp payload MUST保持只读context或完全省略。Agent MUST不创建Foot Analysis mutation或把generated channel写入Sequence。

#### Scenario: Agent尝试把generated channel写入Sequence

- **WHEN** Sequence curves分片提交未登记的Plant Confidence或Landing ChannelId
- **THEN** Reconciler MUST按未知Curve Channel拒绝整个package
- **AND** MUST不修改Sequence、Timeline、Profile或Projection

#### Scenario: Agent尝试写Generated channel

- **WHEN** package editable分片提交未登记的LeftPlant、RightPlant或Landing ChannelId
- **THEN** Reconciler MUST按未知Curve Channel拒绝整个package
- **AND** MUST不把generated payload改写为Sequence素材Curve

### Requirement: Agent 必须阻止 Marker Sync 数据分裂

Agent Document MUST只把raw AnimationClip引用、Rig、Loop/Finite、默认倍率、Marker Sync、Time Mapping、素材Curve、Notify与Analysis Source放在Animation Sequence entity。Timeline Segment、Profile Binding与Blend Space sample MUST只保存稳定Sequence对象引用和各自领域字段；Marker、Curve或Notify副本 MUST被strict codec或Validator拒绝。Target MUST使用stable identity或Document `local:*` identity，不得按AnimationClip、名称、目录、breadcrumb或index猜测Sequence。

#### Scenario: Timeline提交素材Marker副本

- **WHEN** Document在Sequence Segment或AnimationTrack中提交Marker Group或Point Marker
- **THEN** dry-run MUST拒绝整个Document并定位旧字段
- **AND** apply MUST不产生部分Sequence或Timeline修改

#### Scenario: Profile Binding提交素材Curve副本

- **WHEN** Document在Sequence Binding中提交Foot Placement Weight curve
- **THEN** dry-run MUST要求把curve放入被引用Sequence的curves分片
- **AND** MUST不按字段优先级选择一份真相

#### Scenario: None Sequence保留Marker

- **WHEN** Document把Sequence SyncMode设为None但仍保留group、Time Mapping或Marker
- **THEN** dry-run MUST拒绝整个Document
- **AND** apply MUST不产生部分资产修改

## ADDED Requirements

### Requirement: Agent Document必须完整读写Animation Sequence与Timeline Segment

Character Document package MUST在Sequence文件对按稳定identity表达精确AnimationClip/Rig引用、Loop/Finite、默认倍率、Marker Sync、Time Mapping、Notify、Analysis Source与registered素材Curve；Timeline文件对 MUST按Segment identity表达Sequence引用、Start/End、ClipIn、Extrapolation、Weight/Ease及Section。Reconciler MUST为Sequence、Marker、Notify、完整Curve、Segment与Section变化生成typed Mutation；handler MUST只调用Sequence或Timeline正式authoring API。

#### Scenario: 新建Sequence并在Timeline引用

- **WHEN** Document使用`local:*`创建完整Sequence文件对并新增引用它的Timeline Segment
- **THEN** Reconciler MUST为Sequence与Segment建立同一plan的planning symbol
- **AND** apply后的reverse export MUST用正式Sequence identity和对象引用替换全部local引用

#### Scenario: 修改Sequence Marker

- **WHEN** Document移动现有Sequence Marker occurrence
- **THEN** apply MUST保持Marker与Sequence stable identity并只修改Sequence owner
- **AND** Timeline Segment、Profile Binding与Blend Space sample正文 MUST保持不变

## REMOVED Requirements

### Requirement: Agent Document必须完整读写 Timeline Marker 与 Curve Channel

**Reason**: 素材Marker与Curve迁入Animation Sequence；Timeline只保留Segment-local编排Curve、Section及非素材业务Track。

**Migration**: Timeline旧Marker/素材Curve按完整内容签名迁入Sequence文件对；Timeline Segment改为稳定Sequence引用。

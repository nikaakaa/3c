## MODIFIED Requirements

### Requirement: Agent 不得形成第二个动画表现 authoring 入口

Document package editable分片与Mutation Compiler MUST只编辑正式Graph、StateMachine、Timeline、Blackboard及唯一Capability catalog已安装的业务能力。CharacterAnimationPresentationProfile中的Pose Graph、PoseStateMachine、Local/Component转换、Blend、provider/source binding、AnimationSlot与Policy MUST通过共享typed payload和唯一Presentation Mutation进入editable Presentation；Rig v3、Calibration与generated Projection正文 MUST保持只读context。人工共享作者表面与Document apply MUST调用同一Mutation、Validator和事务服务，不得形成第二个动画表现authoring入口。有限Action AnimationTrack与持续Pose Source binding MUST各自在正式owner中编辑marker/curve，不得恢复Pose Graph MarkerSync节点、Locomotion Timeline或Final IK配置。未知Presentation变化 MUST被拒绝，MUST不转换成默认配置。

#### Scenario: Document配置Pose Graph

- **WHEN** AI修改Document v3中Capability已登记的Pose Graph空间化业务字段
- **THEN** Reconciler MUST生成与人工编辑相同的typed Presentation Mutation
- **AND** 未登记字段、Rig正文、generated payload、compiled stage或solver对象 MUST被拒绝

### Requirement: Agent 必须阻止 Marker Sync 数据分裂

Agent Document MUST只把有限Action Marker Sync可写数据放在对应Timeline AnimationTrack entity，只把持续Pose Source Marker Sync可写数据放在对应Presentation Profile binding entity。两类owner MUST使用stable identity或Document local identity与同一typed marker schema，MUST不互相复制，也不得写入Pose Graph节点、Transition、Blackboard、StateMachine edge、ActionProfile、FootPhase或generated Projection。名称、breadcrumb和index不得成为fallback。Validator MUST覆盖None残留、identity、Finite/Cyclic边界、SyncRole、call site、directed pair和animation output coverage。

#### Scenario: None Track保留Marker

- **WHEN** Document把Action Track设为None但仍保留group或Marker
- **THEN** dry-run MUST拒绝整个Document
- **AND** apply MUST不产生部分资产修改

#### Scenario: None Pose Source保留Marker

- **WHEN** Document把持续Pose Source设为None但仍保留SyncRole、group或Marker
- **THEN** dry-run MUST拒绝该Presentation package
- **AND** MUST不把数据移动到Timeline Track

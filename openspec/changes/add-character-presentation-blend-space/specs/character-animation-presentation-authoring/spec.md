## ADDED Requirements

### Requirement: Profile必须唯一绑定Blend Space表现来源

`CharacterAnimationPresentationProfile` MUST允许稳定producer identity绑定`AnimationPoseSourceKind.BlendSpace`与精确`CharacterAnimationBlendSpaceAsset`。同一producer MUST只有一条正式source binding；Timeline、MotionMatching与BlendSpace字段 MUST互斥。Pose Graph节点、Timeline、State、Agent Patch和generated Projection MUST不保存第二份可写Blend Space资源选择。

#### Scenario: 作者把Locomotion producer绑定到Blend Space

- **WHEN** 作者在Profile正式入口为稳定producer选择BlendSpace source kind与资产
- **THEN** Presentation Authoring Service MUST原子保存唯一binding
- **AND** 旧Timeline transition字段 MUST被删除而不是保留备用

### Requirement: Blend Space binding必须从正式composition roots发现producer

Presentation Authoring Service MUST从显式`CharacterPipelineDefinition`的composition roots递归发现Timeline、AnimationTrack或其它正式producer stable identity。它 MUST不从generated Program/Projection、旧Layer、显示名、目录或场景对象猜producer。Binding Validator MUST校验资产Rig、轴接口和可达BlendSpacePlayer合同。

#### Scenario: producer只能通过名称找到

- **WHEN** Profile中只有显示名相同但没有正式stable identity的候选
- **THEN** Authoring Service MUST拒绝绑定
- **AND** MUST不选择目录中的第一个同名资产

### Requirement: Workspace必须提供Blend Space正式入口

安装本能力后，Character Animation Authoring Workspace MUST在Navigator和资源打开流程中提供Blend Space资产模式，并使用与Pose Graph相同的Details、Live、References、Preview和显式Compile边界。安装前的“项目未安装Blend Space”禁用项 MUST被正式入口替换；系统 MUST不新增旧Workbench或第二套资源Inspector。

#### Scenario: 从Profile打开Blend Space引用

- **WHEN** 作者在Profile References中打开一个BlendSpace binding
- **THEN** Workspace MUST定位精确BlendSpaceId和content revision
- **AND** Details MUST显示引用它的producer与Pose Graph节点


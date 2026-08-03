## ADDED Requirements

### Requirement: Profile必须唯一绑定Blend Space Pose source

Pose Graph MUST拥有typed Blend Space Source Slot子资产，`CharacterAnimationPresentationProfile` MUST为该Slot拥有唯一typed Blend Space Binding子资产并精确引用`CharacterAnimationBlendSpaceAsset`。同一Slot MUST只有一条类型匹配binding；Sequence、Motion Matching与Blend Space必须使用各自独立的Slot和Binding类型。Pose Graph节点、Timeline、Gameplay State、Agent Patch和generated Projection MUST不保存第二份可写Blend Space资源选择。

#### Scenario: 作者把Locomotion Pose source绑定到Blend Space

- **WHEN** 作者在Profile正式入口为Graph-owned Blend Space Source Slot创建Binding并选择Blend Space资产
- **THEN** Presentation Authoring Service MUST在Profile文件内原子保存唯一typed Binding子资产
- **AND** 旧Gameplay producer、Timeline locomotion与Selection binding MUST被删除而不是保留备用

### Requirement: Blend Space binding必须从正式PoseState roots发现消费者

Presentation Authoring Service MUST从显式`CharacterPipelineDefinition`的Presentation Profile与PoseState inline subgraph递归发现精确Source Slot对象引用和BlendSpacePlayer消费者。它 MUST按对象引用解析唯一Profile Binding，不得从generated Program/Projection、Timeline、旧Layer、显示名、目录、数组index或场景对象猜source。Binding Validator MUST校验Slot/Binding类型、资产Rig、轴接口、Fact参数投影和可达BlendSpacePlayer合同。

#### Scenario: Pose source只能通过名称找到

- **WHEN** Profile中只有显示名相同但没有引用精确Source Slot对象的候选Binding
- **THEN** Authoring Service MUST拒绝绑定
- **AND** MUST不选择目录中的第一个同名资产

### Requirement: Workspace必须提供Blend Space正式入口

安装本能力后，Character Animation Authoring Workspace MUST在Navigator和资源打开流程中提供Blend Space资产模式，并使用与Pose Graph相同的Details、Live、References、Preview和显式Compile边界。安装前的“项目未安装Blend Space”禁用项 MUST被正式入口替换；系统 MUST不新增旧Workbench或第二套资源Inspector。

#### Scenario: 从Profile打开Blend Space引用

- **WHEN** 作者在Profile References中打开一个BlendSpace binding
- **THEN** Workspace MUST定位精确BlendSpaceId和content revision
- **AND** Details MUST显示引用它的PoseState、Pose source与Pose Graph节点

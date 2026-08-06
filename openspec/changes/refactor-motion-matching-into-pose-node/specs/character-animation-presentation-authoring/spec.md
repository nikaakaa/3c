# character-animation-presentation-authoring Specification

## MODIFIED Requirements

### Requirement: Presentation Profile必须唯一绑定Pose source

`CharacterAnimationPresentationProfile` MUST继续作为所有持续Pose能力的唯一跨资产装配入口。Sequence和BlendSpace binding MUST保持各自typed source；Motion Matching binding MUST引用一个MM Profile、一个typed Database Chooser、SearchDomain identity和正式generated artifact identities，并由Pose Graph中的`MotionMatchingPose`直接消费。Binding MUST不保存裸数据库数组、`CharacterMotionMatchingPoseSourceSlot`、SelectedPosePlayer配置、独立validation profile或fallback clip。

#### Scenario: 配置新增角色Motion Matching

- **WHEN** 作者在`MotionMatchingDemoCharacter` Presentation Profile配置Grounded MM binding
- **THEN** Inspector MUST显示该角色唯一Presentation Rig、MM Profile、Chooser、SearchDomain和产物状态
- **AND** MUST不要求在Pose Graph之外再配置Player或BlendStack

### Requirement: Blend Policy必须属于明确transition owner

Presentation authoring MUST明确区分PoseStateMachine transition policy、MotionMatchingPose Jump Blend Policy和AnimationSlot/Action transition policy。MM policy MUST保存在MM节点payload或其唯一typed policy引用中，并与该节点Rig一致。Inspector、Canvas和Compiler MUST不提供“全局MM默认淡入”或把同一policy复制到SelectedPosePlayer、显式BlendStack和Animancer Transition。

#### Scenario: 编辑MM Jump淡入

- **WHEN** 作者修改MotionMatchingPose的Blend Policy
- **THEN** 修改 MUST只影响该节点内部selection Jump
- **AND** MUST不改变state transition或Attack Slot权重

### Requirement: CharacterAnimationPresentationProfile Inspector必须是唯一Presentation配置入口

Profile Inspector MUST是Presentation Rig、MM Profile、Chooser、Database membership、SourceSet、Blend Policy、Pose Graph binding和generated artifact状态的唯一跨资产入口。它 MUST验证Presentation Rig与FeatureSchema、Database、SourceSet和全部artifacts的RigId+Revision闭包，并验证Chooser数据库均属于MM Profile。Inspector MUST只显示invalid或stale状态；选择Profile、展开面板、刷新、Domain Reload和进入Play Mode MUST不写资产或自动Build。

#### Scenario: Chooser引用其它Profile数据库

- **WHEN** 作者在Inspector查看包含Profile外数据库的Chooser
- **THEN** Inspector MUST显示具体database/profile identity冲突
- **AND** MUST不自动把数据库加入Profile或从Chooser移除

### Requirement: Pose Graph Producer Navigator必须从显式Definition上下文投影

Producer Navigator MUST从当前Pipeline Definition、Presentation Profile和Pose Graph Document投影Sequence、BlendSpace、MotionMatchingPose、PoseHistoryCollector、Chooser与Action producer引用。双击MotionMatchingPose MUST进入该节点唯一entry processing graph；返回操作 MUST恢复原state inline graph和节点选择。Navigator MUST不扫描项目目录猜测GASP数据库或打开旧MM fixture。

#### Scenario: 下钻新增角色MM内部图

- **WHEN** 作者双击`MotionMatchingDemoCharacter` Grounded MotionMatchingPose
- **THEN** Navigator MUST打开该节点的root-owned entry graph
- **AND** 面包屑 MUST显示Definition、Profile、Pose Graph、State和MM node identity

### Requirement: 跨资产表现配置必须保持唯一写入口

MM Profile、Chooser、SourceSet、Database、Pose Graph和Projection的修改 MUST通过正式authoring service与typed Mutation完成。Document exporter、reconciler、Inspector和Canvas MUST使用同一identity和validation规则。系统 MUST不维护旧MM binding与新node binding双写，也 MUST不在代码或Prefab保存第二份数据库列表。

#### Scenario: 删除MM节点

- **WHEN** 作者通过正式Mutation删除MotionMatchingPose
- **THEN** Document MUST删除节点引用并在entry graph无其它引用时删除该图
- **AND** MUST不改写MM Profile、Chooser或数据库资产内容

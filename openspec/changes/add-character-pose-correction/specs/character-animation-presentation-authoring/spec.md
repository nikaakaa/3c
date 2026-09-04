## MODIFIED Requirements

### Requirement: Pipeline Definition 必须引用唯一 Animation Presentation Profile

`CharacterPipelineDefinition` MUST引用唯一`CharacterAnimationPresentationProfile`，不得内联保存动画表现数据。Profile MUST唯一引用Pose Graph、Profile-owned Pose source binding子资产、有限Action producer source binding、node-local Policy、Rig、FullBodyIK Profile与Foot Analysis配置，并唯一拥有静态Correction Set与Correction Binding子资产。Pose Graph MUST唯一保存Presentation Fact Input、PoseStateMachine、Graph-owned Source Slot与Correction Slot子资产、ClipPlayer、BlendSpacePlayer、SelectedPosePlayer、AnimationSlot、composition、Local/Component Pose转换、显式姿态修正、FootPlacement、PoseBoneIKGoals、Goal Assembler、FullBodyIK与Output topology。Correction Node MUST通过精确Slot由当前Profile Binding解析样本与角色骨骼标定，不直接保存角色样本副本。Gameplay Graph、BTSMTL StateMachine、Timeline、Presenter与Prefab MUST不复制这些配置。

#### Scenario: Corin配置动画表现

- **WHEN** Corin Definition引用正式Animation Presentation Profile
- **THEN** Profile MUST为PoseStateMachine Source Slot和Action Slot producer提供唯一资源绑定
- **AND** Definition MUST不内联Run Clip、State transition或Slot policy

#### Scenario: shared Graph被多个角色使用

- **WHEN** 两个CharacterPipelineDefinition引用同一个shared Graph/Timeline
- **THEN** 两个角色 MAY引用不同CharacterAnimationPresentationProfile和Analysis Source
- **AND** 每个Profile MUST为shared Source Slot对象提供自己的binding子资产
- **AND** shared Graph/Timeline MUST不保存角色级资源、分析Rig或校准

#### Scenario: 两个角色复用修正图

- **WHEN** 同一个Correction Slot由两个角色Profile装配
- **THEN** 每个Profile MUST提供自己唯一的Binding、样本集和骨骼标定
- **AND** shared Graph MUST不携带某个角色的可写样本子资产

### Requirement: 跨资产表现配置必须保持唯一写入口

Pose Graph Workspace、Navigator与Details MAY只读显示Action Timeline Segment、Profile direct Clip Binding、Locomotion Sync Group、Clip注册Curve、Policy、Rig与Analysis状态。修改Action Segment编排 MUST导航到Timeline Editor；修改Clip骨骼或注册Curve MUST打开Unity Animation Window中的精确Clip与Preview Target；修改Profile Binding或Sync Group MUST导航到Profile；修改State transition与Slot Policy MUST导航到Pose Graph/Policy owner。Correction Set、Binding与中性/方向样本 MUST通过精确Profile owner的样本编辑入口修改，Pose节点只编辑自身输入、权重和末端政策。动画取帧 MUST只读原Clip并把完整结果提交到该样本owner。人工入口与Document v4 Reconciler MUST分别调用同一正式Mutation和资产事务，系统 MUST不复制字段、提供第二mutation命令、按窗口类型分叉写链或保留字符串binding镜像。

#### Scenario: 从Pose Graph调整Run Phase

- **WHEN** 作者在State source引用面板选择Open Source Curve
- **THEN** 必须打开RunLoop原生AnimationClip与正式Preview Target
- **AND** Pose Graph节点与Profile Binding MUST保持只读Clip引用摘要

#### Scenario: 从节点打开方向样本

- **WHEN** 作者从修正节点的References打开其样本
- **THEN** 工作区 MUST定位当前Definition的Profile Binding和正式样本owner
- **AND** 不得在节点Details创建可写的样本镜像

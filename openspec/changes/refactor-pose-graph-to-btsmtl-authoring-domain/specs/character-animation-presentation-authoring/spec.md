## MODIFIED Requirements

### Requirement: PoseStateMachine工作区必须对齐UE作者口径

Pose Graph Workspace MUST复用Graph Authoring Domain Framework的StateMachine表面，并显示State Machine、State、Transition Rule、State Alias、Sequence Player、Blend Space Player、Slot、Blend Logic与Inertialization等作者术语。StateMachine内部图、State Pose Graph和Transition Rule图 MUST使用明确下钻导航。Presentation domain adapter MUST显示compiled active state、target state、transition progress、Slot playback、source usage和route，不得展示或写入BTSMTL Gameplay State字段。

#### Scenario: 作者打开Locomotion PoseStateMachine

- **WHEN** 作者双击PoseStateMachine节点
- **THEN** 共享表面 MUST装配Presentation adapter并显示Entry、State、Alias和Transition edge
- **AND** MUST不显示Gameplay Action、ConditionRuleGraph或Timeline control edge

### Requirement: CharacterAnimationPresentationProfile Inspector必须是唯一Presentation配置入口

Profile Inspector MUST是人工作者配置Pose Graph、Pose source binding、Blend Policy、Inertialization Policy、Rig Definition、有限Action producer source binding、Foot Analysis Mode与Analysis Source的唯一owner界面。Agent Authoring Document v3 MAY通过同一Presentation Mutation修改这些正式owner，但 MUST不形成第二套字段模型、事务或资产写服务。Timeline Editor继续唯一编辑Action producer-local Clip、Window、Motion、Cue和Timeline marker。

#### Scenario: 人工修改Pose source

- **WHEN** 作者在Profile Inspector修改持续Locomotion source binding
- **THEN** Inspector MUST调用与Document Reconciler相同的Presentation Mutation
- **AND** Pose Graph Details与Timeline MUST不保存第二份binding

#### Scenario: shared Timeline用于不同角色

- **WHEN** 两个Profile使用同一shared Timeline但不同Analysis Source
- **THEN** 各自 MUST生成不同artifact identity与Projection
- **AND** shared Timeline MUST不保存任一角色的Analysis Source

### Requirement: Pose Graph Producer Navigator必须从显式Definition上下文投影

Pose Graph Navigator MUST通过共享Navigator host要求精确Definition context，并从Profile、Pose Graph和Gameplay composition roots分别投影Pose source与有限Action producer。Locomotion分组 MUST显示PoseState、Sequence/BlendSpace/MM consumer和Pose source binding；Action分组 MUST显示Timeline、Track、AnimationChannel与AnimationSlot consumer。Navigator MUST不读取generated Program/Projection完成bootstrap，不按显示名猜测，也不得保存或直接修改第二份binding。

#### Scenario: 查看Locomotion sources

- **WHEN** 作者从Corin Definition展开Locomotion
- **THEN** Navigator MUST列出Idle、Start、Move、Stop、Turn的正式Pose source
- **AND** MUST不列出BaseLocomotion Timeline producer

#### Scenario: 缺少Definition上下文

- **WHEN** 作者直接打开shared Pose Graph且没有精确Definition call-site context
- **THEN** Producer Navigator MUST显示Unavailable及缺失上下文原因
- **AND** MUST不搜索任意角色或使用上一次窗口context

### Requirement: 跨资产表现配置必须保持唯一写入口

Pose Graph Workspace、Navigator与Details MAY只读显示Action Timeline Track和Profile Pose source的resource、marker、curve、Policy、Rig与analysis状态。修改Action Clip、marker、window或curve MUST导航到Timeline Editor；修改Pose source resource、marker或Foot Placement Weight MUST导航到Profile source editor；修改State transition与Slot Policy MUST导航到对应Pose Graph owner。Agent Authoring Document v3 MUST把跨文件目标状态降低到这些相同owner的Presentation Mutation，不得复制字段或提供第二mutation命令。

#### Scenario: 从Pose Graph调整Run marker

- **WHEN** 作者在State source引用面板选择Open Source
- **THEN** 必须打开Profile中的Run Pose source editor
- **AND** Pose Graph节点 MUST保持只读引用

#### Scenario: Document同时修改Profile和Pose Graph

- **WHEN** Document v3提交跨Profile与Pose Graph的合法目标状态
- **THEN** Reconciler MUST在一个资产级事务内调用各正式owner mutation
- **AND** MUST不把Profile字段复制到Pose node

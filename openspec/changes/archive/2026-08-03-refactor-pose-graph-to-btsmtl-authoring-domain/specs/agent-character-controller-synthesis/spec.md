## MODIFIED Requirements

### Requirement: 正式资产必须仍由人类可微调

系统 MUST保持Agent应用后的Gameplay结果为普通BTSMTL Graph、有限Action Timeline与ActionProfile，动画表现结果为CharacterPipelineDefinition引用的CharacterAnimationPresentationProfile与Pose Graph。作者 MUST能在共享Graph Authoring UI调整Gameplay逻辑与Pose拓扑，在Timeline Editor调整有限Action时序，在Profile调整Pose source与Policy。Agent Document v3 MUST通过与这些人工入口相同的类型化Mutation修改正式资产，不得形成第二套资产或字段模型。

#### Scenario: 作者微调Agent应用结果

- **WHEN** Agent应用Attack State、Action Timeline与FullBodyAction Slot后作者继续编辑
- **THEN** 作者 MUST在各正式owner界面看到并修改同一资产结果
- **AND** 三个入口 MUST不双写同一字段

#### Scenario: Agent继续修改

- **WHEN** 作者微调Pose Graph后再次请求Agent增加dodge cancel
- **THEN** checkout或rebase MUST包含最新Gameplay、Timeline与Presentation目标状态
- **AND** apply MUST只执行Document相对最新基线的合法差异

### Requirement: Agent Snapshot 与 Validator 必须递归理解嵌套 StateMachine

Agent Document v3 MUST递归表达Gameplay RootTree、Runnable、inline/shared Graph、BTSMTL nested StateMachine、logical transition、Action activation、有限Action Timeline，以及Presentation Pose Graph、PoseStateMachine、State、Transition、state-local graph、Pose source binding、AnimationSlot与Policy。Validator MUST通过domain identity区分Gameplay StateMachine与PoseStateMachine，并分别调用正式capability与领域validator；持续Pose source MUST不被伪装为Timeline producer。

#### Scenario: Corin Document v3

- **WHEN** 导出迁移后的Corin Character Document
- **THEN** Gameplay editable MUST显示None、Attack、Dodge及其Action Timeline
- **AND** Presentation editable MUST显示Locomotion PoseStateMachine、Pose source、FullBodyAction Slot、Policy与Rig引用
- **AND** BTSMTL node MUST不包含Bone Mask或Pose composition字段

#### Scenario: Timeline channel identity断裂

- **WHEN** 可达AnimationTrack缺失或引用未知AnimationChannelId
- **THEN** Validator MUST输出对应Graph、Timeline与Presentation consumer路径
- **AND** apply transaction MUST回滚

### Requirement: Agent 不得形成第二个动画表现 authoring 入口

Agent MUST只通过Document v3、共享Authoring Capability Catalog、Presentation Reconciler与正式Presentation Mutation编辑CharacterAnimationPresentationProfile、Pose Graph、PoseStateMachine、Pose source binding、AnimationSlot与Policy。系统 MUST不提供Pose专用Patch schema、MCP工具、lowerer、handler、YAML writer或独立事务；Timeline authoring仍由Timeline正式Mutation拥有。

#### Scenario: Document配置Animation Slot

- **WHEN** Presentation分片包含合法AnimationSlot目标状态
- **THEN** Reconciler MUST通过共享capability生成正式Presentation Mutation
- **AND** MUST不转换成Timeline channel或默认mask字段

#### Scenario: 旧Patch请求配置Animation Slot

- **WHEN** 旧Patch或Macro入口提交`configure_animation_slot`
- **THEN** 系统 MUST因旧入口已删除而拒绝
- **AND** MUST不绕过Document transaction修改Presentation

# character-presentation-pose-graph Specification

## ADDED Requirements

### Requirement: LinkedPoseCall必须拥有上下文约束的root作者表面

`LinkedPoseCall` MUST只允许在root Pose Graph中创建，并在Details中从当前Profile选择唯一Group、再从该Group Interface选择Entry。Interface、signature与动态typed ports MUST由Group+Entry派生只读；作者 MUST不输入InterfaceId、EntryId或端口identity字符串。改变Group或Entry前，Mutation preflight MUST验证所有现有edge仍匹配目标端口identity、方向、kind与Pose空间；不兼容时 MUST拒绝修改并报告精确edge，不得静默断线。

#### Scenario: 作者为EquipmentPose放置Call

- **WHEN** 作者在root graph选择Equipment Group和EquipmentPose Entry
- **THEN** Canvas MUST从Interface投影精确Local Pose输入输出并保存稳定Group+Entry引用
- **AND** Details MUST提供打开Group、Interface与对应Implementation Entry的导航

#### Scenario: Entry Graph尝试创建LinkedPoseCall

- **WHEN** 当前Graph context为Linked Implementation Entry
- **THEN** Capability与创建菜单 MUST拒绝该节点
- **AND** MUST不通过子图、粘贴或Document绕过root-only约束

### Requirement: Linked Implementation Entry边界必须由Interface自动投影

每个required Entry Graph的`GraphInput`与`GraphOutput`动态ports MUST由其Interface Entry合同创建并校验，作者 MAY在图内连接业务节点但 MUST不独立修改边界signature。Interface改变后，工作区与Validator MUST把不匹配Entry、port与edge标记为Invalid并提供合同跳转，不得按名称自动迁移。

#### Scenario: 新建Rifle EquipmentHandGoals Entry

- **WHEN** Implementation closure基于Equipment Interface创建该Entry Graph
- **THEN** GraphInput/GraphOutput MUST具有合同声明的Component Pose输入与FullBodyIK Goals输出
- **AND** 作者 MUST不手工输入port kind或identity

## MODIFIED Requirements

### Requirement: Authoring Capability Catalog必须是UI与Document的唯一语义目录

唯一Framework MUST继续通过`GraphAuthoringCapabilityCatalog`查询每个domain的Graph kind、node kind、typed payload、静态/动态logical port、数据类型、Pose空间、非Pose瞬时value空间、execution domain、允许连接、资源引用、创建菜单、显示标题、Details provider与Mutation入口。Pose领域 MUST由唯一`CharacterPoseNodeDefinitionModule`为每个正式Node Kind集中声明Payload、字段、端口、Graph Role、Execution Domain、Operation Family、局部校验与typed lowering，并向共享Capability、人工UI、Document、Clipboard、Mutation、Validator和Compiler投影同一节点局部语义；Capability MUST不再保存与Pose Definition重复的Compiler Handler或布尔能力矩阵。

BTSMTL、AI与其它Graph领域 MAY通过各自正式Definition Adapter向同一Framework提供Capability，但 MUST不被迫引用Pose运行类型。人工UI、Document exporter、strict parser、Reconciler、Validator和Compiler MUST读取同一领域Definition/Capability投影；固定port MUST不在实例数据中复制。Definition与Capability未声明的字段、port、Pose空间转换、瞬时value lineage或execution domain MUST不被任何入口创建或保存，系统 MUST不按C#类型名、显示名、窗口类型或字段路径重复硬编码能力。

Pose Node Definition只拥有节点局部作者与lowering语义，MUST不接管Document package路径、文件闭包、diff、Undo、rollback、save或reverse export事务；现有Reconciler与Document Transaction Service MUST继续分别拥有唯一对账和事务生命周期。

#### Scenario: FootPlacement声明双输出

- **WHEN** FootPlacement Pose Definition声明`pose.component`与`component.full-body-ik-goal-contribution`两个输出
- **THEN** Capability、Canvas、Document、Validator与Compiler MUST从同一Definition投影识别两个稳定port及其lineage规则
- **AND** MUST不把Goal Contribution伪装成Pose、动态字符串port或隐藏Compiler字段

#### Scenario: Goal Contribution连接错误节点

- **WHEN** 作者或Document把`component.full-body-ik-goal-contribution`连接到未声明该输入类型的节点
- **THEN** Mutation MUST在写资产前拒绝
- **AND** Compiler Topology Pass MUST继续执行同一规则作为完整性校验

#### Scenario: 新增Pose节点能力

- **WHEN** 开发者注册一个新的Component Pose骨骼控制节点
- **THEN** 唯一Pose Definition MUST声明其Component Pose端口、execution domain、typed payload、Operation Family与typed lowering
- **AND** Capability、人工创建菜单、Document、Validator和Compiler MUST同时识别该能力而不得注册第二Compiler Handler

#### Scenario: Definition未声明字段

- **WHEN** UI或Document尝试写入当前node Definition未声明的字段
- **THEN** Mutation MUST拒绝该命令并返回稳定诊断
- **AND** MUST不通过SerializedProperty path、自由文本或Reconciler特例绕过目录

#### Scenario: Local Pose连接Component Pose

- **WHEN** 作者或Document创建空间不兼容的Pose edge
- **THEN** 共享connection policy MUST在Mutation前拒绝
- **AND** Compiler Topology Pass MUST继续执行同一规则作为完整性校验

#### Scenario: Definition尝试接管Document事务

- **WHEN** Pose Definition Adapter尝试直接修改Unity对象、执行apply、创建Undo或发布canonical package
- **THEN** Framework MUST拒绝该依赖并保持正式Reconciler与Transaction Service调用链
- **AND** MUST不建立Pose专用Document入口或第二事务Owner

## MODIFIED Requirements

### Requirement: 文档包必须分离可编辑authoring、只读context与service基线

Document package MUST显式分离`editable/`、`context/`与`service.json`。Pose Graph节点、显式Pose空间转换、typed edge、PoseStateMachine、Transition、Presentation Profile source binding、Action Timeline marker/curve MUST进入其正式owner的editable分片；Rig v3正文、Calibration正文、Foot Analysis artifact、Projection、compiled stage/workspace与runtime状态 MUST只进入context。Reconciler MUST拒绝context变化，不得把只读Rig或generated字段转换为默认Presentation Mutation。

#### Scenario: AI读取Character文档包

- **WHEN** AI checkout一个包含FootPlacement的Character Definition
- **THEN** editable MUST能追踪空间化Pose拓扑和Profile binding
- **AND** context MUST只读显示Rig v3 chain、Calibration、artifact与compiled stage摘要

#### Scenario: AI尝试修改只读context

- **WHEN** AI修改Rig v3 pelvis BoneId或generated Foot Analysis channel
- **THEN** dry-run MUST拒绝整个Document
- **AND** apply MUST不产生部分Presentation mutation

### Requirement: Presentation JSON必须由共享Capability生成稀疏typed字段

Presentation Graph、StateMachine、Transition与Profile binding JSON MUST由唯一Graph Authoring Capability生成stable kind、typed payload、Local/Component logical port、execution domain约束和资源引用。JSON MUST显式保存作者创建的空间转换节点与edge；MUST不保存Capability已知固定port镜像、compiled stage、workspace、dense Bone index、PhysicsScene、solver对象、Final IK配置、默认字段或未知property bag。Pose Source binding MUST完整保存其owner的SyncRole、topology、markers与typed curve。

#### Scenario: Sequence Player JSON包含IK字段

- **WHEN** Exporter处理Sequence Player
- **THEN** JSON MUST只包含该Capability登记的source与player字段
- **AND** MUST不输出IK、Pose空间猜测、world stage或默认字段

#### Scenario: Document新增空间转换

- **WHEN** AI在Local Pose与FootPlacement之间新增LocalToComponentPose并连接typed edge
- **THEN** Reconciler MUST降低为与人工Canvas相同的Presentation Mutation
- **AND** 未登记port或错配Pose空间 MUST使dry-run失败

### Requirement: Presentation Reconciler必须调用唯一Presentation Mutation

Presentation Reconciler MUST把Pose Graph、PoseStateMachine、Transition、显式空间转换、Pose Source marker/curve/sync与Profile binding变化降低为唯一typed Presentation Mutation，并与人工UI共用Capability、validator、Undo transaction和资产提交服务。Rig v3正文、Calibration、generated artifact/Projection与compiled stage plan MUST不可写。Reconciler MUST不创建Final IK组件、Timeline Locomotion副本或第二authoring service。

#### Scenario: apply新增Pose State

- **WHEN** Document新增Pose State、Local source图、空间转换和Component Pose控制链
- **THEN** apply MUST在一个Presentation transaction中创建完整authoring拓扑
- **AND** 任一edge、Rig引用或binding非法 MUST回滚整个owner

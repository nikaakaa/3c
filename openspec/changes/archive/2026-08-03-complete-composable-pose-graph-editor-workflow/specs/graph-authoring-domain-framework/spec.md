## MODIFIED Requirements

### Requirement: Authoring Capability Catalog必须是UI与Document的唯一语义目录

唯一Framework MUST通过`GraphAuthoringCapabilityCatalog`查询每个domain的Graph kind、node kind、typed payload、静态/动态logical port、数据类型、Pose空间、execution domain、允许连接、资源引用、创建菜单、显示标题、Details provider、Mutation入口与Compiler handler。人工UI、Document exporter、strict parser、Reconciler、Validator和Compiler MUST读取同一Capability；固定port MUST不在实例数据中复制。Capability未声明的字段、port、Pose空间转换或execution domain MUST不被任何入口创建或保存。

#### Scenario: 新增Pose节点能力

- **WHEN** 开发者注册一个新的Component Pose骨骼控制节点
- **THEN** 同一Capability MUST声明其Component Pose端口、execution domain、typed payload与compiler handler
- **AND** 人工创建菜单、Document、Validator和Compiler MUST同时识别该能力

#### Scenario: capability未声明字段

- **WHEN** UI或Document尝试向FootPlacement节点写入未登记solver对象字段
- **THEN** Mutation MUST拒绝该字段
- **AND** MUST不保留任意property bag或默认值

#### Scenario: Local Pose连接Component Pose

- **WHEN** 作者或Document创建空间不兼容的Pose edge
- **THEN** 共享connection policy MUST在Mutation前拒绝
- **AND** Compiler MUST继续执行同一规则作为完整性校验

### Requirement: Graph Canvas必须复用统一节点与端口投影

唯一Canvas MUST通过Capability与domain adapter投影节点、静态/动态port、edge、selection、clipboard、Undo、搜索与创建。Pose端口 MUST从stable type投影Local/Component空间颜色和标签，转换节点 MUST作为普通serialized authoring节点显示。Canvas MUST不根据C#类型名、显示名或Compiler operation猜测空间，也 MUST不隐藏插入未序列化节点。

#### Scenario: 节点拥有动态输入

- **WHEN** GraphInput或GraphOutput增加一个显式Component Pose动态port
- **THEN** Canvas MUST使用节点局部稳定identity投影并保存该port
- **AND** clipboard与Document往返 MUST保留其Pose空间

#### Scenario: 作者查看空间转换

- **WHEN** Pose Graph包含LocalToComponentPose
- **THEN** Canvas MUST显示Local输入与Component输出
- **AND** Diagnostics MAY显示compiled stage但作者数据 MUST不保存stage index

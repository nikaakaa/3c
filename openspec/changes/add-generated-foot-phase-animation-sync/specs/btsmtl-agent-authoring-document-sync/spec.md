## MODIFIED Requirements

### Requirement: Presentation分片必须保持整包同步与稳定owner

Document v3 MUST使用`editable/presentation/profile.json`、`editable/presentation/pose-graphs/<graph-id>/graph.json`、对应`layout.json`，以及`editable/presentation/pose-state-machines/<state-machine-id>/state-machine.json`与对应`layout.json`表达Presentation目标状态。Profile source binding MUST表达该持续Pose source唯一拥有的Marker Sync mode、canonical group、topology、SyncRole、Time Mapping和ordered marker。Pose StateMachine的`state-machine.json` MUST只表达Entry、State、Alias、Transition、Rule与blend语义；Transition与Rule MUST不保存SyncMode、SyncGroupId、Time Mapping或generated warp plan。同目录`layout.json` MUST只稀疏表达合法Entry、State与Alias的稳定identity和有限二维位置。Profile binding子资产与Pose Graph Source Slot子资产 MUST通过包含asset GUID、有符号且非零local file id和一致asset path的结构化对象引用表达；负local file id MUST视为合法Unity子资产身份；新建子资产 MUST使用`local:*`计划identity并在apply成功后的reverse export中替换为正式对象引用。分片 MUST通过稳定owner identity互相引用，并继续服从整包checkout、hash、dry-run、apply、Conflict与反向导出语义；不得提供文件级apply、旧单文件闭包reader、缺失layout fallback、按显示名解析或缺失local file id fallback。

#### Scenario: AI只修改一个Pose节点的Source Slot

- **WHEN** 仅一个Pose Graph Player改为引用另一个既有Source Slot对象
- **THEN** dry-run与apply MUST仍锁定并处理整个Document包及精确Profile/Pose Graph owner
- **AND** 反向导出 MUST更新整包基线与规范对象引用

#### Scenario: AI创建Profile binding子资产

- **WHEN** editable使用`local:*`声明一个新的Profile-owned binding并引用既有Source Slot
- **THEN** Reconciler MUST生成typed子资产创建、Profile数组更新和资源配置Mutation
- **AND** apply成功后reverse export MUST发布正式GUID与local file id引用

#### Scenario: AI修改Profile source同步策略

- **WHEN** AI把Walk或Run Profile source binding的Time Mapping改为GeneratedFootPhase
- **THEN** Reconciler MUST只修改该binding的正式authoring字段并使Projection stale
- **AND** reverse export MUST在`profile.json`发布canonical策略且不得输出warp knot、artifact连续样本或Projection plan

#### Scenario: checkout导出Pose StateMachine

- **WHEN** Character Document显式checkout包含一个正式Pose StateMachine
- **THEN** 规范包 MUST在同一stable segment目录输出`state-machine.json`与`layout.json`
- **AND** 两个文件 MUST使用相同StateMachine identity并共同进入manifest与document hash

#### Scenario: AI只移动一个Pose State

- **WHEN** AI只修改Pose StateMachine `layout.json`中一个合法State的位置
- **THEN** Reconciler MUST生成同一正式layout owner的typed Presentation Mutation
- **AND** apply MUST更新Undo、资产dirty与canonical package基线
- **AND** MUST不修改StateMachine `ContentRevision`或发布Program、Projection与Native Pose Program

#### Scenario: 旧闭包缺少StateMachine layout文件

- **WHEN** 工具升级前的Document v3 manifest只包含Pose StateMachine `state-machine.json`
- **THEN** dry-run与apply MUST拒绝该旧闭包并要求显式重新checkout
- **AND** MUST不补写文件、兼容读取旧形状或建立两种apply路径

### Requirement: Presentation JSON必须由共享Capability生成稀疏typed字段

Pose Graph node、port、field、StateMachine页面、Source Slot和Profile binding的JSON合同 MUST来自Graph Authoring Domain Framework的同一Authoring Capability Catalog。每个node MUST只包含当前capability有意义的typed payload与node-local动态port；Source关系 MUST使用结构化对象引用。Profile source binding的Marker Sync MUST由同一Capability按mode稀疏输出合法group、topology、SyncRole、Time Mapping与ordered marker；`None` MUST不输出残留字段，MarkerGroup MUST不接受Unspecified Time Mapping。JSON不得输出作者Source Id、Provider Id、C#类型、SerializedProperty path、runtime枚举载荷、联合体空字段、artifact连续同步样本或generated warp plan。

#### Scenario: Sequence Player JSON包含Source Id字符串

- **WHEN** Sequence Player payload包含`pose-source-id`、`provider-id`或任意字符串资源引用
- **THEN** strict parser MUST在Reconciler前拒绝该分片
- **AND** MUST要求类型匹配的Source Slot对象引用

#### Scenario: Sequence Player JSON包含IK字段

- **WHEN** Sequence Player payload包含TwoBoneIK字段或未知property
- **THEN** strict parser MUST在Reconciler前拒绝该分片
- **AND** MUST不忽略字段或保留扩展字典

#### Scenario: Profile binding包含generated payload

- **WHEN** Profile binding JSON包含warp knot、plan identity或Foot Analysis连续同步样本
- **THEN** strict parser MUST在Reconciler前拒绝该`profile.json`
- **AND** MUST只允许作者Time Mapping并由显式Character Build生成Projection plan

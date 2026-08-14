## ADDED Requirements

### Requirement: MCP bridge必须透传Animation Sequence事务

五个BTSMTL lifecycle tool MUST接受并返回`btsmtl-agent-authoring-document.v3`同步与validation结果，并通过显式domain透传CharacterController或AIController事务。Character package MUST覆盖State、Action、Timeline、Animation Sequence、Blend Space引用、Marker、Notify、Curve、Node、Edge与Presentation owner可写语义；Sequence文件对、Profile Binding、Blend Space sample和Timeline Segment MUST进入同一Store、Reconciler、Mutation、transaction与Validator。Bridge MUST不新增Sequence专用action、局部apply、Node级tool、Patch JSON、YAML、反射或旧schema转换。

#### Scenario: apply创建Sequence并接入Timeline

- **WHEN** Character Document在同一plan创建Sequence并让Timeline Segment引用
- **THEN** MCP bridge MUST只调用统一Document application service一次
- **AND** 成功返回 MUST包含同一document hash、applied diff与Clean状态

#### Scenario: Sequence引用preflight失败

- **WHEN** Sequence或Segment引用在preflight失败
- **THEN** Bridge MUST返回机器可读Sequence文件与entity path
- **AND** MUST不改走Sequence局部mutation或保存部分资产

## MODIFIED Requirements

### Requirement: Agent Snapshot 必须完整投影 MotionWarp authoring

唯一当前Agent Character domain Snapshot MUST输出MotionWarp Track/Clip subtype、stable identity、SourceMotionClipId、resolved source path、Translation Mode、Target Offset Space、TargetPlanarOffset、Rotation Mode、Rotation Method、TargetYawOffset、最大平面/yaw修正、最大yaw速率、Limit Policy及当前mode消费的canonical progress curve。Snapshot MUST只投影正式Timeline资产，不输出Preview target、runtime target snapshot或mutable累计轨迹state。旧position/yaw weight与旧target-local专用字段 MUST不再输出。

#### Scenario: 导出Skew普通攻击

- **WHEN** Agent导出包含SkewToTarget与ApproachDirection的Attack Timeline
- **THEN** Snapshot MUST能唯一定位Warp、source MotionCurve、offset空间、两条有效curve与limit
- **AND** Track重排后两者identity关系 MUST保持

#### Scenario: 导出ConstantRate旋转

- **WHEN** Agent导出ConstantRate MotionWarp
- **THEN** Snapshot MUST输出MaximumYawRateDegreesPerSecond
- **AND** MUST不把未消费Yaw Progress冒充运行参数

### Requirement: Agent Patch 必须通过类型化命令修改 MotionWarp

唯一当前Agent Character domain Patch MUST通过typed命令创建MotionWarp Track/Clip、配置source、Translation Mode、Offset Space、Target Pose参数、Rotation Mode、Rotation Method、Limit Policy、限制与所需curve/rate。Lowerer MUST生成唯一immutable command plan，dry-run与apply MUST复用该plan；Handler MUST调用Timeline正式MotionWarp authoring API，MUST不直接编辑YAML、不按名称猜source、不自动转换旧weight，也 MUST不创建MotionWarp专用MCP入口。

#### Scenario: Agent创建普通近战Warp

- **WHEN** Patch为攻击source配置SkewToTarget、ApproachDirection与FaceTarget/ProgressCurve
- **THEN** Lowerer MUST解析全部enum和stable source identity
- **AND** Handler MUST通过正式ConfigureAuthoring与generic curve channel mutation写入同一Clip
- **AND** dry-run与apply MUST得到相同计划

#### Scenario: Patch提交旧weight字段

- **WHEN** Agent Patch包含PositionWeight、YawWeight或旧TargetLocalPlanarOffset字段
- **THEN** schema或lowerer MUST作为未知字段拒绝
- **AND** MUST不转换、忽略或使用默认值继续


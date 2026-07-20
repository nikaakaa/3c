## ADDED Requirements

### Requirement: Agent Snapshot 必须完整投影 MotionWarp authoring

Agent Snapshot MUST输出MotionWarp Track/Clip subtype、stable identity、SourceMotionClipId、resolved source path、position/rotation mode、target offset、weight、clamp和两条canonical progress curve。Snapshot MUST只投影正式Timeline资产，不输出Preview target或runtime mutable state。

#### Scenario: 导出带 MotionWarp 的 Timeline

- **WHEN** Agent导出包含MotionWarp的Character Definition
- **THEN** Snapshot MUST能唯一定位Warp与source MotionCurve
- **AND** Track重排后两者identity关系 MUST保持不变

### Requirement: Agent Patch 必须通过类型化命令修改 MotionWarp

Agent Patch MUST提供创建MotionWarp Track/Clip、配置source与typed参数及删除Clip的确定性命令。Lowerer MUST生成唯一immutable command plan，dry-run与apply MUST复用该plan；Handler MUST调用Timeline正式authoring API，MUST不直接编辑YAML、不按名称猜source，也 MUST不创建第二套MotionWarp配置。

#### Scenario: Agent 创建目标攻击 Warp

- **WHEN** Patch引用一个已存在或同事务创建的MotionCurveClip symbol
- **THEN** Lowerer MUST解析为stable source identity
- **AND** Handler MUST创建合法MotionWarpClip并保持source关系

### Requirement: Agent Validator 必须复用 MotionWarp 正式校验

Agent Validator MUST检查source identity、Timeline owner、窗口、Action channel、Override语义、mode、offset、weight、clamp、progress curve、Action Context与ActionTargetRequirement，并与Inspector和Semantic Compiler复用同一校验服务。任何错误 MUST定位到Graph、Timeline、Track、Clip、ActionProfile与相关source。

#### Scenario: Agent 配置缺少目标的 Warp

- **WHEN** Patch为`ActionTargetRequirement.None`动作增加MotionWarp
- **THEN** dry-run MUST失败并报告目标要求矛盾
- **AND** apply MUST不修改任何资产

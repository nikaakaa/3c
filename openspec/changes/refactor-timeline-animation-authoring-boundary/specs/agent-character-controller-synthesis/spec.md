## MODIFIED Requirements

### Requirement: Agent Validator必须透传正式Foot Analysis编译诊断

Agent Validator MUST透传正式Artifact Builder、Artifact Store、Projection binding与Build Transaction诊断，区分Missing、Stale、Corrupt、Source/Rig/Calibration不匹配和stable clip binding缺失。Agent MUST不采样AnimationClip、不写artifact、不输出feature payload，也不得新增Foot Analysis rebuild或generated curve mutation。

#### Scenario: Agent修改Foot Placement Weight

- **WHEN** 合法v15 Patch只修改现有Foot Placement Weight
- **THEN** apply后正式Definition Build MUST重新校验所需artifact并发布Projection
- **AND** Agent MUST不直接读取或修改artifact文件

#### Scenario: Artifact损坏

- **WHEN** Validator发现当前clip expected artifact为Corrupt
- **THEN** Compile Report MUST定位Clip、Source、Rig、Calibration和artifact identity
- **AND** Agent MUST不使用Timeline、Projection或默认feature修复该文件

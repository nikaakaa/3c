## MODIFIED Requirements

### Requirement: Blend Policy必须属于显式Blend Stack节点

`CharacterAnimationBlendPolicy` MUST只保存CrossFade、Stored Pose、capacity、canonical curve与dense Blend Profile配置，MUST不保存Inertial technique或residual参数。Inertialization节点 MUST引用独立`CharacterPoseInertializationPolicy`并完整物化自己的endpoint pair。

#### Scenario: 旧Policy包含Inertial override

- **WHEN** Build读取仍包含Inertial technique的Blend Policy
- **THEN** Build MUST失败并要求迁移到具体Inertialization节点Policy


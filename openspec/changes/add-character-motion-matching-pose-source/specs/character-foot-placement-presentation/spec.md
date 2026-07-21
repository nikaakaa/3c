## ADDED Requirements

### Requirement: MM Foot Feature必须沿最终Pose贡献进入Foot Placement

Motion Matching selected Clip/SampleTime MUST通过Projection中的正式Foot Analysis与Foot Placement Weight生成source Foot Feature，并沿Resolved Pose Request、PoseSlotFrame与FinalAnimationPoseFrame的实际per-foot contribution进入Foot Placement。Stored Pose与Inertial MUST继续按Blend Stack合同传播feature。Foot Placement MUST不直接查询MM Database、Selection或Cost。

#### Scenario: MM在左脚plant期间Inertial切换

- **WHEN** BaseLocomotionSlot从旧MM sample过渡到新sample
- **THEN** Blend Stack MUST连续组合两侧左脚feature并由Pose Graph输出最终贡献
- **AND** Foot Placement MUST只消费该最终输入

### Requirement: Foot Placement不得反向成为MM搜索输入

Foot Placement的Free/Locked/Sliding、surface anchor、support envelope、pelvis offset与IK结果 MUST不写入MM query、candidate admission、Cost Profile或Database history。Body reset MAY同时清理两者，但二者 MUST不共享mutable接触状态。

#### Scenario: 世界表面导致Foot Placement重新落脚

- **WHEN** Foot Placement因台阶或移动Surface释放旧anchor
- **THEN** MM selection MUST不因该world constraint直接跳转
- **AND** 下一次MM query MUST继续只使用Base pose Foot Feature与trajectory source

## ADDED Requirements

### Requirement: MM Foot Feature必须沿最终Pose贡献进入PredictiveFootPlacement

Motion Matching selected Clip/SampleTime MUST通过Projection中的正式Foot Analysis与Foot Placement Weight生成source Foot Feature，并沿Animation Selection与普通Pose Value的实际per-foot contribution进入显式PredictiveFootPlacement节点。Blend Stack Stored Pose MUST按多source贡献传播feature；局部Inertialization MUST按真实脚骨骼残差包络连续化feature。PredictiveFootPlacement MUST不直接查询MM Database、Selection或Cost。

#### Scenario: MM在左脚plant期间Inertial切换

- **WHEN** MM Blend Stack节点从旧sample过渡到新sample
- **THEN** 该节点 MUST连续组合两侧左脚feature并由下游Pose节点输出最终贡献
- **AND** PredictiveFootPlacement MUST只消费该最终输入并发布Goals

### Requirement: Grounding、PredictiveFootPlacement与FullBodyIK不得反向成为MM搜索输入

FinalIK Grounding结果、PredictiveFootPlacement的Free/Locked/Sliding、surface anchor、support envelope、pelvis goal与Foot Goals，以及FullBodyIK solved pose MUST不写入MM query、candidate admission、Cost Profile或Database history。Body reset MAY同时清理各自状态，但MM、Goal Source与solver MUST不共享mutable接触状态。

#### Scenario: 世界表面导致PredictiveFootPlacement重新落脚

- **WHEN** PredictiveFootPlacement因台阶或移动Surface释放旧anchor
- **THEN** MM selection MUST不因该world constraint直接跳转
- **AND** 下一次MM query MUST继续只使用绑定history source节点的Foot Feature与trajectory source

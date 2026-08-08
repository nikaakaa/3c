## ADDED Requirements

### Requirement: MM Foot Feature必须沿最终Pose贡献进入FootGrounding与预测Modifier

Motion Matching selected Clip/SampleTime MUST通过Projection中的正式Foot Analysis与Foot Placement Weight生成source Foot Feature，并沿Animation Selection与普通Pose Value的实际per-foot contribution先进入显式FootGrounding，再进入可选PredictiveFootPlacementModifier。Blend Stack Stored Pose MUST按多source贡献传播feature；局部Inertialization MUST按真实脚骨骼残差包络连续化feature。FootGrounding与Modifier MUST不直接查询MM Database、Selection或Cost。

#### Scenario: MM在左脚plant期间Inertial切换

- **WHEN** MM Blend Stack节点从旧sample过渡到新sample
- **THEN** 该节点 MUST连续组合两侧左脚feature并由下游Pose节点输出最终贡献
- **AND** FootGrounding MUST只消费该最终输入生成contact/anchor与Baseline Goals，Modifier只消费其Swing/landing输入

### Requirement: Grounding、PredictiveFootPlacementModifier与FullBodyIK不得反向成为MM搜索输入

FootGrounding的Lyra current trace/offset/normal、world contact/anchor、pelvis resolve与Baseline Goals，PredictiveFootPlacementModifier的Swing rewrite、support envelope与Final Goals，以及FullBodyIK solved pose MUST不写入MM query、candidate admission、Cost Profile或Database history。Body reset MAY同时清理各自状态，但MM、Goal Source与solver MUST不共享mutable状态。

#### Scenario: 世界表面导致FootGrounding释放anchor

- **WHEN** FootGrounding因台阶、移动Surface失效或转入Swing释放旧anchor
- **THEN** MM selection MUST不因该world constraint直接跳转
- **AND** 下一次MM query MUST继续只使用绑定history source节点的Foot Feature与trajectory source

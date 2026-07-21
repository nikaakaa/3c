## MODIFIED Requirements

### Requirement: Animation Clip Foot Placement曲线必须沿正式表现投影采样

`CharacterFootPlacementProfile` MUST只声明PoseSourceLayerId和角色级算法参数。每个Timeline Animation Clip MUST以stable clip identity保存一条归一化`Foot Placement Weight`曲线，表达该动画时间点允许Foot Placement整体介入多少；Prediction、Pelvis和Foot Rotation MUST继续由Profile与planner算法负责，不得成为逐Clip重复作者曲线。Foot Placement Weight MUST作为Animation Clip typed Curve Channel进入通用Timeline Curve Editor，不得保留`TimelineFootPlacementWeightCurve`、Foot Placement专用Curve View或第二mutation入口。曲线 MUST随Timeline编译进Presentation Projection。Projection采样 MUST先按producer内部clip weight混合，Runtime再按Animancer实际visible state/layer weight混合`AnimationPoseContribution`。系统 MUST不使用Marker Sync作为Foot contact/plant真相，也 MUST不使用逻辑priority、State、Action、Tag、clip名、asset path或数组index选择策略。

#### Scenario: Attack淡出到Run

- **WHEN** Attack Animation Clip的Foot Placement Weight从0恢复到1并与Run淡入重叠
- **THEN** Foot Placement总权重 MUST先使用各自当前Animation Clip曲线采样，再按两者实际视觉weight连续混合
- **AND** Marker Pair与fraction MUST不进入该权重计算

#### Scenario: 在通用Curve Lane编辑Foot Placement

- **WHEN** 作者展开Animation Track的Foot Placement Weight channel
- **THEN** 该channel MUST与Animation Weight和Ease复用同一Curve Renderer、key交互、Inspector与Undo事务
- **AND** MUST继续调用Animation Clip正式Foot Placement mutation与validator
- **AND** MUST不恢复Prediction、Pelvis或Rotation逐Clip曲线

#### Scenario: Curve Channel缺失或非法

- **WHEN** Foot Placement Weight curve缺少key、包含非有限值或超出`[0,1]`
- **THEN** Timeline validation与Projection build MUST拒绝
- **AND** Editor MUST不使用默认全1 curve作为fallback

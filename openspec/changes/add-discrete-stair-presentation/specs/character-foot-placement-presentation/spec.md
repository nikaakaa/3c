## MODIFIED Requirements

### Requirement: 当前Grounding与未来Support Envelope必须显式分离

每只脚 MUST使用固定容量workspace。普通`FootGrounding` MUST按Lyra Foot Plant参数从输入Foot BoneId执行一次NonAlloc SphereCast，取得唯一current hit、Target Foot Offset Z与Hit Normal，再按Lyra offset/normal smoothing形成current目标，并由Stance Stabilization和Pelvis Resolve生成Baseline Goals；合法stance脚 MAY把该current hit保存为surface-local anchor并随同一移动surface重建。它 MUST不执行FinalIK Grounding Ray/Capsule Quality、heel/toe双查询、Plant Plane或第二current query。可选`PredictiveFootPlacementModifier` MUST只对未被anchor拥有的Swing脚动画路径和Future Landing位置执行NonAlloc Ray/Sphere/Capsule查询，并输出`FutureLandingSupport`、有序Ground Envelope segment与每段minimum allowed sole height。路径候选 MUST只按layer、self-collider、有限值、最大坡度、最大step up/down、腿长可达性、surface identity、edge gap与路径连续性构造，不得覆盖current Sphere Trace结果。全部结果 MUST来自合法有限命中，不得是隐藏Collider、默认平面或fallback，也 MUST不把路径最远命中直接当作当前脚目标。

Foot Placement正式查询Mask MUST包含普通共享`Ground`与Ramp楼梯真实踏面`FootPlacementSurface`，并 MUST排除Gameplay专用`CharacterTraversal`。注册Ramp楼梯的无Renderer Traversal Ramp MUST不成为current Sphere hit、Future Landing Support或Ground Envelope；其真实踏面Collider即使不进入Deterministic Collision Artifact，也 MAY作为合法Presentation Surface。未注册Ramp绑定的离散楼梯 MUST使用同一组`Ground`阶梯Collider同时作为Gameplay与Presentation Surface，Foot Placement MUST按普通Ground规则查询它们，不得要求`FootPlacementSurface`副本。系统 MUST不同时查询Ramp和踏面后按命中优先级选择，不得从KCC support identity、Step阶段或Collision Artifact重建Foot Surface。

#### Scenario: 脚跨过两个楼梯边缘

- **WHEN** 当前脚与预测落点之间存在多个高度连续的合法踏面
- **THEN** Ground Envelope MUST保留surface和edge分段顺序
- **AND** Swing脚只在动画Y低于minimum envelope时抬高
- **AND** 当前脚X/Z MUST不被FutureLandingSupport替换

#### Scenario: 预测路径跨越不可达高差

- **WHEN** 相邻候选高度、edge gap或reach超过Profile允许范围
- **THEN** 后续segment MUST被裁剪并记录明确原因
- **AND** FutureLandingSupport MUST不跨越该中断

#### Scenario: Body沿Gameplay Ramp上楼

- **WHEN** KCC Body沿`CharacterTraversal`连续升高且脚下存在`FootPlacementSurface`真实踏面
- **THEN** Foot Placement MUST从真实踏面生成Lyra current Sphere hit、Baseline Foot Goal和Future Landing
- **AND** MUST不使用Ramp法线或Ramp高度替代可见踏面

#### Scenario: Body沿真实Ground台阶上楼

- **WHEN** KCC Body通过进入Collision Artifact的离散`Ground`阶梯且Foot Placement查询同一PhysicsScene
- **THEN** Foot Placement MUST从同一组Ground Collider生成Lyra current Sphere hit、Baseline Foot Goal和Future Landing
- **AND** MUST不要求Presentation专用Collider或读取KCC Step phase

#### Scenario: Foot Placement Profile包含CharacterTraversal

- **WHEN** Profile配置会让Foot查询命中Gameplay Ramp
- **THEN** Profile或楼梯组合校验 MUST失败并报告冲突Layer
- **AND** Runtime MUST不以踏面优先级或Collider名称消解重叠命中

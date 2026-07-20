## ADDED Requirements

### Requirement: Deterministic KCC必须约束已积分位移而不私有拥有重力

Deterministic KCC MUST消费Fixed Body Motion Integrator产生的完整XYZ `CharacterMotionRequest`，继续以唯一Motor执行continuous cast、slide、step、Grounding与Ground Snap，并准确返回applied displacement、稳定Grounded及方向性Above/Below。只有现有`IsStableOnGround`语义可以映射为portable Grounded；`FoundAnyGround`、非稳定陡坡或普通下方接触 MUST不冒充稳定Grounded。KCC Motor、query kernel、Solver Definition与collision artifact MUST不保存GravityAcceleration、MaximumFallSpeed或私有VerticalVelocity积分规则。Deterministic KCC只有在调用Fixed唯一Body Motion Finalizer提交VerticalVelocity后才能声明`AirborneVerticalMotion`。

#### Scenario: Fixed Actor离开悬崖

- **WHEN** Prepare产生向下gravity delta且KCC找不到稳定支持面
- **THEN** KCC MUST报告Airborne而不执行跨断崖Ground Snap
- **AND** Fixed Finalizer MUST保存candidate VerticalVelocity
- **AND** KCC MUST不自行再次应用Gravity

#### Scenario: Fixed Actor向上撞顶

- **WHEN** 最终request向上且continuous capsule query命中上方阻挡
- **THEN** KCC MUST报告Above并返回受约束的applied displacement
- **AND** Fixed Finalizer MUST按统一规则清零向上VerticalVelocity

#### Scenario: Fixed Actor接触非稳定陡坡

- **WHEN** capsule命中不满足稳定坡度或支撑条件的下方表面
- **THEN** KCC MUST保持`Grounded=false`
- **AND** Fixed Finalizer MUST保存candidate向下VerticalVelocity
- **AND** 后续Tick MUST继续由同一重力积分驱动沿表面下落或滑动

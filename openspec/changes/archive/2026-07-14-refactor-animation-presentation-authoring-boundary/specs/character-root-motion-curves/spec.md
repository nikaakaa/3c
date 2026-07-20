# character-root-motion-curves Specification

## ADDED Requirements

### Requirement: 动画表现淡入淡出不得成为 Root Motion 路径

Animancer transition、state blending 与 FadeGroup MUST只影响 visual animation pose 和 layer/state weight，MUST不从动画混合结果推导 gameplay 位移，不提交 motion contribution，也 MUST不修改逻辑 Transform。动画派生位移仍 MUST由显式 MotionCurveTrack、MotionResolver 与 CharacterMotionStage 处理。

#### Scenario: 闪避 Fade 与 MotionCurve 同时运行

- **WHEN** 闪避 Timeline 的 MotionCurveTrack 提交逻辑 motion
- **AND** Animancer 对动画 state 执行 transition/fade
- **THEN** CharacterMotionStage MUST只应用正式 motion contribution
- **AND** 动画 fade MUST只改变 visual pose 权重
- **AND** 角色逻辑位移 MUST不被重复计算

## REMOVED Requirements

### Requirement: Animation Inertialization 不得成为 Root Motion 路径

**Reason**: 项目自有 Inertialization output job 已删除；Root Motion 边界应约束正式 Animancer 表现混合，而不是保留不存在的执行路径。

#### Scenario: 删除旧 output job 合同

- **WHEN** 动画 transition 执行
- **THEN** 系统 MUST不创建项目自有 inertialization job
- **AND** Root Motion MUST继续只来自正式 motion 管线

## MODIFIED Requirements

### Requirement: Root Motion 通过角色 motion 管线应用

Root Motion curve delta MUST作为原始动画派生位移进入Kernel Evaluate的统一contribution resolve。已解析channel MAY由Operation Set声明的正式Motion Modifier在WorldSolver之前修正，再生成portable WorldRequest并由Session WorldSolver batch产生actual body result。MotionCurve、Modifier与Timeline MUST不直接写Transform或调用CharacterController；AnimationClip、Animancer与Presentation MUST不成为修正来源。

#### Scenario: Root Motion 被墙阻挡

- **WHEN** MotionCurve及其正式Modifier请求的位移穿过墙面
- **THEN** WorldSolver actual result MUST决定WorldSimulationState

#### Scenario: Root Motion 绑定目标修正

- **WHEN** MotionWarp显式引用一个Action MotionCurve
- **THEN** MotionCurve MUST仍提供原始累计位移事实
- **AND** MotionWarp MUST只在channel resolve之后修正其结果
- **AND** 编译产物 MUST保留两者独立source identity

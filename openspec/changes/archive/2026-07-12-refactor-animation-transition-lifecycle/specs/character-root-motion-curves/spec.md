## ADDED Requirements

### Requirement: Animation Inertialization 不得成为 Root Motion 路径

Animation inertialization output job MUST 只修正 visual skeleton 的 local pose，MUST 排除 Animator/visual root 的 root motion 通道。Job 的 root motion 处理 MUST 透传上游结果，MUST NOT 从 pose offset 推导位移，MUST NOT 提交 motion contribution，也 MUST NOT 修改逻辑 Transform。动画派生位移仍 MUST 由显式 MotionCurveTrack、MotionResolver 和 CharacterMotionStage 处理。

#### Scenario: 闪避 Inertialization 与 MotionCurve 同时运行

- **WHEN** 闪避 Timeline 的 MotionCurveTrack 提交逻辑 motion
- **AND** 动画 transition 使用 Inertialization
- **THEN** CharacterMotionStage MUST 只应用正式 motion contribution
- **AND** output job MUST 只平滑 visual local pose
- **AND** 角色逻辑位移 MUST 不被重复计算

#### Scenario: output job 处理 root motion

- **WHEN** Unity animation graph 调用 inertialization job 的 root motion 处理
- **THEN** job MUST 透传上游 root motion
- **AND** job MUST NOT 施加惯性 position/rotation offset 到 root motion 通道

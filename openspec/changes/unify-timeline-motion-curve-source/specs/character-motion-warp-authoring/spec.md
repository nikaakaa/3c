## ADDED Requirements

### Requirement: MotionWarp 必须通过源 Clip 的正式数据段读取运动

MotionWarp MUST 继续通过稳定身份绑定同一 Timeline owner 内的具体 MotionCurveClip，不改为直接绑定共享 RootMotionCurveAsset。源窗口累计位移与 yaw MUST 经该 Clip 的正式源引用、区间、求值模式和时间映射读取，不直接读取旧嵌入曲线。现行 Action/Override/ActorLocal、单位权重、无 Ease、窗口范围、不重叠和非零源位移/yaw 要求 MUST 保留。共享源不使多个 Clip 合并，Gameplay Runtime MUST 沿既有 compiled Motion Modifier 路径消费。

#### Scenario: 同一曲线源在 Timeline 使用两次

- **WHEN** 两个 MotionCurveClip 引用相同源但使用不同区间，一个 Warp 绑定其中之一
- **THEN** Warp MUST 仅使用被绑定 Clip 的窗口和映射
- **AND** MUST 不按共享资产身份、时间重叠或列表顺序改绑另一片段

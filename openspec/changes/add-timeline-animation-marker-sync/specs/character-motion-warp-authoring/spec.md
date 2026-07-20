## MODIFIED Requirements

### Requirement: MotionWarp 修正必须使用 canonical 累计进度曲线

MotionWarpClip MUST分别保存position与yaw的normalized cumulative progress curve。每条curve MUST只包含有限值，时间域 MUST为`[0,1]`，首值 MUST为0，末值 MUST为1，并且 MUST单调不下降。Position Progress与Yaw Progress MUST作为两个显式typed Curve Channel进入Timeline Curve Editor，使用`[0,1]` bounded value domain并通过MotionWarp Clip正式mutation API修改。Timeline Curve Catalog与Agent MUST复用MotionWarp唯一校验，不得在UI中静默Clamp、补端点、重排非法key或生成默认曲线。Runtime MUST继续使用相邻Tick累计采样值之差计算本Tick修正，不得把EaseIn、EaseOut、AnimationTrack weight或Generic Curve Runtime作为第二套Gameplay修正权重。

#### Scenario: 旋转早于位置完成

- **WHEN** yaw progress curve前半段增长更快而position progress curve后半段增长更快
- **THEN** 角色 MUST先完成更多yaw修正再完成更多position修正
- **AND** 两者最终累计修正 MUST分别到达已计算总量

#### Scenario: 在Timeline编辑进度曲线

- **WHEN** 作者在CURVES分组选择Yaw Progress并修改weighted tangent
- **THEN** Editor MUST保存完整Keyframe与wrap mode
- **AND** MotionWarp validator MUST重新校验端点、范围和单调性
- **AND** 非法结果 MUST拒绝整个mutation而不是自动修复

#### Scenario: Agent修改MotionWarp进度

- **WHEN** Agent v14通过registered ChannelId提交Yaw Progress完整curve
- **THEN** handler MUST调用同一MotionWarp mutation与validator
- **AND** MUST不使用MotionWarp专用第二curve patch入口

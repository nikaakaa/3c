## ADDED Requirements

### Requirement: MotionCurve Clip控制曲线必须进入typed Curve Channel Catalog

Timeline中的MotionCurve Clip MUST继续唯一保存Weight、Position X/Y/Z、Yaw与Ease In/Out曲线，并 MUST通过显式registered ChannelId进入同一个Timeline Curve Editor。Position channel MUST声明meter单位与unbounded value domain，Yaw MUST声明degree单位与unbounded value domain，Weight和Ease MUST声明`[0,1]` bounded domain。Curve Editor MUST只调用MotionCurve Clip正式mutation API；Compiler MUST继续把这些曲线降低为既有portable Program constant与MotionCurve operation，不得新增Generic Curve Runtime、第二份inline curve或Presentation motion路径。

#### Scenario: 在Timeline编辑Position Z

- **WHEN** 作者展开MotionCurve Clip的Position Z channel并移动key
- **THEN** Curve Editor MUST按Clip-local time与meter value显示和提交完整curve
- **AND** Semantic Compiler MUST沿既有MotionCurve operation重新编译该curve
- **AND** Animation、Marker Sync与Presentation MUST不成为该位移的第二消费者

#### Scenario: MotionCurve引用RootMotionCurveAsset

- **WHEN** MotionCurve作者数据来自正式RootMotionCurveAsset
- **THEN** RootMotionCurveAsset MUST继续是外部烘焙source
- **AND** Timeline Curve Catalog MUST不复制该资产全部曲线形成第二份authoring

#### Scenario: Position curve超出权重范围

- **WHEN** Position X key值大于1或小于0
- **THEN** Curve Editor MUST按unbounded meter domain显示与编辑
- **AND** MUST不Clamp到`[0,1]`

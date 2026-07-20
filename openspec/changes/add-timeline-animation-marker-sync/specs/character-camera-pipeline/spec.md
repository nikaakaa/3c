## ADDED Requirements

### Requirement: Camera Timeline控制曲线必须作为typed Curve Channel编辑

CameraStateClip与CameraResponseClip的Weight、Ease In与Ease Out曲线 MUST通过显式registered ChannelId进入Timeline Curve Editor。每条curve MUST继续由对应Camera Clip唯一拥有，使用ClipNormalized时间域和`[0,1]` bounded value domain，并通过Camera Clip正式mutation API原子替换。Camera Runtime MUST继续只消费既有compiled request与presentation policy；Curve Editor、Catalog与Agent MUST不直接控制Cinemachine、virtual camera priority、Camera Transform或创建第二个Camera influence stack。

#### Scenario: 编辑Camera Ease In

- **WHEN** 作者在Camera Track展开Ease In channel并移动key
- **THEN** Editor MUST在该Camera Clip起止帧内显示并提交完整curve
- **AND** Camera compile/presentation链 MUST沿既有入口消费修改结果

#### Scenario: Camera与Animation曲线同时展开

- **WHEN** Timeline同时显示Camera Track与Animation Track的CURVES分组
- **THEN** 两者 MUST复用同一Curve Lane交互与frame geometry
- **AND** 每条curve MUST继续使用自己的owner mutation、ChannelId和runtime consumer

#### Scenario: Curve Editor尝试直接控制Cinemachine

- **WHEN** 作者修改Camera Weight或Ease channel
- **THEN** Curve Editor MUST只修改Camera Clip authoring
- **AND** MUST不直接访问Cinemachine组件或写Camera Transform

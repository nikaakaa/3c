# character-animation-foot-analysis-artifact Specification

## MODIFIED Requirements

### Requirement: Definition Build必须精确消费Artifact并发布Projection

Definition Build MUST收集全部可达Presentation Pose source binding与有限Action Timeline/Track/Clip binding，按精确`AnimationClip + Analysis Source` identity校验或生成artifact，再把feature按stable Pose source或Action clip binding嵌入CharacterPresentationProjection。相同AnimationClip MAY复用一次artifact读取，但每个binding MUST保持独立presentation identity。任一required artifact无效 MUST阻止本次Projection发布。

#### Scenario: Locomotion Pose source artifact已提前生成

- **WHEN** Run Pose source所需artifact为Ready且identity/hash匹配
- **THEN** Definition Build MAY复用payload而不重新采样AnimationClip
- **AND** MUST验证其与Pose source、Rig、Analysis Source和Calibration匹配

#### Scenario: Action Timeline artifact已提前生成

- **WHEN** Attack clip artifact为Ready
- **THEN** Build MUST按Timeline/Track/Clip stable binding嵌入feature
- **AND** MUST不把它迁入Locomotion Pose source catalog

### Requirement: Player Runtime必须只消费Projection

SequencePlayer、BlendSpacePlayer、SelectedPosePlayer、AnimationSlot与FootPlacement Runtime MUST只从匹配的CharacterPresentationProjection读取生成feature。Runtime MUST不读取Library artifact、Analysis Source、Sampling Rig、AssetDatabase或Editor Analyzer，也不得在Pose source或Action feature缺失时即时分析AnimationClip。

#### Scenario: Library缓存被删除

- **WHEN** Editor Library artifact在Player构建后被删除
- **THEN** 已发布Player MUST继续只使用Projection运行
- **AND** Runtime行为 MUST不依赖Editor cache存在


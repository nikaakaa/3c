## MODIFIED Requirements

### Requirement: Foot Analysis必须由正式Projection Build生成

单AnimationClip feature MUST先由正式Artifact Builder按精确AnimationClip、Analysis Source、Sampling Rig、Calibration和算法输入生成Editor-only artifact。Definition Build MUST收集全部可达stable clip binding，精确校验或生成所需artifact，再把feature写入对应Projection binding。Projection发布仍 MUST发生在正式Build Transaction中；artifact本身不得成为Runtime或作者真相。

#### Scenario: 同一AnimationClip被多个producer引用

- **WHEN** 多个stable clip binding使用相同AnimationClip和Analysis Source
- **THEN** Build MAY复用同一artifact payload
- **AND** 每个Projection binding MUST仍按自己的Timeline/Track/Clip identity保存精确映射

#### Scenario: 单Clip预分析

- **WHEN** 作者在Timeline工具中提前生成一个clip artifact
- **THEN** 该操作 MUST不发布Program或Projection
- **AND** 后续Definition Build MUST重新校验artifact后才能消费

### Requirement: CharacterAnimationPresentationProfile Inspector 必须是唯一 Presentation 配置入口

系统 MUST在CharacterAnimationPresentationProfile Inspector中唯一编辑Layer catalog、TransitionLibrary、producer binding、Foot Analysis Mode和Analysis Source GUID。Timeline Editor继续唯一编辑producer-local Clip、Marker与registered Curve；Timeline MAY通过领域tool provider临时接收Profile的精确Analysis Source作为面板初值，但 MUST不保存角色级Source或Projection配置。Graph、StateMachine和Timeline MUST不复制Profile作者数据。

#### Scenario: 从Profile打开Timeline Analysis

- **WHEN** 作者从精确Profile上下文打开Timeline并选择AnimationClip
- **THEN** Analysis provider MAY把该Profile的Source作为显式初始选择
- **AND** Timeline资产 MUST不因打开或分析而变脏

#### Scenario: shared Timeline用于不同角色

- **WHEN** 两个Profile使用同一shared Timeline但不同Analysis Source
- **THEN** 各自 MUST生成不同artifact identity与Projection
- **AND** shared Timeline MUST不保存任一角色的Analysis Source

## MODIFIED Requirements

### Requirement: Gameplay Timeline只能提交有限Action播放事实

Compiler MUST把有限Action Timeline AnimationTrack降低为稳定producer binding、直接AnimationClip计划、committed sample contract与source-local Clip Weight计划。SimulationTick MUST只推进Gameplay Timeline logic time并提交Select、Sample、Complete或Release command；PresentationFrame sampler MUST按committed raw sample、cycle、PlaybackMode和source-local clip weight生成Action playback frame与typed parameter page。Timeline MUST不解析Locomotion Phase、不创建Pose、transition、Bone Mask或IK plan。持续Idle、Walk、Run、Start、Stop与Turn MUST不依赖Gameplay Timeline或AnimationChannel。

#### Scenario: Attack Timeline同时产生Window与动画

- **WHEN** Attack Timeline在一个SimulationTick推进Window并选择直接AnimationClip producer
- **THEN** Window MUST进入Gameplay事实链
- **AND** Action playback command MUST进入Presentation-owned inbox
- **AND** Timeline MUST不创建Sequence或Marker binding

#### Scenario: Locomotion持续播放

- **WHEN** 角色保持Run
- **THEN** PoseStateMachine的state-local provider MUST推进Run source
- **AND** Program MUST不创建Run Timeline producer

## REMOVED Requirements

### Requirement: Marker effective time必须由source-local计划解析

该Requirement被删除。有限Action不再拥有Marker Sync；Locomotion source handoff只消费Projection编译的Phase endpoint与relation计划。

#### Scenario: 旧Marker relation进入Action运行链

- **WHEN** Timeline producer、Action source usage或Projection仍包含Marker relation
- **THEN** schema或Projection校验 MUST失败
- **AND** Runtime MUST不把它转换为Locomotion Phase

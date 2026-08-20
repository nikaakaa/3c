## ADDED Requirements

### Requirement: Blend Stack必须只发布source usage而不得拥有Phase relation

Blend Stack MUST在source采样前声明当前与尚未exact release的live `PlayerSourceUsage`，并在source退出时发布精确release。PoseState transition具有compiled `AnimationPhaseRelationPlan`时，Stack MUST只消费source-local endpoint已经解析的effective sample page；没有relation时 MUST使用各source raw visual time。Stack MUST不读取Phase Curve、Profile Group或Clip identity，不选择clock carrier、不建立relation、不执行forward/inverse映射，也 MUST不按blend weight推导同步方向。

#### Scenario: Walk向Run CrossFade并同步Phase

- **WHEN** PoseState edge具有合法Phase relation且usage同时包含Walk与Run
- **THEN** source-local endpoint MUST独立解析两者effective time
- **AND** Blend Stack MUST独立计算两者CrossFade与per-bone weight

#### Scenario: 同一Stack没有Phase relation

- **WHEN** source usage不属于共同Locomotion Sync Group
- **THEN** Stack MUST让每个live source按raw visual time采样
- **AND** Runtime MUST不因Clip显示名或blend weight后台建立relation

## REMOVED Requirements

### Requirement: Blend Stack必须只发布source usage而不得拥有Marker Sync

该Requirement被source-local Phase endpoint合同取代；Blend Stack不再连接或消费MarkerSync节点。

#### Scenario: 旧MarkerSync连接进入Build

- **WHEN** Graph或Projection仍把MarkerSync输出连接到Blend Stack
- **THEN** capability或schema校验 MUST失败
- **AND** MUST不转换为Phase relation

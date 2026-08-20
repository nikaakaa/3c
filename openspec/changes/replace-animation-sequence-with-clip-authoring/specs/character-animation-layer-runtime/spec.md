## ADDED Requirements

### Requirement: Locomotion Phase映射必须编入source-local计划

Projection MUST把Locomotion Phase forward/inverse plan与可达relation编入对应source-local计划。Runtime MUST只用leader raw time、compiled forward phase、follower continuation cycle和compiled inverse plan求target effective Clip time。Runtime MUST不读取AnimationCurve、Profile、Foot Analysis artifact，不搜索Pose，也不得回退normalized time或旧Marker mapping。

#### Scenario: RunLoop接任MovingTurn

- **WHEN** MovingTurn到RunLoop relation具有合法Phase计划
- **THEN** RunLoop Player MUST按Phase inverse得到effective time并采样Pose与Foot Feature
- **AND** MovingTurn与RunLoop各自raw clock MUST保持不变

## MODIFIED Requirements

### Requirement: 基础Pose必须由正式state-local source输出

Base Pose、Idle、Move、Start、Stop、Turn与可选Motion Matching MUST来自Pose Graph中PoseStateMachine选择的ClipPlayer、BlendSpacePlayer或SelectedPosePlayer provider。Gameplay Program、Timeline与Action Lifecycle MUST不提供持续BaseLocomotion producer。Required source缺失或Clip Phase relation无效时 MUST报告typed Pending或Invalid，不得回退旧Sequence、默认Idle、bind pose或历史sample。

#### Scenario: Clip binding失效

- **WHEN** active PoseState的Clip Binding与Projection identity不一致
- **THEN** provider MUST发布Invalid并阻止正式Pose提交
- **AND** MUST不继续使用旧Sequence或上一帧source

## REMOVED Requirements

### Requirement: Marker同步必须编入对应source-local计划

该Requirement被删除；Marker segment、occurrence和relation cursor由Locomotion Phase计划完整取代。

#### Scenario: Runtime发现Marker relation payload

- **WHEN** Runtime加载包含Marker mapping的旧Projection
- **THEN** Projection schema validation MUST失败

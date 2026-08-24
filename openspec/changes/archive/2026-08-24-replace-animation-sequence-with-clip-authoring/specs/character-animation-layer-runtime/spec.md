## ADDED Requirements

### Requirement: Locomotion Phase映射必须编入source-local计划

Projection MUST把Locomotion Phase forward/inverse plan与可达relation编入对应source-local计划。每个relation plan MUST包含TransitionId、编译期固定leader、两侧秒域coverage与validation identity；Runtime MUST用`RelationIdentity + TransitionId + TransitionGeneration`建立唯一relation generation，并只用leader raw time、compiled forward phase、follower continuation cycle和compiled inverse plan求target effective Clip time。Runtime MUST不读取AnimationCurve、Profile、Foot Analysis artifact，不搜索Pose，也不得回退normalized time或旧Marker mapping。

#### Scenario: RunLoop接任MovingTurn

- **WHEN** MovingTurn到RunLoop relation具有合法Phase计划
- **THEN** RunLoop Player MUST按Phase inverse得到effective time并采样Pose与Foot Feature
- **AND** MovingTurn与RunLoop各自raw clock MUST保持不变

### Requirement: Locomotion Phase relation必须服从Transition generation与Player continuation

Compiler MUST按固定规则选择leader：两侧clock authority不同时`CommittedMovement`优先，同authority时outgoing source优先；候选必须覆盖完整Blend可见窗口，优先候选不足时 MAY选择另一侧，两侧都不足时 MUST Build失败。leader在一个relation generation内 MUST不按weight、sample、clock进度或有限端点动态变化。Transition replacement MUST先release旧generation再建立新generation；反向edge MUST使用自己的plan与generation。正常release MUST把最后effective time建立为follower自己的continuation anchor并删除relation generation；AlwaysResetOnEntry、branch replacement、Projection replacement、Presentation Reset与Dispose MUST清除不合法continuation和relation state。

#### Scenario: 同authority的Turn进入RunLoop

- **WHEN** MovingTurn与RunLoop都使用CommittedMovement且MovingTurn coverage覆盖完整Blend窗口
- **THEN** Compiler MUST把outgoing MovingTurn固定为该edge relation的leader
- **AND** Runtime MUST不因RunLoop weight超过MovingTurn而换leader

#### Scenario: Transition在Blend中被替换

- **WHEN** 当前relation generation尚未完成时更高优先级Transition替换目标State
- **THEN** Runtime MUST按旧edge release规则关闭旧generation，再为新TransitionGeneration建立新relation
- **AND** MUST不复用旧follower cycle、effective anchor或relation cursor

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

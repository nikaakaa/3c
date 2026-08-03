## ADDED Requirements

### Requirement: 持续Sequence Pose Source必须拥有完整时间编辑表面

Presentation Profile MUST为每个持续Sequence Pose Source提供唯一`Pose Source Editor`。该表面 MUST使用正式时间尺、Sync Marker lane、typed Curve lane、Foot Analysis候选与Preview，并支持marker新增/删除/拖动、curve key多选/框选/精确值/切线/weighted tangent/复制粘贴和单次Undo事务。编辑结果 MUST写回该Profile binding，不得创建Timeline、Clip副本或第二curve资产。GUID、revision与hash MUST只出现在Diagnostics。

#### Scenario: 作者精确调整Run曲线

- **WHEN** 作者在Run Pose Source Editor框选多个Foot Placement Weight key并编辑weighted tangent
- **THEN** 唯一Profile binding typed curve MUST原子更新
- **AND** Run MUST不需要Timeline或普通Inspector CurveField

#### Scenario: 作者应用左脚接触候选

- **WHEN** 当前Foot Analysis artifact与source输入identity一致
- **THEN** 作者 MAY把选中的Left Foot候选显式应用为该binding的marker
- **AND** generated artifact MUST保持只读

## MODIFIED Requirements

### Requirement: Foot Analysis Source必须是显式可验证的表现作者输入

每个需要Foot Analysis的Presentation Profile MUST显式引用`CharacterFootPlacementAnalysisSource`。该资产 MUST显式绑定一个正式Rig Definition v3、精确Sampling Rig prefab与同identity Rig Calibration；不得从Humanoid avatar、当前Scene、Runtime Prefab旧Foot rig、资源目录或命名猜测。Profile、Pose Source、Action producer、Timeline与AnimationClip MUST不复制Sampling Rig、Rig Mapping或Calibration引用。

#### Scenario: Run Pose source缺少Analysis Source

- **WHEN** Run source需要Foot Feature但Profile没有完整Rig v3、Sampling Rig与Calibration Analysis Source
- **THEN** Validation MUST明确报告不可分析
- **AND** MUST不选用任意Corin prefab、默认Rig或旧artifact

### Requirement: CharacterAnimationPresentationProfile Inspector必须是唯一Presentation配置入口

Profile Inspector MUST唯一编辑Pose Graph、Pose source binding、Blend Policy、Inertialization Policy、Rig Definition、有限Action producer source binding、Foot Analysis Mode与Analysis Source。持续Sequence source MUST从Profile进入Pose Source Editor编辑Clip、marker、SyncRole、typed curve、analysis与preview；BlendSpace与Motion Matching MUST从同一binding入口导航到各自正式编辑器。Timeline Editor继续唯一编辑Action producer-local Clip、Window、Motion、Cue、Timeline marker与curve。系统 MUST不要求为持续source创建Timeline，也 MUST不在普通Inspector保留marker文本或CurveField写入口。

#### Scenario: 从Profile打开Timeline Analysis

- **WHEN** 作者从精确Profile上下文打开有限Action Timeline并选择AnimationClip
- **THEN** Analysis provider MAY把该Profile的Source作为显式初始选择
- **AND** Timeline资产 MUST不因打开或分析而变脏

#### Scenario: shared Timeline用于不同角色

- **WHEN** 两个Profile使用同一shared Timeline但不同Analysis Source
- **THEN** 各自 MUST生成不同artifact identity与Projection
- **AND** shared Timeline MUST不保存任一角色的Analysis Source

#### Scenario: 从Profile打开Run source

- **WHEN** 作者选择持续Sequence Run binding并执行Open Source
- **THEN** Workspace MUST打开Pose Source Editor并保留Definition/Profile/source精确上下文
- **AND** MUST不打开或创建Run Timeline

### Requirement: Animation Marker Sync 必须由实际source owner唯一拥有

有限Action producer的Marker Sync数据 MUST继续由对应Timeline AnimationTrack唯一拥有。持续Pose source的Marker Sync数据 MUST由Profile中的对应Pose source binding唯一拥有。两类owner都 MUST保存明确None或MarkerGroup、SyncGroupId、Finite/Cyclic topology、SyncRole与ordered Point Marker，并通过共享typed marker schema、validator和时间编辑模块修改。它们 MUST不互相复制，也不得把marker写入Gameplay StateMachine、Pose transition、Pose transition Rule、Blackboard、ActionProfile、FootPhase资产或独立Pose Graph MarkerSync节点。PoseState Compiler MUST只根据Transition两侧State的唯一Sequence或BlendSpace source binding推导可选同步计划，不得要求Transition作者重复选择同步模式。

#### Scenario: 编辑Attack marker

- **WHEN** 作者修改Attack1的finite marker
- **THEN** Timeline Editor MUST成为唯一写入口
- **AND** Profile MUST不复制该marker

#### Scenario: 编辑Run marker

- **WHEN** 作者修改Run Pose source的Locomotion.Gait marker
- **THEN** Profile Pose Source Editor MUST成为唯一写入口
- **AND** Timeline Editor MUST不创建RunLoop Track副本

#### Scenario: source明确不参与同步

- **WHEN** 作者把Action track或Pose source配置为`None`
- **THEN** 对应owner MUST原子清空SyncGroupId、topology、SyncRole和markers
- **AND** Runtime MUST保持该source的原始表现时间

### Requirement: Animation Clip控制曲线必须作为typed Curve Channel编辑

有限Action Animation Clip MUST继续由Timeline Clip唯一保存Weight、Ease、Foot Placement Weight等已注册typed Curve Channel。持续Sequence Pose Source MUST只在Profile binding保存source-local Foot Placement Weight typed curve，并由Pose Source Editor复用正式Curve模块编辑；State transition的blend curve MUST继续由Transition Policy拥有。Timeline Clip、Pose source与Transition Policy MUST不双写同一curve，generated每脚feature MUST不成为可编辑Curve Channel，普通Inspector`CurveField` MUST不成为正式写入口。

#### Scenario: 编辑Attack Foot Placement Weight

- **WHEN** 作者展开Attack Clip曲线
- **THEN** Timeline Curve Editor MUST编辑该Clip的typed channel
- **AND** Profile Pose source MUST不保存副本

#### Scenario: 编辑Run Foot Placement Weight

- **WHEN** 作者选择Run Pose source
- **THEN** Pose Source Editor MUST编辑source-local typed curve及完整key/tangent数据
- **AND** MUST不创建Run Timeline Clip

#### Scenario: AnimationClip内容变化

- **WHEN** AnimationClip imported content revision改变但作者曲线未改变
- **THEN** Projection Foot Analysis MUST变为Stale
- **AND** Profile或Timeline资产 MUST不被自动写入任何生成key

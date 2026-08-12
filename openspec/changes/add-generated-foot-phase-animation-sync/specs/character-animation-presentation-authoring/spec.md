## MODIFIED Requirements

### Requirement: Animation Marker Sync 必须由实际source owner唯一拥有

有限Action producer的Marker Sync数据 MUST继续由对应Timeline AnimationTrack唯一拥有。持续Pose source的Marker Sync数据 MUST由Profile中的对应Pose source binding唯一拥有。两类owner都 MUST保存明确None或MarkerGroup；MarkerGroup MUST保存canonical SyncGroupId、Finite/Cyclic topology、SyncRole、ordered Point Marker和明确`AnimationSyncTimeMapping`。Time Mapping MUST只接受`MarkerSegmentFraction`或`GeneratedFootPhase`，不得保留`Unspecified`。relation两侧 MUST使用相同canonical group和Time Mapping。

Marker与Time Mapping MUST不在两类owner之间复制，也不得写入Gameplay StateMachine、Pose transition、Pose transition Rule、Blackboard、ActionProfile、独立FootPhase资产或Pose Graph MarkerSync节点。PoseState Compiler MUST只根据Transition两侧State的唯一Sequence或BlendSpace source binding推导可选同步计划，不得要求Transition作者重复选择同步模式或Time Mapping。`GeneratedFootPhase`只引用现有Foot Analysis Source并由Build生成Projection plan，不得创建pair table作者资产。

#### Scenario: 编辑Attack marker

- **WHEN** 作者修改Attack1的finite marker和明确MarkerSegmentFraction策略
- **THEN** Timeline Editor MUST成为唯一写入口
- **AND** Profile MUST不复制该marker或Time Mapping

#### Scenario: 编辑Run marker

- **WHEN** 作者修改Run Pose source的Locomotion.Gait marker并选择GeneratedFootPhase
- **THEN** Profile Pose source editor MUST成为唯一写入口
- **AND** Timeline Editor MUST不创建RunLoop Track或FootPhase副本

#### Scenario: source明确不参与同步

- **WHEN** 作者把Action track或Pose source配置为`None`
- **THEN** 对应owner MUST原子清空Time Mapping、SyncGroupId、topology、SyncRole和markers
- **AND** Runtime MUST保持该source的原始表现时间

#### Scenario: 同组Time Mapping不一致

- **WHEN** 一次可达relation的leader选择GeneratedFootPhase而follower选择MarkerSegmentFraction
- **THEN** Compiler MUST报告精确owner与策略冲突
- **AND** MUST不选择任一侧策略或建立双计划

### Requirement: Marker Group 必须在 Projection 构建前完整校验

Projection Build MUST分别校验Action AnimationTrack和Presentation Pose source的duration、marker identity、frame/time、有向pair、topology、role、Time Mapping、resource coverage与共同可达SyncGroup pair集合。Pose source使用AnimationClip duration与Profile binding；Action producer使用Timeline duration与Track binding。`GeneratedFootPhase`还 MUST校验两侧精确Foot Analysis artifact、同步描述、算法身份和全部可达occurrence warp plan。任一缺失、跨owner冲突、warp非单调或容量超限 MUST阻止发布；MUST不回退normalized time或MarkerSegmentFraction。

#### Scenario: Walk与Run使用不同时序

- **WHEN** WalkLoop和RunLoop属于同一组、拥有相同有向marker pair集合并共同选择GeneratedFootPhase
- **AND** 两个producer的marker frame和segment时长不同
- **THEN** Compiler MUST接受各自真实marker occurrence并编译双脚warp plan
- **AND** Projection MUST保存各producer时间与relation-local mapping identity

#### Scenario: 有限序列重复MarkerId

- **WHEN** Finite producer使用`LeftPlant -> RightPlant -> LeftPlant`覆盖完整one-shot
- **THEN** Validator MUST接受重复LeftPlant语义id
- **AND** 两个LeftPlant occurrence MUST拥有不同稳定AuthoringId、frame与精确warp occurrence身份

#### Scenario: 同组缺少有向segment

- **WHEN** 同组某target producer缺少其它producer可能成为source的有向marker pair
- **THEN** Compiler MUST报告group compatibility错误
- **AND** MUST不生成依赖运行时normalized-time或Time Mapping fallback的Projection

#### Scenario: marker覆盖区存在无输出空洞

- **WHEN** marker映射可能落入AnimationTrack没有任何合法clip sample的区间
- **THEN** Validator MUST报告output coverage错误
- **AND** MUST不依赖RequireOutput、隐藏Idle或Animancer自动同步填补

#### Scenario: GeneratedFootPhase描述缺失

- **WHEN** owner选择GeneratedFootPhase但任一artifact没有当前algorithm的同步描述
- **THEN** Projection Build MUST报告Missing或Stale并阻止发布
- **AND** MUST不只使用现有Plant Confidence曲线在Runtime临时对齐

### Requirement: Presentation Projection 必须保存规范化 Marker Sync 映射

Projection Compiler MUST把Action producer和Presentation Pose source的同步模式、canonical SyncGroupId、topology、role、Time Mapping、duration、ordered marker与有向pair occurrence索引编入Projection。`MarkerSegmentFraction`计划 MUST明确保存线性策略；`GeneratedFootPhase`计划 MUST额外保存algorithm identity、两侧artifact/source identity与固定容量relation-local warp segment table。Action mapping MUST关联producer binding与AnimationSlot可达pair；Pose source mapping MUST关联Projection-local dense source index、typed source plan与State source consumer。Blend Space GeneratedFootPhase MUST引用同格式Reference-to-Sample warp plan。全部映射 MUST只服务表现采样，不进入Gameplay Program ABI、State codec、Snapshot或Network协议。

#### Scenario: GeneratedFootPhase改变producer表现时间

- **WHEN** Runtime在Marker区间内把leader fraction映射为不同的warped follower fraction
- **THEN** incoming producer的Pose、Foot Analysis与Foot Placement Weight MUST按新的effective sample time求值
- **AND** FootGrounding MUST不读取MarkerId、warp knot或segment名称作为plant事实

#### Scenario: AnimationClip或Calibration变化

- **WHEN** 任一artifact、同步描述、warp algorithm或其它生成输入revision改变
- **THEN** ProjectionRevision MUST更新且旧Projection MUST被拒绝
- **AND** Float32与Fixed Gameplay operation语义 MUST保持不变

#### Scenario: Runtime加载GeneratedFootPhase计划

- **WHEN** Ready Projection包含一个GeneratedFootPhase relation
- **THEN** Runtime MUST只读取Projection中的dense warp plan和source clock
- **AND** MUST不读取Library artifact、Analysis Source、Profile或AnimationClip重新编译

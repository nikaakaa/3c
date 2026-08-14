## MODIFIED Requirements

### Requirement: Marker时间映射必须属于source-local采样计划

Marker topology、SyncGroup、SyncRole、Time Mapping与marker occurrence MUST来自Presentation source binding或Action producer binding。PoseState source同步 MUST由Compiler根据Transition两侧State唯一同步候选、共同canonical MarkerGroup与一致Time Mapping生成具体Source Sync Plan；Transition不得保存同步开关、Time Mapping或Foot Phase pair配置。Action同步 MUST由具体AnimationSlot route和Action source usage拥有。

Runtime MUST在source采样前生成effective sample，并在leader仍有相位覆盖的共同可见期间持续定位有向Marker pair。`MarkerSegmentFraction` MUST使用明确线性比例；`GeneratedFootPhase` MUST根据已选择leader/follower occurrence查找Projection固定warp plan并求值follower fraction。finite leader到达最后coverage时 MUST只提交一次终点映射并让follower从continuation anchor连续推进，不得重复固定target sample。Pose Graph MUST不序列化独立MarkerSync节点，Runtime与Preview MUST不按同名State、clip名称、Action名称、weight、当前骨骼或IK结果建立relation或重新搜索时间。

#### Scenario: Walk State切换Run State

- **WHEN** Transition两侧State唯一同步候选属于同一canonical SyncGroup并共同选择GeneratedFootPhase
- **THEN** Source Sync Plan MUST持续把leader segment fraction通过编译warp映射到target sample
- **AND** MUST不创建BaseLocomotion Gameplay Selection或等待脚接触

#### Scenario: Transition两侧没有共同同步组

- **WHEN** 两侧source binding未声明同一canonical MarkerGroup
- **THEN** 两侧Player MUST使用各自raw source time
- **AND** Compiler MUST生成None计划

#### Scenario: Transition两侧策略不同

- **WHEN** 两侧MarkerGroup相同但Time Mapping不同
- **THEN** Compiler MUST拒绝该Transition source relation
- **AND** MUST不生成两个竞争计划或按target策略覆盖source

#### Scenario: Action source同步数据损坏

- **WHEN** Slot route要求同步但binding或compiled warp缺少合法segment、duration、role、policy或occurrence
- **THEN** Runtime MUST报告稳定typed failure
- **AND** MUST不回退normalized time、linear策略或Animancer自动同步

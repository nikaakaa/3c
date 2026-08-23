## MODIFIED Requirements

### Requirement: Pose Graph必须唯一表达完整表现拓扑

`CharacterAnimationPresentationProfile`引用的Pose Graph MUST唯一表达`ProgramParameterInput -> PoseStateMachine -> state-local Player -> AnimationSlot -> Local Pose composition -> LocalToComponentPose -> Component Pose controls -> Goal Contributions -> Goal Assembler -> FullBodyIK -> ComponentToLocalPose -> OutputPose`。FootPlacement与PoseBoneIKGoals MUST从同一Component Pose扇出typed Goal Contribution，唯一Goal Assembler MUST形成一个Goal Set，唯一FullBodyIK MUST消费原始Component Pose与该Goal Set。

Runtime MUST不在图外补建Goal Assembler、Foot Placement、FBBIK、空间转换、第二Goal Set、第二Pose Graph或第二Output路径。

#### Scenario: 查看完整Foot Placement拓扑

- **WHEN** 作者查看包含FootPlacement与PoseBone Goal来源的正式Pose Graph
- **THEN** 图 MUST明确显示两个Goal Contribution进入唯一Assembler，再进入唯一FullBodyIK
- **AND** MUST不存在多个Goal Set并行汇入FBBIK的隐藏拓扑

### Requirement: Pose端口必须显式区分空间并允许typed控制目标

Pose Graph MUST使用`pose.local`、`pose.component`、`component.full-body-ik-goal-contribution`与`component.full-body-ik-goals`稳定端口类型。FootPlacement与PoseBoneIKGoals只读Component Pose并输出Goal Contribution；Goal Assembler接收固定typed Contribution集合并输出唯一Goal Set；FullBodyIK接收一个Component Pose和一个Goal Set并输出Component Pose。

Goal Contribution、Goal Set与Pose空间不得隐式cast、复用同一端口或通过Skeleton可写IK骨伪装。Goal Assembler MUST拒绝重复Effector Slot、错误Application、不同Frame/Completion/Rig lineage和超过编译容量的Contribution。

#### Scenario: FootPlacement与PoseBone贡献同一Slot

- **WHEN** Compiler发现FootPlacement与PoseBoneIKGoals可能写入同一Effector Slot
- **THEN** Character Build MUST报告两个producer与冲突Slot并拒绝Projection发布
- **AND** Runtime MUST不依赖Goal连接顺序决定覆盖者

### Requirement: Pose Plan必须按拓扑编译为有序执行阶段

Pose Plan Compiler MUST把Goal Contribution收集、唯一Goal Assembler、FBBIK和OutputPose编译为固定有序阶段，并为Pose Constraint根Bank分配固定Foot Result、Goal Contribution、Goal Set与BendHistory容量。Projection MUST静态证明每条正式路径最多一个Goal Assembler、一个Goal Set、一个FBBIK、一个OutputPose和一个Final Writer。

运行时 MUST按编译索引执行Contribution生产与Assembler，不查找作者字符串或动态扩容。正式空Goal输入 MUST由唯一Assembler输出`GoalCount=0`；不得使用Empty Goal兼容节点、Goal Set passthrough/copy或第二Assembler。

#### Scenario: 编译无Goal贡献的角色

- **WHEN** 某角色的正式Pose Graph没有任何有效Goal Contribution
- **THEN** 唯一Assembler MUST编译为固定容量零贡献并发布`GoalCount=0`
- **AND** Compiler MUST不插入Empty Goal fallback或删除唯一FBBIK拓扑

### Requirement: Goal Sources与FullBodyIK必须使用统一typed目标合同

全部Goal Source MUST发布`CharacterFullBodyIkGoalContribution`，至少携带Frame、Completion、Rig、Producer、Slot、Application、Component空间目标与权重。唯一Goal Assembler MUST把合法Contribution规范化为一个`CharacterFullBodyIkGoalSet`；FBBIK MUST不理解Foot State Context、Contact Patch、Constraint State、Pelvis选择或Diagnostics。

FBBIK腿Effector跨帧稳定策略 MUST由正式FullBodyIK Profile和Pending BendHistory决定。Solver MUST不通过搜索FootPlacement SourceKind启用隐藏状态规则；Vendor FinalIK对象内部字段不得成为跨帧真相。

#### Scenario: FBBIK消费Foot Placement贡献

- **WHEN** FootPlacement贡献左右脚与Pelvis Slot且Assembler完成唯一Goal Set
- **THEN** FBBIK MUST只按Goal Application、Slot、Profile与Pending BendHistory执行求解
- **AND** MUST不回调FootPlacement、读取Ground Path或修改Contact ownership

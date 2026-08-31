# 现行Operation Family迁移表

## 迁移口径

公共Header只保留`OperationIndex、Code、Family、ExecutionDomain、PayloadIndex、InputValueRange、OutputValueRange、SourceMapIndex`。下表的“旧字段”指当前万能`CharacterPresentationPoseOperation`中必须迁入Family Payload或直接删除的字段；`NodeId`只通过稳定Source Map保留，不进入运行Family数学。

| Operation Code | 新Family | 当前／目标Domain | 跨帧状态Owner | 本帧页Owner | Workspace与结果 | 旧字段迁移或删除 |
|---|---|---|---|---|---|---|
| `ProgramParameterInput` | Parameter Input | FactAndDemand | 无；默认值属于Image | Program Frame | Parameter value | `ParameterIndex`进payload；其余可选字段删除 |
| `SelectedPosePlayer` | Player | SourceCapture | Actor State.Player continuity | Program demand + Source Frame | Local Pose、parameter、foot feature、discontinuity | Source provider/index、PlayerIndex进payload；Blend／Constraint字段删除 |
| `BlendStack` | Player／Blend Stack | SourceCapture | Actor State.BlendStack与Routing | Program demand + Source Frame | source贡献与Local Pose | Source、BlendNodeIndex、Weight进payload |
| `Inertialization` | Inertialization | PurePose | Actor State.Inertialization | Program Frame | Local Pose、history／residual workspace | InertializationIndex进payload |
| `BlendPose` | Blend | PurePose | 无 | Program Frame | 两个Pose输入、weight、Local Pose输出 | Input A/B、ParameterIndex、Weight进payload |
| `LayeredBoneBlend` | Blend | PurePose | 无 | Program Frame | Pose与dense mask | BoneMaskIndex、Weight和输入进payload |
| `AdditivePose` | Blend | PurePose | 无 | Program Frame | Pose与reference | AdditiveReferenceIndex、Weight和输入进payload |
| `PoseParameterResolve` | Parameter Resolve | PurePose | 无 | Program Frame | Pose parameter policy与Local Pose | ParameterPolicies进专页；万能数组删除 |
| `ModifyBone` | Component Control | PurePose | 无 | Program Frame | Component Pose与ModifyBone descriptor | ModifyBoneIndex、Weight和输入进payload |
| `FootPlacement` | Goal Contribution／Foot | WorldAwareValue | Constraint Bank.Foot／Pelvis历史 | Constraint Pending页，Program只记completion | typed Foot handle、Goal Contribution Result | FootPlacementIndex、输入Pose／Weight value进payload；外部Goal offset删除 |
| `OutputPose` | Output | FinalPublication | Final Publication committed pose | Program output binding + Publication Pending页 | `ProgramOutputPoseResult`与Publication Result | OutputValue、稳定Publication layout handle进payload；Writer不进Graph |
| `BlendSpacePlayer` | Player | SourceCapture | Actor State.Player clock／continuity | Program demand + Source Frame | Local Pose、x/y参数、discontinuity | Source、PlayerIndex、ParameterIndex A/B、range policy进payload |
| `PoseBoneIKGoals` | Goal Contribution／PoseBone | PureValue | Constraint Bank Goal页 | Constraint Pending页 | typed Contribution Result | PoseBoneIkGoalsIndex、输入Pose和输出Contribution value进payload |
| `ClipPlayer` | Player | SourceCapture | Actor State.Clip clock／continuity | Program demand + Source Frame | Local Pose与discontinuity | ClipPlayerIndex、PlayerIndex、source进payload |
| `PoseStateMachine` | State Machine | PurePose | Actor State.StateMachine | Program Frame | active state、blend control与Local Pose | StateMachineIndex进payload；内部operation range由Image descriptor引用 |
| `StatePoseOutput` | State Output | PurePose | 只读StateMachine状态 | Program Frame | state-local Local Pose | Input／Output value与state descriptor handle进payload；不是独立作者Node |
| `AnimationSlot` | Animation Slot | PurePose | Actor State.Slot与Action lifecycle cursor | Program Frame + Source Frame | source/action input、BlendStack workspace与Local Pose | Channel、SlotIndex、control operation、Player／Blend index、selection policy进payload |
| `ActionPlaybackInput` | Action Input | FactAndDemand | Actor State.Action lifecycle | Program Frame | typed ActionPlayback value | Channel与control identity进payload |
| `RootOrientationWarp` | Component Control | PurePose | Actor State.RootWarp control／Routing | Program Frame | Local Pose与warp descriptor | RootOrientationWarpIndex、input/output进payload |
| `LocalToComponentPose` | Space Conversion | PurePose | 无 | Program Frame | Local→Component Pose | Input／Output typed value进payload |
| `ComponentToLocalPose` | Space Conversion | PurePose | 无 | Program Frame | Component→Local Pose | Input／Output typed value进payload |
| `FullBodyIK` | FullBodyIK Constraint | PurePose | Constraint Bank.BendHistory／Solver | Constraint Pending页，Program只记completion | typed FBBIK handle、Solved Component Pose Result | FullBodyIkIndex、GoalSet input、Pose input/output进payload；NativeSlice与offset删除 |
| `LinkedPoseCall` | Linked Pose | PurePose | Actor State.LinkedPose active/reset与routing | Program Frame | typed call inputs/outputs与fragment result | CallIndex／FragmentIndex进payload；动态ports形成value range |
| `MotionMatchingPose` | Motion Matching Player | SourceCapture | Actor State.MM relevance／selection／jump blend | Program demand + Source Frame + Program Frame | MM binding、history、trajectory、pose output | source/player相关字段由MM payload取代；不复用万能索引 |
| `PoseHistoryRead` | Pose History | PurePose | Actor State.PoseHistory | Program Frame | previous history与当前Pose透传 | collector descriptor handle与typed value进payload |
| `PoseHistoryCommit` | 删除死Code；实际commit并入Pose History payload | 当前无producer/consumer | Actor State.PoseHistory | Program Frame | 由唯一Pose History evaluator在固定schedule提交 | 枚举和旧分支删除，不创建兼容Operation |
| `MotionMatchingChooserResolve` | Motion Matching | 当前无Operation producer/consumer；目标FactAndDemand子阶段 | Actor State.MM chooser | Program Frame | typed chooser result | 删除死Code；现有MM正式plan字段进入MM family payload |
| `MotionMatchingEntrySourceCapture` | Motion Matching | 当前无Operation producer/consumer；目标SourceCapture子阶段 | Actor State.MM entry | Source Frame | entry source sample | 删除死Code；由Source Demand／Frame表达 |
| `MotionMatchingEntryProcessing` | Motion Matching | 当前无Operation producer/consumer；目标PurePose子阶段 | Actor State.MM entry | Program Frame | processed entry pose | 删除死Code；由MM family payload表达 |
| `MotionMatchingInternalBlend` | Motion Matching | 当前无Operation producer/consumer；目标PurePose子阶段 | Actor State.MM jump blend | Program Frame | blended Local Pose | 删除死Code；由MM family payload表达 |
| `FullBodyIkGoalAssembler` | Goal Assembler Constraint | PureValue | Constraint Bank GoalSet页 | Constraint Pending页，Program只记completion | typed Assembler handle与GoalSet Result | Contribution input range、GoalSet output进payload；Goal workspace offset不外露 |

## 非Operation作者节点

| Node Kind | 编译结果 |
|---|---|
| `PoseSubgraph` | 只由Graph Closure展开dependency，不生成运行Operation |
| `GraphInput`／`GraphOutput` | 只形成Subgraph／Linked entry接口value绑定，不形成独立业务Operation |
| `EntryPoseInput` | MM Entry Graph的typed入口value，不创建第二source捕获Owner |

## 完成条件

- 每个仍存在的Code恰好属于一个Family，每个Header恰好引用一个合法Payload。
- 死Code必须删除，不能仅保留枚举值和默认payload。
- Family evaluator只读自身payload、typed value和Owner页；不得读取万能记录其它字段。
- Stage Schedule固定每个Operation一次；Foot、PoseBone、Assembler和FBBIK各通过Constraint typed handle调用一次，Writer由Final Publication执行，不是Operation。
- 新Projection破坏性替换旧ABI，不保留v23/v26 reader或运行时版本选择。

# PoseGraph迁移Owner与寿命清单

## 固定接入

- 总行为基线：`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`。
- 第一阶段接入提交：`f32e419`；最后运行实现提交：`5b551cb`。
- 第一阶段证据：`Diagnostics/FootPlacementRuns/20260901-070946-569-c14830f966ee465c887849cfc66b1f2a`。
- 当前Corin Projection仍是`character-presentation-pose-plan/v23`、`character-presentation-pose-runtime/v26`，Plan Hash为`9884a71baf0ae369f36d2a60ab6a19573aba83bdf40489355602d266cb262270`，Projection Revision为`4c1db68f0b6eb946de0fa2cd78ecddee3793cbc000a9dd8b53070afed109612e`。这些身份会在新ABI显式Build时更新；Gameplay Program与Contract身份不能因本重构变化。
- `ad3527e..f32e419`之间Corin Profile、Rig、Pose Graph作者资产和generated资产均无Git差异。代码差异只来自已通过的IK内部Owner、Reset观测和诊断映射，不恢复旧初步Resolved、Fact历史读取或Vendor空历史方向。

## 当前帧顺序

当前正式顺序是：

`Apply Pending Tuning -> Begin AnimationPresentationFrameTransaction -> Action lifecycle -> Action sampling -> Slot计划 -> Pose source/state推进 -> 可选Motion Matching准备 -> source资源准备与release校验 -> PosePlan Prepare -> Validate -> 单次Animancer Evaluate Barrier -> staged Pose/Value/Constraint执行 -> Final Pose预检与Physical Writer -> Pose/Constraint/Source/Action共同Seal -> PostCommit Diagnostics`

迁移允许改变类型和归属，不允许改变上述业务先后、浮点表达式或一次性次数。Barrier前失败Discard；Barrier内及之后失败Fault；Writer成功后只允许no-throw的identity提升和已验证发布。

## 当前Owner与目标Owner

| 当前Owner或页 | 当前保存内容 | 当前问题 | 唯一目标Owner与寿命 | 删除位置 |
|---|---|---|---|---|
| `CharacterPresentationPosePlan` | 作者编译后的Operation、Stage、各descriptor、workspace容量、source map | 静态语义使用万能Operation与分散descriptor | Projection内不可变`CharacterPoseProgramImage`；Build寿命 | 新Image Seal后删除旧Plan reader与旧schema |
| `CharacterPoseGraphNativeProgram`静态数组 | native Operation／Stage、Mask、Reference、Parameter、Rig、Blend、ModifyBone、Goal、Linked Pose表 | 静态表与actor控制页混在一个“Program”对象 | `CharacterPoseProgramExecutionView`；每Actor只读物理视图，由Program Runtime唯一Dispose | Image与View接通后删除旧Native Program |
| `CharacterPoseGraphNativeProgram`Committed/Pending页 | StateMachine、Slot、RootWarp、LinkedPose controls及active fragments | Actor状态伪装成Program内容 | `CharacterPoseActorState`的Committed/Pending页；Actor寿命 | 从Execution View和Image彻底移除 |
| `PosePlanExecutionRuntime` | Source资源、Player、State、Stack、Slot job、MM、release、Native Program、Workspace、Constraint、Writer、Diagnostics、Frame完成状态 | 外层同时理解资源、节点、Constraint、Writer和诊断内部布局 | 拆为Program Runtime、Source Module、Constraint Module、Final Publication与薄协调根 | 各Owner接通后删除旧巨型Implementation，不留wrapper |
| `CharacterPoseGraphStagedExecutor` | 全部静态页、Value页、Slot页、Inertial历史、Constraint对象、Frame identity和各Operation switch | 一个每帧大构造器解释所有Family并直接读Constraint内部入口 | Program Runtime持久Executor；只绑定Execution View、Actor State与Program Frame页，Family evaluator只读本Family payload | Family ABI完成后删除旧类型 |
| `PoseInertializationNativeProgram` | Inertial规则、历史、残差、Committed/Pending状态与默认参数 | 独立子Program持有状态和Tuning | 静态规则进入Image/View；历史与残差进入Actor State；本帧Accumulator进入Program Frame页 | 迁移后删除独立Program语义容器 |
| `PresentationFrameWorkspace` | Action／provider source sample、usage、failure、release completion双页 | 根事务持有业务页索引 | Source-owned Pending页；根只持Source lease/result | Source Module接通后删除根对内部页的索引 |
| `AnimationPresentationFrameTransaction` | Frame identity、Workspace、Action、Sampling、Slot、Pose、MM lease和临时批次 | 只有局部identity，直接暴露每个旧子系统lease | `CharacterPoseFrameTransaction`只持统一Lineage、阶段、Module lease/result和Outcome | typed Result接通后整体改名替换 |
| `CharacterPoseStateSourceRuntime`、Direct Player、Blend Stack、Route、Slot、StateMachine Runtime | source时间、continuity、选择、Transition、Slot、Routing及跨帧控制 | 逻辑状态与物理source装配散在外层 | 逻辑状态进入`CharacterPoseActorState`；物理资源与采样进入Source Module | Program/Source分责完成后删除旧外层数组和字典 |
| Animancer backend、Physical Source Registry、playable arrays、release pools | Playable资源、capture binding、prepared/release握手、deferred release | 与Operation和Actor逻辑状态同Owner | `CharacterPoseSourceModule`静态binding与Owned Pending页 | Source Module闭环后从旧Runtime删除 |
| Motion Matching relevance、demand、source usage、history read/completion | chooser、entry source、处理、内部blend、history handshake | 部分位于MM Module，部分位于Pose Runtime临时页 | Program Actor State保存逻辑；Source页保存采样；Program Frame页保存本帧demand/value/completion | 保留现有MM数学，删除外层重复scratch |
| `CharacterPoseConstraintRuntime`根Bank | Foot、Pelvis、Goal Contribution／Set、FBBIK、BendHistory、Solver Outcome和诊断 | 内部Owner已清晰，但外部接口暴露Operation index、offset、NativeSlice与Final Writer | Constraint Module只收typed编译Handle并发布一个Constraint Result；保留第一阶段内部实现 | 删除外部内部页、offset和第二completion身份 |
| `AnimationFinalPosePhysicalWriter`及Native final binding | Pending／Committed Final Pose读取、整Rig写入与write outcome | Writer由Constraint持有，Program和Constraint同时知道Final页 | 具体`CharacterFinalPosePublication`拥有唯一Final页、binding、Writer和Publication Result | 从Constraint与Program Runtime删除Writer所有权 |
| `AnimationPresentationRuntimeSnapshotPublisher` | 从Native Program、Workspace、Constraint和运行对象拼Snapshot | Diagnostics读取多个Owner内部页 | interest-gated冻结页与Committed Source／Program／Constraint／Publication Result的单向Projector | Result链接通后删除内部对象引用 |
| `CharacterPoseTuningRuntimeBinding`与各Owner直接Apply | 运行时先修改多个对象，失败时反向Apply旧Block | 原子性依赖手动回滚且共享静态对象边界不清 | actor-local`CharacterPoseTuningSnapshot`，Program／Source／Constraint Candidate全成功后一次提升Generation | 删除共享Image/View可变值与反向Apply |
| `CharacterPresentationPosePlanCompiler.CompilationState` | Graph closure、IR、Operation、Value、Stage、Workspace、descriptor和source map原地累积 | 后置Pass可修改前置结论 | 九个不可逆Pass Result；每Pass只消费前置只读Result | Seal Image通过后删除中央State |
| `ICharacterPoseCompilerHandler`与Registry | Payload、端口相关特例、Execution Domain、Code及Player／Slot／Blend布尔矩阵 | Capability、Handler、Validator与Codec重复节点语义 | 唯一Node Definition投影Capability并提供dependency／lowering | 全部Definition覆盖后删除Handler与Registry |

## 跨帧状态保护

| 状态族 | 当前唯一业务含义 | 目标Owner | 初始化／Reset边界 |
|---|---|---|---|
| Movement clock、source generation、raw/effective time、cycle、continuation | 决定Clip／Blend Space／Action source下一帧采样 | Actor State的Player／State子页；物理sample由Source Module发布 | State entry、Preview seek、Projection replacement、Actor Reset沿现行入口 |
| PoseState、active transition、blend elapsed/duration、fact/control generation | 决定下一帧State和Standard Blend | Actor State的StateMachine页 | 现有StateMachine Reset、Fragment reset和完整Actor Reset |
| Slot、ActionPlaybackInput command cursor与release completion | 决定有限Action生命周期和Slot source | Actor State的Action／Slot页，Source Module只拥有资源握手 | Action停止、Channel换代、Projection replacement和Actor Reset |
| Blend Stack、Route、Linked Pose active/reset fragment | 决定下一帧路由、权重和fragment选择 | Actor State的Blend／Routing／LinkedPose页 | 现有route Reset、fragment失活和Actor Reset |
| Inertialization history、velocity、parameter／foot accumulator、residual | 决定下一帧惯性输出 | Actor State的Inertialization页 | 原rule触发、discontinuity、seek、owner reset和Actor Reset |
| Motion Matching relevance、history、selection／pose completion | 决定下一次search、entry与jump blend | Actor State的MM页；同帧value在Program Frame页 | 现有relevance policy、entry、seek和Actor Reset |
| Foot Landing／Interpolation／Primary／Pelvis／Bend | 第一阶段已明确的IK历史 | Constraint Module内部根Bank | 保留`f32e419`的Reset与提交边界，不进入Actor State副本 |
| Final Physical Pose committed页 | Writer失败时的正式保持结果和下次Publication参考 | Final Publication | Rig binding创建、Projection replacement和Actor Dispose |

## 已配置内容目录

当前正式Node Kind共29种，其中`PoseSubgraph`、`GraphInput`、`GraphOutput`、`EntryPoseInput`是closure／界面角色，不一定生成一对一Operation。当前Operation Code共31个；其中`PoseHistoryCommit`和四个`MotionMatching*`内部Code只有枚举声明，没有Compiler producer或Runtime consumer，不能当作已运行路径。新架构要么用正式MM typed payload表达现有MM子阶段，要么删除这些死Code，不能保留“也许会用”的旧ABI占位。

实际迁移范围包括已存在的Clip Player、Blend Space Player、Selected Player、Blend Stack、PoseStateMachine、Action Input、Animation Slot、标准Blend、Layered／Additive、Inertialization、空间转换、Modify Bone、Root Orientation Warp、Linked Pose、Motion Matching、Pose History、Foot Placement、PoseBone Goal、Goal Assembler、FullBodyIK和Output。未配置节点、TrainingEnemy、第二IK、Control Rig和新增动画内容不接入。

## 数据与诊断保护

- Foot Motion仍按Live Contribution最大Weight选择，等权保留遍历首项并排除Stored；实际dominant source时间、cycle和curve不变。
- Foot／Pelvis／Goal／FBBIK沿`f32e419`链路；Reach仍只作观察与Landing完成资格，不恢复骨盆硬夹紧或末端夹脚。
- 诊断继续使用唯一Sampler、1215列根Schema、Analyzer、Publisher、42项规则、紧凑detail和quality-score；Runtime只迁移事实来源。
- 每个代码闭环同时对上一通过提交和`ad3527e`固定包回放。先核对1044输入、Body、source时间和schedule，再核对Pose、Foot、Pelvis、Goal、Solved与Physical。

## MODIFIED Requirements

### Requirement: Pose Graph工作区必须准确映射Authoring、Live与References

正式`CharacterPresentationPoseGraphEditorWindow` MUST围绕唯一`GraphAuthoringCanvasView`提供当前Graph导航、正式角色画面、当前selection Details、Preview typed输入与精确target状态。Details MUST只投影当前Node、State或Transition的Authoring、Runtime Input、Applied、References与错误：Authoring只通过正式Presentation或typed Profile Mutation修改唯一owner字段；Runtime Input只读显示正式输入与来源；Applied只读取当前精确target采用的parameter block、active generation与应用帧；References只读显示Source Slot、Profile binding子资产、实际资源对象、Rig、Policy和call site。稳定identity、GUID、revision、hash与compiled index MUST默认隐藏。Target为Live Actor时节点、edge、State与Structural mutation MUST禁用；TunableDefault MAY按其应用策略修改正式owner并提交当前精确target。revision不匹配 MUST显示Stale并清空旧Applied、运行高亮与diagnostics。Workspace MUST不以全局参数表、独立Live Graph或重复Preview/Live/Diagnostics Dock复制这些数据。

#### Scenario: 查看Locomotion State

- **WHEN** 作者选中Locomotion State的Sequence或BlendSpace Player
- **THEN** Details MUST显示类型匹配的Source Slot对象选择器、该selection的作者字段与应用策略
- **AND** 当前typed输入与Applied值 MUST在对应上下文中只读显示
- **AND** References MUST显示解析后的Profile binding、实际资源、owner与Open Source命令
- **AND** MUST不显示BaseLocomotion Gameplay producer、可编辑Source Id或完整Tuning Layout

#### Scenario: Runtime revision不匹配

- **WHEN** snapshot、parameter block revision与当前文档、Projection或Layout不一致
- **THEN** Applied、运行高亮与diagnostics MUST显示Stale或Rejected并清除旧值
- **AND** MUST不从authoring默认值、Animancer state或上一target伪造结果

### Requirement: Preview、Runtime与Live Debug必须复用同一固定Pose Plan

Projection Compiler MUST把PoseGraph降低为固定Fact/parameter、source demand/capture、空间化PurePose、world-aware Pose与FinalPublication stage table，并发布固定Tuning Layout与默认parameter block。每帧每个source、Player、transition、Slot、composition、转换、IK、world-aware control和writer MUST只执行一次正式计划，所有source合计只进行一次正式PlayableGraph Evaluate。Action Timeline Preview、PoseGraph Preview Fixture、Live Debug与正式Runtime MUST使用同一Projection revision、Routing Plan、stage table、source backend、Pose consumer和completion语义。精确Fixture world context完整时Preview MUST执行真实world-aware stage，不完整时 MUST报告typed Unavailable。Structural mutation或Stale Projection/Pose Plan时Preview MUST停止并等待显式Build；只修改TunableDefault且TuningLayoutHash保持一致时Preview MAY继续执行同一Plan并在帧边界采用Unpublished candidate。系统 MUST不创建临时Plan、第二solver、第二PlayableGraph、shadow skeleton或直接Clip播放器。

#### Scenario: 结构修改后继续Preview

- **WHEN** 作者修改State、Slot、Rig、Pose空间、节点拓扑、workspace容量或其它Structural字段使Projection变为Stale
- **THEN** Preview MUST停止消费旧Plan
- **AND** MUST不创建临时Plan、默认空间转换、旧Projection fallback或自动Build

#### Scenario: 只修改Tunable字段

- **WHEN** 作者只修改layout内的TunableDefault且candidate与当前Program、Projection、Pose Plan、Rig和Layout identity精确匹配
- **THEN** Preview或选定Live target MUST继续执行同一固定Pose Plan
- **AND** MUST在PresentationFrame边界原子采用完整candidate并显示未发布状态

## ADDED Requirements

### Requirement: PoseGraph运行状态必须叠加在同一作者图

PoseGraph Workspace MUST通过匹配当前PoseGraph、Projection revision与completed frame lineage的正式Preview snapshot或RuntimeDebugSession snapshot，在同一作者Canvas上显示当前State、target State、Transition edge/progress、执行Pose节点、availability与contribution。Overlay MUST只读且不得修改Graph asset、打开runtime clone、保存第二份selection或创建第二张Live Graph。没有合法target或revision失配时 MUST清除旧overlay并显示Unavailable或Stale。

#### Scenario: Preview从Idle切换到Run

- **WHEN** 正式PoseStateMachine在Preview中选择Run并完成Transition
- **THEN** 同一作者Canvas MUST显示Idle到Run的Transition与当前Run State状态
- **AND** 高亮数据 MUST来自该Preview正式Pose Plan snapshot

#### Scenario: 切换Live Actor

- **WHEN** 作者从一个Live Actor切换到另一个精确匹配Actor
- **THEN** Workspace MUST释放旧target interest并清除旧节点状态
- **AND** 新高亮 MUST只来自新target的匹配frame lineage

### Requirement: Pose StateMachine Transition必须只以edge和上下文Details呈现

PoseStateMachine中的Transition MUST在共享StateMachine Canvas中以source State到target State的edge呈现，并在选中edge时由Details显示Rule、priority、blend、sync、Authoring值和当前target应用状态。Graph导航 MAY显示StateMachine本身，但 MUST不把每条Transition投影为平铺按钮、重复列表或第二份selection来源。

#### Scenario: 查看Locomotion transitions

- **WHEN** 作者打开Locomotion PoseStateMachine
- **THEN** Canvas MUST显示Entry、State、Alias与Transition edges
- **AND** 作者选择一条edge后Details MUST显示该Transition的正式字段
- **AND** 图导航 MUST不显示Transition按钮清单

### Requirement: PoseGraph Preview输入必须与Live输入所有权分离

Preview Fixture MAY通过会话级typed输入提交Grounded、Movement Mode、Speed、Direction及当前正式Preview所需的其它输入。Live Actor的Gameplay Fact与Runtime Input MUST只读来自正式committed状态；作者不得在PoseGraph Workspace为Live Actor伪造Gameplay输入。TunableDefault不是Gameplay输入，MUST继续通过正式作者Mutation和精确target parameter block candidate应用。

#### Scenario: 本地Preview修改Speed

- **WHEN** 当前target为Preview Instance且作者修改Speed输入
- **THEN** Preview Fixture MUST在后续表现帧提交新的typed输入
- **AND** PoseStateMachine MAY按正式Rule改变State

#### Scenario: Live Actor显示Speed

- **WHEN** 当前target为运行中的Corin Actor
- **THEN** Workspace MUST只读显示该Actor正式提交的Speed与来源
- **AND** MUST不提供覆盖该Gameplay输入的编辑器字段

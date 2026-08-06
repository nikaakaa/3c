# character-presentation-pose-graph Specification

## MODIFIED Requirements

### Requirement: Pose Graph必须唯一表达完整表现拓扑

`CharacterAnimationPresentationProfile`引用的Pose Graph MUST唯一表达`ProgramParameterInput -> PoseStateMachine -> state-local SequencePlayer、BlendSpacePlayer或MotionMatchingPose -> AnimationSlot -> Local Pose composition -> Component Pose处理 -> OutputPose`。当状态使用Motion Matching时，图 MUST显式包含`PoseHistoryCollector`、`MotionMatchingPose`及其typed History/Trajectory/Facts/Binding依赖，MM节点 MUST直接输出Local Pose。图 MUST不包含`SelectedPosePlayer`、`CharacterMotionMatchingPoseSourceSlot`、显式MM `BlendStack`、AnimationSelectionInput、MotionMatchingSelectionInput、MarkerSync节点或图外MM Player。Runtime MUST不在图外补建基础动画、StateMachine、Slot、Blend、History、IK、空间转换或第二Output路径。

#### Scenario: 检查新增角色的Grounded MM链

- **WHEN** 作者打开`MotionMatchingDemoCharacter`的Pose Graph
- **THEN** 图 MUST能从typed事实和Trajectory追踪Chooser、History、MotionMatchingPose、Action Slot及下游Pose处理
- **AND** MUST不显示或生成MM Source Slot、SelectedPosePlayer或外接MM BlendStack

#### Scenario: 普通Sequence状态

- **WHEN** 一个非MM状态使用SequencePlayer
- **THEN** SequencePlayer MUST继续直接输出state-local Pose
- **AND** 系统 MUST不强制为它创建MM History或Chooser

### Requirement: Pose Plan必须按拓扑编译为有序执行阶段

Projection Compiler MUST按typed依赖、Pose空间和execution domain把同一Pose/Value DAG编译为有序阶段。包含MM时，stage table MUST至少表达Frame Context Resolve、History Read、Chooser Resolve、Search、Entry Source Capture、Entry Processing、Internal Blend、History Commit及下游Pose stages；History Read MUST早于Search，History Commit MUST晚于MM Local Pose且早于AnimationSlot和world-aware修正。stage table MUST只属于generated plan，不得写入authoring Graph。每个source每帧 MUST最多capture一次，PlayableGraph MUST最多Evaluate一次，Physical Transform MUST只由final writer写一次。

#### Scenario: 编译MM基础Pose

- **WHEN** 合法图把History Collector和MM节点连接到Action Slot之前
- **THEN** Compiler MUST生成无环的read/search/pose/commit顺序
- **AND** MUST不把History Commit推迟到IK或FinalPublication之后

#### Scenario: History形成同帧反馈环

- **WHEN** 图要求MM搜索读取本帧尚未完成的MM输出
- **THEN** Compiler MUST报告时序环
- **AND** MUST不通过读取未完成page打破该环

### Requirement: State inline graph必须存入root-owned flat graph catalog

每个Pose State inline graph以及每个`MotionMatchingPose` entry processing graph MUST存入root Pose Graph Document的flat graph catalog，并通过稳定owner identity引用。State inline graph负责该State的完整Pose入口；MM entry graph负责每个live entry混合前的局部Pose处理。任何child graph MUST不嵌套序列化自己的子图对象。删除owner时，Mutation MUST在确认无其它引用后删除对应flat graph；复制owner时 MUST创建新graph identity而不是共享可变图。

#### Scenario: 复制MM节点

- **WHEN** 作者复制包含entry graph的MotionMatchingPose节点
- **THEN** Mutation MUST复制图内容并分配新的owner和graph identity
- **AND** 修改副本内部图 MUST不影响原节点

### Requirement: State-local source必须由Profile binding和provider解析

SequencePlayer和BlendSpacePlayer MUST继续通过Presentation Profile typed binding解析各自source provider。Motion Matching MUST通过节点的`motion-matching.binding`解析Profile、Chooser、SearchDomain和generated artifacts，并由节点内部entry player直接采样选择结果；它 MUST不发布`PresentationPoseSourceSample`给外部Player。Profile未绑定、Chooser无效、artifact stale或Rig闭包不完整时，state-local source MUST为Invalid而非使用默认clip。

#### Scenario: MM Profile缺失

- **WHEN** 可达MotionMatchingPose节点的binding没有合法MM Profile
- **THEN** Profile validation和Projection Build MUST失败
- **AND** Runtime MUST不选择默认Sequence或上一个state source

### Requirement: Pose节点必须显式处理可用性和局部连续性

每个Pose source owner MUST显式处理自己的Pending、Ready与Invalid以及局部连续性。SequencePlayer和BlendSpacePlayer按各自正式合同管理采样；MotionMatchingPose MUST原子管理搜索、entry player与internal Blend Stack。PoseStateMachine只拥有state-to-state transition，AnimationSlot只拥有有限Action覆盖；任何同一次MM Jump MUST不再由Inertialization、显式BlendStack或State transition二次淡入。

#### Scenario: MM Jump发生在State transition期间

- **WHEN** 一个MM节点在其State仍有relevance时产生Jump，同时外层State transition正在混合
- **THEN** MM节点 MUST只处理内部entry jump
- **AND** StateMachine MUST只处理两个state输出之间的transition

### Requirement: Preview、Runtime与Live Debug必须复用同一固定Pose Plan

Preview、Runtime和Live Debug MUST使用同一Profile、Pose Graph Document、Projection、Native Pose Program、Frame Context、Chooser、Search Kernel、MM node program、History布局和Blend Stack Kernel。Preview controls MAY注入typed事实、Trajectory和时间，但 MUST不直接指定私有clip、绕过Chooser、调用独立fixture player或维护shadow Pose History。

#### Scenario: Preview调整Trajectory

- **WHEN** 作者在Preview修改未来Trajectory样本
- **THEN** 正式MM节点 MUST在下一预览帧使用该typed输入查询
- **AND** cost、selection和Pose Watch MUST来自同一Projection计划

### Requirement: Pose authoring必须使用共享Capability与类型化Presentation Mutation

Pose Graph Document exporter、reconciler、Canvas、Inspector、创建菜单、粘贴、复制、删除和编译 MUST共享同一Capability Catalog与typed Presentation Mutation。创建`MotionMatchingPose`时，Mutation MUST原子创建节点payload、entry processing graph identity、`EntryPoseInput -> GraphOutput`身份图和必需引用；创建`PoseHistoryCollector`时 MUST创建明确history identity。任何入口 MUST不自行补默认Chooser、Profile、数据库或runtime fallback。

#### Scenario: 从创建菜单添加MotionMatchingPose

- **WHEN** 作者在合法state inline graph创建MotionMatchingPose
- **THEN** Canvas和Document MUST得到同一组节点、端口与entry graph identity
- **AND** 未配置binding时 MUST显示明确未完成状态而不是写入默认资产

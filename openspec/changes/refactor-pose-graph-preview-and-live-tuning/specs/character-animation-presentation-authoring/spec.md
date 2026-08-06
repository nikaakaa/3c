## ADDED Requirements

### Requirement: PoseGraph可运行入口必须来自精确Presentation Profile上下文

正式可运行入口 MUST从`CharacterAnimationPresentationProfile`打开现有`CharacterPresentationPoseGraphEditorWindow`，并一次绑定精确Character Definition、Profile、PoseGraph、Presentation Projection、Rig、Source Bindings与Preview Fixture identity。Action Animation Workspace、有限Timeline、Action call site与AnimationSlot解析 MUST不成为打开PoseGraph的前置条件。缺少或失配上下文时工作区 MUST显示typed Unavailable或Stale，不得从Scene、GameObject名称、资产目录、generated产物或last-used状态猜测。

#### Scenario: 从Corin Profile打开PoseGraph

- **WHEN** 作者在Corin Presentation Profile执行Open Pose Graph
- **THEN** 现有PoseGraph窗口 MUST绑定该Profile引用的正式PoseGraph及其精确Projection、Rig和Source Bindings
- **AND** MUST不打开Action Animation Workspace或要求选择Action

#### Scenario: Attack存在多个call site

- **WHEN** Corin Attack在Action Animation Workspace中具有多个Action call site
- **THEN** PoseGraph正式入口与Preview MUST不受该歧义影响
- **AND** 系统 MUST不通过任一call site猜测PoseGraph上下文

### Requirement: PoseGraph Workspace必须完成现有图的可运行作者闭环

现有PoseGraph Workspace MUST提供显式Validate/Compile/Character Build与状态、正式角色画面、Root Graph/root-owned子图/PoseStateMachine导航、唯一Graph Canvas、当前selection Details、Preview typed输入和精确目标选择。功能 MAY按固定分割区或后续可停靠区域排列，但布局方式 MUST不成为执行链、数据owner或调参语义的一部分。Workspace MUST不要求固定Pose Watch、Asset Browser、Trace、Search Results或Preview Scene Settings面板才能运行现有PoseGraph。

#### Scenario: 打开现有Corin PoseGraph

- **WHEN** 精确上下文和当前发布产物合法
- **THEN** Workspace MUST同时提供可操作的现有PoseGraph和执行该图的Corin角色画面
- **AND** 作者 MUST能够从Root Graph下钻PoseStateMachine与State Graph并返回

#### Scenario: 窗口空间不足

- **WHEN** 当前窗口尺寸不足以同时展开全部辅助内容
- **THEN** Workspace MAY折叠或重排辅助区域
- **AND** 唯一Graph Canvas、当前selection、Preview target和作者数据 MUST保持不变

### Requirement: PoseGraph Workspace必须复用BTSMTL图编辑体验

PoseGraph Workspace MUST复用BTSMTL Graph Authoring interaction core与正式Node/Port/Edge/Inspector视觉资产，提供节点创建搜索、typed端口兼容过滤、移动、框选、连接、断开、删除、复制粘贴、Undo/Redo、StateMachine/State/子图下钻和breadcrumb返回。PoseGraph domain MUST只提供Pose业务node capability、typed payload、port类型、业务标题、颜色、图标、mutation和compiler handler；不得复制GraphView生命周期、selection、Undo、StateMachine manipulator或使用Unity默认Node建立第二套编辑体验。

#### Scenario: 从Pose端口创建节点

- **WHEN** 作者从Local Pose或Component Pose端口拖到空白处
- **THEN** 创建菜单 MUST只显示Capability声明为类型兼容的Pose节点
- **AND** 最终Mutation MUST写入当前现有PoseGraph owner

#### Scenario: 打开Locomotion PoseStateMachine

- **WHEN** 作者双击Locomotion PoseStateMachine节点
- **THEN** Workspace MUST使用共享StateMachine表面显示Entry、State、Alias与Transition edge
- **AND** MUST不打开BTSMTL Gameplay StateMachine数据或第二窗口

### Requirement: PoseGraph Preview必须运行正式Presentation链

PoseGraph Preview MUST通过精确editor-only Fixture把Preview typed输入转换为正式Presentation Fact与Parameter输入，并执行当前发布Projection的同一Pose Plan、source backend、PoseStateMachine、AnimationSlot、Blend、Foot Placement、Full Body IK与FinalPublication。Preview MUST只显示最终正式角色结果；不得直接播放Clip、临时编译Plan、创建第二PlayableGraph、第二solver、shadow skeleton或场景Host fallback。Fixture环境不完整时需要world-aware context的节点 MUST报告typed Unavailable。

#### Scenario: Preview输入驱动Locomotion

- **WHEN** 作者在Preview目标下修改Grounded、Movement Mode、Speed或Direction
- **THEN** 输入 MUST通过正式Presentation Fact/Parameter路径驱动现有PoseStateMachine
- **AND** 角色画面 MUST来自该帧Pose Plan的FinalPublication

#### Scenario: Projection与Rig不匹配

- **WHEN** 当前Fixture、Projection和Rig identity不一致
- **THEN** Preview MUST停止并显示精确失配原因
- **AND** MUST不重新Build、选择近似Fixture或回退场景角色

### Requirement: PoseGraph Workspace必须保持显式发布边界

`Validate`与`Compile` MUST只调用PoseGraph现有轻量校验和编译入口，并把错误映射到Node、Port、State或Transition。`Character Build` MUST只调用Definition唯一正式发布事务。打开窗口、恢复窗口、选择Graph元素、修改字段、Undo/Redo、切换target、播放Preview、保存资产、asset import、refresh或domain reload MUST不自动执行Program Build、Projection Build、Foot Analysis或Motion Matching Database Build。

#### Scenario: 修改Structural字段

- **WHEN** 作者修改PoseGraph拓扑、Source Slot、Rig引用或其它Structural字段
- **THEN** Workspace MUST显示Build Required或Stale
- **AND** MUST等待作者明确点击Character Build

#### Scenario: Compile报告端口错误

- **WHEN** PoseGraph轻量Compile发现不兼容端口或缺失Output路径
- **THEN** Workspace MUST把错误映射到对应Node或Port并允许作者定位
- **AND** MUST不自动执行Character Build修复产物

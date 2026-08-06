## MODIFIED Requirements

### Requirement: 唯一领域框架必须从现有BTSMTL作者UI原地抽象

共享Canvas、Node View、Port View、Edge View、selection、Details host、StateMachine表面、创建菜单、clipboard、Undo与breadcrumb MUST以现有BTSMTL作者UI实现作为提取基线。系统 MUST通过抽取domain-neutral交互并注入document、capability、mutation与presenter完成共享化；MUST不新建功能更少的替代GraphView再切换BTSMTL入口。BTSMTL现有布局、节点信息、黑板变量拖拽、Flow/Property Port、节点搜索与创建、selection、框选、clipboard、Undo、Inspector、子树/StateMachine下钻和Live Debug行为 MUST保持。Character PoseGraph MAY使用不同于BTSMTL逻辑图的工作区排列，但 MUST复用上述交互和视觉能力，不得反向把Preview、动画输入或Pose字段加入BTSMTL Workspace。

#### Scenario: 拖出黑板变量

- **WHEN** 作者从现有Data Catalog把黑板变量拖到BTSMTL画布
- **THEN** 共享实现 MUST保留原拖拽手势、变量节点表现、Property Port和正式BTSMTL mutation语义
- **AND** PoseGraph工作区改造 MUST不改变该操作或BTSMTL布局

#### Scenario: 打开PoseGraph

- **WHEN** 作者从Presentation Profile打开现有PoseGraph
- **THEN** PoseGraph MUST复用同一Canvas交互、selection、Undo、创建菜单和StateMachine表面
- **AND** MUST不继承BTSMTL Gameplay数据、Data Catalog内容或整个Tree窗口布局

## ADDED Requirements

### Requirement: 跨领域节点投影必须复用BTSMTL Node Visual Chrome

Graph Authoring Framework MUST从现有`BaseNode.uxml`、`BaseNode.uss`、`NodePortContainer.uxml`与`NodePortContainer.uss`提供不依赖`BaseNode`序列化模型的共享Node Visual Chrome。`BaseNodeView`、Pose Node、Pose State、Alias与Entry投影 MUST通过该Chrome取得相同的节点边框、标题结构、字体、状态框、选择框、折叠与端口容器；domain adapter MAY提供业务标题、图标、颜色、badge和typed port，但 MUST不使用裸Unity GraphView Node、inline近似样式或复制一套USS。共享视觉 MUST不要求Pose数据继承`BaseNode`、`BaseGraph`或BTSMTL runtime node。

#### Scenario: 投影Pose节点

- **WHEN** Canvas从Pose document与Capability投影Foot Placement或Full Body IK节点
- **THEN** 节点 MUST使用BTSMTL Node Visual Chrome和NodePortContainer
- **AND** typed Pose与Goal端口 MUST继续显示各自稳定标签和颜色
- **AND** MUST不退化为Unity默认GraphView节点

#### Scenario: 打开Pose StateMachine

- **WHEN** Canvas投影Entry、State与Alias
- **THEN** 三类元素 MUST复用共享selection、状态与端口视觉并保留各自业务形状
- **AND** MUST不持有或伪造`BaseNode`数据

### Requirement: Domain Capability必须声明字段交互与应用语义

Graph Authoring Domain Capability MUST为可见作者字段提供稳定owner/field identity、`Structural | TunableDefault | RuntimeInput | DerivedReadOnly`策略和typed mutation/read模型。Tunable字段 MUST额外声明应用时点、状态语义、范围、单位与consumer identity。Details presenter与Live Tuning基础设施 MUST只消费这些声明，不得按具体节点类型、Profile类型、SerializedProperty路径或反射字段名决定字段是否可编辑或可运行时调整。内部owner identity与layout identity MUST默认不进入人工UI。

#### Scenario: 新Pose节点注册可调字段

- **WHEN** 新Pose节点或外部Profile通过Capability注册Tunable字段
- **THEN** 统一Details和Live Tuning基础设施 MUST能够投影其作者值、Applied值与应用状态
- **AND** MUST不要求新增节点专用窗口或全局参数表

#### Scenario: Capability遗漏交互策略

- **WHEN** 可发布字段没有明确InteractionPolicy
- **THEN** Validator MUST阻断发布和Live Tuning
- **AND** MUST不默认把字段当成Tunable或RuntimeInput

### Requirement: PoseGraph工作区必须保持一个Graph交互状态

PoseGraph角色画面、图导航、Details、Preview输入与runtime overlay MUST共享当前`GraphAuthoringCanvasView`的document、page stack和selection。任何区域 MUST不保存第二份Node、Edge、State、Transition、selection或Graph layout；运行目标切换 MUST只更换只读overlay和Applied数据绑定，不得替换作者Graph或打开runtime clone。

#### Scenario: 选择Pose节点

- **WHEN** 作者在Canvas选择一个Full Body IK节点
- **THEN** Details、角色画面overlay和runtime interest MUST使用同一个稳定PoseNodeId
- **AND** MUST不在Preview或Live面板创建第二份节点选择

#### Scenario: Live target失配

- **WHEN** 当前target的Projection revision不再匹配作者document
- **THEN** runtime overlay与Applied值 MUST立即清空并显示Stale
- **AND** 作者Graph、page stack和selection MUST保持不变

## ADDED Requirements

### Requirement: Pose只读Blackboard必须按正式输入范围投影

FlowCanvas原生Blackboard MUST展示当前Root、StatePose、Subgraph或Linked Pose入口可访问的正式输入，区分动画实例变量、只读角色事实和显式子图参数。共享实例变量 MUST引用事件图唯一声明、Contract和Layout，MUST不在每张图复制声明、初值或布局；公开子图入参和source-local曲线 MUST各自保持正式来源。节点配置与随Pose传播的曲线 MUST使用各自正式作者入口，不混入共享变量列表。

面板 MUST以作者名称、类型、来源、作用范围和使用情况为主要信息；内部稳定identity只用于引用、详情和诊断。合法未使用声明 MAY通过筛选展示，但筛选 MUST不改变正式声明或连接。拖拽 MUST通过原typed Mutation创建绑定正确声明的Get，PoseGraph主图 MUST不提供共享动画变量Set。重命名 MUST不改变稳定引用身份。

#### Scenario: 子图显示可访问输入

- **WHEN** 作者进入一个具有公开参数与共享动画变量读取权限的子图
- **THEN** 面板 MUST只列该接口和范围允许读取的声明，并显示各自来源
- **AND** MUST不复制根图全部变量或全部BlendShape曲线

#### Scenario: 变量声明尚未连线

- **WHEN** 一个可访问的合法输入尚未被节点消费
- **THEN** 作者 MUST仍能找到该声明并拖出Get，界面 MAY标记未使用
- **AND** 系统 MUST不因没有消费者删除声明或强制要求根Blackboard为空

#### Scenario: 输入跨范围或类型失配

- **WHEN** Get引用不可访问声明、已删除变量或类型不匹配的输入
- **THEN** 正式Mutation或原生图绑定 MUST在对应Graph/Node/Field返回明确诊断
- **AND** MUST不按显示名猜测另一个声明、创建默认变量或绕过正式接口

### Requirement: Pose消费必须使用同次成功发布的精确类型变量帧

Get、条件和BlendSpace MUST消费事件图同次成功发布的唯一typed变量Frame，Float、Int32、Bool保持精确类型，MUST不通过float表示Int32或Bool。帧 MUST匹配动画实例、表现采样、Simulation sample tick、Reset代际及图/Contract/Layout版本；消费者完成前输出 MUST保持不可重写，Pose节点及其内部算法不得引用可变Blackboard Variable对象。

Source Pending MUST不回退已成功更新的事件状态。Pose MUST保留原Pending/Committed、Fault和最终写入语义，MUST不把输入发布成功等同于Pose成功提交。CharacterAnimationInputContract的共享变量部分 MUST引用事件图唯一合同，原Fact、Slot、World和其它正式输入仍各自保留。

#### Scenario: 同次变量驱动条件与混合

- **WHEN** 一次事件更新成功发布Float、Int32和Bool供Get、条件与BlendSpace读取
- **THEN** 所有消费者 MUST使用同一次发布及同一布局，Int32/Bool不得发生float中转
- **AND** 任一实例、类型、采样或版本失配 MUST在现有输入交接边界明确失败，不补默认motor值

#### Scenario: 姿势Source暂未准备好

- **WHEN** 事件图已经成功更新变量，但本次Pose Source仍为Pending
- **THEN** 事件状态 MUST保持成功更新后的结果，Pose不得提交未完成姿势
- **AND** 后续更新 MUST按正式表现时间继续，不能把旧事件重跑或把输入发布记为Writer完成

### Requirement: Pose正式作者API必须支持完整C#重建与明确根挂接

Pose MUST提供正式对象、配置、曲线、布局、动态端口、资源和引用的读取及类型化修改能力，供公共C#输出/生成薄适配调用。MUST不经Agent Mapper/DTO、Document、JSON中转或整包Reconciler，也不得新增第二Pose模型、中央Validator或源码同步。正式局部与拓扑规则 MUST继续由Pose领域修改/原生图绑定入口拥有。

显式生成 MAY替换物理Unity对象，图/节点/变量引用的业务identity MUST重建一致；生成范围内引用 MUST使用本次新对象，Profile/Definition的明确根挂接 MUST恢复，MUST不依赖旧生成子资产GUID。人工编辑不得自动写源码，重新生成不得自动合并未导出修改；既有图/资源依赖失效与显式准备/采用 MUST保留，真实资源产品构建由原owner负责；生成操作不得自动Build或生成Pose Image。

#### Scenario: 删除生成图后重建

- **WHEN** 已编译创建代码通过公共generate_assets重建指定Pose范围
- **THEN** 节点、稳定Get引用、曲线、布局、动态端口和内部引用 MUST由新对象完整恢复，并挂回指定Profile/Definition
- **AND** 外部资源 MUST保持明确外部引用，旧生成子资产GUID不得成为内容来源

#### Scenario: Pose绑定事件图输入

- **WHEN** Pose作者面打开已绑定的 Character Animation Event Graph
- **THEN** Pose MUST只读取该事件图的唯一变量 Contract/Layout/Frame，并通过正式 Profile 引用保持同一动画宿主
- **AND** EventGraph内容修改 MUST由其原生作者API或公共 C# authoring入口负责，Pose Mutation MUST不携带Agent DTO或替代Document模型

### Requirement: 运行与完整Preview必须共享动画宿主和变量合同

完整Pose/角色Preview MUST沿运行相同的动画宿主和唯一变量Contract/Layout/Frame消费，不创建Preview私有变量更新器或默认motor输入。单资源查看 MUST使用原正式资源调参合同，不要求构造完整角色，也不保留CharacterPresentationProgramParameterFrame.FromDirect作为兼容桥。

Pose消费侧已迁移全部运行、Get、条件、BlendSpace与Preview旧签名，旧CharacterPresentationProgramParameterFrame及旧生产方法不得重新引入。观察窗口仍不拥有播放时钟或第二Pose执行器。

#### Scenario: 角色预览读取作者变量

- **WHEN** 正式角色Preview运行关联事件图并计算Pose
- **THEN** Preview MUST使用与运行相同的变量帧和实例/采样/Reset/版本检查
- **AND** 暂未取得正式变量帧时 MUST明确报告缺失，不能使用固定motor值补齐

#### Scenario: 查看单个动画资源

- **WHEN** 作者通过原正式资源查看入口调整单Clip或BlendSpace资源参数
- **THEN** 该入口 MUST继续使用资源自己的调参合同
- **AND** MUST不为该操作构造默认角色变量帧或恢复旧ProgramParameterFrame生产方法

### Requirement: 原生Pose必须提供分型准备和实际采用结果

Pose领域 MUST提供明确图/资源准备与Create/Replace操作。准备请求 MUST包含RequestId、GraphId/GraphRevision、Rig/资源绑定及版本、动画输入合同和Actor上下文；结果 MUST保留请求来源、共享Status和typed Missing/Invalid/Failed原因，只有Ready才包含合法PreparedBinding。Pending MUST来自真实未完成准备，缺失原生图不得由旧Image、旧总Projection或默认资源补齐。

只有Pose owner实际安装成功后才发布AdoptedResult，携带实际GraphRevision、资源版本、InstanceId和ResetGeneration；主实现仅调用与汇集，预览不得根据资产保存或Prepare成功生成假采用版本。准备失败 MUST不改写旧实例采用身份。显式Replace MUST按正式边界重建实例和重置历史，Stop/Dispose先阻止旧调用并完成在途工作，再失效结果与释放资源；旧generation结果不得写新实例。

#### Scenario: 图已准备但尚未安装

- **WHEN** 资源准备返回Ready但角色装配尚未安装该图实例
- **THEN** 系统 MUST只报告Prepared，不得报告Adopted或增加fake ProgramEpoch
- **AND** 实际采用结果 MUST由Pose owner安装成功后发布

#### Scenario: 新图准备失败

- **WHEN** 请求新版本图但正式资源缺失或端口/引用非法
- **THEN** 准备 MUST返回带来源和字段/资源的typed原因，旧实例实际采用身份不变
- **AND** MUST不生成加载期Image或切回旧Program读取路径

#### Scenario: 替换后旧结果到达

- **WHEN** 原生图替换已停止旧实例而旧generation仍有晚到结果
- **THEN** 结果 MUST因实例/Reset代际失配被拒绝，停止结果不得伪称旧实例仍活动
- **AND** 在途算法和资源释放 MUST由其原owner完成，不新增第二Writer

### Requirement: 原生Pose必须以明确阶段接口交付完整帧结果

共享表现壳 MUST由主实现沿已有时钟调用Pose的PrepareFrame、Evaluate、Commit/Discard和Stop接口，不生成节点遍历表或解释NodeKind。Pose MUST通过同一FlowCanvas原生节点/连接在准备阶段得到活跃分支和source demand，再在唯一Animancer Barrier完成后通过求值阶段取得同次样本、曲线和约束结果。准备结果 MUST不冒充最终姿态，Pose不得再次Evaluate同一PlayableGraph。

每个节点 MUST按Actor、图调用实例、求值身份和阶段复用实际结果；共享输入不得导致Player、状态转换、源采样或Foot/FBBIK重复执行。输入缓冲只读，修改节点使用实例复用输出；缓存不得成为全图Operation/IR/调度计划，也不得复制Source/IK私有状态。Source、当前正式Constraint和Final Publication分别保持唯一业务owner。

完整候选结果与主帧允许提交的令牌进入Commit，最终骨骼只由Final Publication写入。Barrier前失败 MUST按原规则Discard，Barrier内/后或Writer失败按原Fault边界处理，不提交半更新Pose历史，也不承诺回滚已发生的物理写入；EventGraph成功发布的状态不因Pose Pending/Discard回退。节点观察只读取允许发布的同次完成结果，不为旧SourceMap构造Image。

准备/采用状态 MAY分别展示；Pose Watch的姿态、曲线和约束结果 MUST在本帧成功提交后发布，候选求值不得冒充已提交Pose或覆盖最后合法观察结果。

#### Scenario: 一个Player被两条分支读取

- **WHEN** 同一调用实例的同一阶段重复读取Player输出
- **THEN** 原生节点 MUST返回同一求值结果，播放时间及采样需求只推进一次
- **AND** 不同Actor、子图调用、下一求值或另一阶段不得错误使用该缓存

#### Scenario: 源需求已经准备但Barrier尚未完成

- **WHEN** PrepareFrame返回合法需求而采样Barrier尚未完成
- **THEN** Evaluate MUST不能把准备句柄当完成Pose使用，Final Publication不得写骨骼
- **AND** 主壳只调正式阶段接口，不生成替代节点计划

#### Scenario: 约束阶段失败

- **WHEN** 当前IK正式接口在原生Pose求值中返回失败
- **THEN** Pose MUST按实际阶段返回失败并阻止不完整结果提交，IK状态仍由原owner管理
- **AND** 不复制IK状态、不重跑另一求解器、不用默认姿态掩盖错误

## MODIFIED Requirements

### Requirement: Pose参数必须通过typed页面和显式解析传播

Pose Graph MUST分别引用事件图稳定变量引用、正式Fact引用、子图接口参数和source-local曲线引用。动画实例变量的声明、初值、更新及Contract/Layout/Frame MUST由原生事件图唯一拥有，PoseGraph主图只消费其同次成功发布值，不提供共享变量Set、不复制声明或运行时Blackboard。角色表现事实 MUST只从正式同帧Fact合同读取。

source-local曲线参数 MUST随指定Pose Value传播；既有显式`Base | Overlay | Weighted | Max | Min`解析和适用Inertialization响应 MUST保持。曲线与BlendShape MUST不作为每张图重复拥有的外部变量声明。原生图绑定与节点 MUST区分输入变量与Pose曲线的读取来源、类型、作用范围和阶段依赖，并完整解析正式输入与曲线资源；MUST不通过清空root.Parameters省略仍被运行时消费的数据。节点 MUST不按字符串、GameplayTag或State显示名查找运行参数。

普通混合的Curve策略 MUST配置在实际组合节点，内部解析由实际组合节点调用原曲线算法完成，不强制作者连接Pose Parameter Resolve。明确的独立Curve修改能力 MAY保留，但不得恢复通用内部数据汇总作者节点。脚权重保持正式曲线或明确公开输入来源，不能因隐藏内部operation补造默认结果。

#### Scenario: 权重连接参数

- **WHEN** 作者把正式参数接到Alpha
- **THEN** 原生图绑定 MUST确定类型、作用范围与来源，节点读取对应同次typed输入

#### Scenario: 两份Pose需要不同Curve组合

- **WHEN** 作者在组合节点选择Curve混合策略
- **THEN** 原生组合节点 MUST按该策略传播曲线，观察定位到同一节点和调用实例

#### Scenario: Blend权重读取Program参数

- **WHEN** BlendPose权重连接ProgramParameterInput
- **THEN** 原生图绑定 MUST校验正式变量引用、类型、可访问范围和事件图唯一layout
- **AND** Runtime MUST只读取同一动画实例提供的值，不读取可变Gameplay对象或建立第二变量表

#### Scenario: Body读取混合后的脚权重

- **WHEN** Body内部FootPlacement权重绑定输入Pose的正式曲线
- **THEN** FootPlacement MUST读取该Pose上游混合后的同一曲线值
- **AND** 根图无需为了内部曲线读取复制变量声明；有真实外部控制需求时 MAY由Body显式公开输入
- **AND** 权重 MUST保持既有Goal可见权重作用，不释放Anchor、不清零连续历史、不改变Landing Reach准入

#### Scenario: 从Blackboard移除BlendShape属性投影

- **WHEN** 动画属性不再被投影为每张PoseGraph的变量
- **THEN** 正式曲线采样、混合、资源/实例绑定与最终BlendShape写入 MUST继续由原唯一链路完成
- **AND** 系统 MUST不丢弃仍被消费的曲线或用默认值掩盖缺失依赖

#### Scenario: 为动画片段声明面部形变参数

- **WHEN** AnimationClip声明动画属性曲线且Profile映射到明确Renderer／Mesh／BlendShape
- **THEN** 资源绑定 MUST把曲线、目标与容量纳入同一正式集合
- **AND** Pose Graph MUST不把该属性复制成可写Blackboard变量或按作者显示名读取

#### Scenario: 骨骼分层后需要保留另一分支曲线

- **WHEN** 作者在实际组合节点为两份Pose选择明确Curve混合策略
- **THEN** 组合节点 MUST保留骨骼混合职责并按原曲线算法解析参数
- **AND** MUST不要求额外通用汇总节点或创建第二条动画播放链

### Requirement: PoseStateMachine必须是纯表现状态机

PoseStateMachine MUST拥有稳定Entry、State、Transition、State Alias和MaxTransitionsPerFrame。Transition Rule MUST只读取同帧CharacterPresentationFactFrame、同次成功发布且作用范围允许的typed动画变量Frame、TimeInState与StatePoseRemainingTime；MUST不读取Gameplay Blackboard mutable address、ActionInstance、Timeline operation、Unity Transform或World query。State Alias MUST只复用合法source State集合，不得拥有Pose或成为active runtime State。PoseStateMachine MUST由原生Pose实例中的业务状态节点执行，不进入Gameplay Semantic IR、Numeric Program或角色网络快照，也不以通用插件FSM替代已有动画状态语义。

#### Scenario: Idle进入Locomotion

- **WHEN** typed HorizontalSpeed Fact满足Transition Rule
- **THEN** PoseStateMachine MUST按priority和stable order选择唯一target
- **AND** Gameplay MUST不发送PlayRun事件

#### Scenario: 同帧多个Transition成立

- **WHEN** 多条可达Transition Rule同时为true
- **THEN** Runtime MUST遵守正式priority、stable order和MaxTransitionsPerFrame
- **AND** MUST不依赖容器遍历顺序

#### Scenario: 动画变量参与转换条件

- **WHEN** 当前作用范围的Bool、Int32或Float动画变量被条件Get读取
- **THEN** 条件 MUST使用与本次Get/BlendSpace相同的成功发布Frame并保持精确类型和短路读取语义
- **AND** 引用不得转成Gameplay可变地址，不得读取另一实例、旧Reset代际或未发布值

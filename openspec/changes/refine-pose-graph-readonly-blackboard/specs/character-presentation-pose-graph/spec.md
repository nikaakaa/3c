## ADDED Requirements

### Requirement: Pose只读Blackboard必须按正式输入范围投影

FlowCanvas原生Blackboard MUST展示当前Root、StatePose、Subgraph或Linked Pose入口可访问的正式输入，区分动画实例变量、只读角色事实和显式子图参数。输入来源和跨图可见性 MUST由唯一声明与接口合同提供，MUST不在每张图复制共享声明。节点配置与随Pose传播的曲线 MUST使用各自正式作者入口，不混入共享变量列表。

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
- **THEN** Mutation或Compiler MUST在对应Graph/Node/Field返回明确诊断
- **AND** MUST不按显示名猜测另一个声明、创建默认变量或绕过正式接口

## MODIFIED Requirements

### Requirement: Pose参数必须通过typed页面和显式解析传播

Pose Graph MUST通过正式合同引用稳定ParameterId、类型、默认值与允许来源。动画实例变量的声明与更新 MUST由同一正式动画变量owner提供，PoseGraph主图 MUST只读取该实例交接的值，不提供共享变量Set、不复制运行时Blackboard；EventGraph的事件、Set和生命周期不由PoseGraph消费侧另行定义。角色表现事实 MUST只从正式同帧Fact合同读取。

source-local曲线参数 MUST随指定Pose Value传播；既有显式`Base | Overlay | Weighted | Max | Min`解析和适用Inertialization响应 MUST保持。曲线与BlendShape MUST不作为每张图重复拥有的外部变量声明。Compiler MUST区分输入变量与Pose曲线的读取来源、类型、作用范围和执行依赖，并完整收集可达输入及曲线布局；MUST不通过清空root.Parameters省略仍被运行时消费的数据。节点 MUST不按字符串、GameplayTag或State显示名查找运行参数。

#### Scenario: Blend权重读取Program参数

- **WHEN** BlendPose权重连接ProgramParameterInput
- **THEN** Compiler MUST校验ParameterId、类型、可访问范围和正式page layout
- **AND** Runtime MUST只读取同一动画实例提供的值，不读取可变Gameplay对象或建立第二变量表

#### Scenario: Body读取混合后的脚权重

- **WHEN** Body内部FootPlacement权重绑定输入Pose的正式曲线
- **THEN** FootPlacement MUST读取该Pose上游混合后的同一曲线值
- **AND** 根图无需为了内部曲线读取复制变量声明；有真实外部控制需求时 MAY由Body显式公开输入
- **AND** 权重 MUST保持既有Goal可见权重作用，不释放Anchor、不清零连续历史、不改变Landing Reach准入

#### Scenario: 从Blackboard移除BlendShape属性投影

- **WHEN** 动画属性不再被投影为每张PoseGraph的变量
- **THEN** 正式曲线采样、混合、编译布局与最终BlendShape写入 MUST继续由原唯一链路完成
- **AND** 系统 MUST不丢弃仍被消费的曲线或用默认值掩盖缺失依赖

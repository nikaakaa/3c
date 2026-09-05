## Purpose

定义作者如何在 SkillProgram Root、技能局部 Graph、TreeClip/Timeline 与 Character 工作区内配置角色相机资源、编排技能镜头、编辑曲线、预览表现并定位运行来源，确保人工编辑和 Agent 修改进入同一正式数据与事务链。C# Locomotion 控制拓扑不提供 Camera 图节点，也不维护平行控制图。

## ADDED Requirements

### Requirement: 角色相机配置必须由正式 Profile 装配

角色 Definition MUST只引用正式 Camera Profile；Profile MUST装配默认相机序列、角色轨道/球面配置、锁定规则、响应规则、目标槽位和强类型 Sequence、Override、Zoom、Stretch、Shake、Shot、Curve 资源闭包。每个作者字段 MUST明确其真实 owner、单位、坐标空间、时间域、资源类型和消费者。相机参数 MUST不内联复制进 Definition、场景组件和技能控制脚本形成多个真相。

#### Scenario: 作者配置日常跟随

- **WHEN** 作者调整角色默认构图、距离、俯仰或响应
- **THEN** 修改 MUST落到选定 Profile 或其明确引用的资源
- **AND** 依赖视图 MUST说明哪些角色和技能引用该资源

#### Scenario: 创建技能镜头效果

- **WHEN** 作者创建一个 Zoom 或 Shake 资源
- **THEN** 编辑器 MUST展示该类型实际支持的字段、单位、时间规则和曲线入口
- **AND** MUST不要求作者填写 C# 类型名、Cinemachine 实例名称或万能字符串参数

### Requirement: 相机编辑必须接入现有工作区与唯一 Mutation

相机资源目录、Details、引用导航和诊断 MUST通过现有 Character 工作区以及 SkillProgram/技能局部 Graph/TreeClip/Timeline 的领域扩展进入共享 Shell；Gameplay 图 MUST继续使用已有画布、搜索、端口、selection 和 Undo。CameraSequence MUST作为有限相机算法及组合的作者资源编辑，不创建第二套 GraphView、Workbench、动画 Sequence 或 Timeline Sequence 模式。所有编辑、创建、删除、引用替换和导入 MUST经同一正式相机 Mutation 与资产事务。

#### Scenario: 从技能节点打开镜头资源

- **WHEN** 作者选中引用 CameraSequence 的技能局部 Graph 节点、TreeClip 或 Timeline Clip
- **THEN** Details MUST提供精确资源选择和打开资源/返回引用处的导航
- **AND** MUST保持技能局部 Graph/TreeClip/Timeline 与资源各自的真实 owner 和 Undo

#### Scenario: 撤销共享资源修改

- **WHEN** 作者修改共享 Override 轨道后执行 Undo
- **THEN** Undo MUST恢复该真实资源及同一事务中的引用变化
- **AND** 其它引用者 MUST读取恢复后的同一资产，不从窗口副本恢复

### Requirement: SkillProgram与TreeClip/Timeline必须提供可编译的强类型镜头编排

SkillProgram Root 与技能局部 Graph MUST提供序列请求、目标/响应设置、效果触发和 basis 读取的正式能力；TreeClip/Timeline MUST提供有持续生命周期的 Sequence/Override/Zoom/Stretch/Shot 编排和按事件时点触发的 Shake。资源选择 MUST通过 Profile 允许的强类型引用，目标 MUST通过已声明槽位或正式上下文选择；完整 capability MUST同时覆盖创建菜单、字段、端口、校验、Compiler 和 Document。C# Locomotion 控制拓扑不得提供 Camera 图节点。缺少运行支持的类型 MUST不能作为可成功发布的作者能力。

#### Scenario: 编排完整技能镜头

- **WHEN** 作者在技能 Timeline 添加 Override、Zoom、Stretch 和多个 Shake 时点
- **THEN** 每个元素 MUST显示其资源、时点或区间、进入/退出与中断规则，以及同帧顺序
- **AND** 所有输出 MUST进入同一已提交相机请求链

#### Scenario: 选择不匹配资源

- **WHEN** 作者尝试为 Zoom Clip 绑定 Shake 资源或未装配的目标槽位
- **THEN** 资源选择或正式校验 MUST拒绝操作并定位对应 Clip/字段
- **AND** MUST不在运行时转换类型或寻找默认资源

### Requirement: 曲线必须按照真实所有权编辑

Timeline-local Weight/Ease MUST继续由对应 Clip 拥有并使用已有 Curve Lane；共享的相机转场、FOV、空间变化和震动曲线 MUST由正式 Camera Curve 或相应效果资源唯一拥有，并复用现有曲线交互基础设施。每条 Channel MUST声明时间域、值域、单位和唯一 Mutation owner。Timeline引用共享曲线时 MUST提供只读显示和打开真实 owner 的入口，不创建可独立修改的隐式副本。

#### Scenario: 调整技能 Clip 权重

- **WHEN** 作者在 Timeline 修改该 Clip 的 Weight/Ease
- **THEN** 修改 MUST只作用于 Clip 自己的归一化曲线并进入原 Timeline Undo
- **AND** MUST不修改共享效果曲线

#### Scenario: 调整原版进入曲线

- **WHEN** 作者从 Override 打开其共享进入曲线
- **THEN** 编辑器 MUST在真实 Curve owner 下显示完整关键点和插值语义
- **AND** 保存后所有引用者 MUST统一变为需要重建的状态

### Requirement: 作者必须能明确构建并观察产物状态

导入、迁移、依赖解析、Program/Projection Build 和预览准备 MUST由明确命令触发。selection、Inspector 绘制、窗口恢复、资源刷新和字段重绘 MUST不执行这些重操作。作者 MUST能看到配置错误、缺失依赖、未重建和可运行状态，并跳转到精确 owner；修改后的参数不得通过直接写运行相机掩盖未重建状态。

#### Scenario: 修改相机 FOV

- **WHEN** 作者修改 Camera Profile 的 FOV
- **THEN** 编辑器 MUST保存真实作者配置并显示 Projection 已过期
- **AND** MUST不在 Inspector 绘制时构建或直接写活动相机

#### Scenario: 明确执行 Build

- **WHEN** 作者通过精确 Definition 执行正式 Character Build
- **THEN** 相机计划 MUST与同一发布组中的请求合同和其它表现资源一起校验、发布
- **AND** 失败 MUST定位资源或节点，旧发布组 MUST保持完整

### Requirement: 相机诊断必须能从画面结果返回作者来源

工作区与 Timeline 的只读 Live Debug MUST显示活动序列、胜出与被压制请求、资源身份、producer/generation、效果阶段、使用的时间、输入权重、目标、混合起终点、碰撞修正、最终镜头和 basis。诊断 MUST来自正式运行快照，并通过 source map 导航到 Graph、Timeline、Profile 或效果资源；没有匹配实例或 Projection 时 MUST显示不可用或过期，不在 UI 中重新求值。

#### Scenario: 技能结束后镜头仍在退出

- **WHEN** 动作请求已经退休但镜头效果按规则仍处于退出阶段
- **THEN** Live Debug MUST分别显示请求退休状态和效果退出剩余时间、来源及规则
- **AND** 作者 MUST能跳转到对应退出曲线或 Clip

#### Scenario: 作者切换到 Live Debug

- **WHEN** 当前工作区开始观察真实运行镜头
- **THEN** Preview MUST停止占用同一物理输出，Live MUST仅使用匹配的真实快照
- **AND** 切换页签 MUST不改变相机状态或资产 revision

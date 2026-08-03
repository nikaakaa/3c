## ADDED Requirements

### Requirement: Pose Graph Producer Navigator必须从显式Definition上下文投影

Pose Graph Producer Navigator MUST要求精确`CharacterPipelineDefinition`上下文，并调用唯一`CharacterAnimationPresentationAuthoringService`从该Definition的composition roots递归发现可达Graph、Timeline、AnimationTrack和stable producer identity。Navigator MUST按AnimationChannel和source owner分组显示producer、Clip identity、Sync模式与可用导航，不得扫描目录、读取generated Program/Projection完成bootstrap、按显示名或列表index猜测，也不得保存第二份producer binding或flow。

#### Scenario: 查看BaseLocomotion producers

- **WHEN** 作者从Corin Definition上下文打开Pose Graph并展开BaseLocomotion
- **THEN** Navigator MUST列出该Definition正式可达的Idle、Start、Loop、Turn与End producer identity
- **AND** 每个条目 MUST精确定位其Timeline、Track和Clip owner
- **AND** Pose Graph资产 MUST不因展开、搜索或定位而变脏

#### Scenario: 缺少Definition上下文

- **WHEN** 作者直接打开shared Pose Graph且没有精确Definition call-site context
- **THEN** Producer Navigator MUST显示Unavailable及缺失上下文原因
- **AND** MUST不搜索使用该图的任意角色或使用上一次窗口context

### Requirement: 跨资产表现配置必须保持唯一写入口

Pose Graph Navigator、Details与Bottom Dock MAY只读显示AnimationTrack的Clip、SyncGroup、Topology、SyncRole、Marker，Profile/Rig/Policy owner以及generated Foot Analysis状态。修改Clip、Marker和registered Curve MUST精确导航到Timeline Editor；修改Profile、Rig、Blend Policy、Inertialization Policy和Analysis Source MUST精确导航到各自正式Inspector。Pose Graph Workspace MUST不复制这些字段、直接写SerializedProperty或提供第二mutation命令。

#### Scenario: 从Sync面板调整脚接触Marker

- **WHEN** 作者在Pose Graph Sync面板查看WalkLoop与RunLoop Marker
- **THEN** 面板 MUST保持只读并提供Open Source Timeline
- **AND** Timeline Editor MUST成为移动Marker的唯一正式入口
- **AND** Pose Graph与Profile MUST不保存Marker副本

### Requirement: Animation authoring工作区不得自动发布generated产物

打开Profile、Pose Graph或Timeline，选择producer、切换Details页签、修改authoring、切换Preview Target、保存资产、窗口focus、domain reload和AssetDatabase refresh MUST不自动执行Program Build、Projection Build、Foot Analysis batch或Motion Matching Database Build。工作区 MUST显示Dirty、Invalid、Stale、Ready或显式Building状态，只有明确Compile/Build命令 MAY调用现有正式发布事务。

#### Scenario: 选择Stale producer

- **WHEN** 作者在Navigator选择一个Projection已Stale的producer
- **THEN** Details MUST显示Stale来源与受影响revision
- **AND** 系统 MUST不因selection自动重建Projection或Foot Analysis


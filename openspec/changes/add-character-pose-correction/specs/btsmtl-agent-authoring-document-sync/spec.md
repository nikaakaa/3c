## MODIFIED Requirements

### Requirement: 文档包必须分离可编辑authoring、只读context与service基线

文档包 MUST包含service-owned `manifest.json`与`.sync.json`、AI可编辑`editable/`和service-owned只读`context/`。`.sync.json` MUST只保存base source revision、base editable hash与base context hash，不得保存业务authoring。Character Presentation的Profile、Pose Graph、PoseStateMachine、direct Clip Binding、Locomotion Sync Group、AnimationSlot与Policy MUST进入`editable/presentation/`；静态Correction Set、Correction Binding及Graph-owned Correction Slot MUST进入已有Profile/Graph分片中的类型化集合。当前Definition可达原生AnimationClip注册Curve MUST进入`editable/animation-clips/`；Rig资源正文、Body Motion、Foot Analysis generated data、runtime state、Projection与Native Program MUST只进入紧凑只读context或完全省略。样本中的BoneId和目标姿态只表示作者修正数据，不授予修改Rig或Clip骨骼曲线的能力。

#### Scenario: AI读取Character文档包

- **WHEN** checkout导出Character Controller
- **THEN** editable MUST表达Agent正式可写的Graph、StateMachine、Condition、Timeline、Blackboard、Action、Presentation与Clip Curve结构
- **AND** context MUST只读表达Node/Graph schema、可引用asset、dependency与必要能力摘要
- **AND** 文档包 MUST不暴露Unity YAML、managed-reference布局或私有SerializedProperty path

#### Scenario: AI尝试修改只读context

- **WHEN** context文件semantic hash与checkout基线不同
- **THEN** parser或Reconciler MUST返回`readonly_context_modified`
- **AND** MUST不把变化降低为Mutation

#### Scenario: AI编辑方向样本

- **WHEN** AI在Profile的正式Correction Set集合内替换某样本的骨骼目标姿态
- **THEN** 文档 MUST把它识别为样本作者修改
- **AND** Rig正文、原动画骨骼曲线和生成常量 MUST继续只读

### Requirement: Presentation Reconciler必须调用唯一Presentation Mutation

Document v4 Reconciler MUST按owner依赖生成类型化Presentation Mutation计划，并与人工编辑共用validator、资产级transaction、子资产identity allocator、dirty owner与诊断。Source Slot、direct Clip/Blend Space/MM Binding、Locomotion Sync Group、Correction Slot、Correction Set/Binding、Pose Graph和PoseStateMachine的创建、修改、引用与删除 MUST在同一个正式资产事务中处理；Reconciler MUST不直接写Unity YAML、SerializedObject path、generated Projection或第二份字符串binding。

#### Scenario: apply新增Clip Source Slot与binding

- **WHEN** 文档目标状态新增Graph-owned Source Slot、Profile-owned direct Clip Binding并让ClipPlayer引用该Slot
- **THEN** Reconciler MUST按子资产创建、binding配置、Player引用与owner保存顺序生成类型化Mutation
- **AND** 任一失败 MUST回滚全部子资产、数组、节点引用、Gameplay、Timeline、Clip与Presentation变化

#### Scenario: apply修改Locomotion Sync Group

- **WHEN** 文档目标状态调整Group中的原生AnimationClip成员
- **THEN** Reconciler MUST使用结构化Clip引用生成Profile Mutation并校验成员唯一性
- **AND** MUST不修改Clip Curve或自动Build Projection

#### Scenario: 同次创建完整修正配置

- **WHEN** Document目标同时新增local样本集、Slot、Binding和引用它们的节点
- **THEN** dry-run MUST产生完整计划，apply MUST在同一事务中生成真实身份并反向导出
- **AND** 任何一步失败 MUST恢复全部相关owner和正式package

## ADDED Requirements

### Requirement: 修正样本必须完整往返且不成为操作指令

Document v4 MUST用既有Profile/Graph分片表达中性参考、样本、骨骼绑定、轴合同与节点输入的完整目标状态。新增集合 MUST由同一Capability、strict codec、Reconciler、Mutation和Validator闭合；未知字段、非法数值、重复身份、悬空引用和缺少必要样本 MUST被拒绝。提取出处 MUST只作为数据保留，不能被解释为执行采样、运行Preview或Build的命令；不得增加Pose专用MCP、manifest外样本目录或分片apply。

#### Scenario: 删除仍被引用的样本集

- **WHEN** Document移除样本集但保留引用该集合的Binding
- **THEN** dry-run MUST报告悬空引用并拒绝整包计划

#### Scenario: 只修改取帧出处

- **WHEN** Document只修改允许编辑的提取出处而不替换目标姿态数据
- **THEN** Reconciler MUST不执行Clip采样或改变骨骼目标
- **AND** 运行样本数学 MUST仍来自已声明的完整目标姿态

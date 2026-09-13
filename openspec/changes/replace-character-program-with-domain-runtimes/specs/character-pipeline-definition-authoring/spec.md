## MODIFIED Requirements

### Requirement: CharacterPipelineDefinition 必须是配置装配根

角色 Definition MUST只保存明确的控制模块／配置、AbilityGrants、Input、BodyMotion、Effect、Action／Behavior、Equipment、动画 Profile 与 Camera 资源引用，不内联技能或表现规则。Ability MUST独立拥有规则与私有图，Grant 只保存授予和绑定。Definition MUST不再保存生成的整角色 Program 或 Projection 引用，不要求合法技能先依赖某角色表现配置。

#### Scenario: 打开独立Gameplay Ability

- **WHEN** 作者选择一个GameplayAbilityDefinition
- **THEN** Inspector MUST能直接编辑该Ability的规则、资源、结束规则、后续Ability和私有AbilityGraph入口
- **AND** MUST不要求作者再到CharacterPipelineDefinition拼装SkillDefinition、SkillGraph或空ActionContext

#### Scenario: 打开角色Definition

- **WHEN** 作者选择Corin CharacterPipelineDefinition
- **THEN** Inspector MUST优先显示角色引用的正式Config
- **AND** MUST不平铺PoseState、AnimationSlot、Pose节点、transition matrix、producer binding或领域内容状态

#### Scenario: 缺失动画表现Profile

- **WHEN** Definition没有CharacterAnimationPresentationProfile引用
- **THEN** 角色配置校验与实例准备 MUST报告明确错误
- **AND** 系统 MUST不创建内联Profile、默认Pose Graph或从Blend Library猜测配置


### Requirement: Definition Inspector 必须分离作者配置与生成产物

Definition Inspector MUST以正式配置引用为主；技能产物、资源状态和诊断 MUST按领域显示。Selection、OnEnable、Layout、Repaint 和 foldout MUST只读轻量引用或会话缓存，不运行完整构建、decode、依赖扫描或 stale 计算。状态 MUST区分 Missing、Invalid、Unchecked、Ready、Stale 和需要处理的领域；Unchecked 不得显示为 Ready。显式 Refresh Status 才进行对应范围检查，字段变化只使实际受影响范围失效。系统 MUST不再提供整角色 Program／Projection 的生成入口。

#### Scenario: 选择 Definition

- **WHEN** 作者选择或重新选择 CharacterPipelineDefinition
- **THEN** Inspector MUST只根据 serialized reference 与轻量发布 Header 显示 `Missing`、`Invalid` 或 `Unchecked`
- **AND** MUST不计算当前 SourceRevision、解码 Program 或重算 ProjectionRevision

#### Scenario: 重绘 Inspector

- **WHEN** Unity 对已打开的 Definition Inspector 执行 Layout、Repaint 或 foldout 切换
- **THEN** Inspector MUST只绘制当前会话状态
- **AND** MUST不调用 `IsStale`、Compiler、技能产物decode 或任何对应领域 dependency hash 入口

#### Scenario: 显式刷新产物状态

- **WHEN** 作者点击 `Refresh Status`
- **THEN** Inspector MUST执行一次正式完整 stale 检查
- **AND** MUST将结果缓存为 `Ready` 或 `Stale`，后续 Repaint MUST不重复该检查

#### Scenario: 修改 Definition

- **WHEN** 作者通过当前 Inspector 修改影响某领域的 Definition authoring 字段
- **THEN** Inspector MUST立即显示 `Needs Compile`
- **AND** MUST不为更新状态自动运行 Compiler 或 stale 检查

#### Scenario: 编译产物

- **WHEN** 作者显式构建技能或资源 且对应领域处理 成功
- **THEN** Inspector MUST显示 `Ready`
- **AND** Build失败时 MUST不显示虚假的 `Ready`

#### Scenario: 查看生成产物详情

- **WHEN** 作者显式展开 Generated Artifacts 或运行 Compiler Diagnostics
- **THEN** Inspector MAY显示 技能产物／表现资源 identity、Hash、capability 与 report
- **AND** foldout绘制本身 MUST不触发完整 stale 检查
- **AND** Compiler Diagnostics MAY按显式命令运行完整 dry-run


### Requirement: Animation Presentation Profile 必须是唯一表现配置资产

动画 Profile MUST继续唯一装配原生 Pose 图、Rig／Mask／Policy、FullBodyIK、源资源、有限动作绑定与分析资源引用。原生图 MUST唯一表达 PoseState、Player、Slot、Blend、空间转换、Control Rig 与目标求解拓扑，实例绑定 MUST从这些正式引用解析资源。Definition、技能图、Timeline、Prefab、运行节点或编辑窗口 MUST不复制角色级配置；不再要求编译 Projection 或 Image。内部必要目标打包由对应运行节点和 Constraint 模块负责，不暴露为作者必接的编译步骤。

#### Scenario: 一个Profile被一个Definition引用

- **WHEN** 作者选择CharacterAnimationPresentationProfile
- **THEN** Profile Inspector MUST提供Pose Graph、Clip source、Action producer binding、Locomotion Sync Group、Policy、Rig、FullBodyIK和Foot Analysis唯一入口
- **AND** Definition Inspector MUST不内联这些字段

#### Scenario: Action producer解析Foot Analysis

- **WHEN** 角色资源准备编译一个直接AnimationClip的有限Action producer
- **THEN** Compiler MUST从Profile Analysis Source、角色Rig与Clip Analysis Input Hash解析Artifact
- **AND** Action producer binding MUST不保存Foot Analysis identity副本

#### Scenario: Definition Inspector显示Projection状态

- **WHEN** 作者只选择CharacterPipelineDefinition
- **THEN** Inspector MUST只显示Animation Presentation Profile引用与Projection Ready/Stale/Missing摘要
- **AND** MUST不运行Pose Graph Compiler或内联显示node、Clip、Group或mask参数


### Requirement: Body Motion Profile 必须是唯一垂直动力作者配置

角色 MUST显式引用唯一 BodyMotion 配置，保存有限负数重力和有限正数最大下落速度。配置 MUST由运动模块在创建时按数值目标准备，进入角色玩法内容身份与能力要求，不进入技能操作／常量。Host、Scene、Network、WorldSolver、Blackboard MUST不保存第二份配置或补默认。

#### Scenario: Definition缺少Body Motion Profile

- **WHEN** 作者尝试创建缺少Profile的CharacterPipelineDefinition
- **THEN** 配置校验与角色实例准备 MUST明确失败
- **AND** Runtime MUST不创建默认Profile或按Solver补值


### Requirement: Character Definition 必须通过两个配置引用安装Equipment能力

`CharacterPipelineDefinition` MUST只保存可选的`CharacterEquipmentProfile`、`CharacterEquipmentPresentationProfile`引用与Equipment capability声明，不得内嵌Slot、Route、Equipment、Feature、Loadout、visual binding或generated catalog。前者唯一拥有Gameplay装备配置，后者唯一拥有Unity visual binding。Inspector MUST把二者作为纯配置引用显示；生成的技能产物、表现资源与catalog详情 MUST进入只读诊断，不得在Definition主Inspector展开为可编辑副本。

#### Scenario: 为Corin安装Equipment Profile

- **WHEN** 作者在Corin Definition启用Equipment capability
- **THEN** Inspector MUST要求精确选择一个Gameplay Equipment Profile和一个Equipment Presentation Profile
- **AND** Slot/item/Feature与visual binding MUST分别在对应正式Inspector中完成

#### Scenario: Definition展开generated装备表

- **WHEN** 作者选中CharacterPipelineDefinition
- **THEN** Inspector MUST不序列化或绘制第二份generated Equipment catalog
- **AND** 编译状态 MAY以只读摘要显示

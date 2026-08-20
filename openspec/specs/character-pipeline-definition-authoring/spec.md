# character-pipeline-definition-authoring Specification

## Purpose

定义 CharacterPipelineDefinition 作为角色 authoring 配置装配根的纯引用边界、紧凑 Inspector，以及 Animation Presentation Profile 与 generated Program/Projection 的所有权和状态入口。
## Requirements
### Requirement: CharacterPipelineDefinition 必须是配置装配根

`CharacterPipelineDefinition` MUST只保存RootTree、SimulationTickRate、InputProfile、GameplayEffectProfile、ActionProfile、GameplayBehaviorProfile、CharacterAnimationPresentationProfile与generated Program/Projection的正式引用。Definition MUST不内联保存Animation Channel、PoseStateMachine、AnimationSlot、Pose Graph、Policy、Rig、producer binding、Graph、Timeline、runtime lifecycle或compiler report数据。

#### Scenario: 打开角色Definition

- **WHEN** 作者选择Corin CharacterPipelineDefinition
- **THEN** Inspector MUST优先显示角色引用的正式Config
- **AND** MUST不平铺PoseState、AnimationSlot、Pose节点、transition matrix、producer binding或Program Hash

#### Scenario: 缺失动画表现Profile

- **WHEN** Definition没有CharacterAnimationPresentationProfile引用
- **THEN** configuration validation与Compiler MUST报告明确错误
- **AND** 系统 MUST不创建内联Profile、默认Pose Graph或从Blend Library猜测配置

### Requirement: Definition Inspector 必须分离作者配置与生成产物

Definition Inspector MUST以紧凑 Config References 作为默认作者界面。Program/Projection 引用、identity、Hash、capability 与 compiler report MUST属于 Generated Artifacts/Diagnostics 区域。Inspector selection、`OnEnable`、Layout、Repaint 和 foldout 切换 MUST只读取 serialized reference、轻量发布 Header 或当前 Inspector 会话缓存，MUST不运行 Compiler、完整 ProgramId/SourceRevision/ProjectionRevision/Target expectation 计算、Program decode、producer topology projection 或 `IsStale`。轻量发布 Header检查 MUST不加载 Program，也不得遍历 authoring dependency graph。

默认产物状态 MUST为 `Missing`、`Invalid` 或 `Unchecked`。`Unchecked` MUST明确表示产物已发布但当前 authoring source 尚未在本次 Inspector 会话中比较；Inspector MUST不把 `Unchecked` 显示为 `Ready`。只有作者显式执行 `Refresh Status` 后，Inspector MAY调用唯一正式 stale 检查并显示 `Ready` 或 `Stale`。Definition字段修改后 MUST显示 `Needs Compile`；Compile成功后 MAY直接显示 `Ready`。检查结果 MUST只属于Inspector会话，不得写入Definition、Profile、Program或Projection资产。

#### Scenario: 选择 Definition

- **WHEN** 作者选择或重新选择 CharacterPipelineDefinition
- **THEN** Inspector MUST只根据 serialized reference 与轻量发布 Header 显示 `Missing`、`Invalid` 或 `Unchecked`
- **AND** MUST不计算当前 SourceRevision、解码 Program 或重算 ProjectionRevision

#### Scenario: 重绘 Inspector

- **WHEN** Unity 对已打开的 Definition Inspector 执行 Layout、Repaint 或 foldout 切换
- **THEN** Inspector MUST只绘制当前会话状态
- **AND** MUST不调用 `IsStale`、Compiler、Program decode 或任何完整 dependency hash 入口

#### Scenario: 显式刷新产物状态

- **WHEN** 作者点击 `Refresh Status`
- **THEN** Inspector MUST执行一次正式完整 stale 检查
- **AND** MUST将结果缓存为 `Ready` 或 `Stale`，后续 Repaint MUST不重复该检查

#### Scenario: 修改 Definition

- **WHEN** 作者通过当前 Inspector 修改任一 Definition authoring 字段
- **THEN** Inspector MUST立即显示 `Needs Compile`
- **AND** MUST不为更新状态自动运行 Compiler 或 stale 检查

#### Scenario: 编译产物

- **WHEN** 作者点击 Compile 且正式 Build 成功
- **THEN** Inspector MUST显示 `Ready`
- **AND** Build失败时 MUST不显示虚假的 `Ready`

#### Scenario: 查看生成产物详情

- **WHEN** 作者显式展开 Generated Artifacts 或运行 Compiler Diagnostics
- **THEN** Inspector MAY显示 Program/Projection identity、Hash、capability 与 report
- **AND** foldout绘制本身 MUST不触发完整 stale 检查
- **AND** Compiler Diagnostics MAY按显式命令运行完整 dry-run

### Requirement: Animation Presentation Profile 必须是唯一表现配置资产

`CharacterAnimationPresentationProfile` MUST作为ScriptableObject唯一引用Pose Graph、PoseStateMachine topology、node-local Blend/Inertialization Policy与角色Rig Definition，保存Profile-owned typed Source Binding子资产、有限Action producer引用、显式Foot Placement Analysis Mode、Analysis Source对象引用与Locomotion Sync Group。Pose Graph MUST唯一拥有typed Source Slot子资产，并保存Presentation Fact Input、PoseStateMachine、ClipPlayer、BlendSpacePlayer、SelectedPosePlayer、ActionPlaybackInput、AnimationSlot、Player、Mask、Additive、Pose Parameter、TwoBoneIK、LocalToComponentPose、FootPlacement、typed双腿targets、LegIK、ComponentToLocalPose与Output topology。Clip Binding MUST直接引用AnimationClip；Blend Space和Timeline MAY只通过各自正式owner直接引用AnimationClip。Clip Binding、Action producer binding与Timeline MUST不复制素材Curve、Marker、角色Rig或Analysis identity；Action producer binding MUST只保存producer到Timeline/Track的正式引用。Blend Space与Motion Matching资源内部Artifact compatibility identity只用于校验与Profile角色配置一致，不得成为第二角色配置owner。Definition、Gameplay Graph、BTSMTL StateMachine、Timeline、Presenter、Program、Runtime Prefab或独立EditorWindow MUST不保存这些角色级装配配置的可写副本。

#### Scenario: 一个Profile被一个Definition引用

- **WHEN** 作者选择CharacterAnimationPresentationProfile
- **THEN** Profile Inspector MUST提供Pose Graph、Clip source、Action producer binding、Locomotion Sync Group、Policy、Rig和Foot Analysis唯一入口
- **AND** Definition Inspector MUST不内联这些字段

#### Scenario: Action producer解析Foot Analysis

- **WHEN** Definition Build编译一个直接AnimationClip的有限Action producer
- **THEN** Compiler MUST从Profile Analysis Source、角色Rig与Clip Analysis Input Hash解析Artifact
- **AND** Action producer binding MUST不保存Foot Analysis identity副本

#### Scenario: Definition Inspector显示Projection状态

- **WHEN** 作者只选择CharacterPipelineDefinition
- **THEN** Inspector MUST只显示Animation Presentation Profile引用与Projection Ready/Stale/Missing摘要
- **AND** MUST不运行Pose Graph Compiler或内联显示node、Clip、Group或mask参数

### Requirement: Body Motion Profile 必须是唯一垂直动力作者配置

`CharacterPipelineDefinition` MUST显式引用一个`CharacterBodyMotionProfile`，Profile MUST唯一保存有限负数`GravityAcceleration`与有限正数`MaximumFallSpeed`作者配置。Definition Inspector MUST只在作者配置区显示Profile引用与配置错误，MUST不内联或复制Profile字段。Compiler MUST把Profile identity、content revision和参数作为正式source revision与Program descriptor输入；Runtime Host、Scene、Network Model、WorldSolver与Blackboard MUST不保存第二份重力配置或缺失默认。

#### Scenario: Definition缺少Body Motion Profile

- **WHEN** 作者尝试编译或运行缺少Profile的CharacterPipelineDefinition
- **THEN** 配置校验与Compiler MUST明确失败
- **AND** Runtime MUST不创建默认Profile或按Solver补值

### Requirement: Character Definition 必须通过两个配置引用安装Equipment能力

`CharacterPipelineDefinition` MUST只保存可选的`CharacterEquipmentProfile`、`CharacterEquipmentPresentationProfile`引用与Equipment capability声明，不得内嵌Slot、Route、Equipment、Feature、Loadout、visual binding或generated catalog。前者唯一拥有Gameplay装备配置，后者唯一拥有Unity visual binding。Inspector MUST把二者作为纯配置引用显示；生成的Program、Projection与catalog详情 MUST进入只读诊断，不得在Definition主Inspector展开为可编辑副本。

#### Scenario: 为Corin安装Equipment Profile

- **WHEN** 作者在Corin Definition启用Equipment capability
- **THEN** Inspector MUST要求精确选择一个Gameplay Equipment Profile和一个Equipment Presentation Profile
- **AND** Slot/item/Feature与visual binding MUST分别在对应正式Inspector中完成

#### Scenario: Definition展开generated装备表

- **WHEN** 作者选中CharacterPipelineDefinition
- **THEN** Inspector MUST不序列化或绘制第二份generated Equipment catalog
- **AND** 编译状态 MAY以只读摘要显示

### Requirement: Character authoring discovery必须支持显式composition roots

Compiler discovery MUST从Definition的RootTree和Equipment Profile声明的全部Feature Persistent/Route graph建立一个canonical composition root集合，并递归解析各自正式Graph/Timeline引用。每个root MUST携带owner、role、Feature/Route identity和稳定source path。Compiler MUST不通过目录扫描、AssetDatabase全局查找、命名约定或运行时Loadout只发现部分Feature。

#### Scenario: 发现未装备Gun Feature

- **WHEN** Gun Equipment已在Corin Equipment Profile允许catalog中但不是initial Loadout
- **THEN** Compiler MUST仍发现并静态链接Gun Feature roots
- **AND** Session运行中切换到Gun MUST不需要重新发现Graph

#### Scenario: Feature graph owner无法解析

- **WHEN** inline graph缺失serialized owner或owner identity不一致
- **THEN** discovery MUST失败并定位Feature/Route
- **AND** MUST不把它当作RootTree子图猜测owner

### Requirement: Core与Feature ActionProfile必须合并为唯一catalog

Definition直接拥有的core ActionProfile与Equipment Feature导出的ActionProfile MUST按稳定ActionId合并、排序并校验为唯一Character Action catalog。Feature ownership MAY作为source metadata进入Program和diagnostics，但 MUST不成为第二个Action registry或运行时membership表。

#### Scenario: Core Dodge与Sawblade Attack编译

- **WHEN** Corin Definition拥有Core Dodge且Sawblade Feature导出Attack
- **THEN** Program MUST生成一个包含二者的Action catalog
- **AND** Action runtime MUST通过同一ActionId lookup执行准入

#### Scenario: 两个Feature重复ActionId

- **WHEN** Sawblade与Gun导出相同ActionId但并非同一共享ActionProfile identity
- **THEN** Compiler MUST拒绝重复定义
- **AND** MUST不按active Feature覆盖catalog条目

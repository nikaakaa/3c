# character-pipeline-definition-authoring Specification

## Purpose

定义 CharacterPipelineDefinition 作为角色 authoring 配置装配根的纯引用边界和紧凑 Inspector。Definition 只装配正式领域引用，不拥有整角色编译产物、Program/Projection 或运行状态。
## Requirements
### Requirement: CharacterPipelineDefinition 必须是配置装配根

`CharacterPipelineDefinition` MUST 只保存 SimulationTickRate、ControlModuleId、AbilityGrants、InputProfile、GameplayEffectProfile、BodyMotionProfile、GameplayAbilityAdmissionProfile、GameplayBehaviorProfile 与 CharacterAnimationPresentationProfile 等正式领域引用。每个 Ability MUST 由 `AbilityGrant` 精确引用 `GameplayAbilityDefinition` 及其私有 `AbilityGraph`；Definition MUST 不保存 Character RootTree、整角色 Program、整包 Projection、运行状态或编译产物。Definition 也 MUST 不内联保存技能规则、FSM、Animation Channel、PoseStateMachine、AnimationSlot、Pose Graph、Policy、Rig、producer binding、Graph、Timeline、runtime lifecycle 或 compiler report 数据。

`GameplayAbilityDefinition` MUST是可独立打开的技能作者入口，拥有稳定Ability identity、准入规则、目标要求、效果引用、结束规则、后续Ability关系及唯一私有AbilityGraph。`AbilityGrant`只保存角色或装备授予引用、输入消费和目标绑定，不复制Ability规则或执行图。

#### Scenario: 打开独立Gameplay Ability

- **WHEN** 作者选择一个GameplayAbilityDefinition
- **THEN** Inspector MUST能直接编辑该Ability的规则、资源、结束规则、后续Ability和私有AbilityGraph入口
- **AND** MUST不要求作者再到CharacterPipelineDefinition拼装SkillDefinition、SkillGraph或空ActionContext

#### Scenario: 打开角色Definition

- **WHEN** 作者选择Corin CharacterPipelineDefinition
- **THEN** Inspector MUST优先显示角色引用的正式Config
- **AND** MUST不平铺PoseState、AnimationSlot、Pose节点、transition matrix、producer binding或Program Hash

#### Scenario: 缺失动画表现Profile

- **WHEN** Definition没有CharacterAnimationPresentationProfile引用
- **THEN** configuration validation与Compiler MUST报告明确错误
- **AND** 系统 MUST不创建内联Profile、默认Pose Graph或从Blend Library猜测配置

### Requirement: Definition Inspector 必须分离作者配置与生成产物

Definition Inspector MUST 以紧凑 Config References 作为默认作者界面。各领域 binding、identity、revision、capability 与准备报告 MUST 属于 Generated Artifacts/Diagnostics 区域。Inspector selection、`OnEnable`、Layout、Repaint 和 foldout 切换 MUST 只读取 serialized reference、轻量发布 Header 或当前 Inspector 会话缓存，MUST 不运行 Graph Compiler、Timeline Prepare、Pose Runtime、完整 dependency hash 或运行时对象创建。

默认领域状态 MUST 为 `Missing`、`Invalid` 或 `Unchecked`。`Unchecked` MUST 明确表示当前引用尚未在本次 Inspector 会话中完成正式 revision 对账；Inspector MUST 不把 `Unchecked` 显示为 `Ready`。只有作者显式执行 `Refresh Status` 后，Inspector MAY 调用对应领域的轻量 stale 检查并显示 `Ready` 或 `Stale`。Definition 字段修改后 MUST 显示 `Needs Prepare` 或对应领域的待处理状态。检查结果 MUST 只属于 Inspector 会话，不得写入 Definition、Profile 或 Runtime 状态。

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

`CharacterAnimationPresentationProfile` MUST 作为 ScriptableObject 唯一引用 Pose Graph、PoseStateMachine topology、Animation Layer/Control Rig 接口、node-local Blend/Inertialization Policy、角色 Rig Definition 与 FullBodyIK Profile，保存有限 Action producer 引用、显式 Foot Placement Analysis Mode、Analysis Source 对象引用与 Locomotion Sync Group。Pose Graph MUST 唯一拥有 Presentation Fact Input、PoseStateMachine、直接资源 Player、AnimationSlot、Mask、Additive、Pose Parameter、LocalToComponentPose、Component Pose controls、FootPlacement、PoseBoneIKGoals、Control Rig 调用、FullBodyIK、ComponentToLocalPose 与 Output topology；Action Playback Input、Pose Parameter Resolve 与 Goal Assembler 由正式表现宿主协调，不属于整角色编译产物。Player 与 Timeline MUST 直接引用原生 AnimationClip；Action producer binding 与 Timeline MUST 不复制素材注册 Curve、角色 Rig 或 Analysis identity。Blend Space 与 Motion Matching 资源内部 Artifact compatibility identity 只用于校验与 Profile 角色配置一致，不得成为第二角色配置 owner。Definition、Gameplay Graph、BTSMTL StateMachine、Timeline、Presenter 或独立 EditorWindow MUST 不保存这些角色级装配配置的可写副本。

#### Scenario: 一个Profile被一个Definition引用

- **WHEN** 作者选择CharacterAnimationPresentationProfile
- **THEN** Profile Inspector MUST提供Pose Graph、Clip source、Action producer binding、Locomotion Sync Group、Policy、Rig、FullBodyIK和Foot Analysis唯一入口
- **AND** Definition Inspector MUST不内联这些字段

#### Scenario: Action producer解析Foot Analysis

- **WHEN** 正式领域准备一个直接 AnimationClip 的有限 Action producer
- **THEN** 准备流程 MUST 从 Profile Analysis Source、角色 Rig 与 Clip Analysis Input Hash 解析 Artifact
- **AND** Action producer binding MUST不保存Foot Analysis identity副本

#### Scenario: Definition Inspector显示Projection状态

- **WHEN** 作者只选择CharacterPipelineDefinition
- **THEN** Inspector MUST 只显示 Animation Presentation Profile 引用与表现 binding 的 Ready/Stale/Missing 摘要
- **AND** MUST 不运行 Pose Graph Compiler 或内联显示 node、Clip、Group 或 mask 参数

### Requirement: Body Motion Profile 必须是唯一垂直动力作者配置

`CharacterPipelineDefinition` MUST 显式引用一个 `CharacterBodyMotionProfile`，Profile MUST 唯一保存有限负数 `GravityAcceleration` 与有限正数 `MaximumFallSpeed` 作者配置。Definition Inspector MUST 只在作者配置区显示 Profile 引用与配置错误，MUST 不内联或复制 Profile 字段。正式 Control/Motion 准备 MUST 使用 Profile identity、content revision 和参数；Runtime Host、Scene、Network Model、WorldSolver 与 Blackboard MUST 不保存第二份重力配置或缺失默认。

#### Scenario: Definition缺少Body Motion Profile

- **WHEN** 作者尝试准备或运行缺少 Profile 的 CharacterPipelineDefinition
- **THEN** 配置校验与正式领域准备 MUST 明确失败
- **AND** Runtime MUST不创建默认Profile或按Solver补值

### Requirement: Character Definition 必须通过两个配置引用安装Equipment能力

`CharacterPipelineDefinition` MUST 只保存可选的 `CharacterEquipmentProfile`、`CharacterEquipmentPresentationProfile` 引用与 Equipment capability 声明，不得内嵌 Slot、Route、Equipment、Feature、Loadout、visual binding 或运行时 catalog。前者唯一拥有 Gameplay 装备配置，后者唯一拥有 Unity visual binding。Inspector MUST 把二者作为纯配置引用显示；领域准备、catalog 和实际采用详情 MUST 进入只读诊断，不得在 Definition 主 Inspector 展开为可编辑副本。

#### Scenario: 为Corin安装Equipment Profile

- **WHEN** 作者在Corin Definition启用Equipment capability
- **THEN** Inspector MUST要求精确选择一个Gameplay Equipment Profile和一个Equipment Presentation Profile
- **AND** Slot/item/Feature与visual binding MUST分别在对应正式Inspector中完成

#### Scenario: Definition展开generated装备表

- **WHEN** 作者选中CharacterPipelineDefinition
- **THEN** Inspector MUST不序列化或绘制第二份generated Equipment catalog
- **AND** 编译状态 MAY以只读摘要显示

### Requirement: Character authoring discovery必须支持显式composition roots

领域 discovery MUST 从 Definition.AbilityGrants、Ability 的私有 AbilityGraph 和 Equipment Profile 声明的 Feature Persistent/Route graph 建立明确的引用闭包。每个 root MUST 携带 owner、role、Ability/Feature/Route identity 和稳定 source path；Graph 编译只处理 Graph owner 的闭包，Timeline、Pose、Control 和其它领域只解析自己的正式引用。任何 discovery MUST 不通过目录扫描、AssetDatabase 全局查找、命名约定或运行时 Loadout 猜测 owner，也 MUST 不回退到 Character RootTree。

#### Scenario: 发现未装备Gun Feature

- **WHEN** Gun Equipment已在Corin Equipment Profile允许catalog中但不是initial Loadout
- **THEN** 正式领域准备 MUST 仍发现并校验 Gun Feature roots
- **AND** Session运行中切换到Gun MUST不需要重新发现Graph

#### Scenario: Feature graph owner无法解析

- **WHEN** inline graph缺失serialized owner或owner identity不一致
- **THEN** discovery MUST失败并定位Feature/Route
- **AND** MUST不把它当作Skill Graph或其它composition root的子图猜测owner

### Requirement: Core与Feature GameplayAbilityAdmissionProfile必须合并为唯一Action catalog

Definition 直接拥有的 core GameplayAbilityAdmissionProfile 与 Equipment Feature 导出的 GameplayAbilityAdmissionProfile MUST 按稳定 ActionId 合并、排序并校验为唯一 Character Action catalog。Feature ownership MAY 作为 source metadata 进入诊断，但 MUST 不成为第二个 Action registry 或运行时 membership 表；该 catalog 不得被包装成整角色 Program。

#### Scenario: Core Dodge与Sawblade Attack准备

- **WHEN** Corin Definition拥有Core Dodge且Sawblade Feature导出Attack
- **THEN** Action owner MUST 生成一个包含二者的正式 Action catalog
- **AND** Action runtime MUST通过同一ActionId lookup执行准入

#### Scenario: 两个Feature重复ActionId

- **WHEN** Sawblade与Gun导出相同ActionId但并非同一共享GameplayAbilityAdmissionProfile identity
- **THEN** Compiler MUST拒绝重复定义
- **AND** MUST不按active Feature覆盖catalog条目

# character-pipeline-definition-authoring Specification

## Purpose

定义 CharacterPipelineDefinition 作为角色 authoring 配置装配根的纯引用边界、紧凑 Inspector，以及 Animation Presentation Profile 与 generated Program/Projection 的所有权和状态入口。

## Requirements

### Requirement: CharacterPipelineDefinition 必须是配置装配根

`CharacterPipelineDefinition` MUST只保存 RootTree、SimulationTickRate、InputProfile、GameplayEffectProfile、ActionProfile、GameplayBehaviorProfile、CharacterAnimationPresentationProfile 与 generated Program/Projection 的正式引用。Definition MUST不内联保存 Animation Layer、TransitionLibrary、producer binding、Graph、Timeline、runtime lifecycle 或 compiler report 数据。

#### Scenario: 打开角色 Definition

- **WHEN** 作者选择 Corin CharacterPipelineDefinition
- **THEN** Inspector MUST优先显示角色引用的正式 Config
- **AND** MUST不平铺 Animation Layer、producer binding、Program Hash 或 capability 明细

#### Scenario: 缺失动画表现 Profile

- **WHEN** Definition 没有 CharacterAnimationPresentationProfile 引用
- **THEN** configuration validation 与 Compiler MUST报告明确错误
- **AND** 系统 MUST不创建内联默认 Profile 或从 TransitionLibrary 猜测配置

### Requirement: Definition Inspector 必须分离作者配置与生成产物

Definition Inspector MUST以紧凑 Config References 作为默认作者界面。Program/Projection 引用、identity、Hash、capability 与 compiler report MUST属于 Generated Artifacts/Diagnostics 区域；默认状态只显示 `Missing`、`Invalid` 或 `Ready` 与显式 Compile 命令。Inspector selection、Repaint 和 foldout 切换 MUST不运行 Compiler、完整 source revision、Program decode 或 producer topology projection。

#### Scenario: 只检查配置引用

- **WHEN** 作者选择 Definition 但没有修改任何 authoring 字段
- **THEN** Inspector MUST只读取轻量 serialized reference 与 artifact metadata
- **AND** Unity Editor MUST不因该选择触发 Program build

#### Scenario: 查看生成产物详情

- **WHEN** 作者显式展开 Generated Artifacts 或运行 Compiler Diagnostics
- **THEN** Inspector MAY显示 Program/Projection identity、Hash、capability 与 report
- **AND** 重型操作 MUST由该显式命令触发，不得成为默认 Repaint 路径

### Requirement: Animation Presentation Profile 必须是唯一表现配置资产

`CharacterAnimationPresentationProfile` MUST作为 ScriptableObject 唯一保存有序 Layer catalog、Animancer TransitionLibraryAsset 引用和稳定 producer presentation bindings。Definition、Graph、Timeline、Presenter、Program 或独立 EditorWindow MUST不保存同一配置的可写副本。

#### Scenario: 一个 Profile 被一个 Definition 引用

- **WHEN** 作者选择 CharacterAnimationPresentationProfile
- **THEN** Profile Inspector MUST提供 Layer、TransitionLibrary 与 producer binding 唯一写入口
- **AND** Undo 与 dirty owner MUST是 Profile asset

#### Scenario: 一个 Profile 被多个 Definition 引用

- **WHEN** 多个 Definition 引用同一 Profile
- **THEN** 每个 Definition/Profile/Projection 组合 MUST独立通过 producer identity 校验
- **AND** Profile Inspector MUST要求显式 Definition context 才能显示来源投影
- **AND** Profile MUST不保存反向 Definition owner 引用

### Requirement: Body Motion Profile 必须是唯一垂直动力作者配置

`CharacterPipelineDefinition` MUST显式引用一个`CharacterBodyMotionProfile`，Profile MUST唯一保存有限负数`GravityAcceleration`与有限正数`MaximumFallSpeed`作者配置。Definition Inspector MUST只在作者配置区显示Profile引用与配置错误，MUST不内联或复制Profile字段。Compiler MUST把Profile identity、content revision和参数作为正式source revision与Program descriptor输入；Runtime Host、Scene、Network Model、WorldSolver与Blackboard MUST不保存第二份重力配置或缺失默认。

#### Scenario: Definition缺少Body Motion Profile

- **WHEN** 作者尝试编译或运行缺少Profile的CharacterPipelineDefinition
- **THEN** 配置校验与Compiler MUST明确失败
- **AND** Runtime MUST不创建默认Profile或按Solver补值

# project-structure Specification

## Purpose
定义项目目录、配置资产和动画、状态、场景资源的结构边界，避免跨领域资源混放并保持后续模块可发现、可维护。
## Requirements
### Requirement: 角色动画与移动代码目录分层
系统 MUST 将基础移动相关的动画语义、动画播放外观、移动状态机、移动主链和编辑器工具放在明确目录中，避免因为后续扩展打断规则、多层动画、IK 或编辑器而产生并行路径。

#### Scenario: 动画代码分层
- **WHEN** 新增或调整基础移动动画相关运行时代码
- **THEN** 纯数据模型 MUST 放在 `Assets/Scripts/Character/Animation/Model/`
- **AND** ScriptableObject 配置类型 MUST 放在 `Assets/Scripts/Character/Animation/Config/`
- **AND** Animancer 播放外观 MUST 放在 `Assets/Scripts/Character/Animation/Runtime/`
- **AND** Unity Editor 工具 MUST 放在 `Assets/Scripts/Character/Animation/Editor/`

#### Scenario: 移动代码分层
- **WHEN** 新增或调整基础移动状态机相关代码
- **THEN** 移动纯数据模型 MUST 放在 `Assets/Scripts/Character/Movement/Model/`
- **AND** 移动配置类型 MUST 放在 `Assets/Scripts/Character/Movement/Config/`
- **AND** 状态机 builder、condition evaluator 和 pipeline 求解 MUST 放在 `Assets/Scripts/Character/Movement/Solver/`
- **AND** MonoBehaviour 主链和 Unity 适配器 MUST 放在 `Assets/Scripts/Character/Movement/Runtime/`

#### Scenario: 不新增隐式加载路径
- **WHEN** 基础移动动画配置需要被角色 prefab 或场景引用
- **THEN** 实现 MUST 通过显式 ScriptableObject 引用连接
- **AND** MUST NOT 通过 `Resources.Load`、全局单例或运行时硬编码路径加载默认动画配置

### Requirement: 项目侧配置资产目录分层
系统 MUST 将项目侧状态机配置、动作逻辑配置、基础移动配置资产和 Animancer 播放资产分开放置，使设计者能区分逻辑状态、动作参数、动画语义和具体播放资产。

#### Scenario: Locomotion 状态图配置资产归属
- **WHEN** 创建或迁移角色 Locomotion 状态图配置资产
- **THEN** 资产 MUST 放在 `Assets/Configs/3C/StateMachine/`
- **AND** Corin 的正式 Locomotion 状态图 MUST 位于 `Assets/Configs/3C/StateMachine/Locomotion/Corin/`
- **AND** 这些资产 MUST NOT 保存 AnimationClip、Animancer TransitionAsset、动作动画 Profile、Dodge motion config 或 Action interrupt policy set
- **AND** 旧 `Assets/Configs/3C/Statemachine/` MUST NOT 作为并行状态机配置目录保留
- **AND** 旧 `Assets/Configs/3C/StateMachine/FullBody/` MUST NOT 作为正式状态机配置目录保留

#### Scenario: 角色 Action 逻辑配置资产归属
- **WHEN** 创建或迁移角色 ActionSet、Dodge motion config、Dodge interrupt policy set 或同类动作逻辑配置资产
- **THEN** 资产 MUST 放在 `Assets/Configs/3C/Action/<角色>/`
- **AND** 这些资产 MUST 能从动作逻辑入口解析完整 motion 和 interrupt 配置
- **AND** 这些资产 MUST NOT 保存角色具体 AnimationClip 或 Animancer 播放参数
- **AND** 旧 `Assets/Configs/3C/Action/FullBody/` MUST NOT 作为正式动作配置目录保留

#### Scenario: 动作动画配置资产归属
- **WHEN** 创建或迁移动作动画绑定集、动作动画 Profile 或角色动作动画 override
- **THEN** 资产 MUST 放在 `Assets/Configs/3C/Animation/<角色>/Animancer/` 或等价角色动画目录
- **AND** 动作动画绑定 MUST 通过稳定 action id 连接动作逻辑和动画 Profile
- **AND** 动作动画 Profile MUST NOT 定义 FullBody 状态树拓扑、动作进入条件或动作位移权威
- **AND** 旧 `Assets/Configs/3C/Animation/FullBody/` MUST NOT 作为正式动作动画目录保留

#### Scenario: Locomotion 动画配置资产归属
- **WHEN** 创建或迁移基础移动 phase alias 配置、退出策略配置、motion profile 或同类 Locomotion 动画语义配置资产
- **THEN** 资产 MUST 放在 `Assets/Configs/3C/Animation/<角色>/Locomotion/`
- **AND** 这些资产 MUST NOT 定义 FullBody 主状态树拓扑
- **AND** 这些资产 MUST NOT 替代 Locomotion 状态图配置

#### Scenario: Movement 数值配置资产归属
- **WHEN** 创建或迁移基础移动速度、加速度、起步时长或停止阶段退出时长配置
- **THEN** 资产 MUST 放在 `Assets/Configs/3C/Movement/`

#### Scenario: Animancer 播放资产归属
- **WHEN** 创建或迁移 Animancer TransitionLibrary、alias parameter asset 或 TransitionAsset
- **THEN** 资产 MUST 放在 `Assets/Configs/3C/Animation/<角色>/Animancer/`
- **AND** clip、fade、speed、normalized start time 和 Animancer event MUST 在该 Animancer 播放资产层维护

#### Scenario: 配置目录不混放
- **WHEN** 设计者查找 Locomotion 状态图
- **THEN** 设计者 MUST 能在 `Assets/Configs/3C/StateMachine/Locomotion/<角色>/` 找到状态图配置
- **WHEN** 设计者查找 `Action.Dodge` 的运动和打断逻辑配置
- **THEN** 设计者 MUST 能在 `Assets/Configs/3C/Action/<角色>/` 找到动作逻辑配置
- **WHEN** 设计者查找 `Action.Dodge.Directional` 的具体角色动画绑定
- **THEN** 设计者 MUST 能在 `Assets/Configs/3C/Animation/<角色>/Animancer/` 找到动作动画配置
- **WHEN** 设计者查找 `RunEnd` 的具体 clip 或 fade
- **THEN** 设计者 MUST 能在 `Assets/Configs/3C/Animation/<角色>/Animancer/` 找到 Animancer 播放资产

### Requirement: 自研分层状态机代码与文档归属
系统 MUST 将自研统一分层状态机的模型、配置、运行时解释、timeline facts、输出解析和诊断文档放在可发现的 Character 状态机目录和 agent 文档中。目录和文档命名 MUST 体现它是当前角色主线的状态图运行时，而不是 UnityHFSM adapter、BBB 旧状态机或 Locomotion 局部状态机。

#### Scenario: 状态机代码目录清晰
- **WHEN** 新增或调整角色状态机运行时代码
- **THEN** 纯数据模型 MUST 放在 `Assets/Scripts/Character/StateMachine/Model/`
- **AND** ScriptableObject 配置 MUST 放在 `Assets/Scripts/Character/StateMachine/Config/`
- **AND** runner 和状态生命周期 MUST 放在 `Assets/Scripts/Character/StateMachine/Solver/Runtime/`
- **AND** timeline sampler MUST 放在 `Assets/Scripts/Character/StateMachine/Solver/Timeline/`
- **AND** transition evaluator MUST 放在 `Assets/Scripts/Character/StateMachine/Solver/Transition/`
- **AND** output resolver MUST 放在 `Assets/Scripts/Character/StateMachine/Solver/Output/`
- **AND** validator MUST 放在 `Assets/Scripts/Character/StateMachine/Solver/Validation/`
- **AND** Runtime Adapter MUST NOT 混入状态机 solver 目录

#### Scenario: 状态机配置资产目录清晰
- **WHEN** 新增或迁移角色状态机配置资产
- **THEN** 角色状态图配置 MUST 放在 `Assets/Configs/3C/StateMachine/<模块>/<角色>/`
- **AND** Corin 的正式移动状态图 MUST 位于 `Assets/Configs/3C/StateMachine/Locomotion/Corin/CorinLocomotionStateGraph.asset`
- **AND** 旧 `Assets/Configs/3C/Statemachine/` MUST NOT 作为并行状态机配置目录保留
- **AND** 状态机配置资产 MUST NOT 保存 AnimationClip、Animancer TransitionAsset、fade、speed 或 normalized start time

#### Scenario: 文档指向当前主线
- **WHEN** 新增或更新 agent 状态机指南
- **THEN** 指南 MUST 以项目自研统一分层状态机为当前主线
- **AND** UnityHFSM 指南 MUST 标记为历史参考、第三方库参考或未来另行审批方向
- **AND** 文档 MUST 说明状态机运行时不得直接执行运动、播放动画或读取输入系统对象

#### Scenario: 没有隐式 fallback 配置
- **WHEN** 状态机配置缺失或非法
- **THEN** 文档和实现 MUST 要求明确诊断错误
- **AND** MUST NOT 通过 Resources、全局单例、旧字段或代码默认状态图隐式恢复运行

#### Scenario: 测试和验证可发现
- **WHEN** 实施状态机 runtime 重构
- **THEN** tasks MUST 包含自动测试、静态边界验证和手动 Play Mode 验证说明
- **AND** 手动验证 MUST 明确如何观察 active path、pending transition、timeline facts 和 rollback 结果

### Requirement: 正式 Animancer 配置目录命名
系统 MUST 将正式角色 Animancer 播放配置放在 `Assets/Configs/3C/Animation/<角色>/Animancer/...` 或批准的等价 `Animation` 目录下。旧拼写 `Assets/Configs/3C/Animacer/...` MUST NOT 作为正式运行时配置入口。

#### Scenario: Corin Generic rig variant 位于正式目录
- **WHEN** 检查 Corin 默认 Generic Animancer transition library
- **THEN** 正式资产 MUST 位于 `Assets/Configs/3C/Animation/Corin/Animancer/RigVariants/Generic/`
- **AND** `Assets/Configs/3C/Animacer/` MUST NOT 被视为正式入口
- **AND** `Pramater` 拼写目录 MUST NOT 被视为正式参数目录

#### Scenario: 旧目录只能作为迁移残留
- **WHEN** 项目中仍存在旧 `Animacer`、`Statemachine` 或 `Pramater` 目录
- **THEN** 静态校验 MUST 报告它们不是正式入口
- **AND** 正式角色配置根 MUST NOT 引用这些旧目录中的资产

#### Scenario: 规格文字不再批准旧目录为正式入口
- **WHEN** 检查 OpenSpec 当前规格和 active changes
- **THEN** 若 `Animacer`、`Statemachine` 或 `Pramater` 出现，文本 MUST 明确标记为 legacy、迁移残留或反例
- **AND** MUST NOT 同时把旧目录和新目录描述为两个正式入口

### Requirement: Corin 配置资产迁移边界
系统 MUST 将 Corin 正式配置资产保持在 `Assets/Configs/3C` 的职责目录蓝图内。迁移资产时 MUST 保留 `.meta` GUID 或同步更新所有正式引用，并通过自动测试发现 dangling GUID。

#### Scenario: 资产位于正式目录
- **WHEN** 检查 Corin 默认配置资产
- **THEN** 角色根 MUST 位于 `Assets/Configs/3C/Character/Corin/`
- **AND** 状态机 MUST 位于 `Assets/Configs/3C/StateMachine/Locomotion/Corin/`
- **AND** 动作逻辑 MUST 位于 `Assets/Configs/3C/Action/Corin/`
- **AND** 动画表现 MUST 位于 `Assets/Configs/3C/Animation/Corin/`
- **AND** 基础移动 MUST 位于 `Assets/Configs/3C/Movement/`
- **AND** 输入和相机 MUST 位于各自正式目录

#### Scenario: GUID 迁移可追踪
- **WHEN** 正式资产需要移动或重命名
- **THEN** 实施 MUST 优先保留 `.meta` GUID
- **AND** 若 GUID 无法保留，必须更新所有正式 `.asset` 引用
- **AND** 自动测试 MUST 报告 dangling GUID 或空引用

#### Scenario: Prefab 和 Scene 不在本变更中迁移
- **WHEN** 实施 Corin 配置资产迁移
- **THEN** diff MUST NOT 修改 `Assets/Prefabs/Character/可琳.prefab`
- **AND** MUST NOT 修改 `Assets/Prefabs/Character/可琳_Humanoid.prefab`
- **AND** MUST NOT 修改正式场景 `.unity` 文件

### Requirement: 废弃目录不得作为正式配置入口

项目正式配置目录 MUST 反映当前角色主线。旧 FullBody 目录、旧 FullBody 状态机目录和旧 FullBody 动画目录不得作为正式配置、测试样例或未来动作模板继续存在。

#### Scenario: 正式配置只使用角色专属 Action 目录

- **GIVEN** 配置资产目录被扫描
- **WHEN** 测试检查 Action 配置布局
- **THEN** Corin 的正式动作配置位于 `Assets/Configs/3C/Action/Corin`
- **AND** `Assets/Configs/3C/Action/FullBody` 不作为正式配置目录存在

#### Scenario: 正式状态图只使用 Locomotion 目录

- **GIVEN** 配置资产目录被扫描
- **WHEN** 测试检查状态机配置布局
- **THEN** Corin 的移动状态图位于 `Assets/Configs/3C/StateMachine/Locomotion/Corin`
- **AND** `Assets/Configs/3C/StateMachine/FullBody` 不作为正式状态机目录存在

#### Scenario: 正式动画配置只使用角色或模块明确目录

- **GIVEN** 动画配置目录被扫描
- **WHEN** 测试检查 Animancer profile 和动作动画配置布局
- **THEN** 正式动画配置位于角色或模块明确的当前目录
- **AND** `Assets/Configs/3C/Animation/FullBody` 不作为正式动画配置目录存在

### Requirement: 主动规格不得继续引用旧主线作为实现目标

Active specs 和新提案 MUST 不把旧 FullBody 主树、旧 Host Adapter、旧 tick adapter 或旧 presenter 描述为正式实现目标。历史内容若必须提及旧名称，必须明确标记为废弃、迁移或兼容只读语境。

#### Scenario: 新规格不复用旧 FullBody 主线

- **GIVEN** active specs 和未归档变更被检查
- **WHEN** 文档扫描发现 `Action/FullBody`、`StateMachine/FullBody`、旧 FullBody action controller、旧 tick adapter 或旧 presenter
- **THEN** 每处引用必须处于历史、废弃、迁移或只读兼容语境
- **AND** 不得作为未来动作实现或正式运行时接入路径

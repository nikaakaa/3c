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

#### Scenario: FullBody 状态机配置资产归属
- **WHEN** 创建或迁移 FullBody HFSM 树资产、FullBody 状态拓扑资产或 Locomotion 状态图配置资产
- **THEN** 资产 MUST 放在 `Assets/Configs/3C/Statemachine/`
- **AND** FullBody 主树资产 MUST 集中表达 FullBody 主行为域拓扑
- **AND** 这些资产 MUST NOT 保存 AnimationClip、Animancer TransitionAsset、动作动画 Profile、Dodge motion config 或 Action interrupt policy set

#### Scenario: FullBody Action 逻辑配置资产归属
- **WHEN** 创建或迁移 FullBody ActionSet、Dodge motion config、Dodge interrupt policy set 或同类动作逻辑配置资产
- **THEN** 资产 MUST 放在 `Assets/Configs/3C/Action/FullBody/`
- **AND** 这些资产 MUST 能从动作逻辑入口解析完整 motion 和 interrupt 配置
- **AND** 这些资产 MUST NOT 保存角色具体 AnimationClip 或 Animancer 播放参数

#### Scenario: FullBody 动作动画配置资产归属
- **WHEN** 创建或迁移动作动画绑定集、动作动画 Profile 或角色动作动画 override
- **THEN** 资产 MUST 放在 `Assets/Configs/3C/Animation/FullBody/<角色>/`
- **AND** 动作动画绑定 MUST 通过稳定 action id 连接动作逻辑和动画 Profile
- **AND** 动作动画 Profile MUST NOT 定义 FullBody 状态树拓扑、动作进入条件或动作位移权威

#### Scenario: Locomotion 动画配置资产归属
- **WHEN** 创建或迁移基础移动 phase alias 配置、退出策略配置、motion profile 或同类 Locomotion 动画语义配置资产
- **THEN** 资产 MUST 放在 `Assets/Configs/3C/Animation/Locomotion/<角色>/`
- **AND** 这些资产 MUST NOT 定义 FullBody 主状态树拓扑
- **AND** 这些资产 MUST NOT 替代 Locomotion 状态图配置

#### Scenario: Movement 数值配置资产归属
- **WHEN** 创建或迁移基础移动速度、加速度、起步时长或停止 fallback 时长配置
- **THEN** 资产 MUST 放在 `Assets/Configs/3C/Movement/`

#### Scenario: Animancer 播放资产归属
- **WHEN** 创建或迁移 Animancer TransitionLibrary、alias parameter asset 或 TransitionAsset
- **THEN** 资产 MUST 放在 `Assets/Configs/3C/Animacer/<角色>/`
- **AND** clip、fade、speed、normalized start time 和 Animancer event MUST 在该 Animancer 播放资产层维护

#### Scenario: 配置目录不混放
- **WHEN** 设计者查找 FullBody 主状态树
- **THEN** 设计者 MUST 能在 `Assets/Configs/3C/Statemachine/` 找到中心状态机配置
- **WHEN** 设计者查找 `Action.Dodge` 的运动和打断逻辑配置
- **THEN** 设计者 MUST 能在 `Assets/Configs/3C/Action/FullBody/` 找到动作逻辑配置
- **WHEN** 设计者查找 `Action.Dodge.Directional` 的具体角色动画绑定
- **THEN** 设计者 MUST 能在 `Assets/Configs/3C/Animation/FullBody/<角色>/` 找到动作动画配置
- **WHEN** 设计者查找 `RunEnd` 的具体 clip 或 fade
- **THEN** 设计者 MUST 能在 `Assets/Configs/3C/Animacer/<角色>/` 找到 Animancer 播放资产

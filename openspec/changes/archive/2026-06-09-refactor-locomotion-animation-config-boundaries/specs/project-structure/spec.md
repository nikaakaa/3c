## ADDED Requirements

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
系统 MUST 将项目侧基础移动配置资产和 Animancer 播放资产分开放置，使设计者能区分逻辑配置和动画播放参数配置。

#### Scenario: Locomotion 配置资产归属
- **WHEN** 创建或迁移基础移动 phase alias 配置、状态图配置或同类逻辑配置资产
- **THEN** 资产 MUST 放在 `Assets/Configs/3C/Locomotion/`
- **AND** 这些资产 MUST NOT 保存 AnimationClip、Animancer TransitionAsset 或场景实例引用

#### Scenario: Movement 数值配置资产归属
- **WHEN** 创建或迁移基础移动速度、加速度、起步时长或停止 fallback 时长配置
- **THEN** 资产 MUST 放在 `Assets/Configs/3C/Movement/`

#### Scenario: Animancer 播放资产归属
- **WHEN** 创建或迁移 Animancer TransitionLibrary、alias parameter asset 或 TransitionAsset
- **THEN** 资产 MUST 放在 `Assets/Configs/3C/Animacer/<角色>/`
- **AND** clip、fade、speed、normalized start time 和 Animancer event MUST 在该 Animancer 播放资产层维护

#### Scenario: 配置目录不混放
- **WHEN** 设计者查找 `RunEnd` 的逻辑退出时间
- **THEN** 设计者 MUST 能在 `Assets/Configs/3C/Locomotion/` 找到项目侧 Run 配置
- **WHEN** 设计者查找 `RunEnd` 的具体 clip 或 fade
- **THEN** 设计者 MUST 能在 `Assets/Configs/3C/Animacer/<角色>/` 找到 Animancer 播放资产

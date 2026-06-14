## MODIFIED Requirements
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

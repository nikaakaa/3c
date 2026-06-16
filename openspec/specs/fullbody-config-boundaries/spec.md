# fullbody-config-boundaries Specification

## Purpose
定义 FullBody 逻辑状态配置、动作逻辑配置和动画表现配置的分层边界，确保状态树、动作参数和动画 Profile 各自拥有清晰权威。
## Requirements
### Requirement: FullBody 状态配置集中且只表达逻辑状态
系统 MUST 通过中心 FullBody 状态配置表达主行为域的状态树拓扑和节点绑定。该配置 MUST NOT 直接保存动作动画 Profile、Animancer 播放资产、Dodge 运动参数或动作打断策略。

#### Scenario: 状态树只持有拓扑和绑定
- **WHEN** 设计者检查 FullBody HFSM 树资产
- **THEN** 资产 MUST 能展示 `FullBody / Locomotion / Action` 层级
- **AND** Locomotion 节点 MUST 只绑定 `BasicMovementPhase`
- **AND** Action 节点 MUST 只绑定稳定 `ActionStateId`
- **AND** 资产 MUST NOT 引用 `ActionAnimationProfileSO`
- **AND** 资产 MUST NOT 引用 `DodgeActionConfigSO`
- **AND** 资产 MUST NOT 引用 `ActionInterruptPolicySetSO`

#### Scenario: Locomotion 状态图属于状态机配置
- **WHEN** 设计者检查基础移动 `Idle / MoveStart / MoveLoop / MoveStop` 的状态转移规则
- **THEN** 规则 MUST 归属 Locomotion 状态图配置或等价状态机配置
- **AND** 规则 MUST NOT 归属动作动画 Profile
- **AND** 规则 MUST NOT 归属 Animancer TransitionLibrary

### Requirement: FullBody 动作逻辑配置和动作动画配置分离
系统 MUST 将 FullBody 动作逻辑配置和动作动画表现配置拆成独立类型或等价边界。动作逻辑配置 MUST 能定位动作运动参数和打断策略；动作动画配置 MUST 能通过稳定 action id 定位动作动画 Profile。

#### Scenario: ActionSet 不持有动画 Profile
- **WHEN** 设计者或测试读取 `FullBodyActionSetSO`
- **THEN** 它 MUST 能解析 `Action.Dodge` 的 motion config
- **AND** MUST 能解析 `Action.Dodge` 的 interrupt policy set
- **AND** MUST NOT 直接持有 `ActionAnimationProfileSO`

#### Scenario: 动作动画绑定集解析 Profile
- **WHEN** FullBody 主调度入口准备播放 `Action.Dodge`
- **THEN** 系统 MUST 通过动作动画绑定集或等价动画配置入口解析 `Action.Dodge` 的 `ActionAnimationProfileSO`
- **AND** 该绑定集 MUST 校验缺失 Profile
- **AND** 该绑定集 MUST 校验 `Action.Dodge.Directional` 和 `Action.Dodge.Backstep` 两个必要 key

#### Scenario: FullBody 主入口负责装配而不是合并职责
- **WHEN** 当前角色 prefab 装配 FullBody 主调度入口
- **THEN** 主调度入口 MUST 显式引用状态树配置
- **AND** MUST 显式引用动作逻辑配置
- **AND** MUST 显式引用动作动画配置
- **AND** MUST NOT 隐式从 Resources、全局单例或硬编码路径加载动画 Profile

### Requirement: 配置资产目录表达职责归属
系统 MUST 将 FullBody 状态机配置、动作逻辑配置和动画表现配置放在可区分的目录中。目录结构 MUST 帮助设计者判断一个资产是否参与状态拓扑、动作逻辑或动画表现。

#### Scenario: 状态机目录不混入动画资产
- **WHEN** 检查 `Assets/Configs/3C/StateMachine/FullBody`
- **THEN** 该目录 MAY 包含 FullBody HFSM 树资产
- **AND** MAY 包含 Locomotion 状态图配置
- **AND** MUST NOT 包含 `ActionAnimationProfileSO` 资产
- **AND** MUST NOT 包含基础移动动画配置资产

#### Scenario: 动作目录承载动作逻辑配置
- **WHEN** 检查 `Assets/Configs/3C/Action/FullBody`
- **THEN** 该目录 MUST 能定位 FullBody ActionSet 或等价动作逻辑入口
- **AND** Dodge 子目录 MUST 能定位 Dodge motion config
- **AND** Dodge 子目录 MUST 能定位 Dodge interrupt policy set
- **AND** 该目录 MUST NOT 要求保存角色具体 AnimationClip

#### Scenario: 动画目录承载角色动画绑定
- **WHEN** 检查 `Assets/Configs/3C/Animation`
- **THEN** FullBody 动作动画绑定和动作动画 Profile MUST 归属该目录或其角色子目录
- **AND** Locomotion 动画 alias、exit policy 和 motion profile 配置 MUST 归属该目录或其角色子目录
- **AND** 这些动画配置 MUST NOT 定义 FullBody HFSM 树拓扑

### Requirement: 边界调整可测试且不引入分裂路径
系统 MUST 提供自动测试、静态检查和手动验证，证明配置边界调整后仍只有一个 FullBody 主状态权威、一个动作调度入口和一个运动执行出口。

#### Scenario: 自动测试覆盖配置闭环
- **WHEN** 运行 FullBody 配置边界 EditMode 测试
- **THEN** 测试 MUST 覆盖 ActionSet 逻辑配置解析
- **AND** MUST 覆盖动作动画绑定集 Profile 解析
- **AND** MUST 覆盖可琳 prefab 同时绑定状态树、动作逻辑集和动作动画绑定集

#### Scenario: 静态边界检查
- **WHEN** 检查 FullBody 状态树和 ActionSet 源码
- **THEN** 状态树源码 MUST 不引用 Animancer 或 `AnimationClip`
- **AND** ActionSet 源码 MUST 不引用 `ActionAnimationProfileSO`
- **AND** 动作动画绑定源码 MUST 不引用 Locomotion 状态图 builder

#### Scenario: 手动验证不回退
- **WHEN** 用户在 Play Mode 中执行 WASD、Directional Dodge、Backstep Dodge 和替换动作动画 clip 的验证
- **THEN** 基础移动状态路径 MUST 正常变化
- **AND** Dodge active 时 MUST 不叠加基础移动平面位移或 base layer 动画
- **AND** 替换动作动画 clip MUST 不要求修改动作逻辑代码

### Requirement: FullBody 正式配置目录蓝图
系统 MUST 为 `Assets/Configs/3C` 提供一套正式配置目录蓝图，使目录名直接表达资产参与的 Module 归属。正式目录 MUST 区分角色配置根、状态机拓扑、动作逻辑、动画表现、基础移动、输入和相机配置；旧拼写目录或测试资产目录 MUST NOT 作为正式运行时配置入口。

#### Scenario: 目录表达职责
- **WHEN** 检查默认 3C 配置目录
- **THEN** 默认角色配置根 MUST 位于 `Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset`
- **AND** 状态机资产 MUST 位于 `Assets/Configs/3C/StateMachine/FullBody/CorinFullBodyStateMachine.asset` 或批准的等价 Corin FullBody 状态机路径
- **AND** 动作逻辑资产 MUST 位于 `Assets/Configs/3C/Action/`
- **AND** 动画表现资产 MUST 位于 `Assets/Configs/3C/Animation/`
- **AND** 基础移动资产 MUST 位于 `Assets/Configs/3C/Movement/`
- **AND** 输入资产 MUST 位于 `Assets/Configs/3C/Input/` 或 `Assets/Configs/3C/InputReferences/`
- **AND** 相机资产 MUST 位于 `Assets/Configs/3C/Camera/`

#### Scenario: 旧目录不作为正式入口
- **WHEN** 检查默认 3C 配置目录
- **THEN** `Assets/Configs/3C/CharacterConfig.asset` MUST NOT 作为根目录第二正式入口
- **AND** Corin 正式资产名称 MUST NOT 使用 `Default` 前缀
- **AND** `Assets/Configs/3C/Statemachine/` MUST NOT 包含正式状态机资产
- **AND** `Assets/Configs/3C/Animacer/` MUST NOT 作为正式 Animancer 配置入口
- **AND** `Pramater` MUST NOT 作为正式参数目录名
- **AND** 若这些旧目录短期存在，里面的资产 MUST 只能作为迁移残留，并 MUST 被静态校验报告

#### Scenario: Rig variant 目录表达正式变体
- **WHEN** 检查 Corin Animancer 配置目录
- **THEN** Generic transition library MUST 位于 `Assets/Configs/3C/Animation/Corin/Animancer/RigVariants/Generic/CorinGenericAnimancerTransitionLibrary.asset` 或批准的等价 Generic rig variant 目录
- **AND** Generic MUST 被视为本次 Corin 的唯一正式 rig variant
- **AND** Humanoid transition library MUST NOT 被默认角色配置根要求为必需正式 rig variant
- **AND** 若 Humanoid 资产保留，MUST 位于参考、测试或未来迁移目录，或等待后续 OpenSpec 批准后再进入正式 rig variant 目录
- **AND** 正式 Generic rig variant MUST NOT 位于测试、参考或旧拼写目录

#### Scenario: 测试命名资产不被正式配置引用
- **WHEN** 校验默认角色配置闭环
- **THEN** 正式配置 MUST NOT 引用 `TestTurnback*`、`turnback*`、`testTurn` 或等价测试命名资产作为正式 motion profile、animation profile 或状态机绑定
- **AND** 实验资产若保留，MUST 位于明确的测试或参考目录，且不得被正式 `CharacterConfig` 解析

### Requirement: FullBody 配置唯一权威校验
系统 MUST 提供自动校验，证明默认 FullBody 配置闭环中每类可运行语义只有一个正式配置权威。校验 MUST 覆盖状态机节点能力、Dodge motion 参数、TurnBack motion source、动作请求策略、动画 key 和根配置引用。

#### Scenario: Dodge motion 参数只有一个来源
- **WHEN** 校验默认 `Action.Dodge` 配置
- **THEN** Directional duration/distance MUST 只来自正式 Action 逻辑配置或批准的等价动作配置源
- **AND** Backstep duration/distance MUST 只来自正式 Action 逻辑配置或批准的等价动作配置源
- **AND** 默认状态机资产 MUST NOT 并行保存能决定同一 motion 参数的旧 `output` 字段

#### Scenario: TurnBack motion source 只有一个正式引用
- **WHEN** 校验默认 `FullBody/Locomotion/TurnBack` 配置
- **THEN** 状态机资产 MUST 只输出 `Locomotion.Turn.Back` 或等价稳定 source id
- **AND** 正式 baked motion profile MUST 由 Locomotion animation/motion 配置解析
- **AND** 状态机资产 MUST NOT 要求设计者在多个字段重复填写同一个 TurnBack alias

#### Scenario: 根配置引用完整
- **WHEN** 校验默认角色配置根
- **THEN** StateMachine、Movement、Action 逻辑、Action 动画、Locomotion 动画、Animancer、Input 和 Camera 的必需正式引用 MUST 可解析
- **AND** dangling GUID、空必需引用或旧目录引用 MUST 被报告为配置错误
- **AND** 系统 MUST NOT 通过 fallback 配置隐藏这些错误


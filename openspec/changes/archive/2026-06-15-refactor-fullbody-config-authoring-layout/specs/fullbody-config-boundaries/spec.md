## ADDED Requirements
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

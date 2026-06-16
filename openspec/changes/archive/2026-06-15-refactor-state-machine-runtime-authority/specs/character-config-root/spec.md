## MODIFIED Requirements
### Requirement: 角色配置根 SO
系统 MUST 提供一个 `CharacterConfigSO` 作为角色配置的根入口。角色子系统配置 MUST 通过根 SO 的命名子模块引用访问；`PlayerLocomotionController` 上的旧平铺序列化字段 MAY 暂时保留为迁移遗留数据，但 MUST NOT 成为正式运行时解析来源，也 MUST NOT 成为新增模块的扩展方式。

#### Scenario: 根 SO 包含预定子模块
- **WHEN** 设计者打开 `CharacterConfigSO` 资产
- **THEN** 设计者 MUST 能看到以下子模块引用：
  - `stateMachine` → `CharacterStateMachineDefinitionSO`
  - `movement` → `BasicMovementConfigSO`
  - `locomotionAnimation` → `RunLocomotionAnimationConfigSO`
- **AND** 每个必需子模块引用缺失时，运行时 MUST 输出可诊断配置错误
- **AND** 系统 MUST NOT 静默使用旧字段、代码默认值或场景查找结果替代缺失子模块

#### Scenario: 子 SO 保持独立可编辑
- **WHEN** 设计者新创建 `CharacterConfigSO`
- **THEN** 设计者 MUST 能独立创建子 SO 资产（通过各子类型的 CreateAssetMenu）
- **AND** 再将子 SO 拖入根 SO 的子模块引用字段

### Requirement: PlayerLocomotionController 从根 SO 解析子配置
`PlayerLocomotionController` MUST 提供 `characterConfig` 序列化字段作为角色配置根入口。运行时读取子配置 MUST 通过该根 SO 解引用；旧平铺子模块序列化字段 MAY 保留为迁移遗留，但不得作为 fallback，也不得覆盖根 SO 的解析结果。

#### Scenario: 运行时解引用根 SO
- **GIVEN** `PlayerLocomotionController` 已赋值 `characterConfig`
- **AND** `characterConfig` 的各子模块引用非空
- **WHEN** 控制器一帧内需要读取移动配置、动画配置或状态机定义
- **THEN** 它 MUST 从 `characterConfig.Movement`、`characterConfig.LocomotionAnimation` 和 `characterConfig.StateMachine` 获取
- **AND** MUST NOT 通过独立的 `stateMachineDefinition`、`runAnimationConfig` 或 `config` 字段覆盖根 SO 的非空子配置

#### Scenario: 缺失正式配置时报错
- **GIVEN** `PlayerLocomotionController` 加载时
- **AND** `characterConfig` 为空或必需子模块为空
- **WHEN** 正式 gameplay 路径需要对应配置
- **THEN** 系统 MUST 输出明确配置错误诊断
- **AND** MUST 停止相关状态机 tick 或输出提交
- **AND** MUST NOT 从旧平铺字段、子类型默认值、`Resources`、全局单例或代码默认值继续运行

#### Scenario: 新增模块时不需修改 Controller 字段
- **WHEN** 后续新增 `AimingSO` 或 `ActionSO` 等子模块
- **THEN** 开发者在 `CharacterConfigSO` 上增加一个引用字段即可
- **AND** `PlayerLocomotionController` 不应再新增对应的平铺序列化字段

### Requirement: 向后兼容
系统 MUST 确保现有场景资产、预制体和运行时引用在升级本变更后不产生硬加载错误或序列化数据丢失。兼容目标是保留可迁移数据并给出清晰诊断，而不是通过旧字段 fallback 继续正式运行。

#### Scenario: 旧场景加载兼容
- **GIVEN** 现有场景 `Sandbox.unity` 中的 `PlayerLocomotionController` 持有旧平铺序列化字段
- **WHEN** 变更后的代码首次加载该场景
- **THEN** 旧序列化数据 MUST 不丢失
- **AND** 系统 MUST 能提示需要迁移到 `CharacterConfigSO`
- **AND** 系统 MUST NOT 降级 fallback 使用旧字段值作为正式运行时配置

#### Scenario: 状态机配置目录迁移
- **WHEN** 本变更实施完成
- **THEN** 默认状态机配置 MUST 位于 `Assets/Configs/3C/StateMachine/DefaultCharacterStateMachine.asset`
- **AND** 旧 `Assets/Configs/3C/Statemachine/` MUST NOT 作为并行状态机配置目录保留
- **AND** `Assets/Configs/3C/Movement/BasicMovementConfig.asset` 和 `Assets/Configs/3C/Animation/Locomotion/Corin/DefaultRunLocomotionAnimationConfig.asset` MUST 不被移动或删除

### Requirement: 验证
系统 MUST 通过自动测试、编译检查、OpenSpec 校验和手动验证。

#### Scenario: 自动测试覆盖配置解析
- **WHEN** 运行 EditMode 测试
- **THEN** 测试 MUST 覆盖 `CharacterConfigSO` 空引用会产生配置错误
- **AND** MUST 覆盖 `PlayerLocomotionController` 从根 SO 解析子配置
- **AND** MUST 覆盖旧字段不会作为 fallback 被运行时读取

#### Scenario: 编译和 OpenSpec 校验
- **WHEN** 实施完成
- **THEN** 项目 MUST 通过 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`
- **AND** MUST 通过 `dotnet build .\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`
- **AND** MUST 通过 `openspec validate refactor-state-machine-runtime-authority --strict --no-interactive`
- **AND** 验证 MUST NOT 使用 Unity batchmode

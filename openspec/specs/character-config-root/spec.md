# character-config-root Specification

## Purpose
定义角色配置根 `CharacterConfigSO` 的归属、子配置解析顺序和旧场景兼容边界，避免新模块继续在控制器上扩散平铺配置入口。
## Requirements
### Requirement: 角色配置根 SO
系统 MUST 提供一个 `CharacterConfigSO` 作为角色配置的根入口。角色子系统配置 SHOULD 优先通过根 SO 的命名子模块引用访问；`PlayerLocomotionController` 上的旧平铺序列化字段仅作为旧场景和旧 prefab 的兼容 fallback，MUST NOT 成为新增模块的扩展方式。

#### Scenario: 根 SO 包含预定子模块
- **WHEN** 设计者打开 `CharacterConfigSO` 资产
- **THEN** 设计者 MUST 能看到以下子模块引用：
  - `stateMachine` → `CharacterStateMachineDefinitionSO`
  - `movement` → `BasicMovementConfigSO`
  - `locomotionAnimation` → `RunLocomotionAnimationConfigSO`
  - `turnInPlace` → `TurnInPlaceAnimationConfigSO`
- **AND** 每个子模块引用 MUST 可为空；系统 MUST 对空引用有合理的降级行为

#### Scenario: 子 SO 保持独立可编辑
- **WHEN** 设计者新创建 `CharacterConfigSO`
- **THEN** 设计者 MUST 能独立创建子 SO 资产（通过各子类型的 CreateAssetMenu）
- **AND** 再将子 SO 拖入根 SO 的子模块引用字段

### Requirement: PlayerLocomotionController 从根 SO 解析子配置
`PlayerLocomotionController` MUST 提供 `characterConfig` 序列化字段作为角色配置根入口。运行时读取子配置 MUST 优先通过该根 SO 解引用；旧平铺子模块序列化字段 MAY 保留为兼容 fallback，但不得覆盖非空根 SO 的解析结果。

#### Scenario: 运行时解引用根 SO
- **GIVEN** `PlayerLocomotionController` 已赋值 `characterConfig`
- **AND** `characterConfig` 的各子模块引用非空
- **WHEN** 控制器一帧内需要读取移动配置、动画配置或状态机定义
- **THEN** 它 MUST 从 `characterConfig.Movement`、`characterConfig.LocomotionAnimation`、`characterConfig.TurnInPlace` 和 `characterConfig.StateMachine` 获取
- **AND** MUST NOT 通过独立的 `stateMachineDefinition`、`runAnimationConfig`、`turnInPlaceAnimationConfig` 或 `config` 字段覆盖根 SO 的非空子配置

#### Scenario: 降级 fallback 保护
- **GIVEN** `PlayerLocomotionController` 加载时
- **AND** `characterConfig` 为空
- **THEN** 系统 MUST 尝试从旧平铺字段读取子 SO
- **AND** 若旧字段也不可用，系统 MUST 能使用子类型默认值或等价 fallback
- **AND** 系统 MUST 在任何情况下不因配置缺失导致 NullReferenceException

#### Scenario: 新增模块时不需修改 Controller 字段
- **WHEN** 后续新增 `AimingSO` 或 `ActionSO` 等子模块
- **THEN** 开发者在 `CharacterConfigSO` 上增加一个引用字段即可
- **AND** `PlayerLocomotionController` 不应再新增对应的平铺序列化字段

### Requirement: 向后兼容
系统 MUST 确保现有场景资产、预制体和运行时引用在升级本变更后不产生硬加载错误或序列化数据丢失。

#### Scenario: 旧场景加载兼容
- **GIVEN** 现有场景 `Sandbox.unity` 中的 `PlayerLocomotionController` 持有旧平铺序列化字段
- **WHEN** 变更后的代码首次加载该场景
- **THEN** 旧序列化数据 MUST 不丢失
- **AND** `characterConfig` 新字段初始为空
- **AND** 系统 MUST 能降级 fallback 使用旧字段值

#### Scenario: 资产目录不重构
- **WHEN** 本变更实施完成
- **THEN** 现有子 SO 资产文件 MUST 保持原路径
- **AND** `Assets/Configs/3C/Statemachine/DefaultCharacterStateMachine.asset`、`Assets/Configs/3C/Movement/BasicMovementConfig.asset`、`Assets/Configs/3C/Animation/Locomotion/Corin/DefaultRunLocomotionAnimationConfig.asset` 和 `Assets/Configs/3C/Animation/Locomotion/Corin/CorinTurnInPlaceAnimationConfig.asset` MUST 不被移动或删除

### Requirement: 验证
系统 MUST 通过自动测试、编译检查和 OpenSpec 校验。

#### Scenario: 自动测试覆盖配置解析
- **WHEN** 运行 EditMode 测试
- **THEN** 测试 MUST 覆盖 `CharacterConfigSO` 空引用读取返回 null
- **AND** MUST 覆盖 `PlayerLocomotionController` 从根 SO 解析子配置
- **AND** MUST 覆盖降级 fallback 路径

#### Scenario: 编译和 OpenSpec 校验
- **WHEN** 实施完成
- **THEN** 项目 MUST 通过 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`
- **AND** MUST 通过 `dotnet build .\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`
- **AND** MUST 通过 `openspec validate consolidate-character-config-root --strict --no-interactive`
- **AND** 验证 MUST NOT 使用 Unity batchmode

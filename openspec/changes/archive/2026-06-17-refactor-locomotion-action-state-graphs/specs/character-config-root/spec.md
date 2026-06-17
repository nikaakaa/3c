# character-config-root Delta

## MODIFIED Requirements
### Requirement: 角色配置根 SO
系统 MUST 提供一个 `CharacterConfigSO` 作为角色配置的根入口。角色子系统配置 MUST 通过根 SO 的命名子模块引用访问；`PlayerLocomotionController` 上的旧平铺序列化字段 MAY 暂时保留为迁移遗留数据，但 MUST NOT 成为正式运行时解析来源，也 MUST NOT 成为新增模块的扩展方式。默认 Corin 配置中的状态图引用 MUST 被正式解释为 Locomotion graph 引用，Action lifecycle、Dodge action config、Action animation config 和 BodyClaimPolicy MUST 通过 Action 相关子配置解析。

#### Scenario: 根 SO 包含预定子模块
- **WHEN** 设计者打开 `CharacterConfigSO` 资产
- **THEN** 设计者 MUST 能看到以下子模块引用：
  - `stateMachine` 或后续批准的 `locomotionStateGraph` → `CharacterStateMachineDefinitionSO`
  - `movement` → `BasicMovementConfigSO`
  - `locomotionAnimation` → `RunLocomotionAnimationConfigSO`
  - `fullBodyAction` 或等价 Action 逻辑入口
  - `fullBodyActionAnimation` 或等价 Action 动画入口
  - `bodyClaimPolicy` 或等价 BodyClaim policy 入口
- **AND** 每个必需子模块引用缺失时，运行时 MUST 输出可诊断配置错误
- **AND** 系统 MUST NOT 静默使用旧字段、代码默认值或场景查找结果替代缺失子模块

#### Scenario: 子 SO 保持独立可编辑
- **WHEN** 设计者新创建 `CharacterConfigSO`
- **THEN** 设计者 MUST 能独立创建子 SO 资产
- **AND** 再将子 SO 拖入根 SO 的子模块引用字段
- **AND** Action lifecycle config 和 BodyClaimPolicy MUST 不被塞回 Locomotion graph

### Requirement: Corin 默认角色配置闭环资产
系统 MUST 维护一个 Corin 默认角色配置根资产，作为默认角色配置的唯一正式入口。该根资产 MUST 能解析 Locomotion graph、基础移动、Locomotion 动画、Action Interrupt 策略、Action Catalog、BodyClaimPolicy、输入和相机配置。默认根资产 MUST NOT 通过旧 mixed graph 同时解析 Locomotion 和 Action lifecycle。

#### Scenario: 根资产引用完整
- **WHEN** 自动校验加载 `Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset`
- **THEN** Locomotion graph、Movement、LocomotionAnimation、ActionInterruptPolicy、ActionCatalog、BodyClaimPolicy、InputActions、MoveAction、RunAction、LookAction、DodgeInputAction 和 CameraConfig MUST 全部可解析
- **AND** 缺失任一必需引用 MUST 被报告为配置错误
- **AND** 系统 MUST NOT 使用旧 controller 字段补齐缺失引用

#### Scenario: 根资产不引用旧 mixed graph
- **WHEN** 自动校验追踪 Corin 根配置的正式引用链
- **THEN** 引用链 MUST NOT 把包含 `Action.Dodge` 的 mixed `CorinStateMachine.asset` 作为正式 Locomotion graph
- **AND** MUST NOT 包含 `Assets/Configs/3C/Animacer/`
- **AND** MUST NOT 包含 `Assets/Configs/3C/Statemachine/`
- **AND** MUST NOT 包含 `Pramater` 拼写目录
- **AND** MUST NOT 包含 `TestTurnback`、`turnback` 或 `testTurn` 命名资产作为正式配置

#### Scenario: 根资产引用无悬空 GUID
- **WHEN** 自动校验 Corin 根配置和关键子资产引用
- **THEN** 每个正式引用 MUST 能通过 AssetDatabase 或等价资产数据库解析
- **AND** dangling GUID、空引用或缺失 `.meta` MUST 被报告为配置错误

## MODIFIED Requirements
### Requirement: Corin Locomotion graph 资产目录
系统 MUST 将 Corin 默认 Locomotion graph 的正式资产放置在 `Assets/Configs/3C/StateMachine/Locomotion/Corin/` 或经批准的等价 Locomotion 配置目录。旧 mixed graph 资产 MAY 在迁移期间保留为历史文件或被删除，但 MUST NOT 作为正式配置根 fallback。

#### Scenario: 正式 Locomotion graph 路径
- **WHEN** 自动校验默认 Corin 配置
- **THEN** 正式 Locomotion graph MUST 位于 `Assets/Configs/3C/StateMachine/Locomotion/Corin/`
- **AND** graph MUST 只包含批准的 `Locomotion.*` state
- **AND** graph MUST NOT 包含 `Action.*` state

#### Scenario: 不使用 fallback
- **GIVEN** 正式 Locomotion graph 引用缺失
- **WHEN** 正式 gameplay 路径需要 Locomotion graph
- **THEN** 系统 MUST 报告明确配置错误
- **AND** MUST NOT fallback 到旧 `CorinStateMachine.asset`
- **AND** MUST NOT 从 Resources、代码默认值或 scene 查找生成隐藏配置

## MODIFIED Requirements
### Requirement: Corin 输入配置保持单一路径
系统 MUST 通过 Corin 根配置引用的正式 `InputActionAsset` 和 input reference 资产解析 Move、Look、Run 与 Dodge 输入。Shift MUST 同时绑定 Run input fact 与 Dodge request input；Directional Dodge 完成后的持续 Run MUST 通过 Locomotion Run latch 表达，而不是通过额外 fallback 输入、第二套按键配置或要求 Shift 持续按住。

#### Scenario: Shift 同时绑定 Run 与 Dodge
- **WHEN** 自动校验 Corin 正式输入配置
- **THEN** Shift MUST 绑定到 Run action
- **AND** Shift MUST 绑定到 Dodge request action
- **AND** Move、Look、Run、Dodge 的正式引用 MUST 来自根配置引用链
- **AND** 系统 MUST NOT 通过 controller legacy 字段、Resources 或场景查找创建第二套输入绑定

#### Scenario: Run latch 不依赖持续按住 Shift
- **GIVEN** Directional Dodge 已经通过 Shift 请求进入
- **AND** 动作完成帧仍有移动输入
- **WHEN** 玩家松开 Shift 但保持移动输入
- **THEN** 后续 Run MUST 由 Locomotion runtime 的 Run latch 决定
- **AND** 输入配置 MUST NOT 要求 Run action 在后续帧继续为 pressed 才能维持 Run

#### Scenario: 无移动或 Backstep 不产生 Run 配置例外
- **GIVEN** 玩家无方向按下 Shift 或 Directional Dodge 完成帧没有移动输入
- **WHEN** Action lifecycle 完成该动作
- **THEN** 输入配置 MUST NOT 通过隐藏 Run fallback 强制进入 Run
- **AND** Locomotion MUST 能按正式状态回到 Idle 或 Walk 起步

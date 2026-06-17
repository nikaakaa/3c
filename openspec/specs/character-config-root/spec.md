# character-config-root Specification

## Purpose
定义角色配置根 `CharacterConfigSO` 的归属、子配置解析顺序和旧场景兼容边界，避免新模块继续在控制器上扩散平铺配置入口。
## Requirements
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
- **THEN** 默认状态机配置 MUST 位于 `Assets/Configs/3C/StateMachine/Locomotion/Corin/CorinLocomotionStateGraph.asset`
- **AND** 旧 `Assets/Configs/3C/Statemachine/` MUST NOT 作为并行状态机配置目录保留
- **AND** `Assets/Configs/3C/Movement/BasicMovementConfig.asset` 和 `Assets/Configs/3C/Animation/Corin/Locomotion/CorinLocomotionAnimationConfig.asset` MUST 不被移动或删除

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

### Requirement: 角色配置根作为作者总入口
系统 MUST 让 `CharacterConfigSO` 或批准的等价角色配置根成为默认角色配置的作者总入口。该入口 MUST 以命名子模块方式组织正式配置引用，使设计者能从一个资产追踪状态机、基础移动、动作逻辑、动作动画、Locomotion 动画、Animancer 表现、输入和相机配置。该入口 MUST NOT 通过旧平铺字段、Resources、全局单例或硬编码路径提供 fallback。

#### Scenario: 默认角色根位于角色目录
- **WHEN** 检查默认 Corin 角色配置根
- **THEN** 正式资产 MUST 位于 `Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset`
- **AND** `Assets/Configs/3C/CharacterConfig.asset` MUST NOT 作为第二正式入口保留
- **AND** 迁移 MUST 保留 Unity 引用所需的 `.meta` GUID 或更新所有正式引用

#### Scenario: 设计者从根入口追踪子配置
- **WHEN** 设计者打开默认角色配置根
- **THEN** 设计者 MUST 能定位 `stateMachine` 或等价状态机配置引用
- **AND** MUST 能定位 `movement` 或等价基础移动配置引用
- **AND** MUST 能定位 `fullBodyAction` 或等价动作逻辑配置引用
- **AND** MUST 能定位 `fullBodyActionAnimation` 或等价动作动画配置引用
- **AND** MUST 能定位 `locomotionAnimation` 或等价基础移动动画配置引用
- **AND** MUST 能定位 Generic 或等价正式 Animancer rig variant 配置引用
- **AND** MUST NOT 要求 Humanoid rig variant 作为本次默认角色配置根的必需引用
- **AND** MUST 能定位 `input` / `inputReferences` 或等价输入配置引用
- **AND** MUST 能定位 `camera` 或等价相机配置引用

#### Scenario: 缺失正式子配置不 fallback
- **GIVEN** 默认角色配置根缺失任一正式必需子配置
- **WHEN** 正式 gameplay 路径需要该配置
- **THEN** 系统 MUST 输出明确配置错误诊断
- **AND** MUST 停止对应状态机、动作、动画、输入或相机输出
- **AND** MUST NOT 从旧目录、旧字段、代码默认值或场景查找结果继续运行

#### Scenario: prefab 只装配正式根入口
- **WHEN** 检查默认可琳 prefab 或正式场景装配
- **THEN** 角色主调度入口 MUST 能通过角色配置根解析正式子配置
- **AND** 新增正式配置 Module 时 MUST 优先增加角色配置根的命名子模块引用
- **AND** 不应继续在 controller 上新增互不相干的平铺配置字段作为正式扩展方式

### Requirement: 配置作者入口校验口径
系统 MUST 将 `CharacterConfigSO` 视为角色配置作者总入口。controller 上的旧平铺序列化字段 MUST 从正式运行时类型和正式 prefab/scene 序列化中退役，不得作为正式运行时配置入口、fallback 或新增模块扩展方式。系统 MUST 提供自动校验报告旧字段、第二正式入口和缺失根配置。

#### Scenario: 旧字段不保留在正式运行时
- **WHEN** 检查 `PlayerLocomotionController`、`FullBodyActionRuntime`、角色 prefab 或正式 scene
- **THEN** `characterConfig` MUST 指向正式角色根配置
- **AND** `runAnimationConfig`、`config`、`stateMachineDefinition`、`interruptPolicySet`、`dodgeActionConfig` 等旧平铺字段 MUST NOT 作为正式类型字段或正式序列化键存在
- **AND** 系统 MUST NOT 通过这些旧字段补齐缺失的根配置子模块

#### Scenario: 新模块从根入口扩展
- **WHEN** 后续新增 Action、Input、Camera、UpperBody 或 LowerBody 配置模块
- **THEN** 新模块 MUST 优先作为 `CharacterConfigSO` 的命名子模块接入
- **AND** controller MUST NOT 新增同义平铺配置字段作为正式扩展方式

#### Scenario: 缺失根配置不会使用旧入口
- **GIVEN** `CharacterConfigSO` 或对应子模块为空
- **WHEN** 正式 gameplay 路径读取该配置
- **THEN** 系统 MUST 报告配置缺失或停止对应输出
- **AND** MUST NOT 读取旧平铺字段继续运行

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

### Requirement: Corin Prefab 使用正式角色配置根
系统 MUST 让 Corin 正式角色 prefab 和正式场景实例通过 `CorinCharacterConfig.asset` 作为配置根入口。Prefab 和 scene override MUST NOT 通过旧平铺配置字段形成第二正式入口或 fallback。

#### Scenario: Prefab 绑定同一根配置
- **WHEN** 自动校验 `Assets/Prefabs/Character/可琳.prefab` 和 `Assets/Prefabs/Character/可琳_Humanoid.prefab`
- **THEN** `PlayerLocomotionController.characterConfig` MUST 指向 `CorinCharacterConfig.asset`
- **AND** `FullBodyActionRuntime.characterConfig` 和 `CharacterFrameRuntimeController.characterConfig` MUST 指向同一个 `CorinCharacterConfig.asset`
- **AND** 两个 controller MUST NOT 通过各自平铺字段解析不同子配置

#### Scenario: Scene override 不恢复旧入口
- **WHEN** 自动校验正式场景中的 Corin 角色实例
- **THEN** scene override MUST NOT 覆盖为旧 `runAnimationConfig`、`config`、`stateMachineDefinition`、`interruptPolicySet` 或 `dodgeActionConfig` 正式入口
- **AND** scene MUST NOT 引入第二个正式角色配置根

#### Scenario: Humanoid Prefab 不保留重复根字段
- **WHEN** 自动校验 `Assets/Prefabs/Character/可琳_Humanoid.prefab`
- **THEN** `PlayerLocomotionController` 的有效 `characterConfig` MUST 指向 `CorinCharacterConfig.asset`
- **AND** YAML 或 SerializedObject 校验 MUST NOT 发现第二个会覆盖正式根配置的同名残留字段

### Requirement: Character Runtime Controller 使用根配置
`CharacterFrameRuntimeController` 或等价正式角色入口 MUST 从 `CharacterConfigSO` 根配置追踪 Corin 当前 playable 主线需要的 StateMachine、Movement、LocomotionAnimation、Action Interrupt policy、Action Catalog、BodyClaimPolicy、Input 和 Camera 配置。它 MUST NOT 从 FullBody、Locomotion legacy serialized fields 或 `DodgeAction` 平铺字段建立正式 fallback 配置入口。

#### Scenario: Prefab 绑定根配置
- **WHEN** 检查 Corin 正式 prefab 上的角色 runtime 入口
- **THEN** `CharacterFrameRuntimeController` MUST 引用正式 `CharacterConfigSO`
- **AND** 该根配置 MUST 能追踪当前 playable 主线需要的子配置
- **AND** Action Catalog MUST 能解析 `Action.Dodge` definition
- **AND** FullBody、Locomotion legacy serialized config fields 或旧 `DodgeAction` 平铺字段 MUST NOT 成为正式 fallback

#### Scenario: Scene override 不恢复旧入口
- **WHEN** 检查纳入范围的 Corin playable scene override
- **THEN** override MUST 保持 `CharacterFrameRuntimeController` 作为正式入口
- **AND** MUST NOT 重新启用 FullBody 或 Locomotion autoUpdate 作为正式主线
- **AND** MUST NOT 通过 scene override 恢复 `DodgeAction` 平铺配置作为正式入口
- **AND** MUST NOT 新增第二 pipeline、第二 runner、第二 motion executor 或第二 animation presenter

#### Scenario: 缺失正式配置显式失败
- **GIVEN** `CharacterFrameRuntimeController` 缺少正式根配置或根配置缺少必要子配置
- **WHEN** 角色初始化或装配校验运行
- **THEN** 系统 MUST 报告明确错误
- **AND** MUST NOT 回退到 legacy flat fields 或旧 `DodgeAction` 字段
- **AND** MUST NOT 创建隐藏默认配置

### Requirement: 旧平铺配置入口必须退役

正式角色运行时 MUST 只通过 `CharacterConfigSO` 及其子配置解析 Locomotion 与 Action 配置。旧平铺序列化字段不得作为 fallback、兼容读取或未来动作模板继续存在。

#### Scenario: 正式运行时不读取旧平铺字段

- **GIVEN** Corin prefab 和正式 scene 已绑定 `CharacterConfigSO`
- **WHEN** Locomotion 与 Action runtime 初始化
- **THEN** 配置解析只读取角色根配置及其子配置
- **AND** `runAnimationConfig`、旧 `config`、旧 `stateMachineDefinition`、`interruptPolicySet`、`dodgeActionConfig` 不参与配置解析

#### Scenario: 缺失子配置时不 fallback 到旧字段

- **GIVEN** `CharacterConfigSO` 缺失 Locomotion 或 Action 子配置
- **WHEN** runtime 初始化或测试构造缺失配置场景
- **THEN** runtime 报告缺失正式配置
- **AND** runtime 不读取旧平铺字段补齐配置

### Requirement: 正式资产不得保留旧字段风险

正式 prefab、scene 和角色配置资产 MUST 不保留旧平铺字段的非空值；完成清理后，也不得保留可被 Unity 重新识别为正式配置面的旧字段序列化键。

#### Scenario: Prefab 不含旧字段残留

- **GIVEN** Corin 正式 prefab 被扫描
- **WHEN** 测试检查旧字段名和旧组件引用
- **THEN** prefab 不包含旧字段非空值
- **AND** prefab 不包含已退役字段的序列化键残留
- **AND** prefab 只通过 `CharacterConfigSO` 连接正式配置链

#### Scenario: Scene 不含旧字段残留

- **GIVEN** 正式 gameplay scene 被扫描
- **WHEN** 测试检查旧字段名和旧组件引用
- **THEN** scene 不包含旧字段非空值
- **AND** scene 不包含已退役字段的序列化键残留
- **AND** scene 不恢复旧配置入口或旧 presenter

### Requirement: 历史 GUID 迁移必须可追踪但不可成为旧路径入口

迁移后的正式配置资产 MAY 复用历史 GUID 保持引用稳定，但 MUST 位于正式目录，且旧目录不得作为加载或作者ing 入口继续存在。

#### Scenario: 迁移后的 Action 配置引用正式路径

- **GIVEN** Corin Dodge、RequestPolicy 等配置资产从旧目录迁移到角色专属正式目录
- **WHEN** 测试解析 GUID 引用
- **THEN** 引用解析到正式角色目录下的资产
- **AND** 旧 FullBody 配置目录不作为正式入口存在

#### Scenario: 旧 FullBody 状态机 GUID 不再被正式资产引用

- **GIVEN** 历史 FullBody 状态机资产已退役
- **WHEN** 测试扫描 prefab、scene 和角色配置资产
- **THEN** 正式资产不引用旧 FullBody 状态机 GUID
- **AND** Locomotion 状态图引用指向正式 Locomotion 配置目录

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


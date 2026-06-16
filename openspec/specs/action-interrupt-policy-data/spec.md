# action-interrupt-policy-data Specification

## Purpose
定义 Action 打断策略数据、SO 配置、编译校验和默认 Dodge 策略的数据权威，确保运行时仲裁只消费已编译策略而不散落硬编码规则。
## Requirements
### Requirement: 动作打断策略集合数据源
系统 MUST 提供可序列化的动作打断策略集合数据源，用于配置多条从当前状态到目标状态的打断许可规则。该数据源 MUST 使用稳定状态 ID、优先级、时间规则、时间窗口和 force 标记表达策略，并 MUST NOT 依赖 Unity 场景对象、AnimationClip、Animancer 运行时对象、Animator、CharacterController、Input System 或 BBB 运行时类型。

#### Scenario: 空策略集合合法
- **WHEN** 系统创建一个没有任何策略的策略集合
- **THEN** 该集合 MUST 被视为合法数据源
- **AND** 编译后的 runtime policy 列表 MUST 为空

#### Scenario: 策略定义可序列化
- **WHEN** 用户在 Unity Inspector 中配置一条动作打断策略
- **THEN** 策略 MUST 能保存 from state id、target state id、min priority、timing rule、window start、window end 和 force
- **AND** 策略 MUST NOT 要求保存动画 clip、角色 prefab 或场景实例引用

#### Scenario: 策略顺序稳定
- **GIVEN** 一个策略集合中存在多条策略定义
- **WHEN** 系统读取或编译该集合
- **THEN** 输出策略 MUST 保持与配置顺序一致

### Requirement: 策略集合编译
系统 MUST 提供从序列化策略定义到现有 `ActionInterruptPolicy` runtime 数据的编译步骤。编译步骤 MUST 只做数据转换和基础防御，不得调用仲裁器、状态机、动画播放 API 或运行时角色控制器。

#### Scenario: 单条策略编译为 runtime policy
- **GIVEN** 一条 from state id 为 `Action.Attack01`、target state id 为 `Action.Dodge` 的策略定义
- **WHEN** 系统编译策略集合
- **THEN** 输出列表 MUST 包含一条 `ActionInterruptPolicy`
- **AND** 输出 policy 的 from state、target state、min priority、timing rule、window start、window end 和 force MUST 与定义一致

#### Scenario: 编译结果可被仲裁器消费
- **GIVEN** 一个已编译的 runtime policy 列表
- **AND** 一个匹配该 policy 的 `ActionInterruptRequest`
- **WHEN** 调用 `ActionInterruptArbiter`
- **THEN** 仲裁器 MUST 能基于编译结果产生确定裁决

#### Scenario: 编译器不产生运行时旁路
- **WHEN** 系统编译策略集合
- **THEN** 编译器 MUST NOT 调用 `ChangeState`
- **AND** MUST NOT 调用 Animancer 或 Animator 播放 API
- **AND** MUST NOT 修改 Transform、root motion 或角色 prefab

### Requirement: 策略集合校验
系统 MUST 对动作打断策略集合提供统一校验。校验 MUST 覆盖空 ID、负优先级、非法时间窗口、非法 timing rule 和重复策略，并输出可被测试和未来编辑器消费的错误或警告。

#### Scenario: 空状态 ID 报错
- **GIVEN** 一条策略定义缺少 from state id 或 target state id
- **WHEN** 系统校验策略集合
- **THEN** 校验结果 MUST 包含错误

#### Scenario: 非法优先级报错
- **GIVEN** 一条策略定义的 min priority 小于 0
- **WHEN** 系统校验策略集合
- **THEN** 校验结果 MUST 包含错误

#### Scenario: 非法时间窗口报错
- **GIVEN** 一条 `DuringElapsedTimeWindow` 策略的 window end 小于 window start
- **WHEN** 系统校验策略集合
- **THEN** 校验结果 MUST 包含错误

#### Scenario: 重复策略报告 warning
- **GIVEN** 一个策略集合中存在重复的 from state、target state 和 timing rule
- **WHEN** 系统校验策略集合
- **THEN** 校验结果 MUST 包含 warning
- **AND** 重复策略 MUST NOT 被静默忽略

### Requirement: Inspector 配置入口
系统 MUST 提供 Unity Inspector 可编辑的策略集合配置入口。该入口 MAY 使用 ScriptableObject，但 MUST 保持配置层和纯 runtime 仲裁模型分离。

#### Scenario: 创建策略集合资产
- **WHEN** 用户通过 Unity 资源菜单创建动作打断策略集合资产
- **THEN** 资产 MUST 允许用户编辑策略定义列表
- **AND** 资产 MUST 提供转换为纯策略集合或 runtime policy 列表的入口

#### Scenario: 配置资产不污染 solver
- **WHEN** 仲裁器或策略编译器处理 runtime policy
- **THEN** 它们 MUST NOT 要求持有 ScriptableObject、MonoBehaviour、Transform、AnimationClip 或 Animancer 对象

### Requirement: 现有运行时边界保持
系统 MUST 保持当前 Locomotion、Animancer Presenter 和动作打断仲裁器的边界。动作打断策略集合 MAY 作为 FullBody Action 请求准入配置接入运行时，但 MUST NOT 改变 `Idle / MoveStart / MoveLoop / MoveStop` 状态图，也不得让配置数据成为 `MoveStop -> MoveStart` 的必需依赖。

#### Scenario: 基础移动不依赖策略集合
- **WHEN** 当前基础移动状态机处理 `MoveStop` 中重新输入
- **THEN** `MoveStop -> MoveStart` MUST 继续由 Locomotion 状态图处理
- **AND** 基础移动状态机 MUST NOT 依赖动作打断策略集合

#### Scenario: Presenter 不读取策略集合
- **WHEN** 基础移动动画 Presenter 播放移动阶段 alias
- **THEN** Presenter MUST NOT 读取动作打断策略集合
- **AND** Presenter MUST NOT 通过策略集合决定业务打断

#### Scenario: FullBody Action 准入读取策略集合
- **WHEN** FullBody Action 请求门面处理 Dodge 或后续 Action 请求
- **THEN** 它 MAY 读取动作打断策略集合并编译 runtime policy
- **AND** 该读取 MUST 只用于动作请求仲裁
- **AND** MUST NOT 直接提交运动命令或动画播放命令

### Requirement: 可测试和可诊断
系统 MUST 提供自动测试和静态边界验证，证明策略集合可保存、可校验、可编译、可被仲裁器消费，并且不会引入动画或角色控制旁路。

#### Scenario: 自动测试覆盖策略数据
- **WHEN** 运行策略数据 EditMode 测试
- **THEN** 测试 MUST 覆盖空集合、单条编译、多条顺序、非法 ID、负优先级、非法窗口、重复 warning、SO 转换和仲裁器消费

#### Scenario: 静态验证模块边界
- **WHEN** 检查 `Assets/Scripts/Character/Action` 源码
- **THEN** 静态搜索 MUST 能确认该模块不引用 Animancer、AnimationClip、Animator、CharacterController、Cinemachine、Input System 或 `BBBNexus`

#### Scenario: 手动验证配置入口
- **WHEN** 用户在 Unity 中创建策略集合资产
- **THEN** 用户 MUST 能在 Inspector 中配置策略
- **AND** 不需要把动画 clip、角色 prefab 或场景对象拖入该资产

### Requirement: FullBody Action 策略装配入口
系统 MUST 为 FullBody Action 运行时准入提供明确的策略集合装配入口。该入口 MAY 位于 FullBody Action 控制器、角色动作配置或等价主装配点，但 MUST NOT 位于 Locomotion controller、movement pipeline 或 animation presenter。

#### Scenario: FullBody 控制器定位策略集合
- **WHEN** 角色 FullBody Action 请求门面处理 Dodge 请求
- **THEN** 它 MUST 能定位用于 `ActionInterruptArbiter` 的策略集合
- **AND** 策略集合 MUST 编译为纯 runtime policy 列表后再参与仲裁

#### Scenario: 缺失策略集合可诊断
- **GIVEN** 角色没有配置策略集合或策略集合无法编译
- **WHEN** 玩家提交 FullBody Action 请求
- **THEN** 系统 MUST 产生 rejected decision 或配置错误诊断
- **AND** 系统 MUST NOT 绕过策略集合直接让状态机进入动作

#### Scenario: Locomotion 不读取策略集合
- **WHEN** 基础移动处理 `Idle / MoveStart / MoveLoop / MoveStop`
- **THEN** Locomotion controller MUST NOT 读取动作打断策略集合
- **AND** movement pipeline MUST NOT 读取动作打断策略集合
- **AND** animation presenter MUST NOT 读取动作打断策略集合

### Requirement: 默认 Dodge 打断策略
系统 MUST 为默认可琳 FullBody Dodge 提供可配置的进入策略，表达从空 Action 或当前可允许状态进入 `Action.Dodge` 的最小优先级、时间规则、force 和抗性语义。

#### Scenario: 默认策略允许合法 Dodge
- **GIVEN** 当前动作状态为空 Action 或等价可允许状态
- **AND** Dodge 请求优先级满足策略最小优先级
- **AND** 当前 resistance 不阻挡请求
- **WHEN** FullBody Action 请求门面执行仲裁
- **THEN** `ActionInterruptArbiter` MUST 返回 accepted decision

#### Scenario: 默认策略拒绝低优先级 Dodge
- **GIVEN** 当前动作状态匹配默认 Dodge 策略
- **AND** Dodge 请求优先级低于策略最小优先级
- **WHEN** FullBody Action 请求门面执行仲裁
- **THEN** `ActionInterruptArbiter` MUST 返回 rejected decision
- **AND** 拒绝原因 MUST 表示优先级不足

### Requirement: FullBody 请求策略集合命名和归属
系统 MUST 将同时包含 Dodge、TurnBack 或后续 FullBody 状态请求策略的默认策略集合命名并归属为 `CorinFullBodyStateRequestPolicySet.asset` 或批准的等价 FullBody state request policy，而不是 Dodge-only policy。策略集合的名称、目录和根配置引用 MUST 反映其覆盖范围，避免设计者误判该资产只影响 `Action.Dodge`。

#### Scenario: 多请求策略集合不使用 Dodge-only 命名
- **GIVEN** 默认策略集合同时包含 `Action.Dodge` 和 `FullBody/Locomotion/TurnBack` 或等价 TurnBack request policy
- **WHEN** 检查该策略集合资产
- **THEN** 资产名称 MUST 为 `CorinFullBodyStateRequestPolicySet.asset` 或批准的等价 FullBody state request policy 名称
- **AND** 资产 MUST NOT 使用 `DefaultDodgeInterruptPolicySet` 或等价 Dodge-only 名称作为正式资产名

#### Scenario: 策略集合位于动作请求归属目录
- **WHEN** 检查默认策略集合目录
- **THEN** 策略集合 MUST 位于 `Assets/Configs/3C/Action/FullBody/RequestPolicy/` 或批准的等价 FullBody 请求策略目录
- **AND** 它 MUST NOT 放在 Locomotion animation、StateMachine topology 或 Animancer transition 目录下

#### Scenario: 缺失策略集合不回退旧 Dodge 策略
- **GIVEN** 角色配置根或正式装配点缺失 FullBody 请求策略集合
- **WHEN** 请求准入需要 priority、resistance 或 timing window policy
- **THEN** 系统 MUST 报告配置错误或拒绝对应请求
- **AND** MUST NOT 自动查找旧 `DefaultDodgeInterruptPolicySet` 路径作为 fallback


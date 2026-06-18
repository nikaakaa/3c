# action-interrupt-policy-data Specification

## Purpose
定义 Action 打断策略数据、SO 配置、编译校验和默认 Dodge 策略的数据权威，确保运行时仲裁只消费已编译策略而不散落硬编码规则。
## Requirements
### Requirement: 动作打断策略集合数据源
系统 MUST 提供可序列化的动作打断策略集合数据源，用于配置多条从当前状态到目标状态的打断许可规则。该数据源 MUST 使用稳定状态 ID、优先级、required fact id、force 和 resistance 语义表达策略，并 MUST NOT 依赖 Unity 场景对象、AnimationClip、Animancer 运行时对象、Animator、CharacterController、Input System 或 BBB 运行时类型。

#### Scenario: 空策略集合合法
- **WHEN** 系统创建一个没有任何策略的策略集合
- **THEN** 该集合 MUST 被视为合法数据源
- **AND** 编译后的 runtime policy 列表 MUST 为空

#### Scenario: 策略定义可序列化
- **WHEN** 用户在 Unity Inspector 中配置一条动作打断策略
- **THEN** 策略 MUST 能保存 from state id、target state id、min priority、required fact id、force 和 resistance 语义
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
- **AND** 输出 policy 的 from state、target state、min priority、required fact id、force 和 resistance 语义 MUST 与定义一致

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
系统 MUST 对动作打断策略集合提供统一校验。校验 MUST 覆盖空 ID、负优先级、缺失 required fact id、旧 elapsed timing 输入和重复策略，并输出可被测试和未来编辑器消费的错误或警告。

#### Scenario: 空状态 ID 报错
- **GIVEN** 一条策略定义缺少 from state id 或 target state id
- **WHEN** 系统校验策略集合
- **THEN** 校验结果 MUST 包含错误

#### Scenario: 非法优先级报错
- **GIVEN** 一条策略定义的 min priority 小于 0
- **WHEN** 系统校验策略集合
- **THEN** 校验结果 MUST 包含错误

#### Scenario: 旧 elapsed timing 输入需要迁移
- **GIVEN** 一条策略仍使用 `AfterElapsedTime`、`DuringElapsedTimeWindow` 或等价旧 elapsed timing 字段
- **WHEN** 系统校验策略集合
- **THEN** 校验结果 MUST 包含迁移诊断或错误
- **AND** compiler MUST NOT 将旧 elapsed timing 字段作为正式 runtime window 规则输出

#### Scenario: 重复策略报告 warning
- **GIVEN** 一个策略集合中存在重复的 from state、target state 和 required fact id
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
系统 MUST 保持当前 Locomotion、Animancer Presenter 和动作打断仲裁器的边界。动作打断策略集合 MAY 作为 Action 请求准入配置接入运行时，但 MUST NOT 改变 `Idle / MoveStart / MoveLoop / MoveStop` 状态图，也不得让配置数据成为 `MoveStop -> MoveStart` 的必需依赖。

#### Scenario: 基础移动不依赖策略集合
- **WHEN** 当前基础移动状态机处理 `MoveStop` 中重新输入
- **THEN** `MoveStop -> MoveStart` MUST 继续由 Locomotion 状态图处理
- **AND** 基础移动状态机 MUST NOT 依赖动作打断策略集合

#### Scenario: Presenter 不读取策略集合
- **WHEN** 基础移动动画 Presenter 播放移动阶段 alias
- **THEN** Presenter MUST NOT 读取动作打断策略集合
- **AND** Presenter MUST NOT 通过策略集合决定业务打断

#### Scenario: Action 准入读取策略集合
- **WHEN** Action 请求门面处理 Dodge 或后续 Action 请求
- **THEN** 它 MAY 读取动作打断策略集合并编译 runtime policy
- **AND** 该读取 MUST 只用于动作请求仲裁
- **AND** MUST NOT 直接提交运动命令或动画播放命令

### Requirement: 可测试和可诊断
系统 MUST 提供自动测试和静态边界验证，证明策略集合可保存、可校验、可编译、可被仲裁器消费，并且不会引入动画或角色控制旁路。

#### Scenario: 自动测试覆盖策略数据
- **WHEN** 运行策略数据 EditMode 测试
- **THEN** 测试 MUST 覆盖空集合、单条编译、多条顺序、非法 ID、负优先级、缺失 required fact、旧 elapsed timing 迁移诊断、重复 warning、SO 转换和仲裁器消费

#### Scenario: 静态验证模块边界
- **WHEN** 检查 `Assets/Scripts/Character/Action` 源码
- **THEN** 静态搜索 MUST 能确认该模块不引用 Animancer、AnimationClip、Animator、CharacterController、Cinemachine、Input System 或 `BBBNexus`

#### Scenario: 手动验证配置入口
- **WHEN** 用户在 Unity 中创建策略集合资产
- **THEN** 用户 MUST 能在 Inspector 中配置策略
- **AND** 不需要把动画 clip、角色 prefab 或场景对象拖入该资产

### Requirement: Action 策略装配入口
系统 MUST 为 Action 运行时准入提供明确的策略集合装配入口。该入口 MAY 位于 Action 控制器、角色动作配置或等价主装配点，但 MUST NOT 位于 Locomotion controller、movement pipeline 或 animation presenter。

#### Scenario: Action 门面定位策略集合
- **WHEN** 角色 Action 请求门面处理 Dodge 请求
- **THEN** 它 MUST 能定位用于 `ActionInterruptArbiter` 的策略集合
- **AND** 策略集合 MUST 编译为纯 runtime policy 列表后再参与仲裁

#### Scenario: 缺失策略集合可诊断
- **GIVEN** 角色没有配置策略集合或策略集合无法编译
- **WHEN** 玩家提交 Action 请求
- **THEN** 系统 MUST 产生 rejected decision 或配置错误诊断
- **AND** 系统 MUST NOT 绕过策略集合直接让状态机进入动作

#### Scenario: Locomotion 不读取策略集合
- **WHEN** 基础移动处理 `Idle / MoveStart / MoveLoop / MoveStop`
- **THEN** Locomotion controller MUST NOT 读取动作打断策略集合
- **AND** movement pipeline MUST NOT 读取动作打断策略集合
- **AND** animation presenter MUST NOT 读取动作打断策略集合

### Requirement: 默认 Dodge 打断策略
系统 MUST 为默认可琳 Dodge 提供可配置的进入策略，表达从空 Action 或当前可允许状态进入 `Action.Dodge` 的最小优先级、required fact id、force 和抗性语义。

#### Scenario: 默认策略允许合法 Dodge
- **GIVEN** 当前动作状态为空 Action 或等价可允许状态
- **AND** Dodge 请求优先级满足策略最小优先级
- **AND** 当前 resistance 不阻挡请求
- **WHEN** Action 请求门面执行仲裁
- **THEN** `ActionInterruptArbiter` MUST 返回 accepted decision

#### Scenario: 默认策略拒绝低优先级 Dodge
- **GIVEN** 当前动作状态匹配默认 Dodge 策略
- **AND** Dodge 请求优先级低于策略最小优先级
- **WHEN** Action 请求门面执行仲裁
- **THEN** `ActionInterruptArbiter` MUST 返回 rejected decision
- **AND** 拒绝原因 MUST 表示优先级不足

### Requirement: Action Interrupt 策略集合命名和归属
系统 MUST 将同时包含 Dodge、TurnBack 或后续动作中断请求策略的默认策略集合命名并归属为 `CorinActionInterruptPolicySet.asset` 或批准的等价 action interrupt policy，而不是 Dodge-only policy。策略集合的名称、目录和根配置引用 MUST 反映其覆盖范围，避免设计者误判该资产只影响 `Action.Dodge`。

#### Scenario: 多请求策略集合不使用 Dodge-only 命名
- **GIVEN** 默认策略集合同时包含 `Action.Dodge` 和 `Locomotion.TurnBack` 或等价 TurnBack request policy
- **WHEN** 检查该策略集合资产
- **THEN** 资产名称 MUST 为 `CorinActionInterruptPolicySet.asset` 或批准的等价 action interrupt policy 名称
- **AND** 资产 MUST NOT 使用 `DefaultDodgeInterruptPolicySet` 或等价 Dodge-only 名称作为正式资产名

#### Scenario: 策略集合位于动作请求归属目录
- **WHEN** 检查默认策略集合目录
- **THEN** 策略集合 MUST 位于 `Assets/Configs/3C/Action/Corin/InterruptPolicy/` 或批准的等价动作中断策略目录
- **AND** 它 MUST NOT 放在 Locomotion animation、StateMachine topology 或 Animancer transition 目录下

#### Scenario: 缺失策略集合不回退旧 Dodge 策略
- **GIVEN** 角色配置根或正式装配点缺失 Action Interrupt 策略集合
- **WHEN** 请求准入需要 priority、resistance 或 required fact / window fact policy
- **THEN** 系统 MUST 报告配置错误或拒绝对应请求
- **AND** MUST NOT 自动查找旧 `DefaultDodgeInterruptPolicySet` 路径作为 fallback

### Requirement: 状态请求策略不重复定义窗口时间
系统 MUST 让状态请求策略只描述从当前状态到目标状态的准入关系、最小请求优先级、force 和 required fact id。新增状态请求策略 MUST NOT 重新定义同一个窗口的 start/end timing；窗口 timing MUST 来自 `StateTimelinePolicy`，并由 sampler 产出 active facts。旧 `ActionInterruptPolicy` 的 elapsed timing rule MUST 被迁移为 required fact id、timeline fact source 或明确迁移诊断，不得作为正式 runtime 兼容规则保留。

#### Scenario: 新 Attack 策略只引用 combo window
- **GIVEN** Attack01 的 timeline policy 定义了 `attack01-combo` window
- **WHEN** 设计者配置 Attack01 到 Attack02 的请求策略
- **THEN** 策略 MUST 能引用 `ComboInputOpen` 或等价 required fact id
- **AND** 策略 MUST 配置 min priority 或 force
- **AND** 策略 MUST NOT 配置另一份 combo window start/end

#### Scenario: 旧 Dodge timing rule 必须迁移
- **GIVEN** 现有 Dodge 策略使用 elapsed time timing rule
- **WHEN** 本变更迁移策略数据源
- **THEN** 系统 MUST 将该规则迁移为 required fact id / timeline fact source 或报告明确迁移诊断
- **AND** 系统 MUST NOT 在正式 runtime policy 中继续保留 elapsed timing rule

### Requirement: 状态请求策略数据源
系统 MUST 提供可配置的状态请求策略数据源，用于描述从当前状态到目标状态的请求准入规则。该数据源 MUST 能覆盖现有 ActionInterruptPolicy 的 priority、resistance、force 和窗口事实语义，并 MUST 能引用或关联状态 timeline fact id。

#### Scenario: TurnBack 策略引用窗口
- **GIVEN** 策略 from state 为 `Locomotion.MoveLoop`
- **AND** target state 为 `Locomotion.TurnBack`
- **WHEN** 设计者配置策略
- **THEN** 策略 MUST 能引用 TurnBack 允许进入事实或等价 fact id
- **AND** MUST 能配置 min priority 和 force

#### Scenario: Dodge 现有策略可迁移
- **GIVEN** 当前已有 Dodge action interrupt policy
- **WHEN** 系统迁移到状态请求策略数据源
- **THEN** 现有 Dodge priority、窗口准入和 force 语义 MUST 能通过 required fact id 或 timeline fact source 保持
- **AND** 不需要状态机 transition 条件重新判断请求 priority

### Requirement: 策略数据编译到纯 runtime 数据
状态请求策略数据源 MUST 编译为纯 runtime policy 列表。编译器 MUST 只做数据转换和校验，不得调用状态机、Animancer、Animator、motion executor、CharacterController 或 Transform。

#### Scenario: 编译 TurnBack 策略
- **GIVEN** 一个 TurnBack 状态请求策略定义
- **WHEN** 系统编译策略集合
- **THEN** 输出 runtime policy MUST 包含 from state、target state、min priority、force 和 required fact id
- **AND** 输出 policy MUST 不包含 Unity 对象引用

#### Scenario: 缺失 fact 报告错误
- **GIVEN** 策略引用了不存在的 required fact id
- **WHEN** 系统校验策略集合
- **THEN** 校验结果 MUST 包含错误

### Requirement: 策略配置入口不污染 Locomotion
状态请求策略配置 MAY 由角色级 Action 配置、状态机配置或等价正式装配点引用，但 Locomotion movement pipeline、Animancer presenter 和 motion executor MUST NOT 直接读取策略 SO。

#### Scenario: Presenter 不读取策略
- **WHEN** 基础移动动画 Presenter 播放 TurnBack 或 MoveLoop 动画
- **THEN** Presenter MUST NOT 读取状态请求策略资产
- **AND** MUST NOT 由策略资产决定是否切换状态

### Requirement: Action Transition Policy Matrix 作者视图
系统 MUST 提供 Action Transition Policy Matrix 或批准等价作者视图，用于编辑跨 Action 请求准入关系。Matrix MUST 写回正式 Action interrupt / request policy 数据源，并编译为现有 `ActionInterruptPolicy`、状态请求策略 runtime policy 或批准等价纯 runtime policy。Matrix MUST NOT 成为 Branch graph、状态机 runner、motion executor、animation presenter、blackboard writer 或第二角色帧入口。

#### Scenario: Matrix row 编译为 runtime policy
- **GIVEN** matrix row 配置 from `Action.Block`、to `Action.GuardCounter`、request `Attack`、required fact `window.counter.open`
- **WHEN** policy compiler 编译该 matrix
- **THEN** 输出 runtime policy MUST 包含相同 from / to / request / required fact 语义
- **AND** `ActionInterruptArbiter` MUST 能消费该编译结果

#### Scenario: Matrix 不直接执行跳转
- **WHEN** 设计者保存 matrix
- **THEN** matrix adapter MUST NOT 调用 Action lifecycle 切换
- **AND** MUST NOT 调用 motion executor
- **AND** MUST NOT 调用 animation presenter
- **AND** MUST NOT 写 runtime blackboard

#### Scenario: Matrix 是 policy 数据视图
- **GIVEN** 设计者在 Matrix Editor 中新增一行 policy
- **WHEN** 保存该 matrix
- **THEN** 修改 MUST 写回正式 Action interrupt / request policy 数据源
- **AND** MUST NOT 只保存在 GraphView edge、EditorWindow state 或 preview-only object 中

### Requirement: Matrix Row 字段合同
Matrix row MUST 能表达 from action id、to action id、request kind、required fact id、min priority、force 和 resistance 语义。row MAY 包含 diagnostics label 或 editor display metadata，但该 metadata MUST NOT 参与 runtime 仲裁。row MUST NOT 保存 AnimationClip、Animator、Animancer runtime object、Transform、CharacterController、MonoBehaviour、GraphView edge 或 EditorWindow state。

#### Scenario: Row 字段完整编译
- **GIVEN** matrix row 配置了 from、to、request、required fact、min priority、force 和 resistance
- **WHEN** compiler 编译该 row
- **THEN** runtime policy MUST 保留这些仲裁所需语义
- **AND** runtime policy MUST NOT 保存 editor-only display metadata 作为判断依据

#### Scenario: Row 不包含 Unity 对象引用
- **WHEN** validator 检查 matrix row
- **THEN** row MUST NOT 要求配置 AnimationClip、角色 prefab、Animator、AnimancerState、Transform 或 scene object
- **AND** compiler MUST NOT 将这些对象写入 runtime policy

### Requirement: Matrix Scope 仅覆盖 Action-to-Action
Action Transition Policy Matrix 第一版 MUST 只表达 `Action.* -> Action.*` 或批准等价 action id 之间的跨 Action 准入关系。Matrix authoring、editor、validator 和 tests MUST NOT 将 Locomotion state、TurnBack state、Branch TimelineNode、GraphView node 或 editor lane 当成本 Matrix row 的 from/to。Matrix compiler MAY 映射到现有底层 policy runtime 的 state id 字段，但该底层字段名 MUST NOT 扩大 Matrix 作者视图 scope。

#### Scenario: Action row 合法
- **GIVEN** matrix row 的 from 为 `Action.Block`
- **AND** to 为 `Action.GuardCounter`
- **WHEN** validator 检查该 row
- **THEN** from/to scope MUST 被视为合法 Action-to-Action row

#### Scenario: Locomotion target 被拒绝
- **GIVEN** matrix row 的 from 为 `Action.Attack01`
- **AND** to 为 `Locomotion.TurnBack`
- **WHEN** validator 检查该 row
- **THEN** validator MUST 报告 scope 错误
- **AND** MUST NOT 将该 row 作为 Action Transition Policy Matrix row 编译

#### Scenario: Branch TimelineNode 不能作为 target
- **GIVEN** matrix row 的 to 被配置为 `Action.Block.Loop` 或某个 Branch TimelineNode id
- **WHEN** validator 检查该 row
- **THEN** validator MUST 报告 target 不是 action id
- **AND** MUST NOT 将 Branch 内部节点解释成跨 Action 目标

### Requirement: Matrix Row 校验
系统 MUST 对 matrix row 提供统一校验。校验 MUST 覆盖空 from action id、空 to action id、空 request kind、非 Action scope from/to、负 min priority、缺失 required fact id、重复 row、非法 Branch target 和窗口 timing 重复定义。存在 error 时 compiler MUST NOT 生成可被正式 runtime 消费的半成品 policy。

#### Scenario: 空 from/to/request 报错
- **GIVEN** matrix row 缺少 from action id、to action id 或 request kind
- **WHEN** validator 检查该 row
- **THEN** validator MUST 报告错误

#### Scenario: 负 priority 报错
- **GIVEN** matrix row 的 min priority 小于 0
- **WHEN** validator 检查该 row
- **THEN** validator MUST 报告错误

#### Scenario: 重复 row 可诊断
- **GIVEN** matrix 中存在两条 from、to、request 和 required fact 完全相同的 row
- **WHEN** validator 检查 matrix
- **THEN** validator MUST 报告 warning 或 error
- **AND** MUST NOT 静默忽略其中一条 row

### Requirement: 跨 Action 跳转不写入 Branch 图
跨 Action 跳转 MUST 通过 request provider、interrupt arbiter、action lifecycle 和 policy 数据完成。CommittedActionBranch MUST NOT 直接持有指向另一个 Action root 的跳转边，Branch condition 命中 required fact 时也 MUST NOT 直接启动另一个 Action。

#### Scenario: Block 到 GuardCounter 走 policy
- **GIVEN** `Action.Block` 当前输出 `window.counter.open`
- **AND** 玩家提交 Attack 或 Counter 请求
- **WHEN** policy 允许从 `Action.Block` 到 `Action.GuardCounter`
- **THEN** Action interrupt arbiter MAY accept `Action.GuardCounter`
- **AND** `Action.Block` branch MUST NOT 直接跳到 `Action.GuardCounter` branch root

#### Scenario: Branch 只输出当前 Action outcome
- **WHEN** `Action.Block` branch evaluator 运行
- **THEN** 它 MUST 只输出 `Action.Block` 内部 TimelineNode 的 outcome
- **AND** MUST NOT 创建新的 `Action.GuardCounter` lifecycle state

#### Scenario: Branch target 不允许是另一个 Action
- **GIVEN** 设计者尝试把 Branch child target 配置为 `Action.GuardCounter`
- **WHEN** branch validator 或 matrix validator 检查配置
- **THEN** 系统 MUST 报告配置错误
- **AND** MUST 引导该关系进入 Action Transition Policy Matrix 或批准等价 policy 数据

### Requirement: Matrix 策略引用事实而不重复窗口时间
新增跨 Action policy row MUST 优先引用 required fact id 表达窗口准入，MUST NOT 重新配置同一个窗口的 start/end timing。窗口 timing MUST 来自 Action Timeline、ActionTimeline fact source 或批准等价动作时间源。旧 elapsed timing rule MUST 被迁移为 required fact id、timeline fact source 或明确迁移诊断，不得作为正式 runtime 兼容规则保留。

#### Scenario: Counter policy 引用窗口事实
- **GIVEN** `Action.Block` timeline 声明 `window.counter.open`
- **WHEN** 设计者配置 `Action.Block -> Action.GuardCounter`
- **THEN** policy row MUST 引用 `window.counter.open`
- **AND** policy row MUST NOT 配置另一份 counter window start/end

#### Scenario: 缺失 required fact 报错
- **GIVEN** policy row 引用 `window.counter.open`
- **AND** 当前配置没有任何已声明 fact id 匹配它
- **WHEN** policy validator 运行
- **THEN** validator MUST 报告错误
- **AND** runtime MUST NOT 使用隐藏默认窗口允许跳转

#### Scenario: Matrix 不通过前缀猜测匹配 fact
- **GIVEN** timeline 只声明 `window.counter.open`
- **AND** policy row 引用 `window.counter`
- **WHEN** policy validator 运行
- **THEN** validator MUST 报告缺失或不匹配 fact id
- **AND** MUST NOT 因字符串相似而接受该 policy

#### Scenario: Matrix 使用共享 Fact Resolver
- **GIVEN** condition/fact framework 的共享 compile context 声明了 `window.counter.open`
- **AND** matrix row 引用 `window.counter.open`
- **WHEN** policy validator 校验该 row
- **THEN** validator MUST 通过共享 fact resolver 或批准等价 compile context 解析该 fact
- **AND** MUST NOT 使用 matrix-only 隐藏 fact registry 得出不同结果

### Requirement: Matrix Runtime 仲裁语义
Matrix 编译结果 MUST 由 Action interrupt arbiter 或批准等价仲裁器消费。仲裁器 MUST 同时考虑 current action、request kind、required fact、min priority、force 和 resistance 语义。Matrix 本身 MUST NOT 执行 Action lifecycle 切换；accepted decision MUST 交由正式 Action lifecycle 推进。

#### Scenario: Fact active 且 priority 满足时接受
- **GIVEN** 当前 active action 为 `Action.Block`
- **AND** request kind 为 `Attack`
- **AND** active facts 包含 `window.counter.open`
- **AND** request priority 满足 policy min priority
- **WHEN** Action interrupt arbiter 消费 matrix 编译结果
- **THEN** 仲裁器 MUST 返回 accepted `Action.GuardCounter` 或批准等价 accepted decision

#### Scenario: Fact missing 时拒绝
- **GIVEN** 当前 active action 为 `Action.Block`
- **AND** request kind 为 `Attack`
- **AND** active facts 不包含 `window.counter.open`
- **WHEN** Action interrupt arbiter 消费 matrix 编译结果
- **THEN** 仲裁器 MUST 返回 rejected decision 或明确 diagnostics
- **AND** MUST NOT 因存在 from/to/request 匹配而忽略 required fact

#### Scenario: Accepted decision 交给 lifecycle
- **GIVEN** 仲裁器返回 accepted `Action.GuardCounter`
- **WHEN** 本帧 Action runtime 推进
- **THEN** Action lifecycle MUST 负责进入 `Action.GuardCounter`
- **AND** matrix compiler、matrix adapter 和 Branch evaluator MUST NOT 直接创建 active lifecycle state


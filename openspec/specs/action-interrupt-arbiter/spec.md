# action-interrupt-arbiter Specification

## Purpose
定义 Action 打断仲裁的请求、上下文、优先级、抗性、force、timing window 和状态机准入边界。
## Requirements
### Requirement: 纯数据动作打断输入
系统 MUST 提供纯数据动作打断请求、当前状态上下文和裁决结果模型，用于在逻辑层表达“当前状态能否被某个动作请求打断”。这些模型 MUST NOT 依赖 Unity 场景对象、Animancer 运行时对象、AnimationClip、Animator、CharacterController、Input System 或 BBB 运行时类型。

#### Scenario: 请求不携带 Unity 对象
- **WHEN** 系统构建一个动作打断请求
- **THEN** 请求 MUST 使用稳定状态 ID、请求类型、优先级、来源顺序或 tick、过期信息表达意图
- **AND** 请求 MUST NOT 保存 `AnimationClip`
- **AND** 请求 MUST NOT 保存 `UnityEngine.Object`
- **AND** 请求 MUST NOT 保存 Animancer 类型

#### Scenario: 上下文只保存逻辑事实
- **WHEN** 仲裁器读取当前状态上下文
- **THEN** 上下文 MUST 包含当前状态 ID、当前状态已持续时间和当前状态抗性
- **AND** 上下文 MAY 包含当前 simulation tick
- **AND** 上下文 MUST NOT 持有 MonoBehaviour、Transform、Animator 或 Animancer 引用

### Requirement: 打断策略规则
系统 MUST 使用显式策略描述从当前状态到目标状态的打断许可、最小优先级、时间规则和强制打断语义。没有匹配策略时，仲裁器 MUST 拒绝请求。

#### Scenario: 无策略时拒绝
- **GIVEN** 当前状态存在一个动作打断请求
- **AND** 策略集合中没有匹配当前状态和目标状态的策略
- **WHEN** 仲裁器执行裁决
- **THEN** 裁决 MUST 为 rejected
- **AND** 拒绝原因 MUST 表示没有匹配策略

#### Scenario: 优先级不足时拒绝
- **GIVEN** 请求匹配到一个策略
- **AND** 请求优先级低于策略要求的最小优先级
- **WHEN** 仲裁器执行裁决
- **THEN** 裁决 MUST 为 rejected
- **AND** 拒绝原因 MUST 表示优先级不足

#### Scenario: 当前状态抗性阻挡请求
- **GIVEN** 请求匹配到一个非强制策略
- **AND** 请求优先级小于或等于当前状态抗性
- **WHEN** 仲裁器执行裁决
- **THEN** 裁决 MUST 为 rejected
- **AND** 拒绝原因 MUST 表示被当前状态抗性阻挡

#### Scenario: 强制策略绕过抗性
- **GIVEN** 请求匹配到一个显式强制策略
- **AND** 请求满足策略最小优先级和时间规则
- **WHEN** 请求优先级小于或等于当前状态抗性
- **THEN** 仲裁器 MAY 接受该请求

### Requirement: 时间规则
系统 MUST 支持基础时间规则 `Always`、`AfterElapsedTime` 和 `DuringElapsedTimeWindow`。第一版时间判断 MUST 基于当前逻辑状态 elapsed time，不得直接读取 Animancer 当前播放进度或 clip length。

#### Scenario: Always 立即允许
- **GIVEN** 请求匹配到 `Always` 策略
- **AND** 请求满足优先级和抗性规则
- **WHEN** 仲裁器执行裁决
- **THEN** 仲裁器 MUST 接受该请求

#### Scenario: AfterElapsedTime 到时允许
- **GIVEN** 请求匹配到 `AfterElapsedTime` 策略
- **AND** 当前状态 elapsed time 小于策略要求时间
- **WHEN** 仲裁器执行裁决
- **THEN** 仲裁器 MUST 拒绝该请求
- **WHEN** 当前状态 elapsed time 大于或等于策略要求时间
- **THEN** 仲裁器 MUST 接受该请求

#### Scenario: DuringElapsedTimeWindow 只在窗口内允许
- **GIVEN** 请求匹配到 `DuringElapsedTimeWindow` 策略
- **WHEN** 当前状态 elapsed time 早于窗口开始
- **THEN** 仲裁器 MUST 拒绝该请求
- **WHEN** 当前状态 elapsed time 位于窗口开始和窗口结束之间
- **THEN** 仲裁器 MUST 接受该请求
- **WHEN** 当前状态 elapsed time 晚于窗口结束
- **THEN** 仲裁器 MUST 拒绝该请求

### Requirement: 确定性仲裁结果
系统 MUST 在同一帧多个候选请求中输出确定性的单一裁决。仲裁结果 MUST 说明是否接受、选择的请求、目标状态和拒绝原因。

#### Scenario: 选择最高优先级请求
- **GIVEN** 同一帧存在多个可接受请求
- **WHEN** 仲裁器执行裁决
- **THEN** 仲裁器 MUST 选择优先级最高的请求
- **AND** 裁决 MUST 包含该请求的目标状态

#### Scenario: 同优先级稳定选择
- **GIVEN** 同一帧存在多个可接受请求
- **AND** 它们拥有相同优先级
- **WHEN** 仲裁器执行裁决
- **THEN** 仲裁器 MUST 按来源顺序、提交顺序或等价稳定规则选择一个请求
- **AND** 多次使用相同输入执行裁决 MUST 得到相同结果

#### Scenario: 过期请求不参与裁决
- **GIVEN** 请求已超过自身过期 tick 或过期时间
- **WHEN** 仲裁器执行裁决
- **THEN** 该请求 MUST 不得成为 accepted decision 的 selected request

### Requirement: 与现有 Locomotion 边界
系统 MUST 保持当前统一状态机对 `FullBody/Locomotion/Idle|MoveStart|MoveLoop|MoveStop` 的流转职责。动作打断仲裁模块 MAY 作为 FullBody Action 请求进入统一状态机前的纯数据准入门，但 MUST NOT 接管当前 `MoveStop -> MoveStart` 或 `MoveStop -> Idle` 路径。

#### Scenario: MoveStop 重新输入仍由状态图处理
- **GIVEN** 当前基础移动阶段为 `MoveStop`
- **WHEN** 本帧重新出现移动输入
- **THEN** `MoveStop -> MoveStart` MUST 继续由统一角色逻辑状态机 transition 处理
- **AND** 本仲裁模块 MUST NOT 成为该流转的必需依赖

#### Scenario: Presenter 不依赖仲裁器
- **WHEN** 基础移动动画 Presenter 根据 `MovementAnimationContext` 播放 alias
- **THEN** Presenter MUST NOT 调用动作打断仲裁器
- **AND** Presenter MUST NOT 决定业务打断是否允许

#### Scenario: 动作请求准入发生在状态机之前
- **GIVEN** 输入缓冲中存在 Dodge、Attack 或等价 FullBody Action 请求
- **WHEN** 请求需要进入统一状态机
- **THEN** 请求 MUST 先经过动作打断仲裁入口
- **AND** 只有 accepted 请求 MAY 被转换为状态机输入事实

### Requirement: 模块边界和 BBB 参考边界
系统 MAY 参考 BBB 的 priority、resistance、interceptor 和 override 思路，但 MUST NOT 复制 BBB 运行时代码或依赖 BBB 运行时路径。动作打断仲裁模块 MUST 保持纯逻辑边界，供未来状态机、输入缓冲、tick 和编辑器消费。

#### Scenario: 不依赖 BBB 运行时
- **WHEN** 动作打断仲裁模块实现完成
- **THEN** 新增运行时代码 MUST NOT 引用 `BBBNexus` 命名空间
- **AND** MUST NOT 依赖 `Ref/BBB-Nexus` 下的运行时类型、Prefab 或 ScriptableObject

#### Scenario: 不直接切状态
- **WHEN** 仲裁器接受一个请求
- **THEN** 仲裁器 MUST 只返回裁决结果
- **AND** MUST NOT 持有或调用状态机实例
- **AND** MUST NOT 直接调用 `ChangeState`

#### Scenario: 不直接播放动画
- **WHEN** 仲裁器接受一个请求
- **THEN** 仲裁器 MUST NOT 调用 Animancer 或 Animator 播放 API
- **AND** MUST NOT 写入动画层权重、root motion 或 Transform

### Requirement: 校验和测试
系统 MUST 提供策略校验、自动测试和静态边界验证，证明仲裁规则可诊断、确定且不会污染现有动画与移动边界。

#### Scenario: 策略校验报告非法窗口
- **GIVEN** 一个 `DuringElapsedTimeWindow` 策略的结束时间早于开始时间
- **WHEN** 运行策略校验
- **THEN** 校验结果 MUST 报告错误

#### Scenario: 自动测试覆盖核心规则
- **WHEN** 运行动作打断仲裁 EditMode 测试
- **THEN** 测试 MUST 覆盖无请求、无策略、过期、优先级不足、抗性阻挡、强制打断、三种时间规则、多请求最高优先级和同优先级稳定选择

#### Scenario: 静态验证纯逻辑边界
- **WHEN** 检查动作打断仲裁模块源码
- **THEN** 静态搜索 MUST 能确认该模块不引用 Animancer、AnimationClip、Animator、CharacterController、Cinemachine、Input System 或 `BBBNexus`

### Requirement: FullBody Action 运行时准入门
系统 MUST 将 FullBody Action 请求进入统一状态机之前的准入裁决交给 `ActionInterruptArbiter` 或等价动作打断仲裁入口。优先级、抗性、force 和时间窗口 MUST 在生成可被状态机消费的动作请求事实之前完成裁决。

#### Scenario: accepted decision 生成状态机请求事实
- **GIVEN** 输入缓冲中存在未过期 Dodge 请求
- **AND** 当前动作上下文、请求和策略集合使 `ActionInterruptArbiter` 返回 accepted decision
- **WHEN** FullBody Action 请求门面处理本帧输入
- **THEN** 系统 MUST 生成可被统一状态机消费的 Dodge 请求事实
- **AND** 该请求事实 MUST 保留动作变体和世界方向
- **AND** 统一状态机 MAY 通过 `HasInputRequest(Dodge)` 进入 `FullBody/Action/Dodge`

#### Scenario: rejected decision 不生成状态机请求事实
- **GIVEN** 输入缓冲中存在未过期 Dodge 请求
- **AND** `ActionInterruptArbiter` 返回 rejected decision
- **WHEN** FullBody Action 请求门面处理本帧输入
- **THEN** 系统 MUST NOT 生成可被统一状态机消费的 Dodge 请求事实
- **AND** 统一状态机 MUST NOT 因该 rejected 请求进入 `FullBody/Action/Dodge`
- **AND** 输入缓冲中的请求 MUST 保留到过期或后续合法消费

#### Scenario: 仲裁日志可追踪准入结果
- **WHEN** FullBody Action 请求门面调用 `ActionInterruptArbiter`
- **THEN** 系统 MUST 保留 accepted 或 rejected 诊断日志
- **AND** 日志 MUST 能说明目标状态、请求优先级、策略最小优先级和拒绝原因

### Requirement: 默认动作入口不得绕过仲裁器
系统 MUST NOT 在默认 FullBody Action 入口中使用状态机 transition 条件直接裁决动作请求优先级、抗性、force 或时间窗口。统一状态机 MUST 只消费已经过动作仲裁入口接受的请求事实。

#### Scenario: Dodge 入口不直接判断优先级
- **WHEN** 默认统一状态机配置表达 `Locomotion/* -> FullBody/Action/Dodge`
- **THEN** 该 transition MUST NOT 通过 `RequestPriorityAtLeast` 或等价状态机条件直接判断请求优先级
- **AND** 优先级 MUST 由 `ActionInterruptArbiter` 或等价动作打断仲裁入口裁决

#### Scenario: 状态机 solver 不依赖仲裁器实现
- **WHEN** 检查统一状态机 runner 和 transition evaluator 源码
- **THEN** 它们 MUST NOT 引用 `ActionInterruptArbiter`
- **AND** MUST NOT 读取 `ActionInterruptPolicySetSO`
- **AND** MUST NOT 执行动作策略匹配

#### Scenario: 保留纯数据状态机输入边界
- **GIVEN** FullBody Action 请求已经被仲裁接受
- **WHEN** 统一状态机推进本帧状态
- **THEN** 状态机 MUST 只读取纯数据输入事实
- **AND** MUST NOT 直接读取输入缓冲、ScriptableObject 策略资产或 MonoBehaviour 请求门面

### Requirement: FullBody Action 准入上下文收口
系统 MUST 在 FullBody Action 请求进入统一状态机之前构建完整的动作仲裁上下文。该上下文 MUST 包含当前 action state、当前 action elapsed seconds、当前 action resistance 和当前 tick。priority、resistance、force 和 timing window 的裁决 MUST 只发生在动作仲裁入口，不得分散到状态机 transition 条件中。

#### Scenario: Dodge 请求使用配置化 priority 和 resistance
- **GIVEN** 默认角色绑定了 Dodge 动作配置
- **AND** 输入缓冲中存在 Dodge 请求
- **WHEN** FullBody Action 请求门面构建仲裁请求和上下文
- **THEN** 请求 priority MUST 来自 Dodge 动作配置
- **AND** 当前 state 为 `Action.Dodge` 时 context resistance MUST 来自 Dodge 动作配置
- **AND** 当前 state 为 `Action.None` 时 context resistance MUST 为 0

#### Scenario: 状态机不裁决动作请求 priority
- **WHEN** 默认 FullBody Action 入口处理 Dodge 请求
- **THEN** 状态机 transition MUST NOT 使用 `RequestPriorityAtLeast` 或等价条件判断请求 priority
- **AND** `ActionInterruptArbiter` MUST 是该请求 priority、resistance、force 和 timing window 的唯一准入裁决入口

#### Scenario: rejected 请求不生成状态机事实
- **GIVEN** Dodge 请求被当前 resistance、policy min priority 或 timing window 拒绝
- **WHEN** FullBody Action 请求门面完成本帧处理
- **THEN** 系统 MUST NOT 生成可被统一状态机消费的 Dodge input fact
- **AND** 状态机 MUST NOT 因该 rejected 请求进入 `FullBody/Action/Dodge`

### Requirement: Dodge 作为 FullBody Action 管线实例
系统 MUST 将 Dodge 作为 FullBody Action 管线的一个动作实例处理。Dodge 可以拥有自己的实例配置、请求参数、方向/后撤变体、动作位移配置、转向配置、run latch 和返回 Locomotion 规则，但这些差异 MUST 通过同一条 FullBody Action 管线表达。

#### Scenario: Dodge 实例行为仍走同一准入
- **GIVEN** 输入缓冲中存在 Dodge 请求
- **WHEN** 系统处理该请求
- **THEN** 系统 MAY 使用 Dodge 实例逻辑解析 Directional 或 Backstep
- **AND** MAY 使用 Dodge 实例配置决定位移、转向和 resistance
- **BUT** 请求进入统一状态机前 MUST 仍经过 `CharacterActionRequestSubmissionArbiter` 和 `ActionInterruptArbiter`

#### Scenario: Dodge 输出仍由统一状态机负责
- **GIVEN** Dodge 请求已被仲裁接受
- **WHEN** 统一状态机进入 `FullBody/Action/Dodge`
- **THEN** Dodge 的动作位移、动画请求、输入消费和返回 Locomotion MUST 仍由统一状态机输出及其现有执行边界负责
- **AND** 仲裁器 MUST NOT 直接播放 Dodge 动画或执行 Dodge 位移

### Requirement: 动作准入条件不得回流状态机
系统 MUST 防止动作请求 priority 条件重新成为统一状态机 transition 的一部分。状态机 transition 的 `priority` 字段 MAY 继续用于多个 transition 同时满足时的选择顺序，但 MUST NOT 表达动作请求的准入优先级。

#### Scenario: transition priority 仍用于状态图选边
- **GIVEN** 同一个当前状态存在多条条件已满足的 transition
- **WHEN** 统一状态机 runner 解析 transition
- **THEN** runner MUST 使用 transition 自身 priority 选择要执行的 transition
- **AND** 该 priority MUST NOT 替代动作请求 priority、policy min priority 或 current resistance

#### Scenario: 默认动作入口没有请求优先级条件
- **WHEN** 检查默认状态机定义和默认状态机资产
- **THEN** `Locomotion/* -> FullBody/Action/Dodge` transition MUST 只消费已被仲裁接受的 Dodge input fact
- **AND** MUST NOT 包含 `RequestPriorityAtLeast`、`minPriority` 动作准入条件或等价状态机条件

### Requirement: TurnBack Intent 到请求事实的单向准入
状态请求仲裁入口 MUST 将 `LocomotionTurnBackIntent` 视为 TurnBack 请求的候选输入，并在 priority、resistance、force、过期和 timeline window 规则全部通过后，才生成可被统一状态机消费的 TurnBack request fact。仲裁 rejected 时 MUST NOT 生成 accepted request fact。

#### Scenario: intent 构建候选请求
- **GIVEN** locomotion facts 中存在有效 `LocomotionTurnBackIntent`
- **AND** 当前状态为 `FullBody/Locomotion/MoveStart` 或 `FullBody/Locomotion/MoveLoop`
- **AND** gait 和时间窗口允许 TurnBack 候选请求被提交
- **WHEN** 状态请求仲裁入口处理本帧请求
- **THEN** 系统 MUST 构建 TurnBack 候选 request
- **AND** 该 request MUST 携带 priority、origin tick、expire tick 和 world direction

#### Scenario: accepted 后生成状态机事实
- **GIVEN** TurnBack 候选 request 匹配策略
- **AND** request priority 高于有效 resistance
- **AND** timeline window 条件满足
- **WHEN** `ActionInterruptArbiter` 返回 accepted decision
- **THEN** 状态请求仲裁入口 MUST 生成 `CharacterInputRequestFact(InputRequestKind.TurnBack)`

#### Scenario: rejected 后不生成状态机事实
- **GIVEN** TurnBack 候选 request 存在
- **AND** `ActionInterruptArbiter` 因优先级、抗性、过期、策略缺失或 window 条件拒绝该 request
- **WHEN** 状态请求仲裁入口返回本帧结果
- **THEN** 结果 MUST NOT 包含 accepted TurnBack request fact
- **AND** 统一状态机 MUST 无法因该 rejected request 进入 TurnBack

### Requirement: 请求候选构建与仲裁分离
系统 MUST 将请求候选构建和请求准入仲裁分离。request candidate builder MAY 读取自身需要的输入 buffer、Locomotion facts、Action 配置和 current timeline facts 来构建候选请求；`ActionInterruptArbiter` 或等价仲裁入口 MUST 仍是 priority、resistance、force、policy、timeline window 和过期规则的唯一准入裁决者。

#### Scenario: builder 只生成候选
- **WHEN** request candidate builder 发现一个可提交请求
- **THEN** builder MUST 只生成纯数据 candidate request 或等价输入
- **AND** MUST NOT 直接切换状态机状态
- **AND** MUST NOT 直接消费输入缓冲
- **AND** MUST NOT 直接播放动画或执行运动

#### Scenario: 仲裁器产生 accepted/rejected 决策
- **GIVEN** request candidate collection 提供 0..N 个候选请求
- **WHEN** FullBody request submission arbiter 处理候选
- **THEN** 每个需要准入裁决的候选 MUST 经过 `ActionInterruptArbiter`
- **AND** rejected 候选 MUST NOT 生成状态机 request fact
- **AND** accepted 候选 MAY 参与本帧最高优先级选择

#### Scenario: 候选集合稳定排序
- **GIVEN** 多个候选请求在同一帧被接受
- **WHEN** gate 选择本帧 request fact
- **THEN** 选择规则 MUST 使用 request priority
- **AND** 同 priority MUST 使用 builder 顺序、origin step 或等价稳定 tie-break
- **AND** 相同输入序列 MUST 产生相同 accepted request fact 序列


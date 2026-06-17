# action-interrupt-arbiter Delta

## MODIFIED Requirements
### Requirement: FullBody Action 运行时准入门
系统 MUST 将 FullBody Action 请求进入 Action lifecycle 之前的准入裁决交给 `ActionInterruptArbiter` 或等价动作打断仲裁入口。优先级、抗性、force 和时间窗口 MUST 在创建 accepted resolved action 或 Action lifecycle seed 之前完成裁决。accepted Dodge MUST NOT 生成要求默认 Locomotion graph 进入 `FullBody/Action/Dodge` 或 `Action.Dodge` 的状态机请求事实。

#### Scenario: accepted decision 生成 Action lifecycle submission
- **GIVEN** 输入缓冲中存在未过期 Dodge 请求
- **AND** 当前动作上下文、请求和策略集合使 `ActionInterruptArbiter` 返回 accepted decision
- **WHEN** FullBody Action 请求门面处理本帧输入
- **THEN** 系统 MUST 生成 accepted resolved action 或等价 Action lifecycle submission
- **AND** 该 submission MUST 保留动作变体、世界方向、priority、source step 和 motion/animation seed
- **AND** 默认 Locomotion graph MUST NOT 通过 `HasInputRequest(Dodge)` 进入 `FullBody/Action/Dodge`

#### Scenario: rejected decision 不生成 Action lifecycle submission
- **GIVEN** 输入缓冲中存在未过期 Dodge 请求
- **AND** `ActionInterruptArbiter` 返回 rejected decision
- **WHEN** FullBody Action 请求门面处理本帧输入
- **THEN** 系统 MUST NOT 生成 accepted resolved action
- **AND** Action lifecycle MUST NOT active `Action.Dodge`
- **AND** 输入缓冲中的请求 MUST 保留到过期或后续合法消费

#### Scenario: 仲裁日志可追踪准入结果
- **WHEN** FullBody Action 请求门面调用 `ActionInterruptArbiter`
- **THEN** 系统 MUST 保留 accepted 或 rejected 诊断日志
- **AND** 日志 MUST 能说明 action id、请求优先级、策略最小优先级和拒绝原因
- **AND** 日志 MUST NOT 依赖默认 graph target state 才能解释结果

### Requirement: 默认动作入口不得绕过仲裁器
系统 MUST NOT 在默认 FullBody Action 入口中使用 Locomotion graph transition 条件直接裁决动作请求优先级、抗性、force 或时间窗口。默认 Locomotion graph MUST 不包含 Dodge 入口 transition；Action lifecycle MUST 只消费已经过动作仲裁入口接受的纯数据 submission。

#### Scenario: Dodge 入口不直接判断优先级
- **WHEN** 默认 Corin Locomotion graph 表达基础移动 transition
- **THEN** graph MUST NOT 包含 `Locomotion.* -> FullBody/Action/Dodge`
- **AND** graph MUST NOT 包含 `Locomotion.* -> Action.Dodge`
- **AND** 优先级 MUST 由 `ActionInterruptArbiter` 或等价动作打断仲裁入口裁决

#### Scenario: 状态机 solver 不依赖仲裁器实现
- **WHEN** 检查 Locomotion graph runner 和 transition evaluator 源码
- **THEN** 它们 MUST NOT 引用 `ActionInterruptArbiter`
- **AND** MUST NOT 读取 `ActionInterruptPolicySetSO`
- **AND** MUST NOT 执行动作策略匹配

#### Scenario: 保留纯数据 Action lifecycle 输入边界
- **GIVEN** FullBody Action 请求已经被仲裁接受
- **WHEN** Action lifecycle 推进本帧状态
- **THEN** Action lifecycle MUST 只读取纯数据 resolved action 或 lifecycle restore facts
- **AND** MUST NOT 直接读取输入缓冲、ScriptableObject 策略资产或 MonoBehaviour 请求门面

### Requirement: FullBody Action 准入上下文收口
系统 MUST 在 FullBody Action 请求进入 Action lifecycle 之前构建完整的动作仲裁上下文。该上下文 MUST 包含当前 action state、当前 action elapsed seconds、当前 action resistance 和当前 tick。priority、resistance、force 和 timing window 的裁决 MUST 只发生在动作仲裁入口，不得分散到 Locomotion graph transition 条件中。

#### Scenario: Dodge 请求使用配置化 priority 和 resistance
- **GIVEN** 默认角色绑定了 Dodge 动作配置
- **AND** 输入缓冲中存在 Dodge 请求
- **WHEN** FullBody Action 请求门面构建仲裁请求和上下文
- **THEN** 请求 priority MUST 来自 Dodge 动作配置
- **AND** 当前 action 为 `Action.Dodge` 时 context resistance MUST 来自 Dodge 动作配置
- **AND** 当前 action 为 `Action.None` 时 context resistance MUST 为 0

#### Scenario: Locomotion graph 不裁决动作请求 priority
- **WHEN** 默认 FullBody Action 入口处理 Dodge 请求
- **THEN** Locomotion graph transition MUST NOT 使用 `RequestPriorityAtLeast` 或等价条件判断请求 priority
- **AND** `ActionInterruptArbiter` MUST 是该请求 priority、resistance、force 和 timing window 的唯一准入裁决入口

#### Scenario: rejected 请求不生成 Action lifecycle facts
- **GIVEN** Dodge 请求被当前 resistance、policy min priority 或 timing window 拒绝
- **WHEN** FullBody Action 请求门面完成本帧处理
- **THEN** 系统 MUST NOT 生成 accepted Dodge lifecycle seed
- **AND** Action lifecycle MUST NOT 因该 rejected 请求 active `Action.Dodge`

### Requirement: Dodge 作为 FullBody Action 管线实例
系统 MUST 将 Dodge 作为统一 request submission、action resolver、Action lifecycle 和 frame output 的一个动作实例处理。Dodge 可以拥有自己的实例配置、请求参数、方向/后撤变体、动作位移配置、转向配置、Run latch completion policy 和返回 Locomotion 规则，但这些差异 MUST 通过统一请求/打断仲裁、Action lifecycle 和 `CharacterFrameSubmission` 输出提交表达，不得形成 Dodge 专用准入管线或输出管线。

#### Scenario: Dodge 实例行为仍走同一准入
- **GIVEN** 输入缓冲中存在 Dodge 请求
- **WHEN** 系统处理该请求
- **THEN** 系统 MAY 使用 Dodge 实例逻辑解析 Directional 或 Backstep
- **AND** MAY 使用 Dodge 实例配置决定位移、转向和 resistance
- **BUT** 请求进入 Action lifecycle 前 MUST 作为 request submission 进入统一请求/打断仲裁

#### Scenario: Dodge 输出由 Action lifecycle 和角色提交负责
- **GIVEN** Dodge 请求已被仲裁接受
- **WHEN** Action lifecycle active `Action.Dodge`
- **THEN** Dodge 的动作位移、动画请求、输入消费和完成事实 MUST 由 Action lifecycle 与 `CharacterFrameSubmission` 或等价角色级输出提交表达
- **AND** 仲裁器 MUST NOT 直接播放 Dodge 动画或执行 Dodge 位移
- **AND** 默认 Locomotion graph MUST NOT 持有 Dodge 输出配置

#### Scenario: Directional completion policy 写 Run latch
- **GIVEN** Dodge resolved action 为 Directional
- **AND** completion frame 仍有移动输入
- **WHEN** Action motion resolver 判定 Directional 完成
- **THEN** frame output MUST 请求写 Locomotion Run latch
- **AND** 该请求 MUST 不依赖继续按住 Shift

#### Scenario: 无移动 Dodge completion 等待动作动画
- **GIVEN** Dodge resolved action 为 Backstep，或 Directional completion frame 没有移动输入
- **AND** Dodge 动作位移 duration 已达到
- **WHEN** 匹配 Action 动作动画尚未播放完成
- **THEN** Action lifecycle MUST 保持 active
- **AND** frame output MUST NOT 写 Run latch
- **AND** 仲裁器 MUST NOT 通过额外 Dodge 专用出口放行动作

### Requirement: 动作准入条件不得回流状态机
系统 MUST 防止动作请求 priority 条件重新成为 Locomotion graph transition 的一部分。Locomotion graph transition 的 `priority` 字段 MAY 继续用于多个 transition 同时满足时的选择顺序，但 MUST NOT 表达动作请求的准入优先级。

#### Scenario: transition priority 仍用于状态图选边
- **GIVEN** 同一个当前 Locomotion state 存在多条条件已满足的 transition
- **WHEN** graph runner 解析 transition
- **THEN** runner MUST 使用 transition 自身 priority 选择要执行的 transition
- **AND** 该 priority MUST NOT 替代动作请求 priority、policy min priority 或 current resistance

#### Scenario: 默认动作入口没有请求优先级条件
- **WHEN** 检查默认 Locomotion graph 定义和默认 graph 资产
- **THEN** graph MUST NOT 包含 `Locomotion.* -> FullBody/Action/Dodge`
- **AND** graph MUST NOT 包含 `RequestPriorityAtLeast`、`minPriority` 动作准入条件或等价状态机条件

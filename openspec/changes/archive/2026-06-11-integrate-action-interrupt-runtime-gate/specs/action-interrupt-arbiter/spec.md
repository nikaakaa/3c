## ADDED Requirements
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

## MODIFIED Requirements
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

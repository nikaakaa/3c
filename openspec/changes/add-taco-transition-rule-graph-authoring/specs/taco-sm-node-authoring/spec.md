# taco-sm-node-authoring Specification

## MODIFIED Requirements

### Requirement: StateMachineGraph 只表达同层状态结构
系统 MUST 让 `StateMachineGraph` 只包含 `Enter`、`AnyState`、`Exit` 和 `StateNode`。它 MUST NOT 创建 `RootNode`、`OnEnter`、`OnExit`、`StateMachineNode`、普通 `RunnableNode`、Timeline 行为节点、Tree 行为节点或条件用 `ValueNode`。Transition 条件 MUST 下钻到 Transition 边引用的 `TransitionRuleGraph`。

#### Scenario: 创建规则统一
- **WHEN** 编辑器、拖拽、粘贴或脚本路径向 `StateMachineGraph` 创建节点
- **THEN** 系统 MUST 统一通过 `CanCreateNodeType()` 判定
- **AND** 非法节点 MUST NOT 进入正式节点集合

#### Scenario: 控制节点唯一
- **WHEN** 用户打开或校验 `StateMachineGraph`
- **THEN** 每层 MUST 且只能包含一个 `Enter`、一个 `AnyState` 和一个 `Exit`
- **AND** 每层 MUST 至少包含一个 `StateNode`

#### Scenario: 条件节点不在状态机本层
- **WHEN** 用户需要配置 Idle 到 Attack 的 Transition 条件
- **THEN** 用户 MUST 打开该 Transition 的 `TransitionRuleGraph`
- **AND** 系统 MUST NOT 在 `StateMachineGraph` 本层创建条件 `ValueNode`

### Requirement: Transition 是同层 BaseEdge 语义
系统 MUST 将状态转换表达为 `StateMachineGraph` 内的 `BaseEdge`，MUST NOT 新增 `TransitionNode`。Transition MUST 只连接同层 `Enter`、`AnyState`、`StateNode` 和 `Exit`。Transition 条件 MUST 由边引用的 `TransitionRuleGraph` 表达。

#### Scenario: 合法端点
- **WHEN** 用户连接 Transition flow
- **THEN** 合法连接 MUST 是 `Enter -> StateNode`、`AnyState -> StateNode|Exit`、`StateNode -> StateNode|Exit`
- **AND** `RootNode`、`StateMachineNode`、普通 `RunnableNode`、`TimelineNode`、Tree 节点、`ValueNode` 和规则图节点 MUST NOT 成为 Transition 端点

#### Scenario: 条件和优先级
- **WHEN** Transition 配置条件
- **THEN** 条件 MUST 由该 Transition 边引用的 `TransitionRuleGraph` 输出 Bool
- **AND** `AnyState` Transition MUST 配置规则图
- **AND** runtime MUST 按 Transition 优先级选择可通过的边

#### Scenario: 无条件普通转换
- **WHEN** `Enter -> StateNode` 或 `StateNode -> StateNode|Exit` 没有配置规则图
- **THEN** runtime MUST 将该 Transition 视为无条件可通过

### Requirement: 状态机运行时解释
系统 MUST 让 `StateMachineNode` 驱动自己引用的 `StateMachineGraph`，并让 `StateMachineGraphRuntime` 以 `StateNode` 作为 active state。解释器 MUST 从 `Enter` 读取初始 Transition，在每帧写入当前 `BaseGraph.DeltaTime`，并通过 Transition 边引用的 `TransitionRuleGraph` 判断条件。

#### Scenario: 父级 tick 状态机入口
- **WHEN** 父级行为图 tick 到 `StateMachineNode`
- **THEN** `StateMachineNode` MUST 进入引用的 `StateMachineGraph`
- **AND** 父级行为图 MUST 只看到该节点的 `Running/Success/Failure`

#### Scenario: active state tick
- **WHEN** 状态机已有 active state
- **THEN** 本帧 MUST 只 tick active `StateNode` 的状态行为 `SubTree`
- **AND** 其它状态 MUST NOT 被 tick，除非 Transition 切换 active state

#### Scenario: Transition 条件求值
- **WHEN** runtime 枚举 active state 的 outgoing Transition
- **THEN** runtime MUST 按优先级求值对应规则图
- **AND** runtime MUST NOT 读取旧 BoolPort 条件字段作为 fallback

#### Scenario: AnyState 和 Exit
- **WHEN** 状态机已有 active state
- **THEN** runtime MUST 在 tick active state 前检查 `AnyState` Transition
- **AND** 命中 `Exit` MUST 让本层状态机返回 `Success`

#### Scenario: Stop 和 Reset 传播
- **WHEN** 父级 Graph stop 或 reset `StateMachineNode`
- **THEN** 当前 active `StateNode` MUST stop 或 reset 自己的状态行为 `SubTree`

### Requirement: Timeline 和输入通过正式状态行为或规则图链路接入
系统 MUST NOT 将 `TimelineNode` 或 InputAction 节点作为 `StateMachineGraph` 同层状态或 Transition flow 端点。Timeline MUST 通过 `StateNode` 下钻的状态行为 `SubTree` 接入；InputAction 值 MUST 在 `TransitionRuleGraph` 中作为条件输入接入。

#### Scenario: Idle 播放 Timeline
- **WHEN** Idle 状态需要播放 Timeline
- **THEN** 用户 MUST 在 Idle 引用的状态行为 `SubTree` 中创建 `TimelineNode`
- **AND** `TimelineNode` MUST 从所在运行 Graph 获得 `Owner.DeltaTime`

#### Scenario: 输入驱动 Transition
- **WHEN** 用户需要用 InputAction Bool 驱动 Transition
- **THEN** 用户 MUST 在该 Transition 的 `TransitionRuleGraph` 中创建 InputAction Bool 输入节点
- **AND** 该输入节点 MUST NOT 出现在 `StateMachineGraph` 本层合法 Transition flow 端点候选中

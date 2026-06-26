## MODIFIED Requirements

### Requirement: Timeline 通过状态行为 Graph 接入
系统 MUST NOT 将 `TimelineNode` 作为 `StateMachineGraph` 同层状态节点创建。Timeline MUST 通过 `StateMachineNode` 下钻到状态行为 Graph 后接入；承载 `TimelineNode` 的状态行为 Graph MUST 在 tick 时获得 `BaseGraph.DeltaTime`。

#### Scenario: Idle 状态播放 Timeline
- **WHEN** Idle `StateMachineNode` 引用一个状态行为 Graph
- **THEN** 用户 MAY 在该状态行为 Graph 中创建 `TimelineNode`
- **AND** 该状态行为 Graph MUST 在被 tick 时写入或继承本帧 `BaseGraph.DeltaTime`
- **AND** `TimelineNode` MUST 从该运行 Graph 获得 `Owner.DeltaTime`

#### Scenario: 状态机图不直接创建 TimelineNode
- **WHEN** 用户在 `StateMachineGraph` 的节点搜索中查找 Timeline 节点
- **THEN** 系统 MUST NOT 将 `TimelineNode` 暴露为同层可创建状态节点
- **AND** 用户 MUST 通过普通 `StateMachineNode` 的下钻 Graph 创建 Timeline 行为

#### Scenario: Timeline 行为串联
- **WHEN** 用户需要 Timeline 播放结束后继续执行同一状态行为 Graph 内的其它 runnable 节点
- **THEN** `TimelineNode` MUST 提供可连接的 `Output` flow port
- **AND** 系统 MUST NOT 为 Timeline 串联新增并行端口协议

### Requirement: 状态机运行时解释
系统 MUST 让父级 Graph tick 到 `StateMachineNode` 时，由该 SMNode 负责进入并驱动自己引用的下一层 Graph，再把结果以 `Running/Success/Failure` 返回给父级 Graph。状态机解释器 MUST 在每帧把本帧 `DeltaTime` 写入被解释的 `StateMachineGraph`。

#### Scenario: 父级行为图 tick Locomotion
- **WHEN** 父级行为图 tick 到 Locomotion `StateMachineNode`
- **THEN** Locomotion MUST 进入自己引用的 LocomotionGraph
- **AND** LocomotionGraph 如果是 `StateMachineGraph`，MUST 从 `Enter` 开始解释
- **AND** 父级行为图 MUST NOT 直接 tick IdleGraph 或 WalkGraph 内部节点

#### Scenario: 状态下钻进入下一层状态机
- **WHEN** 当前 `StateMachineGraph` 的 active `StateMachineNode` 引用下一层 `StateMachineGraph`
- **THEN** 下一层 `StateMachineGraph` MUST 从 `Enter` 开始解释
- **AND** 系统 MUST NOT 通过当前层内部 edge 连接到下一层 `Enter`

#### Scenario: 状态机图写入本帧时间
- **WHEN** `StateMachineGraphRuntime.Update(deltaTime)` 被调用
- **THEN** 当前 `StateMachineGraph` MUST 记录该 `deltaTime`
- **AND** active `StateMachineNode` 和 transition 条件节点 MUST 能通过 `Owner.DeltaTime` 读取同一帧时间

#### Scenario: 状态机图 tick active state
- **WHEN** LocomotionGraph 当前 active state 是 Idle
- **THEN** 本帧 MUST 只 tick Idle 及其下钻 Graph
- **AND** Walk MUST NOT 被 tick，除非 Transition 切换 active state 到 Walk

#### Scenario: Root 和 Enter 不持续 tick
- **WHEN** LocomotionGraph 已经通过入口源激活了 Idle
- **THEN** 后续帧 MUST NOT tick `Root` 或 `Enter`
- **AND** 系统 MUST 只把 `Root` 作为行为树进入当前层时没有 active state 的图解释入口
- **AND** 系统 MUST 只把 `Enter` 作为父状态下钻进入下一层时没有 active state 的图解释入口

#### Scenario: AnyState 优先检查
- **WHEN** 状态机 Graph 已经存在 active state
- **THEN** 运行时 MUST 在 tick active state 前检查 `AnyState` Transition
- **AND** 命中后 MUST stop 当前 active state 并切换到目标状态或 Exit

#### Scenario: Transition 切换 active state
- **WHEN** Idle 到 Walk 的 Transition 条件成立
- **THEN** 系统 MUST stop Idle 当前执行链路
- **AND** 系统 MUST 将 active state 切换为 Walk
- **AND** 下一次状态执行 MUST 从 Walk 引用的 Graph 开始

#### Scenario: Active state 完成但没有转换
- **WHEN** 当前 active state 的下钻 Graph 返回 `Success`
- **AND** 该 active state 没有命中任何 Transition
- **THEN** 本层 `StateMachineGraph` MUST 保持 `Running`
- **AND** 当前 active state MUST 保持不变，直到后续 Transition 或 Exit 命中

#### Scenario: Exit 完成本层 Graph
- **WHEN** active state 命中指向 `Exit` 的 Transition
- **THEN** 本层 `StateMachineGraph` MUST 返回 Success
- **AND** 父级 `StateMachineNode` MUST 以该结果继续自己的父级生命周期

#### Scenario: Stop 和 Reset 传播
- **WHEN** 父级 Graph stop 或 reset Locomotion `StateMachineNode`
- **THEN** Locomotion MUST stop 或 reset 当前 active state 链路
- **AND** 当前 active state MUST stop 或 reset 自己的下钻 Graph
